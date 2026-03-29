using System.Collections.ObjectModel;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Lumina.Input;

public sealed class BrowserElementsDialog : IDisposable
{
    private readonly object _sync = new();
    private readonly ManualResetEventSlim _ready = new(initialState: false);
    private readonly Thread _uiThread;
    private Exception? _startupException;
    private Window? _window;
    private ComboBox? _typeSelector;
    private TextBox? _filterTextBox;
    private TreeView? _treeView;
    private TextBlock? _previewText;
    private Button? _activateButton;
    private Button? _moveButton;
    private readonly ObservableCollection<BrowserNavigator.ElementsListItem> _items = [];
    private List<BrowserNavigator.ElementsListItem> _allItems = [];
    private bool _disposed;
    private string _searchText = string.Empty;
    private string? _lastSelectedElementRuntimeId;
    private DateTime _lastSearchInputUtc = DateTime.MinValue;
    private static int s_lastSelectedTypeIndex = -1;
    private static string s_lastFilterText = string.Empty;
    private static readonly TimeSpan SearchResetWindow = TimeSpan.FromSeconds(1);

    private static readonly (string Key, string Label)[] ElementTypes =
    [
        ("link", "روابط"),
        ("visitedLink", "روابط مزورة"),
        ("unvisitedLink", "روابط غير مزورة"),
        ("heading", "عناوين"),
        ("formField", "عناصر النماذج"),
        ("button", "أزرار"),
        ("toggleButton", "أزرار تبديل"),
        ("graphic", "رسومات"),
        ("frame", "إطارات"),
        ("separator", "فواصل"),
        ("blockQuote", "اقتباسات كتلية"),
        ("embeddedObject", "عناصر مضمنة"),
        ("textParagraph", "فقرات نصية"),
        ("notLinkBlock", "نص خارج الروابط"),
        ("landmark", "معالم"),
        ("table", "جداول"),
        ("list", "قوائم"),
        ("listItem", "عناصر القوائم"),
        ("treeItem", "عناصر الشجرة"),
        ("tab", "علامات التبويب"),
        ("menuItem", "عناصر القوائم التفاعلية"),
        ("article", "مقالات"),
        ("figure", "أشكال"),
        ("grouping", "مجموعات"),
        ("progressBar", "أشرطة التقدم")
    ];

