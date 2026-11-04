// Motive's lend scenarios, part 2 (engine 0.42.0, brief 096; briefs/096/motive/lend.json "scenarios"): the fight fixes, today's rules on every
// route (F1 the wheelman stays at the wheel and drives off at the break, F2 a broken man is out of the fight, F3 the break count), the veteran's
// shot (V1 a burning car's riders out at once, V2 the placing rule), and the seal after the win (LEND-16).
//  FIX-1 wheelman-stays · FIX-2 wheelman-disabled · FIX-3 broken-out · FIX-4a/b break-count · VET-1 burning-riders-out · VET-2 placed-near
//  (the scene's reach) · VET-3 placed-gun · LEND-16 after-win-seal. Not built as scenarios: LEND-14 (a downed agent carried into our car: the
//  carry and Board are 0.41.0's, unchanged, N-cases) and VET-4 (a late placing: the veteran's bands read it).
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>The three fire on the nearest rival in the fight (present, up, not broken), once a second, until done or the limit.</summary>
        static void Rearguard(Run r, Func<bool> done, int limitS)
        {
            var g = r.G; int end = g.Tick + T(limitS);
            while (!g.Over && g.Tick < end && !done())
            {
                if (g.Tick % Game.Hz == 0)
                    foreach (var op in new[] { Scout, Fixer, Tech })
                    {
                        var o = r.Op(op); if (o.Down || o.InCar || o.LeftMap || o.Busy != null || o.Gun.Order.Length > 0) continue;
                        string best = ""; long bd = long.MaxValue;
                        foreach (var h in g.Hostiles) { if (!h.Present || h.Down || h.Gun.St == "break" || h.Gun.St == "gone") continue; long d = G.D2(o.Pos, h.Pos(g.Tick)); if (d < bd) { bd = d; best = h.Id; } }
                        if (best.Length > 0) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = best });
                    }
                g.Step();
            }
        }
        /// <summary>The lend, Go on it, the three walk in on our car (the crew springs) and fight until the crew breaks; true when it broke.</summary>
        static bool LendAndBreak(Run r)
        {
            var g = r.G; if (!LendAt(r)) return false;
            g.Issue(GoOn(g, "car:official"));
            foreach (var op in new[] { Scout, Fixer, Tech }) r.SendTo(op, g.S.Grid.NearestFree(g.GetawaySeat + new Pt(1600, 0)) ?? g.GetawaySeat);
            Rearguard(r, () => g.FightWhy.Contains("rivals-break"), 90);
            return g.FightWhy.Contains("rivals-break");
        }
        static int LogTickOf(Game g, string code) { int i = g.Log.FindIndex(x => x.Code == code); return i < 0 ? -1 : g.Log[i].Tick; }

        static void Lend096b()
        {
            Add("096-FIX-1", "096", "Motive FIX-1 (F1): the wheelman stays at the wheel through the spring and the fight; the tick after the crew breaks he drives off, for good, picking nobody up: the block ends at the break.", r =>
            {
                var g = r.G; bool broke = LendAndBreak(r);
                r.Is(g.FightSprung != int.MaxValue / 4, "the crew sprang");
                if (!r.Is(broke, Iv($"the crew breaks ({g.Result} {g.FailCode} at {Report.Clock(g.Tick)})"))) return;
                int br = LogTickOf(g, "ai.break"); r.Wait(2);
                int off = LogTickOf(g, "rivals.drives-off");
                r.Is(off >= br && off <= br + 1, Iv($"he drives off at {Report.Clock(off)}, the break at {Report.Clock(br)}"));
                r.Is(!g.Log.Exists(l => l.Code == "rivals.joins" && l.Text.StartsWith("rival-5", StringComparison.Ordinal)), "he never got out");
                r.Until(() => g.GetawayBoxers().Count == 0, T(20)); r.Is(g.GetawayBoxers().Count == 0, "the block is gone");
            }, LendStart());

            Add("096-FIX-2", "096", "Motive FIX-2 (F1): the rival car disabled at its block: the wheelman gets out and fights; the block ends only by the blast, never by a drive-off.", r =>
            {
                var g = r.G; if (!r.Is(UntilBlocked(r), "the rival car blocks our car at the square")) return;
                AllFire(r, "car:rivals"); r.Until(() => g.CarDisabled("car:rivals") || g.Over, T(120));
                if (!r.Is(g.CarDisabled("car:rivals"), "disabled")) return;
                r.Until(() => g.Crew[4].Mode != CrewMode.Aboard || g.Over, T(g.R.BoardS + 2));
                r.Is(g.Crew[4].Mode != CrewMode.Aboard, Iv($"the wheelman gets out ({g.Crew[4].Mode})"));
                r.Is(g.GetawayBoxers().Contains("car:rivals"), "the disabled car still boxes ours");
                r.Until(() => g.CarOf("car:rivals")?.Blown == true || g.Over, T(120));
                r.Is(g.CarOf("car:rivals")?.Blown == true && g.GetawayBoxers().Count == 0 && !g.Chain.Contains("rivals-block-off"), "the blast frees the way; nobody drove it off");
            }, Gone(new Start(), "rival-1", "rival-2", "rival-3", "rival-4"));

            Add("096-FIX-3", "096", "Motive FIX-3 (F2): a broken man puts his gun away and runs off the map at brokenPace; he holds no car; an order to shoot him is a new shooting, traced.", r =>
            {
                var g = r.G; if (!r.Is(LendAndBreak(r), "the crew breaks")) return;
                var b = g.Hostiles.FirstOrDefault(h => h.Gun.St == "break" && !h.Down && h.Present);
                if (!r.Is(b != null, "a broken man still on the street")) return;
                r.Is(!b!.Gun.Drawn && b.Speed == g.R.Crew.BrokenCmS && b.BreakTo == g.S.Stand(g.S.RivalLookoutExit), Iv($"{b.Id}: holstered, running off the map at {b.Speed} cm/s"));
                r.Is(!g.CarHolders().Contains(b.Id), "he holds no car");
                int n0 = g.Ledger.Count(x => x.cause.StartsWith("gunfire", StringComparison.Ordinal));
                var o = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "fire", Target = b.Id }); r.Is(o.ok, Iv($"an order can still shoot him ({o.code} {o.why})"));
                r.Until(() => g.Ledger.Count(x => x.cause.StartsWith("gunfire", StringComparison.Ordinal)) > n0 || b.Gun.St == "gone" || g.Over, T(15));
                r.Line(g.Ledger.Count(x => x.cause.StartsWith("gunfire", StringComparison.Ordinal)) > n0, "the shot at him is traced (gunfire-trail)");
            }, LendStart());

            foreach (var (id, start, onFoot) in new[] { ("096-FIX-4a", LendStart(), 3), ("096-FIX-4b", Turned(Driver), 4) })
                Add(id, "096", $"Motive FIX-4 (F3): {onFoot} on foot in the fight (the wheelman at the wheel not counted): the crew breaks at {onFoot / 2 + 1} down.", r =>
                {
                    var g = r.G; if (onFoot == 4) DealLookout(r, "mislead", 300);
                    if (!r.Is(LendAndBreak(r), Iv($"the crew breaks ({g.Result} {g.FailCode})"))) return;
                    var rivals = g.Hostiles.Where(h => h.Id.StartsWith("rival-", StringComparison.Ordinal) && h.Id != "rival-5").ToList();
                    int down = rivals.Count(h => h.Down);
                    r.Eq((rivals.Count, down), (onFoot, onFoot / 2 + 1), "in the fight and down at the break");
                }, start);

            Add("096-VET-1", "096", "Motive VET-1 (V1): the incoming rival car shot to 0 HP: every rider still aboard gets out at once, unhurt.", r =>
            {
                var g = r.G; if (!r.Is(VeteranShot(r), "the car comes within 20 m on its way in")) return;
                r.Until(() => g.CarBurning("car:rivals") || g.Over, T(90));
                if (!r.Is(g.CarBurning("car:rivals"), Iv($"burning at {Report.Clock(g.Tick)}"))) return;
                var aboard = g.Crew.Where(m => m.Mode == CrewMode.Aboard && !m.Gun.Down).ToList();
                r.Is(aboard.All(m => m.OutAt == g.Tick), Iv($"{aboard.Count} still aboard, all out this tick"));
                r.Wait(1); r.Is(aboard.All(m => m.Mode != CrewMode.Aboard && m.Gun.Health == m.Gun.Max), "out, unhurt");
            });

            Add("096-VET-2", "096", "Motive VET-2 (V2 b): the gang shooting's scene is where the shots came from and where the rival car stopped; an agent 25 m from it is near (placed if the response sees him in the hold), 35 m is not.", r =>
            {
                var g = r.G; if (!r.Is(VeteranShot(r), "the shot")) return;
                r.Until(() => g.CarDisabled("car:rivals") || g.Over, T(60));
                r.Line(g.GangPending, $"seed 0's corner: filed as a gang shooting ({(g.GangPending ? "security.gang" : "seen: " + g.GangClaimedWhy)})");
                var sc = g.GangScenePoints; if (!r.Is(sc.Count > 0, Iv($"the scene: {sc.Count} points"))) return;
                var c = new Pt((int)sc.Average(p => p.X), (int)sc.Average(p => p.Y)); var q = sc.OrderByDescending(p => G.D2(p, c)).First();
                var dir = q - c; long n = Math.Max(1, G.Isqrt((long)dir.X * dir.X + (long)dir.Y * dir.Y)); if (n < 10) { dir = new Pt(100, 0); n = 100; }
                Pt At(int cm) => new Pt(q.X + (int)(dir.X * cm / n), q.Y + (int)(dir.Y * cm / n));
                r.Is(g.NearGangScene(At(2500)), "25 m out from the scene's edge: near");
                r.Is(!g.NearGangScene(At(3500)), "35 m out: not near");
            });

            Add("096-VET-3", "096", "Motive VET-3 (V2 a): after a gang shooting, a weapon drawn in security's sight places the cell, anywhere on the map.", r =>
            {
                var g = r.G; if (!r.Is(VeteranShot(r), "the shot")) return;
                r.Until(() => g.Gunfire > 0 || g.Over, T(20));
                if (!g.GangPending) { r.Line(false, $"seed 0's corner was seen ({g.GangClaimedWhy}): nothing to place"); return; }
                g.Issue(new Order { Tick = g.Tick, Op = Tech, Do = "draw" });
                r.To(Math.Max(g.Tick, r.Send(Tech, "d:Office approach")));
                r.Until(() => !g.GangPending || g.Over, T(30));
                r.Is(!g.GangPending && g.GangClaimedWhy.Contains("weapon drawn"), Iv($"placed: {g.GangClaimedWhy}"));
            });

            Add("096-LEND-16", "096", "Motive LEND-16 (L8): a loud lend; the seal falls after vale.out and before the police clock: the win stands, those left are captured (sealed).", r =>
            {
                var g = r.G; r.Until(() => g.Tick >= T(255), T(480)); // 4:15: the unit on the street about 5:20, the seal 145 s on, after the lent car's Go and before 8:10
                r.Fire(Tech, "camera:entrance", true); r.Until(() => g.Alert >= 2, T(30)); r.Send(Scout, "d:Service lane"); r.Send(Fixer, "d:Service lane");
                if (!LendAt(r)) { r.Is(false, Iv($"lent ({g.Vale} {g.FailCode})")); return; }
                r.Until(() => { if (g.Tick % Game.Hz == 0) foreach (var op in new[] { Scout, Fixer }) { var o = r.Op(op); var t = g.LentHolders().FirstOrDefault(id => g.Hostiles.Exists(h => h.Id == id)); if (t != null && o.Gun.Order.Length == 0 && !o.Down) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = t }); } return g.LentGoQuery().ok || g.Over; }, T(150));
                if (!r.Is(g.Issue(GoOn(g, "car:official")).ok, Iv($"the lent car goes at {Report.Clock(g.Tick)}"))) return;
                int seal = g.Lockdown, police = g.ValeOutTick + T(g.R.SplitPoliceS), close = T(g.ClockOver);
                r.Until(() => g.Over, T(g.R.SplitPoliceS + 5));
                r.Is(g.Result == Outcome.Success && g.Tick == Math.Min(seal, Math.Min(police, close)), Iv($"a win at {Report.Clock(g.Tick)} (seal {Report.Clock(seal)}, police {Report.Clock(police)}, close {Report.Clock(close)})"));
                r.Line(seal < police && seal < close, "the seal came first");
                bool sealed_ = seal < police && seal <= close;
                r.Is(g.LeftBehindAtGo.TrueForAll(x => x.fate == "captured" && (x.why == "downed" || x.why == (sealed_ ? "sealed" : "police"))), "those left captured");
            }, NoRivals(Turned(Driver)));
        }
    }
}
