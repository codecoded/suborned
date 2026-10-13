using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>Cameras (one per views.json entry, disabled, rendered on demand by Shots) plus sun, ambient and fog from
    /// look.json. Projections:
    /// top: orthographic, straight down, north up, fitting the frame.
    /// export: the export's view, "zoom 14 at 38°" = orthographic size view.zoom, pitch view.pitch, yaw view.yaw (compass,
    ///   90 = looking east), centred on view.focus at the street datum (0); stands back just far enough to clear the
    ///   geometry in its frame column (not the whole district's tallest object), and carries TacticalShadows fitted to that
    ///   depth from light.json `shadows.tactical` (brief 007 round 3: the play camera had no shadows at 423 m).
    /// perspective: yaw/pitch/fov fitted so every corner of the frame's bounds is in view, or standing at `eye` looking at
    ///   `look`, or standing on `along` (a road or foot line) at eyeHeight looking down the line.</summary>
    public static class Rig
    {
        public static void Build(Transform cameras, Transform lighting, Transform root, WorldDto world, List<ViewSpec> views, Look look)
        {
            var all = Inspect.Bounds(root);
            foreach (var v in views)
            {
                var cam = Make(cameras, v.Name, look.Colour(look.Entry(v.Clear)));
                cam.nearClipPlane = v.Near;
                cam.enabled = v.Main;
                if (v.Main) cam.gameObject.tag = "MainCamera";
                try
                {
                    switch (v.Projection)
                    {
                        case "top": Top(cam, Frame(root, v, all), v); break;
                        case "export": Export(cam, all, world.View, Focus(root, world, v), float.IsNaN(v.Zoom) ? world.View.Zoom : v.Zoom, v.Aspect, look); break;
                        default:
                            if (v.Eye is { } eye) Stand(cam, eye, v.Look.Value, v);
                            else if (v.Along != null) Along(cam, world, v);
                            else Fit(cam, Frame(root, v, all), v);
                            break;
                    }
                }
                catch (AnchorMissing e)
                {
                    // A view anchored on elements this export lacks (e.g. the sample) is skipped, not guessed.
                    Debug.LogWarning(e.Message);
                    Object.DestroyImmediate(cam.gameObject);
                }
            }
            Sun(lighting, look);
            var fog = look.Entry("fog");
            RenderSettings.fog = true;
            RenderSettings.fogColor = look.Colour(fog);
            RenderSettings.fogMode = J.S(fog, "mode") switch
            {
                "linear" => FogMode.Linear,
                "exponential" => FogMode.Exponential,
                "exponential2" => FogMode.ExponentialSquared,
                var m => throw new InvalidDataException($"World: fog mode `{m}`"),
            };
            RenderSettings.fogDensity = J.F(fog, "density");
            if (RenderSettings.fogMode == FogMode.Linear) { RenderSettings.fogStartDistance = J.F(fog, "start"); RenderSettings.fogEndDistance = J.F(fog, "end"); }
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            if (look.Light("ambient") == null) RenderSettings.ambientLight = look.Colour(look.Entry("ambient"));
            Atmosphere.Apply(lighting, look);
            foreach (var c in cameras.GetComponentsInChildren<Camera>(true))
            {
                if (Atmosphere.HasSky) c.clearFlags = CameraClearFlags.Skybox;
                Atmosphere.Camera(c);
            }
        }

        static void Top(Camera cam, Bounds b, ViewSpec v)
        {
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(b.extents.z, b.extents.x / v.Aspect) * v.Margin;
            cam.transform.SetPositionAndRotation(new Vector3(b.center.x, b.max.y + 10f, b.center.z), Quaternion.Euler(90f, 0f, 0f));
            cam.farClipPlane = b.size.y + 20f;
        }

        /// <summary>Clearance (m) in front of the frame's ground plane that the camera needs.</summary>
        const float StandMargin = 5f;

        static void Export(Camera cam, Bounds all, ViewDto view, Vector3 focus, float zoom, float aspect, Look look)
        {
            cam.orthographic = true;
            cam.orthographicSize = zoom;
            var rot = Quaternion.Euler(view.Pitch, view.Yaw, 0f);
            float standOff = StandOff(rot, focus, zoom, aspect);
            cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * standOff, rot);
            cam.farClipPlane = standOff + all.size.magnitude;
            var t = look.Light("shadows") is { } sh ? J.Get(sh, "tactical") : null;
            if (t == null) throw new InvalidDataException("World: light.json has no shadows.tactical (the export views' shadow fit)");
            var ts = cam.gameObject.AddComponent<TacticalShadows>();
            ts.focusDepth = standOff;
            ts.nearBand = J.F(t, "nearBand"); ts.farBand = J.F(t, "farBand"); ts.cascades = (int)J.F(t, "cascades"); ts.fade = J.F(t, "fade");
            var air = J.A(t, "airSplits"); ts.airSplits = new Vector2((float)(double)air[0], (float)(double)air[1]);
        }

        /// <summary>Distance from the focus back along the view at which an orthographic camera of this size clears its frame column.</summary>
        public static float StandOff(Quaternion rot, Vector3 focus, float zoom, float aspect) => Mathf.Max(Clearance(rot, focus, zoom * aspect, zoom) + StandMargin, zoom);

        /// <summary>How far back along the view (m, from the focus) the nearest geometry inside the orthographic frame column lies:
        /// the column is the frame rectangle (halfW × halfH about the focus) extended toward the camera. Generated meshes are tested
        /// per triangle (camera-space box against the rectangle); kit instances (asset meshes) by their renderer bounds. Runs before
        /// the horizon band and the sea are built.</summary>
        static float Clearance(Quaternion rot, Vector3 focus, float halfW, float halfH)
        {
            var toCam = Matrix4x4.TRS(focus, rot, Vector3.one).inverse;
            float need = 0f;
            var poly = new List<Vector3>(9); var tmp = new List<Vector3>(9);
            foreach (var mf in Object.FindObjectsByType<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (!mesh || !mf.TryGetComponent<MeshRenderer>(out var r) || !r.enabled || !mf.gameObject.activeInHierarchy) continue;
                var (lo, hi) = Box(toCam, r.bounds);
                if (lo.x > halfW || hi.x < -halfW || lo.y > halfH || hi.y < -halfH || -lo.z <= need) continue;
                if (UnityEditor.AssetDatabase.Contains(mesh) || !mesh.isReadable) { need = Mathf.Max(need, -lo.z); continue; }
                var m = toCam * mf.transform.localToWorldMatrix;
                var v = mesh.vertices;
                for (int i = 0; i < v.Length; i++) v[i] = m.MultiplyPoint3x4(v[i]);
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var tri = mesh.GetTriangles(s);
                    for (int i = 0; i < tri.Length; i += 3)
                    {
                        Vector3 a = v[tri[i]], b = v[tri[i + 1]], c = v[tri[i + 2]];
                        if (-Mathf.Min(a.z, Mathf.Min(b.z, c.z)) <= need) continue;
                        if (Mathf.Min(a.x, Mathf.Min(b.x, c.x)) > halfW || Mathf.Max(a.x, Mathf.Max(b.x, c.x)) < -halfW) continue;
                        if (Mathf.Min(a.y, Mathf.Min(b.y, c.y)) > halfH || Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < -halfH) continue;
                        need = Mathf.Max(need, -NearestInColumn(a, b, c, halfW, halfH, poly, tmp));
                    }
                }
            }
            return need;
        }

        /// <summary>Smallest camera-space z of triangle abc inside the column |x| ≤ halfW, |y| ≤ halfH (the triangle clipped to the
        /// column; z is linear over it, so the minimum is at a vertex of the clipped polygon); +∞ when it misses the column.</summary>
        static float NearestInColumn(Vector3 a, Vector3 b, Vector3 c, float halfW, float halfH, List<Vector3> poly, List<Vector3> tmp)
        {
            poly.Clear(); poly.Add(a); poly.Add(b); poly.Add(c);
            Clip(poly, tmp, 0, 1f, halfW); Clip(poly, tmp, 0, -1f, halfW); Clip(poly, tmp, 1, 1f, halfH); Clip(poly, tmp, 1, -1f, halfH);
            float z = float.PositiveInfinity;
            foreach (var p in poly) z = Mathf.Min(z, p.z);
            return z;
        }

        /// <summary>Keeps the part of poly where sign·p[axis] ≤ limit (Sutherland–Hodgman, one plane).</summary>
        static void Clip(List<Vector3> poly, List<Vector3> tmp, int axis, float sign, float limit)
        {
            if (poly.Count == 0) return;
            tmp.Clear();
            for (int i = 0; i < poly.Count; i++)
            {
                Vector3 p = poly[i], q = poly[(i + 1) % poly.Count];
                float dp = sign * p[axis] - limit, dq = sign * q[axis] - limit;
                if (dp <= 0f) tmp.Add(p);
                if ((dp < 0f && dq > 0f) || (dp > 0f && dq < 0f)) tmp.Add(Vector3.Lerp(p, q, dp / (dp - dq)));
            }
            poly.Clear(); poly.AddRange(tmp);
        }

        static (Vector3 lo, Vector3 hi) Box(Matrix4x4 m, Bounds b)
        {
            Vector3 lo = Vector3.positiveInfinity, hi = Vector3.negativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                var p = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p);
            }
            return (lo, hi);
        }

        /// <summary>Tightest fit of the bounds' eight corners (camera-local c, relative to the bounds centre) for a fixed
        /// orientation: the eye sits at (a, b, -d), and |c.x - a| ≤ tanH (c.z + d) for every corner (likewise y), which
        /// gives d = (max(c.x - tanH c.z) - min(c.x + tanH c.z)) / 2 tanH and a = the midpoint. Margin shrinks the frustum.</summary>
        static void Fit(Camera cam, Bounds bounds, ViewSpec v)
        {
            cam.orthographic = false;
            cam.fieldOfView = v.Fov;
            var rot = Quaternion.Euler(v.Pitch, v.Yaw, 0f); var inv = Quaternion.Inverse(rot);
            float tanV = Mathf.Tan(v.Fov * 0.5f * Mathf.Deg2Rad) / v.Margin, tanH = tanV * v.Aspect;
            float hiX = float.MinValue, loX = float.MaxValue, hiY = float.MinValue, loY = float.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                var c = inv * Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                hiX = Mathf.Max(hiX, c.x - tanH * c.z); loX = Mathf.Min(loX, c.x + tanH * c.z);
                hiY = Mathf.Max(hiY, c.y - tanV * c.z); loY = Mathf.Min(loY, c.y + tanV * c.z);
            }
            float d = Mathf.Max((hiX - loX) / (2f * tanH), (hiY - loY) / (2f * tanV));
            var eye = new Vector3((hiX + loX) * 0.5f, (hiY + loY) * 0.5f, -d);
            cam.transform.SetPositionAndRotation(bounds.center + rot * eye, rot);
            cam.farClipPlane = d + 2f * bounds.extents.magnitude;
        }

        /// <summary>Export-view focus: the export's own (street datum), or `look` = a plan point, or `focus` = an element's top centre, or
        /// `street` = the street-level shopfront (lobbies excluded) on that street with the most others within `radius`.</summary>
        static Vector3 Focus(Transform root, WorldDto w, ViewSpec v)
        {
            if (v.Look is { } at) return at;
            if (v.Focus != null)
            {
                var t = FindDeep(root, v.Focus) ?? throw new AnchorMissing($"World: view {v.Name} skipped: `{v.Focus}` not in this build");
                var b = Inspect.Bounds(t);
                return new Vector3(b.center.x, b.max.y, b.center.z);
            }
            if (v.Street != null)
            {
                var shops = w.Frontages.FindAll(f => f.Street == v.Street && f.Level == "street" && !f.Lobby);
                if (shops.Count == 0) throw new AnchorMissing($"World: view {v.Name} skipped: no shopfronts on `{v.Street}`");
                Vector2 best = default; int most = -1;
                foreach (var f in shops)
                {
                    var m = (f.A + f.B) * 0.5f; int n = 0;
                    foreach (var o in shops) if (((o.A + o.B) * 0.5f - m).magnitude <= v.Radius) n++;
                    if (n > most) { most = n; best = m; }
                }
                return new Vector3(best.x, 0f, best.y);
            }
            return new Vector3(w.View.Focus.x, 0f, w.View.Focus.y);
        }

        static void Stand(Camera cam, Vector3 eye, Vector3 look, ViewSpec v)
        {
            cam.orthographic = false;
            cam.fieldOfView = v.Fov;
            cam.farClipPlane = float.IsNaN(v.Far) ? 3000f : v.Far;
            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
        }

        static void Along(Camera cam, WorldDto w, ViewSpec v)
        {
            RoadDto line = null;
            foreach (var r in w.Ground.Foot) if (r.Id == v.Along) line = r;
            foreach (var r in w.Ground.Roads) if (r.Id == v.Along) line = r;
            if (line == null) throw new AnchorMissing($"World: view {v.Name} skipped: `{v.Along}` is not a road or foot line in this export");
            var dir = (line.Line[1] - line.Line[0]).normalized; // first segment
            var at = Vector2.Lerp(line.Line[0], line.Line[1], v.At) + new Vector2(dir.y, -dir.x) * v.Side; // side > 0 = right of the line
            cam.orthographic = false;
            cam.fieldOfView = v.Fov;
            cam.farClipPlane = v.Far;
            cam.transform.SetPositionAndRotation(new Vector3(at.x, line.Level + v.EyeHeight, at.y),
                Quaternion.Euler(v.Pitch, Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, 0f));
        }

        /// <summary>Union of the renderer bounds of the named layers or elements ('*' = all).</summary>
        static Bounds Frame(Transform root, ViewSpec v, Bounds all)
        {
            Bounds? b = null;
            foreach (var name in v.Frame)
            {
                if (name == "*") return all;
                var t = FindDeep(root, name) ?? throw new AnchorMissing($"World: view {v.Name} skipped: `{name}` not in this build");
                var tb = Inspect.Bounds(t);
                if (b is { } have) { have.Encapsulate(tb); b = have; } else b = tb;
            }
            return b ?? throw new InvalidDataException($"World: view {v.Name} has no frame");
        }

        static Transform FindDeep(Transform t, string name)
        {
            foreach (Transform c in t)
            {
                if (c.name == name) return c;
                var hit = FindDeep(c, name);
                if (hit) return hit;
            }
            return null;
        }

        static void Sun(Transform lighting, Look look)
        {
            var e = look.Entry("sun");
            var sun = new GameObject("sun").AddComponent<Light>();
            sun.transform.SetParent(lighting, false);
            sun.type = LightType.Directional;
            sun.color = look.Colour(e);
            sun.intensity = J.F(e, "intensity");
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(J.F(e, "pitch"), J.F(e, "yaw"), 0f);
            RenderSettings.sun = sun;
        }

        static Camera Make(Transform parent, string name, Color clear)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = clear;
            return cam;
        }

        sealed class AnchorMissing : System.Exception { public AnchorMissing(string m) : base(m) { } }
    }
}
