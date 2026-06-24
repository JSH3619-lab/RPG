using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Specs;

namespace RamosPartGenerator.Core.Services;

public sealed class ModuleService
{
    private const string A100Code = "A";
    private const string Ddr4RuleKey = "DDR4";
    private const string Ddr5RuleKey = "DDR5";

    private readonly SpecProvider _specProvider;
    private readonly ProductTextService _productTextService;
    private static readonly string[] GenerationCodes = Enumerable.Range('A', 26).Select(x => ((char)x).ToString()).ToArray();

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
        var isManufacturing = IsManufacturingModule(effectiveRequest.ModuleSourceCode);

        ValidateRequiredCodes(
            effectiveRequest.ModuleSourceCode,
            effectiveRequest.DramTypeCode,
            effectiveRequest.DimmTypeCode,
            effectiveRequest.ModuleDensityCode,
            effectiveRequest.CompositionCode,
            effectiveRequest.DieDensityCode,
            effectiveRequest.GenerationCode,
            effectiveRequest.CompTestCode,
            effectiveRequest.SpeedCode,
            effectiveRequest.VendorCode);
        if (!isManufacturing)
        {
            ValidateRequiredCodes(effectiveRequest.ModuleCompTypeCode);
        }

        if (IsBlankCode(effectiveRequest.IcBrandCode))
        {
            throw new InvalidOperationException("Rev 30 Module은 I.C Brand가 반드시 필요합니다.");
        }

        if (isThirdParty && IsBlankCode(effectiveRequest.PurchaserCode))
        {
            throw new InvalidOperationException("Third-Party Module은 Purchaser가 반드시 필요합니다.");
        }

        ValidateAllowedCodes(
            ("Source", effectiveRequest.ModuleSourceCode, Codes("module_source", "manufacturing_module_source"), false),
            ("DRAM Type", effectiveRequest.DramTypeCode, Codes("dram_type").Select(code => code == "A" ? "4" : code).ToArray(), false),
            ("DIMM Type", effectiveRequest.DimmTypeCode, CodesWithAdditions("dimm_type_common", "C"), false),
            ("Module Density", effectiveRequest.ModuleDensityCode, Codes("module_density"), false),
            ("Bank / VDD", effectiveRequest.BankVddCode, Codes("module_bank_vdd"), true),
            ("Die Density", effectiveRequest.DieDensityCode, Codes("module_die_density"), false),
            ("Composition", effectiveRequest.CompositionCode, ModuleCompositionCodes(isManufacturing), false),
            ("Rank", effectiveRequest.RankCode, CodesWithAdditions("rank", "0"), false),
            ("Generation", effectiveRequest.GenerationCode, GenerationCodes, false),
            ("I.C Brand", effectiveRequest.IcBrandCode, Codes("module_ic_brand"), false),
            ("Comp Type", effectiveRequest.ModuleCompTypeCode, Codes(isManufacturing ? "manufacturing_comp_type" : "comp_type"), false),
            ("Comp Test Site", effectiveRequest.CompTestCode, Codes("tester"), false),
            ("SMT Site", effectiveRequest.ModuleSmtCode, CodesWithAdditions("tester", "0"), false),
            ("Module Test Site", effectiveRequest.ModuleTestCode, CodesWithAdditions("tester", "0"), false),
            ("Speed", effectiveRequest.SpeedCode, Codes("speed_ddr4", "speed_ddr5"), false),
            ("PCB", effectiveRequest.PcbCode, Codes("pcb"), false),
            ("Vendor", effectiveRequest.VendorCode, Codes(isManufacturing ? "vendor_tm" : "vendor"), false),
            ("Purchaser", effectiveRequest.PurchaserCode, Codes("purchaser"), true),
            ("A100 Special", effectiveRequest.A100SpecialCode, Codes("a100_special"), true),
            ("Special Code 2", effectiveRequest.SpecialCode2Code, Codes("module_special_code2"), true),
            ("Special Code 3", effectiveRequest.SpecialCode3Code, Codes("module_special_code3"), true),
            ("Grade Code", effectiveRequest.GradeCode, Codes("grade_code"), true),
            ("Product Bin", effectiveRequest.ProductBinCode, Codes("product_bin"), true));
        ValidateModuleSpeedAndBankVdd(effectiveRequest);
        ValidateModuleDensityForDimmType(effectiveRequest);
        ValidateModuleDieDensity(effectiveRequest);
        ValidateModuleDensityCombination(effectiveRequest);
        ValidateA100Special(effectiveRequest);

