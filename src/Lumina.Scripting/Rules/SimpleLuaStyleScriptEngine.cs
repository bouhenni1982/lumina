using Lumina.Core.Abstractions;
using Lumina.Core.Models;
using NLua;

namespace Lumina.Scripting.Rules;

public sealed class SimpleLuaStyleScriptEngine : IScriptEngine, IDisposable
{
    private readonly Dictionary<string, Lua> _luaStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _scriptsDirectory;
    private readonly string _userScriptsDirectory;

    public SimpleLuaStyleScriptEngine()
    {
        _scriptsDirectory = ResolveScriptsDirectory();
        _userScriptsDirectory = ResolveUserScriptsDirectory();
    }

    public SpeechRequest Handle(ScreenEvent screenEvent)
    {
        Lua lua = GetOrCreateState(screenEvent.Node.SourceProcess);

        LuaTable eventTable = BuildEventTable(lua, screenEvent);
        object[]? results = lua.GetFunction("on_focus_changed")?.Call(eventTable);

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
        foreach (Lua lua in _luaStates.Values)
        {
            lua.Dispose();
        }
    }

    private Lua GetOrCreateState(string processName)
    {
        string normalizedProcessName = string.IsNullOrWhiteSpace(processName)
            ? "default"
            : processName.ToLowerInvariant();

        if (_luaStates.TryGetValue(normalizedProcessName, out Lua? existingLua))
        {
            return existingLua;
        }

        Lua lua = new();
        foreach (string scriptPath in EnumerateScriptLoadOrder(normalizedProcessName))
        {
            if (File.Exists(scriptPath))
            {
                lua.DoFile(scriptPath);
            }
        }

        _luaStates[normalizedProcessName] = lua;
        return lua;
    }

    private static string ResolveScriptsDirectory()
    {
        string baseDirectory = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(baseDirectory, "scripts"),
            Path.Combine(Directory.GetCurrentDirectory(), "scripts"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "scripts")
        ];

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }

    private static string ResolveUserScriptsDirectory()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            return Path.Combine(localAppData, "Lumina", "scripts");
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "scripts", "user");
    }

    private IEnumerable<string> EnumerateScriptLoadOrder(string normalizedProcessName)
    {
        yield return Path.Combine(_scriptsDirectory, "focus_profile.lua");
        yield return Path.Combine(_scriptsDirectory, "apps", $"{normalizedProcessName}.lua");

        yield return Path.Combine(_scriptsDirectory, "user", "focus_profile.lua");
        yield return Path.Combine(_scriptsDirectory, "user", "apps", $"{normalizedProcessName}.lua");

        yield return Path.Combine(_userScriptsDirectory, "focus_profile.lua");
        yield return Path.Combine(_userScriptsDirectory, "apps", $"{normalizedProcessName}.lua");
    }

    private static LuaTable BuildEventTable(Lua lua, ScreenEvent screenEvent)
    {
        LuaTable eventTable = (LuaTable)lua.DoString("return {}")[0]!;
        eventTable["type"] = screenEvent.EventType;
        eventTable["name"] = screenEvent.Node.Name;
        eventTable["role"] = screenEvent.Node.Role;
        eventTable["value"] = screenEvent.Node.Value;
        eventTable["shortcut"] = screenEvent.Node.ShortcutKey;
        eventTable["state"] = screenEvent.Node.StateSummary;
        eventTable["hint"] = screenEvent.Node.Hint;
        eventTable["source_api"] = screenEvent.Node.SourceApi;
        eventTable["semantic_role"] = screenEvent.Node.SemanticRole;
        eventTable["context_kind"] = screenEvent.Node.ContextKind;
        eventTable["process"] = screenEvent.Node.SourceProcess;
        return eventTable;
    }

    private static SpeechRequest BuildFallbackSpeech(ScreenEvent screenEvent)
    {
        AccessibleNode node = screenEvent.Node;

        if (screenEvent.EventType is "liveRegionChanged" or "liveTextChanged")
        {
            string liveText = BuildLiveRegionSpeech(node);
            return new SpeechRequest(
                Text: liveText,
                Priority: screenEvent.Priority,
                Interrupt: screenEvent.Priority >= 100);
        }

        string text = node.Role switch
        {
            _ when node.ContextKind == "browser" && node.SemanticRole == "web_link" => $"رابط {node.Name}",
            _ when node.ContextKind == "browser" && node.SemanticRole == "web_heading" => $"عنوان ويب {node.Name}",
            _ when node.ContextKind == "browser" && node.SemanticRole == "web_document" => $"مستند ويب {node.Name}",
            _ when node.ContextKind == "browser" && node.SemanticRole == "web_edit" => $"حقل ويب {node.Name}",
            "button" => $"زر {node.Name}",
            "edit" => $"حقل تحرير {node.Name}",
            "document" => $"مستند {node.Name}",
            _ => $"{node.Role} {node.Name}"
        };

        if (!string.IsNullOrWhiteSpace(node.Value))
        {
            text = $"{text}. القيمة {node.Value}";
        }

        if (!string.IsNullOrWhiteSpace(node.StateSummary))
        {
            text = $"{text}. {node.StateSummary}";
        }

        if (!string.IsNullOrWhiteSpace(node.ShortcutKey))
        {
            text = $"{text}. اختصار {node.ShortcutKey}";
        }

        return new SpeechRequest(
            Text: text,
            Priority: screenEvent.Priority,
            Interrupt: true);
    }

    private static string BuildLiveRegionSpeech(AccessibleNode node)
    {
        List<string> parts = [];

        if (!string.IsNullOrWhiteSpace(node.Name) && node.Name != "Unnamed")
        {
            parts.Add(node.Name);
        }

        if (!string.IsNullOrWhiteSpace(node.Value) &&
            !string.Equals(node.Value, node.Name, StringComparison.Ordinal))
        {
            parts.Add(node.Value);
        }

        if (!string.IsNullOrWhiteSpace(node.StateSummary))
        {
            parts.Add(node.StateSummary);
        }

        if (parts.Count == 0)
        {
            string fallback = node.SemanticRole switch
            {
                "web_dialog" => "تم تحديث حوار ويب",
                "web_landmark" => "تم تحديث معلم في الصفحة",
                _ => "تم تحديث محتوى حي في الصفحة"
            };

            return fallback;
        }

        return string.Join(". ", parts.Distinct(StringComparer.Ordinal));
    }
}
