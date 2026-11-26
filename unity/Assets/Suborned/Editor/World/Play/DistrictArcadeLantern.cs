using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Suborned.World.Play
{
    sealed partial class DistrictStreetArchitecture
    {
        void ArcadeLantern(Transform station, Vector3 hook)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/Scenery/Lantern.fbx");
            if (!source) throw new BuildFailedException("The Lantern Terraces pendant is missing");
            var lamp=Object.Instantiate(source,station);
            lamp.name="Opal orbital lantern";
            var tiers=lamp.GetComponentsInChildren<MeshRenderer>(true)
                .OrderByDescending(r=>r.GetComponent<MeshFilter>().sharedMesh.vertexCount).ToArray();
            if(tiers.Length!=3) throw new BuildFailedException("The arcade pendant requires three fitted detail levels");
            var opal=new Material(Colour("Enamel_Ivory",true)){name="Arcade opal glass"};
            opal.SetColor("_BaseColor",new Color(.92f,.83f,.66f));opal.SetFloat("_Smoothness",.56f);
            opal.EnableKeyword("_EMISSION");opal.SetColor("_EmissionColor",new Color(.62f,.44f,.24f));
            var bronze=new Material(Colour("Bronze",true)){name="Rolled champagne bronze"};
            bronze.SetColor("_BaseColor",new Color(.77f,.59f,.36f));bronze.SetFloat("_Smoothness",.76f);
            var finishes=new[]{opal,bronze,Colour("Enamel_Teal",true)};
            foreach(var r in tiers)
            {
                if(r.sharedMaterials.Length!=finishes.Length) throw new BuildFailedException("The arcade pendant material regions changed");
                var slots=r.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    var name=slots[i] ? slots[i].name : "";
                    slots[i]=name.StartsWith("A_") ? finishes[0] : name.StartsWith("B_") ? finishes[1] :
                        name.StartsWith("C_") ? finishes[2] : finishes[i];
                }
                r.sharedMaterials=slots;r.renderingLayerMask|=ArchitectureFade.Layer;
            }
            var group=lamp.GetComponent<LODGroup>() ?? lamp.AddComponent<LODGroup>();
            group.SetLODs(new[]{new LOD(.10f,new Renderer[]{tiers[0]}),new LOD(.032f,new Renderer[]{tiers[1]}),new LOD(.006f,new Renderer[]{tiers[2]})});
            group.RecalculateBounds();
            var bounds=tiers[0].bounds;
            lamp.transform.localScale*=2.35f/bounds.size.y;
            bounds=tiers[0].bounds;
            lamp.transform.position+=hook-new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);
        }
    }
}
