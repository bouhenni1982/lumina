namespace Lumina.Core.Models;

public sealed record AccessibleNode(
    string Id,
    string SourceApi,
    string Name,
    string Role,
    string? Value,
    string? Hint,
    string SourceProcess,
    DateTimeOffset TimestampUtc);
