using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Suborned.World.Play
{
    static class DistrictEstateSurfaces
    {
        const string Art = "Assets/Suborned/Materials/Play/";
        static Texture2D Map(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(Art + file)
            ?? throw new BuildFailedException("Missing estate surface " + file);

        public static void Apply(Scene scene, List<Bounds> estateRoofs)
        {
            var stone = new Material(Shader.Find("DecoKit/Lit Cutaway")) { name = "Estate honed limestone", enableInstancing = true };
            stone.SetTexture("_BaseMap", Map("ResidenceStone.jpg"));
            stone.SetTexture("_BumpMap", Map("ResidenceStoneNormal.png"));
            stone.SetFloat("_BumpScale", .65f);
            stone.SetFloat("_Smoothness", .25f);
            stone.EnableKeyword("_NORMALMAP");
            var glass = new Material(Shader.Find("Suborned/Harbour/Shop windows")) { name = "Occupied estate windows", enableInstancing = true };
            glass.SetTexture("_InteriorAtlas", Map("ResidenceWindows.jpg"));
            glass.SetFloat("_FittedSash", 1f);

            int walls = 0, panes = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var source = filter.sharedMesh;
                if (!source || !source.name.StartsWith("megastructures/facade", System.StringComparison.Ordinal) ||
                    !filter.TryGetComponent<MeshRenderer>(out var r) || source.subMeshCount != 1) continue;
                var b = r.bounds;
                if (!estateRoofs.Any(roof => roof.min.x <= b.max.x + 1 && roof.max.x >= b.min.x - 1 &&
                    roof.min.z <= b.max.z + 1 && roof.max.z >= b.min.z - 1)) continue;

                bool window = source.name.StartsWith("megastructures/facade-gloss@", System.StringComparison.Ordinal);
                var mesh = Object.Instantiate(source);
                mesh.name = source.name + (window ? " fitted sashes" : " stone courses");
                var matrix = filter.transform.localToWorldMatrix;
                var points = mesh.vertices.Select(p => matrix.MultiplyPoint3x4(p)).ToArray();
                var normals = mesh.normals.Select(n => matrix.MultiplyVector(n).normalized).ToArray();
                var uv = new Vector2[points.Length];
                if (window)
                {
                    var cells = new Vector2[points.Length];
                    FitWindowUVs(mesh.triangles, points, normals, uv, cells);
                    mesh.uv2 = cells;
                    panes++;
                }
                else
                {
                    for (int i = 0; i < uv.Length; i++)
                    {
                        var p = points[i];
                        var n = normals[i];
                        uv[i] = (Mathf.Abs(n.y) > .70f ? new Vector2(p.x,p.z) :
                            new Vector2(Vector3.Dot(p, Vector3.Cross(n,Vector3.up).normalized),p.y)) / 4.32f;
                    }
                    walls++;
                }
                mesh.uv = uv;
                if (!window) mesh.RecalculateTangents();
                filter.sharedMesh = mesh;
                r.sharedMaterial = window ? glass : stone;
            }
            Debug.Log($"Harbour estates fitted {panes} window batches and surfaced {walls} masonry batches");
        }

        // The source builder's glass rectangles have four vertices, or six after spatial batching.
        // Each right triangle contains both limits on both axes, even when the batch splits its pair.
        static void FitWindowUVs(int[] indices, Vector3[] points, Vector3[] normals, Vector2[] uv, Vector2[] cells)
        {
            for (int t = 0; t < indices.Length; t += 3)
            {
                var i = indices[t]; var j = indices[t + 1]; var k = indices[t + 2];
                var along = Vector3.Cross(normals[i], Vector3.up).normalized;
                float a = Vector3.Dot(points[i],along), b = Vector3.Dot(points[j],along), c = Vector3.Dot(points[k],along);
                float left = Mathf.Min(a,Mathf.Min(b,c)), right = Mathf.Max(a,Mathf.Max(b,c));
                float sill = Mathf.Min(points[i].y,Mathf.Min(points[j].y,points[k].y));
                float head = Mathf.Max(points[i].y,Mathf.Max(points[j].y,points[k].y));
                float width = Mathf.Max(.01f,right - left), height = Mathf.Max(.01f,head - sill);
                int seed = Mathf.RoundToInt((left + right)*8f)*73856093 ^ Mathf.RoundToInt((sill + head)*8f)*19349663;
                seed ^= seed >> 13;
                var cell = new Vector2(seed & 3, width / height);
                for (int v = t; v < t + 3; v++)
                {
                    int id = indices[v];
                    uv[id] = new Vector2((Vector3.Dot(points[id],along)-left)/width,(points[id].y-sill)/height);
                    cells[id] = cell;
                }
            }
        }
    }
}
