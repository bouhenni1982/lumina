using System.Windows;
using System.Windows.Automation;

namespace Lumina.Input;

public static class FocusSnapshotReader
{
    internal static AutomationElement? GetFocusedElement() => AutomationElement.FocusedElement;

    public static string ReadCurrentFocusSummary()
    {
        AutomationElement? element = GetFocusedElement();
        if (element is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        string name = ResolveName(element);
        string role = ResolveRole(element);

        return $"{role} {name}";
    }

    public static string ReadCurrentElementDetails()
    {
        AutomationElement? element = GetFocusedElement();
        if (element is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        string name = ResolveName(element);
        string role = ResolveRole(element);
        string process = ResolveProcessName(element);
        string value = TryReadValue(element);
        string windowTitle = ResolveWindowTitle(element);
        string localizedRole = element.Current.LocalizedControlType ?? string.Empty;
        string helpText = element.Current.HelpText ?? string.Empty;
        string automationId = element.Current.AutomationId ?? string.Empty;

        List<string> segments =
        [
            $"العنصر الحالي {name}",
            $"النوع {role}"
        ];

        if (!string.IsNullOrWhiteSpace(localizedRole) &&
            !string.Equals(localizedRole, role, StringComparison.OrdinalIgnoreCase))
        {
            segments.Add($"الوصف المحلي {localizedRole}");
        }

        if (!string.IsNullOrWhiteSpace(value))
        {
            segments.Add($"القيمة {value}");
        }

        if (IsBrowserContext(element))
        {
            segments.Add($"دور الويب {ResolveWebSemanticRole(element)}");
        }

        if (!string.IsNullOrWhiteSpace(windowTitle))
        {
            segments.Add($"النافذة {windowTitle}");
        }

        segments.Add($"العملية {process}");

        if (!string.IsNullOrWhiteSpace(automationId))
        {
            segments.Add($"معرف الأتمتة {automationId}");
        }

        if (!string.IsNullOrWhiteSpace(helpText))
        {
            segments.Add($"تلميح {helpText}");
        }

        return string.Join(". ", segments);
    }

    public static string ReadCurrentElementAdvancedDetails()
    {
        AutomationElement? element = GetFocusedElement();
        if (element is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        string className = element.Current.ClassName ?? string.Empty;
        string frameworkId = element.Current.FrameworkId ?? string.Empty;
        string acceleratorKey = element.Current.AcceleratorKey ?? string.Empty;
        string accessKey = element.Current.AccessKey ?? string.Empty;
        string itemStatus = element.Current.ItemStatus ?? string.Empty;
        string itemType = element.Current.ItemType ?? string.Empty;
        string patterns = ResolveSupportedPatterns(element);
        Rect bounds = element.Current.BoundingRectangle;

        List<string> segments =
        [
            ReadCurrentElementDetails(),
            $"الإطار {NormalizeValue(frameworkId, "غير معروف")}",
            $"الصنف {NormalizeValue(className, "غير معروف")}",
            $"ممكّن {(element.Current.IsEnabled ? "نعم" : "لا")}",
            $"خارج الشاشة {(element.Current.IsOffscreen ? "نعم" : "لا")}",
            $"يمتلك التركيز {(element.Current.HasKeyboardFocus ? "نعم" : "لا")}"
        ];

        if (!string.IsNullOrWhiteSpace(itemType))
        {
            segments.Add($"نوع العنصر {itemType}");
        }

        if (!string.IsNullOrWhiteSpace(itemStatus))
        {
            segments.Add($"حالة العنصر {itemStatus}");
        }

        if (!string.IsNullOrWhiteSpace(accessKey))
        {
            segments.Add($"مفتاح الوصول {accessKey}");
        }

        if (!string.IsNullOrWhiteSpace(acceleratorKey))
        {
            segments.Add($"مفتاح التسريع {acceleratorKey}");
        }

        if (!bounds.IsEmpty)
        {
            segments.Add($"الموضع {Math.Round(bounds.Left)}, {Math.Round(bounds.Top)}, {Math.Round(bounds.Width)}, {Math.Round(bounds.Height)}");
        }

        if (!string.IsNullOrWhiteSpace(patterns))
        {
            segments.Add($"الأنماط المدعومة {patterns}");
        }

        return string.Join(". ", segments);
    }

    public static string ReadCurrentPageTitle()
    {
        AutomationElement? element = GetFocusedElement();
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
        AutomationElement? element = GetFocusedElement();
        if (element is null)
        {
            return "لا يوجد عنصر ويب نشط حاليا.";
        }

        if (!IsBrowserContext(element))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        return BuildWebSummary(element);
    }

    internal static string BuildWebSummary(AutomationElement element)
    {
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

    internal static bool IsBrowserContext(AutomationElement element)
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

    internal static string ResolveWebSemanticRole(AutomationElement element)
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

    internal static string ResolveWindowTitle(AutomationElement element)
    {
        AutomationElement? window = FindAncestor(
            element,
            current => current.Current.ControlType == ControlType.Window);

        return window is null ? string.Empty : ResolveName(window);
    }

    internal static string TryReadValue(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object? pattern))
        {
            return ((ValuePattern)pattern).Current.Value ?? string.Empty;
        }

        return string.Empty;
    }

    internal static string ResolveName(AutomationElement element) =>
        string.IsNullOrWhiteSpace(element.Current.Name) ? "عنصر غير مسمى" : element.Current.Name;

    internal static string ResolveRole(AutomationElement element) =>
        element.Current.ControlType?.ProgrammaticName?.Replace("ControlType.", "").ToLowerInvariant()
        ?? "control";

    internal static string ResolveProcessName(AutomationElement element)
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

    internal static string ResolveSupportedPatterns(AutomationElement element)
    {
        try
        {
            AutomationPattern[] patterns = element.GetSupportedPatterns();
            if (patterns.Length == 0)
            {
                return string.Empty;
            }

            List<string> names = [];
            foreach (AutomationPattern pattern in patterns)
            {
                string name = pattern.ProgrammaticName ?? string.Empty;
                if (name.StartsWith("AutomationPattern.", StringComparison.OrdinalIgnoreCase))
                {
                    name = name["AutomationPattern.".Length..];
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }

            return string.Join(", ", names);
        }
        catch
        {
            return string.Empty;
        }
    }

    internal static string NormalizeValue(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    internal static AutomationElement? FindAncestor(
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
