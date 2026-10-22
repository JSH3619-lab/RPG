using System.ComponentModel;
using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Desktop;

public sealed partial class MainForm
{
    // 기존 신형변환 Excel 산출물과 같은 색: 변환됨=노랑, 확인 필요=주황
    private static readonly Color LegacyConvertedBack = Color.FromArgb(255, 242, 204);
    private static readonly Color LegacyCheckBack = Color.FromArgb(248, 203, 173);

    private readonly BindingList<LegacyConvertRow> _legacyRows = new();

    private TextBox _legacyInputText = null!;
    private ComboBox _legacyIcBrandCombo = null!;
    private DataGridView _legacyResultGrid = null!;
    private Label _legacyStatusLabel = null!;

    private TabPage BuildLegacyConvertPage()
    {
        var tab = new TabPage("구형→신형 변환") { BackColor = RamosTheme.Surface };
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8),
            BackColor = RamosTheme.Surface
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 230F));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        page.Controls.Add(BuildLegacyInputGroup(), 0, 0);
        _legacyStatusLabel = BuildStatusLabel();
        page.Controls.Add(_legacyStatusLabel, 0, 1);

        _legacyResultGrid = BuildLegacyResultGrid();
        page.Controls.Add(BuildBatchGridGroup("변환 결과 (노랑: 변환됨 / 주황: 확인 필요 / 빨강: 오류 / 무색: 이미 신형)", _legacyResultGrid), 0, 2);

        tab.Controls.Add(page);
        return tab;
    }

    private Control BuildLegacyInputGroup()
    {
        var group = BuildBatchGroup("구형 Comp/Module Full Part (한 줄에 하나)");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(6),
            BackColor = RamosTheme.Panel
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

        _legacyInputText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            CharacterCasing = CharacterCasing.Upper,
            AcceptsReturn = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            ForeColor = RamosTheme.Text,
            Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point)
        };

        // 구형 Module에 넣을 I.C Brand(= Vendor). 원본 코드로 구분할 수 없어 매번 사용자가 고른다(기본값 없음).
        _legacyIcBrandCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 190,
            Margin = new Padding(4, 8, 4, 4)
        };
        _legacyIcBrandCombo.Items.AddRange(new object[] { "S - 삼성 직구매 PKG (S1)", "G - GIGA S1" });

        var buttons = BuildButtonFlow();
        buttons.Controls.Add(new Label { Text = "I.C Brand", AutoSize = true, Margin = new Padding(4, 12, 0, 0), ForeColor = RamosTheme.Text });
        buttons.Controls.Add(_legacyIcBrandCombo);
        buttons.Controls.Add(BuildButton("Convert", ConvertLegacyParts));
        buttons.Controls.Add(BuildButton("Send to Batch", SendLegacyToBatch));
        buttons.Controls.Add(BuildButton("Reset", ResetLegacyConvert));

        layout.Controls.Add(_legacyInputText, 0, 0);
        layout.Controls.Add(buttons, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private DataGridView BuildLegacyResultGrid()
    {
        // 결과 그리드 스타일(셀 선택·클립보드 복사)만 재사용하고 컬럼은 변환 결과용으로 교체한다.
        var grid = BuildResultGrid(_legacyRows);
        grid.Columns.Clear();
        grid.Columns.Add(BuildTextColumn(nameof(LegacyConvertRow.SourceCode), "구형 Part", 170));
        grid.Columns.Add(BuildTextColumn(nameof(LegacyConvertRow.ConvertedCode), "신형 Part", 170));
        grid.Columns.Add(BuildTextColumn(nameof(LegacyConvertRow.ErpStatus), "ERP", 50));
        grid.Columns.Add(BuildTextColumn(nameof(LegacyConvertRow.Notes), "변경 내용", 260));
        grid.Columns.Add(BuildTextColumn(nameof(LegacyConvertRow.Warnings), "확인 필요", 160));
        grid.Columns.Add(BuildTextColumn(nameof(LegacyConvertRow.Error), "오류", 160));
        grid.CellFormatting += (_, args) =>
        {
            if (args.RowIndex < 0 || args.CellStyle is null || grid.Rows[args.RowIndex].DataBoundItem is not LegacyConvertRow row)
            {
                return;
            }

            if (row.Error.Length > 0)
            {
                args.CellStyle.ForeColor = RamosTheme.Danger;
            }
            else if (row.Warnings.Length > 0)
            {
                args.CellStyle.BackColor = LegacyCheckBack;
            }
            else if (row.ConvertedCode != row.SourceCode)
            {
                args.CellStyle.BackColor = LegacyConvertedBack;
            }
        };
        return grid;
    }

    private void ConvertLegacyParts()
    {
        RunGuarded(_legacyStatusLabel, "LegacyConvert.Convert", () =>
        {
            var partCodes = _legacyInputText.Lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
            if (partCodes.Length == 0)
            {
                throw new InvalidOperationException("구형 Part를 한 줄에 하나씩 입력해 주세요.");
            }

            if (_legacyIcBrandCombo.SelectedItem is not string icBrandItem)
            {
                throw new InvalidOperationException("I.C Brand(S: 삼성 직구매 PKG / G: GIGA)를 선택해 주세요.");
            }

            var icBrand = icBrandItem[0];

            RefreshErpIndex();
            _legacyRows.Clear();
            foreach (var partCode in partCodes)
            {
                _legacyRows.Add(ConvertLegacyPart(partCode, icBrand));
            }

            var errorCount = _legacyRows.Count(row => row.Error.Length > 0);
            var checkCount = _legacyRows.Count(row => row.Error.Length == 0 && row.Warnings.Length > 0);
            var changedCount = _legacyRows.Count(row => row.Error.Length == 0 && row.ConvertedCode != row.SourceCode);
            AppLog.Info(
                "LegacyConvert.Convert.Success",
                ("inputCount", partCodes.Length.ToString()),
                ("icBrand", icBrand.ToString()),
                ("changedCount", changedCount.ToString()),
                ("checkCount", checkCount.ToString()),
                ("errorCount", errorCount.ToString()));
            _legacyStatusLabel.ForeColor = errorCount > 0 || checkCount > 0 ? RamosTheme.Danger : RamosTheme.Blue;
            _legacyStatusLabel.Text =
                $"입력 {partCodes.Length} | 변환 {changedCount} | 이미 신형 {partCodes.Length - changedCount - errorCount} | 확인 필요 {checkCount} | 오류 {errorCount}";
        });
    }

    private LegacyConvertRow ConvertLegacyPart(string partCode, char icBrand)
    {
        LegacyPartConversion? result = null;
        try
        {
            result = LegacyPartConverter.Convert(partCode, icBrand);
            var converted = result.ConvertedCode;

            // 신형 코드는 Core 파서 -> 생성기 왕복으로 같은 코드가 나와야 사용할 수 있다.
            var regenerated = LegacyPartConverter.IsCompPart(converted)
                ? _services.Incoming.GeneratePreview(_services.Incoming.ParseCompPart(Revision, converted)).Where(row => row.Kind == "Comp")
                : _services.Module.GeneratePreview(_services.Module.ParseModuleFullPart(Revision, converted));
            if (!regenerated.Any(row => row.PartCode == converted))
            {
                throw new InvalidOperationException("신형 Part를 다시 생성한 결과가 일치하지 않습니다.");
            }

            var erpStatus = _erpIndex.HasData ? (_erpIndex.Contains(converted) ? "등록됨" : "신규") : string.Empty;
            return new LegacyConvertRow(
                result.SourceCode,
                converted,
                erpStatus,
                string.Join(" / ", result.Notes),
                string.Join(" / ", result.Warnings),
                string.Empty);
        }
        catch (Exception ex)
        {
            AppLog.Info("LegacyConvert.Item.Failed", ("partCode", partCode), ("message", ex.Message));
            return new LegacyConvertRow(
                partCode.Trim().ToUpperInvariant(),
                result?.ConvertedCode ?? string.Empty,
                string.Empty,
                result is null ? string.Empty : string.Join(" / ", result.Notes),
                result is null ? string.Empty : string.Join(" / ", result.Warnings),
                ex.Message);
        }
    }

    // 오류·확인 필요 행은 빼고 신형 Part를 MDL 일괄/Comp 일괄 입력 끝에 추가한다. 생성·Export는 기존 일괄 탭에서 한다.
    private void SendLegacyToBatch()
    {
        var sendable = _legacyRows
            .Where(row => row.Error.Length == 0 && row.Warnings.Length == 0)
            .Select(row => row.ConvertedCode)
            .Distinct()
            .ToArray();
        if (sendable.Length == 0)
        {
            SetWarn(_legacyStatusLabel, "보낼 신형 Part가 없습니다. Convert를 먼저 실행해 주세요.");
            return;
        }

        var compCodes = sendable.Where(LegacyPartConverter.IsCompPart).ToArray();
        var mdlCodes = sendable.Except(compCodes).ToArray();
        AppendBatchInput(_batchCompInputText, compCodes);
        AppendBatchInput(_batchMdlInputText, mdlCodes);

        var skippedCount = _legacyRows.Count(row => row.Error.Length > 0 || row.Warnings.Length > 0);
        AppLog.Info(
            "LegacyConvert.SendToBatch",
            ("mdlCount", mdlCodes.Length.ToString()),
            ("compCount", compCodes.Length.ToString()),
            ("skippedCount", skippedCount.ToString()));
        SetInfo(_legacyStatusLabel,
            $"MDL 일괄 {mdlCodes.Length}건, Comp 일괄 {compCodes.Length}건을 입력에 추가했습니다. (오류·확인 필요 {skippedCount}건 제외)");
    }

    private static void AppendBatchInput(TextBox input, IReadOnlyCollection<string> partCodes)
    {
        if (partCodes.Count == 0)
        {
            return;
        }

        var current = input.Text.TrimEnd();
        input.Text = (current.Length == 0 ? string.Empty : current + Environment.NewLine) + string.Join(Environment.NewLine, partCodes);
    }

    private void ResetLegacyConvert()
    {
        _legacyInputText.Text = string.Empty;
        _legacyRows.Clear();
        SetInfo(_legacyStatusLabel, "구형→신형 변환 입력과 결과를 초기화했습니다.");
    }

    private sealed record LegacyConvertRow(
        string SourceCode,
        string ConvertedCode,
        string ErpStatus,
        string Notes,
        string Warnings,
        string Error);
}
