using System.Windows.Automation;

namespace Lumina.Accessibility.Windows;

internal sealed class BrowserAccessibilityAdapter
{
    private static readonly HashSet<string> BrowserProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome",
        "msedge",
        "firefox",
        "electron",
        "code",
        "teams"
    };

    public BrowserAdaptation Apply(
        AutomationElement element,
        string processName,
        string sourceApi,
        string role,
        string? value,
        string? hint)
    {
        if (!IsBrowserContext(element, processName, sourceApi))
        {
            return new BrowserAdaptation(role, null, hint, null);
        }

        string localizedRole = (element.Current.LocalizedControlType ?? string.Empty).ToLowerInvariant();
        string itemType = (element.Current.ItemType ?? string.Empty).ToLowerInvariant();
        string semanticRole = ResolveSemanticRole(role, localizedRole, itemType, hint, value);
        string normalizedRole = NormalizeRole(role, semanticRole);
        string normalizedHint = BuildHint(processName, sourceApi, semanticRole, hint);

        return new BrowserAdaptation(
            Role: normalizedRole,
            SemanticRole: semanticRole,
            Hint: normalizedHint,
            ContextKind: "browser");
    }

    private static bool IsBrowserContext(AutomationElement element, string processName, string sourceApi)
    {
        if (BrowserProcesses.Contains(processName))
        {
            return true;
        }

        if (sourceApi.Contains("IA2", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string className = element.Current.ClassName ?? string.Empty;
        return className.Contains("Chrome", StringComparison.OrdinalIgnoreCase) ||
               className.Contains("Mozilla", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveSemanticRole(
        string role,
        string localizedRole,
        string itemType,
        string? hint,
        string? value)
    {
        string hintText = hint?.ToLowerInvariant() ?? string.Empty;

        if (role is "hyperlink" || localizedRole.Contains("link"))
        {
            return "web_link";
        }

        if (localizedRole.Contains("heading") || itemType.Contains("heading") || hintText.Contains("heading"))
        {
            return "web_heading";
        }

        if (role is "document" || localizedRole.Contains("document"))
        {
            return "web_document";
        }

        if (role is "edit" || localizedRole.Contains("edit") || localizedRole.Contains("text field"))
        {
            return "web_edit";
        }

        if (role is "button" || localizedRole.Contains("button"))
        {
            return "web_button";
        }

        if (role.Contains("radio", StringComparison.OrdinalIgnoreCase) || localizedRole.Contains("radio"))
        {
            return "web_radio";
        }

        if (role.Contains("combo", StringComparison.OrdinalIgnoreCase) || localizedRole.Contains("combo box"))
        {
            return "web_combobox";
        }

        if (localizedRole.Contains("tab"))
        {
            return "web_tab";
        }

        if (localizedRole.Contains("check box") || role.Contains("check", StringComparison.OrdinalIgnoreCase))
        {
            return "web_checkbox";
        }

        if (role is "table" || localizedRole.Contains("table") || localizedRole.Contains("grid"))
        {
            return "web_table";
        }

        if (role is "list" || localizedRole == "list")
        {
            return "web_list";
        }

        if (role is "listitem" || localizedRole.Contains("list item"))
        {
            return "web_listitem";
        }

        if (localizedRole.Contains("dialog") || localizedRole.Contains("alert"))
        {
            return "web_dialog";
        }

        if (localizedRole.Contains("navigation") ||
            localizedRole.Contains("banner") ||
            localizedRole.Contains("main") ||
            localizedRole.Contains("search") ||
            localizedRole.Contains("content info") ||
            localizedRole.Contains("complementary") ||
            itemType.Contains("landmark"))
        {
            return "web_landmark";
        }

        if (!string.IsNullOrWhiteSpace(value) && localizedRole.Contains("text"))
        {
            return "web_text";
        }

        return "web_control";
    }

    private static string NormalizeRole(string role, string semanticRole) =>
        semanticRole switch
        {
            "web_link" => "link",
            "web_heading" => "heading",
            "web_document" => "document",
            "web_edit" => "edit",
            "web_button" => "button",
            "web_radio" => "radiobutton",
            "web_combobox" => "combobox",
            "web_tab" => "tab",
            "web_checkbox" => "checkbox",
            "web_table" => "table",
            "web_list" => "list",
            "web_listitem" => "listitem",
            "web_dialog" => "dialog",
            "web_landmark" => "landmark",
            _ => role
        };

    private static string BuildHint(string processName, string sourceApi, string semanticRole, string? hint)
    {
        List<string> parts = new()
        {
            $"browser:{processName}",
            $"semantic:{semanticRole}",
            $"source:{sourceApi}"
        };

        if (!string.IsNullOrWhiteSpace(hint))
        {
            parts.Add(hint);
        }

        return string.Join(" | ", parts.Distinct(StringComparer.OrdinalIgnoreCase));
    }
}

internal sealed record BrowserAdaptation(
    string Role,
    string? SemanticRole,
    string? Hint,
    string? ContextKind);
