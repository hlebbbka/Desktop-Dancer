using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
namespace MusicDancer;
public partial class MainWindow
{
    private readonly bool profileTrail = Environment.GetCommandLineArgs().Contains("--profile-trail");
    private readonly bool profileLegacyBounds = Environment.GetCommandLineArgs().Contains("--profile-legacy-bounds");
    private readonly bool profileNoChroma = Environment.GetCommandLineArgs().Contains("--profile-no-chroma");
    private sealed record ProfileLyricSample(double Seconds,int Index,bool Fullscreen,int Chars);
    private readonly List<ProfileLyricSample> profileLyricSamples=new();
    private readonly Stopwatch profileLyricClock=Stopwatch.StartNew();
    private readonly List<double> profileFrames = new(), profileCpu = new();
    private double profilePrevious; private bool profileEnteredFullscreen,profileExitedFullscreen;
    private int profileLyricAnimationIndex=-1;
    private bool profileFinishing, profileLifecycleStarted, profileMouseSet,profileLyricsSet;
    private void ProfileFrame(double t, long start)
    {
        if (!profileTrail || profileFinishing) return;
        if(IsDemoRecording)RecordDemoFrame(t);
        if(!profileLyricsSet&&Environment.GetCommandLineArgs().Contains("--profile-lyrics")){profileLyricsSet=true;SpotifyLyricsMode.IsChecked=true;}
        if(!profileMouseSet&&Environment.GetCommandLineArgs().Contains("--profile-mouse")){profileMouseSet=true;WanderingTrail.IsChecked=false;HideTrail.IsChecked=false;}
        if(Environment.GetCommandLineArgs().Contains("--profile-fullscreen")){
            if(t>5&&!profileEnteredFullscreen){profileEnteredFullscreen=true;WanderingTrail.IsChecked=!Environment.GetCommandLineArgs().Contains("--profile-mouse");Fullscreen_Click(this,new RoutedEventArgs());}
            if(t>20&&!profileExitedFullscreen&&!Environment.GetCommandLineArgs().Contains("--profile-fullscreen-stay")){profileExitedFullscreen=true;ExitFullscreen_Click(this,new RoutedEventArgs());}
        }
        if(t>3&&!profileLifecycleStarted&&Environment.GetCommandLineArgs().Contains("--profile-lifecycle")){profileLifecycleStarted=true;RunProfileLifecycle();}
        if(t>4&&Environment.GetCommandLineArgs().Contains("--profile-lyric-animation")){
            SpotifyLyricsMode.IsChecked=false;
            int index=(int)(t-4);
            if(index!=profileLyricAnimationIndex){profileLyricAnimationIndex=index;
                string[] preview={"A short lyric line","A longer lyric line that smoothly changes size and moves upward","A different line to check the white fill and the soft glow"};
                AnimateLyricFrame(new SpotifyLyricFrame(preview[index%3],preview[(index+1)%3],preview[(index+2)%3],"Preview",index));
            }
        }
        if (t > 3)
        {
            if (profilePrevious > 0) profileFrames.Add((t-profilePrevious)*1000);
            profileCpu.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            profilePrevious=t;
            mousePoint = new Point(Stage.ActualWidth*.5+Math.Sin(t)*Stage.ActualWidth*.48,Stage.ActualHeight*.5+Math.Cos(t*1.3)*Stage.ActualHeight*.48); MoveTubes(mousePoint.Value);
        }
        if (t > (Environment.GetCommandLineArgs().Contains("--profile-long") ? 65 : 38)) FinishProfile();
    }
    private async void FinishProfile()
    {
        profileFinishing=true;
        string? browser=null;
        try { browser=await TubeBrowser.ExecuteScriptAsync("JSON.stringify(window.dancerMetrics?.())").WaitAsync(TimeSpan.FromSeconds(3)); } catch { }
        var blocks=Enumerable.Range(0,7).Select(i=>new{block=i,frames=profileFrames.Skip(i*profileFrames.Count/7).Take(profileFrames.Count/7).ToArray()}).ToArray();
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, Environment.GetCommandLineArgs().Contains("--profile-mouse")?"profile-mouse-clock.json":nativeTrailClockEnabled?"profile-native-clock.json":Environment.GetCommandLineArgs().Contains("--profile-unpaced")?"profile-unpaced.json":Environment.GetCommandLineArgs().Contains("--profile-integrated")?"profile-integrated.json":Environment.GetCommandLineArgs().Contains("--profile-webgl")?"profile-webgl.json":Environment.GetCommandLineArgs().Contains("--profile-browser-background-defaults")?"profile-background-default.json":profileLegacyBounds?"profile-legacy.json":profileNoChroma?"profile-no-chroma.json":"profile-full.json"),JsonSerializer.Serialize(new{blocks,cpu=profileCpu,browser,lyricSamples=profileLyricSamples,lyricsChars=LyricsCurrent.Text.Length,lyricsStatus=LyricsSourceStatus.Text,gc=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)},heap=GC.GetTotalMemory(false)}));
        Close();
    }
    private async void RunProfileLifecycle()
    {
        await Task.Delay(17000);HideTrail.IsChecked=true;
        await Task.Delay(2000);HideTrail.IsChecked=false;WanderingTrail.IsChecked=true;
        await Task.Delay(7000);WindowState=WindowState.Minimized;
        await Task.Delay(2000);WindowState=WindowState.Maximized;
    }
}
