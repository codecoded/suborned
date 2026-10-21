// Brief 027, the fight playground (EXPERIMENTAL; rulebook fight.lab; Motive, briefs/027/playground.md). Two parts:
//  · the mechanics scenarios N140–N148, each on a start with the lab on (today's runs are every other scenario: lab off is v0.24):
//    N140 the presets: lab off hashes as today; lab on scales health ×10 and arms the loadout
//    N141 the pip ladder (range band and edges, clamped 1–4) and the chip's read (PreviewFire)
//    N142 damage and hits to down per weapon, the riot gun's near band, the rifle's rings; a loadout over Arms is refused
//    N143 fighting on after a kill (fire.then free) in the safe fight
//    N144 walking into range (fire.close) where today refuses out-of-range
//    N145 a volley: the forecast (PreviewVolley) is exactly what lands; charge paid, spent and refilled; an unpaid volley refused no-charge
//    N146 the police warning: holstered in time, Alert 1 and no engagement; held drawn, today's drawn-seen
//    N147 the field (switch on): paid from charge, refused no-charge short of it; off, an unknown verb
//    N148 two stances: return is refused, a draw is at will, a fire order clears hold
//    N149 a volley out of range walks in and fires its forecast from the stop
//    N152–N154 Motive's §6 rulings (ScenariosFightLabTuning.cs): aim memory, the charge rest, one warning per watcher
//    N150 cover snap (B5): a move ending within 1.5 m of a free point ends on it; not past 1.5 m, not through a wall, not onto a held point,
//         walked (never a jump) mid-volley, let go on moving off; lab off or snap off, today's move
//  · the balance matrix (§6): loud runs L1–L4 from 0:01 and prepared runs P-A, P-G, P-B, scripted as the perfect player (zero reaction
//    time, playground.md §0), under a start's fightLab (a preset), with the pass/fail of §6.2–§6.4 (FightLabMatrix; suborned-sim fight-lab).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Suborned.Sim
{
    /// <summary>One run of the balance matrix (brief 027 §6.5): its outcome and what §6.2–§6.4 read.</summary>
    public sealed class LabRow
    {
        public string Run = "", Preset = "", Result = "", Fail = ""; public int EndTick;
        public int TrailMission, TrailTotal, Alert, StandingStart, StandingEnd; public string PublicSecurity = "", Investigation = ""; public int OpenCases;
        public int OpsDown, OpsCaptured, CellHealth, CellMax, CellHits, FoesDown, CellShots;
        /// <summary>§6.6.2 (Motive, tuning.md): the most armed hostiles one cell agent downed before any hostile had fired on it, and who.</summary>
        public int FreeDowns; public string FreeDownsBy = "";
        /// <summary>§6.6.2 as Pulse measures it (kite-response.md §6.3, "one free kill at most"): the most hostiles of one side (police, rivals,
        /// guard, patrol, units) one agent downed before that side had fired its first shot at the cell, and who on which side.</summary>
        public int FreeSide; public string FreeSideBy = "";
        public bool Loud; public string Verdict = ""; public readonly List<string> Why = new List<string>();
        /// <summary>Verdict 6.4.8 (Motive, tuning.md §7.1): final-fight police bodies seen, those that once had the Vale holder (or an agent within
        /// 15 m of him) in reach on a line, those up at the end without it, and the hostiles' shots at the cell from Vale coming out to the end.</summary>
        public int ContactSeen, ContactHad, ShotsAtCellAfterVale; public readonly List<string> ContactMissing = new List<string>(), ContactMissingUnits = new List<string>();
        public int HealthLostPct => CellMax == 0 ? 0 : (CellMax - CellHealth) * 100 / CellMax;
        public bool Success => Result == "success";
    }

    public static partial class Scenarios
    {
        /// <summary>Another game on the same inputs as `g`, from `start` (a side-by-side check inside a scenario).</summary>
        static Game Twin(Game g, Start start) => new Game(g.R, g.S, start, null, g.People2);
        static Start LabStart(Action<Start>? f = null) { var s = new Start { FightLabOn = true }; f?.Invoke(s); return s; }

        static void FightLabCases()
        {
            FightLabCloseVolley();
            FightLabCoverSnap();
            FightLabTuning(); // tuning.md §6.1–6.4: N152–N154
            FightLabKite(); // F2, Pulse kite-response.md: N155–N158
            FightLabRetest(); // brief 027 retest (Suture R1–R6, Pulse retest-fixes.md, Motive §7): N159–N165
            Add("N140", "—", "Brief 027 presets (EXPERIMENTAL fight.lab): the today preset (lab off) hashes as a start with no lab; with the lab on every health is ×10 (the Enforcer 60, the others 40, the guard 30), the Handover's loadout is three sidearms and the carbine, and every other gun the lab sidearm.", r =>
            {
                var g = r.G; var off = new Start { FightLabOn = false }; off.LabPips = false;
                r.Eq(off.Canonical(), new Start().Canonical(), "the today preset adds nothing to the start state");
                r.Is(g.Lab && g.HealthScale == 10, "the lab is on, health ×10");
                r.Eq((r.Op(Enf).Gun.Max, r.Op(Scout).Gun.Max, g.GuardGun.Max, g.PatrolGun.Max), (60, 40, 30, 40), "health ×10: Enforcer 60, an op 40, the guard 30, the patrol 40");
                r.Eq((r.Op(Scout).Gun.W!.Id, r.Op(Fixer).Gun.W!.Id, r.Op(Tech).Gun.W!.Id, r.Op(Enf).Gun.W!.Id), ("sidearm", "sidearm", "sidearm", "carbine"), "the Handover issue: three sidearms and the carbine");
                r.Eq((g.GuardGun.W!.Id, g.PatrolGun.W!.Id, g.DrawT, g.AimNewT), ("sidearm", "sidearm", g.R.Lab!.DrawTicks, g.R.Lab.AimNewTicks), "everyone else carries the lab sidearm; draw and aim are the lab's");
                r.Eq(g.ChargeOf(OpId.Enforcer), 100, "a full charge bar");
                var today = Twin(g, new Start());
                r.Is(!today.Lab && today.HealthScale == 1 && today.OpOf(OpId.Enforcer).Gun.Max == g.R.HealthEnforcer && today.OpOf(OpId.Scout).Gun.W == g.R.Pistol, "lab off: today's health and pistol");
                r.Wait(1); // a run ends after tick 0 (a replay to 0 would mean the mission's close)
            }, LabStart());

            Add("N141", "—", "Brief 027 P4 (EXPERIMENTAL): the pip ladder. Range band 4 (point blank) / 3 (effective) / 2 (max); untrained −1 and cover −1 beyond point blank, walking −1, high +1, clamped 1–4; the chance is fight.lab.chance; the chip (PreviewFire) reads pips, damage and hits to down, and counts shots to down by the accumulator.", r =>
            {
                var g = r.G; var R = g.R; var sa = R.Lab!.Weapons["sidearm"]; var cb = R.Lab.Weapons["carbine"];
                int lng = (sa.EffCm + sa.MaxCm) / 2; // the sidearm's long band (between its rings: the rulebook's, Motive tunes them)
                r.Eq((R.LabPips(sa, 400, false, true, true, false), R.LabPips(sa, 1200, false, false, false, false), R.LabPips(sa, lng, false, false, true, false), R.LabPips(sa, lng, false, true, true, false)), (4, 3, 1, 1), "4 point blank (edges don't count), 3 effective, 2 −1 untrained in the long band, clamped at 1");
                r.Eq((R.LabPips(sa, 1200, true, false, false, false), R.LabPips(cb, 1500, false, true, true, true), R.LabPips(sa, sa.MaxCm + 100, false, false, false, false)), (2, 2, 0), "walking −1; high +1; beyond max no shot");
                r.To(r.SendTo(Enf, BesideDoor));
                var p = g.PreviewFire(OpId.Enforcer, GuardR);
                r.Eq(p.Refused, "", "the Enforcer can fire on the guard");
                r.Is(p.Pips >= 3 && p.Acc == R.Lab.Chance[p.Pips], $"trained, close: {p.Pips} pips at {p.Acc} ‰");
                r.Eq((p.Damage, p.Health, p.HitsToDown), (20, 30, 2), "the carbine does 20; the guard has 30: two hits");
                r.Is(p.ShotsToDown >= p.HitsToDown && p.TrailShooting == R.GunfireTrail && p.TrailHit == R.HitTrail, $"shots to down by the accumulator ({p.ShotsToDown}); the price: +{p.TrailShooting} T shooting, +{p.TrailHit} T hit");
                r.Eq(g.Log.Count(l => l.Code.StartsWith("shot.", StringComparison.Ordinal)), 0, "a preview fires nothing");
            }, LabStart());

            Add("N142", "—", "Brief 027 P5 (EXPERIMENTAL): damage and hits to down per weapon on the guard (30): sidearm 10 ×3, machine pistol 6 ×5, carbine 20 ×2, riot gun 30 ×1 within 5 m and 10 ×3 beyond, long rifle 25 ×2 out to 50 m; a loadout over the mission's Arms is refused at the start.", r =>
            {
                var g = r.G; var lab = g.R.Lab!;
                r.Eq((r.Op(Scout).Gun.W!.Id, r.Op(Fixer).Gun.W!.Id, r.Op(Tech).Gun.W!.Id), ("machine", "riot", "rifle"), "the loadout: machine pistol, riot gun, long rifle (6 Arms)");
                int H(Weapon w, long d) { int dm = w.DamageAt(d); return (30 + dm - 1) / dm; }
                r.Eq((H(lab.Weapons["sidearm"], 1000), H(lab.Weapons["machine"], 800), H(lab.Weapons["carbine"], 1500), H(lab.Weapons["riot"], 400), H(lab.Weapons["riot"], 800), H(lab.Weapons["rifle"], 4000)), (3, 5, 2, 1, 3, 2), "hits to down a guard: 3, 5, 2, 1 (riot near), 3 (riot far), 2");
                r.Is(lab.Weapons["rifle"].MaxCm > lab.Weapons["carbine"].MaxCm && lab.Weapons["rifle"].NoFireMoving && lab.Weapons["machine"].Charge > 0 && lab.Weapons["carbine"].Charge > 0 && lab.Weapons["sidearm"].Charge == 0, "the rifle outreaches the carbine and never fires walking; the machine pistol and carbine draw charge, the sidearm none");
                var over = LabStart(s => { s.LabLoadout[0] = "machine"; s.LabLoadout[1] = "riot"; s.LabLoadout[2] = "rifle"; }); over.Arms = 3;
                string err = ""; try { Twin(g, over); } catch (FormatException e) { err = e.Message; }
                r.Is(err.Contains("Arms"), $"6 Arms of weapons on a 3-Arms mission is refused ({err})");
                var bad = LabStart(s => s.LabLoadout[0] = "bazooka"); err = "";
                try { Twin(g, bad); } catch (FormatException e) { err = e.Message; }
                r.Is(err.Contains("unknown"), "an unknown weapon is refused (unknown = reject)");
                r.To(T(5));
                var pf = g.PreviewFire(OpId.Tech, GuardR);
                r.Is(pf.Damage == lab.Weapons["rifle"].Damage && pf.HitsToDown == (30 + pf.Damage - 1) / pf.Damage, $"the rifle's chip on the guard: −{pf.Damage} ×{pf.HitsToDown}");
            }, LabStart(s => { s.Arms = 6; s.LabLoadout[0] = "machine"; s.LabLoadout[1] = "riot"; s.LabLoadout[2] = "rifle"; }));

            Add("N143", "—", "Brief 027 P3 (EXPERIMENTAL fire.then free): in the safe fight a fire order whose target goes down returns the agent to at will, and it fights on at the next rival with no new order.", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true); Support(r); ToAmbush(r);
                int firedAt = -1; string first = "";
                if (r.Until(() => { var t = g.Combatants().FirstOrDefault(id => id.StartsWith("rival-", StringComparison.Ordinal)); return t != null && g.PosOf(t).HasValue && g.PreviewFire(OpId.Scout, t).Refused.Length == 0; }, T(30)))
                { first = g.Combatants().First(id => id.StartsWith("rival-", StringComparison.Ordinal)); r.Fire(Scout, first); firedAt = g.Tick; }
                r.Until(() => g.GunOf(first)?.Down == true || g.FightOverTick != Never, T(60));
                r.Is(g.GunOf(first)?.Down == true, $"{first} goes down");
                int downAt = g.Tick; r.Wait(T(3));
                r.Eq(r.Op(Scout).Gun.Stance, "free", "then free: back to at will");
                bool firedOn = r.Shots().Exists(s => s.by == "scout" && s.tick > downAt && s.target != first);
                bool newOrder = g.Orders.Exists(o => o.order.Op == Scout && o.order.Do == "fire" && o.order.Tick > firedAt);
                r.Is(!newOrder && (firedOn || g.FightOverTick != Never), "the Scout fights on with no new order (or the fight is over)");
            }, LabStart());

            Add("N144", "—", "Brief 027 P6 (EXPERIMENTAL fire.close): a fire order out of range walks the grid into the effective ring with a clear line, then fires; it neither lapses nor is refused. Brief 030 (weapons.close, Sky 2026-10-20): today the same order walks in too (it was refused out-of-range).", r =>
            {
                var g = r.G; r.To(T(2));
                var gp = g.PosOf(GuardR)!.Value; long d0 = G.Isqrt(G.D2(r.Op(Scout).Pos, gp));
                var today = Twin(g, new Start()); today.RunTo(T(2));
                var ta = today.Issue(new Order { Tick = today.Tick, Op = Scout, Do = "fire", Target = GuardR });
                r.Is(ta.ok && today.OpOf(OpId.Scout).CloseWalk, $"today: taken at {d0 / 100} m, and the Scout walks into range (brief 030; got {(ta.ok ? "taken" : ta.code)})");
                var pv = g.PreviewFire(OpId.Scout, GuardR);
                r.Is(pv.Refused == "" && pv.Walk && pv.WalkS > 0, $"the chip shows the walk into range ({pv.WalkS} s)");
                r.Fire(Scout, GuardR);
                r.Is(r.Op(Scout).CloseWalk && r.Op(Scout).Walk != null, "the Scout walks into range");
                r.Until(() => r.Shots().Exists(s => s.by == "scout"), T(90));
                var shot = g.Log.FirstOrDefault(l => l.Code.StartsWith("shot.", StringComparison.Ordinal) && l.Text.StartsWith("scout ", StringComparison.Ordinal));
                r.Is(shot.Code != null, "and fires");
                r.Is(!g.Log.Exists(l => l.Code == "fire.lapses"), "the order never lapses");
            }, LabStart());

            Add("N145", "—", "Brief 027 §1.3 (EXPERIMENTAL target mode): a volley of 3 on the guard. PreviewVolley's shot-by-shot forecast is exactly what lands (counted, not rolled); the volley pays 15 charge a shot when given and the carbine 8 a shot as it fires; the bar comes back while the carbine rests; a volley the bar can't pay is refused no-charge.", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"), r.SendTo(Enf, BesideDoor)));
                r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                var cw = r.Op(Enf).Gun.W!; int tc = cw.TargetCharge, own = cw.Charge, max = g.Labr!.ChargeMax, paid = max - 3 * tc; // the rulebook's (Motive tunes them)
                var fv = g.PreviewVolley(OpId.Enforcer, GuardR, 3);
                r.Eq((fv.Refused, fv.ChargeCost, fv.ChargeLeft), ("", 3 * tc, paid), $"three targeted carbine shots cost {3 * tc} of {max}");
                var a = g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "volley", Target = GuardR, Shots = 3 });
                r.Is(a.ok, $"the volley is taken{(a.ok ? "" : ": " + a.why)}");
                r.Eq(g.ChargeOf(OpId.Enforcer), paid, "paid when given");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "volley", Target = GuardR, Shots = 6 }).code, "no-charge", $"a volley of 6 ({6 * tc}) the bar ({paid}) can't pay is refused no-charge");
                r.Until(() => r.Op(Enf).Gun.VolleyLeft == 0, T(20));
                var shots = r.Shots().Where(s => s.by == "enforcer" && s.target == GuardR).Select(s => s.hit).ToList();
                var want = fv.Shots.Take(shots.Count).Select(s => s.hit).ToList();
                r.Is(shots.Count >= 1 && shots.SequenceEqual(want), $"the forecast lands as shown: {string.Join("", want.Select(h => h ? "●" : "○"))} → {string.Join("", shots.Select(h => h ? "●" : "○"))}");
                r.Is(shots.Count == 3 || g.GuardDown, $"exactly the volley's shots ({shots.Count}), unless the guard went down first");
                int low = g.ChargeOf(OpId.Enforcer), n = shots.Count, book = paid - own * n + tc * (3 - n); // each shot drew the carbine's own; the unfired shots' target charge came back
                r.Is(low >= Math.Max(0, book) && low <= Math.Max(0, book) + (g.DrawT + g.AimNewT) * g.Labr.ChargeRegen, $"the carbine drew {own} a shot, the unfired {3 - n} came back: {low} ({paid} − {own}×{n} + {tc}×{3 - n}, plus the bar's regen while drawing and aiming)");
                r.Wait(T(12));
                r.Eq(g.ChargeOf(OpId.Enforcer), max, "the bar comes back while the carbine rests");
            }, LabStart(s => s.Security = 1));

            Add("N146", "—", "Brief 027 P10 (EXPERIMENTAL police warning): the guard sees the Scout draw before any shot and gives 2 s. Holstered in time: Alert 1, no engagement, no drawn-seen; drawn again and held: today's drawn-seen (Alert 2, he engages).", r =>
            {
                var g = r.G;
                r.To(Max(r.Send(Tech, "cabinet"))); r.Do(Tech, "loop", "entrance"); r.Done(Tech); r.Do(Tech, "reroute", "patrol"); r.Done(Tech);
                var post = g.PosOf(GuardR)!.Value; Pt? spot = null;
                for (int dx = -800; dx <= 800 && spot == null; dx += 100) for (int dy = -800; dy <= 800 && spot == null; dy += 100)
                { var q = new Pt(post.X + dx, post.Y + dy); if (g.S.Grid.Free(q) && G.D2(q, post) >= 300L * 300 && g.GuardWatching(q) && !g.PatrolWatching(q) && !g.CameraWatching("pickup", q)) spot = q; }
                if (!r.Is(spot.HasValue, "a spot the guard watches")) return;
                r.To(r.SendTo(Scout, spot!.Value));
                r.Do(Scout, "draw"); r.Wait(1);
                r.Is(g.Warnings.Count == 1 && g.Warnings[0].Watcher == "guard", $"the guard warns ({g.Warnings.Count})");
                r.Eq(g.Alert, 1, "a warning is a found fault: Alert 1");
                r.Wait(T(1)); r.Do(Scout, "holster"); r.Wait(T(3));
                r.Is(g.Alert == 1 && !g.GuardEngaged && !g.Log.Exists(l => l.Code == "drawn.seen"), "holstered in time: Alert 1, no engagement, no drawn-seen");
                r.Is(g.GuardP.Has("saw-weapon-drawn"), "he remembers the face");
                r.Do(Scout, "draw"); r.Wait(T(3));
                r.Is(g.Alert >= 2 && g.Log.Exists(l => l.Code == "drawn.seen"), $"held drawn past the window: today's drawn-seen, Alert {g.Alert}");
            }, LabStart(s => s.Security = 1));

            Add("N147", "—", "Brief 027 option A (EXPERIMENTAL field, switch on): the field costs 50 charge, absorbs 30 for 3 s and stops fire; short of charge it is refused no-charge; with the field off it is an unknown verb.", r =>
            {
                var g = r.G;
                r.Do(Enf, "field");
                r.Eq((g.ChargeOf(OpId.Enforcer), g.FieldOf(OpId.Enforcer).ticks, g.FieldOf(OpId.Enforcer).absorb), (50, 30, 30), "paid 50; 3 s and 30 to absorb");
                r.Do(Enf, "field");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "field" }).code, "no-charge", "the third, with the bar empty, is refused no-charge");
                r.Wait(T(4)); r.Eq(g.FieldOf(OpId.Enforcer).ticks, 0, "the field lapses after 3 s");
                var off = Twin(g, LabStart());
                r.Eq(off.Issue(new Order { Tick = 0, Op = Enf, Do = "field" }).code, "unknown-verb", "field off: unknown verb (unknown = reject)");
            }, LabStart(s => s.LabField = true));

            Add("N148", "—", "Brief 027 P2 (EXPERIMENTAL stances two): a draw fires at will (free); return is refused stance-unknown; hold, then a fire order clears hold. Today return is taken and a draw answers only who fired on the agent.", r =>
            {
                var g = r.G;
                r.Do(Scout, "draw"); r.Eq(r.Op(Scout).Gun.Stance, "free", "drawn means at will");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "stance", Stance = "return" }).code, "stance-unknown", "return is no stance with two");
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "stance", Stance = "hold" }).ok, "hold is");
                var today = Twin(g, new Start());
                today.Issue(new Order { Tick = 0, Op = Scout, Do = "draw" });
                r.Is(today.OpOf(OpId.Scout).Gun.Stance == "return" && today.Issue(new Order { Tick = 0, Op = Scout, Do = "stance", Stance = "return" }).ok, "today: a draw is return, and return is taken");
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = "cell", Do = "panic" }).ok, "with the lab, panic is taken outside a fight");
                r.Wait(1);
            }, LabStart(s => s.LabStances = "two")); // stances two is a lab option; the lab's own is three since tuning.md §13.4 (engine 0.18.0)
        }

        static void FightLabCloseVolley()
        {
            Add("N149", "—", "Brief 027 target mode out of range (Keel 2026-10-20, Latch's B2 question): a volley on a target out of range walks in first (fire.close) and holds its shots until it stands inside the effective ring with a line; PreviewVolley forecasts the shots from that stop point (the walk simulated tick by tick, as the engine walks it) with the walk's ticks and who could fire on the way. What lands is the forecast, from where it said.", r =>
            {
                var g = r.G; r.To(T(2));
                var gp = g.PosOf(GuardR)!.Value;
                r.Is(!G.Within(r.Op(Scout).Pos, gp, r.Op(Scout).Gun.W!.MaxCm), $"the guard is out of the Scout's range ({G.Isqrt(G.D2(r.Op(Scout).Pos, gp)) / 100} m)");
                var fv = g.PreviewVolley(OpId.Scout, GuardR, 3);
                r.Is(fv.Refused == "" && fv.Walk && fv.WalkTicks > 0 && fv.Acc > 0, $"the forecast walks in ({fv.WalkTicks} ticks to {fv.StopAt}) and fires from there at {fv.Pips} pips ({fv.Acc} ‰), not ○○○ from here");
                r.Is(G.Within(fv.StopAt, gp, r.Op(Scout).Gun.W!.EffCm), "the stop point is inside the effective ring");
                int issued = g.Tick;
                var a = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "volley", Target = GuardR, Shots = 3 });
                r.Is(a.ok, $"the volley is taken{(a.ok ? "" : ": " + a.why)}");
                r.Until(() => r.Op(Scout).Gun.VolleyLeft == 0 || g.GuardDown, T(90));
                var mine = r.Shots().Where(x => x.by == "scout" && x.target == GuardR).ToList();
                var want = fv.Shots.Take(mine.Count).Select(x => x.hit).ToList();
                r.Is(mine.Count == 3 || g.GuardDown, $"the volley's shots fired ({mine.Count})");
                r.Is(mine.Count > 0 && mine.Select(x => x.hit).SequenceEqual(want), $"the forecast lands as shown: {string.Join("", want.Select(h => h ? "●" : "○"))} → {string.Join("", mine.Select(x => x.hit ? "●" : "○"))}");
                r.Is(mine.Count > 0 && mine[0].tick >= issued + fv.WalkTicks, $"no shot on the way: the first at {Report.Clock(mine.Count > 0 ? mine[0].tick : 0)}, the stop at {Report.Clock(issued + fv.WalkTicks)}");
                r.Eq(r.Op(Scout).Pos, fv.StopAt, "the Scout stands where the forecast said");
            }, LabStart(s => s.Security = 1));
        }

        static void FightLabCoverSnap()
        {
            Add("N150", "—", "Brief 027 B5 (EXPERIMENTAL fight.lab.coverSnap, Sky approved for the lab preset): a drawn agent's move that ends within 1.5 m of a free cover point of the site, with a straight walkable line to it, ends on the point and holds it; past 1.5 m, across a wall or the cover object, or onto a point another agent holds, holstered, or to a named place, it doesn't; the snap is walked (mid-volley too, and the volley keeps its shots); moving off lets the point go; with the snap off or the lab off the same move ends where it was clicked.", r =>
            {
                var g = r.G; var R = g.R; int snap = g.CoverSnapCm; int sec = g.Tick / Game.Hz;
                r.Is(g.CoverSnapOn && snap == R.Lab!.CoverSnapCm && g.CoverMarksCm == R.Lab.CoverMarksCm, $"the snap is on: {snap} cm, marks within {g.CoverMarksCm / 100} m");
                r.Do(Scout, "draw"); r.Do(Fixer, "draw"); r.Wait(g.DrawT + 1); // the snap and the marks are a drawn agent's (P7)
                bool Ok(CoverPoint c) => c.Present(sec) && c.Q >= R.CoverQPartial;
                var dirs = new[] { (1000, 0), (707, 707), (0, 1000), (-707, 707), (-1000, 0), (-707, -707), (0, -1000), (707, -707) };
                // a click 1 m off a point, on free ground with a straight line to it, that snaps to that point
                CoverPoint? c = null; Pt click = default;
                foreach (var cp in g.S.Cover.Where(Ok).OrderBy(x => x.Id, StringComparer.Ordinal))
                {
                    foreach (var (dx, dy) in dirs) { var q = new Pt(cp.At.X + dx / 10, cp.At.Y + dy / 10); if (g.S.Grid.Free(q) && g.S.Grid.LineFree(q, cp.At) && g.PreviewSnap(OpId.Scout, q)?.Id == cp.Id) { c = cp; click = q; break; } }
                    if (c != null) break;
                }
                if (!r.Is(c != null, $"a cover point with a click 1 m off it ({c?.Id})")) return;
                r.Is(g.PreviewSnap(OpId.Tech, click) == null && g.CoverMarks(OpId.Tech).Count == 0, "the holstered Tech's same click doesn't snap, and shows no marks");
                var off = Twin(g, LabStart(s => s.LabCoverSnap = false)); var today = Twin(g, new Start());
                r.Is(off.PreviewSnap(OpId.Scout, click) == null && today.PreviewSnap(OpId.Scout, click) == null && off.CoverMarks(OpId.Scout).Count == 0, "snap off, or the lab off: no snap and no marks");
                // past the radius: a free click whose nearest eligible point is 1.6–2.5 m off
                Pt? far = null;
                foreach (var cp in g.S.Cover.Where(Ok).OrderBy(x => x.Id, StringComparer.Ordinal))
                {
                    foreach (var (dx, dy) in dirs) { var q = new Pt(cp.At.X + dx * 2 / 10 * 1, cp.At.Y + dy * 2 / 10); long near = g.S.Cover.Where(Ok).Min(x => G.D2(q, x.At)); if (g.S.Grid.Free(q) && near > (long)(snap + 10) * (snap + 10)) { far = q; break; } }
                    if (far != null) break;
                }
                r.Is(far.HasValue && g.PreviewSnap(OpId.Scout, far.Value) == null, $"a click 2 m from the nearest point ({far}) doesn't snap");
                // through a wall or the cover object: a free click within the radius of a point with no straight walkable line to it
                CoverPoint? wc = null; Pt wq = default;
                foreach (var cp in g.S.Cover.Where(Ok).OrderBy(x => x.Id, StringComparer.Ordinal))
                {
                    for (int rr = 60; rr <= snap - 10 && wc == null; rr += 20)
                        foreach (var (dx, dy) in dirs) { var q = new Pt(cp.At.X + dx * rr / 1000, cp.At.Y + dy * rr / 1000); if (g.S.Grid.Free(q) && !g.S.Grid.LineFree(q, cp.At)) { wc = cp; wq = q; break; } }
                    if (wc != null) break;
                }
                r.Is(wc != null && g.PreviewSnap(OpId.Scout, wq)?.Id != wc.Id, $"a click {(wc == null ? 0 : G.Dist(wq, wc.At))} cm from {wc?.Id} across a solid never snaps to it (snaps to {g.PreviewSnap(OpId.Scout, wq)?.Id ?? "none"})");
                // the snap: walked, held, marked
                int start = g.Tick; Pt from = r.Op(Scout).Pos;
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "move", Point = click }).ok, "the move is taken");
                var cv = c!; c = cv;
                r.Is(r.Op(Scout).Walk != null && r.Op(Scout).Walk!.End == cv.At && g.CoverOf(OpId.Scout) == cv.Id, $"it walks to {c.Id}, not to the click, and holds it");
                r.Is(g.Log.Exists(l => l.Code == "order.move" && l.Text.Contains("takes cover at " + c.Id)), "the move's line says so (no new code)");
                r.To(r.Arrival(Scout)); r.Wait(1);
                r.Eq(r.Op(Scout).Pos, c.At, "it stands on the point");
                r.Is(g.Tick - start >= G.Dist(from, c.At) * 10 / g.R.CellSpeed - 1, "walked at a walk, never a jump");
                var mk = g.CoverMarks(OpId.Scout).FirstOrDefault(m => m.Id == c.Id);
                r.Is(mk != null && mk.HeldBy == Scout && mk.DistanceCm == 0, "its mark shows it held by the Scout");
                // two agents on one point: the Fixer's same click can't take it
                r.To(r.SendTo(Fixer, g.S.Grid.NearestFree(new Pt(click.X + 300, click.Y))!.Value));
                var fx = g.PreviewSnap(OpId.Fixer, click);
                r.Is(fx?.Id != c.Id, $"the Fixer's click by {c.Id} doesn't snap onto the Scout's point (snaps to {fx?.Id ?? "none"})");
                g.Issue(new Order { Tick = g.Tick, Op = Fixer, Do = "move", Point = click });
                r.Is(g.CoverOf(OpId.Fixer) != c.Id && g.CoverOf(OpId.Scout) == c.Id, "one body a point");
                // moving off lets it go; the Fixer may then take it
                var away = g.S.Grid.NearestFree(new Pt(c.At.X + 600, c.At.Y + 600))!.Value;
                g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "move", Point = away }); r.Wait(2);
                r.Is(g.CoverOf(OpId.Scout) != c.Id, "the Scout moves off: the point is let go");
                r.Is(g.PreviewSnap(OpId.Fixer, click)?.Id == c.Id, "and is free to snap to again");
                // a named place (a mission point, a person's) never snaps, drawn or not: Suture F8, d:Meet driver lies by the official car's cv-008
                var md = g.S.Stand("d:Meet driver"); var mdc = g.S.Cover.Where(Ok).OrderBy(x => G.D2(md, x.At)).First();
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "move", To = "d:Meet driver" }).ok && r.Op(Scout).Walk?.End == md && g.CoverOf(OpId.Scout) == "",
                     $"the drawn Scout's move to d:Meet driver ends there ({mdc.Id} is {G.Dist(md, mdc.At)} cm off; a click there would {(g.PreviewSnap(OpId.Scout, md) != null ? "snap" : "not snap")})");
            }, LabStart());

            Add("N151", "—", "Brief 027 B5 mid-volley (EXPERIMENTAL coverSnap): a snap given during a volley is walked like any move: the agent fires the rest of its volley on the way at walking pips, keeps its shots, and has no cover until it stands; the walk into range (close) never snaps.", r =>
            {
                var g = r.G; r.To(T(2));
                var gp = g.PosOf(GuardR)!.Value; int sec = g.Tick / Game.Hz;
                // a cover point within the Scout's effective ring of the guard with a line to him, and a click 1 m off it that snaps to it
                var eff = r.Op(Scout).Gun.W!.EffCm; CoverPoint? c = null; Pt click = default;
                foreach (var cp in g.S.Cover.Where(x => x.Present(sec) && x.Q >= g.R.CoverQPartial && G.Within(x.At, gp, eff - 300) && g.S.Sight(x.At, gp, g.R.Eye)).OrderBy(x => x.Id, StringComparer.Ordinal))
                {
                    foreach (var (dx, dy) in new[] { (100, 0), (0, 100), (-100, 0), (0, -100) }) { var q = new Pt(cp.At.X + dx, cp.At.Y + dy); if (g.S.Grid.Free(q) && g.S.Grid.LineFree(q, cp.At) && !g.S.Cover.Exists(x => x != cp && x.Present(sec) && x.Q >= g.R.CoverQPartial && G.D2(q, x.At) <= G.D2(q, cp.At) + 40000)) { c = cp; click = q; break; } }
                    if (c != null) break;
                }
                if (!r.Is(c != null, $"a cover point in range of the guard ({c?.Id})")) return;
                // stand 4 m off the point, in range, and give a volley
                var stand = g.S.Grid.NearestFree(new Pt(c!.At.X + (gp.X - c.At.X) * 4 / Math.Max(1, (int)G.Dist(gp, c.At) / 100) , c.At.Y + (gp.Y - c.At.Y) * 4 / Math.Max(1, (int)G.Dist(gp, c.At) / 100)))!.Value;
                r.To(r.SendTo(Scout, stand)); r.Wait(1);
                g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "draw" }); r.Wait(g.DrawT + 1);
                r.Is(g.PreviewSnap(OpId.Scout, click)?.Id == c.Id, $"drawn, the click 1 m off {c.Id} snaps to it");
                var a = g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "volley", Target = GuardR, Shots = 3 });
                r.Is(a.ok, $"the volley is taken{(a.ok ? "" : ": " + a.why)}");
                bool closing = r.Op(Scout).CloseWalk;
                r.Is(!closing || g.CoverOf(OpId.Scout) == "", "a walk into range holds no cover point (close never snaps)");
                int left = r.Op(Scout).Gun.VolleyLeft; Pt before = r.Op(Scout).Pos;
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "move", Point = click }).ok && g.CoverOf(OpId.Scout) == c.Id, $"mid-volley, the move snaps to {c.Id}");
                r.Eq(r.Op(Scout).Gun.VolleyLeft, left, "the volley keeps its shots (nothing refunded, nothing added)");
                r.Wait(1);
                r.Is(G.Dist(before, r.Op(Scout).Pos) <= g.R.CellSpeed / Game.Hz + 1 && (r.Op(Scout).Walk != null || r.Op(Scout).Pos == c.At), "one tick later it has walked one tick's worth: no jump to cover");
                r.Is(r.Op(Scout).Pos == c.At || g.PreviewFire(OpId.Scout, GuardR).Moving, "on the way it fires at walking pips (the moving edge)");
                r.Until(() => r.Op(Scout).Walk == null, T(10));
                r.Eq(r.Op(Scout).Pos, c.At, "it ends on the point");
            }, LabStart(s => s.Security = 1));
        }

        // ---------------------------------------------------------------- the balance matrix (§6)

        /// <summary>The runs of brief 027 §6.1, in order.</summary>
        public static readonly string[] LabRuns = { "L1", "L1c", "L2", "L3", "L4", "P-A", "P-G", "P-B" };
        /// <summary>The adversarial rows (Suture F7; Motive, tuning.md §6.6, §6.8): "<run>:<policy>", played by PlaySmart.</summary>
        public static readonly string[] LabAdversarial = { "L3:kite", "L3:reset-kite", "L3:focus-reset-kite", "L3:cycle-kite", "L3:carbine-kite", "P-G:reset",
            "L3:enfgo-carfree-runkite", "L3:focus-enfgo-carfree-runkite", "L3:enfgo-carfree-kite", // Suture retest R5 (R1's cheap line)
            // Suture retest2 N3: hold Vale out of reach, then go (at<s>: the Enforcer stands until second s; hold: steps out of reach while police are up)
            "L3:at208-runkite", "L3:focus-at208-runkite", "L3:at208-kite", "L3:at196-runkite", "L3:at236-runkite", "L3:hold-runkite",
            // ... and one tight-margin pass over the kite rows (tight: step away at the ring + 0.3 m, fire inside max - 0.2 m)
            "L3:tight-kite", "L3:tight-reset-kite", "L3:tight-focus-reset-kite", "L3:tight-cycle-kite", "L3:tight-carbine-kite",
            "L3:tight-enfgo-carfree-runkite", "L3:tight-focus-enfgo-carfree-runkite", "L3:tight-enfgo-carfree-kite",
            "L3:tight-at196-runkite", "L3:tight-at208-runkite", "L3:tight-focus-at208-runkite", "L3:tight-hold-runkite",
            // third retest (Suture retest3.md N7, N8): a shorter kite step that stays in the band, and a player who ignores the ambush crew
            "L3:km10-fm5-step200-norivals-at208-runkite", "L3:km15-fm5-step300-norivals-enfgo-carfree-runkite", "L3:tight-step200-focus-norivals-at236-runkite",
            "L3:runfire-kite", "L3:circle-kite", "L3:charge", "P-G:runfire" }; // tuning.md §9.6 (engine 0.15.0): fire on the move

        /// <summary>Play one run of the matrix on a start (a preset's fightLab, or none for today). `routes` holds the route scripts the prepared
        /// runs and L3 start from (route-a-board, route-g-fast-board, route-b-board), by name. `fieldUser`: the player raises the field when an
        /// agent under fire is below half health (the field-on variant).</summary>
        public static Run PlayLabRun(Engine.Inputs i, string id, Start start, IReadOnlyDictionary<string, string> routes, bool coverPlayer = false)
        {
            int colon = id.IndexOf(':'); // an adversarial row: "<run>:<policy>" (ScenariosFightLabAdv.cs)
            if (colon > 0) return PlaySmart(i, id.Substring(0, colon), start, routes, LabPolicy.Parse(id.Substring(colon + 1)), coverPlayer);
            Resets = 0; Kites = 0; // Suture retest R5: the adversarial counters don't carry over into a matrix row
            var r = new Run(Engine.New(i, start)); var g = r.G; g.TrackContact = true; // verdict 6.4.8 (output only)
            r.CoverPlayer = coverPlayer && g.CoverSnapOn; // B5 probe (lab-cover): the player seeks the marks (Run.CoverPlayer); every other row is the same player as before
            if (g.Lab && g.Labr != null && (start.LabField ?? g.Labr.Field))
                r.PerTick = () =>
                {
                    foreach (var op in g.Ops)
                        if (!op.Down && !op.Inside && !op.InCar && op.Busy == null && op.Gun.Health * 2 < op.Gun.Max && op.FieldUntil <= g.Tick && op.Charge >= g.Labr.FieldCost && g.Combatants().Exists(c => g.GunOf(c)?.Target == Sim.Op.Name(op.Id)))
                            g.Issue(new Order { Tick = g.Tick, Op = Sim.Op.Name(op.Id), Do = "field" });
                };
            switch (id)
            {
                case "L1": LoudFront(r, false); break;
                case "L1c": LoudFront(r, false, loop: true); break; // Keel's probe: L1 with the entrance camera looped first (L1 as written fails on the camera, before the fight)
                case "L2": LoudPickup(r); break;
                case "L3": LoudGuardShot(r, routes["route-g-fast-board"]); break;
                case "L4": LoudFront(r, true); break;
                case "P-A": Prepared(r, routes["route-a-board"], 2490); break;
                case "P-G": Prepared(r, routes["route-g-fast-board"], 2066, pgPosts: true); break;
                case "P-B": Prepared(r, routes["route-b-board"], 4470); break;
                default: throw new ArgumentException($"no run {id}");
            }
            if (!g.Over) g.RunTo(Math.Max(g.Tick, T(g.S.Over)) + T(1));
            g.Finish();
            return r;
        }

        static readonly string[] Cell4 = { Scout, Fixer, Tech, Enf };
        /// <summary>0:01, all draw and fire at will: panic with the lab (allowed any time), today draw and stance free (today's panic needs a fight on).</summary>
        static void PanicNow(Run r)
        {
            var g = r.G;
            if (g.Lab) { g.Issue(new Order { Tick = g.Tick, Op = "cell", Do = "panic" }); return; }
            foreach (var op in Cell4) { g.Issue(new Order { Tick = g.Tick, Op = op, Do = "draw" }); g.Issue(new Order { Tick = g.Tick, Op = op, Do = "stance", Stance = "free" }); }
        }
        /// <summary>Who the perfect player shoots: every combatant (Game.Combatants) and the city's gunmen up on the street.</summary>
        static List<string> Foes(Run r) { var l = r.G.Combatants(); foreach (var x in r.CityGunmen()) if (!l.Contains(x)) l.Add(x); return l; }
        static void Hold(Run r, Func<bool> done, int limitS, bool advance, params string[] ops) => r.Engage(() => Foes(r), done, limitS, ops, false, advance);
        static void DrawAll(Run r) { foreach (var op in Cell4) if (!r.Op(op).Gun.Drawn) r.G.Issue(new Order { Tick = r.G.Tick, Op = op, Do = "draw" }); }

        /// <summary>Vale is with the Enforcer: walk him to the car, fight it out (the car free), board everyone standing, Go.</summary>
        static void FightToCar(Run r)
        {
            var g = r.G; if (g.Over || g.Vale != ValeAt.WithEnforcer) return;
            DrawAll(r);
            g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "move", To = "d:Getaway bay" });
            r.FightItOut(150, Scout, Fixer, Tech);
            CarryDowned(r);
            if (!g.Over) r.GetAway(Cell4);
        }

        /// <summary>The Enforcer at the door: Enter (68 s at Alert 2) while the others hold the street, out by the service door with Vale.</summary>
        static void EnterAndOut(Run r)
        {
            var g = r.G; if (g.Over) return;
            int at = r.Send(Enf, "d:Office entrance");
            Hold(r, () => g.Tick >= at || r.Op(Enf).Down, 60, false, Scout, Fixer, Tech);
            if (g.Over) return;
            for (int k = 0; k < T(20) && !g.Over; k++) { if (g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "enter" }).ok) break; Hold(r, () => true, 1, false, Scout, Fixer, Tech); g.Step(); }
            Hold(r, () => r.Op(Enf).Busy == null && !r.Op(Enf).Inside, 200, false, Scout, Fixer, Tech);
        }

        /// <summary>L1 front (L4 everyone: first fight every combatant that comes, nearest first, until nothing engages for 15 s or 4:00).</summary>
        static void LoudFront(Run r, bool everyone, bool loop = false)
        {
            var g = r.G; r.To(T(1)); PanicNow(r);
            if (loop) { r.To(r.Send(Tech, "cabinet")); r.Do(Tech, "loop", "entrance"); r.Done(Tech); }
            if (everyone)
            {
                int quiet = -1;
                Hold(r, () => { if (Foes(r).Count > 0) quiet = g.Tick; return (quiet >= 0 && g.Tick - quiet >= T(15) && g.Tick > T(30)) || g.Tick >= T(240); }, 260, true, Cell4);
            }
            if (g.Over) return;
            r.SendTo(Enf, BesideDoor);
            foreach (var (op, dx, dy) in new[] { (Scout, -400, 0), (Fixer, -400, 300), (Tech, -400, -300) }) { if (loop && op == Tech) continue; var q = g.S.Grid.NearestFree(BesideDoor + new Pt(dx, dy)); if (q.HasValue) r.SendTo(op, q.Value); }
            Hold(r, () => r.Op(Enf).Walk == null || r.Op(Enf).Down, 120, false, Scout, Fixer, Tech);
            if (g.Over) return;
            if (!g.GuardDown) { g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "fire", Target = GuardR }); Hold(r, () => g.GuardDown, 60, false, Scout, Fixer, Tech); }
            EnterAndOut(r); FightToCar(r);
        }

        /// <summary>L2 pickup: all draw at 0:01 and walk to the pickup; fight what comes; the Take at the official car at 6:10; to the getaway car.</summary>
        static void LoudPickup(Run r)
        {
            var g = r.G; r.To(T(1)); PanicNow(r);
            r.Send(Enf, "d:Boarding pavilion"); r.Send(Scout, "d:Boarding pavilion"); r.Send(Fixer, "d:Getaway bay"); r.Send(Tech, "d:Getaway bay");
            Hold(r, () => g.Tick >= T(370), 400, true, Scout, Fixer, Tech);
            for (int k = 0; k < T(140) && !g.Over && g.Vale != ValeAt.WithEnforcer; k++)
            {
                if (r.Op(Enf).Busy == null && !r.Op(Enf).Down) g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "take" });
                if (r.Op(Enf).Busy != null) { Hold(r, () => r.Op(Enf).Busy == null, 60, false, Scout, Fixer, Tech); continue; }
                Hold(r, () => true, 1, false, Scout, Fixer, Tech); g.Step();
            }
            FightToCar(r);
        }

        /// <summary>L3: route G-fast's timing, but at 2:03.6 the Enforcer shoots the guard at his mark instead of the knockout; then Enter.</summary>
        static void LoudGuardShot(Run r, string routeG)
        {
            var g = r.G; Replay(r, routeG, 1236);
            if (g.Over) return;
            g.Issue(new Order { Tick = g.Tick, Op = Enf, Do = "fire", Target = GuardR });
            DrawAll(r);
            Hold(r, () => g.GuardDown, 60, false, Scout, Fixer, Tech);
            EnterAndOut(r); FightToCar(r);
        }

        /// <summary>A prepared route: its script's orders before the fight, then the Enforcer walks Vale to the car as the ambush springs, the
        /// three fight it out with focus fire (the crew broken), everyone standing boards, Go.</summary>
        static void Prepared(Run r, string script, int fightTick, bool pgPosts = false)
        {
            var g = r.G; Replay(r, script, fightTick);
            if (g.Over) return;
            if (g.Vale != ValeAt.WithEnforcer) { r.Until(() => g.Vale == ValeAt.WithEnforcer, T(60)); if (g.Over) return; }
            r.Focus = g.Lab && pgPosts; // tuning.md §7.11.3 (b): P-G's player supports the Enforcer (lab; P-A and P-B keep nearest-first, which §7.11.3's done-check holds unchanged; today keeps nearest-first)
            if (pgPosts && g.Lab && PGPosts(r) is Pt[] posts) // §7.11.3 (c): P-G's posts inside 15 m (lab; today keeps the crew posts)
            {
                string[] who = { Scout, Fixer, Tech };
                for (int k = 0; k < 3; k++) { if (r.Op(who[k]).Busy == null) r.SendTo(who[k], posts[k]); r.Do(who[k], "draw"); AtWill(r, who[k]); }
                r.Do(Enf, "draw"); r.Send(Enf, "d:Getaway bay");
                bool techSent = r.Op(Tech).Busy == null;
                while (!g.Over && g.FightSprung == Never) { if (!techSent && r.Op(Tech).Busy == null) { techSent = true; r.SendTo(Tech, posts[2]); r.Do(Tech, "draw"); AtWill(r, Tech); } g.Step(); }
            }
            else { Support(r); ToAmbush(r); }
            r.FightItOutAll(150, Scout, Fixer, Tech);
            CarryDowned(r);
            if (!g.Over) r.GetAway(Cell4);
        }
        /// <summary>Tuning.md §13.4 (engine 0.18.0): P-G's support agents fire at will once drawn, as they did when the lab's draw was at will
        /// (stances two); with the lab's three a draw is return, so the player sets free (V).</summary>
        static void AtWill(Run r, string op) { var g = r.G; if (g.Lab && g.OpOf(Sim.Op.Parse(op)).Gun.Drawn) g.Issue(new Order { Tick = g.Tick, Op = op, Do = "stance", Stance = "free" }); }
        /// <summary>Tuning.md §7.11.3 (c): P-G's posts for the Scout, the Fixer and the Tech: three free cover points (at least partial, present) within
        /// 15 m of the getaway car, each 6–14 m from an ambush stand it shields against (outside point blank), the nearest each one's crew post,
        /// distinct; null if three can't be found (the crew posts then).</summary>
        static Pt[]? PGPosts(Run r)
        {
            var g = r.G; var car = g.S.At(g.S.GetawayCar); int sec = g.Tick / Game.Hz;
            var amb = g.S.FightAmbush.Select(a => g.S.Stand(a)).ToList();
            var held = new HashSet<string>(g.Hostiles.Select(h => h.Gun.Cover).Where(c => c.Length > 0), StringComparer.Ordinal);
            var ok = g.S.Cover.Where(c => c.Present(sec) && c.Q >= g.R.CoverQPartial && !held.Contains(c.Id) && G.Within(c.At, car, 1500) && g.S.Grid.Free(c.At)
                && amb.Exists(a => !G.Within(c.At, a, 600) && G.Within(c.At, a, 1400) && CoverRules.Protects(c, a, sec, g.R.PointBlankCm))).ToList();
            var used = new HashSet<string>(StringComparer.Ordinal); var posts = new Pt[3]; string[] crew = { "crew:scout", "crew:fixer", "crew:tech" };
            for (int k = 0; k < 3; k++)
            {
                var from = g.S.Stand(crew[k]);
                var c = ok.Where(x => !used.Contains(x.Id)).OrderBy(x => G.D2(x.At, from)).ThenBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault();
                if (c == null) return null;
                used.Add(c.Id); posts[k] = c.At;
            }
            return posts;
        }

        /// <summary>As N11b: anyone downed is carried to the car by the nearest of the three still standing (the perfect player leaves nobody).</summary>
        static void CarryDowned(Run r)
        {
            var g = r.G;
            foreach (var down in new[] { Scout, Fixer, Tech })
            {
                if (g.Over || !r.Op(down).Down || r.Op(down).CarriedBy.HasValue) continue;
                var by = Array.Find(new[] { Fixer, Scout, Tech }, o => o != down && !r.Op(o).Down && !r.Op(o).Carrying.HasValue && r.Op(o).CarryBody.Length == 0 && !r.Op(o).InCar);
                if (by == null) continue;
                var at = g.S.Grid.NearestFree(r.Op(down).Pos); if (!at.HasValue) continue;
                int t = r.SendTo(by, at.Value); while (!g.Over && g.Tick < t) g.Step();
                if (!g.Over && g.Issue(new Order { Tick = g.Tick, Op = by, Do = "carry", Target = down }).ok) r.Done(by);
            }
        }

        /// <summary>A script's orders before `beforeTick`, each at its tick (refused ones too: they are the player's).</summary>
        static void Replay(Run r, string scriptText, int beforeTick)
        {
            var s = Suborned.Sim.Script.Parse(scriptText); var g = r.G;
            var list = s.Orders.Select((o, k) => (o, k)).OrderBy(x => x.o.Tick).ThenBy(x => x.k).ToList();
            foreach (var (o, _) in list)
            {
                if (o.Tick >= beforeTick) break;
                g.RunTo(o.Tick); if (g.Over) return;
                o.Tick = g.Tick; g.Issue(o);
            }
            g.RunTo(beforeTick);
        }

        /// <summary>What a played run of the matrix reads (§6.2, §6.3), with its verdict.</summary>
        public static LabRow Row(string id, string preset, Game g)
        {
            var d = Debrief.Of(g);
            var row = new LabRow
            {
                Run = id, Preset = preset, Result = g.Result.ToString().ToLowerInvariant(), Fail = g.FailCode, EndTick = g.Tick,
                TrailMission = g.Trail, TrailTotal = g.Trail + d.Next.CampaignTrail, Alert = g.Alert, StandingStart = g.StandingAtStart, StandingEnd = g.Standing,
                PublicSecurity = d.PublicSecurity, Investigation = d.InvestigationState, OpenCases = d.Next.OpenCases, Loud = id.StartsWith("L", StringComparison.Ordinal),
            };
            foreach (var op in g.Ops) { row.CellHealth += op.Gun.Health; row.CellMax += op.Gun.Max; if (op.Down) row.OpsDown++; }
            row.OpsCaptured = g.Captured.Count;
            // §6.6.2: a down is the last shot's (the damage notes follow their shot.hit); free while its shooter had not been fired on
            var firedOn = new HashSet<string>(StringComparer.Ordinal); var free = new Dictionary<string, int>(StringComparer.Ordinal); (string by, string t) prev = ("", "");
            var sideFired = new HashSet<string>(StringComparer.Ordinal); var freeSide = new Dictionary<string, int>(StringComparer.Ordinal);
            string SideOf(string x) => x == "rivals" || x.StartsWith("rival-", StringComparison.Ordinal) ? "rivals" : x.StartsWith("security-", StringComparison.Ordinal) || x.StartsWith("officer-", StringComparison.Ordinal) ? "police" : x == "guard" || x == "patrol" ? x : "units";
            var downCodes = new HashSet<string>(StringComparer.Ordinal) { "fight.down", "guard.down", "patrol.down", "security.down", "security.officer-down", "response.down", "rivals.retreat" };
            row.ContactSeen = g.ContactPolice.Count; row.ContactHad = g.ContactHad.Count;
            foreach (var x in g.ContactMissing()) (x.StartsWith("police-", StringComparison.Ordinal) ? row.ContactMissingUnits : row.ContactMissing).Add(x); // the fight's bodies; the brief-013 units apart (Keel: the scope is open, see fight-lab.md §9)
            foreach (var l in g.Log)
            {
                if (l.Tick >= g.FightStart && (l.Code == "shot.hit" || l.Code == "shot.miss"))
                {
                    int a0 = l.Text.IndexOf(" fires on ", StringComparison.Ordinal);
                    if (a0 > 0) { string b0 = l.Text.Substring(0, a0), r0 = l.Text.Substring(a0 + 10); string t0 = r0.Substring(0, Math.Max(0, r0.IndexOf(' '))); bool cb = b0 == "scout" || b0 == "fixer" || b0 == "tech" || b0 == "enforcer", ct = t0 == "scout" || t0 == "fixer" || t0 == "tech" || t0 == "enforcer"; if (!cb && ct) row.ShotsAtCellAfterVale++; }
                }
                if (downCodes.Contains(l.Code))
                {
                    bool cellBy0 = prev.by == "scout" || prev.by == "fixer" || prev.by == "tech" || prev.by == "enforcer";
                    if (cellBy0 && !firedOn.Contains(prev.by)) free[prev.by] = (free.TryGetValue(prev.by, out int fv) ? fv : 0) + 1;
                    if (cellBy0 && !sideFired.Contains(SideOf(prev.t))) { string key = prev.by + " on the " + SideOf(prev.t); freeSide[key] = (freeSide.TryGetValue(key, out int fs) ? fs : 0) + 1; }
                    prev = ("", ""); continue;
                }
                if (l.Code != "shot.hit" && l.Code != "shot.miss") continue;
                int at = l.Text.IndexOf(" fires on ", StringComparison.Ordinal); if (at < 0) continue;
                string by = l.Text.Substring(0, at), rest = l.Text.Substring(at + 10); string t = rest.Substring(0, rest.IndexOf(' '));
                bool cellBy = by == "scout" || by == "fixer" || by == "tech" || by == "enforcer", cellT = t == "scout" || t == "fixer" || t == "tech" || t == "enforcer";
                if (cellBy) row.CellShots++;
                if (cellT && l.Code == "shot.hit") row.CellHits++;
                if (!cellBy && cellT) { firedOn.Add(t); sideFired.Add(SideOf(by)); }
                prev = l.Code == "shot.hit" && cellBy && !cellT ? (by, t) : ("", "");
            }
            foreach (var kv in free.OrderBy(x => x.Key, StringComparer.Ordinal)) if (kv.Value > row.FreeDowns) { row.FreeDowns = kv.Value; row.FreeDownsBy = kv.Key; }
            foreach (var kv in freeSide.OrderBy(x => x.Key, StringComparer.Ordinal)) if (kv.Value > row.FreeSide) { row.FreeSide = kv.Value; row.FreeSideBy = kv.Key; }
            row.FoesDown = g.Hostiles.Count(h => h.Down) + (g.GuardDown ? 1 : 0) + (g.PatrolDown ? 1 : 0) + g.SecGuns.Count(s => s.Down) + g.Units.Count(u => !u.Legacy && u.Gun.Down);
            if (row.Loud)
            {
                if (!row.Success) row.Verdict = "pass (fail)";
                else
                {
                    var miss = new List<string>();
                    if (row.TrailTotal < 12) miss.Add($"Trail {row.TrailTotal} < 12");
                    if (row.Alert < 2) miss.Add($"Alert {row.Alert} < 2");
                    if (row.StandingEnd > row.StandingStart - 2) miss.Add($"Standing {row.StandingEnd} > start−2");
                    // Motive 2026-10-20 (briefs/027/motive/tuning.md §3.1): "public security High" is out of the AND (it needs a civilian witness of violence)
                    if (row.Investigation != "active") miss.Add($"investigation {row.Investigation}");
                    if (!(row.OpsDown > 0 || row.OpsCaptured > 0 || row.HealthLostPct >= 40 || row.OpenCases > 0)) miss.Add($"no op down or captured, health lost {row.HealthLostPct} % < 40, no open case");
                    // 6.4.8 (Motive, tuning.md §7.1, Suture R5): the getaway must be contested
                    if (row.ContactMissing.Count > 0) miss.Add($"6.4.8a {string.Join(", ", row.ContactMissing)} up at the end, never had the Vale holder in reach");
                    if (row.ShotsAtCellAfterVale == 0) miss.Add("6.4.8b no hostile shot at the cell from Vale coming out");
                    if (row.ContactMissingUnits.Count > 0) row.Why.Add($"note 6.4.8a, brief-013 units (not judged): {string.Join(", ", row.ContactMissingUnits)} up at the end without contact");
                    row.Verdict = miss.Count == 0 ? "pass (heavy debrief)" : "FAIL"; row.Why.AddRange(miss);
                }
            }
            else
            {
                var miss = new List<string>();
                if (!row.Success) miss.Add($"{row.Result} {row.Fail}");
                if (row.TrailTotal > 7) miss.Add($"Trail {row.TrailTotal} > 7");
                if (row.Alert > 1) miss.Add($"Alert {row.Alert} > 1");
                if (row.StandingEnd < row.StandingStart) miss.Add($"Standing {row.StandingEnd} < start");
                if (row.OpsDown > 0) miss.Add($"{row.OpsDown} op down");
                int line = id.StartsWith("P-G", StringComparison.Ordinal) ? 33 : 25; // tuning.md §6.5: P-G prepares the door and nothing for the getaway: ≤ 33 % with no op down; P-A, P-B 25 %
                if (row.HealthLostPct > line) miss.Add($"health lost {row.HealthLostPct} % > {line}");
                row.Verdict = miss.Count == 0 ? "pass" : "FAIL"; row.Why.AddRange(miss);
            }
            return row;
        }

        /// <summary>§6.4's "the numbers are wrong" triggers over one preset's rows (and today's, for trigger 6).</summary>
        public static List<string> Triggers(List<LabRow> rows, List<LabRow>? today)
        {
            var t = new List<string>();
            var loud = rows.Where(x => x.Loud).ToList(); var prep = rows.Where(x => !x.Loud).ToList();
            int bestPrep = prep.Where(x => x.Success).Select(x => x.TrailTotal).DefaultIfEmpty(int.MaxValue).Min();
            var pg = prep.FirstOrDefault(x => x.Run == "P-G");
            foreach (var l in loud.Where(x => x.Success))
            {
                if (l.CellHits == 0 || l.HealthLostPct < 25) t.Add($"6.4.1 {l.Run} wins with {l.CellHits} hits taken, {l.HealthLostPct} % health lost");
                if (bestPrep != int.MaxValue && l.TrailTotal < 2 * bestPrep) t.Add($"6.4.2 {l.Run} wins with Trail {l.TrailTotal} < 2 × {bestPrep}");
                if (pg != null && pg.Success && l.EndTick < pg.EndTick - T(15)) t.Add($"6.4.3 {l.Run} wins at {Report.Clock(l.EndTick)}, more than 15 s before P-G ({Report.Clock(pg.EndTick)})");
            }
            int standingHeld = loud.Count(x => x.Success && x.StandingEnd >= x.StandingStart);
            if (standingHeld >= 2) t.Add($"6.4.4 {standingHeld} loud runs win with Standing at or above the start");
            foreach (var p in prep) if (!p.Success || p.OpsDown > 0) t.Add($"6.4.5 {p.Run} {(p.Success ? $"downs {p.OpsDown} op" : "fails " + p.Fail)}");
            foreach (var l in loud) if (l.Run.Contains(':') && l.FreeSide > 1) t.Add($"6.6.2 {l.Run}: the {l.FreeSideBy} downs {l.FreeSide} before that side fires on the cell");
            if (today != null)
                foreach (var l in loud.Where(x => x.Success))
                {
                    var tl = today.FirstOrDefault(x => x.Run == l.Run);
                    if (tl != null && !tl.Success && l.Verdict != "pass (heavy debrief)") t.Add($"6.4.6 {l.Run} failed under today and wins under this preset without a heavy debrief");
                }
            return t;
        }

        /// <summary>§6.4.7, the lethality ceiling (internal tuning): an operative at full health under focus from two trained hostiles at the
        /// effective ring (the sidearm's eff, open ground, both still), counted by the accumulator: game seconds to down. Op 40 / Enforcer 60 in the lab.</summary>
        public static (double op, double enforcer) Lethality(Game g)
        {
            var w = g.PistolW; int acc = g.Lab && g.Labr!.Pips ? g.Labr.Chance[g.R.LabPips(w, w.EffCm, false, false, false, false)] : g.R.Accuracy(w, w.EffCm, false, false, false, false);
            double Down(int health)
            {
                int s0 = g.Lab ? g.Labr!.AccStart : g.R.AccStart; int[] a = { s0, s0 }; int hp = health; // the lab: a new target's count is fight.lab.accStart (tuning.md §6.2)
                for (int tick = g.AimNewT; tick < 100000; tick += w.Every)
                    for (int k = 0; k < 2; k++) { a[k] += acc; if (a[k] >= 1000) { a[k] -= 1000; hp -= w.Damage; if (hp <= 0) return tick / (double)Game.Hz; } }
                return double.PositiveInfinity;
            }
            return (Down(g.OpOf(OpId.Scout).Gun.Max), Down(g.OpOf(OpId.Enforcer).Gun.Max));
        }

        /// <summary>§6.4.7b (Motive, tuning.md §6.2), the cell → hostile floor: an uncovered rival at full health under focus from three trained
        /// sidearms at their effective ring (open ground, all still), counted by the accumulator: game seconds from the first shot to down.
        /// The line is at least 1.0 s.</summary>
        public static double LethalityCell(Game g)
        {
            var w = g.Lab ? g.Labr!.Weapons["sidearm"] : g.R.Pistol; int acc = g.Lab && g.Labr!.Pips ? g.Labr.Chance[g.R.LabPips(w, w.EffCm, false, false, false, false)] : g.R.Accuracy(w, w.EffCm, false, false, false, false);
            int s0 = g.Lab ? g.Labr!.AccStart : g.R.AccStart, hp = g.R.HealthRival * g.HealthScale; int[] a = { s0, s0, s0 };
            for (int tick = 0; tick < 100000; tick += w.Every)
                for (int k = 0; k < 3; k++) { a[k] += acc; if (a[k] >= 1000) { a[k] -= 1000; hp -= w.Damage; if (hp <= 0) return tick / (double)Game.Hz; } }
            return double.PositiveInfinity;
        }

        /// <summary>The playground's two checkpoints (§5): CP-LOUD, N10's start and orders to the tick the Enforcer fires on the guard (the fire
        /// order its last); CP-SAFE, N11's (route A quiet) to the tick the Enforcer is out with Vale, before the ambush. Each as a played Game.</summary>
        public static (Game loud, Game safe) Checkpoints(Engine.Inputs i)
        {
            var rl = new Run(Engine.New(i, Level(1))); var g = rl.G;
            rl.To(Max(rl.Send(Tech, "cabinet"), rl.SendTo(Enf, BesideDoor)));
            rl.Do(Tech, "loop", "entrance"); rl.Done(Tech); rl.Do(Tech, "reroute", "patrol"); rl.Done(Tech);
            int mark = g.NextMarkOf(g.GuardP, g.Tick); if (mark - g.Tick < T(30)) rl.To(mark + 1);
            rl.Fire(Enf, GuardR);
            var rs = new Run(Engine.New(i)); RouteA(rs, reroute: true);
            return (rl.G, rs.G);
        }
    }
}
