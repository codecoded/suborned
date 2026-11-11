using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 109 round 4 (Sky: "when someone gets shot, they should flash if not already, and same for your own players when you get hit"): a quick
    /// white-hot flash on the body of anyone a shot hits (rivals, guards, police, our four, a car), on the figure itself: one more material appended to its own
    /// renderers draws the same mesh again, additive (the Shader Graph glow on white), in its own pose, flaring for a frame or two and gone in 0.18 s. Not
    /// a keyword switch on the figure's own material (the emission variant may be stripped from the build) and never a screen flash. A body already flashing
    /// is not flashed again. Skinned bodies and URP-lit meshes only (a vertex-animated crowd body would draw its flash in its bind pose). One call per engine
    /// hit (HostFight.Shot via JuiceShot).</summary>
    public sealed partial class Juice109Fx
    {
        const float HitS = 0.18f;
        sealed class Flash { public Transform who; public Renderer[] rs; public Material[][] orig; public Material m; public float t0; }
        readonly List<Flash> flashes = new(); readonly Stack<Material> flashPool = new(); Material flashSrc;
        public static int Flashes { get; private set; }

        public void HitFlash(Transform who)
        {
            if (!who) return; foreach (var f in flashes) if (f.who == who) return; // flash if not already
            if (!flashSrc) { flashSrc = Resources.Load<Material>("Juice109/Juice-Glow"); if (!flashSrc) return; }
            var list = new List<Renderer>();
            foreach (var r in who.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r.name.StartsWith("Juice109") || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (r is SkinnedMeshRenderer) { list.Add(r); continue; }
                var sm = r.sharedMaterial; if (r is MeshRenderer && sm && sm.shader && sm.shader.name.StartsWith("Universal Render Pipeline")) list.Add(r);
            }
            if (list.Count == 0) return;
            var m = flashPool.Count > 0 ? flashPool.Pop() : new Material(flashSrc) { name = "Juice109 hit flash" }; m.SetTexture("_BaseMap", Texture2D.whiteTexture); m.SetColor("_Tint", Color.black);
            var fl = new Flash { who = who, rs = list.ToArray(), orig = new Material[list.Count][], m = m, t0 = Time.unscaledTime };
            for (int i = 0; i < fl.rs.Length; i++) { var o = fl.rs[i].sharedMaterials; fl.orig[i] = o; var a = new Material[o.Length + 1]; o.CopyTo(a, 0); a[o.Length] = m; fl.rs[i].sharedMaterials = a; }
            flashes.Add(fl); Flashes++; if (Flashes <= 6) Debug.Log($"Juice109Fx: hit flash on {who.name} ({fl.rs.Length} renderers)");
        }

        /// <summary>Per frame: each flash flares (full for 0.04 s) and fades out; then the body's own materials are put back.</summary>
        void HitFrame()
        {
            float now = Time.unscaledTime;
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                var f = flashes[i]; float t = now - f.t0;
                if (t >= HitS || !f.who)
                {
                    for (int k = 0; k < f.rs.Length; k++) if (f.rs[k]) f.rs[k].sharedMaterials = f.orig[k];
                    flashPool.Push(f.m); flashes.RemoveAt(i); continue;
                }
                float k01 = t < 0.04f ? 1f : 1f - (t - 0.04f) / (HitS - 0.04f);
                f.m.SetColor("_Tint", new Color(1.6f, 1.45f, 1.25f) * (k01 * k01));
            }
        }
    }
}
