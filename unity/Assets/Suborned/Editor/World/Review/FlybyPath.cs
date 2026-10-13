using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>A camera pose on the flyby.</summary>
    public struct Pose
    {
        public Vector3 Position; public Quaternion Rotation;
        public float Fov, OrthoSize, Near, Far, ShadowDistance;
        /// <summary>Set on the play stop and the moves into and out of it: the street focus lies this far along the view, and the
        /// shadows are fitted to it as the game's TacticalShadows does (NaN = the plain ShadowDistance).</summary>
        public float ShadowFocus;
        public bool Ortho;
        public string Label; public int Stop; public bool Holding;

        public void Apply(Camera cam)
        {
            cam.transform.SetPositionAndRotation(Position, Rotation);
            cam.orthographic = Ortho;
            if (Ortho) cam.orthographicSize = OrthoSize; else cam.fieldOfView = Fov;
            cam.nearClipPlane = Near; cam.farClipPlane = Far;
        }
    }

    /// <summary>The flyby from flyby.json, resolved against the export: one key per stop (look target, yaw, pitch, distance, fov),
    /// each held (drifting in yaw) then eased to the next. Scalars ease with smoothstep (distance and fov in log space, so a
    /// dolly zoom into the orthographic play camera reads as one move); on long moves the eye lifts by a sine bump while still
    /// looking at the eased target. Same file + same export = same path.</summary>
    public sealed class FlybyPath
    {
        public sealed class Key
        {
            public string Name;
            public Vector3 Target; public float Yaw, Pitch, Dist, Fov;
            public float Ortho = float.NaN; // orthographic size while held
            public float Hold, Travel, Drift, Shadow;
            public float Start; // timeline seconds the hold begins
            public bool Contact = true; // gets a contact image (rail runs: their first and last key only)
            public bool Linear; // a rail key: the move to the next rail key is at constant speed, no eye lift
            public float EndYaw => Yaw + Drift * Hold;
        }

        public readonly List<Key> Keys = new();
        public int Fps, Width, Height, Quality, ContactWidth, ContactHeight;
        public string Ffmpeg; // render.ffmpeg: the encoder that writes flyby.mp4 (null: frames only)
        public int Crf = 18; public string Preset = "medium"; // render.crf / render.preset: H.264 rate control (a higher CRF = a smaller file)
        public float Duration;
        public float Aspect => (float)Width / Height;

        public static string DefaultPath => HarbourWorldBuilder.Arg("-flyby", Path.GetFullPath(Path.Combine(Application.dataPath, "Suborned/Editor/World/flyby.json")));

        public FlybyPath(string file, WorldIndex index)
        {
            var o = Json.Parse(File.ReadAllText(file));
            var render = J.Get(o, "render"); var size = J.A(render, "size"); var contact = J.A(render, "contact");
            Fps = (int)J.F(render, "fps"); Width = (int)(double)size[0]; Height = (int)(double)size[1]; Quality = (int)J.F(render, "quality");
            ContactWidth = (int)(double)contact[0]; ContactHeight = (int)(double)contact[1];
            Ffmpeg = J.S(render, "ffmpeg");
            if (J.Get(render, "crf") is double crf) Crf = (int)crf;
            if (J.S(render, "preset") is { } preset) Preset = preset;
            var defs = J.Get(o, "defaults");
            float D(object s, string k) => J.Get(s, k) is double d ? (float)d : J.F(defs, k);
            float Opt(object s, string k, float f) => J.Get(s, k) is double d ? (float)d : f;
            var w = index.World;

            foreach (var s in J.A(o, "stops"))
            {
                var name = J.S(s, "name") ?? throw new InvalidDataException("World: flyby stop without name");
                var kind = J.S(s, "kind");
                Key K(string n) => new() { Name = n, Fov = D(s, "fov"), Hold = D(s, "hold"), Travel = D(s, "travel"), Drift = D(s, "drift"), Shadow = D(s, "shadowDistance") };
                switch (kind)
                {
                    case "fit":
                    {
                        var k = K(name);
                        var frame = J.A(s, "frame").Cast<string>().ToList();
                        Fit(k, index.Frame(frame), J.F(s, "yaw"), J.F(s, "pitch"), Opt(s, "margin", 1.05f));
                        Keys.Add(k); break;
                    }
                    case "play":
                    {
                        var k = K(name);
                        float zoom = Opt(s, "zoom", w.View.Zoom);
                        k.Target = new Vector3(w.View.Focus.x, 0f, w.View.Focus.y);
                        k.Yaw = w.View.Yaw; k.Pitch = w.View.Pitch; k.Ortho = zoom;
                        k.Fov = 2f; k.Dist = zoom / Mathf.Tan(k.Fov * 0.5f * Mathf.Deg2Rad); // the perspective twin of the ortho frame
                        Keys.Add(k); break;
                    }
                    case "street":
                    {
                        var street = J.S(s, "street");
                        var shops = w.Frontages.FindAll(f => f.Street == street && f.Level == "street" && !f.Lobby);
                        if (shops.Count == 0) throw new InvalidDataException($"World: flyby stop {name}: no shopfronts on `{street}`");
                        FrontageDto best = null; int most = -1;
                        foreach (var f in shops)
                        {
                            var m = (f.A + f.B) * 0.5f; int n = shops.Count(x => ((x.A + x.B) * 0.5f - m).magnitude <= 40f);
                            if (n > most) { most = n; best = f; }
                        }
                        var mid = (best.A + best.B) * 0.5f; var along = (best.B - best.A).normalized; float h = Opt(s, "eyeHeight", 1.7f), z = best.Z;
                        var outward = best.Normal * Opt(s, "out", 7f);
                        var k = K(name);
                        Look(k, Nudge(s, P(mid + outward - along * 22f, z + h), P(mid + best.Normal * 2f + along * 25f, z + Opt(s, "lookHeight", 3.5f))));
                        Keys.Add(k); break;
                    }
                    case "deck":
                    {
                        var level = w.Levels.Find(l => l.Name == J.S(s, "level")) ?? throw new InvalidDataException($"World: flyby stop {name}: no level `{J.S(s, "level")}`");
                        var deck = level.Decks.OrderByDescending(d => Area(d.Ring)).FirstOrDefault() ?? throw new InvalidDataException($"World: flyby stop {name}: level has no decks");
                        var (c, axis, len) = Axis(deck.Ring);
                        float h = Opt(s, "eyeHeight", 1.7f);
                        var k = K(name);
                        Look(k, Nudge(s, P(c - axis * len * 0.42f, level.Height + h), P(c + axis * len * 0.45f, level.Height + 1.5f)));
                        Keys.Add(k); break;
                    }
                    case "station":
                    {
                        var id = J.S(s, "id");
                        var st = w.Transit.Stations.Find(x => x.Id == id) ?? throw new InvalidDataException($"World: flyby stop {name}: no station `{id}`");
                        var (c, axis, len) = Axis(st.Ring);
                        float top = st.Base + st.Height, h = Opt(s, "eyeHeight", 1.7f);
                        var k = K(name);
                        Look(k, Nudge(s, P(c - axis * len * 0.45f, top + h), P(c + axis * (len * 0.5f + 60f), top + 2f)));
                        Keys.Add(k); break;
                    }
                    case "along":
                    {
                        var id = J.S(s, "id");
                        if (!index.ById.TryGetValue(id, out var el)) throw new InvalidDataException($"World: flyby stop {name}: no element `{id}`");
                        var b = el.Box; bool alongX = b.size.x >= b.size.z;
                        var axis = (alongX ? Vector2.right : Vector2.up) * (Opt(s, "reverse", 0f) > 0 ? -1f : 1f);
                        var side = new Vector2(axis.y, -axis.x);
                        float len = alongX ? b.size.x : b.size.z, at = Opt(s, "at", 0.5f), h = b.max.y + Opt(s, "eyeHeight", 1.7f);
                        var start = new Vector2(b.center.x, b.center.z) + axis * len * (at - 0.5f) + side * Opt(s, "side", 0f);
                        var k = K(name);
                        Look(k, P(start, h), P(start + axis * Opt(s, "ahead", 80f), b.max.y + Opt(s, "lookHeight", 1.5f)));
                        Keys.Add(k); break;
                    }
                    case "megas":
                    {
                        float yaw = J.F(s, "yaw"), step = Opt(s, "yawStep", 90f);
                        var yaws = J.Get(s, "yaws");
                        int i = 0;
                        foreach (var m in w.Megastructures)
                        {
                            var k = K($"{name}: {m.Id.Substring(m.Id.IndexOf('/') + 1)}");
                            float y = yaws != null && J.Get(yaws, m.Id) is double over ? (float)over : yaw + step * i;
                            i++;
                            Fit(k, index.ById[m.Id].Box, y, J.F(s, "pitch"), 1.15f);
                            Keys.Add(k);
                        }
                        break;
                    }
                    case "rail":
                    {   // ride a sky rail line: eye eyeHeight over the beam top on the line, from `from` to `to` metres along it, at `speed` m/s
                        var id = J.S(s, "id");
                        var line = w.Transit.SkyRail.Find(x => x.Id == id) ?? throw new InvalidDataException($"World: flyby stop {name}: no sky rail `{id}`");
                        float from = J.F(s, "from"), to = J.F(s, "to"), speed = Opt(s, "speed", 16f), step = Opt(s, "step", 25f), h = line.Height + Opt(s, "eyeHeight", 2.6f), ahead = Opt(s, "ahead", 70f);
                        int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(to - from) / step));
                        for (int i = 0; i <= n; i++)
                        {
                            float d = Mathf.Lerp(from, to, i / (float)n), dir = Mathf.Sign(to - from);
                            var at = Along(line.Line, d); var look = Along(line.Line, d + dir * ahead);
                            var k = K(i == 0 ? name : $"{name} {i}/{n}");
                            Look(k, P(at, h), P(look, h - Opt(s, "lookDrop", 4f)));
                            k.Linear = true; k.Drift = 0f; k.Contact = i == 0 || i == n;
                            k.Hold = i == 0 ? D(s, "hold") : 0f;
                            k.Travel = i < n ? Mathf.Abs(to - from) / n / speed : D(s, "travel");
                            Keys.Add(k);
                        }
                        break;
                    }
                    case "toward":
                    {   // eye eyeHeight over element `from`'s top, looking at element `to`'s centre (or at height lookZ above it)
                        if (!index.ById.TryGetValue(J.S(s, "from"), out var a)) throw new InvalidDataException($"World: flyby stop {name}: no element `{J.S(s, "from")}`");
                        if (!index.ById.TryGetValue(J.S(s, "to"), out var b)) throw new InvalidDataException($"World: flyby stop {name}: no element `{J.S(s, "to")}`");
                        var eye = new Vector3(a.Box.center.x, a.Box.max.y + Opt(s, "eyeHeight", 30f), a.Box.center.z);
                        var look = b.Box.center; if (J.Get(s, "lookZ") is double lz) look.y = (float)lz;
                        var dir = (look - eye).normalized; float back = Opt(s, "back", 0f);
                        var k = K(name);
                        Look(k, eye - new Vector3(dir.x, 0, dir.z).normalized * back, look);
                        Keys.Add(k); break;
                    }
                    default: throw new InvalidDataException($"World: flyby stop {name} has unknown kind `{kind}`");
                }
            }
            if (Keys.Count == 0) throw new InvalidDataException("World: flyby has no stops");
            float t = 0f;
            for (int i = 0; i < Keys.Count; i++)
            {
                Keys[i].Start = t;
                t += Keys[i].Hold + (i < Keys.Count - 1 ? Keys[i].Travel : 0f);
            }
            Duration = t;
        }

        static Vector3 P(Vector2 plan, float h) => new(plan.x, h, plan.y);

        /// <summary>The point d metres along a polyline from its start (clamped to its ends).</summary>
        static Vector2 Along(Vector2[] line, float d)
        {
            if (d <= 0f) return line[0];
            for (int i = 0; i + 1 < line.Length; i++)
            {
                float l = Vector2.Distance(line[i], line[i + 1]);
                if (d <= l) return Vector2.Lerp(line[i], line[i + 1], l > 0 ? d / l : 0f);
                d -= l;
            }
            return line[^1];
        }

        static float Area(Vector2[] r)
        {
            float a = 0; for (int i = 0; i < r.Length; i++) { var p = r[i]; var q = r[(i + 1) % r.Length]; a += p.x * q.y - q.x * p.y; }
            return Mathf.Abs(a) * 0.5f;
        }

        /// <summary>Centre, long axis and its length of a ring's bounding box.</summary>
        static (Vector2 c, Vector2 axis, float len) Axis(Vector2[] ring)
        {
            var b = WorldIndex.Ring(ring, 0, 0);
            var c = new Vector2(b.center.x, b.center.z);
            return b.size.x >= b.size.z ? (c, Vector2.right, b.size.x) : (c, Vector2.up, b.size.z);
        }

        /// <summary>Stop options `side` (m to the right of the view, level) and `shift` (m along it), applied to eye and look alike, so a
        /// ground-level stop can step clear of a column or tree standing in front of the lens without changing what it frames.</summary>
        static (Vector3 eye, Vector3 look) Nudge(object s, Vector3 eye, Vector3 look)
        {
            float side = J.Get(s, "side") is double a ? (float)a : 0f, shift = J.Get(s, "shift") is double b ? (float)b : 0f;
            var f = new Vector3(look.x - eye.x, 0f, look.z - eye.z).normalized; var r = new Vector3(f.z, 0f, -f.x);
            var o = r * side + f * shift;
            return (eye + o, look + o);
        }

        static void Look(Key k, (Vector3 eye, Vector3 look) p) => Look(k, p.eye, p.look);

        /// <summary>Key standing at eye looking at look.</summary>
        static void Look(Key k, Vector3 eye, Vector3 look)
        {
            var d = look - eye;
            k.Target = look; k.Dist = d.magnitude;
            k.Yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            k.Pitch = Mathf.Asin(-d.y / k.Dist) * Mathf.Rad2Deg;
        }

        /// <summary>Tightest perspective fit of the bounds' corners for a fixed orientation (as Rig.Fit), as a look target and distance.</summary>
        void Fit(Key k, Bounds bounds, float yaw, float pitch, float margin)
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f); var inv = Quaternion.Inverse(rot);
            float tanV = Mathf.Tan(k.Fov * 0.5f * Mathf.Deg2Rad) / margin, tanH = tanV * Aspect;
            float hiX = float.MinValue, loX = float.MaxValue, hiY = float.MinValue, loY = float.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                var c = inv * Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                hiX = Mathf.Max(hiX, c.x - tanH * c.z); loX = Mathf.Min(loX, c.x + tanH * c.z);
                hiY = Mathf.Max(hiY, c.y - tanV * c.z); loY = Mathf.Min(loY, c.y + tanV * c.z);
            }
            float d = Mathf.Max((hiX - loX) / (2f * tanH), (hiY - loY) / (2f * tanV));
            var eye = bounds.center + rot * new Vector3((hiX + loX) * 0.5f, (hiY + loY) * 0.5f, -d);
            var fwd = rot * Vector3.forward;
            float along = Vector3.Dot(bounds.center - eye, fwd);
            k.Target = eye + fwd * along; k.Dist = along; k.Yaw = yaw; k.Pitch = pitch;
        }

        /// <summary>The pose at timeline second t (clamped).</summary>
        public Pose Evaluate(float t)
        {
            t = Mathf.Clamp(t, 0f, Duration);
            int i = Keys.Count - 1;
            while (i > 0 && Keys[i].Start > t) i--;
            var a = Keys[i];
            float s = t - a.Start;
            if (s <= a.Hold || i == Keys.Count - 1)
            {
                var p = Orbit(a.Target, a.Yaw + a.Drift * Mathf.Min(s, a.Hold), a.Pitch, a.Dist, a.Fov, 0f, a.Shadow);
                p.ShadowFocus = float.NaN;
                if (!float.IsNaN(a.Ortho)) { p.Ortho = true; p.OrthoSize = a.Ortho; p.ShadowFocus = a.Dist; }
                p.Label = a.Name; p.Stop = i; p.Holding = true;
                return p;
            }
            var b = Keys[i + 1];
            bool linear = a.Linear && b.Linear;
            float u = Mathf.Clamp01((s - a.Hold) / Mathf.Max(a.Travel, 1e-3f)), e = linear ? u : u * u * (3f - 2f * u);
            float yaw0 = a.EndYaw, yaw1 = yaw0 + Mathf.DeltaAngle(yaw0, b.Yaw);
            var target = Vector3.Lerp(a.Target, b.Target, e);
            float floor = linear ? 0f : Mathf.Clamp(Vector3.Distance(a.Target, b.Target) * 0.35f, 0f, 220f) * Mathf.Sin(Mathf.PI * u); // eye height to keep above roofs mid-move
            var pose = Orbit(target, Mathf.Lerp(yaw0, yaw1, e), Mathf.Lerp(a.Pitch, b.Pitch, e),
                Mathf.Exp(Mathf.Lerp(Mathf.Log(a.Dist), Mathf.Log(b.Dist), e)),
                2f * Mathf.Atan(Mathf.Exp(Mathf.Lerp(Mathf.Log(Mathf.Tan(a.Fov * 0.5f * Mathf.Deg2Rad)), Mathf.Log(Mathf.Tan(b.Fov * 0.5f * Mathf.Deg2Rad)), e))) * Mathf.Rad2Deg,
                floor, Mathf.Exp(Mathf.Lerp(Mathf.Log(a.Shadow), Mathf.Log(b.Shadow), e)));
            pose.Label = $"{a.Name} → {b.Name}"; pose.Stop = i; pose.Holding = false;
            pose.ShadowFocus = !float.IsNaN(a.Ortho) || !float.IsNaN(b.Ortho) ? Vector3.Distance(pose.Position, target) : float.NaN;
            return pose;
        }

        static Pose Orbit(Vector3 target, float yaw, float pitch, float dist, float fov, float floor, float shadow)
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            var eye = target - rot * Vector3.forward * dist;
            float lift = Mathf.Max(0f, floor - eye.y);
            eye += Vector3.up * lift;
            if (lift > 0f) rot = Quaternion.LookRotation(target - eye, Vector3.up);
            float reach = Vector3.Distance(eye, target);
            return new Pose
            {
                Position = eye, Rotation = rot, Fov = fov,
                Near = Mathf.Clamp(reach * 0.01f, 0.2f, 10f), Far = reach + 12000f, ShadowDistance = shadow, // the horizon band reaches 8 km, the planet 6.5 km
            };
        }
    }
}
