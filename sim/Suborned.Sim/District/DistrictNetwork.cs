using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public enum CitizenAct { Block, Rally, Guard, Protect }
    public enum CitizenStage { Calling, Walking, Gathering, Acting }

    public sealed class CitizenRequest
    {
        public CitizenAct Act;
        public CitizenStage Stage;
        public int[] Chain = Array.Empty<int>();
        public int Heard, Due, Resolve;
        public int Actor => Chain[Chain.Length - 1];
        public Pt Site;
        public string Place = "", Update = "";
        public readonly List<int> Crowd = new List<int>();
    }

    public sealed class DistrictNetwork
    {
        public const int CallTicks = 8 * Game.Hz;
        const int WalkPace = 135;
        sealed class Trip
        {
            public Path Path = null!;
            public int Start, Arrive, Return = -1;
            public long Distance(int tick)
            {
                long walked = (long)(tick - Start) * WalkPace;
                if (Return >= 0 && tick >= Return)
                    walked = Math.Min(Path.Length, (long)(Return - Start) * WalkPace) - (long)(tick - Return) * WalkPace;
                return Math.Clamp(walked, 0, Path.Length);
            }
        }

        readonly DistrictRun run;
        readonly SortedDictionary<int, SortedSet<int>> met = new SortedDictionary<int, SortedSet<int>>();
        readonly Trip?[] trips;
        int outOnStreet;
        public readonly Pt?[] FellAt;
        public readonly List<CitizenRequest> Requests = new List<CitizenRequest>();
        public readonly string[] AskedBy;
        public readonly bool[] KnowsCell, Fallen;
        public readonly int[] Wounds;
        public int Completed { get; internal set; }

        public DistrictNetwork(DistrictRun run)
        {
            this.run = run;
            int n = run.Map.People.Length;
            trips = new Trip?[n];
            FellAt = new Pt?[n];
            AskedBy = new string[n];
            KnowsCell = new bool[n];
            Fallen = new bool[n];
            Wounds = new int[n];
        }

        public static string Name(CitizenAct act) => act switch
        {
            CitizenAct.Block => "Block a street",
            CitizenAct.Rally => "Start a riot",
            CitizenAct.Guard => "Attack the Records Office guard",
            _ => "Stand up for the cell"
        };

        public static int Needed(CitizenAct act) => act == CitizenAct.Block ? 1 : act == CitizenAct.Guard ? 3 : 2;
        public bool Away(int person) => trips[person] != null;
        public bool Busy(int person) => Away(person) || Requests.Exists(r => Array.IndexOf(r.Chain, person, r.Heard) >= 0);
        public bool HoldsRoad(Pt p)
        {
            foreach (var r in Requests)
                if (r.Stage == CitizenStage.Acting && (r.Act == CitizenAct.Block || r.Act == CitizenAct.Rally) &&
                    G.Within(r.Site, p, r.Act == CitizenAct.Block ? 650 : 1150)) return true;
            return false;
        }
        public bool StopsApproach(Pt was, Pt next)
        {
            foreach (var r in Requests)
            {
                if (r.Stage != CitizenStage.Acting || r.Act != CitizenAct.Block && r.Act != CitizenAct.Rally) continue;
                if (G.Within(r.Site, next, r.Act == CitizenAct.Block ? 900 : 1400) && G.D2(r.Site, next) < G.D2(r.Site, was)) return true;
            }
            return false;
        }
        public bool GuardOccupied => Requests.Exists(r => r.Act == CitizenAct.Guard && r.Stage == CitizenStage.Acting);

        // Shortest introductions through actual directed relationships. A person on another shift
        // can pass a voice message. The last person carries it out on the street.
        public IEnumerable<(int person, string kind)> Neighbours(int who)
        {
            foreach (var tie in run.Map.People[who].Ties) yield return tie;
            if (met.TryGetValue(who, out var group))
                foreach (int neighbour in group) yield return (neighbour, "street contact");
        }

        internal void Met(int a, int b)
        {
            Remember(a, b);
            Remember(b, a);
        }

        void Remember(int a, int b)
        {
            if (a == b || run.Map.People[a].Ties.Exists(t => t.person == b)) return;
            if (!met.TryGetValue(a, out var group)) met[a] = group = new SortedSet<int>();
            group.Add(b);
        }

        public List<int[]> Routes(int first, Func<int, bool> available)
        {
            var result = new List<int[]>();
            if (!available(first)) return result;
            var seen = new HashSet<int> { first };
            var queue = new Queue<int[]>();
            queue.Enqueue(new[] { first });
            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                result.Add(path);
                foreach (var tie in Neighbours(path[path.Length - 1]))
                {
                    if (!available(tie.person) || !seen.Add(tie.person)) continue;
                    var next = new int[path.Length + 1];
                    path.CopyTo(next, 0);
                    next[path.Length] = tie.person;
                    queue.Enqueue(next);
                }
            }
            return result;
        }

        public string Describe(IEnumerable<int> chain) => string.Join(" → ", chain.Select(i => run.Map.People[i].Name));

        internal int Send(int person, Pt from, Pt to, int tick)
        {
            var map = run.Map;
            var a = map.MeetingSite(from);
            var b = map.MeetingSite(to);
            var path = map.Walk.Route(a, b);
            if (path == null) return -1;
            var route = new Path(new[] { from }.Concat(path).Concat(new[] { b }).ToArray());
            run.Meet(person, from, tick);
            int arrival = tick + Math.Max(1, (int)((route.Length + WalkPace - 1) / WalkPace));
            if (trips[person] == null) outOnStreet++;
            trips[person] = new Trip { Path = route, Start = tick, Arrive = arrival };
            return arrival;
        }

        internal void Return(int person, int tick)
        {
            var trip = trips[person];
            if (trip != null && trip.Return < 0) trip.Return = tick;
        }

        internal void Disperse(int tick)
        {
            if (outOnStreet == 0) return;
            for (int i = 0; i < trips.Length; i++)
            {
                var t = trips[i];
                if (t == null || !Fallen[i] && (t.Return < 0 || t.Distance(tick) > 0)) continue;
                trips[i] = null;
                outOnStreet--;
                run.Release(i, tick);
            }
        }

        internal bool TryPose(int person, int tick, out (Pt at, Pt facing, bool moving) result)
        {
            var t = trips[person];
            result = default;
            if (t == null) return false;
            long mm = t.Distance(tick);
            var heading = t.Path.Heading(mm);
            bool back = t.Return >= 0 && tick >= t.Return;
            result = (t.Path.At(mm), back ? new Pt(-heading.X, -heading.Y) : heading, back ? mm > 0 : tick < t.Arrive);
            return true;
        }

        internal void Hash(Hash.H h)
        {
            if (Completed > 0) h.S("citizen-actions").I(Completed);
            foreach (var row in met)
            {
                h.S("introduced").I(row.Key);
                foreach (int friend in row.Value) h.I(friend);
            }
            foreach (var r in Requests)
            {
                h.S("request").I((int)r.Act).I((int)r.Stage).I(r.Heard).I(r.Due).I(r.Resolve).P(r.Site).S(r.Place).S(r.Update);
                foreach (var i in r.Chain) h.I(i);
                h.S("crowd");
                foreach (var i in r.Crowd) h.I(i);
            }
            for (int i = 0; i < trips.Length; i++)
            {
                if (AskedBy[i] != null || Fallen[i] || KnowsCell[i] || Wounds[i] > 0)
                    h.S("resident").I(i).S(AskedBy[i] ?? "").Z(KnowsCell[i]).Z(Fallen[i]).I(Wounds[i]);
                if (FellAt[i] is Pt at) h.P(at);
                var t = trips[i];
                if (t == null) continue;
                h.S("outing").I(i).I(t.Start).I(t.Arrive).I(t.Return);
                foreach (var p in t.Path.Points) h.P(p);
            }
        }
    }
}
