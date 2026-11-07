// L9 Bodies: the host's and the tests' reads of the law (brief 098; moved out of GameBodies.cs, which neared the 500-line mark, by engine
// 0.47.0's give and take). Read-only and never hashed: they read the bodies the law laid at its latest run (each tick's end).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>What a body is doing in the give and take this tick (host read, BodyMove): squeezing past someone (with whom, and the way it faces:
    /// across its way, shoulders along it), making room for someone (for whom, and the spot it stepped to), or on a change of plans (the place it
    /// went instead, and the chair it took, if one).</summary>
    public readonly struct BodyMoveRead
    {
        public readonly string? SqueezeWith, RoomFor, AltChair; public readonly Pt SqueezeFacing, RoomAt, Alt; public readonly bool Replanned;
        public BodyMoveRead(string? sq, Pt sf, string? rf, Pt ra, bool re, Pt alt, string? chair) { SqueezeWith = sq; SqueezeFacing = sf; RoomFor = rf; RoomAt = ra; Replanned = re; Alt = alt; AltChair = chair; }
    }

    public sealed partial class Game
    {
        /// <summary>For the law's evidence (never hashed): this tick, the bodies held where they stood while their own place moved on, the farthest a
        /// body stands from its own place (cm, and who), and the cell's held operatives.</summary>
        public (int held, int farCm, string farId, int cellHeld) BodyStrain()
        {
            int held = 0, far = 0, cell = 0; string fid = "";
            for (int i = 0; i < bdN; i++) { ref var b = ref bds[i]; if (b.Moving && b.At == b.Prev) { held++; if (b.Kind == BkOp) cell++; } int d = G.Dist(b.At, b.Raw); if (d > far) { far = d; fid = b.Id; } }
            return (held, far, fid, cell);
        }
        /// <summary>Every body the law laid this tick: id, where it stands, its class (0 tenet, 1 armed, 2 the city's) and whether it walks.</summary>
        public List<(string id, Pt at, int cls, bool moving)> BodiesNow()
        {
            var l = new List<(string, Pt, int, bool)>(bdN);
            for (int i = 0; i < bdN; i++) l.Add((bds[i].Id, bds[i].At, bds[i].Cls, bds[i].Moving));
            return l;
        }
        /// <summary>For a probe (never hashed): each body the law laid this tick, id → its own place (where its own rules put it).</summary>
        public void BodyRawsInto(Dictionary<string, Pt> into) { into.Clear(); for (int i = 0; i < bdN; i++) into[bds[i].Id] = bds[i].Raw; }
        /// <summary>For a probe (never hashed): why the law laid a body where it did this tick, as BodyTrace writes it: its goal, its move and any fix
        /// ("side>next", "room>slide", "follow>next>off-ground"); "" when the law did not lay it.</summary>
        public string BodyWhy(string id) => bdIx.TryGetValue(id, out var i) ? WhyGoal[bds[i].Why & 15] + WhyMove[bds[i].Why >> 4] + WhyFix[bds[i].Fix] + (bds[i].Urgent ? ">urgent" : "") : "";
        /// <summary>For a probe (never hashed): each body the law laid this tick that is held (short of its goal and not got on), id → the ticks it
        /// has got nowhere (across its tries; 0 once it gets on).</summary>
        public void BodyStuckInto(Dictionary<string, int> into) { into.Clear(); for (int i = 0; i < bdN; i++) if (bds[i].StuckN > 0) into[bds[i].Id] = bds[i].StuckN; }
        /// <summary>A body as the law saw it this tick, for a probe (never hashed): its own place, lay, where it stands, class, form, moving, new.</summary>
        public string BodyTrace(string id)
        {
            if (!bdIx.TryGetValue(id, out var i)) return id + " (not laid)";
            ref var b = ref bds[i];
            return $"{id} raw {b.Raw} lay {b.Lay} at {b.At} prev {b.Prev} cls {b.Cls} form {b.Form} face {b.Face} moving {b.Moving} fresh {b.Fresh}{(b.Jumped ? " jumped" : "")} pace {b.Pace} ground {Walk.Free(b.At)} why {WhyGoal[b.Why & 15]}{WhyMove[b.Why >> 4]}{WhyFix[b.Fix]} aim {b.Aim}{(b.Mate ? " mate slot " + b.Slot : "")}{(b.Det != null ? (b.Catch ? " back to " : " detour to ") + b.Det.End + " until " + b.DetEnd : "")}{(b.RoomFor != null ? " room for " + b.RoomFor + " at " + b.RoomAt : "")}{(b.SqzId != null ? " squeezing with " + b.SqzId : "")}{(b.AltOn ? " plans changed to " + b.Alt + (b.AltStop != null ? " (" + b.AltStop + ")" : "") + " until " + b.AltUntil : "")} held {b.HeldN} stuck {b.StuckN}{(b.Kind == BkWalker ? " st '" + Walkers[b.Ix].St + "' stop " + (b.Stop ?? "-") : "")} path {(b.Path == null ? "-" : b.Path.Points[0] + ".." + b.Path.End + " len " + b.Path.Length + " mm " + b.PathMm)}";
        }
        /// <summary>A walker squeezing past someone: the way it faces (across its way, toward the other), for its facing read (FacingNow); else null.</summary>
        Pt? SqueezeFacing(Walker w) => R.BodiesOn && w.Body.Current(Tick, bodyRan) && w.Body.SqzId != null ? SqueezeFace(w.P.Id) : null;
        /// <summary>A test read (never hashed): whether the law laid this body as one the give and take never moves: a tenet, the cell or the armed (the rival crew, guards, officers, police, robots, a
        /// staff guard of the city).</summary>
        public bool BodyKeepsPlace(string id) => bdIx.TryGetValue(id, out var i) && (bds[i].Cls != BcSoft || bds[i].Armed);
        /// <summary>A test read (never hashed): whether the law holds this body now (short of its goal, getting nowhere: on its way, not settled).</summary>
        public bool BodyHeld(string id) => bdIx.TryGetValue(id, out var i) && bds[i].StuckN > 0;
        /// <summary>A test read (never hashed): the body as the law laid it this tick: where it stands, its own place, the stop it stands at (a
        /// person of the city's; a caller's call box), its class (0 tenet, 1 armed, 2 the city's) and its form at a stop (1 queue, 2 crew), the
        /// sector facing the law gave it, and a guard's crew spot at a post another holds (−1: none); null when the law did not lay it.</summary>
        public (Pt at, Pt raw, string? stop, int cls, int form, Pt face, int slot)? BodyInfo(string id) { if (!bdIx.TryGetValue(id, out var i)) return null; ref var b = ref bds[i]; return (b.At, b.Raw, b.Stop, b.Cls, b.Form, b.LayFace, b.Mate ? b.Slot : -1); }
        /// <summary>Whether the law laid this body as a tenet this tick (the cast, a person seated, an operative at work or leading Vale).</summary>
        public bool BodyIsTenet(string id) => bdIx.TryGetValue(id, out var i) && bds[i].Cls == BcFixed;
        /// <summary>A body's lay now (cm from where its own rules put it), by id; (0, 0) when the law has not moved it.</summary>
        public Pt BodyLayOf(string id) => bdIx.TryGetValue(id, out var i) ? bds[i].At - bds[i].Raw : default;
        /// <summary>Host read (never hashed): what a body is doing in the give and take this tick (BodyMoveRead), or null when nothing: not laid,
        /// not squeezing, not making room, no change of plans.</summary>
        public BodyMoveRead? BodyMove(string id)
        {
            if (!bdIx.TryGetValue(id, out var i)) return null; ref var b = ref bds[i];
            if (b.SqzId == null && b.RoomFor == null && !b.AltOn) return null;
            return new BodyMoveRead(b.SqzId, b.SqzId != null ? SqueezeFace(id) ?? default : default, b.RoomFor, b.RoomAt, b.AltOn, b.Alt, b.AltStop);
        }
        /// <summary>Host and test read (never hashed): the one this body squeezes past now, and the way it faces (across its way, toward the other);
        /// null when it squeezes with no one.</summary>
        public (string with, Pt facing)? BodySqueeze(string id) => bdIx.TryGetValue(id, out var i) && bds[i].SqzId is string w && SqueezeFace(id) is Pt f ? (w, f) : null;
    }
}
