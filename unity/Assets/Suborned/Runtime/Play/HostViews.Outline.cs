using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using HoverOutlinePass = Suborned.HoverOutlinePass;

namespace Suborned.Play
{
    /// <summary>Brief 101, the hover outline (Suborned.HoverOutlinePass; Glyph's spec, briefs/101/glyph/outline/notes.md "shape"): what each view draws
    /// into the outline's mask. A figure's skinned renderers are drawn as they are (the host puts the outline's bit on them); a plain mesh is drawn
    /// through a twin (the same mesh under it with the stand-in material, off the GPU Resident Drawer, so the drawer rebuilds nothing). The door,
    /// the cabinet and the stashes have no mesh of their own (HostViews.Marker), so each has a stand-in drawn only into the mask:
    ///  · the Office's public door: an arched panel the size of its doorway, in the RecordsEntrance piece's own frame (pivot on the face at the door
    ///    axis on the pavement, forward out of the face; the piece stands where the block's entrance record puts it: office-entrance, at (64, -41.25),
    ///    z 0.75, on the wing's west face y -40.5). From tools/decokit/civic_records.py build(grand): the doors' half width PR 1.65 m on the landing
    ///    ZL 0.75 m, run up into the arched window over them to its spring PS 7.6 m and its semicircle (radius PR), in the doors' plane 0.12 m out
    ///    of the face (doors(): y -0.12);
    ///  · the Tech's cabinet: the box of the block's structure record (civic-block.json civic-control-cabinet: x 44.546..45.454, y -45.344..-43.656,
    ///    1.575 m high), centred where the builder put its element;
    ///  · a stash: its bin (the street item's own mesh, twinned) or its service door's leaf (Editor/World/ServiceDoors: 1.2 m wide, 0.12 to 2.3 m up,
    ///    0.15 m out of the wall).
    /// A camera is its head: a hovering head's parts, a kit pole's model, a lamp mast's crown and pods (its piece, cut at the band under the crown).
    /// View only.</summary>
    public sealed partial class HostViews
    {
        const float ArchHalf = 1.65f, ArchSill = 0.75f, ArchSpring = 7.6f, ArchOut = 0.12f;                              // civic_records.py PR, ZL, PS; doors() y -0.12
        static readonly Vector2 CabinetLo = new(44.546f, -45.344f), CabinetHi = new(45.454f, -43.656f); const float CabinetTall = 1.575f; // civic-block.json civic-control-cabinet
        const float LeafWide = 1.2f, LeafFoot = 0.12f, LeafTop = 2.3f, LeafOut = 0.15f, LeafThick = 0.06f, DoorPartsOut = 0.3f;   // ServiceDoors: the leaf; its element's centre 0.3 m out
        const float MastHeadFrom = 7.3f, MastLensUp = 7.68f; // civic_mast101.py: the seafoam band under the crown begins 7.34 m up (the head stands above it); each lens centre 7.68 m up
        /// <summary>The clip height of a thing that makes its line all round (finite: the shader compares against it).</summary>
        public const float NoClip = -1e9f;

        static readonly Dictionary<Renderer, Renderer> twins = new(); static readonly HashSet<Renderer> drawnOnlyInMask = new();
        static readonly Dictionary<string, Transform> named = new(); static bool namedBuilt;
        static Material standInMat; static Mesh arch;
        readonly Dictionary<string, Renderer> standIns = new();
        readonly List<Renderer> olFound = new(); readonly List<MeshRenderer> olMeshes = new();

        /// <summary>Whether a renderer is a twin or a stand-in (drawn only into the outline's mask: the host turns it on and off).</summary>
        public static bool OutlineOnly(Renderer r) => r && drawnOnlyInMask.Contains(r);

        /// <summary>The renderers a view draws into the outline's mask (into `into`, cleared), and the world height below which it makes no line (a mast's
        /// post), else NoClip. Empty: nothing of it is drawn now.</summary>
        public void OutlineRenderers(string key, List<Renderer> into, out float clipY)
        {
            into.Clear(); clipY = NoClip; if (string.IsNullOrEmpty(key)) return;
            if (key == "marker/door") { Add(into, DoorStandIn()); return; }
            if (key == "marker/cabinet") { Add(into, CabinetStandIn()); return; }
            if (key.StartsWith("stash/")) { StashOutline(key, into); return; }
            if (key.StartsWith("camera/")) { CameraOutline(key, into, ref clipY); return; }
            if (things.TryGetValue(key, out var th) && th.shown && th.t && th.t.gameObject.activeInHierarchy) Collect(th.t, into, true);
        }

        static void Add(List<Renderer> into, Renderer r) { if (r) into.Add(r); }

