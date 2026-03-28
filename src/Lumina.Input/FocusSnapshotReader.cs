using System.Windows;
using System.Windows.Automation;

namespace Lumina.Input;

public static class FocusSnapshotReader
{
    internal static AutomationElement? GetFocusedElement() => AutomationElement.FocusedElement;

    public static string ReadCurrentFocusSummary()
    {
        AutomationElement? element = GetFocusedElement();
        if (element is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        string name = ResolveName(element);
        string role = DescribeRole(element);

        string text = $"{role} {name}";
        string? stateSummary = ResolveStateSummary(element);
        if (!string.IsNullOrWhiteSpace(stateSummary))
        {
            text = $"{text}. {stateSummary}";
        }

        string? shortcutKey = ResolveShortcutKey(element);
        if (!string.IsNullOrWhiteSpace(shortcutKey))
        {
            text = $"{text}. اختصار {shortcutKey}";
        }

        return text;
    }

    public static string ReadCurrentElementDetails()
    {
        AutomationElement? element = GetFocusedElement();
        if (element is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        string name = ResolveName(element);
        string role = DescribeRole(element);
        string process = ResolveProcessName(element);
        string value = TryReadValue(element);
        string windowTitle = ResolveWindowTitle(element);
        string localizedRole = element.Current.LocalizedControlType ?? string.Empty;
        string helpText = element.Current.HelpText ?? string.Empty;
        string automationId = element.Current.AutomationId ?? string.Empty;
        string? shortcutKey = ResolveShortcutKey(element);
        string? stateSummary = ResolveStateSummary(element);

        List<string> segments =
        [
            $"العنصر الحالي {name}",
            $"النوع {role}"
        ];

        if (!string.IsNullOrWhiteSpace(localizedRole) &&
            !string.Equals(localizedRole, role, StringComparison.OrdinalIgnoreCase))
        {
            segments.Add($"الوصف المحلي {localizedRole}");
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            segments.Add($"القيمة {value}");
        }

        if (!string.IsNullOrWhiteSpace(stateSummary))
        {
            segments.Add($"الحالة {stateSummary}");
        }

        if (!string.IsNullOrWhiteSpace(shortcutKey))
        {
            segments.Add($"الاختصار {shortcutKey}");
        }

        if (IsBrowserContext(element))
        {
            segments.Add($"دور الويب {ResolveWebSemanticRole(element)}");
        }

        if (IsSettingsLikeContext(element))
        {
            segments.Add(BuildSettingsContextSummary(element));
        }

        if (!string.IsNullOrWhiteSpace(windowTitle))
        {
            segments.Add($"النافذة {windowTitle}");
        }

        segments.Add($"العملية {process}");

        if (!string.IsNullOrWhiteSpace(automationId))
        {
            segments.Add($"معرف الأتمتة {automationId}");
        }

        if (!string.IsNullOrWhiteSpace(helpText))
        {
            segments.Add($"تلميح {helpText}");
        }

        return string.Join(". ", segments);
    }

    public static string ReadCurrentElementAdvancedDetails()
    {
        AutomationElement? element = GetFocusedElement();
        if (element is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        string className = element.Current.ClassName ?? string.Empty;
        string frameworkId = element.Current.FrameworkId ?? string.Empty;
        string itemStatus = element.Current.ItemStatus ?? string.Empty;
        string itemType = element.Current.ItemType ?? string.Empty;
        string patterns = ResolveSupportedPatterns(element);
        Rect bounds = element.Current.BoundingRectangle;
        string? shortcutKey = ResolveShortcutKey(element);

        List<string> segments =
        [
            ReadCurrentElementDetails(),
            $"الإطار {NormalizeValue(frameworkId, "غير معروف")}",
            $"الصنف {NormalizeValue(className, "غير معروف")}",
            $"ممكّن {(element.Current.IsEnabled ? "نعم" : "لا")}",
            $"خارج الشاشة {(element.Current.IsOffscreen ? "نعم" : "لا")}",
            $"يمتلك التركيز {(element.Current.HasKeyboardFocus ? "نعم" : "لا")}"
        ];

        if (!string.IsNullOrWhiteSpace(itemType))
        {
            segments.Add($"نوع العنصر {itemType}");
        }

        if (!string.IsNullOrWhiteSpace(itemStatus))
        {
            segments.Add($"حالة العنصر {itemStatus}");
        }

        AddIfPresent(segments, shortcutKey, "اختصار");

        if (!bounds.IsEmpty)
        {
            segments.Add($"الموضع {Math.Round(bounds.Left)}, {Math.Round(bounds.Top)}, {Math.Round(bounds.Width)}, {Math.Round(bounds.Height)}");
        }

        if (!string.IsNullOrWhiteSpace(patterns))
        {
            segments.Add($"الأنماط المدعومة {patterns}");
        }

        return string.Join(". ", segments);
    }

    public static string ReadCurrentPageTitle()
    {
        AutomationElement? element = GetFocusedElement();
        if (element is null)
        {
            return "لا توجد صفحة نشطة حاليا.";
        }

        if (!IsBrowserContext(element))
        {
            return "العنصر الحالي ليس داخل متصفح مدعوم.";
        }

        AutomationElement? window = FindAncestor(
            element,
            current => current.Current.ControlType == ControlType.Window);

        string title = window is null ? ResolveName(element) : ResolveName(window);
        return $"عنوان الصفحة أو النافذة {title}";
    }

    public static string ReadCurrentWindowSummary()
    {
        AutomationElement? element = GetFocusedElement();
        if (element is null)
        {
            return "لا توجد نافذة نشطة حاليا.";
        }

        AutomationElement? window = FindAncestor(
            element,
            current => current.Current.ControlType == ControlType.Window);

        if (window is null)
        {
            return "تعذر تحديد النافذة الحالية.";
        }

        string windowName = ResolveName(window);
        string process = ResolveProcessName(window);
        string className = NormalizeValue(window.Current.ClassName ?? string.Empty, "غير معروف");
        string framework = NormalizeValue(window.Current.FrameworkId ?? string.Empty, "غير معروف");
        string focusedRole = DescribeRole(element);
        string focusedName = ResolveName(element);

        List<string> segments =
        [
            $"النافذة الحالية {windowName}",
            $"العملية {process}",
            $"الإطار {framework}",
            $"الصنف {className}",
            $"العنصر النشط {focusedRole} {focusedName}"
        ];

        if (IsSettingsLikeContext(element))
        {
            segments.Add(BuildSettingsContextSummary(element));
        }

        return string.Join(". ", segments);
    }

    public static string ReadCurrentStatusSummary()
    {
        AutomationElement? element = GetFocusedElement();
        if (element is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        string name = ResolveName(element);
        string role = DescribeRole(element);
        string value = TryReadValue(element);
        string helpText = element.Current.HelpText ?? string.Empty;
        string itemStatus = element.Current.ItemStatus ?? string.Empty;
        string? stateSummary = ResolveStateSummary(element);
        string? shortcutKey = ResolveShortcutKey(element);

        List<string> segments =
        [
            $"حالة العنصر {role} {name}",
            $"ممكّن {(element.Current.IsEnabled ? "نعم" : "لا")}",
            $"قابل للتركيز {(element.Current.IsKeyboardFocusable ? "نعم" : "لا")}",
            $"خارج الشاشة {(element.Current.IsOffscreen ? "نعم" : "لا")}",
            $"يمتلك التركيز {(element.Current.HasKeyboardFocus ? "نعم" : "لا")}"
        ];

        if (!string.IsNullOrWhiteSpace(value))
        {
            segments.Add($"القيمة {value}");
        }

        if (!string.IsNullOrWhiteSpace(itemStatus))
        {
            segments.Add($"حالة العنصر {itemStatus}");
        }

        if (!string.IsNullOrWhiteSpace(stateSummary))
        {
            segments.Add($"الخلاصة {stateSummary}");
        }

        if (!string.IsNullOrWhiteSpace(shortcutKey))
        {
            segments.Add($"الاختصار {shortcutKey}");
        }

        if (!string.IsNullOrWhiteSpace(helpText))
        {
            segments.Add($"تلميح {helpText}");
        }

        if (IsSettingsLikeContext(element))
        {
            segments.Add(BuildSettingsContextSummary(element));
        }

        return string.Join(". ", segments);
    }

    public static string ReadCurrentWebSummary()
    {
        AutomationElement? element = GetFocusedElement();
        if (element is null)
        {
            return "لا يوجد عنصر ويب نشط حاليا.";
        }

        if (!IsBrowserContext(element))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        return BuildWebSummary(element);
    }

    internal static string BuildWebSummary(AutomationElement element)
    {
        string semanticRole = ResolveWebSemanticRole(element);
        string name = ResolveName(element);
        string pageTitle = ResolveWindowTitle(element);
        string value = TryReadValue(element);

        string text = semanticRole switch
        {
            "web_link" => $"رابط ويب {name}",
            "web_heading" => $"عنوان صفحة {name}",
            "web_edit" => $"حقل إدخال ويب {name}",
            "web_document" => $"مستند ويب {name}",
            "web_button" => $"زر ويب {name}",
            "web_checkbox" => $"خانة اختيار {name}",
            _ => $"عنصر ويب {name}"
        };

        if (!string.IsNullOrWhiteSpace(value))
        {
            text = $"{text}. القيمة {value}";
        }

        if (!string.IsNullOrWhiteSpace(pageTitle))
        {
            text = $"{text}. الصفحة {pageTitle}";
        }

        return text;
    }

    internal static bool IsBrowserContext(AutomationElement element)
    {
        string processName = ResolveProcessName(element);
        if (processName is "chrome" or "msedge" or "firefox" or "electron" or "code" or "teams")
        {
            return true;
        }

        string className = element.Current.ClassName ?? string.Empty;
        return className.Contains("Chrome", StringComparison.OrdinalIgnoreCase) ||
               className.Contains("Mozilla", StringComparison.OrdinalIgnoreCase);
    }

    internal static string ResolveWebSemanticRole(AutomationElement element)
    {
        string role = ResolveRole(element);
        string localizedRole = (element.Current.LocalizedControlType ?? string.Empty).ToLowerInvariant();
        string itemType = (element.Current.ItemType ?? string.Empty).ToLowerInvariant();
        string helpText = (element.Current.HelpText ?? string.Empty).ToLowerInvariant();

        if (role == "hyperlink" || localizedRole.Contains("link"))
        {
            return "web_link";
        }

        if (localizedRole.Contains("heading") || itemType.Contains("heading") || helpText.Contains("heading"))
        {
            return "web_heading";
        }

        if (role == "document" || localizedRole.Contains("document"))
        {
            return "web_document";
        }

        if (role == "edit" || localizedRole.Contains("edit") || localizedRole.Contains("text field"))
        {
            return "web_edit";
        }

        if (role == "button" || localizedRole.Contains("button"))
        {
            return "web_button";
        }

        if (localizedRole.Contains("check box") || role.Contains("check", StringComparison.OrdinalIgnoreCase))
        {
            return "web_checkbox";
        }

        return "web_control";
    }

    internal static string ResolveWindowTitle(AutomationElement element)
    {
        AutomationElement? window = FindAncestor(
            element,
            current => current.Current.ControlType == ControlType.Window);

        return window is null ? string.Empty : ResolveName(window);
    }

    internal static string TryReadValue(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object? pattern))
        {
            return ((ValuePattern)pattern).Current.Value ?? string.Empty;
        }

        return string.Empty;
    }

    internal static string ResolveName(AutomationElement element) =>
        string.IsNullOrWhiteSpace(element.Current.Name) ? "عنصر غير مسمى" : element.Current.Name;

    internal static string ResolveRole(AutomationElement element) =>
        element.Current.ControlType?.ProgrammaticName?.Replace("ControlType.", "").ToLowerInvariant()
        ?? "control";

    internal static string DescribeRole(AutomationElement element)
    {
        string role = ResolveRole(element);
        return role switch
        {
            "menu" => "قائمة",
            "menuitem" => "عنصر قائمة",
            "button" => "زر",
            "edit" => "حقل تحرير",
            "checkbox" => "خانة اختيار",
            "radiobutton" => "زر اختيار",
            "combobox" => "مربع خيارات",
            "tabitem" => "علامة تبويب",
            "tab" => "تبويب",
            "listitem" => "عنصر قائمة",
            "list" => "قائمة",
            "treeitem" => "عنصر شجرة",
            "tree" => "شجرة",
            "group" => "مجموعة",
            "pane" => "جزء",
            "document" => "مستند",
            "text" => "نص",
            "hyperlink" => "رابط",
            "slider" => "منزلق",
            "progressbar" => "شريط تقدم",
            _ => role
        };
    }

    internal static string ResolveProcessName(AutomationElement element)
    {
        try
        {
            return System.Diagnostics.Process.GetProcessById(element.Current.ProcessId).ProcessName.ToLowerInvariant();
        }
        catch
        {
            return "unknown";
        }
    }

    internal static string ResolveSupportedPatterns(AutomationElement element)
    {
        try
        {
            AutomationPattern[] patterns = element.GetSupportedPatterns();
            if (patterns.Length == 0)
            {
                return string.Empty;
            }

            List<string> names = [];
            foreach (AutomationPattern pattern in patterns)
            {
                string name = pattern.ProgrammaticName ?? string.Empty;
                if (name.StartsWith("AutomationPattern.", StringComparison.OrdinalIgnoreCase))
                {
                    name = name["AutomationPattern.".Length..];
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }

            return string.Join(", ", names);
        }
        catch
        {
            return string.Empty;
        }
    }

    internal static string NormalizeValue(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    internal static string? ResolveShortcutKey(AutomationElement element)
    {
        string acceleratorKey = NormalizeOptionalValue(element.Current.AcceleratorKey);
        string accessKey = NormalizeOptionalValue(element.Current.AccessKey);

        if (string.IsNullOrWhiteSpace(acceleratorKey))
        {
            return accessKey;
        }

        if (string.IsNullOrWhiteSpace(accessKey) ||
            string.Equals(acceleratorKey, accessKey, StringComparison.OrdinalIgnoreCase))
        {
            return acceleratorKey;
        }

        return $"{acceleratorKey} / {accessKey}";
    }

    internal static string? ResolveStateSummary(AutomationElement element)
    {
        List<string> states = [];

        if (element.TryGetCurrentPattern(TogglePattern.Pattern, out object? togglePatternObject))
        {
            string? toggleState = ((TogglePattern)togglePatternObject).Current.ToggleState switch
            {
                ToggleState.On => "محدد",
                ToggleState.Off => "غير محدد",
                ToggleState.Indeterminate => "حالة غير محسومة",
                _ => null
            };

            AddDistinctIfPresent(states, toggleState);
        }

        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object? selectionItemPatternObject))
        {
            string selectionState = ((SelectionItemPattern)selectionItemPatternObject).Current.IsSelected
                ? "محدد"
                : "غير محدد";
            AddDistinctIfPresent(states, selectionState);
        }

        if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out object? expandCollapsePatternObject))
        {
            string? expandState = ((ExpandCollapsePattern)expandCollapsePatternObject).Current.ExpandCollapseState switch
            {
                ExpandCollapseState.Expanded => "موسع",
                ExpandCollapseState.Collapsed => "مطوي",
                ExpandCollapseState.PartiallyExpanded => "موسع جزئيا",
                ExpandCollapseState.LeafNode => "بدون عناصر فرعية",
                _ => null
            };

            AddDistinctIfPresent(states, expandState);
        }

        AddDistinctIfPresent(states, NormalizeOptionalValue(element.Current.ItemStatus));

        if (!element.Current.IsEnabled)
        {
            AddDistinctIfPresent(states, "معطل");
        }

        return states.Count == 0 ? null : string.Join("، ", states);
    }

