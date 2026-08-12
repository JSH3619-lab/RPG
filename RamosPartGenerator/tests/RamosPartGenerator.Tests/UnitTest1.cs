using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;
using RamosPartGenerator.Excel;
using System.IO.Compression;

namespace RamosPartGenerator.Tests;

public class UnitTest1
{
    private static SpecProvider LoadProvider()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        return provider;
    }

    private static string[] SharedOptions(SpecProvider provider, params string[] optionKeys)
    {
        return optionKeys
            .SelectMany(key => provider.SharedSpec.CodeOptions[key])
            .ToArray();
    }

    [Fact]
    public void BuildModuleLookups_UsesModuleDieDensityCodes()
    {
        var provider = LoadProvider();
        var dieDensityOptions = SharedOptions(provider, "module_die_density");
        var pcbOptions = SharedOptions(provider, "pcb");

        Assert.Equal(new[] { "4 - 4Gb", "8 - 8Gb", "A - 16Gb", "H - 24Gb", "B - 32Gb" }, dieDensityOptions);
        Assert.Contains("B - AD5U8C0(ADATA/BP) PCB (Black)", pcbOptions);
    }

    [Fact]
    public void BuildModuleLookups_UsesSpecOptionsForRev30CompDimm()
    {
        var provider = LoadProvider();
        var revisionSpec = provider.GetRevisionSpec("30");
        var dimmTypeOptions = SharedOptions(provider, "dimm_type_common")
            .Concat(revisionSpec.Module.DimmTypeAdditions.Select(code => $"{code} - Comp"))
            .ToArray();

        Assert.Contains("C - Comp", dimmTypeOptions);
        Assert.DoesNotContain("Comp - Comp", dimmTypeOptions);
        Assert.Equal("30.6", revisionSpec.DisplayRevision);
    }

    [Fact]
    public void GeneratePreview_RdimmX8_GeneratesUdimmSalvageAndRdimmBins()
    {
        var provider = LoadProvider();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "30",
            SourceCode = "K",
            DramTypeCode = "S",
            DensityCode = "AH",
            BitOrganizationCode = "08",
            BankCode = "6",
            InterfaceCode = "V",
            RevisionCode = "A",
            CompTypeCode = "P",
            DieBrandCode = "S",
            VendorCode = "S",
            PackageTypeCode = "B",
            TesterCode = "R"
        });

        // 입고 파트는 RDIMM 구분 없이 R로 생성한다.
        Assert.Equal("K4RAH086VA-PSSEL", rows[0].PartCode);
        Assert.DoesNotContain("RDIMM", rows[0].Specification);
        // Comp 파트는 S(DDR5 RDIMM)를 유지한다.
        Assert.Equal("RCSAH086VA-PBSRS", rows[1].PartCode);
        Assert.Contains("DDR5 RDIMM", rows[1].Specification);

        // x8 RDIMM은 CA~CF(UDIMM 구제) + EA~EF(RDIMM) 12개.
        var bins = rows.Where(row => row.Kind == "Comp BIN").ToArray();
        Assert.Equal(12, bins.Length);

        var udimmBin = bins.Single(row => row.PartCode == "RCSAH086VA-PBSRS-CA");
        Assert.DoesNotContain("RDIMM", udimmBin.Specification);

        var rdimmBin = bins.Single(row => row.PartCode == "RCSAH086VA-PBSRS-EA");
        Assert.Contains("DDR5 RDIMM", rdimmBin.Specification);
    }

    [Fact]
    public void GeneratePreview_RdimmX4_GeneratesRdimmBinsOnly()
    {
        var provider = LoadProvider();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "30",
            SourceCode = "K",
            DramTypeCode = "S",
            DensityCode = "AH",
            BitOrganizationCode = "04",
            BankCode = "6",
            InterfaceCode = "V",
            RevisionCode = "A",
            CompTypeCode = "P",
            DieBrandCode = "S",
            VendorCode = "S",
            PackageTypeCode = "B",
            TesterCode = "R"
        });

        Assert.Equal("RCSAH046VA-PBSRS", rows[1].PartCode);

        // x4 RDIMM은 UDIMM 불가라 EA~EF만 6개, CA~CF는 없다.
        var bins = rows.Where(row => row.Kind == "Comp BIN").ToArray();
        Assert.Equal(6, bins.Length);
        Assert.All(bins, row => Assert.Contains("-E", row.PartCode));
        Assert.DoesNotContain(bins, row => row.PartCode.EndsWith("-CA"));
        Assert.Contains("DDR5 RDIMM", bins.Single(row => row.PartCode.EndsWith("-EA")).Specification);
    }

    [Fact]
    public void BuildLookups_IncludesRev306Additions()
    {
        var provider = LoadProvider();

        Assert.Contains("S - DDR5 RDIMM", SharedOptions(provider, "dram_type_comp_extra"));
        Assert.Contains("R - x80 288pin Registered DIMM", SharedOptions(provider, "dimm_type_common"));

        var moduleDensityOptions = SharedOptions(provider, "module_density");
        Assert.Contains("3G - 24GB", moduleDensityOptions);
        Assert.Contains("6G - 48GB", moduleDensityOptions);
        Assert.Contains("DG - 128GB", moduleDensityOptions);
    }

    [Fact]
    public void BuildModuleLookups_IncludesSmallCompDensitiesAndPcbNoAssy()
    {
        var provider = LoadProvider();
        var moduleDensityOptions = SharedOptions(provider, "module_density");
        var pcbOptions = SharedOptions(provider, "pcb");

        Assert.Contains("1G - 1GB", moduleDensityOptions);
        Assert.Contains("2G - 2GB", moduleDensityOptions);
        Assert.Contains("0 - No Ass'y", pcbOptions);
    }

    [Fact]
    public void BuildLookups_IncludesManufacturingSourcesCompTypesAndRamboVendor()
    {
        var provider = LoadProvider();
        var incomingSourceOptions = SharedOptions(provider, "incoming_source", "manufacturing_incoming_source");
        var incomingCompTypeOptions = SharedOptions(provider, "comp_type", "manufacturing_comp_type");
        var incomingVendorOptions = SharedOptions(provider, "vendor", "vendor_tm");

        Assert.Contains("X - RAmos TM", incomingSourceOptions);
        Assert.Contains("0 - Only Test", incomingCompTypeOptions);
        Assert.Contains("X - RAMBO", incomingVendorOptions);
        Assert.Contains("V - GIGA S1 (SV)", SharedOptions(provider, "die_brand"));
        Assert.Contains("P - GIGA S1 (SP)", SharedOptions(provider, "die_brand"));

        var moduleSourceOptions = SharedOptions(provider, "module_source", "manufacturing_module_source");
        var moduleCompTypeOptions = SharedOptions(provider, "comp_type", "manufacturing_comp_type");

        Assert.Contains("XM - Ramos Module TM", moduleSourceOptions);
        Assert.Contains("7 - EMC/Laser-Marking", moduleCompTypeOptions);
        Assert.Contains("V - GIGA S1 (SV)", SharedOptions(provider, "module_ic_brand"));
        Assert.Contains("P - GIGA S1 (SP)", SharedOptions(provider, "module_ic_brand"));
    }

    [Fact]
    public void GeneratePreview_ManufacturingComp_GeneratesIncomingCompAndBins()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "30",
            SourceCode = "X",
            DramTypeCode = "R",
            DensityCode = "AH",
            BitOrganizationCode = "08",
            BankCode = "6",
            InterfaceCode = "V",
            RevisionCode = "A",
            CompTypeCode = "0",
            DieBrandCode = "G",
            VendorCode = "X",
            PackageTypeCode = "B",
            TesterCode = "W"
        });

        Assert.Equal("Incoming", rows[0].Kind);
        Assert.Equal("X4RAH086VA-0GXEL", rows[0].PartCode);
        Assert.Equal("Comp", rows[1].Kind);
        Assert.Equal("XCRAH086VA-0BGWX", rows[1].PartCode);
        Assert.Equal(8, rows.Count);
        Assert.Contains("Only Test Comp", rows[1].Specification);
        Assert.DoesNotContain("TP", rows[1].Specification);
    }

    [Fact]
    public void ParseCompPart_ManufacturingCompMapsToModuleManufacturingSource()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var incomingService = new IncomingCompService(provider, new ProductTextService(provider));
        var moduleService = new ModuleService(provider, new ProductTextService(provider));

        var parsedComp = incomingService.ParseCompPart("30", "XCRAH086VA-0BGWG");
        var parsedModule = moduleService.ParseCompPart("30", "XCRAH086VA-0BGWG");

        Assert.Equal("X", parsedComp.SourceCode);
        Assert.Equal("0", parsedComp.CompTypeCode);
        Assert.Equal("XM", parsedModule.ModuleSourceCode);
        Assert.Equal("0", parsedModule.ModuleCompTypeCode);
    }

    [Fact]
    public void GeneratePreview_ManufacturingModule_AllowsZeroCompTypeAndXmSource()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "XM",
            DramTypeCode = "R",
            DimmTypeCode = "D",
            ModuleDensityCode = "AG",
            DieDensityCode = "A",
            CompositionCode = "8",
            RankCode = "1",
            GenerationCode = "A",
            IcBrandCode = "G",
            ModuleCompTypeCode = "0",
            CompTestCode = "W",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "WM",
            PcbCode = "7",
            VendorCode = "X"
        });

        Assert.Equal("XMRDAG58A1A-G0WRRWM7X", rows[0].PartCode);
        Assert.Equal("XMRDAG58A1A-G0WRRWM7X-TNAGA00", rows[1].PartCode);
        Assert.Contains("RAmos", rows[0].Specification);
        Assert.DoesNotContain("TP", rows[0].Specification);
    }

    [Theory]
    [InlineData("V", "GIGA S1(SV)")]
    [InlineData("P", "GIGA S1(SP)")]
    public void GeneratePreview_IncomingCompSpecification_UsesNewTmDieBrandLabels(
        string dieBrandCode,
        string expectedBrandLabel)
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "30",
            SourceCode = "X",
            DramTypeCode = "R",
            DensityCode = "AH",
            BitOrganizationCode = "08",
            BankCode = "6",
            InterfaceCode = "V",
            RevisionCode = "A",
            CompTypeCode = "0",
            DieBrandCode = dieBrandCode,
            VendorCode = "X",
            PackageTypeCode = "B",
            TesterCode = "W"
        });

        Assert.Contains(expectedBrandLabel, rows[0].Specification);
        Assert.Contains(expectedBrandLabel, rows[1].Specification);
        Assert.Contains(expectedBrandLabel, rows[2].Specification);
    }

    [Theory]
    [InlineData("V", "GIGA S1(SV)")]
    [InlineData("P", "GIGA S1(SP)")]
    public void GeneratePreview_ModuleSpecification_UsesNewTmIcBrandLabels(
        string icBrandCode,
        string expectedBrandLabel)
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "XM",
            DramTypeCode = "R",
            DimmTypeCode = "D",
            ModuleDensityCode = "AG",
            DieDensityCode = "A",
            CompositionCode = "8",
            RankCode = "1",
            GenerationCode = "A",
            IcBrandCode = icBrandCode,
            ModuleCompTypeCode = "0",
            CompTestCode = "W",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "WM",
            PcbCode = "7",
            VendorCode = "X"
        });

        Assert.Contains(expectedBrandLabel, rows[0].Specification);
        Assert.Contains(expectedBrandLabel, rows[1].Specification);
    }

    [Fact]
    public void ParseCompPart_ManufacturingBit48MapsToModuleComposition9()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var incomingService = new IncomingCompService(provider, new ProductTextService(provider));
        var moduleService = new ModuleService(provider, new ProductTextService(provider));

        var rows = incomingService.GeneratePreview(new IncomingCompRequest
        {
            Revision = "30",
            SourceCode = "X",
            DramTypeCode = "R",
            DensityCode = "AH",
            BitOrganizationCode = "48",
            BankCode = "6",
            InterfaceCode = "V",
            RevisionCode = "A",
            CompTypeCode = "3",
            DieBrandCode = "G",
            VendorCode = "X",
            PackageTypeCode = "B",
            TesterCode = "W"
        });

        Assert.Equal("XCRAH486VA-3BGWX", rows[1].PartCode);
        Assert.Contains("DDR5 16Gb x8 (x4 -> x8) A-die GIGA S1 Laser-Marking Comp", rows[1].Specification);

        var parsedModule = moduleService.ParseCompPart("30", rows[1].PartCode);

        Assert.Equal("9", parsedModule.CompositionCode);
    }

    [Fact]
    public void GeneratePreview_ManufacturingModule_Composition9KeepsConversionText()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            CompFullPartCode = "XCRAH486VA-3BGWX",
            DimmTypeCode = "D",
            ModuleDensityCode = "AG",
            RankCode = "1",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "WM",
            PcbCode = "7"
        });

        Assert.Equal("DDR5 UDIMM 16GB (16Gb x8 (x4 -> x8) *8) GIGA S1 RAmos BP PCB(Black)", rows[0].Specification);
    }

    [Fact]
    public void GeneratePreview_Module_RejectsSmallDensityForStandardDimm()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var ex = Assert.Throws<InvalidOperationException>(() => service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "TM",
            DramTypeCode = "R",
            DimmTypeCode = "D",
            ModuleDensityCode = "1G",
            DieDensityCode = "A",
            CompositionCode = "8",
            RankCode = "1",
            GenerationCode = "A",
            IcBrandCode = "G",
            ModuleCompTypeCode = "P",
            CompTestCode = "W",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "WM",
            PcbCode = "7",
            VendorCode = "G",
            PurchaserCode = "H"
        }));

        Assert.Contains("Comp", ex.Message);
    }

    [Fact]
    public void GeneratePreview_Module_AllowsSmallDensityAndNoAssyForCompDimm()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "TM",
            DramTypeCode = "R",
            DimmTypeCode = "C",
            ModuleDensityCode = "1G",
            DieDensityCode = "A",
            CompositionCode = "8",
            RankCode = "0",
            GenerationCode = "A",
            IcBrandCode = "G",
            ModuleCompTypeCode = "P",
            CompTestCode = "W",
            ModuleSmtCode = "0",
            ModuleTestCode = "0",
            SpeedCode = "WM",
            PcbCode = "0",
            VendorCode = "G",
            PurchaserCode = "H"
        });

        Assert.Equal("DDR5 Comp 1GB COO : KR", rows[0].GeneralInfo);
        Assert.Contains("DDR5 16Gb x8 A-die GIGA S1 Partial Comp TP", rows[0].Specification);
        Assert.DoesNotContain("PCB", rows[0].Specification);
    }

    [Fact]
    public void GeneratePreview_ModuleCompDimm_AppliesCompPartDefaults()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            CompFullPartCode = "RCRAH086VA-PBGWG",
            DimmTypeCode = "C",
            SpeedCode = "WM"
        });

        Assert.Equal("RMRC2G58A0A-GPW00WM0GX", rows[0].PartCode);
        Assert.Equal("RMRC2G58A0A-GPW00WM0GX-TN2GA00", rows[1].PartCode);
        Assert.Equal("DDR5 Comp 2GB COO : KR", rows[0].GeneralInfo);
        Assert.Equal("DDR5 16Gb x8 A-die GIGA S1 Partial Comp", rows[0].Specification);
        Assert.Equal("DDR5 16Gb x8 A-die GIGA S1 Partial Comp 5600 MT/s (2800MHz @ 46/45/45)", rows[1].Specification);
    }

    [Fact]
    public void GetCompSaleModuleDensityCode_LeavesUnsupportedDieDensityBlank()
    {
        Assert.Equal("2G", ModuleService.GetCompSaleModuleDensityCode("A"));
        Assert.Equal(string.Empty, ModuleService.GetCompSaleModuleDensityCode("H"));
    }

    [Fact]
    public void GeneratePreview_Rev30_RequiresPurchaserForThirdParty()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        Assert.Throws<InvalidOperationException>(() => service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "30",
            SourceCode = "T",
            DramTypeCode = "R",
            DensityCode = "AH",
            BitOrganizationCode = "08",
            BankCode = "6",
            InterfaceCode = "V",
            RevisionCode = "A",
            CompTypeCode = "P",
            DieBrandCode = "G",
            VendorCode = "G",
            PackageTypeCode = "B",
            TesterCode = "W"
        }));
    }

    [Fact]
    public void GeneratePreview_Rev30_BuildsCompBinsForDdr5()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "30",
            SourceCode = "T",
            DramTypeCode = "R",
            DensityCode = "AH",
            BitOrganizationCode = "08",
            BankCode = "6",
            InterfaceCode = "V",
            RevisionCode = "A",
            CompTypeCode = "P",
            DieBrandCode = "G",
            VendorCode = "G",
            PurchaserCode = "H",
            CompType2Code = "B",
            PackageTypeCode = "B",
            TesterCode = "W"
        });

        Assert.Equal("T4RAH086VA-PGGELHB", rows[0].PartCode);
        Assert.Equal("TCRAH086VA-PBGWGHB", rows[1].PartCode);
        Assert.Equal(8, rows.Count);
        Assert.Equal("TCRAH086VA-PBGWGHB-CF", rows[^1].PartCode);
        Assert.Contains("TP Reball", rows[0].Specification);
        Assert.Contains("TP Reball", rows[1].Specification);
        Assert.Contains("TP Reball 7200 MT/s", rows[2].Specification);
        Assert.Contains("TP Reball 4800 MT/s", rows[^1].Specification);
    }

    [Fact]
    public void GeneratePreview_Rev30_BuildsDdr4CompBinWith3200CaSpeed()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "30",
            SourceCode = "K",
            DramTypeCode = "A",
            DensityCode = "8G",
            BitOrganizationCode = "08",
            BankCode = "5",
            InterfaceCode = "W",
            RevisionCode = "A",
            CompTypeCode = "P",
            DieBrandCode = "G",
            VendorCode = "G",
            PackageTypeCode = "B",
            TesterCode = "W"
        });

        Assert.Equal(3, rows.Count);
        Assert.Equal("RCA8G085WA-PBGWG-CA", rows[2].PartCode);
        Assert.Contains("3200 MT/s", rows[2].Specification);
        Assert.DoesNotContain("7200 MT/s", rows[2].Specification);
    }

    [Theory]
    [InlineData("G", "MDL(GOX)")]
    [InlineData("P", "Partial")]
    [InlineData("U", "Pre-Mark Partial")]
    public void GeneratePreview_IncomingCompSpecification_UsesFullCompTypeDescription(string compTypeCode, string expectedCompTypeText)
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "30",
            SourceCode = "K",
            DramTypeCode = "R",
            DensityCode = "AH",
            BitOrganizationCode = "08",
            BankCode = "6",
            InterfaceCode = "V",
            RevisionCode = "A",
            CompTypeCode = compTypeCode,
            DieBrandCode = "G",
            VendorCode = "G",
            PackageTypeCode = "B",
            TesterCode = "W"
        });

        Assert.Contains($"A-die GIGA S1 {expectedCompTypeText} Comp", rows[0].Specification);
        Assert.Contains($"A-die GIGA S1 {expectedCompTypeText} Comp", rows[1].Specification);
        Assert.DoesNotContain($"{compTypeCode} Comp", rows[0].Specification);
        Assert.DoesNotContain($"{compTypeCode} Comp", rows[1].Specification);
    }

    [Fact]
    public void GeneratePreview_IncomingCompSpecification_UsesPartRevisionForDieLabel()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var parsed = service.ParseCompPart("30", "TCRAH086VP-GBGWGH");
        var rows = service.GeneratePreview(parsed);

        Assert.Contains("P-die", rows[1].Specification);
        Assert.DoesNotContain("G-die", rows[1].Specification);
    }

    [Fact]
    public void GeneratePreview_IncomingCompSpecification_UsesA100AndIcBrandRules()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var parsed = service.ParseCompPart("30", "BCRAH166VH-PBMAAAB");
        var rows = service.GeneratePreview(parsed);

        Assert.Equal("DDR5 16Gb x16 H-die A100 S3 Partial Comp Reball", rows[1].Specification);
        Assert.Equal("DDR5 16Gb x16 H-die A100 S3 Partial Comp Reball 7200 MT/s", rows[2].Specification);
        Assert.DoesNotContain("TP", rows[1].Specification);
        Assert.DoesNotContain("ADATA", rows[1].Specification);
    }

    [Fact]
    public void GeneratePreview_ModuleFullPart_AutoGeneratesBaseAndBin()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleFullPartCode = "TMRDAG58A1P-GPWRRWM7GH"
        });

        Assert.Equal(2, rows.Count);
        Assert.Equal("TMRDAG58A1P-GPWRRWM7GH", rows[0].PartCode);
        Assert.Equal("TMRDAG58A1P-GPWRRWM7GH-TNAGA00", rows[1].PartCode);
        Assert.Equal("Module BIN", rows[1].Kind);
    }

    [Fact]
    public void GeneratePreview_ModuleFullPart_FinishedProductRetestGeneratesDummyAndZeroYParts()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleFullPartCode = "RMRDAG58A1P-GPWRRWM7G",
            IsFinishedProductRetest = true
        });

        Assert.Equal(3, rows.Count);
        Assert.Equal("Module Dummy", rows[0].Kind);
        Assert.Equal("RMRDAG58A1P-GPWRRWM7G00", rows[0].PartCode);
        Assert.EndsWith("Dummy", rows[0].Specification);
        Assert.Equal("Module", rows[1].Kind);
        Assert.Equal("RMRDAG58A1P-GPWRRWM7G0Y", rows[1].PartCode);
        Assert.Equal("Module BIN", rows[2].Kind);
        Assert.Equal("RMRDAG58A1P-GPWRRWM7G0Y-TNAGA00", rows[2].PartCode);
    }

    [Theory]
    [InlineData("RMRDAG58A1P-GPWRRWM7G00")]
    [InlineData("RMRDAG58A1P-GPWRRWM7G0Y")]
    [InlineData("RMRDAG58A1P-GPWRRWM7G0Y-TNAGA00")]
    public void ParseModuleFullPart_FinishedProductRetestUsesLastTwoCharacters(string partCode)
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var parsed = service.ParseModuleFullPart("30", partCode);

        Assert.True(parsed.IsFinishedProductRetest);
        Assert.Equal("G", parsed.VendorCode);
        Assert.Equal("RMRDAG58A1P-GPWRRWM7G", parsed.BasePartCode);
    }

    [Fact]
    public void GeneratePreview_FinishedProductRetestTakesPriorityOverRepairDummy()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleFullPartCode = "BMRDAG58A1A-CPARRWMAAAR",
            IsFinishedProductRetest = true
        });

        Assert.Equal(3, rows.Count);
        Assert.Equal("BMRDAG58A1A-CPARRWMAAAR00", rows[0].PartCode);
        Assert.Equal("BMRDAG58A1A-CPARRWMAAAR0Y", rows[1].PartCode);
        Assert.DoesNotContain(rows, row => row.PartCode == "BMRDAG58A1A-CPARRWMAAA00");
    }

    [Fact]
    public void GeneratePreview_ModuleFullPart_BuildsReadableModuleTexts()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleFullPartCode = "TMRDAG58A1P-GPWRRWM7GH"
        });

        Assert.Equal("UDIMM 16GB COO : KR", rows[0].GeneralInfo);
        Assert.Equal("DDR5 UDIMM 16GB (16Gb x8 *8) GIGA S1 RAmos TP BP PCB(Black)", rows[0].Specification);
        Assert.Equal("DDR5 UDIMM 16GB (16Gb x8 *8) GIGA S1 RAmos TP BP PCB(Black) 5600 MT/s (2800MHz @ 46/45/45)", rows[1].Specification);
    }

    [Theory]
    [InlineData("TMRDAG58A1P-GPWRRWM7GH", "RAmos TP", "RMHK")]
    [InlineData("BMRDAG58A1P-GPWRRWM7GH", "CT TP", "RMHK")]
    [InlineData("BMRDAG58A1P-GPWRRWM7GA", "CT TP", "A100")]
    [InlineData("BMRDAG58A1P-GPWRRWMBAA", "A100", "CT TP")]
    [InlineData("BMRDAG58A1P-GPWRRWM7AA", "A100", "CT TP")]
    public void GeneratePreview_ModuleFullPart_UsesSourceOwnerForThirdPartyUnlessA100(
        string moduleFullPartCode,
        string expectedOwnerText,
        string unexpectedOwnerText)
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleFullPartCode = moduleFullPartCode
        });

        Assert.Contains(expectedOwnerText, rows[0].Specification);
        Assert.DoesNotContain(unexpectedOwnerText, rows[0].Specification);
    }

    [Fact]
    public void BuildModuleTexts_CompSale_UsesFullCompTypeDescription()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ProductTextService(provider);

        var texts = service.BuildModuleTexts(
            partCode: "TEST",
            moduleSourceCode: "TM",
            dramTypeLabel: "DDR5",
            formFactorLabel: "Comp",
            capacityLabel: "16GB",
            dieDensityLabel: "16Gb",
            compositionCode: "8",
            icCountText: "8",
            generationCode: "G",
            icBrandCode: "G",
            moduleCompTypeCode: "P - Partial",
            vendorCode: "G",
            purchaserCode: "H",
            pcbCode: "7",
            isThirdParty: true,
            specialCode2Code: "",
            specialCode3Code: "",
            speedText: "6400 MT/s",
            isCompSale: true);

        Assert.Equal("DDR5 16Gb x8 G-die GIGA S1 Partial Comp TP 6400 MT/s", texts.Specification);
    }

    [Fact]
    public void BuildModuleTexts_CompSale_MapsMdlGoxCodeToFullDescription()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ProductTextService(provider);

        var texts = service.BuildModuleTexts(
            partCode: "TEST",
            moduleSourceCode: "TM",
            dramTypeLabel: "DDR5",
            formFactorLabel: "Comp",
            capacityLabel: "16GB",
            dieDensityLabel: "16Gb",
            compositionCode: "8",
            icCountText: "8",
            generationCode: "G",
            icBrandCode: "G",
            moduleCompTypeCode: "G - MDL(GOX)",
            vendorCode: "G",
            purchaserCode: "H",
            pcbCode: "7",
            isThirdParty: true,
            specialCode2Code: "",
            specialCode3Code: "",
            isCompSale: true);

        Assert.Equal("DDR5 16Gb x8 G-die GIGA S1 MDL(GOX) Comp TP", texts.Specification);
    }

    [Fact]
    public void BuildModuleTexts_MapsAd5u8c0PcbCode()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ProductTextService(provider);

        var texts = service.BuildModuleTexts(
            partCode: "TEST",
            moduleSourceCode: "RM",
            dramTypeLabel: "DDR5",
            formFactorLabel: "UDIMM",
            capacityLabel: "16GB",
            dieDensityLabel: "16Gb",
            compositionCode: "8",
            icCountText: "8",
            generationCode: "G",
            icBrandCode: "G",
            moduleCompTypeCode: "P",
            vendorCode: "G",
            purchaserCode: "",
            pcbCode: "B",
            isThirdParty: false,
            specialCode2Code: "",
            specialCode3Code: "");

        Assert.Equal("DDR5 UDIMM 16GB (16Gb x8 *8) GIGA S1 RAmos BP PCB(Black)", texts.Specification);
    }

    [Fact]
    public void GeneratePreview_ModuleFullPart_UsesStandardModuleSpecRulesForA100AndPcb()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleFullPartCode = "BMRD8G56A1H-MPARRWMBAA"
        });

        Assert.Equal("DDR5 UDIMM 8GB (16Gb x16 *4) S3 A100 BP PCB(Black)", rows[0].Specification);
        Assert.Equal("DDR5 UDIMM 8GB (16Gb x16 *4) S3 A100 BP PCB(Black) 5600 MT/s (2800MHz @ 46/45/45)", rows[1].Specification);
        Assert.DoesNotContain("TP", rows[0].Specification);
        Assert.DoesNotContain("ADATA", rows[0].Specification);
    }

    [Fact]
    public void GeneratePreview_ModuleFullPart_DoesNotUseA100OwnerForInternalSource()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleFullPartCode = "RMRDAG58A1P-GPARRWM7AA"
        });

        Assert.Contains("RAmos", rows[0].Specification);
        Assert.DoesNotContain("A100", rows[0].Specification);
        Assert.DoesNotContain("TP", rows[0].Specification);
    }

    [Theory]
    [InlineData("BMRDAG58A1A-CPARRWMAAAR", "BMRDAG58A1A-CPARRWMAAA00", "1st Repair", "Dummy")]
    [InlineData("BMRDAG58A1A-CPARRWMAAAS", "BMRDAG58A1A-CPARRWMAAAR0", "2nd Repair", "2nd Repair Dummy")]
    [InlineData("BMRDAG58A1A-CPARRWMAAAC", "BMRDAG58A1A-CPARRWMAAAB0", "Reball Repair", "Reball Repair Dummy")]
    public void GeneratePreview_ModuleFullPart_GeneratesRepairDummy(
        string moduleFullPartCode,
        string expectedDummyPartCode,
        string sourceStatusLabel,
        string expectedDummyStatusLabel)
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleFullPartCode = moduleFullPartCode
        });

        Assert.Equal(3, rows.Count);
        Assert.Equal("Module", rows[0].Kind);
        Assert.Equal("Module Dummy", rows[1].Kind);
        Assert.Equal("Module BIN", rows[2].Kind);
        Assert.Equal(expectedDummyPartCode, rows[1].PartCode);
        Assert.Equal(expectedDummyPartCode, rows[1].Name);
        Assert.Equal(rows[0].GeneralInfo, rows[1].GeneralInfo);
        Assert.EndsWith(expectedDummyStatusLabel, rows[1].Specification);
        Assert.DoesNotContain($"{sourceStatusLabel} {expectedDummyStatusLabel}", rows[1].Specification);
    }

    [Fact]
    public void GeneratePreview_ModuleFullPart_DoesNotGenerateDummyForReball()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleFullPartCode = "BMRDAG58A1A-CPARRWMAAAB"
        });

        Assert.Equal(2, rows.Count);
        Assert.DoesNotContain(rows, row => row.Kind == "Module Dummy");
        Assert.Contains("Reball", rows[0].Specification);
    }

    [Fact]
    public void GeneratePreview_IncomingComp_RejectsInvalidTesterCode()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var ex = Assert.Throws<InvalidOperationException>(() => service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "30",
            SourceCode = "K",
            DramTypeCode = "R",
            DensityCode = "AH",
            BitOrganizationCode = "08",
            BankCode = "6",
            InterfaceCode = "V",
            RevisionCode = "A",
            CompTypeCode = "P",
            DieBrandCode = "G",
            VendorCode = "G",
            PackageTypeCode = "B",
            TesterCode = "Z"
        }));

        Assert.Contains("Tester", ex.Message);
    }

    [Fact]
    public void GeneratePreview_Module_RejectsInvalidSpeedForDdr4()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var ex = Assert.Throws<InvalidOperationException>(() => service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "RM",
            DramTypeCode = "4",
            DimmTypeCode = "D",
            ModuleDensityCode = "8G",
            DieDensityCode = "8",
            CompositionCode = "8",
            RankCode = "1",
            GenerationCode = "A",
            IcBrandCode = "G",
            ModuleCompTypeCode = "P",
            CompTestCode = "W",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "QK",
            PcbCode = "1",
            VendorCode = "G"
        }));

        Assert.Contains("DDR4 Module", ex.Message);
    }

    [Fact]
    public void GeneratePreview_Module_CaSpeed_UsesBankVdd8()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        Assert.Contains("8 - 32Bank / 1.25V", provider.SharedSpec.CodeOptions["module_bank_vdd"]);

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "RM",
            DramTypeCode = "R",
            DimmTypeCode = "D",
            ModuleDensityCode = "AG",
            DieDensityCode = "A",
            CompositionCode = "8",
            RankCode = "1",
            GenerationCode = "A",
            IcBrandCode = "G",
            ModuleCompTypeCode = "P",
            CompTestCode = "W",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "CA",
            PcbCode = "7",
            VendorCode = "G"
        });

        Assert.Equal("RMRDAG88A1A-GPWRRCA7G", rows[0].PartCode);
        Assert.Contains("6000 MT/s (3000MHz @ 48/48/48)", rows[1].Specification);
    }

    [Fact]
    public void GeneratePreview_Module_CaSpeed_RejectsOtherBankVdd()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var ex = Assert.Throws<InvalidOperationException>(() => service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "RM",
            DramTypeCode = "R",
            DimmTypeCode = "D",
            ModuleDensityCode = "AG",
            BankVddCode = "6",
            DieDensityCode = "A",
            CompositionCode = "8",
            RankCode = "1",
            GenerationCode = "A",
            IcBrandCode = "G",
            ModuleCompTypeCode = "P",
            CompTestCode = "W",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "CA",
            PcbCode = "7",
            VendorCode = "G"
        }));

        Assert.Contains("Bank/VDD code 8", ex.Message);
    }

    [Fact]
    public void ParseCompPart_RejectsDdr5WithDdr4Bank()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var ex = Assert.Throws<InvalidOperationException>(() => service.ParseCompPart("30", "TCRAH085VP-GBGWGH"));

        Assert.Contains("DDR5", ex.Message);
        Assert.Contains("Bank 6", ex.Message);
    }

    [Fact]
    public void GeneratePreview_Module_RejectsDdr5WithDdr4DieDensity()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var ex = Assert.Throws<InvalidOperationException>(() => service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "TM",
            DramTypeCode = "R",
            DimmTypeCode = "D",
            ModuleDensityCode = "AG",
            DieDensityCode = "8",
            CompositionCode = "8",
            RankCode = "2",
            GenerationCode = "A",
            IcBrandCode = "G",
            ModuleCompTypeCode = "P",
            CompTestCode = "W",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "WM",
            PcbCode = "7",
            VendorCode = "G",
            PurchaserCode = "H"
        }));

        Assert.Contains("DDR5 Module", ex.Message);
        Assert.Contains("Die Density", ex.Message);
    }

    [Fact]
    public void GeneratePreview_Module_RejectsDensityCompositionRankMismatch()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var ex = Assert.Throws<InvalidOperationException>(() => service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "TM",
            DramTypeCode = "R",
            DimmTypeCode = "D",
            ModuleDensityCode = "AG",
            DieDensityCode = "A",
            CompositionCode = "8",
            RankCode = "2",
            GenerationCode = "A",
            IcBrandCode = "G",
            ModuleCompTypeCode = "P",
            CompTestCode = "W",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "WM",
            PcbCode = "7",
            VendorCode = "G",
            PurchaserCode = "H"
        }));

        Assert.Contains("선택값은 16GB", ex.Message);
        Assert.Contains("계산값은 32GB", ex.Message);
    }

    [Fact]
    public void GeneratePreview_Module_A100NoneOptionIsTreatedAsBlank()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "TM",
            DramTypeCode = "R",
            DimmTypeCode = "D",
            ModuleDensityCode = "AG",
            DieDensityCode = "A",
            CompositionCode = "8",
            RankCode = "1",
            GenerationCode = "P",
            IcBrandCode = "G",
            ModuleCompTypeCode = "P",
            CompTestCode = "W",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "WM",
            PcbCode = "7",
            VendorCode = "G",
            PurchaserCode = "H",
            A100SpecialCode = "(None) - MPS PMIC + Renesas SPD"
        });

        Assert.Equal("TMRDAG58A1P-GPWRRWM7GH", rows[0].PartCode);
    }

    [Fact]
    public void GeneratePreview_Module_SpecialCode1Table2_AllowsRambusCodeForNonA100()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "RM",
            DramTypeCode = "R",
            DimmTypeCode = "D",
            ModuleDensityCode = "AG",
            DieDensityCode = "A",
            CompositionCode = "8",
            RankCode = "1",
            GenerationCode = "P",
            IcBrandCode = "G",
            ModuleCompTypeCode = "P",
            CompTestCode = "W",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "WM",
            PcbCode = "7",
            VendorCode = "G",
            A100SpecialCode = "B"
        });

        Assert.Equal("RMRDAG58A1P-GPWRRWM7GB", rows[0].PartCode);
    }

    [Fact]
    public void GeneratePreview_Module_SpecialCode1Table1CodeRejectedForNonA100()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var ex = Assert.Throws<InvalidOperationException>(() => service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            ModuleSourceCode = "RM",
            DramTypeCode = "R",
            DimmTypeCode = "D",
            ModuleDensityCode = "AG",
            DieDensityCode = "A",
            CompositionCode = "8",
            RankCode = "1",
            GenerationCode = "P",
            IcBrandCode = "G",
            ModuleCompTypeCode = "P",
            CompTestCode = "W",
            ModuleSmtCode = "R",
            ModuleTestCode = "R",
            SpeedCode = "WM",
            PcbCode = "7",
            VendorCode = "G",
            A100SpecialCode = "1"
        }));

        Assert.Contains("Special Code 1", ex.Message);
        Assert.Contains("Table 2", ex.Message);
    }

    [Fact]
    public void ParseModuleFullPart_SpecialCode1Table2_RoundTripsRambusCode()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var request = service.ParseModuleFullPart("30", "RMRDAG58A1P-GPWRRWM7GBR");

        Assert.Equal("B", request.A100SpecialCode);
        Assert.Equal("R", request.SpecialCode2Code);
    }

    [Fact]
    public void GeneratePreview_ModuleCompDimm_AutoAppliesSpecialCode1NA()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new ModuleRequest
        {
            Revision = "30",
            CompFullPartCode = "RCRAH086VA-PBGWG",
            DimmTypeCode = "C",
            SpeedCode = "WM"
        });

        Assert.EndsWith("X", rows[0].PartCode);
    }

    [Fact]
    public void ParseModuleFullPart_NonA100SpecialCode2_DoesNotPopulateA100Special()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var request = service.ParseModuleFullPart("30", "TMRDAG58A1P-GPWRRWM7GHR");

        Assert.True(string.IsNullOrEmpty(request.A100SpecialCode));
        Assert.Equal("R", request.SpecialCode2Code);
        Assert.True(string.IsNullOrEmpty(request.SpecialCode3Code));
    }

    [Fact]
    public void ParseModuleFullPart_A100SpecialCode_RequiresA100Condition()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var request = service.ParseModuleFullPart("30", "TMRDAG58A1P-GPARRWM7AA1R");

        Assert.Equal("1", request.A100SpecialCode);
        Assert.Equal("R", request.SpecialCode2Code);
        Assert.True(string.IsNullOrEmpty(request.SpecialCode3Code));
    }

    [Fact]
    public void ParseModuleFullPart_A100SpecialCode_AllowsNonACompTestWhenVendorAndPurchaserAreA()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new ModuleService(provider, new ProductTextService(provider));

        var request = service.ParseModuleFullPart("30", "TMRDAG58A1P-GPWRRWM7AA1R");

        Assert.Equal("W", request.CompTestCode);
        Assert.Equal("1", request.A100SpecialCode);
        Assert.Equal("R", request.SpecialCode2Code);
    }

    [Fact]
    public void GenerateAndParse_IncomingComp_RoundTripsCompPart()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "30",
            SourceCode = "T",
            DramTypeCode = "R",
            DensityCode = "AH",
            BitOrganizationCode = "08",
            BankCode = "6",
            InterfaceCode = "V",
            RevisionCode = "A",
            CompTypeCode = "P",
            DieBrandCode = "G",
            VendorCode = "G",
            PurchaserCode = "H",
            CompType2Code = "B",
            PackageTypeCode = "B",
            TesterCode = "W"
        });

        var parsed = service.ParseCompPart("30", rows[1].PartCode);
        var regeneratedRows = service.GeneratePreview(parsed);

        Assert.Equal(rows[1].PartCode, regeneratedRows[1].PartCode);
        Assert.Equal(rows[^1].PartCode, regeneratedRows[^1].PartCode);
    }

    [Fact]
    public void ExportRegistration_CreatesOpenXmlWorkbook()
    {
        var exporter = new RegistrationExcelExporter();
        var content = exporter.Export(new[]
        {
            new GeneratedPartRow("Comp", "TEST-PART", "TEST-PART", "", "DDR5 test spec")
        });

        using var stream = new MemoryStream(content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.NotNull(archive.GetEntry("[Content_Types].xml"));
        Assert.NotNull(archive.GetEntry("xl/workbook.xml"));
        Assert.NotNull(archive.GetEntry("xl/worksheets/sheet1.xml"));

        using var sheetReader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheetXml = sheetReader.ReadToEnd();
        Assert.Contains("품목코드", sheetXml);
        Assert.DoesNotContain("영업코드", sheetXml);
        Assert.Contains("Comp", sheetXml);
        Assert.DoesNotContain("비고", sheetXml);
        Assert.Contains("TEST-PART", sheetXml);
        Assert.Contains("DDR5 test spec", sheetXml);
    }

    [Fact]
    public void ExportRegistration_AddsSalesCodeColumnAndArialFontWhenModuleRowsExist()
    {
        var exporter = new RegistrationExcelExporter();
        var content = exporter.Export(new[]
        {
            new GeneratedPartRow("Comp", "COMP-PART", "COMP-PART", "", "DDR5 comp spec"),
            new GeneratedPartRow("Module", "MDL-PART", "MDL-PART", "UDIMM 16GB COO : KR", "DDR5 module spec")
        });

        using var stream = new MemoryStream(content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        using var sheetReader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheetXml = sheetReader.ReadToEnd();
        Assert.Contains("<c r=\"B1\" t=\"inlineStr\" s=\"1\"><is><t>품목코드</t></is></c>", sheetXml);
        Assert.Contains("<c r=\"C1\" t=\"inlineStr\" s=\"1\"><is><t>품목명</t></is></c>", sheetXml);
        Assert.Contains("<c r=\"D1\" t=\"inlineStr\" s=\"1\"><is><t>영업코드</t></is></c>", sheetXml);
        Assert.Contains("<c r=\"E1\" t=\"inlineStr\" s=\"1\"><is><t>품목일반정보</t></is></c>", sheetXml);
        Assert.Contains("<c r=\"A3\" t=\"inlineStr\" s=\"0\"><is><t>MDL</t></is></c>", sheetXml);

        using var stylesReader = new StreamReader(archive.GetEntry("xl/styles.xml")!.Open());
        var stylesXml = stylesReader.ReadToEnd();
        Assert.Contains("<name val=\"Arial\"/>", stylesXml);
        Assert.DoesNotContain("Segoe UI", stylesXml);
    }

    [Fact]
    public void ExportRegistration_AutoSizesSpecificationColumnFromLongestText()
    {
        var exporter = new RegistrationExcelExporter();
        var content = exporter.Export(new[]
        {
            new GeneratedPartRow("Comp", "SHORT", "SHORT", "", "DDR4 short spec"),
            new GeneratedPartRow(
                "Comp BIN",
                "ZCAAG485WA-5BGRX-CA",
                "ZCAAG485WA-5BGRX-CA",
                "",
                "DDR4 16Gb x8 (x4 -> x8) A-die S1 Reball/EMC/Laser-Marking Comp 3200 MT/s (1600MHz @ 22/22/22)")
        });

        using var stream = new MemoryStream(content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        using var sheetReader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheetXml = sheetReader.ReadToEnd();
        var sheetDocument = System.Xml.Linq.XDocument.Parse(sheetXml);
        var sheetNamespace = System.Xml.Linq.XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        var specificationColumn = sheetDocument
            .Descendants(sheetNamespace + "col")
            .Single(column => column.Attribute("min")?.Value == "5" && column.Attribute("max")?.Value == "5");
        var specificationWidth = double.Parse(
            specificationColumn.Attribute("width")!.Value,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(specificationWidth > 56d);

        using var stylesReader = new StreamReader(archive.GetEntry("xl/styles.xml")!.Open());
        var stylesXml = stylesReader.ReadToEnd();
        Assert.DoesNotContain("wrapText=\"1\"", stylesXml);
    }

    [Fact]
    public void ExportRegistration_FormatsModuleDummyKindAsMdlDummy()
    {
        var exporter = new RegistrationExcelExporter();
        var content = exporter.Export(new[]
        {
            new GeneratedPartRow("Module Dummy", "DUMMY-PART", "DUMMY-PART", "UDIMM 16GB COO : KR", "DDR5 module dummy spec")
        });

        using var stream = new MemoryStream(content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        using var sheetReader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheetXml = sheetReader.ReadToEnd();
        Assert.Contains("<c r=\"A2\" t=\"inlineStr\" s=\"0\"><is><t>MDL Dummy</t></is></c>", sheetXml);
        Assert.Contains("영업코드", sheetXml);
    }
}
