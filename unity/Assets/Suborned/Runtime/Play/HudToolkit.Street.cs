using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101, the HUD's street kit (Sky 2026-11-08: "street labels top right but tokens bottom right"; Glyph's round 2, briefs/101/glyph/r2, the
    /// S mocks for the top right and the T mocks for the bottom right). Styles in Kit/street.uss (the book's colours; kit.uss is generated and never edited).
    ///  · A street label (KeyText): bold Jost in the book's ink with a solid cream keyline and a crisp shadow, so a word reads over paving, asphalt, palms
    ///    and the fountain alike; an urgent figure is orange with an ink keyline. UI Toolkit's own text outline straddles the glyph's edge (UnityUIE.cginc:
    ///    the face contracts by what the outline dilates) and is capped by the font atlas's padding (under 1 px at 17 px), so the keyline is eight key copies
    ///    a keyline's width out under the type, which keeps its full weight; the shadow is one faint ink copy under the keyline's lower edge.
    ///  · A keyed icon (KeyIcon): a kit pictogram in a fill over the same eight key copies.
    ///  · Enamel, for the bottom right only: the roundel (a disc in the book's deep teal or orange, a cream inline 4 px in) and the lozenge (deep teal, the
    ///    inline 3.5 px in), each over a crisp shadow 2.5 px down; no chrome, no screws.
    /// Cost: plain elements and labels, no painter, no blur, no full-screen pass; a label's copies are set only when its words change.
    /// The budget (tools/hud-budget) reads a run as inside an element by the run's centre, and keylines join neighbours into one run (a button and its time,
    /// an icon and its figure): declare the whole row or group in the manifest (the clock's row, the seal's row), never the bare label.</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>Glyph's keyline at 1080 (r2_base.keyed: 2.6 px for ink type, 2.2 for orange on ink) and the shadow's drop (1.2 px under the keyline).</summary>
        public const float KeyW = 2.6f, KeyHotW = 2.2f, KeyDrop = 1.2f;
        static readonly Vector2[] KeyDirs = { new(1f, 0f), new(0.7071f, 0.7071f), new(0f, 1f), new(-0.7071f, 0.7071f), new(-1f, 0f), new(-0.7071f, -0.7071f), new(0f, -1f), new(0.7071f, -0.7071f) };
        StyleSheet streetSheet;

        /// <summary>The street kit's sheet on the HUD's root (once).</summary>
        void StreetSheet()
        {
            if (streetSheet != null || root == null) return;
            streetSheet = Resources.Load<StyleSheet>("Kit/street"); if (streetSheet != null) root.styleSheets.Add(streetSheet); else Debug.LogWarning("HUD: Kit/street.uss is missing; the street labels draw unstyled");
        }

        /// <summary>A street label: Root holds the shadow copies, the key copies and the face (in flow, so Root measures as the type does; the copies
        /// reach a keyline's width out of it). Set its words with Set, never on the labels.</summary>
        public sealed class KeyText
        {
            public VisualElement Root; public Label Face; public readonly Label[] Copies; public string Words { get; private set; } = "";
            public KeyText(int copies) { Copies = new Label[copies]; }
            public void Set(string s)
            {
                s ??= ""; if (s == Words) return; Words = s; Face.text = s;
                foreach (var c in Copies) c.text = s;
            }
            /// <summary>The fill and key classes (k-ink on k-key, k-hot on k-key-ink): an urgent figure turns orange on an ink keyline.</summary>
            public void Look(string fill, string key)
            {
                foreach (var f in new[] { "k-ink", "k-hot", "k-page", "k-soft", "k-deep" }) Class(Face, f, f == fill);
                for (int i = 0; i < Copies.Length; i++) if (!Copies[i].ClassListContains("k-shadow")) { Class(Copies[i], "k-key", key == "k-key"); Class(Copies[i], "k-key-ink", key == "k-key-ink"); }
            }
        }

        static Label KLabel(VisualElement parent, string face, string colour, float size)
        {
            var l = new Label { pickingMode = PickingMode.Ignore, focusable = false }; l.AddToClassList("k-type"); l.AddToClassList(face); l.AddToClassList(colour);
            l.style.fontSize = size; l.style.flexShrink = 0; parent.Add(l); return l;
        }

        /// <summary>A street label of `size` px in `face` (k-bold, k-semi, k-medium): `fill` over a `key` keyline `kw` wide; `shadow` adds the crisp shadow.
        /// The copies are stretched over the face's own box and moved by a translate: a label left to size itself when absolute set its line ~0.15 em lower
        /// than the face in flow (0.2.274's keyline sat under the type, none above it).</summary>
        static KeyText KText(VisualElement parent, float size, string face = "k-bold", string fill = "k-ink", string key = "k-key", float kw = KeyW, bool shadow = true)
        {
            var t = new KeyText(KeyDirs.Length + (shadow ? 1 : 0)); var r = t.Root = new VisualElement { pickingMode = PickingMode.Ignore, focusable = false };
            r.style.flexShrink = 0; r.style.overflow = Overflow.Visible; parent?.Add(r); int n = 0;
            if (shadow) { var s = KLabel(r, face, "k-shadow", size); Over(s, new Vector2(0f, kw + KeyDrop)); t.Copies[n++] = s; } // one copy, a keyline's width and the drop down: the keyline's lower edge on pale ground
            foreach (var d in KeyDirs) { var c = KLabel(r, face, key, size); Over(c, d * kw); t.Copies[n++] = c; }
            t.Face = KLabel(r, face, fill, size);
            return t;
        }

        /// <summary>A copy over its root's whole box (so its type sits exactly as the face's), moved by `by`.</summary>
        static void Over(VisualElement e, Vector2 by)
        {
            e.style.position = Position.Absolute; e.style.left = 0; e.style.top = 0; e.style.right = 0; e.style.bottom = 0; e.style.translate = new Translate(by.x, by.y);
        }

        static void Pin(VisualElement e, Vector2 at) { e.style.position = Position.Absolute; e.style.left = at.x; e.style.top = at.y; }
        /// <summary>The book's page and ink as solid colours (BkPaper carries the page's 97 % alpha), for the lamps and the rings drawn in code.</summary>
        static Color StPage => new(BkPaper.r, BkPaper.g, BkPaper.b, 1f); // a property: static initialisers across partial files run in no set order

        /// <summary>A keyed icon: a kit pictogram (icons/singles) `d` px in a fill class (ki-ink, ki-hot, ki-deep, ki-page) over the eight key copies.</summary>
        public sealed class KeyIcon
        {
            public VisualElement Root, Face; public readonly VisualElement[] Copies = new VisualElement[8]; public string Name { get; private set; } = "";
            public void Look(string fill, string key)
            {
                foreach (var f in new[] { "ki-ink", "ki-hot", "ki-deep", "ki-page" }) Class(Face, f, f == fill);
                foreach (var c in Copies) { Class(c, "ki-key", key == "ki-key"); Class(c, "ki-key-ink", key == "ki-key-ink"); }
            }
            public void Set(HudToolkit h, string name)
            {
                if (name == Name) return; Name = name ?? ""; bool on = Name.Length > 0; Show(Root, on); if (!on) return;
                var sp = h.Sp("icons/singles/" + Name); var bg = new StyleBackground(sp); Face.style.backgroundImage = bg; foreach (var c in Copies) c.style.backgroundImage = bg;
            }
        }

        KeyIcon KIcon(VisualElement parent, string name, float d, string fill = "ki-ink", string key = "ki-key", float kw = 2.2f)
        {
            var k = new KeyIcon(); var r = k.Root = new VisualElement { pickingMode = PickingMode.Ignore, focusable = false };
            r.style.width = d; r.style.height = d; r.style.flexShrink = 0; r.style.overflow = Overflow.Visible; parent?.Add(r);
            for (int i = 0; i < KeyDirs.Length; i++) { var c = k.Copies[i] = new VisualElement { pickingMode = PickingMode.Ignore }; c.AddToClassList(key); Pin(c, KeyDirs[i] * kw); c.style.width = d; c.style.height = d; r.Add(c); }
            var f = k.Face = new VisualElement { pickingMode = PickingMode.Ignore }; f.AddToClassList(fill); Pin(f, Vector2.zero); f.style.width = d; f.style.height = d; r.Add(f);
            k.Set(this, name); return k;
        }

        /// <summary>An enamel roundel `d` px: its shadow, the disc in `fill` (e-deep, e-orange, e-body), the cream inline `inset` px in. Content goes in Root
        /// after it (absolute, or centred: Root centres its in-flow children). Returns (root, inline) so a hover can light the inline.</summary>
        static (VisualElement root, VisualElement inline) Roundel(VisualElement parent, float d, string fill, float inlineW = 1.6f, float inset = 4f, bool pick = false)
        {
            var r = new VisualElement { pickingMode = pick ? PickingMode.Position : PickingMode.Ignore, focusable = false }; r.style.width = d; r.style.height = d; r.style.flexShrink = 0;
            r.style.alignItems = Align.Center; r.style.justifyContent = Justify.Center; r.style.overflow = Overflow.Visible; parent?.Add(r);
            var sh = Disc(r, d, "e-shadow"); Pin(sh, new Vector2(0f, 2.5f));
            var body = Disc(r, d, fill); Pin(body, Vector2.zero);
            var il = new VisualElement { pickingMode = PickingMode.Ignore }; il.AddToClassList("e-inline"); Pin(il, new Vector2(inset, inset)); il.style.width = il.style.height = d - 2 * inset;
            Round(il, (d - 2 * inset) / 2f); BorderW(il, inlineW); r.Add(il);
            return (r, il);
        }

        static VisualElement Disc(VisualElement parent, float d, string cls)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore }; e.AddToClassList(cls); e.style.width = d; e.style.height = d; Round(e, d / 2f); parent.Add(e); return e;
        }

        static void BorderW(VisualElement e, float w) { e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = w; }

        /// <summary>An enamel lozenge `h` px tall that takes its content's width: shadow, body (e-deep) and the cream inline 3.5 px in, stretched under
        /// Root's in-flow content. Returns (root, inline).</summary>
        static (VisualElement root, VisualElement inline) Lozenge(VisualElement parent, float h, string fill = "e-deep", bool pick = false)
        {
            var r = new VisualElement { pickingMode = pick ? PickingMode.Position : PickingMode.Ignore, focusable = false }; r.style.height = h; r.style.flexShrink = 0;
            r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.overflow = Overflow.Visible; parent?.Add(r);
            VisualElement Layer(string cls, float inset, float dy)
            {
                var e = new VisualElement { pickingMode = PickingMode.Ignore }; e.AddToClassList(cls); e.style.position = Position.Absolute;
                e.style.left = inset; e.style.right = inset; e.style.top = inset + dy; e.style.bottom = inset - dy; Round(e, h / 2f - inset); r.Add(e); return e;
            }
            Layer("e-shadow", 0f, 2.5f); Layer(fill, 0f, 0f); var il = Layer("e-inline", 3.5f, 0f); BorderW(il, 1.4f);
            return (r, il);
        }
    }
}
