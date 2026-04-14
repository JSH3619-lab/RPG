using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.App;

public partial class Form1 : Form
{
    private Label titleLabel = null!;
    private Label specPathTitleLabel = null!;
    private Label specPathValueLabel = null!;
    private TabControl mainTabControl = null!;
    private TabPage incomingTabPage = null!;
    private TabPage moduleTabPage = null!;
    private DataGridView resultsGridView = null!;

    private GroupBox incomingRevisionGroupBox = null!;
    private RadioButton incomingRevision27RadioButton = null!;
    private RadioButton incomingRevision30RadioButton = null!;
    private ComboBox incomingSourceComboBox = null!;
    private ComboBox incomingDramTypeComboBox = null!;
    private ComboBox incomingDensityComboBox = null!;
    private ComboBox incomingBitComboBox = null!;
    private ComboBox incomingBankComboBox = null!;
    private ComboBox incomingInterfaceComboBox = null!;
    private ComboBox incomingPartRevisionComboBox = null!;
    private ComboBox incomingCompTypeComboBox = null!;
    private ComboBox incomingDieBrandComboBox = null!;
    private ComboBox incomingVendorComboBox = null!;
    private ComboBox incomingPurchaserComboBox = null!;
    private ComboBox incomingCompType2ComboBox = null!;
    private ComboBox incomingPackageComboBox = null!;
    private ComboBox incomingTesterComboBox = null!;
    private Label incomingVendorLabel = null!;
    private Label incomingPurchaserLabel = null!;
    private Button previewIncomingButton = null!;

    private GroupBox moduleRevisionGroupBox = null!;
    private RadioButton moduleRevision27RadioButton = null!;
    private RadioButton moduleRevision30RadioButton = null!;
    private TextBox moduleDramTypeTextBox = null!;
    private TextBox moduleDimmTypeTextBox = null!;
    private TextBox moduleDensityTextBox = null!;
    private TextBox moduleDieDensityTextBox = null!;
    private TextBox moduleCompositionTextBox = null!;
    private TextBox moduleRankTextBox = null!;
    private TextBox moduleGenerationTextBox = null!;
    private TextBox moduleIcBrandTextBox = null!;
    private TextBox moduleCompTypeTextBox = null!;
    private TextBox moduleSpeedTextBox = null!;
    private TextBox modulePcbTextBox = null!;
    private TextBox moduleVendorTextBox = null!;
    private TextBox modulePurchaserTextBox = null!;
    private TextBox moduleBasePartTextBox = null!;
    private TextBox moduleBinPartTextBox = null!;
    private Button previewModuleButton = null!;

    private readonly SpecProvider _specProvider;
    private readonly IncomingCompService _incomingCompService;
    private readonly ModuleService _moduleService;

    private static readonly string[] IncomingSourceItems =
    {
        "K - RAmos Memory",
        "T - Ramos TP",
        "C - CTST Memory",
        "B - CTST TP"
    };

    private static readonly string[] DramTypeItems =
    {
        "A - DDR4",
        "R - DDR5"
    };

    private static readonly string[] DensityDdr4Items =
    {
        "4G - 4Gb",
        "8G - 8Gb",
        "AG - 16Gb"
    };

    private static readonly string[] DensityDdr5Items =
    {
        "AH - 16Gb",
        "HE - 24Gb",
        "BH - 32Gb"
    };

    private static readonly string[] BitItems =
    {
        "04 - x4",
        "08 - x8",
        "16 - x16"
    };

