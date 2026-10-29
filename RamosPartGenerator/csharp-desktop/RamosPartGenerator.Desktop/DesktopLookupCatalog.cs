using RamosPartGenerator.Core.Services;
using RamosPartGenerator.Core.Specs;

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
            new("dramTypeCode", "DRAM Type", "common", true, true, Options("dram_type", "dram_type_comp_extra")),
            new("densityCode", "Density", "common", true, true, Options("density_ddr4", "density_ddr5")),
            new("bitOrganizationCode", "Bit Org", "common", true, true, Options("bit")),
            new("bankCode", "Bank", "common", true, true, Options("bank_ddr4", "bank_ddr5")),
            new("interfaceCode", "Interface", "common", true, true, Options("interface_ddr4", "interface_ddr5")),
            new("revisionCode", "Part Revision", "common", true, true, AlphabetOptions()),
            new("compTypeCode", "Comp Type", "comp", true, true, Options("comp_type", "manufacturing_comp_type")),
            new("packageTypeCode", "Package", "comp", true, true, Options("package_type")),
            new("dieBrandCode", "Die Brand", "comp", true, true, Options("die_brand")),
            new("testerCode", "Test Site", "comp", true, true, Options("tester")),
            new("vendorCode", tail.VendorFieldLabel, "comp", true, true, Options("vendor", "vendor_tm")),
            new("purchaserCode", tail.PurchaserFieldLabel ?? "Purchaser", "comp", tail.PurchaserFieldPresent, true, Options("purchaser", "purchaser_tm")),
            new("compType2Code", "Comp Type 2", "comp", true, true, Options("comp_type2"))
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
            new("compositionCode", "Composition", "base", true, true, ModuleCompositionOptions()),
            new("dieDensityCode", "Base Die Density", "base", true, true, Options("module_die_density")),
            new("rankCode", "Rank", "base", true, true, rankItems),
            new("generationCode", "Generation", "base", true, true, AlphabetOptions()),
            new("icBrandCode", "I.C Brand", "structure", true, true, Options("module_ic_brand")),
            new("moduleCompTypeCode", "Comp Type", "structure", true, true, Options("comp_type", "manufacturing_comp_type")),
            new("compTestCode", "Comp Test Site", "structure", true, true, Options("tester")),
            new("moduleSmtCode", "SMT Site", "structure", true, true, new[] { "0 - No Ass'y" }.Concat(Options("tester")).ToArray()),
            new("moduleTestCode", "Module Test Site", "structure", true, true, new[] { "0 - No Ass'y" }.Concat(Options("tester")).ToArray()),
            new("speedCode", "Speed", "structure", true, true, Options("speed_ddr4", "speed_ddr5")),
            new("pcbCode", "PCB", "structure", true, true, Options("pcb")),
            new("vendorCode", spec.Module.VendorFieldLabel, "output", true, true, Options("vendor", "vendor_tm")),
            new("purchaserCode", spec.Module.PurchaserFieldLabel ?? "Purchaser", "output", spec.Module.PurchaserFieldPresent, true, Options("purchaser", "purchaser_tm")),
            new("a100SpecialCode", "Special Code 1", "output", true, true, Options("special_code1_table1", "special_code1_table2")),
            new("specialCode2Code", "Special Code 2", "output", true, true, Options("module_special_code2")),
            new("specialCode3Code", "Special Code 3", "output", true, true, Options("module_special_code3")),
            new("gradeCode", "Grade Code", "output", true, true, Options("grade_code")),
            new("productBinCode", "Product Bin", "output", true, true, Options("product_bin"))
        };

        return new DesktopLookupPage(spec.Revision, spec.DisplayRevision, fields);
    }

    public DesktopLookupPage BuildLpddr(string revision)
    {
        var spec = _specProvider.GetRevisionSpec(revision);
        var fields = new List<DesktopLookupField>
        {
            new("sourceCode", "Source", "spec", true, true, Options("lpddr_incoming_source")),
            new("dramTypeCode", "LPDDR Type", "spec", true, true, Options("lpddr_dram_type")),
            new("densityCode", "Density", "spec", true, true, Options("lpddr_density")),
            new("bitCode", "Bit Org", "spec", true, true, Options("lpddr_bit")),
            new("dieDensityCode", "Die Density (K4)", "spec", true, true, Options("lpddr_die_density")),
            new("dieBitCode", "Die Bit (K4)", "spec", true, true, Options("lpddr_bit")),
            new("bankCode", "Bank", "spec", true, true, Options("lpddr_bank")),
            new("interfaceCode", "Interface", "spec", true, true, Options("lpddr_interface")),
            new("stackCode", "Stack", "spec", true, true, Options("lpddr_stack")),
            new("revisionCode", "Part Revision", "spec", true, true, AlphabetOptions()),
            new("dieTypeCode", "Die Type", "comp", true, true, Options("lpddr_die_type")),
            new("packageTypeCode", "Package", "comp", true, true, Options("lpddr_package_type")),
            new("dieBrandCode", "Die Brand", "comp", true, true, Options("lpddr_die_brand")),
            new("assemblyCode", "조립 업체", "comp", true, true, Options("lpddr_assembly")),
            new("testerCode", "Test Site", "comp", true, true, Options("lpddr_tester")),
            new("speedCode", "Speed", "comp", true, true, Options("lpddr_speed")),
            new("pcbRevisionCode", "PCB Revision", "comp", true, true, new[] { "1" }),
            new("vendorCode", "Vendor", "comp", true, true, Options("lpddr_vendor")),
            new("purchaserCode", "Purchaser", "comp", true, true, Options("lpddr_purchaser"))
        };

        return new DesktopLookupPage(spec.Revision, spec.DisplayRevision, fields);
    }

    public IReadOnlyList<string> SpecialCode1Options(bool isTable1)
    {
        return Options(isTable1 ? "special_code1_table1" : "special_code1_table2");
    }

    public IReadOnlyList<string> ModuleSpeedCodes(string dramTypeCode)
    {
        return TryGetModuleSpeedRule(dramTypeCode, out var rule)
            ? rule.AllowedSpeeds.ToArray()
            : Array.Empty<string>();
    }

    public IReadOnlyList<string> ModuleBankVddCodes(string dramTypeCode)
    {
        return TryGetModuleSpeedRule(dramTypeCode, out var rule)
            ? rule.BankVddBySpeed.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : Array.Empty<string>();
    }

    public string ResolveModuleBankVddCode(string dramTypeCode, string speedCode)
    {
        return TryGetModuleSpeedRule(dramTypeCode, out var rule) &&
               rule.BankVddBySpeed.TryGetValue(speedCode, out var bankVddCode)
            ? bankVddCode
            : string.Empty;
    }

    private bool TryGetModuleSpeedRule(string dramTypeCode, out ModuleSpeedRule rule)
    {
        var ruleKey = dramTypeCode switch
        {
            "4" or "A" => "DDR4",
            "R" => "DDR5",
            _ => string.Empty
        };

        return _specProvider.SharedSpec.ModuleSpeedRules.TryGetValue(ruleKey, out rule!);
    }

    private IReadOnlyList<string> Options(params string[] keys)
    {
        return keys
            .SelectMany(key => _specProvider.SharedSpec.CodeOptions.TryGetValue(key, out var options)
                ? options
                : throw new KeyNotFoundException($"Lookup option set '{key}' was not found."))
            .Distinct() // vendor+vendor_tm, purchaser+purchaser_tm 합칠 때 겹치는 항목 제거
            .ToArray();
    }

    private IReadOnlyList<string> ModuleCompositionOptions()
    {
        // 48은 일반(DRAM PART Rev.32.1) A, TM(TM PART Rev 2.1) 9 두 코드로 노출 — MainForm이 모드별로 거른다.
        return Options("bit")
            .SelectMany(option => DisplayHelpers.ExtractCode(option) switch
            {
                "04" => new[] { "4 - x4" },
                "08" => new[] { "8 - x8" },
                "16" => new[] { "6 - x16" },
                "48" => new[] { "A - x8 (x4 -> x8)", "9 - x8 (x4 -> x8)" },
                _ => new[] { option }
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
