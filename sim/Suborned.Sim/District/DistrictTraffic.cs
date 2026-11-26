using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public sealed class DistrictTraffic
    {
        public readonly struct Pose
        {
            public readonly Pt At, Facing;
            public readonly bool Moving;
            public Pose(Pt at, Pt facing, bool moving) { At = at; Facing = facing; Moving = moving; }
        }

        // Blockers are indexed only while the simulation clock advances. A long sightline can
        // contain the whole service without a car having to test the entire city's population.
        sealed class Occupancy
        {
            readonly Dictionary<(int, int), List<int>> buckets = new();
            readonly Stack<List<int>> pool = new();
            public void Clear() { foreach (var b in buckets.Values) { b.Clear(); pool.Push(b); } buckets.Clear(); }
            static int Cell(int cm) => Grid.FloorDiv(cm, 1200);
            public void Add(Pt p, int id)
            {
                var key = (Cell(p.X), Cell(p.Y));
                if (!buckets.TryGetValue(key, out var b)) buckets.Add(key, b = pool.Count > 0 ? pool.Pop() : new List<int>());
                b.Add(id);
            }
            public bool Blocked(Pt p, int r, List<Pt> people)
            {
                for (int x = Cell(p.X - r); x <= Cell(p.X + r); x++)
                    for (int y = Cell(p.Y - r); y <= Cell(p.Y + r); y++)
                        if (buckets.TryGetValue((x, y), out var ids))
                            foreach (int i in ids) if (G.Within(people[i], p, r)) return true;
                return false;
            }
        }

        readonly DistrictMap map;
        readonly int[] held, streets, phases;
        readonly DistrictLine[] patrols;
        readonly List<Pt> people = new();
        readonly Occupancy pavement = new();
        readonly Pose[] trams, cars;
        readonly TramService rail;
        public int TramCount => rail.Count;
        public int CarCount => cars.Length;
        public int RobotCount => patrols.Length;
        public DistrictTraffic(DistrictMap map)
        {
            this.map = map;
            rail = new TramService(map);
            trams = new Pose[rail.Count];
            var routes = new List<int>(); var clocks = new List<int>();
            for (int i = 0; i < map.Streets.Length; i++)
            {
                var path = map.Streets[i].Path;
                int count = Math.Max(1, (int)((path.Length + 64999) / 65000));
                long round = 2 * ((path.Length + CarPace(i) - 1) / CarPace(i) + 55);
                for (int k = 0; k < count; k++) { routes.Add(i); clocks.Add((int)((i * 1379L + 1900 + round * k / count) % round)); }
            }
            streets = routes.ToArray(); phases = clocks.ToArray();
            held = new int[streets.Length]; cars = new Pose[streets.Length];
            // Patrol a public gathering from one end to the other rather than crossing house walls.
            patrols = map.People.Where(r => r.Local && r.Id.EndsWith("-01", StringComparison.Ordinal))
                .Where((_, i) => i % 2 == 0).Select(r => new DistrictLine { Id = r.Id, Path = r.Walk }).ToArray();
        }
        static int CarPace(int street) => 410 + street % 5 * 37;
        public Pose Tram(int i, int tick) => rail.At(i, tick);
        public Pose Robot(int i, int tick)
        {
            var s = DistrictMap.Shuttle(patrols[i].Path, tick, i * 91, 85, 190);
            return new Pose(s.at, s.facing, s.moving);
        }
        public Pose Car(int i, int tick)
        {
            int lane = streets[i];
            var s = DistrictMap.Shuttle(map.Streets[lane].Path, tick - held[i], phases[i], CarPace(lane), 55);
            var right = CarGeo.Ahead(s.at, new Pt(s.facing.Y, -s.facing.X), map.Streets[lane].LaneCm);
            if (!map.Roads.Free(right) && map.Roads.NearestFree(right) is Pt near && G.Within(near, right, 380)) right = near;
            return new Pose(right, s.facing, s.moving);
        }
        public void Step(Game g, DistrictRun d)
        {
            if (g.Tick % 3 != 0) return;
            pavement.Clear(); people.Clear();
            void Pedestrian(Pt p) { pavement.Add(p, people.Count); people.Add(p); }
            foreach (var op in g.Ops) if (!op.InCar && !op.LeftMap && !op.Inside) Pedestrian(op.Pos);
            for (int i = 0; i < map.People.Length; i++)
                if ((d.Roams(i) || d.Network.Away(i)) && g.DistrictPersonAt(i) is Pt p && !g.S.Grid.InGrid(p)) Pedestrian(p);
            for (int i = 0; i < trams.Length; i++) trams[i] = Tram(i, g.Tick);
            for (int i = 0; i < cars.Length; i++) cars[i] = Car(i, g.Tick);
            for (int i = 0; i < cars.Length; i++)
            {
                var car = cars[i];
                if (!car.Moving || g.S.Grid.InGrid(car.At)) continue;
                var ahead = CarGeo.Ahead(car.At, car.Facing, 420);
                bool blocked = d.Network.HoldsRoad(ahead) || G.Within(ahead, g.GetawayCarPos(), 370) || pavement.Blocked(ahead, 210, people);
                for (int k = 0; k < trams.Length && !blocked; k++) blocked = G.Within(ahead, trams[k].At, 620);
                for (int k = 0; k < cars.Length && !blocked; k++)
                    if (k != i && G.Within(ahead, cars[k].At, 290))
                    {
                        long heading = (long)car.Facing.X * cars[k].Facing.X + (long)car.Facing.Y * cars[k].Facing.Y;
                        blocked = heading > 0 || k < i;
                    }
                if (blocked) held[i] += 3;
            }
        }
        internal void Hash(Hash.H h) { foreach (int v in held) h.I(v); }
    }
}
