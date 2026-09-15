using System.Drawing;
using System.Windows.Forms;
using Krypton.Toolkit;

namespace SOFCMeas
{
    public partial class SOFCMeas
    {
        private MainUiForm m_MainPage;
        private LogViewForm m_LogPage;
        private SettingForm m_SettingPage;
        private bool m_IsAdministrator;

        private void InitializeNavigationPages()
        {
            InitializeWindowButtons();
            ApplicationPaths.EnsureStorageDirectories();
            AuthorityDialog.EnsureDefaultPassword();
            m_MainPage = new MainUiForm(pnlMain);
            m_LogPage = new LogViewForm(pnlLog);
            m_SettingPage = new SettingForm();
            m_SettingPage.CanSave = () => m_IsAdministrator;
            lblUser.Cursor = Cursors.Hand;
            lblUser.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            lblUser.Click += delegate
            {
                using (var dialog = new AuthorityDialog(m_IsAdministrator))
                    if (dialog.ShowDialog(this) == DialogResult.OK) ApplyAuthority(dialog.Administrator);
            };
            ApplyAuthority(false);
            foreach (Control policy in new Control[] { lblLogStoragePath, chkLogAutoDelete,
                lblLogRetentionDays, numLogRetentionDays, btnApplyLogPolicy, lblLogPolicyStatus })
                policy.Visible = false;
            foreach (var page in new Form[] { m_MainPage, m_LogPage, m_SettingPage })
            {
                page.TopLevel = false;
                page.Bounds = new Rectangle(0, pnlTitle.Bottom, ClientSize.Width,
                    System.Math.Max(1, ClientSize.Height - pnlTitle.Bottom));
                page.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                Controls.Add(page);
            }
            btnSettingView.Click += delegate { SelectNavigationPage(m_SettingPage, btnSettingView); };
            // DPI scaling updates parent and child bounds in separate steps.
            // Reconcile after layout resumes; resizing the title during scaling
            // would move right-anchored window buttons before they scale again.
            Layout += delegate { LayoutNavigationPages(); };
        }

        private void LayoutNavigationPages()
        {
            pnlTitle.Width = ClientSize.Width;
            foreach (var page in new Form[] { m_MainPage, m_LogPage, m_SettingPage })
                page.Bounds = new Rectangle(0, pnlTitle.Bottom, ClientSize.Width,
                    System.Math.Max(1, ClientSize.Height - pnlTitle.Bottom));
        }

        private void SelectNavigationPage(Form selected, KryptonButton button)
        {
            if (!m_IsAdministrator && selected != m_MainPage) return;
            foreach (var page in new Form[] { m_MainPage, m_LogPage, m_SettingPage })
            {
                if (page == selected) page.Show();
                else page.Hide();
            }
            selected.BringToFront();
            pnlTitle.BringToFront();
            foreach (var item in new[] { btnMainView, btnLogView, btnSettingView })
            {
                SetMenuState(item, item == button);
                item.AccessibleDescription = item == button ? "선택됨" : "선택 안 됨";
            }
            button.Select();
        }

        private void ApplyAuthority(bool administrator)
        {
            m_IsAdministrator = administrator;
            lblUser.Text = administrator ? "ADMINISTRATOR" : "OPERATOR";
            btnLogView.Enabled = administrator;
            btnSettingView.Enabled = administrator;
            if (!administrator && m_MainPage.Parent != null) ShowMainView();
        }

        private void InitializeWindowButtons()
        {
            var minimize = CreateWindowIcon(false);
            var close = CreateWindowIcon(true);
            btnMinimize.Text = string.Empty;
            btnExit.Text = string.Empty;
            btnMinimize.Image = minimize;
            btnExit.Image = close;
            btnMinimize.Click += delegate { WindowState = FormWindowState.Minimized; };
            var hints = new ToolTip();
            hints.SetToolTip(btnMinimize, "최소화");
            hints.SetToolTip(btnExit, "종료");
            Disposed += delegate { minimize.Dispose(); close.Dispose(); hints.Dispose(); };
        }

        private static Bitmap CreateWindowIcon(bool close)
        {
            var icon = new Bitmap(24, 24);
            using (var graphics = Graphics.FromImage(icon))
            using (var pen = new Pen(Color.White, 2.4F))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                pen.StartCap = pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                if (close)
                {
                    graphics.DrawLine(pen, 5, 5, 19, 19);
                    graphics.DrawLine(pen, 19, 5, 5, 19);
                }
                else graphics.DrawLine(pen, 4, 15, 20, 15);
            }
            return icon;
        }
    }
}


