// Brief 100, the why (Sky 2026-11-07: "It would be good to explain why an alert raised, or why police came"): the host words every Alert rise and police call from
// Fable's `why` block, marks a report on its way, and the meter lists the causes; read only, so a run with all of it replays to the same hash.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using Order = Suborned.Sim.Order;

public class Why100
{
    /// <summary>A script played through the host's own order paths (as Conversations.PlayHost), on its seed, to `until` or the end; `each` runs once a tick.</summary>
    public static HandoverHost Play(string path, int until = -1, Action<HandoverHost> each = null)
    {
        var sc = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Root.Path, path)));
        HandoverHost.SeedPin = sc.Start.Seed;
        try
        {
            var h = Host.Fresh(); h.controls.conversations = ConversationScenes.Never; Host.Set(h, "HintsOn", false);
            var orders = sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
            int end = until > 0 ? until : Game.T(h.Game.S.Over) + 10;
            void To(int t) { while (!h.Game.Over && h.Game.Tick < t && h.Fault == null) { h.RunTo(h.Game.Tick + 1); each?.Invoke(h); } }
            foreach (var o in orders) { if (o.Tick >= end || h.Game.Over) break; To(o.Tick); if (h.Game.Over) break; if (o.Do is "watch" or "listen" or "turn" or "call" && o.Op != "cell") h.GiveScripted(o); else h.Issue(o); }
            To(end); Assert.That(h.Fault, Is.Null, h.Status); return h;
        }
        finally { HandoverHost.SeedPin = 0; }
    }

    static void Words(string where, params string[] s) { foreach (var x in s) Assert.That(x ?? "", Does.Not.Contain("{").And.Not.Contain("}"), $"{where}: an unfilled fill in '{x}'"); }
    /// <summary>The rows as read, for the report (out/batch/hostlogic/why100/; never stdout: the batch's screen stays one line a suite).</summary>
    public static void Keep(string name, string text) { var d = System.IO.Path.Combine(Root.Path, "out/batch/hostlogic/why100"); Directory.CreateDirectory(d); File.WriteAllText(System.IO.Path.Combine(d, name + ".txt"), text); }
    static string Dump(HandoverHost h) => string.Join("\n", h.WhyRows.Select(r => $"  {HandoverHost.MissionClock(r.Tick)} {r.What}:{r.Key}{(r.Variant.Length > 0 ? "/" + r.Variant : "")} A{r.To} · {r.Title} · {r.ShortWhere} · {r.Line}"));

    [Test] public void FablesKeysArePortedAsTheSchemaSaysThem()
    {
        var t = PlayText.Load(); Assert.That(t.HasWhy, "text.json has the why block");
        var want = new Dictionary<string, string>
        {
            ["report:dead:p-records-office-01"] = "dead", ["report:cut:camera:entrance"] = "cut", ["report:waitress"] = "approach", ["report:office-staff"] = "staff", ["report:councillor"] = "let-go",
            ["confirmed:dead:guard"] = "confirmed", ["gunfire:guard"] = "gunfire", ["violence-seen:councillor:driver"] = "violence", ["drawn-seen:camera:pickup:enforcer"] = "drawn", ["drawn-seen:people:scout"] = "drawn-people",
            ["drawn-warned:guard:scout"] = "warned", ["takedown-fails:p-records-office-01"] = "fought", ["loiter:camera:pickup:fixer"] = "loiter", ["recorded:pay:driver:camera:pickup"] = "recorded",
            ["car-reported:patrol"] = "car", ["entry:patrol:enforcer"] = "entry", ["sighting:camera:entrance"] = "sighting", ["report:bogus:x"] = null, ["what-is-this"] = null,
        };
        foreach (var (cause, key) in want) Assert.That(PlayText.WhyKeyOf(cause), Is.EqualTo(key), cause);
        foreach (var k in PlayText.AlertCauses) Assert.That(t.WhyAlert.ContainsKey(k), $"alert.{k} has words");
        var v = t.Words("alert", "gunfire", "other"); Assert.That(v.Title, Is.EqualTo(t.WhyAlert["gunfire"].Base.Title), "a variant without a title keeps the base's"); Assert.That(v.Short, Is.EqualTo(t.WhyAlert["gunfire"].Variants["other"].Short));
        Assert.That(PlayText.FillWhy("{who} saw it {where}", new Dictionary<string, string> { ["who"] = "a clerk" }), Is.EqualTo("A clerk saw it {where}"), "an unknown fill stays as written");
        Assert.That(PlayText.FillWhy("{who} saw", new Dictionary<string, string> { ["who"] = "a clerk" }, false), Is.EqualTo("a clerk saw"));
        Assert.That(t.OnWayWays.Keys, Is.SupersetOf(new[] { "radio", "walk", "run", "look", "camera", "screen", "robot", "tell" })); Assert.That(t.OnWayFix, Is.Not.Empty); Assert.That(t.OnWayNofix, Is.Not.Empty);
    }

    [Test] public void FablesPlacesAreTheSitesOwn()
    {
        var h = Host.Fresh(); var missing = h.Text.WhyPlaces.Keys.Where(k => k != "nearby" && !h.Game.S.HasPlace(k)).ToList();
        Assert.That(missing, Is.Empty, "why.places names a place the site does not have: " + string.Join(", ", missing)); Assert.That(h.WhyPlacesKnown, Is.EqualTo(h.Text.WhyPlaces.Count - 1));
    }

    [Test] public void ACivilianIsNamedByJob()
    {
        var h = Host.Fresh(); var g = h.Game;
        foreach (var w in g.Walkers.Where(w => w.P.Civilian && w.P.Role.Length == 0).Take(40))
        {
            var n = h.PersonName(w.P); Assert.That(n, Is.Not.Empty.And.Not.Contain("{"));
            if (h.Text.TargetNames.TryGetValue("crowd-" + w.P.Job, out var job)) Assert.That(n, Is.EqualTo(job), $"{w.P.Id} ({w.P.Job})");
        }
    }

    /// <summary>The loud win (tools/game/pins/loud.seed3.json): every Alert rise is the ledger's, worded with who and where; the police calls say why; the cards take
    /// their cause; the meter lists them newest first; and the run with all of it is the engine's own run, hash for hash.</summary>
    [Test] public void TheLoudPinSaysWhyAtEveryRiseAndCall()
    {
        var h = Play("tools/game/pins/loud.seed3.json"); var g = h.Game; Keep("loud.seed3", Dump(h));
        Assert.That(h.WhyErrors, Is.Zero);
        var ledger = g.Ledger.Where(x => x.what == "alert").ToList(); var rises = h.WhyRows.Where(r => r.What == "alert").ToList();
        Assert.That(rises.Select(r => (r.Tick, r.To, r.Cause)), Is.EqualTo(ledger.Select(x => (x.tick, x.now, x.cause))), "one row per Alert rise, the ledger's own");
        foreach (var r in rises) { Assert.That(r.Short, Is.Not.Empty, r.Cause); Assert.That(r.Line, Is.Not.Empty, r.Cause); Words(r.Cause, r.Title, r.Short, r.Line, r.Where); }
        var shot = rises.First(); Assert.That(shot.Key, Is.EqualTo("gunfire")); Assert.That(shot.Variant, Is.Empty, "the cell fired: the base words"); Assert.That(shot.Line, Does.StartWith("The Enforcer fired at the guard")); Assert.That(shot.Where, Is.Not.Empty);
        var police = h.WhyRows.Where(r => r.What == "police").ToList(); Assert.That(police, Is.Not.Empty);
        foreach (var r in police) Words(r.Key, r.Title, r.Short, r.Line, r.Where);
        Assert.That(police.First().Tick, Is.EqualTo(shot.Tick)); Assert.That(police.First().Key, Is.EqualTo("heard").Or.EqualTo("witness"), "the first call is the shot's");
        Assert.That(police.Select(r => r.Key), Does.Contain("body"), "the body found brings police to hold the doors");
        // the cards: the Alert card and the first Police card carry their cause, and the line says it
        var alertCard = h.CrucialLog.First(c => c.Kind == "alert"); Assert.That(alertCard.Why, Is.SameAs(shot)); Assert.That(h.CrucialLine(alertCard), Does.StartWith(shot.ShortWhere));
        var policeCard = h.CrucialLog.First(c => c.Kind == "police"); Assert.That(policeCard.Why, Is.Not.Null); Assert.That(h.CrucialLine(policeCard), Does.StartWith(policeCard.Why.ShortWhere)); Assert.That(policeCard.Place, Is.Not.Null, "a click pans to where it happened");
        // the meter: newest first, every rise in it
        var meter = h.WhyMeterRows(20); Assert.That(meter.Select(r => r.Tick), Is.Ordered.Descending); Assert.That(meter.Where(r => r.What == "alert"), Is.EquivalentTo(rises));
        foreach (var r in meter) { var (when, what) = h.WhyMeterLines(r); Assert.That(when, Is.Not.Empty); Words("meter", when, what); if (r.Short.Length > 0) Assert.That(what, Is.Not.Empty, $"{r.Key}: a cause with no place keeps its line"); }
        var more = meter.Where(r => r.Key == "more").ToList(); Assert.That(more.Count, Is.EqualTo(1), "More police again is listed once"); Assert.That(more[0].Tick, Is.EqualTo(h.WhyRows.Last(r => r.Key == "more").Tick), "at its newest");
        Assert.That(h.WhyMeterLines(more[0]).what, Is.EqualTo("the search goes on"));
        Assert.That(h.WhyMeterHead(), Is.EqualTo("Why it's Alert 2").Or.StartsWith("Why it"));
        // read only: the engine alone, from the pin's start and orders, ends with the host's hash
        g.Finish(); var sc = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Root.Path, "tools/game/pins/loud.seed3.json")));
        var alone = Engine.Play(h.PlayInputs, sc.Start, sc.Orders, g.Tick); Assert.That(alone.ClosingHash(), Is.EqualTo(g.ClosingHash()), "the why changed the run");
        var saved = Script.Parse(Script.Write(g, h.PlayInputs.Hash)); Assert.That(saved.Verify(Engine.Play(h.PlayInputs, saved.Start, saved.Orders, saved.Until), h.PlayInputs.Hash), Is.Empty, "the saved run replays");
    }

    /// <summary>Every route and pin on its own seed: each rise is worded (or, for a cause Fable has no words for, says nothing), nothing throws, nothing is half-filled.</summary>
    [Test] public void EveryRouteWordsEveryRise()
    {
        var dir = System.IO.Path.Combine(Root.Path, "contracts/missions/handover/routes"); var files = Directory.GetFiles(dir, "*.json").Where(f => !f.EndsWith("routes.json")).Select(f => System.IO.Path.GetRelativePath(Root.Path, f)).ToList();
        files.AddRange(Directory.GetFiles(System.IO.Path.Combine(Root.Path, "tools/game/pins"), "*.json").Select(f => System.IO.Path.GetRelativePath(Root.Path, f)));
        var bad = new List<string>(); var all = new List<string>(); int rises = 0, calls = 0, marked = 0;
        foreach (var f in files)
        {
            int seenOnWay = 0; var h = Play(f, each: x => seenOnWay += x.OnWay.Count);
            var ledger = h.Game.Ledger.Where(x => x.what == "alert").Select(x => (x.tick, x.now, x.cause)).ToList(); var rows = h.WhyRows.Where(r => r.What == "alert").ToList();
            if (h.WhyErrors > 0) bad.Add($"{f}: {h.WhyErrors} errors");
            if (!rows.Select(r => (r.Tick, r.To, r.Cause)).SequenceEqual(ledger)) bad.Add($"{f}: rows {rows.Count} vs ledger {ledger.Count}");
            foreach (var r in h.WhyRows) foreach (var s in new[] { r.Title, r.Short, r.Line, r.Where }) if (s.Contains('{')) bad.Add($"{f}: {r.Key} '{s}'");
            foreach (var r in rows) if (r.Short.Length == 0 && PlayText.WhyKeyOf(r.Cause) != null) bad.Add($"{f}: {r.Cause} has a key and no short ({r.Key})");
            rises += rows.Count; calls += h.WhyRows.Count(r => r.What == "police"); marked += seenOnWay;
            // an order's own consequences are logged as it is given (the failed takedown's call, read a tick later): keyed at their own tick
            if (f.EndsWith("careless-enforcer-watches-guard.json") && !h.WhyRows.Any(r => r.What == "police" && r.Key == "attack" && r.Short == "the guard called in an attack")) bad.Add($"{f}: the guard's call is not keyed attack");
            if (f.EndsWith("fail-enforcer-down.json") && !h.WhyRows.Any(r => r.What == "seal" && r.Short == "the guard saw the Enforcer's gun")) bad.Add($"{f}: the seal's cause is not the drawn gun");
            all.Add($"{f}:\n{Dump(h)}");
        }
        all.Add($"{files.Count} runs, {rises} rises, {calls} calls, {marked} mark-ticks"); Keep("routes", string.Join("\n", all));
        Assert.That(bad, Is.Empty, string.Join("\n", bad));
    }
}

