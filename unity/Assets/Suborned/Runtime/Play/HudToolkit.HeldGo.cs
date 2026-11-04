using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 096 objective 7 (Glyph's 4b, 4c; Sky's calls "all. recommended"): Go on the car's ring while rivals hold the car is shut by shape, never a colour of its
    /// own: its wedge striped with the lock on it and one violet holder dot, its pill "Go · lock · held by" and a badge in the rivals' violet ring per holder, and the fix
    /// on a plate under the pill ("Step out and draw · or down them", book.heldGo). Police holders keep the plain shut Go (no fix line: there's no fix to draw for). View only.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement heldFix; Label heldFixText;

        /// <summary>Whether a ring way is Go held by the rival crew (its holders name a rival or the rival car).</summary>
        static bool HeldByRivals(HandoverHost.RingSeg s)
        {
            if (!s.Shut || string.IsNullOrEmpty(s.Holders) || s.Row?.I?.Order?.Do != "go") return false;
            foreach (var id in s.Holders.Split(',')) { var t = id.Trim(); if (t == "car:rivals" || PlayText.RoleOfId(t) == "rival") return true; }
            return false;
        }

        /// <summary>The held wedge's stripes: diagonal lines clipped to the annular sector (sampled, so no clip mask is needed), then the violet holder dot under the lock.</summary>
        static void HeldStripes(Painter2D p, Vector2 c, float ri, float ro, float a0, float a1, float mid)
        {
            p.fillColor = InkDeep; p.BeginPath(); p.Arc(c, ro, Deg(a0), Deg(a1)); p.Arc(c, ri, Deg(a1), Deg(a0), ArcDirection.CounterClockwise); p.ClosePath(); p.Fill();
            bool In(Vector2 q) { var d = q - c; float r = d.magnitude; if (r < ri + 1.5f || r > ro - 1.5f) return false; float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; float u = Mathf.DeltaAngle(a0, a), span = a1 - a0; return u >= 0.8f && u <= span - 0.8f; }
            p.strokeColor = new Color(Plate.r, Plate.g, Plate.b, 1f); p.lineWidth = 3.5f; p.lineCap = LineCap.Butt;
            var dir = new Vector2(1f, -1f).normalized; var nrm = new Vector2(dir.y, -dir.x);
            for (float off = -ro; off <= ro; off += 9f)
            {
                Vector2 o = c + nrm * off; bool on = false; Vector2 from = o;
                for (float t = -ro; t <= ro; t += 1.5f)
                {
                    var q = o + dir * t; bool inside = In(q);
                    if (inside && !on) { from = q; on = true; }
                    else if (!inside && on) { p.BeginPath(); p.MoveTo(from); p.LineTo(q - dir * 1.5f); p.Stroke(); on = false; }
                }
                if (on) { p.BeginPath(); p.MoveTo(from); p.LineTo(o + dir * ro); p.Stroke(); }
            }
            var dot = c + Dir(mid) * ((ro + ri) / 2f + 14f); p.fillColor = RivalViolet; p.BeginPath(); p.Arc(dot, 4.5f, Deg(0), Deg(360)); p.Fill();
        }

        /// <summary>The fix under the held Go's pill (a plate, left-aligned with it); hidden when no held Go is shown.</summary>
        void HeldGoFix(bool on, float x, float y)
        {
            var words = BookWordsFile.Get().HeldFix; on &= words.Length > 0;
            if (heldFix == null) { if (!on) return; heldFix = PlateDark(ringRoot, 30f, 0.43f); heldFix.style.position = Position.Absolute; heldFix.style.paddingLeft = 10; heldFix.style.paddingRight = 12; var ic = E(heldFix, null, w: 14, h: 14); ic.style.marginRight = 6; Icon(ic, "run", Cream); heldFixText = Words(heldFix, 13.5f); manifest.Add((heldFix, "lock", () => heldFixText.text)); }
            Show(heldFix, on); if (!on) return;
            Text(heldFixText, words); heldFix.style.left = x; heldFix.style.top = y;
        }
    }
}
