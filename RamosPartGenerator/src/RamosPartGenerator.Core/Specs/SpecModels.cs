using System.Text.Json.Serialization;

namespace RamosPartGenerator.Core.Specs;

public sealed class SharedSpec
{
    [JsonPropertyName("supported_revisions")]
    public List<string> SupportedRevisions { get; set; } = new();

    [JsonPropertyName("excluded_families")]
    public List<string> ExcludedFamilies { get; set; } = new();

    [JsonPropertyName("families")]
    public Dictionary<string, List<string>> Families { get; set; } = new();

    [JsonPropertyName("comp_bin_speed_map")]
    public Dictionary<string, string> CompBinSpeedMap { get; set; } = new();

    [JsonPropertyName("module_speed_rules")]
    public Dictionary<string, ModuleSpeedRule> ModuleSpeedRules { get; set; } = new();

    [JsonPropertyName("item_text_rules")]
    public ItemTextRules ItemTextRules { get; set; } = new();

    [JsonPropertyName("a100_rule")]
    public A100Rule A100Rule { get; set; } = new();

    [JsonPropertyName("code_options")]
    public Dictionary<string, List<string>> CodeOptions { get; set; } = new();
}

public sealed class ModuleSpeedRule
{
    [JsonPropertyName("allowed_speeds")]
    public List<string> AllowedSpeeds { get; set; } = new();

    [JsonPropertyName("bank_vdd_by_speed")]
    public Dictionary<string, string> BankVddBySpeed { get; set; } = new();
}

public sealed class ItemTextRules
{
    [JsonPropertyName("name_equals_code")]
    public bool NameEqualsCode { get; set; }

    [JsonPropertyName("coo_default")]
    public string CooDefault { get; set; } = "KR";

    [JsonPropertyName("general_info")]
    public GeneralInfoRules GeneralInfo { get; set; } = new();

    [JsonPropertyName("specification")]
    public SpecificationRules Specification { get; set; } = new();
}

public sealed class GeneralInfoRules
{
    [JsonPropertyName("dram_and_comp_blank")]
    public bool DramAndCompBlank { get; set; }

    [JsonPropertyName("module_template")]
    public string ModuleTemplate { get; set; } = string.Empty;
}

public sealed class SpecificationRules
{
    [JsonPropertyName("comp_include_comp_type")]
    public bool CompIncludeCompType { get; set; }

    [JsonPropertyName("module_include_comp_type")]
    public bool ModuleIncludeCompType { get; set; }
}

public sealed class A100Rule
{
    [JsonPropertyName("requires_third_party")]
    public bool RequiresThirdParty { get; set; }

    [JsonPropertyName("vendor_code")]
    public string VendorCode { get; set; } = string.Empty;

    [JsonPropertyName("purchaser_code")]
    public string PurchaserCode { get; set; } = string.Empty;
}

public sealed class RevisionSpec
{
    [JsonPropertyName("revision")]
    public string Revision { get; set; } = string.Empty;

    [JsonPropertyName("source_pdf")]
    public string SourcePdf { get; set; } = string.Empty;

    [JsonPropertyName("display_revision")]
    public string DisplayRevision { get; set; } = string.Empty;

    [JsonPropertyName("incoming_comp")]
    public IncomingCompRevisionSpec IncomingComp { get; set; } = new();

    [JsonPropertyName("module")]
    public ModuleRevisionSpec Module { get; set; } = new();

    [JsonPropertyName("ui")]
    public UiRevisionSpec Ui { get; set; } = new();
}

public sealed class IncomingCompRevisionSpec
{
    [JsonPropertyName("tail_model")]
    public IncomingCompTailModel TailModel { get; set; } = new();
}

public sealed class IncomingCompTailModel
{
    [JsonPropertyName("vendor_field_label")]
    public string VendorFieldLabel { get; set; } = string.Empty;

    [JsonPropertyName("vendor_field_position")]
    public int VendorFieldPosition { get; set; }

    [JsonPropertyName("vendor_codes")]
    public List<string> VendorCodes { get; set; } = new();

    [JsonPropertyName("purchaser_field_present")]
    public bool PurchaserFieldPresent { get; set; }

    [JsonPropertyName("purchaser_field_label")]
    public string? PurchaserFieldLabel { get; set; }

    [JsonPropertyName("purchaser_field_position")]
    public int? PurchaserFieldPosition { get; set; }

    [JsonPropertyName("comp_type2_position")]
    public int CompType2Position { get; set; }
}

public sealed class ModuleRevisionSpec
{
    [JsonPropertyName("split_ic_brand_and_comp_type")]
    public bool SplitIcBrandAndCompType { get; set; }

    [JsonPropertyName("combined_comp_field")]
    public CombinedCompField? CombinedCompField { get; set; }

    [JsonPropertyName("ic_brand")]
    public PositionedField? IcBrand { get; set; }

    [JsonPropertyName("comp_type")]
    public PositionedField? CompType { get; set; }

    [JsonPropertyName("dimm_type_additions")]
    public List<string> DimmTypeAdditions { get; set; } = new();

    [JsonPropertyName("rank_additions")]
    public List<string> RankAdditions { get; set; } = new();

    [JsonPropertyName("vendor_field_label")]
    public string VendorFieldLabel { get; set; } = string.Empty;

    [JsonPropertyName("purchaser_field_present")]
    public bool PurchaserFieldPresent { get; set; }

    [JsonPropertyName("purchaser_field_label")]
    public string? PurchaserFieldLabel { get; set; }

    [JsonPropertyName("special_fields")]
    public List<string> SpecialFields { get; set; } = new();
}

public sealed class CombinedCompField
{
    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("meaning")]
    public string Meaning { get; set; } = string.Empty;
}

public sealed class PositionedField
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public int Position { get; set; }
}

public sealed class UiRevisionSpec
{
    [JsonPropertyName("spec_rev_options")]
    public List<string> SpecRevOptions { get; set; } = new();
}
