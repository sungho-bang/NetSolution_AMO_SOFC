// Run "default" to inspect deployment paths without writing or constructing UI.
// Run "isolated" to exercise storage and cleanup only below a unique workspace fixture.
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SOFCMeas;

internal static class LogPathSmoke
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
    private static readonly Assembly App = typeof(SettingForm).Assembly;
    private static int checks;

    private static Type Type(string name) { return App.GetType("SOFCMeas." + name, true); }
    private static object New(string name, params object[] args)
    { return Activator.CreateInstance(Type(name), Instance, null, args, CultureInfo.InvariantCulture); }
    private static object Get(object value, string name)
    { return value.GetType().GetProperty(name, Instance).GetValue(value, null); }
    private static void Set(object value, string name, object item)
    { value.GetType().GetProperty(name, Instance).SetValue(value, item, null); }
    private static object Field(object value, string name)
    { return value.GetType().GetField(name, Instance).GetValue(value); }
    private static object Call(object value, string name, params object[] args)
    { return value.GetType().GetMethod(name, Instance).Invoke(value, args); }
    private static string PathValue(string name)
    { return (string)Type("ApplicationPaths").GetProperty(name, Static).GetValue(null, null); }
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new InvalidOperationException(message); }
    private static Array One(object item)
    { var result = Array.CreateInstance(item.GetType(), 1); result.SetValue(item, 0); return result; }
    private static string Day(string root, DateTime date)
    { return Path.Combine(root, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd")); }
    private static string Marker(string root, DateTime date, string name)
    { string folder = Day(root, date); Directory.CreateDirectory(folder); string file = Path.Combine(folder, name); File.WriteAllText(file, name); return file; }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string mode = args.Length == 0 ? "default" : args[0];
            if (mode == "default")
            {
                Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", null);
                Environment.SetEnvironmentVariable("SOFCMEAS_LOG_ROOT", null);
                Check(PathValue("ApplicationRootDirectory") == @"C:\SOFCMeas", "Application root changed unexpectedly");
                Check(PathValue("DataRootDirectory") == @"C:\SOFCMeas\Data", "Data root changed unexpectedly");
                Check(PathValue("ConfigurationFilePath") == @"C:\SOFCMeas\conf\SOFCMeas.config", "Configuration root changed unexpectedly");
                string expectedLogRoot = args.Length > 1 ? args[1] : @"C:\CSEng\TrimForm\LOG";
                Check(PathValue("LogRootDirectory") == expectedLogRoot, "Configured log root must be " + expectedLogRoot);
                Console.WriteLine("PASS: " + checks + " read-only default path checks. LogRoot=" + PathValue("LogRootDirectory"));
                Console.WriteLine("No directories, logger, UI, or cleanup were initialized.");
                return 0;
            }
            if (mode != "isolated") throw new ArgumentException("Expected default or isolated mode");
            VerifyIsolated();
            Console.WriteLine("PASS: " + checks + " isolated storage/UI/retention checks.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void VerifyIsolated()
    {
        string workspace = Path.GetFullPath(@"D:\Works_Updates\NetSolution\src\SOFCMeas") + Path.DirectorySeparatorChar;
        string fixture = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "log-path-smoke", Guid.NewGuid().ToString("N")));
        Check(fixture.StartsWith(workspace, StringComparison.OrdinalIgnoreCase), "Fixture must stay inside workspace");
        string appRoot = Path.Combine(fixture, "app");
        string logRoot = Path.Combine(fixture, "external-logs");
        string dataRoot = Path.Combine(appRoot, "Data");
        string legacyRoot = Path.Combine(appRoot, "log");
        Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", appRoot);
        Environment.SetEnvironmentVariable("SOFCMEAS_LOG_ROOT", logRoot + Path.DirectorySeparatorChar);
        Directory.CreateDirectory(Path.Combine(appRoot, "conf"));
        File.WriteAllText(Path.Combine(appRoot, "conf", "SOFCMeas.config"),
            "<configuration><appSettings><add key=\"Plc.AutoConnect\" value=\"false\" />" +
            "<add key=\"Station1.Plc.AutoConnect\" value=\"false\" />" +
            "<add key=\"Station2.Plc.AutoConnect\" value=\"false\" />" +
            "<add key=\"Log.AutoDelete\" value=\"false\" /></appSettings></configuration>");
        Check(PathValue("ApplicationRootDirectory") == appRoot, "Application environment root");
        Check(PathValue("DataRootDirectory") == dataRoot, "Data remains below application root");
        Check(PathValue("LogRootDirectory") == logRoot, "Explicit log root overrides shared root and normalizes trailing separator");
        Check(PathValue("ConfigurationFilePath") == Path.Combine(appRoot, "conf", "SOFCMeas.config"), "Configuration remains below application root");
        Type("ApplicationPaths").GetMethod("EnsureStorageDirectories", Static).Invoke(null, null);
        Check(Directory.Exists(logRoot) && Directory.Exists(dataRoot), "Storage initialization creates separate roots");

        var logger = New("ApplicationLogService", 512L);
        Check((string)Get(logger, "RootDirectory") == logRoot, "Default logger resolves external root");
        for (int i = 0; i < 6; i++) Call(logger, "Info", "LOG-PATH-SMOKE", "rollover-" + i + " " + new string('x', 140));
        string[] logFiles = Directory.GetFiles(logRoot, "SOFCMeas_*.log", SearchOption.AllDirectories);
        Check(logFiles.Length > 1, "Application log rollover creates multiple files");
        Check(logFiles.All(file => new FileInfo(file).Length <= 512), "Rolled files respect configured maximum size");
        string logText = string.Join("\n", logFiles.Select(File.ReadAllText));
        for (int i = 0; i < 6; i++) Check(logText.Contains("rollover-" + i + " "), "Rollover retains message " + i);
        Check(Get(logger, "LastError") == null, "External logger reports no storage error");
        Check(!Directory.Exists(legacyRoot), "Logger does not create old app/log root");

        DateTime now = DateTime.Now;
        var record = New("InspectionLogRecord");
        Set(record, "Time", now); Set(record, "StartedAt", now.AddSeconds(-1));
        Set(record, "Station", "#1"); Set(record, "LotNumber", "LOG_PATH_TEST");
        Set(record, "FileName", "MODEL_TEST"); Set(record, "Result", "GOOD");
        Set(record, "SampleCount", 1); Set(record, "PeakLoadKgf", -0.410D);
        var sample = New("InspectionSample");
        Set(sample, "Number", 1); Set(sample, "ReadTime", now);
        Set(sample, "RawValue", -410); Set(sample, "LoadKgf", -0.410D);
        var inspection = New("BufferedInspectionData");
        Set(inspection, "Record", record); Set(inspection, "Samples", One(sample));
        var lot = New("LotStorageData");
        Set(lot, "CreatedAt", now.AddSeconds(-2)); Set(lot, "CompletedAt", now);
        Set(lot, "Station", "#1"); Set(lot, "LotNumber", "LOG_PATH_TEST");
        Set(lot, "FileName", "MODEL_TEST"); Set(lot, "Inspections", One(inspection));
        var storage = New("InspectionStorageService");
        Check((string)Field(storage, "m_LogRootDirectory") == logRoot, "Default inspection storage resolves external root");
        string saved = (string)Call(storage, "SaveLot", lot);
        Check(saved.StartsWith(Path.Combine(dataRoot, "PLC1") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(saved), "LOT CSV stays under Data/PLC1");
        Check(File.ReadAllText(saved).Contains("-0.410"), "Signed measured data survives LOT save");
        string index = Path.Combine(Day(logRoot, now), "Inspection_" + now.ToString("yyyyMMdd") + ".csv");
        Check(File.Exists(index), "Inspection index writes to external dated log folder");
        Check(!File.Exists(saved + ".index.pending"), "Inspection index recovery journal clears after successful write");
        var records = (ICollection)Call(storage, "GetInspectionLogs", now.Date, now.Date, "#1");
        Check(records.Count == 1, "Inspection query finds new external-root index");
        var loadedRecord = records.Cast<object>().Single();
        Check((double)Get(loadedRecord, "PeakLoadKgf") == -0.410D, "Inspection query preserves signed value");
        string exported = Path.Combine(logRoot, "export.csv");
        Call(storage, "ExportInspectionLogs", exported, records);
        Check(File.Exists(exported) && File.ReadAllText(exported).Contains("LOG_PATH_TEST"), "Inspection export uses requested destination");
        Check(!Directory.Exists(legacyRoot), "Inspection save/query does not create old app/log root");

        string expired = Marker(logRoot, now.Date.AddDays(-30), "expired-smoke.log");
        string retained = Marker(logRoot, now.Date.AddDays(-29), "retained-smoke.log");
        string dataSentinel = Marker(dataRoot, now.Date.AddDays(-30), "data-preserved.keep");
        string legacySentinel = Marker(legacyRoot, now.Date.AddDays(-30), "legacy-preserved.keep");
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using (var form = (Form)New("SOFCMeas"))
        {
            form.Shown -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), form,
                form.GetType().GetMethod("SOFCMeas_Shown", Instance));
            foreach (string name in new[] { "m_LogCleanupTimer", "m_IndexRecoveryTimer" })
                ((Timer)Field(form, name)).Stop();
            Check(!form.Visible, "Main form remains unshown; startup event never runs");
            Check(((Control)Field(form, "lblLogStoragePath")).Text == "저장 경로: " + logRoot + "\\yyyy\\MM\\dd", "UI policy label shows actual external root");
            Check((string)Get(Field(form, "m_ApplicationLogService"), "RootDirectory") == logRoot, "UI logger shares external root");
            Check((string)Field(Field(form, "m_InspectionStorageService"), "m_LogRootDirectory") == logRoot, "UI inspection query shares external root");
            var cleanup = Field(form, "m_LogCleanupService");
            Check((string)Field(cleanup, "m_RootDirectory") == logRoot, "UI cleanup uses exact owned fixture log root");
            Check((string)Field(cleanup, "m_ProtectedDataDirectory") == dataRoot, "UI cleanup protects Data root");
            Check(logRoot.StartsWith(fixture + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Cleanup target stays within unique owned fixture");
            var result = Call(cleanup, "DeleteExpiredDateDirectories", 30, now.Date);
            Check((int)Get(result, "DeletedDayDirectoryCount") == 1, "Retention removes only expired test day");
            Check((int)Get(result, "FailedDayDirectoryCount") == 0, "Retention completes without errors");
            Check(!File.Exists(expired), "Expired external test log removed");
            Check(File.Exists(retained), "Retention boundary day preserved");
            Check(File.Exists(index) && File.Exists(exported), "Current index and root-level export preserved");
            Check(File.Exists(saved) && File.Exists(dataSentinel), "LOT data and old Data sentinel preserved");
            Check(File.Exists(legacySentinel), "Old app/log sentinel remains untouched");
            foreach (string name in new[] { "m_Station1Reader", "m_Station2Reader" })
                ((IDisposable)Field(form, name)).Dispose();
        }
        Console.WriteLine("FIXTURE " + fixture);
    }
}
