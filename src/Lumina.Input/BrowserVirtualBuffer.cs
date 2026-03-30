using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Globalization;
using Lumina.Core.Models;

namespace Lumina.Input;

public static class BrowserVirtualBuffer
{
    private static readonly object Sync = new();
    private static readonly Timer DeferredRefreshTimer = new(OnDeferredRefreshTimerTick);
    private static BufferSnapshot? _snapshot;
    private static int _currentIndex = -1;
    private static int _textOffset;
    private static PendingRefreshReason _pendingRefreshReason;

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
            .Select(element =>
            {
                List<string> readingLines = BuildReadingLines(element);
                return new BufferItem(
                    RuntimeId: SafeRuntimeId(element),
                    Element: element,
                    Summary: ResolveBufferSummary(element, readingLines),
                    ReadingLines: readingLines);
            })
            .Where(item => item.ReadingLines.Count > 0)
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

    public static void NotifyAccessibilityEvent(ScreenEvent screenEvent)
    {
        if (screenEvent.Node.ContextKind != "browser")
        {
            return;
        }

        PendingRefreshReason reason = screenEvent.EventType switch
        {
            "focusChanged" => PendingRefreshReason.SyncToFocus,
            "liveRegionChanged" => PendingRefreshReason.Refresh,
            "liveTextChanged" => PendingRefreshReason.Refresh,
            _ => PendingRefreshReason.None
        };

        if (reason == PendingRefreshReason.None)
        {
            return;
        }

        lock (Sync)
        {
            if (reason == PendingRefreshReason.Refresh || _pendingRefreshReason == PendingRefreshReason.None)
            {
                _pendingRefreshReason = reason;
            }
        }

        int delayMs = reason == PendingRefreshReason.Refresh ? 160 : 70;
        DeferredRefreshTimer.Change(delayMs, Timeout.Infinite);
    }

    public static bool IsSyncedToFocusedElement()
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        if (focused is null)
        {
            return false;
        }

        lock (Sync)
        {
            if (_snapshot is null || _snapshot.Items.Count == 0 || _currentIndex < 0 || _currentIndex >= _snapshot.Items.Count)
            {
                return false;
            }

            string runtimeId = SafeRuntimeId(focused);
            if (string.IsNullOrWhiteSpace(runtimeId))
            {
                return false;
            }

            return string.Equals(_snapshot.Items[_currentIndex].RuntimeId, runtimeId, StringComparison.Ordinal);
        }
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

