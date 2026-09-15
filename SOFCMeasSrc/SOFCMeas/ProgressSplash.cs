using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace SOFCMeas
{
    internal sealed class ProgressSplash : IDisposable
    {
        private sealed class SplashWindow : Form
        {
            internal SplashWindow()
            {
                SuspendLayout();
                AutoScaleDimensions = new SizeF(96F, 96F);
                AutoScaleMode = AutoScaleMode.Dpi;
            }

            protected override bool ShowWithoutActivation { get { return true; } }
        }

        private readonly Thread thread;
        private readonly ManualResetEventSlim ready = new ManualResetEventSlim();
        private Form window;
        private Label status;
        private ProgressBar progress;
        private Exception failure;
        private int disposed;

        internal ProgressSplash(string title)
        {
            thread = new Thread(() =>
            {
                try
                {
                    using (window = new SplashWindow { Text = title, ClientSize = new Size(480, 225),
                        FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.CenterScreen,
                        ShowInTaskbar = false, TopMost = true, BackColor = Color.FromArgb(36, 45, 60) })
                    {
                        var logo = new PictureBox { Image = Properties.Resources.AMOCI,
                            BackColor = Color.White, SizeMode = PictureBoxSizeMode.Zoom, Bounds = new Rectangle(20, 20, 120, 80) };
                        window.Controls.Add(logo);
                        window.Controls.Add(new Label { Text = title, ForeColor = Color.White,
                            Font = new Font("맑은 고딕", 17F, FontStyle.Bold), Bounds = new Rectangle(155, 35, 300, 55) });
                        status = new Label { ForeColor = Color.White, Font = new Font("맑은 고딕", 11F), Bounds = new Rectangle(20, 120, 440, 30) };
                        progress = new ProgressBar { Minimum = 0, Maximum = 100, Bounds = new Rectangle(20, 164, 440, 25) };
                        window.Controls.Add(status); window.Controls.Add(progress);
                        window.Shown += delegate { ready.Set(); };
                        window.ResumeLayout(true);
                        Application.Run(window);
                    }
                }
                catch (Exception ex) { failure = ex; ready.Set(); }
            });
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ready.Wait();
            if (failure != null) throw new InvalidOperationException("진행 창을 열 수 없습니다.", failure);
        }

        internal void Report(int percent, string message)
        {
            if (Volatile.Read(ref disposed) != 0) return;
            window.Invoke((Action)(() =>
            {
                progress.Value = Math.Max(0, Math.Min(100, percent));
                status.Text = message + "  " + progress.Value + "%";
                window.Refresh();
            }));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            if (window != null && !window.IsDisposed) window.Invoke((Action)(() => window.Close()));
            thread.Join();
            ready.Dispose();
        }
    }
}
