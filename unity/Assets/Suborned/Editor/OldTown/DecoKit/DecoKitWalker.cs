#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// First-person walker (FirstPersonController) for kit scenes: press Play and walk.
    internal static class DecoKitWalker
    {
        /// Ground under the spawn and a clear capsule over it (run before the bake so a blocked spawn fails fast).
        internal static void Check(Vector3 spawn)
        {
            Physics.SyncTransforms();
            if (!Physics.Raycast(spawn + Vector3.up * 2f, Vector3.down, 3f))
                throw new InvalidOperationException($"[DecoKit] no ground under walker spawn {spawn}");
            var hits = Physics.OverlapCapsule(spawn + Vector3.up * .45f, spawn + Vector3.up * 1.45f, .3f);
            if (hits.Length > 0)
                throw new InvalidOperationException($"[DecoKit] walker spawn {spawn} overlaps " +
                                                    string.Join(", ", hits.Select(h => $"{h.transform.parent?.name}/{h.name}")));
        }

        internal static GameObject Make(Vector3 spawn, Vector3 lookAt)
        {
            Check(spawn);
            var facing = lookAt - spawn;
            facing.y = 0f;
            var player = new GameObject("DK walker");
            var character = player.AddComponent<CharacterController>();
            character.height = 1.8f;
            character.radius = .32f;
            character.center = new Vector3(0f, .9f, 0f);
            character.skinWidth = .035f;
            character.stepOffset = .36f;  // kerbs are 0.15 m, stair risers ~0.16 m
            character.slopeLimit = 52f;
            var view = new GameObject("DK walker camera") { tag = "MainCamera" };
            view.transform.SetParent(player.transform, false);
            view.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            var cam = view.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = .08f;
            cam.farClipPlane = 600f;
            cam.fieldOfView = 62f;
            cam.allowHDR = true;
            view.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.renderShadows = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            var walker = player.AddComponent<FirstPersonController>();
            walker.Configure(view.transform, spawn, Quaternion.LookRotation(facing.normalized, Vector3.up));
            Tune(walker);
            return player;
        }

        /// City-scale pace (the controller's defaults suit the small proofs): 420 m of boulevard in ~40 s at a run.
        internal static void Tune(FirstPersonController walker)
        {
            var so = new UnityEditor.SerializedObject(walker);
            so.FindProperty("walkSpeed").floatValue = 5.2f;
            so.FindProperty("sprintSpeed").floatValue = 11f;
            so.FindProperty("acceleration").floatValue = 30f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
