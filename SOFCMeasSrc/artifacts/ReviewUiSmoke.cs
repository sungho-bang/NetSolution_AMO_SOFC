using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

internal static class ReviewUiSmoke
{
    private static int s_Failures;
    private static Assembly s_SofcAssembly;

    private static void Assert(bool condition, string name)
    {
        Console.WriteLine((condition ? "PASS " : "FAIL ") + name);
        if (!condition)
        {
            s_Failures++;
        }
    }

    private static object GetField(object instance, string name)
    {
        return instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
    }

    private static object NewInternal(string typeName, params object[] arguments)
    {
        return Activator.CreateInstance(
            s_SofcAssembly.GetType(typeName, true),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            arguments,
            null);
    }

    private static void SetProperty(object instance, string name, object value)
    {
        instance.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SetValue(instance, value, null);
    }

    private static bool WaitUntil(Func<bool> condition, int timeoutMilliseconds)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (DateTime.UtcNow < deadline)
        {
            Application.DoEvents();
            if (condition())
            {
                return true;
            }
            Thread.Sleep(20);
        }
        return condition();
    }

    [STAThread]
    private static int Main()
    {
        string deploymentBin = @"C:\SOFCMeas\bin";
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
        {
            string dependencyPath = Path.Combine(
                deploymentBin,
                new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(dependencyPath)
                ? Assembly.LoadFrom(dependencyPath)
                : null;
        };

        string testRoot = Path.Combine(
            @"D:\Works_Updates\NetSolution\src\SOFCMeas\artifacts",
            "storage-format-output");
        Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", testRoot);
        s_SofcAssembly = Assembly.LoadFrom(Path.Combine(deploymentBin, "SOFCMeas.exe"));

        string dataFile = Directory.GetFiles(
            Path.Combine(testRoot, "Data", "PLC1", "2026", "09", "06"),
            "20260906_101400_LOT0601_SAMPLE_SOFC_1*.csv")
            .OrderByDescending(path => path)
            .First();

        object storage = NewInternal(
            "SOFCMeas.InspectionStorageService",
            Path.Combine(testRoot, "Data"),
            Path.Combine(testRoot, "Log"));
        object stationView = Activator.CreateInstance(
            s_SofcAssembly.GetType("SOFCMeas.StationView", true));
        stationView.GetType().GetMethod(
            "BindHistory",
            BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(stationView, new[] { storage, "#1" });

        var grid = (DataGridView)GetField(stationView, "historyResults");
        var combo = (ComboBox)GetField(stationView, "cboInspectionNumber");
        var reviewButton = (Button)GetField(stationView, "btnReview");
        object fileEntry = NewInternal("SOFCMeas.LotFileEntry");
        SetProperty(fileEntry, "Date", new DateTime(2026, 9, 6));
        SetProperty(fileEntry, "FilePath", dataFile);
        int rowIndex = grid.Rows.Add("2026-09-06", Path.GetFileName(dataFile));
        grid.Rows[rowIndex].Tag = fileEntry;
        grid.CurrentCell = grid.Rows[rowIndex].Cells[0];

        MethodInfo raiseCellClick = typeof(DataGridView).GetMethod(
            "OnCellClick",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var clickArguments = new DataGridViewCellEventArgs(0, rowIndex);
        // 실제 더블클릭과 동일하게 CellClick이 연속 두 번 발생하는 경로를 확인합니다.
        raiseCellClick.Invoke(grid, new object[] { clickArguments });
        raiseCellClick.Invoke(grid, new object[] { clickArguments });

        bool loaded = WaitUntil(
            () => combo.Items.Count == 2 && reviewButton.Enabled,
            5000);
        Assert(loaded, "double-click sequence loads the selected file");
        Assert(combo.Items.Count == 2, "inspection-number combo has every inspection");
        Assert(
            combo.SelectedIndex == 0 && Convert.ToInt32(combo.SelectedItem) == 1,
            "inspection-number combo selects inspection 1");

        typeof(Button).GetMethod(
            "OnClick",
            BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(reviewButton, new object[] { EventArgs.Empty });
        var reviewChart = (Chart)GetField(stationView, "m_ReviewChart");
        Assert(reviewChart != null, "REVIEW creates a graph");
        Assert(
            reviewChart != null && reviewChart.Series["하중"].Points.Count == 3,
            "REVIEW graph contains all kgf samples for inspection 1");
        Assert(
            reviewChart != null &&
            Math.Abs(reviewChart.Series["하중"].Points[0].YValues[0] - 0.010) < 0.0000001,
            "REVIEW graph uses parsed kgf without rescaling");

        combo.SelectedIndex = 1;
        typeof(Button).GetMethod(
            "OnClick",
            BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(reviewButton, new object[] { EventArgs.Empty });
        reviewChart = (Chart)GetField(stationView, "m_ReviewChart");
        Assert(
            Convert.ToInt32(combo.SelectedItem) == 2 &&
            reviewChart.Series["하중"].Points.Count == 2,
            "combo selection 2 redraws its two-sample graph");

        ((IDisposable)stationView).Dispose();
        Console.WriteLine("RESULT failures=" + s_Failures);
        return s_Failures == 0 ? 0 : 1;
    }
}
