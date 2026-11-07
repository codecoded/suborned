// L9 Bodies, give and take (brief 098; Sky 2026-11-07, on 0.46.0's last resort, a body held ten seconds put back on its own place in one tick:
// "Do all but the jumping back to spot should be better to dealt with"; a stuck person resolves it as a person would, never with a hidden fix;
// engine 0.47.0). What a held body does in place of jumping (GameBodyDetours.cs says when):
//  · the one in the way makes room: a person of the city in a held body's way (standing, waiting, or coming the other way; not going its own way
//    ahead of it, not queueing at the stop it waits for) steps off its line to the nearest free spot, at the city's walk, and steps back once it
//    has passed (two radii beyond the spot it stood on, along its way), has left the street, or RoomTicks are up. Counted: BodySidesteps.
//  · squeezing past: with no room to make, two people of the city in one another's way pass shoulder to shoulder, turned sideways: between the
//    two only bodies.squeeze each (not a radius), everyone else still two radii off; at bodies.squeezePace; until they have passed and stand two
//    radii apart. Counted: BodySqueezes; the host reads who squeezes with whom and the way each faces (BodySqueeze, GameBodyReads.cs).
//  · changing plans: a person of the city who still gets nowhere (bodies.replan, whatever it tried) drops that step of its errand: on its
//    routine, at a chair or a stop it cannot reach, another free one that serves (another chair; a free spot near the stop), and on its way, its
//    next stop by another way, or the one after (it keeps to that until the step it replaced ends); running for shelter or at a packed door,
//    another refuge it can get to, or its own another way; on its way back from shelter, another way. Counted: BodyReplans, by what it took.
// The tenets (the cast, anyone seated, an operative at work), the armed (the cell, the rival crew, guards, officers, police, robots) are never
// asked to move and never change plans: an operative steps round people or waits. Ties go by the law's fixed order; integer cm, fixed lists.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>Held this many ticks at someone, a body asks them to make room (or, with no room, the two squeeze past).</summary>
        const int AskTicks = 3;
        /// <summary>The most a person stands aside for one (ticks).</summary>
        const int RoomTicks = 60;
        /// <summary>Making room: off the held one's way this far (cm), nearest first, straight off it, then RoomSlantCm back, then on; and at least
        /// two radii and RoomClearCm off its line.</summary>
        static readonly int[] RoomCm = { 30, 45, 60, 80, 100, 130, 160 };
        const int RoomSlantCm = 30, RoomClearCm = 10;
        /// <summary>A squeeze tries this long to pass (ticks); after it the two keep the narrower width only until they stand two radii apart.</summary>
        const int SqueezeTicks = 80;
        /// <summary>A change of plans: another chair within ReplanChairCm of where it stands; a free spot within ReplanSpotCm of its stop, the nearest
        /// by walk from where it stands (within ReplanSearchCm); a change of plans that found nothing is tried again ReplanRetryTicks on.</summary>
        const int ReplanChairCm = 1500, ReplanSpotCm = 500, ReplanSearchCm = 800, ReplanRetryTicks = 20;

        /// <summary>At the start of a body's lay: making room ends once the one it made room for has passed, has left the street, or the time is up;
        /// a squeeze ends once the two have passed each other (or the time is up) and stand two radii apart, or one has left.</summary>
        void GiveStart(int i)
        {
            ref var b = ref bds[i];
            if (b.RoomFor != null && (Tick >= b.RoomEnd || !bdIx.TryGetValue(b.RoomFor, out var a) || Past(bds[a].At, b.RoomFrom, b.RoomDir, R.BodyMinCm))) b.RoomFor = null;
            if (b.SqzId == null) return;
            if (!bdIx.TryGetValue(b.SqzId, out var p) || bds[p].SqzId != b.Id) { b.SqzId = null; return; } // the other has left the street, or squeezes no more
            ref var o = ref bds[p];
            if (!G.Within(b.Prev, o.At, R.BodyMinCm) && (Past(b.Prev, o.At, b.SqzDir, 0) || Tick >= b.SqzEnd)) { b.SqzId = null; o.SqzId = null; } // passed, two radii apart
        }
        /// <summary>Squeezing past someone and still trying (a squeeze past its time keeps only the narrower width until the two stand apart).</summary>
        bool Squeezing(in Bd b) => b.SqzId != null && Tick < b.SqzEnd;
        /// <summary>Whether `at` stands more than `by` beyond `spot` along `dir`.</summary>
        static bool Past(Pt at, Pt spot, Pt dir, int by) { long len = G.Isqrt(G.D2(dir, default)); return len > 0 && (long)(at.X - spot.X) * dir.X + (long)(at.Y - spot.Y) * dir.Y > by * len; }

        /// <summary>Body i is held by j: j makes room if it is a person of the city who can (the file's head); with no room, two people of the city
        /// squeeze past. False when neither (it waits, gives way or walks round).</summary>
        bool AskRoom(int i, int j, Pt from, Pt goal)
        {
            ref var b = ref bds[i]; ref var o = ref bds[j];
            if (b.RoomFor != null || Squeezing(in b) || o.Cls != BcSoft || o.Armed || o.Fresh || o.RoomFor != null || o.SqzId != null || o.GiveEnd > Tick) return false; // the armed and the tenets keep their places; one already giving way is busy
            if (o.Stop != null && o.Stop == b.Stop) return false; // a queue at one stop: it waits its turn
            var dir = goal - from; if (dir == default) return false;
            var way = b.Head != default ? b.Head : dir; // the way it is going (its own place's step), else the way to its goal
            if (o.Head != default && (long)o.Head.X * way.X + (long)o.Head.Y * way.Y > 0) return false; // going its own way ahead of it: it follows
            if (RoomSpot(j, from, dir) is Pt spot) { o.RoomFor = b.Id; o.RoomAt = spot; o.RoomFrom = o.At; o.RoomDir = dir; o.RoomEnd = Tick + RoomTicks; BodySidesteps++; return true; }
            if (b.Cls != BcSoft || b.Armed || b.SqzId != null || G.Within(o.Own, b.Own, R.BodySpaceCm)) return false; // the cell and the armed never squeeze (they wait or walk round); two making for one spot are a queue, not a pass
            if ((long)(o.At.X - from.X) * way.X + (long)(o.At.Y - from.Y) * way.Y <= 0) return false; // only one in its way ahead, coming toward it or standing: a squeeze is a pass
            b.SqzId = o.Id; o.SqzId = b.Id; b.SqzEnd = o.SqzEnd = Tick + SqueezeTicks; b.SqzDir = dir; o.SqzDir = o.Head != default ? o.Head : new Pt(-dir.X, -dir.Y); BodySqueezes++;
            return true;
        }
        /// <summary>A tenet walking (the Enforcer leading Vale; never held, never moved): each person of the city standing or walking within AheadCm
        /// ahead of it, inside two radii and RoomClearCm of its way, makes room now, before it comes (they would otherwise be walked into).</summary>
        void ClearAhead(int i)
        {
            ref var b = ref bds[i]; var dir = b.Head; long len = G.Isqrt(G.D2(dir, default)); if (len == 0) return;
            int cx = FloorDiv(b.At.X, 100), cy = FloorDiv(b.At.Y, 100), r = (AheadCm + 99) / 100;
            for (int dx = -r; dx <= r; dx++) for (int dy = -r; dy <= r; dy++)
                if (bhHead.TryGetValue(CellKey(cx + dx, cy + dy), out var e))
                    for (; e >= 0; e = bhNext[e])
                    {
                        int j = bhBody[e]; if (j == i) continue; ref var o = ref bds[j];
                        if (o.Placed || o.Cls != BcSoft || o.Armed || o.Fresh || o.RoomFor != null || o.SqzId != null) continue; // laid already, or one who keeps its place, or busy
                        var v = o.At - b.At; long along = ((long)v.X * dir.X + (long)v.Y * dir.Y) / len, across = Math.Abs((long)v.X * dir.Y - (long)v.Y * dir.X) / len;
                        if (along <= 0 || along > AheadCm || across >= R.BodyMinCm + RoomClearCm) continue;
                        if (RoomSpot(j, b.At, dir) is Pt spot) { o.RoomFor = b.Id; o.RoomAt = spot; o.RoomFrom = o.At; o.RoomDir = dir; o.RoomEnd = Tick + RoomTicks; BodySidesteps++; }
                    }
        }
        /// <summary>How far ahead of a tenet walking the city makes room (cm).</summary>
        const int AheadCm = 200;
        /// <summary>Where j steps to make room for one going `dir` from `from`: the nearest free spot off that way on the side j stands, then the other.</summary>
        Pt? RoomSpot(int j, Pt from, Pt dir)
        {
            var at = bds[j].At; long len = G.Isqrt(G.D2(dir, default)); if (len == 0) return null;
            long side = (long)dir.X * (at.Y - from.Y) - (long)dir.Y * (at.X - from.X); // which side of the way it stands (left > 0)
            var n = side >= 0 ? new Pt(-dir.Y, dir.X) : new Pt(dir.Y, -dir.X);
            for (int s = 0; s < 2; s++, n = new Pt(-n.X, -n.Y))
                foreach (var d in RoomCm)
                    for (int k = 0; k < 3; k++)
                    {
                        var c = at + Scale(n, d) + (k == 0 ? default : Scale(dir, k == 1 ? -RoomSlantCm : RoomSlantCm)); // straight off the way, then a little back, then a little on
                        long off = Math.Abs((long)dir.X * (c.Y - from.Y) - (long)dir.Y * (c.X - from.X)) / len; // how far off the held one's way
                        if (off >= R.BodyMinCm + RoomClearCm && S.Grid.LineFree(at, c) && Fits(j, c, false)) return c;
                    }
            return null;
        }

        /// <summary>A person of the city on its routine, held bodies.replan with nothing working, changes plans (the file's head). False when it is
        /// not one who may (a tenet, the armed, someone on an errand of fright or report) or nothing serves.</summary>
        bool Replan(int i, Pt from)
        {
            ref var b = ref bds[i]; if (b.Kind != BkWalker || b.Cls != BcSoft || b.Armed || b.AltOn || b.RoomFor != null || Squeezing(in b)) return false;
            var w = Walkers[b.Ix];
            if (w.St == "fleeing" || w.St == "sheltered") return ReplanRefuge(i, from, w); // running for shelter, or at a packed door: another refuge
            if (w.St == "returning") return w.Run != null && PlanDetour(i, from, w.Run.End, true, true) && TakeWay(i, "back"); // back from shelter: another way
            if (w.St.Length > 0) return false; // a report, a check or a fright goes on as it can (L4: block a reporter's way and the news never lands)
            for (int k = 0; k < w.Place.Length; k++)
            {
                if (Tick >= w.From[k] && Tick < w.To[k]) return w.Out[k] && ReplanStop(i, from, w.Place[k], w.To[k]);
                if (k + 1 < w.Place.Length && Tick >= w.To[k] && Tick < w.From[k + 1]) return b.Stop != null && b.Stop == w.Place[k + 1] ? ReplanStop(i, from, b.Stop, w.Out[k + 1] ? w.To[k + 1] : w.From[k + 1]) : ReplanLeg(i, from, w, k); // early at its leg's end, it waits at the next stop
            }
            return false;
        }
        /// <summary>A chair or a stop it cannot get to: another free chair within ReplanChairCm (none taken, none another has taken instead), by a way
        /// round the bodies near it; else the nearest free spot it can walk to within ReplanSpotCm of the stop, not on it (it may be where it stands).</summary>
        bool ReplanStop(int i, Pt from, string stop, int until)
        {
            var c = People2!;
            if (c.FaceVec.ContainsKey(stop)) // a chair: another free one
            {
                string? best = null; long bd = long.MaxValue; Pt bp = default;
                foreach (var ch in c.Chairs) { if (ch == stop || seatHeld.ContainsKey(ch) || ChairTaken(ch)) continue; var at = CrowdPlace(ch); long d = G.D2(at, from); if (d < bd && d <= (long)ReplanChairCm * ReplanChairCm) { bd = d; best = ch; bp = at; } }
                if (best != null && PlanDetour(i, from, bp, true, true)) return TakeAlt(i, bp, best, until, "chair");
            }
            var place = CrowdPlace(stop); // a stop it cannot get to (held by one who keeps it, a tenet on it): the nearest free spot it can walk to that is still near it, maybe where it stands
            if (bodyWalk.Nearest(from, x => G.Within(x, place, ReplanSpotCm) && !G.Within(x, place, R.BodyMinCm) && Fits(i, x, true), ReplanSearchCm) is Pt spot && (G.Within(spot, from, CatchCm) && S.Grid.LineFree(from, spot) || PlanDetour(i, from, spot, true, true))) return TakeAlt(i, spot, null, until, "spot");
            return false;
        }
        /// <summary>On its way and blocked: its next stop by another way (round the bodies near it, however long), else the stop after it.</summary>
        bool ReplanLeg(int i, Pt from, Walker w, int k)
        {
            for (int n = k + 1; n < w.Place.Length && n <= k + 2; n++)
            {
                if (w.Place[n] == "beyond") return false; // off the sheet: it goes on as it can
                var at = CrowdPlace(w.Place[n]); var to = bodyWalk.Free(at) ? at : bodyWalk.NearestFree(at) ?? at;
                if (PlanDetour(i, from, to, true, true)) return TakeAlt(i, to, null, Math.Max(w.Out[n] ? w.To[n] : w.From[n], Tick + 1), n == k + 1 ? "next" : "skip");
            }
            return false;
        }
        /// <summary>Running for shelter and held (the way through jammed, the door it runs to packed): of the refuges within crowd.fleeMax that are no
        /// nearer the shot than it stands (as the crowd picks one, RefugeFor), its own among them, the nearest it can get to round the bodies near it;
        /// it runs on to that one from where it stands, that way (a flight again: the crowd's own state).</summary>
        bool ReplanRefuge(int i, Pt from, Walker w)
        {
            var shot = lastShooterFor.TryGetValue(w.P.Id, out var sh) ? sh : lastShooter; long mine = G.D2(from, shot), max2 = (long)R.CrowdFleeMaxCm * R.CrowdFleeMaxCm;
            refugeTry.Clear(); foreach (var (id, at, _) in S.Refuges) { long d = G.D2(from, at); if (d <= max2 && G.D2(at, shot) > mine) refugeTry.Add((d, id, at)); }
            refugeTry.Sort((x, y) => x.d != y.d ? x.d.CompareTo(y.d) : string.CompareOrdinal(x.id, y.id));
            for (int k = 0; k < refugeTry.Count && k < ReplanRefuges; k++)
            {
                var (_, id, at) = refugeTry[k]; var to = bodyWalk.Free(at) ? at : bodyWalk.NearestFree(at) ?? at; var here = bds[i].At; // where the law laid it this tick: the new flight's first place, so the host reads it there
                if (WayRound(i, here, to, true) is not Path way2) continue; detourPlans++;
                w.Refuge = id; w.Run = way2; w.RunStart = Tick; w.At = here; w.St = "fleeing"; // it runs on, from where it stands
                BodyReplans++; BodyReplansBy["refuge"] = BodyReplansBy.GetValueOrDefault("refuge") + 1; return true;
            }
            return false;
        }
        readonly List<(long d, string id, Pt at)> refugeTry = new List<(long, string, Pt)>();
        /// <summary>A flight's change of plans tries this many refuges, nearest first.</summary>
        const int ReplanRefuges = 3;
        bool TakeWay(int i, string how) { BodyReplans++; BodyReplansBy[how] = BodyReplansBy.GetValueOrDefault(how) + 1; return true; }
        bool ChairTaken(string chair) { for (int j = 0; j < bdN; j++) if (bds[j].AltOn && bds[j].AltStop == chair) return true; return false; }
        bool TakeAlt(int i, Pt at, string? chair, int until, string how)
        {
            ref var b = ref bds[i]; b.AltOn = true; b.Alt = at; b.AltStop = chair; b.AltUntil = until; b.Own = at;
            BodyReplans++; BodyReplansBy[how] = BodyReplansBy.GetValueOrDefault(how) + 1; return true;
        }

        /// <summary>The way a body squeezing past faces: across the way it was going (its shoulders along it), toward the other; null when it squeezes with no one.</summary>
        Pt? SqueezeFace(string id)
        {
            if (!bdIx.TryGetValue(id, out var i)) return null; ref var b = ref bds[i];
            if (b.SqzId == null || !bdIx.TryGetValue(b.SqzId, out var p)) return null;
            var n = new Pt(-b.SqzDir.Y, b.SqzDir.X); var to = bds[p].At - b.At;
            return (long)n.X * to.X + (long)n.Y * to.Y >= 0 ? n : new Pt(-n.X, -n.Y);
        }
    }
}
