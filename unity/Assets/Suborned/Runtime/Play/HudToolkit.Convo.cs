using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 032, 085, look C (GDD v0.36.0 section 10; Glyph's mock): the conversation scene's HUD steps away: the whole frame is the shot. On screen: the line low on the left over a soft shadow, the speaker's
    /// live face in a round inset beside it, the name small above it, a bare Skip, and the dip. A line is a row of word labels, so a leaned-on word takes Barlow SemiBold and a reveal's rest is held open and
    /// invisible: nothing jumps. Split from HudToolkit.Live.</summary>
    public sealed partial class HudToolkit
    {
        const float LineType = 36f, LineNameType = 21f, LineFace = 136f, LineWordGap = 9f, ShadeMax = 0.74f, FaceZoom = 0.8f; const int ShadeBands = 40;
        VisualElement convoLine, convoFace, convoCol, convoWords, convoSkip, convoVeil, convoShade; Label convoName; bool convoHid; string convoLineKey = "", convoSaid = "";
        readonly List<Label> convoWordEls = new(); static readonly string[] convoFaces = new string[4]; Font lineFont, leanFont;
        /// <summary>Elements that are not HUD for the budget (the soft shadow, the dip): the HUD layer of a shot is drawn without them.</summary>
        readonly List<VisualElement> notHud = new();

        /// <summary>The proof reads it: the name and the words showing now ("" in a hold; part of the line in a reveal), and the live face's picture.</summary>
        public static string ConvoSaid => live?.convoSaid ?? "";
        public static RenderTexture ConvoFacePicture { get; private set; }
        /// <summary>The proof reads it: the word in the lean font now ("": none).</summary>
        public static string ConvoLeanNow { get; private set; } = "";

        void ConvoBuild()
        {
            lineFont = Resources.Load<Font>("Kit/fonts/Barlow-Medium"); leanFont = Resources.Load<Font>("Kit/fonts/Barlow-SemiBold");
            // the soft shadow behind the line, not a box: the kit's ink-deep, easing in from a little over a third of the way up the frame, in thin bands of itself
            convoShade = E(root); convoShade.style.position = Position.Absolute; convoShade.style.left = 0; convoShade.style.right = 0; convoShade.style.top = Length.Percent(62); convoShade.style.bottom = 0; convoShade.pickingMode = PickingMode.Ignore; notHud.Add(convoShade);
            for (int i = 0; i < ShadeBands; i++) { var band = E(convoShade); band.style.flexGrow = 1; float k = (i + 0.5f) / ShadeBands; band.style.backgroundColor = new Color(InkDeep.r, InkDeep.g, InkDeep.b, ShadeMax * Mathf.SmoothStep(0f, 1f, k)); }
            convoVeil = E(root); convoVeil.style.position = Position.Absolute; convoVeil.style.left = 0; convoVeil.style.right = 0; convoVeil.style.top = 0; convoVeil.style.bottom = 0; convoVeil.style.backgroundColor = Color.black; convoVeil.pickingMode = PickingMode.Ignore; notHud.Add(convoVeil);
            convoLine = E(root); convoLine.style.position = Position.Absolute; convoLine.style.flexDirection = FlexDirection.Row; convoLine.style.alignItems = Align.FlexEnd; convoLine.pickingMode = PickingMode.Ignore;
            convoFace = E(convoLine, null, w: LineFace, h: LineFace); convoFace.style.flexShrink = 0; Round(convoFace, LineFace / 2f); Border(convoFace, 3f, Cream); convoFace.style.marginRight = 24; convoFace.style.backgroundColor = Cream; convoFace.style.overflow = Overflow.Hidden;
            manifest.Add((convoFace, "image", () => "speaker's face"));
            convoCol = E(convoLine); convoCol.style.flexShrink = 1; convoCol.style.flexGrow = 1; convoCol.style.paddingBottom = 16;
            convoName = L(convoCol, "verb-name"); convoName.style.color = Cream; convoName.style.fontSize = LineNameType; convoName.style.marginBottom = 4; convoName.style.textShadow = LineShadow();
            convoWords = E(convoCol); convoWords.style.flexDirection = FlexDirection.Row; convoWords.style.flexWrap = Wrap.Wrap; convoWords.pickingMode = PickingMode.Ignore;
            manifest.Add((convoCol, "speech", () => host.ConvoNow is HandoverHost.Convo cc && cc.Beat < cc.Lines.Count ? $"{convoName.text} {cc.Lines[cc.Beat].text}" : ""));
            convoSkip = E(root, null, pick: true); convoSkip.style.position = Position.Absolute; convoSkip.style.flexDirection = FlexDirection.Row; convoSkip.style.alignItems = Align.Center; convoSkip.style.paddingLeft = 12; convoSkip.style.paddingRight = 6; convoSkip.style.paddingTop = 6; convoSkip.style.paddingBottom = 6;
            var sk = L(convoSkip, "keycap"); sk.text = "Skip"; sk.style.fontSize = 21; sk.style.color = Cream; sk.style.textShadow = LineShadow(); var chev = E(convoSkip, null, w: 14, h: 14); chev.style.marginLeft = 6; Icon(chev, "chev_down", Cream); chev.style.rotate = new Rotate(new Angle(-90, AngleUnit.Degree));
            convoSkip.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ConvoSkip(); e.StopPropagation(); } });
            manifest.Add((convoSkip, "image", () => "skip"));
        }
        static TextShadow LineShadow() => new() { offset = new Vector2(0f, 2f), blurRadius = 6f, color = new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.9f) };

        bool ConvoHud()
        {
            var c = host.ConvoNow; bool on = c != null;
            if (!on && !convoHid) return false;
            if (convoLine == null) ConvoBuild();
            var mine = new HashSet<VisualElement> { convoShade, convoVeil, convoLine, convoSkip };
            if (on != convoHid)
            {
                convoHid = on;
                foreach (var ch in root.Children()) if (!mine.Contains(ch)) { ch.style.visibility = on ? Visibility.Hidden : StyleKeyword.Null; ch.style.opacity = on ? 0f : StyleKeyword.Null; } // opacity too: a descendant set visible (the pips, a chip's bar) shows through a hidden parent
                foreach (var e in mine) Show(e, on);
                if (!on) { convoSaid = ""; ConvoFacePicture = null; Show(pauseIcon, host.Paused); pauseIcon.style.opacity = 1f; return false; }
            }
            var size = root.panel.visualTree.layout.size; float W = float.IsNaN(size.x) ? 1920 : size.x, H = float.IsNaN(size.y) ? 1080 : size.y; float now = Time.unscaledTime;
            convoVeil.style.opacity = host.ConvoDip; Show(convoVeil, host.ConvoDip > 0.001f);
            bool shot = c.OutAt < 0f || host.ConvoDip > 0f && now - c.OutAt < HandoverHost.ConvoDipS * 0.5f;
            bool speak = shot && c.AfterUntil < 0f && c.Beat < c.Lines.Count && now >= c.BeatAt; Show(convoLine, speak); Show(convoShade, speak); Show(convoSkip, shot);
            convoSkip.style.right = 34; convoSkip.style.bottom = Mathf.Max(22f, H * 0.03f);
            string view = null; convoSaid = "";
            if (speak)
            {
                var (who, line) = c.Lines[c.Beat]; var f = c.Beat < c.Fields.Count ? c.Fields[c.Beat] : null;
                view = host.ConvoFaceView(who); Text(convoName, host.ConvoSpeakerName(c, who));
                convoLine.style.left = Mathf.Max(36f, W * 0.035f); convoLine.style.bottom = Mathf.Max(30f, H * 0.05f); convoLine.style.width = Mathf.Min(W * 0.66f, 1180f);
                ConvoWordsFill(c.Id + c.Beat + line, line, f?.Lean ?? "");
                // a reveal: the start, then the whole line revealS later; the rest stands in its place, unseen
                int total = 0; foreach (var w in line.Split(' ')) if (w.Length > 0) total++;
                int showing = total; if (f != null && f.RevealAfter.Length > 0 && now - c.BeatAt < f.RevealS) { showing = 0; foreach (var w in f.RevealAfter.Split(' ')) if (w.Length > 0) showing++; }
                for (int i = 0; i < convoWordEls.Count; i++) { var el = convoWordEls[i]; if (i < total) el.style.opacity = i < showing ? 1f : 0f; }
                var said = new List<string>(); int n = 0; foreach (var w in line.Split(' ')) if (w.Length > 0 && n++ < showing) said.Add(w); convoSaid = $"{convoName.text}: {string.Join(" ", said)}";
                convoLine.style.opacity = Mathf.Clamp01((now - c.BeatAt) / 0.12f);
            }
            // the live face: the insets' own close camera, one slot, on whoever speaks (no other callout is drawn while the HUD stands away)
            if (host.Views != null)
            {
                for (int i = 0; i < convoFaces.Length; i++) convoFaces[i] = i == 0 ? view ?? "" : ""; host.Views.InsetViews(convoFaces);
                var rt = host.Views.InsetFrame(0, view, FaceZoom); for (int i = 1; i < convoFaces.Length; i++) host.Views.InsetFrame(i, null);
                ConvoFacePicture = rt; Show(convoFace, speak && rt != null);
                if (rt != null && convoFace.style.backgroundImage.value.renderTexture != rt) { convoFace.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt)); }
            }
            else Show(convoFace, false);
            convoShade.SendToBack(); convoVeil.BringToFront();
            return true;
        }

        void ConvoWordsFill(string key, string line, string lean)
        {
            if (key == convoLineKey) return; convoLineKey = key; var ws = line.Split(' '); int used = 0; bool leaned = false; ConvoLeanNow = "";
            foreach (var w in ws)
            {
                if (w.Length == 0) continue;
                if (used >= convoWordEls.Count)
                {
                    var l = new Label(); l.pickingMode = PickingMode.Ignore; l.style.fontSize = LineType; l.style.color = Cream; l.style.marginLeft = 0; l.style.marginTop = 0; l.style.marginBottom = 0; l.style.paddingLeft = 0; l.style.paddingRight = 0; l.style.paddingTop = 0; l.style.paddingBottom = 0; l.style.marginRight = LineWordGap; l.style.unityFontStyleAndWeight = FontStyle.Normal; l.style.whiteSpace = WhiteSpace.NoWrap; l.style.textShadow = LineShadow();
                    convoWords.Add(l); convoWordEls.Add(l);
                }
                var el = convoWordEls[used++]; bool isLean = !leaned && lean.Length > 0 && w.Trim('.', ',', '?', '!', ';', ':', '’', '\'', '"') == lean; leaned |= isLean; if (isLean) ConvoLeanNow = w;
                var font = isLean ? leanFont : lineFont; if (font) el.style.unityFontDefinition = FontDefinition.FromFont(font); el.text = w; Show(el, true);
            }
            for (int i = used; i < convoWordEls.Count; i++) Show(convoWordEls[i], false);
        }
    }
}
