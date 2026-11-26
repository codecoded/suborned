using System.Collections.Generic;
using System.Linq;
using Suborned.Play;
using Suborned.Sim;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Suborned.World.Play
{
    sealed partial class DistrictStreetArchitecture
    {
        const string InteriorAtlas = "Assets/Suborned/Materials/Play/HarbourInteriors.jpg";

        Material ShopGlass(int shop)
        {
            string key = "Harbour shop glass " + shop;
            if (palette.TryGetValue(key, out var glass)) return glass;
            var shader = Shader.Find("Suborned/Harbour/Shop windows");
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(InteriorAtlas);
            if (!shader || !atlas) throw new BuildFailedException("Missing Harbour shop windows or their interior atlas");
            glass = new Material(shader) { name = key, enableInstancing = true };
            glass.SetTexture("_InteriorAtlas", atlas);
            glass.SetFloat("_Shop", shop);
            palette.Add(key, glass);
            return glass;
        }

        // The flyby FBXs share meshes and keep their original pivots, UVs, submeshes and painted material variants.
        // Add only index levels on a copy in the processed scene. The standalone player chooses these on the GPU.
        public void DressTown(Scene scene, SiteFrame frame, DistrictGround ground)
        {
            const string models = "Assets/Suborned/OldTownArt/DecoKit/Models/";
            var prepared = new Dictionary<Mesh, Mesh>();
            var frontages = ground.Courts.Where(c => c.Style == "homes" || c.Style == "market")
                .Select(c => c.Ring.Select(p => frame.World(p)).Aggregate(Vector3.zero, (sum, p) => sum + p) / c.Ring.Length).ToArray();
            int filters = 0, shopfronts = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var f in root.GetComponentsInChildren<MeshFilter>())
            {
                var original = f.sharedMesh;
                if (!original || !f.TryGetComponent<MeshRenderer>(out var renderer) || !renderer.enabled) continue;
                if (!prepared.TryGetValue(original, out var mesh))
                {
                    string path = AssetDatabase.GetAssetPath(original);
                    mesh = original;
                    if (original.lodCount == 1 && path.StartsWith(models, System.StringComparison.Ordinal) &&
                        !path.Contains("/Figures/") && !path.Contains("/Vehicles/") && !path.Contains("/Robots/") &&
                        original.vertexCount >= 450)
                    {
                        mesh = Object.Instantiate(original);
                        mesh.name = original.name;
                        MeshLodUtility.GenerateMeshLods(mesh, 5);
                    }
                    prepared.Add(original, mesh);
                }
                if (mesh != original && !f.GetComponentInParent<LODGroup>()) { f.sharedMesh = mesh; filters++; }

                // The lower display backs are a separate slot in the original Streamline modules. Their upper ribbon
                // glass, stone, ornamental corners and the flyby's parade retain their authored materials.
                if (!original.name.StartsWith("DK_Stream") && !original.name.StartsWith("DK_BalconyBay") &&
                    !original.name.StartsWith("DK_PortholeBay")) continue;
                var b = renderer.bounds;
                if (!frontages.Any(p => b.SqrDistance(p) < 17 * 17)) continue;
                var slots = renderer.sharedMaterials;
                int lobby = System.Array.FindIndex(slots, m => m && m.name == "DK_Lobby");
                if (lobby < 0) continue;
                slots[lobby] = ShopGlass((Mathf.FloorToInt(b.center.x + b.center.z) % 4 + 4) % 4);
                renderer.sharedMaterials = slots;
                shopfronts++;
            }
            Debug.Log($"Harbour streets: distance index levels on {filters} flyby renderers, occupied display backs on {shopfronts} neighbourhood modules");
        }
    }
}
