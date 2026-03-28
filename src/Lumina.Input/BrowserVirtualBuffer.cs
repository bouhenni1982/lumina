using System.Windows.Automation;

namespace Lumina.Input;

public static class BrowserVirtualBuffer
{
    private static readonly object Sync = new();
    private static BufferSnapshot? _snapshot;
    private static int _currentIndex = -1;
    private static int _textOffset;

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
                Element: element,
                Summary: FocusSnapshotReader.BuildWebSummary(element)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Summary))
            .ToList();

        string pageTitle = FocusSnapshotReader.ResolveWindowTitle(focused);
        string focusedRuntimeId = SafeRuntimeId(focused);
        int focusedIndex = items.FindIndex(item => item.RuntimeId == focusedRuntimeId);

        lock (Sync)
        {
            _snapshot = new BufferSnapshot(pageTitle, items);
            _currentIndex = items.Count == 0
                ? -1
                : focusedIndex >= 0 ? focusedIndex : 0;
            _textOffset = 0;
        }

        if (items.Count == 0)
        {
            return "تم تحديث المخزن الظاهري، لكن لم يتم العثور على عناصر ويب قابلة للقراءة.";
        }

        return $"تم تحديث المخزن الظاهري. الصفحة {pageTitle}. العناصر {items.Count}. الموضع الحالي {Math.Max(_currentIndex + 1, 0)}.";
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

    public static string ReadCurrentLine() =>
        EnsureSnapshotReady() ? ReadCurrent() : "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";

    public static string ReadNextLine()
    {
        if (!EnsureSnapshotReady())
        {
            return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
        }

        lock (Sync)
        {
            if (_snapshot is null || _snapshot.Items.Count == 0)
            {
                return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
            }

            if (_currentIndex >= _snapshot.Items.Count - 1)
            {
                return "لا يوجد سطر تال في الصفحة.";
            }

            _currentIndex++;
            _textOffset = 0;
            return FocusBufferItem(_snapshot.Items[_currentIndex]);
        }
    }

    public static string ReadPreviousLine()
    {
        if (!EnsureSnapshotReady())
        {
            return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
        }

        lock (Sync)
        {
            if (_snapshot is null || _snapshot.Items.Count == 0)
            {
                return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
            }

            if (_currentIndex <= 0)
            {
                return "لا يوجد سطر سابق في الصفحة.";
            }

            _currentIndex--;
            _textOffset = 0;
            return FocusBufferItem(_snapshot.Items[_currentIndex]);
        }
    }

    public static string ReadNextCharacter()
    {
        if (!EnsureSnapshotReady())
        {
            return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
        }

        lock (Sync)
        {
            string text = GetCurrentSummaryText();
            if (string.IsNullOrEmpty(text) || _textOffset >= text.Length)
            {
                return "لا يوجد حرف تال في هذا السطر.";
            }

            char character = text[_textOffset];
            _textOffset++;
            return $"الحرف {DescribeCharacter(character)}";
        }
    }

    public static string ReadPreviousCharacter()
    {
        if (!EnsureSnapshotReady())
        {
            return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
        }

        lock (Sync)
        {
            string text = GetCurrentSummaryText();
            if (string.IsNullOrEmpty(text) || _textOffset <= 0)
            {
                return "لا يوجد حرف سابق في هذا السطر.";
            }

            _textOffset--;
            char character = text[_textOffset];
            return $"الحرف {DescribeCharacter(character)}";
        }
    }

    public static string ReadNextWord()
    {
        if (!EnsureSnapshotReady())
        {
            return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
        }

        lock (Sync)
        {
            string text = GetCurrentSummaryText();
            int start = FindNextWordStart(text, _textOffset);
            if (start < 0)
            {
                return "لا توجد كلمة تالية في هذا السطر.";
            }

            int end = FindWordEnd(text, start);
            _textOffset = end;
            return $"الكلمة {text[start..end]}";
        }
    }

    public static string ReadPreviousWord()
    {
        if (!EnsureSnapshotReady())
        {
            return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
        }

        lock (Sync)
        {
            string text = GetCurrentSummaryText();
            int start = FindPreviousWordStart(text, _textOffset);
            if (start < 0)
            {
                return "لا توجد كلمة سابقة في هذا السطر.";
            }

            int end = FindWordEnd(text, start);
            _textOffset = start;
            return $"الكلمة {text[start..end]}";
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
            _textOffset = 0;
            return FocusBufferItem(_snapshot.Items[_currentIndex]);
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

            _textOffset = 0;
            return FocusBufferItem(_snapshot.Items[_currentIndex]);
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

    public static string SyncToFocusedElement()
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        if (focused is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        lock (Sync)
        {
            if (_snapshot is null || _snapshot.Items.Count == 0)
            {
                return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
            }

            string runtimeId = SafeRuntimeId(focused);
            int index = _snapshot.Items.FindIndex(item => item.RuntimeId == runtimeId);
            if (index < 0)
            {
                return "العنصر الحالي غير موجود داخل المخزن الظاهري.";
            }

            _currentIndex = index;
            _textOffset = 0;
            return $"تمت مزامنة المخزن الظاهري. الموضع الحالي {_currentIndex + 1}. {_snapshot.Items[_currentIndex].Summary}";
        }
    }

    private static bool EnsureSnapshotReady()
    {
        lock (Sync)
        {
            if (_snapshot is not null && _snapshot.Items.Count > 0)
            {
                return true;
            }
        }

        string refreshMessage = Refresh();
        return !refreshMessage.Contains("غير جاهز", StringComparison.Ordinal) &&
               !refreshMessage.Contains("لا توجد صفحة", StringComparison.Ordinal) &&
               !refreshMessage.Contains("ليس ضمن سياق ويب", StringComparison.Ordinal) &&
               !refreshMessage.Contains("لم يتم العثور", StringComparison.Ordinal);
    }

    private static string GetCurrentSummaryText()
    {
        if (_snapshot is null || _snapshot.Items.Count == 0 || _currentIndex < 0)
        {
            return string.Empty;
        }

        return _snapshot.Items[_currentIndex].Summary ?? string.Empty;
    }

    private static int FindNextWordStart(string text, int offset)
    {
        int index = Math.Max(offset, 0);
        while (index < text.Length && !char.IsLetterOrDigit(text[index]))
        {
            index++;
        }

        return index < text.Length ? index : -1;
    }

    private static int FindPreviousWordStart(string text, int offset)
    {
        int index = Math.Min(offset - 1, text.Length - 1);
        while (index >= 0 && !char.IsLetterOrDigit(text[index]))
        {
            index--;
        }

        if (index < 0)
        {
            return -1;
        }

        while (index > 0 && char.IsLetterOrDigit(text[index - 1]))
        {
            index--;
        }

        return index;
    }

    private static int FindWordEnd(string text, int start)
    {
        int index = start;
        while (index < text.Length && char.IsLetterOrDigit(text[index]))
        {
            index++;
        }

        return index;
    }

    private static string DescribeCharacter(char character) =>
        character switch
        {
            ' ' => "مسافة",
            '\t' => "جدولة",
            _ => character.ToString()
        };

    private static string FocusBufferItem(BufferItem item)
    {
        try
        {
            item.Element.SetFocus();
        }
        catch
        {
        }

        return item.Summary;
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
    private sealed record BufferItem(string RuntimeId, AutomationElement Element, string Summary);
}
