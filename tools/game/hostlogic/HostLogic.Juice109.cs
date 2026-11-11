// Brief 109 (Sky: "let's add juice"): the juice is view only. Its switch (`-juice on|off`; without it on for a player's own launch and off for an
// automation launch) is a pure read of the launch's flags, and with no rig installed (the host logic, or the juice off) the host's calls into it are
// no-ops: orders taken, the clock run, the run saved and replayed hash for hash.
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;

public class Juice109Cases
{
    [Test] public void TheSwitchReadsTheLaunch()
    {
        Assert.That(Juice109.Decide(new[] { "The Suborned" }), Is.True, "a player's own launch: on");
        Assert.That(Juice109.Decide(new[] { "The Suborned", "-screen-fullscreen", "0", "-screen-width", "1920", "-windowed" }), Is.True, "window flags are not automation");
        Assert.That(Juice109.Decide(new[] { "The Suborned", "-psn_0_123" }), Is.True, "macOS's own flag");
        Assert.That(Juice109.Decide(new[] { "The Suborned", "-shots", "a.json", "out" }), Is.False, "a shot run: off");
        Assert.That(Juice109.Decide(new[] { "The Suborned", "-gameProof" }), Is.False, "a proof: off");
        Assert.That(Juice109.Decide(new[] { "The Suborned", "-shots", "a.json", "out", "-juice", "on" }), Is.True, "-juice on wins");
        Assert.That(Juice109.Decide(new[] { "The Suborned", "-juice", "off" }), Is.False, "-juice off wins");
        Assert.That(Juice109.Decide(new[] { "The Suborned", "-juice" }), Is.True, "a bare -juice means on");
    }

    [Test] public void WithNoRigTheHostsCallsChangeNothing()
    {
        var h = Host.Fresh(); h.controls.conversations = ConversationScenes.Never; Host.Set(h, "HintsOn", false);
        Host.Set(h, "Game", Engine.New(h.PlayInputs, new Start { Seed = 3 })); var g = h.Game;
        Assert.That(Juice109.Live, Is.False, "nothing installs the rig outside a running player");
        var to = g.S.Grid.NearestFree(g.Ops[(int)OpId.Scout].Pos + new Pt(400, 0));
        Assert.That(to.HasValue, Is.True);
        Assert.That(h.Issue(new Order { Op = "scout", Do = "move", Point = to }), Is.True, "the order is taken (JuiceOrder returns at once)");
        h.RunTo(200);
        Juice109.Order(null, null, "go"); Juice109.Takedown(default, g.Tick); // the facade's own guards
        var saved = Script.Parse(Script.Write(g, h.PlayInputs.Hash));
        var again = Engine.Play(h.PlayInputs, saved.Start, saved.Orders, saved.Until);
        Assert.That(saved.Verify(again, h.PlayInputs.Hash), Is.Empty, "the run replays hash for hash");
    }
}
