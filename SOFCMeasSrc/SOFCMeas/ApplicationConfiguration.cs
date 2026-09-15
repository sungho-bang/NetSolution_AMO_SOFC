using System;
using System.Collections.Generic;
using System.Configuration;

namespace SOFCMeas
{
    internal static class ApplicationConfiguration
    {
        private static readonly object s_SyncRoot = new object();

        internal static KeyValueConfigurationCollection LoadAppSettings()
        {
            return OpenConfiguration().AppSettings.Settings;
        }

        internal static void SaveAppSettings(
            IEnumerable<KeyValuePair<string, string>> values, bool removeLegacyPlc = false)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            lock (s_SyncRoot)
            {
                Configuration configuration = OpenConfiguration();
                if (removeLegacyPlc)
                    foreach (string key in configuration.AppSettings.Settings.AllKeys)
                        if (key.StartsWith("Plc.")) configuration.AppSettings.Settings.Remove(key);
                foreach (KeyValuePair<string, string> value in values)
                {
                    KeyValueConfigurationElement setting =
                        configuration.AppSettings.Settings[value.Key];
                    if (setting == null)
                    {
                        configuration.AppSettings.Settings.Add(value.Key, value.Value);
                    }
                    else
                    {
                        setting.Value = value.Value;
                    }
                }

                configuration.Save(ConfigurationSaveMode.Modified);
            }
        }

        private static Configuration OpenConfiguration()
        {
            ExeConfigurationFileMap configurationFileMap =
                new ExeConfigurationFileMap
                {
                    ExeConfigFilename = ApplicationPaths.ConfigurationFilePath
                };

            return ConfigurationManager.OpenMappedExeConfiguration(
                configurationFileMap,
                ConfigurationUserLevel.None);
        }
    }
}
