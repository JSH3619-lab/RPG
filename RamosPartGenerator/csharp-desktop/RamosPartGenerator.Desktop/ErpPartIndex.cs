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
    private DateTime _erpCacheStamp;
    private DateTime _exportedCacheStamp;

    public string? SourceFileName { get; private set; }
    public DateTime? UploadedAt { get; private set; }
    public int Count => _codes.Count;
    public int ExportedCount => _exported.Count;
    public bool HasData => UploadedAt is not null || _exported.Count > 0;

    private const string ErpCacheFile = "erp-part-cache.json";
    private const string ExportedCacheFile = "pgm-exported-cache.json";

    // 팀 공유 폴더의 exe 하나를 같이 실행하므로 exe 폴더에 두면 등록 데이터도 함께 공유된다.
    private static string DataDirectory => AppContext.BaseDirectory;
    private static string CachePath => Path.Combine(DataDirectory, ErpCacheFile);
    private static string ExportedCachePath => Path.Combine(DataDirectory, ExportedCacheFile);

    // v1.5.1까지의 사용자별 저장 위치. 실행 시 exe 폴더로 합쳐 준다.
    private static string LegacyDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RamosPartGenerator");

    public bool Contains(string partCode)
    {
        var code = partCode.Trim();
        return _codes.Contains(code) || _exported.Contains(code);
    }

    public void Load()
    {
        MigrateLegacyErpCache();
        Refresh();
        MigrateLegacyExportedCache();
    }

    /// <summary>다른 사람이 Export·ERP 업로드했을 수 있으므로 파일이 바뀐 경우에만 다시 읽는다.</summary>
    public void Refresh()
    {
        LoadErpCache();
        LoadExportedCache();
    }

    // exe 폴더에 ERP 스냅샷이 없으면 사용자 폴더의 스냅샷으로 1회 채운다.
    private static void MigrateLegacyErpCache()
    {
        try
        {
            var legacyPath = Path.Combine(LegacyDirectory, ErpCacheFile);
            if (!File.Exists(CachePath) && File.Exists(legacyPath))
            {
                File.Copy(legacyPath, CachePath);
            }
        }
        catch (Exception exception)
        {
            AppLog.Error("ErpPartIndex.MigrateFailed", exception, ("fileName", ErpCacheFile));
        }
    }

    // 사용자 폴더의 개인 Export 이력을 공용 이력에 합친다. 이미 있는 코드는 건너뛰므로 매번 실행해도 안전하다.
    private void MigrateLegacyExportedCache()
    {
        try
        {
            var legacyPath = Path.Combine(LegacyDirectory, ExportedCacheFile);
            if (File.Exists(legacyPath))
            {
                RecordExported(ReadCodes(legacyPath));
            }
        }
        catch (Exception exception)
        {
            AppLog.Error("ErpPartIndex.MigrateFailed", exception, ("fileName", ExportedCacheFile));
        }
    }

    public void Update(string sourceFileName, IReadOnlyCollection<string> codes)
    {
        _codes = new HashSet<string>(codes, StringComparer.Ordinal);
        SourceFileName = sourceFileName;
        UploadedAt = DateTime.Now;
        WriteAtomically(
            CachePath,
            JsonSerializer.Serialize(new CacheModel(sourceFileName, UploadedAt, _codes.ToArray()), JsonOptions));
        _erpCacheStamp = File.GetLastWriteTimeUtc(CachePath);
    }

    public void RecordExported(IEnumerable<string> partCodes)
    {
        // 앱을 켜 둔 사이 다른 사람이 Export했을 수 있다. 파일을 다시 읽어 합친 뒤 저장해야 상대 이력이 지워지지 않는다.
        var merged = File.Exists(ExportedCachePath)
            ? new HashSet<string>(ReadCodes(ExportedCachePath), StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);
        var countBefore = merged.Count;
        foreach (var raw in partCodes)
        {
            var code = (raw ?? string.Empty).Trim();
            if (code.Length > 0)
            {
                merged.Add(code);
            }
        }

        _exported.UnionWith(merged);
        if (merged.Count == countBefore)
        {
            return;
        }

        WriteAtomically(ExportedCachePath, JsonSerializer.Serialize(merged.ToArray(), JsonOptions));
        _exportedCacheStamp = File.GetLastWriteTimeUtc(ExportedCachePath);
    }

    // 저장 도중 네트워크가 끊겨도 원본이 깨지지 않도록 임시 파일에 쓴 뒤 교체한다.
    private static void WriteAtomically(string path, string content)
    {
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, content);
        File.Move(tempPath, path, overwrite: true);
    }

    private static IEnumerable<string> ReadCodes(string path)
    {
        var codes = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path), JsonOptions) ?? Array.Empty<string>();
        return codes.Select(code => code.Trim()).Where(code => code.Length > 0);
    }

    private void LoadErpCache()
    {
        try
        {
            var stamp = File.GetLastWriteTimeUtc(CachePath);
            if (!File.Exists(CachePath) || stamp == _erpCacheStamp)
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
            _erpCacheStamp = stamp;
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
            var stamp = File.GetLastWriteTimeUtc(ExportedCachePath);
            if (!File.Exists(ExportedCachePath) || stamp == _exportedCacheStamp)
            {
                return;
            }

            _exported.UnionWith(ReadCodes(ExportedCachePath));
            _exportedCacheStamp = stamp;
        }
        catch (Exception exception)
        {
            AppLog.Error("ErpPartIndex.LoadExportedFailed", exception, ("cachePath", ExportedCachePath));
        }
    }

    private sealed record CacheModel(string? SourceFileName, DateTime? UploadedAt, string[]? Codes);
}
