// The driven agent (brief 025 playground, EXPERIMENTAL; Keel, briefs/024/keel-wasd.md; rulebook cell.drive). The host moves one operative
// freely (a character controller) and hands the engine where it is each tick as a drive sample, an order in the log like any other, so a
// replay reads the samples back and never re-runs the controller. The engine checks every sample against the agent's pace budget and the walk
// grid (and the gap to people), keeps the legal part, and the agent is then simply where it stands for every other rule. Integer state only.
using System;

namespace Suborned.Sim
{
    /// <summary>What the host reads back each tick for a driven operative (Game.DrivePose).</summary>
    public struct DrivePose
    {
        public bool Driven;   // the engine has this operative under the host's control
        public Pt Pos;        // where the engine has it (cm): the host's controller goes here when it differs
        public int Face;      // the last sample's facing (degrees), -1 if none
        public bool Running;  // moving at a run this tick (a run the engine took: carrying or tethered, it walks)
        public bool Corrected; // this tick's sample was not taken whole (budget, a wall, the gap, a busy agent pinned)
        public int Corrections; // samples corrected since control began (diagnostic)
    }

    public sealed partial class Game
    {
        /// <summary>The driven agent's pace, onBusy, onRelease and gap for this run: the playground start's (Start.drive) over cell.drive.</summary>
        int DrivePacePm => Start.DrivePace ?? 1000;
        int DriveGapCm => Start.DriveGap ?? R.Drive!.GapCm;
        string DriveOnBusy => Start.DriveOnBusy ?? R.Drive!.OnBusy;
        string DriveOnRelease => Start.DriveOnRelease ?? R.Drive!.OnRelease;

        /// <summary>The host's read-back for one operative (read-only).</summary>
        public DrivePose DrivePoseOf(OpId id)
        {
            var op = OpOf(id);
            return new DrivePose { Driven = op.Driven, Pos = op.Pos, Face = op.Face, Running = op.Driven && op.Running, Corrected = op.Driven && driveCorrected[(int)id] == Tick, Corrections = op.DriveHeld };
        }
        readonly int[] driveCorrected = { -1, -1, -1, -1 }; // the tick each operative's sample was last corrected (derived from the samples: not hashed)

        /// <summary>A drive sample for this tick, ready to Issue: where the host's controller has the operative (cm), Shift held, facing (degrees).</summary>
        public Order DriveSample(OpId id, Pt at, bool run, int face) => new Order { Tick = Tick, Op = Sim.Op.Name(id), Do = "drive", Point = at, Run = run, Face = ((face % 360) + 360) % 360 };
        /// <summary>The order that hands control of an operative back (it stands, or resumes: cell.drive.onRelease).</summary>
        public Order DriveRelease(OpId id) => new Order { Tick = Tick, Op = Sim.Op.Name(id), Do = "drive", Release = true };

        /// <summary>The drive order (rulebook cell.drive): taken now, applied by the next step. Down and inside are refused before this.</summary>
        string Drive(Op op, Order o)
        {
            if (R.Drive == null) return No("unknown-verb", "verb 'drive' is unknown (unknown = reject): the rulebook has no cell.drive");
            if (o.Release) { if (op.Driven) EndDrive(op, true); return ""; }
            if (!o.Point.HasValue) return No("no-place", "a drive sample says where the agent is (to: [x, y] cm)");
            if (op.InCar) return No("in-car", $"the {o.Op} is in the car: a move steps out first");
            if (!op.Driven)
            {
                op.Driven = true; op.DriveSpan = false; op.DrivePush = 0; op.DriveResume = null; op.DriveHeld = 0;
                if (op.Walk != null) { op.DriveResume = op.Walk.End; op.DriveResumeRun = op.Running; } // the walk control takes over (resumed on release if onRelease resume)
                op.Walk = null; op.WalkMm = 0; op.Running = false;
                op.DriveBudget = DriveCapMm(op, false);
            }
            op.DriveTo = o.Point; op.DriveRun = o.Run;
            if (o.Face >= 0) op.Face = ((o.Face % 360) + 360) % 360;
            return "";
        }

        /// <summary>mm a tick the operative may move driven: a walk (a carrier's while carrying), or a run when one is allowed (RunRefusal less
        /// "busy": a busy agent is pinned anyway), scaled by the playground pace.</summary>
        int DriveCapMm(Op op, bool run)
        {
            bool canRun = run && !(op.Id == OpId.Enforcer && (Vale == ValeAt.WithEnforcer || Vale == ValeAt.Taking)) && !op.Carrying.HasValue && op.CarryBody.Length == 0 && !(op.Id == OpId.Tech && LoopOn);
            int pace = PaceOf(op, canRun);
            return Math.Max(1, (int)((long)pace * DrivePacePm / 1000));
        }
        bool DriveCanRun(Op op) => op.DriveRun && DriveCapMm(op, true) > DriveCapMm(op, false);

