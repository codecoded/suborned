// Engine 0.34.0 (brief 067, Aster: "no input may hang the engine"): S10 waited for the patrol's sweep to come onto the public door; once the
// sweep moved to the service yard it never did, the run ended (vale-leaves) and the wait kept calling Step on a finished game, which returns at
// once, so the process spun at 100 % of a core for ever. The engine's own loops all end (each walks a path, a heap or a ring that shrinks);
// a harness's step limit (Game.HarnessStepLimit, Scenarios.StepLimit) turns any caller's wait for what never comes into this exception.
using System;

namespace Suborned.Sim
{
    /// <summary>A game was stepped past its harness's StepLimit: a caller waits for something that can never come (a condition on a site that
    /// no longer has it, or a run that has ended). Thrown only where a harness set the limit; the host's games have none.</summary>
    public sealed class RunawayException : Exception
    {
        public readonly int Tick, Steps; public readonly bool Over;
        public RunawayException(Game g)
            : base($"run away: {g.StepCalls} Step calls (limit {g.StepLimit}) at {Report.Clock(g.Tick)}, {(g.Over ? $"the run ended at {Report.Clock(g.Tick)} ({g.Result}{(g.FailCode.Length > 0 ? " " + g.FailCode : "")}) and is still being stepped" : "the run has not ended")}: the caller waits for something that never comes")
        { Tick = g.Tick; Steps = g.StepCalls; Over = g.Over; }
    }
}
