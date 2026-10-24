using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using Path = System.IO.Path;
using Key = UnityEngine.InputSystem.Key;

namespace Suborned.Play
{
    /// <summary>`-bot &lt;plan.json&gt; &lt;outDir&gt;` (brief 060 tier 2, Sky approved 2026-10-24): the built player plays itself through real input, at
    /// real time, as LateRun and TpLongRun run rows. Each row is a fresh Handover played by a seeded policy (random, greedy, cautious; or human, which
    /// plays toward a recorded winning route with a person's indecision on top: BotRun.Human.cs) that chooses only from the game's own offers: it selects an agent with F1-F4, clicks a target on screen (a left click opens the ring), reads the ring the
    /// host built (HandoverHost.Ring, from ContextFor) and clicks a wedge, holding it when the wedge is held; it also walks (a ground click), runs
    /// (a double click), pans (WASD) and turns (Q/E). Every input goes through the Input System devices, as ProofInput's do.
    ///
    /// Checks every frame (cheap, in-host): Q-GIVER (a ring, its plan or the open panel offers a giver who is down, carried, inside or in the car),
    /// Q-OFFER (a way taken from the ring, then refused by the engine), Q-WARN-HUD (the seal is running and its marker is not on screen; a fail
    /// whose warning never showed), Q-CARD (the end card against the engine's result, fail code and Vale's state), Q-FEET (a figure's drawn feet off
    /// the walk-file floor by more than 15 cm for over 0.5 s), Q-SOLID (a figure drawn on a cell the engine cannot walk, 0.3 m or more from a free
    /// one, for over 0.5 s), Q-ERR (an error or exception in the console), Q-FRAME (a frame over 50 ms after the warm-up that held no save,
    /// order or end) and Q-MEM (managed or total memory growing more than 5 MB a minute after the first minute). Thresholds are Suture's starting
    /// values ([S], brief 060 §2); they are the constants below.
    ///
    /// At each anomaly: one 1080p HudToolkit shot as JPEG with its manifest, and the replay: the session script up to that tick (exact, through
    /// `suborned-sim play` or `bun tools/qa/run.ts --replay`) plus the row's seed. One capture per check per 2 m cell per 30 s, at most 60 a row.
    /// Files per row (&lt;outDir&gt;/&lt;row&gt;/): session.script.json (the whole run), frames.csv, mem.csv, feet.csv (every figure every 0.5 s),
    /// hud.jsonl (the HUD manifest every 2 s), card.json (the end card's lines and the engine's word), anomalies/*. The report, bot.json
    /// (suborned.bot.report/1), is written after every row and every anomaly. Exits 0 when every row ran, 1 when a row broke, 2 on a bad file.
    /// The sessions go to the row's folder, never to the player's play folder (Sky's saved sessions are QA seeds).</summary>
    public sealed partial class BotRun : MonoBehaviour
    {
        public const string Schema = "suborned.bot.report/1";
        // [S] starting thresholds (Suture tunes them, brief 060 §2)
        public const float FeetTolM = 0.15f, FeetRunS = 0.5f, SolidFreeM = 0.3f, SolidRunS = 0.5f, FrameSpikeMs = 50f, WarmS = 20f, MemSlopeMBMin = 5f;
        // Suture's smoke triage (briefs/060 "Smoke triage", 2026-10-24): the café-seat skip, Q-SOLID against what is drawn, Q-MEM by thirds, the offer race, the open ring's grace
        public const float SeatSkipM = 1.5f, SolidBodyR = 0.25f, SolidOverlapM = 0.05f, MemWarmS = 120f, MemThirdMinS = 60f, MemTotalMBMin = 3f, MemManagedMBMin = 1f;
        public const int OfferRaceTicks = 1, GiverGraceTicks = 2;
        public const float DedupeCellM = 2f, DedupeS = 30f; public const int MaxShots = 60;

        [Serializable] public class BotRow { public string name = "", policy = "random"; public int seed = 1, w = 1920, h = 1080; public float seconds = 660f, decideS = 0f; public HumanTape human; /* the human policy's tape (tools/qa/human.ts; BotRun.Human.cs) */ }
        [Serializable] public class BotFile { public string schema = "", outDir = ""; public BotRow[] rows; }

        sealed class Anomaly { public string Check = "", Key = "", Title = "", Expected = "", Observed = "", Shot = "", Script = "", Clock = "", Noise = ""; public int Tick; public Vector3 At; }
        sealed class Res
        {
            public string Name = "", Policy = "", Status = "running", Error = "", Outcome = "", FailCode = "", Preset = ""; public int Seed, EndTick;
            public int Decisions, Clicks, Rings, RingMisses, Taken, Moves, Pans, Turns, Runs, Given, Refused, Errors, Shots, FeetSamples, Figures;
            public float Seconds; public HumanRes Human; public readonly List<Anomaly> Anomalies = new(); public readonly Dictionary<string, int> Counts = new(); public readonly Dictionary<string, int> Refusals = new();
            public readonly int[] Hist = new int[2001]; public int FrameN, Races, ShutClicks; public double FrameSum, FrameMax; public double MemStart, MemEnd, MemSlope, MemThirdTotal, MemThirdManaged; public string MemJudged = "", CardHead = ""; // frame times as a 0.1 ms histogram (Suture: no list held all run)
        }

        string path, dir; readonly List<Res> results = new(); Res cur; HandoverHost h; System.Random rng; string rowDir;
        readonly Dictionary<string, float> lastCapture = new(); readonly List<string> consoleErrors = new();
        int ordersSeen; (string op, string act, string target, float at, bool shut)? offered;
        readonly int[] ableTick = new int[4], unableSince = new int[4]; List<Vector3> chairs;
        readonly Dictionary<string, float> feetOff = new(), solidIn = new();
        bool capturing, skipFrame; object lastRing, lastPanel; float rowStart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-bot");
            if (i < 0 || i + 2 >= a.Length) return;
            var go = new GameObject("bot-run"); DontDestroyOnLoad(go);
            var r = go.AddComponent<BotRun>(); r.path = a[i + 1]; r.dir = a[i + 2];
        }

        void OnEnable() => Application.logMessageReceived += OnLog;
        void OnDisable() => Application.logMessageReceived -= OnLog;
        void OnDestroy() { HandoverHost.SessionDirOverride = null; HandoverHost.BeforeStep = null; }

