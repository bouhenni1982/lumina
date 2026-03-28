namespace Lumina.Core.Models;

public sealed record ScreenEvent(
    string EventType,
    AccessibleNode Node,
    bool UserInitiated,
    int Priority);
