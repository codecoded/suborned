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

        public static ulong State(Game g)
        {
            var h = new H();
            h.I(unchecked((long)g.StartHash)).I(g.Tick).I(g.Cash).I(g.Trail).I(g.Alert).I(g.Gunfire).I(g.Reported).I(g.Standing).I((int)g.Result).S(g.FailCode);
            foreach (var op in g.Ops)
            {
                h.I((int)op.Id).P(op.Pos).Z(op.Walk != null).I(op.WalkMm).Z(op.Inside);
                h.S(op.Busy?.ToString() ?? "").I(op.BusyEnd);
            }
            foreach (var p in g.People)
            {
                h.S(p.Role).S(p.Temperament).S(p.Need).Z(p.Turned).Z(p.NeedMet).Z(p.Down).Z(p.KRoutine).Z(p.KNeed).Z(p.KTemperament);
                foreach (var v in p.Sc) h.I(v);
                foreach (var a in p.KnownRefused) h.S(a);
                h.I(p.Memory.Count);
                foreach (var m in p.Memory) h.S(m.Act).S(m.By).S(m.Place).I(m.Tick).Z(m.Witnessed);
            }
            h.I(g.EnforcerHealth).I(g.EnforcerNextShot).Z(g.LoopOn);
            h.I(g.LureTick).I(g.GuardAwayUntil).I(g.GuardHealth).I(g.GuardNextShot).I(g.MissingNotice).Z(g.GuardHeld).Z(g.GuardEngaged).Z(g.GuardDown);
            var gp = g.GuardPose(); h.Z(gp.HasValue); if (gp.HasValue) h.P(gp.Value.pos);
            h.I(g.BlockMove).I(g.BlockedFrom).I(g.BlockedUntil).P(g.CarPos());
            h.I((int)g.Vale).Z(g.PickupService).Z(g.EntryInProgress); var vp = g.ValePos(); h.Z(vp.HasValue); if (vp.HasValue) h.P(vp.Value);
            h.Z(g.PatrolDown).Z(g.PatrolChasing).Z(g.PatrolTracking).I(g.PatrolHealth).I(g.PatrolAwayUntil).I(g.PatrolNextShot); var pp = g.PatrolPose(); h.Z(pp.HasValue); if (pp.HasValue) h.P(pp.Value.pos);
            h.I(g.RivalAt).Z(g.RivalsArrived).Z(g.RivalsDrawn).Z(g.RivalsTracking).I(g.RivalsDelayedUntil).I(g.RivalsDrawnTick).I(g.RivalsHealth).I(g.RivalsNextShot).S(g.RivalsDelayMethod);
            var rp = g.RivalsPose(); h.Z(rp.HasValue); if (rp.HasValue) h.P(rp.Value.pos);
            h.I(g.SecurityArrives).I(g.Lockdown).I(g.SecurityHealth).I(g.SecurityNextShot).Z(g.SecurityDown).Z(g.SecurityTracking);
            var sp = g.SecurityPose(); h.Z(sp.HasValue); if (sp.HasValue) h.P(sp.Value.pos);
            h.Z(g.InezWitnessedShooting).S(g.InezMethod).S(g.RouteKind).S(g.RouteGuard).S(g.RouteBot).S(g.RouteRival);
            h.S(g.FightBranch).I(g.FightStart).I(g.FightSprung).I(g.NextVolley).I(g.FightOverTick).Z(g.FightWarned).Z(g.GuardAlly).Z(g.GuardAllyTracking).I(g.GuardAllyNextShot).I(g.FightHits);
            foreach (var x in g.Hostiles) h.S(x.Id).P(x.Pos(g.Tick)).I(x.Health).Z(x.Down).Z(x.Present);
            if (g.People2 != null && g.Start.Crowd)
                foreach (var w in g.Walkers)
                {
                    var at = g.CrowdPos(w); h.Z(at.HasValue); if (at.HasValue) h.P(at.Value);
                    h.I(w.P.Memory.Count); foreach (var m in w.P.Memory) h.S(m.Act).S(m.By).I(m.Tick); foreach (var v in w.P.Sc) h.I(v);
                }
            // what the debrief reads, and the record of the run (Redline E4): every flag, incident, ledger row, debrief line, chain link,
            // action, order and its answer, and every log entry's code and tick (not its words, which are for people)
            h.Z(g.GuardReachedCafe).I(g.Cash);
            foreach (var x in g.IncidentKeys()) h.S(x);
            foreach (var (t, what, d, now, cause) in g.Ledger) h.I(t).S(what).I(d).I(now).S(cause);
            foreach (var (t, line, who, detail) in g.DebriefLines) h.I(t).S(line).S(who).S(detail);
            foreach (var x in g.Chain) h.S(x);
            foreach (var x in g.Actions) h.S(x);
            foreach (var (o, ok, why) in g.Orders) h.S(o.ToString()).Z(ok).S(why);
            foreach (var l in g.Log) h.I(l.Tick).S(l.Code);
            foreach (var op in g.Ops) h.I(op.BusyPaid);
            return h.V;
        }
    }
}
