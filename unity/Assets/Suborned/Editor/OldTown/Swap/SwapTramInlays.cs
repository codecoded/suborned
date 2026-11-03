#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Physical surface ownership: metal rails own their actual projected cross-section;
    /// functional signal faces own the remaining inlay area; tile paving gives way above the inlay.
    /// SignalHead/material/UV gate bindings, transforms, colliders and light timing stay untouched.</summary>
    internal static class SwapTramInlays
    {
        internal static bool Metal(Material m)=>m&&(m.name=="DK_Steel"||m.name=="DK_Steel_Satin"||m.name=="DK_Iron");
        internal static List<List<SwapMesh.V>> Subtract(List<List<SwapMesh.V>> polys,SwapShapes.Shape shape,bool above)
        {
            // Triangle masks can arrive with either winding. Use translated double area to
            // orient the inward half-planes without mutating the shared shape or its vertices.
            var ring=shape.Poly;
            if(ring==null||ring.Length<3)return polys;
            double area=0;var origin=ring[0];
            for(int i=1;i+1<ring.Length;i++)
                area+=((double)ring[i].x-origin.x)*((double)ring[i+1].y-origin.y)-((double)ring[i].y-origin.y)*((double)ring[i+1].x-origin.x);
            if(double.IsNaN(area)||double.IsInfinity(area))throw new System.IO.InvalidDataException("tram inlay: non-finite mask");
            if(area==0)return polys; // a line or point owns no surface
            float winding=area>0?1f:-1f;
            var planes=new List<SwapMesh.Half>();
            for(int i=0;i<ring.Length;i++){var a=ring[i];var b=ring[(i+1)%ring.Length];if(a.x==b.x&&a.y==b.y)continue;float x=-winding*(b.y-a.y),z=winding*(b.x-a.x);planes.Add(new SwapMesh.Half(x,z,-(double)x*a.x-(double)z*a.y));}
            if(above)planes.Add(new SwapMesh.Half(0,0,-shape.Y0,1));
            var result=new List<List<SwapMesh.V>>();
            foreach(var poly in polys){var inside=poly;foreach(var p in planes){if(inside.Count<3)break;var o=SwapMesh.Clip(inside,new SwapMesh.Half(-p.A,-p.B,-p.C,-p.Y));if(o.Count>=3)result.Add(o);inside=SwapMesh.Clip(inside,p);}}
            return result;
        }
        internal static void Apply(OldTownSwap.Report report)
        {
            var rail=new SwapShapes();var inlays=new SwapShapes();
            // Hull each physical extruded rail's actual projected mesh profile separately.
            // All cross-section parts (including the groove floor) are metal: no bed-wide proxy.
            foreach(var pair in SwapTramRender.Applied)
                foreach(var mf in SwapTramRender.Filters(pair.Key))
                {
                    var mats=mf.GetComponent<MeshRenderer>().sharedMaterials;var rd=SwapTramMeshRead.Read(mf.sharedMesh,mf.transform.localToWorldMatrix);
                    var sides=new[]{new List<Vector2>(),new List<Vector2>()};
                    for(int s=0;s<rd.Subs.Count;s++)if(s<mats.Length&&Metal(mats[s]))foreach(int i in rd.Subs[s])
                    {var p=rd.Verts[i].W;double lat=(p.x-pair.Value.Source.Centre.X)*pair.Value.Normal.X+(p.z-OldTownSwap.DZ-pair.Value.Source.Centre.Y)*pair.Value.Normal.Y;sides[lat<0?0:1].Add(new Vector2(p.x,p.z));}
                    foreach(var side in sides)if(side.Count>=3)rail.Add("rail-metal",pair.Value.Source.Id,side.ToArray());
                }
            int signalCuts=0,pavingCuts=0;long keptSignalTriangles=0;
            foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude))
            {
                var mr=mf.GetComponent<MeshRenderer>();if(!mr||!mr.enabled||!mf.sharedMesh)continue;
                var mats=mr.sharedMaterials;
                bool Dot(Material m)=>m&&m.shader&&m.shader.name=="Suborned/Signal Dots";
                bool Light(Material m)=>m&&(Dot(m)||m.name.EndsWith("-crosslight"));
                if(!mats.Any(Light))continue;
                var rd=SwapTramMeshRead.Read(mf.sharedMesh,mf.transform.localToWorldMatrix);var wr=new SwapMesh.Write(rd,true);int cuts=0;var completeSeats=new List<SwapTramSeatUnion.Face>();
                bool dots=mats.Any(Dot);
                bool seatOwner=mats.Any(m=>m&&m.name=="DK_Enamel_Charcoal");
                for(int sub=0;sub<rd.Subs.Count;sub++)for(int j=0;j<rd.Subs[sub].Length;j+=3)
                {
                    var tri=rd.Subs[sub];var a=rd.Verts[tri[j]];var b=rd.Verts[tri[j+1]];var c=rd.Verts[tri[j+2]];
                    float y0=Mathf.Min(a.W.y,Mathf.Min(b.W.y,c.W.y)),y1=Mathf.Max(a.W.y,Mathf.Max(b.W.y,c.W.y));
                    bool ground=(dots||sub<mats.Length&&(Light(mats[sub])||seatOwner&&mats[sub]&&mats[sub].name=="DK_Enamel_Charcoal"))&&y0>=-.001f&&y1<=SwapNetwork.BedLift+.001f&&y1-y0<1e-6f;
                    if(ground&&sub<mats.Length&&mats[sub]&&mats[sub].name.StartsWith("DK_Enamel_Charcoal",StringComparison.Ordinal))completeSeats.Add(new SwapTramSeatUnion.Face{A=a.W,B=b.W,C=c.W,Source=mf.name});
                    if(!ground){wr.Keep(sub,tri[j],tri[j+1],tri[j+2]);continue;}
                    var box=Rect.MinMaxRect(Mathf.Min(a.W.x,Mathf.Min(b.W.x,c.W.x)),Mathf.Min(a.W.z,Mathf.Min(b.W.z,c.W.z)),Mathf.Max(a.W.x,Mathf.Max(b.W.x,c.W.x)),Mathf.Max(a.W.z,Mathf.Max(b.W.z,c.W.z)));
                    var near=rail.Near(box).Where(h=>h.Overlaps(box)).ToArray();var polys=new List<List<SwapMesh.V>>{new(){a,b,c}};
                    foreach(var shape in near)polys=Subtract(polys,shape,false);
                    foreach(var poly in polys)
                    {
                        wr.Poly(sub,poly);keptSignalTriangles+=Math.Max(0,poly.Count-2);
                        var points=poly.Select(v=>new Vector2(v.W.x,v.W.z)).ToArray();
                        if(points.Length>=3)inlays.Add("signal-inlay",mf.name,points,y0,y1);
                    }
                    if(near.Length>0)cuts++;
                }
                // Retain the complete physical seat silhouette BEFORE metal clipping. It
                // recesses only non-metal paving; rail geometry remains exempt and unchanged.
                SwapTramSeatUnion.Add(inlays,completeSeats);
                if(cuts>0){mf.sharedMesh=wr.Mesh(mf.sharedMesh.name+" (tram signal ownership)");UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(mf);signalCuts+=cuts;}
            }
            // The importer can bake the physical enamel seats into a DIFFERENT renderer from
            // their functional light discs. Associate the actual source seat triangles geometrically.
            int separateSeats=0;
            foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude))
            {
                var mr=mf.GetComponent<MeshRenderer>();if(!mr||!mr.enabled||!mf.sharedMesh||mr.bounds.min.y>.006f||mr.bounds.max.y<0)continue;
                var mats=mr.sharedMaterials;
                bool Seat(int s)=>s<mats.Length&&mats[s]&&mats[s].name.StartsWith("DK_Enamel_Charcoal",StringComparison.Ordinal);
                if(!Enumerable.Range(0,mats.Length).Any(Seat))continue;
                var rd=SwapTramMeshRead.Read(mf.sharedMesh,mf.transform.localToWorldMatrix);var seatFaces=new List<SwapTramSeatUnion.Face>();
                for(int sub=0;sub<rd.Subs.Count;sub++)if(Seat(sub))for(int j=0;j<rd.Subs[sub].Length;j+=3)
                {
                    var ix=rd.Subs[sub];var tri=new[]{rd.Verts[ix[j]].W,rd.Verts[ix[j+1]].W,rd.Verts[ix[j+2]].W};
                    float lo=tri.Min(p=>p.y),hi=tri.Max(p=>p.y);if(lo<0||hi>.006f||hi-lo>1e-6f)continue;
                    var box=Rect.MinMaxRect(tri.Min(p=>p.x),tri.Min(p=>p.z),tri.Max(p=>p.x),tri.Max(p=>p.z));
                    // Existing source construction: seat 3mm, light 5mm. This selects actual
                    // lower supporting faces, never a widened light rectangle or guessed lift.
                    if(!inlays.Near(box).Any(h=>h.Overlaps(box)&&Math.Abs(h.Y0-lo-.002f)<1e-6f))continue;
                    seatFaces.Add(new SwapTramSeatUnion.Face{A=tri[0],B=tri[1],C=tri[2],Source=mf.name});separateSeats++;
                }
                SwapTramSeatUnion.Add(inlays,seatFaces);
            }
            foreach(var pair in SwapTramRender.Applied)foreach(var mf in SwapTramRender.Filters(pair.Key))
            {
                var rd=SwapTramMeshRead.Read(mf.sharedMesh,mf.transform.localToWorldMatrix);var wr=new SwapMesh.Write(rd,true);var mats=mf.GetComponent<MeshRenderer>().sharedMaterials;int cuts=0;
                for(int sub=0;sub<rd.Subs.Count;sub++)for(int j=0;j<rd.Subs[sub].Length;j+=3)
                {
                    var tri=rd.Subs[sub];var a=rd.Verts[tri[j]];var b=rd.Verts[tri[j+1]];var c=rd.Verts[tri[j+2]];
                    var bounds=new Bounds(a.W,Vector3.zero);bounds.Encapsulate(b.W);bounds.Encapsulate(c.W);var box=SwapMesh.XZ(bounds);
                    var near=sub<mats.Length&&Metal(mats[sub])?new SwapShapes.Shape[0]:inlays.Near(box).Where(h=>h.Y0<=bounds.max.y&&h.Overlaps(box)).ToArray();
                    if(near.Length==0){wr.Keep(sub,tri[j],tri[j+1],tri[j+2]);continue;}
                    var polys=new List<List<SwapMesh.V>>{new(){a,b,c}};foreach(var h in near)polys=Subtract(polys,h,true);
                    foreach(var poly in polys)wr.Poly(sub,poly);cuts++;
                }
                if(cuts>0){mf.sharedMesh=wr.Mesh(mf.sharedMesh.name+" (tram inlay recess)");UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(mf);pavingCuts+=cuts;}
            }
            report.Log($"tram crossing ownership: metal profiles {rail.All.Count}; {separateSeats} actual separate enamel seat faces / {SwapTramSeatUnion.ConvexComponents} exact convex components / {SwapTramSeatUnion.TriangleFallbacks} fallback triangles; retained {keptSignalTriangles} signal face triangles / {inlays.All.Count} inlay polygons; {signalCuts} signal and {pavingCuts} paving triangle candidates cut; no signal renderer or binding deleted");
        }
    }
}
#endif
