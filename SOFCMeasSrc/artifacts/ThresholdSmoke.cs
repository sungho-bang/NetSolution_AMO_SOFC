using System;using System.IO;using System.Reflection;using System.Windows.Forms;using System.Windows.Forms.DataVisualization.Charting;using System.Drawing;using SOFCMeas;
class ThresholdSmoke {
[STAThread] static void Main(){
Application.EnableVisualStyles();string root=Path.Combine(Path.GetTempPath(),"SOFC-threshold-"+Guid.NewGuid().ToString("N"));Environment.SetEnvironmentVariable("SOFCMEAS_ROOT",root);Directory.CreateDirectory(Path.Combine(root,"conf"));
var t=typeof(StationView);var f=BindingFlags.Instance|BindingFlags.NonPublic;
using(var v=new StationView())using(var host=new Form {Size=new Size(925,940),StartPosition=FormStartPosition.Manual,Location=new Point(-32000,-32000),ShowInTaskbar=false}){
host.Controls.Add(v);host.Show();t.GetMethod("ConfigureGraph",f).Invoke(v,new object[]{1});
var chart=(Chart)t.GetField("chartLoad",f).GetValue(v);var count=(Label)t.GetField("lblCompletedCount",f).GetValue(v);var verdict=(Label)t.GetField("lblVerdictValue",f).GetValue(v);
if(v.SpecText!="0.40"||Math.Abs(chart.ChartAreas[0].AxisX.Maximum-66)>0.0001)throw new Exception("defaults");
var reset=t.GetMethod("ResetLoadGraph",f);var add=t.GetMethod("AddLoadSample",f);var end=t.GetMethod("CompleteInspection",f);
double[] values={0.399,0.4,0.8};string[] expected={"NG","GOOD","GOOD"};
for(int i=0;i<3;i++){reset.Invoke(v,null);add.Invoke(v,new object[]{values[i],(int?)(int)(values[i]*1000)});add.Invoke(v,new object[]{0.2,(int?)200});end.Invoke(v,null);if(verdict.Text!=expected[i]||count.Text!="검사 횟수: "+(i+1))throw new Exception("threshold/count");}
if(Math.Abs(chart.ChartAreas[0].AxisY.Maximum-0.88)>0.0001||chart.ChartAreas[0].AxisY.Enabled!=AxisEnabled.True)throw new Exception("Y scale");
using(var bmp=new Bitmap(900,900)){v.DrawToBitmap(bmp,new Rectangle(0,0,900,900));bmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ThresholdPreview.png"));}
Console.WriteLine("PASS: peak threshold below/equal/above, counts, 66 X range, Y +10%, labels.");
}}}
