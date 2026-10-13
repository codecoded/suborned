using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using static System.Net.WebUtility;

namespace Suborned.World.Review
{
    /// <summary>Props audit of the built scene against the world export: per layer and per kind, what the scene holds (element
    /// markers, kit prefab instances, batched procedural geometry) next to what the export asks for, split kit / procedural /
    /// covered, and every miss with its export id and plan coordinates. A kit piece passes when its prefab instance sits under
    /// the element's marker, from the catalogue's prefab, at the export's place, with meshes, non-zero bounds and working
    /// materials. A procedural element (and each part: pylons, roof items, megastructure parts and prims, frontage signs) passes
    /// when its layer's batched meshes have vertices inside its footprint as the export draws it. Writes
    /// &lt;repo&gt;/out/review/props-audit.html (self-contained, with a thumbnail per kind and per miss).
    /// Menu: Suborned/World/Props Audit. Batchmode (thumbnails need a GPU; with -nographics they are skipped):
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.PropsAudit.Run [-world &lt;export&gt;]</summary>
    public static class PropsAudit
    {
        const float Cell = 1f;

        public static string OutPath => Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "review", "props-audit.html");

        sealed class Miss { public Item Item; public string Reason; public bool Warn; public string Thumb; }

        sealed class Row
        {
            public string Layer, Kind; public int Export, Markers, KitWant, KitOk, ProcWant, ProcOk, Covered, Misses, Warns;
            public Item Example; public string Thumb;
        }

        [MenuItem("Suborned/World/Props Audit")]
        public static void Menu() => HarbourWorldBuilder.Batch(() =>
        {
            if (!RenderSession.EnsureScene()) return;
            var file = Write();
            EditorUtility.RevealInFinder(file);
        });

        public static void Run() => HarbourWorldBuilder.Batch(() => { if (RenderSession.EnsureScene()) Write(); });

