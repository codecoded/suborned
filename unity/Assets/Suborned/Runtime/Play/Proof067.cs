using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;
using Pt = Suborned.Sim.Pt;
using ValeAt = Suborned.Sim.ValeAt;

namespace Suborned.Play
{
    /// <summary>Batch 0.2.123's brief 067 proof (Sky's notes on 0.2.122, briefs/067-sky-play-0122.md; Glyph's spec briefs/067/glyph.md), in the built
    /// player, one JSON object `r067`. Every order is the host's own (clicks, holds and rings as the player gives them, or the engine order where a
    /// route would give it). Pictures go to the run's folder with their HUD manifests (hud-budget, the 2x crops).
    ///  · square: the patrol's new post (read from contracts/missions/handover/mission.json at shot time: patrol-a, patrol-b) at yaw 210, and the
    ///    square by the getaway car at play zoom (yaw 30); the official car at 6:10 with Vale at its kerb-side door: no figure inside a standing
    ///    car's body (engine 0.33.1; gated: carsClear).
    ///  · cones (F): the Office front at 6:00, the guard, the entrance camera and the patrol learned: every learned cone (Alt) against the cones that
    ///    matter now (gated: fewer drawn after).
    ///  · camera (G): a portrait click selects and frames; third person enters behind the agent's heading, at two headings; T back to the map
    ///    centred on the agent at play zoom, at the yaw it was left at (gated).
    ///  · locked (E): the Scout on the guard once heard: the locked pill (no Fire), the shut ring (Listen shut first, Fire last and dashed), a press
    ///    on the shut wedge (its pop, the ring closed); the Tech's click on the guard: the who-can pop, no walk (gated).
    ///  · board (C): three selected, one running: the squad Board pill (the givers' faces), the ring's one Board wedge taken (one order each in
    ///    the same tick), the running Tech's why chip, the pending and filled pips, the boarded out of the selection; then with nobody selected the
    ///    car's Drive pill (its stand live) and ring (Drive, Go last with the struck Vale cameo and the hold, no dashes; its Alt line Fable's
    ///    less.go.noVale), the Drive taken and parked (gated).
    ///  · convo (H): Inez (the Fixer's Help) and the driver (the Fixer's Pay, the Scout's Listen) from the ground view at two headings each: both
    ///    speakers idle and facing, the shot the ladder took (Glyph Rev 2 H1: profile, high, intercut) holding both heads (the intercut the one
    ///    speaking), the Listen forced into the high two-shot (H5), the view back exactly (gated).
    ///  · reticle (D): a lasting fire order on a rival walking to his post: the reticle set, following him, the edge chip off screen, a re-target's
    ///    hand-off (gated: the reticle stays on him while he moves).
    ///  · people (Pulse 067 W2, §2.5; Vela 067 L): a shift guard in the crowd hovered once the guard's sight is learned: "a guard", a pickable
    ///    "Guard", his ring; the relief's 1.2 m step at 5:30 walked; no body in another figure than its walker's; the rival car Sedan_Rival (gated).
    ///  · sidecar (E): the host record beside the session script has lines (gated).</summary>
    public static class Proof067
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
            log.Add($"{name}: pill [{HudToolkit.PillState}: {HudToolkit.PillWords}] stack [{HudToolkit.StackShown}] stands [{HudToolkit.StandsShown}] reticles [{HudToolkit.ReticlesShown}] ring [{h.RingWords}]");
            h.SetPaused(was);
        }
        static IEnumerator Fresh(HandoverHost h) { HandoverHost.PointerWorld = null; HandoverHost.PointerOverride = null; h.PeekLatch = false; h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; yield return null; }
        static void To(HandoverHost h, int tick) { var g = h.Game; while (g.Tick < tick && !g.Over) h.RunTo(Math.Min(tick, g.Tick + 50)); }
        static IEnumerator Settle(HandoverHost h, float s = 0.35f) { h.SetPaused(false); yield return new WaitForSecondsRealtime(s); h.SetPaused(true); yield return Frames(3); }
        static string Root => Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir("")));
        static List<Order> Route(string name)
        {
            var sc = Suborned.Sim.Script.Parse(File.ReadAllText(Path.Combine(Root, "contracts", "missions", "handover", "routes", name + ".json")));
            return sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
        }
        static void Give(HandoverHost h, Order o)
        {
            var c = new Order { Tick = h.Game.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run };
            if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c);
        }
        static Vector2? Screen(HandoverHost h, Vector3 w) { var s = h.View.WorldToScreenPoint(w); return s.z > 0f ? new Vector2(s.x, s.y) : null; }
        static Vector3? ViewAt(HandoverHost h, string key) => h.Views.Find(key) is Transform t && t.gameObject.activeInHierarchy ? t.position : null;
        static float Flat(Vector3 a, Vector3 b) { var d = a - b; d.y = 0f; return d.magnitude; }
        /// <summary>A point of mission.json's `points` (block metres), as a world point; null when the file or the point is missing.</summary>
        static Vector3? MissionPoint(HandoverHost h, string id, List<string> log)
        {
            try
            {
                var root = Suborned.Sim.JNode.Parse(File.ReadAllText(Path.Combine(Root, "contracts", "missions", "handover", "mission.json")), "mission.json");
                double Num(Suborned.Sim.JNode n) => double.Parse(n.NumberText, CultureInfo.InvariantCulture);
                foreach (var p in root["points"].Items) if (p["id"].Str() == id) { var at = p["at"].Items; return h.frame.World(new Pt((int)Math.Round(Num(at[0]) * 100), (int)Math.Round(Num(at[1]) * 100))); }
                log.Add($"r067 square: mission.json has no point {id}");
            }
            catch (Exception e) { log.Add($"r067 square: mission.json not read ({e.Message})"); }
            return null;
        }
        static IEnumerator WaitSim(HandoverHost h, Func<bool> done, int ticks) { var g = h.Game; for (int i = 0; i < ticks && !done() && !g.Over; i++) { h.RunTo(g.Tick + 1); if (i % 20 == 0) yield return null; } }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            var keepConvo = h.controls.conversations;

            // ---- square: the patrol's post and the cleared square (pictures); the official car at 6:10 (gated: nobody inside a standing car)
            {
                yield return Fresh(h); var g = h.Game;
                var pa = MissionPoint(h, "patrol-a", log); var pb = MissionPoint(h, "patrol-b", log); string patrolWhere = "";
                if (pa.HasValue && pb.HasValue)
                {
                    var mid = (pa.Value + pb.Value) / 2f; for (int k = 0; k < 4; k++) { h.TurnSnap(k); if (Mathf.Abs(Mathf.DeltaAngle(h.Yaw, 210f)) < 1f) break; }
                    h.LookAt(mid); To(h, 30 * Suborned.Sim.Game.Hz); yield return Settle(h);
                    var pp = g.PatrolPose(); float off = pp.HasValue ? DistToSegment(h.frame.World(pp.Value.pos), pa.Value, pb.Value) : -1f;
                    patrolWhere = $"mission line {F(pa.Value.x)},{F(pa.Value.z)} to {F(pb.Value.x)},{F(pb.Value.z)}; the engine's patrol {(pp.HasValue ? F(off) + " m off it" : "not on the street")}";
                    yield return Shoot(h, dir, "r067-patrol-post", log);
                }
                h.TurnSnap(0); h.View.orthographicSize = 14f; h.LookAt(h.GetawayWorld); yield return Frames(4); yield return Shoot(h, dir, "r067-square-cleared", log);
                // the official car from 4:50 to 5:20 (Vale walks to its kerb-side door and waits there): nobody drawn inside a standing car's body
                var inCars = new List<string>(); To(h, 4 * 600 + 50 * Suborned.Sim.Game.Hz);
                for (int t = 0; t < 30 * Suborned.Sim.Game.Hz && !g.Over; t += 5) { h.RunTo(g.Tick + 5); h.Views.Frame(1f); foreach (var x in h.FiguresInCars()) if (!inCars.Contains(x)) inCars.Add(x); }
                To(h, 5 * 600 + 10 * Suborned.Sim.Game.Hz); yield return Settle(h); var oc = ViewAt(h, "car/official"); if (oc.HasValue) { h.TurnSnap(0); h.LookAt(oc.Value); } yield return Frames(3);
                var vp = g.ValePos(); var vale = ViewAt(h, "councillor"); float valeFromCar = vp.HasValue ? Suborned.Sim.G.Dist(vp.Value, g.CarPos()) / 100f : -1f;
                yield return Shoot(h, dir, "r067-official-car", log);
                bool carsClear = inCars.Count == 0;
                ok &= carsClear;
                parts.Add($"\"square\": {{\"ok\": {B(carsClear)}, \"patrol\": \"{J(patrolWhere)}\", \"inCars\": \"{J(string.Join(" ", inCars))}\", \"vale\": \"{g.Vale}\", \"valeFromCarM\": {F(valeFromCar)}, \"valeDrawn\": {B(vale.HasValue)}}}");
                log.Add($"r067 square: {(carsClear ? "ok" : "FAILED")}: {patrolWhere}; inside a car 5:50-6:20 [{string.Join(" ", inCars)}]; Vale {g.Vale} {F(valeFromCar)} m from the car's point");
            }

            // ---- cones (F): every learned cone (Alt) against the ones that matter now, at the Office front at 5:00 (brief 080: the shift a minute earlier)
            {
                yield return Fresh(h); var g = h.Game;
                h.GiveWatchScripted(OpId.Scout, "guard"); h.GiveWatchScripted(OpId.Tech, "camera:entrance"); h.GiveWatchScripted(OpId.Fixer, "patrol");
                To(h, 5 * 600); yield return Settle(h); h.Select(OpId.Enforcer, false);
                var door = h.frame.World(g.S.At(g.S.GuardPost)); h.TurnSnap(0); h.View.orthographicSize = 14f; h.LookAt(door); HandoverHost.PointerOverride = new Vector2(40f, 40f); yield return Frames(6);
                h.PeekLatch = true; yield return new WaitForSecondsRealtime(0.4f); yield return Frames(8);
                string before = h.Views.FanStrengths; yield return Shoot(h, dir, "r067-cones-before", log, "alt");
                h.PeekLatch = false; yield return new WaitForSecondsRealtime(0.9f); yield return Frames(4);
                string after = h.Views.FanStrengths; yield return Shoot(h, dir, "r067-cones-after", log);
                int nb = Drawn(before), na = Drawn(after); HandoverHost.PointerOverride = null;
                bool cOk = nb >= 2 && na < nb;
                ok &= cOk;
                parts.Add($"\"cones\": {{\"ok\": {B(cOk)}, \"at\": \"{HandoverHost.MissionClock(g.Tick)}\", \"before\": \"{J(before)}\", \"after\": \"{J(after)}\", \"drawnBefore\": {nb}, \"drawnAfter\": {na}}}");
                log.Add($"r067 cones: {(cOk ? "ok" : "FAILED")}: at {HandoverHost.MissionClock(g.Tick)} before (Alt) [{before}] {nb} drawn; after [{after}] {na} drawn");
            }

            // ---- camera (G): a portrait click frames; third person behind the heading (two headings); back to the map at play zoom, the same yaw
            {
                yield return Fresh(h); var g = h.Game; To(h, 50); yield return Settle(h);
                h.Select(OpId.Scout, false); h.TurnSnap(0); h.View.orthographicSize = 16f; h.LookAt(h.frame.World(g.S.At(g.S.GuardPost))); yield return Frames(3);
                int frames0 = h.PortraitFrames; h.PortraitClick((int)OpId.Fixer, false); yield return new WaitForSecondsRealtime(0.15f); yield return Shoot(h, dir, "r067-portrait-pulse", log);
                yield return new WaitForSecondsRealtime(0.6f); yield return Frames(3);
                var fx = ViewAt(h, "op/fixer"); float framedOff = fx.HasValue ? Flat(h.Focus, fx.Value) : 99f;
                bool framed = h.PortraitFrames > frames0 && h.Selected[(int)OpId.Fixer] && !h.Selected[(int)OpId.Scout] && framedOff < 0.6f;
                yield return Shoot(h, dir, "r067-portrait-framed", log);
                var heads = new List<string>(); bool headsOk = true, backOk = true; var backs = new List<string>();
                for (int pass = 0; pass < 2; pass++)
                {
                    if (pass == 1)
                    {   // a second heading: the Fixer walks 5 m across his first one
                        var me = g.Ops[(int)OpId.Fixer].Pos; float fy = h.Views.FigureYaw("op/fixer") ?? 0f; var dirW = Quaternion.Euler(0f, fy + 90f, 0f) * Vector3.forward;
                        var want = h.frame.Engine(h.frame.World(me) + dirW * 5f); var to = g.Walk.NearestFree(want) ?? want;
                        h.Issue(new Order { Op = "fixer", Do = "move", Point = to }); yield return WaitSim(h, () => g.Ops[(int)OpId.Fixer].Walk == null, 15 * Suborned.Sim.Game.Hz); yield return Settle(h, 0.6f);
                    }
                    float yawMap = h.Yaw, figYaw = h.Views.FigureYaw("op/fixer") ?? float.NaN;
                    h.Select(OpId.Fixer, false); h.DriveToggle(); var tr0 = h.FollowTrace; yield return null; float entryCam = h.FollowYaw, entryFig = h.FollowAgentYaw; { var trs = new List<string> { tr0, h.FollowTrace }; for (int f = 0; f < 4; f++) { yield return null; trs.Add(h.FollowTrace); } log.Add($"r067 camera trace pass {pass + 1}: {string.Join(" | ", trs)}"); } // the same frame: the camera against the figure's drawn heading as it enters
                    yield return new WaitForSecondsRealtime(0.8f); yield return Frames(3);
                    float camYaw = h.FollowYaw, figNow = h.FollowAgentYaw, d0 = Mathf.Abs(Mathf.DeltaAngle(entryCam, entryFig)), d = Mathf.Abs(Mathf.DeltaAngle(camYaw, figNow));
                    bool behind = h.Following && h.Driving && d0 < 12f && d < 12f; headsOk &= behind;
                    heads.Add($"heading {F(figYaw)} (map) {F(entryFig)} (entry) {F(figNow)} (0.8 s) camera {F(entryCam)} → {F(camYaw)} off {F(d0)} / {F(d)}"); yield return Shoot(h, dir, $"r067-tp-entry-{pass + 1}", log);
                    h.DriveLetGoToMap(true); yield return new WaitForSecondsRealtime(0.9f); yield return Frames(4);
                    var fa = ViewAt(h, "op/fixer"); float off = fa.HasValue ? Flat(h.Focus, fa.Value) : 99f;
                    float playZoom = Mathf.Clamp(h.Cam.overhead.zoom, h.zoomMin, h.zoomMax);
                    bool back = !h.Following && h.View.orthographic && Mathf.Abs(h.View.orthographicSize - playZoom) < 0.05f && Mathf.Abs(Mathf.DeltaAngle(h.Yaw, yawMap)) < 0.5f && off < 0.6f && h.FollowExitGlides > 0;
                    backOk &= back; backs.Add($"yaw {F(h.Yaw)} (left {F(yawMap)}) zoom {F(h.View.orthographicSize)} off {F(off)} m");
                    if (pass == 0) yield return Shoot(h, dir, "r067-back-to-plan", log);
                }
                bool gOk = framed && headsOk && backOk;
                ok &= gOk;
                parts.Add($"\"camera\": {{\"ok\": {B(gOk)}, \"framed\": {B(framed)}, \"framedOffM\": {F(framedOff)}, \"entries\": \"{J(string.Join("; ", heads))}\", \"back\": \"{J(string.Join("; ", backs))}\"}}");
                log.Add($"r067 camera: {(gOk ? "ok" : "FAILED")}: portrait framed {framed} ({F(framedOff)} m); entries [{string.Join("; ", heads)}]; back [{string.Join("; ", backs)}]");
            }

            // ---- locked (E): the Scout on the guard once heard; the Tech on the guard (no row): the who-can pop, no walk
            {
                yield return Fresh(h); var g = h.Game; 
                h.GiveWatchScripted(OpId.Scout, "guard"); yield return WaitSim(h, () => h.CastOf("guard")?.KRoutine == true || g.Known("guard") is { Sight: true }, 150 * Suborned.Sim.Game.Hz);
                h.GiveScripted(new Order { Op = "scout", Do = "listen", Target = "guard" }); yield return WaitSim(h, () => h.CastOf("guard")?.KNeed == true && g.Ops[0].Busy == null, 150 * Suborned.Sim.Game.Hz);
                yield return Settle(h); h.Select(OpId.Scout, false);
                var gw = ViewAt(h, "guard"); string pill = "", ring = "", popWords = "", whoFaces = ""; bool shutFirst = false, fireLast = false, closed = false, noWalk = false, whoPop = false;
                if (gw.HasValue)
                {
                    h.TurnSnap(0); h.View.orthographicSize = 14f; h.LookAt(gw.Value); yield return Frames(3);
                    HandoverHost.PointerWorld = gw.Value + Vector3.up * 1.2f; yield return Frames(8); yield return new WaitForSecondsRealtime(0.25f);
                    pill = HudToolkit.PillState + ": " + HudToolkit.PillWords; yield return Shoot(h, dir, "r067-lock-pill-guard", log, "active");
                    if (Screen(h, gw.Value + Vector3.up * 1.2f) is Vector2 gs) { h.LeftClickScripted(gs); yield return Frames(4); }
                    ring = h.RingWords; var segs = h.Ring?.Segs; shutFirst = segs != null && segs.Count > 0 && segs[0].Shut; fireLast = segs != null && segs.FindLastIndex(s => !s.More) is int li && li >= 0 && segs[li].Row?.I?.Order?.Do == "fire" && segs[li].Violent;
                    yield return Shoot(h, dir, "r067-shut-ring-guard", log, "active");
                    if (segs != null && segs.Count > 0 && segs[0].Shut) { int pops0 = h.Pops.Count; h.RingTakeScripted(segs[0].Row?.I?.Order?.Do ?? "listen"); yield return Frames(3); closed = h.Ring == null && h.Pops.Count > pops0; popWords = h.Pops.Count > 0 ? h.Pops[^1].Text : ""; yield return Shoot(h, dir, "r067-shut-pop", log, "active"); }
                    HandoverHost.PointerWorld = null;
                    // the Tech has no row on the guard (no Watch once watched, no Listen): a click pops who can, and never walks
                    h.Select(OpId.Tech, false); yield return Frames(2); int who0 = h.WhoCanPops; var tp0 = g.Ops[(int)OpId.Tech].Pos;
                    if (Screen(h, gw.Value + Vector3.up * 1.2f) is Vector2 gs2) { h.LeftClickScripted(gs2); yield return Frames(3); }
                    whoPop = h.WhoCanPops > who0; noWalk = g.Ops[(int)OpId.Tech].Walk == null && h.Ring == null;
                    whoFaces = h.Pops.Count > 0 && h.Pops[^1].Faces != null ? string.Join(",", h.Pops[^1].Faces.Select(k => Suborned.Sim.Op.Name((OpId)k))) : "";
                    yield return Shoot(h, dir, "r067-who-can", log, "active");
                    // two or more under the cursor: the stack tab (logged, not gated: the crowd decides)
                    h.Select(OpId.Scout, false); var dm = h.frame.World(g.S.At(g.S.GuardPost)); HandoverHost.PointerWorld = dm + Vector3.up * 0.6f; yield return Frames(8); yield return new WaitForSecondsRealtime(0.2f);
                    log.Add($"r067 stack: {h.HoverStackCount} under the cursor at the door, tab [{HudToolkit.StackShown}]"); if (h.HoverStackCount >= 2) yield return Shoot(h, dir, "r067-stack-tab", log, "active");
                    HandoverHost.PointerWorld = null;
                }
                bool eOk = pill.StartsWith("lock") && !pill.Contains("Fire") && shutFirst && closed && whoPop && noWalk; // Fire last and dashed is logged: the Scout has no Fire row on the guard (only the Enforcer's knob gives one), so his shut ring is Listen and the panel
                ok &= eOk;
                parts.Add($"\"locked\": {{\"ok\": {B(eOk)}, \"pill\": \"{J(pill)}\", \"ring\": \"{J(ring)}\", \"shutFirst\": {B(shutFirst)}, \"fireLastDashed\": {B(fireLast)}, \"pressPop\": \"{J(popWords)}\", \"ringClosed\": {B(closed)}, \"whoCanPop\": {B(whoPop)}, \"whoFaces\": \"{whoFaces}\", \"noWalk\": {B(noWalk)}}}");
                log.Add($"r067 locked: {(eOk ? "ok" : "FAILED")}: pill [{pill}]; ring [{ring}] shut first {shutFirst} fire last {fireLast}; press [{popWords}] closed {closed}; who-can {whoPop} [{whoFaces}] no walk {noWalk}");
            }

            // ---- board (C): one order boards the squad; the refused one says why; then the car's Drive with nobody selected
            {
                yield return Fresh(h); var g = h.Game; To(h, 30); yield return Settle(h); bool keepPlan = h.controls.planTogether; h.controls.planTogether = false; // orders given while paused go now (no "start together" queue), as Proof29
                var carW = h.GetawayWorld; h.TurnSnap(0); h.View.orthographicSize = 14f; h.LookAt(carW); yield return Frames(3);
                // the Tech runs off: his Board is refused (board-running); the Scout and the Fixer board
                var tp = g.Ops[(int)OpId.Tech].Pos; var far = g.Walk.NearestFree(new Pt(tp.X + 1800, tp.Y)) ?? tp; h.Issue(new Order { Op = "tech", Do = "move", Point = far, Run = true }); h.RunTo(g.Tick + 3);
                h.Select(OpId.Scout, false); h.Select(OpId.Fixer, true); h.Select(OpId.Tech, true); yield return Frames(2);
                HandoverHost.PointerWorld = carW + Vector3.up * 0.6f; yield return Frames(8); yield return new WaitForSecondsRealtime(0.25f);
                string boardPill = HudToolkit.PillWords; int faces = h.HoverDoers.Count; yield return Shoot(h, dir, "r067-squad-board-pill", log, "active");
                if (Screen(h, carW + Vector3.up * 0.6f) is Vector2 cs) { h.LeftClickScripted(cs); yield return Frames(3); }
                string ring1 = h.RingWords; yield return Shoot(h, dir, "r067-car-ring-board", log, "active");
                int sq0 = h.SquadBoards, orders0 = g.Orders.Count, tick0 = g.Tick; h.RingTakeScripted("board"); HandoverHost.PointerWorld = null; yield return Frames(2);
                var (wi, ww, _) = h.WhyChipOf((int)OpId.Tech); string why = $"{wi} {ww}".Trim();
                bool leftSel = !h.Selected[(int)OpId.Scout] && !h.Selected[(int)OpId.Fixer];
                h.RunTo(g.Tick + 10); yield return Settle(h, 0.3f); var (_, pend, _) = h.SeatState(); string pendWords = string.Join("", pend.Select(x => x ? "p" : "-"));
                yield return Shoot(h, dir, "r067-board-pending", log, "active");
                yield return WaitSim(h, () => g.Ops[0].InCar && g.Ops[1].InCar, 40 * Suborned.Sim.Game.Hz); yield return Settle(h, 0.4f);
                var (filled, _, _) = h.SeatState(); string filledWords = string.Join("", filled.Select(x => x ? "f" : "-")); bool aboard = g.Ops[0].InCar && g.Ops[1].InCar;
                // read once they are aboard: the squad's Board may go out after the take's frame (a paused clock's plan-together queue), on one tick
                // one order for the squad: each doer's first order (the walk-in's move, or the Board itself in reach) on the same tick; the Boards then go as each arrives
                var sameTick = new[] { "scout", "fixer" }.Select(op => g.Orders.Skip(orders0).Where(o => o.order.Op == op && o.order.Do is "board" or "move").Select(o => o.order.Tick).DefaultIfEmpty(-1).First()).Where(t => t >= 0).Distinct().ToList();
                log.Add($"r067 board orders: [{string.Join(",", g.Orders.Skip(orders0).Select(o => $"{o.order.Tick}:{o.order.Op}:{o.order.Do}:{(o.ok ? "taken" : o.code)}"))}]");
                yield return Shoot(h, dir, "r067-board-landed", log, "active");
                // nobody selected: the car answers (Drive is the hold with one stand open and Vale not aboard)
                for (int k = 0; k < 4; k++) if (h.Selected[k]) h.Select((OpId)k, true); yield return Frames(2);
                HandoverHost.PointerWorld = h.GetawayWorld + Vector3.up * 0.6f; yield return Frames(8); yield return new WaitForSecondsRealtime(0.3f);
                string drivePill = HudToolkit.PillWords, standsHover = HudToolkit.StandsShown; yield return Shoot(h, dir, "r067-drive-pill-car", log, "active");
                if (Screen(h, h.GetawayWorld + Vector3.up * 0.6f) is Vector2 cs2) { h.LeftClickScripted(cs2); yield return Frames(3); }
                string ring2 = h.RingWords; bool goLast = h.Ring != null && h.Ring.Segs.FindLastIndex(s => !s.More) is int gl && gl >= 0 && h.Ring.Segs[gl].Row?.I?.Order?.Do == "go" && !h.Ring.Segs[gl].Violent && h.Ring.Segs[gl].HoldWedge && h.Ring.Segs[gl].ObjStruck; // Glyph Rev 2 C5b: last, a struck Vale cameo, the 0.5 s hold, no dashes
                h.PeekLatch = true; yield return new WaitForSecondsRealtime(0.35f); yield return Frames(3); string goAlt = HudToolkit.RingAltShown; log.Add($"r067 go alt line: '{goAlt}' placed {HudToolkit.RingAltPlacement}"); yield return Shoot(h, dir, "r067-car-ring-go-alt", log, "alt"); h.PeekLatch = false; yield return Frames(2); // Fable's less.go.noVale under Alt
                yield return Shoot(h, dir, "r067-car-ring-drive", log, "active");
                if (h.RingWedgeScreen("drive-car") is Vector2 wd) { HandoverHost.PointerOverride = wd; yield return Frames(6); }
                string standsWedge = HudToolkit.StandsShown; yield return Shoot(h, dir, "r067-car-ring-drive-hover", log, "active");
                HandoverHost.PointerOverride = null; HandoverHost.PointerWorld = null; int dOrders0 = g.Orders.Count; bool driveTaken = h.RingTakeScripted("drive-car"); yield return Frames(2);
                bool driveTook = driveTaken; driveTaken &= g.GetawayDriving;
                log.Add($"r067 board drive: take {driveTook}, driving {g.GetawayDriving}, paused {h.Paused}, orders [{string.Join(",", g.Orders.Skip(dOrders0).Select(o => $"{o.order.Op}:{o.order.Do}:{o.order.Target}:{(o.ok ? "taken" : o.code)}"))}]"); yield return WaitSim(h, () => !g.GetawayDriving, 60 * Suborned.Sim.Game.Hz); yield return Settle(h, 0.4f); h.LookAt(h.GetawayWorld); yield return Frames(3);
                bool parked = !g.GetawayDriving && g.CurStand != g.S.Stands[0]; h.controls.planTogether = keepPlan; yield return Shoot(h, dir, "r067-drive-parked", log, "active");
                bool cOk = boardPill.Contains("Board") && faces == 2 && h.SquadBoards > sq0 && sameTick.Count == 1 && ww.Length > 0 && leftSel && aboard && drivePill.Contains("Drive") && standsHover.Contains(":hover") && goLast && goAlt.Length > 0 && driveTaken && parked;
                ok &= cOk;
                parts.Add($"\"board\": {{\"ok\": {B(cOk)}, \"pill\": \"{J(boardPill)}\", \"faces\": {faces}, \"ring\": \"{J(ring1)}\", \"sameTick\": \"{string.Join(",", sameTick)}\", \"techWhy\": \"{J(why)}\", \"leftSelection\": {B(leftSel)}, \"pending\": \"{pendWords}\", \"filled\": \"{filledWords}\", \"drivePill\": \"{J(drivePill)}\", \"standsOnHover\": \"{J(standsHover)}\", \"driveRing\": \"{J(ring2)}\", \"goLastStruckHeld\": {B(goLast)}, \"goAlt\": \"{J(goAlt)}\", \"standsOnWedge\": \"{J(standsWedge)}\", \"driven\": {B(driveTaken)}, \"parked\": {B(parked)}}}");
                log.Add($"r067 board: {(cOk ? "ok" : "FAILED")}: pill [{boardPill}] faces {faces}; ring [{ring1}]; orders on ticks [{string.Join(",", sameTick)}]; Tech why [{why}]; left selection {leftSel}; pips pending {pendWords} filled {filledWords}; drive pill [{drivePill}] stands [{standsHover}]; ring [{ring2}] go last {goLast} alt [{goAlt}]; wedge stands [{standsWedge}]; driven {driveTaken} parked {parked}");
            }

            // ---- convo (H): the ground view, Inez and the driver, two headings each
            {
                h.controls.conversations = ConversationScenes.Always; var scenes = new List<string>(); bool hOk = true;
                IEnumerator Scene(string name, string agent, string wantShot = "")
                {
                    var g = h.Game;
                    for (int i = 0; i < 120 * Suborned.Sim.Game.Hz && !h.ConvoPending && !h.ConvoOn && !g.Over; i++) { h.RunTo(g.Tick + 1); if (i % 10 == 0) yield return null; }
                    for (int i = 0; i < 30 && !h.ConvoOn && h.ConvoPending; i++) yield return null;
                    if (!h.ConvoOn) { scenes.Add($"{name}: no scene ({h.ConvoLast})"); hOk = false; yield break; }
                    var cam = h.View; yield return new WaitForSecondsRealtime(0.5f);
                    var keys = h.ConvoViewKeys(); string ka = keys?.a ?? "", kb = keys?.b ?? "";
                    var (sa, _) = h.Views.FigureClip(ka); var (sb, _) = h.Views.FigureClip(kb);
                    float ya = h.Views.FigureYaw(ka) ?? 0f, yb = h.Views.FigureYaw(kb) ?? 0f; var pa = ViewAt(h, ka); var pb = ViewAt(h, kb);
                    bool idle = sa.StartsWith("idle") && sb.StartsWith("idle");
                    float facing = pa.HasValue && pb.HasValue ? Mathf.Abs(Mathf.DeltaAngle(ya, Mathf.Atan2(pb.Value.x - pa.Value.x, pb.Value.z - pa.Value.z) * Mathf.Rad2Deg)) + Mathf.Abs(Mathf.DeltaAngle(yb, Mathf.Atan2(pa.Value.x - pb.Value.x, pa.Value.z - pb.Value.z) * Mathf.Rad2Deg)) : 999f;
                    bool InFrame(Vector3? p) { if (!p.HasValue) return false; var v = cam.WorldToViewportPoint(p.Value + Vector3.up * 1.6f); return v.z > 0f && v.x > 0.04f && v.x < 0.96f && v.y > 0.06f && v.y < 0.94f; }
                    // Glyph 067 Rev 2 H1: the two-shots hold both heads; the intercut the one speaking now; never one person for the whole scene
                    var cn = h.ConvoNow; string shot = cn?.Shot ?? "";
                    bool both = shot == "intercut" ? InFrame(cn.Framed == 0 ? pa : pb) : shot == "single" ? InFrame(pb) : InFrame(pa) && InFrame(pb);
                    bool shotOk = wantShot.Length == 0 ? shot is "profile" or "high" or "intercut" or "single" : shot == wantShot;
                    yield return Shoot(h, dir, name, log);
                    h.ConvoSkip(); for (int i = 0; i < 60 && h.ConvoOn; i++) yield return null;
                    bool back = h.ConvoLast.Contains("bit-identical");
                    bool sOk = idle && facing < 30f && both && back && shotOk; hOk &= sOk;
                    scenes.Add($"{name}: {(sOk ? "ok" : "FAILED")} {shot}{(wantShot.Length > 0 ? " (forced " + wantShot + ")" : "")} {h.ConvoLast}; {ka} {sa}, {kb} {sb}; facing off {F(facing)} deg; in frame {both}");
                }
                // Inez: route A's opening (the Scout watches and listens), then the Fixer's Help from the ground view (engine 0.44.0: one Help after the Listen turns her)
                yield return Fresh(h); var g0 = h.Game; List<Order> ra = null; try { ra = Route("route-a"); } catch (Exception e) { scenes.Add("route-a: " + e.Message); hOk = false; }
                if (ra != null)
                {
                    // the route's own Help tick (c054: 596, once the Scout's Listen is heard; before it a Help is only the small offer she refuses)
                    var helps = ra.Where(o => o.Op == "fixer" && o.Do == "turn" && o.Approach == "help").Select(o => o.Tick).ToList(); int help1 = helps.Count > 0 ? helps[0] : 600;
                    foreach (var o in ra) { if (o.Tick >= help1 || g0.Over) break; if (o.Tick > g0.Tick) To(h, o.Tick); Give(h, o); while (h.ConvoPending || h.ConvoOn) { yield return null; h.ConvoEndNow(); } }
                    To(h, help1); for (int i = 0; i < 10; i++) { h.ConvoEndNow(); yield return null; } // the Scout's Listen scene (route A's opening) is not this proof's
                    h.Select(OpId.Fixer, false); h.DriveToggle(); yield return new WaitForSecondsRealtime(0.7f);
                    h.GiveScripted(new Order { Op = "fixer", Do = "turn", Target = "waitress", Approach = "help" }); yield return Scene("r067-convo-inez-1", "fixer");
                    h.DriveLetGoToMap(true); yield return new WaitForSecondsRealtime(0.8f);
                }
                // the driver: route B's opening; the Fixer's Pay from the ground view, then the Scout's Listen (the other side)
                yield return Fresh(h); var g1 = h.Game; List<Order> rb = null; try { rb = Route("route-b"); } catch (Exception e) { scenes.Add("route-b: " + e.Message); hOk = false; }
                if (rb != null)
                {
                    foreach (var o in rb) { if (o.Tick >= 620 || g1.Over) break; if (o.Tick > g1.Tick) To(h, o.Tick); Give(h, o); }
                    To(h, 620); for (int i = 0; i < 10; i++) { h.ConvoEndNow(); yield return null; }
                    h.Select(OpId.Fixer, false); h.DriveToggle(); yield return new WaitForSecondsRealtime(0.7f);
                    h.GiveScripted(new Order { Op = "fixer", Do = "turn", Target = "driver", Approach = "pay" }); yield return Scene("r067-convo-driver-1", "fixer");
                    h.DriveLetGoToMap(true); yield return new WaitForSecondsRealtime(0.8f);
                    h.Select(OpId.Scout, false); h.DriveToggle(); yield return new WaitForSecondsRealtime(0.7f);
                    HandoverHost.ConvoForceShot = "high"; // Glyph 067 H5: one scene forced into the high two-shot (the cut and the silhouettes on)
                    h.GiveScripted(new Order { Op = "scout", Do = "listen", Target = "driver" }); yield return Scene("r067-convo-driver-2", "scout", "high");
                    HandoverHost.ConvoForceShot = "";
                    h.DriveLetGoToMap(true); yield return new WaitForSecondsRealtime(0.8f);
                }
                h.controls.conversations = keepConvo; HandoverHost.ConvoForceShot = "";
                ok &= hOk;
                parts.Add($"\"convo\": {{\"ok\": {B(hOk)}, \"scenes\": \"{J(string.Join(" ; ", scenes))}\"}}");
                log.Add($"r067 convo: {(hOk ? "ok" : "FAILED")}: {string.Join(" ; ", scenes)}");
            }

            // ---- reticle (D): a lasting fire order on a rival walking to his post
            {
                // engine 0.41.0: no drop at 2:46; the three step out of the car about 5 s after it boxes ours in (~2:58), so the order waits for
                // rival-1 on foot, walking to his post
                yield return Fresh(h); var g = h.Game; To(h, g.CrewEntry); while (g.Tick < g.CrewEntry + 90 * Suborned.Sim.Game.Hz && !g.Over && !(h.Views.RivalModes.TryGetValue("rival-1", out var m1) && m1 == "walking")) h.RunTo(g.Tick + 1);
                h.RunTo(g.Tick + 5); yield return Settle(h);
                h.Select(OpId.Enforcer, false); var r1 = ViewAt(h, "rival-1"); bool set = false, follows = false, edge = false, handoff = false; string words = "";
                if (r1.HasValue)
                {
                    HandoverHost.PointerOverride = new Vector2(UnityEngine.Screen.width * 0.5f, 4f); /* an earlier section leaves the mouse mid-screen, and this frame centres on a rival: a hover nobody asked for */ h.TurnSnap(0); h.View.orthographicSize = 14f; h.LookAt(r1.Value); yield return new WaitForSecondsRealtime(0.25f);
                    h.Issue(new Order { Op = "enforcer", Do = "fire", Target = "rival-1" }); yield return Frames(4); yield return new WaitForSecondsRealtime(0.1f);
                    set = HudToolkit.ReticlesShown.Contains("rival-1"); yield return Shoot(h, dir, "r067-reticle-set", log, "active");
                    var a0 = ViewAt(h, "rival-1"); h.RunTo(g.Tick + 15); yield return Settle(h, 0.3f); var a1 = ViewAt(h, "rival-1"); if (a1.HasValue) h.LookAt(a1.Value); yield return Frames(3);
                    follows = HudToolkit.ReticlesShown.Contains("rival-1") && a0.HasValue && a1.HasValue; words = HudToolkit.ReticlesShown; yield return Shoot(h, dir, "r067-reticle-moving", log, "active");
                    log.Add($"r067 reticle: rival-1 moved {(a0.HasValue && a1.HasValue ? F(Flat(a0.Value, a1.Value)) : "?")} m while locked");
                    h.LookAt(h.frame.World(g.S.At(g.S.GuardPost))); yield return Frames(4); edge = HudToolkit.ReticlesShown.Contains(":edge"); yield return Shoot(h, dir, "r067-reticle-edge", log, "active");
                    var r2 = ViewAt(h, "rival-2"); if (r2.HasValue) { h.LookAt(r2.Value); yield return Frames(2); h.Issue(new Order { Op = "enforcer", Do = "fire", Target = "rival-2" }); yield return Frames(3); handoff = HudToolkit.ReticlesShown.Contains("rival-2"); yield return Shoot(h, dir, "r067-reticle-handoff", log, "active"); }
                    HandoverHost.PointerOverride = null; // parked through the edge and hand-off frames as well (Latch 0.2.295's open item: a rival came under the left mouse)
                }
                bool dOk = set && follows;
                ok &= dOk;
                parts.Add($"\"reticle\": {{\"ok\": {B(dOk)}, \"set\": {B(set)}, \"follows\": {B(follows)}, \"edge\": {B(edge)}, \"handoff\": {B(handoff)}, \"shown\": \"{J(words)}\"}}");
                log.Add($"r067 reticle: {(dOk ? "ok" : "FAILED")}: set {set} follows {follows} [{words}] edge {edge} hand-off {handoff}");
            }

            // ---- people (Pulse 067 W2 and §2.5, Vela 067 L): a shift guard in the crowd, hovered once the guard's sight is learned, is "a guard",
            //      a pickable named "Guard", wearing his ring; the relief's step onto the official entrance at 5:30 is walked (no drawn step over
            //      0.25 m a tick); bodies keep their walker's figure (logged); the rival car is Vela's Sedan_Rival
            {
                yield return Fresh(h); var g = h.Game; int hz = Suborned.Sim.Game.Hz;
                h.GiveWatchScripted(OpId.Scout, "guard"); To(h, 5 * 600 + 8 * hz); // brief 096: the shift at 5:30 (+40 s, GDD v0.38.0)
                int eased0 = h.Views.StepEases; h.Views.StepEased.Clear(); var last = new Dictionary<string, Vector3>(); float worst = 0f; string worstKey = "";
                for (int t = 0; t < 6 * hz && !g.Over; t++)
                {
                    h.RunTo(g.Tick + 1);
                    for (int i = 0; i < g.Walkers.Count; i++)
                    {
                        string k = "crowd/" + i; var v = ViewAt(h, k); if (!v.HasValue) { last.Remove(k); continue; }
                        if ((!HostViews.HostSpread || h.Views.StepEased.Contains(k)) && last.TryGetValue(k, out var lp)) { float d = Flat(lp, v.Value); if (d > worst) { worst = d; worstKey = k; } } // the drawn step a tick of an eased person, from the tick before its jump; brief 098 L9 (the spread off): of everyone in the crowd
                        last[k] = v.Value;
                    }
                }
                float paceM = (g.R.CrowdRunSpeed / (float)hz + Suborned.Sim.BodyCheck.RoundCm) / 100f; // the engine's own jump rule for the city's people: their run a tick and its rounding (BodyCheck.Moves)
                int eased = h.Views.StepEases - eased0; bool stepOk = HostViews.HostSpread ? eased == 0 || worst <= 0.25f : eased == 0 && worst <= paceM + 0.001f; // brief 098 L9: nobody jumps as drawn
                // a shift guard on the street now (they leave the official door from 4:30), on screen at play zoom
                To(h, 4 * 600 + 45 * hz); yield return Settle(h); h.Select(OpId.Scout, false);
                string gkey = ""; for (int i = 0; i < g.Walkers.Count && gkey.Length == 0; i++) if (!g.Walkers[i].P.Civilian && ViewAt(h, "crowd/" + i).HasValue) gkey = "crowd/" + i;
                string label = "", name = "", kind = "", cones = ""; bool picked = false, ring = false, sec = false;
                if (gkey.Length > 0 && ViewAt(h, gkey) is Vector3 gp)
                {
                    h.TurnSnap(0); h.View.orthographicSize = 14f; h.LookAt(gp); yield return Frames(3);
                    HandoverHost.PointerWorld = (ViewAt(h, gkey) ?? gp) + Vector3.up * 1.0f; yield return Frames(8); yield return new WaitForSecondsRealtime(0.3f); yield return Frames(4);
                    var hv = h.Hover; kind = hv?.Kind ?? ""; sec = hv?.Security == true; label = hv?.Label ?? ""; name = h.HoverName;
                    picked = h.Pickables().Any(pk => pk.T.Kind == "crowd" && pk.T.Key == gkey && pk.Name.Length > 0);
                    var pid = hv?.Id ?? ""; ring = pid.Length > 0 && h.PersonCones.Exists(c => c.Id == pid && c.Kind == "ring"); cones = HudToolkit.ConesShown;
                    yield return Shoot(h, dir, "r067-crowd-guard-hover", log, "active");
                    HandoverHost.PointerWorld = null;
                }
                var (repicked, bodiesChanged) = h.Views.BodyFigureCheck(); string rivalCar = h.Views.CarModelName("car/rivals");
                bool guardOk = gkey.Length > 0 && kind == "crowd" && sec && label.Contains("guard") && name == "Guard" && picked && ring;
                bool pOk = guardOk && stepOk && bodiesChanged == 0 && rivalCar.Contains("Sedan_Rival");
                ok &= pOk;
                parts.Add($"\"people\": {{\"ok\": {B(pOk)}, \"guard\": {{\"ok\": {B(guardOk)}, \"key\": \"{gkey}\", \"kind\": \"{kind}\", \"security\": {B(sec)}, \"label\": \"{J(label)}\", \"name\": \"{J(name)}\", \"pickable\": {B(picked)}, \"ring\": {B(ring)}, \"cones\": \"{J(cones)}\"}}, \"step\": {{\"ok\": {B(stepOk)}, \"eased\": {eased}, \"worstM\": {F(worst)}, \"worstKey\": \"{worstKey}\"}}, \"bodies\": {{\"repicked\": {repicked}, \"changed\": {bodiesChanged}}}, \"rivalCar\": \"{J(rivalCar)}\"}}");
                log.Add($"r067 people: {(pOk ? "ok" : "FAILED")}: shift guard {gkey} hovered {kind} security {sec} [{label}] name [{name}] pickable {picked} ring {ring} cones [{cones}]; step eased {eased} worst {F(worst)} m a tick ({worstKey}); bodies: {repicked} walkers re-picked by DressCrowd, {bodiesChanged} bodies in another figure; rival car [{rivalCar}]");
            }

            // ---- sidecar (E): the host record beside the session script
            {
                string path = h.SidecarPath; int lines = h.SidecarLines; bool exists = path.Length > 0 && File.Exists(path);
                bool sOk = exists && lines > 0; ok &= sOk;
                parts.Add($"\"sidecar\": {{\"ok\": {B(sOk)}, \"path\": \"{J(path)}\", \"lines\": {lines}}}");
                log.Add($"r067 sidecar: {(sOk ? "ok" : "FAILED")}: {path} ({lines} lines)");
            }
            HandoverHost.PointerWorld = null; HandoverHost.PointerOverride = null; h.PeekLatch = false; h.controls.conversations = keepConvo;
            h.SetPaused(false);
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }

        static int Drawn(string strengths) { int n = 0; foreach (var p in strengths.Split(' ')) { int i = p.LastIndexOf('='); if (i > 0 && float.TryParse(p[(i + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0.05f) n++; } return n; }
        static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a; ab.y = 0f; var ap = p - a; ap.y = 0f; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
            var c = a + ab * t; c.y = p.y; return Flat(p, c);
        }
    }
}
