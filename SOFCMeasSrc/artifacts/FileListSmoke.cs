using System;using System.IO;using System.Reflection;using System.Collections;using System.Windows.Forms;using SOFCMeas;
class FileListSmoke{
[STAThread]static void Main(){var flags=BindingFlags.Instance|BindingFlags.NonPublic;string root=Path.Combine(Path.GetTempPath(),"SOFC-Files-"+Guid.NewGuid().ToString("N"));Environment.SetEnvironmentVariable("SOFCMEAS_ROOT",root);
string source="C:\\SOFCMeas\\Data\\PLC1\\2026\\09\\03\\20260903101400_LOT0301_GOOD.csv";
foreach(string plc in new[]{"PLC1","PLC2"}){string folder=Path.Combine(root,"Data",plc,"2026","09","03");Directory.CreateDirectory(folder);File.Copy(source,Path.Combine(folder,plc+".csv"));}
var type=typeof(StationView).Assembly.GetType("SOFCMeas.InspectionStorageService");var storage=Activator.CreateInstance(type,true);var method=type.GetMethod("GetLotFiles",flags);
foreach(string station in new[]{"#1","#2"}){var rows=(IList)method.Invoke(storage,new object[]{new DateTime(2026,9,3),new DateTime(2026,9,6),station});if(rows.Count!=1)throw new Exception("File query");var name=(string)rows[0].GetType().GetProperty("FileName",flags).GetValue(rows[0],null);if(name!="PLC"+station.Substring(1)+".csv")throw new Exception("Station isolation");var inspections=(IList)type.GetMethod("ReadLotFile",flags).Invoke(storage,new object[]{rows[0],station});if(inspections.Count!=12)throw new Exception("File review");}
var empty=(IList)method.Invoke(storage,new object[]{new DateTime(2026,9,4),new DateTime(2026,9,6),"#1"});if(empty.Count!=0)throw new Exception("Date filter");
using(var v=new StationView()){var grid=(DataGridView)typeof(StationView).GetField("historyResults",flags).GetValue(v);if(grid.Columns.Count!=2||grid.Columns[0].HeaderText!="날짜"||grid.Columns[1].HeaderText!="파일")throw new Exception("Columns");}
Console.WriteLine("PASS: folder-only search without log index, station/date filters, two columns, CSV review.");}}
