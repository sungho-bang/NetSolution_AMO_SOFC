using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using SOFCMeas;
using MainForm = global::SOFCMeas.SOFCMeas;

internal static class SpecSaveSmoke
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

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string root = Path.GetFullPath(args[0]);
            Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", root);
            Directory.CreateDirectory(Path.Combine(root, "conf"));
            File.WriteAllText(Path.Combine(root, "conf", "SOFCMeas.config"),
                "<configuration><appSettings>" +
                "<add key=\"Runtime.Simulation\" value=\"0\" />" +
                "<add key=\"Station1.Plc.AutoConnect\" value=\"false\" />" +
                "<add key=\"Station2.Plc.AutoConnect\" value=\"false\" />" +
                "<add key=\"Log.AutoDelete\" value=\"false\" />" +
                "</appSettings></configuration>");

            Application.EnableVisualStyles();
            using (var form = new MainForm())
            {
                ((Timer)Field(form, "m_LogCleanupTimer")).Stop();
                ((Timer)Field(form, "m_IndexRecoveryTimer")).Stop();
                var station1 = (StationView)Field(form, "stationView1");
                var station2 = (StationView)Field(form, "stationView2");
                var settingsPage = (SettingForm)Field(form, "m_SettingPage");
                FieldInfo canSave = settingsPage.GetType().GetField("CanSave", Flags);
                canSave.SetValue(settingsPage, new Func<bool>(() => true));

                Check(station1.SpecText == "0.4" && station2.SpecText == "0.4",
                    "initial SPEC values");
                Call(station1, "ResetLoadGraph");
                Call(station1, "AddLoadSample", 0.405D, (int?)405);

                var grids = (List<DataGridView>)Field(settingsPage, "grids");
                foreach (DataGridViewRow row in grids[1].Rows)
                    if ((string)row.Tag == "Station1.Inspection.ThresholdKgf")
                        row.Cells[1].Value = "0.41";
                foreach (DataGridViewRow row in grids[2].Rows)
                    if ((string)row.Tag == "Station2.Inspection.ThresholdKgf")
                        row.Cells[1].Value = "0.52";

                Call(settingsPage, "SaveValues");
                Check(station1.SpecText == "0.41", "station 1 UI SPEC updates on SAVE");
                Check(station2.SpecText == "0.52", "station 2 UI SPEC updates on SAVE");

                object result1 = Call(station1, "CompleteInspection");
                Check(!(bool)Property(result1, "IsPass"),
                    "sample below new station 1 SPEC is NG without losing active graph");
                Check(Math.Abs((double)Property(result1, "LowerSpecKgf") - 0.41D) < 0.0000001D,
                    "station 1 judgment uses saved SPEC");

                Call(station2, "ResetLoadGraph");
                Call(station2, "AddLoadSample", 0.52D, (int?)520);
                object result2 = Call(station2, "CompleteInspection");
                Check((bool)Property(result2, "IsPass"),
                    "sample equal to new station 2 SPEC is GOOD");
                Check(Math.Abs((double)Property(result2, "LowerSpecKgf") - 0.52D) < 0.0000001D,
                    "station 2 judgment uses saved SPEC");

                var saved = new XmlDocument();
                saved.Load(Path.Combine(root, "conf", "SOFCMeas.config"));
                string station1Saved = saved.SelectSingleNode(
                    "/configuration/appSettings/add[@key='Station1.Inspection.ThresholdKgf']")
                    .Attributes["value"].Value;
                string station2Saved = saved.SelectSingleNode(
                    "/configuration/appSettings/add[@key='Station2.Inspection.ThresholdKgf']")
                    .Attributes["value"].Value;
                Check(station1Saved == "0.41" && station2Saved == "0.52",
                    "saved SPEC values persist in configuration");

                ((IDisposable)Field(form, "m_Station1Reader")).Dispose();
                ((IDisposable)Field(form, "m_Station2Reader")).Dispose();
            }

            Console.WriteLine("PASS: " + checks + " SAVE-to-UI-and-judgment checks");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
