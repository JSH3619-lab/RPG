using RamosPartGenerator.Api.Contracts;
using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Api.Services;

public sealed class LookupCatalog
{
    private readonly SpecProvider _specProvider;

    private static readonly string[] IncomingSourceItems = { "K - RAmos Memory", "T - Ramos TP", "C - CTST Memory", "B - CTST TP" };
    private static readonly string[] DramTypeItems = { "A - DDR4", "R - DDR5" };
    private static readonly string[] DensityDdr4Items = { "4G - 4Gb", "8G - 8Gb", "AG - 16Gb" };
    private static readonly string[] DensityDdr5Items = { "AH - 16Gb", "HE - 24Gb", "BH - 32Gb" };
    private static readonly string[] BitItems = { "04 - x4", "08 - x8", "16 - x16" };
    private static readonly string[] BankDdr4Items = { "5 - 16Bank" };
    private static readonly string[] BankDdr5Items = { "6 - 32Bank" };
    private static readonly string[] InterfaceDdr4Items = { "W - POD 1.2V" };
    private static readonly string[] InterfaceDdr5Items = { "V - POD 1.1V" };
    private static readonly string[] PartRevisionItems = Enumerable.Range('A', 26).Select(x => ((char)x).ToString()).ToArray();
    private static readonly string[] CompTypeItems = { "P - Partial", "U - Pre-Mark Partial", "N - EMC Partial", "H - a chip", "M - Erase Marking", "C - X-Comp", "D - Tested", "G - MDL(GOX)", "T - MDL Reballed(GKKR)", "F - Pre-Mark MDL(FX)", "E - EMC MDL(FX)", "Q - Pre-Mark Reballed(FKKR)", "W - EMC Reballed(FKKR)", "J - G Comp", "A - EMC G Comp", "X - EMC Partial X", "Y - EMC Partial Y", "Z - EMC Partial Z" };
    private static readonly string[] DieBrandItems = { "S - S1(SS)", "G - GIGA S1(SS)", "H - GIGA S2(Hynix)", "M - GIGA S3(Micron)", "C - GIGA S6(CXMT)", "N - GIGA S9(NANYA)" };
    private static readonly string[] Vendor30Items = { "S - S1(SS)", "G - GIGA", "B - BY20", "A - A100", "X - Ramaxel" };
    private static readonly string[] Purchaser30Items = { "(None)", "V - VM", "H - RMHK", "A - ADATA" };
    private static readonly string[] Vendor27Items = { "(None)", "V - VM", "H - RMHK" };
    private static readonly string[] CompType2Items = { "(None)", "B - Reball" };
    private static readonly string[] PackageTypeItems = { "B - FBGA(Flip Chip)", "M - FBGA(DDP)", "R - FBGA(FC-ReMark)", "N - FBGA(DDP-ReMark)" };
    private static readonly string[] TesterItems = { "R - Ramos", "S - No-Test", "A - ADATA", "W - Winpac", "T - DynaCard", "G - GoldKey", "K - CKMT", "Y - Yueyin", "D - OM", "L - SemiconTest", "1 - HTSI", "2 - DLI", "3 - Rayson", "4 - Ramsun", "5 - Powev" };

    private static readonly string[] DimmTypeCommonItems = { "D - UDIMM 288pin", "S - SODIMM 262pin" };
    private static readonly string[] DimmTypeCompItem = { "C - Comp" };
    private static readonly string[] ModuleDensityItems = { "4G - 4GB", "8G - 8GB", "AG - 16GB", "BG - 32GB" };
    private static readonly string[] RankItems = { "1 - 1Rank", "2 - 2Rank", "4 - 4Rank", "8 - 8Rank" };
    private static readonly string[] RankCompItem = { "0 - Comp" };
    private static readonly string[] GenerationItems = Enumerable.Range('A', 26).Select(x => ((char)x).ToString().ToUpperInvariant()).ToArray();
    private static readonly string[] ModuleIcBrandItems = { "S - S1(SS)", "G - GIGA S1(SS)", "H - GIGA S2(Hynix)", "M - GIGA S3(Micron)", "C - GIGA S6(CXMT)", "N - GIGA S9(NANYA)" };
    private static readonly string[] ModuleCompTypeItems = { "P - Partial", "N - EMC Partial", "G - MDL(GOX)", "T - MDL Reballed(GKKR)", "J - G Comp", "A - EMC G Comp" };
    private static readonly string[] PcbItems =
    {
        "1 - DN Green",
        "2 - HJ Green",
        "3 - DN Black",
        "4 - HJ Black",
        "5 - HJ Black 11x11",
        "6 - BP Green",
        "7 - BP Black",
        "8 - BP RGB Black",
        "9 - ADATA/BP Black",
        "A - AXA5UR02",
        "G - Hammer Pass",
        "K - Hammer Fail"
    };

