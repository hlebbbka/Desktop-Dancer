namespace MusicDancer;

internal sealed class BassAtmosphere
{
    private readonly Queue<double> hits = new();
    private double sustain;
    public void UpdateSustain(float[]? spectrum, double dt)
    {
        // Keep the same three low-frequency columns as the strict onset detector.
        double low = spectrum is { Length: >= 3 } ? 1 : 0;
        if (spectrum is { Length: >= 3 })
            for (int i = 0; i < 3; i++)
                low = Math.Min(low, Math.Clamp(Math.Sqrt(Math.Max(0, spectrum[i]) * .6514423076923082 * 4), 0, 1));
        double target = Math.Clamp((low - .65) / .33, 0, 1);
        target = target * target * (3 - 2 * target);
        sustain += (target - sustain) * (1 - Math.Exp(-Math.Max(0, dt) / (target > sustain ? .06 : .16)));
        if (target == 0 && sustain < .0001) sustain = 0;
    }
    public void Trigger(double time) { hits.Enqueue(time); while (hits.Count > 8) hits.Dequeue(); }
    public (double Split, double X, double Y, double Tilt) Sample(double time)
    {
        while (hits.Count > 0 && time - hits.Peek() > 1.2) hits.Dequeue();
        // A held bass keeps a quiet moving bed; each new onset adds its full pulse.
        double split = sustain * .45, x = sustain * .33 * Math.Sin(time * 13),
            y = sustain * .22 * Math.Sin(time * 17), tilt = sustain * .18 * Math.Sin(time * 11);
        foreach (double hit in hits)
        {
            double age = Math.Max(0, time - hit), u = age / .085;
            double envelope = u * u * Math.Exp(2 - 2 * u);
            split += envelope;
            x += Math.Sin(age * 27) * envelope;
            y += Math.Sin(age * 21) * envelope;
            tilt += Math.Sin(age * 18) * envelope;
        }
        return (1 - Math.Exp(-split * 1.4), Math.Tanh(x) * 2.4, Math.Tanh(y) * 1.6, Math.Tanh(tilt) * .16);
    }
    public void Reset() { hits.Clear(); sustain = 0; }
}
