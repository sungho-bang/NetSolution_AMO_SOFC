using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SOFCMeas;

internal static class StartupAxisSmoke
{
    [STAThread]
    private static void Main()
    {
        try
        {
            Run();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.GetType().FullName + ": " + exception.Message);
            if (exception.InnerException != null)
                Console.WriteLine(
                    exception.InnerException.GetType().FullName + ": " +
                    exception.InnerException.Message);
            Environment.ExitCode = 1;
        }
    }

    private static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "SOFC-Axis-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("SOFCMEAS_ROOT", root);
        Directory.CreateDirectory(Path.Combine(root, "conf"));
        File.WriteAllText(Path.Combine(root, "conf", "SOFCMeas.config"),
            "<configuration><appSettings>" +
            "<add key='Station1.Inspection.ThresholdKgf' value='0.50'/>" +
            "<add key='Station1.Graph.YMax' value='9.00'/>" +
            "<add key='Station1.Graph.YAuto' value='true'/>" +
            "<add key='Station1.Graph.YInterval' value='0.10'/>" +
            "<add key='Station1.Graph.XMin' value='0'/>" +
            "<add key='Station1.Graph.XMax' value='60'/>" +
            "<add key='Station1.Graph.XInterval' value='6.6'/>" +
            "<add key='Station1.Graph.XAuto' value='true'/>" +
            "</appSettings></configuration>");

        Application.EnableVisualStyles();
        using (var view = new StationView())
        {
            Type type = typeof(StationView);
            type.GetMethod("ConfigureGraph", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, new object[] { 1 });
            var chart = (Chart)type.GetField("chartLoad", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(view);
            Axis x = chart.ChartAreas[0].AxisX;
            Axis y = chart.ChartAreas[0].AxisY;
            if (Math.Abs(x.Maximum - 66D) > 0.000001D ||
                Math.Abs(x.Interval - 6.6D) > 0.000001D ||
                !x.LabelStyle.Enabled)
                throw new Exception("Startup X-axis does not use configured values.");
            if (Math.Abs(y.Maximum - 0.55D) > 0.000001D)
                throw new Exception("Expected SPEC + 10% Y maximum, actual=" + y.Maximum);
            if (Math.Abs(y.Interval - 0.055D) > 0.000001D || !y.LabelStyle.Enabled)
                throw new Exception("Startup Y-axis is not divided into ten sections.");
            Title caption = chart.Titles.FindByName("YAxisCaption");
            if (!string.IsNullOrEmpty(y.Title) || caption == null ||
                caption.TextOrientation != TextOrientation.Horizontal ||
                caption.Alignment != System.Drawing.ContentAlignment.MiddleLeft ||
                caption.DockedToChartArea != chart.ChartAreas[0].Name ||
                caption.IsDockedInsideChartArea)
                throw new Exception("Y-axis caption is not docked above the Y-axis.");
            Series bootstrap = chart.Series.FindByName("AxisBootstrap");
            if (bootstrap == null || bootstrap.Points.Count != 1)
                throw new Exception("Empty chart does not have an axis bootstrap point.");

            chart.SaveImage(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StartupAxisPreview.png"),
                ChartImageFormat.Png);
            var addSample = type.GetMethod(
                "AddLoadSample",
                BindingFlags.Instance | BindingFlags.NonPublic);
            for (int sample = 1; sample <= 61; sample++)
                addSample.Invoke(view, new object[] { 0.1D, null });
            if (Math.Abs(x.Maximum - 67.1D) > 0.000001D ||
                Math.Abs(x.Interval - 6.71D) > 0.000001D)
                throw new Exception("X-axis did not expand by 10% and remain divided into ten sections.");
            Console.WriteLine("PASS: empty startup chart shows configured X/Y units and horizontal Y title.");
        }
    }
}
