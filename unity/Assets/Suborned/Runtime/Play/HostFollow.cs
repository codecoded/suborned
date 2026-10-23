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
    /// the tactical shadow fit (TacticalShadows) fits the perspective camera on its own (brief 048: 110 m, two cascades).</summary>
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
        public float followFar = 350f; // brief 048 (Aster's call on Redline's plan): 350 m, equal to DistanceHaze.FarClip (haze.json farClip)
        [Tooltip("Q / E in the follow view: degrees a press.")]
        public float followTurnStep = 45f;
        [Tooltip("Seconds the camera takes to catch up with the agent (smooth follow), and to swing in.")]
        public float followSmooth = 0.2f, followSwing = 0.5f;
        [Tooltip("Sky 2026-10-20 (replaces brief 024's no auto-recentre): the chase camera swings behind the agent's heading as it moves; critically damped (no overshoot), this smooth time settles in about 0.5 s.")]
        public float followChaseSmooth = 0.15f;
        [Tooltip("Seconds after a drag-orbit or a Q/E turn before the camera eases back behind a moving agent.")]
        public float followLookHold = 1f;
        [Tooltip("The agent's ground speed (m/s) above which the camera chases its heading; below it the camera holds where it is.")]
        public float followChaseSpeed = 0.5f;

        /// <summary>The follow view is on (the HUD hides its world marks; the camera paths that assume an orthographic camera stand down).</summary>
        public bool Following { get; private set; }
        /// <summary>The agent the follow view is behind (-1 when off).</summary>
        public int FollowAgent => Following ? fAgent : -1;

        struct Overhead { public Vector3 pos, focus; public Quaternion rot; public bool ortho, shadows; public float size, fov, near, far; }
        Overhead left;
        float fYaw, fYawFrom, fYawTo, fTurnStart = -1f, fDist, fSwingStart, fDragX; bool fDragging; int fAgent = -1;
        Vector3 fPivot, fPivotVel; Vector3 fSwingFromPos; Quaternion fSwingFromRot;
        Vector3 fLast, fHeadVel; bool fHasLast; float fChaseVel, fLookUntil = -1f;

        /// <summary>Toggle the follow view (T, or a script).</summary>
        public void FollowToggle() { if (Following) FollowExit(); else FollowEnter(); }

        void FollowEnter()
        {
            if (!cam || Game == null || Following) return;
            if (turnStart >= 0f) { turnStart = Time.unscaledTime - turnSeconds - 1f; ApplyTurn(); } // a quarter turn under way lands first: the view left is a resting one
            turnDragging = false; grabbing = false;
            left = new Overhead { pos = cam.transform.position, rot = cam.transform.rotation, focus = focus, ortho = cam.orthographic, size = cam.orthographicSize, fov = cam.fieldOfView, near = cam.nearClipPlane, far = cam.farClipPlane, shadows = shadows && shadows.enabled };
            fAgent = FollowWho(); fPivot = FollowPoint(fAgent) ?? focus; fPivotVel = Vector3.zero;
            fYaw = fYawFrom = fYawTo = yawNow; fTurnStart = -1f; fDist = fDist > 0f ? fDist : followDistance; fDistNow = fDist; fDragging = false; fHasLast = false; fHeadVel = Vector3.zero; fChaseVel = 0f; fLookUntil = -1f; // Sky: the zoom is the player's setting, kept between visits // behind the agent, looking the way the overhead view looked
            fSwingFromPos = cam.transform.position; fSwingFromRot = cam.transform.rotation; fSwingStart = Time.unscaledTime;
            cam.orthographic = false; cam.fieldOfView = followFov; cam.nearClipPlane = 0.2f; cam.farClipPlane = Mathf.Min(followFar, DistanceHaze.FarClip > 0f ? DistanceHaze.FarClip : followFar); // brief 048: the clip is the haze's far (350 m)
            if (shadows) shadows.enabled = true; // brief 048: TacticalShadows fits a perspective camera itself (110 m, 2 cascades, split 22 m, last 30 % faded)
            Following = true; intentKey = "";
            fSwingFromPos = fPivot + (fSwingFromPos - focus).normalized * 60f; // the swing starts high on the overhead view's line through the agent
            FollowPlace(true);
            Status = $"Follow view: {Suborned.Sim.Op.Name((Suborned.Sim.OpId)fAgent)} ({controls.Label(PlayAction.FollowView)} for the overhead view)";
        }

        void FollowExit()
        {
            if (!Following) return;
            Following = false; fAgent = -1; fDragging = false; FollowUnhide(); dResume = false;
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
            if (kb != null && !fDragging && !Driving) // driven: E acts and the drag orbits (HostDrive)
            {
                if (controls.Down(kb, PlayAction.TurnLeft)) FollowTurn(-1);
                if (controls.Down(kb, PlayAction.TurnRight)) FollowTurn(1);
            }
            if (mouse == null) return;
            float x = mouse.position.ReadValue().x;
            if (!fDragging && controls.Held(kb, PlayAction.TurnDrag) && mouse.middleButton.wasPressedThisFrame) { fDragging = true; fDragX = x; fTurnStart = -1f; }
            if (fDragging)
            {
                if (!mouse.middleButton.isPressed) { fDragging = false; fLookUntil = Time.unscaledTime + followLookHold; } // a look-around: eases back behind the agent after followLookHold
                else { fYaw += (x - fDragX) * turnDragDegPerPixel * OrbitScale * 1080f / Mathf.Max(1f, Screen.height); fYawFrom = fYawTo = fYaw; fDragX = x; }
            }
            float wheel = mouse.scroll.ReadValue().y;
            if (wheel != 0f && !overHud) fDist = FollowWheel(fDist, wheel); // stepped or proportional (brief 025)
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
            if (!Cam.follow.collision) { foreach (var r in fHidden) if (r) r.enabled = true; fHidden.Clear(); return fDist; } // brief 025: collision off in the lab
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
        void FollowUnhide() { foreach (var r in fHidden) if (r) r.enabled = true; fHidden.Clear(); FollowPeopleUnhide(); }

        [Tooltip("Follow view: a passer-by nearer the camera whose drawn box covers at least this share of the followed agent's (feet to head, on screen) is hidden while they do.")]
        public float followPersonCover = 0.12f;
        readonly List<Renderer> fPeopleHidden = new();
        void FollowPeopleUnhide() { foreach (var r in fPeopleHidden) if (r) r.forceRenderingOff = false; fPeopleHidden.Clear(); }

        /// <summary>Glyph 022 (player-follow-cafe: the selected ring sat under a blond passer-by, who stood between the camera and the Fixer): a
        /// passer-by (crowd/*) nearer the camera than the followed agent whose screen box covers part of the agent's is hidden while they do, so
        /// the agent, and the ring at her feet, are never drawn behind (or under) someone else. The cast, the cell and every signal figure stay.</summary>
        void FollowPeople()
        {
            FollowPeopleUnhide();
            if (views == null || !cam || FollowPoint(fAgent) is not Vector3 a) return;
            static Rect Box(UnityEngine.Camera c, Vector3 feet, out float depth)
            {
                var f = c.WorldToScreenPoint(feet); var h = c.WorldToScreenPoint(feet + Vector3.up * 1.8f); depth = f.z;
                float half = Mathf.Abs(c.WorldToScreenPoint(feet + c.transform.right * 0.32f).x - f.x);
                return Rect.MinMaxRect(Mathf.Min(f.x, h.x) - half, Mathf.Min(f.y, h.y), Mathf.Max(f.x, h.x) + half, Mathf.Max(f.y, h.y));
            }
            var ab = Box(cam, a, out var ad); if (ad <= 0f || ab.width * ab.height < 1f) return;
            foreach (var (key, t) in views.All())
            {
                if (!key.StartsWith("crowd/") || !t || !t.gameObject.activeInHierarchy) continue;
                var pb = Box(cam, t.position, out var pd); if (pd <= 0f || pd >= ad - 0.3f) continue; // behind the camera, or not in front of the agent
                float ox = Mathf.Min(ab.xMax, pb.xMax) - Mathf.Max(ab.xMin, pb.xMin), oy = Mathf.Min(ab.yMax, pb.yMax) - Mathf.Max(ab.yMin, pb.yMin);
                if (ox <= 0f || oy <= 0f || ox * oy < followPersonCover * ab.width * ab.height) continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>()) if (!r.forceRenderingOff) { r.forceRenderingOff = true; fPeopleHidden.Add(r); }
            }
        }

        // ---------------------------------------------------------------- brief 041: the leash camera

        /// <summary>The leash's numbers (camera.js LEASH, briefs/040-third-person-research/report.md): gain on the sideways drag, the arm (m) it is
        /// read over, a heading held steadyS within steadyDeg, the look idle idleS, the settle's SmoothDamp time and its cap (°/s), the speed (m/s)
        /// under which the agent counts as standing.</summary>
        public float leashGain = 1f, leashArm = 5.5f, leashSteadyS = 1f, leashSteadyDeg = 20f, leashIdleS = 2f, leashSettleS = 1f, leashMaxDegS = 45f, leashMinSpeed = 0.5f;
        float lIdle = 99f, lSteady, lLastH, lSide = 1f, lVel;
        /// <summary>The proof's per-frame log: unscaled time, camera yaw, agent heading, speed (m/s), settling.</summary>
        public readonly List<(float t, float yaw, float heading, float speed, bool settling)> LeashLog = new();
        public bool LeashLogOn;
        /// <summary>A look-around with the mouse (drag-orbit, Alt + middle-drag) or a Q/E turn: the leash waits leashIdleS before it settles again.</summary>
        void LeashLook() { lIdle = 0f; lVel = 0f; dMoveFrame = null; }

        void LeashFrame(bool snap, float dt)
        {
            if (snap) { lIdle = 99f; lSteady = 0f; lVel = 0f; return; }
            if (fDragging || fTurnStart >= 0f || orbiting) LeashLook();
            lIdle += dt;
            var v = new Vector2(fHeadVel.x, fHeadVel.z); float speed = v.magnitude, h = Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;
            float yr = fYaw * Mathf.Deg2Rad; var right = new Vector2(Mathf.Cos(yr), -Mathf.Sin(yr)); float side = Vector2.Dot(v, right); // Unity's right of the camera's heading
            if (speed > leashMinSpeed)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(lLastH, h)) > leashSteadyDeg) { lSteady = 0f; lLastH = h; } else lSteady += dt;
                if (Mathf.Abs(side) > 0.2f) lSide = Mathf.Sign(side);
            }
            else lSteady = 0f;
            if (lIdle > 0.3f && speed > leashMinSpeed) fYaw += side / leashArm * leashGain * dt * Mathf.Rad2Deg; // sideways motion only: walking at the camera never spins it
            bool settling = controls.leashSettle && speed > leashMinSpeed && lSteady >= leashSteadyS && lIdle >= leashIdleS;
            if (settling)
            {
                float d = Mathf.DeltaAngle(fYaw, h); if (Mathf.Abs(d) > 150f) d = lSide * Mathf.Abs(d); // straight at the camera: round the side it last drifted to
                fYaw = Mathf.SmoothDamp(fYaw, fYaw + d, ref lVel, leashSettleS, leashMaxDegS, dt);
            }
            else lVel *= Mathf.Exp(-dt / 0.2f);
            fYawFrom = fYawTo = fYaw;
            if (LeashLogOn) { LeashLog.Add((Time.unscaledTime, fYaw, h, speed, settling)); if (LeashLog.Count > 20000) LeashLog.RemoveRange(0, 5000); }
        }

        /// <summary>The yaw the keys walk relative to (camera.js moveYaw): latched while a move key is held, so a held key keeps a straight path
        /// as the leash turns (the S-key circling); the camera's own otherwise.</summary>
        float? dMoveFrame;
        float MoveYaw(bool held)
        {
            if (!Following || !controls.Modern) { dMoveFrame = null; return Following ? fYaw : yawNow; }
            if (!held) { dMoveFrame = null; return fYaw; }
            dMoveFrame ??= fYaw; return dMoveFrame.Value;
        }

        void FollowTurn(int dir) { fYawFrom = fYaw; fYawTo += followTurnStep * dir; fTurnStart = Time.unscaledTime; fLookUntil = Time.unscaledTime + turnSeconds + followLookHold; }

        /// <summary>T in either view (under a modal too: the camera may look round there).</summary>
        /// <summary>T (brief 025): take control of the selected agent, or let go; under a modal, or in a plain follow view (the lab's button), the
        /// follow view only.</summary>
        bool FollowKey(Keyboard kb)
        {
            if (!controls.Down(kb, PlayAction.FollowView)) return false;
            if (controls.HoldActs && Driving && controls.Held(kb, PlayAction.AddToGroup)) { DriveNextAgent(); return true; } // brief 041: Shift+T takes the next agent; T lets go (Esc stays the pause menu)
            if (Driving) DriveLetGoToMap(); else if (Following) FollowExit(); else if (Modal == ModalGate.Modal.None) DriveToggle(); else FollowEnter();
            return true;
        }

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
            // Sky 2026-10-20: the chase camera. The agent's drawn heading (its smoothed ground velocity); moving faster than followChaseSpeed, the
            // yaw eases behind it (SmoothDampAngle: critically damped, no overshoot); standing still, the camera holds; a drag or a Q/E turn is a
            // look-around that eases back followLookHold after it ends. Distance and pitch stay as set: only a wall on the line pulls the boom in.
            if (FollowPoint(fAgent) is Vector3 raw)
            {
                if (fHasLast && dt > 1e-4f) { var v = (raw - fLast) / dt; v.y = 0f; fHeadVel = Vector3.Lerp(fHeadVel, v, 1f - Mathf.Exp(-dt / 0.12f)); }
                fLast = raw; fHasLast = true;
            }
            if (controls.Modern) LeashFrame(snap, dt); // brief 041: the leash (testbed/controls/camera.js, briefs/040 report): sideways motion turns it; it settles behind a steady heading
            else if (!snap && !fDragging && fTurnStart < 0f && Time.unscaledTime >= fLookUntil && fHeadVel.magnitude > followChaseSpeed)
            {
                float target = Mathf.Atan2(fHeadVel.x, fHeadVel.z) * Mathf.Rad2Deg;
                fYaw = Mathf.SmoothDampAngle(fYaw, target, ref fChaseVel, followChaseSmooth, Mathf.Infinity, dt); fYawFrom = fYawTo = fYaw;
            }
            else fChaseVel = 0f;
            var fwd = Quaternion.Euler(0f, fYaw, 0f) * Vector3.forward;
            // the boom pulls in when a wall, a planter or a tree bed stands between the agent and the camera, or beside the camera (the city has no
            // colliders: the engine's walk grid, read only, is the test); in at once, back out eased
            float want = FollowClear(fwd);
            fDistNow = snap || want < fDistNow ? want : Mathf.MoveTowards(fDistNow, want, 4f * dt);
            var right = Quaternion.Euler(0f, fYaw, 0f) * Vector3.right * Cam.follow.shoulder; // brief 025: over the shoulder
            var pos = fPivot - fwd * fDistNow + Vector3.up * (followHeight + fDistNow * followRise) + right;
            var look = fPivot + fwd * followAhead + Vector3.up * followLookHeight + right;
            var rot = Quaternion.LookRotation(look - pos, Vector3.up);
            if (Cam.follow.pitch >= 0f) // brief 025: a set pitch: the boom tilts round the agent's head, the camera looks down at that angle
            {
                rot = Quaternion.Euler(Cam.follow.pitch, fYaw, 0f);
                pos = fPivot + Vector3.up * (followLookHeight + followHeight - 1.3f) - rot * Vector3.forward * fDistNow + right;
            }
            float sw = followSwing <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - fSwingStart) / followSwing);
            if (sw < 1f) { float e = sw * sw * (3f - 2f * sw); pos = Vector3.Lerp(fSwingFromPos, pos, e); rot = Quaternion.Slerp(Quaternion.LookRotation(fPivot - fSwingFromPos), rot, e); }
            cam.transform.SetPositionAndRotation(pos, rot);
            FollowPeople();
        }
    }
}
