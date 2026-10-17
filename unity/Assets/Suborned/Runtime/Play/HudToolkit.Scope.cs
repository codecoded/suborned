using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The plot scope (brief 014 rule 7; Glyph §5; Sky: "knowing where Vale is located is not obvious"): a period plotting instrument,
    /// 212 x 132 at 1080p in the bottom left, heading-up: it turns with the view (brief 016), so up on the scope is up on the street at every
    /// yaw, with a north tick on the glass's rim; one scale fits the block at all four yaws. The glass shows the
    /// block from the engine's own walk grid (open ground dull teal, everything built seafoam), and only what the cell knows, told or learned:
    /// the cell as numbered discs (the selected ringed), Vale exact only while an agent sees him (else a dashed ring at his last known place,
    /// growing with its age; the briefing gives the Office), the key people's glyphs (the same as their pips), learned cones (grey when not
    /// live), the patrol's route once its routine is learned, held doors, bodies, stashes holding one, reports in transit and units, and the
    /// main view as a trapezoid. A click or drag pans the camera (in the turned frame: the point under the pointer comes to the centre); a click on a cell disc selects that agent; a key person's glyph finds them.
    /// No orders from the scope: an order needs the street's price first.</summary>
    public sealed partial class HudToolkit
    {
        const float ScopeW = 212, ScopeH = 132, GlassInset = 8;
        VisualElement scope, glass, scopeImage; Paint scopePaint, scopeRim; Vector2 northTip, northDir; bool northOn; Texture2D scopeTex; float scopeImageSize;
        Vector3 scopeC; Vector2 scopeR, scopeF; float scopeScale; bool scopeReady;
        readonly List<VisualElement> blips = new(); int blipUsed;
        readonly List<Fan> scopeFans = new(); float fansAt = -1f; bool scopeDrag;
        static readonly Color Glass = new(0.043f, 0.122f, 0.133f), Floor = new(0.118f, 0.29f, 0.286f), Built = new(0.33f, 0.58f, 0.53f);

        void BuildScope()
        {
            scope = E(root, "s-panel_dark", pick: true); scope.style.position = Position.Absolute; scope.style.left = 14; scope.style.bottom = 14; scope.style.width = ScopeW; scope.style.height = ScopeH;
            glass = E(scope, null, GlassInset, GlassInset, ScopeW - 2 * GlassInset, ScopeH - 2 * GlassInset, pick: true); glass.style.overflow = Overflow.Hidden; glass.style.backgroundColor = Glass; Round(glass, 5);
            scopeImage = E(glass, null, 0, 0, 1, 1); scopeImage.pickingMode = PickingMode.Ignore; // the block, baked north-up, turned with the view under the marks
            scopePaint = P(glass, 0, 0, ScopeW - 2 * GlassInset, ScopeH - 2 * GlassInset, DrawScope);
            scopeRim = P(scope, 0, 0, ScopeW, ScopeH, DrawNorth); // on the frame, over the glass's edge: the north wedge sits on the rim
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

        /// <summary>The mapping (once the camera stands): the block's centre, and one scale that fits the grid's footprint into the glass at every
        /// one of the four yaws; and the block's picture, sampled from the engine's walk grid north-up (x east, y north) on a square the
        /// footprint's diagonal wide, so it covers the glass at any turn.</summary>
        void ScopeSetup()
        {
            var cam = host.View; var g = host.Game; if (!cam || g == null) return;
            var grid = g.S.Grid; var fr = host.frame;
            Vector3 W(int i, int j) => fr.World(new Pt(grid.X0 + i * grid.Cell, grid.Y0 + j * grid.Cell));
            var corners = new[] { W(0, 0), W(grid.Nx, 0), W(0, grid.Ny), W(grid.Nx, grid.Ny) };
            scopeC = (corners[0] + corners[3]) / 2f; scopeC.y = 0;
            float gw = ScopeW - 2 * GlassInset, gh = ScopeH - 2 * GlassInset, diag = 0f;
            scopeScale = float.MaxValue;
            for (int k = 0; k < 4; k++)
            {
                float yaw = (cam.transform.eulerAngles.y + 90f * k) * Mathf.Deg2Rad; var R = new Vector2(Mathf.Cos(yaw), -Mathf.Sin(yaw)); var F = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw));
                float umin = 1e9f, umax = -1e9f, vmin = 1e9f, vmax = -1e9f;
                foreach (var c in corners) { var d = new Vector2(c.x - scopeC.x, c.z - scopeC.z); float u = Vector2.Dot(d, R), v = Vector2.Dot(d, F); umin = Mathf.Min(umin, u); umax = Mathf.Max(umax, u); vmin = Mathf.Min(vmin, v); vmax = Mathf.Max(vmax, v); diag = Mathf.Max(diag, d.magnitude); }
                scopeScale = Mathf.Min(scopeScale, Mathf.Min(gw / (umax - umin), gh / (vmax - vmin)) * 0.98f);
            }
            ScopeAxes(cam);
            // the picture: a square of side 2 * max(the footprint's half diagonal, the glass's half diagonal / scale), 3 texels a glass pixel
            float half = Mathf.Max(diag, 0.5f * Mathf.Sqrt(gw * gw + gh * gh) / scopeScale) + 2f; scopeImageSize = 2f * half * scopeScale;
            int tn = Mathf.Min(1024, Mathf.CeilToInt(scopeImageSize * 3f)); scopeTex = new Texture2D(tn, tn, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[tn * tn];
            for (int y = 0; y < tn; y++)
                for (int x = 0; x < tn; x++)
                {
                    var w = new Vector3(scopeC.x + ((x + 0.5f) / tn * 2f - 1f) * half, 0f, scopeC.z + ((y + 0.5f) / tn * 2f - 1f) * half); var p = fr.Engine(w); // texture row 0 = the south edge
                    int ci = grid.CI(p.X), cj = grid.CJ(p.Y); bool inside = ci >= 0 && cj >= 0 && ci < grid.Nx && cj < grid.Ny;
                    px[y * tn + x] = !inside ? new Color32(0, 0, 0, 0) : grid.FreeCell(ci, cj) ? Floor : Built; // outside the block: clear (the glass shows), so a turned corner never paints past the glass
                }
            scopeTex.SetPixels32(px); scopeTex.Apply();
            scopeImage.style.backgroundImage = new StyleBackground(scopeTex);
            scopeImage.style.left = gw / 2 - scopeImageSize / 2; scopeImage.style.top = gh / 2 - scopeImageSize / 2; scopeImage.style.width = scopeImageSize; scopeImage.style.height = scopeImageSize;
            scopeImage.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));
            scopeReady = true;
        }

        /// <summary>The camera's right and forward on the ground now (the scope turns with the view), and the picture turned to match: a
        /// north-up picture turned by minus the yaw (screen y runs down, so that is a turn of -yaw in the panel's terms).</summary>
        void ScopeAxes(UnityEngine.Camera cam)
        {
            var r3 = cam.transform.right; var f3 = cam.transform.forward; scopeR = new Vector2(r3.x, r3.z).normalized; scopeF = new Vector2(f3.x, f3.z).normalized;
            if (scopeImage != null) scopeImage.style.rotate = new Rotate(new Angle(-cam.transform.eulerAngles.y, AngleUnit.Degree));
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

        /// <summary>The north wedge on the frame (scope space): 11 px wide at its base on the glass's edge, 6 px out into the chrome, and a
        /// 2 px tick along the rim under it.</summary>
        void DrawNorth(Painter2D p, Rect r)
        {
            if (!northOn) return; var at = northTip; var o = northDir; var along = new Vector2(-o.y, o.x);
            p.fillColor = Cream; p.BeginPath(); p.MoveTo(at + along * 5.5f); p.LineTo(at + o * 6f); p.LineTo(at - along * 5.5f); p.ClosePath(); p.Fill();
            p.strokeColor = Cream; p.lineWidth = 2f; p.lineCap = LineCap.Butt; p.BeginPath(); p.MoveTo(at + along * 7.5f - o * 1f); p.LineTo(at - along * 7.5f - o * 1f); p.Stroke();
        }

        void ScopeFrame()
        {
            var g = host.Game; Show(scope, g != null && !m.Ended && !m.BriefingOpen); if (g == null) return;
            if (!scopeReady) ScopeSetup(); if (!scopeReady) return;
            if (host.View) ScopeAxes(host.View);
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
            // north (Glyph's 016 review §1): a cream wedge seated on the frame's inner rim where north lies from the centre, base on the rim line,
            // apex out into the chrome, a 2 px tick on the rim under it; slid along the straight edges, never into the rounded corners
            {
                var nd = new Vector2(Vector2.Dot(Vector2.up, scopeR), -Vector2.Dot(Vector2.up, scopeF)); // world north (+z) in glass terms (y down)
                float hx = gw / 2, hy = gh / 2, kx = Mathf.Abs(nd.x) > 1e-4f ? hx / Mathf.Abs(nd.x) : 1e9f, ky = Mathf.Abs(nd.y) > 1e-4f ? hy / Mathf.Abs(nd.y) : 1e9f;
                const float Corner = 10f; Vector2 on, outward;
                if (ky <= kx) { float x = Mathf.Clamp(nd.x * ky, -hx + Corner, hx - Corner); on = new Vector2(hx + x, nd.y < 0 ? 0 : gh); outward = new Vector2(0, nd.y < 0 ? -1 : 1); }
                else { float y = Mathf.Clamp(nd.y * kx, -hy + Corner, hy - Corner); on = new Vector2(nd.x < 0 ? 0 : gw, hy + y); outward = new Vector2(nd.x < 0 ? -1 : 1, 0); }
                northTip = on + new Vector2(GlassInset, GlassInset); northDir = outward; northOn = true; scopeRim.MarkDirtyRepaint();
            }
            for (int i = blipUsed; i < blips.Count; i++) Show(blips[i], false);
            scopePaint.MarkDirtyRepaint();
        }
    }
}
