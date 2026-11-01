using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
namespace MusicDancer;

public sealed class ChromaticBackground : ShaderEffect
{
    private static readonly PixelShader Shader = new() { UriSource = new Uri("pack://application:,,,/MusicDancer;component/Assets/Chromatic.ps") };
    public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty(nameof(Input), typeof(ChromaticBackground), 0);
    public static readonly DependencyProperty ShiftProperty = DependencyProperty.Register(nameof(Shift), typeof(Point), typeof(ChromaticBackground), new UIPropertyMetadata(new Point(), PixelShaderConstantCallback(0)));
    public Brush Input { get => (Brush)GetValue(InputProperty); set => SetValue(InputProperty, value); }
    public Point Shift { get => (Point)GetValue(ShiftProperty); set => SetValue(ShiftProperty, value); }
    private static readonly PixelShader HudShader = new() { UriSource = new Uri("pack://application:,,,/MusicDancer;component/Assets/ChromaticHud.ps") };
    public ChromaticBackground() : this(false) { }
    public ChromaticBackground(bool hud) { PixelShader = hud ? HudShader : Shader; UpdateShaderValue(InputProperty); UpdateShaderValue(ShiftProperty); }
}
