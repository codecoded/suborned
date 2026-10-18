using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>A third-person follow view, as a test only (Sky 2026-10-18: "yeah, just as a test"; a toggle, not a design change: the overhead
    /// view stays the default and GDD §10's camera limits stand). T (PlayAction.FollowView) swings a perspective camera in behind the selected
    /// agent (the first selected, else the Scout), a few metres back and up, looking down the street at about shoulder height; T again puts
    /// back the exact overhead view that was left (position, rotation, projection, zoom, clip planes, focus, yaw). View only: the engine, the
    /// orders and the replays never see it.
    /// In the view: Q / E turn it round the agent (45° steps, eased), Alt + middle-drag turns it freely, the wheel sets the distance
    /// (followMin to followMax); left click moves, right click acts, as overhead (picking and ground rays are perspective-safe). WASD, the
    /// middle-drag pan and the edge pan do nothing. The HUD's world marks are sized for the orthographic camera and hide (speech and pops stay);
    /// the screen-edge HUD stays. The architecture fade makes no cut for a perspective camera (ArchitectureFade.Cuts), so buildings draw whole;
    /// the tactical shadow fit (TacticalShadows, built for the overhead stand-off) is off, so the URP asset's own shadow distance applies.</summary>
    public sealed partial class HandoverHost
    {
        [Header("Test: the third-person follow view (T)")]
        [Tooltip("Vertical field of view (degrees) in the follow view.")]
        public float followFov = 55f;
        [Tooltip("Distance (m) behind the agent: the default, and the wheel's range.")]
        public float followDistance = 5.5f, followMin = 3f, followMax = 10f;
        [Tooltip("Camera height above the agent's feet: base (m) plus this share of the distance.")]
        public float followHeight = 1.3f, followRise = 0.32f;
        [Tooltip("The camera looks at a point this far ahead of the agent (m), at this height (m): down the street at about shoulder height.")]
        public float followAhead = 8f, followLookHeight = 1.35f;
        [Tooltip("Far clip (m) in the follow view.")]
        public float followFar = 600f;
        [Tooltip("Q / E in the follow view: degrees a press.")]
        public float followTurnStep = 45f;
        [Tooltip("Seconds the camera takes to catch up with the agent (smooth follow), and to swing in.")]
        public float followSmooth = 0.2f, followSwing = 0.5f;

        /// <summary>The follow view is on (the HUD hides its world marks; the camera paths that assume an orthographic camera stand down).</summary>
        public bool Following { get; private set; }
        /// <summary>The agent the follow view is behind (-1 when off).</summary>
        public int FollowAgent => Following ? fAgent : -1;

        struct Overhead { public Vector3 pos, focus; public Quaternion rot; public bool ortho, shadows; public float size, fov, near, far; }
        Overhead left;
        float fYaw, fYawFrom, fYawTo, fTurnStart = -1f, fDist, fSwingStart, fDragX; bool fDragging; int fAgent = -1;
        Vector3 fPivot, fPivotVel; Vector3 fSwingFromPos; Quaternion fSwingFromRot;

        /// <summary>Toggle the follow view (T, or a script).</summary>
        public void FollowToggle() { if (Following) FollowExit(); else FollowEnter(); }

        void FollowEnter()
        {
            if (!cam || Game == null || Following) return;
            if (turnStart >= 0f) { turnStart = Time.unscaledTime - turnSeconds - 1f; ApplyTurn(); } // a quarter turn under way lands first: the view left is a resting one
            turnDragging = false; grabbing = false;
            left = new Overhead { pos = cam.transform.position, rot = cam.transform.rotation, focus = focus, ortho = cam.orthographic, size = cam.orthographicSize, fov = cam.fieldOfView, near = cam.nearClipPlane, far = cam.farClipPlane, shadows = shadows && shadows.enabled };
            fAgent = FollowWho(); fPivot = FollowPoint(fAgent) ?? focus; fPivotVel = Vector3.zero;
            fYaw = fYawFrom = fYawTo = yawNow; fTurnStart = -1f; fDist = fDistNow = followDistance; fDragging = false; // behind the agent, looking the way the overhead view looked
            fSwingFromPos = cam.transform.position; fSwingFromRot = cam.transform.rotation; fSwingStart = Time.unscaledTime;
            cam.orthographic = false; cam.fieldOfView = followFov; cam.nearClipPlane = 0.2f; cam.farClipPlane = followFar;
            if (shadows) shadows.enabled = false;
            Following = true; intentKey = "";
            fSwingFromPos = fPivot + (fSwingFromPos - focus).normalized * 60f; // the swing starts high on the overhead view's line through the agent
            FollowPlace(true);
            Status = $"Follow view: {Suborned.Sim.Op.Name((Suborned.Sim.OpId)fAgent)} ({controls.Label(PlayAction.FollowView)} for the overhead view)";
        }

        void FollowExit()
        {
            if (!Following) return;
            Following = false; fAgent = -1; fDragging = false; FollowUnhide();
            cam.orthographic = left.ortho; cam.orthographicSize = left.size; cam.fieldOfView = left.fov; cam.nearClipPlane = left.near; cam.farClipPlane = left.far;
            cam.transform.SetPositionAndRotation(left.pos, left.rot);
            focus = left.focus; if (fade) fade.focus = focus;
            if (shadows) { shadows.enabled = left.shadows; shadows.focusDepth = standOff; }
            intentKey = ""; Status = "Overhead view";
        }

        /// <summary>The selected agent (the hotbar's), else the first selected, else the Scout.</summary>
        int FollowWho() { int a = (int)Agent; if (Selected[a]) return a; for (int k = 0; k < 4; k++) if (Selected[k]) return k; return 0; }

        /// <summary>Where the agent stands now (the drawn, interpolated capsule), or null while inside or not drawn.</summary>
        Vector3? FollowPoint(int k)
        {
            if (views == null || k < 0) return null;
            var t = views.Find("op/" + Suborned.Sim.Op.Name((Suborned.Sim.OpId)k));
            return t && t.gameObject.activeSelf ? t.position : (Vector3?)null;
        }

        /// <summary>The keys and the wheel in the follow view (T toggles in either view: see FollowKey).</summary>
        void FollowInput(Keyboard kb, UnityEngine.InputSystem.Mouse mouse, bool overHud)
        {
            if (kb != null && !fDragging)
            {
                if (controls.Down(kb, PlayAction.TurnLeft)) FollowTurn(-1);
                if (controls.Down(kb, PlayAction.TurnRight)) FollowTurn(1);
            }
            if (mouse == null) return;
            float x = mouse.position.ReadValue().x;
            if (!fDragging && controls.Held(kb, PlayAction.TurnDrag) && mouse.middleButton.wasPressedThisFrame) { fDragging = true; fDragX = x; fTurnStart = -1f; }
            if (fDragging)
            {
                if (!mouse.middleButton.isPressed) fDragging = false;
                else { fYaw += (x - fDragX) * turnDragDegPerPixel * 1080f / Mathf.Max(1f, Screen.height); fYawFrom = fYawTo = fYaw; fDragX = x; }
            }
            float wheel = mouse.scroll.ReadValue().y;
            if (wheel != 0f && !overHud) fDist = Mathf.Clamp(fDist - Mathf.Sign(wheel) * 0.75f, followMin, followMax);
        }

        /// <summary>Scripted shots and the proof: turn the follow view (no ease, no swing) to look from the agent towards a world point.</summary>
        public void FollowFace(Vector3 world)
        {
            if (!Following) return; var d = world - fPivot; d.y = 0f; if (d.sqrMagnitude < 0.01f) return;
            fYaw = fYawFrom = fYawTo = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; fTurnStart = -1f; fSwingStart = -100f;
            if (FollowPoint(fAgent) is Vector3 p) { fPivot = p; fPivotVel = Vector3.zero; }
            FollowPlace(true);
        }

        [Tooltip("Follow view: a prop (a tree, a lamp: no wider than this, m) between the agent and the camera, or this close to the camera, is hidden while it is in the way; anything wider on the line pulls the camera in.")]
        public float followPropWidth = 6f, followPropNear = 1.6f;
        float fDistNow = 5.5f;
        // the city has no colliders: its tall static renderers, indexed once by 8 m cells of the ground (built on the first follow), are the test
        Dictionary<(int, int), List<(Renderer r, Bounds b, bool prop)>> fIndex; readonly HashSet<Renderer> fHidden = new(), fHideNow = new(), fSeen = new();
        static (int, int) FCell(float x, float z) => (Mathf.FloorToInt(x / 8f), Mathf.FloorToInt(z / 8f));

        void FollowIndex()
        {
            if (fIndex != null) return; fIndex = new();
            foreach (var r in FindObjectsByType<MeshRenderer>())
            {
                if (!r.enabled || r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly || r.transform.IsChildOf(transform)) continue; // the engine's views move; shadow proxies cast only
                var b = r.bounds; float wide = Mathf.Max(b.size.x, b.size.z); if (b.size.y < 1.6f || wide > 60f) continue; // low things the camera sees over; the ground and the horizon
                bool kit = r.name.StartsWith("DK_") || r.transform.parent && r.transform.parent.name.StartsWith("DK_"); bool prop = kit && wide <= followPropWidth; /* kit props (and their LODs); facades and masses are walls */ var (x0, z0) = FCell(b.min.x, b.min.z); var (x1, z1) = FCell(b.max.x, b.max.z);
                for (int x = x0; x <= x1; x++) for (int z = z0; z <= z1; z++) { if (!fIndex.TryGetValue((x, z), out var l)) fIndex[(x, z)] = l = new(); l.Add((r, b, prop)); }
            }
        }

        /// <summary>How far back the camera can stand: the wheel's distance, cut 0.4 m short of the first wall or building on the line from the agent's
        /// head to the camera (never under 1.5 m). Props on that line, or within followPropNear of the camera, are hidden while they are.</summary>
        float FollowClear(Vector3 fwd)
        {
            FollowIndex(); fHideNow.Clear();
            var head = fPivot + Vector3.up * 1.6f; var cam0 = fPivot - fwd * fDist + Vector3.up * (followHeight + fDist * followRise);
            var ray = new Ray(head, (cam0 - head).normalized); float len = Vector3.Distance(head, cam0), d = fDist;
            fSeen.Clear(); var seen = fSeen;
            for (float t = 0f; t < len + 4f; t += 4f)
            {
                var q = ray.GetPoint(Mathf.Min(t, len));
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                {
                    var (cx, cz) = FCell(q.x, q.z); if (!fIndex.TryGetValue((cx + dx, cz + dz), out var l)) continue;
                    foreach (var (r, b, prop) in l)
                    {
                        if (!seen.Add(r)) continue; var bb = b; bb.Expand(0.3f);
                        bool onLine = bb.IntersectRay(ray, out var hit) && hit <= len;
                        if (prop) { if (onLine || b.SqrDistance(cam0) < followPropNear * followPropNear) fHideNow.Add(r); }
                        else if (onLine && !b.Contains(head)) d = Mathf.Min(d, (hit - 0.4f) / Mathf.Max(0.01f, len) * fDist); // a box round the agent (a terrace's awning) is not between him and the camera
                    }
                }
            }
            foreach (var r in fHidden) if (!fHideNow.Contains(r) && r) r.enabled = true;
            foreach (var r in fHideNow) if (r && r.enabled) r.enabled = false;
            fHidden.Clear(); fHidden.UnionWith(fHideNow);
            return Mathf.Max(1.5f, d);
        }
        void FollowUnhide() { foreach (var r in fHidden) if (r) r.enabled = true; fHidden.Clear(); }

        void FollowTurn(int dir) { fYawFrom = fYaw; fYawTo += followTurnStep * dir; fTurnStart = Time.unscaledTime; }

        /// <summary>T in either view (under a modal too: the camera may look round there).</summary>
        bool FollowKey(Keyboard kb) { if (!controls.Down(kb, PlayAction.FollowView)) return false; FollowToggle(); return true; }

        /// <summary>Per frame, after the views moved: the agent followed (a new selection swings to its agent), the turn eased, the camera placed.</summary>
        void FollowFrame()
        {
            if (!Following || !cam) return;
            int who = FollowWho(); if (who != fAgent) fAgent = who;
            FollowPlace(false);
        }

        void FollowPlace(bool snap)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (FollowPoint(fAgent) is Vector3 p) fPivot = snap ? p : Vector3.SmoothDamp(fPivot, p, ref fPivotVel, followSmooth, Mathf.Infinity, dt);
            if (fTurnStart >= 0f)
            {
                float t = turnSeconds <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - fTurnStart) / turnSeconds), s = t * t * (3f - 2f * t);
                fYaw = Mathf.LerpAngle(fYawFrom, fYawTo, s); if (t >= 1f) { fYaw = fYawTo; fTurnStart = -1f; intentKey = ""; }
            }
            var fwd = Quaternion.Euler(0f, fYaw, 0f) * Vector3.forward;
            // the boom pulls in when a wall, a planter or a tree bed stands between the agent and the camera, or beside the camera (the city has no
            // colliders: the engine's walk grid, read only, is the test); in at once, back out eased
            float want = FollowClear(fwd);
            fDistNow = snap || want < fDistNow ? want : Mathf.MoveTowards(fDistNow, want, 4f * dt);
            var pos = fPivot - fwd * fDistNow + Vector3.up * (followHeight + fDistNow * followRise);
            var look = fPivot + fwd * followAhead + Vector3.up * followLookHeight;
            var rot = Quaternion.LookRotation(look - pos, Vector3.up);
            float sw = followSwing <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - fSwingStart) / followSwing);
            if (sw < 1f) { float e = sw * sw * (3f - 2f * sw); pos = Vector3.Lerp(fSwingFromPos, pos, e); rot = Quaternion.Slerp(Quaternion.LookRotation(fPivot - fSwingFromPos), rot, e); }
            cam.transform.SetPositionAndRotation(pos, rot);
        }
    }
}
