using System.IO;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
namespace MusicDancer;
public partial class MainWindow
{
    private GlobalSystemMediaTransportControlsSessionManager? mediaSessions;
    private string? coverKey;
    private bool spotifyUpdating;
    private async Task UpdateSpotifyMedia()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10,0,17763)) return;
        if (spotifyUpdating) return; spotifyUpdating=true;
        try
        {
            mediaSessions ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            GlobalSystemMediaTransportControlsSession? session=null; foreach(var candidate in mediaSessions.GetSessions()) { if(candidate.SourceAppUserModelId.Contains("spotify",StringComparison.OrdinalIgnoreCase)) { session=candidate;break; } }
            if(session==null) { SpotifyTrack.Text=await Task.Run(ReadSpotifyTitle)??T("Ожидание трека"); AlbumCover.Source=null; coverKey=null; return; }
            var props=await session.TryGetMediaPropertiesAsync();
            SpotifyTrack.Text=string.IsNullOrWhiteSpace(props.Title)?T("Ожидание трека"):$"{props.Artist} — {props.Title}";
            string key=props.Artist+"\n"+props.Title+"\n"+props.AlbumTitle;
            if(key==coverKey)return; coverKey=key; AlbumCover.Source=null;
            if(props.Thumbnail==null)return;
            using var stream=await props.Thumbnail.OpenReadAsync();
            if(stream.Size==0||stream.Size>8*1024*1024)return;
            using var reader=new DataReader(stream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)stream.Size); var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);
            using var memory=new MemoryStream(bytes); var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=memory;image.DecodePixelWidth=128;image.EndInit();image.Freeze();AlbumCover.Source=image;
        }
        catch(Exception e) when(e is System.Runtime.InteropServices.COMException || e is IOException || e is InvalidOperationException || e is NotSupportedException || e is FormatException || e is ArgumentException) { coverKey=null; }
        finally { spotifyUpdating=false; }
    }
}