        /// <summary>One tick of a driven operative (from the step, where a walk would advance).</summary>
        void StepDrive(Op op)
        {
            if (op.Down || op.Inside || op.InCar || op.CarriedBy.HasValue) { EndDrive(op, false); return; }
            var d = R.Drive!; var want = op.DriveTo; op.DriveTo = null; op.DriveMoved = false;
            bool run = DriveCanRun(op); int cap = DriveCapMm(op, run);
            op.DriveBudget = Math.Min(op.DriveBudget + cap, cap * d.BurstTicks);
            if (!op.DriveRun || !run) op.DriveSpan = false; // a walk sample (or a run refused: carrying, tethered) ends the run span
            op.Running = false;
            if (!want.HasValue) return;
            var from = op.Pos; var to = want.Value;
            if (op.Busy != null)
            {
                int push = G.Dist(from, to);
                if (push <= d.SlackCm) { op.DrivePush = 0; return; }
                if (DriveOnBusy == "hold") { Corrected(op); return; }
                op.DrivePush += push;
                if (op.DrivePush < d.BusyPushCm) { Corrected(op); return; }
                op.DrivePush = 0;
                Cancel(op); // the cancel order's own cost and log (rulebook cancel)
                if (op.Busy != null) { Corrected(op); return; } // an act that can't be cancelled (the wait inside): pinned
            }
            else op.DrivePush = 0;
            bool whole = true;
            long dist = Mm(from, to), allow = op.DriveBudget + d.SlackCm * 10L; // millimetres, so a step's length is never rounded down into the budget
            if (dist > allow) { to = G.Lerp(from, to, allow, dist); whole = false; }
            var at = AxisX(from, to.X); at = AxisY(at, to.Y);
            int gap = DriveGapCm;
            if (gap > 0 && !GapClear(from, at, gap))
            {
                var xOnly = AxisX(from, to.X); var yOnly = AxisY(from, to.Y);
                at = GapClear(from, xOnly, gap) ? xOnly : GapClear(from, yOnly, gap) ? yOnly : from;
            }
            if (at != want.Value) whole = false;
            if (!whole) Corrected(op);
            op.DriveBudget = (int)Math.Max(-d.SlackCm * 10L, op.DriveBudget - Mm(from, at)); // slack overdraws the budget, never adds to it: the pace holds on average
            op.Pos = at; op.DriveMoved = at != from;
            if (run && op.DriveMoved)
            {
                if (!op.DriveSpan)
                {
                    op.DriveSpan = true; op.RunAlerted = false; op.RunSeen.Clear();
                    if (op.Gun.Order.Length > 0) { op.Gun.Order = ""; op.Gun.Force = false; op.Gun.Target = ""; Note("fire.cancelled", $"{Sim.Op.Name(op.Id)} runs: the fire order ends."); }
                }
                op.Running = true; RunAttention(op);
            }
        }
        static long Mm(Pt a, Pt b) => G.Isqrt(G.D2(a, b) * 100); // mm between two cm points, floored to the mm
        void Corrected(Op op) { op.DriveHeld++; driveCorrected[(int)op.Id] = Tick; }

        /// <summary>Move along x from p to x, stopping at the last centimetre before the first blocked cell (the cell p stands in is passable).</summary>
        Pt AxisX(Pt p, int x)
        {
            var g = S.Grid; int j = g.CJ(p.Y), i0 = g.CI(p.X), i1 = g.CI(x);
            if (i1 == i0) return new Pt(x, p.Y);
            int step = i1 > i0 ? 1 : -1;
            for (int i = i0 + step; ; i += step)
            {
                if (!g.FreeCell(i, j)) return new Pt(step > 0 ? g.X0 + i * g.Cell - 1 : g.X0 + (i + 1) * g.Cell, p.Y);
                if (i == i1) return new Pt(x, p.Y);
            }
        }
        Pt AxisY(Pt p, int y)
        {
            var g = S.Grid; int i = g.CI(p.X), j0 = g.CJ(p.Y), j1 = g.CJ(y);
            if (j1 == j0) return new Pt(p.X, y);
            int step = j1 > j0 ? 1 : -1;
            for (int j = j0 + step; ; j += step)
            {
                if (!g.FreeCell(i, j)) return new Pt(p.X, step > 0 ? g.Y0 + j * g.Cell - 1 : g.Y0 + (j + 1) * g.Cell);
                if (j == j1) return new Pt(p.X, y);
            }
        }
        /// <summary>The step from → to keeps the gap: it ends within `gap` of nobody on the street it came nearer to (stepping away is free).</summary>
        bool GapClear(Pt from, Pt to, int gap)
        {
            if (to == from) return true;
            foreach (var (p, at) in OnTheStreet())
            {
                if (p.Down) continue;
                if (G.Within(to, at, gap) && G.D2(to, at) < G.D2(from, at)) return false;
            }
            return true;
        }

        /// <summary>Control ends: released by the host (release true: onRelease applies), or by the game (down, inside, in the car, a click move).</summary>
        void EndDrive(Op op, bool release)
        {
            op.Driven = false; op.DriveTo = null; op.DriveRun = false; op.DriveSpan = false; op.DriveMoved = false; op.Running = false; op.DrivePush = 0; op.DriveBudget = 0;
            var resume = op.DriveResume; bool resumeRun = op.DriveResumeRun; op.DriveResume = null; op.DriveResumeRun = false;
            if (!release || DriveOnRelease != "resume" || !resume.HasValue || op.Down || op.Inside || op.InCar || op.Busy != null) return;
            var route = S.Grid.Route(op.Pos, resume.Value); if (route == null) return;
            op.Walk = new Path(route); op.WalkMm = 0; if (op.Walk.Length == 0) { op.Walk = null; return; }
            bool run = resumeRun && DriveCapMm(op, true) > DriveCapMm(op, false); // a run where one is allowed (as RunRefusal, without its refusal)
            op.Running = run; op.WalkSpeed = PaceOf(op, run);
            if (run) { op.RunAlerted = false; op.RunSeen.Clear(); }
            Note("order.move", $"{Sim.Op.Name(op.Id)} {(run ? "runs" : "heads")} on for {resume.Value}.");
        }
    }
}
