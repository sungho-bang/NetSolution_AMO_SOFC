using System.Drawing;
using System.Threading.Tasks;
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
            m_SettingPage.ValidateRuntimeApply = ValidateRuntimeSettingsApply;
            m_SettingPage.ApplySavedSettingsAsync = ApplySavedMeasurementSettingsAsync;
            m_SettingPage.SettingsSaved += delegate { ApplySavedSpecSettings(); };
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

        private void ApplySavedSpecSettings()
        {
            var settings = SettingsCatalog.Load();
            double station1Spec = SettingsCatalog.Number(
                settings,
                "Station1.Inspection.ThresholdKgf");
            double station2Spec = SettingsCatalog.Number(
                settings,
                "Station2.Inspection.ThresholdKgf");

            stationView1.ApplyInspectionThreshold(station1Spec);
            stationView2.ApplyInspectionThreshold(station2Spec);

            if (m_ApplicationLogService != null)
            {
                m_ApplicationLogService.Info(
                    "SETTINGS",
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "event=SPEC_SETTINGS_APPLIED station1Kgf={0:0.000000} station2Kgf={1:0.000000}",
                        station1Spec,
                        station2Spec));
            }
        }

        private string ValidateRuntimeSettingsApply()
        {
            if (m_Station1Capture.IsActive || m_Station2Capture.IsActive ||
                stationView1.IsSimulationRun || stationView2.IsSimulationRun)
                return "측정이 진행 중입니다. 현재 측정이 끝난 뒤 SAVE해 주세요.";
            if (m_Station1Lot.IsSaving || m_Station2Lot.IsSaving)
                return "LOT 저장이 진행 중입니다. 저장이 끝난 뒤 SAVE해 주세요.";
            return null;
        }

        private async Task ApplySavedMeasurementSettingsAsync()
        {
            var values = SettingsCatalog.Load();
            PlcGraphSettings station1Settings = PlcGraphSettings.Load(1, "172.20.9.100");
            PlcGraphSettings station2Settings = PlcGraphSettings.Load(2, "172.20.9.101");
            bool simulation = values["Runtime.Simulation"] == "1";

            await Task.WhenAll(
                m_Station1Reader.ApplySettingsAsync(station1Settings, !simulation),
                m_Station2Reader.ApplySettingsAsync(station2Settings, !simulation));

            if (simulation)
            {
                stationView1.ApplyCollectionEnabled(false);
                stationView2.ApplyCollectionEnabled(false);
            }

            ApplyStationMeasurementSettings(stationView1, m_Station1Lot, station1Settings);
            ApplyStationMeasurementSettings(stationView2, m_Station2Lot, station2Settings);
            LogStationSettings(station1Settings);
            LogStationSettings(station2Settings);

            m_ApplicationLogService.Info(
                "SETTINGS",
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "event=MEASUREMENT_SETTINGS_APPLIED simulation={0} " +
                    "station1PollMs={1} station2PollMs={2}",
                    simulation,
                    station1Settings.PollIntervalMilliseconds,
                    station2Settings.PollIntervalMilliseconds));
        }

        private static void ApplyStationMeasurementSettings(
            StationView stationView,
            StationLotState lotState,
            PlcGraphSettings settings)
        {
            stationView.StationTitle = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "설비 #{0}  |  PLC {1}",
                settings.StationNumber,
                settings.IpAddress);
            stationView.MaximumGraphPoints = settings.MaximumGraphPoints;
            stationView.ConfigureGraph(settings.StationNumber, false);
            stationView.SetLoadDeviceName(settings.LoadRawDevice);
            lotState.MaximumInspectionCount = stationView.LotSaveCount;
            stationView.AppendLog(
                System.DateTime.Now,
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "설정 적용 완료: PLC {0}:{1}, 읽기 주기 {2}ms",
                    settings.IpAddress,
                    settings.Port,
                    settings.PollIntervalMilliseconds));
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


