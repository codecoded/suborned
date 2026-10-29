#if UNITY_EDITOR
using System;using System.Collections.Generic;using System.Linq;using UnityEngine;using UnityEditor;
using Object=UnityEngine.Object;
namespace Suborned.OldTown.Editor.Swap
{
    internal static class SwapTramAttributeProof
    {
        static object Measure(IEnumerable<MeshFilter> filters)
        {
            long vertices=0,frameErrors=0,regular=0,degenerate=0,basisErrors=0;double maxNT=0,maxLength=0,maxDirection=0;long negative=0,positive=0;
            foreach(var mf in filters)
            {
                var rd=SwapTramMeshRead.Read(mf.sharedMesh,Matrix4x4.identity);if(!rd.HasT)continue;
                foreach(var v in rd.Verts){vertices++;var t=new Vector3(v.T.x,v.T.y,v.T.z);double dot=Math.Abs(Vector3.Dot(v.N,t)),length=Math.Abs(t.magnitude-1);maxNT=Math.Max(maxNT,dot);maxLength=Math.Max(maxLength,length);if(dot>3e-6||length>3e-6||Math.Abs(v.T.w)!=1)frameErrors++;if(v.T.w<0)negative++;else positive++;}
                foreach(var ix in rd.Subs)for(int i=0;i<ix.Length;i+=3)
                {
                    var a=rd.Verts[ix[i]];var b=rd.Verts[ix[i+1]];var c=rd.Verts[ix[i+2]];
                    double u1=(double)b.U0.x-a.U0.x,u2=(double)c.U0.x-a.U0.x,v1=(double)b.U0.y-a.U0.y,v2=(double)c.U0.y-a.U0.y,det=u1*v2-u2*v1;
                    if(det==0){degenerate++;continue;}regular++;
                    double[] e1={(double)b.P.x-a.P.x,(double)b.P.y-a.P.y,(double)b.P.z-a.P.z},e2={(double)c.P.x-a.P.x,(double)c.P.y-a.P.y,(double)c.P.z-a.P.z};
                    double[] du=new double[3],dv=new double[3];for(int k=0;k<3;k++){du[k]=(e1[k]*v2-e2[k]*v1)/det;dv[k]=(e2[k]*u1-e1[k]*u2)/det;}
                    foreach(int j in new[]{ix[i],ix[i+1],ix[i+2]})
                    {
                        var v=rd.Verts[j];double[] n={v.N.x,v.N.y,v.N.z};double norm=Math.Sqrt(n.Sum(z=>z*z));for(int k=0;k<3;k++)n[k]/=norm;
                        double dot=0;for(int k=0;k<3;k++)dot+=du[k]*n[k];var expected=new double[3];for(int k=0;k<3;k++)expected[k]=du[k]-dot*n[k];
                        double length=Math.Sqrt(expected.Sum(z=>z*z));if(length<1e-20)continue;for(int k=0;k<3;k++)expected[k]/=length;
                        double error=Math.Sqrt(Math.Pow(v.T.x-expected[0],2)+Math.Pow(v.T.y-expected[1],2)+Math.Pow(v.T.z-expected[2],2));maxDirection=Math.Max(maxDirection,error);
                        double bitangent=(n[1]*v.T.z-n[2]*v.T.y)*dv[0]+(n[2]*v.T.x-n[0]*v.T.z)*dv[1]+(n[0]*v.T.y-n[1]*v.T.x)*dv[2];float sign=bitangent<0?-1:1;if(error>3e-6||v.T.w!=sign)basisErrors++;
                    }
                }
            }
            return new Dictionary<string,object>{{"vertices",vertices},{"frameErrors",frameErrors},{"basisOrHandednessErrors",basisErrors},{"maxNormalTangentDot",maxNT},{"maxTangentLengthError",maxLength},{"maxUvTangentVectorError",maxDirection},{"regularUvTriangles",regular},{"degenerateUvTriangles",degenerate},{"negativeHandedVertices",negative},{"positiveHandedVertices",positive}};
        }
        internal static object Run()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suborned/OldTownArt/DecoKit/Models/Ground/DK_Tile_TramRail.fbx");
            var changed=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).Where(mf=>mf.sharedMesh&&(SwapTramRender.Owners.ContainsKey(mf)||mf.sharedMesh.name.Contains("(tram surface cut)")||mf.sharedMesh.name.Contains("(tram signal ownership)")||AssetDatabase.GetAssetPath(mf.sharedMesh).StartsWith("Assets/Suborned/OldTownArt/Swap/TramCorrected/")||AssetDatabase.GetAssetPath(mf.sharedMesh).StartsWith("Assets/Suborned/OldTownArt/Swap/TramSettled/")));
            return new Dictionary<string,object>{{"sourceRailTemplate",Measure(source.GetComponentsInChildren<MeshFilter>(true))},{"emitted",Measure(changed)},{"note","Independent derivatives from final triangle positions/UV0. Degenerate UV charts are explicitly counted and use an orthonormal source-frame fallback, not a fabricated UV derivative. Source smooth normals remain inverse-transpose mapped."}};
        }
    }
}
#endif
