using System.ComponentModel;
using RamosPartGenerator.Core.Models;
using RamosPartGenerator.Core.Services;
using RamosPartGenerator.Excel;

namespace RamosPartGenerator.Desktop;

public sealed class MainForm : Form
{
    private const string Revision = "30";
    private const int ActionLabelWidth = 190;
    private const int ActionButtonColumnWidth = 470;
    private const int ActionRowHeight = 36;
    private const int FieldLabelWidth = 220;

    private readonly DesktopAppServices _services;
    private readonly DesktopLookupPage _incomingLookups;
    private readonly DesktopLookupPage _moduleLookups;
    private readonly Dictionary<string, ComboBox> _incomingFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ComboBox> _moduleFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly BindingList<GeneratedPartRow> _incomingRows = new();
    private readonly BindingList<GeneratedPartRow> _moduleRows = new();

    private TextBox _incomingCompPartText = null!;
    private TextBox _moduleCompPartText = null!;
    private TextBox _moduleFullPartText = null!;
    private Label _incomingStatusLabel = null!;
    private Label _moduleStatusLabel = null!;
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
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "Ramos Part Generator - C#";
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        root.Controls.Add(BuildHeader(), 0, 0);

        var tabs = BuildTabControl();
        tabs.TabPages.Add(BuildIncomingPage());
        tabs.TabPages.Add(BuildModulePage());
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

