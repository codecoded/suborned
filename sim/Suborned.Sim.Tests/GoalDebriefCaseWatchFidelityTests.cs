using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Suborned.Sim;
using Xunit;

public class GoalDebriefCaseWatchFidelityTests
{
    static string Write(Debrief d)
    {
        var w = new JWriter();
        d.Write(w);
        return w.ToString();
    }

    static JsonElement Property(JsonElement e, string name)
    {
        Assert.Equal(JsonValueKind.Object, e.ValueKind);
        Assert.True(e.TryGetProperty(name, out var value));
        return value;
    }

    static void SameJson(JsonElement expected, JsonElement actual)
    {
        Assert.Equal(expected.ValueKind, actual.ValueKind);
        if (expected.ValueKind == JsonValueKind.Object)
        {
            Assert.Equal(expected.EnumerateObject().Select(p => p.Name).OrderBy(x => x).ToArray(),
                actual.EnumerateObject().Select(p => p.Name).OrderBy(x => x).ToArray());
            foreach (var p in expected.EnumerateObject()) SameJson(p.Value, Property(actual, p.Name));
        }
        else if (expected.ValueKind == JsonValueKind.Array)
        {
            var a = expected.EnumerateArray().ToArray();
            var b = actual.EnumerateArray().ToArray();
            Assert.Equal(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++) SameJson(a[i], b[i]);
        }
        else if (expected.ValueKind == JsonValueKind.String)
            Assert.Equal(expected.GetString(), actual.GetString());
        else Assert.Equal(expected.GetRawText(), actual.GetRawText());
    }

    static void Exports(JsonElement root,
        (string who, string kind, string place, int missions)[] cases,
        (string place, int guards, int cameras, int missions)[] watch)
    {
        var next = Property(root, "nextMission");
        var c = Property(next, "cases");
        var a = Property(next, "areaWatch");
        using var ec = JsonDocument.Parse(JsonSerializer.Serialize(cases.Select(x => new { x.who, x.kind, x.place, x.missions })));
        using var ea = JsonDocument.Parse(JsonSerializer.Serialize(watch.Select(x => new { x.place, x.guards, x.cameras, x.missions })));
        SameJson(ec.RootElement, c);
        SameJson(ea.RootElement, a);
    }

    static Game Play(string id)
    {
        var run = Scenarios.Run(Repo.Inputs, Scenarios.Find(id)!);
        Assert.All(run.Checks, c => Assert.True(c.ok));
        return run.G;
    }

    [Fact]
    public void SuccessfulHeadlessReportIncludesEmptyArrays()
    {
        var g = Play("S10");
        Assert.Equal(Outcome.Success, g.Result);
        var d = Debrief.Of(g);
        Assert.Empty(d.Next.Cases);
        Assert.Empty(d.Next.AreaWatch);
        using var report = JsonDocument.Parse(Report.Json("S10", g, new List<(bool, string)>(), true));
        var exported = Property(report.RootElement, "debrief");
        Exports(exported, d.Next.Cases.ToArray(), d.Next.AreaWatch.ToArray());
        using var direct = JsonDocument.Parse(Write(d));
        SameJson(direct.RootElement, exported);
    }

