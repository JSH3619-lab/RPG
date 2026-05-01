using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;
using RamosPartGenerator.Excel;
using System.IO.Compression;

namespace RamosPartGenerator.Tests;

public class UnitTest1
{
    [Fact]
    public void BuildModuleLookups_UsesModuleDieDensityCodes()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var catalog = new RamosPartGenerator.Api.Services.LookupCatalog(provider);

        var page = catalog.BuildModule("30");
        var dieDensityField = Assert.Single(page.Fields, field => field.Key == "dieDensityCode");

        Assert.Equal(new[] { "4 - 4Gb", "8 - 8Gb", "A - 16Gb", "H - 24Gb", "B - 32Gb" }, dieDensityField.Options);
    }

    [Fact]
    public void BuildModuleLookups_UsesSpecOptionsForRev30CompDimm()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var catalog = new RamosPartGenerator.Api.Services.LookupCatalog(provider);

        var page = catalog.BuildModule("30");
        var dimmTypeField = Assert.Single(page.Fields, field => field.Key == "dimmTypeCode");

        Assert.Contains("C - Comp", dimmTypeField.Options);
        Assert.DoesNotContain("Comp - Comp", dimmTypeField.Options);
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

        Assert.Contains($"{expectedCompTypeText} Comp", rows[0].Specification);
        Assert.Contains($"{expectedCompTypeText} Comp", rows[1].Specification);
        Assert.DoesNotContain($"{compTypeCode} Comp", rows[0].Specification);
        Assert.DoesNotContain($"{compTypeCode} Comp", rows[1].Specification);
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
        Assert.Equal("DDR5 UDIMM 16GB (16Gb x8 *8) RMHK (BP PCB) TP", rows[0].Specification);
        Assert.Equal("DDR5 UDIMM 16GB (16Gb x8 *8) RMHK (BP PCB) TP 5600 MT/s", rows[1].Specification);
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

        var request = service.ParseModuleFullPart("30", "TMRDAG58A1P-GPWRRWM7AA1R");

        Assert.Equal("1", request.A100SpecialCode);
        Assert.Equal("R", request.SpecialCode2Code);
        Assert.True(string.IsNullOrEmpty(request.SpecialCode3Code));
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
            new GeneratedPartRow("Comp", "TEST-PART", "TEST-PART", "", "DDR5 test spec", "note")
        });

        using var stream = new MemoryStream(content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.NotNull(archive.GetEntry("[Content_Types].xml"));
        Assert.NotNull(archive.GetEntry("xl/workbook.xml"));
        Assert.NotNull(archive.GetEntry("xl/worksheets/sheet1.xml"));

        using var sheetReader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheetXml = sheetReader.ReadToEnd();
        Assert.Contains("TEST-PART", sheetXml);
        Assert.Contains("DDR5 test spec", sheetXml);
    }
}
