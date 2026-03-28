namespace Lumina.Core.Models;

public sealed record AccessibleNode(
    string Id,
    string SourceApi,
    string Name,
    string Role,
    string? SemanticRole,
    string? Value,
    string? ShortcutKey,
    string? StateSummary,
    string? Hint,
    string? ContextKind,
    string SourceProcess,
    DateTimeOffset TimestampUtc);
