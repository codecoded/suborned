// Engine 0.17.0: the trigger, locks and follow fire (tuning.md §11; Sky on firing 2026-10-21, brief 024). Range scenarios (Scenarios.Range),
// lane layout as Motive's probe (out/motive027/s11). Rules are checks; Motive's §11.3 numbers are lines (met or missed, never a failing check).
//    F11-NEAR      7.9 m from the driven agent follows, 8.1 m doesn't (follow off: it fires with the trigger, from where it stands)
//    F11-HOLSTER   a holstered agent in the ring never follows and never draws, with the setting off or on
//    F11-JOIN      a joining follower waits aim-new, as the driven agent does (today: acc.start; the lab: the pair's count)
//    F11-TAP       tap, tap, tap in today keeps the target and the count: no aim-new, no reset to acc.start
//    F11-BUF       a press in the cooldown fires once, at the ready tick (a buffer of one, not a queue)
//    F11-LOCK      follow on: the press locks those in the ring with a line; the lock outlives the release and never walks
//    F11-REPLAY    a hold, tap, release and follow-setting session replays hash-identical from its start and its orders
//    F11-FOCUS     §11.3: four against four trained officers at 14 m, focus and split (lines: 5.6 s 0 down; 7.1 s 1 down)
//    F11-RUSH      §11.3: the rush on a lone officer at 14 m, one press with follow on (line: down in 1.1 s, 1 hit taken)
//    F11-SPAM      tap spam never beats the hold; the target-switch spam measured (adversarial line)
//    F12-ROT       §12.2: three shooters at 15.3 m firing in turn: the officer keeps its target and fires on cadence
//    F12-NONE      §12.2: every shooter beyond the officer's max ring: rule 3's cover and close-in, as built
//    F13-HOLD      §13.2: a holding agent in the ring neither follows (follow off) nor is locked by a press (follow on); return, it follows
//    F13-RELOCK    a press on the tick after a lock's target falls locks that agent again; a press in the tick it falls too (engine 0.18.0)
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static Answer Trig(Run r, string op, string t, bool on, bool force = false) => r.G.Issue(new Order { Tick = r.G.Tick, Op = op, Do = "trigger", Target = on ? t : "", On = on, Force = force });
        static Answer FollowSetting(Run r, bool on) => r.G.Issue(new Order { Tick = r.G.Tick, Op = "cell", Do = "follow", On = on });
        static Answer TakeControl(Run r, string op) { var o = r.Op(op); return r.G.Issue(r.G.DriveSample(o.Id, o.Pos, false, 0)); }
        static int FirstShot(Run r, string by, string t, int from) { foreach (var s in r.Shots()) if (s.tick >= from && s.by == by && s.target == t) return s.tick; return -1; }
        static List<int> ShotTicks(Run r, string by, int from, int to = int.MaxValue) => r.Shots().Where(s => s.by == by && s.tick >= from && s.tick < to).Select(s => s.tick).ToList();

        /// <summary>The near set-up: the Enforcer 14 m along +x from the near slot, the Scout 7.9 m and the Fixer 8.1 m from it (3-4-5 offsets,
        /// toward the officer), the Tech 3 m from it holstered; all but the Tech drawn; one trained officer at near. Returns the Enforcer's stand.</summary>
        static Pt F11Near(Run r, bool techDrawn = false, int officers = 1)
        {
            var g = r.G; var h = g.RangeSlot("near", 1); var e = new Pt(h.X + 1400, h.Y);
            var at = new Dictionary<string, Pt> { ["enforcer"] = e, ["scout"] = new Pt(e.X - 474, e.Y + 632), ["fixer"] = new Pt(e.X - 486, e.Y + 648), ["tech"] = new Pt(e.X + 300, e.Y) };
            int t = 0; foreach (var kv in at) t = Math.Max(t, r.SendTo(kv.Key, kv.Value));
            r.To(t);
            foreach (var kv in at) r.Eq(r.Op(kv.Key).Pos, kv.Value, $"{kv.Key} stands at its mark");
            r.Do("enforcer", "draw"); r.Do("scout", "draw"); r.Do("fixer", "draw"); if (techDrawn) r.Do("tech", "draw");
            r.Wait(Game.T(2));
            Spawned(r, "police", "near", officers);
            r.Is(TakeControl(r, "enforcer").ok, "the Enforcer is driven");
            return e;
        }

        /// <summary>The Scout alone, driven, drawn, 14 m from one trained officer (3 health, so a pistol needs three hits).</summary>
        static void F11Scout(Run r)
        {
            var g = r.G; var h = g.RangeSlot("near", 1);
            r.To(r.SendTo("scout", new Pt(h.X + 1400, h.Y))); r.Do("scout", "draw"); r.Wait(Game.T(2));
            Spawned(r, "police", "near", 1); r.Is(TakeControl(r, "scout").ok, "the Scout is driven");
        }
        static void Tap(Run r, string op, string t) { Trig(r, op, t, true); r.G.Step(); Trig(r, op, "", false); }

        static void TriggerRange()
        {
            AddRange("F11-NEAR", "today", "lane", "§11.1: within 8 m of the driven agent follows, beyond doesn't; a follower fires with the trigger from where it stands", r =>
            {
                var g = r.G; var e = F11Near(r);
                r.Eq(G.Isqrt(G.D2(r.Op("scout").Pos, e)), 790L, "the Scout stands 7.90 m from the Enforcer");
                r.Eq(G.Isqrt(G.D2(r.Op("fixer").Pos, e)), 810L, "the Fixer stands 8.10 m from it");
                r.Eq(g.FollowRingCm, 800, "the ring is follow-m 8 m");
                r.Eq(string.Join(",", g.FollowersOf(OpId.Enforcer)), "Scout", "only the Scout follows (7.9 m in, 8.1 m out; the Tech is holstered)");
                var sp = r.Op("scout").Pos; int p = g.Tick;
                r.Is(Trig(r, "enforcer", "officer-1", true).ok, "the trigger pressed on officer-1");
                r.Wait(1);
                r.Eq(r.Op("scout").Gun.FollowBy, "enforcer", "the Scout follows the Enforcer's fire (logged fire.follow)");
                r.Eq(g.FireStateOf(OpId.Scout).Mode, "follow", "and the host reads follow");
                r.Eq(r.Op("fixer").Gun.FollowBy, "", "the Fixer, 8.1 m out, doesn't");
                r.Wait(Game.T(3));
                r.Is(FirstShot(r, "scout", "officer-1", p) > 0, "the Scout fires on officer-1 with the trigger");
                r.Eq(r.Op("scout").Pos, sp, "from where it stands: a follower never walks");
                r.Is(g.Log.All(x => x.Code != "fire.follow" || !x.Text.StartsWith("fixer", StringComparison.Ordinal)), "the Fixer never followed");
                r.Is(r.Shots().Where(s => s.by == "fixer").All(s => s.target != "officer-1" || g.Log.Any(x => x.Tick <= s.tick && x.Text.StartsWith("officer-1 fires on fixer", StringComparison.Ordinal))), "any Fixer shot on officer-1 is return fire after officer-1 shot at it");
            });

            AddRange("F11-HOLSTER", "today", "lane", "§11.1: a holstered agent inside the ring never follows and never draws, setting off or on", r =>
            {
                var g = r.G; F11Near(r);
                r.Is(!g.FollowersOf(OpId.Enforcer).Contains(OpId.Tech), "the Tech (3 m, holstered) is no follower");
                int p = g.Tick; Trig(r, "enforcer", "officer-1", true); r.Wait(Game.T(3)); Trig(r, "enforcer", "", false); r.Wait(2);
                FollowSetting(r, true); Trig(r, "enforcer", "officer-1", true); r.Wait(2);
                r.Eq(g.LockOf(OpId.Tech), "", "follow on: the press locks nothing on the Tech");
                r.Wait(Game.T(2));
                r.Eq(r.Op("tech").Gun.Drawn, false, "the Tech's gun stays holstered: the draw is the player's F");
                r.Eq(ShotTicks(r, "tech", p).Count, 0, "and it fires nothing");
            });

            foreach (var preset in new[] { "today", "lab" })
            {
                string sfx = preset == "lab" ? "-LAB" : ""; bool lab = preset == "lab";
                AddRange("F11-JOIN" + sfx, preset, "lane", "§11.2 rule 1: a joining follower pays aim-new like any new target, as the driven agent does" + (lab ? " (the lab: the pair's count)" : " (today: acc.start)"), r =>
                {
                    var g = r.G; F11Near(r);
                    if (lab) r.Eq(r.Op("scout").Gun.Stance, "return", "the lab: a draw sets return, as today (tuning.md §13.4), so the Scout follows");
                    int p = g.Tick; r.Is(Trig(r, "enforcer", "officer-1", true).ok, "pressed on officer-1");
                    r.Until(() => FirstShot(r, "scout", "officer-1", p) > 0 && FirstShot(r, "enforcer", "officer-1", p) > 0, Game.T(5));
                    int se = FirstShot(r, "enforcer", "officer-1", p), ss = FirstShot(r, "scout", "officer-1", p);
                    r.Is(se > 0 && ss > 0, $"both fire ({se - p}, {ss - p} ticks after the press)");
                    r.Eq(ss - p, se - p, "the follower's first shot waits the same aim-new as the driven agent's");
                    r.Is(ss - p >= g.AimNewT, $"at least aim-new ({g.AimNewT} ticks) after the press: no waiver");
                    r.Eq(r.Op("scout").Gun.Target, "officer-1", "the follower took the target");
                });

                AddRange("F11-TAP" + sfx, preset, "lane", "§11.2 rule 2: tap, tap, tap keeps the target and the count: no aim-new, no reset" + (lab ? "" : " to acc.start 500"), r =>
                {
                    var g = r.G; F11Scout(r);
                    int p = g.Tick; Tap(r, "scout", "officer-1");
                    r.Until(() => ShotTicks(r, "scout", p).Count >= 1, Game.T(3));
                    var en = r.Op("scout").Gun; int s1 = ShotTicks(r, "scout", p)[0], acc1 = en.Acc;
                    r.Eq(en.Target, "officer-1", "released: the target is kept");
                    r.Wait(3); Tap(r, "scout", "officer-1"); // tap again in the cooldown
                    r.Eq(en.Acc, acc1, "a press on the same target resets nothing (the count is the pair's)");
                    r.Until(() => ShotTicks(r, "scout", p).Count >= 2, Game.T(3));
                    int s2 = ShotTicks(r, "scout", p)[1];
                    r.Eq(s2 - s1, en.W!.Every, "the second shot comes on cadence: no aim-new after a release");
                    r.Wait(Game.T(2)); int s2n = ShotTicks(r, "scout", p).Count;
                    int p3 = g.Tick; Tap(r, "scout", "officer-1"); // a third tap, long after: fires at once
                    r.Until(() => ShotTicks(r, "scout", p).Count > s2n, Game.T(3));
                    var tk = ShotTicks(r, "scout", p);
                    r.Is(tk.Count > s2n && tk[s2n] - p3 <= 1, $"the third tap, the gun long ready, fires at once ({(tk.Count > s2n ? tk[s2n] - p3 : -1)} ticks)");
                    r.Eq(g.Log.Count(x => x.Code == "fire.trigger"), 1, "one press on a target is one fresh aim (fire.trigger once)");
                });
            }

            AddRange("F11-BUF", "today", "lane", "§11.4: a press during the cooldown is never dropped and fires exactly once, at the ready tick", r =>
            {
                var g = r.G; F11Scout(r);
                int p = g.Tick; Tap(r, "scout", "officer-1");
                r.Until(() => ShotTicks(r, "scout", p).Count >= 1, Game.T(3));
                int s1 = ShotTicks(r, "scout", p)[0]; var en = r.Op("scout").Gun;
                r.To(s1 + 3); Tap(r, "scout", "officer-1");
                r.Wait(2); Tap(r, "scout", "officer-1"); // a second press in the same cooldown
                r.Wait(Game.T(5));
                var t = ShotTicks(r, "scout", p);
                r.Is(t.Count >= 2 && t[1] == s1 + en.W!.Every, $"the buffered press fires at the ready tick (s1 + {en.W!.Every}: {(t.Count > 1 ? t[1] - s1 : -1)})");
                r.Eq(t.Count, 2, "and exactly once: two presses in one cooldown buy one shot");
            });

            AddRange("F11-LOCK", "today", "lane", "§11.2 rule 3: follow on, the press locks those in the ring with a line; the lock outlives the release and never walks", r =>
            {
                var g = r.G; F11Near(r);
                r.Is(FollowSetting(r, true).ok, "the follow setting on (logged)");
                int p = g.Tick; Trig(r, "enforcer", "officer-1", true);
                r.Eq(g.LockOf(OpId.Scout), "officer-1", "the Scout (in the ring) is locked at the press");
                r.Eq(g.LockOf(OpId.Fixer), "", "the Fixer (8.1 m) is not");
                r.Is(r.Op("scout").CloseSpent, "the lock's walk-in is spent");
                r.Is(g.Log.Exists(x => x.Code == "fire.lock" && x.Tick == p), "logged fire.lock");
                r.Eq(g.FireStateOf(OpId.Scout).Mode, "lock", "the host reads lock");
                r.Eq(g.FireStateOf(OpId.Scout).Lead, "enforcer", "taken with the Enforcer");
                g.Step(); Trig(r, "enforcer", "", false);
                r.Eq(g.FollowersOf(OpId.Enforcer).Contains(OpId.Scout), false, "locked, the Scout is engaged: no longer a follower");
                r.Wait(Game.T(2));
                r.Is(FirstShot(r, "scout", "officer-1", p) > 0, "the lock fires after the release (a lasting fire order)");
                // the lock never walks: moved out of reach, it holds lineLapse and lapses where the move ended
                var far = r.G.S.Grid.NearestFree(new Pt(r.Op("scout").Pos.X + 1400, r.Op("scout").Pos.Y)) ?? r.Op("scout").Pos;
                r.Is(Move(r, "scout", far).ok, "the Scout walks off, out of its 16 m");
                r.Until(() => r.Op("scout").Walk == null, Game.T(20)); var stop = r.Op("scout").Pos;
                r.Wait(Game.T(g.R.LineLapseS) + 2);
                r.Eq(g.LockOf(OpId.Scout), "", "out of reach it lapses after line-lapse-s");
                r.Eq(r.Op("scout").Pos, stop, "and it never walked back in");
            });

            AddRange("F11-REPLAY", "today", "lane", "§11.5: a hold, tap, release and follow-setting session replays hash-identical from its start and its orders", r =>
            {
                r.Wait(1); // the outer run plays a tick, so its own replay (to its last tick) is a run too
                var b = PlayRangeBody(r.Inputs!, r.G.Start, rb =>
                {
                    var g = rb.G; F8Setup(rb, 4, 1400); TakeControl(rb, "enforcer");
                    Trig(rb, "enforcer", "officer-1", true); rb.Wait(Game.T(2)); Trig(rb, "enforcer", "", false); rb.Wait(5);
                    for (int k = 0; k < 6; k++) { Trig(rb, "enforcer", "officer-2", true); g.Step(); Trig(rb, "enforcer", "", false); rb.Wait(4); }
                    FollowSetting(rb, true); Trig(rb, "enforcer", "officer-3", true); rb.Wait(Game.T(3)); FollowSetting(rb, false);
                    Trig(rb, "enforcer", "officer-4", true); rb.Wait(Game.T(3)); Trig(rb, "enforcer", "", false); rb.Wait(Game.T(3));
                    g.Issue(g.DriveRelease(OpId.Enforcer)); rb.Wait(Game.T(3)); g.Finish();
                });
                var rep = Engine.Replay(r.Inputs!, b.G);
                r.Is(b.G.Orders.Count(o => o.order.Do == "trigger") >= 16 && b.G.Orders.Exists(o => o.order.Do == "follow"), "the session logs its triggers and the follow setting");
                r.Is(b.G.Log.Exists(x => x.Code == "fire.lock") && b.G.Log.Exists(x => x.Code == "fire.follow"), "followers and locks were derived");
                r.Eq(rep.Checkpoints.Count, b.G.Checkpoints.Count, "the replay has as many checkpoints");
                r.Is(rep.Checkpoints.SequenceEqual(b.G.Checkpoints), "and every one is hash-identical");
                r.Eq(rep.Result, b.G.Result, "with the same result");
            });

            // ---------------------------------------------------------------- §12.2: the police answer everyone shooting them
            AddRange("F12-ROT", "today", "lane", "§12.2: three shooters 15.3 m from one officer (past its 15 m effective ring, inside its 16 m max), firing in turn: it keeps its target and fires on cadence", r =>
            {
                var g = r.G; var h = g.RangeSlot("near", 1); var ops = new[] { "scout", "fixer", "tech" };
                int t = r.SendTo("enforcer", Ray(r, h, 0, 3500)); // the Enforcer out of everyone's way, holstered
                for (int k = 0; k < 3; k++) t = Math.Max(t, r.SendTo(ops[k], Ray(r, h, new[] { -6.0, 0.0, 6.0 }[k], 1530)));
                r.To(t); foreach (var op in ops) r.Do(op, "draw"); r.Wait(Game.T(2));
                foreach (var op in ops) { long d = G.Isqrt(G.D2(r.Op(op).Pos, h)); r.Is(d > 1500 && d <= 1600, $"{op} stands {d / 100.0:0.00} m from the officer's stand"); }
                Spawned(r, "police", "near", 1); var off = H(r, "officer-1"); var at0 = off.Pos(g.Tick);
                int t0 = g.Tick, end = t0 + Game.T(12), turn = 0; var seen = new List<string>();
                while (g.Tick < end && !off.Down && ops.All(o => !r.Op(o).Down))
                {
                    string now = ops[turn % 3];
                    if (r.Op(now).Gun.Order.Length == 0) { foreach (var o in ops) if (o != now && r.Op(o).Gun.Order.Length > 0) g.Issue(new Order { Tick = g.Tick, Op = o, Do = "cease" }); FireOn(r, now, "officer-1"); }
                    int before = ShotTicks(r, now, t0).Count; g.Step();
                    if (ShotTicks(r, now, t0).Count > before) turn++; // its shot fired: the next one's turn
                    if (off.Gun.Target.Length > 0) seen.Add(off.Gun.Target);
                }
                var shots = ShotTicks(r, "officer-1", t0); var cell = ops.Sum(o => ShotTicks(r, o, t0).Count);
                r.Is(cell >= 6, $"the cell fired in turn ({cell} shots)");
                r.Is(shots.Count >= 4, $"the officer answers ({shots.Count} shots in {(g.Tick - t0) / 10.0:0.0} s)");
                int flips = 0; for (int k = 1; k < seen.Count; k++) if (seen[k] != seen[k - 1]) flips++;
                r.Is(seen.Count > 0 && ops.Contains(seen[0]), $"it takes one of the three ({(seen.Count > 0 ? seen[0] : "none")})");
                r.Eq(flips, 0, $"and keeps that target while that shooter keeps firing");
                r.Is(shots.Count >= 2 && Enumerable.Range(1, shots.Count - 1).All(k => shots[k] - shots[k - 1] == off.Gun.W!.Every), $"on cadence ({string.Join(",", Enumerable.Range(1, Math.Max(0, shots.Count - 1)).Select(k => shots[k] - shots[k - 1]))} ticks)");
                r.Eq(off.Pos(g.Tick), at0, "and answers from where it stands (no cover walk while it can answer)");
            });
            AddRange("F12-NONE", "today", "lane", "§12.2: every shooter beyond the officer's 16 m max: it can answer none, so rule 3 (cover, then close) as built", r =>
            {
                var g = r.G; var h = g.RangeSlot("near", 1);
                int t = r.SendTo("enforcer", Ray(r, h, 0, 2000)); foreach (var o in new[] { "scout", "fixer", "tech" }) t = Math.Max(t, r.SendTo(o, Ray(r, h, 0, 3500)));
                r.To(t); r.Do("enforcer", "draw"); r.Wait(Game.T(2));
                Spawned(r, "police", "near", 1); var off = H(r, "officer-1"); var at0 = off.Pos(g.Tick); int t0 = g.Tick;
                r.Is(FireOn(r, "enforcer", "officer-1").ok, "the Enforcer fires from 20 m (carbine)");
                bool moved = false, shotFar = false;
                for (int k = 0; k < Game.T(10) && !off.Down && !r.Op("enforcer").Down; k++)
                {
                    int n = ShotTicks(r, "officer-1", t0).Count; g.Step();
                    if (ShotTicks(r, "officer-1", t0).Count > n && !G.Within(off.Pos(g.Tick), r.Op("enforcer").Pos, off.Gun.W!.MaxCm)) shotFar = true;
                    if (off.Pos(g.Tick) != at0) moved = true;
                }
                r.Is(!shotFar, "it never fires beyond its max ring");
                r.Is(moved, "it leaves its stand (cover or the close-in), as built");
            });

            // ---------------------------------------------------------------- Motive's §11.3 numbers (lines)
            AddRange("F11-FOCUS", "today", "lane", "§11.3: four drawn agents at 14 m against four trained officers: focus (the trigger and three locks on one) against split (one each)", r =>
            {
                r.Wait(1); // the outer run plays a tick, so its own replay (to its last tick) is a run too
                var res = new Dictionary<string, (int clear, int down, int taken)>();
                foreach (var pol in new[] { "focus", "split", "focus-follow", "follow-off" })
                {
                    var b = PlayRangeBody(r.Inputs!, r.G.Start, rb => F11Fight(rb, 4, 1400, pol));
                    res[pol] = F11Result(b);
                    r.Is(true, $"{pol}: clear {(res[pol].clear < 0 ? "no" : (res[pol].clear / 10.0).ToString("0.0") + " s")}, {res[pol].down} down, {res[pol].taken} hits taken; joins {b.G.Log.Count(x => x.Code == "fire.follow")}, leaves {b.G.Log.Count(x => x.Code == "fire.unfollow")}, locks {b.G.Log.Count(x => x.Code == "fire.lock")}");
                }
                r.Line(res["focus"].clear == 56 && res["focus"].down == 0, $"focus 5.6 s, none down (Motive; {res["focus"].clear / 10.0:0.0} s, {res["focus"].down} down)");
                r.Line(res["split"].clear == 71 && res["split"].down == 1, $"split 7.1 s, one down (Motive; {res["split"].clear / 10.0:0.0} s, {res["split"].down} down)");
                r.Line(res["focus"].clear > 0 && res["focus"].clear < res["split"].clear && res["focus"].down < res["split"].down, "focus beats split in today");
            });

            AddRange("F11-RUSH", "today", "lane", "§11.3: the rush on a lone officer at 14 m: one press with follow on locks the three", r =>
            {
                r.Wait(1); // the outer run plays a tick, so its own replay (to its last tick) is a run too
                var b = PlayRangeBody(r.Inputs!, r.G.Start, rb => F11Fight(rb, 1, 1400, "rush"));
                var (clear, down, taken) = F11Result(b);
                r.Is(b.G.Log.Count(x => x.Code == "fire.lock") == 3, "one press locks the three");
                r.Line(clear == 11 && taken == 1, $"down in 1.1 s for one hit taken (Motive; {clear / 10.0:0.0} s, {taken} taken, {down} down)");
            });

            AddRange("F11-SPAM", "today", "lane", "§11.2, §11.4: tap spam never beats the hold; the target-switch spam measured", r =>
            {
                r.Wait(1); // the outer run plays a tick, so its own replay (to its last tick) is a run too
                var hold = PlayRangeBody(r.Inputs!, r.G.Start, rb => F11Solo(rb, "hold"));
                var spam = PlayRangeBody(r.Inputs!, r.G.Start, rb => F11Solo(rb, "spam"));
                var pace = PlayRangeBody(r.Inputs!, r.G.Start, rb => F11Solo(rb, "cadence"));
                var sw = PlayRangeBody(r.Inputs!, r.G.Start, rb => F11Solo(rb, "switch"));
                (int shots, int hits) Of(Run x) { var s = x.Shots().Where(q => q.by == "scout").ToList(); return (s.Count, s.Count(q => q.hit)); }
                var (hS, hH) = Of(hold); var (sS, sH) = Of(spam); var (cS, cH) = Of(pace); var (wS, wH) = Of(sw);
                r.Is(true, $"up to 15 s on two officers at 14 m, the Scout alone (untrained pistol), until it is down: hold {hH}/{hS}, spam {sH}/{sS}, a tap a cadence {cH}/{cS}, switch spam {wH}/{wS} (hits/shots)");
                r.Is(sS <= hS && sH <= hH, "tap spam fires no more shots and lands no more hits than the hold");
                r.Is(cS <= hS && cH <= hH, "nor does a tap a cadence");
                r.Is(true, $"switch spam over the Scout's few shots before it is down: {wH}/{wS} against the hold's {hH}/{hS} (too short to read; the 30 s rows below)");
                // tuning.md §13.1 (engine 0.18.0): the count is the pair's in today too, so a switch resumes the count it left on that officer.
                // Measured over 30 s with the Scout kept up (its health topped up each tick: a harness, not a rule), at least 8 shots each.
                var lh = PlayRangeBody(r.Inputs!, r.G.Start, rb => F11Solo(rb, "hold", "scout", 30, true));
                var lw = PlayRangeBody(r.Inputs!, r.G.Start, rb => F11Solo(rb, "switch", "scout", 30, true));
                var (lhS, lhH) = Of(lh); var (lwS, lwH) = Of(lw);
                r.Is(lhS >= 8 && lwS >= 8, $"30 s, the Scout kept up: the hold {lhH}/{lhS}, switch spam {lwH}/{lwS} (hits/shots; at least 8 shots each)");
                r.Is(lwH <= lhH + 1, $"switching targets every shot hits at most one more than the hold (the 500 head start once per officer, never once per switch): {lwH} against {lhH}");
                var eh = PlayRangeBody(r.Inputs!, r.G.Start, rb => F11Solo(rb, "hold", "enforcer")); var ew = PlayRangeBody(r.Inputs!, r.G.Start, rb => F11Solo(rb, "switch", "enforcer"));
                var ehs = eh.Shots().Where(q => q.by == "enforcer").ToList(); var ews = ew.Shots().Where(q => q.by == "enforcer").ToList();
                r.Line(ews.Count(q => q.hit) * ehs.Count <= ehs.Count(q => q.hit) * ews.Count, $"the Enforcer (trained carbine) too: switch {ews.Count(q => q.hit)}/{ews.Count} against hold {ehs.Count(q => q.hit)}/{ehs.Count}, both officers down in {F11Clear(ew)} against {F11Clear(eh)} ticks");
            });

            // ---------------------------------------------------------------- §13 (engine 0.18.0)
            AddRange("F13-HOLD", "today", "lane", "§13.2: a holding agent in the ring neither follows (follow off) nor is locked by a press (follow on); set to return, it is again", r =>
            {
                var g = r.G; F11Near(r);
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "stance", Stance = "hold" }).ok, "the Scout (7.9 m, drawn) holds");
                int p = g.Tick; Trig(r, "enforcer", "officer-1", true); r.Wait(1);
                r.Eq(g.FollowersOf(OpId.Enforcer).Contains(OpId.Scout), false, "follow off: holding, the Scout is no follower");
                r.Eq(r.Op("scout").Gun.FollowBy, "", "and never joins");
                Trig(r, "enforcer", "", false); r.Wait(2);
                r.Is(FollowSetting(r, true).ok, "the follow setting on");
                Trig(r, "enforcer", "officer-1", true);
                r.Eq(g.LockOf(OpId.Scout), "", "follow on: the press locks nothing on a holding agent");
                r.Eq(ShotTicks(r, "scout", p).Count, 0, "holding, the Scout fired nothing");
                Trig(r, "enforcer", "", false); r.Wait(1);
                r.Is(g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "stance", Stance = "return" }).ok, "the Scout to return");
                r.Is(!H(r, "officer-1").Down && r.Op("scout").Gun.ShotBy.Length == 0, "officer-1 still up, and nobody has fired on the Scout");
                Trig(r, "enforcer", "officer-1", true);
                r.Eq(g.LockOf(OpId.Scout), "officer-1", "returning fire with nobody on it, the press locks it");
            });

            AddRange("F13-RELOCK", "today", "lane", "a lock whose target falls ends: a press the tick after locks that agent on the next target, and so does a press in the tick it falls", r =>
            {
                r.Wait(1); // the outer run plays a tick, so its own replay (to its last tick) is a run too
                int fell = -1;
                var a = PlayRangeBody(r.Inputs!, r.G.Start, rb => fell = F13Relock(rb, -1));
                r.Is(fell > 0, $"officer-1 falls to the Scout's lock (tick {fell})");
                r.Eq(a.G.LockOf(OpId.Scout), "officer-2", "pressed the tick after it falls, the Scout locks officer-2");
                int fell2 = -1;
                var b = PlayRangeBody(r.Inputs!, r.G.Start, rb => fell2 = F13Relock(rb, fell - 1));
                r.Eq(fell2, fell, "pressed one tick earlier, officer-1 falls in the tick after that press, as before");
                r.Eq(b.G.LockOf(OpId.Scout), "officer-2", "pressed in the tick its lock's target falls, the Scout locks officer-2 too");
            });
        }

        /// <summary>F13-RELOCK: follow on; the Scout (in the ring) holds a lock on officer-1 (a fire order) while the driven Enforcer's trigger is
        /// held on officer-2 (kept up: a harness, so the Enforcer's fire never changes who drops officer-1). The press again on officer-2 (a tap:
        /// release and press) comes at tick `pressAt`, before that tick's fire, or (-1) on the tick after officer-1 falls. Returns the tick
        /// officer-1 fell; plays 3 ticks on.</summary>
        static int F13Relock(Run r, int pressAt)
        {
            var g = r.G; F11Near(r, false, 2); FollowSetting(r, true);
            Trig(r, "enforcer", "officer-2", true);
            r.Is(g.LockOf(OpId.Scout) == "officer-2", "the press locks the Scout on officer-2");
            FireOn(r, "scout", "officer-1");
            r.Is(g.LockOf(OpId.Scout) == "officer-1", "then a fire order locks it on officer-1");
            int end = g.Tick + Game.T(30), fell = -1; bool pressed = false;
            void Press() { Trig(r, "enforcer", "", false); Trig(r, "enforcer", "officer-2", true); pressed = true; }
            while (g.Tick < end && !r.Op("scout").Down)
            {
                H(r, "officer-2").Gun.Health = 999;
                if (!pressed && g.Tick == pressAt) Press();
                g.Step();
                if (H(r, "officer-1").Down) { fell = g.Tick; break; }
            }
            if (fell > 0 && !pressed) Press();
            r.Wait(3);
            return fell;
        }

        static int F11Clear(Run b) { var g = b.G; int t0 = g.Log.First(x => x.Code == "fire.trigger").Tick; return g.Hostiles.Count > 0 && g.Hostiles.All(h => h.Down) ? g.Hostiles.Max(h => b.Shots().Where(s => s.target == h.Id && s.hit).Select(s => s.tick).DefaultIfEmpty(-1).Max()) - t0 : -1; }
        static (int clear, int down, int taken) F11Result(Run b)
        {
            var g = b.G; int t0 = g.Log.First(x => x.Code == "fire.trigger").Tick;
            int clear = g.Hostiles.Count > 0 && g.Hostiles.All(h => h.Down) ? g.Hostiles.Max(h => b.Shots().Where(s => s.target == h.Id && s.hit).Select(s => s.tick).DefaultIfEmpty(-1).Max()) - t0 : -1;
            return (clear, DownOf(b), b.Shots().Count(s => s.hit && Cell4R.Contains(s.target)));
        }

        /// <summary>Motive's §11.3 policies with the real inputs: the Enforcer driven on the trigger; focus = its target and three locks on the
        /// lowest-id officer up; split = one each, then the nearest; focus-follow = one press per target with follow on; follow-off = the
        /// trigger held, the rest follow; rush = one press with follow on.</summary>
        static void F11Fight(Run r, int n, int cm, string pol)
        {
            var g = r.G; F8Setup(r, n, cm, "near"); TakeControl(r, "enforcer");
            if (pol == "focus-follow" || pol == "rush") FollowSetting(r, true);
            int end = g.Tick + Game.T(40); bool first = true;
            string Lead() => g.Hostiles.Where(h => !h.Down).OrderBy(h => h.Id, StringComparer.Ordinal).Select(h => h.Id).FirstOrDefault() ?? "";
            string Nearest(Op o) => g.Hostiles.Where(h => !h.Down).OrderBy(h => G.D2(h.Pos(g.Tick), o.Pos)).Select(h => h.Id).FirstOrDefault() ?? "";
            while (g.Tick < end && g.Hostiles.Any(h => !h.Down) && Cell4R.Any(op => !r.Op(op).Down))
            {
                var en = r.Op("enforcer");
                if (!en.Down)
                {
                    string want = pol == "split" ? (first ? "officer-1" : (en.Gun.TrigT.Length > 0 && !H(r, en.Gun.TrigT).Down ? en.Gun.TrigT : Nearest(en))) : Lead();
                    if (want.Length > 0 && (en.Gun.TrigT != want || !en.Gun.Trig)) Trig(r, "enforcer", want, true);
                }
                if (pol == "focus" || pol == "split")
                    for (int k = 1; k < 4; k++)
                    {
                        var o = r.Op(Cell4R[k]); if (o.Down) continue;
                        string want = pol == "focus" ? Lead() : first ? g.Hostiles.Where(h => !h.Down).OrderBy(h => h.Id, StringComparer.Ordinal).Skip(k).Select(h => h.Id).FirstOrDefault() ?? Lead() : o.Gun.Order.Length > 0 ? o.Gun.Order : Nearest(o);
                        if (want.Length > 0 && o.Gun.Order != want) FireOn(r, Cell4R[k], want);
                    }
                first = false;
                g.Step();
            }
        }

        /// <summary>One agent alone (the Scout), driven, against two officers at 14 m for 6 s: hold, spam (a tap every tick), a tap a cadence, or
        /// switch (a tap on the other officer every tick: the reset through a change of target).</summary>
        static void F11Solo(Run r, string pol, string op = "scout", int seconds = 15, bool keepUp = false)
        {
            var g = r.G; var h = g.RangeSlot("near", 1);
            r.To(r.SendTo(op, Ray(r, h, 0, 1400))); r.Do(op, "draw"); r.Wait(Game.T(2));
            Spawned(r, "police", "near", 2); TakeControl(r, op);
            int end = g.Tick + Game.T(seconds), k = 0, fired = 0;
            while (g.Tick < end && !r.Op(op).Down && g.Hostiles.Any(x => !x.Down))
            {
                if (keepUp) r.Op(op).Gun.Health = 999; // a measuring harness (Motive's S13-SPAM): the shooter outlives enough shots to count
                string a = H(r, "officer-1").Down ? "officer-2" : "officer-1", b = H(r, "officer-2").Down ? a : "officer-2";
                switch (pol)
                {
                    case "hold": if (!r.Op(op).Gun.Trig || r.Op(op).Gun.TrigT != a) Trig(r, op, a, true); break;
                    case "spam": if (k % 2 == 0) Trig(r, op, a, true); else Trig(r, op, "", false); break;
                    case "cadence": if (k % 15 == 0) Trig(r, op, a, true); else if (k % 15 == 1) Trig(r, op, "", false); break;
                    case "switch": { var en = r.Op(op).Gun; int n = ShotTicks(r, op, 0).Count; if (n > fired || en.TrigT.Length == 0) Trig(r, op, en.TrigT == a ? b : a, true); fired = n; break; } // the tick after each shot: no cadence lost
                }
                k++; g.Step();
            }
        }
    }
}
