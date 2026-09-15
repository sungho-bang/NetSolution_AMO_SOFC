using System;
using System.IO;

namespace SOFCMeas
{
    internal static class LogCleanupScopeSmoke
    {
        private static int Main()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "SOFCMeas-LogCleanupScope-" + Guid.NewGuid().ToString("N"));
            string logRoot = Path.Combine(root, "Log");
            string dataRoot = Path.Combine(root, "Data");
            string expiredLog = Path.Combine(logRoot, "2026", "01", "01");
            string expiredData = Path.Combine(dataRoot, "2026", "01", "01");

            try
            {
                Directory.CreateDirectory(expiredLog);
                Directory.CreateDirectory(expiredData);
                File.WriteAllText(Path.Combine(expiredLog, "old.log"), "log");
                File.WriteAllText(Path.Combine(expiredData, "inspection.csv"), "data");

                var service = new LogFolderCleanupService(logRoot, dataRoot);
                LogCleanupResult result = service.DeleteExpiredDateDirectories(
                    30,
                    new DateTime(2026, 9, 7));

                if (Directory.Exists(expiredLog) || result.DeletedFileCount != 1)
                {
                    Console.Error.WriteLine("Expired log was not deleted.");
                    return 1;
                }

                if (!File.Exists(Path.Combine(expiredData, "inspection.csv")))
                {
                    Console.Error.WriteLine("Inspection data was deleted.");
                    return 2;
                }

                bool dataRootRejected = false;
                try
                {
                    new LogFolderCleanupService(dataRoot, dataRoot);
                }
                catch (InvalidOperationException)
                {
                    dataRootRejected = true;
                }

                if (!dataRootRejected)
                {
                    Console.Error.WriteLine("Data root was accepted as cleanup root.");
                    return 3;
                }

                Console.WriteLine(
                    "PASS deletedLogFiles={0} dataFilePreserved=true dataRootRejected=true",
                    result.DeletedFileCount);
                return 0;
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }
}
