using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Robots
{
    /// <summary>The robots' rig table and the one call that brings a robot model to life: Attach adds or re-configures RobotFace + RobotIdle
    /// on a model whose name names a rig (DK_RobotButler, DK_RobotButler_Play, ...). The table is Vela's face data
    /// (tools/decokit/art/robots/robot-faces.json, suborned.robotfaces/1, generated from the old DecoKitRobots table), read at build time by
    /// the editor (RobotKit) and serialized where it is used (the Built robot prefabs, the play host), so the player needs no JSON. Held
    /// moods (§15: dot-matrix faces, no cartoon eyes: heart, star, wink; `moodsHeld` / `needsSky` in Vela's file) are never in `moods` here
    /// unless the build was asked for them. View only: nothing here feeds the sim.</summary>
    public static class RobotRigs
    {
        [Serializable] public sealed class Face { public string left = "", right = "", mouth = ""; }
        [Serializable]
        public sealed class Rig
        {
            public string kind;
            public float range = 45f, turn = .45f, home = .35f, corner = 2f;
            public Vector2 hold = new(1.2f, 3.5f), eye = new(5f, .3f), mouth = new(0f, -3f);
            public Vector2Int grid = new(21, 7);
            public Face neutral = new(), angry = new();
            public Face[] moods = Array.Empty<Face>();
            /// <summary>Brief 031: Vela's `attention.stages` (calm, noticed, wary, reporting), the watcher's four faces on this display.</summary>
            public Face[] attention = Array.Empty<Face>();
        }
        [Serializable]
        public sealed class Table
        {
            public Vector2 moodEvery = new(8, 18), moodHold = new(1.4f, 2.6f), blinkEvery = new(2.5f, 5.5f);
            public float lead = .15f; public int gaze = 2;
            public bool heldIncluded; // true only for a review build that asked for the held moods
            public Rig[] rigs = Array.Empty<Rig>();
            [NonSerialized] Dictionary<string, Rig> byKind;
            public Rig Find(string kind)
            {
                if (byKind == null) { byKind = new Dictionary<string, Rig>(StringComparer.Ordinal); foreach (var r in rigs) byKind[r.kind] = r; }
                return kind != null && byKind.TryGetValue(kind, out var rig) ? rig : null;
            }
        }

        /// <summary>The rig a model name stands for: DK_RobotButler, DK_RobotButler_Play, DK_RobotButler_Play_L1, "DK_RobotButler (1)" → RobotButler.</summary>
        public static string KindOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var n = name;
            int sp = n.IndexOf(' '); if (sp > 0) n = n.Substring(0, sp);
            if (n.StartsWith("DK_")) n = n.Substring(3);
            int us = n.IndexOf('_'); if (us > 0) n = n.Substring(0, us);
            return n.StartsWith("Robot") ? n : null;
        }

        static bool Given(Face f) => f != null && !(string.IsNullOrEmpty(f.left) && string.IsNullOrEmpty(f.right) && string.IsNullOrEmpty(f.mouth));
        static RobotFace.Expression E(Face f, RobotFace.Expression fallback) => !Given(f) ? fallback : new RobotFace.Expression(f.left ?? "", f.right ?? "", f.mouth ?? "");

        /// <summary>Adds or configures the face and the head on `go` from its rig (kind, else go's name). Returns the face, or null when
        /// no rig matches. `seed` (a world element path, a host key) fixes the schedule.</summary>
        public static RobotFace Attach(GameObject go, Table t, TextAsset glyphs, string seed, string kind = null)
        {
            if (t == null || glyphs == null) return null;
            var r = t.Find(kind ?? KindOf(go.name));
            if (r == null) return null;
            var moods = new List<RobotFace.Expression>();
            foreach (var m in r.moods) if (Given(m)) moods.Add(E(m, default));
            if (!go.TryGetComponent<RobotFace>(out var face)) face = go.AddComponent<RobotFace>();
            face.Seed = seed ?? "";
            face.Configure(glyphs, r.grid, r.corner, r.eye, r.mouth, E(r.neutral, new RobotFace.Expression("round", "smile")), moods.ToArray(),
                E(r.angry, new RobotFace.Expression("angry_l", "angry_r", "grit")), t.moodEvery, t.moodHold, t.blinkEvery);
            var st = new RobotFace.Expression[r.attention?.Length ?? 0];
            for (int i = 0; i < st.Length; i++) st[i] = new RobotFace.Expression(r.attention[i].left ?? "", r.attention[i].right ?? "", r.attention[i].mouth ?? "");
            face.SetStages(st); // brief 031: the attention faces (empty: the robot shows no stage)
            if (!go.TryGetComponent<RobotIdle>(out var idle)) idle = go.AddComponent<RobotIdle>();
            idle.Configure(face, r.range, r.turn, r.hold, r.home, t.lead, t.gaze);
            return face;
        }
    }
}
