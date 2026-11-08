using System;
using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 096, G1 (engine 0.43.0, rulebook car.go.waits "board"; Motive's misses page; Sky 2026-11-05 "all recommended"): a Go given on either cell car
    /// while an agent is climbing in waits for his Board and lands the tick it lands. The engine answers (Game.GoWouldWait, Game.GoWaiting; LeftBehind already
    /// counts the climber aboard); this reads it for the car's Go row (its line and its Alt does-line; brief 101: no seconds) and the roster (the climber's "then" line).
    /// The words are Fable's (text.json book.cars goOnce, goWaiting, goWaitsRoster; placeholders in HostBook.WordsLend.cs). Nothing here is a rule.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>"car" for ours, "car:official" for Popovic's (the engine's names for a Go's car).</summary>
        static string GoCarOf(string target) => LentOrder(target) ? "car:official" : "car";

        /// <summary>The agents climbing into this car now (a Board under way), in id order, only while the engine says a Go would wait for them.</summary>
        List<int> Climbers(string car)
        {
            var l = new List<int>(); var g = Game; if (g == null || g.Range) return l;
            bool would = false; Guard("go would wait", () => would = g.GoWouldWait(GoCarOf(car))); if (!would) return l;
            bool lent = LentOrder(car);
            for (int k = 0; k < 4; k++) { var op = g.Ops[k]; if (!op.Down && op.Busy is Order b && b.Do == "board" && op.BusyEnd > g.Tick && LentOrder(b.Target) == lent) l.Add(k); }
            return l;
        }

        /// <summary>A Go on this car waits for a Board under way (given, not landed).</summary>
        public bool GoWaitingOn(string car) { bool w = false; var g = Game; if (g != null && !g.Range) Guard("go waiting", () => w = g.GoWaiting(GoCarOf(car))); return w; }

        /// <summary>Popovic climbing into our car with the Enforcer: the Go lands once they are in, so it wins (the card says win, not withdraw).</summary>
        bool ValeClimbingIn => Game != null && Game.Vale == ValeAt.WithEnforcer && Climbers("car").Contains((int)OpId.Enforcer);

        /// <summary>The Go row's line and Alt does-line while someone climbs in: before the click "Go · goes once Tech is in", after it "Waiting for the Tech";
        /// unchanged when nobody is climbing in.</summary>
        void GoWaitWords(Intent gv, string car)
        {
            var ks = Climbers(car); if (ks.Count == 0) return; var w = BookWordsFile.Get();
            string names = string.Join(", ", ks.ConvertAll(k => AgentNames[k]));
            if (GoWaitingOn(car)) { gv.Label = w.GoWaiting.Replace("{list}", names); gv.Does = gv.Label; return; }
            var once = w.GoOnce.Replace("{list}", names); gv.Label += " · " + once; gv.Does = once.Length > 0 ? char.ToUpperInvariant(once[0]) + once[1..] : once;
        }

        /// <summary>The roster's "then" line for agent k: climbing into a car whose Go waits for him, the car goes once he is in; "" else.</summary>
        string GoWaitsUnder(int k)
        {
            var g = Game; if (g == null || g.Ops[k].Busy is not Order b || b.Do != "board") return "";
            return GoWaitingOn(b.Target) && Climbers(b.Target).Contains(k) ? BookWordsFile.Get().GoWaitsRoster : "";
        }

        /// <summary>Who would see Popovic's car leave now (the engine's LentCarWatcher), "" nobody or not lent: the lent car's Go card's "+1 T if seen".</summary>
        string LentWatcherNow() { string s = ""; if (LentCarOurs) Guard("lent car watcher", () => s = Game.LentCarWatcher()); return s ?? ""; }
    }
}
