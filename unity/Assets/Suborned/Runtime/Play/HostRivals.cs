using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 059 §2 in the host (GDD v0.25.0 §10 "the rivals"; Glyph 059 boards 01 and 02; Sky on 0.2.122: "they should be coming in from
    /// somewhere when the chime comes"): what the HUD draws of the rival crew, read from the engine each frame and never sent back.
    ///  · the approach: while the car drives, its next leg as a thin violet line on the road from its nose to its next stop (the drive grid's own
    ///    route to the stop RivalNext names, fetched once a leg, trimmed from the car as it drives), the stop's disc; an edge pointer chip when the
    ///    car is off screen. Drawn from the car's first frame on the map (2:35), with no Watch: it is a car driving past;
    ///  · the drop: each member walking to his post, a dotted violet line from where he is along his walk to its end, and the post's ring at the
    ///    end (the post ring as built: solid 0.55 m, dashed 0.95 m); posted, the ring stays under him (the lookout's on the far kerb);
    ///  · after a Watch on them (RivalsWatched): the window marker over their car (the next move, its seconds, the underline draining over the
    ///    leg), the spring ring round the cell's current stand (the rulebook's fight.safe.ambush) on hover of the car or a posted member, or Alt,
    ///    and while the follow is still possible a dotted violet line with chevrons from their car to Vale's car.
    /// The engine reads: RivalPoses (through the views), RivalNext, RivalsWatched, CrewOf(id).Walk (a member's walk, read only for its line),
    /// the site's drive grid, CurStand. Nothing is typed in: every point is the engine's or the site's.</summary>
    public sealed partial class HandoverHost
    {
        public sealed class RivalMarks
        {
            public bool On, CarShown, Moving, Watched, Spring, Follow, Edge; public Vector3 Car, Stop, SpringAt, FollowTo; public float SpringR;
            public readonly List<Vector3> Approach = new();
            public readonly List<(List<Vector3> walk, Vector3 post, bool ring)> Walks = new();
            public readonly List<Vector3> Posts = new();
        }
        public readonly RivalMarks Rivals = new();
        readonly List<Vector3> rivalLeg = new(); (string state, int tick, Pt stop) rivalLegKey; bool rivalLegHave;
        readonly Dictionary<string, List<Vector3>> walkPool = new();

        /// <summary>A rival target's point: the car (car:rivals) or a member by his id.</summary>
        Pt? RivalPos(Target t) => t.Key == "car:rivals" ? Game.RivalCarPose()?.pos : Game.PosOf(t.Key);

        /// <summary>The engine's MisleadRefusal mirrored for the card (the order still asks the engine): the car parked at the park with its wheelman
        /// aboard, the crew not drawn by gunfire, no final fight.</summary>
        public bool MisleadOpen()
        {
            var g = Game; if (g == null || g.Range || views == null) return false;
            if (views.RivalCarState != "parked" || g.FightBranch.Length > 0 || g.RivalsDrawn) return false;
            if (!views.RivalModes.TryGetValue("rival-5", out var wm) || wm != "aboard") return false;
            var cp = g.RivalCarPose(); return cp.HasValue && g.S.HasPlace(g.S.RivalsPark) && G.Within(cp.Value.pos, g.S.CarStand(g.S.RivalsPark), 300);
        }

        /// <summary>The rival car's next move (RivalNext), guarded; null when none.</summary>
        (string kind, int tick, Pt at)? RivalNextOf(string id) { (string, int, Pt)? r = null; Guard("rival next", () => r = Game.RivalNext(id)); return r; }

        static readonly HashSet<string> CarMovingStates = new() { "entering", "rounding", "tail", "tipout", "tipback", "tostand", "front" };

        /// <summary>Per frame, after the views moved.</summary>
        void RivalMarksFrame()
        {
            var m = Rivals; m.On = m.CarShown = m.Moving = m.Watched = m.Spring = m.Follow = m.Edge = false; m.Approach.Clear(); m.Walks.Clear(); m.Posts.Clear();
            var g = Game; if (g == null || g.Range || views == null || Ending.Length > 0) return;
            var carT = views.Find("car/rivals"); m.CarShown = carT && carT.gameObject.activeInHierarchy;
            m.Watched = g.RivalsWatched;
            if (m.CarShown) m.Car = carT.position;
            m.On = m.CarShown || views.RivalModes.Count > 0;
            if (!m.On) { rivalLegHave = false; return; }
            // the approach: while it drives, to the stop RivalNext names (one route a leg)
            var st = views.RivalCarState;
            if (m.CarShown && CarMovingStates.Contains(st) && RivalNextOf("car:rivals") is { kind: "stops" } nx)
            {
                m.Moving = true; var key = (st, g.CarStTick, nx.at);
                if (!rivalLegHave || !key.Equals(rivalLegKey))
                {
                    rivalLegKey = key; rivalLegHave = true; rivalLeg.Clear();
                    var from = g.RivalCarPose()?.pos ?? nx.at; Pt[] route = null;
                    Guard("rival route", () => route = g.S.Drive.CarRoute(g.S.Drive.NearestFree(from) ?? from, nx.at, g.S.DriveLaneCm));
                    if (route == null || route.Length < 2) route = new[] { from, nx.at };
                    foreach (var q in route) rivalLeg.Add(frame.World(q));
                }
                m.Stop = frame.World(nx.at);
                Trim(m.Car, rivalLeg, m.Approach);
                if (cam) { var vp = cam.WorldToViewportPoint(m.Car); m.Edge = vp.z <= 0 || vp.x < 0 || vp.x > 1 || vp.y < 0 || vp.y > 1; }
            }
            else rivalLegHave = false;
            // the walks to the posts, and the posts held
            foreach (var (id, at, mode) in views.RivalsDrawn())
            {
                if (mode == "posted") { var pg = Goal(id); if (pg is "" or "spot" or "lookout") m.Posts.Add(at); continue; }
                if (mode != "walking") continue;
                CrewMember cm = null; Guard("crew", () => cm = g.CrewOf(id)); var w = cm?.Walk; if (w == null || w.Points.Length < 2) continue;
                if (!walkPool.TryGetValue(id, out var full)) walkPool[id] = full = new List<Vector3>();
                full.Clear(); foreach (var q in w.Points) full.Add(frame.World(q));
                var line = new List<Vector3>(); Trim(at, full, line);
                bool post = cm.Goal is "spot" or "lookout";
                m.Walks.Add((line, full[^1], post));
            }
            // after a Watch: the spring ring round the cell's stand (hover of the car or a posted member, or Alt), the follow line
            if (m.Watched)
            {
                bool hov = Hover != null && Hover.Kind == "rivals" && (Hover.Key == "car:rivals" || views.RivalModes.TryGetValue(Hover.Key, out var hm) && hm == "posted");
                if ((hov || Peeking) && g.R.SafeAmbushCm > 0 && g.FightBranch.Length == 0) { m.Spring = true; m.SpringAt = GetawayWorld; m.SpringR = g.R.SafeAmbushCm / 100f; }
                if (m.CarShown && RivalNextOf("car:rivals") is { kind: "follows" } && views.Find("car/official") is Transform oc && oc.gameObject.activeInHierarchy) { m.Follow = true; m.FollowTo = oc.position; }
            }
        }

        string Goal(string id) { string s = ""; Guard("crew", () => s = Game.CrewOf(id)?.Goal ?? ""); return s; }

        /// <summary>`full` from the point on it nearest `from` onward, starting at `from` (the line never trails what walks it).</summary>
        static void Trim(Vector3 from, List<Vector3> full, List<Vector3> into)
        {
            into.Clear(); into.Add(from); if (full.Count == 0) return;
            int seg = 1; float best = float.MaxValue; var pq = new Vector2(from.x, from.z);
            for (int i = 1; i < full.Count; i++)
            {
                var a = new Vector2(full[i - 1].x, full[i - 1].z); var b = new Vector2(full[i].x, full[i].z); var ab = b - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(pq - a, ab) / ab.sqrMagnitude); float d = (a + ab * t - pq).sqrMagnitude;
                if (d < best) { best = d; seg = i; }
            }
            for (int i = seg; i < full.Count; i++) into.Add(full[i]);
            if (into.Count < 2) into.Add(full[^1]);
        }

        /// <summary>Brief 059's window markers (HostDone.WindowsRead appends them): the rivals' next move over their car once watched (Glyph board
        /// 02); Vale's car parked round the back with the countdown to the close (board 03), the fixed close before it parks while its route shows.</summary>
        void RivalWindowMarks()
        {
            var g = Game; if (g == null || g.Range || g.Over) return;
            if (g.RivalsWatched && RivalNextOf("car:rivals") is { } nx && nx.tick >= 0 && nx.tick > g.Tick)
            {
                string sk = "rivals-next|" + nx.kind; if (!windowSpan.TryGetValue(sk, out var sp) || sp.until != nx.tick) windowSpan[sk] = sp = (nx.tick, Math.Max(1, nx.tick - g.Tick));
                WindowMarks.Add(new WindowMark { Kind = "rivals-next", Thing = nx.kind, Key = "car/rivals", Until = nx.tick, Left = Math.Max(0, (nx.tick - g.Tick + Game.Hz - 1) / Game.Hz), Total = Math.Max(1, (sp.span + Game.Hz - 1) / Game.Hz) });
            }
            int close = Game.T(g.S.Over);
            if (g.Driver.Turned && !g.Driver.Down && g.ValeCarParked && close > g.Tick)
            {
                int span = Math.Max(1, close - Math.Max(0, g.CarInBayTick));
                WindowMarks.Add(new WindowMark { Kind = "vale-car", Thing = "official", Key = "car/official", Until = close, Left = Math.Max(0, (close - g.Tick + Game.Hz - 1) / Game.Hz), Total = Math.Max(1, (span + Game.Hz - 1) / Game.Hz) });
            }
            else if (ValeRoute.On && close > g.Tick) WindowMarks.Add(new WindowMark { Kind = "vale-car", Thing = "official", Key = "car/official", Until = close, Left = -1, Total = -1, Label = MissionClock(close) });
        }
    }
}
