using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
namespace MusicDancer;
internal sealed class SpotifyLyricsReader : IDisposable
{
    private sealed record Line(string Text,TextPattern Pattern,double Font);
    private readonly CancellationTokenSource stop=new();
    private readonly Thread worker;
    private readonly Action<SpotifyLyricFrame> publish;
    private volatile bool enabled,forcePublish;
    private AutomationElement? root,anchor;
    private List<Line>? lines;
    private string track="",observedTrack="";
    private readonly SpotifyLyricStability stability=new();
    private SpotifyLyricFrame? last;
    private int disposed,activeIndex=-1,activeColor=-1;
    private double refreshed;
    private AutomationElement? watchedRoot;
    private readonly StructureChangedEventHandler structureHandler;
    private readonly AutomationEventHandler textHandler;
    private int treeDirty,rootDirty;
    public void RequestRefresh()=>Interlocked.Exchange(ref rootDirty,1);
    public SpotifyLyricsReader(Action<SpotifyLyricFrame> publish){this.publish=publish;structureHandler=(_,_)=>Interlocked.Exchange(ref treeDirty,1);textHandler=(_,_)=>Interlocked.Exchange(ref treeDirty,1);worker=new Thread(Run){IsBackground=true,Name="Spotify lyrics accessibility"};worker.SetApartmentState(ApartmentState.MTA);worker.Start();}
    public void SetEnabled(bool value){enabled=value;if(value)forcePublish=true;}
    public void Dispose(){if(Interlocked.Exchange(ref disposed,1)!=0)return;enabled=false;stop.Cancel();worker.Join(50);}
    private void Reset(){root=null;anchor=null;lines=null;track="";activeIndex=-1;activeColor=-1;}
    private static SpotifyLyricFrame Message(string text)=>new("","","",text);
    private void Run()
    {
        try{while(!stop.IsCancellationRequested){
            SpotifyLyricFrame? frame=null;
            if(enabled){
                for(int attempt=0;attempt<3;attempt++){
                    try{frame=Read();break;}
                    catch(Exception e)when(IsReadFailure(e)){
                        Reset();
                        if(attempt==2)frame=Message("\u041e\u0442\u043a\u0440\u043e\u0439\u0442\u0435 \u043f\u0430\u043d\u0435\u043b\u044c \u00ab\u0422\u0435\u043a\u0441\u0442\u00bb \u0432 Spotify");
                        else if(stop.Token.WaitHandle.WaitOne(15))break;
                    }
                }
            }
            else{DetachWatch();Reset();last=null;stability.Reset();}
            if(frame!=null)frame=stability.Update(frame,observedTrack,Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency);
            if(frame!=null&&(frame!=last||forcePublish)){forcePublish=false;last=frame;try{publish(frame);}catch(InvalidOperationException)when(stop.IsCancellationRequested){}}
            if(stop.Token.WaitHandle.WaitOne(!enabled?500:frame?.Current.Length>0?120:700))break;
        }}finally{DetachWatch();stop.Dispose();}
    }
    private static bool IsReadFailure(Exception e)=>e is ElementNotAvailableException or InvalidOperationException or COMException or System.ComponentModel.Win32Exception or NotSupportedException or ArgumentException or UnauthorizedAccessException;
    private void DetachWatch()
    {
        if(watchedRoot==null)return;
        try{Automation.RemoveStructureChangedEventHandler(watchedRoot,structureHandler);}catch(Exception e)when(IsReadFailure(e)){}
        try{Automation.RemoveAutomationEventHandler(TextPattern.TextChangedEvent,watchedRoot,textHandler);}catch(Exception e)when(IsReadFailure(e)){}
        watchedRoot=null;
    }
    private void EnsureWatch()
    {
        if(root==null)return;
        if(watchedRoot!=null&&Automation.Compare(root,watchedRoot))return;
        DetachWatch();watchedRoot=root;
        Automation.AddStructureChangedEventHandler(root,TreeScope.Subtree,structureHandler);
        Automation.AddAutomationEventHandler(TextPattern.TextChangedEvent,root,TreeScope.Subtree,textHandler);
    }
    private SpotifyLyricFrame Read()
    {
        if(Interlocked.Exchange(ref rootDirty,0)!=0)Reset();
        if(Interlocked.Exchange(ref treeDirty,0)!=0){lines=null;activeIndex=-1;}
        if(root==null){foreach(var process in Process.GetProcessesByName("Spotify")){using(process){if(process.MainWindowHandle!=IntPtr.Zero){root=AutomationElement.FromHandle(process.MainWindowHandle);break;}}}}
        if(root!=null)EnsureWatch();
        if(root==null)return Message("Spotify \u043d\u0435 \u0437\u0430\u043f\u0443\u0449\u0435\u043d");
        if(root.TryGetCurrentPattern(WindowPattern.Pattern,out var wp)&&((WindowPattern)wp).Current.WindowVisualState==WindowVisualState.Minimized){lines=null;return Message("\u0420\u0430\u0437\u0432\u0435\u0440\u043d\u0438\u0442\u0435 Spotify \u0441 \u043e\u0442\u043a\u0440\u044b\u0442\u044b\u043c \u0442\u0435\u043a\u0441\u0442\u043e\u043c \u043f\u0435\u0441\u043d\u0438");}
        string title=root.Current.Name; observedTrack=title;
        if(title!=track){track=title;lines=null;anchor=null;activeIndex=-1;}
        if(Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency-refreshed>.5){lines=null;}
        if(lines==null){
            var textCondition=new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Text);
            var cache=new CacheRequest();cache.Add(AutomationElement.NameProperty);cache.Add(TextPattern.Pattern);
            AutomationElementCollection all;using(cache.Activate())all=root.FindAll(TreeScope.Descendants,textCondition);
            anchor=all.Cast<AutomationElement>().FirstOrDefault(e=>e.Cached.Name.Contains("Musixmatch",StringComparison.OrdinalIgnoreCase));
            if(anchor==null)return Message("\u041e\u0442\u043a\u0440\u043e\u0439\u0442\u0435 \u043f\u0430\u043d\u0435\u043b\u044c \u00ab\u0422\u0435\u043a\u0441\u0442\u00bb \u0432 Spotify");
            var walker=TreeWalker.ControlViewWalker;var parent=walker.GetParent(anchor);var container=parent==null?null:walker.GetParent(parent);
            if(container==null)return Message("\u0422\u0435\u043a\u0441\u0442 Spotify \u043f\u043e\u043a\u0430 \u043d\u0435\u0434\u043e\u0441\u0442\u0443\u043f\u0435\u043d");
            AutomationElementCollection candidates;using(cache.Activate())candidates=container.FindAll(TreeScope.Descendants,textCondition);
            var found=new List<Line>();
            foreach(AutomationElement element in candidates){
                if(!element.TryGetCachedPattern(TextPattern.Pattern,out var pattern))continue;
                var tp=(TextPattern)pattern;var size=tp.DocumentRange.GetAttributeValue(TextPattern.FontSizeAttribute);
                if(size is double font&&font>=14&&!string.IsNullOrWhiteSpace(element.Cached.Name))found.Add(new(element.Cached.Name,tp,font));
            }
            if(found.Count==0)return Message("\u041d\u0435\u0442 \u0434\u043e\u0441\u0442\u0443\u043f\u043d\u044b\u0445 \u0441\u0442\u0440\u043e\u043a \u0442\u0435\u043a\u0441\u0442\u0430 \u0432 Spotify");
            double dominant=found.GroupBy(l=>l.Font).OrderByDescending(g=>g.Count()).ThenByDescending(g=>g.Key).First().Key;
            lines=found.Where(l=>Math.Abs(l.Font-dominant)<.1).ToList();activeIndex=-1;refreshed=Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
        }
        if(anchor==null||TreeWalker.ControlViewWalker.GetParent(anchor)==null){lines=null;return Message("\u041e\u0442\u043a\u0440\u043e\u0439\u0442\u0435 \u043f\u0430\u043d\u0435\u043b\u044c \u00ab\u0422\u0435\u043a\u0441\u0442\u00bb \u0432 Spotify");}
        // Normally only one line needs checking. Scan the complete list only when
        // its highlight changes; this avoids hundreds of cross-process UIA calls.
        if(activeIndex>=0&&activeIndex<lines.Count){
            var previousColor=lines[activeIndex].Pattern.DocumentRange.GetAttributeValue(TextPattern.ForegroundColorAttribute);
            if(previousColor is int value&&value==activeColor)return FrameFor(activeIndex);
        }
        var colors=new int[lines.Count];
        for(int i=0;i<lines.Count;i++){
            var color=lines[i].Pattern.DocumentRange.GetAttributeValue(TextPattern.ForegroundColorAttribute);
            colors[i]=color is int value?value:-1;
        }
        int current=SpotifyLyricSelection.ActiveIndex(colors);
        if(current<0)return Message("Spotify \u043f\u043e\u043a\u0430 \u043d\u0435 \u0432\u044b\u0434\u0435\u043b\u044f\u0435\u0442 \u0442\u0435\u043a\u0443\u0449\u0443\u044e \u0441\u0442\u0440\u043e\u043a\u0443");
        activeIndex=current;activeColor=colors[current];
        return FrameFor(current);
    }
    private SpotifyLyricFrame FrameFor(int current)
    {
        var currentLines=lines??throw new ElementNotAvailableException();
        return new(current>0?currentLines[current-1].Text:"",currentLines[current].Text,current+1<currentLines.Count?currentLines[current+1].Text:"","\u0421\u0438\u043d\u0445\u0440\u043e\u043d\u0438\u0437\u0438\u0440\u043e\u0432\u0430\u043d\u043e \u0441\u043e Spotify",current);
    }
}
