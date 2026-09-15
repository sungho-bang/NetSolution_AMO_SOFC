$p='SOFCMeas/StationView.cs';$s=Get-Content $p -Raw
$start=$s.IndexOf('            btnReview.Click += async delegate');$end=$s.IndexOf('            btnStart.Click += delegate',$start)
$s=$s.Substring(0,$start)+@"
            btnReview.Enabled = false;
            btnReview.Click += delegate { ShowSelectedReview(); };
            historyResults.CellDoubleClick += async (sender, args) =>
            {
                if (args.RowIndex >= 0) await LoadReviewFileAsync(args.RowIndex);
            };
            historyResults.SelectionChanged += delegate
            {
                ++m_ReviewLoadVersion;
                m_ReviewInspections = null;
                cboInspectionNumber.Items.Clear();
                btnReview.Enabled = false;
            };
"@+$s.Substring($end)
$s=$s.Replace('        private string historyStation;',@"
        private string historyStation;
        private System.Collections.Generic.List<BufferedInspectionData> m_ReviewInspections;
        private int m_ReviewLoadVersion;
        private Chart m_ReviewChart;
        private string m_ReviewFileName;

        private async System.Threading.Tasks.Task LoadReviewFileAsync(int rowIndex)
        {
            var file = historyResults.Rows[rowIndex].Tag as LotFileEntry;
            if (file == null || historyStorage == null) return;
            int version = ++m_ReviewLoadVersion;
            m_ReviewInspections = null; cboInspectionNumber.Items.Clear(); btnReview.Enabled = false;
            historyStatus.Text = "검사 번호를 읽는 중...";
            try
            {
                var inspections = await System.Threading.Tasks.Task.Run(() => historyStorage.ReadLotFile(file, historyStation));
                if (IsDisposed || version != m_ReviewLoadVersion) return;
                m_ReviewInspections = inspections; m_ReviewFileName = file.FileName;
                foreach (var inspection in inspections) cboInspectionNumber.Items.Add(inspection.Record.DataRowNumber);
                if (cboInspectionNumber.Items.Count > 0) cboInspectionNumber.SelectedIndex = 0;
                btnReview.Enabled = inspections.Count > 0;
                historyStatus.Text = inspections.Count + "회 검사 로드 완료 · 번호 선택 후 REVIEW";
            }
            catch (Exception ex) { if (!IsDisposed && version == m_ReviewLoadVersion) historyStatus.Text = "읽기 실패: " + ex.Message; }
        }

        private void ShowSelectedReview()
        {
            if (m_ReviewInspections == null || cboInspectionNumber.SelectedIndex < 0) return;
            var inspection = m_ReviewInspections[cboInspectionNumber.SelectedIndex];
            var samples = inspection.Samples; var result = inspection.Record;
            ClearReviewGraph();
            var graph = new Chart { Name = "chartReview", Bounds = chartLoad.Bounds, Anchor = chartLoad.Anchor };
            var area = new ChartArea("Review"); graph.ChartAreas.Add(area);
            area.AxisX.Minimum = 0; area.AxisX.Maximum = Math.Max(m_XMax, samples.Count == 0 ? 0 : samples.Max(s => s.Number)) * 1.1;
            area.AxisY.Minimum = samples.Count == 0 ? 0 : Math.Min(0, samples.Min(s => s.LoadKgf) * 1.1);
            area.AxisY.Maximum = Math.Max(0.001, Math.Max(result.LowerSpecKgf, result.PeakLoadKgf)) * 1.1;
            area.AxisY.Interval = (area.AxisY.Maximum - area.AxisY.Minimum) / 10D;
            ConfigureAxisLabels(area);
            var series = new Series("하중") { ChartType = SeriesChartType.Line, BorderWidth = 2, Color = Color.Teal };
            graph.Series.Add(series);
            foreach (var sample in samples) series.Points.AddXY(sample.Number, sample.LoadKgf);
            graph.Titles.Add("REVIEW 검사 " + result.DataRowNumber + " · " + result.Result + " · 최대 " + result.PeakLoadKgf.ToString("0.000") + " kgf");
            graph.Titles.Add(m_ReviewFileName);
            m_ReviewChart = graph; Controls.Add(graph); graph.BringToFront();
        }

        private void ClearReviewGraph()
        {
            if (m_ReviewChart == null) return;
            Controls.Remove(m_ReviewChart); m_ReviewChart.Dispose(); m_ReviewChart = null;
        }
"@)
$s=$s.Replace('        internal void ResetLoadGraph()'+"`r`n"+'        {','        internal void ResetLoadGraph()'+"`r`n"+'        {'+"`r`n"+'            ClearReviewGraph();')
$s=$s.Replace('                CollectionEnabled = !CollectionEnabled;','                CollectionEnabled = !CollectionEnabled;'+"`r`n"+'                if (CollectionEnabled) ClearReviewGraph();')
$s=$s.Replace('            lblCompletedCount.Text = "검사 횟수: " + m_InspectionCount.ToString(CultureInfo.InvariantCulture);','            lblCompletedCount.Text = "검사 번호";')
Set-Content $p $s -Encoding UTF8
