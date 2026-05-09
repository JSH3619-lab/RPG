using System.Globalization;
using System.Text;

namespace RamosPartGenerator.Desktop;

internal static class AppLog
{
    private const int RetentionDays = 30;
    private static readonly object Sync = new();
    private static bool _retentionChecked;

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
                DeleteExpiredLogFiles();
                File.AppendAllText(CurrentLogPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never block normal program execution.
        }
    }

    private static void DeleteExpiredLogFiles()
    {
        if (_retentionChecked)
        {
            return;
        }

        _retentionChecked = true;
        var cutoffDate = DateTime.Today.AddDays(-RetentionDays);

        foreach (var path in Directory.EnumerateFiles(LogDirectory, "*.log"))
        {
            var logDate = GetLogDate(path);
            if (logDate < cutoffDate)
            {
                File.Delete(path);
            }
        }
    }

    private static DateTime GetLogDate(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        return DateTime.TryParseExact(
            fileName,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var logDate)
            ? logDate.Date
            : File.GetLastWriteTime(path).Date;
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
