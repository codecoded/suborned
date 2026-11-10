using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Brief 105 round 2 (engine 0.53.0; Sky 2026-11-10: "they can't take popovic from the car - they can only shoot at the car"; "the game shouldn't really end
    /// just like that"), section r105 (gated): the car shot, everyone out, Take car. Keel's K3-TAKE (briefs/105/latch/k3-shot.script.json: route A, the whole cell in the
    /// getaway car with Popovic while the rivals hold it) continued through the host (ContinueFrom) from 5:40, then played by the host's own clicks:
    ///  · carHit: the rivals shoot the car (rivals.fire-car): the crucial card for-car is up (Fable's "They're shooting at our car"), and the car's ring, opened with
    ///    nobody selected, shows "Get out" and the car's health bar (RingCarHealth: hit, above where it stops);
    ///  · shotOut: at car.shot-out everyone is out on the street beside it, drawn; Popovic stands with the Enforcer (ValeAt.WithEnforcer), drawn by him; Board and Go
    ///    on our car are refused car-disabled (the host's locks, the engine's codes); the warning card has left (no longer true);
    ///  · takeRing: on the rival car the Fixer's ring offers Take car shut by its wheelman ("Enforcer only"), the Enforcer's open;
    ///  · taken (a line, not gated: the Enforcer can fall at the door under fire, the downed-Enforcer law's own brief): the Enforcer's Take car from the ring lands
    ///    (car.taken), its Go is taken, and the rival car's view drives off the street (HostViews.LeavingLent) instead of vanishing.
    /// Pictures: r105-car-hit, r105-shot-out, r105-take-ring-fixer, r105-take-ring-enforcer, r105-taken-leaves.</summary>
    public static class Proof105
    {
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static string Root => Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir("")));
        static Vector2? Screen(HandoverHost h, Vector3 w) { var s = h.View.WorldToScreenPoint(w); return s.z > 0f ? new Vector2(s.x, s.y) : null; }
        static IEnumerator Shoot(HandoverHost h, string dir, string name, List<string> log)
        {
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), "active")); yield return null; while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: ring [{h.RingWords}] card [{h.CrucialNow?.Kind}: {h.CrucialNow?.Title}]");
        }
        static IEnumerator WaitSim(HandoverHost h, System.Func<bool> done, int ticks) { var g = h.Game; for (int i = 0; i < ticks && !done() && !g.Over; i++) { h.RunTo(g.Tick + 1); if (i % 10 == 0) yield return null; } }
        static IEnumerator RingOn(HandoverHost h, Vector3 at)
        {
            h.CloseRing(); h.LookAt(at); HandoverHost.PointerWorld = at + Vector3.up * 0.6f; yield return Frames(4); // the pointer on the car, as Proof067's car ring: the ring opens where it rests
            if (Screen(h, at + Vector3.up * 0.6f) is Vector2 s) { h.LeftClickScripted(s); yield return Frames(4); }
            HandoverHost.PointerWorld = null;
        }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            void Check(string name, bool good, string words, bool gated = true) { parts.Add($"\"{name}\": {{\"ok\": {B(good)}, \"gated\": {B(gated)}, \"words\": \"{J(words)}\"}}"); if (gated) ok &= good; log.Add($"r105 {name}: {(good ? "ok" : gated ? "FAILED" : "line missed")}: {words}"); }
            string path = Path.Combine(Root, "briefs", "105", "latch", "k3-shot.script.json");
            if (!File.Exists(path)) { json[0] = "{\"ok\": false, \"why\": \"briefs/105/latch/k3-shot.script.json is not in the tree\"}"; log.Add("r105: no script"); yield break; }
            var keepConvo = h.controls.conversations; h.controls.conversations = ConversationScenes.Never;
            h.ContinueFrom(path); h.IntelOpen = false; h.ShowBriefing = false; h.StartClock(); h.SetPaused(true); yield return Frames(3);
            var g = h.Game; var v = h.Views; for (int k = 0; k < 4; k++) if (h.Selected[k]) h.Select((OpId)k, true);
            log.Add($"r105: continued at {HandoverHost.MissionClock(g.Tick)} (tick {g.Tick}), all in the car: {g.Ops.All(o => o.InCar)}, Popovic {g.Vale}");

            // carHit: the rivals shoot the car; the card; the ring with Get out and the health bar
            yield return WaitSim(h, () => g.Log.Count(l => l.Code == "car.health") >= 2, 120 * Game.Hz);
            yield return Frames(2);
            bool card = h.CrucialQueue.Exists(c => c.Kind == "for-car");
            for (int k = 0; k < 4; k++) if (h.Selected[k]) h.Select((OpId)k, true);
            yield return RingOn(h, h.GetawayWorld);
            var health = h.RingCarHealth(); string ring = h.RingWords;
            Check("carHit", g.Log.Exists(l => l.Code == "rivals.fire-car") && card && ring.Contains("Get out") && health.HasValue && health.Value.left < health.Value.max && health.Value.left > health.Value.stops,
                $"fire-car {g.Log.Exists(l => l.Code == "rivals.fire-car")}, card for-car {card} ('{h.CrucialQueue.Find(c => c.Kind == "for-car")?.Title}'), ring [{ring}], health {(health.HasValue ? $"{health.Value.left} of {health.Value.max} (stops at {health.Value.stops})" : "none")} at {HandoverHost.MissionClock(g.Tick)}");
            yield return Shoot(h, dir, "r105-car-hit", log); h.CloseRing();

            // shotOut: everyone out, Popovic with the Enforcer, both drawn; Board and Go refused car-disabled
            yield return WaitSim(h, () => g.Log.Exists(l => l.Code == "car.shot-out"), 90 * Game.Hz);
            h.RunTo(g.Tick + 5); h.SetPaused(false); yield return new WaitForSecondsRealtime(0.4f); h.SetPaused(true); yield return Frames(3);
            var enf = v.Find("op/enforcer"); var vale = v.Find("councillor");
            bool outAll = g.Ops.All(o => o.Down || !o.InCar), drawn = Enumerable.Range(0, 4).All(k => g.Ops[k].Down || v.Find("op/" + Op.Name((OpId)k)) is Transform t && t.gameObject.activeInHierarchy);
            float valeGap = enf && vale && vale.gameObject.activeInHierarchy ? Vector3.Distance(enf.position, vale.position) : -1f;
            var boardCode = g.Issue(new Order { Tick = g.Tick, Op = "scout", Do = "board", Target = "car" }).code; var goLock = h.GoLock().code;
            bool cardGone = !h.CrucialQueue.Exists(c => c.Kind == "for-car");
            Check("shotOut", g.Log.Exists(l => l.Code == "car.shot-out") && !g.Over && outAll && drawn && g.Vale == ValeAt.WithEnforcer && valeGap >= 0f && valeGap < 2.5f && boardCode == "car-disabled" && goLock == "car-disabled" && cardGone,
                $"shot out {g.Log.Exists(l => l.Code == "car.shot-out")} ({g.Result} {g.FailCode}), everyone out {outAll}, drawn {drawn}, Popovic {g.Vale} {valeGap:0.0} m from the Enforcer, Board {boardCode}, Go {goLock}, warning card gone {cardGone}");
            if (enf) h.LookAt(enf.position); yield return Frames(3); yield return Shoot(h, dir, "r105-shot-out", log);

            // takeRing: the rival car's ring, the Fixer's (shut: Enforcer only, while the wheelman sits at the wheel) and the Enforcer's (open)
            var rc = v.Find("car/rivals"); string ringF = "", ringE = ""; bool wheel = g.Crew.Count > 4 && !g.Crew[4].Gun.Down && g.Crew[4].Mode == CrewMode.Aboard;
            if (rc && !g.Over)
            {
                h.Select(OpId.Fixer, false); yield return RingOn(h, rc.position); ringF = h.RingWords; yield return Shoot(h, dir, "r105-take-ring-fixer", log); h.CloseRing();
                h.Select(OpId.Enforcer, false); yield return RingOn(h, rc.position); ringE = h.RingWords; yield return Shoot(h, dir, "r105-take-ring-enforcer", log);
            }
            bool fixerShut = !wheel ? ringF.Contains("Take car") : ringF.Contains("~Take car") && ringF.Contains("Enforcer only");
            bool enfOpen = ringE.Contains("Take car") && !ringE.Contains("~Take car");
            Check("takeRing", rc && fixerShut && enfOpen, $"rival car drawn {(bool)rc}, wheelman aboard {wheel}; Fixer's ring [{ringF}], Enforcer's ring [{ringE}]");

            // taken (a line): the Enforcer takes it from the ring, its Go, and its view drives off
            bool took = h.Ring != null && h.RingTakeScripted("take-car"); yield return Frames(2);
            yield return WaitSim(h, () => g.Ops[(int)OpId.Enforcer].InLent || g.Ops[(int)OpId.Enforcer].Down, 40 * Game.Hz);
            bool landed = g.Lent && h.LentCarId == "car:rivals" && g.Ops[(int)OpId.Enforcer].InLent;
            string goCode = "not given"; float moved = -1f; bool shownLeaving = false;
            if (landed && !g.Over)
            {
                var a0 = rc ? rc.position : Vector3.zero; var go = g.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "go", Target = "car:rivals" }); goCode = go.ok ? "taken" : go.code;
                if (go.ok)
                {
                    h.SetPaused(false); yield return new WaitForSecondsRealtime(1.6f); h.SetPaused(true); yield return Frames(2);
                    shownLeaving = rc && rc.gameObject.activeInHierarchy; moved = rc ? Vector3.Distance(a0, rc.position) : -1f;
                    if (rc) h.LookAt(rc.position); yield return Frames(3); yield return Shoot(h, dir, "r105-taken-leaves", log);
                }
            }
            Check("taken", took && landed && goCode == "taken" && shownLeaving && moved > 3f,
                $"ring take {took}, landed {landed} (Enforcer {(g.Ops[(int)OpId.Enforcer].Down ? "down" : "up")}, {g.Result} {g.FailCode}), Go {goCode}, the view drawn leaving {shownLeaving}, moved {moved:0.0} m in 1.6 s", gated: false);

            HandoverHost.PointerWorld = null; h.CloseRing(); h.controls.conversations = keepConvo; h.Select(OpId.Scout, false);
            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
        }
    }
}
