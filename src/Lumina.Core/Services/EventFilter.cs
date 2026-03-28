using Lumina.Core.Models;

namespace Lumina.Core.Services;

public sealed class EventFilter
{
    private ScreenEvent? _lastEvent;
    private DateTimeOffset _lastTimestampUtc = DateTimeOffset.MinValue;
    private readonly Dictionary<string, DateTimeOffset> _recentLiveEventKeys = new(StringComparer.Ordinal);

    public bool ShouldProcess(ScreenEvent screenEvent)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (screenEvent.EventType is "liveRegionChanged" or "liveTextChanged")
        {
            if (!ShouldProcessLiveEvent(screenEvent, now))
            {
                return false;
            }
        }

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

    private bool ShouldProcessLiveEvent(ScreenEvent screenEvent, DateTimeOffset now)
    {
        string liveText = BuildLiveEventText(screenEvent.Node);
        if (string.IsNullOrWhiteSpace(liveText) || liveText.Length <= 1)
        {
            return false;
        }

        string liveKey = string.Join(
            "|",
            screenEvent.EventType,
            screenEvent.Node.SourceProcess,
            screenEvent.Node.SemanticRole ?? screenEvent.Node.Role,
            liveText);

        RemoveExpiredLiveKeys(now);

        if (_recentLiveEventKeys.TryGetValue(liveKey, out DateTimeOffset timestamp) &&
            now - timestamp < TimeSpan.FromSeconds(2))
        {
            return false;
        }

        _recentLiveEventKeys[liveKey] = now;
        return true;
    }

    private void RemoveExpiredLiveKeys(DateTimeOffset now)
    {
        string[] expiredKeys = _recentLiveEventKeys
            .Where(pair => now - pair.Value >= TimeSpan.FromSeconds(5))
            .Select(pair => pair.Key)
            .ToArray();

        foreach (string expiredKey in expiredKeys)
        {
            _recentLiveEventKeys.Remove(expiredKey);
        }
    }

    private static string BuildLiveEventText(AccessibleNode node)
    {
        List<string> parts = [];

        if (!string.IsNullOrWhiteSpace(node.Name) && node.Name != "Unnamed")
        {
            parts.Add(node.Name.Trim());
        }

        if (!string.IsNullOrWhiteSpace(node.Value) &&
            !string.Equals(node.Value, node.Name, StringComparison.Ordinal))
        {
            parts.Add(node.Value.Trim());
        }

        if (!string.IsNullOrWhiteSpace(node.StateSummary))
        {
            parts.Add(node.StateSummary.Trim());
        }

        return string.Join(" | ", parts.Distinct(StringComparer.Ordinal));
    }
}