    private static readonly string[] BankDdr4Items = { "5 - 16Bank" };
    private static readonly string[] BankDdr5Items = { "6 - 32Bank" };
    private static readonly string[] InterfaceDdr4Items = { "W - POD 1.2V" };
    private static readonly string[] InterfaceDdr5Items = { "V - POD 1.1V" };
    private static readonly string[] CompTypeItems =
    {
        "P - Partial",
        "U - Pre-Mark Partial",
        "N - EMC Partial",
        "H - a chip",
        "M - Erase Marking",
        "C - X-Comp",
        "D - Tested",
        "G - MDL(GOX)",
        "T - MDL Reballed(GKKR)",
        "F - Pre-Mark MDL(FX)",
        "E - EMC MDL(FX)",
        "Q - Pre-Mark Reballed(FKKR)",
        "W - EMC Reballed(FKKR)",
        "J - G Comp",
        "A - EMC G Comp",
        "X - EMC Partial X",
        "Y - EMC Partial Y",
        "Z - EMC Partial Z"
    };

    private static readonly string[] DieBrandItems =
    {
        "S - S1(SS)",
        "G - GIGA S1(SS)",
        "H - GIGA S2(Hynix)",
        "M - GIGA S3(Micron)",
        "C - GIGA S6(CXMT)",
        "N - GIGA S9(NANYA)"
    };

    private static readonly string[] Vendor30Items =
    {
        "S - S1(SS)",
        "G - GIGA",
        "B - BY20",
        "A - A100",
        "X - Ramaxel"
    };

    private static readonly string[] Purchaser30Items =
    {
        "(없음)",
        "V - VM",
        "H - RMHK",
        "A - ADATA"
    };

    private static readonly string[] Vendor27Items =
    {
        "(없음)",
        "V - VM",
        "H - RMHK"
    };

    private static readonly string[] CompType2Items =
    {
        "(없음)",
        "B - Reball"
    };

    private static readonly string[] PackageTypeItems =
    {
        "B - FBGA(Flip Chip)",
        "M - FBGA(DDP)",
        "R - FBGA(FC-ReMark)",
        "N - FBGA(DDP-ReMark)"
    };

    private static readonly string[] TesterItems =
    {
        "R - Ramos",
        "S - No-Test",
        "A - ADATA",
        "W - Winpac",
        "T - DynaCard",
        "G - GoldKey",
        "K - CKMT",
        "Y - Yueyin",
        "D - OM",
        "L - SemiconTest",
        "1 - HTSI",
        "2 - DLI",
        "3 - Rayson",
        "4 - Ramsun",
        "5 - Powev"
    };

    public Form1()
    {
        BuildUi();

        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        _specProvider = new SpecProvider(specDirectory);
        _specProvider.Load();
        var productTextService = new ProductTextService(_specProvider);
        _incomingCompService = new IncomingCompService(_specProvider, productTextService);
        _moduleService = new ModuleService(_specProvider, productTextService);

        Form1_Load(this, EventArgs.Empty);
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        incomingRevision30RadioButton.Checked = true;
        moduleRevision30RadioButton.Checked = true;
        specPathValueLabel.Text = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        ConfigureIncomingLookups();
        UpdateIncomingRevisionUi();
    }

    private void ConfigureIncomingLookups()
    {
        SetComboItems(incomingSourceComboBox, IncomingSourceItems);
        SetComboItems(incomingDramTypeComboBox, DramTypeItems);
        SetComboItems(incomingBitComboBox, BitItems);
        SetComboItems(incomingCompTypeComboBox, CompTypeItems);
        SetComboItems(incomingDieBrandComboBox, DieBrandItems);
        SetComboItems(incomingCompType2ComboBox, CompType2Items);
        SetComboItems(incomingPackageComboBox, PackageTypeItems);
        SetComboItems(incomingTesterComboBox, TesterItems);
        SetComboItems(incomingPartRevisionComboBox, Enumerable.Range('A', 26).Select(x => ((char)x).ToString()).ToArray());

        incomingDramTypeComboBox.TextChanged += (_, _) => UpdateIncomingDramTypeUi();
        incomingDramTypeComboBox.SelectedIndexChanged += (_, _) => UpdateIncomingDramTypeUi();
        incomingSourceComboBox.TextChanged += (_, _) => UpdateIncomingRevisionUi();
        incomingSourceComboBox.SelectedIndexChanged += (_, _) => UpdateIncomingRevisionUi();
        incomingRevision27RadioButton.CheckedChanged += (_, _) => UpdateIncomingRevisionUi();
        incomingRevision30RadioButton.CheckedChanged += (_, _) => UpdateIncomingRevisionUi();
    }

