using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Tests;

public sealed class LegacyPartConverterTests
{
    // 요청/통합 문서1.xlsx(F열) 22건 + 요청/DDR4 Label 및 SPD Change 요청.xlsx(C열) 46건
    // 기대값: 같은 폴더 *_신형변환.xlsx (통합 문서1 I열, Label 요청 D열)
    private static readonly (string Source, string Expected)[] RequestSamples =
    {
        ("RCA8G085WC-PBSRB", "RCA8G085WC-PBSRSB"),
        ("RCA8G085WD-PBSRB", "RCA8G085WD-PBSRSB"),
        ("RCA8G085WE-PBSRB", "RCA8G085WE-PBSRSB"),
        ("RCA8G085WT-HBSRB", "RCA8G085WT-HBSRSB"),
        ("RCAAG085WB-MBSRB", "RCAAG085WB-MBSRSB"),
        ("RCAAG085WB-PBSRB", "RCAAG085WB-PBSRSB"),
        ("RCAAG085WC-PBSRB", "RCAAG085WC-PBSRSB"),
        ("RCRAH086VB-PBSWSB", "RCRAH086VB-PBSWSB"),
        ("RCRAH086VE-PBSWB", "RCRAH086VE-PBSWSB"),
        ("RCRAH086VP-FBGWB", "RCRAH086VP-FBGWGB"),
        ("RCRAH086VP-GBGWB", "RCRAH086VP-GBGWGB"),
        ("RCRAH086VB-UBGWB", "RCRAH086VB-UBGWGB"),
        ("RCRAH086VP-PBSWB", "RCRAH086VP-PBSWSB"),
        ("RCRAH086VB-MBSWB", "RCRAH086VB-MBSWSB"),
        ("RCRAH086VB-MBGWGB", "RCRAH086VB-MBGWGB"),
        ("RCAAG085WB-PBSRSB", "RCAAG085WB-PBSRSB"),
        ("RCA8G085WT-HBSRSB", "RCA8G085WT-HBSRSB"),
        ("RCRAH086VE-PBSWSB", "RCRAH086VE-PBSWSB"),
        ("RCRAH086VP-EBGWGB", "RCRAH086VP-EBGWGB"),
        ("RCRAH086VB-PBGWGB", "RCRAH086VB-PBGWGB"),
        ("RCA8G085WE-PBSRSB", "RCA8G085WE-PBSRSB"),
        ("RCRAH086VB-PBGWGB", "RCRAH086VB-PBGWGB"),
        ("RM4D8G4881E-PRRRWE1B-TN8GA00", "RM4D8G4881E-GPRRRWE1GB-TN8GA00"),
        ("RM4D8G4881E-PRRRWE1B-TN", "RM4D8G4881E-GPRRRWE1GB-TN8GA00"),
        ("RM4D8G4881T-HRRRWE1-TN8GA01", "RM4D8G4881T-GHRRRWE1G-TN8GA00"),
        ("RM4DAG4882D-PRRRWE2-TNAGA01", "RM4DAG4882D-GPRRRWE2G-TNAGA00"),
        ("RM4DAG4882E-MRRRWE2-TNAGA01", "RM4DAG4882E-GMRRRWE2G-TNAGA00"),
        ("RM4DAG48A1M-MRRRTD2-TNAGA01", "RM4DAG48A1M-GMRRRTD2G-TNAGA00"),
        ("RM4DAG4882T-HRRRWE2B-TNAGA00", "RM4DAG4882T-GHRRRWE2GB-TNAGA00"),
        ("RM4DAG48A1C-PWYSWE1-TN", "RM4DAG48A1C-GPWYSWE1G-TNAGA00"),
        ("RM4DAG4882C-PRYSWE1-HM", "RM4DAG4882C-GPRYSTD1G-TNAGA00"),
        ("RM4DAG48A1C-PRRRWE1-TNAGA01", "RM4DAG48A1C-GPRRRWE1G-TNAGA00"),
        ("RM4DAG48A1A-P2YSWE1-TN", "RM4DAG48A1A-GP2YSWE1G-TNAGA00"),
        ("RM4DAG48A1A-PRRRWE1-HMAGA01", "RM4DAG48A1A-GPRRRTD1G-TNAGA00"),
        ("RM4DAG48A1A-PRRRWE1-TNAGA01", "RM4DAG48A1A-GPRRRWE1G-TNAGA00"),
        ("RM4DAG48A1A-PRRRWE1B-TN", "RM4DAG48A1A-GPRRRWE1GB-TNAGA00"),
        ("RM4DAG48A1M-MRRRTD1-TNAGA01", "RM4DAG48A1M-GMRRRTD1G-TNAGA00"),
        ("RM4DAG46A2A-PGGSWEK-TN", "RM4DAG46A2A-GPGGSWEKG-TNAGA00"),
        ("RM4DBG48A2A-PRRRWE2-TNBGA01", "RM4DBG48A2A-GPRRRWE2G-TNBGA00"),
        ("RM4S8G4881D-PRRRWE2-TN8GA01", "RM4S8G4881D-GPRRRWE2G-TN8GA00"),
        ("RM4S8G4881E-PRRRWE2-HM8GA01", "RM4S8G4881E-GPRRRTD2G-TN8GA00"),
        ("RM4S8G4881E-PRRRWE2-TN8GA01", "RM4S8G4881E-GPRRRWE2G-TN8GA00"),
        ("RM4S8G4881E-PRRRWE2B-TN8GA00", "RM4S8G4881E-GPRRRWE2GB-TN8GA00"),
        ("RM4S8G4881E-PRRRWE2B-TN", "RM4S8G4881E-GPRRRWE2GB-TN8GA00"),
        ("RM4S8G4881B-PRRRWE2-TN8GA01", "RM4S8G4881B-GPRRRWE2G-TN8GA00"),
        ("RM4S8G4881B-PRRRWE2B-TN", "RM4S8G4881B-GPRRRWE2GB-TN8GA00"),
        ("RM4S8G4881D-MRRRWE2-TN8GA01", "RM4S8G4881D-GMRRRWE2G-TN8GA00"),
        ("RM4S8G4881E-MRRRWE2-TN8GA01", "RM4S8G4881E-GMRRRWE2G-TN8GA00"),
        ("RM4S8G4881E-MRRRWE2B-TN8GA00", "RM4S8G4881E-GMRRRWE2GB-TN8GA00"),
        ("RM4S8G4881T-HRRRWE2-TN8GA01", "RM4S8G4881T-GHRRRWE2G-TN8GA00"),
        ("RM4S8G46A1C-PGGSTDG-TN", "RM4S8G46A1C-GPGGSTDGG-TN8GA00"),
        ("RM4S8G46A1B-PGGSTDG-TN", "RM4S8G46A1B-GPGGSTDGG-TN8GA00"),
        ("RM4S8G4881D-GGRRRWE2-TN8GA00", "RM4S8G4881D-GGRRRWE2G-TN8GA00"),
        ("RM4SAG48A1C-PRRRWE2-TNAGA01", "RM4SAG48A1C-GPRRRWE2G-TNAGA00"),
        ("RM4SAG48A1C-PRRRWE2B-TNAGA00", "RM4SAG48A1C-GPRRRWE2GB-TNAGA00"),
        ("RM4SAG4882E-PRRRWE2R-TNAGA00", "RM4SAG4882E-GPRRRWE2GR-TNAGA00"),
        ("RM4SAG48A1A-PRRRWE2-TNAGA01", "RM4SAG48A1A-GPRRRWE2G-TNAGA00"),
        ("RM4SAG48A1A-PRRRWE2B-TN", "RM4SAG48A1A-GPRRRWE2GB-TNAGA00"),
        ("RM4SAG48A1A-PRRRWE2B-TNAGA00", "RM4SAG48A1A-GPRRRWE2GB-TNAGA00"),
        ("RM4SAG4882D-MRRRWE2-TNAGA01", "RM4SAG4882D-GMRRRWE2G-TNAGA00"),
        ("RM4SAG48A1A-MRRRWE2-TNAGA01", "RM4SAG48A1A-GMRRRWE2G-TNAGA00"),
        ("RM4SAG4882T-HRRRWE2-TNAGA01", "RM4SAG4882T-GHRRRWE2G-TNAGA00"),
        ("RM4SAG4882C-PRYSWE1-TN", "RM4SAG4882C-GPRYSWE1G-TNAGA00"),
        ("RM4SAG4882B-PRYSWE1-TN", "RM4SAG4882B-GPRYSWE1G-TNAGA00"),
        ("RM4SAG4882E-MRRRWE6-TNAGA01", "RM4SAG4882E-GMRRRWE6G-TNAGA00"),
        ("RM4SAG4882C-TRRRWE6-TNAGA01", "RM4SAG4882C-GTRRRWE6G-TNAGA00"),
        ("RM4SAG48A1B-SPRRRWE2SB-TN", "RM4SAG48A1B-SPRRRWE2SB-TNAGA00"),
        ("RM4SBG48A2A-PRRRWE6-TNBGA01", "RM4SBG48A2A-GPRRRWE6G-TNBGA00"),
    };

