using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
namespace MusicDancer;
public partial class MainWindow
{
    private bool sceneFullscreen;
    private WindowState beforeFullscreen;
    private ResizeMode beforeFullscreenResize;
    private WindowChrome? beforeFullscreenChrome;
    private void Fullscreen_Click(object sender,RoutedEventArgs e)
    {
        if(sceneFullscreen)return;
        sceneFullscreen=true; RefreshSpotifyLyrics(); InitParticles(); RebuildEqualizer(); UpdateTrailFullscreen(); BpmBadge.Margin=new Thickness(22,22,66,22);
        beforeFullscreen=WindowState; beforeFullscreenResize=ResizeMode;
        beforeFullscreenChrome=WindowChrome.GetWindowChrome(this);
        ScenePanels.Children.Remove(SceneFrame);
        SceneFrame.Margin=new Thickness(0);SceneFrame.CornerRadius=new CornerRadius(0);
        FullscreenHost.Children.Insert(0,SceneFrame);
        DanceContent.Visibility=Visibility.Collapsed;
        FullscreenHost.Visibility=Visibility.Visible;
        WindowState=WindowState.Normal;
        WindowChrome.SetWindowChrome(this,null);ResizeMode=ResizeMode.NoResize;
        WindowState=WindowState.Maximized;
        PreviewKeyDown+=Fullscreen_KeyDown;
    }
    private void Fullscreen_KeyDown(object sender,KeyEventArgs e){if(e.Key==Key.Escape){ExitFullscreen_Click(sender,new RoutedEventArgs());e.Handled=true;}}
    private void ExitFullscreen_Click(object sender,RoutedEventArgs e)
    {
        if(!sceneFullscreen)return;
        sceneFullscreen=false; RefreshSpotifyLyrics(); InitParticles(); RebuildEqualizer(); UpdateTrailFullscreen(); BpmBadge.Margin=new Thickness(22);
        PreviewKeyDown-=Fullscreen_KeyDown;
        FullscreenHost.Children.Remove(SceneFrame);
        SceneFrame.Margin=new Thickness(0,0,18,0);SceneFrame.CornerRadius=new CornerRadius(18);
        ScenePanels.Children.Insert(0,SceneFrame);
        FullscreenHost.Visibility=Visibility.Collapsed;
        DanceContent.Visibility=Visibility.Visible;
        WindowState=WindowState.Normal;
        ResizeMode=beforeFullscreenResize;WindowChrome.SetWindowChrome(this,beforeFullscreenChrome);
        WindowState=beforeFullscreen;
    }
}
