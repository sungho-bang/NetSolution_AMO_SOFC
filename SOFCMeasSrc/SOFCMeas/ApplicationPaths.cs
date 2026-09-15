using System;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace SOFCMeas
{
    internal static class ApplicationPaths
    {
        private static readonly string s_ApplicationRootDirectory =
            ResolveApplicationRootDirectory();

        internal static string ApplicationRootDirectory
        {
            get { return s_ApplicationRootDirectory; }
        }

        internal static string DataRootDirectory
        {
            get { return Path.Combine(s_ApplicationRootDirectory, "Data"); }
        }

        internal static string LogRootDirectory
        {
            get { return Path.Combine(s_ApplicationRootDirectory, "log"); }
        }

        internal static string ConfigurationRootDirectory
        {
            get { return Path.Combine(s_ApplicationRootDirectory, "conf"); }
        }

        internal static string ConfigurationFilePath
        {
            get { return Path.Combine(ConfigurationRootDirectory, "SOFCMeas.config"); }
        }

        internal static void EnsureStorageDirectories()
        {
            Directory.CreateDirectory(DataRootDirectory);
            Directory.CreateDirectory(ConfigurationRootDirectory);
            Directory.CreateDirectory(LogRootDirectory);
        }

        internal static string GetDateDirectory(string rootDirectory, DateTime date)
        {
            return Path.Combine(
                rootDirectory,
                date.ToString("yyyy", CultureInfo.InvariantCulture),
                date.ToString("MM", CultureInfo.InvariantCulture),
                date.ToString("dd", CultureInfo.InvariantCulture));
        }

        private static string ResolveApplicationRootDirectory()
        {
            string environmentRoot = Environment.GetEnvironmentVariable("SOFCMEAS_ROOT");
            string configuredRoot = ConfigurationManager.AppSettings["Storage.RootDirectory"];
            string selectedRoot = string.IsNullOrWhiteSpace(environmentRoot)
                ? configuredRoot
                : environmentRoot;
            if (!string.IsNullOrWhiteSpace(selectedRoot))
            {
                return Path.GetFullPath(selectedRoot.Trim())
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            string assemblyLocation = typeof(ApplicationPaths).Assembly.Location;
            string executableDirectory = string.IsNullOrWhiteSpace(assemblyLocation)
                ? AppDomain.CurrentDomain.BaseDirectory
                : Path.GetDirectoryName(assemblyLocation);

            return Path.GetFullPath(
                    Path.Combine(executableDirectory, ".."))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
