// Engine 0.34.2 (Sky 2026-10-26: "entry seen by patrol" is "no, not a failure"; rulebook cell.enter.seen). The patrol robot that sees the
// Enforcer go in at the public door no longer fails the mission: the Enter goes on, and the robot radios what it saw, a seen act reported
// as attention reports a loiterer (GDD §05: a robot's report by radio after attention.report.call; a found fault, Alert at least
// attention.alert, no Trail, a sighting, the dial's dispatch to the door). One report a mission; a patrol downed before it lands drops it.
// Its attention clock on the Enforcer holds through the Enter as through any act (attention.rule). The guard's and the live entrance
// camera's sight of an Enter still fail (entry-seen-guard, entry-seen-camera). A rulebook pinned before 0.34.2 (no cell.enter.seen) fails
// entry-seen-patrol as it did. Hashed only once a report is made, so every run in which the patrol never saw an Enter hashes as before.
namespace Suborned.Sim
{
    public sealed partial class Game
    {
        int entryRepFrom = -1, entryRepLand = -1; bool entryRepDone, entryRepStopped; Pt entryRepAt;

        /// <summary>The patrol saw the Enter (Enter, rulebook cell.enter.seen.patrol "report"): its report goes out now and lands by radio.</summary>
        void EntrySeenByPatrol(Pt door)
        {
            if (entryRepFrom >= 0) return; // once a mission: the Office already has it
            entryRepFrom = Tick; entryRepLand = Tick + T(R.AttnCallS); entryRepAt = door;
            Note("entry.seen", $"The patrol sees the Enforcer go in and radios it (lands {Sim.Report.Clock(entryRepLand)}).");
        }

        /// <summary>Once a tick, after the loiterers' reports: the patrol's report of the Enter lands, or is dropped if the patrol is down.</summary>
        void EntryReports()
        {
            if (entryRepFrom < 0 || entryRepDone || entryRepLand > Tick) return;
            entryRepDone = true;
            if (PatrolDown) { entryRepStopped = true; Note("entry.stopped", "The patrol's report of the entry never lands."); return; }
            Sightings.Add(new Sighting { Watcher = "patrol", Op = "enforcer", From = entryRepFrom, To = Tick, At = entryRepAt });
            RaiseAlert(System.Math.Max(Alert, R.AttnAlert), "entry:patrol:enforcer");
            Note("entry.reported", $"The patrol's report lands: someone went in at the public door. Alert {Alert}.");
            Alarm(null, entryRepAt, null, "");
        }

        /// <summary>The patrol's report of the Enter: when made and when it lands (ticks; -1 none), and whether it landed or was stopped. Read-only.</summary>
        public (int from, int landAt, string state) EntryReport => (entryRepFrom, entryRepLand, entryRepFrom < 0 ? "" : entryRepStopped ? "stopped" : entryRepDone ? "landed" : "pending");

        internal void HashEntrySeen(Hash.H h)
        {
            if (entryRepFrom < 0) return;
            h.S("entry-seen").I(entryRepFrom).I(entryRepLand).P(entryRepAt).Z(entryRepDone).Z(entryRepStopped);
        }
    }
}
