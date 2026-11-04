// Motive's lend scenarios (engine 0.42.0, brief 096; briefs/096/motive/lend.html, lend.json "scenarios"; Sky 2026-11-05 "build the lend now";
// rulebook car.lend). Engine ids 096-LEND-n; the fight fixes and the veteran's placing are ScenariosLend096b.cs. Motive's list is the test list:
//  LEND-1 lend-at-bay · LEND-2 lend-at-kerb · LEND-3 win-fixed · LEND-4 seats · LEND-5 never-driven · LEND-6 spring-leave · LEND-7 spring-near
//  LEND-8 same-tick · LEND-9 gate-per-car · LEND-10 own-car-boxes-lent · LEND-11 drive-round-both-boxed (L10, with its one-car twin) ·
//  LEND-12 lane-bay · LEND-13 police-clock · LEND-15 car-lost · LEND-17 withdraw-not-end · LEND-18 public-fight-once (the lent car's Go
//  judges nothing) · LEND-19 loud-lend · LEND-20 lookout-after-go · LEND-21 talker-driver; LEND-14 (carried out) and LEND-16 (the seal after
//  the win) are ScenariosLend096b.cs. LEND-B is the bands (briefs/096/keel/bands-lend-*.txt) and the check's own replay of every scenario.
// The one-car twins: the scenarios whose subject is the one-car walk (the Take at the bay, Vale walked round) play with Start.OneCar (test
// only: car.lend.split read as 0), listed here; with it every one of them is 0.41.0's rule with the fight fixes.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        /// <summary>Engine 0.42.0: the scenarios of the one-car getaway's rules (the walk round with Vale, 0.41.0's drive-mouth block point), played
        /// with Start.OneCar so they keep testing them; the lend's own are 096-LEND-*.</summary>
        static readonly string[] OneCarIds = { "S20", "S21", "N19b", "N33", "N77", "N78", "P59-FIRST", "KERB-3", "096-RIV-2", "096-RIV-30" };

        static Start LendStart(Start? s = null) { s ??= Turned(Driver); return Gone(s, "rival-4"); } // the driver turned; no lookout (his job is RIV-*)
        static Order GoOn(Game g, string car) => new Order { Tick = g.Tick, Op = "cell", Do = "go", Target = car };
        /// <summary>The in-car Take at the bay (or the kerbside stop) with the lend on: true when the car is lent.</summary>
        static bool LendAt(Run r) { TakeAtBay(r); return r.G.Vale == ValeAt.Lent && r.G.Lent; }
        /// <summary>Send the Scout, the Fixer and the Tech to the service lane, clear of the square (more than 20 m from our car).</summary>
        static void ThreeAway(Run r) { var at = Math.Max(r.Send(Scout, "d:Service lane"), Math.Max(r.Send(Fixer, "d:Service lane"), r.Send(Tech, "d:Service lane"))); r.To(Math.Max(r.G.Tick, at)); }

        static void Lend096()
        {
            Add("096-LEND-1", "096", "Motive LEND-1 (L1): the in-car Take in the service bay ends with the Enforcer at the wheel and Popovic in the back of his own car; the driver gets out and walks off the map; Go is offered on car:official.", r =>
            {
                var g = r.G; if (!r.Is(LendAt(r), Iv($"the Take lends the car ({g.Vale}, {g.Result} {g.FailCode})"))) return;
                var e = r.Op(Enf);
                r.Is(e.InCar && e.InLent && !e.LeftMap && g.Log.Exists(l => l.Code == "lend.lent"), "the Enforcer at the wheel (lend.lent)");
                r.Eq(g.ValePos(), (Pt?)g.CarPos(), "Popovic in the back of his car");
                var d0 = g.DriverPos(); r.Wait(T(4)); var d1 = g.DriverPos();
                r.Is(d0.HasValue && d1.HasValue && G.Dist(d1.Value, g.CarPos()) > G.Dist(d0.Value, g.CarPos()), Iv($"the driver walks off ({d0} to {d1})"));
                var q = g.LentGoQuery(); r.Is(q.ok, Iv($"Go is offered on car:official ({q.code} {q.why})"));
                r.Until(() => !g.DriverPos().HasValue, T(300)); r.Is(!g.DriverPos().HasValue && !g.Over, Iv($"the driver is off the map at {Report.Clock(g.Tick)}"));
            }, LendStart());

            Add("096-LEND-2", "096", "Motive LEND-2: our car on the service drive, the turned driver stops at the kerb short of it; the Take there; the lend at the kerbside stop.", r =>
            {
                var g = r.G; r.To(DriveAtStart(r));
                if (!r.Is(LendAt(r), Iv($"the Take at the kerbside stop lends the car ({g.Vale}, {g.Result} {g.FailCode})"))) return;
                r.Is(g.CarAtKerb && g.Log.Exists(l => l.Code == "car.kerbside"), "his car is at the kerbside stop");
                r.Is(r.Op(Enf).InLent, "the Enforcer at its wheel");
            }, NoRivals(Turned(Driver)));

            Add("096-LEND-3", "096", "Motive LEND-3 (L5): Go on the lent car with the three at the square: vale.out, the win fixed, the mission runs on.", r =>
            {
                var g = r.G; if (!LendAt(r)) { r.Is(false, "lent"); return; }
                var a = g.Issue(GoOn(g, "car:official")); r.Is(a.ok, Iv($"Go on car:official taken ({a.code} {a.why})"));
                r.Is(g.ValeOutTick == g.Tick && g.Vale == ValeAt.Out && g.Log.Exists(l => l.Code == "vale.out"), "vale.out this tick");
                r.Is(!g.Over && g.Result == Outcome.None, "the mission runs on");
                r.Is(r.Op(Enf).LeftMap && g.CarAt("car:official") == null, "the Enforcer and the car are off the map");
                r.Wait(T(10)); r.Is(!g.Over, "ten seconds on, still running (the police clock is 60 s)");
            }, LendStart());

            Add("096-LEND-4", "096", "Motive LEND-4 (L3): a second agent's Board on the lent car is refused board-lent-full while the Enforcer is at the wheel; once he has stepped out (Popovic stays in), the Scout boards and the car's Go is offered.", r =>
            {
                var g = r.G; if (!LendAt(r)) { r.Is(false, "lent"); return; }
                var side = new[] { new Pt(0, -170), new Pt(0, 170), new Pt(-330, 0), new Pt(330, 0) }.Select(d => g.S.Grid.NearestFree(g.CarPos() + d) ?? g.CarPos()).First(p => g.CarBodyCm("car:official", p) is int bd && bd >= 0 && bd <= g.R.CarReachCm); // within car.footprint.reach of its body
                r.To(Math.Max(g.Tick, r.SendTo(Scout, side)));
                var b = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "board", Target = "car:official" });
                r.Eq((b.ok, b.code), (false, "board-lent-full"), Iv($"refused while he is at the wheel: {b.why}"));
                r.Is((b.why ?? "").Contains("enforcer"), "the refusal names who is at the wheel");
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", Point = g.S.Grid.NearestFree(g.CarPos() + new Pt(0, 400)) ?? g.CarPos() }).ok, "the Enforcer steps out");
                r.Wait(T(g.R.BoardS) + 1);
                r.Is(!r.Op(Enf).InCar && !r.Op(Enf).InLent && g.Vale == ValeAt.Lent, "out; Popovic stays in");
                r.Do(Scout, "board", "car:official"); r.Done(Scout);
                r.Is(r.Op(Scout).InLent, "the Scout at the wheel now");
                var q = g.LentGoQuery(); r.Is(q.ok, Iv($"its Go is offered ({q.code} {q.why})"));
            }, NoRivals(Turned(Driver)));

            Add("096-LEND-5", "096", "Motive LEND-5 (L2): the lent car is never Driven: Drive is refused drive-lent.", r =>
            {
                var g = r.G; if (!LendAt(r)) { r.Is(false, "lent"); return; }
                var d = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "drive-car", Target = "square" });
                r.Eq((d.ok, d.code), (false, "drive-lent"), Iv($"Drive refused: {d.why}"));
            }, NoRivals(Turned(Driver)));

            Add("096-LEND-6", "096", "Motive LEND-6 (L7): the three at the service lane, 30 m and more from our car; the lent car leaves: the posted crew springs that tick.", r =>
            {
                var g = r.G; ThreeAway(r); if (!LendAt(r)) { r.Is(false, "lent"); return; }
                r.Is(g.FightBranch == "safe" && g.FightSprung == int.MaxValue / 4, "the safe fight, not sprung yet");
                r.Is(g.Issue(GoOn(g, "car:official")).ok, "Go on the lent car");
                r.Eq(g.FightSprung, g.Tick, "the crew springs that tick");
                r.Is(g.Log.Exists(l => l.Code == "lend.spring"), "lend.spring");
            }, LendStart());

            Add("096-LEND-7", "096", "Motive LEND-7 (L7): after the Take, with Popovic in the lent car, an agent walking within fight.safe.ambush of our car springs the crew.", r =>
            {
                var g = r.G; ThreeAway(r); if (!LendAt(r)) { r.Is(false, "lent"); return; }
                r.SendTo(Scout, g.GetawaySeat);
                r.Until(() => g.FightSprung != int.MaxValue / 4, T(120));
                var d = G.Dist(r.Op(Scout).Pos, g.GetawayCarPos());
                r.Is(g.FightSprung == g.Tick && d <= g.R.SafeAmbushCm && g.Vale == ValeAt.Lent, Iv($"sprung at {Report.Clock(g.Tick)} with the Scout {d / 100} m from our car, Popovic still in the bay"));
            }, LendStart());

            Add("096-LEND-8", "096", "Motive LEND-8 (L5): Go on both cars in one tick: the lent car leaves; our car, boxed in at the square by the rival car, is refused car-boxed.", r =>
            {
                var g = r.G; r.To(Math.Max(g.Tick, r.SendTo(Tech, g.GetawaySeat))); r.Do(Tech, "board", "car"); r.Done(Tech);
                if (!LendAt(r)) { r.Is(false, "lent"); return; }
                var a = g.Issue(GoOn(g, "car:official")); var b = g.Issue(GoOn(g, "car"));
                r.Is(a.ok && g.ValeOutTick == g.Tick, "the lent car leaves (vale.out)");
                r.Eq((b.ok, b.code, b.ids), (false, "car-boxed", "car:rivals"), Iv($"ours refused: {b.why}"));
            }, LendStart());

            Add("096-LEND-9", "096", "Motive LEND-9 (L6): the safe fight is at our car (sprung by the Scout's walk in): the lent car's Go is taken all the same.", r =>
            {
                var g = r.G; ThreeAway(r); if (!LendAt(r)) { r.Is(false, "lent"); return; }
                r.SendTo(Scout, g.GetawaySeat); r.Until(() => g.FightSprung != int.MaxValue / 4, T(120));
                r.Wait(T(2));
                r.Is(g.GetawayBoxers().Count > 0 || g.CarHeld(), "our car is held or boxed");
                var a = g.Issue(GoOn(g, "car:official")); r.Is(a.ok, Iv($"the lent car's Go is taken ({a.code} {a.why})"));
            }, LendStart());

            Add("096-LEND-10", "096", "Motive LEND-10 (L6, car.box): our car Driven to the service drive behind Popovic's in the bay: the lent car's Go is refused car-boxed, naming car:getaway.", r =>
            {
                var g = r.G; r.To(Math.Max(g.Tick, r.SendTo(Tech, g.GetawaySeat))); r.Do(Tech, "board", "car"); r.Done(Tech);
                r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(480));
                r.Do(Tech, "drive-car", "service-drive"); r.Done(Tech);
                if (!LendAt(r)) { r.Is(false, Iv($"lent ({g.Vale} {g.FailCode})")); return; }
                var a = g.Issue(GoOn(g, "car:official"));
                r.Eq((a.ok, a.code, a.ids), (false, "car-boxed", "car:getaway"), Iv($"refused: {a.why}"));
            }, NoRivals(Turned(Driver)));

            Add("096-LEND-11", "096", "Motive LEND-11 (L10): our car on the service drive; the rival car blocks across the drive's mouth on the carriageway, at least 10 m from our car and 5.5 m from the kerbside car's centre: it boxes both. The one-car twin (split 0) keeps 0.41.0's point, which boxes ours and not Popovic's.", r =>
            {
                var g = r.G; r.To(DriveAtStart(r)); if (!r.Is(UntilBlocked(r), "the rival car blocks")) return;
                var st = g.S.StandById("service-drive")!; var bp = g.BlockAt(st).at; var rc = g.RivalCarPose()!.Value;
                r.Is(G.Dist(bp, st.CarAt) >= 1000, Iv($"the block point {bp} is {G.Dist(bp, st.CarAt) / 100.0:0.0} m from our car (>= 10 m)"));
                r.Is(g.S.Drive.SurfaceAt(bp) == 'r', "on the carriageway");
                r.Is(g.GetawayBoxers().Contains("car:rivals"), "ours boxed");
                r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick, T(480));
                r.Is(g.CarAtKerb, "Popovic's car at the kerbside stop");
                r.Is(g.OfficialBoxers().Contains("car:rivals"), Iv($"and Popovic's boxed by it too ({string.Join(",", g.OfficialBoxers())})"));
                r.Is(G.Dist(rc.pos, g.CarPos()) >= 550, Iv($"the rival car {G.Dist(rc.pos, g.CarPos()) / 100.0:0.0} m from the kerbside car's centre (>= 5.5 m, outside the 5 m blast)"));
                if (r.Inputs == null) return;
                var t0 = new Start(); t0.Turned.Add(Driver); t0.OneCar = true; Gone(t0, "rival-4");
                var tw = new Run(Engine.New(r.Inputs, t0)) { Inputs = r.Inputs }; tw.To(DriveAtStart(tw)); UntilBlocked(tw);
                var tg = tw.G; var tp = tg.BlockAt(tg.S.StandById("service-drive")!).at;
                tw.Until(() => tg.CarInBayTick > 0 && tg.Tick >= tg.CarInBayTick, T(480));
                r.Is(tp == new Pt(8600, -1500) && tg.GetawayBoxers().Contains("car:rivals") && !tg.OfficialBoxers().Contains("car:rivals"), Iv($"twin at split 0: 0.41.0's point {tp} boxes ours, not Popovic's ({string.Join(",", tg.OfficialBoxers())})"));
            }, LendStart());

            Add("096-LEND-12", "096", "Motive LEND-12 (L4, 071 R11): the lent car's lane: a hostile 10 m down the drive holds it; 12 m off the drive, beside the bay, does not.", r =>
            {
                var g = r.G; if (!LendAt(r)) { r.Is(false, "lent"); return; }
                var car = g.CarPos(); var down = car + new Pt(1000, 0); var off = car + new Pt(0, 1200);
                r.Is(g.LentHoldsAt(down), Iv($"{down}, 10 m down the drive, holds it"));
                r.Is(!g.LentHoldsAt(off), Iv($"{off}, 12 m off it, does not"));
            }, NoRivals(Turned(Driver)));

            Add("096-LEND-13", "096", "Motive LEND-13 (L8): split-police-s after vale.out the mission ends a win; the three standing outside a car are captured (police), +captured-trail each in the debrief.", r =>
            {
                var g = r.G; ThreeAway(r); if (!LendAt(r)) { r.Is(false, "lent"); return; }
                r.Is(g.Issue(GoOn(g, "car:official")).ok, "Go on the lent car"); int out0 = g.Tick, end = g.SplitPoliceAt;
                r.Is(end <= out0 + T(g.R.SplitPoliceS), Iv($"the police come at {Report.Clock(end)}"));
                r.Until(() => g.Over, T(g.R.SplitPoliceS + 5));
                r.Is(g.Result == Outcome.Success && g.Tick == end, Iv($"the win, ended at {Report.Clock(g.Tick)}"));
                r.Eq(string.Join(",", g.Captured.OrderBy(x => x, StringComparer.Ordinal)), "fixer,scout,tech", "the three captured");
                r.Is(g.LeftBehindAtGo.TrueForAll(x => x.fate == "captured" && (x.why == "police" || x.why == "sealed")), "captured by the police");
                var d = Debrief.Of(g); r.Is(d.Next.Captives.Count == 3 && d.Next.CampaignTrail >= 3 * g.R.CapturedTrail, Iv($"three captives, +{d.Next.CampaignTrail} T"));
            }, LendStart());

            Add("096-LEND-15", "096", "Motive LEND-15 (L9): our car still standing at the end is lost: +witness-trail, hunted, the debrief's car-seen line says 'left'.", r =>
            {
                var g = r.G; ThreeAway(r); if (!LendAt(r)) { r.Is(false, "lent"); return; }
                g.Issue(GoOn(g, "car:official")); int t0 = g.Trail; r.Until(() => g.Over, T(g.R.SplitPoliceS + 5));
                r.Is(g.CarSeenBy == "left" && g.Ledger.Exists(x => x.cause == "car-lost" && x.delta == g.R.WitnessTrail), Iv($"lost: +{g.R.WitnessTrail} T (car-lost)"));
                var d = Debrief.Of(g); r.Is(d.Inv.Hunted.Contains("cell-car") && d.Next.Lines.Exists(l => l.Line == "car-seen" && l.Who == "left"), "hunted; car-seen 'left'");
            }, NoRivals(Turned(Driver)));

            Add("096-LEND-17", "096", "Motive LEND-17 (L8): Go on our car without Popovic, the three aboard, while he is in the bay with the Enforcer: the mission goes on; then the lent car's Go: vale.out, nobody left, a win with nobody captured.", r =>
            {
                var g = r.G; if (!LendAt(r)) { r.Is(false, "lent"); return; }
                foreach (var op in new[] { Scout, Fixer, Tech }) r.SendTo(op, g.GetawaySeat);
                r.Until(() => new[] { Scout, Fixer, Tech }.All(o => g.AtGetaway(r.Op(o).Pos) && r.Op(o).Walk == null), T(200));
                foreach (var op in new[] { Scout, Fixer, Tech }) r.Do(op, "board", "car");
                r.Wait(T(g.R.BoardS) + 1);
                var a = g.Issue(GoOn(g, "car")); r.Is(a.ok, Iv($"our car goes ({a.code} {a.why})"));
                r.Is(!g.Over && g.GetawayGone && g.Vale == ValeAt.Lent, "the mission goes on, Popovic in the bay");
                var b = g.Issue(GoOn(g, "car:official")); r.Is(b.ok, "then the lent car");
                r.Is(g.Over && g.Result == Outcome.Success && g.Captured.Count == 0, Iv($"a win, nobody captured ({g.Result}, {string.Join(",", g.Captured)})"));
            }, NoRivals(Turned(Driver)));

            Add("096-LEND-18", "096", "Motive LEND-18 (fight.public): the lent car leaving judges no fight (nothing is fired).", r =>
            {
                var g = r.G; if (!LendAt(r)) { r.Is(false, "lent"); return; }
                int n = g.Log.Count(l => l.Code == "fight.judged"); g.Issue(GoOn(g, "car:official"));
                r.Eq(g.Log.Count(l => l.Code == "fight.judged"), n, "no fight judged at its Go");
            }, NoRivals(Turned(Driver)));

            Add("096-LEND-19", "096", "Motive LEND-19 (L6): a loud lend (the entrance camera shot before the pickup): the police come for the lent car, a blocker on its lane down the drive; its Go is refused car-held, naming him, until he is down.", r =>
            {
                var g = r.G; r.Until(() => g.CarInBayTick > 0 && g.Tick >= g.CarInBayTick - T(20), T(480));
                r.Fire(Tech, "camera:entrance", true); r.Until(() => g.Alert >= 2, T(30)); r.Send(Scout, "d:Service lane"); r.Send(Fixer, "d:Service lane");
                if (!LendAt(r)) { r.Is(false, Iv($"lent ({g.Vale} {g.FailCode})")); return; }
                r.Is(g.FightBranch == "loud", "the loud fight");
                r.Until(() => g.Blockers.Count > 0, T(90)); if (!r.Is(g.Blockers.Count > 0, "a blocker is picked")) return;
                var b = g.Hostiles.First(h => h.Id == g.Blockers[0]); r.Is(g.LentHoldsAt(b.Home), Iv($"{b.Id} goes for the lent car's lane ({b.Home})"));
                var a = g.Issue(GoOn(g, "car:official")); r.Is(!a.ok && (a.code == "car-held") && (a.ids ?? "").Contains(b.Id), Iv($"refused while he is up: {a.code} {a.ids}"));
                r.Until(() => { if (g.Tick % Game.Hz == 0) foreach (var op in new[] { Scout, Fixer }) if (r.Op(op).Gun.Order.Length == 0 && !r.Op(op).Down) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "fire", Target = b.Id }); return b.Down || g.Over || g.LentGoQuery().ok; }, T(150));
                var q = g.LentGoQuery(); r.Is(b.Down ? q.ok || q.code == "car-held" : q.ok, Iv($"once he is down ({b.Down}), the Go reads {q.code}"));
            }, NoRivals(Turned(Driver)));

            Add("096-LEND-20", "096", "Motive LEND-20 (L11): after vale.out nobody can reach the target: the fight at our car runs on and never fails rivals-reach-vale.", r =>
            {
                var g = r.G; DealLookout(r, "mislead", 300); if (!LendAt(r)) { r.Is(false, Iv($"lent ({g.Vale} {g.FailCode})")); return; }
                g.Issue(GoOn(g, "car:official")); r.Until(() => g.Over, T(g.R.SplitPoliceS + 5));
                r.Is(g.FailCode != "rivals-reach-vale" && g.Result == Outcome.Success, Iv($"{g.Result} {g.FailCode}"));
            }, Turned(Driver));

            Add("096-LEND-21", "096", "Motive LEND-21 (L12): the driver's facts after the lend hold no car of ours (he never saw it: our car stays at the square).", r =>
            {
                var g = r.G; if (!LendAt(r)) { r.Is(false, "lent"); return; }
                var dr = g.PersonByRole("driver"); r.Is(!dr.Memory.Exists(m => m.Act == "saw-getaway"), "no saw-getaway in his memory");
            }, NoRivals(Turned(Driver)));
        }
    }
}
