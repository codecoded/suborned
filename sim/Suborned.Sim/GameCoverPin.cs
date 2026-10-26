// Brief 039 B, cover and pin (engine 0.35.0; Sky picked B on 2026-10-26 through Main, with his three calls all yes; GDD v0.28.0 §06;
// Motive's rules and values from briefs/039/proposal-2026-10-26.md and the probe briefs/039/motive/probe/; Glyph's briefs/039/glyph/b-final.md
// and Forma's briefs/039/forma/pin-poses.md for what the host reads). Rulebook weapons.oneInput, focus, takeCover, pin, moves; each absent
// (a rulebook pinned before 0.35.0) plays as engine 0.34, and none is read with the fight lab on (the lab keeps its own rules).
//  · One input: cease, stance, panic and free are retired (unknown-verb); weapons free is always on (GameOrders.Check, the Game constructor).
//  · The sticky focus: a fire order waits out of reach or line while the agent fights on its own, then takes its target back (CellShooter).
//  · Readable cover: a drawn agent's move in a fight that ends by a cover point within reach snaps to its kneel point (GameFightLab SnapPoint).
//  · The pin: anyone shot at, hit or miss, is pinned for pin.s (renewed by each round); Covering fire pins everyone it covers until it ends.
//    A pinned shooter fires at pin.mod; a pinned final-fight hostile holds (GameAi FightBody, GameKite KiteBody). pin.cell: the cell too.
//  · One move each: Mark, Talk down, Steam, Covering fire (below), each ready again moves.ready after it is given.
// Every rule reads hashed state (ShotByTick, the dictionaries below) and integer distances; ties by id. The host reads change nothing.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>How a person's body reads in a fight (Forma's pin poses; Glyph's b-final): stand, kneel (in cover and not pinned), pinned-cover
    /// (ducked behind it), pinned-open (cowering), down; "" for an id that is nobody on the street. Cover: the cover point he is at ("" none);
    /// KneelAt: that point (the site's point 0.85 m off the prop, on the side it shields), Faces: the way it shields (toward the prop and the
    /// shooter beyond it); Shields: it shields him against his threat now (his last shooter within pin.s, else his target); PinLeft: ticks of
    /// pin left (0 none: it drives the peek, which starts 1.2 s after the last round); Moving: he walks this tick. Read-only.</summary>
    public struct Posture { public string Id, Kind, Cover; public Pt KneelAt, Faces; public bool Shields, Moving; public int PinLeft; }

    /// <summary>A round that landed this tick (Resolve), for the host's kick, flash, flinch, chips and dust: who fired from where at whom, hit or
    /// not, its damage, whether cover took it (a miss on a still target beside cover inside its arc: chips on the prop), whether it was Covering
    /// fire (never a hit), and whether the target went down. Output only: never hashed, cleared at each fight tick.</summary>
    public struct ShotEvent { public int Tick; public string By, Target; public Pt From, At; public bool Hit, Covered, Covering, Down; public int Damage; }

    /// <summary>The move-snap test for a move by a drawn agent ending at a point (Glyph b-final: the snapped ring and the struck shield): whether it
    /// snaps, to which cover point and where it ends (the kneel point), the way that point shields, and the agent's focus from that end: the
    /// cover the focus holds ("" none) and whether it still counts against a shooter standing there (false: the end flanks him, or he is marked).</summary>
    public struct MoveSnap { public bool Snaps; public string Cover; public Pt EndAt, Faces; public string Focus, FocusCover; public bool FocusCovered; }

    /// <summary>An agent's one move (brief 039): its verb, the tick it is ready again (0: ready), and whether it runs now (Covering fire).</summary>
    public struct MoveRead { public string Verb; public int ReadyAt, ReadyIn; public bool Running; }

    /// <summary>The Scout's Mark (""/-1 when none stands): the marked hostile, the ticks left, and his next move: the end of the walk he is on
    /// (null while he stands).</summary>
    public struct MarkRead { public string Target; public int Left; public Pt? Next; }

    /// <summary>A heat-main grille the Tech opened: its id, where it is, the ticks left, the radius no line passes.</summary>
    public struct SteamRead { public string Grille; public Pt At; public int Left, RadiusCm; }

    /// <summary>The Enforcer's Covering fire while it runs (Target "" when none): its target, its first round's tick, its end, the radius pinned.</summary>
    public struct CoverFireRead { public string Target; public int From, Until, RadiusCm; }

    /// <summary>The sticky focus as the host draws it (Glyph's lock disc: why it waits): the fire order's target ("" none), and its state:
    /// walking-in, aiming (taken, its first shot not yet), firing, out-of-reach, no-line (the focus waits; the agent fights on its own).</summary>
    public struct FocusRead { public string Target, State; }

    public sealed partial class Game
    {
        // ---------------------------------------------------------------- the switches (rulebook, never the lab)

        /// <summary>One input plays this run (weapons.oneInput, unless a test start plays the old controls; never with the lab on).</summary>
        public bool OneInputOn => (Start.OneInput ?? R.OneInput) && !Lab && !Start.Pre039; // a test start may play the old controls (Start.OneInput)
        bool StickyOn => R.StickyFocus && !Lab && !Start.Pre039;
        bool PinOn => R.PinS > 0 && !Lab && !Start.Pre039;
        /// <summary>Whether the cell's agents are pinned by hostile fire: the start's pinCell, else the rulebook's weapons.pin.cell (Motive sets it).</summary>
        public bool PinCellOn => Start.PinCell ?? R.PinCell;
        bool MovesOn => R.MovesOn && !Lab && !Start.Pre039;

        // ---------------------------------------------------------------- state (hashed in HashCoverPin; no bytes until any is live)

        readonly SortedDictionary<string, int> pinUntil = new SortedDictionary<string, int>(StringComparer.Ordinal); // Covering fire's pins, by gun id
        readonly int[] moveReady = { 0, 0, 0, 0 }; // the tick each operative's move is ready again
        string markT = ""; int markUntil = -1;
        readonly List<(string id, Pt at, int until)> steams = new List<(string, Pt, int)>();
        string cfT = ""; int cfFrom = -1, cfUntil = -1, cfNext = -1; // Covering fire: target, first round, end, next round
        readonly SortedSet<string> talked = new SortedSet<string>(StringComparer.Ordinal); // rivals talked down (they count to the crew's break)
        readonly List<ShotEvent> shotEvents = new List<ShotEvent>(); // output only

        internal void HashCoverPin(Hash.H h)
        {
            foreach (var kv in pinUntil) if (kv.Value > Tick) h.S("pin").S(kv.Key).I(kv.Value);
            for (int k = 0; k < 4; k++) if (moveReady[k] > Tick) h.S("ready").I(k).I(moveReady[k]);
            if (markT.Length > 0 && markUntil > Tick) h.S("mark").S(markT).I(markUntil);
            foreach (var (id, at, until) in steams) if (until > Tick) h.S("steam").S(id).P(at).I(until);
            if (cfT.Length > 0) h.S("cover-fire").S(cfT).I(cfFrom).I(cfUntil).I(cfNext);
            foreach (var x in talked) h.S("talked").S(x);
        }

        // ---------------------------------------------------------------- the pin

        /// <summary>A gun's pin left, in ticks counting this one (0: not pinned): shot at within pin.s (Gun.ShotByTick, hit or miss; an operative
        /// only with pin.cell): 21 at the tick the round lands, 1 at 2.0 s after it; or under Covering fire until it ends.</summary>
        int PinLeftOf(Gun g)
        {
            if (!PinOn || g.Down) return 0;
            int left = pinUntil.TryGetValue(g.Id, out var u) && u > Tick ? u - Tick : 0;
            if (g.ShotByTick >= 0 && (PinCellOn || !IsOpId(g.Id))) { int e = g.ShotByTick + T(R.PinS) + 1 - Tick; if (e > left) left = e; } // pinned while Tick − ShotByTick ≤ pin.s (Motive's probe): a carbine's next round, 2.0 s on, lands before the pin lapses
            return left;
        }
        bool Pinned(Gun g) => PinLeftOf(g) > 0;
        /// <summary>A pinned final-fight hostile holds (weapons.pin.hold): no advance, no kite step. A breaking one runs on.</summary>
        bool PinHolds(Gun g) => PinOn && R.PinHold && g.St != "break" && Pinned(g);

        /// <summary>The cell's accuracy after every engine-0.34 modifier (ShotAcc, out of the lab): a pinned shooter fires at weapons.pin.mod.</summary>
        int PinAcc(Gun g, int acc) => PinOn && Pinned(g) ? acc * R.PinMod / 1000 : acc;
        /// <summary>Mark: the marked hostile's cover doesn't count against the cell's shots while it stands.</summary>
        bool MarkedFor(Gun shooter, string t) => MovesOn && IsOpId(shooter.Id) && t == markT && Tick < markUntil;

        /// <summary>A gun by id without making one (GunOf makes a crowd person's on first use): for the host's reads.</summary>
        Gun? GunPeek(string id)
        {
            if (IsOpId(id)) return OpOf(Sim.Op.Parse(id)).Gun;
            if (civGuns.TryGetValue(id, out var c)) return c;
            if (CivilianOf(id) is Person) return null;
            return GunOf(id);
        }

        // ---------------------------------------------------------------- the steam screen

        /// <summary>A line from a to b passes within steam.r of an open grille (integer distance from its centre to the segment).</summary>
        bool Steamed(Pt a, Pt b)
        {
            if (steams.Count == 0) return false;
            long r2 = (long)R.SteamRCm * R.SteamRCm;
            foreach (var (_, c, until) in steams)
            {
                if (Tick >= until) continue;
                long dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy, t = l2 == 0 ? 0 : (long)(c.X - a.X) * dx + (long)(c.Y - a.Y) * dy;
                long px, py;
                if (l2 == 0 || t <= 0) { px = a.X; py = a.Y; } else if (t >= l2) { px = b.X; py = b.Y; } else { px = a.X + dx * t / l2; py = a.Y + dy * t / l2; }
                long ex = c.X - px, ey = c.Y - py; if (ex * ex + ey * ey <= r2) return true;
            }
            return false;
        }

        /// <summary>The heat-main grilles of this run: the start's test grilles (the site carries none yet: which grates are heat mains is Motive's
        /// and Vantage's), in id order.</summary>
        List<(string id, Pt at)> Grilles()
        {
            var l = new List<(string, Pt)>(Start.Grilles);
            l.Sort((x, y) => string.CompareOrdinal(x.Item1, y.Item1));
            return l;
        }

        // ---------------------------------------------------------------- the moves (orders)

        static string MoveVerbOf(OpId id) => id == OpId.Scout ? "mark" : id == OpId.Fixer ? "talk-down" : id == OpId.Tech ? "steam" : "covering-fire";
        bool moveDry;

        /// <summary>The four moves (brief 039; rulebook weapons.moves): each the one agent's, ready again moves.ready after it is given.</summary>
        string MoveOrder(Op op, Order o)
        {
            if (!MovesOn) return No("unknown-verb", $"verb '{o.Do}' is unknown (unknown = reject)");
            if (MoveVerbOf(op.Id) != o.Do)
                return o.Do == "mark" ? No("only-scout", "Mark is the Scout's move") : o.Do == "talk-down" ? No("only-fixer", "Talk down is the Fixer's move")
                    : o.Do == "steam" ? No("only-tech", "Steam is the Tech's move") : No("only-enforcer", "Covering fire is the Enforcer's move");
            int ready = moveReady[(int)op.Id];
            if (Tick < ready) return No("move-cooling", $"the {o.Do} is ready again at {ready}", tick: ready);
            string name = Sim.Op.Name(op.Id);
            switch (o.Do)
            {
                case "mark":
                {
                    if (!Combatant(o.Target) || !(PosOf(o.Target) is Pt at)) return No("no-target", "no hostile to mark");
                    if (!G.Within(op.Pos, at, R.MarkReachCm)) return No("out-of-range", $"out of the Scout's reach ({R.MarkReachCm / 100} m)", m: R.MarkReachCm / 100);
                    if (!SeesId(op.Pos, o.Target, at)) return No("no-clear-line", "the Scout can't see him");
                    if (moveDry) return "";
                    markT = o.Target; markUntil = Tick + T(R.MarkS); Spent(op);
                    Note("move.mark", $"The Scout marks {o.Target}: for {R.MarkS} s his cover doesn't count.");
                    return "";
                }
                case "talk-down":
                {
                    var h = HostileOf(o.Target);
                    if (h == null) return SecIndex(o.Target) >= 0 || UnitOf(o.Target) != null || o.Target == "guard" || o.Target == "patrol" ? No("talk-police", "never the police") : No("no-target", "no rival to talk down");
                    if (!IsRival(h.Id)) return No("talk-police", "never the police");
                    if (!h.Present || h.Down || h.Gun.St == "break" || h.Gun.St == "gone" || (CrewOf(h.Id) is CrewMember cm && !cm.Fight)) return No("no-target", "no rival to talk down");
                    var at = h.Pos(Tick);
                    if (!G.Within(op.Pos, at, R.TalkReachCm)) return No("out-of-range", $"out of the Fixer's reach ({R.TalkReachCm / 100} m)", m: R.TalkReachCm / 100);
                    if (!Sees(op.Pos, at)) return No("no-clear-line", "the Fixer can't see him");
                    int down = 0; foreach (var x in Hostiles) if (IsRival(x.Id) && x.Down && (!Range || x.Wave == h.Wave)) down++;
                    if (down < R.TalkNeedsDown) return No("talk-none-down", "none of the crew is down yet");
                    if (R.TalkHurt && h.Gun.Health >= h.Gun.Max) return No("talk-not-hurt", $"{h.Id} isn't hurt");
                    if (moveDry) return "";
                    Spent(op); Engage(op, o, R.TalkShoutS); // the shout: busy in the open, no firing (an act: it ends her fire order)
                    ReleaseCover(h.Gun); LoseTarget(h.Gun); h.Gun.St = "break"; h.Speed = rivSpeed; talked.Add(h.Id);
                    var carAt = Range ? S.Stand(S.RivalsPark) : RivalCarPose() is var rcp && rcp.HasValue ? Walk.NearestFree(rcp.Value.pos) ?? rcp.Value.pos : S.Stand(S.RivalsPark);
                    h.BreakTo = carAt; MoveTo(h, carAt); h.Why = "break";
                    Note("move.talk-down", $"The Fixer talks {h.Id} down{(h.Blocker ? " off the road" : "")}: he lowers his gun and walks off.");
                    RivalDown(h); // he counts to the crew's break as one down
                    return "";
                }
                case "steam":
                {
                    var gs = Grilles(); Pt? at = null; bool any = false;
                    foreach (var (id, p) in gs) { if (G.Within(op.Pos, p, R.SteamReachCm)) any = true; if (id == o.Target) at = p; }
                    if (!at.HasValue) return any ? No("no-target", $"'{o.Target}' is no heat-main grille") : No("steam-no-grille", "no heat-main grille in reach");
                    if (!G.Within(op.Pos, at.Value, R.SteamReachCm)) return No("out-of-range", $"out of the Tech's reach ({R.SteamReachCm / 100} m)", m: R.SteamReachCm / 100);
                    if (moveDry) return "";
                    steams.RemoveAll(x => x.id == o.Target || x.until <= Tick);
                    steams.Add((o.Target, at.Value, Tick + T(R.SteamS))); Spent(op);
                    Note("move.steam", $"The Tech opens the heat main at {o.Target}: steam for {R.SteamS} s.");
                    return "";
                }
                default: // covering-fire
                {
                    var g = op.Gun;
                    if (g.W == null) return No("no-weapon", $"{o.Op} carries no weapon (Arms)");
                    if (op.Carrying.HasValue || op.CarryBody.Length > 0) return No("fire-carrying", "carrying, the operative doesn't fire");
                    if (!Combatant(o.Target) || !(PosOf(o.Target) is Pt at)) return No("no-target", "no hostile to cover");
                    if (!G.Within(op.Pos, at, g.W.MaxCm)) return No("out-of-range", $"out of range ({g.W.MaxCm / 100} m)", m: g.W.MaxCm / 100);
                    if (!SeesId(op.Pos, o.Target, at)) return No("no-clear-line", "no clear line");
                    if (moveDry) return "";
                    if (g.Order.Length > 0) { Note("fire.cancelled", $"{name} stops firing on {g.Order} for the covering fire."); OrderEnd(op, false); }
                    if (!g.Drawn || g.HolsterAt >= 0) { StartDraw(op); g.DrawnBy = "fire"; }
                    cfT = o.Target; cfFrom = Math.Max(Tick, g.DrawReady); cfNext = cfFrom; cfUntil = cfFrom + T(R.CoverFireS); Spent(op);
                    Note("move.covering-fire", $"The Enforcer lays down covering fire on {o.Target} for {R.CoverFireS} s{(CarriesVale(op) ? ", one-handed, holding Vale" : "")}.");
                    return "";
                }
            }
        }

        void Spent(Op op) => moveReady[(int)op.Id] = Tick + T(R.MoveReadyS);

        void EndCoveringFire(string why)
        {
            if (cfT.Length == 0) return;
            Note("move.covering-fire-ends", $"The Enforcer's covering fire ends ({why}).");
            cfT = ""; cfFrom = cfUntil = cfNext = -1;
        }

        /// <summary>Covering fire, each fight tick before the cell shoots: while it runs and its target is a combatant in reach and line, everyone
        /// within moves.cover.r of the target is pinned until it ends, and a round goes at the target every moves.cover.every (a shot that never
        /// hits: it pins the target and costs as gunfire). It ends at its time, the Enforcer down, inside or in the car.</summary>
        void CoveringFireStep()
        {
            if (cfT.Length == 0) return;
            var op = OpOf(OpId.Enforcer);
            if (Tick >= cfUntil) { EndCoveringFire("its time"); return; }
            if (op.Down || op.Inside || op.InCar || op.Gun.W == null) { EndCoveringFire(op.Down ? "down" : "off the street"); return; }
            if (Tick < cfFrom || !Ready(op.Gun)) return;
            if (!Combatant(cfT) || !(PosOf(cfT) is Pt at) || !G.Within(op.Pos, at, op.Gun.W.MaxCm) || !SeesId(op.Pos, cfT, at)) return; // no line: it waits (the time runs)
            long r2 = (long)R.CoverFireRCm * R.CoverFireRCm;
            foreach (var id in CombatantIds())
            {
                if (!Combatant(id) || !(PosOf(id) is Pt p) || G.D2(p, at) > r2) continue;
                var gg = GunOf(id); if (gg == null || gg.Down) continue;
                if (!pinUntil.TryGetValue(gg.Id, out var u) || u < cfUntil) pinUntil[gg.Id] = cfUntil;
            }
            if (Tick < cfNext) return;
            var g = op.Gun; g.Fired = g.Engaged = true;
            pending.Add(new PendingShot { By = "enforcer", T = cfT, G = g, At = at, From = op.Pos, D = Dist(op.Pos, at), Acc = 0, Dmg = 0, Hit = false, Covering = true });
            cfNext = Tick + R.CoverFireEveryTicks; opShotTick[(int)OpId.Enforcer] = Tick;
        }

        /// <summary>Why a move would be refused now, without giving it ("" taken): the move disc's state (Glyph: ready, cooling, shut) and Fable's code.</summary>
        public Answer PreviewMove(Order o)
        {
            var saved = refusal; moveDry = true;
            try
            {
                if (Over) return AnswerOf(No("mission-over", "the mission is over"));
                var op = OpOf(Sim.Op.Parse(o.Op));
                string why = op.Down ? No("agent-down", $"the {o.Op} is down") : op.Inside ? No("enforcer-inside", "the Enforcer is inside the Office")
                    : op.Busy != null ? No("agent-busy", $"{o.Op} is busy until {op.BusyEnd}", tick: op.BusyEnd) : op.InCar ? No("in-car", $"the {o.Op} is in the car: a move steps out first") : MoveOrder(op, o);
                return AnswerOf(why);
            }
            finally { moveDry = false; refusal = saved; }
        }

        // ---------------------------------------------------------------- the host's reads (change nothing)

        /// <summary>The ticks of pin left on a person (0: not pinned). Read-only.</summary>
        public int PinLeft(string id) => GunPeek(id) is Gun g ? PinLeftOf(g) : 0;

        /// <summary>Every person pinned now (the cell, then the city's guns and the final fight's bodies, by id), with the ticks left. Read-only.</summary>
        public List<(string id, int left)> Pins()
        {
            var l = new List<(string, int)>(); if (!PinOn) return l;
            foreach (var op in Ops) { int n = PinLeftOf(op.Gun); if (n > 0) l.Add((Sim.Op.Name(op.Id), n)); }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var ids = new List<string>(); foreach (var id in CombatantIds()) if (seen.Add(id)) ids.Add(id);
            ids.Sort(StringComparer.Ordinal);
            foreach (var id in ids) { var g = GunPeek(id); if (g == null) continue; int n = PinLeftOf(g); if (n > 0) l.Add((id, n)); }
            return l;
        }

        /// <summary>A person's posture (Forma's pin poses): see Posture. Read-only.</summary>
        public Posture PostureOf(string id)
        {
            var p = new Posture { Id = id, Kind = "", Cover = "" };
            var g = GunPeek(id); var at = PosOf(id);
            if (g == null || !at.HasValue) return p;
            if (g.Down) { p.Kind = "down"; return p; }
            p.Moving = Moving(id); p.PinLeft = PinLeftOf(g);
            CoverPoint? c = null;
            if (g.Cover.Length > 0 && CoverById(g.Cover) is CoverPoint held && G.Within(at.Value, held.At, Math.Max(R.CoverSnapCm, CoverSnapCm))) c = held;
            if (c == null) { long bd = long.MaxValue; int sec = Tick / Hz; foreach (var x in LiveCover) { if (!x.Present(sec) || x.Q < R.CoverQPartial) continue; long d = G.D2(at.Value, x.At); if (d <= (long)R.CoverSnapCm * R.CoverSnapCm && d < bd) { bd = d; c = x; } } }
            bool inCover = c != null && !p.Moving;
            if (c != null) { p.Cover = c.Id; p.KneelAt = c.At; p.Faces = c.Faces; }
            string threat = g.ShotByTick >= 0 && Tick - g.ShotByTick < T(Math.Max(1, R.PinS)) && g.ShotBy.Length > 0 ? g.ShotBy : g.Target;
            if (c != null && threat.Length > 0 && PosOf(threat) is Pt tp) p.Shields = CoverRules.Protects(c, tp, Tick / Hz, R.PointBlankCm);
            p.Kind = p.PinLeft > 0 ? (inCover ? "pinned-cover" : "pinned-open") : inCover ? "kneel" : "stand";
            return p;
        }

        /// <summary>The rounds that landed this tick, in the order they resolved (see ShotEvent). Read-only.</summary>
        public IReadOnlyList<ShotEvent> ShotsThisTick => shotEvents;

        /// <summary>The move-snap test (see MoveSnap): what a move by the agent to `to` would end on now, and his focus's cover from there. Read-only.</summary>
        public MoveSnap PreviewMoveSnap(OpId opId, Pt to)
        {
            var op = OpOf(opId); var m = new MoveSnap { EndAt = to, Cover = "", Focus = op.Gun.Order, FocusCover = "" };
            var c = PreviewSnap(opId, to);
            if (c != null) { m.Snaps = true; m.Cover = c.Id; m.EndAt = c.At; m.Faces = c.Faces; }
            if (m.Focus.Length > 0 && PosOf(m.Focus) is Pt fp)
            {
                var fg = GunPeek(m.Focus);
                if (fg != null && fg.Cover.Length > 0) m.FocusCover = fg.Cover;
                m.FocusCovered = !MarkedFor(op.Gun, m.Focus) && CoverMod(m.Focus, fp, m.EndAt) < 1000;
            }
            return m;
        }

        /// <summary>The agent's move: its verb, when it is ready again, whether it runs now. Read-only.</summary>
        public MoveRead MoveOf(OpId id)
        {
            int r = moveReady[(int)id];
            return new MoveRead { Verb = MovesOn ? MoveVerbOf(id) : "", ReadyAt = r > Tick ? r : 0, ReadyIn = Math.Max(0, r - Tick), Running = id == OpId.Enforcer && cfT.Length > 0 };
        }

        /// <summary>The Scout's Mark standing now (Target "" when none). Read-only.</summary>
        public MarkRead MarkOf()
        {
            if (markT.Length == 0 || Tick >= markUntil) return new MarkRead { Target = "", Left = 0 };
            var h = HostileOf(markT); Pt? next = h != null && h.Moving(Tick) ? h.Run!.End : (Pt?)null;
            return new MarkRead { Target = markT, Left = markUntil - Tick, Next = next };
        }

        /// <summary>The grilles steaming now, by id. Read-only.</summary>
        public List<SteamRead> Steams()
        {
            var l = new List<SteamRead>();
            foreach (var (id, at, until) in steams) if (until > Tick) l.Add(new SteamRead { Grille = id, At = at, Left = until - Tick, RadiusCm = R.SteamRCm });
            l.Sort((a, b) => string.CompareOrdinal(a.Grille, b.Grille));
            return l;
        }

        /// <summary>The Enforcer's Covering fire while it runs (Target "" when none). Read-only.</summary>
        public CoverFireRead CoveringFireOf() => cfT.Length == 0 ? new CoverFireRead { Target = "" } : new CoverFireRead { Target = cfT, From = cfFrom, Until = cfUntil, RadiusCm = R.CoverFireRCm };

        /// <summary>The heat-main grilles this run knows (the start's test grilles; the site carries none yet), by id. Read-only.</summary>
        public List<(string id, Pt at)> HeatMains() => Grilles();

        /// <summary>The sticky focus's state (see FocusRead). Read-only.</summary>
        public FocusRead FocusOf(OpId id)
        {
            var op = OpOf(id); var g = op.Gun; var f = new FocusRead { Target = g.Order, State = "" };
            if (g.Order.Length == 0) return f;
            if (op.CloseWalk) { f.State = "walking-in"; return f; }
            if (!(PosOf(g.Order) is Pt at) || g.W == null) { f.State = "no-line"; return f; }
            if (!G.Within(op.Pos, at, g.W.MaxCm)) { f.State = "out-of-reach"; return f; }
            if (!SeesId(op.Pos, g.Order, at)) { f.State = "no-line"; return f; }
            f.State = g.Target == g.Order ? "firing" : "aiming"; // aiming: back from fighting on his own, the aimNew before the first shot
            return f;
        }
    }
}
