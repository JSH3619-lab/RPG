using System.ComponentModel;
using System.Diagnostics;
using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;
using RamosPartGenerator.Excel;

namespace RamosPartGenerator.Desktop;

public sealed partial class MainForm : Form
{
    private const string Revision = "30";
    private const int ActionLabelWidth = 190;
    private const int ActionButtonColumnWidth = 470;
    private const int ActionRowHeight = 44;
    private const int ModeRowHeight = 34;
    private const int SelectorGroupColumnWidth = 220;
    private const int SelectorFieldColumnWidth = 360;
    private static string ApplicationVersion =>
        typeof(MainForm).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    private static readonly string[] IncomingDdr4DensityCodes = { "4G", "8G", "AG" };
    private static readonly string[] IncomingDdr5DensityCodes = { "AH", "HE", "BH" };
    private static readonly string[] IncomingStandardBitCodes = { "04", "08", "16" };
    private static readonly string[] IncomingManufacturingBitCodes = { "04", "08", "16", "48" };
    private static readonly string[] ModuleStandardDensityCodes = { "4G", "8G", "AG", "BG", "CG" };
    private static readonly string[] ModuleDdr4DieDensityCodes = { "4", "8", "A" };
    private static readonly string[] ModuleDdr5DieDensityCodes = { "A", "H", "B" };
    private static readonly string[] ModuleStandardCompositionCodes = { "4", "8", "6" };
    private static readonly string[] ModuleManufacturingCompositionCodes = { "4", "8", "6", "9" };
    private static readonly string[] StandardVendorCodes = { "S", "G", "B", "A" };
    private static readonly string[] ManufacturingVendorCodes = { "X" };
    private static readonly string[] ManufacturingCompSourceCodes = { "XC", "ZC" };
    private static readonly string[] ManufacturingIncomingSourceCodes = { "X", "Z" };
    private static readonly string[] ManufacturingModuleSourceCodes = { "XM", "ZM" };
    private static readonly string[] ManufacturingCompTypeCodes = { "0", "1", "2", "3", "4", "5", "6", "7" };

    private readonly DesktopAppServices _services;
    private readonly DesktopLookupPage _incomingLookups;
    private readonly DesktopLookupPage _moduleLookups;
    private readonly Dictionary<string, LookupFieldState> _incomingFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LookupFieldState> _moduleFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly BindingList<GeneratedPartRow> _incomingRows = new();
    private readonly BindingList<GeneratedPartRow> _moduleRows = new();

    private TextBox _incomingCompPartText = null!;
    private TextBox _moduleCompPartText = null!;
    private TextBox _moduleFullPartText = null!;
    private DataGridView _incomingResultGrid = null!;
    private DataGridView _moduleResultGrid = null!;
    private Label _incomingStatusLabel = null!;
    private Label _moduleStatusLabel = null!;
    private RadioButton _incomingStandardMode = null!;
    private RadioButton _incomingManufacturingMode = null!;
    private RadioButton _moduleStandardMode = null!;
    private RadioButton _moduleManufacturingMode = null!;
    private CheckBox _moduleFinishedProductRetest = null!;
    private bool _updatingIncoming;
    private bool _updatingModule;

    private static class RamosTheme
    {
        public static readonly Color Blue = Color.FromArgb(18, 55, 142);
        public static readonly Color BlueDark = Color.FromArgb(8, 38, 104);
        public static readonly Color BlueLight = Color.FromArgb(231, 238, 252);
        public static readonly Color Gray = Color.FromArgb(116, 118, 123);
        public static readonly Color Text = Color.FromArgb(24, 31, 42);
        public static readonly Color Surface = Color.FromArgb(246, 248, 252);
        public static readonly Color Panel = Color.White;
        public static readonly Color Border = Color.FromArgb(205, 213, 226);
        public static readonly Color Danger = Color.FromArgb(169, 45, 45);
    }

    public MainForm()
    {
        _services = DesktopAppServices.Create();
        _incomingLookups = _services.Lookups.BuildIncoming(Revision);
        _moduleLookups = _services.Lookups.BuildModule(Revision);

        InitializeComponent();
        ResetIncoming();
        ResetModule();
        ResetBatchMdl();
        ResetBatchComp();
        AppLog.Info(
            "MainForm.Initialized",
            ("revision", Revision),
            ("displayRevision", _moduleLookups.DisplayRevision),
            ("appVersion", ApplicationVersion));
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = $"Ramos Part Generator v{ApplicationVersion}";
        ApplyApplicationIcon();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 740);
        Size = new Size(1380, 860);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = RamosTheme.Surface;
        ForeColor = RamosTheme.Text;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = RamosTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        root.Controls.Add(BuildHeader(_moduleLookups.DisplayRevision), 0, 0);

        var tabs = BuildTabControl();
        tabs.TabPages.Add(BuildIncomingPage());
        tabs.TabPages.Add(BuildModulePage());
        tabs.TabPages.Add(BuildBatchPage());
        root.Controls.Add(tabs, 0, 1);

        Controls.Add(root);

