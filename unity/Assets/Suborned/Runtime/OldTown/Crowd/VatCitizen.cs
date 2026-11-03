using System;
using UnityEngine;

namespace Suborned.OldTown.Crowd
{
    /// <summary>One citizen model baked for the instanced crowd (citizen_vat.py): the static mesh (UV1.x = its row in the
    /// frames, with rest normals and tangents), its glTF PBR maps, and every clip's deformed vertices, [frame][vertex]
    /// position xyz + skinning rotation xyzw as float32 (<see cref="Row"/>): the shader turns the rest normal and tangent
    /// by it. The clips are in place; <see cref="Clip.speed"/> is the ground speed (m/s) at which the feet don't skate.</summary>
    public sealed class VatCitizen : ScriptableObject
    {
        [Serializable]
        public struct Clip
        {
            public string name;
            public int start, frames;
            public float fps, speed;
            public float Length => frames / fps;
        }

        public const int Row = 7;  // floats per vertex per frame

        public Mesh mesh;
        public Texture2D albedo, normal, metallicRoughness;  // glTF maps: roughness in G, metal in B
        public TextAsset frames;
        public int vertices;
        public Clip[] clips;

        public int ClipIndex(string clipName) => Array.FindIndex(clips, c => c.name == clipName);

        /// <summary>How wide the figure walks, m: the 90th percentile of its vertices' distance from the vertical axis
        /// over a clip's frames (arms, swinging hems and cloaks included; the rare flung hand not), clamped to
        /// [lo, hi].</summary>
        public float Radius(string clipName, float lo = .25f, float hi = .5f)
        {
            int k = Mathf.Max(0, ClipIndex(clipName));
            using var data = frames.GetData<float>();
            var r = new float[clips[k].frames * vertices];
            for (int f = 0; f < clips[k].frames; f++)
            for (int v = 0; v < vertices; v++)
            {
                int o = ((clips[k].start + f) * vertices + v) * Row;
                r[f * vertices + v] = Mathf.Sqrt(data[o] * data[o] + data[o + 2] * data[o + 2]);
            }
            Array.Sort(r);
            return Mathf.Clamp(r[(int)(.9f * (r.Length - 1))], lo, hi);
        }
    }
}
