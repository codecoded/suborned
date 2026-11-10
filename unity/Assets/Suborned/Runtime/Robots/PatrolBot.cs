using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Robots
{
    /// <summary>
    /// Brief 104: Vela's ball-balancing patrol robot, DK_PatrolBot104 (tools/decokit/patrolbot104.py; briefs/104/vela/patrolbot/notes.md), alive. Its parts
    /// are rigid siblings in the FBX, each at its own pivot; Setup puts every part but the Ball under the Body (keeping where it stands), each forearm under
    /// its arm, and adds the LED face over the Screen's glass (a `_Face` renderer, DK_Face_Amber, which RobotFace drives as on every other robot).
    /// Each frame Drive poses it from what the host reads (Vela's demo timeline, make/demo_motion.py, as maths):
    ///  · the Ball turns to the way it travels and rolls by distance / 0.20 m, so it never slides;
    ///  · the Body leans on a damped spring toward speed and acceleration (into the travel, into a turn; ~1.5 Hz, damping 0.24), at most 10 deg, so a stop
    ///    settles with a small wobble; each shot and a hit kick it;
    ///  · drawn: the ChestPanel hinges open 150 deg, then the Gadget slides 0.14 m out of the well (Sky: longer than the demo's 0.11, so the sidearm clears
    ///    the open hatch); holstered, the reverse; each shot a muzzle flash (two frames) and an 18 mm recoil;
    ///  · a hit: the arms flinch up; the arms otherwise sway a little, more while it rolls;
    ///  · the siren: the beacon's dome lit and a bright lobe sweeping round it at 1.6 rev/s (the dome is opaque, so the sweep is on its glass; the lamp inside spins).
    /// Every renderer here keeps shared materials and no property block, so the GPU Resident Drawer keeps instancing them: the lit dome, the lobe and the flash
    /// are shared materials (one set for every robot), swapped or shown, never written per robot. The face is the one renderer with a property block (RobotFace).
    /// View only: it reads its own drawn position and what the host passes; nothing reaches the engine, so replays are untouched.
    /// </summary>
    public sealed class PatrolBot : MonoBehaviour
    {
        public const string ModelName = "DK_PatrolBot104", Kind = "PatrolBot104";
        public const float BallR = 0.20f, HatchOpenDeg = 150f, SlideOutM = 0.14f, LeanMaxDeg = 10f;
        const float Wn = 2f * Mathf.PI * 1.5f, Zeta = 0.24f, Kv = 0.035f, Kt = 0.05f, Kn = 0.22f; // demo_motion.py: rad per m/s; rad per m/s^2 along and across
        const float DrawS = 0.9f, PanelShare = 0.55f, RecoilM = 0.018f, RecoilTau = 0.067f, FlashS = 0.07f, HurtS = 0.8f, SirenHz = 1.6f;
        public static bool Is(GameObject model) => model && model.name.StartsWith(ModelName);

        sealed class Part { public Transform t; public Vector3 pos0; public Quaternion rot0; public Vector3 x, y, z, along; }
        Part body, ball, panel, gadget, armL, armR, lamp;
        readonly List<(Renderer r, int slot, Material off)> domes = new();
        Renderer face, lobe, flash; RobotFace leds;
        Vector3 muzzleLocal; // the gadget's barrel tip, in the gadget's own space
        Vector3 last, vel, acc; bool seen; float ballYaw, roll, rolled, draw, alert, phase, sway, shotAt = -10f, hitAt = -10f; int shots = -1, health = int.MinValue;
        Vector2 tilt, tiltV; bool lit;

        public bool Ready { get; private set; }
        /// <summary>For the proof: the ball's roll so far (deg), the hatch's angle (deg), the gadget's slide (m), the siren lit, the shots flashed, the hits seen.</summary>
        public float RolledDeg => rolled; public float HatchDeg { get; private set; } public float SlideM { get; private set; } public float LeanDeg => tilt.magnitude * Mathf.Rad2Deg;
        public bool SirenLit => lit; public int Flashes { get; private set; } public int Hits { get; private set; }
        public Renderer FaceRenderer => face; public Transform Body => body?.t; public Transform Panel => panel?.t; public Transform Gadget => gadget?.t; public Transform Ball => ball?.t;
        /// <summary>Where the gadget's muzzle is now (world): the host's muzzle flash and shot line start there.</summary>
        public Vector3 Muzzle => gadget != null ? gadget.t.TransformPoint(muzzleLocal) : transform.position + Vector3.up * 0.55f;
        /// <summary>For the proof: each part's rest rotation as imported (before Setup re-parents it): Vela's risk, an axis turn added on import.</summary>
        public readonly List<(string name, Vector3 euler, Vector3 pivot)> Imported = new();

        static Material domeOn, lobeMat, flashMat; static Mesh lobeMesh, flashMesh;

        /// <summary>Finds the parts, re-parents them, adds the face sheet, the lobe and the flash. False (and the model stays as built) when a part is missing.</summary>
        public bool Setup(Material faceLed)
        {
            var all = new Dictionary<string, Transform>();
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t != transform && !all.ContainsKey(t.name)) all[t.name] = t;
            Transform P(string n) => all.TryGetValue(n.Length == 0 ? ModelName : ModelName + "_" + n, out var t) ? t : null;
            string[] names = { "", "Ball", "ChestPanel", "Gadget", "ArmL", "ArmR", "ForearmL", "ForearmR", "Beacon", "BeaconLamp", "Screen" };
            foreach (var n in names) { var t = P(n); if (!t) { Debug.LogWarning($"PatrolBot: {ModelName}_{n} missing: the robot stands as built"); return false; } Imported.Add((n.Length == 0 ? "Body" : n, t.localEulerAngles, transform.InverseTransformPoint(t.position))); }
            var b = P("");
            foreach (var n in names) if (n is not ("" or "Ball" or "ForearmL" or "ForearmR")) P(n).SetParent(b, true);
            P("ForearmL").SetParent(P("ArmL"), true); P("ForearmR").SetParent(P("ArmR"), true);
            body = Rest(b); ball = Rest(P("Ball")); panel = Rest(P("ChestPanel")); gadget = Rest(P("Gadget")); armL = Rest(P("ArmL")); armR = Rest(P("ArmR")); lamp = Rest(P("BeaconLamp"));
            foreach (var t in new[] { P("Beacon"), P("BeaconLamp") })
                if (t.GetComponent<Renderer>() is Renderer r) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) if (ms[i] && ms[i].name.StartsWith(ModelName + "_Beacon")) domes.Add((r, i, ms[i])); }
            if (domes.Count > 0) { domeOn ??= Glow(domes[0].off, "DK_PatrolBot104_Beacon_On", new Color(1f, 0.012f, 0f) * 0.9f); lobeMat ??= Glow(domes[0].off, "DK_PatrolBot104_Beacon_Lobe", new Color(1f, 0.03f, 0f) * 4f); flashMat ??= Glow(domes[0].off, "DK_PatrolBot104_Flash", new Color(1f, 0.72f, 0.32f) * 6f); }
            MuzzleOf(P("Gadget"));
            face = FaceSheet(P("Screen"), faceLed);
            if (domes.Count > 0) lobe = Lobe(P("Beacon"), domes.Find(d => d.r.transform == P("Beacon")));
            flash = Flash();
            Ready = true;
            return true;
        }

        Part Rest(Transform t)
        {
            var p = t.parent; Vector3 L(Vector3 v) => p.InverseTransformDirection(transform.TransformDirection(v)).normalized;
            return new Part { t = t, pos0 = t.localPosition, rot0 = t.localRotation, x = L(Vector3.right), y = L(Vector3.up), z = L(Vector3.forward), along = p.InverseTransformVector(transform.TransformVector(Vector3.forward)) };
        }

        /// <summary>A shared lit copy of the beacon's own material (DecoKit Lit Cutaway, emission on): the same shader and keywords, so it stays instanced.</summary>
        static Material Glow(Material from, string name, Color linear) { var m = new Material(from) { name = name }; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", linear.gamma); return m; }

        /// <summary>The barrel's tip: the front face of the gadget's bounds, in the gadget's own space (the mesh is not readable in the player; its bounds are).</summary>
        void MuzzleOf(Transform g)
        {
            var mf = g.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) { muzzleLocal = Vector3.zero; return; }
            var bd = mf.sharedMesh.bounds; float best = float.NegativeInfinity; Vector3 tip = bd.center;
            for (int i = 0; i < 8; i++)
            {
                var c = bd.center + Vector3.Scale(bd.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                float f = transform.InverseTransformPoint(g.TransformPoint(c)).z; if (f > best) best = f;
            }
            var cm = transform.InverseTransformPoint(g.TransformPoint(bd.center)); cm.z = best; tip = g.InverseTransformPoint(transform.TransformPoint(cm));
            muzzleLocal = tip;
        }

        /// <summary>The LED face: the Screen's own mesh again, 4 mm proud of the glass, in DK_Face_Amber (DK_RobotFace.shader, additive): RobotFace finds it by
        /// its `_Face` name and sets its glyphs and grid on its property block. No shadows, no probes.</summary>
        Renderer FaceSheet(Transform screen, Material led)
        {
            var mf = screen.GetComponent<MeshFilter>(); if (!mf || !led) { Debug.LogWarning("PatrolBot: no Screen mesh or no LED material: the face stays dark"); return null; }
            var go = new GameObject(ModelName + "_Screen_Face"); go.transform.SetParent(screen, false);
            go.transform.position += transform.TransformDirection(Vector3.forward) * 0.004f;
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = led; Quiet(r);
            return r;
        }

        static void Quiet(Renderer r) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off; }

        /// <summary>The siren's bright lobe: a patch of the dome's own shape (its sub-mesh bounds, a half ellipsoid on its base) 60 deg wide and 6% proud of the
        /// glass, under the spinning lamp, so it turns with it. One mesh and one material for every robot.</summary>
        Renderer Lobe(Transform beacon, (Renderer r, int slot, Material off) dome)
        {
            if (!dome.r || dome.r.GetComponent<MeshFilter>() is not MeshFilter mf || !mf.sharedMesh || dome.slot >= mf.sharedMesh.subMeshCount) return null;
            if (!lobeMesh)
            {
                var sb = mf.sharedMesh.GetSubMesh(dome.slot).bounds; Vector3 lo = new(1e9f, 1e9f, 1e9f), hi = -lo;
                for (int i = 0; i < 8; i++) { var c = transform.InverseTransformPoint(beacon.TransformPoint(sb.center + Vector3.Scale(sb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)))); lo = Vector3.Min(lo, c); hi = Vector3.Max(hi, c); }
                var lampAt = transform.InverseTransformPoint(lamp.t.position); var c0 = new Vector3(lampAt.x, lo.y, lampAt.z); var rad = new Vector3((hi.x - lo.x) * 0.5f, hi.y - lo.y, (hi.z - lo.z) * 0.5f) * 1.06f;
                const int nu = 6, nv = 6; var vs = new List<Vector3>(); var ns = new List<Vector3>(); var ts = new List<int>();
                for (int j = 0; j <= nv; j++) for (int i = 0; i <= nu; i++)
                {
                    float lon = Mathf.Lerp(-30f, 30f, i / (float)nu) * Mathf.Deg2Rad, lat = Mathf.Lerp(-5f, 78f, j / (float)nv) * Mathf.Deg2Rad;
                    var d = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
                    vs.Add(c0 + Vector3.Scale(d, rad) - lampAt); ns.Add(d);
                }
                for (int j = 0; j < nv; j++) for (int i = 0; i < nu; i++) { int a = j * (nu + 1) + i; ts.AddRange(new[] { a, a + nu + 1, a + 1, a + 1, a + nu + 1, a + nu + 2 }); }
                lobeMesh = new Mesh { name = "PatrolBot104_BeaconLobe" }; lobeMesh.SetVertices(vs); lobeMesh.SetNormals(ns); lobeMesh.SetTriangles(ts, 0); lobeMesh.RecalculateBounds();
            }
            var go = new GameObject(ModelName + "_BeaconLobe"); go.transform.SetParent(transform, false); go.transform.localPosition = transform.InverseTransformPoint(lamp.t.position);
            go.AddComponent<MeshFilter>().sharedMesh = lobeMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = lobeMat; Quiet(r); r.enabled = false;
            go.transform.SetParent(lamp.t, true);
            return r;
        }

        /// <summary>The muzzle flash: a long four-sided spike out of the barrel along the robot's front, at the gadget's tip, so it rides the slide and the recoil.</summary>
        Renderer Flash()
        {
            if (!flashMat) return null;
            if (!flashMesh)
            {
                var v = new[] { new Vector3(0, 0, 0.13f), new Vector3(0.028f, 0, 0.025f), new Vector3(0, 0.028f, 0.025f), new Vector3(-0.028f, 0, 0.025f), new Vector3(0, -0.028f, 0.025f), Vector3.zero };
                flashMesh = new Mesh { name = "PatrolBot104_Flash" }; flashMesh.vertices = v; flashMesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 }; flashMesh.RecalculateNormals(); flashMesh.RecalculateBounds();
            }
            var go = new GameObject(ModelName + "_Flash"); go.transform.SetParent(gadget.t, false); go.transform.localPosition = muzzleLocal; go.transform.rotation = transform.rotation;
            go.AddComponent<MeshFilter>().sharedMesh = flashMesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = flashMat; Quiet(r); r.enabled = false;
            return r;
        }

        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        /// <summary>One frame. `drawn`: its weapon out; `shotCount`: the shots it has fired (the engine's log, counted by the host); `hp`: its gun's health
        /// (a drop is a hit); `siren`: the beacon lit; `down`: knocked out (it lies as the old robot does: the view is laid flat, so it holds still);
        /// `alarmed`: the arms lift a little. `seconds` is the face's clock (Time.time).</summary>
        public void Drive(float dt, float seconds, bool drawn, int shotCount, int hp, bool siren, bool down, bool alarmed)
        {
            if (!Ready || dt <= 0f) return;
            var at = transform.position;
            if (shots < 0) shots = shotCount; if (health == int.MinValue) health = hp;
            bool shot = shotCount > shots; shots = shotCount; if (shot) { shotAt = seconds; Kick(0.9f, 0f); }
            if (hp < health && !down) { hitAt = seconds; Hits++; Kick(1.5f, 0.5f); } health = hp;
            if (!seen || (at - last).sqrMagnitude > 1.5f * 1.5f || down) { last = at; vel = acc = Vector3.zero; seen = true; }
            var d = at - last; d.y = 0f; last = at;
            // the ball: turned to the travel (its axle either way round, the nearer), then rolled by the distance along it over its radius
            if (d.sqrMagnitude > 1e-10f)
            {
                float want = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; if (Mathf.Abs(Mathf.DeltaAngle(ballYaw, want)) > 90f) want += 180f;
                ballYaw = Mathf.MoveTowardsAngle(ballYaw, want, 720f * dt);
                var fw = new Vector3(Mathf.Sin(ballYaw * Mathf.Deg2Rad), 0f, Mathf.Cos(ballYaw * Mathf.Deg2Rad));
                float step = Vector3.Dot(d, fw) / BallR * Mathf.Rad2Deg; roll = Mathf.Repeat(roll + step, 360f); rolled += Mathf.Abs(step);
            }
            var yawQ = Quaternion.Inverse(transform.localRotation);
            ball.t.localRotation = yawQ * Quaternion.AngleAxis(ballYaw, Vector3.up) * Quaternion.AngleAxis(roll, Vector3.right) * ball.rot0;
            // the lean: a damped spring toward speed and acceleration (the eased 10 Hz positions smoothed first, so a tick's corner is no kick)
            var v = d / dt; var vs = Vector3.Lerp(vel, v, 1f - Mathf.Exp(-dt / 0.12f)); acc = Vector3.Lerp(acc, (vs - vel) / dt, 1f - Mathf.Exp(-dt / 0.1f)); vel = vs;
            Vector2 tgt = Vector2.zero; float sp = vel.magnitude;
            if (!down && sp > 1e-3f) { var u = vel / sp; float aAlong = Vector3.Dot(acc, u); var aSide = acc - aAlong * u; var t3 = Kv * vel + Kt * aAlong * u + Kn * aSide; tgt = new Vector2(t3.x, t3.z); }
            for (int i = 0; i < 4; i++) { float h = dt / 4f; tiltV += (-Wn * Wn * (tilt - tgt) - 2f * Zeta * Wn * tiltV) * h; tilt += tiltV * h; }
            float max = LeanMaxDeg * Mathf.Deg2Rad; if (tilt.magnitude > max) tilt = tilt.normalized * max; if (down) tilt = tiltV = Vector2.zero;
            var lean = Quaternion.identity;
            if (tilt.sqrMagnitude > 1e-10f) { var dir = new Vector3(tilt.x, 0f, tilt.y); lean = Quaternion.AngleAxis(tilt.magnitude * Mathf.Rad2Deg, Vector3.Cross(Vector3.up, dir.normalized)); }
            body.t.localRotation = Quaternion.Inverse(transform.rotation) * lean * transform.rotation * body.rot0;
            // the draw: the hatch opens first, then the gadget slides out; holstered, the reverse; each shot a recoil and a two-frame flash
            draw = Mathf.MoveTowards(draw, drawn && !down ? 1f : 0f, dt / DrawS);
            HatchDeg = HatchOpenDeg * Smooth(draw / PanelShare);
            float age = seconds - shotAt, recoil = age >= 0f && age < 0.27f ? Mathf.Exp(-age / RecoilTau) : 0f;
            SlideM = SlideOutM * Smooth((draw - PanelShare) / (1f - PanelShare)) - RecoilM * recoil * Smooth((draw - PanelShare) / (1f - PanelShare));
            panel.t.localRotation = Quaternion.AngleAxis(-HatchDeg, panel.x) * panel.rot0; // the bottom edge swings forward and up about the hinge (kit x)
            gadget.t.localPosition = gadget.pos0 + gadget.along * SlideM;
            bool flashOn = flash && age >= 0f && age < FlashS && SlideM > 0.06f; if (flash && flash.enabled != flashOn) flash.enabled = flashOn; if (flashOn) Flashes++;
            // the arms: a sway (more while rolling), lifted a little when alarmed, flung up by a hit
            alert = Mathf.MoveTowards(alert, alarmed && !down ? 1f : 0f, dt / 0.45f); sway += dt;
            float hitAge = seconds - hitAt, flinch = hitAge >= 0f && hitAge < 0.6f ? Mathf.Exp(-hitAge / 0.13f) : 0f, frac = Mathf.Clamp01(sp / 1.0f);
            float swing = down ? 0f : (2.5f + 4.5f * frac) * Mathf.Sin(2f * Mathf.PI * 1.1f * sway), raise = 12f * Smooth(alert) + 22f * flinch;
            armL.t.localRotation = Quaternion.AngleAxis(swing, armL.x) * Quaternion.AngleAxis(-raise, armL.z) * armL.rot0;
            armR.t.localRotation = Quaternion.AngleAxis(-swing, armR.x) * Quaternion.AngleAxis(raise, armR.z) * armR.rot0;
            // the siren: the dome lit (a shared material swapped in), the lamp and its lobe turning
            bool on = siren && !down && domes.Count > 0;
            if (on != lit) { lit = on; foreach (var (r, slot, off) in domes) { var ms = r.sharedMaterials; ms[slot] = on ? domeOn : off; r.sharedMaterials = ms; } if (lobe) lobe.enabled = on; }
            if (on) phase = Mathf.Repeat(phase + 360f * SirenHz * dt, 360f);
            lamp.t.localRotation = Quaternion.AngleAxis(phase, lamp.y) * lamp.rot0;
            // the face: hurt just after a hit (and while down), focused while drawn; else the face's own (moods, the alarm's stare, the attention stages)
            if (!leds) leds = GetComponent<RobotFace>();
            if (leds) leds.Fight(down || hitAge >= 0f && hitAge < HurtS ? 2 : draw > 0.3f ? 1 : 0);
        }

        void Kick(float back, float left)
        {
            var b = -transform.forward; var l = -transform.right; tiltV += new Vector2(b.x, b.z) * back + new Vector2(l.x, l.z) * left;
        }
    }
}
