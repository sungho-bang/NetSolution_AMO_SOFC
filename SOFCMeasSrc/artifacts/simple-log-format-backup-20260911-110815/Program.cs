using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SOFCMeas
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationLogService logService = null;
            ProgressSplash splash = null;
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                ApplicationPaths.EnsureStorageDirectories();
                StoragePolicySettings storageSettings = StoragePolicySettings.Load();
                logService = new ApplicationLogService(
                    storageSettings.MaximumLogFileSizeBytes);

                ApplicationLogService capturedLogService = logService;
                AppDomain.CurrentDomain.UnhandledException += delegate(
                    object sender,
                    UnhandledExceptionEventArgs e)
                {
                    Exception exception = e.ExceptionObject as Exception ??
                        new Exception(Convert.ToString(
                            e.ExceptionObject,
                            CultureInfo.InvariantCulture));
                    capturedLogService.Error(
                        "APPLICATION",
                        "event=UNHANDLED_EXCEPTION isTerminating=" + e.IsTerminating,
                        exception);
                };
                TaskScheduler.UnobservedTaskException += delegate(
                    object sender,
                    UnobservedTaskExceptionEventArgs e)
                {
                    capturedLogService.Error(
                        "APPLICATION",
                        "event=UNOBSERVED_TASK_EXCEPTION",
                        e.Exception);
                };

                logService.Info(
                    "APPLICATION",
                    "event=APPLICATION_BOOTSTRAP_STARTED session=" +
                    ApplicationLogService.CurrentSessionId);
                if (logService.LastError != null)
                {
                    MessageBox.Show(
                        "로그 파일을 생성하지 못했습니다." + Environment.NewLine +
                        "저장 경로: " + logService.RootDirectory + Environment.NewLine +
                        logService.LastError.Message,
                        "로그 저장 오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                LicenseValidationResult licenseResult =
                    LicensePolicy.ValidateAndUpdate();
                if (!licenseResult.IsValid)
                {
                    logService.Warning(
                        "LICENSE",
                        "event=LICENSE_VALIDATION_FAILED status=" +
                        licenseResult.Status);
                    MessageBox.Show(
                        licenseResult.Message,
                        "Software Authorization",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                logService.Info(
                    "LICENSE",
                    "event=LICENSE_VALIDATION_SUCCEEDED " +
                    "licensedAdapterFound=true durationPolicy=" +
                    LicensePolicy.DurationPolicyLogValue +
                    " firstRunUtc=" + FormatUtc(licenseResult.FirstRunUtc) +
                    " expirationUtc=" + FormatUtc(licenseResult.ExpirationUtc));

                splash = new ProgressSplash("프로그램 시작 중");
                splash.Report(10, "저장 폴더와 설정을 읽고 있습니다.");
                splash.Report(30, "화면을 준비하고 있습니다.");
                var main = new SOFCMeas(storageSettings, logService, splash.Report);
                main.Shown += delegate
                {
                    splash.Report(100, "시작 완료");
                    splash.Dispose();
                    // Activate after the splash's close/activation messages have been processed.
                    main.BeginInvoke((Action)(() =>
                    {
                        if (main.IsDisposed || main.Disposing) return;
                        main.BringToFront();
                        main.Activate();
                    }));
                };
                Application.Run(main);
                logService.Info(
                    "APPLICATION",
                    "event=APPLICATION_MESSAGE_LOOP_EXITED");
            }
            catch (Exception exception)
            {
                if (logService != null)
                {
                    logService.Error(
                        "APPLICATION",
                        "event=APPLICATION_FATAL_ERROR",
                        exception);
                }
                else
                {
                    MessageBox.Show(
                        "저장 폴더를 초기화하지 못했습니다." + Environment.NewLine +
                        "저장 경로: " + ApplicationPaths.ApplicationRootDirectory +
                        Environment.NewLine + exception.Message,
                        "저장 경로 오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                throw;
            }
            finally { if (splash != null) splash.Dispose(); }
        }

        private static string FormatUtc(DateTime? value)
        {
            return value.HasValue
                ? value.Value.ToUniversalTime().ToString(
                    "O",
                    CultureInfo.InvariantCulture)
                : "not_applicable";
        }
    }
}
