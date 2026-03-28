using System.Windows.Automation;

namespace Lumina.Samples;

public static class UiaReader
{
    public static string ReadFocusedElement()
    {
        AutomationElement? element = AutomationElement.FocusedElement;
        if (element is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        string name = element.Current.Name ?? "بدون اسم";
        string role = element.Current.ControlType?.ProgrammaticName ?? "unknown";
        string value = string.Empty;

        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object? pattern))
        {
            value = ((ValuePattern)pattern).Current.Value ?? string.Empty;
        }

        return $"العنصر الحالي: {name} | الدور: {role} | القيمة: {value}";
    }
}
