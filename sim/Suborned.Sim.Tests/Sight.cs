// The cell's knowledge of watchers (Sky 2026-10-16: view cones are the Scout's skill; rulebook sight). The fan is the engine's own
// sight test drawn as a polygon: every sampled point the engine sees is inside it and every point it doesn't is outside, but for a band
// of a few centimetres along the fan's edge (the rays' spacing and integer rounding). The reads change nothing, and knowledge never
// changes an outcome, only what the cards show.
using System;
using System.Collections.Generic;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class SightTests
{
    const int Tol = 8; // cm: the band along the fan's edge where a sample may fall either way (the hair-wide rays either side of a corner: 1/512 rad, 6 cm at 30 m)

    static double SegDist(Pt p, Pt a, Pt b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy;
        double t = l2 == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / l2, 0, 1);
        double x = a.X + t * dx - p.X, y = a.Y + t * dy - p.Y; return Math.Sqrt(x * x + y * y);
    }
    static double EdgeDist(Pt p, Pt[] poly)
    {
        double d = double.MaxValue;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++) d = Math.Min(d, SegDist(p, poly[j], poly[i]));
        return d;
    }

    /// <summary>Samples every `step` cm around the watcher; returns the samples where the fan and the engine disagree beyond Tol.</summary>
    static List<string> Disagreements(Game g, string w, bool crowd, int step = 23)
    {
        var f = g.ConeFan(w, crowd); Assert.NotNull(f);
        var bad = new List<string>(); int r = f!.Reach + 200, inside = 0;
        for (int x = -r; x <= r; x += step)
            for (int y = -r; y <= r; y += step)
            {
                var p = new Pt(f.At.X + x, f.At.Y + y);
                var polys = new List<Pt[]> { f.Points }; polys.AddRange(f.More); polys.RemoveAll(q => q.Length < 3); // a mast: a polygon a head
                bool truth = g.InSight(w, p, crowd), drawn = polys.Exists(q => G.InPoly(p, q));
                if (truth) inside++;
                if (truth != drawn && (polys.Count == 0 || polys.Min(q => EdgeDist(p, q)) > Tol)) bad.Add($"{w} at tick {g.Tick}: {p} engine {(truth ? "sees" : "doesn't")}, fan {(drawn ? "in" : "out")}");
            }
        Assert.True(inside > 0, $"{w}: the engine sees nothing around it");
        return bad;
    }

    static void AllAgree(Game g, bool crowd = false)
    {
        var bad = new List<string>(); int n = 0;
        foreach (var w in g.WatcherIds()) { if (g.ConeFan(w, crowd) == null) continue; n++; bad.AddRange(Disagreements(g, w, crowd)); }
        Assert.True(n > 0);
        Assert.True(bad.Count == 0, $"{bad.Count} samples disagree: " + string.Join("; ", bad.Take(8)));
    }

    [Fact]
    public void TheFanIsTheEnginesSightAtTheStart()
    {
        var g = Engine.New(Repo.Inputs); AllAgree(g);
        Assert.Equal(new[] { "camera:entrance", "camera:pickup", "camera:mast-314", "guard", "patrol" }, g.WatcherIds().Where(w => g.ConeFan(w) != null)); // brief 013: the world's Records Square mast
    }

    [Theory]
    [InlineData(150)] [InlineData(330)] [InlineData(500)] [InlineData(740)] [InlineData(910)]
    public void TheSweepingPatrolsFanFollowsItAlongItsSweep(int tick)
    {
        var g = Engine.New(Repo.Inputs); g.RunTo(tick);
        Assert.True(Disagreements(g, "patrol", false).Count == 0, string.Join("; ", Disagreements(g, "patrol", false).Take(5)));
    }

    [Fact]
    public void TheFanWithPeopleOnTheStreetIsCutByThem()
    {
        var g = Engine.New(Repo.Inputs); g.RunTo(300);
        AllAgree(g, crowd: true);
        // somewhere a person stands in a watcher's sight, and the crowd fan is not the walls' fan
        Assert.Contains(g.WatcherIds(), w => g.ConeFan(w, true) is Fan f && f.Stop.Contains("crowd"));
    }

    [Fact]
    public void AWalkingGuardsFanHasItsNearCircleAndALoopedCameraIsNotLive()
    {
        var s = new Start(); s.Turned.Add("waitress");
        var g = Engine.New(Repo.Inputs, s); var r = new Run(g);
        r.Wait(Game.T(5));
        Assert.True(g.Issue(new Order { Tick = g.Tick, Op = "fixer", Do = "move", To = "d:Courtyard" }).ok);
        r.Until(() => g.CanCall("waitress"), Game.T(120));
        Assert.True(g.Issue(new Order { Tick = g.Tick, Op = "fixer", Do = "call", Target = "waitress" }).ok);
        g.Advance(Game.T(10));
        Assert.Equal("walking to café", g.GuardPose()!.Value.phase);
        var f = g.ConeFan("guard")!;
        Assert.Contains("near", f.Stop);
        Assert.Empty(Disagreements(g, "guard", false));
        // the Tech loops the entrance camera: its fan stays, not live, and it notices nobody
        Assert.True(g.Issue(new Order { Tick = g.Tick, Op = "tech", Do = "move", To = "cabinet" }).ok);
        r.Until(() => r.Op("tech").Walk == null, Game.T(120));
        Assert.True(g.Issue(new Order { Tick = g.Tick, Op = "tech", Do = "loop", Target = "entrance" }).ok);
        r.Done("tech");
        Assert.True(g.LoopOn);
        var cam = g.ConeFan("camera:entrance")!;
        Assert.False(cam.Live); Assert.True(cam.Points.Length >= 3);
        Assert.DoesNotContain(g.Detection(), n => n.Watcher == "camera:entrance");
    }

    [Fact]
    public void TheReadsChangeNothing()
    {
        // play S10 (route A) from its orders twice: once bare, once calling every read every tick; the checkpoints are the same
        var played = Scenarios.Run(Repo.Inputs, Scenarios.Find("N11")!).G;
        var orders = played.Orders.Select(o => o.order).ToList();
        var bare = Engine.Play(Repo.Inputs, played.Start, orders, played.Tick);
        var g = Engine.New(Repo.Inputs, played.Start);
        var list = orders.Select((o, i) => (o, i)).OrderBy(x => x.o.Tick).ThenBy(x => x.i).ToList();
        int k = 0;
        while (g.Tick < played.Tick && !g.Over)
        {
            while (k < list.Count && list[k].o.Tick == g.Tick) { g.Issue(list[k].o); k++; }
            var before = Hash.State(g);
            foreach (var w in g.WatcherIds()) { g.ConeFan(w); g.ConeFan(w, true); g.Known(w); g.Routine(w, 600); }
            g.KnownFans(); g.Detection(); g.SeenAt(g.OpOf(OpId.Enforcer).Pos);
            if (g.Tick % 10 == 0) { g.QuoteMove("scout", g.S.Stand("d:Office approach")); g.Preview("help", "waitress"); g.ActivityOf(OpId.Scout); }
            Assert.Equal(before, Hash.State(g));
            if (g.Over) break;
            g.Step();
        }
        while (k < list.Count && list[k].o.Tick == g.Tick) { g.Issue(list[k].o); k++; }
        g.RunTo(played.Tick); g.Finish();
        Assert.Equal(bare.Checkpoints, g.Checkpoints);
        Assert.Equal(played.Result, g.Result);
    }

    [Fact]
    public void KnowledgeNeverChangesAnOutcomeOnlyTheCard()
    {
        // route A's orders, played with no watcher known and with every watcher's sight told: the same run, but for the knowledge itself
        var played = Scenarios.Run(Repo.Inputs, Scenarios.Find("N11")!).G;
        var orders = played.Orders.Select(o => o.order).ToList();
        var told = new Start();
        foreach (var kv in played.Start.Known) told.Known[kv.Key] = new List<string>(kv.Value);
        foreach (var kv in played.Start.Temperament) told.Temperament[kv.Key] = kv.Value;
        told.Turned.AddRange(played.Start.Turned); told.Cash = played.Start.Cash; told.Arms = played.Start.Arms; told.FinalFight = played.Start.FinalFight; told.Crowd = played.Start.Crowd;
        told.Known["camera:entrance"] = new List<string> { "sight" }; told.Known["camera:pickup"] = new List<string> { "sight" }; told.Known["patrol"] = new List<string> { "sight", "routine" };
        var a = Engine.Play(Repo.Inputs, played.Start, orders, played.Tick);
        var b = Engine.Play(Repo.Inputs, told, orders, played.Tick);
        Assert.Equal((a.Result, a.FailCode, a.Trail, a.Alert, a.Cash, a.Standing), (b.Result, b.FailCode, b.Trail, b.Alert, b.Cash, b.Standing));
        Assert.Equal(a.Log.Select(l => (l.Tick, l.Code)), b.Log.Select(l => (l.Tick, l.Code)));
        Assert.Equal(a.Orders.Select(o => (o.ok, o.code)), b.Orders.Select(o => (o.ok, o.code)));
        Assert.Equal(a.Ops.Select(o => o.Pos), b.Ops.Select(o => o.Pos));
        Assert.True(b.Known("camera:entrance")!.Sight && b.Known("camera:entrance")!.How == "told" && !a.Known("camera:entrance")!.Sight);
        Assert.NotEqual(a.Checkpoints.Last().hash, b.Checkpoints.Last().hash); // the knowledge is state: the hash walks it
    }

    [Fact]
    public void TheStartStateRejectsAnUnknownWatcherOrFact()
    {
        var s = new Start(); s.Known["camera:roof"] = new List<string> { "sight" };
        Assert.Throws<FormatException>(() => Engine.New(Repo.Inputs, s));
        var t = new Start(); t.Known["security-1"] = new List<string> { "sight" };
        Assert.Throws<FormatException>(() => Engine.New(Repo.Inputs, t)); // known on arrival, never told
        var u = new Start(); u.Known["camera:entrance"] = new List<string> { "routine" };
        Assert.Throws<FormatException>(() => Engine.New(Repo.Inputs, u)); // a camera's routine comes with its sight
    }

    [Fact]
    public void TheRulebookSightSectionIsReadStrictly()
    {
        var r = Repo.Inputs.Rules;
        Assert.Equal((20, 40), (r.SightWatchS, r.SightRoutineS));
        // brief 031: the reach is the agent's (Scout 30, Fixer 12, Tech 24, Enforcer 3 m); SightReachCm is the Scout's, for hosts that read one
        Assert.Equal(new[] { 3000, 1200, 2400, 300 }, r.WatchReachCms);
        Assert.Equal(3000, r.SightReachCm);
        Assert.Equal(new[] { ("unknown", 0, 8, 20, 45), ("noticed", 3, 6, 18, 35), ("wanted", 6, 5, 15, 25), ("infamous", 10, 3, 12, 18) }, r.AttnBands.ToArray());
        Assert.Equal(("infamous", "wanted", "unknown"), (r.AttnBand(11).id, r.AttnBand(9).id, r.AttnBand(2).id));
        Assert.Equal((true, true, 300, 1, 20, 3), (r.AttnGuardNear, r.AttnOfficerNear, r.AttnNearCm, r.AttnAlert, r.AttnScanS, r.AttnCallS));
        var badSense = Repo.Inputs.Rulebook.Replace("\"robot\": \"cone\"", "\"robot\": \"near\"");
        Assert.NotEqual(Repo.Inputs.Rulebook, badSense); // the canonical text
        Assert.Throws<FormatException>(() => Engine.FromTexts(badSense, Repo.Inputs.PeopleRules, Repo.Inputs.Site)); // L2: a robot sees by its cone only
        Assert.Equal(("watch", "watched"), r.SightKinds["patrol"]);
        Assert.Equal(("arrival", "none"), r.SightKinds["security"]);
        var bad = Repo.Inputs.Rulebook.Replace("\"routine\": \"watched\"", "\"routine\": \"guessed\"");
        Assert.Throws<FormatException>(() => Engine.FromTexts(bad, Repo.Inputs.PeopleRules, Repo.Inputs.Site));
    }
}
