using System;using System.Collections;using System.Reflection;using System.Threading.Tasks;using System.Windows.Forms;using System.Windows.Forms.DataVisualization.Charting;using System.Drawing;using SOFCMeas;
class ReviewSelectSmoke{[STAThread]static void Main(){Application.EnableVisualStyles();var f=BindingFlags.Instance|BindingFlags.NonPublic;var a=typeof(StationView).Assembly;var st=a.GetType("SOFCMeas.InspectionStorageService");var storage=Activator.CreateInstance(st,true);var files=(IList)st.GetMethod("GetLotFiles",f).Invoke(storage,new object[]{new DateTime(2026,9,3),new DateTime(2026,9,3),"#1"});
using(var v=new StationView())using(var host=new Form{ClientSize=new Size(910,900),StartPosition=FormStartPosition.Manual,Location=new Point(-32000,-32000),ShowInTaskbar=false}){
host.Controls.Add(v);host.Show();var t=typeof(StationView);t.GetMethod("BindHistory",f).Invoke(v,new object[]{storage,"#1"});var grid=(DataGridView)t.GetField("historyResults",f).GetValue(v);var combo=(ComboBox)t.GetField("cboInspectionNumber",f).GetValue(v);var button=(Button)t.GetField("btnReview",f).GetValue(v);
int row=grid.Rows.Add("2026-09-03","test.csv");grid.Rows[row].Tag=files[0];grid.CurrentCell=grid.Rows[row].Cells[0];
var task=(Task)t.GetMethod("LoadReviewFileAsync",f).Invoke(v,new object[]{0});while(!task.IsCompleted){Application.DoEvents();System.Threading.Thread.Sleep(10);}task.GetAwaiter().GetResult();
if(combo.Items.Count!=12)throw new Exception("Inspection numbers");combo.SelectedIndex=2;if(t.GetField("m_ReviewChart",f).GetValue(v)!=null)throw new Exception("Premature graph");button.PerformClick();var graph=(Chart)t.GetField("m_ReviewChart",f).GetValue(v);if(graph==null||graph.Series[0].Points.Count!=60||!graph.Titles[0].Text.Contains("검사 3"))throw new Exception("Selected review");
if((int)t.GetField("m_InspectionCount",f).GetValue(v)!=0)throw new Exception("Live count modified");
using(var bmp=new Bitmap(900,880)){v.DrawToBitmap(bmp,new Rectangle(0,0,900,880));bmp.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ReviewSelection.png"));}
t.GetMethod("ResetLoadGraph",f).Invoke(v,null);if(t.GetField("m_ReviewChart",f).GetValue(v)!=null)throw new Exception("Live reset");
Console.WriteLine("PASS: load 12 numbers, select 3, REVIEW draws 60 samples, live data unchanged, reset returns live graph.");
}}}
