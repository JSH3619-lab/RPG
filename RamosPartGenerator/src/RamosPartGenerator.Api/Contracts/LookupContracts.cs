namespace RamosPartGenerator.Api.Contracts;

public sealed record RevisionMetaResponse(string Revision, string DisplayRevision);

public sealed record LookupFieldResponse(
    string Key,
    string Label,
    string Section,
    bool Visible,
    bool AllowFreeText,
    IReadOnlyList<string> Options);

public sealed record LookupPageResponse(
    string Revision,
    string DisplayRevision,
    IReadOnlyList<LookupFieldResponse> Fields);

public sealed record ExportRegistrationRequest(IReadOnlyList<ExportRowRequest> Rows);

public sealed record PartParseRequest(string Revision, string PartCode);

public sealed record ExportRowRequest(
    string Kind,
    string PartCode,
    string Name,
    string GeneralInfo,
    string Specification);
