using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Specs;

namespace RamosPartGenerator.Core.Services;

public sealed class IncomingCompService
{
    private readonly SpecProvider _specProvider;
    private readonly ProductTextService _productTextService;

    public IncomingCompService(SpecProvider specProvider, ProductTextService productTextService)
    {
        _specProvider = specProvider;
        _productTextService = productTextService;
    }

    public IReadOnlyList<GeneratedPartRow> GeneratePreview(IncomingCompRequest request)
    {
        var revisionSpec = _specProvider.GetRevisionSpec(request.Revision);
        var sourceCode = NormalizeCode(request.SourceCode);
        var dramTypeCode = NormalizeCode(request.DramTypeCode);
        var densityCode = NormalizeCode(request.DensityCode);
        var bitOrganizationCode = NormalizeCode(request.BitOrganizationCode);
        var bankCode = NormalizeCode(request.BankCode);
        var interfaceCode = NormalizeCode(request.InterfaceCode);
        var partRevisionCode = NormalizeCode(request.RevisionCode);
        var compTypeCode = NormalizeCode(request.CompTypeCode);
        var dieBrandCode = NormalizeCode(request.DieBrandCode);
        var vendorCode = NormalizeCode(request.VendorCode);
        var purchaserCode = NormalizeCode(request.PurchaserCode);
        var compType2Code = NormalizeCode(request.CompType2Code);
        var packageTypeCode = NormalizeCode(request.PackageTypeCode);
        var testerCode = NormalizeCode(request.TesterCode);

        var isThirdParty = IsThirdPartyIncoming(sourceCode);
        ValidateRequiredCodes(
            sourceCode,
            dramTypeCode,
            densityCode,
            bitOrganizationCode,
            bankCode,
            interfaceCode,
            partRevisionCode,
            compTypeCode,
            dieBrandCode,
            packageTypeCode,
            testerCode);
        ValidateDensity(dramTypeCode, densityCode);
        ValidateRevisionSpecificFields(revisionSpec, isThirdParty, vendorCode, purchaserCode);

        var dddPartCode = BuildIncomingPartCode(
            revisionSpec,
            dramTypeCode,
            densityCode,
            bitOrganizationCode,
            bankCode,
            interfaceCode,
            partRevisionCode,
            compTypeCode,
            dieBrandCode,
            sourceCode,
            vendorCode,
            purchaserCode,
            compType2Code);
        var compPartCode = BuildCompPartCode(
            revisionSpec,
            dramTypeCode,
            densityCode,
            bitOrganizationCode,
            bankCode,
            interfaceCode,
            partRevisionCode,
            compTypeCode,
            packageTypeCode,
            dieBrandCode,
            testerCode,
            sourceCode,
            vendorCode,
            purchaserCode,
            compType2Code);

        var dramTypeLabel = dramTypeCode == "R" ? "DDR5" : "DDR4";
        var densityLabel = GetDensityLabel(densityCode);
        var bitLabel = $"x{bitOrganizationCode.TrimStart('0')}";
        var dieBrandLabel = $"{dieBrandCode}-die";
        var compTypeLabel = compTypeCode;

        var incomingTexts = _productTextService.BuildIncomingCompTexts(
            dddPartCode, dramTypeLabel, densityLabel, bitLabel, dieBrandLabel, compTypeLabel, isThirdParty);
        var compTexts = _productTextService.BuildIncomingCompTexts(
            compPartCode, dramTypeLabel, densityLabel, bitLabel, dieBrandLabel, compTypeLabel, isThirdParty);

        var rows = new List<GeneratedPartRow>
        {
            new("입고", dddPartCode, incomingTexts.Name, incomingTexts.GeneralInfo, incomingTexts.Specification),
            new("Comp", compPartCode, compTexts.Name, compTexts.GeneralInfo, compTexts.Specification)
        };

        if (dramTypeCode == "R")
        {
            foreach (var pair in _specProvider.SharedSpec.CompBinSpeedMap)
            {
                var code = $"{compPartCode}-{pair.Key}";
                var text = _productTextService.BuildIncomingCompTexts(
                    code, dramTypeLabel, densityLabel, bitLabel, dieBrandLabel, compTypeLabel, isThirdParty, pair.Value);
                rows.Add(new("Comp BIN", code, text.Name, text.GeneralInfo, text.Specification, pair.Value));
            }
        }
        else
        {
            var speed = _specProvider.SharedSpec.CompBinSpeedMap["CA"];
            var code = $"{compPartCode}-CA";
            var text = _productTextService.BuildIncomingCompTexts(
                code, dramTypeLabel, densityLabel, bitLabel, dieBrandLabel, compTypeLabel, isThirdParty, speed);
            rows.Add(new("Comp BIN", code, text.Name, text.GeneralInfo, text.Specification, speed));
        }

        return rows;
    }

