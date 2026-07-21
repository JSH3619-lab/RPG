using System.ComponentModel;
using RamosPartGenerator.Core.Models;

namespace RamosPartGenerator.Desktop;

public sealed partial class MainForm
{
    private readonly BindingList<BatchInputPreviewRow> _batchCompAnalysisRows = new();
    private readonly BindingList<GeneratedPartRow> _batchCompRows = new();

    private TextBox _batchCompInputText = null!;
    private CheckBox _batchIncludeCompMdl = null!;
    private ComboBox _batchCompSpeed = null!;
    private DataGridView _batchCompAnalysisGrid = null!;
    private DataGridView _batchCompResultGrid = null!;
    private Label _batchCompStatusLabel = null!;
    private bool _updatingCompBatch;

    private TabPage BuildBatchCompPage()
    {
        var tab = new TabPage("Comp 일괄") { BackColor = RamosTheme.Surface };
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(8),
            BackColor = RamosTheme.Surface
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        page.Controls.Add(BuildBatchCompInputArea(), 0, 0);
        _batchCompStatusLabel = BuildStatusLabel();
        page.Controls.Add(_batchCompStatusLabel, 0, 1);

        _batchCompAnalysisGrid = BuildBatchPreviewGrid(_batchCompAnalysisRows);
        page.Controls.Add(BuildBatchGridGroup("입력 분석", _batchCompAnalysisGrid), 0, 2);

        _batchCompResultGrid = BuildResultGrid(_batchCompRows);
        page.Controls.Add(BuildBatchGridGroup("생성 결과", _batchCompResultGrid), 0, 3);

        tab.Controls.Add(page);
        return tab;
    }

