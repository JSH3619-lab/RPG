using RamosPartGenerator.Core.Models;

namespace RamosPartGenerator.Core.Services;

public sealed class ModuleService
{
    private readonly SpecProvider _specProvider;
    private readonly ProductTextService _productTextService;

    public ModuleService(SpecProvider specProvider, ProductTextService productTextService)
    {
        _specProvider = specProvider;
        _productTextService = productTextService;
    }

    public IReadOnlyList<GeneratedPartRow> GeneratePreview(ModuleRequest request)
    {
        _ = _specProvider.GetRevisionSpec(request.Revision);

        var isThirdParty = !string.IsNullOrWhiteSpace(request.PurchaserCode);
        var dramTypeLabel = request.DramTypeCode == "R" ? "DDR5" : "DDR4";
        var formFactorLabel = request.DimmTypeCode;
        var capacityLabel = request.ModuleDensityCode;
        var dieDensityLabel = request.DieDensityCode;
        var ownerLabel = request.VendorCode;
        var pcbLabel = request.PcbCode;
        var icCount = "8";
        var basePartCode = string.IsNullOrWhiteSpace(request.BasePartCode) ? "MODULE-BASE" : request.BasePartCode;
        var binPartCode = string.IsNullOrWhiteSpace(request.BinPartCode) ? $"{basePartCode}-BIN" : request.BinPartCode;

        var baseText = _productTextService.BuildModuleTexts(
            basePartCode, dramTypeLabel, formFactorLabel, capacityLabel, dieDensityLabel, ownerLabel, pcbLabel, isThirdParty, icCount);
        var binText = _productTextService.BuildModuleTexts(
            binPartCode, dramTypeLabel, formFactorLabel, capacityLabel, dieDensityLabel, ownerLabel, pcbLabel, isThirdParty, icCount, request.SpeedCode, request.DimmTypeCode.Equals("Comp", StringComparison.OrdinalIgnoreCase));

        return new List<GeneratedPartRow>
        {
            new("Module", basePartCode, baseText.Name, baseText.GeneralInfo, baseText.Specification),
            new("Module BIN", binPartCode, binText.Name, binText.GeneralInfo, binText.Specification, request.SpeedCode)
        };
    }
}