        void OnLog(string msg, string stack, LogType type)
        {
            if (type is not (LogType.Error or LogType.Exception or LogType.Assert) || cur == null) return;
            if (msg.StartsWith("BotRun:")) return;
            cur.Errors++; var first = (msg ?? "").Split('\n')[0]; if (first.Length > 200) first = first[..200];
            lock (consoleErrors) consoleErrors.Add($"{type}: {first}");
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            BotFile file;
            try { file = JsonUtility.FromJson<BotFile>(File.ReadAllText(path)); if (file?.rows == null || file.rows.Length == 0) throw new FormatException("no rows"); }
            catch (Exception e) { Write("unreadable: " + e.Message); Application.Quit(2); yield break; }
            float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { Write("no host after 60 s"); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f);
            h.controls.conversations = ConversationScenes.Never; // the bot gives orders as the player's; a conversation scene would hold the clock for keys it does not press (later: scenes too)
            h.controls.planTogether = false; // orders given while paused go now
            bool allOk = true;
            foreach (var row in file.rows)
            {
                cur = new Res { Name = row.name, Policy = row.policy, Seed = row.seed, Preset = h.controls.HoldActs ? "hold" : "click" }; results.Add(cur);
                rowDir = Path.Combine(dir, row.name); Directory.CreateDirectory(Path.Combine(rowDir, "anomalies"));
                HandoverHost.SessionDirOverride = rowDir;
                if (!GameBoot.Arg("-capture-native")) yield return CaptureSize.Apply(row.w, row.h);
                yield return PlayRow(row);
                if (cur.Status == "running") cur.Status = "ok";
                if (cur.Status != "ok") allOk = false;
                Write("running");
            }
            HandoverHost.SessionDirOverride = null;
            Write(allOk ? "ok" : "failed"); yield return new WaitForSecondsRealtime(0.5f); Application.Quit(allOk ? 0 : 1);
        }

        // ---------------------------------------------------------------- one row

