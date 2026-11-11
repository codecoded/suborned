using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Play
{
    /// <summary>Brief 109 round 4 (Sky: "it needs to be done properly, same for car head, tail and streak lights. Not just some flat opacity"): every drawn
    /// car's lights, on its own model's lamps (Editor Juice109Assets reads them from the model: Resources/Juice109/lamps-model.txt, front +Z):
    ///  - headlamps: a lens glow at each lamp (URP's Lens Flare (SRP), procedural, occluded by the depth); at night or in rain while it drives, two
    ///    volumetric-looking beams (Shader Graph Juice_Beam: soft edges by view angle, falling off along the length and from the core, faded where they meet
    ///    the street, drifting dust, HDR so the bloom takes the hot core by the lamp) and one real spot light making the pool on the street;
    ///  - tail lamps: a red HDR glow at each (the bloom takes it), at night always, by day only braking; brighter on braking (the car losing speed);
    ///  - at night while it drives, a short light streak behind each tail lamp (Shader Graph Juice_Streak: a soft core that tapers and fades, HDR).
    /// Lights only where they belong: a parked car at night shows its tails dim and no beams. All view only; nothing reads it.</summary>
    [DefaultExecutionOrder(150)]
    public sealed class Juice109Lights : MonoBehaviour
    {
        sealed class Rig
        {
            public Transform body; public string key; public Vector3[] F = new Vector3[0], R = new Vector3[0];
            public LensFlareComponentSRP[] flares; public MeshRenderer[] beams; public Transform[] tails; public Material tailMat; public TrailRenderer[] streaks; public Light spot;
            public Vector3 last; public float speed, brake, lit, moving, tailWas = -1f; public bool seen;
        }
        readonly Dictionary<Transform, Rig> rigs = new(); readonly Dictionary<string, (Vector3[] f, Vector3[] r)> lamps = new();
        Material glowSrc, beamMat, streakMat; LensFlareDataSRP headData; Mesh headMesh; float refreshT;
        public static int Cars { get; private set; }

        void Start()
        {
            glowSrc = Resources.Load<Material>("Juice109/Juice-Glow");
            var b = Resources.Load<Material>("Juice109/Juice-Beam"); if (b) { beamMat = new Material(b) { name = "Juice109 headlight beam" }; beamMat.SetColor("_Tint", new Color(1.5f, 1.4f, 1.2f)); beamMat.SetVector("_Params", new Vector4(1.3f, 1.5f, 0.8f, 0.35f)); }
            var s = Resources.Load<Material>("Juice109/Juice-Streak"); if (s) { streakMat = new Material(s) { name = "Juice109 tail streak" }; streakMat.SetColor("_Tint", new Color(3.2f, 0.45f, 0.35f)); }
            headData = FlareData(new Color(1f, 0.93f, 0.8f), 1f); headMesh = BeamMesh(9f, 0.09f, 1.9f, 12, 6);
        }

        /// <summary>Procedural lens flare data: a soft round glow and a faint horizontal streak through it (no ghosts: it is a lamp, not a sun).</summary>
        public static LensFlareDataSRP FlareData(Color c, float size)
        {
            var d = ScriptableObject.CreateInstance<LensFlareDataSRP>(); d.name = "Juice109 lamp glow";
            var glow = new LensFlareDataElementSRP { flareType = SRPLensFlareType.Circle, tint = c, localIntensity = 1f, fallOff = 0.45f, edgeOffset = 0.15f, sizeXY = Vector2.one, uniformScale = 0.5f * size, blendMode = SRPLensFlareBlendMode.Additive, position = 0f };
            var core = new LensFlareDataElementSRP { flareType = SRPLensFlareType.Circle, tint = Color.white, localIntensity = 1.4f, fallOff = 0.8f, edgeOffset = 0.05f, sizeXY = Vector2.one, uniformScale = 0.14f * size, blendMode = SRPLensFlareBlendMode.Additive, position = 0f };
            var streak = new LensFlareDataElementSRP { flareType = SRPLensFlareType.Circle, tint = c * 0.6f, localIntensity = 0.25f, fallOff = 0.5f, edgeOffset = 0.2f, sizeXY = new Vector2(3f, 0.1f), uniformScale = 0.4f * size, blendMode = SRPLensFlareBlendMode.Additive, position = 0f };
            d.elements = new[] { glow, core, streak }; return d;
        }

        /// <summary>An open cone along +Z with outward normals (the soft edge is by view angle) and UVs (u round it, v along it, 0 at the lamp).</summary>
        public static Mesh BeamMesh(float len, float r0, float r1, int seg, int rings)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>(); float slope = (r1 - r0) / len;
            for (int j = 0; j <= rings; j++)
            {
                float t = j / (float)rings, r = Mathf.Lerp(r0, r1, t);
                for (int i = 0; i <= seg; i++) { float a = i / (float)seg * Mathf.PI * 2f; var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f); v.Add(dir * r + Vector3.forward * t * len); n.Add((dir - Vector3.forward * slope).normalized); uv.Add(new Vector2(i / (float)seg, t)); }
            }
            for (int j = 0; j < rings; j++) for (int i = 0; i < seg; i++) { int a = j * (seg + 1) + i, b = a + seg + 1; tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            var m = new Mesh { name = "Juice109 beam cone" }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.bounds = new Bounds(new Vector3(0, 0, len / 2f), new Vector3(2 * r1, 2 * r1, len)); return m;
        }

        (Vector3[] f, Vector3[] r) Lamps(string model)
        {
            if (lamps.TryGetValue(model, out var l)) return l;
            Vector3[] P(string line) { var o = new List<Vector3>(); foreach (var q in line.Split(';')) { var c = q.Trim().Split(','); if (c.Length == 3) o.Add(new Vector3(F(c[0]), F(c[1]), F(c[2]))); } return o.ToArray(); }
            Vector3[] f = new Vector3[0], r = new Vector3[0]; var t = Resources.Load<TextAsset>("Juice109/lamps-" + model);
            if (t) foreach (var line in t.text.Split('\n')) { if (line.StartsWith("F ")) f = P(line[2..]); else if (line.StartsWith("R ")) r = P(line[2..]); }
            return lamps[model] = (f, r);
        }
        static float F(string s) => float.Parse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

        void Update()
        {
            var h = Juice109Fx.Host; if (!h || h.Views == null) return;
            if ((refreshT -= Time.unscaledDeltaTime) > 0f) return; refreshT = 0.5f;
            foreach (var r in rigs.Values) r.seen = false;
            foreach (var (key, body, model) in h.Views.CarBodies())
            {
                if (!rigs.TryGetValue(body, out var rig)) { var (f, rr) = Lamps(model); if (f.Length == 0 && rr.Length == 0) continue; rig = Build(key, body, f, rr); rigs[body] = rig; }
                rig.seen = true;
            }
            var gone = new List<Transform>(); foreach (var kv in rigs) if (!kv.Key || !kv.Value.seen) gone.Add(kv.Key);
            foreach (var k in gone) { if (rigs[k].spot) Destroy(rigs[k].spot.gameObject); rigs.Remove(k); } // the rest are the body's children: gone with it, or hidden with it
            Cars = rigs.Count;
        }

        Rig Build(string key, Transform body, Vector3[] f, Vector3[] r)
        {
            var rig = new Rig { body = body, key = key, F = f, R = r, last = body.position };
            float ls = Mathf.Max(1e-4f, body.lossyScale.x);
            rig.flares = new LensFlareComponentSRP[f.Length]; rig.beams = new MeshRenderer[f.Length];
            for (int i = 0; i < f.Length; i++)
            {
                var g = new GameObject("Juice109 headlamp " + i); g.transform.SetParent(body, false); g.transform.localPosition = f[i] + Vector3.forward * 0.05f;
                var lf = g.AddComponent<LensFlareComponentSRP>(); lf.lensFlareData = headData; lf.intensity = 0f; lf.scale = 0.5f; lf.useOcclusion = false; lf.enabled = false;  lf.attenuationByLightShape = false; lf.allowOffScreen = false; lf.maxAttenuationDistance = 400f; lf.maxAttenuationScale = 400f; rig.flares[i] = lf; // a car lamp is rarely hidden from this top-down view; occlusion costs a sampling pass per lamp
                if (beamMat)
                {
                    var bg = new GameObject("Juice109 headlight beam " + i); bg.transform.SetParent(body, false); bg.transform.localPosition = f[i] + Vector3.forward * 0.08f; bg.transform.localRotation = Quaternion.Euler(7f, 0f, 0f); bg.transform.localScale = Vector3.one / ls;
                    bg.AddComponent<MeshFilter>().sharedMesh = headMesh; var mr = bg.AddComponent<MeshRenderer>(); mr.sharedMaterial = beamMat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; mr.enabled = false; rig.beams[i] = mr;
                }
            }
            if (f.Length > 0)
            {
                var sg = new GameObject("Juice109 headlight pool (" + key + ")"); sg.transform.SetParent(transform, false); var sp = sg.AddComponent<Light>();
                sp.type = LightType.Spot; sp.shadows = LightShadows.None; sp.color = new Color(1f, 0.92f, 0.78f); sp.range = 18f; sp.spotAngle = 70f; sp.innerSpotAngle = 35f; sp.intensity = 0f; sp.enabled = false; rig.spot = sp;
            }
            rig.tails = new Transform[r.Length];
            if (glowSrc && r.Length > 0) { rig.tailMat = new Material(glowSrc) { name = "Juice109 tail lamps (" + key + ")" }; rig.tailMat.SetColor("_Tint", Color.black); }
            rig.streaks = new TrailRenderer[r.Length];
            for (int i = 0; i < r.Length; i++)
            {
                if (rig.tailMat)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Juice109 tail lamp " + i; Destroy(q.GetComponent<Collider>()); q.transform.SetParent(body, false); q.transform.localPosition = r[i] - Vector3.forward * 0.04f;
                    var mr = q.GetComponent<MeshRenderer>(); mr.sharedMaterial = rig.tailMat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; rig.tails[i] = q.transform;
                }
                if (streakMat)
                {
                    var tg = new GameObject("Juice109 tail streak " + i); tg.transform.SetParent(body, false); tg.transform.localPosition = r[i] - Vector3.forward * 0.06f;
                    var tr = tg.AddComponent<TrailRenderer>(); tr.time = 0.32f; tr.minVertexDistance = 0.25f; tr.widthMultiplier = 0.16f; tr.textureMode = LineTextureMode.Stretch; tr.emitting = false; tr.sharedMaterial = streakMat;
                    tr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.25f)); tr.shadowCastingMode = ShadowCastingMode.Off; tr.receiveShadows = false; rig.streaks[i] = tr;
                }
            }
            return rig;
        }

        static void Off(Rig rig)
        {
            foreach (var lf in rig.flares) if (lf && lf.enabled) lf.enabled = false;
            foreach (var mr in rig.beams) if (mr && mr.enabled) mr.enabled = false;
            if (rig.spot && rig.spot.enabled) rig.spot.enabled = false;
            foreach (var t in rig.tails) if (t && t.gameObject.activeSelf) t.gameObject.SetActive(false);
            foreach (var st in rig.streaks) if (st) st.emitting = false;
        }

        void LateUpdate()
        {
            var h = Juice109Fx.Host; var cam = h ? h.View : null; if (!cam) return;
            bool night = PlayView107.Night || Weather107.Night, rain = Weather107.Now != null && Weather107.Now.rain > 0.01f, dark = night || rain;
            float dt = Mathf.Max(1e-4f, Time.unscaledDeltaTime), simDt = h.Paused ? 0f : dt * h.Speed;
            foreach (var rig in rigs.Values)
            {
                var b = rig.body; if (!b) continue;
                var vp = cam.WorldToViewportPoint(b.position); bool onScreen = vp.z > 0f && vp.x > -0.15f && vp.x < 1.15f && vp.y > -0.15f && vp.y < 1.15f;
                if (!onScreen) { Off(rig); rig.last = b.position; continue; } // off screen: no lights work at all (its lamps come back as it comes into view)
                var mv = b.position - rig.last; mv.y = 0f; rig.last = b.position;
                float now = simDt > 0f ? mv.magnitude / simDt : rig.speed; if (now > 40f) now = rig.speed; // a jump (a restart, a teleport of the view) is no speed
                float was = rig.speed; rig.speed = Mathf.Lerp(rig.speed, now, 1f - Mathf.Exp(-dt / 0.15f));
                if (simDt > 0f && (was - rig.speed) / simDt > 1.4f && was > 1f) rig.brake = 1f; else rig.brake = Mathf.MoveTowards(rig.brake, 0f, dt / 0.5f); // losing speed: the brake lights
                rig.moving = Mathf.MoveTowards(rig.moving, rig.speed > 0.6f ? 1f : 0f, dt / 0.3f);
                rig.lit = Mathf.MoveTowards(rig.lit, dark ? 1f : 0f, dt / 0.4f);
                float head = rig.lit * Mathf.Lerp(0.25f, 1f, rig.moving) + (1f - rig.lit) * 0.3f * rig.moving * (rig.key == "car/getaway" ? 1f : 0f); // by day only our car's lamps, when it drives
                foreach (var lf in rig.flares) if (lf) { float k = head * (rain ? 1.3f : 1f); bool on = k > 0.01f; if (lf.enabled != on) lf.enabled = on; lf.intensity = k; } // a dark lamp's component off: each costs CPU every frame
                bool beams = rig.lit * rig.moving > 0.02f; foreach (var mr in rig.beams) if (mr && mr.enabled != beams) mr.enabled = beams;
                if (rig.spot)
                {
                    bool on = rig.lit > 0.02f && (rig.moving > 0.02f || rig.key == "car/getaway");
                    if (rig.spot.enabled != on) rig.spot.enabled = on;
                    if (on)
                    {
                        float z = 0f; foreach (var p in rig.F) z = Mathf.Max(z, p.z);
                        rig.spot.transform.SetPositionAndRotation(b.TransformPoint(new Vector3(0f, 0.75f, z)), Quaternion.LookRotation(b.forward * 0.86f - Vector3.up * 0.5f, Vector3.up));
                        rig.spot.intensity = 9f * rig.lit * Mathf.Lerp(0.45f, 1f, rig.moving);
                    }
                }
                float tail = rig.lit * 0.9f + rig.brake * 2.6f;
                if (rig.tailMat && Mathf.Abs(tail - rig.tailWas) > 0.005f) { rig.tailMat.SetColor("_Tint", new Color(2.6f, 0.22f, 0.16f) * tail); rig.tailWas = tail; }
                foreach (var t in rig.tails) if (t) { bool show = tail > 0.02f; if (t.gameObject.activeSelf != show) t.gameObject.SetActive(show); if (show) { t.rotation = cam.transform.rotation; t.localScale = Vector3.one * (0.42f + 0.14f * rig.brake) / Mathf.Max(1e-4f, b.lossyScale.x); } }
                foreach (var s in rig.streaks) if (s) s.emitting = night && rig.moving > 0.5f;
            }
        }
    }
}
