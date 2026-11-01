using MusicDancer;
static int Run(Func<double,float[]> signal) { var d=new LowBassOnset(); int hits=0; for(int i=0;i<250;i++){double t=i*.02;var s=signal(t);bool hit=d.Update(s,t);if(d.Update(s,t+.001))throw new Exception("Repeated snapshot");if(hit)hits++;}return hits;}
static float[] Signal(float low,float mid=0){var s=new float[52];for(int i=0;i<3;i++)s[i]=low;for(int i=3;i<52;i++)s[i]=mid;return s;}
void Check(string name,int actual,int expected){if(actual!=expected)throw new Exception($"{name}: {actual}, expected {expected}");Console.WriteLine($"PASS {name}: {actual}");}
Check("Silence",Run(t=>Signal(0)),0);
Check("High frequencies only",Run(t=>Signal(.001f,.8f)),0);
Check("Moderate bass attacks",Run(t=>Signal(t>=.5&&(t-.5)%1<.12?.1f:.003f)),0);
Check("Sustained maximum",Run(t=>Signal(t<.5?.003f:.45f)),1);
Check("Slow swell to maximum",Run(t=>Signal((float)(.002+t*.1))),0);
Check("Four sharp maximum peaks",Run(t=>Signal(t>=.5&&t<4.5&&(t-.5)%1<.12?.45f:.003f)),4);
Check("Only one of three at maximum",Run(t=>{var s=Signal(.05f);s[0]=t>.5?.5f:0;return s;}),0);
var turn = new BattleTurn();
turn.Update(true, true, 1.9, .5, false);
Check("First dancer owns first four beats", turn.PrimaryBlend == 1 && turn.CanStartMove ? 1 : 0, 1);
turn.Update(true, true, .1, .5, true);
Check("Four-beat boundary waits for landing", turn.PrimaryBlend == 1 && !turn.CanStartMove ? 1 : 0, 1);
turn.Update(true, true, .6, .5, true);
Check("Flip stays with outgoing dancer", turn.PrimaryBlend == 1 && !turn.CanStartMove ? 1 : 0, 1);
turn.Update(true, true, .01, .5, false);
Check("Landing begins handoff without pose jump", turn.PrimaryBlend == 1 && !turn.CanStartMove ? 1 : 0, 1);
turn.Update(true, true, .09, .5, false);
Check("Handoff blends smoothly", Math.Abs(turn.PrimaryBlend - .5) < .00001 && !turn.CanStartMove ? 1 : 0, 1);
turn.Update(true, true, .09, .5, false);
Check("Incoming dancer owns new moves fully", turn.PrimaryBlend == 0 && turn.CanStartMove ? 1 : 0, 1);
turn.Update(true, false, 5, .5, false);
Check("Pause freezes turn", turn.PrimaryBlend == 0 && turn.CanStartMove ? 1 : 0, 1);
turn.Update(true, true, 1.82, .5, false);
turn.Update(true, true, .18, .5, false);
Check("Second dancer returns turn after four beats", turn.PrimaryBlend == 1 && turn.CanStartMove ? 1 : 0, 1);
turn.Update(false, true, .1, .5, false);
Check("Disabling battle restores primary dancer", turn.PrimaryBlend == 1 && turn.CanStartMove ? 1 : 0, 1);
var atmosphere = new BassAtmosphere();
Check("Backdrop idle", atmosphere.Sample(0) == (0d,0d,0d,0d) ? 1 : 0, 1);
atmosphere.Trigger(1);
Check("Bass begins without a position jump", atmosphere.Sample(1) == (0d,0d,0d,0d) ? 1 : 0, 1);
Check("Chromatic peak", atmosphere.Sample(1.085).Split > .7 ? 1 : 0, 1);
Check("Smooth tail settles", atmosphere.Sample(2).Split < .0001 ? 1 : 0, 1);
for(int i=0;i<50;i++) atmosphere.Trigger(3+i*.001);
var strong = atmosphere.Sample(3.12);
Check("Repeated hits stay bounded", strong.Split <= 1 && Math.Abs(strong.X)<=2.4 && Math.Abs(strong.Y)<=1.6 && Math.Abs(strong.Tilt)<=.16 ? 1 : 0, 1);
atmosphere.Reset();
Check("Pause clears backdrop motion", atmosphere.Sample(3.2) == (0d,0d,0d,0d) ? 1 : 0, 1);
atmosphere.Reset();
var heldBass = Signal(.45f);
for(int i=0;i<280;i++) atmosphere.UpdateSustain(heldBass, 1d/140);
var bed = atmosphere.Sample(5);
Check("Sustained bass holds effects", bed.Split > .4 ? 1 : 0, 1);
atmosphere.Trigger(5);
Check("New onset replays over sustained bass", atmosphere.Sample(5.085).Split > bed.Split + .3 ? 1 : 0, 1);
atmosphere.UpdateSustain(null, 1d/140);
Check("Sustain releases smoothly", atmosphere.Sample(7).Split > .3 ? 1 : 0, 1);
for(int i=0;i<280;i++) atmosphere.UpdateSustain(null, 1d/140);
Check("Bass ending clears effects", atmosphere.Sample(8).Split == 0 ? 1 : 0, 1);
for(int i=0;i<280;i++) atmosphere.UpdateSustain(Signal(.001f, .8f), 1d/140);
Check("High frequencies cannot sustain bass effects", atmosphere.Sample(9).Split == 0 ? 1 : 0, 1);

