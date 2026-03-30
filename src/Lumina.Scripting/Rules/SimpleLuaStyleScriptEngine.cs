using Lumina.Core.Abstractions;
using Lumina.Core.Models;
using Lumina.Core.Services;
using NLua;
using System.Text;

namespace Lumina.Scripting.Rules;

public sealed class SimpleLuaStyleScriptEngine : IScriptEngine, IDisposable
{
    private readonly Dictionary<string, Lua> _luaStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LuaStateDiagnostics> _stateDiagnostics = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _scriptsDirectory;
    private readonly string _userScriptsDirectory;

    public SimpleLuaStyleScriptEngine()
    {
        _scriptsDirectory = ResolveScriptsDirectory();
        _userScriptsDirectory = ResolveUserScriptsDirectory();
    }

    public SpeechRequest Handle(ScreenEvent screenEvent)
    {
        try
        {
            string normalizedProcessName = NormalizeProcessName(screenEvent.Node.SourceProcess);
            Lua lua = GetOrCreateState(normalizedProcessName);
            LuaStateDiagnostics diagnostics = GetStateDiagnostics(normalizedProcessName);

            LuaTable eventTable = BuildEventTable(lua, screenEvent);
            object[]? results = lua.GetFunction("on_focus_changed")?.Call(eventTable);

            if (results is not null &&
                results.Length > 0 &&
                results[0] is LuaTable table)
            {
                string action = table["action"]?.ToString() ?? "none";
                string text = table["text"]?.ToString() ?? string.Empty;
                LogLuaResult(screenEvent, diagnostics, action, text);
                if (action == "speak" && IsCorruptedLuaSpeech(text))
                {
                    ErrorLogger.LogWarning(
                        nameof(SimpleLuaStyleScriptEngine),
                        BuildCorruptedSpeechMessage(screenEvent, diagnostics, text));
                    return BuildFallbackSpeech(screenEvent);
                }

                return new SpeechRequest(
                    Text: action == "speak" ? text : string.Empty,
                    Priority: screenEvent.Priority,
                    Interrupt: true);
            }

            return BuildFallbackSpeech(screenEvent);
        }
        catch (Exception exception)
        {
            ErrorLogger.LogError(
                source: nameof(SimpleLuaStyleScriptEngine),
                message: "فشل تنفيذ سكربت Lua، وتم استخدام النطق الاحتياطي.",
                exception: exception,
                context: new
                {
                    screenEvent.EventType,
                    Process = screenEvent.Node.SourceProcess,
                    screenEvent.Node.Name,
                    screenEvent.Node.Role,
                    screenEvent.Node.SemanticRole
                });
            return BuildFallbackSpeech(screenEvent);
        }
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
        if (_luaStates.TryGetValue(processName, out Lua? existingLua))
        {
            return existingLua;
        }

        Lua lua = new();
        List<ScriptFileDiagnostic> loadedScripts = [];
        int loadOrder = 0;

        foreach (string scriptPath in EnumerateScriptLoadOrder(processName))
        {
            if (File.Exists(scriptPath))
            {
                loadOrder++;
                ScriptFileDiagnostic fileDiagnostic = InspectScriptFile(scriptPath, loadOrder);
                loadedScripts.Add(fileDiagnostic);
                ErrorLogger.LogVerbose(
                    nameof(SimpleLuaStyleScriptEngine),
                    $"Lua script load prepare: process={processName}, order={fileDiagnostic.Order}, path={fileDiagnostic.Path}, size={fileDiagnostic.SizeBytes}, bom={fileDiagnostic.ByteOrderMark}, utf8Valid={fileDiagnostic.IsUtf8Valid}, firstBytes={fileDiagnostic.FirstBytesHex}.");

                if (!fileDiagnostic.IsUtf8Valid)
                {
                    ErrorLogger.LogWarning(
                        nameof(SimpleLuaStyleScriptEngine),
                        $"تم تجاهل سكربت Lua غير UTF-8 للتطبيق {processName}. احفظ السكربت بترميز UTF-8 ثم أعد المحاولة. path={scriptPath}");
                    continue;
                }

                ExecuteScriptFile(lua, scriptPath);
            }
        }

        _luaStates[processName] = lua;
        _stateDiagnostics[processName] = new LuaStateDiagnostics(processName, loadedScripts);

        if (loadedScripts.Count == 0)
        {
            ErrorLogger.LogWarning(
                nameof(SimpleLuaStyleScriptEngine),
                $"لم يتم العثور على أي سكربتات Lua للتطبيق {processName}. سيتم استخدام السلوك الاحتياطي فقط عند الحاجة.");
        }
        else
        {
            ErrorLogger.LogInfo(
                nameof(SimpleLuaStyleScriptEngine),
                $"تم تحميل {loadedScripts.Count} سكربت Lua للتطبيق {processName}: {string.Join(" -> ", loadedScripts.Select(script => Path.GetFileName(script.Path)))}");
        }

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

    private LuaStateDiagnostics GetStateDiagnostics(string normalizedProcessName)
    {
        if (_stateDiagnostics.TryGetValue(normalizedProcessName, out LuaStateDiagnostics? diagnostics))
        {
            return diagnostics;
        }

        return new LuaStateDiagnostics(normalizedProcessName, []);
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

        if (ShouldIncludeValueInFocusSpeech(node))
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

    private static bool ShouldIncludeValueInFocusSpeech(AccessibleNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Value))
        {
            return false;
        }

        if (node.Role is not "edit" and not "document" &&
            node.SemanticRole is not "web_edit" and not "web_document")
        {
            return true;
        }

        if (node.Value.Contains('\n') || node.Value.Contains('\r'))
        {
            return false;
        }

        return node.Value.Length <= 120;
    }

