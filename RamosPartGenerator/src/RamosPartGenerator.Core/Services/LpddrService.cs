using RamosPartGenerator.Core.Models;

namespace RamosPartGenerator.Core.Services;

public sealed class LpddrService
{
    private readonly SpecProvider _specProvider;
    private static readonly string[] PartRevisionCodes = Enumerable.Range('A', 26).Select(x => ((char)x).ToString()).ToArray();

    public LpddrService(SpecProvider specProvider)
    {
        _specProvider = specProvider;
    }

    public IReadOnlyList<GeneratedPartRow> GeneratePreview(LpddrRequest request)
    {
        var sourceCode = NormalizeCode(request.SourceCode);
        var dramTypeCode = NormalizeCode(request.DramTypeCode);
        var densityCode = NormalizeCode(request.DensityCode);
        var dieDensityCode = NormalizeCode(request.DieDensityCode);
        var dieBitCode = NormalizeCode(request.DieBitCode);
        var bitCode = NormalizeCode(request.BitCode);
        var bankCode = NormalizeCode(request.BankCode);
        var interfaceCode = NormalizeCode(request.InterfaceCode);
        var stackCode = NormalizeCode(request.StackCode);
        var partRevisionCode = NormalizeCode(request.RevisionCode);
        var dieTypeCode = NormalizeCode(request.DieTypeCode);
        var packageTypeCode = NormalizeCode(request.PackageTypeCode);
        var dieBrandCode = NormalizeCode(request.DieBrandCode);
        var assemblyCode = NormalizeCode(request.AssemblyCode);
        var testerCode = NormalizeCode(request.TesterCode);
        var speedCode = NormalizeCode(request.SpeedCode);
        var pcbRevisionCode = NormalizeCode(request.PcbRevisionCode);
        var vendorCode = NormalizeCode(request.VendorCode);
        var purchaserCode = NormalizeCode(request.PurchaserCode);

        ValidateRequiredCodes(
            sourceCode,
            dramTypeCode,
            densityCode,
            bitCode,
            bankCode,
            interfaceCode,
            partRevisionCode,
            dieTypeCode,
            packageTypeCode,
            dieBrandCode,
            testerCode,
            speedCode,
            vendorCode);
        if (request.IsDieIncoming)
        {
            ValidateRequiredCodes(dieDensityCode, dieBitCode);
        }

        // 조립(0)/Stack(0)/PCB Rev은 "0"이 유효 코드이므로 필수 체크와 분리해서 확인한다.
        if (string.IsNullOrWhiteSpace(request.StackCode))
        {
            throw new InvalidOperationException("LPDDR Stack code is empty.");
        }

        if (string.IsNullOrWhiteSpace(request.PcbRevisionCode))
        {
            pcbRevisionCode = "1";
        }

        if (string.IsNullOrWhiteSpace(request.AssemblyCode))
        {
            throw new InvalidOperationException("LPDDR 조립 업체 code is empty.");
        }

        ValidateAllowedCodes(
            ("Source", sourceCode, Codes("lpddr_incoming_source"), false),
            ("LPDDR Type", dramTypeCode, Codes("lpddr_dram_type"), false),
            ("Density", densityCode, Codes("lpddr_density"), false),
            ("Die Density", dieDensityCode, Codes("lpddr_die_density"), !request.IsDieIncoming),
            ("Die Bit", dieBitCode, Codes("lpddr_bit"), !request.IsDieIncoming),
            ("Bit", bitCode, Codes("lpddr_bit"), false),
            ("Bank", bankCode, Codes("lpddr_bank"), false),
            ("Interface", interfaceCode, Codes("lpddr_interface"), false),
            ("Stack", stackCode, StackCodes, false),
            ("Part Revision", partRevisionCode, PartRevisionCodes, false),
            ("Die Type", dieTypeCode, Codes("lpddr_die_type"), false),
            ("Package", packageTypeCode, Codes("lpddr_package_type"), false),
            ("Die Brand", dieBrandCode, Codes("lpddr_die_brand"), false),
            ("조립 업체", assemblyCode, Codes("lpddr_assembly"), false),
            ("Test Site", testerCode, Codes("lpddr_tester"), false),
            ("Speed", speedCode, Codes("lpddr_speed"), false),
            ("Vendor", vendorCode, Codes("lpddr_vendor"), false),
            ("Purchaser", purchaserCode, Codes("lpddr_purchaser"), true));

        var isThirdParty = sourceCode is "T" or "B";
        if (isThirdParty && IsBlankCode(purchaserCode))
        {
            throw new InvalidOperationException("Third-Party LPDDR requires Purchaser.");
        }

        var purchaserSuffix = IsBlankCode(purchaserCode) ? string.Empty : purchaserCode;

        var dramTypeLabel = GetDramTypeLabel(dramTypeCode);
        var densityLabel = GetDensityLabel(densityCode);
        var bitLabel = GetBitLabel(bitCode);
        var dieRevisionLabel = $"{partRevisionCode}-die";
        var dieBrandLabel = GetDieBrandLabel(dieBrandCode);
        var dieTypeLabel = GetDieTypeLabel(dieTypeCode);
        var packageLabel = GetPackageLabel(packageTypeCode);
        var speedLabel = GetSpeedLabel(speedCode);

        var rows = new List<GeneratedPartRow>();

        string incomingCode;
        string incomingSpec;
        if (request.IsDieIncoming)
        {
            incomingCode = $"{sourceCode}4{dramTypeCode}{dieDensityCode}{dieBitCode}{bankCode}{interfaceCode}{partRevisionCode}"
                + $"-{dieTypeCode}{dieBrandCode}{vendorCode}EB{purchaserSuffix}";
            incomingSpec = JoinSpec(
                dramTypeLabel,
                GetDieDensityLabel(dieDensityCode),
                "Die",
                GetBitLabel(dieBitCode),
                dieRevisionLabel,
                dieBrandLabel,
                dieTypeLabel);
        }
        else
        {
            incomingCode = $"{sourceCode}5{dramTypeCode}{densityCode}{bitCode}{bankCode}{interfaceCode}{stackCode}{partRevisionCode}"
                + $"-{dieTypeCode}{packageTypeCode}{dieBrandCode}{vendorCode}EB{purchaserSuffix}";
            incomingSpec = JoinSpec(
                dramTypeLabel,
                densityLabel,
                "Comp",
                bitLabel,
                dieRevisionLabel,
                dieBrandLabel,
                dieTypeLabel,
                packageLabel);
        }

        rows.Add(new("Incoming", incomingCode, incomingCode, string.Empty, incomingSpec));

        // 현재 운용은 입고(K4/K5)가 Product(RP)로 바로 연결된다. Comp(RD)는 추후 변경 전까지 생성하지 않는다.
        var productBody = $"{dramTypeCode}{densityCode}{bitCode}{bankCode}{interfaceCode}{stackCode}{partRevisionCode}"
            + $"-{dieTypeCode}{packageTypeCode}{assemblyCode}{testerCode}{speedCode}{pcbRevisionCode}{dieBrandCode}{vendorCode}{purchaserSuffix}";
        var productSpec = JoinSpec(
            dramTypeLabel,
            densityLabel,
            "Comp",
            bitLabel,
            dieRevisionLabel,
            dieBrandLabel,
            dieTypeLabel,
            packageLabel,
            speedLabel);

        // DRAM Module과 동일하게 베이스 파트 + BIN 파트를 함께 생성한다. BIN은 Normal(TN/A00) 1종만.
        // Product는 MDL 파트라 일반정보도 생성한다 (Comp 판매용 MDL 패턴).
        var productGeneralInfo = $"{dramTypeLabel} Comp {densityLabel} COO : {_specProvider.SharedSpec.ItemTextRules.CooDefault}";
        var productCode = $"{MapSourceToFamilyPrefix(sourceCode)}P{productBody}";
        rows.Add(new("Product", productCode, productCode, productGeneralInfo, productSpec));

        var productBinCode = $"{productCode}-TN{densityCode}A00";
        rows.Add(new("Product BIN", productBinCode, productBinCode, productGeneralInfo, productSpec));

        return rows;
    }

