#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.OldTown.Sky;
using Suborned.World;
using UnityEngine;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap: our network in the old look (keep/drop rows "Sky track beam and pylons", "Tram", "Tram rails"):
    /// - beams: every beam of our sky track (transit.skyTrack.beams, our route, levels and clearances) swept by the old DecoKitSkyRail beam
    ///   builder (its private Beam, called by reflection on an open SkyPath through our line at its top);
    /// - pylons: at our pylon points (the plan's 30 m spacing, the old spacing), the old pieces: SkyRail_Pylon_Tall foot, the stretched
    ///   SkyRail_Pylon_Shaft, the old single SkyRail_Pylon_Head, and our SkyRail_Pylon_HeadTwin where the line is double (the old had none);
    /// - tram: the old Tile_TramRail (2 m) along every rail piece of ours (our 12 m straights as six old tiles, our 2 m curve chords as one),
    ///   the Boulevard's double track too (edit E12: our bed is what reads on the route; the old single track at x -2.5 and any old road
    ///   module or paint on our bed give way, ClearBed); the old Tram at our stops and parked tram; the old TramCanopy at our stop shelters
    ///   (our TramCanopy_Play only in the zone).
    /// Everything it places goes under one root the cut leaves alone, and adds the holes the old gives way to.</summary>
    internal sealed class SwapNetwork
    {
        readonly OldTownSwap.Report rep; readonly WorldDto w; readonly DecoKitPlacer placer; readonly OurKit kit;
        public readonly Transform Root;
        /// <summary>Old frame holes: beam corridors, pylon plinths, tram corridors.</summary>
        public readonly SwapShapes Holes = new();
        /// <summary>Our tram bed, old frame: each laid tile's footprint (2 m along by 2 m across, the old tile's), for ClearBed and the audit.</summary>
        public readonly SwapShapes Bed = new();
        internal const string RailsRoot = "Tram rails (old Tile_TramRail on our route)";
        /// <summary>Our tiles' top over the deck (E12): 5 mm, over the old road modules (E11, 3 mm), the old paint (4 mm) and our crossings' road
        /// plates (3 mm, kept from our pipeline), so the rails run through a crossing as they do in ours, and nothing lies level with the bed.</summary>
        internal const float BedLift = .005f;
        readonly List<(Vector2 at, Vector2 dir)> rails = new();
        const float BeamH = 1.6f;

        public SwapNetwork(OldTownSwap.Report rep, WorldDto w, DecoKitPlacer placer, OurKit kit)
        {
            this.rep = rep; this.w = w; this.placer = placer; this.kit = kit;
            Root = new GameObject("Network (our routes, old pieces)").transform;
        }

        GameObject Put(string piece, Vector3 at, float yaw, Transform parent)
        {
            var go = placer.Put(piece, at, yaw);
            if (go == null) throw new System.InvalidOperationException($"[Swap] old piece {piece} missing (NP-1)");
            go.transform.SetParent(parent, true);
            return go;
        }

        public void Beams()
        {
            var beam = typeof(DecoKitSkyRail).GetMethod("Beam", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new System.MissingMethodException("DecoKitSkyRail.Beam");
            var root = new GameObject("Sky track (old beam on our route)").transform; root.SetParent(Root, false);
            float m = 0f;
            foreach (var b in w.Transit.SkyTrack.Beams)
            {
                var go = new GameObject(b.Id); go.transform.SetParent(root, false);
                var path = go.AddComponent<SkyPath>();
                path.Set(b.Line.Select(v => OldTownSwap.Old3(v, b.Top)).ToArray(), false);
                beam.Invoke(null, new object[] { path, go.transform, "Swap_" + b.Id.Replace('/', '_').Replace('-', '_') });
                m += path.Length;
                Holes.Ribbon("beam", b.Id, b.Line.Select(OldTownSwap.Old).ToList(), 2.2f, b.Top - BeamH - 2.6f, b.Top + .6f);
            }
            rep.Log($"sky track: {w.Transit.SkyTrack.Beams.Count} beams, {m:0} m, swept by the old DecoKitSkyRail.Beam (ivory body, teal stripe) on our route at top {string.Join("/", w.Transit.SkyTrack.Beams.Select(b => b.Top).Distinct())} m");
        }

        public void Pylons()
        {
            var root = new GameObject("Sky track pylons (old pieces at our points)").transform; root.SetParent(Root, false);
            int tall = 0, bare = 0, twin = 0, single = 0;
            foreach (var p in w.Transit.SkyTrack.Pylons)
            {
                float yaw = -p.Yaw * Mathf.Rad2Deg;
                float ground = p.Foot != null ? p.FootZ : p.ShaftZ;
                if (Mathf.Abs(ground - .18f) < .02f) ground = L.Walk;  // our pavement top (0.18) is the old walk (0.15)
                var g = new GameObject(p.Id).transform; g.SetParent(root, false);
                float shaft0 = ground;
                if (p.Foot != null) { Put("SkyRail_Pylon_Tall", OldTownSwap.Old3(p.At, ground), yaw, g); shaft0 = ground + 12f; tall++; } else bare++;
                float headBottom = p.HeadZ - 2.2f;
                var sh = Put("SkyRail_Pylon_Shaft", OldTownSwap.Old3(p.At, shaft0), yaw, g);
                sh.transform.localScale = new Vector3(1f, Mathf.Max(.1f, headBottom - shaft0), 1f);
                if (p.Head != null && p.Head.Id == "SkyRail_Pylon_HeadTwin")
                {
                    var h = kit.Put("SkyRail_Pylon_HeadTwin", OldTownSwap.Old3(p.At, p.HeadZ), Quaternion.Euler(0f, yaw, 0f), g);
                    if (h == null) throw new System.InvalidOperationException("[Swap] our SkyRail_Pylon_HeadTwin missing (NP-1)");
                    twin++;
                }
                else { Put("SkyRail_Pylon_Head", OldTownSwap.Old3(p.At, p.HeadZ), yaw, g); single++; }
                Holes.Circle("pylon", p.Id, OldTownSwap.Old(p.At), 3f, -2f, p.HeadZ + 1f);
            }
            rep.Log($"pylons: {w.Transit.SkyTrack.Pylons.Count} at our points: {tall} on the old SkyRail_Pylon_Tall, {bare} bare shafts (export: the plan's point stands in a walk zone), heads {twin} our HeadTwin, {single} old single");
        }

        public void Rails()
        {
            var root = new GameObject(RailsRoot).transform; root.SetParent(Root, false);
            var repo = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..",".."));
            var geometry = OldTownSwap.JoinedTram ? SwapTramSource.Load(repo,"out/world/harbour-district.world.json") : null;
            var footprints = geometry?.ToDictionary(f=>f.Source.Id);
            if (geometry == null) { SwapTramRender.Applied.Clear(); SwapTramRender.SurfaceFilters.Clear(); SwapTramRender.Owners.Clear(); SwapTramRender.Geometry = null; }
            int pieces = 0, tiles = 0;
            foreach (var g in w.Ground.Pieces.Where(x => x.Kind == "tram-rail"))
            {
                var d = new Vector2(Mathf.Cos(g.Yaw), Mathf.Sin(g.Yaw));
                float len = g.Size.x; int n = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
                float yaw = -g.Yaw * Mathf.Rad2Deg, step = len / n;
                var nb = new Vector2(-d.y, d.x);
                for (int i = 0; i < n; i++)
                {
                    var c = g.At + d * (-len * .5f + step * (i + .5f));
                    Put("Tile_TramRail", OldTownSwap.Old3(c, BedLift), yaw, root); tiles++;
                    Vector2 a = OldTownSwap.Old(c - d * step * .5f), b = OldTownSwap.Old(c + d * step * .5f);
                    Bed.Add("bed", $"{g.Id}#{i}", footprints != null ? SwapTramRender.Polygon(footprints[$"{g.Id}#{i}"]) : new[] { a + nb, b + nb, b - nb, a - nb }, -.5f, .5f);
                }
                pieces++;
                rails.Add((g.At, d));
                var a0 = OldTownSwap.Old(g.At - d * (len * .5f + .5f)); var b0 = OldTownSwap.Old(g.At + d * (len * .5f + .5f)); var nn = nb * 2.1f;
                Holes.Add("tram", g.Id, new[] { a0 + nn, b0 + nn, b0 - nn, a0 - nn }, -.5f, 4.5f);
            }
            if (geometry != null) SwapTramRender.Apply(root,geometry,rep);
            rep.Log($"tram rails: all {pieces} of our rail pieces laid as {tiles} old Tile_TramRail tiles {BedLift * 1000f:0} mm over the deck, the Boulevard's double track included (E12: the old single track gives way to our bed)");
        }

        /// <summary>Edit E12: our tram bed is what reads on the route. Where they clash, the old gives way, by shape, never by id:
        /// - old road-level modules (top at the deck after E11, body sunk in the road: the old Tile_TramRail, manholes, drains) whose footprint
        ///   overlaps a tile of ours (5 cm in from its edge);
        /// - old road paint (generated flat meshes under 1 cm thick, their top 0.5-12 mm over the deck: DecoKitRoadMarkings' dashes and anything
        ///   painted like them), by connected part, where the part overlaps our bed.
        /// The flat pieces never meet the tram corridor in SwapCut (flat old ground stays), so this is the only place they give way to it.</summary>
        public void ClearBed(SwapPlan plan)
        {
            int modules = 0, paint = 0;
            bool Ours(Transform t) { for (var x = t; x; x = x.parent) if (x.name.StartsWith("Ours (") || x.name.StartsWith("Network (")) return true; return false; }
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (!t || !UnityEditor.PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) || Ours(t)) continue;
                var rs = t.GetComponentsInChildren<MeshRenderer>(true);
                if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.max.y < -.001f || b.max.y > BedLift + .001f || b.min.y > -.02f) continue;
                var box = SwapMesh.XZ(b); box = Rect.MinMaxRect(box.xMin + .05f, box.yMin + .05f, box.xMax - .05f, box.yMax - .05f);
                var hit = Bed.Hit(box, b.min.y, b.max.y);
                if (hit == null) continue;
                rep.Gave("tram bed (our route, E12)", OldTownSwap.PieceOf(t.name), b.center, hit.Id);
                Object.DestroyImmediate(t.gameObject); modules++;
            }
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).ToArray())
            {
                if (!mf || !mf.sharedMesh || Ours(mf.transform) || UnityEditor.PrefabUtility.IsPartOfPrefabInstance(mf.gameObject)) continue;
                var mr = mf.GetComponent<MeshRenderer>(); if (!mr || !mr.enabled) continue;
                var b0 = mr.bounds;
                if (b0.size.y > .01f || b0.max.y < .0005f || b0.max.y > .012f || !Bed.Near(SwapMesh.XZ(b0)).Any()) continue;
                if (!mf.sharedMesh.isReadable) { rep.Log($"tram bed: paint mesh {mf.name} not readable; left whole"); continue; }
                var rd = new SwapMesh.Read(mf.sharedMesh, mf.transform.localToWorldMatrix);
                var ids = rd.Components(out int count);
                var cb = new Bounds[count]; var has = new bool[count];
                for (int s = 0; s < rd.Subs.Count; s++)
                    for (int k = 0; k < ids[s].Length; k++)
                        for (int j = 0; j < 3; j++) { var p = rd.Verts[rd.Subs[s][3 * k + j]].W; int c = ids[s][k]; if (!has[c]) { cb[c] = new Bounds(p, Vector3.zero); has[c] = true; } else cb[c].Encapsulate(p); }
                var drop = new bool[count]; int dropped = 0;
                for (int c = 0; c < count; c++)
                {
                    var hit = has[c] ? Bed.Hit(SwapMesh.XZ(cb[c]), cb[c].min.y - .01f, cb[c].max.y + .01f) : null;
                    if (hit == null) continue;
                    drop[c] = true; dropped++; rep.Gave("tram bed (our route, E12)", "paint:" + mf.name, cb[c].center, hit.Id);
                }
                if (dropped == 0) continue;
                var wr = new SwapMesh.Write(rd);
                for (int s = 0; s < rd.Subs.Count; s++)
                    for (int k = 0; k < ids[s].Length; k++)
                        if (!drop[ids[s][k]]) wr.Keep(s, rd.Subs[s][3 * k], rd.Subs[s][3 * k + 1], rd.Subs[s][3 * k + 2]);
                paint += dropped;
                if (wr.Triangles == 0) { Object.DestroyImmediate(mf.gameObject); continue; }
                mf.sharedMesh = wr.Mesh(mf.sharedMesh.name + " (tram bed)");
            }
            rep.Log($"tram bed (E12): {modules} old road-level modules and {paint} parts of old road paint on our bed gave way");
            CrossGround(plan, Ours);
        }

        /// <summary>Ground surface ownership at the actual laid tile edges. Only the demonstrated gutter-setts and granite
        /// rail/kerb material classes in the road-to-walk band give way; crossing lights and the asphalt below stay.
        /// Render meshes only: no collider, navigation or source mesh is modified.</summary>
        internal static void ClearImportedGround(OldTownSwap.Report report)
        {
            var bed = new SwapShapes();
            foreach (var root in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Where(t => t.name == RailsRoot))
                foreach (Transform t in root)
                {
                    if (!SwapTramRender.Applied.TryGetValue(t,out var f)) throw new System.InvalidOperationException("tram correction requires source geometry mapping first");
                    bed.Add("tile", f.Source.Id, SwapTramRender.Polygon(f));
                }
            int changed = 0, clipped = 0;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude))
            {
                bool ours = false;
                for (var t = mf.transform; t; t = t.parent) if (t.name.StartsWith("Ours (")) { ours = true; break; }
                // Exact generated-ground owner, not every Network/granite object.
                bool crossGround = false;
                for(var t=mf.transform;t;t=t.parent) if(t.name=="Tram street kerbs (old Kerb_Granite on E12's cut edges)") crossGround=true;
                if ((!ours && !crossGround) || !mf.sharedMesh) continue;
                var mr = mf.GetComponent<MeshRenderer>(); if (!mr || !mr.enabled) continue;
                var mats = mr.sharedMaterials;
                bool Ground(int sub) => sub < mats.Length && mats[sub] &&
                    (mats[sub].name == "DK_Gutter_Setts" || mats[sub].name == "DK_Granite_Kerb" || crossGround && mats[sub].name == "DK_Granite_Kerb_B");
                if (!Enumerable.Range(0, mats.Length).Any(Ground)) continue;
                var rd = SwapTramMeshRead.Read(mf.sharedMesh, mf.transform.localToWorldMatrix);
                var wr = new SwapMesh.Write(rd,true); int cuts = 0;
                for (int sub = 0; sub < rd.Subs.Count; sub++)
                    for (int j = 0; j < rd.Subs[sub].Length; j += 3)
                    {
                        int a = rd.Subs[sub][j], b = rd.Subs[sub][j+1], c = rd.Subs[sub][j+2];
                        var bounds = new Bounds(rd.Verts[a].W, Vector3.zero); bounds.Encapsulate(rd.Verts[b].W); bounds.Encapsulate(rd.Verts[c].W);
                        var box = SwapMesh.XZ(bounds);
                        var near = Ground(sub) && bounds.max.y >= BedLift && bounds.min.y <= BedLift + .25f
                            ? bed.Near(box).Where(h => h.Overlaps(box)).ToArray() : new SwapShapes.Shape[0];
                        if (near.Length == 0) { wr.Keep(sub, a, b, c); continue; }
                        var polys = new List<List<SwapMesh.V>> { new() { rd.Verts[a], rd.Verts[b], rd.Verts[c] } };
                        foreach (var h in near)
                        {
                            var next = new List<List<SwapMesh.V>>();
                            // Disjoint outside regions of a convex tile. Keep the first failed half-plane,
                            // with all earlier inside planes applied; this subtracts the footprint exactly.
                            foreach (var poly in polys)
                            {
                                var inside = poly;
                                for (int e = 0; e < h.Poly.Length && inside.Count >= 3; e++)
                                {
                                    var x = h.Poly[e]; var y = h.Poly[(e+1)%h.Poly.Length];
                                    float A = -(y.y-x.y), B = y.x-x.x; double C = -(double)A*x.x-(double)B*x.y;
                                    var outside = SwapMesh.Clip(inside, new SwapMesh.Half(-A,-B,-C));
                                    if (outside.Count >= 3) next.Add(outside);
                                    inside = SwapMesh.Clip(inside, new SwapMesh.Half(A,B,C));
                                }
                            }
                            polys = next;
                        }
                        foreach (var poly in polys) wr.Poly(sub, poly);
                        cuts++;
                    }
                if (cuts == 0) continue;
                mf.sharedMesh = wr.Mesh(mf.sharedMesh.name + " (tram surface cut)");
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
                changed++; clipped += cuts;
                report.Log($"tram surface ownership: {mf.name}: {cuts} candidate triangles clipped; collider mesh retained");
            }
            report.Log($"tram surface ownership: {changed} imported render meshes, {clipped} candidate triangles clipped at laid tile footprints");
        }

        /// <summary>E12, the old walk-level ground across the plan's tram streets (Orbit Square: the old Southside's pedestrian square spans the
        /// boulevard axis at the walk, plan 0.5.5 runs the Boulevard and T1 through it). In each corridor quad of a tram road (kerb to kerb)
        /// that carries our bed: old flat modules standing over the bed (top 1 cm to 1 m, under 45 cm tall) whose centre is more than 0.8 m in
        /// from the kerb line give way whole (the kerbs laid across the road where it met the square; the kerbs along it stay); old generated
        /// flat ground over the bed (parts 1 cm to 1 m high) is cut exactly at the kerb lines, and old granite kerbs (the old KerbLine's pieces,
        /// pivot on the walk edge, facing the road) close each cut edge where walk-level ground was cut.</summary>
        void CrossGround(SwapPlan plan, System.Func<Transform, bool> ours)
        {
            if (plan == null) return;
            var quads = new List<(string id, Vector2[] q, Vector2 a, Vector2 b, float hw)>();
            foreach (var (id, w0, pts) in plan.Roads)
            {
                if (!plan.TramRoads.Contains(id)) continue;
                float hw = w0 * .5f;
                for (int i = 1; i < pts.Length; i++)
                {
                    Vector2 a = OldTownSwap.Old(pts[i - 1]), b = OldTownSwap.Old(pts[i]); if ((b - a).sqrMagnitude < 1e-4f) continue;
                    var d = (b - a).normalized; var n = new Vector2(-d.y, d.x) * hw;
                    var q = new[] { a - d * hw + n, b + d * hw + n, b + d * hw - n, a - d * hw - n };
                    var box = SwapPlan.Box(q);
                    if (!Bed.Near(box).Any(x => SwapMesh.Inside(box, x.Box.center) && Inside(q, x.Box.center))) continue;
                    quads.Add(($"{id}#{i}", q, a, b, hw));
                }
            }
            int gone = 0, cutParts = 0, kerbs = 0;
            var cutTops = new List<(Rect box, float top)>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (!t || !UnityEditor.PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) || ours(t)) continue;
                var rs = t.GetComponentsInChildren<MeshRenderer>(true); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.size.y >= .45f || b.max.y < BedLift + .01f || b.max.y > 1f) continue;
                var c = new Vector2(b.center.x, b.center.z);
                foreach (var (id, q, a, bb, hw) in quads)
                {
                    if (!Inside(q, c)) continue;
                    var d = (bb - a).normalized; float lat = Mathf.Abs(Vector2.Dot(c - a, new Vector2(-d.y, d.x)));
                    if (lat > hw - .8f) continue;
                    rep.Gave("old ground across our tram street (E12)", OldTownSwap.PieceOf(t.name), b.center, id);
                    Object.DestroyImmediate(t.gameObject); gone++; break;
                }
            }
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).ToArray())
            {
                if (!mf || !mf.sharedMesh || ours(mf.transform) || UnityEditor.PrefabUtility.IsPartOfPrefabInstance(mf.gameObject)) continue;
                var mr = mf.GetComponent<MeshRenderer>(); if (!mr || !mr.enabled) continue;
                var b0 = mr.bounds; if (b0.min.y > 1f || b0.max.y < BedLift + .01f) continue;
                var hitQ = quads.Where(x => SwapMesh.Overlaps(SwapPlan.Box(x.q), SwapMesh.XZ(b0))).ToList(); if (hitQ.Count == 0) continue;
                if (!mf.sharedMesh.isReadable) { rep.Log($"tram street: {mf.name} not readable; left whole"); continue; }
                var rd = new SwapMesh.Read(mf.sharedMesh, mf.transform.localToWorldMatrix);
                var ids = rd.Components(out int count);
                var cb = new Bounds[count]; var has = new bool[count];
                for (int s = 0; s < rd.Subs.Count; s++)
                    for (int k = 0; k < ids[s].Length; k++)
                        for (int j = 0; j < 3; j++) { var p = rd.Verts[rd.Subs[s][3 * k + j]].W; int cc = ids[s][k]; if (!has[cc]) { cb[cc] = new Bounds(p, Vector3.zero); has[cc] = true; } else cb[cc].Encapsulate(p); }
                var cut = new List<Vector2[]>[count]; int any = 0;
                for (int cc = 0; cc < count; cc++)
                {
                    if (!has[cc] || cb[cc].max.y < BedLift + .01f || cb[cc].max.y > 1f || cb[cc].size.y > .6f) continue;
                    var box = SwapMesh.XZ(cb[cc]);
                    foreach (var x in hitQ)
                        if (SwapMesh.Overlaps(SwapPlan.Box(x.q), box) && Bed.Near(box).Any(z => z.Overlaps(box) && Inside(x.q, z.Box.center)))
                        { (cut[cc] ??= new List<Vector2[]>()).Add(x.q); }
                    if (cut[cc] == null) continue;
                    any++; cutTops.Add((box, cb[cc].max.y));
                    rep.Gave("old ground across our tram street (E12, cut at the kerb lines)", "mesh:" + mf.name, cb[cc].center, string.Join("+", hitQ.Where(x => cut[cc].Contains(x.q)).Select(x => x.id)));
                }
                if (any == 0) continue;
                var wr = new SwapMesh.Write(rd);
                for (int s = 0; s < rd.Subs.Count; s++)
                    for (int k = 0; k < ids[s].Length; k++)
                    {
                        int ia = rd.Subs[s][3 * k], ib = rd.Subs[s][3 * k + 1], ic = rd.Subs[s][3 * k + 2], cc = ids[s][k];
                        if (cut[cc] == null) { wr.Keep(s, ia, ib, ic); continue; }
                        var polys = new List<List<SwapMesh.V>> { new() { rd.Verts[ia], rd.Verts[ib], rd.Verts[ic] } };
                        foreach (var q in cut[cc])
                        {
                            var next = new List<List<SwapMesh.V>>();
                            foreach (var poly in polys)
                                foreach (var reg in OutsideConvex(q))
                                {
                                    var o = poly; foreach (var h in reg) { o = SwapMesh.Clip(o, h); if (o.Count < 3) break; }
                                    if (o.Count >= 3) next.Add(o);
                                }
                            polys = next;
                        }
                        foreach (var poly in polys) wr.Poly(s, poly);
                    }
                cutParts += any;
                if (wr.Triangles == 0) { Object.DestroyImmediate(mf.gameObject); continue; }
                mf.sharedMesh = wr.Mesh(mf.sharedMesh.name + " (tram street)");
                var col = mf.GetComponent<MeshCollider>(); if (col) col.sharedMesh = mf.sharedMesh;
            }
            // kerbs on the cut edges: every 2 m along each long side of a quad, where walk-level ground was cut just outside it
            var root = new GameObject("Tram street kerbs (old Kerb_Granite on E12's cut edges)").transform; root.SetParent(Root, false);
            var placed = new HashSet<Vector2Int>();
            foreach (var (id, q, a, b, hw) in quads)
            {
                var d = (b - a); float len = d.magnitude; d /= len; var n = new Vector2(-d.y, d.x);
                foreach (var side in new[] { 1f, -1f })
                {
                    int i = 0;
                    for (float s = 1f; s < len; s += 2f, i++)
                    {
                        var e = a + d * s + n * (side * hw); var outP = e + n * (side * .5f);
                        if (!cutTops.Any(x => x.top > .1f && SwapMesh.Inside(x.box, outP))) continue;
                        var key = Vector2Int.RoundToInt(e * 2f); if (!placed.Add(key)) continue;
                        var inward = -n * side;
                        Put($"Kerb_Granite_{i & 1}", new Vector3(e.x, 0f, e.y), Mathf.Atan2(inward.x, inward.y) * Mathf.Rad2Deg, root); kerbs++;
                    }
                }
            }
            rep.Log($"tram streets (E12): {quads.Count} corridor quads of {string.Join(", ", plan.TramRoads.OrderBy(x => x))} carry our bed; {gone} old flat pieces across the road and {cutParts} parts of old walk-level ground over the bed gave way (cut at the kerb lines), {kerbs} old granite kerbs close the cut edges");
        }

        static bool Inside(Vector2[] q, Vector2 p)
        {
            bool? sign = null;
            for (int i = 0; i < q.Length; i++)
            {
                Vector2 a = q[i], b = q[(i + 1) % q.Length];
                float c = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
                if (Mathf.Abs(c) < 1e-6f) continue;
                if (sign == null) sign = c > 0; else if ((c > 0) != sign) return false;
            }
            return true;
        }

        /// <summary>The plane outside a convex polygon as disjoint convex regions: outside edge i and inside edges 0..i-1.</summary>
        static SwapMesh.Half[][] OutsideConvex(Vector2[] q)
        {
            float area = 0f; for (int i = 0; i < q.Length; i++) { var a = q[i]; var b = q[(i + 1) % q.Length]; area += a.x * b.y - b.x * a.y; }
            float k = area > 0f ? 1f : -1f;  // inside = left of each edge for a counter-clockwise ring
            SwapMesh.Half In(int i) { Vector2 a = q[i], b = q[(i + 1) % q.Length]; float A = -(b.y - a.y) * k, B = (b.x - a.x) * k; return new SwapMesh.Half(A, B, -(A * a.x + B * a.y)); }
            var o = new SwapMesh.Half[q.Length][];
            for (int i = 0; i < q.Length; i++)
            {
                var h = In(i); var reg = new List<SwapMesh.Half> { new SwapMesh.Half(-h.A, -h.B, -h.C) };
                for (int j = 0; j < i; j++) reg.Add(In(j));
                o[i] = reg.ToArray();
            }
            return o;
        }

        public void Trams()
        {
            var root = new GameObject("Trams (old Tram at our stops)").transform; root.SetParent(Root, false);
            var placed = new List<Vector2>();
            void Tram(Vector2 at, Vector2 d, string why)
            {
                if (placed.Any(p => Vector2.Distance(p, at) < 15f)) return;
                Put("Tram", OldTownSwap.Old3(at, 0f), Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, root);
                placed.Add(at);
            }
            foreach (var g in w.Transit.Stops.GroupBy(s => s.Id.Substring(0, s.Id.LastIndexOf('/'))))
            {
                var c = Vector2.zero; int k = 0; foreach (var s in g) foreach (var p in s.Ring) { c += p; k++; } c /= Mathf.Max(1, k);
                if (SwapMesh.Inside(OldTownSwap.ZonePlan, c)) continue;
                var near = rails.OrderBy(r => Vector2.Distance(r.at, c)).FirstOrDefault();
                if (rails.Count == 0 || Vector2.Distance(near.at, c) > 12f) { rep.Log($"tram: no rail of ours by stop {g.Key} (old track there)"); continue; }
                Tram(near.at, near.dir, g.Key);
            }
            foreach (var t in w.Trams) if (t.Kit != null) { float y = float.IsNaN(t.Kit.Yaw) ? 0f : t.Kit.Yaw; Tram(t.Kit.At, new Vector2(-Mathf.Sin(y), Mathf.Cos(y)), t.Id); }  // our Tram piece is the old design: unity yaw = -yaw, as KitPlacer
            int canopies = 0;
            foreach (var it in w.Street.Where(x => x.Kind == "tram-shelter" && !SwapMesh.Inside(OldTownSwap.ZonePlan, x.At)))
            { Put("TramCanopy", OldTownSwap.Old3(it.At, it.Z), -it.Yaw * Mathf.Rad2Deg, root); canopies++; }
            rep.Log($"trams: {placed.Count} old Tram at our stops and parked tram; {canopies} old TramCanopy at our stop shelters outside the zone");
        }
    }
}
#endif
