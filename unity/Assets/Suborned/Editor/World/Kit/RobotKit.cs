using System;
using System.Collections.Generic;
using System.IO;
using Suborned.Robots;
using UnityEditor;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Brief 026, robots alive: the builder side of RobotRigs. The rig table is Vela's face data (tools/decokit/art/robots/
    /// robot-faces.json, suborned.robotfaces/1; unknown schema = reject) and the glyph sheet the Unity copy of its RobotGlyphs.txt
    /// (Runtime/Robots/RobotGlyphs.txt, the old asset's GUID so the kit's robot prefabs keep their reference; the build fails if the two
    /// sheets differ). KitLods rigs every adopted robot piece's Built prefab (face + servo head); after the scene is built, Seeds gives each
    /// placed robot its schedule seed: its path under the build root (export ids), so the same export gives the same glances everywhere.
    /// Held moods (Vela's `moodsHeld`, any `needsSky` glyph: heart, star, wink; §15 no cartoon eyes) stay out unless the batch argument
    /// -robotsHeld is given for a review render. View only: the sim never reads any of it.</summary>
    public static class RobotKit
    {
        public const string GlyphsPath = "Assets/Suborned/Runtime/Robots/RobotGlyphs.txt";
        public static string FacesPath => Path.Combine(HarbourWorldBuilder.RepoRoot, "tools", "decokit", "art", "robots", "robot-faces.json");
        public static string SourceGlyphsPath => Path.Combine(HarbourWorldBuilder.RepoRoot, "tools", "decokit", "art", "robots", "RobotGlyphs.txt");
        public static bool HeldWanted => Array.IndexOf(Environment.GetCommandLineArgs(), "-robotsHeld") >= 0;

        static RobotRigs.Table table;
        public static int HeldDropped { get; private set; }

        public static TextAsset Glyphs
        {
            get
            {
                if (File.Exists(SourceGlyphsPath) && Norm(File.ReadAllText(SourceGlyphsPath)) != Norm(File.ReadAllText(Path.Combine(HarbourWorldBuilder.RepoRoot, "unity", GlyphsPath))))
                    throw new InvalidDataException($"World: robots: {GlyphsPath} differs from Vela's {SourceGlyphsPath}: copy it over (keep the .meta)");
                return AssetDatabase.LoadAssetAtPath<TextAsset>(GlyphsPath) ?? throw new FileNotFoundException($"World: robots: {GlyphsPath} missing");
            }
        }

        static string Norm(string s) => s.Replace("\r\n", "\n").TrimEnd();

        /// <summary>Vela's rig table, read once per domain reload (a recompile re-reads it).</summary>
        public static RobotRigs.Table Table
        {
            get
            {
                if (table != null) return table;
                var root = Json.Parse(File.ReadAllText(FacesPath));
                if (J.S(root, "schema") != "suborned.robotfaces/1") throw new InvalidDataException($"World: robots: {FacesPath} schema {J.S(root, "schema")}, want suborned.robotfaces/1");
                var held = new HashSet<string>(StringComparer.Ordinal);
                foreach (var kv in J.O(J.Get(root, "glyphs"))) if (J.B(kv.Value, "needsSky")) held.Add(kv.Key);
                var tm = J.Get(root, "timing");
                var t = new RobotRigs.Table
                {
                    moodEvery = J.P(tm, "moodEvery"), moodHold = J.P(tm, "moodHold"), blinkEvery = J.P(tm, "blinkEvery"),
                    lead = J.F(tm, "lead"), gaze = Mathf.RoundToInt(J.F(tm, "gazeCells")), heldIncluded = HeldWanted,
                };
                var rigs = new List<RobotRigs.Rig>(); HeldDropped = 0;
                foreach (var kv in J.O(J.Get(root, "robots")))
                {
                    var r = kv.Value; var g = J.P(r, "grid");
                    var moods = new List<RobotRigs.Face>();
                    foreach (var m in J.A(r, "moods")) Add(moods, m, held, t.heldIncluded);
                    if (J.Get(r, "moodsHeld") is List<object> mh) foreach (var m in mh) Add(moods, m, held, t.heldIncluded, true);
                    rigs.Add(new RobotRigs.Rig
                    {
                        kind = kv.Key, range = J.F(r, "range"), turn = J.F(r, "turn"), home = J.F(r, "home"), corner = J.F(r, "corner"),
                        hold = J.P(r, "hold"), eye = J.P(r, "eye"), mouth = J.P(r, "mouth"), grid = new Vector2Int(Mathf.RoundToInt(g.x), Mathf.RoundToInt(g.y)),
                        neutral = Face(J.Get(r, "neutral")), angry = Face(J.Get(r, "angry")), moods = moods.ToArray(), attention = Stages(r),
                    });
                }
                t.rigs = rigs.ToArray();
                Debug.Log($"World: robots: {t.rigs.Length} rigs from {Path.GetFileName(FacesPath)}; held moods {(t.heldIncluded ? "INCLUDED (-robotsHeld, review only)" : $"left out ({HeldDropped}; §15, needs Sky)")}");
                return table = t;
            }
        }

        /// <summary>Brief 031: the robot's `attention.stages` in stage order (0 calm .. 3 reporting); a stage glyph the sheet lacks = reject.</summary>
        static RobotRigs.Face[] Stages(object r)
        {
            if (J.Get(r, "attention") is not object at || J.Get(at, "stages") is not List<object> list) return Array.Empty<RobotRigs.Face>();
            var o = new RobotRigs.Face[4];
            foreach (var st in list)
            {
                int k = Mathf.RoundToInt(J.F(st, "stage")); if (k < 0 || k > 3) throw new InvalidDataException($"World: robots: attention stage {k} (want 0..3)");
                o[k] = Face(st);
            }
            for (int k = 0; k < 4; k++) if (o[k] == null) throw new InvalidDataException($"World: robots: attention stage {k} missing");
            return o;
        }

        static RobotRigs.Face Face(object f) => f == null ? null : new RobotRigs.Face { left = J.S(f, "left") ?? "", right = J.S(f, "right") ?? "", mouth = J.S(f, "mouth") ?? "" };

        static void Add(List<RobotRigs.Face> into, object m, HashSet<string> held, bool allow, bool markedHeld = false)
        {
            var f = Face(m);
            bool isHeld = markedHeld || J.B(m, "needsSky") || held.Contains(f.left) || held.Contains(f.right) || held.Contains(f.mouth) || f.left != f.right;
            if (isHeld && !allow) { HeldDropped++; return; }
            into.Add(f);
        }

        // ---------------------------------------------------------------- brief 031: the patrol robot's stage face

        public const string StageMaterialName = "M_RobotFace";
        const string StageMaterialPath = "Assets/Suborned/Materials/Robots/M_RobotFace.mat", StageAtlasPath = KitPlacer.KitRoot + "Textures/robot_face_stage_v1.png";
        const string StageShader = "DecoKit/RobotFaceStage";

        /// <summary>M_RobotFace (brief 031, Vela's kit piece PatrolRobot names it; robot-face-stage.json): DK_RobotFaceStage.shader with the stage
        /// atlas robot_face_stage_v1.png (copied by `bun run kit`; point, no mips, linear) and Vela's colours (phosphor #BFF3E6 x1.6, alarm
        /// #FF5A3C x2.2, linear HDR). Made or refreshed on demand, committed under Materials/Robots; the kit's FBX remaps its slot onto it.</summary>
        public static Material StageMaterial()
        {
            var shader = Shader.Find(StageShader) ?? throw new InvalidDataException($"World: robots: shader {StageShader} missing (Shaders/DecoKit/DK_RobotFaceStage.shader)");
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(StageAtlasPath);
            if (!atlas && File.Exists(Path.Combine(HarbourWorldBuilder.RepoRoot, "unity", StageAtlasPath))) { AssetDatabase.ImportAsset(StageAtlasPath, ImportAssetOptions.ForceSynchronousImport); atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(StageAtlasPath); }
            if (!atlas) throw new FileNotFoundException($"World: robots: {StageAtlasPath} missing (run `bun run kit`)");
            if (AssetImporter.GetAtPath(StageAtlasPath) is TextureImporter ti && (ti.filterMode != FilterMode.Point || ti.mipmapEnabled || ti.sRGBTexture || ti.textureCompression != TextureImporterCompression.Uncompressed))
            { ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.sRGBTexture = false; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.SaveAndReimport(); atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(StageAtlasPath); }
            var m = AssetDatabase.LoadAssetAtPath<Material>(StageMaterialPath);
            if (!m)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Suborned/Materials/Robots")) AssetDatabase.CreateFolder("Assets/Suborned/Materials", "Robots");
                m = new Material(shader) { name = StageMaterialName }; AssetDatabase.CreateAsset(m, StageMaterialPath);
            }
            if (m.shader != shader) m.shader = shader;
            static Color Hdr(string hex, float k) { ColorUtility.TryParseHtmlString(hex, out var c); var l = c.linear * k; l.a = 1f; return l; }
            m.SetTexture("_Atlas", atlas);
            m.SetColor("_Phosphor", Hdr("#BFF3E6", 1.6f).gamma); m.SetColor("_Alarm", Hdr("#FF5A3C", 2.2f).gamma); // SetColor takes gamma; the shader gets the linear HDR value
            m.SetFloat("_Ghost", .05f); m.SetFloat("_Dot", .36f); m.SetFloat("_GazeCells", 2f);
            EditorUtility.SetDirty(m); AssetDatabase.SaveAssetIfDirty(m);
            return m;
        }

        /// <summary>Rigs a robot piece (kit id RobotButler, RobotButler_Play, ...); false when the id names no rig.</summary>
        public static bool Rig(GameObject go, string id)
        {
            var kind = RobotRigs.KindOf(id);
            if (kind == null || Table.Find(kind) == null) return false;
            var face = RobotRigs.Attach(go, Table, Glyphs, "", kind);
            if (face == null) return false;
            if (face.Screens == 0) Debug.LogWarning($"World: robot `{id}` has a rig but no *_Face renderer: its face stays dark");
            if (go.GetComponent<RobotIdle>().Heads == 0) Debug.LogWarning($"World: robot `{id}` has a rig but no *_Head part: its head stays still");
            return true;
        }

        /// <summary>Seeds every placed robot by its path under `root` (export ids); returns how many.</summary>
        public static int Seeds(Transform root)
        {
            int n = 0;
            foreach (var face in root.GetComponentsInChildren<RobotFace>(true))
            {
                face.Seed = PathOf(face.transform, root);
                var idle = face.GetComponent<RobotIdle>(); if (idle) idle.Reseed();
                face.PoseAt(0f, 0);
                if (PrefabUtility.IsPartOfPrefabInstance(face)) PrefabUtility.RecordPrefabInstancePropertyModifications(face);
                EditorUtility.SetDirty(face);
                n++;
            }
            return n;
        }

        static string PathOf(Transform t, Transform root)
        {
            var s = t.name;
            for (var p = t.parent; p && p != root; p = p.parent) s = p.name + "/" + s;
            return s + "#" + t.GetSiblingIndex(); // two robots of one element (a terrace pair) differ by their place among its children
        }

        [InitializeOnLoadMethod] static void Reset() => table = null;
    }
}
