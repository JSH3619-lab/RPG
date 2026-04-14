namespace RamosPartGenerator.Core.Models;

public sealed record GeneratedPartRow(
    string Kind,
    string PartCode,
    string Name,
    string GeneralInfo,
    string Specification,
    string? Note = null
);
