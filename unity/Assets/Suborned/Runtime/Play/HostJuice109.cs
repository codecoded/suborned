using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 109: the host's few words to the juice (Juice109), all view only: an order taken, a takedown landing from the log, and the camera
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

        /// <summary>A new log line (Drain): a takedown landing shakes the view where the Enforcer stands.</summary>
        void JuiceLog(LogLine x)
        {
            if (!Juice109.Live || x.Code is not ("takedown.out" or "takedown.dead")) return;
            var at = views?.Find("op/enforcer") is Transform t ? t.position : focus;
            Juice109.Takedown(at, x.Tick);
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
