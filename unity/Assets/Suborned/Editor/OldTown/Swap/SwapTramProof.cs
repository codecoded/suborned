#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using G=Suborned.OldTown.Editor.Swap.SwapTramJoin;
using Object=UnityEngine.Object;
namespace Suborned.OldTown.Editor.Swap
{
    internal static class SwapTramProof
    {
        sealed class Face {public Vector3 A,B,C;public bool Metal;}
        internal static object Run(string path)
        {
            var grid=new Dictionary<Vector2Int,List<Face>>();long triangles=0,degenerate=0,nonfinite=0,attributes=0;double minN=double.MaxValue,maxN=0,maxNT=0;long railQueries=0,railMiss=0,bedQueries=0,bedMiss=0;
            var edits=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).Where(mf=>mf.sharedMesh&&(mf.sharedMesh.name.Contains("(source tram join)")||mf.sharedMesh.name.Contains("(tram surface cut)")||mf.sharedMesh.name.Contains("(tram signal ownership)"))).ToArray();
            foreach(var mf in edits)
            {
                var rd=SwapTramMeshRead.Read(mf.sharedMesh,mf.transform.localToWorldMatrix);var mats=mf.GetComponent<MeshRenderer>().sharedMaterials;
                bool tile=SwapTramRender.Owners.ContainsKey(mf)||SwapTramRender.Applied.Keys.Any(t=>mf.transform.IsChildOf(t));
                foreach(var v in rd.Verts)
                {
                    if(!Finite(v.P.x)||!Finite(v.P.y)||!Finite(v.P.z)||!Finite(v.N.x)||!Finite(v.N.y)||!Finite(v.N.z)||!Finite(v.T.x)||!Finite(v.T.y)||!Finite(v.T.z)||!Finite(v.T.w)||!Finite(v.U0.x)||!Finite(v.U0.y)||!Finite(v.U1.x)||!Finite(v.U1.y))nonfinite++;
                    if(rd.HasN){double n=v.N.magnitude;minN=Math.Min(minN,n);maxN=Math.Max(maxN,n);if(n<.99||n>1.01)attributes++;}
                    if(rd.HasT){var t=new Vector3(v.T.x,v.T.y,v.T.z);maxNT=Math.Max(maxNT,Math.Abs(Vector3.Dot(t,v.N)));if(t.magnitude<.99||t.magnitude>1.01||Math.Abs(Vector3.Dot(t,v.N))>.01)attributes++;}
                }
                for(int s=0;s<rd.Subs.Count;s++)for(int i=0;i<rd.Subs[s].Length;i+=3)
                {
                    var ix=rd.Subs[s];var a=rd.Verts[ix[i]];var b=rd.Verts[ix[i+1]];var c=rd.Verts[ix[i+2]];triangles++;
                    if(!SwapMesh.HasArea(a,b,c))degenerate++;
                    if(!tile)continue;
                    var f=new Face{A=a.W,B=b.W,C=c.W,Metal=s<mats.Length&&SwapTramInlays.Metal(mats[s])};
                    var box=new Bounds(f.A,Vector3.zero);box.Encapsulate(f.B);box.Encapsulate(f.C);
                    for(int x=Mathf.FloorToInt(box.min.x/2);x<=Mathf.FloorToInt(box.max.x/2);x++)for(int y=Mathf.FloorToInt(box.min.z/2);y<=Mathf.FloorToInt(box.max.z/2);y++)
                    {var k=new Vector2Int(x,y);if(!grid.TryGetValue(k,out var l))grid[k]=l=new();l.Add(f);}
                }
            }
            float? Top(Vector2 q,bool metal)
            {
                if(!grid.TryGetValue(new Vector2Int(Mathf.FloorToInt(q.x/2),Mathf.FloorToInt(q.y/2)),out var fs))return null;
                float? top=null;foreach(var f in fs){if(metal&&!f.Metal)continue;if(Height(f.A,f.B,f.C,q,out float h))top=Mathf.Max(top??float.MinValue,h);}return top;
            }
            var missing=new List<object>();
            foreach(var f in SwapTramRender.Geometry)
                foreach(double along in new[]{-1,-.9,-.5,0,.5,.9,1})foreach(double lateral in new[]{-.5,0,.5})
                {
                    var p=f.Map(along,lateral);var q=new Vector2((float)p.X,(float)(p.Y+OldTownSwap.DZ));
                    bool metal=lateral!=0;var top=Top(q,metal);
                    if(metal){railQueries++;if(top==null||Math.Abs(top.Value-SwapNetwork.BedLift)>.001){railMiss++;if(missing.Count<20)missing.Add(new object[]{f.Source.Id,along,lateral,top});}}
                    else{bedQueries++;if(top==null){bedMiss++;if(missing.Count<20)missing.Add(new object[]{f.Source.Id,along,lateral,top});}}
                }
            var result=new Dictionary<string,object>{{"changedRenderMeshes",edits.Length},{"triangles",triangles},{"zeroArea",degenerate},{"nonfinite",nonfinite},{"attributeFailures",attributes},{"normalLengthMin",minN},{"normalLengthMax",maxN},{"maxNormalTangentDot",maxNT},{"actualRailQueries",railQueries},{"legacyUniform5mmAssertionFailures",railMiss},{"actualBedQueries",bedQueries},{"actualBedMisses",bedMiss},{"firstMisses",missing},{"proof","Actual deformed mesh triangles, no bounds proxies. Rail queries at head centres +/-0.5m; Legacy uniform 5mm assertions are preserved as evidence against that obsolete assumption; acceptance uses actual source-profile-relative coverage in memory-coverage.json."}};
            File.WriteAllText(path,SwapExport.Json(result));return result;
        }
        static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
        internal static bool Height(Vector3 a,Vector3 b,Vector3 c,Vector2 q,out float h)
        {
            double den=((double)b.z-c.z)*(a.x-c.x)+((double)c.x-b.x)*(a.z-c.z);h=0;if(Math.Abs(den)<1e-12)return false;
            double x=(((double)b.z-c.z)*(q.x-c.x)+((double)c.x-b.x)*(q.y-c.z))/den,y=(((double)c.z-a.z)*(q.x-c.x)+((double)a.x-c.x)*(q.y-c.z))/den,z=1-x-y;
            // Float world reconstruction budget, expressed in barycentric coordinates. Never counts a bounds face.
            if(x < -1e-4 || y < -1e-4 || z < -1e-4)return false;h=(float)(x*a.y+y*b.y+z*c.y);return true;
        }
    }
}
#endif
