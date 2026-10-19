using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Suborned.Play;
using Suborned.Sim;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Path = System.IO.Path;
using Target = Suborned.Play.HandoverHost.Target;

namespace Suborned.World.Play
{
    /// <summary>The action panel never faults (Sky 2026-10-19: "engine fault in panel: FormatException: no person in the role 'patrol'"). Plays route
    /// scripts through the live host (the Editor's play mode, the generated scene) and, at 0:30, every 90 s, the first tick of each moment (the
    /// patrol away, a body down, a response unit out, Vale with the Enforcer, the final fight and 5 s into it), opens the panel and prices the hover
    /// (IntentFor, each verb slot picked and none) on every target kind: each cast person, the patrol, each camera, the rivals, the response unit,
    /// both cars, the door, the cabinet, each agent, three passers-by, every body, hostile, unit and stash, and whatever else is drawn; for each
    /// agent alone and all four. Any fault the host's Guard catches is a miss.
    ///   Menu: Suborned/Play/Panel Guard (enters play mode if needed) → out/play-guard/panel-guard.json
    ///   Test: Suborned.Play.Tests.PanelGuardTests (EditMode, enters play mode)
    ///   Live: eval `Suborned.World.Play.PanelGuard.Report(Suborned.World.Play.PanelGuard.Sweep())` while the Handover plays.</summary>
    public static class PanelGuard
    {
        public sealed class Miss { public string Route = "", At = "", Moment = "", Target = "", Selection = "", Where = "", Fault = ""; }
        public sealed class Result { public readonly List<Miss> Misses = new(); public readonly SortedDictionary<string, int> Kinds = new(); public int Panels, Hovers; public readonly List<string> Moments = new(); public double Ms; }

        /// <summary>The routes swept: the front door with a kill, a body and the patrol rerouted; the loud fight; route B (turn, listen, take);
        /// route A (a turned contact called).</summary>
        public static readonly string[] Routes = { "route-g-kill-board", "loud-fight", "route-b-board", "route-a-board" };
        /// <summary>The target kinds every sweep must have opened the panel on at least once.</summary>
        public static readonly string[] MustCover = { "person:waitress", "person:driver", "person:guard", "person:councillor", "patrol", "camera", "rivals", "security", "car:official", "car:getaway", "door", "cabinet", "crowd", "body", "op" };

