using System.ComponentModel;
using RamosPartGenerator.Core.Models;

namespace RamosPartGenerator.Desktop;

public sealed partial class MainForm
{
    private DesktopLookupPage _lpddrLookups = null!;
    private readonly Dictionary<string, LookupFieldState> _lpddrFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly BindingList<GeneratedPartRow> _lpddrRows = new();
    private TextBox _lpddrFullPartText = null!;
    private DataGridView _lpddrResultGrid = null!;
    private Label _lpddrStatusLabel = null!;
    private RadioButton _lpddrPkgMode = null!;
    private RadioButton _lpddrDieMode = null!;
    private CheckBox _lpddrIncludeDuplicates = null!;
    private Label _lpddrErpStatusLabel = null!;
    private bool _updatingLpddr;

    private TabPage BuildLpddrPage()
    {
        var tab = new TabPage("LPDDR") { BackColor = RamosTheme.Surface };
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(8),
            BackColor = RamosTheme.Surface
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, ModeRowHeight + ActionRowHeight * 2 + 8F));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));

        page.Controls.Add(BuildLpddrActions(), 0, 0);
        _lpddrStatusLabel = BuildStatusLabel();
        page.Controls.Add(_lpddrStatusLabel, 0, 1);

        page.Controls.Add(
            BuildFieldSelector(
                new[]
                {
                    new SelectorSection("spec", "Spec"),
                    new SelectorSection("comp", "Comp / Product")
                },
                _lpddrLookups.Fields,
                _lpddrFields,
                HandleLpddrFieldChanged),
            0,
            2);
        _lpddrResultGrid = BuildResultGrid(_lpddrRows);
        AddErpStatusColumn(_lpddrResultGrid);
        page.Controls.Add(_lpddrResultGrid, 0, 3);

        tab.Controls.Add(page);
        return tab;
    }

    private Control BuildLpddrActions()
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

        var inputPanel = new Panel { Dock = DockStyle.Fill, BackColor = RamosTheme.Surface };
        _lpddrFullPartText = new TextBox { Dock = DockStyle.Fill, CharacterCasing = CharacterCasing.Upper };
        inputPanel.Controls.Add(BuildActionTextRow("LPDDR Full Part", _lpddrFullPartText, BuildButton("Parse", ParseLpddr)));

        var buttons = BuildButtonFlow();
        buttons.Controls.Add(BuildButton("Generate", GenerateLpddr));
        buttons.Controls.Add(BuildButton("Export Excel", ExportLpddr));
        buttons.Controls.Add(BuildButton("Delete Selected", DeleteSelectedLpddr));
        buttons.Controls.Add(BuildButton("Reset", ResetLpddr));
        buttons.Controls.Add(BuildErpUploadButton(() => UploadErpParts(_lpddrStatusLabel, "Lpddr")));
        _lpddrIncludeDuplicates = BuildIncludeDuplicatesCheckBox();
        buttons.Controls.Add(_lpddrIncludeDuplicates);
        _lpddrErpStatusLabel = BuildErpStatusLabel();
        buttons.Controls.Add(_lpddrErpStatusLabel);

        panel.Controls.Add(BuildLpddrModeSelector(), 0, 0);
        panel.Controls.Add(inputPanel, 0, 1);
        panel.Controls.Add(buttons, 1, 0);
        panel.SetRowSpan(buttons, 3);
        return panel;
    }

    private Control BuildLpddrModeSelector()
    {
        (_lpddrPkgMode, _lpddrDieMode) = CreateModeSelector(
            isDie =>
            {
                if (!_updatingLpddr)
                {
                    SetLpddrDieMode(isDie, clearFields: true);
                }
            },
            "PKG 입고 (K5)",
            "Die 입고 (K4)");
        _lpddrDieMode.Size = _lpddrPkgMode.Size;
        return BuildModeRow(_lpddrPkgMode, _lpddrDieMode);
    }

    private bool IsLpddrDieMode()
    {
        return _lpddrDieMode.Checked;
    }

    private string LpddrModeName()
    {
        return _lpddrDieMode.Checked ? _lpddrDieMode.Text : _lpddrPkgMode.Text;
    }

    private void SetLpddrDieMode(bool isDie, bool clearFields)
    {
        if (_lpddrDieMode.Checked != isDie)
        {
            _lpddrDieMode.Checked = isDie;
        }

        if (_lpddrPkgMode.Checked == isDie)
        {
            _lpddrPkgMode.Checked = !isDie;
        }

        if (clearFields)
        {
            foreach (var key in new[] { "dieDensityCode", "dieBitCode", "assemblyCode" })
            {
                if (_lpddrFields.TryGetValue(key, out var combo))
                {
                    combo.Text = string.Empty;
                }
            }
        }

        if (!_updatingLpddr)
        {
            RefreshLpddrFieldRules();
        }

        if (clearFields && !_updatingLpddr)
        {
            AppLog.Info("Mode.Change", ("area", "Lpddr"), ("mode", LpddrModeName()));
        }
    }

    private void HandleLpddrFieldChanged(string key)
    {
        if (_updatingLpddr)
        {
            return;
        }

        RefreshLpddrFieldRules(key);
    }

    private void ParseLpddr()
    {
        RunGuarded(_lpddrStatusLabel, "Lpddr.Parse", () =>
        {
            var partCode = _lpddrFullPartText.Text.Trim().ToUpperInvariant();
            AppLog.Info("Lpddr.Parse.Start", ("partCode", partCode));

            var parsed = _services.Lpddr.ParsePart(partCode);
            ApplyLpddrRequest(parsed);
            AppLog.Info(
                "Lpddr.Parse.Success",
                ("partCode", partCode),
                ("sourceCode", parsed.SourceCode),
                ("isDieIncoming", parsed.IsDieIncoming.ToString()));

            var missingNote = string.IsNullOrEmpty(parsed.SpeedCode)
                ? " (조립/테스트/Speed 등 코드에 없는 필드는 직접 선택)"
                : string.Empty;
            SetInfo(_lpddrStatusLabel, $"LPDDR part parsed.{missingNote}");
        });
    }

    private void ApplyLpddrRequest(LpddrRequest request)
    {
        _updatingLpddr = true;
        try
        {
            SetLpddrDieMode(request.IsDieIncoming, clearFields: false);
            SetLpddrField("sourceCode", request.SourceCode);
            SetLpddrField("dramTypeCode", request.DramTypeCode);
            SetLpddrField("densityCode", request.DensityCode);
            SetLpddrField("dieDensityCode", request.DieDensityCode);
            SetLpddrField("dieBitCode", request.DieBitCode);
            SetLpddrField("bitCode", request.BitCode);
            SetLpddrField("bankCode", request.BankCode);
            SetLpddrField("interfaceCode", request.InterfaceCode);
            SetLpddrField("stackCode", request.StackCode);
            SetLpddrField("revisionCode", request.RevisionCode);
            SetLpddrField("dieTypeCode", request.DieTypeCode);
            SetLpddrField("packageTypeCode", request.PackageTypeCode);
            SetLpddrField("dieBrandCode", request.DieBrandCode);
            SetLpddrField("assemblyCode", request.AssemblyCode);
            SetLpddrField("testerCode", request.TesterCode);
            SetLpddrField("speedCode", request.SpeedCode);
            SetLpddrField("pcbRevisionCode", request.PcbRevisionCode);
            SetLpddrField("vendorCode", request.VendorCode);
            SetLpddrField("purchaserCode", request.PurchaserCode);
        }
        finally
        {
            _updatingLpddr = false;
        }

        RefreshLpddrFieldRules();
    }

    private void SetLpddrField(string key, string code)
    {
        if (_lpddrFields.TryGetValue(key, out var combo))
        {
            combo.Text = DisplayHelpers.ResolveDisplayValue(code, LpddrOptions(key));
        }
    }

    private void GenerateLpddr()
    {
        RunGuarded(_lpddrStatusLabel, "Lpddr.Generate", () =>
        {
            var request = CreateLpddrRequest();
            AppLog.Info(
                "Lpddr.Generate.Start",
                ("mode", LpddrModeName()),
                ("sourceCode", request.SourceCode),
                ("dramTypeCode", request.DramTypeCode));

            var generatedRows = _services.Lpddr.GeneratePreview(request).ToArray();
            foreach (var row in generatedRows)
            {
                _lpddrRows.Add(row);
            }

            AppLog.Info(
                "Lpddr.Generate.Success",
                ("mode", LpddrModeName()),
                ("generatedRows", generatedRows.Length.ToString()),
                ("totalRows", _lpddrRows.Count.ToString()),
                ("firstPartCode", FirstPartCode(generatedRows)));

            var registeredCount = NotifyRegisteredParts(generatedRows);
            if (registeredCount > 0)
            {
                SetWarn(_lpddrStatusLabel, $"생성 {_lpddrRows.Count}건 · 이미 등록됨 {registeredCount}건 (품목규격 확인)");
            }
            else
            {
                SetInfo(_lpddrStatusLabel, $"Generated {_lpddrRows.Count} LPDDR rows.");
            }
        });
    }

    private void ExportLpddr()
    {
        ExportRows(_lpddrStatusLabel, "Lpddr", BuildExportRows(), _lpddrIncludeDuplicates.Checked);
    }

    private void DeleteSelectedLpddr()
    {
        var deletedRows = DeleteSelectedRows(_lpddrResultGrid, _lpddrRows);
        AppLog.Info("Lpddr.DeleteSelected", ("deletedRows", deletedRows.ToString()), ("totalRows", _lpddrRows.Count.ToString()));
        SetInfo(_lpddrStatusLabel, deletedRows == 0
            ? "삭제할 결과 셀을 선택해 주세요."
            : $"Deleted {deletedRows} selected LPDDR rows.");
    }

    private void ResetLpddr()
    {
        _updatingLpddr = true;
        try
        {
            SetLpddrDieMode(false, clearFields: false);
            _lpddrFullPartText.Text = string.Empty;
            foreach (var combo in _lpddrFields.Values)
            {
                combo.Text = string.Empty;
                combo.Enabled = true;
            }

            _lpddrRows.Clear();
        }
        finally
        {
            _updatingLpddr = false;
        }

        RefreshLpddrFieldRules();
        SetInfo(_lpddrStatusLabel, "LPDDR fields reset.");
    }

    private LpddrRequest CreateLpddrRequest()
    {
        return new LpddrRequest
        {
            Revision = Revision,
            SourceCode = ReadLpddrCode("sourceCode"),
            IsDieIncoming = IsLpddrDieMode(),
            DramTypeCode = ReadLpddrCode("dramTypeCode"),
            DensityCode = ReadLpddrCode("densityCode"),
            DieDensityCode = ReadLpddrCode("dieDensityCode"),
            DieBitCode = ReadLpddrCode("dieBitCode"),
            BitCode = ReadLpddrCode("bitCode"),
            BankCode = ReadLpddrCode("bankCode"),
            InterfaceCode = ReadLpddrCode("interfaceCode"),
            StackCode = ReadLpddrCode("stackCode"),
            RevisionCode = ReadLpddrCode("revisionCode"),
            DieTypeCode = ReadLpddrCode("dieTypeCode"),
            PackageTypeCode = ReadLpddrCode("packageTypeCode"),
            DieBrandCode = ReadLpddrCode("dieBrandCode"),
            AssemblyCode = ReadLpddrCode("assemblyCode"),
            TesterCode = ReadLpddrCode("testerCode"),
            SpeedCode = ReadLpddrCode("speedCode"),
            PcbRevisionCode = ReadLpddrCode("pcbRevisionCode"),
            VendorCode = ReadLpddrCode("vendorCode"),
            PurchaserCode = ReadLpddrCode("purchaserCode")
        };
    }

    private void RefreshLpddrFieldRules(string? changedKey = null)
    {
        _updatingLpddr = true;
        try
        {
            var isDieMode = IsLpddrDieMode();
            var sourceCode = ReadLpddrCode("sourceCode");

            foreach (var key in new[] { "dieDensityCode", "dieBitCode" })
            {
                if (_lpddrFields.TryGetValue(key, out var combo))
                {
                    combo.Enabled = isDieMode;
                    if (!isDieMode)
                    {
                        combo.Text = string.Empty;
                    }
                }
            }

            if (_lpddrFields.TryGetValue("assemblyCode", out var assemblyCombo))
            {
                // PKG 입고(K5)는 이미 패키지 상태라 Comp 조립이 No Ass'y(0)가 기본. 특수 케이스만 수동 변경.
                if (!isDieMode && string.IsNullOrWhiteSpace(assemblyCombo.Text))
                {
                    assemblyCombo.Text = DisplayHelpers.ResolveDisplayValue("0", LpddrOptions("assemblyCode"));
                }
            }

            if (_lpddrFields.TryGetValue("pcbRevisionCode", out var pcbCombo) &&
                string.IsNullOrWhiteSpace(pcbCombo.Text))
            {
                pcbCombo.Text = "1";
            }

            var isThirdParty = sourceCode is "T" or "B";
            if (_lpddrFields.TryGetValue("purchaserCode", out var purchaserCombo))
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
            _updatingLpddr = false;
        }

        var modeText = IsLpddrDieMode()
            ? "Die 입고(K4): Die Density/Die Bit 입력, 조립 업체 선택"
            : "PKG 입고(K5): 조립 업체 기본 No Ass'y(0)";
        var sourceText = ReadLpddrCode("sourceCode") is "T" or "B" ? "Third-party source selected" : "Internal source selected";
        SetInfo(_lpddrStatusLabel, $"{modeText} | 생성: 입고 + Product (RD 없음) | {sourceText}");
    }

    private IReadOnlyList<string> LpddrOptions(string key)
    {
        return _lpddrLookups.Fields.First(field => field.Key == key).Options;
    }

    private string ReadLpddrCode(string key)
    {
        return _lpddrFields.TryGetValue(key, out var combo)
            ? DisplayHelpers.ExtractCode(combo.Text)
            : string.Empty;
    }
}