        IEnumerator PlayRow(BotRow row)
        {
            rng = new System.Random(row.seed); lastCapture.Clear(); feetOff.Clear(); solidIn.Clear(); lock (consoleErrors) consoleErrors.Clear(); offered = null; for (int k = 0; k < 4; k++) { ableTick[k] = 0; unableSince[k] = -1; }
            string err = Guard(() => { h.Begin(); h.IntelOpen = false; });
            if (err != null) { Fail(err); yield break; }
            yield return null; yield return null;
            err = Guard(() => { h.StartClock(); h.SetPaused(false); });
            if (err != null) { Fail(err); yield break; }
            ordersSeen = h.Game.Orders.Count;
            chairs = new List<Vector3>(); if (h.CafeSeats?.Chairs != null) foreach (var c in h.CafeSeats.Chairs.Values) chairs.Add(h.frame.World(c.at));
            float start = Time.realtimeSinceStartup, nextDecide = start + 1.5f; rowStart = start; bool humanOn = false; float nextFeet = start, nextHud = start, nextMem = start, overAt = -1f;
            float decide = row.decideS > 0f ? row.decideS : row.policy == "cautious" ? 4f : row.policy == "greedy" ? 3f : 2.5f;
            var mem = new List<(float t, double managed, double total)>();
            using var frames = new StreamWriter(Path.Combine(rowDir, "frames.csv")); using var feet = new StreamWriter(Path.Combine(rowDir, "feet.csv")); // streamed (Suture: no buffers held all run)
            frames.WriteLine("ms,tick,what"); feet.WriteLine("t,tick,key,x,y,z,floor,free,skip");
            using var hud = new StreamWriter(Path.Combine(rowDir, "hud.jsonl"));
            double last = Time.realtimeSinceStartupAsDouble; float sealHiddenSince = -1f; bool sealSeen = false, sealMissed = false; int sealFrames = 0, sealHidden = 0;
            var act = new Stack<IEnumerator>(); // an input sequence in progress, stepped a frame at a time (nested sequences pushed)
            while (true)
            {
                yield return null;
                double nowD = Time.realtimeSinceStartupAsDouble, ms = (nowD - last) * 1000.0; last = nowD; float now = (float)nowD, t = now - start;
                var g = h.Game; if (g == null) { Fail("the run went away"); break; }
                string what = HandoverHost.FrameCost; HandoverHost.FrameCost = "";
                if (skipFrame) { skipFrame = false; HandoverHost.FrameCost = ""; what = "shot"; } // the frame after a capture carries its wait: not a frame of play
                if (!capturing && what != "shot") { cur.Hist[Math.Min(2000, (int)(ms * 10.0))]++; cur.FrameN++; cur.FrameSum += ms; cur.FrameMax = Math.Max(cur.FrameMax, ms); frames.Write(F(ms)); frames.Write(','); frames.Write(g.Tick); frames.Write(','); frames.WriteLine(string.IsNullOrEmpty(what) ? "none" : what.Replace(",", "+")); }
                for (int k = 0; k < 4; k++) { if (Able(k)) { ableTick[k] = g.Tick; unableSince[k] = -1; } else if (unableSince[k] < 0) unableSince[k] = g.Tick; } // each agent's ability by tick: the offer race and the open ring's grace
                if (!capturing && t > WarmS && ms > FrameSpikeMs && string.IsNullOrEmpty(what) && overAt < 0f) yield return Hit("Q-FRAME", "frame", "A frame over 50 ms with no save, order or end in it", $"every frame under {FrameSpikeMs} ms after the first {WarmS} s", $"{F(ms)} ms at {HandoverHost.MissionClock(g.Tick)}", Focus());
                // the console
                string[] errs; lock (consoleErrors) { errs = consoleErrors.ToArray(); consoleErrors.Clear(); }
                foreach (var e in errs) yield return Hit("Q-ERR", ErrKey(e), "An error in the console", "no error or exception in the console", e, Focus());
                // the engine's refusals of what the ring offered (Q-OFFER), and every refusal counted
                for (; ordersSeen < g.Orders.Count; ordersSeen++)
                {
                    var (o, ok, code, why) = g.Orders[ordersSeen]; cur.Given++;
                    if (ok) continue; cur.Refused++; cur.Refusals[code ?? ""] = (cur.Refusals.TryGetValue(code ?? "", out var rn) ? rn : 0) + 1;
                    if (offered is { } of && now - of.at < 5f && o.Do == of.act && (of.target.Length == 0 || o.Target == of.target) && (of.op.Length == 0 || o.Op == of.op))
                    {
                        if (of.shut) { cur.ShutClicks++; continue; } // the wedge showed its lock at the click: an honest refusal ("wedge shut"), not Q-OFFER
                        int dk = o.Op is "scout" or "fixer" or "tech" or "enforcer" ? (int)Op.Parse(o.Op) : -1;
                        if (code is "agent-down" or "enforcer-inside" or "in-car" && dk >= 0 && ableTick[dk] >= o.Tick - OfferRaceTicks) { cur.Races++; continue; } // the doer went down in the order's tick: a race nothing on screen could show
                        yield return Hit("Q-OFFER", $"{o.Do}:{code}", $"Offered on the ring, then refused: {o.Do} ({code})", "a way the ring offers is taken, or its lock shows on the wedge before the click", $"{o.Op} {o.Do} {o.Target}: {code} {why}", Focus());
                    }
                }
                // Q-WARN-HUD: the seal running shows its marker (the briefing, menu or end card aside)
                if (h.SealOn && h.Modal == ModalGate.Modal.None && !capturing) // not while a shot holds the frame
                {
                    sealFrames++; sealSeen = true;
                    if (string.IsNullOrEmpty(HudToolkit.SealWords)) { sealHidden++; if (sealHiddenSince < 0f) sealHiddenSince = t; } else sealHiddenSince = -1f;
                    if (sealHiddenSince >= 0f && t - sealHiddenSince >= 0.5f) { if (!sealMissed) { sealMissed = true; yield return Hit("Q-WARN-HUD", "seal", "The seal is running and its clock is not on screen", "the seal marker under the clock pill every frame the seal runs", $"seal at {HandoverHost.MissionClock(g.Lockdown)}, {h.SealLeftS} s left, marker hidden", Focus()); } }
                }
                if (!capturing && now >= nextFeet) { nextFeet = now + 0.5f; foreach (var x in FeetFrame(t, g, feet)) yield return x; }
                if (!capturing && now >= nextHud) { nextHud = now + 2f; var mj = HudToolkit.ManifestNow(); if (mj != null) hud.WriteLine($"{{\"t\": {F(t)}, \"tick\": {g.Tick}, \"manifest\": {mj.Trim()}}}"); }
                if (now >= nextMem) { nextMem = now + 1f; mem.Add((t, GC.GetTotalMemory(false) / 1048576.0, Profiler.GetTotalAllocatedMemoryLong() / 1048576.0)); }
                // Q-GIVER: every ring and panel the host builds
                string giverWhy = null; if (!ReferenceEquals(h.Ring, lastRing) || !ReferenceEquals(h.Panel, lastPanel)) { lastRing = h.Ring; lastPanel = h.Panel; GiverCheck(out giverWhy); } // each ring and panel as built
                if (giverWhy == null && h.Ring != null) OpenRingGivers(g, out giverWhy); // and the open ring every tick (Suture: a giver down longer than GiverGraceTicks still offered)
                if (giverWhy != null) yield return Hit("Q-GIVER", giverWhy.Split(':')[0], "A downed or unable agent offered as the doer", "no giver who is down, carried, inside or in the car", giverWhy, Focus());
                // the end
                bool over = g.Over || h.Ending.Length > 0;
                if (over)
                {
                    if (overAt < 0f) { overAt = now; cur.EndTick = g.Tick; cur.Outcome = g.Result.ToString().ToLowerInvariant(); cur.FailCode = g.FailCode ?? ""; }
                    if (now - overAt > 2.5f) { foreach (var x in CardCheck(g, sealSeen, sealFrames, sealHidden)) yield return x; break; }
                    continue;
                }
                if (t > row.seconds) { cur.Status = "timeout"; cur.Error = $"no end after {row.seconds:0} s of real time (mission clock {HandoverHost.MissionClock(g.Tick)})"; cur.EndTick = g.Tick; yield return Hit("Q-STUCK", "timeout", "The run did not end", $"an ending within {row.seconds:0} s of real time", cur.Error, Focus()); break; }
                if (h.Paused && h.Modal == ModalGate.Modal.None && !capturing) h.SetPaused(false); // nothing the bot does pauses; a scene or a pop that did: play on
                if (act.Count > 0) { Step(act); continue; }
                if (row.policy == "human") // one plan for the whole row (BotRun.Human.cs): pushed once; empty again only when it broke
                {
                    if (humanOn) { if (cur.Status == "running") Fail("the human policy broke (see player.log: BotRun: a step broke)"); continue; }
                    if (row.human?.steps == null || row.human.steps.Length == 0) { Fail("a human row without its tape (tools/qa/human.ts)"); break; }
                    humanOn = true; act.Push(Human(row)); continue;
                }
                if (now >= nextDecide && h.Modal == ModalGate.Modal.None)
                {
                    nextDecide = now + decide * (0.7f + 0.6f * (float)rng.NextDouble()); cur.Decisions++;
                    act.Push(Decide(row.policy));
                }
            }
            HandoverHost.BeforeStep = null;
            cur.Seconds = Time.realtimeSinceStartup - start;
            try { File.WriteAllText(Path.Combine(rowDir, "mem.csv"), "t,managedMB,totalMB\n" + string.Join("\n", mem.Select(m => $"{F(m.t)},{F(m.managed)},{F(m.total)}")) + "\n"); } catch { }
            // Q-MEM (Suture's rule): after a 120 s warm-up the rest is cut in thirds, each third's floor its lowest sample (transients drop out); a leak
            // only when the floor rises on both steps by more than 3 MB/min total or 1 MB/min managed; judged only when each third lasts 60 s. The OLS
            // slope stays in the report as information
            var w = mem.Where(m => m.t > MemWarmS).ToList();
            if (mem.Count > 0) { cur.MemStart = mem[0].total; cur.MemEnd = mem[^1].total; }
            if (w.Count > 30)
            {
                double Slope(Func<(float t, double managed, double total), double> v) { double mt = w.Average(m => m.t), mv = w.Average(v), num = w.Sum(m => (m.t - mt) * (v(m) - mv)), den = w.Sum(m => (m.t - mt) * (m.t - mt)); return den > 0 ? num / den * 60.0 : 0; }
                cur.MemSlope = Math.Max(Slope(m => m.managed), Slope(m => m.total));
            }
            float span = w.Count > 1 ? w[^1].t - w[0].t : 0f;
            if (span / 3f < MemThirdMinS) cur.MemJudged = $"not judged: {F(span)} s after the warm-up (thirds of {MemThirdMinS} s needed)";
            else
            {
                float third = span / 3f, t0m = w[0].t;
                (double tot, double man) Floor(int k) { var part = w.Where(m => m.t >= t0m + k * third && m.t < t0m + (k + 1) * third + 1e-3f).ToList(); return (part.Min(m => m.total), part.Min(m => m.managed)); }
                var f0 = Floor(0); var f1 = Floor(1); var f2 = Floor(2); double perMin = 60.0 / third;
                double t1 = (f1.tot - f0.tot) * perMin, t2 = (f2.tot - f1.tot) * perMin, m1 = (f1.man - f0.man) * perMin, m2 = (f2.man - f1.man) * perMin;
                cur.MemThirdTotal = Math.Min(t1, t2); cur.MemThirdManaged = Math.Min(m1, m2);
                bool leak = t1 > MemTotalMBMin && t2 > MemTotalMBMin || m1 > MemManagedMBMin && m2 > MemManagedMBMin;
                cur.MemJudged = leak ? "leak" : "ok";
                if (leak) yield return Hit("Q-MEM", "thirds", "Memory grows through the run", $"the floor of each third after {MemWarmS} s rising no more than {MemTotalMBMin} MB/min total or {MemManagedMBMin} MB/min managed on both steps", $"total {F(t1)} then {F(t2)} MB/min, managed {F(m1)} then {F(m2)} MB/min (thirds of {F(third)} s)", Focus(), shot: false);
            }
            try { if (h.Game != null) h.SaveSessionTo(Path.Combine(rowDir, "session.script.json")); } catch (Exception e) { Debug.LogWarning("BotRun: session not copied: " + e.Message); }
        }

