using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 098 step 1b (Sky 2026-11-07, on jams: "should be opposite - shot cars can get past, others block"; Keel's jams.html, "The Go card and the ring"):
    /// ordinary traffic standing on a car's way out boxes it in, and Go (or Drive) is refused car-boxed (drive-boxed) naming the passing car (Answer.ids,
    /// car:t-005-02). The player has to read which car it is and that shooting it clears the way (a shot car never boxes anyone in: our car shoves it aside):
    ///  · the refusal's line (the pop's long line, Alt): "Boxed in: the red car is in our way out. Shoot it and we can shove it aside.", and with the rival
    ///    car in the way too, Fable's line for it and then the passing car's; the short (the pop, the ring's shut Go): "Boxed in · red car";
    ///  · the car named pulses where it stands (the HUD's ring pulse on a target, as a refusal's pop on a thing);
    ///  · a mission car held behind traffic on its leg says why on its hover (Game.MissionHoldNow, the passing car named plainly) when no way is offered on it
    ///    (the hover's info line: a car that offers Board, Go, Drive or a locked Watch shows that way instead).
    /// STAND-IN WORDS for Fable (the lines, text.json locks car-boxed and drive-boxed: byTarget "traffic" {line, short} with {cars}, {it} and {are} is read
    /// when present) and Glyph (the short on the pop and the shut Go, the pulse). Read only: nothing here changes a run.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The passing cars a refusal names (car-boxed and drive-boxed name the cars in the way, then any holders), in its order.</summary>
        List<string> BoxingTraffic(Answer a)
        {
            var l = new List<string>(); if (a.code is not ("car-boxed" or "drive-boxed") || string.IsNullOrEmpty(a.ids)) return l;
            foreach (var x in a.ids.Split(',')) { var id = x.Trim(); if (IsTrafficId(id) && !l.Contains(id)) l.Add(id); }
            return l;
        }
        /// <summary>A mission car among a refusal's ids (the rival car across our way out, brief 096's rule): its lock line still applies.</summary>
        static bool BoxingMissionCar(Answer a) { foreach (var x in (a.ids ?? "").Split(',')) if (x.Trim() is "car:rivals" or "car:official" or "car:getaway") return true; return false; }

        static string And(List<string> names) => names.Count <= 1 ? (names.Count == 1 ? names[0] : "") : string.Join(", ", names.GetRange(0, names.Count - 1)) + " and " + names[^1];

        /// <summary>The refusal's line when passing traffic boxes the car in (car-boxed, drive-boxed), else null (the text's own lock line).</summary>
        public string BoxedLine(Answer a)
        {
            var cars = BoxingTraffic(a); if (cars.Count == 0) return null;
            bool one = cars.Count == 1; string list = And(cars.ConvertAll(id => TrafficNameOf(id) ?? "a passing car")), it = one ? "it" : "them", are = one ? "is" : "are";
            var fab = Text?.LockByCode(a.code); string own = fab != null && fab.ByTarget.TryGetValue("traffic", out var bt) && bt.line.Length > 0 ? bt.line : null;
            string line = (own ?? "Boxed in: {cars} {are} in our way out. Shoot {it} and we can shove {it} aside.").Replace("{cars}", list).Replace("{it}", it).Replace("{are}", are);
            if (!BoxingMissionCar(a)) return line;
            string first = fab != null && fab.Line.Length > 0 ? fab.Line : "Boxed in: a car is across the way out. It has to drive off or be blown up.";
            return $"{first} {char.ToUpperInvariant(list[0])}{list.Substring(1)} {are} in the way too: shoot {it} and we can shove {it} aside.";
        }
        /// <summary>The refusal's short when passing traffic boxes the car in: "Boxed in · red car", "Boxed in · 2 cars"; null else.</summary>
        public string BoxedShort(Answer a)
        {
            var cars = BoxingTraffic(a); if (cars.Count == 0) return null;
            var fab = Text?.LockByCode(a.code); if (fab != null && fab.ByTarget.TryGetValue("traffic", out var bt) && bt.shortText.Length > 0) return bt.shortText.Replace("{cars}", cars.Count == 1 ? Head(TrafficNameOf(cars[0]) ?? "a car").ToLowerInvariant() : $"{cars.Count} cars");
            string head = Text?.ShortLock(a.code) ?? "Boxed in";
            return head + " · " + (cars.Count == 1 ? Head(TrafficNameOf(cars[0]) ?? "a car").ToLowerInvariant() : $"{cars.Count} cars");
        }
        /// <summary>A refused Go or Drive boxed in by traffic: each passing car named pulses where it is drawn, so the eye finds it.</summary>
        public void PulseBoxers(Answer a)
        {
            float now = Time.unscaledTime;
            if (views == null) return;
            foreach (var id in BoxingTraffic(a)) if (views.TrafficDrawnAt(id, out var at)) Pulses.Add((at, Feedback.Refused, now));
            while (Pulses.Count > 6) Pulses.RemoveAt(0);
        }

        /// <summary>A mission car in the player's words: "our car", "Popovic's car" (text.json targets), "the rival car".</summary>
        string CarWords(string id) => id switch
        {
            "car:getaway" => "our car", "car:official" => Text?.TargetName("car-official", "Popovic's car") ?? "Popovic's car", "car:rivals" => Text?.TargetName("rivals-car", "the rival car") ?? "the rival car", _ => "a car",
        };

        /// <summary>Why a mission car waits behind traffic now, from the engine's own words (Game.MissionHoldNow: "held 4 s by car:t-003-02 (queued)") with the
        /// passing car named plainly ("held 4 s by the red car (queued)"); "" when nothing holds it.</summary>
        public string MissionHeldWords(string id)
        {
            var g = Game; if (g == null || g.Range || !g.TrafficOn) return "";
            string s = ""; MissionHold last = null;
            Guard("mission hold", () => { s = g.MissionHoldNow(id) ?? ""; if (s.Length > 0) last = g.MissionHolds.FindLast(x => x.Car == id && x.Open); });
            if (s.Length == 0) return "";
            return last != null && last.By.Length > 0 ? s.Replace(last.By, TrafficNameOf(last.By) ?? "a passing car") : s;
        }
        /// <summary>The hover's line on a mission car held behind traffic ("Our car · held 4 s by the red car (queued)"), "" else (HostTraffic.TrafficPill).</summary>
        string MissionPill(Target t)
        {
            if (t == null) return "";
            string id = t.Kind == "car" && t.Key is "getaway" or "official" ? "car:" + t.Key : t.Kind == "rivals" && t.Key == "car:rivals" ? "car:rivals" : null;
            if (id == null) return "";
            var w = MissionHeldWords(id); return w.Length > 0 ? Head(CarWords(id)) + " · " + w : "";
        }
    }
}
