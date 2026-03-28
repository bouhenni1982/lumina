using Lumina.Core.Abstractions;
using Lumina.Core.Models;
using NLua;

namespace Lumina.Scripting.Rules;

public sealed class SimpleLuaStyleScriptEngine : IScriptEngine, IDisposable
{
    private readonly Lua _lua = new();
    private readonly string _scriptPath;
    private bool _loaded;

    public SimpleLuaStyleScriptEngine()
    {
        _scriptPath = ResolveScriptPath();
    }

    public SpeechRequest Handle(ScreenEvent screenEvent)
    {
        EnsureScriptLoaded();

        LuaTable eventTable = BuildEventTable(screenEvent);
        object[]? results = _lua.GetFunction("on_focus_changed")?.Call(eventTable);

        if (results is not null &&
            results.Length > 0 &&
            results[0] is LuaTable table)
        {
            string action = table["action"]?.ToString() ?? "none";
            string text = table["text"]?.ToString() ?? string.Empty;

            return new SpeechRequest(
                Text: action == "speak" ? text : string.Empty,
                Priority: screenEvent.Priority,
                Interrupt: true);
        }

        return BuildFallbackSpeech(screenEvent);
    }

    public void Dispose()
    {
        _lua.Dispose();
    }

    private void EnsureScriptLoaded()
    {
        if (_loaded)
        {
            return;
        }

        if (File.Exists(_scriptPath))
        {
            _lua.DoFile(_scriptPath);
            _loaded = true;
            return;
        }

        _loaded = true;
    }

    private static string ResolveScriptPath()
    {
        string baseDirectory = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(baseDirectory, "scripts", "focus_profile.lua"),
            Path.Combine(Directory.GetCurrentDirectory(), "scripts", "focus_profile.lua"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "scripts", "focus_profile.lua")
        ];

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private LuaTable BuildEventTable(ScreenEvent screenEvent)
    {
        LuaTable eventTable = (LuaTable)_lua.DoString("return {}")[0]!;
        eventTable["type"] = screenEvent.EventType;
        eventTable["name"] = screenEvent.Node.Name;
        eventTable["role"] = screenEvent.Node.Role;
        eventTable["value"] = screenEvent.Node.Value;
        eventTable["hint"] = screenEvent.Node.Hint;
        eventTable["source_api"] = screenEvent.Node.SourceApi;
        eventTable["process"] = screenEvent.Node.SourceProcess;
        return eventTable;
    }

    private static SpeechRequest BuildFallbackSpeech(ScreenEvent screenEvent)
    {
        AccessibleNode node = screenEvent.Node;

        string text = node.Role switch
        {
            "button" => $"زر {node.Name}",
            "edit" => $"حقل تحرير {node.Name}",
            "document" => $"مستند {node.Name}",
            _ => $"{node.Role} {node.Name}"
        };

        if (!string.IsNullOrWhiteSpace(node.Value))
        {
            text = $"{text}. القيمة {node.Value}";
        }

        return new SpeechRequest(
            Text: text,
            Priority: screenEvent.Priority,
            Interrupt: true);
    }
}
