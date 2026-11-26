using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.OldTown.Editor.Swap
{
    // A single hull round a monument's feet would close the entire park beneath a raised sphere.
    // Slice each solid mesh at pedestrian height and keep the union of the occupied two-metre cells.
    public static class DistrictLandmarkSurvey
    {
        [Serializable] class Survey { public string[] pieces; public Span[] spans; }
        [Serializable] class Span { public int east, end, north; }
        static readonly HashSet<string> Pieces = new() { "SphereStation", "DiscTower", "ParabolicArch", "Skylon", "Skyport_Tower", "LinerDock", "CircusMonument" };

        public static void Export()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/OldTown.unity");
            WriteOpen(Path.GetFullPath("../out/batch/swap/landmark-ground.json"));
        }
        internal static void WriteOpen(string path)
        {
            var occupied = new HashSet<Vector2Int>();
            var found = new HashSet<string>();
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                var piece = OldTownSwap.PieceOf(t.name);
                if (!Pieces.Contains(piece) || !PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) continue;
                found.Add(piece);
                foreach (var f in t.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer = f.GetComponent<MeshRenderer>();
                    if (!renderer || !f.sharedMesh || renderer.bounds.min.y > .9f || renderer.bounds.max.y < .9f) continue;
                    Slice(f, occupied);
                }
            }
            if (!found.Contains("SphereStation")) throw new InvalidDataException("The played town has no SphereStation to survey");
            var spans = new List<Span>();
            foreach (var row in occupied.GroupBy(p => p.y).OrderBy(g => g.Key))
            {
                Span run = null;
                foreach (var x in row.Select(p => p.x).OrderBy(x => x))
                {
                    if (run == null || x != run.end) { run = new Span { east = x, north = row.Key }; spans.Add(run); }
                    run.end = x + 2;
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(new Survey { pieces = found.OrderBy(p => p, StringComparer.Ordinal).ToArray(), spans = spans.ToArray() }));
        }

        static void Slice(MeshFilter filter, HashSet<Vector2Int> cells)
        {
            using var read = Mesh.AcquireReadOnlyMeshData(filter.sharedMesh);
            var data = read[0];
            using var verts = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);
            data.GetVertices(verts);
            var cross = new Dictionary<int,List<float>>();
            var matrix = filter.transform.localToWorldMatrix;
            var worldVerts = verts;
            for (int k=0; k<verts.Length; k++) worldVerts[k]=matrix.MultiplyPoint3x4(verts[k]);
            for (int sub=0; sub<data.subMeshCount; sub++)
            {
                var desc=data.GetSubMesh(sub);
                if (desc.topology != MeshTopology.Triangles) continue;
                using var indices=new NativeArray<int>(desc.indexCount, Allocator.Temp);
                data.GetIndices(indices,sub);
                for(int t=0;t<indices.Length;t+=3) {
                    var a=verts[indices[t]]; var b=verts[indices[t+1]]; var c=verts[indices[t+2]];
                    if ((a.y>.9f)==(b.y>.9f) && (a.y>.9f)==(c.y>.9f)) continue;
                    if ((b.y>.9f)!=(c.y>.9f)) { if ((a.y>.9f)==(b.y>.9f)) (a,c)=(c,a); else (a,b)=(b,a); }
                    var p=Vector3.LerpUnclamped(a,b,(.9f-a.y)/(b.y-a.y));
                    var q=Vector3.LerpUnclamped(a,c,(.9f-a.y)/(c.y-a.y));
                    p.z-=OldTownSwap.DZ; q.z-=OldTownSwap.DZ;
                    if(p.z>q.z)(p,q)=(q,p);
                    for(int n=2*Mathf.CeilToInt((p.z-1)/2);n+1<q.z;n+=2) {
                        if(!cross.TryGetValue(n,out var xs)) cross[n]=xs=new List<float>();
                        xs.Add(Mathf.Lerp(p.x,q.x,(n+1-p.z)/(q.z-p.z)));
                    }
                }
            }
            foreach(var row in cross) {
                row.Value.Sort();
                for(int k=1;k<row.Value.Count;k+=2)
                    for(int x=2*Mathf.CeilToInt((row.Value[k-1]-1)/2);x+1<row.Value[k];x+=2)
                        cells.Add(new Vector2Int(x,row.Key));
            }
        }
    }
}
