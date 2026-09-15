using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using SOFCMeas;

internal static class LotRawMatrixSmoke
{
    private static readonly BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly Assembly ApplicationAssembly =
        typeof(SettingForm).Assembly;

    private static object Create(string typeName)
    {
        return Activator.CreateInstance(
            ApplicationAssembly.GetType("SOFCMeas." + typeName, true),
            true);
    }

    private static void Set(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName, InstanceFlags)
            .SetValue(target, value, null);
    }

    private static object Get(object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName, InstanceFlags)
            .GetValue(target, null);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static object CreateInspection(
        int inspectionNumber,
        string result,
        params int[] rawValues)
    {
        object samplePrototype = Create("InspectionSample");
        Array samples = Array.CreateInstance(samplePrototype.GetType(), rawValues.Length);
        for (int index = 0; index < rawValues.Length; index++)
        {
            object sample = Create("InspectionSample");
            Set(sample, "Number", index + 1);
            Set(sample, "ReadTime", new DateTime(2026, 9, 6, 17, 0, index));
            Set(sample, "RawValue", rawValues[index]);
            Set(sample, "LoadKgf", rawValues[index] / 2000D);
            samples.SetValue(sample, index);
        }

        object record = Create("InspectionLogRecord");
        Set(record, "Time", new DateTime(2026, 9, 6, 17, inspectionNumber, 0));
        Set(record, "StartedAt", new DateTime(2026, 9, 6, 17, inspectionNumber - 1, 0));
        Set(record, "Station", "#2");
        Set(record, "LotNumber", "LOT-77");
        Set(record, "FileName", "MODEL-A");
        Set(record, "OperatorName", "TESTER");
        Set(record, "Result", result);
        Set(record, "PeakLoadKgf", rawValues.Length == 0 ? 0D : rawValues[rawValues.Length - 1] / 2000D);
        Set(record, "LowerSpecKgf", 0.75D);
        Set(record, "SampleCount", rawValues.Length);

        object inspection = Create("BufferedInspectionData");
        Set(inspection, "Record", record);
        Set(inspection, "Samples", samples);
        return inspection;
    }

    private static void Main()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "SOFC-LotRawMatrix-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", root);
        Directory.CreateDirectory(Path.Combine(root, "conf"));
        File.WriteAllText(
            Path.Combine(root, "conf", "SOFCMeas.config"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration><appSettings>" +
            "<add key=\"Station2.Plc.LoadScale\" value=\"2000\"/>" +
            "<add key=\"Station2.Inspection.ThresholdKgf\" value=\"0.75\"/>" +
            "</appSettings></configuration>",
            new UTF8Encoding(false));

        object first = CreateInspection(1, "PASS", 1000, 2000, 3000);
        object second = CreateInspection(2, "NG", 4000);
        Array inspections = Array.CreateInstance(first.GetType(), 2);
        inspections.SetValue(first, 0);
        inspections.SetValue(second, 1);

        object lot = Create("LotStorageData");
        Set(lot, "CreatedAt", new DateTime(2026, 9, 6, 17, 0, 0));
        Set(lot, "CompletedAt", new DateTime(2026, 9, 6, 17, 23, 45));
        Set(lot, "Station", "#2");
        Set(lot, "LotNumber", "LOT-77");
        Set(lot, "FileName", "MODEL-A");
        Set(lot, "OperatorName", "TESTER");
        Set(lot, "SpecText", "0.75");
        Set(lot, "Inspections", inspections);

        object storage = Create("InspectionStorageService");
        MethodInfo saveLot = storage.GetType().GetMethod("SaveLot", InstanceFlags);
        string filePath = (string)saveLot.Invoke(storage, new[] { lot });
        Check(
            Path.GetFileName(filePath) == "20260906_172345_LOT-77_MODEL-A.csv",
            "LOT file name format");

        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
        Check(lines.Length == 3, "CSV line count");
        Check(lines[0] == "NO,판정결과,RAWDATA개수,1,2,3", "CSV header");
        Check(lines[1] == "1,GOOD,3,1000,2000,3000", "First RAWDATA row");
        Check(lines[2] == "2,NG,1,4000,,", "Short RAWDATA row padding");

        object file = Create("LotFileEntry");
        Set(file, "Date", new DateTime(2026, 9, 6));
        Set(file, "FilePath", filePath);
        IList readLot = (IList)storage.GetType()
            .GetMethod("ReadLotFile", InstanceFlags)
            .Invoke(storage, new[] { file, "#2" });
        Check(readLot.Count == 2, "LOT REVIEW inspection count");
        IList firstSamples = (IList)Get(readLot[0], "Samples");
        IList secondSamples = (IList)Get(readLot[1], "Samples");
        Check(firstSamples.Count == 3 && secondSamples.Count == 1, "LOT REVIEW RAWDATA counts");
        Check((int)Get(firstSamples[2], "RawValue") == 3000, "LOT REVIEW raw value");
        Check(
            Math.Abs((double)Get(firstSamples[2], "LoadKgf") - 1.5D) < 0.000001D,
            "Station2 LoadScale conversion");

        object secondRecord = Get(second, "Record");
        IList indexedSamples = (IList)storage.GetType()
            .GetMethod("ReadSamples", InstanceFlags)
            .Invoke(storage, new[] { secondRecord });
        Check(indexedSamples.Count == 1, "Indexed REVIEW sample count");
        Check((int)Get(indexedSamples[0], "RawValue") == 4000, "Indexed REVIEW raw value");

        Console.WriteLine(
            "PASS: date_time_LOT_model filename, maximum-width RAWDATA matrix, " +
            "short-row padding, and Station2 REVIEW round-trip.");
    }
}