/// <summary>Brief 100, part 2: a report still on its way is marked over whoever carries it, with its way, its seconds and whether the cell can still stop it; one
/// stopped never lands and pops "Report stopped". The quiet runs are briefs/100/latch/route-g-radio(-stopped).json (route G's knockout, the Enforcer kept by the door).</summary>
public class Why100OnWay
{
    [Test] public void TheLoudPinsWalkersAndLookerAreMarked()
    {
        var seen = new Dictionary<string, (string way, bool fix, string plate, string fixLine, int s)>(); var cams = new List<string>();
        var h = Why100.Play("tools/game/pins/loud.seed3.json", 3080, x =>
        {
            foreach (var m in x.OnWay)
            {
                var (plate, fix, _) = x.OnWayWords(m); seen.TryAdd(m.Id + "@" + m.Way, (m.Way, m.Fix, plate, fix, x.OnWaySeconds(m)));
                if (m.Way == "camera") cams.Add(m.By);
                Assert.That(plate + fix, Does.Not.Contain("{"), m.Id);
            }
        });
        Assert.That(h.WhyErrors, Is.Zero);
        var walks = seen.Values.Where(v => v.way == "walk").ToList(); Assert.That(walks, Is.Not.Empty, "the clerks walking to the Office door at 4:54");
        Assert.That(walks[0].plate, Does.StartWith("Going to the Office door · ").And.EndsWith(" s"), walks[0].plate); Assert.That(walks[0].fix, "a walker can be stopped"); Assert.That(walks[0].fixLine, Is.EqualTo(h.Text.OnWayFix)); Assert.That(walks[0].s, Is.InRange(1, 30));
        Assert.That(seen.Values.Any(v => v.way == "look" && v.plate == h.Text.OnWayWays["look"].plate), "the Idealist going to look first");
        Assert.That(seen.Values.Any(v => v.way == "radio" && v.fix && v.fixLine == h.Text.OnWayFix), "the staff who radio it in");
        foreach (var c in cams.Distinct()) Assert.That(h.Game.Known(c)?.Sight ?? false, Is.True, $"{c}: a camera's tape is marked only once the cell has learned it (Sky 2026-11-07)");
    }

