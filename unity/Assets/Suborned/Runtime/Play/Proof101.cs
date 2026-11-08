using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;
using Pt = Suborned.Sim.Pt;

namespace Suborned.Play
{
    /// <summary>Brief 101, Sky's rule on numbers in play (2026-11-08: "I agree", no hit chance; "Don't look at how long something takes because I don't
    /// have a choice with the action so it's irrelevant as a player. Only thing that feels time is important are the key events that happen"): the moments
    /// Aster asked pictures of, in the mission, each with the words the HUD shows then: the order ring's price plates (the Fixer on Inez), a queued walk
    /// (paused, start together: the ghosts), two agents firing and aiming at a rival (the route loud-fight), the seal and a police card. The check: no
    /// "%" and no "n s" on the ring's plates, the ghosts, the pill and the portrait plates; the seal keeps its clock (a key event). Not gated (pictures
    /// for Sky, Aster 2026-11-08); `-proofOnly r101`.</summary>
    public static class Proof101
    {
        static string B(bool b) => b ? "true" : "false";
        static string J(string x) => (x ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static Vector2 Sp(HandoverHost h, Vector3 w) { var s = h.View.WorldToScreenPoint(w); return new Vector2(s.x, s.y); }
        /// <summary>The mouse put at a screen point, its right button down or up (as ProofHold presses it).</summary>
        static void Btn(Vector2 sp, bool right = false) { if (Mouse.current is not Mouse m) return; var st = new MouseState { position = sp }; if (right) st = st.WithButton(MouseButton.Right, true); InputSystem.QueueStateEvent(m, st); }
        static readonly Regex Secs = new(@"\d+(\.\d+)?\s?s\b"), Pct = new(@"\d+\s?%"), Clock = new(@"\d+:\d\d");
        /// <summary>Words with no hit chance and no seconds in them.</summary>
        static bool Bare(string w) => !Pct.IsMatch(w ?? "") && !Secs.IsMatch(w ?? "");
        static IEnumerator Shoot(HandoverHost h, string dir, string name, List<string> log, string state)
        {
            bool was = h.Paused; h.SetPaused(true);
            while (HudToolkit.ShotBusy) yield return null;
            log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), state)); yield return null; while (HudToolkit.ShotBusy) yield return null;
            h.SetPaused(was);
        }
        /// <summary>A route's orders by tick (contracts/missions/handover/routes; the repo found as the fight lab finds it), given as the shot runner gives them.</summary>
        static List<Order> Route(string name)
        {
            var path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), "contracts", "missions", "handover", "routes", name + ".json");
            var sc = Suborned.Sim.Script.Parse(File.ReadAllText(path));
            return sc.Orders.Select((o, i) => (o, i)).OrderBy(p => p.o.Tick).ThenBy(p => p.i).Select(p => p.o).ToList();
        }
        static void PlayTo(HandoverHost h, List<Order> orders, int until)
        {
            foreach (var o in orders)
            {
                if (o.Tick >= until || h.Game.Over) break; if (o.Tick < h.Game.Tick) continue;
                if (o.Tick > h.Game.Tick) h.RunTo(o.Tick);
                var c = new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run };
                if (c.Do is "watch" or "listen" or "turn" or "call") h.GiveScripted(c); else h.Issue(c);
            }
            if (until > h.Game.Tick && !h.Game.Over) h.RunTo(until);
        }
        /// <summary>The first of these drawn on the street now, and where.</summary>
        static (string id, Vector3 at)? Drawn(HandoverHost h, params string[] ids) { foreach (var id in ids) if (h.DrawnAt(id) is Vector3 w) return (id, w); return null; }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, string[] json)
        {
            var parts = new List<string>(); bool ok = true;
            void Check(string name, bool good, string words) { parts.Add($"\"{name}\": {{\"ok\": {B(good)}, \"words\": \"{J(words)}\"}}"); ok &= good; log.Add($"r101 {name}: {(good ? "ok" : "FAILED")}: {words}"); }
            string Plates() => string.Join(" | ", Enumerable.Range(0, 4).Select(k => h.PlateWords(k)));

            // 1 · the order ring's price plates: the Fixer on Inez at the start (money, Trail, the eye, the answer; no seconds)
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; var g = h.Game;
            h.Select(OpId.Fixer, false); string ring = "";
            if (h.Views.Find("waitress") is Transform iz && iz)
            {
                h.LookAt(iz.position); yield return Frames(3); HandoverHost.PointerWorld = iz.position + Vector3.up; yield return Frames(3);
                h.LeftClickScripted(Sp(h, iz.position + Vector3.up)); yield return Frames(4);
                if (h.Ring != null) ring = string.Join(" | ", h.Ring.Segs.Where(s => !s.More && s.Row?.I != null).Select(s => s.Word + (s.Shut ? " (shut)" : "") + ": " + string.Join(" ", HudModel.PriceLine(h, s.Row.I).Select(f => f.Text).Where(t => t.Length > 0))));
                yield return Shoot(h, dir, "r101-ring-plates", log, "active"); h.CloseRing(); HandoverHost.PointerWorld = null;
            }
            Check("ringPlates", ring.Length > 0 && Bare(ring), ring);

            // 2 · a queued walk: paused with start together, the Scout's and the Tech's walks wait as ghosts that say "Walk", never its seconds
            h.controls.planTogether = true; string ghosts = "";
            if (h.Views.Find("op/scout") is Transform so && so)
            {
                var sc = so.position; h.LookAt(sc); yield return Frames(3); int n = 0;
                foreach (var (k, off) in new[] { (OpId.Scout, new Vector3(7f, 0f, 2f)), (OpId.Tech, new Vector3(-6f, 0f, 5f)) })
                {
                    var to = sc + off; foreach (var nudge in new[] { Vector3.zero, Vector3.right, Vector3.left, Vector3.forward, Vector3.back }) if (g.S.Grid.Free(h.frame.Engine(to + nudge * 2f))) { to += nudge * 2f; break; }
                    h.Select(k, false); yield return Frames(2); h.LeftClickScripted(Sp(h, to)); yield return Frames(3); n++;
                }
                ghosts = string.Join(" | ", h.Plan.Select(p => $"{(OpId)p.K}: {p.Label}")); yield return Frames(3);
                yield return Shoot(h, dir, "r101-queued-walk", log, "rest");
                Check("queuedWalk", h.Plan.Count >= 1 && h.Plan.All(p => p.Label is "Walk" or "Run"), $"{ghosts} ({n} given)");
            }
            else Check("queuedWalk", false, "no Scout on the street");

            // 2b · brief 101 step 6, the first shot: the route loud-fight on seed 3 up to 2:29 (its first shot is at 2:31), the Enforcer drawn and selected, a right
            // hold on the guard, slowed (a shot's price shows only while its hold charges: brief 067 E), pictured part way, then let go early so nothing fires. The
            // row under the pill says what the street will do (Fable's "Goes loud", the Alert it reaches, police, the seal, the Trail): no odds and no seconds
            {
                var pin2 = HandoverHost.SeedPin; HandoverHost.SeedPin = 3;
                h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; g = h.Game; HandoverHost.SeedPin = pin2;
                PlayTo(h, Route("loud-fight"), 1490); h.Issue(new Order { Tick = g.Tick, Op = "enforcer", Do = "draw" }); h.RunTo(g.Tick + 20); h.Select(OpId.Enforcer, false);
                string row = "", words = "";
                if (h.DrawnAt("guard") is Vector3 gw)
                {
                    h.LookAt(gw); yield return Frames(3); var gp = gw + Vector3.up; HandoverHost.PointerWorld = gp; var sp = Sp(h, gp); Btn(sp); yield return Frames(3);
                    HandoverHost.ChargeScale = 10f; Btn(sp, right: true); yield return new WaitForSecondsRealtime(1.0f);
                    row = HudToolkit.LoudRowShown; words = HudToolkit.PillWords;
                    while (HudToolkit.ShotBusy) yield return null; log.Add("r101-first-shot: " + HudToolkit.Shoot(Path.Combine(dir, "r101-first-shot"), "active")); yield return null; while (HudToolkit.ShotBusy) yield return null; // unpaused: a pause drops the charge's look (the pill and its row); the hold is slowed, so nothing fires
                    HandoverHost.PointerWorld = gp + new Vector3(40f, 0f, 40f); Btn(Sp(h, gp) + new Vector2(300f, 300f)); yield return Frames(3); Btn(Sp(h, gp) + new Vector2(300f, 300f)); yield return Frames(3); // away past the hold's 80 px, then up: cancelled, nothing fires
                    HandoverHost.ChargeScale = 1f; HandoverHost.PointerWorld = null;
                }
                bool wordOk = (h.Text?.StreetLoud ?? "").Length == 0 || row.StartsWith(h.Text.StreetLoud + ":");
                Check("firstShot", row.Length > 0 && wordOk && Bare(row) && Bare(words) && g.Alert < 2, $"row '{row}'; pill '{words}'; Alert {g.Alert}, {g.Orders.Count(o => o.ok && o.order.Op == "enforcer" && o.order.Do == "fire")} fire orders given");
            }

            // 3 · the loud fight (the route loud-fight on seed 3, as the late-fight row pins it: on seed 0 its entry is seen before the fight): the Scout,
            // Fixer and Tech fire on the response unit at 3:44
            var pinKeep = HandoverHost.SeedPin; HandoverHost.SeedPin = 3;
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return null; g = h.Game; HandoverHost.SeedPin = pinKeep;
            var orders = Route("loud-fight"); PlayTo(h, orders, 2262); h.Select(OpId.Scout, false);
            for (int t = 2264; t <= 2284 && !g.Over; t += 2) { PlayTo(h, orders, t); yield return Frames(2); if (Enumerable.Range(0, 4).Count(k => h.PlateWords(k) == "firing") >= 2) break; } // before 2287's moves
            yield return new WaitForSecondsRealtime(1f); // the figures ease to where the engine put them
            var tgt = Drawn(h, "security-2", "security-1");
            if (tgt is (string tid, Vector3 tw) && h.Views.Find("op/scout") is Transform sf && sf)
            {
                h.LookAt((tw + sf.position) * 0.5f); yield return Frames(3); HandoverHost.PointerWorld = tw + Vector3.up;
                for (int i = 0; i < 40 && HudToolkit.PillWords.Length == 0; i++) yield return null; yield return Frames(3);
                string pill = HudToolkit.PillWords, plates = Plates();
                yield return Shoot(h, dir, "r101-two-firing", log, "active"); HandoverHost.PointerWorld = null;
                int firing = Enumerable.Range(0, 4).Count(k => g.Ops[k].Gun.Order.Length > 0);
                Check("twoFiring", firing >= 2 && Bare(pill) && Bare(plates), $"{firing} with fire orders on {tid}; pill '{pill}'; plates '{plates}'");
            }
            else Check("twoFiring", false, $"no response officer drawn at tick {g.Tick}");

            // 4 · aiming at a rival: the rivals come for the shooting; the Scout selected, the pointer on one of them (the pill is what a shot will do)
            (string id, Vector3 at)? rv = null;
            for (int t = 2280; t <= 3200 && rv == null && !g.Over; t += 20) { PlayTo(h, orders, t); rv = Drawn(h, "rival-1", "rival-2", "rival-3", "rival-4"); }
            if (rv is (string rid, Vector3 _))
            {
                int armed = Enumerable.Range(0, 4).FirstOrDefault(k => !g.Ops[k].Down && !g.Ops[k].Inside && !g.Ops[k].InCar && g.Ops[k].Gun.W != null);
                h.Select((OpId)armed, false); yield return new WaitForSecondsRealtime(1f); // the figure eases to where the engine put it, then the pointer on it
                var rw = h.DrawnAt(rid) ?? Vector3.zero; h.LookAt(rw); yield return new WaitForSecondsRealtime(0.4f); HandoverHost.PointerWorld = rw + Vector3.up;
                for (int i = 0; i < 40 && HudToolkit.PillWords.Length == 0; i++) yield return null; yield return Frames(3);
                string pill = HudToolkit.PillWords;
                yield return Shoot(h, dir, "r101-aim-rival", log, "active"); string kept = HudToolkit.PillWords; HandoverHost.PointerWorld = null;
                Check("aimRival", pill.Length > 0 && kept == pill && Bare(pill), $"{(OpId)armed} on {rid} at tick {g.Tick}: '{pill}' (after the picture '{kept}')");
            }
            else Check("aimRival", false, $"no rival drawn by tick {g.Tick}");

            // 5 · the seal and a police card: key events keep their time (the seal's clock; the police card's "here in")
            for (int i = 0; i < 3000 && !h.SealOn && !g.Over; i++) h.RunTo(g.Tick + 1);
            if (h.SealOn)
            {
                if (h.Views.Find("op/scout") is Transform s5 && s5) h.LookAt(s5.position);
                HudToolkit.SealHoverForce = true; yield return Frames(4); yield return new WaitForSecondsRealtime(0.4f);
                string seal = HudToolkit.SealWords, card = HudToolkit.CrucialShown;
                yield return Shoot(h, dir, "r101-seal-police", log, "active"); HudToolkit.SealHoverForce = false;
                Check("sealPolice", Clock.IsMatch(seal) && Bare(Plates()), $"seal '{seal}'; card '{card}'; plates '{Plates()}' at tick {g.Tick}");
            }
            else Check("sealPolice", false, $"no seal by tick {g.Tick}");

            json[0] = $"{{\"ok\": {B(ok)}, {string.Join(", ", parts)}}}";
            h.Begin(); h.IntelOpen = false; yield return null;
        }
    }
}
