namespace MusicDancer;
internal static class SpotifyLyricSelection
{
    public static int ActiveIndex(IReadOnlyList<int> colors)
    {
        int index=-1;double min=1,max=-1;bool tied=false;
        for(int i=0;i<colors.Count;i++){
            if(colors[i]<0)continue;
            int color=colors[i];double light=((color&255)*.2126+((color>>8)&255)*.7152+((color>>16)&255)*.0722)/255;
            min=Math.Min(min,light);
            if(light>max+.003){max=light;index=i;tied=false;}else if(Math.Abs(light-max)<=.003)tied=true;
        }
        if(index<0||tied||max<.85)return -1;
        return colors.Count==1||max-min>.06?index:-1;
    }
}
