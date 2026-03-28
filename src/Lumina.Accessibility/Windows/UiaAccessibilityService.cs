using System.Diagnostics;
using System.Windows.Automation;
using Lumina.Core.Abstractions;
using Lumina.Core.Models;

namespace Lumina.Accessibility.Windows;

public sealed class UiaAccessibilityService : IAccessibilityService
{
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

        string id = string.IsNullOrWhiteSpace(element.Current.AutomationId)
            ? $"{element.Current.ProcessId}:{role}:{element.Current.Name}"
            : element.Current.AutomationId;

        string? value = null;
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object? pattern))
        {
            value = ((ValuePattern)pattern).Current.Value;
        }

        AccessibleNode node = new(
            Id: id,
            SourceApi: "UIA",
            Name: element.Current.Name ?? "Unnamed",
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
