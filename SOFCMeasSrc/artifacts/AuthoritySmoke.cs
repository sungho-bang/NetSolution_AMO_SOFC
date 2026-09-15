using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SOFCMeas;
using MainForm = global::SOFCMeas.SOFCMeas;
using Krypton.Toolkit;
class AuthoritySmoke
{
    static object Field(MainForm form, string name) { return typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form); }
    [STAThread] static void Main() { try { Run(); } catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message); Environment.ExitCode = 1; } } static void Run()
    {
        Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", Path.Combine(Path.GetTempPath(), "SOFCNavigation-" + Guid.NewGuid().ToString("N")));
        Application.EnableVisualStyles();
        using (var form = new MainForm())
        {
            var shown = typeof(MainForm).GetMethod("SOFCMeas_Shown", BindingFlags.Instance | BindingFlags.NonPublic);
            form.Shown -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), form, shown);
            ((Timer)Field(form, "m_LogCleanupTimer")).Stop();
            form.WindowState = FormWindowState.Normal;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-32000, -32000);
            form.ShowInTaskbar = false;
            form.Show();
            if (((KryptonButton)Field(form, "btnLogView")).Enabled || ((KryptonButton)Field(form, "btnSettingView")).Enabled) throw new Exception("Operator permissions failed");
            var settingsPage = (SettingForm)Field(form, "m_SettingPage");
            bool blocked = false;
            try { typeof(SettingForm).GetMethod("SaveValues", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(settingsPage, null); }
            catch (TargetInvocationException ex) { blocked = ex.InnerException is UnauthorizedAccessException; }
            if (!blocked) throw new Exception("Operator save was allowed");
            using (var auth = new AuthorityDialog(false))
            {
                auth.StartPosition = FormStartPosition.Manual; auth.Location = new Point(-32000, -32000); auth.Show();
                var role = (ComboBox)auth.Controls["cboRole"];
                var password = (TextBox)auth.Controls["txtPassword"];
                var apply = (Button)auth.Controls["btnApply"];
                if (password.Enabled) throw new Exception("Operator requested password");
                role.SelectedIndex = 1; password.Text = "wrong"; apply.PerformClick();
                if (auth.Administrator || auth.DialogResult == DialogResult.OK) throw new Exception("Wrong password accepted");
                password.Text = "1234"; apply.PerformClick();
                if (!auth.Administrator || auth.DialogResult != DialogResult.OK) throw new Exception("Default password failed");
                typeof(MainForm).GetMethod("ApplyAuthority", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { auth.Administrator });
            }
            string[] pages = { "m_MainPage", "m_LogPage", "m_SettingPage" };
            string[] buttons = { "btnMainView", "btnLogView", "btnSettingView" };
            for (int cycle = 0; cycle < 2; cycle++)
                for (int i = 0; i < 3; i++)
                {
                    ((KryptonButton)Field(form, buttons[i])).PerformClick();
                    Application.DoEvents();
                    for (int j = 0; j < 3; j++)
                        if (((Form)Field(form, pages[j])).Visible != (i == j)) throw new Exception("Page toggle failed");
                    if (i == 1 && !((Control)Field(form, "dgvLogs")).Visible) throw new Exception("Log content hidden");
                    if (i == 2 && ((Form)Field(form, "m_SettingPage")).Controls.Count < 2) throw new Exception("Settings missing");
                }
            var page = (SettingForm)Field(form, "m_SettingPage");
            var gridField = typeof(SettingForm).GetField("grids", BindingFlags.Instance | BindingFlags.NonPublic);
            var grids = (System.Collections.Generic.List<DataGridView>)gridField.GetValue(page);
            foreach (DataGridViewRow row in grids[1].Rows)
            {
                string key = (string)row.Tag;
                if (key == "Station1.Plc.Port") row.Cells[1].Value = "12345";
                if (key == "Station1.Lot.SaveCount") row.Cells[1].Value = "3";
                if (key == "Station1.Graph.XAuto" || key == "Station1.Graph.YAuto") row.Cells[1].Value = "false";
                if (key == "Station1.Graph.XMax") row.Cells[1].Value = "30";
                if (key == "Station1.Graph.YMax") row.Cells[1].Value = "2";
            }
            typeof(SettingForm).GetMethod("SaveValues", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(page, null);
            var plcType = typeof(MainForm).Assembly.GetType("SOFCMeas.PlcGraphSettings");
            var load = plcType.GetMethod("Load", BindingFlags.Static | BindingFlags.NonPublic);
            object plc1 = load.Invoke(null, new object[] { 1, "127.0.0.1" });
            object plc2 = load.Invoke(null, new object[] { 2, "127.0.0.1" });
            var port = plcType.GetProperty("Port", BindingFlags.Instance | BindingFlags.NonPublic);
            if ((int)port.GetValue(plc1, null) != 12345 || (int)port.GetValue(plc2, null) != 10000) throw new Exception("PLC isolation failed");
            var station = (StationView)Field(form, "stationView1");
            typeof(StationView).GetMethod("ConfigureGraph", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(station, new object[] { 1 });
            var chart = (System.Windows.Forms.DataVisualization.Charting.Chart)typeof(StationView).GetField("chartLoad", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(station);
            typeof(StationView).GetMethod("AddLoadSample", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(station, new object[] { 50.0 });
            if (chart.ChartAreas[0].AxisX.Maximum != 30 || chart.ChartAreas[0].AxisY.Maximum != 2) throw new Exception("Fixed axes were overwritten");
            if ((int)typeof(StationView).GetField("LotSaveCount", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(station) != 3) throw new Exception("LOT count not loaded");
            var capture = Field(form, "m_Station1Capture");
            var lot = Field(form, "m_Station1Lot");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            lot.GetType().GetField("MaximumInspectionCount", flags).SetValue(lot, 3);
            var assembly = typeof(MainForm).Assembly;
            string dataRoot = Path.Combine(Environment.GetEnvironmentVariable("SOFCMEAS_ROOT"), "Data");
            for (int n = 1; n <= 3; n++)
            {
                typeof(StationView).GetMethod("ResetLoadGraph", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(station, null);
                capture.GetType().GetMethod("Reset", flags).Invoke(capture, new object[] { (long)n, DateTime.Now });
                object sample = Activator.CreateInstance(assembly.GetType("SOFCMeas.PlcLoadSampleEventArgs"), flags, null,
                    new object[] { (long)n, 1, 500, 0.5, DateTime.Now }, null);
                capture.GetType().GetMethod("Add", flags).Invoke(capture, new[] { sample });
                typeof(StationView).GetMethod("AddLoadSample", flags).Invoke(station, new object[] { 0.5 });
                object ended = Activator.CreateInstance(assembly.GetType("SOFCMeas.PlcGraphCycleEventArgs"), flags, null,
                    new object[] { (long)n, DateTime.Now, DateTime.Now, 1 }, null);
                typeof(MainForm).GetMethod("CompleteAndBufferInspection", flags).Invoke(form,
                    new object[] { "STATION-1", "설비 #1", station, capture, lot, ended });
                if (n < 3 && Directory.GetFiles(dataRoot, "*.csv", SearchOption.AllDirectories).Length != 0) throw new Exception("LOT saved before threshold");
            }
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while ((bool)lot.GetType().GetProperty("IsSaving", flags).GetValue(lot, null) && clock.ElapsedMilliseconds < 5000)
            { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
            if (Directory.GetFiles(dataRoot, "*.csv", SearchOption.AllDirectories).Length == 0 ||
                (int)lot.GetType().GetProperty("InspectionCount", flags).GetValue(lot, null) != 0) throw new Exception("LOT automatic save failed");
            if (((Control)Field(form, "lblTitle")).BackgroundImage == null) throw new Exception("Logo missing");
            form.ClientSize = new Size(1500, 900); Application.DoEvents();
            if (page.Width != form.ClientSize.Width || page.Height != form.ClientSize.Height - 80) throw new Exception("Page did not resize: page=" + page.Size + " client=" + form.ClientSize);
            form.ClientSize = new Size(1920, 1040); Application.DoEvents();
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SettingPreview.png"));
            }
            typeof(MainForm).GetMethod("ApplyAuthority", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { false });
            if (!((Form)Field(form,"m_MainPage")).Visible || ((Form)Field(form,"m_SettingPage")).Visible || ((KryptonButton)Field(form,"btnLogView")).Enabled) throw new Exception("Operator downgrade failed");
            Console.WriteLine("Role dialog authentication and page/save restrictions: PASS; Settings save/reload, PLC isolation, fixed graph axes, LOT count, CI and responsive layout: PASS (PLC start disabled)");
        }
    }
}






