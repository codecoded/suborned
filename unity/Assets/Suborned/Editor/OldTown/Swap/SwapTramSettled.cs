#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Suborned.OldTown.Editor.DecoKit;
using Suborned.World;
using Object=UnityEngine.Object;
namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>One explicitly invoked settled correction, same-reader comparison and targeted capture.
    /// No world/build or gameplay batch. Original scene and imported/collider meshes are read-only.</summary>
    internal static class SwapTramSettled
    {
        const string Evidence="briefs/075/recovery/tram-settled";
        static string Repo=>Path.GetFullPath(Path.Combine(Application.dataPath,"..",".."));
        static string Out(string name)=>Path.Combine(Repo,Evidence,name);
        static string Hash(byte[] bytes){using var sha=SHA256.Create();return string.Concat(sha.ComputeHash(bytes).Select(b=>b.ToString("x2")));}
        static string MeshHash(Mesh mesh)
        {
            if(!mesh)return "none";using var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh);var md=data[0];
            using var hash=SHA256.Create();
            void Add(byte[] a)=>hash.TransformBlock(a,0,a.Length,null,0);
            for(int stream=0;stream<md.vertexBufferCount;stream++)Add(md.GetVertexData<byte>(stream).ToArray());
            Add(md.GetIndexData<byte>().ToArray());
            for(int sub=0;sub<md.subMeshCount;sub++)Add(System.Text.Encoding.UTF8.GetBytes(md.GetSubMesh(sub).ToString()));
            hash.TransformFinalBlock(Array.Empty<byte>(),0,0);return BitConverter.ToString(hash.Hash).Replace("-","");
        }
        sealed class Snapshot
        {
            public Dictionary<Mesh,string> Sources=new();public string[] Transforms,Colliders,Renderers,Materials;public string Nav;
            public Dictionary<MeshFilter,Mesh> Filters=new();
            public Snapshot()
            {
                var ts=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
                Transforms=ts.Select(t=>t.GetEntityId()+"|"+t.localToWorldMatrix.ToString("F9")+"|"+t.localPosition.ToString("F9")+"|"+t.localRotation.ToString("F9")+"|"+t.localScale.ToString("F9")+"|"+t.gameObject.activeSelf).OrderBy(x=>x).ToArray();
                Colliders=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Select(c=>c.GetEntityId()+"|"+EditorJsonUtility.ToJson(c)).OrderBy(x=>x).ToArray();
                var rs=Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);
                Renderers=rs.Select(r=>r.GetEntityId()+"|"+EditorJsonUtility.ToJson(r)).OrderBy(x=>x).ToArray();
                Materials=rs.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Select(m=>m.GetEntityId()+"|"+EditorJsonUtility.ToJson(m)).OrderBy(x=>x).ToArray();
                foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include))if(mf.sharedMesh)Filters.Add(mf,mf.sharedMesh);
                foreach(var mesh in Filters.Values.Concat(Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include).Select(c=>c.sharedMesh)).Where(m=>m).Distinct())Sources.Add(mesh,MeshHash(mesh));
                var host=Object.FindObjectsByType<global::Suborned.Play.HandoverHost>(FindObjectsInactive.Include).FirstOrDefault();
                Nav=host&&host.frame!=null?Hash(System.Text.Encoding.UTF8.GetBytes(string.Join(",",host.frame.heights)+"|"+Convert.ToBase64String(host.frame.kinds??Array.Empty<byte>()))):"none";
            }
            public object Compare(Snapshot a)
            {
                var mutations=Sources.Where(p=>MeshHash(p.Key)!=p.Value).Select(p=>p.Key.name).ToArray();
                var changed=Filters.Where(p=>p.Key&&a.Filters.TryGetValue(p.Key,out var m)&&m!=p.Value).Select(p=>(object)new Dictionary<string,object>{{"object",p.Key.name},{"before",p.Value.name},{"after",p.Key.sharedMesh.name}}).ToList();
                return new Dictionary<string,object>{{"transformsIdentical",Transforms.SequenceEqual(a.Transforms)},{"collidersIdentical",Colliders.SequenceEqual(a.Colliders)},{"rendererStateIdentical",Renderers.SequenceEqual(a.Renderers)},{"materialStateIdentical",Materials.SequenceEqual(a.Materials)},{"navIdentical",Nav==a.Nav},{"sourceMeshMutations",mutations},{"sourceMeshCount",Sources.Count},{"colliderCount",Colliders.Length},{"transformCount",Transforms.Length},{"changedRenderMeshes",changed}};
            }
        }
        public static void Run()=>SwapTramCorrected.FinalizeCandidate();
        static void Body()
        {
            var geometry=SwapTramSource.Load(Repo,"out/world/harbour-district.world.json");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-swap.unity",OpenSceneMode.Single);
            var world=WorldReader.Load(Path.Combine(Repo,"out/world/harbour-district.world.json"));var rep=new OldTownSwap.Report();
            // Warm camera/settings before preservation snapshot. No scene save of capture poses.
            var cam=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Single(c=>c.name=="DK street study camera");
            var cameraMatrix=cam.transform.localToWorldMatrix;var cameraPosition=cam.transform.position;var cameraRotation=cam.transform.rotation;
            var before=new Snapshot();
            SwapTramAudit.Run(rep,world,Out("audit-before.json"));
            Capture(cam,"before",false);cam.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
            var root=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Single(t=>t.name==SwapNetwork.RailsRoot);
            SwapTramRender.Apply(root,geometry,rep);
            SwapNetwork.ClearImportedGround(rep);
            SwapTramInlays.Apply(rep);
            SwapTramAudit.Run(rep,world,Out("audit-after.json"));
            SwapTramProof.Run(Out("mesh-proof.json"));
            var after=new Snapshot();File.WriteAllText(Out("preservation.json"),SwapExport.Json(before.Compare(after)));
            const string assetDir="Assets/Suborned/OldTownArt/Swap/TramSettled";Directory.CreateDirectory(assetDir);int k=0;
            foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include))if(mf.sharedMesh&&!AssetDatabase.Contains(mf.sharedMesh))
            {
                if(mf.sharedMesh.name.Contains("(source tram join)")||mf.sharedMesh.name.Contains("(tram surface cut)")||mf.sharedMesh.name.Contains("(tram signal ownership)"))
                    AssetDatabase.CreateAsset(mf.sharedMesh,$"{assetDir}/surface-{k++}.asset");
            }
            AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(scene,"Assets/Scenes/probe-recovery075-tramsettled.unity"))throw new IOException("tram settled save");
            Capture(cam,"after",true);cam.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
            File.WriteAllText(Out("complete.json"),SwapExport.Json(new Dictionary<string,object>{{"generatedMeshes",k},{"status","reviewable candidate; HOLD until independent review"},{"geometryTiles",geometry.Length}}));
            Debug.Log("[TramSettled] correction, proof and targeted capture complete; HOLD");
        }
        internal static void Capture(Camera cam,string tag,bool motion,string destination=null)
        {
            var focuses=new (string id,Vector2 at)[]{("west-cut",new(-340,-130)),("crossing-west",new(-340,-190)),("crossing-south",new(-2,-389)),("curve-north",new(-346,272)),("curve-south",new(-332,-403))};
            cam.enabled=true;cam.orthographic=false;cam.fieldOfView=55;cam.nearClipPlane=.05f;cam.farClipPlane=2500;cam.useOcclusionCulling=false;
            Object.DestroyImmediate(DecoKitStage.Render(cam,640,360));
            foreach(var view in focuses)
            {
                Vector3 target=OldTownSwap.Old3(view.at,0);
                void Pose(float t){cam.transform.position=target+new Vector3(-5+t*10,2.8f,-7);cam.transform.LookAt(target+new Vector3(0,0,t*4-2),Vector3.up);}
                Pose(.5f);string folder=destination??Out(tag);Directory.CreateDirectory(folder);
                DecoKitStage.WithShadows(120,()=>DecoKitStage.Capture(cam,1920,1080,Path.Combine(folder,view.id+".png"),2));
                if(motion){string frames=Path.Combine(folder,view.id+"-motion");Directory.CreateDirectory(frames);for(int i=0;i<36;i++){Pose(i/35f);DecoKitStage.WithShadows(120,()=>DecoKitStage.Capture(cam,1280,720,Path.Combine(frames,$"{i:0000}.png"),1));}}
            }
        }
    }
}
#endif
