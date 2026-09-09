using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Tests;

public sealed class LpddrServiceTests
{
    // ERP에 실제 등록된 LPDDR4 입고 Comp 파트 3건 (B5 = CTST Third Party, Purchaser H)
    [Theory]
    [InlineData("A", "G", "B5F1G324H1M-PAGGEBH", "LPDDR4 1GB Comp X32 M-die GIGA S1 Partial 1.0T / 10x15")]
    [InlineData("G", "G", "B5F1G324H1M-PGGGEBH", "LPDDR4 1GB Comp X32 M-die GIGA S1 Partial 1.14T / 10x15")]
    [InlineData("F", "M", "B5F1G324H1M-PFMGEBH", "LPDDR4 1GB Comp X32 M-die GIGA S3 Partial 1.05T / 10x14.5")]
    public void GeneratePreview_PkgIncoming_ReproducesRegisteredErpParts(
        string packageTypeCode, string dieBrandCode, string expectedCode, string expectedSpec)
    {
        var rows = CreateService().GeneratePreview(new LpddrRequest
        {
            SourceCode = "B",
            IsDieIncoming = false,
            DramTypeCode = "F",
            DensityCode = "1G",
            BitCode = "32",
            BankCode = "4",
            InterfaceCode = "H",
            StackCode = "1",
            RevisionCode = "M",
            DieTypeCode = "P",
            PackageTypeCode = packageTypeCode,
            DieBrandCode = dieBrandCode,
            AssemblyCode = "0",
            TesterCode = "R",
            SpeedCode = "CP",
            PcbRevisionCode = "1",
            VendorCode = "G",
            PurchaserCode = "H"
        });

        var incoming = rows[0];
        Assert.Equal("Incoming", incoming.Kind);
        Assert.Equal(expectedCode, incoming.PartCode);
        Assert.Equal(expectedCode, incoming.Name);
        Assert.Equal(string.Empty, incoming.GeneralInfo);
        Assert.Equal(expectedSpec, incoming.Specification);
    }

    [Fact]
    public void GeneratePreview_PkgIncoming_GeneratesProductChain()
    {
        var rows = CreateService().GeneratePreview(new LpddrRequest
        {
            SourceCode = "B",
            IsDieIncoming = false,
            DramTypeCode = "F",
            DensityCode = "1G",
            BitCode = "32",
            BankCode = "4",
            InterfaceCode = "H",
            StackCode = "1",
            RevisionCode = "M",
            DieTypeCode = "P",
            PackageTypeCode = "A",
            DieBrandCode = "G",
            AssemblyCode = "0",
            TesterCode = "R",
            SpeedCode = "CP",
            PcbRevisionCode = "1",
            VendorCode = "G",
            PurchaserCode = "H"
        });

        // 입고(K5)가 Product(RP)로 바로 연결된다. Comp(RD)는 추후 변경 전까지 생성하지 않는다.
        // Product는 DRAM Module처럼 베이스 + BIN(-TN…A00) 2행을 생성한다.
        Assert.Equal(3, rows.Count);
        Assert.Equal("Product", rows[1].Kind);
        Assert.Equal("BPF1G324H1M-PA0RCP1GGH", rows[1].PartCode);
        Assert.Equal("LPDDR4 1GB Comp X32 M-die GIGA S1 Partial 1.0T / 10x15 6400 Mbps", rows[1].Specification);
        Assert.Equal("Product BIN", rows[2].Kind);
        Assert.Equal("BPF1G324H1M-PA0RCP1GGH-TN1GA00", rows[2].PartCode);
        Assert.Equal(rows[1].Specification, rows[2].Specification);

        // 입고는 일반정보 공란, Product/Product BIN은 MDL 파트라 일반정보 생성 (Comp 판매용 MDL 패턴)
        Assert.Equal(string.Empty, rows[0].GeneralInfo);
        Assert.Equal("LPDDR4 Comp 1GB COO : KR", rows[1].GeneralInfo);
        Assert.Equal("LPDDR4 Comp 1GB COO : KR", rows[2].GeneralInfo);
    }

