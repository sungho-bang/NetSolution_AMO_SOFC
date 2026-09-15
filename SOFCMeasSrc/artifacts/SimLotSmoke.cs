using System;using System.IO;using System.Reflection;using System.Threading;using System.Threading.Tasks;using System.Windows.Forms;using System.Collections;using SOFCMeas;
using MainForm = global::SOFCMeas.SOFCMeas;
class SimLotSmoke {
static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
static object Get(object o,string n){return o.GetType().GetField(n,F).GetValue(o);}
static void Until(Func<bool> ready){var end=DateTime.Now.AddSeconds(20);while(!ready()){Application.DoEvents();Thread.Sleep(2);if(DateTime.Now>end)throw new Exception("Timeout");}}
static void Pump(Task t){Until(()=>t.IsCompleted);t.GetAwaiter().GetResult();}
[STAThread]static void Main(){Application.EnableVisualStyles();var root=Path.Combine(Path.GetTempPath(),"SimLot-"+Guid.NewGuid().ToString("N"));Environment.SetEnvironmentVariable("SOFCMEAS_ROOT",root);Directory.CreateDirectory(Path.Combine(root,"conf"));File.WriteAllText(Path.Combine(root,"conf","SOFCMeas.config"),"<configuration><appSettings><add key='Runtime.Simulation' value='1'/></appSettings></configuration>");
using(var form=new MainForm()){var h=form.Handle;((System.Windows.Forms.Timer)Get(form,"m_LogCleanupTimer")).Stop();
for(int station=1;station<=2;station++){var view=(StationView)Get(form,"stationView"+station);var vh=view.Handle;SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());view.FileName="TEST_MODEL";view.LotNumber="TEST_LOT";view.OperatorName="TEST_OPERATOR";
var type=view.GetType();var run=(Task)type.GetMethod("RunSimulationAsync",F).Invoke(view,new object[]{1});type.GetField("m_SimulationTask",F).SetValue(view,run);
var pending=(IList)Get(view,"m_PendingSimulations");Until(()=>pending.Count==2);if(!(bool)type.GetProperty("CollectionEnabled",F).GetValue(view,null))throw new Exception("Auto stopped");if(Get(view,"m_ReviewChart")==null)throw new Exception("Peak chart missing");
Until(()=>(int)Get(view,"m_GraphReadCount")>0&&(int)Get(view,"m_GraphReadCount")<60);
Pump((Task)type.GetMethod("StopSimulationAsync",F).Invoke(view,null));if((int)Get(view,"m_InspectionCount")!=2||(int)Get(view,"m_GraphReadCount")!=0||pending.Count!=2)throw new Exception("Partial cancellation changed totals");
var data=Path.Combine(root,"Data","PLC"+station);if(Directory.Exists(data)&&Directory.GetFiles(data,"*.csv",SearchOption.AllDirectories).Length!=0)throw new Exception("Saved before LOT END");
if(station==2){var next=(Task)type.GetMethod("RunSimulationAsync",F).Invoke(view,new object[]{1000});type.GetField("m_SimulationTask",F).SetValue(view,next);} var save=(Task)typeof(MainForm).GetMethod("HandleLotEndRequestedAsync",F).Invoke(form,new object[]{"PLC"+station,"#"+station,view,Get(form,"m_Station"+station+"Capture"),Get(form,"m_Station"+station+"Lot"),false});Pump(save);
var files=Directory.GetFiles(data,"*.csv",SearchOption.AllDirectories);if(files.Length!=1)throw new Exception("LOT file count");var lines=File.ReadAllLines(files[0]);if(lines.Length!=5||!lines[0].Contains("TEST_MODEL")||!lines[0].Contains("TEST_OPERATOR"))throw new Exception("LOT content");if(pending.Count!=0||(int)Get(view,"m_InspectionCount")!=0||((DataGridView)Get(view,"historyResults")).Rows.Count!=0)throw new Exception("LOT reset");
Console.WriteLine("PASS PLC"+station+": two completed cycles, START retained, no premature CSV, STOP discards partial, LOT END writes two rows with UI identity and resets.");
}
}
}
}
