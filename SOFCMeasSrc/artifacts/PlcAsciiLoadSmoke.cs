// Focused, isolated regression harness for the five-WORD PLC ASCII load frame.
// Does not invoke Program.Main, a real PLC, or license registry writes.
// Run with two arguments: a fresh test-data root and the built application's bin directory.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

internal static class PlcAsciiLoadSmoke
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static Assembly App;
    static int Checks, Failures;
    static object LastInvalidLog;
    static readonly ushort[] ImageWords = { 12546, 8235, 11824, 12596, 816 };
    static readonly ushort[] NegativeWords = { 12546, 8237, 11824, 12596, 816 };

    static Type T(string name) { return App.GetType("SOFCMeas." + name, true); }
    static object New(string name, params object[] args)
    { return Activator.CreateInstance(T(name), Flags & ~BindingFlags.Static, null, args, null); }
    static object Call(object target, string name, params object[] args)
    {
        Type type = target as Type ?? target.GetType();
        var method = type.GetMethod(name, Flags);
        if (args.Length < method.GetParameters().Length)
            args = args.Concat(method.GetParameters().Skip(args.Length).Select(p => p.DefaultValue)).ToArray();
        try { return method.Invoke(target is Type ? null : target, args); }
        catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    static object Get(object target, string name)
    {
        var property = target.GetType().GetProperty(name, Flags);
        return property != null ? property.GetValue(target, null) : target.GetType().GetField(name, Flags).GetValue(target);
    }
    static void Set(object target, string name, object value)
    {
        var property = target.GetType().GetProperty(name, Flags);
        if (property != null) property.SetValue(target, value, null);
        else target.GetType().GetField(name, Flags).SetValue(target, value);
    }
    static void Listen(object target, string eventName, Action<object> action)
    {
        var ev = target.GetType().GetEvent(eventName, Flags);
        var args = ev.EventHandlerType.GetMethod("Invoke").GetParameters().Select(p => Expression.Parameter(p.ParameterType)).ToArray();
        var body = Expression.Invoke(Expression.Constant(action), Expression.Convert(args[1], typeof(object)));
        var handler = Expression.Lambda(ev.EventHandlerType, body, args).Compile();
        ev.GetAddMethod(true).Invoke(target, new object[] { handler });
    }
    static void Check(bool ok, string name)
    { Checks++; if (!ok) Failures++; Console.WriteLine((ok ? "PASS " : "FAIL ") + name); }
    static bool Near(double a, double b) { return Math.Abs(a - b) < 0.000000001; }
    static void Pump(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds) { Application.DoEvents(); Thread.Sleep(5); }
    }
    static void Wait(Func<bool> condition, string name)
    {
        var watch = Stopwatch.StartNew();
        while (!condition() && watch.ElapsedMilliseconds < 6000) Pump(10);
        if (!condition()) throw new Exception("Timed out: " + name);
    }
    static ushort[] Words(string ascii)
    {
        if (ascii.Length != 10) throw new ArgumentException("Fixture must contain exactly ten ASCII bytes.");
        return Enumerable.Range(0, 5).Select(i => (ushort)(ascii[i * 2] | ascii[i * 2 + 1] << 8)).ToArray();
    }
    static ushort[] Frame(char sign, string magnitude)
    { return Words("\x02" + "1" + sign + magnitude.PadLeft(6, ' ') + "\x03"); }
    static bool Decode(ushort[] words, out double value)
    {
        var args = new object[] { words, 0D };
        bool valid = (bool)T("PlcLoadDecoder").GetMethod("TryDecode", Flags).Invoke(null, args);
        value = (double)args[1];
        return valid;
    }
    static bool DecodeFrame(ushort[] words, out double value, out string reason)
    {
        var args = new object[] { words, 0D, null };
        bool valid = (bool)T("PlcLoadDecoder").GetMethod("TryDecodeFrame", Flags).Invoke(null, args);
        value = (double)args[1]; reason = (string)args[2];
        return valid;
    }
    static void Reject(ushort[] words, string name)
    {
        double value; string reason;
        Check(!DecodeFrame(words, out value, out reason) && !string.IsNullOrWhiteSpace(reason)
            && !Decode(words, out value), name + " is rejected with an alarm reason");
    }
    static void DecoderTests()
    {
        double value; string reason;
        Check(DecodeFrame(ImageWords, out value, out reason) && Near(value, .410), "image D700..D704 decodes STX 1 + space 0.410 ETX to +0.410 kgf");
        Check(Frame('+', "0.410").SequenceEqual(ImageWords), "fixture confirms low-byte-first ASCII with STX, header, sign and ETX");
        Check(Decode(NegativeWords, out value) && Near(value, -.410), "D701=8237 decodes negative -0.410 kgf");
        Check(Frame('-', "0.410").SequenceEqual(NegativeWords), "negative fixture differs only in the sign byte at D701");
        Check(Decode(Frame('+', "0.000"), out value) && value == 0 && BitConverter.DoubleToInt64Bits(value) >= 0, "positive zero is a valid positive sample");
        Check(Decode(Frame('-', "0.000"), out value) && value == 0 && BitConverter.DoubleToInt64Bits(value) < 0, "negative zero retains its sign bit");
        Check(Decode(Frame('+', "10.410"), out value) && Near(value, 10.410), "six-character magnitude needs no leading padding");
        Check(Decode(Frame('-', "10.410"), out value) && Near(value, -10.410), "six-character negative magnitude uses the separate sign byte");
        Check(Decode(Frame('+', "42"), out value) && Near(value, 42), "integer magnitude with leading ASCII spaces");
        Check(Decode(Frame('+', "999999"), out value) && Near(value, 999999), "full-width integer magnitude");
        Check(Decode(Frame('+', "0.0001"), out value) && Near(value, .0001), "fractional precision is preserved without additional scale division");
        Reject(Words("\0" + "1+ 0.410" + "\x03"), "missing STX");
        Reject(Words("\x03" + "1+ 0.410" + "\x02"), "reversed framing markers");
        Reject(Words("\x02" + "1+ 0.410\0"), "missing ETX");
        Reject(Words("1" + "\x02" + "+ 0.410" + "\x03"), "misplaced STX");
        Reject(Words("\x02" + "1+ 0.41" + "\x03" + "0"), "misplaced ETX and byte after ETX");
        Reject(Words("\x02" + "2+ 0.410" + "\x03"), "unsupported header");
        Reject(Frame(' ', "0.410"), "missing sign");
        Reject(Frame('*', "0.410"), "unsupported sign");
        Reject(Frame('+', "0.x10"), "non-numeric magnitude");
        Reject(Frame('+', "0,410"), "comma decimal separator");
        Reject(Frame('+', "1e003"), "exponent notation");
        Reject(Frame('+', "NaN"), "NaN");
        Reject(Frame('+', ""), "empty magnitude");
        Reject(Frame('+', ".410"), "missing integer digits");
        Reject(Frame('+', "410."), "missing fractional digits");
        Reject(Frame('+', "0..410"), "duplicate decimal point");
        Reject(Frame('+', "0.410 "), "trailing magnitude padding");
        Reject(Frame('+', "0 .410"), "embedded space");
        Reject(Frame('+', "-0.410"), "second minus sign in magnitude");
        Reject(Frame('-', "+0.410"), "second plus sign in magnitude");
        Reject(Frame('+', "\t0.410"), "tab instead of leading ASCII space");
        Reject(Frame('+', "\0" + "0.410"), "NUL in magnitude");
        Reject(Frame('+', "0" + "\x03" + ".410"), "embedded ETX");
        Reject(Frame('+', "0" + "\x02" + ".410"), "embedded STX");
        Reject(Frame('+', "\u00a0" + "0.410"), "non-ASCII padding");
        Reject(ImageWords.Select(word => (ushort)((word >> 8) | (word << 8))).ToArray(), "byte-swapped frame");
        Reject(null, "missing register block");
        Reject(new ushort[3], "obsolete three-WORD payload");
        Reject(new ushort[4], "short register block");
        Reject(new ushort[6], "long register block");
        Check((int)Call(T("PlcLoadDecoder"), "ToLegacyRawValue", 3D, 1000000000D) == int.MaxValue, "positive legacy raw overflow saturates at Int32.MaxValue");
        Check((int)Call(T("PlcLoadDecoder"), "ToLegacyRawValue", -3D, 1000000000D) == int.MinValue, "negative legacy raw overflow saturates at Int32.MinValue");
        var previousCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Check(Decode(ImageWords, out value) && Near(value, .410) && Decode(NegativeWords, out value) && Near(value, -.410), "signed decoding is invariant to operating-system decimal culture");
        }
        finally { Thread.CurrentThread.CurrentCulture = previousCulture; }
    }
    static object Snapshot(ushort request, bool invalid, bool negative = false)
    {
        object snapshot = New("PlcGraphSnapshot");
        Set(snapshot, "RequestValue", request);
        Set(snapshot, "ReadTime", DateTime.Now);
        Set(snapshot, "LoadWords", negative ? NegativeWords : ImageWords);
        Set(snapshot, "LoadKgf", negative ? -.410 : .410);
        Set(snapshot, "LoadRaw", negative ? -410 : 410);
        Set(snapshot, "InvalidLoadValue", invalid);
        return snapshot;
    }
    static void ProtocolTests()
    {
        object protocol = New("PlcGraphProtocol");
        object update = Call(protocol, "Process", Snapshot(1, true), (ushort)1, (ushort)0, true);
        Check((bool)Get(update, "ResetGraph") && !(bool)Get(update, "AddSample") && (bool)Get(update, "IsCollecting"), "invalid first payload still handles START and skips the sample");
        update = Call(protocol, "Process", Snapshot(1, false), (ushort)1, (ushort)0, true);
        Check(!(bool)Get(update, "ResetGraph") && (bool)Get(update, "AddSample") && Near((double)Get(update, "LoadKgf"), .410)
            && ((ushort[])Get(update, "LoadWords")).SequenceEqual(ImageWords), "valid following payload recovers within the same cycle with original words");
        update = Call(protocol, "Process", Snapshot(1, true), (ushort)1, (ushort)0, true);
        Check(!(bool)Get(update, "AddSample") && (bool)Get(update, "IsCollecting"), "invalid middle payload is discarded without repeating the preceding sample");
        update = Call(protocol, "Process", Snapshot(1, false, true), (ushort)1, (ushort)0, true);
        Check((bool)Get(update, "AddSample") && Near((double)Get(update, "LoadKgf"), -.410)
            && ((ushort[])Get(update, "LoadWords")).SequenceEqual(NegativeWords), "protocol preserves sign when the next valid frame is negative");
        update = Call(protocol, "Process", Snapshot(0, true), (ushort)1, (ushort)0, true);
        Check((bool)Get(update, "FreezeGraph") && !(bool)Get(update, "AddSample") && !(bool)Get(update, "IsCollecting"), "invalid load flag does not suppress END");
    }
    static object Settings(int station, int port, string address, double scale)
    {
        object settings = Call(T("PlcGraphSettings"), "Load", station, "127.0.0.1");
        Set(settings, "IpAddress", "127.0.0.1"); Set(settings, "Port", port);
        Set(settings, "AutoConnect", false); Set(settings, "IsAscii", false); Set(settings, "IsUdp", false);
        Set(settings, "PollIntervalMilliseconds", 100); Set(settings, "TimeoutMilliseconds", 500);
        Set(settings, "PostStopReadMilliseconds", 2000);
        Set(settings, "ReconnectDelayMilliseconds", 100); Set(settings, "LoadScale", scale);
        Set(settings, "RequestDevice", "D810"); Set(settings, "LoadRawDevice", address);
        Set(settings, "StartRequestValue", (ushort)1); Set(settings, "EndRequestValue", (ushort)0);
        return settings;
    }
    static void NetworkTests(int station, int address, double scale = 2000D, double expectedKgf = .410, int expectedRaw = 820, ushort[] validPayload = null)
    {
        validPayload = validPayload ?? ImageWords;
        using (var server = new MockPlc(address))
        using (var reader = (IDisposable)New("PlcGraphReader", Settings(station, server.Port, "D" + address, scale)))
        {
            int samples = 0, invalidLogs = 0, errors = 0, resets = 0, freezes = 0,
                valueErrors = 0, alarmErrors = 0, requestLogs = 0, heartbeatLogs = 0;
            ushort[] invalidPayload = Frame('+', "0.x10");
            double ignored; string invalidReason;
            DecodeFrame(invalidPayload, out ignored, out invalidReason);
            Listen(reader, "GraphResetRequested", e => Interlocked.Increment(ref resets));
            Listen(reader, "GraphFreezeRequested", e => Interlocked.Increment(ref freezes));
            Listen(reader, "LoadSampleReceived", e =>
            {
                if (!Near((double)Get(e, "LoadKgf"), expectedKgf) || (int)Get(e, "RawValue") != expectedRaw
                    || !((ushort[])Get(e, "LoadWords")).SequenceEqual(validPayload))
                    Interlocked.Increment(ref valueErrors);
                Interlocked.Increment(ref samples);
            });
            Listen(reader, "LogMessage", e =>
            {
                if ((string)Get(e, "EventName") == "PLC_REQUEST_CHANGED")
                    Interlocked.Increment(ref requestLogs);
                if ((string)Get(e, "EventName") == "PLC_HEARTBEAT")
                    Interlocked.Increment(ref heartbeatLogs);
                if ((string)Get(e, "EventName") == "PLC_LOAD_DATA_INVALID")
                {
                    LastInvalidLog = e;
                    string message = (string)Get(e, "Message");
                    if (Get(e, "Level").ToString() != "Warning" || !(bool)Get(e, "DisplayInStationLog")
                        || !message.Contains("D" + address) || !message.Contains(invalidReason)
                        || invalidPayload.Any(word => !message.Contains(word.ToString(CultureInfo.InvariantCulture))))
                        Interlocked.Increment(ref alarmErrors);
                    Interlocked.Increment(ref invalidLogs);
                }
                if (Get(e, "Level").ToString() == "Error")
                {
                    Interlocked.Increment(ref errors);
                    Console.WriteLine("READER_ERROR " + Get(e, "Message"));
                }
            });
            Call(reader, "SetCollectionEnabled", true);
            Call(reader, "Start", true);
            Wait(() => server.RequestReads >= 2, "request reads before START");
            Check(server.LoadReads == 0, "station " + station + " WAIT_START reads D810 without the load block");

            server.Payload = invalidPayload;
            server.Request = 1;
            Wait(() => invalidLogs > 0 && resets == 1, "invalid payload START");
            Pump(250);
            Check(samples == 0 && Get(reader, "AcquisitionState").ToString() == "Collecting", "station " + station + " malformed ASCII logs and skips samples while keeping START state");
            Check(alarmErrors == 0 && invalidLogs > 0, "station " + station + " invalid frame raises a visible warning with reason, address and all five raw WORDs");

            server.Payload = validPayload;
            Wait(() => samples >= 3, "valid payload recovery");
            Check(valueErrors == 0 && resets == 1, "station " + station + " recovers " + expectedKgf.ToString("0.000", CultureInfo.InvariantCulture)
                + " kgf and raw=" + expectedRaw + " at scale=" + scale.ToString(CultureInfo.InvariantCulture) + ", preserving all five words");
            Check(server.AcceptedConnections == 1 && errors == 0, "station " + station + " malformed ASCII does not restart the connection");

            int firstInvalidLogs = invalidLogs;
            server.Payload = invalidPayload;
            Wait(() => invalidLogs > firstInvalidLogs, "later malformed payload");
            int sampleCount = samples;
            Pump(250);
            Check(samples == sampleCount, "station " + station + " malformed payload during collection does not insert zero or repeat the previous value");
            server.Payload = validPayload;
            int loadReadsAtEnd = server.LoadReads;
            int samplesAtEnd = samples;
            Stopwatch postStopWatch = Stopwatch.StartNew();
            server.Request = 0;
            Wait(() => freezes == 1, "END following invalid payload");
            Check(postStopWatch.ElapsedMilliseconds >= 1800 &&
                  postStopWatch.ElapsedMilliseconds < 4000 &&
                  server.LoadReads >= loadReadsAtEnd + 10 &&
                  samples >= samplesAtEnd + 10,
                "station " + station + " continues D" + address + "..D" + (address + 4) +
                " load display samples for about 2 seconds after END");
            int loadReads = server.LoadReads;
            Pump(250);
            Check(server.LoadReads == loadReads && Get(reader, "AcquisitionState").ToString() == "Evaluating", "station " + station + " END completes the cycle and stops load reads after invalid payload");
            Call(reader, "CompleteEvaluation");
            Pump(250);
            Check(requestLogs == 3 && heartbeatLogs == 0,
                "station " + station + " logs D810 only for initial, START and END values without periodic heartbeat duplicates");
            Check(server.BadFrames == 0 && server.LoadReads > 0, "station " + station + " reads exactly D" + address + "..D" + (address + 4) + " as a single five-WORD batch");
            Check(server.Failure == null, "station " + station + " loopback fixture completed without protocol errors");
        }
    }
    static void UiTests(string root)
    {
        using (var view = (Control)New("StationView"))
        {
            Call(view, "ConfigureGraph", 1);
            Call(view, "SetLoadDeviceName", "D700");
            Call(view, "AddPlcLoadSample", .410, 820, ImageWords);
            string label = (string)Get(Get(view, "lblCurrentLoad"), "Text");
            Check(label.Contains("D700[12546][8235][11824][12596][816]") && label.Contains("+0.410 kgf"), "load display shows original D700..D704 WORDs and explicit positive sign");
            var chart = (Chart)Get(view, "chartLoad");
            Check(chart.Series[0].Points.Count == 1 && Near(chart.Series[0].Points[0].YValues[0], .410), "live graph receives the decoded load");
            view.CreateControl();
            using (var bitmap = new System.Drawing.Bitmap(view.Width, view.Height))
            {
                view.DrawToBitmap(bitmap, view.ClientRectangle);
                bitmap.Save(Path.Combine(root, "StationPositiveLoadPreview.png"));
            }
            Call(view, "AddPlcLoadSample", -.410, -820, NegativeWords);
            label = (string)Get(Get(view, "lblCurrentLoad"), "Text");
            Check(label.Contains("D700[12546][8237][11824][12596][816]") && label.Contains("-0.410 kgf"), "load display preserves negative sign and corresponding raw words");
            Check(chart.Series[0].Points.Count == 2 && Near(chart.Series[0].Points[1].YValues[0], -.410), "live graph preserves negative sample data");
            Check(chart.ChartAreas[0].AxisY.Minimum <= -.410, "automatic Y range includes negative samples even with a configured zero minimum");
            view.CreateControl();
            using (var bitmap = new System.Drawing.Bitmap(view.Width, view.Height))
            {
                view.DrawToBitmap(bitmap, view.ClientRectangle);
                bitmap.Save(Path.Combine(root, "StationLoadPreview.png"));
            }
            Call(view, "ResetLoadGraph");
            double negativeZero;
            Decode(Frame('-', "0.000"), out negativeZero);
            Call(view, "AddPlcLoadSample", negativeZero, 0, Frame('-', "0.000"));
            label = (string)Get(Get(view, "lblCurrentLoad"), "Text");
            Check(label.Contains("-0.000 kgf"), "load display preserves explicit negative zero sign");
            Call(view, "ResetLoadGraph");
            Call(view, "AddPlcLoadSample", -.410, -410, NegativeWords);
            Call(view, "AddPlcLoadSample", -.200, -200, Frame('-', "0.200"));
            object result = Call(view, "CompleteInspection");
            Check((bool)Get(result, "IsValid") && Near((double)Get(result, "PeakLoadKgf"), -.200), "negative-only inspection peak remains negative rather than being clamped to zero");
            Call(view, "ResetLoadGraph");
            Call(view, "AddLoadSample", .500, (int?)500);
            Check(chart.Series[0].Points.Count == 1 && Near(chart.Series[0].Points[0].YValues[0], .500), "existing synthetic sample method remains usable");
        }
        using (var settings = (Form)New("SettingForm"))
        {
            settings.ShowInTaskbar = false;
            settings.StartPosition = FormStartPosition.Manual;
            settings.Location = new System.Drawing.Point(-32000, -32000);
            settings.Show(); Pump(50);
            foreach (var grid in ((List<DataGridView>)Get(settings, "grids")).Skip(1))
            {
                var info = grid.Parent.Controls.OfType<Label>().Single();
                Check(info.Text.Contains("D700 ~ D704") && info.Text.Contains("0.410") && info.Text.Contains("STX") && info.Text.Contains("ETX") && grid.Bottom <= info.Top,
                    grid.Name + " shows the five-WORD range and framed signed example without covering the settings grid");
            }
            using (var bitmap = new System.Drawing.Bitmap(settings.Width, settings.Height))
            {
                settings.DrawToBitmap(bitmap, settings.ClientRectangle);
                bitmap.Save(Path.Combine(root, "PlcSettingsPreview.png"));
            }
        }
    }
    static void AlarmPersistenceTests(string root)
    {
        string logRoot = Path.Combine(root, "PersistedAlarmLog");
        object log = New("ApplicationLogService", logRoot, 1024L * 1024L);
        using (var form = (Form)New("SOFCMeas", null, log, null))
        {
            try
            {
                // Create a handle without showing the main form or firing startup PLC/license work.
                IntPtr handle = form.Handle;
                object reader = Get(form, "m_Station1Reader"), view = Get(form, "stationView1");
                string originalState = (string)Get(view, "StateText");
                Call(reader, "RaiseLogMessage", Get(LastInvalidLog, "Level"), Get(LastInvalidLog, "EventName"),
                    Get(LastInvalidLog, "Message"), null, true);
                string[] lines = Directory.GetFiles(logRoot, "*.log", SearchOption.AllDirectories)
                    .SelectMany(File.ReadAllLines).Where(line => line.Contains("event=PLC_LOAD_DATA_INVALID")).ToArray();
                Check(lines.Length == 1 && lines[0].Contains(" WARN STATION-1 ")
                    && !lines[0].Contains("[SESSION=") && !lines[0].Contains("[SEQ=")
                    && !lines[0].Contains("[PID=") && !lines[0].Contains("[TID=")
                    && lines[0].Contains((string)Get(LastInvalidLog, "Message")) && Get(log, "LastError") == null,
                    "real reader alarm uses the concise format and keeps station WARN diagnostic data");
                string uiLog = (string)Get(Get(view, "txtStationLog"), "Text");
                Check(uiLog.Contains("PLC_LOAD_DATA_INVALID") && uiLog.Contains("invalid_magnitude")
                    && uiLog.Contains("D700") && uiLog.Contains("12546"),
                    "real reader alarm event is appended to the station UI log");
                Check((string)Get(view, "StateText") == originalState,
                    "displaying and saving an invalid-frame alarm does not change station running state");
            }
            finally
            {
                ((IDisposable)Get(form, "m_Station1Reader")).Dispose();
                ((IDisposable)Get(form, "m_Station2Reader")).Dispose();
                ((IDisposable)Get(form, "m_LogCleanupTimer")).Dispose();
            }
        }
    }
    sealed class MockPlc : IDisposable
    {
        readonly TcpListener Listener;
        readonly List<TcpClient> Clients = new List<TcpClient>();
        readonly int LoadAddress;
        public volatile int Request, RequestReads, LoadReads, AcceptedConnections, BadFrames;
        public volatile ushort[] Payload = ImageWords;
        public volatile string Failure;
        public int Port { get; private set; }
        public MockPlc(int loadAddress)
        {
            LoadAddress = loadAddress;
            Listener = new TcpListener(IPAddress.Loopback, 0); Listener.Start();
            Port = ((IPEndPoint)Listener.LocalEndpoint).Port;
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var client = await Listener.AcceptTcpClientAsync();
                        lock (Clients) Clients.Add(client);
                        Interlocked.Increment(ref AcceptedConnections); Handle(client);
                    }
                }
                catch (ObjectDisposedException) { }
                catch (SocketException) { }
            });
        }
        static async Task<byte[]> Read(NetworkStream stream, int length)
        {
            byte[] bytes = new byte[length];
            for (int offset = 0; offset < length;)
            {
                int count = await stream.ReadAsync(bytes, offset, length - offset);
                if (count == 0) throw new IOException("Client closed connection.");
                offset += count;
            }
            return bytes;
        }
        async void Handle(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                while (true)
                {
                    byte[] header = await Read(stream, 9);
                    byte[] body = await Read(stream, header[7] + 256 * header[8]);
                    int command = body[2] + 256 * body[3];
                    var values = new List<byte>();
                    if (command == 0x401)
                    {
                        int address = body[6] + 256 * body[7] + 65536 * body[8];
                        int count = body[10] + 256 * body[11];
                        bool request = address == 810;
                        if (body[9] != 0xa8 || (request ? count != 1 : address != LoadAddress || count != 5))
                            Interlocked.Increment(ref BadFrames);
                        if (request) Interlocked.Increment(ref RequestReads); else Interlocked.Increment(ref LoadReads);
                        ushort[] payload = Payload;
                        for (int i = 0; i < count; i++)
                        {
                            int value = request ? Request : i < payload.Length ? payload[i] : 0;
                            values.Add((byte)value); values.Add((byte)(value >> 8));
                        }
                    }
                    else if (command == 0x403)
                    {
                        int words = body[6], doubleWords = body[7];
                        if (words != 1 || doubleWords != 0) Interlocked.Increment(ref BadFrames);
                        for (int i = 0, offset = 8; i < words + doubleWords; i++, offset += 4)
                        {
                            int address = body[offset] + 256 * body[offset + 1] + 65536 * body[offset + 2];
                            if (address != 810 || body[offset + 3] != 0xa8) Interlocked.Increment(ref BadFrames);
                            Interlocked.Increment(ref RequestReads);
                            values.Add((byte)Request); values.Add((byte)(Request >> 8));
                            if (i >= words) { values.Add(0); values.Add(0); }
                        }
                    }
                    else throw new Exception("Unsupported MC3E command " + command.ToString("X"));
                    byte[] response = new byte[11 + values.Count];
                    response[0] = 0xd0; Array.Copy(header, 2, response, 2, 5);
                    response[7] = (byte)(values.Count + 2); response[8] = (byte)((values.Count + 2) >> 8);
                    values.CopyTo(response, 11); await stream.WriteAsync(response, 0, response.Length);
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { Failure = ex.ToString(); }
        }
        public void Dispose()
        {
            Listener.Stop();
            lock (Clients) foreach (var client in Clients) client.Close();
        }
    }
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            if (args.Length != 2) throw new ArgumentException("Usage: PlcAsciiLoadSmoke.exe <fresh-test-root> <application-bin>");
            string root = Path.GetFullPath(args[0]), bin = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(root); Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", root);
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                string path = Path.Combine(bin, new AssemblyName(e.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
            App = Assembly.LoadFrom(Path.Combine(bin, "SOFCMeas.exe"));
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            var config = (Dictionary<string, string>)Call(T("SettingsCatalog"), "Load");
            Check(config["Station1.Plc.LoadRawDevice"] == "D700" && config["Station2.Plc.LoadRawDevice"] == "D700", "both station defaults use D700");
            config["Station1.Plc.AutoConnect"] = config["Station2.Plc.AutoConnect"] = "false";
            config["Station1.Graph.YMin"] = config["Station2.Graph.YMin"] = "0";
            config["Runtime.Simulation"] = "0";
            Call(T("ApplicationConfiguration"), "SaveAppSettings", config, true);
            DecoderTests(); ProtocolTests(); NetworkTests(1, 700); NetworkTests(2, 730, 2000D, -.410, -820, NegativeWords);
            NetworkTests(1, 700, 1000000000D, 3D, int.MaxValue, Frame('+', "3.000"));
            UiTests(root); AlarmPersistenceTests(root);
            Console.WriteLine("ASCII_LOAD_SMOKE_DONE checks=" + Checks + " failures=" + Failures);
            return Failures == 0 ? 0 : 2;
        }
        catch (Exception ex) { Console.WriteLine("HARNESS_ERROR " + ex); return 1; }
    }
}
