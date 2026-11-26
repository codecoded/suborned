using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed class DistrictTraffic
    {
        public readonly struct Pose
        {
            public readonly Pt At, Facing;
            public readonly bool Moving;
            public Pose(Pt at, Pt facing, bool moving)
            {
                At = at;
                Facing = facing;
                Moving = moving;
            }
        }

        readonly DistrictMap map;
        readonly int[] held;
        readonly List<Pt> people = new List<Pt>();
        readonly Pose[] trams;
        public DistrictTraffic(DistrictMap map)
        {
            this.map = map;
            held = new int[map.Streets.Length];
            trams = new Pose[map.Trams.Length];
        }

        public Pose Tram(int i, int tick)
        {
            var s = DistrictMap.Shuttle(map.Trams[i].Path, tick, 1200 + i * 4179, 730, 95);
            return new Pose(s.at, s.facing, s.moving);
        }

        public Pose Car(int i, int tick)
        {
            var s = DistrictMap.Shuttle(map.Streets[i].Path, tick - held[i], 1900 + i * 1379, 440 + i % 4 * 35, 55);
            var right = CarGeo.Ahead(s.at, new Pt(s.facing.Y, -s.facing.X), 215);
            return new Pose(right, s.facing, s.moving);
        }

        // Outside the civic traffic simulation, road vehicles give way to pedestrians, the cell's
        // car and trams. A hold delays that vehicle's own timetable instead of snapping its pose.
        public void Step(Game g, DistrictRun d)
        {
            if (g.Tick % 3 != 0)
                return;
            people.Clear();
            foreach (var op in g.Ops)
                if (!op.InCar && !op.LeftMap && !op.Inside)
                    people.Add(op.Pos);
            for (int i = 0; i < map.People.Length; i++)
                if ((d.Roams(i) || d.Network.Away(i)) && g.DistrictPersonAt(i)is Pt p && !g.S.Grid.InGrid(p))
                    people.Add(p);
            for (int i = 0; i < trams.Length; i++)
                trams[i] = Tram(i, g.Tick);
            for (int i = 0; i < held.Length; i++)
            {
                var car = Car(i, g.Tick);
                if (!car.Moving || g.S.Grid.InGrid(car.At))
                    continue;
                var ahead = CarGeo.Ahead(car.At, car.Facing, 400);
                bool blocked = d.Network.HoldsRoad(ahead) || G.Within(ahead, g.GetawayCarPos(), 370);
                for (int k = 0; k < trams.Length && !blocked; k++)
                    blocked = G.Within(ahead, trams[k].At, 620);
                for (int k = 0; k < people.Count && !blocked; k++)
                    blocked = G.Within(ahead, people[k], 210);
                if (blocked)
                    held[i] += 3;
            }
        }

        internal void Hash(Hash.H h)
        {
            foreach (int value in held)
                h.I(value);
        }
    }
}
