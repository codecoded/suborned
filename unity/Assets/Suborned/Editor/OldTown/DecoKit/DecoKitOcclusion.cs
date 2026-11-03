#if UNITY_EDITOR
using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Umbra occlusion for the street: view cells only where the player walks (the district, walk level to 60 m), so
    /// the canyon's facades cull what stands behind them. Occluders are the kit prefabs (DecoKitModels flags them).
    internal static class DecoKitOcclusion
    {
        static readonly Bounds Walkable = new(new Vector3(-30f, 30f, 50f), new Vector3(260f, 60f, 710f));  // x -160..100, z -305..405 (Southside)

        internal static void Bake()
        {
            var area = Object.FindAnyObjectByType<OcclusionArea>() ?? new GameObject("DK occlusion area").AddComponent<OcclusionArea>();
            area.center = Walkable.center;
            area.size = Walkable.size;
            StaticOcclusionCulling.smallestOccluder = DecoKitStage.Arg("dkOccluder", 5f);
            StaticOcclusionCulling.smallestHole = .25f;
            StaticOcclusionCulling.backfaceThreshold = 100f;
            var clock = Stopwatch.StartNew();
            if (!StaticOcclusionCulling.Compute()) throw new InvalidOperationException("[DecoKit] occlusion bake failed");
            DecoKitStage.Save();
            Debug.Log($"[DecoKit] occlusion baked in {clock.Elapsed.TotalSeconds:0} s, {StaticOcclusionCulling.umbraDataSize / 1024} KB");
        }
    }
}
#endif
