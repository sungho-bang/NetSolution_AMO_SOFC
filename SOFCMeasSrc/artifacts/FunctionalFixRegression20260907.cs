// Isolated diagnostic harness. Does not invoke Program.Main, real PLCs, or license registry writes.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

internal static class FunctionalFixRegression20260907
{
    static Assembly App;
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static string Root;
    static int Checks, Findings, Failures;
    static Type T(string n) { return App.GetType("SOFCMeas." + n, true); }
    static object New(string n, params object[] args) { return Activator.CreateInstance(T(n), Flags & ~BindingFlags.Static, null, args, null); }
    static object Call(object o, string name, params object[] args)
    {
        Type t = o as Type ?? o.GetType();
        var method = t.GetMethod(name, Flags);
        if (args.Length < method.GetParameters().Length)
            args = args.Concat(method.GetParameters().Skip(args.Length).Select(p => p.DefaultValue)).ToArray();
        try { return method.Invoke(o is Type ? null : o, args); }
        catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    static object Get(object o, string n)
    {
        var p = o.GetType().GetProperty(n, Flags);
        return p != null ? p.GetValue(o, null) : o.GetType().GetField(n, Flags).GetValue(o);
    }
    static void Set(object o, string n, object v)
    {
        var p = o.GetType().GetProperty(n, Flags);
        if (p != null) p.SetValue(o, v, null); else o.GetType().GetField(n, Flags).SetValue(o, v);
    }
    static object Obj(string n, params object[] fields)
    {
        object o = New(n);
        for (int i = 0; i < fields.Length; i += 2) Set(o, (string)fields[i], fields[i+1]);
        return o;
    }
    static Array Arr(string n, params object[] items)
    {
        Array a = Array.CreateInstance(T(n), items.Length);
        for (int i = 0; i < items.Length; i++) a.SetValue(items[i], i);
        return a;
    }
    static void Check(bool ok, string name) { Checks++; if(!ok) Failures++; Console.WriteLine((ok ? "PASS " : "FAIL ") + name); }
    static void Finding(bool reproduced, string name) { if (reproduced) Findings++; Console.WriteLine((reproduced ? "REPRODUCED " : "NOT_REPRODUCED ") + name); }
    static void Pump(int ms)
    {
        var timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < ms) { Application.DoEvents(); Thread.Sleep(10); }
    }
    static void Wait(Func<bool> done, string label, int ms = 4000)
    {
        var timer = Stopwatch.StartNew();
        while (!done() && timer.ElapsedMilliseconds < ms) Pump(10);
        if (!done()) throw new Exception("Timed out: " + label);
    }
    static Dictionary<string,string> Config()
    { return (Dictionary<string,string>)Call(T("SettingsCatalog"), "Load"); }
    static void Save(Dictionary<string,string> cfg)
    { Call(T("ApplicationConfiguration"), "SaveAppSettings", cfg, true); }
    static object Inspection(string verdict, params double[] loads)
    {
        var now = new DateTime(2026, 9, 7, 12, 0, 0);
        var samples = loads.Select((x,i) => Obj("InspectionSample", "Number", i+1, "ReadTime", now.AddSeconds(i), "RawValue", (int)(x*1000), "LoadKgf", x)).ToArray();
        var record = Obj("InspectionLogRecord", "Time", now, "StartedAt", now, "Station", "#1", "LotNumber", "AUDIT-LOT", "FileName", "AUDIT-MODEL", "OperatorName", "AUDIT", "Result", verdict, "LowerSpecKgf", .4, "PeakLoadKgf", loads.Max(), "SampleCount", loads.Length);
        return Obj("BufferedInspectionData", "Record", record, "Samples", Arr("InspectionSample", samples));
    }
    static object Lot(params object[] inspections)
    {
        return Obj("LotStorageData", "CreatedAt", new DateTime(2026,9,7,11,0,0), "CompletedAt", new DateTime(2026,9,7,12,0,0), "Station", "#1", "LotNumber", "AUDIT-LOT", "FileName", "AUDIT-MODEL", "OperatorName", "AUDIT", "SpecText", "0.40", "Inspections", Arr("BufferedInspectionData", inspections));
    }
    static void Listen(object o, string eventName, Action<object> action)
    {
        var ev = o.GetType().GetEvent(eventName, Flags);
        var ps = ev.EventHandlerType.GetMethod("Invoke").GetParameters().Select(p => Expression.Parameter(p.ParameterType)).ToArray();
        var handler = Expression.Lambda(ev.EventHandlerType, Expression.Invoke(Expression.Constant(action), Expression.Convert(ps[1], typeof(object))), ps).Compile();
        ev.GetAddMethod(true).Invoke(o, new object[]{handler});
    }
    static void Tests()
    {
        var cfg = Config();
        cfg["Station1.Plc.AutoConnect"] = cfg["Station2.Plc.AutoConnect"] = "false";
        cfg["Log.AutoDelete"] = "false";
        cfg["Runtime.Simulation"] = "0";
        cfg["Station1.IpAddress"] = cfg["Station2.IpAddress"] = "127.0.0.1";
        Save(cfg);
        using (var view = (Control)New("StationView"))
        {
            Check(!(bool)Get(Get(view,"txtLotNumber"),"ReadOnly") && !(bool)Get(Get(view,"txtOperator"),"ReadOnly"),"initial LOT/model/operator fields are editable");
            Call(view, "ConfigureGraph", 1);
            var chart = (Chart)Get(view,"chartLoad");
            Check(Math.Abs(chart.ChartAreas[0].AxisX.Maximum - 66) < .0001 && Math.Abs(chart.ChartAreas[0].AxisX.Interval - 6.6) < .0001, "X=60 -> maximum66 and10divisions");
            Check(Math.Abs(chart.ChartAreas[0].AxisY.Maximum / chart.ChartAreas[0].AxisY.Interval - 10) < .0001, "Y10divisions");
            Call(view,"AddLoadSample", .4, (int?)400);
            var result = Call(view,"CompleteInspection");
            Check((bool)Get(result,"IsPass"), "peak equals SPEC => GOOD");
            Check(chart.Series.FindByName("LivePeak") != null, "judgment peak marker exists");
            Call(view,"ResetLoadGraph");
            for (int i=0; i<68; i++) Call(view,"AddLoadSample", .3, (int?)300);
            Check(chart.ChartAreas[0].AxisX.Maximum >= 68, "X automatically expands beyond initial range");
            cfg["Station1.Inspection.ThresholdKgf"] = "0.405"; Save(cfg);
            Call(view,"ConfigureGraph",1); Call(view,"AddLoadSample",.407,(int?)407);
            result=Call(view,"CompleteInspection");
            Check((bool)Get(result,"IsPass") && (double)Get(result,"LowerSpecKgf")==.405,"SPEC=0.405 peak=0.407 => GOOD with exact configured threshold");
            cfg["Station1.Inspection.ThresholdKgf"]="0.40"; cfg["Station1.Graph.YMax"]="2"; cfg["Station1.Graph.YAuto"]="false"; Save(cfg);
            Call(view,"ConfigureGraph",1);
            Check(Math.Abs(chart.ChartAreas[0].AxisY.Maximum-2)<.0001, "YMax=2 YAuto=false actual="+chart.ChartAreas[0].AxisY.Maximum);
            cfg["Station1.Graph.YMin"]="1";
            Call(T("SettingsCatalog"),"Validate",cfg); Save(cfg);
            bool axisError=false;
            try { Call(view,"ConfigureGraph",1); using(var bitmap=new System.Drawing.Bitmap(700,350)) { chart.Size=bitmap.Size; chart.DrawToBitmap(bitmap,chart.ClientRectangle); } }
            catch(Exception ex) { axisError=true; Console.WriteLine("DETAIL invalid-axis "+ex.GetType().Name+": "+ex.Message); }
            Check(!axisError && chart.ChartAreas[0].AxisY.Maximum > chart.ChartAreas[0].AxisY.Minimum, "validated YMin=1 YMax=2 renders without invalid axis");
            cfg["Station1.Graph.YMin"]="0"; cfg["Station1.Graph.YMax"]="0.40"; cfg["Station1.Graph.YAuto"]="true";
            cfg["Station1.Plc.EndRequestValue"]="1";
            bool accepted=true; try {Call(T("SettingsCatalog"),"Validate",cfg);} catch {accepted=false;}
            Check(!accepted,"configuration rejects equal START and END values");
            cfg["Station1.Plc.EndRequestValue"]="0"; cfg["Station1.Plc.Port"]="12345.0";
            Call(T("SettingsCatalog"),"Validate",cfg); Save(cfg);
            object plcSettings=Call(T("PlcGraphSettings"),"Load",1,"127.0.0.1");
            Check((int)Get(plcSettings,"Port")==12345 && cfg["Station1.Plc.Port"]=="12345","validated Port=12345.0 is canonicalized and reloads as12345");
            cfg["Station1.Plc.Port"]="10000"; Save(cfg);
        }
        var data=Path.Combine(Root,"format-data"); var logs=Path.Combine(Root,"format-log");
        var storage=New("InspectionStorageService",data,logs);
        var inspections=new[]{Inspection("GOOD",.401,.602,.5),Inspection("NG",.12,.203)};
        var lot=Lot(inspections);
        var path=(string)Call(storage,"SaveLot",lot);
        var lines=File.ReadAllLines(path);
        Check(lines[0]=="모델명,LOT NO.,작업자,SPEC,생산수량,GOOD,NG,Yield(%)" && lines[1]=="AUDIT-MODEL,AUDIT-LOT,AUDIT,0.40,2,1,1,50.00","CSV summary2rows");
        Check(lines[3]=="NO,판정결과,RAWDATA개수,1,2,3" && lines[4]=="1,GOOD,3,0.401,0.602,0.500" && lines[5]=="2,NG,2,0.120,0.203,","CSV kgf3decimals max-width padding");
        var entry=Obj("LotFileEntry","Date",new DateTime(2026,9,7),"FilePath",path);
        var loaded=(IList)Call(storage,"ReadLotFile",entry,"#1");
        Check(loaded.Count==2 && (double)Get(((IList)Get(loaded[0],"Samples"))[1],"LoadKgf")==.602,"savedCSV parsing preserves kgf");
        using(var view=(Control)New("StationView"))
        {
            Call(view,"ConfigureGraph",1); Call(view,"BindHistory",storage,"#1");
            var grid=(DataGridView)Get(view,"historyResults"); var combo=(ComboBox)Get(view,"cboInspectionNumber");
            grid.Rows.Add("2026-09-07",Path.GetFileName(path)); grid.Rows[0].Tag=entry;
            var task=(Task)Call(view,"LoadReviewFileAsync",0); Wait(()=>task.IsCompleted,"reviewload");
            Check(combo.Items.Count==2 && combo.SelectedIndex==0,"review file loads inspection selector");
            var cellClick=typeof(DataGridView).GetMethod("OnCellClick",Flags);
            cellClick.Invoke(grid,new object[]{new DataGridViewCellEventArgs(0,0)});
            cellClick.Invoke(grid,new object[]{new DataGridViewCellEventArgs(0,0)});
            Wait(()=>combo.Items.Count==2,"double click event review selector");
            Pump(100);
            Check(combo.Items.Count==2 && combo.SelectedIndex==0,"two rapid file clicks update selector without duplicates");
            Call(view,"ShowSelectedReview"); var review=(Chart)Get(view,"m_ReviewChart");
            Check(review.Series[0].Points.Count==3 && review.Series[0].Points[1].YValues[0]==.602,"REVIEW graph correct for inspection1");
            combo.SelectedIndex=1;
            review=(Chart)Get(view,"m_ReviewChart");
            Check(review.Series[0].Points.Count==2 && (string)Get(Get(view,"lblVerdictValue"),"Text")=="NG","select inspection2 updates NG summary and graph together");
            Call(view,"ShowSelectedReview");
            Check(((Chart)Get(view,"m_ReviewChart")).Series[0].Points.Count==2,"REVIEW redraw inspection2");
        }
        var index=Path.Combine(logs,"2026","09","07","Inspection_20260907.csv");
        int before=Directory.GetFiles(data,"*.csv",SearchOption.AllDirectories).Length;
        var retryLot=Lot(Inspection("GOOD",.4)); string retryPath=null;
        using(var locked=new FileStream(index,FileMode.Open,FileAccess.Read,FileShare.None))
        {
            retryPath=(string)Call(storage,"SaveLot",retryLot);
            Check(File.Exists(retryPath) && Get(retryLot,"IndexWarning")!=null && File.Exists(retryPath+".index.pending"),"indexlock leaves committedCSV and durable recoveryjournal, not savefailure");
        }
        int after=Directory.GetFiles(data,"*.csv",SearchOption.AllDirectories).Length;
        var restartedStorage=New("InspectionStorageService",data,logs);
        Check((int)Call(restartedStorage,"RetryPendingInspectionIndexes")==1 && !File.Exists(retryPath+".index.pending"),"new service recovers durable pendingindex after lock released");
        Check((int)Call(restartedStorage,"RetryPendingInspectionIndexes")==0,"repeated indexrecovery is idempotent");
        Call(storage,"SaveLot",retryLot);
        Check(after==before+1 && Directory.GetFiles(data,"*.csv",SearchOption.AllDirectories).Length==before+1,"retry same save does not create duplicateCSV");
        Check(File.ReadAllLines(index).Length==4,"recovered index contains exactly original2 and recovered1 records");

        using(var form=(Form)New("SOFCMeas"))
        {
            var handle=form.Handle;
            ((System.Windows.Forms.Timer)Get(form,"m_LogCleanupTimer")).Stop();
            var view=Get(form,"stationView1"); var reader=Get(form,"m_Station1Reader"); var lotState=Get(form,"m_Station1Lot");
            Check(form.ClientSize.Height==1040,"main client height1040");
            bool denied=false; try {Call(Get(form,"m_SettingPage"),"SaveValues");}catch(UnauthorizedAccessException){denied=true;}
            Check(denied,"OPERATOR cannot save settings");
            Set(lotState,"MaximumInspectionCount",2);
            // These are direct UI event-injection tests, not connection tests.
            Set(reader,"m_WorkerTask",Task.CompletedTask);
            Set(Get(form,"m_Station2Reader"),"m_WorkerTask",Task.CompletedTask);
            Call(view,"SetCollectionState",true);
            foreach(int raw in new[]{500,100,200})
            {
                Call(reader,"ApplyUpdate",Obj("PlcGraphUpdate","ResetGraph",true,"AddSample",true,"LoadRaw",raw,"ReadTime",DateTime.Now));
                Call(reader,"ApplyUpdate",Obj("PlcGraphUpdate","FreezeGraph",true,"ReadTime",DateTime.Now));
            }
            Check((int)Get(lotState,"InspectionCount")==2,"rolling capacity2 after3cycles retains2");
            Check((int)Get(Get(form,"m_Station2Lot"),"InspectionCount")==0,"PLC1 events do not change PLC2 list");
            var retained=Get(lotState,"m_Inspections") as IList;
            Check((string)Get(Get(retained[0],"Record"),"Result")=="NG","rolling removes oldest GOOD");
            Check((int)Get(view,"m_InspectionCount")==2 && (int)Get(view,"m_PassCount")==0 && (int)Get(view,"m_FailCount")==2,"rolling list=2NG and live summary=2,totalGOOD0,NG2");
            var secondReader=Get(form,"m_Station2Reader");
            Call(Get(form,"stationView2"),"SetCollectionState",true);
            Call(secondReader,"ApplyUpdate",Obj("PlcGraphUpdate","ResetGraph",true,"AddSample",true,"LoadRaw",700,"ReadTime",DateTime.Now));
            Call(secondReader,"ApplyUpdate",Obj("PlcGraphUpdate","FreezeGraph",true,"ReadTime",DateTime.Now));
            var save=(Task)Call(form,"HandleLotEndRequestedAsync","STATION-1","#1",view,reader,Get(form,"m_Station1Capture"),lotState,false);
            Wait(()=>save.IsCompleted,"lot save"); if(save.IsFaulted) throw save.Exception;
            Check((int)Get(lotState,"InspectionCount")==0 && Get(reader,"AcquisitionState").ToString()=="WaitStart","LOT save clears own list and returns WaitStart without completiondialog");
            Check((int)Get(Get(form,"m_Station2Lot"),"InspectionCount")==1,"PLC1 LOTEND preserves PLC2 buffered result");
            Check(!(bool)Get(Get(view,"txtLotNumber"),"ReadOnly") && !(bool)Get(Get(view,"txtModel"),"ReadOnly"),"LOT end leaves model and LOT fields editable");
            Console.WriteLine("DETAIL START button while enabled="+Get(Get(view,"btnStart"),"Text"));
            ((IDisposable)reader).Dispose(); ((IDisposable)Get(form,"m_Station2Reader")).Dispose();
        }
        using(var view=(Control)New("StationView"))
        {
            Call(view,"ConfigureGraph",1); Call(view,"BindHistory",storage,"#1");
            int completed=0;
            var ev=view.GetType().GetEvent("SimulationCompleted",Flags);
            var p=Expression.Parameter(ev.EventHandlerType.GetMethod("Invoke").GetParameters()[0].ParameterType);
            Action<object> complete=o=>completed++;
            ev.GetAddMethod(true).Invoke(view,new object[]{Expression.Lambda(ev.EventHandlerType,Expression.Invoke(Expression.Constant(complete),Expression.Convert(p,typeof(object))),p).Compile()});
            var sim=(Task)Call(view,"RunSimulationAsync",1); Wait(()=>sim.IsCompleted,"simulation60samples");
            Check(completed==1 && (int)Get(view,"m_GraphReadCount")==60,"simulation completes60samples and1judgment");
            Call(view,"SetCollectionState",false);
            sim=(Task)Call(view,"RunSimulationAsync",20); Pump(60);
            ((CancellationTokenSource)Get(view,"m_SimulationCancellation")).Cancel(); Wait(()=>sim.IsCompleted,"simulationcancel");
            Check(completed==1 && !(bool)Get(view,"CollectionEnabled"),"simulationcancel does not buffer incomplete inspection");
        }
        PolicyTests();
        NetworkTests();
        FullFlowTests();
    }
    static void FullFlowTests()
    {
        using(var server1=new MockPlc()) using(var server2=new MockPlc()) using(var form=(Form)New("SOFCMeas"))
        {
            var handle=form.Handle; ((System.Windows.Forms.Timer)Get(form,"m_LogCleanupTimer")).Stop();
            var readers=new[]{Get(form,"m_Station1Reader"),Get(form,"m_Station2Reader")};
            var views=new[]{Get(form,"stationView1"),Get(form,"stationView2")};
            var lots=new[]{Get(form,"m_Station1Lot"),Get(form,"m_Station2Lot")};
            try
            {
                for(int i=0;i<2;i++)
                {
                    var settings=Get(readers[i],"Settings");
                    Set(settings,"IpAddress","127.0.0.1");Set(settings,"Port",i==0?server1.Port:server2.Port);Set(settings,"AutoConnect",true);Set(settings,"PollIntervalMilliseconds",100);
                    Call(views[i],"SetCollectionState",true);
                }
                server1.Raw=500;server2.Raw=300; server1.Request=server2.Request=1;
                Wait(()=>((IList)Get(Get(form,"m_Station1Capture"),"Samples")).Count>=3 && ((IList)Get(Get(form,"m_Station2Capture"),"Samples")).Count>=3,"fullFlow firstsamples");
                server1.Request=server2.Request=0;
                Wait(()=>(int)Get(lots[0],"InspectionCount")==1 && (int)Get(lots[1],"InspectionCount")==1,"fullFlow judgments");
                var firstRecord=Get(((IList)Get(lots[0],"m_Inspections"))[0],"Record");
                var secondRecord=Get(((IList)Get(lots[1],"m_Inspections"))[0],"Record");
                Check((string)Get(firstRecord,"Result")=="GOOD" && (string)Get(secondRecord,"Result")=="NG","end-to-end twoTCP PLCs -> WinForms dispatch -> independent GOOD/NG lists");
                int rawReads=server1.RawReads;Pump(250);Check(server1.RawReads==rawReads,"end-to-end END stops raw reads after automatic judgment");
                server1.Raw=600;server1.Request=1;
                Wait(()=>(long)Get(Get(form,"m_Station1Capture"),"CycleNumber")==2 && ((IList)Get(Get(form,"m_Station1Capture"),"Samples")).Count>=2,"fullFlow secondcycle");
                var graph=(Chart)Get(views[0],"chartLoad");
                Check(graph.Series[0].Points.All(p=>p.YValues[0]==.6),"end-to-end nextSTART resets graph and accepts only newcycle samples");
                server1.Request=0;Wait(()=>(int)Get(lots[0],"InspectionCount")==2,"fullFlow cycle2end");
                Set(views[0],"FileName","EDITED-MODEL"); Set(views[0],"LotNumber","EDITED-LOT"); Set(views[0],"OperatorName","EDITED-OPERATOR");
                var sync=Get(Get(form,"m_InspectionStorageService"),"m_SyncRoot");Task save;
                Monitor.Enter(sync);
                try
                {
                    save=(Task)Call(form,"HandleLotEndRequestedAsync","STATION-1","#1",views[0],readers[0],Get(form,"m_Station1Capture"),lots[0],false);
                    Check(!(bool)Get(Get(views[0],"txtLotNumber"),"ReadOnly"),"job fields remain editable while SAVING");
                    Set(views[0],"LotNumber","NEXT-LOT"); Set(views[0],"FileName","NEXT-MODEL"); Set(views[0],"OperatorName","NEXT-OPERATOR");
                    server1.Request=1;int reads=server1.RawReads;Pump(400);
                    Check(Get(readers[0],"AcquisitionState").ToString()=="Saving" && server1.RawReads==reads && (int)Get(lots[0],"InspectionCount")==2,"end-to-end delayedLOT save ignores incomingSTART and keeps snapshot unchanged");
                }
                finally {Monitor.Exit(sync);}
                Wait(()=>save.IsCompleted,"fullFlow savecompletion");
                string savedFile=Directory.GetFiles(Path.Combine(Root,"Data","PLC1"),"*_EDITED-LOT_EDITED-MODEL.csv",SearchOption.AllDirectories).Single();
                Check(File.ReadAllLines(savedFile)[1].StartsWith("EDITED-MODEL,EDITED-LOT,EDITED-OPERATOR,"),"LOTEND captures latest metadata; changes during save cannot alter committed header");
                Check((string)Get(views[0],"LotNumber")=="NEXT-LOT","edits during save remain available for nextLOT");
                var storage=Get(form,"m_InspectionStorageService");
                var savedEntries=(IEnumerable)Call(storage,"GetInspectionLogs",DateTime.Today,DateTime.Today,"#1");
                Check(savedEntries.Cast<object>().Where(r=>(string)Get(r,"DataFileName")==Path.GetFileName(savedFile)).All(r=>(string)Get(r,"LotNumber")=="EDITED-LOT" && (string)Get(r,"FileName")=="EDITED-MODEL"),"CSV and index metadata use the same LOTEND snapshot");
                Check((int)Get(lots[0],"InspectionCount")==0 && (int)Get(lots[1],"InspectionCount")==1,"end-to-end LOT save clears only PLC1; PLC2 unchanged");
                Wait(()=>((IList)Get(Get(form,"m_Station1Capture"),"Samples")).Count>=1 && (bool)Get(Get(form,"m_Station1Capture"),"IsActive"),"fullFlow resumes");
                Check(Get(readers[0],"AcquisitionState").ToString()=="Collecting","end-to-end collection resumes afterLOT save (completiondialog disabled in test)");
            }
            finally {foreach(var reader in readers)((IDisposable)reader).Dispose();}
        }
    }
    static void PolicyTests()
    {
        var policy=T("LicensePolicy");
        Check((bool)Call(policy,"HasMatchingPhysicalAddress",new[]{"00-11-22-33-44-55","66-77-88-99-AA-BB"},new[]{"66778899aabb"}),"any registered MAC permits matching adapter");
        Check(!(bool)Call(policy,"HasMatchingPhysicalAddress",new string[0],new[]{"66778899AABB"}),"empty MAC list rejects");
        var first=new DateTime(2026,9,7,0,0,0,DateTimeKind.Utc);
        Check(Call(policy,"EvaluateUsagePeriod",first,first,first.AddDays(40).AddTicks(-1)).ToString()=="Valid","licensevalid until40days");
        Check(Call(policy,"EvaluateUsagePeriod",first,first,first.AddDays(40)).ToString()=="UsagePeriodExpired","licenseexpires exactly40days");
        Check(Call(policy,"EvaluateUsagePeriod",first,first.AddDays(10),first.AddDays(9)).ToString()=="SystemClockRollbackDetected","license detects backwardclock");
        var logRoot=Path.Combine(Root,"cleanup-log"); var dataRoot=Path.Combine(Root,"cleanup-data");
        var oldLog=Path.Combine(logRoot,"2026","01","01"); var oldData=Path.Combine(dataRoot,"2026","01","01");
        Directory.CreateDirectory(oldLog); Directory.CreateDirectory(oldData);
        File.WriteAllText(Path.Combine(oldLog,"audit.log"),"temporary test log");
        File.WriteAllText(Path.Combine(oldData,"audit.csv"),"temporary test data");
        var cleanup=New("LogFolderCleanupService",logRoot,dataRoot);
        Call(cleanup,"DeleteExpiredDateDirectories",30,new DateTime(2026,9,7));
        Check(!Directory.Exists(oldLog) && File.Exists(Path.Combine(oldData,"audit.csv")),"automatic cleanup removes only expired testlogs; testData survives");
        bool rejects=false;try{New("LogFolderCleanupService",dataRoot,dataRoot);}catch(TargetInvocationException ex){rejects=ex.InnerException is InvalidOperationException;}
        Check(rejects,"cleanup rejects protected Data as deletionroot");
        var log=New("ApplicationLogService",Path.Combine(Root,"log-test"),600L);
        for(int i=0;i<8;i++)Call(log,"Info",i%2==0?"STATION-1":"STATION-2","event=AUDIT_SAMPLE value="+i);
        var files=Directory.GetFiles(Path.Combine(Root,"log-test"),"*.log",SearchOption.AllDirectories);
        Check(files.Length>1 && files.All(f=>new FileInfo(f).Length<=600),"logs save and rotate at size limit");
        var texts=files.SelectMany(File.ReadAllLines).ToArray();
        Check(texts.Any(x=>x.Contains("[STATION-1]")) && texts.Any(x=>x.Contains("[STATION-2]")) && texts.All(x=>x.Contains("[SESSION=") && x.Contains("[SEQ=")),"logs identify both stations/session/sequence");
        var badRoot=Path.Combine(Root,"log-unwritable-root"); File.WriteAllText(badRoot,"test file blocks directory creation");
        var badLog=New("ApplicationLogService",badRoot,600L);
        Call(badLog,"Info","AUDIT","logfailure");
        Check(Get(badLog,"LastError")!=null && (long)Get(badLog,"FailureCount")>0,"log failures are tracked without stopping collection");
        using(var form=(Form)New("SOFCMeas",null,badLog,null))
        {
            var handle=form.Handle; ((System.Windows.Forms.Timer)Get(form,"m_LogCleanupTimer")).Stop();
            Call(form,"RefreshLogStorageStatus");
            Check(((Label)Get(form,"m_LogStorageStatus")).Text.Contains("오류"),"runtime logfailure displayed as persistent UI warning");
            File.Delete(badRoot); Call(badLog,"Info","AUDIT","logrecovered"); Pump(50);
            Check(Get(badLog,"LastError")==null && ((Label)Get(form,"m_LogStorageStatus")).Text.Contains("복구"),"recovered logstorage retains visible failure history");
        }
    }
    static void NetworkTests()
    {
        using(var server1=new MockPlc()) using(var server2=new MockPlc())
        {
            var readers=new object[2]; var counts=new int[2];
            for(int i=0;i<2;i++)
            {
                int n=i;
                var settings=Call(T("PlcGraphSettings"),"Load",i+1,"127.0.0.1");
                Set(settings,"IpAddress","127.0.0.1"); Set(settings,"Port",i==0?server1.Port:server2.Port);
                Set(settings,"AutoConnect",false); Set(settings,"PollIntervalMilliseconds",100); Set(settings,"TimeoutMilliseconds",500);
                readers[i]=New("PlcGraphReader",settings);
                Listen(readers[i],"LoadSampleReceived",e=>Interlocked.Increment(ref counts[n]));
                Listen(readers[i],"LogMessage",e=>{if(Get(e,"Level").ToString()=="Error") Console.WriteLine("MOCK_ERROR "+Get(e,"Message")+" "+Get(e,"Error"));});
                Call(readers[i],"SetCollectionEnabled",true); Call(readers[i],"Start");
                Check(Get(readers[i],"m_WorkerTask")==null,"AutoConnect=false suppresses automatic connection PLC"+(i+1));
                Call(readers[i],"Start",true);
            }
            try
            {
                Wait(()=>server1.RequestReads>=2 && server2.RequestReads>=2,"loopback request reads");
                Check(true,"manualSTART connects both PLCs with AutoConnect=false");
                Check(server1.RawReads==0 && server2.RawReads==0,"WAIT_START reads requests but no LOAD_RAW on bothPLCs");
                server1.Request=1; server2.Request=1; server2.Raw=800;
                Wait(()=>counts[0]>=3 && counts[1]>=3,"two station collection");
                Check(server1.RawReads>0 && server2.RawReads>0,"both PLCs collect duringSTART");
                server1.Request=0;
                Wait(()=>Get(readers[0],"AcquisitionState").ToString()=="Evaluating","END to evaluating");
                int c1=counts[0], c2=counts[1], raw1=server1.RawReads;
                server1.Request=1; Pump(400);
                Check(counts[0]==c1 && server1.RawReads==raw1 && counts[1]>c2,"EVALUATING blocks samples/raw while otherPLC continues");
                Call(readers[0],"CompleteEvaluation"); Wait(()=>counts[0]>c1,"post-evaluation restart");
                server1.Request=0; Wait(()=>Get(readers[0],"AcquisitionState").ToString()=="Evaluating","secondEND"); Call(readers[0],"CompleteEvaluation");
                Call(readers[0],"SetSaving",true); server1.Request=1;
                raw1=server1.RawReads; c1=counts[0]; Pump(400);
                Check(server1.RawReads==raw1 && counts[0]==c1,"SAVING ignores START and LOAD_RAW");
                Call(readers[0],"SetSaving",false); Wait(()=>counts[0]>c1,"post-save request");
                Console.WriteLine("OBSERVED held START from duringSAVING becomes a new cycle immediately on resume (no new edge required)");
                Call(readers[0],"SetCollectionEnabled",false); Pump(200); raw1=server1.RawReads; Pump(300);
                Check(server1.RawReads==raw1,"softwareSTOP preventsLOAD_RAW");
            }
            finally {foreach(var reader in readers) if(reader!=null)((IDisposable)reader).Dispose();}
        }
    }
    sealed class MockPlc : IDisposable
    {
        readonly TcpListener listener;
        readonly List<TcpClient> clients=new List<TcpClient>();
        public volatile int Request, Raw=500, RequestReads, RawReads;
        public int Port {get;private set;}
        public MockPlc()
        {
            listener=new TcpListener(IPAddress.Loopback,0); listener.Start(); Port=((IPEndPoint)listener.LocalEndpoint).Port;
            Task.Run(async()=>{try{while(true){var c=await listener.AcceptTcpClientAsync(); lock(clients)clients.Add(c); Handle(c);}}catch(ObjectDisposedException){}catch(SocketException){}});
        }
        static async Task<byte[]> Read(NetworkStream s,int count)
        {var b=new byte[count]; int n=0; while(n<count){int r=await s.ReadAsync(b,n,count-n); if(r==0)throw new IOException("closed");n+=r;}return b;}
        async void Handle(TcpClient c)
        {
            try
            {
                var s=c.GetStream();
                while(true)
                {
                    var h=await Read(s,9); var b=await Read(s,h[7]+256*h[8]); int command=b[2]+256*b[3];
                    var values=new List<byte>();
                    if(command==0x401)
                    {
                        int address=b[6]+256*b[7]+65536*b[8], count=b[10]+256*b[11];
                        if(address==810)Interlocked.Increment(ref RequestReads); else Interlocked.Increment(ref RawReads);
                        for(int i=0;i<count;i++){int v=address+i==810?Request:address+i==801?(Raw&65535):address+i==802?(Raw>>16):0; values.Add((byte)v);values.Add((byte)(v>>8));}
                    }
                    else if(command==0x403)
                    {
                        int words=b[6], dwords=b[7], offset=8;
                        for(int i=0;i<words+dwords;i++,offset+=4)
                        {
                            int address=b[offset]+256*b[offset+1]+65536*b[offset+2], v=address==810?Request:Raw;
                            if(address==810)Interlocked.Increment(ref RequestReads); else Interlocked.Increment(ref RawReads);
                            values.Add((byte)v);values.Add((byte)(v>>8)); if(i>=words){values.Add((byte)(v>>16));values.Add((byte)(v>>24));}
                        }
                    }
                    else throw new Exception("Unsupported mock command "+command.ToString("X")+" frame="+BitConverter.ToString(b));
                    byte[] response=new byte[11+values.Count];response[0]=0xd0;Array.Copy(h,2,response,2,5);response[7]=(byte)(values.Count+2);response[8]=(byte)((values.Count+2)>>8);values.CopyTo(response,11);
                    await s.WriteAsync(response,0,response.Length);
                }
            }
            catch(IOException){}catch(ObjectDisposedException){}catch(Exception ex){Console.WriteLine("MOCK_SERVER_ERROR "+ex.Message);}
        }
        public void Dispose(){listener.Stop();lock(clients)foreach(var c in clients)c.Close();}
    }
    [STAThread] static int Main(string[] args)
    {
        Root=Path.GetFullPath(args[0]); Directory.CreateDirectory(Root);
        Environment.SetEnvironmentVariable("SOFCMEAS_ROOT",Root);
        string bin=Path.GetFullPath(args[1]);
        AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{var p=Path.Combine(bin,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
        App=Assembly.LoadFrom(Path.Combine(bin,"SOFCMeas.exe"));
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        // Keep a UI handle alive while testing short-lived controls and async click handlers.
        var messageHost=new Form(); var messageHandle=messageHost.Handle;
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        AppDomain.CurrentDomain.UnhandledException+=(s,e)=>{var ex=e.ExceptionObject as Exception;Console.WriteLine("UNHANDLED "+(ex==null?"unknown":ex.GetType().FullName+" "+ex.Message+" "+ex.StackTrace));};
        int exitCode=1;
        messageHost.BeginInvoke((Action)(()=>
        {
            try {Tests(); Console.WriteLine("AUDIT_DONE checks="+Checks+" failures="+Failures+" reproduced="+Findings);exitCode=Failures==0?0:2;}
            catch(Exception ex){Console.WriteLine("HARNESS_ERROR "+ex);exitCode=1;}
            finally {Application.ExitThread();}
        }));
        Application.Run(); messageHost.Dispose(); return exitCode;
    }
}
