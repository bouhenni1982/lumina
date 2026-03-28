using Lumina.Core.Models;

namespace Lumina.Core.Services;

public sealed class EventFilter
{
    private ScreenEvent? _lastEvent;
    private DateTimeOffset _lastTimestampUtc = DateTimeOffset.MinValue;

    public bool ShouldProcess(ScreenEvent screenEvent)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (_lastEvent is not null &&
            _lastEvent.EventType == screenEvent.EventType &&
            _lastEvent.Node.Name == screenEvent.Node.Name &&
            _lastEvent.Node.Role == screenEvent.Node.Role &&
            now - _lastTimestampUtc < TimeSpan.FromMilliseconds(120))
        {
            return false;
        }

        _lastEvent = screenEvent;
        _lastTimestampUtc = now;
        return true;
    }
}
