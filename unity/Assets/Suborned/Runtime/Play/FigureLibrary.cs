using System;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 020 (stand-in people, Forma): the rigged figures and the one shared AnimatorController the play scene draws people with.
    /// Built in the Editor from briefs/020/people/manifest.json (Suborned/People/Build Figure Library) and baked onto the host; view only.</summary>
    [CreateAssetMenu(menuName = "Suborned/Figure Library")]
    public sealed class FigureLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Figure
        {
            public string id = "";
            [Tooltip("The figure's FBX model (skeleton, LOD meshes, its Humanoid avatar).")]
            public GameObject model;
            [Tooltip("LODGroup screen heights from the manifest (lodScreenHeights), LOD0 first.")]
            public float[] lodHeights = Array.Empty<float>();
            [Tooltip("Walks with walk_f.")]
            public bool female;
        }

        public Figure[] figures = Array.Empty<Figure>();
        [Tooltip("One controller for every figure: a state per clip, named as the clip (idle, walk, run, lying, carried, ...).")]
        public RuntimeAnimatorController controller;
        [Tooltip("Clips' natural ground speeds (m/s) by state name, for the stride (manifest naturalSpeedMs).")]
        public string[] speedStates = Array.Empty<string>(); public float[] speeds = Array.Empty<float>();
        [Tooltip("Pools by key (from the manifest's playRoles, families and jobBlocks): 'role/<role>', 'family/<family>', 'job/<job>' → figure ids.")]
        public string[] poolKeys = Array.Empty<string>(); public string[] poolIds = Array.Empty<string>(); // poolIds[i]: comma-joined

        [Tooltip("Forma round 2: the serving tray (manifest props 'tray'), held in the right hand while a server plays serve or serve_walk.")]
        public GameObject tray;
        [Tooltip("The tray's seat on each figure's RightHand bone (manifest props[tray].attach.perFigure): figure ids, local positions, local Euler angles.")]
        public string[] trayFigures = Array.Empty<string>(); public Vector3[] trayPos = Array.Empty<Vector3>(), trayEuler = Array.Empty<Vector3>();
        public int TraySeat(string id) => Array.IndexOf(trayFigures, id);

        public Figure Find(string id) { foreach (var f in figures) if (f.id == id) return f; return null; }
        public float Speed(string state) { for (int i = 0; i < speedStates.Length; i++) if (speedStates[i] == state) return speeds[i]; return 0f; }

        /// <summary>A figure id from a pool, the same for the same seed (a person's id) every run.</summary>
        public string Pick(string pool, string seed)
        {
            for (int i = 0; i < poolKeys.Length; i++)
                if (poolKeys[i] == pool)
                {
                    var ids = poolIds[i].Split(','); if (ids.Length == 0) return null;
                    uint h = 2166136261; foreach (var c in seed ?? "") { h ^= c; h *= 16777619; } // FNV-1a: stable across runs and builds
                    return ids[h % (uint)ids.Length];
                }
            return null;
        }
    }
}