    private bool IsThirdPartyIncoming(string sourceCode)
    {
        return _specProvider.SharedSpec.Families.TryGetValue("incoming_third_party", out var familyCodes)
            && familyCodes.Contains(sourceCode, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeCode(string? code)
    {
        var value = (code ?? string.Empty).Trim().ToUpperInvariant();
        return value is "" or "0" or "(없음)" ? "0" : value;
    }

    private static bool IsBlankCode(string code) => code == "0";

    private static void ValidateRequiredCodes(params string[] requiredCodes)
    {
        if (requiredCodes.Any(IsBlankCode))
        {
            throw new InvalidOperationException("입고/Comp 생성 필수 코드가 비어 있습니다.");
        }
    }

    private static void ValidateDensity(string dramTypeCode, string densityCode)
    {
        var valid = dramTypeCode switch
        {
            "A" => densityCode is "4G" or "8G" or "AG",
            "R" => densityCode is "AH" or "HE" or "BH",
            _ => false
        };

        if (!valid)
        {
            throw new InvalidOperationException(dramTypeCode switch
            {
                "A" => "DDR4: Density는 4G / 8G / AG만 허용됩니다.",
                "R" => "DDR5: Density는 AH / HE / BH만 허용됩니다.",
                _ => "지원하지 않는 DRAM Type입니다."
            });
        }
    }

    private static void ValidateRevisionSpecificFields(RevisionSpec revisionSpec, bool isThirdParty, string vendorCode, string purchaserCode)
    {
        if (revisionSpec.Revision == "30")
        {
            if (IsBlankCode(vendorCode))
            {
                throw new InvalidOperationException("Rev 30에서는 Vendor가 반드시 필요합니다.");
            }

            if (isThirdParty && IsBlankCode(purchaserCode))
            {
                throw new InvalidOperationException("Third-Party Comp는 Purchaser가 반드시 필요합니다.");
            }

            return;
        }

        if (isThirdParty && IsBlankCode(vendorCode))
        {
            throw new InvalidOperationException("Rev 27 Third-Party는 Vendor(For Third-party)가 반드시 필요합니다.");
        }
    }

    private static string BuildIncomingPartCode(
        RevisionSpec revisionSpec,
        string dramTypeCode,
        string densityCode,
        string bitOrganizationCode,
        string bankCode,
        string interfaceCode,
        string partRevisionCode,
        string compTypeCode,
        string dieBrandCode,
        string sourceCode,
        string vendorCode,
        string purchaserCode,
        string compType2Code)
    {
        var code = sourceCode
            + "4"
            + dramTypeCode
            + densityCode
            + bitOrganizationCode
            + bankCode
            + interfaceCode
            + partRevisionCode;

        if (revisionSpec.Revision == "30")
        {
            code += "-" + compTypeCode + dieBrandCode + vendorCode + "EL";
            if (!IsBlankCode(purchaserCode))
            {
                code += purchaserCode;
            }
        }
        else
        {
            code += "-" + compTypeCode + dieBrandCode + "EL";
            if (!IsBlankCode(vendorCode))
            {
                code += vendorCode;
            }
        }

        if (!IsBlankCode(compType2Code))
        {
            code += compType2Code;
        }

        return code;
    }

    private static string BuildCompPartCode(
        RevisionSpec revisionSpec,
        string dramTypeCode,
        string densityCode,
        string bitOrganizationCode,
        string bankCode,
        string interfaceCode,
        string partRevisionCode,
        string compTypeCode,
        string packageTypeCode,
        string dieBrandCode,
        string testerCode,
        string sourceCode,
        string vendorCode,
        string purchaserCode,
        string compType2Code)
    {
        var code = MapIncomingToCompFamily(sourceCode)
            + dramTypeCode
            + densityCode
            + bitOrganizationCode
            + bankCode
            + interfaceCode
            + partRevisionCode
            + "-"
            + compTypeCode
            + packageTypeCode
            + dieBrandCode
            + testerCode;

        if (revisionSpec.Revision == "30")
        {
            code += vendorCode;
            if (!IsBlankCode(purchaserCode))
            {
                code += purchaserCode;
            }
        }
        else if (!IsBlankCode(vendorCode))
        {
            code += vendorCode;
        }

        if (!IsBlankCode(compType2Code))
        {
            code += compType2Code;
        }

        return code;
    }

    private static string MapIncomingToCompFamily(string sourceCode)
    {
        return sourceCode switch
        {
            "K" => "RC",
            "T" => "TC",
            "C" => "CC",
            "B" => "BC",
            _ => throw new InvalidOperationException($"지원하지 않는 입고 SourceCode입니다: {sourceCode}")
        };
    }

    private static string GetDensityLabel(string densityCode)
    {
        return densityCode switch
        {
            "4G" => "4Gb",
            "8G" => "8Gb",
            "AG" => "16Gb",
            "AH" => "16Gb",
            "HE" => "24Gb",
            "BH" => "32Gb",
            _ => densityCode
        };
    }
}
