$p='SOFCMeas/StationView.cs';$s=Get-Content $p -Raw
$start=$s.IndexOf('            };            btnStart.Click += delegate');$end=$s.IndexOf('            MatchInputLabel(lblModel', $start)
$s=$s.Substring(0,$start)+@"
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
                    await RunSimulationAsync();
                    return;
                }
                SetCollectionState(!CollectionEnabled);
            };
            Disposed += delegate { if (m_SimulationCancellation != null) m_SimulationCancellation.Cancel(); };
"@+$s.Substring($end)
$pos=$s.IndexOf('        internal event EventHandler CreateFileRequested;')
$s=$s.Insert($pos,@"
        internal bool IsSimulationRun { get; private set; }
        private System.Threading.CancellationTokenSource m_SimulationCancellation;

        private void SetCollectionState(bool enabled)
        {
            CollectionEnabled = enabled;
            if (enabled) ClearReviewGraph();
            btnStart.Text = enabled ? "START" : "STOP";
            btnStart.BackColor = enabled ? Color.FromArgb(0, 151, 86) : Color.FromArgb(170, 75, 80);
            btnStart.ForeColor = enabled ? Color.White : Color.Yellow;
            if (CollectionEnabledChanged != null) CollectionEnabledChanged(this, EventArgs.Empty);
        }

        private async System.Threading.Tasks.Task RunSimulationAsync(int intervalMilliseconds = 1000)
        {
            if (IsSimulationRun || historyStorage == null) return;
            string error;
            if (!TryValidateJobInformation(out error)) { MessageBox.Show(this, error, "시뮬레이션"); return; }
            IsSimulationRun = true;
            var cancellation = new System.Threading.CancellationTokenSource();
            m_SimulationCancellation = cancellation;
            bool previousLotEnd = btnLotEnd.Enabled;
            btnLotEnd.Enabled = false;
            try
            {
                var settings = SettingsCatalog.Load();
                string stationKey = historyStation == "#2" ? "Station2." : "Station1.";
                double scale = SettingsCatalog.Number(settings, stationKey + "Plc.LoadScale");
                txtSpec.Text = SettingsCatalog.Number(settings, stationKey + "Inspection.ThresholdKgf").ToString("0.00", CultureInfo.InvariantCulture);
                double threshold = double.Parse(txtSpec.Text, CultureInfo.InvariantCulture);
                string model = FileName, lotNumber = LotNumber, operatorName = OperatorName, spec = SpecText;
                SetCollectionState(true); ResetLoadGraph();
                StateText = "SIMULATION"; StateColor = Color.DarkOrange;
                DateTime started = DateTime.Now;
                var samples = new System.Collections.Generic.List<InspectionSample>();
                var random = new Random(); double peak = Math.Max(0.01, threshold) * (1.1 + random.NextDouble() * 0.3);
                AppendLog(started, "SIMULATION 시작: 1초 간격, 60회 가상 하중 수집");
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
                    AddLoadSample(kgf, raw);
                }
                var result = CompleteInspection();
                SetCollectionState(false); btnStart.Enabled = false;
                StateText = "SAVING";
                var record = new InspectionLogRecord { Time = DateTime.Now, StartedAt = started,
                    Station = historyStation, LotNumber = lotNumber, FileName = model, OperatorName = operatorName,
                    Result = result.IsValid ? (result.IsPass ? "GOOD" : "NG") : "INVALID",
                    PeakLoadKgf = result.PeakLoadKgf, LowerSpecKgf = result.LowerSpecKgf, UpperSpecKgf = result.UpperSpecKgf,
                    SampleCount = samples.Count, Message = "SIMULATION: 60 generated samples; not PLC measurements." };
                var lot = new LotStorageData { CreatedAt = started, CompletedAt = record.Time, Station = historyStation,
                    LotNumber = lotNumber, FileName = model, OperatorName = operatorName, SpecText = spec,
                    Inspections = new[] { new BufferedInspectionData { Record = record, Samples = samples } } };
                string path = await System.Threading.Tasks.Task.Run(() => historyStorage.SaveLot(lot));
                if (IsDisposed) return;
                AppendLog(DateTime.Now, "SIMULATION 60회 완료 / 저장 완료: " + path);
                historyFrom.Value = record.Time.Date; historyTo.Value = record.Time.Date;
                var files = await System.Threading.Tasks.Task.Run(() => historyStorage.GetLotFiles(record.Time.Date, record.Time.Date, historyStation));
                if (IsDisposed) return;
                historyResults.Rows.Clear(); int savedRow = -1;
                foreach (var file in files)
                {
                    int row = historyResults.Rows.Add(file.Date.ToString("yyyy-MM-dd"), file.FileName);
                    historyResults.Rows[row].Tag = file;
                    if (file.FilePath == path) savedRow = row;
                }
                if (savedRow >= 0)
                {
                    historyResults.CurrentCell = historyResults.Rows[savedRow].Cells[0];
                    await LoadReviewFileAsync(savedRow);
                    if (!IsDisposed) ShowSelectedReview();
                }
                StateText = "STOP";
            }
            catch (OperationCanceledException) { if (!IsDisposed) { FreezeLoadGraph(); AppendLog(DateTime.Now, "SIMULATION 중지: 미완료 데이터는 저장하지 않았습니다."); } }
            catch (Exception ex) { if (!IsDisposed) { AppendLog(DateTime.Now, "SIMULATION 오류: " + ex.Message); MessageBox.Show(this, ex.Message, "시뮬레이션 오류", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
            finally
            {
                if (!IsDisposed) { SetCollectionState(false); btnStart.Enabled = true; btnLotEnd.Enabled = previousLotEnd; StateText = "STOP"; }
                IsSimulationRun = false; m_SimulationCancellation = null; cancellation.Dispose();
            }
        }

"@)
Set-Content $p $s -Encoding UTF8
