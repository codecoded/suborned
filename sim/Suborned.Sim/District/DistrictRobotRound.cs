using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public enum StreetRobot { Patrol, Porter, Shopkeeper, Waiter, Nanny, Butler }

    // A machine follows a fixed working round on the same clear floor as the people it serves.
    internal sealed class DistrictRobotRound
    {
        public readonly StreetRobot Kind;
        public readonly bool CarriesPapers;
        readonly Path route;
        readonly int phase, pace, dwell;

        DistrictRobotRound(StreetRobot kind, DistrictResident resident, bool papers = false)
        {
            Kind = kind;
            CarriesPapers = papers;
            route = resident.Walk;
            phase = resident.Phase;
            (pace, dwell) = kind switch {
                StreetRobot.Porter => (108, 55), StreetRobot.Waiter => (93, 68),
                StreetRobot.Shopkeeper => (74, 90), StreetRobot.Nanny => (79, 64),
                _ => (91, 72)
            };
        }

        public DistrictTraffic.Pose At(int tick)
        {
            var s = DistrictMap.Shuttle(route, tick, phase, pace, dwell);
            return new DistrictTraffic.Pose(s.at, s.facing, s.moving);
        }

        public (int lift, long wheelMm) Mechanism(int tick)
        {
            var (number, leg) = DistrictMap.ShuttleLeg(route, tick, phase, pace, dwell);
            long distance = number * route.Length + Math.Min(route.Length, Math.Max(0, leg - dwell) * pace);
            // The deck or hand rises, holds for inspection, then stows before the wheels start.
            int lift = (int)Math.Clamp(Math.Min(leg, dwell - leg) * 2500 / dwell, 0, 1000);
            return (lift, distance);
        }

        public static DistrictRobotRound[] Make(DistrictMap map)
        {
            var rounds = new List<DistrictRobotRound>();
            foreach (var venue in map.Ground.Venues)
            {
                bool sideStreet = venue.Id.Contains("-round-");
                if (sideStreet && !venue.Id.EndsWith("0", StringComparison.Ordinal)) continue;
                var kind = venue.Style switch {
                    "platform" or "waterfront" => StreetRobot.Porter,
                    "coffee" => StreetRobot.Waiter,
                    "market" or "papers" => StreetRobot.Shopkeeper,
                    "garden" => StreetRobot.Nanny,
                    _ => sideStreet ? StreetRobot.Patrol : StreetRobot.Butler
                };
                string prefix = "p-z-harbour-" + venue.Id + "-";
                var locals = map.People.Where(p => p.Id.Length == prefix.Length + 2 && p.Id.StartsWith(prefix, StringComparison.Ordinal)
                    && p.Walk.Length > 1500).OrderBy(p => Math.Abs(p.Walk.Length - 26000)).ToArray();
                if (locals.Length == 0) continue;
                rounds.Add(new DistrictRobotRound(kind, locals[0], venue.Style == "papers"));
                // The porter brings stock in while the shop machine tends its counter.
                if (venue.Style == "market" && locals.Length > 1)
                    rounds.Add(new DistrictRobotRound(StreetRobot.Porter, locals[locals.Length - 1]));
            }
            return rounds.ToArray();
        }
    }
}
