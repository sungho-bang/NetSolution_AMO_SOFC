namespace SOFCMeas
{
    partial class SOFCMeas
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SOFCMeas));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.pnlTitle = new Krypton.Toolkit.KryptonPanel();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnMinimize = new System.Windows.Forms.Button();
            this.lblUser = new System.Windows.Forms.Label();
            this.lblHb2Status = new System.Windows.Forms.Label();
            this.lblHb1Status = new System.Windows.Forms.Label();
            this.btnLogView = new Krypton.Toolkit.KryptonButton();
            this.btnMainView = new Krypton.Toolkit.KryptonButton();
            this.btnSettingView = new Krypton.Toolkit.KryptonButton();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlMain = new Krypton.Toolkit.KryptonPanel();
            this.stationView2 = new global::SOFCMeas.StationView();
            this.stationView1 = new global::SOFCMeas.StationView();
            this.pnlLog = new Krypton.Toolkit.KryptonPanel();
            this.lblLogSummary = new System.Windows.Forms.Label();
            this.dgvLogs = new System.Windows.Forms.DataGridView();
            this.colLogTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLogStation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLogLot = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLogModel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLogResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLogLoad = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLogMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlLogFilter = new System.Windows.Forms.Panel();
            this.lblLogPolicyStatus = new System.Windows.Forms.Label();
            this.btnApplyLogPolicy = new Krypton.Toolkit.KryptonButton();
            this.numLogRetentionDays = new System.Windows.Forms.NumericUpDown();
            this.lblLogRetentionDays = new System.Windows.Forms.Label();
            this.chkLogAutoDelete = new System.Windows.Forms.CheckBox();
            this.lblLogStoragePath = new System.Windows.Forms.Label();
            this.btnExportLog = new Krypton.Toolkit.KryptonButton();
            this.btnSearchLog = new Krypton.Toolkit.KryptonButton();
            this.cboLogStation = new System.Windows.Forms.ComboBox();
            this.lblLogStation = new System.Windows.Forms.Label();
            this.dtpLogTo = new System.Windows.Forms.DateTimePicker();
            this.lblDateSeparator = new System.Windows.Forms.Label();
            this.dtpLogFrom = new System.Windows.Forms.DateTimePicker();
            this.lblLogPeriod = new System.Windows.Forms.Label();
            this.lblLogTitle = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pnlTitle)).BeginInit();
            this.pnlTitle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pnlMain)).BeginInit();
            this.pnlMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pnlLog)).BeginInit();
            this.pnlLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLogs)).BeginInit();
            this.pnlLogFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLogRetentionDays)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlTitle
            // 
            this.pnlTitle.Controls.Add(this.btnExit);
            this.pnlTitle.Controls.Add(this.btnMinimize);
            this.pnlTitle.Controls.Add(this.lblUser);
            this.pnlTitle.Controls.Add(this.lblHb2Status);
            this.pnlTitle.Controls.Add(this.lblHb1Status);
            this.pnlTitle.Controls.Add(this.btnLogView);
            this.pnlTitle.Controls.Add(this.btnMainView);
            this.pnlTitle.Controls.Add(this.btnSettingView);
            this.pnlTitle.Controls.Add(this.lblTitle);
            this.pnlTitle.Location = new System.Drawing.Point(0, 0);
            this.pnlTitle.Name = "pnlTitle";
            this.pnlTitle.Size = new System.Drawing.Size(1920, 80);
            this.pnlTitle.StateCommon.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(45)))), ((int)(((byte)(60)))));
            this.pnlTitle.StateCommon.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(45)))), ((int)(((byte)(60)))));
            this.pnlTitle.TabIndex = 0;
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(1852, 10);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(48, 60);
            this.btnExit.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnExit.Text = "×";
            this.btnExit.AccessibleName = "종료";
            this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExit.BackColor = System.Drawing.Color.FromArgb(54, 65, 82);
            this.btnExit.ForeColor = System.Drawing.Color.White;
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(104, 121, 139);
            this.btnExit.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(190, 55, 65);
            this.btnExit.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(150, 40, 50);
            this.btnExit.TabStop = false;
            this.btnMinimize.Location = new System.Drawing.Point(1798, 10);
            this.btnMinimize.Name = "btnMinimize";
            this.btnMinimize.Size = new System.Drawing.Size(48, 60);
            this.btnMinimize.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnMinimize.Text = "—";
            this.btnMinimize.AccessibleName = "최소화";
            this.btnMinimize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMinimize.BackColor = System.Drawing.Color.FromArgb(54, 65, 82);
            this.btnMinimize.ForeColor = System.Drawing.Color.White;
            this.btnMinimize.UseVisualStyleBackColor = false;
            this.btnMinimize.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(104, 121, 139);
            this.btnMinimize.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(66, 81, 100);
            this.btnMinimize.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(42, 52, 68);
            this.btnMinimize.TabStop = false;
            // 
            // lblUser
            // 
            this.lblUser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(123)))), ((int)(((byte)(132)))));
            this.lblUser.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.lblUser.ForeColor = System.Drawing.Color.White;
            this.lblUser.Location = new System.Drawing.Point(1618, 16);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(168, 48);
            this.lblUser.TabIndex = 5;
            this.lblUser.Text = "OPERATOR";
            this.lblUser.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblHb2Status
            // 
            this.lblHb2Status.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(112)))), ((int)(((byte)(128)))));
            this.lblHb2Status.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.lblHb2Status.ForeColor = System.Drawing.Color.White;
            this.lblHb2Status.Location = new System.Drawing.Point(1436, 16);
            this.lblHb2Status.Name = "lblHb2Status";
            this.lblHb2Status.Size = new System.Drawing.Size(168, 48);
            this.lblHb2Status.TabIndex = 4;
            this.lblHb2Status.Text = "#2 연결 대기";
            this.lblHb2Status.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblHb1Status
            // 
            this.lblHb1Status.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(112)))), ((int)(((byte)(128)))));
            this.lblHb1Status.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.lblHb1Status.ForeColor = System.Drawing.Color.White;
            this.lblHb1Status.Location = new System.Drawing.Point(1254, 16);
            this.lblHb1Status.Name = "lblHb1Status";
            this.lblHb1Status.Size = new System.Drawing.Size(168, 48);
            this.lblHb1Status.TabIndex = 3;
            this.lblHb1Status.Text = "#1 연결 대기";
            this.lblHb1Status.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnLogView
            // 
            this.btnLogView.Location = new System.Drawing.Point(642, 13);
            this.btnLogView.Name = "btnLogView";
            this.btnLogView.Size = new System.Drawing.Size(170, 54);
            this.btnLogView.StateCommon.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(45)))), ((int)(((byte)(60)))));
            this.btnLogView.StateCommon.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(45)))), ((int)(((byte)(60)))));
            this.btnLogView.StateCommon.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(84)))), ((int)(((byte)(101)))), ((int)(((byte)(122)))));
            this.btnLogView.StateCommon.Border.DrawBorders = ((Krypton.Toolkit.PaletteDrawBorders)((((Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom) 
            | Krypton.Toolkit.PaletteDrawBorders.Left) 
            | Krypton.Toolkit.PaletteDrawBorders.Right)));
            this.btnLogView.StateCommon.Border.Rounding = 5F;
            this.btnLogView.StateCommon.Content.ShortText.Color1 = System.Drawing.Color.White;
            this.btnLogView.StateCommon.Content.ShortText.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.btnLogView.TabIndex = 2;
            this.btnLogView.Values.DropDownArrowColor = System.Drawing.Color.Empty;
            this.btnLogView.Values.Text = "LOG VIEW";
            // 
            // btnMainView
            // 
            this.btnMainView.Location = new System.Drawing.Point(456, 13);
            this.btnMainView.Name = "btnMainView";
            this.btnMainView.Size = new System.Drawing.Size(170, 54);
            this.btnMainView.StateCommon.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(90)))));
            this.btnMainView.StateCommon.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(180)))), ((int)(((byte)(90)))));
            this.btnMainView.StateCommon.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(211)))), ((int)(((byte)(123)))));
            this.btnMainView.StateCommon.Border.DrawBorders = ((Krypton.Toolkit.PaletteDrawBorders)((((Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom) 
            | Krypton.Toolkit.PaletteDrawBorders.Left) 
            | Krypton.Toolkit.PaletteDrawBorders.Right)));
            this.btnMainView.StateCommon.Border.Rounding = 5F;
            this.btnMainView.StateCommon.Content.ShortText.Color1 = System.Drawing.Color.White;
            this.btnMainView.StateCommon.Content.ShortText.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.btnMainView.TabIndex = 1;
            this.btnMainView.Values.DropDownArrowColor = System.Drawing.Color.Empty;
            this.btnMainView.Values.Text = "MAIN";
            // 
            // btnSettingView
            // 
            this.btnSettingView.Location = new System.Drawing.Point(828, 13);
            this.btnSettingView.Name = "btnSettingView";
            this.btnSettingView.Size = new System.Drawing.Size(170, 54);
            this.btnSettingView.StateCommon.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(45)))), ((int)(((byte)(60)))));
            this.btnSettingView.StateCommon.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(45)))), ((int)(((byte)(60)))));
            this.btnSettingView.StateCommon.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(84)))), ((int)(((byte)(101)))), ((int)(((byte)(122)))));
            this.btnSettingView.StateCommon.Border.DrawBorders = ((Krypton.Toolkit.PaletteDrawBorders)((((Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom) 
            | Krypton.Toolkit.PaletteDrawBorders.Left) 
            | Krypton.Toolkit.PaletteDrawBorders.Right)));
            this.btnSettingView.StateCommon.Border.Rounding = 5F;
            this.btnSettingView.StateCommon.Content.ShortText.Color1 = System.Drawing.Color.White;
            this.btnSettingView.StateCommon.Content.ShortText.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.btnSettingView.TabIndex = 7;
            this.btnSettingView.Values.DropDownArrowColor = System.Drawing.Color.Empty;
            this.btnSettingView.Values.Text = "SETTING";
            // 
            // lblTitle
            // 
            this.lblTitle.AccessibleName = "AMOSENSE CI";
            this.lblTitle.BackColor = System.Drawing.Color.White;
            this.lblTitle.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.lblTitle.BackgroundImage = global::SOFCMeas.Properties.Resources.AMOCI;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 24F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(2, 2);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(145, 77);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlMain
            // 
            this.pnlMain.Controls.Add(this.stationView2);
            this.pnlMain.Controls.Add(this.stationView1);
            this.pnlMain.AutoScroll = true;
            this.pnlMain.Location = new System.Drawing.Point(0, 80);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(1920, 960);
            this.pnlMain.StateCommon.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(239)))), ((int)(((byte)(244)))));
            this.pnlMain.StateCommon.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(239)))), ((int)(((byte)(244)))));
            this.pnlMain.TabIndex = 1;
            // 
            // stationView2
            // 
            this.stationView2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.stationView2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.stationView2.FileName = "SOFC_B07";
            this.stationView2.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.stationView2.GraphColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(139)))), ((int)(((byte)(0)))));
            this.stationView2.Location = new System.Drawing.Point(990, 50);
            this.stationView2.LogText = "PLC 연결 대기...";
            this.stationView2.LotNumber = "260827-B03";
            this.stationView2.Name = "stationView2";
            this.stationView2.OperatorName = "김영희";
            this.stationView2.Size = new System.Drawing.Size(900, 866);
            this.stationView2.SpecText = "0.40";
            this.stationView2.StateColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(112)))), ((int)(((byte)(128)))));
            this.stationView2.StateText = "WAIT";
            this.stationView2.StationTitle = "설비 #2  |  PLC 172.20.9.101";
            this.stationView2.TabIndex = 1;
            // 
            // stationView1
            // 
            this.stationView1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.stationView1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.stationView1.FileName = "SOFC_A12";
            this.stationView1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.stationView1.GraphColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(118)))), ((int)(((byte)(128)))));
            this.stationView1.Location = new System.Drawing.Point(30, 50);
            this.stationView1.LogText = "PLC 연결 대기...";
            this.stationView1.LotNumber = "260827-A01";
            this.stationView1.Name = "stationView1";
            this.stationView1.OperatorName = "홍길동";
            this.stationView1.Size = new System.Drawing.Size(900, 866);
            this.stationView1.SpecText = "0.40";
            this.stationView1.StateColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(112)))), ((int)(((byte)(128)))));
            this.stationView1.StateText = "WAIT";
            this.stationView1.StationTitle = "설비 #1  |  PLC 172.20.9.100";
            this.stationView1.TabIndex = 0;
            // 
            // pnlLog
            // 
            this.pnlLog.Controls.Add(this.lblLogSummary);
            this.pnlLog.Controls.Add(this.dgvLogs);
            this.pnlLog.Controls.Add(this.pnlLogFilter);
            this.pnlLog.Location = new System.Drawing.Point(0, 80);
            this.pnlLog.Name = "pnlLog";
            this.pnlLog.Size = new System.Drawing.Size(1920, 960);
            this.pnlLog.StateCommon.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(239)))), ((int)(((byte)(244)))));
            this.pnlLog.StateCommon.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(239)))), ((int)(((byte)(244)))));
            this.pnlLog.TabIndex = 2;
            this.pnlLog.Visible = false;
            // 
            // lblLogSummary
            // 
            this.lblLogSummary.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblLogSummary.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(82)))), ((int)(((byte)(92)))));
            this.lblLogSummary.Location = new System.Drawing.Point(30, 906);
            this.lblLogSummary.Name = "lblLogSummary";
            this.lblLogSummary.Size = new System.Drawing.Size(1860, 34);
            this.lblLogSummary.TabIndex = 2;
            this.lblLogSummary.Text = "저장된 검사 로그를 조회하세요.";
            this.lblLogSummary.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dgvLogs
            // 
            this.dgvLogs.AllowUserToAddRows = false;
            this.dgvLogs.AllowUserToDeleteRows = false;
            this.dgvLogs.AllowUserToResizeRows = false;
            this.dgvLogs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvLogs.BackgroundColor = System.Drawing.Color.White;
            this.dgvLogs.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvLogs.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvLogs.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(78)))), ((int)(((byte)(84)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(78)))), ((int)(((byte)(84)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvLogs.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvLogs.ColumnHeadersHeight = 58;
            this.dgvLogs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colLogTime,
            this.colLogStation,
            this.colLogLot,
            this.colLogModel,
            this.colLogResult,
            this.colLogLoad,
            this.colLogMessage});
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("맑은 고딕", 11F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(52)))), ((int)(((byte)(61)))));
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(218)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(52)))), ((int)(((byte)(61)))));
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvLogs.DefaultCellStyle = dataGridViewCellStyle4;
            this.dgvLogs.EnableHeadersVisualStyles = false;
            this.dgvLogs.Location = new System.Drawing.Point(30, 215);
            this.dgvLogs.MultiSelect = false;
            this.dgvLogs.Name = "dgvLogs";
            this.dgvLogs.ReadOnly = true;
            this.dgvLogs.RowHeadersVisible = false;
            this.dgvLogs.RowTemplate.Height = 64;
            this.dgvLogs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvLogs.Size = new System.Drawing.Size(1860, 683);
            this.dgvLogs.TabIndex = 1;
            // 
            // colLogTime
            // 
            this.colLogTime.FillWeight = 110F;
            this.colLogTime.HeaderText = "시간";
            this.colLogTime.Name = "colLogTime";
            this.colLogTime.ReadOnly = true;
            // 
            // colLogStation
            // 
            this.colLogStation.FillWeight = 55F;
            this.colLogStation.HeaderText = "설비";
            this.colLogStation.Name = "colLogStation";
            this.colLogStation.ReadOnly = true;
            // 
            // colLogLot
            // 
            this.colLogLot.FillWeight = 95F;
            this.colLogLot.HeaderText = "LOT NO.";
            this.colLogLot.Name = "colLogLot";
            this.colLogLot.ReadOnly = true;
            // 
            // colLogModel
            // 
            this.colLogModel.FillWeight = 85F;
            this.colLogModel.HeaderText = "파일명";
            this.colLogModel.Name = "colLogModel";
            this.colLogModel.ReadOnly = true;
            // 
            // colLogResult
            // 
            this.colLogResult.FillWeight = 65F;
            this.colLogResult.HeaderText = "판정";
            this.colLogResult.Name = "colLogResult";
            this.colLogResult.ReadOnly = true;
            // 
            // colLogLoad
            // 
            this.colLogLoad.FillWeight = 85F;
            this.colLogLoad.HeaderText = "최대하중(kgf)";
            this.colLogLoad.Name = "colLogLoad";
            this.colLogLoad.ReadOnly = true;
            // 
            // colLogMessage
            // 
            this.colLogMessage.FillWeight = 210F;
            this.colLogMessage.HeaderText = "메시지";
            this.colLogMessage.Name = "colLogMessage";
            this.colLogMessage.ReadOnly = true;
            // 
            // pnlLogFilter
            // 
            this.pnlLogFilter.BackColor = System.Drawing.Color.White;
            this.pnlLogFilter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlLogFilter.Controls.Add(this.lblLogPolicyStatus);
            this.pnlLogFilter.Controls.Add(this.btnApplyLogPolicy);
            this.pnlLogFilter.Controls.Add(this.numLogRetentionDays);
            this.pnlLogFilter.Controls.Add(this.lblLogRetentionDays);
            this.pnlLogFilter.Controls.Add(this.chkLogAutoDelete);
            this.pnlLogFilter.Controls.Add(this.lblLogStoragePath);
            this.pnlLogFilter.Controls.Add(this.btnExportLog);
            this.pnlLogFilter.Controls.Add(this.btnSearchLog);
            this.pnlLogFilter.Controls.Add(this.cboLogStation);
            this.pnlLogFilter.Controls.Add(this.lblLogStation);
            this.pnlLogFilter.Controls.Add(this.dtpLogTo);
            this.pnlLogFilter.Controls.Add(this.lblDateSeparator);
            this.pnlLogFilter.Controls.Add(this.dtpLogFrom);
            this.pnlLogFilter.Controls.Add(this.lblLogPeriod);
            this.pnlLogFilter.Controls.Add(this.lblLogTitle);
            this.pnlLogFilter.Location = new System.Drawing.Point(30, 30);
            this.pnlLogFilter.Name = "pnlLogFilter";
            this.pnlLogFilter.Size = new System.Drawing.Size(1860, 165);
            this.pnlLogFilter.TabIndex = 0;
            // 
            // lblLogPolicyStatus
            // 
            this.lblLogPolicyStatus.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblLogPolicyStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(82)))), ((int)(((byte)(92)))));
            this.lblLogPolicyStatus.Location = new System.Drawing.Point(1260, 108);
            this.lblLogPolicyStatus.Name = "lblLogPolicyStatus";
            this.lblLogPolicyStatus.Size = new System.Drawing.Size(567, 42);
            this.lblLogPolicyStatus.TabIndex = 14;
            this.lblLogPolicyStatus.Text = "로그 정책을 확인하는 중입니다.";
            this.lblLogPolicyStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnApplyLogPolicy
            // 
            this.btnApplyLogPolicy.Location = new System.Drawing.Point(1040, 106);
            this.btnApplyLogPolicy.Name = "btnApplyLogPolicy";
            this.btnApplyLogPolicy.Size = new System.Drawing.Size(200, 45);
            this.btnApplyLogPolicy.StateCommon.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(118)))), ((int)(((byte)(128)))));
            this.btnApplyLogPolicy.StateCommon.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(118)))), ((int)(((byte)(128)))));
            this.btnApplyLogPolicy.StateCommon.Border.DrawBorders = ((Krypton.Toolkit.PaletteDrawBorders)((((Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom) 
            | Krypton.Toolkit.PaletteDrawBorders.Left) 
            | Krypton.Toolkit.PaletteDrawBorders.Right)));
            this.btnApplyLogPolicy.StateCommon.Border.Rounding = 3F;
            this.btnApplyLogPolicy.StateCommon.Content.ShortText.Color1 = System.Drawing.Color.White;
            this.btnApplyLogPolicy.StateCommon.Content.ShortText.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnApplyLogPolicy.TabIndex = 13;
            this.btnApplyLogPolicy.Values.DropDownArrowColor = System.Drawing.Color.Empty;
            this.btnApplyLogPolicy.Values.Text = "정책 저장 / 지금 정리";
            // 
            // numLogRetentionDays
            // 
            this.numLogRetentionDays.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.numLogRetentionDays.Location = new System.Drawing.Point(940, 113);
            this.numLogRetentionDays.Maximum = new decimal(new int[] {
            3650,
            0,
            0,
            0});
            this.numLogRetentionDays.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numLogRetentionDays.Name = "numLogRetentionDays";
            this.numLogRetentionDays.Size = new System.Drawing.Size(80, 27);
            this.numLogRetentionDays.TabIndex = 12;
            this.numLogRetentionDays.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numLogRetentionDays.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // lblLogRetentionDays
            // 
            this.lblLogRetentionDays.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblLogRetentionDays.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(78)))), ((int)(((byte)(84)))));
            this.lblLogRetentionDays.Location = new System.Drawing.Point(840, 106);
            this.lblLogRetentionDays.Name = "lblLogRetentionDays";
            this.lblLogRetentionDays.Size = new System.Drawing.Size(100, 44);
            this.lblLogRetentionDays.TabIndex = 11;
            this.lblLogRetentionDays.Text = "보관일수";
            this.lblLogRetentionDays.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // chkLogAutoDelete
            // 
            this.chkLogAutoDelete.AutoSize = true;
            this.chkLogAutoDelete.Checked = true;
            this.chkLogAutoDelete.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkLogAutoDelete.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.chkLogAutoDelete.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(78)))), ((int)(((byte)(84)))));
            this.chkLogAutoDelete.Location = new System.Drawing.Point(702, 115);
            this.chkLogAutoDelete.Name = "chkLogAutoDelete";
            this.chkLogAutoDelete.Size = new System.Drawing.Size(84, 23);
            this.chkLogAutoDelete.TabIndex = 10;
            this.chkLogAutoDelete.Text = "자동삭제";
            this.chkLogAutoDelete.UseVisualStyleBackColor = true;
            // 
            // lblLogStoragePath
            // 
            this.lblLogStoragePath.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblLogStoragePath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(82)))), ((int)(((byte)(92)))));
            this.lblLogStoragePath.Location = new System.Drawing.Point(24, 106);
            this.lblLogStoragePath.Name = "lblLogStoragePath";
            this.lblLogStoragePath.Size = new System.Drawing.Size(650, 44);
            this.lblLogStoragePath.TabIndex = 9;
            this.lblLogStoragePath.Text = "저장 경로: " + ApplicationPaths.LogRootDirectory + "\\yyyy\\MM\\dd";
            this.lblLogStoragePath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnExportLog
            // 
            this.btnExportLog.Location = new System.Drawing.Point(1647, 26);
            this.btnExportLog.Name = "btnExportLog";
            this.btnExportLog.Size = new System.Drawing.Size(180, 56);
            this.btnExportLog.StateCommon.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(104)))), ((int)(((byte)(82)))), ((int)(((byte)(171)))));
            this.btnExportLog.StateCommon.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(104)))), ((int)(((byte)(82)))), ((int)(((byte)(171)))));
            this.btnExportLog.StateCommon.Border.DrawBorders = ((Krypton.Toolkit.PaletteDrawBorders)((((Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom) 
            | Krypton.Toolkit.PaletteDrawBorders.Left) 
            | Krypton.Toolkit.PaletteDrawBorders.Right)));
            this.btnExportLog.StateCommon.Border.Rounding = 3F;
            this.btnExportLog.StateCommon.Content.ShortText.Color1 = System.Drawing.Color.White;
            this.btnExportLog.StateCommon.Content.ShortText.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnExportLog.TabIndex = 8;
            this.btnExportLog.Values.DropDownArrowColor = System.Drawing.Color.Empty;
            this.btnExportLog.Values.Text = "CSV 내보내기";
            // 
            // btnSearchLog
            // 
            this.btnSearchLog.Location = new System.Drawing.Point(1450, 26);
            this.btnSearchLog.Name = "btnSearchLog";
            this.btnSearchLog.Size = new System.Drawing.Size(180, 56);
            this.btnSearchLog.StateCommon.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(151)))), ((int)(((byte)(86)))));
            this.btnSearchLog.StateCommon.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(151)))), ((int)(((byte)(86)))));
            this.btnSearchLog.StateCommon.Border.DrawBorders = ((Krypton.Toolkit.PaletteDrawBorders)((((Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom) 
            | Krypton.Toolkit.PaletteDrawBorders.Left) 
            | Krypton.Toolkit.PaletteDrawBorders.Right)));
            this.btnSearchLog.StateCommon.Border.Rounding = 3F;
            this.btnSearchLog.StateCommon.Content.ShortText.Color1 = System.Drawing.Color.White;
            this.btnSearchLog.StateCommon.Content.ShortText.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnSearchLog.TabIndex = 7;
            this.btnSearchLog.Values.DropDownArrowColor = System.Drawing.Color.Empty;
            this.btnSearchLog.Values.Text = "조회";
            // 
            // cboLogStation
            // 
            this.cboLogStation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLogStation.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.cboLogStation.FormattingEnabled = true;
            this.cboLogStation.Items.AddRange(new object[] {
            "전체 설비",
            "설비 #1",
            "설비 #2"});
            this.cboLogStation.Location = new System.Drawing.Point(1177, 35);
            this.cboLogStation.Name = "cboLogStation";
            this.cboLogStation.Size = new System.Drawing.Size(210, 29);
            this.cboLogStation.TabIndex = 6;
            // 
            // lblLogStation
            // 
            this.lblLogStation.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblLogStation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(78)))), ((int)(((byte)(84)))));
            this.lblLogStation.Location = new System.Drawing.Point(1097, 29);
            this.lblLogStation.Name = "lblLogStation";
            this.lblLogStation.Size = new System.Drawing.Size(80, 48);
            this.lblLogStation.TabIndex = 5;
            this.lblLogStation.Text = "설비";
            this.lblLogStation.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // dtpLogTo
            // 
            this.dtpLogTo.CustomFormat = "yyyy-MM-dd";
            this.dtpLogTo.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.dtpLogTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpLogTo.Location = new System.Drawing.Point(842, 35);
            this.dtpLogTo.Name = "dtpLogTo";
            this.dtpLogTo.Size = new System.Drawing.Size(210, 29);
            this.dtpLogTo.TabIndex = 4;
            // 
            // lblDateSeparator
            // 
            this.lblDateSeparator.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.lblDateSeparator.Location = new System.Drawing.Point(797, 29);
            this.lblDateSeparator.Name = "lblDateSeparator";
            this.lblDateSeparator.Size = new System.Drawing.Size(45, 48);
            this.lblDateSeparator.TabIndex = 3;
            this.lblDateSeparator.Text = "~";
            this.lblDateSeparator.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // dtpLogFrom
            // 
            this.dtpLogFrom.CustomFormat = "yyyy-MM-dd";
            this.dtpLogFrom.Font = new System.Drawing.Font("맑은 고딕", 12F);
            this.dtpLogFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpLogFrom.Location = new System.Drawing.Point(587, 35);
            this.dtpLogFrom.Name = "dtpLogFrom";
            this.dtpLogFrom.Size = new System.Drawing.Size(210, 29);
            this.dtpLogFrom.TabIndex = 2;
            // 
            // lblLogPeriod
            // 
            this.lblLogPeriod.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblLogPeriod.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(78)))), ((int)(((byte)(84)))));
            this.lblLogPeriod.Location = new System.Drawing.Point(502, 29);
            this.lblLogPeriod.Name = "lblLogPeriod";
            this.lblLogPeriod.Size = new System.Drawing.Size(85, 48);
            this.lblLogPeriod.TabIndex = 1;
            this.lblLogPeriod.Text = "조회기간";
            this.lblLogPeriod.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLogTitle
            // 
            this.lblLogTitle.Font = new System.Drawing.Font("맑은 고딕", 19F, System.Drawing.FontStyle.Bold);
            this.lblLogTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(68)))), ((int)(((byte)(74)))));
            this.lblLogTitle.Location = new System.Drawing.Point(24, 0);
            this.lblLogTitle.Name = "lblLogTitle";
            this.lblLogTitle.Size = new System.Drawing.Size(380, 96);
            this.lblLogTitle.TabIndex = 0;
            this.lblLogTitle.Text = "검사 로그 조회";
            this.lblLogTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SOFCMeas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(239)))), ((int)(((byte)(244)))));
            this.ClientSize = new System.Drawing.Size(1920, 1040);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlLog);
            this.Controls.Add(this.pnlTitle);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(1920, 1040);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(1918, 1038);
            this.Name = "SOFCMeas";
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.ShowIcon = true;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SOFCMeas";
            ((System.ComponentModel.ISupportInitialize)(this.pnlTitle)).EndInit();
            this.pnlTitle.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pnlMain)).EndInit();
            this.pnlMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pnlLog)).EndInit();
            this.pnlLog.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvLogs)).EndInit();
            this.pnlLogFilter.ResumeLayout(false);
            this.pnlLogFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLogRetentionDays)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Krypton.Toolkit.KryptonPanel pnlTitle;
        private Krypton.Toolkit.KryptonButton btnSettingView;
        private System.Windows.Forms.Label lblTitle;
        private Krypton.Toolkit.KryptonButton btnMainView;
        private Krypton.Toolkit.KryptonButton btnLogView;
        private System.Windows.Forms.Label lblHb1Status;
        private System.Windows.Forms.Label lblHb2Status;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnMinimize;
        private Krypton.Toolkit.KryptonPanel pnlMain;
        private StationView stationView1;
        private StationView stationView2;
        private Krypton.Toolkit.KryptonPanel pnlLog;
        private System.Windows.Forms.Panel pnlLogFilter;
        private System.Windows.Forms.Label lblLogTitle;
        private System.Windows.Forms.Label lblLogPeriod;
        private System.Windows.Forms.DateTimePicker dtpLogFrom;
        private System.Windows.Forms.Label lblDateSeparator;
        private System.Windows.Forms.DateTimePicker dtpLogTo;
        private System.Windows.Forms.Label lblLogStation;
        private System.Windows.Forms.ComboBox cboLogStation;
        private Krypton.Toolkit.KryptonButton btnSearchLog;
        private Krypton.Toolkit.KryptonButton btnExportLog;
        private System.Windows.Forms.DataGridView dgvLogs;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLogTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLogStation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLogLot;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLogModel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLogResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLogLoad;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLogMessage;
        private System.Windows.Forms.Label lblLogSummary;
        private System.Windows.Forms.Label lblLogStoragePath;
        private System.Windows.Forms.CheckBox chkLogAutoDelete;
        private System.Windows.Forms.Label lblLogRetentionDays;
        private System.Windows.Forms.NumericUpDown numLogRetentionDays;
        private Krypton.Toolkit.KryptonButton btnApplyLogPolicy;
        private System.Windows.Forms.Label lblLogPolicyStatus;
    }
}



