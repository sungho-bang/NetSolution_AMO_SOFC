using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace SOFCMeas
{
    internal sealed class SettingEntry
    {
        internal string Key, Name, Default, Type;
        internal double Min, Max;
        internal SettingEntry(string key, string name, string value, string type, double min = 0, double max = 0)
        { Key = key; Name = name; Default = value; Type = type; Min = min; Max = max; }
    }
    internal static class SettingsCatalog
    {
        internal static List<SettingEntry> Entries()
        {
            var entries = new List<SettingEntry> {
                new SettingEntry("Runtime.Simulation", "시뮬레이션 (0: PLC / 1: 가상 입력)", "0", "enum:0|1"),
                new SettingEntry("Security.AdminPassword", "관리자 비밀번호", "1234", "password"),
                new SettingEntry("Log.RetentionDays", "로그 보존일", "30", "int", 1, 3650),
                new SettingEntry("Log.AutoDelete", "로그 자동 삭제", "true", "bool"),
                new SettingEntry("Log.MaximumFileSizeMb", "로그 파일 크기(MB)", "20", "int", 1, 1024)
            };
            var plc = new[] {
                new SettingEntry("Plc.AutoConnect", "PLC 자동 연결", "true", "bool"),
                new SettingEntry("Plc.Port", "PLC 포트", "10000", "int", 1, 65535),
                new SettingEntry("Plc.Encoding", "통신 코드", "Binary", "enum:Binary|ASCII"),
                new SettingEntry("Plc.Transport", "전송 방식", "TCP", "enum:TCP|UDP"),
                new SettingEntry("Plc.Frame", "MC 프레임", "3E", "enum:3E|4E"),
                new SettingEntry("Plc.Processor", "PLC 시리즈 (Q: Q/L, iQR: iQ-R)", "Q", "enum:Q|iQR"),
                new SettingEntry("Plc.RemotePassword", "PLC 원격 비밀번호 (미사용: 공란)", "", "optionalPassword"),
                new SettingEntry("Plc.PollIntervalMs", "읽기 주기(ms)", "1000", "int", 100, 5000),
                new SettingEntry("Plc.ReconnectDelayMs", "재연결 간격(ms)", "2000", "int", 100, 60000),
                new SettingEntry("Plc.TimeoutMs", "통신 Timeout(ms)", "3000", "int", 100, 65535),
                new SettingEntry("Plc.LoadScale", "내부·시뮬레이션 원시값 배율 (kgf × 값)", "1000", "number", 0.000001, 1000000000),
                new SettingEntry("Plc.MaxGraphPoints", "최대 그래프 점 수 (0: 전체)", "0", "int", 0, 1000000),
                new SettingEntry("Plc.RequestDevice", "요청 디바이스", "D810", "device"),
                new SettingEntry("Plc.LoadRawDevice", "하중 시작 디바이스 (5 WORD / ASCII)", "D700", "device"),
                new SettingEntry("Plc.StartRequestValue", "START 요청값", "1", "int", 0, 65535),
                new SettingEntry("Plc.EndRequestValue", "END 요청값", "0", "int", 0, 65535)
            };
            for (int station = 1; station <= 2; station++)
            {
                string prefix = "Station" + station + ".";
                entries.Add(new SettingEntry(prefix + "IpAddress", "PLC IP", station == 1 ? "172.20.9.100" : "172.20.9.101", "ip"));
                foreach (var item in plc)
                    entries.Add(new SettingEntry(prefix + item.Key, item.Name, item.Default, item.Type, item.Min, item.Max));
                entries.Add(new SettingEntry(prefix + "Lot.SaveCount", "LOT 검사 LIST 최대 개수", "600", "int", 1, 100000));
                entries.Add(new SettingEntry(prefix + "Inspection.ThresholdKgf", "SPEC 기준값 (kgf, 이상 GOOD)", "0.40", "number", 0, 1000000));
                foreach (var axis in new[] { "X", "Y" })
                {
                    string key = prefix + "Graph." + axis;
                    entries.Add(new SettingEntry(key + "Auto", axis + "축 자동 확장", "true", "bool"));
                    entries.Add(new SettingEntry(key + "Min", axis + "축 최소값", "0", "number", -1000000, 1000000));
                    entries.Add(new SettingEntry(key + "Max", axis + "축 기준 최대값 (자동: +10%)", axis == "X" ? "60" : "0.40", "number", -1000000, 1000000));
                    entries.Add(new SettingEntry(key + "Interval", axis + "축 눈금 간격", axis == "X" ? "6.6" : "0.2", "number", 0.000001, 1000000));
                }
            }
            return entries;
        }
        internal static Dictionary<string, string> Load()
        {
            var config = ApplicationConfiguration.LoadAppSettings();
            var values = new Dictionary<string, string>();
            foreach (KeyValueConfigurationElement item in config) values[item.Key] = item.Value;
            foreach (var entry in Entries())
                if (!values.ContainsKey(entry.Key))
                {
                    string fallback = entry.Key.StartsWith("Station") ? entry.Key.Substring(entry.Key.IndexOf('.') + 1) : entry.Key;
                    values[entry.Key] = values.ContainsKey(fallback) ? values[fallback] : entry.Default;
                }
            // Expand legacy shared PLC defaults before removing the duplicate keys.
            foreach (string key in values.Keys.Where(k => k.StartsWith("Plc.")).ToArray()) values.Remove(key);
            return values;
        }
        internal static void Validate(Dictionary<string, string> values)
        {
            foreach (var entry in Entries())
            {
                string value = values[entry.Key]; double number; bool flag; IPAddress ip;
                bool valid = entry.Type == "password" ? !string.IsNullOrWhiteSpace(value)
                    : entry.Type == "optionalPassword" ? true
                    : entry.Type.StartsWith("enum:") ? entry.Type.Substring(5).Split('|').Contains(value)
                    : entry.Type == "bool" ? bool.TryParse(value, out flag)
                    : entry.Type == "ip" ? IPAddress.TryParse(value, out ip)
                    : entry.Type == "device" ? Regex.IsMatch(value, @"^[Dd][0-9]+$")
                    : double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                        && !double.IsNaN(number) && !double.IsInfinity(number)
                        && number >= entry.Min && number <= entry.Max
                        && (entry.Type != "int" || number == Math.Truncate(number));
                if (!valid) throw new ArgumentException(entry.Key + " : " + entry.Name + " 값이 올바르지 않습니다.");
            }
            for (int station = 1; station <= 2; station++)
            {
                string stationPrefix = "Station" + station + ".Plc.";
                if (Number(values, stationPrefix + "StartRequestValue") ==
                    Number(values, stationPrefix + "EndRequestValue"))
                    throw new ArgumentException(stationPrefix + "START 요청값과 END 요청값은 달라야 합니다.");
                foreach (var axis in new[] { "X", "Y" })
                {
                    string key = "Station" + station + ".Graph." + axis;
                    if (Number(values, key + "Max") <= Number(values, key + "Min"))
                        throw new ArgumentException(key + " : 최대값은 최소값보다 커야 합니다.");
                    if ((Number(values,key+"Max")-Number(values,key+"Min"))/Number(values,key+"Interval") > 10000)
                        throw new ArgumentException(key + " : 눈금 간격이 너무 작습니다.");
                }
            }
            // Use a canonical representation before saving, so the integer readers
            // do not silently replace valid values such as 12345.0 or 1e3.
            foreach (var entry in Entries().Where(e => e.Type == "int"))
                values[entry.Key] = Number(values, entry.Key).ToString("0", CultureInfo.InvariantCulture);
        }

        internal static bool TryReadInteger(string text, int minimum, int maximum, out int value)
        {
            double parsed;
            value = 0;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ||
                double.IsNaN(parsed) || double.IsInfinity(parsed) || parsed < minimum ||
                parsed > maximum || parsed != Math.Truncate(parsed)) return false;
            value = (int)parsed;
            return true;
        }
        internal static double Number(Dictionary<string, string> values, string key)
        { return double.Parse(values[key], CultureInfo.InvariantCulture); }
    }
}