        var basePartCode = BuildModuleBasePartCode(effectiveRequest);
        var binPartCode = BuildModuleBinPartCode(basePartCode, effectiveRequest);
        var dramTypeLabel = GetModuleDramTypeLabel(effectiveRequest.DramTypeCode);
        var formFactorLabel = GetModuleFormFactorLabel(effectiveRequest.DimmTypeCode);
        var capacityLabel = GetModuleDensityLabel(effectiveRequest.ModuleDensityCode);
        var dieDensityLabel = GetDensityLabel(effectiveRequest.DieDensityCode);
        var icCount = CalculateModuleIcCount(
            effectiveRequest.ModuleDensityCode,
            effectiveRequest.DieDensityCode,
            effectiveRequest.CompositionCode,
            effectiveRequest.RankCode);
        var speedText = GetModuleSpeedText(effectiveRequest.SpeedCode);
        var isCompSale = effectiveRequest.DimmTypeCode.Equals("C", StringComparison.OrdinalIgnoreCase);

        var baseText = _productTextService.BuildModuleTexts(
            basePartCode,
            effectiveRequest.ModuleSourceCode,
            dramTypeLabel,
            formFactorLabel,
            capacityLabel,
            dieDensityLabel,
            effectiveRequest.CompositionCode,
            icCount,
            effectiveRequest.GenerationCode,
            effectiveRequest.IcBrandCode,
            effectiveRequest.ModuleCompTypeCode,
            effectiveRequest.VendorCode,
            effectiveRequest.PurchaserCode,
            effectiveRequest.PcbCode,
            isThirdParty,
            effectiveRequest.SpecialCode2Code,
            effectiveRequest.SpecialCode3Code,
            null,
            isCompSale,
            effectiveRequest.CompTestCode,
            isManufacturing);
        var binText = _productTextService.BuildModuleTexts(
            binPartCode,
            effectiveRequest.ModuleSourceCode,
            dramTypeLabel,
            formFactorLabel,
            capacityLabel,
            dieDensityLabel,
            effectiveRequest.CompositionCode,
            icCount,
            effectiveRequest.GenerationCode,
            effectiveRequest.IcBrandCode,
            effectiveRequest.ModuleCompTypeCode,
            effectiveRequest.VendorCode,
            effectiveRequest.PurchaserCode,
            effectiveRequest.PcbCode,
            isThirdParty,
            effectiveRequest.SpecialCode2Code,
            effectiveRequest.SpecialCode3Code,
            speedText,
            isCompSale,
            effectiveRequest.CompTestCode,
            isManufacturing);

        var rows = new List<GeneratedPartRow>
        {
            new("Module", basePartCode, baseText.Name, baseText.GeneralInfo, baseText.Specification),
            new("Module BIN", binPartCode, binText.Name, binText.GeneralInfo, binText.Specification)
        };

        if (TryBuildModuleRepairDummy(
                basePartCode,
                baseText.Specification,
                effectiveRequest.SpecialCode2Code,
                out var dummyPartCode,
                out var dummySpecification))
        {
            rows.Insert(1, new GeneratedPartRow(
                "Module Dummy",
                dummyPartCode,
                dummyPartCode,
                baseText.GeneralInfo,
                dummySpecification));
        }

        return rows;
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
        var purchaserCode = tailPart.Length >= 6 ? tailPart.Substring(5, 1) : "0";

        var moduleSourceCode = MapCompFamilyToModuleFamily(compFamilyCode);
        var moduleDramTypeCode = MapCompDramTypeToModuleDramType(compDramTypeCode);
        var compositionCode = MapCompBitToModuleComposition(compBitCode);
        var dieDensityCode = MapCompDensityToModuleDieDensity(compDensityCode);
        if (string.IsNullOrWhiteSpace(moduleSourceCode) ||
            string.IsNullOrWhiteSpace(moduleDramTypeCode) ||
            string.IsNullOrWhiteSpace(compositionCode) ||
            string.IsNullOrWhiteSpace(dieDensityCode))
        {
            throw new InvalidOperationException("Comp Full Part에서 Module 기본값을 자동 매핑할 수 없습니다.");
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
            IcBrandCode = dieBrandCode,
            ModuleCompTypeCode = compTypeCode,
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
            BankVddCode = headPart.Substring(6, 1),
            PcbCode = string.Empty,
            CompositionCode = headPart.Substring(7, 1),
            DieDensityCode = headPart.Substring(8, 1),
            RankCode = headPart.Substring(9, 1),
            GenerationCode = headPart.Substring(10, 1),
            GradeCode = string.IsNullOrEmpty(binTail) ? string.Empty : binTail[..2],
            ProductBinCode = string.IsNullOrEmpty(binTail) ? string.Empty : binTail[^3..]
        };

