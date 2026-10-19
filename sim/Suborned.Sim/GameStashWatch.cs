// The Scout's Watch on a container (GDD §05 bodies: "A container is emptied on a round every 15:00, its time learnable by Watch, in the
// mission only"). The round itself is GameTakedown's ServiceRound and the HUD's NextRound; this file holds what the cell has learned of
// it. The Watch reuses the rulebook's sight.watch (its seconds and its reach) and the Watch cancel line (lost: nothing learned). Mission
// knowledge only: never carried to the debrief. Hashed only once something is learned, so every run that never watches a container
// hashes as it always did.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>The containers whose round the cell has learned this mission, in the order learned, with the tick each was learned.</summary>
        readonly List<(string id, int tick)> roundsKnown = new List<(string id, int tick)>();

        /// <summary>Whether this id is a container stash (one with a servicing round).</summary>
        bool IsContainer(string id) => StashById(id) is StashPlace s && s.Kind == "container";

        /// <summary>Whether the cell knows this container's round (a completed Watch). Read-only.</summary>
        public bool KnowsRound(string stashId) => roundsKnown.Exists(r => r.id == stashId);

        /// <summary>A Watch on a container (the Scout's: §03 Watch reveals a routine): within sight.watch reach of the container's stand on a
        /// clear line; it takes sight.watch seconds, costs nothing, and lands as the round learned. A round already known is refused
        /// already-watched, free. `engage` false only checks (LearnQuery) and changes nothing.</summary>
        string WatchContainer(Op op, Order o, bool engage = true)
        {
            var s = StashById(o.Target);
            if (s == null || s.Kind != "container") return No("target-unknown", $"'{o.Target}' is no container to watch here");
            if (op.Id != OpId.Scout) return No("only-scout", "only the Scout watches a container's round (§03)");
            if (KnowsRound(s.Id)) return No("already-watched", $"the cell knows when the round empties the {s.Id} already: a Watch teaches nothing more");
            if (!G.Within(op.Pos, s.Stand, R.SightReachCm)) return No("too-far", $"stand within {R.SightReachCm / 100} m of the {s.Id}", m: R.SightReachCm / 100);
            if (!Sees(op.Pos, s.Stand)) return No("no-clear-line", $"no clear line: move to where the {Sim.Op.Name(op.Id)} can see it");
            if (!engage) return "";
            Engage(op, o, R.SightWatchS);
            op.BusyQuote = null; op.BusyPaid = 0;
            return "";
        }

        void LandWatchContainer(Op op, Order o)
        {
            var s = StashById(o.Target);
            if (s == null || s.Kind != "container" || KnowsRound(s.Id)) return;
            roundsKnown.Add((s.Id, Tick));
            int next = NextRound(s.Id).tick, secs = next / Hz;
            Note("scout.watch.container", $"The Scout has watched the {s.Id}: the round empties it every {R.ServicePeriodS / 60}:{R.ServicePeriodS % 60:00}, next at {secs / 60}:{secs % 60:00}.");
        }

        /// <summary>The learned rounds for the checkpoint hash: no bytes until the cell has learned one.</summary>
        internal void HashStashWatch(Hash.H h)
        {
            if (roundsKnown.Count == 0) return;
            h.S("rounds-known").I(roundsKnown.Count);
            foreach (var (id, tick) in roundsKnown) h.S(id).I(tick);
        }
    }
}
