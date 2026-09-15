using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Krypton.Toolkit;

namespace SOFCMeas
{
    public partial class SOFCMeas : Form
    {
        private sealed class StationCaptureState
        {
            private readonly List<InspectionSample> m_Samples =
                new List<InspectionSample>();

            internal long CycleNumber { get; private set; }
            internal DateTime StartedAt { get; private set; }
            internal bool IsActive { get; private set; }

            internal IReadOnlyList<InspectionSample> Samples
            {
                get { return m_Samples; }
            }

            internal void Reset(long cycleNumber, DateTime startedAt)
            {
                CycleNumber = cycleNumber;
                StartedAt = startedAt;
                IsActive = true;
                m_Samples.Clear();
            }

            internal void Add(PlcLoadSampleEventArgs sample)
            {
                if (StartedAt == default(DateTime))
                {
                    StartedAt = sample.ReadTime;
                }

                m_Samples.Add(new InspectionSample
                {
                    Number = m_Samples.Count + 1,
                    ReadTime = sample.ReadTime,
                    RawValue = sample.RawValue,
                    LoadKgf = sample.LoadKgf
                });
            }

            internal BufferedInspectionData Complete(InspectionLogRecord record)
            {
                IsActive = false;
                return new BufferedInspectionData
                {
                    Record = record,
                    Samples = m_Samples.ToArray()
                };
            }

            internal int Abort()
            {
                int discardedSampleCount = m_Samples.Count;
                IsActive = false;
                StartedAt = default(DateTime);
                m_Samples.Clear();
                return discardedSampleCount;
            }
        }

        private sealed class StationLotState
        {
            internal int MaximumInspectionCount = 600;

            private readonly List<BufferedInspectionData> m_Inspections =
                new List<BufferedInspectionData>();

            internal bool IsPrepared { get; private set; }
            internal bool IsSaving { get; set; }
            internal DateTime CreatedAt { get; private set; }
            internal string FileName { get; private set; }
            internal string LotNumber { get; private set; }
            internal string OperatorName { get; private set; }
            internal string SpecText { get; private set; }

            internal int InspectionCount
            {
                get { return m_Inspections.Count; }
            }

            internal IReadOnlyList<BufferedInspectionData> Inspections
            {
                get { return m_Inspections; }
            }

            internal void Prepare(
                DateTime createdAt,
                string fileName,
                string lotNumber,
                string operatorName,
                string specText)
            {
                if (m_Inspections.Count > 0)
                {
                    throw new InvalidOperationException(
                        "저장하지 않은 LOT 데이터가 남아 있습니다.");
                }

                IsPrepared = true;
                CreatedAt = createdAt;
                FileName = fileName;
                LotNumber = lotNumber;
                OperatorName = operatorName;
                SpecText = specText;
            }

            internal bool TryAdd(
                BufferedInspectionData inspection,
                out BufferedInspectionData removedOldestInspection)
            {
                if (inspection == null)
                {
                    throw new ArgumentNullException(nameof(inspection));
                }

                removedOldestInspection = null;
                int capacity = Math.Max(1, MaximumInspectionCount);
                if (m_Inspections.Count >= capacity)
                {
                    removedOldestInspection = m_Inspections[0];
                    m_Inspections.RemoveAt(0);
                }

                m_Inspections.Add(inspection);
                return true;
            }

            internal LotStorageData CreateStorageData(
                string station,
                DateTime completedAt)
            {
                return new LotStorageData
                {
                    CreatedAt = CreatedAt,
                    CompletedAt = completedAt,
                    Station = station,
                    LotNumber = LotNumber,
                    FileName = FileName,
                    OperatorName = OperatorName,
                    SpecText = SpecText,
                    Inspections = m_Inspections.Select(inspection =>
                    {
                        var record = inspection.Record.Copy();
                        record.FileName = FileName;
                        record.LotNumber = LotNumber;
                        record.OperatorName = OperatorName;
                        return new BufferedInspectionData
                        {
                            Record = record,
                            Samples = inspection.Samples.ToArray()
                        };
                    }).ToArray()
                };
            }

            internal void UpdateJobInformation(
                string fileName,
                string lotNumber,
                string operatorName,
                string specText)
            {
                FileName = fileName;
                LotNumber = lotNumber;
                OperatorName = operatorName;
                SpecText = specText;
            }

            internal void RemoveSavedInspections(int count, bool keepPrepared)
            {
                int removeCount = Math.Min(Math.Max(0, count), m_Inspections.Count);
                if (removeCount > 0)
                {
                    m_Inspections.RemoveRange(0, removeCount);
                }

                if (m_Inspections.Count == 0 && !keepPrepared)
                {
                    Clear();
                }
            }

            private void Clear()
            {
                m_Inspections.Clear();
                IsPrepared = false;
                IsSaving = false;
                CreatedAt = default(DateTime);
                FileName = null;
                LotNumber = null;
                OperatorName = null;
                SpecText = null;
            }
        }

        private static readonly Color MenuDefaultColor = Color.FromArgb(36, 45, 60);
        private static readonly Color MenuSelectedColor = Color.FromArgb(0, 180, 90);
        private static readonly Color MenuBorderColor = Color.FromArgb(84, 101, 122);

        private PlcGraphReader m_Station1Reader;
        private PlcGraphReader m_Station2Reader;
        private readonly StationCaptureState m_Station1Capture =
            new StationCaptureState();
        private readonly StationCaptureState m_Station2Capture =
            new StationCaptureState();
        private readonly StationLotState m_Station1Lot = new StationLotState();
        private readonly StationLotState m_Station2Lot = new StationLotState();
        private readonly List<InspectionLogRecord> m_CurrentLogRecords =
            new List<InspectionLogRecord>();

        private StoragePolicySettings m_StoragePolicySettings;
        private ApplicationLogService m_ApplicationLogService;
        private InspectionStorageService m_InspectionStorageService;
        private LogFolderCleanupService m_LogCleanupService;
        private Timer m_LogCleanupTimer;
        private Timer m_IndexRecoveryTimer;
        private bool m_IndexRecoveryRunning;
        private Label m_LogStorageStatus;
        private long m_ObservedLogFailureCount;
        private bool m_LogFailureAcknowledged;
        private bool m_LogCleanupRunning;
        private DateTime m_LastLogCleanupDate = DateTime.MinValue;
        private readonly DateTime m_ApplicationStartedAt = DateTime.Now;

        public SOFCMeas()
            : this(null, null)
        {
        }

        internal SOFCMeas(
            StoragePolicySettings storagePolicySettings,
            ApplicationLogService applicationLogService,
            Action<int, string> progress = null)
        {
            // Keep the initial 96-DPI layout intact until pages are reparented
            // and runtime controls have been added, so they scale together.
            SuspendLayout();
            InitializeComponent();
            InitializeNavigationPages();
            if (progress != null) progress(55, "화면과 권한 설정을 준비했습니다.");

            DoubleBuffered = true;
            cboLogStation.SelectedIndex = 0;
            dtpLogFrom.Value = DateTime.Today.AddDays(-6);
            dtpLogTo.Value = DateTime.Today;

            InitializeStorageServices(storagePolicySettings, applicationLogService);
            stationView1.BindHistory(m_InspectionStorageService, "#1");
            stationView2.BindHistory(m_InspectionStorageService, "#2");
            if (progress != null) progress(70, "저장 서비스를 준비했습니다.");
            InitializeLogGridData();
            WireUiEvents();
            InitializePlcGraphReaders();
            if (progress != null) progress(90, "PLC 통신 설정을 준비했습니다.");
            ShowMainView();
            ResumeLayout(true);
        }

        private void WireUiEvents()
        {
            btnMainView.Click += delegate { ShowMainView(); };
            btnLogView.Click += delegate { ShowLogView(); };
            btnExit.Click += delegate { Close(); };
            btnSearchLog.Click += btnSearchLog_Click;
            btnExportLog.Click += btnExportLog_Click;
            btnApplyLogPolicy.Click += btnApplyLogPolicy_Click;
            KeyDown += SOFCMeas_KeyDown;
            Shown += SOFCMeas_Shown;
            FormClosing += SOFCMeas_FormClosing;
            FormClosed += SOFCMeas_FormClosed;
        }