    public BrowserElementsDialog()
    {
        _uiThread = new Thread(RunWindow)
        {
            IsBackground = true,
            Name = "LuminaBrowserElementsDialog"
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
        _ready.Wait();
        if (_startupException is not null)
        {
            throw new InvalidOperationException("تعذر إنشاء نافذة عناصر الويب.", _startupException);
        }
    }

    public string ShowOrFocus()
    {
        if (_window is null)
        {
            return "تعذر فتح قائمة عناصر الصفحة.";
        }

        _window.Dispatcher.BeginInvoke(() =>
        {
            SelectPreferredType();
            RestoreFilterText();
            RefreshCurrentTypeItems();
            _window.Show();
            _window.Activate();
            _window.Focus();
            (_filterTextBox as UIElement ?? _treeView)?.Focus();
        });

        return "تم فتح قائمة عناصر الصفحة.";
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        if (_window is not null)
        {
            _window.Dispatcher.Invoke(() => _window.Close());
        }
    }

    private void RunWindow()
    {
        try
        {
            var typeLabel = new TextBlock
            {
                Text = "نوع العنصر",
                Margin = new Thickness(12, 12, 12, 6),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };

            _typeSelector = new ComboBox
            {
                Margin = new Thickness(12, 0, 12, 10),
                ItemsSource = ElementTypes.Select(item => item.Label).ToArray(),
                SelectedIndex = 0
            };
            _typeSelector.SelectionChanged += (_, _) =>
            {
                s_lastSelectedTypeIndex = _typeSelector.SelectedIndex;
                RefreshCurrentTypeItems();
            };

            var filterLabel = new TextBlock
            {
                Text = "تصفية",
                Margin = new Thickness(12, 0, 12, 6),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };

            _filterTextBox = new TextBox
            {
                Margin = new Thickness(12, 0, 12, 10)
            };
            _filterTextBox.TextChanged += (_, _) =>
            {
                s_lastFilterText = _filterTextBox.Text ?? string.Empty;
                ApplyFilter();
            };
            _filterTextBox.KeyDown += (_, args) =>
            {
                if (args.Key == Key.Down)
                {
                    _treeView?.Focus();
                    GetSelectedTreeNode()?.BringIntoView();
                    args.Handled = true;
                }
                else if (args.Key == Key.Enter)
                {
                    InvokeDefaultAction();
                    args.Handled = true;
                }
                else if (args.Key == Key.Escape)
                {
                    _window?.Hide();
                    args.Handled = true;
                }
            };

            _treeView = new TreeView
            {
                Margin = new Thickness(12, 0, 12, 12),
                Background = new SolidColorBrush(Color.FromRgb(18, 24, 38)),
                Foreground = Brushes.WhiteSmoke,
                BorderBrush = new SolidColorBrush(Color.FromRgb(62, 79, 112))
            };
            _treeView.SelectedItemChanged += (_, _) =>
            {
                _lastSelectedElementRuntimeId = GetSelectedItemRuntimeId();
                UpdateButtonsState();
                UpdatePreview();
            };
            _treeView.MouseDoubleClick += (_, _) => MoveToSelection();
            _treeView.KeyDown += (_, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    InvokeDefaultAction();
                    args.Handled = true;
                }
                else if (args.Key == Key.Escape)
                {
                    _window?.Hide();
                    args.Handled = true;
                }
                else if (args.Key == Key.Up && IsSelectedFirstLeafNode())
                {
                    _filterTextBox?.Focus();
                    _filterTextBox?.SelectAll();
                    args.Handled = true;
                }
                else if (HandleTreeSearchKey(args))
                {
                    args.Handled = true;
                }
            };

            _activateButton = new Button
            {
                Content = "تفعيل",
                Width = 110,
                Margin = new Thickness(12, 0, 8, 12)
            };
            _activateButton.Click += (_, _) => ActivateSelection();

            _moveButton = new Button
            {
                Content = "انتقال",
                Width = 110,
                Margin = new Thickness(0, 0, 8, 12)
            };
            _moveButton.Click += (_, _) => MoveToSelection();

            var closeButton = new Button
            {
                Content = "إغلاق",
                Width = 110,
                Margin = new Thickness(0, 0, 12, 12)
            };
            closeButton.Click += (_, _) => _window?.Hide();

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttons.Children.Add(_activateButton);
            buttons.Children.Add(_moveButton);
            buttons.Children.Add(closeButton);

            _previewText = new TextBlock
            {
                Margin = new Thickness(12, 0, 12, 12),
                Foreground = Brushes.Gainsboro,
                TextWrapping = TextWrapping.Wrap
            };

            var root = new DockPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(10, 14, 24))
            };

            DockPanel.SetDock(typeLabel, Dock.Top);
            DockPanel.SetDock(_typeSelector, Dock.Top);
            DockPanel.SetDock(filterLabel, Dock.Top);
            DockPanel.SetDock(_filterTextBox, Dock.Top);
            DockPanel.SetDock(buttons, Dock.Bottom);
            DockPanel.SetDock(_previewText, Dock.Bottom);
            root.Children.Add(typeLabel);
            root.Children.Add(_typeSelector);
            root.Children.Add(filterLabel);
            root.Children.Add(_filterTextBox);
            root.Children.Add(buttons);
            root.Children.Add(_previewText);
            root.Children.Add(_treeView);

            _window = new Window
            {
                Title = "Lumina Elements List",
                Width = 640,
                Height = 520,
                Content = root,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false
            };
            _window.Closing += (_, args) =>
            {
                args.Cancel = true;
                _window.Hide();
            };

            UpdateButtonsState();
            UpdatePreview();

