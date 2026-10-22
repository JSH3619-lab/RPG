namespace RamosPartGenerator.Core.Services;

// ConvertedCode: 신형 Part (이미 신형이면 원본과 동일) / Warnings: 규칙대로 변환했지만 사용자 확인이 필요한 항목
public sealed record LegacyPartConversion(
    string SourceCode,
    string ConvertedCode,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Warnings
);

// 구형(Rev.27 이하) Comp/Module Part -> 신형(Rev.30) Part 변환 규칙
public static class LegacyPartConverter
{
    // shared.json speed_ddr4/speed_ddr5 코드. Module tail 구조(구형/Rev.27 2글자형) 판별에만 사용한다.
    private static readonly HashSet<string> SpeedCodes = new() { "TD", "WE", "QK", "WM", "CM", "CA", "CP", "CQ", "CR", "CS" };
    // 신형 Vendor 판정용. B(BY20)는 구형 끝 B(Reball)와 충돌하므로 제외한다.
    private const string VendorCodes = "SGA";
    private const string SpecialCode2Codes = "RSBCM";
    // Grade HM(Speed Down) 폐지: 한 단계 낮은 Speed + Grade TN
    private static readonly Dictionary<string, string> SpeedDown = new() { ["WE"] = "TD", ["WM"] = "QK" };

    // icBrand: 구형 Module(I.C Brand 없음)에 넣을 I.C Brand = Vendor. 원본 코드로 구분할 수 없어 사용자가 고른다.
    // S = 삼성 직구매 PKG(S1), G = GIGA. Comp와 Rev.27 2글자형은 사용하지 않는다.
    public static LegacyPartConversion Convert(string partCode, char icBrand)
    {
        if (icBrand is not ('S' or 'G'))
        {
            throw new ArgumentException($"I.C Brand는 S 또는 G만 선택할 수 있습니다: {icBrand}", nameof(icBrand));
        }

        var code = partCode.Trim().ToUpperInvariant();
        return IsCompPart(code) ? ConvertComp(code) : ConvertModule(code, icBrand);
    }

    public static bool IsCompPart(string partCode)
    {
        return partCode.StartsWith("RC") || partCode.StartsWith("TC") || partCode.StartsWith("CC") || partCode.StartsWith("BC");
    }

    private static LegacyPartConversion ConvertComp(string code)
    {
        var parts = code.Split('-');
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Comp Part 형식이 올바르지 않습니다: {code}");
        }

        // 신형 tail: CompType Package DieBrand Tester Vendor [Purchaser] [CompType2]
        var tail = parts[1];
        if (tail.Length >= 5 && VendorCodes.Contains(tail[4]))
        {
            return new LegacyPartConversion(code, code, new[] { "신형(변경 없음)" }, Array.Empty<string>());
        }

        // 구형 tail: CompType Package DieBrand Tester [B=Reball]
        if (tail.Length is not (4 or 5))
        {
            throw new InvalidOperationException($"해석할 수 없는 Comp tail입니다: {tail}");
        }

        if (tail.Length == 5 && tail[4] != 'B')
        {
            throw new InvalidOperationException($"구형 Comp Type 2 코드를 확인할 수 없습니다: {tail[4]}");
        }

        var dieBrand = tail[2];
        if (!VendorCodes.Contains(dieBrand))
        {
            throw new InvalidOperationException($"Die Brand {dieBrand}에 대응할 Vendor가 정해지지 않았습니다.");
        }

        var notes = new List<string> { $"Vendor {dieBrand} 추가(=Die Brand)" };
        if (tail.Length == 5)
        {
            notes.Add("끝 B = Comp Type 2 Reball 유지");
        }

