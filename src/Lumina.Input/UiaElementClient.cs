using System.Windows.Automation;

namespace Lumina.Input;

internal sealed class UiaElementClient
{
    private string? _role;
    private string? _semanticRole;
    private string? _process;
    private string? _name;
    private string? _stateSummary;
    private string? _value;
    private string? _runtimeId;
    private string? _webSummary;
    private bool? _browserContext;

    public UiaElementClient(AutomationElement? element)
    {
        Element = element;
    }

    public AutomationElement? Element { get; }

    public bool Exists => Element is not null;

    public bool BrowserContext => _browserContext ??= Element is not null && FocusSnapshotReader.IsBrowserContext(Element);

    public string Role => _role ??= Element is null ? "none" : FocusSnapshotReader.ResolveRole(Element);

    public string SemanticRole => _semanticRole ??=
        Element is null
            ? "none"
            : BrowserContext
                ? FocusSnapshotReader.ResolveWebSemanticRole(Element)
                : "none";

    public string Process => _process ??= Element is null ? "none" : FocusSnapshotReader.ResolveProcessName(Element);

    public string Name => _name ??= Element is null ? "none" : FocusSnapshotReader.ResolveName(Element);

    public string? StateSummary => _stateSummary ??= Element is null ? null : FocusSnapshotReader.ResolveStateSummary(Element);

    public string Value => _value ??= Element is null ? string.Empty : FocusSnapshotReader.TryReadValue(Element);

    public string RuntimeId => _runtimeId ??= Element is null ? string.Empty : ResolveRuntimeId(Element);

    public string WebSummary => _webSummary ??= Element is null ? string.Empty : FocusSnapshotReader.BuildWebSummary(Element);

    public bool IsDocumentRole => Element is not null && string.Equals(Role, "document", StringComparison.Ordinal);

    public bool IsBrowserEditFocused => Element is not null && BrowserContext && SemanticRole == "web_edit" && !IsDocumentRole;

    public static UiaElementClient FromElement(AutomationElement? element) => new(element);

    public static UiaElementClient ForFocusedElement() => new(FocusSnapshotReader.GetFocusedElement());

    private static string ResolveRuntimeId(AutomationElement element)
    {
        try
        {
            int[]? runtimeId = element.GetRuntimeId();
            return runtimeId is null ? string.Empty : string.Join("-", runtimeId);
        }
        catch
        {
            return string.Empty;
        }
    }
}
