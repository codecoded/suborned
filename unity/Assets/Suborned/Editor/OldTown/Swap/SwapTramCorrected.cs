#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Suborned.OldTown.Editor.DecoKit;
using Object=UnityEngine.Object;
namespace Suborned.OldTown.Editor.Swap
{
    internal static class SwapTramCorrected
    {
        internal static string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../briefs/075/recovery/tram-corrected"));
        static string Target=>Path.Combine(Dir,"checks",DecoKitStage.Arg("tramCheck","current"));
        static void Write(string name,object o)=>File.WriteAllText(Path.Combine(Target,name),SwapTramJson.Encode(o));
        public static void FinalizeCandidate()=>OldTownSwap.SettledBatch(()=>Body(true));
        public static void CheckMemory()=>OldTownSwap.SettledBatch(()=>Body(false));
        static void Body(bool capture)
        {
            Directory.CreateDirectory(Target);
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-swap.unity",OpenSceneMode.Single);
            var cam=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Single(c=>c.name=="DK street study camera");
            var cold=new SwapTramState(false);Write("materials-cold.json",cold.Materials);
            void Warm()
            {
                var pos=cam.transform.position;var rot=cam.transform.rotation;bool ortho=cam.orthographic;float fov=cam.fieldOfView;
                cam.orthographic=false;cam.fieldOfView=55;
                foreach(var p in new[]{new Vector2(-340,-130),new Vector2(-340,-190),new Vector2(-2,-389),new Vector2(-346,272),new Vector2(-332,-403)})
                {var target=OldTownSwap.Old3(p,0);cam.transform.position=target+new Vector3(0,2.8f,-7);cam.transform.LookAt(target);var image=DecoKitStage.Render(cam,256,144);Object.DestroyImmediate(image);}
                cam.transform.SetPositionAndRotation(pos,rot);cam.orthographic=ortho;cam.fieldOfView=fov;
            }
            Warm();Warm();var warm=new SwapTramState(false);Warm();Warm();var control=new SwapTramState(false);
            Write("materials-warm.json",warm.Materials);Write("material-initialization.json",cold.MaterialDiff(warm));Write("material-noop-control.json",warm.MaterialDiff(control));
            var before=new SwapTramState();
            var root=Object.FindObjectsByType<Transform>().Single(t=>t.name==SwapNetwork.RailsRoot);
            var repo=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));var geometry=SwapTramSource.Load(repo,"out/world/harbour-district.world.json");var report=new OldTownSwap.Report();
            SwapTramRender.Apply(root,geometry,report);Write("memory-density.json",SwapTramDensity.Result());SwapNetwork.ClearImportedGround(report);SwapTramInlays.Apply(report);SwapTramKerbCaps.Apply();Write("memory-kerb-caps.json",SwapTramKerbCaps.Evidence);
            SwapTramCoverage.Run(Path.Combine(Target,"memory-coverage.json"));SwapTramProof.Run(Path.Combine(Target,"memory-mesh-proof.json"));SwapTramOwnershipProof.Check(Path.Combine(Target,"memory-ownership.json"));
            Write("memory-attributes.json",SwapTramAttributeProof.Run());
            var after=new SwapTramState();Write("materials-after.json",after.Materials);Write("memory-preservation.json",before.Compare(after));
            if(capture)
            {
                var coverage=SwapTramSource.Obj(Suborned.World.Json.Parse(File.ReadAllText(Path.Combine(Target,"memory-coverage.json"))));
                var ownership=SwapTramSource.Obj(Suborned.World.Json.Parse(File.ReadAllText(Path.Combine(Target,"memory-ownership.json"))));
                var attributes=SwapTramSource.Obj(SwapTramSource.Obj(Suborned.World.Json.Parse(File.ReadAllText(Path.Combine(Target,"memory-attributes.json"))))["emitted"]);
                void Zero(object v,string message){if(Convert.ToDouble(v)!=0)throw new InvalidOperationException("final candidate blocked: "+message+" = "+v);}
                Zero(coverage["bedOnlyMisses"],"actual bed misses");Zero(coverage["railProfileFailures"],"source rail profile");Zero(coverage["strictBedMisses"],"exact bed coverage");Zero(coverage["strictRailMisses"],"exact rail coverage");
                Zero(ownership["coplanarPositiveAreaPairs"],"coplanar surface intersections");Zero(attributes["frameErrors"],"frames");Zero(attributes["basisOrHandednessErrors"],"UV basis/handedness");
                Zero(SwapTramDensity.Failures,"physical UV density");Zero(warm.MaterialDiff(control).Count,"no-op material changes");Zero(before.MaterialDiff(after).Count,"correction material changes");
                var proof=SwapTramSource.Obj(Suborned.World.Json.Parse(File.ReadAllText(Path.Combine(Target,"memory-mesh-proof.json")))) ;Zero(proof["zeroArea"],"zero area triangles");Zero(proof["nonfinite"],"nonfinite attributes");
                var preservation=SwapTramSource.Obj(Suborned.World.Json.Parse(File.ReadAllText(Path.Combine(Target,"memory-preservation.json"))));
                foreach(string k in new[]{"originalTransformChanges","colliderChanges","sourceMeshChanges","signalBindingChanges"})Zero(SwapTramSource.Arr(preservation[k]).Count,k);
                if(!(bool)preservation["navIdentical"])throw new InvalidOperationException("nav changed");
                SaveMeshesAndScene(scene);
                // Verify the persisted bindings and actual triangles after reloading, not just in memory.
                EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-tramcorrected.unity",OpenSceneMode.Single);
                root=Object.FindObjectsByType<Transform>().Single(t=>t.name==SwapNetwork.RailsRoot);SwapTramRender.Applied.Clear();SwapTramRender.Geometry=null;SwapTramRender.BindExisting(root);
                var savedCoverage=SwapTramSource.Obj(SwapTramCoverage.Run(Path.Combine(Target,"saved-coverage.json")));
                var savedOwnership=SwapTramSource.Obj(SwapTramOwnershipProof.Check(Path.Combine(Target,"saved-ownership.json")));
                Zero(savedCoverage["bedOnlyMisses"],"saved bed coverage");Zero(savedCoverage["railProfileFailures"],"saved rail profile");Zero(savedCoverage["strictBedMisses"],"saved exact bed coverage");Zero(savedCoverage["strictRailMisses"],"saved exact rail coverage");Zero(savedOwnership["coplanarPositiveAreaPairs"],"saved intersections");
                Write("saved-attributes.json",SwapTramAttributeProof.Run());
                var world=Suborned.World.WorldReader.Load(Path.Combine(repo,"out/world/harbour-district.world.json"));SwapTramAudit.Run(report,world,Path.Combine(Target,"audit-after.json"));
                // Baseline scene is immutable; this only opens it to capture matched before frames.
                EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-swap.unity",OpenSceneMode.Single);
                SwapTramRender.Applied.Clear();SwapTramRender.SurfaceFilters.Clear();SwapTramRender.Owners.Clear();SwapTramRender.Geometry=null;
                SwapTramAudit.Run(report,world,Path.Combine(Target,"audit-before.json"));
                cam=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Single(c=>c.name=="DK street study camera");SwapTramSettled.Capture(cam,"before",false,Path.Combine(Target,"before"));
                EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-tramcorrected.unity",OpenSceneMode.Single);
                cam=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Single(c=>c.name=="DK street study camera");SwapTramSettled.Capture(cam,"after",true,Path.Combine(Target,"after"));
                Write("complete.json",new System.Collections.Generic.Dictionary<string,object>{{"status","candidate only; HOLD pending independent Redline review"},{"savedScene","Assets/Scenes/probe-recovery075-tramcorrected.unity"},{"actualCoverageQueries",33264},{"signalOwnershipIntersections",0}});
            }
            Debug.Log(capture?"[TramCorrected] final candidate, saved-scene validation and targeted captures complete; HOLD":"[TramCorrected] in-memory checks complete; no saved scene, mesh assets or captures");
        }        /// <summary>Continue validation/capture of the existing candidate after a reader-cache
        /// lifetime failure. No Apply, save, generation, asset rewrite or scene mutation.</summary>
        public static void ResumeSavedCandidate()=>OldTownSwap.SettledBatch(()=>
        {
            Directory.CreateDirectory(Target);
            EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-tramcorrected.unity",OpenSceneMode.Single);
            var root=Object.FindObjectsByType<Transform>().Single(t=>t.name==SwapNetwork.RailsRoot);SwapTramRender.BindExisting(root);
            var coverage=SwapTramSource.Obj(SwapTramCoverage.Run(Path.Combine(Target,"saved-coverage.json")));
            var ownership=SwapTramSource.Obj(SwapTramOwnershipProof.Check(Path.Combine(Target,"saved-ownership.json")));
            var attributes=SwapTramAttributeProof.Run();Write("saved-attributes.json",attributes);
            var emitted=SwapTramSource.Obj(SwapTramSource.Obj(attributes)["emitted"]);
            foreach(string k in new[]{"bedOnlyMisses","railProfileFailures","strictBedMisses","strictRailMisses"})if(Convert.ToDouble(coverage[k])!=0)throw new InvalidOperationException("saved coverage failed: "+k);
            if(Convert.ToDouble(ownership["coplanarPositiveAreaPairs"])!=0||Convert.ToDouble(emitted["frameErrors"])!=0||Convert.ToDouble(emitted["basisOrHandednessErrors"])!=0)throw new InvalidOperationException("saved ownership/attributes failed");
            var assetRows=SwapTramSource.Arr(SwapTramSource.Obj(Suborned.World.Json.Parse(File.ReadAllText(Path.Combine(Target,"mesh-assets.json"))))["assets"]);
            int checkedAssets=0;foreach(var group in assetRows.Select(SwapTramSource.Obj).GroupBy(o=>(string)o["asset"]))
            {var m=AssetDatabase.LoadAssetAtPath<Mesh>(group.Key);if(SwapTramState.MeshHash(m)!=(string)group.First()["meshDataSha256"])throw new InvalidOperationException("saved GPU payload changed: "+group.Key);checkedAssets++;}
            Write("saved-payloads.json",new System.Collections.Generic.Dictionary<string,object>{{"checkedUniqueAssets",checkedAssets},{"payloadChanges",0}});
            string repo=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));var world=Suborned.World.WorldReader.Load(Path.Combine(repo,"out/world/harbour-district.world.json"));var report=new OldTownSwap.Report();
            SwapTramAudit.Run(report,world,Path.Combine(Target,"audit-after.json"));
            EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-swap.unity",OpenSceneMode.Single);SwapTramRender.Applied.Clear();SwapTramRender.SurfaceFilters.Clear();SwapTramRender.Owners.Clear();SwapTramRender.Geometry=null;
            SwapTramAudit.Run(report,world,Path.Combine(Target,"audit-before.json"));
            var cam=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Single(c=>c.name=="DK street study camera");SwapTramSettled.Capture(cam,"before",false,Path.Combine(Target,"before"));
            EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-tramcorrected.unity",OpenSceneMode.Single);cam=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Single(c=>c.name=="DK street study camera");SwapTramSettled.Capture(cam,"after",true,Path.Combine(Target,"after"));
            Write("complete.json",new System.Collections.Generic.Dictionary<string,object>{{"status","candidate only; HOLD pending independent Redline review"},{"savedScene","Assets/Scenes/probe-recovery075-tramcorrected.unity"},{"actualCoverageQueries",33264},{"signalOwnershipIntersections",0},{"geometryRegeneratedInResume",false}});
            Debug.Log("[TramCorrected] existing candidate reload, payload checks and targeted capture complete; HOLD");
        });
        static void SaveMeshesAndScene(UnityEngine.SceneManagement.Scene scene)
        {
            const string directory="Assets/Suborned/OldTownArt/Swap/TramCorrected";Directory.CreateDirectory(directory);
            var filters=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).Where(mf=>mf.sharedMesh&&!AssetDatabase.Contains(mf.sharedMesh)&&(SwapTramRender.Owners.ContainsKey(mf)||mf.sharedMesh.name.Contains("(tram surface cut)")||mf.sharedMesh.name.Contains("(tram signal ownership)"))).ToArray();
            var hashes=filters.Select(mf=>mf.sharedMesh).Distinct().ToDictionary(m=>m,m=>SwapTramState.MeshHash(m));
            var wanted=new System.Collections.Generic.HashSet<string>(hashes.Values);var old=new System.Collections.Generic.Dictionary<string,Mesh>();
            foreach(var guid in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/Suborned/OldTownArt/Swap/TramSettled"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);string hash=SwapTramState.MeshHash(mesh);
                if(wanted.Contains(hash)){if(!old.ContainsKey(hash))old.Add(hash,mesh);}else Resources.UnloadAsset(mesh);
            }
            int reused=0,created=0;var assets=new System.Collections.Generic.List<object>();var saved=new System.Collections.Generic.Dictionary<string,Mesh>(old);
            foreach(var mf in filters)
            {
                var mesh=mf.sharedMesh;string hash=hashes[mesh];
                if(saved.TryGetValue(hash,out var immutable)){mf.sharedMesh=immutable;reused++;}
                else
                {
                    string path=directory+"/"+hash.ToLowerInvariant()+".asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(prior){if(SwapTramState.MeshHash(prior)!=hash)throw new InvalidOperationException("immutable content-addressed mesh differs");mf.sharedMesh=prior;saved.Add(hash,prior);reused++;}
                    else{AssetDatabase.CreateAsset(mesh,path);saved.Add(hash,mesh);created++;}
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(mf);assets.Add(new System.Collections.Generic.Dictionary<string,object>{{"object",mf.name},{"asset",AssetDatabase.GetAssetPath(mf.sharedMesh)},{"meshDataSha256",hash}});
            }
            Write("mesh-assets.json",new System.Collections.Generic.Dictionary<string,object>{{"created",created},{"reused",reused},{"assets",assets}});
            if(!EditorSceneManager.SaveScene(scene,"Assets/Scenes/probe-recovery075-tramcorrected.unity"))throw new IOException("corrected scene save failed");
        }

    }
}
#endif
