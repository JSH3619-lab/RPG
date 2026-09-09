namespace RamosPartGenerator.Core.Models;

public sealed class LpddrRequest
{
    public string Revision { get; init; } = string.Empty;
    public string SourceCode { get; init; } = string.Empty;
    /// <summary>true = Die 입고(K4 계열), false = PKG 입고(K5 계열)</summary>
    public bool IsDieIncoming { get; init; }
    public string DramTypeCode { get; init; } = string.Empty;
    public string DensityCode { get; init; } = string.Empty;
    public string DieDensityCode { get; init; } = string.Empty;
    public string DieBitCode { get; init; } = string.Empty;
    public string BitCode { get; init; } = string.Empty;
    public string BankCode { get; init; } = string.Empty;
    public string InterfaceCode { get; init; } = string.Empty;
    public string StackCode { get; init; } = string.Empty;
    public string RevisionCode { get; init; } = string.Empty;
    public string DieTypeCode { get; init; } = string.Empty;
    public string PackageTypeCode { get; init; } = string.Empty;
    public string DieBrandCode { get; init; } = string.Empty;
    public string AssemblyCode { get; init; } = string.Empty;
    public string TesterCode { get; init; } = string.Empty;
    public string SpeedCode { get; init; } = string.Empty;
    public string PcbRevisionCode { get; init; } = string.Empty;
    public string VendorCode { get; init; } = string.Empty;
    public string PurchaserCode { get; init; } = string.Empty;
}
