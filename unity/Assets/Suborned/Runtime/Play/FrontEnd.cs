using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 051 (front end) and brief 047 (loading screen), Glyph's specs briefs/047/layout/SPEC.md and briefs/051/layout/SPEC.md: the loading
    /// screen, the main menu, the mission brief, and the Options and Controls panels, drawn from Resources/Loading (loading.json, frontend.json and
    /// the picture loading_bg.png: one swap changes the whole front end, nothing in code names the picture, a position or a colour beyond the kit's).
    /// The player opens on the small FrontEnd scene (GameBuild puts it first): the loading screen shows while HarbourDistrict loads, then the menu
    /// stands over the paused game (its camera off). New game → mission brief (contracts/missions/handover/brief.json, copied into the build) → the
    /// existing tactical briefing. A proof or batch run (any automation flag) never sees it: the FrontEnd scene loads HarbourDistrict at once.
    /// Sizes are ref px at 1080p (the panel scales with the screen height); all type is Jost (Medium for titles and buttons, Regular-Tab for the rest).</summary>
    public sealed partial class FrontEnd : MonoBehaviour
    {
        public const string BootScene = "FrontEnd", GameScene = "HarbourDistrict";
        public static FrontEnd Live { get; private set; }
        /// <summary>A front-end screen has the keys and the clicks (the host's Update stops after its housekeeping).</summary>
        public static bool Covers => Live != null && Live.screen != Screen.None;
        /// <summary>The frame the front end last closed: the key that closed it (Esc, Enter, Space) is not the game's too.</summary>
        public static int ClosedFrame = -1;
        public enum Screen { None, Loading, Menu, Brief, Options, Controls }
        Screen screen = Screen.None;
        public Screen Showing => screen;
        /// <summary>The panel was opened from the pause menu (no picture: the frozen game under an even veil; Back returns to the pause menu).</summary>
        bool fromPause;

        // ---------------------------------------------------------------- boot

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (SceneManager.GetActiveScene().name != BootScene) return;
            bool shots = GameBoot.ArgValue("-frontEndShots") != null;
            if (GameBoot.Automation && !shots) { SceneManager.LoadScene(GameScene); return; } // a proof or batch row: straight to play, as before the front end
            var fe = Ensure(); Settings.Current.ApplyDisplay(); fe.StartCoroutine(fe.LoadGame());
        }

        /// <summary>The front end, made on first use (the pause menu's Options and Controls in a scene that did not start from the boot scene).</summary>
        public static FrontEnd Ensure()
        {
            if (Live) return Live;
            var go = new GameObject("front-end"); DontDestroyOnLoad(go);
            return go.AddComponent<FrontEnd>();
        }

        // ---------------------------------------------------------------- data (Resources/Loading)

        [Serializable] public sealed class Rect01 { public float x, y, w, h; }
        [Serializable] public sealed class TitleLine { public string text = "", font = "Jost-Medium"; public float sizeRef = 20, trackingEm, opacity = 1, gapAboveRef; public string color = "#f7f4ec"; }
        [Serializable] public sealed class Rule { public string color = "#99784d"; public float heightRef = 1, gapAboveRef = 12; }
        [Serializable] public sealed class ShadowS { public string color = "#0b1615"; public float alpha = 0.35f, blurRef = 14; public float[] offsetRef = { 0, 2 }; }
        [Serializable] public sealed class TitleS { public Rect01 rect = new(); public float maxScale = 1.25f; public TitleLine[] lines = Array.Empty<TitleLine>(); public Rule rule = new(); public ShadowS shadow = new(); }
        [Serializable] public sealed class ImageS { public string name = "loading_bg"; public float[] focus = { 0.5f, 0.5f }; public PushIn pushIn = new(); }
        [Serializable] public sealed class PushIn { public float scale, seconds = 24; }
        [Serializable] public sealed class Scrim { public string edge = "bottom", color = "#0b1615", curve = "smoothstep"; public float heightFrac = 0.3f, alphaAtEdge = 0.5f, holdFrac; }
        [Serializable] public sealed class ColA { public string color = "#0b1615"; public float alpha = 1, widthRef = 1, seconds = 1.6f; }
        [Serializable] public sealed class BarS { public float leftRef = 64, rightRef = 64, bottomRef = 66, heightRef = 5, radiusRef = 2.5f; public ColA track = new(), rim = new(), fill = new(), cap = new(), shimmer = new(); }
        [Serializable] public sealed class PercentS { public bool enabled = true; public float sizeRef = 14.5f; public string color = "#f7f4ec"; }
        [Serializable] public sealed class LoadingLineS { public bool enabled = true; public string head = "Loading", separator = "·", headColor = "#b0cdc7", textColor = "#f7f4ec"; public float sizeRef = 15.5f, trackingEm = 0.1f, gapAboveBarRef = 16; public bool uppercase = true; public PercentS percent = new(); }
        [Serializable] public sealed class BuildS { public bool enabled = true; public string color = "#b0cdc7"; public float alpha = 0.85f, sizeRef = 12.5f, bottomRef = 30, rightRef = 64; }
        [Serializable] public sealed class TimingS { public float fadeInSeconds = 0.5f, titleDelaySeconds = 0.25f, titleFadeSeconds = 0.6f, titleRiseRef = 8, barDelaySeconds = 0.5f, barFadeSeconds = 0.4f, minShowSeconds = 1.5f, barSmoothSeconds = 0.25f, completeHoldSeconds = 0.25f, uiFadeOutSeconds = 0.25f, pictureFadeOutSeconds = 0.6f; }
        [Serializable] public sealed class LoadingData { public string schema = ""; public float refHeight = 1080; public ImageS image = new(); public Scrim[] scrims = Array.Empty<Scrim>(); public TitleS title = new(); public BarS bar = new(); public LoadingLineS loadingLine = new(); public BuildS build = new(); public TimingS timing = new(); }

        [Serializable] public sealed class TypeS { public string font = "Jost-Regular", color = "cream", activeColor = "", dimColor = "", valueColor = ""; public float sizeRef = 16, trackingEm, lineRef, paragraphGapRef, alpha = 1; public bool uppercase; }
        [Serializable] public sealed class Types { public TypeS button = new(), menuButton = new(), panelTitle = new(), tab = new(), group = new(), rowLabel = new(), rowValue = new(), keycap = new(), briefTitle = new(), briefHead = new(), briefBody = new(), build = new(); }
        [Serializable] public sealed class Colors { public string ink = "#17302f", inkDeep = "#0b1615", cream = "#f7f4ec", textDim = "#b0cdc7", chrome = "#cbc6c3", panelLine = "#d2e6e2", live = "#60e0ea", brass = "#99784d", pipEmpty = "#142c2c"; }
        [Serializable] public sealed class FocusS { public float ringRef = 2, glowAlpha = 0.4f, glowRef = 10; }
        [Serializable] public sealed class ButtonS { public FocusS focus = new(); public float disabledLabelAlpha = 0.55f; }
        [Serializable] public sealed class MenuS { public float leftRef = 72, bottomRef = 96, widthRef = 300, heightRef = 56, gapRef = 14; }
        [Serializable] public sealed class RowFocus { public float alpha = 0.38f, tickRef = 3, radiusRef = 6; }
        [Serializable] public sealed class FooterS { public float lineY = 100, resetW = 168, resetH = 34, backW = 170, backH = 48, bottomInset = 24; }
        [Serializable] public sealed class PanelS { public float widthRef = 960, heightRef = 780, titleY = 40, closeRef = 29, closeInset = 26, tabsY = 100, tabH = 34, tabGap = 10, tabPadX = 26, bodyTop = 158, bodyBottomInset = 100, rowH = 44, groupH = 38, labelX = 10, controlRight = 10, controlW = 380; public float[] padRef = { 48, 40, 48, 28 }; public RowFocus rowFocus = new(); public FooterS footer = new(); }
        [Serializable] public sealed class SliderS { public float trackH = 6, trackAlpha = 0.7f, rimAlpha = 0.35f, knobRef = 22, width = 250, valueW = 56; }
        [Serializable] public sealed class SwitchS { public float w = 46, h = 24, knobRef = 20, onFillAlpha = 0.4f, offFillAlpha = 0.65f, rimAlpha = 0.55f, wordW = 44; }
        [Serializable] public sealed class ChoiceS { public float w = 300, chevRef = 16, chevGapRef = 16; }
        [Serializable] public sealed class KeycapS { public float h = 30, minW = 52, padX = 14, altH = 24; }
        [Serializable] public sealed class ControlsS { public SliderS slider = new(); public SwitchS @switch = new(); public ChoiceS choice = new(); public KeycapS keycap = new(); }
        [Serializable] public sealed class ClashS { public float h = 52, markRef = 26, keyBadgeRef = 24; }
        [Serializable] public sealed class ControlsPageS { public string[] tabs = { "Command", "Camera", "Fights", "Third person" }; public int columns = 2; public float columnGapRef = 40, rowH = 36, labelX = 6; public ClashS clashStrip = new(); }
        [Serializable] public sealed class PauseS { public float widthRef = 360, topFrac = 0.12f, buttonW = 292, buttonH = 46, gapRef = 10, titleGapRef = 20; public float[] padRef = { 34, 28, 34, 26 }; }
        [Serializable] public sealed class ColumnS { public float rightRef = 64, widthRef = 568, topRef = 84, bottomRef = 64; }
        [Serializable] public sealed class BriefButton { public float w = 220, h = 52; public string label = "Continue"; }
        [Serializable] public sealed class HeadRule { public float heightRef = 1, gapAboveRef = 16, gapBelowRef = 20; }
        [Serializable] public sealed class BriefS { public float[] imageFocus = { 1f, 0.5f }; public ColumnS column = new(); public HeadRule headRule = new(); public BriefButton button = new(); }
        [Serializable] public sealed class ScrimSets { public Scrim[] menu = Array.Empty<Scrim>(), panel = Array.Empty<Scrim>(), brief = Array.Empty<Scrim>(); }
        [Serializable] public sealed class TimingF { public float screenFadeSeconds = 0.3f, buttonStaggerSeconds = 0.05f, buttonRiseRef = 6, menuFadeInAfterLoading = 0.4f, pageSwitchSeconds = 0.12f; }
        [Serializable] public sealed class FrontData { public string schema = ""; public Colors colors = new(); public Types type = new(); public ScrimSets scrims = new(); public ButtonS button = new(); public MenuS menu = new(); public PanelS panel = new(); public ControlsS controls = new(); public ControlsPageS controlsPage = new(); public PauseS pause = new(); public BriefS brief = new(); public TimingF timing = new(); }

        /// <summary>The mission's brief (contracts/missions/&lt;mission&gt;/brief.json, Fable's words, `suborned.mission-brief/2`; StreamingAssets/missions/
        /// &lt;mission&gt;/brief.json in the build): three variants, `pick` the one shown (Sky picks later), each a hook, handler lines, a dossier
        /// (target, location, window, objective), intel and a sign-off, laid out in `order`; empty parts are left out.</summary>
        [Serializable] public sealed class Dossier { public string target = "", location = "", window = "", objective = ""; }
        [Serializable] public sealed class BriefVariant { public string hook = "", signoff = "", @short = ""; public string[] handler = Array.Empty<string>(), intel = Array.Empty<string>(); public Dossier dossier = new(); }
        [Serializable] public sealed class BriefVariants { public BriefVariant A = new(), B = new(), C = new(); }
        [Serializable]
        public sealed class MissionBrief
        {
            public string schema = "", mission = "", title = "", place = "", time = "", pick = "A"; public string[] order = { "hook", "handler", "dossier", "intel", "signoff" }; public BriefVariants variants = new();
            public BriefVariant Picked => (pick ?? "A").Trim().ToUpperInvariant() switch { "B" => variants.B, "C" => variants.C, _ => variants.A };
        }

        public LoadingData L = new(); public FrontData F = new(); MissionBrief brief;
        /// <summary>frontend.json for the HUD's pause card (the same type and colours), loaded once without making the front end.</summary>
        public static FrontData Shared => shared ??= Json<FrontData>("Loading/frontend"); static FrontData shared;
        static readonly Dictionary<string, Font> sharedFonts = new();
        /// <summary>A label in frontend.json's type style, for a panel that is not the front end's (the pause card): font, size, colour, case, tracking.</summary>
        public static void Typeset(Label l, TypeS t, string text, string colorOverride = null)
        {
            var name = string.IsNullOrEmpty(t.font) || t.font == "Jost-Regular" ? "Jost-Regular-Tab" : t.font;
            if (!sharedFonts.TryGetValue(name, out var f)) sharedFonts[name] = f = Resources.Load<Font>("Kit/fonts/" + name);
            if (f) l.style.unityFontDefinition = FontDefinition.FromFont(f);
            var c = Shared.colors; var cn = colorOverride ?? t.color;
            l.style.color = Hex(cn switch { "ink" => c.ink, "inkDeep" => c.inkDeep, "cream" => c.cream, "textDim" => c.textDim, _ => cn });
            l.style.unityFontStyleAndWeight = FontStyle.Normal; l.style.fontSize = t.sizeRef; l.style.letterSpacing = 0;
            l.style.marginLeft = l.style.marginRight = l.style.marginTop = l.style.marginBottom = 0; l.style.paddingLeft = l.style.paddingRight = l.style.paddingTop = l.style.paddingBottom = 0;
            SetText(l, t, text);
        }
        Texture2D picture;
        public static PanelSettings SharedPanel { get; private set; }

        static T Json<T>(string resource) where T : new()
        {
            var t = Resources.Load<TextAsset>(resource);
            if (!t) { Debug.LogError($"FrontEnd: Resources/{resource}.json missing; defaults used"); return new T(); }
            try { return JsonUtility.FromJson<T>(t.text) ?? new T(); } catch (Exception e) { Debug.LogError($"FrontEnd: {resource}.json: {e.Message}"); return new T(); }
        }

        public static MissionBrief LoadBrief(string mission = "handover")
        {
            string path = Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "contracts", "missions", mission, "brief.json"))
                                               : Path.Combine(Application.streamingAssetsPath, "missions", mission, "brief.json");
            try { if (File.Exists(path)) return JsonUtility.FromJson<MissionBrief>(File.ReadAllText(path)); }
            catch (Exception e) { Debug.LogError($"FrontEnd: {path}: {e.Message}"); }
            Debug.LogError($"FrontEnd: no mission brief at {path}");
            return new MissionBrief { title = "The Handover" };
        }

        // ---------------------------------------------------------------- the panel and its layers

        UIDocument doc; VisualElement root, pictureLayer, pictureEl, scrimLayer, titleGroup, loadingGroup, menuGroup, briefGroup, panelVeil, panelGroup, buildLabel, blocker;
        readonly Dictionary<string, Font> fonts = new();
        readonly Dictionary<string, Sprite> sprites = new();
        public float W => root?.layout.width is float w && !float.IsNaN(w) && w > 0 ? w : 1920f * UnityEngine.Screen.width / Mathf.Max(1f, UnityEngine.Screen.height) * (1080f / 1080f);
        public float H => root?.layout.height is float h && !float.IsNaN(h) && h > 0 ? h : 1080f;

        void Awake()
        {
            Live = this;
            L = Json<LoadingData>("Loading/loading"); F = Json<FrontData>("Loading/frontend"); brief = LoadBrief();
            picture = Resources.Load<Texture2D>("Loading/" + (string.IsNullOrEmpty(L.image.name) ? "loading_bg" : L.image.name));
            if (!picture) Debug.LogError("FrontEnd: the picture Resources/Loading/" + L.image.name + " is missing");
            var ps = Resources.Load<PanelSettings>("Kit/HudPanel");
            var go = new GameObject("front-end-ui"); go.SetActive(false); go.transform.SetParent(transform, false);
            doc = go.AddComponent<UIDocument>();
            var p = ps ? Instantiate(ps) : ScriptableObject.CreateInstance<PanelSettings>();
            p.scaleMode = PanelScaleMode.ScaleWithScreenSize; p.referenceResolution = new Vector2Int(1920, 1080); p.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; p.match = 1f; // 1 ref px = H / 1080 device px
            p.sortingOrder = 1000; doc.panelSettings = p; SharedPanel = p; go.SetActive(true);
            root = doc.rootVisualElement; root.pickingMode = PickingMode.Ignore; root.style.flexGrow = 1;
            var uss = Resources.Load<StyleSheet>("Kit/kit"); if (uss != null) root.styleSheets.Add(uss);
            Build(); Show(Screen.None);
        }

        void OnDestroy() { if (Live == this) Live = null; }

        // ---------------------------------------------------------------- small helpers

        public static Color Hex(string s, float a = 1f) { if (string.IsNullOrEmpty(s) || !ColorUtility.TryParseHtmlString(s, out var c)) c = Color.magenta; c.a *= a; return c; }
        public Color C(string name, float a = 1f) => Hex(name switch
        {
            "ink" => F.colors.ink, "inkDeep" => F.colors.inkDeep, "cream" => F.colors.cream, "textDim" => F.colors.textDim, "chrome" => F.colors.chrome,
            "panelLine" => F.colors.panelLine, "live" => F.colors.live, "brass" => F.colors.brass, "pipEmpty" => F.colors.pipEmpty, _ => name,
        }, a);

        public static VisualElement E(VisualElement parent, string cls = null, float x = float.NaN, float y = float.NaN, float w = float.NaN, float h = float.NaN, bool pick = false)
        {
            var e = new VisualElement { pickingMode = pick ? PickingMode.Position : PickingMode.Ignore, focusable = false };
            if (cls != null) foreach (var c in cls.Split(' ')) if (c.Length > 0) e.AddToClassList(c);
            if (!float.IsNaN(x)) { e.style.position = Position.Absolute; e.style.left = x; }
            if (!float.IsNaN(y)) { e.style.position = Position.Absolute; e.style.top = y; }
            if (!float.IsNaN(w)) e.style.width = w; if (!float.IsNaN(h)) e.style.height = h;
            parent?.Add(e); return e;
        }
        public static void Fill(VisualElement e) { e.style.position = Position.Absolute; e.style.left = 0; e.style.top = 0; e.style.right = 0; e.style.bottom = 0; }
        public static void Show(VisualElement e, bool on) { if (e != null) e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None; }
        public static void Border(VisualElement e, float w, Color c, float r = -1)
        {
            e.style.borderTopWidth = w; e.style.borderBottomWidth = w; e.style.borderLeftWidth = w; e.style.borderRightWidth = w;
            e.style.borderTopColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c; e.style.borderRightColor = c;
            if (r >= 0) Radius(e, r);
        }
        public static void Radius(VisualElement e, float r) { e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r; e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r; }

        public Font FontOf(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "Jost-Regular";
            if (name == "Jost-Regular") name = "Jost-Regular-Tab"; // the kit carries Regular with tabular digits only (the letters are Regular's)
            if (fonts.TryGetValue(name, out var f)) return f;
            f = Resources.Load<Font>("Kit/fonts/" + name); if (!f) Debug.LogWarning($"FrontEnd: font Kit/fonts/{name} missing"); fonts[name] = f; return f;
        }
        public Sprite Sp(string file) { if (sprites.TryGetValue(file, out var s)) return s; s = Resources.Load<Sprite>($"Kit/{file}@2x"); sprites[file] = s; return s; }

        /// <summary>A label in one of frontend.json's type styles (font, size, tracking, colour, case).</summary>
        public Label T(VisualElement parent, TypeS t, string text, string colorOverride = null)
        {
            var l = new Label { pickingMode = PickingMode.Ignore }; parent?.Add(l);
            Style(l, t, colorOverride); SetText(l, t, text); return l;
        }
        public void Style(Label l, TypeS t, string colorOverride = null)
        {
            var f = FontOf(t.font); if (f) l.style.unityFontDefinition = FontDefinition.FromFont(f);
            l.style.unityFontStyleAndWeight = FontStyle.Normal; l.style.fontSize = t.sizeRef; l.style.letterSpacing = 0;
            l.style.color = C(colorOverride ?? t.color, t.alpha <= 0 ? 1f : t.alpha);
            l.style.marginLeft = 0; l.style.marginRight = 0; l.style.marginTop = 0; l.style.marginBottom = 0; l.style.paddingLeft = 0; l.style.paddingRight = 0; l.style.paddingTop = 0; l.style.paddingBottom = 0;
            l.userData = t;
        }
        /// <summary>The text, upper-cased when the style says. Tracking (letter-spacing in em) is drawn exactly: a tracked style (0.05 em and up: the title,
        /// the menu's buttons, heads) lays its glyphs out one child each with the gap between (UI Toolkit's letter-spacing did not match the mockups' em).</summary>
        public static void SetText(Label l, TypeS t, string text)
        {
            var s = t != null && t.uppercase ? (text ?? "").ToUpperInvariant() : text ?? "";
            if (t == null || t.trackingEm < 0.05f || s.Length > 48) { l.Clear(); l.text = s; return; }
            l.text = ""; l.Clear(); l.style.flexDirection = FlexDirection.Row; l.style.alignItems = Align.FlexEnd;
            float gap = t.trackingEm * t.sizeRef;
            for (int i = 0; i < s.Length; i++)
            {
                var c = new Label(s[i] == ' ' ? "" : s[i].ToString()) { pickingMode = PickingMode.Ignore };
                c.style.marginLeft = 0; c.style.marginTop = 0; c.style.marginBottom = 0; c.style.paddingLeft = 0; c.style.paddingRight = 0; c.style.paddingTop = 0; c.style.paddingBottom = 0;
                c.style.marginRight = i < s.Length - 1 ? gap : 0; if (s[i] == ' ') c.style.width = 0.28f * t.sizeRef;
                c.style.unityTextAlign = TextAnchor.LowerLeft; l.Add(c);
            }
        }

        /// <summary>An overlay's alpha as the mockups mean it (blended in sRGB): UI Toolkit blends in linear space in this project, where black at a
        /// over a colour leaves (1 - a) of its linear value, which reads lighter; 1 - (1 - a)^2.2 leaves the sRGB value at (1 - a) (Glyph 0.2.85 #1, #4:
        /// the brief's held zone and the panel veil measured short).</summary>
        public static float SrgbAlpha(float a) => QualitySettings.activeColorSpace == ColorSpace.Linear ? 1f - Mathf.Pow(1f - Mathf.Clamp01(a), 2.2f) : a;

        /// <summary>A scrim band: a 1 x 64 (or 64 x 1) gradient baked once, stretched over its edge (no shader).</summary>
        VisualElement ScrimEl(VisualElement parent, Scrim s)
        {
            var e = E(parent); e.style.position = Position.Absolute;
            var col = Hex(s.color);
            if (s.edge == "full") { Fill(e); e.style.backgroundColor = new Color(col.r, col.g, col.b, SrgbAlpha(s.alphaAtEdge)); e.userData = s; return e; }
            bool vert = s.edge is "top" or "bottom"; const int N = 64;
            var tex = new Texture2D(vert ? 1 : N, vert ? N : 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "scrim-" + s.edge };
            for (int i = 0; i < N; i++)
            {
                float u = i / (N - 1f); // 0 at the edge, 1 at the band's far end
                float t = s.holdFrac >= 1f ? 0f : Mathf.Clamp01((u - s.holdFrac) / Mathf.Max(0.0001f, 1f - s.holdFrac));
                float a = s.alphaAtEdge * (1f - (s.curve == "smoothstep" ? t * t * (3f - 2f * t) : t));
                // texture rows: y = 0 is the bottom of the texture (drawn at the element's bottom)
                int px = s.edge switch { "top" => N - 1 - i, "bottom" => i, "left" => i, _ => N - 1 - i };
                var c = new Color(col.r, col.g, col.b, SrgbAlpha(a));
                if (vert) tex.SetPixel(0, px, c); else tex.SetPixel(px, 0, c);
            }
            tex.Apply(false, true);
            e.style.backgroundImage = new StyleBackground(tex);
            e.style.backgroundSize = new BackgroundSize(new Length(100, LengthUnit.Percent), new Length(100, LengthUnit.Percent));
            var frac = new Length(s.heightFrac * 100f, LengthUnit.Percent);
            switch (s.edge)
            {
                case "top": e.style.left = 0; e.style.right = 0; e.style.top = 0; e.style.height = frac; break;
                case "bottom": e.style.left = 0; e.style.right = 0; e.style.bottom = 0; e.style.height = frac; break;
                case "left": e.style.top = 0; e.style.bottom = 0; e.style.left = 0; e.style.width = frac; break;
                default: e.style.top = 0; e.style.bottom = 0; e.style.right = 0; e.style.width = frac; break;
            }
            return e;
        }

        // ---------------------------------------------------------------- build

        readonly List<VisualElement> loadingScrims = new(), menuScrims = new(), briefScrims = new(), panelScrims = new();
        Label loadHead, loadStage, loadPct; VisualElement barTrack, barFill, barCap, barShimmer;

        void Build()
        {
            blocker = E(root, pick: true); Fill(blocker); blocker.RegisterCallback<PointerDownEvent>(e => e.StopPropagation()); blocker.RegisterCallback<WheelEvent>(e => e.StopPropagation());
            blocker.style.backgroundColor = C("inkDeep");
            pictureLayer = E(blocker); Fill(pictureLayer); pictureLayer.style.overflow = Overflow.Hidden;
            pictureEl = E(pictureLayer); pictureEl.style.position = Position.Absolute;
            if (picture) { pictureEl.style.backgroundImage = new StyleBackground(picture); pictureEl.style.backgroundSize = new BackgroundSize(new Length(100, LengthUnit.Percent), new Length(100, LengthUnit.Percent)); }
            scrimLayer = E(blocker); Fill(scrimLayer);
            foreach (var s in L.scrims) loadingScrims.Add(ScrimEl(scrimLayer, s));
            foreach (var s in F.scrims.menu) menuScrims.Add(ScrimEl(scrimLayer, s));
            foreach (var s in F.scrims.brief) briefScrims.Add(ScrimEl(scrimLayer, s));
            panelVeil = E(blocker); Fill(panelVeil);
            foreach (var s in F.scrims.panel) panelScrims.Add(ScrimEl(panelVeil, s));
            BuildTitle();
            BuildLoading();
            buildLabel = T(blocker, new TypeS { font = "Jost-Regular-Tab", sizeRef = L.build.sizeRef, color = L.build.color, alpha = L.build.alpha }, BuildStamp.Line);
            buildLabel.style.position = Position.Absolute; buildLabel.style.right = L.build.rightRef; buildLabel.style.bottom = L.build.bottomRef;
            BuildMenu(); BuildBrief(); BuildPanel();
        }

        void BuildTitle()
        {
            titleGroup = E(blocker); titleGroup.style.position = Position.Absolute; titleGroup.style.flexDirection = FlexDirection.Column; titleGroup.style.alignItems = Align.FlexStart;
            var sh = L.title.shadow; Label widest = null; float prevSize = 0;
            foreach (var ln in L.title.lines)
            {
                var l = T(titleGroup, new TypeS { font = ln.font, sizeRef = ln.sizeRef, trackingEm = ln.trackingEm, color = ln.color, alpha = ln.opacity }, ln.text);
                l.style.marginTop = prevSize > 0 ? ln.gapAboveRef - 0.25f * prevSize - 0.30f * ln.sizeRef : 0; l.style.unityTextAlign = TextAnchor.UpperLeft; prevSize = ln.sizeRef;
                if (sh != null) l.style.textShadow = new TextShadow { offset = new Vector2(sh.offsetRef.Length > 0 ? sh.offsetRef[0] : 0, sh.offsetRef.Length > 1 ? sh.offsetRef[1] : 2), blurRadius = sh.blurRef, color = Hex(sh.color, sh.alpha) };
                if (widest == null || ln.sizeRef > ((TypeS)widest.userData).sizeRef) widest = l;
            }
            var rule = E(titleGroup); rule.style.height = L.title.rule.heightRef; rule.style.marginTop = L.title.rule.gapAboveRef - 0.36f * prevSize; rule.style.backgroundColor = Hex(L.title.rule.color);
            if (widest != null) widest.RegisterCallback<GeometryChangedEvent>(_ => { rule.style.width = widest.layout.width; FitTitle(); });
        }

        void BuildLoading()
        {
            loadingGroup = E(blocker); Fill(loadingGroup);
            var b = L.bar;
            barTrack = E(loadingGroup); barTrack.style.position = Position.Absolute; barTrack.style.left = b.leftRef; barTrack.style.right = b.rightRef; barTrack.style.bottom = b.bottomRef; barTrack.style.height = b.heightRef;
            barTrack.style.backgroundColor = Hex(b.track.color, b.track.alpha); Border(barTrack, b.rim.widthRef, Hex(b.rim.color, b.rim.alpha), b.radiusRef); barTrack.style.overflow = Overflow.Hidden;
            barFill = E(barTrack); barFill.style.position = Position.Absolute; barFill.style.left = 0; barFill.style.top = 0; barFill.style.bottom = 0; barFill.style.backgroundColor = Hex(b.fill.color); barFill.style.overflow = Overflow.Hidden;
            barShimmer = E(barFill); barShimmer.style.position = Position.Absolute; barShimmer.style.top = 0; barShimmer.style.bottom = 0; barShimmer.style.width = b.shimmer.widthRef;
            var sh = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var sc = Hex(b.shimmer.color);
            for (int i = 0; i < 64; i++) { float u = i / 63f; float a = Mathf.Sin(u * Mathf.PI); sh.SetPixel(i, 0, new Color(sc.r, sc.g, sc.b, b.shimmer.alpha * a * a)); }
            sh.Apply(false, true); barShimmer.style.backgroundImage = new StyleBackground(sh); barShimmer.style.backgroundSize = new BackgroundSize(new Length(100, LengthUnit.Percent), new Length(100, LengthUnit.Percent));
            barCap = E(barFill); barCap.style.position = Position.Absolute; barCap.style.right = 0; barCap.style.top = 0; barCap.style.bottom = 0; barCap.style.width = b.cap.widthRef; barCap.style.backgroundColor = Hex(b.cap.color);
            var ll = L.loadingLine;
            var line = E(loadingGroup); line.style.position = Position.Absolute; line.style.left = b.leftRef; line.style.bottom = b.bottomRef + b.heightRef + ll.gapAboveBarRef; line.style.flexDirection = FlexDirection.Row;
            var lt = new TypeS { font = "Jost-Regular-Tab", sizeRef = ll.sizeRef, trackingEm = ll.trackingEm, uppercase = ll.uppercase, color = ll.headColor };
            loadHead = T(line, lt, ll.head); loadStage = T(line, new TypeS { font = lt.font, sizeRef = lt.sizeRef, trackingEm = lt.trackingEm, uppercase = lt.uppercase, color = ll.textColor }, "");
            loadPct = T(loadingGroup, new TypeS { font = "Jost-Regular-Tab", sizeRef = ll.percent.sizeRef, color = ll.percent.color }, "0%");
            loadPct.style.position = Position.Absolute; loadPct.style.right = b.rightRef; loadPct.style.bottom = b.bottomRef + b.heightRef + ll.gapAboveBarRef;
            Show(loadPct, ll.percent.enabled); Show(line, ll.enabled);
        }

        // ---------------------------------------------------------------- the picture's cover mapping and the title

        Vector2 focus = new(0.5f, 0.5f); float push;
        /// <summary>Picture-space (0..1) to screen ref px under the cover fit and the focus (the loading spec's maths).</summary>
        public Vector2 Map(float u, float v, bool withPush = true)
        {
            if (!picture) return new Vector2(u * W, v * H);
            float s = Mathf.Max(W / picture.width, H / picture.height) * (1f + (withPush ? push : 0f));
            float dw = picture.width * s, dh = picture.height * s;
            float x0 = (W - dw) * focus.x, y0 = (H - dh) * focus.y;
            return new Vector2(x0 + u * dw, y0 + v * dh);
        }
        void LayoutPicture()
        {
            if (!picture) return;
            var a = Map(0, 0); var b = Map(1, 1);
            pictureEl.style.left = a.x; pictureEl.style.top = a.y; pictureEl.style.width = b.x - a.x; pictureEl.style.height = b.y - a.y;
            FitTitle();
        }
        void FitTitle()
        {
            if (titleGroup == null) return;
            var r = L.title.rect; var a = Map(r.x, r.y, false); var b = Map(r.x + r.w, r.y + r.h, false); // the title stays put while the picture pushes in (loading and menu match: Glyph 0.2.85)
            float margin = 64f; a.x = Mathf.Clamp(a.x, margin, W - margin); b.x = Mathf.Clamp(b.x, margin, W - margin);
            float gw = titleGroup.layout.width, gh = titleGroup.layout.height;
            float k = 1f; if (gw > 1 && gh > 1) k = Mathf.Min(L.title.maxScale, (b.x - a.x) / gw, (b.y - a.y) / gh, 1f * L.title.maxScale); k = Mathf.Min(k, L.title.maxScale);
            titleGroup.style.left = a.x; titleGroup.style.top = a.y + titleRise;
            titleGroup.style.transformOrigin = new TransformOrigin(0, 0); titleGroup.style.scale = new Scale(new Vector3(Mathf.Min(k, 1f), Mathf.Min(k, 1f), 1));
        }
        float titleRise;

        // ---------------------------------------------------------------- screens

        public void Show(Screen s, bool pause = false)
        {
            var was = screen; screen = s; if (s == Screen.None && was != Screen.None) ClosedFrame = Time.frameCount; fromPause = pause && (s == Screen.Options || s == Screen.Controls);
            bool pic = !fromPause && s != Screen.None;
            Show(blocker, s != Screen.None);
            blocker.style.backgroundColor = pic ? C("inkDeep") : Color.clear;
            Show(pictureLayer, pic); Show(scrimLayer, pic); Show(titleGroup, pic && s is Screen.Loading or Screen.Menu);
            foreach (var e in loadingScrims) Show(e, pic && s is Screen.Loading or Screen.Menu);
            foreach (var e in menuScrims) Show(e, pic && s == Screen.Menu);
            foreach (var e in briefScrims) Show(e, pic && s == Screen.Brief);
            Show(panelVeil, s is Screen.Options or Screen.Controls);
            foreach (var e in panelScrims) { var sc = e.userData as Scrim; var vc = Hex(sc?.color ?? F.colors.inkDeep); e.style.opacity = 1f; e.style.backgroundColor = new Color(vc.r, vc.g, vc.b, SrgbAlpha(fromPause ? 0.40f : sc?.alphaAtEdge ?? 0.52f)); } // from the pause menu: the even veil at 0.40 over the frozen game
            Show(loadingGroup, s == Screen.Loading); Show(menuGroup, s == Screen.Menu); Show(briefGroup, s == Screen.Brief);
            Show(panelGroup, s is Screen.Options or Screen.Controls);
            Show(buildLabel, pic && s != Screen.Brief);
            focus = s == Screen.Brief && F.brief.imageFocus.Length == 2 ? new Vector2(F.brief.imageFocus[0], F.brief.imageFocus[1]) : new Vector2(L.image.focus.Length == 2 ? L.image.focus[0] : 0.5f, L.image.focus.Length == 2 ? L.image.focus[1] : 0.5f);
            CoverGame(pic);
            if (s == Screen.Menu) MenuShow(was); else if (s == Screen.Brief) BriefShow(); else if (s is Screen.Options or Screen.Controls) PanelShow(s == Screen.Options);
            shown = Time.unscaledTime; FocusFirst();
            LayoutPicture();
        }
        float shown;

        /// <summary>The game's cameras stand down under a full picture (nothing the player sees is drawn twice); they come back as they were.</summary>
        readonly List<Camera> covered = new();
        Camera ownCam;
        /// <summary>One camera always draws while the game's are down (clears to ink-deep, culls nothing, no post): with no camera at all the Metal
        /// player never presents a frame and stalls (seen in the first front-end proof run).</summary>
        void CoverGame(bool on)
        {
            if (on)
            {
                if (!ownCam)
                {
                    var go = new GameObject("front-end camera"); go.transform.SetParent(transform, false);
                    ownCam = go.AddComponent<Camera>(); ownCam.clearFlags = CameraClearFlags.SolidColor; ownCam.backgroundColor = C("inkDeep"); ownCam.cullingMask = 0; ownCam.depth = -100; ownCam.enabled = false;
                    var ud = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ud.renderPostProcessing = false; ud.renderShadows = false;
                }
                foreach (var c in Camera.allCameras) if (c && c != ownCam && c.enabled && c.targetTexture == null && c.cameraType == CameraType.Game && c.gameObject.scene.name != BootScene) { c.enabled = false; covered.Add(c); }
                if (!ownCam.enabled) ownCam.enabled = true;
            }
            else { foreach (var c in covered) if (c) c.enabled = true; covered.Clear(); if (ownCam) ownCam.enabled = false; }
        }

        void Update()
        {
            if (screen == Screen.None) return;
            if (!fromPause) CoverGame(true); // a camera the scene brought in under the picture (the load) stands down too
            if (L.image.pushIn != null && L.image.pushIn.scale > 0 && screen is Screen.Loading or Screen.Menu) { push = L.image.pushIn.scale * Mathf.Clamp01((Time.unscaledTime - pushStart) / Mathf.Max(1f, L.image.pushIn.seconds)); }
            LayoutPicture();
            if (screen == Screen.Loading) LoadingFrame();
            else Keys();
            Animate();
            if (screen is Screen.Options or Screen.Controls) PanelFrame();
        }
        float pushStart;

        // ---------------------------------------------------------------- loading (the real progress: the scene, the people, the mission)

        float barTarget, barShown, barVel; string stage = "";
        public float BarShown => barShown;
        public void SetProgress(float v, string st) { barTarget = Mathf.Max(barTarget, Mathf.Clamp01(v)); stage = st ?? ""; }
        /// <summary>The front-end proof's frame: the bar as the loader shows it mid-way.</summary>
        public void ForceProgress(float v, string st) { barTarget = barShown = v; barVel = 0; stage = st; LoadingFrame(); }

        void LoadingFrame()
        {
            barShown = Mathf.SmoothDamp(barShown, barTarget, ref barVel, Mathf.Max(0.01f, L.timing.barSmoothSeconds), Mathf.Infinity, Time.unscaledDeltaTime);
            if (barTarget >= 1f && Mathf.Abs(barShown - 1f) < 0.005f) barShown = 1f;
            barFill.style.width = new Length(barShown * 100f, LengthUnit.Percent);
            float fw = barTrack.layout.width * barShown, sw = L.bar.shimmer.widthRef, per = Mathf.Max(0.2f, L.bar.shimmer.seconds);
            Show(barShimmer, barShown < 0.999f && fw > 4); barShimmer.style.left = Mathf.Repeat(Time.unscaledTime / per, 1f) * (fw + sw) - sw;
            loadStage.text = stage.Length > 0 ? $" {L.loadingLine.separator} {(L.loadingLine.uppercase ? stage.ToUpperInvariant() : stage)}" : "";
            loadPct.text = $"{Mathf.RoundToInt(barShown * 100f)}%";
        }

        IEnumerator LoadGame()
        {
            pushStart = Time.unscaledTime; Show(Screen.Loading);
            FadeIn(pictureLayer, 0f, L.timing.fadeInSeconds); FadeIn(titleGroup, L.timing.titleDelaySeconds, L.timing.titleFadeSeconds, L.timing.titleRiseRef);
            FadeIn(loadingGroup, L.timing.barDelaySeconds, L.timing.barFadeSeconds); FadeIn(buildLabel, L.timing.barDelaySeconds, L.timing.barFadeSeconds);
            float t0 = Time.unscaledTime; yield return null;
            SetProgress(0.02f, "Harbour District");
            var op = SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Single);
            if (op == null) { Debug.LogError("FrontEnd: HarbourDistrict is not in the build"); yield break; }
            while (!op.isDone) { SetProgress(op.progress / 0.9f * 0.8f, "Harbour District"); yield return null; }
            SetProgress(0.85f, "People");
            HandoverHost h = null; float tw = Time.unscaledTime;
            while (((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) && Time.unscaledTime - tw < 60f) { yield return null; }
            SetProgress(0.95f, "Plan"); yield return null; yield return null;
            SetProgress(1f, "Mission");
            if (shotsMode) yield break; // the front-end proof drives the screens from here (FrontEnd.Proof)
            while (barShown < 0.999f || Time.unscaledTime - t0 < L.timing.minShowSeconds) yield return null;
            yield return new WaitForSecondsRealtime(L.timing.completeHoldSeconds);
            FadeOut(loadingGroup, L.timing.uiFadeOutSeconds); yield return new WaitForSecondsRealtime(L.timing.uiFadeOutSeconds);
            Show(Screen.Menu);
        }
        bool shotsMode => GameBoot.ArgValue("-frontEndShots") != null;

        // ---------------------------------------------------------------- simple fades (unscaled time)

        sealed class Fade { public VisualElement E; public float From, To, Start, Dur, Rise; }
        readonly List<Fade> fades = new();
        void FadeIn(VisualElement e, float delay, float dur, float rise = 0) { if (e == null) return; e.style.opacity = 0; fades.RemoveAll(f => f.E == e); fades.Add(new Fade { E = e, From = 0, To = 1, Start = Time.unscaledTime + delay, Dur = Mathf.Max(0.01f, dur), Rise = rise }); }
        void FadeOut(VisualElement e, float dur) { if (e == null) return; fades.RemoveAll(f => f.E == e); fades.Add(new Fade { E = e, From = e.resolvedStyle.opacity, To = 0, Start = Time.unscaledTime, Dur = Mathf.Max(0.01f, dur) }); }
        void Animate()
        {
            for (int i = fades.Count - 1; i >= 0; i--)
            {
                var f = fades[i]; float u = Mathf.Clamp01((Time.unscaledTime - f.Start) / f.Dur); float e = 1f - (1f - u) * (1f - u); // ease out
                f.E.style.opacity = Mathf.Lerp(f.From, f.To, e);
                if (f.Rise > 0) { if (f.E == titleGroup) { titleRise = f.Rise * (1f - e); FitTitle(); } else f.E.style.translate = new Translate(0, f.Rise * (1f - e)); }
                if (u >= 1f) fades.RemoveAt(i);
            }
        }
        /// <summary>The proof's still: every fade at its end.</summary>
        public void Settle() { foreach (var f in fades) { f.E.style.opacity = f.To; f.E.style.translate = new Translate(0, 0); } fades.Clear(); titleRise = 0; FitTitle(); }

        // ---------------------------------------------------------------- focus and keys (Up/Down move, Enter/Space take, Left/Right change, Tab/Q/E tabs, Esc back)

        public sealed class Focusable
        {
            public VisualElement El, Ring; public Action Activate; public Action<int, bool> Adjust; public bool Row; public Func<bool> Visible;
        }
        readonly List<Focusable> focusables = new();
        int focusIx = -1; bool keyFocus;
        IEnumerable<Focusable> Current() { var l = focusables.Where(f => f.El != null && IsShown(f.El) && (f.Visible == null || f.Visible())).ToList(); return l.Where(f => f.Row).Concat(l.Where(f => !f.Row)); } // a page's rows, then its footer
        static bool IsShown(VisualElement e) { for (var p = e; p != null; p = p.parent) if (p.resolvedStyle.display == DisplayStyle.None || p.style.display == DisplayStyle.None) return false; return true; }
        void FocusFirst() { keyFocus = screen is Screen.Menu or Screen.Brief; focusIx = -1; /* the default focus ring shows on open (Glyph 0.2.85 #8); the mouse hover stays the sprite alone */ var list = Current().ToList(); if (list.Count > 0) { focusIx = focusables.IndexOf(DefaultFocus(list)); } PaintFocus(); }
        Focusable DefaultFocus(List<Focusable> list) => screen switch { Screen.Brief => list.LastOrDefault() ?? list[0], _ => list[0] };
        void PaintFocus()
        {
            for (int i = 0; i < focusables.Count; i++)
            {
                var f = focusables[i]; bool on = i == focusIx && keyFocus;
                if (f.Ring != null) Show(f.Ring, on);
            }
        }
        void MoveFocus(int d)
        {
            var list = Current().ToList(); if (list.Count == 0) return;
            int at = focusIx >= 0 ? list.IndexOf(focusables[focusIx]) : -1;
            at = at < 0 ? 0 : (at + d + list.Count) % list.Count;
            focusIx = focusables.IndexOf(list[at]); keyFocus = true; PaintFocus();
        }
        /// <summary>The proof's keyboard step (Down = +1).</summary>
        public void KeyMove(int d) => MoveFocus(d);
        Focusable Focused => focusIx >= 0 && focusIx < focusables.Count ? focusables[focusIx] : null;

        void Keys()
        {
            var kb = Keyboard.current; if (kb == null) return;
            if (listening != null) { ListenFrame(kb); return; }
            if (Time.unscaledTime - shown < 0.05f) return;
            if (kb.escapeKey.wasPressedThisFrame) { Back(); return; }
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) MoveFocus(-1);
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) MoveFocus(1);
            bool shift = kb.shiftKey.isPressed;
            if (kb.tabKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) { if (screen is Screen.Options or Screen.Controls) StepTab(kb.qKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame && shift ? -1 : 1); }
            var f = Focused; if (f == null || !keyFocus) { if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) { keyFocus = true; if (focusIx < 0) MoveFocus(1); PaintFocus(); } return; }
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) f.Activate?.Invoke();
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) f.Adjust?.Invoke(-1, shift);
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) f.Adjust?.Invoke(1, shift);
        }

        /// <summary>Esc: closes the rebind prompt, else back one screen; on the main menu it does nothing; on the brief nothing (a story beat).</summary>
        public void Back()
        {
            switch (screen)
            {
                case Screen.Options: case Screen.Controls:
                    if (clash != null) { ClashCancel(); return; }
                    if (fromPause) { Show(Screen.None); return; } // the pause menu (still open under it) takes over again
                    Show(Screen.Menu); return;
            }
        }

        // ---------------------------------------------------------------- buttons (the kit's cream text plate, the focus ring as the HUD's "ready")

        public VisualElement Button(VisualElement parent, string text, float w, float h, Action act, TypeS type, out Focusable fo, bool small = false)
        {
            var b = E(parent, small ? "s-plate_dark" : "s-btn_text_normal", w: w, h: h, pick: true);
            if (!small) b.style.unitySliceScale = h / 80f; // Glyph 0.2.85 #3: the plate's tips follow its height (the kit's class fixes 0.5px); kept across the hover and pressed classes
            b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center; b.style.flexShrink = 0;
            var l = T(b, type, text, small ? "cream" : null); l.style.unityTextAlign = TextAnchor.MiddleCenter;
            var ring = E(b); ring.style.position = Position.Absolute; ring.style.left = -4; ring.style.top = -4; ring.style.right = -4; ring.style.bottom = -4;
            Border(ring, F.button.focus.ringRef, C("live"), h * 0.3f + 4); // the chamfer's line (Glyph 0.2.85 #8) ring.style.backgroundColor = C("live", F.button.focus.glowAlpha * 0.25f); Show(ring, false);
            var f = new Focusable { El = b, Ring = ring, Activate = act }; focusables.Add(f); fo = f;
            if (!small)
            {
                b.RegisterCallback<PointerEnterEvent>(_ => { b.RemoveFromClassList("s-btn_text_normal"); b.AddToClassList("s-btn_text_hover"); });
                b.RegisterCallback<PointerLeaveEvent>(_ => { b.RemoveFromClassList("s-btn_text_hover"); b.RemoveFromClassList("s-btn_text_pressed"); b.AddToClassList("s-btn_text_normal"); });
                b.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { b.RemoveFromClassList("s-btn_text_hover"); b.AddToClassList("s-btn_text_pressed"); e.StopPropagation(); } });
            }
            else b.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) e.StopPropagation(); });
            b.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.button != 0) return;
                if (!small) { b.RemoveFromClassList("s-btn_text_pressed"); b.AddToClassList("s-btn_text_hover"); }
                keyFocus = false; PaintFocus(); act?.Invoke(); e.StopPropagation();
            });
            b.userData = l;
            return b;
        }

        // ---------------------------------------------------------------- main menu

        readonly Dictionary<string, (VisualElement b, Focusable f)> menuButtons = new();
        /// <summary>The run saved on Quit to menu that Continue returns to in this session (the live game), or the newest saved session on disk.</summary>
        string continuePath; bool liveRun;
        public bool ContinueShown { get; private set; }
        public static bool ForceNoSave; // the proof's "menu-nosave" frame

        void BuildMenu()
        {
            menuGroup = E(blocker); Fill(menuGroup);
            var m = F.menu;
            var col = E(menuGroup); col.style.position = Position.Absolute; col.style.left = m.leftRef; col.style.bottom = m.bottomRef; col.style.flexDirection = FlexDirection.Column;
            // brief 051 (Aster's default until Sky says): New game first, Continue second (only when a saved run exists)
            foreach (var (id, label) in new[] { ("new", "New game"), ("continue", "Continue"), ("options", "Options"), ("controls", "Controls"), ("quit", "Quit") })
            {
                var b = Button(col, label, m.widthRef, m.heightRef, () => MenuAct(id), F.type.menuButton, out var fo);
                b.style.marginTop = m.gapRef; menuButtons[id] = (b, fo); b.name = "menu-" + id;
            }
        }

        void MenuShow(Screen was)
        {
            ContinueShown = !ForceNoSave && (LiveRunAvailable() || (continuePath = NewestSession()) != null);
            Show(menuButtons["continue"].b, ContinueShown);
            int i = 0;
            foreach (var id in new[] { "new", "continue", "options", "controls", "quit" })
            {
                var b = menuButtons[id].b; if (!ContinueShown && id == "continue") continue;
                if (was is Screen.Loading or Screen.None or Screen.Brief) FadeIn(b, F.timing.menuFadeInAfterLoading * 0 + F.timing.buttonStaggerSeconds * i, F.timing.menuFadeInAfterLoading, F.timing.buttonRiseRef);
                else { b.style.opacity = 1; b.style.translate = new Translate(0, 0); }
                i++;
            }
        }

        /// <summary>The proof's press of a main-menu button (null when it is not shown).</summary>
        public Action MenuButton(string id) => menuButtons.TryGetValue(id, out var m) && IsShown(m.b) ? () => MenuAct(id) : null;

        bool LiveRunAvailable() { var h = FindAnyObjectByType<HandoverHost>(); return liveRun && h != null && h.Game != null && !h.Game.Over && !h.rangeMode; }

        /// <summary>The newest saved Handover session that can be continued: not ended, at least one order, recorded by this engine.</summary>
        public static string NewestSession()
        {
            try
            {
                var dir = Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "out", "play")) : Path.Combine(Application.persistentDataPath, "play");
                if (!Directory.Exists(dir)) return null;
                foreach (var f in Directory.GetFiles(dir, "handover-*.script.json").OrderByDescending(File.GetLastWriteTimeUtc).Take(12))
                {
                    try
                    {
                        var sc = Suborned.Sim.Script.Parse(File.ReadAllText(f));
                        if (sc.Orders.Count == 0 || sc.Until <= 0) continue;
                        if (sc.Expect != null && (sc.Expect.Outcome != "none" || sc.Expect.Engine != Suborned.Sim.Engine.Version)) continue;
                        return f;
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception e) { Debug.LogWarning("FrontEnd: saved sessions not read: " + e.Message); }
            return null;
        }

        void MenuAct(string id)
        {
            var h = FindAnyObjectByType<HandoverHost>();
            switch (id)
            {
                case "new":
                    if (h != null && (h.rangeMode)) { pendingAfterLoad = "brief"; SceneManager.LoadScene(GameScene); return; }
                    if (h != null && (h.Game == null || h.Game.Tick > 0 || h.Game.Orders.Count > 0 || h.Continued)) h.Begin(); // a fresh run (the one left is saved at its last tick)
                    liveRun = false; Show(Screen.Brief); return;
                case "continue":
                    if (LiveRunAvailable()) { Show(Screen.None); return; }
                    if (continuePath != null && h != null) { h.ContinueFrom(continuePath); liveRun = false; Show(Screen.None); }
                    return;
                case "options": Show(Screen.Options); return;
                case "controls": Show(Screen.Controls); return;
                case "quit":
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit(0);
#endif
                    return;
            }
        }

        string pendingAfterLoad;
        void OnEnable() { SceneManager.sceneLoaded += SceneLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= SceneLoaded; }
        void SceneLoaded(Scene s, LoadSceneMode m)
        {
            if (pendingAfterLoad == null || s.name != GameScene) return;
            var p = pendingAfterLoad; pendingAfterLoad = null; StartCoroutine(AfterLoad(p));
        }
        IEnumerator AfterLoad(string what)
        {
            HandoverHost h = null; float t0 = Time.unscaledTime;
            while (((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) && Time.unscaledTime - t0 < 60f) yield return null;
            if (what == "menu") { liveRun = false; Show(Screen.Menu); } else if (what == "brief") Show(Screen.Brief);
        }

        /// <summary>The pause menu's Quit to menu: the run is saved (as Quit does) and the menu comes up over it; Continue returns to it. From the fight
        /// range the Handover loads first.</summary>
        public static void QuitToMenu(HandoverHost h)
        {
            var fe = Ensure();
            if (h != null && h.rangeMode) { fe.pendingAfterLoad = "menu"; SceneManager.LoadScene(GameScene); return; }
            fe.liveRun = h != null && h.Game != null && !h.Game.Over; fe.Show(Screen.Menu);
        }

        // ---------------------------------------------------------------- mission brief

        Label briefTitle; VisualElement briefText;
        /// <summary>The brief as a dossier (Fable's /2; Glyph's paragraph mockup predates it): the title and the brass rule, then in `order`: the hook (Jost Medium,
        /// larger), the handler's lines, the dossier rows (a dim head word, the value), the intel as short bulleted lines, the sign-off; Continue bottom right.
        /// Sizes and colours are frontend.json's brief type and the kit's.</summary>
        void BuildBrief()
        {
            briefGroup = E(blocker); Fill(briefGroup);
            var c = F.brief.column; var v = brief.Picked;
            var colm = E(briefGroup); colm.style.position = Position.Absolute; colm.style.right = c.rightRef; colm.style.top = c.topRef; colm.style.width = c.widthRef; colm.style.flexDirection = FlexDirection.Column; colm.name = "brief-column";
            briefTitle = T(colm, F.type.briefTitle, brief.title); briefTitle.style.whiteSpace = WhiteSpace.Normal;
            var rule = E(colm); rule.style.height = F.brief.headRule.heightRef; rule.style.marginTop = F.brief.headRule.gapAboveRef; rule.style.marginBottom = F.brief.headRule.gapBelowRef; rule.style.backgroundColor = C("brass");
            var body = F.type.briefBody; var head = F.type.briefHead; float gap = Mathf.Max(14f, body.paragraphGapRef) + 8f;
            var valueT = new TypeS { font = body.font, sizeRef = 17f, color = "cream" };
            briefText = E(colm); briefText.style.flexDirection = FlexDirection.Column;
            var keys = new List<Label>();
            // the key column: Glyph's 96 at least, else the widest key (OBJECTIVE in Jost Medium 14.5 tracked 0.14 runs past 96) and a 14 gap, so the values still align
            bool keysSized = false;
            briefText.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (keysSized || keys.Count == 0 || keys.Any(kk => float.IsNaN(kk.layout.width) || kk.layout.width <= 0)) return;
                float w = 96f; foreach (var kk in keys) w = Mathf.Max(w, kk.layout.width); // the natural widths, measured once
                keysSized = true; foreach (var kk in keys) kk.style.width = w + 14f;
            });
            Label Line(VisualElement parent, TypeS t, string text, float below) { var l = T(parent, t, text); l.style.whiteSpace = WhiteSpace.Normal; l.style.marginBottom = below; return l; }
            VisualElement Row(string k, string val)
            {
                var row = E(briefText); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.FlexStart; row.style.marginBottom = 8;
                var hk = T(row, head, k); hk.style.minWidth = 96; hk.style.marginTop = 3; hk.style.flexShrink = 0; keys.Add(hk);
                var hv = T(row, valueT, val); hv.style.whiteSpace = WhiteSpace.Normal; hv.style.flexShrink = 1; hv.style.flexGrow = 1;
                return row;
            }
            foreach (var part in brief.order ?? Array.Empty<string>())
                switch (part)
                {
                    case "hook": if (!string.IsNullOrWhiteSpace(v.hook)) Line(briefText, new TypeS { font = "Jost-Medium", sizeRef = 24f, color = "cream" }, v.hook, gap); break;
                    case "handler":
                        if (v.handler != null && v.handler.Length > 0) { foreach (var h in v.handler) if (!string.IsNullOrWhiteSpace(h)) Line(briefText, body, h, 6f); briefText[briefText.childCount - 1].style.marginBottom = gap; }
                        break;
                    case "dossier":
                        var d = v.dossier; int n0 = briefText.childCount;
                        if (!string.IsNullOrWhiteSpace(d.target)) Row("Target", d.target); if (!string.IsNullOrWhiteSpace(d.location)) Row("Location", d.location);
                        if (!string.IsNullOrWhiteSpace(d.window)) Row("Window", d.window); if (!string.IsNullOrWhiteSpace(d.objective)) Row("Objective", d.objective);
                        if (briefText.childCount > n0) briefText[briefText.childCount - 1].style.marginBottom = gap;
                        break;
                    case "intel":
                        if (v.intel != null && v.intel.Length > 0)
                        {
                            Line(briefText, head, "Intel", 8f);
                            foreach (var it in v.intel)
                            {
                                if (string.IsNullOrWhiteSpace(it)) continue;
                                var row = E(briefText); row.style.flexDirection = FlexDirection.Row; row.style.marginBottom = 5;
                                var dot = E(row, w: 5, h: 5); dot.style.backgroundColor = C("live"); Radius(dot, 2.5f); dot.style.marginTop = 9; dot.style.marginRight = 12; dot.style.flexShrink = 0;
                                var l = T(row, valueT, it); l.style.whiteSpace = WhiteSpace.Normal; l.style.flexShrink = 1;
                            }
                            briefText[briefText.childCount - 1].style.marginBottom = gap;
                        }
                        break;
                    case "signoff": if (!string.IsNullOrWhiteSpace(v.signoff)) Line(briefText, new TypeS { font = "Jost-Medium", sizeRef = 18f, trackingEm = 0.04f, color = "cream" }, v.signoff, 0f); break;
                }
            var bb = F.brief.button;
            var b = Button(briefGroup, bb.label, bb.w, bb.h, BriefContinue, F.type.button, out _); b.style.position = Position.Absolute; b.style.right = c.rightRef; b.style.bottom = c.bottomRef; b.name = "brief-continue";
        }
        void BriefShow() { FadeIn(briefGroup, 0f, F.timing.screenFadeSeconds); }
        void BriefContinue() { Show(Screen.None); } // the host's own tactical briefing (modal until Begin) is up under it

        /// <summary>Words of the brief (for the proof's report).</summary>
        public int BriefWords { get { var v = brief.Picked; var all = new List<string> { v.hook, v.signoff, v.dossier.target, v.dossier.location, v.dossier.window, v.dossier.objective }; all.AddRange(v.handler ?? Array.Empty<string>()); all.AddRange(v.intel ?? Array.Empty<string>()); return all.Where(x => x != null).Sum(p => p.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length); } }
        public string BriefPick => brief.pick;
    }
}
