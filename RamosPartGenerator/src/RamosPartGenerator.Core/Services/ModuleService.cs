using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Specs;

namespace RamosPartGenerator.Core.Services;

public sealed class ModuleService
{
    private readonly SpecProvider _specProvider;
    private readonly ProductTextService _productTextService;
    private static readonly Dictionary<string, string> PcbLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1"] = "DN Green",
        ["2"] = "HJ Green",
        ["3"] = "DN Black",
        ["4"] = "HJ Black",
        ["5"] = "HJ Black 11x11",
        ["6"] = "BP Green",
        ["7"] = "BP Black",
        ["8"] = "BP RGB Black",
        ["9"] = "ADATA/BP Black",
        ["A"] = "AXA5UR02",
        ["G"] = "Hammer Pass",
        ["K"] = "Hammer Fail"
    };

    public ModuleService(SpecProvider specProvider, ProductTextService productTextService)
    {
        _specProvider = specProvider;
        _productTextService = productTextService;
    }

    public IReadOnlyList<GeneratedPartRow> GeneratePreview(ModuleRequest request)
    {
        var revisionSpec = _specProvider.GetRevisionSpec(request.Revision);
        var effectiveRequest = BuildEffectiveRequest(request, revisionSpec);
        var isThirdParty = IsThirdPartyModule(effectiveRequest.ModuleSourceCode);

        ValidateRequiredCodes(
            effectiveRequest.ModuleSourceCode,
            effectiveRequest.DramTypeCode,
            effectiveRequest.DimmTypeCode,
            effectiveRequest.ModuleDensityCode,
            effectiveRequest.CompositionCode,
            effectiveRequest.DieDensityCode,
            effectiveRequest.RankCode,
            effectiveRequest.GenerationCode,
            effectiveRequest.ModuleCompTypeCode,
            effectiveRequest.CompTestCode,
            effectiveRequest.ModuleSmtCode,
            effectiveRequest.ModuleTestCode,
            effectiveRequest.SpeedCode,
            effectiveRequest.PcbCode,
            effectiveRequest.VendorCode);

        if (revisionSpec.Revision == "30" && IsBlankCode(effectiveRequest.IcBrandCode))
        {
            throw new InvalidOperationException("Rev 30 Module은 I.C Brand가 반드시 필요합니다.");
        }

        if (revisionSpec.Revision == "30" && isThirdParty && IsBlankCode(effectiveRequest.PurchaserCode))
        {
            throw new InvalidOperationException("Third-Party Module은 Purchaser가 반드시 필요합니다.");
        }

        var basePartCode = BuildModuleBasePartCode(revisionSpec, effectiveRequest);
        var binPartCode = BuildModuleBinPartCode(basePartCode, effectiveRequest);
        var dramTypeLabel = GetModuleDramTypeLabel(effectiveRequest.DramTypeCode);
        var formFactorLabel = GetModuleFormFactorLabel(effectiveRequest.DimmTypeCode);
        var capacityLabel = GetModuleDensityLabel(effectiveRequest.ModuleDensityCode);
        var dieDensityLabel = GetDensityLabel(effectiveRequest.DieDensityCode);
        var ownerLabel = GetModuleOwnerLabel(effectiveRequest.ModuleSourceCode);
        var pcbLabel = GetPcbLabel(effectiveRequest.PcbCode);
        var icCount = CalculateModuleIcCount(
            effectiveRequest.ModuleDensityCode,
            effectiveRequest.DieDensityCode,
            effectiveRequest.CompositionCode,
            effectiveRequest.RankCode);
        var speedText = GetModuleSpeedText(effectiveRequest.SpeedCode);

        var baseText = _productTextService.BuildModuleTexts(
            basePartCode, dramTypeLabel, formFactorLabel, capacityLabel, dieDensityLabel, ownerLabel, pcbLabel, isThirdParty, icCount);
        var binText = _productTextService.BuildModuleTexts(
            binPartCode, dramTypeLabel, formFactorLabel, capacityLabel, dieDensityLabel, ownerLabel, pcbLabel, isThirdParty, icCount, speedText, effectiveRequest.DimmTypeCode.Equals("C", StringComparison.OrdinalIgnoreCase));

        return new List<GeneratedPartRow>
        {
            new("Module", basePartCode, baseText.Name, baseText.GeneralInfo, baseText.Specification),
            new("Module BIN", binPartCode, binText.Name, binText.GeneralInfo, binText.Specification, speedText)
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

    private ModuleRequest BuildEffectiveRequest(ModuleRequest request, RevisionSpec revisionSpec)
    {
        var effective = NormalizeRequest(request);

        if (!string.IsNullOrWhiteSpace(effective.CompFullPartCode))
        {
            var parsedComp = ParseCompPart(revisionSpec.Revision, effective.CompFullPartCode);
            effective = MergeRequests(parsedComp, effective);
        }

        if (!string.IsNullOrWhiteSpace(effective.ModuleFullPartCode))
        {
            var parsedModule = ParseModuleFullPart(revisionSpec.Revision, effective.ModuleFullPartCode);
            effective = MergeRequests(parsedModule, effective);
        }

        return NormalizeRequest(effective);
    }

    private static ModuleRequest NormalizeRequest(ModuleRequest request)
    {
        return new ModuleRequest
        {
            Revision = request.Revision,
            ModuleSourceCode = NormalizeCode(request.ModuleSourceCode),
            CompFullPartCode = NormalizeCode(request.CompFullPartCode),
            ModuleFullPartCode = NormalizeCode(request.ModuleFullPartCode),
            DramTypeCode = NormalizeCode(request.DramTypeCode),
            DimmTypeCode = NormalizeCode(request.DimmTypeCode),
            ModuleDensityCode = NormalizeCode(request.ModuleDensityCode),
            DieDensityCode = NormalizeCode(request.DieDensityCode),
            CompositionCode = NormalizeCode(request.CompositionCode),
            RankCode = NormalizeCode(request.RankCode),
            GenerationCode = NormalizeCode(request.GenerationCode),
            IcBrandCode = NormalizeCode(request.IcBrandCode),
            ModuleCompTypeCode = NormalizeCode(request.ModuleCompTypeCode),
            CompTestCode = NormalizeCode(request.CompTestCode),
            ModuleSmtCode = NormalizeCode(request.ModuleSmtCode),
            ModuleTestCode = NormalizeCode(request.ModuleTestCode),
            SpeedCode = NormalizeCode(request.SpeedCode),
            PcbCode = NormalizeCode(request.PcbCode),
            VendorCode = NormalizeCode(request.VendorCode),
            PurchaserCode = NormalizeCode(request.PurchaserCode),
            A100SpecialCode = NormalizeCode(request.A100SpecialCode),
            SpecialCode2Code = NormalizeCode(request.SpecialCode2Code),
            SpecialCode3Code = NormalizeCode(request.SpecialCode3Code),
            GradeCode = NormalizeCode(request.GradeCode),
            ProductBinCode = NormalizeCode(request.ProductBinCode),
            BasePartCode = NormalizeCode(request.BasePartCode),
            BinPartCode = NormalizeCode(request.BinPartCode)
        };
    }

    private static ModuleRequest MergeRequests(ModuleRequest parsedRequest, ModuleRequest overrideRequest)
    {
        return new ModuleRequest
        {
            Revision = string.IsNullOrWhiteSpace(overrideRequest.Revision) ? parsedRequest.Revision : overrideRequest.Revision,
            ModuleSourceCode = PreferValue(parsedRequest.ModuleSourceCode, overrideRequest.ModuleSourceCode),
            CompFullPartCode = PreferValue(parsedRequest.CompFullPartCode, overrideRequest.CompFullPartCode),
            ModuleFullPartCode = PreferValue(parsedRequest.ModuleFullPartCode, overrideRequest.ModuleFullPartCode),
            DramTypeCode = PreferValue(parsedRequest.DramTypeCode, overrideRequest.DramTypeCode),
            DimmTypeCode = PreferValue(parsedRequest.DimmTypeCode, overrideRequest.DimmTypeCode),
            ModuleDensityCode = PreferValue(parsedRequest.ModuleDensityCode, overrideRequest.ModuleDensityCode),
            DieDensityCode = PreferValue(parsedRequest.DieDensityCode, overrideRequest.DieDensityCode),
            CompositionCode = PreferValue(parsedRequest.CompositionCode, overrideRequest.CompositionCode),
            RankCode = PreferValue(parsedRequest.RankCode, overrideRequest.RankCode),
            GenerationCode = PreferValue(parsedRequest.GenerationCode, overrideRequest.GenerationCode),
            IcBrandCode = PreferValue(parsedRequest.IcBrandCode, overrideRequest.IcBrandCode),
            ModuleCompTypeCode = PreferValue(parsedRequest.ModuleCompTypeCode, overrideRequest.ModuleCompTypeCode),
            CompTestCode = PreferValue(parsedRequest.CompTestCode, overrideRequest.CompTestCode),
            ModuleSmtCode = PreferValue(parsedRequest.ModuleSmtCode, overrideRequest.ModuleSmtCode),
            ModuleTestCode = PreferValue(parsedRequest.ModuleTestCode, overrideRequest.ModuleTestCode),
            SpeedCode = PreferValue(parsedRequest.SpeedCode, overrideRequest.SpeedCode),
            PcbCode = PreferValue(parsedRequest.PcbCode, overrideRequest.PcbCode),
            VendorCode = PreferValue(parsedRequest.VendorCode, overrideRequest.VendorCode),
            PurchaserCode = PreferValue(parsedRequest.PurchaserCode, overrideRequest.PurchaserCode),
            A100SpecialCode = PreferValue(parsedRequest.A100SpecialCode, overrideRequest.A100SpecialCode),
            SpecialCode2Code = PreferValue(parsedRequest.SpecialCode2Code, overrideRequest.SpecialCode2Code),
            SpecialCode3Code = PreferValue(parsedRequest.SpecialCode3Code, overrideRequest.SpecialCode3Code),
            GradeCode = PreferValue(parsedRequest.GradeCode, overrideRequest.GradeCode),
            ProductBinCode = PreferValue(parsedRequest.ProductBinCode, overrideRequest.ProductBinCode),
            BasePartCode = PreferValue(parsedRequest.BasePartCode, overrideRequest.BasePartCode),
            BinPartCode = PreferValue(parsedRequest.BinPartCode, overrideRequest.BinPartCode)
        };
    }

    private static string PreferValue(string parsedValue, string overrideValue)
    {
        return IsBlankCode(overrideValue) ? parsedValue : overrideValue;
    }

    private static string BuildModuleBasePartCode(RevisionSpec revisionSpec, ModuleRequest request)
    {
        var bankVddCode = GetModuleBankVddCode(request.DramTypeCode, request.SpeedCode);
        if (IsBlankCode(bankVddCode))
        {
            throw new InvalidOperationException("선택한 Speed에 맞는 Module Bank/VDD 코드를 계산할 수 없습니다.");
        }

        var basePartCode = request.ModuleSourceCode + request.DramTypeCode + request.DimmTypeCode + request.ModuleDensityCode + bankVddCode + request.CompositionCode + request.DieDensityCode + request.RankCode + request.GenerationCode;

        if (revisionSpec.Revision == "30")
        {
            basePartCode += "-" + request.IcBrandCode + request.ModuleCompTypeCode + request.CompTestCode + request.ModuleSmtCode + request.ModuleTestCode + request.SpeedCode + request.PcbCode + request.VendorCode;
        }
        else
        {
            basePartCode += "-" + request.ModuleCompTypeCode + request.CompTestCode + request.ModuleSmtCode + request.ModuleTestCode + request.SpeedCode + request.PcbCode + request.VendorCode;
        }

        if (!IsBlankCode(request.PurchaserCode))
        {
            basePartCode += request.PurchaserCode;
        }

        if (revisionSpec.Revision == "30" && !IsBlankCode(request.A100SpecialCode))
        {
            basePartCode += request.A100SpecialCode;
        }

        if (!IsBlankCode(request.SpecialCode2Code))
        {
            basePartCode += request.SpecialCode2Code;
        }

        if (!IsBlankCode(request.SpecialCode3Code))
        {
            basePartCode += request.SpecialCode3Code;
        }

        return basePartCode;
    }

    private static string BuildModuleBinPartCode(string basePartCode, ModuleRequest request)
    {
        var gradeCode = IsBlankCode(request.GradeCode) ? "TN" : request.GradeCode;
        var productBinCode = IsBlankCode(request.ProductBinCode) ? "A00" : request.ProductBinCode;
        return $"{basePartCode}-{gradeCode}{request.ModuleDensityCode}{productBinCode}";
    }

    private static string GetModuleBankVddCode(string dramTypeCode, string speedCode)
    {
        return (dramTypeCode, speedCode) switch
        {
            ("4", "WE") => "4",
            ("R", "QK") or ("R", "WM") => "5",
            ("R", "CM") or ("R", "CQ") => "6",
            ("R", "CR") or ("R", "CS") => "7",
            _ => string.Empty
        };
    }

    private static string GetModuleDramTypeLabel(string dramTypeCode)
    {
        return dramTypeCode == "R" ? "DDR5" : "DDR4";
    }

    private static string GetModuleFormFactorLabel(string dimmTypeCode)
    {
        return dimmTypeCode switch
        {
            "D" => "UDIMM",
            "S" => "SODIMM",
            "C" => "Comp",
            _ => dimmTypeCode
        };
    }

    private static string GetModuleDensityLabel(string moduleDensityCode)
    {
        return moduleDensityCode switch
        {
            "4G" => "4GB",
            "8G" => "8GB",
            "AG" => "16GB",
            "BG" => "32GB",
            "CG" => "64GB",
            _ => moduleDensityCode
        };
    }

    private static string GetDensityLabel(string densityCode)
    {
        return densityCode switch
        {
            "4" => "4Gb",
            "8" => "8Gb",
            "A" => "16Gb",
            "H" => "24Gb",
            "B" => "32Gb",
            "C" => "64Gb",
            _ => densityCode
        };
    }

    private static string GetModuleOwnerLabel(string moduleSourceCode)
    {
        return moduleSourceCode switch
        {
            "RM" or "TM" => "RAmos",
            "CM" or "BM" => "CT",
            _ => moduleSourceCode
        };
    }

    private static string GetPcbLabel(string pcbCode)
    {
        return PcbLabels.TryGetValue(pcbCode, out var label) ? label : pcbCode;
    }

    private static string CalculateModuleIcCount(string moduleDensityCode, string dieDensityCode, string compositionCode, string rankCode)
    {
        var moduleDensityGb = moduleDensityCode switch
        {
            "1G" => 1d,
            "2G" => 2d,
            "4G" => 4d,
            "8G" => 8d,
            "AG" => 16d,
            "BG" => 32d,
            "CG" => 64d,
            _ => 0d
        };

        var dieDensityGb = dieDensityCode switch
        {
            "4" => 4d,
            "8" => 8d,
            "A" => 16d,
            "H" => 24d,
            "B" => 32d,
            _ => 0d
        };

        var bitFactor = compositionCode switch
        {
            "4" => 0.5d,
            "8" => 1d,
            "6" => 2d,
            _ => 0d
        };

        if (dieDensityGb <= 0 || bitFactor <= 0)
        {
            return "ERR";
        }

        var icCount = (int)Math.Round((moduleDensityGb * 8d) / (dieDensityGb * bitFactor), MidpointRounding.AwayFromZero);
        return rankCode == "2" ? $"{icCount} (2R)" : icCount.ToString();
    }

    private static string GetModuleSpeedText(string speedCode)
    {
        return speedCode switch
        {
            "WE" => "3200 MT/s",
            "QK" => "4800 MT/s",
            "WM" => "5600 MT/s",
            "CM" => "6000 MT/s",
            "CQ" => "6400 MT/s",
            "CR" => "6800 MT/s",
            "CS" => "7200 MT/s",
            _ => speedCode
        };
    }

    private static bool IsThirdPartyModule(string moduleSourceCode)
    {
        return moduleSourceCode is "TM" or "BM";
    }

    private static bool IsBlankCode(string code)
    {
        return string.IsNullOrWhiteSpace(code) || code == "0";
    }

    private static void ValidateRequiredCodes(params string[] requiredCodes)
    {
        if (requiredCodes.Any(IsBlankCode))
        {
            throw new InvalidOperationException("Module 생성 필수 코드가 비어 있습니다.");
        }
    }
}
