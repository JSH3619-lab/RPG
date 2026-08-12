using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Desktop;

/// <summary>
/// 필드 코드 옵션(code_options)을 추가/수정/삭제하고 저장 시 스냅샷을 남긴다. 저장/복원 후 SpecChanged로 호출측에 알린다.
/// </summary>
internal sealed class SpecEditorForm : Form
{
    private static readonly Color Blue = Color.FromArgb(18, 55, 142);
    private static readonly Color BlueDark = Color.FromArgb(8, 38, 104);
    private static readonly Color Border = Color.FromArgb(205, 213, 226);
    private static readonly Color Surface = Color.FromArgb(246, 248, 252);

    private readonly SpecEditService _editService;
    private readonly ComboBox _optionSetCombo = new();
    private readonly DataGridView _grid = new();
    private readonly Label _statusLabel = new();

    public event EventHandler? SpecChanged;

    public SpecEditorForm(SpecEditService editService)
    {
        _editService = editService;
        BuildUi();
        LoadOptionSets();
    }

    private void BuildUi()
    {
        Text = "스펙 코드 옵션 편집";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(560, 520);
        Size = new Size(620, 560);
        BackColor = Surface;
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

        var pickerRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        pickerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        pickerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pickerRow.Controls.Add(new Label
        {
            Text = "옵션셋",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = BlueDark
        }, 0, 0);
        _optionSetCombo.Dock = DockStyle.Fill;
        _optionSetCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _optionSetCombo.SelectedIndexChanged += (_, _) => LoadOptionsIntoGrid();
        pickerRow.Controls.Add(_optionSetCombo, 1, 0);
        root.Controls.Add(pickerRow, 0, 0);

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = true;
        _grid.AllowUserToDeleteRows = true;
        _grid.RowHeadersVisible = false;
        _grid.BackgroundColor = Color.White;
        _grid.GridColor = Border;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Blue;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "code",
            HeaderText = "코드",
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "description",
            HeaderText = "설명",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        root.Controls.Add(_grid, 0, 1);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = BlueDark;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 2);

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        buttonRow.Controls.Add(BuildButton("닫기", (_, _) => Close(), primary: false));
        buttonRow.Controls.Add(BuildButton("백업에서 복원…", (_, _) => RestoreFromBackup(), primary: false));
        buttonRow.Controls.Add(BuildButton("저장 (스냅샷 자동)", (_, _) => Save(), primary: true));
        root.Controls.Add(buttonRow, 0, 3);

        Controls.Add(root);
    }

    private Button BuildButton(string text, EventHandler onClick, bool primary)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(6, 6, 0, 6),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Blue : Color.White,
            ForeColor = primary ? Color.White : BlueDark
        };
        button.FlatAppearance.BorderColor = Border;
        button.Click += onClick;
        return button;
    }

    private void LoadOptionSets()
    {
        _optionSetCombo.Items.Clear();
        foreach (var key in _editService.ListOptionSetKeys())
        {
            _optionSetCombo.Items.Add(key);
        }

        if (_optionSetCombo.Items.Count > 0)
        {
            _optionSetCombo.SelectedIndex = 0;
        }
    }

    private void LoadOptionsIntoGrid()
    {
        _grid.Rows.Clear();
        if (_optionSetCombo.SelectedItem is not string key)
        {
            return;
        }

        foreach (var option in _editService.GetOptions(key))
        {
            var separatorIndex = option.IndexOf(" - ", StringComparison.Ordinal);
            var code = separatorIndex > 0 ? option[..separatorIndex] : option;
            var description = separatorIndex > 0 ? option[(separatorIndex + 3)..] : string.Empty;
            _grid.Rows.Add(code, description);
        }

        SetStatus($"{key}: {_grid.Rows.Count - 1}개 코드", isError: false);
    }

    private void Save()
    {
        if (_optionSetCombo.SelectedItem is not string key)
        {
            return;
        }

        var options = new List<string>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var code = Convert.ToString(row.Cells["code"].Value)?.Trim() ?? string.Empty;
            var description = Convert.ToString(row.Cells["description"].Value)?.Trim() ?? string.Empty;
            if (code.Length == 0 && description.Length == 0)
            {
                continue;
            }

            options.Add($"{code} - {description}");
        }

        try
        {
            _editService.SaveOptions(key, options);
            SetStatus($"저장됨: {key} ({options.Count}개). 스냅샷 생성됨.", isError: false);
            SpecChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void RestoreFromBackup()
    {
        var backups = _editService.ListBackups();
        if (backups.Count == 0)
        {
            SetStatus("복원할 백업이 없습니다.", isError: true);
            return;
        }

        using var picker = new BackupPickerForm(backups);
        if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedBackupId is null)
        {
            return;
        }

        try
        {
            _editService.RestoreBackup(picker.SelectedBackupId);
            LoadOptionsIntoGrid();
            SetStatus($"복원됨: {picker.SelectedBackupId}", isError: false);
            SpecChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        _statusLabel.ForeColor = isError ? Color.FromArgb(169, 45, 45) : BlueDark;
        _statusLabel.Text = message;
    }

    private sealed class BackupPickerForm : Form
    {
        private readonly ListBox _list = new();

        public string? SelectedBackupId { get; private set; }

        public BackupPickerForm(IReadOnlyList<SpecBackup> backups)
        {
            Text = "백업에서 복원";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(340, 400);
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            _list.Dock = DockStyle.Fill;
            _list.IntegralHeight = false;
            foreach (var backup in backups)
            {
                var label = backup.Timestamp == DateTime.MinValue
                    ? backup.Id
                    : backup.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                _list.Items.Add(new BackupItem(backup.Id, label));
            }
            _list.DoubleClick += (_, _) => Confirm();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(8)
            };
            var restore = new Button { Text = "복원", AutoSize = true, DialogResult = DialogResult.None };
            restore.Click += (_, _) => Confirm();
            var cancel = new Button { Text = "취소", AutoSize = true, DialogResult = DialogResult.Cancel };
            buttons.Controls.Add(restore);
            buttons.Controls.Add(cancel);

            Controls.Add(_list);
            Controls.Add(buttons);
        }

        private void Confirm()
        {
            if (_list.SelectedItem is BackupItem item)
            {
                SelectedBackupId = item.Id;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private sealed record BackupItem(string Id, string Label)
        {
            public override string ToString() => Label;
        }
    }
}
