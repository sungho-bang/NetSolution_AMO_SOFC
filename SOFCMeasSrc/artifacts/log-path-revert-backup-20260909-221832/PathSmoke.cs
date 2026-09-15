using System;using System.IO;using System.Reflection;using System.Collections;using SOFCMeas;
class PathSmoke {
static BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic,I=BindingFlags.Instance|BindingFlags.NonPublic;
static Assembly A=typeof(SettingForm).Assembly;
static object New(string n){return Activator.CreateInstance(A.GetType("SOFCMeas."+n),true);}
static void Set(object o,string p,object v){o.GetType().GetProperty(p,I).SetValue(o,v,null);}
static void Check(bool ok,string m){if(!ok)throw new Exception(m);}
static void Main(string[] args){
 string root=args.Length==0?"C:\\SOFCMeas":Path.Combine(Path.GetTempPath(),"SOFC-path-"+Guid.NewGuid().ToString("N"));
 Environment.SetEnvironmentVariable("SOFCMEAS_ROOT",args.Length==0?null:root);
 Environment.SetEnvironmentVariable("SOFCMEAS_LOG_ROOT",null);
 string logRoot=args.Length==0?@"C:\CSEng\TrimForm\LOG":Path.Combine(root,"log");
 var paths=A.GetType("SOFCMeas.ApplicationPaths");
 Func<string,string> get=p=>(string)paths.GetProperty(p,S).GetValue(null,null);
 Check(get("ApplicationRootDirectory")==root,"Root");Check(get("ConfigurationFilePath")==Path.Combine(root,"conf","SOFCMeas.config"),"conf");Check(get("LogRootDirectory")==logRoot,"log");Check(get("DataRootDirectory")==Path.Combine(root,"Data"),"Data");
 if(args.Length==0){Console.WriteLine("PASS: deployed application root C:\\SOFCMeas, log root C:\\CSEng\\TrimForm\\LOG");return;}
 paths.GetMethod("EnsureStorageDirectories",S).Invoke(null,null);
 var lt=A.GetType("SOFCMeas.ApplicationLogService");var log=Activator.CreateInstance(lt,I,null,new object[]{20L*1024*1024},null);lt.GetMethod("Info",I).Invoke(log,new object[]{"PATH-TEST","isolated validation"});
 Check(Directory.GetFiles(get("LogRootDirectory"),"*.log",SearchOption.AllDirectories).Length==1,"Application log output");
 var record=New("InspectionLogRecord");Set(record,"Time",DateTime.Now);Set(record,"StartedAt",DateTime.Now);Set(record,"Station","#1");Set(record,"LotNumber","PATH_TEST");Set(record,"FileName","MODEL_TEST");Set(record,"Result","PASS");
 var sample=New("InspectionSample");Set(sample,"Number",1);Set(sample,"ReadTime",DateTime.Now);Set(sample,"RawValue",500);Set(sample,"LoadKgf",0.5);
 var samples=Array.CreateInstance(sample.GetType(),1);samples.SetValue(sample,0);
 var data=New("BufferedInspectionData");Set(data,"Record",record);Set(data,"Samples",samples);var inspections=Array.CreateInstance(data.GetType(),1);inspections.SetValue(data,0);
 var lot=New("LotStorageData");Set(lot,"CreatedAt",DateTime.Now);Set(lot,"CompletedAt",DateTime.Now);Set(lot,"Station","#1");Set(lot,"LotNumber","PATH_TEST");Set(lot,"FileName","MODEL_TEST");Set(lot,"Inspections",inspections);
 var storage=New("InspectionStorageService");string file=(string)storage.GetType().GetMethod("SaveLot",I).Invoke(storage,new object[]{lot});Check(file.StartsWith(get("DataRootDirectory"))&&File.Exists(file),"LOT data output");
 var records=(ICollection)storage.GetType().GetMethod("GetInspectionLogs",I).Invoke(storage,new object[]{DateTime.Today,DateTime.Today,"#1"});Check(records.Count==1,"Saved-result lookup");
 Console.WriteLine("PASS: log writing, LOT CSV output, saved-result index/query using shared root");
}}