    internal static bool IsSettingsLikeContext(AutomationElement element)
    {
        string process = ResolveProcessName(element);
        string windowTitle = ResolveWindowTitle(element);
        string className = element.Current.ClassName ?? string.Empty;
        string itemType = element.Current.ItemType ?? string.Empty;
        string automationId = element.Current.AutomationId ?? string.Empty;
        bool titleOrMetadataSuggestsSettings =
            windowTitle.Contains("settings", StringComparison.OrdinalIgnoreCase) ||
            windowTitle.Contains("الإعدادات", StringComparison.OrdinalIgnoreCase) ||
            className.Contains("settings", StringComparison.OrdinalIgnoreCase) ||
            itemType.Contains("settings", StringComparison.OrdinalIgnoreCase) ||
            automationId.Contains("settings", StringComparison.OrdinalIgnoreCase);

        return process == "systemsettings" ||
               (process == "applicationframehost" && titleOrMetadataSuggestsSettings) ||
               titleOrMetadataSuggestsSettings;
    }

    internal static string BuildSettingsContextSummary(AutomationElement element)
    {
        AutomationElement? group = FindAncestor(
            element,
            current => current.Current.ControlType == ControlType.Group ||
                       current.Current.ControlType == ControlType.TabItem ||
                       current.Current.ControlType == ControlType.Pane);

        List<string> segments = [];

        if (group is not null)
        {
            string groupName = ResolveName(group);
            string groupRole = DescribeRole(group);
            if (!string.IsNullOrWhiteSpace(groupName) && groupName != "عنصر غير مسمى")
            {
                segments.Add($"{groupRole} الحالية {groupName}");
            }
        }

        AutomationElement? tabItem = FindAncestor(
            element,
            current => current.Current.ControlType == ControlType.TabItem);

        if (tabItem is not null)
        {
            segments.Add($"التبويب {ResolveName(tabItem)}");
        }

        AutomationElement scope = group ?? element;
        int buttons = CountDescendants(scope, ControlType.Button);
        int checkboxes = CountDescendants(scope, ControlType.CheckBox);
        int radioButtons = CountDescendants(scope, ControlType.RadioButton);
        int comboBoxes = CountDescendants(scope, ControlType.ComboBox);
        int edits = CountDescendants(scope, ControlType.Edit);
        int listItems = CountDescendants(scope, ControlType.ListItem);

        List<string> counts = [];
        AddCountIfAny(counts, buttons, "أزرار");
        AddCountIfAny(counts, checkboxes, "خانات اختيار");
        AddCountIfAny(counts, radioButtons, "أزرار اختيار");
        AddCountIfAny(counts, comboBoxes, "مربعات خيارات");
        AddCountIfAny(counts, edits, "حقول تحرير");
        AddCountIfAny(counts, listItems, "عناصر قائمة");

        if (counts.Count > 0)
        {
            segments.Add($"محتوى القسم {string.Join("، ", counts)}");
        }

        return segments.Count == 0
            ? "ضمن سياق إعدادات"
            : string.Join(". ", segments);
    }

    private static int CountDescendants(AutomationElement root, ControlType controlType)
    {
        try
        {
            return root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, controlType)).Count;
        }
        catch
        {
            return 0;
        }
    }

    private static string NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static void AddIfPresent(List<string> segments, string? value, string prefix)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            segments.Add($"{prefix} {value}");
        }
    }

    private static void AddDistinctIfPresent(List<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value, StringComparer.Ordinal))
        {
            values.Add(value);
        }
    }

    private static void AddCountIfAny(List<string> counts, int count, string label)
    {
        if (count > 0)
        {
            counts.Add($"{label} {count}");
        }
    }

    internal static AutomationElement? FindAncestor(
        AutomationElement element,
        Func<AutomationElement, bool> predicate)
    {
        AutomationElement? current = element;
        while (current is not null)
        {
            if (predicate(current))
            {
                return current;
            }

            current = TreeWalker.ControlViewWalker.GetParent(current);
        }

        return null;
    }
}