    private void UpdateIncomingDramTypeUi()
    {
        var dramTypeCode = ExtractCode(incomingDramTypeComboBox.Text);
        if (dramTypeCode == "A")
        {
            SetComboItems(incomingDensityComboBox, DensityDdr4Items, preserveText: false);
            SetComboItems(incomingBankComboBox, BankDdr4Items, preserveText: false);
            SetComboItems(incomingInterfaceComboBox, InterfaceDdr4Items, preserveText: false);
            incomingBankComboBox.Text = BankDdr4Items[0];
            incomingInterfaceComboBox.Text = InterfaceDdr4Items[0];
        }
        else if (dramTypeCode == "R")
        {
            SetComboItems(incomingDensityComboBox, DensityDdr5Items, preserveText: false);
            SetComboItems(incomingBankComboBox, BankDdr5Items, preserveText: false);
            SetComboItems(incomingInterfaceComboBox, InterfaceDdr5Items, preserveText: false);
            incomingBankComboBox.Text = BankDdr5Items[0];
            incomingInterfaceComboBox.Text = InterfaceDdr5Items[0];
        }
        else
        {
            SetComboItems(incomingDensityComboBox, DensityDdr4Items.Concat(DensityDdr5Items).ToArray(), preserveText: true);
            SetComboItems(incomingBankComboBox, BankDdr4Items.Concat(BankDdr5Items).ToArray(), preserveText: true);
            SetComboItems(incomingInterfaceComboBox, InterfaceDdr4Items.Concat(InterfaceDdr5Items).ToArray(), preserveText: true);
        }
    }

    private void UpdateIncomingRevisionUi()
    {
        var isRev27 = GetSelectedIncomingRevision() == "27";
        var sourceCode = ExtractCode(incomingSourceComboBox.Text);
        var isInternalSource = sourceCode is "K" or "C";

        incomingVendorLabel.Text = isRev27 ? "Vendor (For TP)" : "Vendor";
        incomingPurchaserLabel.Visible = !isRev27;
        incomingPurchaserComboBox.Visible = !isRev27;

        if (isRev27)
        {
            SetComboItems(incomingVendorComboBox, Vendor27Items, preserveText: true);
            if (isInternalSource)
            {
                incomingVendorComboBox.Text = "(없음)";
                incomingVendorComboBox.Enabled = false;
            }
            else
            {
                incomingVendorComboBox.Enabled = true;
            }

            incomingPurchaserComboBox.Text = "(없음)";
            incomingPurchaserComboBox.Enabled = false;
        }
        else
        {
            SetComboItems(incomingVendorComboBox, Vendor30Items, preserveText: true);
            SetComboItems(incomingPurchaserComboBox, Purchaser30Items, preserveText: true);
            incomingVendorComboBox.Enabled = true;

            if (isInternalSource)
            {
                incomingPurchaserComboBox.Text = "(없음)";
                incomingPurchaserComboBox.Enabled = false;
            }
            else
            {
                incomingPurchaserComboBox.Enabled = true;
            }
        }
    }

