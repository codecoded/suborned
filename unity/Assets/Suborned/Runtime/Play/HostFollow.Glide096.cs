using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 096 (Sky on 0.2.251: "Coming out of third person it zooms far out, and then snaps to game plan"). The glide between the map
    /// view and third person, both ways, as a matched-frame dolly. It was older than 0.2.251: since brief 067 G3 the glide back ended on a
    /// perspective camera 60 m up the map's line at the follow FOV (55°), a frame about three times the map's (half-height 31 m against play
    /// zoom 10), and then cut to the orthographic map; the swing in started from the same wide frame.
    /// Now one end is the follow camera (its own position and FOV), the other the map frame (its ground focus, rotation, ortho half-height, at the
    /// stand-off the map camera needs there). Along the glide the point the camera looks at, the frame's half-height there (log) and the lens's
    /// tan(FOV/2) (log) ease from one end to the other; the camera stands at half-height / tan back from that point, so the frame never jumps.
    /// Over the map half of the glide the projection blends to orthographic (the two matrices interpolated; matched at the focus depth), so the
    /// last frame is the map picture itself and the switch to the orthographic camera changes nothing on screen.</summary>
    public sealed partial class HandoverHost
    {
        struct GlideEnd { public Vector3 target; public Quaternion rot; public float halfH, tanHalf; public bool ortho; }
        /// <summary>The map end's lens, at most tan 10° (a near-orthographic lens), and the farthest it stands back for that (m), inside the follow haze's clear 90 m.</summary>
        const float GlideMapTan = 0.1763f, GlideMapReach = 60f;
        GlideEnd fExitFrom, fEnterMap; float fEnterSwingStart = -1f; bool glideMatrix;

        /// <summary>A perspective camera as a glide end: the point on its axis at the depth of the followed agent, and the frame there.</summary>
        GlideEnd FollowSide(Vector3 pos, Quaternion rot, float fov)
        {
            var fwd = rot * Vector3.forward; float d = Mathf.Max(1f, Vector3.Dot(fPivot - pos, fwd)), t = Mathf.Tan(Mathf.Clamp(fov, 1f, 170f) * 0.5f * Mathf.Deg2Rad);
            return new GlideEnd { target = pos + fwd * d, rot = rot, halfH = d * t, tanHalf = t };
        }

        /// <summary>The map frame as a glide end: its ground focus, rotation and half-height, the lens from where its camera stands (the stand-off
        /// the frame needs, PlayStandOff; never so close a roof crosses the near plane).</summary>
        GlideEnd MapSide(Vector3 ground, Quaternion rot, float halfH, bool ortho, float fov)
        {
            if (!ortho) { float t = Mathf.Tan(Mathf.Clamp(fov, 1f, 170f) * 0.5f * Mathf.Deg2Rad); return new GlideEnd { target = ground, rot = rot, halfH = halfH, tanHalf = t }; }
            float need = TryGetComponent<PlayStandOff>(out var so) ? so.NeedAt(cam, ground, rot, halfH) : 0f;
            float d = Mathf.Max(need, Mathf.Min(halfH / GlideMapTan, GlideMapReach), 1f);
            return new GlideEnd { target = ground, rot = rot, halfH = halfH, tanHalf = halfH / d, ortho = true };
        }

        /// <summary>The camera at s along the glide (0 = end a, 1 = end b).</summary>
        void GlidePlace(GlideEnd a, GlideEnd b, float s)
        {
            var rot = Quaternion.Slerp(a.rot, b.rot, s);
            float h = Mathf.Exp(Mathf.Lerp(Mathf.Log(a.halfH), Mathf.Log(b.halfH), s)), t = Mathf.Exp(Mathf.Lerp(Mathf.Log(a.tanHalf), Mathf.Log(b.tanHalf), s));
            var target = Vector3.Lerp(a.target, b.target, s);
            cam.transform.SetPositionAndRotation(target - rot * Vector3.forward * (h / t), rot);
            cam.fieldOfView = 2f * Mathf.Atan(t) * Mathf.Rad2Deg;
            float k = (a.ortho ? 1f - s : 0f) + (b.ortho ? s : 0f); k = Mathf.Clamp01((k - 0.5f) / 0.5f); k = k * k * (3f - 2f * k); // the orthographic share: the map half only
            if (k <= 0f) { GlideMatrixOff(); return; }
            float n = cam.nearClipPlane, f = cam.farClipPlane, w = h * cam.aspect;
            var p = Matrix4x4.Perspective(cam.fieldOfView, cam.aspect, n, f); var o = Matrix4x4.Ortho(-w, w, -h, h, n, f);
            var m = new Matrix4x4(); for (int i = 0; i < 16; i++) m[i] = Mathf.Lerp(p[i], o[i], k);
            cam.projectionMatrix = m; glideMatrix = true;
        }

        void GlideMatrixOff() { if (glideMatrix && cam) { cam.ResetProjectionMatrix(); glideMatrix = false; } }
    }
}
