using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;
using Face = Suborned.Play.HandoverHost.Face;

namespace Suborned.Play
{
    /// <summary>`-shots033 &lt;dir&gt;` (Latch, 2026-10-21; briefs 030 option A, 032 and 033): shots in the built player, each with its HUD layer,
    /// manifest and parts for tools/hud-budget, and the layout audit (HudToolkit.AuditText) after each. The HUD with no plate at rest; the
    /// Tab strip; Pay armed on the Fixer over Inez (the cursor's glyph, the portrait's); the portraits forced through the nine faces (two
    /// shots, 4 + 4, and the ninth); the noticed face live (the Enforcer idling in front of the entrance camera); the fight faces live on route
    /// G's way out (under fire, hurt, badly hurt, downed: whichever the run reaches); the conversation scene on route A (Listen, Help, Call)
    /// with the clock and the view checked before and after; then route A to 3:00 with scenes Always, First time and Never, their final
    /// checkpoint hashes compared. Orders go in as a click gives them, so each run is an ordinary session; shots.json logs it all.</summary>
    public sealed class Shots033 : MonoBehaviour
    {
        public string Dir = "";
        readonly List<string> log = new(), audits = new(), hashes = new();

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            HandoverHost h = null; float t0 = Time.realtimeSinceStartup;
            while ((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) { if (Time.realtimeSinceStartup - t0 > 60f) { Write("no host"); Application.Quit(2); yield break; } yield return null; }
            yield return new WaitForSecondsRealtime(3f);
            var keep = (h.controls.actionPlate, h.controls.conversations);
            h.controls.actionPlate = false; h.controls.conversations = ConversationScenes.Never; // the plate off (Sky's A); no scenes until the scene shots
            h.SetNight(false); h.IntelOpen = false; yield return null;
            h.StartClock(); h.SetPaused(true); yield return new WaitForSecondsRealtime(0.6f);
            var cam = h.View; var g = h.Game;
            bool scenesOnly = GameBoot.ArgValue("-shots033-only") == "scenes"; // Latch 2026-10-21: only the conversation scenes (9), for a framing re-shoot
            if (!scenesOnly) {
            // 1 · at rest, no plate (the Scout selected, nothing armed)
            HandoverHost.PointerOverride = new Vector2(Screen.width * 0.62f, Screen.height * 0.42f); h.ClearHover();
            yield return Shot(h, "01-rest-no-plate", "rest");
            // 2 · the Fixer selected, Tab: the strip beside his portrait for 2.5 s
            h.Select(OpId.Fixer, false); h.ShowVerbStrip(); yield return new WaitForSecondsRealtime(0.2f); h.ShowVerbStrip();
            yield return Shot(h, "02-tab-strip", "active");
            // 3 · Pay armed (Shift+2): the glyph rides the cursor and sits on the portrait; the hover chip over Inez
            var plate = h.PlateOf(OpId.Fixer); int pay = Array.FindIndex(plate, x => x.Id == "pay"); h.Pick(pay >= 0 ? pay : 1);
            if (h.Views.Find("waitress") is Transform inez) { h.LookAt(inez.position); yield return null; yield return null; var sp = cam.WorldToScreenPoint(inez.position + Vector3.up * 1.0f); HandoverHost.PointerOverride = new Vector2(sp.x + 4, sp.y); }
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot(h, "03-armed-pay", "active");
            h.Cancel(); HandoverHost.PointerOverride = new Vector2(Screen.width * 0.62f, Screen.height * 0.42f); h.ClearHover(); h.Select(OpId.Scout, false);
            // 4, 5 · the nine faces, forced (the cue layer drawn by state), 4 + 4 + the ninth
            h.LookAt(h.frame.World(HandoverHost.OpeningFocus));
            foreach (var (name, set) in new[] { ("04-faces-calm-noticed-underfire-hurt", new[] { Face.Calm, Face.Noticed, Face.UnderFire, Face.Hurt }), ("05-faces-firing-badlyhurt-downed-success", new[] { Face.Firing, Face.BadlyHurt, Face.Downed, Face.Success }), ("06-faces-focused", new[] { Face.Focused, Face.Focused, Face.Focused, Face.Focused }) })
            {
                for (int k = 0; k < 4; k++) HandoverHost.FaceForce[k] = set[k];
                yield return new WaitForSecondsRealtime(0.45f); // the cross-fade done; the success sweep is a beat, the ticks stay
                yield return Shot(h, name, "rest");
                log.Add($"{name}: forced {string.Join(", ", set)}");
            }
            for (int k = 0; k < 4; k++) HandoverHost.FaceForce[k] = null;
            // 7 · noticed, live: the Enforcer idles in front of the entrance camera until it notices him (brief 031's spot)
            {
                var c = g.S.Cameras.Find(x => x.Id == "entrance"); const string camW = "camera:entrance";
                var spot = c == null ? null : Spot(g, c.At, c.Facing, 400, "enforcer", p => g.InSight(camW, p, false) && !g.InSight("guard", p, false) && !g.InSight("patrol", p, false));
                if (spot.HasValue)
                {
                    h.Issue(new Order { Op = "enforcer", Do = "move", Point = spot.Value }); Arrive(h, 3, 1200);
                    h.Select(OpId.Enforcer, false); h.TurnSnap(0); h.LookAt(h.Views.Find("camera/entrance").position);
                    int cap = 0; while (h.FaceOf(3) != Face.Noticed && cap++ < 900 && !g.Over) { h.RunTo(g.Tick + 1); yield return null; }
                    log.Add($"noticed live: face {h.FaceOf(3)}, eye stage {h.EyeStage(3)} at {Suborned.Sim.Report.Clock(g.Tick)}");
                    yield return Shot(h, "07-live-noticed", "rest");
                }
                else log.Add("noticed live: no spot in front of the entrance camera");
            }
            // 8 · the fight faces, live: the recorded fight routes (contracts/missions/handover/routes, -routes): loud-fight (the whole cell fights it
            // out), then fight-loud-enforcer-down (the Enforcer carries Vale into the officers); each face shot the first time an agent shows it
            var want = new List<Face> { Face.UnderFire, Face.Firing, Face.Hurt, Face.BadlyHurt, Face.Downed };
            foreach (var route in new[] { "loud-fight", "fight-loud-enforcer-down" })
            {
                if (want.Count == 0) break;
                var orders = Route(route); if (orders == null) { log.Add($"fight faces: no {route}.json under {Routes}"); continue; }
                h.Begin(); yield return null; h.StartClock(); h.SetPaused(true); h.IntelOpen = false; g = h.Game; yield return null;
                int oi = 0, n = 0;
                while (!g.Over && g.Tick < 5100 && want.Count > 0)
                {
                    while (oi < orders.Count && orders[oi].Tick <= g.Tick) Give(h, orders[oi++]);
                    h.RunTo(g.Tick + 1); if (++n % 2 != 0) continue;
                    yield return null;
                    for (int k = 0; k < 4 && want.Count > 0; k++)
                    {
                        var f = h.FaceOf(k); if (!want.Contains(f)) continue; want.Remove(f);
                        if (h.Views.Find("op/" + Suborned.Sim.Op.Name((OpId)k)) is Transform at) h.LookAt(at.position);
                        h.Select((OpId)k, false); yield return null;
                        log.Add($"live {f} ({route}): the {Suborned.Sim.Op.Name((OpId)k)} at {Suborned.Sim.Report.Clock(g.Tick)}, health {g.Ops[k].Gun.Health}/{g.Ops[k].Gun.Max}, faces {string.Join(",", Enumerable.Range(0, 4).Select(i => h.FaceOf(i)))}");
                        yield return Shot(h, $"08-live-{f.ToString().ToLowerInvariant()}", "rest", 0.05f);
                    }
                }
                log.Add($"fight faces ({route}): ran to {Suborned.Sim.Report.Clock(g.Tick)}, over {g.Over} ({g.Result}), fight {g.FightOn}");
            }
            log.Add($"fight faces not reached live: {(want.Count == 0 ? "none" : string.Join(", ", want))}");
            }
            // 9 · the conversation scene on route A (contracts/missions/handover/routes/route-a.json): the Scout's Listen, the Fixer's Help (twice),
            // the Call; the clock and the view before and after
            var ra = Route("route-a");
            if (ra == null) log.Add($"scenes: no route-a.json under {Routes}");
            else
            {
                h.controls.conversations = ConversationScenes.Always;
                h.Begin(); yield return null; h.StartClock(); h.SetPaused(true); h.IntelOpen = false; g = h.Game; yield return null;
                int shots = 0, oi = 0;
                while (!g.Over && g.Tick < 1800)
                {
                    while (oi < ra.Count && ra[oi].Tick <= g.Tick) { Give(h, ra[oi++]); if (h.ConvoPending) yield return Scene(h, $"scene-{++shots}"); }
                    h.RunTo(g.Tick + 1); if (h.ConvoPending) yield return Scene(h, $"scene-{++shots}");
                }
                // 10 · the same orders with scenes Always, First time and Never: the final checkpoint hash and the history, each to 3:00
                foreach (var mode in scenesOnly ? new ConversationScenes[0] : new[] { ConversationScenes.Always, ConversationScenes.FirstTime, ConversationScenes.Never })
                {
                    h.controls.conversations = mode; h.Begin(); yield return null; h.StartClock(); h.SetPaused(true); g = h.Game; int played0 = h.ConvoCount;
                    var orders = Route("route-a"); oi = 0;
                    while (!g.Over && g.Tick < 1800)
                    {
                        while (oi < orders.Count && orders[oi].Tick <= g.Tick) { Give(h, orders[oi++]); if (h.ConvoPending) { yield return null; yield return null; h.ConvoEndNow(); } }
                        h.RunTo(g.Tick + 1); if (h.ConvoPending) { yield return null; yield return null; h.ConvoEndNow(); }
                    }
                    var cp = g.Checkpoints.Count > 0 ? g.Checkpoints[^1] : (0, 0UL);
                    hashes.Add($"{{\"mode\": \"{mode}\", \"tick\": {g.Tick}, \"scenes\": {h.ConvoCount - played0}, \"checkpoint\": \"{cp.Item1}:{cp.Item2:x16}\", \"log\": {g.Log.Count}, \"orders\": {g.Orders.Count}}}");
                    log.Add($"hash {mode}: tick {g.Tick}, {h.ConvoCount - played0} scenes, checkpoint {cp.Item1}:{cp.Item2:x16}, log {g.Log.Count} lines, {g.Orders.Count} orders");
                }
            }
            h.controls.actionPlate = keep.actionPlate; h.controls.conversations = keep.conversations; HandoverHost.PointerOverride = null;
            Write("ok"); yield return new WaitForSecondsRealtime(0.5f); Application.Quit(0);
        }

