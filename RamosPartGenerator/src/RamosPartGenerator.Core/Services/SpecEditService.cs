using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RamosPartGenerator.Core.Services;

public sealed record SpecBackup(string Id, DateTime Timestamp);

/// <summary>
/// PGM 안에서 specs/*.json의 code_options를 편집하고, 저장할 때마다 스냅샷을 남겨 시점 복원을 지원한다.
/// 단일 exe 배포 시 specs 폴더가 디스크에 없을 수 있어, 첫 저장 때 현재 로드된 스펙을 디스크로 실체화한다.
/// </summary>
public sealed class SpecEditService
{
    private const int MaxBackups = 20;
    private const string BackupStampFormat = "yyyyMMdd-HHmmss-fff";

    private readonly SpecProvider _specProvider;

    public SpecEditService(SpecProvider specProvider)
    {
        _specProvider = specProvider;
    }

    private string SpecDirectory => _specProvider.SpecDirectory;
    private string BackupRoot => Path.Combine(SpecDirectory, "backup");

    public IReadOnlyList<string> ListOptionSetKeys()
    {
        return _specProvider.SharedSpec.CodeOptions.Keys
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<string> GetOptions(string optionSetKey)
    {
        return _specProvider.SharedSpec.CodeOptions.TryGetValue(optionSetKey, out var options)
            ? options.ToArray()
            : Array.Empty<string>();
    }

    public void SaveOptions(string optionSetKey, IReadOnlyList<string> options)
    {
        ValidateOptions(options);
        MaterializeToDisk();
        Snapshot();

        var sharedPath = Path.Combine(SpecDirectory, "shared.json");
        var root = JsonNode.Parse(
            File.ReadAllText(sharedPath),
            documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!
            .AsObject();
        var codeOptions = root["code_options"]?.AsObject()
            ?? throw new InvalidOperationException("shared.json에 code_options 항목이 없습니다.");

        var array = new JsonArray();
        foreach (var option in options)
        {
            array.Add(option);
        }
        codeOptions[optionSetKey] = array;

        File.WriteAllText(sharedPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        _specProvider.Load();
    }

    public IReadOnlyList<SpecBackup> ListBackups()
    {
        if (!Directory.Exists(BackupRoot))
        {
            return Array.Empty<SpecBackup>();
        }

        return Directory.GetDirectories(BackupRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .OrderByDescending(name => name, StringComparer.Ordinal)
            .Select(name => new SpecBackup(name!, ParseTimestamp(name!)))
            .ToArray();
    }

    public void RestoreBackup(string backupId)
    {
        var dir = Path.Combine(BackupRoot, backupId);
        if (!Directory.Exists(dir))
        {
            throw new InvalidOperationException($"백업을 찾을 수 없습니다: {backupId}");
        }

        MaterializeToDisk();
        Snapshot(); // 복원 자체도 되돌릴 수 있도록 현재 상태를 먼저 스냅샷한다.
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            File.Copy(file, Path.Combine(SpecDirectory, Path.GetFileName(file)), overwrite: true);
        }
        _specProvider.Load();
    }

    private void MaterializeToDisk()
    {
        Directory.CreateDirectory(SpecDirectory);
        foreach (var fileName in _specProvider.GetSpecFileNames())
        {
            var path = Path.Combine(SpecDirectory, fileName);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, _specProvider.ReadRawSpecJson(fileName));
            }
        }
    }

    private void Snapshot()
    {
        var dir = Path.Combine(BackupRoot, DateTime.Now.ToString(BackupStampFormat, CultureInfo.InvariantCulture));
        Directory.CreateDirectory(dir);
        foreach (var file in Directory.GetFiles(SpecDirectory, "*.json"))
        {
            File.Copy(file, Path.Combine(dir, Path.GetFileName(file)), overwrite: true);
        }
        Prune();
    }

    private void Prune()
    {
        if (!Directory.Exists(BackupRoot))
        {
            return;
        }

        var staleDirs = Directory.GetDirectories(BackupRoot)
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .Skip(MaxBackups);
        foreach (var dir in staleDirs)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // 오래된 백업 정리 실패는 편집을 막지 않는다.
            }
        }
    }

    private static void ValidateOptions(IReadOnlyList<string> options)
    {
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in options)
        {
            var separatorIndex = option.IndexOf(" - ", StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex >= option.Length - 3)
            {
                throw new InvalidOperationException($"옵션 형식이 올바르지 않습니다 (코드 - 설명): {option}");
            }

            var code = option[..separatorIndex].Trim();
            if (code.Length == 0)
            {
                throw new InvalidOperationException("코드는 비어 있을 수 없습니다.");
            }

            if (!seenCodes.Add(code))
            {
                throw new InvalidOperationException($"코드가 중복되었습니다: {code}");
            }
        }
    }

    private static DateTime ParseTimestamp(string id)
    {
        return DateTime.TryParseExact(id, BackupStampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp)
            ? timestamp
            : DateTime.MinValue;
    }
}
