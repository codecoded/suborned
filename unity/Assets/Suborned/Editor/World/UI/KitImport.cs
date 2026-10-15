using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Suborned.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Path = System.IO.Path;

namespace Suborned.World.UI
{
    /// <summary>Glyph's HUD kit (briefs/011/ui/kit: layout.json, sprites.json, the PNGs at @1x and @2x, the OFL fonts) into Unity for the UI Toolkit
    /// view (HudToolkit), under Assets/Suborned/UI/Resources/Kit so the runtime loads it with Resources:
    ///  · the @2x PNGs, imported as sprites with the kit's @2x nine-slice insets as their borders (the view draws them at slice scale 0.5, so
    ///    1080p is 1 px a texel-pair and 4K is sharp); the icons from icons/singles; the static font cuts (Jost, Barlow) and their licences;
    ///  · layout.json and sprites.json as they are (the view reads rects and windows from them);
    ///  · kit.uss, generated from layout.json: the 34 colours as variables, the 62 type styles as .t-&lt;name&gt; classes (font cut, size, colour,
    ///    letter spacing), each sprite as .s-&lt;name&gt; (background and slices), the button states (:hover, :active) and the scene kinds;
    ///  · HudTheme.tss (imports kit.uss) and HudPanel.asset: scale with screen size from 1920 x 1080, match height (the kit's reference).
    /// Idempotent: files are copied only when their bytes differ.</summary>
    public static class KitImport
    {
        public static string KitDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "briefs", "011", "ui", "kit"));
        public const string Dest = "Assets/Suborned/UI/Resources/Kit";

        [MenuItem("Suborned/UI/Import Glyph's Kit")]
        public static void Menu() => Debug.Log(Import());

        public static string Import()
        {
            if (!Directory.Exists(KitDir)) throw new DirectoryNotFoundException(KitDir);
            var full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", Dest));
            Directory.CreateDirectory(full);
            int copied = 0, same = 0; var pngs = new List<string>();
            void Copy(string rel)
            {
                var src = Path.Combine(KitDir, rel); var dst = Path.Combine(full, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                var b = File.ReadAllBytes(src);
                if (File.Exists(dst) && File.ReadAllBytes(dst).AsSpan().SequenceEqual(b)) { same++; return; }
                File.WriteAllBytes(dst, b); copied++;
            }
            foreach (var dir in new[] { "panels", "buttons", "badges", "markers", "overlays", "portraits", "roster", "icons/singles" })
                foreach (var f in Directory.GetFiles(Path.Combine(KitDir, dir), "*@2x.png")) { var rel = Path.GetRelativePath(KitDir, f); Copy(rel); pngs.Add(rel); }
            foreach (var f in new[] { "Jost-Medium.ttf", "Jost-SemiBold.ttf", "Jost-Bold.ttf", "Barlow-Regular.ttf", "Barlow-Medium.ttf", "Barlow-SemiBold.ttf", "OFL-Jost.txt", "OFL-Barlow.txt" })
                Copy(Path.Combine("fonts", f));
            Copy("layout.json"); Copy("sprites.json");

            var layout = JNode.Parse(File.ReadAllText(Path.Combine(KitDir, "layout.json")), "layout.json");
            var sprites = JNode.Parse(File.ReadAllText(Path.Combine(KitDir, "sprites.json")), "sprites.json");
            var uss = Uss(layout, sprites, pngs);
            WriteIfChanged(Path.Combine(full, "kit.uss"), uss);
            WriteIfChanged(Path.Combine(full, "HudTheme.tss"), "@import url(\"kit.uss\");\n");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // sprites: @2x nine-slice insets as the sprite borders (left, bottom, right, top)
            var slices = SliceTable(sprites);
            int set = 0;
            foreach (var rel in pngs)
            {
                var path = $"{Dest}/{rel.Replace('\\', '/')}";
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                var name = Path.GetFileNameWithoutExtension(rel).Replace("@2x", "");
                var border = slices.TryGetValue(name, out var sl) ? new Vector4(sl.l, sl.b, sl.r, sl.t) : Vector4.zero;
                bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single || ti.mipmapEnabled || ti.spriteBorder != border
                    || ti.textureCompression != TextureImporterCompression.Uncompressed || !ti.alphaIsTransparency || ti.npotScale != TextureImporterNPOTScale.None;
                if (!dirty) continue;
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.mipmapEnabled = false; ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed; ti.npotScale = TextureImporterNPOTScale.None; ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp; ti.spriteBorder = border; ti.spritePixelsPerUnit = 100;
                ti.SaveAndReimport(); set++;
            }

            // the panel settings: the kit's reference 1920 x 1080, scale with the screen, match height
            var psPath = $"{Dest}/HudPanel.asset";
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(psPath);
            if (ps == null) { ps = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(ps, psPath); }
            ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>($"{Dest}/HudTheme.tss");
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize; ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; ps.match = 1f; ps.sortingOrder = 10; ps.clearColor = false;
            EditorUtility.SetDirty(ps); AssetDatabase.SaveAssets();
            return $"Kit: {pngs.Count} sprites ({copied} files copied, {same} unchanged, {set} importers set), 6 fonts, kit.uss {uss.Length / 1024} KB, HudPanel.asset → {Dest}";
        }

        static void WriteIfChanged(string path, string text) { if (!File.Exists(path) || File.ReadAllText(path) != text) File.WriteAllText(path, text); }

        static Dictionary<string, (int l, int t, int r, int b)> SliceTable(JNode sprites)
        {
            var o = new Dictionary<string, (int, int, int, int)>();
            var items = sprites.Opt("sprites") ?? sprites;
            foreach (var kv in items.Entries)
                if (kv.Value.Kind == JKind.Obj && kv.Value.Opt("slice@2x") is JNode s)
                    o[kv.Key] = (s["left"].Int(), s["top"].Int(), s["right"].Int(), s["bottom"].Int());
            return o;
        }

        static string Num(double v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        static double D(JNode n) => double.Parse(n.Kind == JKind.Num ? n.NumberText : n.Str(), System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>kit.uss from layout.json and sprites.json (generated: edit the kit, then import again).</summary>
        static string Uss(JNode layout, JNode sprites, List<string> pngs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("/* Generated by Suborned/UI/Import Glyph's Kit from briefs/011/ui/kit/layout.json and sprites.json. Do not edit: edit the kit and import again. */");
            // colours
            sb.AppendLine(":root {");
            foreach (var kv in layout["colors"].Entries) sb.AppendLine($"    --{kv.Key}: {kv.Value["hex"].Str()};");
            sb.AppendLine("}");
            // type styles: the font cut by family and weight
            string Cut(string fam, int w) => fam == "display" ? (w >= 700 ? "Jost-Bold" : w >= 600 ? "Jost-SemiBold" : "Jost-Medium") : (w >= 600 ? "Barlow-SemiBold" : w >= 500 ? "Barlow-Medium" : "Barlow-Regular");
            foreach (var kv in layout["type_styles"].Entries)
            {
                var t = kv.Value;
                sb.AppendLine($".t-{kv.Key} {{ -unity-font: url(\"fonts/{Cut(t["font"].Str(), t["weight"].Int())}.ttf\"); font-size: {Num(D(t["size_px"]))}px; color: var(--{t["color"].Str()}); letter-spacing: {Num(D(t["letter_spacing_px"]))}px; -unity-font-style: normal; margin: 0; padding: 0; }}");
            }
            // sprites: background, nine-slice insets at @2x drawn at half scale
            var items = sprites.Opt("sprites") ?? sprites;
            var have = new HashSet<string>(pngs.Select(p => p.Replace('\\', '/')));
            foreach (var kv in items.Entries)
            {
                if (kv.Value.Kind != JKind.Obj || kv.Value.Opt("file@2x") is not JNode f) continue;
                var file = f.Str(); if (!have.Contains(file)) continue;
                sb.Append($".s-{kv.Key} {{ background-image: url(\"{file}\");");
                if (kv.Value.Opt("slice@2x") is JNode s)
                    sb.Append($" -unity-slice-left: {s["left"].Int()}; -unity-slice-top: {s["top"].Int()}; -unity-slice-right: {s["right"].Int()}; -unity-slice-bottom: {s["bottom"].Int()}; -unity-slice-scale: 0.5px;");
                sb.AppendLine(" }");
            }
            foreach (var single in pngs.Where(p => p.Replace('\\', '/').StartsWith("icons/singles/")))
            {
                var name = Path.GetFileNameWithoutExtension(single).Replace("@2x", "");
                sb.AppendLine($".i-{name} {{ background-image: url(\"{single.Replace('\\', '/')}\"); -unity-background-image-tint-color: var(--cream); }}");
            }
            // button states (the kit's normal / hover / pressed / disabled / selected sprites)
            foreach (var btn in new[] { "btn_verb", "btn_close", "btn_ack", "btn_text" })
            {
                bool Has(string st) => items.Opt($"{btn}_{st}") != null;
                if (Has("hover")) sb.AppendLine($".{btn}:hover {{ background-image: url(\"buttons/{btn}_hover@2x.png\"); }}");
                if (Has("pressed")) sb.AppendLine($".{btn}:active {{ background-image: url(\"buttons/{btn}_pressed@2x.png\"); }}");
                if (Has("selected")) sb.AppendLine($".{btn}.selected {{ background-image: url(\"buttons/{btn}_selected@2x.png\"); }}");
                if (Has("disabled")) sb.AppendLine($".{btn}.disabled {{ background-image: url(\"buttons/{btn}_disabled@2x.png\"); }}");
            }
            // scene kinds: the header tint
            foreach (var (kind, col) in new[] { ("learned", "kind-learn"), ("approach", "kind-approach"), ("call", "kind-call"), ("city", "kind-city"), ("warning", "kind-warn"), ("fight", "kind-fight") })
                sb.AppendLine($".kind-{kind} {{ background-color: var(--{col}); }}");
            sb.AppendLine(".ellipsis { overflow: hidden; white-space: nowrap; text-overflow: ellipsis; }");
            sb.AppendLine(".wrap { white-space: normal; }");
            return sb.ToString();
        }
    }
}