        /// <summary>A scene the last act asked for: the next frames start it; a shot on its first line, then Skip; the clock and the view checked.</summary>
        IEnumerator Scene(HandoverHost h, string name)
        {
            var cam = h.View; var pos = cam.transform.position; var rot = cam.transform.rotation; float size = cam.orthographicSize; bool ortho = cam.orthographic; int tick = h.Game.Tick;
            for (int i = 0; i < 30 && !h.ConvoOn && h.ConvoPending; i++) yield return null;
            if (!h.ConvoOn) { log.Add($"{name}: no scene ({h.ConvoLast})"); yield break; }
            var c = h.ConvoNow; string what = $"{c.Verb} {c.Target}, {c.Lines.Count} beats: {string.Join(" | ", c.Lines.Select(l => l.speaker + ": " + l.text))}";
            yield return new WaitForSecondsRealtime(0.55f);
            yield return Shot(h, $"09-{name}-{c.Verb}", "rest", 0f);
            h.ConvoSkip(); for (int i = 0; i < 40 && h.ConvoOn; i++) yield return null;
            bool same = cam.transform.position == pos && cam.transform.rotation == rot && cam.orthographicSize == size && cam.orthographic == ortho;
            log.Add($"{name}: {what}; clock {Suborned.Sim.Report.Clock(tick)} before, {Suborned.Sim.Report.Clock(h.Game.Tick)} after; {h.ConvoLast}; view {(same ? "bit-identical" : "DIFFERENT")} after");
        }

