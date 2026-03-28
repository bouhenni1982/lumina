using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Lumina.Core.Abstractions;
using Lumina.Core.Models;

namespace Lumina.Output.Inspection;

public sealed class LiveInspectorSink : IInspectorSink
{
    private readonly object _sync = new();
    private readonly ManualResetEventSlim _ready = new(initialState: false);
    private readonly Thread _uiThread;
    private Window? _window;
    private ListBox? _listBox;
    private ObservableCollection<string>? _entries;
    private bool _disposed;

    public LiveInspectorSink()
    {
        _uiThread = new Thread(RunWindow)
        {
            IsBackground = true,
            Name = "LuminaInspectorWindow"
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
        _ready.Wait();
    }

    public bool IsEnabled { get; private set; } = true;

    public void Record(ScreenEvent screenEvent, SpeechRequest speechRequest)
    {
        if (!IsEnabled || _window is null)
        {
            return;
        }

        string line =
            $"{DateTime.Now:HH:mm:ss} | {screenEvent.Node.SourceProcess} | {screenEvent.Node.SourceApi} | " +
            $"{screenEvent.Node.Role} | {screenEvent.Node.Name} | {speechRequest.Text}";

        _window.Dispatcher.BeginInvoke(() =>
        {
            if (_entries is null || _listBox is null)
            {
                return;
            }

            _entries.Insert(0, line);
            while (_entries.Count > 200)
            {
                _entries.RemoveAt(_entries.Count - 1);
            }

            _listBox.SelectedIndex = 0;
            _listBox.ScrollIntoView(_listBox.SelectedItem);
        });
    }

    public void Toggle()
    {
        if (_window is null)
        {
            return;
        }

        IsEnabled = !IsEnabled;
        _window.Dispatcher.BeginInvoke(() =>
        {
            _window.Title = IsEnabled
                ? "Lumina Inspector"
                : "Lumina Inspector (paused)";

            _window.Visibility = IsEnabled
                ? Visibility.Visible
                : Visibility.Hidden;
        });
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
            _window.Dispatcher.Invoke(() =>
            {
                _window.Close();
            });
        }
    }

    private void RunWindow()
    {
        _entries = new ObservableCollection<string>();

        var header = new TextBlock
        {
            Text = "آخر أحداث focus الملتقطة من Lumina",
            Margin = new Thickness(12, 12, 12, 8),
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White
        };

        _listBox = new ListBox
        {
            Margin = new Thickness(12, 0, 12, 12),
            Background = new SolidColorBrush(Color.FromRgb(18, 24, 38)),
            Foreground = Brushes.WhiteSmoke,
            BorderBrush = new SolidColorBrush(Color.FromRgb(62, 79, 112)),
            ItemsSource = _entries
        };

        var root = new DockPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(10, 14, 24))
        };

        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(_listBox);

        _window = new Window
        {
            Title = "Lumina Inspector",
            Width = 980,
            Height = 520,
            Content = root,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        _window.Closed += (_, _) =>
        {
            if (Application.Current is not null)
            {
                Application.Current.Shutdown();
            }
        };

        _ready.Set();

        var application = new Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };

        application.Run(_window);
    }
}
