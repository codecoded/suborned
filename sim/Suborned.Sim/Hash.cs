// Checksums: FNV-1a 64 over a canonical walk of the whole state, in a fixed order. A replay is right when its checkpoint hashes match.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public static class Hash
    {
        const ulong Offset = 14695981039346656037UL, Prime = 1099511628211UL;
        public static ulong Of(string s) { var h = new H(); h.S(JNode.Canon(s)); return h.V; }
        public static string Hex(ulong v) => v.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);

        public sealed class H
        {
            public ulong V = Offset;
            public H B(byte b) { unchecked { V ^= b; V *= Prime; } return this; }
            public H I(long v) { unchecked { for (int k = 0; k < 8; k++) B((byte)(v >> (8 * k))); } return this; }
            public H Z(bool v) => B(v ? (byte)1 : (byte)0);
            public H S(string s) { I(s.Length); unchecked { foreach (var c in s) { B((byte)c); B((byte)(c >> 8)); } } return this; }
            public H P(Pt p) => I(p.X).I(p.Y);
        }

        public static void HashGun(H h, Gun g) => Gun(h, g);
        static void Gun(H h, Gun g)
        {
            h.S(g.Id).S(g.W?.Id ?? "").Z(g.Trained).I(g.Health).I(g.Max).Z(g.Down).Z(g.Drawn).I(g.DrawReady).I(g.HolsterAt).I(g.DrawTick).S(g.Stance)
             .S(g.Order).Z(g.Force).S(g.Target).I(g.Acc).I(g.NextShot).I(g.LostAt).S(g.ShotBy).Z(g.Fired).Z(g.Engaged).Z(g.Hit)
             .I(g.ShotByTick).S(g.St).I(g.Until).I(g.NoFoeSince).I(g.StepAt).I(g.UnprotSince).I(g.Class).I(g.LostTick).I(g.SightLostAt).S(g.Cover).S(g.LostTarget);
        }

        /// <summary>One person's state alone (what they are, know and remember), for "before and after" checks.</summary>
        public static ulong Person(Person p)
        {
            var h = new H();
            h.S(p.Role).S(p.Id).S(p.Temperament).S(p.Need).Z(p.Turned).Z(p.NeedMet).Z(p.Down).Z(p.KRoutine).Z(p.KNeed).Z(p.KTemperament);
            foreach (var v in p.Sc) h.I(v);
            foreach (var x in p.KnownRefused) h.S(x);
            foreach (var m in p.Memory) h.S(m.Act).S(m.By).S(m.Place).I(m.Tick).Z(m.Witnessed).S(m.Told ?? "");
            return h.V;
        }

        /// <summary>For a behaviour-only comparison across a move of standing.start (suborned-sim hashes --standing-as n, brief 014): hash Standing
        /// as if the run had started at n. Never set in play.</summary>
        public static int StandingAs;
        static void Fight(H h, PublicFight f)
        {
            h.I(f.Start).I(f.Last).I(f.End).Z(f.Final).Z(f.Public).Z(f.OnSecurity).Z(f.OnRivals).S(f.FirstSecurity).S(f.RivalsFirst).I(f.CrewBroken).I(f.Rivals.Count);
            foreach (var r in f.Rivals) h.S(r);
            h.S(f.Outcome).I(f.Delta);
        }
        public static ulong State(Game g)
        {
            var h = new H();
            h.I(unchecked((long)g.StartHash)).I(g.Tick).I(g.Cash).I(g.Trail).I(g.Alert).I(g.Gunfire).I(g.Reported).I(g.Standing + StandingAs).I((int)g.Result).S(g.FailCode);
            foreach (var op in g.Ops)
            {
                h.I((int)op.Id).P(op.Pos).Z(op.Walk != null).I(op.WalkMm).Z(op.Inside);
                if (op.Running && op.Walk != null) h.S("run").I(op.WalkSpeed); // only a run adds bytes: a walk hashes as it always did
                if (op.Driven) h.S("drive").Z(op.DriveTo.HasValue).P(op.DriveTo ?? default).Z(op.DriveRun).Z(op.DriveSpan).Z(op.Running).Z(op.DriveMoved).I(op.Face).I(op.DriveBudget).I(op.DrivePush).Z(op.DriveResume.HasValue).P(op.DriveResume ?? default).Z(op.DriveResumeRun); // brief 025: only a driven operative adds bytes
                h.S(op.Busy?.ToString() ?? "").I(op.BusyEnd);
                Gun(h, op.Gun); h.I(op.Carrying.HasValue ? (int)op.Carrying.Value : -1).I(op.CarriedBy.HasValue ? (int)op.CarriedBy.Value : -1).Z(op.RunAlerted).I(op.RunSeen.Count);
            }
            foreach (var p in g.People)
            {
                h.S(p.Role).S(p.Temperament).S(p.Need).Z(p.Turned).Z(p.NeedMet).Z(p.Down).Z(p.KRoutine).Z(p.KNeed).Z(p.KTemperament);
                if (p.Secret.Length > 0) h.S(p.Secret).Z(p.KSecret).I(p.Summoned); // only a person with a secret adds bytes
                foreach (var v in p.Sc) h.I(v);
                foreach (var a in p.KnownRefused) h.S(a);
                h.I(p.Memory.Count);
                foreach (var m in p.Memory) h.S(m.Act).S(m.By).S(m.Place).I(m.Tick).Z(m.Witnessed).S(m.Told ?? "");
            }
            h.Z(g.LoopOn).Z(g.CellFiredOn);
            if (g.LoopCam != "entrance") h.S(g.LoopCam); // only a loop on another camera adds bytes
            Gun(h, g.GuardGun); Gun(h, g.PatrolGun); Gun(h, g.RivalsGun); foreach (var s in g.SecGuns) Gun(h, s);
            foreach (var role in new[] { "waitress", "driver", "councillor" }) Gun(h, g.GunOf(role)!);
            h.I(g.LureTick).I(g.GuardAwayUntil).I(g.MissingNotice).Z(g.GuardHeld).Z(g.GuardEngaged).Z(g.GuardDown);
            var gp = g.GuardPose(); h.Z(gp.HasValue); if (gp.HasValue) h.P(gp.Value.pos);
            h.I(g.BlockMove).I(g.BlockedFrom).I(g.BlockedUntil).P(g.CarPos());
            h.I((int)g.Vale).Z(g.PickupService).Z(g.EntryInProgress); var vp = g.ValePos(); h.Z(vp.HasValue); if (vp.HasValue) h.P(vp.Value);
            h.Z(g.PatrolDown).Z(g.PatrolChasing).Z(g.PatrolTracking).I(g.PatrolAwayUntil); var pp = g.PatrolPose(); h.Z(pp.HasValue); if (pp.HasValue) h.P(pp.Value.pos);
            h.I(g.RivalAt).Z(g.RivalsArrived).Z(g.RivalsDrawn).Z(g.RivalsTracking).I(g.RivalsDelayedUntil).I(g.RivalsDrawnTick).S(g.RivalsDelayMethod);
            var rp = g.RivalsPose(); h.Z(rp.HasValue); if (rp.HasValue) h.P(rp.Value.pos);
            h.I(g.SecurityArrives).I(g.Lockdown).Z(g.SecurityDown).Z(g.SecurityTracking);
            var sp = g.SecurityPose(); h.Z(sp.HasValue); if (sp.HasValue) h.P(sp.Value.pos);
            h.Z(g.InezWitnessedShooting).S(g.InezMethod).S(g.RouteKind).S(g.RouteGuard).S(g.RouteBot).S(g.RouteRival);
            h.S(g.FightBranch).I(g.FightStart).I(g.FightSprung).I(g.FightOverTick).Z(g.FightWarned).Z(g.GuardAlly).I(g.FightHits).I(g.FightShots).I(g.DownedByEnforcer).I(g.DownedByGuard).I(g.DownedByCell);
            foreach (var x in g.Hostiles) { h.S(x.Id).P(x.Pos(g.Tick)).Z(x.Present); Gun(h, x.Gun); }
            foreach (var x in g.Captured) h.S(x);
            foreach (var x in g.Hostiles) h.P(x.Home);
            g.HashAi(h.I); h.I(g.LastShotTick);
            if (g.Fights.Count > 0 || g.OpenFight != null) // fight.public (Sky 2026-10-16): only a run with a fight adds bytes
            {
                foreach (var f in g.Fights) Fight(h, f);
                h.Z(g.OpenFight != null); if (g.OpenFight != null) Fight(h, g.OpenFight);
            }
            if (g.People2 != null && g.Start.Crowd)
                foreach (var w in g.Walkers)
                {
                    var at = g.CrowdPos(w); h.Z(at.HasValue); if (at.HasValue) h.P(at.Value);
                    if (w.St.Length > 0) h.S(w.St).I(w.Until).I(w.RunStart).P(w.At).S(w.Refuge); // only a scattered person adds bytes
                    if (w.Pace != 0) h.I(w.Pace); // brief 013: only a reporter's walk adds bytes
                    h.I(w.P.Memory.Count); foreach (var m in w.P.Memory) h.S(m.Act).S(m.By).I(m.Tick); foreach (var v in w.P.Sc) h.I(v);
                }
            // what the debrief reads, and the record of the run (Redline E4): every flag, incident, ledger row, debrief line, chain link,
            // action, order and its answer, and every log entry's code and tick (not its words, which are for people)
            h.Z(g.GuardReachedCafe).I(g.Cash);
            foreach (var x in g.IncidentKeys()) h.S(x);
            foreach (var (t, what, d, now, cause) in g.Ledger) h.I(t).S(what).I(d).I(what == "standing" ? now + StandingAs : now).S(cause);
            foreach (var (t, line, who, detail) in g.DebriefLines) h.I(t).S(line).S(who).S(detail);
            foreach (var x in g.Chain) h.S(x);
            foreach (var x in g.Actions) h.S(x);
            foreach (var (o, ok, code, _) in g.Orders) h.S(o.ToString()).Z(ok).S(code); // the refusal code, never the words (Fable)
            foreach (var l in g.Log) h.I(l.Tick).S(l.Code);
            foreach (var op in g.Ops) h.I(op.BusyPaid);
            foreach (var i in g.Incidents) { h.I(i.Tick).S(i.Kind).S(i.Cause).P(i.At).I(i.Witnesses.Count); foreach (var (p, at) in i.Witnesses) h.S(p.Id).P(at); }
            foreach (var s in g.Sightings) h.S(s.Watcher).S(s.Op).I(s.From).I(s.To).P(s.At);
            foreach (var x in g.ExposedByWatcher) h.S(x);
            g.HashSight(h); // what the cell knows of watchers: no bytes until it knows something (rulebook sight)
            g.HashTakedown(h);
            g.HashGetaway(h);
            g.HashRounds(h); // brief 022: no bytes unless the round is played // brief 018: no bytes until someone boards, a blocker is picked or the car leaves // brief 013: no bytes until a takedown, a downed guard's mark, an alarm or a marked foe
            return h.V;
        }
    }
}
