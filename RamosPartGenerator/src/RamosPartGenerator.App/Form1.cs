using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.App;

public partial class Form1 : Form
{
    private static readonly Color AppBackgroundColor = Color.FromArgb(245, 247, 251);
    private static readonly Color SurfaceColor = Color.White;
    private static readonly Color BorderColor = Color.FromArgb(218, 223, 230);
    private static readonly Color PrimaryColor = Color.FromArgb(37, 99, 235);
    private static readonly Color AccentColor = Color.FromArgb(245, 158, 11);
    private static readonly Color AccentDarkColor = Color.FromArgb(217, 119, 6);
    private static readonly Color SuccessColor = Color.FromArgb(22, 163, 74);
    private static readonly Color SuccessDarkColor = Color.FromArgb(21, 128, 61);
    private static readonly Color SecondaryColor = Color.FromArgb(100, 116, 139);
    private static readonly Color SecondaryDarkColor = Color.FromArgb(71, 85, 105);
    private static readonly Font UiFont = new("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    private static readonly Font UiBoldFont = new("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);

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
    private TextBox incomingFullPartTextBox = null!;
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
    private Button incomingParseButton = null!;
    private Button incomingGenerateButton = null!;
    private Button incomingResetButton = null!;

    private GroupBox moduleRevisionGroupBox = null!;
    private RadioButton moduleRevision27RadioButton = null!;
    private RadioButton moduleRevision30RadioButton = null!;
    private TextBox moduleCompFullPartTextBox = null!;
    private TextBox moduleFullPartTextBox = null!;
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
    private Button moduleCompParseButton = null!;
    private Button moduleFullParseButton = null!;
    private Button moduleGenerateButton = null!;
    private Button moduleResetButton = null!;

    private readonly SpecProvider _specProvider;
    private readonly IncomingCompService _incomingCompService;
    private readonly ModuleService _moduleService;

