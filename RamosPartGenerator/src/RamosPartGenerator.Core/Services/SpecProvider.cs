using System.Text.Json;
using RamosPartGenerator.Core.Specs;

namespace RamosPartGenerator.Core.Services;

public sealed class SpecProvider
{
    private readonly string _specDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private SharedSpec? _sharedSpec;
    private readonly Dictionary<string, RevisionSpec> _revisionSpecs = new(StringComparer.OrdinalIgnoreCase);

    public SpecProvider(string specDirectory)
    {
        _specDirectory = specDirectory;
    }

    public SharedSpec SharedSpec => _sharedSpec ?? throw new InvalidOperationException("Shared spec is not loaded.");

    public void Load()
    {
        var sharedPath = Path.Combine(_specDirectory, "shared.json");
        _sharedSpec = DeserializeFile<SharedSpec>(sharedPath);

        _revisionSpecs.Clear();
        foreach (var revision in _sharedSpec.SupportedRevisions)
        {
            var revisionPath = Path.Combine(_specDirectory, $"rev{revision}.json");
            var revisionSpec = DeserializeFile<RevisionSpec>(revisionPath);
            _revisionSpecs[NormalizeRevision(revision)] = revisionSpec;
        }
    }

    public IReadOnlyList<string> GetSupportedRevisions()
    {
        EnsureLoaded();
        return SharedSpec.SupportedRevisions;
    }

    public RevisionSpec GetRevisionSpec(string revision)
    {
        EnsureLoaded();
        var normalizedRevision = NormalizeRevision(revision);
        if (_revisionSpecs.TryGetValue(normalizedRevision, out var revisionSpec))
        {
            return revisionSpec;
        }

        throw new KeyNotFoundException($"Revision '{revision}' spec was not found.");
    }

    private void EnsureLoaded()
    {
        if (_sharedSpec is null)
        {
            Load();
        }
    }

    private T DeserializeFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Spec file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var value = JsonSerializer.Deserialize<T>(json, _jsonOptions);
        return value ?? throw new InvalidOperationException($"Failed to deserialize spec file: {path}");
    }

    private static string NormalizeRevision(string revision)
    {
        var normalized = revision.Trim().ToUpperInvariant().Replace("REV", string.Empty).Replace(" ", string.Empty);
        return normalized switch
        {
            "27.2" => "27",
            "27" => "27",
            "30.0" => "30",
            "30" => "30",
            _ => normalized
        };
    }
}
