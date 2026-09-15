using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SOFCMeas;
using MainForm = global::SOFCMeas.SOFCMeas;
class SplashSmoke
{
 [STAThread] static void Main()
 {
  Application.EnableVisualStyles();
  var type = typeof(MainForm).Assembly.GetType("SOFCMeas.ProgressSplash");
  for(int i=0;i<3;i++)
  {
   var splash = Activator.CreateInstance(type, BindingFlags.Instance|BindingFlags.NonPublic, null, new object[]{"진행 창 테스트"}, null);
   var report = type.GetMethod("Report", BindingFlags.Instance|BindingFlags.NonPublic);
   foreach(int value in new[]{0,35,65,100}) report.Invoke(splash,new object[]{value,"처리 중"});
   var progress = (ProgressBar)type.GetField("progress",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(splash);
   int actual = (int)progress.Invoke((Func<int>)(()=>progress.Value));
   if(actual!=100) throw new Exception("Progress failed");
   ((IDisposable)splash).Dispose(); ((IDisposable)splash).Dispose();
   var thread = (Thread)type.GetField("thread",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(splash);
   if(thread.IsAlive) throw new Exception("Splash thread still running");
  }
  Console.WriteLine("Splash creation/progress updates/repeated disposal/thread shutdown: PASS");
 }
}
