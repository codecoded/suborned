// Brief 099, the debrief (Sky 2026-11-07: "that should be a like our book in terms of presentation, nice, visually appealing with explainers and stats", then "yeah, try A"):
// every ending opens the book on the run's front page (Glyph's concept A), built from the ended run alone, laid out on the book's grid; it reads only, so a saved run replays
// to the same hash whether the page was opened or not. Same harness as HostLogic.cs (the real engine, the real text.json and the marked stand-ins, no views).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Suborned.Play;
using Suborned.Sim;
using Path = System.IO.Path;

public class Debrief099
{
    static float Tw(string s, float size, string font) => (s?.Length ?? 0) * size * .52f;
    const string QuietWin = "contracts/missions/handover/routes/route-a.json", Lookout = "contracts/missions/handover/routes/fail-rivals-reach-vale.json", Loud = "tools/game/pins/loud.seed3.json";

    /// <summary>A script played through the host to its end, then the debrief opened as the game opens it (BookOpenEnd, from the book's key check).</summary>
    public static HandoverHost Ended(string path)
    {
        var h = Why100.Play(path); Assert.That(h.Game.Over && h.Ending.Length > 0, $"{path} did not end: tick {h.Game.Tick}");
        Assert.That((bool)Host.Call(h, "BookOpenEnd"), "the ended run opens its debrief"); Assert.That(h.BookOpen && h.BookEnd && h.BookSectionNow == "job" && h.Modal == ModalGate.Modal.Book);
        return h;
    }
    static void Keep(string name, string text) { var d = Path.Combine(Root.Path, "out/batch/hostlogic/debrief099"); Directory.CreateDirectory(d); File.WriteAllText(Path.Combine(d, name + ".txt"), text); }
    public static string Dump(BookPage p) => string.Join("\n", new[] { $"{p.Kicker} | {p.Name} | {p.Sub} | face {p.HeroFace}", $"way {p.WayWord} ({p.WayNote})" }
        .Concat(p.Ticks.Select(t => $"  {(t.Met ? "✓" : t.Lost ? "✕" : t.Unknown ? "?" : "○")} {t.Words} · {t.Note}"))
        .Concat(new[] { $"next {p.NextHead}: {p.NextLine} [{string.Join(", ", p.NextWays.Select(c => c.Word + " " + c.Who))}]" })
        .Concat(p.Stats.Select(s => $"  {s.Label} {s.Value}{(s.Hot ? " (hot)" : "")}: {s.Why}"))
        .Concat(p.City.Select(c => $"  city {c.Icon}: {c.Head} / {c.Line}"))
        .Concat(new[] { "run " + string.Join(" · ", p.Track.Select(s => $"{s.Word} {s.Time}{(s.Face.Length > 0 ? " [face]" : "")}{(s.Hot ? " (hot)" : "")}")) })
        .Concat(p.Fold == null ? new string[0] : new[] { $"fold {p.Fold.Title} {p.Fold.Head}: {p.Fold.Means}", $"  pips {string.Join(" ", p.Fold.Pips.Select(x => x.n + (x.hot ? "!" : "")))}" }.Concat(p.Fold.Rows.Select(r => $"  {r.Value} {r.Words} {r.When} [{string.Join(",", r.Faces)}]")).Append("  law " + p.Fold.Law))
        .Concat(new[] { "ways " + string.Join(" · ", p.Again.Select(a => a.Word + (a.Main ? "*" : ""))) + $" | {p.NewWord} / {p.SameWord}" }));