        private void InitializeStorageServices(
            StoragePolicySettings storagePolicySettings,
            ApplicationLogService applicationLogService)
        {
            ApplicationPaths.EnsureStorageDirectories();
            m_StoragePolicySettings = storagePolicySettings ?? StoragePolicySettings.Load();
            m_ApplicationLogService = applicationLogService ?? new ApplicationLogService(
                m_StoragePolicySettings.MaximumLogFileSizeBytes);
            m_InspectionStorageService = new InspectionStorageService(
                m_ApplicationLogService);
            m_LogCleanupService = new LogFolderCleanupService(
                ApplicationPaths.LogRootDirectory,
                ApplicationPaths.DataRootDirectory);

            numLogRetentionDays.Value = m_StoragePolicySettings.LogRetentionDays;
            chkLogAutoDelete.Checked = m_StoragePolicySettings.LogAutoDelete;
            lblLogStoragePath.Text = string.Format(
                "저장 경로: {0}\\yyyy\\MM\\dd",
                ApplicationPaths.LogRootDirectory);
            UpdateLogPolicyStatus("정책 로드 완료");

            m_LogCleanupTimer = new Timer
            {
                Interval = 60 * 60 * 1000
            };
            m_LogCleanupTimer.Tick += LogCleanupTimer_Tick;
            m_LogCleanupTimer.Start();

            InitializeLogStorageStatus();
            m_IndexRecoveryTimer = new Timer { Interval = 30000 };
            m_IndexRecoveryTimer.Tick += async delegate { await RecoverInspectionIndexesAsync(); };
            m_IndexRecoveryTimer.Start();
            Disposed += delegate
            {
                m_ApplicationLogService.StorageStateChanged -= LogStorageStateChanged;
                if (m_IndexRecoveryTimer != null) m_IndexRecoveryTimer.Dispose();
            };

            m_ApplicationLogService.Info(
                "APPLICATION",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=STORAGE_INITIALIZED applicationRoot=\"{0}\" dataRoot=\"{1}\" " +
                    "logRoot=\"{2}\" retentionDays={3} autoDelete={4} maxFileMb={5}",
                    EscapeLogValue(ApplicationPaths.ApplicationRootDirectory),
                    EscapeLogValue(ApplicationPaths.DataRootDirectory),
                    EscapeLogValue(ApplicationPaths.LogRootDirectory),
                    m_StoragePolicySettings.LogRetentionDays,
                    m_StoragePolicySettings.LogAutoDelete,
                    m_StoragePolicySettings.MaximumLogFileSizeMb));
        }

        private void InitializeLogStorageStatus()
        {
            m_LogStorageStatus = new Label
            {
                Name = "lblLogStorageStatus", Bounds = new Rectangle(1010, 16, 230, 48),
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold), ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand
            };
            pnlTitle.Controls.Add(m_LogStorageStatus);
            m_LogStorageStatus.Click += delegate
            {
                Exception failure = m_ApplicationLogService.LastError ?? m_ApplicationLogService.LastFailure;
                string details = "로그 저장 경로: " + m_ApplicationLogService.RootDirectory;
                if (failure != null)
                    details += Environment.NewLine + "마지막 실패: " +
                        m_ApplicationLogService.LastFailureTime.ToString("yyyy-MM-dd HH:mm:ss") +
                        Environment.NewLine + failure.Message + Environment.NewLine +
                        "저장 장애 동안 누락된 로그는 복구되지 않습니다.";
                MessageBox.Show(this, details, "로그 저장 상태", MessageBoxButtons.OK,
                    failure == null ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                m_LogFailureAcknowledged = true;
                RefreshLogStorageStatus();
            };
            m_ApplicationLogService.StorageStateChanged += LogStorageStateChanged;
            RefreshLogStorageStatus();
        }

        private void LogStorageStateChanged(object sender, EventArgs args)
        {
            RunOnUiThread(RefreshLogStorageStatus);
        }

        private void RefreshLogStorageStatus()
        {
            if (IsDisposed || Disposing || m_LogStorageStatus == null) return;
            long failures = m_ApplicationLogService.FailureCount;
            if (failures != m_ObservedLogFailureCount)
            {
                m_ObservedLogFailureCount = failures;
                m_LogFailureAcknowledged = false;
                string detail = "로그 파일 저장 장애: " + m_ApplicationLogService.LastFailure?.Message;
                stationView1.AppendLog(DateTime.Now, detail);
                stationView2.AppendLog(DateTime.Now, detail);
            }
            bool failed = m_ApplicationLogService.LastError != null;
            bool recovered = !failed && failures > 0 && !m_LogFailureAcknowledged;
            m_LogStorageStatus.Text = failed ? "로그 저장 오류\r\n클릭하여 상세 확인"
                : recovered ? "로그 저장 복구됨\r\n누락 이력 확인" : "로그 저장 정상";
            m_LogStorageStatus.BackColor = failed ? Color.Firebrick
                : recovered ? Color.DarkOrange : Color.FromArgb(0, 118, 128);
            m_LogStorageStatus.AccessibleDescription = m_LogStorageStatus.Text;
        }

        private async Task RecoverInspectionIndexesAsync()
        {
            if (m_IndexRecoveryRunning || IsDisposed || Disposing) return;
            m_IndexRecoveryRunning = true;
            try
            {
                int recovered = await Task.Run(() => m_InspectionStorageService.RetryPendingInspectionIndexes());
                if (!IsDisposed && !Disposing && recovered > 0)
                {
                    stationView1.AppendLog(DateTime.Now, "검사 로그 인덱스 복구 완료: " + recovered + "개 LOT");
                    stationView2.AppendLog(DateTime.Now, "검사 로그 인덱스 복구 완료: " + recovered + "개 LOT");
                    if (pnlLog.Visible) RefreshInspectionLogGrid(false);
                }
            }
            catch (Exception exception)
            {
                m_ApplicationLogService.Warning("INSPECTION-STORAGE", "event=INSPECTION_INDEX_RECOVERY_FAILED", exception);
            }
            finally { m_IndexRecoveryRunning = false; }
        }

        private void InitializePlcGraphReaders()
        {
            PlcGraphSettings station1Settings = PlcGraphSettings.Load(1, "172.20.9.100");
            PlcGraphSettings station2Settings = PlcGraphSettings.Load(2, "172.20.9.101");
            stationView1.StationTitle = "설비 #1  |  PLC " + station1Settings.IpAddress;
            stationView2.StationTitle = "설비 #2  |  PLC " + station2Settings.IpAddress;

            LogStationSettings(station1Settings);
            LogStationSettings(station2Settings);

            stationView1.MaximumGraphPoints = station1Settings.MaximumGraphPoints;
            stationView2.MaximumGraphPoints = station2Settings.MaximumGraphPoints;
            stationView1.ConfigureGraph(1);
            stationView2.ConfigureGraph(2);
            m_Station1Lot.MaximumInspectionCount = stationView1.LotSaveCount;
            m_Station2Lot.MaximumInspectionCount = stationView2.LotSaveCount;
            stationView1.SetLoadDeviceName(station1Settings.LoadRawDevice);
            stationView2.SetLoadDeviceName(station2Settings.LoadRawDevice);
            stationView1.ResetLoadGraph();
            stationView2.ResetLoadGraph();

            m_Station1Reader = CreatePlcGraphReader(station1Settings);
            m_Station2Reader = CreatePlcGraphReader(station2Settings);

            AttachPlcGraphReader(
                m_Station1Reader,
                stationView1,
                lblHb1Status,
                "#1",
                m_Station1Capture,
                m_Station1Lot);
            AttachPlcGraphReader(
                m_Station2Reader,
                stationView2,
                lblHb2Status,
                "#2",
                m_Station2Capture,
                m_Station2Lot);
        }

