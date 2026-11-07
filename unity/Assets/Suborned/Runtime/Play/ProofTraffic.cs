using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Brief 098 step 1, passing traffic's player probe (Latch; `-trafficProbe &lt;dir&gt;`, a player only), after ProofDial: the start page's Cars row clicked
    /// through its named buttons; Play builds the street with cars on the same seed; every passing car the engine has is drawn by one view and no view is left
    /// over (the host's table and the scene's own objects), in its kit model and, a sedan, in a paint of the palette; a hover on a passing car is a car target
    /// and its pill says what it is doing (HostTraffic.TrafficPill); a car shot by the Enforcer stands where it stopped, its driver out and drawn as one of the
    /// crowd (GrowCrowd), its smoke drawn in the world, its hover "wrecked"; the engines are heard while cars pass near the focus (sound on); cars back to none
    /// draws none. A picture at each step. Writes &lt;dir&gt;/traffic-probe.json and quits; its own boot, so GameBoot's proof sections are untouched.</summary>
    public sealed class ProofTraffic : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || GameBoot.ArgValue("-trafficProbe") is not string d) return;
            var go = new GameObject("traffic-probe"); DontDestroyOnLoad(go); go.AddComponent<ProofTraffic>().dir = Path.GetFullPath(d);
        }

        string dir; readonly List<(string what, bool ok, string detail)> checks = new(); readonly List<string> shots = new(), notes = new();
        void Check(string what, bool ok, string detail = "") { checks.Add((what, ok, detail)); Debug.Log($"TrafficProbe: {(ok ? "ok" : "FAIL")} {what} {detail}"); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 90f) { Check("a host", false, "none after 90 s"); Write(); yield break; } yield return null; }
            yield return CaptureSize.Apply(1920, 1080);
            yield return new WaitForSecondsRealtime(3f);
            HandoverHost.SeedPin = null; h.Begin(); yield return null; yield return null;
            var g0 = h.Game; int seed = g0.Seed;
            Check("a new game with no counts has no traffic and draws no car", !g0.TrafficOn && h.Views.TrafficViewsOn == 0 && SceneCars() == 0, $"seed {seed}");
            if (h.DialTodayOf(Population.Cars) is not { } tc) { Check("the dial's cars in the bundle", false, "Population.Today has no cars"); Write(); yield break; }

            // 1 · the Cars row: +10, −, − is 8; nothing is built until Play
            yield return Page(h);
            Check("the Cars row is on the panel with its buttons", Has("dial-cars-minus10") && Has("dial-cars-minus") && Has("dial-cars-plus") && Has("dial-cars-plus10"), $"today {tc.Today}, {tc.Floor}–{tc.Max}");
            Click("dial-cars-plus10"); yield return null; Click("dial-cars-minus"); yield return null; Click("dial-cars-minus"); yield return null; yield return null;
            Check("the clicks set 8 cars and build nothing", h.DialShown(Population.Cars) == 8 && h.DialPending && ReferenceEquals(h.Game, g0), HudToolkit.DialLineNow);
            yield return Shot(h, "traffic-1-panel");
            Click("dial-play"); yield return null; yield return null; yield return null;
            var g = h.Game;
            Check("Play builds traffic on the same seed", !ReferenceEquals(g, g0) && g.Seed == seed && g.TrafficOn && g.Start.Counts.Count == 1 && g.Start.Counts.TryGetValue(Population.Cars, out var cn) && cn == 8, $"built in {h.DialBuildMs:0} ms; {g.TrafficNow().Count} cars at 0:00");

            // 2 · 30 s of the street: every car a view, none left over; the models and paints
            for (int i = 0; i < 30 && !g.Over; i++) { h.RunTo(g.Tick + 10); yield return null; }
            yield return null;
            var now = g.TrafficNow(); int drawn = 0; var kits = new SortedDictionary<string, int>(); var paints = new SortedSet<string>(); int box = 0, unpainted = 0, lookalike = 0;
            foreach (var v in now)
            {
                var k = h.Views.TrafficKitOf(v.Id); kits[k] = kits.TryGetValue(k, out var n) ? n + 1 : 1; if (k.EndsWith("/box")) box++;
                if (k.StartsWith("sedan")) { var p = h.Views.TrafficPaintOf(v.Id); if (p.Length == 0) unpainted++; else { paints.Add(k[..7] + " " + p); int pi = Array.IndexOf(h.trafficPaintKeys, p); if (h.trafficPaintAvoid != null && pi >= 0 && pi < h.trafficPaintAvoid.Length && (h.trafficPaintAvoid[pi] & (k.StartsWith("sedan-g") ? 2 : 1)) != 0) lookalike++; } }
                if (h.Views.TrafficDrawnAt(v.Id, out _)) drawn++;
            }
            Check("every passing car is drawn by one view, none left over", now.Count > 0 && drawn == now.Count && h.Views.TrafficViewsOn == now.Count && SceneCars() == now.Count, $"cars {now.Count}, drawn {drawn}, views {h.Views.TrafficViewsOn}, active in the scene {SceneCars()}, built {h.Views.TrafficViewsMade}");
            Check("each car in its kit model, a sedan in a paint of the palette, never one kept off its body (it would pass for a mission car)", box == 0 && unpainted == 0 && lookalike == 0, $"{string.Join(", ", Pairs(kits))}; paints {string.Join(", ", paints)}; look-alikes {lookalike}");

            // 3 · the hover on a passing car (the Scout selected: no way on a car, so the pill is the car's state)
            h.Select(OpId.Scout, false);
            TrafficView hv = null; foreach (var v in now) if (v.InZone && h.Views.TrafficDrawnAt(v.Id, out _) && (hv == null || v.Speed < hv.Speed)) hv = v; // a slow one: the words say what holds it
            if (hv != null && h.Views.TrafficDrawnAt(hv.Id, out var ha))
            {
                h.LookAt(ha); h.SetPaused(true); yield return null; HandoverHost.PointerWorld = ha + Vector3.up * 0.8f;
                for (int i = 0; i < 8; i++) yield return null; yield return new WaitForSecondsRealtime(0.3f);
                var t = h.Hover; string want = t != null ? h.TrafficPill(t) : "";
                Check("hover: a passing car is a car target and the pill says what it is doing", t != null && HandoverHost.IsTrafficCar(t) && "car:" + t.Key == hv.Id && HudToolkit.PillState == "info" && HudToolkit.PillWords == want && want.Length > 0, $"{hv.Id} ({hv.Model}, {hv.Speed} cm/s, why '{g.TrafficWhy(hv.Id)}'): pill '{HudToolkit.PillWords}' ({HudToolkit.PillState})");
                yield return Shot(h, "traffic-2-hover"); HandoverHost.PointerWorld = null; h.SetPaused(false);
            }
            else Check("hover: a passing car on the zone's roads", false, "none drawn");

            // 4 · the Enforcer shoots the nearest passing car within 18 m: it stands where it stopped, its driver out and drawn, its smoke in the world
            h.Select(OpId.Enforcer, false); var en = g.Ops[(int)OpId.Enforcer]; string shot = ""; int firedAt = -1;
            h.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "draw" });
            for (int k = 0; k < 900 && !g.Over && shot.Length == 0; k++)
            {
                TrafficView best = null; long bd = long.MaxValue; foreach (var v in g.TrafficNow()) { if (!v.InZone || v.State != "moving") continue; long d = G.D2(en.Pos, v.At); if (d < bd) { bd = d; best = v; } }
                if (best != null && bd <= 1800L * 1800L) { if (h.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "fire", Target = best.Id })) { shot = best.Id; firedAt = g.Tick; } }
                else if (best != null && k % 20 == 0 && g.Walk.NearestFree(best.At) is Pt to) h.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "move", Point = to, Run = true });
                h.RunTo(g.Tick + 1); if (k % 10 == 0) yield return null;
            }
            if (shot.Length > 0)
            {
                for (int k = 0; k < 300 && !g.Over && !(g.CarOf(shot) is { DisabledAt: >= 0 }); k++) { h.RunTo(g.Tick + 1); if (k % 10 == 0) yield return null; }
                h.RunTo(g.Tick + 30); yield return null; yield return null;
                var sv = g.TrafficCarById(shot) != null ? h.Views.TrafficOf(shot) : null; var ct = g.CarOf(shot);
                int wi = sv == null ? -1 : g.Walkers.FindIndex(w => w.P.Id == sv.Driver); var dv = wi < 0 ? null : h.Views.Find("crowd/" + wi);
                Check("a shot car stands, its driver out and drawn as one of the crowd", sv != null && ct != null && ct.DisabledAt >= 0 && !sv.DriverIn && sv.Speed == 0 && wi >= 0 && dv && dv.gameObject.activeInHierarchy && h.Views.CrowdGrown >= 1, $"{shot}: fired at {Report.Clock(firedAt)}, disabled {(ct != null && ct.DisabledAt >= 0 ? Report.Clock(ct.DisabledAt) : "no")}, driver {sv?.Driver} walker {wi}, crowd views added {h.Views.CrowdGrown}");
                Check("its smoke is drawn in the world and its hover says so", h.CarFxDrawn(shot) && h.TrafficWords(shot).StartsWith("wrecked"), $"fx {h.CarFxDrawn(shot)}, words '{h.TrafficWords(shot)}'");
                if (h.Views.TrafficDrawnAt(shot, out var sa)) { h.LookAt(sa); h.SetPaused(true); yield return null; yield return Shot(h, "traffic-3-shot"); h.SetPaused(false); }
                foreach (var v in g.TrafficNow()) if (v.Mode is "duck" or "flee" or "brake" or "abandoned") notes.Add($"{v.Id} {v.Model} {v.Mode} {v.State}");
            }
            else Check("the Enforcer fired on a passing car", false, $"none within 18 m in 90 s (Enforcer at {en.Pos})");

            // 5 · the engines are heard while cars pass near the focus
            int heard = 0; if (!HandoverHost.SoundOff) for (int k = 0; k < 120 && !g.Over; k++) { h.RunTo(g.Tick + 1); yield return null; heard = Mathf.Max(heard, h.TrafficVoicesPlaying); }
            Check("passing cars are heard (at most the voice cap)", HandoverHost.SoundOff || heard >= 1 && heard <= HandoverHost.TrafficVoices, HandoverHost.SoundOff ? "sound off (-mute): not asked" : $"most voices at once {heard} (cap {HandoverHost.TrafficVoices})");

            // 6 · cars back to none: no traffic, no car drawn
            h.BeginSameSeed(); yield return null; yield return Page(h);
            for (int i = 0; i < 8; i++) { Click("dial-cars-minus"); yield return null; }
            Click("dial-play"); yield return null; yield return null; yield return null; for (int i = 0; i < 10; i++) { h.RunTo(h.Game.Tick + 10); yield return null; }
            Check("cars back to none: today's world, no car drawn", h.Game.Start.Counts.Count == 0 && !h.Game.TrafficOn && h.Views.TrafficViewsOn == 0 && SceneCars() == 0, $"start {Hash.Hex(h.Game.StartHash)}");
            Write();
        }

        static IEnumerable<string> Pairs(SortedDictionary<string, int> d) { foreach (var kv in d) yield return $"{kv.Key} {kv.Value}"; }
        /// <summary>The passing cars' views active in the scene (by their names, car/&lt;slot&gt;-&lt;trip&gt;: the mission cars are car/official, car/getaway, car/rivals).</summary>
        static int SceneCars() { int n = 0; foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude)) if (t && t.name.StartsWith("car/", StringComparison.Ordinal) && t.name is not ("car/official" or "car/getaway" or "car/rivals")) n++; return n; }

        IEnumerator Page(HandoverHost h)
        {
            if (!h.ShowBriefing) h.ShowBriefing = true;
            float t0 = Time.realtimeSinceStartup; while (HudToolkit.DialElement("dial-play") == null && Time.realtimeSinceStartup - t0 < 10f) yield return null;
            yield return new WaitForSecondsRealtime(0.6f);
        }
        static bool Has(string name) => HudToolkit.DialElement(name) != null;
        static bool Click(string name) { var e = HudToolkit.DialElement(name); if (e == null) return false; using (var ev = ClickEvent.GetPooled()) { ev.target = e; e.SendEvent(ev); } return true; }

        IEnumerator Shot(HandoverHost h, string name)
        {
            while (HudToolkit.ShotBusy) yield return null;
            var r = HudToolkit.Shoot(Path.Combine(dir, name), "rest"); yield return null; while (HudToolkit.ShotBusy) yield return null;
            shots.Add($"{name}.png ({r})");
        }

        void Write()
        {
            bool ok = checks.Count > 0 && checks.TrueForAll(c => c.ok);
            var sb = new StringBuilder("{\n \"schema\": \"suborned.traffic-probe/1\", \"ok\": ").Append(ok ? "true" : "false").Append(",\n \"checks\": [\n");
            for (int i = 0; i < checks.Count; i++) sb.Append("  {\"what\": ").Append(Q(checks[i].what)).Append(", \"ok\": ").Append(checks[i].ok ? "true" : "false").Append(", \"detail\": ").Append(Q(checks[i].detail)).Append(i + 1 < checks.Count ? "},\n" : "}\n");
            sb.Append(" ],\n \"after the shot\": [").Append(string.Join(", ", notes.ConvertAll(Q))).Append("],\n \"shots\": [").Append(string.Join(", ", shots.ConvertAll(Q))).Append("]\n}\n");
            File.WriteAllText(Path.Combine(dir, "traffic-probe.json"), sb.ToString());
            Debug.Log($"TrafficProbe: {(ok ? "ok" : "FAILED")}, {checks.FindAll(c => c.ok).Count}/{checks.Count} checks → {dir}");
            GameBoot.Leave(ok ? 0 : 1);
        }
        static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ") + "\"";
    }
}
