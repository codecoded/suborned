using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>GDD v0.24 §10 (Sky picked (h), 2026-10-18): the civic block stays as built and the Records Office front is played at yaw 210.
    /// At the opening yaw 30 the public door is behind the Office's wings. This is what the view knows about that door:
    ///  · doorSeen: for each of the four yaws (30, 120, 210, 300) the share of a 1.75 m figure at the door seen past architecture, without
    ///    the fade (V-1's measure, tools/missions/handover/visibility.ts), measured on the scene's own meshes at install (DoorSight, Editor) and
    ///    baked on the host; "clear" is V-1's limit, half the figure;
    ///  · the door in clear view: resting at a clear yaw with the door marker on screen; H13's door cause retires after doorClearS of it;
    ///  · TurnToDoor: the door marker's turn glyph turns the view in quarter steps to the nearest clear yaw, the door held where it stands
    ///    on screen while the street turns round it.
    /// View only: the engine, the orders and the replays never see it.</summary>
    public sealed partial class HandoverHost
    {
        [Header("GDD v0.24: the Office door's view")]
        [Tooltip("Share of a 1.75 m figure at the public door seen past architecture (no fade) at yaw 30, 120, 210, 300; baked at install (PlayInstall, DoorSight).")]
        public float[] doorSeen = new float[0];
        [Tooltip("The yaw the Office front is played at (GDD v0.24 §10: 210; the mission's `yaw` on the Office entrance when it carries one): the door turn's goal when the door is clear there.")]
        public int doorPlayedYaw = 210;
        [Tooltip("Seconds the door must stand in clear view before H13's door cause is spent.")]
        public float doorClearS = 2f;

        /// <summary>V-1's limit: half the figure seen.</summary>
        public const float DoorClearLimit = 0.5f;
        /// <summary>The door is clear at view k (0 = the opening yaw 30, then each 90° clockwise). Unbaked (an old scene): the GDD's yaw 210.</summary>
        public bool DoorClearAt(int k) => doorSeen != null && doorSeen.Length == 4 ? doorSeen[((k % 4) + 4) % 4] >= DoorClearLimit : ((k % 4) + 4) % 4 == 2;
        /// <summary>A wall hides the door at the view the turn rests on (or is heading to).</summary>
        public bool DoorHiddenNow => !DoorClearAt(YawIndex);
        /// <summary>The door's world point (the door marker's ground), else null.</summary>
        Vector3? DoorWorld => Game == null ? null : frame.World(Game.S.At(Game.S.GuardPost));

        /// <summary>Quarter turns (+ = E, − = Q) from the view now to the played yaw (doorPlayedYaw) when the door is clear there, else to the
        /// nearest clear view; two either way go by Q (Fable's "Q Q"); 0 when the door is clear now, or nowhere.</summary>
        public int DoorTurnSteps()
        {
            int now = YawIndex; if (DoorClearAt(now)) return 0;
            int played = Mathf.RoundToInt(Mathf.Repeat(doorPlayedYaw - baseYaw, 360f) / 90f) % 4;
            if (DoorClearAt(played)) { int d = ((played - now) % 4 + 4) % 4; return d == 3 ? -1 : d == 2 ? -2 : d; }
            foreach (var s in new[] { -1, 1, -2, 2 }) if (DoorClearAt(now + s)) return s;
            return 0;
        }

        /// <summary>The keys of that turn as hint tokens ("&lt;TurnLeft&gt; &lt;TurnLeft&gt;"), else "".</summary>
        public string DoorTurnKeys()
        {
            int s = DoorTurnSteps(); var k = s < 0 ? "<TurnLeft>" : "<TurnRight>";
            return s == 0 ? "" : string.Join(" ", System.Linq.Enumerable.Repeat(k, Mathf.Abs(s)));
        }

        float doorClearFor;
        /// <summary>Seconds the door has stood in clear view (resting at a clear yaw, the marker on screen), unscaled: a pause counts.</summary>
        public float DoorClearFor => doorClearFor;

        /// <summary>Per frame: the clear-view clock, and H13's door cause spent once it reaches doorClearS (here, not in Hints(): a paused player
        /// turns the view too).</summary>
        void DoorViewFrame()
        {
            bool clear = false;
            if (cam && !Following && !Turning && Game != null && DoorWorld is Vector3 d && DoorClearAt(YawIndex))
            {
                var vp = cam.WorldToViewportPoint(d + Vector3.up * 1.2f);
                clear = vp.z > 0f && vp.x > 0.04f && vp.x < 0.96f && vp.y > 0.04f && vp.y < 0.96f;
            }
            doorClearFor = clear ? doorClearFor + Mathf.Min(Time.unscaledDeltaTime, 0.1f) : 0f;
            if (HintNow is { Id: "H13", Cause: "door" } && doorClearFor >= doorClearS) { hintsDone.Add(H13Door); Learn(H13Door); HintNow = null; }
        }

        int turnQueue; Vector3? turnAnchor; Vector3 turnAnchorOff; float turnAnchorYaw;
        /// <summary>The door marker's turn glyph: quarter turns to the nearest view where the door is clear, eased one after the other, the door
        /// held where it stands on screen.</summary>
        public void TurnToDoor()
        {
            TurnInit(); int s = DoorTurnSteps(); if (s == 0 || !cam || DoorWorld is not Vector3 d) return;
            var g = new Vector3(d.x, 0f, d.z);
            turnAnchor = g; turnAnchorOff = new Vector3(focus.x, 0f, focus.z) - g; turnAnchorYaw = yawNow;
            int one = s > 0 ? 1 : -1; turnQueue = s - one; BeginTurn(yawTo + 90f * one);
        }

        /// <summary>ApplyTurn's focus while anchored: the anchor's ground point keeps its place on screen as the view turns about it.</summary>
        bool AnchoredFocus()
        {
            if (turnAnchor is not Vector3 a) return false;
            var f = a + Quaternion.Euler(0f, yawNow - turnAnchorYaw, 0f) * turnAnchorOff; focus = new Vector3(f.x, 0f, f.z); return true;
        }

        /// <summary>A quarter of a queued turn finished: the next one, or the anchor lets go.</summary>
        void TurnQueueNext()
        {
            if (turnQueue == 0) { turnAnchor = null; return; }
            int one = turnQueue > 0 ? 1 : -1; turnQueue -= one; yawFrom = yawNow; yawTo = yawNow + 90f * one; turnStart = Time.unscaledTime;
        }

        /// <summary>The player took the view (a key, a drag, a pan or a centring): a queued door turn stops where it is.</summary>
        void TurnQueueCancel() { turnQueue = 0; turnAnchor = null; }
    }
}
