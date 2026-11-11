// Brief 107 (engine 0.54.0; Sky 2026-11-11: "Yes to all three"): the weather and the light, a LOOK ONLY. No rule reads them: no sight, no crowd,
// no price, no clock. They are in each game's start state so a later rule could read them and a saved run replays exactly:
//  · a new game's host asks Start.RollLook, which draws both from the seed on their own streams ({seed}/look/weather, {seed}/look/light; rulebook
//    streams.keys.start), weighted by the rulebook's dial (look.weather, look.light). No other draw is touched: every start draw is keyed by its own
//    character and stage (GameSeed.cs), so no existing roll moves. Seed 0 is the neutral start: the dial's first kind (clear, day);
//  · the briefing may change either before the clock runs: the orders `weather` and `light` (to: a kind), taken at tick 0 only (look-fixed after),
//    a kind the dial does not name refused (look-unknown); one weather and one light for the whole mission;
//  · a start that sets them logs start.weather and start.light; a taken order logs its own. A start that sets neither (every script, scenario,
//    saved run and sweep before 0.54.0) is clear by day, logs nothing and hashes byte for byte as before.
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    /// <summary>The rulebook's look (brief 107): the odds of each weather and each light, as integer weights. Read by no rule.</summary>
    public sealed class LookRules
    {
        /// <summary>The kinds the engine knows, in the dial's order (the first is the neutral start's). The rulebook names exactly these: unknown = reject.</summary>
        public static readonly string[] Weathers = { "clear", "overcast", "rain", "mist" }, Lights = { "day", "night" };
        public readonly int[] WeatherOdds = new int[Weathers.Length], LightOdds = new int[Lights.Length];
        public LookRules(JNode n, Func<JNode, int> ri)
        {
            n.Only("weather", "light", "readBy", "rule", "tag", "src", "proposed", "reason");
            Odds(n["weather"], Weathers, WeatherOdds, ri); Odds(n["light"], Lights, LightOdds, ri);
            foreach (var _ in n["readBy"].Items) throw new FormatException("look.readBy: engine 0.54.0 has no rule that reads the look (brief 107: a look only)");
            if (n["rule"].Str().Length == 0) throw new FormatException("look.rule is empty");
        }
        static void Odds(JNode n, string[] kinds, int[] into, Func<JNode, int> ri)
        {
            n.Only(kinds); int total = 0;
            for (int i = 0; i < kinds.Length; i++) { into[i] = ri(n[kinds[i]]); if (into[i] < 0) throw new FormatException($"{n.Path}.{kinds[i]}: a weight is 0 or more"); total += into[i]; }
            if (total < 1) throw new FormatException($"{n.Path}: the weights sum to at least 1");
        }
        /// <summary>The kind this seed draws (rulebook streams draw: x = floor(u × total ÷ 2^32), walk the kinds subtracting each weight). Seed 0: the first.</summary>
        public static string Draw(int seed, string stage, string[] kinds, int[] odds)
        {
            if (seed == 0) return kinds[0];
            long total = 0; foreach (var w in odds) total += w;
            long x = (long)((ulong)Streams.Nth(Streams.Fnv1a32(seed.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/look/" + stage), 1) * (ulong)total >> 32);
            for (int i = 0; i < kinds.Length; i++) { x -= odds[i]; if (x < 0) return kinds[i]; }
            throw new InvalidOperationException("look: the draw ran past the weights");
        }
        public string WeatherOf(int seed) => Draw(seed, "weather", Weathers, WeatherOdds);
        public string LightOf(int seed) => Draw(seed, "light", Lights, LightOdds);
    }

    public sealed partial class Start
    {
        /// <summary>Brief 107: the weather (clear, overcast, rain, mist) and the light (day, night) this game starts with. Null: not set, which plays
        /// clear by day and adds no bytes (every start before 0.54.0). A new game's host sets both with RollLook; hashed into the start when set.</summary>
        public string? Weather, Light;
        /// <summary>A new game: the weather and the light drawn from this start's seed by the rulebook's dial, unless the start already sets them
        /// (Replay keeps the game's own). The host calls it once, after the seed.</summary>
        public Start RollLook(Rules r) { Weather ??= r.Look.WeatherOf(Seed); Light ??= r.Look.LightOf(Seed); return this; }
        static void ParseLook(Start s, JNode n)
        {
            if (n.Opt("weather") is JNode w) { s.Weather = w.Str(); if (Array.IndexOf(LookRules.Weathers, s.Weather) < 0) throw new FormatException($"{w.Path}: weather is one of {string.Join(", ", LookRules.Weathers)} (unknown = reject)"); }
            if (n.Opt("light") is JNode l) { s.Light = l.Str(); if (Array.IndexOf(LookRules.Lights, s.Light) < 0) throw new FormatException($"{l.Path}: light is one of {string.Join(", ", LookRules.Lights)} (unknown = reject)"); }
        }
        void WriteLook(JWriter w) { if (Weather != null) w.Prop("weather", Weather); if (Light != null) w.Prop("light", Light); } // only a start that sets them adds bytes
    }

    public sealed partial class Game
    {
        /// <summary>Brief 107: the weather and the light now (the start's, or the briefing's order). A look only: no rule reads them, and they are not in
        /// the state hash (the start's are in the start hash, a change is an order). The host draws from these.</summary>
        public string Weather { get; private set; } = "clear";
        public string Light { get; private set; } = "day";
        public bool Night => Light == "night";

        void InitLook()
        {
            if (Start.Weather != null) { Weather = Start.Weather; Note("start.weather", $"Weather: {Weather}."); }
            if (Start.Light != null) { Light = Start.Light; Note("start.light", $"Light: {Light}."); }
        }

        /// <summary>The briefing's weather or light (op "cell", to: a kind): before the clock runs only, one for the whole mission.</summary>
        string LookSet(Order o)
        {
            bool weather = o.Do == "weather"; var kinds = weather ? LookRules.Weathers : LookRules.Lights;
            if (Array.IndexOf(kinds, o.To) < 0) return No("look-unknown", $"{o.Do} is one of {string.Join(", ", kinds)}, not '{o.To}'");
            if (Tick > 0) return No("look-fixed", $"the {o.Do} is set at the briefing, before the clock runs; it holds for the whole mission");
            if (weather) { Weather = o.To; Note("start.weather", $"Weather: {Weather}."); } else { Light = o.To; Note("start.light", $"Light: {Light}."); }
            return "";
        }
    }
}
