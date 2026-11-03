#if UNITY_EDITOR
using System;using System.Collections.Generic;using UnityEngine;
using G=Suborned.OldTown.Editor.Swap.SwapTramJoin;
namespace Suborned.OldTown.Editor.Swap
{
    /// <summary>Independent physical-chart check on final float positions/UVs. The permitted
    /// error is calculated from UV float rounding and double operation bounds, never a fit tolerance.</summary>
    internal static class SwapTramDensity
    {
        internal static long Channels,Failures;internal static double MaxError,MaxBound,MinRatio=double.MaxValue,MaxRatio,SumRatio;
        static double[] Sub(Vector3 a,Vector3 b)=>new[]{(double)a.x-b.x,(double)a.y-b.y,(double)a.z-b.z};
        static double[] Cross(double[] a,double[] b)=>new[]{a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};
        static double Dot(double[] a,double[] b)=>a[0]*b[0]+a[1]*b[1]+a[2]*b[2];
        static double Len(double[] a)=>Math.Sqrt(Dot(a,a));
        static double HalfUlp(float x){int b=BitConverter.SingleToInt32Bits(Math.Abs(x));float next=BitConverter.Int32BitsToSingle(b+1);return ((double)next-Math.Abs(x))*.5;}
        internal static void Record(List<SwapMesh.V> poly,Vector3 gu,Vector3 gv,SwapMesh.V source0,Vector3 sourceP0,G.Footprint f,double sign,double height)
        {
            foreach(var gradient in new[]{(gu,false),(gv,true)})
            {
                var g=gradient.Item1;var worldG=new[]{sign*(f.Direction.X*g.x+f.Normal.X*g.z),(double)g.y,sign*(f.Direction.Y*g.x+f.Normal.Y*g.z)};
                for(int i=1;i+1<poly.Count;i++)
                {
                    var vs=new[]{poly[0],poly[i],poly[i+1]};var a=Sub(vs[1].P,vs[0].P);var b=Sub(vs[2].P,vs[0].P);var n=Cross(a,b);double den=Dot(n,n);if(den==0)continue;
                    var l1=Cross(b,n);var l2=Cross(n,a);for(int k=0;k<3;k++){l1[k]/=den;l2[k]/=den;}
                    double U(SwapMesh.V v)=>gradient.Item2?v.U0.y:v.U0.x;
                    var actual=new double[3];var expected=new double[3];double nd=Dot(n,worldG)/den;
                    for(int k=0;k<3;k++){actual[k]=l1[k]*(U(vs[1])-U(vs[0]))+l2[k]*(U(vs[2])-U(vs[0]));expected[k]=worldG[k]-n[k]*nd;}
                    double[] eps=new double[3];for(int k=0;k<3;k++)
                    {
                        var p=vs[k].P;double absoluteTerms=(Math.Abs(p.x)+Math.Abs(f.Source.Centre.X)+Math.Abs(sourceP0.x))*Math.Abs(g.x)+(Math.Abs(p.z)+Math.Abs(f.Source.Centre.Y)+OldTownSwap.DZ+Math.Abs(sourceP0.z))*Math.Abs(g.z)+(Math.Abs(p.y)+Math.Abs(height)+Math.Abs(sourceP0.y))*Math.Abs(g.y)+Math.Abs(U(source0));
                        eps[k]=HalfUlp((float)U(vs[k]))+32*2.220446049250313e-16*absoluteTerms;
                    }
                    double bound=Len(l1)*(eps[1]+eps[0])+Len(l2)*(eps[2]+eps[0])+128*2.220446049250313e-16*(Len(actual)+Len(expected));
                    double error=Len(new[]{actual[0]-expected[0],actual[1]-expected[1],actual[2]-expected[2]});Channels++;MaxError=Math.Max(MaxError,error);MaxBound=Math.Max(MaxBound,bound);if(error>bound)Failures++;
                    double length=Len(expected);if(length>1e-8){double ratio=Len(actual)/length;MinRatio=Math.Min(MinRatio,ratio);MaxRatio=Math.Max(MaxRatio,ratio);SumRatio+=ratio;}
                }
            }
        }
        internal static object Result()=>new Dictionary<string,object>{{"triangleUvChannels",Channels},{"failuresAgainstDerivedFloatBound",Failures},{"maximumPhysicalGradientError",MaxError},{"maximumCalculatedRoundingBound",MaxBound},{"minimumDensityRatio",MinRatio},{"maximumDensityRatio",MaxRatio},{"scope","Actual emitted tram positions/UV0 against source physical chart derivatives; UV rounding + double-operation bound per triangle, no global tolerance inflation"}};
    }
}
#endif
