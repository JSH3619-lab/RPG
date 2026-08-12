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
        string? speedText = null,
        string compType2Code = "",
        string icBrandCode = "",
        string vendorCode = "",
        string purchaserCode = "")
    {
        var name = partCode;
        var generalInfo = string.Empty;
        var isA100 = IsIncomingA100(isThirdParty, vendorCode, purchaserCode);

        var pieces = new List<string>
        {
            dramTypeLabel,
            densityLabel,
            bitLabel,
            dieBrandLabel
        };

        if (isA100)
        {
            pieces.Add("A100");
        }

        var icBrandLabel = GetIcBrandSpecLabel(icBrandCode);
        if (!string.IsNullOrWhiteSpace(icBrandLabel))
        {
            pieces.Add(icBrandLabel);
        }

        pieces.Add(compTypeLabel);
        pieces.Add("Comp");

        if (isThirdParty && !isA100)
        {
            pieces.Add("TP");
        }

        var compType2Label = GetCompType2Description(compType2Code);
        if (!string.IsNullOrWhiteSpace(compType2Label))
        {
            pieces.Add(compType2Label);
        }

        if (!string.IsNullOrWhiteSpace(speedText))
        {
            pieces.Add(speedText);
        }

        return (name, generalInfo, string.Join(" ", pieces.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    public (string Name, string GeneralInfo, string Specification) BuildModuleTexts(
        string partCode,
        string moduleSourceCode,
        string dramTypeLabel,
        string formFactorLabel,
        string capacityLabel,
        string dieDensityLabel,
        string compositionCode,
        string icCountText,
        string generationCode,
        string icBrandCode,
        string moduleCompTypeCode,
        string vendorCode,
        string purchaserCode,
        string pcbCode,
        bool isThirdParty,
        string specialCode2Code,
        string specialCode3Code,
        string? speedText = null,
        bool isCompSale = false,
        string compTestCode = "",
        bool isManufacturing = false)
    {
        var shared = _specProvider.SharedSpec;
        var name = partCode;
        var generalInfo = isCompSale
            ? $"{dramTypeLabel} Comp {capacityLabel} COO : {shared.ItemTextRules.CooDefault}"
            : $"{formFactorLabel} {capacityLabel} COO : {shared.ItemTextRules.CooDefault}";

        var specification = isCompSale
            ? BuildCompSaleSpecification(
                dramTypeLabel,
                dieDensityLabel,
                compositionCode,
                generationCode,
                icBrandCode,
                moduleCompTypeCode,
                compTestCode,
                vendorCode,
                purchaserCode,
                isThirdParty,
                specialCode2Code,
                specialCode3Code,
                speedText,
                isManufacturing)
            : BuildStandardModuleSpecification(
                moduleSourceCode,
                dramTypeLabel,
                formFactorLabel,
                capacityLabel,
                dieDensityLabel,
                compositionCode,
                icCountText,
                icBrandCode,
                compTestCode,
                vendorCode,
                purchaserCode,
                pcbCode,
                isThirdParty,
                specialCode2Code,
                specialCode3Code,
                speedText);

        return (name, generalInfo, specification);
    }

    private static string BuildStandardModuleSpecification(
        string moduleSourceCode,
        string dramTypeLabel,
        string formFactorLabel,
        string capacityLabel,
        string dieDensityLabel,
        string compositionCode,
        string icCountText,
        string icBrandCode,
        string compTestCode,
        string vendorCode,
        string purchaserCode,
        string pcbCode,
        bool isThirdParty,
        string specialCode2Code,
        string specialCode3Code,
        string? speedText)
    {
        var specPieces = new List<string>
        {
            dramTypeLabel,
            formFactorLabel,
            capacityLabel
        };

        var icCountCore = ExtractIcCountCore(icCountText);
        var compositionText = GetCompositionText(compositionCode);
        if (!string.IsNullOrWhiteSpace(icCountCore) &&
            !icCountCore.Equals("ERR", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(compositionText))
        {
            specPieces.Add($"({dieDensityLabel} {compositionText} *{icCountCore})");
        }

        var icBrandLabel = GetIcBrandSpecLabel(icBrandCode);
        if (!string.IsNullOrWhiteSpace(icBrandLabel))
        {
            specPieces.Add(icBrandLabel);
        }

        var isA100 = IsModuleA100(isThirdParty, vendorCode, purchaserCode);
        var ownerLabel = ResolveModuleOwnerLabel(moduleSourceCode, isA100);
        if (!string.IsNullOrWhiteSpace(ownerLabel))
        {
            specPieces.Add(ownerLabel);
        }

        if (isThirdParty && !isA100)
        {
            specPieces.Add("TP");
        }

        var pcbPiece = GetModulePcbSpecPiece(pcbCode);
        if (!string.IsNullOrWhiteSpace(pcbPiece))
        {
            specPieces.Add(pcbPiece);
        }

        specPieces.AddRange(GetModuleStatusLabels(specialCode2Code, specialCode3Code));

        if (!string.IsNullOrWhiteSpace(speedText))
        {
            specPieces.Add(speedText);
        }

        return NormalizeWhitespace(string.Join(" ", specPieces.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    private static string BuildCompSaleSpecification(
        string dramTypeLabel,
        string dieDensityLabel,
        string compositionCode,
        string generationCode,
        string icBrandCode,
        string moduleCompTypeCode,
        string compTestCode,
        string vendorCode,
        string purchaserCode,
        bool isThirdParty,
        string specialCode2Code,
        string specialCode3Code,
        string? speedText,
        bool isManufacturing)
    {
        var isA100 = IsModuleA100(isThirdParty, vendorCode, purchaserCode);
        var specPieces = new List<string>
        {
            dramTypeLabel,
            dieDensityLabel,
            GetCompositionText(compositionCode),
            GetGenerationDieLabel(generationCode),
            GetIcBrandShortLabel(icBrandCode),
            GetCompTypeDescription(moduleCompTypeCode, isManufacturing),
            "Comp"
        };

        if (isThirdParty && !isA100)
        {
            specPieces.Add("TP");
        }

        specPieces.AddRange(GetModuleStatusLabels(specialCode2Code, specialCode3Code));

        if (!string.IsNullOrWhiteSpace(speedText))
        {
            specPieces.Add(speedText);
        }

        return NormalizeWhitespace(string.Join(" ", specPieces.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    private static string ExtractIcCountCore(string icCountText)
    {
        return (icCountText ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
    }

    private static string GetCompositionText(string compositionCode)
    {
        return compositionCode switch
        {
            "4" => "x4",
            "8" => "x8",
            "9" => "x8 (x4 -> x8)",
            "6" => "x16",
            _ => compositionCode
        };
    }

    private static string ResolveModuleOwnerLabel(string moduleSourceCode, bool isA100)
    {
        if (isA100)
        {
            return "A100";
        }

        return moduleSourceCode switch
        {
            "RM" or "TM" => "RAmos",
            "CM" or "BM" => "CT",
            "XM" => "RAmos",
            "ZM" => "CT",
            _ => string.Empty
        };
    }

    private static string GetModulePcbSpecPiece(string pcbCode)
    {
        return pcbCode switch
        {
            "0" => string.Empty,
            "1" => "DN PCB(Green)",
            "2" => "HJ PCB(Green)",
            "3" => "DN PCB(Black)",
            "4" => "HJ PCB(Black)",
            "5" => "HJ PCB(Black, 11x11)",
            "6" => "BP PCB(Green)",
            "7" => "BP PCB(Black)",
            "8" => "BP RGB PCB(Black)",
            "9" => "BP PCB(Black)",
            "A" => "AXA5UR02 PCB(Black)",
            "B" => "BP PCB(Black)",
            "G" => "Hammer Pass",
            "K" => "Hammer Fail",
            _ => pcbCode
        };
    }

    private static string GetIcBrandSpecLabel(string icBrandCode)
    {
        return icBrandCode switch
        {
            "S" => "S1",
            "G" => "GIGA S1",
            "V" => "GIGA S1(SV)",
            "P" => "GIGA S1(SP)",
            "H" => "S2",
            "M" => "S3",
            "C" => "S6",
            "N" => "S9",
            _ => string.Empty
        };
    }

    private static bool IsIncomingA100(bool isThirdParty, string vendorCode, string purchaserCode)
    {
        return isThirdParty &&
               NormalizeLookupCode(vendorCode).Equals("A", StringComparison.OrdinalIgnoreCase) &&
               NormalizeLookupCode(purchaserCode).Equals("A", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsModuleA100(bool isThirdParty, string vendorCode, string purchaserCode)
    {
        return isThirdParty &&
               NormalizeLookupCode(vendorCode).Equals("A", StringComparison.OrdinalIgnoreCase) &&
               NormalizeLookupCode(purchaserCode).Equals("A", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetModuleStatusLabels(string specialCode2Code, string specialCode3Code)
    {
        if (TryGetSpecialCode2Label(specialCode2Code, out var special2Label))
        {
            yield return special2Label;
        }

        if (TryGetSpecialCode3Label(specialCode3Code, out var special3Label))
        {
            yield return special3Label;
        }
    }

    private static bool TryGetSpecialCode2Label(string code, out string label)
    {
        label = code switch
        {
            "R" => "1st Repair",
            "S" => "2nd Repair",
            "B" => "Reball",
            "C" => "Reball Repair",
            _ => string.Empty
        };
        return !string.IsNullOrWhiteSpace(label);
    }

    private static bool TryGetSpecialCode3Label(string code, out string label)
    {
        label = code switch
        {
            "R" => "RMA",
            "M" => "RMA",
            "Y" => "Retest",
            _ => string.Empty
        };
        return !string.IsNullOrWhiteSpace(label);
    }

    private static string GetGenerationDieLabel(string generationCode)
    {
        return string.IsNullOrWhiteSpace(generationCode) ? string.Empty : $"{generationCode}-die";
    }

    private static string GetIcBrandShortLabel(string icBrandCode)
    {
        return icBrandCode switch
        {
            "S" => "S1",
            "G" => "GIGA S1",
            "V" => "GIGA S1(SV)",
            "P" => "GIGA S1(SP)",
            "H" => "S2",
            "M" => "S3",
            "C" => "S6",
            "N" => "S9",
            "A" => "A100",
            "X" => "RAMBO",
            _ => icBrandCode
        };
    }

    public static string GetCompTypeDescription(string compTypeCode, bool isManufacturing = false)
    {
        var normalizedCode = NormalizeLookupCode(compTypeCode);
        if (isManufacturing)
        {
            return normalizedCode switch
            {
                "0" => "Only Test",
                "1" => "Reball",
                "2" => "EMC",
                "3" => "Laser-Marking",
                "4" => "Reball/EMC",
                "5" => "Reball/EMC/Laser-Marking",
                "6" => "Reball/Laser-Marking",
                "7" => "EMC/Laser-Marking",
                _ => normalizedCode
            };
        }

        return normalizedCode switch
        {
            "P" => "Partial",
            "U" => "Pre-Mark Partial",
            "N" => "EMC Partial",
            "H" => "a chip",
            "M" => "Erase Marking",
            "C" => "X-Comp",
            "D" => "Tested",
            "G" => "MDL(GOX)",
            "T" => "MDL Reballed(GKKR)",
            "F" => "Pre-Mark MDL(FX)",
            "E" => "EMC MDL(FX)",
            "Q" => "Pre-Mark Reballed(FKKR)",
            "W" => "EMC Reballed(FKKR)",
            "J" => "G Comp",
            "A" => "EMC G Comp",
            "X" => "EMC Partial X",
            "Y" => "EMC Partial Y",
            "Z" => "EMC Partial Z",
            _ => normalizedCode
        };
    }

    private static string GetCompType2Description(string compType2Code)
    {
        var normalizedCode = NormalizeLookupCode(compType2Code);
        return normalizedCode switch
        {
            "B" => "Reball",
            "1" => "Reball/EMC",
            _ => string.Empty
        };
    }

    private static string NormalizeLookupCode(string rawValue)
    {
        var trimmed = (rawValue ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Equals("(None)", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var separatorIndex = trimmed.IndexOf(" - ", StringComparison.Ordinal);
        return separatorIndex > -1
            ? trimmed[..separatorIndex].Trim()
            : trimmed;
    }

    private static string NormalizeWhitespace(string rawText)
    {
        var normalizedText = (rawText ?? string.Empty)
            .Replace('\u00A0', ' ')
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        while (normalizedText.Contains("  ", StringComparison.Ordinal))
        {
            normalizedText = normalizedText.Replace("  ", " ", StringComparison.Ordinal);
        }

        return normalizedText;
    }
}
