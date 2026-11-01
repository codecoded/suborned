using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Order = Suborned.Sim.Order;
using OpId = Suborned.Sim.OpId;
using ValeAt = Suborned.Sim.ValeAt;

namespace Suborned.Play
{
    /// <summary>Brief 085, the second round: whole conversations at landing, walking bystanders, the pressed guard, the live insets, the capture clip, the Door price, through the Input System's own
    /// devices where it is input (bun run hostlogic can't reach Update), in r083's run. Pictures `r085-*`; each row names what it measured.</summary>
    public static partial class Proof085Scenes
    {
        static void Keys(params Key[] down) { if (Keyboard.current is Keyboard kb) InputSystem.QueueStateEvent(kb, new KeyboardState(down)); }
        static void Btn(Vector2 sp, bool left = false) { if (Mouse.current is Mouse m) { var st = new MouseState { position = sp }; if (left) st = st.WithButton(MouseButton.Left, true); InputSystem.QueueStateEvent(m, st); } }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Press(Key k) { Keys(k); yield return Frames(3); Keys(); yield return Frames(3); }
        static IEnumerator Click(Vector2 sp) { Btn(sp); yield return Frames(2); Btn(sp, true); yield return Frames(3); Btn(sp); yield return Frames(3); }
        static IEnumerator Shoot(string dir, string name, List<string> log) { while (HudToolkit.ShotBusy) yield return null; log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null; }
        static List<Order> Route(string name) { var path = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FightLab.Dir(""))), "contracts", "missions", "handover", "routes", name + ".json"); var sc = Suborned.Sim.Script.Parse(File.ReadAllText(path)); var l = new List<(Order o, int i)>(); for (int i = 0; i < sc.Orders.Count; i++) l.Add((sc.Orders[i], i)); l.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.i.CompareTo(b.i)); var r = new List<Order>(); foreach (var (o, _) in l) r.Add(o); return r; }
        sealed class RouteRun { public List<Order> Orders; public int I; }

        static IEnumerator Fresh(HandoverHost h, ConversationScenes mode, bool sameSeed = false)
        {
            HandoverHost.PointerWorld = null; HandoverHost.PointerOverride = null; Keys(); Btn(new Vector2(Screen.width * .5f, 40f));
            if (h.Driving) h.DriveLetGoToMap(); if (h.Following) h.FollowToggle();
            h.controls.conversations = mode; h.SetSpeed(1); if (sameSeed) h.BeginSameSeed(); else h.Begin(); h.IntelOpen = false; h.ShowBriefing = false; h.StartClock(); h.SetPaused(true); yield return Frames(3);
            h.View.orthographicSize = 10f; h.Select(OpId.Scout, false); yield return Frames(2);
        }
        static void Give(HandoverHost h, Order o) { var c = new Order { Tick = o.Tick, Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Point = o.Point, How = o.How, Run = o.Run }; if (c.Do is "watch" or "listen" or "turn" or "call" && c.Op != "cell") h.GiveScripted(c); else h.Issue(c); }

        /// <summary>The route's orders on their ticks, the clock stepped directly, until a scene is asked for or the tick is reached.</summary>
        static void Step(HandoverHost h, RouteRun r, int until)
        {
            var g = h.Game;
            while (!g.Over && g.Tick < until && !h.ConvoPending)
            {
                while (r.I < r.Orders.Count && r.Orders[r.I].Tick <= g.Tick && !h.ConvoPending) Give(h, r.Orders[r.I++]);
                if (h.ConvoPending) break;
                h.RunTo(g.Tick + 1);
            }
        }
        /// <summary>On to the next scene of the route; on its first line; false when none came by `until`.</summary>
        static IEnumerator ToScene(HandoverHost h, RouteRun r, int until, bool[] got)
        {
            Step(h, r, until); got[0] = false;
            for (int i = 0; i < 30 && h.ConvoPending && !h.ConvoOn; i++) yield return null;
            if (h.ConvoOn) { yield return new WaitForSecondsRealtime(0.35f); got[0] = true; }
        }
        static IEnumerator EndScene(HandoverHost h) { if (!h.ConvoOn) { h.ConvoEndNow(); yield break; } yield return Press(Key.Escape); for (int i = 0; i < 40 && h.ConvoOn; i++) yield return null; h.ConvoEndNow(); yield return Frames(2); }
        /// <summary>Word for word against the file: the scene's lines are the conversation's beats in order.</summary>
        static string Words(HandoverHost h, string id)
        {
            var c = h.ConvoNow; if (c == null) return "no scene"; if (c.Id != id) return $"plays '{c.Id}'";
            var want = h.Text.Conversations.Find(x => x.Id == id); if (want == null) return "not in the file"; if (want.Beats.Count != c.Lines.Count) return $"{c.Lines.Count} lines of {want.Beats.Count}";
            for (int i = 0; i < want.Beats.Count; i++) if (want.Beats[i].Line != c.Lines[i].text || want.Beats[i].Speaker != c.Lines[i].speaker) return $"line {i} differs";
            return "";
        }
        static string PictureHash(RenderTexture rt)
        {
            if (!rt) return "none"; var keep = RenderTexture.active; RenderTexture.active = rt; var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false); t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply(); RenderTexture.active = keep;
            long sum = 0; var px = t.GetRawTextureData<byte>(); for (int i = 0; i < px.Length; i += 7) sum = sum * 31 + px[i]; Object.Destroy(t); return sum.ToString();
        }
        /// <summary>The share of a picture that isn't its ground (the top left corner's colour): a faceless inset reads about 0.</summary>
        static float PictureFill(RenderTexture rt)
        {
            if (!rt) return 0f; var keep = RenderTexture.active; RenderTexture.active = rt; var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false); t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply(); RenderTexture.active = keep;
            var px = t.GetPixels32(); var g = px[px.Length - 1]; int n = 0; foreach (var p in px) if (Mathf.Abs(p.r - g.r) + Mathf.Abs(p.g - g.g) + Mathf.Abs(p.b - g.b) > 30) n++; Object.Destroy(t); return n / (float)px.Length;
        }
        /// <summary>Every inset line for `seconds`, the clock running: two frames apart differ and each has a face. A callout pushed in over a line, gone before its frames, takes the speech inset's slot and gives it back: what blanked the guard's second line on route A.</summary>
        static IEnumerator EveryInsetLine(HandoverHost h, float seconds, List<string> seen, bool[] ok, int[] hits, bool push)
        {
            string last = ""; float t0 = Time.unscaledTime; int line = 0;
            while (Time.unscaledTime - t0 < seconds)
            {
                hits[0] = Mathf.Max(hits[0], HudToolkit.SpeechPlateHits);
                var co = h.Callouts.Find(x => x.Speech != null); yield return null;
                if (co == null || co.Speech.Text == last || !HudToolkit.DockedSpeech) continue;
                last = co.Speech.Text; line++;
                if (push) h.Callouts.Add(new HandoverHost.Callout { Watcher = "proof", View = line % 2 == 1 ? co.View : "patrol", Op = "scout", Stage = 3, Seq = 900 + line, Born = Time.unscaledTime, Until = Time.unscaledTime + 0.5f });
                for (float w = Time.unscaledTime; Time.unscaledTime - w < 0.75f;) { hits[0] = Mathf.Max(hits[0], HudToolkit.SpeechPlateHits); yield return null; }
                var rt = HudToolkit.SpeechPicture; var a = PictureHash(rt); float f0 = PictureFill(rt); yield return new WaitForSecondsRealtime(0.35f); rt = HudToolkit.SpeechPicture; var b = PictureHash(rt); float f1 = PictureFill(rt);
                bool good = a != "none" && a != b && f0 > 0.05f && f1 > 0.05f; ok[0] &= good; seen.Add($"{co.View} '{last}': frames differ {a != b}, face {f0:0.00}/{f1:0.00}");
            }
        }
        static float GroundOff(HandoverHost h, Vector3 at) { var c = h.View; if (!c) return 999f; var f = c.transform.forward; float k = (at.y - c.transform.position.y) / f.y; var hit = c.transform.position + f * k; hit.y = at.y; return Vector3.Distance(hit, at); }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            var keep = h.controls.conversations; var got = new bool[1];
            // ---- route A: Listen, the two Helps, the Call, each whole at its landing; First time counts by scene, not by person
            yield return Fresh(h, ConversationScenes.FirstEachScene); var g = h.Game; var ra = new RouteRun { Orders = Route("route-a") };
            var cam = h.View; var pos0 = cam.transform.position; var rot0 = cam.transform.rotation; yield return ToScene(h, ra, 900, got); string w = Words(h, "listen-inez"); var c = h.ConvoNow;
            row("r085listenWhole", got[0] && w.Length == 0 && c.HasCards, $"{h.ConvoLast}; {w}; said '{HudToolkit.ConvoSaid}'; picks {string.Join(",", h.ConvoPicks)}");
            if (got[0])
            {
                int tick0 = g.Tick, ord0 = g.Orders.Count; yield return Shoot(dir, "r085-conv-line", log);
                // the hold: this line stays as long as it takes to read, no input
                int b0 = c.Beat; float t0 = Time.unscaledTime, heldFor = c.BeatAt; float want = HandoverHost.ReadS(c.Lines[b0].text); while (h.ConvoOn && c.Beat == b0 && Time.unscaledTime - t0 < want + 3f) yield return null; float held = Time.unscaledTime - heldFor;
                row("r085readHold", Mathf.Abs(held - want) < 0.35f, $"line of {c.Lines[b0].text.Length} chars held {held:0.00} s, wants {want:0.00} s");
                // click and Space advance, and neither gives an order
                {
                    int b1 = c.Beat; yield return Click(new Vector2(Screen.width * .5f, Screen.height * .5f)); int b2 = c.Beat; yield return Press(Key.Space); int b3 = c.Beat;
                    row("r085advance", b2 == b1 + 1 && b3 == b2 + 1 && g.Orders.Count == ord0, $"line {b1}, click {b2}, Space {b3}; orders {ord0} to {g.Orders.Count}");
                    yield return ListenBeats(h, c, dir, log, row);
                    for (int i = 0; i < 40 && h.ConvoOn; i++) yield return null;
                    h.SetPaused(true); yield return Frames(3);
                    row("r085viewBack", h.ConvoLast.Contains("bit-identical") && cam.transform.position == pos0 && cam.transform.rotation == rot0, h.ConvoLast);
                    row("r085clockHeld", g.Tick == tick0 && g.Orders.Count == ord0, $"tick {tick0} to {g.Tick}; orders {ord0} to {g.Orders.Count}");
                    yield return CardOnPerson(h, dir, log, row);
                }
            }
            yield return EndScene(h);
            // the two Helps count as two scenes, the Call as a third, under First time
            yield return ToScene(h, ra, 1300, got); string w1 = Words(h, "help-inez-1"); var c1 = h.ConvoNow; row("r085help1", got[0] && w1.Length == 0, $"{h.ConvoLast}; {w1}; {string.Join(" / ", c1?.Learned ?? new List<string>())}"); yield return Shoot(dir, "r085-help-1", log); yield return EndScene(h);
            // a walker in the shot: the Scout, sent across just before the second Help lands (in this proof's run only)
            Step(h, ra, 1000); int land2 = g.Ops[1].Busy != null ? g.Ops[1].BusyEnd : g.Tick + 300; Step(h, ra, land2 - 6);
            if (g.Ops[0].Walk == null && g.PersonPos(g.Inez) is Suborned.Sim.Pt ip2)
            {   // the first of eight headings with 8 m of open walk that keeps clear of both speakers: along the pair first, so a profile shot sees her cross it
                var fx = g.Ops[1].Pos; var sp0 = g.Ops[0].Pos; float a0d = Mathf.Atan2(fx.Y - ip2.Y, fx.X - ip2.X);
                foreach (int d in new[] { 2, 6, 1, 3, 5, 7, 0, 4 })
                {
                    float a = a0d + d * Mathf.PI / 4f; var to = new Suborned.Sim.Pt(sp0.X + (int)(Mathf.Cos(a) * 600f), sp0.Y + (int)(Mathf.Sin(a) * 600f)); bool open = true;
                    for (int i = 1; i <= 3; i++) { var q = new Suborned.Sim.Pt(sp0.X + (to.X - sp0.X) * i / 6, sp0.Y + (to.Y - sp0.Y) * i / 6); if (!g.Walk.Free(q)) open = false; } // 3 m clear, then wherever the walk goes
                    if (open) { h.Issue(new Order { Op = "scout", Do = "move", Point = g.Walk.NearestFree(to) ?? to }); log.Add($"r085 the walker's heading: {d} of 8, walking {g.Ops[0].Walk != null}"); break; }
                }
                if (g.Ops[0].Walk == null) log.Add("r085 the walker: no open heading");
            }
            yield return ToScene(h, ra, 1300, got); string w2 = Words(h, "help-inez-2"); row("r085help2", got[0] && w2.Length == 0, $"second Help, same person, under First time: {h.ConvoLast}; {w2}");
            if (got[0])
            {
                int tickB = g.Tick; string walker = null, stander = null; float wd = 1e9f, sd = 1e9f;
                foreach (var (n, x) in h.Views.All())
                {
                    if (!x || !x.gameObject.activeInHierarchy || h.ConvoIsSpeaker(n) || !(n.StartsWith("crowd/") || n.StartsWith("op/"))) continue;
                    var (st, _) = h.Views.FigureClip(n); if (st.Length == 0) continue; var v = cam.WorldToViewportPoint(x.position + Vector3.up); bool inShot = v.z > 0f && v.x > 0.05f && v.x < 0.95f && v.y > 0.1f && v.y < 0.9f; float dd = (x.position - cam.transform.position).sqrMagnitude * (inShot ? 0.01f : 1f);
                    if (h.Views.WalkingNow(n)) { if (dd < wd) { wd = dd; walker = n; } } else if (dd < sd) { sd = dd; stander = n; }
                }
                if (walker != null)
                {
                    var wt = h.Views.Find(walker); yield return new WaitForSecondsRealtime(0.15f); yield return Shoot(dir, "r085-bystander", log); var walkLog = new List<string>(); var a0 = wt.position; var wa = a0; float wt0 = Time.unscaledTime, tp = wt0; bool walked = true; for (int i = 0; i < 8; i++) { yield return new WaitForSecondsRealtime(0.125f); float tn = Time.unscaledTime; var wb = wt.position; var (stn, spn) = h.Views.FigureClip(walker); float gs = Vector3.Distance(wa, wb) / Mathf.Max(0.01f, tn - tp); walkLog.Add($"{stn} {spn:0.00} {gs:0.0}"); wa = wb; tp = tn; walked &= stn.StartsWith("walk") || h.Views.SceneStopped > 0; }
                    var (stStand, spStand) = stander != null ? h.Views.FigureClip(stander) : ("", 0f); float dist = Vector3.Distance(a0, wt.position), dt = Time.unscaledTime - wt0;
                    row("r085bystander", g.Tick == tickB && dist > 0.3f && walked, $"{walker} moved {dist:0.00} m in {dt:0.00} s on a held clock (tick {tickB} to {g.Tick}); clip, anim speed, ground m/s: {string.Join("; ", walkLog)}; {h.Views.SceneWalkers} walkers, {h.Views.SceneTurned} turns aside, {h.Views.SceneStopped} stopped");
                    row("r085standing", stander == null || spStand > 0.05f, $"{stander}: {stStand}, anim {spStand:0.00}");
                    yield return EndScene(h); h.SetPaused(true); yield return Frames(3); var back = h.Views.EngineAt(walker); float snap = back.HasValue ? Vector3.Distance(wt.position, back.Value) : 99f; row("r085snapBack", snap < 0.4f, $"{walker} {snap:0.00} m from the engine's place at the cut out");
                }
                else { row("r085bystander", false, "no walker at this scene"); yield return Shoot(dir, "r085-help-2", log); }
                if (h.ConvoOn) yield return Shoot(dir, "r085-help-2", log);
            }
            yield return EndScene(h);
            var callO = new Order { Op = "fixer", Do = "call", Target = "waitress" }; string helpDoor = h.DoorClock(callO); row("r085doorHelpRoute", helpDoor == null, $"the Call way after Help: Door {helpDoor ?? "hidden"}");
            h.Select(OpId.Fixer, false); yield return ToScene(h, ra, 1320, got); string w3 = Words(h, "call-inez"); c = h.ConvoNow; var cuts = new List<string>();
            if (got[0]) yield return CallHolds(h, c, cuts, dir, log, row);
            row("r085callCuts", got[0] && w3.Length == 0 && cuts.Contains("guard") && cuts.Contains("fixer") && cuts.Contains("waitress"), $"{w3}; shots {string.Join(",", cuts)}");
            yield return EndScene(h); Step(h, ra, g.Tick + 2); /* the route's other orders at this tick first: the Enter is timed to them */ yield return Frames(3);
            var staleC = h.Callouts.Find(x => x.Speech != null); row("r085callNoStale", staleC == null && !h.Bubbles.Exists(b => b.Speaker == "guard" && Time.unscaledTime - b.Born < b.Life), $"after the Call's scene: inset line '{staleC?.Speech?.Text ?? "none"}'");
            // ---- the capture clip on route A, at Speed 1, real time
            Step(h, ra, 1330); var en = g.Ops[(int)OpId.Enforcer]; bool enterOn = en.Inside && en.Busy?.Do == "enter"; int end0 = en.BusyEnd; if (enterOn) { h.RunTo(end0 - 102 - 15); } h.SetPaused(false); yield return Frames(2);
            float wait0 = Time.unscaledTime; while (h.ClipNow != HandoverHost.ClipState.Playing && Time.unscaledTime - wait0 < 6f) yield return null;
            yield return new WaitForSecondsRealtime(0.35f); h.SetPaused(true); yield return Frames(2); float clipStartAt = g.Tick; yield return Shoot(dir, "r085-clip-start", log); var pic0 = PictureHash(h.ClipTexture); string word = HudToolkit.ClipWordNow; h.SetPaused(false);
            yield return new WaitForSecondsRealtime(4f); h.SetPaused(true); yield return Frames(2); yield return Shoot(dir, "r085-clip-mid", log); var pic1 = PictureHash(h.ClipTexture); double vidMid = h.ClipVideoTime, wantMid = h.ClipWantTime;
            row("r085clipPlays", enterOn && h.ClipNow == HandoverHost.ClipState.Playing && pic0 != pic1 && word.Length > 0, $"state {h.ClipNow} '{word}'; pictures differ {pic0 != pic1}; video {vidMid:0.00} wants {wantMid:0.00}; {h.ClipLast}");
            // the click: the camera to the door, no order; at yaw 30, then back
            int ordC = g.Orders.Count; var door = h.ClipLookAt() ?? Vector3.zero; var bef = cam.transform.position; string clickPath = "device";
            if (HudToolkit.ClipScreen is Vector2 csp) { yield return Click(csp); if (cam.transform.position == bef) { clickPath = "handler (the HUD's PointerDown missed an unfocused player)"; h.ClipClick(); yield return Frames(2); } }
            float offDoor = GroundOff(h, door); yield return Shoot(dir, "r085-clip-click-yaw30", log);
            row("r085clipClick", offDoor < 2f && g.Orders.Count == ordC, $"camera {offDoor:0.0} m off the door ({clickPath}); orders {ordC} to {g.Orders.Count}");
            h.Select(OpId.Scout, false); h.SetPaused(false); yield return Press(Key.T); yield return new WaitForSecondsRealtime(0.7f); h.SetPaused(true); yield return Frames(2); bool inGround = h.Following; var clipOn = HudToolkit.ClipScreen; yield return Shoot(dir, "r085-clip-ground", log);
            int notDrive = 0; for (int i = ordC; i < g.Orders.Count; i++) if (g.Orders[i].order.Do != "drive") notDrive++;
            if (clipOn is Vector2 cg) { yield return Click(cg); if (h.Following) { h.ClipClick(); clickPath = "handler"; } yield return Frames(3); }
            for (int i = ordC; i < g.Orders.Count; i++) if (g.Orders[i].order.Do != "drive") notDrive++;
            row("r085clipGround", inGround && clipOn != null && !h.Following && GroundOff(h, door) < 2f && notDrive == 0, $"in the ground view {inGround}, clip shown {clipOn != null}; a click: following {h.Following}, camera {GroundOff(h, door):0.0} m off the door; orders other than the view's own {notDrive} ({clickPath})");
            h.SetPaused(false); float tOut = Time.unscaledTime; while (h.ClipNow == HandoverHost.ClipState.Playing && Time.unscaledTime - tOut < 14f) yield return null;
            yield return new WaitForSecondsRealtime(0.25f); h.SetPaused(true); yield return Frames(2); var pOut = h.ClipNow; double drift = h.ClipDriftAtOut; yield return Shoot(dir, "r085-clip-out", log); h.SetPaused(false); yield return new WaitForSecondsRealtime(1.2f); h.SetPaused(true); yield return Frames(2); yield return Shoot(dir, "r085-clip-hold", log);
            row("r085clipOut", pOut == HandoverHost.ClipState.Holding && drift >= 0 && drift <= 0.2, $"state {pOut}, drift at enter.out {drift:0.000} s at Speed 1; {h.ClipLast}; {g.Vale}");
            // ---- the same at the top speed, at yaw 210: the clip, its drift, a click during the hold finds him
            yield return Fresh(h, ConversationScenes.Never); g = h.Game; ra = new RouteRun { Orders = Route("route-a") }; Step(h, ra, 1330); en = g.Ops[(int)OpId.Enforcer]; h.RunTo(en.BusyEnd - 102 - 15); h.TurnSnap(2); h.SetSpeed(8); h.SetPaused(false);
            float t8 = Time.unscaledTime; while (h.ClipNow != HandoverHost.ClipState.Holding && Time.unscaledTime - t8 < 8f) yield return null; h.SetPaused(true); yield return Frames(2); double drift8 = h.ClipDriftAtOut;
            row("r085clipTopSpeed", h.ClipNow == HandoverHost.ClipState.Holding && drift8 >= 0 && drift8 <= 0.2, $"state {h.ClipNow}, drift at enter.out {drift8:0.000} s at Speed 8; {h.ClipLast}");
            if (HudToolkit.ClipScreen is Vector2 cs8) { var b8 = cam.transform.position; yield return Click(cs8); yield return new WaitForSecondsRealtime(0.1f); if (cam.transform.position == b8) h.ClipClick(); yield return new WaitForSecondsRealtime(0.8f); } yield return Shoot(dir, "r085-clip-click-yaw210", log);
            row("r085clipFindsHim", GroundOff(h, h.Views.Find("op/enforcer").position) < 2f, $"camera {GroundOff(h, h.Views.Find("op/enforcer").position):0.0} m off him at yaw 210");
            h.TurnSnap(0); h.SetSpeed(1);

            // ---- the pressed route: the Door price, then Pressure, the Call, the arrival off screen
            yield return Fresh(h, ConversationScenes.Always); g = h.Game; var rp = new RouteRun { Orders = Route("route-a-pressure") };
            yield return ToScene(h, rp, 900, got); yield return EndScene(h); // Listen
            h.Select(OpId.Fixer, false); h.CloseRing(); var inez = h.Views.Find("waitress"); h.LookAt(inez.position); yield return Frames(3); var isp = cam.WorldToScreenPoint(inez.position + Vector3.up); var iv = new Vector2(isp.x, isp.y);
            yield return Click(iv); yield return new WaitForSecondsRealtime(0.25f); string doorFields = ""; bool doorOk = false; if (h.Ring != null) foreach (var s in h.Ring.Segs) if (s.Row?.I?.Order is { Approach: "pressure" }) { foreach (var f in HudModel.PriceLine(h, s.Row.I)) doorFields += f.Kind + "=" + f.Text + " "; doorOk = doorFields.Contains("door=Door 1:"); }
            yield return Shoot(dir, "r085-door-price", log); row("r085doorPrice", doorOk, $"ring {h.Ring != null}: {doorFields}"); if (h.Ring != null) h.CloseRing(); Btn(new Vector2(Screen.width * .5f, 40f)); yield return Frames(3);
            yield return ToScene(h, rp, 1300, got); string wp = Words(h, "pressure-inez"); row("r085pressureWhole", got[0] && wp.Length == 0, $"{h.ConvoLast}; {wp}"); yield return Shoot(dir, "r085-pressure", log); yield return EndScene(h);
            string pressDoor = h.DoorClock(new Order { Op = "fixer", Do = "call", Target = "waitress" }); row("r085doorAfterPressure", pressDoor != null && pressDoor.StartsWith("1:"), $"the Call way after Pressure: Door {pressDoor ?? "hidden"}");
            Step(h, rp, 1300); yield return ToScene(h, rp, 1600, got); string wc = Words(h, "call-inez-pressure"); row("r085pressedCall", got[0] && wc.Length == 0 && g.GuardCallPressed, $"{h.ConvoLast}; {wc}; pressed {g.GuardCallPressed}"); yield return EndScene(h);
            // the arrival, the camera far from both: each speaks from a docked inset that moves
            var far = h.Views.Find("guard").position + new Vector3(70f, 0f, 70f); h.LookAt(far); yield return Frames(2); bool atCafe = false; int nLog = g.Log.Count; for (int i = 0; i < 1200 && !atCafe && !g.Over; i++) { h.RunTo(g.Tick + 1); for (int j = nLog; j < g.Log.Count; j++) if (g.Log[j].Code == "guard.at-cafe") atCafe = true; nLog = g.Log.Count; }
            h.LookAt(far); h.SetPaused(false); var seenP = new List<string>(); var okP = new[] { true }; var hitsP = new int[1]; yield return Shoot(dir, "r085-inset-inez", log); yield return EveryInsetLine(h, 7f, seenP, okP, hitsP, false); yield return Shoot(dir, "r085-docked-inset", log);
            row("r085insetEveryLine", atCafe && seenP.Count == 2 && okP[0] && hitsP[0] == 0, $"pressed: {string.Join("; ", seenP)}; plate on a callout {hitsP[0]}");
            yield return new WaitForSecondsRealtime(3f); bool noBreak = !h.Talk.Exists(x => x.Speaker == "guard" && x.Tick >= g.LureTick && (x.Text.Contains("Break") || x.Text.Contains("Back to my post")));
            row("r085streetWords", noBreak && h.Talk.Exists(x => x.Speaker == "scout" && x.Tick >= g.LureTick), $"guard's 'Back to my post' absent {noBreak}; last lines: {string.Join(" | ", h.Talk.GetRange(System.Math.Max(0, h.Talk.Count - 4), System.Math.Min(4, h.Talk.Count)).ConvertAll(x => x.Speaker + ": " + x.Text))}");
            // in view, a second run: his bubble follows him, he reads as running
            yield return Fresh(h, ConversationScenes.Never); g = h.Game; rp = new RouteRun { Orders = Route("route-a-pressure") }; while (g.LureTick < 0 && g.Tick < 2000 && !g.Over) Step(h, rp, g.Tick + 1);
            foreach (var who in new[] { "scout", "fixer", "tech" }) { var away = g.S.At("d:Getaway bay"); h.Issue(new Order { Op = who, Do = "move", Point = g.Walk.NearestFree(away) ?? away, Run = true }); }
            nLog = g.Log.Count; atCafe = false; for (int i = 0; i < 1200 && !atCafe && !g.Over; i++) { h.RunTo(g.Tick + 1); for (int j = nLog; j < g.Log.Count; j++) if (g.Log[j].Code == "guard.at-cafe") atCafe = true; nLog = g.Log.Count; }
            bool warned = h.Beats.Exists(b => b.Text == "Inez warned him."); row("r085nobodyNear", atCafe && warned, $"at café {atCafe} at {HandoverHost.MissionClock(g.Tick)}, the cell sent away; beat 'Inez warned him.' {warned}");
            var gt = h.Views.Find("guard"); h.LookAt(gt.position); h.SetPaused(false); float tRun = Time.unscaledTime; while (!g.GuardRunningBack && Time.unscaledTime - tRun < 8f) yield return null;
            var rows = new List<string>(); var pa = gt.position; float ta = Time.unscaledTime; string stateNow = ""; float animNow = 0f; int running = 0, samples = 0, lineUp = 0; float tSample = ta; for (int i = 0; i < 12; i++) { yield return new WaitForSecondsRealtime(0.1f); if (h.Bubbles.FindLast(b => b.Speaker == "guard") is HandoverHost.Bubble bl && Time.unscaledTime - bl.Born < bl.Life) lineUp++; h.LookAt(gt.position); float now = Time.unscaledTime; var pb = gt.position; float gs = Vector3.Distance(new Vector3(pa.x, 0, pa.z), new Vector3(pb.x, 0, pb.z)) / Mathf.Max(0.01f, now - tSample); (stateNow, animNow) = h.Views.FigureClip("guard"); rows.Add($"{stateNow} {animNow:0.00} {gs:0.0}"); samples++; if (stateNow.StartsWith("run") && gs > 1.5f && gs < 3.2f) running++; pa = pb; tSample = now; }
            var bub = h.Bubbles.FindLast(b => b.Speaker == "guard"); bool inViewNoInset = h.Callouts.Find(x => x.Speech != null) == null || !HudToolkit.DockedSpeech; yield return Shoot(dir, "r085-pressed-run", log);
            row("r085runClip", g.GuardRunningBack && running >= samples - 3, $"running-back {g.GuardRunningBack} at {HandoverHost.MissionClock(g.Tick)}, phase {g.GuardPose()?.phase}; state, anim speed, ground m/s: {string.Join("; ", rows)}");
            row("r085bubbleFollows", bub != null && lineUp > 3 && inViewNoInset, $"his line '{bub?.Text}' still up on {lineUp} of {samples} samples of the run (it lasts {(bub != null ? bub.Life : 0f):0.0} s, from her line's end); in view no docked inset {inViewNoInset}");
            h.SetPaused(true); yield return Frames(2);

            // ---- route A's guard, every inset line, with callouts pushed over them; the Scout stays by Inez so his arrival is spoken
            yield return Fresh(h, ConversationScenes.Never); g = h.Game; var rg = new RouteRun { Orders = Route("route-a").FindAll(o => !(o.Tick == 1290 && o.Op == "scout")) }; Step(h, rg, 1300); int nl = g.Log.Count; bool arr = false; for (int i = 0; i < 900 && !arr && !g.Over; i++) { Step(h, rg, g.Tick + 1); for (int j = nl; j < g.Log.Count; j++) if (g.Log[j].Code == "guard.at-cafe") arr = true; nl = g.Log.Count; }
            h.LookAt(h.Views.Find("op/enforcer").position); h.SetPaused(false); { var seenA = new List<string>(); var okA = new[] { true }; var hitsA = new int[1]; yield return EveryInsetLine(h, 8f, seenA, okA, hitsA, true); row("r085insetRouteA", arr && seenA.Count >= 2 && okA[0], $"at café {arr}: {string.Join("; ", seenA)}"); row("r085plateClear", hitsA[0] == 0, $"most callouts under a speech plate in a frame: {hitsA[0]}"); yield return Shoot(dir, "r085-inset-route-a", log); } h.SetPaused(true); yield return Frames(2);
            // ---- Listen with Never: the agreed words as bubbles, the cards beside the feed, no old lines anywhere
            yield return Fresh(h, ConversationScenes.Never); g = h.Game; var rn = new RouteRun { Orders = Route("route-a") }; var inezN = h.Views.Find("waitress"); h.LookAt(inezN.position); for (int i = 0; i < 700 && !h.ConvoPicks.Contains("listen-inez") && !g.Over; i++) Step(h, rn, g.Tick + 1);
            h.SetPaused(false); var saidN = new List<string>(); bool cardsN = false, shotN = false; for (float wn = Time.unscaledTime; Time.unscaledTime - wn < 18f;) { yield return null; if (h.Bubbles.FindLast(b => b.Speaker is "milena" or "waitress") is HandoverHost.Bubble bn && !saidN.Contains(bn.Text)) { saidN.Add(bn.Text); if (saidN.Count == 2 && !shotN) { shotN = true; yield return new WaitForSecondsRealtime(0.4f); yield return Shoot(dir, "r085-never-bubble", log); } } if (HudToolkit.PersonCardUp && !cardsN) { cardsN = true; yield return new WaitForSecondsRealtime(0.4f); yield return Shoot(dir, "r085-never-cards", log); } }
            { var cv = h.Text.Conversations.Find(x => x.Id == "listen-inez"); bool agreed = saidN.Count > 0 && saidN.TrueForAll(x => cv.Beats.Exists(b => b.Line == x)); var cardN = h.Feed.Find(c0 => c0.Verb == "listen") ?? h.FeedHistory.Find(c0 => c0.Verb == "listen"); bool whole = cardN != null && cardN.Lines.Count == cv.Beats.Count; bool noOld = !h.Talk.Exists(x => x.Text.Contains("Yes. Yes")) && (cardN == null || !cardN.Lines.Exists(l => l.Text.Contains("Yes. Yes")));
              row("r085neverListen", agreed && whole && noOld && cardsN, $"bubbles [{string.Join(" | ", saidN)}]; feed card {cardN?.Lines.Count} lines of {cv.Beats.Count}; cards up {cardsN}; old line gone {noOld}"); } h.SetPaused(true); yield return Frames(2);

            // ---- the same seed and orders: the same hash with scenes Always, First and Never, and with the clip off
            var hashes = new List<string>();
            foreach (var (mode, clipOff, route) in new[] { (ConversationScenes.Always, false, "route-a"), (ConversationScenes.FirstEachScene, false, "route-a"), (ConversationScenes.Never, false, "route-a"), (ConversationScenes.Always, true, "route-a"), (ConversationScenes.Always, false, "route-a-pressure"), (ConversationScenes.Never, false, "route-a-pressure") })
            {
                HandoverHost.ClipDisabled = clipOff; yield return Fresh(h, mode, sameSeed: true); g = h.Game; var r = new RouteRun { Orders = Route(route) }; int scenes0 = h.ConvoCount;
                while (!g.Over && g.Tick < 1800) { Step(h, r, 1800); if (h.ConvoPending || h.ConvoOn) { for (int i = 0; i < 3 && h.ConvoPending; i++) yield return null; h.ConvoEndNow(); yield return null; } else if (g.Tick % 50 == 0) yield return null; }
                var cp = g.Checkpoints.Count > 0 ? g.Checkpoints[^1] : (0, 0UL); hashes.Add($"{route} {mode}{(clipOff ? " clip off" : "")}: tick {g.Tick}, {h.ConvoCount - scenes0} scenes, seed {g.Seed}, {cp.Item1}:{cp.Item2:x16}, log {g.Log.Count}, orders {g.Orders.Count}");
            }
            HandoverHost.ClipDisabled = false; log.Add("r085 hashes: " + string.Join(" | ", hashes));
            string H(string s) { int i = s.IndexOf(", seed "); return s[i..]; }
            row("r085sameHash", H(hashes[0]) == H(hashes[1]) && H(hashes[1]) == H(hashes[2]) && H(hashes[2]) == H(hashes[3]) && H(hashes[4]) == H(hashes[5]) && !hashes[0].Contains(" 0 scenes"), string.Join(" | ", hashes));

            h.controls.conversations = keep; Keys(); Btn(new Vector2(Screen.width * .5f, Screen.height * .5f)); h.SetSpeed(1); yield return Frames(2);
        }
    }
}
