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

        if (IsUsefulLivePart(node.Name))
        {
            parts.Add(NormalizeLivePart(node.Name));
        }

        if (IsUsefulLivePart(node.Value) &&
            !string.Equals(NormalizeLivePart(node.Value), NormalizeLivePart(node.Name), StringComparison.Ordinal))
        {
            parts.Add(NormalizeLivePart(node.Value));
        }

        string stateSummary = NormalizeLiveState(node.StateSummary);
        if (IsUsefulLivePart(stateSummary))
        {
            parts.Add(stateSummary);
        }

        return string.Join(" | ", parts.Distinct(StringComparer.Ordinal));
    }

    private static bool IsUsefulLivePart(string? value)
    {
        string normalized = NormalizeLivePart(value);
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length <= 1)
        {
            return false;
        }

        return normalized is not "unnamed" and not "unknown" &&
               normalized.Any(char.IsLetterOrDigit);
    }

    private static string NormalizeLivePart(string? value) =>
        (value ?? string.Empty)
            .Replace('\u00A0', ' ')
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

    private static string NormalizeLiveState(string? stateSummary)
    {
        if (string.IsNullOrWhiteSpace(stateSummary))
        {
            return string.Empty;
        }

        string[] noisyStates =
        [
            "حالي",
            "تمت زيارته",
            "قيد التحديث"
        ];

        string[] parts = stateSummary
            .Split('،', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !noisyStates.Contains(part, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return string.Join("، ", parts);
    }
}
