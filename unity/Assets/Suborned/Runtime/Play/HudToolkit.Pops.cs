using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    /// <summary>Brief 101 round 3, piece 4 (Glyph's rule 1, briefs/101/glyph/r3: "Pops: the icon and its words on the street"; Sky: "yes"; moved here from HudToolkit.Less):
    /// a pop is its pictogram and at most four words as a street label (Jost Bold 15.5 in the book's ink on a cream keyline), with no plate. Its lead is the host's art:
    /// a kit pictogram keyed in ink; a kit badge on an ink disc (a refusal wears mark_no); an agent's face in the cream ring; a struck way ("#struck": the report that
    /// never lands, the car a fight has started round) the way's pictogram on an ink disc in a cream ring, struck through. The "who can" pop is faces (the selected
    /// struck, a pointer, who can); "Pick when to fire" is the three stance marks. Alt shows the long line. It retires the plate_dark pop.</summary>
    public sealed partial class HudToolkit
    {
        sealed class PopEl { public VisualElement Root, Lead, Badge, Struck, StruckIcon, Face, Faces, Stances; public KeyIcon Icon; public KeyText Word; }
        readonly List<PopEl> popEls = new();
        readonly List<Rect> popPlaced = new();
        /// <summary>Glyph r123 F2: a pop stands this far above a speech bubble.</summary>
        const float PopSpeechGap = 16f, PopH = 26f, PopType = 15.5f;

        PopEl NewPop()
        {
            var p = new PopEl(); var r = p.Root = StackRow(marks, PopH); r.style.position = Position.Absolute; r.style.justifyContent = Justify.FlexStart;
            var lead = p.Lead = E(r, null, h: PopH); lead.style.flexDirection = FlexDirection.Row; lead.style.alignItems = Align.Center; lead.style.flexShrink = 0; lead.style.overflow = Overflow.Visible;
            p.Icon = KIcon(lead, "", 16f, kw: 2f);
            p.Badge = E(lead, "e-ink", w: 19, h: 19); Round(p.Badge, 9.5f); p.Badge.style.flexShrink = 0; p.Badge.style.alignItems = Align.Center; p.Badge.style.justifyContent = Justify.Center; E(p.Badge, null, w: 16, h: 16).name = "art";
            p.Struck = E(lead, "e-ink e-inline", w: 24, h: 24); Round(p.Struck, 12f); BorderW(p.Struck, 1.8f); p.Struck.style.flexShrink = 0; p.Struck.style.alignItems = Align.Center; p.Struck.style.justifyContent = Justify.Center;
            p.StruckIcon = E(p.Struck, null, w: 14, h: 14); P(p.Struck, -1.8f, -1.8f, 24, 24, (pp, rc) => { var a = new Vector2(3f, 21f); var b = new Vector2(21f, 3f); pp.lineCap = LineCap.Butt; pp.strokeColor = BkInk; pp.lineWidth = 4.6f; pp.BeginPath(); pp.MoveTo(a); pp.LineTo(b); pp.Stroke(); pp.strokeColor = StPage; pp.lineWidth = 2.2f; pp.BeginPath(); pp.MoveTo(a); pp.LineTo(b); pp.Stroke(); });
            p.Face = SFace(lead, 18f, 1.6f);
            p.Word = KText(r, PopType); p.Word.Root.style.marginLeft = 6f;
            int pi = popEls.Count; popEls.Add(p);
            manifest.Add((r, "pop", () => popEls[pi].Word.Root.style.display == DisplayStyle.None ? "" : popEls[pi].Word.Words)); // Splice r123: a refusal pop is a `pop` (an icon and at most four words), with its words
            return p;
        }

        /// <summary>The pop's lead from the host's art: a pictogram (struck or keyed), a badge on an ink disc, or a face.</summary>
        void PopLead(PopEl p, string art)
        {
            bool struck = art.EndsWith("#struck"); var a = struck ? art[..^7] : art; if (a.Length == 0) a = "badges/mark_no";
            bool icon = a.StartsWith("icons/singles/"), face = a.StartsWith("portraits/"), badge = !icon && !face;
            Show(p.Icon.Root, icon && !struck); Show(p.Struck, icon && struck); Show(p.Badge, badge); Show(p.Face, face);
            string name = icon ? a["icons/singles/".Length..] : "";
            if (icon && !struck) { p.Icon.Set(this, name); p.Icon.Look("ki-ink", "ki-key"); }
            if (icon && struck) Icon(p.StruckIcon, name, Cream);
            if (badge) Bg(p.Badge.Q("art"), a, Color.white);
            if (face) Bg(p.Face.Q("face"), a, Color.white);
            p.Word.Root.style.marginLeft = badge ? 4f : 6f; // Glyph: a badge's words 4 px after it, a pictogram's 6
        }

        /// <summary>Brief 067 E4 (Glyph): a "who can" pop (PopUp.Faces): the selected agents' faces struck by a cream diagonal, a pointer, then the faces of who can (up to
        /// 3); no words (Alt shows the WhoCan line in their place). Brief 101 round 3: the faces in the built cream ring, the pointer keyed.</summary>
        void WhoCanFaces(PopEl p, HandoverHost.PopUp pp, bool alt)
        {
            bool on = pp.Faces != null && pp.Faces.Length > 0 && !alt;
            if (p.Faces == null) { if (!on) return; p.Faces = new VisualElement { pickingMode = PickingMode.Ignore }; p.Faces.style.flexDirection = FlexDirection.Row; p.Faces.style.alignItems = Align.Center; p.Faces.style.flexShrink = 0; p.Root.Insert(0, p.Faces); }
            Show(p.Faces, on); if (!on) return; Show(p.Lead, false); Show(p.Word.Root, alt);
            var key = string.Join(",", pp.Struck ?? System.Array.Empty<int>()) + "|" + string.Join(",", pp.Faces);
            if (p.Faces.userData as string == key) return; p.Faces.userData = key; p.Faces.Clear();
            void Face(int k, bool struck, float left)
            {
                var c = SFace(p.Faces, 18f, 1.6f); c.style.marginLeft = left; CameoOf(c, k);
                if (struck) { var sk = E(c, "e-page", -2f, 8f, 22f, 2f); sk.style.rotate = new Rotate(-45f); }
            }
            bool first = true; foreach (var k in pp.Struck ?? System.Array.Empty<int>()) { Face(k, true, first ? 0f : 3f); first = false; }
            var pt = KIcon(p.Faces, "pointer", 13f, kw: 2f); pt.Root.style.marginLeft = 6f; pt.Root.style.marginRight = 6f;
            first = true; foreach (var k in pp.Faces) { Face(k, false, first ? 0f : 3f); first = false; }
        }

        /// <summary>Glyph r123 §4: a stance-unknown pop shows the roster's three stance marks (badges/stance_hold, stance_return, stance_free, 16 px), no words; Alt keeps the line.</summary>
        void StanceIcons(PopEl p, HandoverHost.PopUp pp, bool alt)
        {
            bool on = pp.Code == "stance-unknown" && !alt;
            if (p.Stances == null) { if (!on) return; p.Stances = E(p.Root); p.Stances.style.flexDirection = FlexDirection.Row; p.Stances.style.alignItems = Align.Center; p.Stances.style.flexShrink = 0;
                foreach (var st in new[] { "hold", "return", "free" }) { var b = E(p.Stances, null, w: 16, h: 16); b.style.flexShrink = 0; b.style.marginLeft = st == "hold" ? 0 : 5; Bg(b, $"badges/stance_{st}", Color.white); } }
            Show(p.Stances, on); if (!on) return; Show(p.Lead, false); Show(p.Word.Root, false);
        }

        void Pops(Camera cam)
        {
            float now = Time.unscaledTime; bool alt = host.Peeking; var list = new List<HandoverHost.PopUp>();
            for (int i = m.Pops.Count - 1; i >= 0 && list.Count < 3; i--) if (now <= m.Pops[i].Until) list.Add(m.Pops[i]);
            while (popEls.Count < 3) NewPop();
            float stack = 0; List<Rect> speech = null; popPlaced.Clear();
            for (int i = 0; i < popEls.Count; i++)
            {
                var p = popEls[i]; var r = p.Root; bool has = i < list.Count; Show(r, has); if (!has) continue;
                var pp = list[i]; Vector2 at;
                if (pp.World.HasValue && Proj(pp.World.Value + Vector3.up * 2.9f, out var wp)) at = wp;
                else if (pp.Screen.HasValue) { at = RuntimePanelUtils.ScreenToPanel(root.panel, pp.Screen.Value) + new Vector2(18, -26 - stack); stack += 30; }
                else { Show(r, false); continue; }
                Show(p.Lead, true); PopLead(p, pp.Icon);
                string words = alt && pp.Long.Length > 0 ? pp.Long : pp.Text; p.Word.Set(words); Show(p.Word.Root, words.Length > 0); KeyWrap(p.Word, alt ? 320f : 0f);
                WhoCanFaces(p, pp, alt); // brief 067 E4 (Glyph): the "who can" pop is faces, not words (Alt keeps the line)
                StanceIcons(p, pp, alt); // Glyph r123 §4: "Pick when to fire" is the roster's three stance marks, no words (Alt keeps the line)
                float w = r.resolvedStyle.width; if (float.IsNaN(w)) w = 120;
                float ph = r.resolvedStyle.height; if (float.IsNaN(ph)) ph = PopH;
                float py = pp.World.HasValue ? ClearOfSpeech(speech ??= SpeechRects(), at.x - w / 2, w, at.y + ph, ph, PopSpeechGap) - ph : at.y; // brief 044: a pop on a thing sits above its speech, never under the leader
                float px = pp.Screen.HasValue && !pp.World.HasValue ? at.x : at.x - w / 2;
                for (int guard = 0; guard < 4; guard++) { bool hit = false; foreach (var q in popPlaced) if (q.Overlaps(new Rect(px - 3, py - 3, w + 6, ph + 6))) { py = q.yMin - ph - 6f; hit = true; } if (!hit) break; } // Aster r123 pre-flight 3: two pops never share a spot (the newer steps up)
                popPlaced.Add(new Rect(px, py, w, ph));
                if (host.Ring != null && ringRoot != null && ringRoot.resolvedStyle.display != DisplayStyle.None)
                {   // Glyph's 0.2.104 review #7: a pop never sits under the open ring's rim: it drops below the ring's circle
                    var rc = new Vector2(ringRoot.style.left.value.value, ringRoot.style.top.value.value); float rr = HandoverHost.RingOuter + 8f;
                    var near = new Vector2(Mathf.Clamp(rc.x, px, px + w), Mathf.Clamp(rc.y, py, py + ph));
                    if ((near - rc).sqrMagnitude < rr * rr) py = rc.y + rr + 10f;
                }
                r.style.left = px; r.style.top = py; r.style.opacity = Mathf.Clamp01((pp.Until - now) / 0.3f);
                if (r.parent.IndexOf(r) < r.parent.childCount - popEls.Count) r.BringToFront(); // above every world mark (the pool grows after the pops were made)
            }
        }

        /// <summary>A street label that may wrap (Alt's long line): the face and its copies held to `maxW` px and wrapped alike; 0 keeps it on one line.</summary>
        static void KeyWrap(KeyText t, float maxW)
        {
            var ws = maxW > 0f ? WhiteSpace.Normal : WhiteSpace.NoWrap; if (t.Face.style.whiteSpace == ws) return;
            t.Face.style.whiteSpace = ws; t.Face.style.maxWidth = maxW > 0f ? new StyleLength(maxW) : new StyleLength(StyleKeyword.None); foreach (var c in t.Copies) c.style.whiteSpace = ws;
        }
    }
}
