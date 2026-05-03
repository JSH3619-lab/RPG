using RamosPartGenerator.Core.Models;

namespace RamosPartGenerator.Core.Services;

public sealed class IncomingCompService
{
    private readonly SpecProvider _specProvider;
    private readonly ProductTextService _productTextService;
    private static readonly string[] PartRevisionCodes = Enumerable.Range('A', 26).Select(x => ((char)x).ToString()).ToArray();

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
        ValidateAllowedCodes(
            ("Source", sourceCode, Codes("incoming_source"), false),
            ("DRAM Type", dramTypeCode, Codes("dram_type"), false),
            ("Bit", bitOrganizationCode, Codes("bit"), false),
            ("Bank", bankCode, Codes("bank_ddr4", "bank_ddr5"), false),
            ("Interface", interfaceCode, Codes("interface_ddr4", "interface_ddr5"), false),
            ("Part Revision", partRevisionCode, PartRevisionCodes, false),
            ("Comp Type", compTypeCode, Codes("comp_type"), false),
            ("Die Brand", dieBrandCode, Codes("die_brand"), false),
            ("Vendor", vendorCode, Codes("vendor"), false),
            ("Purchaser", purchaserCode, Codes("purchaser"), true),
            ("Comp Type 2", compType2Code, Codes("comp_type2"), true),
            ("Package", packageTypeCode, Codes("package_type"), false),
            ("Tester", testerCode, Codes("tester"), false));
        ValidateDramDefaults(dramTypeCode, bankCode, interfaceCode);
        ValidateRev30Fields(isThirdParty, vendorCode, purchaserCode);

