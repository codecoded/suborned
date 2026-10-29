#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The mid-ground city between the boulevard's frontage and the far-city band in the sky (DK_Skyline_Far):
    /// cheap streamline massing on a 70 m block grid (the boulevard on a grid road), ribbon-windowed slabs with
    /// round ends and setbacks, banded drums with
    /// overhanging caps, climbing in height with distance so the silhouette steps up into the painted band. Kept
    /// clear of the hero set (frontage, plaza, landmarks, spaceport pads) and under the sky rail and aircar lanes.
    /// One mesh per 420 m chunk (culling), submesh per material, no colliders: it is only ever seen through haze.
    internal static class DecoKitCityMass
    {
        const float Cell = 70f, Road = 16f, Extent = 1400f, Chunk = 420f, Pitch = 3.4f, Ribbon = 1.3f;
        const int BodySlots = 3, GlassSlot = 3, AccentSlot = 4;  // bodies: the frontage's cream, ivory and stone
        static readonly string[] Slots = { "DK_Enamel_Cream", "DK_Enamel_Ivory", "DK_Limestone", "DK_GlassDark", "DK_Enamel_Teal" };

        // footprints (x, z, clear radius) of what the massing must not touch (DecoKitStreetSkyline, DecoKitSkyTraffic):
        // the sphere station with the rail's ring round it, the landing pad, the background towers
        static readonly Vector3[] Keep = new[] { new Vector3(-130f, 330f, 125f), new Vector3(-260f, 1700f, 230f) }
            .Concat(DecoKitStreetSkyline.Towers.Select(t => new Vector3(t.at.x, t.at.z, 45f))).ToArray();

        /// Edit E10 (brief 075 swap): null = the old builders, or a rule given each block centre (x, z, old Unity frame) and the old ceiling,
        /// returning the ceiling to use (0 = keep clear) and whether the block must stand (no park gap): OldTownSwap keeps the massing off the
        /// plan's roads, parks, stations and the Handover zone and lets it fill the plan's blocks at domestic height.
        internal static System.Func<float, float, float, (float cap, bool always)> Rule;

        internal static void Build(Vector3[] railRoute)
        {
            var rng = new System.Random(1951);
            var chunks = new Dictionary<Vector2Int, DecoKitMassMesh>();
            int blocks = 0;
            for (float x = -Extent; x <= Extent; x += Cell)             // block centres; x = 0 is the boulevard
                for (float z = -1085f; z < Extent + 35f; z += Cell)     // the ground slab ends at +-1500
                {
                    float cap = Cap(x, z, railRoute); bool always = false;
                    if (Rule != null) (cap, always) = Rule(x, z, cap);
                    if (cap <= 0f) continue;
                    float cluster = Mathf.PerlinNoise(x * .0021f + 3.1f, z * .0021f + 7.7f);
                    if (rng.NextDouble() > .35 + .75 * cluster && !always) continue;  // parks and sparse districts between clusters
                    var key = new Vector2Int(Mathf.FloorToInt(x / Chunk), Mathf.FloorToInt(z / Chunk));
                    if (!chunks.TryGetValue(key, out var mesh)) chunks[key] = mesh = new DecoKitMassMesh(Slots.Length);
                    Block(mesh, rng, new Vector3(x, L.Walk, z), cap, cluster);
                    blocks++;
                }

            EnsureFolder(GeneratedDir);
            var root = new GameObject("Deco City Mass").transform;
            var mats = System.Array.ConvertAll(Slots, s => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/{s}.mat"));
            int tris = 0;
            foreach (var (key, mm) in chunks)
            {
                var name = $"CityMass_{key.x}_{key.y}";
                var mesh = mm.Build(name);
                var path = $"{GeneratedDir}/{name}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);
                var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterials = mats;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
                tris += mm.Triangles;
            }
            Debug.Log($"[DecoKit] city mass: {blocks} blocks, {chunks.Count} chunks, {tris} tris");
        }

        /// Height ceiling for a block at (x, z): 0 = keep clear. Low under the rail and the rooftop lane, below the
        /// high lane where it crosses; the hero set and the side street are clear.
        static float Cap(float x, float z, Vector3[] rail)
        {
            float ax = Mathf.Abs(x);
            if (x > P.StepsX - Cell * .5f) return 0f;                // the harbour and the sea
            if (P.InDistrict(x + Cell * .5f, z)) return 0f;          // the composed district (a block reaches ~29 m out)
            if (DecoKitSouthside.Contains(new Vector3(x, 0f, z), Cell * .5f)) return 0f;
            if (ax < 40f && z < 480f) return 0f;                      // the boulevard beyond it
            if (ax < 140f && z > 340f && z < 620f) return 0f;         // HQ plaza, gate towers, Skylon
            foreach (var k in Keep)
                if (new Vector2(x - k.x, z - k.y).sqrMagnitude < (k.z + Cell * .5f) * (k.z + Cell * .5f)) return 0f;
            float cap = 220f;
            if (RailDistance(new Vector2(x, z), rail) < 40f) cap = 16f;          // under the beam (bottom ~22 m)
            if (ax < 120f && z < 600f) cap = Mathf.Min(cap, 55f);                // rooftop lane at 70 m
            if (z > 80f && z < 290f) cap = Mathf.Min(cap, 110f);                 // high lane at 130 m
            return cap;
        }

        static float RailDistance(Vector2 p, Vector3[] rail)
        {
            float best = float.MaxValue;
            for (int i = 0; i < rail.Length; i++)
            {
                Vector2 a = new(rail[i].x, rail[i].z), b = new(rail[(i + 1) % rail.Length].x, rail[(i + 1) % rail.Length].z);
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        /// One city block: a slab (with 0-2 setbacks) or a drum tower, sometimes a low podium neighbour.
        static void Block(DecoKitMassMesh m, System.Random rng, Vector3 c, float cap, float cluster)
        {
            float R() => (float)rng.NextDouble();
            float d = new Vector2(c.x, c.z - 200f).magnitude;
            float h = Mathf.Lerp(14f, 48f, Mathf.Clamp01(d / 1300f)) * Mathf.Lerp(.6f, 1.5f, R()) * (.7f + .6f * cluster);
            if (R() < .08f) h *= 2.6f;  // the occasional tower that breaks the roofline
            h = Mathf.Min(h, cap);
            int body = rng.Next(BodySlots);
            float lot = (Cell - Road) * .5f;
            var jitter = new Vector3((R() - .5f) * 6f, 0f, (R() - .5f) * 6f);
            if (h > 40f && R() < .3f)
            {
                float r = Mathf.Lerp(9f, lot * .8f, R());
                m.Drum(c + jitter, r, c.y, c.y + h, 16, body);
                for (float y = c.y + 5f; y + Ribbon < c.y + h; y += Pitch) m.Drum(c + jitter, r + .06f, y, y + Ribbon, 16, GlassSlot, false);
                m.Drum(c + jitter, r * 1.35f, c.y + h, c.y + h + 1.2f, 16, R() < .5f ? AccentSlot : body);  // cap disc
                m.Drum(c + jitter, r * .35f, c.y + h + 1.2f, c.y + h + 1.2f + h * .12f, 10, body);          // lantern
                return;
            }
            // slabs: long and thin (the streamline read) or squarer; half of them with round ends
            bool slab = R() < .6f, round = R() < .5f;
            var half = slab ? new Vector2(lot * Mathf.Lerp(.75f, .95f, R()), lot * Mathf.Lerp(.3f, .45f, R()))
                            : new Vector2(lot * Mathf.Lerp(.55f, .9f, R()), lot * Mathf.Lerp(.55f, .9f, R()));
            if (R() < .5f) half = new Vector2(half.y, half.x);
            int tiers = h > 30f ? rng.Next(3) : 0;
            float y0 = c.y, top = c.y + h;
            for (int t = 0; t <= tiers; t++)
            {
                float y1 = t == tiers ? top : Mathf.Lerp(y0, top, t == 0 ? .6f : .7f);
                Slab(m, c + jitter, half, y0, y1, round && slab, body);
                y0 = y1;
                half *= Mathf.Lerp(.68f, .82f, R());
            }
            if (R() < .45f)  // low podium block sharing the lot
            {
                var off = new Vector3(half.x > half.y ? 0f : lot * .55f, 0f, half.x > half.y ? lot * .55f : 0f);
                float ph = Mathf.Min(Mathf.Lerp(7f, 14f, R()), cap);
                var ph2 = new Vector2(lot * .4f, lot * .4f);
                m.Box(c - jitter + off, ph2, c.y, c.y + ph, (body + 1) % BodySlots);
                m.Bands(c - jitter + off, ph2, c.y + 3.5f, c.y + ph - .6f, Pitch, Ribbon, GlassSlot);
            }
        }

        /// A banded box, or with round ends: a box between two half-drums on its long axis (radius = half width).
        static void Slab(DecoKitMassMesh m, Vector3 c, Vector2 half, float y0, float y1, bool round, int body)
        {
            if (!round)
            {
                m.Box(c, half, y0, y1, body);
                m.Bands(c, half, y0 + 4f, y1 - .8f, Pitch, Ribbon, GlassSlot);
                return;
            }
            bool alongX = half.x >= half.y;
            float r = Mathf.Min(half.x, half.y), reach = Mathf.Max(half.x, half.y) - r;
            var core = alongX ? new Vector2(reach, r) : new Vector2(r, reach);
            var end = alongX ? new Vector3(reach, 0f, 0f) : new Vector3(0f, 0f, reach);
            m.Box(c, core, y0, y1, body);
            m.Bands(c, core, y0 + 4f, y1 - .8f, Pitch, Ribbon, GlassSlot);
            foreach (var e in new[] { c + end, c - end })
            {
                m.Drum(e, r, y0, y1, 12, body);
                for (float y = y0 + 4f; y + Ribbon < y1 - .8f; y += Pitch) m.Drum(e, r + .06f, y, y + Ribbon, 12, GlassSlot, false);
            }
        }
    }
}
#endif
