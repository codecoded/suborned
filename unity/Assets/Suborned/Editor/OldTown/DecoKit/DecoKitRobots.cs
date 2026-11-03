#if UNITY_EDITOR
using System.Collections.Generic;
using Suborned.OldTown.Robots;
using UnityEditor;
using UnityEngine;
using E = Suborned.OldTown.Robots.RobotFace.Expression;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// Brings the service robot prefabs to life: RobotIdle turns the head (DK_*_Head), RobotFace draws the display
    /// (DK_*_Face), and RobotIdle.Alarm(suspect) turns both on someone, red-eyed. Personality lives in the numbers:
    /// the butler glances rarely, slowly and not far; the porter snaps round like a turret; the nanny mostly keeps
    /// her eyes on the pram. LED grids are the Face screen sizes in robots.py divided by the LED pitch the old bead
    /// faces used, so the neutral faces match them cell for cell.
    internal static class DecoKitRobots
    {
        const string Glyphs = "Assets/Suborned/OldTownArt/Data/RobotGlyphs.txt";

        sealed class Rig
        {
            public float range, turn, home, corner;
            public Vector2 hold, eye, mouth;
            public Vector2Int grid;
            public E neutral;
            public E[] moods;
            public E angry = new("angry_l", "angry_r", "grit");  // alarmed: red, glaring, teeth gritted
        }

        static readonly Dictionary<string, Rig> Rigs = new()
        {
            ["RobotButler"] = new Rig
            {
                range = 35f, turn = .7f, hold = new(2f, 5f), home = .5f,
                grid = new(21, 7), corner = 1.7f, eye = new(5f, .3f), neutral = new E("calm", ""), angry = new E("angry_l", "angry_r", ""),
                moods = new[] { new E("happy", ""), new E("calm", "happy", "") },  // a smile of the eyes; a wink
            },
            ["RobotPorter"] = new Rig
            {
                range = 70f, turn = .35f, hold = new(.8f, 2.5f), home = .3f,
                grid = new(30, 13), corner = 2.3f, eye = new(6.8f, 2.3f), mouth = new(0f, -3.2f), neutral = new E("wide", "grin"),
                moods = new[] { new E("happy", "beam"), new E("wide", "o"), new E("star", "grin") },
            },
            ["RobotWaiter"] = new Rig
            {
                range = 50f, turn = .4f, hold = new(1f, 3f), home = .35f,
                grid = new(15, 12), corner = 5f, eye = new(3.5f, 2.1f), mouth = new(0f, -2.9f), neutral = new E("round", "smile"),
                moods = new[] { new E("happy", "grin"), new E("round", "happy", "smile"), new E("round", "o") },
            },
            ["RobotNanny"] = new Rig
            {
                range = 30f, turn = .6f, hold = new(1.5f, 4f), home = .6f,
                grid = new(26, 15), corner = 7f, eye = new(6f, 1.8f), mouth = new(0f, -3.4f), neutral = new E("happy", "smile"),
                moods = new[] { new E("heart", "smile"), new E("round", "smile"), new E("happy", "beam") },
            },
            ["RobotShopkeeper"] = new Rig
            {
                range = 55f, turn = .45f, hold = new(1f, 3f), home = .4f,
                grid = new(24, 9), corner = 1.6f, eye = new(5.3f, 1.3f), mouth = new(0f, -3.2f), neutral = new E("round", "smile"),
                moods = new[] { new E("happy", "beam"), new E("round", "happy", "smile"), new E("star", "smile") },
            },
        };

        internal static void Apply(GameObject go, string asset)
        {
            if (!Rigs.TryGetValue(asset, out var r)) return;
            var glyphs = AssetDatabase.LoadAssetAtPath<TextAsset>(Glyphs);
            if (glyphs == null) Debug.LogError($"[DecoKit] {asset}: no robot glyphs at {Glyphs}");
            var face = go.AddComponent<RobotFace>();
            face.Configure(glyphs, r.grid, r.corner, r.eye, r.mouth, r.neutral, r.moods, r.angry);
            go.AddComponent<RobotIdle>().Configure(face, r.range, r.turn, r.hold, r.home);
        }
    }
}
#endif
