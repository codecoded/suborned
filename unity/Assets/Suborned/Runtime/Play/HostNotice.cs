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
    /// here computes a sight rule; all reads are read-only (no hash moves). Cost: one cone rebuilt a frame, round-robin, the hovered one first.</summary>
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

        /// <summary>Per frame (after ShowSight): which people's cones show, and their shapes (one rebuilt a frame).</summary>
        void PersonConesFrame()
        {
            PersonCones.Clear(); ConeAct = "";
            var g = Game; if (g == null || views == null || Ending.Length > 0 || Following) return;
            Guard("cones", () =>
            {
                var armed = ArmedNotice(); string hid = HoverPersonId();
                var want = new List<(string id, string act, float alpha, bool body)>();
                if (hid.Length > 0)
                {
                    string act = "";
                    if (armed is { } a && (g.CaresAbout(hid, a.act) || a.eyes.Contains(hid))) act = a.act;
                    else foreach (var (la, lp) in LiveActs()) if (g.NoticesNow(hid, la, lp)) { act = la; break; }
                    if (act.Length > 0) want.Add((hid, act, 1f, armed is { } ab && ab.act == act && ab.body));
                }
                if (Peeking && armed is { } aa)
                {   // Alt: the armed act's people, the 6 nearest (its card's eye first)
                    var near = new List<(long d, string id)>();
                    void Consider(Person p, Pt? at) { if (p == null || !at.HasValue || p.Down || p.Id == hid || p == g.GuardP || p == g.ValeP) return; if (!(aa.eyes.Contains(p.Id) || g.CaresAbout(p.Id, aa.act))) return; long d = G.D2(at.Value, aa.at); if (d > 2000L * 2000L) return; near.Add((aa.eyes.Contains(p.Id) ? -1 : d, p.Id)); }
                    foreach (var w in g.Walkers) Consider(w.P, g.PosOfPerson(w.P));
                    foreach (var role in new[] { "waitress", "driver" }) { var p = CastOf(role); if (p != null) Consider(p, g.PersonPos(p)); }
                    near.Sort((x, y) => x.d.CompareTo(y.d));
                    for (int i = 0; i < near.Count && want.Count < (hid.Length > 0 ? 7 : 6); i++) want.Add((near[i].id, aa.act, 0.9f, aa.body));
                }
                if (want.Count == 0) return;
                ConeAct = want[0].act;
                // rebuild: the hovered one whenever its key moves, then one other a frame, round-robin
                int robin = want.Count > 1 ? 1 + coneRobin++ % (want.Count - 1) : -1;
                for (int i = 0; i < want.Count; i++)
                {
                    var (id, act, alpha, body) = want[i];
                    var ps = g.PersonCone(id); if (ps.Kind is not ("wedge" or "ring")) continue;
                    string key = $"{ps.Kind}|{ps.At.X},{ps.At.Y}|{ps.Facing.X},{ps.Facing.Y}|{ps.Reach}|{act}|{g.Alert}";
                    if (!coneCache.TryGetValue(id, out var cm)) coneCache[id] = cm = new ConeMark { Id = id };
                    if (cm.Key != key && (i == 0 || i == robin || cm.BuiltTick < 0)) BuildCone(cm, ps, act, key);
                    if (cm.Outline.Count < 2) continue;
                    cm.Alpha = alpha; cm.Near = body && ps.Kind == "wedge" && ps.NearCm > 0; cm.NearM = ps.NearCm / 100f;
                    PersonCones.Add(cm);
                }
                if (coneCache.Count > 64) { var keep = new HashSet<string>(); foreach (var c in PersonCones) keep.Add(c.Id); var drop = new List<string>(); foreach (var k in coneCache.Keys) if (!keep.Contains(k)) drop.Add(k); foreach (var k in drop) coneCache.Remove(k); }
            });
        }

        const float ConeStepCm = 50f, ConeWedgeDeg = 5f, ConeRingDeg = 7.5f;

        /// <summary>The cone's shape from the engine's point test: rays from the person at a few degrees apart, sampled every 0.5 m to the reach.
        /// Along a ray the sight ends at the last seen sample (a wall: the outline is cut there, no hatch); an unseen stretch before it is dead
        /// ground behind a solid the eye sees over (hatched, with its own edge).</summary>
        void BuildCone(ConeMark cm, PersonSight ps, string act, string key)
        {
            var g = Game; cm.Key = key; cm.BuiltTick = g.Tick; cm.Kind = ps.Kind; cm.Outline.Clear(); cm.Cut.Clear(); cm.Dead.Clear(); cm.DeadEdges.Clear();
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
                for (int s = 0; s < nS; s++) { bool v = g.NoticesNow(ps.Person, act, P(ang, Rs(s))); vis[i, s] = v; if (v) last[i] = s; }
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
