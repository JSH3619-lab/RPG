namespace RamosPartGenerator.Core.Services;

public sealed class ProductTextService
{
    private readonly SpecProvider _specProvider;

    public ProductTextService(SpecProvider specProvider)
    {
        _specProvider = specProvider;
    }

    public (string Name, string GeneralInfo, string Specification) BuildIncomingCompTexts(
        string partCode,
        string dramTypeLabel,
        string densityLabel,
        string bitLabel,
        string dieBrandLabel,
        string compTypeLabel,
        bool isThirdParty,
        string? speedText = null)
    {
        var shared = _specProvider.SharedSpec;
        var name = shared.ItemTextRules.NameEqualsCode ? partCode : partCode;
        var generalInfo = string.Empty;

        var pieces = new List<string>
        {
            dramTypeLabel,
            densityLabel,
            bitLabel,
            dieBrandLabel,
            compTypeLabel,
            "Comp"
        };

        if (isThirdParty)
        {
            pieces.Add("TP");
        }

        if (!string.IsNullOrWhiteSpace(speedText))
        {
            pieces.Add(speedText);
        }

        return (name, generalInfo, string.Join(" ", pieces.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    public (string Name, string GeneralInfo, string Specification) BuildModuleTexts(
        string partCode,
        string dramTypeLabel,
        string formFactorLabel,
        string capacityLabel,
        string dieDensityLabel,
        string ownerLabel,
        string pcbLabel,
        bool isThirdParty,
        string icCountText,
        string? speedText = null,
        bool isCompSale = false)
    {
        var shared = _specProvider.SharedSpec;
        var name = shared.ItemTextRules.NameEqualsCode ? partCode : partCode;
        var generalInfo = isCompSale
            ? $"{dramTypeLabel} Comp {capacityLabel} COO : {shared.ItemTextRules.CooDefault}"
            : $"{formFactorLabel} {capacityLabel} COO : {shared.ItemTextRules.CooDefault}";

        var specPieces = new List<string>
        {
            dramTypeLabel,
            formFactorLabel,
            capacityLabel,
            $"({dieDensityLabel} x8 *{icCountText})",
            ownerLabel,
            $"({pcbLabel} PCB)"
        };

        if (isThirdParty)
        {
            specPieces.Add("TP");
        }

        if (!string.IsNullOrWhiteSpace(speedText))
        {
            specPieces.Add(speedText);
        }

        return (name, generalInfo, string.Join(" ", specPieces.Where(x => !string.IsNullOrWhiteSpace(x))));
    }
}
