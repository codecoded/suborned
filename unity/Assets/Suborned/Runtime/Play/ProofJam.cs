using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Brief 098 step 1b, the jam's player probe (Latch; `-jamProbe &lt;dir&gt; -jamSession &lt;script.json&gt;`, a player only), after ProofTraffic. A saved run whose Go is
    /// refused car-boxed by a passing car (briefs/098/latch/jam.script.json: route B on seed 0 with 16 cars; the Scout stays out, shoots the car in the way, runs back and
    /// boards, and the Go is taken), played through the host's own orders as the shot rig gives them (seed and counts from the session). It checks, Sky's rule ("shot cars
    /// can get past, others block") as the player reads it:
    ///  · the refused Go's pop names the passing car plainly (its colour) and its long line says shooting it clears the way (HostTrafficBoxed), and the car named pulses;
    ///  · its hover says it boxes our car in (an agent aboard selected: no way is offered on it, so the pill is its state);
    ///  · once shot it boxes nobody in, and its hover says so (wrecked or abandoned);
    ///  · the Go is taken, the engine pushes the wreck off the way on the Go's tick, and the view draws it sliding there as our car leaves (HostViews.TrafficShove): it
    ///    waits for the car, never moves more than a short step in a frame, and ends where the engine has it.
    /// A picture at each step. Writes &lt;dir&gt;/jam-probe.json and quits; its own boot, so GameBoot's proof sections are untouched.</summary>
    public sealed class ProofJam : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor || GameBoot.ArgValue("-jamProbe") is not string d || GameBoot.ArgValue("-jamSession") is not string sp) return;
            var go = new GameObject("jam-probe"); DontDestroyOnLoad(go); var p = go.AddComponent<ProofJam>(); p.dir = Path.GetFullPath(d); p.session = Path.GetFullPath(sp);
        }

        string dir, session; readonly List<(string what, bool ok, string detail)> checks = new(); readonly List<string> shots = new(), notes = new();
        void Check(string what, bool ok, string detail = "") { checks.Add((what, ok, detail)); Debug.Log($"JamProbe: {(ok ? "ok" : "FAIL")} {what} {detail}"); }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 90f) { Check("a host", false, "none after 90 s"); Write(); yield break; } yield return null; }
            yield return CaptureSize.Apply(1920, 1080);
            yield return new WaitForSecondsRealtime(3f);
            Script sc; try { sc = Script.Parse(File.ReadAllText(session)); } catch (Exception e) { Check("the session", false, e.Message); Write(); yield break; }
            HandoverHost.SeedPin = sc.Start.Seed; HandoverHost.CountsPin = new SortedDictionary<string, int>(sc.Start.Counts, StringComparer.Ordinal);
            h.controls.conversations = ConversationScenes.Never; h.Begin(); h.StartClock(); h.SetPaused(true); yield return null;
            var g = h.Game; var orders = sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList(); int oi = 0;
            Check("the session plays its start", g.Seed == sc.Start.Seed && g.TrafficOn && g.Start.Counts.Count == sc.Start.Counts.Count, $"seed {g.Seed}, counts {string.Join(",", g.Start.Counts.Select(kv => kv.Key + "=" + kv.Value))}");
            // the session's orders up to a tick, as the shot rig gives them (the host's own Issue: a refusal pops as a click's)
            IEnumerator To(int tick) { while (oi < orders.Count && orders[oi].Tick <= tick && !g.Over) { var o = orders[oi++]; if (o.Tick > g.Tick) { h.RunTo(o.Tick); yield return null; } if (g.Over) break; var c = new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run }; if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c); } if (tick > g.Tick && !g.Over) { h.RunTo(tick); yield return null; } }

            // 1 · the first Go refused: car-boxed, naming a passing car; the pop names it plainly and says shooting it clears the way
            int goAt = orders.First(o => o.Do == "go" && string.IsNullOrEmpty(o.Target)).Tick;
            yield return To(goAt); yield return null;
            var refused = g.Orders.LastOrDefault(x => x.order.Do == "go"); var lock0 = h.GoLock(); string boxer = (lock0.ids ?? "").Split(',').FirstOrDefault(x => x.StartsWith("car:t-")) ?? "";
            Check("the Go is refused car-boxed by a passing car", !refused.ok && refused.code == "car-boxed" && boxer.Length > 0, $"{Report.Clock(g.Tick)}: {refused.code} [{lock0.ids}] {refused.why}");
            if (boxer.Length == 0) { Write(); yield break; }
            string name = h.TrafficNameOf(boxer) ?? ""; var pop = h.Pops.LastOrDefault(); string bare = name.StartsWith("the ") ? name.Substring(4) : name;
            Check("the pop names the car plainly", pop != null && pop.Text == h.BoxedShort(lock0) && bare.Length > 0 && pop.Text.EndsWith(bare) && !pop.Text.Contains("car:"), $"pop '{pop?.Text}' (the car {boxer}: {name}, paint {h.Views.TrafficPaintOf(boxer)})");
            Check("its long line names it and says shooting it clears the way (HostTrafficBoxed's line)", pop != null && pop.Long == h.BoxedLine(lock0) && pop.Long.Contains(name) && !pop.Long.Contains("car:"), $"'{pop?.Long}'"); // the words themselves: HostLogic.Jam098 (Fable's to change)
            bool pulsed = h.Views.TrafficDrawnAt(boxer, out var ba) && h.Pulses.Exists(p => (p.world - ba).sqrMagnitude < 1f);
            Check("the car named pulses where it stands", pulsed, $"{h.Pulses.Count} pulses");
            h.LookAt(h.GetawayWorld); for (int i = 0; i < 4; i++) yield return null; // our car in the middle of the view: the pop stands over it (above its speech)
            var mf = HudToolkit.ManifestNow() ?? ""; bool drawn = pop != null && mf.Contains("\"kind\": \"pop\"") && mf.Contains(pop.Text);
            Check("the pop is drawn over our car", drawn, drawn ? $"'{pop.Text}' on screen" : "no pop in the HUD's manifest");
            yield return Shot(h, "jam-1-refused");

            // 2 · its hover (an agent aboard selected: no way on it), a few seconds on
            yield return To(goAt + 40);
            int aboard = Array.FindIndex(g.Ops, o => o.InCar && !o.InLent);
            if (aboard >= 0 && h.Views.TrafficDrawnAt(boxer, out var bh))
            {
                h.Select((OpId)aboard, false); h.LookAt(bh); yield return null; HandoverHost.PointerWorld = bh + Vector3.up * 0.8f;
                for (int i = 0; i < 8; i++) yield return null; yield return new WaitForSecondsRealtime(0.3f);
                var t = h.Hover; string pill = t != null ? h.TrafficPill(t) : "";
                Check("its hover says it boxes our car in", t != null && "car:" + t.Key == boxer && pill.Contains("boxing our car in") && HudToolkit.PillWords == pill, $"{(OpId)aboard} selected: pill '{HudToolkit.PillWords}' ({HudToolkit.PillState}); words '{pill}'");
                yield return Shot(h, "jam-2-hover"); HandoverHost.PointerWorld = null;
                // 2b · the rival car, held behind it (Game.MissionHoldNow): its hover says why, the passing car named plainly
                var rv = h.Views.Find("car/rivals"); string held = h.MissionHeldWords("car:rivals");
                if (rv && rv.gameObject.activeInHierarchy && held.Length > 0)
                {
                    h.LookAt(rv.position); yield return null; HandoverHost.PointerWorld = rv.position + Vector3.up * 0.8f;
                    for (int i = 0; i < 8; i++) yield return null; yield return new WaitForSecondsRealtime(0.3f);
                    var rt = h.Hover; string rp = rt != null ? h.TrafficPill(rt) : "";
                    bool info = HudToolkit.PillState == "info"; // the held words are the hover's pill only when no way is offered on the car (a lock or an offer takes the pill first)
                    Check("a mission car held behind traffic: the engine's words (MissionHoldNow), the passing car named plainly", held.StartsWith("held ") && held.Contains(name) && !held.Contains("car:") && rp.EndsWith(held) && (!info || HudToolkit.PillWords == rp),
                        $"'{held}'; the hover on {(rt == null ? "nothing" : rt.Kind + " " + rt.Key)} with the {(OpId)aboard} selected shows '{HudToolkit.PillWords}' ({HudToolkit.PillState}){(info ? "" : ": a way is offered on it, so its pill is that way's")}");
                    yield return Shot(h, "jam-2b-rival-held"); HandoverHost.PointerWorld = null;
                }
                else notes.Add($"the rival car was not held behind traffic at {Report.Clock(g.Tick)} ('{held}'), or not drawn");
            }
            else Check("its hover says it boxes our car in", false, $"nobody aboard ({aboard}) or the car not drawn");

            // 3 · shot: it boxes nobody in, and says so
            int shotAt = orders.First(o => o.Tick > goAt && o.Do == "move").Tick;
            yield return To(shotAt); yield return null;
            var sv = h.Views.TrafficOf(boxer); var boxers = g.GetawayBoxers();
            Check("shot, it boxes nobody in", sv != null && sv.Shot && sv.Boxes.Length == 0 && !boxers.Contains(boxer), $"{Report.Clock(g.Tick)}: {sv?.State} {sv?.Mode}, boxers [{string.Join(",", boxers)}]");
            string wordsShot = h.TrafficWords(boxer);
            Check("its hover says it is wrecked or abandoned", wordsShot.StartsWith("wrecked") || wordsShot.StartsWith("abandoned"), $"'{wordsShot}'");
            if (h.Views.TrafficDrawnAt(boxer, out var bs)) { h.LookAt(bs); yield return null; yield return Shot(h, "jam-3-shot"); }

            // 4 · the Go: taken, the wreck pushed off the way on its tick, drawn sliding as our car leaves
            int go2 = orders.Last(o => o.Do == "go").Tick;
            yield return To(go2 - 1); // the tick before the Go: where the wreck stands (the rival car may have pushed it on its way in) and is drawn
            h.Views.TrafficDrawnAt(boxer, out var before); int pushedBefore = h.Views.TrafficOf(boxer)?.Shoved ?? 0;
            yield return To(go2);
            var gv = h.Views.TrafficOf(boxer); bool sliding = h.Views.WreckSliding(boxer);
            Check("the Go is taken and the engine pushes the wreck off the way", g.Over && g.Result == Outcome.Success && gv != null && gv.ShovedBy == "car:getaway" && gv.Shoved > pushedBefore, $"{g.Result} at {Report.Clock(g.Tick)}; pushed {pushedBefore} → {gv?.Shoved} cm by {gv?.ShovedBy}");
            var engineAt = gv != null ? h.frame.World(gv.At) : before; float jump = Flat(engineAt - before);
            Check("on the Go's tick the view has not jumped there", sliding && h.Views.TrafficDrawnAt(boxer, out var now0) && Flat(now0 - before) < 0.05f, $"the engine moved it {jump:0.00} m; drawn {(h.Views.TrafficDrawnAt(boxer, out var n0) ? Flat(n0 - before) : -1):0.00} m from where it stood; sliding {sliding}");
            h.LookAt(Vector3.Lerp(h.GetawayWorld, before, 0.6f)); h.SetPaused(false);
            float tS = Time.realtimeSinceStartup, movedAt = -1f, carAtMove = -1f; int frames = 0; var path = new List<string>();
            while (Time.realtimeSinceStartup - tS < 12f && h.Views.WreckSliding(boxer))
            {
                yield return null; frames++;
                if (h.Views.TrafficDrawnAt(boxer, out var p)) { if (movedAt < 0 && Flat(p - before) > 0.02f) { movedAt = Time.realtimeSinceStartup - tS; carAtMove = h.Views.GetawayLeftM; } if (frames % 6 == 0) path.Add($"{Time.realtimeSinceStartup - tS:0.00}s {Flat(p - before):0.00}m car {h.Views.GetawayLeftM:0.0}m"); }
                if (frames == 20 || frames == 45) yield return Shot(h, $"jam-4-leaving-{frames}");
            }
            yield return null; h.Views.TrafficDrawnAt(boxer, out var after);
            float left = Flat(after - engineAt), step = h.Views.WreckSlideMaxStepM;
            Check("drawn sliding, not jumping: it waits for our car, steps a little each frame and ends where the engine has it", movedAt > 0.1f && carAtMove > 1f && !h.Views.WreckSliding(boxer) && left < 0.05f && step < 0.6f && h.Views.WreckSlides >= 1,
                $"it began to move {movedAt:0.00} s after the Go, our car {carAtMove:0.0} m along its way out; {jump:0.00} m in {frames} frames, the most in a frame {step:0.00} m, {left:0.000} m from the engine's place at the end (our car {h.Views.GetawayLeftM:0.0} m along); slides {h.Views.WreckSlides}");
            notes.Add("the slide: " + string.Join(", ", path));
            yield return Shot(h, "jam-5-after");
            try { notes.Add("the player's session: " + h.SaveSessionTo(Path.Combine(dir, "jam-player.script.json"))); } catch (Exception e) { notes.Add("the player's session was not saved: " + e.Message); } // replayed headless (suborned-sim play) beside the probe
            Write();
        }

        static float Flat(Vector3 d) { d.y = 0f; return d.magnitude; }

        IEnumerator Shot(HandoverHost h, string name)
        {
            while (HudToolkit.ShotBusy) yield return null;
            var r = HudToolkit.Shoot(Path.Combine(dir, name), "rest"); yield return null; while (HudToolkit.ShotBusy) yield return null;
            shots.Add($"{name}.png ({r})");
        }

        void Write()
        {
            bool ok = checks.Count > 0 && checks.TrueForAll(c => c.ok);
            var sb = new StringBuilder("{\n \"schema\": \"suborned.jam-probe/1\", \"ok\": ").Append(ok ? "true" : "false").Append(", \"session\": ").Append(Q(session)).Append(",\n \"checks\": [\n");
            for (int i = 0; i < checks.Count; i++) sb.Append("  {\"what\": ").Append(Q(checks[i].what)).Append(", \"ok\": ").Append(checks[i].ok ? "true" : "false").Append(", \"detail\": ").Append(Q(checks[i].detail)).Append(i + 1 < checks.Count ? "},\n" : "}\n");
            sb.Append(" ],\n \"notes\": [").Append(string.Join(", ", notes.ConvertAll(Q))).Append("],\n \"shots\": [").Append(string.Join(", ", shots.ConvertAll(Q))).Append("]\n}\n");
            File.WriteAllText(Path.Combine(dir, "jam-probe.json"), sb.ToString());
            Debug.Log($"JamProbe: {(ok ? "ok" : "FAILED")}, {checks.FindAll(c => c.ok).Count}/{checks.Count} checks → {dir}");
            GameBoot.Leave(ok ? 0 : 1);
        }
        static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ") + "\"";
    }
}
