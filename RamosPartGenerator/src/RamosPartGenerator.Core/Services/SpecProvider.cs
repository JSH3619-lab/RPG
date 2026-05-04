using System.Text.Json;
using System.Reflection;
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
        _sharedSpec = DeserializeSpec<SharedSpec>("shared.json");

        _revisionSpecs.Clear();
        foreach (var revision in _sharedSpec.SupportedRevisions)
        {
            var revisionSpec = DeserializeSpec<RevisionSpec>($"rev{revision}.json");
            _revisionSpecs[NormalizeRevision(revision)] = revisionSpec;
        }
    }

    public IReadOnlyList<string> GetSupportedRevisions()
    {
        EnsureLoaded();
        return SharedSpec.SupportedRevisions.ToArray();
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

    private T DeserializeSpec<T>(string fileName)
    {
        var path = Path.Combine(_specDirectory, fileName);
        var json = File.Exists(path)
            ? File.ReadAllText(path)
            : ReadEmbeddedSpec(fileName, path);
        var value = JsonSerializer.Deserialize<T>(json, _jsonOptions);
        return value ?? throw new InvalidOperationException($"Failed to deserialize spec file: {path}");
    }

    private static string ReadEmbeddedSpec(string fileName, string fallbackPath)
    {
        var resourceName = $"RamosPartGenerator.Core.specs.{fileName}";
        var assembly = typeof(SpecProvider).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new FileNotFoundException($"Spec file not found: {fallbackPath} or embedded resource {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string NormalizeRevision(string revision)
    {
        var normalized = revision.Trim().ToUpperInvariant().Replace("REV", string.Empty).Replace(" ", string.Empty);
        return normalized switch
        {
            "30.0" => "30",
            "30" => "30",
            _ => normalized
        };
    }
}
