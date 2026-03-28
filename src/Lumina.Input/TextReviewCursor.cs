using System.Text.RegularExpressions;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace Lumina.Input;

public static class TextReviewCursor
{
    private static readonly object Sync = new();
    private static readonly Regex SentenceRegex = new(@"[^.!?؟]+[.!?؟]*", RegexOptions.Compiled);
    private static string? _elementRuntimeId;
    private static TextPatternRange? _lineRange;
    private static TextPatternRange? _characterRange;
    private static string? _sentenceContextKey;
    private static int _sentenceIndex = -1;

    public static string ReadCurrentLine() => ReadCurrent(TextUnit.Line, "السطر", "العنصر الحالي لا يدعم مراجعة النص.");

    public static string ReadPreviousLine() => ReadLine(-1, "لا يوجد سطر سابق.", "العنصر الحالي لا يدعم مراجعة النص.");

    public static string ReadNextLine() => ReadLine(1, "لا يوجد سطر لاحق.", "العنصر الحالي لا يدعم مراجعة النص.");

    public static string ReadPreviousCharacter() => ReadCharacter(-1, "لا يوجد حرف سابق.", "العنصر الحالي لا يدعم مراجعة الأحرف.");

    public static string ReadNextCharacter() => ReadCharacter(1, "لا يوجد حرف لاحق.", "العنصر الحالي لا يدعم مراجعة الأحرف.");

    public static string ReadPreviousWord() => ReadUnit(-1, TextUnit.Word, "لا توجد كلمة سابقة.", "العنصر الحالي لا يدعم مراجعة الكلمات.", "الكلمة");

    public static string ReadNextWord() => ReadUnit(1, TextUnit.Word, "لا توجد كلمة لاحقة.", "العنصر الحالي لا يدعم مراجعة الكلمات.", "الكلمة");

    public static string ReadPreviousParagraph() => ReadUnit(-1, TextUnit.Paragraph, "لا توجد فقرة سابقة.", "العنصر الحالي لا يدعم مراجعة الفقرات.", "الفقرة");

    public static string ReadNextParagraph() => ReadUnit(1, TextUnit.Paragraph, "لا توجد فقرة لاحقة.", "العنصر الحالي لا يدعم مراجعة الفقرات.", "الفقرة");

    public static string ReadPreviousSentence() => ReadSentence(-1, "لا توجد جملة سابقة.", "العنصر الحالي لا يدعم مراجعة الجمل.");

    public static string ReadNextSentence() => ReadSentence(1, "لا توجد جملة لاحقة.", "العنصر الحالي لا يدعم مراجعة الجمل.");

    public static string MoveToStartOfLine() => MoveToLineBoundary(toStart: true);

    public static string MoveToEndOfLine() => MoveToLineBoundary(toStart: false);

    public static string SayAllFromReviewCursor()
    {
        if (!TryGetTextPattern(out AutomationElement? element, out TextPattern? pattern))
        {
            return "العنصر الحالي لا يدعم القراءة المتصلة.";
        }

        lock (Sync)
        {
            EnsureRanges(pattern, element);

            TextPatternRange anchor = _characterRange?.Clone() ?? GetAnchorRange(pattern);
            TextPatternRange documentEnd = pattern.DocumentRange.Clone();
            anchor.MoveEndpointByRange(TextPatternRangeEndpoint.End, documentEnd, TextPatternRangeEndpoint.End);

            string text = Normalize(anchor.GetText(-1));
            return string.IsNullOrWhiteSpace(text)
                ? "لا يوجد نص متبق للقراءة."
                : $"قراءة متصلة {text}";
        }
    }

    private static string ReadCurrent(TextUnit unit, string label, string unsupportedMessage)
    {
        if (!TryGetTextPattern(out AutomationElement? element, out TextPattern? pattern))
        {
            return unsupportedMessage;
        }

        lock (Sync)
        {
            EnsureRanges(pattern, element);

            TextPatternRange? range = unit switch
            {
                TextUnit.Line => _lineRange,
                TextUnit.Character => _characterRange,
                _ => null
            };

            if (range is null)
            {
                return unsupportedMessage;
            }

            string text = Normalize(range.GetText(-1));
            return string.IsNullOrWhiteSpace(text) ? unsupportedMessage : $"{label} {text}";
        }
    }

