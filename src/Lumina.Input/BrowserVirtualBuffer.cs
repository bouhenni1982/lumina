using System.Windows.Automation;
using System.Windows.Automation.Text;

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
        List<BufferItem> rawItems = BrowserNavigator
            .EnumerateBufferCandidates(root)
            .Select(element => new BufferItem(
                RuntimeId: SafeRuntimeId(element),
                Element: element,
                Summary: FocusSnapshotReader.BuildWebSummary(element),
                ReadingLines: BuildReadingLines(element)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Summary) && item.ReadingLines.Count > 0)
            .ToList();

        List<BufferLine> items = ExpandToBufferLines(rawItems);

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

        int elementCount = rawItems.Count;
        return $"تم تحديث المخزن الظاهري. الصفحة {pageTitle}. العناصر {elementCount}. أسطر القراءة {items.Count}. الموضع الحالي {Math.Max(_currentIndex + 1, 0)}.";
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

    public static bool IsCurrentEditField()
    {
        lock (Sync)
        {
            if (_snapshot is null || _snapshot.Items.Count == 0 || _currentIndex < 0)
            {
                return false;
            }

            return FocusSnapshotReader.ResolveWebSemanticRole(_snapshot.Items[_currentIndex].Element) == "web_edit";
        }
    }

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

            int elementCount = _snapshot.Items
                .Select(item => item.RuntimeId)
                .Distinct(StringComparer.Ordinal)
                .Count();

            return $"المخزن الظاهري للصفحة {_snapshot.PageTitle}. العناصر {elementCount}. أسطر القراءة {_snapshot.Items.Count}. الموضع الحالي {Math.Max(_currentIndex + 1, 0)}.";
        }
    }

    public static string SyncToFocusedElement()
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        if (focused is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        return SyncToElement(focused);
    }

    internal static string SyncToElement(AutomationElement element)
    {
        if (element is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        lock (Sync)
        {
            if (_snapshot is null || _snapshot.Items.Count == 0)
            {
                return "المخزن الظاهري غير جاهز. استخدم أمر تحديث المخزن أولا.";
            }

            string runtimeId = SafeRuntimeId(element);
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

    private static List<string> BuildReadingLines(AutomationElement element)
    {
        List<string> lines = [];

        bool addedRichText = AddReadableTextFromElement(lines, element);
        if (!addedRichText)
        {
            AddReadingSegment(lines, FocusSnapshotReader.BuildWebSummary(element));
        }

        string value = FocusSnapshotReader.TryReadValue(element);
        if (!string.IsNullOrWhiteSpace(value))
        {
            AddReadingSegment(lines, $"القيمة {value}");
        }

        string? state = FocusSnapshotReader.ResolveStateSummary(element);
        if (!string.IsNullOrWhiteSpace(state))
        {
            AddReadingSegment(lines, $"الحالة {state}");
        }

        string? shortcut = FocusSnapshotReader.ResolveShortcutKey(element);
        if (!string.IsNullOrWhiteSpace(shortcut))
        {
            AddReadingSegment(lines, $"الاختصار {shortcut}");
        }

        string helpText = element.Current.HelpText ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(helpText))
        {
            AddReadingSegment(lines, helpText);
        }

        return lines;
    }

    private static bool AddReadableTextFromElement(List<string> lines, AutomationElement element)
    {
        bool addedAny = false;

        if (TryReadTextPatternText(element, out string? textPatternText))
        {
            foreach (string line in SplitIntoReadableSegments(textPatternText))
            {
                if (IsUsefulReadingLine(line) && !lines.Contains(line, StringComparer.Ordinal))
                {
                    lines.Add(line);
                    addedAny = true;
                }
            }
        }

        string name = FocusSnapshotReader.ResolveName(element);
        if (IsUsefulReadingLine(name) && !lines.Contains(name, StringComparer.Ordinal))
        {
            lines.Insert(0, name);
            addedAny = true;
        }

        return addedAny;
    }

    private static List<BufferLine> ExpandToBufferLines(List<BufferItem> rawItems)
    {
        List<BufferLine> lines = [];

        foreach (BufferItem item in rawItems)
        {
            for (int i = 0; i < item.ReadingLines.Count; i++)
            {
                string lineText = item.ReadingLines[i];
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    continue;
                }

                lines.Add(new BufferLine(
                    RuntimeId: item.RuntimeId,
                    Element: item.Element,
                    Summary: lineText,
                    ParentSummary: item.Summary,
                    LineIndexWithinElement: i));
            }
        }

        return lines;
    }

    private static void AddReadingSegment(List<string> segments, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        foreach (string segment in SplitIntoReadableSegments(text))
        {
            if (!segments.Contains(segment, StringComparer.Ordinal))
            {
                segments.Add(segment);
            }
        }
    }

    private static bool TryReadTextPatternText(AutomationElement element, out string? text)
    {
        text = null;

        try
        {
            if (!element.TryGetCurrentPattern(TextPattern.Pattern, out object? patternObject))
            {
                return false;
            }

            TextPattern pattern = (TextPattern)patternObject;
            string patternText = NormalizeReadingText(pattern.DocumentRange.GetText(-1));
            if (string.IsNullOrWhiteSpace(patternText))
            {
                return false;
            }

            text = patternText;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> SplitIntoReadableSegments(string text)
    {
        text = NormalizeReadingText(text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        string[] separators = [". ", "، ", "\r\n", "\n"];
        List<string> working = [text.Trim()];

        foreach (string separator in separators)
        {
            List<string> next = [];
            foreach (string part in working)
            {
                foreach (string splitPart in part.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!string.IsNullOrWhiteSpace(splitPart))
                    {
                        next.Add(splitPart.Trim().TrimEnd('.', '،'));
                    }
                }
            }

            working = next;
        }

        return working.Count == 0 ? [text.Trim()] : working;
    }

    private static string NormalizeReadingText(string? text) =>
        (text ?? string.Empty)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal)
            .Replace('\u00A0', ' ')
            .Trim();

    private static bool IsUsefulReadingLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        string value = line.Trim();
        if (value.Length <= 1)
        {
            return false;
        }

        return value.Any(char.IsLetterOrDigit);
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

    private static string FocusBufferItem(BufferLine item)
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

    private sealed record BufferSnapshot(string PageTitle, List<BufferLine> Items);
    private sealed record BufferItem(string RuntimeId, AutomationElement Element, string Summary, List<string> ReadingLines);
    private sealed record BufferLine(
        string RuntimeId,
        AutomationElement Element,
        string Summary,
        string ParentSummary,
        int LineIndexWithinElement);
}
