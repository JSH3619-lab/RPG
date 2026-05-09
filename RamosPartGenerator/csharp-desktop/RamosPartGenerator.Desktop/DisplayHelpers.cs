namespace RamosPartGenerator.Desktop;

internal static class DisplayHelpers
{
    public static string ExtractCode(string? rawValue)
    {
        var trimmed = (rawValue ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed) ||
            trimmed.Equals("(None)", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("(None) - ", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var separatorIndex = trimmed.IndexOf(" - ", StringComparison.Ordinal);
        return separatorIndex > -1 ? trimmed[..separatorIndex].Trim() : trimmed;
    }

    public static string ResolveDisplayValue(string? code, IEnumerable<string> options)
    {
        var normalizedCode = ExtractCode(code).ToUpperInvariant();
        if (string.IsNullOrEmpty(normalizedCode))
        {
            return string.Empty;
        }

        var matched = options.FirstOrDefault(option =>
            ExtractCode(option).Equals(normalizedCode, StringComparison.OrdinalIgnoreCase));

        if (matched is not null)
        {
            return matched;
        }

        return normalizedCode == "0" ? string.Empty : normalizedCode;
    }

    public static string ExtractModuleDramCode(string? rawValue)
    {
        var code = ExtractCode(rawValue);
        return code.Equals("A", StringComparison.OrdinalIgnoreCase) ? "4" : code;
    }
}