    /// <summary>
    /// LPDDR 파트코드를 계열(K4/K5 입고, RD Comp, RP Product) 자동 판별로 역파싱한다.
    /// 코드에 없는 필드(K5의 조립/테스트/Speed 등)는 빈 값으로 남긴다.
    /// </summary>
    public LpddrRequest ParsePart(string partCode)
    {
        var normalized = (partCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("LPDDR Full Part is empty.");
        }

        var segments = normalized.Split('-');
        if (segments.Length < 2 || segments[0].Length < 2 || segments[1].Length == 0)
        {
            throw new InvalidOperationException("LPDDR Full Part format is invalid. Check '-' position.");
        }

        var head = segments[0];
        var tail = segments[1];
        var familyKind = head[1];

        return familyKind switch
        {
            '4' => ParseDieIncoming(head, tail),
            '5' => ParsePkgIncoming(head, tail),
            'D' or 'P' => ParseCompOrProduct(head, tail),
            _ => throw new InvalidOperationException($"Unsupported LPDDR family: {head[..2]}")
        };
    }

    private static LpddrRequest ParseDieIncoming(string head, string tail)
    {
        ValidateSourceChar(head[0]);
        if (head.Length != 10)
        {
            throw new InvalidOperationException("LPDDR Die 입고(K4) prefix length is invalid. 예: K4F4K164HM");
        }

        if (tail.Length is not (5 or 6) || tail.Substring(3, 2) != "EB")
        {
            throw new InvalidOperationException("LPDDR Die 입고(K4) tail format is invalid. 예: WGGEB");
        }

        return new LpddrRequest
        {
            SourceCode = head[..1],
            IsDieIncoming = true,
            DramTypeCode = head.Substring(2, 1),
            DieDensityCode = head.Substring(3, 2),
            DieBitCode = head.Substring(5, 2),
            BankCode = head.Substring(7, 1),
            InterfaceCode = head.Substring(8, 1),
            RevisionCode = head.Substring(9, 1),
            DieTypeCode = tail.Substring(0, 1),
            DieBrandCode = tail.Substring(1, 1),
            VendorCode = tail.Substring(2, 1),
            PurchaserCode = tail.Length == 6 ? tail.Substring(5, 1) : string.Empty
        };
    }