    /// <summary>An open fold lies over the dimmed rows when it opens above the numbers (the veil steps them back): only that overlap is the design's.</summary>
    static bool Under(BookSlot a, BookSlot fold) => fold == BookSlot.Fold && a is BookSlot.WayIn or BookSlot.Ticks or BookSlot.Next;
    /// <summary>Every slot on the page, inside the grid; the text column's slots never over one another; nothing under the ways on; every word filled.</summary>
    static void Fits(BookPage p, string what)
    {
        var r = BookLayout.Place(p, Tw);
        foreach (var it in r.Items) Assert.That(it.At.xMin >= 0f && it.At.yMin >= 0f && it.At.xMax <= 1920f && it.At.yMax <= 1080f, $"{what}: {it.Slot}#{it.Index} {it.At} off the page");
        var col = r.Items.Where(i => i.Slot is BookSlot.WayIn or BookSlot.Ticks or BookSlot.Next or BookSlot.Stats or BookSlot.Fold or BookSlot.City or BookSlot.Track).ToList();
        for (int a = 0; a < col.Count; a++) for (int b = a + 1; b < col.Count; b++)
                if (col[a].Slot != col[b].Slot && !Under(col[a].Slot, col[b].Slot) && !Under(col[b].Slot, col[a].Slot)) Assert.That(col[a].At.Overlaps(col[b].At), Is.False, $"{what}: {col[a].Slot} {col[a].At} over {col[b].Slot} {col[b].At}");
        var ways = r.Items.Where(i => i.Slot == BookSlot.Again).ToList(); Assert.That(ways, Is.Not.Empty, $"{what}: no ways on");
        foreach (var it in col) foreach (var w in ways) Assert.That(it.At.Overlaps(w.At), Is.False, $"{what}: {it.Slot} {it.At} under the way on {w.At}");
        var run = r.Items.Where(i => i.Slot == BookSlot.Track).ToList(); if (run.Count > 0) Assert.That(run[^1].Index, Is.EqualTo(p.Track.Count - 1), $"{what}: the run keeps its end");
        foreach (var it in run) foreach (var l in r.Debrief.Run[it.Index]) Assert.That(Tw(l, r.Debrief.RunSize, "Jost-SemiBold"), Is.LessThanOrEqualTo(r.TrackStep - 14f + .01f), $"{what}: the run's '{l}' runs past its column ({r.TrackStep:0} px)");
        Host.NoTokens(what, new[] { p.Kicker, p.Name, p.Sub, p.WayWord, p.WayNote, p.NextHead, p.NextLine }.Concat(p.Ticks.SelectMany(t => new[] { t.Words, t.Note })).Concat(p.Stats.SelectMany(s => new[] { s.Label, s.Value, s.Why }))
            .Concat(p.City.SelectMany(c => new[] { c.Head, c.Line })).Concat(p.Track.Select(s => s.Word)).Concat(p.Again.Select(a => a.Word)).Concat(p.Fold == null ? new string[0] : p.Fold.Rows.Select(x => x.Words).Append(p.Fold.Law).Append(p.Fold.Means)).ToArray());
    }

