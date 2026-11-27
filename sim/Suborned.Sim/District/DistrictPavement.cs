// Civil machines run on a clock. They can hold one tick to give somebody room.
// The same grounded positions feed the sim, meetings, traffic and both crowd LODs.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    using Pose = DistrictTraffic.Pose;

    internal sealed partial class DistrictPavement
    {
        readonly DistrictMap map;
        readonly int[] radius, delays, clock, previousClock, passing, cellPassing = new int[4];
        readonly bool[] brake;
        readonly Pose[] machines, proposed;
        Pose[] people, lastPeople;
        readonly Dictionary<(int,int), List<int>> cells = new();
        readonly Stack<List<int>> pool = new();
        int sampled = -1;
        const int CellCm = 500;

        public DistrictPavement(DistrictMap map)
        {
            this.map = map;
            int n = map.RobotRounds.Length;
            radius = new int[n]; delays = new int[n]; clock = new int[n]; previousClock = new int[n];
            machines = new Pose[n]; proposed = new Pose[n]; brake = new bool[n];
            for (int i = 0; i < n; i++) radius[i] = Footprint(map.RobotRounds[i].Kind);
            people = new Pose[map.People.Length]; lastPeople = new Pose[people.Length]; passing = new int[people.Length];
        }

        // Enclosing ground radius in cm, including brushes, wheel forks, outstretched arms and luggage.
        internal static int Footprint(StreetRobot kind) => kind switch {
            StreetRobot.Porter => 120, StreetRobot.Shopkeeper => 75, StreetRobot.Sweeper => 58,
            StreetRobot.CapsuleRunner => 40, _ => 72
        };

        static int Cell(int c) => Grid.FloorDiv(c, CellCm);
        List<int>? Near(Pt at) => cells.TryGetValue((Cell(at.X), Cell(at.Y)), out var ids) ? ids : null;

        void IndexMachines()
        {
            foreach (var list in cells.Values) { list.Clear(); pool.Push(list); }
            cells.Clear();
            for (int i = 0; i < machines.Length; i++)
            {
                var at = machines[i].At;
                // Includes foresight, one robot step and the offset of the longest trolley.
                int reach = radius[i] + 325;
                for (int x = Cell(at.X-reach); x <= Cell(at.X+reach); x++)
                for (int y = Cell(at.Y-reach); y <= Cell(at.Y+reach); y++)
                {
                    var key = (x,y);
                    if (!cells.TryGetValue(key, out var list)) cells.Add(key, list = pool.Count > 0 ? pool.Pop() : new List<int>());
                    list.Add(i);
                }
            }
        }

        internal void Open(DistrictRun district, int tick)
        {
            Array.Clear(delays,0,delays.Length);
            Array.Clear(passing,0,passing.Length);
            Array.Clear(cellPassing,0,cellPassing.Length);
            sampled = tick;
            for (int i = 0; i < machines.Length; i++)
            {
                clock[i] = previousClock[i] = tick;
                machines[i] = map.RobotRounds[i].At(tick);
            }
            IndexMachines();
            for (int i = 0; i < people.Length; i++)
            {
                var planned = district.ScheduledPose(i,tick);
                var clear = Place(planned.at, i, map.Walk);
                people[i] = lastPeople[i] = new Pose(clear, planned.facing, planned.moving);
            }
        }

        internal bool TryPerson(int who, int tick, out Pose p)
        {
            if (tick == sampled) { p = people[who]; return true; }
            if (tick >= 0 && tick == sampled - 1) { p = lastPeople[who]; return true; }
            p = default; return false;
        }

        internal int RobotClock(int who, int tick) =>
            tick == sampled-1 ? previousClock[who] : Math.Max(0, tick-delays[who]);

        internal Pose Robot(int who, int tick) =>
            tick == sampled ? machines[who] : map.RobotRounds[who].At(RobotClock(who,tick));

        void GiveWay(Pt pedestrian)
        {
            var nearby = Near(pedestrian);
            if (nearby == null) return;
            foreach (int i in nearby)
            {
                long distance = G.D2(proposed[i].At,pedestrian);
                int gap = radius[i] + PersonCm + 12;
                if (distance <= (long)gap*gap && distance < G.D2(machines[i].At,pedestrian)) brake[i] = true;
            }
        }

        public void Step(Game game, DistrictRun district)
        {
            if (sampled == game.Tick) return;
            if (sampled != game.Tick-1) Open(district,game.Tick-1);
            for (int i = 0; i < machines.Length; i++)
            {
                proposed[i] = map.RobotRounds[i].At(clock[i]+1);
                brake[i] = false;
            }
            foreach (var op in game.Ops)
                if (!op.InCar && !op.Inside && !op.LeftMap) GiveWay(op.Pos);
            for (int i = 0; i < people.Length; i++)
            {
                if (district.Network.FellAt[i] is Pt fallen) GiveWay(fallen);
                else if (district.Roams(i) || district.Network.Away(i)) GiveWay(people[i].At);
            }
            // Civic actors with their own posts, fights and departures keep their established body law.
            foreach (var w in game.Walkers)
            {
                if (w.St.Length == 0 && map.Index.TryGetValue(w.P.Id,out int who) && district.Roams(who)) continue;
                if (game.CrowdPos(w) is Pt at) GiveWay(at);
            }

            for (int i = 0; i < machines.Length; i++)
            {
                previousClock[i] = clock[i];
                if (brake[i])
                {
                    delays[i]++;
                    machines[i] = new Pose(machines[i].At, machines[i].Facing, false);
                }
                else { clock[i]++; machines[i] = proposed[i]; }
            }
            IndexMachines();
            (people,lastPeople) = (lastPeople,people);
            for (int i = 0; i < people.Length; i++)
            {
                var raw = district.ScheduledPose(i,game.Tick);
                var prior = lastPeople[i];
                if (district.Meetings[i].HasValue && !district.Network.Away(i) || district.Network.Fallen[i])
                {
                    people[i] = new Pose(raw.at,raw.facing,false);
                    passing[i] = 0;
                    continue;
                }
                // At most a walking stride, including while returning to a route after a sidestep.
                Pt at = Walk(prior.At,raw.at,raw.facing,raw.moving,16,i,ref passing[i],map.Walk);
                bool moving = at != prior.At;
                people[i] = new Pose(at,moving ? at-prior.At : raw.facing,moving);
            }
            sampled = game.Tick;
        }

        internal Pt CellStep(Op op, Pt target, Pt heading, int stride, Grid floor, bool arriving)
        {
            int id = (int)op.Id;
            return Walk(op.StepFrom,target,heading,true,stride,id,ref cellPassing[id],floor,arriving);
        }

        internal bool AtOccupiedDestination(Pt at, Pt target)
        {
            var nearby = Near(target);
            if (nearby == null || !Clear(at,Near(at))) return false;
            foreach (int r in nearby)
                if (G.Within(target,machines[r].At,radius[r]+PersonCm) &&
                    G.Within(at,target,radius[r]+PersonCm+40)) return true;
            return false;
        }

        internal void Hash(Hash.H h)
        {
            for (int i = 0; i < delays.Length; i++) if (delays[i] > 0) h.S("service.wait").I(i).I(delays[i]);
            h.S("pavement").I(sampled);
            for (int i = 0; i < people.Length; i++) h.P(people[i].At).I(passing[i]);
            foreach (int side in cellPassing) h.I(side);
        }
    }
}
