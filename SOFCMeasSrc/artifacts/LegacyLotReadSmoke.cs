using System;
using System.Collections;
using System.IO;
using System.Reflection;

internal static class LegacyLotReadSmoke
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.NonPublic;

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

    private static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            throw new ArgumentException("application assembly and legacy CSV are required");
        }

        string root = Path.Combine(
            Path.GetTempPath(),
            "SOFC-LegacyLot-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", root);
        string folder = Path.Combine(root, "Data", "PLC1", "2026", "09", "03");
        Directory.CreateDirectory(folder);
        string copy = Path.Combine(folder, "legacy.csv");
        File.Copy(args[1], copy);

        Assembly assembly = Assembly.LoadFrom(args[0]);
        Type storageType = assembly.GetType("SOFCMeas.InspectionStorageService", true);
        Type fileType = assembly.GetType("SOFCMeas.LotFileEntry", true);
        object storage = Activator.CreateInstance(storageType, true);
        object file = Activator.CreateInstance(fileType, true);
        Set(file, "Date", new DateTime(2026, 9, 3));
        Set(file, "FilePath", copy);

        IList inspections = (IList)storageType
            .GetMethod("ReadLotFile", InstanceFlags)
            .Invoke(storage, new[] { file, "#1" });
        if (inspections.Count != 12)
        {
            throw new InvalidOperationException("Legacy inspection count");
        }

        IList samples = (IList)Get(inspections[0], "Samples");
        if (samples.Count != 60)
        {
            throw new InvalidOperationException("Legacy sample count");
        }

        Console.WriteLine("PASS: legacy LOT CSV remains readable.");
    }
}
