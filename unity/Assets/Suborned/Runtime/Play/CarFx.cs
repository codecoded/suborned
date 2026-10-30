using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Play
{
    /// <summary>Brief 078 (Sky 2026-10-30: "proper unity explosions VFX to the car when it blows up"): a car's damage drawn in the world, at the
    /// car, with the built-in Particle System (Shuriken) on URP's particle shaders. One rig per car on screen: smoke from the engine bay while it
    /// is disabled; flames, thick smoke and a flickering orange point light (no shadows) while it burns; at the blast a fireball, a white core,
    /// a 0.35 s flash light, flying debris and sparks that bounce on the street, a dust ring out along the ground and a dust and smoke column
    /// that rises and drifts, a scorch mark on the ground; then a smoke plume that thins over 24 s and a smoulder that stays with the wreck.
    /// Bright and slightly stylised (a cartoon puff texture lit from above, a hot yellow fireball), never grim.
    ///
    /// Presentation only: the engine's car state (Game.CarTarget: DisabledAt, BurningAt, BlastAt, Blown) decides every window; nothing here is
    /// read back. Each part is a pure function of mission time: the rig's systems never play by themselves (playOnAwake off, auto random seed
    /// off); CarFxPool steps each one with ParticleSystem.Simulate on a fixed 1/60 s grid of the mission clock, from the tick its window opened,
    /// seeded from the car id, that tick and the part. A replay draws the same smoke at the same mission time; pausing the clock stops it, the
    /// play speed (=/−) speeds it, the fight lab's slow plays it slow.</summary>
    public sealed class CarFxRig : MonoBehaviour
    {
        public enum Part { Bonnet, Flames, FireSmoke, Fireball, Core, Debris, Sparks, Column, Ring, Plume, Smoulder }
        public const int PartCount = 11;
        public ParticleSystem[] parts = new ParticleSystem[PartCount];
        public Light fireLight, flashLight;
        public MeshRenderer scorch;
        public Transform ground;

        /// <summary>The materials one rig uses (CarFxArt makes them; the Editor saves them as assets so their shader variants ship).</summary>
        public sealed class Materials { public Material smoke, fire, glow, fireball, debris, scorch; }

        static readonly Vector3 Wind = new(0.42f, 0f, 0.18f); // m/s, one fixed drift for every plume (look only)

        /// <summary>Builds the rig (inactive parts, no particles) under `parent`. The Editor saves the result as the CarFx prefab.</summary>
        public static CarFxRig Create(Materials m, Transform parent = null)
        {
            var go = new GameObject("car-fx"); if (parent) go.transform.SetParent(parent, false);
            var rig = go.AddComponent<CarFxRig>();
            var g = new GameObject("ground"); g.transform.SetParent(go.transform, false); rig.ground = g.transform; // the collision plane: the street under the car (+y up)

            // disabled: grey smoke from the engine bay (the nose is the model's +z)
            var p = Sys(go.transform, "bonnet", m.smoke, new Vector3(0f, 1.0f, 1.45f), 30, 4f, true);
            Life(p, 2.4f, 3.2f, 0.7f, 1.1f, 0.55f, 0.85f, true);
            Shape(p, ParticleSystemShapeType.Cone, 0.3f, 14f, new Vector3(-90f, 0f, 0f));
            var em = p.emission; em.rateOverTime = 7f;
            Grow(p, 0.6f, 2.6f); Fade(p, new Color(0.9f, 0.9f, 0.88f), new Color(0.7f, 0.7f, 0.7f), 0.55f, 0.12f); Drift(p, 0.25f, 0.25f); Spin(p, 25f);
            rig.parts[(int)Part.Bonnet] = p;

            // burning: flames over the bonnet and cabin (pointing up: a few degrees of rotation), additive
            ParticleSystem.MainModule main;
            p = Sys(go.transform, "flames", m.fire, new Vector3(0f, 0.95f, 0.25f), 48, 5f, true);
            Life(p, 0.45f, 0.75f, 0f, 0f, 0.65f, 1.05f, false);
            main = p.main; main.startRotation = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            Shape(p, ParticleSystemShapeType.Box, 0f, 0f, Vector3.zero, new Vector3(1.3f, 0.2f, 2.8f));
            em = p.emission; em.rateOverTime = 34f;
            var vol = p.velocityOverLifetime; vol.enabled = true; vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.25f); vol.y = new ParticleSystem.MinMaxCurve(1.7f, 2.6f); vol.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.2f);
            Curve(p, new Keyframe(0f, 0.5f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.15f));
            Colours(p, new[] { (0f, new Color(1f, 1f, 0.85f)), (0.08f, new Color(1f, 0.85f, 0.38f)), (0.45f, new Color(1f, 0.48f, 0.14f)), (1f, new Color(0.8f, 0.2f, 0.08f)) },
                       new[] { (0f, 0f), (0.08f, 1f), (0.45f, 0.9f), (1f, 0f) });
            Sort(p, -2f); rig.parts[(int)Part.Flames] = p;

            // burning: thick warm-charcoal smoke above the flames
            p = Sys(go.transform, "fire-smoke", m.smoke, new Vector3(0f, 1.7f, 0.2f), 44, 5f, true);
            Life(p, 3.2f, 4.2f, 0f, 0f, 1.1f, 1.6f, true);
            Shape(p, ParticleSystemShapeType.Box, 0f, 0f, Vector3.zero, new Vector3(1.2f, 0.3f, 2.4f));
            em = p.emission; em.rateOverTime = 9f;
            vol = p.velocityOverLifetime; vol.enabled = true; vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(Wind.x * 0.6f, Wind.x * 1.4f); vol.y = new ParticleSystem.MinMaxCurve(1.5f, 2.2f); vol.z = new ParticleSystem.MinMaxCurve(Wind.z * 0.6f, Wind.z * 1.4f);
            Grow(p, 0.7f, 3.0f); Colours(p, new[] { (0f, new Color(0.3f, 0.26f, 0.25f)), (0.5f, new Color(0.44f, 0.42f, 0.42f)), (1f, new Color(0.6f, 0.6f, 0.61f)) },
                                     new[] { (0f, 0f), (0.1f, 0.68f), (0.5f, 0.5f), (1f, 0f) });
            Spin(p, 20f); rig.parts[(int)Part.FireSmoke] = p;

            // the blast: a hot fireball (puffs, alpha-blended, HDR tint so it blooms under the grade)
            p = Sys(go.transform, "fireball", m.fireball, new Vector3(0f, 1.0f, 0f), 20, 2f, false);
            Life(p, 0.55f, 0.95f, 2.5f, 6f, 1.5f, 2.4f, true);
            Shape(p, ParticleSystemShapeType.Sphere, 0.9f, 0f, Vector3.zero);
            Burst(p, (0f, 16));
            Drag(p, 0.18f); main = p.main; main.gravityModifier = -0.15f;
            Curve(p, new Keyframe(0f, 0.5f), new Keyframe(0.4f, 1.25f), new Keyframe(1f, 1.45f));
            Colours(p, new[] { (0f, new Color(1f, 1f, 0.9f)), (0.15f, new Color(1f, 0.86f, 0.36f)), (0.45f, new Color(1f, 0.46f, 0.12f)), (0.75f, new Color(0.36f, 0.22f, 0.17f)), (1f, new Color(0.32f, 0.3f, 0.29f)) },
                       new[] { (0f, 1f), (0.6f, 0.95f), (0.8f, 0.7f), (1f, 0f) });
            Spin(p, 40f); Sort(p, -3f, ParticleSystemSortMode.YoungestInFront); rig.parts[(int)Part.Fireball] = p;

            // the blast's white core (additive glow), a few frames
            p = Sys(go.transform, "core", m.glow, new Vector3(0f, 1.2f, 0f), 4, 1f, false);
            Life(p, 0.22f, 0.3f, 0f, 0f, 4.5f, 5.5f, false);
            Shape(p, ParticleSystemShapeType.Sphere, 0.1f, 0f, Vector3.zero); Burst(p, (0f, 3));
            Curve(p, new Keyframe(0f, 0.6f), new Keyframe(1f, 1.2f));
            Colours(p, new[] { (0f, Color.white), (1f, new Color(1f, 0.8f, 0.4f)) }, new[] { (0f, 1f), (1f, 0f) });
            Sort(p, -4f); rig.parts[(int)Part.Core] = p;

            // flying debris: lit chrome and charred bits, bouncing on the street
            p = Sys(go.transform, "debris", m.debris, new Vector3(0f, 0.9f, 0f), 16, 3f, false);
            Life(p, 2.0f, 2.6f, 6f, 11f, 0.14f, 0.32f, false);
            main = p.main; main.startRotation3D = true; main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f); main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f); main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 1f; main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.22f, 0.21f, 0.22f), new Color(0.86f, 0.84f, 0.8f));
            Shape(p, ParticleSystemShapeType.Cone, 0.8f, 50f, new Vector3(-90f, 0f, 0f)); Burst(p, (0f, 14));
            var rot = p.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true; rot.x = new ParticleSystem.MinMaxCurve(-9f, 9f); rot.y = new ParticleSystem.MinMaxCurve(-9f, 9f); rot.z = new ParticleSystem.MinMaxCurve(-9f, 9f);
            Curve(p, new Keyframe(0f, 1f), new Keyframe(0.85f, 1f), new Keyframe(1f, 0f));
            Bounce(p, rig.ground, 0.4f, 0.35f);
            var r = p.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx"); r.alignment = ParticleSystemRenderSpace.World;
            r.shadowCastingMode = ShadowCastingMode.Off; rig.parts[(int)Part.Debris] = p;

            // sparks: stretched additive streaks, bouncing
            p = Sys(go.transform, "sparks", m.glow, new Vector3(0f, 1.0f, 0f), 60, 2f, false);
            Life(p, 0.35f, 0.9f, 7f, 15f, 0.07f, 0.13f, false);
            Shape(p, ParticleSystemShapeType.Hemisphere, 0.5f, 0f, new Vector3(-90f, 0f, 0f)); Burst(p, (0f, 36), (0.12f, 12));
            main = p.main; main.gravityModifier = 0.9f;
            Colours(p, new[] { (0f, new Color(1f, 0.96f, 0.72f)), (0.5f, new Color(1f, 0.62f, 0.2f)), (1f, new Color(1f, 0.35f, 0.1f)) }, new[] { (0f, 1f), (0.7f, 1f), (1f, 0f) });
            Bounce(p, rig.ground, 0.3f, 0.4f);
            r = p.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 1.5f;
            Sort(p, -3f); rig.parts[(int)Part.Sparks] = p;

            // the dust and smoke column: warm tan dust that greys as it rises and drifts
            p = Sys(go.transform, "column", m.smoke, new Vector3(0f, 0.6f, 0f), 18, 6f, false);
            Life(p, 3.5f, 5.0f, 1.2f, 3.2f, 1.8f, 2.8f, true);
            Shape(p, ParticleSystemShapeType.Hemisphere, 1.2f, 0f, new Vector3(-90f, 0f, 0f)); Burst(p, (0.04f, 10), (0.25f, 6));
            Drag(p, 0.1f); main = p.main; main.gravityModifier = -0.12f;
            vol = p.velocityOverLifetime; vol.enabled = true; vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(Wind.x * 0.8f, Wind.x * 1.4f); vol.y = new ParticleSystem.MinMaxCurve(0.4f, 0.8f); vol.z = new ParticleSystem.MinMaxCurve(Wind.z * 0.8f, Wind.z * 1.4f);
            Grow(p, 0.5f, 2.4f);
            Colours(p, new[] { (0f, new Color(0.8f, 0.66f, 0.52f)), (0.4f, new Color(0.55f, 0.51f, 0.48f)), (1f, new Color(0.64f, 0.63f, 0.62f)) }, new[] { (0f, 0f), (0.06f, 0.8f), (0.4f, 0.6f), (1f, 0f) });
            Spin(p, 15f); rig.parts[(int)Part.Column] = p;

            // the dust ring out along the street (it carries the blast's reach; CarFxPool scales its speed to the rulebook's blast radius)
            p = Sys(go.transform, "ring", m.smoke, new Vector3(0f, 0.3f, 0f), 20, 2f, false);
            Life(p, 0.9f, 1.3f, 7f, 9f, 1.0f, 1.5f, true);
            Shape(p, ParticleSystemShapeType.Circle, 0.8f, 0f, new Vector3(90f, 0f, 0f)); Burst(p, (0f, 18));
            Drag(p, 0.35f); Grow(p, 1f, 2.2f);
            Colours(p, new[] { (0f, new Color(0.85f, 0.74f, 0.6f)), (1f, new Color(0.75f, 0.72f, 0.68f)) }, new[] { (0f, 0.7f), (1f, 0f) });
            rig.parts[(int)Part.Ring] = p;

            // after the blast: a plume that thins over 24 s
            p = Sys(go.transform, "plume", m.smoke, new Vector3(0f, 1.2f, 0f), 50, 24f, false);
            Life(p, 4.0f, 5.5f, 0f, 0f, 1.2f, 1.7f, true);
            Shape(p, ParticleSystemShapeType.Box, 0f, 0f, Vector3.zero, new Vector3(1.2f, 0.3f, 2.4f));
            em = p.emission; em.rateOverTime = new ParticleSystem.MinMaxCurve(8f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.3f, 0.6f), new Keyframe(1f, 0.15f)));
            vol = p.velocityOverLifetime; vol.enabled = true; vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(Wind.x * 0.7f, Wind.x * 1.5f); vol.y = new ParticleSystem.MinMaxCurve(1.4f, 2.0f); vol.z = new ParticleSystem.MinMaxCurve(Wind.z * 0.7f, Wind.z * 1.5f);
            Grow(p, 0.7f, 3.2f);
            Colours(p, new[] { (0f, new Color(0.24f, 0.23f, 0.23f)), (0.5f, new Color(0.42f, 0.41f, 0.41f)), (1f, new Color(0.6f, 0.6f, 0.61f)) }, new[] { (0f, 0f), (0.1f, 0.62f), (0.5f, 0.45f), (1f, 0f) });
            Spin(p, 18f); rig.parts[(int)Part.Plume] = p;

            // the wreck smoulders while it stands
            p = Sys(go.transform, "smoulder", m.smoke, new Vector3(0f, 0.9f, 0.4f), 14, 5f, true);
            Life(p, 3.5f, 4.5f, 0f, 0f, 0.6f, 0.9f, true);
            Shape(p, ParticleSystemShapeType.Box, 0f, 0f, Vector3.zero, new Vector3(0.9f, 0.2f, 1.8f));
            em = p.emission; em.rateOverTime = 2.2f;
            vol = p.velocityOverLifetime; vol.enabled = true; vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(Wind.x * 0.6f, Wind.x * 1.2f); vol.y = new ParticleSystem.MinMaxCurve(0.7f, 1.0f); vol.z = new ParticleSystem.MinMaxCurve(Wind.z * 0.6f, Wind.z * 1.2f);
            Grow(p, 0.7f, 2.6f); Fade(p, new Color(0.62f, 0.61f, 0.6f), new Color(0.7f, 0.7f, 0.7f), 0.42f, 0.15f); Spin(p, 15f);
            rig.parts[(int)Part.Smoulder] = p;

            rig.fireLight = Lamp(go.transform, "fire-light", new Vector3(0f, 1.6f, 0.2f), new Color(1f, 0.58f, 0.24f), 8f);
            rig.flashLight = Lamp(go.transform, "flash-light", new Vector3(0f, 1.8f, 0f), new Color(1f, 0.86f, 0.62f), 18f);

            // the scorch on the street: a flat quad just above the ground (no decal pass on PC_Renderer); alpha set per frame by the pool
            var sq = new GameObject("scorch"); sq.transform.SetParent(go.transform, false); sq.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            sq.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); sq.transform.localScale = new Vector3(6.2f, 7.4f, 1f);
            sq.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var mr = sq.AddComponent<MeshRenderer>(); mr.sharedMaterial = m.scorch; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; mr.enabled = false;
            rig.scorch = mr;
            return rig;
        }

        // ------------------------------------------------------------------ module helpers (authoring only)

        static ParticleSystem Sys(Transform parent, string name, Material mat, Vector3 at, int max, float duration, bool loop)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = at;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false; ps.randomSeed = 1;
            var main = ps.main; main.playOnAwake = false; main.loop = loop; main.duration = duration; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.scalingMode = ParticleSystemScalingMode.Hierarchy; main.useUnscaledTime = false;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate; // the pool steps it by hand; never Unity's own auto culling
            main.stopAction = ParticleSystemStopAction.None;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            r.renderMode = ParticleSystemRenderMode.Billboard; r.maxParticleSize = 3f; r.minParticleSize = 0f; r.sortMode = ParticleSystemSortMode.None;
            r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off; r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            return ps;
        }

        static void Life(ParticleSystem p, float l0, float l1, float v0, float v1, float s0, float s1, bool spin)
        {
            var main = p.main; main.startLifetime = new ParticleSystem.MinMaxCurve(l0, l1); main.startSpeed = new ParticleSystem.MinMaxCurve(v0, v1);
            main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startRotation = spin ? new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f) : new ParticleSystem.MinMaxCurve(0f);
            main.startColor = Color.white;
        }

        static void Shape(ParticleSystem p, ParticleSystemShapeType type, float radius, float angle, Vector3 rotation, Vector3? scale = null)
        {
            var sh = p.shape; sh.enabled = true; sh.shapeType = type; sh.radius = Mathf.Max(0.0001f, radius); sh.angle = angle; sh.rotation = rotation;
            if (scale.HasValue) sh.scale = scale.Value; sh.radiusThickness = 1f;
        }

        static void Burst(ParticleSystem p, params (float t, int n)[] bursts)
        {
            var em = p.emission; em.rateOverTime = 0f; var b = new ParticleSystem.Burst[bursts.Length];
            for (int i = 0; i < bursts.Length; i++) b[i] = new ParticleSystem.Burst(bursts[i].t, (short)bursts[i].n);
            em.SetBursts(b);
        }

        static void Grow(ParticleSystem p, float from, float to) => Curve(p, new Keyframe(0f, from), new Keyframe(1f, to));
        static void Curve(ParticleSystem p, params Keyframe[] keys)
        {
            var sz = p.sizeOverLifetime; sz.enabled = true; float hi = 0f; foreach (var k in keys) hi = Mathf.Max(hi, k.value);
            for (int i = 0; i < keys.Length; i++) keys[i].value /= hi;
            sz.size = new ParticleSystem.MinMaxCurve(hi, new AnimationCurve(keys));
        }

        static void Colours(ParticleSystem p, (float t, Color c)[] colours, (float t, float a)[] alphas)
        {
            var g = new Gradient(); var ck = new GradientColorKey[colours.Length]; var ak = new GradientAlphaKey[alphas.Length];
            for (int i = 0; i < ck.Length; i++) ck[i] = new GradientColorKey(colours[i].c, colours[i].t);
            for (int i = 0; i < ak.Length; i++) ak[i] = new GradientAlphaKey(alphas[i].a, alphas[i].t);
            g.SetKeys(ck, ak); var col = p.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(g);
        }

        static void Fade(ParticleSystem p, Color c0, Color c1, float peak, float rise) => Colours(p, new[] { (0f, c0), (1f, c1) }, new[] { (0f, 0f), (rise, peak), (1f, 0f) });
        static void Drift(ParticleSystem p, float wx, float wz)
        {
            var vol = p.velocityOverLifetime; vol.enabled = true; vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(Wind.x * wx * 2f, Wind.x * wx * 3f); vol.y = new ParticleSystem.MinMaxCurve(0f, 0.1f); vol.z = new ParticleSystem.MinMaxCurve(Wind.z * wz * 2f, Wind.z * wz * 3f);
        }
        static void Spin(ParticleSystem p, float degS) { var rot = p.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-degS * Mathf.Deg2Rad, degS * Mathf.Deg2Rad); }
        static void Drag(ParticleSystem p, float dampen) { var lv = p.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 0.6f; lv.dampen = dampen; }
        static void Sort(ParticleSystem p, float fudge, ParticleSystemSortMode mode = ParticleSystemSortMode.None) { var r = p.GetComponent<ParticleSystemRenderer>(); r.sortingFudge = fudge; r.sortMode = mode; }

        static void Bounce(ParticleSystem p, Transform plane, float dampen, float bounce)
        {
            var c = p.collision; c.enabled = true; c.type = ParticleSystemCollisionType.Planes; c.SetPlane(0, plane);
            c.dampen = dampen; c.bounce = bounce; c.lifetimeLoss = 0f; c.radiusScale = 0.5f; c.sendCollisionMessages = false; // planes only: deterministic, no physics scene
        }

        static Light Lamp(Transform parent, string name, Vector3 at, Color c, float range)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = at;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = 0f; l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel; l.cullingMask = ~(1 << SignalsFeature.Layer); // never on the signal layer (unlit anyway)
            l.enabled = false; return l;
        }
    }

    /// <summary>Brief 078: the effect's textures, made in code (no paid or generated art): a cartoon smoke puff lit from above, a flame tongue, a
    /// soft glow, a scorch with burst streaks. Deterministic (integer hash noise). And the URP particle materials over them.</summary>
    public static class CarFxArt
    {
        public const string UnlitShader = "Universal Render Pipeline/Particles/Unlit", LitShader = "Universal Render Pipeline/Particles/Simple Lit";

        static float Hash(int x, int y) { unchecked { uint h = (uint)(x * 374761393 + y * 668265263); h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) & 0xffffff) / 16777215f; } }
        static float Noise1(float t, int seed) { int i = Mathf.FloorToInt(t); float f = t - i; f = f * f * (3f - 2f * f); return Mathf.Lerp(Hash(i, seed), Hash(i + 1, seed), f); }

        static Texture2D Tex(string name, int n, System.Func<float, float, Color> px)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, anisoLevel = 1 };
            var c = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) c[y * n + x] = px((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f);
            t.SetPixels(c); t.Apply(true); return t;
        }

        /// <summary>A puff: a lobed round silhouette, shaded two-tone from the upper left (a cel look that reads at 40 px).</summary>
        public static Texture2D Puff() => Tex("fx-puff", 128, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
            float rr = 0.8f + 0.07f * Mathf.Sin(5f * a + 1.3f) + 0.05f * Mathf.Sin(8f * a + 0.4f) + 0.03f * Mathf.Sin(13f * a + 2.1f);
            float alpha = Mathf.Clamp01(Mathf.InverseLerp(rr, rr - 0.28f, d)); alpha *= alpha * (3f - 2f * alpha);
            float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Min(1f, d / rr) * Mathf.Min(1f, d / rr)));
            float lit = (-0.45f * x / rr + 0.6f * y / rr + 0.66f * nz); float shade = Mathf.Lerp(0.7f, 1f, Mathf.SmoothStep(0.05f, 0.45f, lit));
            shade *= 0.94f + 0.06f * Hash(Mathf.FloorToInt((x + 1f) * 9f), Mathf.FloorToInt((y + 1f) * 9f)); // a little grain
            return new Color(shade, shade, shade, alpha);
        });

        /// <summary>A flame tongue: narrow at the top, round at the base, white (the particle colour tints it).</summary>
        public static Texture2D Flame() => Tex("fx-flame", 64, (x, y) =>
        {   // a soft rounded tongue: a round base, a short taper, a smooth falloff (a hard edge read as paper triangles at play zoom)
            float v = (y + 1f) * 0.5f, w = 0.34f + 0.16f * Mathf.Clamp01(1f - v * 1.25f);
            float dy = v < 0.4f ? (v - 0.4f) / 0.38f : (v - 0.4f) / 0.58f, d = Mathf.Sqrt(x * x / (w * w * 4f) + dy * dy);
            float alpha = Mathf.Pow(Mathf.Clamp01(1f - d), 1.4f);
            return new Color(1f, 1f, 1f, alpha);
        });

        /// <summary>A soft glow (the core, the sparks).</summary>
        public static Texture2D Glow() => Tex("fx-glow", 64, (x, y) => { float d = Mathf.Clamp01(Mathf.Sqrt(x * x + y * y)); float a = Mathf.Pow(1f - d, 2.2f); return new Color(1f, 1f, 1f, a); });

        /// <summary>A scorch: dark at the middle, ragged burst streaks out to the edge.</summary>
        public static Texture2D Scorch() => Tex("fx-scorch", 128, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y), a = (Mathf.Atan2(y, x) + Mathf.PI) / (2f * Mathf.PI);
            float spokes = Noise1(a * 23f, 7) * 0.65f + Noise1(a * 61f, 11) * 0.35f, reach = 0.45f + 0.5f * spokes;
            float alpha = Mathf.Clamp01(Mathf.InverseLerp(reach, reach * 0.35f, d)) * (0.85f + 0.15f * Hash(Mathf.FloorToInt((x + 1f) * 31f), Mathf.FloorToInt((y + 1f) * 31f)));
            return new Color(0.09f, 0.075f, 0.065f, alpha * 0.92f);
        });

        public sealed class Textures { public Texture2D puff, flame, glow, scorch; }
        public static Textures MakeTextures() => new() { puff = Puff(), flame = Flame(), glow = Glow(), scorch = Scorch() };

        /// <summary>The rig's materials over `tex`. Shaders by name: the Editor saves these as assets (the variants ship with the build); a host
        /// without the prefab makes them at run time (the Editor, or a build that carries the shaders).</summary>
        public static CarFxRig.Materials MakeMaterials(Textures tex)
        {
            var unlit = Shader.Find(UnlitShader); var lit = Shader.Find(LitShader);
            if (!unlit || !lit) return null;
            return new CarFxRig.Materials
            {
                smoke = Particle(unlit, tex.puff, false, new Color(1f, 1f, 1f, 1f), "fx-smoke"),
                fireball = Particle(unlit, tex.puff, false, new Color(1.7f, 1.55f, 1.4f, 1f), "fx-fireball"),
                fire = Particle(unlit, tex.flame, true, new Color(2.2f, 1.7f, 1.3f, 1f), "fx-fire"),
                glow = Particle(unlit, tex.glow, true, new Color(2.4f, 2.1f, 1.7f, 1f), "fx-glow"),
                scorch = Particle(unlit, tex.scorch, false, new Color(1f, 1f, 1f, 1f), "fx-scorch"),
                debris = Debris(lit),
            };
        }

        public static Material Particle(Shader sh, Texture2D tex, bool additive, Color tint, string name)
        {
            var m = new Material(sh) { name = name, enableInstancing = false };
            m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", tint);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f); m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false); m.SetShaderPassEnabled("DepthNormalsOnly", false); m.SetShaderPassEnabled("ShadowCaster", false);
            return m;
        }

        static Material Debris(Shader lit)
        {
            var m = new Material(lit) { name = "fx-debris" }; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 0f); m.SetFloat("_Smoothness", 0.55f);
            m.SetColor("_SpecColor", new Color(0.6f, 0.6f, 0.6f)); m.EnableKeyword("_SPECULAR_COLOR"); return m;
        }
    }

    /// <summary>Brief 078: a car as the effect reads it, made by the host from the engine each frame (seconds on the mission clock; -1 = not yet).</summary>
    public struct CarFxState
    {
        public string Id; public Vector3 At; public Quaternion Facing;
        public float DisabledS, BurningS, BlastS; public bool Blown; public float BlastM;
    }

    /// <summary>Brief 078: the pooled rigs (MaxLive, made once, never per event) and the rule that steps them from the mission clock. A car keeps
    /// its rig while it is among the MaxLive most urgent (a fresh blast, burning, disabled, a wreck; the nearer the camera the sooner); a car with
    /// no rig is left to the HUD's flat shapes. Nothing here reaches the engine.</summary>
    public sealed class CarFxPool
    {
        public const int MaxLive = 4;
        /// <summary>The grid every part is stepped on (s of mission time).</summary>
        public const float Step = 1f / 60f;
        /// <summary>Grid steps a frame may take one by one; more (a long seek, 8x at a low frame rate) and the part is re-simulated from its start in one call.</summary>
        public static int MaxStepsPerFrame = 16;
        /// <summary>For captures: the mission time to draw (s), instead of the host's; below 0 off.</summary>
        public static float TimeOverride = -1f;

        static readonly float[] LifeMax = { 3.2f, 0.75f, 4.2f, 0.95f, 0.3f, 2.6f, 0.9f, 5.0f, 1.3f, 5.5f, 4.5f };
        static readonly bool[] Looping = { true, true, true, false, false, false, false, false, false, false, true };

        sealed class Slot
        {
            public CarFxRig rig; public string car = ""; public bool used;
            public readonly float[] simT = new float[CarFxRig.PartCount]; public readonly float[] startS = new float[CarFxRig.PartCount];
            public float ringFor = -1f; public float scorchA = -1f; public float flashI, fireI;
            public void Idle() { for (int i = 0; i < simT.Length; i++) { simT[i] = -1f; startS[i] = float.NaN; } }
        }

        readonly Slot[] slots = new Slot[MaxLive];
        readonly Transform root;
        readonly MaterialPropertyBlock mpb = new();
        public int LiveRigs { get; private set; }
        public int LiveParticles { get; private set; }
        public int LiveLights { get; private set; }
        public double LastMs { get; private set; }
        /// <summary>The most a frame took (ms) since ResetPeak: for the capture's cost read.</summary>
        public double PeakMs { get; private set; }
        public int PeakSteps { get; private set; }
        public void ResetPeak() { PeakMs = 0; PeakSteps = 0; }
        public int StepsLastFrame { get; private set; }
        public int SeeksLastFrame { get; private set; }

        public CarFxPool(Transform parent, GameObject prefab)
        {
            root = new GameObject("car-fx-pool").transform; root.SetParent(parent, false);
            CarFxRig.Materials made = null;
            for (int i = 0; i < MaxLive; i++)
            {
                CarFxRig rig = null;
                if (prefab) { var go = Object.Instantiate(prefab, root, false); go.name = "car-fx-" + i; rig = go.GetComponent<CarFxRig>(); }
                if (!rig) { made ??= CarFxArt.MakeMaterials(CarFxArt.MakeTextures()); if (made == null) { Debug.LogWarning("CarFx: URP particle shaders not found; the HUD shapes stand in"); break; } rig = CarFxRig.Create(made, root); rig.name = "car-fx-" + i; }
                slots[i] = new Slot { rig = rig }; slots[i].Idle(); Hide(slots[i]);
            }
        }

        public bool Ready => slots[0] != null;

        /// <summary>Whether the car's damage is drawn by a rig this frame (the HUD then skips its flat smoke and flame).</summary>
        public bool Drawn(string car) { foreach (var s in slots) if (s != null && s.used && s.car == car) return true; return false; }

        public void ReleaseAll() { foreach (var s in slots) if (s != null) Release(s); }

        void Release(Slot s) { s.used = false; s.car = ""; foreach (var p in s.rig.parts) if (p) p.Clear(false); s.Idle(); Hide(s); }

        static void Hide(Slot s)
        {
            if (s.rig.fireLight) s.rig.fireLight.enabled = false; if (s.rig.flashLight) s.rig.flashLight.enabled = false; if (s.rig.scorch) s.rig.scorch.enabled = false;
            s.flashI = s.fireI = 0f; s.scorchA = -1f;
        }

        static readonly List<(int pri, float d, int i)> order = new();
        static readonly HashSet<string> keep = new();

        /// <summary>One frame: `now` is the mission time drawn (s), `cars` the damaged cars, `focus` the camera's focus (tiebreak).</summary>
        public void Frame(float now, List<CarFxState> cars, Vector3 focus)
        {
            if (!Ready) return;
            var sw = System.Diagnostics.Stopwatch.StartNew(); int steps = 0, seeks = 0;
            // who gets a rig: a fresh blast first, then burning, then disabled, then wrecks; nearer the focus first
            order.Clear();
            for (int i = 0; i < cars.Count; i++)
            {
                var c = cars[i]; int pri = c.Blown ? (now - c.BlastS < 8f ? 0 : 3) : c.BurningS >= 0f && now >= c.BurningS ? 1 : c.DisabledS >= 0f && now >= c.DisabledS ? 2 : 9;
                if (pri < 9) order.Add((pri, (c.At - focus).sqrMagnitude, i));
            }
            order.Sort((a, b) => a.pri != b.pri ? a.pri.CompareTo(b.pri) : a.d != b.d ? a.d.CompareTo(b.d) : string.CompareOrdinal(cars[a.i].Id, cars[b.i].Id));
            keep.Clear(); for (int k = 0; k < order.Count && k < MaxLive; k++) keep.Add(cars[order[k].i].Id);
            foreach (var s in slots) if (s != null && s.used && !keep.Contains(s.car)) Release(s);
            LiveRigs = 0; LiveParticles = 0; LiveLights = 0;
            for (int k = 0; k < order.Count && k < MaxLive; k++)
            {
                var c = cars[order[k].i]; Slot s = null;
                foreach (var x in slots) if (x != null && x.used && x.car == c.Id) { s = x; break; }
                if (s == null) foreach (var x in slots) if (x != null && !x.used) { s = x; s.used = true; s.car = c.Id; s.Idle(); break; }
                if (s == null) continue;
                Drive(s, c, now, ref steps, ref seeks); LiveRigs++;
                foreach (var p in s.rig.parts) if (p) LiveParticles += p.particleCount;
                if (s.rig.fireLight && s.rig.fireLight.enabled) LiveLights++; if (s.rig.flashLight && s.rig.flashLight.enabled) LiveLights++;
            }
            StepsLastFrame = steps; SeeksLastFrame = seeks; LastMs = sw.Elapsed.TotalMilliseconds; if (LastMs > PeakMs) PeakMs = LastMs; if (steps > PeakSteps) PeakSteps = steps;
        }

        /// <summary>A part's window on the mission clock: emission from start to stop, drawn until end (s); start NaN = not this car's.</summary>
        static void Window(CarFxRig.Part part, in CarFxState c, out float start, out float stop, out float end)
        {
            const float Inf = float.PositiveInfinity; start = float.NaN; stop = Inf; end = Inf;
            float blastStop = c.Blown ? c.BlastS : Inf;
            switch (part)
            {
                case CarFxRig.Part.Bonnet: if (c.DisabledS >= 0f) { start = c.DisabledS; stop = c.BurningS >= 0f ? c.BurningS + 0.8f : blastStop; } break;
                case CarFxRig.Part.Flames: if (c.BurningS >= 0f) { start = c.BurningS; stop = c.Blown ? c.BlastS + 6f : Inf; } break;
                case CarFxRig.Part.FireSmoke: if (c.BurningS >= 0f) { start = c.BurningS; stop = blastStop; } break;
                case CarFxRig.Part.Plume: if (c.Blown) { start = c.BlastS; stop = c.BlastS + 24f; } break;
                case CarFxRig.Part.Smoulder: if (c.Blown) start = c.BlastS + 16f; break;
                default: if (c.Blown) { start = c.BlastS; stop = c.BlastS + 0.5f; } break; // the blast's bursts
            }
            if (!float.IsNaN(start) && !float.IsInfinity(stop)) end = stop + LifeMax[(int)part] + 0.25f;
        }

        void Drive(Slot s, in CarFxState c, float now, ref int steps, ref int seeks)
        {
            var rig = s.rig; rig.transform.SetPositionAndRotation(c.At, c.Facing);
            if (s.ringFor != c.BlastM && c.BlastM > 0f && rig.parts[(int)CarFxRig.Part.Ring] is { } ring)
            {   // the dust ring runs out to about the rulebook's blast radius (look only)
                var m = ring.main; m.startSpeed = new ParticleSystem.MinMaxCurve(c.BlastM * 1.5f, c.BlastM * 1.8f); s.ringFor = c.BlastM;
            }
            for (int i = 0; i < CarFxRig.PartCount; i++)
            {
                var p = rig.parts[i]; if (!p) continue;
                Window((CarFxRig.Part)i, c, out float start, out float stop, out float end);
                if (float.IsNaN(start) || now < start || now > end) { if (s.simT[i] >= 0f) { p.Clear(false); s.simT[i] = -1f; } continue; }
                if (s.startS[i] != start) { s.simT[i] = -1f; s.startS[i] = start; } // a new window (a respawned car): a new seed
                float target = Mathf.Floor((now - start) / Step + 0.01f) * Step /* 1 % of a step: above float error on a 12 min mission clock */, local = stop - start;
                if (s.simT[i] >= 0f && target < s.simT[i] - 1e-4f) s.simT[i] = -1f; // time went back (a restart): draw it again from the start
                if (s.simT[i] < 0f)
                {   // the window opens: re-seeded, at its start; stepped from there below (a far start is a seek)
                    p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); p.randomSeed = Seed(c.Id, start, i);
                    var em0 = p.emission; em0.enabled = true; p.Simulate(0f, false, true, false); s.simT[i] = 0f;
                }
                if ((target - s.simT[i]) / Step > MaxStepsPerFrame + 0.5f)
                {   // seek: re-seeded and simulated from the window's start (a looping part: its last few lifetimes are its steady state)
                    p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    p.randomSeed = Seed(c.Id, start, i);
                    float t = Looping[i] && target > LifeMax[i] * 2f && target < local ? LifeMax[i] * 2f : target;
                    var em = p.emission; em.enabled = true; Rate(p, i, c, start + Mathf.Min(t, local));
                    p.Simulate(Mathf.Min(t, local), false, true, false); // emitting up to the window's stop (a burst part's bursts land)
                    if (t > local) { em.enabled = false; p.Simulate(t - local, false, false, false); } // then only the particles age
                    s.simT[i] = target; seeks++;
                    continue;
                }
                int n = Mathf.RoundToInt((target - s.simT[i]) / Step);
                for (int k = 0; k < n; k++)
                {
                    float t0 = s.simT[i] + k * Step; var em = p.emission; bool on = t0 < local - 1e-4f; if (em.enabled != on) em.enabled = on;
                    Rate(p, i, c, start + t0); p.Simulate(Step, false, false, false); steps++;
                }
                s.simT[i] = target;
            }
            Lights(s, c, now);
        }

        /// <summary>The flames die down over the 6 s after the blast (a function of mission time).</summary>
        static void Rate(ParticleSystem p, int part, in CarFxState c, float t)
        {
            if (part != (int)CarFxRig.Part.Flames) return;
            float k = c.Blown && t >= c.BlastS ? Mathf.Clamp01(1f - (t - c.BlastS) / 6f) * 0.8f : 1f; var em = p.emission; if (Mathf.Abs(em.rateOverTimeMultiplier - 34f * k) > 0.01f) em.rateOverTimeMultiplier = 34f * k;
        }

        void Lights(Slot s, in CarFxState c, float now)
        {
            var rig = s.rig; float seed = (Seed(c.Id, c.BurningS, 99) & 0xffff) / 655.36f;
            // the fire's light: in over 1 s, a flicker from two noise rates on the mission clock, out over the 6 s after the blast
            float fire = 0f;
            if (c.BurningS >= 0f && now >= c.BurningS && (!c.Blown || now < c.BlastS + 6f))
            {
                float inK = Mathf.Clamp01((now - c.BurningS) / 1f), outK = c.Blown && now >= c.BlastS ? 1.4f * (1f - (now - c.BlastS) / 6f) : 1f;
                float flick = 0.72f + 0.38f * Mathf.PerlinNoise(seed, now * 7f) + 0.14f * Mathf.PerlinNoise(seed + 17f, now * 19f);
                fire = 3.2f * inK * Mathf.Max(0f, outK) * flick;
            }
            // the flash: up in 0.03 s, gone by 0.35 s
            float flash = 0f;
            if (c.Blown && now >= c.BlastS && now < c.BlastS + 0.35f) { float a = now - c.BlastS; flash = a < 0.03f ? 42f * a / 0.03f : 42f * Mathf.Pow(1f - (a - 0.03f) / 0.32f, 2f); }
            if (rig.fireLight) { bool on = fire > 0.01f; if (rig.fireLight.enabled != on) rig.fireLight.enabled = on; if (on && s.fireI != fire) rig.fireLight.intensity = s.fireI = fire; }
            if (rig.flashLight) { bool on = flash > 0.01f; if (rig.flashLight.enabled != on) rig.flashLight.enabled = on; if (on && s.flashI != flash) rig.flashLight.intensity = s.flashI = flash; }
            // the scorch: in over 0.3 s from the blast
            float sa = c.Blown && now >= c.BlastS ? Mathf.Clamp01((now - c.BlastS) / 0.3f) * 0.88f : 0f;
            if (rig.scorch && Mathf.Abs(sa - s.scorchA) > 0.004f)
            {
                s.scorchA = sa; rig.scorch.enabled = sa > 0.004f;
                if (rig.scorch.enabled) { rig.scorch.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", new Color(1f, 1f, 1f, sa)); rig.scorch.SetPropertyBlock(mpb); }
            }
        }

        /// <summary>The paint's darkening for a car at `now` (1 = as built): disabled a touch, charring while it burns, black-brown once it blows.</summary>
        public static float Tint(in CarFxState c, float now)
        {
            if (c.Blown && now >= c.BlastS) return Mathf.Lerp(0.4f, 0.16f, Mathf.Clamp01((now - c.BlastS) / 0.3f));
            if (c.BurningS >= 0f && now >= c.BurningS) { float burn = c.BlastS > c.BurningS ? c.BlastS - c.BurningS : 6f; return Mathf.Lerp(0.85f, 0.42f, Mathf.Clamp01((now - c.BurningS) / burn)); }
            if (c.DisabledS >= 0f && now >= c.DisabledS) return 0.9f;
            return 1f;
        }

        /// <summary>A part's seed: FNV-1a over the car id, the window's start (ms) and the part.</summary>
        public static uint Seed(string id, float startS, int part)
        {
            unchecked
            {
                uint h = 2166136261u; foreach (char ch in id ?? "") { h ^= ch; h *= 16777619u; }
                int ms = Mathf.RoundToInt(startS * 1000f); h ^= (uint)ms; h *= 16777619u; h ^= (uint)part; h *= 16777619u;
                return h == 0 ? 1u : h;
            }
        }

        /// <summary>For checks: every live particle's position in slot order (the replay comparison).</summary>
        public List<Vector3> Snapshot()
        {
            var l = new List<Vector3>(); var buf = new ParticleSystem.Particle[64];
            foreach (var s in slots)
            {
                if (s == null || !s.used) continue;
                foreach (var p in s.rig.parts) { if (!p) continue; if (buf.Length < p.main.maxParticles) buf = new ParticleSystem.Particle[p.main.maxParticles]; int n = p.GetParticles(buf); for (int i = 0; i < n; i++) l.Add(buf[i].position); }
            }
            return l;
        }

        /// <summary>For checks: each used slot's car, rig position and per-part particle counts.</summary>
        public string Describe()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var s in slots) { if (s == null || !s.used) continue; sb.Append(s.car).Append(" at ").Append(s.rig.transform.position.ToString("F4")).Append(" yaw ").Append(s.rig.transform.eulerAngles.y.ToString("F3")).Append(" ["); foreach (var p in s.rig.parts) sb.Append(p ? p.particleCount : -1).Append(' '); sb.Append("] "); }
            return sb.ToString();
        }

        /// <summary>For the capture's cost read: each live particle's world position and current size (m).</summary>
        public void Sizes(List<(Vector3 at, float size, bool additive)> into)
        {
            into.Clear(); var buf = new ParticleSystem.Particle[64];
            foreach (var s in slots)
            {
                if (s == null || !s.used) continue;
                for (int k = 0; k < s.rig.parts.Length; k++)
                {
                    var p = s.rig.parts[k]; if (!p || k == (int)CarFxRig.Part.Debris) continue; if (buf.Length < p.main.maxParticles) buf = new ParticleSystem.Particle[p.main.maxParticles];
                    int n = p.GetParticles(buf); bool add = k is (int)CarFxRig.Part.Flames or (int)CarFxRig.Part.Core or (int)CarFxRig.Part.Sparks;
                    for (int i = 0; i < n; i++) into.Add((buf[i].position, buf[i].GetCurrentSize(p), add));
                }
            }
        }
    }
}
