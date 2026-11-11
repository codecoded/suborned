using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Suborned.World.Play
{
    /// <summary>Brief 106: Mesh LOD levels (MeshLodUtility, about half the indices a level; Unity 6.2+) on the merged ground chunks the swap writes
    /// (DecoKitTileMerge: the street's, Southside's and the network's tram rails and kerbs), added to their mesh assets AFTER every Editor bake of the
    /// town. Found 2026-11-11: with the levels in the meshes, the swap's reflection-probe bake (DecoKitStage.BakeEnvironment) hung the GPU and macOS
    /// killed Unity (exit 128 three times, even with the renderers pinned to level 0); without them it bakes as before. So the levels come last:
    /// ZoneBake097.BakeTown calls Apply once its bake is saved, and `-executeMethod Suborned.World.Play.MeshLod106.Cli` adds them alone (about a minute).
    /// Only the index buffers grow: vertices, UVs and the scene are untouched, and the chunks are probe-lit. At runtime BuiltIns frees the levels or pins
    /// level 0 (`-meshLod on|off`). WARNING for anyone who bakes OldTown.unity's lighting or probes in the Editor: rebake the town (unity-world), or
    /// expect the same hang. `-meshLod106 off` skips the step.</summary>
    public static class MeshLod106
    {
        static readonly string[] Assets =
        {
            "Assets/Suborned/OldTownArt/DecoKit/Generated/Ground_tiles_merged.asset",
            "Assets/Suborned/OldTownArt/DecoKit/Generated/Southside_tiles_merged.asset",
            "Assets/Suborned/OldTownArt/DecoKit/Generated/Network_tiles_merged.asset",
        };
        const int Levels = 6;

        public static void Cli()
        {
            try { Debug.Log(Apply()); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static string Apply()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-meshLod106") is int i && i >= 0 && i + 1 < Environment.GetCommandLineArgs().Length && Environment.GetCommandLineArgs()[i + 1] == "off")
                return "MeshLod106: skipped (-meshLod106 off)";
            int meshes = 0, added = 0; long l0 = 0, last = 0; var missing = 0;
            foreach (var path in Assets)
            {
                var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToArray();
                if (all.Length == 0) { missing++; continue; }
                foreach (var m in all)
                {
                    meshes++;
                    if (m.lodCount <= 1) { MeshLodUtility.GenerateMeshLods(m, Levels); EditorUtility.SetDirty(m); added++; }
                    l0 += Tris(m, 0); last += Tris(m, m.lodCount - 1);
                }
            }
            AssetDatabase.SaveAssets();
            return $"MeshLod106: {meshes} chunk meshes in {Assets.Length - missing} assets ({missing} missing), levels added to {added}; {l0} triangles at level 0, {last} at the last level";
        }

        static long Tris(Mesh m, int lod) { long n = 0; for (int s = 0; s < m.subMeshCount; s++) n += m.lodCount > 1 ? m.GetLod(s, lod).indexCount / 3 : (long)m.GetIndexCount(s) / 3; return n; }
    }
}
