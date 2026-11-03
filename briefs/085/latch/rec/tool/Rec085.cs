using System; using System.Collections; using System.Collections.Generic; using System.Diagnostics; using System.IO;
using Suborned.Play; using Suborned.Sim; using UnityEngine; using UnityEngine.InputSystem; using UnityEngine.InputSystem.LowLevel;
using Debug = UnityEngine.Debug; using Path = System.IO.Path; using Order = Suborned.Sim.Order;

/// <summary>Recordings for Sky's phone, brief 085: a helper assembly beside a copy of the player, none of it in the game. Real time, the real player and HUD; frames through ScreenCapture, paced to the wall; dead walks are skipped by stepping the clock with recording off.</summary>
public sealed partial class Rec085 : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-rec085"); if (i < 0 || i + 2 >= a.Length) return;
        var go = new GameObject("rec085"); DontDestroyOnLoad(go); var r = go.AddComponent<Rec085>(); r.what = a[i + 1]; r.outFile = a[i + 2];
    }
    string what = "", outFile = "", label = ""; HandoverHost h; Game G => h.Game; bool rec; Process ff; Stream pipe; byte[] buf; double t0; long frames, dups; int segFrames; bool done;
    readonly List<string> notes = new(); List<Order> orders = new(); int oi;
    void Note(string s) { notes.Add($"[{(frames / 30f):0.0}s tick {(h != null && h.Game != null ? G.Tick : -1)}] {s}"); Debug.Log("rec085: " + s); }

    IEnumerator Start()
    {
        Application.targetFrameRate = 30; float w0 = Time.realtimeSinceStartup;
        while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - w0 > 90f) { Application.Quit(3); yield break; } yield return null; }
        yield return new WaitForSecondsRealtime(4f); Note($"screen {Screen.width}x{Screen.height}, {BuildStamp.Line}");
        StartCoroutine(Pump());
        IEnumerator run = what == "book" ? Book() : what == "squad" ? Squad() : what == "sky" ? Sky(false) : what == "skycheck" ? Sky(true) : what == "sound" ? Sound() : what == "route-a" ? RouteA() : what == "pressure" ? Pressure() : Camera();
        while (true) { object cur; try { if (!run.MoveNext()) break; cur = run.Current; } catch (Exception e) { Note("threw: " + e); break; } yield return cur; }
        rec = false; yield return null; yield return null; HandoverHost.BeforeStep = null; done = true;
        try { pipe?.Close(); ff?.WaitForExit(); } catch (Exception e) { Note("ffmpeg: " + e.Message); }
        Note($"frames {frames}, repeated {dups}"); File.WriteAllLines(outFile + ".notes.txt", notes); Application.Quit(0);
    }

    IEnumerator Pump()
    {
        var eof = new WaitForEndOfFrame();
        while (!done)
        {
            yield return eof; if (!rec) { continue; }
            var t = ScreenCapture.CaptureScreenshotAsTexture();
            if (ff == null) StartFf(t);
            var raw = t.GetRawTextureData<byte>(); if (buf == null || buf.Length != raw.Length) buf = new byte[raw.Length]; raw.CopyTo(buf); Destroy(t);
            double now = Time.realtimeSinceStartupAsDouble; if (segFrames == 0) t0 = now;
            pipe.Write(buf, 0, buf.Length); frames++; segFrames++;
            now = Time.realtimeSinceStartupAsDouble;
            while (now > t0 + segFrames / 30.0 + 1 / 30.0) { pipe.Write(buf, 0, buf.Length); frames++; segFrames++; dups++; now = Time.realtimeSinceStartupAsDouble; }
            while (now < t0 + segFrames / 30.0) { if (t0 + segFrames / 30.0 - now > 0.002) System.Threading.Thread.Sleep(1); now = Time.realtimeSinceStartupAsDouble; }
        }
    }
    void StartFf(Texture2D t)
    {
        string fmt = t.format switch { TextureFormat.RGBA32 => "rgba", TextureFormat.ARGB32 => "argb", TextureFormat.BGRA32 => "bgra", _ => "rgb24" }; Note($"capture {t.width}x{t.height} {t.format}");
        var si = new ProcessStartInfo("/opt/homebrew/bin/ffmpeg", $"-y -v error -f rawvideo -pix_fmt {fmt} -s {t.width}x{t.height} -r 30 -i - -vf vflip -c:v libx264 -preset veryfast -crf 16 -threads 4 -pix_fmt yuv420p \"{outFile}\"") { UseShellExecute = false, RedirectStandardInput = true, CreateNoWindow = true };
        ff = Process.Start(si); pipe = ff.StandardInput.BaseStream;
    }
    void RecOn() { segFrames = 0; rec = true; if (tapBuf != null) AudioMaster.Tap = tapBuf; }
    void RecOff() { rec = false; AudioMaster.Tap = null; }
    float[] tapBuf; Vector2 dotAt; bool dotOn, dotDown;

    void OnGUI()
    {
        if (dotOn) { var kp = GUI.color; float ds = dotDown ? 22f : 14f; GUI.color = dotDown ? new Color(0.38f, 0.88f, 0.92f, 0.95f) : new Color(1f, 1f, 1f, 0.9f); GUI.DrawTexture(new Rect(dotAt.x - ds / 2f, Screen.height - dotAt.y - ds / 2f, ds, ds), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, GUI.color, 0f, ds / 2f); GUI.color = kp; }
        if (label.Length == 0) return; var st = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height / 30f), alignment = TextAnchor.MiddleCenter }; st.normal.textColor = Color.white;
        var c = new GUIContent(label); var s = st.CalcSize(c); var r = new Rect((Screen.width - s.x - 24f) / 2f, Screen.height * 0.02f, s.x + 24f, s.y + 10f);
        var keep = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.85f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = keep; GUI.Label(r, c, st);
    }

    // ---- driving
    static List<Order> Route(string name) { var path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), "contracts", "missions", "handover", "routes", name + ".json"); var sc = Script.Parse(File.ReadAllText(path)); var l = new List<(Order o, int i)>(); for (int i = 0; i < sc.Orders.Count; i++) l.Add((sc.Orders[i], i)); l.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.i.CompareTo(b.i)); var r = new List<Order>(); foreach (var (o, _) in l) r.Add(o); return r; }
    void Give(HandoverHost host, Order o) { var c = new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run }; if (c.Do is "watch" or "listen" or "turn" or "call" && c.Op != "cell") host.GiveScripted(c); else host.Issue(c); }
    IEnumerator Fresh(string route, Func<Order, bool> keep = null)
    {
        HandoverHost.BeforeStep = null; h.controls.conversations = ConversationScenes.Always; h.controls.planTogether = false; h.SetSpeed(1); h.Begin(); yield return null; h.IntelOpen = false; h.ShowBriefing = false; h.StartClock(); h.SetPaused(true); yield return null; h.View.orthographicSize = 10f;
        orders = Route(route); if (keep != null) orders = orders.FindAll(o => keep(o)); oi = 0;
        HandoverHost.BeforeStep = host => { while (oi < orders.Count && orders[oi].Tick <= host.Game.Tick && !host.Game.Over && !host.ConvoPending) Give(host, orders[oi++]); };
    }
    /// <summary>Dead time: the clock stepped with recording off, up to a condition; a scene nobody asked to film is ended.</summary>
    IEnumerator Jump(Func<bool> until, int max = 4000, bool endScenes = true)
    {
        h.SetPaused(true); int n = 0;
        while (!until() && !G.Over && n++ < max)
        {
            h.RunTo(G.Tick + 1);
            if (h.ConvoPending) { if (endScenes) { Note("skipped a scene: " + h.ConvoLast); h.ConvoEndNow(); } else break; }
            if (n % 40 == 0) yield return null;
        }
        yield return null;
    }
    bool Near(int k, string act, int left) { var o = G.Ops[k]; return o.Busy != null && (o.Busy.Do == act || o.Busy.Approach == act) && o.BusyEnd - G.Tick <= left; }
    /// <summary>Recording, the clock running, until a scene has played whole; a tail after it.</summary>
    IEnumerator Scene(string name, float tail, Action atRecord = null)
    {
        RecOn(); h.SetPaused(false); float w = Time.unscaledTime; atRecord?.Invoke();
        while (!h.ConvoOn && Time.unscaledTime - w < 12f) yield return null;
        if (!h.ConvoOn) { Note($"{name}: no scene came: {h.ConvoLast}"); yield break; }
        var c = h.ConvoNow; Note($"{name}: {h.ConvoLast}; {c.Lines.Count} lines, walkers {h.Views.SceneWalkers}"); w = Time.unscaledTime; float s0 = w; int beat = -1;
        while (h.ConvoOn && Time.unscaledTime - w < 120f) { if (c.Beat != beat) { beat = c.Beat; Note($"  line {beat}: {HudToolkit.ConvoSaid}"); } yield return null; }
        Note($"{name}: {Time.unscaledTime - s0:0.0} s; walkers {h.Views.SceneWalkers}, turned {h.Views.SceneTurned}, stopped {h.Views.SceneStopped}");
        if (tail >= 1f) yield return new WaitForSecondsRealtime(0.3f); Note($"  card: {HudToolkit.PersonCardWords}, docked {HudToolkit.PersonCardDocked}");
        float tw = Time.unscaledTime; while (Time.unscaledTime - tw < tail || tail >= 1f && h.PersonCardNow != null) yield return null;
    }
    static void Mouse2(Vector2 sp, bool left = false) { if (Mouse.current is Mouse m) { var st = new MouseState { position = sp }; if (left) st = st.WithButton(MouseButton.Left, true); InputSystem.QueueStateEvent(m, st); } }
    IEnumerator RingOn(Transform t, float seconds, string name)
    {
        h.LookAt(t.position); yield return null; yield return null; var sp = h.View.WorldToScreenPoint(t.position + Vector3.up); var v = new Vector2(sp.x, sp.y);
        Mouse2(v); yield return new WaitForSecondsRealtime(0.5f); Mouse2(v, true); yield return new WaitForSecondsRealtime(0.1f); Mouse2(v); yield return new WaitForSecondsRealtime(0.2f);
        string f = ""; if (h.Ring != null) foreach (var s in h.Ring.Segs) if (s.Row?.I != null) { f += " | " + s.Row.I.Order?.Do + "/" + s.Row.I.Order?.Approach + ":"; foreach (var p in HudModel.PriceLine(h, s.Row.I)) f += " " + p.Text; }
        Note($"{name}: ring {h.Ring != null}{f}"); yield return new WaitForSecondsRealtime(seconds);
        if (h.Ring != null) h.CloseRing(); Mouse2(new Vector2(Screen.width * .5f, 40f)); yield return new WaitForSecondsRealtime(0.2f);
    }

    // ---- sound: the cafe street, the Help scene and its card, a watcher noticing, a short fight; the mix itself as the audio track
    IEnumerator Sound()
    {
        tapBuf = new float[AudioSettings.outputSampleRate * 2 * 170]; AudioMaster.TapAt = 0;
        yield return Fresh("route-a", o => o.Tick < 1000); h.TurnSnap(0); var inez = h.Views.Find("waitress"); h.LookAt(inez.position); h.Select(OpId.Fixer, false);
        yield return Jump(() => Near(1, "help", 100)); h.LookAt(inez.position); AudioMaster.Reset(); Note($"pack {h.PackRead}; {Settings.Current.audio.master}/{Settings.Current.audio.effects}/{Settings.Current.audio.ambience}");
        yield return Scene("help-1", 4f); Note($"after the Help: peak in {M(AudioMaster.PeakIn)}, out {M(AudioMaster.PeakOut)}, taken {AudioMaster.MostReductionDb:0.0} dB, RMS {AudioMaster.RmsMaxDb:0.0}");
        h.Select(OpId.Scout, false); var sc = h.Views.Find("op/scout"); var at = G.S.At("d:Office entrance"); h.Issue(new Order { Op = "scout", Do = "move", Point = G.Walk.NearestFree(at) ?? at, Run = true }); Note("the Scout runs for the Office door (mine)"); int n0 = G.Log.Count; float w = Time.unscaledTime; bool seen = false; while (Time.unscaledTime - w < 22f && !seen) { h.LookAt(sc.position); for (int j = n0; j < G.Log.Count; j++) if (G.Log[j].Code.StartsWith("attention.")) { seen = true; Note(G.Log[j].Code + ": " + G.Log[j].Text); } n0 = G.Log.Count; yield return null; }
        yield return new WaitForSecondsRealtime(3f); RecOff(); AudioMaster.Reset();
        var en = h.Views.Find("op/enforcer"); var near = new Pt(6925, -4250); h.Issue(new Order { Op = "enforcer", Do = "move", Point = near }); yield return Jump(() => G.Ops[3].Walk == null, 1500); h.Issue(new Order { Op = "enforcer", Do = "draw" }); h.LookAt(en.position); h.SetPaused(false); RecOn(); yield return new WaitForSecondsRealtime(1.5f);
        bool fired = h.Issue(new Order { Op = "enforcer", Do = "fire", Target = "guard" }); Note($"fire on the guard: {fired}"); w = Time.unscaledTime; n0 = G.Log.Count; int shots = 0; while (Time.unscaledTime - w < 24f) { h.LookAt(en.position); for (int j = n0; j < G.Log.Count; j++) { var c = G.Log[j].Code; if (c.StartsWith("shot.")) shots++; if (c.EndsWith(".down") || c == "alarm.dispatches" || c.StartsWith("patrol.")) Note(c); } n0 = G.Log.Count; yield return null; }
        Note($"fight: {shots} shots; peak in {M(AudioMaster.PeakIn)}, out {M(AudioMaster.PeakOut)}, taken {AudioMaster.MostReductionDb:0.0} dB, hard stops {AudioMaster.HardStops}, RMS {AudioMaster.RmsMaxDb:0.0}"); RecOff();
        var cues = new HashSet<string>(); foreach (var q in h.CuesPlayed) cues.Add(q.Split('@')[0]); Note("cues: " + string.Join(",", cues));
        int nt = AudioMaster.TapAt; using (var fs = File.Create(outFile + ".f32")) using (var bw = new BinaryWriter(fs)) for (int i = 0; i < nt; i++) bw.Write(tapBuf[i]); Note($"audio {nt / 2 / (float)AudioSettings.outputSampleRate:0.00} s at {AudioSettings.outputSampleRate}");
    }
    static string M(float v) => (20f * Mathf.Log10(Mathf.Max(v, 1e-6f))).ToString("0.0");

    // ---- 1
    IEnumerator RouteA()
    {
        yield return Fresh("route-a", o => !(o.Tick == 1296 && o.Op == "scout") && !(o.Tick == 0 && o.Op == "scout")); /* the Scout stays by Inez, so the guard's arrival is seen and spoken */ foreach (var o in orders) if (o.Tick > 1296) o.Tick += 5; /* the Call's scene takes the 1290 orders a tick late here, so the Enter comes a moment later */ h.TurnSnap(0); var inez = h.Views.Find("waitress"); h.LookAt(inez.position); h.Select(OpId.Scout, false);
        foreach (var o in orders) if (o.Tick >= 596) o.Tick += 20; /* heard at 1:00 from 4 m rather than 0:59: the Help comes just after */ { var door0 = G.S.At(G.S.GuardCafe); var pt = new Pt(door0.X + 450, door0.Y + 250); orders.Insert(0, new Order { Tick = 0, Op = "scout", Do = "move", Point = G.Walk.NearestFree(pt) ?? pt }); Note("mine, not the route's: the Scout goes 4 m off rather than to beside-inez"); }
        yield return Jump(() => G.Ops[0].Act?.Do == "listen" || G.Ops[0].Busy?.Do == "listen"); h.LookAt(inez.position); h.SetPaused(false); RecOn(); Note($"Listen given; spot {G.ListenSpotNow("scout")}"); while (G.Ops[0].Busy?.Do != "listen") yield return null; Note($"under way, Scout {Vector3.Distance(h.Views.Find("op/scout").position, inez.position):0.0} m from Inez"); yield return new WaitForSecondsRealtime(12f); RecOff(); Note("cut: most of the 30 s"); yield return Jump(() => Near(0, "listen", 40)); h.LookAt(inez.position); yield return Scene("listen-inez", 3f); RecOff();
        h.Select(OpId.Fixer, false); yield return Jump(() => Near(1, "help", 30)); h.LookAt(inez.position); yield return Scene("help-1", 2.5f); RecOff();
        yield return Jump(() => Near(1, "help", 30)); h.LookAt(inez.position); h.SetPaused(false);
        RecOn(); while (!Near(1, "help", 8)) yield return null;
        { var ip = G.PersonPos(G.Inez).Value; var fx = G.Ops[1].Pos; var sp0 = G.Ops[0].Pos; float a0 = Mathf.Atan2(fx.Y - ip.Y, fx.X - ip.X) + Mathf.PI / 2f; var to = new Pt(sp0.X + (int)(Mathf.Cos(a0) * 900f), sp0.Y + (int)(Mathf.Sin(a0) * 900f)); h.Issue(new Order { Op = "scout", Do = "move", Point = G.Walk.NearestFree(to) ?? to }); Note("the Scout walks across the shot (mine, not the route's)"); }
        yield return Scene("help-2", 0f); h.SetPaused(true); Note("  recording only: the clock paused while the Help's card shows; the route's Call lands a tick after this scene"); { float tc = Time.unscaledTime; while (h.PersonCardNow != null && Time.unscaledTime - tc < 6f) yield return null; } h.SetPaused(false); RecOff();
        h.LookAt(inez.position); int nLog = G.Log.Count; yield return Scene("call-inez", 0.5f);
        var door = h.ClipLookAt(); var gt = h.Views.Find("guard"); h.LookAt(G.Ops[3].Pos.Equals(default(Pt)) ? gt.position : h.Views.Find("op/enforcer").position); Note("after the call: on the Enforcer at the door");
        float w = Time.unscaledTime; string said = ""; while (Time.unscaledTime - w < 8f) { var en = h.Views.Find("op/enforcer"); if (en && en.gameObject.activeInHierarchy) h.LookAt(en.position); var co = h.Callouts.Find(x => x.Speech != null); if (co != null && said != co.Speech.Text) { said = co.Speech.Text; Note($"line by inset: {co.View}: {said}, docked {HudToolkit.DockedSpeech}"); } yield return null; }
        RecOff();
        var ef = G.Ops[3]; yield return Jump(() => G.Ops[3].Busy?.Do == "enter" && G.Ops[3].BusyEnd - G.Tick <= 102 + 25); var ex = h.ClipLookAt() ?? gt.position; h.LookAt(h.Views.Find("guard").position); h.SetPaused(false); RecOn();
        w = Time.unscaledTime; while (h.ClipNow != HandoverHost.ClipState.Playing && Time.unscaledTime - w < 8f) yield return null; Note($"clip {h.ClipNow}: {HudToolkit.ClipWordNow}");
        yield return new WaitForSecondsRealtime(4f); h.ClipClick(); Note("clip clicked: to the door"); w = Time.unscaledTime; while (h.ClipNow == HandoverHost.ClipState.Playing && Time.unscaledTime - w < 12f) yield return null; Note($"out: {h.ClipLast}, drift {h.ClipDriftAtOut:0.000}");
        yield return new WaitForSecondsRealtime(1f); h.ClipClick(); Note("clip clicked: to him"); w = Time.unscaledTime; while (Time.unscaledTime - w < 5f) { yield return null; } RecOff();
        // the guard's arrival comes after the Enforcer is out on this route: as far as it goes in the fight
        int from = G.Log.Count; yield return Jump(() => { for (int j = from; j < G.Log.Count; j++) if (G.Log[j].Code is "guard.at-cafe" or "guard.recalled") return true; return G.Tick >= 1900; }, 400); string last = ""; for (int j = from; j < G.Log.Count; j++) if (G.Log[j].Code.StartsWith("guard.")) last += " " + G.Log[j].Code; Note($"guard after the exit:{last}");
        if (last.Contains("at-cafe")) { var en = h.Views.Find("op/enforcer"); h.LookAt(en.position); h.SetPaused(false); RecOn(); w = Time.unscaledTime; said = ""; while (Time.unscaledTime - w < 7f) { h.LookAt(en.position); var co = h.Callouts.Find(x => x.Speech != null); if (co != null && said != co.Speech.Text) { said = co.Speech.Text; Note($"line by inset: {co.View}: {said}, docked {HudToolkit.DockedSpeech}"); } yield return null; } RecOff(); }
    }
    // ---- 2
    IEnumerator Pressure()
    {
        yield return Fresh("route-a-pressure", o => o.Approach != "pressure" && o.Tick != 846); h.TurnSnap(0); var inez = h.Views.Find("waitress"); h.LookAt(inez.position); h.Select(OpId.Scout, false);
        yield return Jump(() => h.ConvoPicks.Contains("listen-inez")); yield return Jump(() => G.Ops[0].Busy == null); h.Select(OpId.Fixer, false); h.LookAt(inez.position); yield return null; RecOn(); yield return new WaitForSecondsRealtime(0.6f); yield return RingOn(inez, 5f, "ring, Pressure way");
        h.GiveScripted(new Order { Tick = G.Tick, Op = "fixer", Do = "turn", Target = "waitress", Approach = "pressure" }); yield return new WaitForSecondsRealtime(0.5f); RecOff();
        yield return Jump(() => Near(1, "pressure", 30)); h.LookAt(inez.position); yield return Scene("pressure-inez", 2f); RecOff();
        yield return Jump(() => G.Ops[1].Busy == null, 20); h.SetPaused(true); h.LookAt(inez.position); yield return null; RecOn(); yield return RingOn(inez, 3f, "ring, Call way");
        Action call = () => { h.GiveScripted(new Order { Tick = G.Tick, Op = "fixer", Do = "call", Target = "waitress" }); h.Issue(new Order { Tick = G.Tick, Op = "fixer", Do = "move", Point = new Pt(725, -5025) }); h.Issue(new Order { Tick = G.Tick, Op = "scout", Do = "move", Point = new Pt(725, -4625) }); };
        yield return Scene("call-inez-pressure", 0.5f, call); RecOff();
        var gt = h.Views.Find("guard"); yield return Jump(() => G.GuardPose() is { } gp && G.PersonPos(G.Inez) is Pt ip && G.Dist2Ok(gp.pos, ip)); h.LookAt(gt.position); h.SetPaused(false); RecOn(); int nLog = G.Log.Count; float w = Time.unscaledTime; bool run = false; float runAt = 0f; string bub = ""; bool atc = false;
        while (Time.unscaledTime - w < 30f) { h.LookAt(gt.position); for (int j = nLog; j < G.Log.Count; j++) { if (G.Log[j].Code == "guard.at-cafe") { atc = true; Note("guard.at-cafe"); } if (G.Log[j].Code == "guard.leaves-cafe") Note("guard.leaves-cafe"); } nLog = G.Log.Count; if (h.Bubbles.Count > 0 && h.Bubbles[^1].Text != bub) { bub = h.Bubbles[^1].Text; Note($"bubble {h.Bubbles[^1].Speaker}: {bub}"); } if (G.GuardRunningBack && !run) { run = true; runAt = Time.unscaledTime; Note("running back"); } if (run && Time.unscaledTime - runAt > 8f) break; yield return null; }
        var (cl, sp) = h.Views.FigureClip("guard"); Note($"clip at the end {cl} {sp:0.00}; at café {atc}"); RecOff();
    }
    // ---- 3
    static CamPreset Preset(string name, bool persp, float zoom, float pitch, float fov) { var p = new CamPreset { name = name }; p.overhead.perspective = persp; p.overhead.zoom = zoom; p.overhead.zoomMin = 4f; p.overhead.zoomMax = 40f; p.overhead.pitch = pitch; p.overhead.fov = fov; return p; }
    IEnumerator Camera()
    {
        var views = new[] { ("Today: orthographic 10, 38°", Preset("a", false, 10f, 38f, 30f)), ("Orthographic 8, 38°", Preset("b", false, 8f, 38f, 30f)), ("Orthographic 10, 32°", Preset("c", false, 10f, 32f, 30f)), ("Perspective FOV 30, 38°", Preset("d", true, 8.9f, 38f, 30f)) };
        foreach (var (spot, turn) in new[] { ("Office door", 2), ("Café terrace", 0) })
            foreach (var (name, preset) in views)
            {
                yield return Fresh("route-a", o => o.Tick < 790); yield return Jump(() => G.Tick >= 800); h.Select(OpId.Scout, false);
                var at = spot == "Office door" ? h.Views.Find("guard").position : h.Views.Find("waitress").position; h.CamUse(preset); h.TurnSnap(turn); h.LookAt(at); for (int i = 0; i < 6; i++) yield return null;
                var cam = h.View; float z0 = preset.overhead.zoom; var right = cam.transform.right; right.y = 0f; right.Normalize(); var fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
                label = $"{name} · {spot}"; h.SetPaused(false); RecOn(); float w = Time.unscaledTime; Note(label);
                while (Time.unscaledTime - w < 10f) { float t = (Time.unscaledTime - w) / 10f, a = t * Mathf.PI * 2f; cam.orthographicSize = z0 * (1f - 0.18f * Mathf.Sin(a)); h.LookAt(at + right * (7f * Mathf.Sin(a)) + fwd * (3f * Mathf.Sin(2f * a))); yield return null; }
                RecOff(); label = ""; h.SetPaused(true); h.CamUse(new CamPreset()); yield return null;
            }
    }
}
static class RecExt { public static bool Dist2Ok(this Game g, Pt a, Pt b) { long dx = a.X - b.X, dy = a.Y - b.Y; return dx * dx + dy * dy <= 400L * 400L; } }
