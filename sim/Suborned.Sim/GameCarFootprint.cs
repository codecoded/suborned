// A standing car is ground nobody walks through (engine 0.33.1; rulebook car.footprint; brief 041, Sky playing 0.2.122: "characters are
// walking through cars"). The walk grid is the site's and never changes (one grid serves every game); each game keeps a mask of the cells its
// standing cars cover, rebuilt only when a car stands somewhere new, starts or stops. Every walk the engine makes goes through Walk, the grid
// seen with that mask:
//  · a car blocks the cells whose centres lie within its footprint (car.footprint len along its heading, w across, centred on its point)
//    grown by car.footprint.clear; while it drives it blocks nothing;
//  · a walk that would end on a car's ground ends at the free cell nearest that point and steps in (getting into the car); a walk from inside
//    one starts at the free cell nearest (getting out); a move order to its ground ends beside it (GameOrders Move);
//  · "at the car" is within car.footprint.reach of its body (CarBodyCm), no longer at-reach-cm of its centre.
// The mask is derived from the cars' state, never hashed; the same orders give the same mask on every run, and a read by the host only
// rebuilds the same mask the engine would.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>The walk grid as one game sees it now: the site's grid less the cells its standing cars cover (rulebook car.footprint).
    /// The same calls as Grid; the cell maths (CI, Centre, …) are the grid's.</summary>
    public readonly struct WalkGrid
    {
        public readonly Grid Grid; public readonly bool[]? Mask; readonly Game? owner;
        public WalkGrid(Grid g, bool[]? m, Game? game = null) { Grid = g; Mask = m; owner = game; }
        public bool Free(Pt p) => Grid.Free(p, Mask);
        public bool FreeCell(int i, int j) => Grid.Open(i, j, Mask);
        /// <summary>True when p is free ground of the site but a standing car covers it.</summary>
        public bool CarCovers(Pt p) => Mask != null && Grid.Free(p) && !Grid.Free(p, Mask);
        public Pt? NearestFree(Pt p) => Grid.NearestFree(p, Mask);
        public Pt? NearestFreeAny(Pt p) => Grid.NearestFreeAny(p, Mask);
        public bool LineFree(Pt a, Pt b) => Grid.LineFree(a, b, Mask);
        public Pt? Nearest(Pt from, Func<Pt, bool> ok, int maxCm) => Grid.Nearest(from, ok, maxCm, Mask);
        /// <summary>The walk from a to b round the standing cars. b on a car's ground: the walk to its side (RouteBeside), then the step in to b.
        /// Null exactly where the grid's own Route is (b off free ground, or no way).</summary>
        public Pt[]? Route(Pt a, Pt b)
        {
            if (!CarCovers(b)) return Grid.Route(a, b, Mask);
            var r = RouteBeside(a, b); if (r == null) return Grid.NearestFree(b, Mask).HasValue ? null : Grid.Route(a, b); // no free cell within 6 m of it: as the grid alone walks it
            var l = new Pt[r.Length + 1]; Array.Copy(r, l, r.Length); l[r.Length] = b; return l;
        }
        /// <summary>The walk from a to the side of the car that covers b (Game.BesideRoute: the side a comes to first, off its ground and at the
        /// car); without a game, the walk to the free cell nearest b. Null when nothing free is near b or no walk reaches it.</summary>
        public Pt[]? RouteBeside(Pt a, Pt b)
        {
            if (owner != null && CarCovers(b)) return owner.BesideRoute(a, b, this);
            var side = Grid.NearestFree(b, Mask); return side.HasValue ? Grid.Route(a, side.Value, Mask) : null;
        }
        public int CI(int x) => Grid.CI(x);
        public int CJ(int y) => Grid.CJ(y);
        public Pt Centre(int i, int j) => Grid.Centre(i, j);
        public bool InGrid(Pt p) => Grid.InGrid(p);
    }

    public sealed partial class Game
    {
        /// <summary>A car standing on the street now (it blocks the walk): its id, its point and its heading (never (0, 0)).</summary>
        public readonly struct StandingCar { public readonly string Id; public readonly Pt At, Heading; public StandingCar(string id, Pt at, Pt h) { Id = id; At = at; Heading = h.X == 0 && h.Y == 0 ? new Pt(0, 100) : h; } }

        /// <summary>The cars standing now, in a fixed order (car:getaway, car:official, car:rivals, then the range's by id). A host read too.</summary>
        public List<StandingCar> StandingCars()
        {
            var l = new List<StandingCar>(3);
            if (Range) { foreach (var c in rangeCars) if (!c.Replaced) l.Add(new StandingCar(c.Id, S.At($"spawn:car:{c.Slot}"), CarNose)); return l; }
            if (getawayPath == null && CurStand != null) l.Add(new StandingCar("car:getaway", CurStand.CarAt, CurStand.Facing));
            if (OfficialStanding()) l.Add(new StandingCar("car:official", CarPos(), OfficialHeading()));
            if (!CarMoving && RivalCarPose() is var rp && rp.HasValue) l.Add(new StandingCar("car:rivals", rp.Value.pos, rp.Value.heading));
            return l;
        }
        /// <summary>The official car stands: at the pickup until it drives (the block move is retired, brief 059), parked round the back, or stopped.</summary>
        bool OfficialStanding()
        {
            if (carStoppedAt.HasValue) return true;
            if (valeDriveStart < 0 || carToService == null) return BlockMove == INF || Tick < BlockMove || (Tick >= BlockedFrom && Tick < BlockedUntil) || Tick >= BlockedUntil + blockMoveT;
            return Tick >= CarInBayTick;
        }
        /// <summary>The official car's heading, wherever it is: its bay's long axis at the pickup (site car.blockFacing, the official bay's kerb;
        /// brief 041: the host drew it across the bay, rear on the kerb by the door), along its drive while it drives, car.serviceFacing parked in
        /// the service bay, along the road at the drive's mouth, and the way it was going where it stopped.</summary>
        Pt OfficialHeading() => carStoppedAt.HasValue ? carStoppedHead : OfficialHeadingLive();
        Pt OfficialHeadingLive()
        {
            if (valeDriveStart < 0 || carToService == null) return S.CarBlockFacing;
            if (Tick >= CarInBayTick) return CarAtMouth ? carToService.Heading(CarDriveMm(Tick)) : S.CarServiceFacing;
            return carToService.Heading(CarDriveMm(Tick));
        }
        Pt carStoppedHead;
        /// <summary>The official car stops where it is (its driver down, or the car disabled), keeping its heading.</summary>
        void StopOfficial(Pt at) { carStoppedHead = OfficialHeadingLive(); carStoppedAt = at; }

        // ---------------------------------------------------------------- the mask

        bool[]? carMask; readonly List<int> carMaskCells = new List<int>(); long[] carMaskKey = Array.Empty<long>();
        /// <summary>The cells the standing cars cover now (index i·Ny + j), or null when no car stands. Rebuilt only when the standing cars move.</summary>
        bool[]? CarCells()
        {
            var cars = StandingCars();
            var key = new long[cars.Count * 4];
            for (int k = 0; k < cars.Count; k++) { var c = cars[k]; key[4 * k] = Array.IndexOf(CarIds, c.Id) + 1 + (c.Id.Length << 8); key[4 * k + 1] = c.At.X; key[4 * k + 2] = c.At.Y; key[4 * k + 3] = (long)c.Heading.X * 4_000_037L + c.Heading.Y; }
            if (carMask != null && SameKey(key, carMaskKey)) return carMaskCells.Count == 0 ? null : carMask;
            var g = S.Grid; carMask ??= new bool[g.Nx * g.Ny];
            foreach (var k in carMaskCells) carMask[k] = false;
            carMaskCells.Clear(); carMaskKey = key;
            foreach (var c in cars) Cover(c, R.CarClearCm, (i, j) => { int k = i * g.Ny + j; if (!carMask[k] && g.FreeCell(i, j)) { carMask[k] = true; carMaskCells.Add(k); } });
            return carMaskCells.Count == 0 ? null : carMask;
        }
        static bool SameKey(long[] a, long[] b) { if (a.Length != b.Length) return false; for (int k = 0; k < a.Length; k++) if (a[k] != b[k]) return false; return true; }

        void Cover(StandingCar c, int grow, Action<int, int> cell) => CoverCells(S.Grid, R, c.At, c.Heading, grow, cell);
        /// <summary>Each grid cell whose centre lies within a car's footprint at `at`, heading `h`, grown by `grow` cm (exact integer test on the centre).</summary>
        public static void CoverCells(Grid g, Rules r, Pt at, Pt h, int grow, Action<int, int> cell)
        {
            if (h.X == 0 && h.Y == 0) h = new Pt(0, 100);
            long hl = r.CarLenCm / 2 + grow, hw = r.CarWCm / 2 + grow;
            long hx = h.X, hy = h.Y, n2 = hx * hx + hy * hy; // |h|²; the tests compare squares, so no root
            int reach = (int)Math.Min(int.MaxValue, hl + hw);
            int i0 = g.CI(at.X - reach), i1 = g.CI(at.X + reach), j0 = g.CJ(at.Y - reach), j1 = g.CJ(at.Y + reach);
            for (int i = Math.Max(0, i0); i <= Math.Min(g.Nx - 1, i1); i++)
                for (int j = Math.Max(0, j0); j <= Math.Min(g.Ny - 1, j1); j++)
                {
                    var p = g.Centre(i, j); long dx = p.X - at.X, dy = p.Y - at.Y;
                    long along = dx * hx + dy * hy, across = dx * hy - dy * hx; // × |h|
                    if (along * along <= hl * hl * n2 && across * across <= hw * hw * n2) cell(i, j);
                }
        }
        /// <summary>A mask (Grid.Open) of the cells the given cars cover, grown by car.footprint.clear; null for none.</summary>
        public static bool[]? MaskOf(Grid g, Rules r, IEnumerable<(Pt at, Pt heading)> cars)
        {
            bool[]? m = null;
            foreach (var (at, h) in cars) CoverCells(g, r, at, h, r.CarClearCm, (i, j) => { if (g.FreeCell(i, j)) (m ??= new bool[g.Nx * g.Ny])[i * g.Ny + j] = true; });
            return m;
        }
        /// <summary>The cars standing when the Handover starts (the getaway car at its first stand, the official car at the pickup, along its bay),
        /// as a mask: the crowd's routine legs, made once at load, go round them (rulebook car.footprint). Null on a site with no stands (the range).</summary>
        public static bool[]? HomeCars(Site s, Rules r)
        {
            if (s.IsRange || s.Stands.Count == 0) return null;
            return MaskOf(s.Grid, r, new[] { (s.Stands[0].CarAt, s.Stands[0].Facing), (s.CarBlockPath[0], s.CarBlockFacing) });
        }

        /// <summary>The walk grid as this game sees it now (rulebook car.footprint): every walk the engine makes goes through it. A host read too
        /// (a click on a car's ground: Walk.Free is false there).</summary>
        public WalkGrid Walk => new WalkGrid(S.Grid, CarCells(), this);

        // ---------------------------------------------------------------- the cars' epochs (the crowd's legs)

        /// <summary>When the standing cars changed, read once a tick at its start (CarEpochStep): from each tick on, the cars' mask (a copy), and
        /// whether they are the mission's start cars (the crowd's legs built at load go round exactly those). Derived from the cars' state at a
        /// fixed point of the step, so a host read never moves it; never hashed.</summary>
        readonly List<(int tick, int id, bool home, long[] key, bool[]? mask)> carEpochs = new List<(int, int, bool, long[], bool[]?)>();
        long[] KeyOf(List<StandingCar> cars)
        {
            var key = new long[cars.Count * 3];
            for (int k = 0; k < cars.Count; k++) { var c = cars[k]; key[3 * k] = c.At.X; key[3 * k + 1] = c.At.Y; key[3 * k + 2] = (long)c.Heading.X * 4_000_037L + c.Heading.Y; }
            return key;
        }
        long[] HomeKey() => S.IsRange || S.Stands.Count == 0 ? Array.Empty<long>() : KeyOf(new List<StandingCar> { new StandingCar("car:getaway", S.Stands[0].CarAt, S.Stands[0].Facing), new StandingCar("car:official", S.CarBlockPath[0], S.CarBlockFacing) });
        /// <summary>At the start of each tick (and at the game's start): a new epoch when the standing cars differ from the last one's.</summary>
        void CarEpochStep()
        {
            if (Range || People2 == null) return;
            var cars = StandingCars(); var key = KeyOf(cars);
            if (carEpochs.Count > 0 && SameKey(key, carEpochs[carEpochs.Count - 1].key)) return;
            var list = new List<(Pt, Pt)>(cars.Count); foreach (var c in cars) list.Add((c.At, c.Heading));
            carEpochs.Add((Tick, carEpochs.Count, SameKey(key, HomeKey()), key, MaskOf(S.Grid, R, list)));
        }
        /// <summary>The cars' epoch in force at `tick` (the last one begun by then; before the first, or past the last read, the nearest).</summary>
        (int id, bool home, bool[]? mask) CarEpochAt(int tick)
        {
            if (carEpochs.Count == 0) return (0, true, null);
            for (int k = carEpochs.Count - 1; k > 0; k--) if (carEpochs[k].tick <= tick) return (carEpochs[k].id, carEpochs[k].home, carEpochs[k].mask);
            return (carEpochs[0].id, carEpochs[0].home, carEpochs[0].mask);
        }

        /// <summary>The walk from a to the car that covers b, ending beside it (rulebook car.footprint): of the free cell nearest b and the points
        /// off the car's sides (its doors), and off its ends only for a b by an end, level with b (b's place along and across the car, pushed out past the clearance by a quarter cell,
        /// each on the free cell centre nearest its cell's), those no more than 1.5 m further from b than the nearest is, the one with the shortest walk from a
        /// (ties: the nearer b, then the earlier in that order). So a walker coming from the road stops on the road side, one coming to a point by its
        /// tail stops at its tail; a side's point is within car.footprint.reach of the body. Null when no walk reaches any of them.</summary>
        internal Pt[]? BesideRoute(Pt a, Pt b, WalkGrid w)
        {
            StandingCar? on = null;
            foreach (var c in StandingCars()) { bool hit = false; CoverCells(S.Grid, R, c.At, c.Heading, R.CarClearCm, (i, j) => { if (!hit && S.Grid.CI(b.X) == i && S.Grid.CJ(b.Y) == j) hit = true; }); if (hit) { on = c; break; } }
            var near = w.Grid.NearestFree(b, w.Mask); if (!near.HasValue) return null;
            var cands = new List<Pt> { near.Value };
            if (on.HasValue)
            {
                var c = on.Value; long hx = c.Heading.X, hy = c.Heading.Y, n = G.Isqrt(hx * hx + hy * hy); if (n == 0) { hx = 0; hy = 100; n = 100; }
                long dx = b.X - c.At.X, dy = b.Y - c.At.Y, along = (dx * hx + dy * hy) / n, across = (dx * hy - dy * hx) / n;
                long outL = R.CarLenCm / 2 + R.CarClearCm + S.Grid.Cell / 4, outW = R.CarWCm / 2 + R.CarClearCm + S.Grid.Cell / 4;
                Pt Local(long u, long v) => new Pt(c.At.X + (int)((u * hx + v * hy) / n), c.At.Y + (int)((u * hy - v * hx) / n)); // along u, across v (the heading turned clockwise: +v is its right)
                var faces = Math.Abs(along) > R.CarLenCm / 2 - R.CarClearCm ? new[] { Local(along, outW), Local(along, -outW), Local(outL, across), Local(-outL, across) } : new[] { Local(along, outW), Local(along, -outW) }; // its doors are on its sides: an end only for a point by that end
                foreach (var q in faces)
                    if (w.Grid.NearestFree(w.Grid.Centre(w.Grid.CI(q.X), w.Grid.CJ(q.Y)), w.Mask) is Pt f && !cands.Contains(f)) cands.Add(f); // a cell centre, as every walk's end
            }
            long limit = G.Dist(near.Value, b) + 150; Pt[]? best = null; long bestLen = long.MaxValue, bestD = long.MaxValue;
            foreach (var f in cands)
            {
                long d = G.Dist(f, b); if (d > limit) continue;
                var r = w.Grid.Route(a, f, w.Mask); if (r == null) continue;
                long len = new Path(r).Length;
                if (len < bestLen || (len == bestLen && d < bestD)) { best = r; bestLen = len; bestD = d; }
            }
            return best;
        }

        // ---------------------------------------------------------------- at the car

        /// <summary>How far p is from the car's body (cm; 0 on or inside it): the footprint, not grown. -1 when that car is not standing.</summary>
        public int CarBodyCm(string id, Pt p)
        {
            foreach (var c in StandingCars()) if (c.Id == id) return BodyCm(c, p);
            return -1;
        }
        int BodyCm(StandingCar c, Pt p)
        {
            long hx = c.Heading.X, hy = c.Heading.Y, n = G.Isqrt(hx * hx + hy * hy); if (n == 0) n = 1;
            long dx = p.X - c.At.X, dy = p.Y - c.At.Y;
            long along = Math.Abs(dx * hx + dy * hy) / n, across = Math.Abs(dx * hy - dy * hx) / n; // cm
            long ox = Math.Max(0, along - R.CarLenCm / 2), oy = Math.Max(0, across - R.CarWCm / 2);
            return (int)G.Isqrt(ox * ox + oy * oy);
        }
        /// <summary>At the getaway car (rulebook car.footprint.reach): within reach of its body where it stands; while it drives, never. Board's
        /// own test (stand-at-getaway), and a rival's at the car; a host and test read.</summary>
        public bool AtGetaway(Pt p)
        {
            if (Range) return G.Within(p, Car, R.AtReachCm); // the range has no getaway car: its mark (site getaway.car) is a point, at-reach-cm of it as before
            if (getawayPath != null) return false;
            int d = CarBodyCm("car:getaway", p); return d >= 0 && d <= R.CarReachCm;
        }
    }
}
