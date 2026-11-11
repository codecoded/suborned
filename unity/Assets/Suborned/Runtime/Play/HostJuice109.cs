using System;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 109: the host's few words to the juice (Juice109), all view only: an order taken, a social win, a shot, and the camera
    /// following the getaway out after the Go. Nothing here changes an order, a tick or a decision; with the juice off (or in the host logic) each
    /// call returns at its first line.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>An order the engine took (IssueCore): where the agent sets off and where it goes.</summary>
        void JuiceOrder(Order o, Vector3? target)
        {
            if (!Juice109.Live || Game == null || o == null) return;
            Vector3? from = IsOpName(o.Op) && views?.Find("op/" + o.Op) is Transform t ? t.position : (Vector3?)null;
            Vector3? to = target ?? (o.Point.HasValue ? frame.World(o.Point.Value) : !string.IsNullOrEmpty(o.To) && Game.S.HasPlace(o.To) ? frame.World(Game.S.Stand(o.To)) : (Vector3?)null);
            if (to.HasValue && from.HasValue) to = new Vector3(to.Value.x, from.Value.y, to.Value.z); // the agent's level: the frame's point is the street's
            Juice109.Order(from, to, o.Do);
        }

        /// <summary>A new log line (Drain): a social win puts the signature on whoever it landed on (round 4: the turn's flare, used more, varied by kind;
        /// Juice109Fx.SocialWin). The person comes from the line: "(role)" for a person, the first word for a rival, the code's agent for a Watch.</summary>
        void JuiceLog(LogLine x)
        {
            if (!Juice109.Live) return; string c = x.Code, kind = null, key = null;
            string Paren() { int a = x.Text.IndexOf('('), b = a >= 0 ? x.Text.IndexOf(')', a + 1) : -1; return b > a + 1 ? x.Text.Substring(a + 1, b - a - 1) : null; }
            string First() { int sp = x.Text.IndexOf(' '); return sp > 0 ? x.Text[..sp] : null; }
            if (c.StartsWith("turn.", StringComparison.Ordinal) && c.EndsWith(".turned", StringComparison.Ordinal)) { kind = "turn"; key = Paren(); }
            else if (c == "rivals.paid") { kind = "paid"; key = First(); }
            else if (c == "rivals.misled") { kind = "misled"; key = First(); if (views?.Find(key ?? "") == null) foreach (var m in Game.Crew) if (m.Role == "lookout") key = m.Id; }
            else if (c.EndsWith(".listen.heard", StringComparison.Ordinal)) { kind = "heard"; key = c.StartsWith("rivals.", StringComparison.Ordinal) ? First() : Paren(); }
            else if (c.EndsWith(".watch.watched", StringComparison.Ordinal)) { kind = "watched"; key = "op/" + c[..c.IndexOf('.')]; }
            else if (c.StartsWith("call.", StringComparison.Ordinal) && c.Length > 5) { kind = "call"; key = c[5..]; }
            else if (c == "lend.lent") { kind = "lent"; key = "car/official"; }
            if (kind == null || key == null) return;
            if (views?.Find(key) is Transform who && who.gameObject.activeInHierarchy) Juice109.SocialWin(who, kind);
            else Debug.Log($"Juice109: {c}: no drawn \"{key}\" for the social win");
        }

        /// <summary>HostFight.Shot: every engine shot, at the gun and where it went; glass when it hit a car or a camera.</summary>
        void JuiceShot(Vector3 src, Vector3 dst, bool hit, bool covered, string target)
        {
            if (!Juice109.Live) return; var k = ViewKey(target);
            Juice109.Shot(src, dst, hit, covered, k.StartsWith("car/", System.StringComparison.Ordinal) || k.StartsWith("camera/", System.StringComparison.Ordinal));
            if (hit && views?.Find(k) is Transform body && body.gameObject.activeInHierarchy) Juice109.HitFlash(body); // round 4: the body hit flashes (rivals, guards, police, ours)
        }

        bool juiceFollowing;
        /// <summary>Juice109Fx after the Go: ease the view's centre onto the car, as the portrait click's glide does (CamFollowFrame). False once the
        /// player touches the view (a pan key, the wheel, a middle or left drag, a turn): the follow stops for good, the view is theirs. The host's own
        /// moves (an end's camera restore) don't stop it.</summary>
        public bool JuiceFollow(Vector3 car)
        {
            if (!cam || Following || Game == null) return false;
            var kb = UnityEngine.InputSystem.Keyboard.current; var ms = UnityEngine.InputSystem.Mouse.current; var c = controls;
            bool touched = kb != null && (c.Held(kb, PlayAction.PanUp) || c.Held(kb, PlayAction.PanDown) || c.Held(kb, PlayAction.PanLeft) || c.Held(kb, PlayAction.PanRight) || c.Held(kb, PlayAction.TurnLeft) || c.Held(kb, PlayAction.TurnRight))
                || ms != null && (ms.scroll.ReadValue().y != 0f || ms.middleButton.isPressed || ms.leftButton.isPressed);
            if (touched) { juiceFollowing = false; return false; }
            var g = new Vector3(car.x, 0f, car.z);
            focus = Vector3.Lerp(focus, g, 1f - Mathf.Exp(-Mathf.Min(Time.unscaledDeltaTime, 0.1f) / 0.35f)); TurnLookAt(focus); PlaceCamera();
            if (!juiceFollowing) Debug.Log($"Juice109: the view follows the getaway car out (from {focus:F1})");
            juiceFollowing = true; return true;
        }
        public void JuiceFollowEnd() => juiceFollowing = false;
    }
}