        // ---------------------------------------------------------------- deciding (policies) and giving (real input)

        static readonly string[] GreedyOrder = { "go", "board", "take", "act", "call", "loop", "reroute", "turn", "watch", "listen", "carry", "restrain", "stash", "takedown", "fire" };
        static readonly string[] CautiousOrder = { "watch", "listen", "turn", "loop", "reroute", "call", "act", "take", "board", "go", "carry" };

        /// <summary>Go is worth giving: Vale is in the car and every agent is in it or down (no one standing is left behind).</summary>
        static bool GoReady(Game g) => g.Vale == ValeAt.Car && g.Ops.All(o => o.InCar || o.Down);
        bool Able(int k) { var op = h.Game.Ops[k]; return !op.Down && !op.Inside && !op.InCar && op.CarriedBy == null; }
        bool Idle(int k) { var op = h.Game.Ops[k]; return op.Busy == null && op.Act == null && op.Walk == null; }

        IEnumerator Decide(string policy)
        {
            var g = h.Game; var cam = h.View; if (cam == null) yield break;
            // the agent: random any able (busy too: that is a test); greedy and cautious an idle able one when there is one
            var able = Enumerable.Range(0, 4).Where(Able).ToList();
            if (policy != "random" && GoReady(g) && Enumerable.Range(0, 4).FirstOrDefault(j => g.Ops[j].InCar) is int kc && g.Ops[kc].InCar) able = new List<int> { kc }; // Go is given from the car, by one aboard
            if (able.Count == 0) yield break;
            var idle = policy != "random" && GoReady(g) ? able : able.Where(Idle).ToList();
            int k = policy == "random" || idle.Count == 0 ? able[rng.Next(able.Count)] : idle[rng.Next(idle.Count)];
            if (!h.Selected[k] || h.Agent != (OpId)k) { yield return Keys(new[] { Key.F1 + k }, 2); }
            double roll = rng.NextDouble();
            if (policy == "random" && roll < 0.04) { cur.Turns++; yield return Keys(new[] { rng.Next(2) == 0 ? Key.Q : Key.E }, 2); yield break; }
            // the candidates: what is drawn on screen now, off the HUD, and the host's own offers on it
            var cands = new List<(HandoverHost.Pickable p, Vector2 sp, HandoverHost.ContextPlan plan)>();
            var opPos = h.frame.World(g.Ops[k].Pos);
            HandoverHost.FrameCost += HandoverHost.FrameCost.Length > 0 ? "+bot" : "bot"; // the offers are priced this frame (BuildPanel): Q-FRAME does not count the bot's own work
            foreach (var p in h.Pickables().OrderBy(p => (p.Anchor - opPos).sqrMagnitude).Take(12))
            {
                if (p.T.Kind == "op" && !g.Ops[(int)Op.Parse(p.T.Key)].Down) continue;
                var s3 = cam.WorldToScreenPoint(p.Anchor); if (s3.z <= 0f) continue; var sp = new Vector2(s3.x, s3.y);
                if (sp.x < 80 || sp.y < 80 || sp.x > Screen.width - 80 || sp.y > Screen.height - 80 || HandoverHost.OverHud(sp)) continue;
                HandoverHost.ContextPlan plan = null; try { plan = h.ContextFor(p.T, sp); } catch (Exception e) { Debug.LogWarning($"BotRun: ContextFor {p.T.Kind}/{p.T.Key}: {e.Message}"); }
                if (plan == null) continue; cands.Add((p, sp, plan));
            }
            bool Offers(HandoverHost.ContextPlan pl) => pl.Rep.Count > 0 && Enumerable.Range(0, pl.Rep.Count).Any(w => pl.Row(w).Available);
            var useful = cands.Where(c => Offers(c.plan)).ToList();
            // a walk, a run, or a pan when nothing here is worth a click
            bool move = policy == "random" ? roll < 0.22 : useful.Count == 0 && roll < 0.5;
            if (useful.Count == 0 && !move) { yield return PanToward(k); yield break; }
            if (move) { yield return GroundMove(k, policy == "random" && rng.NextDouble() < 0.25); yield break; }
            // the target and the way, by policy
            (HandoverHost.Pickable p, Vector2 sp, HandoverHost.ContextPlan plan) pick; int wantWay = -1;
            if (policy == "random") { var pool = cands.Count > 0 && (useful.Count == 0 || rng.NextDouble() < 0.15) ? cands : useful; if (pool.Count == 0) { yield return PanToward(k); yield break; } pick = pool[rng.Next(pool.Count)]; } // now and then a thing with nothing open: its lock should say why
            else
            {
                var order = policy == "cautious" ? CautiousOrder : GreedyOrder; float best = float.MinValue; pick = useful[0];
                foreach (var c in useful) for (int w = 0; w < c.plan.Rep.Count; w++)
                    {
                        var r = c.plan.Row(w); if (!r.Available || r.I?.Order == null) continue; var o = r.I.Order;
                        if (policy == "cautious" && (o.Do is "takedown" or "fire" || o.Do == "turn" && o.Approach == "pressure" || ContextRank.Num(r.I.Trail) > 0)) continue;
                        if (o.Do == "go" && !GoReady(g)) continue; // the 0.2.117 greedy row: Go ranked first with Vale outside (a withdraw) opened the going-loud card 164 times, was backed off each time, and nobody else boarded
                        int rank = Array.IndexOf(order, o.Do); if (rank < 0) continue;
                        float score = (order.Length - rank) * 10f - ContextRank.Num(r.I.Usd) / 20000f - ContextRank.Num(r.I.Trail) * 3f + (c.plan.Givers[w].Contains((OpId)k) ? 2f : 0f) + (float)rng.NextDouble();
                        if (score > best) { best = score; pick = c; wantWay = w; }
                    }
                if (wantWay < 0) { yield return PanToward(k); yield break; }
            }
            // the click that opens the ring (item 33: a left click on a thing), then the wedge
            cur.Clicks++; yield return Click(pick.sp);
            float until = Time.realtimeSinceStartup + 0.8f; while (h.Ring == null && Time.realtimeSinceStartup < until) yield return null;
            var rs = h.Ring; if (rs == null) { cur.RingMisses++; yield break; }
            cur.Rings++;
            var segs = Enumerable.Range(0, rs.Segs.Count).Where(i => !rs.Segs[i].More).ToList();
            if (segs.Count == 0) { yield return Click(rs.Screen + new Vector2(0f, -200f)); yield break; }
            int seg;
            if (wantWay >= 0 && segs.FirstOrDefault(i => rs.Segs[i].Way == wantWay) is int si && rs.Segs[si].Way == wantWay) seg = si;
            else if (policy == "random") seg = segs[rng.Next(segs.Count)];
            else { var open = segs.Where(i => !rs.Segs[i].Shut && rs.Segs[i].Row?.Available == true).ToList(); if (open.Count == 0) { yield return Keys(new[] { Key.Escape }, 2); yield break; } seg = open[0]; }
            var s = rs.Segs[seg]; var od = s.Row?.I?.Order;
            offered = od == null ? null : (s.By.Count == 1 ? Op.Name(s.By[0]) : "", od.Do ?? "", od.Target ?? "", Time.realtimeSinceStartup, s.Shut || s.Row?.Available == false); // a wedge shown shut (or a locked row) at the click
            cur.Taken++;
            var at = WedgeAt(rs, seg, rs.Segs.Count);
            bool hold = s.HoldWedge || s.Harmful || od?.Do is "fire" or "go"; // Go: held through the card when it leaves a downed agent behind
            yield return Press(at, hold ? 1.4f : 0f); // a held wedge: the wedge's hold (0.5 s) and the going-loud card's (0.5 s) on the same button
            if (h.Loud != null && h.Loud.Source == "wait") yield return RightTap(at); // a card that would fail the mission waits for a hold on it: back off
            yield return Wait(0.3f);
        }

