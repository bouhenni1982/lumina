using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace Lumina.Input;

public static class TextReviewCursor
{
    private static readonly object Sync = new();
    private static string? _elementRuntimeId;
    private static TextPatternRange? _lineRange;
    private static TextPatternRange? _characterRange;

    public static string ReadPreviousLine() => ReadLine(-1, "لا يوجد سطر سابق.", "العنصر الحالي لا يدعم مراجعة النص.");

    public static string ReadNextLine() => ReadLine(1, "لا يوجد سطر لاحق.", "العنصر الحالي لا يدعم مراجعة النص.");

    public static string ReadPreviousCharacter() => ReadCharacter(-1, "لا يوجد حرف سابق.", "العنصر الحالي لا يدعم مراجعة الأحرف.");

    public static string ReadNextCharacter() => ReadCharacter(1, "لا يوجد حرف لاحق.", "العنصر الحالي لا يدعم مراجعة الأحرف.");

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
}
