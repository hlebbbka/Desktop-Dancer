namespace MusicDancer;

// Uses the same normalization as the visible equalizer, not a quiet-audio threshold.
internal sealed class LowBassOnset
{
    private readonly Queue<(double Time, double Level)> history = new();
    private readonly double sensitivity;
    private float[]? lastSpectrum;
    private double lastHit = -10;
    private bool armed = true;
    public LowBassOnset(double sensitivity = 0.6514423076923082) => this.sensitivity = sensitivity;
    public bool Update(float[] spectrum, double time)
    {
        if (ReferenceEquals(lastSpectrum, spectrum)) return false;
        lastSpectrum = spectrum;
        // The minimum means all three bars must reach the top together.
        double level = 1;
        for (int i=0;i<3;i++) level = Math.Min(level, Math.Clamp(Math.Sqrt(Math.Max(0,spectrum[i])*sensitivity*4),0,1));
        while (history.Count > 0 && time-history.Peek().Time > .12) history.Dequeue();
        double trough = history.Count == 0 ? level : history.Min(p=>p.Level);
        if (level < .75) armed = true;
        bool hit = armed && time-lastHit >= .24 && level >= .98 && level-trough >= .18;
        history.Enqueue((time,level));
        if (hit) { lastHit=time; armed=false; }
        return hit;
    }
    public void Reset() { history.Clear(); lastSpectrum=null; lastHit=-10; armed=true; }
}
