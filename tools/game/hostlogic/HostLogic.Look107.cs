// Brief 107 (engine 0.54.0, Keel; Sky: the weather and light are rolled from the seed and changed at the briefing): the host's side. The briefing's weather and
// Day / Night give the engine's `weather` and `light` orders (HandoverHost.ChooseWeather, SetLook), taken before the clock only; the game holds them; the run
// saved with them replays them, hash for hash. A pinned or given seed rolls no look (its start stays byte for byte).
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;

public class Look107
{
    static HandoverHost Fresh(int seed)
    {
        var h = Host.Fresh(); h.controls.conversations = ConversationScenes.Never; Host.Set(h, "HintsOn", false);
        Host.Set(h, "Game", Engine.New(h.PlayInputs, new Start { Seed = seed })); return h;
    }

    [Test] public void TheBriefingsWeatherAndLightAreOrdersThatReplay()
    {
        var h = Fresh(5); var g = h.Game;
        Assert.That(g.Weather, Is.EqualTo("clear")); Assert.That(g.Light, Is.EqualTo("day"), "a given start with no look plays clear and day");
        h.ChooseWeather("rain"); Assert.That(h.SetLook("light", "night"), Is.True);
        Assert.That(g.Weather, Is.EqualTo("rain")); Assert.That(g.Light, Is.EqualTo("night"));
        h.RunTo(300); Assert.That(g.Tick, Is.GreaterThan(0));
        Assert.That(h.SetLook("weather", "mist"), Is.False, "once the clock has run the weather holds"); Assert.That(g.Weather, Is.EqualTo("rain"));
        var saved = Script.Parse(Script.Write(g, h.PlayInputs.Hash));
        var again = Engine.Play(h.PlayInputs, saved.Start, saved.Orders, saved.Until);
        Assert.That(saved.Verify(again, h.PlayInputs.Hash), Is.Empty, "the saved run replays");
        Assert.That(again.Weather, Is.EqualTo("rain")); Assert.That(again.Light, Is.EqualTo("night"), "the replay draws the briefing's choice");
    }
}
