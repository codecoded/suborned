using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The plot scope (brief 014 rule 7; Glyph §5; Sky: "knowing where Vale is located is not obvious"): a period plotting instrument,
    /// 212 x 132 at 1080p in the bottom left, rotated to the camera's fixed yaw so up on the scope is up on the street. The glass shows the
    /// block from the engine's own walk grid (open ground dull teal, everything built seafoam), and only what the cell knows, told or learned:
    /// the cell as numbered discs (the selected ringed), Vale exact only while an agent sees him (else a dashed ring at his last known place,
    /// growing with its age; the briefing gives the Office), the key people's glyphs (the same as their pips), learned cones (grey when not
    /// live), the patrol's route once its routine is learned, held doors, bodies, stashes holding one, reports in transit and units, and the
    /// main view as a trapezoid. A click or drag pans the camera; a click on a cell disc selects that agent; a key person's glyph finds them.
    /// No orders from the scope: an order needs the street's price first.</summary>
    public sealed partial class HudToolkit
    {
        const float ScopeW = 212, ScopeH = 132, GlassInset = 8;
        VisualElement scope, glass; Paint scopePaint; Texture2D scopeTex;
        Vector3 scopeC; Vector2 scopeR, scopeF; float scopeScale; bool scopeReady;
        readonly List<VisualElement> blips = new(); int blipUsed;
        readonly List<Fan> scopeFans = new(); float fansAt = -1f; bool scopeDrag;
        static readonly Color Glass = new(0.043f, 0.122f, 0.133f), Floor = new(0.118f, 0.29f, 0.286f), Built = new(0.33f, 0.58f, 0.53f);

        void BuildScope()
        {
            scope = E(root, "s-panel_dark", pick: true); scope.style.position = Position.Absolute; scope.style.left = 14; scope.style.bottom = 14; scope.style.width = ScopeW; scope.style.height = ScopeH;
            glass = E(scope, null, GlassInset, GlassInset, ScopeW - 2 * GlassInset, ScopeH - 2 * GlassInset, pick: true); glass.style.overflow = Overflow.Hidden; glass.style.backgroundColor = Glass; Round(glass, 5);
            scopePaint = P(glass, 0, 0, ScopeW - 2 * GlassInset, ScopeH - 2 * GlassInset, DrawScope);
            glass.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return; e.StopPropagation(); scopeDrag = true; glass.CapturePointer(e.pointerId);
                var lp = (Vector2)e.localPosition;
                for (int k = 0; k < 4; k++) if (ScopeOf(host.frame.World(host.Game.Ops[k].Pos)) is Vector2 q && (q - lp).sqrMagnitude < 64) { host.PortraitClick(k, e.shiftKey); scopeDrag = false; return; }
                foreach (var role in Cast) if (role != "councillor" && host.Views?.Find(role) is Transform t && t.gameObject.activeSelf && ScopeOf(t.position) is Vector2 q2 && (q2 - lp).sqrMagnitude < 64) { FindPerson(role); scopeDrag = false; return; }
                if (ScopeOf(host.ValeKnown) is Vector2 v && (v - lp).sqrMagnitude < 100) { FindPerson("councillor"); scopeDrag = false; return; }
                host.LookAt(WorldOf(lp));
            });
            glass.RegisterCallback<PointerMoveEvent>(e => { if (scopeDrag) host.LookAt(WorldOf(e.localPosition)); });
            glass.RegisterCallback<PointerUpEvent>(e => { scopeDrag = false; glass.ReleasePointer(e.pointerId); });
            manifest.Add((scope, "image", () => "plot scope"));
        }

        /// <summary>The mapping (once the camera stands): the block's centre, the camera's right and forward on the ground, the scale that fits the
        /// grid's turned footprint into the glass; and the glass's texture, sampled from the engine's walk grid.</summary>
        void ScopeSetup()
        {
            var cam = host.View; var g = host.Game; if (!cam || g == null) return;
            var grid = g.S.Grid; var fr = host.frame;
            Vector3 W(int i, int j) => fr.World(new Pt(grid.X0 + i * grid.Cell, grid.Y0 + j * grid.Cell));
            var r3 = cam.transform.right; var f3 = cam.transform.forward; scopeR = new Vector2(r3.x, r3.z).normalized; scopeF = new Vector2(f3.x, f3.z).normalized;
            var corners = new[] { W(0, 0), W(grid.Nx, 0), W(0, grid.Ny), W(grid.Nx, grid.Ny) };
            scopeC = (corners[0] + corners[3]) / 2f; scopeC.y = 0;
            float umin = 1e9f, umax = -1e9f, vmin = 1e9f, vmax = -1e9f;
            foreach (var c in corners) { var d = new Vector2(c.x - scopeC.x, c.z - scopeC.z); float u = Vector2.Dot(d, scopeR), v = Vector2.Dot(d, scopeF); umin = Mathf.Min(umin, u); umax = Mathf.Max(umax, u); vmin = Mathf.Min(vmin, v); vmax = Mathf.Max(vmax, v); }
            float gw = ScopeW - 2 * GlassInset, gh = ScopeH - 2 * GlassInset;
            scopeScale = Mathf.Min(gw / (umax - umin), gh / (vmax - vmin)) * 0.98f;
            int tw = (int)gw * 2, th = (int)gh * 2; scopeTex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[tw * th];
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    var w = WorldOf(new Vector2(x / 2f, gh - y / 2f)); var p = fr.Engine(w);
                    int ci = grid.CI(p.X), cj = grid.CJ(p.Y); bool inside = ci >= 0 && cj >= 0 && ci < grid.Nx && cj < grid.Ny;
                    px[y * tw + x] = !inside ? Glass : grid.FreeCell(ci, cj) ? Floor : Built;
                }
            scopeTex.SetPixels32(px); scopeTex.Apply();
            glass.style.backgroundImage = new StyleBackground(scopeTex);
            scopeReady = true;
        }

        Vector2? ScopeOf(Vector3 w)
        {
            if (!scopeReady) return null;
            var d = new Vector2(w.x - scopeC.x, w.z - scopeC.z);
            return new Vector2((ScopeW - 2 * GlassInset) / 2 + Vector2.Dot(d, scopeR) * scopeScale, (ScopeH - 2 * GlassInset) / 2 - Vector2.Dot(d, scopeF) * scopeScale);
        }

        Vector3 WorldOf(Vector2 local)
        {
            float u = (local.x - (ScopeW - 2 * GlassInset) / 2) / scopeScale, v = -(local.y - (ScopeH - 2 * GlassInset) / 2) / scopeScale;
            var d = scopeR * u + scopeF * v; return new Vector3(scopeC.x + d.x, 0, scopeC.z + d.y);
        }

        VisualElement Blip(string art, float size, Color? tint = null)
        {
            while (blips.Count <= blipUsed) { var e = E(glass, null, 0, 0, 12, 12); var l = L(e, "keycap"); l.name = "n"; l.style.position = Position.Absolute; l.style.left = 0; l.style.top = 0; l.style.right = 0; l.style.bottom = 0; l.style.unityTextAlign = TextAnchor.MiddleCenter; l.style.fontSize = 8.5f; blips.Add(e); }
            var b = blips[blipUsed++]; Show(b, true); b.style.width = size; b.style.height = size; Border(b, 0, Color.clear); Round(b, 0); b.style.backgroundColor = Color.clear;
            b.Q<Label>("n").text = ""; if (art != null) Art(b, art, tint); else b.style.backgroundImage = StyleKeyword.Null;
            return b;
        }

        readonly List<System.Action<Painter2D>> scopeOps = new();
        void DrawScope(Painter2D p, Rect r) { foreach (var f in scopeOps) f(p); }

        void ScopeFrame()
        {
            var g = host.Game; Show(scope, g != null && !m.Ended && !m.BriefingOpen); if (g == null) return;
            if (!scopeReady) ScopeSetup(); if (!scopeReady) return;
            scopeOps.Clear(); blipUsed = 0; bool alt = host.Peeking; float now = Time.unscaledTime;
            // a faint plotting grid and the sweep arm (cosmetic)
            float gw = ScopeW - 2 * GlassInset, gh = ScopeH - 2 * GlassInset; var mid = new Vector2(gw / 2, gh / 2);
            scopeOps.Add(pp =>
            {
                pp.strokeColor = new Color(0.6f, 0.85f, 0.8f, 0.07f); pp.lineWidth = 1;
                for (float x = 12; x < gw; x += 24) { pp.BeginPath(); pp.MoveTo(new Vector2(x, 0)); pp.LineTo(new Vector2(x, gh)); pp.Stroke(); }
                for (float y = 12; y < gh; y += 24) { pp.BeginPath(); pp.MoveTo(new Vector2(0, y)); pp.LineTo(new Vector2(gw, y)); pp.Stroke(); }
                float a = Time.unscaledTime * 0.9f; pp.strokeColor = new Color(0.6f, 0.95f, 0.9f, 0.12f); pp.lineWidth = 1.5f; pp.BeginPath(); pp.MoveTo(mid); pp.LineTo(mid + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 140); pp.Stroke();
            });
            // learned cones (every one the cell has learned: the scope remembers), grey when not live; the patrol's route once its routine is known
            if (now - fansAt > 0.25f) { fansAt = now; scopeFans.Clear(); try { scopeFans.AddRange(g.KnownFans()); } catch { } }
            foreach (var f in scopeFans)
            {
                if (f.Points.Length < 3) continue; var pts = new List<Vector2>(); foreach (var pt in f.Points) if (ScopeOf(host.frame.World(pt)) is Vector2 q) pts.Add(q);
                Color c = !f.Live ? new Color(0.6f, 0.65f, 0.64f) : f.Kind == "camera" ? Red : f.Kind == "rivals" ? Violet : Amber;
                scopeOps.Add(pp => { pp.fillColor = new Color(c.r, c.g, c.b, 0.28f); pp.strokeColor = c; pp.lineWidth = 1; pp.BeginPath(); pp.MoveTo(pts[0]); for (int i = 1; i < pts.Count; i++) pp.LineTo(pts[i]); pp.ClosePath(); pp.Fill(); pp.Stroke(); });
            }
            if (g.Known("patrol") is { Routine: true } && !g.PatrolDown && ScopeOf(host.frame.World(g.S.Stand(g.S.PatrolA))) is Vector2 pa && ScopeOf(host.frame.World(g.S.Stand(g.S.PatrolB))) is Vector2 pb)
                scopeOps.Add(pp => { pp.fillColor = Amber; float len = Vector2.Distance(pa, pb); int n = Mathf.Max(2, (int)(len / 4)); for (int i = 0; i <= n; i++) { pp.BeginPath(); pp.Arc(Vector2.Lerp(pa, pb, i / (float)n), 0.9f, Deg(0), Deg(360)); pp.Fill(); } });
            // the main view's footprint on the ground
            var cam = host.View;
            if (cam)
            {
                var ground = new Plane(Vector3.up, Vector3.zero); var vs = new List<Vector2>();
                foreach (var c in new[] { new Vector3(0, 0), new Vector3(1, 0), new Vector3(1, 1), new Vector3(0, 1) })
                { var ray = cam.ViewportPointToRay(c); if (ground.Raycast(ray, out var d) && ScopeOf(ray.GetPoint(d)) is Vector2 q) vs.Add(q); }
                if (vs.Count == 4) scopeOps.Add(pp => { pp.strokeColor = new Color(1, 1, 1, 0.85f); pp.lineWidth = 1.2f; pp.BeginPath(); pp.MoveTo(vs[0]); pp.LineTo(vs[1]); pp.LineTo(vs[2]); pp.LineTo(vs[3]); pp.ClosePath(); pp.Stroke(); });
            }
            // held doors, bodies, stashes holding a body, reports in transit, units
            foreach (var d in m.Doors) if (!d.Preview && ScopeOf(d.World) is Vector2 q) { var b = Blip("icons/singles/lock", 10, Cream); Place(b, q, 10, 10); }
            foreach (var b0 in m.Bodies) if (ScopeOf(b0.World) is Vector2 q) { var b = Blip(b0.Dead ? "icons/singles/body_dead" : "icons/singles/body_out", 10, Cream); Place(b, q, 10, 10); }
            foreach (var st in g.S.Stashes) { var (used, _) = g.StashRoom(st.Id); if ((used > 0 || alt) && ScopeOf(host.frame.World(st.At)) is Vector2 q) scopeOps.Add(pp => { pp.fillColor = used > 0 ? Cream : new Color(1, 1, 1, 0.4f); pp.BeginPath(); pp.MoveTo(q + new Vector2(0, -4)); pp.LineTo(q + new Vector2(4, 0)); pp.LineTo(q + new Vector2(0, 4)); pp.LineTo(q + new Vector2(-4, 0)); pp.ClosePath(); pp.Fill(); }); }
            foreach (var rp in m.Reports) if (ScopeOf(rp.World) is Vector2 q)
                {
                    var b = Blip("icons/singles/bang", 10, Cream); Place(b, q, 10, 10);
                    if (rp.To.HasValue && ScopeOf(rp.To.Value) is Vector2 to) scopeOps.Add(pp => { pp.fillColor = Cream; int n = Mathf.Max(2, (int)(Vector2.Distance(q, to) / 3)); for (int i = 1; i <= n; i++) { pp.BeginPath(); pp.Arc(Vector2.Lerp(q, to, i / (float)n), 0.7f, Deg(0), Deg(360)); pp.Fill(); } });
                }
            foreach (var u in m.Units) if (ScopeOf(u.World) is Vector2 q) { var b = Blip(u.Robot ? "badges/signal_guard" : "badges/signal_police", 10); Place(b, q, 10, 10); }
            // the key people (the same glyphs as their pips): Inez, the guard, the driver where they stand
            foreach (var role in Cast)
            {
                if (role == "councillor" || !KnownOf(role)) continue;
                if (host.Views?.Find(role) is Transform t && t.gameObject.activeSelf && ScopeOf(t.position) is Vector2 q) { var b = Blip(CastBadge(role), 11); Place(b, q, 11, 11); }
            }
            // Vale: exact while seen, else a dashed ring at his last known place that grows with the age of what the cell knows
            if (ScopeOf(host.ValeKnown) is Vector2 vq)
            {
                if (host.ValeSeen) { var b = Blip("badges/npc_councillor", 12); Place(b, vq, 12, 12); }
                else
                {
                    float age = (g.Tick - host.ValeKnownTick) / (float)Game.Hz, rad = Mathf.Min(26f, 6f + age * 0.08f);
                    scopeOps.Add(pp => Dashed(pp, vq, rad, TurqLight, 1.4f, 14));
                    var b = Blip("icons/singles/p_unknown", 9, TurqLight); Place(b, vq, 9, 9);
                }
            }
            // the cell: four numbered discs, the selected ringed turquoise
            for (int k = 0; k < 4; k++)
            {
                var op = g.Ops[k]; if (ScopeOf(host.frame.World(op.Pos)) is not Vector2 q) continue;
                var b = Blip(null, 11); Place(b, q, 11, 11); Round(b, 5.5f); b.style.backgroundColor = op.Down ? new Color(0.5f, 0.52f, 0.52f) : Cream;
                Border(b, host.Selected[k] ? 1.6f : 0.6f, host.Selected[k] ? Turq : Ink); var n = b.Q<Label>("n"); n.text = (k + 1).ToString(); n.style.color = Ink;
            }
            // the levels: street, deck, sky concourse; the scope draws the street (every Handover act is on it)
            scopeOps.Add(pp => { for (int i = 0; i < 3; i++) { pp.fillColor = i == 0 ? Cream : new Color(1, 1, 1, 0.25f); pp.BeginPath(); pp.Arc(new Vector2(gw - 8, gh - 9 - i * 7), 1.8f, Deg(0), Deg(360)); /* Glyph round 2: 7 px in from the glass edge */ pp.Fill(); } });
            for (int i = blipUsed; i < blips.Count; i++) Show(blips[i], false);
            scopePaint.MarkDirtyRepaint();
        }
    }
}
