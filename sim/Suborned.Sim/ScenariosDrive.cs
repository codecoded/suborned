// The driven agent (brief 025 playground, EXPERIMENTAL; rulebook cell.drive; engine 0.9.0). Each scenario drives an operative by drive samples
// as a host would (one a tick, where its controller has the agent), and checks what the engine keeps. The suite replays every one of them
// from its start state and order log (the samples are orders), so "the samples replay exactly" is checked for each.
//  D01 a straight walk: samples a walk's pace apart are taken whole; the agent is where the host put it; nothing corrected
//  D02 the pace budget: samples a metre ahead each tick move the agent at its walk (two ticks' burst at most, plus slack), flagged corrected
//  D03 a teleport: a sample 50 m away moves the agent one burst, no more
//  D04 a wall: pushed diagonally into a straight wall, the agent never enters a blocked cell and slides along it
//  D05 a corner: two blocked cells meeting at a corner: a diagonal push never passes between them
//  D06 one run, one notice: a run zig-zagging in sight of the guard is one span (run.seen once, Alert 1); after a walk sample a new span
//  D07 the tether and the loop: the Tech holding the loop asks to run and walks (a run is refused while tethered); driven 2 m off, the loop ends
//  D08 walking Vale: the Enforcer with Vale asks to run and walks at his walk; Vale stays at his side
//  D09 a busy agent: pushing the Scout mid-Watch past busyPush cancels the Watch at its cost (lost); under onBusy hold she stands until it lands
//  D10 release and the click: control ends with a click move; released, the agent stands (onRelease stand) or walks on (resume)
//  D11 the gap: driven straight at the guard, the agent stops short of 40 cm and slides past; with the playground's gap 0 it walks through
//  D12 unknown = reject: a gait the rulebook doesn't name, a facing out of range, release or face on another verb are format errors; a sample
//      with no position, in the car, or down is refused; a playground pace doubles every cap
using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public static partial class Scenarios
    {
        static bool Sample(Run r, string op, Pt at, bool run = false, int face = 0)
        {
            var g = r.G; var a = g.Issue(g.DriveSample(Sim.Op.Parse(op), at, run, face));
            return a.ok;
        }
        /// <summary>Drive `ticks` ticks: each tick a sample `v` cm on from where the engine has the agent (a host whose controller is pulled back).</summary>
        static int Push(Run r, string op, Pt v, int ticks, bool run = false)
        {
            int taken = 0;
            for (int k = 0; k < ticks && !r.G.Over; k++) { var p = r.Op(op).Pos; if (Sample(r, op, new Pt(p.X + v.X, p.Y + v.Y), run, Heading(v))) taken++; r.G.Step(); }
            return taken;
        }
        static int Heading(Pt v) => (int)((Math.Atan2(v.Y, v.X) * 180 / Math.PI + 360) % 360); // a scenario's facing only (never read by a rule)
        static bool OnFree(Run r, string op) => r.G.S.Grid.Free(r.Op(op).Pos);
        /// <summary>The first free cell (ring search from `near`) whose +x neighbour is blocked along three rows, with free ground behind and beside it.</summary>
        static Pt? WallEast(Grid gr, Pt near)
        {
            int ci = gr.CI(near.X), cj = gr.CJ(near.Y);
            for (int rr = 0; rr < 120; rr++)
                for (int i = ci - rr; i <= ci + rr; i++) for (int j = cj - rr; j <= cj + rr; j++)
                {
                    if (Math.Max(Math.Abs(i - ci), Math.Abs(j - cj)) != rr) continue;
                    bool ok = true;
                    for (int k = -1; k <= 6 && ok; k++) ok = gr.FreeCell(i, j + k) && gr.FreeCell(i - 1, j + k) && !gr.FreeCell(i + 1, j + k);
                    if (ok) return gr.Centre(i, j);
                }
            return null;
        }
        /// <summary>A free cell whose +x and +y neighbours are blocked and whose diagonal +x+y is free: a corner a diagonal step could slip through.</summary>
        static (Pt at, Pt beyond)? Corner(Grid gr, Pt near)
        {
            int ci = gr.CI(near.X), cj = gr.CJ(near.Y);
            for (int rr = 0; rr < 160; rr++)
                for (int i = ci - rr; i <= ci + rr; i++) for (int j = cj - rr; j <= cj + rr; j++)
                {
                    if (Math.Max(Math.Abs(i - ci), Math.Abs(j - cj)) != rr) continue;
                    if (gr.FreeCell(i, j) && !gr.FreeCell(i + 1, j) && !gr.FreeCell(i, j + 1) && gr.FreeCell(i + 1, j + 1) && gr.FreeCell(i - 1, j) && gr.FreeCell(i, j - 1))
                        return (gr.Centre(i, j), gr.Centre(i + 1, j + 1));
                }
            return null;
        }
        static int WalkCm(Run r) => r.G.R.CellSpeed / 10;    // cm a tick at a walk (cell.speed, mm a tick)
        static int RunCm(Run r) => r.G.R.RunSpeed / 10;

        static void DriveCases()
        {
            Add("D01", "—", "A straight walk (brief 025, EXPERIMENTAL): the Scout driven 5 s along open ground, a walk's pace apart: every sample taken whole, the agent exactly where the host put her, nothing corrected; the samples are orders in the log.", r =>
            {
                var g = r.G; var start = r.Op(Scout).Pos; int v = WalkCm(r);
                int taken = Push(r, Scout, new Pt(0, -v), T(5));
                r.Eq(taken, T(5), "every sample taken");
                r.Eq(r.Op(Scout).Pos, new Pt(start.X, start.Y - v * T(5)), "where the host put her");
                var pose = g.DrivePoseOf(OpId.Scout);
                r.Is(pose.Driven && pose.Corrections == 0 && !pose.Corrected, $"driven, nothing corrected ({pose.Corrections})");
                r.Eq(g.Orders.Count(o => o.order.Do == "drive"), T(5), "the samples are in the order log");
                r.Eq(g.ActivityOf(OpId.Scout).Phase, "driven", "the HUD's activity: driven");
            }, DriveStart());

            Add("D02", "—", "The pace budget: samples a metre ahead of the Scout each tick. She moves at most two ticks' walk plus slack in the first tick and a walk a tick after (cell.drive.burst, slack): 5 s covers a walk's 7 m, not 50, and each sample is flagged corrected.", r =>
            {
                var g = r.G; var start = r.Op(Scout).Pos; int v = WalkCm(r), slack = g.R.Drive!.SlackCm;
                Sample(r, Scout, new Pt(start.X, start.Y - 100)); g.Step();
                int first = start.Y - r.Op(Scout).Pos.Y;
                r.Is(first <= 2 * v + slack && first >= v, $"the first tick: {first} cm (a walk {v}, burst {2 * v}, slack {slack})");
                Push(r, Scout, new Pt(0, -100), T(5) - 1);
                int went = start.Y - r.Op(Scout).Pos.Y;
                r.Is(went <= v * T(5) + v + slack + 1 && went >= v * T(5) - v, $"5 s at a walk: {went} cm (a walk's {v * T(5)}; one tick's burst and the slack at most over it)");
                r.Is(g.DrivePoseOf(OpId.Scout).Corrections >= T(5) - 1, "each sample corrected");
                r.Is(OnFree(r, Scout), "on free ground");
            }, DriveStart());

            Add("D03", "—", "A teleport: one sample 50 m away. The Scout moves one burst (two ticks' walk plus slack), and the host reads where she is.", r =>
            {
                var g = r.G; var start = r.Op(Scout).Pos; int v = WalkCm(r), slack = g.R.Drive!.SlackCm;
                Sample(r, Scout, new Pt(start.X, start.Y - 5000)); g.Step();
                int d = G.Dist(start, r.Op(Scout).Pos);
                r.Is(d > 0 && d <= 2 * v + slack, $"{d} cm, not 50 m");
                var pose = g.DrivePoseOf(OpId.Scout);
                r.Is(pose.Corrected && pose.Pos == r.Op(Scout).Pos, "the host reads the corrected pose");
            }, DriveStart());

            Add("D04", "—", "A wall: the Scout walked (a click) to the nearest straight wall to her east, then driven diagonally into it (east and along) for 4 s. She never stands on a blocked cell or past the wall's face, and slides along it at the along part of her pace.", r =>
            {
                var g = r.G; var gr = g.S.Grid;
                var w = WallEast(gr, r.Op(Scout).Pos);
                r.Is(w.HasValue, "a straight wall near the Scout");
                if (!w.HasValue) return;
                r.To(r.SendTo(Scout, w.Value));
                int face = gr.X0 + (gr.CI(w.Value.X) + 1) * gr.Cell; // the wall's west face (cm)
                int y0 = r.Op(Scout).Pos.Y, v = WalkCm(r), worstX = int.MinValue; bool free = true;
                for (int k = 0; k < T(4); k++)
                {
                    var p = r.Op(Scout).Pos; Sample(r, Scout, new Pt(p.X + v, p.Y + v / 2)); g.Step();
                    worstX = Math.Max(worstX, r.Op(Scout).Pos.X); free &= OnFree(r, Scout);
                }
                r.Is(free && worstX < face, $"never on a blocked cell, never past the face (x at most {worstX.ToString(System.Globalization.CultureInfo.InvariantCulture)}, face {face.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
                r.Is(r.Op(Scout).Pos.Y - y0 >= v / 2 * T(3), $"slides along it: {r.Op(Scout).Pos.Y - y0} cm");
            }, DriveStart());

            Add("D05", "—", "A corner: a free cell whose east and south neighbours are blocked and whose diagonal is free. Driven straight at the diagonal for 2 s, the Scout never passes between the two blocked cells: she stays in her cell or slides out by a free side, never into the diagonal by the corner.", r =>
            {
                var g = r.G; var gr = g.S.Grid;
                var c = Corner(gr, r.Op(Scout).Pos);
                r.Is(c.HasValue, "such a corner on the block");
                if (!c.HasValue) return;
                r.To(r.SendTo(Scout, c.Value.at));
                int ci = gr.CI(c.Value.at.X), cj = gr.CJ(c.Value.at.Y); bool through = false, free = true;
                for (int k = 0; k < T(2); k++)
                {
                    Sample(r, Scout, c.Value.beyond); g.Step();
                    var p = r.Op(Scout).Pos; free &= gr.Free(p);
                    if (gr.CI(p.X) == ci + 1 && gr.CJ(p.Y) == cj + 1) through = true;
                }
                r.Is(free && !through, "never through the corner");
            }, DriveStart());

            Add("D06", "—", "One run, one notice (cell.drive: a run is a span): the Scout clicked to the guard's forecourt, then run zig-zagging in his sight for 4 s with Shift held throughout: one span, so run.seen once (Alert 1) however the heading turns. A walk sample ends the span; running again is a new span, noticed again.", r =>
            {
                var g = r.G; int v = RunCm(r);
                r.To(r.SendTo(Scout, r.Stand("d:Office approach")));
                for (int k = 0; k < T(4); k++) Push(r, Scout, (k / 5) % 2 == 0 ? new Pt(v, v / 3) : new Pt(-v, v / 3), 1, run: true);
                int seen = g.Log.Count(l => l.Code == "run.seen");
                r.Eq((seen, g.Alert), (1, 1), "one span: noticed once, Alert 1");
                r.Is(r.Op(Scout).RunSeen.Count >= 1, $"the people who saw her remember the run once each ({r.Op(Scout).RunSeen.Count})");
                Push(r, Scout, new Pt(0, WalkCm(r)), 3); // a walk: the span ends
                for (int k = 0; k < T(2); k++) Push(r, Scout, new Pt(0, -v), 1, run: true);
                r.Eq(g.Log.Count(l => l.Code == "run.seen"), 2, "a new span is noticed again");
            }, DriveStart());

            Add("D07", "—", "The tether: the Tech runs (a click) to the cabinet and loops the entrance camera. Driven with Shift held she walks (a run is refused while she holds the loop, as a move's run is); 2 m off the cabinet the loop ends (loop.ends).", r =>
            {
                var g = r.G;
                r.To(r.Send(Tech, "cabinet", run: true)); r.Do(Tech, "loop", "entrance"); r.Done(Tech);
                r.Is(g.LoopOn, "the loop holds");
                var from = r.Op(Tech).Pos; var away = g.S.Grid.Route(from, r.Stand("d:Courtyard"))!; var dir = away[1] - away[0];
                int len = Math.Max(1, G.Dist(away[0], away[1])), v = RunCm(r); var step = new Pt(dir.X * v / len, dir.Y * v / len);
                Push(r, Tech, step, 1, run: true);
                int went = G.Dist(from, r.Op(Tech).Pos);
                r.Is(went <= 2 * WalkCm(r) + g.R.Drive!.SlackCm && !r.Op(Tech).Running, $"tethered, she walks: {went} cm, not running");
                for (int k = 0; k < T(5) && g.LoopOn; k++) Push(r, Tech, step, 1, run: true);
                r.Is(!g.LoopOn && g.Log.Exists(l => l.Code == "loop.ends"), $"off the cabinet the loop ends ({G.Dist(r.Op(Tech).Pos, g.S.At("cabinet"))} cm off)");
            }, DriveStart());

            Add("D08", "—", "Walking Vale: route A to the Take; the Enforcer driven with Shift held walks at his walk (nobody runs carrying Vale), and Vale stays at his side.", r =>
            {
                var g = r.G;
                RouteA(r, reroute: true);
                r.Eq(g.Vale, ValeAt.WithEnforcer, "Vale is with the Enforcer");
                var from = r.Op(Enf).Pos; var to = g.S.Grid.Route(from, r.Stand("d:Getaway bay"))!; var dir = to[1] - to[0];
                int len = Math.Max(1, G.Dist(to[0], to[1])), v = RunCm(r);
                Push(r, Enf, new Pt(dir.X * v / len, dir.Y * v / len), T(2), run: true);
                int went = G.Dist(from, r.Op(Enf).Pos), walk = WalkCm(r) * T(2) + WalkCm(r) + g.R.Drive!.SlackCm + 1; // a walk, one tick's burst, slack once
                r.Is(went <= walk && !r.Op(Enf).Running, $"he walks: {went} cm in 2 s (a walk's {WalkCm(r) * T(2)})");
                r.Eq(g.ValePos(), (Pt?)(r.Op(Enf).Pos + new Pt(100, 0)), "Vale at his side");
            });

            Add("D09", "—", "A busy agent (onBusy cancel): the Scout clicked to Inez and set to Watch her, then pushed: until busyPush (25 cm) in all she is pinned; past it the Watch is cancelled at its cost (lost: nothing learned) and she moves.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "watch", Inez);
                Sample(r, Scout, r.Op(Scout).Pos); // take control standing still
                g.Step();
                var at = r.Op(Scout).Pos;
                Push(r, Scout, new Pt(10, 0), 2);
                r.Is(r.Op(Scout).Busy != null && r.Op(Scout).Pos == at, "pushed 20 cm: pinned, still watching");
                Push(r, Scout, new Pt(10, 0), 2);
                r.Is(r.Op(Scout).Busy == null && g.Log.Exists(l => l.Code == "cancel.watch"), "past 25 cm the Watch is cancelled (lost)");
                r.Is(!g.Inez.KRoutine, "nothing learned");
                Push(r, Scout, new Pt(10, 0), 3);
                r.Is(r.Op(Scout).Pos != at, "and she moves");
            }, DriveStart());

            Add("D09b", "—", "A busy agent (onBusy hold, the playground's switch): the same Watch, pushed throughout; she stands until it lands, then moves, and it teaches what a Watch teaches.", r =>
            {
                var g = r.G;
                r.To(r.Send(Scout, "beside-inez")); r.Do(Scout, "watch", Inez);
                var at = r.Op(Scout).Pos; int end = r.Op(Scout).BusyEnd;
                while (g.Tick < end - 1) Push(r, Scout, new Pt(10, 0), 1);
                r.Is(r.Op(Scout).Pos == at && !g.Log.Exists(l => l.Code == "cancel.watch"), "held: pinned, never cancelled");
                Push(r, Scout, new Pt(10, 0), 3);
                r.Is(r.Op(Scout).Busy == null && r.Op(Scout).Pos != at, "the Watch lands, then she moves");
                r.Is(g.Log.Exists(l => l.Code == "scout.watch.watched"), "it landed");
            }, DriveStart(onBusy: "hold"));

            Add("D10", "—", "Release and the click (onRelease stand): the Scout clicked to the Courtyard; control taken mid-walk (the walk ends); released, she stands. Taken again, a click move ends control and she walks.", r =>
            {
                var g = r.G;
                r.Send(Scout, "d:Courtyard"); r.Wait(T(2));
                Push(r, Scout, new Pt(0, WalkCm(r)), 3);
                r.Is(r.Op(Scout).Driven && r.Op(Scout).Walk == null, "control ends the walk");
                g.Issue(g.DriveRelease(OpId.Scout)); var at = r.Op(Scout).Pos; r.Wait(T(2));
                r.Is(!r.Op(Scout).Driven && r.Op(Scout).Pos == at, "released: she stands");
                Push(r, Scout, new Pt(0, WalkCm(r)), 2);
                r.Send(Scout, "d:Courtyard");
                r.Is(!r.Op(Scout).Driven && r.Op(Scout).Walk != null, "a click move takes control back");
            }, DriveStart());

            Add("D10b", "—", "Release, resume (the playground's onRelease resume): control taken mid-walk to the Courtyard and released: she walks on to the Courtyard from where she was left.", r =>
            {
                var g = r.G;
                r.Send(Scout, "d:Courtyard"); r.Wait(T(2));
                Push(r, Scout, new Pt(0, WalkCm(r)), 3);
                g.Issue(g.DriveRelease(OpId.Scout));
                r.Is(!r.Op(Scout).Driven && r.Op(Scout).Walk != null && r.Op(Scout).Walk!.End == r.Stand("d:Courtyard"), "she walks on to the Courtyard");
                r.To(r.Arrival(Scout));
                r.Eq(r.Op(Scout).Pos, r.Stand("d:Courtyard"), "and arrives");
            }, DriveStart(onRelease: "resume"));

            Add("D11", "—", "The gap (cell.drive.gap 40 cm): the Scout clicked to 3 m in front of the guard at his post, then driven straight at him for 3 s: she never ends a tick nearer than 40 cm to him while closing. With the playground's gap 0, the same samples walk her through him.", r => GapCase(r, 40), DriveStart());
            Add("D11b", "—", "The gap off (the playground's gap 0): D11's samples walk the Scout to the guard's own spot.", r => GapCase(r, 0), DriveStart(gap: 0));

            Add("D12", "—", "Unknown = reject: a gait the rulebook doesn't name (crouch), a facing of 400, release or face on a move are format errors; a sample with no position is refused (no-place), and in the car (in-car); a playground pace of 2000 per mille doubles a driven walk.", r =>
            {
                var g = r.G;
                bool Throws(string json) { try { Order.Parse(JNode.Parse(json, "order")); return false; } catch (FormatException) { return true; } }
                r.Is(Throws("{\"tick\":0,\"op\":\"scout\",\"do\":\"drive\",\"to\":[0,0],\"gait\":\"crouch\"}"), "a gait the rulebook doesn't name: rejected");
                r.Is(Throws("{\"tick\":0,\"op\":\"scout\",\"do\":\"drive\",\"to\":[0,0],\"face\":400}"), "a facing of 400: rejected");
                r.Is(Throws("{\"tick\":0,\"op\":\"scout\",\"do\":\"move\",\"to\":[0,0],\"release\":true}") && Throws("{\"tick\":0,\"op\":\"scout\",\"do\":\"move\",\"to\":[0,0],\"face\":10}"), "release or face on a move: rejected");
                r.Eq(g.Issue(new Order { Tick = g.Tick, Op = Scout, Do = "drive" }).code, "no-place", "a sample with no position: refused");
                var start = r.Op(Scout).Pos; int v = WalkCm(r);
                Push(r, Scout, new Pt(0, -2 * v), T(2));
                int went = start.Y - r.Op(Scout).Pos.Y;
                r.Is(went >= 2 * v * T(2) - 2 * v && went <= 2 * v * T(2) + 4 * v, $"pace 2000 per mille: {went} cm in 2 s (twice a walk's {v * T(2)})");
            }, DriveStart(pace: 2000));

            Add("DP1", "—", "The recorded driven session (the replay proof for game:check, brief 025): a session driven the way a host does it (a float controller's positions rounded to centimetres, one sample a tick, Shift on and off, a wall pushed, a Watch pushed into its cancel, a release), saved with its record. Replayed from its start state and order log on this runtime, every checkpoint matches the record, and the outcome.", DrivenSessionProof, DrivenSessionStart());
        }

        static Start DriveStart(int? pace = null, string? onBusy = null, string? onRelease = null, int? gap = null)
        {
            var s = new Start { DrivePace = pace, DriveOnBusy = onBusy, DriveOnRelease = onRelease, DriveGap = gap };
            return s;
        }

        static void GapCase(Run r, int gap)
        {
            var g = r.G; var guard = g.PosOf("guard")!.Value;
            var route = g.S.Grid.Route(guard, r.Stand("d:Office approach"))!;
            var dir = route[1] - route[0]; int len = Math.Max(1, G.Dist(route[0], route[1]));
            var spot = g.S.Grid.NearestFree(new Pt(guard.X + dir.X * 300 / len, guard.Y + dir.Y * 300 / len)) ?? guard;
            r.To(r.SendTo(Scout, spot));
            int nearest = int.MaxValue; bool closingInside = false;
            for (int k = 0; k < T(3); k++)
            {
                var gp = g.PosOf("guard")!.Value; var before = r.Op(Scout).Pos;
                var d = gp - before; int dl = Math.Max(1, G.Dist(gp, before)), v = WalkCm(r);
                Sample(r, Scout, dl <= v ? gp : new Pt(before.X + d.X * v / dl, before.Y + d.Y * v / dl)); g.Step();
                var after = r.Op(Scout).Pos; int now = G.Dist(after, g.PosOf("guard")!.Value);
                nearest = Math.Min(nearest, now);
                if (now < 40 && G.D2(after, gp) < G.D2(before, gp)) closingInside = true;
            }
            if (gap > 0) r.Is(!closingInside && nearest >= 38, $"never closer than the gap while closing (nearest {nearest} cm)");
            else r.Is(nearest <= 15, $"gap off: she walks to him ({nearest} cm)");
        }
    }
}