    [Fact]
    public void GeneratePreview_DieIncoming_BuildsK4CodeFromPdfExample()
    {
        // PDF Rev.31.0 p.10 예시: K4 F 4K 16 4 H M - W G G EB (Purchaser 0 = 생략)
        var rows = CreateService().GeneratePreview(new LpddrRequest
        {
            SourceCode = "K",
            IsDieIncoming = true,
            DramTypeCode = "F",
            DieDensityCode = "4K",
            DieBitCode = "16",
            DensityCode = "1G",
            BitCode = "32",
            BankCode = "4",
            InterfaceCode = "H",
            StackCode = "1",
            RevisionCode = "M",
            DieTypeCode = "W",
            PackageTypeCode = "A",
            DieBrandCode = "G",
            AssemblyCode = "R",
            TesterCode = "R",
            SpeedCode = "CH",
            PcbRevisionCode = "1",
            VendorCode = "G",
            PurchaserCode = ""
        });

        Assert.Equal(3, rows.Count);
        Assert.Equal("K4F4K164HM-WGGEB", rows[0].PartCode);
        Assert.Equal("LPDDR4 4Gb Die x16 M-die GIGA S1 Whole", rows[0].Specification);
        Assert.Equal("RPF1G324H1M-WARRCH1GG", rows[1].PartCode);
        Assert.Equal("RPF1G324H1M-WARRCH1GG-TN1GA00", rows[2].PartCode);
    }

