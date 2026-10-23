using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Order = Suborned.Sim.Order;

namespace Suborned.Play
{
    /// <summary>`-late &lt;resolved.json&gt; &lt;outDir&gt;` (brief 045, Splice's `late-fight` batch row; Redline found the late lag): per row a fresh run of
    /// the script, fast-forwarded paused to `warmTick` as the shot runner gives orders, then played at real time with every later order given in
    /// the host's per-tick loop (HandoverHost.BeforeStep, before the step that leaves its tick) through the click paths, so Save() and every other
    /// cost of an order runs as in play. Frame time is the real gap between frames; the window is from `startTick` to the frame before the end,
    /// the tail is the end frame and `tailSeconds` after it. Writes `late.json` (suborned.late.report/1) after every row and `name.frames.csv`;
    /// exits 0 every row ran, 1 a row did not, 2 the file would not read.</summary>
    public sealed class LateRun : MonoBehaviour
    {
        public const string Schema = "suborned.late.report/1";

        [Serializable] public class LateRow { public string name = "", scriptPath = "", outcome = "", failCode = ""; public int warmTick, startTick, endTick, w = 1920, h = 1080; public float tailSeconds = 2f; }
        [Serializable] public class LateFile { public string schema = "", outDir = ""; public LateRow[] rows; }

        sealed class Res
        {
            public string Name = "", Status = "ok", Error = "", Outcome = "", FailCode = "", Csv = ""; public int EndTick, Given, Refused;
            public readonly List<(double ms, int tick, string phase, string what)> Frames = new();
        }

        string path, dir; readonly List<Res> results = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-late");
            if (i < 0 || i + 2 >= a.Length) return;
            var go = new GameObject("late-run"); DontDestroyOnLoad(go);
            var r = go.AddComponent<LateRun>(); r.path = a[i + 1]; r.dir = a[i + 2];
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            LateFile file;
            try { file = JsonUtility.FromJson<LateFile>(File.ReadAllText(path)); if (file?.rows == null || file.rows.Length == 0) throw new FormatException("no rows"); }
            catch (Exception e) { Write("unreadable: " + e.Message); Application.Quit(2); yield break; }
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { Write("no host after 60 s"); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f); // the HUD built, the world streamed in
            h.controls.conversations = ConversationScenes.Never; // orders go in as the player's, with no scene to wait for
            bool allOk = true;
            foreach (var row in file.rows)
            {
                var res = new Res { Name = row.name, Csv = row.name + ".frames.csv" }; results.Add(res);
                if (!GameBoot.Arg("-capture-native")) yield return CaptureSize.Apply(row.w, row.h);
                List<Order> orders = null; string err = null;
                try
                {
                    h.Begin(); h.IntelOpen = false;
                    var sc = Suborned.Sim.Script.Parse(File.ReadAllText(row.scriptPath));
                    orders = sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
                }
                catch (Exception e) { err = "the script would not read: " + e.Message; }
                if (err != null) { Fail(res, err); allOk = false; Write("running"); continue; }
                yield return null;
                int oi = 0;
                void GiveOne(HandoverHost host, Order o)
                {
                    var g = host.Game; int n0 = g.Orders.Count; var c = Copy(o);
                    if (c.Do is "watch" or "listen" or "turn" or "call") host.GiveScripted(c); else host.Issue(c);
                    res.Given++; for (int k = n0; k < g.Orders.Count; k++) if (!g.Orders[k].ok) res.Refused++;
                }
                // 1 · paused, fast-forward to warmTick (not timed)
                err = Guard(() =>
                {
                    h.StartClock(); h.SetPaused(true);
                    while (oi < orders.Count && orders[oi].Tick < row.warmTick && !h.Game.Over) { var o = orders[oi++]; if (o.Tick > h.Game.Tick) h.RunTo(o.Tick); if (h.Game.Over) break; GiveOne(h, o); }
                    if (row.warmTick > h.Game.Tick && !h.Game.Over) h.RunTo(row.warmTick);
                });
                if (err != null) { Fail(res, err); allOk = false; Write("running"); continue; }
                // 2 · real time: every later order given before the step that leaves its tick
                HandoverHost.BeforeStep = host => { while (oi < orders.Count && orders[oi].Tick <= host.Game.Tick && !host.Game.Over) GiveOne(host, orders[oi++]); };
                HandoverHost.FrameCost = ""; h.SetPaused(false);
                double last = Time.realtimeSinceStartupAsDouble, overAt = -1; bool timing = false;
                while (true)
                {
                    yield return null;
                    double now = Time.realtimeSinceStartupAsDouble, ms = (now - last) * 1000.0; last = now;
                    var g = h.Game; if (g == null) { Fail(res, "the run went away"); break; }
                    string what = HandoverHost.FrameCost; HandoverHost.FrameCost = "";
                    bool over = g.Over || h.Ending.Length > 0;
                    if (over && overAt < 0) { overAt = now; res.EndTick = g.Tick; res.Outcome = g.Result.ToString().ToLowerInvariant(); res.FailCode = g.FailCode ?? ""; res.Frames.Add((ms, g.Tick, "t", what)); continue; }
                    if (overAt >= 0) { if (now - overAt > row.tailSeconds) break; res.Frames.Add((ms, g.Tick, "t", what)); continue; }
                    if (timing) res.Frames.Add((ms, g.Tick, "w", what));
                    if (!timing && g.Tick >= row.startTick) timing = true; // the frames from the first one that ends at or past startTick (this one's gap was before it)
                    if (g.Tick > row.endTick + 5 * Suborned.Sim.Game.Hz) { Fail(res, "ran past the end"); break; }
                    if (h.Paused) h.SetPaused(false); // nothing in this run pauses (a modal, a scene): play on as the script did
                }
                HandoverHost.BeforeStep = null; HandoverHost.FrameCost = "";
                try { File.WriteAllText(Path.Combine(dir, res.Csv), "ms,tick,phase,what\n" + string.Join("\n", res.Frames.Select(f => $"{F(f.ms)},{f.tick},{f.phase},{What(f.what)}")) + "\n"); } catch { }
                if (res.Status != "ok") allOk = false;
                Write("running");
            }
            Write(allOk ? "ok" : "failed"); yield return new WaitForSecondsRealtime(0.5f); Application.Quit(allOk ? 0 : 1);
        }

