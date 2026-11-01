using System.Windows;
namespace MusicDancer;
public partial class MainWindow
{
    private readonly ChromaticBackground trackChromatic = new(true), bpmChromatic = new(true), exitChromatic = new(true);
    private static void UpdateHudChroma(FrameworkElement target, ChromaticBackground effect, double pixels, bool active)
    {
        effect.Shift = new Point(pixels / Math.Max(1,target.ActualWidth), pixels*.2 / Math.Max(1,target.ActualHeight));
        var next = active ? effect : null;
        if(!ReferenceEquals(target.Effect,next))target.Effect=next;
    }
}
