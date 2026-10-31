using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 085 (Glyph's capture-and-cues section 1): the capture clip as a wide live inset, the same family as the medal: 320 x 180 (240 x 135 right of the scope when the panel is short), hung
    /// 22 px above the plot scope, corners r 12, the medal's strokes (2 px cream over 4 px ink at 0.7, a breathing second ring), a corner cameo, a chevron at the door, one word on a plate. On
    /// enter.out the ring turns turquoise-light with the kit's ready glow and pops once, and the chevron follows him. A click jumps the camera (HostCaptureClip). View only.</summary>
    public sealed partial class HudToolkit
    {
        /// <summary>The clip's own box, grown by Glyph's 14 px, for every keep-out (null until the first frame; hidden while the clip is).</summary>
        VisualElement clipBlock, clipRoot, clipPic, clipPlate, clipCameo; Label clipWord; Paint clipRing; Vector2 clipAim; bool clipOut; float clipOutSeen = -1f;
        /// <summary>For the proofs: where it is on screen (pixels, origin bottom left), null when hidden.</summary>
        public static Vector2? ClipScreen { get { var r = live?.clipRoot; if (r == null || r.resolvedStyle.display == DisplayStyle.None) return null; var c = r.worldBound.center; float k = Screen.height / Mathf.Max(1f, live.root.panel.visualTree.layout.height); return new Vector2(c.x * k, Screen.height - c.y * k); } }
        public static string ClipWordNow => live?.clipWord?.text ?? "";

        void ClipBuild()
        {
            clipBlock = E(root); clipBlock.style.position = Position.Absolute; clipBlock.pickingMode = PickingMode.Ignore; // nothing drawn: the keep-out
            clipRoot = E(root, pick: true); clipRoot.name = "capture-clip"; clipRoot.style.position = Position.Absolute;
            clipRoot.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.ClipClick(); e.StopPropagation(); } });
            clipPic = E(clipRoot); clipPic.style.position = Position.Absolute; clipPic.style.left = 0; clipPic.style.top = 0; clipPic.style.right = 0; clipPic.style.bottom = 0; Round(clipPic, 12); clipPic.style.overflow = Overflow.Hidden; clipPic.style.backgroundColor = Ink; clipPic.pickingMode = PickingMode.Ignore;
            clipRing = P(clipRoot, -20, -20, 400, 240, (p, r) => ClipRingPaint(p, r)); clipRing.pickingMode = PickingMode.Ignore;
            clipCameo = Cameo(clipRoot, 28f); clipCameo.style.position = Position.Absolute; CameoOf(clipCameo, (int)Suborned.Sim.OpId.Enforcer); clipCameo.pickingMode = PickingMode.Ignore;
            clipPlate = E(clipRoot, "s-plate_dark"); clipPlate.style.position = Position.Absolute; clipPlate.style.left = 8; clipPlate.style.height = 26; clipPlate.style.paddingLeft = 10; clipPlate.style.paddingRight = 10; clipPlate.style.justifyContent = Justify.Center; clipPlate.pickingMode = PickingMode.Ignore;
            clipWord = L(clipPlate, "verb-name"); clipWord.style.fontSize = 13.5f; clipWord.style.color = Cream;
            manifest.Add((clipRoot, "image", () => "capture clip"));
            Show(clipRoot, false); Show(clipBlock, false);
        }

        void ClipHud(float W, float H)
        {
            if (clipRoot == null) ClipBuild();
            bool on = host.ClipNow is HandoverHost.ClipState.Playing or HandoverHost.ClipState.Holding or HandoverHost.ClipState.Closing && !m.Ended && !m.BriefingOpen && host.Modal == ModalGate.Modal.None && !host.FailCamOn;
            Show(clipRoot, on); Show(clipBlock, on); if (!on) { clipOutSeen = -1f; return; }
            // the place: hung off the scope; a short panel, right of the scope
            bool scopeOn = scope != null && scope.resolvedStyle.display != DisplayStyle.None && scope.worldBound.height > 1f; float top = scopeOn ? scope.worldBound.yMin : H - 146f;
            float pw = 320f, ph = 180f, x = 20f, y = top - 22f - ph;
            float squadBottom = squadBtn != null && squadBtn.resolvedStyle.display != DisplayStyle.None ? squadBtn.worldBound.yMax : 468f;
            if (y < squadBottom + 8f) { pw = 240f; ph = 135f; x = scopeOn ? scope.worldBound.xMax + 12f : 240f; y = H - 14f - ph; }
            clipRoot.style.left = x; clipRoot.style.top = y; clipRoot.style.width = pw; clipRoot.style.height = ph;
            clipBlock.style.left = x - 14f; clipBlock.style.top = y - 14f; clipBlock.style.width = pw + 28f; clipBlock.style.height = ph + 28f;
            clipRing.style.width = pw + 40f; clipRing.style.height = ph + 40f; clipCameo.style.left = pw - 30f + 6f; clipCameo.style.top = ph - 30f + 6f; clipPlate.style.top = ph - 13f;
            if (host.ClipTexture != null && clipPic.style.backgroundImage.value.renderTexture != host.ClipTexture) clipPic.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(host.ClipTexture));
            Text(clipWord, host.ClipWord);
            // its one 1.05 pop at the start, and again as he comes out
            clipOut = host.ClipNow == HandoverHost.ClipState.Holding || host.ClipOutAt >= 0f; float now = Time.unscaledTime, since = clipOut ? now - host.ClipOutAt : now - host.ClipBorn;
            float pop = since < 0.4f ? 1f + 0.05f * Mathf.Sin(Mathf.Clamp01(since / 0.4f) * Mathf.PI) : 1f; clipRoot.style.scale = new Scale(new Vector3(pop, pop, 1f)); clipRoot.style.opacity = host.ClipAlpha;
            // the chevron: on the side facing the door (or him), on screen or off
            if (host.ClipLookAt() is Vector3 w && host.View)
            {
                var vp = host.View.WorldToViewportPoint(w); var c = new Vector2(x + pw / 2f, y + ph / 2f); var t = vp.z > 0f ? new Vector2(vp.x * W, (1f - vp.y) * H) : c + new Vector2(0.5f - vp.x, vp.y - 0.5f) * 4000f;
                clipAim = t - c; if (clipAim.sqrMagnitude < 1f) clipAim = Vector2.right; clipAim.Normalize();
            }
            clipRing.MarkDirtyRepaint(); clipRoot.BringToFront();
        }

        static void RoundedPath(Painter2D p, Rect r, float rad)
        {
            p.BeginPath(); p.MoveTo(new Vector2(r.xMin + rad, r.yMin)); p.LineTo(new Vector2(r.xMax - rad, r.yMin)); p.ArcTo(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMin + rad), rad); p.LineTo(new Vector2(r.xMax, r.yMax - rad)); p.ArcTo(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax - rad, r.yMax), rad);
            p.LineTo(new Vector2(r.xMin + rad, r.yMax)); p.ArcTo(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMax - rad), rad); p.LineTo(new Vector2(r.xMin, r.yMin + rad)); p.ArcTo(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + rad, r.yMin), rad); p.ClosePath();
        }

        void ClipRingPaint(Painter2D p, Rect full)
        {
            var r = new Rect(20f, 20f, full.width - 40f, full.height - 40f); float t = Time.unscaledTime; var col = clipOut ? TurqLight : Cream;
            if (clipOut) { p.strokeColor = new Color(TurqLight.r, TurqLight.g, TurqLight.b, 0.35f); p.lineWidth = 7f; RoundedPath(p, r, 12f); p.Stroke(); } // the kit's ready glow
            p.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.7f); p.lineWidth = 4f; RoundedPath(p, r, 12f); p.Stroke();
            p.strokeColor = col; p.lineWidth = 2f; RoundedPath(p, r, 12f); p.Stroke();
            if (!clipOut) { p.strokeColor = new Color(Cream.r, Cream.g, Cream.b, 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 1.6f))); var r2 = new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f); RoundedPath(p, r2, 16f); p.Stroke(); }
            // the chevron from the medal's dock, 5 to 12 px outside the rim
            var c = r.center; float k = Mathf.Min(Mathf.Abs(clipAim.x) < 1e-3f ? float.MaxValue : r.width / 2f / Mathf.Abs(clipAim.x), Mathf.Abs(clipAim.y) < 1e-3f ? float.MaxValue : r.height / 2f / Mathf.Abs(clipAim.y)); var rim = c + clipAim * k;
            var tip = rim + clipAim * 12f; var bk = rim + clipAim * 5f; var side = new Vector2(-clipAim.y, clipAim.x) * 6f;
            p.lineCap = LineCap.Round; p.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.7f); p.lineWidth = 4.6f; p.BeginPath(); p.MoveTo(bk + side); p.LineTo(tip); p.LineTo(bk - side); p.Stroke();
            p.strokeColor = Cream; p.lineWidth = 2.6f; p.BeginPath(); p.MoveTo(bk + side); p.LineTo(tip); p.LineTo(bk - side); p.Stroke();
        }
    }
}
