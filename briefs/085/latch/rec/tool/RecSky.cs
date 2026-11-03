using System; using System.Collections; using System.Collections.Generic; using System.Globalization; using System.IO; using System.Reflection; using System.Text.RegularExpressions;
using Suborned.Play; using Suborned.Sim; using UnityEngine; using UnityEngine.InputSystem;
using Order = Suborned.Sim.Order; using Path = System.IO.Path; using Target = Suborned.Play.HandoverHost.Target;

/// <summary>Sky's saved games on film (he asked for a video of his play, build 0.2.212): his script's orders at their ticks through h.Issue (as they are, no re-pointing), his selection, ring opens and click pops from the host record, all view only.
/// The camera is chosen here (not recorded): play zoom 10, following the last ordered agent, a ring's target, or the shots. `skycheck` runs the same path flat out with scenes ended and rings opened, to prove the checkpoints before filming.</summary>
public sealed partial class Rec085
{
    [Serializable] sealed class Side { public int tick; public float t; public string ring = "", on = "", way = "", text = "", icon = "", click = ""; public string[] sel; }
    sealed class RingEv { public int open, close; public string on, way, closeHow, ways = ""; public Target T; }
    sealed class Hit { public float at; public Vector3 from, to; public bool cell; }
    static string ArgOf(string flag) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, flag); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    readonly List<(Side s, string raw)> side = new(); int si; readonly List<RingEv> rings = new(); RingEv ringNow; int ri; List<Order> skyOrders = new(); int skyI; bool refusedThisTick; int refuseTick = -1;
    readonly List<(int from, int to, float hold)> holds = new(); readonly HashSet<int> sceneDone = new(); int ringOpened, ringMissed, ringSameWays, ringOtherWays, wedgeMissed, popsReplayed, saidAcks;
    MethodInfo mClickRing, mTargetAt, mSayOrder, mWedge;

    void ReadSide(string path)
    {
        RingEv open = null; Side prev = null; string prevRaw = null;
        foreach (var line in File.ReadLines(path))
        {
            var s = JsonUtility.FromJson<Side>(line); side.Add((s, line));
            if (prev != null) { float over = s.t - prev.t - (s.tick - prev.tick) / 10f; if (over > 1f) holds.Add((prev.tick, s.tick, over)); }
            prev = s; prevRaw = line;
            if (!line.Contains("\"ring\":")) continue;
            if (s.ring is "open" or "open-shut")
            {
                if (open != null && open.on == s.on) { if (line.Contains("\"ways\":")) open.ways = Regex.Match(line, "\"ways\":\"([^\"]*)\"").Groups[1].Value; continue; }
                open = new RingEv { open = s.tick, on = s.on, close = int.MaxValue }; rings.Add(open);
            }
            else if (open != null) { open.close = s.tick; open.closeHow = s.ring; open.way = s.way; open = null; }
        }
        Note($"host record: {side.Count} lines, {rings.Count} ring opens, his holds of the clock over 1 s: {string.Join(", ", holds.ConvertAll(x => $"ticks {x.from}-{x.to} {x.hold:0.0} s"))}");
    }

    Vector3? RingWorld(RingEv e) { e.T ??= MakeTarget(e.on); return h.TargetWorld(e.T); }
    Target MakeTarget(string on) { int c = on.IndexOf('/'); var kind = c > 0 ? on[..c] : on; var key = c > 0 ? on[(c + 1)..] : ""; return new Target { Kind = kind, Key = key, Label = key }; }

    void SkySelect(string[] names)
    {
        if (names == null || names.Length == 0) { Array.Clear(h.Selected, 0, 4); return; }
        for (int i = 0; i < names.Length; i++) h.Select(Op.Parse(names[i]), i > 0);
    }

    /// <summary>Everything of Sky's at this tick, before the step that leaves it: the host record, then his orders.</summary>
    void SkyTick(HandoverHost host, bool check)
    {
        int tk = host.Game.Tick; var pops = new List<Side>();
        while (si < side.Count && side[si].s.tick <= tk)
        {
            var (s, raw) = side[si++];
            if (raw.Contains("\"sel\":")) SkySelect(s.sel);
            else if (raw.Contains("\"pop\":")) pops.Add(s);
            else if (raw.Contains("\"ring\":") && s.ring is "open" or "open-shut") { if (ringNow == null && ri < rings.Count && rings[ri].open == s.tick && rings[ri].on == s.on) SkyRingOpen(rings[ri++], check); }
            else if (raw.Contains("\"ring\":") && ringNow != null && ringNow.on == s.on) { host.CloseRing(); ringNow = null; if (!check) ParkMouse(); }
        }
        if (ringNow != null && !check && ringNow.way != null && ringNow.close - tk == 7) SkyWedge(ringNow);
        bool refused = false;
        while (skyI < skyOrders.Count && skyOrders[skyI].Tick <= tk && !host.Game.Over)
        {
            var o = skyOrders[skyI++]; bool ok = host.Issue(o); if (!ok) { refused = true; Note($"his order refused as it was for him? {o.Op} {o.Do} {o.Target}"); }
            if (ok && o.Op != "cell" && o.Do is "watch" or "listen" or "turn" or "loop" or "disable" or "enter" or "reroute" or "take" or "board" or "carry" or "restrain") { try { mSayOrder.Invoke(host, new object[] { Op.Parse(o.Op), o, null }); saidAcks++; } catch (Exception e) { Note("ack: " + e.GetBaseException().Message); } }
            BeatFrom(o);
        }
        foreach (var p in pops) { if (refused || p.text == "Camera shot out") continue; host.Pop(p.text, null, SelectedAt(), HandoverHost.Feedback.Info, 2.5f, p.icon); popsReplayed++; }
    }
    Vector3? SelectedAt() { for (int k = 0; k < 4; k++) if (h.Selected[k]) return h.WorldOf(Op.Name((OpId)k)); return null; }

    void SkyRingOpen(RingEv e, bool check)
    {
        var w = RingWorld(e); if (!w.HasValue) { ringMissed++; Note($"ring on {e.on}: nothing drawn"); return; }
        var sp3 = h.View.WorldToScreenPoint(w.Value + Vector3.up); var sp = new Vector2(sp3.x, sp3.y);
        var t = (Target)mTargetAt.Invoke(h, new object[] { sp, false }); if (t != null && $"{t.Kind}/{t.Key}" == e.on) e.T = t;
        if (!check) Mouse2(sp);
        bool ok = (bool)mClickRing.Invoke(h, new object[] { e.T, sp });
        if (!ok || h.Ring == null) { ringMissed++; Note($"ring on {e.on}: did not open (his ways {e.ways})"); return; }
        ringOpened++; ringNow = e;
        var ways = string.Join(" | ", h.Ring.Segs.ConvertAll(x => x.More ? "..." : (x.Shut ? "~" : "") + x.Word + (x.Shut && x.ShutText.Length > 0 ? " (" + x.ShutText + ")" : "")));
        if (ways == e.ways) ringSameWays++; else if (e.ways.Length > 0) { ringOtherWays++; Note($"ring on {e.on}: mine [{ways}], his [{e.ways}]"); }
        Note($"ring open on {e.on}, sel {SelWords()}: [{ways}] until tick {e.close} ({e.closeHow} {e.way})");
    }
    string SelWords() { var l = new List<string>(); for (int k = 0; k < 4; k++) if (h.Selected[k]) l.Add(Op.Name((OpId)k)); return string.Join("+", l); }
    void SkyWedge(RingEv e)
    {
        var rs = h.Ring; if (rs == null || string.IsNullOrEmpty(e.way)) return;
        int i = rs.Segs.FindIndex(x => e.way == "..." ? x.More : x.Word == e.way); if (i < 0) i = rs.Segs.FindIndex(x => x.Word.StartsWith(e.way)); if (i < 0) { wedgeMissed++; return; }
        var at = (Vector2)mWedge.Invoke(null, new object[] { rs, i }); Mouse2(at);
    }
    static void ParkMouse() => Mouse2(new Vector2(Screen.width * 0.5f, 6f));

    // ---- the camera: chosen here, never Sky's
    readonly List<(int tick, List<string> who)> beats = new(); int beatI; RingEv wantRing; List<string> subject; float subjectAt = -100f; int zone, nextLog; Vector3 camVel, camAt, anchorCafe, anchorOffice; readonly List<Hit> hits = new(); int logAt;
    void BeatFrom(Order o) { }
    void BuildBeats()
    {
        foreach (var o in skyOrders)
        {
            if (o.Op == "cell" || o.Do is "holster" or "draw" or "cancel" || o.Do == "drive") continue;
            var who = new List<string> { o.Op }; if (o.Do == "fire" && !string.IsNullOrEmpty(o.Target)) who.Add(o.Target);
            if (beats.Count > 0 && beats[^1].tick == o.Tick) { foreach (var w in who) if (!beats[^1].who.Contains(w)) beats[^1].who.Add(w); continue; }
            beats.Add((o.Tick, who));
        }
    }
    Vector3? Where(string id)
    {
        if (id.StartsWith("camera:")) return h.TargetWorld(new Target { Kind = "camera", Key = id[7..] });
        if (id.StartsWith("car:")) return h.TargetWorld(new Target { Kind = "car", Key = id[4..] });
        return h.DrawnAt(id);
    }
    Vector3? Centre(List<string> ids) { Vector3 c = Vector3.zero; int n = 0; Vector3? first = null; foreach (var id in ids) { var w = Where(id); if (!w.HasValue) continue; first ??= w; if ((w.Value - first.Value).magnitude > 24f) continue; c += w.Value; n++; } return n > 0 ? c / n : (Vector3?)null; }

    void ReadShots()
    {
        var g = G; for (; logAt < g.Log.Count; logAt++)
        {
            var l = g.Log[logAt]; if (!l.Code.StartsWith("shot.")) continue;
            int f = l.Text.IndexOf(" fires on ", StringComparison.Ordinal), a = f > 0 ? l.Text.IndexOf(" at ", f + 1, StringComparison.Ordinal) : -1; if (a <= f) continue;
            string by = l.Text[..f], t = l.Text[(f + 10)..a]; var fw = h.DrawnAt(by); var tw = h.DrawnAt(t); if (!fw.HasValue) continue;
            hits.Add(new Hit { at = Time.unscaledTime, from = fw.Value, to = tw ?? fw.Value, cell = IsCell(by) || IsCell(t) });
        }
        hits.RemoveAll(x => Time.unscaledTime - x.at > 3.5f);
    }
    static bool IsCell(string id) => id is "scout" or "fixer" or "tech" or "enforcer";

    /// <summary>What the camera wants now: an open or coming ring's thing, else the shots of the last moments, else whoever was ordered last.</summary>
    Vector3? Want()
    {
        int tk = G.Tick;
        RingEv r = ringNow; if (r == null) for (int i = ri; i < rings.Count && rings[i].open - tk <= 25; i++) { if (rings[i].open >= tk) { r = rings[i]; break; } }
        wantRing = r; if (r != null && RingWorld(r) is Vector3 rw) return rw;
        if (hits.Count > 0)
        {
            var use = hits.FindAll(x => x.cell); if (use.Count == 0) use = hits;
            var lastOne = use[^1]; Vector3 c = Vector3.zero; int n = 0;
            foreach (var x in use) { if ((x.from - lastOne.from).magnitude > 22f) continue; c += x.from + x.to; n += 2; }
            var mid = c / Mathf.Max(n, 1); if ((mid - lastOne.from).magnitude > 12f) mid = lastOne.from + (mid - lastOne.from).normalized * 12f;
            return mid;
        }
        while (beatI < beats.Count && beats[beatI].tick - 10 <= tk) { if (subject == null || Time.unscaledTime - subjectAt >= 5f || beatI + 1 >= beats.Count || beats[beatI + 1].tick - 10 > tk) { subject = beats[beatI].who; subjectAt = Time.unscaledTime; } beatI++; }
        return subject != null ? Centre(subject) : null;
    }
    void CamFrame()
    {
        ReadShots(); if (h.ConvoOn || h.ConvoPending || h.FailCamOn || h.Ending.Length > 0) { camVel = Vector3.zero; return; }
        var want = Want(); if (!want.HasValue) return; var to = want.Value; to.y = 0f;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f); camAt = Vector3.SmoothDamp(camAt, to, ref camVel, 0.7f, 60f, dt); h.LookAt(camAt);
        // one turn a game: from the opening yaw to the Records Office front when the first fight comes there, and it stays (the camera never swings back and forth)
        bool fightDue = hits.Count > 0 || wantRing != null && wantRing.way == "Fire";
        if (zone == 0 && fightDue && (to - anchorOffice).sqrMagnitude < (to - anchorCafe).sqrMagnitude) { zone = 2; h.turnSeconds = 1.6f; h.TurnTo(zone); Note($"camera turns once to view {zone} (yaw {30 + 90 * zone})"); }
        if (h.View.orthographicSize != 10f) h.View.orthographicSize = 10f;
    }

    // ---- scenes: as long as Sky held each
    float HisHold(int tick) { float best = 0f; foreach (var (a, b, hold) in holds) if (tick >= a - 1 && tick <= b + 1 && hold > best) best = hold; return best; }
    IEnumerator SceneFrame()
    {
        if (!h.ConvoOn) yield break;
        var c = h.ConvoNow; int tk = G.Tick; float s0 = Time.unscaledTime, his = HisHold(tk), full = 0f; foreach (var (_, tx) in c.Lines) full += HandoverHost.ReadS(tx);
        Note($"scene at tick {tk}: {h.ConvoLast}; {c.Lines.Count} lines, about {full:0.0} s whole; Sky's clock was held {his:0.0} s here");
        bool cut = false; int beat = -1;
        while (h.ConvoOn)
        {
            if (c.Beat != beat) { beat = c.Beat; Note($"  line {beat}: {HudToolkit.ConvoSaid}"); }
            if (!cut && his > 0.4f && his < full - 0.5f && Time.unscaledTime - s0 >= his - 0.3f) { cut = true; h.ConvoSkip(); Note($"  ended at his {his:0.0} s, as he left it"); }
            yield return null;
        }
        Note($"  scene over after {Time.unscaledTime - s0:0.0} s");
    }

    IEnumerator Sky(bool check)
    {
        var scriptPath = ArgOf("-recscript"); var sidePath = ArgOf("-recside"); var text = File.ReadAllText(scriptPath);
        int seed = int.Parse(Regex.Match(text, "\"seed\":\\s*(\\d+)").Groups[1].Value, CultureInfo.InvariantCulture); var wantHash = ArgOf("-rechash") ?? "";
        var sc = Script.Parse(text); skyOrders = new List<Order>(sc.Orders); ReadSide(sidePath); BuildBeats();
        var ht = typeof(HandoverHost);
        mClickRing = ht.GetMethod("ClickRing", Any, null, new[] { typeof(Target), typeof(Vector2) }, null); mTargetAt = ht.GetMethod("TargetAt", Any); mSayOrder = ht.GetMethod("SayOrder", Any); mWedge = ht.GetMethod("WedgeScreen", Any);
        if (mClickRing == null || mTargetAt == null || mSayOrder == null || mWedge == null) throw new Exception("a host method moved");
        if (Keyboard.current != null) InputSystem.DisableDevice(Keyboard.current);
        HandoverHost.BeforeStep = null; HandoverHost.SessionDirOverride = Path.GetDirectoryName(outFile); HandoverHost.SeedPin = seed;
        h.SetSpeed(1); h.Begin(); yield return null; h.IntelOpen = false; h.ShowBriefing = false; h.StartClock(); h.SetPaused(true); yield return null; h.View.orthographicSize = 10f;
        Note($"seed {G.Seed}, scenes {h.controls.conversations}, session {h.SessionPath}, {skyOrders.Count} orders; Automation hints {h.HintsOn}");
        anchorCafe = h.Views.Find("waitress").position; anchorOffice = h.Views.Find("guard").position; anchorCafe.y = anchorOffice.y = 0f;
        ParkMouse(); h.TurnSnap(0); camAt = h.View.transform.position; logAt = G.Log.Count;
        var st = h.WorldOf("scout"); if (st.HasValue) { camAt = st.Value; camAt.y = 0f; h.LookAt(camAt); }
        HandoverHost.BeforeStep = host => SkyTick(host, check);
        if (check)
        {
            int n = 0;
            while (h.Ending.Length == 0 && n++ < 6000)
            {
                h.RunTo(G.Tick + 1);
                if (h.ConvoPending || h.ConvoOn) { yield return null; var c = h.ConvoNow; float full = 0f; if (c != null) foreach (var (_, tx) in c.Lines) full += HandoverHost.ReadS(tx); Note($"scene at tick {G.Tick}: {h.ConvoLast}; about {full:0.0} s whole; his hold {HisHold(G.Tick):0.0} s"); h.ConvoEndNow(); yield return null; }
                if (n % 40 == 0) yield return null;
            }
        }
        else
        {
            tapBuf = new float[AudioSettings.outputSampleRate * 2 * 620]; AudioMaster.TapAt = 0; AudioMaster.Reset();
            RecOn(); yield return new WaitForSecondsRealtime(1.2f); h.SetPaused(false); Note("clock runs");
            while (h.Ending.Length == 0)
            {
                if (h.ConvoOn) yield return SceneFrame(); else { if (G.Tick >= nextLog) { nextLog = G.Tick + 100; Note($"on tick {G.Tick}: focus {camAt}, zone {zone}, sel {SelWords()}, shots near {hits.Count}"); } CamFrame(); yield return null; }
            }
            HandoverHost.BeforeStep = null; Note($"over: fail cam {h.FailCamOn}, {h.Ending}");
        }
        float w = Time.unscaledTime; while (h.Ending.Contains("checking") && Time.unscaledTime - w < 30f) yield return null;
        if (!check) { w = Time.unscaledTime; while (Time.unscaledTime - w < 3f) yield return null; while (h.FailCamOn && Time.unscaledTime - w < 45f) yield return null; float w2 = Time.unscaledTime; while (Time.unscaledTime - w2 < 6f) yield return null; RecOff(); int nt = AudioMaster.TapAt; using (var fs = File.Create(outFile + ".f32")) using (var bw = new BinaryWriter(fs)) for (int i = 0; i < nt; i++) bw.Write(tapBuf[i]); Note($"audio {nt / 2 / (float)AudioSettings.outputSampleRate:0.00} s at {AudioSettings.outputSampleRate}"); }
        var final = Regex.Match(h.Ending, "final ([0-9a-f]+)").Groups[1].Value; HandoverHost.BeforeStep = null;
        Note($"END: {h.Ending}"); Note($"final hash {final} (his {wantHash}): {(final == wantHash ? "SAME" : "DIFFERS")}"); Note($"rings: opened {ringOpened}, not opened {ringMissed}, ways same as his {ringSameWays}, other {ringOtherWays}; wedge not found {wedgeMissed}; click pops replayed {popsReplayed}; acks said {saidAcks}; orders given {skyI}/{skyOrders.Count}");
    }
}
