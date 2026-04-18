using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Specs;

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

    public ModuleRequest ParseCompPart(string revision, string partCode)
    {
        var revisionSpec = _specProvider.GetRevisionSpec(revision);
        var normalizedPartCode = NormalizeCode(partCode);
        var parts = normalizedPartCode.Split('-');
        if (parts.Length != 2)
        {
            throw new InvalidOperationException("Comp Full Part 형식이 올바르지 않습니다.");
        }

        var headPart = parts[0];
        var tailPart = parts[1];
        if (headPart.Length < 10 || tailPart.Length < 5)
        {
            throw new InvalidOperationException("Comp Full Part 길이가 예상 형식과 맞지 않습니다.");
        }

        var compFamilyCode = headPart[..2];
        var compDramTypeCode = headPart.Substring(2, 1);
        var compDensityCode = headPart.Substring(3, 2);
        var compBitCode = headPart.Substring(5, 2);
        var compRevisionCode = headPart.Substring(9, 1);

        var compTypeCode = tailPart.Substring(0, 1);
        var dieBrandCode = tailPart.Substring(2, 1);
        var testerCode = tailPart.Substring(3, 1);
        var vendorCode = tailPart.Substring(4, 1);
        var purchaserCode = revisionSpec.Revision == "30" && tailPart.Length >= 6 ? tailPart.Substring(5, 1) : "0";

        var moduleSourceCode = MapCompFamilyToModuleFamily(compFamilyCode);
        var moduleDramTypeCode = MapCompDramTypeToModuleDramType(compDramTypeCode);
        var compositionCode = MapCompBitToModuleComposition(compBitCode);
        var dieDensityCode = MapCompDensityToModuleDieDensity(compDensityCode);
        if (string.IsNullOrWhiteSpace(moduleSourceCode) ||
            string.IsNullOrWhiteSpace(moduleDramTypeCode) ||
            string.IsNullOrWhiteSpace(compositionCode) ||
            string.IsNullOrWhiteSpace(dieDensityCode))
        {
            throw new InvalidOperationException("Comp Full Part에서 Module용 자동 매핑을 만들 수 없습니다.");
        }

        return new ModuleRequest
        {
            Revision = revisionSpec.Revision,
            CompFullPartCode = normalizedPartCode,
            ModuleSourceCode = moduleSourceCode,
            DramTypeCode = moduleDramTypeCode,
            CompositionCode = compositionCode,
            DieDensityCode = dieDensityCode,
            GenerationCode = compRevisionCode,
            IcBrandCode = revisionSpec.Module.SplitIcBrandAndCompType ? dieBrandCode : string.Empty,
            ModuleCompTypeCode = revisionSpec.Module.SplitIcBrandAndCompType ? compTypeCode : dieBrandCode + compTypeCode,
            CompTestCode = testerCode,
            VendorCode = vendorCode,
            PurchaserCode = purchaserCode == "0" ? string.Empty : purchaserCode
        };
    }

    public ModuleRequest ParseModuleFullPart(string revision, string partCode)
    {
        var revisionSpec = _specProvider.GetRevisionSpec(revision);
        var normalizedPartCode = NormalizeCode(partCode);
        var parts = normalizedPartCode.Split('-');
        if (parts.Length is < 2 or > 3)
        {
            throw new InvalidOperationException("Module Full Part 형식이 올바르지 않습니다.");
        }

        var headPart = parts[0];
        var tailPart = parts[1];
        var binTail = parts.Length == 3 ? parts[2] : string.Empty;
        if (headPart.Length != 11)
        {
            throw new InvalidOperationException($"Module Full Part 앞부분 길이가 올바르지 않습니다. 현재 길이: {headPart.Length} / 기대 길이: 11");
        }

        if (!string.IsNullOrEmpty(binTail) && binTail.Length != 7)
        {
            throw new InvalidOperationException("Module BIN suffix 길이가 올바르지 않습니다. 예: TNAGA00");
        }

        var request = new ModuleRequest
        {
            Revision = revisionSpec.Revision,
            ModuleFullPartCode = normalizedPartCode,
            BasePartCode = string.IsNullOrEmpty(binTail) ? normalizedPartCode : $"{headPart}-{tailPart}",
            BinPartCode = string.IsNullOrEmpty(binTail) ? string.Empty : normalizedPartCode,
            ModuleSourceCode = headPart[..2],
            DramTypeCode = headPart.Substring(2, 1),
            DimmTypeCode = headPart.Substring(3, 1),
            ModuleDensityCode = headPart.Substring(4, 2),
            PcbCode = string.Empty,
            CompositionCode = headPart.Substring(7, 1),
            DieDensityCode = headPart.Substring(8, 1),
            RankCode = headPart.Substring(9, 1),
            GenerationCode = headPart.Substring(10, 1),
            GradeCode = string.IsNullOrEmpty(binTail) ? string.Empty : binTail[..2],
            ProductBinCode = string.IsNullOrEmpty(binTail) ? string.Empty : binTail[^3..]
        };

        var bankVddCode = headPart.Substring(6, 1);
        if (tailPart.Length < 9)
        {
            throw new InvalidOperationException($"Rev {revisionSpec.DisplayRevision} Module tail 길이가 부족합니다.");
        }

        if (revisionSpec.Revision == "30")
        {
            request.IcBrandCode = tailPart.Substring(0, 1);
            request.ModuleCompTypeCode = tailPart.Substring(1, 1);
            request.CompTestCode = tailPart.Substring(2, 1);
            request.ModuleSmtCode = tailPart.Substring(3, 1);
            request.ModuleTestCode = tailPart.Substring(4, 1);
            request.SpeedCode = tailPart.Substring(5, 2);
            request.PcbCode = tailPart.Substring(7, 1);
            request.VendorCode = tailPart.Substring(8, 1);

            var trailingText = tailPart.Length > 9 ? tailPart[9..] : string.Empty;
            if (!string.IsNullOrEmpty(trailingText) && IsPurchaserCode(trailingText[..1]))
            {
                request.PurchaserCode = trailingText[..1];
                trailingText = trailingText[1..];
            }

            if (!string.IsNullOrEmpty(trailingText))
            {
                request.A100SpecialCode = trailingText[..1];
                trailingText = trailingText[1..];
            }

            if (!string.IsNullOrEmpty(trailingText))
            {
                request.SpecialCode2Code = trailingText[..1];
                trailingText = trailingText[1..];
            }

            if (!string.IsNullOrEmpty(trailingText))
            {
                request.SpecialCode3Code = trailingText[..1];
                trailingText = trailingText[1..];
            }

            if (!string.IsNullOrEmpty(trailingText))
            {
                throw new InvalidOperationException($"Rev 30 Module tail에 해석되지 않은 코드가 남아 있습니다: {trailingText}");
            }
        }
        else
        {
            request.ModuleCompTypeCode = tailPart[..2];
            request.CompTestCode = tailPart.Substring(2, 1);
            request.ModuleSmtCode = tailPart.Substring(3, 1);
            request.ModuleTestCode = tailPart.Substring(4, 1);
            request.SpeedCode = tailPart.Substring(5, 2);
            request.PcbCode = tailPart.Substring(7, 1);
            request.VendorCode = tailPart.Substring(8, 1);

            var trailingText = tailPart.Length > 9 ? tailPart[9..] : string.Empty;
            if (!string.IsNullOrEmpty(trailingText))
            {
                request.SpecialCode2Code = trailingText[..1];
                trailingText = trailingText[1..];
            }

            if (!string.IsNullOrEmpty(trailingText))
            {
                request.SpecialCode3Code = trailingText[..1];
                trailingText = trailingText[1..];
            }

            if (!string.IsNullOrEmpty(trailingText))
            {
                throw new InvalidOperationException($"Rev 27 Module tail에 해석되지 않은 코드가 남아 있습니다: {trailingText}");
            }
        }

        request.PcbCode = string.IsNullOrWhiteSpace(request.PcbCode) ? bankVddCode : request.PcbCode;
        return request;
    }

    private static string NormalizeCode(string? partCode)
    {
        return (partCode ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", string.Empty);
    }

    private static string MapCompFamilyToModuleFamily(string compFamilyCode)
    {
        return compFamilyCode switch
        {
            "RC" => "RM",
            "TC" => "TM",
            "CC" => "CM",
            "BC" => "BM",
            _ => string.Empty
        };
    }

    private static string MapCompDramTypeToModuleDramType(string compDramTypeCode)
    {
        return compDramTypeCode switch
        {
            "A" => "4",
            "R" => "R",
            _ => string.Empty
        };
    }

    private static string MapCompBitToModuleComposition(string compBitCode)
    {
        return compBitCode switch
        {
            "04" => "4",
            "08" => "8",
            "16" => "6",
            _ => string.Empty
        };
    }

    private static string MapCompDensityToModuleDieDensity(string compDensityCode)
    {
        return compDensityCode switch
        {
            "4G" => "4",
            "8G" => "8",
            "AG" or "AH" => "A",
            "HE" => "H",
            "BG" or "BH" => "B",
            _ => string.Empty
        };
    }

    private static bool IsPurchaserCode(string code)
    {
        return code is "V" or "H" or "A" or "0";
    }
}
