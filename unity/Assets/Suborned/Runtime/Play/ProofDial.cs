using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Brief 098 step 0, the dial's player probe (Latch; `-dialProbe &lt;dir&gt;`, a player only): the start page's panel in the real player, clicked through its named
    /// buttons with UI Toolkit click events. It checks: a new game with no counts is today's world; a click builds nothing; Play builds the street again with the numbers on
    /// the same seed; every crowd view in the scene is a walker of the game (a street rebuilt with fewer keeps no view of anyone who left); Replay keeps the seed and the
    /// counts; Today then Play gives back the first game's start exactly; the numbers last played are neither read nor written by a probe. A picture of the page at each
    /// step. Writes &lt;dir&gt;/dial-probe.json (and the census lines) and quits; its own boot, so GameBoot's proof sections are untouched.</summary>
    public sealed class ProofDial : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || GameBoot.ArgValue("-dialProbe") is not string d) return;
            var go = new GameObject("dial-probe"); DontDestroyOnLoad(go); go.AddComponent<ProofDial>().dir = Path.GetFullPath(d);
        }

        string dir; readonly List<(string what, bool ok, string detail)> checks = new(); readonly List<string> census = new(), shots = new();
        void Check(string what, bool ok, string detail = "") { checks.Add((what, ok, detail)); Debug.Log($"DialProbe: {(ok ? "ok" : "FAIL")} {what} {detail}"); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 90f) { Check("a host", false, "none after 90 s"); Write(); yield break; } yield return null; }
            yield return CaptureSize.Apply(1920, 1080); // the pictures at the shot rig's size, whatever the display
            yield return new WaitForSecondsRealtime(3f); // the HUD built, the world streamed in
            HandoverHost.SeedPin = null; h.Begin(); yield return null; yield return null; // a rolled seed (a probe pins 0), so "the same seed" means something
            string pg = PlayerPrefs.GetInt(HandoverHost.DialPrefPrefix + "guards", -7) + "/" + PlayerPrefs.GetInt(HandoverHost.DialPrefPrefix + "people", -7);
            var tg = h.DialTodayOf("guards"); var tp = h.DialTodayOf("people");
            if (tg == null || tp == null) { Check("the dial's rules in the bundle", false, "Population.Today gave nothing"); Write(); yield break; }
            var g0 = h.Game; int seed = g0.Seed; ulong start0 = g0.StartHash;
            Check("a new game with no counts is today's world", g0.Start.Counts.Count == 0 && !g0.DialOn, $"seed {seed}, walkers {g0.Walkers.Count}");
            Views(h, "today");

            // 1 · the start page: the panel and its buttons
            yield return Page(h);
            Check("the panel is up on the start page with its buttons", Has("dial") && Has("dial-guards-minus10") && Has("dial-guards-minus") && Has("dial-guards-plus") && Has("dial-guards-plus10") && Has("dial-people-plus10") && Has("dial-today") && Has("dial-play"));
            yield return Shot(h, "dial-1-today");

            // 2 · clicks: three more guards, 36 more people; nothing is built until Play
            for (int i = 0; i < 3; i++) Click("dial-guards-plus"); for (int i = 0; i < 3; i++) Click("dial-people-plus10"); for (int i = 0; i < 6; i++) Click("dial-people-plus");
            yield return null; yield return null;
            Check("the clicks set the numbers", h.DialShown("guards") == tg.Today + 3 && h.DialShown("people") == tp.Today + 36, $"{h.DialShown("guards")} / {h.DialShown("people")}");
            Check("a click builds nothing", ReferenceEquals(h.Game, g0) && h.DialPending, HudToolkit.DialLineNow);
            yield return Shot(h, "dial-2-changed");

            // 3 · Play: the same seed, the numbers, the start sequence
            Click("dial-play"); yield return null; yield return null; yield return null;
            var g1 = h.Game; Census(g1, "play");
            Check("Play builds the street again on the same seed", !ReferenceEquals(g1, g0) && g1.Seed == seed && g1.Start.Counts.TryGetValue("guards", out var cg) && cg == tg.Today + 3 && g1.Start.Counts.TryGetValue("people", out var cp) && cp == tp.Today + 36, $"seed {g1.Seed}, built in {h.DialBuildMs:0} ms");
            Check("Play begins (the page shuts, the start sequence runs)", !h.ShowBriefing && !h.BookOpen);
            Views(h, "more");
            yield return new WaitForSecondsRealtime(6f); h.SetPaused(true); yield return null;
            yield return Shot(h, "dial-3-playing");
            // 3b · the hover on an added guard and an added passer-by on the street: the HUD's own target and chip (the Enforcer selected: the takedown's offer)
            h.Select(OpId.Enforcer, false);
            foreach (var job in new[] { "guard", "visitor" })
            {
                var w = h.Game.Walkers.Find(x => x.Added && x.P.Job == job && Apart(h.Game, x)) ?? h.Game.Walkers.Find(x => x.Added && x.P.Job == job && h.Game.CrowdPos(x).HasValue); // one standing apart: in a knot the HUD rightly prefers security (Pulse 067 W2)
                var v = w == null ? null : h.Views.Find("crowd/" + h.Game.Walkers.IndexOf(w));
                if (w == null || !v || !v.gameObject.activeInHierarchy) { Check($"hover: an added {job} on the street", false, $"none drawn at {Report.Clock(h.Game.Tick)}"); continue; }
                h.LookAt(v.position); yield return null; HandoverHost.PointerWorld = v.position + Vector3.up;
                for (int i = 0; i < 8; i++) yield return null; yield return new WaitForSecondsRealtime(0.3f);
                var t = h.Hover; var tw = t == null ? null : h.Game.Walkers.Find(x => x.P.Id == t.Id);
                Check($"hover: an added {job} reads as anyone", t != null && t.Kind == "crowd" && tw != null && tw.Added && tw.P.Job == job && t.Security == (job == "guard"), $"{w.P.Id} pointed at; hover {(t == null ? "nothing" : $"{t.Kind} {t.Id} '{t.Label}'")}");
                yield return Shot(h, $"dial-3-hover-{job}");
                HandoverHost.PointerWorld = null; yield return null;
            }
            // 3c · an added guard taken down lies where he fell in his figure, and his standing view goes: the Enforcer runs to him (re-aimed as he walks his beat) and
            // knocks him out once within reach, as a route gives it (the engine's takedown wants the Enforcer within 150 cm)
            if (h.Game.Walkers.Find(x => x.Added && x.P.Job == "guard" && h.Game.CrowdPos(x).HasValue) is { } dw)
            {
                var g = h.Game; var en = g.Ops[(int)OpId.Enforcer]; bool took = false; string why = "";
                for (int k = 0; k < 240 && !dw.P.Down && !g.Over; k++)
                {
                    if (g.CrowdPos(dw) is Pt at && !took)
                    {
                        if (G.D2(en.Pos, at) <= 140L * 140L) { took = h.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "takedown", Target = dw.P.Id, How = "knockout" }); if (!took) why = h.Status; }
                        else if (k % 3 == 0) h.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "move", Point = at, Run = true });
                    }
                    h.RunTo(g.Tick + 10); if (k % 20 == 0) yield return null;
                }
                if (dw.P.Down) h.RunTo(g.Tick + 120); // the knockout's struggle ends and he lies where he fell
                yield return null; yield return null;
                var body = h.Views.Find("body/" + dw.P.Id); var stand = h.Views.Find("crowd/" + g.Walkers.IndexOf(dw));
                Check("an added guard taken down lies where he fell", dw.P.Down && body && body.gameObject.activeInHierarchy && !(stand && stand.gameObject.activeInHierarchy), $"{dw.P.Id}: takedown {(took ? "taken" : "not taken " + why)}, down {dw.P.Down} at {Report.Clock(g.Tick)}, body view {(body ? "drawn" : "none")}");
                if (body) { h.LookAt(body.position); yield return null; yield return Shot(h, "dial-3-down-guard"); }
            }

            // 4 · Replay keeps the seed and the counts
            h.BeginSameSeed(); yield return null; yield return null; var g2 = h.Game;
            Check("Replay keeps the seed and the counts", g2.Seed == seed && g2.StartHash == g1.StartHash, $"{Hash.Hex(g2.StartHash)}");
            Views(h, "replay");

            // 5 · fewer: 8 guards, 60 people, by clicks; the street rebuilt keeps no view of anyone who left
            yield return Page(h);
            int dg = h.DialShown("guards") - 8, dp = h.DialShown("people") - 60; // counted clicks (a click lands on the panel's next event pass, not at once)
            for (int i = 0; i < dg / 10; i++) Click("dial-guards-minus10"); for (int i = 0; i < dg % 10; i++) Click("dial-guards-minus");
            for (int i = 0; i < dp / 10; i++) Click("dial-people-minus10"); for (int i = 0; i < dp % 10; i++) Click("dial-people-minus");
            yield return null; yield return null;
            Check("the clicks take people away", h.DialShown("guards") == 8 && h.DialShown("people") == 60, $"{h.DialShown("guards")} / {h.DialShown("people")}");
            yield return Shot(h, "dial-4-fewer");
            Click("dial-play"); yield return null; yield return null; yield return null;
            var g3 = h.Game; Census(g3, "fewer");
            int gone = 0; foreach (var c in g3.Census) gone += c.Removed.Count;
            Check("fewer plays the counts, the cast kept", g3.Census.Count == 2 && g3.Census[0].Count == 8 && g3.Census[1].Count == 60 && gone == tg.Today - 8 + tp.Today - 60, $"{gone} left");
            Views(h, "fewer");

            // 6 · Today, then Play: the first game's start exactly (no counts on its seed)
            h.BeginSameSeed(); yield return null; yield return Page(h);
            Click("dial-today"); yield return null; Click("dial-play"); yield return null; yield return null; yield return null;
            var g4 = h.Game; Census(g4, "today");
            Check("Today then Play is today's world byte for byte", g4.Start.Counts.Count == 0 && !g4.DialOn && g4.Seed == seed && g4.StartHash == start0, Hash.Hex(g4.StartHash));
            Views(h, "today again");

            string pg2 = PlayerPrefs.GetInt(HandoverHost.DialPrefPrefix + "guards", -7) + "/" + PlayerPrefs.GetInt(HandoverHost.DialPrefPrefix + "people", -7);
            Check("a probe neither reads nor writes the numbers last played", pg == pg2, $"{pg} before, {pg2} after");
            Write();
        }

        IEnumerator Page(HandoverHost h)
        {
            if (!h.ShowBriefing) h.ShowBriefing = true;
            float t0 = Time.realtimeSinceStartup; while (HudToolkit.DialElement("dial-play") == null && Time.realtimeSinceStartup - t0 < 10f) yield return null;
            yield return new WaitForSecondsRealtime(0.6f); // the page's slide and fade
        }

        /// <summary>On the street with nobody else of the crowd within 3 m and no door within 4.5 m (so the pointer has one person under it).</summary>
        static bool Apart(Game g, Walker w)
        {
            if (!(g.CrowdPos(w) is Pt p)) return false;
            foreach (var o in g.Walkers) if (o != w && g.CrowdPos(o) is Pt q && G.D2(p, q) < 300L * 300L) return false;
            foreach (var e in g.S.Entrances) if (G.D2(p, e.at) < 450L * 450L) return false; // clear of a door's marker too: under brief 067 F8 the overlap goes to the selection's calm way (Tab steps on)
            return true;
        }
        static bool Has(string name) => HudToolkit.DialElement(name) != null;
        /// <summary>A click on the panel's button by name, as UI Toolkit delivers one (its ClickEvent callback); false when it is not up.</summary>
        static bool Click(string name)
        {
            var e = HudToolkit.DialElement(name); if (e == null) return false;
            using (var ev = ClickEvent.GetPooled()) { ev.target = e; e.SendEvent(ev); }
            return true;
        }

        /// <summary>The crowd views against the game's walkers: the host's table and the scene's own objects (inactive ones too), a frame after the build.</summary>
        void Views(HandoverHost h, string when)
        {
            int table = 0; foreach (var (k, _) in h.Views.All()) if (k.StartsWith("crowd/", StringComparison.Ordinal)) table++;
            int scene = 0; foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include)) if (t && t.name.StartsWith("crowd/", StringComparison.Ordinal)) scene++;
            int walkers = h.Game.Walkers.Count, added = 0; foreach (var w in h.Game.Walkers) if (w.Added) added++;
            Check($"{when}: a crowd view a walker, none left over", table == walkers && scene == walkers, $"walkers {walkers} (added {added}), views {table}, in the scene {scene}");
        }

        void Census(Game g, string when) { if (!g.DialOn) census.Add($"{when}: unset (today's world)"); else foreach (var c in g.Census) census.Add($"{when}: {HandoverHost.CensusLine(c)}"); }

        IEnumerator Shot(HandoverHost h, string name)
        {
            while (HudToolkit.ShotBusy) yield return null;
            var r = HudToolkit.Shoot(Path.Combine(dir, name), "rest"); yield return null; while (HudToolkit.ShotBusy) yield return null;
            shots.Add($"{name}.png ({r})");
        }

        void Write()
        {
            bool ok = checks.Count > 0 && checks.TrueForAll(c => c.ok);
            var sb = new StringBuilder("{\n \"schema\": \"suborned.dial-probe/1\", \"ok\": ").Append(ok ? "true" : "false").Append(",\n \"checks\": [\n");
            for (int i = 0; i < checks.Count; i++) sb.Append("  {\"what\": ").Append(Q(checks[i].what)).Append(", \"ok\": ").Append(checks[i].ok ? "true" : "false").Append(", \"detail\": ").Append(Q(checks[i].detail)).Append(i + 1 < checks.Count ? "},\n" : "}\n");
            sb.Append(" ],\n \"census\": [").Append(string.Join(", ", census.ConvertAll(Q))).Append("],\n \"shots\": [").Append(string.Join(", ", shots.ConvertAll(Q))).Append("]\n}\n");
            File.WriteAllText(Path.Combine(dir, "dial-probe.json"), sb.ToString());
            Debug.Log($"DialProbe: {(ok ? "ok" : "FAILED")}, {checks.FindAll(c => c.ok).Count}/{checks.Count} checks → {dir}");
            GameBoot.Leave(ok ? 0 : 1);
        }
        static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ") + "\"";
    }
}
