using System;using System.IO;using System.Reflection;using System.Threading.Tasks;using System.Threading;using System.Windows.Forms;using SOFCMeas;
class SimulationSmoke{
static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;static Type T=typeof(StationView);
static void Pump(Task task){while(!task.IsCompleted){Application.DoEvents();Thread.Sleep(2);}task.GetAwaiter().GetResult();}
[STAThread]static void Main(string[] args){Application.EnableVisualStyles();string root=Path.Combine(Path.GetTempPath(),"SOFC-SIM-"+Guid.NewGuid().ToString("N"));Environment.SetEnvironmentVariable("SOFCMEAS_ROOT",root);Directory.CreateDirectory(Path.Combine(root,"conf"));File.WriteAllText(Path.Combine(root,"conf","SOFCMeas.config"),"<configuration><appSettings><add key='Runtime.Simulation' value='1'/></appSettings></configuration>");
var storage=Activator.CreateInstance(T.Assembly.GetType("SOFCMeas.InspectionStorageService"),true);
using(var host=new Form{ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new System.Drawing.Point(-32000,-32000)})using(var one=new StationView())using(var two=new StationView()){
host.Controls.Add(one);host.Controls.Add(two);host.Show();T.GetMethod("BindHistory",F).Invoke(one,new[]{storage,(object)"#1"});T.GetMethod("BindHistory",F).Invoke(two,new[]{storage,(object)"#2"});
int interval=args.Length>0?1000:1;
var a=(Task)T.GetMethod("RunSimulationAsync",F).Invoke(one,new object[]{interval});var b=(Task)T.GetMethod("RunSimulationAsync",F).Invoke(two,new object[]{interval});Pump(Task.WhenAll(a,b));
foreach(var v in new[]{one,two}){if((bool)T.GetProperty("CollectionEnabled",F).GetValue(v,null))throw new Exception("Not stopped");if((int)T.GetField("m_GraphReadCount",F).GetValue(v)!=60)throw new Exception("Sample count");var grid=(DataGridView)T.GetField("historyResults",F).GetValue(v);if(grid.Rows.Count!=1)throw new Exception("List registration");if(T.GetField("m_ReviewChart",F).GetValue(v)==null)throw new Exception("No final graph");}
if(Directory.GetFiles(Path.Combine(root,"Data","PLC1"),"*.csv",SearchOption.AllDirectories).Length!=1||Directory.GetFiles(Path.Combine(root,"Data","PLC2"),"*.csv",SearchOption.AllDirectories).Length!=1)throw new Exception("Files");
var cancel=(Task)T.GetMethod("RunSimulationAsync",F).Invoke(one,new object[]{1000});((CancellationTokenSource)T.GetField("m_SimulationCancellation",F).GetValue(one)).Cancel();Pump(cancel);
if(Directory.GetFiles(Path.Combine(root,"Data"),"*.csv",SearchOption.AllDirectories).Length!=2)throw new Exception("Partial file");
Console.WriteLine("PASS: two independent stations, 60 samples, STOP, CSV, list and final graph; cancellation skips partial save. Interval="+interval);
}}}
