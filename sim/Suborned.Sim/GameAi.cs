// The fight AI (Pulse, briefs/011/pulse-fight-ai.md; rulebook `ai`): one machine for every hostile body. routine → alerted → advance →
// engage ↔ hold, break → gone (rivals), down. Targets by class (whoever fired at it, whoever holds Vale, the nearest with a weapon drawn,
// the nearest), kept until a better class appears; cover from the site's cover points (Cover.cs); police hold their stand; the response
// unit joins the loud fight; the guard as an ally runs to his post and shoots the rival nearest the Enforcer. Integers only, ties on id.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        readonly Dictionary<string, string> coverTaken = new Dictionary<string, string>(StringComparer.Ordinal); // cover point → body
        readonly bool[] secJoined = new bool[8];
        readonly Dictionary<string, int> standOverflow = new Dictionary<string, int>(StringComparer.Ordinal);
        Pt patrolOrigin; bool patrolReturning;

        /// <summary>The AI's own state for the checkpoint hash: cover taken, the response unit joined, the patrol's origin and return.</summary>
        internal void HashAi(Func<long, Hash.H> i)
        {
            var keys = new List<string>(coverTaken.Keys); keys.Sort(StringComparer.Ordinal);
            foreach (var k in keys) { foreach (var c in k) i(c); foreach (var c in coverTaken[k]) i(c); i(-1); }
            if (forCar.Count > 0) { i(-67); foreach (var id in forCar) { foreach (var c in id) i(c); i(-1); } } // engine 0.34.0 (M067-K3 a): only while any walk to the car
            for (int k = 0; k < SecGuns.Length; k++) i(secJoined[k] ? 1 : 0);
            i(patrolOrigin.X); i(patrolOrigin.Y); i(patrolReturning ? 1 : 0);
            var so = new List<string>(standOverflow.Keys); so.Sort(StringComparer.Ordinal);
            foreach (var k in so) { foreach (var c in k) i(c); i(standOverflow[k]); }
        }

        static bool IsRival(string id) => id.StartsWith("rival-", StringComparison.Ordinal);
        bool IsHolder(Hostile h) => !IsRival(h.Id) || h.Blocker; // police and the response unit hold their stand (Pulse §3.2); a blocker its lane point (brief 018 §1)
        Pt Car => GetawayCarPos(); // brief 059 K6: where the getaway car is now (its stand, or along its Drive)

        // ---------------------------------------------------------------- moving a body

        void MoveTo(Hostile h, Pt to)
        {
            h.Closing = false; // engine 0.34.1: AdvanceOnOp sets it again for its own walk
            var from = h.Pos(Tick);
            var a = Walk.NearestFree(from) ?? from; var b = Walk.NearestFree(to) ?? to;
            var route = Walk.Route(a, b);
            if (route == null || route.Length < 2) { h.At = from; h.Run = null; return; }
            h.Run = new Path(route); h.RunStart = Tick; h.At = h.Run.End;
            if (h.Run.Length == 0) { h.Run = null; }
        }
        void Stop(Hostile h) { h.At = h.Pos(Tick); h.Run = null; h.Closing = false; }

        void TakeCover(Gun g, CoverPoint c) { ReleaseCover(g); coverTaken[c.Id] = g.Id; g.Cover = c.Id; g.UnprotSince = -1; Note("ai.cover", $"{g.Id} takes cover at {c.Id} ({c.Kind} {c.Of}, q {c.Q})."); }
        void ReleaseCover(Gun g) { if (g.Cover.Length > 0 && coverTaken.TryGetValue(g.Cover, out var who) && who == g.Id) coverTaken.Remove(g.Cover); g.Cover = ""; }
        CoverPoint? CoverById(string id) { foreach (var c in LiveCover) if (c.Id == id) return c; return null; }
        HashSet<string> Taken() => new HashSet<string>(coverTaken.Keys, StringComparer.Ordinal);
        bool Protected(Pt pos, Pt shooter) => LiveCover.Count > 0 && CoverRules.Modifier(LiveCover, pos, shooter, Tick / Hz, R.CoverSnapCm, R.PointBlankCm, R.CoverQFull, R.CoverQPartial, R.ModCover, R.ModCoverPartial) < 1000;
        CoverPoint? SelectCover(Pt me, Pt objective, Pt? foe, int seekCm) =>
            LiveCover.Count == 0 ? null : CoverRules.Select(LiveCover, me, objective, foe, Taken(), seekCm, Tick / Hz, R.PointBlankCm, R.CoverQPartial, R.CoverRetreatCm, R.CoverStepCloserCm);

        /// <summary>Where an advancer without cover halts: the first point of its route within ai.stopReach of its weapon's effective reach of
        /// the target (Pulse §2.1), or where it stands if it is already that close.</summary>
        Pt StopPoint(Pt from, Pt target, int effCm)
        {
            long stop = (long)effCm * R.StopReachPerMille / 1000;
            if (G.D2(from, target) <= stop * stop) return from;
            var a = Walk.NearestFree(from) ?? from; var b = Walk.NearestFree(target) ?? target;
            var route = Walk.Route(a, b); if (route == null) return from;
            var p = new Path(route);
            for (long mm = 0; mm <= p.Length; mm += 500) { var q = p.At(mm); if (G.D2(q, target) <= stop * stop) return q; } // every 50 cm (Path is in mm)
            return p.End;
        }

        // ---------------------------------------------------------------- foes and targets

        /// <summary>The foes a body may shoot: in its weapon's effective reach, on a clear line its `sees` allows. Before the final fight only an
        /// operative with a weapon out, who has fired, or who holds Vale; in it every operative (ai.intent.final every; the range always) or, with final agent (tuning.md §15.2), only an operative with its own cause, and, for the rivals, the guard ally.</summary>
        List<(string id, Pt at)> FoesInReach(string id, Gun g, Pt pos, Func<Pt, bool> sees, bool anyOp)
        {
            var l = new List<(string, Pt)>(); long eff = (long)g.W!.EffCm * g.W.EffCm;
            foreach (var op in Ops)
            {
                if (op.Down || op.Inside) continue;
                bool foe = (anyOp && (R.Intent.FinalEvery || Range)) || Provoked(id, op); // engine 0.20.0 (ai.intent.final agent, tuning.md §15.2, PROVISIONAL): in the final fight too, per agent; the range every // engine 0.19.0 (ai.intent): per agent, from what it is seen doing or its own known face (was: a weapon out, fired, Vale, or security saw violence, for every body)
                if (!foe || !sees(op.Pos)) continue;
                long d2 = G.D2(pos, op.Pos);
                if (d2 > eff && !(d2 <= (long)g.W.MaxCm * g.W.MaxCm && PoliceBody(id) && ShotAtRecently(g, Sim.Op.Name(op.Id)))) continue; // tuning.md §7.10 rule 1: shot at, the police answer to their max ring
                l.Add((Sim.Op.Name(op.Id), op.Pos));
            }
            if (anyOp && IsRival(id) && GuardAlly && !GuardDown) { var ga = PosOf("guard"); if (ga.HasValue && G.D2(pos, ga.Value) <= eff && sees(ga.Value)) l.Add(("guard", ga.Value)); }
            return l;
        }

        /// <summary>Pulse §2.2: class 1 the foe that fired at this body within weapons.lineLapse, 2 the operative holding Vale, 3 the nearest with
        /// a weapon drawn, 4 the nearest; ties on distance, then id. The body keeps its target until it is down or out of the list, unless a
        /// foe of a better class appears.</summary>
        string ChooseTarget(string id, Gun g, Pt pos, List<(string id, Pt at)> foes, bool ally = false)
        {
            if (foes.Count == 0) return "";
            int Class(string f, Pt at)
            {
                if (PoliceBody(id) && IsOpId(f) ? ShotByOpRecently(g, f) : g.ShotBy == f && g.ShotByTick >= 0 && Tick - g.ShotByTick <= T(R.LineLapseS)) return 1; // tuning.md §12.2: police, any operative shooting them; the rest, the last shooter
                if (ally) return 3;
                if (f == "enforcer" && Vale == ValeAt.WithEnforcer) return 2;
                var fg = GunOf(f); return fg != null && fg.Drawn ? 3 : 4;
            }
            string best = ""; int bc = 9; long bd = long.MaxValue;
            Pt? enf = ally && !Enforcer.Inside ? Enforcer.Pos : (Pt?)null;
            foreach (var (f, at) in foes)
            {
                int c = Class(f, at);
                if (ally && c == 3 && enf.HasValue) c = 2; // the guard ally's class 2: the rival nearest the Enforcer (distance below is to him)
                long d = ally && c == 2 ? G.D2(enf!.Value, at) : G.D2(pos, at);
                if (c < bc || (c == bc && (d < bd || (d == bd && string.CompareOrdinal(f, best) < 0)))) { best = f; bc = c; bd = d; }
            }
            foreach (var (f, at) in foes)
                if (f == g.Target)
                {
                    int cur = Class(f, at); if (ally && cur == 3 && enf.HasValue) cur = 2;
                    if (bc >= cur) { g.Class = cur; return f; } // committed: only a better class replaces it
                }
            g.Class = bc; return best;
        }

        /// <summary>Aim with a warm reacquire (Pulse §2.1 hold): a target lost and found again within ai.reacquire is shot at once.</summary>
        void AimAt(Gun g, string t, bool react)
        {
            if (g.Target == t) return;
            if (t == g.LostTarget && g.LostTick >= 0 && Tick - g.LostTick <= T(R.ReacquireS)) { g.Target = t; g.NextShot = Math.Max(g.NextShot, Tick); return; }
            Aim(g, t, react);
        }
        void LoseTarget(Gun g) { if (g.Target.Length > 0) { g.LostTarget = g.Target; g.LostTick = Tick; } g.Target = ""; }

        // ---------------------------------------------------------------- the bodies

        void CityBody(string id)
        {
            var h = HostileOf(id);
            if (h != null) { FightBody(h); return; }
            int k = SecIndex(id);
            if (k >= 0) { if (!SecGuns[k].Down) PreFight(id, SecGuns[k], SecOfficerPose(k)?.pos, p => SecOfficerWatching(k, p, false)); return; } // its own aim ignores people (Pulse §1.6)
            switch (id)
            {
                case "guard":
                    if (!GuardEngaged || GuardDown) return;
                    PreFight("guard", GuardGun, PosOf("guard"), p => GuardWatching(p, false));
                    if (GuardGun.NoFoeSince >= 0 && Tick - GuardGun.NoFoeSince >= T(R.GuardReturnS)) { GuardEngaged = false; GuardGun.NoFoeSince = -1; Note("guard.stands-down", "No foe in reach: the guard stands down."); }
                    return;
                case "patrol":
                    if (!PatrolChasing || PatrolDown || PatrolAwayUntil > Tick) return;
                    PreFight("patrol", PatrolGun, PosOf("patrol"), p => PatrolWatching(p, false));
                    if (!patrolReturning && PatrolGun.NoFoeSince >= 0 && Tick - PatrolGun.NoFoeSince >= T(R.PatrolResumeS)) { patrolReturning = true; Note("patrol.resumes", "No foe in reach: the patrol goes back to its sweep."); }
                    return;
            }
            if (CrewOf(id) is CrewMember cm) // brief 059 K8: each member of the crew on foot, by his own cone (engine 0.19.0: the crew watches the street from its arrival)
            {
                if (cm.Fight || !Up(cm) || !cm.OnFoot || CrewBrokenPre) { if (!cm.Fight) LoseTarget(cm.Gun); return; } // a broken crew makes for its car and fires nothing (Motive M059-3.5)
                if (!RivalsDrawn && !R.Intent.RivalsStreet) return;
                int nf = cm.Gun.NoFoeSince; PreFight(id, cm.Gun, MemberPose(cm)?.pos, p => MemberWatching(cm, p)); if (!RivalsDrawn && cm.Gun.Target.Length == 0) cm.Gun.NoFoeSince = nf; // no foe leaves its gun as it was (nothing reads NoFoeSince for the crew)
            }
        }

        /// <summary>A body before the final fight (the guard engaging, the patrol, the response unit en route, the rival crew drawn by gunfire):
        /// it shoots a foe in its weapon's effective reach that it sees, by class; a first sight costs react (Pulse §3.3, §3.5, §3.6).</summary>
        void PreFight(string id, Gun g, Pt? posN, Func<Pt, bool> sees)
        {
            if (g.Down || g.W == null || !posN.HasValue) { LoseTarget(g); return; }
            var pos = posN.Value;
            var foes = FoesInReach(id, g, pos, sees, false);
            string t = ChooseTarget(id, g, pos, foes);
            if (t.Length == 0) { LoseTarget(g); if (g.NoFoeSince < 0) g.NoFoeSince = Tick; return; }
            g.NoFoeSince = -1;
            AimAt(g, t, true);
            if (Tick >= g.NextShot) { var at = PosOf(t)!.Value; Shoot(id, g, pos, t, at, Dist(pos, at), Moving(id) ? Gait.Walk : Gait.Stand, false); }
        }

        /// <summary>A final-fight body (rivals, officers, the response unit once joined): one transition a tick, then its action.</summary>
        void FightBody(Hostile h)
        {
            var g = h.Gun;
            if (g.Down) { ReleaseCover(g); h.Closing = false; return; }
            if (!h.Present || g.St == "gone") return;
            if (ForCarBody(h)) return; // engine 0.34.0 (Motive M067-K3 a): going for the unguarded car, firing as he walks
            var pos = h.Pos(Tick); var w = g.W!;
            Func<Pt, bool> sees = p => Sees(pos, p);
            var foes = g.St == "break" ? new List<(string, Pt)>() : FoesInReach(h.Id, g, pos, sees, true);
            bool moving = h.Moving(Tick);
            bool pinHold = PinHolds(g); // brief 039 (weapons.pin.hold): a pinned body holds, it doesn't advance
            switch (g.St) // one transition a tick (Pulse §1.4)
            {
                case "alerted":
                    if (Tick >= g.Until)
                    {
                        if (foes.Count > 0) { g.St = "engage"; var t0 = ChooseTarget(h.Id, g, pos, foes); g.Target = t0; g.Acc = AccFor(g, t0); g.NextShot = Math.Max(g.NextShot, Tick); } // the wait was the aim
                        else if (!pinHold) { g.St = "advance"; Advance(h); } // brief 039: pinned, it waits where it is
                    }
                    break;
                case "advance":
                    if (foes.Count > 0) { if (!h.Blocker) Stop(h); g.St = "engage"; moving = h.Moving(Tick); } // a blocker keeps walking to the road (brief 018 §1)
                    else if (!moving) { g.St = "hold"; h.Closing = false; } // engine 0.34.1: the advance on an operative has ended
                    break;
                case "engage":
                    if (foes.Count == 0)
                    {
                        if (g.NoFoeSince < 0) g.NoFoeSince = Tick;
                        else if (Tick - g.NoFoeSince >= T(R.EngageLapseS))
                        {
                            g.NoFoeSince = -1; LoseTarget(g);
                            if (IsHolder(h) && pos == h.Home) g.St = "hold"; else if (IsHolder(h) && KiteHolds(g)) g.St = "hold"; else if (pinHold) g.St = "hold"; else { g.St = "advance"; Advance(h); } // brief 027 F2: no walk home while alarmed; brief 039: pinned, it holds
                        }
                    }
                    else g.NoFoeSince = -1;
                    break;
                case "hold":
                    if (foes.Count > 0) { g.St = "engage"; g.NoFoeSince = -1; }
                    else if (IsHolder(h) && pos != h.Home && !moving && !KiteHolds(g)) { if (!pinHold) { g.St = "advance"; Advance(h); } } // brief 039: pinned, it holds
                    else if (!IsHolder(h) && IsRival(h.Id) && !moving && FightBranch == "safe" && !pinHold) // engine 0.34.0 (Motive M067-SQ2): no stall; brief 039: not while pinned
                    {
                        if (g.NoFoeSince < 0) g.NoFoeSince = Tick;
                        else if (Tick - g.NoFoeSince >= T(R.EngageLapseS)) { g.NoFoeSince = -1; AdvanceOnOp(h, pos); }
                    }
                    break;
                case "break":
                    if (G.Within(pos, h.BreakTo ?? S.Stand(S.RivalsPark), R.RivalGoneCm) || !moving) { g.St = "gone"; h.Present = false; Note("ai.gone", $"{h.Id} is gone."); }
                    return;
            }
            if (g.St != "engage" || foes.Count == 0) { if (g.St != "engage") LoseTarget(g); return; }
            if (moving && !R.CoverStepFires && CoverStepping(h)) return; // tuning.md §8.3 fallback: a step to cover fires nothing
            if (moving && Lab && !LabFireMoving) return; // tuning.md §8.3 (Sky 2026-10-21, engine 0.15.0): a hostile fires as it walks in both presets (the lab's hostilesFireMoving switch can still turn it off there)
            string t = ChooseTarget(h.Id, g, pos, foes);
            var at = PosOf(t)!.Value;
            // cover (Pulse §2.3): leave a point unprotected for cover.leave; a rival steps to a point that shields it, once each cover.stepGap
            bool prot = Protected(pos, at);
            if (g.Cover.Length > 0)
            {
                var c = CoverById(g.Cover);
                if (c == null || !c.Present(Tick / Hz)) ReleaseCover(g);
                else if (prot) g.UnprotSince = -1;
                else if (g.UnprotSince < 0) g.UnprotSince = Tick;
                else if (Tick - g.UnprotSince >= T(R.CoverLeaveS)) ReleaseCover(g);
            }
            if (!prot && IsRival(h.Id) && !h.Blocker && Tick - g.StepAt >= T(R.CoverStepGapS))
            {
                var c = SelectCoverOut(pos, Car, at, R.SeekRivalCm); // engine 0.34.0 (Motive M067-SQ1): never a cover point in the hold or the lane
                if (c != null) { g.StepAt = Tick; TakeCover(g, c); MoveTo(h, c.At); h.Why = "answer"; if (h.Moving(Tick) && ((Lab && !LabFireMoving) || !R.CoverStepFires)) return; }
            }
            AimAt(g, t, false);
            if (Tick >= g.NextShot) Shoot(h.Id, g, pos, t, at, Dist(pos, at), h.Moving(Tick) ? Gait.Walk : Gait.Stand, false);
        }

        /// <summary>Tuning.md §8.3's fallback: a rival (not the blocker) in a fight, walking to the cover point it stepped to (FightBody's cover
        /// step: engage, holding the point, its walk ending on it). Advances, closes and lanes are not steps to cover.</summary>
        bool CoverStepping(Hostile h)
        {
            var g = h.Gun; if (g.St != "engage" || h.Blocker || !IsRival(h.Id) || g.Cover.Length == 0 || h.Run == null) return false;
            var c = CoverById(g.Cover); return c != null && G.Within(h.Run.End, c.At, R.CoverSnapCm);
        }

        /// <summary>Where an advancer goes (Pulse §3): police and the response unit to their stand; a rival to the cover the data give with the
        /// Enforcer (or the car) as its foe, within ai.seek.rival, else to its stop point short of the car.</summary>
        void Advance(Hostile h)
        {
            var g = h.Gun; var pos = h.Pos(Tick);
            if (IsHolder(h)) { if (h.Blocker) { MoveTo(h, h.Home); h.Why = "blocker"; } else MoveToFight(h, h.Home); return; } // engine 0.34.0 (Motive M067-SQ1): back to its stand round the car's hold (the blocker's lane is the road)
            Pt foe = !Enforcer.Inside && !Enforcer.Down ? Enforcer.Pos : Car;
            var c = SelectCover(pos, Car, foe, R.SeekRivalCm); h.Why = "advance";
            if (c != null) { TakeCover(g, c); MoveTo(h, c.At); return; }
            MoveTo(h, StopPoint(pos, Car, g.W!.EffCm));
        }

        /// <summary>The guard as the cell's ally (Pulse §3.4): he runs to his post (or the cover the data give within ai.seek.ally of it) and
        /// shoots, by his own classes: a rival that fired at him, the rival nearest the Enforcer, the nearest.</summary>
        void AllyBody()
        {
            var g = GuardGun; if (GuardDown) return;
            var pos = PosOf("guard"); if (!pos.HasValue) return;
            if (GuardAllyMoving) return; // running in: fires nothing
            var foes = new List<(string, Pt)>(); long eff = (long)g.W!.EffCm * g.W.EffCm;
            foreach (var h in Hostiles)
            {
                if (!IsRival(h.Id) || !h.Present || h.Down || h.Gun.St == "break" || h.Gun.St == "gone") continue;
                var at = h.Pos(Tick); if (G.D2(pos.Value, at) <= eff && Sees(pos.Value, at)) foes.Add((h.Id, at));
            }
            string t = ChooseTarget("guard", g, pos.Value, foes, ally: true);
            if (t.Length == 0) { LoseTarget(g); return; }
            AimAt(g, t, false);
            var tp = PosOf(t)!.Value;
            if (Tick >= g.NextShot) Shoot("guard", g, pos.Value, t, tp, Dist(pos.Value, tp), Gait.Stand, false);
        }

        /// <summary>The guard ally's post: the cover the data give within ai.seek.ally of guard-ally, else the post itself.</summary>
        Pt AllyStop()
        {
            var post = S.Stand(S.FightGuard);
            var c = SelectCover(post, Car, null, R.SeekAllyCm);
            if (c == null) return post;
            TakeCover(GuardGun, c); return c.At;
        }

        /// <summary>Rivals break (Pulse §3.1): the tick a rival goes down, if more than half the crew is down, every rival still up runs for its car.</summary>
        void RivalDown(Hostile? who = null)
        {
            int n = 0, down = 0;
            bool Crew(Hostile h) => IsRival(h.Id) && (!Range || who == null || h.Wave == who.Wave); // brief 029: in the range a crew is one spawn
            foreach (var h in Hostiles) if (Crew(h)) { n++; if (h.Down || talked.Contains(h.Id)) down++; } // brief 039: a rival talked down counts as one down
            if (down * 2 <= n) return;
            bool any = false;
            var carAt = Range ? S.Stand(S.RivalsPark) : RivalCarPose() is var rcp && rcp.HasValue ? Walk.NearestFree(rcp.Value.pos) ?? rcp.Value.pos : S.Stand(S.RivalsPark); // brief 059: they run for their car where it stands
            foreach (var h in Hostiles)
            {
                if (!Crew(h) || h.Down || h.Gun.St == "break" || h.Gun.St == "gone") continue;
                if (!h.Present) { h.Gun.St = "gone"; if (CrewOf(h.Id) is CrewMember gm && !gm.Fight) { gm.Mode = CrewMode.Gone; gm.ModeTick = Tick; } continue; } // still lying in wait, or still in the car: it never comes
                ReleaseCover(h.Gun); LoseTarget(h.Gun); h.Gun.St = "break"; h.Speed = rivSpeed; h.BreakTo = carAt; MoveTo(h, carAt); h.Why = "break"; any = true;
            }
            if (any) { FightWhy.Add("rivals-break"); Note("ai.break", "More than half the crew is down: the rivals break for their car."); }
        }

        /// <summary>The response unit joins a loud final fight (Pulse §3.3): at the fight's start, or its arrival if later, each officer still
        /// up runs (no alert delay) to the nearest police stand; a stand taken sends it to the nearest free cover within ai.seek.police of the
        /// stand, else to a slot ai.overflow beside it, across the line to the car, alternating sides.</summary>
        void JoinResponse()
        {
            if (FightBranch != "loud" || Tick < SecurityArrives) return;
            for (int k = 0; k < SecGuns.Length; k++)
            {
                if (secJoined[k] || SecGuns[k].Down) continue;
                var pose = SecOfficerPose(k); if (!pose.HasValue) continue;
                var from = pose.Value.pos;
                string stand = ""; long bd = long.MaxValue;
                foreach (var st in CurStand.Police) { long d = G.D2(from, S.Stand(st)); if (d < bd || (d == bd && string.CompareOrdinal(st, stand) < 0)) { bd = d; stand = st; } } // brief 059 K6: the stand's police stands
                var sp = S.Stand(stand);
                bool taken = false;
                foreach (var o in Hostiles) if (!o.Down && o.Home == sp) taken = true;
                standOverflow.TryGetValue(stand, out int overflow);
                Pt home = sp; string cov = "";
                if (taken)
                {
                    CoverPoint? best = null; long cd = long.MaxValue;
                    foreach (var c in LiveCover)
                    {
                        if (coverTaken.ContainsKey(c.Id) || c.Q < R.CoverQPartial || !c.Present(Tick / Hz)) continue;
                        long d = G.D2(sp, c.At); if (d > (long)R.SeekPoliceCm * R.SeekPoliceCm) continue;
                        if (d < cd || (d == cd && best != null && string.CompareOrdinal(c.Id, best.Id) < 0)) { cd = d; best = c; }
                    }
                    if (best != null) { home = best.At; cov = best.Id; }
                    else
                    {
                        long vx = Car.X - sp.X, vy = Car.Y - sp.Y, len = Math.Max(1, G.Isqrt(vx * vx + vy * vy)); int side = overflow % 2 == 0 ? 1 : -1; int step = 1 + overflow / 2;
                        var slot = new Pt(sp.X + (int)(-vy * R.OverflowCm * step * side / len), sp.Y + (int)(vx * R.OverflowCm * step * side / len));
                        home = Walk.NearestFree(slot) ?? sp; standOverflow[stand] = overflow + 1;
                    }
                }
                var gun = SecGuns[k]; gun.Engaged = true; gun.St = "advance"; LoseTarget(gun);
                var h = new Hostile { Id = gun.Id, At = from, Speed = secSpeed, Present = true, Gun = gun, Home = home };
                Hostiles.Add(h); secJoined[k] = true;
                if (cov.Length > 0) TakeCover(gun, CoverById(cov)!);
                MoveToFight(h, home); // engine 0.34.0 (Motive M067-SQ1): to its stand round the car's hold (h.Why "post")
                Note("ai.join", $"{gun.Id} of the response unit joins the fight at {(taken ? (cov.Length > 0 ? cov : "a slot beside " + stand) : stand)}.");
            }
        }
    }
}
