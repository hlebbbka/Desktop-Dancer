namespace MusicDancer;
internal sealed record SpotifyLyricFrame(string Previous,string Current,string Next,string Status,int Index=-1);
internal sealed class SpotifyLyricStability
{
    private SpotifyLyricFrame? good;
    private string key="";
    private double updated;
    public SpotifyLyricFrame Update(SpotifyLyricFrame frame,string track,double time)
    {
        if(track!=key){key=track;good=null;}
        if(frame.Current.Length>0){good=frame;updated=time;return frame;}
        return good!=null&&time-updated<.9?good:frame;
    }
    public void Reset(){good=null;key="";updated=0;}
}