        var dddPartCode = BuildIncomingPartCode(
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
        var compTypeLabel = ProductTextService.GetCompTypeDescription(compTypeCode);

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
                rows.Add(new("Comp BIN", code, text.Name, text.GeneralInfo, text.Specification));
            }
        }
        else
        {
            var speed = _specProvider.SharedSpec.CompBinSpeedMap["CA"];
            var code = $"{compPartCode}-CA";
            var text = _productTextService.BuildIncomingCompTexts(
                code, dramTypeLabel, densityLabel, bitLabel, dieBrandLabel, compTypeLabel, isThirdParty, speed);
            rows.Add(new("Comp BIN", code, text.Name, text.GeneralInfo, text.Specification));
        }

        return rows;
    }


    public IncomingCompRequest ParseCompPart(string revision, string partCode)
    {
        var revisionSpec = _specProvider.GetRevisionSpec(revision);
        var normalizedPartCode = (partCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedPartCode))
        {
            throw new InvalidOperationException("Comp Full Part가 비어 있습니다.");
        }

        var separatorIndex = normalizedPartCode.IndexOf('-');
        if (separatorIndex <= 0 || separatorIndex >= normalizedPartCode.Length - 1)
        {
            throw new InvalidOperationException("Comp Full Part 형식이 잘못되었습니다. '-' 위치를 확인하세요.");
        }

        var prefix = normalizedPartCode[..separatorIndex];
        var tail = normalizedPartCode[(separatorIndex + 1)..];

        if (prefix.Length != 10)
        {
            throw new InvalidOperationException("Comp Full Part prefix 길이가 올바르지 않습니다.");
        }

        if (tail.Length < 4)
        {
            throw new InvalidOperationException("Comp Full Part tail 길이가 올바르지 않습니다.");
        }

        var compFamily = prefix[..2];
        var sourceCode = MapCompToIncomingFamily(compFamily);
        var dramTypeCode = prefix.Substring(2, 1);
        var densityCode = prefix.Substring(3, 2);
        var bitOrganizationCode = prefix.Substring(5, 2);
        var bankCode = prefix.Substring(7, 1);
        var interfaceCode = prefix.Substring(8, 1);
        var revisionCode = prefix.Substring(9, 1);

        var compTypeCode = tail.Substring(0, 1);
        var packageTypeCode = tail.Substring(1, 1);
        var dieBrandCode = tail.Substring(2, 1);
        var testerCode = tail.Substring(3, 1);
        var remaining = tail.Length > 4 ? tail[4..] : string.Empty;

        var vendorCode = "0";
        var purchaserCode = "0";
        var compType2Code = "0";

        if (remaining.Length < 1)
        {
            throw new InvalidOperationException("Rev 30 Comp Full Part에는 Vendor가 필요합니다.");
        }

        vendorCode = remaining.Substring(0, 1);
        remaining = remaining.Length > 1 ? remaining[1..] : string.Empty;

        if (remaining.Length == 1)
        {
            if (IsPurchaserCode(remaining))
            {
                purchaserCode = remaining;
            }
            else
            {
                compType2Code = remaining;
            }
        }
        else if (remaining.Length >= 2)
        {
            purchaserCode = remaining.Substring(0, 1);
            compType2Code = remaining.Substring(1, 1);
        }

        ValidateDensity(dramTypeCode, densityCode);

        return new IncomingCompRequest
        {
            Revision = revisionSpec.Revision,
            SourceCode = sourceCode,
            DramTypeCode = dramTypeCode,
            DensityCode = densityCode,
            BitOrganizationCode = bitOrganizationCode,
            BankCode = bankCode,
            InterfaceCode = interfaceCode,
            RevisionCode = revisionCode,
            CompTypeCode = compTypeCode,
            DieBrandCode = dieBrandCode,
            VendorCode = vendorCode,
            PurchaserCode = purchaserCode,
            CompType2Code = compType2Code,
            PackageTypeCode = packageTypeCode,
            TesterCode = testerCode
        };
    }
    private bool IsThirdPartyIncoming(string sourceCode)
    {
        return _specProvider.SharedSpec.Families.TryGetValue("incoming_third_party", out var familyCodes)
            && familyCodes.Contains(sourceCode, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeCode(string? code)
    {
        var value = (code ?? string.Empty).Trim();
        var separatorIndex = value.IndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex > -1)
        {
            value = value[..separatorIndex].Trim();
        }

        value = value.ToUpperInvariant();
        return value is "" or "0" or "(없음)" or "(NONE)" or "NONE" ? "0" : value;
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

    private static void ValidateRev30Fields(bool isThirdParty, string vendorCode, string purchaserCode)
    {
        if (IsBlankCode(vendorCode))
        {
            throw new InvalidOperationException("Rev 30에서는 Vendor가 반드시 필요합니다.");
        }

        if (isThirdParty && IsBlankCode(purchaserCode))
        {
            throw new InvalidOperationException("Third-Party Comp는 Purchaser가 반드시 필요합니다.");
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
                ? options.Select(ExtractCode).Where(code => !IsBlankCode(code))
                : throw new KeyNotFoundException($"Code option set '{key}' was not found."))
            .ToArray();
    }

    private static string ExtractCode(string option)
    {
        return NormalizeCode(option);
    }

    private static void ValidateDramDefaults(string dramTypeCode, string bankCode, string interfaceCode)
    {
        if (dramTypeCode == "A" && (bankCode != "5" || interfaceCode != "W"))
        {
            throw new InvalidOperationException("DDR4는 Bank 5 / Interface W만 허용됩니다.");
        }

        if (dramTypeCode == "R" && (bankCode != "6" || interfaceCode != "V"))
        {
            throw new InvalidOperationException("DDR5는 Bank 6 / Interface V만 허용됩니다.");
        }
    }

    private static string BuildIncomingPartCode(
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

        code += "-" + compTypeCode + dieBrandCode + vendorCode + "EL";
        if (!IsBlankCode(purchaserCode))
        {
            code += purchaserCode;
        }

        if (!IsBlankCode(compType2Code))
        {
            code += compType2Code;
        }

        return code;
    }

    private static string BuildCompPartCode(
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

        code += vendorCode;
        if (!IsBlankCode(purchaserCode))
        {
            code += purchaserCode;
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


    private static string MapCompToIncomingFamily(string compFamily)
    {
        return compFamily switch
        {
            "RC" => "K",
            "TC" => "T",
            "CC" => "C",
            "BC" => "B",
            _ => throw new InvalidOperationException($"지원하지 않는 Comp Family입니다: {compFamily}")
        };
    }

    private static bool IsPurchaserCode(string code)
    {
        return code is "V" or "H" or "A" or "0";
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


