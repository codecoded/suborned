using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101 round 3, piece 6 (Glyph's act ring, briefs/101/glyph/r3/ring.png; Sky: "yes"): each wedge's way and price are street labels beside it, no plate:
    /// the way's word (Jost Bold 15.5 in the book's ink on a cream keyline; the lit way's in the book's deep teal; a shut way's in the soft ink with the kit's mark_no
    /// before its reason), the thing it moves as a face in the cream ring, then the price as keyed pictograms and figures (Jost Bold 15): money, Trail, Alert, the
    /// eyes (soft ink; open, the cones' red), the outcome's kit badge on a cream disc. A holstered Fire wears the draw mark, a held Go its lock, "held by" and the
    /// holders' badges, a violent way the loud mark. The wheel's rim is cream (HudToolkit.Ring). It retires the plate_dark price plates and the chrome rim.</summary>
    public sealed partial class HudToolkit
    {
        const float WayType = 15.5f, PriceType = 15f, PriceIcon = 16f;

        /// <summary>A wedge's label row: the word, the draw mark, the thing's face, the held marks, the price, the loud mark (Pill is the row, the manifest's lock).</summary>
        void NewWedgeLabel(Wedge w)
        {
            w.Pill = StackRow(ringRoot, 26f); w.Pill.style.position = Position.Absolute; w.Pill.style.justifyContent = Justify.FlexStart;
            w.Draw = RoundTab(w.Pill, 18f, "fire", false, 0.6f); w.Draw.style.marginRight = 6;
            w.WordK = KText(w.Pill, WayType); w.Word = w.WordK.Face;
            w.ShutMark = Badge16(w.Pill, "badges/mark_no"); w.ShutMark.style.marginLeft = 8; Show(w.ShutMark, false);
            w.Obj = SFace(w.Pill, 18f, 1.6f); w.Obj.style.marginLeft = 8; // brief 057 §2: the thing the way moves (a pictogram goes in its "face")
            w.Held = E(w.Pill); w.Held.style.flexDirection = FlexDirection.Row; w.Held.style.alignItems = Align.Center; w.Held.style.marginLeft = 8; // brief 057 §4.2: a shut Go's lock and holders
            KIcon(w.Held, "lock", PriceIcon, kw: 1.8f); w.HeldByK = KText(w.Held, 13.5f, "k-semi", "k-soft"); w.HeldByK.Root.style.marginLeft = 4; w.HeldByK.Root.style.marginRight = 4; // brief 096: "held by" before the badges
            w.Fields = E(w.Pill); w.Fields.style.flexDirection = FlexDirection.Row; w.Fields.style.alignItems = Align.Center;
            w.LoudK = KIcon(w.Pill, "bang", PriceIcon, kw: 1.8f); w.LoudK.Root.style.marginLeft = 8;
        }

        /// <summary>A kit badge 16 px on the page's cream disc (18.4 px): the price's outcome mark, a shut way's mark_no.</summary>
        VisualElement Badge16(VisualElement parent, string art)
        {
            var d = E(parent, "e-page", w: 18.4f, h: 18.4f); Round(d, 9.2f); d.style.flexShrink = 0; d.style.alignItems = Align.Center; d.style.justifyContent = Justify.Center;
            var b = E(d, null, w: PriceIcon, h: PriceIcon); b.name = "art"; Bg(b, art, Color.white); return d;
        }

        /// <summary>A wedge's label now: its word and state, the thing's face, the held marks, the price and the loud mark.</summary>
        void WedgeLabel(Wedge w, HandoverHost.RingSeg s, bool lit, bool held, List<HudModel.PField> fs)
        {
            w.WordK.Set(s.Word); bool shut = s.Shut && !held;
            w.WordK.Look(shut ? "k-soft" : lit ? "k-deep" : "k-ink", "k-key");
            Show(w.ShutMark, shut); Show(w.Draw, s.Draws);
            Show(w.Obj, s.Obj.Length > 0);
            if (s.Obj.Length > 0) { var fc = w.Obj.Q("face"); if (s.Obj.StartsWith("icon:")) { fc.style.backgroundImage = StyleKeyword.None; fc.style.backgroundColor = BkBody; if (fc.childCount == 0) E(fc, null, 3.5f, 3.5f, 11, 11); Show(fc[0], true); Icon(fc[0], s.Obj[5..], Cream); } else { fc.style.backgroundColor = BkPale; if (fc.childCount > 0) Show(fc[0], false); Bg(fc, s.Obj, Color.white); } }
            { var sk = w.Obj.Q("strike"); if (sk == null && s.ObjStruck) { sk = E(w.Obj, "e-page", -2f, 8f, 22f, 2f); sk.name = "strike"; sk.style.rotate = new Rotate(-45f); } if (sk != null) Show(sk, s.ObjStruck && s.Obj.Length > 0); } // brief 067 C5b (Glyph): Go before Vale: Vale's face struck
            Show(w.Held, s.Shut); Show(w.Fields, !s.Shut || s.ShutText.Length > 0);
            w.ShutLine = s.Shut ? s.Word + (s.ShutText.Length > 0 ? " · " + s.ShutText : "") : "";
            if (s.Shut)
            {   // the lock, then one badge a holder (brief 057 §4.2): the same holders the world marks
                if (w.HeldKey != s.Holders) { w.HeldKey = s.Holders; w.HeldIds.Clear(); foreach (var hx in s.Holders.Split(",")) if (hx.Trim().Length > 0) w.HeldIds.Add(hx.Trim()); } var ids = w.HeldIds; /* Redline 064 #6: split once a ring, not a frame */
                while (w.Held.childCount < 2 + Mathf.Max(1, ids.Count)) { var c0 = E(w.Held, "e-ink e-inline", w: 18, h: 18); Round(c0, 9f); BorderW(c0, 1.6f); c0.style.flexShrink = 0; c0.style.marginRight = 2; E(c0, null, 0.4f, 0.4f, 14, 14).name = "face"; }
                Show(w.HeldByK.Root, held); if (held) w.HeldByK.Set(BookWordsFile.Get().HeldBy);
                for (int k = 2; k < w.Held.childCount; k++) { bool has = k - 2 < ids.Count; Show(w.Held[k], has); if (!has) continue; bool rv = PlayText.RoleOfId(ids[k - 2]) == "rival" || ids[k - 2] == "car:rivals"; Bg(w.Held[k].Q("face"), "badges/" + (rv ? "signal_rival" : "signal_police"), Color.white); RingColour(w.Held[k], rv && held ? RivalViolet : (Color?)null); } // brief 096: a rival holder's badge in the rivals' violet ring
            }
            StreetFields(w.Fields, fs, ref w.FieldsKey);
            Show(w.LoudK.Root, s.Violent);
        }

        /// <summary>The price as street labels (Glyph's ring): each mark its keyed pictogram (16 px) and its figure (Jost Bold 15), the eyes in the soft ink (open: the
        /// cones' red), an outcome badge on the page's cream disc, the left-behind faces in the cream ring. Rebuilt only when the fields change.</summary>
        void StreetFields(VisualElement into, List<HudModel.PField> fs, ref string key)
        {
            var sb = new System.Text.StringBuilder(); foreach (var f in fs) sb.Append(f.Kind).Append(f.Icon).Append(f.Text).Append(f.Eye).Append(f.Mark).Append(f.Found).Append(string.Join(",", f.Icons)).Append('|');
            var k = "#" + sb; if (k == key) return; key = k; into.Clear();
            bool first = true;
            foreach (var f in fs)
            {
                var part = E(into); part.style.flexDirection = FlexDirection.Row; part.style.alignItems = Align.Center; part.style.flexShrink = 0; part.style.marginLeft = first ? 8f : 9f; first = false;
                KeyIcon Ki(string icon, string fill) { var ki = KIcon(part, icon, PriceIcon, fill, "ki-key", 1.8f); return ki; }
                if (f.Found) Ki("found", "ki-ink").Root.style.marginRight = 3;
                if (f.Kind == "eye") // Glyph, "Eyes, revised": the bare glyph, its line on hover; the soft ink (open: the cones' red)
                {
                    var ki = Ki(f.Icon, "ki-ink"); ki.Face.style.unityBackgroundImageTintColor = f.Eye == "open" ? Red : BkSoft;
                    var gl = ki.Root; gl.pickingMode = PickingMode.Position; var tip = f.Tip; gl.RegisterCallback<PointerEnterEvent>(_ => EyeTipShow(gl, tip)); gl.RegisterCallback<PointerLeaveEvent>(_ => EyeTipShow(null, null));
                    if (f.Text.Length > 0) { var en = KText(part, PriceType); en.Root.style.marginLeft = 3; en.Set(f.Text); }
                    continue;
                }
                if (f.Kind == "mark") { Badge16(part, $"badges/mark_{f.Mark}"); continue; }
                if (f.Kind == "left") // brief 018 (Glyph r1): a face in the cream ring, then its fate (cuffs captured, footprints walks out)
                {
                    foreach (var li in f.Icons)
                    {
                        var who = li.Split(':')[0]; bool cap = li.EndsWith(":captured");
                        var cm = SFace(part, 18f, 1.6f); Bg(cm.Q("face"), $"portraits/cameo_{who}_normal", Color.white); if (cap) RingColour(cm.Q<VisualElement>(className: "e-inline"), Red);
                        var fg = cap ? Cuffs(part, Red) : Ki("s_moving", "ki-ink").Root; fg.style.marginLeft = 3; fg.style.marginRight = 8; fg.style.flexShrink = 0;
                    }
                    continue;
                }
                if (f.Kind == "learns") { foreach (var li in f.Icons) Ki(li, "ki-ink").Root.style.marginRight = 3; continue; } // Glyph r5 #3: keyed like the other price marks (a round tab means "click me"; the cream disc is the outcome badge's)
                if (f.Icon.Length > 0) Ki(f.Icon, "ki-ink").Root.style.marginRight = 3;
                if (f.Text.Length > 0) KText(part, PriceType).Set(f.Text);
            }
        }
    }
}
