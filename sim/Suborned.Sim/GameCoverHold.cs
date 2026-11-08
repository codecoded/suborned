// Brief 098 (engine 0.50.0; Aster's ruling on the 0.49.0 report, on Sky's "We can do better"): fighters hold cover (rulebook fight.cover).
// One law for every fighter in a fight: the rival crew, police officers, the response unit, police units. The blocker keeps his lane.
//  · Holding: a fighter who reaches cover that shields him from the threat holds it while it still does. He leaves it only for a reason:
//    flanked or exposed (FightBody: unshielded from his target for ai.cover.leave; KiteBody: a shooter he can't answer whose line it doesn't
//    shield), his crew's plan (Advance, AdvanceOnOp, the press, the walk to a held car, the break), his target out of reach or sight for
//    ai.engageLapse (the advance, which closes: GameSquare.cs), or the cover lost (CoverById finds it no longer standing).
//  · Committing: a move out of a shooter's line commits him to where he moved (Gun.Commit). On the walk there and once he stands there, he
//    takes no new step out of that shooter's line while the place still shields him from where the shooter stands now, nor out of another
//    shooter's line the place shields him from too. Any other walk (an advance, a stop to fight, the press) spends it.
//  · Fighting from cover: a fighter whose target is in his reach steps only to cover within fight.cover.reach of his effective reach of that
//    target, with the line to him (FightFromCover). With none, he fights from where he stands.
//  · Walking a move through: an advance (Advance, AdvanceOnOp) and a police body's close (§7.10 rule 2) are never turned back by a step out of
//    a line (Hostile.Closing, KiteTick); on the walk of a move out of a line, his target out of reach is no reason yet (FightBody's lapse).
// Why (quiet-g seed 1 on 0.49.0): rival-1 engaged the Tech at 15.1 m in the open and stepped 5.6 m back to cv-035, out of his own reach; shot
// at on that walk, the step out of the line turned him to cv-036; with nobody in reach he advanced again. Six times from 5:17 to 6:34, while
// the car stayed boxed in. Integers only; no draw.
using System;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>He moved out of `by`'s line toward `dest`: committed to where his walk ends, holding cover point `c` (null: a blind point). With
        /// no walk (no route) and not already there, nothing is committed.</summary>
        void CommitTo(Hostile h, string by, Pt dest, CoverPoint? c)
        {
            var g = h.Gun;
            if (h.Run == null && !G.Within(h.At, dest, R.CoverSnapCm)) { CommitEnd(g); return; }
            g.Commit = by; g.CommitAt = h.At; g.CommitCover = c?.Id ?? "";
        }
        /// <summary>The same for a police unit walked off its post (KiteDisp): committed to its path's end.</summary>
        static void CommitUnit(Gun g, string by, Pt end, CoverPoint? c) { g.Commit = by; g.CommitAt = end; g.CommitCover = c?.Id ?? ""; }
        static void CommitEnd(Gun g) { if (g.Commit.Length == 0) return; g.Commit = ""; g.CommitCover = ""; g.CommitAt = default; }

        /// <summary>The place he is committed to shields him from a shooter at `sp`: its cover point stands and shields him from there, or (a blind
        /// point) the shooter has no line to it.</summary>
        bool CommitShields(Gun g, Pt sp)
        {
            if (g.CommitCover.Length == 0) return !Sees(g.CommitAt, sp);
            var c = CoverById(g.CommitCover); return c != null && c.Present(Tick / Hz) && CoverRules.Protects(c, sp, Tick / Hz, R.PointBlankCm);
        }

        /// <summary>The commitment holds against a shooter at `sp`: he is still on his walk there or stands there, and the place still shields him
        /// from `sp`. Spent (he walked elsewhere or stands elsewhere), it ends here.</summary>
        bool CommitHolds(Hostile h, Pt sp)
        {
            var g = h.Gun; if (g.Commit.Length == 0) return false;
            bool bound = h.Moving(Tick) ? h.Run!.End == g.CommitAt : G.Within(h.Pos(Tick), g.CommitAt, R.CoverSnapCm);
            if (!bound) { CommitEnd(g); return false; }
            return CommitShields(g, sp);
        }
        /// <summary>The same for a police unit: its walk off its post still ends there.</summary>
        bool CommitHoldsUnit(Unit u, Pt sp)
        {
            var g = u.Gun; if (g.Commit.Length == 0) return false;
            if (!unitDisp.TryGetValue(u.Id, out var d) || d.P.End != g.CommitAt) { CommitEnd(g); return false; }
            return CommitShields(g, sp);
        }

        /// <summary>He walks to cover point c: his walk ends beside it.</summary>
        bool OnWayTo(Hostile h, CoverPoint c) => h.Moving(Tick) && G.Within(h.Run!.End, c.At, R.CoverSnapCm);

        /// <summary>The cover a fighter whose target (at `target`) is in his reach steps to: of the points the cover rule gives against the target
        /// (SQ1: never in the car's hold or on its lane), in its order, the first within fight.cover.reach of his effective reach of the target,
        /// with the line to him. Null for none: he fights from where he stands.</summary>
        CoverPoint? FightFromCover(Pt pos, Pt target, int effCm)
        {
            if (LiveCover.Count == 0) return null;
            long reach = (long)effCm * R.CoverHoldReachPerMille / 1000; var skip = TakenOrHold();
            for (;;)
            {
                var c = CoverRules.Select(LiveCover, pos, Car, target, skip, R.SeekRivalCm, Tick / Hz, R.PointBlankCm, R.CoverQPartial, R.CoverRetreatCm, R.CoverStepCloserCm);
                if (c == null) return null;
                if (G.Dist(c.At, target) <= reach && Sees(c.At, target)) return c;
                skip.Add(c.Id); // the next in the cover rule's order
            }
        }

        /// <summary>A rival shot at by a shooter he can't answer steps out of the line (fight.kite; SQ1: never into the car's hold or onto its lane):
        /// to the cover that shields him from the shooter, else the nearest blind point within 8 m; with no hide, an advance to his ring of the
        /// shooter (retest R4; not while pinned). At most once each ai.cover.stepGap, never while where he stands shields him. Engine 0.50.0
        /// (fight.cover): committed to where he moved, he takes no new step while the place still shields him from where the shooter stands now,
        /// and a walk to a blind point or an advance holds no cover point.</summary>
        void KiteRivalStep(Gun g, Pt pos, Pt sp, Hostile h, int eff)
        {
            if (Tick - g.StepAt < T(R.CoverStepGapS) || Protected(pos, sp)) return;
            if (h.Blocker) return; // Motive M059-B2 (engine 0.33.0; GDD §06): a rival blocker keeps its lane and answers only from it, as the police blocker does
            if (CommitHolds(h, sp)) return; // engine 0.50.0: committed to where he moved
            g.StepAt = Tick;
            var dest = KiteHideOut(g, pos, sp, Car, out var c); bool hide = dest.HasValue; // engine 0.34.0 (Motive M067-SQ1): the step out of a line never ends in the car's hold or lane
            if (!dest.HasValue && !PinHolds(g)) { var sp2 = StopPoint(pos, sp, eff); if (sp2 != pos && !HoldsCar(sp2)) dest = sp2; } // brief 039: pinned, no advance; retest R4 (Pulse §5): no hide, it advances to its ring of the shooter (SQ1: not into the hold; else it stands)
            if (!dest.HasValue) return;
            if (g.KiteLast < 0) Note("ai.kite", $"{g.Id} answers {g.KiteBy}: first shot at {Sim.Report.Clock(g.KiteAt)}, moves at {Sim.Report.Clock(Tick)} ({Tick - g.KiteAt} ticks), {(c != null ? $"out of the line to {c.Id}" : "out of the line")}.");
            g.KiteLast = Tick;
            if (c != null) TakeCover(g, c); else ReleaseCover(g);
            MoveTo(h, dest.Value, true); h.Why = "answer";
            if (hide) CommitTo(h, g.KiteBy, dest.Value, c);
        }
    }
}