        /// <summary>One frame of a nested input sequence: the innermost step runs; a step that yields a sequence starts it.</summary>
        static void Step(Stack<IEnumerator> st)
        {
            while (st.Count > 0)
            {
                var top = st.Peek(); bool more;
                try { more = top.MoveNext(); } catch (Exception e) { Debug.LogWarning("BotRun: a step broke: " + e.Message); st.Clear(); return; }
                if (!more) { st.Pop(); continue; }
                if (top.Current is IEnumerator inner) { st.Push(inner); continue; }
                return; // a frame's wait
            }
        }

        /// <summary>A wedge's middle on screen (HandoverHost.RingSegAt's geometry: 0 at 12 o'clock, clockwise, between the inner and outer radius).</summary>
        static Vector2 WedgeAt(HandoverHost.RingState rs, int i, int n)
        {
            float k = Mathf.Max(1f, Mathf.Round(Screen.height / 1080f)), r = (HandoverHost.RingInner + HandoverHost.RingOuter) * 0.5f * k, a = i * 360f / n * Mathf.Deg2Rad;
            return rs.Screen + new Vector2(Mathf.Sin(a) * r, Mathf.Cos(a) * r);
        }

        IEnumerator GroundMove(int k, bool run)
        {
            var g = h.Game; var cam = h.View; var c = cam.WorldToScreenPoint(h.frame.World(g.Ops[k].Pos));
            for (int tries = 0; tries < 8; tries++)
            {
                var sp = new Vector2(c.x, c.y) + new Vector2((float)(rng.NextDouble() * 2 - 1) * 320f, (float)(rng.NextDouble() * 2 - 1) * 220f);
                if (sp.x < 80 || sp.y < 80 || sp.x > Screen.width - 80 || sp.y > Screen.height - 80 || HandoverHost.OverHud(sp)) continue;
                bool near = false; foreach (var p in h.Pickables()) { var s3 = cam.WorldToScreenPoint(p.Anchor); if (s3.z > 0 && (new Vector2(s3.x, s3.y) - sp).magnitude < 40f) { near = true; break; } }
                if (near) continue; // a click on a thing opens its ring: this is a walk
                cur.Moves++; if (run) { cur.Runs++; yield return Click(sp); yield return Wait(0.06f); }
                yield return Click(sp); yield break;
            }
        }

        IEnumerator PanToward(int k)
        {
            var g = h.Game; var cam = h.View; cur.Pans++;
            // toward a person nobody has dealt with yet, else back to the agent
            var ps = h.Pickables().Where(p => p.T.Kind is "person" or "camera" or "cabinet" or "door" or "car").ToList();
            var aim = ps.Count > 0 && rng.NextDouble() < 0.7 ? ps[rng.Next(ps.Count)].Anchor : h.frame.World(g.Ops[k].Pos);
            var s3 = cam.WorldToScreenPoint(aim); var d = new Vector2(s3.x - Screen.width / 2f, s3.y - Screen.height / 2f); if (s3.z < 0) d = -d;
            var keys = new List<Key>(); if (d.y > 60) keys.Add(Key.W); if (d.y < -60) keys.Add(Key.S); if (d.x > 60) keys.Add(Key.D); if (d.x < -60) keys.Add(Key.A);
            if (keys.Count == 0) yield break;
            int frames = Mathf.Clamp((int)(d.magnitude / 12f), 6, 45);
            yield return Keys(keys.ToArray(), frames);
        }

        // ---------------------------------------------------------------- devices (as ProofInput)

        static void MouseState0(Vector2 sp, bool left, bool right)
        {
            if (Mouse.current is not Mouse m) return;
            var st = new MouseState { position = sp }; if (left) st = st.WithButton(MouseButton.Left, true); if (right) st = st.WithButton(MouseButton.Right, true);
            InputSystem.QueueStateEvent(m, st);
        }
        static IEnumerator Click(Vector2 sp) { MouseState0(sp, false, false); yield return null; MouseState0(sp, true, false); yield return null; MouseState0(sp, false, false); yield return null; }
        static IEnumerator Press(Vector2 sp, float holdS)
        {
            MouseState0(sp, false, false); yield return null; MouseState0(sp, true, false); yield return null;
            float until = Time.realtimeSinceStartup + holdS; while (Time.realtimeSinceStartup < until) { MouseState0(sp, true, false); yield return null; }
            MouseState0(sp, false, false); yield return null;
        }
        static IEnumerator RightTap(Vector2 sp) { MouseState0(sp, false, true); yield return null; MouseState0(sp, false, false); yield return null; }
        static IEnumerator Keys(Key[] down, int frames)
        {
            if (Keyboard.current is not Keyboard kb) yield break;
            for (int i = 0; i < Math.Max(1, frames); i++) { InputSystem.QueueStateEvent(kb, new KeyboardState(down)); yield return null; }
            InputSystem.QueueStateEvent(kb, new KeyboardState()); yield return null;
        }
        static IEnumerator Wait(float s) { float until = Time.realtimeSinceStartup + s; while (Time.realtimeSinceStartup < until) yield return null; }

        // ---------------------------------------------------------------- checks