        if (tailPart.Length < 9)
        {
            throw new InvalidOperationException($"Rev {revisionSpec.DisplayRevision} Module tail 길이가 부족합니다.");
        }

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

        if (IsA100SpecialEligible(request) &&
            !string.IsNullOrEmpty(trailingText) &&
            IsA100SpecialCode(trailingText[..1]))
        {
            request.A100SpecialCode = trailingText[..1];
            trailingText = trailingText[1..];
        }

        if (!string.IsNullOrEmpty(trailingText) && IsSpecialCode2(trailingText[..1]))
        {
            request.SpecialCode2Code = trailingText[..1];
            trailingText = trailingText[1..];
        }

        if (!string.IsNullOrEmpty(trailingText) && IsSpecialCode3(trailingText[..1]))
        {
            request.SpecialCode3Code = trailingText[..1];
            trailingText = trailingText[1..];
        }

        if (!string.IsNullOrEmpty(trailingText))
        {
            throw new InvalidOperationException($"Rev 30 Module tail에 해석되지 않은 코드가 남아 있습니다: {trailingText}");
        }

        ValidateModuleSpeedAndBankVdd(request);
        ValidateModuleDensityForDimmType(request);
        ValidateModuleDieDensity(request);
        ValidateModuleDensityCombination(request);

