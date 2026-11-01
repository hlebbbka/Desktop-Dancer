namespace MusicDancer;

// A turn ends after four beats, but an airborne dancer keeps ownership until landing.
internal sealed class BattleTurn
{
    private bool enabled, first = true, waiting;
    private double beats, transition = 1;
    public bool CanStartMove => !enabled || (!waiting && transition >= 1);
    public double PrimaryBlend
    {
        get
        {
            double blend = transition * transition * (3 - 2 * transition);
            return first ? blend : 1 - blend;
        }
    }

    public void Update(bool active, bool playing, double dt, double period, bool airborne)
    {
        if (!active || !enabled)
        {
            first = true; waiting = false; beats = 0; transition = 1;
        }
        enabled = active;
        if (!active || !playing) return;
        transition = System.Math.Min(1, transition + dt / .18);
        beats += dt / System.Math.Clamp(period, .3, 1);
        if (beats >= 4) waiting = true;
        if (waiting && !airborne)
        {
            first = !first;
            beats = 0;
            waiting = false;
            transition = 0;
        }
    }
}
