using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using McpXLib;
using McpXPrefix = McpXLib.Enums.Prefix;
using McpXProcessorSeries = McpXLib.Enums.ProcessorSeries;
using McpXRequestFrame = McpXLib.Enums.RequestFrame;

namespace SOFCMeas
{
    internal enum PlcGraphConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    internal enum PlcAcquisitionState
    {
        Disabled,
        WaitStart,
        Collecting,
        Evaluating,
        Saving
    }

    internal sealed class PlcGraphConnectionStateChangedEventArgs : EventArgs
    {
        internal PlcGraphConnectionStateChangedEventArgs(
            PlcGraphConnectionState state,
            Exception error)
        {
            State = state;
            Error = error;
        }

        internal PlcGraphConnectionState State { get; private set; }
        internal Exception Error { get; private set; }
    }

    internal sealed class PlcLoadSampleEventArgs : EventArgs
    {
        internal PlcLoadSampleEventArgs(
            long cycleNumber,
            int sampleNumber,
            int rawValue,
            double loadKgf,
            DateTime readTime)
        {
            CycleNumber = cycleNumber;
            SampleNumber = sampleNumber;
            RawValue = rawValue;
            LoadKgf = loadKgf;
            ReadTime = readTime;
        }

        internal long CycleNumber { get; private set; }
        internal int SampleNumber { get; private set; }
        internal int RawValue { get; private set; }
        internal ushort[] LoadWords { get; set; }
        internal double LoadKgf { get; private set; }
        internal DateTime ReadTime { get; private set; }
    }

    internal sealed class PlcGraphCycleEventArgs : EventArgs
    {
        internal PlcGraphCycleEventArgs(
            long cycleNumber,
            DateTime eventTime,
            DateTime startedAt,
            int sampleCount)
        {
            CycleNumber = cycleNumber;
            EventTime = eventTime;
            StartedAt = startedAt;
            SampleCount = sampleCount;
        }

        internal long CycleNumber { get; private set; }
        internal DateTime EventTime { get; private set; }
        internal DateTime StartedAt { get; private set; }
        internal int SampleCount { get; private set; }
    }

    internal enum PlcGraphLogLevel
    {
        Info,
        Warning,
        Error
    }

    internal sealed class PlcGraphLogEventArgs : EventArgs
    {
        internal PlcGraphLogEventArgs(
            DateTime time,
            PlcGraphLogLevel level,
            string eventName,
            string message,
            Exception error,
            bool displayInStationLog)
        {
            Time = time;
            Level = level;
            EventName = eventName;
            Message = message;
            Error = error;
            DisplayInStationLog = displayInStationLog;
        }

        internal DateTime Time { get; private set; }
        internal PlcGraphLogLevel Level { get; private set; }
        internal string EventName { get; private set; }
        internal string Message { get; private set; }
        internal Exception Error { get; private set; }
        internal bool DisplayInStationLog { get; private set; }
    }

    internal sealed class PlcGraphReader : IDisposable
    {
        private sealed class DeviceAddress
        {
            internal DeviceAddress(McpXPrefix prefix, string address)
            {
                Prefix = prefix;
                Address = address;
            }

            internal McpXPrefix Prefix { get; private set; }
            internal string Address { get; private set; }
        }

        private readonly object m_SyncRoot = new object();
        private bool m_CollectionEnabled;
        private int m_CollectionVersion;
        private PlcAcquisitionState m_AcquisitionState =
            PlcAcquisitionState.Disabled;
        private PlcAcquisitionState? m_LastIgnoredStartState;
        internal int CollectionVersion { get { lock (m_SyncRoot) return m_CollectionVersion; } }
        internal PlcAcquisitionState AcquisitionState
        {
            get { lock (m_SyncRoot) return m_AcquisitionState; }
        }

