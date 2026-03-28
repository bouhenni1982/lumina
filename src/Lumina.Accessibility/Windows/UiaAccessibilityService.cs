using System.Diagnostics;
using System.Windows.Automation;
using Lumina.Core.Abstractions;
using Lumina.Core.Models;

namespace Lumina.Accessibility.Windows;

public sealed class UiaAccessibilityService : IAccessibilityService
{
    private readonly MsaaFallbackProbe _msaaFallbackProbe = new();

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

        string id = string.IsNullOrWhiteSpace(element.Current.AutomationId)
            ? $"{element.Current.ProcessId}:{role}:{name}"
            : element.Current.AutomationId;

        AccessibleNode node = new(
            Id: id,
            SourceApi: sourceApi,
            Name: name,
            Role: role,
            Value: value,
            Hint: element.Current.HelpText,
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
}
