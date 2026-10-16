// Pulse 013 scratch probe v2 (brief 013, witnesses): who on the r016 cast would find and report a body at a place, and how fast;
// report-point coverage; the cost of the discovery check. Reads the engine as it is, changes nothing. bun-free: dotnet run -c Release [bench|cover]
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Suborned.Sim;

static class P
{
    static string F(int cm) => (cm / 100.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    static Site S = null!; static Rules R = null!;
    static readonly Dictionary<(int, int, int, int), int> walkCache = new();
    // walking distance in cm along the grid between two points (A*), cached per cell pair
    static int Walk(Pt a, Pt b)
    {
        var fa = S.Grid.NearestFree(a) ?? a; var fb = S.Grid.NearestFree(b) ?? b;
        var key = (S.Grid.CI(fa.X), S.Grid.CJ(fa.Y), S.Grid.CI(fb.X), S.Grid.CJ(fb.Y));
        if (walkCache.TryGetValue(key, out var v)) return v;
        var r = S.Grid.Route(fa, fb); int len;
        if (r == null || r.Length < 2) len = G.Dist(a, b) * 2; else { long mm = new Suborned.Sim.Path(r).Length; len = (int)(mm / 10) + G.Dist(a, fa) + G.Dist(b, fb); }
        walkCache[key] = len; return len;
    }

    // the proposed reaction (Pulse 013 §2), a reference for the C#: what a person does on FINDING a body (no act seen), at an Alert.
    // returns the verb and the seconds until their report lands at the desk, given where they stand, the body and the report points.
    // verbs: report, check-report (go to the body first), quiet, avoid, radio
    record Reaction(string Verb, double LandS, string Via);
    const int WALK = 140, RUN = 260;
    static Reaction React(Person p, Pt me, Pt body, List<(string id, Pt at)> points, int alert)
    {
        int b = p.Sc[0], res = p.Sc[1], fear = p.Sc[2], loy = p.Sc[3];
        bool staff = p.Job == "guard" || p.Job == "patrol-officer" || p.Job == "watcher" || p.Job == "investigator" || p.Role == "guard";
        double react = 2.0;
        if (staff) return new Reaction("radio", react + 3.0, "staff radio");
        string v = p.Temperament;
        bool informer = res >= 2;
        string verb = informer ? "report" : v switch { "loyal" => "report", "idealist" => "check-report", "mercenary" => alert >= 2 ? "report" : "quiet", "fearful" => "avoid", _ => "quiet" };
        if (fear >= 3 && !informer) verb = "avoid";
        if (verb == "quiet" || verb == "avoid") return new Reaction(verb, double.PositiveInfinity, "");
        // report point: nearest by walking distance to a door/box (the report point's own pace: walk)
        string best = ""; int bd = int.MaxValue; Pt from = me;
        double t = react;
        if (verb == "check-report") { int d = Math.Max(0, G.Dist(me, body) - 300); t += d / (double)WALK + 2.0; from = body; }
        foreach (var (id, at) in points) { int d = Walk(from, at); if (d < bd) { bd = d; best = id; } }
        t += bd / (double)WALK + 6.0;
        return new Reaction(verb, t, best);
    }

    static int Main(string[] args)
    {
        var root = Engine.FindRoot(Directory.GetCurrentDirectory());
        var inp = Engine.Load(root);
        var g = Engine.New(inp, new Start());
        S = g.S; R = g.R;
        int eye = R.Eye, wr = R.WitnessRangeCm;
        var mode = args.Length > 0 ? args[0] : "tables";
        var doors = S.Refuges.Where(r => r.kind == "door").Select(r => (r.id, r.at)).ToList();
        Console.WriteLine("report points (doors): " + string.Join(", ", doors.Select(d => $"{d.id}@({F(d.at.X)},{F(d.at.Y)})")));
        // ---- walkable cells and coverage by walking distance to the nearest report point
        var samples = new List<Pt>();
        for (int x = S.Grid.X0 + 100; x < S.Grid.X0 + S.Grid.Nx * S.Grid.Cell; x += 400)
            for (int y = S.Grid.Y0 + 100; y < S.Grid.Y0 + S.Grid.Ny * S.Grid.Cell; y += 400) { var p = new Pt(x, y); if (S.Grid.Free(p)) samples.Add(p); }
        Console.WriteLine($"walkable samples (4 m): {samples.Count}");
        if (mode == "cover")
        {
            // coverage now (3 doors), then greedy call boxes until every sample is within 30 m of a point
            var pts = new List<(string id, Pt at)>(doors);
            int[] best = samples.Select(s => pts.Min(d => Walk(s, d.at))).ToArray();
            void Rep(string label)
            {
                var sorted = best.OrderBy(x => x).ToArray();
                int Pc(int p) => sorted[Math.Min(sorted.Length - 1, sorted.Length * p / 100)];
                Console.WriteLine($"{label}: median {F(Pc(50))} m, p90 {F(Pc(90))} m, max {F(sorted[^1])} m; within 25 m {100 * best.Count(x => x <= 2500) / best.Length}%, within 40 m {100 * best.Count(x => x <= 4000) / best.Length}%");
            }
            Rep("doors only");
            // candidate box cells: free samples within 2 m of a blocked cell (a kerb or a wall), 2 m grid
            var cands = new List<Pt>();
            foreach (var s in samples)
            {
                bool edge = false;
                foreach (var (dx, dy) in new[] { (150, 0), (-150, 0), (0, 150), (0, -150) }) if (!S.Grid.Free(new Pt(s.X + dx, s.Y + dy))) edge = true;
                if (edge) cands.Add(s);
            }
            Console.WriteLine($"box candidates (beside a wall or kerb): {cands.Count}");
            int added = 0;
            int TH = int.Parse(Environment.GetEnvironmentVariable("THRESH") ?? "3000");
            while (best.Max() > TH && added < 40)
            {
                // choose the candidate that brings the most uncovered samples within 30 m, ties on x then y
                Pt bc = default; int bn = -1;
                foreach (var c in cands)
                {
                    int n = 0; for (int i = 0; i < samples.Count; i++) if (best[i] > TH && Walk(samples[i], c) <= TH) n++;
                    if (n > bn || (n == bn && (c.X < bc.X || (c.X == bc.X && c.Y < bc.Y)))) { bn = n; bc = c; }
                }
                if (bn <= 0) break;
                pts.Add(($"box-{added + 1}", bc)); added++;
                for (int i = 0; i < samples.Count; i++) best[i] = Math.Min(best[i], Walk(samples[i], bc));
                Console.WriteLine($"  + box-{added} at ({F(bc.X)},{F(bc.Y)}) covers {bn} more; worst now {F(best.Max())} m");
            }
            Rep($"doors + {added} boxes");
            File.WriteAllText("boxes.txt", string.Join("\n", pts.Select(p => $"{p.id},{F(p.at.X)},{F(p.at.Y)}")));
            return 0;
        }
        if (mode == "resp")
        {
            var origins = new List<(string, Pt)> { ("security-entry", S.Stand(S.SecEntry)), ("rival-entry", S.Stand(S.RivalsEntry)), ("patrol-a", S.Stand(S.PatrolA)), ("patrol-b", S.Stand(S.PatrolB)), ("door:cafe-entrance", doors[0].at) };
            var targets = new List<(string, Pt)>();
            void T(string n, double xm, double ym) { var p = new Pt((int)(xm * 100), (int)(ym * 100)); targets.Add((n, S.Grid.NearestFree(p) ?? p)); }
            T("guard door", 51.75, -30); T("pickup bay", 78.1, -33.7); T("R3 lane middle", 63, -44); T("getaway bay", 8.5, -48.2); T("cafe door", -10.3, -29); T("official car bay", 79, -31);
            Console.WriteLine("walking distance m and time s at police 2.2 m/s | robot 0.85 m/s");
            foreach (var (on, op) in origins) { var sb2 = new StringBuilder($"{on,-20}"); foreach (var (tn, tp) in targets) { int d = Walk(op, tp); sb2.Append($" {tn}: {d / 100.0:0}m {d / 220.0:0}s/{d / 85.0:0}s;"); } Console.WriteLine(sb2); }
            return 0;
        }
        if (mode == "bench") return Bench(g);
        // ---- schedule of the street (1 Hz snapshots), then who finds a body where
        int horizon = 5100;
        var snaps = new List<(List<(Person p, Pt at)> everyone, Pt? guard, string guardPhase)>();
        var sites = new List<(string name, Pt at, string victim)>();
        void C(string n, double xm, double ym, string victim = "none") { var p = new Pt((int)(xm * 100), (int)(ym * 100)); sites.Add((n, S.Grid.NearestFree(p) ?? p, victim)); }
        C("guard-door", 51.75, -30.0, "guard"); C("pickup-driver", 78.1, -33.7, "driver");
        C("K1 cabinet-0 lane", 57.75, -43.75); C("K3 control cabinet", 23.9, -17.0); C("K6 bins Rocket Lane", 46.0, -61.8);
        C("R1 behind cafe", -23.4, -35.0); C("R2 lane north mouth", 49.0, -44.5); C("R3 lane middle", 63.0, -44.0); C("R4 lane south end", 74.0, -43.5);
        C("R5 office east side", 63.75, -18.75); C("R6 official tower pocket", 72.75, -34.75); C("R7 cafe east end", -18.75, -16.25);
        C("D1 office service door", 63.0, -41.1); C("D2 cafe kitchen door", -22.4, -26.0);
        var watch = new List<bool[]>(); // per site: per second: camera entrance, camera pickup, guard cone, patrol cone
        var ws = new List<(bool ce, bool cp, bool gu, bool pa)[]>();
        while (g.Tick < horizon && !g.Over)
        {
            if (g.Tick % 10 == 0)
            {
                var all = new List<(Person, Pt)>();
                foreach (var w in g.Walkers) { var at = g.CrowdPos(w); if (at.HasValue) all.Add((w.P, at.Value)); }
                foreach (var p in g.People) { var at = g.PersonPos(p); if (at.HasValue) all.Add((p, at.Value)); }
                snaps.Add((all, g.GuardPose()?.pos, g.GuardPose()?.phase ?? ""));
                var row = new (bool, bool, bool, bool)[sites.Count];
                for (int i = 0; i < sites.Count; i++) row[i] = (g.CameraWatching("entrance", sites[i].at, false), g.CameraWatching("pickup", sites[i].at, false), g.GuardWatching(sites[i].at, false), g.PatrolWatching(sites[i].at, false));
                ws.Add(row);
            }
            g.Step();
        }
        int N = snaps.Count;
        Console.WriteLine($"seconds {N}; fail {g.FailCode}; on the street by 60 s: " + string.Join(" ", Enumerable.Range(0, N / 60).Select(i => $"{i * 60}:{snaps[i * 60].everyone.Count}")));
        // report points for the table: the 3 doors (+ boxes from boxes.txt if present, with BOXES=1)
        var points = new List<(string id, Pt at)>(doors);
        if (Environment.GetEnvironmentVariable("BOXES") == "1" && File.Exists("boxes.txt"))
            foreach (var l in File.ReadAllLines("boxes.txt")) { var q = l.Split(','); if (q[0].StartsWith("box")) points.Add((q[0], new Pt((int)(double.Parse(q[1]) * 100), (int)(double.Parse(q[2]) * 100)))); }
        Console.WriteLine($"report points in use: {points.Count}");
        int screenCm = 400;
        Console.WriteLine("\nnearest report point by walking distance (doors | doors + 7 boxes)");
        var withBoxes = new List<(string id, Pt at)>(doors);
        if (File.Exists("boxes40.txt")) foreach (var l in File.ReadAllLines("boxes40.txt")) { var q = l.Split(','); if (q[0].StartsWith("box")) withBoxes.Add((q[0], new Pt((int)(double.Parse(q[1]) * 100), (int)(double.Parse(q[2]) * 100)))); }
        foreach (var (name, at, _) in sites)
        {
            (string id, int d) A(List<(string id, Pt at)> pts) { string bi = ""; int bd = int.MaxValue; foreach (var (id, pt) in pts) { int d = Walk(at, pt); if (d < bd) { bd = d; bi = id; } } return (bi, bd); }
            var a = A(points); var b = A(withBoxes);
            Console.WriteLine($"  {name,-26} {a.id} {F(a.d)} m | {b.id} {F(b.d)} m");
        }
        var drops = Enumerable.Range(0, 48).Select(i => 5 + i * 10).ToArray();
        bool loopCam = Environment.GetEnvironmentVariable("LOOP") == "1";
        Console.WriteLine($"scenario: entrance camera {(loopCam ? "LOOPED" : "live")}; report points {points.Count}; drops every 10 s from 5 s to 485 s; pre = drop before 5:00 (T<300), post = from 5:00 (the chime 5:30 empties the Office)");
        foreach (int range in new[] { wr, screenCm })
        {
            Console.WriteLine($"\n=== body seen from {F(range)} m (clear line) === P(first report lands within 30 s / 60 s / never in 240 s) and median landing (s after the drop) and who lands first");
            for (int si = 0; si < sites.Count; si++)
            {
                var (name, at, victim) = sites[si];
                var outp = new StringBuilder($"{name,-26}");
                foreach (var phase in new[] { "pre", "post" })
                {
                    var lands = new List<double>(); int n = 0, never = 0; var who = new Dictionary<string, int>(); var seenN = new List<int>();
                    foreach (int T in drops)
                    {
                        if ((phase == "pre") != (T < 300) || T >= N - 1) continue; n++;
                        double firstLand = double.PositiveInfinity; string lander = ""; var set = new HashSet<string>(); var done = new HashSet<string>();
                        for (int t = T; t < Math.Min(N, T + 240); t++)
                        {
                            foreach (var (p, pp) in snaps[t].everyone)
                            {
                                if (victim == "guard" && p.Role == "guard") continue;
                                if (victim == "driver" && p.Role == "driver") continue;
                                if (!(G.Within(pp, at, range) && S.Sight(pp, at, eye))) continue;
                                string key = p.Id + p.Role; if (!done.Add(key)) continue;
                                if (t < T + 60) set.Add(key);
                                var r = React(p, pp, at, points, 0);
                                double land = (t - T) + r.LandS;
                                if (land < firstLand) { firstLand = land; lander = r.Verb == "radio" ? "staff" : "civilian-" + r.Verb; }
                            }
                            var w = ws[t][si]; if (loopCam) w.ce = false;
                            if (w.ce) { double wait = (20 - (t % 20)) % 20; double l = (t - T) + wait + 3.0; if (l < firstLand) { firstLand = l; lander = "camera"; } }
                            if (w.cp) { double wait = (20 - ((t + 7) % 20)) % 20; double l = (t - T) + wait + 3.0; if (l < firstLand) { firstLand = l; lander = "camera"; } }
                            if (w.gu && victim != "guard") { double l = (t - T) + 5.0; if (l < firstLand) { firstLand = l; lander = "guard"; } }
                            if (w.pa) { double l = (t - T) + 4.0; if (l < firstLand) { firstLand = l; lander = "patrol"; } }
                        }
                        seenN.Add(set.Count);
                        if (double.IsPositiveInfinity(firstLand)) never++; else { lands.Add(firstLand); who[lander] = who.GetValueOrDefault(lander) + 1; }
                    }
                    if (n == 0) continue; lands.Sort();
                    int le30 = lands.Count(x => x <= 30), le60 = lands.Count(x => x <= 60);
                    string med = lands.Count == 0 ? "-" : $"{lands[lands.Count / 2]:0}";
                    string top = string.Join("/", who.OrderByDescending(kv => kv.Value).Take(3).Select(kv => $"{kv.Key}{100 * kv.Value / n}%"));
                    outp.Append($" | {phase} n={n}: <=30 {100 * le30 / n}% <=60 {100 * le60 / n}% never {100 * never / n}% med {med} s; people/60s med {seenN.OrderBy(x => x).ElementAt(seenN.Count / 2)} [{top}]");
                }
                Console.WriteLine(outp);
            }
        }
        // ---- who passes: unique people in range over the whole mission, by job and temperament, with first times
        foreach (int range in new[] { wr, screenCm })
        {
            Console.WriteLine($"\n=== who passes within {F(range)} m with a clear line, 0:00-8:30 (unique people; first time in range; verb at Alert 0) ===");
            for (int si = 0; si < sites.Count; si++)
            {
                var (name, at, victim) = sites[si];
                var first = new Dictionary<string, (int t, Person p, Pt pp)>();
                for (int t = 0; t < N; t++)
                    foreach (var (p, pp) in snaps[t].everyone)
                    {
                        if (victim == "guard" && p.Role == "guard") continue; if (victim == "driver" && p.Role == "driver") continue;
                        if (!(G.Within(pp, at, range) && S.Sight(pp, at, eye))) continue;
                        string key = p.Id + p.Role; if (!first.ContainsKey(key)) first[key] = (t, p, pp);
                    }
                var grp = first.Values.GroupBy(v => ((v.p.Role.Length > 0 ? v.p.Role : (v.p.Job.Length > 0 ? v.p.Job : "?")) + "/" + v.p.Temperament + "/" + React(v.p, v.pp, at, points, 0).Verb)).OrderBy(x => x.Key).ToList();
                var rep = first.Values.Select(v => (v.t, r: React(v.p, v.pp, at, points, 0), v.p)).Where(x => !double.IsPositiveInfinity(x.r.LandS)).OrderBy(x => x.t + x.r.LandS).FirstOrDefault();
                string fr = rep.p == null ? "none" : $"{rep.p.Id}{(rep.p.Role.Length > 0 ? "(" + rep.p.Role + ")" : "")} in range {rep.t / 60}:{rep.t % 60:00}, lands +{rep.r.LandS:0} s ({rep.r.Verb})";
                Console.WriteLine($"{name,-26} {first.Count,3} people | first reporter: {fr} | " + string.Join(", ", grp.Select(x => $"{x.Key}x{x.Count()}")));
            }
        }
        return 0;
    }

    // cost of the discovery check: for B bodies and N people, per tick, with a distance prefilter (integers) and a sight test only for those within range
    static int Bench(Game g)
    {
        int eye = R.Eye, wr = R.WitnessRangeCm;
        var rnd = new Random(7);
        var free = new List<Pt>();
        for (int x = S.Grid.X0 + 100; x < S.Grid.X0 + S.Grid.Nx * S.Grid.Cell; x += 100)
            for (int y = S.Grid.Y0 + 100; y < S.Grid.Y0 + S.Grid.Ny * S.Grid.Cell; y += 100) { var p = new Pt(x, y); if (S.Grid.Free(p)) free.Add(p); }
        Console.WriteLine($"free 1 m samples: {free.Count}");
        // the step cost, for scale: the whole engine tick with the crowd
        var sw = Stopwatch.StartNew(); int ticks = 0; while (g.Tick < 3300 && !g.Over) { g.Step(); ticks++; }
        Console.WriteLine($"engine Step with the 130-person crowd: {sw.Elapsed.TotalMilliseconds * 1000 / ticks:0.0} us/tick over {ticks} ticks (0:00-5:30)");
        foreach (int n in new[] { 130, 1300, 3000, 6000 })
            foreach (int bodies in new[] { 1, 4, 12 })
            {
                var people = new Pt[n]; for (int i = 0; i < n; i++) people[i] = free[rnd.Next(free.Count)];
                var body = new Pt[bodies]; for (int i = 0; i < bodies; i++) body[i] = free[rnd.Next(free.Count)];
                // (a) linear scan with the squared-distance prefilter; sight only inside 13 m
                int reps = 2000; long hits = 0, sights = 0;
                sw.Restart();
                for (int r = 0; r < reps; r++)
                    foreach (var b in body)
                        for (int i = 0; i < n; i++)
                            if (G.Within(people[i], b, wr)) { sights++; if (S.Sight(people[i], b, eye)) hits++; }
                double linear = sw.Elapsed.TotalMilliseconds * 1000 / reps;
                // (b) the same, bucket grid of 13 m cells (a person list per cell, rebuilt once a tick: counted in)
                int cs = wr; var bucket = new Dictionary<(int, int), List<int>>();
                sw.Restart();
                for (int r = 0; r < reps; r++)
                {
                    bucket.Clear();
                    for (int i = 0; i < n; i++) { var k = (people[i].X / cs, people[i].Y / cs); if (!bucket.TryGetValue(k, out var l)) bucket[k] = l = new List<int>(); l.Add(i); }
                    foreach (var b in body)
                    {
                        int bx = b.X / cs, by = b.Y / cs;
                        for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                            if (bucket.TryGetValue((bx + dx, by + dy), out var l)) foreach (int i in l) if (G.Within(people[i], b, wr)) { if (S.Sight(people[i], b, eye)) hits++; }
                    }
                }
                double bucketed = sw.Elapsed.TotalMilliseconds * 1000 / reps;
                Console.WriteLine($"people {n,5} bodies {bodies,2}: linear {linear,7:0.0} us/tick  (sight tests/tick {sights / (double)reps:0.0}) | bucketed {bucketed,7:0.0} us/tick  [hits/tick {hits / (double)reps / 2:0.0}]");
            }
        // one Sight test, alone
        var a2 = free[rnd.Next(free.Count)]; var b2 = free[rnd.Next(free.Count)];
        sw.Restart(); int c = 0; for (int i = 0; i < 200000; i++) if (S.Sight(a2, b2, eye)) c++;
        Console.WriteLine($"one Sight test ({S.Occluders.Count} occluders): {sw.Elapsed.TotalMilliseconds * 1000 / 200000 * 1000:0} ns");
        sw.Restart(); long len = 0; for (int i = 0; i < 300; i++) { var r = S.Grid.Route(S.Grid.NearestFree(free[rnd.Next(free.Count)]) ?? a2, S.Grid.NearestFree(free[rnd.Next(free.Count)]) ?? b2); if (r != null) len += r.Length; }
        Console.WriteLine($"one grid Route (A*): {sw.Elapsed.TotalMilliseconds * 1000 / 300:0} us");
        return 0;
    }
}