    [Fact]
    public void Convert_RequestSamples_MatchExpectedAndRoundTripThroughCore()
    {
        Assert.Equal(68, RequestSamples.Length);

        var specProvider = new SpecProvider(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs")));
        specProvider.Load();
        var textService = new ProductTextService(specProvider);
        var incomingService = new IncomingCompService(specProvider, textService);
        var moduleService = new ModuleService(specProvider, textService);

        foreach (var (source, expected) in RequestSamples)
        {
            Assert.Equal(expected, LegacyPartConverter.Convert(source, 'G').ConvertedCode);

            // 신형 코드는 Core 파서 -> 생성기 왕복으로 같은 코드가 나와야 한다.
            var regenerated = expected.StartsWith("RC")
                ? incomingService.GeneratePreview(incomingService.ParseCompPart("30", expected)).Where(x => x.Kind == "Comp")
                : moduleService.GeneratePreview(moduleService.ParseModuleFullPart("30", expected));
            Assert.Contains(regenerated, x => x.PartCode == expected);
        }
    }

    [Theory]
    [InlineData("RCA8G085WC-PBSRB", "RCA8G085WC-PBSRSB")]     // Vendor = Die Brand S, 끝 B = Reball
    [InlineData("RCRAH086VP-FBGWB", "RCRAH086VP-FBGWGB")]     // Vendor = Die Brand G
    [InlineData("RCA8G085WC-PBSR", "RCA8G085WC-PBSRS")]       // Comp Type 2 없음
    [InlineData("RCRAH086VB-MBGWGB", "RCRAH086VB-MBGWGB")]    // 이미 신형
    public void Convert_Comp_InsertsVendorAfterTester(string source, string expected)
    {
        Assert.Equal(expected, LegacyPartConverter.Convert(source, 'G').ConvertedCode);
    }

    [Theory]
    [InlineData("RM4D8G4881E-PRRRWE1B-TN", "RM4D8G4881E-GPRRRWE1GB-TN8GA00")]        // 구형: I.C Brand G + Vendor G, 끝 B 유지
    [InlineData("RM4SAG4882E-PRRRWE2R-TNAGA00", "RM4SAG4882E-GPRRRWE2GR-TNAGA00")]   // 끝 R = 1st Repair 유지
    [InlineData("RM4S8G4881D-GGRRRWE2-TN8GA00", "RM4S8G4881D-GGRRRWE2G-TN8GA00")]    // Rev.27 2글자형: Vendor = I.C Brand
    [InlineData("RM4S8G4881D-GGRRRWE2S-TN8GA00", "RM4S8G4881D-GGRRRWE2GS-TN8GA00")]  // Rev.27.2 끝 S = 2nd Repair
    [InlineData("RM4S8G4881D-GGRRRWE2B-TN8GA00", "RM4S8G4881D-GGRRRWE2GB-TN8GA00")]  // Rev.27.2 끝 B = Reball
    [InlineData("RM4SAG48A1B-SPRRRWE2SB-TN", "RM4SAG48A1B-SPRRRWE2SB-TNAGA00")]      // 이미 신형 tail 유지, BIN만 보정
    [InlineData("RM4DAG4882C-PRYSWE1-HM", "RM4DAG4882C-GPRYSTD1G-TNAGA00")]          // HM: WE -> TD, Grade TN
    [InlineData("RM5SAG48A1A-GPRRRWM1G-HMAGA00", "RM5SAG48A1A-GPRRRQK1G-TNAGA00")]   // HM: WM -> QK
    [InlineData("RM4DAG4882D-PRRRWE2-TNAGA01", "RM4DAG4882D-GPRRRWE2G-TNAGA00")]     // A01 -> A00
    [InlineData("RM4SAG4882E-PRRRWE2", "RM4SAG4882E-GPRRRWE2G-TNAGA00")]             // BIN 없음 -> TN{Density}A00
    public void Convert_Module_AppliesTailAndBinRules(string source, string expected)
    {
        Assert.Equal(expected, LegacyPartConverter.Convert(source, 'G').ConvertedCode);
    }

    [Fact]
    public void Convert_LegacyCompTypeH_FlagsConfirmation()
    {
        var result = LegacyPartConverter.Convert("RM4S8G4881T-HRRRWE2-TN8GA01", 'G');

        Assert.Equal("RM4S8G4881T-GHRRRWE2G-TN8GA00", result.ConvertedCode);
        Assert.Contains(result.Warnings, x => x.Contains("확인 필요"));
        Assert.Empty(LegacyPartConverter.Convert("RM4S8G4881E-PRRRWE2-TN8GA01", 'G').Warnings);
    }

    // 2026-10-21 사용자 확정: 삼성 직구매 PKG 레거시는 I.C Brand/Vendor S. 기대값은 요청/DDR4 Label 및 SPD Change 요청.xlsx
    // New Part 열(S버전, 0Y Retest 포함)에서 0Y를 뺀 값. Rev.27 2글자형(GG...)은 선택과 무관하게 G 유지.
    [Theory]
    [InlineData("RM4D8G4881E-PRRRWE1B-TN8GA00", "RM4D8G4881E-SPRRRWE1SB-TN8GA00")]
    [InlineData("RM4D8G4881T-HRRRWE1-TN8GA01", "RM4D8G4881T-SHRRRWE1S-TN8GA00")]      // α chip H -> SH
    [InlineData("RM4DAG4882C-PRYSWE1-HM", "RM4DAG4882C-SPRYSTD1S-TNAGA00")]
    [InlineData("RM4SAG4882E-PRRRWE2R-TNAGA00", "RM4SAG4882E-SPRRRWE2SR-TNAGA00")]
    [InlineData("RM4SBG48A2A-PRRRWE6-TNBGA01", "RM4SBG48A2A-SPRRRWE6S-TNBGA00")]
    [InlineData("RM4S8G4881D-GGRRRWE2-TN8GA00", "RM4S8G4881D-GGRRRWE2G-TN8GA00")]     // Rev.27 2글자형(GOX) 유지
    [InlineData("RM4SAG48A1B-SPRRRWE2SB-TN", "RM4SAG48A1B-SPRRRWE2SB-TNAGA00")]      // 이미 신형
    [InlineData("RCA8G085WC-PBSRB", "RCA8G085WC-PBSRSB")]                            // Comp는 Die Brand 기준(선택 무관)
    public void Convert_SamsungDirectPkg_UsesIcBrandS(string source, string expected)
    {
        var result = LegacyPartConverter.Convert(source, 'S');

        Assert.Equal(expected, result.ConvertedCode);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Convert_LabelRequestNewParts_MatchSamsungVersionAndRoundTrip()
    {
        // 요청/DDR4 Label 및 SPD Change 요청.xlsx C열(MID) -> D열(New Part, 0Y 제외) 43건
        var samples = new (string Source, string Expected)[]
        {
            ("RM4D8G4881E-PRRRWE1B-TN8GA00", "RM4D8G4881E-SPRRRWE1SB-TN8GA00"),
            ("RM4D8G4881T-HRRRWE1-TN8GA01", "RM4D8G4881T-SHRRRWE1S-TN8GA00"),
            ("RM4DAG4882D-PRRRWE2-TNAGA01", "RM4DAG4882D-SPRRRWE2S-TNAGA00"),
            ("RM4DAG4882E-MRRRWE2-TNAGA01", "RM4DAG4882E-SMRRRWE2S-TNAGA00"),
            ("RM4DAG48A1M-MRRRTD2-TNAGA01", "RM4DAG48A1M-SMRRRTD2S-TNAGA00"),
            ("RM4DAG4882T-HRRRWE2B-TNAGA00", "RM4DAG4882T-SHRRRWE2SB-TNAGA00"),
            ("RM4DAG48A1C-PWYSWE1-TN", "RM4DAG48A1C-SPWYSWE1S-TNAGA00"),
            ("RM4DAG4882C-PRYSWE1-HM", "RM4DAG4882C-SPRYSTD1S-TNAGA00"),
            ("RM4DAG48A1C-PRRRWE1-TNAGA01", "RM4DAG48A1C-SPRRRWE1S-TNAGA00"),
            ("RM4DAG48A1A-P2YSWE1-TN", "RM4DAG48A1A-SP2YSWE1S-TNAGA00"),
            ("RM4DAG48A1A-PRRRWE1-HMAGA01", "RM4DAG48A1A-SPRRRTD1S-TNAGA00"),
            ("RM4DAG48A1A-PRRRWE1-TNAGA01", "RM4DAG48A1A-SPRRRWE1S-TNAGA00"),
            ("RM4DAG48A1A-PRRRWE1B-TN", "RM4DAG48A1A-SPRRRWE1SB-TNAGA00"),
            ("RM4DAG48A1M-MRRRTD1-TNAGA01", "RM4DAG48A1M-SMRRRTD1S-TNAGA00"),
            ("RM4DAG46A2A-PGGSWEK-TN", "RM4DAG46A2A-SPGGSWEKS-TNAGA00"),
            ("RM4DBG48A2A-PRRRWE2-TNBGA01", "RM4DBG48A2A-SPRRRWE2S-TNBGA00"),
            ("RM4S8G4881D-PRRRWE2-TN8GA01", "RM4S8G4881D-SPRRRWE2S-TN8GA00"),
            ("RM4S8G4881E-PRRRWE2-HM8GA01", "RM4S8G4881E-SPRRRTD2S-TN8GA00"),
            ("RM4S8G4881E-PRRRWE2-TN8GA01", "RM4S8G4881E-SPRRRWE2S-TN8GA00"),
            ("RM4S8G4881E-PRRRWE2B-TN8GA00", "RM4S8G4881E-SPRRRWE2SB-TN8GA00"),
            ("RM4S8G4881B-PRRRWE2-TN8GA01", "RM4S8G4881B-SPRRRWE2S-TN8GA00"),
            ("RM4S8G4881B-PRRRWE2B-TN", "RM4S8G4881B-SPRRRWE2SB-TN8GA00"),
            ("RM4S8G4881D-MRRRWE2-TN8GA01", "RM4S8G4881D-SMRRRWE2S-TN8GA00"),
            ("RM4S8G4881E-MRRRWE2-TN8GA01", "RM4S8G4881E-SMRRRWE2S-TN8GA00"),
            ("RM4S8G4881E-MRRRWE2B-TN8GA00", "RM4S8G4881E-SMRRRWE2SB-TN8GA00"),
            ("RM4S8G4881T-HRRRWE2-TN8GA01", "RM4S8G4881T-SHRRRWE2S-TN8GA00"),
            ("RM4S8G46A1C-PGGSTDG-TN", "RM4S8G46A1C-SPGGSTDGS-TN8GA00"),
            ("RM4S8G46A1B-PGGSTDG-TN", "RM4S8G46A1B-SPGGSTDGS-TN8GA00"),
            ("RM4S8G4881D-GGRRRWE2-TN8GA00", "RM4S8G4881D-GGRRRWE2G-TN8GA00"),
            ("RM4SAG48A1C-PRRRWE2-TNAGA01", "RM4SAG48A1C-SPRRRWE2S-TNAGA00"),
            ("RM4SAG48A1C-PRRRWE2B-TNAGA00", "RM4SAG48A1C-SPRRRWE2SB-TNAGA00"),
            ("RM4SAG4882E-PRRRWE2R-TNAGA00", "RM4SAG4882E-SPRRRWE2SR-TNAGA00"),
            ("RM4SAG48A1A-PRRRWE2-TNAGA01", "RM4SAG48A1A-SPRRRWE2S-TNAGA00"),
            ("RM4SAG48A1A-PRRRWE2B-TN", "RM4SAG48A1A-SPRRRWE2SB-TNAGA00"),
            ("RM4SAG4882D-MRRRWE2-TNAGA01", "RM4SAG4882D-SMRRRWE2S-TNAGA00"),
            ("RM4SAG48A1A-MRRRWE2-TNAGA01", "RM4SAG48A1A-SMRRRWE2S-TNAGA00"),
            ("RM4SAG4882T-HRRRWE2-TNAGA01", "RM4SAG4882T-SHRRRWE2S-TNAGA00"),
            ("RM4SAG4882C-PRYSWE1-TN", "RM4SAG4882C-SPRYSWE1S-TNAGA00"),
            ("RM4SAG4882B-PRYSWE1-TN", "RM4SAG4882B-SPRYSWE1S-TNAGA00"),
            ("RM4SAG4882E-MRRRWE6-TNAGA01", "RM4SAG4882E-SMRRRWE6S-TNAGA00"),
            ("RM4SAG4882C-TRRRWE6-TNAGA01", "RM4SAG4882C-STRRRWE6S-TNAGA00"),
            ("RM4SAG48A1B-SPRRRWE2SB-TN", "RM4SAG48A1B-SPRRRWE2SB-TNAGA00"),
            ("RM4SBG48A2A-PRRRWE6-TNBGA01", "RM4SBG48A2A-SPRRRWE6S-TNBGA00"),
        };
        Assert.Equal(43, samples.Length);

        var specProvider = new SpecProvider(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs")));
        specProvider.Load();
        var moduleService = new ModuleService(specProvider, new ProductTextService(specProvider));

        foreach (var (source, expected) in samples)
        {
            Assert.Equal(expected, LegacyPartConverter.Convert(source, 'S').ConvertedCode);
            Assert.Contains(moduleService.GeneratePreview(moduleService.ParseModuleFullPart("30", expected)), x => x.PartCode == expected);
        }
    }

    [Fact]
    public void Convert_InvalidIcBrand_Throws()
    {
        Assert.Throws<ArgumentException>(() => LegacyPartConverter.Convert("RM4S8G4881E-PRRRWE2-TN8GA01", 'H'));
    }

    [Theory]
    [InlineData("RCA8G085WC-PBSRX")]              // 구형 Comp Type 2 미확인
    [InlineData("RCA8G085WC-PBVR")]               // Die Brand V -> Vendor 미정
    [InlineData("RM4SAG4882E-PRRRWE2Z-TNAGA00")]  // 끝 코드 미확인
    [InlineData("RM4SAG4882E-PRRRTD2-HMAGA00")]   // TD는 하향 규칙 없음
    [InlineData("RM4SAG4882E-PRRRWE2-TNAGA02")]   // Product Bin 미확인
    public void Convert_UnknownCode_Throws(string source)
    {
        Assert.Throws<InvalidOperationException>(() => LegacyPartConverter.Convert(source, 'G'));
    }
}
