using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Brief 080's host proof (`-proofOnly seed`; section `seed`, written to proof.json, not gated until Splice adds it): New game rolls a seed that is never 0, Replay keeps it, a new
    /// game rolls another; a played run's saved session replays exactly on its seed; the fire offer over the guard and its Alt show no hit chance and no seconds (brief 101, Sky's rule on
    /// numbers in play); closed-in's words for every why. The other proof
    /// sections run on seed 0, the neutral start (HandoverHost.SeedPin), which is what their fixed ticks read.</summary>
    public static class ProofSeed
    {
        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var keep = HandoverHost.SeedPin; HandoverHost.SeedPin = null;
            h.Begin(); h.IntelOpen = false; yield return null; int a = h.Game.Seed;
            h.BeginSameSeed(); yield return null; int again = h.Game.Seed;
            h.Begin(); yield return null; int b = h.Game.Seed;
            bool rolled = a > 0 && b > 0 && a != b, kept = again == a;
            h.StartClock(); h.SetPaused(true); yield return null; var g = h.Game;
            // a played fight: every agent draws and the Enforcer fires on the guard, on the run's own rolled shots
            // the pill as a player sees it: the Enforcer drawn, over the guard, then Alt (pictures for Glyph's 2x crop); brief 101: no "%" and no "n s" in either
            string chip = "";
            g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "draw" }); h.RunTo(g.Tick + 15); h.Select(OpId.Enforcer, false);
            string pillState = "", altLine = "";
            if (h.Views.Find("guard") is Transform gv && gv)
            {
                h.LookAt(gv.position); yield return null; HandoverHost.PointerWorld = gv.position + Vector3.up; for (int i = 0; i < 8; i++) yield return null; yield return new WaitForSecondsRealtime(0.3f);
                pillState = HudToolkit.PillState; chip = HudToolkit.PillWords; while (HudToolkit.ShotBusy) yield return null; log.Add("seed-fire-chip: " + HudToolkit.Shoot(Path.Combine(dir, "seed-fire-chip"), "active")); yield return null; while (HudToolkit.ShotBusy) yield return null;
                h.PeekLatch = true; yield return new WaitForSecondsRealtime(0.3f); altLine = HudToolkit.LockAltShown + " | " + HudToolkit.PillWords; log.Add("seed-fire-alt: " + HudToolkit.Shoot(Path.Combine(dir, "seed-fire-alt"), "active")); /* Glyph's budget §1 (briefs/067/glyph-budget.md): the state is the pill's, not the view's; Alt held over the guard keeps his pill (Glyph r4 item 6, settled 2026-11-09) */ yield return null; while (HudToolkit.ShotBusy) yield return null;
                h.PeekLatch = false; HandoverHost.PointerWorld = null; yield return null;
            }
            log.Add($"seed chip: pill state [{pillState}] alt [{altLine.Replace("\n", " / ")}]");
            g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "fire", Target = "guard" });
            while (g.Tick < 90 * Game.Hz && !g.Over) h.RunTo(Math.Min(90 * Game.Hz, g.Tick + 50));
            string path = Path.Combine(dir, "seed-session.script.json"); h.SaveSessionTo(path);
            var sc = Script.Parse(File.ReadAllText(path));
            var rg = Engine.Play(h.PlayInputs, sc.Start, sc.Orders, sc.Until);
            bool saved = sc.Start.Seed == b; string verdict = sc.Verify(rg, h.PlayInputs.Hash); bool same = verdict.Length == 0; ulong final = rg.Checkpoints[rg.Checkpoints.Count - 1].hash;
            // a second reading: the same orders on another seed are another game (a shot's draw, or a phase), and the record says which seed it was
            var other = Script.Parse(File.ReadAllText(path)); other.Start.Seed = b == int.MaxValue ? 1 : b + 1;
            var og = Engine.Play(h.PlayInputs, other.Start, other.Orders, other.Until); bool differs = og.Checkpoints[og.Checkpoints.Count - 1].hash != final;
            var whys = new[] { "no-driver", "car-held", "on-foot", "no-go" }; var missing = new List<string>();
            foreach (var w in whys) if (h.Text?.CauseFor("closed-in", w) == null) missing.Add(w); // brief 096: debrief.cause's closed-in words
            static bool Numbers(string w) => w.Contains("%") || System.Text.RegularExpressions.Regex.IsMatch(w, @"\d+(\.\d+)? s\b");
            bool ok = rolled && kept && saved && same && differs && missing.Count == 0 && !Numbers(chip) && !Numbers(altLine) && h.Text.TdComing.Length > 0;
            log.Add($"seed: rolled {a} {b}, replay {again}; saved {sc.Start.Seed}, replay same {same} ({verdict}), another seed differs {differs}; shots {g.CellShots} hits {g.CellHits}; chip \"{chip}\"; closedIn missing [{string.Join(",", missing)}]");
            json[0] = $"{{\"ok\": {(ok ? "true" : "false")}, \"rolled\": [{a}, {b}], \"replayKept\": {again}, \"savedSeed\": {sc.Start.Seed}, \"savedReplaysSame\": {(same ? "true" : "false")}, \"otherSeedDiffers\": {(differs ? "true" : "false")}, \"shots\": {g.CellShots}, \"hits\": {g.CellHits}, \"chip\": \"{chip}\", \"closedInMissing\": {missing.Count}}}";
            HandoverHost.SeedPin = keep; h.Begin(); h.IntelOpen = false; yield return null;
        }
    }
}
