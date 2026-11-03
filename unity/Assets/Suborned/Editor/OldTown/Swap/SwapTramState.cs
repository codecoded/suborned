#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Suborned.OldTown.Editor.Swap
{
    internal sealed class SwapTramState
    {
        readonly Dictionary<Transform,string> transforms=new();readonly Dictionary<Collider,string> colliders=new();
        readonly Dictionary<Mesh,string> sources=new();readonly string nav;
        readonly Dictionary<global::Suborned.Play.SignalHead,string> signals=new();
        internal readonly Dictionary<string,Dictionary<string,object>> Materials=new();
        internal static string MeshHash(Mesh mesh)
        {
            using var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh);var md=data[0];using var sha=SHA256.Create();
            void Add(byte[] b)=>sha.TransformBlock(b,0,b.Length,null,0);
            Add(Encoding.UTF8.GetBytes(string.Join("|",mesh.GetVertexAttributes().Select(a=>$"{a.attribute}/{a.format}/{a.dimension}/{a.stream}"))+"|"+mesh.bounds.ToString("R")));
            for(int i=0;i<md.vertexBufferCount;i++)Add(md.GetVertexData<byte>(i).ToArray());Add(md.GetIndexData<byte>().ToArray());
            for(int i=0;i<md.subMeshCount;i++){var d=md.GetSubMesh(i);Add(Encoding.UTF8.GetBytes($"{d.indexStart}/{d.indexCount}/{d.baseVertex}/{d.firstVertex}/{d.vertexCount}/{d.topology}"));}
            sha.TransformFinalBlock(Array.Empty<byte>(),0,0);return BitConverter.ToString(sha.Hash).Replace("-","");
        }
        static string TransformState(Transform t)=>t.localPosition.ToString("R")+"|"+t.localRotation.ToString("R")+"|"+t.localScale.ToString("R")+"|"+t.gameObject.activeSelf;
        static string Nav()
        {
            var host=Object.FindObjectsByType<global::Suborned.Play.HandoverHost>(FindObjectsInactive.Include).FirstOrDefault();
            return host&&host.frame!=null?string.Join(",",host.frame.heights)+"|"+Convert.ToBase64String(host.frame.kinds??Array.Empty<byte>()):"none";
        }
        internal SwapTramState(bool full=true)
        {
            var rs=Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);
            foreach(var m in rs.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct())
            {
                var key=AssetDatabase.GetAssetPath(m)+"|"+m.name+"|"+m.GetEntityId();
                var props=new Dictionary<string,object>();Flatten(Suborned.World.Json.Parse(EditorJsonUtility.ToJson(m)),"",props);Materials.Add(key,props);
            }
            if(!full)return;
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))transforms.Add(t,TransformState(t));
            foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))colliders.Add(c,EditorJsonUtility.ToJson(c));
            var meshes=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).Select(mf=>mf.sharedMesh).Concat(Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include).Select(c=>c.sharedMesh)).Where(m=>m).Distinct();
            foreach(var m in meshes)sources.Add(m,MeshHash(m));foreach(var h in Object.FindObjectsByType<global::Suborned.Play.SignalHead>(FindObjectsInactive.Include))signals.Add(h,EditorJsonUtility.ToJson(h));nav=Nav();
        }
        static void Flatten(object value,string prefix,Dictionary<string,object> output)
        {
            if(value is Dictionary<string,object> dict){foreach(var pair in dict)Flatten(pair.Value,prefix+"/"+pair.Key,output);}
            else if(value is List<object> list){for(int i=0;i<list.Count;i++)Flatten(list[i],prefix+"["+i+"]",output);output[prefix+".length"]=list.Count;}
            else output[prefix]=value;
        }
        static object PropertyName(Dictionary<string,object> props,string path)
        {
            var match=System.Text.RegularExpressions.Regex.Match(path,@"^(.*\[\d+\])/second(?:/.*)?$");
            return match.Success&&props.TryGetValue(match.Groups[1].Value+"/first",out var name)?name:null;
        }
        internal List<object> MaterialDiff(SwapTramState after)
        {
            var diff=new List<object>();
            foreach(var key in Materials.Keys.Union(after.Materials.Keys).OrderBy(k=>k))
            {
                Materials.TryGetValue(key,out var a);after.Materials.TryGetValue(key,out var b);
                if(a==null||b==null){diff.Add(new Dictionary<string,object>{{"material",key},{"before",a},{"after",b}});continue;}
                foreach(var p in a.Keys.Union(b.Keys).OrderBy(k=>k)){a.TryGetValue(p,out var x);b.TryGetValue(p,out var y);if(!Equals(x,y))diff.Add(new Dictionary<string,object>{{"material",key},{"property",p},{"propertyName",PropertyName(a,p)},{"before",x},{"after",y}});}
            }return diff;
        }
        internal object Compare(SwapTramState after)=>new Dictionary<string,object>{{"originalTransforms",transforms.Count},{"originalTransformChanges",transforms.Where(p=>!p.Key||TransformState(p.Key)!=p.Value).Select(p=>p.Key?p.Key.name:"destroyed").ToArray()},{"colliders",colliders.Count},{"colliderChanges",colliders.Where(p=>!p.Key||EditorJsonUtility.ToJson(p.Key)!=p.Value).Select(p=>p.Key?p.Key.name:"destroyed").ToArray()},{"originalMeshes",sources.Count},{"sourceMeshChanges",sources.Where(p=>!p.Key||MeshHash(p.Key)!=p.Value).Select(p=>p.Key?p.Key.name:"destroyed").ToArray()},{"signalBindingChanges",signals.Where(p=>!p.Key||EditorJsonUtility.ToJson(p.Key)!=p.Value).Select(p=>p.Key?p.Key.name:"destroyed").ToArray()},{"navIdentical",nav==Nav()},{"materialPropertyDifferences",MaterialDiff(after)}};
    }
}
#endif