        void GiverCheck(out string why)
        {
            why = null; var g = h.Game; if (g == null) return;
            string Bad(OpId k) { var op = g.Ops[(int)k]; return op.Down ? "down" : op.CarriedBy != null ? "carried" : op.Inside ? "inside" : op.InCar ? "in the car" : null; }
            var rs = h.Ring;
            if (rs != null)
            {
                foreach (var s in rs.Segs) { if (s.More || s.Shut) continue; foreach (var k in s.By) if (Bad(k) is string b) { why = $"ring:{Op.Name(k)} {b}: offered for {s.Word} on {rs.Plan?.T?.Kind}/{rs.Plan?.T?.Key}"; return; } }
                var pl = rs.Plan; if (pl != null) for (int w = 0; w < pl.Rep.Count; w++) if (pl.Row(w).Available) foreach (var k in pl.Givers[w]) if (Bad(k) is string b) { why = $"plan:{Op.Name(k)} {b}: a giver of {pl.Row(w).Verb} on {pl.T?.Kind}/{pl.T?.Key}"; return; }
            }
            var pn = h.Panel; if (pn != null) foreach (var r in pn.Rows) if (r.Available && Bad(r.By) is string b) { why = $"panel:{Op.Name(r.By)} {b}: offered {r.Verb} on {pn.T?.Kind}/{pn.T?.Key}"; return; }
        }

        /// <summary>Q-GIVER on the open ring, every frame (Suture): a wedge still offered by a giver who has been unable longer than GiverGraceTicks.</summary>
        void OpenRingGivers(Game g, out string why)
        {
            why = null; var rs = h.Ring; if (rs == null) return;
            foreach (var sg in rs.Segs)
            {
                if (sg.More || sg.Shut) continue;
                foreach (var k in sg.By) { int i = (int)k; if (unableSince[i] >= 0 && g.Tick - unableSince[i] > GiverGraceTicks) { var op = g.Ops[i]; why = $"open-ring:{Op.Name(k)} {(op.Down ? "down" : op.CarriedBy != null ? "carried" : op.Inside ? "inside" : "in the car")} {(g.Tick - unableSince[i]) / 10f:0.0} s: still offered for {sg.Word} on {rs.Plan?.T?.Kind}/{rs.Plan?.T?.Key}"; return; } }
            }
        }

        IEnumerable FeetFrame(float t, Game g, StreamWriter csv)
        {
            var grid = g.S.Grid; int n = 0;
            foreach (var (key, p, skip0) in h.Views.FigureFeet())
            {
                bool skip = skip0;
                n++; var b = h.frame.ToBlock(new Vector2(p.x, p.z)); float floor = h.frame.Height(b.x, b.y); var ep = h.frame.Engine(p); bool free = grid.Free(ep);
                // Suture: a crowd figure within SeatSkipM of a café chair is not judged (the last metre to a chair lies inside the grid's clearance, by design)
                if (!skip && key.StartsWith("crowd/") && chairs != null) foreach (var c in chairs) { var d = c - p; d.y = 0f; if (d.sqrMagnitude < SeatSkipM * SeatSkipM) { skip = true; break; } }
                csv.Write(F(t)); csv.Write(','); csv.Write(g.Tick); csv.Write(','); csv.Write(key); csv.Write(','); csv.Write(F(p.x)); csv.Write(','); csv.Write(F(p.y)); csv.Write(','); csv.Write(F(p.z)); csv.Write(','); csv.Write(F(floor)); csv.Write(','); csv.Write(free ? 1 : 0); csv.Write(','); csv.WriteLine(skip ? 1 : 0);
                cur.FeetSamples++;
                if (skip) { feetOff.Remove(key); solidIn.Remove(key); continue; }
                float dy = Mathf.Abs(p.y - floor);
                if (dy > FeetTolM) { if (!feetOff.TryGetValue(key, out var since)) feetOff[key] = t; else if (t - since >= FeetRunS) { feetOff[key] = float.MaxValue; yield return Hit("Q-FEET", key, $"Feet off the floor: {key}", $"feet within {FeetTolM * 100:0} cm of the drawn floor", $"feet {F(p.y)} m, floor {F(floor)} m ({F(dy * 100f)} cm) for over {FeetRunS} s", p); } }
                else feetOff.Remove(key);
                bool inGrid = ep.X >= grid.X0 && ep.Y >= grid.Y0 && ep.X < grid.X0 + grid.Nx * grid.Cell && ep.Y < grid.Y0 + grid.Ny * grid.Cell; // the walk grid covers the site only: outside it nothing is free (smoke row 0.2.113: crowd beyond the block flagged)
                // Suture: against what is drawn: the body (SolidBodyR) overlapping a walk-file solid cell (kind 2, 4) by more than SolidOverlapM; without the
                // baked kinds, off a free walk cell by SolidFreeM measured to the free cell's edge (the grid's blocked cells carry the 0.65 m body clearance)
                bool inSolid = h.frame.HasKinds ? h.frame.SolidNear(b.x, b.y, SolidBodyR - SolidOverlapM)
                    : inGrid && !free && (grid.NearestFree(ep) is not Pt nf || G.Dist(nf, ep) - grid.Cell / 2 >= SolidFreeM * 100f);
                if (inSolid) { if (!solidIn.TryGetValue(key, out var since)) solidIn[key] = t; else if (t - since >= SolidRunS) { solidIn[key] = float.MaxValue; yield return Hit("Q-SOLID", key, $"A figure inside a solid: {key}", "every figure on a cell the engine can walk, or within 0.3 m of one", $"{key} at ({F(p.x)}, {F(p.z)}) for over {SolidRunS} s", p); } }
                else solidIn.Remove(key);
            }
            cur.Figures = Math.Max(cur.Figures, n);
        }