        /// <summary>Audits the open scene; returns the report path. Logs one summary line (and an error per hard miss).</summary>
        public static string Write()
        {
            var t0 = DateTime.UtcNow;
            var index = new WorldIndex(WorldReader.Load(RenderSession.ExportPath));
            var root = GameObject.Find(HarbourWorldBuilder.RootName)?.transform ?? throw new InvalidDataException($"World: no {HarbourWorldBuilder.RootName} root in the open scene");
            var batches = GameObject.Find("batches")?.transform ?? throw new InvalidDataException("World: no batches root in the open scene");

            // ---- what the scene holds
            var grids = new Dictionary<string, Dictionary<long, Vector2>>();
            var batchStats = new SortedDictionary<string, (int renderers, long verts, int mats)>(StringComparer.Ordinal);
            foreach (Transform layer in batches)
            {
                var grid = grids[layer.name] = new Dictionary<long, Vector2>();
                int n = 0; long v = 0; var mats = new HashSet<Material>();
                foreach (var mf in layer.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = mf.sharedMesh; if (!mesh) continue;
                    n++; v += mesh.vertexCount;
                    if (mf.TryGetComponent<MeshRenderer>(out var mr)) foreach (var m in mr.sharedMaterials) mats.Add(m);
                    var m2w = mf.transform.localToWorldMatrix;
                    foreach (var p0 in mesh.vertices)
                    {
                        var p = m2w.MultiplyPoint3x4(p0);
                        long key = Key(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.z / Cell));
                        grid[key] = grid.TryGetValue(key, out var y) ? new Vector2(Mathf.Min(y.x, p.y), Mathf.Max(y.y, p.y)) : new Vector2(p.y, p.y);
                    }
                }
                batchStats[layer.name] = (n, v, mats.Count);
            }
            var markers = new Dictionary<string, Dictionary<string, Transform>>();
            foreach (Transform layer in root)
            {
                var d = markers[layer.name] = new Dictionary<string, Transform>(StringComparer.Ordinal);
                foreach (Transform c in layer) d[c.name] = c;
            }
            var prefabTotals = new SortedDictionary<string, int>(StringComparer.Ordinal);
            int missingPrefabs = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.IsPrefabAssetMissing(t.gameObject)) { missingPrefabs++; continue; }
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) continue;
                var name = Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject));
                prefabTotals[name] = prefabTotals.TryGetValue(name, out var c) ? c + 1 : 1;
            }

            // ---- each export item against the scene
            var rows = new Dictionary<string, Row>(StringComparer.Ordinal);
            var misses = new List<Miss>();
            var kitRows = new SortedDictionary<string, (int want, int ok)>(StringComparer.Ordinal);
            Row RowOf(Item it)
            {
                var key = it.Layer + "|" + it.Kind;
                if (!rows.TryGetValue(key, out var r)) rows[key] = r = new Row { Layer = it.Layer, Kind = it.Kind, Example = it };
                return r;
            }
            void Fail(Row r, Item it, string why, bool warn = false)
            {
                misses.Add(new Miss { Item = it, Reason = why, Warn = warn });
                if (warn) r.Warns++; else r.Misses++;
                if (!warn && r.Misses == 1) r.Example = it;
            }
            bool Present(Item it)
            {
                if (!grids.TryGetValue(it.Layer, out var grid)) return false;
                var b = it.Box; b.Expand(it.Tol * 2f);
                int x0 = Mathf.FloorToInt(b.min.x / Cell), x1 = Mathf.FloorToInt(b.max.x / Cell), z0 = Mathf.FloorToInt(b.min.z / Cell), z1 = Mathf.FloorToInt(b.max.z / Cell);
                for (int x = x0; x <= x1; x++)
                    for (int z = z0; z <= z1; z++)
                        if (grid.TryGetValue(Key(x, z), out var y) && y.y >= b.min.y && y.x <= b.max.y) return true;
                return false;
            }

            foreach (var it in index.Items)
            {
                var r = RowOf(it); r.Export++;
                Transform marker = markers.TryGetValue(it.Layer, out var ms) && ms.TryGetValue(it.Id, out var mk) ? mk : null;
                if (marker) r.Markers++; else Fail(r, it, "no marker in the scene");
                if (it.Source == "covered") { r.Covered++; continue; }
                if (it.Source == "none") continue;

                if (it.Kits.Count > 0)
                {
                    var instances = marker ? marker.GetComponentsInChildren<Transform>(true).Where(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) || PrefabUtility.IsPrefabAssetMissing(t.gameObject)).ToList() : new List<Transform>();
                    foreach (var want in it.Kits)
                    {
                        r.KitWant++;
                        (int want, int ok) kr = kitRows.TryGetValue(want.Id, out var have) ? have : (0, 0); kr.want++;
                        var problem = Kit(index, want, instances, it, out var warn);
                        if (problem == null) { r.KitOk++; kr.ok++; }
                        else Fail(r, it, problem, false);
                        if (warn != null) Fail(r, it, warn, true);
                        kitRows[want.Id] = kr;
                    }
                }
                if (it.Procedural)
                {
                    r.ProcWant++;
                    if (Present(it)) r.ProcOk++; else Fail(r, it, $"no batched geometry in its footprint (layer {it.Layer})");
                }
                if (marker && (it.Procedural || it.Kits.Count > 0))
                {
                    var off = Vector2.Distance(new Vector2(marker.position.x, marker.position.z), new Vector2(it.Box.center.x, it.Box.center.z));
                    float allow = new Vector2(it.Box.extents.x, it.Box.extents.z).magnitude * 1.25f + 2f;
                    if (off > allow) Fail(r, it, $"built geometry centre is {off:F1} m from the export footprint centre", true);
                }
                foreach (var s in it.Subs)
                {
                    var sr = RowOf(s); sr.Export++;
                    if (s.Source == "covered") { sr.Covered++; continue; }
                    sr.ProcWant++;
                    if (Present(s)) sr.ProcOk++; else Fail(sr, s, $"no batched geometry in its footprint (layer {s.Layer})");
                }
            }
            // scene markers the export does not have
            var unexpected = new List<string>();
            foreach (var (layer, d) in markers)
                foreach (var name in d.Keys)
                    if (!index.ById.ContainsKey(name)) unexpected.Add($"{layer}/{name}");

            // ---- thumbnails
            int thumbs = 0;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                using var session = new RenderSession();
                var cam = RenderSession.TempCamera("audit-thumb");
                try
                {
                    session.Grab(cam, 32, 32);
                    foreach (var r in rows.Values.OrderBy(r => r.Layer).ThenBy(r => r.Kind)) { r.Thumb = Thumb(session, cam, r.Example.Box); thumbs++; }
                    foreach (var m in misses.Where(m => !m.Warn).Take(60)) { m.Thumb = Thumb(session, cam, m.Item.Box); thumbs++; }
                }
                finally { UnityEngine.Object.DestroyImmediate(cam.gameObject); }
            }

            // ---- report
            int hard = misses.Count(m => !m.Warn), soft = misses.Count - hard;
            var html = Html(index, rows, misses, kitRows, prefabTotals, batchStats, markers, unexpected, missingPrefabs, (DateTime.UtcNow - t0).TotalSeconds);
            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
            File.WriteAllText(OutPath, html);
            foreach (var m in misses.Where(m => !m.Warn).Take(20)) Debug.LogWarning($"World: audit miss {m.Item.Id} ({m.Item.Kind}) at plan {Fmt(m.Item.Plan)}: {m.Reason}");
            int kitWant = rows.Values.Sum(r => r.KitWant), kitOk = rows.Values.Sum(r => r.KitOk), procWant = rows.Values.Sum(r => r.ProcWant), procOk = rows.Values.Sum(r => r.ProcOk);
            Debug.Log($"World: props audit {index.Items.Count} elements + {index.Items.Sum(i => i.Subs.Count)} parts; kit {kitOk}/{kitWant} ok, procedural {procOk}/{procWant} present, " +
                $"covered {rows.Values.Sum(r => r.Covered)}; {hard} misses, {soft} warnings; {unexpected.Count} unexpected markers, {missingPrefabs} missing prefabs; {thumbs} thumbnails → {OutPath}");
            return OutPath;
        }

        static long Key(int x, int z) => ((long)(x + 1_000_000) << 32) | (uint)(z + 1_000_000);

        /// <summary>Null when the wanted piece is under the marker, from the right prefab, intact and in place; else why not.</summary>
        static string Kit(WorldIndex index, KitWant want, List<Transform> instances, Item it, out string warn)
        {
            warn = null;
            if (!index.Placer.Prefabs.TryGetValue(want.Id, out var path)) return $"kit `{want.Id}` is not adopted in contracts/world/kit.json";
            var missing = instances.FirstOrDefault(t => PrefabUtility.IsPrefabAssetMissing(t.gameObject));
            var match = instances.Where(t => !PrefabUtility.IsPrefabAssetMissing(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == path)
                .OrderBy(t => Vector2.Distance(new Vector2(t.position.x, t.position.z), want.At)).FirstOrDefault();
            if (!match) return missing ? $"kit `{want.Id}`: prefab instance whose asset is missing" : $"kit `{want.Id}` ({want.Why}) not placed (expected {path})";
            instances.Remove(match);
            var rs = match.GetComponentsInChildren<MeshRenderer>(true);
            if (rs.Length == 0) return $"kit `{want.Id}`: no renderers";
            Bounds? b = null;
            foreach (var mr in rs)
            {
                var mesh = mr.GetComponent<MeshFilter>()?.sharedMesh;
                if (!mesh || mesh.vertexCount == 0) return $"kit `{want.Id}`: renderer {mr.name} has no mesh";
                foreach (var m in mr.sharedMaterials)
                    if (!m || !m.shader || !m.shader.isSupported || m.shader.name.StartsWith("Hidden/InternalErrorShader")) return $"kit `{want.Id}`: renderer {mr.name} has a missing or broken material";
                b = b is { } h ? Enc(h, mr.bounds) : mr.bounds;
            }
            var size = b.Value.size;
            if (size.x < 0.01f || size.y < 0.01f || size.z < 0.01f) return $"kit `{want.Id}`: zero-size bounds {size}";
            float off = Vector2.Distance(new Vector2(match.position.x, match.position.z), want.At);
            if (off > 0.05f) return $"kit `{want.Id}`: placed {off:F2} m from the export's point";
            if (!float.IsNaN(want.Z) && Mathf.Abs(match.position.y - want.Z) > 0.05f) return $"kit `{want.Id}`: stands at {match.position.y:F2} m, export says {want.Z:F2} m";
            if (it.Layer == "street" && index.World.Street.Find(s => s.Id == it.Id) is { } si && si.Size.z > 0.05f)
            {
                float ratio = size.y / si.Size.z;
                if (ratio < 0.5f || ratio > 2f) warn = $"kit `{want.Id}` is {size.y:F2} m tall, the export sizes this {si.Kind} {si.Size.z:F2} m (x{ratio:F2})";
            }
            return null;
        }

        static Bounds Enc(Bounds a, Bounds b) { a.Encapsulate(b); return a; }

        /// <summary>A 220x150 render of a box from the south-west, 35° down, as a data URI.</summary>
        static string Thumb(RenderSession session, Camera cam, Bounds box)
        {
            float r = Mathf.Max(box.extents.magnitude, 1.5f), fov = 32f;
            var rot = Quaternion.Euler(35f, 30f, 0f);
            float d = r / Mathf.Sin(fov * 0.5f * Mathf.Deg2Rad) * 1.05f;
            cam.orthographic = false; cam.fieldOfView = fov;
            cam.transform.SetPositionAndRotation(box.center - rot * Vector3.forward * d, rot);
            cam.nearClipPlane = Mathf.Max(0.1f, d - r * 1.5f); cam.farClipPlane = d + Mathf.Max(r * 3f, 600f);
            session.ShadowDistance = d + r * 2f + 50f;
            return "data:image/jpeg;base64," + Convert.ToBase64String(session.Grab(cam, 220, 150).EncodeToJPG(80));
        }

        static string Fmt(Vector3 p) => $"({p.x:F1}, {p.y:F1}, {p.z:F2})";

        static string Html(WorldIndex index, Dictionary<string, Row> rows, List<Miss> misses, SortedDictionary<string, (int want, int ok)> kitRows,
            SortedDictionary<string, int> prefabTotals, SortedDictionary<string, (int renderers, long verts, int mats)> batchStats,
            Dictionary<string, Dictionary<string, Transform>> markers, List<string> unexpected, int missingPrefabs, double seconds)
        {
            var w = index.World; var sb = new StringBuilder();
            int hard = misses.Count(m => !m.Warn);
            sb.Append("<!doctype html><meta charset=utf-8><title>Harbour District props audit</title><style>")
              .Append("body{font:13px system-ui;margin:20px;color:#1c2330;background:#f6f3ec}h1{margin:0 0 4px}h2{margin-top:28px;border-bottom:2px solid #1f6f6a}")
              .Append("table{border-collapse:collapse;margin:6px 0}td,th{border:1px solid #cfc8b8;padding:3px 7px;text-align:right;vertical-align:middle}th{background:#e7e1d3}td.l,th.l{text-align:left}")
              .Append(".bad{background:#f7d3cc}.ok{color:#1f6f6a}.warn{background:#fbeec8}img{display:block;border-radius:3px}.pill{display:inline-block;padding:2px 8px;border-radius:10px;font-weight:600}")
              .Append(".pg{background:#cde9e4}.pb{background:#f2b8ad}code{font-size:12px}</style>");
            sb.Append($"<h1>Harbour District props audit</h1><p>Scene <code>{HarbourWorldBuilder.ScenePath}</code> against <code>{HtmlEncode(RenderSession.ExportPath.Replace(HarbourWorldBuilder.RepoRoot + "/", ""))}</code> ({w.Schema}, source {HtmlEncode(w.Source)}), {DateTime.Now:yyyy-MM-dd HH:mm}, {seconds:F1}s. ")
              .Append($"<span class='pill {(hard == 0 ? "pg" : "pb")}'>{hard} misses</span> <span class=pill style='background:#fbeec8'>{misses.Count - hard} warnings</span></p>")
              .Append("<p>Kit = a DecoKit prefab instance under the element's marker, from the catalogue's prefab, at the export's point and height, with meshes, non-zero bounds and working materials. ")
              .Append("Procedural = vertices of the layer's batched meshes inside the element's footprint as the export draws it (prims by their plan half-diagonal, 0.35 m tolerance; a procedural sign by its zone above the shop header along the frontage, where the builder's sign grammar puts it); a pass means the geometry is there, not that it is right. ")
              .Append("Covered = an element a generated stand-in piece replaces by design (tower upper masses and roofs, megastructure parts). Plan coordinates are (x east, y north, base height) in metres.</p>");

            // per layer
            sb.Append("<h2>Per layer</h2><table><tr><th class=l>layer</th><th>export</th><th>markers in scene</th><th>kit ok / wanted</th><th>procedural present / wanted</th><th>parts present / wanted</th><th>covered</th><th>misses</th><th>warnings</th><th>batch renderers</th><th>batch vertices</th><th>materials</th></tr>");
            foreach (var layer in WorldIndex.LayerOrder)
            {
                var top = rows.Values.Where(r => r.Layer == layer && !IsPart(r.Kind)).ToList();
                var parts = rows.Values.Where(r => r.Layer == layer && IsPart(r.Kind)).ToList();
                int exp = top.Sum(r => r.Export), mk = markers.TryGetValue(layer, out var d) ? d.Count : 0, miss = rows.Values.Where(r => r.Layer == layer).Sum(r => r.Misses);
                var bs = batchStats.TryGetValue(layer, out var s) ? s : (0, 0L, 0);
                sb.Append($"<tr class='{(miss > 0 || mk != exp ? "bad" : "")}'><td class=l>{layer}</td><td>{exp}</td><td>{mk}</td><td>{top.Sum(r => r.KitOk)} / {top.Sum(r => r.KitWant)}</td><td>{top.Sum(r => r.ProcOk)} / {top.Sum(r => r.ProcWant)}</td>")
                  .Append($"<td>{parts.Sum(r => r.ProcOk)} / {parts.Sum(r => r.ProcWant)}</td><td>{top.Sum(r => r.Covered) + parts.Sum(r => r.Covered)}</td><td>{miss}</td><td>{rows.Values.Where(r => r.Layer == layer).Sum(r => r.Warns)}</td><td>{bs.Item1}</td><td>{bs.Item2:N0}</td><td>{bs.Item3}</td></tr>");
            }
            sb.Append("</table>");
            if (unexpected.Count > 0) sb.Append($"<p class=bad>{unexpected.Count} scene markers not in the export: {HtmlEncode(string.Join(", ", unexpected.Take(40)))}</p>");
            else sb.Append("<p class=ok>Every scene marker is an export element.</p>");
            if (missingPrefabs > 0) sb.Append($"<p class=bad>{missingPrefabs} prefab instances with a missing asset.</p>");

            // headline groups Sky asked for
            sb.Append("<h2>At a glance</h2><table><tr><th class=l>group</th><th>export</th><th>kit</th><th>procedural</th><th>ok</th></tr>");
            void G(string name, Func<Row, bool> f)
            {
                var rs = rows.Values.Where(f).ToList();
                int exp = rs.Sum(r => r.Export), ok = rs.Sum(r => r.KitOk + r.ProcOk + r.Covered);
                sb.Append($"<tr><td class=l>{name}</td><td>{exp}</td><td>{rs.Sum(r => r.KitWant)}</td><td>{rs.Sum(r => r.ProcWant)}</td><td class='{(rs.Sum(r => r.Misses) > 0 ? "bad" : "ok")}'>{(rs.Sum(r => r.Misses) == 0 ? "all present" : rs.Sum(r => r.Misses) + " misses")}</td></tr>");
            }
            G("buildings (masses)", r => r.Layer == "buildings");
            G("frontages", r => r.Kind.StartsWith("frontage:"));
            G("signs (fascia + kit)", r => r.Kind.StartsWith("sign:") || (r.Kind.StartsWith("frontage:") && r.KitWant > 0));
            G("street furniture (all)", r => r.Layer == "street");
            G("trees", r => r.Kind.StartsWith("street:tree") || r.Kind == "street:tub-palm" || r.Kind.StartsWith("civic:cafe-palm"));
            G("lamps", r => r.Kind.StartsWith("street:lamp"));
            G("roofs", r => r.Kind.StartsWith("roof:"));
            G("roof items", r => r.Kind.StartsWith("roof-item:"));
            G("tram stops / sky rail stations", r => r.Kind is "tram-stop" or "skyrail-station");
            G("sky rail pylons", r => r.Kind == "skyrail-pylon");
            G("levels / decks", r => r.Layer is "levels" or "decks");
            G("stairs / lifts / escalators / ramps", r => r.Layer == "connectors");
            G("megastructures (+ parts)", r => r.Layer == "megastructures");
            G("civic block pieces", r => r.Layer == "civic");
            G("gates", r => r.Layer == "gates");
            G("landmarks", r => r.Layer == "landmarks");
            G("kit facades (bays, ends, corners)", r => r.Layer == "facades");
            G("roof crowns", r => r.Kind.StartsWith("roof:") && r.KitWant > 0);
            G("signatures (SpaceHQ, towers, SkyBridge, stations, gate block)", r => r.Layer == "signatures");
            G("horizon band", r => r.Layer == "horizon");
            sb.Append("</table>");

            // per kind
            sb.Append("<h2>Per kind</h2><table><tr><th></th><th class=l>layer</th><th class=l>kind</th><th>export</th><th>markers</th><th>kit ok / wanted</th><th>procedural present / wanted</th><th>covered</th><th>misses</th><th>warnings</th><th class=l>example</th></tr>");
            foreach (var r in rows.Values.OrderBy(r => Array.IndexOf(WorldIndex.LayerOrder, r.Layer)).ThenBy(r => r.Kind, StringComparer.Ordinal))
            {
                sb.Append($"<tr class='{(r.Misses > 0 ? "bad" : r.Warns > 0 ? "warn" : "")}'><td>{(r.Thumb != null ? $"<img src='{r.Thumb}' width=110 height=75 loading=lazy>" : "")}</td><td class=l>{r.Layer}</td><td class=l>{HtmlEncode(r.Kind)}</td><td>{r.Export}</td><td>{(IsPart(r.Kind) ? "part" : r.Markers.ToString())}</td>")
                  .Append($"<td>{(r.KitWant > 0 ? $"{r.KitOk} / {r.KitWant}" : "")}</td><td>{(r.ProcWant > 0 ? $"{r.ProcOk} / {r.ProcWant}" : "")}</td><td>{(r.Covered > 0 ? r.Covered.ToString() : "")}</td><td>{(r.Misses > 0 ? r.Misses.ToString() : "")}</td><td>{(r.Warns > 0 ? r.Warns.ToString() : "")}</td>")
                  .Append($"<td class=l><code>{HtmlEncode(r.Example.Id)}</code> {Fmt(r.Example.Plan)}</td></tr>");
            }
            sb.Append("</table>");

            // kit pieces
            sb.Append("<h2>Kit pieces</h2><table><tr><th class=l>piece</th><th>wanted by the export</th><th>placed ok</th><th>instances in scene</th><th class=l>prefab</th></tr>");
            foreach (var id in kitRows.Keys.Union(prefabTotals.Keys.Where(k => !kitRows.ContainsKey(k) && !index.Placer.Prefabs.Values.Any(p => Path.GetFileNameWithoutExtension(p) == k))).OrderBy(k => k, StringComparer.Ordinal))
            {
                var (want, ok) = kitRows.TryGetValue(id, out var kr) ? kr : (0, 0);
                var prefab = index.Placer.Prefabs.TryGetValue(id, out var p) ? p : "";
                int inScene = prefabTotals.TryGetValue(Path.GetFileNameWithoutExtension(prefab), out var n) ? n : prefabTotals.TryGetValue(id, out var m) ? m : 0;
                sb.Append($"<tr class='{(ok < want ? "bad" : "")}'><td class=l>{HtmlEncode(id)}</td><td>{want}</td><td>{ok}</td><td>{inScene}</td><td class=l><code>{HtmlEncode(prefab)}</code></td></tr>");
            }
            sb.Append("</table>");

            // misses
            sb.Append($"<h2>Misses ({hard})</h2>");
            if (hard == 0) sb.Append("<p class=ok>None: every export element and part has its marker and its geometry or kit piece in the scene.</p>");
            else Table(sb, misses.Where(m => !m.Warn));
            sb.Append($"<h2>Warnings ({misses.Count - hard})</h2><p>Not failures: a kit piece whose height is far from the export's size for the item, or built geometry centred far from the export footprint.</p>");
            Table(sb, misses.Where(m => m.Warn).Take(400));
            return sb.ToString();
        }

        static void Table(StringBuilder sb, IEnumerable<Miss> list)
        {
            sb.Append("<table><tr><th></th><th class=l>export id</th><th class=l>layer</th><th class=l>kind</th><th class=l>plan (x, y, z)</th><th class=l>reason</th></tr>");
            foreach (var m in list)
                sb.Append($"<tr class='{(m.Warn ? "warn" : "bad")}'><td>{(m.Thumb != null ? $"<img src='{m.Thumb}' width=110 height=75>" : "")}</td><td class=l><code>{HtmlEncode(m.Item.Id)}</code></td><td class=l>{m.Item.Layer}</td><td class=l>{HtmlEncode(m.Item.Kind)}</td><td class=l>{Fmt(m.Item.Plan)}</td><td class=l>{HtmlEncode(m.Reason)}</td></tr>");
            sb.Append("</table>");
        }

        static bool IsPart(string kind) => kind.StartsWith("roof-item:") || kind.StartsWith("mega-") || kind == "skyrail-pylon" || kind.StartsWith("sign:");
    }
}
