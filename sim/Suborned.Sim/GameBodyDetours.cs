// L9 Bodies, held (brief 098: "walkers step round or wait"; give and take, engine 0.47.0, Sky 2026-11-07: "Do all but the jumping back to spot
// should be better to dealt with"; nobody ever jumps). A body short of its goal that has not got on (still within two of its steps, or DetourHeldCm,
// of where it was held: it stood, or slid to and fro beside what holds it) is held, and it does what a person would, in this order:
//  · AskTicks held, and each AskTicks after while held: the one in its way now makes room, if that is a person of the city (GameBodyGive.cs);
//    with no room, two people of the city pass shoulder to shoulder (a squeeze). The armed, the cell and the tenets are never asked to move.
//  · DetourTicks held: two who hold each other a second (face to face in a gap one body wide: between Vale's car and the Office's facade, seen
//    2026-11-07, three held there a minute): the one with the lesser right of way, later in the law's order (the city's people before the armed,
//    then id), gives way: back GiveBackMm along the way it came (or straight away from the other), for GiveTicks, then on again. Else it walks
//    round: the walk grid's way from where it stands to its goal with the cells of the bodies near it shut that stand or come the other way
//    (those going its way it follows: a queue through a gap), followed as its own walk until it
//    gets to the end, its time runs out, or its own rules plan again. A way much longer than the straight line is not taken at first (it waits,
//    as in a queue at a door); each second it stays held allows DetourLongerCm more (a car parked across its walk is walked round, seen
//    2026-11-07: a way of 10 m round one, refused at twice the gap and 4 m, held a person 30 s while his own place walked 47 m on).
//  · bodies.replan got nowhere, whatever it tried: a person of the city on its routine changes plans (GameBodyGive.cs). Anyone else waits.
// And a body walled off from where it makes for (its own walk runs through a building, its own place jumped behind a wall) walks back to it by
// the walk grid at once (a Catch), however long the way: 0.46.0 put it back on its own place in one tick (the last resort and the lost body,
// counted BodyRejoins), which Sky ruled out. One try a second a body, and a few a tick. Deterministic: the law's order, the grid's own route.
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
        /// <summary>Ways round, back and changes of plans planned at most each tick (the rest try again next tick).</summary>
        const int DetourPlansPerTick = 4;
        /// <summary>A way round is taken at first up to twice the straight line and this (cm); each try refused allows this more.</summary>
        const int DetourLongerCm = 400;
        int detourPlans; bool[]? detourMask;
        /// <summary>Giving way: for this many ticks, back this far along the way it came (mm; cm straight away from the other without a walk).</summary>
        const int GiveTicks = 20; const long GiveBackMm = 2000; const int GiveBackCm = 200;
        /// <summary>A way back that found no route is not tried again for this many ticks.</summary>
        const int CatchRetryTicks = 10;

        /// <summary>Where a body on its way round aims now, or null when it is done (at the end, or out of time).</summary>
        Pt? DetourAim(ref Bd b, Pt from)
        {
            var p = b.Det; if (p == null) return null;
            if (Tick >= b.DetEnd || G.Within(from, p.End, DetourDoneCm)) { b.Det = null; b.Catch = false; return null; }
            return p.At(Math.Min(p.Length, p.NearestMm(from, 0, p.Length) + DetourAheadMm));
        }

        /// <summary>After a body is laid: short of its goal and not got on (still within DetourHeldCm, or two of its steps, of where it was held),
        /// the count goes up and it does what a person would (the file's head): asks for room, squeezes past, gives way, walks round, changes plans.</summary>
        void DetourAfter(int i, Pt from, Pt goal, int step)
        {
            ref var b = ref bds[i]; int held = step == int.MaxValue ? DetourHeldCm : Math.Max(DetourHeldCm, 2 * step); // a fast walker sliding to and fro moves farther
            if (b.GiveEnd > Tick || b.RoomFor != null) { b.HeldN = 0; b.HeldAt = b.At; return; } // giving way, or making room
            if (b.At == goal && b.Det == null) { b.HeldN = 0; b.StuckN = 0; b.DetTries = 0; b.HeldAt = b.At; return; } // there
            bool got = !G.Within(b.At, b.HeldAt, held);
            if (b.HeldN == 0 || got) { if (b.HeldN > 0 && got) { b.DetTries = 0; b.StuckN = 0; } b.HeldN = 1; b.HeldAt = b.At; return; } // held now; or it got on (the tries start again)
            ++b.StuckN; ++b.HeldN;
            if (b.HeldN >= AskTicks && b.HeldN % AskTicks == 0 && b.Blk >= 0 && AskRoom(i, b.Blk, from, goal)) return; // whoever is in its way now makes room, or the two squeeze past (GameBodyGive.cs): a little crowd parts one by one
            if (b.Blk >= 0 && bds[b.Blk].RoomFor == b.Id) return; // the one in its way is making room for it: it waits
            if (b.StuckN >= R.BodyReplanT && (b.StuckN - R.BodyReplanT) % ReplanRetryTicks == 0 && detourPlans < DetourPlansPerTick && Replan(i, from)) { b.StuckN = 0; b.HeldN = 0; return; } // nowhere still, whatever it tried: a person of the city changes plans (tried again a while on when nothing serves)
            if (b.HeldN < DetourTicks || detourPlans >= DetourPlansPerTick) return;
            b.HeldN = 0;
            if (b.Det != null) { b.Det = null; b.Catch = false; b.DetTries = Math.Min(b.DetTries + 1, 20); } // its way round got it nowhere: dropped, and tried again
            if (b.Blk >= 0) // held by one it holds: the lesser right of way gives way
            {
                ref var o = ref bds[b.Blk];
                if (o.HeldN > 0 && (o.Placed ? o.Blk == i : o.BlkId == b.Id) && o.Key < b.Key && GivePoint(in b, from, o.At) is Pt back) { b.GiveEnd = Tick + GiveTicks; b.GiveAt = back; return; }
            }
            if (!PlanDetour(i, from, goal)) b.DetTries = Math.Min(b.DetTries + 1, 20); // none, or far longer than the straight line: it waits a second more (a queue at a door)
        }

        /// <summary>The way round, from where it stands to `goal`, round the bodies near it: taken (Det) unless none, or far longer than the straight
        /// line for how long it has been held; with `any`, however long (a change of plans: its next stop by another way), and with `all` round every
        /// body near it, those going its way too (another way, not the jammed one).</summary>
        bool PlanDetour(int i, Pt from, Pt goal, bool any = false, bool all = false)
        {
            ref var b = ref bds[i]; detourPlans++;
            var way = WayRound(i, from, goal, all);
            if (way == null || (!any && way.Length > (2L * G.Dist(from, goal) + DetourLongerCm * (1L + b.DetTries)) * 10)) return false;
            b.Det = way; b.Catch = false; b.DetEnd = Tick + (int)(way.Length / Math.Max(1, peopleWalk)) + 2 * Hz; b.DetTries = 0; return true;
        }
        /// <summary>The walk grid's way from `from` to `goal` with the cells of the bodies within DetourNearCm of it shut that stand or come the other
        /// way (and the cars' ground, grown by a radius); those going its way it follows, so a queue still finds the gap it files through; null when
        /// there is none.</summary>
        Path? WayRound(int i, Pt from, Pt goal, bool all = false)
        {
            var grid = bodyWalk.Grid; int n = grid.Nx * grid.Ny;
            if (detourMask == null || detourMask.Length != n) detourMask = new bool[n];
            Array.Clear(detourMask, 0, n); foreach (var car in bodyCars) ShutCar(grid, car); // the law's own ground: a car's body grown by a radius (tried 2026-11-07: the routes' wider car margin first made no difference)
            int cx = FloorDiv(from.X, 100), cy = FloorDiv(from.Y, 100), r = DetourNearCm / 100; var dir = goal - from;
            for (int dx = -r; dx <= r; dx++) for (int dy = -r; dy <= r; dy++)
                if (bhHead.TryGetValue(CellKey(cx + dx, cy + dy), out var e))
                    for (; e >= 0; e = bhNext[e]) { int j = bhBody[e]; if (j == i) continue; ref var o = ref bds[j]; if (!all && o.Cls != BcFixed && o.Head != default && (long)o.Head.X * dir.X + (long)o.Head.Y * dir.Y > 0) continue; Shut(grid, o.At); } // those going its way are followed, not walked round (a queue through a gap); those standing or coming the other way are walked round
            int fi = grid.CI(from.X), fj = grid.CJ(from.Y); if (fi >= 0 && fj >= 0 && fi < grid.Nx && fj < grid.Ny) detourMask[fi * grid.Ny + fj] = false; // the way starts where it stands, not past whoever holds it
            var route = grid.Route(from, goal, detourMask);
            if (route == null && grid.NearestFree(goal, detourMask) is Pt near && G.Within(near, goal, R.BodySpaceCm)) route = grid.Route(from, near, detourMask);
            return route != null && route.Length >= 2 ? new Path(route) : null;
        }

        /// <summary>A body walled off from where it makes for walks back to it on the walk grid (round the standing cars), at once and however long the
        /// way: it never jumps there. No route: it tries again CatchRetryTicks on.</summary>
        void PlanCatch(int i, Pt from, Pt aim)
        {
            if (detourPlans >= DetourPlansPerTick) return;
            ref var b = ref bds[i]; detourPlans++;
            var to = bodyWalk.Free(aim) ? aim : bodyWalk.NearestFree(aim) ?? aim;
            var r = bodyWalk.Route(from, to);
            if (r == null || r.Length < 2) { b.DetEnd = Tick + CatchRetryTicks; return; }
            b.Det = new Path(r); b.Catch = true; b.DetEnd = Tick + (int)(b.Det.Length / Math.Max(1, WalkCmT * 10)) + 3 * Hz; b.DetTries = 0;
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
