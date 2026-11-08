using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    /// <summary>Brief 101 round 3, the sweep (Glyph's rule 4, briefs/101/glyph/r3: "a pip over a person"; Sky: "yes"; moved here from HudToolkit.Less): the pips over the people the
    /// cell knows of wear their role's badge in the built cream ring over a soft shadow (the badge's chrome rim clipped under the ring), on a fine cream stem to the head;
    /// the turned contact's ✓ on the page's cream at its lower right; the hovered person's name a street label (Jost SemiBold 14), no plate.</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>The people the cell knows of (told in the briefing, or learned) carry a pip with their role glyph over the head, the same glyph
        /// as their blip on the scope; hovering it names them. The pips never sit on each other: a later one moves up.</summary>
        readonly List<KeyText> pipNames = new();

        void Pips(Camera cam)
        {
            while (pipEls.Count < Cast.Length)
            {
                // brief 101 round 3 (Glyph's rule 4): the role's badge in the built cream ring over a soft shadow (the badge's own chrome rim clipped under it); its name a street label
                var r = E(marks, null, 0, 0, 26, 26, pick: true); r.style.overflow = Overflow.Visible; var sh = Disc(r, 26f, "e-shadow"); Pin(sh, new Vector2(0f, 2f)); sh.style.opacity = 0.6f;
                var clip = E(r, "e-page", 0, 0, 26, 26); Round(clip, 13f); clip.style.overflow = Overflow.Hidden; var bd = E(clip, null, -4, -4, 34, 34);
                var ring = E(r, "e-inline", 0, 0, 26, 26); Round(ring, 13f); BorderW(ring, 2f);
                var nmK = KText(marks, 14f, "k-semi"); var nm = nmK.Root; nm.style.position = Position.Absolute; pipNames.Add(nmK);
                int k = pipEls.Count; r.RegisterCallback<PointerEnterEvent>(_ => pipHover = Cast[k]); r.RegisterCallback<PointerLeaveEvent>(_ => { if (pipHover == Cast[k]) pipHover = null; });
                r.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { FindPerson(Cast[k]); e.StopPropagation(); } else if (e.button == 1) { host.RightOnPerson(Cast[k]); e.StopPropagation(); } }); // Sky, route B: a right click on the pin is a right click on the person
                var tk = E(r, "e-page", 14.5f, 14.5f, 14, 14); tk.name = "turned"; Round(tk, 7f); E(tk, null, 1, 1, 12, 12).name = "art"; Show(tk, false); // the turned contact's ✓ on the pip's lower right, on the page's cream
                pipEls.Add((r, bd, nm)); manifest.Add((r, "image", () => "pip " + Cast[k]));
            }
            var placed = new List<Rect>(); bool alt = host.Peeking;
            for (int i = 0; i < Cast.Length; i++)
            {
                var (r, bd, nm) = pipEls[i]; var role = Cast[i];
                // Glyph 022: a server at a table stands under its parasol: her pip sits on her head (2.05 m), not up at the canopy's height
                const float lift = 2.3f; // Glyph 022: the served table's parasol is hidden, so the pip stands at its usual height; the stem says whose it is
                bool on = !m.Ended && !m.BriefingOpen && KnownOf(role) && host.Views?.Find(role) is Transform t && t.gameObject.activeSelf && Proj(t.position + Vector3.up * lift, out var q) && InPlay(q); // Glyph N7: no pip behind the roster's band
                if (!on) { Show(r, false); Show(nm, false); continue; }
                Proj(host.Views.Find(role).position + Vector3.up * lift, out var at);
                var rect = new Rect(at.x - 13, at.y - 26, 26, 26);
                // a later pip moves up off an earlier one; Glyph round 2: a pip never covers another mark (the guard's shield over the door's lock):
                // it moves aside, 2 px clear; Glyph 022 (the shield over the cup's corner): both rules again until neither moves it
                for (int pass = 0; pass < 4; pass++)
                {
                    bool moved = false;
                    for (int guard = 0; guard < 6; guard++) { bool hit = false; foreach (var pr in placed) if (pr.Overlaps(rect)) { rect.y = pr.y - 28; hit = true; moved = true; } if (!hit) break; }
                    for (int guard = 0; guard < 4; guard++) { bool hit = false; foreach (var mr in OtherMarks()) if (mr.Overlaps(rect)) { rect.x = mr.x - rect.width - 2 >= at.x - 40 ? mr.x - rect.width - 2 : mr.xMax + 2; hit = true; moved = true; break; } if (!hit) break; }
                    bool clear = true; foreach (var pr in placed) if (pr.Overlaps(rect)) clear = false;
                    if (clear || !moved) break;
                }
                // under the cursor's price chip: the chip is about this person, so the pip steps out from under it (above the chip)
                if (chip2.resolvedStyle.display != DisplayStyle.None && chip2.layout.width > 0) { var cr = new Rect(chip2.style.left.value.value, chip2.style.top.value.value, chip2.layout.width, chip2.layout.height); if (cr.Overlaps(rect)) rect.y = cr.y - rect.height - 2; }
                placed.Add(rect);
                if (Proj(host.Views.Find(role).position + Vector3.up * 1.9f, out var head)) { var top = new Vector2(rect.center.x, rect.yMax - 2); var hd = head; paints.Add(pp => { pp.lineCap = LineCap.Round; pp.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.55f); pp.lineWidth = 3.4f; pp.BeginPath(); pp.MoveTo(top); pp.LineTo(hd); pp.Stroke(); pp.strokeColor = new Color(0.97f, 0.96f, 0.92f, 0.95f); pp.lineWidth = 1.8f; pp.BeginPath(); pp.MoveTo(top); pp.LineTo(hd); pp.Stroke(); pp.fillColor = pp.strokeColor; pp.BeginPath(); pp.Arc(hd, 2.2f, new Angle(0), new Angle(360, AngleUnit.Degree)); pp.Fill(); }); } // a stem to the top of the head, ending on a dot (Glyph 022: the cup sat over a customer behind her)
                Show(r, true); r.style.left = rect.x; r.style.top = rect.y; Bg(bd, CastBadge(role)); MoodFrame(r, role); // engine 0.28: the mood disc beside the pip (two icons at most)
                { var tk = r.Q("turned"); bool turned = role is "waitress" or "driver" && host.Game.PersonByRole(role) is { } tp && tp.Turned; Show(tk, turned); if (turned) Bg(tk.Q("art"), "badges/mark_ok", Color.white); }
                float pulse = pulseAt.TryGetValue(role, out var pa) ? Time.unscaledTime - pa : 99f;
                float s = pulse < 2f ? 1f + 0.35f * Mathf.Abs(Mathf.Sin(pulse * Mathf.PI * 2f)) : 1f; r.style.scale = new Scale(new Vector2(s, s));
                bool named = pipHover == role || alt || (host.Hover?.Kind == "person" && host.Hover.Key == role); Show(nm, named); // the hovered person is named by their pip's tag (no second chip)
                if (named)
                {
                    { string nmw = Cap1(host.Text?.TargetName(role, role) ?? role).Replace("the ", ""); string md = ""; if (alt && role != "councillor") try { md = host.Game.MoodOf(role); } catch (System.Exception) { md = ""; } pipNames[i].Set(alt && host.Text != null && host.Text.AltMood.TryGetValue(md, out var amw) ? $"{nmw} · {amw}" : nmw); } // Fable (less.alt.mood): on Alt the mood's words follow the name
                    float tw = nm.resolvedStyle.width; if (float.IsNaN(tw) || tw < 1) tw = 60;
                    bool Free(Rect t) { foreach (var mr in OtherMarks()) if (mr.Overlaps(t)) return false; foreach (var pr in placed) if (pr != rect && pr.Overlaps(t)) return false; return true; }
                    var right = new Rect(rect.x + 28, rect.y + 3, tw, 20); var left = new Rect(rect.x - 2 - tw, rect.y + 3, tw, 20); var above = new Rect(rect.center.x - tw / 2, rect.y - 22, tw, 20);
                    var tag = Free(above) ? above : Free(right) ? right : left; // Glyph 016 r2 A: above the pips first; an eye pip covered the guard's tag ("ard")
                    // Glyph 0.2.65 review 5: a bubble covered "Inez" (its leader landed on the tag): while speech sits on the tag, the tag goes (the
                    // bubble's leader says who speaks)
                    bool spoken = false; var tg = new Rect(tag.x - 6, tag.y - 6, tag.width + 12, tag.height + 12); foreach (var br in bubbleRects) if (br.Overlaps(tg)) spoken = true;
                    foreach (var (_, bx, bh) in bubbleLeads) if (Rect.MinMaxRect(Mathf.Min(bh.x, bx.center.x) - 6, bx.yMax, Mathf.Max(bh.x, bx.center.x) + 6, bh.y).Overlaps(tg)) spoken = true;
                    if (spoken) Show(nm, false); else { nm.style.left = tag.x; nm.style.top = tag.y; placed.Add(tag); }
                }
            }
        }
    }
}
