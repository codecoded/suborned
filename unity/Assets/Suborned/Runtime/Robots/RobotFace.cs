using System;
using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Robots
{
    /// <summary>
    /// A service robot's dot-matrix display face (DK_RobotFace.shader on the model's DK_*_Face screens): two eye glyphs and a mouth
    /// glyph from RobotGlyphs.txt on the display's LED grid (§15: dot-matrix display faces, no cartoon eyes). It blinks (the eye rows
    /// switch off toward the middle, then one bar), its eyes jump toward where the head is about to turn (the gaze RobotIdle passes in),
    /// and every so often it shows another expression. Alarmed (Alarm) it flickers for a quarter second, then stares: the LEDs turn red
    /// and pulse, the face turns angry, and it stops blinking.
    ///
    /// Ported from the old project's Scripts/Robots/RobotFace.cs (brief 026; same script GUID, so the kit's robot prefabs keep their
    /// serialized faces). View only: a pure function of time on a looping schedule seeded by `seed` (the world element's id, set by the
    /// builder or the play host; else, as the old one, by where the robot stands). Nothing here is read by the sim, so replays are
    /// untouched. The rig numbers come from robots.json (RobotRigs); in edit mode, the neutral face.
    /// </summary>
    [ExecuteAlways]
    public sealed class RobotFace : MonoBehaviour
    {
        [Serializable]
        public struct Expression
        {
            public string left, right, mouth;  // glyph names; "" leaves it dark
            public Expression(string eyes, string mouth) : this(eyes, eyes, mouth) { }
            public Expression(string left, string right, string mouth) { this.left = left; this.right = right; this.mouth = mouth; }
        }

        [SerializeField] TextAsset glyphs;
        [SerializeField] string seed = "";                    // stable id (world element / host key); "" = by position
        [SerializeField] Vector2Int grid = new(21, 7);        // LED cells across and up the display
        [SerializeField] float corner = 2f;                   // the display's corner radius, cells
        [SerializeField] Vector2 eye = new(5f, .3f);          // the viewer's-right eye centre from the grid centre, cells
        [SerializeField] Vector2 mouth = new(0f, -3f);
        [SerializeField] Expression neutral = new("round", "smile");
        [SerializeField] Expression[] moods = Array.Empty<Expression>();
        [SerializeField] Vector2 moodEvery = new(8f, 18f), moodHold = new(1.4f, 2.6f);
        [SerializeField] Vector2 blinkEvery = new(2.5f, 5.5f);
        [SerializeField] Expression angry = new("angry_l", "angry_r", "grit");
        [SerializeField, ColorUsage(false, true)] Color alarmGlow = new(2.6f, .1f, .04f);  // linear HDR red
        // brief 031 (Vela's robot-faces.json `attention`): the watcher's four stages on this robot's own display: calm (its moods and blinks),
        // noticed (wide eyes on the agent), wary (narrowed, brows down), reporting (the alarm pictogram, flashing 0.3 s on, 0.2 s off)
        [SerializeField] Expression[] stages = Array.Empty<Expression>();
        [SerializeField, ColorUsage(false, true)] Color reportGlow = new(2.2f, .22f, .1f);  // #FF5A3C at 2.2, linear

        const int Blinks = 24, Moods = 8;
        const float Half = .04f, Shut = .07f, Double = .26f;  // a blink: 40 ms half, 70 ms bar, 40 ms half
        const float Flicker = .25f, FlickerHz = 16f, PulseHz = 1.5f, Pulse = .25f;  // alarm: in at 16 Hz, then +-25% at 1.5 Hz

        static readonly int GlyphsId = Shader.PropertyToID("_DK_RobotGlyphs"), GridId = Shader.PropertyToID("_Grid"),
            EyeLId = Shader.PropertyToID("_EyeL"), EyeRId = Shader.PropertyToID("_EyeR"), MouthId = Shader.PropertyToID("_Mouth"),
            GlowId = Shader.PropertyToID("_Glow");
        static readonly Dictionary<TextAsset, Atlas> Atlases = new();

        // runtime caches: never serialized (a domain reload would bring them back as empty arrays, not null)
        [NonSerialized] Renderer[] screens;
        [NonSerialized] Color[] glow;  // each screen material's own LED colour
        [NonSerialized] Vector4 lastKey; [NonSerialized] bool posed; // the face last set (PoseAt sets the block only on a change)
        [NonSerialized] MaterialPropertyBlock block;
        [NonSerialized] Atlas atlas;
        [NonSerialized] float[] blinkAt, moodAt, moodFor;
        [NonSerialized] int[] moodIs;
        [NonSerialized] float blinkLoop, moodLoop, alarmAt = float.PositiveInfinity, calmAt = float.PositiveInfinity;
        [NonSerialized] int stage, stageGaze;

        /// <summary>When the first expression change comes (for captures that want one in frame).</summary>
        public float FirstMoodAt => moods.Length > 0 && (screens != null || Build()) ? moodAt[0] : 0f;
        /// <summary>Screens this face drives (0 = the model has no DK_*_Face renderer).</summary>
        public int Screens => screens != null || Build() ? screens.Length : 0;
        public bool Alarmed(float seconds) => seconds >= alarmAt && seconds < calmAt;

        public string Seed { get => seed; set { if (seed != value) { seed = value ?? ""; screens = null; } } }

        public void Configure(TextAsset glyphSheet, Vector2Int cells, float cornerCells, Vector2 eyeAt, Vector2 mouthAt,
                              Expression rest, Expression[] others, Expression alarmed, Vector2 moodEverySeconds, Vector2 moodHoldSeconds, Vector2 blinkEverySeconds)
        {
            glyphs = glyphSheet; grid = cells; corner = cornerCells; eye = eyeAt; mouth = mouthAt; neutral = rest; moods = others ?? Array.Empty<Expression>();
            angry = alarmed; moodEvery = moodEverySeconds; moodHold = moodHoldSeconds; blinkEvery = blinkEverySeconds;
            screens = null;
            PoseAt(0f, 0);
        }

        void OnEnable()
        {
            screens = null;
            PoseAt(0f, 0);
        }

        void OnValidate() => screens = null;

        /// <summary>Brief 031: the attention stages this face can show (index 0 calm .. 3 reporting); fewer than four = it shows none.</summary>
        public void SetStages(Expression[] attention) { stages = attention ?? Array.Empty<Expression>(); }
        public bool HasStages => stages != null && stages.Length >= 4;
        /// <summary>Brief 031: the watcher's displayed stage (0 calm, 1 noticed, 2 wary, 3 reporting) and the eyes' shift toward the agent in
        /// cells (stages 1 and 2; + toward the viewer's right). View only: the host drives it from the engine's Attention.</summary>
        public void Stage(int s, int gazeCells) { stage = HasStages ? Mathf.Clamp(s, 0, 3) : 0; stageGaze = gazeCells; }
        public int StageNow => stage;

        /// <summary>Turn hostile from `seconds` on this robot's clock (Time.time in play) until Calm.</summary>
        public void Alarm(float seconds) { alarmAt = seconds; calmAt = float.PositiveInfinity; }

        public void Calm(float seconds) => calmAt = seconds;

        /// <summary>Pose at `seconds` on this robot's schedule with the eyes shifted `gaze` cells toward the viewer's right.</summary>
        public void PoseAt(float seconds, int gaze)
        {
            if ((screens == null || blinkAt == null || blinkAt.Length == 0) && !Build()) return;
            if (screens.Length == 0) return;
            float since = seconds - alarmAt;
            bool alarmed = since >= 0f && seconds < calmAt && (since >= Flicker || (int)(since * FlickerHz) % 2 == 1);
            var e = alarmed ? angry : Mood(seconds);  // t = 0 is always the neutral face, eyes open
            float lid = alarmed ? 0f : Lid(seconds);
            var red = (alarmGlow * (1f + Pulse * Mathf.Sin(2f * Mathf.PI * PulseHz * Mathf.Max(since - Flicker, 0f)))).gamma;
            bool staged = stage > 0 && HasStages, report = staged && stage == 3;
            if (staged) { e = stages[stage]; lid = 0f; gaze = stage < 3 ? stageGaze : 0; } // brief 031: the stage outranks the old alarm glare (its wary eyes are the glare)
            var reportCol = (reportGlow * (Mathf.Repeat(seconds, .5f) < .3f ? 1f : .12f)).gamma; // 2 Hz: 0.3 s on, 0.2 s off
            Vector2 mouthAt = report ? Vector2.zero : mouth; // the pictogram sits on the display's centre
            Vector4 l = Place(e.left, new Vector2(-eye.x, eye.y), lid, gaze), r = Place(e.right, eye, lid, gaze), m = Place(e.mouth, mouthAt, 0f, 0);
            Shader.SetGlobalTexture(GlyphsId, atlas.texture);
            // brief 045 F6 / 048 item 4: a property block set on a renderer every frame made the GPU Resident Drawer rebuild its draw lists every
            // frame (CPUDrawInstanceData.RebuildDrawListsIfNeeded, MeshRendererProcessor.DestroyInstances): set it only when the face changes
            var key = new Vector4(l.x + l.y * 31f + l.z * 977f + l.w * 7919f, r.x + r.y * 31f + r.z * 977f + r.w * 7919f, m.x + m.y * 31f + m.z * 977f + m.w * 7919f, report ? 1f + (Mathf.Repeat(seconds, .5f) < .3f ? 1f : 0f) : alarmed && !staged ? 10f + Mathf.Round(Mathf.Sin(2f * Mathf.PI * PulseHz * Mathf.Max(since - Flicker, 0f)) * 8f) : 0f);
            if (posed && key == lastKey) return;
            posed = true; lastKey = key;
            for (int i = 0; i < screens.Length; i++)
            {
                var s = screens[i];
                if (!s) continue;
                s.GetPropertyBlock(block);
                block.SetColor(GlowId, report ? reportCol : alarmed && !staged ? red : glow[i]);  // pre-encoded, as the materials
                block.SetVector(GridId, new Vector4(grid.x, grid.y, corner, 0f));
                block.SetVector(EyeLId, l);
                block.SetVector(EyeRId, r);
                block.SetVector(MouthId, m);
                s.SetPropertyBlock(block);
            }
        }

        /// <summary>(origin cell x, y, atlas tile, blink) of a glyph centred `at` cells from the grid centre. The left eye is the right
        /// eye's mirror image cell for cell, so an odd glyph on an even grid still sits symmetric.</summary>
        Vector4 Place(string name, Vector2 at, float lid, int gaze)
        {
            if (string.IsNullOrEmpty(name) || !atlas.index.TryGetValue(name, out var tile)) return new Vector4(0f, 0f, -1f, 0f);
            var size = atlas.size[tile];
            int x = Mathf.FloorToInt(grid.x * .5f + Mathf.Abs(at.x) - size.x * .5f + .5f);
            if (at.x < 0f) x = grid.x - size.x - x;
            int y = Mathf.FloorToInt(grid.y * .5f + at.y - size.y * .5f + .5f);
            return new Vector4(x + gaze, y, tile, lid);
        }

        Expression Mood(float t)
        {
            if (moods.Length == 0) return neutral;
            t = Mathf.Repeat(t, moodLoop);
            for (int i = 0; i < Moods; i++)
                if (t >= moodAt[i] && t < moodAt[i] + moodFor[i]) return moods[moodIs[i]];
            return neutral;
        }

        float Lid(float t)
        {
            t = Mathf.Repeat(t, blinkLoop);
            int i = 0;
            while (i < Blinks - 1 && blinkAt[i + 1] <= t) i++;
            float dt = t - blinkAt[i];
            return dt < Half ? 1f : dt < Half + Shut ? 2f : dt < Half + Shut + Half ? 1f : 0f;
        }

        /// <summary>The schedule's seed: FNV-1a of the id when there is one (stable across builds and machines), else the old rule (position).</summary>
        internal static int SeedOf(string id, Vector3 p, int salt, float kx, float kz, int prime)
        {
            if (string.IsNullOrEmpty(id)) return Mathf.RoundToInt(p.x * kx) ^ Mathf.RoundToInt(p.z * kz) * prime;
            unchecked
            {
                uint h = 2166136261u ^ (uint)salt;
                foreach (char c in id) { h ^= c; h *= 16777619u; }
                return (int)(h & 0x7fffffff);
            }
        }

        bool Build()
        {
            posed = false; // new screens: set the block again
            if (glyphs == null) return false;
            if (!Atlases.TryGetValue(glyphs, out atlas) || atlas.texture == null) Atlases[glyphs] = atlas = Atlas.Parse(glyphs.text);
            screens = Array.FindAll(GetComponentsInChildren<Renderer>(true), s => s.name.EndsWith("_Face"));
            glow = Array.ConvertAll(screens, s => s.sharedMaterial && s.sharedMaterial.HasProperty(GlowId) ? s.sharedMaterial.GetColor(GlowId) : Color.black);
            block ??= new MaterialPropertyBlock();

            var rng = new System.Random(SeedOf(seed, transform.position, 1, 31.7f, 57.3f, 6151));
            float R(Vector2 v) => v.x + (v.y - v.x) * (float)rng.NextDouble();
            blinkAt = new float[Blinks];
            blinkAt[0] = -1f;  // never mid-blink at t = 0
            for (int i = 1; i < Blinks; i++)  // one in five comes as a double blink
                blinkAt[i] = blinkAt[i - 1] + (i > 1 && blinkAt[i - 1] - blinkAt[i - 2] > Double && rng.NextDouble() < .2
                    ? Double : R(blinkEvery));
            blinkLoop = blinkAt[^1] + R(blinkEvery);
            moodAt = new float[Moods];
            moodFor = new float[Moods];
            moodIs = new int[Moods];
            float at = 0f;
            for (int i = 0; i < Moods; i++)
            {
                moodAt[i] = at += R(moodEvery);
                moodFor[i] = R(moodHold);
                moodIs[i] = rng.Next(Math.Max(moods.Length, 1));
                at += moodFor[i];
            }
            moodLoop = at + R(moodEvery);
            return true;
        }

        /// <summary>RobotGlyphs.txt packed into 8 x 8 tiles in a row: r = lit, g = the glyph's height / 8, glyphs bottom-left.</summary>
        sealed class Atlas
        {
            public Texture2D texture;
            public readonly Dictionary<string, int> index = new();
            public readonly List<Vector2Int> size = new();

            public static Atlas Parse(string text)
            {
                var a = new Atlas();
                var rows = new List<List<string>>();
                foreach (var raw in text.Split('\n'))
                {
                    var line = raw.Trim();
                    if (line.StartsWith("="))
                    {
                        a.index[line.Substring(1).Trim()] = rows.Count;
                        rows.Add(new List<string>());
                    }
                    else if (line.Length > 0 && !line.StartsWith(";") && rows.Count > 0) rows[^1].Add(line);
                }
                var px = new Color32[Mathf.Max(rows.Count, 1) * 8 * 8];
                int w = Mathf.Max(rows.Count, 1) * 8;
                for (int k = 0; k < rows.Count; k++)
                {
                    var g = rows[k];
                    int h = Mathf.Min(g.Count, 8), gw = 0;
                    for (int j = 0; j < h; j++)
                    {
                        gw = Mathf.Max(gw, Mathf.Min(g[j].Length, 8));
                        for (int i = 0; i < Mathf.Min(g[j].Length, 8); i++)
                            if (g[j][i] == '#') px[(h - 1 - j) * w + k * 8 + i].r = 255;  // top row first in the file
                    }
                    for (int y = 0; y < 8; y++)
                        for (int x = 0; x < 8; x++) px[y * w + k * 8 + x].g = (byte)(h * 255 / 8);
                    a.size.Add(new Vector2Int(gw, h));
                }
                a.texture = new Texture2D(w, 8, TextureFormat.RGBA32, false, true)
                {
                    name = "DK_RobotGlyphs", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave,
                };
                a.texture.SetPixels32(px);
                a.texture.Apply(false, true);
                return a;
            }
        }
    }
}