        static string Out => Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "play-guard");
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("Suborned/Play/Panel Guard (every target, every selection)")]
        static void Menu()
        {
            if (!EditorApplication.isPlaying) { pending = true; EditorApplication.EnterPlaymode(); return; }
            Report(Sweep());
        }

        static bool pending;
        [InitializeOnLoadMethod]
        static void Hook() => EditorApplication.playModeStateChanged += s => { if (s == PlayModeStateChange.EnteredPlayMode && pending) { pending = false; EditorApplication.delayCall += () => Report(Sweep()); } };

        /// <summary>Writes the result to out/play-guard/panel-guard.json and the console; returns the one-line verdict.</summary>
        public static string Report(Result r)
        {
            var missing = Missing(r);
            var line = $"Panel guard: {r.Misses.Count} faults over {r.Panels} panels and {r.Hovers} hovers ({r.Moments.Count} moments, {r.Ms / 1000:F1} s); kinds {string.Join(", ", r.Kinds.Select(k => $"{k.Key} {k.Value}"))}{(missing.Count > 0 ? $"; NOT COVERED: {string.Join(", ", missing)}" : "")}";
            Directory.CreateDirectory(Out);
            var json = "{\n  \"verdict\": " + Q(r.Misses.Count == 0 && missing.Count == 0 ? "pass" : "fail") + ",\n  \"panels\": " + r.Panels + ", \"hovers\": " + r.Hovers + ",\n  \"moments\": [" + string.Join(", ", r.Moments.Select(Q)) + "],\n  \"notCovered\": [" + string.Join(", ", missing.Select(Q)) + "],\n  \"misses\": [\n"
                + string.Join(",\n", r.Misses.Select(m => $"    {{ \"route\": {Q(m.Route)}, \"at\": {Q(m.At)}, \"moment\": {Q(m.Moment)}, \"target\": {Q(m.Target)}, \"selection\": {Q(m.Selection)}, \"where\": {Q(m.Where)}, \"fault\": {Q(m.Fault)} }}")) + "\n  ]\n}\n";
            File.WriteAllText(Path.Combine(Out, "panel-guard.json"), json);
            if (r.Misses.Count == 0 && missing.Count == 0) Debug.Log(line);
            else Debug.LogError(line + "\n" + string.Join("\n", r.Misses.Take(40).Select(m => $"{m.Route} {m.At} ({m.Moment}) {m.Target} [{m.Selection}] {m.Where}: {m.Fault}")));
            return line;
        }

        public static List<string> Missing(Result r) => MustCover.Where(k => !r.Kinds.ContainsKey(k)).ToList();
        static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";

        /// <summary>Plays each route through the host in play mode and sweeps the panel at its moments. The host restarts (Begin) per route and
        /// is left paused at the last route's end.</summary>
        public static Result Sweep(string[] routes = null)
        {
            var h = UnityEngine.Object.FindAnyObjectByType<HandoverHost>();
            if (h == null) throw new InvalidOperationException("Panel guard: no HandoverHost: play the Handover scene first (Suborned/Play/Play the Handover)");
            var r = new Result(); var sw = System.Diagnostics.Stopwatch.StartNew();
            var step = typeof(HandoverHost).GetMethod("StepOnce", Any) ?? throw new MissingMethodException("HandoverHost.StepOnce");
            foreach (var id in routes ?? Routes)
            {
                var path = Path.Combine(HarbourWorldBuilder.RepoRoot, "contracts", "missions", "handover", "routes", id + ".json");
                var script = Script.Parse(File.ReadAllText(path));
                h.Begin(); h.SetPaused(true); Clear(h);
                var g = h.Game; int until = script.Until > 0 ? script.Until : Game.T(g.S.Over);
                var done = new HashSet<string>(); int next = 0; int fightAt = -1;
                while (!g.Over && g.Tick < until)
                {
                    while (next < script.Orders.Count && script.Orders[next].Tick <= g.Tick)
                    {
                        var o = script.Orders[next++];
                        h.Issue(new Order { Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run, Stance = o.Stance, Force = o.Force });
                        Clear(h);
                    }
                    if (g.Over) break;
                    if (g.FightOn && fightAt < 0) fightAt = g.Tick;
                    foreach (var m in Moments(g, fightAt)) if (done.Add(m)) { h.RunTo(g.Tick); SweepNow(h, r, id, m); }
                    if (!(bool)step.Invoke(h, null)) { Clear(h); }
                }
            }
            h.SetPaused(true); h.ClosePanel();
            sw.Stop(); r.Ms = sw.Elapsed.TotalMilliseconds;
            return r;
        }

        /// <summary>The moments due now (each sweeps once per route).</summary>
        static IEnumerable<string> Moments(Game g, int fightAt)
        {
            int t = g.Tick, hz = Game.Hz;
            if (t >= 30 * hz) yield return "0:30";
            for (int s = 90; s * hz <= t; s += 90) yield return "mid " + HandoverHost.MissionClock(s * hz);
            if (g.PatrolAwayUntil > t) yield return "patrol away";
            if (g.Bodies.Count > 0) yield return "body";
            if (g.Units.Exists(u => u.St is not ("gone" or "wait"))) yield return "unit";
            if (g.Vale == ValeAt.WithEnforcer) yield return "vale out";
            if (fightAt >= 0) yield return "final fight";
            if (fightAt >= 0 && t >= fightAt + 5 * hz) yield return "final fight +5 s";
        }

        static readonly (string name, OpId[] ids)[] Selections =
        {
            ("scout", new[] { OpId.Scout }), ("fixer", new[] { OpId.Fixer }), ("tech", new[] { OpId.Tech }), ("enforcer", new[] { OpId.Enforcer }),
            ("all four", new[] { OpId.Scout, OpId.Fixer, OpId.Tech, OpId.Enforcer }),
        };

        /// <summary>Every target now, by kind, for every selection: the panel opened, and the hover priced with no verb picked and each slot.</summary>
        public static void SweepNow(HandoverHost h, Result r, string route, string moment)
        {
            r.Moments.Add($"{route} {HandoverHost.MissionClock(h.Game.Tick)} {moment}");
            var intentFor = typeof(HandoverHost).GetMethod("IntentFor", Any) ?? throw new MissingMethodException("HandoverHost.IntentFor");
            var picked = typeof(HandoverHost).GetProperty("Picked", Any);
            var keepSel = (bool[])h.Selected.Clone();
            try
            {
                foreach (var t in Targets(h))
                {
                    string kind = t.Kind is "person" or "car" ? $"{t.Kind}:{t.Key}" : t.Kind;
                    r.Kinds[kind] = r.Kinds.TryGetValue(kind, out var n) ? n + 1 : 1;
                    foreach (var (name, ids) in Selections)
                    {
                        h.Select(ids[0], false); for (int i = 1; i < ids.Length; i++) h.Select(ids[i], true);
                        Try(h, r, route, moment, t, name, "panel", () => h.OpenPanel(t, new Vector2(640, 360))); r.Panels++;
                        h.ClosePanel();
                        int slots = HandoverHost.Slots[(int)h.Agent].Length;
                        for (int s = -1; s < slots; s++)
                        {
                            picked.SetValue(h, s);
                            Try(h, r, route, moment, t, name, s < 0 ? "hover" : $"hover, slot {s + 1} picked", () => intentFor.Invoke(h, new object[] { t })); r.Hovers++;
                        }
                        picked.SetValue(h, -1);
                    }
                }
            }
            finally
            {
                h.ClosePanel(); picked.SetValue(h, -1);
                int first = Array.IndexOf(keepSel, true); if (first >= 0) { h.Select((OpId)first, false); for (int k = first + 1; k < 4; k++) if (keepSel[k]) h.Select((OpId)k, true); }
            }
        }

        static void Try(HandoverHost h, Result r, string route, string moment, Target t, string sel, string where, Action a)
        {
            Exception e = null;
            try { a(); } catch (TargetInvocationException x) { e = x.InnerException ?? x; } catch (Exception x) { e = x; }
            e ??= h.Fault; // the host's Guard caught it: what the player sees as "engine fault in …"
            if (e == null) return;
            var frames = (e.StackTrace ?? "").Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).Take(4);
            r.Misses.Add(new Miss { Route = route, At = HandoverHost.MissionClock(h.Game.Tick), Moment = moment, Target = $"{t.Kind}/{t.Key}{(t.Id.Length > 0 && t.Id != t.Key ? $" ({t.Id})" : "")}", Selection = sel, Where = where, Fault = $"{e.GetType().Name}: {e.Message} @ {string.Join(" < ", frames)}" });
            Clear(h);
        }

        /// <summary>Clears a caught fault so the sweep goes on (the host stops on one: Redline E1).</summary>
        static void Clear(HandoverHost h) { if (h.Fault != null) typeof(HandoverHost).GetProperty("Fault", Any)!.SetValue(h, null); }

        /// <summary>Every target kind the panel can open on now: the fixed ones whether drawn or not, the engine's lists, and whatever is drawn.</summary>
        public static List<Target> Targets(HandoverHost h)
        {
            var g = h.Game; var l = new List<Target>();
            void Add(string kind, string key, string label, string id = "") { if (!l.Exists(x => x.Kind == kind && x.Key == key)) l.Add(new Target { Kind = kind, Key = key, Label = label, Id = id }); }
            foreach (var p in g.People) if (p.Role.Length > 0) Add("person", p.Role, p.Role, p.Role);
            Add("patrol", "patrol", "the patrol");
            foreach (var c in g.S.Cameras) Add("camera", c.Id, $"the {c.Id} camera");
            Add("rivals", "rivals", "the rival crew"); Add("security", "security", "the response unit");
            Add("car", "official", "the official car"); Add("car", "getaway", "the getaway car");
            Add("door", "door", "the public door"); Add("cabinet", "cabinet", "the control cabinet");
            foreach (var op in new[] { "scout", "fixer", "tech", "enforcer" }) Add("op", op, op);
            int crowd = 0; for (int i = 0; i < g.Walkers.Count && crowd < 3; i++) if (g.Walkers[i].P is Person cp && !cp.Down) { Add("crowd", "crowd/" + i, "a passer-by", cp.Id); crowd++; }
            foreach (var b in g.Bodies) { var k = HostViews.KeyOf(b.P); Add("body", k, "a body", k); }
            foreach (var x in g.Hostiles) if (!x.Down) Add("hostile", x.Id, x.Id.StartsWith("rival") ? "a rival" : "an officer");
            foreach (var u in g.Units) if (u.St is not ("gone" or "wait")) Add("unit", u.Id, u.Robot ? "a response robot" : "an officer");
            foreach (var s in g.S.Stashes) Add("stash", s.Id, s.Id, s.Id);
            foreach (var p in h.Pickables()) Add(p.T.Kind, p.T.Key, p.T.Label, p.T.Id);
            return l;
        }
    }
}
