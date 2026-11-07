using System.Collections.Generic;
using System.IO;
using Suborned.Play;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.Play
{
    /// <summary>Brief 098 step 1 (law L8): what the passing cars are drawn with, on the host (HostTraffic's fields; HostViews.Traffic draws them). Set by the
    /// install (PlayInstall.Dress) and again at every build (GameBuild.Build), so an installed scene picks up a change without a new install:
    ///  · the van: the kit's BoxTruck_Play once it is copied (it is a candidate today), else the old town's DK_BoxTruck (the same 2.63 x 6.56 m body);
    ///  · the scooter: Vela's DK_HoverScooter (adopted);
    ///  · the sedans' paints: Pulse's palette (out/world/harbour-district.presentation.json `paints`: each key's body and roof DK_CarPaint_* materials), else every key
    ///    of kit.json carPaints; the slots they replace are kit.json carPaints.slots (Sedan_Official's and Sedan_Getaway's body and roof);
    ///  · a paint that would pass for a mission car is kept off every passing sedan: its body colour within CIEDE2000 NearDE of Vale's car's body enamel or ours
    ///    (the two bodies share one chassis and read alike from above, so it holds for both). Today: tropical-turquoise (11.3 from Vale's teal), dusk-pink (13.6)
    ///    and tangerine (14.3 from our coral); the other eight run.
    /// Nothing missing stops a build: a missing model draws the box, missing paints the bodies as built (said in the log line).</summary>
    public static class TrafficKit
    {
        const string KitModels = "Assets/Suborned/Kit/Models/Vehicles/", KitMats = "Assets/Suborned/Kit/Materials/";
        /// <summary>How close (CIEDE2000) a passing sedan's body colour may come to Vale's car's or ours before that paint is kept off every passing sedan.</summary>
        public const float NearDE = 18f;

        /// <summary>Sets the host's traffic fields; marks the scene dirty only when one changed. Returns the log line.</summary>
        public static string Apply(HandoverHost host)
        {
            if (!host) return "traffic kit: no host";
            var repo = HarbourWorldBuilder.RepoRoot;
            var van = AssetDatabase.LoadAssetAtPath<GameObject>(KitModels + "DK_BoxTruck_Play.fbx") ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/OldTownArt/DecoKit/Prefabs/Vehicles/DK_BoxTruck.prefab");
            var scooter = AssetDatabase.LoadAssetAtPath<GameObject>(KitModels + "DK_HoverScooter.fbx");
            var kit = Json.Parse(File.ReadAllText(Path.Combine(repo, "contracts/world/kit.json")));
            var cp = J.Get(kit, "carPaints"); var sl = J.Get(cp, "slots");
            string Slot(string piece, string part) => J.S(J.Get(sl, piece), part) ?? "";
            var slots = new[] { Slot("Sedan_Official", "body"), Slot("Sedan_Official", "roof"), Slot("Sedan_Getaway", "body"), Slot("Sedan_Getaway", "roof") };
            // the palette: Pulse's (presentation.json paints, body and roof material names), else kit.json carPaints' keys
            var pres = Path.Combine(repo, "out/world/harbour-district.presentation.json"); string from;
            var pairs = new List<(string key, string body, string roof)>();
            if (File.Exists(pres) && J.Get(Json.Parse(File.ReadAllText(pres)), "paints") is Dictionary<string, object> pp)
            { from = "presentation.json paints"; foreach (var kv in pp) pairs.Add((kv.Key, J.S(J.Get(kv.Value, "materials"), "body"), J.S(J.Get(kv.Value, "materials"), "roof"))); }
            else { from = "kit.json carPaints"; if (J.Get(cp, "keys") is Dictionary<string, object> ks) foreach (var kv in ks) pairs.Add((kv.Key, J.S(kv.Value, "body"), J.S(kv.Value, "roof"))); }
            pairs.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
            var mats = new List<Material>(); var keys = new List<string>(); var miss = new List<string>(); var avoid = new List<int>(); var kept = new List<string>();
            Color Enamel(string slot) => AssetDatabase.LoadAssetAtPath<Material>(KitMats + slot + ".mat") is Material m && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.clear;
            Color offBody = Enamel(slots[0]), getBody = Enamel(slots[2]);
            foreach (var (key, body, roof) in pairs)
            {
                var b = body == null ? null : AssetDatabase.LoadAssetAtPath<Material>(KitMats + body + ".mat"); var r = roof == null ? null : AssetDatabase.LoadAssetAtPath<Material>(KitMats + roof + ".mat");
                if (!b || !r) { miss.Add(key); continue; }
                mats.Add(b); mats.Add(r); keys.Add(key);
                var bc = b.HasProperty("_BaseColor") ? b.GetColor("_BaseColor") : Color.clear; float dv = offBody.a > 0f ? DE00(bc, offBody) : 999f, dg = getBody.a > 0f ? DE00(bc, getBody) : 999f;
                int a = dv < NearDE || dg < NearDE ? 3 : 0; // 1 Sedan_Official, 2 Sedan_Getaway: both, as the bodies read alike
                avoid.Add(a); if (a != 0) kept.Add($"{key} ({(dv <= dg ? $"{dv:0.0} from Vale's" : $"{dg:0.0} from ours")})");
            }
            bool changed = host.trafficVan != van || host.trafficScooter != scooter || !Same(host.trafficPaints, mats) || !Same(host.trafficPaintKeys, keys) || !Same(host.trafficSlots, slots) || !SameInts(host.trafficPaintAvoid, avoid);
            if (changed)
            {
                host.trafficVan = van; host.trafficScooter = scooter; host.trafficPaints = mats.ToArray(); host.trafficPaintKeys = keys.ToArray(); host.trafficSlots = slots; host.trafficPaintAvoid = avoid.ToArray();
                EditorUtility.SetDirty(host); EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
            }
            return $"traffic kit{(changed ? " (set)" : "")}: van {(van ? van.name : "none: the box")}, scooter {(scooter ? scooter.name : "none: the box")}, {keys.Count} paints from {from}{(miss.Count > 0 ? $" ({miss.Count} with a material missing: {string.Join(", ", miss)})" : "")}{(kept.Count > 0 ? $"; kept off every sedan, as they would pass for a mission car (CIEDE2000 < {NearDE}): {string.Join(", ", kept)}" : "")}, slots {string.Join(" ", slots)}";
        }

        static bool SameInts(int[] a, List<int> b) { if (a == null || a.Length != b.Count) return false; for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false; return true; }
        /// <summary>CIEDE2000 between two material colours (sRGB as the material stores them; D65): the perceptual difference (about 2 is just seen side by side).</summary>
        static float DE00(Color ca, Color cb)
        {
            Vector3 p = Lab(ca), q = Lab(cb); float c1 = Mathf.Sqrt(p.y * p.y + p.z * p.z), c2 = Mathf.Sqrt(q.y * q.y + q.z * q.z), cm = (c1 + c2) / 2f;
            float g = 0.5f * (1f - Mathf.Sqrt(Mathf.Pow(cm, 7f) / (Mathf.Pow(cm, 7f) + Mathf.Pow(25f, 7f)))), a1 = (1f + g) * p.y, a2 = (1f + g) * q.y;
            float c1p = Mathf.Sqrt(a1 * a1 + p.z * p.z), c2p = Mathf.Sqrt(a2 * a2 + q.z * q.z);
            float h1 = (Mathf.Atan2(p.z, a1) * Mathf.Rad2Deg + 360f) % 360f, h2 = (Mathf.Atan2(q.z, a2) * Mathf.Rad2Deg + 360f) % 360f, dh = h2 - h1;
            if (c1p * c2p == 0f) dh = 0f; else if (dh > 180f) dh -= 360f; else if (dh < -180f) dh += 360f;
            float dL = q.x - p.x, dC = c2p - c1p, dH = 2f * Mathf.Sqrt(c1p * c2p) * Mathf.Sin(dh / 2f * Mathf.Deg2Rad), lm = (p.x + q.x) / 2f, cpm = (c1p + c2p) / 2f;
            float hm = c1p * c2p == 0f ? h1 + h2 : Mathf.Abs(h1 - h2) <= 180f ? (h1 + h2) / 2f : h1 + h2 < 360f ? (h1 + h2 + 360f) / 2f : (h1 + h2 - 360f) / 2f;
            float t = 1f - 0.17f * Mathf.Cos((hm - 30f) * Mathf.Deg2Rad) + 0.24f * Mathf.Cos(2f * hm * Mathf.Deg2Rad) + 0.32f * Mathf.Cos((3f * hm + 6f) * Mathf.Deg2Rad) - 0.20f * Mathf.Cos((4f * hm - 63f) * Mathf.Deg2Rad);
            float dth = 30f * Mathf.Exp(-Mathf.Pow((hm - 275f) / 25f, 2f)), rc = 2f * Mathf.Sqrt(Mathf.Pow(cpm, 7f) / (Mathf.Pow(cpm, 7f) + Mathf.Pow(25f, 7f)));
            float sl = 1f + 0.015f * (lm - 50f) * (lm - 50f) / Mathf.Sqrt(20f + (lm - 50f) * (lm - 50f)), sc = 1f + 0.045f * cpm, sh = 1f + 0.015f * cpm * t, rt = -Mathf.Sin(2f * dth * Mathf.Deg2Rad) * rc;
            float l = dL / sl, c = dC / sc, h = dH / sh; return Mathf.Sqrt(l * l + c * c + h * h + rt * c * h);
        }
        static Vector3 Lab(Color c)
        {
            static float Lin(float u) => u <= 0.04045f ? u / 12.92f : Mathf.Pow((u + 0.055f) / 1.055f, 2.4f);
            static float F(float t) => t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : 7.787f * t + 16f / 116f;
            float r = Lin(c.r), g = Lin(c.g), bl = Lin(c.b);
            float x = (0.4124f * r + 0.3576f * g + 0.1805f * bl) / 0.95047f, y = 0.2126f * r + 0.7152f * g + 0.0722f * bl, z = (0.0193f * r + 0.1192f * g + 0.9505f * bl) / 1.08883f;
            return new Vector3(116f * F(y) - 16f, 500f * (F(x) - F(y)), 200f * (F(y) - F(z)));
        }

        static bool Same<T>(T[] a, IList<T> b) where T : class { if (a == null || a.Length != b.Count) return false; for (int i = 0; i < a.Length; i++) if (!Equals(a[i], b[i])) return false; return true; }
    }
}
