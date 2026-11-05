using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Suborned.Play
{
    /// <summary>Brief 096, Redline's render audit item 2 option B (Sky 2026-11-05: "looks worth it"; Glint's prototype briefs/096/glint/shadowB): the
    /// orthographic play camera stands back along its view only as far as the tallest geometry in its frame column needs (StandOffField), not a fixed
    /// 160 m. An orthographic frame is the same picture at any stand-off, so the framing (zoom, pitch, yaw, focus) never changes; what changes is the
    /// shadow map: TacticalShadows fits one cascade from the camera to the street, so a short stand-off gives sharp shadows (about 70 m and 2.3 cm a
    /// texel at the Records Office against 160 m today) and a tall tower in frame (SpaceHQ, about 250 m) moves the camera back so its roof never
    /// crosses the near plane.
    /// Each frame, after the host has placed the camera: the need now (the frame's own column) and ahead (the column grown by `lookAhead` m, so a
    /// tower is counted before a pan, turn or zoom brings it in). The stand-off eases toward the need ahead (up in `riseSeconds`, down in
    /// `fallSeconds`) and is never below the need now (no clipping, ever); a jump of the focus (a glide's end, LookAt across the map) snaps. The camera
    /// moves along its own axis about its ground point; the host's standOff (the fail camera, the convo's high shot, CamDistance) and the shadows'
    /// focusDepth follow. Perspective views (follow, drive, the camera lab's lens) are left alone: they have their own fit.
    /// Review switch: GradeReview's spec `standOff` sets Fixed (a fixed stand-off for one launch, as the prototype did).</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class PlayStandOff : MonoBehaviour
    {
        /// <summary>A fixed stand-off (m) for this launch (GradeReview's review switch); NaN = from the frame.</summary>
        public static float Fixed = float.NaN;
        [Tooltip("Clearance (m) between the nearest roof in the column and the near plane.")]
        public float margin = 4f;
        [Tooltip("Metres round the frame counted ahead of time, so the stand-off has risen before a pan, turn or zoom brings a tower in.")]
        public float lookAhead = 30f;
        [Tooltip("Seconds the stand-off takes to rise (about 63 %) toward the need ahead; and to fall when the tall roofs have left.")]
        public float riseSeconds = 0.25f, fallSeconds = 1.5f;
        [Tooltip("Plan grid cell (m) of the height field.")]
        public float cell = 8f;
        [Tooltip("The least stand-off (m).")]
        public float minStand = 20f;
        [Tooltip("A focus move longer than this in one frame (m) snaps the stand-off (a jump, not a pan).")]
        public float snapJump = 60f;

        HandoverHost host; StandOffField field; float now = float.NaN, builtAt; int builtCount; Vector3 lastFocus;
        /// <summary>The stand-off now (m), the need of the frame's own column, and of the column with the look-ahead.</summary>
        public float Now => now;
        public float NeedNow { get; private set; }
        public float NeedAhead { get; private set; }
        public StandOffField Field => field;

        void LateUpdate()
        {
            if (!host) host = GetComponent<HandoverHost>();
            var cam = host ? host.View : null;
            if (!cam || !cam.orthographic || host.Following) { now = float.NaN; return; } // brief 096: the overhead view resumes at its frame's need (a snap, below), never a stale stand-off eased from
            var t = cam.transform; var fwd = t.forward;
            if (fwd.y > -0.05f) return;
            if (field == null || Time.unscaledTime - builtAt > 3f && builtCount >= 0) Build(cam);
            var focus = t.position + fwd * (-t.position.y / fwd.y); // the ground point the host placed the camera over
            var e = t.eulerAngles; float halfH = cam.orthographicSize, halfW = halfH * cam.aspect, clear = margin + cam.nearClipPlane;
            NeedNow = field.Need(focus, e.y, e.x, halfW, halfH, 0f) + clear;
            NeedAhead = field.Need(focus, e.y, e.x, halfW, halfH, lookAhead) + clear;
            float target = Mathf.Max(NeedAhead, minStand);
            if (!float.IsNaN(Fixed)) now = Fixed;
            else if (float.IsNaN(now) || (focus - lastFocus).sqrMagnitude > snapJump * snapJump) now = target;
            else
            {
                float tau = target > now ? riseSeconds : fallSeconds;
                now += (target - now) * (1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.01f, tau)));
                now = Mathf.Max(now, NeedNow);
            }
            lastFocus = focus;
            host.standOff = now;
            t.position = focus - fwd * now;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, now + 1500f);
            if (cam.TryGetComponent<TacticalShadows>(out var ts)) { ts.focusDepth = now; ts.uncapped = HandoverHost.FreeZoom; } // brief 096: free zoom (debug) lets the shadow band reach the frame's edge
        }

        /// <summary>Brief 096: the stand-off (m) an orthographic map frame needs at a ground focus (the glide from third person lands there): the
        /// frame's own column, the clearance and the least stand-off. 0 before the field is built.</summary>
        public float NeedAt(Camera cam, Vector3 focus, Quaternion rot, float halfH)
        {
            if (field == null || !cam) return 0f;
            var e = rot.eulerAngles; if (e.x < 3f || e.x > 89.9f) return 0f;
            return Mathf.Max(field.Need(focus, e.y, e.x, halfH * cam.aspect, halfH, 0f) + margin + cam.nearClipPlane, minStand);
        }

        /// <summary>The height field from every mesh the play camera draws: bounds for small ones; triangles for large ones (a merged tile, a band)
        /// when readable, else bounds (conservative). Built on the first frame and once more 3 s later if the scene's meshes changed.</summary>
        void Build(Camera cam)
        {
            var sw = Stopwatch.StartNew();
            var all = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            if (field != null && all.Length == builtCount) { builtCount = -1; return; }
            var use = new List<MeshRenderer>(all.Length);
            float minX = float.PositiveInfinity, minZ = float.PositiveInfinity, maxX = float.NegativeInfinity, maxZ = float.NegativeInfinity;
            foreach (var r in all)
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy || r.forceRenderingOff || (cam.cullingMask & (1 << r.gameObject.layer)) == 0) continue;
                if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;
                var b = r.bounds; if (b.max.y <= 0.5f) continue;
                use.Add(r);
                if (b.size.x > 1500f || b.size.z > 1500f) continue; // a sky or horizon band: its own triangles mark where it stands
                minX = Mathf.Min(minX, b.min.x); minZ = Mathf.Min(minZ, b.min.z); maxX = Mathf.Max(maxX, b.max.x); maxZ = Mathf.Max(maxZ, b.max.z);
            }
            if (float.IsInfinity(minX)) { minX = minZ = -500f; maxX = maxZ = 500f; }
            var f = new StandOffField(cell, minX - 400f, minZ - 400f, maxX + 400f, maxZ + 400f);
            int tri = 0, boxes = 0, unreadable = 0; var verts = new List<Vector3>(); var idx = new List<int>();
            foreach (var r in use)
            {
                var b = r.bounds; bool large = b.size.x > 3f * cell || b.size.z > 3f * cell;
                var mesh = large && r.TryGetComponent<MeshFilter>(out var mf) ? mf.sharedMesh : null;
                if (mesh && mesh.isReadable)
                {
                    var m = r.transform.localToWorldMatrix; mesh.GetVertices(verts);
                    for (int i = 0; i < verts.Count; i++) verts[i] = m.MultiplyPoint3x4(verts[i]);
                    for (int s = 0; s < mesh.subMeshCount; s++) { mesh.GetTriangles(idx, s); for (int i = 0; i + 2 < idx.Count; i += 3) f.AddTriangle(verts[idx[i]], verts[idx[i + 1]], verts[idx[i + 2]]); tri += idx.Count / 3; }
                }
                else
                {
                    if (large && mesh) unreadable++;
                    if (b.size.x > 1500f || b.size.z > 1500f) continue; // an unreadable band: its box would cover the map
                    f.AddBox(b); boxes++;
                }
            }
            bool again = field != null; field = f; builtAt = Time.unscaledTime; builtCount = again ? -1 : all.Length;
            var hi = f.Highest();
            Debug.Log($"PlayStandOff: height field {(again ? "rebuilt" : "built")} in {sw.ElapsedMilliseconds} ms from {use.Count} meshes ({boxes} boxes, {tri} triangles, {unreadable} large unreadable as boxes), " +
                $"cell {cell} m, highest {hi.y:0.0} m at ({hi.x:0}, {hi.z:0})");
            if (!traceStarted && GameBoot.ArgValue("-standTrace") is string dir) { traceStarted = true; StartCoroutine(Trace(dir)); }
        }

        bool traceStarted;
        /// <summary>Scratch check (`-standTrace dir`): a pan toward the highest roof, a quarter turn, a zoom out and back, logging the stand-off and the
        /// needs each frame (trace.csv) and a frame at each stop. The log's `gap` is the stand-off less the need now: never below 0 (no clipping).</summary>
        IEnumerator Trace(string dir)
        {
            Directory.CreateDirectory(dir);
            var csv = new StringBuilder("frame,phase,t,standOff,needNow,needAhead,gap,step,zoom,yaw,focusX,focusZ\n");
            var cam = host.View; float last = now; string phase = "settle"; int shot = 0;
            void Row() { var p = cam.transform.position + cam.transform.forward * now; csv.Append($"{Time.frameCount},{phase},{Time.unscaledTime:0.000},{now:0.00},{NeedNow:0.00},{NeedAhead:0.00},{now - NeedNow:0.00},{now - last:0.000},{cam.orthographicSize:0.0},{cam.transform.eulerAngles.y:0.0},{p.x:0.0},{p.z:0.0}\n"); last = now; }
            IEnumerator Frames(int n) { for (int i = 0; i < n; i++) { yield return null; Row(); } }
            void Shot(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"{++shot:00}-{name}.png"));
            yield return new WaitForSecondsRealtime(4f);
            var hi = field.Highest(); var tower = new Vector3(hi.x, 0f, hi.z);
            var start = host.Focus; yield return Frames(30); Shot("start");
            phase = "pan"; var from = tower - (tower - start).normalized * 160f; host.LookAt(from); yield return Frames(60);
            for (int i = 0; i <= 150; i++) { host.LookAt(Vector3.Lerp(from, tower, i / 150f)); yield return null; Row(); if (i == 75) Shot("pan-mid"); }
            yield return Frames(30); Shot("tower-play");
            phase = "turn"; host.TurnBy(1); yield return Frames(45); Shot("tower-turned");
            phase = "zoomout"; float z0 = cam.orthographicSize; for (int i = 0; i <= 30; i++) { cam.orthographicSize = Mathf.Lerp(z0, 18.5f, i / 30f); yield return null; Row(); }
            yield return Frames(30); Shot("tower-plan");
            phase = "back"; host.LookAt(start); yield return Frames(120); Shot("back");
            File.WriteAllText(Path.Combine(dir, "trace.csv"), csv.ToString());
            Debug.Log($"PlayStandOff: trace written to {dir}");
        }
    }
}
