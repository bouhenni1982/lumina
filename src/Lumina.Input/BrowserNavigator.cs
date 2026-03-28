using System.Windows.Automation;

namespace Lumina.Input;

public static class BrowserNavigator
{
    public static string SummarizeCurrentPage()
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا توجد صفحة نشطة حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement root = ResolveNavigationRoot(current);
        List<AutomationElement> elements = EnumerateElements(root).ToList();
        if (elements.Count == 0)
        {
            return "تعذر تحليل الصفحة الحالية.";
        }

        int headings = 0;
        int links = 0;
        int editFields = 0;
        int buttons = 0;
        int checkboxes = 0;
        int landmarks = 0;
        int tables = 0;
        int lists = 0;
        int dialogs = 0;
        int formFields = 0;

        foreach (AutomationElement element in elements)
        {
            switch (FocusSnapshotReader.ResolveWebSemanticRole(element))
            {
                case "web_heading":
                    headings++;
                    break;
                case "web_link":
                    links++;
                    break;
                case "web_edit":
                    editFields++;
                    formFields++;
                    break;
                case "web_button":
                    buttons++;
                    formFields++;
                    break;
                case "web_checkbox":
                    checkboxes++;
                    formFields++;
                    break;
                case "web_radio":
                case "web_combobox":
                    formFields++;
                    break;
                case "web_landmark":
                    landmarks++;
                    break;
                case "web_table":
                    tables++;
                    break;
                case "web_list":
                    lists++;
                    break;
                case "web_dialog":
                    dialogs++;
                    break;
            }
        }

        string pageTitle = FocusSnapshotReader.ResolveWindowTitle(current);
        List<string> parts = new();
        if (!string.IsNullOrWhiteSpace(pageTitle))
        {
            parts.Add($"الصفحة {pageTitle}");
        }

        parts.Add($"العناوين {headings}");
        parts.Add($"الروابط {links}");
        parts.Add($"حقول الإدخال {editFields}");
        parts.Add($"الأزرار {buttons}");

        if (checkboxes > 0)
        {
            parts.Add($"خانات الاختيار {checkboxes}");
        }

        if (landmarks > 0)
        {
            parts.Add($"المعالم {landmarks}");
        }

        if (tables > 0)
        {
            parts.Add($"الجداول {tables}");
        }

        if (lists > 0)
        {
            parts.Add($"القوائم {lists}");
        }

        if (dialogs > 0)
        {
            parts.Add($"الحوارات {dialogs}");
        }

        if (formFields > 0)
        {
            parts.Add($"عناصر النماذج {formFields}");
        }

