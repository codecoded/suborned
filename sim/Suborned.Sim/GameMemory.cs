// A person's memory and ladder moves (rulebook memory, ladders): moved here from GameOrders.cs (brief 096, Keel: that file is over the 500-line mark).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        void LadderMove(Person p, string move)
        {
            foreach (var (k, by) in R.Ladders[move]) p.Sc[k] = R.Clamp(k, p.Sc[k] + by);
        }

        /// <summary>rulebook memory (K1): an entry on the subject; when full, the oldest sighting or told fact goes first (Motive's review #9).</summary>
        void Remember(Person p, string act, string by, bool witnessed)
        {
            p.Memory.Add(new Mem { Act = act, By = by, Place = p.Place, Tick = Tick, Witnessed = witnessed });
            if (p.Memory.Count <= R.MemoryCap) return;
            int drop = -1;
            for (int j = 0; j < p.Memory.Count; j++)
                if (Sighting(p.Memory[j].Act) && (drop < 0 || p.Memory[j].Tick < p.Memory[drop].Tick)) drop = j;
            if (drop < 0) for (int j = 0; j < p.Memory.Count; j++) if (drop < 0 || p.Memory[j].Tick < p.Memory[drop].Tick) drop = j;
            p.Memory.RemoveAt(drop);
        }

        /// <summary>rulebook memory.whenFull: what a person saw (or was told) goes before what was done to them.</summary>
        static bool Sighting(string act) => act == "saw-act" || act == "saw-runner" || act == "saw-gunfire" || act == "saw-violence" || act == "saw-weapon-drawn" || act == "told"
            || act == "saw-takedown" || act == "saw-carry" || act == "found-body" || act == "heard-alarm"; // brief 013's sightings
    }
}