        /// <summary>The recorded routes' folder (-routes; default the repo's contracts/missions/handover/routes beside the app's out/build/play).</summary>
        static string Routes => GameBoot.ArgValue("-routes") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "..", "..", "contracts", "missions", "handover", "routes"));
        static List<Order> Route(string name)
        {
            var f = Path.Combine(Routes, name + ".json"); if (!File.Exists(f)) return null;
            try { return Suborned.Sim.Script.Parse(File.ReadAllText(f)).Orders.OrderBy(o => o.Tick).ToList(); } catch (Exception) { return null; }
        }
        /// <summary>A recorded order as the player gives it: a person's verb as a click (its walk, ack and arrive), anything else as the engine order.</summary>
        static void Give(HandoverHost h, Order o) { if (o.Do is "watch" or "listen" or "turn" or "call" && o.Op != "cell") h.GiveScripted(o); else h.Issue(o); }

        static Order Copy(Order o) => new() { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run };

        static void Arrive(HandoverHost h, int k, int cap) { int g = 0; while ((h.Game.Ops[k].Walk != null || h.Game.Ops[k].Busy != null) && g++ < cap && !h.Game.Over) h.RunTo(h.Game.Tick + 1); }

        /// <summary>The free ground nearest `cm` out along `facing` from `from` that the agent can walk to and `ok` accepts (Shots031's).</summary>
        static Suborned.Sim.Pt? Spot(Suborned.Sim.Game g, Suborned.Sim.Pt from, Suborned.Sim.Pt facing, int cm, string who, Func<Suborned.Sim.Pt, bool> ok)
        {
            double len = Math.Sqrt((double)facing.X * facing.X + (double)facing.Y * facing.Y); if (len < 1) return null;
            var aim = new Suborned.Sim.Pt(from.X + (int)(facing.X / len * cm), from.Y + (int)(facing.Y / len * cm));
            var grid = g.S.Grid; var me = g.Ops[(int)Suborned.Sim.Op.Parse(who)].Pos; Suborned.Sim.Pt? best = null; long bd = long.MaxValue;
            for (int dx = -300; dx <= 300; dx += 25) for (int dy = -300; dy <= 300; dy += 25)
            {
                var p = new Suborned.Sim.Pt(aim.X + dx, aim.Y + dy); long d = (long)dx * dx + (long)dy * dy; if (d >= bd || !grid.Free(p) || !ok(p)) continue;
                if (grid.Route(me, p) == null) continue; best = p; bd = d;
            }
            return best;
        }

        IEnumerator Shot(HandoverHost h, string name, string state, float settle = 0.35f)
        {
            if (settle > 0f) yield return new WaitForSecondsRealtime(settle);
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(Dir, name), state));
            yield return null; while (HudToolkit.ShotBusy) yield return null;
            var bad = HudToolkit.AuditText();
            audits.Add($"{{\"shot\": \"{name}\", \"clean\": {(bad.Count == 0 ? "true" : "false")}, \"issues\": [{string.Join(", ", bad.Select(b => "\"" + Esc(b) + "\""))}]}}");
        }

        static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "'");
        void Write(string status)
        {
            var esc = string.Join(", ", log.Select(l => "\"" + Esc(l) + "\""));
            File.WriteAllText(Path.Combine(Dir, "shots.json"), $"{{\n \"schema\": \"suborned.shots-033/1\",\n \"status\": \"{status}\",\n \"screen\": \"{Screen.width}x{Screen.height}\",\n \"build\": \"{Esc(BuildStamp.Line)}\",\n \"audit\": [\n  {string.Join(",\n  ", audits)}\n ],\n \"hashes\": [{string.Join(", ", hashes)}],\n \"log\": [{esc}]\n}}\n");
        }
    }
}
