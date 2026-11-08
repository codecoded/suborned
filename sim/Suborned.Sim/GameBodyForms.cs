// L9 Bodies, the placing (GameBodies.cs gathers and stores): one body laid where its own rules put it when that is clear, else at the first clear
// goal of a fixed list, moved to at the law's paces and never over its own (nobody jumps). And the armed groups' spread at their source (brief
// 098: "groups spread, with real tactics"): a crew spot round a place, bodies.crew apart, each facing its own sector, for the rival crew drawn to
// the Office front, the response unit at the end of its walk and police units at their destination.
//  · the goal: where it makes for (its own place, the lay easing back; or a change of plans); at a stop another holds, the first free spot of the
//    stop's form (a queue behind it at bodies.queue, then an arc round it; a guard at a post another guard holds, the nearest free crew spot in
//    sight of it, bodies.crew out, facing that spot's sector, his until he leaves); holding its lay; a walker's sidestep (right, then left);
//    rings round its own place, first by choice (personal space), then by need (two radii); a body new to the street in a press (a crowd coming
//    back out of one door) the nearest room by walk within twice bodies.search; making room for one it holds, giving way, a way round or back
//    (GameBodyGive.cs, GameBodyDetours.cs).
//  · the move: straight at the goal, at bodies.shove when it must make room now (it stands inside two radii of someone it must respect, or walks),
//    back at bodies.back to its own place, at the city's walk to a spot it chose, slower squeezing past; each never over the body's own pace
//    (BodyPace). Only a body new to the street is placed at its goal at once.
//  · if that step is not clear: hold where it stands, wait where it stood last tick (a walker), or the nearest clear place within its pace; if
//    none, it is laid at the step and counted (BodyOverlapsNow): the law's tests read that count as a failure.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        // 16 directions, per mille (cos, sin), from 0° in 22.5° steps: integer, the same on every machine
        static readonly int[] DirX = { 1000, 924, 707, 383, 0, -383, -707, -924, -1000, -924, -707, -383, 0, 383, 707, 924 };
        static readonly int[] DirY = { 0, 383, 707, 924, 1000, 924, 707, 383, 0, -383, -707, -924, -1000, -924, -707, -383 };
        static readonly int[] RingCm = { 15, 30, 45, 60, 80, 100, 130, 160, 200 };
        static readonly int[] SideCm = { 20, 40, 60, 80, 100 };
        /// <summary>A crew's spots round a post: (angle of the spot from the post's facing, the sector its holder faces), degrees; then, at twice the
        /// spread, the outer ring between them (so the way out to an outer spot passes between two inner ones, half a spread from each).</summary>
        static readonly (int at, int face)[] CrewSlots = { (60, 45), (-60, -45), (120, 90), (-120, -90), (180, 180) };
        static readonly (int at, int face)[] CrewSlots2 = { (30, 30), (-30, -30), (90, 90), (-90, -90), (150, 150), (-150, -150) };
        static readonly int[] QueueArc = { 90, -90, 135, -135 };
        /// <summary>A queue's spots are set this much past its step: a spot set at a spacing in integer cm falls a little short of it.</summary>
        const int RoundCm = 2;
        int tenetTick = -1; readonly List<int> crowded = new List<int>();
        /// <summary>The city's walk in cm a tick (crowd.walk is cm a second).</summary>
        int WalkCmT => Math.Max(1, peopleWalk / Hz);
        const byte WhyOwn = 1, WhyFollow = 2, WhyCatch = 3, WhyForm = 4, WhyHold = 5, WhySide = 6, WhyRing = 7, WhyRing2 = 8, WhyWide = 9, WhyNone = 10, WhyDetour = 11, WhyGive = 12, WhyRoom = 13, WhyAlt = 14;
        static readonly string[] WhyGoal = { "", "own", "follow", "catch", "form", "hold", "side", "ring", "ring2", "wide", "none", "detour", "give", "room", "alt" }, WhyMove = { "", ">next", ">slide", ">cur", ">wait", ">near", ">crowded" }, WhyFix = { "", ">room", ">gave-way", ">OVERLAP", ">off-ground" };
        /// <summary>A body's Fix: laid again by BodyRepair (room, gave way), still overlapping (counted), or stepping off ground no body stands on
        /// (a car's, or a solid cell its own place left it on).</summary>
        const byte FixOff = 4;
        /// <summary>A body more than this behind its own place follows its own walk, aiming FollowAheadMm along it past the point nearest it.</summary>
        const int FollowCm = 120; const long FollowAheadMm = 1500;
        /// <summary>A body this far from where it makes for with a wall between walks back to it by the walk grid, at once (GameBodyDetours.cs).</summary>
        const int CatchCm = 150;
        /// <summary>Engine 0.48.0 (Latch at people=128: x-person-0050 "shuffles 14 cm back and forth four times in 0.8 s"): a body the law steps
        /// aside keeps to that side for a moment, as long as a held body waits before it looks for a way round (GameBodySides.cs).</summary>
        const int SlideKeepTicks = DetourTicks;

        void LayOne(int i)
        {
            ref var b = ref bds[i];
            if (b.Cls == BcFixed && b.Fresh && b.WalksFirm && Blocker(i, b.Own, false) is int on && on >= 0 && bds[on].Cls != BcFixed) b.Cls = BcFirm; // one of the cast coming out where someone stands (Vale out of the Office door onto a guard at its post) steps out beside them, then walks to its place as a tenet walking does
            if (b.Cls == BcFixed)
            {
                b.At = b.Own; b.LayFace = default; b.Placed = true; b.HeldN = 0; b.StuckN = 0; // engine 0.48.0: a tenet at its place (a person seated) is never held (0.47 kept the count it sat down with)
                if (tenetTick != Tick && Blocker(i, b.At, false) is int t && t >= 0 && bds[t].Cls == BcFixed) { tenetTick = Tick; BodyTenetTicks++; } // two tenets inside two radii: the tenet wins
                HashAdd(i, b.At); if (b.Head != default) ClearAhead(i); return; // a tenet walking (the Enforcer leading Vale): the city ahead of it makes room before it comes (GameBodyGive.cs)
            }
            GiveStart(i); // GameBodyGive.cs: making room or a squeeze ends once the other has passed
            Pt cur = b.Raw + b.Lay, from = b.Prev, goal, gface = b.LayFace; bool form = false; byte why; // cur: holding its lay; from: where it stood; why: the goal and the move (BodyTrace reads them)
            bool behind = !b.AltOn && b.Path != null && !b.Fresh && G.D2(b.Raw, from) > (long)FollowCm * FollowCm; // fallen behind its own place: it follows its own walk, not a line through walls
            if (!b.Fresh && from != b.Raw && (!OffCars(from) || (!S.Grid.Free(from) && !b.SitIn && !(G.Within(from, b.Own, StepInCm) && !S.Grid.Free(b.Own))))) b.Fix = FixOff; // a car came to stand where it stood, or its own place left it on ground no body stands on (a door point in a facade): it walks off it (BodyGround); engine 0.49.0: not one stepping into its own place off the walk's ground across the last of it (BodyGround's step in: a customer held behind his walk into a café chair stepped back out of the table's clearance every third tick, drive-round #1 at people=32), nor an operative into his Listen's seat
            bool onCar = b.Fix == FixOff;
            b.Aim = behind ? FollowAim(in b, from) : b.Own;
            if (!b.Fresh && b.Det == null && Tick >= b.DetEnd && !G.Within(from, b.Aim, CatchCm) && !S.Grid.LineFree(from, b.Aim)) PlanCatch(i, from, b.Aim); // walled off from where it makes for (a routine leg through a building, its own place jumped behind a wall): it walks back by the walk grid (GameBodyDetours.cs)
            bool kept = false;
            if (!onCar && BodyGround(in b, b.Aim) && Blocker(i, b.Aim, true) < 0) { goal = b.Aim; gface = default; why = b.AltOn ? WhyAlt : behind ? WhyFollow : WhyOwn; }
            else if (KeptPick(i, out goal)) { kept = true; why = b.PickWhy; form = why == WhyForm; gface = default; } // engine 0.48.0: the spot the law picked last tick, while it stays clear (GameBodySides.cs)
            else if (!b.AltOn && FormGoal(i, out goal, out var ff)) { gface = ff; form = true; why = WhyForm; }
            else if (b.Lay != default && !b.AltOn && Fits(i, cur, true)) { goal = cur; why = WhyHold; }
            else if (b.Moving && SideGoal(i, cur, out goal)) why = WhySide;
            else if (RingGoal(i, true, out goal)) why = WhyRing;
            else if (RingGoal(i, false, out goal)) why = WhyRing2;
            else if ((b.Fresh || onCar) && bodyWalk.Nearest(onCar ? from : b.Raw, c => Fits(i, c, false), 2 * R.BodySearchCm) is Pt wide) { goal = wide; why = WhyWide; } // new on the street in a press (a crowd back out of one door), or standing where no body stands (a car came to stand on it): the nearest room by walk
            else { goal = cur; why = WhyNone; }
            if (form && b.Det != null && !b.Catch && G.Within(b.Det.End, b.Own, R.BodyMinCm) && !G.Within(goal, b.Own, R.BodyMinCm)) { b.Det = null; } // a spot of its stop's form to stand at (a queue's, a crew spot): a way round onto the stop itself, which another holds, is spent
            if (b.RoomFor != null) { goal = b.RoomAt; why = WhyRoom; form = false; } // making room for one it held (GameBodyGive.cs)
            else if (b.GiveEnd > Tick) { goal = b.GiveAt; why = WhyGive; form = false; } // giving way to one it held (GameBodyDetours.cs)
            else if (DetourAim(ref b, from) is Pt way) { goal = way; why = b.Catch ? WhyCatch : WhyDetour; form = false; } // on its way round what held it, or back to where it makes for
            PickAfter(i, why, goal, kept);
            bool urgent = !Fits(i, from, false); b.Urgent = urgent; // someone stands inside two radii of where it stood (a tenet walked on it), or a car stood on it: it steps clear now
            int rate = b.Fresh ? int.MaxValue : urgent || onCar || (why == WhyRoom && RoomForTenet(in b)) ? R.BodyShoveCmT : why == WhyDetour || why == WhyGive || why == WhyRoom ? WalkCmT : why == WhyCatch ? Math.Max(R.BodyBackCmT, WalkCmT) : b.Moving && goal != b.Aim ? R.BodyShoveCmT : behind && goal == b.Aim ? Math.Max(R.BodyBackCmT, WalkCmT) : goal == b.Raw ? R.BodyBackCmT : Math.Max(R.BodyBackCmT, WalkCmT); // behind, it hurries
            // engine 0.47.1 (Latch at people=128: one about to squeeze was slid at the city's run, once straight back; drawn, a dash): the law's own moves go no
            // faster than a walk (the city's), or the body's own step this tick when that is faster: a sidestep, making room (for a tenet too), giving way, a way
            // round or back, a stop's form, a ring round its own place, stepping off ground no body stands on, sliding round whoever is in its way. Only its own
            // way keeps the law's pace (its own place, catching it up, a change of plans' place), and stepping clear of one already inside two radii (urgent).
            int head = (int)G.Isqrt(G.D2(b.Head, default)), walk = Math.Min(b.Pace, Math.Max(WalkCmT, head));
            bool ownWay = !onCar && goal == b.Aim && (why == WhyOwn || why == WhyFollow || why == WhyAlt);
            int reach = b.Fresh ? int.MaxValue : b.SqzId != null ? Math.Min(b.Pace, R.BodySqueezeCmT) : Math.Min(urgent || ownWay ? b.Pace : walk, rate + head); // its own step this tick and the law's pace on top, never over its own pace (nobody jumps); squeezing past, slower
            int slide = b.Fresh || urgent ? reach : Math.Min(reach, walk); // round whoever is in its way: a step aside, at a walk
            int most = b.Fresh ? R.BodySearchCm : Math.Min(b.Pace, R.BodyShoveCmT); // the farthest it may step aside this tick when it must (where it stood is not clear: urgent)
            var next = Toward(from, goal, reach); Pt at; bool clear = Fits(i, next, false); if (!clear) b.Blk = Blocker(i, next, false); // who holds its step (GameBodyDetours.cs)
            if (clear) { at = next; why |= 1 << 4; }
            else if (Slide(i, from, goal, slide, out var sl) && (long)(sl.X - from.X) * b.SlideV.X + (long)(sl.Y - from.Y) * b.SlideV.Y >= 0) { at = sl; why |= 2 << 4; } // round whoever is in the way, toward where it is going; never back the other way while it keeps to the side it stepped to (no dancing, engine 0.48.0: a slide, a wait and a slide straight back was a shuffle; it waits, then asks, gives way or walks round)
            else if (cur != next && G.Within(cur, from, reach) && Fits(i, cur, false)) { at = cur; why |= 3 << 4; }
            else if (!b.Fresh && Fits(i, from, false)) { at = from; why |= 4 << 4; } // a walker waits a moment where it stood
            else if (NearClear(i, from, most, out var nc)) { at = nc; why |= 5 << 4; } // or steps aside from there
            else { at = LeastCrowded(i, from, next, cur, most); why |= 6 << 4; crowded.Add(i); } // none clear: where it overlaps least (BodyRepair tries again)
            b.Why = why; if (b.Fix != FixOff) b.Fix = 0;
            b.At = at; b.Placed = true; b.Reach = b.Fresh ? R.BodySearchCm : most; b.LayFace = form ? gface : at == b.Raw ? default : b.LayFace;
            HashAdd(i, at); DetourAfter(i, from, goal, reach);
        }
        /// <summary>Making room for a tenet walking (the Enforcer leading Vale): it would walk on whoever stays, so they step aside quickly.</summary>
        bool RoomForTenet(in Bd b) => b.RoomFor != null && bdIx.TryGetValue(b.RoomFor, out var a) && bds[a].Cls == BcFixed;
        bool Fits(int i, Pt c, bool comfort) => BodyGround(in bds[i], c) && Blocker(i, c, comfort) < 0;
        /// <summary>Where a body behind its own place aims: FollowAheadMm along its walk past the point nearest it, then on to the first point of the
        /// walk on free ground (a walk out of a door starts over the door's own ground), no farther than its own place.</summary>
        Pt FollowAim(in Bd b, Pt from)
        {
            var p = b.Path!; long lag = (long)G.Dist(b.Raw, from) * 10 + 3000;
            if (b.PathBack) // walked end to start: behind it lies at greater mm
            {
                long mb = Math.Max(b.PathMm, p.NearestMm(from, b.PathMm, b.PathMm + lag) - FollowAheadMm);
                while (mb > b.PathMm && !(S.Grid.Free(p.At(mb)) && OffCars(p.At(mb)))) mb = Math.Max(b.PathMm, mb - 500);
                return p.At(mb);
            }
            long mm = Math.Min(b.PathMm, p.NearestMm(from, b.PathMm - lag, b.PathMm) + FollowAheadMm);
            while (mm < b.PathMm && !(S.Grid.Free(p.At(mm)) && OffCars(p.At(mm)))) mm = Math.Min(b.PathMm, mm + 500);
            return p.At(mm);
        }
        /// <summary>After everyone is laid: a body still inside two radii of another looks again for room among everyone's final places, within its
        /// reach of where it stood; failing that, the one it overlaps (unless a tenet) does the same, giving way (it may step back where it stood).
        /// Twice over; what is left is counted (BodyOverlapsNow, BodyOverlapTicks).</summary>
        void BodyRepair()
        {
            if (crowded.Count == 0) return; // laid clear of everyone laid before it, and those after it clear of it: nobody overlaps
            for (int round = 0; round < 3; round++)
            {
                bool moved = false;
                foreach (var i in crowded)
                {
                    ref var b = ref bds[i];
                    int j = Blocker(i, b.At, false); if (j < 0) continue;
                    if (Room(i, out var c)) { b.At = c; HashAdd(i, c); b.Fix = 1; moved = true; continue; }
                    ref var o = ref bds[j]; if (o.Cls == BcFixed) continue;
                    if (Room(j, out var c2)) { o.At = c2; HashAdd(j, c2); o.Fix = 2; moved = true; }
                }
                if (!moved) break;
            }
            foreach (var i in crowded) if (Blocker(i, bds[i].At, false) >= 0) { BodyOverlapsNow++; BodyOverlapTicks++; bds[i].Fix = 3; }
        }
        /// <summary>The nearest place clear of every body, within its reach of where it stood: where it stood, then rings round where it stands now
        /// and round where it stood.</summary>
        bool Room(int i, out Pt at)
        {
            ref var b = ref bds[i]; at = default; int reach = b.Reach;
            if (b.At != b.Prev && !b.Fresh && Fits(i, b.Prev, false)) { at = b.Prev; return true; }
            foreach (var basis in new[] { b.At, b.Prev })
                foreach (var r in RingCm)
                {
                    if (r > reach) break;
                    for (int d = 0; d < 16; d++) { var c = basis + new Pt(DirX[d] * r / 1000, DirY[d] * r / 1000); if ((b.Fresh || G.Within(c, b.Prev, reach)) && Fits(i, c, false)) { at = c; return true; } }
                }
            return false;
        }
        /// <summary>No clear place within its reach: of where it stood, its step, its lay and the rings round where it stood, the one whose nearest
        /// body is least inside two radii (on its own ground).</summary>
        Pt LeastCrowded(int i, Pt from, Pt next, Pt cur, int reach)
        {
            Pt best = from; long bs = long.MinValue;
            void consider(Pt c) { if (!BodyGround(in bds[i], c) || !G.Within(c, from, reach == int.MaxValue ? int.MaxValue / 2 : reach)) return; long s = Clearance(i, c); if (s > bs) { bs = s; best = c; } }
            consider(from); consider(next); consider(cur);
            foreach (var r in RingCm) { if (r > reach) break; for (int d = 0; d < 16; d++) consider(from + new Pt(DirX[d] * r / 1000, DirY[d] * r / 1000)); }
            return best;
        }
        /// <summary>How far c stands from its nearest respected body beyond two radii (negative: inside), cm.</summary>
        long Clearance(int i, Pt c)
        {
            long best = long.MaxValue; int cx = FloorDiv(c.X, 100), cy = FloorDiv(c.Y, 100); ref var b = ref bds[i];
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                if (bhHead.TryGetValue(CellKey(cx + dx, cy + dy), out var e))
                    for (; e >= 0; e = bhNext[e]) { int j = bhBody[e]; if (j == i) continue; ref var o = ref bds[j]; long d = G.Isqrt(G.D2(c, o.At)) - Need(in b, in o, false); if (d < best) best = d; }
            return best;
        }
        /// <summary>A step from where it stood toward its goal, turned 22.5° at a time to either side (the nearer turn first, to 90°), as long as
        /// the step it can take: round a body in the way.</summary>
        bool Slide(int i, Pt from, Pt goal, int reach, out Pt at)
        {
            at = default; var v = goal - from; if (v == default || reach <= 0) return false;
            long len = Math.Min(reach == int.MaxValue ? long.MaxValue : reach, G.Isqrt(G.D2(v, default))); int d0 = DirNearest(v);
            for (int k = 1; k <= 4; k++)
                for (int s = 1; s >= -1; s -= 2) { int d = (d0 + s * k + 16) & 15; var c = from + new Pt((int)(DirX[d] * len / 1000), (int)(DirY[d] * len / 1000)); if (Fits(i, c, false)) { at = c; return true; } }
            return false;
        }
        /// <summary>a moved toward b by at most `rate` cm (integer; b when within it).</summary>
        static Pt Toward(Pt a, Pt b, int rate)
        {
            long d2 = G.D2(a, b); if (rate == int.MaxValue || d2 <= (long)rate * rate) return b;
            long d = G.Isqrt(d2); return new Pt(a.X + (int)((long)(b.X - a.X) * rate / d), a.Y + (int)((long)(b.Y - a.Y) * rate / d));
        }
        /// <summary>v turned by `deg` degrees (30, 45, 60, 90, 120, 135, 150, 180 and their negatives; integer per mille).</summary>
        internal static Pt Rot(Pt v, int deg)
        {
            int a = Math.Abs(deg), c = a == 0 ? 1000 : a == 30 ? 866 : a == 45 ? 707 : a == 60 ? 500 : a == 90 ? 0 : a == 120 ? -500 : a == 135 ? -707 : a == 150 ? -866 : a == 180 ? -1000 : throw new ArgumentException($"turn {deg}°"),
                s = (a == 0 || a == 180 ? 0 : a == 30 || a == 150 ? 500 : a == 45 || a == 135 ? 707 : a == 90 ? 1000 : 866) * Math.Sign(deg);
            return new Pt((int)(((long)v.X * c - (long)v.Y * s) / 1000), (int)(((long)v.X * s + (long)v.Y * c) / 1000));
        }
        /// <summary>v scaled to `len` cm (integer); (len, 0) for a zero vector.</summary>
        static Pt Scale(Pt v, int len) { long l = G.Isqrt(G.D2(v, default)); return l == 0 ? new Pt(len, 0) : new Pt((int)(v.X * (long)len / l), (int)(v.Y * (long)len / l)); }

        /// <summary>At a stop another holds: a queue behind it (bodies.queue a step, facing the way the stop faces), then an arc beside and behind it;
        /// a guard whose post another guard holds, the nearest free crew spot round it (bodies.crew, facing that spot's sector), then twice as far.
        /// Engine 0.49.0: a queue's spot is on the walk's free ground. Only a body's own way crosses the last of the solid ground into a place off it
        /// (BodyGround's step in), so one waiting for a chair never waits inside a table's clearance (drive-round #1 at people=32: x-person-0008
        /// waited 72 cm off cafe-table-3's seat, off the walk's ground, which held it there, and the man who had stood up from that seat could not
        /// pass it in the 83 cm gap between the table and the terrace's plinth for 179 s).</summary>
        bool FormGoal(int i, out Pt goal, out Pt face)
        {
            ref var b = ref bds[i]; goal = default; face = default;
            if (b.Form == FmNone || b.Moving) return false;
            var f = b.Face == default ? new Pt(100, 0) : b.Face;
            bool crew = b.Form == FmCrew && b.Mate; // a guard at a post another holds: a crew spot (BodyPosts)
            if (!crew) // anyone else at its place: a step aside (a queue behind a stop, else beside it), at a queue's step
            {
                bool queueFits(Pt c) => S.Grid.Free(c) && OffCars(c) && Fits(i, c, true);
                var back = Scale(new Pt(-f.X, -f.Y), R.BodyQueueCm + RoundCm);
                for (int k = 1; k <= 5; k++) { var c = b.Raw + new Pt(back.X * k, back.Y * k); if (queueFits(c)) { goal = c; return true; } }
                foreach (var a in QueueArc) { var c = b.Raw + Scale(Rot(f, a), R.BodyQueueCm + RoundCm); if (queueFits(c)) { goal = c; return true; } }
                return false;
            }
            long post = PostKey(b.Raw); slotsHeld.TryGetValue(post, out var held);
            if (b.Slot >= 0) // his spot, while it stays one (someone walking across it is no matter)
            {
                var c = CrewSlot(b.Raw, f, b.Slot);
                if (SlotOk(i, c) && !SlotTaken(i, c)) { goal = c; face = Rot(f, SlotFace(b.Slot)); return true; }
                slotsHeld[post] = held &= ~(1 << b.Slot); b.Slot = -1;
            }
            for (int ring = 0; ring < 2; ring++) // the nearest free spot no mate has, of the nearer ring with one ("the next takes the nearest free spot round it")
            {
                long best = long.MaxValue; int k = -1;
                for (int s = 0, n = ring == 0 ? CrewSlots.Length : CrewSlots2.Length; s < n; s++)
                {
                    int slot = ring * CrewSlots.Length + s; if ((held & (1 << slot)) != 0) continue;
                    var c = CrewSlot(b.Raw, f, slot); long d = G.D2(c, b.Prev);
                    if (d < best && SlotOk(i, c) && Fits(i, c, true)) { best = d; k = slot; goal = c; }
                }
                if (k < 0) continue;
                b.Slot = k; slotsHeld[post] = held | (1 << k); face = Rot(f, SlotFace(k)); return true;
            }
            return false; // none: he keeps personal space where there is room (the rings)
        }
        /// <summary>A crew spot is ground he can stand on in sight of the post (no wall between: a spot round a corner or behind a facade is none).</summary>
        bool SlotOk(int i, Pt c) => BodyGround(in bds[i], c) && S.Grid.LineFree(bds[i].Raw, c);
        /// <summary>Someone standing (not walking past) within two radii of a crew spot: it is theirs now.</summary>
        bool SlotTaken(int i, Pt c)
        {
            int cx = FloorDiv(c.X, 100), cy = FloorDiv(c.Y, 100); long two = (long)R.BodyMinCm * R.BodyMinCm;
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                if (bhHead.TryGetValue(CellKey(cx + dx, cy + dy), out var e))
                    for (; e >= 0; e = bhNext[e]) { int j = bhBody[e]; if (j != i && !bds[j].Moving && bds[j].Raw != bds[i].Raw && G.D2(c, bds[j].At) < two) return true; }
            return false;
        }
        /// <summary>A crew spot round a post facing f (0–4 the inner ring at bodies.crew, 5–10 the outer at twice it), and the sector its holder faces.</summary>
        Pt CrewSlot(Pt post, Pt f, int slot) => slot < CrewSlots.Length ? post + Scale(Rot(f, CrewSlots[slot].at), R.BodyCrewCm) : post + Scale(Rot(f, CrewSlots2[slot - CrewSlots.Length].at), 2 * R.BodyCrewCm);
        static int SlotFace(int slot) => slot < CrewSlots.Length ? CrewSlots[slot].face : CrewSlots2[slot - CrewSlots.Length].face;
        /// <summary>A walker's sidestep off the way it walks: right, then left, a little farther each time.</summary>
        bool SideGoal(int i, Pt cur, out Pt goal)
        {
            ref var b = ref bds[i]; var right = new Pt(b.Head.Y, -b.Head.X);
            for (int pass = 0; pass < 2; pass++)
                foreach (var d in SideCm)
                    for (int s = 1; s >= -1; s -= 2) { var c = cur + Scale(new Pt(right.X * s, right.Y * s), d); if (Fits(i, c, pass == 0)) { goal = c; return true; } }
            goal = default; return false;
        }
        /// <summary>Rings round its own place, nearest first, each starting from the way away from whoever stands nearest it.</summary>
        bool RingGoal(int i, bool comfort, out Pt goal)
        {
            ref var b = ref bds[i]; int j = Blocker(i, b.Aim, comfort); int d0 = j >= 0 ? DirNearest(b.Aim - bds[j].At) : (int)(Hash.Of(b.Id) & 15);
            foreach (var r in RingCm)
            {
                if (r > R.BodySearchCm) break;
                for (int k = 0; k < 16; k++) { int d = (d0 + ((k & 1) == 1 ? (k + 1) / 2 : 16 - k / 2)) & 15; var c = b.Aim + new Pt(DirX[d] * r / 1000, DirY[d] * r / 1000); if (Fits(i, c, comfort)) { goal = c; return true; } }
            }
            goal = default; return false;
        }
        /// <summary>The nearest clear place round where it stands, within `reach`.</summary>
        bool NearClear(int i, Pt cur, int reach, out Pt at)
        {
            ref var b = ref bds[i]; int j = Blocker(i, cur, false); int d0 = j >= 0 ? DirNearest(cur - bds[j].At) : 0;
            foreach (var r in RingCm)
            {
                if (r > reach) break;
                for (int k = 0; k < 16; k++) { int d = (d0 + ((k & 1) == 1 ? (k + 1) / 2 : 16 - k / 2)) & 15; var c = cur + new Pt(DirX[d] * r / 1000, DirY[d] * r / 1000); if (Fits(i, c, false)) { at = c; return true; } }
            }
            at = default; return false;
        }
        /// <summary>The index of the 16 directions nearest v.</summary>
        static int DirNearest(Pt v) { int best = 0; long bd = long.MinValue; for (int d = 0; d < 16; d++) { long dot = (long)v.X * DirX[d] + (long)v.Y * DirY[d]; if (dot > bd) { bd = dot; best = d; } } return best; }

        // ---------------------------------------------------------------- the armed spread at its source

        /// <summary>The k-th of an armed group's spots round a place facing `face` (k = 0 the place): bodies.crew off it at the crew slots' angles, the
        /// nearest free cell of the walk grid when that is a wall; with the law off, the place.</summary>
        internal Pt CrewSpot(Pt at, Pt face, int k)
        {
            if (!R.BodiesOn || k <= 0) return at;
            var (a, _) = CrewSlots[(k - 1) % CrewSlots.Length]; int ring = 1 + (k - 1) / CrewSlots.Length;
            var c = at + Scale(Rot(face == default ? new Pt(100, 0) : face, a), R.BodyCrewCm * ring);
            return Walk.Free(c) ? c : Walk.NearestFree(c) ?? at;
        }
        /// <summary>The k-th of an armed group's facings: the first along `face`, the next turned out to either side, then the flanks, then the rear.</summary>
        internal Pt SectorFace(Pt face, int k) => !R.BodiesOn || k <= 0 || face == default ? face : Rot(face, CrewSlots[(k - 1) % CrewSlots.Length].face);
    }
}