        IEnumerable CardCheck(Game g, bool sealSeen, int sealFrames, int sealHidden)
        {
            var lines = HudToolkit.EndLinesNow?.ToList() ?? new List<string>(); cur.CardHead = lines.Count > 0 ? lines[0] : "";
            var text = h.Text; string fc = g.FailCode ?? ""; bool boarded = g.Log.Any(l => l.Code == "board.in" && l.Text.IndexOf("councillor", StringComparison.OrdinalIgnoreCase) >= 0) || g.Vale == ValeAt.Car;
            string want = g.Result == Outcome.Success ? "Vale is in the car" : g.Result == Outcome.Withdrawn ? "You withdrew" : text != null && text.EndFail.TryGetValue(fc, out var ft) && ft.Length > 0 ? ft.TrimEnd('.') : "";
            var problems = new List<string>();
            if (lines.Count == 0) problems.Add("no end card lines");
            else if (want.Length > 0 && lines[0] != want) problems.Add($"head \"{lines[0]}\", the engine's result wants \"{want}\"");
            if (g.Result != Outcome.Success && lines.Any(l => l.Contains("escapes with Vale"))) problems.Add("a failed run's card says the crew escaped with Vale");
            string reached = text != null && text.Route.TryGetValue("vale-reaches-getaway-car", out var rw) ? rw : "Vale reached the getaway car";
            if (!boarded && lines.Any(l => l.Contains(reached.TrimEnd('.')))) problems.Add($"the card says \"{reached}\" but Vale never boarded (no board.in)");
            if (g.Result == Outcome.Failure && fc == "sealed" && h.SealedCar is string car && car.Length > 0 && car != "unknown" && lines.Count > 1 && text != null)
            { var (sl, _, _) = h.SealedWords(g.Cause as FailCause); if (!string.IsNullOrEmpty(sl) && lines[1] != sl) problems.Add($"sealed with the car {car}: line \"{lines[1]}\", wants \"{sl}\""); }
            try
            {
                var sb = new StringBuilder("{\n");
                sb.Append($" \"result\": \"{Esc(g.Result.ToString().ToLowerInvariant())}\", \"failCode\": \"{Esc(fc)}\", \"tick\": {g.Tick}, \"valeBoarded\": {(boarded ? "true" : "false")}, \"sealedCar\": \"{Esc(h.SealedCar)}\",\n");
                sb.Append($" \"cause\": \"{Esc(g.Cause is FailCause c ? c.Why : "")}\",\n \"lines\": [{string.Join(", ", lines.Select(l => "\"" + Esc(l) + "\""))}],\n \"problems\": [{string.Join(", ", problems.Select(p => "\"" + Esc(p) + "\""))}]\n}}\n");
                File.WriteAllText(Path.Combine(rowDir, "card.json"), sb.ToString());
            }
            catch { }
            foreach (var p in problems) yield return Hit("Q-CARD", fc.Length > 0 ? fc : cur.Outcome, "The end card contradicts the engine", "the card's head and lines agree with the result, the fail code and Vale's state", p, Focus(), dedupe: false);
            // Q-WARN-HUD at the fail: a sealed run whose clock never showed (the fail had no warning on screen)
            if (fc == "sealed" && (!sealSeen || sealHidden == sealFrames)) yield return Hit("Q-WARN-HUD", "sealed-unwarned", "Sealed with no clock on screen", "the seal marker on screen from the seal's start to the end", sealSeen ? $"the marker hidden in all {sealFrames} frames of the seal" : "the seal never ran in a frame the HUD drew", Focus(), dedupe: false);
            // the card itself, always (the shot Q-CARD's reader and Glyph look at)
            yield return Shot("end-card");
        }

        Vector3 Focus() => h.View ? h.View.transform.position + h.View.transform.forward * 30f : Vector3.zero;

        /// <summary>An anomaly: counted always; captured (shot and replay) once per check per 2 m cell per 30 s, at most MaxShots a row.</summary>
        IEnumerator Hit(string check, string key, string title, string expected, string observed, Vector3 at, bool shot = true, bool dedupe = true)
        {
            cur.Counts[check] = (cur.Counts.TryGetValue(check, out var c) ? c : 0) + 1;
            float now = Time.realtimeSinceStartup; string cell = $"{check}|{Mathf.FloorToInt(at.x / DedupeCellM)}|{Mathf.FloorToInt(at.z / DedupeCellM)}|{key}";
            if (dedupe && lastCapture.TryGetValue(cell, out var was) && now - was < DedupeS) yield break;
            lastCapture[cell] = now;
            var g = h.Game; var a = new Anomaly { Check = check, Key = key, Title = title, Expected = expected, Observed = observed, Tick = g?.Tick ?? 0, Clock = g != null ? HandoverHost.MissionClock(g.Tick) : "", At = at, Noise = cur.Human != null ? NoiseNow() : "" };
            cur.Anomalies.Add(a);
            if (cur.Anomalies.Count > 400) cur.Anomalies.RemoveAt(0);
            string stem = $"{cur.Anomalies.Count:000}-{check}-{a.Tick}";
            try { if (g != null) { var sp = Path.Combine(rowDir, "anomalies", stem + ".script.json"); h.SaveSessionTo(sp); a.Script = Rel(sp); } } catch (Exception e) { Debug.LogWarning("BotRun: replay not saved: " + e.Message); }
            if (shot && cur.Shots < MaxShots) { var sh = Shot(stem); while (sh.MoveNext()) yield return sh.Current; a.Shot = cur.Shots > 0 ? Rel(Path.Combine(rowDir, "anomalies", stem + ".jpg")) : ""; }
            Write("running");
        }

        /// <summary>One capture (HudToolkit.Shoot, the game held still while it runs), kept as a JPEG and its manifest; the PNG, HUD layer and parts go.</summary>
        IEnumerator Shot(string stem)
        {
            if (cur.Shots >= MaxShots) yield break;
            capturing = true; bool was = h.Paused; h.SetPaused(true);
            while (HudToolkit.ShotBusy) yield return null;
            var basePath = Path.Combine(rowDir, "anomalies", stem); HudToolkit.Shoot(basePath, "rest"); yield return null;
            float until = Time.realtimeSinceStartup + 10f; while (HudToolkit.ShotBusy && Time.realtimeSinceStartup < until) yield return null;
            try
            {
                if (File.Exists(basePath + ".png"))
                {
                    var tex = new Texture2D(2, 2); tex.LoadImage(File.ReadAllBytes(basePath + ".png")); File.WriteAllBytes(basePath + ".jpg", tex.EncodeToJPG(82)); Destroy(tex);
                    File.Delete(basePath + ".png"); if (File.Exists(basePath + ".hud.png")) File.Delete(basePath + ".hud.png"); if (File.Exists(basePath + ".parts.json")) File.Delete(basePath + ".parts.json");
                    cur.Shots++;
                }
            }
            catch (Exception e) { Debug.LogWarning("BotRun: shot not kept: " + e.Message); }
            h.SetPaused(was); capturing = false; skipFrame = true; yield return null;
        }

        string Rel(string p) => p.StartsWith(dir) ? p[dir.Length..].TrimStart('/', '\\') : p;
        static string ErrKey(string e) { var s = new string(e.Where(ch => !char.IsDigit(ch)).ToArray()); return s.Length > 80 ? s[..80] : s; }
        void Fail(string why) { cur.Status = "failed"; cur.Error = why; Debug.LogWarning($"BotRun: {cur.Name}: {why}"); }
        static string Guard(Action a) { try { a(); return null; } catch (Exception e) { return e.GetType().Name + ": " + e.Message; } }
        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "'").Replace("\n", " ").Replace("\u0001", " ");
        static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        void Write(string status)
        {
            var sb = new StringBuilder();
            sb.Append("{\n \"schema\": \"").Append(Schema).Append("\",\n \"status\": \"").Append(Esc(status)).Append("\",\n \"screen\": \"").Append(Screen.width).Append('x').Append(Screen.height)
              .Append("\",\n \"build\": \"").Append(Esc(BuildStamp.Line)).Append("\",\n \"thresholds\": {\"feetTolM\": ").Append(F(FeetTolM)).Append(", \"feetRunS\": ").Append(F(FeetRunS)).Append(", \"solidFreeM\": ").Append(F(SolidFreeM))
              .Append(", \"frameSpikeMs\": ").Append(F(FrameSpikeMs)).Append(", \"warmS\": ").Append(F(WarmS)).Append(", \"memWarmS\": ").Append(F(MemWarmS)).Append(", \"memThirdMinS\": ").Append(F(MemThirdMinS)).Append(", \"memTotalMBMin\": ").Append(F(MemTotalMBMin)).Append(", \"memManagedMBMin\": ").Append(F(MemManagedMBMin)).Append(", \"seatSkipM\": ").Append(F(SeatSkipM)).Append(", \"solidBodyR\": ").Append(F(SolidBodyR)).Append(", \"solidOverlapM\": ").Append(F(SolidOverlapM)).Append(", \"offerRaceTicks\": ").Append(OfferRaceTicks).Append(", \"giverGraceTicks\": ").Append(GiverGraceTicks).Append(", \"dedupeCellM\": ").Append(F(DedupeCellM)).Append(", \"dedupeS\": ").Append(F(DedupeS)).Append(", \"maxShots\": ").Append(MaxShots).Append("},\n \"rows\": [");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i]; double mean = r.FrameN > 0 ? r.FrameSum / r.FrameN : 0, max = r.FrameMax, p99 = 0; int over50 = 0;
                { long want = (long)Math.Ceiling(0.99 * r.FrameN), run = 0; for (int b = 0; b < r.Hist.Length; b++) { run += r.Hist[b]; if (b >= 500) over50 += r.Hist[b]; if (p99 == 0 && run >= want && r.FrameN > 0) p99 = (b + 1) / 10.0; } } // the 0.1 ms histogram's p99 (bin top)
                sb.Append(i == 0 ? "\n" : ",\n").Append("  {\"name\": \"").Append(Esc(r.Name)).Append("\", \"policy\": \"").Append(Esc(r.Policy)).Append("\", \"seed\": ").Append(r.Seed).Append(", \"status\": \"").Append(Esc(r.Status)).Append('"');
                if (r.Error.Length > 0) sb.Append(", \"error\": \"").Append(Esc(r.Error)).Append('"');
                sb.Append(", \"preset\": \"").Append(r.Preset).Append("\", \"seconds\": ").Append(F(r.Seconds)).Append(", \"endTick\": ").Append(r.EndTick).Append(", \"outcome\": \"").Append(Esc(r.Outcome)).Append("\", \"failCode\": \"").Append(Esc(r.FailCode)).Append("\", \"cardHead\": \"").Append(Esc(r.CardHead)).Append('"')
                  .Append(", \"input\": {\"decisions\": ").Append(r.Decisions).Append(", \"clicks\": ").Append(r.Clicks).Append(", \"rings\": ").Append(r.Rings).Append(", \"ringMisses\": ").Append(r.RingMisses).Append(", \"taken\": ").Append(r.Taken)
                  .Append(", \"moves\": ").Append(r.Moves).Append(", \"runs\": ").Append(r.Runs).Append(", \"pans\": ").Append(r.Pans).Append(", \"turns\": ").Append(r.Turns).Append('}')
                  .Append(", \"orders\": {\"given\": ").Append(r.Given).Append(", \"refused\": ").Append(r.Refused).Append(", \"codes\": {").Append(string.Join(", ", r.Refusals.Select(kv => $"\"{Esc(kv.Key)}\": {kv.Value}"))).Append("}}")
                  .Append(", \"frames\": {\"count\": ").Append(r.FrameN).Append(", \"meanMs\": ").Append(F(mean)).Append(", \"p99Ms\": ").Append(F(p99)).Append(", \"maxMs\": ").Append(F(max)).Append(", \"over50\": ").Append(over50).Append('}')
                  .Append(", \"memory\": {\"startMB\": ").Append(F(r.MemStart)).Append(", \"endMB\": ").Append(F(r.MemEnd)).Append(", \"slopeMBPerMin\": ").Append(F(r.MemSlope)).Append(", \"thirdTotalMBPerMin\": ").Append(F(r.MemThirdTotal)).Append(", \"thirdManagedMBPerMin\": ").Append(F(r.MemThirdManaged)).Append(", \"judged\": \"").Append(Esc(r.MemJudged)).Append("\"}")
                  .Append(", \"races\": ").Append(r.Races).Append(", \"shutClicks\": ").Append(r.ShutClicks)
                  .Append(", \"consoleErrors\": ").Append(r.Errors).Append(", \"feetSamples\": ").Append(r.FeetSamples).Append(", \"figures\": ").Append(r.Figures).Append(", \"shots\": ").Append(r.Shots)
                  .Append(", \"counts\": {").Append(string.Join(", ", r.Counts.Select(kv => $"\"{kv.Key}\": {kv.Value}"))).Append('}')
                  .Append(", \"anomalies\": [");
                for (int j = 0; j < r.Anomalies.Count; j++)
                {
                    var a = r.Anomalies[j];
                    sb.Append(j == 0 ? "\n    " : ",\n    ").Append("{\"check\": \"").Append(a.Check).Append("\", \"key\": \"").Append(Esc(a.Key)).Append("\", \"title\": \"").Append(Esc(a.Title)).Append("\", \"expected\": \"").Append(Esc(a.Expected))
                      .Append("\", \"observed\": \"").Append(Esc(a.Observed)).Append("\", \"tick\": ").Append(a.Tick).Append(", \"clock\": \"").Append(a.Clock).Append("\", \"at\": [").Append(F(a.At.x)).Append(", ").Append(F(a.At.z))
                      .Append("], \"shot\": \"").Append(Esc(a.Shot)).Append("\", \"script\": \"").Append(Esc(a.Script)).Append('"');
                    if (a.Noise.Length > 0) sb.Append(", \"noise\": \"").Append(Esc(a.Noise)).Append('"');
                    sb.Append('}');
                }
                sb.Append(r.Anomalies.Count > 0 ? "\n  ]" : "]");
                if (r.Human != null) WriteHuman(sb, r.Human);
                sb.Append('}');
            }
            sb.Append("\n ]\n}\n");
            try { File.WriteAllText(Path.Combine(dir, "bot.json"), sb.ToString()); } catch (Exception e) { Debug.LogWarning("BotRun: bot.json not written: " + e.Message); }
        }
    }
}
