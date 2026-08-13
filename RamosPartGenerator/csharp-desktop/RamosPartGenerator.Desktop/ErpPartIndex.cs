using System.Text.Json;

namespace RamosPartGenerator.Desktop;

/// <summary>
/// 중복 판정용 파트 인덱스. ERP 업로드 스냅샷과, PGM이 Export한 파트 원장을 합쳐서 확인한다.
/// ERP 재업로드 없이도 "이미 내보낸 파트"를 등록됨으로 잡아 준다.
/// </summary>
internal sealed class ErpPartIndex
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private HashSet<string> _codes = new(StringComparer.Ordinal);
    private HashSet<string> _exported = new(StringComparer.Ordinal);

    public string? SourceFileName { get; private set; }
    public DateTime? UploadedAt { get; private set; }
    public int Count => _codes.Count;
    public int ExportedCount => _exported.Count;
    public bool HasData => UploadedAt is not null || _exported.Count > 0;

    private static string CachePath => Path.Combine(AppContext.BaseDirectory, "erp-part-cache.json");
    private static string ExportedCachePath => Path.Combine(AppContext.BaseDirectory, "pgm-exported-cache.json");

    public bool Contains(string partCode)
    {
        var code = partCode.Trim();
        return _codes.Contains(code) || _exported.Contains(code);
    }

    public void Load()
    {
        LoadErpCache();
        LoadExportedCache();
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

    public void RecordExported(IEnumerable<string> partCodes)
    {
        var added = false;
        foreach (var raw in partCodes)
        {
            var code = (raw ?? string.Empty).Trim();
            if (code.Length > 0 && _exported.Add(code))
            {
                added = true;
            }
        }

        if (added)
        {
            File.WriteAllText(ExportedCachePath, JsonSerializer.Serialize(_exported.ToArray(), JsonOptions));
        }
    }

    private void LoadErpCache()
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

    private void LoadExportedCache()
    {
        try
        {
            if (!File.Exists(ExportedCachePath))
            {
                return;
            }

            var codes = JsonSerializer.Deserialize<string[]>(File.ReadAllText(ExportedCachePath), JsonOptions);
            if (codes is null)
            {
                return;
            }

            _exported = new HashSet<string>(
                codes.Select(code => code.Trim()).Where(code => code.Length > 0),
                StringComparer.Ordinal);
        }
        catch (Exception exception)
        {
            AppLog.Error("ErpPartIndex.LoadExportedFailed", exception, ("cachePath", ExportedCachePath));
        }
    }

    private sealed record CacheModel(string? SourceFileName, DateTime? UploadedAt, string[]? Codes);
}
