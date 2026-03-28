using System.Text;
using System.Text.Json;

namespace Lumina.Core.Services;

public static class ErrorLogger
{
    public enum LogVerbosity
    {
        ErrorsOnly,
        Info,
        Verbose
    }

    private static readonly object Sync = new();
    private static readonly string LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
    private static readonly string TextLogPath = Path.Combine(LogDirectory, "lumina.log");
    private static readonly string ErrorLogPath = Path.Combine(LogDirectory, "errors.jsonl");
    private static LogVerbosity _verbosity = ResolveInitialVerbosity();

    public static string GetLogDirectory()
    {
        EnsureLogDirectory();
        return LogDirectory;
    }

    public static LogVerbosity GetVerbosity() => _verbosity;

    public static string GetStatusSummary() =>
        $"السجل {DescribeVerbosity(_verbosity)}. المجلد {GetLogDirectory()}";

    public static string CycleVerbosity()
    {
        _verbosity = _verbosity switch
        {
            LogVerbosity.ErrorsOnly => LogVerbosity.Info,
            LogVerbosity.Info => LogVerbosity.Verbose,
            _ => LogVerbosity.ErrorsOnly
        };

        LogInfo(nameof(ErrorLogger), $"تم تغيير مستوى السجل إلى {DescribeVerbosity(_verbosity)}.");
        return $"تم تغيير مستوى السجل إلى {DescribeVerbosity(_verbosity)}.";
    }

    public static string GetLatestErrorSummary()
    {
        EnsureLogDirectory();
        if (!File.Exists(ErrorLogPath))
        {
            return "لا يوجد ملف أخطاء بعد.";
        }

        string? lastLine = File.ReadLines(ErrorLogPath)
            .LastOrDefault(line => !string.IsNullOrWhiteSpace(line));
        if (string.IsNullOrWhiteSpace(lastLine))
        {
            return "لا يوجد خطأ مسجل حتى الآن.";
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(lastLine);
            JsonElement root = document.RootElement;

            string source = root.TryGetProperty("source", out JsonElement sourceElement)
                ? sourceElement.GetString() ?? "غير معروف"
                : "غير معروف";
            string message = root.TryGetProperty("message", out JsonElement messageElement)
                ? messageElement.GetString() ?? "بدون رسالة"
                : "بدون رسالة";
            string timestamp = root.TryGetProperty("timestampUtc", out JsonElement timestampElement)
                ? timestampElement.GetString() ?? "وقت غير معروف"
                : "وقت غير معروف";

            return $"آخر خطأ من {source}. {message}. التوقيت {timestamp}.";
        }
        catch (Exception exception)
        {
            LogError(nameof(ErrorLogger), "تعذر تحليل آخر خطأ مسجل.", exception);
            return "تعذر قراءة آخر خطأ من ملف السجل.";
        }
    }

    public static void LogInfo(string source, string message) => WriteTextLog("INFO", source, message);

    public static void LogWarning(string source, string message) => WriteTextLog("WARN", source, message);

    public static void LogVerbose(string source, string message) => WriteTextLog("VERBOSE", source, message);

    public static void LogError(string source, string message, Exception exception, object? context = null)
    {
        EnsureLogDirectory();

        WriteTextLog("ERROR", source, $"{message}{Environment.NewLine}{exception}");

        var payload = new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            level = "error",
            source,
            message,
            exception = new
            {
                type = exception.GetType().FullName,
                exception.Message,
                exception.StackTrace,
                innerException = exception.InnerException?.ToString()
            },
            context
        };

        string json = JsonSerializer.Serialize(payload);
        lock (Sync)
        {
            File.AppendAllText(ErrorLogPath, json + Environment.NewLine, Encoding.UTF8);
        }
    }

    private static void WriteTextLog(string level, string source, string message)
    {
        if (!ShouldWrite(level))
        {
            return;
        }

        EnsureLogDirectory();

        string line = $"{DateTimeOffset.UtcNow:O} [{level}] {source}: {message}{Environment.NewLine}";
        lock (Sync)
        {
            File.AppendAllText(TextLogPath, line, Encoding.UTF8);
        }
    }

    private static void EnsureLogDirectory()
    {
        lock (Sync)
        {
            Directory.CreateDirectory(LogDirectory);
        }
    }

    private static bool ShouldWrite(string level) =>
        level switch
        {
            "ERROR" => true,
            "WARN" => _verbosity is LogVerbosity.Info or LogVerbosity.Verbose,
            "INFO" => _verbosity is LogVerbosity.Info or LogVerbosity.Verbose,
            "VERBOSE" => _verbosity is LogVerbosity.Verbose,
            _ => true
        };

    private static string DescribeVerbosity(LogVerbosity verbosity) =>
        verbosity switch
        {
            LogVerbosity.ErrorsOnly => "أخطاء فقط",
            LogVerbosity.Info => "معلومات",
            LogVerbosity.Verbose => "تشخيص موسع",
            _ => "غير معروف"
        };

    private static LogVerbosity ResolveInitialVerbosity()
    {
        string? rawValue = Environment.GetEnvironmentVariable("LUMINA_LOG_LEVEL");
        return rawValue?.Trim().ToLowerInvariant() switch
        {
            "error" or "errors" => LogVerbosity.ErrorsOnly,
            "verbose" or "trace" or "debug" => LogVerbosity.Verbose,
            _ => LogVerbosity.Info
        };
    }
}
