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
    /// <summary>Batch 0.2.123's 059 proof (brief 059; Sky on 0.2.122: "rivals just suddenly appeared when near car, they should be coming in from
    /// somewhere when the chime comes"), in the built player, one JSON object `r059`:
    ///  · arrive (gated): a fresh run to 2:35, the rival car drawn from its first tick on the map with nobody of the crew on foot, its approach line
    ///    and the cue `rivals-coming` (Chime) on the rivals.coming tick, the engine loop playing; engine 0.41.0 (briefs/096/waymark/timeline.md):
    ///    at about 2:53 it stops across our car's way out (blocking, nobody on foot before), then three step out and the lookout after them (every
    ///    member first drawn within 5 m of the stopped car, never at a post; the wheelman stays in) and the lookout posts. Pictures
    ///    r059-235-turns-in (the car at its entry, at the quarter yaw where the cell's car is not in front of it), r059-235-square (the opening
    ///    view, the edge chip), r059-253-box-in, r059-300-step-out, r059-lookout-posted.
    ///  · watch: a Watch on the rival car (RivalsWatched), its next-move marker (the lookout's follow while the car blocks); Alt shows the spring
    ///    ring round the cell's stand: r059-watch-alt.
    ///  · pickup (route B's opening, from routes/route-b.json up to the Scout's mislead of the lookout at 4:30): the turned driver drives Vale round
    ///    (r059-613-round, the route and its times), parks in the service bay (r059-625-parked: the wait marker and the hand-off ring); our car,
    ///    boxed in, is refused the Drive into the yard (drive-boxed); the Enforcer takes him from the back seat (r059-take-back-seat: Vale is not
    ///    drawn on the street). With the Tech aboard the getaway car: the yard drawn "after Vale's car" before it parks (r059-yard-after-vale).
    ///    The Drive in its own run before the rivals come: hovered (r059-drive-hover: route preview, the pill) and given; its arrival
    ///    (r059-drive-parked: "Parked"). A Drive back to the stand it is at pops drive-here at the stand (r059-drive-here).
    ///  · fight (not gated, logged): if the final fight starts in the pickup run, every member drawn the tick before is drawn the tick after within
    ///    1.5 m of where he was (no vanish, no fresh crew at the ambush spots).
    /// Every order is the host's own. Pictures go to the run's folder; the HUD manifests with them, for hud-budget.</summary>
    public static class Proof059
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
            log.Add($"{name}: rivals [{HudToolkit.RivalsShown}] stands [{HudToolkit.StandsShown}] vale [{HudToolkit.ValeRouteShown}] windows [{HudToolkit.WindowWords}]");
            h.SetPaused(was);
        }
        static Vector2 OffStreet => new(Screen.width * 0.5f, 4f);
        static IEnumerator Fresh(HandoverHost h) { h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; yield return null; }
        static void To(HandoverHost h, int tick) { var g = h.Game; while (g.Tick < tick && !g.Over) h.RunTo(Math.Min(tick, g.Tick + 50)); }
        static IEnumerator Settle(HandoverHost h, float s = 0.35f) { h.SetPaused(false); yield return new WaitForSecondsRealtime(s); h.SetPaused(true); yield return Frames(3); }
        static List<Order> Route(string name)
        {
            var path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), "contracts", "missions", "handover", "routes", name + ".json");
            var sc = Suborned.Sim.Script.Parse(File.ReadAllText(path));
            return sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
        }
        static void Give(HandoverHost h, Order o)
        {
            var c = new Order { Tick = h.Game.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run };
            if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c);
        }

        /// <summary>A view's renderers' screen rect (top-left origin), empty when hidden.</summary>
        static Rect ScreenRect(HandoverHost h, string key)
        {
            var t = h.Views.Find(key); if (!t || !t.gameObject.activeInHierarchy || !h.View) return Rect.zero;
            bool any = false; float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) continue; var b = r.bounds; any = true;
                for (int i = 0; i < 8; i++) { var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)); var s = h.View.WorldToScreenPoint(c); x0 = Mathf.Min(x0, s.x); x1 = Mathf.Max(x1, s.x); y0 = Mathf.Min(y0, Screen.height - s.y); y1 = Mathf.Max(y1, Screen.height - s.y); }
            }
            return any ? Rect.MinMaxRect(x0, y0, x1, y1) : Rect.zero;
        }

        /// <summary>Looks at the rival car at the first quarter yaw where the cell's car does not cover it (Aster's routing: "the cell's car must not be
        /// in the way"); logs the yaw and the overlap.</summary>
        static IEnumerator FrameRivalCar(HandoverHost h, List<string> log, string name)
        {
            var car = h.Views.Find("car/rivals"); if (!car) yield break;
            int best = 0; float bestOverlap = float.MaxValue;
            for (int k = 0; k < 4; k++)
            {
                h.TurnSnap(k); h.LookAt(car.position); yield return Frames(2);
                var a = ScreenRect(h, "car/rivals"); var b = ScreenRect(h, "car/getaway");
                float ov = a.Overlaps(b) ? Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax)) is var i ? i.width * i.height : 0f : 0f;
                if (ov < bestOverlap) { bestOverlap = ov; best = k; }
                if (ov <= 0f) break;
            }
            h.TurnSnap(best); h.LookAt(car.position); yield return Frames(3);
            log.Add($"{name}: yaw {h.Yaw:0} (quarter {best}), cell's car over the rival car {bestOverlap:0} px²");
        }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true; HandoverHost.PointerOverride = null; HandoverHost.PointerWorld = null;

            // ---- arrive: 2:35 the car turns in, about 2:53 it boxes our car in, the four step out of it, the lookout posts (gated). Engine 0.41.0
            // (Waymark's briefs/096/waymark/timeline.md): no drop at 2:46 and no park at 3:10; the car drives to our stand's block point and stops
            // across its way out, three step out 5 s after (2 s apart) and post round our car, the lookout walks to his post, the wheelman stays in
            {
                yield return Fresh(h); var g = h.Game; int entry = g.CrewEntry, Hz = Suborned.Sim.Game.Hz;
                int carFirst = -1, onFootBefore = 0, cueAt = -1, block = -1; int cues0 = h.CuesPlayed.Count; bool approach = false, loop = false, edge = false;
                var firstAt = new Dictionary<string, (int tick, float dCar, string mode)>(); Pt stopAt = default;
                To(h, entry - 10);
                while (g.Tick < entry + 3 && !g.Over)
                {
                    h.RunTo(g.Tick + 1); yield return null;
                    if (carFirst < 0 && h.Views.Find("car/rivals") is Transform ct && ct.gameObject.activeInHierarchy) carFirst = g.Tick;
                    if (carFirst < 0) foreach (var _ in h.Views.RivalsDrawn()) onFootBefore++;
                }
                if (h.CuesPlayed.Count > cues0) cueAt = entry; // the cue's own line names its clock: logged below
                HandoverHost.PointerOverride = OffStreet; /* an earlier section leaves the mouse mid-screen, and these frames centre on a rival: a hover nobody asked for */ yield return Settle(h, 0.5f); approach = HudToolkit.RivalsShown.Contains("approach"); loop = h.EngineLoopPlaying;
                yield return FrameRivalCar(h, log, "r059-235-turns-in"); yield return Shoot(h, dir, "r059-235-turns-in", log);
                h.TurnSnap(0); h.LookAt(h.GetawayWorld); yield return Frames(4); edge = HudToolkit.RivalsShown.Contains("edge"); yield return Shoot(h, dir, "r059-235-square", log);
                // the box-in: the car stops across our car's way out (nobody on foot before it stops)
                while (g.Tick < entry + 60 * Hz && !g.Over && g.CarSt != Suborned.Sim.CrewCar.Blocking) { h.RunTo(g.Tick + 1); foreach (var _ in h.Views.RivalsDrawn()) onFootBefore++; }
                if (g.CarSt == Suborned.Sim.CrewCar.Blocking) { block = g.Tick; stopAt = g.RivalCarPose()?.pos ?? default; }
                h.RunTo(g.Tick + Hz); yield return Settle(h); string carState = h.Views.RivalCarState; var bc = h.Views.Find("car/rivals"); if (bc) { h.TurnSnap(0); h.LookAt(bc.position); } yield return Frames(3); yield return Shoot(h, dir, "r059-253-box-in", log);
                // the step-out (the pointer stays parked off the street through its frame: Latch 0.2.295's open item, a delta that runs done first left a rival under it): each member's first drawn tick and where (within 5 m of the stopped car, never at a post)
                while (block >= 0 && g.Tick < block + 30 * Hz && !g.Over)
                {
                    h.RunTo(g.Tick + 1);
                    foreach (var (id, at, mode) in h.Views.RivalsDrawn())
                        if (!firstAt.ContainsKey(id)) { var e = g.PosOf(id) ?? h.frame.Engine(at); firstAt[id] = (g.Tick, Suborned.Sim.G.Dist(e, stopAt) / 100f, mode); }
                    if (g.Tick == block + 7 * Hz) { yield return Settle(h); var dc = h.Views.Find("car/rivals"); if (dc) { h.TurnSnap(2); h.LookAt(dc.position); } yield return Frames(3); yield return Shoot(h, dir, "r059-300-step-out", log); }
                }
                HandoverHost.PointerOverride = null;
                // the lookout at his post (Motive RIV-1: by 4:20 on every seed)
                while (block >= 0 && g.Tick < block + 150 * Hz && !(h.Views.RivalModes.TryGetValue("rival-4", out var lm) && lm == "posted") && !g.Over) h.RunTo(g.Tick + 5);
                var lt = h.Views.Find("rival-4"); if (lt) h.LookAt(lt.position); yield return Settle(h); yield return Shoot(h, dir, "r059-lookout-posted", log);
                bool lookout = h.Views.RivalModes.TryGetValue("rival-4", out var lmode) && lmode == "posted";
                // the four who get out are first drawn within 5 m of the stopped car; the wheelman stays in
                bool fourOut = new[] { "rival-1", "rival-2", "rival-3", "rival-4" }.All(firstAt.ContainsKey);
                bool nearCar = firstAt.Where(kv => kv.Key != "rival-5").All(kv => kv.Value.dCar <= 5f);
                string cueWords = string.Join(",", h.CuesPlayed.Skip(cues0));
                bool aOk = carFirst >= 0 && Math.Abs(carFirst - entry) <= 1 && onFootBefore == 0 && cueWords.Contains("rivals-coming") && approach && block >= 0 && carState == "blocking" && fourOut && nearCar && lookout;
                ok &= aOk;
                string fa = string.Join(", ", firstAt.Select(kv => $"\"{kv.Key}\": {{\"at\": \"{HandoverHost.MissionClock(kv.Value.tick)}\", \"fromCarM\": {F(kv.Value.dCar)}, \"mode\": \"{kv.Value.mode}\"}}"));
                parts.Add($"\"arrive\": {{\"ok\": {B(aOk)}, \"entry\": \"{HandoverHost.MissionClock(entry)}\", \"carFirst\": \"{(carFirst < 0 ? "never" : HandoverHost.MissionClock(carFirst))}\", \"onFootBeforeBlock\": {onFootBefore}, \"cues\": \"{J(cueWords)}\", \"audio\": \"{h.AudioSourceOf}\", \"loop\": {B(loop)}, \"approach\": {B(approach)}, \"edgeChip\": {B(edge)}, \"block\": \"{(block < 0 ? "never" : HandoverHost.MissionClock(block))}\", \"car\": \"{carState}\", \"first\": {{{fa}}}, \"fourOut\": {B(fourOut)}, \"nearCar\": {B(nearCar)}, \"lookout\": {B(lookout)}}}");
                log.Add($"r059 arrive: {(aOk ? "ok" : "FAILED")}: entry {HandoverHost.MissionClock(entry)} car first {carFirst} on foot before the block {onFootBefore}; cues [{cueWords}] ({h.AudioSourceOf}) loop {loop}; approach {approach} edge {edge}; block {(block < 0 ? "never" : HandoverHost.MissionClock(block))} car {carState}; first [{fa}]; lookout {lookout}");

                // ---- watch: the timetable, the marker, the spring ring with Alt
                h.GiveWatchScripted(OpId.Scout, "car:rivals");
                for (int i = 0; i < 120 * Suborned.Sim.Game.Hz && !g.RivalsWatched && !g.Over; i += 5) h.RunTo(g.Tick + 5);
                yield return Settle(h); var wc = h.Views.Find("car/rivals"); if (wc) h.LookAt(wc.position);
                HandoverHost.PointerOverride = OffStreet; h.PeekLatch = true; yield return new WaitForSecondsRealtime(0.3f); yield return Frames(6);
                string pip = HudToolkit.WindowWords, marks = HudToolkit.RivalsShown; yield return Shoot(h, dir, "r059-watch-alt", log, "alt"); h.PeekLatch = false; HandoverHost.PointerOverride = null;
                bool wOk = g.RivalsWatched && pip.Contains("rivals-next") && marks.Contains("spring");
                ok &= wOk;
                parts.Add($"\"watch\": {{\"ok\": {B(wOk)}, \"watched\": {B(g.RivalsWatched)}, \"at\": \"{HandoverHost.MissionClock(g.Tick)}\", \"windows\": \"{J(pip)}\", \"marks\": \"{J(marks)}\"}}");
                log.Add($"r059 watch: {(wOk ? "ok" : "FAILED")}: watched {g.RivalsWatched} at {HandoverHost.MissionClock(g.Tick)}; windows [{pip}]; marks [{marks}]");
            }

            // ---- pickup: route B's opening (the driver turned, the false tip), Vale driven round, the Take from the back seat; the Drive
            {
                yield return Fresh(h); var g = h.Game; List<Order> rb = null; string why = "";
                try { rb = Route("route-b"); } catch (Exception e) { why = e.Message; }
                bool turned = false, driving = false, parked = false, takeOk = false, yardShut = false, hoverRoute = false, driveTaken = false, arrived = false, herePop = false, boxed = false;
                string roundMarks = "", parkMarks = "", pill = "", stands = "", hereWords = "", parkedChip = "", boxedWhy = "", driveAt = "";
                int fightAt = -1; var jumps = new List<string>();
                if (rb != null)
                {
                    foreach (var o in rb) { if (o.Tick > 2400 || g.Over) break; if (o.Tick > g.Tick) To(h, o.Tick); Give(h, o); } // the opening: the listen, the loop, the pay
                    To(h, 2400); h.GiveScripted(new Order { Op = "tech", Do = "board", Target = "car" }); // the Tech aboard the getaway car (for the Drive): as a click gives it (engine 0.33.1: Board is within 1 m of the car's body, so he walks beside it first)
                    for (int i = 0; i < 40 * Suborned.Sim.Game.Hz && !g.Ops[(int)OpId.Tech].InCar && !g.Over; i++) h.RunTo(g.Tick + 1);
                    foreach (var o in rb) { if (o.Tick <= 2400 || o.Op == "tech") continue; if (o.Tick > 2700 || g.Over) break; if (o.Tick > g.Tick) To(h, o.Tick); Give(h, o); } // engine 0.41.0: the Scout misleads the lookout (4:30), else he follows Popovic's car and reaches him first
                    To(h, Math.Max(g.Tick, 2800)); turned = g.Driver.Turned;
                    // the yard "after Vale's car": the Tech selected aboard (4:40, before Vale's car parks: brief 080, the shift a minute earlier)
                    h.Select(OpId.Tech, false); yield return Settle(h); h.LookAt(h.frame.World(g.S.StandById("service-drive")?.CarAt ?? g.CarPos())); h.TurnSnap(2); yield return Frames(4);
                    stands = HudToolkit.StandsShown; yardShut = stands.Contains("service-drive:after-vale"); yield return Shoot(h, dir, "r059-yard-after-vale", log);
                    int leave = Suborned.Sim.Game.T(g.S.ValeAtCar) + Suborned.Sim.Game.T(g.R.ValeInS);
                    To(h, leave + 2 * Suborned.Sim.Game.Hz); yield return Settle(h); driving = h.ValeRoute.Driving;
                    var oc = h.Views.Find("car/official"); if (oc) h.LookAt(oc.position); yield return Frames(3); roundMarks = HudToolkit.ValeRouteShown; yield return Shoot(h, dir, "r059-613-round", log);
                    for (int i = 0; i < 60 * Suborned.Sim.Game.Hz && !g.ValeCarParked && !g.Over; i++) h.RunTo(g.Tick + 1);
                    parked = g.ValeCarParked; h.RunTo(g.Tick + Suborned.Sim.Game.Hz); yield return Settle(h); oc = h.Views.Find("car/official"); if (oc) h.LookAt(oc.position); yield return Frames(3);
                    parkMarks = HudToolkit.ValeRouteShown + " | " + HudToolkit.WindowWords; yield return Shoot(h, dir, "r059-625-parked", log);
                    // engine 0.41.0: the rival car has boxed ours in since about 2:53, so the Drive into the yard after the park is refused drive-boxed,
                    // naming their car (GDD §05 rivals; Keel's BOX-2); the Drive itself is proven in its own run before they come, below
                    var st = g.S.StandById("service-drive");
                    if (st != null) { int n0 = g.Orders.Count; bool given = h.Issue(new Order { Op = "tech", Do = "drive-car", Target = "service-drive" }); var ans = g.Orders.Count > n0 ? g.Orders[^1] : default; boxed = !given && g.Orders.Count > n0 && ans.code == "drive-boxed" && g.CurStand != st; boxedWhy = g.Orders.Count > n0 ? $"{ans.code}: {ans.why}" : "no order"; }
                    // the Take from the back seat: the Enforcer beside the car (its rear door), then take
                    var vp = g.ValePos(); var e = g.Ops[(int)OpId.Enforcer];
                    if (vp.HasValue && g.ValeCarParked)
                    {
                        var to = g.S.Grid.NearestFree(vp.Value); if (to.HasValue && Suborned.Sim.G.Dist(e.Pos, vp.Value) > g.R.ActReachCm - 50) { h.Issue(new Order { Op = "enforcer", Do = "move", Point = to }); for (int i = 0; i < 40 * Suborned.Sim.Game.Hz && e.Walk != null && !g.Over; i++) h.RunTo(g.Tick + 1); }
                        takeOk = h.Issue(new Order { Op = "enforcer", Do = "take" }); h.RunTo(g.Tick + 5 * Suborned.Sim.Game.Hz); yield return Settle(h);
                        var oc2 = h.Views.Find("car/official"); if (oc2) h.LookAt(oc2.position); yield return Frames(3);
                        bool valeHidden = !(h.Views.Find("councillor") is Transform vt && vt.gameObject.activeInHierarchy);
                        log.Add($"r059 take: given {takeOk}, Vale {g.Vale}, drawn on the street {!valeHidden}"); takeOk &= valeHidden || g.Vale == ValeAt.WithEnforcer;
                        yield return Shoot(h, dir, "r059-take-back-seat", log);
                    }
                    // the fight (not gated): every member drawn the tick before its start is drawn after it within 1.5 m
                    var before = new Dictionary<string, Vector3>();
                    for (int i = 0; i < 180 * Suborned.Sim.Game.Hz && !g.Over && g.FightStart >= int.MaxValue / 8; i++)
                    {
                        if (g.Vale == ValeAt.WithEnforcer && e.Walk == null && e.Busy == null && !e.InCar) h.Issue(new Order { Op = "enforcer", Do = "board", Target = "car" });
                        before.Clear(); foreach (var (id, at, _) in h.Views.RivalsDrawn()) before[id] = at;
                        h.RunTo(g.Tick + 1); h.Views.Frame(1f);
                    }
                    if (g.FightStart < int.MaxValue / 8)
                    {
                        fightAt = g.FightStart; var after = h.Views.RivalsDrawn().ToDictionary(x => x.id, x => x.at);
                        foreach (var kv in before) if (!after.TryGetValue(kv.Key, out var a2)) jumps.Add(kv.Key + ":vanished"); else if ((a2 - kv.Value).magnitude > 1.5f) jumps.Add($"{kv.Key}:{(a2 - kv.Value).magnitude:0.0}m");
                        foreach (var kv in after) if (!before.ContainsKey(kv.Key)) jumps.Add(kv.Key + ":appeared");
                        yield return Settle(h); h.LookAt(h.GetawayWorld); yield return Frames(3); yield return Shoot(h, dir, "r059-fight-start", log, "active"); // brief 080: Vale just taken: the goal line changes here, so this is no resting frame
                    }
                }
                // the Drive, in its own run before the rivals turn in (our car not yet boxed, the driver not turned so the yard is open; Keel's
                // BOX-2 drives there the same way): hovered (the route preview, the pill), given, arrived; a Drive to the stand it is at pops drive-here
                yield return Fresh(h); g = h.Game; var ds = g.S.StandById("service-drive");
                if (ds != null)
                {
                    h.GiveScripted(new Order { Op = "tech", Do = "board", Target = "car" });
                    for (int i = 0; i < 60 * Suborned.Sim.Game.Hz && !g.Ops[(int)OpId.Tech].InCar && !g.Over; i++) h.RunTo(g.Tick + 1);
                    h.Select(OpId.Tech, false); HandoverHost.PointerWorld = h.frame.World(ds.CarAt) + Vector3.up * 0.3f; h.LookAt(h.frame.World(ds.CarAt)); yield return Frames(6); yield return new WaitForSecondsRealtime(0.3f);
                    pill = h.HoverIntent != null ? $"{h.HoverIntent.Label} · {h.HoverIntent.Time}" : ""; hoverRoute = HudToolkit.StandsShown.Contains("service-drive:hover:route");
                    yield return Shoot(h, dir, "r059-drive-hover", log, "active"); HandoverHost.PointerWorld = null;
                    driveAt = HandoverHost.MissionClock(g.Tick); driveTaken = h.Issue(new Order { Op = "tech", Do = "drive-car", Target = "service-drive" });
                    for (int i = 0; i < 60 * Suborned.Sim.Game.Hz && g.GetawayDriving && !g.Over; i++) h.RunTo(g.Tick + 1);
                    arrived = g.CurStand == ds && g.Tick < g.CrewEntry; h.RunTo(g.Tick + 2); yield return Settle(h, 0.4f); h.LookAt(h.GetawayWorld); yield return Frames(3);
                    parkedChip = h.LastChip != null ? string.Join(",", h.LastChip.Lines.ConvertAll(x => x.Word)) : ""; yield return Shoot(h, dir, "r059-drive-parked", log, "active");
                    int pops0 = h.Pops.Count; h.Issue(new Order { Op = "tech", Do = "drive-car", Target = "service-drive" }, null, null); yield return Frames(3);
                    herePop = h.Pops.Count > pops0 && h.Pops[^1].Icon.Contains("car"); hereWords = h.Pops.Count > 0 ? h.Pops[^1].Text : ""; yield return Shoot(h, dir, "r059-drive-here", log, "active");
                }
                bool pOk = rb != null && turned && driving && parked && boxed && takeOk && yardShut && hoverRoute && pill.Contains("Drive") && driveTaken && arrived && herePop;
                ok &= pOk;
                parts.Add($"\"pickup\": {{\"ok\": {B(pOk)}, \"route\": \"{(rb != null ? "route-b to 4:30" : J(why))}\", \"turned\": {B(turned)}, \"driving\": {B(driving)}, \"round\": \"{J(roundMarks)}\", \"parked\": {B(parked)}, \"parkMarks\": \"{J(parkMarks)}\", \"boxed\": {B(boxed)}, \"boxedWhy\": \"{J(boxedWhy)}\", \"take\": {B(takeOk)}, \"yardShut\": {B(yardShut)}, \"stands\": \"{J(stands)}\", \"pill\": \"{J(pill)}\", \"hoverRoute\": {B(hoverRoute)}, \"drive\": {B(driveTaken)}, \"driveAt\": \"{driveAt}\", \"arrived\": {B(arrived)}, \"chip\": \"{J(parkedChip)}\", \"herePop\": {B(herePop)}, \"hereWords\": \"{J(hereWords)}\", \"fight\": \"{(fightAt < 0 ? "none" : HandoverHost.MissionClock(fightAt))}\", \"jumps\": \"{J(string.Join(" ", jumps))}\"}}");
                log.Add($"r059 pickup: {(pOk ? "ok" : "FAILED")}: turned {turned}; round {driving} [{roundMarks}]; parked {parked} [{parkMarks}]; boxed {boxed} [{boxedWhy}]; take {takeOk}; yard shut {yardShut} [{stands}]; hover [{pill}] route {hoverRoute}; drive {driveTaken} at {driveAt} arrived {arrived} chip [{parkedChip}]; drive-here pop {herePop} [{hereWords}]; fight {(fightAt < 0 ? "none" : HandoverHost.MissionClock(fightAt))} jumps [{string.Join(" ", jumps)}]");
            }
            h.SetPaused(false);
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }
    }
}