        void OnDestroy() { HandoverHost.BeforeStep = null; }

        static Order Copy(Order o) => new() { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run, Stance = o.Stance, Force = o.Force, On = o.On, Release = o.Release, Face = o.Face, Count = o.Count, Shots = o.Shots, Then = o.Then };

        static string Guard(Action a) { try { a(); return null; } catch (Exception e) { return e.GetType().Name + ": " + e.Message; } }
        void Fail(Res r, string why) { r.Status = "failed"; r.Error = why; Debug.LogError($"LateRun: {r.Name}: {why}"); }
        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "'").Replace("\n", " ");
        static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        /// <summary>The frames.csv `what` column: what the host did that frame (save, order, end, joined by "+"), else "none".</summary>
        static string What(string w) => string.IsNullOrEmpty(w) ? "none" : w.Replace(",", "+");

        void Write(string status)
        {
            var sb = new StringBuilder();
            sb.Append("{\n \"schema\": \"").Append(Schema).Append("\",\n \"status\": \"").Append(Esc(status)).Append("\",\n \"screen\": \"").Append(Screen.width).Append('x').Append(Screen.height)
              .Append("\",\n \"build\": \"").Append(Esc(BuildStamp.Line)).Append("\",\n \"rows\": [");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                var w = r.Frames.Where(f => f.phase == "w").Select(f => f.ms).OrderBy(x => x).ToList();
                var t = r.Frames.Where(f => f.phase == "t").Select(f => f.ms).ToList();
                double p99 = w.Count > 0 ? w[Math.Max(0, (int)Math.Ceiling(0.99 * w.Count) - 1)] : 0, mean = w.Count > 0 ? w.Average() : 0, wmax = w.Count > 0 ? w[^1] : 0, tmax = t.Count > 0 ? t.Max() : 0;
                // Aster for Splice (brief 045): every frame over 25 ms carries its label, not only the five worst (at least five are listed)
                var worst = r.Frames.OrderByDescending(f => f.ms).Where((f, i) => i < 5 || f.ms > 25.0).ToList();
                sb.Append(i == 0 ? "\n" : ",\n").Append("  {\"name\": \"").Append(Esc(r.Name)).Append("\", \"status\": \"").Append(r.Status).Append('"');
                if (r.Error.Length > 0) sb.Append(", \"error\": \"").Append(Esc(r.Error)).Append('"');
                sb.Append(", \"endTick\": ").Append(r.EndTick).Append(", \"outcome\": \"").Append(Esc(r.Outcome)).Append("\", \"failCode\": \"").Append(Esc(r.FailCode))
                  .Append("\", \"ordersGiven\": ").Append(r.Given).Append(", \"ordersRefused\": ").Append(r.Refused)
                  .Append(", \"window\": {\"count\": ").Append(w.Count).Append(", \"meanMs\": ").Append(F(mean)).Append(", \"p99Ms\": ").Append(F(p99)).Append(", \"maxMs\": ").Append(F(wmax)).Append(", \"over25\": ").Append(w.Count(x => x > 25.0)).Append(", \"over50\": ").Append(w.Count(x => x > 50.0)).Append('}')
                  .Append(", \"tail\": {\"count\": ").Append(t.Count).Append(", \"maxMs\": ").Append(F(tmax)).Append(", \"over50\": ").Append(t.Count(x => x > 50.0)).Append(", \"over100\": ").Append(t.Count(x => x > 100.0)).Append('}')
                  .Append(", \"worst\": [").Append(string.Join(", ", worst.Select(f => $"{{\"ms\": {F(f.ms)}, \"tick\": {f.tick}, \"what\": \"{Esc(f.what.Length > 0 ? f.what : f.phase == "t" ? "tail" : "")}\"}}"))).Append(']')
                  .Append(", \"frames\": \"").Append(Esc(r.Csv)).Append("\"}");
            }
            sb.Append("\n ]\n}\n");
            try { File.WriteAllText(Path.Combine(dir, "late.json"), sb.ToString()); } catch (Exception e) { Debug.LogError("LateRun: late.json not written: " + e.Message); }
        }
    }
}
