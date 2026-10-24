using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Engine 0.28/0.29 (brief 054 §3.1, §9.1, §10; Glyph's hud29.spec.json; next-batch item 19): who would notice what.
    /// A civilian's cone shows on hover, only where it matters: the act armed (a picked knob, a hold under way, a ring or panel row, else what a
    /// selected agent carries or holds drawn) is one they care about at the Alert now (Game.CaresAbout), or they are on the armed card's eye
    /// (Quote.WitnessIds, TakedownQuote.People), or an operative they would see is doing a seen act now (Game.NoticesNow). Alt: the armed act's
    /// people, the 6 nearest. Security (Kind ring) draw a ring, the door guard keeps his cone. A dashed near ring (PersonSight.NearCm) rides with a
    /// civilian's wedge while the armed order leaves or drops a body.
    /// The shape is the engine's: Game.PersonCone gives the wedge (At, Facing, Reach, HalfCm) and every sample inside it is the engine's own
    /// point test (Game.NoticesNow for the act), so walls cut it and the stretches behind solids it sees over are its shadows (hatched). Nothing
    /// here computes a sight rule; all reads are read-only (no hash moves). Cost (Redline 064 #1): at most one cone built a frame, the hovered one
    /// first, each build one NoticeProbeOf and its NoticesAt reads; the key quantised to 25 cm and 5 degrees.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>One person's sight as the HUD draws it (world space).</summary>
        public sealed class ConeMark
        {
            public string Id = "", Kind = ""; public float Alpha = 1f; public bool Near; public float NearM; public Vector3 At;
            /// <summary>The outline in order (wedge: the hub, the far points, the hub; ring: closed), and per segment whether it is a cut (a wall
            /// ends the sight short of the reach) rather than the reach or the wedge's sides.</summary>
            public readonly List<Vector3> Outline = new(); public readonly List<bool> Cut = new();
            /// <summary>Dead ground (seen over, not into): cells as quads, hatched; and the edges where a dead cell meets seen ground.</summary>
            public readonly List<Vector3[]> Dead = new(); public readonly List<(Vector3 a, Vector3 b)> DeadEdges = new();
            public string Key = ""; public int BuiltTick = -1;
            /// <summary>Redline 064 #1: the key it was built for and the key it wants now (quantised), its shape and act this frame.</summary>
            public ConeKey KeyQ, Want; public bool Built; public PersonSight Ps; public string Act = "";
        }
        /// <summary>A cone's cache key (no string built a frame).</summary>
        public readonly struct ConeKey : IEquatable<ConeKey>
        {
            public readonly string Kind, Act; public readonly int X, Y, A, Reach, Alert;
            public ConeKey(string kind, int x, int y, int a, int reach, string act, int alert) { Kind = kind; X = x; Y = y; A = a; Reach = reach; Act = act; Alert = alert; }
            public bool Equals(ConeKey o) => X == o.X && Y == o.Y && A == o.A && Reach == o.Reach && Alert == o.Alert && string.Equals(Kind, o.Kind) && string.Equals(Act, o.Act);
            public override bool Equals(object o) => o is ConeKey k && Equals(k);
            public override int GetHashCode() => HashCode.Combine(X, Y, A, Reach, Alert);
        }
        /// <summary>The cones to draw this frame (empty: none).</summary>
        public readonly List<ConeMark> PersonCones = new();
        readonly Dictionary<string, ConeMark> coneCache = new();
        int coneRobin;
        /// <summary>The act the cones were read for ("" none), and whether it leaves a body (the near ring).</summary>
        public string ConeAct { get; private set; } = "";

        /// <summary>The armed order's notice act (rulebook witness.notice), whether it leaves or drops a body, and where it happens. Null when nothing
        /// is armed.</summary>
        (string act, bool body, Pt at, List<string> eyes)? ArmedNotice()
        {
            var g = Game; if (g == null) return null;
            Order o = null; Intent it = null; Target on = null;
            if (Charge is { Shown: true, Fire: false, Plan: not null } ch && ch.Plan.Row(ch.Way)?.I is Intent ci) { it = ci; o = ci.Order; on = ch.T; }
            else if (Charge is { Shown: true, Fire: true } cf) { o = new Order { Do = "fire" }; on = cf.T; }
            else if (Ring is { } rs && rs.Lit >= 0 && rs.Lit < rs.Segs.Count && rs.Segs[rs.Lit].Row?.I is Intent ri) { it = ri; o = ri.Order; on = Ring.Plan?.T; }
            else if (Panel != null && PanelHover >= 0 && PanelHover < Panel.Rows.Count) { it = Panel.Rows[PanelHover].I; o = it?.Order; on = Panel.T; }
            else if (Picked >= 0 && PickedId is string pid && pid.Length > 0) o = new Order { Do = pid == "pressure" ? "turn" : pid, Approach = pid };
            string act = o == null ? "" : NoticeActOf(o); bool body = o != null && (o.Do == "takedown" || o.Do == "drop");
            Pt at = default; bool have = false;
            if (act.Length > 0 && on != null && TargetWorld(on) is Vector3 tw) { at = frame.Engine(tw); have = true; }
            if (act.Length == 0)
            {   // nothing armed: what a selected agent holds now (a body carried, Vale's grip, a drawn weapon)
                for (int k = 0; k < 4 && act.Length == 0; k++)
                {
                    if (!Selected[k] || !Able(k)) continue; var op = g.Ops[k];
                    if (op.CarryBody.Length > 0) act = "carry"; else if (k == (int)OpId.Enforcer && g.Vale == ValeAt.WithEnforcer) act = "escort"; else if (Armed(k) && Drawn(k)) act = "weapon-drawn";
                    if (act.Length > 0) { at = op.Pos; have = true; }
                }
            }
            if (act.Length == 0) return null;
            if (!have && Hover != null && TargetWorld(Hover) is Vector3 hw) { at = frame.Engine(hw); have = true; } // a picked knob with no target yet: the act would happen at the thing under the pointer
            if (!have) { int k = (int)Agent; at = g.Ops[k].Pos; }
            var eyes = new List<string>(); if (it?.Eyes != null) eyes.AddRange(it.Eyes); if (it?.Td != null) foreach (var pe in it.Td.People) eyes.Add(pe.id);
            return (act, body, at, eyes);
        }

        /// <summary>An order as the act a witness would notice (rulebook witness.notice), "" for one nobody does (a Watch, Listen, a call).</summary>
        static string NoticeActOf(Order o) => o.Do switch
        {
            "takedown" => "takedown", "fire" => "weapon-drawn", "carry" or "restrain" or "stash" => "carry", "drop" => "body", "take" => "escort",
            "turn" => o.Approach == "pressure" ? "threat" : "", "move" => o.Run ? "run" : "", _ => "",
        };

        /// <summary>An operative doing a seen act now, as (act, where).</summary>
        IEnumerable<(string act, Pt at)> LiveActs()
        {
            var g = Game;
            for (int k = 0; k < 4; k++)
            {
                var op = g.Ops[k]; if (op.Down || op.Inside || op.InCar) continue;
                if (op.Busy?.Do == "takedown") yield return ("takedown", op.Pos);
                if (op.CarryBody.Length > 0) yield return ("carry", op.Pos);
                if (k == (int)OpId.Enforcer && g.Vale == ValeAt.WithEnforcer) yield return ("escort", op.Pos);
                if (op.Busy?.Do == "turn" && op.Busy.Approach == "pressure") yield return ("threat", op.Pos);
                if (Armed(k) && Drawn(k)) yield return ("weapon-drawn", op.Pos);
                if (doing[k].Phase == "running") yield return ("run", op.Pos);
            }
        }

        /// <summary>The person id under the cursor when it is a civilian or a security person on the street (never the cell, the door guard or Vale).</summary>
        string HoverPersonId()
        {
            var t = Hover; if (t == null) return "";
            if (t.Kind == "crowd") return t.Id ?? "";
            if (t.Kind == "person" && t.Key is "waitress" or "driver") return CastOf(t.Key)?.Id ?? "";
            return "";
        }

        /// <summary>Brief 064 (Redline): measuring only. True puts back the round's first cone and fan costs (an exact cone key, every new cone built
        /// the frame it shows, a point test that finds the person again per sample; fans rebuilt on every fade step, FanMatters every frame), so
        /// the perf probe (-perf064) can time before and after in one build. Never set in play.</summary>
        public static bool PerfLegacy;
        /// <summary>For the perf probe: cones built (BuildCone) and point tests made since the last read.</summary>
        public static int ConeBuilds, ConeQueries;

        readonly List<(string id, string act, float alpha, bool body)> coneWant = new();
        readonly List<(long d, string id)> coneNear = new();
        static readonly string[] ConeCast = { "waitress", "driver" };
        Action conesCore;

        /// <summary>Per frame (after ShowSight): which people's cones show, and their shapes. Redline 064 #1: at most one cone is built a frame
        /// (the hovered one first when its key moved, then round-robin among the rest that need it; a new cone shows as its plain wedge until
        /// its turn), and the key is quantised (25 cm, 5 degrees) so a walker does not rebuild every tick.</summary>
        void PersonConesFrame()
        {
            PersonCones.Clear(); ConeAct = "";
            var g = Game; if (g == null || views == null || Ending.Length > 0 || Following) return;
            Guard("cones", conesCore ??= ConesCore);
        }

        void ConesCore()
        {
            var g = Game;
            var armed = ArmedNotice(); string hid = HoverPersonId();
            var want = coneWant; want.Clear();
            if (hid.Length > 0)
            {
                string act = "";
                if (armed is { } a && (g.CaresAbout(hid, a.act) || a.eyes.Contains(hid))) act = a.act;
                else foreach (var (la, lp) in LiveActs()) if (g.NoticesNow(hid, la, lp)) { act = la; break; }
                if (act.Length > 0) want.Add((hid, act, 1f, armed is { } ab && ab.act == act && ab.body));
            }
            if (Peeking && armed is { } aa)
            {   // Alt: the armed act's people, the 6 nearest (its card's eye first)
                var near = coneNear; near.Clear();
                foreach (var w in g.Walkers) ConeConsider(g, aa, hid, w.P, g.PosOfPerson(w.P));
                foreach (var role in ConeCast) { var p = CastOf(role); if (p != null) ConeConsider(g, aa, hid, p, g.PersonPos(p)); }
                near.Sort((x, y) => x.d.CompareTo(y.d));
                for (int i = 0; i < near.Count && want.Count < (hid.Length > 0 ? 7 : 6); i++) want.Add((near[i].id, aa.act, 0.9f, aa.body));
            }
            if (want.Count == 0) return;
            ConeAct = want[0].act;
            int built = 0, needing = 0; bool hovered = hid.Length > 0;
            for (int i = 0; i < want.Count; i++)
            {
                var (id, act, alpha, body) = want[i];
                var ps = g.PersonCone(id); if (ps.Kind is not ("wedge" or "ring")) continue;
                if (!coneCache.TryGetValue(id, out var cm)) coneCache[id] = cm = new ConeMark { Id = id };
                var key = ConeKeyOf(ps, act, g.Alert);
                bool stale = !cm.Built || !cm.KeyQ.Equals(key);
                if (PerfLegacy)
                {   // the round's first rule: the hovered one, one round-robin, and any never built, all this frame
                    if (stale && (i == 0 || cm.BuiltTick < 0 || i == 1 + coneRobin % Math.Max(1, want.Count - 1))) { BuildCone(cm, ps, act, key); built++; }
                }
                else if (stale)
                {
                    if (i == 0 && hovered && built == 0) { BuildCone(cm, ps, act, key); built++; }
                    else { if (!cm.Built && cm.Outline.Count < 2) PlainCone(cm, ps); needing++; }
                }
                cm.Alpha = alpha; cm.Near = body && ps.Kind == "wedge" && ps.NearCm > 0; cm.NearM = ps.NearCm / 100f;
                cm.Ps = ps; cm.Act = act; cm.Want = key;
                if (cm.Outline.Count >= 2) PersonCones.Add(cm);
            }
            if (PerfLegacy) coneRobin++;
            else if (built == 0 && needing > 0)
            {   // one of the others, round-robin
                int n = PersonCones.Count;
                for (int t = 0; t < n; t++)
                {
                    var cm = PersonCones[(coneRobin + t) % n];
                    if (cm.Built && cm.KeyQ.Equals(cm.Want)) continue;
                    BuildCone(cm, cm.Ps, cm.Act, cm.Want); coneRobin = (coneRobin + t + 1) % Math.Max(1, n); break;
                }
            }
            if (coneCache.Count > 64) { var keep = new HashSet<string>(); foreach (var c in PersonCones) keep.Add(c.Id); var drop = new List<string>(); foreach (var k in coneCache.Keys) if (!keep.Contains(k)) drop.Add(k); foreach (var k in drop) coneCache.Remove(k); }
        }

        /// <summary>Redline 064 #9: Begin's reset of the state this round added (view-only: a restart or Replay starts clean).</summary>
        void ResetRound24()
        {
            Array.Clear(rewalks, 0, rewalks.Length); noticingNow.Clear(); coneCache.Clear(); coneRobin = 0; PersonCones.Clear();
            RerouteReset(); sealSpanFrom = sealSpanLock = -1; noWalkCell = new Pt(int.MinValue, 0); noWalkWas = false; fanMatters.Clear(); fanMattersTick = -1;
        }

        void ConeConsider(Game g, (string act, bool body, Pt at, List<string> eyes) aa, string hid, Person p, Pt? at)
        {
            if (p == null || !at.HasValue || p.Down || p.Id == hid || p == g.GuardP || p == g.ValeP) return;
            if (!(aa.eyes.Contains(p.Id) || g.CaresAbout(p.Id, aa.act))) return;
            long d = G.D2(at.Value, aa.at); if (d > 2000L * 2000L) return;
            coneNear.Add((aa.eyes.Contains(p.Id) ? -1 : d, p.Id));
        }

        /// <summary>A cone's key: its kind, where it stands to 25 cm, its facing to the 5 degree ray step, its reach, the act and the Alert
        /// (Redline 064 #1). PerfLegacy keys it exactly, as the round did.</summary>
        static ConeKey ConeKeyOf(PersonSight ps, string act, int alert)
        {
            if (PerfLegacy) return new ConeKey(ps.Kind, ps.At.X, ps.At.Y, ps.Facing.X * 100000 + ps.Facing.Y, ps.Reach, act, alert);
            int deg = ps.Kind == "ring" ? 0 : Mathf.RoundToInt(Mathf.Atan2(ps.Facing.Y, ps.Facing.X) * Mathf.Rad2Deg / ConeWedgeDeg);
            return new ConeKey(ps.Kind, Mathf.RoundToInt(ps.At.X / 25f), Mathf.RoundToInt(ps.At.Y / 25f), deg, ps.Reach, act, alert);
        }

        /// <summary>A new cone's plain wedge (or ring) at its reach, no engine reads: shown until its build comes round.</summary>
        void PlainCone(ConeMark cm, PersonSight ps)
        {
            cm.Kind = ps.Kind; cm.Outline.Clear(); cm.Cut.Clear(); cm.Dead.Clear(); cm.DeadEdges.Clear();
            bool ring = ps.Kind == "ring"; float reach = Mathf.Max(100f, ps.Reach);
            float a0 = Mathf.Atan2(ps.Facing.Y, ps.Facing.X), half = ring ? Mathf.PI : Mathf.Atan2(ps.HalfCm, reach);
            int n = ring ? 48 : 13; var hub = frame.World(ps.At); cm.At = hub;
            if (!ring) cm.Outline.Add(hub);
            for (int i = 0; i < n; i++) { float ang = ring ? a0 + i * 2f * Mathf.PI / n : a0 - half + 2f * half * i / (n - 1); cm.Outline.Add(frame.World(new Pt(ps.At.X + Mathf.RoundToInt(Mathf.Cos(ang) * reach), ps.At.Y + Mathf.RoundToInt(Mathf.Sin(ang) * reach)))); }
            if (!ring) cm.Outline.Add(hub); else cm.Outline.Add(cm.Outline[0]);
            for (int j = 0; j + 1 < cm.Outline.Count; j++) cm.Cut.Add(false);
        }

        const float ConeStepCm = 50f, ConeWedgeDeg = 5f, ConeRingDeg = 7.5f;

        /// <summary>The cone's shape from the engine's point test: rays from the person at a few degrees apart, sampled every 0.5 m to the reach.
        /// Along a ray the sight ends at the last seen sample (a wall: the outline is cut there, no hatch); an unseen stretch before it is dead
        /// ground behind a solid the eye sees over (hatched, with its own edge).</summary>
        void BuildCone(ConeMark cm, PersonSight ps, string act, ConeKey key)
        {
            var g = Game; cm.KeyQ = key; cm.Built = true; cm.BuiltTick = g.Tick; ConeBuilds++;
            var probe = PerfLegacy ? null : g.NoticeProbeOf(ps.Person); /* Keel's batch read (engine 0.30.0, Redline 064 #1): the person found once for every sample */
            cm.Kind = ps.Kind; cm.Outline.Clear(); cm.Cut.Clear(); cm.Dead.Clear(); cm.DeadEdges.Clear();
            bool ring = ps.Kind == "ring"; float reach = Mathf.Max(100f, ps.Reach);
            float a0 = Mathf.Atan2(ps.Facing.Y, ps.Facing.X), half = ring ? Mathf.PI : Mathf.Atan2(ps.HalfCm, reach) - 0.5f * Mathf.Deg2Rad;
            float stepA = (ring ? ConeRingDeg : ConeWedgeDeg) * Mathf.Deg2Rad;
            int nRays = ring ? Mathf.RoundToInt(2f * Mathf.PI / stepA) : Mathf.Max(2, Mathf.CeilToInt(2f * half / stepA) + 1);
            int nS = Mathf.CeilToInt(reach / ConeStepCm);
            var vis = new bool[nRays, nS]; var last = new int[nRays]; var angs = new float[nRays];
            Pt P(float ang, float r) => new(ps.At.X + Mathf.RoundToInt(Mathf.Cos(ang) * r), ps.At.Y + Mathf.RoundToInt(Mathf.Sin(ang) * r));
            float Rs(int s) => Mathf.Min(reach, (s + 1) * ConeStepCm);
            for (int i = 0; i < nRays; i++)
            {
                float ang = ring ? a0 + i * stepA : a0 - half + 2f * half * i / (nRays - 1); angs[i] = ang; last[i] = -1;
                for (int s = 0; s < nS; s++) { var pt = P(ang, Rs(s)); bool v = PerfLegacy ? g.NoticesNow(ps.Person, act, pt) : probe != null && g.NoticesAt(probe, act, pt); ConeQueries++; vis[i, s] = v; if (v) last[i] = s; }
            }
            var hub = frame.World(ps.At); cm.At = hub;
            Vector3 W(float ang, float r) => frame.World(P(ang, Mathf.Max(30f, r)));
            float End(int i) => last[i] < 0 ? 30f : last[i] == nS - 1 ? reach : Rs(last[i]) + ConeStepCm * 0.5f;
            bool AtReach(int i) => last[i] == nS - 1;
            if (!ring) { cm.Outline.Add(hub); }
            for (int i = 0; i < nRays; i++) cm.Outline.Add(W(angs[i], End(i)));
            if (!ring) cm.Outline.Add(hub); else cm.Outline.Add(cm.Outline[0]);
            for (int j = 0; j + 1 < cm.Outline.Count; j++)
            {
                int i0 = ring ? j : j - 1, i1 = ring ? (j + 1) % nRays : j; // the far points' rays (the wedge's first and last segments are its sides)
                bool side = !ring && (j == 0 || j == cm.Outline.Count - 2);
                cm.Cut.Add(!side && !(AtReach(i0) && AtReach(i1)));
            }
            // dead cells: unseen samples before the last seen one on their ray
            float da = stepA * 0.5f, dr = ConeStepCm * 0.5f;
            bool Dead(int i, int s) { if (ring) i = (i % nRays + nRays) % nRays; else if (i < 0 || i >= nRays) return false; return s >= 0 && s <= last[i] && !vis[i, s]; }
            bool Seen(int i, int s) { if (ring) i = (i % nRays + nRays) % nRays; else if (i < 0 || i >= nRays) return false; return s >= 0 && s <= last[i] && vis[i, s]; }
            for (int i = 0; i < nRays; i++)
                for (int s = 0; s <= last[i]; s++)
                {
                    if (vis[i, s]) continue;
                    float r = Rs(s), rIn = Mathf.Max(30f, r - dr), rOut = Mathf.Min(reach, r + dr), aL = angs[i] - da, aR = angs[i] + da;
                    var q = new[] { W(aL, rIn), W(aL, rOut), W(aR, rOut), W(aR, rIn) }; cm.Dead.Add(q);
                    if (Seen(i, s - 1)) cm.DeadEdges.Add((q[0], q[3]));
                    if (Seen(i, s + 1)) cm.DeadEdges.Add((q[1], q[2]));
                    if (Seen(i - 1, s)) cm.DeadEdges.Add((q[0], q[1]));
                    if (Seen(i + 1, s)) cm.DeadEdges.Add((q[3], q[2]));
                }
        }

        // ---------------------------------------------------------------- the knobs' prediction marks (hud29 predictionMarks.verbPlate)

        /// <summary>Per knob of the selected agent's plate: "likely", "hurt", "ok", "half" or "" (priced when the hover changes).</summary>
        public readonly string[] KnobPred = { "", "", "", "", "" };
        void KnobPredUpdate(Target t)
        {
            for (int i = 0; i < KnobPred.Length; i++) KnobPred[i] = "";
            if (t == null || t.Kind != "person" || t.Key == "councillor" || Agent != OpId.Fixer || !Able((int)Agent) || CastOf(t.Key) is not Person p || p.Turned) return;
            var plate = PlateOf(Agent);
            for (int i = 0; i < plate.Length && i < KnobPred.Length; i++)
            {
                if (plate[i].Dim || plate[i].Id is not ("ask" or "pay" or "help" or "pressure")) continue;
                Quote q = null; if (!Guard("price", () => q = Game.Preview(plate[i].Id, t.Key, OpId.Fixer)) || q == null) continue;
                KnobPred[i] = q.Likely ? (p.KTemperament ? "ok" : "likely") : q.Hurt ? (p.KTemperament ? "half" : "hurt") : "";
            }
        }
    }
}
