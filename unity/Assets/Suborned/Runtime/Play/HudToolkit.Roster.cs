using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The roster's tokens and type. Brief 046's tall column (the stretched column art, the row under each portrait, the steel dividers, its ALIGN audit) is
    /// retired by brief 101 step 4: the roster is the street faces (HudToolkit.Faces), whose audit is FacesAlign. The lab's export (`rosterSpec` in
    /// Resources/HudStyle/revisedA.tokens.json) still gives the column's scale; its other keys are read and unused.</summary>
    public sealed partial class HudToolkit
    {
        // ---------------------------------------------------------------- the export's roster spec (JsonUtility: fixed keys only)
        [System.Serializable] sealed class TokMark { public float dy, h, w, x; }
        [System.Serializable] sealed class TokDiv { public string id = ""; public float d2x = 55, d, gap, rowDy = 36.8f; public TokMark mark = new(); }
        [System.Serializable] sealed class TokDivs { public TokDiv none, space, hair, band, steel, notch, well; }
        [System.Serializable] sealed class TokColUnder { public string picked = "steel"; public TokDivs dividers = new(); }
        [System.Serializable] sealed class TokColumn { public string recipe = "tallColumn/2"; public int ease = 6, row = 222, w = 218, h = 833; public int[] cuts = { 223, 412, 601, 789 }, body = { 36, 182 }; public TokColUnder under = new(); }
        [System.Serializable] sealed class TokTab { public float dx = 9.48f, dy = 38.35f, w = 67, h = 16; }
        [System.Serializable] sealed class TokUnderSlots { public TokTab tab = new(); public float[] weapon = { 0, 0, 16, 16 }, status = { 19, 0, 16, 16 }, dots = { 39, 4, 28, 8 }; }
        [System.Serializable] sealed class TokSlots { public TokUnderSlots under = new(); }
        [System.Serializable] sealed class TokGeom { public float s = 0.79f, XC = 42.98f, WIN = 52.14f, MAT = 29.6f, RING = 36.34f; }
        [System.Serializable] sealed class TokRosterSpec { public TokGeom geometry = new(); public TokSlots slots = new(); public TokColumn column = new(); }
        [System.Serializable] sealed class TokRoster { public string layout = "under", divider = "steel", column = "bare"; }
        [System.Serializable] sealed class TokFontSide { public string file = ""; }
        [System.Serializable] sealed class TokFontSpec { public string id = ""; public TokFontSide body = new(), strong = new(); }

        static TokRosterSpec RS => Tok.rosterSpec ??= new TokRosterSpec();
        /// <summary>The roster column's scale on screen (rosterSpec.geometry.s: the lab drew the kit column at 0.79); the street faces keep it (HudToolkit.Faces).</summary>
        static float RosterScale => RS.geometry.s > 0.1f ? RS.geometry.s : 0.79f;
        /// <summary>The column's height in its own px (the four faces and their pips).</summary>
        static float ColH => 416f;
        /// <summary>Face i's centre in the column's own px (brief 101 step 4: the street faces; the tall column's bezels, rows and dividers are retired).</summary>
        static Vector2 BezelAt(int i) => FaceC(i);

        /// <summary>The cause icon of a hostile's strongest cause (brief 046 why.icons: kit icons only; the gun never repeats: "drawn" rides on
        /// the weapon disc).</summary>
        static string WhyIcon(string cause) => cause switch { "fired" => "bang", "vale" => "s_carrying", "violence" => "takedown", "crewmate" => "takedown", "face" => "papers", /* engine 0.33.0: a crewmate saw one of the crew taken down */ "seen" => "sawyou", "range" or "final" => "bang", _ => "" };

        // ---------------------------------------------------------------- the HUD type (brief 046 round 4/5, Sky: "Jost Regular")

        /// <summary>The kit's Jost styles (kit.uss) and the role each takes in the picked type (fontSpec): strong for the clock, the clock bar's
        /// values, counters, heads and status words (and every SemiBold), the body face for the rest. Barlow (speech, reading lines) is kept.</summary>
        static readonly Dictionary<string, bool> JostStyles = new()
        {
            ["clock"] = true, ["pill-value"] = true, ["role-tag"] = true, ["slot-state-ready"] = true, ["slot-state-busy"] = true, ["slot-state-hurt"] = true, ["slot-state-down"] = true,
            ["slot-time"] = true, ["numplate"] = false, ["keycap"] = false, ["verb-name"] = false, ["objective"] = false, ["hint-tab"] = true, ["section-label"] = true, ["panel-title"] = false,
            ["card-title"] = false, ["card-tag"] = false, ["briefing-title"] = true, ["button-label"] = true, ["loud-title"] = true, ["loud-hold"] = false, ["feed-title"] = true,
            ["feed-clock"] = false, ["feed-bar"] = true, ["feed-filter"] = false, ["chip-value"] = false, ["fight-chip"] = false, ["tooltip-title"] = false,
        };
        static readonly Dictionary<string, Font> typeFonts = new();
        static Font TypeFont(string file)
        {
            if (string.IsNullOrEmpty(file)) return null; var name = System.IO.Path.GetFileNameWithoutExtension(file);
            if (typeFonts.TryGetValue(name, out var f)) return f;
            f = Resources.Load<Font>("Kit/fonts/" + name); if (f == null) Debug.LogWarning($"HUD: font '{name}' is not in Kit/fonts; the kit's Jost kept");
            return typeFonts[name] = f;
        }
        /// <summary>A kit label's face by the type token: the body or strong file of fontSpec for a Jost style, drawn as cut (no synthetic bold).</summary>
        static void TypeOf(Label l, string style)
        {
            if (!JostStyles.TryGetValue(style, out bool strong)) return;
            var fs = Tok.fontSpec; if (fs == null) return;
            var f = TypeFont(strong ? fs.strong.file : fs.body.file); if (f == null) return;
            l.style.unityFontDefinition = FontDefinition.FromFont(f); l.style.unityFontStyleAndWeight = FontStyle.Normal;
        }
    }
}
