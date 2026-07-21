using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Tests;

public sealed class BatchGenerationServiceTests
{
    private const string BaseModulePart = "RMRDAG58A1A-GPWRRWM7G";

    [Fact]
    public void GenerateFromModuleParts_BaseAndFirstRepair_ProducesRequestedRows()
    {
        var service = CreateService();

        var result = service.GenerateFromModuleParts(
            new[] { BaseModulePart },
            new MdlBatchOptions
            {
                IncludeBasePid = true,
                IncludeBaseMfgId = true,
                IncludeFirstRepair = true
            });

        Assert.Equal(BatchItemStatus.Success, result.Items[0].Status);
        Assert.Equal(new[]
        {
            "RMRDAG58A1A-GPWRRWM7G",
            "RMRDAG58A1A-GPWRRWM7G-TNAGA00",
            "RMRDAG58A1A-GPWRRWM7G00",
            "RMRDAG58A1A-GPWRRWM7GR",
            "RMRDAG58A1A-GPWRRWM7GR-TNAGA00"
        }, result.Rows.Select(row => row.PartCode));
    }

    [Fact]
    public void GenerateFromModuleParts_FirstRepairAndRetest_SharesOneDummy00()
    {
        var service = CreateService();

        var result = service.GenerateFromModuleParts(
            new[] { BaseModulePart },
            new MdlBatchOptions
            {
                IncludeFirstRepair = true,
                IncludeFinishedProductRetest = true
            });

        var sharedDummyRows = result.Rows
            .Where(row => row.PartCode == "RMRDAG58A1A-GPWRRWM7G00")
            .ToArray();
        Assert.Single(sharedDummyRows);
        Assert.EndsWith("Dummy", sharedDummyRows[0].Specification);
        Assert.DoesNotContain("1st Repair Dummy", sharedDummyRows[0].Specification);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Equal(5, result.Rows.Count);
    }

    [Fact]
    public void GenerateFromModuleParts_OriginalCompRelated_ProducesIncomingCompAndAllBins()
    {
        var service = CreateService();

        var result = service.GenerateFromModuleParts(
            new[] { BaseModulePart },
            new MdlBatchOptions { IncludeOriginalCompRelated = true });

        Assert.Equal(8, result.Rows.Count);
        Assert.Equal("K4RAH086VA-PGGEL", result.Rows[0].PartCode);
        Assert.Equal("RCRAH086VA-PBGWG", result.Rows[1].PartCode);
        Assert.Equal(
            new[] { "CA", "CB", "CC", "CD", "CE", "CF" },
            result.Rows.Skip(2).Select(row => row.PartCode[^2..]));
        Assert.DoesNotContain(result.Rows, row => row.PartCode.Contains("PBGWGB"));
    }

    [Fact]
    public void GenerateFromModuleParts_ReballCompRelated_UsesCompType2B()
    {
        var service = CreateService();

        var result = service.GenerateFromModuleParts(
            new[] { BaseModulePart },
            new MdlBatchOptions { IncludeReballCompRelated = true });

        Assert.Equal(8, result.Rows.Count);
        Assert.Equal("K4RAH086VA-PGGELB", result.Rows[0].PartCode);
        Assert.Equal("RCRAH086VA-PBGWGB", result.Rows[1].PartCode);
        Assert.All(result.Rows.Skip(2), row => Assert.Contains("PBGWGB-", row.PartCode));
    }

    [Fact]
    public void GenerateFromModuleParts_FirstRepairOnly_DoesNotGenerateCompRows()
    {
        var service = CreateService();

        var result = service.GenerateFromModuleParts(
            new[] { BaseModulePart },
            new MdlBatchOptions { IncludeFirstRepair = true });

        Assert.Equal(3, result.Rows.Count);
        Assert.All(result.Rows, row => Assert.StartsWith("Module", row.Kind));
    }

    [Fact]
    public void GenerateFromModuleParts_BaseFirstRepairAndOriginalComp_ProducesThirteenRows()
    {
        var service = CreateService();

        var result = service.GenerateFromModuleParts(
            new[] { BaseModulePart },
            new MdlBatchOptions
            {
                IncludeBasePid = true,
                IncludeBaseMfgId = true,
                IncludeFirstRepair = true,
                IncludeOriginalCompRelated = true
            });

        Assert.Equal(13, result.Rows.Count);
        Assert.Equal(5, result.Rows.Count(row => row.Kind.StartsWith("Module")));
        Assert.Equal(1, result.Rows.Count(row => row.Kind == "Incoming"));
        Assert.Equal(1, result.Rows.Count(row => row.Kind == "Comp"));
        Assert.Equal(6, result.Rows.Count(row => row.Kind == "Comp BIN"));
    }

    [Theory]
    [InlineData("RMRDAG58A1A-GPWRRWM7GB", ModuleBatchInputKind.Reball)]
    [InlineData("RMRDAG58A1A-GPWRRWM7GR", ModuleBatchInputKind.FirstRepair)]
    [InlineData("RMRDAG58A1A-GPWRRWM7GS", ModuleBatchInputKind.SecondRepair)]
    [InlineData("RMRDAG58A1A-GPWRRWM7GC", ModuleBatchInputKind.ReballRepair)]
    [InlineData("RMRDAG58A1A-GPWRRWM7G00", ModuleBatchInputKind.SharedDummy)]
    [InlineData("RMRDAG58A1A-GPWRRWM7GR0", ModuleBatchInputKind.SecondRepairDummy)]
    [InlineData("RMRDAG58A1A-GPWRRWM7GB0", ModuleBatchInputKind.ReballRepairDummy)]
    [InlineData("RMRDAG58A1A-GPWRRWM7G0Y", ModuleBatchInputKind.FinishedProductRetest)]
    public void GenerateFromModuleParts_DetectsExistingWorkPart(
        string inputPartCode,
        ModuleBatchInputKind expectedInputKind)
    {
        var service = CreateService();

        var result = service.GenerateFromModuleParts(
            new[] { inputPartCode },
            new MdlBatchOptions { IncludeBasePid = true });

        Assert.Equal(expectedInputKind, result.Items[0].DetectedInputKind);
        Assert.Equal(BaseModulePart, result.Rows[0].PartCode);
    }

