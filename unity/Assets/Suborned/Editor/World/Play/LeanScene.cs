using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Suborned.World.Play
{
    /// <summary>Brief 097 (Sky, 2026-11-06: "drop any assets outside of the district office for now that aren't used for faster iteration and
    /// we add back in later"): a scratch-only copy of OldTown.unity holding the Handover zone and a margin round it. Only pure geometry outside
    /// the keep box goes (objects whose components are all Transform, mesh, renderer, LOD group or collider, and whose whole subtree's bounds
    /// miss the box); every script, camera, light, volume and probe stays, and so does anything that reaches into the box. The copy is saved
    /// at <see cref="LeanPath"/> (git-ignored) under the same scene name, "OldTown", so the front end loads it unchanged. The real OldTown.unity
    /// is never touched: a build without -lean ships the whole town. Used by GameBuild.CliScratch with -lean [margin m, default 110].</summary>
    public static class LeanScene
    {
        public const string LeanPath = "Assets/Scenes/Lean/OldTown.unity";
        // OldTownSwap.ZonePlan (plan x, plan y) moved by OldTownSwap.DZ along Unity z: x -226..-84, z -166..58 in the old town's frame
        internal static readonly Rect ZoneXZ = Rect.MinMaxRect(-226f, -372f + 206f, -84f, -148f + 206f);
        static readonly HashSet<Type> Geometry = new() { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(LODGroup), typeof(MeshCollider), typeof(BoxCollider), typeof(SphereCollider), typeof(CapsuleCollider) };

        public static float MarginFromArgs()
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-lean");
            if (i < 0) return -1f;
            return i + 1 < a.Length && float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var m) ? m : 110f;
        }

        /// <summary>Saves the open scene as the lean copy, strips it and saves again; returns the copy's path. The open scene stays the copy.</summary>
        public static string Make(float margin)
        {
            var src = EditorSceneManager.GetActiveScene();
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", Path.GetDirectoryName(LeanPath)!));
            if (!EditorSceneManager.SaveScene(src, LeanPath, true)) throw new IOException($"LeanScene: could not save {LeanPath}");
            var scene = EditorSceneManager.OpenScene(LeanPath, OpenSceneMode.Single);
            var keep = Rect.MinMaxRect(ZoneXZ.xMin - margin, ZoneXZ.yMin - margin, ZoneXZ.xMax + margin, ZoneXZ.yMax + margin);
            int dropped = 0, renderersDropped = 0, kept = 0;
            foreach (var root in scene.GetRootGameObjects()) Walk(root.transform, keep, ref dropped, ref renderersDropped);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-leanSea") >= 0) Sea(keep);
            kept = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log($"LeanScene: {LeanPath}: margin {margin} m round the zone; dropped {dropped} objects ({renderersDropped} renderers), {kept} renderers kept");
            return LeanPath;
        }

        static void Walk(Transform t, Rect keep, ref int dropped, ref int renderersDropped)
        {
            var go = t.gameObject;
            bool prefabPart = PrefabUtility.IsPartOfPrefabInstance(go), prefabRoot = PrefabUtility.IsOutermostPrefabInstanceRoot(go);
            if ((!prefabPart || prefabRoot) && PureGeometry(go, out var b, out int n) && n > 0 && !Hits(b, keep))
            {
                dropped++; renderersDropped += n; Object.DestroyImmediate(go); return;
            }
            if (prefabPart && !prefabRoot) return; // inside a prefab instance: whole or nothing
            for (int i = t.childCount - 1; i >= 0; i--) Walk(t.GetChild(i), keep, ref dropped, ref renderersDropped);
        }

        static bool PureGeometry(GameObject go, out Bounds bounds, out int renderers)
        {
            bounds = default; renderers = 0; bool any = false;
            foreach (var c in go.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                if (!Geometry.Contains(c.GetType())) return false;
                if (c is Renderer r) { renderers++; if (any) bounds.Encapsulate(r.bounds); else { bounds = r.bounds; any = true; } }
            }
            return true;
        }

        /// <summary>-leanSea (Sky 2026-11-06: "knocking those out and replacing with water"): the old harbour's water (DK_Bay_Water) as a frame round the
        /// keep box, 0.3 m over the street (over the 0.18 m walks), 800 m out, so the cut roads and paving beyond the margin sink under a sea and the
        /// district reads as an island. Four quads, no shadows, nothing in the sim reads them.</summary>
        static void Sea(Rect keep)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Suborned/OldTownArt/DecoKit/Generated/DK_Bay_Water.mat"); if (!mat) { Debug.LogWarning("LeanScene: no DK_Bay_Water"); return; }
            const float Out = 800f, Y = 0.3f; var root = new GameObject("Lean sea (brief 097)");
            var o = Rect.MinMaxRect(keep.xMin - Out, keep.yMin - Out, keep.xMax + Out, keep.yMax + Out);
            var parts = new[] { Rect.MinMaxRect(o.xMin, o.yMin, o.xMax, keep.yMin), Rect.MinMaxRect(o.xMin, keep.yMax, o.xMax, o.yMax),
                                Rect.MinMaxRect(o.xMin, keep.yMin, keep.xMin, keep.yMax), Rect.MinMaxRect(keep.xMax, keep.yMin, o.xMax, keep.yMax) };
            foreach (var r in parts)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(q.GetComponent<Collider>()); q.transform.SetParent(root.transform, false);
                q.transform.SetPositionAndRotation(new Vector3(r.center.x, Y, r.center.y), Quaternion.Euler(90f, 0f, 0f)); q.transform.localScale = new Vector3(r.width, r.height, 1f);
                var mr = q.GetComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Debug.Log($"LeanScene: sea round the keep box at {Y} m, {Out} m out");
        }

        static bool Hits(Bounds b, Rect keep) => b.max.x >= keep.xMin && b.min.x <= keep.xMax && b.max.z >= keep.yMin && b.min.z <= keep.yMax;
    }
}
