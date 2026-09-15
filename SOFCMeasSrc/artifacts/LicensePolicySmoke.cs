using System;
using System.Linq;
using System.Net.NetworkInformation;
using SOFCMeas;

internal static class LicensePolicySmoke
{
    private static int s_Failures;

    private static void Assert(bool condition, string name)
    {
        if (condition)
        {
            Console.WriteLine("PASS " + name);
            return;
        }

        s_Failures++;
        Console.WriteLine("FAIL " + name);
    }

    private static int Main()
    {
        Assert(
            LicensePolicy.DurationPolicyLogValue == "PERMANENT",
            "usage duration is permanent");
        Assert(
            LicensePolicy.HasLicensedPhysicalAddress(
                new[] { "F4-4E-FC-22-E3-79" }),
            "licensed MAC with separators");
        Assert(
            LicensePolicy.HasLicensedPhysicalAddress(
                new[] { "f44efc22e379" }),
            "licensed MAC normalized");
        Assert(
            !LicensePolicy.HasLicensedPhysicalAddress(
                new[] { "00-11-22-33-44-55" }),
            "different MAC rejected");
        Assert(
            LicensePolicy.HasMatchingPhysicalAddress(
                new[] { "00-11-22-33-44-55", "AA-BB-CC-DD-EE-FF" },
                new[] { "12-34-56-78-90-AB", "aabbccddeeff" }),
            "any one of multiple registered MAC addresses accepted");
        Assert(
            !LicensePolicy.HasMatchingPhysicalAddress(
                new string[0],
                new[] { "F4-4E-FC-22-E3-79" }),
            "empty registered MAC list rejected");

        DateTime first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert(
            LicensePolicy.EvaluateUsagePeriod(
                first,
                first.AddDays(39),
                first.AddDays(39).AddHours(23)) ==
                LicenseValidationStatus.Valid,
            "before day 40 valid");
        Assert(
            LicensePolicy.EvaluateUsagePeriod(
                first,
                first.AddDays(39),
                first.AddDays(40)) ==
                LicenseValidationStatus.UsagePeriodExpired,
            "day 40 expired");
        Assert(
            LicensePolicy.EvaluateUsagePeriod(
                first,
                first.AddDays(10),
                first.AddDays(9)) ==
                LicenseValidationStatus.SystemClockRollbackDetected,
            "clock rollback rejected");

        string[] localAddresses = NetworkInterface.GetAllNetworkInterfaces()
            .Select(adapter => adapter.GetPhysicalAddress().ToString())
            .ToArray();
        Assert(
            LicensePolicy.HasLicensedPhysicalAddress(localAddresses),
            "licensed MAC present on this PC regardless of connection state");

        LicenseValidationResult validation = LicensePolicy.ValidateAndUpdate();
        Assert(
            validation.IsValid &&
            !validation.FirstRunUtc.HasValue &&
            !validation.ExpirationUtc.HasValue,
            "permanent validation does not apply first-run or expiration dates");

        Console.WriteLine("RESULT failures=" + s_Failures);
        return s_Failures == 0 ? 0 : 1;
    }
}
