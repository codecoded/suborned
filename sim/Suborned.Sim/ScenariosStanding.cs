// Brief 014 (Keel, 2026-10-16; Sky's approval of briefs/014/motive-standing.md): Standing leans Hero. One scenario a rule, on the real
// block and the promoted cast. The word moves Standing only between missions, so every in-mission price stays as it was.
//  N88 help spreads: route A's Help ×2 on Inez is private (no move in the mission); she tells her ties: +1, the Hero band next mission
//  N89 harm spreads: a Pressure in sight moves it at once (−1) and Inez's word does again (−1): 0, Motive's route A Pressure row
//  N90 harm spreads from a knockout: the guard, knocked out unseen, comes home and tells: −1, the cause names him
//  N91 the cap: three people helped, the word nets +3 and moves the district +2
//  N92, N92b drift both ways: a district where nothing moved steps back toward the start, from the Hero side and from the Menace side, never past it
//  N93 helped and pressured counts as pressured: one Help (not yet), then a Pressure nobody sees: the word is −1, and the Help adds nothing
//  N94 the start is +2: neutral prices in the mission, and nothing moved means steady at +2
//  N95-N101 a public fight moves Standing once, when it ends, by whom the cell fought (Sky 2026-10-16, via Aster; fight.public, replacing
//            0.6.1's per-target gunfire.heard): feared rivals driven off +1 (the safe final fight), a rival fight not won 0, firing first on
//            neutral rivals −1, a fight with security −1, a mixed fight −1, a fight nobody heard 0, and two fights 30 s apart two moves
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static Start Knowing(params string[] roles)
        {
            var s = new Start { FinalFight = false, Cash = 1_000_000 };
            foreach (var role in roles) s.Known[role] = new List<string> { "routine", "need", "temperament" };
            return s;
        }
        static Start StartingAt(int standing) { var s = new Start { FinalFight = false, Standing = standing }; return s; }

        /// <summary>The Fixer stands within approach reach of a role and helps, reading the card as a player would, until the person turns.</summary>
        static bool HelpRole(Run r, string role, int most = 3)
        {
            var g = r.G; var p = g.PersonByRole(role);
            r.ForHer("turn", role); // brief 022: a server is met at her post
            var at = g.PersonPos(p); if (!r.Is(at.HasValue, $"{role} is on the street at {Report.Clock(g.Tick)}")) return false;
            if (!G.Within(r.Op(Fixer).Pos, at!.Value, g.R.ApproachReachCm)) r.To(r.SendTo(Fixer, g.S.Grid.NearestFree(at.Value)!.Value));
            HelpUntilTurned(r, role, most);
            return r.Is(p.Has(g.R.HelpedAct), $"{role} was helped");
        }
        static StandingLine OneLine(Run r, Between next)
        {
            r.Eq(next.Standing.Count, 1, "one Standing line: the Handover plays in one district");
            r.Is(next.Lines.Count(l => l.Line == "standing") == 1, "and one debrief line for it");
            return next.Standing[0];
        }

        static void StandingCases()
        {
            Add("N88", "—", "Help spreads (Sky 2026-10-16: 'if you help in a good way then can move to hero'; briefs/014/motive-standing.md rule 2): route A's opening, Inez helped until she turns. The Help is private, so nothing moves in the mission and every price is the neutral one. Between missions Inez tells her ties a stranger helped her: +1, once however many ties she has. From +2 the district reaches +3, the Hero band, for the next mission. The word is a feeling, not a fact: nobody's memory changes.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard"));
                HelpUntilTurned(r, Inez);
                r.Is(g.Inez.Turned && g.Inez.Count(g.R.HelpedAct) >= 1, $"Inez helped ×{g.Inez.Count(g.R.HelpedAct)} and turned");
                r.Eq(g.Standing, g.R.StandingStart, "the Help is private: Standing stays at the start in the mission");
                r.Eq(g.StandingMoves.Count, 0, "nothing moved it");
                int ties = g.People2!.Ties[g.Inez.Id].Count(t => g.R.WordTies.Contains(t.Item1));
                r.Is(ties >= 2, $"she has {ties} family, lover or workmate ties");
                var before = g.Walkers.Select(w => Hash.Person(w.P)).ToList();
                var next = g.BetweenMissions(g.Investigate());
                var l = OneLine(r, next);
                r.Is(next.Word.Count == 1 && next.Word[0] == (Inez, "hero"), "one teller: Inez, toward Hero");
                r.Eq((l.From, l.End, l.Word, l.Drift, l.To), (2, 2, 1, 0, 3), "from +2: her word +1 (once, not once per tie), no drift: +3");
                r.Eq((l.Band, l.Cause, l.Who), ("hero", "word-helped", Inez), "the Hero band, because a helped person told her ties");
                r.Eq(g.R.BandOf(l.To), Band.Hero, "the next mission starts in Hero prices");
                r.Is(g.Walkers.Select(w => Hash.Person(w.P)).SequenceEqual(before), "a feeling, not a fact: nobody in the city holds anything new");
                r.Is(!next.Lines.Exists(l2 => l2.Line == "word" && l2.Who == Inez && l2.Detail.Contains("help")), "no fact of the Help goes along her ties");
            });

            Add("N89", "—", "Harm spreads (briefs/014/motive-standing.md rules 3 and 4; Motive's route A Pressure row): the Fixer presses Inez with customers in sight. The threat seen moves Standing at once (−1, as built); between missions Inez tells her ties (−1). From +2 the district ends at 0: neutral. The cause is the earliest of two equal moves, the threat seen.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "d:Courtyard")); r.Do(Scout, "watch", Inez); r.Done(Scout);
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "listen", Inez); r.Done(Scout);
                r.To(r.Send(Fixer, "d:Courtyard"));
                r.Is(g.Preview("pressure", Inez).Witnesses > 0, "customers are in sight");
                r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer);
                r.Eq(g.Standing, g.R.StandingStart - g.R.StandingStep, "the threat seen: one step toward Menace at once");
                r.Is(g.StandingMoves.Count == 1 && g.StandingMoves[0].cause == "seen-threat" && g.StandingMoves[0].who == Inez, "its cause: a threat seen, done to Inez");
                var next = g.BetweenMissions(g.Investigate());
                var l = OneLine(r, next);
                r.Is(next.Word.Count == 1 && next.Word[0] == (Inez, "menace"), "Inez tells her ties: toward Menace");
                r.Eq((l.From, l.End, l.Word, l.Drift, l.To), (2, 1, -1, 0, 0), "+2, the threat −1, her word −1: 0");
                r.Eq((l.Band, l.Cause, l.Who), ("neutral", "seen-threat", Inez), "neutral; the earliest of the two equal causes: the threat seen");
            });

            Add("N90", "—", "Harm spreads from a knockout (briefs/014/motive-standing.md rule 3; Motive's route G knockout row): the guard knocked out from beside after his 2:30 mark, nobody seeing it (N46's opening). No Standing moves in the mission. He wakes and comes home; between missions he tells his ties: −1, from +2 to +1, and the line names him.", r =>
            {
                var g = r.G;
                GuardQuiet(r);
                r.Takedown("guard", "knockout"); r.Done(Enf);
                var a = ActOn(r, GuardId);
                r.Is(!a.Witnessed && a.Noticed.Count == 0, "nobody saw it");
                r.Eq(g.Standing, g.R.StandingStart, "no Standing in the mission");
                r.To(T(400));
                var next = g.BetweenMissions(g.Investigate());
                var l = OneLine(r, next);
                r.Is(next.Word.Count == 1 && next.Word[0] == (GuardR, "menace"), "the guard tells his ties: toward Menace");
                r.Eq((l.From, l.End, l.Word, l.Drift, l.To), (2, 2, -1, 0, 1), "+2, his word −1: +1");
                r.Eq((l.Band, l.Cause, l.Who), ("neutral", "word-knocked-out", GuardR), "neutral, because a knocked-out man came home and told");
            });

            Add("N91", "—", "The cap (briefs/014/motive-standing.md rule 2: ±2 per district per mission): the Fixer helps the driver, Inez and the guard (their needs told before the mission, cash to spare, no final fight). Three helped people tell their ties: +3, clamped to +2. From +2 the district reaches +4. A Loyal guard reports the approach: an incident for Alert, no Standing.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Boarding pavilion"));
                HelpRole(r, Driver);
                HelpRole(r, Inez);
                HelpRole(r, GuardR);
                r.Eq(g.Standing, g.R.StandingStart, "every Help is private: no move in the mission");
                var next = g.BetweenMissions(g.Investigate());
                var l = OneLine(r, next);
                r.Eq(next.Word.Count(w => w.toward == "hero"), 3, "three tellers, toward Hero");
                r.Eq((l.WordUncapped, l.Word), (3, g.R.WordStandingCap), "the word nets +3 and moves the district +2 (the cap)");
                r.Eq((l.From, l.To, l.Drift), (2, 4, 0), "+2 → +4");
                r.Eq((l.Band, l.Cause, l.Who), ("hero", "word-helped", Inez), "Hero, because helped people told their ties (the line names the first teller by person id: Inez, p-cafe-14)");
            }, Knowing(Driver, Inez, GuardR));

            Add("N92", "—", "Drift down (briefs/014/motive-standing.md rule 5): a district that starts at +5 (a campaign's Hero) and where nothing moves Standing this mission steps 1 back toward +2: +4, still Hero. Unkept Hero fades.", r =>
            {
                var g = r.G;
                r.Eq(g.Standing, 5, "the start a campaign handed on");
                r.To(T(120));
                var l = OneLine(r, g.BetweenMissions(g.Investigate()));
                r.Eq((l.From, l.End, l.Word, l.Drift, l.To), (5, 5, 0, -g.R.StandingDrift, 4), "nothing moved it: one step back toward the start");
                r.Eq((l.Band, l.Cause, l.Who), ("hero", "drift-down", ""), "still Hero; the cause is the drift");
            }, StartingAt(5));

            Add("N92b", "—", "Drift up, and never past the start (briefs/014/motive-standing.md rule 5): a Menace district (−3) where nothing moves Standing steps to −2, out of the Menace band; one at +3 steps to +2 and one at +2 holds.", r =>
            {
                var g = r.G;
                r.To(T(120));
                var l = OneLine(r, g.BetweenMissions(g.Investigate()));
                r.Eq((l.From, l.Drift, l.To), (-3, g.R.StandingDrift, -2), "−3 → −2: the city forgives a step");
                r.Eq((l.Band, l.Cause), ("neutral", "drift-up"), "out of the Menace band");
                foreach (var (from, to, cause) in new[] { (3, 2, "drift-down"), (2, 2, "steady") })
                {
                    var s = StartingAt(from); var g2 = new Game(g.R, g.S, s, null, g.People2); g2.RunTo(T(120));
                    var l2 = g2.BetweenMissions(g2.Investigate()).Standing[0];
                    r.Eq((l2.To, l2.Cause), (to, cause), $"from {from:+0;-0;0}: never past the start");
                }
            }, StartingAt(-3));

            Add("N93", "—", "Helped and pressured counts as pressured (briefs/014/motive-standing.md rule 3: a threat outweighs a kindness). Inez, Fearful, takes one Help (not yet turned), then the Fixer presses her with nobody in sight (no crowd in this scenario's world). Nothing moves in the mission; between missions her word is −1 and the Help adds nothing: +1.", r =>
            {
                var g = r.G;
                r.To(r.Send(Fixer, "d:Courtyard"));
                var q = g.Preview("help", Inez);
                r.Do(Fixer, "turn", Inez, "help"); r.Done(Fixer);
                r.Is(g.Inez.Has(g.R.HelpedAct) && !g.Inez.Turned, $"one Help landed and she is not yet turned (the card said {q.Outcome}, ×{q.Orders})");
                r.Is(g.Preview("pressure", Inez).Witnesses == 0, "nobody would see the threat");
                r.Do(Fixer, "turn", Inez, "pressure"); r.Done(Fixer);
                r.Is(g.Inez.Has("pressure"), "the Pressure landed");
                r.Eq(g.Standing, g.R.StandingStart, "nothing seen: no move in the mission");
                var next = g.BetweenMissions(g.Investigate());
                var l = OneLine(r, next);
                r.Is(next.Word.Count == 1 && next.Word[0] == (Inez, "menace"), "she tells it as a threat, never as a kindness");
                r.Eq((l.Word, l.To, l.Cause, l.Who), (-1, 1, "word-pressured", Inez), "+2 → +1, because a pressured person told her ties");
            }, Knowing(Inez).Also(s => s.Crowd = false));

            Add("N94", "—", "The start is +2 (Sky 2026-10-16: 'Hero is kinda the default'; briefs/014/motive-standing.md rule 1): one step short of the Hero band, so the mission's prices are neutral (Inez's Help and Pay cards carry no Hero line) and Heat adds nothing from Standing. A mission where nothing moves it ends steady at +2.", r =>
            {
                var g = r.G;
                r.Eq((g.R.StandingStart, g.StandingAtStart, g.Standing), (2, 2, 2), "standing.start is +2, and the mission starts there");
                r.Eq(g.R.BandOf(g.Standing), Band.Neutral, "neutral: Hero begins at +3, Menace at −3");
                r.Eq((g.R.HeroAt, g.R.MenaceAt), (3, -3), "the bands are unchanged");
                foreach (var a in new[] { "help", "pay", "ask", "pressure" })
                    r.Is(!g.Preview(a, Inez).Lines.Exists(x => (x.Line == "hero-overcharge" || x.Line == "menace-discount") && x.Holds != "no"), $"the {a} card holds no Standing line");
                r.Eq(Math.Max(0, -g.Standing), 0, "Heat takes nothing from Standing at the start (Trail + max(0, −Standing))");
                r.To(T(120));
                var l = OneLine(r, g.BetweenMissions(g.Investigate()));
                r.Eq((l.From, l.To, l.Drift, l.Cause), (2, 2, 0, "steady"), "nothing moved: steady at +2");
            }, Knowing(Inez));

            PublicFightCases();
        }
        static Start Also(this Start s, Action<Start> f) { f(s); return s; }
    }
}