    private static readonly string[] IncomingSourceItems = { "K - RAmos Memory", "T - Ramos TP", "C - CTST Memory", "B - CTST TP" };
    private static readonly string[] DramTypeItems = { "A - DDR4", "R - DDR5" };
    private static readonly string[] DensityDdr4Items = { "4G - 4Gb", "8G - 8Gb", "AG - 16Gb" };
    private static readonly string[] DensityDdr5Items = { "AH - 16Gb", "HE - 24Gb", "BH - 32Gb" };
    private static readonly string[] BitItems = { "04 - x4", "08 - x8", "16 - x16" };
    private static readonly string[] BankDdr4Items = { "5 - 16Bank" };
    private static readonly string[] BankDdr5Items = { "6 - 32Bank" };
    private static readonly string[] InterfaceDdr4Items = { "W - POD 1.2V" };
    private static readonly string[] InterfaceDdr5Items = { "V - POD 1.1V" };
    private static readonly string[] CompTypeItems = { "P - Partial", "U - Pre-Mark Partial", "N - EMC Partial", "H - a chip", "M - Erase Marking", "C - X-Comp", "D - Tested", "G - MDL(GOX)", "T - MDL Reballed(GKKR)", "F - Pre-Mark MDL(FX)", "E - EMC MDL(FX)", "Q - Pre-Mark Reballed(FKKR)", "W - EMC Reballed(FKKR)", "J - G Comp", "A - EMC G Comp", "X - EMC Partial X", "Y - EMC Partial Y", "Z - EMC Partial Z" };
    private static readonly string[] DieBrandItems = { "S - S1(SS)", "G - GIGA S1(SS)", "H - GIGA S2(Hynix)", "M - GIGA S3(Micron)", "C - GIGA S6(CXMT)", "N - GIGA S9(NANYA)" };
    private static readonly string[] Vendor30Items = { "S - S1(SS)", "G - GIGA", "B - BY20", "A - A100", "X - Ramaxel" };
    private static readonly string[] Purchaser30Items = { "(None)", "V - VM", "H - RMHK", "A - ADATA" };
    private static readonly string[] Vendor27Items = { "(None)", "V - VM", "H - RMHK" };
    private static readonly string[] CompType2Items = { "(None)", "B - Reball" };
    private static readonly string[] PackageTypeItems = { "B - FBGA(Flip Chip)", "M - FBGA(DDP)", "R - FBGA(FC-ReMark)", "N - FBGA(DDP-ReMark)" };
    private static readonly string[] TesterItems = { "R - Ramos", "S - No-Test", "A - ADATA", "W - Winpac", "T - DynaCard", "G - GoldKey", "K - CKMT", "Y - Yueyin", "D - OM", "L - SemiconTest", "1 - HTSI", "2 - DLI", "3 - Rayson", "4 - Ramsun", "5 - Powev" };

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
                incomingVendorComboBox.Text = "(None)";
                incomingVendorComboBox.Enabled = false;
            }
            else
            {
                incomingVendorComboBox.Enabled = true;
            }

            incomingPurchaserComboBox.Text = "(None)";
            incomingPurchaserComboBox.Enabled = false;
        }
        else
        {
            SetComboItems(incomingVendorComboBox, Vendor30Items, preserveText: true);
            SetComboItems(incomingPurchaserComboBox, Purchaser30Items, preserveText: true);
            incomingVendorComboBox.Enabled = true;
            if (isInternalSource)
            {
                incomingPurchaserComboBox.Text = "(None)";
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
        titleLabel = new Label { AutoSize = true, Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point), ForeColor = Color.FromArgb(15, 23, 42), Location = new Point(18, 14), Text = "Ramos Part Generator" };
        specPathTitleLabel = new Label { AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(22, 55), Text = "Specs" };
        specPathValueLabel = new Label { AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(76, 55), Text = "-" };
        mainTabControl = new TabControl { Location = new Point(18, 86), Size = new Size(1120, 430), Font = UiFont };
        incomingTabPage = new TabPage("Incoming & Comp") { BackColor = AppBackgroundColor };
        moduleTabPage = new TabPage("Module") { BackColor = AppBackgroundColor };
        mainTabControl.TabPages.Add(incomingTabPage);
        mainTabControl.TabPages.Add(moduleTabPage);
        resultsGridView = new DataGridView { Location = new Point(18, 528), Size = new Size(1120, 220), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
        ApplyGridTheme(resultsGridView);
        BuildIncomingTab();
        BuildModuleTab();
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = AppBackgroundColor;
        ClientSize = new Size(1160, 768);
        Text = "Ramos Part Generator";
        Controls.Add(resultsGridView);
        Controls.Add(mainTabControl);
        Controls.Add(specPathValueLabel);
        Controls.Add(specPathTitleLabel);
        Controls.Add(titleLabel);
    }

    private void BuildIncomingTab()
    {
        var controlPanel = CreateCardPanel(18, 16, 1060, 86);
        var commonPanel = CreateCardPanel(18, 118, 340, 256);
        var compPanel = CreateCardPanel(374, 118, 340, 256);
        var optionPanel = CreateCardPanel(730, 118, 348, 256);
        controlPanel.Controls.Add(CreateSectionHeader("Quick Input", 18, 14));
        controlPanel.Controls.Add(CreateLabel("Spec Rev", 18, 48));
        incomingRevisionGroupBox = CreateRevisionGroupBox(92, 36, out incomingRevision27RadioButton, out incomingRevision30RadioButton);
        controlPanel.Controls.Add(incomingRevisionGroupBox);
        controlPanel.Controls.Add(CreateLabel("Comp Full Part", 240, 48));
        incomingFullPartTextBox = CreateWideTextBox(360, 36, 420);
        incomingParseButton = CreateAccentButton("Parse", 794, 35, 108, 38);
        incomingGenerateButton = CreatePrimaryButton("Generate", 914, 20, 128, 48);
        incomingResetButton = CreateSecondaryButton("Reset", 914, 72, 128, 36);
        incomingParseButton.Click += incomingParseButton_Click;
        incomingGenerateButton.Click += previewIncomingButton_Click;
        incomingResetButton.Click += incomingResetButton_Click;
        controlPanel.Controls.Add(incomingFullPartTextBox);
        controlPanel.Controls.Add(incomingParseButton);
        controlPanel.Controls.Add(incomingGenerateButton);
        controlPanel.Controls.Add(incomingResetButton);
        commonPanel.Controls.Add(CreateSectionHeader("Common", 18, 14));
        incomingSourceComboBox = CreateEditableComboBox(138, 42, 176);
        incomingDramTypeComboBox = CreateEditableComboBox(138, 74, 176);
        incomingDensityComboBox = CreateEditableComboBox(138, 106, 176);
        incomingBitComboBox = CreateEditableComboBox(138, 138, 176);
        incomingBankComboBox = CreateEditableComboBox(138, 170, 176);
        incomingInterfaceComboBox = CreateEditableComboBox(138, 202, 176);
        incomingPartRevisionComboBox = CreateEditableComboBox(138, 234, 176);
        commonPanel.Controls.AddRange(new Control[] { CreateFieldLabel("Source", 18, 46), incomingSourceComboBox, CreateFieldLabel("DRAM Type", 18, 78), incomingDramTypeComboBox, CreateFieldLabel("Density", 18, 110), incomingDensityComboBox, CreateFieldLabel("Bit", 18, 142), incomingBitComboBox, CreateFieldLabel("Bank", 18, 174), incomingBankComboBox, CreateFieldLabel("Interface", 18, 206), incomingInterfaceComboBox, CreateFieldLabel("Part Revision", 18, 238), incomingPartRevisionComboBox });
        compPanel.Controls.Add(CreateSectionHeader("Comp Fields", 18, 14));
        incomingCompTypeComboBox = CreateEditableComboBox(138, 42, 176);
        incomingDieBrandComboBox = CreateEditableComboBox(138, 74, 176);
        incomingVendorComboBox = CreateEditableComboBox(138, 106, 176);
        incomingPurchaserComboBox = CreateEditableComboBox(138, 138, 176);
        incomingCompType2ComboBox = CreateEditableComboBox(138, 170, 176);
        incomingVendorLabel = CreateFieldLabel("Vendor", 18, 110);
        incomingPurchaserLabel = CreateFieldLabel("Purchaser", 18, 142);
        compPanel.Controls.AddRange(new Control[] { CreateFieldLabel("Comp Type", 18, 46), incomingCompTypeComboBox, CreateFieldLabel("Die Brand", 18, 78), incomingDieBrandComboBox, incomingVendorLabel, incomingVendorComboBox, incomingPurchaserLabel, incomingPurchaserComboBox, CreateFieldLabel("Comp Type 2", 18, 174), incomingCompType2ComboBox });
        optionPanel.Controls.Add(CreateSectionHeader("Extra", 18, 14));
        incomingPackageComboBox = CreateEditableComboBox(138, 42, 176);
        incomingTesterComboBox = CreateEditableComboBox(138, 74, 176);
        optionPanel.Controls.AddRange(new Control[] { CreateFieldLabel("Package", 18, 46), incomingPackageComboBox, CreateFieldLabel("Tester", 18, 78), incomingTesterComboBox });
        incomingTabPage.Controls.AddRange(new Control[] { controlPanel, commonPanel, compPanel, optionPanel });
    }
    private void BuildModuleTab()
    {
        var controlPanel = CreateCardPanel(18, 16, 1060, 120);
        var corePanel = CreateCardPanel(18, 152, 340, 222);
        var configPanel = CreateCardPanel(374, 152, 340, 222);
        var partPanel = CreateCardPanel(730, 152, 348, 222);
        controlPanel.Controls.Add(CreateSectionHeader("Quick Input", 18, 14));
        controlPanel.Controls.Add(CreateLabel("Spec Rev", 18, 48));
        moduleRevisionGroupBox = CreateRevisionGroupBox(92, 36, out moduleRevision27RadioButton, out moduleRevision30RadioButton);
        controlPanel.Controls.Add(moduleRevisionGroupBox);
        controlPanel.Controls.Add(CreateLabel("Comp Full Part", 240, 26));
        moduleCompFullPartTextBox = CreateWideTextBox(360, 22, 420);
        moduleCompParseButton = CreateAccentButton("Parse", 794, 21, 108, 38);
        moduleCompParseButton.Click += moduleCompParseButton_Click;
        controlPanel.Controls.Add(moduleCompFullPartTextBox);
        controlPanel.Controls.Add(moduleCompParseButton);
        controlPanel.Controls.Add(CreateLabel("Module Full Part", 240, 66));
        moduleFullPartTextBox = CreateWideTextBox(360, 62, 420);
        moduleFullParseButton = CreateAccentButton("Parse", 794, 61, 108, 38);
        moduleFullParseButton.Click += moduleFullParseButton_Click;
        controlPanel.Controls.Add(moduleFullPartTextBox);
        controlPanel.Controls.Add(moduleFullParseButton);
        moduleGenerateButton = CreatePrimaryButton("Generate", 914, 20, 128, 48);
        moduleResetButton = CreateSecondaryButton("Reset", 914, 72, 128, 36);
        moduleGenerateButton.Click += previewModuleButton_Click;
        moduleResetButton.Click += moduleResetButton_Click;
        controlPanel.Controls.Add(moduleGenerateButton);
        controlPanel.Controls.Add(moduleResetButton);
        corePanel.Controls.Add(CreateSectionHeader("Module Base", 18, 14));
        moduleDramTypeTextBox = CreateTextBox(138, 42, 176);
        moduleDimmTypeTextBox = CreateTextBox(138, 74, 176);
        moduleDensityTextBox = CreateTextBox(138, 106, 176);
        moduleDieDensityTextBox = CreateTextBox(138, 138, 176);
        moduleCompositionTextBox = CreateTextBox(138, 170, 176);
        corePanel.Controls.AddRange(new Control[] { CreateFieldLabel("DRAM Type", 18, 46), moduleDramTypeTextBox, CreateFieldLabel("DIMM Type", 18, 78), moduleDimmTypeTextBox, CreateFieldLabel("Module Density", 18, 110), moduleDensityTextBox, CreateFieldLabel("Die Density", 18, 142), moduleDieDensityTextBox, CreateFieldLabel("Composition", 18, 174), moduleCompositionTextBox });
        configPanel.Controls.Add(CreateSectionHeader("Structure", 18, 14));
        moduleRankTextBox = CreateTextBox(138, 42, 176);
        moduleGenerationTextBox = CreateTextBox(138, 74, 176);
        moduleIcBrandTextBox = CreateTextBox(138, 106, 176);
        moduleCompTypeTextBox = CreateTextBox(138, 138, 176);
        moduleSpeedTextBox = CreateTextBox(138, 170, 176);
        configPanel.Controls.AddRange(new Control[] { CreateFieldLabel("Rank", 18, 46), moduleRankTextBox, CreateFieldLabel("Generation", 18, 78), moduleGenerationTextBox, CreateFieldLabel("I.C Brand", 18, 110), moduleIcBrandTextBox, CreateFieldLabel("Comp Type", 18, 142), moduleCompTypeTextBox, CreateFieldLabel("Speed", 18, 174), moduleSpeedTextBox });
        partPanel.Controls.Add(CreateSectionHeader("Output Fields", 18, 14));
        modulePcbTextBox = CreateTextBox(138, 42, 176);
        moduleVendorTextBox = CreateTextBox(138, 74, 176);
        modulePurchaserTextBox = CreateTextBox(138, 106, 176);
        moduleBasePartTextBox = CreateTextBox(138, 138, 176);
        moduleBinPartTextBox = CreateTextBox(138, 170, 176);
        partPanel.Controls.AddRange(new Control[] { CreateFieldLabel("PCB", 18, 46), modulePcbTextBox, CreateFieldLabel("Vendor", 18, 78), moduleVendorTextBox, CreateFieldLabel("Purchaser", 18, 110), modulePurchaserTextBox, CreateFieldLabel("Base Part", 18, 142), moduleBasePartTextBox, CreateFieldLabel("Bin Part", 18, 174), moduleBinPartTextBox });
        moduleTabPage.Controls.AddRange(new Control[] { controlPanel, corePanel, configPanel, partPanel });
    }

    private void incomingParseButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(this, "Incoming/Comp full part parser is the next step.", "Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void incomingResetButton_Click(object? sender, EventArgs e)
    {
        incomingFullPartTextBox.Clear();
        foreach (var comboBox in new[] { incomingSourceComboBox, incomingDramTypeComboBox, incomingDensityComboBox, incomingBitComboBox, incomingBankComboBox, incomingInterfaceComboBox, incomingPartRevisionComboBox, incomingCompTypeComboBox, incomingDieBrandComboBox, incomingVendorComboBox, incomingPurchaserComboBox, incomingCompType2ComboBox, incomingPackageComboBox, incomingTesterComboBox })
        {
            comboBox.Text = string.Empty;
        }
    }

    private void moduleCompParseButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(this, "Module comp full part parser is the next step.", "Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void moduleFullParseButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(this, "Module full part parser is the next step.", "Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void moduleResetButton_Click(object? sender, EventArgs e)
    {
        moduleCompFullPartTextBox.Clear();
        moduleFullPartTextBox.Clear();
        foreach (var textBox in new[] { moduleDramTypeTextBox, moduleDimmTypeTextBox, moduleDensityTextBox, moduleDieDensityTextBox, moduleCompositionTextBox, moduleRankTextBox, moduleGenerationTextBox, moduleIcBrandTextBox, moduleCompTypeTextBox, moduleSpeedTextBox, modulePcbTextBox, moduleVendorTextBox, modulePurchaserTextBox, moduleBasePartTextBox, moduleBinPartTextBox })
        {
            textBox.Clear();
        }
    }

    private static Label CreateLabel(string text, int x, int y) => new() { AutoSize = true, Font = UiBoldFont, ForeColor = Color.FromArgb(30, 41, 59), Text = text, Location = new Point(x, y + 4) };
    private static Label CreateFieldLabel(string text, int x, int y) => new() { AutoSize = false, Width = 112, Height = 24, Font = UiBoldFont, ForeColor = Color.FromArgb(51, 65, 85), Text = text, Location = new Point(x, y), TextAlign = ContentAlignment.MiddleLeft };
    private static Label CreateSectionHeader(string text, int x, int y) => new() { AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point), ForeColor = Color.FromArgb(15, 23, 42), Text = text, Location = new Point(x, y) };

    private static Panel CreateCardPanel(int x, int y, int width, int height)
    {
        var panel = new Panel { Location = new Point(x, y), Size = new Size(width, height), BackColor = SurfaceColor };
        panel.Paint += (_, e) => { using var pen = new Pen(BorderColor, 1); e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1); };
        return panel;
    }

    private static ComboBox CreateEditableComboBox(int x, int y, int width) => new() { Location = new Point(x, y), Width = width, DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems, DropDownWidth = Math.Max(width, 320), Font = UiFont, FlatStyle = FlatStyle.Flat };
    private static TextBox CreateTextBox(int x, int y, int width) => new() { Location = new Point(x, y), Width = width, Font = UiFont, BorderStyle = BorderStyle.FixedSingle };
    private static TextBox CreateWideTextBox(int x, int y, int width) => CreateTextBox(x, y, width);

    private static GroupBox CreateRevisionGroupBox(int x, int y, out RadioButton rev27RadioButton, out RadioButton rev30RadioButton)
    {
        var groupBox = new GroupBox { Location = new Point(x, y), Size = new Size(130, 42), TabStop = false };
        rev27RadioButton = new RadioButton { Text = "27", Font = UiBoldFont, Location = new Point(12, 14), AutoSize = true };
        rev30RadioButton = new RadioButton { Text = "30", Font = UiBoldFont, Location = new Point(68, 14), AutoSize = true };
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
        if (string.IsNullOrWhiteSpace(text) || text == "(None)" || text == "(없음)")
        {
            return "0";
        }

        var separatorIndex = text.IndexOf(" - ", StringComparison.Ordinal);
        return separatorIndex > 0 ? text[..separatorIndex].Trim().ToUpperInvariant() : text.ToUpperInvariant();
    }

    private static Button CreateAccentButton(string text, int x, int y, int width, int height)
    {
        var button = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, height), Font = UiBoldFont };
        StyleButton(button, AccentColor, AccentDarkColor, Color.White);
        return button;
    }

    private static Button CreatePrimaryButton(string text, int x, int y, int width, int height)
    {
        var button = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, height), Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point) };
        StyleButton(button, SuccessColor, SuccessDarkColor, Color.White);
        return button;
    }

    private static Button CreateSecondaryButton(string text, int x, int y, int width, int height)
    {
        var button = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, height), Font = UiBoldFont };
        StyleButton(button, SecondaryColor, SecondaryDarkColor, Color.White);
        return button;
    }

    private static void StyleButton(Button button, Color backColor, Color hoverColor, Color foreColor)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Cursor = Cursors.Hand;
        button.MouseEnter += (_, _) => button.BackColor = hoverColor;
        button.MouseLeave += (_, _) => button.BackColor = backColor;
    }

    private static void ApplyGridTheme(DataGridView grid)
    {
        grid.BackgroundColor = SurfaceColor;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryColor;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = UiBoldFont;
        grid.ColumnHeadersHeight = 34;
        grid.DefaultCellStyle.BackColor = SurfaceColor;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
        grid.DefaultCellStyle.Font = UiFont;
        grid.GridColor = BorderColor;
    }
}
