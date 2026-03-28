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

        string name = ResolveName(element);
        string role = ResolveRole(element);

        return $"{role} {name}";
    }

    public static string ReadCurrentPageTitle()
    {
        AutomationElement? element = AutomationElement.FocusedElement;
        if (element is null)
        {
            return "لا توجد صفحة نشطة حاليا.";
        }

        if (!IsBrowserContext(element))
        {
            return "العنصر الحالي ليس داخل متصفح مدعوم.";
        }

        AutomationElement? window = FindAncestor(
            element,
            current => current.Current.ControlType == ControlType.Window);

        string title = window is null ? ResolveName(element) : ResolveName(window);
        return $"عنوان الصفحة أو النافذة {title}";
    }

    public static string ReadCurrentWebSummary()
    {
        AutomationElement? element = AutomationElement.FocusedElement;
        if (element is null)
        {
            return "لا يوجد عنصر ويب نشط حاليا.";
        }

        if (!IsBrowserContext(element))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        string semanticRole = ResolveWebSemanticRole(element);
        string name = ResolveName(element);
        string pageTitle = ResolveWindowTitle(element);
        string value = TryReadValue(element);

        string text = semanticRole switch
        {
            "web_link" => $"رابط ويب {name}",
            "web_heading" => $"عنوان صفحة {name}",
            "web_edit" => $"حقل إدخال ويب {name}",
            "web_document" => $"مستند ويب {name}",
            "web_button" => $"زر ويب {name}",
            "web_checkbox" => $"خانة اختيار {name}",
            _ => $"عنصر ويب {name}"
        };

        if (!string.IsNullOrWhiteSpace(value))
        {
            text = $"{text}. القيمة {value}";
        }

        if (!string.IsNullOrWhiteSpace(pageTitle))
        {
            text = $"{text}. الصفحة {pageTitle}";
        }

        return text;
    }

    private static bool IsBrowserContext(AutomationElement element)
    {
        string processName = ResolveProcessName(element);
        if (processName is "chrome" or "msedge" or "firefox" or "electron" or "code" or "teams")
        {
            return true;
        }

        string className = element.Current.ClassName ?? string.Empty;
        return className.Contains("Chrome", StringComparison.OrdinalIgnoreCase) ||
               className.Contains("Mozilla", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveWebSemanticRole(AutomationElement element)
    {
        string role = ResolveRole(element);
        string localizedRole = (element.Current.LocalizedControlType ?? string.Empty).ToLowerInvariant();
        string itemType = (element.Current.ItemType ?? string.Empty).ToLowerInvariant();
        string helpText = (element.Current.HelpText ?? string.Empty).ToLowerInvariant();

        if (role == "hyperlink" || localizedRole.Contains("link"))
        {
            return "web_link";
        }

        if (localizedRole.Contains("heading") || itemType.Contains("heading") || helpText.Contains("heading"))
        {
            return "web_heading";
        }

        if (role == "document" || localizedRole.Contains("document"))
        {
            return "web_document";
        }

        if (role == "edit" || localizedRole.Contains("edit") || localizedRole.Contains("text field"))
        {
            return "web_edit";
        }

        if (role == "button" || localizedRole.Contains("button"))
        {
            return "web_button";
        }

        if (localizedRole.Contains("check box") || role.Contains("check", StringComparison.OrdinalIgnoreCase))
        {
            return "web_checkbox";
        }

        return "web_control";
    }

    private static string ResolveWindowTitle(AutomationElement element)
    {
        AutomationElement? window = FindAncestor(
            element,
            current => current.Current.ControlType == ControlType.Window);

        return window is null ? string.Empty : ResolveName(window);
    }

    private static string TryReadValue(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object? pattern))
        {
            return ((ValuePattern)pattern).Current.Value ?? string.Empty;
        }

        return string.Empty;
    }

    private static string ResolveName(AutomationElement element) =>
        string.IsNullOrWhiteSpace(element.Current.Name) ? "عنصر غير مسمى" : element.Current.Name;

    private static string ResolveRole(AutomationElement element) =>
        element.Current.ControlType?.ProgrammaticName?.Replace("ControlType.", "").ToLowerInvariant()
        ?? "control";

    private static string ResolveProcessName(AutomationElement element)
    {
        try
        {
            return System.Diagnostics.Process.GetProcessById(element.Current.ProcessId).ProcessName.ToLowerInvariant();
        }
        catch
        {
            return "unknown";
        }
    }

    private static AutomationElement? FindAncestor(
        AutomationElement element,
        Func<AutomationElement, bool> predicate)
    {
        AutomationElement? current = element;
        while (current is not null)
        {
            if (predicate(current))
            {
                return current;
            }

            current = TreeWalker.ControlViewWalker.GetParent(current);
        }

        return null;
    }
}
