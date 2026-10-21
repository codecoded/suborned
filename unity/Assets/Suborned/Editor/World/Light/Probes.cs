using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>Baked reflection probes from light.json `probes.list` (Glint, 008 B5), so chrome and glass mirror the civic
    /// square and the Office front rather than only the default cube. Custom mode (no realtime probe, no refresh at runtime),
    /// box projection, HDR, baked once the world is built and verified, with the sun as light.json sets it, to
    /// &lt;scene&gt;-probe-&lt;name&gt;.exr beside the scene. Without a graphics device (-nographics) none is made: a probe that
    /// cannot be baked would mirror nothing.</summary>
    public static class Probes
    {
        /// <summary>A probe light.json gives no intensity (Glint, 026 chrome fix).</summary>
        public const float DefaultIntensity = 0.8f;

        /// <summary>Makes and bakes every listed probe under `lighting/probes`; returns how many.</summary>
        public static int Build(Transform lighting, Look look)
        {
            var spec = look.Light("probes");
            if (spec == null) return 0;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Debug.LogWarning("World: no graphics device; the light.json reflection probes are not made");
                return 0;
            }
            var group = Nodes.Group(lighting, "probes");
            var made = new List<(ReflectionProbe probe, string path)>();
            foreach (var e in J.A(spec, "list"))
            {
                var name = J.S(e, "name") ?? throw new InvalidDataException("World: a light.json probe has no name");
                var go = new GameObject(name);
                go.transform.SetParent(group, false);
                go.transform.localPosition = V3(e, "position");
                var p = go.AddComponent<ReflectionProbe>();
                p.mode = ReflectionProbeMode.Custom;
                p.boxProjection = true;
                p.hdr = true;
                p.center = Vector3.zero;
                p.size = V3(e, "size");
                p.resolution = (int)J.F(e, "resolution");
                p.importance = (int)J.F(e, "importance");
                p.blendDistance = J.F(e, "blendDistance");
                p.intensity = J.Get(e, "intensity") != null ? J.F(e, "intensity") : DefaultIntensity; // Glint, 026 chrome fix: 0.75 civic square, 0.85 office front, 0.8 by default
                made.Add((p, Path.ChangeExtension(HarbourWorldBuilder.ScenePath, null) + "-probe-" + name + ".exr"));
            }
            using (new Review.RenderSession()) // GPU Resident Drawer off and shaders compiled synchronously, as for every manual render
                foreach (var (p, path) in made) Bake(p, path);
            return made.Count;
        }

        static void Bake(ReflectionProbe p, string path)
        {
            if (!Lightmapping.BakeReflectionProbe(p, path)) throw new IOException($"World: could not bake reflection probe `{p.name}` to {path}");
            if (AssetImporter.GetAtPath(path) is TextureImporter ti && ti.textureShape != TextureImporterShape.TextureCube)
            {
                ti.textureShape = TextureImporterShape.TextureCube;
                ti.SaveAndReimport();
            }
            p.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path) ?? throw new IOException($"World: baked probe {path} did not import");
        }

        static Vector3 V3(object o, string key)
        {
            var a = J.A(o, key);
            if (a.Count != 3) throw new InvalidDataException($"World: light.json probe `{key}` needs 3 numbers");
            return new Vector3((float)(double)a[0], (float)(double)a[1], (float)(double)a[2]);
        }
    }
}