    [Test] public void TheRadioPlateCountsDownAndLands()
    {
        var secs = new List<int>(); string plate = ""; int from = -1;
        var h = Why100.Play("briefs/100/latch/route-g-radio.json", 2300, x => { var m = x.OnWay.FirstOrDefault(w => w.Way == "radio"); if (m != null) { if (from < 0) from = x.Game.Tick; secs.Add(x.OnWaySeconds(m)); plate = x.OnWayWords(m).plate; } });
        Why100.Keep("route-g-radio", $"radio marked from tick {from} ({HandoverHost.MissionClock(from)}), seconds {string.Join(",", secs.Distinct())}; rise at {h.WhyRows.FirstOrDefault(r => r.What == "alert")?.Tick}");
        Assert.That(secs, Is.Not.Empty, "the shift guard's radio is marked"); Assert.That(secs, Is.Ordered.Descending); Assert.That(secs[0], Is.InRange(4, 6)); Assert.That(secs[^1], Is.InRange(0, 1));
        Assert.That(plate, Does.StartWith("Radioing it in · "));
        var rise = h.WhyRows.First(r => r.What == "alert"); Assert.That(rise.Key, Is.EqualTo("down")); Assert.That(rise.ShortWhere, Does.StartWith("another guard ").And.EndsWith(" at the Office door"), "by.another: a guard reporting the guard"); Assert.That(rise.ShortWhere, Does.Contain(" the guard ")); // the fills, not Fable's verb (the style loop may reword it)
        Assert.That(h.StoppedReports, Is.Zero);
    }

