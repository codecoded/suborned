#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Suborned.OldTown.Editor.Swap
{
    internal static class SwapTramMeshRead
    {
        static readonly Dictionary<Mesh,Mesh> copies=new();
        internal static SwapMesh.Read Read(Mesh mesh,Matrix4x4 matrix)
        {
            for(int ch=2;ch<8;ch++)if(mesh.HasVertexAttribute((VertexAttribute)((int)VertexAttribute.TexCoord0+ch)))throw new System.IO.InvalidDataException("tram rewrite cannot drop extra UV channels");
            for(int s=0;s<mesh.subMeshCount;s++)if(mesh.GetSubMesh(s).topology!=MeshTopology.Triangles)throw new System.IO.InvalidDataException("tram rewrite cannot drop nontriangle submeshes");
            if(mesh.isReadable)return new SwapMesh.Read(mesh,matrix);
            if(!copies.TryGetValue(mesh,out var copy)||!copy)
            {
                using var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh);var md=data[0];copy=new Mesh{name=mesh.name+" (editor read copy)",indexFormat=mesh.indexFormat};
                using(var a=new Unity.Collections.NativeArray<Vector3>(md.vertexCount,Unity.Collections.Allocator.Temp)){md.GetVertices(a);copy.vertices=a.ToArray();}
                if(md.HasVertexAttribute(VertexAttribute.Normal))using(var a=new Unity.Collections.NativeArray<Vector3>(md.vertexCount,Unity.Collections.Allocator.Temp)){md.GetNormals(a);copy.normals=a.ToArray();}
                if(md.HasVertexAttribute(VertexAttribute.Tangent))using(var a=new Unity.Collections.NativeArray<Vector4>(md.vertexCount,Unity.Collections.Allocator.Temp)){md.GetTangents(a);copy.tangents=a.ToArray();}
                if(md.HasVertexAttribute(VertexAttribute.Color))using(var a=new Unity.Collections.NativeArray<Color>(md.vertexCount,Unity.Collections.Allocator.Temp)){md.GetColors(a);copy.colors=a.ToArray();}
                foreach(int ch in new[]{0,1})if(md.HasVertexAttribute((VertexAttribute)((int)VertexAttribute.TexCoord0+ch)))using(var a=new Unity.Collections.NativeArray<Vector2>(md.vertexCount,Unity.Collections.Allocator.Temp)){md.GetUVs(ch,a);copy.SetUVs(ch,a.ToArray());}
                copy.subMeshCount=md.subMeshCount;
                for(int s=0;s<md.subMeshCount;s++){using var a=new Unity.Collections.NativeArray<int>(md.GetSubMesh(s).indexCount,Unity.Collections.Allocator.Temp);md.GetIndices(a,s);copy.SetTriangles(a.ToArray(),s,false);}
                copy.RecalculateBounds();copies[mesh]=copy;
            }
            return new SwapMesh.Read(copy,matrix);
        }
    }
}
#endif