        internal void SetCollectionEnabled(bool enabled)
        {
            lock (m_SyncRoot)
            {
                m_CollectionEnabled = enabled;
                m_CollectionVersion++;
                m_AcquisitionState = enabled
                    ? PlcAcquisitionState.WaitStart
                    : PlcAcquisitionState.Disabled;
                m_LastIgnoredStartState = null;
                m_Protocol.ResetConnectionState();
                m_CycleActive = false;
            }
        }

        internal void CompleteEvaluation()
        {
            bool changed = false;
            PlcAcquisitionState nextState = PlcAcquisitionState.Disabled;
            lock (m_SyncRoot)
            {
                if (m_AcquisitionState != PlcAcquisitionState.Evaluating)
                {
                    return;
                }

                nextState = m_CollectionEnabled
                    ? PlcAcquisitionState.WaitStart
                    : PlcAcquisitionState.Disabled;
                m_AcquisitionState = nextState;
                m_CollectionVersion++;
                m_LastIgnoredStartState = null;
                m_Protocol.ResetConnectionState();
                m_CycleActive = false;
                changed = true;
            }

            if (changed)
            {
                RaiseLogMessage(
                    PlcGraphLogLevel.Info,
                    "ACQUISITION_STATE_CHANGED",
                    "from=EVALUATING to=" + nextState.ToString().ToUpperInvariant(),
                    null,
                    false);
            }
        }

        internal void SetSaving(bool saving)
        {
            PlcAcquisitionState previousState;
            PlcAcquisitionState nextState;
            lock (m_SyncRoot)
            {
                previousState = m_AcquisitionState;
                nextState = saving
                    ? PlcAcquisitionState.Saving
                    : m_CollectionEnabled
                        ? PlcAcquisitionState.WaitStart
                        : PlcAcquisitionState.Disabled;
                m_AcquisitionState = nextState;
                m_CollectionVersion++;
                m_LastIgnoredStartState = null;
                m_Protocol.ResetConnectionState();
                m_CycleActive = false;
            }

            RaiseLogMessage(
                PlcGraphLogLevel.Info,
                "ACQUISITION_STATE_CHANGED",
                "from=" + previousState.ToString().ToUpperInvariant() +
                " to=" + nextState.ToString().ToUpperInvariant(),
                null,
                false);
        }
        private PlcGraphSettings m_Settings;
        private readonly PlcGraphProtocol m_Protocol = new PlcGraphProtocol();
        private DeviceAddress m_RequestDevice;
        private DeviceAddress m_LoadRawDevice;

        private CancellationTokenSource m_Cancellation;
        private Task m_WorkerTask;
        private McpX m_Client;
        private bool m_Disposed;
        private int m_ConnectionAttempt;
        private long m_CycleNumber;
        private int m_CycleSampleCount;
        private DateTime m_CycleStartedAt;
        private bool m_CycleActive;
        private DateTime m_LastHeartbeatAt;

        internal PlcGraphReader(PlcGraphSettings settings)
        {
            m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            m_RequestDevice = ParseDeviceAddress(settings.RequestDevice);
            m_LoadRawDevice = ParseDeviceAddress(settings.LoadRawDevice);
        }

        internal event EventHandler<PlcGraphConnectionStateChangedEventArgs> ConnectionStateChanged;
        internal event EventHandler<PlcGraphCycleEventArgs> GraphResetRequested;
        internal event EventHandler<PlcLoadSampleEventArgs> LoadSampleReceived;
        internal event EventHandler<PlcGraphCycleEventArgs> GraphFreezeRequested;
        internal event EventHandler<PlcGraphLogEventArgs> LogMessage;

        internal PlcGraphSettings Settings
        {
            get { return m_Settings; }
        }