        return request;
    }

    private static string NormalizeCode(string? partCode)
    {
        var value = (partCode ?? string.Empty).Trim();
        var separatorIndex = value.IndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex > -1)
        {
            value = value[..separatorIndex].Trim();
        }

        value = value.ToUpperInvariant().Replace(" ", string.Empty);
        return value is "" or "0" or "(없음)" or "(NONE)" or "NONE" ? "0" : value;
    }

    private static string MapCompFamilyToModuleFamily(string compFamilyCode)
    {
        return compFamilyCode switch
        {
            "RC" => "RM",
            "TC" => "TM",
            "CC" => "CM",
            "BC" => "BM",
            "XC" => "XM",
            "ZC" => "ZM",
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
            "48" => "9",
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

    private static bool IsA100SpecialEligible(ModuleRequest request)
    {
        return IsThirdPartyModule(request.ModuleSourceCode) &&
               IsA100Code(request.CompTestCode) &&
               IsA100Code(request.VendorCode) &&
               IsA100Code(request.PurchaserCode);
    }

    private static bool IsA100Code(string code)
    {
        return code.Equals(A100Code, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsA100SpecialCode(string code)
    {
        return Codes("a100_special").Contains(code, StringComparer.OrdinalIgnoreCase);
    }

    private bool IsSpecialCode2(string code)
    {
        return Codes("module_special_code2").Contains(code, StringComparer.OrdinalIgnoreCase);
    }

    private bool IsSpecialCode3(string code)
    {
        return Codes("module_special_code3").Contains(code, StringComparer.OrdinalIgnoreCase);
    }

    private ModuleRequest BuildEffectiveRequest(ModuleRequest request, RevisionSpec revisionSpec)
    {
        var effective = NormalizeRequest(request);

        if (!IsBlankCode(effective.CompFullPartCode))
        {
            var parsedComp = ParseCompPart(revisionSpec.Revision, effective.CompFullPartCode);
            effective = MergeRequests(parsedComp, effective);
        }

        if (!IsBlankCode(effective.ModuleFullPartCode))
        {
            var parsedModule = ParseModuleFullPart(revisionSpec.Revision, effective.ModuleFullPartCode);
            effective = MergeRequests(effective, parsedModule);
        }

        ApplyCompSaleDefaults(effective);
        return NormalizeRequest(effective);
    }

    private static void ApplyCompSaleDefaults(ModuleRequest request)
    {
        if (!request.DimmTypeCode.Equals("C", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var moduleDensityCode = GetCompSaleModuleDensityCode(request.DieDensityCode);
        if (IsBlankCode(request.ModuleDensityCode) && !string.IsNullOrEmpty(moduleDensityCode))
        {
            request.ModuleDensityCode = moduleDensityCode;
        }

        if (IsBlankCode(request.RankCode))
        {
            request.RankCode = "0";
        }

        if (IsBlankCode(request.ModuleSmtCode))
        {
            request.ModuleSmtCode = "0";
        }

        if (IsBlankCode(request.ModuleTestCode))
        {
            request.ModuleTestCode = "0";
        }

        if (IsBlankCode(request.PcbCode))
        {
            request.PcbCode = "0";
        }
    }

    public static string GetCompSaleModuleDensityCode(string dieDensityCode)
    {
        return NormalizeCode(dieDensityCode) switch
        {
            "8" => "1G",
            "A" => "2G",
            "B" => "4G",
            "C" => "8G",
            _ => string.Empty
        };
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
            BankVddCode = NormalizeCode(request.BankVddCode),
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
            BankVddCode = PreferValue(parsedRequest.BankVddCode, overrideRequest.BankVddCode),
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

    private string BuildModuleBasePartCode(ModuleRequest request)
    {
        var bankVddCode = IsBlankCode(request.BankVddCode)
            ? GetModuleBankVddCode(request.DramTypeCode, request.SpeedCode)
            : request.BankVddCode;
        if (IsBlankCode(bankVddCode))
        {
            throw new InvalidOperationException("선택한 Speed에 맞는 Module Bank/VDD 코드를 계산할 수 없습니다.");
        }

        var basePartCode = request.ModuleSourceCode + request.DramTypeCode + request.DimmTypeCode + request.ModuleDensityCode + bankVddCode + request.CompositionCode + request.DieDensityCode + request.RankCode + request.GenerationCode;

        basePartCode += "-" + request.IcBrandCode + request.ModuleCompTypeCode + request.CompTestCode + request.ModuleSmtCode + request.ModuleTestCode + request.SpeedCode + request.PcbCode + request.VendorCode;

        if (!IsBlankCode(request.PurchaserCode))
        {
            basePartCode += request.PurchaserCode;
        }

        if (!IsBlankCode(request.A100SpecialCode))
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

    private static bool TryBuildModuleRepairDummy(
        string basePartCode,
        string baseSpecification,
        string specialCode2Code,
        out string dummyPartCode,
        out string dummySpecification)
    {
        dummyPartCode = string.Empty;
        dummySpecification = string.Empty;

        if (!TryGetModuleRepairDummyRule(
                specialCode2Code,
                out var sourceStatusLabel,
                out var replacementSuffix,
                out var dummyStatusLabel) ||
            !basePartCode.EndsWith(specialCode2Code, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        dummyPartCode = basePartCode[..^specialCode2Code.Length] + replacementSuffix;
        var specificationCore = RemoveTrailingStatusLabel(baseSpecification, sourceStatusLabel);
        dummySpecification = string.Join(
            " ",
            new[] { specificationCore, dummyStatusLabel }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return true;
    }

    private static bool TryGetModuleRepairDummyRule(
        string specialCode2Code,
        out string sourceStatusLabel,
        out string replacementSuffix,
        out string dummyStatusLabel)
    {
        (sourceStatusLabel, replacementSuffix, dummyStatusLabel) = specialCode2Code switch
        {
            "R" => ("1st Repair", "00", "1st Repair Dummy"),
            "S" => ("2nd Repair", "R0", "2nd Repair Dummy"),
            "C" => ("Reball Repair", "B0", "Reball Repair Dummy"),
            _ => (string.Empty, string.Empty, string.Empty)
        };

        return !string.IsNullOrWhiteSpace(dummyStatusLabel);
    }

    private static string RemoveTrailingStatusLabel(string specification, string statusLabel)
    {
        var trimmed = specification.Trim();
        return trimmed.EndsWith(statusLabel, StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^statusLabel.Length].TrimEnd()
            : trimmed;
    }

    private string GetModuleBankVddCode(string dramTypeCode, string speedCode)
    {
        return TryGetModuleSpeedRule(dramTypeCode, out _, out var rule) &&
               rule.BankVddBySpeed.TryGetValue(speedCode, out var bankVddCode)
            ? bankVddCode
            : string.Empty;
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
            "1G" => "1GB",
            "2G" => "2GB",
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

    private static string CalculateModuleIcCount(string moduleDensityCode, string dieDensityCode, string compositionCode, string rankCode)
    {
        if (!TryGetCompositionWidth(compositionCode, out var compositionWidth) ||
            !TryGetRankCount(rankCode, out var rankCount))
        {
            return "ERR";
        }

        var icCount = 64 / compositionWidth * rankCount;
        return rankCode == "2" ? $"{icCount} (2R)" : icCount.ToString();
    }

    private string GetModuleSpeedText(string speedCode)
    {
        var option = OptionsForSpeed()
            .FirstOrDefault(item => ExtractCode(item).Equals(speedCode, StringComparison.OrdinalIgnoreCase));
        if (option is null)
        {
            return speedCode;
        }

        var separatorIndex = option.IndexOf(" - ", StringComparison.Ordinal);
        return separatorIndex > -1 ? option[(separatorIndex + 3)..].Trim() : speedCode;
    }

    private static bool IsThirdPartyModule(string moduleSourceCode)
    {
        return moduleSourceCode is "TM" or "BM";
    }

    private static bool IsManufacturingModule(string moduleSourceCode)
    {
        return moduleSourceCode is "XM" or "ZM";
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

    private static void ValidateAllowedCodes(params (string FieldName, string Code, IReadOnlyCollection<string> AllowedCodes, bool AllowBlank)[] checks)
    {
        foreach (var (fieldName, code, allowedCodes, allowBlank) in checks)
        {
            if (allowBlank && IsBlankCode(code))
            {
                continue;
            }

            if (!allowedCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{fieldName} 코드가 허용 목록에 없습니다: {code}");
            }
        }
    }

    private IReadOnlyCollection<string> Codes(params string[] optionKeys)
    {
        return optionKeys
            .SelectMany(key => _specProvider.SharedSpec.CodeOptions.TryGetValue(key, out var options)
                ? options.Select(ExtractCode)
                : throw new KeyNotFoundException($"Code option set '{key}' was not found."))
            .ToArray();
    }

    private IReadOnlyCollection<string> CodesWithAdditions(string optionKey, params string[] additions)
    {
        return Codes(optionKey).Concat(additions).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private IReadOnlyCollection<string> ModuleCompositionCodes(bool isManufacturing)
    {
        var optionKeys = isManufacturing ? new[] { "bit", "bit_tm" } : new[] { "bit" };
        return Codes(optionKeys)
            .Select(code => code switch
            {
                "04" => "4",
                "08" => "8",
                "16" => "6",
                "48" => "9",
                _ => code.TrimStart('0')
            })
            .ToArray();
    }

    private static string ExtractCode(string option)
    {
        return NormalizeCode(option);
    }

    private IReadOnlyCollection<string> OptionsForSpeed()
    {
        return _specProvider.SharedSpec.CodeOptions.TryGetValue("speed_ddr4", out var ddr4Options) &&
               _specProvider.SharedSpec.CodeOptions.TryGetValue("speed_ddr5", out var ddr5Options)
            ? ddr4Options.Concat(ddr5Options).ToArray()
            : Array.Empty<string>();
    }

    private static bool TryGetModuleDensityGb(string moduleDensityCode, out double densityGb)
    {
        densityGb = moduleDensityCode switch
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

        return densityGb > 0;
    }

    private static bool TryGetDieDensityGb(string dieDensityCode, out double densityGb)
    {
        densityGb = dieDensityCode switch
        {
            "4" => 4d,
            "8" => 8d,
            "A" => 16d,
            "H" => 24d,
            "B" => 32d,
            _ => 0d
        };

        return densityGb > 0;
    }

    private static bool TryGetCompositionWidth(string compositionCode, out int width)
    {
        width = compositionCode switch
        {
            "4" => 4,
            "8" or "9" => 8,
            "6" => 16,
            _ => 0
        };

        return width > 0;
    }

    private static bool TryGetRankCount(string rankCode, out int rankCount)
    {
        rankCount = rankCode switch
        {
            "1" => 1,
            "2" => 2,
            _ => 0
        };

        return rankCount > 0;
    }

    private static void ValidateModuleDieDensity(ModuleRequest request)
    {
        var valid = request.DramTypeCode switch
        {
            "4" => request.DieDensityCode is "4" or "8" or "A",
            "R" => request.DieDensityCode is "A" or "H" or "B",
            _ => false
        };

        if (!valid)
        {
            throw new InvalidOperationException(request.DramTypeCode switch
            {
                "4" => "DDR4 Module은 Die Density 4Gb / 8Gb / 16Gb만 허용됩니다.",
                "R" => "DDR5 Module은 Die Density 16Gb / 24Gb / 32Gb만 허용됩니다.",
                _ => "지원하지 않는 Module DRAM Type입니다."
            });
        }
    }

    private static void ValidateModuleDensityForDimmType(ModuleRequest request)
    {
        if (request.ModuleDensityCode is "1G" or "2G" &&
            !request.DimmTypeCode.Equals("C", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Module Density 1GB / 2GB는 DIMM Type Comp에서만 사용할 수 있습니다.");
        }
    }

    private static void ValidateModuleDensityCombination(ModuleRequest request)
    {
        if (request.DimmTypeCode.Equals("C", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!TryGetModuleDensityGb(request.ModuleDensityCode, out var selectedModuleGb) ||
            !TryGetDieDensityGb(request.DieDensityCode, out var dieDensityGb) ||
            !TryGetCompositionWidth(request.CompositionCode, out var compositionWidth) ||
            !TryGetRankCount(request.RankCode, out var rankCount))
        {
            throw new InvalidOperationException("Module Density/Die Density/Composition/Rank 조합을 계산할 수 없습니다.");
        }

        var calculatedModuleGb = dieDensityGb * (64d / compositionWidth) * rankCount / 8d;
        if (Math.Abs(selectedModuleGb - calculatedModuleGb) > 0.001d)
        {
            throw new InvalidOperationException($"Module Density/Die Density/Composition/Rank 조합이 맞지 않습니다. 선택값은 {FormatGb(selectedModuleGb)}이지만 계산값은 {FormatGb(calculatedModuleGb)}입니다.");
        }
    }

    private static string FormatGb(double value)
    {
        return Math.Abs(value - Math.Round(value)) < 0.001d
            ? $"{Math.Round(value):0}GB"
            : $"{value:0.##}GB";
    }

    private void ValidateModuleSpeedAndBankVdd(ModuleRequest request)
    {
        if (!TryGetModuleSpeedRule(request.DramTypeCode, out var ruleKey, out var rule))
        {
            throw new InvalidOperationException($"지원하지 않는 Module DRAM Type입니다: {request.DramTypeCode}");
        }

        if (!rule.AllowedSpeeds.Contains(request.SpeedCode, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{ruleKey} Module은 Speed {string.Join(" / ", rule.AllowedSpeeds)}만 허용됩니다.");
        }

        var expectedBankVdd = GetModuleBankVddCode(request.DramTypeCode, request.SpeedCode);
        if (!IsBlankCode(request.BankVddCode) &&
            !request.BankVddCode.Equals(expectedBankVdd, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{ruleKey} Module의 선택 Speed는 Bank/VDD code {expectedBankVdd}만 허용됩니다.");
        }
    }

    private bool TryGetModuleSpeedRule(string dramTypeCode, out string ruleKey, out ModuleSpeedRule rule)
    {
        ruleKey = GetModuleSpeedRuleKey(dramTypeCode);
        if (string.IsNullOrEmpty(ruleKey))
        {
            rule = new ModuleSpeedRule();
            return false;
        }

        if (_specProvider.SharedSpec.ModuleSpeedRules.TryGetValue(ruleKey, out var speedRule))
        {
            rule = speedRule;
            return true;
        }

        rule = new ModuleSpeedRule();
        return false;
    }

    private static string GetModuleSpeedRuleKey(string dramTypeCode)
    {
        return dramTypeCode switch
        {
            "4" => Ddr4RuleKey,
            "R" => Ddr5RuleKey,
            _ => string.Empty
        };
    }

    private static void ValidateA100Special(ModuleRequest request)
    {
        if (IsBlankCode(request.A100SpecialCode))
        {
            return;
        }

        if (!IsA100SpecialEligible(request))
        {
            throw new InvalidOperationException("A100 Special Code는 Third-Party + Comp Test Site A + Vendor A + Purchaser A 조건에서만 사용할 수 있습니다.");
        }
    }
}
