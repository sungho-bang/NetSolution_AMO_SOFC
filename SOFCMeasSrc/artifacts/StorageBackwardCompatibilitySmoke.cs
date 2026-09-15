using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SOFCMeas;

internal static class StorageBackwardCompatibilitySmoke
{
    private static int s_Failures;

    private static void Assert(bool condition, string name)
    {
        Console.WriteLine((condition ? "PASS " : "FAIL ") + name);
        if (!condition)
        {
            s_Failures++;
        }
    }

    private static int Main()
    {
        Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", @"C:\SOFCMeas");
        var service = new InspectionStorageService(
            @"C:\SOFCMeas\Data",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "compatibility-log"));

        var integerMatrix = new LotFileEntry
        {
            Date = new DateTime(2026, 9, 6),
            FilePath = @"C:\SOFCMeas\Data\PLC1\2026\09\06\20260906_174418_260827-A01_SOFC_A12.csv"
        };
        List<BufferedInspectionData> integerRows =
            service.ReadLotFile(integerMatrix, "#1");
        Assert(integerRows.Count == 1, "existing integer RAWDATA matrix loads");
        Assert(
            Math.Abs(integerRows[0].Samples[0].LoadKgf - 0.010) < 0.0000001,
            "existing integer RAWDATA still applies load scale");

        var legacySummary = new LotFileEntry
        {
            Date = new DateTime(2026, 9, 6),
            FilePath = @"C:\SOFCMeas\Data\PLC1\2026\09\06\20260906101400_LOT0601_GOOD.csv"
        };
        List<BufferedInspectionData> legacyRows =
            service.ReadLotFile(legacySummary, "#1");
        Assert(legacyRows.Count == 12, "existing one-row summary file loads");
        Assert(
            Math.Abs(legacyRows[0].Samples[0].LoadKgf - 0.020) < 0.0000001,
            "existing kgf values remain unchanged");
        Assert(
            legacyRows.All(row => row.Record.FileName == "SAMPLE_SOFC_1") &&
            legacyRows.All(row => row.Record.LotNumber == "LOT0601"),
            "existing one-row summary metadata loads");

        Console.WriteLine("RESULT failures=" + s_Failures);
        return s_Failures == 0 ? 0 : 1;
    }
}
