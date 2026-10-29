#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using G=Suborned.OldTown.Editor.Swap.SwapTramJoin;
using Object=UnityEngine.Object;
namespace Suborned.OldTown.Editor.Swap
{
    internal static class SwapTramCoverage
    {
        internal sealed class Face
        {
            internal Vector3 A,B,C;internal string Mesh,Material,Id;internal int Sub,Triangle;internal bool Metal,Signal;
            internal object Context()=>new Dictionary<string,object>{{"mesh",Mesh},{"material",Material},{"id",Id},{"submesh",Sub},{"triangle",Triangle},{"vertices",new[]{XYZ(A),XYZ(B),XYZ(C)}}};
        }
        static double[] XYZ(Vector3 p)=>new[]{(double)p.x,p.y,p.z};
        internal static IEnumerable<Face> Faces(MeshFilter mf,string id,bool signal=false)
        {
            var rd=SwapTramMeshRead.Read(mf.sharedMesh,mf.transform.localToWorldMatrix);var mats=mf.GetComponent<MeshRenderer>().sharedMaterials;
            for(int s=0;s<rd.Subs.Count;s++)for(int j=0;j<rd.Subs[s].Length;j+=3)
            {
                var ix=rd.Subs[s];yield return new Face{A=rd.Verts[ix[j]].W,B=rd.Verts[ix[j+1]].W,C=rd.Verts[ix[j+2]].W,Mesh=mf.sharedMesh.name,Material=s<mats.Length&&mats[s]?mats[s].name:"none",Id=id,Sub=s,Triangle=j/3,Metal=s<mats.Length&&SwapTramInlays.Metal(mats[s]),Signal=signal};
            }
        }
        internal sealed class Index
        {
            internal readonly Dictionary<Vector2Int,List<Face>> Cells=new();
            internal void Add(Face f)
            {
                var bb=new Bounds(f.A,Vector3.zero);bb.Encapsulate(f.B);bb.Encapsulate(f.C);
                if(bb.min.y>.3f||bb.max.y<-.3f)return;
                for(int x=Mathf.FloorToInt(bb.min.x/2);x<=Mathf.FloorToInt(bb.max.x/2);x++)for(int y=Mathf.FloorToInt(bb.min.z/2);y<=Mathf.FloorToInt(bb.max.z/2);y++)
                {var key=new Vector2Int(x,y);if(!Cells.TryGetValue(key,out var list))Cells[key]=list=new();list.Add(f);}
            }
            internal List<Face> Near(Vector2 q)=>Cells.TryGetValue(new Vector2Int(Mathf.FloorToInt(q.x/2),Mathf.FloorToInt(q.y/2)),out var list)?list:new List<Face>();
            internal (double? h,Face face) Top(Vector2 q,bool metal=false,bool combined=false,bool exact=false)
            {
                double? top=null;Face winner=null;
                foreach(var f in Near(q))if((!metal||f.Metal)&&(!f.Signal||combined)&&Bary(f,q,out var h,out _,out var minimum)&&(exact?ContainsExact(f,q):minimum>=-1e-4))
                    if(top==null||h>top){top=h;winner=f;}
                return(top,winner);
            }
        }
        static System.Numerics.BigInteger Integer(float f)
        {
            uint bits=(uint)BitConverter.SingleToInt32Bits(f);int exponent=(int)((bits>>23)&255);uint mantissa=(bits&0x7fffff)|(exponent==0?0u:0x800000u);
            var n=new System.Numerics.BigInteger(mantissa);if(exponent>0)n<<=exponent-1;return(bits&0x80000000)!=0?-n:n;
        }
        static int Orient(float ax,float ay,float bx,float by,float qx,float qy)
        {
            double x=(double)bx-ax,y=(double)by-ay,u=(double)qx-ax,v=(double)qy-ay,det=x*v-y*u;
            double sumX=Math.Abs(ax)+Math.Abs(bx)+Math.Abs(qx),sumY=Math.Abs(ay)+Math.Abs(by)+Math.Abs(qy);
            double error=16*2.220446049250313e-16*sumX*sumY;
            if(Math.Abs(det)>error)return Math.Sign(det);
            // Every finite float is an integer times 2^-149. This fallback gives an EXACT
            // predicate on the actual renderer vertices, without expanding any triangle.
            return((Integer(bx)-Integer(ax))*(Integer(qy)-Integer(ay))-(Integer(by)-Integer(ay))*(Integer(qx)-Integer(ax))).Sign;
        }
        internal static bool ContainsExact(Face f,Vector2 q)
        {
            if(q.x<Mathf.Min(f.A.x,Mathf.Min(f.B.x,f.C.x))||q.x>Mathf.Max(f.A.x,Mathf.Max(f.B.x,f.C.x))||q.y<Mathf.Min(f.A.z,Mathf.Min(f.B.z,f.C.z))||q.y>Mathf.Max(f.A.z,Mathf.Max(f.B.z,f.C.z)))return false;
            int t=Orient(f.A.x,f.A.z,f.B.x,f.B.z,f.C.x,f.C.z);if(t==0)return false;
            return Orient(f.A.x,f.A.z,f.B.x,f.B.z,q.x,q.y)*t>=0&&Orient(f.B.x,f.B.z,f.C.x,f.C.z,q.x,q.y)*t>=0&&Orient(f.C.x,f.C.z,f.A.x,f.A.z,q.x,q.y)*t>=0;
        }
        internal static bool Bary(Face f,Vector2 q,out double h,out double[] bary,out double min)
        {
            var a=f.A;var b=f.B;var c=f.C;double den=((double)b.z-c.z)*((double)a.x-c.x)+((double)c.x-b.x)*((double)a.z-c.z);
            h=0;bary=null;min=0;if(den==0)return false;
            double x=(((double)b.z-c.z)*((double)q.x-c.x)+((double)c.x-b.x)*((double)q.y-c.z))/den;
            double y=(((double)c.z-a.z)*((double)q.x-c.x)+((double)a.x-c.x)*((double)q.y-c.z))/den,z=1-x-y;
            h=x*a.y+y*b.y+z*c.y;bary=new[]{x,y,z};min=Math.Min(x,Math.Min(y,z));return true;
        }
        static Index BuildIndex()
        {
            var index=new Index();
            foreach(var pair in SwapTramRender.Applied)foreach(var mf in SwapTramRender.Filters(pair.Key))foreach(var f in Faces(mf,pair.Value.Source.Id))index.Add(f);
            foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude))
            {
                var mr=mf.GetComponent<MeshRenderer>();if(!mr||!mr.enabled||!mf.sharedMesh)continue;
                if(!mr.sharedMaterials.Any(m=>m&&(m.name.EndsWith("-crosslight")||m.shader.name=="Suborned/Signal Dots")))continue;
                foreach(var f in Faces(mf,mf.name,true))index.Add(f);
            }
            return index;
        }
        internal static object Run(string path)
        {
            var index=BuildIndex();
            var misses=new List<object>();var railMiss=new List<object>();long bed=0,rail=0,strictBedMiss=0,strictRailMiss=0;double maxDelta=0;var exactMisses=new List<object>();
            // Read the actual immutable source FBX, including its bevel profile.
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/OldTownArt/DecoKit/Models/Ground/DK_Tile_TramRail.fbx");
            var sf=source.GetComponentsInChildren<MeshFilter>(true).Single();var reference=Faces(sf,"source").ToArray();
            double? Expected(double along,double lateral)
            {
                var q=new Vector2((float)along,(float)lateral);double? hi=null;
                foreach(var f in reference)if(f.Metal&&Bary(f,q,out var h,out _,out var m)&&m>=-1e-4)hi=Math.Max(hi??double.MinValue,h);
                return hi+SwapNetwork.BedLift;
            }
            foreach(var f in SwapTramRender.Geometry)foreach(double along in new[]{-1,-.9,-.5,0,.5,.9,1})foreach(double lat in new[]{-.5,0,.5})
            {
                var p=f.Map(along,lat);var q=new Vector2((float)p.X,(float)(p.Y+OldTownSwap.DZ));var hit=index.Top(q,lat!=0);var combined=index.Top(q,false,true);var strict=index.Top(q,lat!=0,false,true);if(strict.h==null){if(lat==0)strictBedMiss++;else strictRailMiss++;exactMisses.Add(new object[]{f.Source.Id,along,lat,new[]{q.x,q.y}});}
                if(lat==0)
                {
                    bed++;if(hit.h==null)
                    {
                        var candidates=index.Near(q).Where(t=>!t.Signal).Select(t=>{bool ok=Bary(t,q,out var h,out var bary,out double min);return(t,ok,h,bary,min);}).Where(t=>t.ok).OrderByDescending(t=>t.min).Take(3).Select(t=>(object)new Dictionary<string,object>{{"face",t.t.Context()},{"height",t.h},{"barycentric",t.bary},{"minimumBarycentric",t.min}}).ToArray();
                        misses.Add(new Dictionary<string,object>{{"id",f.Source.Id},{"along",along},{"lateral",lat},{"plan",new[]{p.X,p.Y}},{"worldQuery",new[]{q.x,q.y}},{"combinedHeight",combined.h},{"combinedFace",combined.face?.Context()},{"startSeam",new[]{f.Start.Right.X,f.Start.Right.Y,f.Start.Left.X,f.Start.Left.Y}},{"endSeam",new[]{f.End.Right.X,f.End.Right.Y,f.End.Left.X,f.End.Left.Y}},{"nearestTriangles",candidates}});
                    }
                }
                else
                {
                    rail++;double? expected=Expected(along,lat);double delta=hit.h!=null&&expected!=null?Math.Abs(hit.h.Value-expected.Value):double.PositiveInfinity;
                    if(!double.IsInfinity(delta))maxDelta=Math.Max(maxDelta,delta);
                    if(delta>1e-5)railMiss.Add(new Dictionary<string,object>{{"id",f.Source.Id},{"along",along},{"lateral",lat},{"plan",new[]{p.X,p.Y}},{"actual",hit.h},{"expected",expected},{"delta",double.IsInfinity(delta)?null:(object)delta},{"face",hit.face?.Context()}});
                }
            }
            var result=new Dictionary<string,object>{{"strictBedMisses",strictBedMiss},{"strictRailMisses",strictRailMiss},{"strictMisses",exactMisses},{"strictPredicate","Adaptive double / exact BigInteger orientation on actual float triangles; zero geometric expansion"},{"bedQueries",bed},{"bedOnlyMisses",misses.Count},{"bedMisses",misses},{"railQueries",rail},{"railProfileFailures",railMiss.Count},{"maximumSourceProfileDelta",maxDelta},{"railFailures",railMiss}};
            File.WriteAllText(path,SwapTramJson.Encode(result));return result;
        }
        public static void VerifySavedAuditSamples()
        {
            try
            {
                string repo=Path.GetFullPath(Path.Combine(Application.dataPath,"../..")),dir=Path.Combine(repo,"briefs/075/recovery/tram-corrected");
                EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-tramcorrected.unity",OpenSceneMode.Single);
                var root=Object.FindObjectsByType<Transform>().Single(t=>t.name==SwapNetwork.RailsRoot);SwapTramRender.BindExisting(root);var index=BuildIndex();
                var world=Suborned.World.WorldReader.Load(Path.Combine(repo,"out/world/harbour-district.world.json"));var misses=new List<object>();int samples=0;
                foreach(var track in world.Transit.Tram)
                {
                    float carry=0;var line=track.Line;var offs=track.Spacing>0?new[]{-track.Spacing*.5f,track.Spacing*.5f}:new[]{0f};
                    for(int i=1;i<line.Length;i++)
                    {
                        Vector2 a=line[i-1],b=line[i];float length=Vector2.Distance(a,b);if(length<1e-4f)continue;var d=(b-a)/length;var n=new Vector2(-d.y,d.x);
                        for(float x=carry;x<length;x+=1f)foreach(float offset in offs)foreach(float lat in new[]{-.6f,0f,.6f})
                        {var plan=a+d*x+n*(offset+lat);var q=OldTownSwap.Old(plan);samples++;var hit=index.Top(q,false,true,true);if(hit.h==null)misses.Add(new object[]{track.Id,new[]{plan.x,plan.y},offset,lat});}
                        carry=Mathf.Repeat(carry-length,1f);
                    }
                }
                var old=SwapTramSource.Obj(Suborned.World.Json.Parse(File.ReadAllText(Path.Combine(repo,"briefs/075/recovery/tram-gaps/evidence.json"))));var probes=new List<object>();int failures=0;
                foreach(var row in SwapTramSource.Arr(old["rows"]).Select(SwapTramSource.Obj))
                {
                    var points=new[]{SwapTramSource.Obj(row["sample"])["q"],SwapTramSource.Obj(row["arc"])["exact_point"],SwapTramSource.Obj(row["arc"])["centre_point"]};
                    foreach(var value in points){var p=SwapTramSource.Point(value);var q=new Vector2((float)p.X,(float)(p.Y+OldTownSwap.DZ));var hit=index.Top(q,false,true,true);if(hit.h==null)failures++;probes.Add(new Dictionary<string,object>{{"plan",new[]{p.X,p.Y}},{"height",hit.h},{"face",hit.face?.Context()}});}
                }
                File.WriteAllText(Path.Combine(dir,"saved-audit-surface-proof.json"),SwapTramJson.Encode(new Dictionary<string,object>{{"samples",samples},{"misses",misses},{"originalAndExactArcProbes",probes},{"originalAndExactArcMisses",failures},{"predicate","Actual bed plus retained signal triangle surfaces; exact float-coordinate orientation, no expanded bounds or triangle tolerance"}}));
                if(misses.Count>0||failures>0)throw new InvalidOperationException("saved actual audit surface coverage failed");
                Debug.Log("[TramCorrected] actual mesh coverage: "+samples+" audit samples + 18 original/exact-arc probes, zero misses");
            }
            catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        public static void DiagnosePrior()
        {
            try
            {
                string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../briefs/075/recovery/tram-corrected"));
                EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-tramsettled.unity",OpenSceneMode.Single);
                var saved=Object.FindObjectsByType<Transform>().Single(t=>t.name==SwapNetwork.RailsRoot);
                File.WriteAllText(Path.Combine(dir,"prior-persisted-bindings.json"),SwapTramJson.Encode(new Dictionary<string,object>{{"tiles",saved.childCount},{"filters",saved.GetComponentsInChildren<MeshFilter>(true).Select(mf=>(object)new Dictionary<string,object>{{"name",mf.sharedMesh.name},{"asset",AssetDatabase.GetAssetPath(mf.sharedMesh)}}).ToArray()}}));
                EditorSceneManager.OpenScene("Assets/Scenes/probe-recovery075-swap.unity",OpenSceneMode.Single);
                var root=Object.FindObjectsByType<Transform>().Single(t=>t.name==SwapNetwork.RailsRoot);
                var geometry=SwapTramSource.Load(Path.GetFullPath(Path.Combine(Application.dataPath,"../..")),"out/world/harbour-district.world.json");
                var report=new OldTownSwap.Report();SwapTramRender.Apply(root,geometry,report);SwapNetwork.ClearImportedGround(report);
                Run(Path.Combine(dir,"prior-localized.json"));
                Debug.Log("[TramCorrected] prior coverage localization complete; no scene save or capture");
            }
            catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
#endif
