using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>`-perf064 &lt;dir&gt;` (Redline's brief 064, findings 1 and 2): the scene Redline named, timed before and after the fixes in one build.
    /// The Scout watches the patrol and the Tech the entrance camera (their fans learned; the patrol sweeps on, so its fan moves every tick),
    /// the Enforcer is selected with Takedown armed, Alt is held (PeekLatch: the armed act's people get cones, up to 7), and the pointer stays
    /// on the walker nearest the café forecourt (its cone on hover, rebuilt as it walks); the camera on the forecourt at play zoom, the crowd on,
    /// 2880 x 1864 (the Air's size). After a warm-up the probe runs four windows: before, after, before, after (HandoverHost.PerfLegacy on, off),
    /// each `seconds` long, and reports per window the frame times (mean, p99, max), the host's own cost a frame (ShowSight with the fans,
    /// PersonConesFrame, RerouteFrame: mean and max ms), cone builds and point tests a second, and fan geometry builds a second. Writes
    /// perf064.json and quits. Measures only: nothing here changes a rule, and PerfLegacy is never set in play.</summary>
    public sealed class Perf064 : MonoBehaviour
    {
        string dir; float seconds = 20f, warm = 30f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            var d = GameBoot.ArgValue("-perf064"); if (d == null) return;
            var go = new GameObject("perf064"); DontDestroyOnLoad(go); var p = go.AddComponent<Perf064>(); p.dir = d;
            if (GameBoot.ArgValue("-perf-seconds") is string s && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) p.seconds = v;
            if (GameBoot.ArgValue("-perf-warm") is string w && float.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out var wv)) p.warm = wv;
        }

        /// <summary>Redline 064 R7: each window also keeps FrameTimingManager's CPU main-thread, render-thread and GPU frame ms (the split the wall-clock
        /// interval cannot give), and its wall-clock start and end (Unix ms) so the batch's load sampler can be lined up with it (Splice, tools/game).</summary>
        sealed class Win { public string Name = ""; public bool Legacy; public readonly List<double> Ms = new(), Sight = new(), Cones = new(), Reroute = new(), Main = new(), Render = new(), Gpu = new(); public int ConeBuilds, ConeQueries, FanGeoms, Cones0; public float Secs; public int Tick0, Tick1; public long Unix0, Unix1; }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup; string note = "";
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { File.WriteAllText(Path.Combine(dir, "perf064.json"), "{\"status\": \"no host\"}\n"); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f);
            if (!GameBoot.Arg("-capture-native")) yield return CaptureSize.Apply(2880, 1864);
            h.controls.conversations = ConversationScenes.Never; h.controls.planTogether = false;
            // the scene: fans learned, the takedown armed, Alt held, the forecourt in view
            Game g = null; Vector3 forecourt = default;
            try
            {
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); g = h.Game;
                h.GiveScripted(new Order { Op = "scout", Do = "watch", Target = "patrol" });
                h.GiveScripted(new Order { Op = "tech", Do = "watch", Target = "entrance" });
                int lim = g.Tick + Game.T(150);
                while (g.Tick < lim && !g.Over && !((g.Known("patrol")?.Sight ?? false) && (g.Known("entrance")?.Sight ?? false))) h.RunTo(g.Tick + 5);
                note = $"patrol fan {(g.Known("patrol")?.Sight ?? false)}, entrance fan {(g.Known("entrance")?.Sight ?? false)} at {HandoverHost.MissionClock(g.Tick)}";
                h.Select(OpId.Enforcer, false);
                var plate = h.PlateOf(OpId.Enforcer); int slot = Array.FindIndex(plate, x => x.Id == "takedown"); if (slot >= 0) h.Pick(slot); else note += "; no takedown on the plate";
                forecourt = h.frame.World(new Pt(-1500, -2900)); h.LookAt(forecourt);
                h.PeekLatch = true;
            }
            catch (Exception e) { note += "; setup: " + e.Message; }
            yield return null;
            // the pointer rides the walker nearest the forecourt
            string walker = null;
            IEnumerator Frame()
            {
                var cam = h.View; if (cam == null) yield break;
                if (walker == null || h.Views.Find(walker) is not Transform wt || !wt.gameObject.activeInHierarchy)
                {
                    walker = null; float best = float.MaxValue;
                    foreach (var (k, t) in h.Views.All()) { if (!k.StartsWith("crowd/") || !t || !t.gameObject.activeInHierarchy) continue; float d = (t.position - forecourt).sqrMagnitude; if (d < best) { best = d; walker = k; } }
                }
                if (walker != null && h.Views.Find(walker) is Transform w2) { var sp = cam.WorldToScreenPoint(w2.position + Vector3.up); HandoverHost.PointerOverride = new Vector2(sp.x, sp.y); }
            }
            h.SetPaused(false); HandoverHost.PerfTiming = true;
            float warmUntil = Time.realtimeSinceStartup + warm;
            while (Time.realtimeSinceStartup < warmUntil && !h.Game.Over) { var f = Frame(); while (f.MoveNext()) { } if (h.Paused) h.SetPaused(false); yield return null; }
            var wins = new List<Win> { new() { Name = "before-1", Legacy = true }, new() { Name = "after-1" }, new() { Name = "before-2", Legacy = true }, new() { Name = "after-2" } };
            double tick2ms = 1000.0 / System.Diagnostics.Stopwatch.Frequency; var ft = new FrameTiming[1];
            foreach (var w in wins)
            {
                HandoverHost.PerfLegacy = w.Legacy; yield return null; yield return null; // the switch settles (one frame of each cost)
                HandoverHost.ConeBuilds = HandoverHost.ConeQueries = 0; HostViews.FanGeometries = 0; w.Tick0 = h.Game.Tick;
                double last = Time.realtimeSinceStartupAsDouble, start = last; int cones = 0, frames = 0; w.Unix0 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                while (Time.realtimeSinceStartupAsDouble - start < seconds && !h.Game.Over)
                {
                    var f = Frame(); while (f.MoveNext()) { }
                    if (h.Paused) h.SetPaused(false);
                    yield return null;
                    double now = Time.realtimeSinceStartupAsDouble; w.Ms.Add((now - last) * 1000.0); last = now;
                    w.Sight.Add(HandoverHost.PerfSightTicks * tick2ms); w.Cones.Add(HandoverHost.PerfConesTicks * tick2ms); w.Reroute.Add(HandoverHost.PerfRerouteTicks * tick2ms);
                    FrameTimingManager.CaptureFrameTimings(); // R7: the frame the timings describe lags a frame or two behind (the GPU's); fine for a 20 s window's mean and p99
                    if (FrameTimingManager.GetLatestTimings(1, ft) == 1) { if (ft[0].cpuMainThreadFrameTime > 0) w.Main.Add(ft[0].cpuMainThreadFrameTime); if (ft[0].cpuRenderThreadFrameTime > 0) w.Render.Add(ft[0].cpuRenderThreadFrameTime); if (ft[0].gpuFrameTime > 0) w.Gpu.Add(ft[0].gpuFrameTime); }
                    cones += h.PersonCones.Count; frames++;
                }
                w.Unix1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); w.Secs = (float)(Time.realtimeSinceStartupAsDouble - start); w.ConeBuilds = HandoverHost.ConeBuilds; w.ConeQueries = HandoverHost.ConeQueries; w.FanGeoms = HostViews.FanGeometries; w.Tick1 = h.Game.Tick;
                w.Cones0 = frames > 0 ? (int)Math.Round(cones / (double)frames) : 0;
            }
            HandoverHost.PerfLegacy = false; HandoverHost.PerfTiming = false; HandoverHost.PointerOverride = null; h.PeekLatch = false;
            var sb = new StringBuilder("{\n \"schema\": \"suborned.perf064/1\",\n");
            sb.Append($" \"build\": \"{BuildStamp.Line}\", \"screen\": \"{Screen.width}x{Screen.height}\", \"device\": \"{SystemInfo.deviceModel} {SystemInfo.graphicsDeviceName}\", \"warmS\": {F(warm)}, \"windowS\": {F(seconds)},\n");
            sb.Append($" \"setup\": \"{note.Replace("\"", "'")}\", \"walker\": \"{walker}\", \"over\": {(h.Game.Over ? "true" : "false")},\n \"windows\": [");
            for (int i = 0; i < wins.Count; i++)
            {
                var w = wins[i];
                sb.Append(i == 0 ? "\n" : ",\n").Append($"  {{\"name\": \"{w.Name}\", \"legacy\": {(w.Legacy ? "true" : "false")}, \"seconds\": {F(w.Secs)}, \"ticks\": [{w.Tick0}, {w.Tick1}], \"cones\": {w.Cones0}, ")
                  .Append($"\"frameMs\": {Stats(w.Ms)}, \"sightMs\": {Stats(w.Sight)}, \"conesMs\": {Stats(w.Cones)}, \"rerouteMs\": {Stats(w.Reroute)}, ")
                  .Append($"\"mainMs\": {Stats(w.Main)}, \"renderMs\": {Stats(w.Render)}, \"gpuMs\": {Stats(w.Gpu)}, \"unix\": [{w.Unix0}, {w.Unix1}], ")
                  .Append($"\"coneBuildsPerS\": {F(w.ConeBuilds / Math.Max(0.001f, w.Secs))}, \"coneQueriesPerS\": {F(w.ConeQueries / Math.Max(0.001f, w.Secs))}, \"fanGeometriesPerS\": {F(w.FanGeoms / Math.Max(0.001f, w.Secs))}}}");
            }
            sb.Append("\n ]\n}\n");
            File.WriteAllText(Path.Combine(dir, "perf064.json"), sb.ToString());
            yield return new WaitForSecondsRealtime(0.3f); Application.Quit(0);
        }

        static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string Stats(List<double> v)
        {
            if (v.Count == 0) return "{\"n\": 0}";
            var s = v.OrderBy(x => x).ToList(); double p99 = s[Math.Max(0, (int)Math.Ceiling(0.99 * s.Count) - 1)];
            return $"{{\"n\": {s.Count}, \"mean\": {F(s.Average())}, \"p99\": {F(p99)}, \"max\": {F(s[^1])}, \"over16_7\": {s.Count(x => x > 16.7)}}}";
        }
    }
}