        return string.Join(". ", parts);
    }

    public static string MoveToNextLink() => MoveToNextSemanticRole("web_link", "لا يوجد رابط تال في الصفحة.");
    public static string MoveToPreviousLink() => MoveToPreviousSemanticRole("web_link", "لا يوجد رابط سابق في الصفحة.");

    public static string MoveToNextHeading() => MoveToNextSemanticRole("web_heading", "لا يوجد عنوان تال في الصفحة.");
    public static string MoveToPreviousHeading() => MoveToPreviousSemanticRole("web_heading", "لا يوجد عنوان سابق في الصفحة.");

    public static string MoveToNextEditField() => MoveToNextSemanticRole("web_edit", "لا يوجد حقل إدخال تال في الصفحة.");
    public static string MoveToPreviousEditField() => MoveToPreviousSemanticRole("web_edit", "لا يوجد حقل إدخال سابق في الصفحة.");

    public static string MoveToNextButton() => MoveToNextSemanticRole("web_button", "لا يوجد زر تال في الصفحة.");
    public static string MoveToPreviousButton() => MoveToPreviousSemanticRole("web_button", "لا يوجد زر سابق في الصفحة.");

    public static string MoveToNextCheckbox() => MoveToNextSemanticRole("web_checkbox", "لا توجد خانة اختيار تالية في الصفحة.");
    public static string MoveToPreviousCheckbox() => MoveToPreviousSemanticRole("web_checkbox", "لا توجد خانة اختيار سابقة في الصفحة.");

    public static string MoveToNextLandmark() => MoveToNextSemanticRole("web_landmark", "لا يوجد معلم تال في الصفحة.");
    public static string MoveToPreviousLandmark() => MoveToPreviousSemanticRole("web_landmark", "لا يوجد معلم سابق في الصفحة.");

    public static string MoveToNextTable() => MoveToNextSemanticRole("web_table", "لا يوجد جدول تال في الصفحة.");
    public static string MoveToPreviousTable() => MoveToPreviousSemanticRole("web_table", "لا يوجد جدول سابق في الصفحة.");

    public static string MoveToNextList() => MoveToNextSemanticRole("web_list", "لا توجد قائمة تالية في الصفحة.");
    public static string MoveToPreviousList() => MoveToPreviousSemanticRole("web_list", "لا توجد قائمة سابقة في الصفحة.");

    public static string MoveToNextDialog() => MoveToNextSemanticRole("web_dialog", "لا يوجد حوار تال في الصفحة.");
    public static string MoveToPreviousDialog() => MoveToPreviousSemanticRole("web_dialog", "لا يوجد حوار سابق في الصفحة.");

    public static string MoveToNextFormField() => MoveToNextFormFieldCore(moveNext: true);
    public static string MoveToPreviousFormField() => MoveToNextFormFieldCore(moveNext: false);

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

    private static string MoveToPreviousSemanticRole(string semanticRole, string missingMessage)
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
        IEnumerable<AutomationElement> orderedCandidates = EnumerateBeforeCurrent(elements, currentIndex)
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

    private static string MoveToNextFormFieldCore(bool moveNext)
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
            return moveNext
                ? "لا يوجد عنصر نموذج تال في الصفحة."
                : "لا يوجد عنصر نموذج سابق في الصفحة.";
        }

        int currentIndex = elements.FindIndex(element => SameElement(element, current));
        IEnumerable<AutomationElement> orderedCandidates = moveNext
            ? EnumerateAfterCurrent(elements, currentIndex)
            : EnumerateBeforeCurrent(elements, currentIndex);

        foreach (AutomationElement candidate in orderedCandidates)
        {
            string role = FocusSnapshotReader.ResolveWebSemanticRole(candidate);
            if (!IsFormFieldRole(role))
            {
                continue;
            }

            try
            {
                candidate.SetFocus();
                return FocusSnapshotReader.BuildWebSummary(candidate);
            }
            catch
            {
            }
        }

        return moveNext
            ? "لا يوجد عنصر نموذج تال في الصفحة."
            : "لا يوجد عنصر نموذج سابق في الصفحة.";
    }

    internal static AutomationElement ResolveNavigationRootForBuffer(AutomationElement current) => ResolveNavigationRoot(current);

    internal static IEnumerable<AutomationElement> EnumerateBufferCandidates(AutomationElement root) =>
        EnumerateElements(root).Where(IsBufferCandidate);

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

    private static bool IsBufferCandidate(AutomationElement element)
    {
        string semanticRole = FocusSnapshotReader.ResolveWebSemanticRole(element);
        if (semanticRole is "web_control")
        {
            return false;
        }

        string name = FocusSnapshotReader.ResolveName(element);
        return !string.IsNullOrWhiteSpace(name) && name != "عنصر غير مسمى";
    }

    private static bool IsFormFieldRole(string semanticRole) =>
        semanticRole is "web_edit" or "web_combobox" or "web_checkbox" or "web_radio" or "web_button";

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

    private static IEnumerable<AutomationElement> EnumerateBeforeCurrent(IReadOnlyList<AutomationElement> elements, int currentIndex)
    {
        int startIndex = currentIndex < 0 ? elements.Count - 1 : currentIndex - 1;

        for (int index = startIndex; index >= 0; index--)
        {
            yield return elements[index];
        }

        for (int index = elements.Count - 1; index > startIndex; index--)
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
