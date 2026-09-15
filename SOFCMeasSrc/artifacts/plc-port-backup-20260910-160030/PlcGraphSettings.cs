using System;
using System.Configuration;
using System.Globalization;

namespace SOFCMeas
{
    internal sealed class PlcGraphSettings
    {
        private static KeyValueConfigurationCollection s_AppSettings = LoadAppSettings();

        private PlcGraphSettings()
        {
        }

        internal int StationNumber { get; private set; }
        internal string IpAddress { get; private set; }
        internal int Port { get; private set; }
        internal bool IsAscii { get; private set; }
        internal bool IsUdp { get; private set; }
        internal McpXLib.Enums.RequestFrame Frame { get; private set; }
        internal McpXLib.Enums.ProcessorSeries Processor { get; private set; }
        internal string RemotePassword { get; private set; }
        internal int PollIntervalMilliseconds { get; private set; }
        internal int ReconnectDelayMilliseconds { get; private set; }
        internal int TimeoutMilliseconds { get; private set; }
        internal double LoadScale { get; private set; }
        internal int MaximumGraphPoints { get; private set; }
        internal bool AutoConnect { get; private set; }

        internal string RequestDevice { get; private set; }
        internal ushort StartRequestValue { get; private set; }
        internal ushort EndRequestValue { get; private set; }
        internal string LoadRawDevice { get; private set; }

        internal static PlcGraphSettings Load(int stationNumber, string defaultIpAddress)
        {
            if (stationNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stationNumber));
            }

            s_AppSettings = LoadAppSettings();
            string stationPrefix = "Station" + stationNumber.ToString(CultureInfo.InvariantCulture) + ".";

            var result = new PlcGraphSettings
            {
                StationNumber = stationNumber,
                IpAddress = ReadString(stationPrefix + "IpAddress", defaultIpAddress),
                Port = ReadInt(stationPrefix + "Plc.Port", 10000, 1, 65535),
                IsAscii = ReadString(stationPrefix + "Plc.Encoding", "Binary") == "ASCII",
                IsUdp = ReadString(stationPrefix + "Plc.Transport", "TCP") == "UDP",
                Frame = ReadString(stationPrefix + "Plc.Frame", "3E") == "4E" ? McpXLib.Enums.RequestFrame.E4 : McpXLib.Enums.RequestFrame.E3,
                Processor = ReadString(stationPrefix + "Plc.Processor", "Q") == "iQR" ? McpXLib.Enums.ProcessorSeries.iQR : McpXLib.Enums.ProcessorSeries.Q,
                RemotePassword = ReadString(stationPrefix + "Plc.RemotePassword", null),
                PollIntervalMilliseconds = ReadInt(stationPrefix + "Plc.PollIntervalMs", 1000, 100, 5000),
                ReconnectDelayMilliseconds = ReadInt(stationPrefix + "Plc.ReconnectDelayMs", 2000, 100, 60000),
                TimeoutMilliseconds = ReadInt(stationPrefix + "Plc.TimeoutMs", 3000, 100, ushort.MaxValue),
                LoadScale = ReadDouble(stationPrefix + "Plc.LoadScale", 1000D, 0.000001D),
                MaximumGraphPoints = ReadInt(stationPrefix + "Plc.MaxGraphPoints", 0, 0, 1000000),
                AutoConnect = ReadBool(stationPrefix + "Plc.AutoConnect", true),
                RequestDevice = ReadString(stationPrefix + "Plc.RequestDevice", "D810"),
                StartRequestValue = checked((ushort)ReadInt(
                    stationPrefix + "Plc.StartRequestValue",
                    1,
                    ushort.MinValue,
                    ushort.MaxValue)),
                EndRequestValue = checked((ushort)ReadInt(
                    stationPrefix + "Plc.EndRequestValue",
                    0,
                    ushort.MinValue,
                    ushort.MaxValue)),
                LoadRawDevice = ReadString(stationPrefix + "Plc.LoadRawDevice", "D700")
            };
            if (result.StartRequestValue == result.EndRequestValue)
                throw new ConfigurationErrorsException(stationPrefix + "START 요청값과 END 요청값은 달라야 합니다.");
            return result;
        }

        private static string ReadString(string key, string defaultValue)
        {
            string value = ReadSetting(key);
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        private static int ReadInt(string key, int defaultValue, int minimum, int maximum)
        {
            int value;
            if (!SettingsCatalog.TryReadInteger(ReadSetting(key), minimum, maximum, out value))
            {
                return defaultValue;
            }

            return value;
        }

        private static double ReadDouble(string key, double defaultValue, double minimum)
        {
            double value;
            if (!double.TryParse(
                    ReadSetting(key),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value < minimum)
            {
                return defaultValue;
            }

            return value;
        }

        private static bool ReadBool(string key, bool defaultValue)
        {
            bool value;
            return bool.TryParse(ReadSetting(key), out value)
                ? value
                : defaultValue;
        }

        private static string ReadSetting(string key)
        {
            KeyValueConfigurationElement setting = s_AppSettings[key];
            if (setting == null && key.StartsWith("Station") && key.Contains(".Plc."))
                setting = s_AppSettings[key.Substring(key.IndexOf('.') + 1)];
            return setting == null ? null : setting.Value;
        }

        private static KeyValueConfigurationCollection LoadAppSettings()
        {
            return ApplicationConfiguration.LoadAppSettings();
        }
    }
}