    [Test] public void AStoppedReportNeverLandsAndSaysSo()
    {
        bool marked = false; Person radio = null; int from = -1, stoppedAt = -1, pops = 0;
        var h = Why100.Play("briefs/100/latch/route-g-radio-stopped.json", 2400, x => { var m = x.OnWay.FirstOrDefault(w => w.Way == "radio" && w.Fix); if (m != null) { marked = true; radio ??= x.Game.PersonById(m.By); if (from < 0) from = x.Game.Tick; } if (x.StoppedReports > pops) { pops = x.StoppedReports; stoppedAt = x.Game.Tick; } });
        Why100.Keep("route-g-radio-stopped", $"radio marked from tick {from} ({HandoverHost.MissionClock(from)}), stopped at {stoppedAt} ({HandoverHost.MissionClock(stoppedAt)}); rows:\n" + string.Join("\n", h.WhyRows.Select(r => $"  {HandoverHost.MissionClock(r.Tick)} {r.What}:{r.Key} · {r.Title} · {r.ShortWhere}")));
        Assert.That(marked, "the radio was marked, stoppable"); Assert.That(radio, Is.Not.Null);
        var rep = h.Game.Reps.First(r => r.P == radio); Assert.That(rep.Phase, Is.EqualTo("stopped")); Assert.That(h.Game.Reps.Any(r => r.P == radio && r.Phase == "landed"), Is.False, "his report never lands");
        Assert.That(h.StoppedReports, Is.EqualTo(1)); Assert.That(h.WhyErrors, Is.Zero);
        Assert.That(h.WhyRows.Where(r => r.What == "alert").All(r => r.Short.Length > 0), "the Alert that rose anyway says why");
        var pc = h.CrucialLog.First(c => c.Kind == "police"); Assert.That(pc.Why?.Key, Is.EqualTo("body"), "the card says why they come (a body found), not the lockdown it brings"); Assert.That(pc.Title, Is.EqualTo("Police coming"));
        var g = h.Game; g.Finish(); var sc = Script.Parse(File.ReadAllText(System.IO.Path.Combine(Root.Path, "briefs/100/latch/route-g-radio-stopped.json")));
        Assert.That(Engine.Play(h.PlayInputs, sc.Start, sc.Orders, g.Tick).ClosingHash(), Is.EqualTo(g.ClosingHash()), "the why and the marks changed the run");
    }
}
