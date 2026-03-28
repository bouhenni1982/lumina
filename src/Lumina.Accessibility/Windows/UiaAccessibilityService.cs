using System.Diagnostics;
using System.Windows.Automation;
using Lumina.Core.Abstractions;
using Lumina.Core.Models;

namespace Lumina.Accessibility.Windows;

public sealed class UiaAccessibilityService : IAccessibilityService
{
    private readonly MsaaFallbackProbe _msaaFallbackProbe = new();
    private readonly Ia2FallbackProbe _ia2FallbackProbe = new();
    private readonly BrowserAccessibilityAdapter _browserAccessibilityAdapter = new();

    public event EventHandler<ScreenEvent>? EventRaised;

    public void Start()
    {
        Automation.AddAutomationFocusChangedEventHandler(OnFocusChanged);
    }

    private void OnFocusChanged(object src, AutomationFocusChangedEventArgs args)
    {
        if (src is not AutomationElement element)
        {
            return;
        }

        string processName = ResolveProcessName(element.Current.ProcessId);
        string role =
            element.Current.ControlType?.ProgrammaticName?.Replace("ControlType.", "").ToLowerInvariant()
            ?? "control";
        string name = element.Current.Name ?? "Unnamed";
        string? value = null;
        string sourceApi = "UIA";
        string? hint = element.Current.HelpText;

        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object? pattern))
        {
            value = ((ValuePattern)pattern).Current.Value;
        }

        MsaaAccessibleInfo? msaaInfo = _msaaFallbackProbe.TryGetInfo(element.Current.NativeWindowHandle);
        if (msaaInfo is not null)
        {
            bool usedFallback = false;

            if (string.IsNullOrWhiteSpace(name) || name == "Unnamed")
            {
                name = msaaInfo.Name ?? name;
                usedFallback = true;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                value = msaaInfo.Value ?? value;
                usedFallback = true;
            }

            if (string.IsNullOrWhiteSpace(role) || role == "control")
            {
                role = msaaInfo.Role;
                usedFallback = true;
            }

            if (usedFallback)
            {
                sourceApi = "UIA+MSAA";
            }
        }

        Ia2AccessibleInfo? ia2Info = _ia2FallbackProbe.TryGetInfo(
            element.Current.NativeWindowHandle,
            processName);

        if (ia2Info is not null)
        {
            if (string.IsNullOrWhiteSpace(hint))
            {
                hint = BuildIa2Hint(ia2Info);
            }

            string ia2Tag = ia2Info.IsAvailable ? "IA2" : "IA2-candidate";
            sourceApi = sourceApi.Contains("MSAA", StringComparison.Ordinal)
                ? $"UIA+MSAA+{ia2Tag}"
                : $"UIA+{ia2Tag}";
        }

        BrowserAdaptation browserAdaptation = _browserAccessibilityAdapter.Apply(
            element,
            processName,
            sourceApi,
            role,
            value,
            hint);

        role = browserAdaptation.Role;
        hint = browserAdaptation.Hint;

        string id = string.IsNullOrWhiteSpace(element.Current.AutomationId)
            ? $"{element.Current.ProcessId}:{role}:{name}"
            : element.Current.AutomationId;

        AccessibleNode node = new(
            Id: id,
            SourceApi: sourceApi,
            Name: name,
            Role: role,
            SemanticRole: browserAdaptation.SemanticRole,
            Value: value,
            Hint: hint,
            ContextKind: browserAdaptation.ContextKind,
            SourceProcess: processName,
            TimestampUtc: DateTimeOffset.UtcNow);

        EventRaised?.Invoke(
            this,
            new ScreenEvent(
                EventType: "focusChanged",
                Node: node,
                UserInitiated: true,
                Priority: 100));
    }

    public void Dispose()
    {
        Automation.RemoveAllEventHandlers();
    }

    private static string ResolveProcessName(int processId)
    {
        try
        {
            return Process.GetProcessById(processId).ProcessName;
        }
        catch
        {
            return "unknown";
        }
    }

    private static string BuildIa2Hint(Ia2AccessibleInfo ia2Info)
    {
        List<string> parts = new();

        if (!string.IsNullOrWhiteSpace(ia2Info.Description))
        {
            parts.Add(ia2Info.Description);
        }

        if (!string.IsNullOrWhiteSpace(ia2Info.KeyboardShortcut))
        {
            parts.Add($"shortcut: {ia2Info.KeyboardShortcut}");
        }

        if (!ia2Info.IsAvailable)
        {
            parts.Add($"ia2 candidate via {ia2Info.Framework}/{ia2Info.WindowClass}");
        }

        return string.Join(" | ", parts);
    }
}
