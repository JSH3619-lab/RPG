using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Tests;

public class UnitTest1
{
    [Fact]
    public void GeneratePreview_Rev27_BuildsIncomingAndCompCodes()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "27",
            SourceCode = "K",
            DramTypeCode = "A",
            DensityCode = "8G",
            BitOrganizationCode = "08",
            BankCode = "5",
            InterfaceCode = "W",
            RevisionCode = "E",
            CompTypeCode = "P",
            DieBrandCode = "G",
            VendorCode = "V",
            CompType2Code = "B",
            PackageTypeCode = "B",
            TesterCode = "S"
        });

        Assert.Equal("K4A8G085WE-PGELVB", rows[0].PartCode);
        Assert.Equal("RCA8G085WE-PBGSVB", rows[1].PartCode);
        Assert.Equal("RCA8G085WE-PBGSVB-CA", rows[2].PartCode);
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

    [Fact]
    public void GeneratePreview_Rev27_InternalSource_AllowsBlankVendor()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var service = new IncomingCompService(provider, new ProductTextService(provider));

        var rows = service.GeneratePreview(new IncomingCompRequest
        {
            Revision = "27",
            SourceCode = "K",
            DramTypeCode = "A",
            DensityCode = "8G",
            BitOrganizationCode = "08",
            BankCode = "5",
            InterfaceCode = "W",
            RevisionCode = "E",
            CompTypeCode = "P",
            DieBrandCode = "G",
            VendorCode = "0",
            CompType2Code = "B",
            PackageTypeCode = "B",
            TesterCode = "S"
        });

        Assert.Equal("K4A8G085WE-PGELB", rows[0].PartCode);
        Assert.Equal("RCA8G085WE-PBGSB", rows[1].PartCode);
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
}