    private static LpddrRequest ParsePkgIncoming(string head, string tail)
    {
        ValidateSourceChar(head[0]);
        if (head.Length != 11)
        {
            throw new InvalidOperationException("LPDDR PKG 입고(K5) prefix length is invalid. 예: B5F1G324H1M");
        }

        if (tail.Length is not (6 or 7) || tail.Substring(4, 2) != "EB")
        {
            throw new InvalidOperationException("LPDDR PKG 입고(K5) tail format is invalid. 예: PAGGEBH");
        }

        return new LpddrRequest
        {
            SourceCode = head[..1],
            IsDieIncoming = false,
            DramTypeCode = head.Substring(2, 1),
            DensityCode = head.Substring(3, 2),
            BitCode = head.Substring(5, 2),
            BankCode = head.Substring(7, 1),
            InterfaceCode = head.Substring(8, 1),
            StackCode = head.Substring(9, 1),
            RevisionCode = head.Substring(10, 1),
            DieTypeCode = tail.Substring(0, 1),
            PackageTypeCode = tail.Substring(1, 1),
            DieBrandCode = tail.Substring(2, 1),
            VendorCode = tail.Substring(3, 1),
            PurchaserCode = tail.Length == 7 ? tail.Substring(6, 1) : string.Empty
        };
    }

    private static LpddrRequest ParseCompOrProduct(string head, string tail)
    {
        var sourceCode = MapFamilyPrefixToSource(head[..1]);
        if (head.Length != 11)
        {
            throw new InvalidOperationException("LPDDR Comp/Product prefix length is invalid. 예: RDF1G324H1M");
        }

        if (tail.Length is not (9 or 10))
        {
            throw new InvalidOperationException("LPDDR Comp/Product tail length is invalid. 예: PA0RCP1GG");
        }

        return new LpddrRequest
        {
            SourceCode = sourceCode,
            IsDieIncoming = false,
            DramTypeCode = head.Substring(2, 1),
            DensityCode = head.Substring(3, 2),
            BitCode = head.Substring(5, 2),
            BankCode = head.Substring(7, 1),
            InterfaceCode = head.Substring(8, 1),
            StackCode = head.Substring(9, 1),
            RevisionCode = head.Substring(10, 1),
            DieTypeCode = tail.Substring(0, 1),
            PackageTypeCode = tail.Substring(1, 1),
            AssemblyCode = tail.Substring(2, 1),
            TesterCode = tail.Substring(3, 1),
            SpeedCode = tail.Substring(4, 2),
            PcbRevisionCode = tail.Substring(6, 1),
            DieBrandCode = tail.Substring(7, 1),
            VendorCode = tail.Substring(8, 1),
            PurchaserCode = tail.Length == 10 ? tail.Substring(9, 1) : string.Empty
        };
        // Product(RP)의 -TN{Density}A00 BIN 태그는 재생성 시 자동으로 붙으므로 파싱하지 않는다.
    }

