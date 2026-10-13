using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Evidence: per-layer counts checked against the export, and a fingerprint of the built hierarchy (paths,
    /// transforms, mesh vertices and triangles, material colours) so two builds can be compared without diffing the
    /// .unity file (Unity re-IDs objects on every build).</summary>
    public static class Inspect
    {
        /// <summary>Geometry bounds of t: the batch registry during a build, else its renderers.</summary>
        public static Bounds Bounds(Transform t)
        {
            if (Combine.Of(t) is { } registered) return registered;
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        public static SortedDictionary<string, int> Expected(WorldDto w)
        {
            var g = w.Ground;
            return new SortedDictionary<string, int>(System.StringComparer.Ordinal)
            {
                ["ground"] = g.Roads.Count + g.Circuses.Count + g.Water.Count + g.Quays.Count + g.Foot.Count + g.Medians.Count + g.Flats.Count + g.Plots.Count,
                ["transit"] = w.Transit.Tram.Count + w.Transit.SkyRail.Count + w.Transit.Stops.Count + w.Transit.Stations.Count,
                ["buildings"] = w.Buildings.Count,
                ["skins"] = w.Skins.Count,
                ["roofs"] = w.Roofs.Count,
                ["frontages"] = w.Frontages.Count,
                ["connectors"] = w.Connectors.Count,
                ["street"] = w.Street.Count,
                ["levels"] = w.Levels.Count,
                ["decks"] = w.Levels.Sum(l => l.Decks.Count),
                ["megastructures"] = w.Megastructures.Count,
                ["civic"] = w.Civic.Count,
                ["gates"] = w.Gates.Count,
                ["landmarks"] = w.Landmarks.Count,
                ["facades"] = w.Facades.Count,
                ["signatures"] = w.Signatures.Count,
                ["horizon"] = w.Horizon?.Pieces.Count ?? 0,
            };
        }

        /// <summary>Logs counts; logs an error per layer whose built objects differ from the parsed export, and per layer
        /// where the parsed export differs from the exporter's own `counts` (a reader that drops elements). Returns the mismatch count.</summary>
        public static int Verify(Transform root, WorldDto w)
        {
            int bad = 0; var sb = new StringBuilder("World: counts");
            var expected = Expected(w);
            foreach (var kv in expected)
            {
                var layer = root.Find(kv.Key);
                int have = layer ? layer.childCount : -1;
                sb.Append($" {kv.Key} {have}/{kv.Value}");
                if (have != kv.Value) { bad++; Debug.LogError($"World: layer {kv.Key} has {have} objects, export has {kv.Value}"); }
                int empty = layer ? Combine.Empty(layer) : 0;
                if (empty > 0) { bad++; Debug.LogError($"World: layer {kv.Key}: {empty} elements built without geometry"); }
                if (w.Counts != null && (!w.Counts.TryGetValue(kv.Key, out var declared) || declared != kv.Value))
                { bad++; Debug.LogError($"World: layer {kv.Key}: reader found {kv.Value}, export declares {(w.Counts.ContainsKey(kv.Key) ? w.Counts[kv.Key] : -1)}"); }
            }
            if (w.Counts != null)
                foreach (var k in w.Counts.Keys)
                    if (!expected.ContainsKey(k)) { bad++; Debug.LogError($"World: export declares layer `{k}` the builder does not build"); }
            Debug.Log(sb.ToString());
            return bad;
        }

        /// <summary>Kit vs procedural per layer the kit reaches: pieces placed against the export's kit refs and the generated
        /// stand-ins, elements built procedurally against the elements that must be (street without a ref; every frontage, whose
        /// fascia stays procedural under a kit sign; landmarks without a ref, their pads on top; roofs with parts), and elements a
        /// stand-in covers (a tower's upper mass and roof). Logs one line; returns the mismatch count.</summary>
        public static int VerifyKit(Transform root, WorldDto w, KitPlacer placer, StandIns s)
        {
            int coveredMasses = w.Buildings.Count(s.Covers), coveredRoofs = w.Roofs.Count(s.Covers);
            var want = new (string layer, int kit, int proc, int covered)[]
            {
                ("street", w.Street.Count(x => x.Kit != null), w.Street.Count(x => x.Kit == null), 0),
                ("frontages", w.Frontages.Count(f => f.SignKit != null), w.Frontages.Count, 0),
                ("landmarks", w.Landmarks.Sum(l => (l.Kit != null ? 1 : 0) + l.Kits.Count), w.Landmarks.Count(l => l.Kit == null), 0),
                ("buildings", 0, w.Buildings.Count - coveredMasses, coveredMasses),
                ("roofs", w.Roofs.Count(r => !s.Covers(r)) == 0 ? 0 : w.Roofs.Where(r => !s.Covers(r)).Sum(r => (r.Kit != null ? 1 : 0) + (r.Crown != null ? 1 : 0)), w.Roofs.Count(r => r.Parts.Count > 0 && !s.Covers(r)), coveredRoofs),
                ("facades", w.Facades.Count, 0, 0),
                ("signatures", w.Signatures.Count(x => x.Kit != null), w.Signatures.Count(x => x.Kit == null), 0),
                ("horizon", w.Horizon?.Pieces.Count ?? 0, 0, 0),
                ("civic", w.Civic.Count(m => m.Kit != null), w.Civic.Count(m => m.Kit == null), 0),
                ("megastructures", w.Megastructures.Count(m => m.Gateway != null) + s.Megas, w.Megastructures.Count(m => !s.Covers(m)), 0),
            };
            int bad = 0; var sb = new StringBuilder("World: kit/procedural/covered");
            foreach (var (layer, kit, proc, cov) in want)
            {
                var t = root.Find(layer);
                int k = placer.Placed(layer), p = Combine.Procedural(t), c = Combine.Covered(t);
                sb.Append($" {layer} {k}/{kit} {p}/{proc} {c}/{cov}");
                if (k != kit || p != proc || c != cov) { bad++; Debug.LogError($"World: layer {layer}: kit {k}/{kit}, procedural {p}/{proc}, covered {c}/{cov}"); }
            }
            Debug.Log(sb.ToString());
            return bad;
        }

        /// <summary>SHA-256 over the hierarchy walk, hashed node by node (batched meshes hold ~10^6 vertices).</summary>
        public static string Fingerprint(params Transform[] roots)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var sb = new StringBuilder();
            foreach (var r in roots) Walk(r, "", sb, hash);
            var hex = new StringBuilder();
            foreach (var b in hash.GetHashAndReset()) hex.Append(b.ToString("x2"));
            return hex.ToString();
        }

        static void Walk(Transform t, string path, StringBuilder sb, IncrementalHash hash)
        {
            path += "/" + t.name;
            sb.Clear();
            sb.Append(path).Append(' ').Append(t.localPosition.ToString("F4")).Append(t.localRotation.ToString("F4")).Append(t.localScale.ToString("F4")).Append('\n');
            if (t.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh && UnityEditor.AssetDatabase.Contains(mf.sharedMesh))
                sb.Append(UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh)).Append(':').Append(mf.sharedMesh.name).Append(' ')
                  .Append(mf.sharedMesh.vertexCount).Append('\n'); // a kit mesh: the asset (hashed by `bun run kit`), not its vertices again per instance
            else if (mf && mf.sharedMesh)
            {
                foreach (var v in mf.sharedMesh.vertices) sb.Append(v.ToString("F4"));
                foreach (var i in mf.sharedMesh.triangles) sb.Append(i).Append(',');
                sb.Append('\n');
            }
            if (t.TryGetComponent<Camera>(out var cam))
                sb.Append(new Vector3(cam.orthographic ? cam.orthographicSize : cam.fieldOfView, cam.nearClipPlane, cam.farClipPlane).ToString("F4")).Append('\n');
            if (t.TryGetComponent<MeshRenderer>(out var mr))
                foreach (var m in mr.sharedMaterials)
                    if (m) sb.Append(m.name).Append(m.HasColor("_BaseColor") ? m.GetColor("_BaseColor").ToString("F4") : "").Append('\n');
            hash.AppendData(Encoding.UTF8.GetBytes(sb.ToString()));
            foreach (Transform c in t) Walk(c, path, sb, hash);
        }
    }
}
