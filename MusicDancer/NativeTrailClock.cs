using System.Windows;
namespace MusicDancer;
public partial class MainWindow
{
    private readonly bool nativeTrailClockEnabled = !Environment.GetCommandLineArgs().Contains("--profile-browser-clock");
    private TrailFrameClock? trailFrameClock;
    private int trailFramePending;
    private bool trailClockActive;
    private void ConfigureTrailClock()
    {
        trailFrameClock??=new TrailFrameClock(()=>{
            if(System.Threading.Interlocked.CompareExchange(ref trailFramePending,1,0)!=0)return;
            if(Dispatcher.HasShutdownStarted){System.Threading.Interlocked.Exchange(ref trailFramePending,0);return;}
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render,new Action(()=>{
                if(!trailClockActive||!tubesReady||WindowState==WindowState.Minimized){System.Threading.Interlocked.Exchange(ref trailFramePending,0);return;}
                try{if(pointerDirty&&WanderingTrail.IsChecked!=true&&HideTrail.IsChecked!=true){
                    pointerDirty=false;
                    TubeBrowser.CoreWebView2.PostWebMessageAsJson(FormattableString.Invariant($"{{\"type\":\"frame\",\"pointer\":{{\"x\":{pendingPointer.X},\"y\":{pendingPointer.Y}}}}}"));
                } else if(WanderingTrail.IsChecked==true)TubeBrowser.CoreWebView2.PostWebMessageAsJson(FormattableString.Invariant($"{{\"type\":\"frame\",\"bassLevel\":{trailBassLevel}}}"));
                else TubeBrowser.CoreWebView2.PostWebMessageAsJson("{\"type\":\"frame\"}");}
                catch(Exception e)when(e is InvalidOperationException or System.Runtime.InteropServices.COMException){System.Threading.Interlocked.Exchange(ref trailFramePending,0);}
            }));
        });
        Closed+=(_,_)=>trailFrameClock?.Dispose();
    }
    private bool HandleClockMessage(string message)
    {
        if(message=="clock:frame-ready"){System.Threading.Interlocked.Exchange(ref trailFramePending,0);return true;}
        if(message is "clock:active" or "clock:idle"){
            trailClockActive=message=="clock:active"&&WindowState!=WindowState.Minimized;
            trailFrameClock?.SetActive(trailClockActive);return true;
        }
        return false;
    }
}
