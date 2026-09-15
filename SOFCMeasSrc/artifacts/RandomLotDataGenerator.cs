using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SOFCMeas;

internal static class RandomLotDataGenerator
{
    private const int InspectionCountPerLot = 12;
    private const double SpecKgf = 0.40;

    private static InspectionSample[] CreateSamples(
        Random random,
        DateTime startedAt,
        double targetPeak)
    {
        int sampleCount = random.Next(48, 73);
        var samples = new InspectionSample[sampleCount];
        for (int index = 0; index < sampleCount; index++)
        {
            double t = sampleCount == 1 ? 0D : index / (sampleCount - 1D);
            double normalized;
            if (t <= 0.82D)
            {
                double rising = t / 0.82D;
                normalized = rising * rising * (3D - 2D * rising);
            }
            else
            {
                normalized = 1D - 0.14D * ((t - 0.82D) / 0.18D);
            }

            double noise = (random.NextDouble() - 0.5D) * 0.006D;
            double kgf = Math.Max(0D, 0.008D + targetPeak * normalized + noise);
            kgf = Math.Round(kgf, 3, MidpointRounding.AwayFromZero);
            samples[index] = new InspectionSample
            {
                Number = index + 1,
                ReadTime = startedAt.AddSeconds(index),
                RawValue = Convert.ToInt32(Math.Round(
                    kgf * 1000D,
                    MidpointRounding.AwayFromZero)),
                LoadKgf = kgf
            };
        }

        return samples;
    }

    private static BufferedInspectionData CreateInspection(
        Random random,
        string station,
        string model,
        string lotNumber,
        string operatorName,
        DateTime startedAt,
        int inspectionNumber)
    {
        // GOOD와 NG가 날짜/설비마다 섞이도록 목표 최고점을 분산합니다.
        double targetPeak = inspectionNumber % 5 == 0
            ? 0.34D + random.NextDouble() * 0.05D
            : 0.43D + random.NextDouble() * 0.18D;
        InspectionSample[] samples = CreateSamples(random, startedAt, targetPeak);
        double peak = samples.Max(sample => sample.LoadKgf);
        string result = peak >= SpecKgf ? "GOOD" : "NG";
        return new BufferedInspectionData
        {
            Samples = samples,
            Record = new InspectionLogRecord
            {
                Time = startedAt.AddSeconds(samples.Length),
                StartedAt = startedAt,
                Station = station,
                LotNumber = lotNumber,
                FileName = model,
                OperatorName = operatorName,
                Result = result,
                PeakLoadKgf = peak,
                LowerSpecKgf = SpecKgf,
                UpperSpecKgf = SpecKgf,
                SampleCount = samples.Length,
                Message = "Generated sample data"
            }
        };
    }

    private static string CreateLot(
        InspectionStorageService storage,
        Random random,
        int stationNumber,
        DateTime completedAt)
    {
        string station = "#" + stationNumber.ToString(CultureInfo.InvariantCulture);
        string stationCode = stationNumber == 1 ? "A" : "B";
        string model = stationNumber == 1 ? "SOFC_A12" : "SOFC_B07";
        string lotNumber = completedAt.ToString("yyMMdd", CultureInfo.InvariantCulture) +
            "-" + stationCode + "01";
        string operatorName = stationNumber == 1 ? "SAMPLE_1" : "SAMPLE_2";
        DateTime lotStartedAt = completedAt.AddMinutes(-30);
        var inspections = new List<BufferedInspectionData>();
        for (int inspection = 1; inspection <= InspectionCountPerLot; inspection++)
        {
            inspections.Add(CreateInspection(
                random,
                station,
                model,
                lotNumber,
                operatorName,
                lotStartedAt.AddMinutes(inspection * 2),
                inspection));
        }

        return storage.SaveLot(new LotStorageData
        {
            CreatedAt = lotStartedAt,
            CompletedAt = completedAt,
            Station = station,
            LotNumber = lotNumber,
            FileName = model,
            OperatorName = operatorName,
            SpecText = SpecKgf.ToString("0.00", CultureInfo.InvariantCulture),
            Inspections = inspections
        });
    }

    private static int Main()
    {
        const string applicationRoot = @"C:\SOFCMeas";
        Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", applicationRoot);
        string dataRoot = Path.GetFullPath(Path.Combine(applicationRoot, "Data"))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        string[] existingCsvFiles = Directory.Exists(dataRoot)
            ? Directory.GetFiles(dataRoot, "*.csv", SearchOption.AllDirectories)
            : new string[0];
        foreach (string existingCsvFile in existingCsvFiles)
        {
            string fullPath = Path.GetFullPath(existingCsvFile);
            if (!fullPath.StartsWith(dataRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "CSV file is outside the verified data root: " + fullPath);
            }

            File.Delete(fullPath);
        }
        Console.WriteLine("DELETED=" + existingCsvFiles.Length);

        var storage = new InspectionStorageService(
            dataRoot,
            Path.Combine(applicationRoot, "Log"));
        var random = new Random(20260906);
        var outputs = new List<string>();

        for (int day = 3; day <= 6; day++)
        {
            outputs.Add(CreateLot(
                storage,
                random,
                1,
                new DateTime(2026, 9, day, 10, 15 + day, 0)));
            outputs.Add(CreateLot(
                storage,
                random,
                2,
                new DateTime(2026, 9, day, 14, 25 + day, 0)));
        }

        foreach (string output in outputs)
        {
            Console.WriteLine(output);
        }

        string[] createdCsvFiles = Directory.GetFiles(
            dataRoot,
            "*.csv",
            SearchOption.AllDirectories);
        Console.WriteLine("CREATED=" + createdCsvFiles.Length);
        return outputs.Count == 8 && createdCsvFiles.Length == 8 ? 0 : 1;
    }
}