    private static void ValidateSourceChar(char sourceChar)
    {
        if (sourceChar is not ('K' or 'T' or 'C' or 'B'))
        {
            throw new InvalidOperationException($"Unsupported LPDDR incoming source: {sourceChar}");
        }
    }

    private static string MapFamilyPrefixToSource(string familyPrefix)
    {
        return familyPrefix switch
        {
            "R" => "K",
            "T" => "T",
            "C" => "C",
            "B" => "B",
            _ => throw new InvalidOperationException($"Unsupported LPDDR family prefix: {familyPrefix}")
        };
    }

    private static readonly string[] StackCodes = { "0", "1", "2" };

    private static string MapSourceToFamilyPrefix(string sourceCode)
    {
        return sourceCode switch
        {
            "K" => "R",
            "T" => "T",
            "C" => "C",
            "B" => "B",
            _ => throw new InvalidOperationException($"Unsupported LPDDR SourceCode: {sourceCode}")
        };
    }

    private static string GetDramTypeLabel(string dramTypeCode)
    {
        return dramTypeCode switch
        {
            "F" => "LPDDR4",
            "U" => "LPDDR4X",
            "L" => "LPDDR5",
            "K" => "LPDDR5X",
            _ => dramTypeCode
        };
    }

    private static string GetDensityLabel(string densityCode)
    {
        return densityCode switch
        {
            "1G" => "1GB",
            "2G" => "2GB",
            "3G" => "3GB",
            "4G" => "4GB",
            "6G" => "6GB",
            "8G" => "8GB",
            _ => densityCode
        };
    }

    private static string GetDieDensityLabel(string dieDensityCode)
    {
        return dieDensityCode switch
        {
            "4K" => "4Gb",
            "6S" => "6Gb",
            "8E" => "8Gb",
            "2E" => "12Gb",
            "6E" => "16Gb",
            _ => dieDensityCode
        };
    }

    private static string GetBitLabel(string bitCode)
    {
        return bitCode switch
        {
            "16" => "x16",
            "32" => "X32",
            _ => $"x{bitCode}"
        };
    }

    private static string GetDieTypeLabel(string dieTypeCode)
    {
        return dieTypeCode switch
        {
            "P" => "Partial",
            "T" => "Tested",
            "W" => "Whole",
            "A" => "A-Grade",
            _ => dieTypeCode
        };
    }

    private static string GetDieBrandLabel(string dieBrandCode)
    {
        return dieBrandCode switch
        {
            "S" => "S1",
            "G" => "GIGA S1",
            "H" => "GIGA S2",
            "M" => "GIGA S3",
            "C" => "GIGA S6",
            _ => dieBrandCode
        };
    }

    private static string GetPackageLabel(string packageTypeCode)
    {
        return packageTypeCode switch
        {
            "A" => "1.0T / 10x15",
            "B" => "1.1T / 12.4x15",
            "C" => "1.2T / 12.4x15",
            "D" => "441 FBGA",
            "E" => "496 FBGA",
            "F" => "1.05T / 10x14.5",
            "G" => "1.14T / 10x15",
            _ => packageTypeCode
        };
    }

    private static string GetSpeedLabel(string speedCode)
    {
        return speedCode switch
        {
            "CH" => "3200 Mbps",
            "CJ" => "3733 Mbps",
            "CL" => "4266 Mbps",
            "CN" => "5500 Mbps",
            "CP" => "6400 Mbps",
            "CT" => "7500 Mbps",
            "CU" => "8533 Mbps",
            "CV" => "9600 Mbps",
            _ => speedCode
        };
    }

    private static string JoinSpec(params string[] pieces)
    {
        return string.Join(" ", pieces.Where(x => !string.IsNullOrWhiteSpace(x)));
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
        return value is "" or "(NONE)" or "NONE" ? "0" : value;
    }

    private static bool IsBlankCode(string code) => code == "0";

    private static void ValidateRequiredCodes(params string[] requiredCodes)
    {
        if (requiredCodes.Any(IsBlankCode))
        {
            throw new InvalidOperationException("LPDDR required code is empty.");
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
                throw new InvalidOperationException($"{fieldName} code is not allowed: {code}");
            }
        }
    }

    private IReadOnlyCollection<string> Codes(params string[] optionKeys)
    {
        return optionKeys
            .SelectMany(key => _specProvider.SharedSpec.CodeOptions.TryGetValue(key, out var options)
                ? options.Select(NormalizeCode)
                : throw new KeyNotFoundException($"Code option set '{key}' was not found."))
            .ToArray();
    }
}
