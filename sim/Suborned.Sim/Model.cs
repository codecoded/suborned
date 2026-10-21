// The engine's state pieces: people (the cast the rules act on), the cell's four operatives, orders, the event log, the action card.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Suborned.Sim
{
    /// <summary>A memory entry (rulebook memory, K1): {act, by, place, time, witnessed}; `By` is an operative's face, only an id.</summary>
    public struct Mem
    {
        public string Act, By, Place; public int Tick; public bool Witnessed;
        public string? Told; // a told entry's facts, '|'-joined (rulebook memory.acts.told: as told)
    }

    /// <summary>A simulated person (people.rules tiers.simulated): routine, need, temperament, memory, and what the cell knows of them.</summary>
    public sealed class Person
    {
        public string Role = "", Id = "", Name = "", Temperament = "", Need = "", Job = "";
        /// <summary>The need's record (brief 017, sim people `record`, `size`, `holder`): the catalogue's record of the `Need` kind, its size and
        /// its holder ("beyond": nobody on the sheet; phase 2 binds a person). "" when the host loaded no people file. Phase 1: read, never acted on.</summary>
        public string Record = "", Size = "", Holder = "";
        /// <summary>The city's own security (§05: guards, patrol officers, investigators, watchers) are no civilians: violence they see is not §05's "seen by a civilian".</summary>
        public bool Civilian => Job != "guard" && Job != "patrol-officer" && Job != "investigator" && Job != "watcher" && Role != "guard";
        public readonly int[] Sc = new int[4];
        public readonly List<Mem> Memory = new List<Mem>();
        public bool Turned, NeedMet, Down;
        public bool KRoutine, KNeed, KTemperament, KSecret; // the cell's knowledge of this person
        public string Secret = "";                 // people.rules secrets: affair, record, or none (Motive, motive-lovers.md)
        public Person? Lover;                       // the other end of a lover tie, when both are cast
        public int Summoned;                        // times this person came to a lover's call this shift
        public readonly SortedSet<string> KnownRefused = new SortedSet<string>(StringComparer.Ordinal); // approaches the cell saw this person refuse: the card's ✗
        public string Suspected = "";              // a need the briefing names (§07 cast: Inez in debt, the driver needs cash): shown as suspected, unlocks nothing
        public string Place = ""; // the place they stand at for the mission (memory's place)
        public bool Knows(string what) => what == "routine" ? KRoutine : what == "need" ? KNeed : what == "temperament" ? KTemperament : what == "refused" ? KnownRefused.Count > 0 : what == "secret" ? KSecret && Secret.Length > 0 : throw new FormatException($"knowledge '{what}'");
        /// <summary>Learn a fact. A secret learned on one lover is learned on both, with the tie (people.rules secrets.affair).</summary>
        public void Learn(string what)
        {
            if (what == "routine") KRoutine = true; else if (what == "need") KNeed = true; else if (what == "temperament") KTemperament = true;
            else if (what == "secret") { if (Secret.Length == 0) return; KSecret = true; if (Secret == "affair" && Lover != null) Lover.KSecret = true; }
            else throw new FormatException($"knowledge '{what}'");
        }
        public void Forget(string what) { if (what == "routine") KRoutine = false; else if (what == "need") KNeed = false; else if (what == "temperament") KTemperament = false; else if (what == "secret") KSecret = false; }
        public int Count(string act) { int n = 0; foreach (var m in Memory) if (m.Act == act) n++; return n; }
        public bool Has(string act) { foreach (var m in Memory) if (m.Act == act) return true; return false; }
    }

    public enum OpId { Scout = 0, Fixer = 1, Tech = 2, Enforcer = 3 }

    /// <summary>Anyone who can be shot, and what they shoot with (rulebook weapons; Sky 2026-10-15, briefs/011/motive-fights.md): health, the
    /// weapon and its state, the standing fire order or stance, and the integer accumulator that makes hits deterministic (no dice).</summary>
    public sealed class Gun
    {
        public readonly string Id;                     // an operative, "guard", "patrol", "security-1", "rivals", a role, a hostile's id
        public Weapon? W; public bool Trained;         // null: unarmed (an operative without Arms, a civilian)
        public int Health, Max; public bool Down;
        public bool Drawn; public int DrawReady, HolsterAt = -1, DrawTick = -1; // drawing until DrawReady; holstering until HolsterAt
        public string Stance = "return";              // hold, return, free (an operative's)
        /// <summary>Brief 030: how an operative's weapon was drawn: key (a draw order: stays drawn), fire or panic (the fight drew it: weapons.autoHolster), "" holstered.</summary>
        public string DrawnBy = "";
        public string Order = ""; public bool Force;   // a standing fire order: until the target is down, cease, or another order
        public int OrderTick = -1;                     // when the standing fire order was given (tuning.md §9.1: an order given in the final fight ends at fight.over; one given after it stands)
        public string Target = ""; public int Acc, NextShot, LostAt = -1; // the target aimed at, its accumulator, the next shot, when the line broke
        public string ShotBy = "";                     // the last shooter who fired on this one (stance return answers them)
        public bool Fired, Engaged, Hit;               // has fired; in action (a new target costs aimNew, not react); hit once (its Trail paid)
        public int ShotByTick = -1;                    // when ShotBy last fired on this one (Pulse §1.5: "whoever is shooting at me" for line-lapse)
        /// <summary>Tuning.md §12.2 (engine 0.17.0): the last tick each operative (op id order) fired on this one, -1 never; police answer every
        /// operative who fired on them within weapons.lineLapse. Hashed only where it says more than ShotBy and ShotByTick, and only while live.</summary>
        public readonly int[] OpShot = { -1, -1, -1, -1 };
        // the fight AI's state (Pulse, briefs/011/pulse-fight-ai.md §2): one machine for every hostile body
        public string St = "";                         // routine, alerted, advance, engage, hold, break, gone ("" for the cell and civilians)
        public int Until, NoFoeSince = -1, StepAt = -100000, UnprotSince = -1, Class, LostTick = -1, SightLostAt = -1;
        public string Cover = "", LostTarget = "";     // the cover point it holds; the target it last lost (its aim stays warm)
        // brief 027 (EXPERIMENTAL, fight.lab; hashed only with the lab on): a volley's shots still to fire and the charge each one paid (refunded
        // if it ends early), and the standing fire order's then (free: back to at will when its target is down; stop: today's)
        public int VolleyLeft, VolleyPer; public string Then = "";
        /// <summary>Brief 027 aim memory (tuning.md §6.1; lab only, null with the lab off): this shooter's count per target it has fired at, for
        /// the whole mission. Taking a target resumes its count (a new one starts at fight.lab.accStart); cease, holster, a switch keep it.</summary>
        public SortedDictionary<string, int>? Mem;
        /// <summary>Brief 027 F2 (lab kite only; hashed in Game.HashKite): the alarm (first and latest shot on its side, the shooter), the last
        /// answer, the walk spent on alarms this run (cm) and where it stood last tick, and whether the chase budget is spent.</summary>
        public int KiteAt = -1, KiteSeen = -1, KiteLast = -1, KiteWalked; public string KiteBy = ""; public Pt KitePrev; public bool KiteHasPrev, KiteCapped;
        /// <summary>Brief 027 retest (lab kite only; hashed in Game.HashKite): the tick the budget was spent (re-arm, Pulse retest-fixes.md §1c), the tick a hold began (the crew, §5.2), the last cover step of a fight body against an out-of-reach shooter (Motive tuning.md §7.2 rule 3).</summary>
        public int KiteCapTick = -1, KiteHoldAt = -1;
        /// <summary>Tuning.md §7.10 rule 2: when this police body, shielded and shot from beyond its max ring, began holding (or last closed); -1 none.</summary>
        public int KiteCoverAt = -1;
        /// <summary>Tuning.md §11 (engine 0.17.0; Sky on firing, brief 024): the driven agent's trigger, from the logged trigger input. Down
        /// (Trig); its target (TrigT), kept on release with the aim and the count, so a press on the same target fires on cadence with no
        /// aim-new and no reset; a press waiting for its one shot (TrigTap: a buffer of one, never a queue); forced (the Ctrl case). Hashed
        /// only when set.</summary>
        public bool Trig, TrigTap, TrigForce; public string TrigT = "";
        /// <summary>Tuning.md §11.1: the driven agent whose fire this agent follows ("" none), derived each tick from the state (op id order,
        /// integer distance); kept so the log says when it joins and leaves. Hashed only when set.</summary>
        public string FollowBy = "";
        /// <summary>Engine 0.18.0: a press with follow on (§11.2 rule 3) found this agent in the ring but locked on another target still up; if
        /// that target falls in the tick that follows, the press takes the agent at the tick's end (the lead's name, the pressed target, forced).
        /// Set between ticks by the press, cleared at the next tick's end. Hashed only when set.</summary>
        public string LockAskBy = "", LockAsk = ""; public bool LockAskForce;
        public Gun(string id) { Id = id; }
    }

    public sealed class Op
    {
        public OpId Id; public Pt Pos;
        public Path? Walk; public long WalkMm;           // a move in progress
        public int WalkSpeed; public bool Running;       // its pace in cm/s (= mm a tick), and whether it is a run (Sky 2026-10-15: double-click runs)
        public bool RanLast;                             // tuning.md §10.2 settle: it was running (and moving) at the last fight tick (hashed only when it disagrees with now)
        public int SettleUntil = -1;                     // the settle's wait ends at this tick (host read only: Game.RangeLines "steadying"; never hashed, never read by a rule)
        public Order? Busy; public int BusyEnd;          // an action in progress, landing at BusyEnd
        public int BusyStart;                            // when it began (the HUD's progress; derived, so not hashed)
        public Quote? BusyQuote; public long BusyPaid;   // the card's price when the order was given, and the money taken (Redline E2)
        public bool Inside;                              // the Enforcer inside the Office (§01 exteriors only: an off-screen action)
        public bool InCar;                               // brief 018: boarded the getaway car (off the street, doesn't fire, a target as beside cover)
        public Gun Gun = null!;                          // health, weapon, fire order and stance (rulebook weapons)
        public OpId? Carrying, CarriedBy;                // carrying a downed operative, or being carried (rulebook cell.carry)
        public string CarryBody = "";                    // brief 013: the body (a person's id) this operative carries
        public bool RunAlerted;                          // this run was seen by security: its found fault is counted (rulebook cell.runAlert, once a run)
        // brief 025 (EXPERIMENTAL, rulebook cell.drive): the driven agent. Hashed only while Driven, so every run without a drive hashes as before
        public bool Driven;                              // the host moves this operative (drive samples)
        public Pt? DriveTo;                              // this tick's sample, consumed by the step
        public bool DriveRun, DriveSpan, DriveMoved;     // the sample asks to run; a run span is open (once-a-span notice); moved this tick
        public int Face = -1;                            // the last sample's facing, degrees (-1: none given)
        public int DriveBudget, DrivePush;               // mm the agent may still move (refilled each tick); cm pushed while busy
        public Pt? DriveResume; public bool DriveResumeRun; // the walk control took over (onRelease resume)
        public int DriveHeld;                            // samples the engine corrected (diagnostic for the host; not hashed)
        // brief 027 (EXPERIMENTAL, fight.lab; hashed only with the lab on): the charge bar, when a charged weapon last fired, the field, the
        // walk into range a fire order started (fire.close), and the volleys given in the current fight (charge off: target.volleysPerFight)
        public int Charge, LastCharged = -1000, FieldUntil = -1, FieldLeft, VolleyFight = -2, VolleysUsed, CloseAt; public bool CloseWalk;
        /// <summary>Tuning.md §8.2–8.3 (engine 0.15.0): where the operative stood before this tick's step (its gait's direction; derived each
        /// tick, not hashed), and whether a player move given while its fire order stood spent that order's walk-in (hashed only when set).</summary>
        public Pt StepFrom; public bool CloseSpent;
        public readonly HashSet<string> RunSeen = new HashSet<string>(StringComparer.Ordinal); // who remembers this run (once each)
        public bool Down => Gun.Down;
        public static string Name(OpId id) => id == OpId.Scout ? "scout" : id == OpId.Fixer ? "fixer" : id == OpId.Tech ? "tech" : "enforcer";
        public static OpId Parse(string s) => s == "scout" ? OpId.Scout : s == "fixer" ? OpId.Fixer : s == "tech" ? OpId.Tech : s == "enforcer" ? OpId.Enforcer : throw new FormatException($"operative '{s}' is unknown (scout, fixer, tech, enforcer)");
    }

    /// <summary>
    /// One order, as the GDD controls give it (§10: select/move, engage, fire, call; the §03 verbs). JSON:
    /// {"tick": 120, "op": "scout", "do": "move", "to": "d:Courtyard"} · {"tick": 690, "op": "fixer", "do": "turn", "approach": "help", "target": "waitress"}
    /// Verbs: move (to: a place id or [x, y] cm) · watch, listen (target: a role) · turn (approach, target) · call (target: a turned role)
    /// · loop (target: a camera) · reroute (target: "patrol") · mislead (target: "rivals") · enter · take · withdraw.
    /// Brief 018 (Sky 2026-10-18): board (target "car" or none: into the getaway car, with whoever the operative carries) · go (op "cell" or
    /// any operative: the car leaves with whoever is in it) · a move on a boarded operative steps out first. Extract is retired.
    /// "run": true on a move (opt-in, Sky 2026-10-15: double-click runs) walks it at the rulebook's run speed; absent, it walks as before.
    /// Shoot-outs (Sky 2026-10-15, briefs/011/motive-fights.md §2.4): draw · holster · fire (target: guard, patrol, security or
    /// security-n, rivals, hostiles or a hostile's id, and with "force": true only, waitress, driver, councillor; fires until the target is
    /// down) · cease · stance ("stance": hold, return, free) · carry (target: a downed operative) · panic (op "cell", once a fight is on).
    /// New keys are written only when set, so every older order reads and hashes as before.
    /// cancel (Sky 2026-10-15 playtest): stops what the operative is doing now (Game.ActivityOf), at the rulebook cancel line's cost.
    /// Brief 013 (Sky 2026-10-16): takedown (op enforcer; target a person's id or role; "how": knockout or kill) · carry (target a body: the
    /// person's id or role) · drop · stash (target a stash id) · restrain (target a body).
    /// Brief 025 (EXPERIMENTAL, rulebook cell.drive): drive ("to": [x, y] cm, "run", "face" 0..359) is a driven agent's sample, one a tick;
    /// {"do": "drive", "release": true} hands control back.
    /// </summary>
    public sealed class Order
    {
        public int Tick; public string Op = "", Do = "", Target = "", Approach = "", To = "", Stance = "", How = ""; public Pt? Point; public bool Run, Force;
        /// <summary>Brief 025 (EXPERIMENTAL): a drive sample's facing in whole degrees (-1: none), and a drive order that hands control back.</summary>
        public int Face = -1; public bool Release;
        /// <summary>Brief 027 (EXPERIMENTAL, fight.lab): a volley's shots (0: none), and a fire order's then ("": the rulebook's; free or stop).</summary>
        public int Shots; public string Then = "";
        /// <summary>Brief 029 (the fight range): a spawn's count, 1..6 (0: none).</summary>
        public int Count;
        /// <summary>Tuning.md §11 (engine 0.17.0): a trigger's press (true) or release (false), and the follow setting's on (lock) or off
        /// (fire with the trigger). Written for those two verbs only, always.</summary>
        public bool On;
        /// <summary>The fight range (GameRange.cs): who sits in a spawned car ("" nobody; police, rivals, bystander). Written only when set.</summary>
        public string With = "";
        static readonly string[] Keys = { "tick", "op", "do", "target", "approach", "to", "run", "stance", "force", "how", "face", "release", "shots", "then", "count", "on", "with" };
        public static Order Parse(JNode n)
        {
            n.Only(Keys);
            var o = new Order { Tick = n["tick"].Int(), Do = n["do"].Str() };
            o.Op = n.Opt("op")?.Str() ?? "";
            o.Target = n.Opt("target")?.Str() ?? "";
            o.Approach = n.Opt("approach")?.Str() ?? "";
            if (n.Opt("to") is JNode to) { if (to.Kind == JKind.Str) o.To = to.Str(); else o.Point = Pt.Of(to); }
            if (n.Opt("run") is JNode run) { o.Run = run.Bool(); if (o.Run && o.Do != "move" && o.Do != "drive") throw new FormatException($"{n.Path}: run is for a move or a drive sample (no other verb walks)"); }
            if (n.Opt("face") is JNode fc) { o.Face = fc.Int(); if (o.Do != "drive" || o.Face < 0 || o.Face > 359) throw new FormatException($"{n.Path}: face is a drive sample's facing, 0..359 degrees"); }
            if (n.Opt("release") is JNode rl) { o.Release = rl.Bool(); if (o.Do != "drive") throw new FormatException($"{n.Path}: release is for a drive order"); }
            if (n.Opt("stance") is JNode st) { o.Stance = st.Str(); if (o.Do != "stance") throw new FormatException($"{n.Path}: stance is for the stance order"); }
            if (n.Opt("force") is JNode fo) { o.Force = fo.Bool(); if (o.Force && o.Do != "fire" && o.Do != "trigger") throw new FormatException($"{n.Path}: force is for a fire order or a trigger (the Ctrl case)"); }
            if (o.Do == "trigger" || o.Do == "follow") o.On = (n.Opt("on") ?? throw new FormatException($"{n.Path}: a {o.Do} says on: true or false")).Bool(); // tuning.md §11
            else if (n.Opt("on") != null) throw new FormatException($"{n.Path}: on is for a trigger or the follow setting");
            if (n.Opt("how") is JNode hw) { o.How = hw.Str(); if (o.Do != "takedown") throw new FormatException($"{n.Path}: how is for a takedown (knockout or kill)"); }
            if (n.Opt("shots") is JNode sh) { o.Shots = sh.Int(); if (o.Do != "volley" || o.Shots < 1 || o.Shots > 100) throw new FormatException($"{n.Path}: shots is a volley's, 1 or more"); }
            if (n.Opt("count") is JNode ct) { o.Count = ct.Int(); if (o.Do != "spawn" || o.Count < 1 || o.Count > 6) throw new FormatException($"{n.Path}: count is a spawn's, 1..6"); }
            if (n.Opt("with") is JNode wi) { o.With = wi.Str(); if (o.Do != "spawn") throw new FormatException($"{n.Path}: with is a spawn's (who sits in a car; the engine refuses it on anything else)"); }
            if (n.Opt("then") is JNode th) { o.Then = th.Str(); if (o.Do != "fire" || (o.Then != "free" && o.Then != "stop")) throw new FormatException($"{n.Path}: then is a fire order's, free or stop"); }
            return o;
        }
        public void Write(JWriter w)
        {
            w.Obj().Prop("tick", Tick);
            if (Op.Length > 0) w.Prop("op", Op);
            w.Prop("do", Do);
            if (Target.Length > 0) w.Prop("target", Target);
            if (Approach.Length > 0) w.Prop("approach", Approach);
            if (To.Length > 0) w.Prop("to", To);
            if (Point.HasValue) w.Key("to").Raw($"[{Point.Value.X.ToString(CultureInfo.InvariantCulture)},{Point.Value.Y.ToString(CultureInfo.InvariantCulture)}]");
            if (Run) w.Prop("run", true); // written only when true, so a walk's order (and every old script) reads and hashes as before
            if (Stance.Length > 0) w.Prop("stance", Stance);
            if (Force) w.Prop("force", true);
            if (How.Length > 0) w.Prop("how", How); // brief 013: written only when set, so every older order reads and hashes as before
            if (Face >= 0) w.Prop("face", Face); // brief 025: likewise
            if (Release) w.Prop("release", true);
            if (Shots > 0) w.Prop("shots", Shots); // brief 027: likewise
            if (Then.Length > 0) w.Prop("then", Then);
            if (Count > 0) w.Prop("count", Count); // brief 029: likewise
            if (With.Length > 0) w.Prop("with", With); // the range's car spawn: likewise
            if (Do == "trigger" || Do == "follow") w.Prop("on", On); // tuning.md §11: only these verbs, always (a release is on: false)
            w.EndObj();
        }
        public override string ToString() { var w = new JWriter(); Write(w); return w.ToString(); }
    }

    public struct LogLine { public int Tick; public string Code, Text; }

    /// <summary>The answer to an order (Fable's request, briefs/011/fable-text.md): taken, or refused with a stable kebab-case code, the
    /// engine's words for the log, and the numbers the line needs as fields (-1 when it has none). The checkpoint hash reads the code, never
    /// the words. Deconstructs as (ok, why) or (ok, code, why).</summary>
    public struct Answer
    {
        public bool ok; public string code, why; public int m, cm, tick; public long usd;
        /// <summary>The ids the refusal names, comma-joined (brief 018: car-held names its holders, in id order); "" when it names none.</summary>
        public string ids;
        public void Deconstruct(out bool ok, out string why) { ok = this.ok; why = this.why; }
        public static implicit operator (bool ok, string why)(Answer a) => (a.ok, a.why); // older callers that hold (ok, why) keep compiling
        public void Deconstruct(out bool ok, out string code, out string why) { ok = this.ok; code = this.code; why = this.why; }
        /// <summary>Every refusal code the engine gives (Fable's `locks`, plus needs-* for a rulebook need of another kind).</summary>
        public static readonly string[] Codes =
        {
            "mission-over", "engine-error", "unknown-verb", "agent-down", "enforcer-inside", "agent-busy", "guard-down", "guard-not-at-door", "person-down",
            "person-away", "no-place", "unwalkable", "no-walk", "run-carrying-vale", "run-carrying-downed", "run-tethered", "only-scout", "only-fixer",
            "only-tech", "only-enforcer", "too-far", "needs-routine", "needs-need", "needs-temperament", "no-act-need", "need-met", "refusal-remembered",
            "already-turned", "no-cash", "driver-too-late", "turn-needs-approach", "vale-not-contact", "not-a-contact", "car-gone", "car-busy",
            "call-too-late", "nothing-to-call", "loop-wrong-target", "loop-holds", "stand-at-cabinet", "patrol-down", "patrol-away", "no-rivals",
            "stand-near-rivals", "vale-not-inside", "stand-at-door", "vale-not-at-car", "stand-beside-vale", "stand-at-getaway",
            "car-held", "no-weapon", "already-drawn", "not-drawn", "stance-unknown", "panic-not-cell", "panic-no-fight", "panic-nobody", "not-driven", "follow-not-cell", "fire-running",
            "fire-carrying", "no-target", "fire-on-cell", "target-unknown", "civilian-target", "out-of-range", "no-clear-line", "carry-vale",
            "already-carrying", "carry-names-downed", "carry-self", "not-down", "already-carried", "nothing-to-cancel", "keeps-vale", "holstering",
            "stand-beside-op",
            "no-leverage", "summon-off-street", "summon-busy", "summon-too-far", "summon-alert", "summon-saw-violence", "summon-came-once", "summon-no-visit",
            "needs-tier", "needs-reach", "needs-need-is", "needs-standing", "needs-fit", "needs-scale", "needs-trail",
            // brief 013: takedowns, bodies and stashes, the doors at lockdown
            "takedown-how", "takedown-vale", "no-body", "body-stashed", "not-carrying", "stand-at-stash", "stash-full", "body-dead", "already-restrained", "stand-beside-body", "doors-locked", "door-held",
            // Sky 2026-10-18: a Watch or Listen that can teach nothing more is refused, free (GameOrders.ScoutTeaches, GameSight.WatchWatcher)
            "already-heard", "already-watched",
            // brief 018 (Sky 2026-10-18): Board and Go
            "board-running", "board-tethered", "in-car", "car-empty", "fire-carrying-vale",
            // brief 023 (Motive 2026-10-19, rulebook fight.over): no fire order on a final-fight hostile once the final fight is over
            "fight-over",
            // brief 027 (EXPERIMENTAL, fight.lab): a volley or the field the agent's charge can't pay (with charge off: no volleys left this fight)
            "no-charge",
        };
    }

    /// <summary>The action card's price (§02 full price; K8 line items; Sky Q3 "?").</summary>
    public sealed class Quote
    {
        public sealed class L { public string Line = ""; public long Usd, S, Trail, Orders; public string Holds = "no"; } // yes, no, ?
        public readonly List<L> Lines = new List<L>();
        public long Usd, S, Trail;          // one order, the lines that hold
        public long WorstUsd;               // the cash the card asks for: the lines that hold or show "?"
        public long Orders = 1;             // orders of this approach that turn the person
        public string Outcome = "?";        // the outcome the card shows
        public string Event = "";            // the rulebook event the card prices
        public string Reports = "no";        // risk: "yes", "no" or "?" — would this person report the approach (+A)? (Motive's review, other findings 2)
        public int AlertIfReported;          // the Alert a report would raise it to
        public int Witnesses;                // risk: people within witness range of the target now, who would see a traceable act (+1 T); 0 for a private event (rulebook privacy)
        public int WalkS;                    // seconds the acting operative walks to stand within reach, 0 if already there
        public bool Leverage;                // Pressure on a known secret: "exposure: their secret" (Motive, motive-lovers.md §2.2)
        public SeenQuote? Seen;              // the card's "seen by" where the act is done (Preview only; rulebook sight.unknown): "?" for an unknown watcher
        public string Refused = "";          // a Watch or Listen that can teach nothing more: already-watched or already-heard (Sky 2026-10-18), the row greyed; "" otherwise
    }
}
