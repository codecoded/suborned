using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The plot scope (brief 014 rule 7; Glyph §5; Sky: "knowing where Vale is located is not obvious"): brief 101 step 5 (Glyph's S mocks, briefs/101/glyph/r2):
    /// a small round lens of dark glass, 146 px at 1080p in the bottom left, a cream rim, its three round tabs on the rim (turn left, the cones eye, turn right:
    /// HudToolkit.Keys096, HudToolkit.Sight); it retires the 212 x 132 plate. Heading-up: it turns with the view (brief 016), so up on the scope is up on the street at
    /// every yaw, with a north wedge on the rim; one scale keeps the whole block inside the lens at every yaw (its corners within the map's radius). The glass shows the
    /// block from the engine's own walk grid (open ground dull teal, everything built seafoam), and only what the cell knows, told or learned:
    /// the cell as numbered discs (the selected ringed), Vale exact only while an agent sees him (else a dashed ring at his last known place,
    /// growing with its age; the briefing gives the Office), the key people's glyphs (the same as their pips), learned cones (grey when not
    /// live), the patrol's route once its routine is learned, held doors, bodies, stashes holding one, reports in transit and units, and the
    /// main view as a trapezoid. A click or drag pans the camera (in the turned frame: the point under the pointer comes to the centre); a click on a cell disc selects that agent; a key person's glyph finds them.
    /// No orders from the scope: an order needs the street's price first.</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>The lens (the glass is the whole disc; the map keeps 7 px off the rim) and its place: the mock's 146 px at (18, 922).</summary>
        const float ScopeW = 146, ScopeH = 146, GlassInset = 0, MapIn = 7, LensLeft = 18, LensBottom = 12;
        VisualElement scope, glass, scopeImage; Paint scopePaint, scopeRim; Vector2 northTip, northDir; bool northOn; Texture2D scopeTex; float scopeImageSize;
        Vector3 scopeC; Vector2 scopeR, scopeF; float scopeScale; bool scopeReady;
        readonly List<VisualElement> blips = new(); int blipUsed;
        readonly List<Fan> scopeFans = new(); float fansAt = -1f; bool scopeDrag;
        /// <summary>The glass (the book's ink, 87 %, as the mock), the open ground (the book's deep teal, half) and what is built (the pale seafoam, 60 %): the street's light parts on dark glass.</summary>
        static readonly Color Glass = new(0.09f, 0.188f, 0.184f, 0.87f), Floor = new(0.122f, 0.424f, 0.404f, 0.5f), Built = new(0.608f, 0.812f, 0.761f, 0.6f);

        void BuildScope()
        {
            scope = E(root, null, pick: false); scope.style.position = Position.Absolute; scope.style.left = LensLeft; scope.style.bottom = LensBottom; scope.style.width = ScopeW; scope.style.height = ScopeH; scope.style.overflow = Overflow.Visible;
            var sh = Disc(scope, ScopeW, "e-shadow"); Pin(sh, new Vector2(0f, 2.5f)); sh.style.opacity = 0.8f; // the lens stands on a soft shadow
            glass = E(scope, null, GlassInset, GlassInset, ScopeW - 2 * GlassInset, ScopeH - 2 * GlassInset, pick: true); glass.style.overflow = Overflow.Hidden; glass.style.backgroundColor = Glass; Round(glass, ScopeW / 2f);
            scopeImage = E(glass, null, 0, 0, 1, 1); scopeImage.pickingMode = PickingMode.Ignore; // the block, baked north-up, turned with the view under the marks
            scopePaint = P(glass, 0, 0, ScopeW - 2 * GlassInset, ScopeH - 2 * GlassInset, DrawScope);
            var rim = E(scope, "e-inline", 0, 0, ScopeW, ScopeH); Round(rim, ScopeW / 2f); BorderW(rim, 2.2f); // the cream rim
            scopeRim = P(scope, 0, 0, ScopeW, ScopeH, DrawNorth); // over the rim: the north wedge sits on it
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
            foreach (var c in corners) diag = Mathf.Max(diag, new Vector2(c.x - scopeC.x, c.z - scopeC.z).magnitude);
            scopeScale = (ScopeW / 2f - MapIn) / Mathf.Max(1f, diag) * 0.98f; // the round lens: the block's corners stay inside the map's circle at every yaw
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

        /// <summary>The three tabs' places on the rim, in degrees (0 east, y down): turn left, the cones eye, turn right (the mock's).</summary>
        static readonly float[] ScopeTabAngles = { -145f, -90f, -35f };
        /// <summary>A tab of `d` px on the rim at `deg` (its centre 7 px out of the rim, the mock's radius 80).</summary>
        static Vector2 ScopeTabAt(float deg, float d) => new Vector2(ScopeW / 2f, ScopeH / 2f) + new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)) * (ScopeW / 2f + 7f) - new Vector2(d / 2f, d / 2f);

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
            SetNum(b.Q<Label>("n"), ""); if (art != null) Art(b, art, tint); else b.style.backgroundImage = StyleKeyword.Null;
            return b;
        }

        readonly List<System.Action<Painter2D>> scopeOps = new();
        void DrawScope(Painter2D p, Rect r) { foreach (var f in scopeOps) f(p); }

        /// <summary>The north wedge on the rim (scope space): 11 px wide at its base on the rim, 6 px out, cream over an ink edge so it reads on any ground.</summary>
        void DrawNorth(Painter2D p, Rect r)
        {
            if (!northOn) return; var at = northTip; var o = northDir; var along = new Vector2(-o.y, o.x);
            p.fillColor = BkInk; p.BeginPath(); p.MoveTo(at + along * 7f - o * 1f); p.LineTo(at + o * 7.5f); p.LineTo(at - along * 7f - o * 1f); p.ClosePath(); p.Fill();
            p.fillColor = Cream; p.BeginPath(); p.MoveTo(at + along * 5.5f); p.LineTo(at + o * 6f); p.LineTo(at - along * 5.5f); p.ClosePath(); p.Fill();
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
                Border(b, host.Selected[k] ? 1.6f : 0.6f, host.Selected[k] ? TurqLight : Ink); var n = b.Q<Label>("n"); SetNum(n, (k + 1).ToString()); n.style.color = Ink;
            }
            // the levels: street, deck, sky concourse; the scope draws the street (every Handover act is on it): three dots inside the rim at the lower right
            scopeOps.Add(pp => { for (int i = 0; i < 3; i++) { var lc = mid + new Vector2(Mathf.Cos((32f + i * 7f) * Mathf.Deg2Rad), Mathf.Sin((32f + i * 7f) * Mathf.Deg2Rad)) * (gw / 2f - 10f); pp.fillColor = i == 0 ? Cream : new Color(1, 1, 1, 0.25f); pp.BeginPath(); pp.Arc(lc, 1.8f, Deg(0), Deg(360)); pp.Fill(); } });
            // north (Glyph's 016 review §1): a cream wedge seated on the rim where north lies from the centre, its apex out; the tabs keep the top, so a north under one
            // slides along the rim clear of it (the tabs stand at -145, -90 and -35 degrees)
            {
                var nd = new Vector2(Vector2.Dot(Vector2.up, scopeR), -Vector2.Dot(Vector2.up, scopeF)).normalized; float a = Mathf.Atan2(nd.y, nd.x) * Mathf.Rad2Deg;
                foreach (var tabAt in ScopeTabAngles) if (Mathf.Abs(Mathf.DeltaAngle(a, tabAt)) < 14f) a = tabAt + (Mathf.DeltaAngle(tabAt, a) >= 0f ? 14f : -14f);
                var o = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                northTip = new Vector2(ScopeW / 2f, ScopeH / 2f) + o * (ScopeW / 2f); northDir = o; northOn = true; scopeRim.MarkDirtyRepaint();
            }
            for (int i = blipUsed; i < blips.Count; i++) Show(blips[i], false);
            scopePaint.MarkDirtyRepaint();
        }
    }
}
