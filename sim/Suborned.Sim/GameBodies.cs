// Brief 098, law L9 Bodies (Sky 2026-11-07, playing, three guards inside one another on a call box and the rival crew on one spot: "our people
// bunch up together too much - we need to think about real tactics from this, and ensuring NPC's don't overlap each, they have real boundaries";
// rulebook `bodies`, engine 0.46.0; give and take, engine 0.47.0: Sky 2026-11-07 on the last resort, "Do all but the jumping back to spot should be
// better to dealt with"). Every person on the street has a body, no two bodies on their feet overlap, and nobody jumps: no body the law lays moves
// farther in a tick than its own pace (BodyPace). Each tick, after every rule has moved its people, the bodies are laid (BodyStep):
//  · gathered: everyone upright on the street inside the walk window, once each, with the place their own rules give them now ("raw") and last
//    tick's displacement ("lay"). A body on the ground is left where it fell; a person carried, or in a car, is off the street. One new to the
//    street (out of a door, a car) is placed; one already on it whose own place jumped (a fright's end, a leg timed faster than a run, a new walk)
//    walks there from where it stood, at its pace.
//  · ordered: the tenets (fixed: the cast, a person in a café chair, an operative at work, the Enforcer leading Vale), then the armed (firm: the
//    cell, the patrol, the rival crew, fight bodies, officers, police units), then the city's people (soft); standing before walking; kind; id.
//  · laid one by one (GameBodyForms.cs): where its own rules put it when that is clear of every body it must respect (one laid this tick, or one
//    still where it stood), else a goal (holding its lay, a queue or a crew spot at a stop held, a sidestep or a wait, rings round its place), at
//    the law's paces, never over its own; a body held asks a person of the city in its way to make room, two people of the city face to face in a
//    gap squeeze past, and a person of the city who still cannot get through changes plans (GameBodyGive.cs); the ways round (GameBodyDetours.cs).
//  · stored: each body's lay, read by its pose (CrowdPos, MemberPose, Hostile.Pos, SecOfficerPose, UnitPose, an operative's Pos) through the next
//    tick's rules, so the rules and the host read the same laid bodies. Hashed (HashBodies). The fixed are never moved.
// Integer cm, no floats; a fixed order and fixed candidate lists: the same seed and orders lay the same bodies.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>L9: a body's lay. Lay: the offset from the place its own rules give it (cm); Raw: that place when it was laid; Face: a sector
    /// facing the law gave it at a shared post ((0, 0): its own); At1: the tick it was laid + 1 (0: never). A lay holds for the tick it was laid
    /// and the next tick's reads; older, the body has left the street and comes back on its own place.</summary>
    public struct BodyLay
    {
        public Pt Lay, Raw, Face; public int At1; public bool Held; // Held: it could not move last tick (it waited): those who would have it make way walk round it instead
        /// <summary>Ticks it has been held running; its way round what held it (GameBodyDetours.cs), the tick that way runs out, and whether it is a
        /// way back to its own place (Catch: walled off from it, or its own place jumped off its walk).</summary>
        public int HeldN, DetEnd, DetTries; public Path? Det; public Pt HeldAt; public bool Catch;
        /// <summary>Who held its step last (id), and its giving way to them: until GiveEnd, back to GiveAt (GameBodyDetours.cs).</summary>
        public string? BlkId; public int GiveEnd; public Pt GiveAt;
        /// <summary>Ticks it has got nowhere, across its tries (a person of the city changes plans at bodies.replan).</summary>
        public int StuckN;
        /// <summary>Making room (GameBodyGive.cs): for whom, the spot it stepped to, where it stood when asked, the asker's way then, until when.</summary>
        public string? RoomFor; public Pt RoomAt, RoomFrom, RoomDir, RoomRaw; public int RoomEnd; // RoomRaw: its own place when asked (engine 0.48.0: once that has moved on, it goes its own way)
        /// <summary>The last it made room for and until when it is not asked again by them (it stood aside once; they did not get past).</summary>
        public string? RoomDone; public int RoomDoneUntil;
        /// <summary>Squeezing past (GameBodyGive.cs): with whom, until when at most, and the way it was going when the squeeze began.</summary>
        public string? SqzId; public int SqzEnd; public Pt SqzDir;
        /// <summary>A change of plans (GameBodyGive.cs): the place it went instead (another chair, another spot, its next stop the other way), the
        /// chair it took (null: none), and the tick the step of its routine it replaced ends.</summary>
        public bool AltOn; public Pt Alt; public string? AltStop; public int AltUntil;
        /// <summary>Its last slide round someone ((0, 0): none) and the tick until which it keeps to that side (engine 0.48.0, no shuffling: it never
        /// slides back the other way before then; GameBodyForms.cs).</summary>
        public Pt SlideV; public int SlideUntil;
        /// <summary>Engine 0.48.0: the spot the law picked for it (a sidestep, a ring, a queue's spot; WhyGoal), as an offset from where it makes
        /// for, kept until PickUntil while it stays clear (GameBodySides.cs).</summary>
        public byte PickWhy; public Pt PickOff; public int PickUntil; public bool PickComfort; // PickComfort: picked with personal space, kept only while it keeps it
        /// <summary>A guard's crew spot at a post another holds (0–4 the inner ring, 5–10 the outer; −1: none): his while he stays there, so two never make for one.</summary>
        public int Slot;
        /// <summary>The plan its own rules had when it was laid (a walk, a stand, a state, and the tick and place they started from). Once its rules
        /// plan again (a new walk, a stop, a fright) they plan from where it stands, so the lay is spent: it holds only while the plan is the same.</summary>
        public object? BRef; public int BN; public Pt BAt;
        public bool Same((object? r, int n, Pt at) p) => ReferenceEquals(BRef, p.r) && BN == p.n && BAt == p.at;
        public bool Live(int tick) => At1 > 0 && tick >= At1 - 1 && tick <= At1;
        /// <summary>Laid by the law's latest run (`ran`: the tick it last ran): a body it did not lay then (off the street, out of the window) has none.</summary>
        public bool Current(int tick, int ran) => At1 == ran + 1 && tick >= ran && tick <= ran + 1;
    }

    public sealed partial class Game
    {
        internal const byte BkCast = 0, BkPatrol = 1, BkOp = 2, BkHostile = 3, BkCrew = 4, BkOfficer = 5, BkUnit = 6, BkWalker = 7;
        internal const byte BcFixed = 0, BcFirm = 1, BcSoft = 2;
        internal const byte FmNone = 0, FmQueue = 1, FmCrew = 2;

        /// <summary>One body this tick. At: where it stands now (laid this tick, or where it stood last tick while waiting its turn: Prev).</summary>
        struct Bd
        {
            public string Id; public byte Kind, Cls, Form; public int Ix, Reach, Pace; public bool Moving, Fresh, Jumped, Placed, Armed, Held, Mate, WalksFirm, Urgent, Laden, Idle, SitIn; // Idle (engine 0.49.0): an operative with no order, off the place the player put him: he makes room as anyone does (GameBodyGive.cs); SitIn: an operative taking his last step into his Listen's seat, across ground no body stands on // Laden: an operative carrying someone (never asked to make room: GameBodyGive.cs) // Mate: a guard at a post point another holds (BodyPosts); Pace: its own top pace (cm a tick); Jumped: its own place jumped while it stood on the street; Urgent: one laid before it this tick stands inside two radii of where it stood (a probe read, never stored)
            public Pt Raw, Own, Lay, At, Prev, Head, Face, LayFace, Aim; public long Key, PathMm; public int StuckN, HeldN, DetEnd, DetTries, Slot, Blk, GiveEnd; public Path? Det; public bool Catch; public Pt HeldAt, GiveAt; public string? BlkId; public byte Why, Fix; public object? BRef; public int BN; public Pt BAt; public Path? Path; public bool PathBack; public string? Stop; // Path, PathMm: the walk its own place is on, how far along, and walked end to start (a kite's way back); Own: where it makes for (its own place, or its change of plans)
            public string? RoomFor, SqzId, AltStop, RoomDone; public Pt RoomAt, RoomFrom, RoomDir, RoomRaw, SqzDir, Alt, SlideV; public int RoomEnd, SqzEnd, AltUntil, RoomDoneUntil, SlideUntil, PickUntil; public bool AltOn, PickComfort; public byte PickWhy; public Pt PickOff; // GameBodyGive.cs; SlideV, SlideUntil: its last slide and how long it keeps to that side; PickWhy, PickOff, PickUntil: the law's kept pick (GameBodySides.cs)
        }
        Bd[] bds = new Bd[128]; int bdN; int[] bdOrder = new int[128]; long[] bdKeys = new long[128];
        readonly Dictionary<long, int> bhHead = new Dictionary<long, int>();
        int[] bhBody = new int[256], bhNext = new int[256]; int bhN;
        int[] bdMark = new int[128]; int bdStamp;
        /// <summary>This tick's bodies by id (the law's index).</summary>
        readonly Dictionary<string, int> bdIx = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly BodyLay[] castLay = new BodyLay[4]; BodyLay patrolLay; readonly BodyLay[] crewLay = new BodyLay[5]; BodyLay[] secLay = Array.Empty<BodyLay>();
        WalkGrid bodyWalk;
        /// <summary>Host and test reads (never hashed): ticks where two tenets stood inside two radii (the tenet wins), and bodies the law could not
        /// clear this tick (laid where they overlap; 0 when the law holds).</summary>
        public int BodyTenetTicks, BodyOverlapsNow; public long BodyOverlapTicks;
        /// <summary>Host and test reads (never hashed), the give and take (GameBodyGive.cs): people of the city asked to make room, squeezes begun
        /// (each a pair), and changes of plans, by what was taken instead (chair, spot, next).</summary>
        public int BodySidesteps, BodySqueezes, BodyReplans; public readonly SortedDictionary<string, int> BodyReplansBy = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Where a pose's own rules put a body, laid by L9 for this tick's reads.</summary>
        Pt Laid(Pt raw, in BodyLay b, int tick, (object? r, int n, Pt at) plan) => R.BodiesOn && tick >= Tick && b.Current(tick, bodyRan) && b.Same(plan) ? raw + b.Lay : raw;
        /// <summary>The tick the law last laid the bodies (each tick's end, and the start): a lay from an earlier run no longer holds.</summary>
        int bodyRan = -1;
        /// <summary>An operative's walk carries its lay while it is the walk the lay was taken on (a new walk starts where he stands).</summary>
        Pt OpLay(Op op) => R.BodiesOn && op.Walk != null && ReferenceEquals(op.BodyWalk, op.Walk) && op.Body.Current(Tick, bodyRan) ? op.Body.Lay : default;
        /// <summary>The cast's and the patrol's poses, laid by L9 (their own rules: the *Raw reads in Game.cs). A tenet standing at its place is never
        /// moved (its lay is zero); walking, it steps round people as anyone armed does.</summary>
        public (Pt pos, Pt facing, string phase)? GuardPose() { var g = GuardPoseRaw(); return g.HasValue ? (Laid(g.Value.pos, castLay[2], Tick, CastPlan(2)), g.Value.facing, g.Value.phase) : g; }
        public Pt? DriverPos() { var d = DriverPosRaw(); return d.HasValue ? Laid(d.Value, castLay[1], Tick, CastPlan(1)) : d; }
        public Pt? ValePos() { var v = ValePosRaw(); return v.HasValue ? Laid(v.Value, castLay[3], Tick, CastPlan(3)) : v; }
        public Pt? PersonPos(Person p) { var r = PersonPosRaw(p); if (!r.HasValue || CrewOfPerson(p) != null) return r; for (int k = 0; k < 4 && k < People.Count; k++) if (People[k] == p) return Laid(r.Value, castLay[k], Tick, CastPlan(k)); return r; }
        public (Pt pos, Pt facing)? PatrolPose() { var q = PatrolPoseRaw(); return q.HasValue && !PatrolDown ? (Laid(q.Value.pos, patrolLay, Tick, PatrolPlan()), q.Value.facing) : q; }
        /// <summary>An officer of the response unit: his place and facing, laid by L9 (his own rules: SecOfficerPoseRaw in Game.cs; joined to the
        /// final fight, his fight body's).</summary>
        public (Pt pos, Pt facing)? SecOfficerPose(int k) { var p = SecOfficerPoseRaw(k); return p.HasValue && !secJoined[k] && k < secLay.Length ? (Laid(p.Value.pos, secLay[k], Tick, SecPlan(k)), p.Value.facing) : p; }
        /// <summary>A walker's facing the law gives it, while the law holds it: a sector at a shared post, sideways while it squeezes past someone,
        /// a chair's own facing in a chair it took instead of its own (GameBodyGive.cs).</summary>
        Pt? BodyFace(Walker w) => !R.BodiesOn || !w.Body.Current(Tick, bodyRan) ? (Pt?)null : w.Body.SqzId != null ? SqueezeFace(w.P.Id) : w.St.Length == 0 && w.Body.Face != default ? w.Body.Face : w.St.Length == 0 && w.Body.AltOn && w.Body.AltStop != null && G.Within(w.Body.Raw + w.Body.Lay, w.Body.Alt, SitCm) ? StopFace(w.Body.AltStop) : (Pt?)null;
        // each kind's plan (BodyLay.Same): what its own rules re-plan from where it stands
        (object? r, int n, Pt at) WalkerPlan(Walker w) => w.St.Length == 0 ? (null, 0, default) : ((object?)w.Run ?? w.St, w.RunStart, w.At);
        (object? r, int n, Pt at) CrewPlan(CrewMember m) => ((object?)m.Disp ?? m.Walk, m.WalkStart, m.Place);
        (object? r, int n, Pt at) UnitPlan(Unit u) => (unitDisp.TryGetValue(u.Id, out var d) ? d : (object?)u.Route, u.Launch, u.Dest);
        (object? r, int n, Pt at) SecPlan(int k) => (secDisp.TryGetValue(k, out var d) ? d : (object?)securityPath, SecurityArrives, default);
        (object? r, int n, Pt at) CastPlan(int i) => i == 1 ? (Lent ? "lent" : OfficialOut ? "out" : null, valeDriveStart, default) : i == 2 ? GuardPlan() : i == 3 ? (null, (int)Vale, default) : (null, 0, default);
        (object? r, int n, Pt at) GuardPlan() => GuardHeld ? (GuardAlly && guardAllyPath != null ? (guardAllyPath, guardAllyStart, default) : (heldPhase, 0, heldPos)) : guardRunning ? (guardRun, LureTick, default) : (null, LureTick, default);
        (object? r, int n, Pt at) PatrolPlan() => PatrolChasing ? ("chase", 0, default) : PatrolUnit() is Unit pu ? (pu, 0, default) : PatrolAwayUntil > Tick ? ("away", PatrolAwayUntil, default) : (null, 0, default);

        // ---------------------------------------------------------------- the step

        void BodyStep()
        {
            if (!R.BodiesOn || Range) return;
            if (secLay.Length != SecGuns.Length) secLay = new BodyLay[SecGuns.Length];
            bodyRan = Tick; detourPlans = 0; bodyWalk = Walk; bodyCars.Clear(); bodyCars.AddRange(StandingCars()); BodyGather(); BodyPosts();
            for (int i = 0; i < bdN; i++) { bdKeys[i] = bds[i].Key; bdOrder[i] = i; }
            Array.Sort(bdKeys, bdOrder, 0, bdN);
            bhHead.Clear(); bhN = 0;
            for (int i = 0; i < bdN; i++) HashAdd(i, bds[i].At);
            BodyOverlapsNow = 0; crowded.Clear();
            for (int k = 0; k < bdN; k++) LayOne(bdOrder[k]);
            BodyRepair(); // GameBodyForms.cs: anyone still inside two radii, among everyone's final places
            for (int i = 0; i < bdN; i++) BodyStore(i);
            if (seatHeld.Count > seatSeen.Count) { seatGone.Clear(); foreach (var kv in seatHeld) if (!seatSeen.Contains(kv.Key)) seatGone.Add(kv.Key); foreach (var c in seatGone) seatHeld.Remove(c); } // a chair its sitter has left is free
            bodyWalk = default;
        }

        void BodyGather()
        {
            bdN = 0; bdIx.Clear(); seatSeen.Clear();
            // the tenets: the cast where their rules put them, the patrol on its sweep
            for (int i = 0; i < People.Count && i < 4; i++) { var at = CastOnFoot(People[i]); if (at.HasValue) AddBd(People[i].Id, BkCast, BcFixed, i, at.Value, castLay[i], FmNone, default, CastPlan(i), !(People[i] == ValeP && (Vale == ValeAt.WithEnforcer || Vale == ValeAt.Taking)) || !castLay[i].Live(Tick) || castLay[i].Lay != default); } // Vale held or being seized goes with whoever holds him; engine 0.47.1: stepping out (of the Office door) where someone stands, he steps out beside them first, as the rest of the cast do, and joins his holder as a tenet walking does (0.47.0 laid him on a passer-by)
            if (!PatrolDown && PatrolPoseRaw() is var pp && pp.HasValue)
            {
                AddBd("patrol", BkPatrol, BcFirm, 0, pp.Value.pos, patrolLay, FmNone, default, PatrolPlan());
                if (PatrolUnit() is Unit pu) { if ((pu.St == "walk" || pu.St == "back") && pu.Route != null) OnPath("patrol", pu.Route, (long)Math.Max(0, Tick - pu.Launch) * pu.Speed); } // its walks, followed when it falls behind (not a line through walls)
                else if (RerouteWalkPlan() is var rw && rw.HasValue) { var w = rw.Value; if (Tick < w.leave) OnPath("patrol", w.@out, Math.Min((long)Math.Max(0, Tick - w.outAt) * w.mmT, w.turnMm)); else OnPath("patrol", w.back, (long)(Tick - w.leave) * w.mmT); }
            }
            // the cell: an operative at work, or leading Vale, is a tenet; else armed and firm
            for (int i = 0; i < 4; i++)
            {
                var op = Ops[i]; if (op.Inside || op.InCar || op.CarriedBy.HasValue || op.Down || op.LeftMap) continue;
                var seat = SeatOf(op); var into = seat == null ? SittingInto(op) : null; if ((seat ?? into) is string ch) { seatHeld[ch] = Sim.Op.Name(op.Id); seatSeen.Add(ch); } // engine 0.49.0: an operative in a café chair, or taking his last step into it, holds it, as a customer does
                bool walkSame = op.Walk != null && ReferenceEquals(op.BodyWalk, op.Walk), fixedOp = (op.Busy != null && !(op.Busy.Do == "listen" && op.Walk != null)) || (op.Id == OpId.Enforcer && Vale == ValeAt.WithEnforcer) || seat != null;
                var raw = fixedOp ? op.Pos : op.Walk != null ? op.Walk.At(op.WalkMm) : op.Body.Live(Tick) && op.Pos == op.Body.Raw + op.Body.Lay ? op.Body.Raw : op.Pos; // at work or leading Vale he stands where he stands
                bool anew = fixedOp || (op.Walk != null ? !walkSame : raw == op.Pos); // a new walk or a set place starts where he stands
                AddBd(Sim.Op.Name(op.Id), BkOp, fixedOp ? BcFixed : BcFirm, i, raw, op.Body, FmNone, default, (null, 0, default), false, anew); // his plan is his walk and his place (above)
                if (bdN > 0 && bds[bdN - 1].Id == Sim.Op.Name(op.Id)) { if (op.Walk != null) { bds[bdN - 1].Path = op.Walk; bds[bdN - 1].PathMm = op.WalkMm; } bds[bdN - 1].Laden = op.Carrying.HasValue || op.CarryBody.Length > 0; bds[bdN - 1].Idle = !fixedOp && OpIdle(op, raw); bds[bdN - 1].SitIn = into != null; }
            }
            // the armed: fight bodies, the rival crew on foot, the response unit's officers, police units and robots
            for (int i = 0; i < Hostiles.Count; i++) { var h = Hostiles[i]; if (!h.Present || h.Down || h.Gun.St == "gone") continue; AddBd(h.Id, BkHostile, BcFirm, i, h.RawPos(Tick), h.Body, FmNone, default, h.Plan); if (h.Run != null) OnPath(h.Id, h.Run, (long)Math.Max(0, Tick - h.RunStart) * h.Speed); }
            for (int i = 0; i < Crew.Count; i++) { var m = Crew[i]; if (m.Fight || !Up(m)) continue; if (MemberPoseRaw(m) is var mp && mp.HasValue) { AddBd(m.Id, BkCrew, BcFirm, i, mp.Value.pos, crewLay[i], FmNone, default, CrewPlan(m)); if (m.Disp != null) OnDisp(m.Id, m.Disp, rivSpeed); else if (m.Mode == CrewMode.Walking && m.Walk != null) OnPath(m.Id, m.Walk, (long)Math.Max(0, Tick - m.WalkStart) * m.Speed); } }
            for (int k = 0; k < SecGuns.Length; k++) { if (secJoined[k]) continue; if (SecOfficerPoseRaw(k) is var sp && sp.HasValue) { AddBd(SecGuns[k].Id, BkOfficer, BcFirm, k, sp.Value.pos, secLay[k], FmNone, default, SecPlan(k)); if (secDisp.TryGetValue(k, out var sd)) OnDisp(SecGuns[k].Id, sd, secSpeed); } }
            for (int i = 0; i < Units.Count; i++) { var u = Units[i]; if (u.Legacy || u.Patrol || !u.OnStreet(Tick)) continue; if (UnitPoseRaw(u) is var up && up.HasValue) { AddBd(u.Id, BkUnit, BcFirm, i, up.Value.pos, u.Body, FmNone, default, UnitPlan(u)); if (unitDisp.TryGetValue(u.Id, out var ud)) OnDisp(u.Id, ud, u.Speed); else if ((u.St == "walk" || u.St == "back") && u.Route != null) OnPath(u.Id, u.Route, (long)Math.Max(0, Tick - u.Launch) * u.Speed); } }
            // the city's people (the dial's guards armed, at a post a crew spot; a person in a café chair a tenet, in a chair it took instead too)
            if (People2 != null && Start.Crowd)
                for (int i = 0; i < Walkers.Count; i++)
                {
                    var w = Walkers[i]; if (w.P.Down) continue;
                    var raw = CrowdRaw(w, out var stop, out var leg, out var legMm); if (!raw.HasValue) continue;
                    var pb = w.Body; bool alt = pb.AltOn && pb.Live(Tick) && w.St.Length == 0 && Tick < pb.AltUntil; // a change of plans holds while its step of the routine lasts
                    string? seat = alt ? pb.AltStop : stop; var stood = pb.Live(Tick) ? pb.Raw + pb.Lay : raw.Value; Pt seatAt = alt && pb.AltStop != null ? pb.Alt : raw.Value;
                    bool seated = seat != null && People2.FaceVec.ContainsKey(seat) && (!alt || pb.AltStop != null) && Sits(seat, w.P.Id, pb.Live(Tick) ? stood - seatAt : default), armed = IsStaff(w.P);
                    var face = stop != null ? StopFace(stop) ?? default : w.St == "calling" ? CallFace(w) : default;
                    byte form = alt || (stop == null && w.St != "calling") ? FmNone : armed ? FmCrew : FmQueue;
                    AddBd(w.P.Id, BkWalker, seated ? BcFixed : BcSoft, i, raw.Value, pb, form, face, WalkerPlan(w));
                    if (bdN > 0 && bds[bdN - 1].Id == w.P.Id) { ref var nb = ref bds[bdN - 1]; nb.Armed = armed; nb.Path = leg; nb.PathMm = legMm; nb.Stop = stop ?? (w.St == "calling" ? w.Refuge : null); if (seated && alt) nb.Own = seatAt; if (!alt) { nb.AltOn = false; nb.AltStop = null; } }
                }
        }

        void AddBd(string id, byte kind, byte cls, int ix, Pt raw, in BodyLay prev, byte form, Pt face, (object? r, int n, Pt at) plan, bool walksFirm = false, bool anew = false)
        {
            if (!(S.Grid.InGrid(raw) || (prev.Live(Tick) && S.Grid.InGrid(prev.Raw + prev.Lay))) || bdIx.ContainsKey(id)) return; // off the window, its own place and where it stands (one whose own place has walked off the sheet walks out after it): nobody sees them there; once each
            if (bdN == bds.Length) { Array.Resize(ref bds, bdN * 2); Array.Resize(ref bdOrder, bdN * 2); Array.Resize(ref bdKeys, bdN * 2); Array.Resize(ref bdMark, bdN * 2); }
            bool live = prev.Live(Tick), fresh = !live; // new on the street (out of a door, a car, a refuge): placed; nobody already on it is ever placed
            bool same = !anew && (kind == BkOp || prev.Same(plan)), jumped = live && (long)G.D2(raw, prev.Raw) > (long)JumpCm * JumpCm; // its own place jumped (a fright's end, a fast leg, a new walk): it walks there from where it stood
            var lay = live && same && !jumped ? prev.Lay : default; // a new plan starts where it stands: the lay is spent
            if (walksFirm && !fresh && (raw != prev.Raw || prev.Raw + prev.Lay != raw)) cls = BcFirm; // a tenet walking, or not yet back on its own place (where it stood, whatever its plan), steps round people; on it, it is never moved
            if (cls == BcFixed) lay = default;
            bool moving = !fresh && raw != prev.Raw;
            ref var b = ref bds[bdN];
            var stood = live ? prev.Raw + prev.Lay : raw; // where it stood last tick: it is there until it is laid, so those laid before it leave it room to wait
            b = new Bd { Id = id, Kind = kind, Cls = cls, Ix = ix, Raw = raw, Own = raw, Lay = lay, At = stood, Prev = stood, Held = live && prev.Held, Moving = moving, Fresh = fresh, Jumped = jumped, Head = moving && !jumped ? raw - prev.Raw : default, Form = moving ? FmNone : form, Face = face, LayFace = live ? prev.Face : default };
            b.Pace = PaceCmT(kind, ix); b.WalksFirm = walksFirm;
            b.Key = ((long)cls << 40) | ((long)(moving ? 1 : 0) << 36) | ((long)kind << 28) | (long)ix; // within a class: those standing, then those walking
            b.BRef = plan.r; b.BN = plan.n; b.BAt = plan.at;
            bool keep = live && same; b.HeldN = keep ? prev.HeldN : 0; b.DetTries = keep ? prev.DetTries : 0; b.HeldAt = prev.HeldAt; b.StuckN = keep ? prev.StuckN : 0; b.Blk = -1; b.BlkId = keep ? prev.BlkId : null; b.GiveEnd = keep ? prev.GiveEnd : 0; b.GiveAt = prev.GiveAt; b.Det = keep ? prev.Det : null; b.Catch = b.Det != null && prev.Catch; b.DetEnd = keep ? prev.DetEnd : 0; b.Slot = keep && prev.At1 > 0 ? prev.Slot : -1; // a new plan: no way round, no crew spot
            if (live && prev.RoomDone != null && Tick < prev.RoomDoneUntil) { b.RoomDone = prev.RoomDone; b.RoomDoneUntil = prev.RoomDoneUntil; }
            if (keep && prev.RoomFor != null && Tick < prev.RoomEnd) { b.RoomFor = prev.RoomFor; b.RoomAt = prev.RoomAt; b.RoomFrom = prev.RoomFrom; b.RoomDir = prev.RoomDir; b.RoomRaw = prev.RoomRaw; b.RoomEnd = prev.RoomEnd; } // making room, while its own plan stands
            else if (live && prev.RoomFor != null) { b.RoomDone = prev.RoomFor; b.RoomDoneUntil = Tick + RoomTicks; } // its time up, or its own plan changed: it stood aside once (not asked again by them a while)
            if (live && prev.SqzId != null) { b.SqzId = prev.SqzId; b.SqzEnd = prev.SqzEnd; b.SqzDir = prev.SqzDir; } // squeezing past someone: a body's state, whatever its plan, until the two stand apart
            if (live && Tick < prev.SlideUntil) { b.SlideV = prev.SlideV; b.SlideUntil = keep ? prev.SlideUntil : Tick + 1; } // engine 0.48.0: it keeps to the side it slid to for a moment, while its plan stands (a new plan: only the next tick's, as 0.47 had)
            if (keep && Tick < prev.PickUntil) { b.PickWhy = prev.PickWhy; b.PickOff = prev.PickOff; b.PickUntil = prev.PickUntil; b.PickComfort = prev.PickComfort; } // and the spot the law picked for it, while its plan stands
            if (keep && prev.AltOn) { b.AltOn = true; b.Alt = prev.Alt; b.AltStop = prev.AltStop; b.AltUntil = prev.AltUntil; if (prev.AltStop == null || cls != BcFixed) b.Own = prev.Alt; } // a change of plans, while its step of the routine lasts (BodyGather)
            bdIx[id] = bdN; bdN++;
        }
        /// <summary>Guards sent to one post point (the dial's added guards share points once each point has one): the one standing nearest it last
        /// tick holds it (a tie: the first in the law's order); the others are his mates, who take crew spots round it (FormGoal).</summary>
        void BodyPosts()
        {
            postIx.Clear(); slotsHeld.Clear();
            for (int i = 0; i < bdN; i++) { bds[i].Mate = false; if (bds[i].Armed && bds[i].Form == FmCrew) postIx.Add(i); }
            for (int a = 0; a < postIx.Count; a++)
            {
                ref var b = ref bds[postIx[a]]; long mine = G.D2(b.Prev, b.Raw);
                for (int c = 0; c < postIx.Count && !b.Mate; c++)
                {
                    if (c == a) continue; ref var o = ref bds[postIx[c]]; if (o.Raw != b.Raw) continue;
                    long theirs = G.D2(o.Prev, o.Raw); if (theirs < mine || (theirs == mine && o.Key < b.Key)) b.Mate = true; // another stands nearer the point
                }
            }
            foreach (var i in postIx) // each mate keeps the crew spot he had (the first in the law's order, if two had one)
            {
                ref var b = ref bds[i]; if (!b.Mate || b.Slot < 0) { b.Slot = -1; continue; }
                long post = PostKey(b.Raw); slotsHeld.TryGetValue(post, out var held);
                if ((held & (1 << b.Slot)) != 0) b.Slot = -1; else slotsHeld[post] = held | (1 << b.Slot);
            }
        }
        readonly List<int> postIx = new List<int>();
        /// <summary>The crew spots taken at each post point this tick (a bit a spot).</summary>
        readonly Dictionary<long, int> slotsHeld = new Dictionary<long, int>();
        static long PostKey(Pt p) => unchecked(((long)p.X << 32) ^ (uint)p.Y);
        /// <summary>The walk the body just gathered is on (its own place PathMm along it): a body behind its own place follows it (GameBodyForms.cs).</summary>
        void OnPath(string id, Path p, long mm, bool back = false) { if (bdN > 0 && bds[bdN - 1].Id == id) { bds[bdN - 1].Path = p; bds[bdN - 1].PathMm = Math.Max(0, Math.Min(mm, p.Length)); bds[bdN - 1].PathBack = back; } }
        /// <summary>A kite's step off a line and back (KiteDisp): out along its walk, held at its end, or back along it end to start.</summary>
        void OnDisp(string id, KiteDisp d, int speed) { if (d.Phase == 0) OnPath(id, d.P, (long)Math.Max(0, Tick - d.Start) * speed); else if (d.Phase == 2) OnPath(id, d.P, d.P.Length - (long)Math.Max(0, Tick - d.BackStart) * speed, true); }
        /// <summary>An operative's walk is held back to his body's progress when he is more than this short of where its clock has him (mm).</summary>
        const long WalkSlackMm = 300;
        /// <summary>A body whose own place moves farther than this in one tick has jumped (a fright's end, a leg timed faster than a run, a new walk's
        /// start): it walks there from where it stood, with no lay carried over.</summary>
        const int JumpCm = 100;

        /// <summary>L9 (a seat is one person's at a time): the chair's sitter, from when they sat until they leave it. A person due in a chair someone
        /// else still sits in (two days shifted by the seed onto one seat, or a person early at the end of their walk) waits beside it, or takes
        /// another (GameBodyGive.cs).</summary>
        readonly SortedDictionary<string, string> seatHeld = new SortedDictionary<string, string>(StringComparer.Ordinal);
        readonly HashSet<string> seatSeen = new HashSet<string>(StringComparer.Ordinal); readonly List<string> seatGone = new List<string>();
        /// <summary>Whether `id` sits in `chair` now: it is theirs, or free and they stand within SitCm of it (`off`: where they stand from the chair).</summary>
        bool Sits(string chair, string id, Pt off)
        {
            if (seatHeld.TryGetValue(chair, out var who) ? who != id : G.D2(off, default) > SitCm * SitCm) return false; // taken, or not yet in it: he walks in first
            seatHeld[chair] = id; seatSeen.Add(chair); return true;
        }
        /// <summary>A person sits once the law has them within this of the chair (they walk in from where they waited).</summary>
        const int SitCm = 20;
        /// <summary>Where the cast stand on foot now, or null (down, in a car, inside).</summary>
        Pt? CastOnFoot(Person p)
        {
            if (p.Down) return null;
            if (p == Driver) { var d = DriverPosRaw(); return d.HasValue && d.Value != CarPos() ? d : null; } // at the wheel he is in the car
            if (p == ValeP)
            {
                if (Vale == ValeAt.Leaving || Vale == ValeAt.Pickup || Vale == ValeAt.WithEnforcer) return ValePosRaw();
                if (Vale == ValeAt.Taking) { var v = ValePosRaw(); return v.HasValue && v.Value != CarPos() ? v : null; }
                return null;
            }
            return PersonPosRaw(p);
        }
        /// <summary>A caller at a call box faces it.</summary>
        Pt CallFace(Walker w) { foreach (var (id, at) in S.CallBoxes) if (id == w.Refuge) return at - w.At; return default; }

        // ---------------------------------------------------------------- paces

        /// <summary>The fastest a body of this kind goes on its own feet (cm a second), the rulebook's run paces: the cell's run for the cell and the
        /// cast (the Enforcer leading Vale at a run), the city's run for its people (the dial's guards among them), a rival's following pace for the
        /// rival crew and fight bodies (officers walk slower), security's for officers and police, a robot's for the patrol and robots. The law never
        /// moves a body farther in a tick than this (nobody jumps), and `BodyCheck` counts any move over it.</summary>
        public int BodyPaceCmS(string kind) => kind switch
        {
            "crowd" or "dial-person" or "dial-guard" => R.CrowdRunSpeed,
            "rival" => RivalPaceCmS, "fight" => Math.Max(RivalPaceCmS, R.SecuritySpeed), "officer" or "police" => R.SecuritySpeed,
            "patrol" or "robot" => Math.Max(Math.Max(R.RobotSpeed, R.PatrolChase), R.RerouteWalkCmS),
            _ => R.RunSpeed, // the cell and the cast
        };
        int RivalPaceCmS => Math.Max(Math.Max(R.RivalsSpeed, Cr.FollowCmS), Math.Max(Cr.BrokenCmS, Cr.LookoutRunCmS));
        /// <summary>A body's pace in cm a tick (rounded up), by the law's kind.</summary>
        int PaceCmT(byte kind, int ix)
        {
            int s = kind switch { BkWalker => BodyPaceCmS("crowd"), BkCrew => BodyPaceCmS("rival"), BkHostile => BodyPaceCmS("fight"), BkOfficer => BodyPaceCmS("officer"), BkPatrol => BodyPaceCmS("patrol"), BkUnit => BodyPaceCmS(Units[ix].Robot ? "robot" : "police"), _ => R.RunSpeed };
            return (s + Hz - 1) / Hz;
        }

        void BodyStore(int i)
        {
            ref var b = ref bds[i];
            var lay = b.At - b.Raw; var bl = new BodyLay { Lay = lay, Raw = b.Raw, Face = b.LayFace, At1 = Tick + 1, Held = b.Moving && b.At == b.Prev, BRef = b.BRef, BN = b.BN, BAt = b.BAt, HeldN = b.HeldN, DetTries = b.DetTries, HeldAt = b.HeldAt, StuckN = b.StuckN, Det = b.Det, Catch = b.Det != null && b.Catch, DetEnd = b.DetEnd, BlkId = b.Blk >= 0 ? bds[b.Blk].Id : null, GiveEnd = b.GiveEnd, GiveAt = b.GiveAt, Slot = b.Mate ? b.Slot : -1,
                RoomFor = b.RoomFor, RoomAt = b.RoomAt, RoomFrom = b.RoomFrom, RoomDir = b.RoomDir, RoomRaw = b.RoomRaw, RoomEnd = b.RoomEnd, SqzId = b.SqzId, SqzEnd = b.SqzEnd, SqzDir = b.SqzDir, AltOn = b.AltOn, Alt = b.Alt, AltStop = b.AltStop, AltUntil = b.AltUntil, RoomDone = b.RoomDone, RoomDoneUntil = b.RoomDoneUntil };
            (bl.SlideV, bl.SlideUntil) = SideAfter(in b); bl.PickWhy = Tick + 1 < b.PickUntil ? b.PickWhy : (byte)0; bl.PickOff = bl.PickWhy != 0 ? b.PickOff : default; bl.PickUntil = bl.PickWhy != 0 ? b.PickUntil : 0; bl.PickComfort = bl.PickWhy != 0 && b.PickComfort; // engine 0.48.0: the side it keeps to, the spot kept (GameBodySides.cs)
            switch (b.Kind)
            {
                case BkCast: castLay[b.Ix] = bl; break;
                case BkPatrol: patrolLay = bl; break;
                case BkOp:
                    {
                        var op = Ops[b.Ix];
                        if (op.Walk != null && b.Cls != BcFixed && !G.Within(b.At, op.Walk.End, R.BodyMinCm + R.BodySpaceCm)) // his walk waits for him: held back, he has not got as far along it as its clock says, so it ends when he arrives; within two radii and a step of its end it ends where the bodies there (a car pulled in on it) let him stand
                        {
                            long got = op.Walk.NearestMm(b.At, op.WalkMm - 10L * G.Dist(b.At, b.Raw) - 1000, op.WalkMm);
                            if (got + WalkSlackMm < op.WalkMm) { op.WalkMm = got; bl.Raw = op.Walk.At(got); bl.Lay = b.At - bl.Raw; }
                        }
                        op.Body = bl; op.BodyWalk = op.Walk; op.Pos = b.At; break;
                    }
                case BkHostile: Hostiles[b.Ix].Body = bl; break;
                case BkCrew: crewLay[b.Ix] = bl; break;
                case BkOfficer: secLay[b.Ix] = bl; break;
                case BkUnit: Units[b.Ix].Body = bl; break;
                case BkWalker: Walkers[b.Ix].Body = bl; break;
            }
        }

        // ---------------------------------------------------------------- the spatial hash (1 m cells; a body is listed in every cell it has stood in this tick)

        static long CellOf(Pt p) => CellKey(FloorDiv(p.X, 100), FloorDiv(p.Y, 100));
        /// <summary>A 1 m cell's key: x in the high word, y's bits in the low (unchecked: a negative y is its bits, as in a Release build).</summary>
        static long CellKey(int cx, int cy) => unchecked(((long)cx << 32) ^ (uint)cy);
        static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);
        void HashAdd(int i, Pt p)
        {
            if (bhN == bhBody.Length) { Array.Resize(ref bhBody, bhN * 2); Array.Resize(ref bhNext, bhN * 2); }
            long c = CellOf(p); bhBody[bhN] = i; bhNext[bhN] = bhHead.TryGetValue(c, out var h) ? h : -1; bhHead[c] = bhN++;
        }

        /// <summary>The nearest body within `reach` of c that body i must respect and that stands inside `need(j)` of c, or -1: every body laid this
        /// tick and every body still to be laid where it stood (so each can always wait where it stood, and the law is never forced into an overlap;
        /// only a tenet that walks, the Enforcer leading Vale, or one that appears, can be).</summary>
        int Blocker(int i, Pt c, bool comfort)
        {
            ref var b = ref bds[i]; if (++bdStamp == int.MaxValue) { Array.Clear(bdMark, 0, bdMark.Length); bdStamp = 1; }
            int cx = FloorDiv(c.X, 100), cy = FloorDiv(c.Y, 100), best = -1; long bd2 = long.MaxValue;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!bhHead.TryGetValue(CellKey(cx + dx, cy + dy), out var e)) continue;
                    for (; e >= 0; e = bhNext[e])
                    {
                        int j = bhBody[e]; if (j == i || bdMark[j] == bdStamp) continue; bdMark[j] = bdStamp;
                        ref var o = ref bds[j];
                        int need = Need(in b, in o, comfort); long d2 = G.D2(c, o.At);
                        if (d2 < (long)need * need && d2 < bd2) { bd2 = d2; best = j; }
                    }
                }
            return best;
        }
        /// <summary>How far apart b must stand from o: two radii always, the narrower squeeze between two squeezing past each other; by choice
        /// (comfort) a person of the city walking gives personal space, and standing keeps it from anyone standing (a guard at his post only two
        /// radii from the mates who come to it), a queue at a stop its step. The cell, the cast and the armed keep two radii (fight.spacing).</summary>
        int Need(in Bd b, in Bd o, bool comfort)
        {
            if (b.SqzId != null && o.SqzId != null && b.SqzId == o.Id && o.SqzId == b.Id) return 2 * R.BodySqueezeCm; // shoulder to shoulder, turned sideways (GameBodyGive.cs)
            int hard = R.BodyMinCm; if (!comfort || b.Cls != BcSoft) return hard; // the cell, the cast and the armed are on business: they keep two radii (the fight's spacing)
            if (b.Moving) return R.BodySpaceCm;
            if (o.Moving) return hard;
            if (b.Form == FmQueue && o.Form == FmQueue && G.D2(b.Raw, o.Raw) < (long)hard * hard) return R.BodyQueueCm; // a queue at one stop
            if (b.Armed && o.Armed && b.Raw == o.Raw) return b.Mate ? R.BodySpaceCm : hard; // guards at one post point: the holder keeps it, his mates keep personal space from him (FormGoal sends them to crew spots)
            return b.Cls == BcSoft ? R.BodySpaceCm : hard;
        }
        /// <summary>Ground a body may stand on: its own place, where it stands, or a free cell of the site's walk grid (no wall) with no standing car's
        /// body within a radius of it (a body touches a car's side; the walk mask's wider margin is for routes). One standing where no body stands (a
        /// car came to stand on it; its own place left it on a solid cell) steps off it, a pace at a time (FixOff); its own place off free ground it
        /// steps into (StepInCm).</summary>
        bool BodyGround(in Bd b, Pt c) => c == b.Raw || c == b.At || (b.Fix == FixOff ? G.Within(c, b.Prev, b.Pace + 1) : S.Grid.Free(c) && OffCars(c)) || (G.Within(c, b.Own, StepInCm) && !S.Grid.Free(b.Own)) || (!S.Grid.InGrid(c) && !S.Grid.InGrid(b.Raw)); // off the sheet the street goes on: a body whose own place has gone off it walks out over its edge
        /// <summary>A place off the walk grid's free ground (a post in a facade's recess, a door point) is stepped into across the last of the solid
        /// ground, as its own walk does: within this of it.</summary>
        const int StepInCm = 160;
        readonly List<StandingCar> bodyCars = new List<StandingCar>();
        bool OffCars(Pt c) { foreach (var car in bodyCars) if (OnCar(car, c)) return false; return true; }
        /// <summary>A body at c would touch this standing car (its body grown by a radius).</summary>
        bool OnCar(in StandingCar car, Pt c)
        {
            long r = R.BodyRadiusCm, hx = car.Heading.X, hy = car.Heading.Y, n2 = hx * hx + hy * hy, dx = c.X - car.At.X, dy = c.Y - car.At.Y, hl = car.Hl(R) + r, hw = car.Hw(R) + r;
            long along = dx * hx + dy * hy, across = dx * hy - dy * hx;
            return along * along <= hl * hl * n2 && across * across <= hw * hw * n2;
        }

        /// <summary>The law's state for the checkpoint hash: every lay and sector facing still live, by kind and id (no bytes while nobody is moved),
        /// and the give and take while it runs (making room, a squeeze, a change of plans).</summary>
        internal void HashBodies(Hash.H h)
        {
            if (!R.BodiesOn || Range) return;
            void put(string id, in BodyLay b)
            {
                if (!b.Live(Tick)) return;
                if (b.Lay != default || b.Face != default) h.S(id).P(b.Lay).P(b.Face).P(b.Raw).I(b.DetEnd).I(b.GiveEnd).P(b.GiveAt);
                if (b.RoomFor != null) h.S("room").S(id).S(b.RoomFor).P(b.RoomAt).P(b.RoomRaw).I(b.RoomEnd);
                if (b.RoomDone != null) h.S("roomed").S(id).S(b.RoomDone).I(b.RoomDoneUntil);
                if (b.SqzId != null) h.S("squeeze").S(id).S(b.SqzId).I(b.SqzEnd);
                if (b.AltOn) h.S("alt").S(id).P(b.Alt).S(b.AltStop ?? "").I(b.AltUntil);
                if (b.SlideV != default) h.S("slid").S(id).P(b.SlideV).I(b.SlideUntil);
                if (b.PickWhy != 0) h.S("pick").S(id).I(b.PickWhy).P(b.PickOff).I(b.PickUntil).I(b.PickComfort ? 1 : 0);
            }
            for (int i = 0; i < 4; i++) put(Sim.Op.Name(Ops[i].Id), Ops[i].Body);
            for (int i = 0; i < Crew.Count; i++) put(Crew[i].Id, crewLay[i]);
            foreach (var x in Hostiles) put(x.Id, x.Body);
            for (int k = 0; k < secLay.Length; k++) put(SecGuns[k].Id, secLay[k]);
            foreach (var u in Units) { put(u.Id, u.Body); if (u.Slot > 0) h.S("slot").S(u.Id).I(u.Slot); }
            foreach (var w in Walkers) put(w.P.Id, w.Body);
            foreach (var kv in seatHeld) h.S("seat").S(kv.Key).S(kv.Value);
            foreach (var kv in refugeOut) h.S("out").S(kv.Key).I(kv.Value);
        }
    }
}
