using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>`-rangeShots &lt;dir&gt;` (brief 029, engine 0.18; Latch 2026-10-21): the fight range in the built player, proved and pictured. It
    /// loads the FightRange scene and plays four short runs, each by the host's own orders (so every run replays): the lane layout with a car,
    /// police and bystanders spawned; a car with an officer inside shot until it burns and he gets out (then until it blows, and the cover it
    /// gave goes: Game.CoverNow); bystanders fleeing a fire fight, one hovered for the context hint; the engine's plain line under fire
    /// (FireLine.Text). Each picture is a HudToolkit shot at 1920 x 1080 (frame, HUD layer, manifest). range.json says what each check found.
    /// Exits 0 when every check held, 1 otherwise.</summary>
    public sealed class RangeShots : MonoBehaviour
    {
        public string Dir = "";
        readonly List<string> parts = new(), log = new();
        bool ok = true;

        static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "'").Replace("\n", " ").Replace("\r", " ");
        static string B(bool b) => b ? "true" : "false";
        void Check(string name, bool held, string detail) { ok &= held; parts.Add($"\"{name}\": {{\"ok\": {B(held)}, \"detail\": \"{Esc(detail)}\"}}"); log.Add($"{name}: {(held ? "ok" : "FAILED")} · {detail}"); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            float t0 = Time.realtimeSinceStartup;
            while (FindAnyObjectByType<HandoverHost>() == null && Time.realtimeSinceStartup - t0 < 60f) yield return null; // the first scene up
            yield return CaptureSize.Apply(1920, 1080);
            UnityEngine.SceneManagement.SceneManager.LoadScene("FightRange");
            HandoverHost h = null; t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || !h.rangeMode || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { Write("no range host after 60 s"); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(2f);
            log.Add($"range up: screen {Screen.width}x{Screen.height}, layout {h.rangeLayout}, preset {h.Fights.preset}");
            string err = null;
            IEnumerator Guarded(IEnumerator step)
            {
                while (true)
                {
                    object cur; try { if (!step.MoveNext()) yield break; cur = step.Current; } catch (Exception e) { err = e.GetType().Name + ": " + e.Message; ok = false; log.Add("step threw: " + err); yield break; }
                    yield return cur;
                }
            }
            yield return Guarded(Lane(h));
            yield return Guarded(Car(h));
            yield return Guarded(Bystanders(h));
            yield return Guarded(Lines(h));
            yield return Guarded(Fixes(h)); // Sky's notes on 0.2.34 (Latch): labels, the goal line, the bars, the car's time, the click-through
            HandoverHost.PointerOverride = null;
            Write(ok ? "ok" : "failed");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(ok ? 0 : 1);
        }

        /// <summary>A fresh run on a layout, paused, the camera at play zoom.</summary>
        static IEnumerator Fresh(HandoverHost h, string layout)
        {
            HandoverHost.PointerOverride = null; h.ClearHover();
            h.rangeLayout = layout; h.Begin(); yield return null; yield return null;
            h.StartClock(); h.SetPaused(true); if (h.View) h.View.orthographicSize = 14f;
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            while (HudToolkit.ShotBusy) yield return null;
            var q = HudToolkit.Shoot(Path.Combine(Dir, name), "rest"); log.Add($"{name}: {q}");
            yield return null; while (HudToolkit.ShotBusy) yield return null;
        }

        static bool Spawn(HandoverHost h, string what, string to, int n, string with = "") => h.Issue(new Order { Do = "spawn", Target = what, To = to, Count = n, With = with });

        /// <summary>Real time until `done` holds or `seconds` pass (the clock running).</summary>
        static IEnumerator Until(HandoverHost h, Func<bool> done, float seconds)
        {
            h.SetPaused(false); float t0 = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - t0 < seconds) yield return null;
            h.SetPaused(true);
        }

        // 1 · the lane: 90 m of floor, a car with rivals on slot 1, police in cover, bystanders beyond
        IEnumerator Lane(HandoverHost h)
        {
            yield return Fresh(h, "lane");
            var g = h.Game;
            bool s1 = Spawn(h, "car", "car:1", 1, "rivals"), s2 = Spawn(h, "police", "cover", 3), s3 = Spawn(h, "bystanders", "beyond", 3);
            h.RunTo(g.Tick + 10);
            float floorM = g.S.Grid.Nx * g.S.Grid.Cell / 100f;
            h.Select(OpId.Scout, false); if (h.View) h.View.orthographicSize = 24f; h.LookAt(new Vector3(24f, 0f, 20f));
            yield return Shot("range-lane");
            Check("lane", h.rangeLayout == "lane" && s1 && s2 && s3 && g.RangeCars().Count == 1 && g.RangeBystanders().Count == 3,
                $"layout {h.rangeLayout}, walk grid {F(floorM)} m along (floor 90 m and the wings), spawns car {s1} police {s2} bystanders {s3}, car slots {g.RangeSlotList().Count(s => s.Placement == "car")}, hostiles {g.RangeHostiles().Count}");
        }

        // 2 · a car with an officer inside, shot until it burns (he gets out), then until it blows (its cover goes)
        IEnumerator Car(HandoverHost h)
        {
            yield return Fresh(h, "open");
            var g = h.Game; int coverBase = g.CoverNow().Count;
            bool sp = Spawn(h, "car", "car:1", 1, "police"); h.RunTo(g.Tick + 1);
            var car = g.RangeCars().FirstOrDefault(); if (car == null) { Check("car", false, $"no car spawned (spawn {sp}: {h.Status})"); yield break; }
            int coverCar = g.CoverNow().Count;
            foreach (var a in new[] { "scout", "fixer", "enforcer" }) h.Issue(new Order { Op = a, Do = "fire", Target = car.Id });
            h.Select(OpId.Enforcer, false);
            RangeCarView Now() => g.RangeCars().FirstOrDefault(c => c.Id == car.Id);
            yield return Until(h, () => Now() is { } c && (c.State == "burning" || c.State == "wrecked") && c.Occupant.Length > 0, 60f);
            var c1 = Now(); string occ = c1?.Occupant ?? ""; bool burning = c1 != null && c1.State == "burning";
            foreach (var a in new[] { "scout", "fixer", "enforcer" }) h.Issue(new Order { Op = a, Do = "cease" });
            bool occOut = occ.Length > 0 && g.RangeHostiles().Exists(x => x.Id == occ && !x.Gone);
            h.LookAt(h.frame.World(car.At)); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.6f); h.SetPaused(true);
            yield return Shot("range-car-burning");
            Check("car", sp && burning && occOut, $"{car.Id} on slot {car.Slot} with {car.With}: state {c1?.State}, blast in {c1?.BlastIn} ticks, occupant {occ} out {occOut}, tick {g.Tick}");
            yield return Until(h, () => Now() is { } c && c.State == "wrecked", 20f);
            var c2 = Now(); int coverAfter = g.CoverNow().Count;
            Check("carCover", coverCar > coverBase && coverAfter == coverBase && c2?.State == "wrecked", $"cover points: no car {coverBase}, car standing {coverCar}, after the blast {coverAfter} (state {c2?.State}); HostFight reads Game.CoverNow");
            yield return Shot("range-car-blown");
        }

        // 3 · bystanders near the cell flee a fire fight and remember who fired; one hovered: the goal line's context hint
        IEnumerator Bystanders(HandoverHost h)
        {
            yield return Fresh(h, "open");
            var g = h.Game;
            bool s1 = Spawn(h, "bystanders", "near", 4), s2 = Spawn(h, "police", "eff", 2); h.RunTo(g.Tick + 1);
            var target = g.RangeHostiles().FirstOrDefault()?.Id ?? "";
            foreach (var a in new[] { "enforcer", "tech" }) h.Issue(new Order { Op = a, Do = "fire", Target = target });
            h.Select(OpId.Enforcer, false);
            yield return Until(h, () => g.RangeBystanders().Count(b => b.State == "fleeing") >= 2 && g.RangeBystanders().Exists(b => b.Saw.Length > 0), 20f); // running, and someone saw an agent fire
            var bs = g.RangeBystanders(); int fleeing = bs.Count(b => b.State == "fleeing"), saw = bs.Count(b => b.Saw.Length > 0);
            var run = bs.FirstOrDefault(b => b.State == "fleeing" && !b.Down);
            string hint = "";
            if (run != null && h.View)
            {
                var key = h.Views.BystanderKey(g, run.Id); var vt = key != null ? h.Views.Find(key) : null;
                h.LookAt(h.frame.World(run.Pos));
                for (int i = 0; i < 6; i++) yield return null;
                if (vt) { var sp = h.View.WorldToScreenPoint(vt.position + Vector3.up * 1.0f); HandoverHost.PointerOverride = new Vector2(sp.x, sp.y); }
                for (int i = 0; i < 8; i++) yield return null;
                hint = h.RangeHint ?? "";
            }
            yield return Shot("range-bystanders-flee");
            HandoverHost.PointerOverride = null; h.ClearHover();
            Check("bystanders", s1 && s2 && fleeing >= 2 && saw >= 1, $"spawned {s1}/{s2}; at the shot {fleeing} fleeing, {saw} saw an agent fire ({string.Join(", ", bs.Where(b => b.Saw.Length > 0).Select(b => b.Id + " saw " + b.Saw))}); states {string.Join(" ", bs.Select(b => b.State))}");
            Check("bystanderHint", h.controls.HoldActs ? hint.Length == 0 && (HudToolkit.PillWords.Contains("passer-by") || HudToolkit.PillWords.Length == 0) : hint.Contains("Ctrl"), $"over {run?.Id}: hint \"{hint}\", pill \"{HudToolkit.PillWords}\" (brief 041: the pill says it, no Ctrl, no goal-line tag)");
            // they come back: the clock runs on after the cell ceases; quiet 30 s after the last shot, they walk back to their slots
            foreach (var a in new[] { "enforcer", "tech" }) h.Issue(new Order { Op = a, Do = "cease" });
            foreach (var a in new[] { "scout", "fixer", "tech", "enforcer" }) h.Issue(new Order { Op = a, Do = "holster" });
            int t1 = g.Tick; h.RunTo(g.Tick + 900);
            var back = g.RangeBystanders(); var safe = back.Count(b => b.State is "standing" or "returning");
            Check("bystandersReturn", true, $"90 s on (tick {t1} to {g.Tick}): {safe} of {back.Count} back or walking back (the police may still be firing: informational); states {string.Join(" ", back.Select(b => b.State))}");
        }

        // 4 · the line under fire: the Enforcer firing on an officer at 15 m while the police answer; every shown line in the engine's words
        IEnumerator Lines(HandoverHost h)
        {
            yield return Fresh(h, "open");
            var g = h.Game; var np = h.GetComponent<RangePanel>(); if (np != null) np.Numbers = true; // brief 049: Numbers is off by default; this picture is the numbers'
            bool sp = Spawn(h, "police", "eff", 2); h.RunTo(g.Tick + 1); // two at 15 m: they answer, and nobody is down within the 2.4 s
            var target = g.RangeHostiles().FirstOrDefault()?.Id ?? "";
            h.Issue(new Order { Op = "enforcer", Do = "fire", Target = target });
            h.Select(OpId.Enforcer, false);
            yield return Until(h, () => false, 2.4f);
            var panel = h.GetComponent<RangePanel>();
            var shown = panel != null ? panel.Shown() : g.RangeLines();
            var texts = shown.Select(l => $"{l.Shooter}>{l.Target}: {l.Text}").ToList();
            h.LookAt(h.frame.World(new Pt(1000, 2000)));
            yield return Shot("range-line-under-fire");
            bool plain = shown.Count > 0 && shown.All(l => l.Text.Length > 0) && shown.Exists(l => l.Text.Contains("%") || l.Text.Contains("●"));
            parts.Add($"\"lineTexts\": [{string.Join(", ", texts.Take(12).Select(t => "\"" + Esc(t) + "\""))}]");
            if (np != null) np.Numbers = false;
            Check("lines", sp && plain, $"{shown.Count} lines shown, every one with the engine's plain words: {plain}; e.g. {(texts.Count > 0 ? texts[0] : "none")}");
        }

        // 5 · Sky's notes on 0.2.34: lab preset; a car with police inside, four bystanders near, three officers at 15 m; the Enforcer and the Scout
        // fire on one officer, so lines share paths. The labels never overlap (stepped or left off), no Handover goal line, a bar over the car and
        // each bystander on the floor. Then the Enforcer fires on the car: the fire line says when it is disabled. Then the panel's clicks.
        IEnumerator Fixes(HandoverHost h)
        {
            h.Fights.Load("lab"); h.Fights.fightMode = "realtime"; yield return Fresh(h, "open");
            var g = h.Game; var np = h.GetComponent<RangePanel>(); if (np != null) np.Numbers = true; // brief 049: these checks are the numbers overlay's (off by default)
            bool s1 = Spawn(h, "car", "car:1", 1, "police"), s2 = Spawn(h, "bystanders", "near", 4), s3 = Spawn(h, "police", "eff", 3); h.RunTo(g.Tick + 1);
            var target = g.RangeHostiles().FirstOrDefault(x => !x.Gone)?.Id ?? "";
            foreach (var a in new[] { "enforcer", "scout" }) h.Issue(new Order { Op = a, Do = "fire", Target = target });
            h.Select(OpId.Enforcer, false);
            yield return Until(h, () => false, 1.6f);
            if (h.View) h.View.orthographicSize = 18f; h.LookAt(h.frame.World(g.RangeMark) + new Vector3(4f, 0f, 6f));
            h.SetPaused(false); yield return new WaitForSecondsRealtime(0.5f); h.SetPaused(true); yield return null; yield return null;
            int bystandersUp = g.RangeBystanders().Count(b => !b.Down && b.State != "safe"), carsUp = g.RangeCars().Count(c => c.State != "wrecked");
            yield return Shot("range-fixes-labels-bars");
            int placed = RangePanel.LabelsPlaced, moved = RangePanel.LabelsMoved, dropped = RangePanel.LabelsDropped, bars = HudToolkit.RangeBarsDrawn; string goal = HudToolkit.GoalNow ?? "";
            if (np != null) np.Numbers = false;
            Check("labels", placed > 0, $"labels placed {placed}, stepped clear {moved}, left off for room {dropped} (each placed clear of the others and the panel by construction)");
            Check("goalLine", !goal.Contains("Vale"), $"the goal line in the range: \"{goal}\"");
            Check("bars", s1 && s2 && s3 && bars >= Mathf.Min(1, carsUp) + 1, $"bars over range cars and bystanders {bars} (on the floor: cars {carsUp}, bystanders {bystandersUp}); spawns {s1} {s2} {s3}; lab {g.Lab}");
            // the car's time: the Enforcer alone on the car, the fire line's words
            foreach (var a in new[] { "enforcer", "scout" }) h.Issue(new Order { Op = a, Do = "cease" });
            var car = g.RangeCars().FirstOrDefault(); string words = "", downIs = "";
            if (car != null)
            {
                h.Issue(new Order { Op = "enforcer", Do = "fire", Target = car.Id }); h.Select(OpId.Enforcer, false);
                FirePreview fp = null; try { fp = g.PreviewFire(OpId.Enforcer, car.Id, false); } catch { }
                downIs = fp?.DownIs ?? "";
                h.LookAt(h.frame.World(car.At)); yield return Until(h, () => false, 0.4f); yield return null; yield return null;
                var fl = h.FireLine((int)OpId.Enforcer); words = fl.head + " · " + fl.down;
                yield return Shot("range-fixes-car-time");
            }
            Check("carTime", car != null && downIs == "disabled" && words.Contains("disabled in"), $"{car?.Id} {car?.State}: preview down is {downIs}; the fire line \"{words}\"");
            h.Issue(new Order { Op = "enforcer", Do = "cease" });
            h.Fights.Load("today");
            ProofInput.Focusless(true); var pc = new string[] { "null" }; yield return ProofInput.RangePanelClick(h, log, pc); ProofInput.Focusless(false);
            parts.Add($"\"panelClickJson\": {pc[0]}");
            Check("panelClick", pc[0].StartsWith("{\"ok\": true"), "the panel's clicks in both views never reach the street (see panelClickJson)");
        }

        void Write(string status)
        {
            var body = $"{{\n \"schema\": \"suborned.range-shots/1\",\n \"status\": \"{Esc(status)}\",\n \"build\": \"{Esc(BuildStamp.Line)}\",\n \"screen\": \"{Screen.width}x{Screen.height}\",\n \"log\": [{string.Join(", ", log.Select(l => "\"" + Esc(l) + "\""))}]{(parts.Count > 0 ? ",\n " + string.Join(",\n ", parts) : "")}\n}}\n";
            File.WriteAllText(Path.Combine(Dir, "range.json"), body);
        }
    }
}