    private static string ReadLine(int delta, string boundaryMessage, string unsupportedMessage)
    {
        if (!TryGetTextPattern(out AutomationElement? element, out TextPattern? pattern))
        {
            return unsupportedMessage;
        }

        lock (Sync)
        {
            EnsureRanges(pattern, element);

            if (_lineRange is null)
            {
                return unsupportedMessage;
            }

            int moved = _lineRange.Move(TextUnit.Line, delta);
            if (moved == 0)
            {
                return boundaryMessage;
            }

            _lineRange.ExpandToEnclosingUnit(TextUnit.Line);
            string text = Normalize(_lineRange.GetText(-1));
            if (string.IsNullOrWhiteSpace(text))
            {
                return boundaryMessage;
            }

            _characterRange = _lineRange.Clone();
            _characterRange.ExpandToEnclosingUnit(TextUnit.Character);
            return $"السطر {text}";
        }
    }

    private static string ReadCharacter(int delta, string boundaryMessage, string unsupportedMessage)
    {
        if (!TryGetTextPattern(out AutomationElement? element, out TextPattern? pattern))
        {
            return unsupportedMessage;
        }

        lock (Sync)
        {
            EnsureRanges(pattern, element);

            if (_characterRange is null)
            {
                return unsupportedMessage;
            }

            int moved = _characterRange.Move(TextUnit.Character, delta);
            if (moved == 0)
            {
                return boundaryMessage;
            }

            _characterRange.ExpandToEnclosingUnit(TextUnit.Character);
            string text = Normalize(_characterRange.GetText(-1));
            if (string.IsNullOrWhiteSpace(text))
            {
                return boundaryMessage;
            }

            return $"الحرف {DescribeCharacter(text)}";
        }
    }

    private static string ReadUnit(int delta, TextUnit unit, string boundaryMessage, string unsupportedMessage, string label)
    {
        if (!TryGetTextPattern(out AutomationElement? element, out TextPattern? pattern))
        {
            return unsupportedMessage;
        }

        lock (Sync)
        {
            EnsureRanges(pattern, element);

            TextPatternRange range = _characterRange?.Clone() ?? GetAnchorRange(pattern);
            int moved = range.Move(unit, delta);
            if (moved == 0)
            {
                return boundaryMessage;
            }

            range.ExpandToEnclosingUnit(unit);
            string text = Normalize(range.GetText(-1));
            if (string.IsNullOrWhiteSpace(text))
            {
                return boundaryMessage;
            }

            _characterRange = range.Clone();
            return $"{label} {text}";
        }
    }

    private static string MoveToLineBoundary(bool toStart)
    {
        if (!TryGetTextPattern(out AutomationElement? element, out TextPattern? pattern))
        {
            return "العنصر الحالي لا يدعم مراجعة النص.";
        }

        lock (Sync)
        {
            EnsureRanges(pattern, element);

            if (_lineRange is null)
            {
                return "العنصر الحالي لا يدعم مراجعة النص.";
            }

            TextPatternRange line = _lineRange.Clone();
            line.ExpandToEnclosingUnit(TextUnit.Line);

            TextPatternRange probe = line.Clone();
            probe.ExpandToEnclosingUnit(TextUnit.Character);

            while (true)
            {
                TextPatternRange next = probe.Clone();
                int moved = next.Move(TextUnit.Character, toStart ? -1 : 1);
                if (moved == 0)
                {
                    break;
                }

                next.ExpandToEnclosingUnit(TextUnit.Character);
                if (CrossesLineBoundary(line, next))
                {
                    break;
                }

                probe = next;
            }

            _characterRange = probe.Clone();
            string text = Normalize(_characterRange.GetText(-1));
            return toStart
                ? $"بداية السطر {DescribeCharacter(text)}"
                : $"نهاية السطر {DescribeCharacter(text)}";
        }
    }

