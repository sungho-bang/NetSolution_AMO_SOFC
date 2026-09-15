using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using SOFCMeas;
using MainForm = global::SOFCMeas.SOFCMeas;
class StartGateSmoke
{
 [STAThread] static void Main()
 {
  Application.EnableVisualStyles();
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  using(var station = new StationView())
  {
   var start=(Button)typeof(StationView).GetField("btnStart",flags).GetValue(station);
   using(var form=new Form {ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new Point(-32000,-32000),ClientSize=new Size(900,866)})
   {
    form.Controls.Add(station); form.Show();
    if(start.Text!="STOP") throw new Exception("Startup must be STOP");
    start.PerformClick();
    if(start.Text!="START"||start.ForeColor!=Color.White)throw new Exception("START style failed");
    start.PerformClick();
    if(start.Text!="STOP"||start.ForeColor!=Color.Yellow)throw new Exception("STOP style failed");
    if(station.Controls["grpHistorySearch"]==null)throw new Exception("Designer history missing");
    using(var image=new Bitmap(900,866)){station.DrawToBitmap(image,new Rectangle(0,0,900,866));image.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"StationPreview.png"));}
   }
  }
  var asm=typeof(MainForm).Assembly;
  var protocol=asm.GetType("SOFCMeas.PlcGraphProtocol");
  var snapshotType=asm.GetType("SOFCMeas.PlcGraphSnapshot");
  object engine=Activator.CreateInstance(protocol,true);
  object snapshot=Activator.CreateInstance(snapshotType,true);
  var request=snapshotType.GetProperty("RequestValue",flags);
  var process=protocol.GetMethod("Process",flags);
  request.SetValue(snapshot,(ushort)1,null);
  foreach(bool enabled in new[]{false,true,false,true})
  {
   var update=process.Invoke(engine,new object[]{snapshot,(ushort)1,(ushort)0,enabled});
   var add=(bool)update.GetType().GetProperty("AddSample",flags).GetValue(update,null);
   if(add!=enabled)throw new Exception("PLC sample gate failed");
  }
  request.SetValue(snapshot,(ushort)0,null);
  var end=process.Invoke(engine,new object[]{snapshot,(ushort)1,(ushort)0,true});
  if(!(bool)end.GetType().GetProperty("FreezeGraph",flags).GetValue(end,null))throw new Exception("PLC end failed");
  Console.WriteLine("START/STOP colors, designer history and PLC collection gate: PASS");
 }
}
