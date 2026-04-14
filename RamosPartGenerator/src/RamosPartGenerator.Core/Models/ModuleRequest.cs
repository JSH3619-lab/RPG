namespace RamosPartGenerator.Core.Models;

public sealed class ModuleRequest
{
    public string Revision { get; set; } = "30";
    public string DramTypeCode { get; set; } = string.Empty;
    public string DimmTypeCode { get; set; } = string.Empty;
    public string ModuleDensityCode { get; set; } = string.Empty;
    public string DieDensityCode { get; set; } = string.Empty;
    public string CompositionCode { get; set; } = string.Empty;
    public string RankCode { get; set; } = string.Empty;
    public string GenerationCode { get; set; } = string.Empty;
    public string IcBrandCode { get; set; } = string.Empty;
    public string ModuleCompTypeCode { get; set; } = string.Empty;
    public string SpeedCode { get; set; } = string.Empty;
    public string PcbCode { get; set; } = string.Empty;
    public string VendorCode { get; set; } = string.Empty;
    public string PurchaserCode { get; set; } = string.Empty;
    public string BasePartCode { get; set; } = string.Empty;
    public string BinPartCode { get; set; } = string.Empty;
}
