using System;
using System.ComponentModel;
using System.Globalization;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SOFCMeas
{
    public partial class StationView : UserControl
    {
        private static readonly Color PassColor = Color.FromArgb(0, 151, 86);
        private static readonly Color PassBackColor = Color.FromArgb(226, 242, 233);
        private static readonly Color FailColor = Color.FromArgb(220, 53, 69);
        private static readonly Color FailBackColor = Color.FromArgb(252, 232, 235);
        private static readonly Color InvalidColor = Color.FromArgb(243, 156, 18);
        private static readonly Color InvalidBackColor = Color.FromArgb(255, 245, 224);
        private static readonly Regex SpecNumberPattern = new Regex(
            @"(?<![\d.,])[-+]?(?:\d+(?:[.,]\d*)?|[.,]\d+)",
            RegexOptions.Compiled);
        private const string AxisBootstrapSeriesName = "AxisBootstrap";
        private const string YAxisCaptionTitleName = "YAxisCaption";
        private const string LivePeakSeriesName = "LivePeak";

        private int m_GraphReadCount;
        private int m_MaximumGraphPoints;
        private bool m_GraphFrozen;
        private string m_LoadDeviceText = "D700";
        private ushort[] m_LastLoadWords;
        private double m_LastLoadKgf;
        private bool m_HasLoadSample;
        private double m_PeakLoadKgf;
        private int m_PeakSampleNumber;
        private int m_InspectionCount;
        private int m_PassCount;
        private int m_FailCount;
        private bool m_JobInformationEditing;
        private bool m_LotPrepared;
        private int m_BufferedInspectionCount;
        private bool m_AutoX = true, m_AutoY = true;
        private double m_XMin = 0, m_XMax = 60, m_XInterval = 6.6;
        private double m_YMin = 0, m_YMax = 0.4, m_YInterval = 0.2;
        private double m_InspectionThresholdKgf = 0.4;
        internal int LotSaveCount = 600;
        internal bool CollectionEnabled { get; private set; }
        internal event EventHandler CollectionEnabledChanged;
        private InspectionStorageService historyStorage;
        private string historyStation;
        private System.Collections.Generic.List<BufferedInspectionData> m_ReviewInspections;
        private int m_ReviewLoadVersion;
        private Chart m_ReviewChart;
        private bool m_ShowingReviewSummary;
        private string m_LiveVerdict = "-";
        private Color m_LiveVerdictForeground = Color.FromArgb(96, 112, 128);
        private Color m_LiveVerdictBackground = Color.FromArgb(238, 242, 245);

        private async System.Threading.Tasks.Task LoadReviewFileAsync(int rowIndex)
        {
            var file = historyResults.Rows[rowIndex].Tag as LotFileEntry;
            if (file == null || historyStorage == null) return;
            int version = ++m_ReviewLoadVersion;
            m_ReviewInspections = null; cboInspectionNumber.Items.Clear(); btnReview.Enabled = false;
            AppendLog(DateTime.Now, "검사 번호를 읽는 중...");
            try
            {
                var inspections = await System.Threading.Tasks.Task.Run(() => historyStorage.ReadLotFile(file, historyStation));
                if (IsDisposed || version != m_ReviewLoadVersion) return;
                m_ReviewInspections = inspections;
                foreach (var inspection in inspections) cboInspectionNumber.Items.Add(inspection.Record.DataRowNumber);
                if (cboInspectionNumber.Items.Count > 0) cboInspectionNumber.SelectedIndex = 0;
                btnReview.Enabled = inspections.Count > 0;
                AppendLog(DateTime.Now, inspections.Count + "회 검사 로드 완료 · 번호 선택 후 REVIEW");
            }
            catch (Exception ex) { if (!IsDisposed && version == m_ReviewLoadVersion) AppendLog(DateTime.Now, "읽기 실패: " + ex.Message); }
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
            area.AxisX.Interval = (area.AxisX.Maximum - area.AxisX.Minimum) / 10D;
            area.AxisY.Minimum = samples.Count == 0 ? 0 : Math.Min(0, samples.Min(s => s.LoadKgf) * 1.1);
            area.AxisY.Maximum = Math.Max(0.001, Math.Max(result.LowerSpecKgf, result.PeakLoadKgf)) * 1.1;
            area.AxisY.Interval = (area.AxisY.Maximum - area.AxisY.Minimum) / 10D;
            ConfigureAxisLabels(area);
            var series = new Series("하중") { ChartType = SeriesChartType.Line, BorderWidth = 2, Color = historyStation == "#2" ? Color.DarkOrange : Color.Teal };
            graph.Series.Add(series);
            foreach (var sample in samples) series.Points.AddXY(sample.Number, sample.LoadKgf);
            EnsureAxisBootstrapSeries(graph, area);
            EnsureYAxisCaptionTitle(graph, area);
            if (samples.Count > 0)
            {
                var peak = samples.OrderByDescending(sample => sample.LoadKgf).First();
                var marker = new Series("최대값") {
                    ChartType = SeriesChartType.Point, MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 20, MarkerColor = Color.FromArgb(85, 255, 0, 0),
                    MarkerBorderColor = Color.FromArgb(180, 220, 0, 0), MarkerBorderWidth = 2,
                    LabelForeColor = Color.Firebrick, Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                };
                marker.Points.AddXY(peak.Number, peak.LoadKgf);
                marker.Points[0].Label = "최대 " + FormatSignedLoad(peak.LoadKgf) + " kgf";
                graph.Series.Add(marker);
            }
            m_ReviewChart = graph; Controls.Add(graph); chartLoad.Hide(); graph.Show(); graph.BringToFront();
            UpdateReviewSummary();
        }

        private void UpdateReviewSummary()
        {
            if (m_ReviewInspections == null || cboInspectionNumber.SelectedIndex < 0) return;
            m_ShowingReviewSummary = true;
            int total = m_ReviewInspections.Count;
            int good = m_ReviewInspections.Count(i => i.Record.Result == "GOOD" || i.Record.Result == "PASS");
            int ng = total - good;
            lblInspectionHeader.Text = "검사현황 (REVIEW)";
            lblCountValue.Text = total.ToString(CultureInfo.InvariantCulture);
            lblPassValue.Text = good.ToString(CultureInfo.InvariantCulture);
            lblFailValue.Text = ng.ToString(CultureInfo.InvariantCulture);
            lblYieldValue.Text = (total == 0 ? 0 : 100D * good / total).ToString("0.00", CultureInfo.InvariantCulture);
            string verdict = m_ReviewInspections[cboInspectionNumber.SelectedIndex].Record.Result;
            bool pass = verdict == "GOOD" || verdict == "PASS";
            DrawVerdict(verdict, pass ? PassColor : verdict == "INVALID" ? InvalidColor : FailColor,
                pass ? PassBackColor : verdict == "INVALID" ? InvalidBackColor : FailBackColor);
        }

        private void ClearReviewGraph()
        {
            m_ShowingReviewSummary = false;
            lblInspectionHeader.Text = "검사현황";
            UpdateInspectionSummary(m_InspectionCount == 0 ? 0 : m_PassCount * 100D / m_InspectionCount);
            DrawVerdict(m_LiveVerdict, m_LiveVerdictForeground, m_LiveVerdictBackground);
            if (m_ReviewChart == null) return;
            Controls.Remove(m_ReviewChart); m_ReviewChart.Dispose(); m_ReviewChart = null;
            chartLoad.Show();
        }

        internal void BindHistory(InspectionStorageService storage, string station)
        { historyStorage = storage; historyStation = station; }

        private void InitializeHistorySearch()
        {
            historyFrom.Value = DateTime.Today.AddDays(-6); historyTo.Value = DateTime.Today;
            historyFind.Click += async delegate
            {
                var from = historyFrom.Value.Date; var to = historyTo.Value.Date;
                if (from > to) { AppendLog(DateTime.Now, "시작일은 종료일보다 늦을 수 없습니다."); return; }
                if (historyStorage == null) { AppendLog(DateTime.Now, "저장 서비스를 준비 중입니다."); return; }
                historyFind.Enabled = false; AppendLog(DateTime.Now, "조회 중..."); historyResults.Rows.Clear();
                try
                {
                    var records = await System.Threading.Tasks.Task.Run(() => historyStorage.GetLotFiles(from, to, historyStation));
                    if (IsDisposed) return;
                    foreach (var record in records)
                    {
                        int index = historyResults.Rows.Add(record.Date.ToString("yyyy-MM-dd"), record.FileName);
                        historyResults.Rows[index].Tag = record;
                    }
                    AppendLog(DateTime.Now, records.Count == 0 ? "해당 기간의 저장 결과가 없습니다." : records.Count + "건 조회 완료");
                }
                catch (Exception ex) { if (!IsDisposed) AppendLog(DateTime.Now, "조회 실패: " + ex.Message); }
                finally { if (!IsDisposed) historyFind.Enabled = true; }
            };
        }

        internal void ConfigureGraph(int station)
        {
            var settings = SettingsCatalog.Load();
            string prefix = "Station" + station + ".Graph.";
            double specMaximum = SettingsCatalog.Number(
                settings,
                "Station" + station + ".Inspection.ThresholdKgf");
            chartLoad.Series[0].Color = station == 2 ? Color.DarkOrange : Color.Teal;
            m_AutoX = bool.Parse(settings[prefix + "XAuto"]);
            m_AutoY = bool.Parse(settings[prefix + "YAuto"]);
            m_XMin = SettingsCatalog.Number(settings, prefix + "XMin");
            m_XMax = SettingsCatalog.Number(settings, prefix + "XMax");
            m_XInterval = SettingsCatalog.Number(settings, prefix + "XInterval");
            m_YMin = SettingsCatalog.Number(settings, prefix + "YMin");
            m_YMax = SettingsCatalog.Number(settings, prefix + "YMax");
            m_YInterval = SettingsCatalog.Number(settings, prefix + "YInterval");
            LotSaveCount = (int)SettingsCatalog.Number(settings, "Station" + station + ".Lot.SaveCount");
            SetInspectionThreshold(specMaximum);
            ResetLoadGraph();
        }

        public StationView()
        {
            InitializeComponent();
            SetJobInformationEditing(true);
            InitializeHistorySearch();
            btnReview.Enabled = false;
            btnReview.ForeColor = Color.White;
            btnReview.Paint += delegate(object sender, PaintEventArgs e)
            {
                if (btnReview.Enabled) return;
                // Standard disabled buttons ignore ForeColor and render gray text.
                using (var background = new SolidBrush(btnReview.BackColor))
                    e.Graphics.FillRectangle(background, btnReview.ClientRectangle);
                TextRenderer.DrawText(e.Graphics, btnReview.Text, btnReview.Font,
                    btnReview.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            };
            btnReview.Click += delegate { ShowSelectedReview(); };
            historyResults.CellClick += async (sender, args) =>
            {
                if (args.RowIndex >= 0) await LoadReviewFileAsync(args.RowIndex);
            };
            cboInspectionNumber.SelectedIndexChanged += delegate
            {
                // Once REVIEW is open, change the curve and verdict together.
                // Merely loading a file must not replace the live verdict.
                if (m_ReviewChart != null) ShowSelectedReview();
            };
            historyResults.SelectionChanged += delegate
            {
                ++m_ReviewLoadVersion;
                m_ReviewInspections = null;
                cboInspectionNumber.Items.Clear();
                btnReview.Enabled = false;
                ClearReviewGraph();
            };
            btnStart.Click += async delegate
            {
                if (IsSimulationRun)
                {
                    if (m_SimulationCancellation != null) m_SimulationCancellation.Cancel();
                    return;
                }
                if (!CollectionEnabled && SettingsCatalog.Load()["Runtime.Simulation"] == "1")
                {
                    m_SimulationTask = RunSimulationAsync();
                    await m_SimulationTask;
                    return;
                }
                SetCollectionState(!CollectionEnabled);
            };
            Disposed += delegate { if (m_SimulationCancellation != null) m_SimulationCancellation.Cancel(); };
            MatchInputLabel(lblModel, txtModel);
            MatchInputLabel(lblOperator, txtOperator);
            MatchInputLabel(lblLotNumber, txtLotNumber);
            MatchInputLabel(lblSpec, txtSpec);
            ResetInspectionSummary();
            btnLotEnd.Enabled = false;
            btnLotEnd.Click += delegate { RaiseLotEndRequested(); };
        }

        internal bool IsSimulationRun { get; private set; }
        private System.Threading.CancellationTokenSource m_SimulationCancellation;
        private System.Threading.Tasks.Task m_SimulationTask;
        private bool m_SimulationCompleted;
        internal event Action<BufferedInspectionData> SimulationCompleted;
        internal async System.Threading.Tasks.Task StopSimulationAsync()
        {
            if (m_SimulationCancellation != null) m_SimulationCancellation.Cancel();
            if (m_SimulationTask != null) await m_SimulationTask;
            if (m_SimulationCompleted) SetCollectionState(false);
        }

        internal void ClearPendingSimulationList()
        {
            m_ReviewInspections = null;
            cboInspectionNumber.Items.Clear();
            btnReview.Enabled = false;
            ClearReviewGraph();
        }

        private void SetCollectionState(bool enabled)
        {
            CollectionEnabled = enabled;
            if (!enabled) m_SimulationCompleted = false;
            if (enabled) ClearReviewGraph();
            btnStart.Text = enabled ? "START" : "STOP";
            btnStart.BackColor = enabled ? Color.FromArgb(0, 151, 86) : Color.FromArgb(170, 75, 80);
            btnStart.ForeColor = enabled ? Color.White : Color.Yellow;
            if (CollectionEnabledChanged != null) CollectionEnabledChanged(this, EventArgs.Empty);
        }

        private async System.Threading.Tasks.Task RunSimulationAsync(int intervalMilliseconds = 300)
        {
            if (IsSimulationRun || historyStorage == null) return;
            string error;
            if (!TryValidateJobInformation(out error)) { MessageBox.Show(this, error, "시뮬레이션"); return; }
            IsSimulationRun = true;
            m_SimulationCompleted = false;
            var cancellation = new System.Threading.CancellationTokenSource();
            m_SimulationCancellation = cancellation;
            try
            {
                var settings = SettingsCatalog.Load();
                string stationKey = historyStation == "#2" ? "Station2." : "Station1.";
                double scale = SettingsCatalog.Number(settings, stationKey + "Plc.LoadScale");
                SetInspectionThreshold(SettingsCatalog.Number(settings, stationKey + "Inspection.ThresholdKgf"));
                double threshold = m_InspectionThresholdKgf;
                string model = FileName, lotNumber = LotNumber, operatorName = OperatorName, spec = SpecText;
                SetCollectionState(true); ResetLoadGraph();
                StateText = "SIMULATION"; StateColor = Color.DarkOrange;

                    DateTime started = DateTime.Now;
                    var samples = new System.Collections.Generic.List<InspectionSample>();
                    var random = new Random(); double peak = Math.Max(0.01, threshold) * (1.1 + random.NextDouble() * 0.3);
                    AppendLog(started, "SIMULATION 시작: 300ms 간격, 60회 가상 하중 수집");
                    for (int i = 0; i < 60; i++)
                    {
                        await System.Threading.Tasks.Task.Delay(intervalMilliseconds, cancellation.Token);
                        cancellation.Token.ThrowIfCancellationRequested();
                        double t = i / 59D;
                        double crest = historyStation == "#2" ? 0.67 : 0.86;
                        double u = t <= crest ? t / crest : (t - crest) / (1 - crest);
                        double smooth = u * u * (3 - 2 * u);
                        double shape = t <= crest ? smooth : 1 - (historyStation == "#2" ? 0.9 : 0.08) * smooth;
                        int raw = checked((int)Math.Round((0.01 + peak * shape) * scale));
                        double kgf = raw / scale;
                        samples.Add(new InspectionSample { Number = i + 1, RawValue = raw, LoadKgf = kgf, ReadTime = DateTime.Now });
                        if (i == 0) ResetLoadGraph();
                        AddLoadSample(kgf, raw);
                    }
                    var result = CompleteInspection();
    
    
                    var record = new InspectionLogRecord { Time = DateTime.Now, StartedAt = started,
                        Station = historyStation, LotNumber = lotNumber, FileName = model, OperatorName = operatorName,
                        Result = result.IsValid ? (result.IsPass ? "GOOD" : "NG") : "INVALID",
                        PeakLoadKgf = result.PeakLoadKgf, LowerSpecKgf = result.LowerSpecKgf, UpperSpecKgf = result.UpperSpecKgf,
                        SampleCount = samples.Count, Message = "SIMULATION: 60 generated samples; not PLC measurements." };
                    record.DataRowNumber = result.InspectionCount;
                    var completed = new BufferedInspectionData { Record = record, Samples = samples };
                    if (SimulationCompleted != null) SimulationCompleted(completed);
                    AppendLog(DateTime.Now, "SIMULATION END_REQ: 60회 완료 / 검사 " + record.DataRowNumber + " 메모리 누적 (LOT END 시 저장)");
                    m_SimulationCompleted = true;
                    StateText = "COMPLETE";
            }
            catch (OperationCanceledException) { if (!IsDisposed) { ResetLoadGraph(); m_LastLoadKgf = 0; UpdateCurrentLoadText(); AppendLog(DateTime.Now, "SIMULATION STOP: 진행 중 데이터 초기화 / 완료 검사 " + m_InspectionCount + "회 유지"); } }
            catch (Exception ex) { if (!IsDisposed) { AppendLog(DateTime.Now, "SIMULATION 오류: " + ex.Message); MessageBox.Show(this, ex.Message, "시뮬레이션 오류", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
            finally
            {
                if (!IsDisposed) { if (!m_SimulationCompleted) { SetCollectionState(false); StateText = "STOP"; } btnStart.Enabled = true; }
                IsSimulationRun = false; m_SimulationCancellation = null; cancellation.Dispose();
            }
        }
        internal event EventHandler CreateFileRequested;
        private void MatchInputLabel(Label label, TextBox input)
        {
            EventHandler align = delegate
            {
                label.Top = input.Top;
                label.Height = input.Height;
            };
            input.SizeChanged += align;
            input.LocationChanged += align;
            // A sibling label can be scaled after the input's change events.
            // Align again after the container has finished laying out children.
            Layout += delegate { align(input, EventArgs.Empty); };
            align(input, EventArgs.Empty);
        }

        internal event EventHandler LotEndRequested;

        internal bool JobInformationEditing
        {
            get { return m_JobInformationEditing; }
        }

        [Category("SOFC")]
        public string StationTitle
        {
            get { return lblStationTitle.Text; }
            set { lblStationTitle.Text = value; }
        }

        [Category("SOFC")]
        public string StateText
        {
            get { return lblStationState.Text; }
            set { lblStationState.Text = value; }
        }

        [Category("SOFC")]
        public Color StateColor
        {
            get { return lblStationState.BackColor; }
            set { lblStationState.BackColor = value; }
        }

        [Category("SOFC")]
        public string FileName
        {
            get { return txtModel.Text; }
            set { txtModel.Text = value; }
        }

        [Category("SOFC")]
        public string OperatorName
        {
            get { return txtOperator.Text; }
            set { txtOperator.Text = value; }
        }

        [Category("SOFC")]
        public string LotNumber
        {
            get { return txtLotNumber.Text; }
            set { txtLotNumber.Text = value; }
        }

        [Category("SOFC")]
        public string SpecText
        {
            get { return m_InspectionThresholdKgf.ToString("R", CultureInfo.InvariantCulture); }
            set
            {
                double lower, upper;
                if (!TryParseSpecRange(value, out lower, out upper))
                    throw new ArgumentException("SPEC 기준값이 올바르지 않습니다.");
                SetInspectionThreshold(lower);
            }
        }

        private void SetInspectionThreshold(double threshold)
        {
            if (double.IsNaN(threshold) || double.IsInfinity(threshold) || threshold < 0)
                throw new ArgumentOutOfRangeException(nameof(threshold));
            m_InspectionThresholdKgf = threshold;
            txtSpec.Text = threshold.ToString("R", CultureInfo.InvariantCulture);
        }

        [Category("SOFC")]
        public Color GraphColor
        {
            get { return chartLoad.Series[0].Color; }
            set { chartLoad.Series[0].Color = value; }
        }

        [Category("SOFC")]
        public string LogText
        {
            get { return txtStationLog.Text; }
            set { txtStationLog.Text = value; }
        }

        [Category("SOFC")]
        [DefaultValue(0)]
        public int MaximumGraphPoints
        {
            get { return m_MaximumGraphPoints; }
            set { m_MaximumGraphPoints = Math.Max(0, value); }
        }

        internal void ResetLoadGraph()
        {
            ClearReviewGraph();
            RemoveLivePeakMarker();
            Series series = chartLoad.Series[0];
            series.Points.Clear();
            m_GraphReadCount = 0;
            m_GraphFrozen = false;
            m_HasLoadSample = false;
            m_PeakLoadKgf = 0D;
            m_PeakSampleNumber = 0;

            ChartArea chartArea = chartLoad.ChartAreas[0];
            chartArea.AxisX.Minimum = m_XMin;
            chartArea.AxisX.Maximum = m_AutoX ? m_XMin + (m_XMax - m_XMin) * 1.1D : m_XMax;
            chartArea.AxisX.Interval = m_AutoX
                ? (chartArea.AxisX.Maximum - chartArea.AxisX.Minimum) / 10D
                : m_XInterval;
            chartArea.AxisY.Minimum = m_YMin;
            chartArea.AxisY.Maximum = m_AutoY ? m_YMin + (m_YMax - m_YMin) * 1.1D : m_YMax;
            chartArea.AxisY.Interval = m_YInterval;
            ConfigureAxisLabels(chartArea);
            EnsureAxisBootstrapSeries(chartLoad, chartArea);
            EnsureYAxisCaptionTitle(chartLoad, chartArea);

            m_LastLoadWords = null;
            m_LastLoadKgf = 0D;
            UpdateCurrentLoadText();
            chartLoad.Invalidate();
        }

        internal void AddLoadSample(double loadKgf, int? rawValue = null)
        {
            AddLoadSampleCore(loadKgf, null);
        }

        internal void AddPlcLoadSample(double loadKgf, int rawValue, ushort[] loadWords)
        {
            if (loadWords == null || loadWords.Length != PlcLoadDecoder.WordCount)
                throw new ArgumentException("하중 데이터는 연속 5 WORD여야 합니다.", nameof(loadWords));
            AddLoadSampleCore(loadKgf, loadWords);
        }

        private void AddLoadSampleCore(double loadKgf, ushort[] loadWords)
        {
            if (m_GraphFrozen || double.IsNaN(loadKgf) || double.IsInfinity(loadKgf))
            {
                return;
            }

            Series series = chartLoad.Series[0];
            int readNumber = ++m_GraphReadCount;
            series.Points.AddXY(readNumber, loadKgf);

            if (!m_HasLoadSample || loadKgf > m_PeakLoadKgf)
            {
                m_PeakLoadKgf = loadKgf;
                m_PeakSampleNumber = readNumber;
                m_HasLoadSample = true;
            }

            while (m_MaximumGraphPoints > 0 &&
                   series.Points.Count > m_MaximumGraphPoints)
            {
                series.Points.RemoveAt(0);
            }

            AdjustXAxis(series, readNumber);
            AdjustYAxis(loadKgf);
            m_LastLoadWords = loadWords == null ? null : (ushort[])loadWords.Clone();
            m_LastLoadKgf = loadKgf;
            UpdateCurrentLoadText();
            chartLoad.Invalidate();
        }

        internal void FreezeLoadGraph()
        {
            m_GraphFrozen = true;
            chartLoad.Invalidate();
        }

        internal StationInspectionResult CompleteInspection()
        {
            FreezeLoadGraph();

            if (!m_HasLoadSample)
            {
                ApplyVerdict("INVALID", InvalidColor, InvalidBackColor);
                return CreateInvalidResult("수집된 하중 샘플이 없습니다.");
            }

            double lowerSpec = m_InspectionThresholdKgf;
            double upperSpec = lowerSpec;

            double peakLoad = m_PeakLoadKgf;
            bool isPass = peakLoad >= lowerSpec;
            ShowLivePeakMarker();

            m_InspectionCount++;
            if (isPass)
            {
                m_PassCount++;
                ApplyVerdict("GOOD", PassColor, PassBackColor);
            }
            else
            {
                m_FailCount++;
                ApplyVerdict("NG", FailColor, FailBackColor);
            }

            double yieldPercent = m_InspectionCount == 0
                ? 0D
                : m_PassCount * 100D / m_InspectionCount;

            UpdateInspectionSummary(yieldPercent);

            return new StationInspectionResult
            {
                IsValid = true,
                IsPass = isPass,
                PeakLoadKgf = peakLoad,
                LowerSpecKgf = lowerSpec,
                UpperSpecKgf = upperSpec,
                InspectionCount = m_InspectionCount,
                PassCount = m_PassCount,
                FailCount = m_FailCount,
                YieldPercent = yieldPercent
            };
        }

        private void ShowLivePeakMarker()
        {
            RemoveLivePeakMarker();
            if (!m_HasLoadSample || m_PeakSampleNumber < 1)
            {
                return;
            }

            Series marker = new Series(LivePeakSeriesName)
            {
                ChartArea = chartLoad.ChartAreas[0].Name,
                ChartType = SeriesChartType.Point,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 20,
                MarkerColor = Color.FromArgb(85, 255, 0, 0),
                MarkerBorderColor = Color.FromArgb(180, 220, 0, 0),
                MarkerBorderWidth = 2,
                LabelForeColor = Color.Firebrick,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                IsVisibleInLegend = false
            };
            marker.SmartLabelStyle.Enabled = true;
            marker.Points.AddXY(m_PeakSampleNumber, m_PeakLoadKgf);
            marker.Points[0].Label = "최대 " +
                FormatSignedLoad(m_PeakLoadKgf) +
                " kgf";
            chartLoad.Series.Add(marker);
            chartLoad.Invalidate();
        }

        private void RemoveLivePeakMarker()
        {
            Series marker = chartLoad.Series.FindByName(LivePeakSeriesName);
            if (marker != null)
            {
                chartLoad.Series.Remove(marker);
            }
        }

        internal void ResetInspectionSummary()
        {
            m_InspectionCount = 0;
            m_PassCount = 0;
            m_FailCount = 0;
            UpdateInspectionSummary(0D);
            ApplyVerdict("-", Color.FromArgb(96, 112, 128), Color.FromArgb(238, 242, 245));
        }

        internal void SetLoadDeviceName(string deviceAddress)
        {
            m_LoadDeviceText = string.IsNullOrWhiteSpace(deviceAddress)
                ? "LOAD_RAW"
                : deviceAddress.Trim().ToUpperInvariant();
            UpdateCurrentLoadText();
        }

        private void UpdateCurrentLoadText()
        {
            // PLC ASCII words are separate from the scaled internal raw value.
            string words = m_LastLoadWords == null ? string.Concat(Enumerable.Repeat("[0000]", PlcLoadDecoder.WordCount))
                : string.Concat(m_LastLoadWords.Select(word => "[" + word.ToString(CultureInfo.InvariantCulture) + "]"));
            // The frame sign is authoritative even when the magnitude is zero.
            bool negative = m_LastLoadWords != null
                ? (m_LastLoadWords[1] & 0xFF) == '-'
                : BitConverter.DoubleToInt64Bits(m_LastLoadKgf) < 0;
            string signedLoad = (negative ? "-" : "+") + Math.Abs(m_LastLoadKgf).ToString("0.000", CultureInfo.InvariantCulture);
            lblCurrentLoad.Text = string.Format(CultureInfo.InvariantCulture,
                "{0}{1}\r\n{2} kgf  |  검사 횟수 {3}",
                m_LoadDeviceText, words, signedLoad, m_InspectionCount);
        }

        private static string FormatSignedLoad(double loadKgf)
        {
            return (BitConverter.DoubleToInt64Bits(loadKgf) < 0 ? "-" : "+")
                + Math.Abs(loadKgf).ToString("0.000", CultureInfo.InvariantCulture);
        }

        internal void SetJobInformationEditing(bool editing)
        {
            // Job metadata stays editable even during collection and LOT saving.
            m_JobInformationEditing = true;
            txtModel.ReadOnly = false;
            txtOperator.ReadOnly = false;
            txtLotNumber.ReadOnly = false;
            txtModel.TabStop = txtOperator.TabStop = txtLotNumber.TabStop = true;
            txtSpec.ReadOnly = true;
        }

        internal void SetBufferedInspectionSummary(
            System.Collections.Generic.IReadOnlyList<BufferedInspectionData> inspections)
        {
            m_InspectionCount = inspections.Count;
            m_PassCount = inspections.Count(i =>
                string.Equals(i.Record.Result, "GOOD", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(i.Record.Result, "PASS", StringComparison.OrdinalIgnoreCase));
            m_FailCount = m_InspectionCount - m_PassCount;
            UpdateInspectionSummary(m_InspectionCount == 0 ? 0 : m_PassCount * 100D / m_InspectionCount);
        }

        internal void SetLotPrepared(bool prepared, int inspectionCount)
        {
            m_LotPrepared = prepared;
            m_BufferedInspectionCount = inspectionCount;
            SetJobInformationEditing(false);
            btnLotEnd.Enabled = prepared && inspectionCount > 0;
            btnLotEnd.Values.Text = prepared
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "LOT 종료 ({0}/{1})",
                    inspectionCount, LotSaveCount)
                : "LOT 종료";
        }

        internal void SetLotSaving(bool saving)
        {
            btnStart.Enabled = !saving;
            btnLotEnd.Enabled = !saving &&
                                m_LotPrepared &&
                                m_BufferedInspectionCount > 0;
            btnLotEnd.Values.Text = saving
                ? "LOT 저장 중..."
                : m_LotPrepared
                    ? string.Format(
                        CultureInfo.InvariantCulture,
                        "LOT 종료 ({0}/{1})",
                        m_BufferedInspectionCount, LotSaveCount)
                    : "LOT 종료";
        }

        internal void SetPlcCollectionState(bool collecting)
        {
        }

        internal bool TryValidateJobInformation(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(FileName))
            {
                errorMessage = "파일명을 입력하세요.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(OperatorName))
            {
                errorMessage = "작업자명을 입력하세요.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(LotNumber))
            {
                errorMessage = "LOT NO.를 입력하세요.";
                return false;
            }

            double lowerSpec;
            double upperSpec;
            if (!TryParseSpecRange(SpecText, out lowerSpec, out upperSpec))
            {
                errorMessage = "SPEC 기준값이 올바르지 않습니다. 예: 0.40";
                return false;
            }

            errorMessage = null;
            return true;
        }

        internal void AppendLog(DateTime time, string message)
        {
            string line = string.Format("{0:HH:mm:ss.fff}  {1}", time, message);

            if (txtStationLog.Text == "PLC 연결 대기...")
            {
                txtStationLog.Clear();
            }

            if (txtStationLog.TextLength > 0)
            {
                txtStationLog.AppendText(Environment.NewLine);
            }

            txtStationLog.AppendText(line);

            string[] lines = txtStationLog.Lines;
            if (lines.Length > 100)
            {
                txtStationLog.Lines = lines.Skip(lines.Length - 100).ToArray();
            }

            txtStationLog.SelectionStart = txtStationLog.TextLength;
            txtStationLog.ScrollToCaret();
        }

        private static void ConfigureAxisLabels(ChartArea area)
        {
            area.AxisX.Enabled = AxisEnabled.True;
            area.AxisY.Enabled = AxisEnabled.True;
            area.AxisX.LabelStyle.Enabled = true;
            area.AxisY.LabelStyle.Enabled = true;
            area.AxisY.IsLabelAutoFit = false;
            area.AxisY.IsMarginVisible = false;
            area.AxisY.LabelStyle.IsEndLabelVisible = true;
            area.AxisY.LabelStyle.Font = new Font("맑은 고딕", 9F);
            area.AxisX.LabelStyle.ForeColor = Color.Black;
            area.AxisY.LabelStyle.ForeColor = Color.Black;
            area.AxisY.LabelStyle.Format = "0.###";
            area.AxisY.Title = string.Empty;
            area.AxisX.Title = "샘플 횟수";
            area.AxisX.MajorGrid.Enabled = true;
            area.AxisY.MajorGrid.Enabled = true;
            area.AxisY.MajorTickMark.Enabled = true;
            if (double.IsNaN(area.AxisX.Interval) || area.AxisX.Interval <= 0D)
                area.AxisX.Interval = (area.AxisX.Maximum - area.AxisX.Minimum) / 10D;
            area.AxisY.Interval = (area.AxisY.Maximum - area.AxisY.Minimum) / 10D;
            area.AxisX.MajorGrid.Interval = area.AxisX.Interval;
            area.AxisX.MajorTickMark.Interval = area.AxisX.Interval;
            area.AxisY.MajorTickMark.Interval = area.AxisY.Interval;
            area.AxisX.LabelStyle.Interval = area.AxisX.Interval;
            area.AxisX.LabelStyle.Format = "0.##";
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(225, 228, 232);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(225, 228, 232);
            area.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            area.AxisX.MinorGrid.Enabled = false;
            area.AxisY.MinorGrid.Enabled = false;
            area.AxisY.MajorGrid.Interval = area.AxisY.Interval;
            area.AxisY.LabelStyle.Interval = area.AxisY.Interval;
        }

        private static void EnsureAxisBootstrapSeries(Chart chart, ChartArea area)
        {
            Series bootstrap = chart.Series.FindByName(AxisBootstrapSeriesName);
            if (bootstrap == null)
            {
                bootstrap = new Series(AxisBootstrapSeriesName)
                {
                    ChartArea = area.Name,
                    ChartType = SeriesChartType.Point,
                    Color = Color.Transparent,
                    MarkerStyle = MarkerStyle.None,
                    IsVisibleInLegend = false,
                    IsValueShownAsLabel = false
                };
                chart.Series.Add(bootstrap);
            }

            bootstrap.Points.Clear();
            bootstrap.Points.AddXY(area.AxisX.Minimum, area.AxisY.Minimum);
        }

        private static void EnsureYAxisCaptionTitle(Chart chart, ChartArea area)
        {
            Title caption = chart.Titles.FindByName(YAxisCaptionTitleName);
            if (caption == null)
            {
                caption = new Title { Name = YAxisCaptionTitleName };
                chart.Titles.Add(caption);
            }

            caption.Text = "하중 (kgf)";
            caption.Font = new Font("맑은 고딕", 9F);
            caption.ForeColor = Color.Black;
            caption.TextOrientation = TextOrientation.Horizontal;
            caption.DockedToChartArea = area.Name;
            caption.IsDockedInsideChartArea = false;
            caption.Docking = Docking.Top;
            caption.DockingOffset = 0;
            caption.Alignment = ContentAlignment.MiddleLeft;
            caption.Position.Auto = true;
        }

        private void AdjustYAxis(double loadKgf)
        {
            if (!m_AutoY) return;
            var area = chartLoad.ChartAreas[0];
            double minimum = Math.Min(m_YMin, Math.Min(0, loadKgf * 1.1D));
            area.AxisY.Minimum = Math.Min(area.AxisY.Minimum, minimum);
            double maximum = Math.Max(m_YMax, m_PeakLoadKgf);
            area.AxisY.Maximum = maximum + (maximum - area.AxisY.Minimum) * 0.1D;
            area.AxisY.Interval = (area.AxisY.Maximum - area.AxisY.Minimum) / 10D;
            ConfigureAxisLabels(area);
        }

        private void AdjustXAxis(Series series, int readNumber)
        {
            if (!m_AutoX) return;
            var area = chartLoad.ChartAreas[0];
            area.AxisX.Minimum = m_XMin;
            area.AxisX.Maximum = m_XMin + (Math.Max(m_XMax, readNumber) - m_XMin) * 1.1D;
            area.AxisX.Interval = (area.AxisX.Maximum - area.AxisX.Minimum) / 10D;
            ConfigureAxisLabels(area);
        }
        private static double CalculateXAxisInterval(int readCount)
        {
            if (readCount <= 10)
            {
                return 1D;
            }

            double roughInterval = readCount / 8D;
            double magnitude = Math.Pow(
                10D,
                Math.Floor(Math.Log10(roughInterval)));
            double normalized = roughInterval / magnitude;

            if (normalized <= 1D)
            {
                return magnitude;
            }

            if (normalized <= 2D)
            {
                return 2D * magnitude;
            }

            if (normalized <= 5D)
            {
                return 5D * magnitude;
            }

            return 10D * magnitude;
        }

        private StationInspectionResult CreateInvalidResult(string errorMessage)
        {
            return new StationInspectionResult
            {
                IsValid = false,
                InspectionCount = m_InspectionCount,
                PassCount = m_PassCount,
                FailCount = m_FailCount,
                YieldPercent = m_InspectionCount == 0
                    ? 0D
                    : m_PassCount * 100D / m_InspectionCount,
                ErrorMessage = errorMessage
            };
        }

        private void UpdateInspectionSummary(double yieldPercent)
        {
            if (m_ShowingReviewSummary) return;
            UpdateCurrentLoadText();
            lblCompletedCount.Text = "검사 번호";
            lblCountValue.Text = m_InspectionCount.ToString(CultureInfo.InvariantCulture);
            lblPassValue.Text = m_PassCount.ToString(CultureInfo.InvariantCulture);
            lblFailValue.Text = m_FailCount.ToString(CultureInfo.InvariantCulture);
            lblYieldValue.Text = yieldPercent.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void ApplyVerdict(string verdict, Color foreground, Color background)
        {
            m_LiveVerdict = verdict;
            m_LiveVerdictForeground = foreground;
            m_LiveVerdictBackground = background;
            if (!m_ShowingReviewSummary) DrawVerdict(verdict, foreground, background);
        }

        private void DrawVerdict(string verdict, Color foreground, Color background)
        {
            lblVerdictCaption.ForeColor = foreground;
            lblVerdictValue.ForeColor = foreground;
            lblVerdictCaption.BackColor = background;
            lblVerdictValue.BackColor = background;
            lblVerdictValue.Text = verdict;
        }

        private static bool TryParseSpecRange(
            string specText,
            out double lowerSpec,
            out double upperSpec)
        {
            lowerSpec = 0D;
            upperSpec = 0D;

            if (!TryParseSpecNumber((specText ?? "").Trim(), out lowerSpec) ||
                double.IsNaN(lowerSpec) || double.IsInfinity(lowerSpec) || lowerSpec < 0)
                return false;
            // Keep legacy CSV columns readable; both store the single threshold for new results.
            upperSpec = lowerSpec;
            return true;
        }

        private static bool TryParseSpecNumber(string value, out double result)
        {
            return double.TryParse(
                value.Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result);
        }

        private void RaiseCreateFileRequested()
        {
            EventHandler handler = CreateFileRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void RaiseLotEndRequested()
        {
            EventHandler handler = LotEndRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}







