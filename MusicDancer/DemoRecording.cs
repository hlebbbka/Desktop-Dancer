using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MusicDancer;
public partial class MainWindow
{
    // Explicit documentation-capture mode. Never used by the normal launcher.
    private readonly string? demoName=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--record-demo="))?.Split('=',2)[1];
    private bool IsDemoRecording=>profileTrail&&demoName!=null;
    private int demoFrames;
    private bool demoFrameInFlight;
    private System.Windows.Controls.Image? demoTrailSnapshot;
    private void StartDemoRecording()
    {
        if(!IsDemoRecording)return;
        ApplyLanguage(demoName=="battle"?"ru":"en");SetImage(null);
        string folder=Path.Combine(Directory.GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))!.FullName,"IMG for Desktop Dancer");
        string partner=Path.Combine(folder,"Пиздося в худи.png");
        if(File.Exists(partner))LoadPartner(partner);
        DuetMode.IsChecked=BattleMode.IsChecked=demoName=="battle";
        SpotifyLyricsMode.IsChecked=false;FewerJumps.IsChecked=true;NoFlip.IsChecked=false;ShakeOnly.IsChecked=false;
        SizeSlider.Value=1;HideTrail.IsChecked=false;WanderingTrail.IsChecked=true;HeavyBass.IsChecked=false;
        ApplyBackground(demoName=="battle"?"#1F4261":"#392A58");
        SpotifyTrack.Text="Desktop Dancer · Bass demo";AlbumCover.Source=Character.Source;
        StatusText.Text=demoName=="battle"?"Демонстрация":"Demo";
        if(demoName=="battle")Fullscreen_Click(this,new RoutedEventArgs());
    }
    private async void RecordDemoFrame(double time)
    {
        if(!IsDemoRecording||time<20)return;
        if(time>=26){Close();return;}
        if(demoFrameInFlight||(time-20)*18<demoFrames)return;
        demoFrameInFlight=true;
        try{
        // WebView's GPU surface is not included by RenderTargetBitmap. Read
        // our own canvas and insert its transparent pixels at the same layer.
        if(tubesReady){
            using var memory=new MemoryStream();
            await TubeBrowser.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,memory);
            memory.Position=0;
            var trail=new BitmapImage();trail.BeginInit();trail.CacheOption=BitmapCacheOption.OnLoad;trail.StreamSource=memory;trail.DecodePixelWidth=720;trail.EndInit();trail.Freeze();
            if(demoTrailSnapshot==null){demoTrailSnapshot=new System.Windows.Controls.Image{IsHitTestVisible=false,Stretch=Stretch.Fill};SceneBackground.Children.Insert(SceneBackground.Children.IndexOf(TubeBrowser)+1,demoTrailSnapshot);}
            demoTrailSnapshot.Source=trail;
        }
        double width=720,height=Math.Round(Stage.ActualHeight*width/Stage.ActualWidth);
        if(height<1)return;
        Stage.UpdateLayout();
        var drawing=new DrawingVisual();using(var context=drawing.RenderOpen())context.DrawRectangle(new VisualBrush(Stage),null,new Rect(0,0,width,height));
        var bitmap=new RenderTargetBitmap((int)width,(int)height,96,96,PixelFormats.Pbgra32);bitmap.Render(drawing);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        string folder=Path.Combine(AppContext.BaseDirectory,"demo-frames",demoName!);Directory.CreateDirectory(folder);
        using(var stream=File.Create(Path.Combine(folder,$"{demoFrames:0000}.png")))encoder.Save(stream);
        demoFrames++;
        }catch(Exception e){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"demo-error.txt"),e.ToString());}
        finally{demoFrameInFlight=false;}
    }
}
