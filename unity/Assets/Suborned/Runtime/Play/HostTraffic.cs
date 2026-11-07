using System;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 098 step 1 (law L8; Sky 2026-11-07: "bring in more cars etc - the game then just does that"), the host's side of the passing cars beside
    /// their views (HostViews.Traffic): the kit they are drawn with (PlayInstall.TrafficKit), the pick, the names and the hover's words. A passing car is a car
    /// target like the mission's: Fire, the fire line, the blast and the car effects take it by its engine id (car:&lt;slot&gt;-&lt;trip&gt;), and the engine
    /// gives it cover and solid ground while it stands. Hovered with no way offered on it, the pill says what it is and what it is doing, in plain words,
    /// from the engine's own reads (the car's state and its driver's mode, Game.TrafficWhy: what holds it). The words are stand-ins for Fable: the names
    /// read text.json targets car-passing, car-van and car-scooter when it has them. Read only: nothing here changes a run.</summary>
    public sealed partial class HandoverHost
    {
        [Tooltip("Brief 098 step 1: the passing cars' van (the old town's DK_BoxTruck; BoxTruck_Play is a kit candidate, not copied) and scooter (Vela's DK_HoverScooter); null = the box. The sedans are sedanOfficial and sedanGetaway, repainted.")]
        public GameObject trafficVan, trafficScooter;
        [Tooltip("Brief 098 step 1: Pulse's palette (presentation.json paints) as body and roof material pairs, with their keys; and the slots they replace (kit.json carPaints.slots: official body, official roof, getaway body, getaway roof).")]
        public Material[] trafficPaints; public string[] trafficPaintKeys, trafficSlots;
        [Tooltip("Brief 098 step 1: per paint, the bodies it is kept off (1 Sedan_Official, 2 Sedan_Getaway; today both or neither): its body colour would pass for the official car or ours (TrafficKit.NearDE, CIEDE2000).")]
        public int[] trafficPaintAvoid;

        /// <summary>A car target that is a passing car: neither the Handover's own two (the rival car is a target of its own kind) nor the fight range's.</summary>
        public static bool IsTrafficCar(Target t) => t != null && t.Kind == "car" && t.Key.Length > 0 && t.Key is not ("official" or "getaway") && !t.Key.StartsWith("range-", StringComparison.Ordinal);

        /// <summary>TargetOf: a passing car's view (car/&lt;slot&gt;-&lt;trip&gt;) as a car target while the engine has it on the roads, else null.</summary>
        Target TrafficTarget(string n)
        {
            var v = views?.TrafficOf("car:" + n.Substring(4)); if (v == null) return null;
            return new Target { Kind = "car", Key = n.Substring(4), Label = TrafficName(v) };
        }

        /// <summary>A passing car's name in the player's words, by what it is and (a car) the colour it is painted (brief 098 step 1b, so the player can find the
        /// one named: "the red car", "the cream car", "the van", "the scooter"). Stand-ins for Fable: text.json targets car-van, car-scooter and car-colour
        /// ("the {colour} car") are read when present.</summary>
        string TrafficName(TrafficView v) => v.Model switch
        {
            "van" => Text?.TargetName("car-van", "the van") ?? "the van", "scooter" => Text?.TargetName("car-scooter", "the scooter") ?? "the scooter",
            _ => TrafficColour(views?.TrafficPaintOf(v.Id) ?? "") is { Length: > 0 } c ? (Text?.TargetName("car-colour", "the {colour} car") ?? "the {colour} car").Replace("{colour}", c) : Text?.TargetName("car-passing", "the passing car") ?? "the passing car",
        };
        /// <summary>A passing car's name by its engine id (car:t-…): its view's, else the engine's own read of it (TrafficNow); null when the roads have no such car.</summary>
        public string TrafficNameOf(string id) => (views?.TrafficOf(id) ?? EngineTrafficView(id)) is TrafficView v ? TrafficName(v) : null;
        /// <summary>A passing car as the engine reads it now (Game.TrafficNow), for a word asked between the views' ticks or with no views; null when none.</summary>
        TrafficView EngineTrafficView(string id) { var g = Game; TrafficView v = null; if (g != null && g.TrafficOn && IsTrafficId(id)) Guard("traffic now", () => v = g.TrafficNow().Find(x => x.Id == id)); return v; }
        /// <summary>An engine id that names a passing car: car: and the dial's traffic prefix (population.json kinds.cars.id, "t-").</summary>
        bool IsTrafficId(string id) => !string.IsNullOrEmpty(id) && id.StartsWith("car:" + (Bundle?.Inputs?.Population?.Traffic?.IdPrefix ?? "t-"), StringComparison.Ordinal);
        /// <summary>A palette paint (presentation.json paints, the key) as the colour a player would call it; the body's colour, not the roof's.</summary>
        static string TrafficColour(string paint) => paint switch
        {
            "" => "", "canary-yellow" => "yellow", "cream-turquoise" => "cream", "harbor-blue" => "blue", "larkspur-blue" => "pale blue", "matador-red" => "red", "onyx" => "black",
            "orchid" => "lilac", "seafoam-green" => "green", "tropical-turquoise" => "turquoise", "dusk-pink" => "pink", "tangerine" => "orange",
            _ => paint.Split('-')[0],
        };
        /// <summary>The fire line's name for a car id when it is a passing car's, else null (FireName keeps its own).</summary>
        string TrafficFireName(string id) => TrafficNameOf(id);

        /// <summary>The hover pill's line for a passing car with no way offered on it (HudToolkit.HoldPillFrame): "Red car · waiting for someone to cross"; for a mission
        /// car held by traffic, why (MissionPill); "" for anything else.</summary>
        public string TrafficPill(Target t)
        {
            if (!IsTrafficCar(t) || views?.TrafficOf("car:" + t.Key) is not TrafficView v) return MissionPill(t);
            var w = TrafficWords(v.Id); return w.Length > 0 ? Head(TrafficName(v)) + " · " + w : Head(TrafficName(v));
        }
        /// <summary>A name as a pill's head: "the red car" → "Red car".</summary>
        static string Head(string name) { if (name.StartsWith("the ", StringComparison.Ordinal)) name = name.Substring(4); return name.Length > 0 ? char.ToUpperInvariant(name[0]) + name.Substring(1) : name; }

        /// <summary>What a passing car is doing now, in plain words: its state (wrecked, on fire, burnt out), its driver's mode (ducking, braking hard, pulled
        /// up for the lockdown, fled), else what holds it (Game.TrafficWhy's first thing ahead and the turn it waits for) once it is slow, else "driving through".</summary>
        public string TrafficWords(string id)
        {
            var g = Game; if (g != null && ReferenceEquals(g, wordsGame) && g.Tick == wordsTick && id == wordsId) return wordsSaid; // the hover asks every frame; the words move only with a tick (TrafficWhy builds the street's snapshot)
            wordsSaid = TrafficWordsNow(id); wordsGame = g; wordsTick = g?.Tick ?? -1; wordsId = id; return wordsSaid;
        }
        Game wordsGame; int wordsTick = -1; string wordsId = "", wordsSaid = "";
        string TrafficWordsNow(string id) => TrafficWordsOf(views?.TrafficOf(id));
        /// <summary>TrafficWordsNow for one car's read (the views' copy of TrafficNow's).</summary>
        string TrafficWordsOf(TrafficView v)
        {
            var g = Game; if (g == null || v == null) return ""; var id = v.Id;
            string shoved = v.Shot && v.ShovedAt >= 0 && g.Tick - v.ShovedAt <= 2 ? " · being pushed aside" : ""; // brief 098 step 1b: a mission car is shoving the wreck out of its way
            switch (v.State) { case "blown": return "burnt out" + shoved; case "burning": return "on fire" + shoved; case "disabled": return (v.DriverIn ? "wrecked" : "wrecked, the driver ran") + shoved; }
            if (!v.DriverIn || v.Mode == "abandoned") return "abandoned, the driver ran" + shoved;
            if (v.Boxes.Length > 0) return $"boxing {CarWords(v.Boxes)} in"; // brief 098 step 1b: it stands across that car's way out (shoot it and the car can shove it aside)
            if (v.MakingRoom.Length > 0) return $"making room for {CarWords(v.MakingRoom)}";
            switch (v.Mode) { case "duck": return "the driver is ducking"; case "brake": return "braking hard"; case "held": return "pulled up for the lockdown"; }
            string why = ""; try { why = g.TrafficWhy(id) ?? ""; } catch (Exception) { }
            bool flee = v.Mode == "flee", stopped = v.Speed <= 30, slow = v.Speed < 250; // cm/s: 0.3 m/s and 2.5 m/s (the access roads' lanes run at 5 m/s)
            string held = stopped || slow ? HeldWords(why, stopped) : "";
            if (flee) return held.Length > 0 ? "fleeing · " + held : "fleeing the shooting";
            return held.Length > 0 ? held : stopped ? "stopped" : "driving through";
        }

        /// <summary>Game.TrafficWhy's parts ("a person 2.4 m ahead", "a car 6.0 m ahead (will not move on)", "turn j3-a 4.0 m ahead: held", "the road in is closed") in plain words.</summary>
        static string HeldWords(string why, bool stopped)
        {
            if (why.Length == 0) return "";
            var parts = why.Split(new[] { "; " }, StringSplitOptions.None); string first = parts[0]; bool still = first.Contains("(will not move on)");
            bool junction = false, closed = false;
            foreach (var p in parts) { if (p.StartsWith("turn ", StringComparison.Ordinal) && !p.EndsWith(": free", StringComparison.Ordinal)) junction = true; if (p == "the road in is closed") closed = true; }
            if (first.StartsWith("a person", StringComparison.Ordinal)) return still ? "going round someone in the road" : stopped ? "waiting for someone to cross" : "slowing for someone in the road";
            if (first.StartsWith("a body", StringComparison.Ordinal)) return stopped ? "stopped for a body in the road" : "slowing for a body in the road";
            if (first.StartsWith("a mission car", StringComparison.Ordinal)) return "giving way to a car";
            if (first.StartsWith("a car", StringComparison.Ordinal)) return still ? (stopped ? "stuck behind a stopped car" : "edging round a stopped car") : stopped ? "queuing behind a car" : "following a car";
            if (first.StartsWith("the kerb", StringComparison.Ordinal)) return "squeezing past";
            if (junction) return "waiting at the junction";
            if (closed) return "the roads are closed";
            return "";
        }
    }
}
