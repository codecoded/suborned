using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;

namespace Suborned.Play
{
    /// <summary>Host round 4 for batch 0.2.123 (Aster, 2026-10-26), in the built player, one JSON object `r123`; pictures `r123-*` with their HUD
    /// layers and manifests (hud-budget, Glyph's 2x density check). Run after r067 in the full proof (-gameProof), or alone: `-proof123 &lt;dir&gt;`
    /// (Proof123Run: these sections only, then quit). Not gated yet (tools/game/proofgates.ts lists the gated sections; Splice's).
    ///  · fight (Sky 2026-10-26: "Simplify the visuals, it's way too crowded. Just having the agent facing who they're shooting should be enough
    ///    to not need those lines everywhere"): the safe fight at the square (Keel's fixture sq-ra-h3, engine 0.34.0's SQ2: the Scout, Fixer and
    ///    Tech fight it out at 4:15) and the loud fight (bal-loud-x4: the police in at 3:45, the volley at 3:46), each at play zoom (yaw 30, zoom 14)
    ///    on the cell's own fight (its agents on foot and what they fire at), the clock run 0.4 s for the turns, the cones drawn logged; then the
    ///    clock runs 1.5 s and every shooter's facing is measured each frame (HostViews.Aim: the target each view faces, its figure's drawn yaw; a
    ///    shooter is counted once it has faced its target 0.3 s, the turn's own time): the worst error, and the frames over 25°; then the safe
    ///    fight from the ground view (the Scout's third person, 1.2 s, the same measure). No line is drawn from a shooter to a target (the tracers,
    ///    the per-shot lines, the streaks and the reticle's hand-off line are gone: HudToolkit.Fire, HudToolkit.Less, HudToolkit.Lock); the flashes
    ///    drawn are counted.
    ///  · looks (Forma 067 W1, the Velvet Set): 3:10, the rival car parks and the three post round the cell's car (framed on the three at play
    ///    zoom), then the lookout posted on the far kerb (framed alone at play zoom: the crew stands over 40 m apart): every member on foot drawn in
    ///    his own look (HostViews.RivalFigure: rival-1 rival, rival-2 rival_b, rival-3 rival_d, rival-4 rival_c, rival-5 rival). The wheelman rides
    ///    aboard until the fight; the safe fight's frame has all five (a body wears its member's look).</summary>
    public static class Proof123
    {
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Shoot(HandoverHost h, string dir, string name, List<string> log, string state = "rest")
        {
            bool was = h.Paused; h.SetPaused(true);
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), state)); yield return null; while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: reticles [{HudToolkit.ReticlesShown}] zoom {F(h.View ? h.View.orthographicSize : -1f)} yaw {F(h.Yaw)}");
            h.SetPaused(was);
        }
        static IEnumerator Fresh(HandoverHost h) { HandoverHost.PointerWorld = null; HandoverHost.PointerOverride = null; h.PeekLatch = false; h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; yield return null; }
        static void To(HandoverHost h, int tick) { var g = h.Game; while (g.Tick < tick && !g.Over) h.RunTo(Math.Min(tick, g.Tick + 50)); }
        static string Root => Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir("")));
        static Vector3? ViewAt(HandoverHost h, string key) => h.Views.Find(key) is Transform t && t.gameObject.activeInHierarchy ? t.position : null;

        /// <summary>A script's orders in tick order, given as the batch's rows give them (the host's own Issue, the cast's verbs scripted).</summary>
        sealed class Feed
        {
            readonly List<Order> orders; int next;
            public Feed(List<Order> o) { orders = o; }
            public void To(HandoverHost h, int tick)
            {
                var g = h.Game;
                while (next < orders.Count && orders[next].Tick <= tick && !g.Over)
                {
                    var o = orders[next++]; Proof123.To(h, o.Tick); if (g.Over) break;
                    var c = new Order { Tick = g.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run };
                    if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c);
                }
                if (tick > g.Tick && !g.Over) Proof123.To(h, tick);
            }
        }
        /// <summary>Keel's fixture (sim/Suborned.Sim/Fixtures/&lt;name&gt;.script.json, composed on the engine it ships with).</summary>
        static Feed Fixture(string name)
        {
            var sc = Suborned.Sim.Script.Parse(File.ReadAllText(Path.Combine(Root, "sim", "Suborned.Sim", "Fixtures", name + ".script.json")));
            return new Feed(sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList());
        }

        /// <summary>The cell's own fight now: each agent on foot and what it fires at (its fire state's target, drawn).</summary>
        static List<Vector3> FightPoints(HandoverHost h)
        {
            var l = new List<Vector3>();
            for (int k = 0; k < 4; k++)
            {
                if (ViewAt(h, "op/" + Suborned.Sim.Op.Name((OpId)k)) is not Vector3 p) continue; l.Add(p);
                var t = h.FireNow[k].Target; if (h.FireNow[k].Firing && t.Length > 0 && h.DrawnAt(t) is Vector3 tw) l.Add(tw);
            }
            return l;
        }

        /// <summary>At yaw 30 on the points' middle, at play zoom (14; `wider`: up to 22 in steps of 2 m while a point stands outside the middle
        /// 88 %); the points in frame are logged.</summary>
        static IEnumerator Frame(HandoverHost h, List<Vector3> pts, List<string> log, string what, bool wider = false)
        {
            h.TurnSnap(0); if (pts.Count == 0) { log.Add($"r123 {what}: nothing to frame"); yield break; }
            var mid = Vector3.zero; foreach (var p in pts) mid += p; mid /= pts.Count;
            float z = 14f; int inFrame = 0;
            for (; z <= (wider ? 22f : 14f); z += 2f)
            {
                h.View.orthographicSize = z; h.LookAt(mid); yield return Frames(2);
                inFrame = 0; foreach (var p in pts) { var v = h.View.WorldToViewportPoint(p + Vector3.up * 1f); if (v.z > 0f && v.x >= 0.06f && v.x <= 0.94f && v.y >= 0.06f && v.y <= 0.94f) inFrame++; }
                if (inFrame == pts.Count) break;
            }
            log.Add($"r123 {what}: {inFrame} of {pts.Count} points in frame at zoom {F(h.View.orthographicSize)}");
        }

        /// <summary>Each shooter's drawn yaw against the bearing to the target it faces (HostViews.Aim), sampled every frame for `seconds` of play;
        /// a shooter counts once it has faced the same target 0.3 s (the turn). Returns the worst error, frames with any shooter over 25°, the
        /// shooters seen and the flashes drawn.</summary>
        static IEnumerator FacingHold(HandoverHost h, float seconds, Dictionary<string, string> result)
        {
            var since = new Dictionary<string, (Vector3 at, float t)>(); float worst = 0f; string worstKey = ""; int frames = 0, badFrames = 0; var shooters = new HashSet<string>();
            int flash0 = HudToolkit.ShotFlashes, hflash0 = HudToolkit.ShotFlashesHostile; float t0 = Time.realtimeSinceStartup, u0 = Time.unscaledTime; h.SetPaused(false);
            var shotSeen = new HashSet<(string side, float born)>(); // Glyph r123 G1: the shots logged in the window (ShotFx keeps the last 24), to tell no flash from no shot
            while (Time.realtimeSinceStartup - t0 < seconds && !h.Game.Over)
            {
                yield return null; frames++; float now = Time.realtimeSinceStartup; bool bad = false;
                foreach (var kv in h.Views.Aim)
                {
                    var key = kv.Key; var t = h.Views.Find(key); if (!t || !t.gameObject.activeInHierarchy || h.Views.FigureYaw(key) is not float yaw) continue;
                    if (!since.TryGetValue(key, out var s) || (s.at - kv.Value).sqrMagnitude > 4f) { since[key] = (kv.Value, now); continue; } // a new target (or one moved 2 m in a frame): its turn starts
                    since[key] = (kv.Value, s.t);
                    if (now - s.t < 0.3f) continue;
                    var d = kv.Value - t.position; d.y = 0f; if (d.sqrMagnitude < 0.04f) continue;
                    float err = Mathf.Abs(Mathf.DeltaAngle(yaw, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg)); shooters.Add(key);
                    if (err > worst) { worst = err; worstKey = key; }
                    if (err > 25f) bad = true;
                }
                foreach (var k in since.Keys.ToList()) if (!h.Views.Aim.ContainsKey(k)) since.Remove(k);
                foreach (var fx in h.ShotFx) if (fx.born >= u0) shotSeen.Add((fx.side, fx.born));
                if (bad) badFrames++;
            }
            h.SetPaused(true); yield return Frames(2);
            result["frames"] = frames.ToString(); result["badFrames"] = badFrames.ToString(); result["worst"] = F(worst); result["worstKey"] = worstKey;
            result["shooters"] = string.Join(",", shooters.OrderBy(x => x)); result["flashes"] = (HudToolkit.ShotFlashes - flash0).ToString();
            result["shots"] = shotSeen.Count.ToString(); result["hostileShots"] = shotSeen.Count(x => x.side != "cell").ToString(); result["hostileFlashes"] = (HudToolkit.ShotFlashesHostile - hflash0).ToString();
        }

        /// <summary>The crew's five as drawn now: id, its look (the body's when he is down), the look it should wear, its mode.</summary>
        static (string words, bool ok, int looks) Looks(HandoverHost h)
        {
            var w = new List<string>(); bool ok = true; var seen = new HashSet<string>();
            foreach (var id in HostViews.RivalKeys)
            {
                string want = HostViews.RivalFigure(id), key = ViewAt(h, id).HasValue ? id : ViewAt(h, "body/" + id).HasValue ? "body/" + id : "";
                string got = key.Length > 0 ? h.Views.FigureId(key) : "";
                string mode = h.Views.RivalModes.TryGetValue(id, out var m) ? m : "";
                if (got.Length > 0) { seen.Add(got); ok &= got == want; }
                w.Add($"{id}={(key.Length == 0 ? "not drawn" : got)}{(got.Length > 0 && got != want ? "(want " + want + ")" : "")}{(key.StartsWith("body/") ? " body" : "")} {mode}".Trim());
            }
            return (string.Join("; ", w), ok, seen.Count);
        }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            var keepConvo = h.controls.conversations; h.controls.conversations = ConversationScenes.Never;

            // ---- looks (Forma 067 W1): 3:10, the car parks across from Vale's and the three post round the cell's car (framed on the three at play
            //      zoom); 3:20, the lookout posted on the far kerb (the four on foot framed together, as wide as they need); the wheelman rides aboard
            //      (not drawn: logged). All five together in their four looks: the safe fight's frame below.
            {
                yield return Fresh(h); var g = h.Game; To(h, g.RivalAt + 10); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.35f); h.SetPaused(true); yield return Frames(3);
                var three = new List<Vector3>(); foreach (var id in new[] { "rival-1", "rival-2", "rival-3" }) if (ViewAt(h, id) is Vector3 p) three.Add(p);
                h.Select(OpId.Scout, false); yield return Frame(h, three, log, "looks at 3:10 (the three at the cell's car)");
                var (w310, ok310, n310) = Looks(h); string at310 = HandoverHost.MissionClock(g.Tick);
                yield return Shoot(h, dir, "r123-looks-310", log);
                while (g.Tick < g.RivalAt + 40 * Suborned.Sim.Game.Hz && !(h.Views.RivalModes.TryGetValue("rival-4", out var lm) && lm == "posted") && !g.Over) h.RunTo(g.Tick + 5);
                h.SetPaused(false); yield return new WaitForSecondsRealtime(0.35f); h.SetPaused(true); yield return Frames(3);
                var look4 = new List<Vector3>(); if (ViewAt(h, "rival-4") is Vector3 l4) look4.Add(l4); // the crew stands over 40 m apart now (three at the cell's car, the lookout on the far kerb): the lookout at play zoom
                yield return Frame(h, look4, log, "looks with the lookout posted (rival-4)");
                var (w320, ok320, n320) = Looks(h); string at320 = HandoverHost.MissionClock(g.Tick); bool drawn4 = new[] { "rival-1", "rival-2", "rival-3", "rival-4" }.All(id => ViewAt(h, id).HasValue);
                yield return Shoot(h, dir, "r123-looks-320", log);
                bool looksOk = ok310 && ok320 && n310 >= 3 && drawn4 && n320 >= 4; ok &= looksOk;
                parts.Add($"\"looks\": {{\"ok\": {B(looksOk)}, \"at310\": \"{at310}\", \"distinct310\": {n310}, \"members310\": \"{J(w310)}\", \"at320\": \"{at320}\", \"onFoot4\": {B(drawn4)}, \"distinct320\": {n320}, \"members320\": \"{J(w320)}\"}}");
                log.Add($"r123 looks: {(looksOk ? "ok" : "FAILED")}: at {at310} {n310} looks drawn: {w310}; at {at320} the first four on foot {drawn4}, {n320} looks: {w320}");
            }

            // ---- fight, safe at the square (sq-ra-h3, 4:15) and from the ground view; loud (bal-loud-x4, 3:46.6)
            var fights = new List<string>(); bool fOk = true;
            foreach (var (name, fixture, tick, shot) in new[] { ("safe", "sq-ra-h3", 2550, "r123-fight-safe"), ("loud", "bal-loud-x4", 2266, "r123-fight-loud") })
            {
                yield return Fresh(h); var g = h.Game; Feed feed = null; string err = "";
                try { feed = Fixture(fixture); } catch (Exception e) { err = e.Message; }
                if (feed == null) { fights.Add($"\"{name}\": {{\"ok\": false, \"error\": \"{J(err)}\"}}"); log.Add($"r123 fight {name}: FAILED: {fixture} would not read ({err})"); fOk = false; continue; }
                feed.To(h, tick - 4); h.Select(OpId.Scout, false);
                yield return Frame(h, FightPoints(h), log, $"fight {name}");
                h.SetPaused(false); yield return new WaitForSecondsRealtime(0.4f); h.SetPaused(true); yield return Frames(2); // the shooters turned (0.15 s) and the marks settled
                yield return Frame(h, FightPoints(h), log, $"fight {name} for the picture");
                string branch = g.FightBranch; var (words, _, n) = Looks(h); string fans = h.Views.FanStrengths;
                yield return Shoot(h, dir, shot, log);
                log.Add($"r123 fight {name} picture: cones drawn [{fans}]");
                var fr = new Dictionary<string, string>(); yield return FacingHold(h, 1.5f, fr); // then the clock runs 1.5 s, every shooter's facing sampled each frame
                string ground = "";
                if (name == "safe" && !g.Over && !g.Ops[(int)OpId.Scout].Down && !g.Ops[(int)OpId.Scout].InCar)
                {   // the ground view: the Scout's third person, the clock running 1.2 s, the same measure
                    h.Select(OpId.Scout, false); h.DriveToggle(); yield return new WaitForSecondsRealtime(0.6f);
                    var gr = new Dictionary<string, string>(); yield return FacingHold(h, 2.5f, gr); // Glyph r123 G1: 2.5 s (1.2 s could pass with no hostile shot)
                    yield return Shoot(h, dir, "r123-fight-ground", log);
                    bool gOk = h.Following && gr["badFrames"] == "0" && (gr["hostileShots"] == "0" || gr["hostileFlashes"] != "0"); fOk &= gOk; // G1: a hostile's shot in the ground view shows its flash
                    ground = $", \"ground\": {{\"ok\": {B(gOk)}, \"following\": {B(h.Following)}, \"frames\": {gr["frames"]}, \"badFrames\": {gr["badFrames"]}, \"worstDeg\": {gr["worst"]}, \"worstKey\": \"{gr["worstKey"]}\", \"shooters\": \"{gr["shooters"]}\", \"flashes\": {gr["flashes"]}, \"shots\": {gr["shots"]}, \"hostileShots\": {gr["hostileShots"]}, \"hostileFlashes\": {gr["hostileFlashes"]}}}";
                    log.Add($"r123 fight ground: {(gOk ? "ok" : "FAILED")}: following {h.Following}; {gr["frames"]} frames, {gr["badFrames"]} with a shooter over 25 deg, worst {gr["worst"]} deg ({gr["worstKey"]}); shooters [{gr["shooters"]}]; flashes {gr["flashes"]} ({gr["hostileFlashes"]} hostile) for {gr["shots"]} shots ({gr["hostileShots"]} hostile)");
                    h.DriveLetGoToMap(); yield return new WaitForSecondsRealtime(0.4f);
                }
                bool on = branch.Length > 0 && g.FightStart < int.MaxValue / 8, oneOk = on && fr["badFrames"] == "0" && fr["shooters"].Length > 0; fOk &= oneOk;
                fights.Add($"\"{name}\": {{\"ok\": {B(oneOk)}, \"fixture\": \"{fixture}\", \"at\": \"{HandoverHost.MissionClock(g.Tick)}\", \"branch\": \"{branch}\", \"frames\": {fr["frames"]}, \"badFrames\": {fr["badFrames"]}, \"worstDeg\": {fr["worst"]}, \"worstKey\": \"{fr["worstKey"]}\", \"shooters\": \"{fr["shooters"]}\", \"flashes\": {fr["flashes"]}, \"shots\": {fr["shots"]}, \"hostileShots\": {fr["hostileShots"]}, \"hostileFlashes\": {fr["hostileFlashes"]}, \"reticles\": \"{J(HudToolkit.ReticlesShown)}\", \"cones\": \"{J(fans)}\", \"looks\": \"{J(words)}\", \"distinctLooks\": {n}{ground}}}");
                log.Add($"r123 fight {name}: {(oneOk ? "ok" : "FAILED")}: {fixture} at {HandoverHost.MissionClock(g.Tick)}, branch {(branch.Length > 0 ? branch : "none")}; {fr["frames"]} frames, {fr["badFrames"]} with a shooter over 25 deg, worst {fr["worst"]} deg ({fr["worstKey"]}); shooters [{fr["shooters"]}]; flashes {fr["flashes"]}; reticles [{HudToolkit.ReticlesShown}]; crew {words}");
            }
            ok &= fOk;
            parts.Add($"\"fight\": {{\"ok\": {B(fOk)}, {string.Join(", ", fights)}}}");

            HandoverHost.PointerWorld = null; HandoverHost.PointerOverride = null; h.controls.conversations = keepConvo;
            h.SetPaused(false);
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }
    }

    /// <summary>`-proof123 &lt;dir&gt;`: Proof123 alone (the host round's pictures and checks, a few minutes), proof123.json and its log in the folder, then quit.</summary>
    public sealed class Proof123Run : MonoBehaviour
    {
        public string Dir = "";
        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir); var log = new List<string>();
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { File.WriteAllText(Path.Combine(Dir, "proof123.json"), "{\"status\": \"no host after 60 s\"}\n"); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f); // the HUD built, the world streamed in
            var json = new[] { "null" }; string status = "ok";
            var run = Proof123.Run(h, Dir, log, json);
            while (true)
            {
                object cur;
                try { if (!run.MoveNext()) break; cur = run.Current; }
                catch (Exception e) { status = "threw: " + e.Message.Replace("\"", "'"); log.Add("r123 threw: " + e); break; }
                yield return cur;
            }
            File.WriteAllText(Path.Combine(Dir, "proof123.log"), string.Join("\n", log) + "\n");
            File.WriteAllText(Path.Combine(Dir, "proof123.json"), $"{{\n \"schema\": \"suborned.proof123/1\",\n \"status\": \"{status}\",\n \"screen\": \"{Screen.width}x{Screen.height}\",\n \"r123\": {json[0]}\n}}\n");
            yield return new WaitForSecondsRealtime(0.3f);
            Application.Quit(status == "ok" ? 0 : 3);
        }
    }
}
