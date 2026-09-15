using System;
using System.IO;
using System.Linq;
using System.Reflection;

internal static class SimpleLogFormatSmoke
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Write(object logger, string method, params object[] args)
    {
        logger.GetType().GetMethod(method, Flags, null,
            Array.ConvertAll(args, value => value.GetType()), null).Invoke(logger, args);
    }

    private static int Main(string[] args)
    {
        try
        {
            string bin = Path.GetFullPath(args[0]);
            string root = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(root);
            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
            {
                string path = Path.Combine(bin, new AssemblyName(eventArgs.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
            Assembly app = Assembly.LoadFrom(Path.Combine(bin, "SOFCMeas.exe"));
            Type type = app.GetType("SOFCMeas.ApplicationLogService", true);
            object logger = Activator.CreateInstance(
                type,
                Flags,
                null,
                new object[] { root, 1024L * 1024L },
                null);

            Write(logger, "Info", "APPLICATION", "event=APP_STARTED");
            Write(logger, "Info", "STATION-1", "event=CONNECTED");
            Write(logger, "Warning", "APPLICATION", "event=LOW_SPACE");
            Write(logger, "Error", "STATION-2", "event=PLC_FAILED", new Exception("test"));

            string[] lines = Directory.GetFiles(root, "*.log", SearchOption.AllDirectories)
                .SelectMany(File.ReadAllLines)
                .ToArray();
            Check(lines.Length == 4, "four log lines");
            Check(lines[0].EndsWith(" event=APP_STARTED"), "application info has only time and event");
            Check(lines[1].Contains(" STATION-1 event=CONNECTED"), "station info keeps station identity");
            Check(lines[2].Contains(" WARN event=LOW_SPACE"), "application warning keeps severity");
            Check(lines[3].Contains(" ERROR STATION-2 event=PLC_FAILED"), "station error keeps severity and station");
            Check(lines.All(line =>
                !line.Contains("+09:00") &&
                !line.Contains("[SESSION=") &&
                !line.Contains("[SEQ=") &&
                !line.Contains("[PID=") &&
                !line.Contains("[TID=") &&
                !line.Contains("[INFO]") &&
                !line.Contains("[APPLICATION]")),
                "technical metadata removed");

            Console.WriteLine("PASS: " + checks + " concise log format checks");
            foreach (string line in lines) Console.WriteLine(line);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