        return new LegacyPartConversion(code, $"{parts[0]}-{tail[..4]}{dieBrand}{tail[4..]}", notes, Array.Empty<string>());
    }

    private static LegacyPartConversion ConvertModule(string code, char icBrand)
    {
        var parts = code.Split('-');
        if (parts.Length is < 2 or > 3 || parts[0].Length != 11)
        {
            throw new InvalidOperationException($"Module Part 형식이 올바르지 않습니다: {code}");
        }

        var head = parts[0];
        var tail = parts[1];
        var binTail = parts.Length == 3 ? parts[2] : string.Empty;
        var density = head.Substring(4, 2);
        var notes = new List<string>();
        var warnings = new List<string>();

        // 변환 후 tail은 항상 I.C Brand CompType CompTest SMT ModuleTest Speed(2) PCB Vendor [...]
        string newTail;
        if (tail.Length >= 7 && SpeedCodes.Contains(tail.Substring(4, 2)))
        {
            // 구형: CompType CompTest SMT ModuleTest Speed(2) PCB [Special Code 2]
            var rest = tail[7..];
            ValidateSpecialCode2(rest);
            newTail = icBrand + tail[..7] + icBrand + rest;
            notes.Add($"Comp Type {tail[0]} -> I.C Brand {icBrand} + {tail[0]} ({(icBrand == 'S' ? "삼성 직구매" : "GIGA")})");
            notes.Add($"Vendor {icBrand} 추가");
            // α chip H는 삼성 직구매(SH)만 확정이다. GH는 아직 확정되지 않았다.
            if (tail[0] == 'H' && icBrand == 'G')
            {
                warnings.Add("구형 Comp Type H(α chip) -> GH 적용, 확인 필요 (확정은 SH)");
            }
        }
        else if (tail.Length >= 8 && SpeedCodes.Contains(tail.Substring(5, 2)))
        {
            // Rev.27~29: I.C Brand + CompType 2글자
            var rest = tail[8..];
            // Rev.27.2에는 Internal 파트의 Vendor 자리가 없고 PCB 뒤는 Special Code 1(R/S/B/C)이다.
            // 끝 단독 S는 신형 Vendor S가 아니라 2nd Repair로 읽는다(2026-10-22 사용자 확정).
            if (rest.Length > 0 && VendorCodes.Contains(rest[0]) && rest != "S")
            {
                newTail = tail;
                notes.Add("Tail 신형 구조(변경 없음)");
            }
            else
            {
                var brand = tail[0];
                if (!VendorCodes.Contains(brand))
                {
                    throw new InvalidOperationException($"I.C Brand {brand}에 대응할 Vendor가 정해지지 않았습니다.");
                }

                ValidateSpecialCode2(rest);
                newTail = tail[..8] + brand + rest;
                notes.Add($"I.C Brand {brand} 기존 표기, Vendor {brand} 추가");
            }
        }
        else
        {
            throw new InvalidOperationException($"해석할 수 없는 Module tail입니다: {tail}");
        }

        var grade = "TN";
        var productBin = "A00";
        if (binTail.Length == 7)
        {
            grade = binTail[..2];
            productBin = binTail[4..];
            if (binTail.Substring(2, 2) != density)
            {
                warnings.Add($"BIN Density {binTail.Substring(2, 2)} != Module Density {density}, 확인 필요");
            }
        }
        else if (binTail.Length == 2)
        {
            grade = binTail;
            notes.Add($"BIN -{binTail} -> -TN{density}A00 (신형 BIN 형식)");
        }
        else if (binTail.Length == 0)
        {
            notes.Add($"BIN 없음 -> -TN{density}A00 추가");
        }
        else
        {
            throw new InvalidOperationException($"Module BIN 형식이 올바르지 않습니다: {binTail}");
        }

        if (grade == "HM")
        {
            var speed = newTail.Substring(5, 2);
            if (!SpeedDown.TryGetValue(speed, out var lowerSpeed))
            {
                throw new InvalidOperationException($"Grade HM인데 Speed {speed}의 하향 규칙이 없습니다.");
            }

            newTail = newTail[..5] + lowerSpeed + newTail[7..];
            grade = "TN";
            notes.Add($"HM(Speed Down) -> Speed {speed}->{lowerSpeed}, Grade TN");
        }
        else if (grade != "TN")
        {
            throw new InvalidOperationException($"Grade {grade}를 확인할 수 없습니다.");
        }

        if (productBin == "A01")
        {
            productBin = "A00";
            notes.Add("Product Bin A01 -> A00");
        }
        else if (productBin is not ("A00" or "B00"))
        {
            throw new InvalidOperationException($"Product Bin {productBin}을 확인할 수 없습니다.");
        }

        return new LegacyPartConversion(code, $"{head}-{newTail}-{grade}{density}{productBin}", notes, warnings);
    }

    private static void ValidateSpecialCode2(string rest)
    {
        if (rest.Length > 1 || (rest.Length == 1 && !SpecialCode2Codes.Contains(rest[0])))
        {
            throw new InvalidOperationException($"끝 코드를 확인할 수 없습니다: {rest}");
        }
    }
}
