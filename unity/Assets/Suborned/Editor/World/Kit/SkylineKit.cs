using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World
{
    /// <summary>The export's brief 007 layers, all kit pieces: `facades` (bays, ends, corners on the street faces of generated buildings;
    /// KitCover hides the procedural walls they stand on), `signatures` (SpaceHQ, the tower sites' regenerated pieces, the SkyBridge, the
    /// sky rail stations, the Parade gate block, the OceanLiner on the water) and `horizon` (the distant band, rockets, planet and moons) plus
    /// the horizon ground under `sea`: the bay sector(s) in the water material, the land beyond, the banks.
    /// Placement: a facade piece's export point is its footprint centre (the piece is modelled with its pivot on the facade plane);
    /// a SkyBridge stands its lowest point on its base; every other piece stands its pivot on (at, base). Horizon pieces cast no
    /// shadows and take the export's haze share: ring pieces a lit copy of their materials mixed toward the haze colour, sky bodies
    /// (planet, moons) Suborned/SkyBody (unlit, unfogged, the kit's map and colour with the haze mixed over them).</summary>
    public sealed class SkylineKit
    {
        readonly KitPlacer placer; readonly Look look;
        readonly Dictionary<(Material, int, bool), Material> hazed = new();

        public SkylineKit(KitPlacer placer, Look look) { this.placer = placer; this.look = look; }

        /// <summary>Brief 072 (Sky's decision 4): the sky bodies' haze share (builder kit.json `skyBodyHaze`), in place of the export's; NaN = the export's.</summary>
        public float SkyBodyHaze = float.NaN;
        public int SkyBodiesL0;

        /// <summary>The facades layer: the wave-2 street pieces as placed pieces; brief 072's every-face modules too, those of one column baked into one
        /// instance (KitColumns, Redline's plan 3.2).</summary>
        public void Facades(Transform layer, List<FacadeDto> facades)
        {
            KitColumns.Reset();
            KitColumns.Place(layer, facades, placer, placer.ClassDistances("column"));
        }

        public static float SignatureZ(KitPlacer placer, SignatureDto s) => s.Kind == "skybridge" ? placer.PivotForLowest(s.Kit, s.Base) : s.Base;

        public void Signatures(Transform layer, List<SignatureDto> signatures, Nodes nodes)
        {
            foreach (var s in signatures)
            {
                var el = Nodes.Element(layer, s.Id);
                if (s.Kit != null) { placer.Place(el, layer.name, s.Kit, s.Kit.At, SignatureZ(placer, s), 0f); continue; }
                // no adopted piece yet: its ground as a plain mass to its tip, so the skyline keeps the volume (and the gap is visible)
                if (s.Ground is not { Length: 4 } g) throw new InvalidDataException($"World: signature {s.Id} has neither a kit piece nor a ground ({s.Why})");
                Debug.LogWarning($"World: signature {s.Id} is procedural: {s.Why}");
                var ring = new[] { new Vector2(g[0], g[2]), new Vector2(g[1], g[2]), new Vector2(g[1], g[3]), new Vector2(g[0], g[3]) };
                nodes.Part(el, s.Id, new MeshKit().Prism(ring, s.Base, Mathf.Max(s.Tip, s.Base + 1f)), "landmark");
            }
        }

        /// <summary>Horizon pieces under `layer` (after Rig: the views frame the district, not the band) and the sea sectors, land and banks under `seaRoot`.</summary>
        public void Horizon(Transform layer, Transform seaRoot, HorizonDto h, GroundDto ground)
        {
            if (h == null) return;
            if (!ColorUtility.TryParseHtmlString(h.Haze, out var haze)) throw new InvalidDataException($"World: horizon haze `{h.Haze}`");
            foreach (var p in h.Pieces)
            {
                var el = Nodes.Element(layer, p.Id);
                el.position = new Vector3(p.Kit.At.x, p.Base, p.Kit.At.y);
                var t = placer.Place(el, layer.name, p.Kit, p.Kit.At, p.Base, 0f);
                if (p.TiltDeg != 0f)
                {   // lean the top TiltDeg toward the plan direction TiltYaw
                    var d = new Vector3(Mathf.Cos(p.TiltYaw), 0f, Mathf.Sin(p.TiltYaw));
                    var to = Vector3.up * Mathf.Cos(p.TiltDeg * Mathf.Deg2Rad) + d * Mathf.Sin(p.TiltDeg * Mathf.Deg2Rad);
                    t.rotation = Quaternion.FromToRotation(Vector3.up, to) * t.rotation;
                }
                bool sky = p.Ring is "planet" or "moon";
                float share = sky && !float.IsNaN(SkyBodyHaze) ? SkyBodyHaze : p.Haze;
                if (sky && t.TryGetComponent<LODGroup>(out _))
                {   // brief 072: a sky body is a few thousand triangles at ~30 px; its L1/L2 read faceted, so it always draws L0: unpacked, no LODGroup, no far levels
                    PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    Object.DestroyImmediate(t.GetComponent<LODGroup>());
                    foreach (var c in t.GetComponentsInChildren<Transform>(true).Where(c => c != t && !KitLods.IsLod0(c, t)).Select(c => c.gameObject).ToList())
                        if (c) Object.DestroyImmediate(c);
                    SkyBodiesL0++;
                }
                foreach (var r in t.GetComponentsInChildren<MeshRenderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = Hazed(mats[i], haze, share, sky);
                    r.sharedMaterials = mats;
                    if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                }
            }
            Debug.Log($"World: sky bodies drawn at L0 always {SkyBodiesL0}, haze share {(float.IsNaN(SkyBodyHaze) ? "the export's" : SkyBodyHaze.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " (builder kit.json skyBodyHaze; the export says " + string.Join("/", h.Pieces.Where(x => x.Ring is "planet" or "moon").Select(x => x.Haze.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Distinct()) + ")")}");
            Sea(seaRoot, h, ground, haze);
        }

        Material Hazed(Material src, Color haze, float share, bool sky)
        {
            var key = (src, Mathf.RoundToInt(share * 1000f), sky);
            if (hazed.TryGetValue(key, out var have)) return have;
            var c = src.HasColor("_BaseColor") ? src.GetColor("_BaseColor") : Color.white;
            var mixed = Color.Lerp(c, haze, share); mixed.a = 1f;
            Material m;
            if (sky)
            {
                m = new Material(Shader.Find("Suborned/SkyBody") ?? throw new InvalidDataException("World: shader Suborned/SkyBody not found (Assets/Suborned/Shaders)"))
                    { name = $"{src.name}/sky-haze{share:F2}" };
                // the kit's own map and colour (the old DecoKit planet bands, ring profile, moon; brief 072 decision 4), the haze mixed
                // over them in the shader so the texture survives the mix
                string map = src.HasTexture("_BaseMap") && src.GetTexture("_BaseMap") ? "_BaseMap" : src.HasTexture("_MainTex") && src.GetTexture("_MainTex") ? "_MainTex" : null;
                if (map != null)
                {
                    m.SetTexture("_BaseMap", src.GetTexture(map));
                    m.SetTextureScale("_BaseMap", src.GetTextureScale(map));
                    m.SetTextureOffset("_BaseMap", src.GetTextureOffset(map));
                }
                c.a = 1f;
                m.SetColor("_BaseColor", c);
                m.SetColor("_HazeColor", haze);
                m.SetFloat("_Haze", share);
            }
            else
            {   // the haze also lifts the shaded side (atmospheric perspective): emission of the haze at half the share
                m = new Material(src) { name = $"{src.name}/haze{share:F2}" };
                m.SetColor("_BaseColor", mixed);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                m.SetColor("_EmissionColor", haze * (share * 0.5f));
            }
            return hazed[key] = m;
        }

        /// <summary>The horizon ground under `root` (not an export layer: the export counts the pieces only): the bay sector(s) at
        /// sea.level in the water material, fading into the haze from fadeFrom to the edge of the world (size / 2); the land beyond the
        /// district (`beyond_ground`) at its level, the patches over it and the terraced hills; the banks as prisms (quay-coloured: their surface) so the step down to the water is a face.
        /// No square sea plane: the sectors and the land partition the square outside the sheet.</summary>
        void Sea(Transform root, HorizonDto h, GroundDto g, Color haze)
        {
            if (h.SeaSectors.Count == 0) throw new InvalidDataException("World: horizon has no sea sectors (the square sea plane is retired)");
            look.SeaFade(h.SeaCentre, h.SeaFadeFrom, h.SeaSize * 0.5f, haze);
            foreach (var s in h.SeaSectors) Flat(root, s.Id, new MeshKit().Flat(Poly.Clean(s.Ring), h.SeaLevel), s.Surface);
            foreach (var l in h.Land) Flat(root, l.Id, new MeshKit().Flat(Poly.Clean(l.Ring), l.Level), l.Surface);
            // patches (parks, tree clumps, promenades) lie a few cm over the land; hills are terraced prisms. One mesh per surface.
            var bySurface = new SortedDictionary<string, MeshKit>(System.StringComparer.Ordinal);
            MeshKit Of(string surface) => bySurface.TryGetValue(surface, out var k) ? k : bySurface[surface] = new MeshKit();
            foreach (var p in h.Patches) Of(p.Surface).Flat(Poly.Clean(p.Ring), p.Level);
            foreach (var hl in h.Hills) Of(hl.Surface).Prism(Poly.Clean(hl.Ring), hl.Base, hl.Base + hl.Height);
            foreach (var kv in bySurface) Flat(root, "horizon/" + kv.Key, kv.Value, kv.Key);
            var banks = new MeshKit();
            string bankSurface = null;
            foreach (var b in h.Banks)
            {
                banks.Prism(Poly.Clean(b.Ring), b.Base, b.Base + b.Height);
                if (bankSurface != null && bankSurface != b.Surface) throw new InvalidDataException($"World: horizon banks mix surfaces ({bankSurface}, {b.Surface})");
                bankSurface = b.Surface;
            }
            if (bankSurface != null) Flat(root, "horizon/banks", banks, bankSurface);
        }

        void Flat(Transform root, string id, MeshKit kit, string surface)
        {
            var go = new GameObject(id) { isStatic = true };
            go.transform.SetParent(root, false);
            var mesh = kit.Build(id); var mat = look.Mat(surface);
            if (Look.NeedsBoxUv(mat)) Look.BoxUv(mesh); // 072-F: the horizon's colour surfaces carry a detail pair
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
