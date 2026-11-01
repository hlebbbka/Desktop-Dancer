namespace MusicDancer;

// Estimates tempo from recurring rises in bass energy over a rolling window.
internal sealed class TempoTracker
{
    private const double Step = .025;
    private readonly double[] history = new double[400];
    private int count, head;
    private double lastSample = -1, lastEstimate, previous, floor;
    public double Period { get; private set; } = .5;
    public bool HasEstimate { get; private set; }
    public void Reset() { Array.Clear(history); count = head = 0; lastSample = -1; lastEstimate = previous = floor = 0; HasEstimate = false; Period = .5; }
    public void Update(double time, double bass)
    {
        if (lastSample >= 0 && time - lastSample < Step) return;
        if (lastSample >= 0 && time - lastSample > .12) { Reset(); }
        lastSample = lastSample < 0 ? time : lastSample + Step;
        floor += (bass - floor) * .08;
        double onset = Math.Max(0, bass - previous); previous = bass;
        history[head] = onset > floor * .012 ? onset : 0;
        head = (head + 1) % history.Length; count = Math.Min(count + 1, history.Length);
        if (count < 160 || time - lastEstimate < 1) return;
        lastEstimate = time;
        double best = 0; int bestLag = 0;
        for (int lag = 13; lag <= 34; lag++)
        {
            double cross = 0, a = 0, b = 0;
            for (int i = lag; i < count; i++) { double x = history[(head - 1 - i + history.Length) % history.Length]; double y = history[(head - 1 - (i - lag) + history.Length) % history.Length]; cross += x*y; a += x*x; b += y*y; }
            double score = cross / Math.Sqrt(a*b + 1e-12);
            if (score > best) { best = score; bestLag = lag; }
        }
        if (bestLag != 0 && best > .12) { double measured = bestLag * Step; Period = HasEstimate ? Period * .75 + measured * .25 : measured; HasEstimate = true; }
    }
}