        internal void Start(bool manualRequest = false)
        {
            lock (m_SyncRoot)
            {
                ThrowIfDisposed();

                if (m_WorkerTask != null)
                {
                    RaiseLogMessage(
                        PlcGraphLogLevel.Warning,
                        "READER_START_IGNORED",
                        "reason=already_started",
                        null,
                        false);
                    return;
                }

                if (!manualRequest && !m_Settings.AutoConnect)
                {
                    RaiseConnectionStateChanged(PlcGraphConnectionState.Disconnected, null);
                    RaiseLogMessage(
                        PlcGraphLogLevel.Warning,
                        "AUTO_CONNECT_DISABLED",
                        "autoConnect=false",
                        null,
                        true);
                    return;
                }

                m_Cancellation = new CancellationTokenSource();
                CancellationToken token = m_Cancellation.Token;
                RaiseLogMessage(
                    PlcGraphLogLevel.Info,
                    "READER_STARTED",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "endpoint={0}:{1} pollMs={2} timeoutMs={3} reconnectMs={4}",
                        m_Settings.IpAddress,
                        m_Settings.Port,
                        m_Settings.PollIntervalMilliseconds,
                        m_Settings.TimeoutMilliseconds,
                        m_Settings.ReconnectDelayMilliseconds),
                    null,
                    false);
                m_WorkerTask = Task.Run(() => RunAsync(token));
            }
        }

        internal async Task ApplySettingsAsync(
            PlcGraphSettings settings,
            bool startReader)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            DeviceAddress requestDevice = ParseDeviceAddress(settings.RequestDevice);
            DeviceAddress loadRawDevice = ParseDeviceAddress(settings.LoadRawDevice);

            CancellationTokenSource cancellation;
            Task worker;
            McpX client;
            PlcGraphSettings previousSettings;
            lock (m_SyncRoot)
            {
                ThrowIfDisposed();
                if (settings.StationNumber != m_Settings.StationNumber)
                    throw new ArgumentException("동일한 설비의 PLC 설정만 적용할 수 있습니다.", nameof(settings));

                previousSettings = m_Settings;
                cancellation = m_Cancellation;
                worker = m_WorkerTask;
                client = m_Client;
                m_Cancellation = null;
                m_WorkerTask = null;
                m_Client = null;
                // Invalidate any callbacks already queued by the old polling loop.
                // This keeps one inspection cycle on one complete settings set.
                m_CollectionVersion++;
                m_AcquisitionState = PlcAcquisitionState.Disabled;
                m_LastIgnoredStartState = null;
                m_Protocol.ResetConnectionState();
                m_CycleActive = false;
            }

            if (cancellation != null) cancellation.Cancel();
            if (client != null) DisposeClient(client);

            if (worker != null)
            {
                int stopTimeoutMilliseconds = Math.Max(
                    1000,
                    previousSettings.TimeoutMilliseconds + 500);
                Task completed = await Task.WhenAny(
                        worker,
                        Task.Delay(stopTimeoutMilliseconds))
                    .ConfigureAwait(false);
                if (!ReferenceEquals(completed, worker))
                {
                    RaiseLogMessage(
                        PlcGraphLogLevel.Error,
                        "READER_RECONFIGURE_TIMEOUT",
                        "timeoutMs=" + stopTimeoutMilliseconds.ToString(CultureInfo.InvariantCulture),
                        null,
                        true);
                    throw new TimeoutException("PLC 통신 설정을 적용하는 동안 기존 통신 종료 시간이 초과되었습니다.");
                }

                try
                {
                    await worker.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // 설정 변경을 위해 요청한 정상적인 취소입니다.
                }
            }

            if (cancellation != null) cancellation.Dispose();

            lock (m_SyncRoot)
            {
                ThrowIfDisposed();
                m_Settings = settings;
                m_RequestDevice = requestDevice;
                m_LoadRawDevice = loadRawDevice;
                m_CollectionVersion++;
                m_AcquisitionState = m_CollectionEnabled
                    ? PlcAcquisitionState.WaitStart
                    : PlcAcquisitionState.Disabled;
                m_LastIgnoredStartState = null;
                m_Protocol.ResetConnectionState();
                m_CycleActive = false;
                m_CycleSampleCount = 0;
                m_LastHeartbeatAt = DateTime.MinValue;
            }

