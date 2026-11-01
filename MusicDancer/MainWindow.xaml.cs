using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MusicDancer;
public partial class MainWindow : Window
{
    private const double FixedSensitivity = 0.6514423076923082, FixedEnergy = 0.9807692307692306, FixedSize = 0.8961538461538461;
    private string backgroundColor = "#392A58"; private Color accentColor = Color.FromRgb(183, 154, 255); private PetWindow? pet; private SystemAudio? audio; private TimeSpan lastRenderTime = TimeSpan.MinValue;
    private readonly System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
    private bool paused; private double volumeMeter; private double smooth, impulse, average, bassAverage, bassJump, flipStart = -100; private int beatCount; private double previousBass, bassPulse, jumpStart = -100, jumpStrength, glowPulse, bassFast, upperMotion; private string? imagePath; private DateTime lastBeat = DateTime.MinValue;
    private sealed class Particle { public double X, Y, Dx, Dy, Vx, Vy; public System.Windows.Shapes.Ellipse Shape = null!; public TranslateTransform Position = new(); }
    private readonly List<Particle> particles = new(); private Point? mousePoint; private readonly Random random = new();
    private readonly double[] bars = new double[78]; private readonly TempoTracker tempo = new(); private double lastSoundTime, dancePhase, previousTick, lastSpotifyCheck = -10;
    private readonly LowBassOnset lowBassOnset = new();
    private readonly BassAtmosphere bassAtmosphere = new();
    private double lastAtmosphereActive = -10;
    private DateTime lastBassBeat = DateTime.MinValue;
    private readonly string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tanctspol", "settings.json");
    private sealed class Settings { public double Sensitivity {get;set;} = 2; public double Energy {get;set;} = 1; public double Size {get;set;} = 1; public string? ImagePath {get;set;} public bool ShakeOnly {get;set;} public bool NoFlip {get;set;} public bool FewerJumps {get;set;} public bool HeavyBass {get;set;} public bool WanderingTrail {get;set;} public bool HideTrail {get;set;} public bool SpotifyLyrics {get;set;} public string? Language {get;set;} public string? BackgroundColor {get;set;} public bool Duet {get;set;} public bool Battle {get;set;} public string? PartnerPath {get;set;} }
    public MainWindow()
    {
        InitializeComponent(); InitializeLocalization(); InitializeSpotifyLyrics(); if(profileTrail && profileLegacyBounds){SceneBackground.ClipToBounds=false;Particles.ClipToBounds=false;Decor.ClipToBounds=false;} Loaded += async (_, _) => await InitializeTubes(); StateChanged += async (_, _) => await UpdateEffectsActivity(); LoadSettings(); LoadGallery(); InitializeFeatures(); CompositionTarget.Rendering += RenderFrame; Loaded+=(_,_)=>StartDemoRecording(); if(!IsDemoRecording)Connect(); Closed += (_,_) => { CompositionTarget.Rendering -= RenderFrame; pet?.Close(); TubeBrowser.Dispose(); audio?.Dispose(); if(!profileTrail) SaveSettings(); };
    }
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) { ToggleMaximized(); return; }
        if (WindowState == WindowState.Normal && e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }
    private void ToggleMaximized() { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□"; }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximized();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Connect()
    {
        lowBassOnset.Reset(); audio?.Dispose(); audioStatus="Подключение..."; StatusText.Text = T(audioStatus); StatusDot.Fill = Brushes.Gray;
        var a = new SystemAudio(); audio = a; a.Status += message => Dispatcher.BeginInvoke(() => { if (audio != a) return; audioStatus=message; StatusText.Text = LocalizedAudioStatus(); StatusDot.Fill = message.StartsWith("Системный") ? Brushes.MediumAquamarine : Brushes.Salmon; }); a.Start();
    }
    private static double Blend(double coefficient, double dt) => 1 - Math.Pow(1 - coefficient, dt * 140);
    private void RenderFrame(object? sender, EventArgs e) { if (e is RenderingEventArgs frame) { if (frame.RenderingTime == lastRenderTime) return; lastRenderTime = frame.RenderingTime; } long diagnosticStart = profileTrail ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; FlushTubesPointer(); Tick(sender, e); if(profileTrail) ProfileFrame(watch.Elapsed.TotalSeconds, diagnosticStart); }
    private void Tick(object? sender, EventArgs e)
    {
        double t = watch.Elapsed.TotalSeconds;
        double dt = previousTick > 0 ? Math.Clamp(t - previousTick, .0001, .05) : 1.0 / 140; previousTick = t;
        double raw = paused ? 0 : IsDemoRecording ? .35 : Math.Clamp((audio?.Level ?? 0) * FixedSensitivity * 2.5, 0, 1);
        smooth += (raw - smooth) * Blend(raw > smooth ? .104 : .023, dt); average += (raw - average) * Blend(.0057, dt);
        if (!paused && raw > .055 && raw > average * 1.3 && DateTime.UtcNow - lastBeat > TimeSpan.FromMilliseconds(180)) { impulse = Math.Max(impulse, raw); lastBeat = DateTime.UtcNow; }
        if (!IsDemoRecording && WindowState != WindowState.Minimized && t - lastSpotifyCheck >= 2) { lastSpotifyCheck = t; UpdateSpotifyTitle(); }
        var soundSpectrum = audio?.Spectrum;
        if(profileTrail){soundSpectrum=new float[52];float testBass=t% .6 < .4 ? .45f : .003f;for(int i=0;i<3;i++)soundSpectrum[i]=testBass;}
        if(IsDemoRecording&&soundSpectrum!=null)for(int i=3;i<52;i++)soundSpectrum[i]=(float)(.002+.009*(1+Math.Sin(t*5+i*.45)));
        // The first six spectrum bins cover roughly 55–160 Hz. Compare a fast
        // envelope with a slower baseline so repeated kicks remain detectable.
        double bass = 0;
        if (!paused && soundSpectrum != null)
        {
            for (int i = 0; i < 6; i++) bass += soundSpectrum[(int)Math.Round(i * 51.0 / 25)] * soundSpectrum[(int)Math.Round(i * 51.0 / 25)];
            bass = Math.Sqrt(bass / 6) * FixedSensitivity;
        }
        if (paused || raw < .005) { if (t - lastSoundTime > 3) tempo.Reset(); } else { lastSoundTime = t; tempo.Update(t, bass); }
        if (!paused) dancePhase += dt * 2 * Math.PI / tempo.Period;
        bassFast += (bass - bassFast) * Blend(.3, dt);
        bassAverage += (bass - bassAverage) * Blend(.012, dt);
        double rise = Math.Max(0, bassFast - previousBass); previousBass = bassFast;
        bassPulse = Math.Max(rise, bassPulse * Math.Pow(.97, dt * 140));
        bool strictHit = !paused && soundSpectrum != null && lowBassOnset.Update(soundSpectrum,t);

        if (paused) lowBassOnset.Reset();
        if (strictHit && WindowState != WindowState.Minimized) { bassAtmosphere.Trigger(t); BoostWanderingTrail(); }
        if (paused || WindowState == WindowState.Minimized) bassAtmosphere.Reset();
        else bassAtmosphere.UpdateSustain(soundSpectrum, dt);
        bool bassHit = FewerJumps.IsChecked == true ? strictHit
            : bassFast > .006 && bassFast > bassAverage * 1.08 && bassPulse > .00015
                && DateTime.UtcNow-lastBassBeat > TimeSpan.FromSeconds(Math.Clamp(tempo.Period*.75,.25,.6));
        bool airborne = ShakeOnly.IsChecked != true &&
            (t - jumpStart < Math.Clamp(tempo.Period * .8, .25, .6) ||
             (NoFlip.IsChecked != true && t - flipStart < .8));
        battleTurn.Update(BattleMode.IsChecked == true && partnerSource != null,
            !paused && raw > .01, dt, tempo.Period, airborne);
        if (!paused && bassHit)
        {
            lastBassBeat = DateTime.UtcNow; dancePhase += Math.IEEERemainder(-dancePhase, 2 * Math.PI) * .2; glowPulse = Math.Max(glowPulse, .9);
            if (battleTurn.CanStartMove)
            {
                jumpStart = t; jumpStrength = Math.Clamp(.8 + bassFast * 2, .8, 1.25); beatCount++;
                if (ShakeOnly.IsChecked != true && NoFlip.IsChecked != true && beatCount % 12 == 0 && FixedEnergy > .25 && t - flipStart > 8) flipStart = t;
            }
        }
        double upper = 0; if (!paused && soundSpectrum != null) { for (int i = 10; i < 26; i++) upper += soundSpectrum[(int)Math.Round(i * 51.0 / 25)] * soundSpectrum[(int)Math.Round(i * 51.0 / 25)]; upper = Math.Clamp(Math.Sqrt(upper / 16) * FixedSensitivity * 12, 0, 1); } upperMotion += (upper - upperMotion) * Blend(upper > upperMotion ? .13 : .035, dt);
        double jumpProgress = Math.Clamp((t - jumpStart) / Math.Clamp(tempo.Period * .8, .25, .6), 0, 1); bassJump = jumpProgress < 1 ? Math.Sin(Math.PI * jumpProgress) * jumpStrength : 0; impulse *= Math.Pow(.971, dt * 140); double energy = FixedEnergy; double amount = Math.Clamp(upperMotion * energy, 0, 1.7);
        double flipProgress = Math.Clamp((t - flipStart) / .8, 0, 1); double flip = flipProgress < 1 ? 360 * (flipProgress * flipProgress * (3 - 2 * flipProgress)) : 0; Rotate.Angle = paused ? 0 : ShakeOnly.IsChecked == true ? Math.Sin(t * 18) * Math.Clamp(smooth * 2.5, 0, 2.5) : Math.Sin(dancePhase) * amount * 13 - (NoFlip.IsChecked == true ? 0 : flip);
        Scale.ScaleX = SizeSlider.Value * (1 + amount * .05); Scale.ScaleY = SizeSlider.Value * (1 - upperMotion * energy * .04);
        Move.Y = paused ? 0 : ShakeOnly.IsChecked == true ? Math.Sin(t * 21) * Math.Clamp(smooth * 1.5, 0, 1.5) : -bassJump * energy * 95 - (NoFlip.IsChecked != true && flipProgress < 1 ? Math.Sin(flipProgress * Math.PI) * 55 * energy : 0);
        Move.X = paused ? 0 : ShakeOnly.IsChecked == true ? Math.Sin(t * 19) * Math.Clamp(smooth * 2.5, 0, 2.5) : Math.Sin(dancePhase / 2) * amount * 13;
        AnimateFeatures(t,dt,raw);
        pet?.Update(Character.Source, Scale.ScaleX, Scale.ScaleY, Rotate.Angle, Move.X, Move.Y);
        glowPulse *= Math.Pow(.979, dt * 140); if (WindowState == WindowState.Minimized) return; var atmosphere = bassAtmosphere.Sample(t);
        UpdateWanderingBass(atmosphere.Split,t);
        double bassEffectStrength = HeavyBass.IsChecked == true ? 3 : 1;
        BassShift.X = atmosphere.X * bassEffectStrength; BassShift.Y = atmosphere.Y * bassEffectStrength; BassTilt.Angle = atmosphere.Tilt * bassEffectStrength;
        FullscreenBassShift.X = sceneFullscreen ? atmosphere.X * bassEffectStrength : 0;
        FullscreenBassShift.Y = sceneFullscreen ? atmosphere.Y * bassEffectStrength : 0;
        FullscreenBassTilt.Angle = sceneFullscreen ? atmosphere.Tilt * bassEffectStrength : 0;
        double hudShakeX = sceneFullscreen ? 0 : atmosphere.X * bassEffectStrength, hudShakeY = sceneFullscreen ? 0 : atmosphere.Y * bassEffectStrength, hudTilt = sceneFullscreen ? 0 : atmosphere.Tilt * bassEffectStrength;
        TrackBassMove.X = hudShakeX * .8; TrackBassMove.Y = hudShakeY * .8; TrackBassTilt.Angle = hudTilt * .8;
        BpmBassMove.X = -hudShakeX * .7; BpmBassMove.Y = hudShakeY * .9; BpmBassTilt.Angle = -hudTilt;
        ExitBassMove.X = sceneFullscreen ? hudShakeX * .7 : 0; ExitBassMove.Y = sceneFullscreen ? -hudShakeY * .8 : 0; ExitBassTilt.Angle = sceneFullscreen ? hudTilt : 0;
        PlatformBassScale.ScaleX = PlatformBassScale.ScaleY = 1 + atmosphere.Split * .08 * bassEffectStrength;
        BassChromatic.Shift = new Point(atmosphere.Split * bassEffectStrength * 7 / Math.Max(1, Stage.ActualWidth), atmosphere.Split * bassEffectStrength * 2 / Math.Max(1, Stage.ActualHeight));
        if (atmosphere.Split > .001) lastAtmosphereActive = t;
        var backgroundEffect = !profileNoChroma && !paused && t-lastAtmosphereActive < 1 ? BassChromatic : null;
        if(profileTrail && profileLegacyBounds) backgroundEffect = atmosphere.Split > .001 ? BassChromatic : null;
        var sceneEffect = sceneFullscreen ? null : backgroundEffect;
        var fullEffect = sceneFullscreen ? backgroundEffect : null;
        if (!ReferenceEquals(SceneBackground.Effect, sceneEffect)) SceneBackground.Effect = sceneEffect;
        if (!ReferenceEquals(FullscreenHost.Effect, fullEffect)) FullscreenHost.Effect = fullEffect;
        bool hudChromaActive = !sceneFullscreen && !paused && t-lastAtmosphereActive < 1;
        double hudSplit = atmosphere.Split * bassEffectStrength * 3;
        UpdateHudChroma(TrackBadge, trackChromatic, hudSplit, hudChromaActive);
        UpdateHudChroma(BpmBadge, bpmChromatic, hudSplit, hudChromaActive);
        UpdateHudChroma(FullscreenExit, exitChromatic, hudSplit, hudChromaActive && sceneFullscreen);
        BassGlow.Opacity = paused ? 0 : Math.Clamp(glowPulse + bass * .35, 0, .85); BassGlow.Width = BassGlow.Height = 440 + glowPulse * 160;
        Glow.Width = 220 + amount * 120; Glow.Opacity = .35 + Math.Min(.6, amount * .4+glowPulse*.35);
        // Display actual audio loudness independently of dance sensitivity and pause.
        double volume = Math.Clamp(audio?.MixerLevel ?? 0, 0, 1);
        double meterTarget = volume <= .0001 ? 0 : volume;
        double meterResponse = 1 - Math.Exp(-dt / (meterTarget > volumeMeter ? .035 : .18));
        volumeMeter += (meterTarget - volumeMeter) * meterResponse;
        if (meterTarget == 0 && volumeMeter < .001) volumeMeter = 0;
        LevelScale.ScaleX = volumeMeter;
        LevelText.Text = volume > .0001 ? T("Музыка играет") : T("Ожидание звука");
        BpmText.Text = IsDemoRecording ? "100 BPM" : paused ? T("BPM · пауза") : raw < .005 ? T("BPM · ожидание музыки") : tempo.HasEstimate ? $"{Math.Round(60 / tempo.Period):0} BPM" : T("BPM · определяем…");
        AnimateParticles(t, dt);
        var spectrum = IsDemoRecording?soundSpectrum:audio?.Spectrum; for (int i=0; i<Decor.Children.Count; i++) if (Decor.Children[i] is System.Windows.Shapes.Rectangle bar) { double value = paused || spectrum == null ? 0 : Math.Clamp(Math.Sqrt(EqualizerSample(spectrum, i, Decor.Children.Count) * FixedSensitivity * 4), 0, 1); bars[i] += (value - bars[i]) * Blend(value > bars[i] ? .212 : .05, dt); double h = 5 + bars[i] * 120; bar.Height = h; Canvas.SetTop(bar, Math.Max(0, Stage.ActualHeight - 78 - h)); bar.Opacity = .35 + bars[i] * .55; }
    }
    private delegate bool WindowCallback(IntPtr window, IntPtr data);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, IntPtr data);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, System.Text.StringBuilder text, int maxCount);
    private async void UpdateSpotifyTitle() => await UpdateSpotifyMedia();
    private string? ReadSpotifyTitle()
    {
        var ids = new HashSet<uint>();
        foreach (var process in System.Diagnostics.Process.GetProcessesByName("Spotify")) { using (process) { try { ids.Add((uint)process.Id); } catch (InvalidOperationException) { } } }
        string? title = null;
        EnumWindows((window, _) => { GetWindowThreadProcessId(window, out uint id); if (!ids.Contains(id)) return true; var text = new System.Text.StringBuilder(512); GetWindowText(window, text, text.Capacity); var name = text.ToString().Trim(); if (name.Contains(" - ")) { title = name; return false; } return true; }, IntPtr.Zero);
        return title;
    }
    private void InitParticles()
    {
        Particles.Children.Clear(); particles.Clear();
        if (Stage.ActualWidth < 10 || Stage.ActualHeight < 10) return;
        for (int i = 0; i < (sceneFullscreen ? 140 : 70); i++)
        {
            double size = 2 + random.NextDouble() * 2.5;
            var shape = new System.Windows.Shapes.Ellipse { Width = size, Height = size, Fill = new SolidColorBrush(accentColor), Opacity = .3 + random.NextDouble() * .3 };
            var particle = new Particle { X = random.NextDouble() * Stage.ActualWidth, Y = random.NextDouble() * Stage.ActualHeight, Shape = shape };
            shape.RenderTransform = particle.Position; particles.Add(particle); Particles.Children.Add(shape);
        }
    }
    private void Stage_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) { mousePoint = e.GetPosition(Stage); MoveTubes(mousePoint.Value); }
    private void Stage_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) { mousePoint = null; LeaveTubes(); }
    private void Stage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(Stage);
        CursorFx.Click(p, watch.Elapsed.TotalSeconds);
    }
    private void AnimateParticles(double t, double dt)
    {
        CursorFx.Animate(t, dt);
        double step = Math.Min(dt * 60, 3);
        foreach (var p in particles)
        {
            if (mousePoint is Point mouse)
            {
                double dx = p.X + p.Dx - mouse.X, dy = p.Y + p.Dy - mouse.Y, distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > .1 && distance < 100) { double force = (1 - distance / 100) * .65 * step; p.Vx += dx / distance * force; p.Vy += dy / distance * force; }
            }
            var waveForce = CursorFx.WaveForce(new Point(p.X + p.Dx, p.Y + p.Dy)); p.Vx += waveForce.X * step; p.Vy += waveForce.Y * step;
            p.Vx += -p.Dx * .012 * step; p.Vy += -p.Dy * .012 * step;
            double damping = Math.Pow(.88, step); p.Vx *= damping; p.Vy *= damping; p.Dx += p.Vx * step; p.Dy += p.Vy * step;
            p.Position.X = p.X + p.Dx; p.Position.Y = p.Y + p.Dy;
        }
    }
    private void Stage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        InitParticles(); RebuildEqualizer();
    }
    private void RebuildEqualizer()
    {
        if(Decor==null)return;
        int count=sceneFullscreen?78:52;
        Decor.Children.Clear(); Array.Clear(bars);
        var brush=new SolidColorBrush(accentColor);brush.Freeze();
        for(int i=0;i<count;i++){
            var bar=new System.Windows.Shapes.Rectangle { Width=5, Height=5, RadiusX=2.5, RadiusY=2.5, Fill=brush, Opacity=.25 };
            Canvas.SetLeft(bar,(i+1)*Stage.ActualWidth/(count+1));Decor.Children.Add(bar);
        }
    }
    private static double EqualizerSample(float[] spectrum,int index,int count)
    {
        if(spectrum.Length==0)return 0;
        double position=index*(spectrum.Length-1d)/Math.Max(1,count-1);
        int left=(int)position,right=Math.Min(left+1,spectrum.Length-1);
        return spectrum[left]+(spectrum[right]-spectrum[left])*(position-left);
    }
    private void Background_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string color) ApplyBackground(color);
    }
    private void ApplyBackground(string value)
    {
        try
        {
            Color color = (Color)ColorConverter.ConvertFromString(value);
            backgroundColor = value;
            accentColor = value.ToUpperInvariant() switch { "#1F4261" => Color.FromRgb(118, 183, 242), "#1C4C3C" => Color.FromRgb(114, 216, 174), "#5D2C43" => Color.FromRgb(243, 149, 167), "#5C3B23" => Color.FromRgb(247, 186, 114), "#3C414C" => Color.FromRgb(199, 203, 214), _ => Color.FromRgb(183, 154, 255) };
            CursorFx.Accent = accentColor;
            var accent = new SolidColorBrush(accentColor);
            Resources["ThemeAccent"] = accent;
            Resources["ThemeAccentLight"] = new SolidColorBrush(Color.FromRgb((byte)((accentColor.R + 255) / 2), (byte)((accentColor.G + 255) / 2), (byte)((accentColor.B + 255) / 2)));
            foreach (var particle in particles) particle.Shape.Fill = accent;
            foreach (var child in Decor.Children) if (child is System.Windows.Shapes.Rectangle bar) bar.Fill = accent;
            ToggleButton.Background = new SolidColorBrush(Color.FromRgb((byte)(accentColor.R * .7), (byte)(accentColor.G * .7), (byte)(accentColor.B * .7)));
            LevelFill.Background = accent;
            BassGlow.Fill = new RadialGradientBrush { GradientStops = new GradientStopCollection { new(Color.FromArgb(204, accentColor.R, accentColor.G, accentColor.B), 0), new(Color.FromArgb(102, accentColor.R, accentColor.G, accentColor.B), .42), new(Color.FromArgb(0, accentColor.R, accentColor.G, accentColor.B), 1) } };
            Glow.Fill = new SolidColorBrush(Color.FromRgb((byte)(accentColor.R * .42), (byte)(accentColor.G * .36), (byte)(accentColor.B * .48)));
            Background = new SolidColorBrush(Color.FromRgb((byte)(color.R * .22), (byte)(color.G * .22), (byte)(color.B * .22)));
            SceneBackground.Background = new RadialGradientBrush { Center = new Point(.5, .55), RadiusX = .75, RadiusY = .75,
                GradientStops = new GradientStopCollection { new(color, 0), new(Color.FromRgb((byte)(color.R * .46), (byte)(color.G * .46), (byte)(color.B * .46)), .75), new(Color.FromRgb((byte)(color.R * .36), (byte)(color.G * .36), (byte)(color.B * .36)), 1) } };
        }
        catch (FormatException) { }
    }
    private void LoadGallery()
    {
        string folder = Path.Combine(Directory.GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))?.FullName ?? "", "IMG for Desktop Dancer");
        if (!Directory.Exists(folder)) return;
        string[] preferred = { "Кот в деловом костюме", "Пиздося в худи серьезная", "Пиздося в худи", "Пачулька", "Чапулька" };
        foreach (string path in Directory.EnumerateFiles(folder).Where(p => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" }.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase)).OrderBy(p => { int index = Array.FindIndex(preferred, name => string.Equals(name, Path.GetFileNameWithoutExtension(p), StringComparison.OrdinalIgnoreCase)); return index < 0 ? int.MaxValue : index; }).ThenBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
        {
            try
            {
                var thumb = new BitmapImage(); thumb.BeginInit(); thumb.CacheOption = BitmapCacheOption.OnLoad; thumb.DecodePixelWidth = 100; thumb.UriSource = new Uri(path); thumb.EndInit(); thumb.Freeze();
                var image = new Image { Source = thumb, Width = 60, Height = 70, Stretch = Stretch.Uniform };
                var button = new Button { Content = image, Tag = Path.GetFileNameWithoutExtension(path), ToolTip = T(Path.GetFileNameWithoutExtension(path)), Padding = new Thickness(5), Margin = new Thickness(0, 0, 7, 0) };
                button.Click += (_, _) => { try { SetImage(path); } catch { MessageBox.Show(T("Не удалось открыть изображение."), "Desktop Dancer"); } };
                CharacterGallery.Children.Add(button);
            }
            catch (Exception ex) when (ex is IOException || ex is NotSupportedException || ex is ArgumentException) { }
        }
    }
    private void SetImage(string? path)
    {
        string actual = path ?? Path.Combine(AppContext.BaseDirectory, "Assets", "default-character.png");
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.UriSource = new Uri(actual, UriKind.Absolute); bitmap.EndInit(); bitmap.Freeze(); Character.Source = bitmap; imagePath = path; CharacterName.Text = CharacterDisplayName(path);
    }
    private void Choose_Click(object sender, RoutedEventArgs e) { var dialog = new OpenFileDialog { Filter = T("Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|Все файлы|*.*"), Title = T("Выберите персонажа") }; if (dialog.ShowDialog() == true) try { SetImage(dialog.FileName); } catch { MessageBox.Show(T("Не удалось открыть изображение. Попробуйте PNG или JPG."), "Desktop Dancer"); } }
    private void Pet_Click(object sender, RoutedEventArgs e) { if (pet != null) { pet.Close(); return; } var window = new PetWindow(Character.Source,language); pet = window; window.Closed += (_, _) => { pet = null; PetButton.Content = T("✦  Вывести питомца"); }; PetButton.Content = T("✕  Убрать питомца"); window.Show(); }
    private void Toggle_Click(object sender, RoutedEventArgs e) { paused = !paused; ToggleButton.Content = paused ? T("▶  Продолжить") : T("⏸  Пауза"); }
    private void Reconnect_Click(object sender, RoutedEventArgs e) => Connect();
    private void LoadSettings() { try { if (File.Exists(settingsFile)) { var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsFile)); if (s != null) { ApplyLanguage(s.Language); DuetMode.IsChecked=s.Duet; BattleMode.IsChecked=s.Battle; partnerPath=s.PartnerPath; SizeSlider.Value = s.Size; ShakeOnly.IsChecked = s.ShakeOnly; NoFlip.IsChecked = s.NoFlip; FewerJumps.IsChecked = s.FewerJumps; HeavyBass.IsChecked = s.HeavyBass; WanderingTrail.IsChecked = s.WanderingTrail && !s.HideTrail; HideTrail.IsChecked=s.HideTrail; SpotifyLyricsMode.IsChecked=s.SpotifyLyrics; if (!string.IsNullOrEmpty(s.BackgroundColor)) ApplyBackground(s.BackgroundColor); if (s.ImagePath != null) { string selected = s.ImagePath; if (!File.Exists(selected) && Path.GetFileName(selected).Equals("Пищдося в худи серьезная.png", StringComparison.OrdinalIgnoreCase)) selected = Path.Combine(Path.GetDirectoryName(selected)!, "Пиздося в худи серьезная.png"); if (File.Exists(selected)) { SetImage(selected); return; } } } } } catch { } SetImage(null); }
    private void SaveSettings() { try { Directory.CreateDirectory(Path.GetDirectoryName(settingsFile)!); File.WriteAllText(settingsFile, JsonSerializer.Serialize(new Settings { Language=language, Sensitivity=FixedSensitivity, Energy=FixedEnergy, Duet=DuetMode.IsChecked==true, Battle=BattleMode.IsChecked==true, PartnerPath=partnerPath, Size=SizeSlider.Value, ShakeOnly=ShakeOnly.IsChecked == true, NoFlip=NoFlip.IsChecked == true, FewerJumps=FewerJumps.IsChecked == true, HeavyBass=HeavyBass.IsChecked == true, WanderingTrail=WanderingTrail.IsChecked == true, HideTrail=HideTrail.IsChecked == true, SpotifyLyrics=SpotifyLyricsMode.IsChecked == true, BackgroundColor=backgroundColor, ImagePath=imagePath })); } catch { } }
}
