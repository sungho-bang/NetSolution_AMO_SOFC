using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SOFCMeas;

internal static class LivePeakMarkerSmoke
{
    private const BindingFlags Flags =
        BindingFlags.Instance | BindingFlags.NonPublic;

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Type type = typeof(StationView);
        using (var host = new Form())
        using (var view = new StationView())
        {
            host.Controls.Add(view);
            host.StartPosition = FormStartPosition.Manual;
            host.Location = new Point(-32000, -32000);
            host.Show();
            IntPtr handle = view.Handle;

            type.GetMethod("ResetLoadGraph", Flags).Invoke(view, null);
            MethodInfo add = type.GetMethod("AddLoadSample", Flags);
            add.Invoke(view, new object[] { 0.120D, 120 });
            add.Invoke(view, new object[] { 0.458D, 458 });
            add.Invoke(view, new object[] { 0.426D, 426 });
            type.GetMethod("CompleteInspection", Flags).Invoke(view, null);

            Chart chart = (Chart)type.GetField("chartLoad", Flags).GetValue(view);
            Series marker = chart.Series.FindByName("LivePeak");
            Check(marker != null, "Live peak marker missing");
            Check(marker.ChartType == SeriesChartType.Point, "Live peak chart type");
            Check(marker.MarkerStyle == MarkerStyle.Circle, "Live peak marker shape");
            Check(marker.MarkerColor.A > 0 && marker.MarkerColor.A < 255, "Live peak transparency");
            Check(marker.Points.Count == 1, "Live peak point count");
            Check(Math.Abs(marker.Points[0].XValue - 2D) < 0.000001D, "Live peak X position");
            Check(Math.Abs(marker.Points[0].YValues[0] - 0.458D) < 0.000001D, "Live peak value");
            Check(marker.Points[0].Label == "최대 0.458 kgf", "Live peak label");

            type.GetMethod("ResetLoadGraph", Flags).Invoke(view, null);
            Check(chart.Series.FindByName("LivePeak") == null, "Live peak reset");
        }

        Console.WriteLine(
            "PASS: judged live graph shows a translucent peak circle and peak value, " +
            "then clears it on graph reset.");
    }
}
