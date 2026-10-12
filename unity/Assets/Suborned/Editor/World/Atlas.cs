using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>One swatch atlas for the facade kit: every colour is a cell of a 16 x 16 point-sampled texture and a mesh picks
    /// its colour by UV (all vertices of a face share the cell centre). Two Suborned/FacadeLit materials share it, matte and glossy
    /// (the shader reads ambient occlusion and surface class from the vertex colour),
    /// so building bodies, windows, shopfronts and roof items in any number of colours cost two renderers per tile.</summary>
    public sealed class Atlas
    {
        const int Grid = 16, Cell = 4;
        readonly Dictionary<string, int> index = new();
        readonly List<Color32> colours = new();
        public Material Matte { get; private set; }
        public Material Glossy { get; private set; }

        public void Add(string name, Color c)
        {
            if (index.ContainsKey(name)) return;
            if (colours.Count >= Grid * Grid) throw new InvalidDataException("World: facade atlas full (256 swatches)");
            index[name] = colours.Count; colours.Add(c);
        }

        public Color Colour(string name) => colours[index[name]];

        public bool Has(string name) => index.ContainsKey(name);

        public Vector2 Uv(string name)
        {
            if (!index.TryGetValue(name ?? "", out var i)) throw new InvalidDataException($"World: facade swatch `{name}` not in facade.json");
            return new Vector2((i % Grid + 0.5f) / Grid, (i / Grid + 0.5f) / Grid);
        }

        public void Bake(Shader shader, float matte, float glossy)
        {
            var tex = new Texture2D(Grid * Cell, Grid * Cell, TextureFormat.RGBA32, false) { name = "facade-atlas", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[Grid * Cell * Grid * Cell];
            for (int i = 0; i < Grid * Grid; i++)
            {
                var c = i < colours.Count ? colours[i] : new Color32(255, 0, 255, 255);
                for (int y = 0; y < Cell; y++) for (int x = 0; x < Cell; x++)
                    px[((i / Grid) * Cell + y) * Grid * Cell + (i % Grid) * Cell + x] = c;
            }
            tex.SetPixels32(px); tex.Apply(false, false);
            Matte = Make(shader, tex, "facade", matte); Glossy = Make(shader, tex, "facade-gloss", glossy);
        }

        static Material Make(Shader shader, Texture tex, string name, float smooth)
        {
            var m = new Material(shader) { name = name, enableInstancing = true };
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            return m;
        }
    }
}
