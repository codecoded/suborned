using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using OpId = Suborned.Sim.OpId;
using Pt = Suborned.Sim.Pt;
using G = Suborned.Sim.G;

namespace Suborned.Play
{
    /// <summary>Brief 085, Sky's play notes on 0.2.177, the input path bun run hostlogic can't reach (it calls the Give methods): through the Input System's own devices, in r083's run.
    ///  · squad: T with the squad selected keeps it, 3 changes who is in hand and keeps it, T back finds it on the map.
    ///  · fire: a right hold in third person with the squad, nobody drawn, no F: all four take the order and draw.
    ///  · guard: at his post in the door's box, the pointer on him and 20 px beside him hovers him with a pill, a click 20 px beside him opens his ring; further off the box is still the door's; third person's focus is him. Pictures `r085-*`.</summary>
    public static class Proof085
    {
        internal static void Keys(params Key[] down) { if (Keyboard.current is Keyboard kb) InputSystem.QueueStateEvent(kb, new KeyboardState(down)); }
        internal static void Mouse2(Vector2 sp, bool left = false, bool right = false) { if (Mouse.current is Mouse m) { var st = new MouseState { position = sp }; if (left) st = st.WithButton(MouseButton.Left, true); if (right) st = st.WithButton(MouseButton.Right, true); InputSystem.QueueStateEvent(m, st); } }
        internal static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        internal static IEnumerator Press(Key k) { Keys(k); yield return Frames(3); Keys(); yield return Frames(3); }
        internal static IEnumerator Fresh(HandoverHost h)
        {
            HandoverHost.PointerWorld = null; HandoverHost.PointerOverride = null; Keys(); Mouse2(new Vector2(Screen.width * .5f, Screen.height * .5f));
            if (h.Driving) h.DriveLetGoToMap(); if (h.Following) h.FollowToggle();
            h.Begin(); h.IntelOpen = false; h.StartClock(); h.SetPaused(true); yield return Frames(3);
            h.View.orthographicSize = 10f; h.Select(OpId.Scout, false); yield return Frames(2);
        }
        static IEnumerator Shoot(string dir, string name, List<string> log) { while (HudToolkit.ShotBusy) yield return null; log.Add($"{name}: " + HudToolkit.Shoot(Path.Combine(dir, name), "rest")); yield return null; while (HudToolkit.ShotBusy) yield return null; }
        static string Sel(HandoverHost h) { var s = ""; for (int k = 0; k < 4; k++) s += h.Selected[k] ? "1" : "0"; return s; }
        /// <summary>Run the clock until the guard stands at his post in the door's box (his café stop over), up to the mission's first minutes; true when he does.</summary>
        internal static bool GuardAtPost(HandoverHost h)
        {
            var g = h.Game; var post = g.S.At(g.S.GuardPost);
            for (int t = 0; t < 400 && !g.Over; t++) { if (g.GuardPose()?.pos is Pt p && G.Dist(p, post) < 40) return true; h.RunTo(g.Tick + 5); }
            return false;
        }

