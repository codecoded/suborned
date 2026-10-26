using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 059 K6 in the host (GDD v0.25.0 §10 "Drive"; Glyph 059 board 04 and "the shut stand and Drive's refusals", 2026-10-26; Sky's 1a):
    /// the getaway car's Drive between its stands, and Vale's car driven round the back by a turned driver (board 03).
    ///  · With the getaway car or an agent in it selected (an able selected agent aboard, or the car's ring or panel open), every stand of the
    ///    site's getaway.stands shows a footprint on the ground (its car place, its facing; the stand it is on under the car, no disc). A stand
    ///    is a target ("stand", its id): its one way is Drive (`drive-car`, the agent in the car), priced by the engine's DriveTicks, $0, 0 T,
    ///    no Alert; hovered, the route preview along the drive grid (the engine's own CarRoute between the stands' cells) and the pill
    ///    "Drive · 28 s"; the hold commits it as every hold does. No ghosts once the final fight has started (drive-fight).
    ///  · The yard "after Vale's car" (Glyph 2026-10-26 §1-§3): with the driver turned and Vale's car not yet parked, the stand next to the
    ///    service bay draws shut-looking (50 %, short dashes, a plate of nextevent and Vale's cameo; the dashes crawl while his car drives round)
    ///    and stays orderable (the engine refuses nothing there: an early car is a price, K5). At car.parks it opens.
    ///  · drive-held: every stand but the car's is struck (45 %), the holders pulse as for Go. Refusals pop at the stand (PopIcon's drive-*).
    ///  · Vale's car (board 03): with the driver turned, hovered (the car or the driver), or while it drives round: the dashed cream route from
    ///    the official bay to the service bay (site car.servicePath), the times plate "6:13 → 6:25" (the engine's tick for Vale in, and its
    ///    CarInBayTick once it drives; before, the route's ticks at car.pace as the engine counts them), the hand-off ring at its rear door.
    /// Everything reads the engine and the site; orders are engine orders, so a session replays as played.</summary>
    public sealed partial class HandoverHost
    {
        public sealed class StandGhost
        {
            public string Id = ""; public Stand St; public Vector3 At, Fwd; public bool Here, Shut, Hover, Struck, Crawl; public float OpenedAt = -10f;
            public readonly List<Vector3> Route = new();
        }
        public sealed class DriveMarks { public bool On, Live; public readonly List<StandGhost> Ghosts = new(); public string Driver = ""; public float LiveK; /** brief 067 C5a: 0 rest, 1 live, eased 0.25 s */ }
        public readonly DriveMarks Drive = new();
        public sealed class ValeRouteMarks
        {
            public bool On, Driving, Parked, AtMouth, HandOff, Plate; public string From = "", To = ""; public Vector3 PlateAt, HandOffAt, TakeDisc;
            public readonly List<Vector3> Route = new();
        }
        public readonly ValeRouteMarks ValeRoute = new();
        Transform standRoot; readonly Dictionary<string, (Transform t, BoxCollider c)> standCols = new();
        readonly Dictionary<(string, string), List<Vector3>> standRoutes = new();
        bool valeParkedSeen; float valeParkedAt = -10f;

        /// <summary>The stand ids of the site in order (square first), for the proof.</summary>
        public List<string> StandIds() { var l = new List<string>(); if (Game != null) foreach (var s in Game.S.Stands) l.Add(s.Id); return l; }

        /// <summary>The final fight has started (the engine's FightStart): no Drive, no ghosts.</summary>
        bool FightStarted => Game != null && Game.FightStart < int.MaxValue / 8;

        /// <summary>The agent who would drive: the first selected able agent in the car, else (the car's ring or panel open) the first aboard; -1 none.</summary>
        public int DriverAgent()
        {
            var g = Game; if (g == null) return -1;
            for (int k = 0; k < 4; k++) if (Selected[k] && g.Ops[k].InCar && !g.Ops[k].Down) return k;
            for (int k = 0; k < 4; k++) if (g.Ops[k].InCar && !g.Ops[k].Down) return k; // brief 067 C1.4 (Glyph): else the first aboard (the car answers with no selection)
            return -1;
        }

        /// <summary>Brief 067 C5a (Glyph): the stands draw live (full, the centre disc, the hovered route) while an aboard agent is selected, the car
        /// or a stand is hovered, the car's ring or panel is open, a Drive wedge is lit or hovered, or the pill or a hold is on a Drive; else at rest.</summary>
        bool StandsLive()
        {
            var g = Game; for (int k = 0; k < 4; k++) if (Selected[k] && g.Ops[k].InCar && !g.Ops[k].Down) return true;
            if (Hover != null && (IsGetaway(Hover) || Hover.Kind == "stand")) return true;
            if (Ring?.Plan?.T is { } rt && (IsGetaway(rt) || rt.Kind == "stand") || Panel?.T is { } pt && (IsGetaway(pt) || pt.Kind == "stand")) return true;
            return DriveNamed() != null;
        }
        /// <summary>The stand a Drive names now: the ring's hovered (else lit) Drive wedge, the hold's charge on a Drive, or the pill's offer of one.</summary>
        string DriveNamed()
        {
            if (Ring is { } rs) { int i = rs.Hover >= 0 ? rs.Hover : rs.Lit; if (i >= 0 && i < rs.Segs.Count && rs.Segs[i].Row?.I?.Order is Order ro && ro.Do == "drive-car") return ro.Target; }
            if (Charge is { Done: false, Fire: false, Plan: not null } c && c.Plan.Row(c.Way)?.I?.Order is Order co && co.Do == "drive-car") return co.Target;
            if (HoverIntent?.Order is Order ho && ho.Do == "drive-car" && HoverIntent.Actionable) return ho.Target;
            return null;
        }

        /// <summary>The yard is "after Vale's car" (Glyph 2026-10-26 §1): the stand by the service bay while the turned driver has not parked there yet.</summary>
        public bool StandAfterVale(Stand st)
        {
            var g = Game; if (g == null || st == null || st == g.S.Stands[0] || !g.Driver.Turned || g.Driver.Down || g.ValeCarParked || g.Over) return false;
            if (g.Vale is ValeAt.WithEnforcer or ValeAt.Car) return false;
            Pt bay; try { bay = g.S.CarStand(g.S.CarService); } catch (Exception) { return false; }
            return G.Within(st.CarAt, bay, 1500);
        }

        /// <summary>Drive's price for the line (the engine's DriveTicks, seconds rounded up).</summary>
        Intent DriveIntent(OpId by, Stand st)
        {
            var g = Game; int ticks = -1; Guard("drive ticks", () => ticks = g.DriveTicks(st.Id));
            var i = new Intent { Slot = -1, Label = "Drive", Order = new Order { Op = Op.Name(by), Do = "drive-car", Target = st.Id } };
            i.Time = ticks < 0 ? "" : $"{(ticks + Game.Hz - 1) / Game.Hz} s"; i.Usd = "$0"; i.Trail = "T 0"; i.Alert = "A 0";
            i.Outcome = StandAfterVale(st) ? "after Vale's car" : "";
            return i;
        }

        /// <summary>The engine's DriveCar refusals, mirrored for the card in its order (the order still asks the engine).</summary>
        Answer DriveLock(OpId by, Stand st)
        {
            var g = Game; var op = g.Ops[(int)by];
            Answer No(string code, string why, string ids = "") => new Answer { ok = false, code = code, why = why, m = -1, cm = -1, usd = -1, tick = -1, ids = ids };
            if (!g.R.DriveCarOn) return No("unknown-verb", "no Drive in this rulebook");
            if (!op.InCar) return No("drive-no-driver", $"the {Op.Name(by)} is not in the getaway car: board it first");
            if (FightStarted) return No("drive-fight", "the final fight has started: fight it out here, or Go");
            foreach (var x in g.Ops) if (x.InCar && x.Busy != null) return No("drive-busy", $"the {Op.Name(x.Id)} in the car is busy");
            if (st == g.CurStand && !g.GetawayDriving) return No("drive-here", "the car is there already");
            List<string> hs = null; Guard("car holders", () => hs = g.CarHolders()); if (hs != null && hs.Count > 0) return No("drive-held", "the car is held", string.Join(",", hs));
            return new Answer { ok = true, code = "", why = "" };
        }

        /// <summary>A stand's panel rows: Drive by the agent who would drive (BuildPanel).</summary>
        void StandRows(ActionPanel pn, List<int> order, Target t)
        {
            var g = Game; var st = g.S.StandById(t.Key); if (st == null) return;
            int k = DriverAgent(); if (k < 0) foreach (var o in order) if (g.Ops[o].InCar) { k = o; break; }
            if (k < 0 && order.Count > 0) k = order[0];
            if (k < 0) return;
            var v = DriveIntent((OpId)k, st); Words(v, t); v.Name = "Drive"; var lk = DriveLock((OpId)k, st);
            pn.Rows.Add(new PanelRow { By = (OpId)k, Slot = -1, I = v, Verb = v.Name, Lock = lk, Reason = lk.ok ? "" : ShortOf(v.Order, lk) ?? lk.why, Available = lk.ok, Unlock = lk.ok ? "" : LockLine(v.Order, lk) ?? "" });
        }

        /// <summary>A refused Drive's pop sits at the stand's footprint (Glyph 2026-10-26 §5), not at the cursor.</summary>
        Vector3? StandWorld(string id) { var st = Game?.S.StandById(id); return st == null ? null : frame.World(st.CarAt); }

        /// <summary>The car's route from one stand to another on the drive grid (the engine's own CarRoute between their cells), cached.</summary>
        List<Vector3> StandRoute(Stand from, Stand to)
        {
            if (standRoutes.TryGetValue((from.Id, to.Id), out var l)) return l;
            l = new List<Vector3>(); Pt[] r = null; Guard("drive route", () => r = Game.S.Drive.CarRoute(from.CarCell, to.CarCell, Game.S.DriveLaneCm));
            foreach (var q in r ?? new[] { from.CarCell, to.CarCell }) l.Add(frame.World(q));
            standRoutes[(from.Id, to.Id)] = l; return l;
        }

        /// <summary>The stands' click targets: one box a stand under the view root, live only while the ghosts show.</summary>
        void StandColliders(bool on)
        {
            var g = Game; if (g == null || viewRoot == null) return;
            if (standRoot == null || standRoot.parent != viewRoot) { standRoot = new GameObject("stands").transform; standRoot.SetParent(viewRoot, false); standCols.Clear(); standRoutes.Clear(); }
            foreach (var st in g.S.Stands)
            {
                if (!standCols.TryGetValue(st.Id, out var sc))
                {
                    var go = new GameObject("stand/" + st.Id); go.transform.SetParent(standRoot, false);
                    var c = go.AddComponent<BoxCollider>(); c.size = new Vector3(1.9f, 0.6f, 4.6f); c.center = new Vector3(0f, 0.3f, 0f);
                    var at = frame.World(st.CarAt); go.transform.position = at;
                    var f = frame.World(new Pt(st.CarAt.X + st.Facing.X, st.CarAt.Y + st.Facing.Y)) - at; f.y = 0f; if (f.sqrMagnitude > 1e-6f) go.transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
                    standCols[st.Id] = sc = (go.transform, c);
                }
                bool live = on && !(st == g.CurStand && !g.GetawayDriving); // the stand under the car is never a target (drive-here)
                if (sc.t.gameObject.activeSelf != live) sc.t.gameObject.SetActive(live);
            }
        }

        /// <summary>Per frame, after the views: the ghosts and Vale's car's route.</summary>
        void DriveMarksFrame()
        {
            var d = Drive; d.On = false; d.Live = false; d.Ghosts.Clear(); d.Driver = "";
            var g = Game; if (g == null || g.Range || views == null || Ending.Length > 0 || g.Over) { StandColliders(false); ValeRoute.On = false; return; }
            int drv = DriverAgent();
            d.On = g.R.DriveCarOn && g.S.Stands.Count > 1 && !FightStarted && g.AnyInCar; // brief 067 C1.4 (Glyph): whenever anyone is aboard (Vale counts), not only with an aboard agent selected
            d.Live = d.On && StandsLive(); var named = d.On ? DriveNamed() : null;
            StandColliders(d.On);
            // the yard opening (Glyph §2): the tick Vale's car parks
            if (g.ValeCarParked && !valeParkedSeen) valeParkedAt = Time.unscaledTime; valeParkedSeen = g.ValeCarParked;
            if (d.On)
            {
                d.Driver = drv >= 0 ? Op.Name((OpId)drv) : "";
                List<string> holders = null; Guard("car holders", () => holders = g.CarHolders()); bool held = holders != null && holders.Count > 0;
                bool valeDriving = g.Driver.Turned && g.Vale == ValeAt.InCar && !g.PickupService && g.CarInBayTick >= 0;
                foreach (var st in g.S.Stands)
                {
                    var gh = new StandGhost { Id = st.Id, St = st, At = frame.World(st.CarAt) };
                    var f = frame.World(new Pt(st.CarAt.X + st.Facing.X, st.CarAt.Y + st.Facing.Y)) - gh.At; f.y = 0f; gh.Fwd = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
                    gh.Here = st == g.CurStand && !g.GetawayDriving;
                    gh.Shut = !gh.Here && StandAfterVale(st); gh.Crawl = gh.Shut && valeDriving;
                    gh.Struck = !gh.Here && held;
                    gh.Hover = Hover != null && Hover.Kind == "stand" && Hover.Key == st.Id || named == st.Id; // brief 067 C5b-c: a hovered or lit Drive wedge, a Drive charge or the car's Drive pill light its stand
                    gh.OpenedAt = StandAfterValeOnce(st) ? valeParkedAt : -10f;
                    if (gh.Hover && !gh.Here && !gh.Struck) gh.Route.AddRange(StandRoute(g.CurStand, st));
                    d.Ghosts.Add(gh);
                }
            }
            d.LiveK = Mathf.MoveTowards(d.LiveK, d.Live ? 1f : 0f, Time.unscaledDeltaTime / 0.25f);
            ValeRouteFrame();
        }

        /// <summary>The yard stand was "after Vale's car" in this run (so its opening pulse plays once, at car.parks).</summary>
        bool StandAfterValeOnce(Stand st)
        {
            var g = Game; if (st == null || st == g.S.Stands[0] || !g.Driver.Turned) return false;
            Pt bay; try { bay = g.S.CarStand(g.S.CarService); } catch (Exception) { return false; }
            return G.Within(st.CarAt, bay, 1500) && g.ValeCarParked;
        }

        /// <summary>Board 03: Vale's car driven round the back by the turned driver.</summary>
        void ValeRouteFrame()
        {
            var v = ValeRoute; v.On = v.Driving = v.Parked = v.AtMouth = v.HandOff = v.Plate = false; v.Route.Clear();
            var g = Game; if (g == null || g.Range || !g.Driver.Turned || g.Driver.Down || g.Over) return;
            if (g.Vale is ValeAt.WithEnforcer or ValeAt.Car or ValeAt.Office) return;
            v.Parked = g.ValeCarParked; v.AtMouth = g.CarAtMouth;
            v.Driving = g.Vale == ValeAt.InCar && !g.PickupService && g.CarInBayTick >= 0;
            bool hov = Hover != null && (Hover.Kind == "car" && Hover.Key == "official" || Hover.Kind == "person" && Hover.Key is "driver" or "councillor") || Drive.Ghosts.Exists(x => x.Hover && x.Shut);
            var path = g.S.CarServicePath; if (path == null || path.Length < 2) return;
            // the route: from the bay (or the car as it drives) to the service bay; never once parked
            if (!v.Parked && (hov || v.Driving))
            {
                v.On = true; var full = new List<Vector3>(); foreach (var q in path) full.Add(frame.World(q));
                var carT = views.Find("car/official");
                if (v.Driving && carT && carT.gameObject.activeInHierarchy) Trim(carT.position, full, v.Route); else v.Route.AddRange(full);
                int leave = Game.T(g.S.ValeAtCar) + Game.T(g.R.ValeInS), arrive = g.CarInBayTick >= 0 ? g.CarInBayTick : leave + DriveProfileTicks(path);
                bool shutHover = Drive.Ghosts.Exists(x => x.Hover && x.Shut);
                v.From = MissionClock(leave); v.To = MissionClock(arrive); v.Plate = shutHover ? g.Driver.KRoutine : hov; // Glyph 2026-10-26 §3: on a shut stand's hover only once the driver's routine is known
                v.PlateAt = full[full.Count / 2];
            }
            // the hand-off ring: where the Enforcer takes him from the back seat (parked; or hovered, at the route's end)
            if (v.Parked || v.On)
            {
                Pt carAt = v.Parked ? g.CarPos() : path[^1]; Pt? head = null; Guard("car heading", () => head = g.CarHeading());
                var c = frame.World(carAt); var fw = head is Pt h && (h.X != 0 || h.Y != 0) ? Flat(frame.World(new Pt(carAt.X + h.X, carAt.Y + h.Y)) - c) : Flat(frame.World(path[^1]) - frame.World(path[^2]));
                if (fw.sqrMagnitude < 1e-6f) fw = Vector3.forward; fw.Normalize(); var right = Vector3.Cross(Vector3.up, fw);
                Vector3 door = g.S.HasPlace(g.S.ValeService) ? frame.World(g.S.At(g.S.ValeService)) : c + right; // the door side: toward the service door
                float side = Vector3.Dot(door - c, right) >= 0f ? 1f : -1f;
                v.HandOff = true; v.HandOffAt = c - fw * 0.3f + right * (1.5f * side); v.TakeDisc = v.HandOffAt + right * (1.05f * side);
            }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>The engine's car.pace profile mirrored (Game.CarProfile, read only): ticks to drive a route, each tick its cell's pace.</summary>
        int DriveProfileTicks(Pt[] pts)
        {
            var p = new Sim.Path(pts); var g = Game; long at = 0; int n = 0;
            while (at < p.Length && n < 100000) { int pace = g.S.Drive.SurfaceAt(p.At(at)) == 'r' ? g.R.CarRoadSpeed : g.R.CarYardSpeed; at = Math.Min(p.Length, at + Math.Max(1, pace)); n++; }
            return n;
        }

        /// <summary>A stand target from its view name ("stand/&lt;id&gt;").</summary>
        Target StandTarget(string n)
        {
            var id = n[6..]; var st = Game?.S.StandById(id); if (st == null) return null;
            return new Target { Kind = "stand", Key = id, Id = id, Label = Text?.TargetName("stand-" + id, "the " + id) ?? id };
        }
    }
}
