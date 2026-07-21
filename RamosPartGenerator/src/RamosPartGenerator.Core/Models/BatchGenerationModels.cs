namespace RamosPartGenerator.Core.Models;

public enum BatchItemStatus
{
    Success,
    PartialSuccess,
    Failed
}

public enum ModuleBatchInputKind
{
    Normal,
    Reball,
    FirstRepair,
    SecondRepair,
    ReballRepair,
    SharedDummy,
    SecondRepairDummy,
    ReballRepairDummy,
    FinishedProductRetest
}

public sealed class MdlBatchOptions
{
    public bool IncludeBasePid { get; set; }
    public bool IncludeBaseMfgId { get; set; }
    public bool IncludeReball { get; set; }
    public bool IncludeFirstRepair { get; set; }
    public bool IncludeSecondRepair { get; set; }
    public bool IncludeReballRepair { get; set; }
    public bool IncludeFinishedProductRetest { get; set; }
    public bool IncludeOriginalCompRelated { get; set; }
    public bool IncludeReballCompRelated { get; set; }
}

public sealed class CompBatchOptions
{
    public bool IncludeCompMdl { get; set; }
    public string SpeedCode { get; set; } = string.Empty;
}

public sealed record BatchItemResult(
    string InputPartCode,
    ModuleBatchInputKind DetectedInputKind,
    BatchItemStatus Status,
    IReadOnlyList<GeneratedPartRow> Rows,
    IReadOnlyList<string> Messages);

public sealed record BatchGenerationResult(
    IReadOnlyList<BatchItemResult> Items,
    IReadOnlyList<GeneratedPartRow> Rows,
    int DuplicateCount);
