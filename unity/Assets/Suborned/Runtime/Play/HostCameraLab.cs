using System;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 025 item 1 (Sky 2026-10-19: "I think the camera angle needs work - would love a debug on this to try various options"): the
    /// camera's playground values, one preset at a time (CamPreset), set live from the camera lab (CameraLab, P). View only: the engine, the
    /// orders and the replays never see the camera. Nothing here is a design decision; the built-in preset is today's view (zoom 14 at 38°, yaw
    /// 30 in quarter steps, orthographic; the follow view as it was).</summary>
    [Serializable]
    public sealed class CamPreset
    {
        public string name = "Built-in";
        [Serializable] public sealed class Overhead
        {
            public float zoom = 14f, zoomMin = 12.5f, zoomMax = 18.5f; // the visible half-height (m) at the focus: orthographic size, or the perspective distance that shows as much
            public float pitch = 38f;
            public bool freeYaw = false;     // Alt + middle-drag rests where it is left (off: snaps to the nearest quarter)
            public bool perspective = false;
            public float fov = 30f;
        }
        [Serializable] public sealed class Follow
        {
            public float distance = 5.5f, distanceMin = 3f, distanceMax = 10f;
            public float height = 1.3f, rise = 0.32f;  // the camera stands height + rise x distance above the feet
            public float pitch = -1f;                  // degrees down; below 0 = look down the street (lookAhead, lookHeight)
            public float lookAhead = 8f, lookHeight = 1.35f;
            public float shoulder = 0f;                // metres right (+) or left (-) of the agent
            public float fov = 55f;
            public bool orbitDrag = true;              // left-drag on the street orbits (yaw, and pitch when pitch is set); a click still moves
            public float orbitDegPerPixel = 0.25f;
            public bool collision = true;              // pull in before walls, hide props on the line
            public float smoothing = 0.2f;             // seconds to catch up with the agent
        }
        [Serializable] public sealed class Wheel
        {
            public bool proportional = false;          // trackpad-friendly: the zoom moves with the scroll's size, not its sign
            public float rate = 0.08f;                 // proportional: the zoom scales by exp(-scroll x rate)
            public float step = 0.5f;                  // stepped: metres (overhead) a notch; the follow view steps 1.5 x this
        }
        public Overhead overhead = new();
        public Follow follow = new();
        public Wheel wheel = new();

        public CamPreset Clone() => JsonUtility.FromJson<CamPreset>(JsonUtility.ToJson(this));
    }

    public sealed partial class HandoverHost
    {
        /// <summary>The camera values now (CameraLab edits this object and calls CamApply).</summary>
        public CamPreset Cam { get; private set; } = new();
        /// <summary>The last raw scroll value read (the lab shows it, to tune the wheel rate on a trackpad).</summary>
        public float LastScroll { get; private set; }

        /// <summary>Take a preset (a copy) and apply it now: zoom, pitch, projection; the follow view's values on its next frame.</summary>
        public void CamUse(CamPreset p, bool zoomToo = true) { Cam = (p ?? new CamPreset()).Clone(); CamApply(zoomToo); }

        /// <summary>Apply the current values: the overhead zoom (clamped), pitch and projection, and the follow fields.</summary>
        public void CamApply(bool zoomToo = false)
        {
            var o = Cam.overhead; var f = Cam.follow;
            zoomMin = Mathf.Max(0.5f, Mathf.Min(o.zoomMin, o.zoomMax)); zoomMax = Mathf.Max(zoomMin, o.zoomMax);
            followFov = f.fov; followDistance = Mathf.Clamp(f.distance * FollowDistanceFactor, f.distanceMin, f.distanceMax); // brief 051: Options, Camera, Camera distance (Close 0.75, Default 1, Far 1.4 of the preset's) followMin = Mathf.Min(f.distanceMin, f.distanceMax); followMax = Mathf.Max(f.distanceMin, f.distanceMax);
            followHeight = f.height; followRise = f.rise; followAhead = f.lookAhead; followLookHeight = f.lookHeight; followSmooth = Mathf.Max(0f, f.smoothing);
            if (!cam) return;
            if (zoomToo) cam.orthographicSize = Mathf.Clamp(o.zoom, zoomMin, zoomMax); else cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, zoomMin, zoomMax);
            if (Following) { cam.fieldOfView = f.fov; fDist = Mathf.Clamp(fDist, followMin, followMax); return; }
            if (FailCamOn) return;
            TurnInit(); pitch = Mathf.Clamp(o.pitch, 5f, 89.5f);
            if (!o.freeYaw && !Turning) { float rel = Mathf.Repeat(yawNow - baseYaw, 360f); int k = Mathf.RoundToInt(rel / 90f) % 4; yawNow = yawFrom = yawTo = Mathf.Repeat(baseYaw + 90f * k, 360f); } // quarter steps again: rest on the nearest
            cam.transform.rotation = Quaternion.Euler(pitch, yawNow, 0f);
            CamProjection(); PlaceCamera(); intentKey = "";
        }

        /// <summary>The overhead projection: orthographic, or perspective at the distance that shows the same half-height at the focus.</summary>
        void CamProjection()
        {
            if (!cam || Following) return;
            var o = Cam.overhead;
            if (cam.orthographic == !o.perspective && (!o.perspective || Mathf.Approximately(cam.fieldOfView, o.fov))) return;
            cam.orthographic = !o.perspective;
            if (o.perspective) { cam.fieldOfView = Mathf.Clamp(o.fov, 5f, 100f); cam.nearClipPlane = 0.3f; }
            else cam.nearClipPlane = 0.3f;
            if (shadows) shadows.enabled = !o.perspective; // the tactical shadow fit is built for the orthographic stand-off
        }

        /// <summary>Where the overhead camera stands from the focus: the stand-off (orthographic), or the distance that shows zoom as the half-height.</summary>
        float CamDistance => cam && !cam.orthographic ? cam.orthographicSize / Mathf.Tan(Mathf.Max(1f, cam.fieldOfView) * 0.5f * Mathf.Deg2Rad) : standOff;

        /// <summary>The overhead wheel: a notch is a step (the sign only, as before), or the zoom scales with the scroll's size (proportional).</summary>
        void OverheadWheel(float wheel)
        {
            LastScroll = wheel; if (!cam) return; var w = Cam.wheel;
            float size = w.proportional ? cam.orthographicSize * Mathf.Exp(-wheel * w.rate) : cam.orthographicSize - Mathf.Sign(wheel) * w.step;
            cam.orthographicSize = Mathf.Clamp(size, zoomMin, zoomMax);
            if (!cam.orthographic) PlaceCamera();
        }

        /// <summary>The follow view's wheel: its distance, stepped or proportional.</summary>
        float FollowWheel(float dist, float wheel)
        {
            LastScroll = wheel; var w = Cam.wheel;
            return Mathf.Clamp(w.proportional ? dist * Mathf.Exp(-wheel * w.rate) : dist - Mathf.Sign(wheel) * w.step * 1.5f, followMin, followMax);
        }

        /// <summary>The follow view's orbit by a left-drag (brief 025): yaw by the horizontal drag; with a set pitch, the vertical drag tilts it.</summary>
        void FollowOrbit(Vector2 delta)
        {
            var f = Cam.follow; float k = f.orbitDegPerPixel * 1080f / Mathf.Max(1f, Screen.height);
            fYaw += delta.x * k; fYawFrom = fYawTo = fYaw; fTurnStart = -1f;
            if (f.pitch >= 0f) f.pitch = Mathf.Clamp(f.pitch - delta.y * k, 0f, 89f);
        }
    }
}
