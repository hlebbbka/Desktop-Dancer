using System.Windows;
namespace MusicDancer;
public partial class MainWindow
{
    private SpotifyLyricsReader? nativeLyrics;
    private void InitializeSpotifyLyrics(){StateChanged+=(_,_)=>RefreshSpotifyLyrics();Closed+=(_,_)=>nativeLyrics?.Dispose();}
    private void SpotifyLyrics_Changed(object sender,RoutedEventArgs e)
    {
        if(LyricsLayer==null)return;
        if(SpotifyLyricsMode.IsChecked==true){
            nativeLyrics??=new SpotifyLyricsReader(frame=>{
                if(Dispatcher.HasShutdownStarted)return;
                Dispatcher.BeginInvoke(new Action(()=>{
                    if(SpotifyLyricsMode.IsChecked!=true||sceneFullscreen||WindowState==WindowState.Minimized)return;
                    if(profileTrail)profileLyricSamples.Add(new(profileLyricClock.Elapsed.TotalSeconds,frame.Index,sceneFullscreen,frame.Current.Length));
                    AnimateLyricFrame(frame);
                    LyricsSourceStatus.Text=frame.Current.Length>0?T("Текст синхронизирован"):T("Текст не синхронизирован");
                    LyricsLayer.Visibility=frame.Current.Length>0?Visibility.Visible:Visibility.Collapsed;
                }));
            });
        }else{LyricsLayer.Visibility=Visibility.Collapsed;LyricsPrevious.Text=LyricsCurrent.Text=LyricsNext.Text=LyricsWhiteGlow.Text=LyricsLeaving.Text="";shownLyric=null;}
        RefreshSpotifyLyrics();
    }
    private void RefreshSpotifyLyrics()
    {
        bool active=SpotifyLyricsMode.IsChecked==true&&!sceneFullscreen&&WindowState!=WindowState.Minimized;
        LyricsSourceStatus.Text=T("Текст не синхронизирован");
        if(!active){LyricsLayer.Visibility=Visibility.Collapsed;shownLyric=null;}
        nativeLyrics?.RequestRefresh();
        nativeLyrics?.SetEnabled(active);
    }
}
