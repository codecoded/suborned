#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Brief 075 swap (Latch): the mesh operations the cut needs, on any mesh (merged tiles, slabs, city mass chunks, our batches):
    /// connected components (vertices welded at 1 mm in world space) and an exact XZ clip of triangles against a rectangle, keeping either
    /// the part inside it or the part outside (the outside of a rectangle is four disjoint convex regions, each a Sutherland-Hodgman clip).
    /// Attributes (normal, tangent, uv0, uv1, colour) are interpolated along the cut edges.</summary>
    internal static class SwapMesh
    {
        internal struct V
        {
            public Vector3 P, W, N; public Vector4 T; public Vector2 U0, U1; public Color C;
            public static V Lerp(V a, V b, float t) => new V
            {
                P = Vector3.LerpUnclamped(a.P, b.P, t), W = Vector3.LerpUnclamped(a.W, b.W, t), N = Vector3.LerpUnclamped(a.N, b.N, t).normalized,
                T = Vector4.LerpUnclamped(a.T, b.T, t), U0 = Vector2.LerpUnclamped(a.U0, b.U0, t), U1 = Vector2.LerpUnclamped(a.U1, b.U1, t), C = Color.LerpUnclamped(a.C, b.C, t),
            };
        }

        /// <summary>A mesh read out: per vertex attributes (world position from l2w), per submesh triangles.</summary>
        internal sealed class Read
        {
            public V[] Verts; public List<int[]> Subs = new(); public bool HasN, HasT, HasU0, HasU1, HasC;
            public Read(Mesh m, Matrix4x4 l2w)
            {
                var p = m.vertices; var n = m.normals; var t = m.tangents; var u0 = new List<Vector2>(); m.GetUVs(0, u0); var u1 = new List<Vector2>(); m.GetUVs(1, u1); var c = m.colors;
                HasN = n.Length == p.Length; HasT = t.Length == p.Length; HasU0 = u0.Count == p.Length; HasU1 = u1.Count == p.Length; HasC = c.Length == p.Length;
                Verts = new V[p.Length];
                for (int i = 0; i < p.Length; i++)
                    Verts[i] = new V { P = p[i], W = l2w.MultiplyPoint3x4(p[i]), N = HasN ? n[i] : Vector3.up, T = HasT ? t[i] : Vector4.zero, U0 = HasU0 ? u0[i] : default, U1 = HasU1 ? u1[i] : default, C = HasC ? c[i] : Color.white };
                for (int s = 0; s < m.subMeshCount; s++) Subs.Add(m.GetSubMesh(s).topology == MeshTopology.Triangles ? m.GetTriangles(s) : new int[0]);
            }

            /// <summary>Component id per triangle (per submesh), vertices welded at 1 mm in world space.</summary>
            public List<int[]> Components(out int count)
            {
                var weld = new Dictionary<Vector3Int, int>(Verts.Length);
                var wid = new int[Verts.Length];
                for (int i = 0; i < Verts.Length; i++)
                {
                    var k = Vector3Int.RoundToInt(Verts[i].W * 1000f);
                    if (!weld.TryGetValue(k, out var id)) weld[k] = id = weld.Count;
                    wid[i] = id;
                }
                var parent = new int[weld.Count];
                for (int i = 0; i < parent.Length; i++) parent[i] = i;
                int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
                foreach (var tris in Subs)
                    for (int i = 0; i + 2 < tris.Length; i += 3)
                    {
                        int a = Find(wid[tris[i]]), b = Find(wid[tris[i + 1]]), c = Find(wid[tris[i + 2]]);
                        if (a != b) parent[a] = b; b = Find(b); if (b != c) parent[b] = c;
                    }
                var remap = new Dictionary<int, int>();
                var outIds = new List<int[]>();
                foreach (var tris in Subs)
                {
                    var ids = new int[tris.Length / 3];
                    for (int i = 0; i < ids.Length; i++)
                    {
                        int r = Find(wid[tris[3 * i]]);
                        if (!remap.TryGetValue(r, out var c)) remap[r] = c = remap.Count;
                        ids[i] = c;
                    }
                    outIds.Add(ids);
                }
                count = remap.Count;
                return outIds;
            }
        }

        /// <summary>Collects output triangles (vertex structs) per submesh and writes a mesh in the source's local space.</summary>
        internal sealed class Write
        {
            readonly List<V> verts = new(); readonly List<List<int>> subs = new(); readonly Read src;
            readonly Dictionary<int, int> fromSrc = new();
            readonly bool finalFrames;
            public int Triangles { get; private set; }
            public Write(Read src, bool finalFrames = false) { this.src = src; this.finalFrames = finalFrames; for (int s = 0; s < src.Subs.Count; s++) subs.Add(new List<int>()); }
            public void Keep(int sub, int a, int b, int c) { subs[sub].Add(Src(a)); subs[sub].Add(Src(b)); subs[sub].Add(Src(c)); Triangles++; }
            int Src(int i) { if (!fromSrc.TryGetValue(i, out var o)) { fromSrc[i] = o = verts.Count; verts.Add(src.Verts[i]); } return o; }
            public void Poly(int sub, List<V> poly)
            {
                if (poly.Count < 3) return;
                // Clip can retain a boundary vertex twice or collapse a polygon onto an edge.
                // Test in output-local 3D, not XZ: vertical faces and genuinely thin triangles survive.
                int b = -1;
                for (int i = 1; i + 1 < poly.Count; i++)
                {
                    if (!HasArea(poly[0], poly[i], poly[i + 1])) continue;
                    if (b < 0) { b = verts.Count; verts.AddRange(poly); }
                    subs[sub].Add(b); subs[sub].Add(b + i); subs[sub].Add(b + i + 1); Triangles++;
                }
            }
            public Mesh Mesh(string name)
            {
                if(finalFrames && src.HasT)
                {
                    var variants=new Dictionary<(int,Vector4),int>();
                    foreach(var sub in subs)for(int i=0;i<sub.Count;i+=3)
                    {
                        var a=verts[sub[i]];var b=verts[sub[i+1]];var c=verts[sub[i+2]];
                        var basis=SwapTramFrames.BasisOf(a,b,c);
                        for(int j=0;j<3;j++)
                        {
                            int old=sub[i+j];var v=verts[old];v.N=v.N.normalized;var t=SwapTramFrames.Frame(v,basis);
                            if(v.T.Equals(t)){verts[old]=v;continue;}
                            if(!variants.TryGetValue((old,t),out var id)){id=verts.Count;v.T=t;verts.Add(v);variants.Add((old,t),id);}sub[i+j]=id;
                        }
                    }
                }
                if(finalFrames)
                {
                    // Derivative-specific corner variants can leave old, unreferenced vertices.
                    // Pack only referenced output vertices; no source triangle or attribute is removed.
                    var live=new List<V>();var remap=new Dictionary<int,int>();
                    foreach(var sub in subs)for(int i=0;i<sub.Count;i++){int old=sub[i];if(!remap.TryGetValue(old,out int id)){id=live.Count;live.Add(verts[old]);remap.Add(old,id);}sub[i]=id;}
                    verts.Clear();verts.AddRange(live);
                }
                var m = new Mesh { name = name, indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                var p = new Vector3[verts.Count]; for (int i = 0; i < p.Length; i++) p[i] = verts[i].P; m.vertices = p;
                if (src.HasN) { var a = new Vector3[verts.Count]; for (int i = 0; i < a.Length; i++) a[i] = verts[i].N; m.normals = a; }
                if (src.HasT) { var a = new Vector4[verts.Count]; for (int i = 0; i < a.Length; i++) a[i] = verts[i].T; m.tangents = a; }
                if (src.HasU0) { var a = new Vector2[verts.Count]; for (int i = 0; i < a.Length; i++) a[i] = verts[i].U0; m.uv = a; }
                if (src.HasU1) { var a = new Vector2[verts.Count]; for (int i = 0; i < a.Length; i++) a[i] = verts[i].U1; m.uv2 = a; }
                if (src.HasC) { var a = new Color[verts.Count]; for (int i = 0; i < a.Length; i++) a[i] = verts[i].C; m.colors = a; }
                m.subMeshCount = subs.Count;
                for (int s = 0; s < subs.Count; s++) m.SetTriangles(subs[s], s, false);
                m.RecalculateBounds();
                return m;
            }
        }

        /// <summary>Half-plane a*x + b*z + c >= 0 in world XZ.</summary>
        internal readonly struct Half { public readonly float A, B, Y; public readonly double C; public Half(float a, float b, double c, float y = 0) { A = a; B = b; C = c; Y = y; } public float D(Vector3 w) => (float)((double)A * w.x + (double)B * w.z + C + (double)Y * w.y); }

        internal static List<V> Clip(List<V> poly, Half h)
        {
            var o = new List<V>(poly.Count + 2);
            for (int i = 0; i < poly.Count; i++)
            {
                V a = poly[i], b = poly[(i + 1) % poly.Count];
                float da = h.D(a.W), db = h.D(b.W);
                if (da >= 0f) o.Add(a);
                if ((da >= 0f) != (db >= 0f)) o.Add(V.Lerp(a, b, da / (da - db)));
            }
            // Exact coordinate equality only; Unity's approximate Vector3 == would erase thin cuts.
            for (int i = o.Count - 1; i >= 0 && o.Count > 1; i--)
                if (SamePosition(o[i].P, o[(i + 1) % o.Count].P)) o.RemoveAt(i);
            return o;
        }

        static bool SamePosition(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;

        /// <summary>Scale-relative double-precision area check on the actual output vertices.
        /// No world-unit cutoff: tiny valid faces survive. The relative sine threshold is 1e-12,
        /// well below float coordinate precision; only collapsed/numerically collinear fans are rejected.</summary>
        internal static bool HasArea(V a, V b, V c)
        {
            double ux = (double)b.P.x - a.P.x, uy = (double)b.P.y - a.P.y, uz = (double)b.P.z - a.P.z;
            double vx = (double)c.P.x - a.P.x, vy = (double)c.P.y - a.P.y, vz = (double)c.P.z - a.P.z;
            double x = uy * vz - uz * vy, y = uz * vx - ux * vz, z = ux * vy - uy * vx;
            double u2 = ux * ux + uy * uy + uz * uz, v2 = vx * vx + vy * vy + vz * vz;
            double area2 = x * x + y * y + z * z;
            if (double.IsNaN(area2) || double.IsInfinity(area2))
                throw new System.IO.InvalidDataException("Swap: non-finite clipped triangle");
            return area2 > u2 * v2 * 1e-24;
        }

        /// <summary>The convex regions of the plane outside rect (x0..x1, z0..z1): left, right, below-middle, above-middle.</summary>
        internal static Half[][] Outside(Rect r) => new[]
        {
            new[] { new Half(-1, 0, r.xMin) },
            new[] { new Half(1, 0, -r.xMax) },
            new[] { new Half(1, 0, -r.xMin), new Half(-1, 0, r.xMax), new Half(0, -1, r.yMin) },
            new[] { new Half(1, 0, -r.xMin), new Half(-1, 0, r.xMax), new Half(0, 1, -r.yMax) },
        };
        internal static Half[][] Inside(Rect r) => new[] { new[] { new Half(1, 0, -r.xMin), new Half(-1, 0, r.xMax), new Half(0, 1, -r.yMin), new Half(0, -1, r.yMax) } };

        /// <summary>One triangle into the output, clipped to the union of the given disjoint convex regions.</summary>
        internal static void ClipInto(Write w, int sub, V a, V b, V c, Half[][] regions)
        {
            foreach (var reg in regions)
            {
                var poly = new List<V> { a, b, c };
                foreach (var h in reg) { poly = Clip(poly, h); if (poly.Count < 3) break; }
                if (poly.Count >= 3) w.Poly(sub, poly);
            }
        }

        internal static bool Overlaps(Rect a, Rect b, float eps = 0.01f) => a.xMin < b.xMax - eps && a.xMax > b.xMin + eps && a.yMin < b.yMax - eps && a.yMax > b.yMin + eps;
        internal static bool Inside(Rect r, Vector2 p) => p.x >= r.xMin && p.x <= r.xMax && p.y >= r.yMin && p.y <= r.yMax;
        internal static Rect XZ(Bounds b) => Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);
    }
    internal static class SwapTramFrames
    {
        internal readonly struct D3
        {
            internal readonly double X,Y,Z;internal D3(double x,double y,double z){X=x;Y=y;Z=z;}
            internal D3(Vector3 p):this(p.x,p.y,p.z){}
            public static D3 operator +(D3 a,D3 b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
            public static D3 operator -(D3 a,D3 b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
            public static D3 operator *(D3 a,double k)=>new(a.X*k,a.Y*k,a.Z*k);
            internal static double Dot(D3 a,D3 b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
            internal static D3 Cross(D3 a,D3 b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
            internal double Length=>Math.Sqrt(Dot(this,this));internal D3 Unit=>this*(1/Length);
        }
        internal readonly struct Basis
        {
            internal readonly D3 U,V;internal readonly bool Regular;
            internal Basis(D3 u,D3 v,bool regular){U=u;V=v;Regular=regular;}
        }
        internal static Basis BasisOf(SwapMesh.V a,SwapMesh.V b,SwapMesh.V c)
        {
            double u1=(double)b.U0.x-a.U0.x,u2=(double)c.U0.x-a.U0.x,v1=(double)b.U0.y-a.U0.y,v2=(double)c.U0.y-a.U0.y,det=u1*v2-u2*v1;
            if(det==0){DegenerateUv++;return new Basis(default,default,false);}Regular++;
            var x=new D3(b.P)-new D3(a.P);var y=new D3(c.P)-new D3(a.P);
            return new Basis((x*v2-y*v1)*(1/det),(y*u1-x*u2)*(1/det),true);
        }
        internal static Vector4 Frame(SwapMesh.V v,Basis basis)
        {
            var n=new D3(v.N).Unit;var t=basis.Regular?basis.U:new D3(v.T.x,v.T.y,v.T.z);
            // Two crosses avoid catastrophic cancellation in Gram-Schmidt near a smooth corner.
            t=D3.Cross(n,D3.Cross(t,n));
            if(t.Length<1e-20){var axis=Math.Abs(n.X)<.8?new D3(1,0,0):new D3(0,0,1);t=D3.Cross(axis,n);}t=t.Unit;
            float sign=basis.Regular?(D3.Dot(D3.Cross(n,t),basis.V)<0?-1:1):(v.T.w<0?-1:1);
            var f=new Vector3((float)t.X,(float)t.Y,(float)t.Z);
            MaxOrthogonality=Math.Max(MaxOrthogonality,Math.Abs(Vector3.Dot(v.N.normalized,f)));MaxLengthError=Math.Max(MaxLengthError,Math.Abs(f.magnitude-1));return new Vector4(f.x,f.y,f.z,sign);
        }
        internal static long Regular,DegenerateUv;internal static double MaxOrthogonality,MaxLengthError;
        internal static Vector3 Gradient(Vector3 p0,Vector3 p1,Vector3 p2,double a,double b,double c)
        {
            double ux=(double)p1.x-p0.x,uy=(double)p1.y-p0.y,uz=(double)p1.z-p0.z;
            double vx=(double)p2.x-p0.x,vy=(double)p2.y-p0.y,vz=(double)p2.z-p0.z;
            double nx=uy*vz-uz*vy,ny=uz*vx-ux*vz,nz=ux*vy-uy*vx,den=nx*nx+ny*ny+nz*nz;
            if(den==0)return Vector3.zero;
            double d1=b-a,d2=c-a;
            return new Vector3((float)(((vy*nz-vz*ny)*d1+(ny*uz-nz*uy)*d2)/den),(float)(((vz*nx-vx*nz)*d1+(nz*ux-nx*uz)*d2)/den),(float)(((vx*ny-vy*nx)*d1+(nx*uy-ny*ux)*d2)/den));
        }
        internal static Vector4 Frame(SwapMesh.V v,Vector3 du,Vector3 dv,bool regular)=>Frame(v,new Basis(new D3(du),new D3(dv),regular));
        internal static void Derivatives(SwapMesh.V a,SwapMesh.V b,SwapMesh.V c,out Vector3 du,out Vector3 dv,out bool regular)
        {
            double u1=(double)b.U0.x-a.U0.x,u2=(double)c.U0.x-a.U0.x,v1=(double)b.U0.y-a.U0.y,v2=(double)c.U0.y-a.U0.y,den=u1*v2-u2*v1;
            regular=den!=0;du=dv=Vector3.zero;
            if(!regular){DegenerateUv++;return;}Regular++;
            double ax=(double)b.P.x-a.P.x,ay=(double)b.P.y-a.P.y,az=(double)b.P.z-a.P.z,bx=(double)c.P.x-a.P.x,by=(double)c.P.y-a.P.y,bz=(double)c.P.z-a.P.z;
            du=new Vector3((float)((ax*v2-bx*v1)/den),(float)((ay*v2-by*v1)/den),(float)((az*v2-bz*v1)/den));
            dv=new Vector3((float)((bx*u1-ax*u2)/den),(float)((by*u1-ay*u2)/den),(float)((bz*u1-az*u2)/den));
            if(!Finite(du)||!Finite(dv))throw new System.IO.InvalidDataException("tram UV derivative is nonfinite");
        }
        static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
    }
}
#endif
