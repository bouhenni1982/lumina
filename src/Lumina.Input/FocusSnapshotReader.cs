using System.Windows.Automation;

namespace Lumina.Input;

public static class FocusSnapshotReader
{
    public static string ReadCurrentFocusSummary()
    {
        AutomationElement? element = AutomationElement.FocusedElement;
        if (element is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        string name = string.IsNullOrWhiteSpace(element.Current.Name) ? "عنصر غير مسمى" : element.Current.Name;
        string role =
            element.Current.ControlType?.ProgrammaticName?.Replace("ControlType.", "").ToLowerInvariant()
            ?? "control";

        return $"{role} {name}";
    }
}
