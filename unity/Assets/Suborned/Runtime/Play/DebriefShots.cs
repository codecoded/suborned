using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Brief 099, `-debriefShots &lt;dir&gt; -capture-size 1920x1080`: the debrief page in the real player beside Glyph's concept (briefs/099/glyph/A-front-*.png): the quiet
    /// win (route A), the lookout's fail at 5:53 (its Time fold too, which opens over the numbers), the loud win with Trail opened, the two deepest fail pages (where the run gives
    /// way), a withdrawal, and the start page after Try again (its kicker beside the debrief's). Each script is played through the host's own
    /// order paths to its end; the fail camera plays as in the game; the page opens as the game opens it. For each picture: the page's manifest (tools/hud-budget, state page),
    /// its layout audit (the HUD's own and the page's), its words (pages.txt); and, once a run's page has been opened and every fold toggled, its saved session replayed to the
    /// same hash. Its own flag; no proof section runs it. Reads only.</summary>
    public sealed class DebriefShots : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (GameBoot.ArgValue("-debriefShots") is string d) { Application.runInBackground = true; var go = new GameObject("debrief-shots"); DontDestroyOnLoad(go); go.AddComponent<DebriefShots>().Dir = d; } }
        string Dir = ""; readonly StringBuilder pages = new(), audit = new();

        /// <summary>The repo: up from the app (or the Editor's Assets) to the folder that holds contracts/missions, however deep the player was built (a scratch player sits deeper than out/build/play).</summary>
        static string Root { get { var d = new System.IO.DirectoryInfo(Application.dataPath); for (int i = 0; i < 14 && d != null; i++, d = d.Parent) if (Directory.Exists(Path.Combine(d.FullName, "contracts", "missions"))) return d.FullName; return Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))); } }

        IEnumerator Shot(HandoverHost h, string name)
        {
            yield return new WaitForSecondsRealtime(0.9f); yield return new WaitForEndOfFrame(); // the page's slide (0.16 s) and the street's glide (0.45 s) settle
            var t = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(Dir, name + ".png"), t.EncodeToPNG()); Destroy(t);
            File.WriteAllText(Path.Combine(Dir, name + ".manifest.json"), HudToolkit.BookManifest());
            var bad = HudToolkit.BookAudit(); bad.AddRange(HudToolkit.AuditText());
            audit.AppendLine($"{name}: {(bad.Count == 0 ? "clean" : bad.Count + " finding(s)")}"); foreach (var b in bad) audit.AppendLine("  " + b);
            var p = h.BookPageNow; pages.AppendLine($"== {name} ({Screen.width}x{Screen.height}, build {h.DebriefBuildMs:0.0} ms, rows replayed {h.NeedsReplayed})"); if (p != null) pages.AppendLine(PageText(p));
            if (p != null && HudToolkit.BookLaidOut is { Debrief: not null } lr) // the run as the page laid it out: the stops it kept, their size, and the city's rows shown
            {
                var run = new List<string>(); foreach (var it in lr.Of(BookSlot.Track)) run.Add(string.Join(" / ", lr.Debrief.Run[it.Index])); int city = 0; foreach (var _ in lr.Of(BookSlot.City)) city++;
                pages.AppendLine($"  laid out: run {run.Count}/{p.Track.Count} at {lr.Debrief.RunSize:0.#} px: {string.Join(" · ", run)}; city {city}/{p.City.Count}; rows {lr.Debrief.TicksShown}/{p.Ticks.Count}\n");
            }
            Debug.Log($"debrief-shots: {name} {Screen.width}x{Screen.height}, audit {bad.Count}");
        }

        /// <summary>A script to its end through the host, as a player gives the orders (the Watch, Listen, Turn and Call through the scripted give), on the script's seed.</summary>
        static IEnumerator Play(HandoverHost h, string rel)
        {
            var sc = Script.Parse(File.ReadAllText(Path.Combine(Root, rel))); HandoverHost.SeedPin = sc.Start.Seed;
            if (h.BookOpen) h.BookClose(); h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null;
            var list = new List<(int i, Order o)>(); for (int i = 0; i < sc.Orders.Count; i++) list.Add((i, sc.Orders[i])); list.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.i.CompareTo(b.i));
            var g = h.Game; int end = Game.T(g.S.Over) + 10;
            foreach (var (_, o) in list) { if (g.Over) break; h.RunTo(o.Tick); if (g.Over) break; if (o.Do is "watch" or "listen" or "turn" or "call" && o.Op != "cell") h.GiveScripted(o); else h.Issue(o); }
            if (!g.Over) h.RunTo(end);
            float t0 = Time.realtimeSinceStartup; while (!(h.BookOpen && h.BookEnd) && Time.realtimeSinceStartup - t0 < 45f) yield return null; // the fail camera first, as in the game
        }

        /// <summary>A runner that threw never leaves the player waiting: after eight minutes what it has is written and the player quits (4).</summary>
        IEnumerator Watchdog() { yield return new WaitForSecondsRealtime(480f); audit.AppendLine("the runner did not finish in 8 minutes: see player.log"); Write(); Application.Quit(4); }
        void Write() { File.WriteAllText(Path.Combine(Dir, "pages.txt"), pages.ToString()); File.WriteAllText(Path.Combine(Dir, "audit.txt"), audit.ToString()); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir); InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; StartCoroutine(Watchdog());
            HandoverHost h; float t0 = Time.realtimeSinceStartup; while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 90f) { Application.Quit(3); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f);
            h.controls.conversations = ConversationScenes.Never; h.controls.planTogether = false; var verdicts = new List<string>();
            foreach (var (name, rel, folds) in new[] { ("debrief-quiet-win", "contracts/missions/handover/routes/route-a.json", new string[0]), ("debrief-fail-lookout", "contracts/missions/handover/routes/fail-rivals-reach-vale.json", new[] { "time" }), ("debrief-loud", "tools/game/pins/loud.seed3.json", new[] { "trail" }),
                ("debrief-tall", "contracts/missions/handover/routes/fight-safe-unguarded.json", new string[0]), ("debrief-tallest", "contracts/missions/handover/routes/route-g-fast-board.json", new string[0]) }) // the two deepest fail pages: the run gives way
            {
                if (!File.Exists(Path.Combine(Root, rel))) { audit.AppendLine($"{name}: no script at {Path.Combine(Root, rel)}"); continue; }
                yield return Play(h, rel);
                if (!(h.BookOpen && h.BookEnd)) { audit.AppendLine($"{name}: the debrief never opened ({h.Ending}; fail camera {h.FailCamOn})"); continue; }
                yield return Shot(h, name);
                foreach (var f in folds) { h.DebriefFold(f); yield return Shot(h, name + "-" + f); h.DebriefFold(null); yield return null; }
                foreach (var f in new[] { "time", "cash", "trail", "alert", "standing" }) { h.DebriefFold(f); yield return null; h.DebriefFold(null); } // every fold toggled, then the saved run replayed
                float w0 = Time.realtimeSinceStartup; while (h.Ending.Contains("checking") && Time.realtimeSinceStartup - w0 < 30f) yield return null;
                string replay; try { var saved = Script.Parse(File.ReadAllText(h.SessionPath)); var diffs = saved.Verify(Engine.Play(h.PlayInputs, saved.Start, saved.Orders, saved.Until), h.PlayInputs.Hash); replay = string.IsNullOrEmpty(diffs) ? "the saved run replays to the same hash" : "DIFFERS: " + diffs; }
                catch (Exception e) { replay = "replay failed: " + e.Message; }
                verdicts.Add($"{name}: {h.Ending} · after the page and every fold: {replay}");
                if (h.FailCamAvailable) // Watch the replay: the fail camera plays the deciding moment, then the page comes back
                {
                    h.DebriefWay("replay"); float r0 = Time.realtimeSinceStartup; while (!h.FailCamOn && Time.realtimeSinceStartup - r0 < 15f) yield return null; bool played = h.FailCamOn;
                    while (!(h.BookOpen && h.BookEnd) && Time.realtimeSinceStartup - r0 < 60f) yield return null;
                    verdicts.Add($"{name}: Watch the replay: the fail camera {(played ? "played" : "DID NOT PLAY")}, the page {(h.BookOpen && h.BookEnd ? "came back" : "DID NOT COME BACK")}");
                }
            }
            // Try again on the same start: the book shuts, a new run on the same seed at 0:00, the start's book opens on Job
            if (h.BookOpen && h.BookEnd) { int seed = h.Game.Seed; h.DebriefSame(true); h.DebriefWay("again"); yield return null; yield return null; yield return null; verdicts.Add($"Try again (same start): seed {h.Game.Seed} (was {seed}), tick {h.Game.Tick}, the start's book {(h.BookOpen && h.BookStart ? "open on " + h.BookSectionNow : "NOT OPEN")}");
                if (h.BookOpen && h.BookStart) yield return Shot(h, "start-page"); } // the start page's kicker, beside the debrief's
            // a withdrawal's page, then Back to the start: the main menu over the stopped street
            yield return Play(h, "contracts/missions/handover/routes/withdraw.json");
            if (h.BookOpen && h.BookEnd) { yield return Shot(h, "debrief-withdraw"); h.DebriefWay("start"); yield return null; yield return null; verdicts.Add($"Back to the start: the main menu {(FrontEnd.Covers ? "is up" : "IS NOT UP")}, the book {(h.BookOpen ? "STILL OPEN" : "shut")}"); }
            else verdicts.Add("withdraw: the debrief never opened");
            Write(); File.WriteAllText(Path.Combine(Dir, "replay.txt"), string.Join("\n", verdicts) + "\n");
            HandoverHost.SeedPin = 0; Application.Quit(0);
        }

        static string PageText(BookPage p)
        {
            var sb = new StringBuilder($"{p.Kicker} | {p.Name} | {p.Sub}\n{p.WayWord} {p.WayNote}\n");
            foreach (var t in p.Ticks) sb.AppendLine($"  {(t.Met ? "✓" : t.Lost ? "✕" : t.Unknown ? "?" : "○")} {t.Words} · {t.Note}");
            if (p.NextLine.Length > 0) sb.AppendLine($"{p.NextHead}: {p.NextLine} [{string.Join(", ", p.NextWays.ConvertAll(c => c.Word + " " + c.Who))}]");
            foreach (var s in p.Stats) sb.AppendLine($"  {s.Label} {s.Value}: {s.Why}");
            foreach (var c in p.City) sb.AppendLine($"  {c.Head} / {c.Line}");
            sb.AppendLine("  run: " + string.Join(" · ", p.Track.ConvertAll(s => s.Word + " " + s.Time)));
            if (p.Fold != null) { sb.AppendLine($"  fold {p.Fold.Title} {p.Fold.Head}"); foreach (var r in p.Fold.Rows) sb.AppendLine($"    {r.Value} {r.Words} {r.When}"); }
            return sb.ToString();
        }
    }
}
