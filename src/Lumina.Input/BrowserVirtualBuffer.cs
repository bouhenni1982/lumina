using System.Windows.Automation;

namespace Lumina.Input;

public static class BrowserVirtualBuffer
{
    private static readonly object Sync = new();
    private static BufferSnapshot? _snapshot;
    private static int _currentIndex = -1;

    public static string Refresh()
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        if (focused is null)
        {
            return "لا توجد صفحة نشطة حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(focused))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement root = BrowserNavigator.ResolveNavigationRootForBuffer(focused);
        List<BufferItem> items = BrowserNavigator
            .EnumerateBufferCandidates(root)
            .Select(element => new BufferItem(
                RuntimeId: SafeRuntimeId(element),
                Summary: FocusSnapshotReader.BuildWebSummary(element)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Summary))
            .ToList();

        string pageTitle = FocusSnapshotReader.ResolveWindowTitle(focused);

        lock (Sync)
        {
            _snapshot = new BufferSnapshot(pageTitle, items);
            _currentIndex = items.Count == 0 ? -1 : 0;
        }

        if (items.Count == 0)
        {
            return "تم تحديث المخزن الظاهري، لكن لم يتم العثور على عناصر ويب قابلة للقراءة.";
        }

        return $"تم تحديث المخزن الظاهري. الصفحة {pageTitle}. العناصر {items.Count}.";
    }

    public static string ReadCurrent()
    {
        lock (Sync)
        {
            if (_snapshot is null || _snapshot.Items.Count == 0 || _currentIndex < 0)
            {
                return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
            }

            return _snapshot.Items[_currentIndex].Summary;
        }
    }

    public static string MoveNext()
    {
        lock (Sync)
        {
            if (_snapshot is null || _snapshot.Items.Count == 0)
            {
                return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
            }

            _currentIndex = (_currentIndex + 1) % _snapshot.Items.Count;
            return _snapshot.Items[_currentIndex].Summary;
        }
    }

    public static string MovePrevious()
    {
        lock (Sync)
        {
            if (_snapshot is null || _snapshot.Items.Count == 0)
            {
                return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
            }

            _currentIndex--;
            if (_currentIndex < 0)
            {
                _currentIndex = _snapshot.Items.Count - 1;
            }

            return _snapshot.Items[_currentIndex].Summary;
        }
    }

    public static string SummarizeBuffer()
    {
        lock (Sync)
        {
            if (_snapshot is null)
            {
                return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
            }

            return $"المخزن الظاهري للصفحة {_snapshot.PageTitle}. العناصر {_snapshot.Items.Count}. الموضع الحالي {Math.Max(_currentIndex + 1, 0)}.";
        }
    }

    private static string SafeRuntimeId(AutomationElement element)
    {
        try
        {
            int[]? runtimeId = element.GetRuntimeId();
            return runtimeId is null ? string.Empty : string.Join("-", runtimeId);
        }
        catch
        {
            return string.Empty;
        }
    }

    private sealed record BufferSnapshot(string PageTitle, List<BufferItem> Items);
    private sealed record BufferItem(string RuntimeId, string Summary);
}