    public LookupCatalog(SpecProvider specProvider)
    {
        _specProvider = specProvider;
    }

    public IReadOnlyList<RevisionMetaResponse> GetRevisions()
    {
        return _specProvider
            .GetSupportedRevisions()
            .Select(revision => _specProvider.GetRevisionSpec(revision))
            .Select(spec => new RevisionMetaResponse(spec.Revision, spec.DisplayRevision))
            .ToArray();
    }

    public LookupPageResponse BuildIncoming(string revision)
    {
        var spec = _specProvider.GetRevisionSpec(revision);
        var tail = spec.IncomingComp.TailModel;
        var fields = new List<LookupFieldResponse>
        {
            new("sourceCode", "Source", "common", true, true, IncomingSourceItems),
            new("dramTypeCode", "DRAM Type", "common", true, true, DramTypeItems),
            new("densityCode", "Density", "common", true, true, DensityDdr4Items.Concat(DensityDdr5Items).ToArray()),
            new("bitOrganizationCode", "Bit", "common", true, true, BitItems),
            new("bankCode", "Bank", "common", true, true, BankDdr4Items.Concat(BankDdr5Items).ToArray()),
            new("interfaceCode", "Interface", "common", true, true, InterfaceDdr4Items.Concat(InterfaceDdr5Items).ToArray()),
            new("revisionCode", "Part Revision", "common", true, true, PartRevisionItems),
            new("compTypeCode", "Comp Type", "comp", true, true, CompTypeItems),
            new("dieBrandCode", "Die Brand", "comp", true, true, DieBrandItems),
            new("vendorCode", tail.VendorFieldLabel, "comp", true, true, spec.Revision == "27" ? Vendor27Items : Vendor30Items),
            new("purchaserCode", tail.PurchaserFieldLabel ?? "Purchaser", "comp", tail.PurchaserFieldPresent, true, Purchaser30Items),
            new("compType2Code", "Comp Type 2", "comp", true, true, CompType2Items),
            new("packageTypeCode", "Package", "extra", true, true, PackageTypeItems),
            new("testerCode", "Tester", "extra", true, true, TesterItems)
        };

        return new LookupPageResponse(spec.Revision, spec.DisplayRevision, fields);
    }

    public LookupPageResponse BuildModule(string revision)
    {
        var spec = _specProvider.GetRevisionSpec(revision);
        var dimmTypeItems = spec.Module.DimmTypeAdditions.Count == 0
            ? DimmTypeCommonItems
            : DimmTypeCommonItems.Concat(spec.Module.DimmTypeAdditions.Select(code => $"{code} - Comp")).ToArray();
        var rankItems = spec.Module.RankAdditions.Count == 0
            ? RankItems
            : RankItems.Concat(spec.Module.RankAdditions.Select(code => $"{code} - Comp")).ToArray();

        var fields = new List<LookupFieldResponse>
        {
            new("compFullPart", "Comp Full Part", "quick", true, true, Array.Empty<string>()),
            new("moduleFullPart", "Module Full Part", "quick", true, true, Array.Empty<string>()),
            new("dramTypeCode", "DRAM Type", "base", true, true, DramTypeItems),
            new("dimmTypeCode", "DIMM Type", "base", true, true, dimmTypeItems),
            new("moduleDensityCode", "Module Density", "base", true, true, ModuleDensityItems),
            new("dieDensityCode", "Die Density", "base", true, true, DensityDdr4Items.Concat(DensityDdr5Items).ToArray()),
            new("compositionCode", "Composition", "base", true, true, BitItems),
            new("rankCode", "Rank", "structure", true, true, rankItems),
            new("generationCode", "Generation", "structure", true, true, GenerationItems),
            new("icBrandCode", spec.Module.SplitIcBrandAndCompType ? (spec.Module.IcBrand?.Label ?? "I.C Brand") : "I.C Brand + Comp Type", "structure", true, true, ModuleIcBrandItems),
            new("moduleCompTypeCode", spec.Module.SplitIcBrandAndCompType ? "Comp Type" : "Comp Type", "structure", spec.Module.SplitIcBrandAndCompType, true, ModuleCompTypeItems),
            new("speedCode", "Speed", "structure", true, true, Array.Empty<string>()),
            new("pcbCode", "PCB", "output", true, true, PcbItems),
            new("vendorCode", spec.Module.VendorFieldLabel, "output", true, true, spec.Revision == "27" ? Vendor27Items : Vendor30Items),
            new("purchaserCode", spec.Module.PurchaserFieldLabel ?? "Purchaser", "output", spec.Module.PurchaserFieldPresent, true, Purchaser30Items),
            new("basePartCode", "Base Part", "output", true, true, Array.Empty<string>()),
            new("binPartCode", "BIN Part", "output", true, true, Array.Empty<string>())
        };

        return new LookupPageResponse(spec.Revision, spec.DisplayRevision, fields);
    }
}
