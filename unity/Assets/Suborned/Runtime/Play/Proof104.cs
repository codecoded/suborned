using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Robots;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Brief 104 (Sky: "he's awesome"), section r104 (gated): Vela's PatrolBot104 in both robot roles, in the played scene.
    ///  · roles: the patrol's view and every robot unit's view (the response dial's, unit/robot-*) draw DK_PatrolBot104 with PatrolBot driving it and the LED
    ///    face on its Screen (a `_Face` renderer RobotFace drives); the officers stay people;
    ///  · import: each part stands at its pivot (Vela's tiles/pivots.json, kit metres, 1 cm), and its import rotation is said (Vela's risk: an added axis turn);
    ///  · rolling: on its sweep the ball's roll matches the ground the robot covered (distance / 0.20 m, within 15%: the ball eases to its heading);
    ///  · the draw: the Enforcer fires on the guard; the gunfire brings the dial's robots (and breaks the patrol to it within 30 m); once a robot has its
    ///    weapon out, its hatch stands open (150 deg) and its gadget out (0.14 m), its face focused, its siren lit;
    ///  · instancing: on every robot the face sheet is the one renderer with a property block, after the faces have changed (calm, the alarm's stare,
    ///    focused); every other part keeps shared materials on a shader the GPU Resident Drawer instances (DecoKit Lit Cutaway, URP Lit), the same
    ///    material objects on every robot.
    /// Pictures at play zoom: r104-patrol (on its sweep), r104-drawn (a robot drawn, hatch open), r104-units (the response's robots).</summary>
    public static class Proof104
    {
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Shoot(string dir, string name, List<string> log)
        {
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null;
        }
        /// <summary>Vela's pivots (briefs/104/vela/patrolbot/tiles/pivots.json, as the kit and Unity have them: x across, y up, z front).</summary>
        static readonly Dictionary<string, Vector3> Pivots = new()
        {
            ["Body"] = new(0f, 0.40f, 0f), ["Ball"] = new(0f, 0.20f, 0f), ["ChestPanel"] = new(0f, 0.6657f, 0.2131f), ["Gadget"] = new(0f, 0.541f, 0.045f),
            ["ArmL"] = new(-0.2471f, 0.70f, 0f), ["ArmR"] = new(0.2471f, 0.70f, 0f), ["ForearmL"] = new(-0.3389f, 0.613f, 0.0299f), ["ForearmR"] = new(0.3389f, 0.613f, 0.0299f),
            ["Beacon"] = new(0f, 1.105f, 0f), ["BeaconLamp"] = new(0f, 1.178f, 0f), ["Screen"] = new(0f, 0.91f, 0.2374f),
        };
        static readonly HashSet<string> Instanced = new() { "DecoKit/Lit Cutaway", "DecoKit/Complex Lit Cutaway", "Universal Render Pipeline/Lit" };

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            void Check(string name, bool good, string words) { parts.Add($"\"{name}\": {{\"ok\": {B(good)}, \"words\": \"{J(words)}\"}}"); ok &= good; log.Add($"r104 {name}: {(good ? "ok" : "FAILED")}: {words}"); }
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return Frames(3); var g = h.Game; var v = h.Views;

            // roles and import
            v.Bots104.TryGetValue("patrol", out var pb);
            Check("patrolRole", pb && pb.Ready && v.CarModelName("patrol") == PatrolBot.ModelName && pb.FaceRenderer && pb.GetComponent<RobotFace>() is { Screens: 1 } && PatrolBot.Is(h.robotUnit),
                $"the patrol draws '{v.CarModelName("patrol")}' (rigged {B(pb && pb.Ready)}, face sheet {(pb && pb.FaceRenderer ? pb.FaceRenderer.name : "none")}, LED screens {(pb ? pb.GetComponent<RobotFace>()?.Screens ?? 0 : 0)}); the units' robot model '{(h.robotUnit ? h.robotUnit.name : "none")}'");
            if (!pb || !pb.Ready) { json[0] = $"{{\"ok\": false, {string.Join(", ", parts)}}}"; yield break; }
            float worst = 0f; string rots = "";
            foreach (var (n, e, p) in pb.Imported) { if (Pivots.TryGetValue(n, out var want)) worst = Mathf.Max(worst, (p - want).magnitude); rots += $"{n} ({e.x:0.#}, {e.y:0.#}, {e.z:0.#}) "; }
            bool identity = pb.Imported.All(x => Quaternion.Angle(Quaternion.Euler(x.euler), Quaternion.identity) < 0.5f);
            Check("import", pb.Imported.Count == Pivots.Count && worst < 0.01f, $"{pb.Imported.Count} parts, worst pivot {worst * 100f:0.0} cm off Vela's; import rotations {(identity ? "all identity" : "NOT identity: " + rots.Trim())}");

            // rolling on its sweep, at play zoom, at 1x until it has covered a metre (at most 10 s: its sweep stops at its marks)
            var pv = v.Find("patrol"); h.LookAt(pv.position); yield return Frames(2);
            float r0 = pb.RolledDeg, path = 0f, secs = 0f; var at = pb.transform.position; h.SetPaused(false);
            for (; secs < 10f && path < 1f; secs += Time.unscaledDeltaTime) { yield return null; var p = pb.transform.position; p.y = at.y; path += (p - at).magnitude; at = p; h.LookAt(pv.position); }
            h.SetPaused(true); yield return Frames(2); yield return Shoot(dir, "r104-patrol", log);
            float rolledM = (pb.RolledDeg - r0) * Mathf.Deg2Rad * PatrolBot.BallR;
            Check("rolls", path > 0.3f && Mathf.Abs(rolledM - path) <= 0.15f * path, $"the patrol covered {path:0.00} m in {secs:0.0} s; its ball rolled {rolledM:0.00} m ({pb.RolledDeg - r0:0} deg at r {PatrolBot.BallR} m); lean now {pb.LeanDeg:0.0} deg");

            // the draw: the Enforcer walks into the patrol's sight and draws; the patrol turns on the drawn weapon (patrol.breaks), its own weapon out. The
            // dial's robots come only from a dock, and the Handover's square dock stands empty (as built): the patrol is the mission's one robot
            var enf = g.Ops[(int)OpId.Enforcer]; Pt? spot = null; bool drew = false;
            for (int tries = 0; tries < 6 && !HostViews.WeaponOut(g.PatrolGun) && !g.Over; tries++)
            {
                var pAt = g.PatrolPose()?.pos ?? enf.Pos;
                spot = g.S.Grid.Nearest(enf.Pos, q => G.D2(q, pAt) <= 900L * 900 && g.PatrolWatching(q, false), 20000); if (!spot.HasValue) { h.RunTo(g.Tick + 50); continue; }
                h.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "move", Point = spot.Value }); h.RunTo(g.Tick + 5);
                for (int i = 0; i < 60 * Game.Hz && enf.Walk != null && !g.Over && !HostViews.WeaponOut(g.PatrolGun); i += 5) { h.RunTo(g.Tick + 5); if (i % 50 == 0) yield return null; }
                if (!drew) { g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "draw" }); h.RunTo(g.Tick + 1); foreach (var who in new[] { "scout", "fixer", "tech", "enforcer" }) g.Issue(new Order { Tick = g.Tick, Op = who, Do = "stance", Stance = "hold" }); drew = true; } // the cell holds its fire: the patrol is to draw, not to be downed
                for (int i = 0; i < 12 * Game.Hz && !HostViews.WeaponOut(g.PatrolGun) && !g.Over; i += 2) h.RunTo(g.Tick + 1);
            }
            PatrolBot drawnBot = null; string drawnKey = ""; int faces = 0; var faceSeen = new HashSet<int>();
            for (int i = 0; i < 30 * Game.Hz && !g.Over && drawnBot == null; i += 2)
            {
                h.RunTo(g.Tick + 2); if (i % 10 == 0) yield return null;
                foreach (var kv in v.Bots104) { var f = kv.Value ? kv.Value.GetComponent<RobotFace>() : null; if (f) faceSeen.Add(f.FightNow * 10 + (f.Alarmed(Time.time) ? 1 : 0)); }
                foreach (var kv in v.Bots104) { var gun = kv.Key == "patrol" ? g.PatrolGun : g.Units.FirstOrDefault(u => "unit/" + u.Id == kv.Key)?.Gun; if (HostViews.WeaponOut(gun) && v.Find(kv.Key) is Transform tv && tv.gameObject.activeInHierarchy) { drawnBot = kv.Value; drawnKey = kv.Key; break; } }
            }
            string when = HandoverHost.MissionClock(g.Tick);
            if (drawnBot)
            {
                h.LookAt(drawnBot.transform.position); h.SetPaused(true); // the engine held with its weapon out; the view keeps its frames, so the hatch opens and the gadget slides out
                for (float t = 0f; t < 1.6f; t += Time.unscaledDeltaTime) { yield return null; h.LookAt(drawnBot.transform.position); }
                yield return Frames(2);
            }
            var df = drawnBot ? drawnBot.GetComponent<RobotFace>() : null; if (df) faceSeen.Add(df.FightNow * 10 + (df.Alarmed(Time.time) ? 1 : 0)); faces = faceSeen.Count;
            if (drawnBot) yield return Shoot(dir, "r104-drawn", log);
            Check("draw", drawnBot && drawnBot.HatchDeg > 140f && drawnBot.SlideM > 0.10f && df && df.FightNow >= 1 && drawnBot.SirenLit,
                drawnBot ? $"{drawnKey} drawn at {when}: hatch {drawnBot.HatchDeg:0} deg, gadget out {drawnBot.SlideM:0.000} m, face {(df ? df.FightNow : -1)} (1 focused, 2 hurt), siren {B(drawnBot.SirenLit)}, flashes {drawnBot.Flashes}, hits {drawnBot.Hits}; Alert {g.Alert}"
                         : $"no robot drew by {when} (the Enforcer at {enf.Pos}, drawn {B(enf.Gun.Drawn)}, last spot in the patrol's sight {(spot.HasValue ? spot.Value.ToString() : "none")}, patrol chasing {B(g.PatrolChasing)}, down {B(g.PatrolDown)} (health {g.PatrolGun.Health}); the log on the patrol: {string.Join(" | ", g.Log.Where(l => l.Text.Contains("patrol") || l.Code.StartsWith("patrol")).Select(l => HandoverHost.MissionClock(l.Tick) + " " + l.Code + ": " + l.Text).TakeLast(6))}; over {B(g.Over)}; Alert {g.Alert}; robots drawn: {string.Join(", ", v.Bots104.Keys)})");

            // the response's robots: present as PatrolBot104 on the street
            var units = g.Units.Where(u => u.Robot && !u.Patrol && !u.Legacy).ToList(); var unitKeys = units.Select(u => "unit/" + u.Id).ToList();
            var shown = unitKeys.Where(k => v.Find(k) is Transform t && t.gameObject.activeInHierarchy).ToList();
            bool rolesOk = units.Count == 0 || unitKeys.All(k => v.Find(k) == null || v.CarModelName(k) == PatrolBot.ModelName && v.Bots104.ContainsKey(k));
            var officers = g.Units.Where(u => !u.Robot && !u.Legacy).Select(u => "unit/" + u.Id).Where(k => v.Find(k) != null).ToList();
            bool officersPeople = officers.All(k => !v.Bots104.ContainsKey(k));
            if (shown.Count > 0) { h.LookAt(v.Find(shown[0]).position); yield return Frames(3); yield return Shoot(dir, "r104-units", log); }
            Check("unitRoles", rolesOk && officersPeople && (units.Count == 0 || shown.Count > 0 || unitKeys.Any(k => v.Find(k) != null)), $"{units.Count} dial robots sent ({string.Join(", ", unitKeys)}), on the street now {shown.Count}; every robot unit drawn as {PatrolBot.ModelName}: {B(rolesOk)}; {officers.Count} officers, none a robot: {B(officersPeople)}");

            // instancing: the face the only property block per robot, every other part shared and instanced
            var bad = new List<string>(); var shared = new Dictionary<string, Material>(); int robots = 0, plain = 0;
            foreach (var kv in v.Bots104)
            {
                if (!kv.Value) continue; robots++; int mpb = 0;
                foreach (var r in kv.Value.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.name == "outline-twin") continue; // the hover outline's mask twin (HostViews.Outline): drawn only into the mask, while outlined
                    if (r.HasPropertyBlock()) { mpb++; if (r != kv.Value.FaceRenderer) bad.Add($"{kv.Key}:{r.name} has a property block"); continue; }
                    if (r == kv.Value.FaceRenderer) continue; plain++;
                    var ms = r.sharedMaterials;
                    for (int i = 0; i < ms.Length; i++)
                    {
                        var m = ms[i]; if (!m) { bad.Add($"{kv.Key}:{r.name}[{i}] no material"); continue; }
                        if (!Instanced.Contains(m.shader.name)) bad.Add($"{kv.Key}:{r.name}[{i}] {m.shader.name}");
                        if (m.name.EndsWith("(Instance)")) bad.Add($"{kv.Key}:{r.name}[{i}] a per-robot copy");
                        string slot = r.name + "#" + i + (kv.Value.SirenLit ? "+" : "-"); if (shared.TryGetValue(slot, out var was) && was != m) bad.Add($"{kv.Key}:{slot} not the shared material"); else shared[slot] = m;
                    }
                }
                if (mpb != 1) bad.Add($"{kv.Key}: {mpb} property blocks (want the face's 1)");
            }
            Check("instanced", robots > 0 && bad.Count == 0 && faces >= 2, $"{robots} robots, {plain} plain part renderers; face states seen {faces} ({string.Join(",", faceSeen.OrderBy(x => x))}: fight x10 + stare); {(bad.Count == 0 ? "the face is each robot's one property block; every part shared and on an instanced shader" : string.Join("; ", bad.Take(6)))}");

            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }
    }
}