            var application = new Application
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };

            _ready.Set();
            application.Run();
        }
        catch (Exception exception)
        {
            _startupException = exception;
            throw;
        }
        finally
        {
            _ready.Set();
        }
    }

    private void RefreshCurrentTypeItems()
    {
        if (_typeSelector is null || _treeView is null)
        {
            return;
        }

        _lastSelectedElementRuntimeId = GetSelectedItemRuntimeId();
        string typeKey = ElementTypes[Math.Max(_typeSelector.SelectedIndex, 0)].Key;
        _searchText = string.Empty;
        _allItems = BrowserNavigator.GetElementsListItems(typeKey);
        ApplyFilter();
    }

    private void RestoreFilterText()
    {
        if (_filterTextBox is null)
        {
            return;
        }

        if (!string.Equals(_filterTextBox.Text, s_lastFilterText, StringComparison.Ordinal))
        {
            _filterTextBox.Text = s_lastFilterText;
            _filterTextBox.CaretIndex = _filterTextBox.Text.Length;
        }
    }

    private void SelectPreferredType()
    {
        if (_typeSelector is null)
        {
            return;
        }

        if (s_lastSelectedTypeIndex >= 0 && s_lastSelectedTypeIndex < ElementTypes.Length)
        {
            _typeSelector.SelectedIndex = s_lastSelectedTypeIndex;
            return;
        }

        string preferredType = BrowserNavigator.GetPreferredElementsListType();
        int index = Array.FindIndex(ElementTypes, item => item.Key == preferredType);
        if (index >= 0)
        {
            _typeSelector.SelectedIndex = index;
        }
    }

    private void UpdateButtonsState()
    {
        BrowserNavigator.ElementsListItem? item = GetSelectedItem();
        bool hasItem = item is not null;
        bool canActivate = hasItem && item!.CanActivate;

        if (_moveButton is not null)
        {
            _moveButton.IsEnabled = hasItem;
            _moveButton.IsDefault = hasItem && !canActivate;
            _moveButton.Content = hasItem && !canActivate ? "انتقال افتراضي" : "انتقال";
        }

        if (_activateButton is not null)
        {
            _activateButton.IsEnabled = canActivate;
            _activateButton.IsDefault = canActivate;
            _activateButton.Content = canActivate ? "تفعيل افتراضي" : "تفعيل";
        }
    }

    private void UpdatePreview()
    {
        if (_previewText is null)
        {
            return;
        }

        BrowserNavigator.ElementsListItem? item = GetSelectedItem();
        if (item is null)
        {
            _previewText.Text = "اختر عنصرًا فعليًا من الشجرة لعرض معاينته.";
            return;
        }

        List<string> parts = [item.Summary];
        if (!string.IsNullOrWhiteSpace(item.ParentContext))
        {
            parts.Add($"السياق {item.ParentContext}");
        }

        _previewText.Text = string.Join(". ", parts);
    }

    private void ApplyFilter()
    {
        if (_treeView is null)
        {
            return;
        }

        string filter = (_filterTextBox?.Text ?? string.Empty).Trim();
        _searchText = string.Empty;
        List<BrowserNavigator.ElementsListItem> filteredItems = string.IsNullOrWhiteSpace(filter)
            ? _allItems
            : _allItems
                .Where(item =>
                    item.Label.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    item.Summary.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    item.ParentContext.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();

        _items.Clear();
        foreach (BrowserNavigator.ElementsListItem item in filteredItems)
        {
            _items.Add(item);
        }

        RebuildTree(filteredItems);
        UpdateButtonsState();
        UpdatePreview();
    }

    private void RebuildTree(List<BrowserNavigator.ElementsListItem> items)
    {
        if (_treeView is null)
        {
            return;
        }

        _treeView.Items.Clear();
        Dictionary<string, TreeViewItem> groups = new(StringComparer.Ordinal);
        TreeViewItem? selectedNode = null;

        foreach (BrowserNavigator.ElementsListItem item in items)
        {
            ItemCollection parentCollection = _treeView.Items;
            string pathKey = string.Empty;

            foreach (string segment in item.ContextPath)
            {
                pathKey = string.IsNullOrEmpty(pathKey) ? segment : $"{pathKey}\u001F{segment}";
                if (!groups.TryGetValue(pathKey, out TreeViewItem? groupNode))
                {
                    groupNode = new TreeViewItem
                    {
                        Header = segment,
                        IsExpanded = true,
                        FontWeight = FontWeights.SemiBold
                    };
                    parentCollection.Add(groupNode);
                    groups[pathKey] = groupNode;
                }

                parentCollection = groupNode.Items;
            }

            TreeViewItem leafNode = new()
            {
                Header = item.Label,
                Tag = item
            };
            parentCollection.Add(leafNode);

            if (selectedNode is null &&
                !string.IsNullOrWhiteSpace(_lastSelectedElementRuntimeId) &&
                string.Equals(GetRuntimeId(item), _lastSelectedElementRuntimeId, StringComparison.Ordinal))
            {
                selectedNode = leafNode;
            }
            else if (selectedNode is null && item.IsCurrent)
            {
                selectedNode = leafNode;
            }
        }

        TreeViewItem? fallbackNode = selectedNode ?? FindFirstLeafNode(_treeView.Items);
        if (fallbackNode is not null)
        {
            fallbackNode.IsSelected = true;
            fallbackNode.BringIntoView();
        }
    }

    private bool HandleTreeSearchKey(KeyEventArgs args)
    {
        if (_treeView is null || _items.Count == 0)
        {
            return false;
        }

        if (args.Key is Key.Back or Key.Delete || Keyboard.Modifiers != ModifierKeys.None)
        {
            _searchText = string.Empty;
            return false;
        }

        string keyText = new KeyConverter().ConvertToString(args.Key) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(keyText) || keyText.Length != 1)
        {
            return false;
        }

        DateTime now = DateTime.UtcNow;
        if (now - _lastSearchInputUtc > SearchResetWindow)
        {
            _searchText = string.Empty;
        }

        _lastSearchInputUtc = now;
        string lowerText = keyText.ToLowerInvariant();
        _searchText = _searchText == lowerText ? lowerText : _searchText + lowerText;

        List<TreeViewItem> leaves = GetLeafNodes().ToList();
        if (leaves.Count == 0)
        {
            return false;
        }

        bool skipCurrent = _searchText.Length == 1;
        IEnumerable<TreeViewItem> orderedLeaves = EnumerateSearchCandidates(leaves, skipCurrent);

        TreeViewItem? match = orderedLeaves.FirstOrDefault(node =>
            node.Tag is BrowserNavigator.ElementsListItem item &&
            item.Label.StartsWith(_searchText, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            SystemSounds.Beep.Play();
            return false;
        }

        ExpandAncestors(match);
        match.IsSelected = true;
        match.BringIntoView();
        _lastSelectedElementRuntimeId = GetRuntimeId(match.Tag as BrowserNavigator.ElementsListItem);
        return true;
    }

    private void MoveToSelection()
    {
        BrowserNavigator.ElementsListItem? item = GetSelectedItem();
        if (item is null)
        {
            return;
        }

        if (BrowserNavigator.FocusElement(item.Element))
        {
            _lastSelectedElementRuntimeId = GetRuntimeId(item);
            _window?.Hide();
        }
    }

    private void ActivateSelection()
    {
        BrowserNavigator.ElementsListItem? item = GetSelectedItem();
        if (item is null)
        {
            return;
        }

        if (!BrowserNavigator.FocusElement(item.Element))
        {
            return;
        }

        _lastSelectedElementRuntimeId = GetRuntimeId(item);
        BrowserNavigator.ActivateCurrentElement();
        _window?.Hide();
    }

    private void InvokeDefaultAction()
    {
        BrowserNavigator.ElementsListItem? item = GetSelectedItem();
        if (item is null)
        {
            return;
        }

        if (item.CanActivate)
        {
            ActivateSelection();
            return;
        }

        MoveToSelection();
    }

    private BrowserNavigator.ElementsListItem? GetSelectedItem() =>
        GetSelectedTreeNode()?.Tag as BrowserNavigator.ElementsListItem;

    private string? GetSelectedItemRuntimeId() =>
        GetRuntimeId(GetSelectedItem());

    private TreeViewItem? GetSelectedTreeNode() =>
        FindSelectedNode(_treeView?.Items);

    private static TreeViewItem? FindSelectedNode(ItemCollection? items)
    {
        if (items is null)
        {
            return null;
        }

        foreach (object item in items)
        {
            if (item is not TreeViewItem treeItem)
            {
                continue;
            }

            if (treeItem.IsSelected)
            {
                return treeItem;
            }

            TreeViewItem? child = FindSelectedNode(treeItem.Items);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private static TreeViewItem? FindFirstLeafNode(ItemCollection items)
    {
        foreach (object item in items)
        {
            if (item is not TreeViewItem treeItem)
            {
                continue;
            }

            if (treeItem.Tag is BrowserNavigator.ElementsListItem)
            {
                return treeItem;
            }

            TreeViewItem? child = FindFirstLeafNode(treeItem.Items);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private bool IsSelectedFirstLeafNode()
    {
        TreeViewItem? selected = GetSelectedTreeNode();
        if (selected is null || _treeView is null)
        {
            return false;
        }

        TreeViewItem? firstLeaf = FindFirstLeafNode(_treeView.Items);
        return firstLeaf is not null && ReferenceEquals(firstLeaf, selected);
    }

    private IEnumerable<TreeViewItem> GetLeafNodes()
    {
        if (_treeView is null)
        {
            yield break;
        }

        foreach (TreeViewItem node in EnumerateLeafNodes(_treeView.Items))
        {
            yield return node;
        }
    }

    private IEnumerable<TreeViewItem> EnumerateSearchCandidates(
        IReadOnlyList<TreeViewItem> leaves,
        bool skipCurrent)
    {
        TreeViewItem? currentNode = GetSelectedTreeNode();
        int selectedIndex = leaves
            .Select((node, index) => new { node, index })
            .Where(entry => ReferenceEquals(entry.node, currentNode))
            .Select(entry => entry.index)
            .DefaultIfEmpty(-1)
            .First();

        if (selectedIndex < 0)
        {
            foreach (TreeViewItem leaf in leaves)
            {
                yield return leaf;
            }

            yield break;
        }

        int startIndex = skipCurrent ? selectedIndex + 1 : selectedIndex;
        for (int offset = 0; offset < leaves.Count; offset++)
        {
            int index = (startIndex + offset) % leaves.Count;
            if (!skipCurrent || index != selectedIndex)
            {
                yield return leaves[index];
            }
        }
    }

    private static IEnumerable<TreeViewItem> EnumerateLeafNodes(ItemCollection items)
    {
        foreach (object item in items)
        {
            if (item is not TreeViewItem treeItem)
            {
                continue;
            }

            if (treeItem.Tag is BrowserNavigator.ElementsListItem)
            {
                yield return treeItem;
                continue;
            }

            foreach (TreeViewItem child in EnumerateLeafNodes(treeItem.Items))
            {
                yield return child;
            }
        }
    }

    private static void ExpandAncestors(TreeViewItem node)
    {
        DependencyObject? current = VisualTreeHelper.GetParent(node);
        while (current is not null)
        {
            if (current is TreeViewItem treeItem)
            {
                treeItem.IsExpanded = true;
            }

            current = VisualTreeHelper.GetParent(current);
        }
    }

    private static string? GetRuntimeId(BrowserNavigator.ElementsListItem? item)
    {
        if (item is null)
        {
            return null;
        }

        try
        {
            int[]? runtimeId = item.Element.GetRuntimeId();
            return runtimeId is null ? null : string.Join("-", runtimeId);
        }
        catch
        {
            return null;
        }
    }
}
