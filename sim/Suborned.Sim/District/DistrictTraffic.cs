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

            public bool CarAhead(Pt p, int self, Pose[] cars, bool[] vans)
            {
                const int reach = 380;
                for (int x = Cell(p.X - reach); x <= Cell(p.X + reach); x++)
                    for (int y = Cell(p.Y - reach); y <= Cell(p.Y + reach); y++)
                        if (buckets.TryGetValue((x, y), out var ids))
                            foreach (int i in ids)
                            {
                                if (i == self || !G.Within(p, cars[i].At, vans[i] ? reach : 290)) continue;
                                var a = cars[self].Facing; var b = cars[i].Facing;
                                if ((long)a.X * b.X + (long)a.Y * b.Y > 0 || i < self) return true;
                            }
                return false;
            }
        }

        readonly DistrictMap map;
        readonly int[] held, streets, phases;
        readonly bool[] vans, waiting;
        readonly List<Pt> people = new();
        readonly Occupancy pavement = new(), carriageway = new();
        readonly Pose[] trams, cars;
        readonly TramService rail;
        public int TramCount => rail.Count;
        public int CarCount => cars.Length;
        public int RobotCount => map.RobotRounds.Length;
        public DistrictTraffic(DistrictMap map)
        {
            this.map = map;
            rail = new TramService(map);
            trams = new Pose[rail.Count];
            var routes = new List<int>(); var clocks = new List<int>();
            for (int i = 0; i < map.Streets.Length; i++)
            {
                var path = map.Streets[i].Path;
                int spacing = map.Streets[i].LaneCm >= 700 ? 29000 : 42000;
                int count = Math.Max(2, (int)((path.Length + spacing - 1) / spacing));
                long round = 2 * ((path.Length + CarPace(i) - 1) / CarPace(i) + 55);
                for (int k = 0; k < count; k++) { routes.Add(i); clocks.Add((int)((i * 1379L + 1900 + round * k / count) % round)); }
            }
            streets = routes.ToArray(); phases = clocks.ToArray();
            held = new int[streets.Length]; cars = new Pose[streets.Length];
            vans = new bool[cars.Length]; waiting = new bool[cars.Length];
            for (int i = 0; i < vans.Length; i++) vans[i] = (i * 3 + streets[i]) % 7 == 0;
        }
        static int CarPace(int street) => 410 + street % 5 * 37;
        public Pose Tram(int i, int tick) => rail.At(i, tick);
        public bool DeliveryVan(int i) => vans[i];
        public StreetRobot RobotKind(int i) => map.RobotRounds[i].Kind;
        public bool CarriesPapers(int i) => map.RobotRounds[i].CarriesPapers;
        public (int lift, long wheelMm) ServiceMechanism(int i, int tick) => map.RobotRounds[i].Mechanism(tick);
        public Pose Robot(int i, int tick) => map.RobotRounds[i].At(tick);
        public Pose Car(int i, int tick)
        {
            var p = ScheduledCar(i, tick);
            return waiting[i] ? new Pose(p.At, p.Facing, false) : p;
        }
        Pose ScheduledCar(int i, int tick)
        {
            int lane = streets[i];
            var s = DistrictMap.Shuttle(map.Streets[lane].Path, tick - held[i], phases[i], CarPace(lane), 55);
            var across = new Pt(s.facing.Y, -s.facing.X);
            int offset = map.Streets[lane].LaneCm;
            var right = CarGeo.Ahead(s.at, across, offset);
            // Beside a platform find the open part of this cross-section. Snapping to the
            // centre of a free two-metre grid cell made a moving car lurch between cells.
            if (!map.Roads.Free(right))
                for (int shift = 40; shift <= 380; shift += 40)
                {
                    var inner = CarGeo.Ahead(s.at, across, Math.Max(0, offset - shift));
                    if (map.Roads.Free(inner)) { right = inner; break; }
                    var outer = CarGeo.Ahead(s.at, across, offset + shift);
                    if (map.Roads.Free(outer)) { right = outer; break; }
                }
            return new Pose(right, s.facing, s.moving);
        }
        public void Step(Game g, DistrictRun d)
        {
            // Sense every third tick, but hold a yielding car on each intervening tick.
            // Subtracting three at once makes its wheels creep forward and jump backwards in a queue.
            for (int i = 0; i < held.Length; i++) if (waiting[i]) held[i]++;
            if (g.Tick % 3 != 0) return;
            pavement.Clear(); people.Clear();
            void Pedestrian(Pt p) { pavement.Add(p, people.Count); people.Add(p); }
            foreach (var op in g.Ops) if (!op.InCar && !op.LeftMap && !op.Inside) Pedestrian(op.Pos);
            for (int i = 0; i < map.People.Length; i++)
                if ((d.Roams(i) || d.Network.Away(i)) && g.DistrictPersonAt(i) is Pt p && !g.S.Grid.InGrid(p)) Pedestrian(p);
            for (int i = 0; i < trams.Length; i++) trams[i] = Tram(i, g.Tick);
            carriageway.Clear();
            for (int i = 0; i < cars.Length; i++)
            {
                cars[i] = ScheduledCar(i, g.Tick);
                carriageway.Add(cars[i].At, i);
            }
            for (int i = 0; i < cars.Length; i++)
            {
                var car = cars[i];
                waiting[i] = false;
                if (!car.Moving || g.S.Grid.InGrid(car.At)) continue;
                var ahead = CarGeo.Ahead(car.At, car.Facing, vans[i] ? 510 : 420);
                bool blocked = d.Network.HoldsRoad(ahead) || G.Within(ahead, g.GetawayCarPos(), 370) || pavement.Blocked(ahead, 210, people);
                for (int k = 0; k < trams.Length && !blocked; k++) blocked = G.Within(ahead, trams[k].At, 620);
                if (!blocked) blocked = carriageway.CarAhead(ahead, i, cars, vans);
                waiting[i] = blocked;
            }
        }
        internal void Hash(Hash.H h) { for (int i = 0; i < held.Length; i++) h.I(held[i]).Z(waiting[i]); }
    }
}