    [Test] public void TheQuietWinIsTheStartPageAnswered()
    {
        var h = Ended(QuietWin); var p = h.BookPageNow; var g = h.Game; var dw = DebriefWords.Get(); Keep("quiet-win", Dump(p));
        Assert.That(g.Result, Is.EqualTo(Outcome.Success)); Assert.That(p.KickerHot, Is.False); Assert.That(p.Kicker, Does.EndWith(HandoverHost.MissionClock(g.Tick)).And.Contain("HANDOVER"));
        Assert.That(p.Name, Is.EqualTo(h.ValeOut ? BookWordsFile.Get().EndWinHead : dw.Debrief("headline", "win")).And.Not.Empty); Assert.That(p.Sub, Is.Not.Empty);
        Assert.That(p.WayWord, Is.EqualTo(h.Text.BriefWord("ways.front-door", null)), "route A goes in by the front door");
        var want = BookWordsFile.Get().Needs["front-door"].Where(n => n.Id is not ("go" or "left-safe")).ToList(); Assert.That(p.Ticks.Count, Is.EqualTo(want.Count), "the way's own column, as the start page shows it under Front door (the shared go and left-safe are the win itself)");
        foreach (var id in new[] { "guard-away", "door-camera", "inside", "getaway" }) { var t = p.Ticks[want.FindIndex(n => n.Id == id)]; Assert.That(t.Met, $"{id} came true on a win"); Assert.That(t.Note, Does.Match(@"\d:\d\d$"), $"{id}: when it came true"); }
        Assert.That(p.Ticks[want.FindIndex(n => n.Id == "guard-away")].Note, Does.StartWith(dw.W("how", "called")), "Inez's call drew the guard off");
        Assert.That(p.Ticks.Any(t => t.Lost), Is.False, "a win loses no row"); Assert.That(p.NextLine, Is.Empty);
        Assert.That(p.Stats.Select(s => s.Id), Is.EqualTo(new[] { "time", "cash", "trail", "alert", "standing" }));
        Assert.That(p.Stats[0].Value, Is.EqualTo(HandoverHost.MissionClock(g.Tick))); Assert.That(p.Stats[2].Value, Is.EqualTo("+" + g.Trail));
        long spent = (g.Start.Cash ?? g.S.Purse) - g.Cash; Assert.That(p.Stats[1].Value, Is.EqualTo("$" + spent.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)));
        Assert.That(p.Stats.All(s => s.Why.Length > 0), "every number has its reason: " + string.Join(" | ", p.Stats.Select(s => s.Label + ": " + s.Why)));
        Assert.That(p.Track.Count, Is.InRange(3, 8)); Assert.That(p.Track[^1].Word, Is.EqualTo(dw.W("beats", "away"))); Assert.That(p.Track.Any(s => s.Face.Length > 0), "the cell's acts as their faces");
        Assert.That(p.Again.Select(a => a.Id), Is.EqualTo(new[] { "start", "again" }), "a win has no fail camera to watch"); Assert.That(p.SameWord, Does.EndWith(" · " + g.Seed));
        Assert.That(p.City.Count, Is.GreaterThan(0)); Fits(p, "quiet win");
    }

    [Test] public void TheLookoutsFailMarksTheRowItLostAndSaysWhatToDoNextTime()
    {
        var h = Ended(Lookout); var p = h.BookPageNow; var g = h.Game; var dw = DebriefWords.Get(); Keep("fail-lookout", Dump(p));
        var c = g.Cause as FailCause; Assert.That(g.FailCode, Is.EqualTo("rivals-reach-vale")); Assert.That(c?.Why, Is.EqualTo("lookout"), "the scripted fail is the lookout's");
        Assert.That(p.KickerHot); Assert.That(p.Name, Is.EqualTo(h.EndCauseWords().Head));
        Assert.That(p.WayWord, Is.EqualTo(h.Text.BriefWord("ways.pickup", null)), "the driver was turned: the pickup");
        var lost = p.Ticks.Single(t => t.Lost); var want = BookWordsFile.Get().Needs["pickup"];
        Assert.That(lost.Words, Is.EqualTo(want.Single(n => n.Id == "has-vale").Words), "the row it lost: the Enforcer never had Popovic");
        Assert.That(lost.Note, Does.EndWith(" · " + HandoverHost.MissionClock(g.Tick)).And.StartWith(char.ToLowerInvariant(p.Name[0]).ToString()));
        Assert.That(p.Ticks[want.FindIndex(n => n.Id == "driver")].Met, "the driver was turned");
        Assert.That(p.NextHead, Is.EqualTo(dw.W("next", "head"))); Assert.That(p.NextLine, Is.EqualTo(dw.W("next", "by", "rivals-reach-vale", "lookout", "line")).And.Not.Empty);
        Assert.That(p.NextWays.Select(w => w.Word), Is.EqualTo(BookWordsFile.Get().FeedCards["lookout"].Fixes.Select(f => f.word)), "the cell's own ways on the lookout (the crucial card's)");
        Assert.That(p.Stats[0].Why, Is.EqualTo(dw.W("next", "by", "rivals-reach-vale", "lookout", "time")));
        Assert.That(p.Track[^1].Hot && p.Track[^1].Word == dw.W("beats", "lost")); Fits(p, "lookout fail");
    }

    [Test] public void TheLoudWinsTrailFoldNamesWhoLeftEveryPoint()
    {
        var h = Ended(Loud); var g = h.Game; h.DebriefFold("trail"); var p = h.BookPageNow; Keep("loud-trail", Dump(p));
        Assert.That(p.Fold?.Id, Is.EqualTo("trail")); var laid = BookLayout.Place(p, Tw); Assert.That(laid.Has(BookSlot.City) || laid.Has(BookSlot.Track), Is.False, "the fold takes the city's and the run's place");
        Assert.That(p.Fold.Pips.Sum(x => x.n), Is.EqualTo(g.Trail), "one pip a point"); Assert.That(h.DebriefUnworded, Is.Empty, "every Trail cause has its words: " + string.Join(", ", h.DebriefUnworded));
        int shown = p.Fold.Rows.Sum(r => BookLayout.SignedValue(r.Value)); Assert.That(shown, Is.EqualTo(g.Trail), "the rows add up to the Trail");
        var lay = BookLayout.Place(p, Tw).Debrief; Assert.That(lay.FoldRows, Is.GreaterThanOrEqualTo(3), "the fold shows its biggest rows"); if (lay.FoldRows < p.Fold.Rows.Count) Assert.That(lay.FoldRest && BookLayout.SignedValue(lay.FoldRestValue) == p.Fold.Rows.Skip(lay.FoldRows - 1).Sum(r => BookLayout.SignedValue(r.Value)), "the last row sums the rest");
        var rivals = p.Fold.Rows.First(); Assert.That(rivals.Hot && rivals.Faces.Count >= 2, $"the biggest row is the rivals' shooting, by more than one of us: {rivals.Words}");
        Assert.That(p.Fold.Rows.Select(r => BookLayout.SignedValue(r.Value)), Is.Ordered.Descending);
        Assert.That(p.Fold.Law, Does.Contain("+" + g.R.GunfireTrail)); Fits(p, "loud trail fold");
        var cases = h.EndDebrief.Next.Cases; Assert.That(cases, Is.Not.Empty, "the loud run's killing opens a case");
        Assert.That(p.City.Select(c => c.Line), Does.Contain(DebriefWords.Fill(DebriefWords.Get().W("city", "case", "line"), false, ("missions", cases[0].missions.ToString()))), "{missions} is the count alone, as Fable's line has it (\"{missions}-mission case\")");
        Assert.That(p.City.Select(c => c.Line), Has.None.Contains("missions-mission"));
        h.DebriefFold("trail"); Assert.That(h.BookPageNow.Fold, Is.Null, "a second click shuts it");
    }

    [Test] public void EveryNumbersFoldOpensAndFits()
    {
        foreach (var path in new[] { QuietWin, Lookout, Loud })
        {
            var h = Ended(path); var all = new List<string>();
            foreach (var id in new[] { "time", "cash", "trail", "alert", "standing" })
            {
                h.DebriefFold(id); var p = h.BookPageNow; Assert.That(p.Fold?.Id, Is.EqualTo(id), $"{path}: {id}"); Assert.That(p.Fold.Title, Does.StartWith(p.Stats.First(s => s.Id == id).Label));
                Fits(p, $"{Path.GetFileName(path)} {id}"); all.Add(Dump(p)); h.DebriefFold(null);
            }
            Keep("folds-" + Path.GetFileNameWithoutExtension(path), string.Join("\n\n", all));
        }
    }

    /// <summary>Every route script to its end (the wins, each fail cause, the withdrawal, the final fights): the page opens, fits, says nothing half-filled, has its five numbers
    /// and the run ends on the outcome; a fail names the row it lost at.</summary>
    [Test] public void EveryEndingOpensItsDebrief()
    {
        var dir = Path.Combine(Root.Path, "contracts/missions/handover/routes"); var files = Directory.GetFiles(dir, "*.json").Where(f => !f.EndsWith("routes.json")).OrderBy(f => f).ToList();
        var said = new List<string>(); var codes = new HashSet<string>(); int fails = 0;
        foreach (var f in files)
        {
            var rel = Path.GetRelativePath(Root.Path, f); var h = Why100.Play(rel); var g = h.Game; if (!g.Over) { said.Add($"{Path.GetFileName(f)}: did not end by {HandoverHost.MissionClock(g.Tick)}"); continue; }
            Assert.That((bool)Host.Call(h, "BookOpenEnd"), $"{rel}: the debrief did not open ({h.Ending})"); var p = h.BookPageNow; bool win = g.Result == Outcome.Success;
            Assert.That(p.Name, Is.Not.Empty, rel); Assert.That(p.Stats.Count, Is.EqualTo(5), rel); Assert.That(p.Track.Count, Is.InRange(1, 8), rel);
            Assert.That(p.Track[^1].Hot, Is.EqualTo(g.Result == Outcome.Failure), $"{rel}: the run ends on the outcome (a fail's in the orange)");
            h.DebriefFold("time"); var tl = BookLayout.Place(h.BookPageNow, Tw).Debrief; Assert.That(tl.FoldShown.Any(i => h.BookPageNow.Fold.Rows[i].Keep), $"{rel}: the Time fold always shows the end"); h.DebriefFold(null); if (p.Ticks.Count > 0) Assert.That(p.Ticks.Count(t => t.Lost), Is.EqualTo(g.Result == Outcome.Failure ? 1 : 0), $"{rel}: one row lost a fail; a win or a withdrawal none");
            Fits(p, Path.GetFileName(f)); codes.Add(win ? "win" : g.FailCode); if (!win) fails++;
            said.Add($"{Path.GetFileName(f)}: {g.Result} {g.FailCode} {HandoverHost.MissionClock(g.Tick)} · {p.Name} · {p.WayWord} {p.Ticks.Count(t => t.Met)}/{p.Ticks.Count} · lost: {p.Ticks.FirstOrDefault(t => t.Lost)?.Words} · next: {p.NextLine} · ways {string.Join("/", p.Again.Select(x => x.Id))}");
        }
        Keep("endings", string.Join("\n", said)); Assert.That(codes.Count, Is.GreaterThanOrEqualTo(6), "the scripts cover a win and several fail causes: " + string.Join(", ", codes)); Assert.That(fails, Is.GreaterThan(5));
    }

    /// <summary>The run's labels at the schema's widest (debrief.schema.ts, beats: 18 characters at the widest fills), as one word that cannot break and as three words that break
    /// in two, on every ending, with the widest seed after Same start (HostSeed.RollSeed: up to int.MaxValue): every page fits (Fits: each label inside its column, every column clear
    /// of the ways on, the run's end kept); a page with no Next time has the room for its run. Which pages give their run way, and how, is kept in widest.txt.</summary>
    [Test] public void EveryEndingFitsTheWidestRunLabels()
    {
        var dir = Path.Combine(Root.Path, "contracts/missions/handover/routes"); var files = Directory.GetFiles(dir, "*.json").Where(f => !f.EndsWith("routes.json")).OrderBy(f => f).ToList();
        var said = new List<string>(); var dw = DebriefWords.Get(); int pages = 0;
        foreach (var f in files)
        {
            var rel = Path.GetRelativePath(Root.Path, f); var h = Why100.Play(rel); if (!h.Game.Over) continue;
            Assert.That((bool)Host.Call(h, "BookOpenEnd"), rel); var p = h.BookPageNow; p.SameWord = $"{dw.W("ways", "same")} · {int.MaxValue}";
            foreach (var widest in new[] { "Counterinfiltrated", "Popovic walked out" })
            {
                Assert.That(widest.Length, Is.EqualTo(18), "the beats' slot at its widest (debrief.schema.ts)"); foreach (var s in p.Track) s.Word = widest;
                string what = $"{Path.GetFileName(f)} '{widest}'"; Fits(p, what); pages++;
                var r = BookLayout.Place(p, Tw); var run = r.Items.Where(i => i.Slot == BookSlot.Track).ToList();
                if (p.NextLine.Length == 0) Assert.That(run, Is.Not.Empty, $"{what}: a page with no Next time has the room for its run");
                said.Add($"{what}: run {run.Count}/{p.Track.Count}{(run.Count > 0 ? $" at {r.Debrief.RunSize} px on {run.Max(i => r.Debrief.Run[i.Index].Length)} line(s)" : "")}, city {r.Items.Count(i => i.Slot == BookSlot.City)}/{p.City.Count}, rows {r.Debrief.TicksShown}/{p.Ticks.Count}");
            }
        }
        Keep("widest", string.Join("\n", said)); Assert.That(pages, Is.GreaterThan(60), "every ending, twice");
    }

    [Test] public void OpeningAFoldMovesNothingAboveIt()
    {
        foreach (var path in new[] { QuietWin, Lookout, Loud })
        {
            var h = Ended(path); var shut = BookLayout.Place(h.BookPageNow, Tw);
            string Above(BookLayout.Result r) => string.Join(" ", r.Items.Where(i => i.Slot is BookSlot.WayIn or BookSlot.Ticks or BookSlot.Next or BookSlot.Stats or BookSlot.Name or BookSlot.Sub).Select(i => $"{i.Slot}{i.Index}@{i.At.x:0},{i.At.y:0}"));
            foreach (var id in new[] { "time", "cash", "trail", "alert", "standing" }) { h.DebriefFold(id); Assert.That(Above(BookLayout.Place(h.BookPageNow, Tw)), Is.EqualTo(Above(shut)), $"{Path.GetFileName(path)}: opening {id} moved the page"); h.DebriefFold(null); }
        }
    }

    [Test] public void ThePageReadsOnlyAndTheSavedRunReplaysToTheSameHash()
    {
        foreach (var path in new[] { QuietWin, Lookout, Loud })
        {
            var h = Ended(path); var g = h.Game; ulong before = Hash.State(g); int ledger = g.Ledger.Count, log = g.Log.Count, orders = g.Orders.Count;
            foreach (var id in new[] { "time", "cash", "trail", "alert", "standing" }) { h.DebriefFold(id); h.DebriefFold(null); }
            h.DebriefSame(true); h.BookShow("cell", 1); h.BookShow("job"); h.BookShow("journal"); h.BookShow("job");
            Assert.That(Hash.State(g), Is.EqualTo(before), $"{path}: the page changed the run"); Assert.That((g.Ledger.Count, g.Log.Count, g.Orders.Count), Is.EqualTo((ledger, log, orders)));
            var saved = Script.Parse(Script.Write(g, h.PlayInputs.Hash)); Assert.That(saved.Verify(Engine.Play(h.PlayInputs, saved.Start, saved.Orders, saved.Until), h.PlayInputs.Hash), Is.Empty, $"{path}: the saved run replays");
        }
    }

    [Test] public void ARunNotSteppedHereGetsItsRowsFromAReplay()
    {
        var live = Ended(QuietWin); var want = live.BookPageNow.Ticks.Select(t => (t.Met, t.Note)).ToList(); var played = live.Game;
        var h = Host.Fresh(); Host.Set(h, "Game", played); Host.Set(h, "EndDebrief", Debrief.Of(played)); Host.Set(h, "Ending", "adopted");
        Assert.That((bool)Host.Call(h, "BookOpenEnd")); Assert.That(h.NeedsReplayed, "an adopted run is replayed for its rows");
        Assert.That(h.BookPageNow.Ticks.Select(t => (t.Met, t.Note)), Is.EqualTo(want), "the replay's rows are play's");
    }

    [Test] public void TryAgainKeepsTheSameStartOrRollsANewOne()
    {
        var h = Ended(QuietWin); int seed = h.Game.Seed; h.DebriefSame(true); Assert.That(h.BookPageNow.SameOn);
        h.DebriefWay("again"); Assert.That(h.BookOpen && h.BookEnd, Is.False, "a new run shuts the debrief"); Assert.That(h.Game.Tick, Is.EqualTo(0)); Assert.That(h.Game.Seed, Is.EqualTo(seed), "the same start keeps its seed");
        Assert.That(h.DebriefSameStart, Is.False, "the next ending offers a new start again");
    }

    [Test] public void EveryWordThePageReadsIsFablesOrAMarkedStandIn()
    {
        foreach (var path in new[] { QuietWin, Lookout, Loud }) { var h = Ended(path); foreach (var id in new[] { "time", "cash", "trail", "alert", "standing" }) { h.DebriefFold(id); } }
        var dw = DebriefWords.Get(); Assert.That(dw.FromStandIn.Count + dw.FromFable.Count, Is.GreaterThan(30));
        Keep("words", $"from Fable's debrief.page: {dw.FromFable.Count}\n  {string.Join("\n  ", dw.FromFable)}\nfrom the stand-ins: {dw.FromStandIn.Count}\n  {string.Join("\n  ", dw.FromStandIn)}");
        var stand = JNode.Parse(File.ReadAllText(Path.Combine(Root.Path, "unity/Assets/Suborned/Resources/Debrief099/standins.json")));
        foreach (var k in new[] { "head", "way", "how", "numbers", "reasons", "trail", "city", "beats", "next", "ways" }) Assert.That(stand.Has(k), $"the stand-ins keep Fable's shape: {k}");
    }
}
