using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 109: the host's few words to the juice (Juice109), all view only: an order taken, a turn landing, a shot, and the camera
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

        /// <summary>A new log line (Drain): a turn landing (turn.<approach>.turned) puts a warm flare on the person turned, named in the line's "(role)".</summary>
        void JuiceLog(LogLine x)
        {
            if (!Juice109.Live || !x.Code.StartsWith("turn.", System.StringComparison.Ordinal) || !x.Code.EndsWith(".turned", System.StringComparison.Ordinal)) return;
            int a = x.Text.IndexOf('('), b = a >= 0 ? x.Text.IndexOf(')', a + 1) : -1; if (b <= a + 1) return;
            if (views?.Find(x.Text.Substring(a + 1, b - a - 1)) is not Transform w0 || !w0.gameObject.activeInHierarchy) Debug.Log($"Juice109: {x.Code}: no drawn \"{x.Text.Substring(a + 1, b - a - 1)}\" for the flare");
            if (views?.Find(x.Text.Substring(a + 1, b - a - 1)) is Transform who && who.gameObject.activeInHierarchy) Juice109.Turned(who);
        }

        /// <summary>HostFight.Shot: every engine shot, at the gun and where it went; glass when it hit a car or a camera.</summary>
        void JuiceShot(Vector3 src, Vector3 dst, bool hit, bool covered, string target)
        {
            if (!Juice109.Live) return; var k = ViewKey(target);
            Juice109.Shot(src, dst, hit, covered, k.StartsWith("car/", System.StringComparison.Ordinal) || k.StartsWith("camera/", System.StringComparison.Ordinal));
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