        /// <summary>Every drawn mesh under `t`: a skinned one as it is (every LOD: the camera's culling picks one), a plain one by its twin (LOD 0 only).</summary>
        void Collect(Transform t, List<Renderer> into, bool skinned)
        {
            t.GetComponentsInChildren(false, olFound);
            foreach (var r in olFound)
            {
                if (!r.enabled || drawnOnlyInMask.Contains(r)) continue;
                if (r is SkinnedMeshRenderer) { if (skinned) into.Add(r); }
                else if (r is MeshRenderer mr && Lod0(mr, t)) Add(into, Twin(mr));
            }
        }

        /// <summary>A plain mesh renderer drawn by the camera close up: not under a "__LOD" part (the kit's lower LODs) nor in a LOD group's later levels.</summary>
        static bool Lod0(Renderer r, Transform top)
        {
            if (lod0.TryGetValue(r, out var was)) return was; if (lod0.Count > 2048) lod0.Clear();
            bool is0 = true;
            for (var p = r.transform; p && p != top.parent; p = p.parent)
            {
                if (p.name.StartsWith("__LOD")) { is0 = false; break; }
                if (p.GetComponent<LODGroup>() is LODGroup g) { var lods = g.GetLODs(); for (int i = 1; i < lods.Length && is0; i++) if (System.Array.IndexOf(lods[i].renderers, r) >= 0) is0 = false; break; }
            }
            return lod0[r] = is0;
        }
        static readonly Dictionary<Renderer, bool> lod0 = new(); // a renderer's LOD never changes: asked once, not every frame it is outlined

        /// <summary>The twin of a plain mesh renderer, made once: the same mesh, under it, with the stand-in material on every submesh.</summary>
        static Renderer Twin(MeshRenderer r)
        {
            if (twins.TryGetValue(r, out var tw) && tw) return tw;
            var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) return null;
            if (twins.Count > 512) { var dead = new List<Renderer>(); foreach (var kv in twins) if (!kv.Key || !kv.Value) dead.Add(kv.Key); foreach (var d in dead) twins.Remove(d); drawnOnlyInMask.RemoveWhere(x => !x); }
            tw = StandIn("outline-twin", r.transform, mf.sharedMesh, r.gameObject.layer); if (!tw) return null;
            tw.transform.localPosition = Vector3.zero; tw.transform.localRotation = Quaternion.identity; tw.transform.localScale = Vector3.one;
            var mats = new Material[mf.sharedMesh.subMeshCount]; for (int i = 0; i < mats.Length; i++) mats[i] = standInMat; tw.sharedMaterials = mats;
            twins[r] = tw; return tw;
        }

        /// <summary>A renderer drawn only into the outline's mask: off until outlined, no shadows, probes or motion, out of the GPU Resident Drawer.</summary>
        static Renderer StandIn(string name, Transform parent, Mesh mesh, int layer)
        {
            if (!standInMat) { var sh = Resources.Load<Shader>("Shaders/HoverOutlineStandIn"); if (!sh) return null; standInMat = new Material(sh) { name = "outline stand-in", hideFlags = HideFlags.HideAndDontSave }; }
            var go = new GameObject(name); go.layer = layer; go.transform.SetParent(parent, false); // its shader has no DOTS instancing: the GPU Resident Drawer leaves it alone
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = standInMat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off; mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            mr.allowOcclusionWhenDynamic = false; mr.renderingLayerMask = 0; mr.enabled = false;
            drawnOnlyInMask.Add(mr); return mr;
        }

        /// <summary>Turns an outline renderer on (with its bit) or off: a twin or stand-in is enabled only while outlined; a figure keeps its own bits.</summary>
        public static void OutlineMark(Renderer r, uint bit, bool on)
        {
            if (!r) return;
            if (drawnOnlyInMask.Contains(r)) { r.renderingLayerMask = on ? bit : 0u; r.enabled = on; return; }
            r.renderingLayerMask = on ? r.renderingLayerMask | bit : r.renderingLayerMask & ~bit;
        }

        /// <summary>A scene element by its exact name (the builders name them by the export's ids, with slashes), from one walk of the scene, kept.</summary>
        Transform Named(string name)
        {
            if (named.TryGetValue(name, out var t) && t) return t;
            if (namedBuilt && !named.ContainsKey(name)) return null;
            named.Clear(); namedBuilt = true;
            foreach (var go in host.gameObject.scene.GetRootGameObjects())
                foreach (var x in go.GetComponentsInChildren<Transform>(true))
                    if (x.name == "DK_RecordsEntrance" || x.name.StartsWith("civic/civic-control-cabinet") || x.name.StartsWith("service-door/") || x.name.StartsWith("street/civic/stash/")) named.TryAdd(x.name, x);
            return named.TryGetValue(name, out t) ? t : null;
        }
        /// <summary>The scene walk Named needs, done once at a quiet moment (the host calls it on its first outline frame), not on the first hover.</summary>
        public void OutlineWarm() { if (!namedBuilt) Named("DK_RecordsEntrance"); }

