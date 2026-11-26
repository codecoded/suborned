using System.Collections.Generic;
using System.Linq;
using Suborned.Play;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Suborned.World.Play
{
    static partial class DistrictFerryGardens
    {
        static int ResidentialGardens(Scene scene, Transform root, SiteFrame frame, DistrictGround ground, Material paint, Quaternion heading)
        {
            var palm=new Kit("TerracePlanter",paint,.26f,.105f,.031f);
            var flowers=new Kit("TerraceBorder",paint,.26f,.105f,.031f);
            var bay=new Kit("TerraceBay",paint,.30f,.115f,.019f);
            var oldGardens=new List<Bounds>();
            int beds=0;
            foreach(var roof in ground.RoofTerraces)
            {
                var sw=frame.World(roof.A); sw.y=roof.HeightCm*.01f;
                var ne=frame.World(roof.B); ne.y=sw.y;
                float length=ne.x-sw.x;
                int bays=Mathf.Max(1,Mathf.RoundToInt(length/8f));
                int row=Mathf.RoundToInt(sw.y/3.6f);
                for(int i=0;i<bays;i++)
                {
                    var at=sw+Vector3.right*((i+.5f)*length/bays);
                    bay.Place(root,at+Vector3.forward*.09f,heading,length/(8*bays));
                    // Interleave flowering terraces, occasional tall palms and open sun decks.
                    if((i+row)%6==4 || length/bays<7.4f) continue;
                    var kit=(i+2*row)%4==0 ? palm : flowers;
                    kit.Place(root,at+Vector3.forward*(1.02f+(i+row)%3*.22f),heading);
                    beds++;
                }
                // The earlier roof programme has a 3 m crown centred 1.2 m inside the parapet.
                var box=new Bounds();box.SetMinMax(sw+new Vector3(-.45f,-.06f,-.48f),ne+new Vector3(.45f,5.56f,.48f));
                oldGardens.Add(box);
            }
            RemoveOldPlanting(scene,oldGardens);
            return beds;
        }

        static void RemoveOldPlanting(Scene scene, List<Bounds> gardens)
        {
            // The flyby folded tubs, poles, spheres and beds into multi-building roof chunks.
            // Filter only the roof programmes inside the surveyed terraces, retaining neighbouring roofs and their UVs.
            int removed=0;
            foreach(var root in scene.GetRootGameObjects())
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var source=filter.sharedMesh;
                if(!source || !source.name.StartsWith("roofs/facade") || !filter.TryGetComponent<MeshRenderer>(out var renderer)) continue;
                var inside=gardens.Where(g=>g.Intersects(renderer.bounds)).ToArray();
                if(inside.Length==0) continue;
                var m=filter.transform.localToWorldMatrix;
                var vertices=source.vertices.Select(v=>m.MultiplyPoint3x4(v)).ToArray();
                var indices=new List<int>[source.subMeshCount];
                int lost=0, kept=0;
                for(int sub=0;sub<indices.Length;sub++)
                {
                    var old=source.GetTriangles(sub);
                    var list=indices[sub]=new List<int>(old.Length);
                    for(int i=0;i<old.Length;i+=3)
                    {
                        var centre=(vertices[old[i]]+vertices[old[i+1]]+vertices[old[i+2]])/3f;
                        if(System.Array.Exists(inside,g=>g.Contains(centre))) {lost++;continue;}
                        list.Add(old[i]);list.Add(old[i+1]);list.Add(old[i+2]);kept++;
                    }
                }
                if(lost==0) continue;
                removed+=lost;
                if(kept==0) {renderer.enabled=false;filter.sharedMesh=null;continue;}
                var replacement=Object.Instantiate(source);replacement.name=source.name+" with planted terraces";
                for(int sub=0;sub<indices.Length;sub++) replacement.SetTriangles(indices[sub],sub,false);
                replacement.RecalculateBounds();filter.sharedMesh=replacement;
            }
            Debug.Log($"Harbour roofs replaced {removed} baked stand-in planting triangles across {gardens.Count} housing terraces");
        }
    }
}
