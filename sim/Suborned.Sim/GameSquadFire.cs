// The squad fire order, agent by agent (engine 0.26.0; Sky playing 0.2.65: "I had the whole squad selected but some of my agents weren't
// firing at the target e.g. the Scout"; briefs/041/next-batch.md, item "squad fire"). A read-only host read; no rule, state or hash moves.
//  · FireQuery answers, for one operative, what a fire order on a target would get if it were given now: taken, or the refusal's code,
//    words and numbers, exactly as Issue would answer it (the same order of checks as Check then Fire: mission over, down, inside, busy,
//    in the car, then no weapon, carrying, walking Vale, the target, a civilian unforced, and out of reach or line with no walk in).
//  · Taken includes the cases the engine itself resolves: a holstered gun draws (brief 030), an order out of reach or line walks into the
//    effective ring first (weapons.close), a driven agent is never walked and holds. Walk says which: the walk in that Issue would start.
//  · The host asks it for every selected agent before a squad fire order, gives the order to each one it takes, and shows the refusal's
//    code on the plate of each one it doesn't (Latch's side; Fable's two words).
//  · Kept equal to Issue by the QF-* scenarios (FireQuery and Issue agree for every agent of every case they build).
using System;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Would `opId` take a fire order on `target` now (force: a civilian or contact named; focus: the focus click's order)? The
        /// answer Issue would give, without giving it. `walk`: taken, and it would walk into range or to a line first (never for a focus or a
        /// driven agent). Read-only: nothing changes but what PreviewFire also makes (a crowd person's gun the first time a shot names them).</summary>
        public Answer FireQuery(OpId opId, string target, bool force, bool focus, out bool walk)
        {
            var saved = refusal; refusal = default; string why; walk = false;
            try
            {
                var op = OpOf(opId); string name = Sim.Op.Name(opId);
                why = Over ? No("mission-over", "the mission is over")
                    : op.Down ? No("agent-down", $"the {name} is down")
                    : op.Inside ? No("enforcer-inside", "the Enforcer is inside the Office")
                    : op.Busy != null ? No("agent-busy", $"{name} is busy until {op.BusyEnd}", tick: op.BusyEnd)
                    : op.InCar ? No("in-car", $"the {name} is in the car: a move steps out first")
                    : FireWouldTake(op, name, target, force, focus, out walk);
            }
            catch (Exception e) when (!(e is OutOfMemoryException)) { why = No("engine-error", "engine error: " + e.GetType().Name + ": " + e.Message); walk = false; }
            var a = AnswerOf(why);
            refusal = saved;
            return a;
        }
        public Answer FireQuery(OpId opId, string target, bool force = false, bool focus = false) => FireQuery(opId, target, force, focus, out _);

        /// <summary>Fire's checks without its changes (GameCombat.cs Fire): the same codes and words, in the same order.</summary>
        string FireWouldTake(Op op, string name, string target, bool force, bool focus, out bool walk)
        {
            walk = false; var g = op.Gun;
            if (g.W == null) return No("no-weapon", $"{name} carries no weapon (Arms)");
            if (op.Carrying.HasValue || op.CarryBody.Length > 0) return No("fire-carrying", "carrying, the operative doesn't fire");
            if (CarriesVale(op)) return No("fire-carrying-vale", "walking Vale, the Enforcer doesn't fire: the others cover him (brief 018)");
            string t = FireTarget(op, target);
            if (t.Length == 0) return No("no-target", target == "hostiles" ? "no hostile in range with a clear line" : $"no {target} to fire at");
            if (IsOpId(t)) return No("fire-on-cell", "the cell doesn't fire on its own");
            if (Range && !RangeTarget(t)) return No("target-unknown", $"target '{target}' is not one the range spawned (brief 029: hostiles, bystanders, cars)");
            var tg = GunOf(t); if (tg == null) return No("target-unknown", $"target '{target}' is unknown (unknown = reject)");
            if (IsCivilian(t) && !force) return No("civilian-target", $"{t} is a civilian or a contact: a target only when the order is forced");
            var at = PosOf(t);
            if (!at.HasValue || tg.Down) return No("no-target", $"no {target} to fire at");
            bool far = !G.Within(op.Pos, at.Value, g.W.MaxCm), blind = !far && !Sees(op.Pos, at.Value);
            if (focus) return ""; // tuning.md §14: a focus is a lock, taken out of reach too (it never walks in; it lapses)
            if ((far || blind) && !CloseOn) return far ? No("out-of-range", $"out of range ({g.W.MaxCm / 100} m)", m: g.W.MaxCm / 100) : No("no-clear-line", "no clear line");
            if ((far || blind) && !op.Driven)
            {
                if (CloseTo(op, at.Value) == null) return far ? No("out-of-range", $"out of range ({g.W.MaxCm / 100} m) and no walk into range", m: g.W.MaxCm / 100) : No("no-clear-line", "no clear line and no walk to one");
                walk = true;
            }
            return "";
        }
    }
}
