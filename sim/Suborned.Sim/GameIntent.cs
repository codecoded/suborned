// Hostile intent, per agent (engine 0.19.0; Pulse, briefs/027/pulse/hostile-initiative.md, with Sky's answers 2026-10-21; rulebook ai.intent).
// Sky: "each character has their own history, scout for example is stealth - shouldn't be punished if enforcer goes in wild." So before the
// final fight a hostile fires on an agent for what that agent is seen doing, or for its own known face; never for another agent's acts, and
// never for the Alert alone (Sky's answer 2: Alert 3 is not "anyone in a cone").
//  · Kinds: rivals (the crew, the ambush), robots (the patrol, the response's robots), police (everyone else armed).
//  · Foes (a closed set, ai.intent): drawn, fired, vale, violence (security saw it), face (security knows the face), seen (the crew saw it act).
//  · Police: their foes, the face from intent-face-alert (2); a drawn weapon seen at Alert 0..intent-warn-max-alert before any shot gets the
//    drop-it warning (the lab's, promoted for police); from intent-close-alert (3) a police body that sees a foe beyond its ring closes in on
//    the sighting by fight.kite's close-in. Robots: their foes, the face from Alert 2, or at any Alert at wanted or infamous Heat (Pulse §2.3, Sky's answer 4). Rivals: they watch the street from their
//    arrival (Sky's answer 3); calm, a drawn weapon or the Vale holder; alerted (drawn by gunfire or Alert 2), also an agent they saw act, and
//    the drawn crew presses for the Vale holder.
//  · A face is known when security saw the agent draw or do violence or warned it, or a witness's report landed that remembers the agent
//    doing violence or drawing. Loitering (attention) is a found fault, never a face.
//  · Nobody draws for the cell (Sky's answer 1): the host shows who is under fire (UnderFireOf, GameWeaponsFree.cs).
// The final fight (ai.intent.final "every") and the range are as before: every operative is a foe there.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        readonly SortedSet<string> faces = new SortedSet<string>(StringComparer.Ordinal);     // operatives whose face security knows (ai.intent face)
        readonly SortedSet<string> rivalsSaw = new SortedSet<string>(StringComparer.Ordinal); // operatives the rival crew saw act (ai.intent seen)
        /// <summary>Operatives whose face security knows this mission (ai.intent face). Read-only.</summary>
        public IReadOnlyCollection<string> KnownFaces => faces;
        /// <summary>Operatives the rival crew saw act this mission (ai.intent seen). Read-only.</summary>
        public IReadOnlyCollection<string> RivalsSawAct => rivalsSaw;
        static readonly string[] FaceActs = { "saw-takedown", "saw-carry", "saw-violence", "saw-gunfire", "saw-weapon-drawn" };

        /// <summary>A hostile body's intent kind (ai.intent): rivals, robots (the patrol, the response's robots), police (everyone else armed).</summary>
        string IntentKind(string id)
        {
            if (id == "rivals" || IsRival(id)) return "rivals";
            if (id == "patrol") return "robots";
            foreach (var u in Units) if (u.Id == id && (u.Robot || u.Patrol)) return "robots";
            return "police";
        }
        bool RivalsAlert => RivalsDrawn || Alert >= R.Intent.RivalsAlertedAt;
        /// <summary>The Alert from which a kind fires on a known face: police intent-face-alert; robots the same, or 0 while the cell's Heat band
        /// is one of robots.faceHeat (wanted, infamous); rivals never (they go by what they saw).</summary>
        int FaceAt(string kind) => kind == "police" ? R.Intent.PoliceFace : kind == "robots" ? (Array.IndexOf(R.Intent.RobotFaceHeat, HeatBand) >= 0 ? 0 : R.Intent.RobotFace) : 9;

        /// <summary>Does body `id` count operative `op` a foe before the final fight (ai.intent)? Its sight and reach are the caller's.</summary>
        bool Provoked(string id, Op op)
        {
            var ir = R.Intent; string kind = IntentKind(id);
            string[] foes = kind == "police" ? ir.PoliceFoes : kind == "robots" ? ir.RobotFoes : RivalsAlert ? ir.RivalsAlerted : ir.RivalsCalm;
            foreach (var f in foes) if (HasFoe(f, op)) return true;
            return Alert >= FaceAt(kind) && faces.Contains(Sim.Op.Name(op.Id));
        }
        bool HasFoe(string f, Op op)
        {
            string name = Sim.Op.Name(op.Id);
            switch (f)
            {
                case "drawn": return op.Gun.Drawn && !Warned(op); // under a warning nobody fires on the draw yet (the window is the point of it)
                case "fired": return op.Gun.Fired;
                case "vale": return op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer;
                case "violence": return markedFoes.Contains(name);
                case "face": return faces.Contains(name);
                case "seen": return rivalsSaw.Contains(name);
            }
            throw new InvalidOperationException($"ai.intent: '{f}' is not a foe"); // Rules rejects it at load
        }

        /// <summary>Security now knows this operative's face (ai.intent face).</summary>
        void KnowFace(string name) { if (IsOpId(name)) faces.Add(name); }
        /// <summary>A witness's report landed (ReportsStep): the faces it remembers doing violence or drawing are known.</summary>
        void FacesFromReport(Person p) { foreach (var m in p.Memory) if (m.Witnessed && Array.IndexOf(FaceActs, m.Act) >= 0) KnowFace(m.By); }

        // ---------------------------------------------------------------- the rival crew's eyes (Sky's answer 3)

        /// <summary>The rival crew's cone on the street (perception.rivals): from their arrival with ai.intent.rivals.street, else once drawn
        /// (RivalsWatching). Its own sight ignores people, as RivalsWatching does.</summary>
        public bool RivalsStreetWatching(Pt p)
        {
            if (!RivalsDrawn && !R.Intent.RivalsStreet) return false;
            var r = RivalsPose(); if (!r.HasValue) return false;
            return G.InCone(r.Value.pos, r.Value.facing, R.RivReachCm, R.RivHalfCm, near, p) && Sees(r.Value.pos, p);
        }
        bool RivalsStreetOn => RivalsPresent && (RivalsDrawn || (R.Intent.RivalsStreet));

        /// <summary>Once a fight tick, before the bodies act: what the crew sees each agent do (a weapon out, a takedown, a carry).</summary>
        void IntentTick()
        {
            if (!RivalsStreetOn || RivalsGun.Down) return;
            foreach (var op in Ops)
            {
                if (op.Down || op.Inside || op.InCar) continue;
                bool acts = op.Gun.Drawn || op.Busy?.Do == "takedown" || op.CarryBody.Length > 0 || op.Carrying.HasValue;
                if (acts && RivalsStreetWatching(op.Pos)) rivalsSaw.Add(Sim.Op.Name(op.Id));
            }
        }
        /// <summary>A shot landed (Resolve): the crew sees an operative that fires in its cone.</summary>
        void IntentShot(string by, Pt from) { if (IsOpId(by) && RivalsStreetOn && !RivalsGun.Down && RivalsStreetWatching(from)) rivalsSaw.Add(by); }

        // ---------------------------------------------------------------- engaging by face (the guard, the patrol)

        /// <summary>An operative `id`'s body would fire on by its face alone, that it sees now inside its effective reach (the guard, the patrol not yet engaged).</summary>
        Op? FaceFoe(string id, Gun g, Pt pos, Func<Pt, bool> sees)
        {
            if (g.W == null || faces.Count == 0) return null;
            if (Alert < FaceAt(IntentKind(id))) return null;
            long eff = (long)g.W.EffCm * g.W.EffCm;
            foreach (var op in Ops)
            {
                if (op.Down || op.Inside || op.InCar || !faces.Contains(Sim.Op.Name(op.Id))) continue;
                if (G.D2(pos, op.Pos) <= eff && sees(op.Pos)) return op;
            }
            return null;
        }
        /// <summary>The door guard or the patrol, not yet engaged, sees an agent whose face is known at its rung: it engages him (CityBody).</summary>
        void IntentEngage()
        {
            if (faces.Count == 0) return;
            if (!GuardEngaged && !GuardDown && !GuardAlly && GuardPose() is var gp && gp.HasValue && FaceFoe("guard", GuardGun, gp.Value.pos, p => GuardWatching(p, false)) is Op go)
            {
                if (gp.Value.phase != "door" && !GuardHeld) { GuardHeld = true; heldPos = gp.Value.pos; heldFacing = gp.Value.facing; heldPhase = "drawing on the cell"; GuardAwayUntil = INF; }
                GuardEngaged = true; GuardGun.NoFoeSince = -1;
            }
            if (!PatrolChasing && !PatrolDown && PatrolAwayUntil <= Tick && PatrolUnit() == null && PatrolPose() is var pp && pp.HasValue && FaceFoe("patrol", PatrolGun, pp.Value.pos, p => PatrolWatching(p, false)) is Op po)
            {
                chasePos = pp.Value.pos; PatrolChasing = true; chaseOp = po.Id; patrolOrigin = po.Pos; patrolReturning = false; PatrolGun.NoFoeSince = -1;
                Note("patrol.breaks", "The patrol knows the face: it breaks its route and turns on the cell.");
            }
        }

        // ---------------------------------------------------------------- police close in on a sighting (from intent-close-alert)

        /// <summary>From intent-close-alert, a police body (the response unit before it joins, a police unit) that sees a foe beyond its
        /// effective ring marks it as fight.kite's shooter, so the kite's close-in (KiteBody) walks it in on the sighting; the mark is kept fresh
        /// while it still sees him and lapses by kite.lapse once it does not.</summary>
        void IntentSight(Gun g, Pt pos, Func<Pt, bool> sees)
        {
            if (Alert < R.Intent.PoliceCloseIn || SecurityLevel < R.Intent.PoliceCloseLevel || g.W == null || g.Down) return;
            if (g.KiteAt >= 0 && g.KiteBy.Length > 0 && !OpOf(Sim.Op.Parse(g.KiteBy)).Down && sees(OpOf(Sim.Op.Parse(g.KiteBy)).Pos)) { g.KiteSeen = Math.Max(g.KiteSeen, Tick); return; }
            if (g.KiteAt >= 0) return; // an alarm of its own (a shot) runs its course
            long eff = (long)g.W.EffCm * g.W.EffCm; string who = ""; long bd = long.MaxValue;
            foreach (var op in Ops)
            {
                if (op.Down || op.Inside || op.InCar || !Provoked(g.Id, op) || !sees(op.Pos)) continue;
                long d = G.D2(pos, op.Pos); if (d <= eff) continue;
                if (d < bd) { bd = d; who = Sim.Op.Name(op.Id); }
            }
            if (who.Length == 0) return;
            KiteMark(g, who);
            Note("ai.kite", $"{g.Id} sees the {who} at {G.Isqrt(bd) / 100} m and closes in.");
        }

        // ---------------------------------------------------------------- the drawn crew presses for the Vale holder

        /// <summary>The drawn crew, while the Enforcer holds Vale on the street, walks to its stop point of him (ai.stopReach of its effective
        /// ring), re-planned each kite.replan; once he no longer holds Vale it holds where it stands (it never walks back). True while the press
        /// owns the crew's walk (KiteRivals then does nothing else).</summary>
        bool IntentPress()
        {
            if (!R.Intent.RivalsPress) return false;
            var g = RivalsGun; if (!RivalsDrawn || !RivalsPresent || g.Down || g.W == null) return false;
            bool pressing = rivalsDisp != null && rivalsDisp.Press;
            var e = Enforcer;
            if (Vale != ValeAt.WithEnforcer || e.Inside || e.Down) return pressing;
            if (pressing && Tick - rivalsDisp!.Start < Kr.KiteReplan) return true;
            var pose = RivalsPose(); if (!pose.HasValue) return pressing;
            var pos = pose.Value.pos; var to = StopPoint(pos, e.Pos, g.W.EffCm);
            var path = to == pos ? null : KitePath(pos, to, int.MaxValue / 20);
            if (!pressing) Note("ai.kite", "The rivals see Vale taken and press for the Enforcer.");
            rivalsDisp = new KiteDisp { P = path ?? new Path(new[] { pos, pos }), Start = Tick, Face = Facing(path?.End ?? pos, e.Pos), Press = true };
            return true;
        }

        // ---------------------------------------------------------------- the police warning in today (Pulse §2.1: the lab's, for police)

        /// <summary>Does this security watcher give the drop-it warning in today: police (the guard, the response unit, a police unit; never a
        /// camera, the patrol or a robot), at Alert up to warn.maxAlert, before any shot and outside the final fight, to an agent with a weapon
        /// out and no target (no fire order, lock, focus or trigger: one that is about to fire gets no beat).</summary>
        bool TodayWarns(string w, Op op)
        {
            if (Lab || Gunfire > 0 || CellFiredOn || FightBranch.Length > 0 || Alert > R.Intent.WarnMaxAlert) return false;
            if (op.Gun.Order.Length > 0 || op.Gun.TrigT.Length > 0) return false; // a drawn weapon only (Pulse §2.1): an agent already given a target is about to fire, no beat
            if (w == "guard") return !GuardDown;
            if (w == "security") return true;
            foreach (var u in Units) if (u.Id == w) return !u.Robot && !u.Patrol;
            return false;
        }

        /// <summary>Hostile intent's state for the checkpoint hash: no bytes until a face is known, the crew saw an act, or a today warning opened.</summary>
        internal void HashIntent(Hash.H h)
        {
            if (faces.Count > 0) { h.S("faces"); foreach (var x in faces) h.S(x); }
            if (rivalsSaw.Count > 0) { h.S("rivalsSaw"); foreach (var x in rivalsSaw) h.S(x); }
            if (!Lab && (warnings.Count > 0 || warnedBy.Count > 0 || warnLogged.Count > 0))
            {
                h.S("warn");
                foreach (var x in warnings) h.S(x.Watcher).I((int)x.Op).I(x.Until).I(x.DrawTick);
                foreach (var kv in warnedBy) h.S(kv.Key).I(kv.Value);
                foreach (var x in warnLogged) h.S(x);
            }
        }
    }
}
