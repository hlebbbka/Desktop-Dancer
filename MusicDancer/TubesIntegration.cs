using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
namespace MusicDancer;
public partial class MainWindow
{
    private bool tubesReady, pointerDirty, pointerInFlight; private Point pendingPointer; private readonly System.Threading.SemaphoreSlim effectsLifecycle = new(1,1);
    private async Task InitializeTubes()
    {
        try
        {
            var folder=IsDemoRecording?Path.Combine(AppContext.BaseDirectory,"demo-webview"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Tanctspol","WebView");
            var environment=await CoreWebView2Environment.CreateAsync(null,folder,new CoreWebView2EnvironmentOptions((Environment.GetCommandLineArgs().Contains("--profile-integrated") ? "--force_low_power_gpu" : "--force_high_performance_gpu") +
                (Environment.GetCommandLineArgs().Contains("--profile-browser-background-defaults") ? "" :
                " --disable-renderer-backgrounding --disable-background-timer-throttling --disable-backgrounding-occluded-windows --disable-features=CalculateNativeWinOcclusion") + (nativeTrailClockEnabled || Environment.GetCommandLineArgs().Contains("--profile-unpaced") ? " --disable-gpu-vsync --disable-frame-rate-limit" : "")));
            await TubeBrowser.EnsureCoreWebView2Async(environment);
            if(nativeTrailClockEnabled){
                ConfigureTrailClock();
                await TubeBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.__dancerNativeFrames=true;");
            }
            if(Environment.GetCommandLineArgs().Contains("--profile-unpaced"))
                await TubeBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.__dancerUnpaced=true;");
            if(Environment.GetCommandLineArgs().Contains("--profile-integrated"))
                await TubeBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.__dancerLowPower=true;");
            if(Environment.GetCommandLineArgs().Contains("--profile-webgl"))
                await TubeBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.__dancerForceWebGL=true;");
            TubeBrowser.CoreWebView2.Settings.AreDefaultContextMenusEnabled=false;
            TubeBrowser.CoreWebView2.Settings.IsStatusBarEnabled=false;
            TubeBrowser.CoreWebView2.SetVirtualHostNameToFolderMapping("dancer.local",Path.Combine(AppContext.BaseDirectory,"Web"),CoreWebView2HostResourceAccessKind.DenyCors);
            TubeBrowser.CoreWebView2.WebMessageReceived+=(_,e)=> {var message=e.TryGetWebMessageAsString(); if(HandleClockMessage(message))return; if(message=="pointer-ready") { pointerInFlight=false; FlushTubesPointer(); return; } if(message.StartsWith("gpu:")) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"gpu-info.json"),message.Substring(4)); return; } tubesReady=message=="ready"; File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"tubes-status.txt"),message); if(tubesReady) { UpdateWanderingTrail(); UpdateTrailFullscreen(); _ = UpdateEffectsActivity(); }};
            TubeBrowser.Source=new Uri("https://dancer.local/index.html"+(profileTrail?"?profile":""));
        }
        catch(Exception e) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"tubes-status.txt"),e.ToString()); }
    }
    private async Task UpdateEffectsActivity()
    {
        await effectsLifecycle.WaitAsync();
        try
        {
            if (TubeBrowser.CoreWebView2 == null || !tubesReady) return;
            if (WindowState == WindowState.Minimized)
            {
                trailClockActive=false;trailFrameClock?.SetActive(false);
                mousePoint = null; LeaveTubes();
                TubeBrowser.Visibility = Visibility.Hidden;
                await TubeBrowser.ExecuteScriptAsync("window.dancerActive(false)");
                bool suspended = await TubeBrowser.CoreWebView2.TrySuspendAsync();
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"effects-activity.txt"), suspended ? "suspended" : "render-loop-stopped");
            }
            else
            {
                TubeBrowser.CoreWebView2.Resume();
                TubeBrowser.Visibility = Visibility.Visible;
                await TubeBrowser.ExecuteScriptAsync("window.dancerActive(true)");
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"effects-activity.txt"), "active");
            }
        }
        catch(Exception e) when (e is ObjectDisposedException || e is InvalidOperationException || e is System.Runtime.InteropServices.COMException) { }
        finally { effectsLifecycle.Release(); }
    }
    private void MoveTubes(Point p) { if(HideTrail.IsChecked==true||WanderingTrail.IsChecked==true)return; pendingPointer = p; pointerDirty = true; }
    private void FlushTubesPointer()
    {
        if(nativeTrailClockEnabled && trailClockActive)return;
        if (HideTrail.IsChecked==true || !pointerDirty || pointerInFlight || !tubesReady || WindowState == WindowState.Minimized) return;
        pointerDirty = false;
        try { pointerInFlight=true; TubeBrowser.CoreWebView2.PostWebMessageAsJson(FormattableString.Invariant($"{{\"type\":\"move\",\"x\":{pendingPointer.X},\"y\":{pendingPointer.Y}}}")); }
        catch(Exception e) when (e is ObjectDisposedException || e is InvalidOperationException || e is System.Runtime.InteropServices.COMException) { pointerInFlight=false; }
    }
    private void LeaveTubes()
    {
        pointerDirty = false;
        if (!tubesReady) return;
        try { TubeBrowser.CoreWebView2.PostWebMessageAsJson("{\"type\":\"leave\"}"); }
        catch(Exception e) when (e is ObjectDisposedException || e is InvalidOperationException || e is System.Runtime.InteropServices.COMException) { }
    }
    private void HideTrail_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        if(WanderingTrail==null)return;
        bool hidden=HideTrail.IsChecked==true;
        if(hidden){trailClockActive=false;trailFrameClock?.SetActive(false);}
        WanderingTrail.IsEnabled=!hidden;
        if(hidden)WanderingTrail.IsChecked=false;
        UpdateWanderingTrail();
    }
    private void WanderingTrail_Changed(object sender, System.Windows.RoutedEventArgs e) => UpdateWanderingTrail();
    private void UpdateWanderingTrail()
    {
        pointerDirty=false;
        if(!tubesReady)return;
        var p=mousePoint??new Point(Stage.ActualWidth*.5,Stage.ActualHeight*.5);
        try { TubeBrowser.CoreWebView2.PostWebMessageAsJson(System.Text.Json.JsonSerializer.Serialize(new {type="wander",hidden=HideTrail.IsChecked==true,enabled=WanderingTrail.IsChecked==true,inside=mousePoint!=null,x=p.X,y=p.Y})); }
        catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException) { }
    }
    private void BoostWanderingTrail()
    {
        if(!tubesReady||WanderingTrail.IsChecked!=true)return;
        try { TubeBrowser.CoreWebView2.PostWebMessageAsJson("{\"type\":\"bass\"}"); }
        catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException) { }
    }
    private double trailBassLevel,lastTrailBassSent;
    private void UpdateWanderingBass(double level,double time)
    {
        trailBassLevel=Math.Clamp(level,0,1);
        // Native pacing carries the current envelope in its existing frame packet.
        if(nativeTrailClockEnabled||!tubesReady||WanderingTrail.IsChecked!=true||time-lastTrailBassSent<1d/30)return;
        lastTrailBassSent=time;
        try { TubeBrowser.CoreWebView2.PostWebMessageAsJson(FormattableString.Invariant($"{{\"type\":\"bass-level\",\"level\":{trailBassLevel}}}")); }
        catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException) { }
    }
    private void UpdateTrailFullscreen()
    {
        if(!tubesReady)return;
        try { TubeBrowser.CoreWebView2.PostWebMessageAsJson(sceneFullscreen ? "{\"type\":\"fullscreen\",\"enabled\":true}" : "{\"type\":\"fullscreen\",\"enabled\":false}"); }
        catch(Exception ex) when(ex is InvalidOperationException or System.Runtime.InteropServices.COMException) { }
    }
}
