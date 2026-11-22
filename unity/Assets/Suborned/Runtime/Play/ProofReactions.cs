using System.Collections;
using System.IO;
using Path = System.IO.Path;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    public static class ProofReactions
    {
        public static IEnumerator ActionMenu(HandoverHost h, string dir)
        {
            var screen = new Vector2(Screen.width / 2, Screen.height / 2);
            var target = new HandoverHost.Target { Kind = "camera", Key = "mast-z0", Label = "mast camera" };
            var input = GameBoot.ArgValue("-availabilitySession") ?? Path.GetFullPath(Path.Combine(FightLab.Dir(""), "../../tools/game/fixtures/mast-camera-watch.script.json"));
            var replay = Script.Parse(File.ReadAllText(input));
            int firstShot = replay.Orders.FindIndex(o => o.Do == "fire" && o.Target == "camera:mast-z0");
            var before = Engine.Play(h.PlayInputs, replay.Start, replay.Orders.GetRange(0, firstShot), replay.Orders[firstShot].Tick);
            var beforePath = Path.Combine(dir, "before-camera-fire.script.json");
            File.WriteAllText(beforePath, Script.Write(before, h.PlayInputs.Hash));
            h.ContinueFrom(beforePath); h.controls.preset = ControlPreset.HoldActs; h.SelectAll(); h.SetPaused(true);
            var car = new HandoverHost.Target { Kind = "car", Key = "getaway", Label = "getaway car" };
            h.ClickRing(car, screen);
            bool board = h.Ring != null && h.Ring.Segs.Exists(s => s.Row?.I?.Order?.Do == "board") &&
                !h.Ring.Segs.Exists(s => s.Shut || s.Row?.I?.Order?.Do == "go");
            h.OpenPanel(car, screen);
            board &= h.Panel != null && h.Panel.Rows.Exists(r => r.I.Order.Do == "board") &&
                !h.Panel.Rows.Exists(r => !r.Available || r.I.Order.Do == "go");
            h.ClosePanel();
            h.ClickRing(target, screen);
            int way = h.Ring?.Segs.FindIndex(s => s.Row?.I?.Order?.Do == "fire") ?? -1;
            for (int i = firstShot; i < replay.Orders.Count; i++)
            {
                var order = replay.Orders[i];
                h.Game.RunTo(order.Tick); h.Game.Issue(order);
            }
            h.Game.RunTo(replay.Until);
            bool dark = h.Game.CameraDark(target.Key);
            int oldCount = h.Game.Orders.Count;
            if (way >= 0) h.RingTakeScripted(way);
            yield return new WaitForSecondsRealtime(0.4f);
            bool stale = way >= 0 && dark && h.Ring == null && h.Game.Orders.Count == oldCount;
            h.ContinueFrom(input);
            h.SelectAll(); h.SetPaused(true);
            bool CameraAction(Order o) => o != null && o.Do is "fire" or "watch" or "loop" or "disable";
            h.ClickRing(target, screen);
            bool ring = h.Ring == null || !h.Ring.Segs.Exists(s => s.Shut || CameraAction(s.Row?.I?.Order));
            h.OpenPanel(target, screen);
            bool panel = h.Panel == null || !h.Panel.Rows.Exists(r => !r.Available || CameraAction(r.I.Order));
            var game = h.Game;
            var watch = new Order { Tick = game.Tick, Do = "watch", Op = "scout", Target = "camera:mast-z0" };
            bool refused = game.LearnQuery(watch.Op, watch.Do, watch.Target).code == "camera-dead" &&
                game.Issue(watch).code == "camera-dead";
            h.ClosePanel(); h.CloseRing();

            // Start a real Watch while the camera is intact. An invalid replacement leaves it running,
            // then the other three agents shoot the camera and the Watch ends without teaching its sight.
            h.ContinueFrom(beforePath); h.SetPaused(true); game = h.Game;
            watch.Tick = game.Tick;
            bool watching = game.Issue(watch).ok;
            int first = game.Orders.Count;
            bool kept = !h.Issue(new Order { Do = "fire", Op = "scout", Target = "camera:missing" }, screen, null) &&
                game.Orders.Count == first && game.Ops[0].Busy?.Do == "watch";
            foreach (var shot in replay.Orders.FindAll(o => o.Do == "fire" && o.Op != "scout"))
            {
                game.RunTo(shot.Tick); game.Issue(shot);
            }
            game.RunTo(replay.Until);
            bool ended = watching && game.CameraDark(target.Key) && game.Ops[0].Busy == null &&
                game.Dropped.Exists(d => d.op == OpId.Scout && d.code == "camera-dead") && !game.Known(watch.Target).Sight;
            bool ok = board && dark && ring && panel && refused && kept && stale && ended;
            File.WriteAllText(Path.Combine(dir, "action-menu.txt"), $"{(ok ? "PASS" : "FAIL")} empty car offers Board and hides Go {board}, camera destroyed {dark}, no camera actions on ring {ring} or panel {panel}, Watch rejected {refused}, stale click removed {stale}, invalid replacement kept live Watch {kept}, destruction ended Watch without intel {ended}");
        }

        public static IEnumerator SquadOrders(HandoverHost h, string dir)
        {
            var input = GameBoot.ArgValue("-orderSession");
            if (input == null)
            {
                var g = Scenarios.Run(h.PlayInputs, Scenarios.Find("096-RIV-11")).G;
                var script = Script.Parse(Script.Write(g, h.PlayInputs.Hash));
                int i = script.Orders.FindIndex(o => o.Do == "takedown" && o.Target == "rival-4");
                var before = Engine.Play(h.PlayInputs, script.Start, script.Orders.GetRange(0, i), script.Orders[i].Tick);
                input = Path.Combine(dir, "before-rival-order.script.json");
                File.WriteAllText(input, Script.Write(before, h.PlayInputs.Hash));
            }
            h.ContinueFrom(input);
            h.controls.preset = ControlPreset.HoldActs;
            h.SelectAll();
            h.SetPaused(false);
            var target = new HandoverHost.Target { Kind = "rivals", Key = "rival-4", Id = "rival-4", Label = "the lookout" };
            string pedestrian = GameBoot.ArgValue("-orderPedestrian");
            if (pedestrian != null)
            {
                int index = h.Game.Walkers.FindIndex(w => w.P.Id == pedestrian);
                target = new HandoverHost.Target { Kind = "crowd", Key = "crowd/" + index, Id = pedestrian, Label = "a passer-by" };
            }
            var screen = new Vector2(Screen.width / 2, Screen.height / 2);
            var game = h.Game;
            bool squad = h.Agent != OpId.Enforcer && h.Selected[(int)OpId.Enforcer];
            for (int k = 0; k < h.Selected.Length; k++) squad &= h.Selected[k] == !game.Ops[k].Down;
            int first = game.Orders.Count, log = game.Log.Count;
            bool opened = h.ClickRing(target, screen);
            int kill = opened ? h.Ring.Segs.FindIndex(s => s.Row?.I?.Order?.How == "kill" && !s.Shut) : -1;
            string ways = h.RingWords;
            if (kill >= 0)
            {
                // Exercise the menu decision with a known witness even on a quiet scenario seed.
                if (GameBoot.ArgValue("-orderSession") == null) h.Ring.Segs[kill].Row.I.Td.Seen.Seen = "yes";
                h.RingTakeScripted(kill);
            }
            bool pending = h.PendingTarget((int)OpId.Enforcer) == target.Id;
            bool diverted = h.Loud != null;
            for (int i = 0; i < 60 && h.PendingTarget((int)OpId.Enforcer) == target.Id && !game.Over; i++)
                h.RunTo(game.Tick + Game.Hz);
            var orders = game.Orders.GetRange(first, game.Orders.Count - first);
            var attempts = orders.FindAll(e => e.order.Do == "takedown");
            var result = game.Log.FindIndex(log, l => l.Code == "takedown.starts" || l.Code == "takedown.fails");
            bool taken = attempts.Count == 1 && attempts[0].ok && attempts[0].order.Op == "enforcer"
                && attempts[0].order.How == "kill" && attempts[0].order.Target == target.Id;
            int walks = orders.FindAll(e => e.ok && e.order.Do == "move" && e.order.Op == "enforcer").Count;
            bool ok = squad && kill >= 0 && !diverted && taken && result >= 0 && (pedestrian == null || walks > 1);
            File.WriteAllText(Path.Combine(dir, "squad-kill.txt"), $"{(ok ? "PASS" : "FAIL")} squad {squad}, wedge {kill}, confirmation {diverted}, walked {pending} ({walks} legs), attempts {attempts.Count}, ring [{ways}], result {(result >= 0 ? game.Log[result].Code : "none")}");
            if (GameBoot.ArgValue("-orderSession") == null)
            {
                h.Begin(); h.StartClock(); h.SelectAll();
                var cast = new HandoverHost.Target { Kind = "person", Key = "waitress", Id = "waitress" };
                h.OpenPanel(cast, screen);
                int row = h.Panel.Rows.FindIndex(r => r.Available && r.I?.Order?.How == "knockout");
                bool stayed = false, correctActor = false;
                if (row >= 0)
                {
                    h.Panel.Rows[row].I.Td.Seen.Seen = "yes";
                    int prior = h.Game.Orders.Count;
                    h.PanelDo(row);
                    yield return null; yield return null;
                    stayed = h.Loud?.Source == "wait" && h.Game.Orders.Count == prior;
                    if (stayed && UnityEngine.InputSystem.Mouse.current is { } mouse)
                    {
                        var state = new UnityEngine.InputSystem.LowLevel.MouseState().WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left);
                        UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, state);
                        h.GateHoldByCard();
                        yield return new WaitForSecondsRealtime(0.8f);
                        UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState());
                        var issued = h.Game.Orders.GetRange(prior, h.Game.Orders.Count - prior);
                        correctActor = h.Loud == null && issued.Exists(e => e.ok && e.order.Op == "enforcer" && (e.order.Do == "takedown" || e.order.Do == "move" && h.PendingTarget((int)OpId.Enforcer) == cast.Key));
                    }
                }
                File.WriteAllText(Path.Combine(dir, "confirmation.txt"), $"{(stayed && correctActor ? "PASS" : "FAIL")} card remained after click {stayed}, confirmed for Enforcer with squad selected {correctActor}");
            }
            yield return null;
        }

        public static IEnumerator Run(HandoverHost h, string dir)
        {
            h.Begin();
            h.StartClock();
            yield return null;
            int picked = System.Array.FindAll(h.Selected, s => s).Length;
            File.WriteAllText(Path.Combine(dir, "mission-selection.txt"), $"{(picked == 4 ? "PASS" : "FAIL")} {picked} operatives selected");
            h.Select(OpId.Tech, false);
            yield return null;
            foreach (var device in new[] { "pickup", "mast-z0" })
            {
                string id = h.Game.ControlPanel(device);
                var panel = h.Views.Find("marker/" + id);
                bool drawn = panel && panel.GetComponentInChildren<Renderer>() != null;
                if (drawn)
                {
                    h.LookAt(panel.position);
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir, id + ".png"));
                    yield return null;
                }
                var pickedPanel = new HandoverHost.Target { Kind = "cabinet", Key = id, Label = id };
                h.OpenPanel(pickedPanel, new Vector2(Screen.width / 2, Screen.height / 2));
                bool linked = h.Panel?.Rows.Exists(r => r.I?.Order?.Do == "loop" && r.I.Order.Target == device) == true
                    && !h.Panel.Rows.Exists(r => r.I?.Order?.Do is "loop" or "disable" && r.I.Order.Target != device);
                h.ClosePanel();
                File.WriteAllText(Path.Combine(dir, id + ".txt"), $"{(drawn && linked ? "PASS" : "FAIL")} mesh {drawn}, linked actions {linked}");
            }
            h.OpenMenu(); h.ResumeFromMenu();
            bool pausedSelection = h.Selected[(int)OpId.Tech] && System.Array.FindAll(h.Selected, x => x).Length == 1;
            h.ContinueFrom(h.SessionPath);
            bool resumed = pausedSelection && h.Continued && h.Selected[(int)OpId.Tech] && System.Array.FindAll(h.Selected, x => x).Length == 1;
            File.WriteAllText(Path.Combine(dir, "continued-selection.txt"), $"{(resumed ? "PASS" : "FAIL")} restored Tech selection");
            var source = Scenarios.Run(h.PlayInputs, Scenarios.Find("N68")).G;
            var recorded = Script.Parse(Script.Write(source, h.PlayInputs.Hash));
            recorded.Start.RecoverCasualties = true;
            var recovered = Engine.Play(h.PlayInputs, recorded.Start, recorded.Orders, source.Tick);
            var removal = recovered.Log.Find(e => e.Code == "body.recovered");
            bool hidden = false;
            if (removal.Code != null)
            {
                var before = Engine.Play(h.PlayInputs, recorded.Start, recorded.Orders, removal.Tick - 1);
                string file = Path.Combine(dir, "before-recovery.script.json");
                File.WriteAllText(file, Script.Write(before, h.PlayInputs.Hash));
                h.ContinueFrom(file); h.SetPaused(true);
                var body = h.Views.Find("body/guard");
                bool visible = body && body.gameObject.activeSelf;
                h.RunTo(removal.Tick);
                yield return null;
                hidden = visible && body && !body.gameObject.activeSelf && h.Game.BodyOf(h.Game.GuardP)?.Recovered == true;
            }
            File.WriteAllText(Path.Combine(dir, "body-recovery.txt"), $"{(hidden ? "PASS" : "FAIL")} guard removed at {removal.Tick}");
            foreach (string id in new[] { "N125", "N127" })
            {
                var scene = Scenarios.Run(h.PlayInputs, Scenarios.Find(id));
                var g = scene.G;
                if (id == "N125")
                {
                    var shot = scene.Shots().Find(s => s.by.StartsWith("rival-") && s.target == "enforcer");
                    bool answered = false;
                    string clip = "", cues = "";
                    if (shot.by != null)
                    {
                        var script = Script.Parse(Script.Write(g, h.PlayInputs.Hash));
                        var beforeShot = Engine.Play(h.PlayInputs, script.Start, script.Orders, shot.tick - 1);
                        string session = Path.Combine(dir, "before-return-fire.script.json");
                        File.WriteAllText(session, Script.Write(beforeShot, h.PlayInputs.Hash));
                        h.ContinueFrom(session); h.SetPaused(true);
                        if (h.Views.Find(shot.by) is Transform shooter) h.LookAt(shooter.position);
                        h.RunTo(shot.tick);
                        clip = h.Views.FigureClip(shot.by).state;
                        bool aimed = h.Views.Aim.ContainsKey(shot.by);
                        bool flash = h.ShotFx.Exists(fx => fx.side == "rival");
                        bool fired = h.Views.FiredNow.Contains(shot.by);
                        cues = $"aim {aimed}, flash {flash}, shot {fired}, actual tick {h.Game.Tick}";
                        answered = aimed && flash && fired && clip is "shoot" or "walk_armed" or "run_armed" or "cover_fire" or "aim_pistol";
                        var feet = h.Views.Find(shot.by);
                        var firingPosition = feet.position;
                        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "return-fire.png"));
                        yield return null;
                        var next = scene.Shots().Find(s => s.by == shot.by && s.target == shot.target && s.tick > shot.tick);
                        if (next.by != null)
                        {
                            h.RunTo(next.tick);
                            float distance = Vector3.Distance(firingPosition, feet.position);
                            cues += $", second shot {next.tick}, feet moved {distance:F2} m";
                            answered &= distance < 0.6f && h.Views.FiredNow.Contains(shot.by);
                        }
                        else answered = false;
                    }
                    File.WriteAllText(Path.Combine(dir, "return-fire.txt"), $"{(answered ? "PASS" : "FAIL")} {shot.by} fires at {shot.target} on tick {shot.tick}; figure playing {clip}, {cues}");
                }
                bool seal = id == "N127";
                if (seal)
                {
                    var inputs = h.PlayInputs;
                    var run = Script.Parse(Script.Write(g, inputs.Hash));
                    var before = Engine.Play(inputs, run.Start, run.Orders, g.Tick - 9 * Game.Hz);
                    string path = Path.Combine(dir, "before-road-closure.script.json");
                    File.WriteAllText(path, Script.Write(before, inputs.Hash));
                    h.ContinueFrom(path); h.SetPaused(true);
                    yield return null;
                    bool clear = !h.Views.SealedRoad.HasValue;
                    h.RunTo(g.Tick - 7 * Game.Hz);
                    var sedan = GameObject.Find("District road closure/Response sedan");
                    Vector3 from = sedan ? sedan.transform.position : Vector3.zero;
                    h.RunTo(g.Tick - 1);
                    bool moving = sedan && Vector3.Distance(from, sedan.transform.position) > 5f;
                    bool playable = !h.Game.Over && h.Views.SealedRoad.HasValue;
                    File.WriteAllText(Path.Combine(dir, "road-arrival.txt"), $"{(clear && moving && playable ? "PASS" : "FAIL")} open first {clear}, sedan moved {moving}, playable until closure {playable}");
                    h.RunTo(g.Tick);
                    g = h.Game;
                }
                else h.ShowEnded(g);
                if (!seal && h.Views.Find("op/enforcer") is Transform t) h.LookAt(t.position);
                float started = Time.unscaledTime, first = -1, last = -1;
                bool closureOnScreen = false, cue = false, waited = false, captured = false, earlyBook = false;
                int tick = g.Tick;
                do
                {
                    yield return null;
                    float progress = h.Views.FallProgress("op/enforcer");
                    if (progress >= 0 && first < 0) first = progress;
                    last = progress;
                    cue |= HudToolkit.VisibleSealNotice.Contains("District sealed");
                    waited |= h.EndHeld;
                    earlyBook |= h.EndHeld && h.BookEnd;
                    if (seal && h.Views.SealedRoad is Vector3 road && Time.unscaledTime - started > 1.5f)
                    {
                        Vector3 view = h.View.WorldToViewportPoint(road);
                        closureOnScreen |= view.z > 0 && Mathf.Abs(view.x - 0.5f) < 0.25f && Mathf.Abs(view.y - 0.5f) < 0.25f;
                    }
                    if (!captured && (seal ? cue && closureOnScreen : progress > 0.55f))
                    {
                        captured = true;
                        ScreenCapture.CaptureScreenshot(Path.Combine(dir, id + "-reaction.png"));
                    }
                } while (h.EndHeld && Time.unscaledTime - started < 8f);
                for (int i = 0; i < 3 && !h.BookEnd; i++) yield return null;
                bool ok = waited && !earlyBook && h.BookEnd && !h.EndHeld && tick == g.Tick && (seal ? g.FailCode == "sealed" && cue && closureOnScreen : g.FailCode == "enforcer-down" && last >= 0.98f && first < last);
                File.WriteAllText(Path.Combine(dir, id + "-reaction.txt"), $"{(ok ? "PASS" : "FAIL")} {g.FailCode} tick {tick}, held {Time.unscaledTime - started:F2}s, animation {first:F3} to {last:F3}, seal visible {cue}, road in frame {closureOnScreen}, debrief {h.BookEnd}, premature {earlyBook}");
            }
        }
    }
}