    private static void OnDeferredRefreshTimerTick(object? state)
    {
        PendingRefreshReason reason;
        lock (Sync)
        {
            reason = _pendingRefreshReason;
            _pendingRefreshReason = PendingRefreshReason.None;
        }

        if (reason == PendingRefreshReason.None)
        {
            return;
        }

        try
        {
            if (reason == PendingRefreshReason.SyncToFocus)
            {
                string syncText = SyncToFocusedElement();
                if (syncText.Contains("غير موجود داخل المخزن الظاهري", StringComparison.Ordinal) ||
                    syncText.Contains("غير جاهز", StringComparison.Ordinal))
                {
                    Refresh();
                    SyncToFocusedElement();
                }

                return;
            }

            bool hasSnapshot;
            lock (Sync)
            {
                hasSnapshot = _snapshot is not null && _snapshot.Items.Count > 0;
            }

            if (!hasSnapshot)
            {
                return;
            }

            string refreshText = Refresh();
            if (refreshText.Contains("تم تحديث المخزن الظاهري", StringComparison.Ordinal))
            {
                SyncToFocusedElement();
            }
        }
        catch
        {
        }
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
        if (FocusSnapshotReader.IsEditableBrowserDocument(element))
        {
            AddReadingSegment(lines, BuildStructuralSummary(element));

            string editorValue = FocusSnapshotReader.TryReadValue(element);
            if (!string.IsNullOrWhiteSpace(editorValue))
            {
                string normalizedValue = NormalizeReadingText(editorValue);
                if (!normalizedValue.Contains('\n', StringComparison.Ordinal) &&
                    normalizedValue.Length <= 120)
                {
                    AddReadingSegment(lines, $"القيمة {normalizedValue}");
                }
            }

            string? editorState = FocusSnapshotReader.ResolveStateSummary(element);
            if (!string.IsNullOrWhiteSpace(editorState))
            {
                AddReadingSegment(lines, $"الحالة {editorState}");
            }

            return lines;
        }

        bool addedRichText = AddReadableTextFromElement(lines, element);
        if (!addedRichText)
        {
            AddReadingSegment(lines, FocusSnapshotReader.BuildWebSummary(element));
        }
        else
        {
            string structuralSummary = BuildStructuralSummary(element);
            if (ShouldIncludeStructuralSummary(lines, structuralSummary))
            {
                lines.Insert(0, structuralSummary);
            }
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

    private static string ResolveBufferSummary(AutomationElement element, IReadOnlyList<string> readingLines)
    {
        string structuralSummary = BuildStructuralSummary(element);
        if (IsUsableSummary(structuralSummary))
        {
            return structuralSummary;
        }

        string summary = FocusSnapshotReader.BuildWebSummary(element);
        if (IsUsableSummary(summary))
        {
            return summary;
        }

        string name = FocusSnapshotReader.ResolveName(element);
        if (IsUsableSummary(name))
        {
            return name;
        }

        string value = FocusSnapshotReader.TryReadValue(element);
        if (IsUsableSummary(value))
        {
            return value;
        }

        return readingLines.FirstOrDefault(IsUsableSummary) ?? string.Empty;
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

    private static string BuildStructuralSummary(AutomationElement element)
    {
        string semanticRole = FocusSnapshotReader.ResolveWebSemanticRole(element);
        string name = FocusSnapshotReader.ResolveName(element);
        string? state = FocusSnapshotReader.ResolveStateSummary(element);

        string summary = semanticRole switch
        {
            "web_link" => BuildRoleSummary("رابط ويب", name),
            "web_heading" => BuildRoleSummary("عنوان صفحة", name),
            "web_edit" => BuildRoleSummary("حقل إدخال ويب", name),
            "web_button" => BuildRoleSummary("زر ويب", name),
            "web_togglebutton" => BuildRoleSummary("زر تبديل ويب", name),
            "web_checkbox" => BuildRoleSummary("خانة اختيار", name),
            "web_radio" => BuildRoleSummary("زر اختيار ويب", name),
            "web_combobox" => BuildRoleSummary("مربع خيارات ويب", name),
            "web_graphic" => BuildRoleSummary("رسم ويب", name),
            "web_frame" => BuildRoleSummary("إطار ويب", name),
            "web_tab" => BuildRoleSummary("علامة تبويب ويب", name),
            "web_menuitem" => BuildRoleSummary("عنصر قائمة ويب", name),
            "web_table" => BuildRoleSummary("جدول", name),
            "web_list" => BuildRoleSummary("قائمة ويب", name),
            "web_listitem" => BuildRoleSummary("عنصر قائمة ويب", name),
            "web_dialog" => BuildRoleSummary("حوار ويب", name),
            "web_landmark" => BuildRoleSummary("معلم صفحة", name),
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(summary))
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(state) ? summary : $"{summary}. {state}";
    }

    private static string BuildRoleSummary(string label, string name) =>
        string.IsNullOrWhiteSpace(name) || name == "عنصر غير مسمى"
            ? label
            : $"{label} {name}";

    private static bool ShouldIncludeStructuralSummary(IReadOnlyList<string> lines, string structuralSummary)
    {
        if (!IsUsableSummary(structuralSummary))
        {
            return false;
        }

        foreach (string line in lines)
        {
            if (!IsUsableSummary(line))
            {
                continue;
            }

            if (AreSimilarForSpeech(structuralSummary, line) ||
                AreSimilarForSpeech(line, structuralSummary))
            {
                return false;
            }
        }

        return true;
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

    private static bool IsUsableSummary(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string value = text.Trim();
        if (value == "عنصر غير مسمى")
        {
            return false;
        }

        return value.Any(char.IsLetterOrDigit);
    }

    private static bool AreSimilarForSpeech(string left, string right)
    {
        string normalizedLeft = NormalizeForSpeechComparison(left);
        string normalizedRight = NormalizeForSpeechComparison(right);
        if (string.IsNullOrWhiteSpace(normalizedLeft) || string.IsNullOrWhiteSpace(normalizedRight))
        {
            return false;
        }

        return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase) ||
               normalizedLeft.Contains(normalizedRight, StringComparison.OrdinalIgnoreCase) ||
               normalizedRight.Contains(normalizedLeft, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeForSpeechComparison(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text
            .Replace("رابط ويب", string.Empty, StringComparison.Ordinal)
            .Replace("عنوان صفحة", string.Empty, StringComparison.Ordinal)
            .Replace("حقل إدخال ويب", string.Empty, StringComparison.Ordinal)
            .Replace("زر ويب", string.Empty, StringComparison.Ordinal)
            .Replace("زر تبديل ويب", string.Empty, StringComparison.Ordinal)
            .Replace("خانة اختيار", string.Empty, StringComparison.Ordinal)
            .Replace("زر اختيار ويب", string.Empty, StringComparison.Ordinal)
            .Replace("مربع خيارات ويب", string.Empty, StringComparison.Ordinal)
            .Replace("رسم ويب", string.Empty, StringComparison.Ordinal)
            .Replace("إطار ويب", string.Empty, StringComparison.Ordinal)
            .Replace("علامة تبويب ويب", string.Empty, StringComparison.Ordinal)
            .Replace("عنصر قائمة ويب", string.Empty, StringComparison.Ordinal)
            .Replace("قائمة ويب", string.Empty, StringComparison.Ordinal)
            .Replace("جدول", string.Empty, StringComparison.Ordinal)
            .Replace("حوار ويب", string.Empty, StringComparison.Ordinal)
            .Replace("معلم صفحة", string.Empty, StringComparison.Ordinal)
            .Replace("الحالة", string.Empty, StringComparison.Ordinal)
            .Replace("القيمة", string.Empty, StringComparison.Ordinal)
            .Replace(".", " ", StringComparison.Ordinal)
            .Replace("،", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static int FindNextWordStart(string text, int offset)
    {
        int index = Math.Max(offset, 0);
        while (index < text.Length && !IsWordCharacter(text, index))
        {
            index++;
        }

        return index < text.Length ? index : -1;
    }

    private static int FindPreviousWordStart(string text, int offset)
    {
        int index = Math.Min(offset - 1, text.Length - 1);
        while (index >= 0 && !IsWordCharacter(text, index))
        {
            index--;
        }

        if (index < 0)
        {
            return -1;
        }

        while (index > 0 && IsWordCharacter(text, index - 1))
        {
            index--;
        }

        return index;
    }

    private static int FindWordEnd(string text, int start)
    {
        int index = start;
        while (index < text.Length && IsWordCharacter(text, index))
        {
            index++;
        }

        return index;
    }

    private static bool IsWordCharacter(string text, int index)
    {
        char value = text[index];
        UnicodeCategory category = char.GetUnicodeCategory(value);
        if (char.IsLetterOrDigit(value) ||
            category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark)
        {
            return true;
        }

        if (value is '\'' or '_' or '-')
        {
            bool hasWordCharBefore = index > 0 && IsLetterDigitOrMark(text[index - 1]);
            bool hasWordCharAfter = index + 1 < text.Length && IsLetterDigitOrMark(text[index + 1]);
            return hasWordCharBefore && hasWordCharAfter;
        }

        return false;
    }

    private static bool IsLetterDigitOrMark(char value)
    {
        UnicodeCategory category = char.GetUnicodeCategory(value);
        return char.IsLetterOrDigit(value) ||
               category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;
    }

    private static string DescribeCharacter(char character) =>
        character switch
        {
            ' ' => "مسافة",
            '\t' => "جدولة",
            '\n' => "سطر جديد",
            '.' => "نقطة",
            ',' => "فاصلة",
            '،' => "فاصلة عربية",
            ':' => "نقطتان",
            ';' => "فاصلة منقوطة",
            '؛' => "فاصلة منقوطة عربية",
            '!' => "علامة تعجب",
            '?' => "علامة استفهام",
            '؟' => "علامة استفهام عربية",
            '-' => "شرطة",
            '_' => "شرطة سفلية",
            '/' => "شرطة مائلة",
            '\\' => "شرطة مائلة عكسية",
            '(' => "قوس فتح",
            ')' => "قوس إغلاق",
            '[' => "قوس مربع فتح",
            ']' => "قوس مربع إغلاق",
            '{' => "قوس معقوف فتح",
            '}' => "قوس معقوف إغلاق",
            '"' => "علامة اقتباس مزدوجة",
            '\'' => "علامة اقتباس مفردة",
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

        string parentSummary = item.ParentSummary ?? string.Empty;
        string lineSummary = item.Summary ?? string.Empty;

        if (item.LineIndexWithinElement <= 0 || string.IsNullOrWhiteSpace(parentSummary))
        {
            return string.IsNullOrWhiteSpace(parentSummary) ? lineSummary : parentSummary;
        }

        if (string.IsNullOrWhiteSpace(lineSummary))
        {
            return parentSummary;
        }

        if (AreSimilarForSpeech(parentSummary, lineSummary))
        {
            return lineSummary;
        }

        if (ShouldSpeakLineBeforeParent(item, parentSummary, lineSummary))
        {
            return $"{lineSummary}. {parentSummary}";
        }

        return $"{parentSummary}. {lineSummary}";
    }

    private static bool ShouldSpeakLineBeforeParent(BufferLine item, string parentSummary, string lineSummary)
    {
        if (item.LineIndexWithinElement <= 0)
        {
            return false;
        }

        string normalizedParent = NormalizeForSpeechComparison(parentSummary);
        string normalizedLine = NormalizeForSpeechComparison(lineSummary);
        if (string.IsNullOrWhiteSpace(normalizedParent) || string.IsNullOrWhiteSpace(normalizedLine))
        {
            return false;
        }

        return normalizedParent.Length <= normalizedLine.Length / 2;
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

    private enum PendingRefreshReason
    {
        None,
        SyncToFocus,
        Refresh
    }
}
