using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace MusicDancer;
public partial class MainWindow
{
    private SpotifyLyricFrame? shownLyric;
    private readonly SolidColorBrush lyricPreviousBrush=new(Color.FromArgb(101,255,255,255));
    private bool lyricBrushesReady;
    private static readonly IEasingFunction lyricEase = new CubicEase{EasingMode=EasingMode.EaseOut};
    private static void LyricAnimate(DependencyObject target,DependencyProperty property,double from,double to,double seconds)
    {
        var animation=new DoubleAnimation(from,to,TimeSpan.FromSeconds(seconds)){EasingFunction=lyricEase};
        if(target is Animatable freezable)freezable.BeginAnimation(property,animation);
        else if(target is UIElement element)element.BeginAnimation(property,animation);
    }
    private void PrepareLyricBrushes()
    {
        if(lyricBrushesReady)return;lyricBrushesReady=true;LyricsPrevious.Foreground=lyricPreviousBrush;
        LyricsCurrent.Foreground=new SolidColorBrush(Color.FromArgb(244,255,255,255));
        LyricsWhiteGlow.Foreground=Brushes.White;
    }
    private static Point LyricCenter(FrameworkElement element,UIElement relative)=>element.TranslatePoint(new Point(element.ActualWidth*.5,element.ActualHeight*.5),relative);
    private void AnimateLyricFrame(SpotifyLyricFrame frame)
    {
        PrepareLyricBrushes();
        if(shownLyric?.Current==frame.Current&&shownLyric.Index==frame.Index&&shownLyric.Previous==frame.Previous&&shownLyric.Next==frame.Next)return;
        bool visible=LyricsLayer.Visibility==Visibility.Visible&&shownLyric?.Current.Length>0;
        bool forward=visible&&frame.Index==shownLyric!.Index+1&&shownLyric.Next==frame.Current&&shownLyric.Current==frame.Previous;
        Point oldCurrent=visible?LyricCenter(LyricsCurrentRow,Stage):new(),oldNext=visible?LyricCenter(LyricsNext,Stage):new();
        Point oldPrevious=visible?LyricsPrevious.TranslatePoint(new Point(),Stage):new();
        string leaving=LyricsPrevious.Text;double leavingWidth=LyricsPrevious.ActualWidth;
        LyricsPrevious.Text=frame.Previous;LyricsCurrent.Text=LyricsWhiteGlow.Text=frame.Current;LyricsNext.Text=frame.Next;
        shownLyric=frame;
        if(frame.Current.Length==0){LyricsLeaving.Opacity=0;return;}
        LyricsLayer.Visibility=Visibility.Visible;
        LyricsPreviousMove.BeginAnimation(TranslateTransform.YProperty,null);LyricsPreviousMove.Y=0;
        LyricsCurrentMove.BeginAnimation(TranslateTransform.YProperty,null);LyricsCurrentMove.Y=0;
        LyricsPreviousScale.BeginAnimation(ScaleTransform.ScaleXProperty,null);LyricsPreviousScale.BeginAnimation(ScaleTransform.ScaleYProperty,null);LyricsPreviousScale.ScaleX=LyricsPreviousScale.ScaleY=1;
        LyricsCurrentScale.BeginAnimation(ScaleTransform.ScaleXProperty,null);LyricsCurrentScale.BeginAnimation(ScaleTransform.ScaleYProperty,null);LyricsCurrentScale.ScaleX=LyricsCurrentScale.ScaleY=1;
        LyricsNextMove.BeginAnimation(TranslateTransform.YProperty,null);LyricsNextMove.Y=0;
        LyricsLayer.UpdateLayout();
        double currentOffset=forward?oldNext.Y-LyricCenter(LyricsCurrentRow,Stage).Y:18;
        double previousOffset=forward?oldCurrent.Y-LyricCenter(LyricsPrevious,Stage).Y:12;
        LyricAnimate(LyricsCurrentMove,TranslateTransform.YProperty,currentOffset,0,.28);
        LyricAnimate(LyricsPreviousMove,TranslateTransform.YProperty,previousOffset,0,.28);
        LyricAnimate(LyricsNextMove,TranslateTransform.YProperty,26,0,.28);
        if(forward){
            LyricAnimate(LyricsCurrentScale,ScaleTransform.ScaleXProperty,19d/30,1,.28);LyricAnimate(LyricsCurrentScale,ScaleTransform.ScaleYProperty,19d/30,1,.28);
            LyricAnimate(LyricsPreviousScale,ScaleTransform.ScaleXProperty,30d/19,1,.28);LyricAnimate(LyricsPreviousScale,ScaleTransform.ScaleYProperty,30d/19,1,.28);
        }
        lyricPreviousBrush.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(forward?Color.FromArgb(244,255,255,255):Color.FromArgb(101,255,255,255),Color.FromArgb(101,255,255,255),TimeSpan.FromSeconds(.28)){EasingFunction=lyricEase});
        LyricAnimate(LyricsCurrentRow,OpacityProperty,forward?1:0,1,.2);LyricAnimate(LyricsPrevious,OpacityProperty,forward?1:0,1,.25);LyricAnimate(LyricsNext,OpacityProperty,0,1,.25);
        if(visible&&leaving.Length>0){
            LyricsLeaving.Text=leaving;LyricsLeaving.Width=leavingWidth;
            var position=Stage.TranslatePoint(oldPrevious,LyricsLayer);Canvas.SetLeft(LyricsLeaving,position.X);Canvas.SetTop(LyricsLeaving,position.Y);
            LyricAnimate(LyricsLeavingMove,TranslateTransform.YProperty,0,-24,.23);LyricAnimate(LyricsLeaving,OpacityProperty,1,0,.23);
        }else{LyricsLeaving.BeginAnimation(OpacityProperty,null);LyricsLeaving.Opacity=0;}

    }
}