if (OperatingSystem.IsWindows()) {
 int ticks=0;
 using var frameClock=new TrailFrameClock(()=>System.Threading.Interlocked.Increment(ref ticks));
 frameClock.SetActive(true);await Task.Delay(600);frameClock.SetActive(false);await Task.Delay(40);
 Check("Native frame clock runs near 140 Hz", ticks>=60&&ticks<=100?1:0,1);
 int stopped=ticks;await Task.Delay(100);Check("Native frame clock stops while inactive", ticks,stopped);
 frameClock.SetActive(true);await Task.Delay(150);frameClock.SetActive(false);
 Check("Native frame clock resumes", ticks>stopped+10?1:0,1);
 frameClock.Dispose();stopped=ticks;await Task.Delay(50);Check("Native frame clock releases worker",ticks,stopped);
}

Check("Spotify highlighted line", SpotifyLyricSelection.ActiveIndex(new[]{11648767,16777215,11648767}),1);
Check("Spotify seek changes selection", SpotifyLyricSelection.ActiveIndex(new[]{11648767,11648767,16777215}),2);
Check("No guessed line when all white",SpotifyLyricSelection.ActiveIndex(new[]{16777215,16777215}),-1);
Check("No guessed line when all dim",SpotifyLyricSelection.ActiveIndex(new[]{11648767,11648767}),-1);
Check("Unsupported lyric style",SpotifyLyricSelection.ActiveIndex(new[]{-1,-1}),-1);
Check("Single highlighted lyric",SpotifyLyricSelection.ActiveIndex(new[]{16777215}),0);

var lyricStability=new SpotifyLyricStability();
var goodLyric=new SpotifyLyricFrame("before","active","next","synced",3);
var unavailableLyric=new SpotifyLyricFrame("","","","waiting");
lyricStability.Update(goodLyric,"track A",1);
Check("Keep genuine lyric during DOM replacement",lyricStability.Update(unavailableLyric,"track A",1.3).Index,3);
Check("Do not keep stale lyrics indefinitely",lyricStability.Update(unavailableLyric,"track A",2).Index,-1);
lyricStability.Update(goodLyric,"track A",3);
Check("Track change clears previous text immediately",lyricStability.Update(unavailableLyric,"track B",3.1).Index,-1);

Check("Missing language defaults to Russian",UiText.Normalize(null)=="ru"?1:0,1);
Check("Unknown language defaults to Russian",UiText.Normalize("fr")=="ru"?1:0,1);
Check("English preference restores",UiText.Normalize("en")=="en"?1:0,1);
Check("English language covers every built-in label",UiText.Entries.All(e=>!string.IsNullOrWhiteSpace(e.English)&&!System.Text.RegularExpressions.Regex.IsMatch(e.English,"[А-Яа-яЁё]"))?1:0,1);
Check("Russian labels remain unchanged",UiText.Entries.All(e=>UiText.Get(e.Russian,"ru")==e.Russian)?1:0,1);
Check("English labels resolve",UiText.Entries.All(e=>UiText.Get(e.Russian,"en")==e.English)?1:0,1);
Check("User content is preserved",UiText.Get("Мой собственный персонаж","en")=="Мой собственный персонаж"?1:0,1);
Check("Image filters keep their extensions",UiText.Entries.Where(e=>e.Key.EndsWith("Filter")).All(e=>e.Russian.Split('|').Where((_,i)=>i%2==1).SequenceEqual(e.English.Split('|').Where((_,i)=>i%2==1)))?1:0,1);
