using System.Globalization;
using System.Text;

namespace RamosPartGenerator.Desktop;

internal static class AppLog
{
    private static readonly object Sync = new();

    public static string LogDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RamosPartGenerator",
            "logs");

    public static string CurrentLogPath => Path.Combine(LogDirectory, $"{DateTime.Now:yyyyMMdd}.log");

    public static void Info(string eventName, params (string Key, string? Value)[] details)
    {
        Write("INFO", eventName, null, details);
    }

    public static void Error(string eventName, Exception exception, params (string Key, string? Value)[] details)
    {
        Write("ERROR", eventName, exception, details);
    }

    private static void Write(string level, string eventName, Exception? exception, params (string Key, string? Value)[] details)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var line = BuildLine(level, eventName, exception, details);

            lock (Sync)
            {
                File.AppendAllText(CurrentLogPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never block normal program execution.
        }
    }

    private static string BuildLine(string level, string eventName, Exception? exception, IEnumerable<(string Key, string? Value)> details)
    {
        var builder = new StringBuilder();
        builder.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture));
        builder.Append(" [").Append(level).Append("] ");
        builder.Append(eventName);

        foreach (var (key, value) in details)
        {
            AppendField(builder, key, value);
        }

        if (exception is not null)
        {
            AppendField(builder, "exceptionType", exception.GetType().Name);
            AppendField(builder, "exceptionMessage", exception.Message);
            AppendField(builder, "exception", exception.ToString());
        }

        return builder.ToString();
    }

    private static void AppendField(StringBuilder builder, string key, string? value)
    {
        builder.Append(' ');
        builder.Append(key);
        builder.Append("=\"");
        builder.Append(Escape(value));
        builder.Append('"');
    }

    private static string Escape(string? value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