    private static bool IsCorruptedLuaSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (text.Contains("????", StringComparison.Ordinal))
        {
            return true;
        }

        int questionMarks = text.Count(character => character == '?');
        if (questionMarks < 3)
        {
            return false;
        }

        bool hasArabic = text.Any(character => character is >= '\u0600' and <= '\u06FF');
        return !hasArabic && questionMarks >= Math.Max(text.Length / 5, 3);
    }

    private static string NormalizeProcessName(string processName) =>
        string.IsNullOrWhiteSpace(processName)
            ? "default"
            : processName.ToLowerInvariant();

    private static ScriptFileDiagnostic InspectScriptFile(string scriptPath, int order)
    {
        byte[] bytes = File.ReadAllBytes(scriptPath);
        return new ScriptFileDiagnostic(
            Order: order,
            Path: scriptPath,
            SizeBytes: bytes.LongLength,
            ByteOrderMark: DetectByteOrderMark(bytes),
            IsUtf8Valid: IsValidUtf8(bytes),
            FirstBytesHex: FormatFirstBytes(bytes));
    }

    private static void ExecuteScriptFile(Lua lua, string scriptPath)
    {
        _ = lua.DoFile(scriptPath);
    }

    private static string DetectByteOrderMark(byte[] bytes)
    {
        if (bytes.Length >= 3 &&
            bytes[0] == 0xEF &&
            bytes[1] == 0xBB &&
            bytes[2] == 0xBF)
        {
            return "UTF-8 BOM";
        }

        if (bytes.Length >= 2 &&
            bytes[0] == 0xFF &&
            bytes[1] == 0xFE)
        {
            return "UTF-16 LE BOM";
        }

        if (bytes.Length >= 2 &&
            bytes[0] == 0xFE &&
            bytes[1] == 0xFF)
        {
            return "UTF-16 BE BOM";
        }

        return "none";
    }

    private static bool IsValidUtf8(byte[] bytes)
    {
        try
        {
            Encoding utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            _ = utf8.GetString(bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatFirstBytes(byte[] bytes)
    {
        int count = Math.Min(bytes.Length, 16);
        if (count == 0)
        {
            return "empty";
        }

        return Convert.ToHexString(bytes.AsSpan(0, count));
    }

    private static void LogLuaResult(ScreenEvent screenEvent, LuaStateDiagnostics diagnostics, string action, string text)
    {
        if (action != "speak")
        {
            ErrorLogger.LogVerbose(
                nameof(SimpleLuaStyleScriptEngine),
                $"Lua result: process={screenEvent.Node.SourceProcess}, event={screenEvent.EventType}, action={action}, textLength={text.Length}, scripts={diagnostics.DescribeLoadedScripts()}.");
            return;
        }

        ErrorLogger.LogVerbose(
            nameof(SimpleLuaStyleScriptEngine),
            $"Lua speak result: process={screenEvent.Node.SourceProcess}, event={screenEvent.EventType}, role={screenEvent.Node.Role}, semanticRole={screenEvent.Node.SemanticRole}, rawText={QuoteForLog(text)}, textLength={text.Length}, hasArabic={ContainsArabic(text)}, questionMarks={CountQuestionMarks(text)}, sourceHint={BuildSpeechSourceHint(screenEvent, text)}, scripts={diagnostics.DescribeLoadedScripts()}.");
    }

    private static string BuildCorruptedSpeechMessage(ScreenEvent screenEvent, LuaStateDiagnostics diagnostics, string text)
    {
        return $"تم تجاهل نص Lua مشوه للتطبيق {screenEvent.Node.SourceProcess} والرجوع إلى النطق الاحتياطي. rawText={QuoteForLog(text)}. sourceHint={BuildSpeechSourceHint(screenEvent, text)}. event={screenEvent.EventType}. role={screenEvent.Node.Role}. semanticRole={screenEvent.Node.SemanticRole}. scripts={diagnostics.DescribeLoadedScripts()}.";
    }

    private static string BuildSpeechSourceHint(ScreenEvent screenEvent, string text)
    {
        List<string> matches = [];

        if (ContainsArabic(text))
        {
            matches.Add("contains_arabic");
        }

        if (ContainsValue(text, screenEvent.Node.Name))
        {
            matches.Add("includes_event_name");
        }

        if (ContainsValue(text, screenEvent.Node.Value))
        {
            matches.Add("includes_event_value");
        }

        if (matches.Count == 0)
        {
            matches.Add("likely_literal_or_transformed_text");
        }

        return string.Join(",", matches);
    }

    private static bool ContainsValue(string text, string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        text.Contains(value, StringComparison.Ordinal);

    private static bool ContainsArabic(string text) =>
        text.Any(character => character is >= '\u0600' and <= '\u06FF');

    private static int CountQuestionMarks(string text) =>
        text.Count(character => character == '?');

    private static string QuoteForLog(string value) =>
        "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private static string BuildLiveRegionSpeech(AccessibleNode node)
    {
        List<string> parts = [];

        string name = NormalizeLiveSpeechPart(node.Name);
        string value = NormalizeLiveSpeechPart(node.Value);
        string state = NormalizeLiveSpeechState(node.StateSummary);

        if (IsUsefulLiveSpeechPart(name))
        {
            parts.Add(name);
        }

        if (IsUsefulLiveSpeechPart(value) &&
            !string.Equals(value, name, StringComparison.Ordinal))
        {
            parts.Add(value);
        }

        if (IsUsefulLiveSpeechPart(state))
        {
            parts.Add(state);
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

    private static bool IsUsefulLiveSpeechPart(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length > 1 &&
        value != "Unnamed" &&
        value.Any(char.IsLetterOrDigit);

    private static string NormalizeLiveSpeechPart(string? value) =>
        (value ?? string.Empty)
            .Replace('\u00A0', ' ')
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

    private static string NormalizeLiveSpeechState(string? stateSummary)
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

    private sealed record ScriptFileDiagnostic(
        int Order,
        string Path,
        long SizeBytes,
        string ByteOrderMark,
        bool IsUtf8Valid,
        string FirstBytesHex);

    private sealed record LuaStateDiagnostics(
        string ProcessName,
        IReadOnlyList<ScriptFileDiagnostic> LoadedScripts)
    {
        public string DescribeLoadedScripts()
        {
            if (LoadedScripts.Count == 0)
            {
                return "none";
            }

            return string.Join(
                " | ",
                LoadedScripts.Select(script =>
                    $"#{script.Order}:{Path.GetFileName(script.Path)}[bom={script.ByteOrderMark},utf8={script.IsUtf8Valid},bytes={script.FirstBytesHex}]"));
        }
    }
}
