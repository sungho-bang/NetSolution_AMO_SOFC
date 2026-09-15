namespace SOFCMeas
{
    partial class StationView
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

        #region 구성 요소 디자이너에서 생성한 코드

        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title1 = new System.Windows.Forms.DataVisualization.Charting.Title();
            this.pnlStationHeader = new System.Windows.Forms.Panel();
            this.lblStationState = new System.Windows.Forms.Label();
            this.lblStationTitle = new System.Windows.Forms.Label();
            this.lblJobInfo = new System.Windows.Forms.Label();
            this.lblModel = new System.Windows.Forms.Label();
            this.txtModel = new System.Windows.Forms.TextBox();
            this.lblOperator = new System.Windows.Forms.Label();
            this.txtOperator = new System.Windows.Forms.TextBox();
            this.lblLotNumber = new System.Windows.Forms.Label();
            this.txtLotNumber = new System.Windows.Forms.TextBox();
            this.lblSpec = new System.Windows.Forms.Label();
            this.txtSpec = new System.Windows.Forms.TextBox();
            this.lblGraph = new System.Windows.Forms.Label();
            this.lblCurrentLoad = new System.Windows.Forms.Label();
            this.lblCompletedCount = new System.Windows.Forms.Label();
            this.cboInspectionNumber = new System.Windows.Forms.ComboBox();
            this.btnReview = new System.Windows.Forms.Button();
            this.chartLoad = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tblInspection = new System.Windows.Forms.TableLayoutPanel();
            this.lblInspectionHeader = new System.Windows.Forms.Label();
            this.lblInspectionValueHeader = new System.Windows.Forms.Label();
            this.lblCountCaption = new System.Windows.Forms.Label();
            this.lblCountValue = new System.Windows.Forms.Label();
            this.lblPassCaption = new System.Windows.Forms.Label();
            this.lblPassValue = new System.Windows.Forms.Label();
            this.lblFailCaption = new System.Windows.Forms.Label();
            this.lblFailValue = new System.Windows.Forms.Label();
            this.lblYieldCaption = new System.Windows.Forms.Label();
            this.lblYieldValue = new System.Windows.Forms.Label();
            this.lblVerdictCaption = new System.Windows.Forms.Label();
            this.lblVerdictValue = new System.Windows.Forms.Label();
            this.lblLog = new System.Windows.Forms.Label();
            this.txtStationLog = new System.Windows.Forms.TextBox();
            this.btnLotEnd = new Krypton.Toolkit.KryptonButton();
            this.grpHistorySearch = new System.Windows.Forms.GroupBox();
            this.historyFrom = new System.Windows.Forms.DateTimePicker();
            this.historyTo = new System.Windows.Forms.DateTimePicker();
            this.historyResults = new System.Windows.Forms.DataGridView();
            this.Time = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Model = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.historyFind = new System.Windows.Forms.Button();
            this.lblHistoryFrom = new System.Windows.Forms.Label();
            this.lblHistoryTo = new System.Windows.Forms.Label();
            this.Result = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Lot = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Peak = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnStart = new System.Windows.Forms.Button();
            this.pnlStationHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartLoad)).BeginInit();
            this.tblInspection.SuspendLayout();
            this.grpHistorySearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.historyResults)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlStationHeader
            // 
            this.pnlStationHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(78)))), ((int)(((byte)(84)))));
            this.pnlStationHeader.Controls.Add(this.lblStationState);
            this.pnlStationHeader.Controls.Add(this.lblStationTitle);
            this.pnlStationHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlStationHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlStationHeader.Name = "pnlStationHeader";
            this.pnlStationHeader.Size = new System.Drawing.Size(900, 58);
            this.pnlStationHeader.TabIndex = 0;
            // 
            // lblStationState
            // 
            this.lblStationState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(151)))), ((int)(((byte)(86)))));
            this.lblStationState.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.lblStationState.ForeColor = System.Drawing.Color.White;
            this.lblStationState.Location = new System.Drawing.Point(733, 9);
            this.lblStationState.Name = "lblStationState";
            this.lblStationState.Size = new System.Drawing.Size(145, 40);
            this.lblStationState.TabIndex = 1;
            this.lblStationState.Text = "READY";
            this.lblStationState.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStationTitle
            // 
            this.lblStationTitle.Font = new System.Drawing.Font("맑은 고딕", 16F, System.Drawing.FontStyle.Bold);
            this.lblStationTitle.ForeColor = System.Drawing.Color.White;
            this.lblStationTitle.Location = new System.Drawing.Point(20, 0);
            this.lblStationTitle.Name = "lblStationTitle";
            this.lblStationTitle.Size = new System.Drawing.Size(680, 58);
            this.lblStationTitle.TabIndex = 0;
            this.lblStationTitle.Text = "설비 #1  |  PLC 172.20.9.100";
            this.lblStationTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblJobInfo
            // 
            this.lblJobInfo.AutoSize = true;
            this.lblJobInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblJobInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(68)))), ((int)(((byte)(74)))));
            this.lblJobInfo.Location = new System.Drawing.Point(20, 76);
            this.lblJobInfo.Name = "lblJobInfo";
            this.lblJobInfo.Size = new System.Drawing.Size(138, 20);
            this.lblJobInfo.TabIndex = 1;
            this.lblJobInfo.Text = "작업정보 입력 (PC)";
            // 
            // lblModel
            // 
            this.lblModel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(228)))), ((int)(((byte)(232)))));
            this.lblModel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblModel.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblModel.Location = new System.Drawing.Point(22, 108);
            this.lblModel.Name = "lblModel";
            this.lblModel.Size = new System.Drawing.Size(95, 27);
            this.lblModel.TabIndex = 2;
            this.lblModel.Text = "모델 명";
            this.lblModel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtModel
            // 
            this.txtModel.BackColor = System.Drawing.Color.White;
            this.txtModel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtModel.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.txtModel.Location = new System.Drawing.Point(118, 108);
            this.txtModel.Name = "txtModel";
            this.txtModel.ReadOnly = false;
            this.txtModel.Size = new System.Drawing.Size(286, 27);
            this.txtModel.TabIndex = 3;
            this.txtModel.TabStop = true;
            this.txtModel.Text = "SOFC_A12";
            this.txtModel.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblOperator
            // 
            this.lblOperator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(228)))), ((int)(((byte)(232)))));
            this.lblOperator.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOperator.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblOperator.Location = new System.Drawing.Point(426, 108);
            this.lblOperator.Name = "lblOperator";
            this.lblOperator.Size = new System.Drawing.Size(95, 27);
            this.lblOperator.TabIndex = 4;
            this.lblOperator.Text = "작업자";
            this.lblOperator.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtOperator
            // 
            this.txtOperator.BackColor = System.Drawing.Color.White;
            this.txtOperator.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOperator.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.txtOperator.Location = new System.Drawing.Point(522, 108);
            this.txtOperator.Name = "txtOperator";
            this.txtOperator.ReadOnly = false;
            this.txtOperator.Size = new System.Drawing.Size(358, 27);
            this.txtOperator.TabIndex = 5;
            this.txtOperator.TabStop = true;
            this.txtOperator.Text = "홍길동";
            this.txtOperator.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblLotNumber
            // 
            this.lblLotNumber.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(228)))), ((int)(((byte)(232)))));
            this.lblLotNumber.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblLotNumber.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblLotNumber.Location = new System.Drawing.Point(22, 151);
            this.lblLotNumber.Name = "lblLotNumber";
            this.lblLotNumber.Size = new System.Drawing.Size(95, 27);
            this.lblLotNumber.TabIndex = 6;
            this.lblLotNumber.Text = "LOT NO.";
            this.lblLotNumber.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtLotNumber
            // 
            this.txtLotNumber.BackColor = System.Drawing.Color.White;
            this.txtLotNumber.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtLotNumber.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.txtLotNumber.Location = new System.Drawing.Point(118, 151);
            this.txtLotNumber.Name = "txtLotNumber";
            this.txtLotNumber.ReadOnly = false;
            this.txtLotNumber.Size = new System.Drawing.Size(286, 27);
            this.txtLotNumber.TabIndex = 7;
            this.txtLotNumber.TabStop = true;
            this.txtLotNumber.Text = "260827-A01";
            this.txtLotNumber.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblSpec
            // 
            this.lblSpec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(228)))), ((int)(((byte)(232)))));
            this.lblSpec.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSpec.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblSpec.Location = new System.Drawing.Point(426, 151);
            this.lblSpec.Name = "lblSpec";
            this.lblSpec.Size = new System.Drawing.Size(95, 27);
            this.lblSpec.TabIndex = 8;
            this.lblSpec.Text = "SPEC";
            this.lblSpec.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtSpec
            // 
            this.txtSpec.BackColor = System.Drawing.Color.White;
            this.txtSpec.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSpec.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.txtSpec.Location = new System.Drawing.Point(522, 151);
            this.txtSpec.Name = "txtSpec";
            this.txtSpec.ReadOnly = true;
            this.txtSpec.Size = new System.Drawing.Size(358, 27);
            this.txtSpec.TabIndex = 9;
            this.txtSpec.TabStop = false;
            this.txtSpec.Text = "0.40";
            this.txtSpec.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblGraph
            // 
            this.lblGraph.AutoSize = true;
            this.lblGraph.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblGraph.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(68)))), ((int)(((byte)(74)))));
            this.lblGraph.Location = new System.Drawing.Point(20, 209);
            this.lblGraph.Name = "lblGraph";
            this.lblGraph.Size = new System.Drawing.Size(177, 20);
            this.lblGraph.TabIndex = 10;
            this.lblGraph.Text = "실시간 하중 그래프 (kgf)";
            // 
            // lblCurrentLoad
            // 
            this.lblCurrentLoad.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblCurrentLoad.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(118)))), ((int)(((byte)(128)))));
            this.lblCurrentLoad.Location = new System.Drawing.Point(230, 195);
            this.lblCurrentLoad.Name = "lblCurrentLoad";
            this.lblCurrentLoad.Size = new System.Drawing.Size(340, 40);
            this.lblCurrentLoad.TabIndex = 18;
            this.lblCurrentLoad.Text = "D700[0000][0000][0000][0000][0000]\r\n+0.000 kgf  |  검사 횟수 0";
            this.lblCurrentLoad.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCompletedCount
            // 
            this.lblCompletedCount.Location = new System.Drawing.Point(8, 255);
            this.lblCompletedCount.Name = "lblCompletedCount";
            this.lblCompletedCount.Size = new System.Drawing.Size(67, 30);
            this.lblCompletedCount.TabIndex = 19;
            this.lblCompletedCount.Text = "검사 번호";
            this.lblCompletedCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboInspectionNumber
            // 
            this.cboInspectionNumber.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInspectionNumber.Location = new System.Drawing.Point(81, 261);
            this.cboInspectionNumber.Name = "cboInspectionNumber";
            this.cboInspectionNumber.Size = new System.Drawing.Size(82, 23);
            this.cboInspectionNumber.TabIndex = 20;
            // 
            // btnReview
            // 
            this.btnReview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(49)))), ((int)(((byte)(105)))), ((int)(((byte)(176)))));
            this.btnReview.FlatAppearance.BorderSize = 0;
            this.btnReview.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(82)))), ((int)(((byte)(141)))));
            this.btnReview.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(123)))), ((int)(((byte)(194)))));
            this.btnReview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReview.ForeColor = System.Drawing.Color.White;
            this.btnReview.Location = new System.Drawing.Point(172, 259);
            this.btnReview.Name = "btnReview";
            this.btnReview.Size = new System.Drawing.Size(110, 30);
            this.btnReview.TabIndex = 20;
            this.btnReview.Text = "REVIEW";
            this.btnReview.UseVisualStyleBackColor = false;
            // 
            // chartLoad
            // 
            chartArea1.AxisX.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.True;
            chartArea1.AxisX.Interval = 6.6D;
            chartArea1.AxisX.LabelStyle.Font = new System.Drawing.Font("맑은 고딕", 8F);
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(228)))), ((int)(((byte)(232)))));
            chartArea1.AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot;
            chartArea1.AxisX.Maximum = 66D;
            chartArea1.AxisX.Minimum = 0D;
            chartArea1.AxisX.Title = "샘플 횟수 (기본 1초/회)";
            chartArea1.AxisX.TitleFont = new System.Drawing.Font("맑은 고딕", 8F);
            chartArea1.AxisY.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.True;
            chartArea1.AxisY.Interval = 0.044D;
            chartArea1.AxisY.LabelStyle.Font = new System.Drawing.Font("맑은 고딕", 8F);
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(228)))), ((int)(((byte)(232)))));
            chartArea1.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot;
            chartArea1.AxisY.Maximum = 0.44D;
            chartArea1.AxisY.Minimum = 0D;
            chartArea1.BackColor = System.Drawing.Color.White;
            chartArea1.Name = "LoadArea";
            this.chartLoad.ChartAreas.Add(chartArea1);
            this.chartLoad.Location = new System.Drawing.Point(22, 239);
            this.chartLoad.Name = "chartLoad";
            series1.BorderWidth = 4;
            series1.ChartArea = "LoadArea";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
            series1.Color = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(118)))), ((int)(((byte)(128)))));
            series1.Name = "Load";
            this.chartLoad.Series.Add(series1);
            this.chartLoad.Size = new System.Drawing.Size(548, 372);
            this.chartLoad.TabIndex = 11;
            this.chartLoad.Text = "하중 그래프";
            title1.Alignment = System.Drawing.ContentAlignment.MiddleLeft;
            title1.DockedToChartArea = "LoadArea";
            title1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            title1.IsDockedInsideChartArea = false;
            title1.Name = "YAxisCaption";
            title1.Text = "하중 (kgf)";
            this.chartLoad.Titles.Add(title1);
            // 
            // tblInspection
            // 
            this.tblInspection.ColumnCount = 2;
            this.tblInspection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 56F));
            this.tblInspection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.tblInspection.Controls.Add(this.lblInspectionHeader, 0, 0);
            this.tblInspection.Controls.Add(this.lblInspectionValueHeader, 1, 0);
            this.tblInspection.Controls.Add(this.lblCountCaption, 0, 1);
            this.tblInspection.Controls.Add(this.lblCountValue, 1, 1);
            this.tblInspection.Controls.Add(this.lblPassCaption, 0, 2);
            this.tblInspection.Controls.Add(this.lblPassValue, 1, 2);
            this.tblInspection.Controls.Add(this.lblFailCaption, 0, 3);
            this.tblInspection.Controls.Add(this.lblFailValue, 1, 3);
            this.tblInspection.Controls.Add(this.lblYieldCaption, 0, 4);
            this.tblInspection.Controls.Add(this.lblYieldValue, 1, 4);
            this.tblInspection.Controls.Add(this.lblVerdictCaption, 0, 5);
            this.tblInspection.Controls.Add(this.lblVerdictValue, 1, 5);
            this.tblInspection.Location = new System.Drawing.Point(592, 194);
            this.tblInspection.Name = "tblInspection";
            this.tblInspection.RowCount = 6;
            this.tblInspection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 54F));
            this.tblInspection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblInspection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblInspection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblInspection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblInspection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblInspection.Size = new System.Drawing.Size(288, 333);
            this.tblInspection.TabIndex = 12;
            // 
            // lblInspectionHeader
            // 
            this.lblInspectionHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(78)))), ((int)(((byte)(84)))));
            this.lblInspectionHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblInspectionHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInspectionHeader.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblInspectionHeader.ForeColor = System.Drawing.Color.White;
            this.lblInspectionHeader.Location = new System.Drawing.Point(0, 0);
            this.lblInspectionHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblInspectionHeader.Name = "lblInspectionHeader";
            this.lblInspectionHeader.Size = new System.Drawing.Size(161, 54);
            this.lblInspectionHeader.TabIndex = 0;
            this.lblInspectionHeader.Text = "검사현황";
            this.lblInspectionHeader.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblInspectionValueHeader
            // 
            this.lblInspectionValueHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(78)))), ((int)(((byte)(84)))));
            this.lblInspectionValueHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblInspectionValueHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInspectionValueHeader.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblInspectionValueHeader.ForeColor = System.Drawing.Color.White;
            this.lblInspectionValueHeader.Location = new System.Drawing.Point(161, 0);
            this.lblInspectionValueHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblInspectionValueHeader.Name = "lblInspectionValueHeader";
            this.lblInspectionValueHeader.Size = new System.Drawing.Size(127, 54);
            this.lblInspectionValueHeader.TabIndex = 1;
            this.lblInspectionValueHeader.Text = "값";
            this.lblInspectionValueHeader.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblCountCaption
            // 
            this.lblCountCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCountCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCountCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCountCaption.Location = new System.Drawing.Point(0, 54);
            this.lblCountCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblCountCaption.Name = "lblCountCaption";
            this.lblCountCaption.Size = new System.Drawing.Size(161, 55);
            this.lblCountCaption.TabIndex = 2;
            this.lblCountCaption.Text = "검사수량(EA)";
            this.lblCountCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblCountValue
            // 
            this.lblCountValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCountValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCountValue.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblCountValue.Location = new System.Drawing.Point(161, 54);
            this.lblCountValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblCountValue.Name = "lblCountValue";
            this.lblCountValue.Size = new System.Drawing.Size(127, 55);
            this.lblCountValue.TabIndex = 3;
            this.lblCountValue.Text = "0";
            this.lblCountValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPassCaption
            // 
            this.lblPassCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPassCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPassCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblPassCaption.Location = new System.Drawing.Point(0, 109);
            this.lblPassCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblPassCaption.Name = "lblPassCaption";
            this.lblPassCaption.Size = new System.Drawing.Size(161, 55);
            this.lblPassCaption.TabIndex = 4;
            this.lblPassCaption.Text = "GOOD(EA)";
            this.lblPassCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPassValue
            // 
            this.lblPassValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPassValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPassValue.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblPassValue.Location = new System.Drawing.Point(161, 109);
            this.lblPassValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblPassValue.Name = "lblPassValue";
            this.lblPassValue.Size = new System.Drawing.Size(127, 55);
            this.lblPassValue.TabIndex = 5;
            this.lblPassValue.Text = "0";
            this.lblPassValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblFailCaption
            // 
            this.lblFailCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFailCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFailCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblFailCaption.Location = new System.Drawing.Point(0, 164);
            this.lblFailCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblFailCaption.Name = "lblFailCaption";
            this.lblFailCaption.Size = new System.Drawing.Size(161, 55);
            this.lblFailCaption.TabIndex = 6;
            this.lblFailCaption.Text = "NG(EA)";
            this.lblFailCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblFailValue
            // 
            this.lblFailValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFailValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFailValue.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblFailValue.Location = new System.Drawing.Point(161, 164);
            this.lblFailValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblFailValue.Name = "lblFailValue";
            this.lblFailValue.Size = new System.Drawing.Size(127, 55);
            this.lblFailValue.TabIndex = 7;
            this.lblFailValue.Text = "0";
            this.lblFailValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblYieldCaption
            // 
            this.lblYieldCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblYieldCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblYieldCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblYieldCaption.Location = new System.Drawing.Point(0, 219);
            this.lblYieldCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblYieldCaption.Name = "lblYieldCaption";
            this.lblYieldCaption.Size = new System.Drawing.Size(161, 55);
            this.lblYieldCaption.TabIndex = 8;
            this.lblYieldCaption.Text = "Yield(%)";
            this.lblYieldCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblYieldValue
            // 
            this.lblYieldValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblYieldValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblYieldValue.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblYieldValue.Location = new System.Drawing.Point(161, 219);
            this.lblYieldValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblYieldValue.Name = "lblYieldValue";
            this.lblYieldValue.Size = new System.Drawing.Size(127, 55);
            this.lblYieldValue.TabIndex = 9;
            this.lblYieldValue.Text = "0.00";
            this.lblYieldValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblVerdictCaption
            // 
            this.lblVerdictCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(242)))), ((int)(((byte)(233)))));
            this.lblVerdictCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblVerdictCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVerdictCaption.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblVerdictCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(151)))), ((int)(((byte)(86)))));
            this.lblVerdictCaption.Location = new System.Drawing.Point(0, 274);
            this.lblVerdictCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblVerdictCaption.Name = "lblVerdictCaption";
            this.lblVerdictCaption.Size = new System.Drawing.Size(161, 59);
            this.lblVerdictCaption.TabIndex = 10;
            this.lblVerdictCaption.Text = "판정";
            this.lblVerdictCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblVerdictValue
            // 
            this.lblVerdictValue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(242)))), ((int)(((byte)(233)))));
            this.lblVerdictValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblVerdictValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVerdictValue.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblVerdictValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(151)))), ((int)(((byte)(86)))));
            this.lblVerdictValue.Location = new System.Drawing.Point(161, 274);
            this.lblVerdictValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblVerdictValue.Name = "lblVerdictValue";
            this.lblVerdictValue.Size = new System.Drawing.Size(127, 59);
            this.lblVerdictValue.TabIndex = 11;
            this.lblVerdictValue.Text = "-";
            this.lblVerdictValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLog
            // 
            this.lblLog.BackColor = System.Drawing.Color.White;
            this.lblLog.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(118)))), ((int)(((byte)(128)))));
            this.lblLog.Location = new System.Drawing.Point(22, 626);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new System.Drawing.Size(95, 25);
            this.lblLog.TabIndex = 13;
            this.lblLog.Text = "LOG";
            this.lblLog.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtStationLog
            // 
            this.txtStationLog.BackColor = System.Drawing.Color.White;
            this.txtStationLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtStationLog.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtStationLog.Location = new System.Drawing.Point(22, 650);
            this.txtStationLog.Multiline = true;
            this.txtStationLog.Name = "txtStationLog";
            this.txtStationLog.ReadOnly = true;
            this.txtStationLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStationLog.Size = new System.Drawing.Size(560, 105);
            this.txtStationLog.TabIndex = 14;
            this.txtStationLog.TabStop = false;
            this.txtStationLog.Text = "PLC 연결 대기...";
            // 
            // btnLotEnd
            // 
            this.btnLotEnd.Location = new System.Drawing.Point(308, 778);
            this.btnLotEnd.Name = "btnLotEnd";
            this.btnLotEnd.Size = new System.Drawing.Size(274, 64);
            this.btnLotEnd.StateCommon.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(78)))), ((int)(((byte)(84)))));
            this.btnLotEnd.StateCommon.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(78)))), ((int)(((byte)(84)))));
            this.btnLotEnd.StateCommon.Border.DrawBorders = ((Krypton.Toolkit.PaletteDrawBorders)((((Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom) 
            | Krypton.Toolkit.PaletteDrawBorders.Left) 
            | Krypton.Toolkit.PaletteDrawBorders.Right)));
            this.btnLotEnd.StateCommon.Border.Rounding = 3F;
            this.btnLotEnd.StateCommon.Content.ShortText.Color1 = System.Drawing.Color.White;
            this.btnLotEnd.StateCommon.Content.ShortText.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnLotEnd.TabIndex = 16;
            this.btnLotEnd.TabStop = false;
            this.btnLotEnd.Values.DropDownArrowColor = System.Drawing.Color.Empty;
            this.btnLotEnd.Values.Text = "LOT END";
            // 
            // grpHistorySearch
            // 
            this.grpHistorySearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpHistorySearch.Controls.Add(this.historyFrom);
            this.grpHistorySearch.Controls.Add(this.historyTo);
            this.grpHistorySearch.Controls.Add(this.historyResults);
            this.grpHistorySearch.Controls.Add(this.historyFind);
            this.grpHistorySearch.Controls.Add(this.lblHistoryFrom);
            this.grpHistorySearch.Controls.Add(this.lblCompletedCount);
            this.grpHistorySearch.Controls.Add(this.lblHistoryTo);
            this.grpHistorySearch.Controls.Add(this.cboInspectionNumber);
            this.grpHistorySearch.Controls.Add(this.btnReview);
            this.grpHistorySearch.Location = new System.Drawing.Point(590, 544);
            this.grpHistorySearch.Name = "grpHistorySearch";
            this.grpHistorySearch.Size = new System.Drawing.Size(288, 298);
            this.grpHistorySearch.TabIndex = 21;
            this.grpHistorySearch.TabStop = false;
            this.grpHistorySearch.Text = "저장 결과 조회";
            // 
            // historyFrom
            // 
            this.historyFrom.CustomFormat = "yyyy-MM-dd";
            this.historyFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.historyFrom.Location = new System.Drawing.Point(44, 27);
            this.historyFrom.Name = "historyFrom";
            this.historyFrom.Size = new System.Drawing.Size(142, 23);
            this.historyFrom.TabIndex = 0;
            // 
            // historyTo
            // 
            this.historyTo.CustomFormat = "yyyy-MM-dd";
            this.historyTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.historyTo.Location = new System.Drawing.Point(44, 56);
            this.historyTo.Name = "historyTo";
            this.historyTo.Size = new System.Drawing.Size(142, 23);
            this.historyTo.TabIndex = 1;
            // 
            // historyResults
            // 
            this.historyResults.AllowUserToAddRows = false;
            this.historyResults.AllowUserToDeleteRows = false;
            this.historyResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.historyResults.BackgroundColor = System.Drawing.Color.White;
            this.historyResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Time,
            this.Model});
            this.historyResults.Location = new System.Drawing.Point(8, 101);
            this.historyResults.Name = "historyResults";
            this.historyResults.ReadOnly = true;
            this.historyResults.RowHeadersVisible = false;
            this.historyResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.historyResults.Size = new System.Drawing.Size(272, 142);
            this.historyResults.TabIndex = 2;
            // 
            // Time
            // 
            this.Time.HeaderText = "날짜";
            this.Time.Name = "Time";
            this.Time.ReadOnly = true;
            this.Time.Width = 90;
            // 
            // Model
            // 
            this.Model.HeaderText = "파일";
            this.Model.Name = "Model";
            this.Model.ReadOnly = true;
            this.Model.Width = 340;
            // 
            // historyFind
            // 
            this.historyFind.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(118)))), ((int)(((byte)(128)))));
            this.historyFind.FlatAppearance.BorderSize = 0;
            this.historyFind.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(94)))), ((int)(((byte)(104)))));
            this.historyFind.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(139)))), ((int)(((byte)(149)))));
            this.historyFind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.historyFind.ForeColor = System.Drawing.Color.White;
            this.historyFind.Location = new System.Drawing.Point(194, 27);
            this.historyFind.Name = "historyFind";
            this.historyFind.Size = new System.Drawing.Size(83, 53);
            this.historyFind.TabIndex = 3;
            this.historyFind.Text = "FIND";
            this.historyFind.UseVisualStyleBackColor = false;
            // 
            // lblHistoryFrom
            // 
            this.lblHistoryFrom.Location = new System.Drawing.Point(8, 30);
            this.lblHistoryFrom.Name = "lblHistoryFrom";
            this.lblHistoryFrom.Size = new System.Drawing.Size(38, 24);
            this.lblHistoryFrom.TabIndex = 5;
            this.lblHistoryFrom.Text = "시작";
            // 
            // lblHistoryTo
            // 
            this.lblHistoryTo.Location = new System.Drawing.Point(8, 59);
            this.lblHistoryTo.Name = "lblHistoryTo";
            this.lblHistoryTo.Size = new System.Drawing.Size(38, 24);
            this.lblHistoryTo.TabIndex = 6;
            this.lblHistoryTo.Text = "종료";
            // 
            // Result
            // 
            this.Result.HeaderText = "판정";
            this.Result.Name = "Result";
            this.Result.ReadOnly = true;
            this.Result.Width = 60;
            // 
            // Lot
            // 
            this.Lot.HeaderText = "LOT NO.";
            this.Lot.Name = "Lot";
            this.Lot.ReadOnly = true;
            // 
            // Peak
            // 
            this.Peak.HeaderText = "최대 하중(kgf)";
            this.Peak.Name = "Peak";
            this.Peak.ReadOnly = true;
            this.Peak.Width = 110;
            // 
            // btnStart
            // 
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(75)))), ((int)(((byte)(80)))));
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.btnStart.ForeColor = System.Drawing.Color.Yellow;
            this.btnStart.Location = new System.Drawing.Point(22, 778);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(274, 64);
            this.btnStart.TabIndex = 22;
            this.btnStart.Text = "STOP";
            this.btnStart.UseVisualStyleBackColor = false;
            // 
            // StationView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.btnLotEnd);
            this.Controls.Add(this.lblLog);
            this.Controls.Add(this.txtStationLog);
            this.Controls.Add(this.tblInspection);
            this.Controls.Add(this.chartLoad);
            this.Controls.Add(this.lblCurrentLoad);
            this.Controls.Add(this.lblGraph);
            this.Controls.Add(this.txtSpec);
            this.Controls.Add(this.lblSpec);
            this.Controls.Add(this.txtLotNumber);
            this.Controls.Add(this.lblLotNumber);
            this.Controls.Add(this.txtOperator);
            this.Controls.Add(this.lblOperator);
            this.Controls.Add(this.txtModel);
            this.Controls.Add(this.lblModel);
            this.Controls.Add(this.lblJobInfo);
            this.Controls.Add(this.pnlStationHeader);
            this.Controls.Add(this.grpHistorySearch);
            this.Controls.Add(this.btnStart);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.Name = "StationView";
            this.Size = new System.Drawing.Size(900, 866);
            this.pnlStationHeader.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chartLoad)).EndInit();
            this.tblInspection.ResumeLayout(false);
            this.grpHistorySearch.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.historyResults)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlStationHeader;
        private System.Windows.Forms.GroupBox grpHistorySearch;
        private System.Windows.Forms.DateTimePicker historyFrom;
        private System.Windows.Forms.DateTimePicker historyTo;
        private System.Windows.Forms.DataGridView historyResults;
        private System.Windows.Forms.Button historyFind;
        private System.Windows.Forms.Label lblHistoryFrom;
        private System.Windows.Forms.Label lblHistoryTo;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.DataGridViewTextBoxColumn historyColumnTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn historyColumnResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn historyColumnModel;
        private System.Windows.Forms.DataGridViewTextBoxColumn historyColumnLot;
        private System.Windows.Forms.DataGridViewTextBoxColumn historyColumnPeak;
        private System.Windows.Forms.Label lblStationState;
        private System.Windows.Forms.Label lblStationTitle;
        private System.Windows.Forms.Label lblJobInfo;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.TextBox txtModel;
        private System.Windows.Forms.Label lblOperator;
        private System.Windows.Forms.TextBox txtOperator;
        private System.Windows.Forms.Label lblLotNumber;
        private System.Windows.Forms.TextBox txtLotNumber;
        private System.Windows.Forms.Label lblSpec;
        private System.Windows.Forms.TextBox txtSpec;
        private System.Windows.Forms.Label lblGraph;
        private System.Windows.Forms.Label lblCurrentLoad;
        private System.Windows.Forms.Label lblCompletedCount;
        private System.Windows.Forms.ComboBox cboInspectionNumber;
        private System.Windows.Forms.Button btnReview;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartLoad;
        private System.Windows.Forms.TableLayoutPanel tblInspection;
        private System.Windows.Forms.Label lblInspectionHeader;
        private System.Windows.Forms.Label lblInspectionValueHeader;
        private System.Windows.Forms.Label lblCountCaption;
        private System.Windows.Forms.Label lblCountValue;
        private System.Windows.Forms.Label lblPassCaption;
        private System.Windows.Forms.Label lblPassValue;
        private System.Windows.Forms.Label lblFailCaption;
        private System.Windows.Forms.Label lblFailValue;
        private System.Windows.Forms.Label lblYieldCaption;
        private System.Windows.Forms.Label lblYieldValue;
        private System.Windows.Forms.Label lblVerdictCaption;
        private System.Windows.Forms.Label lblVerdictValue;
        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.TextBox txtStationLog;
        private Krypton.Toolkit.KryptonButton btnLotEnd;
        private System.Windows.Forms.DataGridViewTextBoxColumn Time;
        private System.Windows.Forms.DataGridViewTextBoxColumn Result;
        private System.Windows.Forms.DataGridViewTextBoxColumn Model;
        private System.Windows.Forms.DataGridViewTextBoxColumn Lot;
        private System.Windows.Forms.DataGridViewTextBoxColumn Peak;
    }
}



