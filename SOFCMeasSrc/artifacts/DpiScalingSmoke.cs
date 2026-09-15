// Geometry scaling is simulated with Control.Scale; this does not change monitor DPI.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SOFCMeas;
using MainForm = global::SOFCMeas.SOFCMeas;

internal static class DpiScalingSmoke
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;
    private static string output;

    private static object Field(object instance, string name)
    { return instance.GetType().GetField(name, Private).GetValue(instance); }

    private static void Call(object instance, string name, params object[] args)
    { instance.GetType().GetMethod(name, Private).Invoke(instance, args); }

    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new InvalidOperationException(message); }

    private static void Near(int actual, float expected, string message)
    { Check(Math.Abs(actual - expected) <= 3, message + ": actual=" + actual + ", expected=" + expected); }

    private static void CheckDpi(ContainerControl control)
    {
        Check(control.AutoScaleMode == AutoScaleMode.Dpi, control.GetType().Name + " must use DPI scaling");
        SizeF baseline = control.AutoScaleDimensions;
        SizeF current = control.CurrentAutoScaleDimensions;
        Check(baseline.Width == baseline.Height && baseline.Width >= 96,
            control.GetType().Name + " invalid DPI baseline " + baseline);
        Check(baseline.Width == 96 || Math.Abs(baseline.Width - current.Width) < 0.1f,
            control.GetType().Name + " baseline is neither design 96 DPI nor current DPI: " + baseline);
    }

    private static void Capture(Control control, string name)
    {
        using (var bitmap = new Bitmap(control.Width, control.Height))
        {
            control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(output, name + ".png"), ImageFormat.Png);
        }
    }

    [STAThread]
    private static int Main()
    {
        try
        {
            output = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dpi-smoke");
            string root = Path.Combine(output, "isolated-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "conf"));
            File.WriteAllText(Path.Combine(root, "conf", "SOFCMeas.config"),
                "<configuration><appSettings><add key=\"Plc.AutoConnect\" value=\"false\" />" +
                "<add key=\"Log.AutoDelete\" value=\"false\" />" +
                "<add key=\"Station1.Plc.AutoConnect\" value=\"false\" />" +
                "<add key=\"Station2.Plc.AutoConnect\" value=\"false\" /></appSettings></configuration>");
            Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", root);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            VerifyOtherContainers();
            foreach (float factor in new[] { 1f, 1.5f, 2f }) VerifyMain(factor);
            Console.WriteLine("PASS: " + checks + " DPI configuration/layout/navigation checks.");
            Console.WriteLine("Factors 1.0/1.5/2.0 use Control.Scale geometry simulation, not real monitor-DPI transitions.");
            Console.WriteLine("PLC startup, application entry point, and splash worker threads were not executed.");
            Console.WriteLine("Screenshots and isolated runtime data: " + output);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void VerifyMain(float factor)
    {
        using (var form = new MainForm())
        {
            form.Shown -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), form,
                typeof(MainForm).GetMethod("SOFCMeas_Shown", Private));
            foreach (string name in new[] { "m_LogCleanupTimer", "m_IndexRecoveryTimer" })
                ((Timer)Field(form, name)).Stop();
            Call(form, "ApplyAuthority", true);
            form.WindowState = FormWindowState.Normal;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-20000, -20000);
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();
            Console.WriteLine("Runtime DPI=" + form.CurrentAutoScaleDimensions.Width + "; DeviceDpi=" + form.DeviceDpi + "; simulated factor=" + factor);
            var pages = new Form[] { (Form)Field(form, "m_MainPage"), (Form)Field(form, "m_LogPage"), (Form)Field(form, "m_SettingPage") };
            string[] buttonNames = { "btnMainView", "btnLogView", "btnSettingView" };
            foreach (Form page in pages) CheckDpi(page);
            CheckDpi(form);
            var title = (Control)Field(form, "pnlTitle");
            var main = (Control)Field(form, "pnlMain");
            var log = (Control)Field(form, "pnlLog");
            var first = (StationView)Field(form, "stationView1");
            var second = (StationView)Field(form, "stationView2");
            CheckDpi(first); CheckDpi(second);
            Check(main.Parent == pages[0] && log.Parent == pages[1], "Transferred content must remain hosted in its page");
            var graph = (Control)Field(first, "chartLoad");
            int initialTitleHeight = title.Height;
            Rectangle initialStation = first.Bounds;
            Rectangle initialGraph = graph.Bounds;
            if (factor != 1) form.Scale(new SizeF(factor, factor));
            form.PerformLayout();
            Application.DoEvents();
            Console.WriteLine("Client=" + form.ClientSize + "; minimum=" + form.MinimumSize + "; maximum=" + form.MaximumSize + "; title=" + title.Bounds);
            Near(title.Height, initialTitleHeight * factor, "Title scale");
            Near(first.Width, initialStation.Width * factor, "Station width scale");
            Near(first.Left, initialStation.Left * factor, "Station horizontal scale");
            Near(graph.Width, initialGraph.Width * factor, "Nested chart width scale");
            Near(graph.Top, initialGraph.Top * factor, "Nested chart vertical scale");
            foreach (StationView station in new[] { first, second })
                foreach (string suffix in new[] { "Model", "Operator", "LotNumber", "Spec" })
                {
                    var label = (Control)Field(station, "lbl" + suffix);
                    var input = (Control)Field(station, "txt" + suffix);
                    Check(label.Top == input.Top && label.Height == input.Height,
                        "Job label/input misalignment: " + station.Name + "." + suffix + " label=" + label.Bounds + " input=" + input.Bounds);
                }
            foreach (string name in new[] { "btnMinimize", "btnExit" })
            {
                var button = (Control)Field(form, name);
                Console.WriteLine(name + "=" + button.Bounds);
                Check(title.ClientRectangle.Contains(button.Bounds), "Window button outside header: " + name + "=" + button.Bounds);
            }
            var stable = new Dictionary<Control, Rectangle>();
            foreach (Control control in new Control[] { first, second, graph, title, (Control)Field(form, "pnlLogFilter") })
                stable.Add(control, control.Bounds);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
                {
                    Call(form, "SelectNavigationPage", pages[pageIndex], Field(form, buttonNames[pageIndex]));
                    Application.DoEvents();
                    foreach (Form page in pages)
                    {
                        Check(page.Visible == (page == pages[pageIndex]), "Only selected page must be visible");
                        Check(page.Top == title.Bottom, "Page/header boundary: " + page.Name + " " + page.Bounds + " title=" + title.Bounds);
                        Check(page.Width == form.ClientSize.Width && page.Bottom == form.ClientSize.Height,
                            "Page must fill remaining client area: " + page.Name);
                    }
                    Check(first.Right <= second.Left, "Stations overlap");
                    Check(main.Bounds == pages[0].ClientRectangle && log.Bounds == pages[1].ClientRectangle,
                        "Reparented content must fill its page");
                    foreach (var pair in stable)
                        Check(pair.Key.Bounds == pair.Value, "Repeated navigation changed geometry: " + pair.Key.Name);
                }
            }
            Call(form, "SelectNavigationPage", pages[0], Field(form, buttonNames[0]));
            Application.DoEvents();
            var scroll = (ScrollableControl)main;
            Check(scroll.AutoScroll, "Main content must permit scrolling when enlarged");
            if (factor == 1)
                Check(!scroll.VerticalScroll.Visible && !scroll.HorizontalScroll.Visible,
                    "Fitting main content should not need scrollbars");
            else
            {
                Check(scroll.VerticalScroll.Visible, "Short main viewport must expose vertical scrolling");
                Check(scroll.DisplayRectangle.Bottom >= Math.Max(first.Bottom, second.Bottom),
                    "Scroll extent must include both station bottoms");
                scroll.AutoScrollPosition = new Point(0, scroll.VerticalScroll.Maximum);
                Application.DoEvents();
                foreach (StationView station in new[] { first, second })
                    foreach (string name in new[] { "btnStart", "btnLotEnd" })
                    {
                        var button = (Control)Field(station, name);
                        Rectangle viewportBounds = scroll.RectangleToClient(button.Parent.RectangleToScreen(button.Bounds));
                        Check(scroll.ClientRectangle.Contains(viewportBounds),
                            "Scrolled button must be accessible: " + station.Name + "." + name + "=" + viewportBounds);
                    }
                if (factor == 2) Capture(form, "main-bottom-scroll-simulated-2.0");
                scroll.AutoScrollPosition = Point.Empty;
                Application.DoEvents();
                Check(scroll.AutoScrollPosition == Point.Empty, "Scroll position must restore to top");
                foreach (var pair in stable)
                    Check(pair.Key.Bounds == pair.Value, "Scrolling changed geometry: " + pair.Key.Name);
            }
            Capture(form, "main-simulated-" + factor.ToString("0.0", CultureInfo.InvariantCulture));
            if (factor == 2)
            {
                Call(form, "SelectNavigationPage", pages[2], Field(form, buttonNames[2]));
                Application.DoEvents();
                Capture(form, "settings-simulated-2.0");
            }
            foreach (string reader in new[] { "m_Station1Reader", "m_Station2Reader" })
                ((IDisposable)Field(form, reader)).Dispose();
        }
    }

    private static void VerifyOtherContainers()
    {
        Type splashType = typeof(MainForm).Assembly.GetType("SOFCMeas.ProgressSplash").GetNestedType("SplashWindow", BindingFlags.NonPublic);
        using (var splash = (Form)Activator.CreateInstance(splashType, true))
        { splash.ResumeLayout(true); CheckDpi(splash); }
        using (var station = new StationView()) CheckDpi(station);
        using (var main = new MainUiForm()) CheckDpi(main);
        using (var log = new LogViewForm()) CheckDpi(log);
        using (var settings = new SettingForm()) CheckDpi(settings);
        using (var dialog = new AuthorityDialog(false))
        {
            CheckDpi(dialog);
            dialog.StartPosition = FormStartPosition.Manual;
            dialog.Location = new Point(-20000, -20000);
            dialog.ShowInTaskbar = false;
            dialog.Show();
            Application.DoEvents();
            dialog.PerformLayout();
            Capture(dialog, "authority-simulated-1.0");
            Rectangle original = dialog.Controls.Find("rdoAdministrator", true)[0].Bounds;
            dialog.Scale(new SizeF(2, 2));
            dialog.PerformLayout();
            Near(dialog.Controls.Find("rdoAdministrator", true)[0].Left, original.Left * 2, "Authority control scale");
            var apply = dialog.Controls.Find("btnApply", true)[0];
            var password = dialog.Controls.Find("txtPassword", true)[0];
            Check(password.Bottom <= apply.Top, "Authority input overlaps apply button");
            Capture(dialog, "authority-simulated-2.0");
        }
    }
}
