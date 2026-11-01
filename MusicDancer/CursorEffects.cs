using System.Windows;
using System.Windows.Media;

namespace MusicDancer;

// Cached shaded wavefronts: no mesh generation or cursor trail per frame.
public sealed class CursorEffects : FrameworkElement
{
    private sealed class Wave { public Point Center; public double Start; public DrawingGroup Drawing = null!; }
    private readonly List<Wave> waves = new();
    private double time;
    public Color Accent { get; set; } = Color.FromRgb(183,154,255);
    public void Click(Point point, double now)
    {
        if (waves.Count >= 3) waves.RemoveAt(0);
        var drawing = new DrawingGroup();
        using(var dc = drawing.Open())
        {
            // Broad dark trough and a narrow light crest give a liquid relief.
            var brush = new RadialGradientBrush { Center = new Point(.5,.5), GradientOrigin = new Point(.5,.5), RadiusX = .5, RadiusY = .5 };
            brush.GradientStops.Add(new GradientStop(Colors.Transparent,0));
            brush.GradientStops.Add(new GradientStop(Colors.Transparent,.67));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0,0,0,0),.74));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(85,0,0,0),.81));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(28,Accent.R,Accent.G,Accent.B),.85));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(105,Accent.R,Accent.G,Accent.B),.88));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(52,235,245,255),.90));
            brush.GradientStops.Add(new GradientStop(Colors.Transparent,.96));
            brush.GradientStops.Add(new GradientStop(Colors.Transparent,1));
            brush.Freeze(); dc.DrawEllipse(brush,null,new Point(0,0),1,1);
        }
        drawing.Freeze(); waves.Add(new Wave { Center=point,Start=now,Drawing=drawing });
        InvalidateVisual();
    }
    public void Animate(double now, double dt)
    {
        time = now;
        if (waves.Count == 0) return;
        waves.RemoveAll(w=>now-w.Start>1.65);
        InvalidateVisual();
    }
    public Vector WaveForce(Point point)
    {
        var result = new Vector();
        foreach(var wave in waves)
        {
            double age=time-wave.Start; Vector delta=point-wave.Center; double d=delta.Length;
            double front=(20+age*230)*.88, gap=d-front;
            if(d>.1 && Math.Abs(gap)<30) result+=delta/d*(Math.Cos(gap/30*Math.PI)*.5+.5)*(1-age/1.65)*.7;
        }
        return result;
    }
    protected override void OnRender(DrawingContext dc)
    {
        foreach(var wave in waves)
        {
            double age=time-wave.Start, radius=20+age*230;
            dc.PushOpacity(Math.Min(1,age*14)*Math.Pow(Math.Max(0,1-age/1.65),1.5));
            dc.PushTransform(new MatrixTransform(radius,0,0,radius,wave.Center.X,wave.Center.Y));
            dc.DrawDrawing(wave.Drawing);
            dc.Pop(); dc.Pop();
        }
    }
}
