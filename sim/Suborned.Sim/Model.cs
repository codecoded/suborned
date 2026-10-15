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
        public bool KRoutine, KNeed, KTemperament; // the cell's knowledge of this person
        public readonly SortedSet<string> KnownRefused = new SortedSet<string>(StringComparer.Ordinal); // approaches the cell saw this person refuse: the card's ✗
        public string Suspected = "";              // a need the briefing names (§07 cast: Inez in debt, the driver needs cash): shown as suspected, unlocks nothing
        public string Place = ""; // the place they stand at for the mission (memory's place)
        public bool Knows(string what) => what == "routine" ? KRoutine : what == "need" ? KNeed : what == "temperament" ? KTemperament : what == "refused" ? KnownRefused.Count > 0 : throw new FormatException($"knowledge '{what}'");
        public void Learn(string what) { if (what == "routine") KRoutine = true; else if (what == "need") KNeed = true; else if (what == "temperament") KTemperament = true; else throw new FormatException($"knowledge '{what}'"); }
        public void Forget(string what) { if (what == "routine") KRoutine = false; else if (what == "need") KNeed = false; else if (what == "temperament") KTemperament = false; }
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
    /// </summary>
    public sealed class Order
    {
        public int Tick; public string Op = "", Do = "", Target = "", Approach = "", To = "", Stance = ""; public Pt? Point; public bool Run, Force;
        static readonly string[] Keys = { "tick", "op", "do", "target", "approach", "to", "run", "stance", "force" };
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
            w.EndObj();
        }
        public override string ToString() { var w = new JWriter(); Write(w); return w.ToString(); }
    }

    public struct LogLine { public int Tick; public string Code, Text; }

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
        public int Witnesses;                // risk: people within witness range of the target now, who would see a traceable act (+1 T)
        public int WalkS;                    // seconds the acting operative walks to stand within reach, 0 if already there
    }
}
