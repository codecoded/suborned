using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;
using ValeAt = Suborned.Sim.ValeAt;

namespace Suborned.Play
{
    /// <summary>Batch 0.2.118's proof (briefs/041/next-batch.md items A, C, E, F), in the built player, one JSON object `r118`:
    ///  · cam058 (engine 0.31.0, Glyph 058; gated): the Tech at the cabinet, the ring on the entrance camera reads Loop · Disable · Fire; the
    ///    Scout's reads Fire alone; the loop lands and its window marker counts m:ss from about 2:00 (no Tech face); a Loop on the pickup camera
    ///    while it runs is shown shut (loop-holds); Disable on the pickup camera lands "Camera cut" with the desk's clock, then the dead disc stays
    ///    after the report; a camera shot is priced "+2", never "/hit" (FireTrail). Pictures r118-ring-camera-tech (Glyph's 2x crop: run at 2160),
    ///    -ring-camera-scout, -loop-marker, -loop-holds, -camera-cut, -dead-disc.
    ///  · Glint's shot list (pictures and measurements, not gated; the values go back to Glint): H1-H5 the crossing heads nearest the Records
    ///    Office (SignalHead.Hold), R1-R5 the capsule rim, the uncut shadow, the silhouettes and the reach (each frame logs the agents' screen
    ///    points so the fill can be measured), V1-V3 Vale C on the landing, carried and at eye level.
    ///  · 0.2.123: the camera ring at rest (Tech, Scout, Enforcer: -rest), the camera states Glyph had not seen (r118-alt-camera-panel,
    ///    -loop-cone, -camera-on, -dead-disc-shot), H5 re-framed on three near heads, Glint's edge trial (r118-v1-edge-55/-70/-85) and the F1
    ///    test: every shot logs the silhouette people with no cut and the fade points against the cap (`uncut`, gated: none on any frame), and
    ///    the reach at yaw 210 (`reach`, gated: at most ReachedMax).
    /// Nothing here changes a rule; every order is the host's own. The light is put back to day at the end.</summary>
    public static class Proof118
    {
        /// <summary>Brief 078 (Suture's section ranking; Sky: delta only): r118 keeps its asserts (cam058, heads, uncut, reach). The pictures with no pass or fail (the camera states, the head pictures past one yaw by day, evening and night, Vale, the driver, the four agents, the footway) run only under `-proofPictures`, for Glint's or Glyph's review.</summary>
        static readonly bool Archive = GameBoot.Arg("-proofPictures");
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static Vector2 Sp(HandoverHost h, Vector3 w) { var s = h.View.WorldToScreenPoint(w); return new Vector2(s.x, s.y); }
        static IEnumerator Shoot(HandoverHost h, string dir, string name, List<string> log, string state = "rest")
        {
            bool was = h.Paused; h.SetPaused(true);
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), state)); yield return null; while (HudToolkit.ShotBusy) yield return null;
            log.Add(Uncut(h, name));
            h.SetPaused(was);
        }
        /// <summary>Glint 056 (2026-10-26): every shot logs the silhouette people with no cut (pass: none) and the fade points asked for against the cap;
        /// at or past the cap logs "overflow" (one in the service yard means MaxPoints goes to 14, not a failure of the rule).</summary>
        static int uncutFrames, withUncut, maxPoints, overflows;
        internal static string Uncut(HandoverHost h, string name, bool count = true)
        {
            string u = h.UncutSilhouettePeople(); int n = h.FadePointCount; bool over = n >= ArchitectureFade.MaxPoints;
            if (count) { uncutFrames++; if (u.Length > 0) withUncut++; maxPoints = Math.Max(maxPoints, n); if (over) overflows++; }
            return $"{name}: uncut [{u}] points {n}/{ArchitectureFade.MaxPoints}{(over ? " overflow" : "")}";
        }
        /// <summary>Glint 056 R5 / F1 perf: renderers on the discarding variant; a frame past this is rejected.</summary>
        public const int ReachedMax = 140;
        /// <summary>Aster 0.2.123 item 3 (Glyph's 0.2.122 question): the open ring at rest, the pointer held in its dead zone (no wedge hovered, only the
        /// lit one lit): its wedges logged clockwise from 12 o'clock.</summary>
        static IEnumerator RestRing(HandoverHost h, string dir, string name, List<string> log)
        {
            if (h.Ring == null) { log.Add($"{name}: no ring open"); yield break; }
            var keep = HandoverHost.PointerOverride; HandoverHost.PointerOverride = h.Ring.Screen; yield return Frames(4); yield return new WaitForSecondsRealtime(0.3f);
            log.Add($"{name}: ring at rest [{RingWords(h)}] hover {h.Ring?.Hover ?? -2} lit {h.Ring?.Lit ?? -2}");
            yield return Shoot(h, dir, name, log, "active"); HandoverHost.PointerOverride = keep;
        }
        static string RingWords(HandoverHost h) => h.Ring == null ? "" : string.Join(" | ", h.Ring.Segs.ConvertAll(x => x.More ? "⋯" : (x.Shut ? "~" : "") + x.Word));
        static List<Order> Route(string name)
        {
            var path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), "contracts", "missions", "handover", "routes", name + ".json");
            var sc = Suborned.Sim.Script.Parse(File.ReadAllText(path));
            return sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
        }
        static void Play(HandoverHost h, List<Order> orders, int until, int runTo)
        {
            foreach (var o in orders)
            {
                if (o.Tick >= until || h.Game.Over) break;
                if (o.Tick > h.Game.Tick) h.RunTo(o.Tick);
                var c = new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run };
                if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c);
            }
            if (runTo > h.Game.Tick && !h.Game.Over) h.RunTo(runTo);
        }
        static IEnumerator Fresh(HandoverHost h) { h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; yield return null; }

        /// <summary>The light phase for a shot: day, evening (held at full Dusk) or night; waits for the ease.</summary>
        static IEnumerator Phase(HandoverHost h, string phase)
        {
            if (phase == "night") h.SetNight(true);
            else { h.SetNight(false); if (phase == "evening" && LightPhase.Instance) LightPhase.Instance.SetEvening(); }
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 6f) { var lp = LightPhase.Instance; if (!lp || (phase == "evening" ? lp.Dusk >= 0.99f : phase == "day" ? lp.Dusk <= 0.01f : true) && Time.realtimeSinceStartup - t0 > 1.5f) break; yield return null; }
        }

        /// <summary>The agents' and key people's screen points now (for Glint's pixel measures: R4 fill, V1 jacket).</summary>
        static string Points(HandoverHost h, params string[] keys)
        {
            var l = new List<string>();
            foreach (var k in keys) { var t = h.Views.Find(k); if (!t || !t.gameObject.activeInHierarchy) continue; var s = Sp(h, t.position + Vector3.up * 1.0f); l.Add($"{k}@{s.x:0},{Screen.height - s.y:0}"); }
            return string.Join(" ", l);
        }

        /// <summary>A renderer's screen rect "x0,y0,x1,y1" (top-left origin, pixels of the current window), from its world bounds' eight corners.</summary>
        static string Rect(HandoverHost h, Renderer r)
        {
            if (!r || !h.View) return "none";
            var b = r.bounds; float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < 8; i++) { var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)); var s = h.View.WorldToScreenPoint(c); x0 = Mathf.Min(x0, s.x); x1 = Mathf.Max(x1, s.x); y0 = Mathf.Min(y0, Screen.height - s.y); y1 = Mathf.Max(y1, Screen.height - s.y); }
            return $"{x0:0},{y0:0},{x1:0},{y1:0}";
        }
        /// <summary>A figure's drawn height on screen in pixels (its bones), -1 when none; at 1080p a person reads 40-60 px.</summary>
        static int PxTall(HandoverHost h, Transform t)
        {
            if (!t || !h.View) return -1; var rs = t.GetComponentsInChildren<SkinnedMeshRenderer>(); if (rs.Length == 0) return -1;
            /* the bones, not the skinned bounds (those are loose: the driver read 114 px); each bone also 0.12 m up, for the crown */
            float lo = float.MaxValue, hi = float.MinValue;
            var pts = rs.SelectMany(r => r.bones).Where(b => b).ToList();
            if (pts.Count == 0) { pts.Add(t); foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name is "Head" or "Hips") pts.Add(c); } // brief 069 A: an optimised figure has no bone list: its feet (the root) and the exposed Head
            foreach (var bn in pts) { foreach (var c in new[] { bn.position, bn.position + Vector3.up * 0.12f }) { var s = h.View.WorldToScreenPoint(c); lo = Mathf.Min(lo, s.y); hi = Mathf.Max(hi, s.y); } }
            if (lo > hi) return -1;
            return Mathf.RoundToInt((hi - lo) * 1080f / Mathf.Max(1, Screen.height));
        }
        static Vector3 Centre(HandoverHost h)
        {
            var c = Vector3.zero; int n = 0;
            foreach (var k in new[] { "op/scout", "op/fixer", "op/tech", "op/enforcer" }) { var t = h.Views.Find(k); if (t && t.gameObject.activeInHierarchy) { c += t.position; n++; } }
            return n > 0 ? c / n : h.frame.World(HandoverHost.OpeningFocus);
        }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true; bool keepPlan = h.controls.planTogether; h.controls.planTogether = false;
            uncutFrames = withUncut = maxPoints = overflows = 0; int reachMax = -1; var reachRows = new List<string>();
            HandoverHost.PointerOverride = null; HandoverHost.PointerWorld = null;

            // ---- cam058 (engine 0.31.0): the ring, the loop's 2:00, one loop at a time, Disable and the desk's clock, the dead disc, the price
            {
                yield return Fresh(h); var g = h.Game;
                h.Issue(new Order { Op = "tech", Do = "move", To = "cabinet" });
                for (int i = 0; i < 90 * Suborned.Sim.Game.Hz && !Suborned.Sim.G.Within(g.Ops[(int)OpId.Tech].Pos, g.S.At("cabinet"), g.R.AtReachCm) && !g.Over; i += 5) h.RunTo(g.Tick + 5);
                bool atCab = Suborned.Sim.G.Within(g.Ops[(int)OpId.Tech].Pos, g.S.At("cabinet"), g.R.AtReachCm);
                string ringTech = "", ringScout = "", holds = "", marker = "", cut = "", desk = "", after = ""; bool shutLoop = false; int deadDiscs0 = HudToolkit.DeadDiscsShown;
                var ec = h.Views.Find("camera/entrance"); var pc = h.Views.Find("camera/pickup");
                if (ec)
                {
                    h.Select(OpId.Tech, false); h.LookAt(ec.position); yield return Frames(3);
                    h.LeftClickScripted(Sp(h, ec.position)); yield return Frames(4); ringTech = RingWords(h); yield return new WaitForSecondsRealtime(0.3f); yield return Shoot(h, dir, "r118-ring-camera-tech", log, "active"); yield return RestRing(h, dir, "r118-ring-camera-tech-rest", log); h.CloseRing();
                    h.Select(OpId.Scout, false); yield return Frames(2);
                    h.LeftClickScripted(Sp(h, ec.position)); yield return Frames(4); ringScout = RingWords(h); yield return new WaitForSecondsRealtime(0.3f); yield return Shoot(h, dir, "r118-ring-camera-scout", log, "active"); yield return RestRing(h, dir, "r118-ring-camera-scout-rest", log); h.CloseRing();
                    // Aster 0.2.123 item 3: one more armed agent's ring on the camera, at rest (Fire alone, or Watch and Fire)
                    h.Select(OpId.Enforcer, false); yield return Frames(2);
                    h.LeftClickScripted(Sp(h, ec.position)); yield return Frames(4); yield return RestRing(h, dir, "r118-ring-camera-enforcer-rest", log); h.CloseRing();
                    h.Select(OpId.Tech, false); yield return Frames(2);
                    h.Issue(new Order { Op = "tech", Do = "loop", Target = "entrance" });
                    for (int i = 0; i < 40 * Suborned.Sim.Game.Hz && !g.LoopOn && !g.Over; i++) h.RunTo(g.Tick + 1);
                    h.RunTo(g.Tick + 2); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.4f); h.SetPaused(true); yield return Frames(2);
                    marker = HudToolkit.WindowWords; yield return Shoot(h, dir, "r118-loop-marker", log, "active");
                }
                if (pc && g.LoopOn)
                {
                    h.Select(OpId.Tech, false); h.LookAt(pc.position); yield return Frames(3);
                    h.LeftClickScripted(Sp(h, pc.position)); yield return Frames(4); holds = RingWords(h);
                    shutLoop = h.Ring != null && h.Ring.Segs.Exists(x => x.Shut && x.Row?.Lock.code == "loop-holds"); yield return new WaitForSecondsRealtime(0.3f); yield return Shoot(h, dir, "r118-loop-holds", log, "active"); h.CloseRing();
                    h.Issue(new Order { Op = "tech", Do = "disable", Target = "pickup" });
                    for (int i = 0; i < 30 * Suborned.Sim.Game.Hz && g.CameraDeadAs("pickup") != "cut" && !g.Over; i++) h.RunTo(g.Tick + 1);
                    h.RunTo(g.Tick + 2); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.4f); h.SetPaused(true); yield return Frames(2);
                    cut = h.LastChip != null ? string.Join(",", h.LastChip.Lines.ConvertAll(x => x.Word)) : ""; desk = HudToolkit.WindowWords;
                    yield return Shoot(h, dir, "r118-camera-cut", log, "active");
                    for (int i = 0; i < 40 * Suborned.Sim.Game.Hz && g.CutReps.Exists(x => !x.Done) && !g.Over; i++) h.RunTo(g.Tick + 1);
                    h.RunTo(g.Tick + 2); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.3f); h.SetPaused(true); yield return Frames(3);
                    after = HudToolkit.WindowWords; yield return Shoot(h, dir, "r118-dead-disc", log, "active");
                }
                string price = h.FireTrail(OpId.Scout, "camera:entrance");
                bool loopMarker = marker.Contains("loop:entrance:1:5") || marker.Contains("loop:entrance:2:00");
                bool cOk = atCab && ringTech.Contains("Loop") && ringTech.Contains("Disable") && ringTech.Contains("Fire") && !ringTech.Contains("⋯")
                    && ringScout.Contains("Fire") && !ringScout.Contains("Loop") && !ringScout.Contains("Disable") && loopMarker && shutLoop
                    && g.CameraDeadAs("pickup") == "cut" && cut.Contains("Camera cut") && desk.Contains("disable:pickup:") && !after.Contains("disable:pickup:") && HudToolkit.DeadDiscsShown > deadDiscs0 && price == "+2";
                ok &= cOk;
                parts.Add($"\"cam058\": {{\"ok\": {B(cOk)}, \"atCabinet\": {B(atCab)}, \"ringTech\": \"{J(ringTech)}\", \"ringScout\": \"{J(ringScout)}\", \"loopMarker\": \"{J(marker)}\", \"loopHolds\": \"{J(holds)}\", \"shutLoop\": {B(shutLoop)}, \"cutChip\": \"{J(cut)}\", \"desk\": \"{J(desk)}\", \"afterReport\": \"{J(after)}\", \"deadDiscs\": {HudToolkit.DeadDiscsShown - deadDiscs0}, \"alert\": {g.Alert}, \"firePrice\": \"{price}\"}}");
                log.Add($"r118 cam058: {(cOk ? "ok" : "FAILED")}: cabinet {atCab}; Tech ring [{ringTech}]; Scout ring [{ringScout}]; marker [{marker}]; second loop [{holds}] shut {shutLoop}; cut chip [{cut}] desk [{desk}] after [{after}] discs {HudToolkit.DeadDiscsShown - deadDiscs0}; Alert {g.Alert}; fire price {price}");
            }

            // ---- Glyph's unverified 0.2.122 states (Aster 0.2.123 item 4): the looped camera's hollow dashed cone (the Tech watches the entrance camera
            // first, so its cone is learned and drawn), the Alt breakdowns for Loop and Disable (the camera's panel with Alt), the "Camera on" pop at the
            // loop's 2:00 end, then the shot-out dead disc (disc_light_24, the ink fire icon). Pictures, not gated; the words logged.
            if (Archive)
            {
                yield return Fresh(h); var g = h.Game; var ec = h.Views.Find("camera/entrance");
                bool learned = false, looped = false, on = false, shot = false; string fans = "", alt = "", pop = "", shotCam = "";
                h.GiveWatchScripted(OpId.Tech, "camera:entrance");
                for (int i = 0; i < 900 && !(learned = g.Known("camera:entrance") is { Sight: true }) && !g.Over; i++) h.RunTo(g.Tick + 1);
                h.Issue(new Order { Op = "tech", Do = "move", To = "cabinet" });
                for (int i = 0; i < 90 * Suborned.Sim.Game.Hz && !Suborned.Sim.G.Within(g.Ops[(int)OpId.Tech].Pos, g.S.At("cabinet"), g.R.AtReachCm) && !g.Over; i += 5) h.RunTo(g.Tick + 5);
                if (ec && learned)
                {
                    // the Alt breakdowns: the entrance camera's panel with the Tech selected (Loop, Disable, Fire rows), Alt latched
                    h.Select(OpId.Tech, false); h.LookAt(ec.position); yield return Frames(3);
                    h.OpenPanel(new HandoverHost.Target { Kind = "camera", Key = "entrance", Label = h.CameraName("entrance") }, Sp(h, ec.position)); h.PeekLatch = true;
                    yield return Frames(6); yield return new WaitForSecondsRealtime(0.4f);
                    alt = h.Panel != null ? string.Join(" | ", h.Panel.Rows.ConvertAll(r => (r.Available ? "" : "~") + (r.I?.Order?.Do ?? "?"))) : "no panel";
                    yield return Shoot(h, dir, "r118-alt-camera-panel", log, "active"); h.PeekLatch = false; h.ClosePanel(); yield return Frames(2);
                    // the loop lands: the learned cone draws recorded (hollow, dashed all round)
                    h.Issue(new Order { Op = "tech", Do = "loop", Target = "entrance" });
                    for (int i = 0; i < 40 * Suborned.Sim.Game.Hz && !g.LoopOn && !g.Over; i++) h.RunTo(g.Tick + 1);
                    h.RunTo(g.Tick + 2); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.4f); h.SetPaused(true); h.LookAt(ec.position); yield return Frames(4);
                    fans = h.Views.LoopedFans; looped = g.LoopOn && fans.Contains("camera:entrance");
                    yield return Shoot(h, dir, "r118-loop-cone", log);
                    // the loop's end (2:00 after it landed): the "Camera on" pop at the camera
                    int pops0 = h.WindowClosePops;
                    for (int i = 0; i < 150 * Suborned.Sim.Game.Hz && g.LoopOn && !g.Over; i += 5) h.RunTo(g.Tick + 5);
                    h.RunTo(g.Tick + 1); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.35f); h.SetPaused(true); h.LookAt(ec.position); yield return Frames(3);
                    pop = h.LastClosePop; on = !g.LoopOn && h.WindowClosePops > pops0 && pop.Length > 0;
                    yield return Shoot(h, dir, "r118-camera-on", log, "active");
                }
                // the shot-out disc: the Scout walks to a spot in pistol range with a clear line to a camera, then fires on it
                {
                    var sc = g.Ops[(int)OpId.Scout]; int reach = Math.Max(300, sc.Gun.W.MaxCm - 200);
                    foreach (var cm in g.S.Cameras)
                    {
                        if (g.CameraDeadAs(cm.Id).Length > 0) continue; var cAt = cm.At;
                        var spot = g.S.Grid.Nearest(sc.Pos, q => Suborned.Sim.G.D2(q, cAt) <= (long)reach * reach && g.S.Sight(q, cAt, g.R.Eye), 9000);
                        if (!spot.HasValue) continue; shotCam = cm.Id;
                        h.Issue(new Order { Op = "scout", Do = "move", Point = spot.Value });
                        for (int i = 0; i < 120 * Suborned.Sim.Game.Hz && sc.Walk != null && !g.Over; i += 5) h.RunTo(g.Tick + 5);
                        h.Issue(new Order { Op = "scout", Do = "fire", Target = "camera:" + cm.Id }, (Vector2?)null, (Vector3?)null);
                        for (int i = 0; i < 20 * Suborned.Sim.Game.Hz && g.CameraDeadAs(cm.Id) != "shot" && !g.Over; i++) h.RunTo(g.Tick + 1);
                        shot = g.CameraDeadAs(cm.Id) == "shot"; break;
                    }
                    var ct = shotCam.Length > 0 ? h.Views.Find("camera/" + shotCam) : null;
                    if (ct) { h.SetPaused(false); yield return new WaitForSecondsRealtime(0.4f); h.SetPaused(true); h.LookAt(ct.position); yield return Frames(4); yield return Shoot(h, dir, "r118-dead-disc-shot", log, "active"); }
                }
                parts.Add($"\"camStates\": {{\"learned\": {B(learned)}, \"looped\": {B(looped)}, \"loopedFans\": \"{J(fans)}\", \"altPanel\": \"{J(alt)}\", \"cameraOn\": {B(on)}, \"pop\": \"{J(pop)}\", \"shotCamera\": \"{J(shotCam)}\", \"shot\": {B(shot)}, \"alert\": {g.Alert}}}");
                log.Add($"r118 camera states: learned {learned}; looped cone {looped} [{fans}]; Alt panel [{alt}]; Camera on {on} ('{pop}'); shot out {shot} ({shotCam}); Alert {g.Alert}");
            }

            // ---- H1-H5 (Glint, brief 061): the crossing heads nearest the Records Office, held in each state (pictures; measured by Glint)
            {
                yield return Fresh(h);
                var office = h.Views.Find("camera/entrance"); var at = office ? office.position : h.frame.World(HandoverHost.OpeningFocus);
                var heads = SignalHead.All.Where(x => x && x.gameObject.activeInHierarchy).OrderBy(x => (x.transform.position - at).sqrMagnitude).ToList();
                string note;
                if (heads.Count == 0) { note = "no SignalHead in this world (the export or the kit adoption predates brief 061)"; parts.Add($"\"heads\": {{\"ok\": null, \"note\": \"{note}\"}}"); log.Add("r118 heads: " + note); }
                else
                {
                    var head = heads[0]; var crossing = head.Crossing; var shots = new List<string>();
                    string[] states = { "walk", "clearing", "stop", "arriving" };
                    yield return Phase(h, "day");
                    for (int y = 0; y < (Archive ? 4 : 1); y++) { h.TurnSnap(y); h.LookAt(head.transform.position); foreach (var st in states) { SignalHead.Hold = st; SignalHead.HoldLeft = st == "arriving" ? 2f : 7f; yield return Frames(2); yield return new WaitForSecondsRealtime(SignalHead.CrossfadeS + 0.15f); /* past the crossfade: 0.2.120's frames caught it mid-fade */ var n = $"r118-h1-{st}-yaw{Mathf.RoundToInt(h.Yaw)}"; yield return Shoot(h, dir, n, log); shots.Add(n); log.Add($"{n}: h1 figure {Rect(h, head.Figure)} face {Rect(h, head.Rim)}"); } }
                    foreach (var ph in Archive ? new[] { "evening", "night" } : new string[0])
                    {
                        yield return Phase(h, ph);
                        foreach (var y in new[] { 0, 2 }) { h.TurnSnap(y); h.LookAt(head.transform.position); foreach (var st in states) { SignalHead.Hold = st; yield return Frames(2); yield return new WaitForSecondsRealtime(SignalHead.CrossfadeS + 0.15f); var n = $"r118-h2-{ph}-{st}-yaw{Mathf.RoundToInt(h.Yaw)}"; yield return Shoot(h, dir, n, log); shots.Add(n); } }
                    }
                    // H3: eye level on the head, walk, clearing, stop, by day and at night
                    foreach (var ph in Archive ? new[] { "day", "night" } : new string[0])
                    {
                        yield return Phase(h, ph);
                        var hp = head.transform.position; var fwd = head.transform.forward; fwd.y = 0f; if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward; fwd.Normalize();
                        foreach (var st in new[] { "walk", "clearing", "stop" }) { SignalHead.Hold = st; yield return Frames(2); yield return new WaitForSecondsRealtime(SignalHead.CrossfadeS + 0.15f); var n = $"r118-h3-{ph}-{st}"; yield return Pose(h, dir, n, hp + fwd * 6f + Vector3.up * 1.7f, hp + Vector3.up * 2.4f, 45f, log); shots.Add(n); }
                    }
                    // H4: the clearing head and the guard's cone in one frame at play zoom, day and night (composed: the guard stands where he stands)
                    foreach (var ph in Archive ? new[] { "day", "night" } : new string[0])
                    {
                        yield return Phase(h, ph); SignalHead.Hold = "clearing"; yield return new WaitForSecondsRealtime(SignalHead.CrossfadeS + 0.15f); var gd = h.Views.Find("guard");
                        h.TurnSnap(2); h.LookAt(gd ? (gd.position + head.transform.position) * 0.5f : head.transform.position); yield return Frames(4);
                        var n = $"r118-h4-{ph}"; yield return Shoot(h, dir, n, log); shots.Add(n);
                        log.Add($"{n}: head {Points(h)} guard {(gd ? (gd.position - head.transform.position).magnitude.ToString("0.0", CultureInfo.InvariantCulture) : "?")} m from the head");
                    }
                    // H5: three states in one frame at night (the three nearest heads of other crossings, each held on its own)
                    if (Archive)
                    {
                        yield return Phase(h, "night"); SignalHead.Hold = "";
                        // 0.2.122's frame showed no head (Glint): the three were the nearest of three crossings to the Office, too far apart for one frame, and
                        // their centre fell on a roof. Now: the nearest head, then the heads of two other crossings nearest to it, at the yaw that keeps all three
                        // inside the frame's middle 80 %
                        var three = new List<SignalHead> { head };
                        foreach (var x in heads.OrderBy(x => (x.transform.position - head.transform.position).sqrMagnitude)) { if (three.Count == 3) break; if (three.Exists(y => y.Crossing == x.Crossing)) continue; three.Add(x); }
                        string[] st3 = { "walk", "clearing", "stop" };
                        for (int i = 0; i < three.Count; i++) three[i].HoldHere = st3[i];
                        var c3 = Vector3.zero; foreach (var x in three) c3 += x.transform.position; c3 /= Mathf.Max(1, three.Count);
                        int bestYaw = 0, bestIn = -1;
                        for (int y = 0; y < 4; y++)
                        {
                            h.TurnSnap(y); h.LookAt(c3); yield return Frames(2); int inside = 0;
                            foreach (var x in three) { var vp = h.View.WorldToViewportPoint(x.transform.position + Vector3.up * 2.4f); if (vp.z > 0f && vp.x > 0.1f && vp.x < 0.9f && vp.y > 0.1f && vp.y < 0.9f) inside++; }
                            if (inside > bestIn) { bestIn = inside; bestYaw = y; }
                        }
                        h.TurnSnap(bestYaw); h.LookAt(c3); yield return Frames(2); yield return new WaitForSecondsRealtime(SignalHead.CrossfadeS + 0.15f); yield return Shoot(h, dir, "r118-h5-night", log); shots.Add("r118-h5-night");
                        log.Add($"r118-h5-night: {three.Count} heads ({string.Join(", ", three.Select(x => x.Crossing))}), {bestIn} inside the middle 80 % at yaw {Mathf.RoundToInt(h.Yaw)}; spread {F(three.Max(a => three.Max(b => (a.transform.position - b.transform.position).magnitude)))} m");
                        foreach (var x in three) x.HoldHere = "";
                    }
                    SignalHead.Hold = ""; yield return Phase(h, "day"); h.TurnSnap(0);
                    parts.Add($"\"heads\": {{\"ok\": true, \"crossing\": \"{J(crossing)}\", \"heads\": {heads.Count}, \"cycled\": {heads.Count(x => x.HasCycle)}, \"rim\": {B(head.Rim)}, \"ring\": {B(head.Ring)}, \"dots\": {head.Dots.Length}, \"shots\": {shots.Count}}}");
                    log.Add($"r118 heads: {heads.Count} heads ({heads.Count(x => x.HasCycle)} on a cycle); nearest {crossing}: rim {(bool)head.Rim}, ring {(bool)head.Ring}, dots {head.Dots.Length}; {shots.Count} shots");
                }
            }

            // ---- R1-R5 (Glint, brief 056 F): the rim in a shaded wall, the uncut shadow, the silhouettes, the reach; the Enforcer beside the Office
            // door on route A (2:10) at yaw 210 (played) and 30, day, evening and night; each frame logs the people's screen points and the reach
            {
                yield return Fresh(h); var g = h.Game; Play(h, Route("route-a"), 130 * Suborned.Sim.Game.Hz, 130 * Suborned.Sim.Game.Hz);
                var fade = h.View ? h.View.GetComponent<ArchitectureFade>() : null; var rows = new List<string>();
                foreach (var ph in Archive ? new[] { "day", "evening", "night" } : new[] { "day" }) // the reach's gate needs one frame at yaw 210
                {
                    yield return Phase(h, ph);
                    foreach (var y in Archive ? new[] { 2, 0 } : new[] { 2 })
                    {
                        h.TurnSnap(y); var en = h.Views.Find("op/enforcer"); if (en) h.LookAt(en.position); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.35f); h.SetPaused(true); yield return Frames(2);
                        var n = $"r118-r-{ph}-yaw{Mathf.RoundToInt(h.Yaw)}"; yield return Shoot(h, dir, n, log);
                        var pts = Points(h, "op/scout", "op/fixer", "op/tech", "op/enforcer", "guard", "waitress"); int reached = fade ? fade.Reached : -1;
                        rows.Add($"{{\"shot\": \"{n}\", \"points\": \"{pts}\", \"reached\": {reached}, \"capsules\": {B(fade && fade.capsules)}}}"); log.Add($"{n}: {pts}; reached {reached}");
                        if (Mathf.RoundToInt(h.Yaw) == 210) { reachMax = Math.Max(reachMax, reached); reachRows.Add($"{{\"shot\": \"{n}\", \"reached\": {reached}}}"); }
                    }
                }
                yield return Phase(h, "day"); h.TurnSnap(0);
                parts.Add($"\"rim\": {{\"frames\": [{string.Join(", ", rows)}]}}");
            }

            // ---- V1-V3 (Glint, brief 063): Vale C out of the official entrance on his landing (yaw 210, day/evening/night; day also at 30, 120, 300), carried by the Enforcer
            // (yaw 30 and 120, day), at eye level in sun
            if (Archive)
            {
                yield return Fresh(h); var g = h.Game; var vrows = new List<string>();
                var vale = h.Views.Find("councillor"); var gd = h.Views.Find("guard"); bool landing = false;
                for (int i = 0; i < 420 * Suborned.Sim.Game.Hz && !g.Over && vale; i += 5) { h.RunTo(g.Tick + 5); if (i % 20 == 0) yield return null; if (g.Vale != ValeAt.Office && vale.gameObject.activeInHierarchy) { landing = true; break; } }
                /* Vale comes out of the official entrance at 5:50 (site schedule valeExits), not by the guard's public door: give him 2 s on his landing */
                if (landing) h.RunTo(g.Tick + 2 * Suborned.Sim.Game.Hz);
                if (landing)
                    foreach (var ph in new[] { "day", "evening", "night" })
                    {
                        yield return Phase(h, ph); h.TurnSnap(2); h.LookAt(vale.position); yield return Frames(4);
                        var n = $"r118-v1-{ph}"; yield return Shoot(h, dir, n, log); var pts = Points(h, "councillor", "guard", "driver"); vrows.Add($"{{\"shot\": \"{n}\", \"points\": \"{pts}\"}}"); log.Add($"{n}: {pts}; Vale {PxTall(h, vale)} px tall");
                        if (ph == "day")
                        {
                            var fade = h.View ? h.View.GetComponent<ArchitectureFade>() : null; int reached = fade ? fade.Reached : -1;
                            reachMax = Math.Max(reachMax, reached); reachRows.Add($"{{\"shot\": \"{n}\", \"reached\": {reached}}}"); log.Add($"{n}: reached {reached}");
                            // Glint 056 F2 (2026-10-26): the edge trial on the same frame, the soft band from 0.55, 0.70 and 0.85 of the radius (Glint picks)
                            if (fade)
                            {
                                float keep = fade.edgeInner;
                                foreach (var (e, tag) in new[] { (0.55f, "55"), (0.70f, "70"), (0.85f, "85") }) { fade.edgeInner = e; yield return Frames(3); yield return Shoot(h, dir, $"r118-v1-edge-{tag}", log); }
                                fade.edgeInner = 0.70f; yield return Frames(2); log.Add($"r118 edge trial: 0.55, 0.70, 0.85 shot; edgeInner was {F(keep)}, set to 0.70 after");
                            }
                        }
                    }
                if (landing) // E: Vale in play by day at the other three yaws (yaw 210 above), his drawn height in px (about 60 at 1080p)
                {
                    yield return Phase(h, "day");
                    foreach (var y in new[] { 0, 1, 3 })
                    {
                        h.TurnSnap(y); h.LookAt(vale.position); yield return Frames(4);
                        var n = $"r118-v1-day-yaw{Mathf.RoundToInt(h.Yaw)}"; yield return Shoot(h, dir, n, log); var pts = Points(h, "councillor", "guard"); vrows.Add($"{{\"shot\": \"{n}\", \"points\": \"{pts}\", \"px\": {PxTall(h, vale)}}}"); log.Add($"{n}: {pts}; Vale {PxTall(h, vale)} px tall");
                    }
                }
                yield return Phase(h, "day");
                if (vale) { var vp = vale.position; yield return Pose(h, dir, "r118-v3-eye", vp + vale.forward * 3f + Vector3.up * 1.6f, vp + Vector3.up * 1.4f, 40f, log); yield return Pose(h, dir, "r118-v3-eye-back", vp - vale.forward * 3f + Vector3.up * 1.6f, vp + Vector3.up * 1.4f, 40f, log); }
                // V2: carried (route A to the Enforcer out with Vale)
                yield return Fresh(h); g = h.Game; var orders = Route("route-a");
                foreach (var o in orders) // the route's orders until the Enforcer is out with Vale (the board and Go after it are not given)
                {
                    while (g.Tick < o.Tick && g.Vale != ValeAt.WithEnforcer && !g.Over) h.RunTo(Math.Min(o.Tick, g.Tick + 5));
                    if (g.Vale == ValeAt.WithEnforcer || g.Over) break;
                    var c = new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run };
                    if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c);
                }
                for (int i = 0; i < 120 * Suborned.Sim.Game.Hz && g.Vale != ValeAt.WithEnforcer && !g.Over; i += 5) h.RunTo(g.Tick + 5);
                bool carried = g.Vale == ValeAt.WithEnforcer;
                if (carried)
                    foreach (var y in new[] { 0, 1, 2, 3 })
                    {
                        h.TurnSnap(y); var en = h.Views.Find("op/enforcer"); if (en) h.LookAt(en.position); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.3f); h.SetPaused(true); yield return Frames(2);
                        var n = $"r118-v2-yaw{Mathf.RoundToInt(h.Yaw)}"; yield return Shoot(h, dir, n, log); var pts = Points(h, "councillor", "op/enforcer"); vrows.Add($"{{\"shot\": \"{n}\", \"points\": \"{pts}\"}}"); log.Add($"{n}: {pts}");
                    }
                if (carried)
                {
                    // E: the portraits in play: Intel open (the cast faces), the feed's history (the NPC chips), the carry plate (in the v2 frames)
                    h.TurnSnap(0); var en = h.Views.Find("op/enforcer"); if (en) h.LookAt(en.position);
                    h.IntelOpen = true; yield return Frames(6); yield return new WaitForSecondsRealtime(0.3f); yield return Shoot(h, dir, "r118-e-intel", log, "active"); h.IntelOpen = false;
                    h.FeedHistoryOpen = true; yield return Frames(6); yield return new WaitForSecondsRealtime(0.3f); yield return Shoot(h, dir, "r118-e-feed", log, "active"); h.FeedHistoryOpen = false;
                    yield return Frames(3);
                }
                h.TurnSnap(0);
                parts.Add($"\"vale\": {{\"landing\": {B(landing)}, \"carried\": {B(carried)}, \"frames\": [{string.Join(", ", vrows)}]}}");
                log.Add($"r118 vale: landing {landing}, carried {carried}, {vrows.Count} frames");
            }

            // ---- E: the driver's face and hands (Glyph's albedo, brief 063), eye level, and at play zoom
            if (Archive)
            {
                yield return Fresh(h); var dv = h.Views.Find("driver"); bool on = dv && dv.gameObject.activeInHierarchy;
                if (on)
                {
                    h.TurnSnap(2); h.LookAt(dv.position); yield return Frames(4); yield return Shoot(h, dir, "r118-e-driver-play", log);
                    var f = dv.forward; f.y = 0f; if (f.sqrMagnitude < 1e-4f) f = Vector3.forward; f.Normalize(); var p = dv.position;
                    yield return Pose(h, dir, "r118-e-driver-face", p + f * 1.6f + Vector3.up * 1.62f, p + Vector3.up * 1.58f, 28f, log);
                    yield return Pose(h, dir, "r118-e-driver-hands", p + f * 1.5f + Vector3.right * 0.3f + Vector3.up * 1.25f, p + Vector3.up * 1.0f, 38f, log);
                }
                log.Add($"r118 driver: {(on ? $"at {dv.position.x:0.0},{dv.position.y:0.00},{dv.position.z:0.0}, {PxTall(h, dv)} px tall at play zoom" : "no driver figure in view")}");
                h.TurnSnap(0);
            }

            // ---- C (056 F): the four agents close together in front of the café and of the Office, yaw 30, 120, 210 and 300, by day
            if (Archive)
            {
                var crows = new List<string>();
                foreach (var (place, tag) in new[] { ("d:Courtyard", "cafe"), ("d:Office approach", "office") })
                {
                    yield return Fresh(h); var g = h.Game; var at = g.S.At(place); var fade = h.View ? h.View.GetComponent<ArchitectureFade>() : null;
                    foreach (var op in new[] { "scout", "fixer", "tech", "enforcer" }) h.Issue(new Order { Op = op, Do = "move", To = place });
                    int there = 0;
                    for (int i = 0; i < 150 * Suborned.Sim.Game.Hz && !g.Over; i += 5)
                    {
                        h.RunTo(g.Tick + 5); there = 0;
                        foreach (var id in new[] { OpId.Scout, OpId.Fixer, OpId.Tech, OpId.Enforcer }) if (Suborned.Sim.G.Within(g.Ops[(int)id].Pos, at, 450)) there++;
                        if (there == 4) break;
                    }
                    yield return Phase(h, "day");
                    for (int y = 0; y < 4; y++)
                    {
                        h.TurnSnap(y); h.LookAt(Centre(h)); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.35f); h.SetPaused(true); yield return Frames(2);
                        var n = $"r118-c-{tag}-yaw{Mathf.RoundToInt(h.Yaw)}"; yield return Shoot(h, dir, n, log);
                        var pts = Points(h, "op/scout", "op/fixer", "op/tech", "op/enforcer"); int reached = fade ? fade.Reached : -1;
                        crows.Add($"{{\"shot\": \"{n}\", \"there\": {there}, \"points\": \"{pts}\", \"reached\": {reached}, \"capsules\": {B(fade && fade.capsules)}}}"); log.Add($"{n}: {there}/4 within 4.5 m of {place}; {pts}; reached {reached}");
                    }
                }
                h.TurnSnap(0);
                parts.Add($"\"cut4\": {{\"frames\": [{string.Join(", ", crows)}]}}");
            }

            // ---- F: a lane-mouth footway (brief 061, the export's ground.footways), the one nearest the Records Office, at play zoom and eye level
            if (Archive)
            {
                yield return Fresh(h);
                var office = h.Views.Find("camera/entrance"); var near = office ? office.position : h.frame.World(HandoverHost.OpeningFocus);
                string fid = ""; Vector3 fo = Vector3.zero, fin = Vector3.forward; float depth = 6f, run = 2.4f, best = float.MaxValue;
                try
                {
                    var wpath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), "out", "world", "harbour-district.world.json");
                    var txt = File.ReadAllText(wpath); var inv = CultureInfo.InvariantCulture;
                    foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(txt, "\"id\":\"(footway/[^\"]+)\"[^{]*?\"level\":([-0-9.]+),\"frame\":\\{\"origin\":\\[([-0-9.]+),([-0-9.]+)\\],\"along\":\\[[-0-9.]+,[-0-9.]+\\],\"into\":\\[([-0-9.]+),([-0-9.]+)\\]\\},\"half\":[0-9.]+,\"depth\":([0-9.]+)"))
                    {
                        float lv = float.Parse(m.Groups[2].Value, inv); var o = new Vector3(float.Parse(m.Groups[3].Value, inv), lv, float.Parse(m.Groups[4].Value, inv));
                        float d2 = (o - near).sqrMagnitude; if (d2 >= best) continue;
                        best = d2; fid = m.Groups[1].Value; fo = o; fin = new Vector3(float.Parse(m.Groups[5].Value, inv), 0f, float.Parse(m.Groups[6].Value, inv)); depth = float.Parse(m.Groups[7].Value, inv);
                    }
                }
                catch (Exception e) { log.Add("r118 footway: " + e.Message); }
                if (fid.Length > 0)
                {
                    var mid = fo + fin * (depth * 0.5f);
                    foreach (var y in new[] { 0, 2 }) { h.TurnSnap(y); h.LookAt(mid); yield return Frames(4); yield return Shoot(h, dir, $"r118-f-footway-yaw{Mathf.RoundToInt(h.Yaw)}", log); }
                    yield return Pose(h, dir, "r118-f-footway-eye", fo + fin * (depth + run + 5f) + Vector3.up * 1.7f, fo + fin * (depth * 0.4f), 50f, log);
                    log.Add($"r118 footway: {fid} at {fo.x:0.0},{fo.z:0.0} ({Mathf.Sqrt(best):0} m from the Office camera)");
                    parts.Add($"\"footway\": {{\"id\": \"{J(fid)}\", \"metres\": {F(Mathf.Sqrt(best))}}}");
                }
                else { log.Add("r118 footway: none found in the export"); parts.Add("\"footway\": null"); }
                h.TurnSnap(0);
            }

            // Glint 056 F1 test: no silhouette person without a cut on any frame; the reach under the cap at yaw 210 (reject past ReachedMax)
            {
                bool uOk = withUncut == 0, rOk = reachMax <= ReachedMax; ok &= uOk && rOk;
                parts.Add($"\"uncut\": {{\"ok\": {B(uOk)}, \"frames\": {uncutFrames}, \"withUncut\": {withUncut}, \"maxPoints\": {maxPoints}, \"cap\": {ArchitectureFade.MaxPoints}, \"overflows\": {overflows}}}");
                parts.Add($"\"reach\": {{\"ok\": {B(rOk)}, \"max\": {reachMax}, \"limit\": {ReachedMax}, \"frames\": [{string.Join(", ", reachRows)}]}}");
                log.Add($"r118 uncut: {(uOk ? "ok" : "FAILED")}: {withUncut} of {uncutFrames} frames with an uncut silhouette person; max points {maxPoints}/{ArchitectureFade.MaxPoints}, {overflows} at the cap; reach max {reachMax} (limit {ReachedMax}) {(rOk ? "ok" : "FAILED")}");
            }
            h.controls.planTogether = keepPlan; SignalHead.Hold = ""; h.SetNight(false);
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }

        /// <summary>A world-only picture from a free camera (the play view's lens copied, perspective), as Proof29's.</summary>
        static IEnumerator Pose(HandoverHost h, string dir, string name, Vector3 eye, Vector3 look, float fov, List<string> log)
        {
            var view = h.View; if (!view) yield break;
            var go = new GameObject("r118-cam"); var pc = go.AddComponent<Camera>(); pc.CopyFrom(view); pc.enabled = false; go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
            pc.orthographic = false; pc.fieldOfView = fov; pc.nearClipPlane = 0.05f; pc.farClipPlane = 400f; go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
            yield return new WaitForEndOfFrame();
            int w = Mathf.Min(Screen.width, 1920), ht = Mathf.Min(Screen.height, 1080); var rt = new RenderTexture(w, ht, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            try
            {
                for (int i = 0; i < 2; i++) { pc.targetTexture = rt; pc.Render(); }
                var prev = RenderTexture.active; RenderTexture.active = rt; var tx = new Texture2D(w, ht, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, w, ht), 0, 0); tx.Apply(); RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), tx.EncodeToJPG(90)); UnityEngine.Object.Destroy(tx); log.Add($"{name}: ok ({w}x{ht})");
            }
            catch (Exception e) { log.Add($"{name}: FAILED {e.Message}"); }
            pc.targetTexture = null; rt.Release(); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(go);
        }
    }
}
