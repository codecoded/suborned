// L9 Bodies, the way round (brief 098: "walkers step round or wait"): a body held DetourTicks running (short of its goal and still within two
// of its steps, or DetourHeldCm, of where it was held: it stood, or slid to and fro beside what holds it) walks round whatever holds it. Its step only turns up to
// 90° off its goal, so a wall on one side and a person seated or standing on the other can box it in (seen 2026-11-07 at guards=64: a guard
// 13 s sliding to and fro against the café's chairs while his own place walked on to the Office door). It takes the
// walk grid's way from where it stands to its goal with the cells of every body near it shut, and follows that as its own walk until it gets to
// the end, its time runs out, or its own rules plan again. A way much longer than the straight line is not taken at first (it waits, as in a
// queue at a door); each second it stays held allows DetourLongerCm more (a car parked across its walk is walked round, seen 2026-11-07: a
// way of 10 m round one, refused at twice the gap and 4 m, held a person 30 s while his own place walked 47 m on). One try a second a body,
// and a few a tick. Deterministic: the law's order, the grid's own route.
// Two who hold each other a second (face to face in a gap one body wide: between Vale's car and the Office's facade, seen 2026-11-07, three
// held there a minute): the one with the lesser right of way, later in the law's order (the city's people before the armed, then id), gives
// way: back GiveBackMm along the way it came (or straight away from the other), for GiveTicks, then on again. And a last resort: got nowhere
// BodyStuckTicks, whatever it tried, it rejoins its own place (counted: the law's evidence lists them).
using System;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Held this many ticks running (short of its goal and within two of its steps, or DetourHeldCm, of where the count began), a body looks for a way round.</summary>
        const int DetourTicks = 10, DetourHeldCm = 30;
        /// <summary>The bodies whose cells are shut for the way round: those within this of it (cm).</summary>
        const int DetourNearCm = 300;
        /// <summary>A body on its way round aims this far along it past the point nearest it (mm), and is done within DetourDoneCm of its end.</summary>
        const long DetourAheadMm = 700; const int DetourDoneCm = 30;
        /// <summary>Ways round planned at most each tick (the rest try again next tick).</summary>
        const int DetourPlansPerTick = 4;
        /// <summary>The law's last resort: a body that has got nowhere this many ticks, whatever it tried (gave way, a way round, waited), rejoins its own
        /// place as one new to the street (its own place, or the nearest room to it by walk; counted, BodyRejoins). Seen 2026-11-07: a narrow
        /// passage one body wide shut by someone posted in it (a rival lookout in a one-cell alley), a gap beside Vale's car.</summary>
        const int BodyStuckTicks = 100;
        /// <summary>A way round is taken at first up to twice the straight line and this (cm); each try refused allows this more.</summary>
        const int DetourLongerCm = 400;
        int detourPlans; bool[]? detourMask;
        /// <summary>Giving way: for this many ticks, back this far along the way it came (mm; cm straight away from the other without a walk).</summary>
        const int GiveTicks = 20; const long GiveBackMm = 2000; const int GiveBackCm = 200;

        /// <summary>Where a body on its way round aims now, or null when it is done (at the end, or out of time).</summary>
        Pt? DetourAim(ref Bd b, Pt from)
        {
            var p = b.Det; if (p == null) return null;
            if (Tick >= b.DetEnd || G.Within(from, p.End, DetourDoneCm)) { b.Det = null; return null; }
            return p.At(Math.Min(p.Length, p.NearestMm(from, 0, p.Length) + DetourAheadMm));
        }

        /// <summary>After a body is laid: short of its goal and not got on (still within DetourHeldCm, or two of its steps, of where it was held),
        /// the count goes up; DetourTicks running, it gives way to one it holds who holds it, or looks for a way round; BodyStuckTicks running,
        /// whatever it tried, the law's last resort.</summary>
        void DetourAfter(int i, Pt from, Pt goal, int step)
        {
            ref var b = ref bds[i]; int held = step == int.MaxValue ? DetourHeldCm : Math.Max(DetourHeldCm, 2 * step); // a fast walker sliding to and fro moves farther
            if (b.GiveEnd > Tick) { b.HeldN = 0; b.HeldAt = b.At; return; } // giving way
            if (b.At == goal && b.Det == null) { b.HeldN = 0; b.StuckN = 0; b.DetTries = 0; b.HeldAt = b.At; return; } // there
            bool got = !G.Within(b.At, b.HeldAt, held);
            if (b.HeldN == 0 || got) { if (b.HeldN > 0 && got) { b.DetTries = 0; b.StuckN = 0; } b.HeldN = 1; b.HeldAt = b.At; return; } // held now; or it got on (the tries start again)
            if (++b.StuckN >= BodyStuckTicks) { b.Rejoin = true; b.StuckN = 0; b.HeldN = 0; b.Det = null; BodyRejoins++; return; } // got nowhere this long, whatever it tried: the law's last resort
            if (++b.HeldN < DetourTicks || detourPlans >= DetourPlansPerTick) return;
            b.HeldN = 0;
            if (b.Det != null) { b.Det = null; b.DetTries = Math.Min(b.DetTries + 1, 20); } // its way round got it nowhere: dropped, and tried again
            if (b.Blk >= 0) // held by one it holds: the lesser right of way gives way
            {
                ref var o = ref bds[b.Blk];
                if (o.HeldN > 0 && (o.Placed ? o.Blk == i : o.BlkId == b.Id) && o.Key < b.Key && GivePoint(in b, from, o.At) is Pt back) { b.GiveEnd = Tick + GiveTicks; b.GiveAt = back; return; }
            }
            if (!PlanDetour(i, from, goal)) b.DetTries = Math.Min(b.DetTries + 1, 20); // none, or far longer than the straight line: it waits a second more (a queue at a door)
        }

        /// <summary>The way round, from where it stands to `goal`, round the bodies near it: taken (Det) unless none, or far longer than the straight
        /// line for how long it has been held.</summary>
        bool PlanDetour(int i, Pt from, Pt goal)
        {
            ref var b = ref bds[i]; detourPlans++;
            var grid = bodyWalk.Grid; int n = grid.Nx * grid.Ny;
            if (detourMask == null || detourMask.Length != n) detourMask = new bool[n];
            Array.Clear(detourMask, 0, n); foreach (var car in bodyCars) ShutCar(grid, car); // the law's own ground: a car's body grown by a radius (tried 2026-11-07: the routes' wider car margin first made no difference)
            int cx = FloorDiv(from.X, 100), cy = FloorDiv(from.Y, 100), r = DetourNearCm / 100;
            for (int dx = -r; dx <= r; dx++) for (int dy = -r; dy <= r; dy++)
                if (bhHead.TryGetValue(CellKey(cx + dx, cy + dy), out var e))
                    for (; e >= 0; e = bhNext[e]) { int j = bhBody[e]; if (j != i) Shut(grid, bds[j].At); }
            int fi = grid.CI(from.X), fj = grid.CJ(from.Y); if (fi >= 0 && fj >= 0 && fi < grid.Nx && fj < grid.Ny) detourMask[fi * grid.Ny + fj] = false; // the way starts where it stands, not past whoever holds it
            var route = grid.Route(from, goal, detourMask);
            if (route == null && grid.NearestFree(goal, detourMask) is Pt near && G.Within(near, goal, R.BodySpaceCm)) route = grid.Route(from, near, detourMask);
            var way = route != null && route.Length >= 2 ? new Path(route) : null;
            if (way == null || way.Length > (2L * G.Dist(from, goal) + DetourLongerCm * (1L + b.DetTries)) * 10) return false;
            b.Det = way; b.DetEnd = Tick + (int)(way.Length / Math.Max(1, peopleWalk)) + 2 * Hz; b.DetTries = 0; return true;
        }

        /// <summary>Where a body giving way backs to: GiveBackMm back along its own walk past the point nearest it, else GiveBackCm straight away
        /// from the one it holds; null when that is no ground to stand on (it looks for a way round instead).</summary>
        Pt? GivePoint(in Bd b, Pt from, Pt away)
        {
            if (b.Path is Path p)
            {
                long lag = (long)G.Dist(b.Raw, from) * 10 + 3000;
                var c = b.PathBack ? p.At(Math.Min(p.Length, p.NearestMm(from, b.PathMm, b.PathMm + lag) + GiveBackMm)) : p.At(Math.Max(0, p.NearestMm(from, b.PathMm - lag, b.PathMm) - GiveBackMm));
                if (BodyGround(in b, c)) return c;
            }
            var v = from - away; if (v == default) return null;
            var d = from + Scale(v, GiveBackCm); return BodyGround(in b, d) && S.Grid.LineFree(from, d) ? d : null;
        }
        /// <summary>Shuts, in the way-round mask, every cell whose centre a body could not stand on for this standing car.</summary>
        void ShutCar(Grid grid, in StandingCar car)
        {
            int reach = car.Hl(R) + car.Hw(R) + R.BodyRadiusCm; // a box round the car that holds its grown body at any heading
            int i0 = Math.Max(0, grid.CI(car.At.X - reach)), i1 = Math.Min(grid.Nx - 1, grid.CI(car.At.X + reach)), j0 = Math.Max(0, grid.CJ(car.At.Y - reach)), j1 = Math.Min(grid.Ny - 1, grid.CJ(car.At.Y + reach));
            for (int ci = i0; ci <= i1; ci++) for (int cj = j0; cj <= j1; cj++) if (OnCar(car, grid.Centre(ci, cj))) detourMask![ci * grid.Ny + cj] = true;
        }
        /// <summary>Shuts, in the way-round mask, every cell whose centre stands inside two radii of a body at c.</summary>
        void Shut(Grid grid, Pt c)
        {
            int m = R.BodyMinCm; long m2 = (long)m * m;
            int i0 = grid.CI(c.X - m), i1 = grid.CI(c.X + m), j0 = grid.CJ(c.Y - m), j1 = grid.CJ(c.Y + m);
            for (int ci = Math.Max(0, i0); ci <= Math.Min(grid.Nx - 1, i1); ci++)
                for (int cj = Math.Max(0, j0); cj <= Math.Min(grid.Ny - 1, j1); cj++)
                    if (G.D2(grid.Centre(ci, cj), c) < m2) detourMask![ci * grid.Ny + cj] = true;
        }
    }
}
