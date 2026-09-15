using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using SOFCMeas;
class PlcSettingsSmoke {
 static BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic, I=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 [STAThread] static void Main(){
 Application.EnableVisualStyles();
 string root=Path.Combine(Path.GetTempPath(),"SOFC-PlcSettings-"+Guid.NewGuid().ToString("N"));
 Environment.SetEnvironmentVariable("SOFCMEAS_ROOT",root); Directory.CreateDirectory(Path.Combine(root,"Data"));
 string path=Path.Combine(root,"Data","SOFCMeas.config");
 File.WriteAllText(path,"<configuration><appSettings><add key='Plc.Port' value='12000'/><add key='Plc.LoadScale' value='2000'/><add key='Station1.Plc.Port' value='13000'/><add key='Custom.Keep' value='yes'/></appSettings></configuration>");
 var asm=typeof(SettingForm).Assembly;var cat=asm.GetType("SOFCMeas.SettingsCatalog");
 var vals=(Dictionary<string,string>)cat.GetMethod("Load",S).Invoke(null,null);
 Check(vals["Station1.Plc.Port"]=="13000"&&vals["Station2.Plc.Port"]=="12000","Legacy precedence");
 Check(!vals.ContainsKey("Plc.Port")&&vals["Station2.Plc.LoadScale"]=="2000","Legacy expansion");
 using(var ui=new SettingForm()){
 ui.GetType().GetField("CanSave",I).SetValue(ui,new Func<bool>(()=>true));
 var grids=(List<DataGridView>)ui.GetType().GetField("grids",I).GetValue(ui);
 foreach(DataGridViewRow row in grids[1].Rows){string k=(string)row.Tag; if(k.EndsWith(".Encoding"))row.Cells[1].Value="ASCII";if(k.EndsWith(".Transport"))row.Cells[1].Value="UDP";if(k.EndsWith(".Frame"))row.Cells[1].Value="4E";if(k.EndsWith(".Processor"))row.Cells[1].Value="iQR";}
 ui.GetType().GetMethod("SaveValues",I).Invoke(ui,null);
 ui.StartPosition=FormStartPosition.Manual;ui.Location=new Point(-32000,-32000);ui.Show();Application.DoEvents();
 foreach(var grid in grids.Skip(1)){var info=grid.Parent.Controls.OfType<Label>().Single();Check(info.Text.Contains("D801 ~ D802"),"WORD range");Check(grid.Bottom<=info.Top,"Info overlaps grid");}
 using(var bmp=new Bitmap(ui.Width,ui.Height)){ui.DrawToBitmap(bmp,new Rectangle(0,0,bmp.Width,bmp.Height));bmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SettingsPreview.png"));}
 }
 string saved=File.ReadAllText(path);Check(!saved.Contains("key=\"Plc.Port\""),"Duplicate still saved");Check(saved.Contains("Custom.Keep"),"Unrelated key lost");
 var settingsType=asm.GetType("SOFCMeas.PlcGraphSettings");var load=settingsType.GetMethod("Load",S);
 var settings=load.Invoke(null,new object[]{1,"127.0.0.1"});
 Check((bool)settingsType.GetProperty("IsAscii",I).GetValue(settings,null),"ASCII runtime");Check((bool)settingsType.GetProperty("IsUdp",I).GetValue(settings,null),"UDP runtime");Check(settingsType.GetProperty("Frame",I).GetValue(settings,null).ToString()=="E4","4E runtime");Check(settingsType.GetProperty("Processor",I).GetValue(settings,null).ToString()=="iQR","iQR runtime");
 var two=load.Invoke(null,new object[]{2,"127.0.0.1"});Check(!(bool)settingsType.GetProperty("IsAscii",I).GetValue(two,null),"Station separation");
 var lib=Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"McpXLib.dll"));var converter=lib.GetType("McpXLib.Utils.DeviceConverter");var convert=converter.GetMethod("ConvertValueArray",S).MakeGenericMethod(typeof(int));
 foreach(int raw in new[]{0,500,65536,-500,int.MinValue,int.MaxValue}){
 ushort low=unchecked((ushort)raw), high=unchecked((ushort)(raw>>16));
 var bytes=new byte[]{(byte)low,(byte)(low>>8),(byte)high,(byte)(high>>8)};
 int actual=((int[])convert.Invoke(null,new object[]{bytes}))[0];Check(actual==raw,"2WORD conversion "+raw);
 Check(actual/1000.0==raw/1000.0,"kgf scale");}
 vals["Station1.Plc.Encoding"]="invalid";bool rejected=false;try{cat.GetMethod("Validate",S).Invoke(null,new object[]{vals});}catch(TargetInvocationException){rejected=true;}Check(rejected,"Invalid enum allowed");
 Console.WriteLine("PASS: legacy migration, independent settings, runtime connection options, UI layout, signed 2WORD boundaries.");
 }
}
