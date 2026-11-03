using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.OldTown.Crowd
{
    /// <summary>Draws every agent wearing one <see cref="VatCitizen"/> in a single Graphics.RenderMeshPrimitives call
    /// (VatCrowd.shader): the baked frames uploaded once, the per-instance pose and animation each frame; and, from the
    /// same pose buffer, a second call for their contact shadows (VatCrowdBlob.shader).</summary>
    public sealed class VatCrowdRenderer : IDisposable
    {
        static readonly int FramesId = Shader.PropertyToID("_VatFrames"), PoseId = Shader.PropertyToID("_VatPose"),
                            AnimId = Shader.PropertyToID("_VatAnim"), VerticesId = Shader.PropertyToID("_VatVertices"),
                            BaseMapId = Shader.PropertyToID("_BaseMap"), BumpMapId = Shader.PropertyToID("_BumpMap"),
                            MetalRoughId = Shader.PropertyToID("_MetalRoughMap"), MetallicId = Shader.PropertyToID("_Metallic");

        readonly VatCitizen citizen;
        readonly Material material, blob;
        static Mesh quad;
        readonly GraphicsBuffer frames, pose, anim;
        RenderParams rp, blobRp;

        public VatCrowdRenderer(VatCitizen citizen, Shader shader, Shader blobShader, int capacity)
        {
            this.citizen = citizen;
            material = new Material(shader) { name = $"VatCrowd {citizen.name}", enableInstancing = true };
            material.SetTexture(BaseMapId, citizen.albedo);
            if (citizen.normal != null) material.SetTexture(BumpMapId, citizen.normal);
            if (citizen.metallicRoughness != null) material.SetTexture(MetalRoughId, citizen.metallicRoughness);
            else material.SetFloat(MetallicId, 0f);  // the white default would read as fully metal
            var data = citizen.frames.GetData<float>();
            frames = new GraphicsBuffer(GraphicsBuffer.Target.Structured, data.Length, sizeof(float));
            frames.SetData(data);
            pose = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Mathf.Max(1, capacity), 16);
            anim = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Mathf.Max(1, capacity), 16);
            material.SetBuffer(FramesId, frames);
            material.SetBuffer(PoseId, pose);
            material.SetBuffer(AnimId, anim);
            material.SetInt(VerticesId, citizen.vertices);
            // every rendering layer: with light layers on, URP only draws a shadow caster whose mask meets the light's
            rp = new RenderParams(material) { receiveShadows = true, renderingLayerMask = uint.MaxValue };
            if (blobShader == null) return;
            blob = new Material(blobShader) { name = $"VatCrowd blob {citizen.name}" };
            blob.SetBuffer(PoseId, pose);
            blob.SetBuffer(AnimId, anim);
            blobRp = new RenderParams(blob);
        }

        static Mesh Quad()
        {
            if (quad != null) return quad;
            quad = new Mesh { name = "VatCrowd blob quad" };
            quad.SetVertices(new[] { new Vector3(-.5f, 0f, -.5f), new Vector3(.5f, 0f, -.5f), new Vector3(.5f, 0f, .5f), new Vector3(-.5f, 0f, .5f) });
            quad.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            return quad;
        }

        /// <summary>poses: world xyz + yaw (rad); anims: frame a, frame b, blend, scale; bounds: round every instance
        /// (the shader places them, so Unity's culling and the shadow casters' depth fit see only this box).</summary>
        public void Draw(Vector4[] poses, Vector4[] anims, int count, Bounds bounds, bool shadows, bool contact)
        {
            if (count <= 0) return;
            rp.worldBounds = bounds;
            pose.SetData(poses, 0, 0, count);
            anim.SetData(anims, 0, 0, count);
            rp.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            for (int s = 0; s < citizen.mesh.subMeshCount; s++)
                Graphics.RenderMeshPrimitives(rp, citizen.mesh, s, count);
            if (blob == null || !contact) return;
            blobRp.worldBounds = bounds;
            Graphics.RenderMeshPrimitives(blobRp, Quad(), 0, count);
        }

        public void Dispose()
        {
            frames?.Dispose();
            pose?.Dispose();
            anim?.Dispose();
            foreach (var m in new[] { material, blob })
                if (m == null) continue;
                else if (Application.isPlaying) UnityEngine.Object.Destroy(m);
                else UnityEngine.Object.DestroyImmediate(m);
        }
    }
}
