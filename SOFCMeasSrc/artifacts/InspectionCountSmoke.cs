using System;using System.Reflection;using System.Windows.Forms;using SOFCMeas;
class InspectionCountSmoke { [STAThread] static void Main(){
var f=BindingFlags.Instance|BindingFlags.NonPublic;var t=typeof(StationView);
using(var v=new StationView()){
var label=(Label)t.GetField("lblCurrentLoad",f).GetValue(v);
Action<string> check=s=>{if(!label.Text.Contains("검사 횟수 "+s))throw new Exception(label.Text);};
var add=t.GetMethod("AddLoadSample",f);var reset=t.GetMethod("ResetLoadGraph",f);var complete=t.GetMethod("CompleteInspection",f);
reset.Invoke(v,null);check("0");
for(int i=0;i<3;i++)add.Invoke(v,new object[]{0.5,(int?)500});check("0");
complete.Invoke(v,null);check("1");reset.Invoke(v,null);check("1");
add.Invoke(v,new object[]{0.9,(int?)900});complete.Invoke(v,null);check("2");
reset.Invoke(v,null);complete.Invoke(v,null);check("2");
t.GetMethod("ResetInspectionSummary",f).Invoke(v,null);check("0");
Console.WriteLine("PASS: samples do not increment count; PASS/FAIL completion increments; invalid skipped; LOT reset clears count.");
}}}
