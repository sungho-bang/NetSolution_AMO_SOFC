using System;using System.IO;using System.Reflection;using System.Collections;using System.Linq;using SOFCMeas;
class SampleLots {
static BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic,I=BindingFlags.Instance|BindingFlags.NonPublic;
static Assembly A=typeof(SettingForm).Assembly;
static object New(string n){return Activator.CreateInstance(A.GetType("SOFCMeas."+n),true);}
static void Set(object o,string p,object v){o.GetType().GetProperty(p,I).SetValue(o,v,null);}
static object Get(object o,string p){return o.GetType().GetProperty(p,I).GetValue(o,null);}
static void Main(string[] args){
bool real=args.Length>0&&args[0]=="generate";
string root=real?"C:\\SOFCMeas":Path.Combine(Path.GetTempPath(),"SOFC-LotFormat-"+Guid.NewGuid().ToString("N"));Environment.SetEnvironmentVariable("SOFCMEAS_ROOT",root);
A.GetType("SOFCMeas.ApplicationPaths").GetMethod("EnsureStorageDirectories",S).Invoke(null,null);
var storage=New("InspectionStorageService");var rng=new Random(9032026);int files=0,total=0;
for(DateTime date=new DateTime(2026,9,3);date<=new DateTime(2026,9,6);date=date.AddDays(1))for(int station=1;station<=2;station++){
var started=date.AddHours(9+station);string lotNo="SAMPLE_"+date.ToString("yyyyMMdd")+"_S"+station;
var lot=New("LotStorageData");Set(lot,"CreatedAt",started);Set(lot,"CompletedAt",started.AddMinutes(14));Set(lot,"Station","#"+station);Set(lot,"LotNumber",lotNo);Set(lot,"FileName","SAMPLE_SOFC_"+station);Set(lot,"OperatorName","SAMPLE");Set(lot,"SpecText","0.40");
var inspections=Array.CreateInstance(A.GetType("SOFCMeas.BufferedInspectionData"),12);
for(int n=0;n<12;n++){
bool good=station==1||n%4!=0;double peak=good?0.41+rng.NextDouble()*0.19:0.25+rng.NextDouble()*0.14;var samples=Array.CreateInstance(A.GetType("SOFCMeas.InspectionSample"),60);double max=0;
for(int j=0;j<60;j++){
double shape=Math.Max(0,1-Math.Abs(j-30)/30D);int raw=(int)Math.Round(1000*(0.02+(peak-0.02)*shape+(j==30?0:(rng.NextDouble()-0.5)*0.004)));raw=Math.Max(20,raw);double kgf=raw/1000D;max=Math.Max(max,kgf);
var sample=New("InspectionSample");Set(sample,"Number",j+1);Set(sample,"RawValue",raw);Set(sample,"LoadKgf",kgf);Set(sample,"ReadTime",started.AddMinutes(n).AddSeconds(j+1));samples.SetValue(sample,j);}
var record=New("InspectionLogRecord");Set(record,"Time",started.AddMinutes(n+1));Set(record,"StartedAt",started.AddMinutes(n));Set(record,"Station","#"+station);Set(record,"LotNumber",lotNo);Set(record,"FileName","SAMPLE_SOFC_"+station);Set(record,"OperatorName","SAMPLE");Set(record,"Result",max>=0.4?"GOOD":"NG");Set(record,"PeakLoadKgf",max);Set(record,"LowerSpecKgf",0.4);Set(record,"UpperSpecKgf",0.4);Set(record,"Message","SAMPLE: random demonstration data, not production measurements.");
var data=New("BufferedInspectionData");Set(data,"Record",record);Set(data,"Samples",samples);inspections.SetValue(data,n);}
Set(lot,"Inspections",inspections);string path=(string)storage.GetType().GetMethod("SaveLot",I).Invoke(storage,new object[]{lot});var lines=File.ReadAllLines(path);if(lines.Length!=15||!lines[0].StartsWith("모델명,")||!lines[2].StartsWith("NO.,판정결과,"))throw new Exception("CSV layout");
foreach(var data in inspections){var record=Get(data,"Record");var back=(IList)storage.GetType().GetMethod("ReadSamples",I).Invoke(storage,new object[]{record});if(back.Count!=60)throw new Exception("Review sample count");var source=(IList)Get(data,"Samples");for(int j=0;j<60;j++)if((double)Get(back[j],"LoadKgf")!=(double)Get(source[j],"LoadKgf"))throw new Exception("Value mismatch");}
var records=(IEnumerable)storage.GetType().GetMethod("GetInspectionLogs",I).Invoke(storage,new object[]{date,date,"#"+station});int found=0;foreach(var record in records)if((string)Get(record,"LotNumber")==lotNo){found++;var back=(IList)storage.GetType().GetMethod("ReadSamples",I).Invoke(storage,new object[]{record});if(back.Count!=60)throw new Exception("Index roundtrip");}if(found!=12)throw new Exception("Index rows");
files++;total+=12;if(real)Console.WriteLine(path);
}
Console.WriteLine("PASS: "+files+" LOT CSV files, "+total+" inspections, 60 samples each; layout, data and REVIEW roundtrip verified.");
}}
