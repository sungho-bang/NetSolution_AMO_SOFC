using System;
using System.IO;
using System.Reflection;

internal static class PlcEndpointSmoke
{
    private const BindingFlags Flags =
        BindingFlags.Instance | BindingFlags.Static |
        BindingFlags.Public | BindingFlags.NonPublic;

    private static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: PlcEndpointSmoke.exe <application-bin>");
            return 2;
        }

        string bin = Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e)
        {
            string dependency = Path.Combine(bin, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(dependency) ? Assembly.LoadFrom(dependency) : null;
        };

        Assembly app = Assembly.LoadFrom(Path.Combine(bin, "SOFCMeas.exe"));
        Type settingsType = app.GetType("SOFCMeas.PlcGraphSettings", true);
        MethodInfo load = settingsType.GetMethod("Load", Flags);
        for (int station = 1; station <= 2; station++)
        {
            string expectedIp = station == 1 ? "172.20.9.100" : "172.20.9.101";
            object settings = load.Invoke(null, new object[] { station, expectedIp });
            string ip = (string)settingsType.GetProperty("IpAddress", Flags).GetValue(settings, null);
            int port = (int)settingsType.GetProperty("Port", Flags).GetValue(settings, null);
            bool autoConnect = (bool)settingsType.GetProperty("AutoConnect", Flags).GetValue(settings, null);
            if (ip != expectedIp || port != 5000 || !autoConnect)
            {
                Console.Error.WriteLine(
                    "FAIL station={0} ip={1} port={2} autoConnect={3}",
                    station, ip, port, autoConnect);
                return 1;
            }
            Console.WriteLine(
                "PASS station={0} ip={1} port={2} autoConnect={3}",
                station, ip, port, autoConnect);
        }
        return 0;
    }
}
