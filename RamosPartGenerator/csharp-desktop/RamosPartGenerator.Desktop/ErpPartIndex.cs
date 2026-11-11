using System.Text.Json;
using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Desktop;

/// <summary>
/// 중복 판정용 파트 인덱스. ERP 업로드 스냅샷과 PGM이 Export한 파트 원장을 대조한다.
/// Export만 된 파트는 시각을 보고 "ERP 반영 대기"와 "누락 의심"으로 나눈다 (ErpRegistrationRules).
/// </summary>
internal sealed class ErpPartIndex
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private HashSet<string> _codes = new(StringComparer.Ordinal);
    private HashSet<string> _exported = new(StringComparer.Ordinal);
    private Dictionary<string, ExportLogEntry> _exportLog = new(StringComparer.Ordinal);
    private DateTime _erpCacheStamp;
    private DateTime _exportedCacheStamp;
    private DateTime _exportLogStamp;

    public string? SourceFileName { get; private set; }
    public DateTime? UploadedAt { get; private set; }
    // ERP 파일을 받은(저장한) 시각. 이전 버전 캐시에는 없어 업로드 시각으로 대신한다.
    public DateTime? SnapshotAt { get; private set; }
    public int Count => _codes.Count;
    public int ExportedCount => _exported.Count;
    public bool HasData => UploadedAt is not null || _exported.Count > 0;
    public bool IsStale => SnapshotAt is not null && DateTime.Now - SnapshotAt.Value > ErpRegistrationRules.StaleSnapshot;

    private const string ErpCacheFile = "erp-part-cache.json";
    // 코드 목록(string[])은 이전 버전과 같은 형식으로 유지하고, Export 시각 등은 별도 로그 파일에 둔다.
    private const string ExportedCacheFile = "pgm-exported-cache.json";
    private const string ExportLogFile = "pgm-export-log.json";

    // 팀 공유 폴더의 exe 하나를 같이 실행하므로 exe 폴더에 두면 등록 데이터도 함께 공유된다.
    private static string DataDirectory => AppContext.BaseDirectory;
    private static string CachePath => Path.Combine(DataDirectory, ErpCacheFile);
    private static string ExportedCachePath => Path.Combine(DataDirectory, ExportedCacheFile);
    private static string ExportLogPath => Path.Combine(DataDirectory, ExportLogFile);

    // v1.5.1까지의 사용자별 저장 위치. 실행 시 exe 폴더로 합쳐 준다.
    private static string LegacyDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RamosPartGenerator");

    public ErpRegistration GetStatus(string partCode)
    {
        var code = partCode.Trim();
        var exported = _exported.Contains(code);
        DateTime? exportedAt = exported && _exportLog.TryGetValue(code, out var entry) ? entry.ExportedAt : null;
        return ErpRegistrationRules.Classify(_codes.Contains(code), exported, exportedAt, UploadedAt is null ? null : SnapshotAt);
    }

    /// <summary>이미 등록된 것으로 보고 Export에서 제외할 파트인지 (ERP 등록됨 또는 ERP 반영 대기).</summary>
    public bool Contains(string partCode) => ErpRegistrationRules.TreatAsRegistered(GetStatus(partCode));

    /// <summary>Export 이력에 있지만 ERP에 없는 누락 의심 파트.</summary>
    public IReadOnlyList<(string Code, ExportLogEntry? Entry)> SuspectedMissing()
    {
        return _exported
            .Where(code => GetStatus(code) == ErpRegistration.SuspectedMissing)
            .Select(code => (code, _exportLog.TryGetValue(code, out var entry) ? entry : null))
            .ToArray();
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
        LoadExportLog();
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
                var merged = ReadExportedFile();
                var added = ReadCodes(legacyPath).Where(merged.Add).ToArray();
                if (added.Length > 0)
                {
                    WriteAtomically(ExportedCachePath, JsonSerializer.Serialize(merged.ToArray(), JsonOptions));
                    _exportedCacheStamp = File.GetLastWriteTimeUtc(ExportedCachePath);
                }

                _exported = merged;
            }
        }
        catch (Exception exception)
        {
            AppLog.Error("ErpPartIndex.MigrateFailed", exception, ("fileName", ExportedCacheFile));
        }
    }

    public void Update(string sourceFileName, IReadOnlyCollection<string> codes, DateTime snapshotAt)
    {
        _codes = new HashSet<string>(codes, StringComparer.Ordinal);
        SourceFileName = sourceFileName;
        UploadedAt = DateTime.Now;
        SnapshotAt = snapshotAt;
        WriteAtomically(
            CachePath,
            JsonSerializer.Serialize(new CacheModel(sourceFileName, UploadedAt, _codes.ToArray(), SnapshotAt), JsonOptions));
        _erpCacheStamp = File.GetLastWriteTimeUtc(CachePath);
    }

    public void RecordExported(IEnumerable<string> partCodes, string fileName)
    {
        var codes = partCodes.Select(raw => (raw ?? string.Empty).Trim()).Where(code => code.Length > 0).ToArray();

        // 로그(시각)를 코드 목록보다 먼저 쓴다 — 목록에만 있고 시각이 없으면 바로 누락 의심이 되므로.
        // 다시 Export한 파트(누락 의심 재포함 등)는 시각을 새로 고쳐 "ERP 반영 대기"로 돌린다.
        // ponytail: 두 사람이 같은 순간 Export하면 한쪽 로그 항목이 덮일 수 있다(잠금 없음). 그 파트는 누락 의심으로
        // 다시 포함되는 안전한 쪽으로 틀리므로 둔다. 잦아지면 파일 잠금을 추가한다.
        var log = ReadExportLogFile();
        var entry = new ExportLogEntry(DateTime.Now, Environment.MachineName, Path.GetFileName(fileName));
        foreach (var code in codes)
        {
            log[code] = entry;
        }

        WriteAtomically(ExportLogPath, JsonSerializer.Serialize(log, JsonOptions));
        _exportLog = log;
        _exportLogStamp = File.GetLastWriteTimeUtc(ExportLogPath);

        // 앱을 켜 둔 사이 다른 사람이 Export했을 수 있다. 파일을 다시 읽어 합친 뒤 저장해야 상대 이력이 지워지지 않는다.
        var merged = ReadExportedFile();
        var countBefore = merged.Count;
        merged.UnionWith(codes);
        if (merged.Count != countBefore)
        {
            WriteAtomically(ExportedCachePath, JsonSerializer.Serialize(merged.ToArray(), JsonOptions));
            _exportedCacheStamp = File.GetLastWriteTimeUtc(ExportedCachePath);
        }

        _exported = merged;
    }

    // 저장 도중 네트워크가 끊겨도 원본이 깨지지 않도록 임시 파일에 쓴 뒤 교체한다.
    // 임시 파일 이름은 매번 달리 — 두 사람이 동시에 저장할 때 같은 .tmp를 두고 충돌해 Export가 실패하지 않도록.
    private static void WriteAtomically(string path, string content)
    {
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, content);
        File.Move(tempPath, path, overwrite: true);
    }

    private static IEnumerable<string> ReadCodes(string path)
    {
        var codes = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path), JsonOptions) ?? Array.Empty<string>();
        return codes.Select(code => code.Trim()).Where(code => code.Length > 0);
    }

    private static HashSet<string> ReadExportedFile()
    {
        return File.Exists(ExportedCachePath)
            ? new HashSet<string>(ReadCodes(ExportedCachePath), StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);
    }

    private static Dictionary<string, ExportLogEntry> ReadExportLogFile()
    {
        var log = File.Exists(ExportLogPath)
            ? JsonSerializer.Deserialize<Dictionary<string, ExportLogEntry>>(File.ReadAllText(ExportLogPath), JsonOptions)
            : null;
        return new Dictionary<string, ExportLogEntry>(log ?? new(), StringComparer.Ordinal);
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
            SnapshotAt = cache.SnapshotAt ?? cache.UploadedAt;
            _erpCacheStamp = stamp;
        }
        catch (Exception exception)
        {
            AppLog.Error("ErpPartIndex.LoadFailed", exception, ("cachePath", CachePath));
        }
    }

    // 파일 전체로 교체한다 — 이력 파일에서 지운 잘못 만든 파트가 재시작 없이도 빠지도록.
    private void LoadExportedCache()
    {
        try
        {
            var stamp = File.GetLastWriteTimeUtc(ExportedCachePath);
            if (!File.Exists(ExportedCachePath) || stamp == _exportedCacheStamp)
            {
                return;
            }

            _exported = ReadExportedFile();
            _exportedCacheStamp = stamp;
        }
        catch (Exception exception)
        {
            AppLog.Error("ErpPartIndex.LoadExportedFailed", exception, ("cachePath", ExportedCachePath));
        }
    }

    private void LoadExportLog()
    {
        try
        {
            var stamp = File.GetLastWriteTimeUtc(ExportLogPath);
            if (!File.Exists(ExportLogPath) || stamp == _exportLogStamp)
            {
                return;
            }

            _exportLog = ReadExportLogFile();
            _exportLogStamp = stamp;
        }
        catch (Exception exception)
        {
            AppLog.Error("ErpPartIndex.LoadExportLogFailed", exception, ("cachePath", ExportLogPath));
        }
    }

    internal sealed record ExportLogEntry(DateTime ExportedAt, string? By, string? File);

    private sealed record CacheModel(string? SourceFileName, DateTime? UploadedAt, string[]? Codes, DateTime? SnapshotAt = null);
}
