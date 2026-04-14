namespace RamosPartGenerator.Core.Models;

public sealed class IncomingCompRequest
{
    public string Revision { get; set; } = "30";
    public string SourceCode { get; set; } = string.Empty;
    public string DramTypeCode { get; set; } = string.Empty;
    public string DensityCode { get; set; } = string.Empty;
    public string BitOrganizationCode { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string InterfaceCode { get; set; } = string.Empty;
    public string RevisionCode { get; set; } = string.Empty;
    public string CompTypeCode { get; set; } = string.Empty;
    public string DieBrandCode { get; set; } = string.Empty;
    public string VendorCode { get; set; } = string.Empty;
    public string PurchaserCode { get; set; } = string.Empty;
    public string CompType2Code { get; set; } = string.Empty;
    public string PackageTypeCode { get; set; } = string.Empty;
    public string TesterCode { get; set; } = string.Empty;
}
