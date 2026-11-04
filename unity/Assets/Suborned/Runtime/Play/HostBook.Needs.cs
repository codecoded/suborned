using System;
using System.Collections.Generic;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 088 (Sky on 0.2.227: "add a mission objectives log and put more details in there"): what each way in needs, as conditions, never methods. The list is the mission's data
    /// (text.json book.job.needs, by way id); each condition's check is a few words of a small vocabulary over engine state the host already reads, so a new mission's list needs no code:
    ///   guard-away · camera-blind:&lt;camera&gt; (looped or dark) · inside:&lt;agent&gt; · vale:&lt;state&gt;[,&lt;state&gt;] · turned:&lt;role&gt; · log:&lt;code&gt; · car-guarded · left-safe; " | " is "any of".
    /// `known` (same vocabulary plus sight:&lt;watcher&gt;) keeps a condition at "?" until the cell has learned what it rests on. Words may carry {t:valeExits} {t:valeAtCar} {t:driverLast} {t:chime}.
    /// Nothing here gives an order or changes state: reads only.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>A check in the vocabulary, now; null when a word isn't in the vocabulary (the proof holds the data to it).</summary>
        public bool? BookCheck(string check)
        {
            var g = Game; if (g == null || string.IsNullOrWhiteSpace(check)) return null; bool any = false;
            foreach (var part in check.Split('|'))
            {
                var t = part.Trim(); int c = t.IndexOf(':'); string k = c < 0 ? t : t[..c], a = c < 0 ? "" : t[(c + 1)..]; bool v;
                switch (k)
                {
                    case "guard-away": v = !g.GuardAtDoor; break;
                    case "camera-blind": v = g.CameraDark(a) || g.LoopOn && g.LoopCam == a; break;
                    case "inside": v = a is "scout" or "fixer" or "tech" or "enforcer" && g.Ops[(int)Op.Parse(a)].Inside; break;
                    case "vale": v = Array.Exists(a.Split(','), s => Enum.TryParse<ValeAt>(s.Trim(), out var st) && g.Vale == st); break;
                    case "turned": v = g.PersonByRole(a) is { Turned: true }; break;
                    case "log": v = g.Log.Exists(l => l.Code == a); break;
                    case "sight": v = g.Known(a) is { Sight: true }; break;
                    case "car-guarded": v = g.Vale == ValeAt.Car && (!g.CarHeld() || Array.Exists(g.Ops, CarGuard)); break; // brief 096: the opposite of fight.unguarded's CarUnguarded
                    case "left-safe": v = g.LeftBehind().TrueForAll(x => x.fate == "walked"); break; // brief 096 (GDD §06 left behind): nobody a Go now leaves would be captured
                    default: return null;
                }
                any |= v;
            }
            return any;
        }

        /// <summary>Brief 096: the engine's guard of a held car (Game.Guards, sim/Suborned.Sim/GameUnguarded.cs, engine 0.35.2), read here as it reads:
        /// up, on the street outside the car, gun drawn or drawing, not holstering. An undrawn agent standing by guards nothing.</summary>
        static bool CarGuard(Op o) => !o.Down && !o.InCar && !o.Inside && o.Gun.Drawn && o.Gun.HolsterAt < 0;

        /// <summary>The words with their times, from the mission's own clock (the site's seconds).</summary>
        string BookTimes(string w)
        {
            var s = Game.S; foreach (var (k, sec) in new[] { ("valeExits", s.ValeExits), ("valeAtCar", s.ValeAtCar), ("driverLast", s.DriverLast), ("chime", s.Chime) }) w = w.Replace("{t:" + k + "}", MissionClock(sec * Game.Hz));
            return w;
        }

        /// <summary>The conditions of the ways the page shows, each with its state now.</summary>
        void BookNeeds(BookPage p)
        {
            var bw = BookWordsFile.Get();
            foreach (var w in p.Ways)
                if (bw.Needs.TryGetValue(w.Verb, out var list))
                    foreach (var n in list)
                    {
                        bool unknown = n.Known.Length > 0 && BookCheck(n.Known) != true;
                        p.Needs.Add(new BookNeed { Id = n.Id, Group = w.Verb, Words = BookTimes(n.Words), Unknown = unknown, Met = !unknown && BookCheck(n.Check) == true });
                    }
        }
    }
}
