using System.Windows.Automation;

namespace Lumina.Input;

public static class BrowserNavigator
{
    public static string MoveToNextLink() => MoveToNextSemanticRole("web_link", "لا يوجد رابط تال في الصفحة.");

    public static string MoveToNextHeading() => MoveToNextSemanticRole("web_heading", "لا يوجد عنوان تال في الصفحة.");

    public static string MoveToNextEditField() => MoveToNextSemanticRole("web_edit", "لا يوجد حقل إدخال تال في الصفحة.");

    private static string MoveToNextSemanticRole(string semanticRole, string missingMessage)
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement root = ResolveNavigationRoot(current);
        List<AutomationElement> elements = EnumerateElements(root).ToList();
        if (elements.Count == 0)
        {
            return missingMessage;
        }

        int currentIndex = elements.FindIndex(element => SameElement(element, current));
        IEnumerable<AutomationElement> orderedCandidates = EnumerateAfterCurrent(elements, currentIndex)
            .Where(element => FocusSnapshotReader.ResolveWebSemanticRole(element) == semanticRole);

        foreach (AutomationElement candidate in orderedCandidates)
        {
            try
            {
                candidate.SetFocus();
                return FocusSnapshotReader.BuildWebSummary(candidate);
            }
            catch
            {
            }
        }

        return missingMessage;
    }

    private static AutomationElement ResolveNavigationRoot(AutomationElement current)
    {
        AutomationElement? document = FocusSnapshotReader.FindAncestor(
            current,
            element => element.Current.ControlType == ControlType.Document);

        if (document is not null)
        {
            return document;
        }

        AutomationElement? window = FocusSnapshotReader.FindAncestor(
            current,
            element => element.Current.ControlType == ControlType.Window);

        return window ?? current;
    }

    private static IEnumerable<AutomationElement> EnumerateElements(AutomationElement root)
    {
        Queue<AutomationElement> queue = new();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            AutomationElement current = queue.Dequeue();
            yield return current;

            AutomationElement? child = TreeWalker.ControlViewWalker.GetFirstChild(current);
            while (child is not null)
            {
                queue.Enqueue(child);
                child = TreeWalker.ControlViewWalker.GetNextSibling(child);
            }
        }
    }

    private static IEnumerable<AutomationElement> EnumerateAfterCurrent(IReadOnlyList<AutomationElement> elements, int currentIndex)
    {
        int startIndex = currentIndex < 0 ? 0 : currentIndex + 1;

        for (int index = startIndex; index < elements.Count; index++)
        {
            yield return elements[index];
        }

        for (int index = 0; index < startIndex && index < elements.Count; index++)
        {
            yield return elements[index];
        }
    }

    private static bool SameElement(AutomationElement first, AutomationElement second)
    {
        int[]? left = first.GetRuntimeId();
        int[]? right = second.GetRuntimeId();
        if (left is null || right is null || left.Length != right.Length)
        {
            return false;
        }

        for (int index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }
}
