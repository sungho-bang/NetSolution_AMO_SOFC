using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace SOFCMeas
{
    internal enum LicenseValidationStatus
    {
        Valid,
        LicensedAdapterNotFound,
        UsagePeriodExpired,
        SystemClockRollbackDetected,
        LicenseStateInvalid,
        LicenseStateUnavailable
    }

    internal sealed class LicenseValidationResult
    {
        internal LicenseValidationResult(
            LicenseValidationStatus status,
            string message,
            DateTime? firstRunUtc,
            DateTime? expirationUtc)
        {
            Status = status;
            Message = message;
            FirstRunUtc = firstRunUtc;
            ExpirationUtc = expirationUtc;
        }

        internal LicenseValidationStatus Status { get; private set; }
        internal string Message { get; private set; }
        internal DateTime? FirstRunUtc { get; private set; }
        internal DateTime? ExpirationUtc { get; private set; }
        internal bool IsValid { get { return Status == LicenseValidationStatus.Valid; } }
    }

    /// <summary>
    /// SOFCMeas 실행 장비와 사용 기간을 확인합니다.
    /// UI나 PLC 코드와 분리해 영구 버전 전환 시 정책만 변경할 수 있게 합니다.
    /// </summary>
    internal static class LicensePolicy
    {
        // 허용할 MAC Address를 아래 배열에 계속 추가합니다.
        // 등록된 항목 중 PC의 어댑터 하나와 일치하면 실행을 허용합니다.
        private static readonly string[] LicensedPhysicalAddresses =
        {
            "F4-4E-FC-22-E3-79",
            "30-9C-23-92-2E-1B",
            "FC-9D-05-81-C2-B0"
            
            // 예: "00-11-22-33-44-55",
        };

        // 허용 MAC 목록을 늘려도 기존 40일 상태의 서명이 바뀌지 않게 하는 고정값입니다.
        private const string LicenseStateBinding = "F44EFC22E379";
        private const int UsagePeriodDays = 40;

        // 사용 기간은 제한하지 않습니다. MAC 장비 잠금은 계속 유지됩니다.
        private static readonly bool PermanentDuration = true;

        private const string RegistryPath = @"Software\NetSolution\SOFCMeas\License";
        private const string FirstRunValueName = "FirstRunUtcTicks";
        private const string LastRunValueName = "LastRunUtcTicks";
        private const string SealValueName = "StateSeal";
        private const string SealKey =
            "SOFCMeas.NetSolution.License.State.2026.F4-4E-FC-22-E3-79";
        private static readonly TimeSpan ClockRollbackTolerance = TimeSpan.FromMinutes(5);

        internal static string DurationPolicyLogValue
        {
            get { return PermanentDuration ? "PERMANENT" : "40_DAYS"; }
        }

        internal static LicenseValidationResult ValidateAndUpdate()
        {
            try
            {
                if (!HasLicensedPhysicalAddress(GetPhysicalAddresses()))
                {
                    return Failure(
                        LicenseValidationStatus.LicensedAdapterNotFound,
                        "You do not have permission to use this software.\r\n" +
                        "The program will now close.");
                }

                if (PermanentDuration)
                {
                    return Success(null, null);
                }

                return ValidateAndUpdateUsagePeriod(DateTime.UtcNow);
            }
            catch (Exception)
            {
                return Failure(
                    LicenseValidationStatus.LicenseStateUnavailable,
                    "The software authorization could not be verified.\r\n" +
                    "The program will now close.");
            }
        }

        internal static bool HasLicensedPhysicalAddress(IEnumerable<string> addresses)
        {
            if (addresses == null)
            {
                return false;
            }

            return HasMatchingPhysicalAddress(
                LicensedPhysicalAddresses,
                addresses);
        }

        internal static bool HasMatchingPhysicalAddress(
            IEnumerable<string> registeredAddresses,
            IEnumerable<string> machineAddresses)
        {
            if (registeredAddresses == null || machineAddresses == null)
            {
                return false;
            }

            var registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string registeredAddress in registeredAddresses)
            {
                string normalized = NormalizePhysicalAddress(registeredAddress);
                if (!string.IsNullOrEmpty(normalized))
                {
                    registered.Add(normalized);
                }
            }

            foreach (string machineAddress in machineAddresses)
            {
                if (registered.Contains(NormalizePhysicalAddress(machineAddress)))
                {
                    return true;
                }
            }

            return false;
        }

        internal static LicenseValidationStatus EvaluateUsagePeriod(
            DateTime firstRunUtc,
            DateTime lastRunUtc,
            DateTime nowUtc)
        {
            DateTime first = firstRunUtc.ToUniversalTime();
            DateTime last = lastRunUtc.ToUniversalTime();
            DateTime now = nowUtc.ToUniversalTime();

            DateTime lastWithTolerance = last > DateTime.MaxValue - ClockRollbackTolerance
                ? DateTime.MaxValue
                : last.Add(ClockRollbackTolerance);
            DateTime lastWithoutTolerance = last < DateTime.MinValue + ClockRollbackTolerance
                ? DateTime.MinValue
                : last.Subtract(ClockRollbackTolerance);
            if (first > lastWithTolerance || now < lastWithoutTolerance)
            {
                return LicenseValidationStatus.SystemClockRollbackDetected;
            }

            DateTime expiration;
            try
            {
                expiration = first.AddDays(UsagePeriodDays);
            }
            catch (ArgumentOutOfRangeException)
            {
                return LicenseValidationStatus.LicenseStateInvalid;
            }

            return now >= expiration
                ? LicenseValidationStatus.UsagePeriodExpired
                : LicenseValidationStatus.Valid;
        }

        private static IEnumerable<string> GetPhysicalAddresses()
        {
            var addresses = new List<string>();
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 연결 여부로 거르지 않습니다. Bluetooth 등 현재 연결되지 않은
                // 어댑터도 이 장비의 물리 주소로 인정합니다.
                PhysicalAddress address = adapter.GetPhysicalAddress();
                if (address != null)
                {
                    string text = address.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        addresses.Add(text);
                    }
                }
            }

            return addresses;
        }

        private static LicenseValidationResult ValidateAndUpdateUsagePeriod(DateTime nowUtc)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath, true))
                {
                    if (key == null)
                    {
                        return StateUnavailable();
                    }

                    object firstValue = key.GetValue(FirstRunValueName, null);
                    object lastValue = key.GetValue(LastRunValueName, null);
                    object sealValue = key.GetValue(SealValueName, null);

                    bool stateIsMissing = firstValue == null &&
                        lastValue == null && sealValue == null;
                    if (stateIsMissing)
                    {
                        long nowTicks = nowUtc.Ticks;
                        WriteState(key, nowTicks, nowTicks);
                        DateTime expiration = nowUtc.AddDays(UsagePeriodDays);
                        return Success(nowUtc, expiration);
                    }

                    if (firstValue == null || lastValue == null || sealValue == null)
                    {
                        return StateInvalid();
                    }

                    long firstTicks;
                    long lastTicks;
                    try
                    {
                        firstTicks = Convert.ToInt64(firstValue, CultureInfo.InvariantCulture);
                        lastTicks = Convert.ToInt64(lastValue, CultureInfo.InvariantCulture);
                    }
                    catch (Exception exception) when (
                        exception is FormatException ||
                        exception is InvalidCastException ||
                        exception is OverflowException)
                    {
                        return StateInvalid();
                    }

                    string storedSeal = Convert.ToString(
                        sealValue,
                        CultureInfo.InvariantCulture);
                    if (!IsValidStateSeal(firstTicks, lastTicks, storedSeal))
                    {
                        return StateInvalid();
                    }

                    DateTime firstRunUtc;
                    DateTime lastRunUtc;
                    try
                    {
                        firstRunUtc = new DateTime(firstTicks, DateTimeKind.Utc);
                        lastRunUtc = new DateTime(lastTicks, DateTimeKind.Utc);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return StateInvalid();
                    }

                    LicenseValidationStatus status = EvaluateUsagePeriod(
                        firstRunUtc,
                        lastRunUtc,
                        nowUtc);
                    DateTime expirationUtc = firstRunUtc.AddDays(UsagePeriodDays);
                    if (status == LicenseValidationStatus.UsagePeriodExpired)
                    {
                        return Failure(
                            status,
                            "The software usage period has expired.\r\n" +
                            "The program will now close.",
                            firstRunUtc,
                            expirationUtc);
                    }

                    if (status == LicenseValidationStatus.SystemClockRollbackDetected)
                    {
                        return Failure(
                            status,
                            "The software authorization could not be verified " +
                            "because the system clock was changed.\r\n" +
                            "The program will now close.",
                            firstRunUtc,
                            expirationUtc);
                    }

                    if (status != LicenseValidationStatus.Valid)
                    {
                        return StateInvalid();
                    }

                    long updatedLastTicks = Math.Max(lastTicks, nowUtc.Ticks);
                    WriteState(key, firstTicks, updatedLastTicks);
                    return Success(firstRunUtc, expirationUtc);
                }
            }
            catch (Exception)
            {
                return StateUnavailable();
            }
        }

        private static void WriteState(RegistryKey key, long firstTicks, long lastTicks)
        {
            key.SetValue(FirstRunValueName, firstTicks, RegistryValueKind.QWord);
            key.SetValue(LastRunValueName, lastTicks, RegistryValueKind.QWord);
            key.SetValue(
                SealValueName,
                CreateStateSeal(firstTicks, lastTicks),
                RegistryValueKind.String);
            key.Flush();
        }

        private static string CreateStateSeal(long firstTicks, long lastTicks)
        {
            string payload = string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1}|{2}",
                firstTicks,
                lastTicks,
                LicenseStateBinding);

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SealKey)))
            {
                return Convert.ToBase64String(
                    hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            }
        }

        private static bool IsValidStateSeal(
            long firstTicks,
            long lastTicks,
            string storedSeal)
        {
            if (string.IsNullOrEmpty(storedSeal))
            {
                return false;
            }

            byte[] expected;
            byte[] actual;
            try
            {
                expected = Convert.FromBase64String(
                    CreateStateSeal(firstTicks, lastTicks));
                actual = Convert.FromBase64String(storedSeal);
            }
            catch (FormatException)
            {
                return false;
            }

            if (expected.Length != actual.Length)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < expected.Length; i++)
            {
                difference |= expected[i] ^ actual[i];
            }

            return difference == 0;
        }

        private static string NormalizePhysicalAddress(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = new StringBuilder(12);
            foreach (char character in value)
            {
                if (Uri.IsHexDigit(character))
                {
                    normalized.Append(char.ToUpperInvariant(character));
                }
            }

            return normalized.Length == 12 ? normalized.ToString() : string.Empty;
        }

        private static LicenseValidationResult Success(
            DateTime? firstRunUtc,
            DateTime? expirationUtc)
        {
            return new LicenseValidationResult(
                LicenseValidationStatus.Valid,
                string.Empty,
                firstRunUtc,
                expirationUtc);
        }

        private static LicenseValidationResult Failure(
            LicenseValidationStatus status,
            string message,
            DateTime? firstRunUtc = null,
            DateTime? expirationUtc = null)
        {
            return new LicenseValidationResult(
                status,
                message,
                firstRunUtc,
                expirationUtc);
        }

        private static LicenseValidationResult StateInvalid()
        {
            return Failure(
                LicenseValidationStatus.LicenseStateInvalid,
                "The software authorization information is invalid.\r\n" +
                "The program will now close.");
        }

        private static LicenseValidationResult StateUnavailable()
        {
            return Failure(
                LicenseValidationStatus.LicenseStateUnavailable,
                "The software authorization information could not be read or saved.\r\n" +
                "The program will now close.");
        }
    }
}