    private Control BuildBatchCompInputArea()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = RamosTheme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        layout.Controls.Add(BuildBatchCompInputGroup(), 0, 0);
        layout.Controls.Add(BuildBatchCompOptionsGroup(), 1, 0);
        return layout;
    }

    private Control BuildBatchCompInputGroup()
    {
        var group = BuildBatchGroup("Comp Full Part (한 줄에 하나)");
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

        _batchCompInputText = new TextBox
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
        _batchCompInputText.TextChanged += (_, _) => InvalidateBatchCompAnalysis();

        var buttons = BuildButtonFlow();
        buttons.Controls.Add(BuildButton("Generate", GenerateBatchComp));
        buttons.Controls.Add(BuildButton("Export Excel", ExportBatchComp));
        buttons.Controls.Add(BuildButton("Delete Selected", DeleteSelectedBatchComp));
        buttons.Controls.Add(BuildButton("Reset", ResetBatchComp));

        layout.Controls.Add(_batchCompInputText, 0, 0);
        layout.Controls.Add(buttons, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private Control BuildBatchCompOptionsGroup()
    {
        var group = BuildBatchGroup("생성 항목");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12),
            BackColor = RamosTheme.Panel
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Incoming + Comp + Comp BIN (항상 생성)",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = RamosTheme.Text
        }, 0, 0);

        _batchIncludeCompMdl = new CheckBox
        {
            AutoSize = true,
            Text = "Comp_MDL 생성",
            ForeColor = RamosTheme.Text,
            Margin = new Padding(6, 4, 12, 4)
        };
        _batchIncludeCompMdl.CheckedChanged += (_, _) =>
        {
            _batchCompSpeed.Enabled = _batchIncludeCompMdl.Checked;
            InvalidateBatchCompAnalysis();
        };
        layout.Controls.Add(_batchIncludeCompMdl, 0, 1);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Comp_MDL Speed",
            TextAlign = ContentAlignment.BottomLeft,
            ForeColor = RamosTheme.Text
        }, 0, 2);

        _batchCompSpeed = new ComboBox
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Enabled = false
        };
        _batchCompSpeed.Items.AddRange(ModuleOptions("speedCode").Cast<object>().ToArray());
        _batchCompSpeed.SelectedIndexChanged += (_, _) => InvalidateBatchCompAnalysis();
        layout.Controls.Add(_batchCompSpeed, 0, 3);

        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Speed 누락/불일치 시 Comp_MDL만 실패 처리됩니다.",
            ForeColor = RamosTheme.Gray,
            Padding = new Padding(0, 8, 0, 0)
        }, 0, 4);

        group.Controls.Add(layout);
        return group;
    }

    private void GenerateBatchComp()
    {
        RunGuarded(_batchCompStatusLabel, "BatchComp.Generate", () =>
        {
            var result = CreateBatchCompResult();
            ShowBatchCompAnalysis(result);
            if (result.Rows.Count == 0)
            {
                throw new InvalidOperationException("생성 가능한 결과가 없습니다. 입력 분석 메시지를 확인해 주세요.");
            }

            _batchCompRows.Clear();
            foreach (var row in result.Rows)
            {
                _batchCompRows.Add(row);
            }

            AppLog.Info(
                "BatchComp.Generate.Success",
                ("inputCount", result.Items.Count.ToString()),
                ("rowCount", _batchCompRows.Count.ToString()),
                ("duplicateCount", result.DuplicateCount.ToString()),
                ("includeCompMdl", _batchIncludeCompMdl.Checked.ToString()),
                ("speedCode", DisplayHelpers.ExtractCode(_batchCompSpeed.Text)),
                ("firstPartCode", FirstPartCode(_batchCompRows)));
            SetBatchCompStatus(result, $"Generated {_batchCompRows.Count} batch rows.");
        });
    }

    private BatchGenerationResult CreateBatchCompResult()
    {
        var partCodes = _batchCompInputText.Lines
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        if (partCodes.Length == 0)
        {
            throw new InvalidOperationException("Comp Full Part를 한 줄에 하나씩 입력해 주세요.");
        }

        return _services.Batch.GenerateFromCompParts(partCodes, new CompBatchOptions
        {
            IncludeCompMdl = _batchIncludeCompMdl.Checked,
            SpeedCode = DisplayHelpers.ExtractCode(_batchCompSpeed.Text)
        });
    }

    private void ShowBatchCompAnalysis(BatchGenerationResult result)
    {
        _batchCompAnalysisRows.Clear();
        foreach (var item in result.Items)
        {
            _batchCompAnalysisRows.Add(new BatchInputPreviewRow(
                item.InputPartCode,
                FormatDetectedInputKind(item.DetectedInputKind),
                FormatBatchStatus(item.Status),
                item.Rows.Count,
                string.Join(" | ", item.Messages),
                item.Status));
        }

        SetBatchCompStatus(result, "입력 분석이 완료되었습니다.");
    }

    private void SetBatchCompStatus(BatchGenerationResult result, string prefix)
    {
        var successCount = result.Items.Count(item => item.Status == BatchItemStatus.Success);
        var partialCount = result.Items.Count(item => item.Status == BatchItemStatus.PartialSuccess);
        var failedCount = result.Items.Count(item => item.Status == BatchItemStatus.Failed);
        _batchCompStatusLabel.ForeColor = failedCount > 0 || partialCount > 0 ? RamosTheme.Danger : RamosTheme.Blue;
        _batchCompStatusLabel.Text =
            $"{prefix} 입력 {result.Items.Count} | 정상 {successCount} | 일부 성공 {partialCount} | 실패 {failedCount} | 중복 제외 {result.DuplicateCount} | 결과 {result.Rows.Count}";
    }

    private void ExportBatchComp()
    {
        ExportRows(_batchCompStatusLabel, "BatchComp", _batchCompRows.ToArray());
    }

    private void DeleteSelectedBatchComp()
    {
        var deletedRows = DeleteSelectedRows(_batchCompResultGrid, _batchCompRows);
        AppLog.Info(
            "BatchComp.DeleteSelected",
            ("deletedRows", deletedRows.ToString()),
            ("totalRows", _batchCompRows.Count.ToString()));
        SetInfo(_batchCompStatusLabel, deletedRows == 0
            ? "삭제할 결과 셀을 선택해 주세요."
            : $"Deleted {deletedRows} selected batch rows.");
    }

    private void ResetBatchComp()
    {
        _updatingCompBatch = true;
        try
        {
            _batchCompInputText.Text = string.Empty;
            _batchIncludeCompMdl.Checked = false;
            _batchCompSpeed.SelectedIndex = -1;
            _batchCompSpeed.Enabled = false;
            _batchCompAnalysisRows.Clear();
            _batchCompRows.Clear();
        }
        finally
        {
            _updatingCompBatch = false;
        }

        SetInfo(_batchCompStatusLabel, "Comp 일괄 입력과 결과를 초기화했습니다.");
    }

    private void InvalidateBatchCompAnalysis()
    {
        if (_updatingCompBatch)
        {
            return;
        }

        _batchCompAnalysisRows.Clear();
        SetInfo(_batchCompStatusLabel, _batchCompRows.Count == 0
            ? "입력 또는 선택이 변경되었습니다. Generate를 실행해 주세요."
            : "입력 또는 선택이 변경되었습니다. 기존 생성 결과는 유지됩니다.");
    }
}
