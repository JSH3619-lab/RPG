using RamosPartGenerator.Api.Contracts;
using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Api.Services;

public sealed class LookupCatalog
{
    private readonly SpecProvider _specProvider;

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
            new("sourceCode", "Source", "common", true, true, Options("incoming_source")),
            new("dramTypeCode", "DRAM Type", "common", true, true, Options("dram_type")),
            new("densityCode", "Density", "common", true, true, Options("density_ddr4", "density_ddr5")),
            new("bitOrganizationCode", "Bit", "common", true, true, Options("bit")),
            new("bankCode", "Bank", "common", true, true, Options("bank_ddr4", "bank_ddr5")),
            new("interfaceCode", "Interface", "common", true, true, Options("interface_ddr4", "interface_ddr5")),
            new("revisionCode", "Part Revision", "common", true, true, AlphabetOptions()),
            new("compTypeCode", "Comp Type", "comp", true, true, Options("comp_type")),
            new("dieBrandCode", "Die Brand", "comp", true, true, Options("die_brand")),
            new("vendorCode", tail.VendorFieldLabel, "comp", true, true, Options("vendor")),
            new("purchaserCode", tail.PurchaserFieldLabel ?? "Purchaser", "comp", tail.PurchaserFieldPresent, true, Options("purchaser")),
            new("compType2Code", "Comp Type 2", "comp", true, true, Options("comp_type2")),
            new("packageTypeCode", "Package", "extra", true, true, Options("package_type")),
            new("testerCode", "Tester", "extra", true, true, Options("tester"))
        };

        return new LookupPageResponse(spec.Revision, spec.DisplayRevision, fields);
    }

    public LookupPageResponse BuildModule(string revision)
    {
        var spec = _specProvider.GetRevisionSpec(revision);
        var dimmTypeItems = spec.Module.DimmTypeAdditions.Count == 0
            ? Options("dimm_type_common")
            : Options("dimm_type_common").Concat(spec.Module.DimmTypeAdditions.Select(code => $"{code} - Comp")).ToArray();
        var rankItems = spec.Module.RankAdditions.Count == 0
            ? Options("rank")
            : Options("rank").Concat(spec.Module.RankAdditions.Select(code => $"{code} - Comp")).ToArray();

        var fields = new List<LookupFieldResponse>
        {
            new("compFullPart", "Comp Full Part", "quick", true, true, Array.Empty<string>()),
            new("moduleFullPart", "Module Full Part", "quick", true, true, Array.Empty<string>()),
            new("moduleSourceCode", "Source", "base", true, true, Options("module_source")),
            new("dramTypeCode", "DRAM Type", "base", true, true, Options("dram_type")),
            new("dimmTypeCode", "DIMM Type", "base", true, true, dimmTypeItems),
            new("moduleDensityCode", "Module Density", "base", true, true, Options("module_density")),
            new("bankVddCode", "Bank / VDD", "base", true, true, Options("module_bank_vdd")),
            new("dieDensityCode", "Die Density", "base", true, true, Options("module_die_density")),
            new("compositionCode", "Composition", "base", true, true, Options("bit")),
            new("rankCode", "Rank", "base", true, true, rankItems),
            new("generationCode", "Generation", "base", true, true, AlphabetOptions()),
            new("icBrandCode", spec.Module.IcBrand?.Label ?? "I.C Brand", "structure", true, true, Options("module_ic_brand")),
            new("moduleCompTypeCode", spec.Module.CompType?.Label ?? "Comp Type", "structure", true, true, Options("comp_type")),
            new("compTestCode", "Comp Test Site", "structure", true, true, Options("tester")),
            new("speedCode", "Speed", "structure", true, true, Options("speed_ddr4", "speed_ddr5")),
            new("moduleSmtCode", "SMT Site", "structure", true, true, new[] { "0 - No Ass'y" }.Concat(Options("tester")).ToArray()),
            new("moduleTestCode", "Module Test Site", "structure", true, true, new[] { "0 - No Ass'y" }.Concat(Options("tester")).ToArray()),
            new("pcbCode", "PCB", "structure", true, true, Options("pcb")),
            new("vendorCode", spec.Module.VendorFieldLabel, "output", true, true, Options("vendor")),
            new("purchaserCode", spec.Module.PurchaserFieldLabel ?? "Purchaser", "output", spec.Module.PurchaserFieldPresent, true, Options("purchaser")),
            new("a100SpecialCode", "A100 Special", "output", true, true, Options("a100_special")),
            new("specialCode2Code", "Special Code 2", "output", true, true, Options("module_special_code2")),
            new("specialCode3Code", "Special Code 3", "output", true, true, Options("module_special_code3")),
            new("gradeCode", "Grade Code", "output", true, true, Options("grade_code")),
            new("productBinCode", "Product Bin", "output", true, true, Options("product_bin"))
        };

        return new LookupPageResponse(spec.Revision, spec.DisplayRevision, fields);
    }

    private IReadOnlyList<string> Options(params string[] keys)
    {
        return keys
            .SelectMany(key => _specProvider.SharedSpec.CodeOptions.TryGetValue(key, out var options)
                ? options
                : throw new KeyNotFoundException($"Lookup option set '{key}' was not found."))
            .ToArray();
    }

    private static IReadOnlyList<string> AlphabetOptions()
    {
        return Enumerable.Range('A', 26).Select(x => ((char)x).ToString()).ToArray();
    }
}
