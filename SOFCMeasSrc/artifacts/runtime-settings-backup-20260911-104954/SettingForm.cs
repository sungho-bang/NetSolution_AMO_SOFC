using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SOFCMeas
{
    public class SettingForm : Form
    {
        private readonly List<DataGridView> grids = new List<DataGridView>();
        private Dictionary<string, string> values;
        internal Func<bool> CanSave = () => false;
        internal event EventHandler SettingsSaved;
        public SettingForm()
        {
            SuspendLayout();
            Name = "SettingForm"; Text = "SETTING";
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(232, 239, 244); ClientSize = new Size(1920, 960);
            Padding = new Padding(15);
            var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            for (int i = 0; i < 3; i++) columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
            Controls.Add(columns);
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            var save = new Button { Name = "btnSaveSettings", Text = "SAVE", Dock = DockStyle.Right, Width = 130, Font = new Font("맑은 고딕", 12F, FontStyle.Bold) };
            footer.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "SAVE 후 SPEC은 즉시 표시·판정에 적용됩니다. 시뮬레이션은 다음 START부터, 나머지 설정은 재시작 후 적용됩니다. X: 샘플 횟수 / Y: 하중(kgf).", TextAlign = ContentAlignment.MiddleLeft });
            footer.Controls.Add(save); Controls.Add(footer);
            values = SettingsCatalog.Load();
            var entries = SettingsCatalog.Entries();
            for (int i = 0; i < 3; i++)
            {
                var group = new GroupBox { Text = i == 0 ? "공통 설정" : "설비 #" + i, Dock = DockStyle.Fill, Padding = new Padding(10), Font = new Font("맑은 고딕", 11F, FontStyle.Bold) };
                var grid = new DataGridView { Name = "settingsGrid" + i, Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                    RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = Color.White, Font = new Font("맑은 고딕", 10F), SelectionMode = DataGridViewSelectionMode.CellSelect };
                grid.Columns.Add("Item", "항목"); grid.Columns.Add("Value", "값");
                grid.Columns[0].ReadOnly = true; grid.Columns[0].FillWeight = 65; grid.Columns[1].FillWeight = 35;
                grid.RowTemplate.Height = 30;
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(215, 233, 245);
                grid.DefaultCellStyle.SelectionForeColor = Color.Black;
                string prefix = "Station" + i + ".";
                foreach (var pair in values.Where(p => i == 0 ? !p.Key.StartsWith("Station1.") && !p.Key.StartsWith("Station2.") : p.Key.StartsWith(prefix))
                    .OrderBy(p => { int index = entries.FindIndex(e => e.Key == p.Key); return index < 0 ? int.MaxValue : index; }))
                {
                    var entry = entries.FirstOrDefault(e => e.Key == pair.Key);
                    int row = grid.Rows.Add(entry == null ? pair.Key : entry.Name, pair.Value);
                    grid.Rows[row].Tag = pair.Key;
                    grid.Rows[row].Cells[0].ToolTipText = pair.Key;
                    if (entry != null && entry.Type == "bool")
                        grid.Rows[row].Cells[1] = new DataGridViewComboBoxCell { DataSource = new[] { "true", "false" }, Value = pair.Value.ToLowerInvariant() };
                    if (entry != null && entry.Type.StartsWith("enum:"))
                        grid.Rows[row].Cells[1] = new DataGridViewComboBoxCell { DataSource = entry.Type.Substring(5).Split('|'), Value = pair.Value };
                }
                if (i > 0)
                {
                    var loadInfo = new Label { Dock = DockStyle.Bottom, Height = 150, Font = new Font("맑은 고딕", 9F), Padding = new Padding(4) };
                    Action refreshLoadInfo = () =>
                    {
                        var deviceRow = grid.Rows.Cast<DataGridViewRow>().First(r => (string)r.Tag == prefix + "Plc.LoadRawDevice");
                        var scaleRow = grid.Rows.Cast<DataGridViewRow>().First(r => (string)r.Tag == prefix + "Plc.LoadScale");
                        string device = Convert.ToString(deviceRow.Cells[1].Value).Trim().ToUpperInvariant();
                        uint address;
                        uint lastWordOffset = (uint)(PlcLoadDecoder.WordCount - 1);
                        string range = device.Length > 1 && device[0] == 'D' && uint.TryParse(device.Substring(1), out address) && address <= uint.MaxValue - lastWordOffset
                            ? device + " ~ D" + (address + lastWordOffset) : "시작 디바이스를 확인하세요";
                        loadInfo.Text = "하중 영역: " + range + " (5 WORD / ASCII)\r\n"
                            + "앞 WORD부터 하위 바이트 → 상위 바이트 순서로 연결\r\n"
                            + "필수 문법: STX(02) + 헤더 1 + 부호(+/-) + 숫자 + ETX(03)\r\n"
                            + "숫자 영역 6바이트: 앞 공백 허용, 부호 포함 kgf로 처리\r\n"
                            + "예: 12546, 8235, 11824, 12596, 816 → +0.410 kgf\r\n"
                            + "문법 오류: 샘플 폐기 · 알람 로그 기록 · 수집 계속\r\n"
                            + "내부·시뮬레이션 원시값 = kgf × " + Convert.ToString(scaleRow.Cells[1].Value);
                    };
                    grid.CellValueChanged += (sender, args) => refreshLoadInfo();
                    refreshLoadInfo();
                    group.Controls.Add(loadInfo);
                }
                grids.Add(grid); group.Controls.Add(grid); columns.Controls.Add(group, i, 0);
                grid.BringToFront();
                grid.CellFormatting += delegate(object sender, DataGridViewCellFormattingEventArgs e)
                {
                    if (e.RowIndex >= 0 && e.ColumnIndex == 1 && ((string)grid.Rows[e.RowIndex].Tag).EndsWith("Password"))
                    { e.Value = string.IsNullOrEmpty(Convert.ToString(e.Value)) ? "" : "••••••"; e.FormattingApplied = true; }
                };
                grid.EditingControlShowing += delegate(object sender, DataGridViewEditingControlShowingEventArgs e)
                {
                    var editor = e.Control as TextBox;
                    if (editor != null) editor.UseSystemPasswordChar = ((string)grid.CurrentRow.Tag).EndsWith("Password");
                };
            }
            save.Click += delegate
            {
                try { SaveValues(); MessageBox.Show(this, "설정을 저장했습니다. SPEC은 즉시 표시와 판정에 적용됩니다. 시뮬레이션은 다음 START부터, 나머지 설정은 재시작 후 적용됩니다.", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "저장 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            ResumeLayout(true);
        }
        internal void SaveValues()
        {
            if (!CanSave()) throw new UnauthorizedAccessException("ADMINISTRATOR 권한이 필요합니다.");
            var updated = new Dictionary<string, string>(values);
            foreach (var grid in grids)
            {
                if (!grid.EndEdit()) throw new ArgumentException("편집 중인 값을 확인하세요.");
                foreach (DataGridViewRow row in grid.Rows) updated[(string)row.Tag] = Convert.ToString(row.Cells[1].Value).Trim();
            }
            SettingsCatalog.Validate(updated);
            ApplicationConfiguration.SaveAppSettings(updated, true);
            values = updated;
            EventHandler handler = SettingsSaved;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}

