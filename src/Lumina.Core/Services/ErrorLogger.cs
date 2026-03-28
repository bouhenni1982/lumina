using System.Text;
using System.Text.Json;

namespace Lumina.Core.Services;

public static class ErrorLogger
{
    private static readonly object Sync = new();
    private static readonly string LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
    private static readonly string TextLogPath = Path.Combine(LogDirectory, "lumina.log");
    private static readonly string ErrorLogPath = Path.Combine(LogDirectory, "errors.jsonl");

    public static string GetLogDirectory()
    {
        EnsureLogDirectory();
        return LogDirectory;
    }

    public static void LogInfo(string source, string message) => WriteTextLog("INFO", source, message);

    public static void LogWarning(string source, string message) => WriteTextLog("WARN", source, message);

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
}
