using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MusicDancer;
public partial class MainWindow
{
    private string? partnerPath;private ImageSource? partnerSource;private double pokeUntil=-10,sleepAmount;private double primaryBlend=1; private readonly BattleTurn battleTurn = new();
    private void InitializeFeatures()
    {
        Character.MouseLeftButtonDown += (_, e) => { pokeUntil = watch.Elapsed.TotalSeconds + .7; e.Handled = true; };
        PartnerCharacter.MouseLeftButtonDown += (_, e) => { pokeUntil = watch.Elapsed.TotalSeconds + .7; e.Handled = true; };
        string folder = Path.Combine(Directory.GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))!.FullName, "IMG for Desktop Dancer");
        string[] preferred = { "Кот в деловом костюме", "Пиздося в худи серьезная", "Пиздося в худи", "Пачулька", "Чапулька" };
        var paths = Directory.Exists(folder) ? Directory.EnumerateFiles(folder)
            .Where(p => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" }.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase))
            .OrderBy(p => { int index = Array.FindIndex(preferred, name => string.Equals(name, Path.GetFileNameWithoutExtension(p), StringComparison.OrdinalIgnoreCase)); return index < 0 ? int.MaxValue : index; })
            .ThenBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase).ToArray() : Array.Empty<string>();
        foreach (string path in paths)
        {
            try
            {
                var thumb = new BitmapImage(); thumb.BeginInit(); thumb.CacheOption = BitmapCacheOption.OnLoad; thumb.DecodePixelWidth = 100; thumb.UriSource = new Uri(path); thumb.EndInit(); thumb.Freeze();
                var button = new Button { Content = new Image { Source = thumb, Width = 60, Height = 70, Stretch = Stretch.Uniform }, Tag = Path.GetFileNameWithoutExtension(path), ToolTip = T(Path.GetFileNameWithoutExtension(path)), Padding = new Thickness(5), Margin = new Thickness(0, 0, 7, 0) };
                button.Click += (_, _) => LoadPartner(path);
                PartnerGallery.Children.Add(button);
            }
            catch (Exception ex) when (ex is IOException or NotSupportedException or ArgumentException) { }
        }
        if (partnerPath != null && File.Exists(partnerPath)) LoadPartner(partnerPath);
        else if (paths.Length > 0) LoadPartner(paths.FirstOrDefault(p => !string.Equals(p,imagePath,StringComparison.OrdinalIgnoreCase)) ?? paths[0]);
    }
    private void LoadPartner(string path){try{var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.UriSource=new Uri(path,UriKind.Absolute);image.EndInit();image.Freeze();partnerSource=image;partnerPath=path;PartnerCharacter.Source=image;PartnerLabel.Text=CharacterDisplayName(path);}catch(Exception ex)when(ex is IOException||ex is NotSupportedException||ex is ArgumentException){MessageBox.Show(T("Не удалось открыть второго персонажа."),"Desktop Dancer");}}
    private void PartnerFile_Click(object sender,RoutedEventArgs e){var d=new OpenFileDialog{Filter=T("Изображения|*.png;*.jpg;*.jpeg;*.bmp"),Title=T("Второй танцор")};if(d.ShowDialog()==true)LoadPartner(d.FileName);}
    private void AnimateFeatures(double t,double dt,double raw){bool quiet=!paused&&t-lastSoundTime>4;sleepAmount+=((quiet?1:0)-sleepAmount)*(1-Math.Exp(-dt*3));if(sleepAmount>.001){Rotate.Angle+=sleepAmount*(t-lastSoundTime>12?12:Math.Sin(t*.8)*2);Scale.ScaleY*=1-sleepAmount*(t-lastSoundTime>12?.10:.015);Move.Y+=sleepAmount*12;}bool poked=t<pokeUntil;if(poked){double q=1-(pokeUntil-t)/.7;Move.Y-=Math.Sin(q*Math.PI)*28;Rotate.Angle+=Math.Sin(q*Math.PI*3)*10;}if(Character.IsMouseOver&&!poked){var pos=Mouse.GetPosition(Character);Rotate.Angle+=Math.Clamp((pos.X-Character.ActualWidth/2)/35,-3,3);}CharacterMood.Text=poked?"♥":"";bool battle=BattleMode.IsChecked==true&&partnerSource!=null;
        primaryBlend=battleTurn.PrimaryBlend;
        bool duet=(DuetMode.IsChecked==true||battle)&&partnerSource!=null;
        PartnerCharacter.Visibility=duet?Visibility.Visible:Visibility.Collapsed;
        Character.Opacity=battle?.7+.3*primaryBlend:1;PartnerCharacter.Opacity=battle?1-.3*primaryBlend:1;
        if(duet){double angle=Rotate.Angle,y=Move.Y;Move.X-=Math.Min(140,Stage.ActualWidth*.18);Scale.ScaleX*=.83;Scale.ScaleY*=.83;PartnerScale.ScaleX=Scale.ScaleX;PartnerScale.ScaleY=Scale.ScaleY;
            if(battle&&!quiet&&!paused){double neutral=SizeSlider.Value*.83;double danceSx=Scale.ScaleX,danceSy=Scale.ScaleY;Scale.ScaleX=neutral+(danceSx-neutral)*primaryBlend;Scale.ScaleY=neutral+(danceSy-neutral)*primaryBlend;PartnerScale.ScaleX=neutral+(danceSx-neutral)*(1-primaryBlend);PartnerScale.ScaleY=neutral+(danceSy-neutral)*(1-primaryBlend);Rotate.Angle=angle*primaryBlend-3*(1-primaryBlend);Move.Y=y*primaryBlend;PartnerRotate.Angle=-angle*(1-primaryBlend)+3*primaryBlend;PartnerMove.Y=y*(1-primaryBlend);}
            else{PartnerRotate.Angle=-angle;PartnerMove.Y=quiet?y:-Math.Abs(Math.Sin(dancePhase+Math.PI/2))*upperMotion*28-bassJump*FixedEnergy*70;}
            PartnerMove.X=140-Math.Sin(dancePhase/2)*8*(battle?1-primaryBlend:1);
            if(battle)Move.X=-140+(Move.X+140)*primaryBlend;
        }
        pet?.SetFeatures(partnerSource,duet,quiet,t-lastSoundTime>12,paused);
        pet?.SetPartnerPose(PartnerScale.ScaleX,PartnerScale.ScaleY,PartnerRotate.Angle,PartnerMove.Y,battle?1-.3*primaryBlend:1);
    }
}