        Renderer DoorStandIn()
        {
            if (standIns.TryGetValue("door", out var r)) return r;
            var piece = Named("DK_RecordsEntrance");
            if (piece) { if (!arch) arch = Arch(ArchHalf, ArchSill, ArchSpring, ArchOut); r = StandIn("outline/door", root, arch, piece.gameObject.layer); if (r) { r.transform.SetPositionAndRotation(piece.position, piece.rotation); r.transform.localScale = piece.lossyScale; } }
            else Debug.LogWarning("HostViews: no DK_RecordsEntrance in the scene: the Office door has no outline");
            standIns["door"] = r; return r;
        }

        Renderer CabinetStandIn()
        {
            if (standIns.TryGetValue("cabinet", out var r)) return r;
            var el = Named("civic/civic-control-cabinet");
            Vector2 lo = frame.ToPlan(CabinetLo.x, CabinetLo.y), hi = frame.ToPlan(CabinetHi.x, CabinetHi.y), mid = (lo + hi) / 2f;
            var centre = el ? el.position : new Vector3(mid.x, frame.Height((CabinetLo.x + CabinetHi.x) / 2f, (CabinetLo.y + CabinetHi.y) / 2f) + CabinetTall / 2f, mid.y);
            var ay = new Vector3(frame.axisY.x, 0f, frame.axisY.y); // the box's long side runs along block y; LookRotation puts block x across it
            r = StandIn("outline/cabinet", root, cube, 0);
            if (r) { r.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(ay.normalized, Vector3.up)); r.transform.localScale = new Vector3((CabinetHi.x - CabinetLo.x) * frame.axisX.magnitude, CabinetTall, (CabinetHi.y - CabinetLo.y) * frame.axisY.magnitude); }
            standIns["cabinet"] = r; return r;
        }

        void StashOutline(string key, List<Renderer> into)
        {
            var g = host.Game; var st = g?.S.Stashes.Find(x => "stash/" + x.Id == key); if (st == null) return;
            if (st.Kind != "door") { var bin = Named("street/civic/stash/" + st.Of); if (bin) Collect(bin, into, false); return; }
            if (!standIns.TryGetValue(key, out var r))
            {
                var el = Named("service-door/" + st.Of); var e = g.S.Entrances.Find(x => x.id == st.Of);
                if (el && e.id != null)
                {
                    var at = frame.World(e.at); var n = at - el.position; n.y = 0f;
                    if (n.sqrMagnitude > 0.0025f)
                    {
                        n.Normalize(); r = StandIn("outline/" + key, root, cube, 0); // the element stands at its parts' centre, 0.3 m out of the wall; the leaf 0.15 m out
                        if (r) { var c = el.position - n * (DoorPartsOut - LeafOut); c.y = at.y + (LeafFoot + LeafTop) / 2f; r.transform.SetPositionAndRotation(c, Quaternion.LookRotation(n, Vector3.up)); r.transform.localScale = new Vector3(LeafWide, LeafTop - LeafFoot, LeafThick); }
                    }
                }
                standIns[key] = r;
            }
            Add(into, r);
        }

        void CameraOutline(string key, List<Renderer> into, ref float clipY)
        {
            if (masts.TryGetValue(key, out var m) && m.W.piece) // the lamp mast: its piece (the lenses too), cut below the crown
            {
                var piece = m.W.piece; piece.GetComponentsInChildren(false, olMeshes);
                foreach (var mr in olMeshes) if (mr.enabled && !drawnOnlyInMask.Contains(mr) && Lod0(mr, piece)) Add(into, Twin(mr));
                float foot = things.TryGetValue(key, out var pole) && pole.t ? pole.t.position.y - 1.6f : piece.position.y; // the stand-in stands 1.6 m over the foot (the piece's element is at its parts' centre)
                clipY = foot + m.Lens * (MastHeadFrom / MastLensUp); return; // the lenses as built set the scale
            }
            if (camHeads.TryGetValue(key, out var h) && h.Head) Collect(h.Head, into, true); // a hovering head (its model and lens), or a pole's lens and housing
            if (things.TryGetValue(key, out var th) && th.t && th.t.Find("model") is Transform pm) Collect(pm, into, true); // the kit pole
        }

        /// <summary>The door's panel in the piece's frame: x across the face, y up, z out of it; a fan from inside (the shape is convex).</summary>
        static Mesh Arch(float half, float sill, float spring, float z, int seg = 24)
        {
            var v = new List<Vector3> { new(0f, (sill + spring) / 2f, z), new(-half, sill, z), new(half, sill, z) };
            for (int i = 0; i <= seg; i++) { float a = Mathf.PI * i / seg; v.Add(new Vector3(half * Mathf.Cos(a), spring + half * Mathf.Sin(a), z)); }
            var t = new List<int>(); int n = v.Count - 1; // the outline: 1, 2, then the arc 3..n, back to 1
            for (int i = 1; i < n; i++) { t.Add(0); t.Add(i); t.Add(i + 1); }
            t.Add(0); t.Add(n); t.Add(1);
            var m = new Mesh { name = "outline door" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
        }
    }
}
