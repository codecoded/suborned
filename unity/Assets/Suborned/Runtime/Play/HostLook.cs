using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 107 (Sky 2026-11-11: "Yes to all three"; engine 0.54.0, Keel): the weather and the light are rolled from the seed into each new game's
    /// start, can be changed at the briefing (an order, before the clock runs) and hold for the whole mission. LOOK ONLY: no rule reads them; the view
    /// (PlayView107) draws Game.Weather and Game.Light. Replay and Restart keep the game's own, as they keep its seed. Only a rolled seed (a new game) rolls the
    /// look: a pinned or given seed (proofs, batch rows, pins, `-seed n`, the host-logic tests) plays its start byte for byte (clear, day unless it says). The briefing's Day / Night and weather words give the orders (HudToolkit.Look107.cs); `-night` stays a view override for the proofs.</summary>
    public sealed partial class HandoverHost
    {
        (string weather, string light)? lookAgain;

        /// <summary>NewFightGame, after the seed: the look Replay keeps, else the roll (a new game's rolled seed only).</summary>
        void LookForBegin(Start s)
        {
            if (lookAgain is { } k) { s.Weather = k.weather; s.Light = k.light; lookAgain = null; }
            else if (seedRolled) s.RollLook(PlayInputs.Rules); // a new game only: a pinned, given or kept seed (proofs, pins, `-seed n`, tests) plays its start as it is
        }

        /// <summary>BeginSameSeed: the next Begin keeps the look of the game just played.</summary>
        void LookKeep() => lookAgain = Game != null && (Game.Start.Weather != null || Game.Start.Light != null || Game.Weather != "clear" || Game.Light != "day") ? (Game.Weather, Game.Light) : null;

        /// <summary>The briefing's light or weather: an order at tick 0 (the engine refuses it once the clock has run). False when it was not taken.</summary>
        public bool SetLook(string what, string kind)
        {
            if (Game == null || Game.Tick > 0) return false;
            var before = what == "weather" ? Game.Weather : Game.Light;
            Issue(new Order { Tick = 0, Op = "cell", Do = what, To = kind });
            return (what == "weather" ? Game.Weather : Game.Light) == kind || before == kind;
        }
    }
}
