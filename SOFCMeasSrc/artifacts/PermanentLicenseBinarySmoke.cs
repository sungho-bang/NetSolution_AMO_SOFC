using System;
using System.IO;
using System.Reflection;

internal static class PermanentLicenseBinarySmoke
{
    private const BindingFlags Flags =
        BindingFlags.Static | BindingFlags.Instance |
        BindingFlags.Public | BindingFlags.NonPublic;

    private static int Main(string[] args)
    {
        string bin = Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e)
        {
            string dependency = Path.Combine(bin, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(dependency) ? Assembly.LoadFrom(dependency) : null;
        };

        Assembly app = Assembly.LoadFrom(Path.Combine(bin, "SOFCMeas.exe"));
        Type policy = app.GetType("SOFCMeas.LicensePolicy", true);
        string duration = (string)policy.GetProperty(
            "DurationPolicyLogValue", Flags).GetValue(null, null);
        object result = policy.GetMethod("ValidateAndUpdate", Flags).Invoke(null, null);
        Type resultType = result.GetType();
        bool valid = (bool)resultType.GetProperty("IsValid", Flags).GetValue(result, null);
        object firstRun = resultType.GetProperty("FirstRunUtc", Flags).GetValue(result, null);
        object expiration = resultType.GetProperty("ExpirationUtc", Flags).GetValue(result, null);
        if (duration != "PERMANENT" || !valid || firstRun != null || expiration != null)
        {
            Console.Error.WriteLine("FAIL duration={0} valid={1} firstRun={2} expiration={3}",
                duration, valid, firstRun, expiration);
            return 1;
        }

        Console.WriteLine("PASS duration=PERMANENT valid=true firstRun=null expiration=null");
        return 0;
    }
}
