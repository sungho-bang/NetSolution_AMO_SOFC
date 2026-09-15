using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SOFCMeas
{
    internal sealed class LogCleanupResult
    {
        internal LogCleanupResult()
        {
            FailureDetails = new List<string>();
        }

        internal DateTime FirstRetainedDate { get; set; }
        internal int DeletedDayDirectoryCount { get; set; }
        internal int DeletedFileCount { get; set; }
        internal int FailedDayDirectoryCount { get; set; }
        internal List<string> FailureDetails { get; private set; }
    }

    internal sealed class LogFolderCleanupService
    {
        private readonly string m_RootDirectory;
        private readonly string m_RootDirectoryPrefix;
        private readonly string m_ProtectedDataDirectory;

        internal LogFolderCleanupService(
            string logRootDirectory,
            string protectedDataDirectory)
        {
            if (string.IsNullOrWhiteSpace(logRootDirectory))
            {
                throw new ArgumentException(
                    "로그 정리 대상 경로가 비어 있습니다.",
                    nameof(logRootDirectory));
            }

            if (string.IsNullOrWhiteSpace(protectedDataDirectory))
            {
                throw new ArgumentException(
                    "보호할 데이터 경로가 비어 있습니다.",
                    nameof(protectedDataDirectory));
            }

            m_RootDirectory = NormalizeDirectory(logRootDirectory);
            m_ProtectedDataDirectory = NormalizeDirectory(protectedDataDirectory);
            if (IsSameOrChildPath(m_RootDirectory, m_ProtectedDataDirectory) ||
                IsSameOrChildPath(m_ProtectedDataDirectory, m_RootDirectory))
            {
                throw new InvalidOperationException(
                    "데이터 폴더 또는 그 상위/하위 경로는 자동 삭제 대상으로 사용할 수 없습니다.");
            }

            m_RootDirectory = Path.GetFullPath(logRootDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            m_RootDirectoryPrefix = m_RootDirectory + Path.DirectorySeparatorChar;
        }

        internal LogCleanupResult DeleteExpiredDateDirectories(
            int retentionDays,
            DateTime today)
        {
            if (retentionDays < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(retentionDays));
            }

            DateTime firstRetainedDate = today.Date.AddDays(-(retentionDays - 1));
            LogCleanupResult result = new LogCleanupResult
            {
                FirstRetainedDate = firstRetainedDate
            };

            foreach (KeyValuePair<DateTime, string> dateDirectory in
                GetDateDirectories().Where(item => item.Key < firstRetainedDate))
            {
                string fullPath = EnsureChildPath(dateDirectory.Value);
                try
                {
                    int fileCount = Directory.EnumerateFiles(
                            fullPath,
                            "*",
                            SearchOption.AllDirectories)
                        .Count();
                    Directory.Delete(fullPath, true);
                    result.DeletedDayDirectoryCount++;
                    result.DeletedFileCount += fileCount;
                }
                catch (IOException exception)
                {
                    result.FailedDayDirectoryCount++;
                    result.FailureDetails.Add(
                        fullPath + " | " + exception.GetType().Name + ": " + exception.Message);
                }
                catch (UnauthorizedAccessException exception)
                {
                    result.FailedDayDirectoryCount++;
                    result.FailureDetails.Add(
                        fullPath + " | " + exception.GetType().Name + ": " + exception.Message);
                }
            }

            DeleteEmptyParentDirectories(result);
            return result;
        }

        private IEnumerable<KeyValuePair<DateTime, string>> GetDateDirectories()
        {
            List<KeyValuePair<DateTime, string>> directories =
                new List<KeyValuePair<DateTime, string>>();

            if (!Directory.Exists(m_RootDirectory))
            {
                return directories;
            }

            foreach (string yearDirectory in Directory.EnumerateDirectories(m_RootDirectory))
            {
                if (IsReparsePoint(yearDirectory))
                {
                    continue;
                }

                string year = Path.GetFileName(yearDirectory);
                if (!IsDigits(year, 4))
                {
                    continue;
                }

                foreach (string monthDirectory in Directory.EnumerateDirectories(yearDirectory))
                {
                    if (IsReparsePoint(monthDirectory))
                    {
                        continue;
                    }

                    string month = Path.GetFileName(monthDirectory);
                    if (!IsDigits(month, 2))
                    {
                        continue;
                    }

                    foreach (string dayDirectory in Directory.EnumerateDirectories(monthDirectory))
                    {
                        if (IsReparsePoint(dayDirectory))
                        {
                            continue;
                        }

                        string day = Path.GetFileName(dayDirectory);
                        DateTime date;
                        if (IsDigits(day, 2) && DateTime.TryParseExact(
                            year + month + day,
                            "yyyyMMdd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out date))
                        {
                            directories.Add(
                                new KeyValuePair<DateTime, string>(date, dayDirectory));
                        }
                    }
                }
            }

            return directories;
        }

        private void DeleteEmptyParentDirectories(LogCleanupResult result)
        {
            if (!Directory.Exists(m_RootDirectory))
            {
                return;
            }

            foreach (string yearDirectory in Directory.EnumerateDirectories(m_RootDirectory))
            {
                if (IsReparsePoint(yearDirectory))
                {
                    continue;
                }

                foreach (string monthDirectory in Directory.EnumerateDirectories(yearDirectory))
                {
                    TryDeleteEmptyDirectory(monthDirectory, result);
                }

                TryDeleteEmptyDirectory(yearDirectory, result);
            }
        }

        private string EnsureChildPath(string directory)
        {
            string fullPath = Path.GetFullPath(directory);
            if (!fullPath.StartsWith(
                m_RootDirectoryPrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("로그 루트 밖의 경로는 삭제할 수 없습니다.");
            }

            if (IsSameOrChildPath(fullPath, m_ProtectedDataDirectory))
            {
                throw new InvalidOperationException("데이터 폴더는 자동 삭제할 수 없습니다.");
            }

            return fullPath;
        }

        private static string NormalizeDirectory(string directory)
        {
            return Path.GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsSameOrChildPath(string path, string possibleParent)
        {
            string normalizedPath = NormalizeDirectory(path);
            string normalizedParent = NormalizeDirectory(possibleParent);
            if (string.Equals(
                normalizedPath,
                normalizedParent,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return normalizedPath.StartsWith(
                normalizedParent + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDigits(string value, int length)
        {
            int ignored;
            return value != null &&
                   value.Length == length &&
                   int.TryParse(
                       value,
                       NumberStyles.None,
                       CultureInfo.InvariantCulture,
                       out ignored);
        }

        private static bool IsReparsePoint(string directory)
        {
            return (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0;
        }

        private static void TryDeleteEmptyDirectory(
            string directory,
            LogCleanupResult result)
        {
            try
            {
                if (Directory.Exists(directory) &&
                    !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory, false);
                }
            }
            catch (IOException exception)
            {
                result.FailureDetails.Add(
                    directory + " | " + exception.GetType().Name + ": " + exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                result.FailureDetails.Add(
                    directory + " | " + exception.GetType().Name + ": " + exception.Message);
            }
        }
    }
}
