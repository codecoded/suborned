// Brief 098 step 1b (Sky 2026-11-07: "shot cars can get past, others block"): a passing car standing on a mission car's way out boxes it in
// (GameBoxIn.cs). This file only keeps the record of it, for the host and the sweep (never hashed, never read by a rule): each spell a passing
// car boxed the getaway car or the official car in where it stood, who boxed it, and how much of it someone was aboard to leave (the cell in
// the getaway car, Vale in his). Read at the end of each traffic tick; nothing without traffic.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>One spell a passing car boxed a mission car in: the mission car (car:getaway, car:official), the first passing car in its way (By),
    /// the tick it began, how long it lasted (so far, while Open), and the ticks of it with someone aboard who could have left.</summary>
    public sealed class MissionBox { public string Car = "", By = ""; public int From, Ticks, Aboard; public bool Open; }

    public sealed partial class Game
    {
        /// <summary>Brief 098 step 1b (host and sweep read, never hashed): every spell passing traffic boxed a mission car in, in the order they began.</summary>
        public readonly List<MissionBox> MissionBoxes = new List<MissionBox>();

        /// <summary>The end of each traffic tick (not the warm-up): who of the passing cars boxes the getaway car and the official car in now.</summary>
        void TrafficBoxWatch()
        {
            BoxSpell("car:getaway", GetawayBoxers(), AnyInCar);
            BoxSpell("car:official", OfficialBoxers(), Vale == ValeAt.InCar || (Vale == ValeAt.Lent && lentId == "car:official"));
        }
        void BoxSpell(string car, List<string> bx, bool aboard)
        {
            string? by = bx.Find(IsTrafficId); var last = MissionBoxes.FindLast(x => x.Car == car);
            if (by == null) { if (last != null) last.Open = false; return; }
            if (last == null || !last.Open) MissionBoxes.Add(last = new MissionBox { Car = car, From = Tick, By = by, Open = true });
            last.Ticks++; if (aboard) last.Aboard++;
        }
    }
}