    [Fact]
    public void SerializationPreservesDistinctValuesAssociationsOrderAndDuplicates()
    {
        var d = Debrief.Of(Play("S10"));
        // Serialization-boundary data, not new campaign rules or balance values.
        d.Next.Cases.Clear();
        d.Next.AreaWatch.Clear();
        d.Next.Cases.Add(("person-z", "missing", "block\"z", -1));
        d.Next.Cases.Add(("person-a", "found-killing", "block\na", 7));
        d.Next.Cases.Add(("person-b", "found-killing", "block\"z", 0));
        d.Next.Cases.Add(d.Next.Cases[0]);
        d.Next.AreaWatch.Add(("block\"z", 2, 5, 7));
        d.Next.AreaWatch.Add(("block\na", 11, 3, 1));
        d.Next.AreaWatch.Add(("block\"z", 0, 9, 0));
        d.Next.AreaWatch.Add(d.Next.AreaWatch[0]);
        var cases = d.Next.Cases.ToArray();
        var watch = d.Next.AreaWatch.ToArray();
        var legacy = Legacy(d);
        var first = Write(d);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(first, Write(d));
            Assert.Equal(cases, d.Next.Cases.ToArray());
            Assert.Equal(watch, d.Next.AreaWatch.ToArray());
            AssertLegacy(legacy, Write(d));
        }
        using var json = JsonDocument.Parse(first);
        Exports(json.RootElement, cases, watch);
    }

    [Fact]
    public void ExistingLegacyFieldsKeepTheirNamesTypesAndValues()
    {
        foreach (var id in new[] { "S10", "N47", "N48", "N119" })
        {
            var g = Play(id);
            var d = Debrief.Of(g);
            var expected = Legacy(d);
            var cases = d.Next.Cases.ToArray();
            var watch = d.Next.AreaWatch.ToArray();
            var json = Write(d);
            AssertLegacy(expected, json);
            using var parsed = JsonDocument.Parse(json);
            Exports(parsed.RootElement, cases, watch);
        }
    }

    [Fact]
    public void MissingPersonExportsReplayWithoutChangingConsequencesOrChecksums()
    {
        var g = Play("N47");
        var d = Debrief.Of(g);
        Assert.NotEmpty(d.Next.Cases);
        Assert.Contains(d.Next.Cases, c => c.kind == "missing" && c.missions == -1);
        Assert.NotEmpty(d.Next.AreaWatch);
        var cases = d.Next.Cases.ToArray();
        var watch = d.Next.AreaWatch.ToArray();
        var state = Hash.State(g);
        var checkpoints = g.Checkpoints.ToArray();
        var script = Script.Write(g, Repo.Inputs.Hash);
        var status = (g.Tick, g.Result, g.FailCode, g.Trail, g.Alert, g.Cash, g.Standing);
        var first = Write(d);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(first, Write(d));
            Assert.Equal(first, Write(Debrief.Of(g)));
            Assert.Equal(cases, d.Next.Cases.ToArray());
            Assert.Equal(watch, d.Next.AreaWatch.ToArray());
            Assert.Equal(state, Hash.State(g));
            Assert.Equal(checkpoints, g.Checkpoints.ToArray());
            Assert.Equal(script, Script.Write(g, Repo.Inputs.Hash));
            Assert.Equal(status, (g.Tick, g.Result, g.FailCode, g.Trail, g.Alert, g.Cash, g.Standing));
        }
        var replay = Engine.Replay(Repo.Inputs, g);
        Assert.Equal(g.StartHash, replay.StartHash);
        Assert.Equal(checkpoints, replay.Checkpoints.ToArray());
        Assert.Equal(g.Result, replay.Result);
        Assert.Equal(g.FailCode, replay.FailCode);
        Assert.Equal(state, Hash.State(replay));
        Assert.Equal("", Script.Parse(script).Verify(replay, Repo.Inputs.Hash));
        Assert.Equal(first, Write(Debrief.Of(replay)));
        using var json = JsonDocument.Parse(first);
        Exports(json.RootElement, cases, watch);
        using var replayJson = JsonDocument.Parse(Write(Debrief.Of(replay)));
        Exports(replayJson.RootElement, cases, watch);
    }

    [Fact]
    public void ReadingLiveConsequencesDoesNotCommitOrAdvanceAnything()
    {
        var sc = Scenarios.Find("N47")!;
        Game Prepare()
        {
            var start = Start.Parse(JNode.Parse(sc.Start.Canonical()));
            var g = Engine.New(Repo.Inputs, start);
            var run = new Run(g);
            sc.Body(run);
            Assert.All(run.Checks, c => Assert.True(c.ok));
            return g;
        }
        var observed = Prepare();
        var control = Prepare();
        Assert.False(observed.Over);
        Assert.Equal(Hash.State(control), Hash.State(observed));
        var freshBefore = Hash.State(Engine.New(Repo.Inputs));
        var d = Debrief.Of(observed);
        var cases = d.Next.Cases.ToArray();
        var watch = d.Next.AreaWatch.ToArray();
        Assert.NotEmpty(cases);
        Assert.NotEmpty(watch);
        var first = Write(d);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(first, Write(Debrief.Of(observed)));
            Assert.Equal(first, Write(d));
            Assert.Equal(cases, d.Next.Cases.ToArray());
            Assert.Equal(watch, d.Next.AreaWatch.ToArray());
            Assert.Equal(Hash.State(control), Hash.State(observed));
            Assert.Equal(control.Tick, observed.Tick);
            Assert.Equal(control.Result, observed.Result);
            Assert.Equal(control.Checkpoints.ToArray(), observed.Checkpoints.ToArray());
        }
        Assert.Equal(freshBefore, Hash.State(Engine.New(Repo.Inputs)));
        int until = observed.Tick + Game.Hz;
        observed.RunTo(until);
        control.RunTo(until);
        Assert.True(observed.Issue(new Order { Tick = observed.Tick, Do = "withdraw" }).ok);
        Assert.True(control.Issue(new Order { Tick = control.Tick, Do = "withdraw" }).ok);
        observed.Finish();
        control.Finish();
        Assert.Equal(Hash.State(control), Hash.State(observed));
        Assert.Equal(control.Checkpoints.ToArray(), observed.Checkpoints.ToArray());
        Assert.Equal(Script.Write(control, Repo.Inputs.Hash), Script.Write(observed, Repo.Inputs.Hash));
        Assert.Equal(Write(Debrief.Of(control)), Write(Debrief.Of(observed)));
        using var json = JsonDocument.Parse(first);
        Exports(json.RootElement, cases, watch);
    }

    // Independent legacy JSON contract oracle, using public debrief data captured before Write.
    static string Legacy(Debrief d)
    {
        var inv = new Dictionary<string, object>
        {
            ["count"] = d.Inv.Investigators, ["hops"] = d.Inv.Hops, ["sightings"] = d.Sightings,
            ["findings"] = d.Inv.Findings.Select(x =>
            {
                var f = new Dictionary<string, object> { ["hop"] = x.Hop, ["who"] = x.Who,
                    ["outcome"] = x.Outcome, ["lead"] = x.Lead, ["gives"] = x.Facts.ToArray() };
                if (x.Cause.Length > 0) f["because"] = x.Cause;
                return f;
            }).ToArray()
        };
        if (d.Inv.Hunted.Count > 0) inv["hunted"] = d.Inv.Hunted.ToArray();
        var n = d.Next;
        var next = new Dictionary<string, object>
        {
            ["ambush"] = n.Ambush, ["ambushBy"] = n.AmbushBy.ToArray(),
            ["standing"] = n.Standing.Select(l => new {
                district = l.District, from = l.From, to = l.To, band = l.Band, cause = l.Cause, who = l.Who,
                mission = l.End - l.From, found = l.Found, word = l.Word, wordUncapped = l.WordUncapped, drift = l.Drift,
                moves = l.Moves.Select(m => new { delta = m.delta, cause = m.cause, who = m.who }).ToArray()
            }).ToArray(),
            ["lines"] = n.Lines.Select(l => new { line = l.Line, who = l.Who, detail = l.Detail }).ToArray(),
            ["contacts"] = n.Contacts.Select(c => new { who = c.Role.Length > 0 ? c.Role : c.Id,
                name = c.Name, need = c.Need, needKnown = c.KNeed, bravery = c.Sc[0], resentment = c.Sc[1],
                fear = c.Sc[2], loyalty = c.Sc[3] }).ToArray()
        };
        if (n.Cases.Count > 0 || n.CampaignTrail != 0)
        {
            next["campaignTrail"] = n.CampaignTrail;
            next["campaignStanding"] = n.CampaignStanding;
            next["openCases"] = n.OpenCases;
            next["nextLevel"] = n.NextLevel;
        }
        if (n.Captives.Count > 0 || n.CarSeenBy.Length > 0)
        {
            if (n.CarSeenBy.Length > 0) next["carSeenBy"] = n.CarSeenBy;
            next["captives"] = n.Captives.Select(c => new { who = c.Op, why = c.Why, heldAt = c.HeldAt,
                holds = c.Holds, talksAt = c.TalksAt, rescueUntil = c.RescueUntil, bravery = c.Sc[0],
                resentment = c.Sc[1], fear = c.Sc[2], loyalty = c.Sc[3], left = c.Left, right = c.Right,
                gives = c.Facts.ToArray() }).ToArray();
        }
        var root = new Dictionary<string, object>
        {
            ["publicSecurity"] = d.PublicSecurity,
            ["investigation"] = new { state = d.InvestigationState, cause = d.InvestigationCause },
            ["contacts"] = d.Contacts, ["loud"] = d.Loud, ["assets"] = d.Assets.ToArray(),
            ["route"] = d.Route.ToArray(), ["extraInterventions"] = d.Extra.ToArray(), ["chain"] = d.Chain.ToArray(),
            ["investigators"] = inv, ["nextMission"] = next,
            ["cell"] = d.Cell.Select(c => new { op = c.op, health = c.health, max = c.max }).ToArray(),
            ["captured"] = d.Captured.ToArray()
        };
        if (d.FightBranch.Length > 0)
            root["finalFight"] = new { branch = d.FightBranch, hostiles = d.FightHostiles,
                downedByEnforcer = d.FightDownedByEnforcer, downedByGuard = d.FightDownedByGuard,
                downedByCell = d.FightDownedByCell, hitsTaken = d.FightHits, shots = d.FightShots,
                why = d.FightWhy.ToArray() };
        if (d.LeftBehind.Count > 0)
            root["leftBehind"] = d.LeftBehind.Select(x => new { who = x.op, fate = x.fate, why = x.why }).ToArray();
        if (d.Cause != null)
        {
            using var cause = JsonDocument.Parse(d.Cause.Json());
            root["cause"] = cause.RootElement.Clone();
        }
        return JsonSerializer.Serialize(root);
    }

    static void AssertLegacy(string expected, string actual)
    {
        using var e = JsonDocument.Parse(expected);
        using var a = JsonDocument.Parse(actual);
        // Remove only the two explicitly authorized additions before comparing the entire old contract.
        var root = a.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => (object)p.Value.Clone());
        root["nextMission"] = Property(a.RootElement, "nextMission").EnumerateObject()
            .Where(p => p.Name != "cases" && p.Name != "areaWatch")
            .ToDictionary(p => p.Name, p => p.Value.Clone());
        using var stripped = JsonDocument.Parse(JsonSerializer.Serialize(root));
        SameJson(e.RootElement, stripped.RootElement);
    }
}