    [Fact]
    public void GeneratePreview_ThirdPartyWithoutPurchaser_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateService().GeneratePreview(new LpddrRequest
        {
            SourceCode = "T",
            IsDieIncoming = false,
            DramTypeCode = "L",
            DensityCode = "8G",
            BitCode = "16",
            BankCode = "5",
            InterfaceCode = "Y",
            StackCode = "2",
            RevisionCode = "B",
            DieTypeCode = "P",
            PackageTypeCode = "B",
            DieBrandCode = "S",
            AssemblyCode = "0",
            TesterCode = "W",
            SpeedCode = "CP",
            PcbRevisionCode = "1",
            VendorCode = "S",
            PurchaserCode = ""
        }));

        Assert.Contains("Purchaser", exception.Message);
    }

    // 역파싱 → 재생성 라운드트립: 파싱한 코드가 입고 행으로 그대로 재현되어야 한다.
    [Theory]
    [InlineData("B5F1G324H1M-PAGGEBH")]
    [InlineData("B5F1G324H1M-PGGGEBH")]
    [InlineData("B5F1G324H1M-PFMGEBH")]
    public void ParsePart_PkgIncoming_RoundTripsToSameIncomingCode(string partCode)
    {
        var service = CreateService();

        var parsed = service.ParsePart(partCode);

        Assert.False(parsed.IsDieIncoming);
        Assert.Equal("B", parsed.SourceCode);
        Assert.Equal("H", parsed.PurchaserCode);
        Assert.Equal(string.Empty, parsed.SpeedCode);

        // K5 코드에 없는 Comp 필드만 채워서 재생성하면 동일한 입고 코드가 나온다.
        var rows = service.GeneratePreview(new LpddrRequest
        {
            SourceCode = parsed.SourceCode,
            IsDieIncoming = parsed.IsDieIncoming,
            DramTypeCode = parsed.DramTypeCode,
            DensityCode = parsed.DensityCode,
            BitCode = parsed.BitCode,
            BankCode = parsed.BankCode,
            InterfaceCode = parsed.InterfaceCode,
            StackCode = parsed.StackCode,
            RevisionCode = parsed.RevisionCode,
            DieTypeCode = parsed.DieTypeCode,
            PackageTypeCode = parsed.PackageTypeCode,
            DieBrandCode = parsed.DieBrandCode,
            VendorCode = parsed.VendorCode,
            PurchaserCode = parsed.PurchaserCode,
            AssemblyCode = "0",
            TesterCode = "R",
            SpeedCode = "CP",
            PcbRevisionCode = "1"
        });

        Assert.Equal(partCode, rows[0].PartCode);
    }

    [Theory]
    [InlineData("RDF1G324H1M-WARRCH1GG")]
    [InlineData("RPF1G324H1M-WARRCH1GG-TN1GA00")]
    public void ParsePart_CompAndProduct_RestoreAllCompFields(string partCode)
    {
        var parsed = CreateService().ParsePart(partCode);

        Assert.False(parsed.IsDieIncoming);
        Assert.Equal("K", parsed.SourceCode);
        Assert.Equal("F", parsed.DramTypeCode);
        Assert.Equal("1G", parsed.DensityCode);
        Assert.Equal("W", parsed.DieTypeCode);
        Assert.Equal("A", parsed.PackageTypeCode);
        Assert.Equal("R", parsed.AssemblyCode);
        Assert.Equal("R", parsed.TesterCode);
        Assert.Equal("CH", parsed.SpeedCode);
        Assert.Equal("1", parsed.PcbRevisionCode);
        Assert.Equal("G", parsed.DieBrandCode);
        Assert.Equal("G", parsed.VendorCode);
        Assert.Equal(string.Empty, parsed.PurchaserCode);
    }

    [Fact]
    public void ParsePart_DieIncoming_RestoresDieFields()
    {
        var parsed = CreateService().ParsePart("K4F4K164HM-WGGEB");

        Assert.True(parsed.IsDieIncoming);
        Assert.Equal("K", parsed.SourceCode);
        Assert.Equal("4K", parsed.DieDensityCode);
        Assert.Equal("16", parsed.DieBitCode);
        Assert.Equal("4", parsed.BankCode);
        Assert.Equal("H", parsed.InterfaceCode);
        Assert.Equal("M", parsed.RevisionCode);
        Assert.Equal("W", parsed.DieTypeCode);
        Assert.Equal("G", parsed.DieBrandCode);
        Assert.Equal("G", parsed.VendorCode);
        Assert.Equal(string.Empty, parsed.PurchaserCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("B5F1G324H1M")]
    [InlineData("A5F1G324H1M-PAGGEBH")]
    [InlineData("B5F1G324H1-PAGGEBH")]
    [InlineData("B5F1G324H1M-PAGGXBH")]
    public void ParsePart_InvalidCode_Throws(string partCode)
    {
        Assert.Throws<InvalidOperationException>(() => CreateService().ParsePart(partCode));
    }

    [Fact]
    public void Export_LpddrProductRows_IncludeSalesCodeColumn()
    {
        var rows = CreateService().GeneratePreview(new LpddrRequest
        {
            SourceCode = "B",
            IsDieIncoming = false,
            DramTypeCode = "F",
            DensityCode = "2G",
            BitCode = "32",
            BankCode = "4",
            InterfaceCode = "H",
            StackCode = "1",
            RevisionCode = "M",
            DieTypeCode = "P",
            PackageTypeCode = "A",
            DieBrandCode = "G",
            AssemblyCode = "0",
            TesterCode = "W",
            SpeedCode = "CJ",
            PcbRevisionCode = "1",
            VendorCode = "G",
            PurchaserCode = "H"
        });

        var bytes = new RamosPartGenerator.Excel.RegistrationExcelExporter().Export(rows);
        using var archive = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var sheetXml = reader.ReadToEnd();

        // Product(RP)는 MDL 파트 — 영업코드 열이 포함되어야 한다 (값은 수기 기입용 빈칸).
        Assert.Contains("영업코드", sheetXml);
    }

    private static LpddrService CreateService()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        return new LpddrService(provider);
    }
}