    private void previewIncomingButton_Click(object? sender, EventArgs e)
    {
        var request = new IncomingCompRequest
        {
            Revision = GetSelectedIncomingRevision(),
            SourceCode = ExtractCode(incomingSourceComboBox.Text),
            DramTypeCode = ExtractCode(incomingDramTypeComboBox.Text),
            DensityCode = ExtractCode(incomingDensityComboBox.Text),
            BitOrganizationCode = ExtractCode(incomingBitComboBox.Text),
            BankCode = ExtractCode(incomingBankComboBox.Text),
            InterfaceCode = ExtractCode(incomingInterfaceComboBox.Text),
            RevisionCode = ExtractCode(incomingPartRevisionComboBox.Text),
            CompTypeCode = ExtractCode(incomingCompTypeComboBox.Text),
            DieBrandCode = ExtractCode(incomingDieBrandComboBox.Text),
            VendorCode = ExtractCode(incomingVendorComboBox.Text),
            PurchaserCode = ExtractCode(incomingPurchaserComboBox.Text),
            CompType2Code = ExtractCode(incomingCompType2ComboBox.Text),
            PackageTypeCode = ExtractCode(incomingPackageComboBox.Text),
            TesterCode = ExtractCode(incomingTesterComboBox.Text)
        };

        try
        {
            BindRows(_incomingCompService.GeneratePreview(request));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void previewModuleButton_Click(object? sender, EventArgs e)
    {
        var request = new ModuleRequest
        {
            Revision = GetSelectedModuleRevision(),
            DramTypeCode = moduleDramTypeTextBox.Text.Trim(),
            DimmTypeCode = moduleDimmTypeTextBox.Text.Trim(),
            ModuleDensityCode = moduleDensityTextBox.Text.Trim(),
            DieDensityCode = moduleDieDensityTextBox.Text.Trim(),
            CompositionCode = moduleCompositionTextBox.Text.Trim(),
            RankCode = moduleRankTextBox.Text.Trim(),
            GenerationCode = moduleGenerationTextBox.Text.Trim(),
            IcBrandCode = moduleIcBrandTextBox.Text.Trim(),
            ModuleCompTypeCode = moduleCompTypeTextBox.Text.Trim(),
            SpeedCode = moduleSpeedTextBox.Text.Trim(),
            PcbCode = modulePcbTextBox.Text.Trim(),
            VendorCode = moduleVendorTextBox.Text.Trim(),
            PurchaserCode = modulePurchaserTextBox.Text.Trim(),
            BasePartCode = moduleBasePartTextBox.Text.Trim(),
            BinPartCode = moduleBinPartTextBox.Text.Trim()
        };

        try
        {
            BindRows(_moduleService.GeneratePreview(request));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BindRows(IReadOnlyList<GeneratedPartRow> rows)
    {
        resultsGridView.AutoGenerateColumns = true;
        resultsGridView.DataSource = rows.ToList();
    }

    private string GetSelectedIncomingRevision() => incomingRevision27RadioButton.Checked ? "27" : "30";

    private string GetSelectedModuleRevision() => moduleRevision27RadioButton.Checked ? "27" : "30";

    private void BuildUi()
    {
        titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point),
            Location = new Point(16, 12),
            Text = "Ramos Part Generator"
        };

        specPathTitleLabel = new Label
        {
            AutoSize = true,
            Location = new Point(20, 52),
            Text = "Specs"
        };

        specPathValueLabel = new Label
        {
            AutoSize = true,
            Location = new Point(70, 52),
            Text = "-"
        };

        mainTabControl = new TabControl
        {
            Location = new Point(16, 80),
            Size = new Size(980, 360)
        };

        incomingTabPage = new TabPage("입고 & Comp");
        moduleTabPage = new TabPage("Module");
        mainTabControl.TabPages.Add(incomingTabPage);
        mainTabControl.TabPages.Add(moduleTabPage);

        resultsGridView = new DataGridView
        {
            Location = new Point(16, 452),
            Size = new Size(980, 220),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        BuildIncomingTab();
        BuildModuleTab();

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1014, 691);
        Text = "Ramos Part Generator";

        Controls.Add(resultsGridView);
        Controls.Add(mainTabControl);
        Controls.Add(specPathValueLabel);
        Controls.Add(specPathTitleLabel);
        Controls.Add(titleLabel);
    }

    private void BuildIncomingTab()
    {
        incomingRevisionGroupBox = CreateRevisionGroupBox(140, 8, out incomingRevision27RadioButton, out incomingRevision30RadioButton);
        incomingSourceComboBox = CreateEditableComboBox(140, 52);
        incomingDramTypeComboBox = CreateEditableComboBox(140, 82);
        incomingDensityComboBox = CreateEditableComboBox(140, 112);
        incomingBitComboBox = CreateEditableComboBox(140, 142);
        incomingBankComboBox = CreateEditableComboBox(140, 172);
        incomingInterfaceComboBox = CreateEditableComboBox(140, 202);
        incomingPartRevisionComboBox = CreateEditableComboBox(140, 232);
        incomingCompTypeComboBox = CreateEditableComboBox(450, 52);
        incomingDieBrandComboBox = CreateEditableComboBox(450, 82);
        incomingVendorComboBox = CreateEditableComboBox(450, 112);
        incomingPurchaserComboBox = CreateEditableComboBox(450, 142);
        incomingCompType2ComboBox = CreateEditableComboBox(450, 172);
        incomingPackageComboBox = CreateEditableComboBox(760, 52);
        incomingTesterComboBox = CreateEditableComboBox(760, 82);
        incomingVendorLabel = CreateLabel("Vendor", 330, 116);
        incomingPurchaserLabel = CreateLabel("Purchaser", 330, 146);
        previewIncomingButton = new Button
        {
            Text = "입고/Comp 미리생성",
            Location = new Point(760, 120),
            Size = new Size(170, 32)
        };
        previewIncomingButton.Click += previewIncomingButton_Click;

        incomingTabPage.Controls.AddRange(
        new Control[]
        {
            CreateLabel("Spec Rev", 20, 20), incomingRevisionGroupBox,
            CreateLabel("Source", 20, 56), incomingSourceComboBox,
            CreateLabel("DRAM Type", 20, 86), incomingDramTypeComboBox,
            CreateLabel("Density", 20, 116), incomingDensityComboBox,
            CreateLabel("Bit", 20, 146), incomingBitComboBox,
            CreateLabel("Bank", 20, 176), incomingBankComboBox,
            CreateLabel("Interface", 20, 206), incomingInterfaceComboBox,
            CreateLabel("Part Revision", 20, 236), incomingPartRevisionComboBox,
            CreateLabel("Comp Type", 330, 56), incomingCompTypeComboBox,
            CreateLabel("Die Brand", 330, 86), incomingDieBrandComboBox,
            incomingVendorLabel, incomingVendorComboBox,
            incomingPurchaserLabel, incomingPurchaserComboBox,
            CreateLabel("Comp Type 2", 330, 176), incomingCompType2ComboBox,
            CreateLabel("Package", 670, 56), incomingPackageComboBox,
            CreateLabel("Tester", 670, 86), incomingTesterComboBox,
            previewIncomingButton
        });
    }

    private void BuildModuleTab()
    {
        moduleRevisionGroupBox = CreateRevisionGroupBox(140, 8, out moduleRevision27RadioButton, out moduleRevision30RadioButton);
        moduleDramTypeTextBox = CreateTextBox(140, 52);
        moduleDimmTypeTextBox = CreateTextBox(140, 82);
        moduleDensityTextBox = CreateTextBox(140, 112);
        moduleDieDensityTextBox = CreateTextBox(140, 142);
        moduleCompositionTextBox = CreateTextBox(140, 172);
        moduleRankTextBox = CreateTextBox(430, 52);
        moduleGenerationTextBox = CreateTextBox(430, 82);
        moduleIcBrandTextBox = CreateTextBox(430, 112);
        moduleCompTypeTextBox = CreateTextBox(430, 142);
        moduleSpeedTextBox = CreateTextBox(430, 172);
        modulePcbTextBox = CreateTextBox(730, 52);
        moduleVendorTextBox = CreateTextBox(730, 82);
        modulePurchaserTextBox = CreateTextBox(730, 112);
        moduleBasePartTextBox = CreateTextBox(730, 142);
        moduleBinPartTextBox = CreateTextBox(730, 172);
        previewModuleButton = new Button
        {
            Text = "Module 미리생성",
            Location = new Point(730, 210),
            Size = new Size(170, 32)
        };
        previewModuleButton.Click += previewModuleButton_Click;

        moduleTabPage.Controls.AddRange(
        new Control[]
        {
            CreateLabel("Spec Rev", 20, 20), moduleRevisionGroupBox,
            CreateLabel("DRAM Type", 20, 56), moduleDramTypeTextBox,
            CreateLabel("DIMM Type", 20, 86), moduleDimmTypeTextBox,
            CreateLabel("Module Density", 20, 116), moduleDensityTextBox,
            CreateLabel("Die Density", 20, 146), moduleDieDensityTextBox,
            CreateLabel("Composition", 20, 176), moduleCompositionTextBox,
            CreateLabel("Rank", 320, 56), moduleRankTextBox,
            CreateLabel("Generation", 320, 86), moduleGenerationTextBox,
            CreateLabel("I.C Brand", 320, 116), moduleIcBrandTextBox,
            CreateLabel("Comp Type", 320, 146), moduleCompTypeTextBox,
            CreateLabel("Speed", 320, 176), moduleSpeedTextBox,
            CreateLabel("PCB", 640, 56), modulePcbTextBox,
            CreateLabel("Vendor", 640, 86), moduleVendorTextBox,
            CreateLabel("Purchaser", 640, 116), modulePurchaserTextBox,
            CreateLabel("Base Part", 640, 146), moduleBasePartTextBox,
            CreateLabel("Bin Part", 640, 176), moduleBinPartTextBox,
            previewModuleButton
        });
    }

    private static Label CreateLabel(string text, int x, int y)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Location = new Point(x, y + 4)
        };
    }

