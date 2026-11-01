using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace MusicDancer;
internal sealed class PetWindow : Window
{
    private readonly Image character,partner;
    private readonly TextBlock mood;
    private readonly ScaleTransform scale=new(),partnerScale=new();
    private readonly RotateTransform rotation=new(),partnerRotation=new();
    private readonly TranslateTransform movement=new(),partnerMovement=new();
    private readonly System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();
    private double lastTime,reactUntil=-10,dragUntil=-10; private double secondSx=1,secondSy=1,secondAngle,secondY,secondOpacity=1;
    private bool duet,quiet,sleeping,paused,dragging,moved;
    private Point dragScreen;private double dragLeft,dragTop;
    public PetWindow(ImageSource source,string language="ru")
    {
        Width=420;Height=520;WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;
        Left=SystemParameters.WorkArea.Right-Width;Top=SystemParameters.WorkArea.Bottom-Height;
        character=MakeImage(source,scale,rotation,movement);partner=MakeImage(source,partnerScale,partnerRotation,partnerMovement);partner.Visibility=Visibility.Collapsed;
        mood=new TextBlock{Foreground=Brushes.White,FontSize=20,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,350),IsHitTestVisible=false};
        var grid=new Grid();grid.Children.Add(character);grid.Children.Add(partner);grid.Children.Add(mood);Content=grid;
        MouseLeftButtonDown+=(_,e)=>{dragging=true;moved=false;dragScreen=PointToScreen(e.GetPosition(this));dragLeft=Left;dragTop=Top;CaptureMouse();e.Handled=true;};
        MouseMove+=(_,e)=>{if(!dragging)return;Point p=PointToScreen(e.GetPosition(this));Vector d=p-dragScreen;var source=PresentationSource.FromVisual(this);if(source?.CompositionTarget!=null)d=source.CompositionTarget.TransformFromDevice.Transform(d);if(d.Length>4)moved=true;Left=dragLeft+d.X;Top=dragTop+d.Y;rotation.Angle=Math.Clamp(d.X*.1,-15,15);};
        MouseLeftButtonUp+=(_,e)=>{if(!dragging)return;dragging=false;ReleaseMouseCapture();dragUntil=clock.Elapsed.TotalSeconds+4;if(!moved)reactUntil=clock.Elapsed.TotalSeconds+.8;};
        LostMouseCapture+=(_,_)=>dragging=false;
        var menu=new ContextMenu();var close=new MenuItem{Header="Закрыть питомца"};close.Click+=(_,_)=>Close();menu.Items.Add(close);ContextMenu=menu;
        SetLanguage(language);
    }
    public void SetLanguage(string language){if(ContextMenu?.Items[0] is MenuItem close)close.Header=UiText.Get("Закрыть питомца",language);}
    private static Image MakeImage(ImageSource source,ScaleTransform size,RotateTransform turn,TranslateTransform move){var image=new Image{Source=source,Width=155,Height=240,Stretch=Stretch.Uniform,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,95),RenderTransformOrigin=new Point(.5,.5)};RenderOptions.SetBitmapScalingMode(image,BitmapScalingMode.NearestNeighbor);var group=new TransformGroup();group.Children.Add(size);group.Children.Add(turn);group.Children.Add(move);image.RenderTransform=group;return image;}
    public void SetFeatures(ImageSource? source,bool duo,bool silent,bool asleep,bool isPaused){duet=duo&&source!=null;if(source!=null&&partner.Source!=source)partner.Source=source;partner.Visibility=duet?Visibility.Visible:Visibility.Collapsed;quiet=silent;sleeping=asleep&&silent;paused=isPaused;}
    public void SetPartnerPose(double sx,double sy,double angle,double y,double opacity){secondSx=sx;secondSy=sy;secondAngle=angle;secondY=y;secondOpacity=opacity;}
    public void Update(ImageSource source,double sx,double sy,double angle,double x,double y)
    {
        double t=clock.Elapsed.TotalSeconds,dt=lastTime>0?Math.Clamp(t-lastTime,0,.05):0;lastTime=t;if(character.Source!=source)character.Source=source;
        double reaction=t<reactUntil?Math.Sin((reactUntil-t)/.8*Math.PI)*22:0;
        double look=IsMouseOver&&!dragging?Math.Clamp((Mouse.GetPosition(this).X-Width/2)/45,-3,3):0;
        scale.ScaleX=sx;scale.ScaleY=sy;if(!dragging)rotation.Angle=angle+look+(reaction>0?Math.Sin(t*18)*8:0);
        movement.X=duet?-75+Math.Sin(t*2)*3:x*.65;movement.Y=y*.65-reaction;
        partnerScale.ScaleX=secondSx;partnerScale.ScaleY=secondSy;partnerRotation.Angle=secondAngle;partnerMovement.X=75;partnerMovement.Y=secondY*.65-reaction;partner.Opacity=secondOpacity;
        mood.Text=t<reactUntil?"♥":"";
    }
}
