using System;using System.Reflection;using System.Windows.Forms;using SOFCMeas;
class WordDisplaySmoke { [STAThread] static void Main(){
var f=BindingFlags.Instance|BindingFlags.NonPublic;
using(var v=new StationView()){
var t=typeof(StationView);var label=(Label)t.GetField("lblCurrentLoad",f).GetValue(v);var add=t.GetMethod("AddLoadSample",f);var reset=t.GetMethod("ResetLoadGraph",f);
reset.Invoke(v,null);if(!label.Text.Contains("D801[-][-]")||!label.Text.Contains("READ 0"))throw new Exception("reset");
add.Invoke(v,new object[]{0.5,(int?)500});if(!label.Text.Contains("D801[500][0]")||!label.Text.Contains("READ 1"))throw new Exception("positive");
add.Invoke(v,new object[]{-0.5,(int?)(-500)});if(!label.Text.Contains("D801[65036][65535]")||!label.Text.Contains("READ 2"))throw new Exception("negative");
reset.Invoke(v,null);if(!label.Text.Contains("[-][-]")||!label.Text.Contains("READ 0"))throw new Exception("reset stale");
Console.WriteLine("PASS: raw WORD display, signed values, sample count and reset");
}}}