    private static ComboBox CreateEditableComboBox(int x, int y)
    {
        return new ComboBox
        {
            Location = new Point(x, y),
            Width = 160,
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };
    }

    private static TextBox CreateTextBox(int x, int y)
    {
        return new TextBox
        {
            Location = new Point(x, y),
            Width = 140
        };
    }

    private static GroupBox CreateRevisionGroupBox(int x, int y, out RadioButton rev27RadioButton, out RadioButton rev30RadioButton)
    {
        var groupBox = new GroupBox
        {
            Location = new Point(x, y),
            Size = new Size(160, 40),
            TabStop = false
        };

        rev27RadioButton = new RadioButton
        {
            Text = "27",
            Location = new Point(10, 14),
            AutoSize = true
        };

        rev30RadioButton = new RadioButton
        {
            Text = "30",
            Location = new Point(80, 14),
            AutoSize = true
        };

        groupBox.Controls.Add(rev27RadioButton);
        groupBox.Controls.Add(rev30RadioButton);
        return groupBox;
    }

    private static void SetComboItems(ComboBox comboBox, IEnumerable<string> items, bool preserveText = false)
    {
        var currentText = comboBox.Text;
        comboBox.BeginUpdate();
        comboBox.Items.Clear();
        foreach (var item in items)
        {
            comboBox.Items.Add(item);
        }
        comboBox.EndUpdate();

        if (preserveText)
        {
            comboBox.Text = currentText;
        }
    }

    private static string ExtractCode(string? rawValue)
    {
        var text = (rawValue ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text) || text == "(없음)")
        {
            return "0";
        }

        var separatorIndex = text.IndexOf(" - ", StringComparison.Ordinal);
        return separatorIndex > 0 ? text[..separatorIndex].Trim().ToUpperInvariant() : text.ToUpperInvariant();
    }
}
