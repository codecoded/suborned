using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Suborned.World.Play
{
    static class DistrictCourtModel
    {
        const string Materials = "Assets/Suborned/Materials/Play/";
        const string Kit = "Assets/Suborned/Kit/Prefabs/Built/";
        static Texture2D Load(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(Materials+file)
            ?? throw new BuildFailedException("Missing Lantern Court finish "+file);

        static object CourtPlan(string id)
        {
            var file = Path.Combine(HarbourWorldBuilder.RepoRoot, "contracts/world/harbour-district.plan.json");
            return J.A(Json.Parse(File.ReadAllText(file)), "megastructures").Single(m => "mega/" + J.S(m, "id") == id);
        }

        public static Vector3 BridgeSoffit(float streetHeight)
        {
            var bridge = J.Get(CourtPlan("mega/lantern-terraces"), "bridge");
            var rect = J.A(bridge, "rect").Select(v => (float)(double)v).ToArray();
            return new Vector3((rect[0] + rect[1]) * .5f, streetHeight + J.F(bridge, "base"),
                (rect[2] + rect[3]) * .5f + Suborned.OldTown.OldTownFrame.DZ);
        }

        public static void PlantBalconies(Transform parent, float streetHeight)
        {
            var variants = new List<GameObject>();
            foreach (var name in new[] { "2_A", "4_B", "2_C" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "DK_PlanterRun_" + name + "_TB.prefab");
                if (!prefab) throw new BuildFailedException("Missing testbed balcony planting " + name);
                variants.Add(prefab);
            }
            foreach (var wing in J.A(CourtPlan("mega/lantern-terraces"), "wings"))
            {
                var id = J.S(wing, "id");
                if (id != "north" && id != "west" && id != "bow") continue;
                var r = J.A(wing, "rect").Select(n => (float)(double)n).ToArray();
                bool horizontal = id == "north";
                float centre = horizontal ? (r[0] + r[1]) * .5f : (r[2] + r[3]) * .5f;
                float face = horizontal ? r[2] : (id == "bow" ? r[0] : r[1]);
                float outward = id == "west" ? 1 : -1;
                // Ground-storey balconies project 1.18 m and the next storey's ledge projects .94 m.
                // Keep the deep planting behind the outer guard rail, in the straight run clear of the corner.
                var bays = horizontal ? new[] { -4.8f, .4f, 4.8f } : new[] { -8.6f, -3.8f, 3.6f, 8.5f };
                for (int i = 0; i < bays.Length; i++)
                {
                    Plant(centre + bays[i], face + .58f * outward, 3.86f, i % 3);
                    if ((i + (horizontal ? 1 : 0)) % 2 == 0)
                        Plant(centre + bays[i] + .6f, face + .46f * outward, 6.93f, (i + 2) % 3);
                }
                void Plant(float along, float edge, float level, int variant)
                {
                    var box = Object.Instantiate(variants[variant], parent);
                    box.name = "Lantern " + id + " • resident's window box";
                    var position = new Vector3(horizontal ? along : edge, streetHeight + level,
                        (horizontal ? edge : along) + Suborned.OldTown.OldTownFrame.DZ);
                    box.transform.SetPositionAndRotation(position, Quaternion.Euler(0, horizontal ? 0 : 90, 0));
                }
            }
        }

        public static void ShadePalms(UnityEngine.SceneManagement.Scene scene, Vector3 court)
        {
            var neighbourhood = new Bounds(court, new Vector3(150, 45, 145));
            var finishes = new Dictionary<Material, Material>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!neighbourhood.Intersects(renderer.bounds)) continue;
                var slots = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    var original = slots[i];
                    if (!original || (original.name != "DK_Leaf_PalmFan" && original.name != "DK_Leaf_PalmFeather")) continue;
                    if (!finishes.TryGetValue(original, out var finish))
                    {
                        finish = new Material(original) { name = original.name + " • Lantern Court" };
                        finish.SetColor("_BaseColor", new Color(.57f, .66f, .62f));
                        finish.SetFloat("_Smoothness", .24f);
                        finishes.Add(original, finish);
                    }
                    slots[i] = finish;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = slots;
            }
        }

        public static void Build(Transform root, MegaDto block, MassDto plot)
        {
            var pedestal = block.Parts.Single(p => p.Id.EndsWith("/bed/0", System.StringComparison.Ordinal));
            var ring = pedestal.Ring;
            var at = new Vector3((ring.Min(p=>p.x)+ring.Max(p=>p.x))*.5f,pedestal.Base,(ring.Min(p=>p.y)+ring.Max(p=>p.y))*.5f);
            var mat = new Material(Shader.Find("DecoKit/Lit Cutaway")) { name="Lantern Court • ivory stone and celadon", enableInstancing=true };
            mat.SetTexture("_BaseMap",Load("CourtColour.jpg"));
            mat.SetTexture("_MetallicGlossMap",Load("CourtMetal.png")); mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Smoothness",.90f);
            mat.SetTexture("_EmissionMap",Load("CourtLights.png"));mat.SetColor("_EmissionColor",new Color(.66f,.72f,.61f));mat.EnableKeyword("_EMISSION");
            foreach(var name in new[]{"CourtWest","CourtCrescent","CourtMarket","MeridianClock"})
            {
                var model = new DistrictFerryGardens.Kit(name,mat,.43f,.14f,.006f);
                var far = model.Place(root,at,Quaternion.identity);
                var near = far.GetComponentInParent<LODGroup>().GetLODs()[0].renderers[0];
                Combine.Kit(root,near.bounds,(int)near.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3);
            }
            Pave(root,plot,pedestal.Base);
            var estate = CourtPlan(block.Id);
            int i=0;
            foreach(var bed in J.A(estate,"planting"))
            {
                var p=J.P(bed,"at");
                var species=J.S(bed,"kit");
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Kit+species+".prefab");
                if(!prefab)throw new BuildFailedException("Lantern Court needs its planted "+species);
                var tree=Object.Instantiate(prefab,root);
                tree.name="Lantern Court • "+species;
                tree.transform.SetPositionAndRotation(new Vector3(p.x,pedestal.Base+.49f,p.y),Quaternion.Euler(0,49+i++*83,0));
                KitLods.Floor(tree.transform,KitLods.GuardM);
            }
        }

        static void Pave(Transform parent,MassDto plot,float height)
        {
            var stock=AssetDatabase.LoadAssetAtPath<Material>("Assets/Suborned/OldTownArt/DecoKit/Generated/DK_Pave_Flags_Metric.mat");
            if(!stock)throw new BuildFailedException("Lantern Court needs its flagstone paving");
            var mat=new Material(stock){name="Lantern Court • honed flags"};
            mat.SetColor("_BaseColor",new Color(.70f,.73f,.68f));
            var points=plot.Ring.Select(p=>new Vector3(p.x,height+.012f,p.y)).ToArray();
            var slab=new MeshKit();
            for(int i=0;i<points.Length;i++)slab.Vert(points[i],plot.Ring[i],Vector3.up);
            for(int i=1;i<points.Length-1;i++)
            {
                int a=i,b=i+1;
                if(Vector3.Cross(points[a]-points[0],points[b]-points[0]).y<0) (a,b)=(b,a);
                slab.Tri(0,a,b);
            }
            Combine.Add(parent,"megastructures",slab,mat);
        }
    }
}
