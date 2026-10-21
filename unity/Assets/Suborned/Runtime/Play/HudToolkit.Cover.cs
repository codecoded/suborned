using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 027 B5 (P7, EXPERIMENTAL with the lab preset): the cover marks near each selected drawn agent during a fight (Game.CoverMarks,
    /// within coverSnap.marks). Each mark is a half-shield on the kneel point and the arc it shields, drawn on the ground on that side: a full
    /// point's arc solid and its shield filled, a partial point's arc dashed and its shield half filled. It shields against the agent's nearest
    /// combatant now: cell turquoise; it doesn't (that combatant is outside its arc): cream, dimmed; held by someone else: faint, it can't be
    /// taken. The point a hovered move will snap to (HostCover) is lit, a little larger and pulsing, with a dashed tether from the cursor's
    /// ground; the move ring already stands on it. Read only.</summary>
    public sealed partial class HudToolkit
    {
        partial void FlCoverMarks(Game g, float now)
        {
            if (g == null || !g.CoverSnapOn || !(g.FightOn || host.Frozen)) return;
            var mv = host.MoveNow; string snap = mv?.SnapId ?? ""; var done = new HashSet<string>();
            for (int k = 0; k < 4; k++)
            {
                if (!host.Selected[k]) continue;
                List<CoverMark> marks = null; try { marks = g.CoverMarks((OpId)k); } catch { }
                if (marks == null) continue;
                string me = Op.Name((OpId)k);
                foreach (var c in marks)
                {
                    if (!done.Add(c.Id)) continue;
                    bool held = c.HeldBy.Length > 0 && c.HeldBy != me, lit = c.Id == snap;
                    Color col = held ? new Color(Cream.r, Cream.g, Cream.b, 0.35f) : c.Shields || lit ? CellTurquoiseC : new Color(Cream.r, Cream.g, Cream.b, 0.9f);
                    // the arc it shields: ±acos(cos) about Faces, 70 cm out, projected point by point
                    float half = Mathf.Acos(Mathf.Clamp(c.Cos / 1000f, -1f, 1f)); float face = Mathf.Atan2(c.Faces.Y, c.Faces.X); float r = lit ? 85f : 70f;
                    var arc = new List<Vector2>(); int n = 14;
                    for (int i = 0; i <= n; i++)
                    {
                        float a = face - half + 2f * half * i / n; var p = new Pt(c.At.X + Mathf.RoundToInt(Mathf.Cos(a) * r), c.At.Y + Mathf.RoundToInt(Mathf.Sin(a) * r));
                        if (Proj(host.frame.World(p) + Vector3.up * 0.05f, out var q)) arc.Add(q);
                    }
                    if (!Proj(host.frame.World(c.At) + Vector3.up * 0.05f, out var at)) continue;
                    float pulse = lit ? 0.75f + 0.25f * Mathf.Sin(now * Mathf.PI * 2f / 1.2f) : 1f; bool full = c.Full; float sc = lit ? 1.25f : 1f;
                    Vector2? tether = lit && mv.SnapFrom.HasValue && Proj(mv.SnapFrom.Value + Vector3.up * 0.05f, out var tf) ? tf : null;
                    flPaints.Add(pp =>
                    {
                        if (tether.HasValue) DashLine(pp, tether.Value, at, CellTurquoiseC, 1.6f);
                        if (arc.Count > 1)
                        {
                            // an ink under-stroke so it reads on pale stone, then the arc: solid (full) or dashed (partial)
                            pp.lineCap = LineCap.Round; pp.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.8f * Mathf.Max(0.5f, col.a)); pp.lineWidth = (full ? 6.5f : 5f) * sc;
                            pp.BeginPath(); pp.MoveTo(arc[0]); for (int i = 1; i < arc.Count; i++) pp.LineTo(arc[i]); pp.Stroke();
                            var ac = new Color(col.r, col.g, col.b, col.a * pulse); pp.strokeColor = ac; pp.lineWidth = (full ? 4f : 2.6f) * sc; // full: thick, partial: thin (both solid: dashes vanished on stone)
                            pp.BeginPath(); pp.MoveTo(arc[0]); for (int i = 1; i < arc.Count; i++) pp.LineTo(arc[i]); pp.Stroke();
                        }
                        HalfShield(pp, at + new Vector2(0, lit ? -40f : -11f), 8.5f * sc, col, full, pulse); // the snap target's shield above the move ring (the ring stands on the point)
                    });
                }
            }
        }

        /// <summary>The half-shield: an ink-rimmed shield 14 px tall; full = filled, partial = its left half filled; the outline in the mark's colour.</summary>
        static void HalfShield(Painter2D p, Vector2 c, float r, Color col, bool full, float pulse)
        {
            Vector2 tl = c + new Vector2(-r, -r), tr = c + new Vector2(r, -r), bot = c + new Vector2(0, r * 1.15f);
            void Outline() { p.BeginPath(); p.MoveTo(tl); p.LineTo(tr); p.BezierCurveTo(tr + new Vector2(0, r * 1.2f), bot + new Vector2(r * 0.5f, -r * 0.2f), bot); p.BezierCurveTo(bot + new Vector2(-r * 0.5f, -r * 0.2f), tl + new Vector2(0, r * 1.2f), tl); p.ClosePath(); }
            p.fillColor = new Color(FlGround.r, FlGround.g, FlGround.b, 0.88f * col.a); Outline(); p.Fill();
            var fc = new Color(col.r, col.g, col.b, col.a * pulse);
            if (full) { p.fillColor = fc; Outline(); p.Fill(); }
            else { p.fillColor = fc; p.BeginPath(); p.MoveTo(tl); p.LineTo(new Vector2(c.x, tl.y)); p.LineTo(bot); p.BezierCurveTo(bot + new Vector2(-r * 0.5f, -r * 0.2f), tl + new Vector2(0, r * 1.2f), tl); p.ClosePath(); p.Fill(); }
            p.strokeColor = new Color(Ink.r, Ink.g, Ink.b, col.a); p.lineWidth = 3f; p.lineJoin = LineJoin.Round; Outline(); p.Stroke();
            p.strokeColor = new Color(Cream.r, Cream.g, Cream.b, col.a); p.lineWidth = 1.2f; Outline(); p.Stroke(); // a cream rim on an ink line: it reads on pale stone and on the road
        }

        static void DashLine(Painter2D p, Vector2 a, Vector2 b, Color col, float w)
        {
            float len = Vector2.Distance(a, b); if (len < 1f) return; var d = (b - a) / len; p.strokeColor = col; p.lineWidth = w; p.lineCap = LineCap.Butt;
            for (float t = 0; t < len; t += 10f) { p.BeginPath(); p.MoveTo(a + d * t); p.LineTo(a + d * Mathf.Min(len, t + 6f)); p.Stroke(); }
        }
    }
}