    private static Control BuildHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = RamosTheme.Surface
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 3F));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point),
            Text = "Ramos Part Generator",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = RamosTheme.Blue
        };
        var subtitle = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Spec Rev 30",
            ForeColor = RamosTheme.Gray,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 0, 0)
        };
        var line = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = RamosTheme.Blue
        };

        panel.Controls.Add(title, 0, 0);
        panel.Controls.Add(subtitle, 0, 1);
        panel.Controls.Add(line, 0, 2);
        return panel;
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
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));

        page.Controls.Add(BuildIncomingActions(), 0, 0);
        _incomingStatusLabel = BuildStatusLabel();
        page.Controls.Add(_incomingStatusLabel, 0, 1);

        var fieldGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = RamosTheme.Surface
        };
        fieldGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        fieldGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        fieldGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        fieldGrid.Controls.Add(BuildFieldGroup("Common", IncomingFields("common"), _incomingFields, HandleIncomingFieldChanged), 0, 0);
        fieldGrid.Controls.Add(BuildFieldGroup("Comp Fields", IncomingFields("comp"), _incomingFields, HandleIncomingFieldChanged), 1, 0);
        fieldGrid.Controls.Add(BuildFieldGroup("Extra", IncomingFields("extra"), _incomingFields, HandleIncomingFieldChanged), 2, 0);
        page.Controls.Add(fieldGrid, 0, 2);
        page.Controls.Add(BuildResultGrid(_incomingRows), 0, 3);

        tab.Controls.Add(page);
        return tab;
    }

    private Control BuildIncomingActions()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 4),
            BackColor = RamosTheme.Surface
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionButtonColumnWidth));

        var inputPanel = new Panel { Dock = DockStyle.Fill, BackColor = RamosTheme.Surface };
        _incomingCompPartText = new TextBox { Dock = DockStyle.Fill, CharacterCasing = CharacterCasing.Upper };
        inputPanel.Controls.Add(BuildActionTextRow("Comp Full Part", _incomingCompPartText));

        var buttons = BuildButtonFlow();
        buttons.Controls.Add(BuildButton("Parse", ParseIncoming));
        buttons.Controls.Add(BuildButton("Generate", GenerateIncoming));
        buttons.Controls.Add(BuildButton("Export Excel", ExportIncoming));
        buttons.Controls.Add(BuildButton("Reset", ResetIncoming));

        panel.Controls.Add(inputPanel, 0, 0);
        panel.Controls.Add(buttons, 1, 0);
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
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        page.Controls.Add(BuildModuleActions(), 0, 0);
        _moduleStatusLabel = BuildStatusLabel();
        page.Controls.Add(_moduleStatusLabel, 0, 1);

        var fieldGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = RamosTheme.Surface
        };
        fieldGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        fieldGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        fieldGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        fieldGrid.Controls.Add(BuildFieldGroup("Module Base", ModuleFields("base"), _moduleFields, HandleModuleFieldChanged), 0, 0);
        fieldGrid.Controls.Add(BuildFieldGroup("Structure", ModuleFields("structure"), _moduleFields, HandleModuleFieldChanged), 1, 0);
        fieldGrid.Controls.Add(BuildFieldGroup("Output", ModuleFields("output"), _moduleFields, HandleModuleFieldChanged), 2, 0);
        page.Controls.Add(fieldGrid, 0, 2);
        page.Controls.Add(BuildResultGrid(_moduleRows), 0, 3);

        tab.Controls.Add(page);
        return tab;
    }

    private Control BuildModuleActions()
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
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

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
        buttons.Controls.Add(BuildButton("Reset", ResetModule));

        panel.Controls.Add(inputRows, 0, 0);
        panel.SetRowSpan(inputRows, 2);
        panel.Controls.Add(buttons, 1, 0);
        panel.SetRowSpan(buttons, 2);
        return panel;
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
        textBox.Margin = new Padding(0, 5, 10, 5);
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

    private static Label BuildFieldLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(6, 0, 8, 0),
            ForeColor = RamosTheme.Text
        };
    }

    private static GroupBox BuildFieldGroup(
        string title,
        IEnumerable<DesktopLookupField> fields,
        Dictionary<string, ComboBox> target,
        Action<string> onChanged)
    {
        var group = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = title,
            Padding = new Padding(10),
            BackColor = RamosTheme.Panel,
            ForeColor = RamosTheme.BlueDark
        };
        var visibleFields = fields.Where(field => field.Visible).ToArray();
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = RamosTheme.Panel
        };
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            BackColor = RamosTheme.Panel
        };
        table.RowCount = visibleFields.Length;
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, FieldLabelWidth));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var row = 0;
        foreach (var field in visibleFields)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            var label = BuildFieldLabel(field.Label);
            var combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                IntegralHeight = false,
                Margin = new Padding(0, 2, 6, 2),
                Tag = field,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Color.White,
                ForeColor = RamosTheme.Text
            };
            combo.Items.AddRange(field.Options.Cast<object>().ToArray());
            combo.TextChanged += (_, _) => onChanged(field.Key);

            table.Controls.Add(label, 0, row);
            table.Controls.Add(combo, 1, row);
            target[field.Key] = combo;
            row++;
        }

        scrollPanel.Controls.Add(table);
        group.Controls.Add(scrollPanel);
        return group;
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
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
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

    private IEnumerable<DesktopLookupField> IncomingFields(string section)
    {
        return _incomingLookups.Fields.Where(field => field.Section == section);
    }

    private IEnumerable<DesktopLookupField> ModuleFields(string section)
    {
        return _moduleLookups.Fields.Where(field => field.Section == section);
    }

    private IReadOnlyList<string> IncomingOptions(string key)
    {
        return _incomingLookups.Fields.First(field => field.Key == key).Options;
    }

    private IReadOnlyList<string> ModuleOptions(string key)
    {
        return _moduleLookups.Fields.First(field => field.Key == key).Options;
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
        RunGuarded(_incomingStatusLabel, () =>
        {
            var parsed = _services.Incoming.ParseCompPart(Revision, _incomingCompPartText.Text);
            ApplyIncomingRequest(parsed);
            SetInfo(_incomingStatusLabel, "Incoming/Comp part parsed.");
        });
    }

    private void GenerateIncoming()
    {
        RunGuarded(_incomingStatusLabel, () =>
        {
            foreach (var row in _services.Incoming.GeneratePreview(CreateIncomingRequest()))
            {
                _incomingRows.Add(row);
            }

            SetInfo(_incomingStatusLabel, $"Generated {_incomingRows.Count} incoming/comp rows.");
        });
    }

    private void ExportIncoming()
    {
        ExportRows(_incomingStatusLabel);
    }

    private void ResetIncoming()
    {
        _updatingIncoming = true;
        try
        {
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

            if (ShouldRefreshOptions(changedKey, "dramTypeCode") &&
                _incomingFields.TryGetValue("densityCode", out var densityCombo))
            {
                var densityOptions = dramTypeCode switch
                {
                    "A" => IncomingOptions("densityCode").Where(option => new[] { "4G", "8G", "AG" }.Contains(DisplayHelpers.ExtractCode(option))).ToArray(),
                    "R" => IncomingOptions("densityCode").Where(option => new[] { "AH", "HE", "BH" }.Contains(DisplayHelpers.ExtractCode(option))).ToArray(),
                    _ => IncomingOptions("densityCode")
                };
                SetComboOptions(densityCombo, densityOptions);
            }

            if (ShouldRefreshOptions(changedKey, "dramTypeCode") &&
                _incomingFields.TryGetValue("bankCode", out var bankCombo))
            {
                var bankOptions = dramTypeCode switch
                {
                    "A" => IncomingOptions("bankCode").Where(option => DisplayHelpers.ExtractCode(option) == "5").ToArray(),
                    "R" => IncomingOptions("bankCode").Where(option => DisplayHelpers.ExtractCode(option) == "6").ToArray(),
                    _ => IncomingOptions("bankCode")
                };
                SetComboOptions(bankCombo, bankOptions);
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
                    "A" => IncomingOptions("interfaceCode").Where(option => DisplayHelpers.ExtractCode(option) == "W").ToArray(),
                    "R" => IncomingOptions("interfaceCode").Where(option => DisplayHelpers.ExtractCode(option) == "V").ToArray(),
                    _ => IncomingOptions("interfaceCode")
                };
                SetComboOptions(interfaceCombo, interfaceOptions);
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

            var isThirdParty = sourceCode is "T" or "B";
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
        var dramText = selectedDram switch
        {
            "A" => "DDR4 fixed: 16Bank / POD 1.2V",
            "R" => "DDR5 fixed: 32Bank / POD 1.1V",
            _ => "Select DRAM Type to apply Bank / Interface defaults"
        };
        var sourceText = selectedSource is "T" or "B" ? "Third-party source selected" : "Internal source selected";
        SetInfo(_incomingStatusLabel, $"{dramText} | Rev 30: Vendor + Purchaser | {sourceText}");
    }

    private string ReadIncomingCode(string key)
    {
        return _incomingFields.TryGetValue(key, out var combo) ? DisplayHelpers.ExtractCode(combo.Text) : string.Empty;
    }

    private void SetIncomingField(string key, string code)
    {
        if (_incomingFields.TryGetValue(key, out var combo))
        {
            combo.Text = DisplayHelpers.ResolveDisplayValue(code, IncomingOptions(key));
        }
    }

    private void ParseModuleComp()
    {
        RunGuarded(_moduleStatusLabel, () =>
        {
            var parsed = _services.Module.ParseCompPart(Revision, _moduleCompPartText.Text);
            ApplyModuleRequest(parsed);
            SetInfo(_moduleStatusLabel, "Comp Full Part parsed into module fields.");
        });
    }

    private void ParseModuleFull()
    {
        RunGuarded(_moduleStatusLabel, () =>
        {
            var parsed = _services.Module.ParseModuleFullPart(Revision, _moduleFullPartText.Text);
            ApplyModuleRequest(parsed);
            SetInfo(_moduleStatusLabel, "Module Full Part parsed.");
        });
    }

    private void GenerateModule()
    {
        RunGuarded(_moduleStatusLabel, () =>
        {
            foreach (var row in _services.Module.GeneratePreview(CreateModuleRequest()))
            {
                _moduleRows.Add(row);
            }

            SetInfo(_moduleStatusLabel, $"Generated {_moduleRows.Count} module rows.");
        });
    }

    private void ExportModule()
    {
        ExportRows(_moduleStatusLabel);
    }

    private void ResetModule()
    {
        _updatingModule = true;
        try
        {
            _moduleCompPartText.Text = string.Empty;
            _moduleFullPartText.Text = string.Empty;
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
            ProductBinCode = ReadModuleCode("productBinCode")
        };
    }

    private void ApplyModuleRequest(ModuleRequest request)
    {
        _updatingModule = true;
        try
        {
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
            var speedCode = ReadModuleCode("speedCode");
            var isThirdParty = sourceCode is "TM" or "BM";

            if (ShouldRefreshOptions(changedKey, "dramTypeCode") &&
                _moduleFields.TryGetValue("speedCode", out var speedCombo))
            {
                var speedOptions = dramTypeCode switch
                {
                    "4" => ModuleOptions("speedCode").Where(option => DisplayHelpers.ExtractCode(option) == "WE").ToArray(),
                    "R" => ModuleOptions("speedCode").Where(option => new[] { "QK", "WM", "CM", "CQ", "CR", "CS" }.Contains(DisplayHelpers.ExtractCode(option))).ToArray(),
                    _ => ModuleOptions("speedCode")
                };
                SetComboOptions(speedCombo, speedOptions);
                speedCode = ReadModuleCode("speedCode");
            }

            if (ShouldRefreshOptions(changedKey, "dramTypeCode") &&
                _moduleFields.TryGetValue("dieDensityCode", out var dieDensityCombo))
            {
                var dieDensityOptions = dramTypeCode switch
                {
                    "4" => ModuleOptions("dieDensityCode").Where(option => new[] { "4", "8", "A" }.Contains(DisplayHelpers.ExtractCode(option))).ToArray(),
                    "R" => ModuleOptions("dieDensityCode").Where(option => new[] { "A", "H", "B" }.Contains(DisplayHelpers.ExtractCode(option))).ToArray(),
                    _ => ModuleOptions("dieDensityCode")
                };
                SetComboOptions(dieDensityCombo, dieDensityOptions);
            }

            if (ShouldRefreshOptions(changedKey, "dramTypeCode", "speedCode") &&
                _moduleFields.TryGetValue("bankVddCode", out var bankVddCombo))
            {
                var bankVddCode = ResolveModuleBankVdd(dramTypeCode, speedCode);
                var bankVddOptions = !string.IsNullOrEmpty(bankVddCode)
                    ? ModuleOptions("bankVddCode").Where(option => DisplayHelpers.ExtractCode(option) == DisplayHelpers.ExtractCode(bankVddCode)).ToArray()
                    : dramTypeCode switch
                    {
                        "4" => ModuleOptions("bankVddCode").Where(option => DisplayHelpers.ExtractCode(option) == "4").ToArray(),
                        "R" => ModuleOptions("bankVddCode").Where(option => new[] { "5", "6", "7" }.Contains(DisplayHelpers.ExtractCode(option))).ToArray(),
                        _ => ModuleOptions("bankVddCode")
                    };
                SetComboOptions(bankVddCombo, bankVddOptions);
                if (!string.IsNullOrEmpty(bankVddCode))
                {
                    bankVddCombo.Text = bankVddCode;
                }
                bankVddCombo.Enabled = string.IsNullOrEmpty(bankVddCode);
            }

            if (_moduleFields.TryGetValue("purchaserCode", out var purchaserCombo))
            {
                purchaserCombo.Enabled = isThirdParty;
                if (!isThirdParty)
                {
                    purchaserCombo.Text = string.Empty;
                }
            }

            if (_moduleFields.TryGetValue("a100SpecialCode", out var a100Combo))
            {
                var enabled = isThirdParty && ReadModuleCode("vendorCode") == "A" && ReadModuleCode("purchaserCode") == "A";
                a100Combo.Enabled = enabled;
                if (!enabled)
                {
                    a100Combo.Text = string.Empty;
                }
            }
        }
        finally
        {
            _updatingModule = false;
        }

        var sourceText = ReadModuleCode("moduleSourceCode");
        var sourceStatus = string.IsNullOrEmpty(sourceText)
            ? "Parse Comp Part or Module Part to derive source"
            : $"Source: {sourceText}";
        var partyStatus = sourceText is "TM" or "BM"
            ? "Third-party module"
            : string.IsNullOrEmpty(sourceText) ? "Source not determined yet" : "Internal module";
        SetInfo(_moduleStatusLabel, $"{sourceStatus} | Rev 30: I.C Brand / Comp Type / Vendor + Purchaser | {partyStatus}");
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
        var bankVddCode = dramTypeCode == "4" && speedCode == "WE"
            ? "4"
            : dramTypeCode == "R" && new[] { "QK", "WM" }.Contains(speedCode)
                ? "5"
                : dramTypeCode == "R" && new[] { "CM", "CQ" }.Contains(speedCode)
                    ? "6"
                    : dramTypeCode == "R" && new[] { "CR", "CS" }.Contains(speedCode)
                        ? "7"
                        : string.Empty;

        return DisplayHelpers.ResolveDisplayValue(bankVddCode, ModuleOptions("bankVddCode"));
    }

    private static bool ShouldRefreshOptions(string? changedKey, params string[] dependencyKeys)
    {
        return changedKey is null || dependencyKeys.Contains(changedKey, StringComparer.OrdinalIgnoreCase);
    }

    private static void SetComboOptions(ComboBox combo, IEnumerable<string> options)
    {
        var optionList = options.ToArray();
        var currentText = combo.Text;
        var currentCode = DisplayHelpers.ExtractCode(currentText);

        if (!HasSameOptions(combo, optionList))
        {
            var autoCompleteMode = combo.AutoCompleteMode;
            var autoCompleteSource = combo.AutoCompleteSource;

            combo.BeginUpdate();
            try
            {
                combo.AutoCompleteMode = AutoCompleteMode.None;
                combo.AutoCompleteSource = AutoCompleteSource.None;
                combo.Items.Clear();
                combo.Items.AddRange(optionList.Cast<object>().ToArray());
            }
            finally
            {
                combo.AutoCompleteSource = autoCompleteSource;
                combo.AutoCompleteMode = autoCompleteMode;
                combo.EndUpdate();
            }
        }

        if (string.IsNullOrEmpty(currentCode))
        {
            if (!string.IsNullOrEmpty(combo.Text))
            {
                combo.Text = string.Empty;
            }
            return;
        }

        var resolved = DisplayHelpers.ResolveDisplayValue(currentCode, optionList);
        var nextText = string.IsNullOrEmpty(resolved) || resolved == currentCode && !optionList.Any(option => DisplayHelpers.ExtractCode(option) == currentCode)
            ? string.Empty
            : resolved;
        if (!string.Equals(combo.Text, nextText, StringComparison.Ordinal))
        {
            combo.Text = nextText;
        }
    }

    private static bool HasSameOptions(ComboBox combo, IReadOnlyList<string> options)
    {
        if (combo.Items.Count != options.Count)
        {
            return false;
        }

        for (var index = 0; index < options.Count; index++)
        {
            if (!string.Equals(combo.Items[index]?.ToString(), options[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private void ExportRows(Label statusLabel)
    {
        RunGuarded(statusLabel, () =>
        {
            var rows = BuildExportRows();
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
                return;
            }

            File.WriteAllBytes(dialog.FileName, _services.Exporter.Export(rows));
            SetInfo(statusLabel, $"Excel 내보내기 완료: {dialog.FileName}");
        });
    }

    private GeneratedPartRow[] BuildExportRows()
    {
        return _incomingRows.Concat(_moduleRows).ToArray();
    }

    private string BuildExportFileName()
    {
        return $"DRAM 품목정보({DateTime.Now:yyMMdd}).xlsx";
    }

    private static void RunGuarded(Label statusLabel, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            statusLabel.ForeColor = RamosTheme.Danger;
            statusLabel.Text = ex.Message;
        }
    }

    private static void SetInfo(Label statusLabel, string message)
    {
        statusLabel.ForeColor = RamosTheme.Blue;
        statusLabel.Text = message;
    }

    private sealed class DesktopAppServices
    {
        private DesktopAppServices(
            DesktopLookupCatalog lookups,
            IncomingCompService incoming,
            ModuleService module,
            RegistrationExcelExporter exporter)
        {
            Lookups = lookups;
            Incoming = incoming;
            Module = module;
            Exporter = exporter;
        }

        public DesktopLookupCatalog Lookups { get; }
        public IncomingCompService Incoming { get; }
        public ModuleService Module { get; }
        public RegistrationExcelExporter Exporter { get; }

        public static DesktopAppServices Create()
        {
            var specDirectory = Path.Combine(AppContext.BaseDirectory, "specs");
            var specProvider = new SpecProvider(specDirectory);
            specProvider.Load();

            var textService = new ProductTextService(specProvider);
            return new DesktopAppServices(
                new DesktopLookupCatalog(specProvider),
                new IncomingCompService(specProvider, textService),
                new ModuleService(specProvider, textService),
                new RegistrationExcelExporter());
        }
    }
}
