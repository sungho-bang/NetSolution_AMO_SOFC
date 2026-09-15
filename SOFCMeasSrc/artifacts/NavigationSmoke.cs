using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SOFCMeas;
using MainForm = global::SOFCMeas.SOFCMeas;
using Krypton.Toolkit;
class NavigationSmoke
{
    static object Field(MainForm form, string name) { return typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form); }
    [STAThread] static void Main()
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
                    if (i == 2 && !((Control)Field(form, "numLogRetentionDays")).Visible) throw new Exception("Settings missing");
                }
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SettingPreview.png"));
            }
            Console.WriteLine("Three-page navigation, repeated toggles, log/settings visibility: PASS (PLC start disabled)");
        }
    }
}
