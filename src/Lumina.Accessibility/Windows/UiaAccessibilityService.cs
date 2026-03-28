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
    private readonly AutomationFocusChangedEventHandler _focusChangedHandler;
    private readonly AutomationEventHandler _liveRegionChangedHandler;
    private readonly AutomationPropertyChangedEventHandler _propertyChangedHandler;

    public event EventHandler<ScreenEvent>? EventRaised;

    public UiaAccessibilityService()
    {
        _focusChangedHandler = OnFocusChanged;
        _liveRegionChangedHandler = OnLiveRegionChanged;
        _propertyChangedHandler = OnLivePropertyChanged;
    }

    public void Start()
    {
        Automation.AddAutomationFocusChangedEventHandler(_focusChangedHandler);
        Automation.AddAutomationEventHandler(
            AutomationElementIdentifiers.LiveRegionChangedEvent,
            AutomationElement.RootElement,
            TreeScope.Subtree,
            _liveRegionChangedHandler);
        Automation.AddAutomationPropertyChangedEventHandler(
            AutomationElement.RootElement,
            TreeScope.Subtree,
            _propertyChangedHandler,
            AutomationElement.NameProperty,
            AutomationElement.HelpTextProperty,
            AutomationElement.ItemStatusProperty,
            ValuePattern.ValueProperty,
            AutomationElementIdentifiers.LiveSettingProperty);
    }

    private void OnFocusChanged(object src, AutomationFocusChangedEventArgs args)
    {
        if (src is not AutomationElement element)
        {
            return;
        }

        RaiseScreenEvent(element, "focusChanged", userInitiated: true, priority: 100);
    }

    private void OnLiveRegionChanged(object src, AutomationEventArgs args)
    {
        if (src is not AutomationElement element)
        {
            return;
        }

        if (!ShouldAnnounceLiveElement(element))
        {
            return;
        }

        RaiseScreenEvent(element, "liveRegionChanged", userInitiated: false, priority: ResolveLivePriority(element));
    }

    private void OnLivePropertyChanged(object src, AutomationPropertyChangedEventArgs args)
    {
        if (src is not AutomationElement element)
        {
            return;
        }

        if (!ShouldAnnounceLiveElement(element))
        {
            return;
        }

        if (args.Property != AutomationElement.NameProperty &&
            args.Property != AutomationElement.HelpTextProperty &&
            args.Property != AutomationElement.ItemStatusProperty &&
            args.Property != ValuePattern.ValueProperty &&
            args.Property != AutomationElementIdentifiers.LiveSettingProperty)
        {
            return;
        }

        RaiseScreenEvent(element, "liveTextChanged", userInitiated: false, priority: ResolveLivePriority(element));
    }

    public void Dispose()
    {
        Automation.RemoveAutomationFocusChangedEventHandler(_focusChangedHandler);
        Automation.RemoveAutomationEventHandler(
            AutomationElementIdentifiers.LiveRegionChangedEvent,
            AutomationElement.RootElement,
            _liveRegionChangedHandler);
        Automation.RemoveAutomationPropertyChangedEventHandler(
            AutomationElement.RootElement,
            _propertyChangedHandler);
    }

    private void RaiseScreenEvent(AutomationElement element, string eventType, bool userInitiated, int priority)
    {
        AccessibleNode node = BuildAccessibleNode(element);
        EventRaised?.Invoke(
            this,
            new ScreenEvent(
                EventType: eventType,
                Node: node,
                UserInitiated: userInitiated,
                Priority: priority));
    }

    private AccessibleNode BuildAccessibleNode(AutomationElement element)
    {
        string processName = ResolveProcessName(element.Current.ProcessId);
        string role =
            element.Current.ControlType?.ProgrammaticName?.Replace("ControlType.", "").ToLowerInvariant()
            ?? "control";
        string name = element.Current.Name ?? "Unnamed";
        string? value = null;
        string? shortcutKey = ResolveShortcutKey(element);
        string? stateSummary = ResolveStateSummary(element);
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
        hint = AppendLiveSettingHint(element, browserAdaptation.Hint);

        string id = string.IsNullOrWhiteSpace(element.Current.AutomationId)
            ? $"{element.Current.ProcessId}:{role}:{name}"
            : element.Current.AutomationId;

        return new AccessibleNode(
            Id: id,
            SourceApi: sourceApi,
            Name: name,
            Role: role,
            SemanticRole: browserAdaptation.SemanticRole,
            Value: value,
            ShortcutKey: shortcutKey,
            StateSummary: stateSummary,
            Hint: hint,
            ContextKind: browserAdaptation.ContextKind,
            SourceProcess: processName,
            TimestampUtc: DateTimeOffset.UtcNow);
    }

    private static bool ShouldAnnounceLiveElement(AutomationElement element)
    {
        if (!IsLikelyBrowserContext(element))
        {
            return false;
        }

        AutomationLiveSetting liveSetting = ResolveLiveSetting(element);
        if (liveSetting != AutomationLiveSetting.Off)
        {
            return true;
        }

        string semanticRole = ResolveSemanticRole(element);
        if (semanticRole is "web_dialog" or "web_landmark")
        {
            return true;
        }

        string localizedRole = (element.Current.LocalizedControlType ?? string.Empty).ToLowerInvariant();
        string itemType = (element.Current.ItemType ?? string.Empty).ToLowerInvariant();
        string name = (element.Current.Name ?? string.Empty).ToLowerInvariant();

        return localizedRole.Contains("alert") ||
               localizedRole.Contains("status") ||
               itemType.Contains("alert") ||
               itemType.Contains("status") ||
               name.Contains("alert") ||
               name.Contains("status");
    }

    private static bool IsLikelyBrowserContext(AutomationElement element)
    {
        string processName = ResolveProcessName(element.Current.ProcessId);
        string sourceApi = "UIA";
        string role =
            element.Current.ControlType?.ProgrammaticName?.Replace("ControlType.", "").ToLowerInvariant()
            ?? "control";
        string? value = null;
        string? hint = element.Current.HelpText;

        return new BrowserAccessibilityAdapter()
            .Apply(element, processName, sourceApi, role, value, hint)
            .ContextKind == "browser";
    }

    private static string ResolveSemanticRole(AutomationElement element)
    {
        string processName = ResolveProcessName(element.Current.ProcessId);
        string role =
            element.Current.ControlType?.ProgrammaticName?.Replace("ControlType.", "").ToLowerInvariant()
            ?? "control";
        string? hint = element.Current.HelpText;

        return new BrowserAccessibilityAdapter()
            .Apply(element, processName, "UIA", role, null, hint)
            .SemanticRole ?? "web_control";
    }

    private static AutomationLiveSetting ResolveLiveSetting(AutomationElement element)
    {
        try
        {
            object propertyValue = element.GetCurrentPropertyValue(AutomationElementIdentifiers.LiveSettingProperty);
            return propertyValue is AutomationLiveSetting liveSetting
                ? liveSetting
                : AutomationLiveSetting.Off;
        }
        catch
        {
            return AutomationLiveSetting.Off;
        }
    }

    private static int ResolveLivePriority(AutomationElement element) =>
        ResolveLiveSetting(element) == AutomationLiveSetting.Assertive ? 110 : 90;

    private static string? AppendLiveSettingHint(AutomationElement element, string? hint)
    {
        AutomationLiveSetting liveSetting = ResolveLiveSetting(element);
        if (liveSetting == AutomationLiveSetting.Off)
        {
            return hint;
        }

        string liveText = liveSetting == AutomationLiveSetting.Assertive
            ? "live:assertive"
            : "live:polite";

        if (string.IsNullOrWhiteSpace(hint))
        {
            return liveText;
        }

        if (hint.Contains(liveText, StringComparison.OrdinalIgnoreCase))
        {
            return hint;
        }

        return $"{hint} | {liveText}";
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

    private static string? ResolveShortcutKey(AutomationElement element)
    {
        string acceleratorKey = NormalizeMetadataValue(element.Current.AcceleratorKey);
        string accessKey = NormalizeMetadataValue(element.Current.AccessKey);

        if (string.IsNullOrWhiteSpace(acceleratorKey))
        {
            return accessKey;
        }

        if (string.IsNullOrWhiteSpace(accessKey) ||
            string.Equals(acceleratorKey, accessKey, StringComparison.OrdinalIgnoreCase))
        {
            return acceleratorKey;
        }

        return $"{acceleratorKey} / {accessKey}";
    }

    private static string? ResolveStateSummary(AutomationElement element)
    {
        List<string> states = [];

        if (element.TryGetCurrentPattern(TogglePattern.Pattern, out object? togglePatternObject))
        {
            string? toggleState = ((TogglePattern)togglePatternObject).Current.ToggleState switch
            {
                ToggleState.On => "محدد",
                ToggleState.Off => "غير محدد",
                ToggleState.Indeterminate => "حالة غير محسومة",
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(toggleState))
            {
                states.Add(toggleState);
            }
        }

        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object? selectionItemPatternObject))
        {
            string selectionState = ((SelectionItemPattern)selectionItemPatternObject).Current.IsSelected
                ? "محدد"
                : "غير محدد";

            if (!states.Contains(selectionState, StringComparer.Ordinal))
            {
                states.Add(selectionState);
            }
        }

        if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out object? expandCollapsePatternObject))
        {
            string? expandState = ((ExpandCollapsePattern)expandCollapsePatternObject).Current.ExpandCollapseState switch
            {
                ExpandCollapseState.Expanded => "موسع",
                ExpandCollapseState.Collapsed => "مطوي",
                ExpandCollapseState.PartiallyExpanded => "موسع جزئيا",
                ExpandCollapseState.LeafNode => "بدون عناصر فرعية",
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(expandState))
            {
                states.Add(expandState);
            }
        }

        if (IsLikelyBrowserContext(element))
        {
            foreach (string browserState in ResolveBrowserSpecificStates(element))
            {
                if (!states.Contains(browserState, StringComparer.Ordinal))
                {
                    states.Add(browserState);
                }
            }
        }

        string itemStatus = NormalizeMetadataValue(element.Current.ItemStatus);
        if (!string.IsNullOrWhiteSpace(itemStatus))
        {
            states.Add(itemStatus);
        }

        if (!element.Current.IsEnabled)
        {
            states.Add("معطل");
        }

        if (states.Count == 0)
        {
            return null;
        }

        return string.Join("، ", states.Distinct(StringComparer.Ordinal));
    }

    private static IEnumerable<string> ResolveBrowserSpecificStates(AutomationElement element)
    {
        List<string> states = [];
        string helpText = (element.Current.HelpText ?? string.Empty).ToLowerInvariant();
        string itemStatus = (element.Current.ItemStatus ?? string.Empty).ToLowerInvariant();
        string itemType = (element.Current.ItemType ?? string.Empty).ToLowerInvariant();
        string localizedRole = (element.Current.LocalizedControlType ?? string.Empty).ToLowerInvariant();

        if (ContainsAny(helpText, itemStatus, itemType, "required", "obligatoire", "مطلوب"))
        {
            states.Add("مطلوب");
        }

        if (ContainsAny(helpText, itemStatus, itemType, "invalid", "error", "erreur", "غير صالح", "خطأ"))
        {
            states.Add("غير صالح");
        }

        if (ContainsAny(helpText, itemStatus, localizedRole, "current", "actuel", "الحالي"))
        {
            states.Add("حالي");
        }

        if (ContainsAny(helpText, itemStatus, "visited"))
        {
            states.Add("تمت زيارته");
        }

        if (ContainsAny(helpText, itemStatus, "busy", "loading", "chargement", "جار"))
        {
            states.Add("قيد التحديث");
        }

        return states;
    }

    private static bool ContainsAny(string first, string second, params string[] needles)
    {
        foreach (string needle in needles)
        {
            if ((!string.IsNullOrWhiteSpace(first) && first.Contains(needle, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(second) && second.Contains(needle, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static string? NormalizeMetadataValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
