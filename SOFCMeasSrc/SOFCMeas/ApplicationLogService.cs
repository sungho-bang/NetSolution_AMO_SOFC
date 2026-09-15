using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace SOFCMeas
{
    internal sealed class ApplicationLogService
    {
        private static readonly Encoding LogEncoding = new UTF8Encoding(false);

        private readonly object m_SyncRoot = new object();
        private readonly string m_RootDirectory;
        private readonly long m_MaximumFileSizeBytes;
        private Exception m_LastError;
        private Exception m_LastFailure;
        private DateTime m_LastFailureTime;
        private long m_FailureCount;
        internal event EventHandler StorageStateChanged;

        internal Exception LastFailure { get { lock (m_SyncRoot) return m_LastFailure; } }
        internal DateTime LastFailureTime { get { lock (m_SyncRoot) return m_LastFailureTime; } }
        internal long FailureCount { get { lock (m_SyncRoot) return m_FailureCount; } }

        internal ApplicationLogService(long maximumFileSizeBytes)
            : this(ApplicationPaths.LogRootDirectory, maximumFileSizeBytes)
        {
        }

        internal ApplicationLogService(string rootDirectory, long maximumFileSizeBytes)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("로그 루트 경로가 비어 있습니다.", nameof(rootDirectory));
            }

            if (maximumFileSizeBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumFileSizeBytes));
            }

            m_RootDirectory = Path.GetFullPath(rootDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            m_MaximumFileSizeBytes = maximumFileSizeBytes;
        }

        internal string RootDirectory
        {
            get { return m_RootDirectory; }
        }

        internal Exception LastError
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return m_LastError;
                }
            }
        }

        internal void Info(string source, string message)
        {
            Write(DateTime.Now, "INFO", source, message, null);
        }

        internal void Warning(string source, string message)
        {
            Write(DateTime.Now, "WARN", source, message, null);
        }

        internal void Warning(string source, string message, Exception exception)
        {
            Write(DateTime.Now, "WARN", source, message, exception);
        }

        internal void Error(string source, string message, Exception exception)
        {
            Write(DateTime.Now, "ERROR", source, message, exception);
        }

        private void Write(
            DateTime logTime,
            string level,
            string source,
            string message,
            Exception exception)
        {
            bool notify = false;
            lock (m_SyncRoot)
            {
                try
                {
                    string directory = ApplicationPaths.GetDateDirectory(
                        m_RootDirectory,
                        logTime);
                    Directory.CreateDirectory(directory);

                    string detail = exception == null
                        ? NormalizeSingleLine(message)
                        : NormalizeSingleLine(message) +
                          " exception=\"" + EscapeValue(exception.ToString()) + "\"";
                    string context = FormatContext(level, source);
                    string line = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0:yyyy-MM-dd HH:mm:ss.fff} {1}{2}{3}",
                        logTime,
                        context,
                        detail,
                        Environment.NewLine);
                    int requiredBytes = LogEncoding.GetByteCount(line);
                    string filePath = FindWritableFilePath(
                        directory,
                        logTime,
                        requiredBytes);

                    using (FileStream stream = new FileStream(
                        filePath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.Read))
                    using (StreamWriter writer = new StreamWriter(stream, LogEncoding))
                    {
                        writer.Write(line);
                    }

                    notify = m_LastError != null;
                    m_LastError = null;
                }
                catch (Exception logError)
                {
                    // 로그 저장 장애가 PLC 수집이나 화면 동작을 중단시키지 않도록 합니다.
                    notify = m_LastError == null;
                    m_LastError = logError;
                    m_LastFailure = logError;
                    m_LastFailureTime = logTime;
                    m_FailureCount++;
                }
            }
            // Never dispatch UI callbacks while holding the logging lock.
            if (notify)
            {
                EventHandler handler = StorageStateChanged;
                if (handler != null)
                {
                    try { handler(this, EventArgs.Empty); }
                    catch (Exception callbackError) { Debug.WriteLine(callbackError); }
                }
            }
        }

        private string FindWritableFilePath(
            string directory,
            DateTime logTime,
            int requiredBytes)
        {
            string dateText = logTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            int index = 1;

            while (true)
            {
                string filePath = Path.Combine(
                    directory,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "SOFCMeas_{0}_{1:000}.log",
                        dateText,
                        index));

                if (!File.Exists(filePath) ||
                    new FileInfo(filePath).Length + requiredBytes <=
                    m_MaximumFileSizeBytes)
                {
                    return filePath;
                }

                index++;
            }
        }

        private static string NormalizeSingleLine(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "-"
                : value.Replace("\r\n", " <= ")
                    .Replace('\r', ' ')
                    .Replace('\n', ' ')
                    .Trim();
        }

        private static string FormatContext(string level, string source)
        {
            string normalizedLevel = NormalizeSingleLine(level);
            string normalizedSource = NormalizeSingleLine(source);
            bool information = string.Equals(
                normalizedLevel,
                "INFO",
                StringComparison.OrdinalIgnoreCase);
            bool application = string.Equals(
                normalizedSource,
                "APPLICATION",
                StringComparison.OrdinalIgnoreCase);

            if (information && application) return string.Empty;
            if (information) return normalizedSource + " ";
            if (application) return normalizedLevel + " ";
            return normalizedLevel + " " + normalizedSource + " ";
        }

        private static string EscapeValue(string value)
        {
            return NormalizeSingleLine(value)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }
    }
}
