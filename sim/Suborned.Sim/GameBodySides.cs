// L9 Bodies, keeping to one side (engine 0.48.0; Aster, brief 098 after L9's follow-up: "a body can still change its mind on the spot at a
// walk": x-person-0050 shuffled 14 cm back and forth four times in 0.8 s; Keel's probe found the same in a sidestep chosen afresh each tick,
// right then left, 13 cm a tick for two seconds in the crowd sheltering at the Office door). The law's choices for a body hold for a moment,
// as long as a held body waits before it looks for a way round (SlideKeepTicks):
//  · a slide round someone keeps to its side: no slide back toward the other side until the moment is up (GameBodyForms.cs);
//  · a spot the law picked for it (a sidestep off its way, a ring round its own place, a spot of its stop's form) is kept while it stays clear,
//    instead of being picked again each tick, so a body between two others no longer swaps sides; its own place clear again takes it back.
//    Not the cell's: an operative's walk waits for his body, so his own place moves with him and a spot kept from it would swing too.
// It forgets sooner once it gets on or reaches where it makes for, takes another way (round, back, giving way, a change of plans), makes room,
// or its own rules plan again. Stepping clear of one already inside two radii (urgent) is never held to a pick. Integer cm, fixed order.
namespace Suborned.Sim
{
    public sealed partial class Game
    {
        /// <summary>A step's part across the body's own way (its own place's step this tick); all of it when its own place stands.</summary>
        static Pt Across(in Bd b, Pt v)
        {
            long h2 = G.D2(b.Head, default); if (h2 == 0) return v;
            long d = (long)v.X * b.Head.X + (long)v.Y * b.Head.Y;
            return new Pt(v.X - (int)(d * b.Head.X / h2), v.Y - (int)(d * b.Head.Y / h2));
        }
        /// <summary>The slide side kept after this tick's lay: a slide sets it anew for SlideKeepTicks (its part across the body's way); else the
        /// side already kept runs on to its end (none: (0, 0), 0).</summary>
        (Pt v, int until) SideAfter(in Bd b)
        {
            if (b.Why >> 4 == 2) { var v = Across(in b, b.At - b.Prev); if (G.D2(v, default) > (long)BodyCheck.RoundCm * BodyCheck.RoundCm) return (v, Tick + 1 + SlideKeepTicks); }
            return Tick + 1 < b.SlideUntil ? (b.SlideV, b.SlideUntil) : (default, 0);
        }
        /// <summary>The spot the law picked for this body within its moment (from where it makes for), while it is clear and still a pick of that
        /// kind: a sidestep for one walking, a stop's spot for one standing at its stop (a guard's crew spot keeps its own: BodyLay.Slot).</summary>
        bool KeptPick(int i, out Pt goal)
        {
            ref var b = ref bds[i]; goal = default;
            if (b.PickWhy == 0 || Tick >= b.PickUntil || b.Fresh || b.AltOn || b.Kind == BkOp) return false; // an operative's walk waits for his body (BodyStore), so a spot kept from his own place would swing with it
            if ((b.PickWhy == WhySide && !b.Moving) || (b.PickWhy == WhyForm && (b.Form != FmQueue || b.Moving))) return false;
            var c = b.Aim + b.PickOff; if (!Fits(i, c, b.PickComfort) || !OffDoorWay(i, c)) return false; // taken now, by the standard it was picked by: it picks again; engine 0.50.0, nor a doorway for a guard at a post
            goal = c; return true;
        }
        /// <summary>After the goal is chosen: a new pick of the law's (a sidestep, a ring, a queue's spot) is kept for SlideKeepTicks; a kept one runs
        /// on; anything else (its own way, making room, giving way, a way round or back) drops it.</summary>
        void PickAfter(int i, byte why, Pt goal, bool kept)
        {
            ref var b = ref bds[i]; if (kept) return;
            if (b.Kind != BkOp && (why == WhySide || why == WhyRing || why == WhyRing2 || (why == WhyForm && b.Form == FmQueue))) { b.PickWhy = why; b.PickOff = goal - b.Aim; b.PickUntil = Tick + 1 + SlideKeepTicks; b.PickComfort = why != WhyRing2 && Fits(i, goal, true); }
            else b.PickWhy = 0;
        }
    }
}
