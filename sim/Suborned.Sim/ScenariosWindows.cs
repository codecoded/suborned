// The window marker's one read (brief 043 §6, engine 0.24.0): Game.Windows() lists the cell's open windows (kind, thing, until-tick or -1
// held) so the host reads one list instead of GuardAwayUntil, BlockedUntil, PatrolAwayUntil, RivalsDelayedUntil and LoopOn + LoopCam; and the
// rerouted patrol's return is a log code (patrol.returns), the window's closing pop, as guard.back, car.clears and loop.ends are for theirs.
// Each scenario also runs twice and replays from its orders (the Theory tests).
using System;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static string WindowsNow(Game g) => string.Join(" ", g.Windows().Select(w => w.ToString()));
        static OpenWindow? WindowOf(Game g, string kind, string thing) => g.Windows().FirstOrDefault(w => w.Kind == kind && w.Thing == thing);

        static void WindowCases()
        {
            Add("N181", "—", "Windows() (brief 043 §6, engine 0.24.0): nothing is open at the start. The Fixer calls Inez: call:guard opens until GuardAwayUntil and closes on guard.back. He calls the driver: call:pickup opens on car.blocks, until BlockedUntil, and closes on car.clears. The Tech loops the entrance camera (loop:entrance, held: -1) and reroutes the patrol: reroute:patrol until PatrolAwayUntil, the loop still listed; at that tick the log says patrol.returns, once, and the window is gone. The Tech walks off and the loop closes on loop.ends. A false tip on the rivals: mislead:rivals until RivalsDelayedUntil. The list always matches the five fields it reads, tick by tick.", r =>
            {
                var g = r.G;
                // the list against the fields, every tick from here on (the oracle is the field read the host does today)
                bool same = true; string firstOff = "";
                void Check()
                {
                    var w = g.Windows();
                    bool guardOpen = g.LureTick >= 0 && !g.GuardDown && !g.GuardHeld && g.Tick < g.GuardAwayUntil;
                    bool ok = (WindowOf(g, "call", "guard") is OpenWindow cg ? guardOpen && cg.Until == g.GuardAwayUntil : !guardOpen)
                        && (WindowOf(g, "call", "pickup") != null) == (g.Tick >= g.BlockedFrom && !g.PickupCameraLive())
                        && (WindowOf(g, "reroute", "patrol") is OpenWindow rp ? rp.Until == g.PatrolAwayUntil && rp.Until > g.Tick : g.PatrolAwayUntil <= g.Tick)
                        && (WindowOf(g, "loop", g.LoopCam) is OpenWindow lw ? g.LoopOn && lw.Until == -1 : !g.LoopOn)
                        && w.All(x => x.Until == -1 || x.Until > g.Tick);
                    if (!ok && same) { same = false; firstOff = $"{Report.Clock(g.Tick)}: {WindowsNow(g)}"; }
                }
                void Go(int tick) { while (g.Tick < tick && !g.Over) { g.Step(); Check(); } }
                r.Eq(g.Windows().Count, 0, "nothing is open at the start");

                // Call Inez
                r.To(T(20)); r.Do(Fixer, "call", Inez); Go(r.Op(Fixer).BusyEnd);
                var cg = WindowOf(g, "call", "guard");
                r.Is(cg != null && cg.Until == g.GuardAwayUntil && cg.Until > g.Tick, $"the call lands: call:guard until {Report.Clock(g.GuardAwayUntil)} ({WindowsNow(g)})");
                r.Is(g.Log.Exists(l => l.Code == "call.waitress" && l.Tick == g.Tick), "on call.waitress");

                // Call the driver, early: the car's hold ends before driverLast, so car.clears closes it
                r.Do(Fixer, "call", Driver); Go(r.Op(Fixer).BusyEnd);
                r.Is(WindowOf(g, "call", "pickup") == null, "called, the car is still on its way: the camera is not blind yet");
                Go(g.BlockedFrom);
                var cp = WindowOf(g, "call", "pickup");
                r.Is(cp != null && cp.Until == g.BlockedUntil && g.BlockedUntil < T(g.S.DriverLast), $"car.blocks: call:pickup until {Report.Clock(g.BlockedUntil)} ({WindowsNow(g)})");
                r.Is(g.Log.Exists(l => l.Code == "car.blocks" && l.Tick == g.Tick), "on car.blocks");

                // the Tech: the loop, then the reroute
                Go(r.Send(Tech, "cabinet", run: true));
                r.Do(Tech, "loop", "entrance"); Go(r.Op(Tech).BusyEnd);
                var lw = WindowOf(g, "loop", "entrance");
                r.Is(lw != null && lw.Until == -1, $"loop.on: loop:entrance, held (-1) ({WindowsNow(g)})");
                r.Do(Tech, "reroute", "patrol"); Go(r.Op(Tech).BusyEnd);
                int away = g.PatrolAwayUntil;
                var rp = WindowOf(g, "reroute", "patrol");
                r.Is(rp != null && rp.Until == away && away == g.Tick + T(g.R.RerouteAwayS), $"patrol.rerouted: reroute:patrol until {Report.Clock(away)}, {g.R.RerouteAwayS} s ({WindowsNow(g)})");
                r.Is(WindowOf(g, "loop", "entrance") != null, "the loop is still listed");

                // the closings: guard.back and car.clears on their ticks
                int guardBack = g.GuardAwayUntil, clears = g.BlockedUntil;
                Go(Math.Max(guardBack, clears));
                r.Is(g.Log.Exists(l => l.Code == "guard.back" && l.Tick == guardBack) && WindowOf(g, "call", "guard") == null, $"guard.back at {Report.Clock(guardBack)}: call:guard gone");
                r.Is(g.Log.Exists(l => l.Code == "car.clears" && l.Tick == clears) && WindowOf(g, "call", "pickup") == null, $"car.clears at {Report.Clock(clears)}: call:pickup gone");

                // the patrol returns: one log line on the tick its false orders run out
                Go(away - 1);
                r.Is(WindowOf(g, "reroute", "patrol") != null && !g.Log.Exists(l => l.Code == "patrol.returns"), "a tick before: still away, no patrol.returns");
                Go(away);
                r.Is(g.Log.Count(l => l.Code == "patrol.returns") == 1 && g.Log.Exists(l => l.Code == "patrol.returns" && l.Tick == away), $"patrol.returns at {Report.Clock(away)}, once");
                r.Is(WindowOf(g, "reroute", "patrol") == null && g.ConeFan("patrol")!.Live, "the window is gone and the patrol sweeps again");
                Go(away + T(5));
                r.Eq(g.Log.Count(l => l.Code == "patrol.returns"), 1, "said once");

                // the loop ends when the Tech walks off
                int leaves = g.Tick; r.SendTo(Tech, r.Op(Tech).Pos + new Pt(0, g.R.TetherCm + 600)); // a move lets the loop go (cancel, loop.ends)
                Go(g.Tick + T(10));
                r.Is(!g.LoopOn && g.Log.Exists(l => l.Code == "loop.ends" && l.Tick >= leaves) && WindowOf(g, "loop", "entrance") == null, "the Tech walks off, loop.ends: the loop window is gone");

                // a false tip
                Go(r.SendTo(Scout, new Pt(8250, -3100)));
                r.Is(r.Until(() => { g.Step(); Check(); return g.RivalsPresent; }, T(400)), $"the rivals arrive ({Report.Clock(g.Tick)})");
                r.Do(Scout, "mislead", "rivals"); Go(r.Op(Scout).BusyEnd);
                var mr = WindowOf(g, "mislead", "rivals");
                r.Is(mr != null && mr.Until == g.RivalsDelayedUntil && mr.Until > g.Tick, $"rivals.misled: mislead:rivals until {Report.Clock(g.RivalsDelayedUntil)} ({WindowsNow(g)})");
                r.Is(same, "tick by tick the list matched the fields" + (same ? "" : " (first off at " + firstOff + ")"));
                r.Is(!g.Over, "the mission still runs");
            }, Turned(Inez, Driver));

            Add("N182", "—", "patrol.returns is the reroute's only (engine 0.24.0): a run with no reroute never logs it, and its Windows() never lists a reroute.", r =>
            {
                var g = r.G; bool listed = false;
                while (g.Tick < T(300) && !g.Over) { g.Step(); if (WindowOf(g, "reroute", "patrol") != null) listed = true; }
                r.Is(!listed && !g.Log.Exists(l => l.Code == "patrol.returns"), "no reroute: no window, no patrol.returns");
            });
        }
    }
}