    private static string ReadSentence(int delta, string boundaryMessage, string unsupportedMessage)
    {
        if (!TryGetTextPattern(out AutomationElement? element, out TextPattern? pattern))
        {
            return unsupportedMessage;
        }

        lock (Sync)
        {
            EnsureRanges(pattern, element);

            TextPatternRange anchor = _characterRange?.Clone() ?? GetAnchorRange(pattern);
            TextPatternRange paragraphRange = anchor.Clone();
            paragraphRange.ExpandToEnclosingUnit(TextUnit.Paragraph);

            string paragraphText = paragraphRange.GetText(-1) ?? string.Empty;
            List<string> sentences = ResolveSentences(paragraphText);
            if (sentences.Count == 0)
            {
                return unsupportedMessage;
            }

            string contextKey = $"{GetRuntimeId(element)}::{paragraphText}";
            if (!string.Equals(_sentenceContextKey, contextKey, StringComparison.Ordinal))
            {
                _sentenceContextKey = contextKey;
                _sentenceIndex = ResolveCurrentSentenceIndex(paragraphText, anchor, paragraphRange, sentences);
            }
            else
            {
                _sentenceIndex += delta;
            }

            if (_sentenceIndex < 0 || _sentenceIndex >= sentences.Count)
            {
                _sentenceIndex = Math.Clamp(_sentenceIndex, 0, sentences.Count - 1);
                return boundaryMessage;
            }

            return $"الجملة {sentences[_sentenceIndex]}";
        }
    }

    private static bool TryGetTextPattern(out AutomationElement? element, out TextPattern? pattern)
    {
        element = FocusSnapshotReader.GetFocusedElement();
        pattern = null;

        if (element is null)
        {
            return false;
        }

        if (!element.TryGetCurrentPattern(TextPattern.Pattern, out object? patternObject))
        {
            return false;
        }

        pattern = (TextPattern)patternObject;
        return true;
    }

    private static void EnsureRanges(TextPattern pattern, AutomationElement element)
    {
        string runtimeId = GetRuntimeId(element);
        if (_elementRuntimeId != runtimeId || _lineRange is null || _characterRange is null)
        {
            TextPatternRange anchor = GetAnchorRange(pattern);
            _lineRange = anchor.Clone();
            _lineRange.ExpandToEnclosingUnit(TextUnit.Line);

            _characterRange = anchor.Clone();
            _characterRange.ExpandToEnclosingUnit(TextUnit.Character);
            _elementRuntimeId = runtimeId;
            _sentenceContextKey = null;
            _sentenceIndex = -1;
        }
    }

    private static TextPatternRange GetAnchorRange(TextPattern pattern)
    {
        TextPatternRange[] selection = pattern.GetSelection();
        if (selection.Length > 0)
        {
            return selection[0].Clone();
        }

        TextPatternRange document = pattern.DocumentRange.Clone();
        document.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, -1);
        return document;
    }

    private static bool CrossesLineBoundary(TextPatternRange lineRange, TextPatternRange characterRange)
    {
        TextPatternRange start = characterRange.Clone();
        start.MoveEndpointByRange(TextPatternRangeEndpoint.End, start, TextPatternRangeEndpoint.Start);
        return lineRange.CompareEndpoints(TextPatternRangeEndpoint.Start, start, TextPatternRangeEndpoint.Start) > 0 ||
               lineRange.CompareEndpoints(TextPatternRangeEndpoint.End, start, TextPatternRangeEndpoint.Start) <= 0;
    }

    private static string GetRuntimeId(AutomationElement element)
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

    private static string Normalize(string? text) =>
        (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();

    private static string DescribeCharacter(string text) =>
        text switch
        {
            " " => "مسافة",
            "\t" => "جدولة",
            _ => text
        };

    private static List<string> ResolveSentences(string paragraphText)
    {
        List<string> sentences = [];
        foreach (Match match in SentenceRegex.Matches(paragraphText))
        {
            string text = Normalize(match.Value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                sentences.Add(text);
            }
        }

        return sentences;
    }

    private static int ResolveCurrentSentenceIndex(
        string paragraphText,
        TextPatternRange anchor,
        TextPatternRange paragraphRange,
        List<string> sentences)
    {
        TextPatternRange prefixRange = paragraphRange.Clone();
        prefixRange.MoveEndpointByRange(TextPatternRangeEndpoint.End, anchor, TextPatternRangeEndpoint.Start);
        string prefixText = prefixRange.GetText(-1) ?? string.Empty;
        int currentOffset = prefixText.Length;

        int runningOffset = 0;
        for (int i = 0; i < sentences.Count; i++)
        {
            int start = paragraphText.IndexOf(sentences[i], runningOffset, StringComparison.Ordinal);
            if (start < 0)
            {
                start = runningOffset;
            }

            int end = start + sentences[i].Length;
            if (currentOffset <= end)
            {
                return i;
            }

            runningOffset = end;
        }

        return Math.Max(sentences.Count - 1, 0);
    }
}