        ResumeLayout(false);
    }

    private void ApplyApplicationIcon()
    {
        try
        {
            using var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icon is not null)
            {
                Icon = (Icon)icon.Clone();
            }
        }
        catch
        {
            // Icon loading should not block app startup.
        }
    }

    private static TabControl BuildTabControl()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(142, 32),
            SizeMode = TabSizeMode.Fixed,
            Padding = new Point(12, 4)
        };

        tabs.DrawItem += (_, args) =>
        {
            var selected = args.Index == tabs.SelectedIndex;
            var bounds = args.Bounds;
            using var background = new SolidBrush(selected ? RamosTheme.Blue : RamosTheme.Panel);
            using var border = new Pen(RamosTheme.Border);
            using var textBrush = new SolidBrush(selected ? Color.White : RamosTheme.BlueDark);
            args.Graphics.FillRectangle(background, bounds);
            args.Graphics.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            TextRenderer.DrawText(
                args.Graphics,
                tabs.TabPages[args.Index].Text,
                tabs.Font,
                bounds,
                selected ? Color.White : RamosTheme.BlueDark,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };

        return tabs;
    }

    private static Control BuildHeader(string displayRevision)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = RamosTheme.Panel,
            Padding = new Padding(10, 6, 10, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 4F));

        var logo = new PictureBox
        {
            Dock = DockStyle.Fill,
            Image = LoadLogoImage(),
            Margin = new Padding(0, 0, 18, 4),
            SizeMode = PictureBoxSizeMode.Zoom
        };

        var titlePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = RamosTheme.Panel,
            Margin = Padding.Empty
        };
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point),
            Text = "Part Generator",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = RamosTheme.BlueDark
        };
        var subtitle = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"App Ver {ApplicationVersion}  |  Spec Rev {displayRevision}",
            ForeColor = RamosTheme.Gray,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 0, 0)
        };
        titlePanel.Controls.Add(title, 0, 0);
        titlePanel.Controls.Add(subtitle, 0, 1);

        var line = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = RamosTheme.Blue
        };

        panel.Controls.Add(logo, 0, 0);
        panel.Controls.Add(titlePanel, 1, 0);
        panel.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = RamosTheme.Panel }, 2, 0);
        panel.Controls.Add(line, 0, 1);
        panel.SetColumnSpan(line, 3);
        return panel;
    }

    private static Image? LoadLogoImage()
    {
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
        if (File.Exists(logoPath))
        {
            try
            {
                using var image = Image.FromFile(logoPath);
                return new Bitmap(image);
            }
            catch
            {
            }
        }

        using var stream = typeof(MainForm).Assembly.GetManifestResourceStream("RamosPartGenerator.Desktop.Assets.logo.png");
        if (stream is null)
        {
            return null;
        }

        using var embeddedImage = Image.FromStream(stream);
        return new Bitmap(embeddedImage);
    }

    private TabPage BuildIncomingPage()
    {
        var tab = new TabPage("Incoming && Comp") { BackColor = RamosTheme.Surface };
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(8),
            BackColor = RamosTheme.Surface
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, ModeRowHeight + ActionRowHeight + 8F));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));

        page.Controls.Add(BuildIncomingActions(), 0, 0);
        _incomingStatusLabel = BuildStatusLabel();
        page.Controls.Add(_incomingStatusLabel, 0, 1);

        page.Controls.Add(
            BuildFieldSelector(
                new[]
                {
                    new SelectorSection("common", "Common"),
                    new SelectorSection("comp", "Comp Fields"),
                    new SelectorSection("extra", "Extra")
                },
                _incomingLookups.Fields,
                _incomingFields,
                HandleIncomingFieldChanged),
            0,
            2);
        _incomingResultGrid = BuildResultGrid(_incomingRows);
        page.Controls.Add(_incomingResultGrid, 0, 3);

        tab.Controls.Add(page);
        return tab;
    }

    private Control BuildIncomingActions()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(0, 4, 0, 4),
            BackColor = RamosTheme.Surface
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionButtonColumnWidth));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, ModeRowHeight));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, ActionRowHeight));

        var inputPanel = new Panel { Dock = DockStyle.Fill, BackColor = RamosTheme.Surface };
        _incomingCompPartText = new TextBox { Dock = DockStyle.Fill, CharacterCasing = CharacterCasing.Upper };
        inputPanel.Controls.Add(BuildActionTextRow("Comp Full Part", _incomingCompPartText, BuildButton("Parse", ParseIncoming)));

        var buttons = BuildButtonFlow();
        buttons.Controls.Add(BuildButton("Generate", GenerateIncoming));
        buttons.Controls.Add(BuildButton("Export Excel", ExportIncoming));
        buttons.Controls.Add(BuildButton("Delete Selected", DeleteSelectedIncoming));
        buttons.Controls.Add(BuildButton("Reset", ResetIncoming));

        panel.Controls.Add(BuildIncomingModeSelector(), 0, 0);
        panel.Controls.Add(inputPanel, 0, 1);
        panel.Controls.Add(buttons, 1, 0);
        panel.SetRowSpan(buttons, 2);
        return panel;
    }

    private TabPage BuildModulePage()
    {
        var tab = new TabPage("Module") { BackColor = RamosTheme.Surface };
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(8),
            BackColor = RamosTheme.Surface
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 142F));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));

        page.Controls.Add(BuildModuleActions(), 0, 0);
        _moduleStatusLabel = BuildStatusLabel();
        page.Controls.Add(_moduleStatusLabel, 0, 1);

        page.Controls.Add(
            BuildFieldSelector(
                new[]
                {
                    new SelectorSection("base", "Module Base"),
                    new SelectorSection("structure", "Structure"),
                    new SelectorSection("output", "Output")
                },
                _moduleLookups.Fields,
                _moduleFields,
                HandleModuleFieldChanged),
            0,
            2);
        _moduleResultGrid = BuildResultGrid(_moduleRows);
        page.Controls.Add(_moduleResultGrid, 0, 3);

        tab.Controls.Add(page);
        return tab;
    }

    private Control BuildModuleActions()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(0, 4, 0, 4),
            BackColor = RamosTheme.Surface
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionButtonColumnWidth));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, ModeRowHeight));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, ActionRowHeight));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, ActionRowHeight));

        var inputRows = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = RamosTheme.Surface
        };
        inputRows.RowStyles.Add(new RowStyle(SizeType.Absolute, ActionRowHeight));
        inputRows.RowStyles.Add(new RowStyle(SizeType.Absolute, ActionRowHeight));

        _moduleCompPartText = new TextBox { Dock = DockStyle.Fill, CharacterCasing = CharacterCasing.Upper };
        _moduleFullPartText = new TextBox { Dock = DockStyle.Fill, CharacterCasing = CharacterCasing.Upper };
        inputRows.Controls.Add(BuildActionTextRow("Comp Full Part", _moduleCompPartText, BuildButton("Parse", ParseModuleComp)), 0, 0);
        inputRows.Controls.Add(BuildActionTextRow("Module Full Part", _moduleFullPartText, BuildButton("Parse", ParseModuleFull)), 0, 1);

        var buttons = BuildButtonFlow();
        buttons.Controls.Add(BuildButton("Generate", GenerateModule));
        buttons.Controls.Add(BuildButton("Export Excel", ExportModule));
        buttons.Controls.Add(BuildButton("Delete Selected", DeleteSelectedModule));
        buttons.Controls.Add(BuildButton("Reset", ResetModule));

        panel.Controls.Add(BuildModuleModeSelector(), 0, 0);
        panel.Controls.Add(inputRows, 0, 1);
        panel.SetRowSpan(inputRows, 2);
        panel.Controls.Add(buttons, 1, 0);
        panel.SetRowSpan(buttons, 3);
        return panel;
    }

    private Control BuildIncomingModeSelector()
    {
        (_incomingStandardMode, _incomingManufacturingMode) = CreateModeSelector(
            isManufacturing =>
            {
                if (!_updatingIncoming)
                {
                    SetIncomingManufacturingMode(isManufacturing, clearFields: true);
                }
            },
            "Standard",
            "TM");
        return BuildModeRow(_incomingStandardMode, _incomingManufacturingMode);
    }

    private Control BuildModuleModeSelector()
    {
        (_moduleStandardMode, _moduleManufacturingMode) = CreateModeSelector(
            isManufacturing =>
            {
                if (!_updatingModule)
                {
                    SetModuleManufacturingMode(isManufacturing, clearFields: true);
                }
            },
            "Standard",
            "TM");
        _moduleFinishedProductRetest = new CheckBox
        {
            Text = "완제품 Retest (00/0Y)",
            AutoSize = true,
            Margin = new Padding(16, 7, 0, 0)
        };
        return BuildModeRow(_moduleStandardMode, _moduleManufacturingMode, _moduleFinishedProductRetest);
    }

    private static (RadioButton Standard, RadioButton Manufacturing) CreateModeSelector(
        Action<bool> onModeChanged,
        string standardText,
        string manufacturingText)
    {
        var standard = new RadioButton
        {
            Text = standardText,
            AutoSize = false,
            Size = new Size(104, 28),
            Checked = true,
            Appearance = Appearance.Button,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 3, 8, 3),
            UseVisualStyleBackColor = false
        };
        var manufacturing = new RadioButton
        {
            Text = manufacturingText,
            AutoSize = false,
            Size = new Size(58, 28),
            Appearance = Appearance.Button,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 3, 0, 3),
            UseVisualStyleBackColor = false
        };
        ApplyModeRadioStyle(standard);
        ApplyModeRadioStyle(manufacturing);

        standard.CheckedChanged += (_, _) =>
        {
            ApplyModeRadioStyle(standard);
            ApplyModeRadioStyle(manufacturing);
            if (standard.Checked)
            {
                onModeChanged(false);
            }
        };
        manufacturing.CheckedChanged += (_, _) =>
        {
            ApplyModeRadioStyle(standard);
            ApplyModeRadioStyle(manufacturing);
            if (manufacturing.Checked)
            {
                onModeChanged(true);
            }
        };

        return (standard, manufacturing);
    }

    private static void ApplyModeRadioStyle(RadioButton radio)
    {
        radio.BackColor = radio.Checked ? RamosTheme.Blue : Color.FromArgb(238, 242, 248);
        radio.ForeColor = radio.Checked ? Color.White : RamosTheme.Gray;
        radio.FlatAppearance.BorderSize = 0;
        radio.FlatAppearance.CheckedBackColor = RamosTheme.Blue;
        radio.FlatAppearance.MouseOverBackColor = radio.Checked ? RamosTheme.BlueDark : RamosTheme.BlueLight;
        radio.FlatAppearance.MouseDownBackColor = radio.Checked ? RamosTheme.BlueDark : RamosTheme.BlueLight;
        radio.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
    }

    private static Control BuildModeRow(RadioButton standard, RadioButton manufacturing, Control? additionalOption = null)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = ModeRowHeight,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = RamosTheme.Surface
        };
        row.RowStyles.Add(new RowStyle(SizeType.Absolute, ModeRowHeight));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionLabelWidth));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.Controls.Add(BuildActionLabel("Part Mode"), 0, 0);

        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = RamosTheme.Surface
        };
        options.Controls.Add(standard);
        options.Controls.Add(manufacturing);
        if (additionalOption is not null)
        {
            options.Controls.Add(additionalOption);
        }
        row.Controls.Add(options, 1, 0);
        return row;
    }

    private static TableLayoutPanel BuildActionTextRow(string labelText, TextBox textBox, Button? trailingButton = null)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = ActionRowHeight,
            ColumnCount = trailingButton is null ? 2 : 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = RamosTheme.Surface
        };
        row.RowStyles.Add(new RowStyle(SizeType.Absolute, ActionRowHeight));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionLabelWidth));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        row.Controls.Add(BuildActionLabel(labelText), 0, 0);
        textBox.Dock = DockStyle.Fill;
        textBox.Margin = new Padding(0, 4, 10, 4);
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = Color.White;
        textBox.ForeColor = RamosTheme.Text;
        row.Controls.Add(textBox, 1, 0);

        if (trailingButton is not null)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104F));
            trailingButton.Margin = new Padding(0, 3, 8, 3);
            row.Controls.Add(trailingButton, 2, 0);
        }

        return row;
    }

    private static Label BuildActionLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = false,
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(6, 0, 14, 0),
            ForeColor = RamosTheme.Text
        };
    }

    private static FlowLayoutPanel BuildButtonFlow()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(8, 0, 0, 0),
            BackColor = RamosTheme.Surface
        };
    }

    private static Button BuildButton(string text, Action action)
    {
        var button = new Button
        {
            AutoSize = false,
            Size = new Size(96, 30),
            Text = text,
            Margin = new Padding(4),
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false
        };
        var isPrimary = text.Equals("Generate", StringComparison.OrdinalIgnoreCase);
        button.BackColor = isPrimary ? RamosTheme.Blue : Color.White;
        button.ForeColor = isPrimary ? Color.White : RamosTheme.BlueDark;
        button.FlatAppearance.BorderColor = isPrimary ? RamosTheme.BlueDark : RamosTheme.Border;
        button.FlatAppearance.MouseOverBackColor = isPrimary ? RamosTheme.BlueDark : RamosTheme.BlueLight;
        button.FlatAppearance.MouseDownBackColor = isPrimary ? RamosTheme.BlueDark : RamosTheme.BlueLight;
        button.Click += (_, _) => action();
        return button;
    }

    private static Label BuildStatusLabel()
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = RamosTheme.Blue,
            Padding = new Padding(3, 0, 0, 0),
            BackColor = RamosTheme.Surface
        };
    }

    private static Control BuildFieldSelector(
        IReadOnlyList<SelectorSection> sections,
        IReadOnlyList<DesktopLookupField> fields,
        Dictionary<string, LookupFieldState> target,
        Action<string> onChanged)
    {
        return new FieldSelectorPanel(sections, fields, target, onChanged);
    }

    private static DataGridView BuildResultGrid(object dataSource)
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            DataSource = dataSource,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            MultiSelect = true,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
            BorderStyle = BorderStyle.FixedSingle,
            BackgroundColor = RamosTheme.Panel,
            GridColor = RamosTheme.Border,
            EnableHeadersVisualStyles = false
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = RamosTheme.Blue;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = RamosTheme.Blue;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = RamosTheme.Text;
        grid.DefaultCellStyle.SelectionBackColor = RamosTheme.Blue;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.AlternatingRowsDefaultCellStyle.BackColor = RamosTheme.BlueLight;
        grid.Columns.Add(BuildTextColumn(nameof(GeneratedPartRow.Kind), "구분", 80));
        grid.Columns.Add(BuildTextColumn(nameof(GeneratedPartRow.PartCode), "품목코드", 150));
        grid.Columns.Add(BuildTextColumn(nameof(GeneratedPartRow.Name), "품목명", 150));
        grid.Columns.Add(BuildTextColumn(nameof(GeneratedPartRow.GeneralInfo), "품목일반정보", 140));
        grid.Columns.Add(BuildTextColumn(nameof(GeneratedPartRow.Specification), "품목규격", 260));
        grid.CellFormatting += (_, args) =>
        {
            if (args.ColumnIndex >= 0 &&
                grid.Columns[args.ColumnIndex].DataPropertyName == nameof(GeneratedPartRow.Kind) &&
                args.Value is string kind)
            {
                args.Value = FormatKind(kind);
                args.FormattingApplied = true;
            }
        };
        return grid;
    }

    private static DataGridViewTextBoxColumn BuildTextColumn(string propertyName, string headerText, float fillWeight)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.Automatic
        };
    }

    private static string FormatKind(string kind)
    {
        return kind switch
        {
            "Incoming" or "입고" => "입고",
            "Comp" => "Comp",
            "Comp BIN" => "Comp BIN",
            "Module" => "MDL",
            "Module Dummy" => "MDL Dummy",
            "Module BIN" => "MDL BIN",
            _ => kind
        };
    }

    private IReadOnlyList<string> IncomingOptions(string key)
    {
        return _incomingLookups.Fields.First(field => field.Key == key).Options;
    }

    private IReadOnlyList<string> ModuleOptions(string key)
    {
        return _moduleLookups.Fields.First(field => field.Key == key).Options;
    }

    private string[] IncomingOptionsByCodes(string key, params string[] codes)
    {
        return OptionsByCodes(IncomingOptions(key), codes);
    }

    private string[] ModuleOptionsByCodes(string key, params string[] codes)
    {
        return OptionsByCodes(ModuleOptions(key), codes);
    }

    private bool IsIncomingManufacturingMode()
    {
        return _incomingManufacturingMode.Checked;
    }

    private bool IsModuleManufacturingMode()
    {
        return _moduleManufacturingMode.Checked;
    }

    private string IncomingModeName()
    {
        return _incomingManufacturingMode.Checked ? _incomingManufacturingMode.Text : _incomingStandardMode.Text;
    }

    private string ModuleModeName()
    {
        return _moduleManufacturingMode.Checked ? _moduleManufacturingMode.Text : _moduleStandardMode.Text;
    }

    private void SetIncomingManufacturingMode(bool isManufacturing, bool clearFields)
    {
        if (_incomingManufacturingMode.Checked != isManufacturing)
        {
            _incomingManufacturingMode.Checked = isManufacturing;
        }

        if (_incomingStandardMode.Checked == isManufacturing)
        {
            _incomingStandardMode.Checked = !isManufacturing;
        }

        if (clearFields)
        {
            ClearIncomingModeSensitiveFields();
        }

        if (!_updatingIncoming)
        {
            RefreshIncomingFieldRules();
        }

        if (clearFields && !_updatingIncoming)
        {
            AppLog.Info("Mode.Change", ("area", "IncomingComp"), ("mode", IncomingModeName()));
        }
    }

    private void SetModuleManufacturingMode(bool isManufacturing, bool clearFields)
    {
        if (_moduleManufacturingMode.Checked != isManufacturing)
        {
            _moduleManufacturingMode.Checked = isManufacturing;
        }

        if (_moduleStandardMode.Checked == isManufacturing)
        {
            _moduleStandardMode.Checked = !isManufacturing;
        }

        if (clearFields)
        {
            ClearModuleModeSensitiveFields();
        }

        if (!_updatingModule)
        {
            RefreshModuleFieldRules();
        }

        if (clearFields && !_updatingModule)
        {
            AppLog.Info("Mode.Change", ("area", "Module"), ("mode", ModuleModeName()));
        }
    }

    private void ClearIncomingModeSensitiveFields()
    {
        foreach (var key in new[] { "sourceCode", "bitOrganizationCode", "compTypeCode", "vendorCode", "purchaserCode" })
        {
            if (_incomingFields.TryGetValue(key, out var combo))
            {
                combo.Text = string.Empty;
            }
        }
    }

    private void ClearModuleModeSensitiveFields()
    {
        foreach (var key in new[] { "moduleSourceCode", "compositionCode", "moduleCompTypeCode", "vendorCode", "purchaserCode", "a100SpecialCode" })
        {
            if (_moduleFields.TryGetValue(key, out var combo))
            {
                combo.Text = string.Empty;
            }
        }
    }

    private void HandleIncomingFieldChanged(string key)
    {
        if (_updatingIncoming)
        {
            return;
        }

        RefreshIncomingFieldRules(key);
    }

    private void HandleModuleFieldChanged(string key)
    {
        if (_updatingModule)
        {
            return;
        }

        RefreshModuleFieldRules(key);
    }

    private void ParseIncoming()
    {
        RunGuarded(_incomingStatusLabel, "Incoming.Parse", () =>
        {
            var partCode = _incomingCompPartText.Text.Trim().ToUpperInvariant();
            AppLog.Info("Incoming.Parse.Start", ("mode", IncomingModeName()), ("partCode", partCode));

            var parsed = _services.Incoming.ParseCompPart(Revision, partCode);
            ApplyIncomingRequest(parsed);
            AppLog.Info(
                "Incoming.Parse.Success",
                ("mode", IncomingModeName()),
                ("partCode", partCode),
                ("sourceCode", parsed.SourceCode),
                ("compTypeCode", parsed.CompTypeCode));
            SetInfo(_incomingStatusLabel, "Incoming/Comp part parsed.");
        });
    }

    private void GenerateIncoming()
    {
        RunGuarded(_incomingStatusLabel, "Incoming.Generate", () =>
        {
            var request = CreateIncomingRequest();
            AppLog.Info(
                "Incoming.Generate.Start",
                ("mode", IncomingModeName()),
                ("sourceCode", request.SourceCode),
                ("compTypeCode", request.CompTypeCode));

            var generatedRows = _services.Incoming.GeneratePreview(request).ToArray();
            foreach (var row in generatedRows)
            {
                _incomingRows.Add(row);
            }

            AppLog.Info(
                "Incoming.Generate.Success",
                ("mode", IncomingModeName()),
                ("generatedRows", generatedRows.Length.ToString()),
                ("totalRows", _incomingRows.Count.ToString()),
                ("firstPartCode", FirstPartCode(generatedRows)));
            SetInfo(_incomingStatusLabel, $"Generated {_incomingRows.Count} incoming/comp rows.");
        });
    }

    private void ExportIncoming()
    {
        ExportRows(_incomingStatusLabel, "IncomingComp");
    }

    private void DeleteSelectedIncoming()
    {
        var deletedRows = DeleteSelectedRows(_incomingResultGrid, _incomingRows);
        AppLog.Info("Incoming.DeleteSelected", ("deletedRows", deletedRows.ToString()), ("totalRows", _incomingRows.Count.ToString()));
        SetInfo(_incomingStatusLabel, deletedRows == 0
            ? "삭제할 결과 셀을 선택해 주세요."
            : $"Deleted {deletedRows} selected incoming/comp rows.");
    }

    private void ResetIncoming()
    {
        _updatingIncoming = true;
        try
        {
            SetIncomingManufacturingMode(false, clearFields: false);
            _incomingCompPartText.Text = string.Empty;
            foreach (var combo in _incomingFields.Values)
            {
                combo.Text = string.Empty;
                combo.Enabled = true;
            }

            _incomingRows.Clear();
        }
        finally
        {
            _updatingIncoming = false;
        }

        RefreshIncomingFieldRules();
        SetInfo(_incomingStatusLabel, "Incoming/Comp fields reset.");
    }

    private IncomingCompRequest CreateIncomingRequest()
    {
        return new IncomingCompRequest
        {
            Revision = Revision,
            SourceCode = ReadIncomingCode("sourceCode"),
            DramTypeCode = ReadIncomingCode("dramTypeCode"),
            DensityCode = ReadIncomingCode("densityCode"),
            BitOrganizationCode = ReadIncomingCode("bitOrganizationCode"),
            BankCode = ReadIncomingCode("bankCode"),
            InterfaceCode = ReadIncomingCode("interfaceCode"),
            RevisionCode = ReadIncomingCode("revisionCode"),
            CompTypeCode = ReadIncomingCode("compTypeCode"),
            DieBrandCode = ReadIncomingCode("dieBrandCode"),
            VendorCode = ReadIncomingCode("vendorCode"),
            PurchaserCode = ReadIncomingCode("purchaserCode"),
            CompType2Code = ReadIncomingCode("compType2Code"),
            PackageTypeCode = ReadIncomingCode("packageTypeCode"),
            TesterCode = ReadIncomingCode("testerCode")
        };
    }

    private void ApplyIncomingRequest(IncomingCompRequest request)
    {
        _updatingIncoming = true;
        try
        {
            SetIncomingManufacturingMode(ManufacturingIncomingSourceCodes.Contains(request.SourceCode, StringComparer.OrdinalIgnoreCase), clearFields: false);
            SetIncomingField("sourceCode", request.SourceCode);
            SetIncomingField("dramTypeCode", request.DramTypeCode);
            SetIncomingField("densityCode", request.DensityCode);
            SetIncomingField("bitOrganizationCode", request.BitOrganizationCode);
            SetIncomingField("bankCode", request.BankCode);
            SetIncomingField("interfaceCode", request.InterfaceCode);
            SetIncomingField("revisionCode", request.RevisionCode);
            SetIncomingField("compTypeCode", request.CompTypeCode);
            SetIncomingField("dieBrandCode", request.DieBrandCode);
            SetIncomingField("vendorCode", request.VendorCode);
            SetIncomingField("purchaserCode", request.PurchaserCode);
            SetIncomingField("compType2Code", request.CompType2Code);
            SetIncomingField("packageTypeCode", request.PackageTypeCode);
            SetIncomingField("testerCode", request.TesterCode);
        }
        finally
        {
            _updatingIncoming = false;
        }

        RefreshIncomingFieldRules();
    }

    private void RefreshIncomingFieldRules(string? changedKey = null)
    {
        _updatingIncoming = true;
        try
        {
            var dramTypeCode = ReadIncomingCode("dramTypeCode");
            var sourceCode = ReadIncomingCode("sourceCode");
            var isManufacturingMode = IsIncomingManufacturingMode();

            if (ShouldRefreshOptions(changedKey, "sourceCode") &&
                _incomingFields.TryGetValue("sourceCode", out var sourceCombo))
            {
                var sourceOptions = isManufacturingMode
                    ? IncomingOptionsByCodes("sourceCode", ManufacturingCompSourceCodes)
                    : OptionsExceptCodes(IncomingOptions("sourceCode"), ManufacturingCompSourceCodes);
                SetFieldOptions(sourceCombo, sourceOptions);
            }

            if (ShouldRefreshOptions(changedKey, "compTypeCode") &&
                _incomingFields.TryGetValue("compTypeCode", out var compTypeCombo))
            {
                var compTypeOptions = isManufacturingMode
                    ? IncomingOptionsByCodes("compTypeCode", ManufacturingCompTypeCodes)
                    : OptionsExceptCodes(IncomingOptions("compTypeCode"), ManufacturingCompTypeCodes);
                SetFieldOptions(compTypeCombo, compTypeOptions);
            }

            if (ShouldRefreshOptions(changedKey, "sourceCode") &&
                _incomingFields.TryGetValue("bitOrganizationCode", out var bitCombo))
            {
                var bitOptions = isManufacturingMode
                    ? IncomingOptionsByCodes("bitOrganizationCode", IncomingManufacturingBitCodes)
                    : IncomingOptionsByCodes("bitOrganizationCode", IncomingStandardBitCodes);
                SetFieldOptions(bitCombo, bitOptions);
            }

            if (ShouldRefreshOptions(changedKey, "dramTypeCode") &&
                _incomingFields.TryGetValue("densityCode", out var densityCombo))
            {
                var densityOptions = dramTypeCode switch
                {
                    "A" => IncomingOptionsByCodes("densityCode", IncomingDdr4DensityCodes),
                    "R" => IncomingOptionsByCodes("densityCode", IncomingDdr5DensityCodes),
                    _ => IncomingOptions("densityCode")
                };
                SetFieldOptions(densityCombo, densityOptions);
            }

            if (ShouldRefreshOptions(changedKey, "dramTypeCode") &&
                _incomingFields.TryGetValue("bankCode", out var bankCombo))
            {
                var bankOptions = dramTypeCode switch
                {
                    "A" => IncomingOptionsByCodes("bankCode", "5"),
                    "R" => IncomingOptionsByCodes("bankCode", "6"),
                    _ => IncomingOptions("bankCode")
                };
                SetFieldOptions(bankCombo, bankOptions);
                bankCombo.Enabled = dramTypeCode is not ("A" or "R");
                if (dramTypeCode == "A")
                {
                    bankCombo.Text = DisplayHelpers.ResolveDisplayValue("5", IncomingOptions("bankCode"));
                }
                else if (dramTypeCode == "R")
                {
                    bankCombo.Text = DisplayHelpers.ResolveDisplayValue("6", IncomingOptions("bankCode"));
                }
            }

            if (ShouldRefreshOptions(changedKey, "dramTypeCode") &&
                _incomingFields.TryGetValue("interfaceCode", out var interfaceCombo))
            {
                var interfaceOptions = dramTypeCode switch
                {
                    "A" => IncomingOptionsByCodes("interfaceCode", "W"),
                    "R" => IncomingOptionsByCodes("interfaceCode", "V"),
                    _ => IncomingOptions("interfaceCode")
                };
                SetFieldOptions(interfaceCombo, interfaceOptions);
                interfaceCombo.Enabled = dramTypeCode is not ("A" or "R");
                if (dramTypeCode == "A")
                {
                    interfaceCombo.Text = DisplayHelpers.ResolveDisplayValue("W", IncomingOptions("interfaceCode"));
                }
                else if (dramTypeCode == "R")
                {
                    interfaceCombo.Text = DisplayHelpers.ResolveDisplayValue("V", IncomingOptions("interfaceCode"));
                }
            }

            var isThirdParty = !isManufacturingMode && (sourceCode is "T" or "B");
            if (_incomingFields.TryGetValue("vendorCode", out var vendorCombo))
            {
                var vendorOptions = isManufacturingMode
                    ? IncomingOptionsByCodes("vendorCode", ManufacturingVendorCodes)
                    : IncomingOptionsByCodes("vendorCode", StandardVendorCodes);
                SetFieldOptions(vendorCombo, vendorOptions);
            }

            if (_incomingFields.TryGetValue("purchaserCode", out var purchaserCombo))
            {
                purchaserCombo.Enabled = isThirdParty;
                if (!isThirdParty)
                {
                    purchaserCombo.Text = string.Empty;
                }
            }
        }
        finally
        {
            _updatingIncoming = false;
        }

        var selectedDram = ReadIncomingCode("dramTypeCode");
        var selectedSource = ReadIncomingCode("sourceCode");
        var selectedManufacturingMode = IsIncomingManufacturingMode();
        var dramText = selectedDram switch
        {
            "A" => "DDR4 fixed: 16Bank / POD 1.2V",
            "R" => "DDR5 fixed: 32Bank / POD 1.1V",
            _ => "Select DRAM Type to apply Bank / Interface defaults"
        };
        var sourceText = selectedManufacturingMode
            ? "TM source selected"
            : selectedSource is "T" or "B" ? "Third-party source selected" : "Internal source selected";
        SetInfo(_incomingStatusLabel, $"{dramText} | Rev 30: Vendor + Purchaser | {sourceText}");
    }

    private string ReadIncomingCode(string key)
    {
        if (!_incomingFields.TryGetValue(key, out var combo))
        {
            return string.Empty;
        }

        var code = DisplayHelpers.ExtractCode(combo.Text);
        return key == "sourceCode" ? DisplayHelpers.ToIncomingSourceCode(code) : code;
    }

    private void SetIncomingField(string key, string code)
    {
        if (_incomingFields.TryGetValue(key, out var combo))
        {
            var displayCode = key == "sourceCode" ? DisplayHelpers.ToCompSourceCode(code) : code;
            combo.Text = DisplayHelpers.ResolveDisplayValue(displayCode, IncomingOptions(key));
        }
    }

    private void ParseModuleComp()
    {
        RunGuarded(_moduleStatusLabel, "Module.ParseComp", () =>
        {
            var partCode = _moduleCompPartText.Text.Trim().ToUpperInvariant();
            AppLog.Info("Module.ParseComp.Start", ("mode", ModuleModeName()), ("partCode", partCode));

            var keepCompDimm = ReadModuleCode("dimmTypeCode") == "C";
            var parsed = _services.Module.ParseCompPart(Revision, partCode);
            ApplyModuleRequest(parsed);
            if (keepCompDimm)
            {
                _updatingModule = true;
                try
                {
                    SetModuleField("dimmTypeCode", "C");
                }
                finally
                {
                    _updatingModule = false;
                }

                RefreshModuleFieldRules("dimmTypeCode");
            }
            AppLog.Info(
                "Module.ParseComp.Success",
                ("mode", ModuleModeName()),
                ("partCode", partCode),
                ("moduleSourceCode", parsed.ModuleSourceCode),
                ("moduleCompTypeCode", parsed.ModuleCompTypeCode));
            SetInfo(_moduleStatusLabel, "Comp Full Part parsed into module fields.");
        });
    }

    private void ParseModuleFull()
    {
        RunGuarded(_moduleStatusLabel, "Module.ParseFull", () =>
        {
            var partCode = _moduleFullPartText.Text.Trim().ToUpperInvariant();
            AppLog.Info("Module.ParseFull.Start", ("mode", ModuleModeName()), ("partCode", partCode));

            var parsed = _services.Module.ParseModuleFullPart(Revision, partCode);
            ApplyModuleRequest(parsed);
            AppLog.Info(
                "Module.ParseFull.Success",
                ("mode", ModuleModeName()),
                ("partCode", partCode),
                ("moduleSourceCode", parsed.ModuleSourceCode),
                ("moduleCompTypeCode", parsed.ModuleCompTypeCode));
            SetInfo(_moduleStatusLabel, "Module Full Part parsed.");
        });
    }

    private void GenerateModule()
    {
        RunGuarded(_moduleStatusLabel, "Module.Generate", () =>
        {
            var request = CreateModuleRequest();
            AppLog.Info(
                "Module.Generate.Start",
                ("mode", ModuleModeName()),
                ("moduleSourceCode", request.ModuleSourceCode),
                ("moduleCompTypeCode", request.ModuleCompTypeCode));

            var generatedRows = _services.Module.GeneratePreview(request).ToArray();
            foreach (var row in generatedRows)
            {
                _moduleRows.Add(row);
            }

            AppLog.Info(
                "Module.Generate.Success",
                ("mode", ModuleModeName()),
                ("generatedRows", generatedRows.Length.ToString()),
                ("totalRows", _moduleRows.Count.ToString()),
                ("firstPartCode", FirstPartCode(generatedRows)));
            SetInfo(_moduleStatusLabel, $"Generated {_moduleRows.Count} module rows.");
        });
    }

    private void ExportModule()
    {
        ExportRows(_moduleStatusLabel, "Module");
    }

    private void DeleteSelectedModule()
    {
        var deletedRows = DeleteSelectedRows(_moduleResultGrid, _moduleRows);
        AppLog.Info("Module.DeleteSelected", ("deletedRows", deletedRows.ToString()), ("totalRows", _moduleRows.Count.ToString()));
        SetInfo(_moduleStatusLabel, deletedRows == 0
            ? "삭제할 결과 셀을 선택해 주세요."
            : $"Deleted {deletedRows} selected module rows.");
    }

    private void ResetModule()
    {
        _updatingModule = true;
        try
        {
            SetModuleManufacturingMode(false, clearFields: false);
            _moduleCompPartText.Text = string.Empty;
            _moduleFullPartText.Text = string.Empty;
            _moduleFinishedProductRetest.Checked = false;
            foreach (var combo in _moduleFields.Values)
            {
                combo.Text = string.Empty;
                combo.Enabled = true;
            }

            _moduleRows.Clear();
        }
        finally
        {
            _updatingModule = false;
        }

        RefreshModuleFieldRules();
        SetInfo(_moduleStatusLabel, "Module fields reset.");
    }

    private ModuleRequest CreateModuleRequest()
    {
        return new ModuleRequest
        {
            Revision = Revision,
            ModuleSourceCode = ReadModuleCode("moduleSourceCode"),
            CompFullPartCode = _moduleCompPartText.Text.Trim().ToUpperInvariant(),
            ModuleFullPartCode = _moduleFullPartText.Text.Trim().ToUpperInvariant(),
            DramTypeCode = ReadModuleDramCode(),
            DimmTypeCode = ReadModuleCode("dimmTypeCode"),
            ModuleDensityCode = ReadModuleCode("moduleDensityCode"),
            BankVddCode = ReadModuleCode("bankVddCode"),
            DieDensityCode = ReadModuleCode("dieDensityCode"),
            CompositionCode = ReadModuleCode("compositionCode"),
            RankCode = ReadModuleCode("rankCode"),
            GenerationCode = ReadModuleCode("generationCode"),
            IcBrandCode = ReadModuleCode("icBrandCode"),
            ModuleCompTypeCode = ReadModuleCode("moduleCompTypeCode"),
            CompTestCode = ReadModuleCode("compTestCode"),
            ModuleSmtCode = ReadModuleCode("moduleSmtCode"),
            ModuleTestCode = ReadModuleCode("moduleTestCode"),
            SpeedCode = ReadModuleCode("speedCode"),
            PcbCode = ReadModuleCode("pcbCode"),
            VendorCode = ReadModuleCode("vendorCode"),
            PurchaserCode = ReadModuleCode("purchaserCode"),
            A100SpecialCode = ReadModuleCode("a100SpecialCode"),
            SpecialCode2Code = ReadModuleCode("specialCode2Code"),
            SpecialCode3Code = ReadModuleCode("specialCode3Code"),
            GradeCode = ReadModuleCode("gradeCode"),
            ProductBinCode = ReadModuleCode("productBinCode"),
            IsFinishedProductRetest = _moduleFinishedProductRetest.Checked
        };
    }

    private void ApplyModuleRequest(ModuleRequest request)
    {
        _updatingModule = true;
        try
        {
            SetModuleManufacturingMode(ManufacturingModuleSourceCodes.Contains(request.ModuleSourceCode, StringComparer.OrdinalIgnoreCase), clearFields: false);
            _moduleFinishedProductRetest.Checked = request.IsFinishedProductRetest;
            SetModuleField("moduleSourceCode", request.ModuleSourceCode);
            SetModuleField("dramTypeCode", request.DramTypeCode);
            SetModuleField("dimmTypeCode", request.DimmTypeCode);
            SetModuleField("moduleDensityCode", request.ModuleDensityCode);
            SetModuleField("bankVddCode", request.BankVddCode);
            SetModuleField("dieDensityCode", request.DieDensityCode);
            SetModuleField("compositionCode", request.CompositionCode);
            SetModuleField("rankCode", request.RankCode);
            SetModuleField("generationCode", request.GenerationCode);
            SetModuleField("icBrandCode", request.IcBrandCode);
            SetModuleField("moduleCompTypeCode", request.ModuleCompTypeCode);
            SetModuleField("compTestCode", request.CompTestCode);
            SetModuleField("moduleSmtCode", request.ModuleSmtCode);
            SetModuleField("moduleTestCode", request.ModuleTestCode);
            SetModuleField("speedCode", request.SpeedCode);
            SetModuleField("pcbCode", request.PcbCode);
            SetModuleField("vendorCode", request.VendorCode);
            SetModuleField("purchaserCode", request.PurchaserCode);
            SetModuleField("a100SpecialCode", request.A100SpecialCode);
            SetModuleField("specialCode2Code", request.SpecialCode2Code);
            SetModuleField("specialCode3Code", request.SpecialCode3Code);
            SetModuleField("gradeCode", request.GradeCode);
            SetModuleField("productBinCode", request.ProductBinCode);
        }
        finally
        {
            _updatingModule = false;
        }

        RefreshModuleFieldRules();
    }

    private void RefreshModuleFieldRules(string? changedKey = null)
    {
        _updatingModule = true;
        try
        {
            var sourceCode = ReadModuleCode("moduleSourceCode");
            var dramTypeCode = ReadModuleDramCode();
            var dimmTypeCode = ReadModuleCode("dimmTypeCode");
            var speedCode = ReadModuleCode("speedCode");
            var isManufacturingMode = IsModuleManufacturingMode();
            var isThirdParty = !isManufacturingMode && (sourceCode is "TM" or "BM");

            if (ShouldRefreshOptions(changedKey, "moduleSourceCode") &&
                _moduleFields.TryGetValue("moduleSourceCode", out var sourceCombo))
            {
                var sourceOptions = isManufacturingMode
                    ? ModuleOptionsByCodes("moduleSourceCode", ManufacturingModuleSourceCodes)
                    : OptionsExceptCodes(ModuleOptions("moduleSourceCode"), ManufacturingModuleSourceCodes);
                SetFieldOptions(sourceCombo, sourceOptions);
            }

            if (ShouldRefreshOptions(changedKey, "moduleCompTypeCode") &&
                _moduleFields.TryGetValue("moduleCompTypeCode", out var compTypeCombo))
            {
                var compTypeOptions = isManufacturingMode
                    ? ModuleOptionsByCodes("moduleCompTypeCode", ManufacturingCompTypeCodes)
                    : OptionsExceptCodes(ModuleOptions("moduleCompTypeCode"), ManufacturingCompTypeCodes);
                SetFieldOptions(compTypeCombo, compTypeOptions);
            }

            if (ShouldRefreshOptions(changedKey, "moduleSourceCode") &&
                _moduleFields.TryGetValue("compositionCode", out var compositionCombo))
            {
                var compositionOptions = isManufacturingMode
                    ? ModuleOptionsByCodes("compositionCode", ModuleManufacturingCompositionCodes)
                    : ModuleOptionsByCodes("compositionCode", ModuleStandardCompositionCodes);
                SetFieldOptions(compositionCombo, compositionOptions);
            }

            if (ShouldRefreshOptions(changedKey, "dimmTypeCode") &&
                _moduleFields.TryGetValue("moduleDensityCode", out var moduleDensityCombo))
            {
                var densityOptions = dimmTypeCode == "C"
                    ? ModuleOptions("moduleDensityCode")
                    : ModuleOptionsByCodes("moduleDensityCode", ModuleStandardDensityCodes);
                SetFieldOptions(moduleDensityCombo, densityOptions);
            }

            if (ShouldRefreshOptions(changedKey, "dramTypeCode") &&
                _moduleFields.TryGetValue("speedCode", out var speedCombo))
            {
                var speedOptions = dramTypeCode switch
                {
                    "4" or "R" => ModuleOptionsByCodes(
                        "speedCode",
                        _services.Lookups.ModuleSpeedCodes(dramTypeCode).ToArray()),
                    _ => ModuleOptions("speedCode")
                };
                SetFieldOptions(speedCombo, speedOptions);
                speedCode = ReadModuleCode("speedCode");
            }

            if (ShouldRefreshOptions(changedKey, "dramTypeCode") &&
                _moduleFields.TryGetValue("dieDensityCode", out var dieDensityCombo))
            {
                var dieDensityOptions = dramTypeCode switch
                {
                    "4" => ModuleOptionsByCodes("dieDensityCode", ModuleDdr4DieDensityCodes),
                    "R" => ModuleOptionsByCodes("dieDensityCode", ModuleDdr5DieDensityCodes),
                    _ => ModuleOptions("dieDensityCode")
                };
                SetFieldOptions(dieDensityCombo, dieDensityOptions);
            }

            if (ShouldRefreshOptions(changedKey, "dramTypeCode", "speedCode") &&
                _moduleFields.TryGetValue("bankVddCode", out var bankVddCombo))
            {
                var bankVddCode = ResolveModuleBankVdd(dramTypeCode, speedCode);
                var bankVddOptions = !string.IsNullOrEmpty(bankVddCode)
                    ? ModuleOptionsByCodes("bankVddCode", DisplayHelpers.ExtractCode(bankVddCode))
                    : dramTypeCode switch
                    {
                        "4" or "R" => ModuleOptionsByCodes(
                            "bankVddCode",
                            _services.Lookups.ModuleBankVddCodes(dramTypeCode).ToArray()),
                        _ => ModuleOptions("bankVddCode")
                    };
                SetFieldOptions(bankVddCombo, bankVddOptions);
                if (!string.IsNullOrEmpty(bankVddCode))
                {
                    bankVddCombo.Text = bankVddCode;
                }
                bankVddCombo.Enabled = string.IsNullOrEmpty(bankVddCode);
            }

            if (_moduleFields.TryGetValue("purchaserCode", out var purchaserCombo))
            {
                if (_moduleFields.TryGetValue("vendorCode", out var vendorCombo))
                {
                    var vendorOptions = isManufacturingMode
                        ? ModuleOptionsByCodes("vendorCode", ManufacturingVendorCodes)
                        : ModuleOptionsByCodes("vendorCode", StandardVendorCodes);
                    SetFieldOptions(vendorCombo, vendorOptions);
                }

                purchaserCombo.Enabled = isThirdParty;
                if (!isThirdParty)
                {
                    purchaserCombo.Text = string.Empty;
                }
            }

            if (_moduleFields.TryGetValue("a100SpecialCode", out var a100Combo))
            {
                var enabled = IsModuleA100SpecialEnabled(isThirdParty);
                a100Combo.Enabled = enabled;
                if (!enabled)
                {
                    a100Combo.Text = string.Empty;
                }
            }

            if (dimmTypeCode == "C" &&
                ShouldRefreshOptions(changedKey, "dimmTypeCode", "dieDensityCode"))
            {
                ApplyModuleCompSaleDefaults();
            }
        }
        finally
        {
            _updatingModule = false;
        }

        var sourceText = ReadModuleCode("moduleSourceCode");
        var isSelectedManufacturingMode = IsModuleManufacturingMode();
        var sourceStatus = string.IsNullOrEmpty(sourceText)
            ? "Parse Comp Part or Module Part to derive source"
            : $"Source: {sourceText}";
        var partyStatus = isSelectedManufacturingMode
            ? "TM module"
            : (sourceText is "TM" or "BM")
            ? "Third-party module"
            : string.IsNullOrEmpty(sourceText) ? "Source not determined yet" : "Internal module";
        var a100Status = IsModuleA100SpecialEnabled(sourceText is "TM" or "BM")
            ? "A100 Special 활성"
            : "A100 조건: Vendor A + Purchaser A";
        SetInfo(
            _moduleStatusLabel,
            $"{sourceStatus} | Rev {_moduleLookups.DisplayRevision} | {partyStatus} | {a100Status}");
    }

    private void ApplyModuleCompSaleDefaults()
    {
        var moduleDensityCode = ModuleService.GetCompSaleModuleDensityCode(ReadModuleCode("dieDensityCode"));
        if (!string.IsNullOrEmpty(moduleDensityCode))
        {
            SetModuleField("moduleDensityCode", moduleDensityCode);
        }

        SetModuleField("rankCode", "0");
        SetModuleField("moduleSmtCode", "0");
        SetModuleField("moduleTestCode", "0");
        SetModuleField("pcbCode", "0");
    }

    private string ReadModuleCode(string key)
    {
        return _moduleFields.TryGetValue(key, out var combo) ? DisplayHelpers.ExtractCode(combo.Text) : string.Empty;
    }

    private string ReadModuleDramCode()
    {
        return _moduleFields.TryGetValue("dramTypeCode", out var combo)
            ? DisplayHelpers.ExtractModuleDramCode(combo.Text)
            : string.Empty;
    }

    private void SetModuleField(string key, string code)
    {
        if (!_moduleFields.TryGetValue(key, out var combo))
        {
            return;
        }

        var displayCode = key == "dramTypeCode" && code == "4" ? "A" : code;
        combo.Text = DisplayHelpers.ResolveDisplayValue(displayCode, ModuleOptions(key));
    }

    private string ResolveModuleBankVdd(string dramTypeCode, string speedCode)
    {
        var bankVddCode = _services.Lookups.ResolveModuleBankVddCode(dramTypeCode, speedCode);

        return DisplayHelpers.ResolveDisplayValue(bankVddCode, ModuleOptions("bankVddCode"));
    }

    private bool IsModuleA100SpecialEnabled(bool isThirdParty)
    {
        return isThirdParty &&
               ReadModuleCode("vendorCode") == "A" &&
               ReadModuleCode("purchaserCode") == "A";
    }

    private static string[] OptionsByCodes(IEnumerable<string> options, params string[] codes)
    {
        var allowedCodes = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
        return options
            .Where(option => allowedCodes.Contains(DisplayHelpers.ExtractCode(option)))
            .ToArray();
    }

    private static string[] OptionsExceptCodes(IEnumerable<string> options, params string[] codes)
    {
        var excludedCodes = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
        return options
            .Where(option => !excludedCodes.Contains(DisplayHelpers.ExtractCode(option)))
            .ToArray();
    }

    private static bool ShouldRefreshOptions(string? changedKey, params string[] dependencyKeys)
    {
        return changedKey is null || dependencyKeys.Contains(changedKey, StringComparer.OrdinalIgnoreCase);
    }

    private static void SetFieldOptions(LookupFieldState field, IEnumerable<string> options)
    {
        var optionList = options.ToArray();
        var currentText = field.Text;
        var currentCode = DisplayHelpers.ExtractCode(currentText);

        field.SetOptions(optionList);

        if (string.IsNullOrEmpty(currentCode))
        {
            if (!string.IsNullOrEmpty(field.Text))
            {
                field.Text = string.Empty;
            }
            return;
        }

        var resolved = DisplayHelpers.ResolveDisplayValue(currentCode, optionList);
        var nextText = string.IsNullOrEmpty(resolved) || resolved == currentCode && !optionList.Any(option => DisplayHelpers.ExtractCode(option) == currentCode)
            ? string.Empty
            : resolved;
        if (!string.Equals(field.Text, nextText, StringComparison.Ordinal))
        {
            field.Text = nextText;
        }
    }

    private static int DeleteSelectedRows(DataGridView grid, BindingList<GeneratedPartRow> rows)
    {
        var selectedRowIndices = grid.SelectedCells
            .Cast<DataGridViewCell>()
            .Select(cell => cell.RowIndex)
            .Where(index => index >= 0 && index < rows.Count)
            .Distinct()
            .OrderByDescending(index => index)
            .ToArray();

        foreach (var rowIndex in selectedRowIndices)
        {
            rows.RemoveAt(rowIndex);
        }

        return selectedRowIndices.Length;
    }

    private void ExportRows(Label statusLabel, string area)
    {
        ExportRows(statusLabel, area, BuildExportRows());
    }

    private void ExportRows(Label statusLabel, string area, IReadOnlyList<GeneratedPartRow> exportRows)
    {
        RunGuarded(statusLabel, $"{area}.Export", () =>
        {
            var rows = exportRows.ToArray();
            AppLog.Info(
                "Export.Start",
                ("area", area),
                ("rowCount", rows.Length.ToString()),
                ("incomingRows", _incomingRows.Count.ToString()),
                ("moduleRows", _moduleRows.Count.ToString()));
            if (rows.Length == 0)
            {
                throw new InvalidOperationException("먼저 생성 결과를 만들어 주세요.");
            }

            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "xlsx",
                FileName = BuildExportFileName(),
                Filter = "Excel Workbook (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                AppLog.Info("Export.Canceled", ("area", area), ("rowCount", rows.Length.ToString()));
                return;
            }

            File.WriteAllBytes(dialog.FileName, _services.Exporter.Export(rows));
            var opened = TryOpenExportedFile(dialog.FileName, area);
            AppLog.Info(
                "Export.Success",
                ("area", area),
                ("rowCount", rows.Length.ToString()),
                ("opened", opened.ToString()),
                ("filePath", dialog.FileName));
            SetInfo(statusLabel, opened
                ? $"Excel 내보내기 완료 및 열기: {dialog.FileName}"
                : $"Excel 내보내기 완료(파일 열기 실패): {dialog.FileName}");
        });
    }

    private static bool TryOpenExportedFile(string filePath, string area)
    {
        try
        {
            Process.Start(new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception exception)
        {
            AppLog.Error("Export.OpenFailed", exception, ("area", area), ("filePath", filePath));
            return false;
        }
    }

    private GeneratedPartRow[] BuildExportRows()
    {
        return _incomingRows.Concat(_moduleRows).ToArray();
    }

    private string BuildExportFileName()
    {
        return $"DRAM 품목정보({DateTime.Now:yyMMdd}).xlsx";
    }

    private static string FirstPartCode(IReadOnlyList<GeneratedPartRow> rows)
    {
        return rows.Count == 0 ? string.Empty : rows[0].PartCode;
    }

    private static void RunGuarded(Label statusLabel, Action action)
    {
        RunGuarded(statusLabel, "Action", action);
    }

    private static void RunGuarded(Label statusLabel, string operationName, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            AppLog.Error($"{operationName}.Failed", ex);
            statusLabel.ForeColor = RamosTheme.Danger;
            statusLabel.Text = ex.Message;
        }
    }

    private static void SetInfo(Label statusLabel, string message)
    {
        statusLabel.ForeColor = RamosTheme.Blue;
        statusLabel.Text = message;
    }

    private sealed record SelectorSection(string Key, string Title);

    private sealed class LookupFieldState
    {
        private string _text = string.Empty;
        private bool _enabled = true;
        private IReadOnlyList<string> _options;

        public LookupFieldState(DesktopLookupField field)
        {
            Field = field;
            _options = field.Options.ToArray();
        }

        public DesktopLookupField Field { get; }
        public string Key => Field.Key;
        public string Label => Field.Label;
        public string Section => Field.Section;
        public IReadOnlyList<string> Options => _options;

        public string Text
        {
            get => _text;
            set
            {
                var nextText = value ?? string.Empty;
                if (string.Equals(_text, nextText, StringComparison.Ordinal))
                {
                    return;
                }

                _text = nextText;
                ValueChanged?.Invoke(this, EventArgs.Empty);
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? ValueChanged;
        public event EventHandler? StateChanged;

        public void SetOptions(IReadOnlyList<string> options)
        {
            if (_options.SequenceEqual(options, StringComparer.Ordinal))
            {
                return;
            }

            _options = options.ToArray();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class FieldSelectorPanel : UserControl
    {
        private const int ColumnHeaderHeight = 34;
        private const int GroupItemHeight = 62;
        private const int FieldItemHeight = 50;
        private const int OptionItemHeight = 44;

        private readonly IReadOnlyList<SelectorSection> _sections;
        private readonly IReadOnlyList<LookupFieldState> _fields;
        private readonly ListBox _sectionList;
        private readonly ListBox _fieldList;
        private readonly ListBox _optionList;
        private readonly Label _optionTitle;
        private readonly Label _optionHint;
        private readonly Label _summaryCodes;
        private SelectorSection? _selectedSection;
        private LookupFieldState? _selectedField;
        private bool _rendering;
        private bool _renderPending;

        public FieldSelectorPanel(
            IReadOnlyList<SelectorSection> sections,
            IReadOnlyList<DesktopLookupField> fields,
            Dictionary<string, LookupFieldState> target,
            Action<string> onChanged)
        {
            Dock = DockStyle.Fill;
            BackColor = RamosTheme.Surface;
            Margin = new Padding(0, 0, 0, 6);
            DoubleBuffered = true;

            target.Clear();
            _fields = fields
                .Where(field => field.Visible)
                .Select(field =>
                {
                    var state = new LookupFieldState(field);
                    state.ValueChanged += (_, _) => onChanged(state.Key);
                    state.StateChanged += (_, _) => RequestRender();
                    target[state.Key] = state;
                    return state;
                })
                .ToArray();
            _sections = sections
                .Where(section => _fields.Any(field => field.Section == section.Key))
                .ToArray();
            _selectedSection = _sections.FirstOrDefault();
            _selectedField = FieldsForSelectedSection().FirstOrDefault();

            _sectionList = CreateListBox(GroupItemHeight);
            _fieldList = CreateListBox(FieldItemHeight);
            _optionList = CreateListBox(OptionItemHeight);
            _optionTitle = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = RamosTheme.BlueDark
            };
            _optionHint = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = RamosTheme.Gray
            };
            _summaryCodes = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = RamosTheme.BlueDark
            };

            _sectionList.DrawItem += DrawSectionItem;
            _fieldList.DrawItem += DrawFieldItem;
            _optionList.DrawItem += DrawOptionItem;
            _sectionList.SelectedIndexChanged += (_, _) => HandleSectionSelected();
            _fieldList.SelectedIndexChanged += (_, _) => HandleFieldSelected();
            _optionList.SelectedIndexChanged += (_, _) => HandleOptionSelected();
            _sectionList.Items.AddRange(_sections.Cast<object>().ToArray());

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = RamosTheme.Border,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(1)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SelectorGroupColumnWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SelectorFieldColumnWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Controls.Add(BuildColumn("그룹", null, _sectionList, mutedHeader: true), 0, 0);
            layout.Controls.Add(BuildSeparator(), 1, 0);
            layout.Controls.Add(BuildColumn("필드", null, _fieldList, mutedHeader: false), 2, 0);
            layout.Controls.Add(BuildSeparator(), 3, 0);
            layout.Controls.Add(BuildColumn("선택 옵션", "드롭다운 대신 클릭해서 값을 확정", BuildOptionBody(), mutedHeader: false), 4, 0);
            Controls.Add(layout);

            Render();
        }

        private void RequestRender()
        {
            if (IsDisposed || _renderPending)
            {
                return;
            }

            _renderPending = true;
            if (!IsHandleCreated)
            {
                _renderPending = false;
                Render();
                return;
            }

            BeginInvoke(new Action(() =>
            {
                if (IsDisposed)
                {
                    return;
                }

                _renderPending = false;
                Render();
            }));
        }

        private IEnumerable<LookupFieldState> FieldsForSelectedSection()
        {
            return _selectedSection is null
                ? Enumerable.Empty<LookupFieldState>()
                : _fields.Where(field => field.Section == _selectedSection.Key);
        }

        private void Render()
        {
            if (_rendering)
            {
                return;
            }

            _rendering = true;
            SuspendLayout();
            try
            {
                if (_selectedSection is null || !_sections.Contains(_selectedSection))
                {
                    _selectedSection = _sections.FirstOrDefault();
                }

                var visibleFields = FieldsForSelectedSection().ToArray();
                if (_selectedField is null || !visibleFields.Contains(_selectedField))
                {
                    _selectedField = visibleFields.FirstOrDefault();
                }

                _sectionList.SelectedItem = _selectedSection;
                RenderFields(visibleFields);
                RenderOptions();
                _sectionList.Invalidate();
            }
            finally
            {
                ResumeLayout(false);
                _rendering = false;
            }
        }

        private void HandleSectionSelected()
        {
            if (_rendering || _sectionList.SelectedItem is not SelectorSection section)
            {
                return;
            }

            _selectedSection = section;
            _selectedField = _fields.FirstOrDefault(field => field.Section == section.Key);
            Render();
        }

        private void HandleFieldSelected()
        {
            if (_rendering || _fieldList.SelectedItem is not LookupFieldState field)
            {
                return;
            }

            _selectedField = field;
            Render();
        }

        private void HandleOptionSelected()
        {
            if (_rendering || _selectedField is null || !_selectedField.Enabled)
            {
                return;
            }

            if (_optionList.SelectedItem is OptionListItem { Value: not null } option)
            {
                _selectedField.Text = option.Value;
            }
        }

        private void RenderFields(IReadOnlyList<LookupFieldState> visibleFields)
        {
            var itemsMatch = _fieldList.Items.Count == visibleFields.Count;
            for (var index = 0; itemsMatch && index < visibleFields.Count; index++)
            {
                itemsMatch = ReferenceEquals(_fieldList.Items[index], visibleFields[index]);
            }

            if (itemsMatch)
            {
                if (!ReferenceEquals(_fieldList.SelectedItem, _selectedField))
                {
                    _fieldList.SelectedItem = _selectedField;
                }

                _fieldList.Invalidate();
                return;
            }

            _fieldList.BeginUpdate();
            try
            {
                _fieldList.Items.Clear();
                _fieldList.Items.AddRange(visibleFields.Cast<object>().ToArray());
                _fieldList.SelectedItem = _selectedField;
            }
            finally
            {
                _fieldList.EndUpdate();
            }
        }

        private void RenderOptions()
        {
            _optionList.BeginUpdate();
            try
            {
                _optionList.Items.Clear();
                if (_selectedField is null)
                {
                    _optionTitle.Text = "선택할 필드가 없습니다.";
                    _optionHint.Text = string.Empty;
                    _optionList.Items.Add(new OptionListItem(null, string.Empty, "선택할 필드가 없습니다."));
                }
                else
                {
                    _optionTitle.Text = _selectedField.Label;
                    _optionHint.Text = _selectedField.Enabled
                        ? "옵션을 클릭하면 값이 확정됩니다."
                        : "이 필드는 현재 조건에서 자동 고정됩니다.";

                    if (_selectedField.Options.Count == 0)
                    {
                        _optionList.Items.Add(new OptionListItem(null, string.Empty, "선택 가능한 옵션이 없습니다."));
                    }
                    else
                    {
                        foreach (var option in _selectedField.Options)
                        {
                            var optionParts = SplitOption(option);
                            _optionList.Items.Add(new OptionListItem(option, optionParts.Code, optionParts.Description));
                        }

                        var selectedIndex = _selectedField.Options
                            .Select((option, index) => new { option, index })
                            .FirstOrDefault(item => IsSelectedOption(_selectedField, item.option))?.index ?? -1;
                        _optionList.SelectedIndex = selectedIndex;
                    }
                }
            }
            finally
            {
                _optionList.EndUpdate();
            }

            var selectedCodes = _fields
                .Select(field => DisplayHelpers.ExtractCode(field.Text))
                .Where(code => !string.IsNullOrEmpty(code));
            _summaryCodes.Text = string.Join("   ", selectedCodes.DefaultIfEmpty("선택된 코드 없음"));
        }

        private Control BuildOptionBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = RamosTheme.Panel
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));

            var titlePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = new Padding(16, 6, 16, 2),
                BackColor = RamosTheme.Panel
            };
            titlePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            titlePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            titlePanel.Controls.Add(_optionTitle, 0, 0);
            titlePanel.Controls.Add(_optionHint, 0, 1);

            var summaryPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = new Padding(16, 6, 16, 6),
                BackColor = RamosTheme.Surface
            };
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            summaryPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            summaryPanel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "현재 선택 코드",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = RamosTheme.Gray
            }, 0, 0);
            summaryPanel.Controls.Add(_summaryCodes, 0, 1);

            body.Controls.Add(titlePanel, 0, 0);
            body.Controls.Add(_optionList, 0, 1);
            body.Controls.Add(summaryPanel, 0, 2);
            return body;
        }

        private static Control BuildColumn(string title, string? hint, Control body, bool mutedHeader)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = RamosTheme.Panel
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, ColumnHeaderHeight));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = hint is null ? 1 : 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(16, 0, 16, 0),
                BackColor = mutedHeader ? Color.FromArgb(240, 243, 248) : Color.FromArgb(248, 250, 253)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            if (hint is not null)
            {
                header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            }

            header.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Text = title,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = RamosTheme.BlueDark
            }, 0, 0);
            if (hint is not null)
            {
                header.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    AutoEllipsis = true,
                    Text = hint,
                    TextAlign = ContentAlignment.MiddleRight,
                    ForeColor = RamosTheme.Gray
                }, 1, 0);
            }

            panel.Controls.Add(header, 0, 0);
            panel.Controls.Add(body, 0, 1);
            return panel;
        }

        private static Control BuildSeparator()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                BackColor = RamosTheme.Border
            };
        }

        private static ListBox CreateListBox(int itemHeight)
        {
            return new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                IntegralHeight = false,
                ItemHeight = itemHeight,
                Margin = Padding.Empty,
                BackColor = RamosTheme.Panel,
                ForeColor = RamosTheme.Text
            };
        }

        private void DrawSectionItem(object? sender, DrawItemEventArgs args)
        {
            if (args.Index < 0 || _sectionList.Items[args.Index] is not SelectorSection section)
            {
                return;
            }

            var sectionFields = _fields.Where(field => field.Section == section.Key).ToArray();
            var subtitle = string.Join(", ", sectionFields.Take(3).Select(field => field.Label));
            if (sectionFields.Length > 3)
            {
                subtitle += "...";
            }

            DrawTwoLineItem(args, section.Title, subtitle, enabled: true);
        }

        private void DrawFieldItem(object? sender, DrawItemEventArgs args)
        {
            if (args.Index < 0 || _fieldList.Items[args.Index] is not LookupFieldState field)
            {
                return;
            }

            DrawTwoLineItem(args, field.Label, BuildFieldSubtitle(field), field.Enabled);
        }

        private void DrawOptionItem(object? sender, DrawItemEventArgs args)
        {
            if (args.Index < 0 || _optionList.Items[args.Index] is not OptionListItem option)
            {
                return;
            }

            var enabled = _selectedField?.Enabled == true && option.Value is not null;
            var selected = enabled && args.State.HasFlag(DrawItemState.Selected);
            using (var backgroundBrush = new SolidBrush(selected ? RamosTheme.Blue : RamosTheme.Panel))
            {
                args.Graphics.FillRectangle(backgroundBrush, args.Bounds);
            }

            if (option.Value is null)
            {
                TextRenderer.DrawText(
                    args.Graphics,
                    option.Description,
                    Font,
                    new Rectangle(args.Bounds.X + 16, args.Bounds.Y, args.Bounds.Width - 32, args.Bounds.Height),
                    RamosTheme.Gray,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                return;
            }

            using var codeFont = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
            var codeColor = selected ? Color.White : enabled ? RamosTheme.BlueDark : RamosTheme.Gray;
            var textColor = selected ? Color.White : enabled ? RamosTheme.Text : RamosTheme.Gray;
            TextRenderer.DrawText(
                args.Graphics,
                option.Code,
                codeFont,
                new Rectangle(args.Bounds.X + 24, args.Bounds.Y, 60, args.Bounds.Height),
                codeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(
                args.Graphics,
                option.Description,
                Font,
                new Rectangle(args.Bounds.X + 86, args.Bounds.Y, args.Bounds.Width - 100, args.Bounds.Height),
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (!selected)
            {
                using var linePen = new Pen(Color.FromArgb(237, 241, 247));
                args.Graphics.DrawLine(linePen, args.Bounds.X + 10, args.Bounds.Bottom - 1, args.Bounds.Right - 10, args.Bounds.Bottom - 1);
            }
        }

        private static void DrawTwoLineItem(DrawItemEventArgs args, string title, string subtitle, bool enabled)
        {
            var selected = args.State.HasFlag(DrawItemState.Selected);
            var background = selected
                ? RamosTheme.BlueLight
                : enabled ? RamosTheme.Panel : Color.FromArgb(242, 244, 248);
            using (var backgroundBrush = new SolidBrush(background))
            {
                args.Graphics.FillRectangle(backgroundBrush, args.Bounds);
            }

            if (selected)
            {
                using var selectedBrush = new SolidBrush(RamosTheme.Blue);
                args.Graphics.FillRectangle(selectedBrush, args.Bounds.X, args.Bounds.Y, 4, args.Bounds.Height);
            }

            var left = args.Bounds.X + (selected ? 14 : 18);
            var titleColor = enabled
                ? selected ? RamosTheme.BlueDark : RamosTheme.Text
                : RamosTheme.Gray;
            using var titleFont = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
            TextRenderer.DrawText(
                args.Graphics,
                title,
                titleFont,
                new Rectangle(left, args.Bounds.Y + 7, args.Bounds.Width - 28, 18),
                titleColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(
                args.Graphics,
                subtitle,
                SystemFonts.MessageBoxFont,
                new Rectangle(left, args.Bounds.Y + 28, args.Bounds.Width - 28, 18),
                RamosTheme.Gray,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            using var linePen = new Pen(Color.FromArgb(237, 241, 247));
            args.Graphics.DrawLine(linePen, args.Bounds.X, args.Bounds.Bottom - 1, args.Bounds.Right, args.Bounds.Bottom - 1);
        }

        private static string BuildFieldSubtitle(LookupFieldState field)
        {
            var text = string.IsNullOrWhiteSpace(field.Text) ? "선택 안 됨" : field.Text;
            return field.Enabled ? text : $"{text} / 자동 고정";
        }

        private static bool IsSelectedOption(LookupFieldState field, string option)
        {
            var selectedCode = DisplayHelpers.ExtractCode(field.Text);
            var optionCode = DisplayHelpers.ExtractCode(option);
            if (!string.IsNullOrEmpty(selectedCode) || !string.IsNullOrEmpty(optionCode))
            {
                return selectedCode.Equals(optionCode, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(field.Text, option, StringComparison.Ordinal);
        }

        private static (string Code, string Description) SplitOption(string option)
        {
            var separatorIndex = option.IndexOf(" - ", StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                return (string.IsNullOrEmpty(DisplayHelpers.ExtractCode(option)) ? option : DisplayHelpers.ExtractCode(option), option);
            }

            return (option[..separatorIndex], option[(separatorIndex + 3)..]);
        }

        private sealed record OptionListItem(string? Value, string Code, string Description);
    }

    private sealed class DesktopAppServices
    {
        private DesktopAppServices(
            DesktopLookupCatalog lookups,
            IncomingCompService incoming,
            ModuleService module,
            BatchGenerationService batch,
            RegistrationExcelExporter exporter)
        {
            Lookups = lookups;
            Incoming = incoming;
            Module = module;
            Batch = batch;
            Exporter = exporter;
        }

        public DesktopLookupCatalog Lookups { get; }
        public IncomingCompService Incoming { get; }
        public ModuleService Module { get; }
        public BatchGenerationService Batch { get; }
        public RegistrationExcelExporter Exporter { get; }

        public static DesktopAppServices Create()
        {
            var specDirectory = Path.Combine(AppContext.BaseDirectory, "specs");
            var specProvider = new SpecProvider(specDirectory);
            AppLog.Info("Spec.Load.Start", ("specDirectory", specDirectory));
            try
            {
                specProvider.Load();
                AppLog.Info(
                    "Spec.Load.Success",
                    ("specDirectory", specDirectory),
                    ("revisions", string.Join(",", specProvider.GetSupportedRevisions())));
            }
            catch (Exception ex)
            {
                AppLog.Error("Spec.Load.Failed", ex, ("specDirectory", specDirectory));
                throw;
            }

            var textService = new ProductTextService(specProvider);
            var incomingService = new IncomingCompService(specProvider, textService);
            var moduleService = new ModuleService(specProvider, textService);
            return new DesktopAppServices(
                new DesktopLookupCatalog(specProvider),
                incomingService,
                moduleService,
                new BatchGenerationService(moduleService, incomingService),
                new RegistrationExcelExporter());
        }
    }
}
