using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Desktop;

internal sealed record DesktopLookupField(
    string Key,
    string Label,
    string Section,
    bool Visible,
    bool AllowFreeText,
    IReadOnlyList<string> Options);

internal sealed record DesktopLookupPage(
    string Revision,
    string DisplayRevision,
    IReadOnlyList<DesktopLookupField> Fields);

internal sealed class DesktopLookupCatalog
{
    private readonly SpecProvider _specProvider;

    public DesktopLookupCatalog(SpecProvider specProvider)
    {
        _specProvider = specProvider;
    }

    public DesktopLookupPage BuildIncoming(string revision)
    {
        var spec = _specProvider.GetRevisionSpec(revision);
        var tail = spec.IncomingComp.TailModel;
        var fields = new List<DesktopLookupField>
        {
            new("sourceCode", "Comp Source", "common", true, true, CompSourceOptions()),
            new("dramTypeCode", "DRAM Type", "common", true, true, Options("dram_type")),
            new("densityCode", "Density", "common", true, true, Options("density_ddr4", "density_ddr5")),
            new("bitOrganizationCode", "Bit", "common", true, true, Options("bit", "bit_tm")),
            new("bankCode", "Bank", "common", true, true, Options("bank_ddr4", "bank_ddr5")),
            new("interfaceCode", "Interface", "common", true, true, Options("interface_ddr4", "interface_ddr5")),
            new("revisionCode", "Part Revision", "common", true, true, AlphabetOptions()),
            new("compTypeCode", "Comp Type", "comp", true, true, Options("comp_type", "manufacturing_comp_type")),
            new("dieBrandCode", "Die Brand", "comp", true, true, Options("die_brand")),
            new("vendorCode", tail.VendorFieldLabel, "comp", true, true, Options("vendor", "vendor_tm")),
            new("purchaserCode", tail.PurchaserFieldLabel ?? "Purchaser", "comp", tail.PurchaserFieldPresent, true, Options("purchaser")),
            new("compType2Code", "Comp Type 2", "comp", true, true, Options("comp_type2")),
            new("packageTypeCode", "Package", "extra", true, true, Options("package_type")),
            new("testerCode", "Tester", "extra", true, true, Options("tester"))
        };

        return new DesktopLookupPage(spec.Revision, spec.DisplayRevision, fields);
    }

    public DesktopLookupPage BuildModule(string revision)
    {
        var spec = _specProvider.GetRevisionSpec(revision);
        var dimmTypeItems = spec.Module.DimmTypeAdditions.Count == 0
            ? Options("dimm_type_common")
            : Options("dimm_type_common").Concat(spec.Module.DimmTypeAdditions.Select(code => $"{code} - Comp")).ToArray();
        var rankItems = spec.Module.RankAdditions.Count == 0
            ? Options("rank")
            : Options("rank").Concat(spec.Module.RankAdditions.Select(code => $"{code} - Comp")).ToArray();

        var fields = new List<DesktopLookupField>
        {
            new("moduleSourceCode", "Source", "base", true, true, Options("module_source", "manufacturing_module_source")),
            new("dramTypeCode", "DRAM Type", "base", true, true, Options("dram_type")),
            new("dimmTypeCode", "DIMM Type", "base", true, true, dimmTypeItems),
            new("moduleDensityCode", "Module Density", "base", true, true, Options("module_density")),
            new("bankVddCode", "Bank / VDD", "base", true, true, Options("module_bank_vdd")),
            new("dieDensityCode", "Die Density", "base", true, true, Options("module_die_density")),
            new("compositionCode", "Composition", "base", true, true, ModuleCompositionOptions()),
            new("rankCode", "Rank", "base", true, true, rankItems),
            new("generationCode", "Generation", "base", true, true, AlphabetOptions()),
            new("icBrandCode", "I.C Brand", "structure", true, true, Options("module_ic_brand")),
            new("moduleCompTypeCode", "Comp Type", "structure", true, true, Options("comp_type", "manufacturing_comp_type")),
            new("compTestCode", "Comp Test Site", "structure", true, true, Options("tester")),
            new("speedCode", "Speed", "structure", true, true, Options("speed_ddr4", "speed_ddr5")),
            new("moduleSmtCode", "SMT Site", "structure", true, true, new[] { "0 - No Ass'y" }.Concat(Options("tester")).ToArray()),
            new("moduleTestCode", "Module Test Site", "structure", true, true, new[] { "0 - No Ass'y" }.Concat(Options("tester")).ToArray()),
            new("pcbCode", "PCB", "structure", true, true, Options("pcb")),
            new("vendorCode", spec.Module.VendorFieldLabel, "output", true, true, Options("vendor", "vendor_tm")),
            new("purchaserCode", spec.Module.PurchaserFieldLabel ?? "Purchaser", "output", spec.Module.PurchaserFieldPresent, true, Options("purchaser")),
            new("a100SpecialCode", "A100 Special", "output", true, true, Options("a100_special")),
            new("specialCode2Code", "Special Code 2", "output", true, true, Options("module_special_code2")),
            new("specialCode3Code", "Special Code 3", "output", true, true, Options("module_special_code3")),
            new("gradeCode", "Grade Code", "output", true, true, Options("grade_code")),
            new("productBinCode", "Product Bin", "output", true, true, Options("product_bin"))
        };

        return new DesktopLookupPage(spec.Revision, spec.DisplayRevision, fields);
    }

    private IReadOnlyList<string> Options(params string[] keys)
    {
        return keys
            .SelectMany(key => _specProvider.SharedSpec.CodeOptions.TryGetValue(key, out var options)
                ? options
                : throw new KeyNotFoundException($"Lookup option set '{key}' was not found."))
            .ToArray();
    }

    private IReadOnlyList<string> ModuleCompositionOptions()
    {
        return Options("bit", "bit_tm")
            .Select(option => DisplayHelpers.ExtractCode(option) switch
            {
                "04" => "4 - x4",
                "08" => "8 - x8",
                "16" => "6 - x16",
                "48" => "9 - x8 (x4 -> x8)",
                _ => option
            })
            .ToArray();
    }

    private IReadOnlyList<string> CompSourceOptions()
    {
        var standardOptions = Options("incoming_source")
            .Select(option =>
            {
                var incomingCode = DisplayHelpers.ExtractCode(option);
                var description = option[(option.IndexOf(" - ", StringComparison.Ordinal) + 3)..];
                return $"{DisplayHelpers.ToCompSourceCode(incomingCode)} - {description} (입고: {incomingCode})";
            });
        var manufacturingOptions = Options("manufacturing_comp_source")
            .Select(option => $"{option} (입고: {DisplayHelpers.ToIncomingSourceCode(DisplayHelpers.ExtractCode(option))})");

        return standardOptions.Concat(manufacturingOptions).ToArray();
    }

    private static IReadOnlyList<string> AlphabetOptions()
    {
        return Enumerable.Range('A', 26).Select(value => ((char)value).ToString()).ToArray();
    }
}
