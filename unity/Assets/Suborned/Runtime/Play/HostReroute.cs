using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 057 §2 (Sky 2026-10-24 on the Tech's Reroute: "not clear what we're rerouting here"; then "yeah I like seeing the route of a
    /// patrol bot, just make it thin, and when rerouting it, see the new route when it's in play and make sure robot follows it"): what Reroute
    /// moves, drawn in the world while its way is hovered or lit, and kept drawn while the reroute is in play (PatrolAwayUntil), cleared when the
    /// patrol returns. The sweep's ends and the wait spot are the site's (patrol-a, patrol-b, patrol-reroute), never typed in. The route is the
    /// engine's: Game.PatrolLegs (engine 0.30.0: the walk out, the wait and the walk back while rerouted); the hover's preview, before
    /// any reroute, is the walk grid's own route from where it stands to the wait spot (Grid.Route, the path finder the walk uses). Reads only.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>What the HUD draws this frame: none, the hover's preview, or the reroute in play.</summary>
        public sealed class RerouteMarks
        {
            public bool On, Hover, Live, FromEngine; public Vector3 A, B, Wait, Patrol; public readonly List<Vector3> Route = new();
        }
        public readonly RerouteMarks Reroute = new();
        Pt lastSweepAt; bool haveSweep; int rerouteKeyTick = -1; string rerouteKey = "";

        /// <summary>The engine's reroute path (engine 0.30.0, Keel: Game.PatrolLegs, read by name so this compiles on 0.29): while the patrol is on
        /// false orders, the points of its legs out (the walk to the wait spot), wait and back (the walk to its sweep), in walk order; else null.</summary>
        Pt[] EngineRoute()
        {
            if (Game == null) return null; PatrolPlan plan = null;
            if (!Guard("patrol legs", () => plan = Game.PatrolLegs()) || plan == null || plan.Mode != "away") return null;
            var pts = new List<Pt>();
            foreach (var leg in plan.Legs) { if (leg.Kind is not ("out" or "wait" or "back" or "meet")) continue; foreach (var q in leg.Points) if (pts.Count == 0 || pts[^1] != q) pts.Add(q); }
            return pts.Count >= 2 ? pts.ToArray() : null;
        }

        /// <summary>Per tick-frame: whether a Reroute is being looked at (its ring wedge, panel row, the hold's offer or charge) or is in play.</summary>
        void RerouteFrame()
        {
            var m = Reroute; m.On = m.Hover = m.Live = m.FromEngine = false; var g = Game; if (g == null || Ending.Length > 0) return;
            Guard("reroute", () =>
            {
                var pose = g.PatrolPose(); bool away = g.PatrolAwayUntil > g.Tick && !g.PatrolDown;
                if (pose.HasValue && !away && !g.PatrolChasing) { lastSweepAt = pose.Value.pos; haveSweep = true; }
                bool Is(Intent i) => i?.Order?.Do == "reroute";
                m.Hover = Ring is { } rs && (rs.Hover >= 0 ? rs.Hover : rs.Lit) is int ri && ri >= 0 && ri < rs.Segs.Count && Is(rs.Segs[ri].Row?.I)
                    || Panel != null && PanelHover >= 0 && PanelHover < Panel.Rows.Count && Is(Panel.Rows[PanelHover].I)
                    || Ring == null && Panel == null && Is(HoverIntent) || Charge is { Fire: false, Plan: not null } ch && Is(ch.Plan.Row(ch.Way)?.I);
                m.Live = away; m.On = (m.Hover || m.Live) && pose.HasValue && !g.PatrolDown;
                if (!m.On) return;
                var S = g.S; m.A = frame.World(S.Stand(S.PatrolA)); m.B = frame.World(S.Stand(S.PatrolB)); m.Wait = frame.World(S.Stand(S.PatrolReroute));
                m.Patrol = Views?.Find("patrol") is Transform pt && pt.gameObject.activeInHierarchy ? pt.position : frame.World(pose.Value.pos);
                var er = EngineRoute(); m.FromEngine = er != null && er.Length >= 2;
                Pt from = m.Live && haveSweep ? lastSweepAt : pose.Value.pos, to = S.Stand(S.PatrolReroute);
                string key = m.FromEngine ? "e" + er.Length + ":" + er[0].X + "," + er[0].Y + ":" + er[^1].X : $"g{from.X},{from.Y}>{to.X},{to.Y}";
                if (key == rerouteKey && Math.Abs(g.Tick - rerouteKeyTick) < 30) return;
                rerouteKey = key; rerouteKeyTick = g.Tick; m.Route.Clear();
                var pts = m.FromEngine ? er : S.Grid.Route(from, to);
                if (pts == null || pts.Length < 2) pts = new[] { from, to };
                foreach (var q in pts) m.Route.Add(frame.World(q));
            });
        }

        /// <summary>The proof's measure: how far a world point lies from the drawn route (m, on the ground), or -1 with no route.</summary>
        public float OffRoute(Vector3 p) => OffRoute(p, Reroute.Route);
        public static float OffRoute(Vector3 p, List<Vector3> r)
        {
            if (r == null || r.Count < 2) return -1f; float best = float.MaxValue; var q = new Vector2(p.x, p.z);
            for (int i = 1; i < r.Count; i++)
            {
                var a = new Vector2(r[i - 1].x, r[i - 1].z); var b = new Vector2(r[i].x, r[i].z); var ab = b - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, (a + ab * t - q).magnitude);
            }
            return best;
        }
    }
}