        public static IEnumerator Run(HandoverHost h, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            // ---- squad: T keeps it, on the way in and back
            yield return Fresh(h); var g = h.Game; h.SetPaused(false); yield return Press(Key.Digit5); string s0 = Sel(h);
            yield return Press(Key.T); yield return new WaitForSecondsRealtime(0.7f); bool inHand = h.Driving; string s1 = Sel(h); int a1 = h.DriveAgent;
            yield return Press(Key.Digit3); yield return Frames(3); string s2 = Sel(h); int a2 = h.DriveAgent;
            yield return Press(Key.T); yield return new WaitForSecondsRealtime(0.8f); string s3 = Sel(h);
            row("r085squad", s0 == "1111" && inHand && s1 == "1111" && a1 == 0 && s2 == "1111" && a2 == 2 && !h.Following && s3 == "1111", $"selected {s0}; T {s1} in hand {a1}; 3 {s2} in hand {a2}; T back {s3}, following {h.Following}");

            // ---- fire: a right hold in third person, nobody drawn
            yield return Fresh(h); g = h.Game; bool post = GuardAtPost(h); var near = g.S.At(g.S.GuardPost);
            h.SelectAll(); h.GroupWalk(new Pt(near.X + 600, near.Y + 700), false, null); h.SetPaused(false); for (int i = 0; i < 200 && (g.Ops[0].Walk != null || g.Ops[3].Walk != null); i++) h.RunTo(g.Tick + 5); h.SetPaused(true); yield return Frames(2);
            h.SetPaused(false); yield return Press(Key.T); yield return new WaitForSecondsRealtime(0.7f); h.SetPaused(true);
            var gt = h.Views.Find("guard"); h.FollowFace(gt.position); yield return Frames(3); var gsp = h.View.WorldToScreenPoint(gt.position + Vector3.up); var gv = new Vector2(gsp.x, gsp.y);
            int d0 = 0; for (int k = 0; k < 4; k++) if (g.Ops[k].Gun.Drawn) d0++;
            string under = h.ProbeAt(gv); yield return Shoot(dir, "r085-third-squad", log);
            Mouse2(gv); yield return Frames(3); Mouse2(gv, right: true); yield return new WaitForSecondsRealtime(HandoverHost.ChargeS + HandoverHost.RightTapS + 0.25f); Mouse2(gv); yield return Frames(4);
            int took = 0, drew = 0; string each = ""; for (int k = 0; k < 4; k++) { var gu = g.Ops[k].Gun; if (gu.Order.Length > 0) took++; if (gu.Drawn) drew++; each += $"{k}:'{gu.Order}'/{h.FireRefusalOf(k).code} "; }
            row("r085fire", post && d0 == 0 && took == 4 && drew == 4, $"at post {post}, drawn before {d0}, took {took}, drew {drew} ({each}), under the pointer {under}");
            if (h.Driving) h.DriveLetGoToMap(); yield return Frames(2);

            // ---- guard: hover and click at his post
            yield return Fresh(h); g = h.Game; bool at2 = GuardAtPost(h); gt = h.Views.Find("guard"); h.LookAt(gt.position); yield return Frames(3);
            var cam = h.View; var a0 = cam.WorldToScreenPoint(gt.position + Vector3.up); var c = new Vector2(a0.x, a0.y); var hover = new List<string>(); bool allGuard = true; float kp = Screen.height / 1080f; var side = new Vector2(20f * kp, 0f); var box = new Vector2(40f * kp, 0f); // kit px at 1080p: the pick circle is 28, his body about 22, the box 65 each side at play zoom
            foreach (var off in new[] { Vector2.zero, side, -side, new Vector2(0, -20f * kp), new Vector2(14f * kp, 14f * kp) })
            {
                Mouse2(c + off); yield return Frames(4); string w = HudToolkit.PillWords; bool him = h.Hover?.Key == "guard"; allGuard &= him && w.Length > 0; hover.Add($"{off.x:0},{off.y:0}: {h.Hover?.Kind}/{h.Hover?.Key} '{w}'");
            }
            // coming in as a hand does: off the street, into the door's box, then onto him: the box never holds the pill (the note's cause: it stayed the door)
            Mouse2(new Vector2(Screen.width * .5f, 40f)); yield return new WaitForSecondsRealtime(0.3f); Mouse2(c + box); yield return new WaitForSecondsRealtime(0.25f); string inBox = $"{h.Hover?.Kind}/{h.Hover?.Key}"; Mouse2(c); yield return Frames(4); string fromBox = $"{h.Hover?.Kind}/{h.Hover?.Key} '{HudToolkit.PillWords}'"; bool boxLets = h.Hover?.Key == "guard" && HudToolkit.PillWords.Length > 0;
            row("r085guardFromBox", inBox == "door/door" && boxLets, $"in the box {inBox}, then on him {fromBox}");
            Mouse2(c + side); yield return Frames(4); yield return Shoot(dir, "r085-guard-hover", log);
            row("r085guardHover", at2 && allGuard, string.Join("; ", hover));
            Mouse2(c + side, left: true); yield return Frames(2); Mouse2(c + side); yield return Frames(4); bool ring = h.Ring != null && h.Ring.Plan.T.Key == "guard"; string rk = h.Ring?.Plan.T.Key ?? "none"; yield return Shoot(dir, "r085-guard-ring", log); if (h.Ring != null) h.CloseRing(); yield return Frames(2);
            row("r085guardClick", ring, $"ring on {rk}");
            // the Enforcer, whose calm way here is the door: on his body the guard (his takedown is in the ring), the box round him the door's
            h.Select(OpId.Enforcer, false); Mouse2(new Vector2(Screen.width * .5f, 40f)); yield return new WaitForSecondsRealtime(0.3f); Mouse2(c); yield return Frames(4); string enfOn = $"{h.Hover?.Kind}/{h.Hover?.Key} '{HudToolkit.PillWords}'"; bool enfGuard = h.Hover?.Key == "guard"; yield return Shoot(dir, "r085-guard-enforcer", log);
            Mouse2(new Vector2(Screen.width * .5f, 40f)); yield return new WaitForSecondsRealtime(0.3f); /* the pill's 0.15 s stick */ Mouse2(c + box); yield return Frames(4); string enfBox = $"{h.Hover?.Kind}/{h.Hover?.Key} '{HudToolkit.PillWords}'"; bool enfDoor = h.Hover?.Kind == "door";
            row("r085doorStays", enfGuard && enfDoor, $"Enforcer on him: {enfOn}; 40 px beside: {enfBox}");
            // third person: the focus is the guard, not the box he stands in
            h.Select(OpId.Scout, false); h.Issue(new Suborned.Sim.Order { Op = "scout", Do = "move", Point = new Pt(near.X + 300, near.Y + 300) }); h.SetPaused(false); for (int i = 0; i < 200 && g.Ops[0].Walk != null; i++) h.RunTo(g.Tick + 5);
            Mouse2(new Vector2(4, 4)); yield return Press(Key.T); yield return new WaitForSecondsRealtime(0.7f); h.SetPaused(true); h.FollowFace(gt.position); yield return Frames(6);
            string fk = $"{h.DriveFocus?.Kind}/{h.DriveFocus?.Key}"; row("r085guardFocus", !(h.DriveFocus?.Kind == "door"), $"driving {h.Driving}, focus {fk}");
            if (h.Driving) h.DriveLetGoToMap(); yield return Frames(2); Keys(); Mouse2(new Vector2(Screen.width * .5f, Screen.height * .5f)); yield return Frames(2);
            yield return Proof085Squad.Run(h, row); // Sky on 0.2.212: the ring's Fire and the ring in third person
            yield return Proof085Scenes.Run(h, dir, log, row); // whole conversations, bystanders, the pressed call, the insets, the capture clip, the Door price
            yield return Proof085Outline.Run(h, dir, log, row); // brief 101: the hover outline (the guard, the door, one of the cell, the mast's head), no focus needed
        }
    }
}
