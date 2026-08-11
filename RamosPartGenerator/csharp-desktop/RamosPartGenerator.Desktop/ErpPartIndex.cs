using System.Text.Json;

namespace RamosPartGenerator.Desktop;

internal sealed class ErpPartIndex
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private HashSet<string> _codes = new(StringComparer.Ordinal);

    public string? SourceFileName { get; private set; }
    public DateTime? UploadedAt { get; private set; }
    public int Count => _codes.Count;
    public bool HasData => UploadedAt is not null;

    private static string CachePath => Path.Combine(AppContext.BaseDirectory, "erp-part-cache.json");

    public bool Contains(string partCode)
    {
        return _codes.Contains(partCode.Trim());
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(CachePath))
            {
                return;
            }

            var cache = JsonSerializer.Deserialize<CacheModel>(File.ReadAllText(CachePath), JsonOptions);
            if (cache?.Codes is null)
            {
                return;
            }

            _codes = new HashSet<string>(
                cache.Codes.Select(code => code.Trim()).Where(code => code.Length > 0),
                StringComparer.Ordinal);
            SourceFileName = cache.SourceFileName;
            UploadedAt = cache.UploadedAt;
        }
        catch (Exception exception)
        {
            AppLog.Error("ErpPartIndex.LoadFailed", exception, ("cachePath", CachePath));
        }
    }

    public void Update(string sourceFileName, IReadOnlyCollection<string> codes)
    {
        _codes = new HashSet<string>(codes, StringComparer.Ordinal);
        SourceFileName = sourceFileName;
        UploadedAt = DateTime.Now;
        File.WriteAllText(
            CachePath,
            JsonSerializer.Serialize(new CacheModel(sourceFileName, UploadedAt, _codes.ToArray()), JsonOptions));
    }

    private sealed record CacheModel(string? SourceFileName, DateTime? UploadedAt, string[]? Codes);
}
