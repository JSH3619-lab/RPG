using System.ComponentModel;
using RamosPartGenerator.Core.Models;

namespace RamosPartGenerator.Desktop;

public sealed partial class MainForm
{
    private readonly BindingList<BatchInputPreviewRow> _batchPreviewRows = new();
    private readonly BindingList<GeneratedPartRow> _batchRows = new();

    private TextBox _batchMdlInputText = null!;
    private CheckBox _batchBasePid = null!;
    private CheckBox _batchBaseMfgId = null!;
    private CheckBox _batchReball = null!;
    private CheckBox _batchFirstRepair = null!;
    private CheckBox _batchSecondRepair = null!;
    private CheckBox _batchReballRepair = null!;
    private CheckBox _batchFinishedProductRetest = null!;
    private CheckBox _batchOriginalCompRelated = null!;
    private CheckBox _batchReballCompRelated = null!;
    private DataGridView _batchPreviewGrid = null!;
    private DataGridView _batchResultGrid = null!;
    private Label _batchStatusLabel = null!;
    private bool _updatingBatch;

    private TabPage BuildBatchPage()
    {
        var tab = new TabPage("Batch Generate") { BackColor = RamosTheme.Surface };
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(8),
            BackColor = RamosTheme.Surface
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 270F));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        page.Controls.Add(BuildBatchMdlInputArea(), 0, 0);
        _batchStatusLabel = BuildStatusLabel();
        page.Controls.Add(_batchStatusLabel, 0, 1);

        _batchPreviewGrid = BuildBatchPreviewGrid();
        page.Controls.Add(BuildBatchGridGroup("입력 분석", _batchPreviewGrid), 0, 2);

        _batchResultGrid = BuildResultGrid(_batchRows);
        page.Controls.Add(BuildBatchGridGroup("생성 결과", _batchResultGrid), 0, 3);

        tab.Controls.Add(page);
        return tab;
    }

    private Control BuildBatchMdlInputArea()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = RamosTheme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));

        layout.Controls.Add(BuildBatchInputGroup(), 0, 0);
        layout.Controls.Add(BuildBatchOptionsPanel(), 1, 0);
        return layout;
    }

    private Control BuildBatchInputGroup()
    {
        var group = BuildBatchGroup("MDL Full Part (한 줄에 하나)");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(6),
            BackColor = RamosTheme.Panel
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));

        _batchMdlInputText = new TextBox
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
        _batchMdlInputText.TextChanged += (_, _) => InvalidateBatchPreview();

        var buttons = BuildButtonFlow();
        buttons.Controls.Add(BuildButton("Generate", GenerateBatchMdl));
        buttons.Controls.Add(BuildButton("Export Excel", ExportBatchMdl));
        buttons.Controls.Add(BuildButton("Delete Selected", DeleteSelectedBatchMdl));
        buttons.Controls.Add(BuildButton("Reset", ResetBatchMdl));

        layout.Controls.Add(_batchMdlInputText, 0, 0);
        layout.Controls.Add(buttons, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private Control BuildBatchOptionsPanel()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8, 0, 0, 0),
            BackColor = RamosTheme.Surface
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 27F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 43F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

        _batchBasePid = BuildBatchCheckBox("기본 PID");
        _batchBaseMfgId = BuildBatchCheckBox("기본 MFGID");
        layout.Controls.Add(BuildBatchOptionGroup("기본 Part", _batchBasePid, _batchBaseMfgId), 0, 0);

        _batchReball = BuildBatchCheckBox("Reball");
        _batchFirstRepair = BuildBatchCheckBox("1차 Repair");
        _batchSecondRepair = BuildBatchCheckBox("2차 Repair");
        _batchReballRepair = BuildBatchCheckBox("Reball Repair");
        _batchFinishedProductRetest = BuildBatchCheckBox("완제품 Retest (00/0Y)");
        layout.Controls.Add(BuildBatchOptionGroup(
            "작업 Part",
            _batchReball,
            _batchFirstRepair,
            _batchSecondRepair,
            _batchReballRepair,
            _batchFinishedProductRetest), 0, 1);

        _batchOriginalCompRelated = BuildBatchCheckBox("원본 Comp 관련");
        _batchReballCompRelated = BuildBatchCheckBox("Reball Comp 관련");
        layout.Controls.Add(BuildBatchOptionGroup(
            "Comp 관련",
            _batchOriginalCompRelated,
            _batchReballCompRelated), 0, 2);

        return layout;
    }

    private static GroupBox BuildBatchGroup(string text)
    {
        return new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = text,
            ForeColor = RamosTheme.BlueDark,
            BackColor = RamosTheme.Panel,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 0, 6)
        };
    }

    private static Control BuildBatchOptionGroup(string text, params CheckBox[] checkBoxes)
    {
        var group = BuildBatchGroup(text);
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(4, 5, 4, 2),
            BackColor = RamosTheme.Panel
        };
        flow.Controls.AddRange(checkBoxes);
        group.Controls.Add(flow);
        return group;
    }

    private CheckBox BuildBatchCheckBox(string text)
    {
        var checkBox = new CheckBox
        {
            AutoSize = true,
            Text = text,
            ForeColor = RamosTheme.Text,
            Margin = new Padding(6, 4, 12, 4)
        };
        checkBox.CheckedChanged += (_, _) => InvalidateBatchPreview();
        return checkBox;
    }

    private static Control BuildBatchGridGroup(string text, DataGridView grid)
    {
        var group = BuildBatchGroup(text);
        grid.Margin = new Padding(4);
        group.Controls.Add(grid);
        return group;
    }

    private DataGridView BuildBatchPreviewGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            DataSource = _batchPreviewRows,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
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
        grid.Columns.Add(BuildTextColumn(nameof(BatchInputPreviewRow.InputPartCode), "입력 Full Part", 210));
        grid.Columns.Add(BuildTextColumn(nameof(BatchInputPreviewRow.DetectedInputKind), "감지 상태", 90));
        grid.Columns.Add(BuildTextColumn(nameof(BatchInputPreviewRow.Status), "처리 상태", 80));
        grid.Columns.Add(BuildTextColumn(nameof(BatchInputPreviewRow.GeneratedCount), "생성 수", 60));
        grid.Columns.Add(BuildTextColumn(nameof(BatchInputPreviewRow.Message), "메시지", 260));
        grid.CellFormatting += (_, args) =>
        {
            if (args.RowIndex < 0 || grid.Rows[args.RowIndex].DataBoundItem is not BatchInputPreviewRow row)
            {
                return;
            }

            if (row.StatusCode != BatchItemStatus.Success)
            {
                grid.Rows[args.RowIndex].DefaultCellStyle.ForeColor = RamosTheme.Danger;
            }
        };
        return grid;
    }

    private void GenerateBatchMdl()
    {
        RunGuarded(_batchStatusLabel, "BatchMdl.Generate", () =>
        {
            var result = CreateBatchPreview();
            ShowBatchPreview(result);
            if (result.Rows.Count == 0)
            {
                throw new InvalidOperationException("생성 가능한 결과가 없습니다. 입력 분석 메시지를 확인해 주세요.");
            }

            _batchRows.Clear();
            foreach (var row in result.Rows)
            {
                _batchRows.Add(row);
            }

            AppLog.Info(
                "BatchMdl.Generate.Success",
                ("inputCount", result.Items.Count.ToString()),
                ("rowCount", _batchRows.Count.ToString()),
                ("duplicateCount", result.DuplicateCount.ToString()),
                ("firstPartCode", FirstPartCode(_batchRows)));
            SetBatchStatus(result, $"Generated {_batchRows.Count} batch rows.");
        });
    }

    private BatchGenerationResult CreateBatchPreview()
    {
        var partCodes = _batchMdlInputText.Lines
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        if (partCodes.Length == 0)
        {
            throw new InvalidOperationException("MDL Full Part를 한 줄에 하나씩 입력해 주세요.");
        }

        return _services.Batch.GenerateFromModuleParts(partCodes, ReadBatchMdlOptions());
    }

    private MdlBatchOptions ReadBatchMdlOptions()
    {
        return new MdlBatchOptions
        {
            IncludeBasePid = _batchBasePid.Checked,
            IncludeBaseMfgId = _batchBaseMfgId.Checked,
            IncludeReball = _batchReball.Checked,
            IncludeFirstRepair = _batchFirstRepair.Checked,
            IncludeSecondRepair = _batchSecondRepair.Checked,
            IncludeReballRepair = _batchReballRepair.Checked,
            IncludeFinishedProductRetest = _batchFinishedProductRetest.Checked,
            IncludeOriginalCompRelated = _batchOriginalCompRelated.Checked,
            IncludeReballCompRelated = _batchReballCompRelated.Checked
        };
    }

    private void ShowBatchPreview(BatchGenerationResult result)
    {
        _batchPreviewRows.Clear();
        foreach (var item in result.Items)
        {
            _batchPreviewRows.Add(new BatchInputPreviewRow(
                item.InputPartCode,
                FormatDetectedInputKind(item.DetectedInputKind),
                FormatBatchStatus(item.Status),
                item.Rows.Count,
                string.Join(" | ", item.Messages),
                item.Status));
        }

        SetBatchStatus(result, "입력 분석이 완료되었습니다.");
    }

    private void SetBatchStatus(BatchGenerationResult result, string prefix)
    {
        var successCount = result.Items.Count(item => item.Status == BatchItemStatus.Success);
        var partialCount = result.Items.Count(item => item.Status == BatchItemStatus.PartialSuccess);
        var failedCount = result.Items.Count(item => item.Status == BatchItemStatus.Failed);
        _batchStatusLabel.ForeColor = failedCount > 0 || partialCount > 0 ? RamosTheme.Danger : RamosTheme.Blue;
        _batchStatusLabel.Text =
            $"{prefix} 입력 {result.Items.Count} | 정상 {successCount} | 일부 성공 {partialCount} | 실패 {failedCount} | 중복 제외 {result.DuplicateCount} | 예상 결과 {result.Rows.Count}";
    }

    private void ExportBatchMdl()
    {
        ExportRows(_batchStatusLabel, "BatchMdl", _batchRows.ToArray());
    }

    private void DeleteSelectedBatchMdl()
    {
        var deletedRows = DeleteSelectedRows(_batchResultGrid, _batchRows);
        AppLog.Info(
            "BatchMdl.DeleteSelected",
            ("deletedRows", deletedRows.ToString()),
            ("totalRows", _batchRows.Count.ToString()));
        SetInfo(_batchStatusLabel, deletedRows == 0
            ? "삭제할 결과 셀을 선택해 주세요."
            : $"Deleted {deletedRows} selected batch rows.");
    }

    private void ResetBatchMdl()
    {
        _updatingBatch = true;
        try
        {
            _batchMdlInputText.Text = string.Empty;
            foreach (var checkBox in BatchMdlCheckBoxes())
            {
                checkBox.Checked = false;
            }

            _batchPreviewRows.Clear();
            _batchRows.Clear();
        }
        finally
        {
            _updatingBatch = false;
        }

        SetInfo(_batchStatusLabel, "MDL 일괄 입력과 결과를 초기화했습니다.");
    }

    private IEnumerable<CheckBox> BatchMdlCheckBoxes()
    {
        yield return _batchBasePid;
        yield return _batchBaseMfgId;
        yield return _batchReball;
        yield return _batchFirstRepair;
        yield return _batchSecondRepair;
        yield return _batchReballRepair;
        yield return _batchFinishedProductRetest;
        yield return _batchOriginalCompRelated;
        yield return _batchReballCompRelated;
    }

    private void InvalidateBatchPreview()
    {
        if (_updatingBatch)
        {
            return;
        }

        _batchPreviewRows.Clear();
        SetInfo(_batchStatusLabel, _batchRows.Count == 0
            ? "입력 또는 선택이 변경되었습니다. Generate를 실행해 주세요."
            : "입력 또는 선택이 변경되었습니다. 기존 생성 결과는 유지됩니다.");
    }

    private static string FormatDetectedInputKind(ModuleBatchInputKind kind)
    {
        return kind switch
        {
            ModuleBatchInputKind.Normal => "일반",
            ModuleBatchInputKind.Reball => "Reball",
            ModuleBatchInputKind.FirstRepair => "1차 Repair",
            ModuleBatchInputKind.SecondRepair => "2차 Repair",
            ModuleBatchInputKind.ReballRepair => "Reball Repair",
            ModuleBatchInputKind.SharedDummy => "공용 00 Dummy",
            ModuleBatchInputKind.SecondRepairDummy => "2차 Dummy",
            ModuleBatchInputKind.ReballRepairDummy => "Reball Repair Dummy",
            ModuleBatchInputKind.FinishedProductRetest => "완제품 Retest",
            _ => kind.ToString()
        };
    }

    private static string FormatBatchStatus(BatchItemStatus status)
    {
        return status switch
        {
            BatchItemStatus.Success => "정상",
            BatchItemStatus.PartialSuccess => "일부 성공",
            BatchItemStatus.Failed => "실패",
            _ => status.ToString()
        };
    }

    private sealed record BatchInputPreviewRow(
        string InputPartCode,
        string DetectedInputKind,
        string Status,
        int GeneratedCount,
        string Message,
        BatchItemStatus StatusCode);
}
