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
    }

    /// <summary>A simulated person (people.rules tiers.simulated): routine, need, temperament, memory, and what the cell knows of them.</summary>
    public sealed class Person
    {
        public string Role = "", Id = "", Name = "", Temperament = "", Need = "";
        public readonly int[] Sc = new int[4];
        public readonly List<Mem> Memory = new List<Mem>();
        public bool Turned, NeedMet, Down;
        public bool KRoutine, KNeed, KTemperament; // the cell's knowledge of this person
        public string Suspected = "";              // a need the briefing names (§07 cast: Inez in debt, the driver needs cash): shown as suspected, unlocks nothing
        public string Place = ""; // the place they stand at for the mission (memory's place)
        public bool Knows(string what) => what == "routine" ? KRoutine : what == "need" ? KNeed : what == "temperament" ? KTemperament : throw new FormatException($"knowledge '{what}'");
        public void Learn(string what) { if (what == "routine") KRoutine = true; else if (what == "need") KNeed = true; else if (what == "temperament") KTemperament = true; else throw new FormatException($"knowledge '{what}'"); }
        public void Forget(string what) { if (what == "routine") KRoutine = false; else if (what == "need") KNeed = false; else if (what == "temperament") KTemperament = false; }
        public int Count(string act) { int n = 0; foreach (var m in Memory) if (m.Act == act) n++; return n; }
        public bool Has(string act) { foreach (var m in Memory) if (m.Act == act) return true; return false; }
    }

    public enum OpId { Scout = 0, Fixer = 1, Tech = 2, Enforcer = 3 }

    public sealed class Op
    {
        public OpId Id; public Pt Pos;
        public Path? Walk; public long WalkMm;           // a move in progress
        public Order? Busy; public int BusyEnd;          // an action in progress, landing at BusyEnd
        public bool Inside;                              // the Enforcer inside the Office (§01 exteriors only: an off-screen action)
        public static string Name(OpId id) => id == OpId.Scout ? "scout" : id == OpId.Fixer ? "fixer" : id == OpId.Tech ? "tech" : "enforcer";
        public static OpId Parse(string s) => s == "scout" ? OpId.Scout : s == "fixer" ? OpId.Fixer : s == "tech" ? OpId.Tech : s == "enforcer" ? OpId.Enforcer : throw new FormatException($"operative '{s}' is unknown (scout, fixer, tech, enforcer)");
    }

    /// <summary>
    /// One order, as the GDD controls give it (§10: select/move, engage, fire, call; the §03 verbs). JSON:
    /// {"tick": 120, "op": "scout", "do": "move", "to": "d:Courtyard"} · {"tick": 690, "op": "fixer", "do": "turn", "approach": "help", "target": "waitress"}
    /// Verbs: move (to: a place id or [x, y] cm) · watch, listen (target: a role) · turn (approach, target) · call (target: a turned role)
    /// · loop (target: a camera) · reroute (target: "patrol") · mislead (target: "rivals") · fire (target: guard, waitress, driver,
    /// councillor, patrol, security, rivals) · enter · take · extract · withdraw.
    /// </summary>
    public sealed class Order
    {
        public int Tick; public string Op = "", Do = "", Target = "", Approach = "", To = ""; public Pt? Point;
        static readonly string[] Keys = { "tick", "op", "do", "target", "approach", "to" };
        public static Order Parse(JNode n)
        {
            n.Only(Keys);
            var o = new Order { Tick = n["tick"].Int(), Do = n["do"].Str() };
            o.Op = n.Opt("op")?.Str() ?? "";
            o.Target = n.Opt("target")?.Str() ?? "";
            o.Approach = n.Opt("approach")?.Str() ?? "";
            if (n.Opt("to") is JNode to) { if (to.Kind == JKind.Str) o.To = to.Str(); else o.Point = Pt.Of(to); }
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
