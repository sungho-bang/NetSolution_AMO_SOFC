using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace SOFCMeas
{
    internal sealed class ApplicationLogService
    {
        private static readonly Encoding LogEncoding = new UTF8Encoding(false);
        private static readonly string SessionId =
            Guid.NewGuid().ToString("N").Substring(0, 12);
        private static readonly int ProcessId = Process.GetCurrentProcess().Id;
        private static long s_LogSequence;

        private readonly object m_SyncRoot = new object();
        private readonly string m_RootDirectory;
        private readonly long m_MaximumFileSizeBytes;
        private Exception m_LastError;

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

        internal static string CurrentSessionId
        {
            get { return SessionId; }
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
                    string line = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0:yyyy-MM-dd HH:mm:ss.fff zzz} " +
                        "[SESSION={1}] [SEQ={2}] [PID={3}] [TID={4}] [{5}] [{6}] {7}{8}",
                        logTime,
                        SessionId,
                        Interlocked.Increment(ref s_LogSequence),
                        ProcessId,
                        Thread.CurrentThread.ManagedThreadId,
                        level,
                        NormalizeSingleLine(source),
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

                    m_LastError = null;
                }
                catch (Exception logError)
                {
                    // 로그 저장 장애가 PLC 수집이나 화면 동작을 중단시키지 않도록 합니다.
                    m_LastError = logError;
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

        private static string EscapeValue(string value)
        {
            return NormalizeSingleLine(value)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }
    }
}
