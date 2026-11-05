// The crew as people to talk to (engine 0.41.0, brief 096; Motive briefs/096/motive/rivals.html §1-2, rivals.json lookoutJob, thinning, crew;
// Sky 2026-11-04 "all recommended"; rulebook rivals.turn, rivals.lookout, cell.mislead). Each of the five has a temperament, Mercenary or Loyal,
// drawn by his id alone (never the game's seed), no kind over rivals.turn.maxShare of the crew. On a member on foot and posted:
//  · Watch (any agent): as a Watch on the rival car, the crew's timetable (RivalNext).
//  · Listen (the Scout; scout.listen's time and reach; private): teaches his temperament (rivals.listen.heard).
//  · Pay (the Fixer; rivals.turn: $30,000, 35 s, act reach; private; engine 0.43.0: a Watch on the crew first, rivals.turn.needs): a Mercenary takes it (+2 T) and walks off the map, gone for the mission and
//    never recalled; a Loyal man refuses, the money stays with the cell, the refusal is remembered and he tells his crew: at the spring every
//    member counts the Fixer a foe. No need record: Ask, Help and Pressure are refused (crew-no-need).
//  · Mislead (the Scout; cell.mislead: 30 s, +1 T, act reach): the lookout to the target's other door, a posted man to the lookout's post; he
//    watches it rivals.lookout.misled s and walks back. Still able, he joins the final fight from wherever he is; the lookout comes back
//    knowing the Scout's face (a foe at the spring). The false tip on the rival car is retired (Motive Q2 a: tip-retired).
// Out of reach, the agent walks in (cell.approach) and the act starts once in reach, as on any person; a driven agent is never walked (too-far).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>The temperaments by id (Streams.Fnv1a32 of "rival-crew/id/temperament", the kind in rivals.turn.kinds order), then the cap: while
        /// a kind holds more than maxShare of the crew, its highest id flips. A test's Start.Temperament may name a member (test only).</summary>
        void DrawTemperaments()
        {
            foreach (var m in Crew) m.Temperament = Cr.Kinds[(int)(Streams.Fnv1a32("rival-crew/" + m.Id + "/temperament") % (uint)Cr.Kinds.Length)];
            int max = Cr.Size * Cr.TemperMaxPct / 100;
            foreach (var k in Cr.Kinds)
                for (int i = Crew.Count - 1; i >= 0 && Crew.FindAll(x => x.Temperament == k).Count > max; i--)
                    if (Crew[i].Temperament == k) Crew[i].Temperament = Array.Find(Cr.Kinds, x => x != k)!;
            foreach (var m in Crew) if (Start.Temperament.TryGetValue(m.Id, out var t)) { if (Array.IndexOf(Cr.Kinds, t) < 0) throw new FormatException($"start temperament {m.Id}: {t} is not one of {string.Join(", ", Cr.Kinds)}"); m.Temperament = t; }
        }
        /// <summary>Host read: a member's temperament as the cell knows it: mercenary, loyal, or "?" before a Listen (or a refusal) taught it.</summary>
        public string CrewTemperament(string id) => CrewOf(id) is CrewMember m ? (m.KTemperament ? m.Temperament : "?") : "";
        /// <summary>Host read: a member was paid off (gone for good), refused a Pay, or was misled and is away.</summary>
        public bool CrewPaid(string id) => CrewOf(id)?.Paid ?? false;

        /// <summary>Money in the log's words, the same in every culture (Redline E7): 30,000.</summary>
        static string Usd(long v) => v.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture);
        /// <summary>The person a turn names: a member of the crew by id, else the cast by role.</summary>
        Person TurnSubject(string target) => IsCrewId(target) && !Range ? CrewOf(target)!.P : PersonByRole(target);

        /// <summary>The target's other door (the mission's data): the public entrance of the target's building nearest the door he leaves by.</summary>
        Pt OtherDoor()
        {
            var exit = S.At(S.ValeExit); (string id, string building, string kind, Pt at, Pt stand)? own = null; long bd = long.MaxValue;
            foreach (var e in S.Entrances) { long d = G.D2(e.at, exit); if (d < bd) { bd = d; own = e; } }
            if (own == null) throw new FormatException("rivals.lookout: the site has no entrances, so the lookout has no other door to be misled to");
            Pt? best = null; bd = long.MaxValue;
            foreach (var e in S.Entrances) { if (e.id == own.Value.id || e.building != own.Value.building || e.kind != "public") continue; long d = G.D2(e.at, own.Value.at); if (d < bd) { bd = d; best = e.stand; } }
            return best ?? throw new FormatException($"rivals.lookout: building {own.Value.building} has no public door besides {own.Value.id}, so the lookout has no other door to be misled to");
        }
        /// <summary>Where a misled member is sent: the lookout to the target's other door, a posted man to the lookout's post.</summary>
        Pt MisledTo(CrewMember m) => m == Lookout ? OtherDoor() : S.Stand(S.RivalLookout);
        /// <summary>Where a misled member walks back to: the lookout's post, or his spot round the cell's car.</summary>
        Pt MisledHome(CrewMember m) => m == Lookout ? S.Stand(S.RivalLookout) : S.Stand(CrewStand.Ambush[Math.Max(0, m.Spot)]);
        int WalkTicks(Pt a, Pt b, int speed) { long mm = WalkMm(a, b); return mm >= long.MaxValue / 8 ? 0 : (int)((mm + speed - 1) / Math.Max(1, speed)); }
        /// <summary>The tick a misled member, watching now, will be back at his post.</summary>
        int MisledBackTick(CrewMember m) => Math.Max(Tick, m.Until) + WalkTicks(m.Place, MisledHome(m), rivSpeed);
        /// <summary>Host read (the Mislead card's "back at his post m:ss"): the tick this member would be back if the Scout's Mislead were given now
        /// in place (cell.mislead.s, then the walk there, rivals.lookout.misled, the walk back); -1 for no member on foot.</summary>
        public int MisleadBackTick(string id)
        {
            var m = CrewOf(id); var p = m == null ? null : MemberPose(m); if (m == null || !p.HasValue) return -1;
            var to = MisledTo(m); return Tick + T(R.MisleadS) + WalkTicks(p.Value.pos, to, rivSpeed) + T(Cr.MisledS) + WalkTicks(to, MisledHome(m), rivSpeed);
        }
        /// <summary>A misled member's watch ends: he walks back.</summary>
        void MisledBack(CrewMember m) { m.Until = INF; StartWalk(m, m.Place, MisledHome(m), "back", rivSpeed); Note("rivals.misled-back", $"{m.Id} gives up on the word and walks back."); }

        // ---------------------------------------------------------------- the orders on a member

        /// <summary>The member an order names, there to be spoken to: up, on foot and posted (not walking, not in the final fight, not gone); "" else.</summary>
        string CrewPresent(string id, out CrewMember m, out Pt at)
        {
            m = CrewOf(id)!; at = default;
            if (!Up(m)) return m.Paid ? No("crew-gone", $"{id} took the money and is gone") : m.Gun.Down ? No("person-down", $"{id} is down") : No("person-away", $"{id} is not on the street");
            if (m.Fight) return No("crew-fighting", $"{id} is in the fight");
            var p = MemberPose(m); if (!p.HasValue) return No("person-away", $"{id} is in the rival car, off the street");
            if (m.Mode != CrewMode.Posted) return No("crew-walking", $"{id} is on the move: speak to him where he stands");
            at = p.Value.pos; return "";
        }

        /// <summary>A Watch or a Listen on a member (Scout's route for crew ids).</summary>
        string CrewLearn(Op op, Order o, bool engage)
        {
            if (o.Do == "listen" && op.Id != OpId.Scout) return No("only-scout", "only the Scout listens (§03)");
            var miss = CrewPresent(o.Target, out var m, out var at); if (miss.Length > 0) return miss;
            if (o.Do == "watch")
            {
                if (RivalsWatched) return No("already-watched", "the cell knows the rivals' timetable already: a Watch teaches nothing more");
                int wr = R.WatchReachCm(op.Id);
                if (!G.Within(op.Pos, at, wr)) return CrewWalkIn(op, o, m, wr, engage);
                if (!Sees(op.Pos, at)) return No("no-clear-line", $"no clear line: move to where the {Sim.Op.Name(op.Id)} can see {m.Id}");
                if (!engage) return "";
                Engage(op, o, R.SightWatchS); op.BusyQuote = null; op.BusyPaid = 0; return "";
            }
            if (m.KTemperament) return No("already-heard", $"the cell knows {m.Id}'s temperament already: a Listen teaches nothing more");
            if (!G.Within(op.Pos, at, listenReach)) return CrewWalkIn(op, o, m, listenReach, engage);
            if (!engage) return "";
            Engage(op, o, (int)R.Num("v:listen-s")); op.BusyQuote = CrewQuote(o, m); op.BusyPaid = 0;
            return "";
        }

        /// <summary>A turn on a member: Pay only (rivals.turn); the rest need a need record the crew don't have.</summary>
        string CrewTurn(Op op, Order o, bool engage = true) // engage false: CrewActQuery (GameCrewQuery.cs)
        {
            if (op.Id != OpId.Fixer) return No("only-fixer", "only the Fixer turns people (§03)");
            if (o.Approach != "pay") return No("crew-no-need", $"the rival crew have no need to meet: only Pay is offered on {o.Target}");
            var miss = CrewPresent(o.Target, out var m, out var at); if (miss.Length > 0) return miss;
            if (m.Refused) return No("refused-before", $"{m.Id} refused the money once: he won't take it now");
            if (Cr.PayNeedsWatch && !RivalsWatched) return No("needs-routine", $"watch the crew first: a Watch on {m.Id} (or any of them, or their car) shows who they are and what they do"); // engine 0.43.0 (rivals.turn.needs; Motive's rivals page)
            if (Cash < Cr.PayUsd) return No("no-cash", $"the cell has ${Usd(Cash)}: the Pay is ${Usd(Cr.PayUsd)}");
            if (!G.Within(op.Pos, at, actReach)) return CrewWalkIn(op, o, m, actReach, engage);
            if (!engage) return "";
            Cash -= Cr.PayUsd; // paid when given; a refusal gives it back
            Engage(op, o, Cr.PayS); op.BusyQuote = CrewQuote(o, m); op.BusyPaid = Cr.PayUsd;
            return "";
        }

        /// <summary>The Scout's Mislead: a member on foot (the false tip on the rival car is retired: tip-retired).</summary>
        string CrewMislead(Op op, Order o, bool engage = true)
        {
            if (op.Id != OpId.Scout) return No("only-scout", "only the Scout misleads (§03)");
            if (o.Target.Length == 0 || o.Target == "rivals" || o.Target == "car:rivals") return No("tip-retired", "the false tip on the rival car is retired (brief 096): mislead one of the crew on foot");
            if (!IsCrewId(o.Target)) return No("target-unknown", $"Mislead names one of the rival crew on foot (rival-1 to rival-5), not '{o.Target}'");
            var miss = CrewPresent(o.Target, out var m, out var at); if (miss.Length > 0) return miss;
            if (!G.Within(op.Pos, at, actReach)) return CrewWalkIn(op, o, m, actReach, engage);
            if (!engage) return "";
            Engage(op, o, R.MisleadS); op.BusyQuote = CrewQuote(o, m); return "";
        }

        /// <summary>The walk in (cell.approach) to a member out of reach: taken, the agent walks to him (re-aimed every cell.approach.repath s) and the act
        /// starts once in reach, checked again as if given there; a driven agent is never walked (too-far). `engage` false: a query (the card).</summary>
        string CrewWalkIn(Op op, Order o, CrewMember m, int reach, bool engage)
        {
            if (!WalksIn(op)) return No("too-far", $"stand within {reach / 100} m of {m.Id}", m: reach / 100);
            var at = MemberPos(m, Tick); var to = Walk.NearestFree(at); var rt = to.HasValue ? Walk.Route(op.Pos, to.Value) : null;
            if (rt == null) return No("no-walk", $"no walk to {m.Id}");
            if (!engage) return "";
            if (CoverSnapOn) DropCover(op);
            op.Walk = new Path(rt); if (op.Walk.Length == 0) op.Walk = null; op.WalkMm = 0; op.CloseWalk = false; op.Running = false; op.WalkSpeed = PaceOf(op, false);
            op.Act = o; op.ActNext = Tick + T(R.ApproachRepathS);
            Note("order.walk-in", $"{Sim.Op.Name(op.Id)} walks to {m.Id} to {(o.Do == "turn" ? o.Approach : o.Do)}.");
            return "";
        }
        /// <summary>ApproachStep's for a member: in reach, the act is given there (refused: dropped); else re-aimed at where he stands.</summary>
        void CrewApproachStep(Op op, Order o)
        {
            var m = CrewOf(o.Target)!; var p = MemberPose(m);
            if (!Up(m) || m.Fight || !p.HasValue) { DropAct(op, o, m.Paid ? "crew-gone" : "person-away", $"{m.Id} is not on the street"); return; }
            int reach = o.Do == "listen" ? listenReach : o.Do == "watch" ? R.WatchReachCm(op.Id) : actReach;
            if (G.Within(op.Pos, p.Value.pos, reach))
            {
                op.Act = null; op.Walk = null; op.WalkMm = 0;
                var saved = refusal; refusal = default;
                string why = o.Do == "turn" ? CrewTurn(op, o) : o.Do == "mislead" ? CrewMislead(op, o) : CrewLearn(op, o, true);
                var code = refusal.code ?? "uncoded"; refusal = saved;
                if (why.Length > 0) { Dropped.Add((Tick, op.Id, o, code, why)); Note("order.dropped", $"{Sim.Op.Name(op.Id)} {(o.Do == "turn" ? o.Approach : o.Do)} {o.Target}: {code}: {why}"); }
                return;
            }
            if (op.Walk == null || Tick >= op.ActNext)
            {
                var to = Walk.NearestFree(p.Value.pos); var rt = to.HasValue ? Walk.Route(op.Pos, to.Value) : null;
                if (rt == null) { DropAct(op, o, "no-walk", $"no walk to {m.Id}"); return; }
                op.Walk = new Path(rt); if (op.Walk.Length == 0) op.Walk = null; op.WalkMm = 0; op.ActNext = Tick + T(R.ApproachRepathS);
            }
        }

        /// <summary>The card's price of an act on a member (§02 full price): a Listen's time; a Pay's money, time and Trail, its outcome by what the
        /// cell knows of him ("?" before a Listen); a Mislead's time and Trail. A read too (Latch's card).</summary>
        public Quote CrewQuote(Order o, CrewMember m)
        {
            var q = new Quote { Event = o.Do == "turn" ? "rivals.pay" : o.Do == "listen" ? "rivals.listen" : "rivals.mislead" };
            if (o.Do == "turn") { q.Usd = q.WorstUsd = Cr.PayUsd; q.S = Cr.PayS; q.Trail = Cr.PayTrail; q.Outcome = !m.KTemperament ? "?" : m.Temperament == "mercenary" ? "turned" : "refused"; }
            else if (o.Do == "listen") { q.S = (long)R.Num("v:listen-s"); q.Outcome = "heard"; }
            else { q.S = R.MisleadS; q.Trail = R.MisleadTrail; q.Outcome = "misled"; }
            q.Lines.Add(new Quote.L { Line = "base", Usd = q.Usd, S = q.S, Trail = q.Trail, Holds = q.Outcome == "?" ? "?" : "yes" });
            return q;
        }

        /// <summary>A Listen, Pay or Mislead on a member lands (Land's route for crew ids).</summary>
        void LandCrew(Op op, Order o)
        {
            var m = CrewOf(o.Target)!; long paid = op.BusyPaid; op.BusyPaid = 0;
            bool there = Up(m) && !m.Fight && m.OnFoot;
            switch (o.Do)
            {
                case "watch": RivalsWatched = true; Note($"{Sim.Op.Name(op.Id)}.watch.watched", $"The rivals' timetable is read: their next move, where each will go, and that the lookout follows {ValeName}'s car."); return;
                case "listen":
                    if (!there) { Note("rivals.listen-missed", $"{m.Id} is gone before the Scout hears anything."); return; }
                    m.KTemperament = true;
                    Note("rivals.listen.heard", m.Temperament == "mercenary" ? $"{m.Id} is in it for the money: he would take ${Usd(Cr.PayUsd)} to walk away." : $"{m.Id} is loyal to his crew: he would refuse money and tell them.");
                    return;
                case "turn":
                    if (!there) { Cash += paid; Note("rivals.pay-missed", $"{m.Id} is gone before the Fixer can pay him. The money stays with the cell."); return; }
                    m.KTemperament = true;
                    if (m.Temperament == "mercenary")
                    {
                        m.Paid = true; AddTrail(Cr.PayTrail, "pay:" + m.Id); Actions.Add("turn.pay:" + m.Id); Chain.Add(m.Id + "-paid-off");
                        StartWalk(m, MemberPos(m, Tick), S.Stand(S.RivalLookoutExit), "exit", rivSpeed);
                        Note("rivals.paid", $"{m.Id} takes the money and walks off. He is out of it.");
                    }
                    else
                    {
                        Cash += paid; m.Refused = true; springFoes.Add(Sim.Op.Name(OpId.Fixer)); Chain.Add(m.Id + "-refused");
                        Note("rivals.refuses", $"{m.Id} won't take the money, and tells his crew about the Fixer.");
                    }
                    return;
                case "mislead":
                    if (!there || (m.Mode == CrewMode.Walking && m.Goal == "follow")) { Note("mislead.missed", there ? $"{m.Id} is already running after {ValeName}'s car: the word misses him." : $"{m.Id} is gone before the word lands."); return; }
                    m.Misled = true; m.Until = INF; AddTrail(R.MisleadTrail, "mislead:" + m.Id); Actions.Add("mislead:" + m.Id); Chain.Add(m.Id + "-misled");
                    StartWalk(m, MemberPos(m, Tick), MisledTo(m), "misled", rivSpeed);
                    Note("rivals.misled", m == Lookout ? $"The rival lookout takes the word and goes to watch the other door." : $"{m.Id} takes the word and walks off to the lookout's post.");
                    return;
            }
        }

        /// <summary>The spring (Spring): every member counts the agents his crew was told of a foe (a Loyal refusal: the Fixer), and a misled man
        /// back from the word knows the Scout's face.</summary>
        void CrewSprings()
        {
            foreach (var m in Crew)
            {
                foreach (var f in springFoes) m.Avenge.Add(f);
                if (m.Misled) m.Avenge.Add(Sim.Op.Name(OpId.Scout));
            }
        }
    }
}
