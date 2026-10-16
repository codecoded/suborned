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
        public string Order = ""; public bool Force;   // a standing fire order: until the target is down, cease, or another order
        public string Target = ""; public int Acc, NextShot, LostAt = -1; // the target aimed at, its accumulator, the next shot, when the line broke
        public string ShotBy = "";                     // the last shooter who fired on this one (stance return answers them)
        public bool Fired, Engaged, Hit;               // has fired; in action (a new target costs aimNew, not react); hit once (its Trail paid)
        public int ShotByTick = -1;                    // when ShotBy last fired on this one (Pulse §1.5: "whoever is shooting at me" for line-lapse)
        // the fight AI's state (Pulse, briefs/011/pulse-fight-ai.md §2): one machine for every hostile body
        public string St = "";                         // routine, alerted, advance, engage, hold, break, gone ("" for the cell and civilians)
        public int Until, NoFoeSince = -1, StepAt = -100000, UnprotSince = -1, Class, LostTick = -1, SightLostAt = -1;
        public string Cover = "", LostTarget = "";     // the cover point it holds; the target it last lost (its aim stays warm)
        public Gun(string id) { Id = id; }
    }

    public sealed class Op
    {
        public OpId Id; public Pt Pos;
        public Path? Walk; public long WalkMm;           // a move in progress
        public int WalkSpeed; public bool Running;       // its pace in cm/s (= mm a tick), and whether it is a run (Sky 2026-10-15: double-click runs)
        public Order? Busy; public int BusyEnd;          // an action in progress, landing at BusyEnd
        public int BusyStart;                            // when it began (the HUD's progress; derived, so not hashed)
        public Quote? BusyQuote; public long BusyPaid;   // the card's price when the order was given, and the money taken (Redline E2)
        public bool Inside;                              // the Enforcer inside the Office (§01 exteriors only: an off-screen action)
        public Gun Gun = null!;                          // health, weapon, fire order and stance (rulebook weapons)
        public OpId? Carrying, CarriedBy;                // carrying a downed operative, or being carried (rulebook cell.carry)
        public string CarryBody = "";                    // brief 013: the body (a person's id) this operative carries
        public bool RunAlerted;                          // this run was seen by security: its found fault is counted (rulebook cell.runAlert, once a run)
        public readonly HashSet<string> RunSeen = new HashSet<string>(StringComparer.Ordinal); // who remembers this run (once each)
        public bool Down => Gun.Down;
        public static string Name(OpId id) => id == OpId.Scout ? "scout" : id == OpId.Fixer ? "fixer" : id == OpId.Tech ? "tech" : "enforcer";
        public static OpId Parse(string s) => s == "scout" ? OpId.Scout : s == "fixer" ? OpId.Fixer : s == "tech" ? OpId.Tech : s == "enforcer" ? OpId.Enforcer : throw new FormatException($"operative '{s}' is unknown (scout, fixer, tech, enforcer)");
    }

    /// <summary>
    /// One order, as the GDD controls give it (§10: select/move, engage, fire, call; the §03 verbs). JSON:
    /// {"tick": 120, "op": "scout", "do": "move", "to": "d:Courtyard"} · {"tick": 690, "op": "fixer", "do": "turn", "approach": "help", "target": "waitress"}
    /// Verbs: move (to: a place id or [x, y] cm) · watch, listen (target: a role) · turn (approach, target) · call (target: a turned role)
    /// · loop (target: a camera) · reroute (target: "patrol") · mislead (target: "rivals") · enter · take · extract · withdraw.
    /// "run": true on a move (opt-in, Sky 2026-10-15: double-click runs) walks it at the rulebook's run speed; absent, it walks as before.
    /// Shoot-outs (Sky 2026-10-15, briefs/011/motive-fights.md §2.4): draw · holster · fire (target: guard, patrol, security or
    /// security-n, rivals, hostiles or a hostile's id, and with "force": true only, waitress, driver, councillor; fires until the target is
    /// down) · cease · stance ("stance": hold, return, free) · carry (target: a downed operative) · panic (op "cell", once a fight is on).
    /// New keys are written only when set, so every older order reads and hashes as before.
    /// cancel (Sky 2026-10-15 playtest): stops what the operative is doing now (Game.ActivityOf), at the rulebook cancel line's cost.
    /// Brief 013 (Sky 2026-10-16): takedown (op enforcer; target a person's id or role; "how": knockout or kill) · carry (target a body: the
    /// person's id or role) · drop · stash (target a stash id) · restrain (target a body).
    /// </summary>
    public sealed class Order
    {
        public int Tick; public string Op = "", Do = "", Target = "", Approach = "", To = "", Stance = "", How = ""; public Pt? Point; public bool Run, Force;
        static readonly string[] Keys = { "tick", "op", "do", "target", "approach", "to", "run", "stance", "force", "how" };
        public static Order Parse(JNode n)
        {
            n.Only(Keys);
            var o = new Order { Tick = n["tick"].Int(), Do = n["do"].Str() };
            o.Op = n.Opt("op")?.Str() ?? "";
            o.Target = n.Opt("target")?.Str() ?? "";
            o.Approach = n.Opt("approach")?.Str() ?? "";
            if (n.Opt("to") is JNode to) { if (to.Kind == JKind.Str) o.To = to.Str(); else o.Point = Pt.Of(to); }
            if (n.Opt("run") is JNode run) { o.Run = run.Bool(); if (o.Run && o.Do != "move") throw new FormatException($"{n.Path}: run is for a move (no other verb walks)"); }
            if (n.Opt("stance") is JNode st) { o.Stance = st.Str(); if (o.Do != "stance") throw new FormatException($"{n.Path}: stance is for the stance order"); }
            if (n.Opt("force") is JNode fo) { o.Force = fo.Bool(); if (o.Force && o.Do != "fire") throw new FormatException($"{n.Path}: force is for a fire order (the Ctrl case)"); }
            if (n.Opt("how") is JNode hw) { o.How = hw.Str(); if (o.Do != "takedown") throw new FormatException($"{n.Path}: how is for a takedown (knockout or kill)"); }
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
            "stand-near-rivals", "vale-not-inside", "stand-at-door", "vale-not-at-car", "stand-beside-vale", "no-vale-yet", "stand-at-getaway",
            "car-held", "no-weapon", "already-drawn", "not-drawn", "stance-unknown", "panic-not-cell", "panic-no-fight", "panic-nobody", "fire-running",
            "fire-carrying", "no-target", "fire-on-cell", "target-unknown", "civilian-target", "out-of-range", "no-clear-line", "carry-vale",
            "already-carrying", "carry-names-downed", "carry-self", "not-down", "already-carried", "nothing-to-cancel", "keeps-vale", "holstering",
            "stand-beside-op",
            "no-leverage", "summon-off-street", "summon-busy", "summon-too-far", "summon-alert", "summon-saw-violence", "summon-came-once", "summon-no-visit",
            "needs-tier", "needs-reach", "needs-need-is", "needs-standing", "needs-fit", "needs-scale", "needs-trail",
            // brief 013: takedowns, bodies and stashes, the doors at lockdown
            "takedown-how", "takedown-vale", "no-body", "body-stashed", "not-carrying", "stand-at-stash", "stash-full", "body-dead", "already-restrained", "stand-beside-body", "doors-locked", "door-held",
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
    }
}
