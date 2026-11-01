using System.Diagnostics;
using System.Runtime.InteropServices;
namespace MusicDancer;
internal sealed class TrailFrameClock : IDisposable
{
    private readonly CancellationTokenSource stop = new();
    private readonly Thread worker;
    private readonly Action tick;
    private volatile bool active; private int disposed;
    private readonly bool timerResolution;
    [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint period);
    [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint period);
    public TrailFrameClock(Action tick)
    {
        this.tick=tick;timerResolution=timeBeginPeriod(1)==0;
        worker=new Thread(Run){IsBackground=true,Name="Trail frame clock"};worker.Start();
    }
    public void SetActive(bool value)=>active=value;
    private void Run()
    {
        double step=Stopwatch.Frequency/140d,next=Stopwatch.GetTimestamp();
        var wait=stop.Token.WaitHandle;
        while(!stop.IsCancellationRequested)
        {
            if(!active){if(wait.WaitOne(20))break;next=Stopwatch.GetTimestamp();continue;}
            double now=Stopwatch.GetTimestamp();
            if(now>=next){tick();next+=step;if(next<now-step)next=now+step;}
            double ms=(next-Stopwatch.GetTimestamp())*1000/Stopwatch.Frequency;
            if(wait.WaitOne(Math.Max(1,(int)ms)))break;
        }
    }
    public void Dispose(){if(Interlocked.Exchange(ref disposed,1)!=0)return;stop.Cancel();worker.Join(1000);if(timerResolution)timeEndPeriod(1);stop.Dispose();}
}
