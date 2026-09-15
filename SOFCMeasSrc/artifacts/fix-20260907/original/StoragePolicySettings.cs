using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;

namespace SOFCMeas
{
    internal sealed class StoragePolicySettings
    {
        private const int DefaultLogRetentionDays = 30;
        private const int DefaultMaximumLogFileSizeMb = 20;

        internal int LogRetentionDays { get; private set; }
        internal bool LogAutoDelete { get; private set; }
        internal int MaximumLogFileSizeMb { get; private set; }

        internal long MaximumLogFileSizeBytes
        {
            get { return MaximumLogFileSizeMb * 1024L * 1024L; }
        }

        internal static StoragePolicySettings Load()
        {
            KeyValueConfigurationCollection settings =
                ApplicationConfiguration.LoadAppSettings();

            return new StoragePolicySettings
            {
                LogRetentionDays = ReadInt(
                    settings,
                    "Log.RetentionDays",
                    DefaultLogRetentionDays,
                    1,
                    3650),
                LogAutoDelete = ReadBool(settings, "Log.AutoDelete", true),
                MaximumLogFileSizeMb = ReadInt(
                    settings,
                    "Log.MaximumFileSizeMb",
                    DefaultMaximumLogFileSizeMb,
                    1,
                    1024)
            };
        }

        internal void SaveLogRetentionPolicy(int retentionDays, bool autoDelete)
        {
            if (retentionDays < 1 || retentionDays > 3650)
            {
                throw new ArgumentOutOfRangeException(nameof(retentionDays));
            }

            ApplicationConfiguration.SaveAppSettings(
                new[]
                {
                    new KeyValuePair<string, string>(
                        "Log.RetentionDays",
                        retentionDays.ToString(CultureInfo.InvariantCulture)),
                    new KeyValuePair<string, string>(
                        "Log.AutoDelete",
                        autoDelete.ToString().ToLowerInvariant())
                });

            LogRetentionDays = retentionDays;
            LogAutoDelete = autoDelete;
        }

        private static string ReadValue(
            KeyValueConfigurationCollection settings,
            string key)
        {
            KeyValueConfigurationElement setting = settings[key];
            return setting == null ? null : setting.Value;
        }

        private static int ReadInt(
            KeyValueConfigurationCollection settings,
            string key,
            int defaultValue,
            int minimum,
            int maximum)
        {
            int value;
            return int.TryParse(
                       ReadValue(settings, key),
                       NumberStyles.Integer,
                       CultureInfo.InvariantCulture,
                       out value) &&
                   value >= minimum &&
                   value <= maximum
                ? value
                : defaultValue;
        }

        private static bool ReadBool(
            KeyValueConfigurationCollection settings,
            string key,
            bool defaultValue)
        {
            bool value;
            return bool.TryParse(ReadValue(settings, key), out value)
                ? value
                : defaultValue;
        }
    }
}