        private PlcGraphReader CreatePlcGraphReader(PlcGraphSettings settings)
        {
            try
            {
                PlcGraphReader reader = new PlcGraphReader(settings);
                m_ApplicationLogService.Info(
                    GetStationSource(settings.StationNumber),
                    "event=PLC_READER_INITIALIZED");
                return reader;
            }
            catch (Exception exception)
            {
                m_ApplicationLogService.Error(
                    GetStationSource(settings.StationNumber),
                    "event=PLC_READER_INITIALIZATION_FAILED",
                    exception);
                throw;
            }
        }

        private void AttachPlcGraphReader(
            PlcGraphReader reader,
            StationView stationView,
            Label connectionBadge,
            string stationLabel,
            StationCaptureState captureState,
            StationLotState lotState)
        {
            string stationSource = GetStationSource(reader.Settings.StationNumber);
            reader.SetCollectionEnabled(stationView.CollectionEnabled);
            stationView.CollectionEnabledChanged += delegate
            {
                if (stationView.IsSimulationRun)
                {
                    reader.SetCollectionEnabled(false);
                    connectionBadge.Text = stationLabel + " SIMULATION";
                    connectionBadge.BackColor = Color.DarkOrange;
                    return;
                }
                reader.SetCollectionEnabled(stationView.CollectionEnabled);
                if (stationView.CollectionEnabled) reader.Start(true);
                if (!stationView.CollectionEnabled)
                {
                    int discarded = captureState.Abort();
                    stationView.SetPlcCollectionState(false);
                    stationView.AppendLog(DateTime.Now, "STOP: 수집 중지, 미완료 샘플 " + discarded + "개 제외");
                }
                else stationView.AppendLog(DateTime.Now, "START: PLC 시작 신호 대기");
            };

            stationView.CreateFileRequested += delegate
            {
                HandleCreateFileRequested(
                    stationSource,
                    stationLabel,
                    stationView,
                    lotState);
            };
            stationView.SimulationCompleted += inspection =>
            {
                EnsureLotPreparedForPlcStart(stationSource, stationLabel, stationView, lotState);
                BufferedInspectionData removedOldestInspection;
                lotState.TryAdd(inspection, out removedOldestInspection);
                if (removedOldestInspection != null)
                {
                    stationView.AppendLog(
                        DateTime.Now,
                        "LOT 검사 LIST가 가득 차 가장 오래된 결과를 제거했습니다.");
                    m_ApplicationLogService.Info(
                        stationSource,
                        "event=LOT_OLDEST_INSPECTION_REMOVED reason=rolling_capacity " +
                        "capacity=" + lotState.MaximumInspectionCount.ToString(
                            CultureInfo.InvariantCulture));
                }
                stationView.SetBufferedInspectionSummary(lotState.Inspections);
                stationView.SetLotPrepared(true, lotState.InspectionCount);
            };
            stationView.LotEndRequested += async delegate
            {
                await HandleLotEndRequestedAsync(
                    stationSource,
                    stationLabel,
                    stationView,
                    reader,
                    captureState,
                    lotState);
            };

            reader.ConnectionStateChanged += delegate(
                object sender,
                PlcGraphConnectionStateChangedEventArgs e)
            {
                string connectionMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "event=CONNECTION_STATE_CHANGED state={0} endpoint={1}:{2}",
                    e.State,
                    reader.Settings.IpAddress,
                    reader.Settings.Port);
                if (e.Error == null)
                {
                    m_ApplicationLogService.Info(stationSource, connectionMessage);
                }
                else
                {
                    m_ApplicationLogService.Error(
                        stationSource,
                        connectionMessage,
                        e.Error);
                }

                RunOnUiThread(delegate
                {
                    if (stationView.IsSimulationRun) return;
                    ApplyConnectionState(
                        stationView,
                        connectionBadge,
                        stationLabel,
                        e);

                    if (e.State == PlcGraphConnectionState.Error &&
                        captureState.IsActive)
                    {
                        int discardedSampleCount = captureState.Abort();
                        stationView.FreezeLoadGraph();
                        stationView.SetPlcCollectionState(false);
                        stationView.SetLotPrepared(
                            lotState.IsPrepared,
                            lotState.InspectionCount);
                        stationView.AppendLog(
                            DateTime.Now,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "PLC 통신 중단으로 미완료 검사 샘플 {0}개를 폐기했습니다.",
                                discardedSampleCount));
                        m_ApplicationLogService.Warning(
                            stationSource,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "event=INCOMPLETE_CYCLE_MEMORY_DISCARDED cycle={0} " +
                                "discardedSampleCount={1} reason=plc_communication_error " +
                                "bufferedInspectionCount={2}",
                                captureState.CycleNumber,
                                discardedSampleCount,
                                lotState.InspectionCount),
                            e.Error);
                    }
                });
            };

            reader.GraphResetRequested += delegate(
                object sender,
                PlcGraphCycleEventArgs e)
            {
                int collectionVersion = reader.CollectionVersion;
                RunOnUiThread(delegate
                {
                    if (stationView.IsSimulationRun || !stationView.CollectionEnabled ||
                        lotState.IsSaving || collectionVersion != reader.CollectionVersion) return;
                    EnsureLotPreparedForPlcStart(
                        stationSource,
                        stationLabel,
                        stationView,
                        lotState);
                    captureState.Reset(e.CycleNumber, e.StartedAt);
                    stationView.ResetLoadGraph();
                    stationView.SetPlcCollectionState(true);
                    stationView.StateText = "RUN";
                    stationView.StateColor = Color.FromArgb(0, 151, 86);
                    m_ApplicationLogService.Info(
                        stationSource,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=CYCLE_UI_STARTED cycle={0} startedAt={1:O} " +
                            "lot=\"{2}\" fileName=\"{3}\" operator=\"{4}\" spec=\"{5}\"",
                            e.CycleNumber,
                            e.StartedAt,
                            EscapeLogValue(stationView.LotNumber),
                            EscapeLogValue(stationView.FileName),
                            EscapeLogValue(stationView.OperatorName),
                            EscapeLogValue(stationView.SpecText)));
                });
            };

            reader.LoadSampleReceived += delegate(
                object sender,
                PlcLoadSampleEventArgs e)
            {
                int collectionVersion = reader.CollectionVersion;
                RunOnUiThread(delegate
                {
                    if (stationView.IsSimulationRun || !stationView.CollectionEnabled ||
                        lotState.IsSaving || collectionVersion != reader.CollectionVersion) return;
                    if (captureState.CycleNumber != e.CycleNumber)
                    {
                        m_ApplicationLogService.Warning(
                            stationSource,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "event=SAMPLE_CYCLE_MISMATCH readerCycle={0} uiCycle={1} " +
                                "sample={2}",
                                e.CycleNumber,
                                captureState.CycleNumber,
                                e.SampleNumber));
                    }

                    captureState.Add(e);
                    if (e.LoadWords == null)
                        stationView.AddLoadSample(e.LoadKgf, e.RawValue);
                    else
                        stationView.AddPlcLoadSample(e.LoadKgf, e.RawValue, e.LoadWords);
                });
            };

            reader.GraphFreezeRequested += delegate(
                object sender,
                PlcGraphCycleEventArgs e)
            {
                int collectionVersion = reader.CollectionVersion;
                RunOnUiThread(delegate
                {
                    if (stationView.IsSimulationRun || !stationView.CollectionEnabled ||
                        lotState.IsSaving || collectionVersion != reader.CollectionVersion) return;
                    try
                    {
                        stationView.SetPlcCollectionState(false);
                        CompleteAndBufferInspection(
                            stationSource,
                            stationLabel,
                            stationView,
                            captureState,
                            lotState,
                            e);
                    }
                    finally
                    {
                        reader.CompleteEvaluation();
                    }
                });
            };

            reader.LogMessage += delegate(
                object sender,
                PlcGraphLogEventArgs e)
            {
                WritePlcLog(stationSource, e);
                if (e.DisplayInStationLog)
                {
                    RunOnUiThread(delegate
                    {
                        stationView.AppendLog(
                            e.Time,
                            e.EventName + " - " + e.Message);
                    });
                }
            };
        }

        private void LogStationSettings(PlcGraphSettings settings)
        {
            m_ApplicationLogService.Info(
                GetStationSource(settings.StationNumber),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=PLC_SETTINGS_LOADED station={0} endpoint={1}:{2} autoConnect={3} " +
                    "pollMs={4} reconnectMs={5} timeoutMs={6} loadScale={7:0.######} " +
                    "maxGraphPoints={8} requestDevice={9} startValue={10} endValue={11} " +
                    "loadRaw={12} loadWords=5 loadFormat=ASCII_STX_HEADER_SIGN_MAGNITUDE_ETX",
                    settings.StationNumber,
                    settings.IpAddress,
                    settings.Port,
                    settings.AutoConnect,
                    settings.PollIntervalMilliseconds,
                    settings.ReconnectDelayMilliseconds,
                    settings.TimeoutMilliseconds,
                    settings.LoadScale,
                    settings.MaximumGraphPoints,
                    settings.RequestDevice,
                    settings.StartRequestValue,
                    settings.EndRequestValue,
                    settings.LoadRawDevice));
        }

        private void WritePlcLog(string stationSource, PlcGraphLogEventArgs e)
        {
            string message = "event=" + e.EventName + " " + e.Message;
            switch (e.Level)
            {
                case PlcGraphLogLevel.Warning:
                    m_ApplicationLogService.Warning(stationSource, message, e.Error);
                    break;

                case PlcGraphLogLevel.Error:
                    m_ApplicationLogService.Error(stationSource, message, e.Error);
                    break;

                default:
                    if (e.Error == null)
                    {
                        m_ApplicationLogService.Info(stationSource, message);
                    }
                    else
                    {
                        m_ApplicationLogService.Error(stationSource, message, e.Error);
                    }
                    break;
            }
        }

        private static string GetStationSource(int stationNumber)
        {
            return "STATION-" + stationNumber.ToString(CultureInfo.InvariantCulture);
        }

        private static string EscapeLogValue(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace('\r', ' ')
                    .Replace('\n', ' ');
        }

        private void HandleCreateFileRequested(
            string stationSource,
            string stationLabel,
            StationView stationView,
            StationLotState lotState)
        {
            if (lotState.IsSaving)
            {
                return;
            }

            if (!stationView.JobInformationEditing)
            {
                stationView.SetJobInformationEditing(true);
                stationView.AppendLog(
                    DateTime.Now,
                    "파일명/작업자/LOT NO./SPEC을 입력한 후 입력 완료를 누르세요.");
                m_ApplicationLogService.Info(
                    stationSource,
                    "event=LOT_INFORMATION_EDIT_STARTED");
                return;
            }

            string validationError;
            if (!stationView.TryValidateJobInformation(out validationError))
            {
                m_ApplicationLogService.Warning(
                    stationSource,
                    "event=LOT_INFORMATION_REJECTED reason=\"" +
                    EscapeLogValue(validationError) + "\"");
                MessageBox.Show(
                    this,
                    validationError,
                    stationLabel + " 작업정보 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            lotState.Prepare(
                DateTime.Now,
                stationView.FileName.Trim(),
                stationView.LotNumber.Trim(),
                stationView.OperatorName.Trim(),
                stationView.SpecText.Trim());
            stationView.ResetInspectionSummary();
            stationView.ResetLoadGraph();
            stationView.SetLotPrepared(true, 0);
            stationView.AppendLog(DateTime.Now, "LOT 작업정보 입력 완료 - PLC START 대기");
            m_ApplicationLogService.Info(
                stationSource,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=LOT_PREPARED lot=\"{0}\" fileName=\"{1}\" operator=\"{2}\" " +
                    "spec=\"{3}\"",
                    EscapeLogValue(lotState.LotNumber),
                    EscapeLogValue(lotState.FileName),
                    EscapeLogValue(lotState.OperatorName),
                    EscapeLogValue(lotState.SpecText)));
        }

        private void EnsureLotPreparedForPlcStart(
            string stationSource,
            string stationLabel,
            StationView stationView,
            StationLotState lotState)
        {
            if (lotState.IsPrepared)
            {
                return;
            }

            DateTime now = DateTime.Now;
            string fileName = string.IsNullOrWhiteSpace(stationView.FileName)
                ? stationSource + "_" + now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
                : stationView.FileName.Trim();
            string lotNumber = string.IsNullOrWhiteSpace(stationView.LotNumber)
                ? "UNASSIGNED_" + now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
                : stationView.LotNumber.Trim();
            string operatorName = string.IsNullOrWhiteSpace(stationView.OperatorName)
                ? "UNKNOWN"
                : stationView.OperatorName.Trim();
            string specText = stationView.SpecText == null
                ? string.Empty
                : stationView.SpecText.Trim();

            stationView.FileName = fileName;
            stationView.LotNumber = lotNumber;
            stationView.OperatorName = operatorName;
            lotState.Prepare(now, fileName, lotNumber, operatorName, specText);
            stationView.SetLotPrepared(true, 0);
            stationView.AppendLog(
                now,
                "파일 생성 준비 없이 START_REQ가 입력되어 현재 작업정보로 LOT을 자동 준비했습니다.");
            m_ApplicationLogService.Warning(
                stationSource,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=LOT_AUTO_PREPARED reason=start_request_before_file_create " +
                    "station={0} lot=\"{1}\" fileName=\"{2}\" operator=\"{3}\" spec=\"{4}\"",
                    stationLabel,
                    EscapeLogValue(lotNumber),
                    EscapeLogValue(fileName),
                    EscapeLogValue(operatorName),
                    EscapeLogValue(specText)));
        }

        private async Task HandleLotEndRequestedAsync(
            string stationSource,
            string stationLabel,
            StationView stationView,
            PlcGraphReader reader,
            StationCaptureState captureState,
            StationLotState lotState,
            bool showCompletion = true)
        {
            if (lotState.IsSaving)
            {
                return;
            }

            await stationView.StopSimulationAsync();
            if (lotState.IsSaving) return;

            if (captureState.IsActive)
            {
                m_ApplicationLogService.Warning(
                    stationSource,
                    "event=LOT_END_REJECTED reason=inspection_active");
                MessageBox.Show(
                    this,
                    "검사 수집 중에는 LOT을 종료할 수 없습니다. END_REQ 후 다시 실행하세요.",
                    stationLabel + " LOT 종료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!lotState.IsPrepared || lotState.InspectionCount == 0)
            {
                m_ApplicationLogService.Warning(
                    stationSource,
                    "event=LOT_END_REJECTED reason=no_buffered_inspections");
                MessageBox.Show(
                    this,
                    "메모리에 저장된 검사 데이터가 없습니다.",
                    stationLabel + " LOT 종료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string jobError;
            if (!stationView.TryValidateJobInformation(out jobError))
            {
                MessageBox.Show(this, jobError, stationLabel + " 작업정보 확인",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Editable job fields are committed as one LOT header at LOT END.
            // The immutable save snapshot is unaffected by edits during disk I/O.
            lotState.UpdateJobInformation(stationView.FileName.Trim(),
                stationView.LotNumber.Trim(), stationView.OperatorName.Trim(),
                stationView.SpecText.Trim());
            DateTime completedAt = DateTime.Now;
            LotStorageData lotData = lotState.CreateStorageData(stationLabel, completedAt);
            int savedInspectionCount = lotData.Inspections.Count;
            lotState.IsSaving = true;
            reader.SetSaving(true);
            stationView.SetLotSaving(true);
            stationView.AppendLog(completedAt, "LOT 데이터를 HDD에 저장하는 중입니다...");
            m_ApplicationLogService.Info(
                stationSource,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=LOT_SAVE_STARTED lot=\"{0}\" fileName=\"{1}\" inspectionCount={2}",
                    EscapeLogValue(lotData.LotNumber),
                    EscapeLogValue(lotData.FileName),
                    savedInspectionCount));

            try
            {
                string dataFilePath = await Task.Run(
                    () => m_InspectionStorageService.SaveLot(lotData));
                lotState.RemoveSavedInspections(
                    savedInspectionCount,
                    false);

                bool keepPrepared = lotState.IsPrepared;
                if (!keepPrepared)
                {
                    stationView.ClearPendingSimulationList();
                    stationView.ResetInspectionSummary();
                    stationView.ResetLoadGraph();
                    stationView.StateText = "READY";
                    stationView.StateColor = Color.FromArgb(0, 118, 128);
                }

                stationView.SetLotPrepared(keepPrepared, lotState.InspectionCount);
                stationView.AppendLog(
                    DateTime.Now,
                    "LOT 저장 완료 및 메모리 초기화: " + dataFilePath);
                if (lotData.IndexWarning != null) stationView.AppendLog(DateTime.Now, lotData.IndexWarning);
                m_ApplicationLogService.Info(
                    stationSource,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=LOT_SAVE_COMPLETED lot=\"{0}\" inspectionCount={1} " +
                        "remainingMemoryCount={2} file=\"{3}\" fileBytes={4}",
                        EscapeLogValue(lotData.LotNumber),
                        savedInspectionCount,
                        lotState.InspectionCount,
                        EscapeLogValue(dataFilePath),
                        File.Exists(dataFilePath)
                            ? new FileInfo(dataFilePath).Length
                            : 0L));

                if (pnlLog.Visible)
                {
                    RefreshInspectionLogGrid(false);
                }
                if (showCompletion)
                    MessageBox.Show(this,
                        stationLabel + " LOT 저장이 완료되었습니다." + Environment.NewLine +
                        "검사 횟수: " + savedInspectionCount + Environment.NewLine + dataFilePath +
                        (lotData.IndexWarning == null ? string.Empty : Environment.NewLine + lotData.IndexWarning),
                        "저장 완료", MessageBoxButtons.OK,
                        lotData.IndexWarning == null ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception exception)
            {
                stationView.AppendLog(
                    DateTime.Now,
                    "LOT 저장 실패 - 메모리 데이터 유지: " + exception.Message);
                m_ApplicationLogService.Error(
                    stationSource,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=LOT_SAVE_FAILED lot=\"{0}\" inspectionCount={1} " +
                        "memoryPreserved=true",
                        EscapeLogValue(lotData.LotNumber),
                        savedInspectionCount),
                    exception);
                MessageBox.Show(
                    this,
                    "LOT 데이터를 저장하지 못했습니다. 메모리 데이터는 유지됩니다." +
                    Environment.NewLine + exception.Message,
                    stationLabel + " LOT 저장 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // Intentionally remain SAVING until the user dismisses the
                // completion/error dialog. START requests are ignored until then.
                lotState.IsSaving = false;
                reader.SetSaving(false);
                stationView.SetLotSaving(false);
            }
        }

        private void CompleteAndBufferInspection(
            string stationSource,
            string stationLabel,
            StationView stationView,
            StationCaptureState captureState,
            StationLotState lotState,
            PlcGraphCycleEventArgs cycleEvent)
        {
            DateTime completedAt = DateTime.Now;
            if (captureState.CycleNumber != cycleEvent.CycleNumber)
            {
                m_ApplicationLogService.Warning(
                    stationSource,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=CYCLE_COMPLETION_MISMATCH readerCycle={0} uiCycle={1} " +
                        "readerSampleCount={2} uiSampleCount={3}",
                        cycleEvent.CycleNumber,
                        captureState.CycleNumber,
                        cycleEvent.SampleCount,
                        captureState.Samples.Count));
            }

            m_ApplicationLogService.Info(
                stationSource,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=INSPECTION_EVALUATION_STARTED cycle={0} readerSampleCount={1} " +
                    "uiSampleCount={2} lot=\"{3}\" fileName=\"{4}\" operator=\"{5}\" spec=\"{6}\"",
                    cycleEvent.CycleNumber,
                    cycleEvent.SampleCount,
                    captureState.Samples.Count,
                    EscapeLogValue(stationView.LotNumber),
                    EscapeLogValue(stationView.FileName),
                    EscapeLogValue(stationView.OperatorName),
                    EscapeLogValue(stationView.SpecText)));

            StationInspectionResult result = stationView.CompleteInspection();
            stationView.StateText = "DONE";
            stationView.StateColor = Color.FromArgb(0, 118, 128);

            string verdict = result.IsValid
                ? (result.IsPass ? "GOOD" : "NG")
                : "INVALID";
            string message = result.IsValid
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "검사 완료: PEAK {0:0.000} kgf / SPEC {1:0.000} / {3} " +
                    "(검사 {4}, GOOD {5}, NG {6}, Yield {7:0.00}%)",
                    result.PeakLoadKgf,
                    result.LowerSpecKgf,
                    result.UpperSpecKgf,
                    verdict,
                    result.InspectionCount,
                    result.PassCount,
                    result.FailCount,
                    result.YieldPercent)
                : "검사 판정 INVALID - " + result.ErrorMessage;

            stationView.AppendLog(completedAt, message);

            m_ApplicationLogService.Info(
                stationSource,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=INSPECTION_EVALUATED cycle={0} verdict={1} valid={2} " +
                    "peakKgf={3:0.000000} lowerSpecKgf={4:0.000000} upperSpecKgf={5:0.000000} " +
                    "sampleCount={6} inspectionCount={7} passCount={8} failCount={9} " +
                    "yieldPercent={10:0.00} message=\"{11}\"",
                    cycleEvent.CycleNumber,
                    verdict,
                    result.IsValid,
                    result.PeakLoadKgf,
                    result.LowerSpecKgf,
                    result.UpperSpecKgf,
                    captureState.Samples.Count,
                    result.InspectionCount,
                    result.PassCount,
                    result.FailCount,
                    result.YieldPercent,
                    EscapeLogValue(message)));

            InspectionLogRecord record = new InspectionLogRecord
            {
                Time = completedAt,
                StartedAt = captureState.StartedAt == default(DateTime)
                    ? completedAt
                    : captureState.StartedAt,
                Station = stationLabel,
                LotNumber = lotState.LotNumber ?? stationView.LotNumber,
                FileName = lotState.FileName ?? stationView.FileName,
                OperatorName = lotState.OperatorName ?? stationView.OperatorName,
                Result = verdict,
                PeakLoadKgf = result.PeakLoadKgf,
                LowerSpecKgf = result.LowerSpecKgf,
                UpperSpecKgf = result.UpperSpecKgf,
                SampleCount = captureState.Samples.Count,
                Message = message
            };

            BufferedInspectionData inspection = captureState.Complete(record);
            if (!lotState.IsPrepared)
            {
                EnsureLotPreparedForPlcStart(
                    stationSource,
                    stationLabel,
                    stationView,
                    lotState);
            }

            BufferedInspectionData removedOldestInspection;
            lotState.TryAdd(inspection, out removedOldestInspection);
            if (removedOldestInspection != null)
            {
                stationView.AppendLog(
                    DateTime.Now,
                    "LOT 검사 LIST가 가득 차 가장 오래된 결과를 제거하고 새 결과를 추가했습니다.");
                m_ApplicationLogService.Info(
                    stationSource,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=LOT_OLDEST_INSPECTION_REMOVED cycle={0} lot=\"{1}\" " +
                        "capacity={2} removedVerdict={3} addedVerdict={4}",
                        cycleEvent.CycleNumber,
                        EscapeLogValue(record.LotNumber),
                        lotState.MaximumInspectionCount,
                        removedOldestInspection.Record == null
                            ? string.Empty
                            : removedOldestInspection.Record.Result,
                        verdict));
            }

            stationView.SetBufferedInspectionSummary(lotState.Inspections);
            stationView.SetLotPrepared(true, lotState.InspectionCount);
            stationView.AppendLog(
                DateTime.Now,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "메모리 저장 완료: LOT 검사 {0}/{1}건",
                    lotState.InspectionCount, lotState.MaximumInspectionCount));
            m_ApplicationLogService.Info(
                stationSource,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=INSPECTION_BUFFERED_IN_MEMORY cycle={0} lot=\"{1}\" " +
                    "fileName=\"{2}\" verdict={3} lotInspectionCount={4} sampleCount={5}",
                    cycleEvent.CycleNumber,
                    EscapeLogValue(record.LotNumber),
                    EscapeLogValue(record.FileName),
                    verdict,
                    lotState.InspectionCount,
                    record.SampleCount));

        }

        private static void ApplyConnectionState(
            StationView stationView,
            Label connectionBadge,
            string stationLabel,
            PlcGraphConnectionStateChangedEventArgs e)
        {
            switch (e.State)
            {
                case PlcGraphConnectionState.Connecting:
                    connectionBadge.Text = stationLabel + " 연결 중";
                    connectionBadge.BackColor = Color.FromArgb(243, 156, 18);
                    stationView.StateText = "CONNECT";
                    stationView.StateColor = Color.FromArgb(243, 156, 18);
                    break;

                case PlcGraphConnectionState.Connected:
                    connectionBadge.Text = stationLabel + " PLC 연결";
                    connectionBadge.BackColor = Color.FromArgb(0, 151, 86);
                    stationView.StateText = "READY";
                    stationView.StateColor = Color.FromArgb(0, 118, 128);
                    break;

                case PlcGraphConnectionState.Error:
                    connectionBadge.Text = stationLabel + " 통신 오류";
                    connectionBadge.BackColor = Color.FromArgb(220, 53, 69);
                    stationView.StateText = "ERROR";
                    stationView.StateColor = Color.FromArgb(220, 53, 69);
                    break;

                default:
                    connectionBadge.Text = stationLabel + " 연결 대기";
                    connectionBadge.BackColor = Color.FromArgb(96, 112, 128);
                    stationView.StateText = "WAIT";
                    stationView.StateColor = Color.FromArgb(96, 112, 128);
                    break;
            }
        }

        private void RunOnUiThread(Action action)
        {
            if (action == null || IsDisposed || Disposing || !IsHandleCreated)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(delegate { ExecuteUiAction(action); }));
                }
                catch (InvalidOperationException exception)
                {
                    // 폼 종료 중에는 대기 중인 PLC UI 갱신을 무시합니다.
                    if (m_ApplicationLogService != null)
                    {
                        m_ApplicationLogService.Warning(
                            "APPLICATION",
                            "event=UI_DISPATCH_IGNORED reason=form_closing",
                            exception);
                    }
                }

                return;
            }

            ExecuteUiAction(action);
        }

        private void ExecuteUiAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                if (m_ApplicationLogService != null)
                {
                    m_ApplicationLogService.Error(
                        "APPLICATION",
                        "event=PLC_UI_CALLBACK_FAILED",
                        exception);
                }

                throw;
            }
        }

        private void ShowMainView()
        {
            SelectNavigationPage(m_MainPage, btnMainView);
        }

        private void ShowLogView()
        {
            if (!m_IsAdministrator) return;
            SelectNavigationPage(m_LogPage, btnLogView);
            RefreshInspectionLogGrid(false);
        }

        private static void SetMenuState(KryptonButton button, bool selected)
        {
            Color backColor = selected ? MenuSelectedColor : MenuDefaultColor;
            button.StateCommon.Back.Color1 = backColor;
            button.StateCommon.Back.Color2 = backColor;
            button.StateCommon.Border.Color1 = selected
                ? Color.FromArgb(35, 211, 123)
                : MenuBorderColor;
            button.StateCommon.Border.Color2 = button.StateCommon.Border.Color1;
            button.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;

            button.OverrideDefault.Back.Color1 = backColor;
            button.OverrideDefault.Back.Color2 = backColor;
            button.OverrideDefault.Back.ColorStyle = PaletteColorStyle.Solid;
            button.OverrideDefault.Border.Color1 = button.StateCommon.Border.Color1;
            button.OverrideDefault.Border.Color2 = button.StateCommon.Border.Color1;

            button.OverrideFocus.Back.Color1 = backColor;
            button.OverrideFocus.Back.Color2 = backColor;
            button.OverrideFocus.Back.ColorStyle = PaletteColorStyle.Solid;
            button.OverrideFocus.Border.Color1 = button.StateCommon.Border.Color1;
            button.OverrideFocus.Border.Color2 = button.StateCommon.Border.Color1;
        }

        private void InitializeLogGridData()
        {
            dgvLogs.Rows.Clear();
            m_CurrentLogRecords.Clear();
            lblLogSummary.Text = string.Format(
                "로그: {0}  |  데이터: {1}",
                ApplicationPaths.LogRootDirectory,
                ApplicationPaths.DataRootDirectory);
        }

        private bool RefreshInspectionLogGrid(bool showValidationMessage)
        {
            DateTime fromDate = dtpLogFrom.Value.Date;
            DateTime toDate = dtpLogTo.Value.Date;
            string stationFilter = GetSelectedStationFilter();
            if (fromDate > toDate)
            {
                m_ApplicationLogService.Warning(
                    "LOG-UI",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=INSPECTION_LOG_QUERY_REJECTED reason=invalid_date_range " +
                        "from={0:yyyy-MM-dd} to={1:yyyy-MM-dd} station={2}",
                        fromDate,
                        toDate,
                        stationFilter ?? "ALL"));
                if (showValidationMessage)
                {
                    MessageBox.Show(
                        this,
                        "조회 시작일은 종료일보다 늦을 수 없습니다.",
                        "조회기간 확인",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                return false;
            }

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                m_ApplicationLogService.Info(
                    "LOG-UI",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=INSPECTION_LOG_QUERY_STARTED from={0:yyyy-MM-dd} " +
                        "to={1:yyyy-MM-dd} station={2} requestedByUser={3}",
                        fromDate,
                        toDate,
                        stationFilter ?? "ALL",
                        showValidationMessage));
                Cursor previousCursor = Cursor;
                Cursor = Cursors.WaitCursor;
                try
                {
                    IReadOnlyList<InspectionLogRecord> records =
                        m_InspectionStorageService.GetInspectionLogs(
                             fromDate,
                             toDate,
                            stationFilter);

                    m_CurrentLogRecords.Clear();
                    m_CurrentLogRecords.AddRange(records);

                    dgvLogs.SuspendLayout();
                    try
                    {
                        dgvLogs.Rows.Clear();
                        foreach (InspectionLogRecord record in records)
                        {
                            AddInspectionLogRow(record);
                        }
                    }
                    finally
                    {
                        dgvLogs.ResumeLayout();
                    }

                    lblLogSummary.Text = string.Format(
                        CultureInfo.InvariantCulture,
                        "조회 {0}건  |  {1:yyyy-MM-dd} ~ {2:yyyy-MM-dd}  |  마지막 갱신: {3:yyyy-MM-dd HH:mm:ss}",
                        records.Count,
                        fromDate,
                        toDate,
                        DateTime.Now);
                    stopwatch.Stop();
                    m_ApplicationLogService.Info(
                        "LOG-UI",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=INSPECTION_LOG_QUERY_COMPLETED from={0:yyyy-MM-dd} " +
                            "to={1:yyyy-MM-dd} station={2} recordCount={3} elapsedMs={4}",
                            fromDate,
                            toDate,
                            stationFilter ?? "ALL",
                            records.Count,
                            stopwatch.ElapsedMilliseconds));
                }
                finally
                {
                    Cursor = previousCursor;
                }

                return true;
            }
            catch (Exception exception)
            {
                lblLogSummary.Text = "검사 로그 조회 실패: " + exception.Message;
                m_ApplicationLogService.Error(
                    "LOG-UI",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=INSPECTION_LOG_QUERY_FAILED from={0:yyyy-MM-dd} " +
                        "to={1:yyyy-MM-dd} station={2}",
                        fromDate,
                        toDate,
                        stationFilter ?? "ALL"),
                    exception);

                if (showValidationMessage)
                {
                    MessageBox.Show(
                        this,
                        "검사 로그를 조회하지 못했습니다." + Environment.NewLine +
                        exception.Message,
                        "로그 조회 오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                return false;
            }
        }

        private void AddInspectionLogRow(InspectionLogRecord record)
        {
            int rowIndex = dgvLogs.Rows.Add(
                record.Time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                record.Station,
                record.LotNumber,
                record.FileName,
                record.Result,
                record.PeakLoadKgf.ToString("0.000", CultureInfo.InvariantCulture),
                record.Message);

            DataGridViewCell resultCell =
                dgvLogs.Rows[rowIndex].Cells[colLogResult.Index];
            if ((string.Equals(record.Result, "PASS", StringComparison.OrdinalIgnoreCase) || string.Equals(record.Result, "GOOD", StringComparison.OrdinalIgnoreCase)))
            {
                resultCell.Style.ForeColor = Color.FromArgb(0, 151, 86);
            }
            else if (string.Equals(record.Result, "INVALID", StringComparison.OrdinalIgnoreCase))
            {
                resultCell.Style.ForeColor = Color.FromArgb(232, 139, 0);
            }
            else
            {
                resultCell.Style.ForeColor = Color.FromArgb(220, 53, 69);
            }

            resultCell.Style.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
        }

        private string GetSelectedStationFilter()
        {
            switch (cboLogStation.SelectedIndex)
            {
                case 1:
                    return "#1";
                case 2:
                    return "#2";
                default:
                    return null;
            }
        }

        private void btnSearchLog_Click(object sender, EventArgs e)
        {
            RefreshInspectionLogGrid(true);
        }

        private void btnExportLog_Click(object sender, EventArgs e)
        {
            if (m_CurrentLogRecords.Count == 0)
            {
                m_ApplicationLogService.Warning(
                    "LOG-UI",
                    "event=INSPECTION_LOG_EXPORT_REJECTED reason=no_records");
                MessageBox.Show(
                    this,
                    "내보낼 검사 로그가 없습니다. 먼저 조회를 실행하세요.",
                    "CSV 내보내기",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "검사 로그 CSV 내보내기";
                dialog.InitialDirectory = ApplicationPaths.LogRootDirectory;
                dialog.Filter = "CSV 파일 (*.csv)|*.csv|모든 파일 (*.*)|*.*";
                dialog.DefaultExt = "csv";
                dialog.AddExtension = true;
                dialog.FileName = string.Format(
                    CultureInfo.InvariantCulture,
                    "SOFCMeas_Log_{0:yyyyMMdd}_{1:yyyyMMdd}.csv",
                    dtpLogFrom.Value.Date,
                    dtpLogTo.Value.Date);

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    m_ApplicationLogService.Info(
                        "LOG-UI",
                        "event=INSPECTION_LOG_EXPORT_CANCELLED");
                    return;
                }

                try
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    m_ApplicationLogService.Info(
                        "LOG-UI",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=INSPECTION_LOG_EXPORT_STARTED recordCount={0} file=\"{1}\"",
                            m_CurrentLogRecords.Count,
                            EscapeLogValue(dialog.FileName)));
                    m_InspectionStorageService.ExportInspectionLogs(
                        dialog.FileName,
                        m_CurrentLogRecords);
                    stopwatch.Stop();
                    m_ApplicationLogService.Info(
                        "LOG-UI",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=INSPECTION_LOG_EXPORT_COMPLETED recordCount={0} " +
                            "file=\"{1}\" fileBytes={2} elapsedMs={3}",
                            m_CurrentLogRecords.Count,
                            EscapeLogValue(dialog.FileName),
                            File.Exists(dialog.FileName)
                                ? new FileInfo(dialog.FileName).Length
                                : 0L,
                            stopwatch.ElapsedMilliseconds));
                    MessageBox.Show(
                        this,
                        "CSV 내보내기가 완료되었습니다.",
                        "CSV 내보내기",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception exception)
                {
                    m_ApplicationLogService.Error(
                        "LOG-UI",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "event=INSPECTION_LOG_EXPORT_FAILED recordCount={0} file=\"{1}\"",
                            m_CurrentLogRecords.Count,
                            EscapeLogValue(dialog.FileName)),
                        exception);
                    MessageBox.Show(
                        this,
                        "CSV 내보내기에 실패했습니다." + Environment.NewLine +
                        exception.Message,
                        "CSV 내보내기 오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private async void btnApplyLogPolicy_Click(object sender, EventArgs e)
        {
            int retentionDays = Decimal.ToInt32(numLogRetentionDays.Value);
            bool autoDelete = chkLogAutoDelete.Checked;
            DateTime firstRetainedDate = DateTime.Today.AddDays(-(retentionDays - 1));
            DialogResult confirmation = MessageBox.Show(
                this,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "로그 보관기간을 {0}일로 저장합니다.{1}" +
                    "{2:yyyy-MM-dd} 이전의 로그 날짜 폴더만 지금 정리합니다.{1}" +
                    "검사 데이터 폴더는 삭제하지 않습니다.{1}" +
                    "계속하시겠습니까?",
                    retentionDays,
                    Environment.NewLine,
                    firstRetainedDate),
                "로그 정책 저장",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.Yes)
            {
                m_ApplicationLogService.Info(
                    "LOG-POLICY",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=LOG_POLICY_CHANGE_CANCELLED requestedRetentionDays={0} " +
                        "requestedAutoDelete={1}",
                        retentionDays,
                        autoDelete));
                return;
            }

            try
            {
                m_ApplicationLogService.Info(
                    "LOG-POLICY",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=LOG_POLICY_CHANGE_STARTED oldRetentionDays={0} oldAutoDelete={1} " +
                        "newRetentionDays={2} newAutoDelete={3}",
                        m_StoragePolicySettings.LogRetentionDays,
                        m_StoragePolicySettings.LogAutoDelete,
                        retentionDays,
                        autoDelete));
                m_StoragePolicySettings.SaveLogRetentionPolicy(
                    retentionDays,
                    autoDelete);
                m_ApplicationLogService.Info(
                    "LOG-POLICY",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=LOG_POLICY_CHANGE_COMPLETED retentionDays={0} autoDelete={1}",
                        retentionDays,
                        autoDelete));
                UpdateLogPolicyStatus("정책 저장 완료");
                await RunLogCleanupAsync(true);
            }
            catch (Exception exception)
            {
                UpdateLogPolicyStatus("정책 저장 실패");
                m_ApplicationLogService.Error(
                    "LOG-POLICY",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=LOG_POLICY_CHANGE_FAILED requestedRetentionDays={0} " +
                        "requestedAutoDelete={1}",
                        retentionDays,
                        autoDelete),
                    exception);
                MessageBox.Show(
                    this,
                    "로그 정책을 저장하지 못했습니다." + Environment.NewLine +
                    exception.Message,
                    "로그 정책 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void LogCleanupTimer_Tick(object sender, EventArgs e)
        {
            RefreshLogStorageStatus();
            await RecoverInspectionIndexesAsync();
            await RunLogCleanupAsync(false);
        }

        private async Task<LogCleanupResult> RunLogCleanupAsync(bool force)
        {
            DateTime today = DateTime.Today;
            if (m_LogCleanupRunning)
            {
                m_ApplicationLogService.Warning(
                    "LOG-CLEANUP",
                    "event=LOG_CLEANUP_SKIPPED reason=already_running force=" + force);
                return null;
            }

            if (!force && !m_StoragePolicySettings.LogAutoDelete)
            {
                m_ApplicationLogService.Info(
                    "LOG-CLEANUP",
                    "event=LOG_CLEANUP_SKIPPED reason=auto_delete_disabled force=false");
                return null;
            }

            if (!force && m_LastLogCleanupDate == today)
            {
                m_ApplicationLogService.Info(
                    "LOG-CLEANUP",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=LOG_CLEANUP_SKIPPED reason=already_completed_today date={0:yyyy-MM-dd}",
                        today));
                return null;
            }

            m_LogCleanupRunning = true;
            btnApplyLogPolicy.Enabled = false;
            UpdateLogPolicyStatus("만료 로그 정리 중...");

            try
            {
                int retentionDays = m_StoragePolicySettings.LogRetentionDays;
                Stopwatch stopwatch = Stopwatch.StartNew();
                m_ApplicationLogService.Info(
                    "LOG-CLEANUP",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=LOG_CLEANUP_STARTED force={0} retentionDays={1} " +
                        "firstRetainedDate={2:yyyy-MM-dd} root=\"{3}\" " +
                        "scope=log_only dataDeletion=false",
                        force,
                        retentionDays,
                        today.AddDays(-(retentionDays - 1)),
                        EscapeLogValue(ApplicationPaths.LogRootDirectory)));
                LogCleanupResult result = await Task.Run(
                    () => m_LogCleanupService.DeleteExpiredDateDirectories(
                        retentionDays,
                        today));
                stopwatch.Stop();
                m_LastLogCleanupDate = today;

                string cleanupMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "정리: 파일 {0}, 폴더 {1}, 실패 {2}",
                    result.DeletedFileCount,
                    result.DeletedDayDirectoryCount,
                    result.FailedDayDirectoryCount);
                m_ApplicationLogService.Info(
                    "LOG-CLEANUP",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=LOG_CLEANUP_COMPLETED force={0} firstRetainedDate={1:yyyy-MM-dd} " +
                        "deletedFiles={2} deletedDayDirectories={3} failedDayDirectories={4} " +
                        "elapsedMs={5} scope=log_only dataDeletion=false",
                        force,
                        result.FirstRetainedDate,
                        result.DeletedFileCount,
                        result.DeletedDayDirectoryCount,
                        result.FailedDayDirectoryCount,
                        stopwatch.ElapsedMilliseconds));
                foreach (string failureDetail in result.FailureDetails)
                {
                    m_ApplicationLogService.Warning(
                        "LOG-CLEANUP",
                        "event=LOG_CLEANUP_DIRECTORY_FAILED detail=\"" +
                        EscapeLogValue(failureDetail) + "\"");
                }

                if (!IsDisposed && !Disposing)
                {
                    UpdateLogPolicyStatus(cleanupMessage);
                    if (pnlLog.Visible && result.DeletedFileCount > 0)
                    {
                        RefreshInspectionLogGrid(false);
                    }
                }

                return result;
            }
            catch (Exception exception)
            {
                m_ApplicationLogService.Error(
                    "LOG-CLEANUP",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=LOG_CLEANUP_FAILED force={0} retentionDays={1} root=\"{2}\"",
                        force,
                        m_StoragePolicySettings.LogRetentionDays,
                        EscapeLogValue(ApplicationPaths.LogRootDirectory)),
                    exception);
                if (!IsDisposed && !Disposing)
                {
                    UpdateLogPolicyStatus("자동정리 실패: " + exception.Message);
                }
                return null;
            }
            finally
            {
                m_LogCleanupRunning = false;
                if (!IsDisposed)
                {
                    btnApplyLogPolicy.Enabled = true;
                }
            }
        }

        private void UpdateLogPolicyStatus(string status)
        {
            lblLogPolicyStatus.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0}MB/파일 | {1}일 | 자동 {2} | {3}",
                m_StoragePolicySettings.MaximumLogFileSizeMb,
                m_StoragePolicySettings.LogRetentionDays,
                m_StoragePolicySettings.LogAutoDelete ? "ON" : "OFF",
                status);
        }

        private void SOFCMeas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private async void SOFCMeas_Shown(object sender, EventArgs e)
        {
            m_ApplicationLogService.Info(
                "APPLICATION",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=APPLICATION_STARTED product=SOFCMeas version={0} session={1} " +
                    "machine=\"{2}\" os=\"{3}\" process64Bit={4} clr=\"{5}\"",
                    Application.ProductVersion,
                    ApplicationLogService.CurrentSessionId,
                    EscapeLogValue(Environment.MachineName),
                    EscapeLogValue(Environment.OSVersion.VersionString),
                    Environment.Is64BitProcess,
                    EscapeLogValue(Environment.Version.ToString())));
            try
            {
                m_ApplicationLogService.Info(
                    "APPLICATION",
                    "event=PLC_READERS_START_REQUESTED stations=STATION-1,STATION-2");
                if (SettingsCatalog.Load()["Runtime.Simulation"] != "1")
                {
                    m_Station1Reader.Start();
                    m_Station2Reader.Start();
                }
            }
            catch (Exception exception)
            {
                m_ApplicationLogService.Error(
                    "APPLICATION",
                    "event=PLC_READERS_START_FAILED",
                    exception);
                throw;
            }

            await RunLogCleanupAsync(false);
        }

        private void SOFCMeas_FormClosing(object sender, FormClosingEventArgs e)
        {
            int unsavedInspectionCount =
                m_Station1Lot.InspectionCount + m_Station2Lot.InspectionCount;
            bool collectionActive =
                m_Station1Capture.IsActive || m_Station2Capture.IsActive;
            bool saveRunning = m_Station1Lot.IsSaving || m_Station2Lot.IsSaving;

            if (e.CloseReason != CloseReason.UserClosing)
            {
                return;
            }

            DialogResult result = MessageBox.Show(
                this,
                (!collectionActive && !saveRunning && unsavedInspectionCount == 0)
                    ? "프로그램을 종료하시겠습니까?"
                    : string.Format(
                    CultureInfo.InvariantCulture,
                    "아직 HDD에 저장하지 않은 검사 데이터가 {0}건 있습니다.{1}" +
                    "종료하면 메모리 데이터가 사라집니다. 계속 종료하시겠습니까?",
                    unsavedInspectionCount,
                    Environment.NewLine),
                "프로그램 종료 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                m_ApplicationLogService.Info(
                    "APPLICATION",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=APPLICATION_CLOSE_CANCELLED unsavedInspectionCount={0} " +
                        "collectionActive={1} saveRunning={2}",
                        unsavedInspectionCount,
                        collectionActive,
                        saveRunning));
                return;
            }

            m_ApplicationLogService.Warning(
                "APPLICATION",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "event=UNSAVED_LOT_DATA_DISCARDED station1Count={0} station2Count={1} " +
                    "collectionActive={2} saveRunning={3}",
                    m_Station1Lot.InspectionCount,
                    m_Station2Lot.InspectionCount,
                    collectionActive,
                    saveRunning));
        }

        private void SOFCMeas_FormClosed(object sender, FormClosedEventArgs e)
        {
            using (var splash = new ProgressSplash("프로그램 종료 중"))
            {
            splash.Report(10, "종료 작업을 시작합니다.");
            if (m_ApplicationLogService != null)
            {
                m_ApplicationLogService.Info(
                    "APPLICATION",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "event=APPLICATION_STOPPING closeReason={0} uptimeSeconds={1:0.000}",
                        e.CloseReason,
                        (DateTime.Now - m_ApplicationStartedAt).TotalSeconds));
            }

            if (m_LogCleanupTimer != null)
            {
                m_LogCleanupTimer.Stop();
                m_LogCleanupTimer.Tick -= LogCleanupTimer_Tick;
                m_LogCleanupTimer.Dispose();
                m_LogCleanupTimer = null;
            }

            if (m_Station1Reader != null)
            {
                splash.Report(35, "설비 #1 PLC 통신을 종료합니다.");
                m_Station1Reader.Dispose();
                m_Station1Reader = null;
            }

            if (m_Station2Reader != null)
            {
                splash.Report(65, "설비 #2 PLC 통신을 종료합니다.");
                m_Station2Reader.Dispose();
                m_Station2Reader = null;
            }

            if (m_ApplicationLogService != null)
            {
                m_ApplicationLogService.Info(
                    "APPLICATION",
                    string.Format(
                        CultureInfo.InvariantCulture,
                    "event=APPLICATION_STOPPED uptimeSeconds={0:0.000}",
                        (DateTime.Now - m_ApplicationStartedAt).TotalSeconds));
            }
            splash.Report(100, "종료 완료");
            }
        }
    }
}