            RaiseConnectionStateChanged(PlcGraphConnectionState.Disconnected, null);
            RaiseLogMessage(
                PlcGraphLogLevel.Info,
                "READER_SETTINGS_APPLIED",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "endpoint={0}:{1} pollMs={2} reconnectMs={3} timeoutMs={4} " +
                    "requestDevice={5} loadRaw={6}",
                    settings.IpAddress,
                    settings.Port,
                    settings.PollIntervalMilliseconds,
                    settings.ReconnectDelayMilliseconds,
                    settings.TimeoutMilliseconds,
                    settings.RequestDevice,
                    settings.LoadRawDevice),
                null,
                true);

            if (startReader) Start();
        }

        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                McpX client = null;
                DateTime connectStartedAt = DateTime.Now;
                int attempt = Interlocked.Increment(ref m_ConnectionAttempt);

                try
                {
                    RaiseConnectionStateChanged(PlcGraphConnectionState.Connecting, null);
                    RaiseLogMessage(
                        PlcGraphLogLevel.Info,
                        "PLC_CONNECT_ATTEMPT",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "attempt={0} endpoint={1}:{2}",
                            attempt,
                            m_Settings.IpAddress,
                            m_Settings.Port),
                        null,
                        true);

                    client = CreateClient();

                    lock (m_SyncRoot)
                    {
                        if (m_Disposed || token.IsCancellationRequested)
                        {
                            DisposeClient(client);
                            return;
                        }

                        m_Client = client;
                    }

                    m_Protocol.ResetConnectionState();
                    m_LastHeartbeatAt = DateTime.MinValue;
                    RaiseConnectionStateChanged(PlcGraphConnectionState.Connected, null);
                    RaiseLogMessage(
                        PlcGraphLogLevel.Info,
                        "PLC_CONNECTED",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "attempt={0} endpoint={1}:{2} connectElapsedMs={3:0}",
                            attempt,
                            m_Settings.IpAddress,
                            m_Settings.Port,
                            (DateTime.Now - connectStartedAt).TotalMilliseconds),
                        null,
                        true);

                    await MonitorAsync(client, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    Exception error = UnwrapException(exception);

                    if (!token.IsCancellationRequested)
                    {
                        RaiseConnectionStateChanged(PlcGraphConnectionState.Error, error);
                        RaiseLogMessage(
                            PlcGraphLogLevel.Error,
                            "PLC_COMMUNICATION_FAILED",
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "attempt={0} endpoint={1}:{2} cycle={3} cycleActive={4} " +
                                "sampleCount={5}",
                                attempt,
                                m_Settings.IpAddress,
                                m_Settings.Port,
                                m_CycleNumber,
                                m_CycleActive,
                                m_CycleSampleCount),
                            error,
                            true);

                        if (m_CycleActive)
                        {
                            RaiseLogMessage(
                                PlcGraphLogLevel.Warning,
                                "CYCLE_INTERRUPTED",
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "cycle={0} sampleCount={1} reason=connection_lost",
                                    m_CycleNumber,
                                    m_CycleSampleCount),
                                null,
                                true);
                            m_CycleActive = false;
                        }
                    }
                }
                finally
                {
                    lock (m_SyncRoot)
                    {
                        if (ReferenceEquals(m_Client, client))
                        {
                            m_Client = null;
                        }
                    }

                    if (client != null)
                    {
                        DisposeClient(client);
                    }

                    RaiseLogMessage(
                        PlcGraphLogLevel.Info,
                        "PLC_CONNECTION_CLOSED",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "attempt={0} cancellationRequested={1}",
                            attempt,
                            token.IsCancellationRequested),
                        null,
                        false);
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    RaiseLogMessage(
                        PlcGraphLogLevel.Warning,
                        "PLC_RECONNECT_SCHEDULED",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "nextAttempt={0} delayMs={1}",
                            attempt + 1,
                            m_Settings.ReconnectDelayMilliseconds),
                        null,
                        true);
                    await Task.Delay(
                            m_Settings.ReconnectDelayMilliseconds,
                            token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task MonitorAsync(McpX client, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Stopwatch pollStopwatch = Stopwatch.StartNew();
                int collectionVersion;
                lock (m_SyncRoot) collectionVersion = m_CollectionVersion;
                PlcGraphSnapshot snapshot = await ReadSnapshotAsync(
                        client,
                        collectionVersion)
                    .ConfigureAwait(false);

                PlcGraphUpdate update = null;
                PlcAcquisitionState acquisitionState;
                PlcAcquisitionState? ignoredStartState = null;
                lock (m_SyncRoot)
                {
                    if (collectionVersion != m_CollectionVersion) continue;
                    acquisitionState = m_AcquisitionState;
                    if (acquisitionState == PlcAcquisitionState.Evaluating ||
                        acquisitionState == PlcAcquisitionState.Saving ||
                        acquisitionState == PlcAcquisitionState.Disabled)
                    {
                        if (snapshot.RequestValue == m_Settings.StartRequestValue &&
                            m_LastIgnoredStartState != acquisitionState)
                        {
                            m_LastIgnoredStartState = acquisitionState;
                            ignoredStartState = acquisitionState;
                        }
                        else if (snapshot.RequestValue != m_Settings.StartRequestValue)
                        {
                            m_LastIgnoredStartState = null;
                        }
                    }
                    else
                    {
                        m_LastIgnoredStartState = null;
                        update = m_Protocol.Process(
                            snapshot,
                            m_Settings.StartRequestValue,
                            m_Settings.EndRequestValue,
                            m_CollectionEnabled);

                        if (update.ResetGraph)
                        {
                            m_AcquisitionState = PlcAcquisitionState.Collecting;
                        }
                        else if (update.FreezeGraph)
                        {
                            m_AcquisitionState = PlcAcquisitionState.Evaluating;
                        }

                        acquisitionState = m_AcquisitionState;
                        ApplyUpdate(update);
                    }
                }

                if (ignoredStartState.HasValue)
                {
                    RaiseLogMessage(
                        PlcGraphLogLevel.Info,
                        "PLC_START_IGNORED",
                        "state=" + ignoredStartState.Value.ToString().ToUpperInvariant() +
                        " requestDevice=" + m_Settings.RequestDevice +
                        " requestValue=" + snapshot.RequestValue.ToString(
                            CultureInfo.InvariantCulture),
                        null,
                        true);
                }

                if (m_LastHeartbeatAt == DateTime.MinValue ||
                    snapshot.ReadTime - m_LastHeartbeatAt >= TimeSpan.FromSeconds(10))
                {
                    m_LastHeartbeatAt = snapshot.ReadTime;
                    RaiseLogMessage(
                        PlcGraphLogLevel.Info,
                        "PLC_HEARTBEAT",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "requestDevice={0} requestValue={1} loadRaw={2} " +
                            "collecting={3} cycle={4} sampleCount={5}",
                            m_Settings.RequestDevice,
                            snapshot.RequestValue,
                            snapshot.LoadRaw,
                            acquisitionState == PlcAcquisitionState.Collecting,
                            m_CycleNumber,
                            m_CycleSampleCount) +
                        " acquisitionState=" + acquisitionState.ToString().ToUpperInvariant(),
                        null,
                        false);
                }

                int remainingDelay = Math.Max(
                    0,
                    m_Settings.PollIntervalMilliseconds -
                    checked((int)Math.Min(int.MaxValue, pollStopwatch.ElapsedMilliseconds)));
                if (remainingDelay > 0)
                {
                    await Task.Delay(remainingDelay, token).ConfigureAwait(false);
                }
            }
        }

        private async Task<PlcGraphSnapshot> ReadSnapshotAsync(
            McpX client,
            int collectionVersion)
        {
            PlcGraphSnapshot snapshot = new PlcGraphSnapshot();

            await client.OptimizedReadAsync(builder =>
            {
                builder.Add<ushort>(
                        m_RequestDevice.Prefix,
                        m_RequestDevice.Address,
                        value => snapshot.RequestValue = value);
            }).ConfigureAwait(false);

            bool readLoadRaw;
            lock (m_SyncRoot)
            {
                readLoadRaw = collectionVersion == m_CollectionVersion &&
                    m_CollectionEnabled &&
                    (m_AcquisitionState == PlcAcquisitionState.WaitStart ||
                     m_AcquisitionState == PlcAcquisitionState.Collecting) &&
                    snapshot.RequestValue == m_Settings.StartRequestValue;
            }

            if (readLoadRaw)
            {
                // Read the complete five-WORD frame together, including STX, sign and ETX.
                snapshot.LoadWords = await client.BatchReadAsync<ushort>(
                    m_LoadRawDevice.Prefix,
                    m_LoadRawDevice.Address,
                    PlcLoadDecoder.WordCount).ConfigureAwait(false);

                double loadKgf;
                string invalidReason;
                if (PlcLoadDecoder.TryDecodeFrame(snapshot.LoadWords, out loadKgf, out invalidReason))
                {
                    snapshot.LoadKgf = loadKgf;
                    // Keep the existing integer raw representation for legacy consumers.
                    // The parsed ASCII decimal remains the authoritative kgf value.
                    snapshot.LoadRaw = PlcLoadDecoder.ToLegacyRawValue(loadKgf, m_Settings.LoadScale);
                }
                else
                {
                    snapshot.InvalidLoadValue = true;
                    RaiseLogMessage(
                        PlcGraphLogLevel.Warning,
                        "PLC_LOAD_DATA_INVALID",
                        string.Format(CultureInfo.InvariantCulture,
                            "알람: PLC 하중 프레임 오류. 샘플 폐기 후 수집 계속. " +
                            "device={0} words=[{1}] reason={2} action=discard_sample_continue",
                            m_Settings.LoadRawDevice,
                            FormatLoadWords(snapshot.LoadWords),
                            invalidReason),
                        null,
                        true);
                }
            }

            snapshot.ReadTime = DateTime.Now;
            return snapshot;
        }

        private void ApplyUpdate(PlcGraphUpdate update)
        {
            if (update.RequestChanged)
            {
                string signal = update.RequestValue == m_Settings.StartRequestValue
                    ? "START_REQ"
                    : update.RequestValue == m_Settings.EndRequestValue
                        ? "END_REQ"
                        : "UNKNOWN_REQ";
                RaiseLogMessage(
                    update.InvalidRequestValue
                        ? PlcGraphLogLevel.Warning
                        : PlcGraphLogLevel.Info,
                    "PLC_REQUEST_CHANGED",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "signal={0} device={1} value={2} readTime={3:O}",
                        signal,
                        m_Settings.RequestDevice,
                        update.RequestValue,
                        update.ReadTime),
                    null,
                    true);
            }

            if (update.ResetGraph)
            {
                if (m_CycleActive)
                {
                    RaiseLogMessage(
                        PlcGraphLogLevel.Warning,
                        "CYCLE_RESTARTED",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "previousCycle={0} previousSampleCount={1}",
                            m_CycleNumber,
                            m_CycleSampleCount),
                        null,
                        true);
                }

                m_CycleNumber++;
                m_CycleSampleCount = 0;
                m_CycleStartedAt = update.ReadTime;
                m_CycleActive = true;

                RaiseGraphResetRequested(new PlcGraphCycleEventArgs(
                    m_CycleNumber,
                    update.ReadTime,
                    m_CycleStartedAt,
                    m_CycleSampleCount));
                RaiseLogMessage(
                    PlcGraphLogLevel.Info,
                    "CYCLE_STARTED",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "cycle={0} requestDevice={1} requestValue={2} readTime={3:O}",
                        m_CycleNumber,
                        m_Settings.RequestDevice,
                        m_Settings.StartRequestValue,
                        update.ReadTime),
                    null,
                    true);
            }

            if (update.AddSample)
            {
                // ASCII already contains kgf; only legacy/simulated updates use the divisor.
                double loadKgf = update.LoadKgf ?? update.LoadRaw / m_Settings.LoadScale;
                int sampleNumber = ++m_CycleSampleCount;
                RaiseLoadSampleReceived(new PlcLoadSampleEventArgs(
                    m_CycleNumber,
                    sampleNumber,
                    update.LoadRaw,
                    loadKgf,
                    update.ReadTime) { LoadWords = update.LoadWords });
                RaiseLogMessage(
                    PlcGraphLogLevel.Info,
                    "SAMPLE_ACCEPTED",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "cycle={0} sample={1} raw={2} loadKgf={3:0.000000} readTime={4:O} words=[{5}]",
                        m_CycleNumber,
                        sampleNumber,
                        update.LoadRaw,
                        loadKgf,
                        update.ReadTime,
                        FormatLoadWords(update.LoadWords)),
                    null,
                    false);
            }

            if (update.FreezeGraph)
            {
                m_CycleActive = false;
                RaiseGraphFreezeRequested(new PlcGraphCycleEventArgs(
                    m_CycleNumber,
                    update.ReadTime,
                    m_CycleStartedAt,
                    m_CycleSampleCount));
                RaiseLogMessage(
                    PlcGraphLogLevel.Info,
                    "CYCLE_ENDED",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "cycle={0} requestDevice={1} requestValue={2} sampleCount={3} " +
                        "durationMs={4:0} readTime={5:O}",
                        m_CycleNumber,
                        m_Settings.RequestDevice,
                        m_Settings.EndRequestValue,
                        m_CycleSampleCount,
                        (update.ReadTime - m_CycleStartedAt).TotalMilliseconds,
                        update.ReadTime),
                    null,
                    true);
            }
        }

        private static string FormatLoadWords(ushort[] words)
        {
            return words == null ? string.Empty : string.Join(",",
                Array.ConvertAll(words, word => word.ToString(CultureInfo.InvariantCulture)));
        }

        private McpX CreateClient()
        {
            return new McpX(
                m_Settings.IpAddress,
                m_Settings.Port,
                m_Settings.RemotePassword,
                m_Settings.IsAscii,
                m_Settings.IsUdp,
                m_Settings.Frame,
                checked((ushort)m_Settings.TimeoutMilliseconds),
                m_Settings.Processor);
        }

        private static DeviceAddress ParseDeviceAddress(string deviceAddress)
        {
            if (string.IsNullOrWhiteSpace(deviceAddress))
            {
                throw new ArgumentException("PLC 디바이스 주소가 비어 있습니다.");
            }

            string normalized = deviceAddress.Trim().ToUpperInvariant();
            string selectedPrefix = null;

            foreach (string prefixName in Enum.GetNames(typeof(McpXPrefix)))
            {
                if (normalized.StartsWith(prefixName, StringComparison.OrdinalIgnoreCase) &&
                    (selectedPrefix == null || prefixName.Length > selectedPrefix.Length))
                {
                    selectedPrefix = prefixName;
                }
            }

            if (selectedPrefix == null || normalized.Length == selectedPrefix.Length)
            {
                throw new ArgumentException(
                    "PLC 디바이스 주소가 올바르지 않습니다: " + deviceAddress);
            }

            McpXPrefix prefix = (McpXPrefix)Enum.Parse(
                typeof(McpXPrefix),
                selectedPrefix,
                true);

            return new DeviceAddress(
                prefix,
                normalized.Substring(selectedPrefix.Length));
        }

        private static Exception UnwrapException(Exception exception)
        {
            AggregateException aggregate = exception as AggregateException;
            if (aggregate == null)
            {
                return exception;
            }

            AggregateException flattened = aggregate.Flatten();
            return flattened.InnerExceptions.Count == 1
                ? flattened.InnerExceptions[0]
                : flattened;
        }

        private void DisposeClient(McpX client)
        {
            try
            {
                client.Dispose();
            }
            catch (Exception exception)
            {
                // 종료 중 PLC 소켓 오류가 UI 프로세스를 중단시키지 않도록 합니다.
                RaiseLogMessage(
                    PlcGraphLogLevel.Warning,
                    "PLC_CLIENT_DISPOSE_FAILED",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "endpoint={0}:{1}",
                        m_Settings.IpAddress,
                        m_Settings.Port),
                    exception,
                    false);
            }
        }

        private void RaiseConnectionStateChanged(
            PlcGraphConnectionState state,
            Exception error)
        {
            EventHandler<PlcGraphConnectionStateChangedEventArgs> handler = ConnectionStateChanged;
            if (handler != null)
            {
                handler(this, new PlcGraphConnectionStateChangedEventArgs(state, error));
            }
        }

        private void RaiseGraphResetRequested(PlcGraphCycleEventArgs args)
        {
            EventHandler<PlcGraphCycleEventArgs> handler = GraphResetRequested;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void RaiseLoadSampleReceived(PlcLoadSampleEventArgs args)
        {
            EventHandler<PlcLoadSampleEventArgs> handler = LoadSampleReceived;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void RaiseGraphFreezeRequested(PlcGraphCycleEventArgs args)
        {
            EventHandler<PlcGraphCycleEventArgs> handler = GraphFreezeRequested;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void RaiseLogMessage(
            PlcGraphLogLevel level,
            string eventName,
            string message,
            Exception error,
            bool displayInStationLog)
        {
            EventHandler<PlcGraphLogEventArgs> handler = LogMessage;
            if (handler != null)
            {
                handler(
                    this,
                    new PlcGraphLogEventArgs(
                        DateTime.Now,
                        level,
                        eventName,
                        message,
                        error,
                        displayInStationLog));
            }
        }

        private void ThrowIfDisposed()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(nameof(PlcGraphReader));
            }
        }

        public void Dispose()
        {
            CancellationTokenSource cancellation;
            Task worker;
            McpX client;

            lock (m_SyncRoot)
            {
                if (m_Disposed)
                {
                    return;
                }

                RaiseLogMessage(
                    PlcGraphLogLevel.Info,
                    "READER_STOP_REQUESTED",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "cycle={0} cycleActive={1} sampleCount={2}",
                        m_CycleNumber,
                        m_CycleActive,
                        m_CycleSampleCount),
                    null,
                    false);
                m_Disposed = true;
                cancellation = m_Cancellation;
                worker = m_WorkerTask;
                client = m_Client;
                m_Cancellation = null;
                m_WorkerTask = null;
                m_Client = null;
            }

            if (cancellation != null)
            {
                cancellation.Cancel();
            }

            if (client != null)
            {
                DisposeClient(client);
            }

            if (worker != null)
            {
                try
                {
                    bool stopped = worker.Wait(TimeSpan.FromMilliseconds(
                        Math.Max(1000, m_Settings.TimeoutMilliseconds + 500)));
                    if (!stopped)
                    {
                        RaiseLogMessage(
                            PlcGraphLogLevel.Warning,
                            "READER_STOP_TIMEOUT",
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "timeoutMs={0}",
                                Math.Max(1000, m_Settings.TimeoutMilliseconds + 500)),
                            null,
                            false);
                    }
                }
                catch (AggregateException exception)
                {
                    // 종료 과정에서 발생한 통신 예외는 무시합니다.
                    RaiseLogMessage(
                        PlcGraphLogLevel.Warning,
                        "READER_STOP_FAILED",
                        "worker wait failed",
                        UnwrapException(exception),
                        false);
                }
            }

            if (cancellation != null)
            {
                cancellation.Dispose();
            }

            RaiseConnectionStateChanged(PlcGraphConnectionState.Disconnected, null);
            RaiseLogMessage(
                PlcGraphLogLevel.Info,
                "READER_STOPPED",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "connectionAttempts={0} lastCycle={1} lastSampleCount={2}",
                    m_ConnectionAttempt,
                    m_CycleNumber,
                    m_CycleSampleCount),
                null,
                false);
        }
    }
}
