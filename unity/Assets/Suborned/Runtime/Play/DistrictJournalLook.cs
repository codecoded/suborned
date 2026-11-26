using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    public sealed partial class DistrictJournal
    {
        enum Section { Faces, Leads, Requests }
        Section section, returnTo;
        VisualElement frame, mast, bar, foot;
        Font bodyFont, deckFont, serifFont;
        Texture2D faces, skyline;
        readonly Dictionary<string, int> faceCells = new(StringComparer.Ordinal);
        static Color C(byte r, byte g, byte b) => new Color32(r, g, b, 255);
        static readonly Color Deep = C(21, 46, 43), Deck = C(32, 55, 48), Trim = C(125, 137, 113),
            Cream = C(248, 236, 211), Brass = C(237, 196, 130), Paper = C(244, 239, 225),
            Ivory = C(32, 54, 48), Gold = C(43, 92, 75), Blue = C(71, 95, 84);
        static void All(VisualElement e, float n)
        {
            e.style.paddingLeft = e.style.paddingRight = e.style.paddingTop = e.style.paddingBottom = n;
        }
        static void Border(VisualElement e, float n, Color c)
        {
            e.style.borderLeftWidth = e.style.borderRightWidth = e.style.borderTopWidth = e.style.borderBottomWidth = n;
            e.style.borderLeftColor = e.style.borderRightColor = e.style.borderTopColor = e.style.borderBottomColor = c;
        }
        static void Full(VisualElement e)
        {
            e.style.position = Position.Absolute;
            e.style.left = e.style.top = e.style.right = e.style.bottom = 0;
        }
        static VisualElement Box(VisualElement parent, string name = "")
        {
            var e = new VisualElement { name = name }; parent.Add(e); return e;
        }
        Label Text(VisualElement p, string words, int size, Color color, Font font = null)
        {
            var e = new Label(words) { pickingMode = PickingMode.Ignore };
            p.Add(e); e.style.fontSize = size; e.style.color = color; e.style.whiteSpace = WhiteSpace.Normal;
            e.style.unityFontDefinition = FontDefinition.FromFont(font ? font : bodyFont);
            e.style.marginTop = e.style.marginBottom = e.style.marginLeft = e.style.marginRight = 0;
            return e;
        }
        Label Label(VisualElement p, string words, int size, Color color)
        {
            var e = Text(p, words, size + 2, color, size >= 17 ? deckFont : bodyFont);
            e.style.marginBottom = 6; e.style.marginTop = size >= 17 ? 9 : 0;
            return e;
        }
        Label Caps(VisualElement p, string words, Color color, int size = 12)
        {
            var l = Text(p, words.ToUpperInvariant(), size, color, deckFont);
            l.style.letterSpacing = 1.7f;
            return l;
        }
        Button Button(VisualElement p, string words, Action act)
        {
            var b = new Button(act) { text = words };
            p.Add(b); Border(b, 1, C(69, 105, 87)); b.style.backgroundColor = C(32, 77, 64);
            b.style.color = Cream; b.style.fontSize = 17; b.style.unityFontDefinition = FontDefinition.FromFont(deckFont);
            b.style.whiteSpace = WhiteSpace.Normal; b.style.unityTextAlign = TextAnchor.MiddleLeft;
            b.style.minHeight = 37; b.style.marginTop = 4; b.style.marginBottom = 1; b.style.marginLeft = b.style.marginRight = 0;
            b.style.paddingLeft = b.style.paddingRight = 14;
            b.RegisterCallback<PointerEnterEvent>(_ => b.style.backgroundColor = C(47, 96, 80));
            b.RegisterCallback<PointerLeaveEvent>(_ => b.style.backgroundColor = C(32, 77, 64));
            return b;
        }

        void Build()
        {
            bodyFont = Resources.Load<Font>("Kit/fonts/Barlow-Medium");
            deckFont = Resources.Load<Font>("Kit/fonts/Jost-SemiBold");
            serifFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Liberation Serif", "DejaVu Serif" }, 62);
            faces = Resources.Load<Texture2D>("Kit/contacts/harbour-faces");
            skyline = Resources.Load<Texture2D>("Kit/contacts/harbour-mast");
            var ids = Resources.Load<TextAsset>("Kit/contacts/harbour-face-ids");
            if (ids) { var lines = ids.text.Split(new[] { '\r','\n' }, StringSplitOptions.RemoveEmptyEntries); for (int i=0; i<lines.Length; i++) faceCells[lines[i]] = i; }
            var template = Resources.Load<PanelSettings>("Kit/HudPanel");
            settings = template ? Instantiate(template) : ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1440, 900);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = .65f;
            settings.sortingOrder = 55;
            var go = new GameObject("District book"); go.SetActive(false); go.transform.SetParent(transform, false);
            document = go.AddComponent<UIDocument>(); document.panelSettings = settings; go.SetActive(true);
            root = document.rootVisualElement; root.pickingMode = PickingMode.Ignore; root.style.flexGrow = 1;
            sheet = Box(root, "contact-screen"); Full(sheet); sheet.style.backgroundColor = new Color(.025f,.075f,.067f,.77f);
            sheet.style.alignItems = Align.Center; sheet.style.justifyContent = Justify.Center;
            frame = Box(sheet, "contact-inventory"); frame.style.backgroundColor = Deep; Border(frame, 2, Trim); All(frame, 4);
            frame.style.maxWidth = 1220; frame.style.maxHeight = 790;
            mast = Box(frame, "contact-mast"); mast.style.position = Position.Relative; mast.style.overflow = Overflow.Hidden;
            bar = Box(frame, "contact-scopes"); bar.style.flexDirection = FlexDirection.Row; bar.style.alignItems = Align.Center;
            bar.style.backgroundColor = Deck; bar.style.minHeight = 48; bar.style.flexShrink = 0;
            bar.style.paddingLeft = 24; bar.style.paddingRight = 18; bar.style.borderBottomWidth = 1; bar.style.borderBottomColor = Trim;
            pages = new ScrollView(); pages.style.flexGrow = 1; pages.style.minHeight = 0; frame.Add(pages);
            foot = Box(frame, "contact-count"); foot.style.flexDirection = FlexDirection.Row; foot.style.alignItems = Align.Center;
            foot.style.backgroundColor = Deep; foot.style.minHeight = 35; foot.style.flexShrink = 0; foot.style.paddingLeft = 27;
            foot.style.borderTopWidth = 1; foot.style.borderTopColor = Trim;
            marker = Box(root, "contact-map-marker"); marker.pickingMode = PickingMode.Ignore;
            marker.style.position = Position.Absolute; marker.style.backgroundColor = Deep; All(marker, 5);
            markerText = Text(marker, "", 15, Cream, deckFont); marker.style.display = DisplayStyle.None;
            sheet.style.display = DisplayStyle.None;
        }

        void SizePage()
        {
            bool profile = person >= 0;
            frame.style.maxWidth = profile ? 700 : 1220;
            frame.style.maxHeight = profile ? writingOrder ? 690 : 575 : 790;
            frame.style.width = Length.Percent(88);
            frame.style.height = Length.Percent(profile ? 79 : 85);
        }

        public bool PortraitsReady => faces && faceCells.Count >= 126;
        void Portrait(VisualElement parent, string id, float size)
        {
            var photo = new UnityEngine.UIElements.Image { scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            parent.Add(photo); photo.style.width = photo.style.height = size;
            photo.style.flexShrink = 0; photo.style.backgroundColor = Deck;
            if (faces && faceCells.TryGetValue(id, out int k))
            {
                const int columns = 13; int rows = (faceCells.Count + columns - 1) / columns;
                photo.image = faces; photo.uv = new Rect((float)(k % columns)/columns, 1f-(float)(k/columns+1)/rows, 1f/columns, 1f/rows);
                photo.name = "portrait-" + id;
            }
        }

        void PreparePage()
        {
            pages.Clear(); bar.Clear(); mast.Clear(); foot.Clear(); searchBox = null;
            SizePage();
            pages.scrollOffset = Vector2.zero;
            bool profile = person >= 0;
            mast.style.height = profile ? 145 : 156; mast.style.flexShrink = 0;
            var art = Box(mast); Full(art); art.pickingMode = PickingMode.Ignore;
            if (skyline) { art.style.backgroundImage = new StyleBackground(skyline); art.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); }
            var shade = Box(mast); Full(shade); shade.pickingMode = PickingMode.Ignore;
            shade.generateVisualContent += HeaderShade;
            var inset = Box(mast); Full(inset); inset.pickingMode = PickingMode.Ignore;
            inset.style.top = inset.style.bottom = inset.style.left = inset.style.right = 6;
            Border(inset, 1, new Color(.83f, .83f, .69f, .20f));
            mast.style.borderBottomColor = Brass; mast.style.borderBottomWidth = 2;
            var close = Button(mast, "×", () => { if (!BackToIndex()) Close(); });
            close.name = "close-contacts"; close.tooltip = profile ? "Go back" : "Close contacts";
            close.style.position = Position.Absolute; close.style.right = 15; close.style.top = 11; close.style.fontSize = 24;
            close.style.minHeight = 30; close.style.width = 36; close.style.height = 34; All(close, 0); close.style.unityTextAlign = TextAnchor.MiddleCenter;
            pages.style.backgroundColor = profile ? Paper : Deep;
            var content = pages.contentContainer; content.style.paddingLeft = content.style.paddingRight = profile ? 32 : 21;
            content.style.paddingTop = profile ? 10 : 17; content.style.paddingBottom = 16;
            Nav("People", Section.Faces); Nav("Leads", Section.Leads); Nav("Requests", Section.Requests);
            var spacer = Box(bar); spacer.style.flexGrow = 1;
            var journal = Button(bar, "Journal", () => { HidePage(); host.BookShow("journal"); });
            journal.style.minHeight = 28; journal.style.fontSize = 14;
        }

        static void HeaderShade(MeshGenerationContext context)
        {
            var rect = context.visualElement.contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;
            var mesh = context.Allocate(6, 12);
            void Pair(float x, byte opacity)
            {
                var tint = new Color32(18, 46, 45, opacity);
                mesh.SetNextVertex(new Vertex { position = new Vector3(x, 0, Vertex.nearZ), tint = tint });
                mesh.SetNextVertex(new Vertex { position = new Vector3(x, rect.height, Vertex.nearZ), tint = tint });
            }
            Pair(0, 249); Pair(rect.width * .31f, 229); Pair(rect.width, 32);
            mesh.SetNextIndex(0); mesh.SetNextIndex(2); mesh.SetNextIndex(1);
            mesh.SetNextIndex(2); mesh.SetNextIndex(3); mesh.SetNextIndex(1);
            mesh.SetNextIndex(2); mesh.SetNextIndex(4); mesh.SetNextIndex(3);
            mesh.SetNextIndex(4); mesh.SetNextIndex(5); mesh.SetNextIndex(3);
        }

        void Nav(string word, Section next)
        {
            bool active = section == next;
            var b = new Button(() => { person = -1; writingOrder = false; section = next; search = ""; reply = ""; Page(); }) { text = word.ToUpperInvariant() };
            bar.Add(b); b.name = "contacts-nav-" + next.ToString().ToLowerInvariant(); All(b, 0); Border(b,0,Trim); b.style.marginRight = 24;
            b.style.height = 44; b.style.backgroundColor = Color.clear; b.style.color = active ? Brass : C(198,209,192);
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            b.style.unityFontDefinition = FontDefinition.FromFont(deckFont); b.style.fontSize = 14; b.style.letterSpacing = 1.2f;
            if (active) { b.style.borderBottomWidth = 3; b.style.borderBottomColor = Brass; }
        }

        void Heading(string title, string subtitle = "", string id = "")
        {
            var row = Box(mast); row.pickingMode = PickingMode.Ignore; row.style.position = Position.Absolute;
            row.style.left = 28; row.style.bottom = 14; row.style.right = 75; row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            if (id.Length > 0)
            {
                var mount = Box(row); All(mount,3); mount.style.backgroundColor = Cream; mount.style.marginRight = 20; Portrait(mount,id,94);
            }
            var words = Box(row); words.style.flexShrink = 1;
            Caps(words, subtitle.Length == 0 ? "Harbour district" : subtitle, Brass);
            var name = Text(words,title,id.Length == 0 ? 56 : 44,Cream,serifFont); name.style.letterSpacing = -1.5f;
        }

        void PersonHeader(string id, string name, string job, string place)
        {
            Heading(name, job.Replace('-', ' ') + "  /  " + place, bound.ContactKnown(person) ? id : "");
            Caps(foot, bound.ContactKnown(person) ? bound.District.Encountered[person] ? "Met in person" : "Known by introduction" : "District lead", Brass);
        }
    }
}
