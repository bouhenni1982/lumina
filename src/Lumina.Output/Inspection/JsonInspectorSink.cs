using System.IO;
using System.Text.Json;
using Lumina.Core.Abstractions;
using Lumina.Core.Models;

namespace Lumina.Output.Inspection;

public sealed class JsonInspectorSink : IInspectorSink
{
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private readonly object _sync = new();
    public bool IsEnabled { get; private set; } = true;

    public JsonInspectorSink()
    {
        _logDirectory = Path.Combine(AppContext.BaseDirectory, "inspector");
        Directory.CreateDirectory(_logDirectory);
        _logFilePath = Path.Combine(_logDirectory, "focus-events.jsonl");
    }

    public void Record(ScreenEvent screenEvent, SpeechRequest speechRequest)
    {
        if (!IsEnabled)
        {
            return;
        }

        var payload = new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            eventType = screenEvent.EventType,
            node = new
            {
                screenEvent.Node.Id,
                screenEvent.Node.SourceApi,
                screenEvent.Node.Name,
                screenEvent.Node.Role,
                screenEvent.Node.SemanticRole,
                screenEvent.Node.Value,
                screenEvent.Node.ShortcutKey,
                screenEvent.Node.StateSummary,
                screenEvent.Node.Hint,
                screenEvent.Node.ContextKind,
                screenEvent.Node.SourceProcess
            },
            speech = new
            {
                speechRequest.Text,
                speechRequest.Priority,
                speechRequest.Interrupt
            }
        };

        string json = JsonSerializer.Serialize(payload);
        lock (_sync)
        {
            File.AppendAllText(_logFilePath, json + Environment.NewLine);
        }

        Console.WriteLine($"[Inspector] {screenEvent.Node.SourceProcess} | {screenEvent.Node.Role} | {screenEvent.Node.Name}");
    }

    public void Toggle()
    {
        IsEnabled = !IsEnabled;
        Console.WriteLine(IsEnabled ? "[Inspector] enabled" : "[Inspector] disabled");
    }

    public void Dispose()
    {
    }
}
