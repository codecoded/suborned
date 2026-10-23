using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 044 (direction A "Chrome plate", revised after Sky's pick 2026-10-22): the overlays' kit pieces. Every colour here has a
    /// source in the kit (a sprite, `kit.uss`, or the style contract's colour where the HUD already shows it; the audit table is brief 044 §6);
    /// kind, harm and role never get a colour of their own. The act's kind reads by its icon on a disc the size of the doer's face, in the same
    /// chrome ring (Sky: "make the icons larger like avatar on right"); harm reads by shape (chamfered strip, stripes, the light disc).</summary>
    public sealed partial class HudToolkit
    {
        // kit.uss :root
        static readonly Color InkDeep = Hex(0x0b1615), Chrome = Hex(0xcbc6c3), Dim = Hex(0xb0cdc7), PanelLine = Hex(0xd2e6e2), Brass = Hex(0x99784d), Ivory = Hex(0xeee9da);
        /// <summary>Sampled from the kit sprites: the body of `pill_status` (the tail's inner triangle) and of `disc_dark_24` / `plate_dark` (the act disc).</summary>
        static readonly Color TailBody = Hex(0x0c3434), DiscBody = Hex(0x203f3e);

        // ---------------------------------------------------------------- the overlay look's tokens (brief 046, Sky's pick: "Revised A")
        // Read from the HUD style lab's export (testbed/hud-style/export/revisedA.tokens.json, copied to Resources/HudStyle): colours, the act
        // disc and face sizes, the hold's fill and easing. A missing or unreadable file keeps the values below (the same Revised A).
        /// <summary>type.font: a kit font file under Kit/fonts (no extension), the overlays' one face (Glyph picks it in the lab; Sky 2026-10-22: "too
        /// bold, needs to be a lot cleaner"); type.weight: "normal" draws the file as cut, "bold" asks UI Toolkit to embolden it (synthetic: the
        /// kit's Jost-Bold file plus a bold style was double bold, 0.2.65).</summary>
        [System.Serializable] sealed class TokType { public string main = "#f7f4ec", dim = "#b0cdc7", head = "#b0cdc7", card = "#17302f", font = "Jost-Bold", weight = "normal"; }
        [System.Serializable] sealed class TokAccent { public string live = "#60e0ea", warn = "#f0a020", hair = "#99784d", tail = "#0c3434"; }
        [System.Serializable] sealed class TokDisc { public string family = "dark", icon = "#f7f4ec"; public float size = 26, iconSize = 15; }
        [System.Serializable] sealed class TokFace { public float size = 24; }
        [System.Serializable] sealed class TokHold { public string anim = "fill", ease = "linear", release = "snap", cancel = "drain", fill = "#60e0ea"; public float alpha = 0.4f, ms = 1200; }
        [System.Serializable] sealed class Tokens { public string schema = "", id = "builtin"; public TokType type = new(); public TokAccent accent = new(); public TokDisc disc = new(); public TokFace face = new(); public TokHold hold = new(); public TokRoster roster; public TokRosterSpec rosterSpec; public TokFontSpec fontSpec; public TokWhy why; }
        [System.Serializable] sealed class TokArrow { public string style = "arc"; public float alpha = 0.85f, weight = 1f; }
        [System.Serializable] sealed class TokWhy { public float pulseMs = 900f; public TokArrow arrow = new(); }
        static Tokens tok;
        static Tokens Tok
        {
            get
            {
                if (tok != null) return tok;
                try { var ta = Resources.Load<TextAsset>("HudStyle/revisedA.tokens"); tok = ta != null ? JsonUtility.FromJson<Tokens>(ta.text) : null; } catch (System.Exception e) { Debug.LogWarning("HUD: style tokens unreadable: " + e.Message); }
                tok ??= new Tokens(); Debug.Log($"HUD: style tokens '{tok.id}' ({tok.schema})"); return tok;
            }
        }
        static Color Col(string hex, Color fallback) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        /// <summary>The overlays' type colour (type.main).</summary>
        static Color LookText => Col(Tok.type.main, Cream);
        /// <summary>The live accent: the hold's sweep, "Let go", its ring, the Watch's reach, the window's underline, the chip's arrow (accent.live).</summary>
        static Color LookLive => Col(Tok.accent.live, TurqLight);
        /// <summary>"Running out": the window's last 10 s, the attention clock from "wary" (accent.warn).</summary>
        static Color LookSoon => Col(Tok.accent.warn, Amber);
        /// <summary>The head word ("Hold", "Right hold") and its dot (type.head).</summary>
        static Color LookHead => Col(Tok.type.head, Dim);
        /// <summary>The chip's hairline (accent.hair) and the tail's body (accent.tail).</summary>
        static Color LookHair => Col(Tok.accent.hair, Brass);
        static Color LookTail => Col(Tok.accent.tail, TailBody);
        /// <summary>Lines on the ground: destination rings, the paused plan's path and rings.</summary>
        static Color LookGround => LookText;
        static Color HoldFill => Col(Tok.hold.fill, TurqLight);
        static float HoldSweepAlpha => Tok.hold.alpha;
        const float ReadyGlowAlpha = 0.13f, ReadyRingW = 2f, ReadyScale = 1.05f, HeadDotAlpha = 0.7f;
        /// <summary>The face cameo inside its ring (face.size); the act disc beside it is the cameo's size (Sky 2026-10-22: "make the icons larger
        /// like avatar on right"). The kit's `disc_dark_24` draws its chrome rim across 48 of its 64 px (measured: alpha and rim at x 8..56 on
        /// the middle row), so its box is the cameo's outer diameter / 0.75; the lab's disc.size is a drawn diameter (a CSS circle fills its
        /// box) and only ever grows it. The icon is two thirds of the drawn disc (disc.iconSize at least): 20 px at face 24.</summary>
        static float FaceSize => Tok.face.size;
        const float DiscDrawn = 48f / 64f;
        static float DiscVisible => Mathf.Max(FaceSize + 6f, Tok.disc.size);
        static float DiscSize => Mathf.Round(DiscVisible / DiscDrawn);
        static float DiscIcon => Mathf.Max(Tok.disc.iconSize, Mathf.Round(DiscVisible * 2f / 3f));
        /// <summary>The overlays' face (type.font, type.weight): applied to the Jost Bold families the overlays use (chip-value, verb-name).</summary>
        static Font lookFont; static string lookFontName;
        static void LookType(Label l)
        {
            var name = string.IsNullOrEmpty(Tok.type.font) ? "Jost-Bold" : Tok.type.font;
            if (name != "Jost-Bold")
            {
                if (lookFontName != name) { lookFontName = name; lookFont = Resources.Load<Font>("Kit/fonts/" + name); if (lookFont == null) Debug.LogWarning($"HUD: type.font '{name}' is not in Kit/fonts; Jost-Bold kept"); }
                if (lookFont != null) l.style.unityFontDefinition = FontDefinition.FromFont(lookFont);
            }
            l.style.unityFontStyleAndWeight = Tok.type.weight == "bold" ? FontStyle.Bold : FontStyle.Normal;
        }
        /// <summary>The hold's sweep: how far it shows for a charge at `progress` (0..1), by hold.ease (linear; easeOut, easeIn, easeInOut).</summary>
        static float HoldSweep(float progress)
        {
            float p = Mathf.Clamp01(progress);
            return Tok.hold.ease switch { "easeOut" or "ease-out" => 1f - (1f - p) * (1f - p), "easeIn" or "ease-in" => p * p, "easeInOut" or "ease-in-out" => p < 0.5f ? 2f * p * p : 1f - 2f * (1f - p) * (1f - p), _ => p };
        }

        /// <summary>A face cameo in the chrome ring (2 px chrome, 1 px ink-deep outside it): `inner` px of face, `inner + 6` across. The face is
        /// the child named "face".</summary>
        static VisualElement Cameo(VisualElement parent, float inner, float ring = 2f)
        {
            float d = inner + 2f * ring + 2f;
            var w = E(parent, null, w: d, h: d); Round(w, d / 2f); w.style.flexShrink = 0; w.style.backgroundColor = Chrome; Border(w, 1f, InkDeep);
            var f = E(w, null, ring, ring, inner, inner); Round(f, inner / 2f); f.name = "face"; f.style.backgroundColor = DiscBody;
            return w;
        }
        void CameoOf(VisualElement wrap, int agent) => Bg(wrap.Q("face"), $"portraits/cameo_{Op.Name((OpId)agent)}_normal", Color.white);

        /// <summary>The act's disc as the lab's Revised A draws it: the kit's `disc_dark_24` sprite (its own chrome rim) at disc.size beside the
        /// face cameo, a cream icon of disc.iconSize; harm and fire: `disc_light_24`, ink icon. The icon is the child named "glyph".</summary>
        VisualElement ActDiscEl(VisualElement parent)
        {
            float d = DiscSize, g = DiscIcon;
            var w = E(parent, null, w: d, h: d); w.style.flexShrink = 0; Bg(w, "markers/disc_dark_24", Color.white);
            var glyph = E(w, null, (d - g) / 2f, (d - g) / 2f, g, g); glyph.name = "glyph";
            return w;
        }
        void ActDiscOf(VisualElement wrap, string icon, bool light)
        {
            Bg(wrap, light ? "markers/disc_light_24" : "markers/disc_dark_24", Color.white);
            Icon(wrap.Q("glyph"), icon, light ? Col(Tok.type.card, Ink) : Col(Tok.disc.icon, Cream));
        }

        /// <summary>A block of the kit's warning stripes (`overlays/stripes_warn`, tiled at 22 x 15 px as the kit draws it).</summary>
        VisualElement Stripes(VisualElement parent, float w, float h)
        {
            var e = E(parent, null, w: w, h: h); e.style.flexShrink = 0; Bg(e, "overlays/stripes_warn", Color.white);
            e.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            e.style.backgroundSize = new BackgroundSize(new Length(22f), new Length(15f));
            e.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left); e.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top);
            Round(e, 2f); return e;
        }

        /// <summary>A rounded rectangle's outline as dashes (UI Toolkit has no dashed border): the cancel tag's edge.</summary>
        static void DashedRect(Painter2D p, Rect r, float rad, Color col, float w, float dash = 4f, float gap = 3f)
        {
            p.strokeColor = col; p.lineWidth = w; p.lineCap = LineCap.Butt;
            var pts = new System.Collections.Generic.List<Vector2>();
            void Arc(Vector2 c, float a0) { for (int i = 0; i <= 6; i++) { float a = (a0 + 90f * i / 6f) * Mathf.Deg2Rad; pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad); } }
            float h = w / 2f; r = Rect.MinMaxRect(r.xMin + h, r.yMin + h, r.xMax - h, r.yMax - h);
            Arc(new Vector2(r.xMax - rad, r.yMin + rad), -90f); Arc(new Vector2(r.xMax - rad, r.yMax - rad), 0f); Arc(new Vector2(r.xMin + rad, r.yMax - rad), 90f); Arc(new Vector2(r.xMin + rad, r.yMin + rad), 180f); pts.Add(pts[0]);
            bool on = true; float left = dash;
            for (int i = 1; i < pts.Count; i++)
            {
                var a = pts[i - 1]; var b = pts[i]; float len = Vector2.Distance(a, b), t = 0f;
                while (t < len)
                {
                    float step = Mathf.Min(left, len - t); var s0 = Vector2.Lerp(a, b, t / len); var s1 = Vector2.Lerp(a, b, (t + step) / len);
                    if (on) { p.BeginPath(); p.MoveTo(s0); p.LineTo(s1); p.Stroke(); }
                    t += step; left -= step; if (left <= 0.001f) { on = !on; left = on ? dash : gap; }
                }
            }
        }

        /// <summary>A plate_dark at a smaller slice scale (its chrome rim thins with it; the layout audit measures the rim at the same scale).</summary>
        static VisualElement PlateDark(VisualElement parent, float h, float scale)
        {
            var e = E(parent, "s-plate_dark", w: float.NaN, h: h); e.style.unitySliceScale = scale; e.style.flexDirection = FlexDirection.Row; e.style.alignItems = Align.Center; e.style.flexShrink = 0; e.pickingMode = PickingMode.Ignore;
            return e;
        }

        /// <summary>The overlays' one type: Jost Bold (kit `t-chip-value`) in cream at `size`.</summary>
        static Label Words(VisualElement parent, float size, Color? col = null)
        {
            var l = L(parent, "chip-value"); l.style.fontSize = size; LookType(l); l.style.color = col ?? LookText; l.style.whiteSpace = WhiteSpace.NoWrap; return l;
        }
    }
}