    [Fact]
    public void GenerateFromModuleParts_DuplicateAndInvalidInputs_DoNotStopBatch()
    {
        var service = CreateService();

        var result = service.GenerateFromModuleParts(
            new[] { BaseModulePart, BaseModulePart.ToLowerInvariant(), "INVALID", "" },
            new MdlBatchOptions { IncludeBasePid = true });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(BatchItemStatus.Success, result.Items[0].Status);
        Assert.Equal(BatchItemStatus.Failed, result.Items[1].Status);
        Assert.Single(result.Rows);
        Assert.Equal(1, result.DuplicateCount);
    }

    [Fact]
    public void GenerateFromCompParts_ProducesIncomingCompAndAllBins()
    {
        var service = CreateService();

        var result = service.GenerateFromCompParts(
            new[] { "RCRAH086VA-PBGWG" },
            new CompBatchOptions());

        Assert.Equal(BatchItemStatus.Success, result.Items[0].Status);
        Assert.Equal(8, result.Rows.Count);
        Assert.Equal("K4RAH086VA-PGGEL", result.Rows[0].PartCode);
        Assert.Equal("RCRAH086VA-PBGWG", result.Rows[1].PartCode);
        Assert.Equal(6, result.Rows.Count(row => row.Kind == "Comp BIN"));
    }

    [Fact]
    public void GenerateFromCompParts_WithCompMdl_UsesSelectedSpeed()
    {
        var service = CreateService();

        var result = service.GenerateFromCompParts(
            new[] { "RCRAH086VA-PBGWG" },
            new CompBatchOptions { IncludeCompMdl = true, SpeedCode = "WM" });

        Assert.Equal(BatchItemStatus.Success, result.Items[0].Status);
        Assert.Equal(10, result.Rows.Count);
        Assert.Equal("RMRC2G58A0A-GPW00WM0G", result.Rows[8].PartCode);
        Assert.Equal("RMRC2G58A0A-GPW00WM0G-TN2GA00", result.Rows[9].PartCode);
    }

    [Fact]
    public void GenerateFromCompParts_ReballComp_CreatesReballCompMdl()
    {
        var service = CreateService();

        var result = service.GenerateFromCompParts(
            new[] { "RCRAH086VA-PBGWGB" },
            new CompBatchOptions { IncludeCompMdl = true, SpeedCode = "WM" });

        Assert.Equal(ModuleBatchInputKind.Reball, result.Items[0].DetectedInputKind);
        Assert.Equal(BatchItemStatus.Success, result.Items[0].Status);
        Assert.Contains(result.Rows, row => row.PartCode == "RMRC2G58A0A-GPW00WM0GB");
        Assert.Contains(result.Rows, row => row.Kind == "Module BIN" && row.PartCode.Contains("WM0GB-"));
    }

    [Fact]
    public void GenerateFromCompParts_MissingCompMdlSpeed_KeepsCompRowsAndReportsError()
    {
        var service = CreateService();

        var result = service.GenerateFromCompParts(
            new[] { "RCRAH086VA-PBGWG" },
            new CompBatchOptions { IncludeCompMdl = true });

        Assert.Equal(BatchItemStatus.PartialSuccess, result.Items[0].Status);
        Assert.Equal(8, result.Rows.Count);
        Assert.Contains(result.Items[0].Messages, message => message.Contains("Speed"));
    }

    [Fact]
    public void GenerateFromCompParts_UnknownCompMdlDensity_DoesNotGuessValue()
    {
        var service = CreateService();

        var result = service.GenerateFromCompParts(
            new[] { "RCRHE086VA-PBGWG" },
            new CompBatchOptions { IncludeCompMdl = true, SpeedCode = "WM" });

        Assert.Equal(BatchItemStatus.PartialSuccess, result.Items[0].Status);
        Assert.Equal(8, result.Rows.Count);
        Assert.Contains(result.Items[0].Messages, message => message.Contains("Module Density"));
    }

    [Fact]
    public void GenerateFromCompParts_DuplicateAndInvalidInputs_DoNotStopBatch()
    {
        var service = CreateService();

        var result = service.GenerateFromCompParts(
            new[] { "RCRAH086VA-PBGWG", "rcrah086va-pbgwg", "INVALID" },
            new CompBatchOptions());

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(BatchItemStatus.Success, result.Items[0].Status);
        Assert.Equal(BatchItemStatus.Failed, result.Items[1].Status);
        Assert.Equal(8, result.Rows.Count);
        Assert.Equal(1, result.DuplicateCount);
    }

    private static BatchGenerationService CreateService()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);
        provider.Load();
        var textService = new ProductTextService(provider);
        return new BatchGenerationService(
            new ModuleService(provider, textService),
            new IncomingCompService(provider, textService));
    }
}
