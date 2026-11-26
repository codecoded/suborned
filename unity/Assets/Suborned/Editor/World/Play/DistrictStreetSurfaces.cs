using System.Collections.Generic;
using System.Linq;
using Suborned.Play;
using Suborned.Sim;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
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
        public void DressTown(Scene scene, SiteFrame frame, DistrictGround ground, Transform district)
        {
            const string models = "Assets/Suborned/OldTownArt/DecoKit/Models/";
            var prepared = new Dictionary<Mesh, Mesh>();
            var flatPaving = new List<MeshRenderer>();
            var civic = frame.World(new Pt(0, 0));
            var frontages = ground.Courts.Where(c => c.Style == "homes" || c.Style == "market")
                .Select(c => c.Ring.Select(p => frame.World(p)).Aggregate(Vector3.zero, (sum, p) => sum + p) / c.Ring.Length).ToArray();
            int filters = 0, shopfronts = 0, distantBays = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var f in root.GetComponentsInChildren<MeshFilter>())
            {
                var original = f.sharedMesh;
                if (!original || !f.TryGetComponent<MeshRenderer>(out var renderer) || !renderer.enabled) continue;
                var bounds = renderer.bounds;
                if (f.transform.parent && f.transform.parent.name.EndsWith("tiles merged", System.StringComparison.OrdinalIgnoreCase) &&
                    bounds.max.y <= .30f && bounds.size.y < .65f && bounds.SqrDistance(civic) > 75f * 75f &&
                    renderer.shadowCastingMode == ShadowCastingMode.On)
                {
                    // Street kerbs and flush rails receive the city's shadows. Their millimetre bevels
                    // have no meaningful silhouette in a directional shadow map.
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    flatPaving.Add(renderer);
                }
                if (original.lodCount > 1 && f.transform.parent && f.transform.parent.name == "Network tiles merged" &&
                    renderer.bounds.SqrDistance(frame.World(new Pt(0,0))) > 75f*75f)
                    renderer.meshLodSelectionBias = 1; // the fine bevels on repeated rail tiles outside the civic block
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
                bool ungrouped = mesh != original && !f.GetComponentInParent<LODGroup>();
                if (ungrouped) { f.sharedMesh = mesh; filters++; }

                // The lower display backs are a separate slot in the original Streamline modules. Their upper ribbon
                // glass, stone, ornamental corners and the flyby's parade retain their authored materials.
                if (!DecoratedBay(original.name)) continue;
                var b = renderer.bounds;
                if (frontages.Any(p => b.SqrDistance(p) < 17 * 17))
                {
                    var slots = renderer.sharedMaterials;
                    int lobby = System.Array.FindIndex(slots, m => m && m.name == "DK_Lobby");
                    if (lobby >= 0)
                    {
                        slots[lobby] = ShopGlass((Mathf.FloorToInt(b.center.x + b.center.z) % 4 + 4) % 4);
                        renderer.sharedMaterials = slots;
                        shopfronts++;
                    }
                }
                if (ungrouped) { DistantFront(renderer,mesh); distantBays++; }
            }
            FinishDistantFronts(district);
            if (flatPaving.Count > 0)
                scene.GetRootGameObjects().First(o => o.GetComponentInChildren<HandoverHost>(true))
                    .AddComponent<DistrictPavementShadows>().surfaces = flatPaving.ToArray();
            Debug.Log($"Harbour daylight: {flatPaving.Count} at-grade pavement chunks receive shadows without casting into the sun map");
            Debug.Log($"Harbour streets: distance index levels on {filters} flyby renderers, single-material distance fronts on {distantBays} bays, occupied display backs on {shopfronts} neighbourhood modules");
        }
    }
}
