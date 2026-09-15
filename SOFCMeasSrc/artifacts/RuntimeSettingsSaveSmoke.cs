using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml;
using SOFCMeas;
using MainForm = global::SOFCMeas.SOFCMeas;

internal static class RuntimeSettingsSaveSmoke
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;

    private static object Field(object value, string name)
    { return value.GetType().GetField(name, Flags).GetValue(value); }

    private static object Call(object value, string name, params object[] args)
    { return value.GetType().GetMethod(name, Flags).Invoke(value, args); }

    private static object Property(object value, string name)
    { return value.GetType().GetProperty(name, Flags).GetValue(value, null); }

    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new InvalidOperationException(message); }

    private static void SetGridValue(List<DataGridView> grids, string key, string value)
    {
        foreach (DataGridView grid in grids)
            foreach (DataGridViewRow row in grid.Rows)
                if ((string)row.Tag == key)
                {
                    row.Cells[1].Value = value;
                    return;
                }
        throw new InvalidOperationException("setting row not found: " + key);
    }

    private static string ReadSaved(string path, string key)
    {
        var xml = new XmlDocument();
        xml.Load(path);
        return xml.SelectSingleNode(
            "/configuration/appSettings/add[@key='" + key + "']")
            .Attributes["value"].Value;
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string root = Path.GetFullPath(args[0]);
            Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", root);
            Directory.CreateDirectory(Path.Combine(root, "conf"));
            string configPath = Path.Combine(root, "conf", "SOFCMeas.config");
            File.WriteAllText(configPath,
                "<configuration><appSettings>" +
                "<add key=\"Runtime.Simulation\" value=\"0\" />" +
                "<add key=\"Station1.Plc.AutoConnect\" value=\"false\" />" +
                "<add key=\"Station2.Plc.AutoConnect\" value=\"false\" />" +
                "<add key=\"Station1.Plc.PollIntervalMs\" value=\"1000\" />" +
                "<add key=\"Station2.Plc.PollIntervalMs\" value=\"1000\" />" +
                "<add key=\"Log.AutoDelete\" value=\"false\" />" +
                "</appSettings></configuration>");

            Application.EnableVisualStyles();
            using (var form = new MainForm())
            {
                ((Timer)Field(form, "m_LogCleanupTimer")).Stop();
                ((Timer)Field(form, "m_IndexRecoveryTimer")).Stop();
                var station1 = (StationView)Field(form, "stationView1");
                var settingsPage = (SettingForm)Field(form, "m_SettingPage");
                settingsPage.GetType().GetField("CanSave", Flags)
                    .SetValue(settingsPage, new Func<bool>(() => true));

                Call(station1, "ResetLoadGraph");
                Call(station1, "AddLoadSample", 0.405D, (int?)405);
                var chart = (Chart)Field(station1, "chartLoad");
                Check(chart.Series[0].Points.Count == 1, "precondition graph sample");

                var grids = (List<DataGridView>)Field(settingsPage, "grids");
                SetGridValue(grids, "Station1.Plc.PollIntervalMs", "500");
                SetGridValue(grids, "Station2.Plc.PollIntervalMs", "750");
                SetGridValue(grids, "Station1.Plc.PostStopReadMs", "2000");
                SetGridValue(grids, "Station2.Plc.PostStopReadMs", "2000");
                SetGridValue(grids, "Station1.Plc.LoadRawDevice", "D710");
                SetGridValue(grids, "Station1.Plc.MaxGraphPoints", "2");
                SetGridValue(grids, "Station1.Lot.SaveCount", "321");
                SetGridValue(grids, "Station1.Inspection.ThresholdKgf", "0.41");
                SetGridValue(grids, "Station1.Graph.XAuto", "false");
                SetGridValue(grids, "Station1.Graph.XMax", "25");
                SetGridValue(grids, "Station1.Graph.XInterval", "2.5");
                SetGridValue(grids, "Station1.Graph.YAuto", "false");
                SetGridValue(grids, "Station1.Graph.YMin", "-1");
                SetGridValue(grids, "Station1.Graph.YMax", "2");
                SetGridValue(grids, "Station1.Graph.YInterval", "0.25");

                ((Task)Call(settingsPage, "SaveValuesAndApplyAsync"))
                    .GetAwaiter().GetResult();

                object reader1 = Field(form, "m_Station1Reader");
                object reader2 = Field(form, "m_Station2Reader");
                object reader1Settings = Property(reader1, "Settings");
                object reader2Settings = Property(reader2, "Settings");
                Check((int)Property(reader1Settings, "PollIntervalMilliseconds") == 500,
                    "station 1 runtime poll interval is 500ms");
                Check((int)Property(reader2Settings, "PollIntervalMilliseconds") == 750,
                    "station 2 runtime poll interval is 750ms");
                Check((int)Property(reader1Settings, "PostStopReadMilliseconds") == 2000 &&
                    (int)Property(reader2Settings, "PostStopReadMilliseconds") == 2000,
                    "both stations apply the 2-second post-stop load window");
                Check((string)Property(reader1Settings, "LoadRawDevice") == "D710",
                    "runtime load device changed");
                Check(station1.MaximumGraphPoints == 2, "maximum graph points applied");
                Check((int)Field(Field(form, "m_Station1Lot"), "MaximumInspectionCount") == 321,
                    "LOT capacity applied");
                Check(station1.SpecText == "0.41", "SPEC UI applied");
                Check(chart.Series[0].Points.Count == 1, "current graph preserved on apply");
                Check(Math.Abs(chart.ChartAreas[0].AxisX.Maximum - 25D) < 0.000001D,
                    "manual X maximum applied");
                Check(Math.Abs(chart.ChartAreas[0].AxisX.Interval - 2.5D) < 0.000001D,
                    "manual X interval applied");
                Check(Math.Abs(chart.ChartAreas[0].AxisY.Minimum - (-1D)) < 0.000001D &&
                    Math.Abs(chart.ChartAreas[0].AxisY.Maximum - 2D) < 0.000001D,
                    "manual Y range applied");
                Check(Math.Abs(chart.ChartAreas[0].AxisY.Interval - 0.25D) < 0.000001D,
                    "manual Y interval applied");
                Check(ReadSaved(configPath, "Station1.Plc.PollIntervalMs") == "500",
                    "500ms persisted");

                object capture = Field(form, "m_Station1Capture");
                Call(capture, "Reset", 99L, DateTime.Now);
                SetGridValue(grids, "Station1.Plc.PollIntervalMs", "600");
                bool rejectedWhileMeasuring = false;
                try
                {
                    ((Task)Call(settingsPage, "SaveValuesAndApplyAsync"))
                        .GetAwaiter().GetResult();
                }
                catch (InvalidOperationException)
                {
                    rejectedWhileMeasuring = true;
                }
                Check(rejectedWhileMeasuring, "SAVE rejected during active measurement");
                Check(ReadSaved(configPath, "Station1.Plc.PollIntervalMs") == "500",
                    "rejected SAVE does not persist mixed-cycle setting");
                Call(capture, "Abort");

                Task simulation = (Task)Call(station1, "RunSimulationAsync", 0);
                Check(station1.LogText.Contains("SIMULATION 시작: 500ms 간격"),
                    "simulation measurement uses saved 500ms interval");
                ((System.Threading.CancellationTokenSource)Field(
                    station1,
                    "m_SimulationCancellation")).Cancel();
                simulation.GetAwaiter().GetResult();
                Check(!(bool)Property(station1, "IsSimulationRun"),
                    "configured simulation interval can be cancelled cleanly");

                ((IDisposable)reader1).Dispose();
                ((IDisposable)reader2).Dispose();
            }

            Console.WriteLine("PASS: " + checks + " runtime settings SAVE checks");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
