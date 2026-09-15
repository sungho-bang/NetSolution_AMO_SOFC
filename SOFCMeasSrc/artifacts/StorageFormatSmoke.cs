using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SOFCMeas;

internal static class StorageFormatSmoke
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

    private static InspectionSample Sample(int number, int rawValue, double kgf)
    {
        return new InspectionSample
        {
            Number = number,
            RawValue = rawValue,
            LoadKgf = kgf,
            ReadTime = new DateTime(2026, 9, 6, 10, 0, number)
        };
    }

    private static BufferedInspectionData Inspection(
        string result,
        params InspectionSample[] samples)
    {
        return new BufferedInspectionData
        {
            Record = new InspectionLogRecord
            {
                Time = new DateTime(2026, 9, 6, 10, 14, 0),
                StartedAt = new DateTime(2026, 9, 6, 10, 13, 0),
                Station = "#1",
                LotNumber = "LOT0601",
                FileName = "SAMPLE_SOFC_1",
                OperatorName = "SAMPLE",
                Result = result,
                LowerSpecKgf = 0.40
            },
            Samples = samples
        };
    }

    private static int Main()
    {
        string testRoot = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "storage-format-output");
        Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", testRoot);
        Directory.CreateDirectory(Path.Combine(testRoot, "conf"));
        File.WriteAllText(
            Path.Combine(testRoot, "conf", "SOFCMeas.config"),
            "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
            "<configuration><appSettings>" +
            "<add key=\"Station1.Plc.LoadScale\" value=\"1000\" />" +
            "</appSettings></configuration>");

        string dataRoot = Path.Combine(testRoot, "Data");
        string logRoot = Path.Combine(testRoot, "Log");
        var service = new InspectionStorageService(dataRoot, logRoot);
        var inspections = new[]
        {
            Inspection(
                "GOOD",
                Sample(1, 10, 0.010),
                Sample(2, 11, 0.011),
                Sample(3, 12, 0.012)),
            Inspection(
                "NG",
                Sample(1, 20, 0.020),
                Sample(2, 21, 0.021))
        };
        var lot = new LotStorageData
        {
            CreatedAt = new DateTime(2026, 9, 6, 10, 0, 0),
            CompletedAt = new DateTime(2026, 9, 6, 10, 14, 0),
            Station = "#1",
            LotNumber = "LOT0601",
            FileName = "SAMPLE_SOFC_1",
            OperatorName = "SAMPLE",
            SpecText = "0.40",
            Inspections = inspections
        };

        string savedPath = service.SaveLot(lot);
        string[] lines = File.ReadAllLines(savedPath);
        Assert(
            lines[0] == "모델명,LOT NO.,작업자,SPEC,생산수량,GOOD,NG,Yield(%)",
            "summary labels on first row");
        Assert(
            lines[1] == "SAMPLE_SOFC_1,LOT0601,SAMPLE,0.40,2,1,1,50.00",
            "summary values on second row");
        Assert(string.IsNullOrEmpty(lines[2]), "blank separator row");
        Assert(
            lines[3] == "NO,판정결과,RAWDATA개수,1,2,3",
            "rawdata header uses maximum sample count");
        Assert(
            lines[4] == "1,GOOD,3,0.010,0.011,0.012",
            "rawdata values stored as kgf");
        Assert(
            lines[5] == "2,NG,2,0.020,0.021,",
            "short rawdata row padded to maximum width");

        var file = new LotFileEntry
        {
            Date = lot.CompletedAt,
            FilePath = savedPath
        };
        List<BufferedInspectionData> loaded = service.ReadLotFile(file, "#1");
        Assert(loaded.Count == 2, "new two-row summary file loads for review");
        Assert(
            loaded[0].Record.FileName == "SAMPLE_SOFC_1" &&
            loaded[0].Record.LotNumber == "LOT0601" &&
            loaded[0].Record.OperatorName == "SAMPLE",
            "summary values restored for review");
        Assert(
            loaded[0].Samples.Select(sample => sample.LoadKgf)
                .SequenceEqual(new[] { 0.010, 0.011, 0.012 }),
            "kgf values restored without a second scale conversion");

        List<InspectionSample> selected = service.ReadSamples(inspections[0].Record);
        Assert(
            selected.Select(sample => sample.LoadKgf)
                .SequenceEqual(new[] { 0.010, 0.011, 0.012 }),
            "individual review reads new kgf row");

        ApplicationConfiguration.SaveAppSettings(new Dictionary<string, string>
        {
            { "Station1.Plc.LoadScale", "1000000000" }
        });
        double negativeFrameKgf;
        Assert(PlcLoadDecoder.TryDecode(new ushort[] { 12546, 8237, 11824, 12596, 816 }, out negativeFrameKgf)
            && negativeFrameKgf == -0.410D, "D701 minus sign decodes to negative image load");
        var asciiInspection = Inspection("GOOD",
            Sample(1, int.MaxValue, 3D),
            Sample(2, int.MinValue, -3D),
            Sample(3, PlcLoadDecoder.ToLegacyRawValue(negativeFrameKgf, 1000000000D), negativeFrameKgf),
            Sample(4, 410000000, 0.410D));
        var asciiLot = new LotStorageData
        {
            CreatedAt = lot.CreatedAt,
            CompletedAt = lot.CompletedAt.AddSeconds(1),
            Station = lot.Station,
            LotNumber = lot.LotNumber,
            FileName = lot.FileName,
            OperatorName = lot.OperatorName,
            SpecText = lot.SpecText,
            Inspections = new[] { asciiInspection }
        };
        string asciiPath = service.SaveLot(asciiLot);
        Assert(File.ReadAllLines(asciiPath)[4] == "1,GOOD,4,3.000,-3.000,-0.410,0.410",
            "ASCII kgf values save unchanged even beyond legacy raw integer range");
        var asciiFile = new LotFileEntry { Date = lot.CompletedAt, FilePath = asciiPath };
        IReadOnlyList<InspectionSample> asciiSamples = service.ReadLotFile(asciiFile, "#1")[0].Samples;
        Assert(asciiSamples.Select(sample => sample.LoadKgf).SequenceEqual(new[] { 3D, -3D, -0.410D, 0.410D }),
            "ASCII kgf review preserves exact values with large internal raw scale");
        Assert(asciiSamples[0].RawValue == int.MaxValue && asciiSamples[1].RawValue == int.MinValue,
            "legacy integer representation saturates without losing kgf samples");
        Assert(service.ReadSamples(asciiInspection.Record).Select(sample => sample.LoadKgf)
            .SequenceEqual(new[] { 3D, -3D, -0.410D, 0.410D }),
            "individual review preserves ASCII kgf beyond legacy raw integer range");

        Console.WriteLine("OUTPUT " + savedPath);
        Console.WriteLine("RESULT failures=" + s_Failures);
        return s_Failures == 0 ? 0 : 1;
    }
}
