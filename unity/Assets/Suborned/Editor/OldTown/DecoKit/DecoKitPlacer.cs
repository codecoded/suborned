#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitData;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Places kit prefabs by name. Missing prefabs (families not built yet) are counted and skipped, so a
    /// layout can be authored ahead of its assets.
    internal sealed class DecoKitPlacer
    {
        readonly Dictionary<string, GameObject> prefabs = new();
        readonly Dictionary<string, Transform> groups = new();
        readonly HashSet<string> missing = new();
        readonly Transform root;

        internal DecoKitPlacer(Transform root)
        {
            this.root = root;
            foreach (var row in DecoKitData.Assets().assets)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(row));
                if (prefab != null) prefabs[row.name] = prefab;
            }
        }

        internal IReadOnlyCollection<string> Missing => missing;
        internal bool Has(string name) => prefabs.ContainsKey(name);

        /// The prefab asset itself (for movers that spawn their own instances); null counts as missing.
        internal GameObject Prefab(string name)
        {
            if (prefabs.TryGetValue(name, out var prefab)) return prefab;
            missing.Add(name);
            return null;
        }

        /// Pivot placement: position + yaw (degrees), optional X mirror (for handed modules).
        internal GameObject Put(string name, Vector3 pos, float yaw = 0f, bool mirror = false, float scale = 1f)
        {
            if (!prefabs.TryGetValue(name, out var prefab)) { missing.Add(name); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Group(name));
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = new Vector3(mirror ? -scale : scale, scale, scale);
            return go;
        }

        /// Everything placed so far: (prefab name, pivot).
        internal IEnumerable<(string name, Vector3 pos)> Placed()
        {
            foreach (var (name, group) in groups)
                foreach (Transform t in group) yield return (name, t.position);
        }

        /// Places so the world-space bounds' +Z face sits on frontZ and the bounds are centred on x.
        internal GameObject PutFront(string name, float x, float frontZ, float y, float yaw = 0f, bool mirror = false)
        {
            var go = Put(name, new Vector3(x, y, frontZ), yaw, mirror);
            if (go == null) return null;
            var b = DecoKitModels.Bounds(go);
            go.transform.position += new Vector3(x - b.center.x, 0f, frontZ - b.max.z);
            return go;
        }

        /// Places so the bounds' -Z face sits on backZ (e.g. a stair against a facade).
        internal GameObject PutBack(string name, float x, float backZ, float y, float yaw = 0f)
        {
            var go = Put(name, new Vector3(x, y, backZ), yaw);
            if (go == null) return null;
            var b = DecoKitModels.Bounds(go);
            go.transform.position += new Vector3(x - b.center.x, 0f, backZ - b.min.z);
            return go;
        }

        /// World-space size of a prefab at a yaw (measured on a throwaway instance).
        internal Vector3 Size(string name, float yaw = 0f)
        {
            var go = Put(name, Vector3.zero, yaw);
            if (go == null) return Vector3.zero;
            var s = DecoKitModels.Bounds(go).size;
            Object.DestroyImmediate(go);
            return s;
        }

        /// Guards the ground convention: a ground module placed at y = 0 must top out at the walk height.
        internal void ReportTop(string name, float walk)
        {
            var go = Put(name, new Vector3(0f, 0f, -500f));
            if (go == null) return;
            float top = DecoKitModels.Bounds(go).max.y;
            Object.DestroyImmediate(go);
            var msg = $"[DecoKit] {name} top y = {top:0.000} (walk {walk:0.000})";
            if (Mathf.Abs(top - walk) > .005f) Debug.LogError(msg);
            else Debug.Log(msg);
        }

        Transform Group(string name)
        {
            if (groups.TryGetValue(name, out var t)) return t;
            t = new GameObject(name).transform;
            t.SetParent(root, false);
            return groups[name] = t;
        }
    }
}
#endif
