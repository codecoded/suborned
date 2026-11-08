using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>Brief 101 step 6, the hover label in the street (Glyph's S mocks, briefs/101/glyph/r2/S-aim.png; moved here from HudToolkit.ActMark): what a click or a
    /// hold on the thing under the pointer does, laid over it as street labels, with no plate, no chrome and no tail: the act's disc (the street tab's deep body and cream
    /// ring; harm reads by shape, the disc light with an ink glyph; a shot is the crosshair), the head word ("Hold", "Right hold", "Let go", "to Inez") in soft ink,
    /// the words in the book's ink on a cream keyline, the doers' faces, "Watched spot" in urgent orange, the prediction mark. The hold fills a turquoise ring round the
    /// disc (a hold let go early drains it); full, it says "Let go" in deep teal and pops a little. A shot that would go loud carries one row under it: Fable's
    /// "Goes loud" on an orange pill, the Alert lamps it reaches, the police badge and the seal's lock when that brings them, the Trail; never a hit chance or a time.
    /// The words (PillWords) stay the whole line the proofs read.</summary>
    public sealed partial class HudToolkit
    {
        VisualElement vpillRing, vpillCross, loudRow, loudLamps, loudPolice, loudLockIc, loudTrailIc; KeyText vpillHeadK, vpillDotK, vpillTextK, vpillLockDotK, vpillLockK, loudTrail; Label loudWord;
        static readonly string[] PillHeads = { "Let go", "Right hold", "Hold" };
        /// <summary>The prediction mark for the next PillLook (set by its caller from the intent: Intent.PredMark).</summary>
        string pillPredNow = "";
        /// <summary>Brief 101 step 6: the fire offer whose price the next PillLook lays out as the "Goes loud" row (HoldPillFrame sets it), null for any other pill.</summary>
        HandoverHost.Intent pillFire;
        /// <summary>The proof reads it: the first shot's row now ("Goes loud:2:police:seal:+2/hit"), "" hidden.</summary>
        public static string LoudRowShown { get; private set; } = "";
        const float PillDisc = 26f, PillRingR = 17f;

        void BuildPill()
        {
            StreetSheet();
            vpill = E(marks, null, 0, 0, float.NaN, PillH); vpill.style.flexDirection = FlexDirection.Row; vpill.style.alignItems = Align.Center; vpill.pickingMode = PickingMode.Ignore;
            vpill.style.overflow = Overflow.Visible; vpill.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100)); vpill.style.paddingLeft = 4; vpill.style.paddingRight = 4;
            // the act's disc and the hold's ring round it; a shot's crosshair in its place
            var dw = E(vpill, null, w: PillDisc, h: PillDisc); dw.style.flexShrink = 0; dw.style.overflow = Overflow.Visible;
            vpillRing = P(dw, PillDisc / 2f - 24f, PillDisc / 2f - 24f, 48f, 48f, (p, r) => { if (SweepShownNow < 0f && !vpillReady) return; var c = r.center; RingArc(p, c, PillRingR, 1f, new Color(BkInk.r, BkInk.g, BkInk.b, 0.45f), 5.4f); RingArc(p, c, PillRingR, vpillReady ? 1f : SweepShownNow, BkTurq, 3.4f); });
            vpillDisc = E(dw, "e-body e-inline", 0, 0, PillDisc, PillDisc); Round(vpillDisc, PillDisc / 2f); BorderW(vpillDisc, 2f); var gl = E(vpillDisc, null, (PillDisc - 18f) / 2f, (PillDisc - 18f) / 2f, 14, 14); gl.name = "glyph";
            vpillCross = P(dw, 0, 0, PillDisc, PillDisc, (p, r) => KeyedCross(p, r.center, 9f)); Show(vpillCross, false);
            vpillHeadK = KText(vpill, 15f, "k-semi", "k-soft"); vpillHeadK.Root.style.marginLeft = 9; vpillHead = vpillHeadK.Face;
            vpillDotK = KText(vpill, 15f, "k-semi", "k-soft"); vpillDotK.Set("·"); vpillDotK.Root.style.marginLeft = 5; vpillDotK.Root.style.marginRight = 5; vpillDot = vpillDotK.Face;
            vpillTextK = KText(vpill, 15.5f); vpillText = vpillTextK.Face;
            // brief 067 E3 (Glyph): the locked pill's tail: a dot, then the short lock in soft ink
            vpillLockDotK = KText(vpill, 15f, "k-semi", "k-soft"); vpillLockDotK.Set("·"); vpillLockDotK.Root.style.marginLeft = 5; vpillLockDotK.Root.style.marginRight = 5; Show(vpillLockDotK.Root, false);
            vpillLockK = KText(vpill, 15f, "k-semi", "k-soft"); Show(vpillLockK.Root, false);
            BuildPillPred(); // engine 0.28: the likely / worse plate after the price, before the faces (hud29 predictionMarks)
            vpillFaces = E(vpill); vpillFaces.style.flexDirection = FlexDirection.Row; vpillFaces.style.marginLeft = 10; vpillFaces.style.flexShrink = 0; vpillFaces.style.overflow = Overflow.Visible;
            for (int i = 0; i < 4; i++) { var f = Cameo(vpillFaces, FaceSize); f.style.marginLeft = i == 0 ? 0 : -7; faceEls.Add(f); }
            // "Watched spot" (Fable row 8): the stand spot is in a known cone: the eye and the words in urgent orange on an ink keyline
            vpillSeen = E(vpill); vpillSeen.style.flexDirection = FlexDirection.Row; vpillSeen.style.alignItems = Align.Center; vpillSeen.style.flexShrink = 0; vpillSeen.style.marginLeft = 10;
            KIcon(vpillSeen, "sawyou", 16f, "ki-hot", "ki-key-ink", 1.7f); var sw = KText(vpillSeen, 14f, "k-semi", "k-hot", "k-key-ink", KeyHotW); sw.Set("Watched spot"); sw.Root.style.marginLeft = 5;
            // Glyph 2026-10-26 §3: a Drive to the yard before Vale's car is parked: Vale's cameo and the next-event mark (no words)
            vpillAfter = E(vpill); vpillAfter.style.flexDirection = FlexDirection.Row; vpillAfter.style.alignItems = Align.Center; vpillAfter.style.flexShrink = 0; vpillAfter.style.marginLeft = 10;
            { var vc = Cameo(vpillAfter, 14f, 2f); Bg(vc.Q("face"), "badges/npc_councillor", Color.white); KIcon(vpillAfter, "nextevent", 16f, "ki-deep").Root.style.marginLeft = 4; }
            Show(vpillAfter, false);
            vpillTail = P(marks, 0, 0, TailW, TailH, (p, r) => { }); // no tail in the street: the label stands over its thing (kept for the shared placement)
            // the lit pip of an act that just opened (brief 043): a cream sparkle on the faces' top-right
            vpillPip = P(vpillFaces, 0, -5, 18, 18, (p, r) => Sparkle(p, r.center, 7.5f, Cream)); vpillPip.style.left = StyleKeyword.Auto; vpillPip.style.right = -5; Show(vpillPip, false);
            manifest.Add((vpill, "line", () => vpillLine));
            BuildLoudRow();
            Show(vpill, false); Show(vpillTail, false);
        }

        /// <summary>The first shot's row (Glyph's S-aim price_row): "Goes loud" in page cream on an orange pill, the Alert's icon and lamps, the police badge, the seal's
        /// lock, the Trail; under the label, left with it.</summary>
        void BuildLoudRow()
        {
            loudRow = E(marks); loudRow.style.position = Position.Absolute; loudRow.style.flexDirection = FlexDirection.Row; loudRow.style.alignItems = Align.Center; loudRow.style.height = 28; loudRow.pickingMode = PickingMode.Ignore;
            var pill = E(loudRow, "e-orange"); pill.style.height = 26; pill.style.flexShrink = 0; Round(pill, 13f); pill.style.paddingLeft = 11; pill.style.paddingRight = 11; pill.style.justifyContent = Justify.Center; pill.name = "loud-pill";
            loudWord = KLabel(pill, "k-bold", "k-page", 15f); // page cream on the orange pill: no keyline needed
            KIcon(loudRow, "p_alert", 20f, "ki-hot").Root.style.marginLeft = 12;
            loudLamps = E(loudRow); loudLamps.style.flexDirection = FlexDirection.Row; loudLamps.style.marginLeft = 5; loudLamps.style.alignItems = Align.Center;
            for (int i = 0; i < 3; i++) { var l = E(loudLamps, null, w: 15.2f, h: 15.2f); l.style.flexShrink = 0; l.style.marginLeft = i == 0 ? 0 : 0.8f; Round(l, 7.6f); Border(l, 1.6f, BkInk); }
            loudPolice = E(loudRow, "e-page", w: 24, h: 24); loudPolice.style.flexShrink = 0; loudPolice.style.marginLeft = 12; Round(loudPolice, 12f); Border(loudPolice, 1.5f, BkInk); Bg(E(loudPolice, null, 2, 2, 19, 19), "badges/signal_police", Color.white);
            loudLockIc = KIcon(loudRow, "lock", 20f).Root; loudLockIc.style.marginLeft = 12;
            loudTrailIc = KIcon(loudRow, "p_trail", 20f).Root; loudTrailIc.style.marginLeft = 12; loudTrail = KText(loudRow, 17f); loudTrail.Root.style.marginLeft = 4;
            manifest.Add((loudRow, "marker", () => loudWord.text)); Show(loudRow, false);
        }

        /// <summary>The act's disc in the street: the deep body, a cream ring and a cream glyph; harm the page cream body, an ink ring and an ink glyph; a shot the crosshair.</summary>
        void PillDiscOf(string icon, bool harm)
        {
            bool cross = icon == "fire"; Show(vpillCross, cross); Show(vpillDisc, !cross && icon.Length > 0); if (cross || icon.Length == 0) return;
            Class(vpillDisc, "e-body", !harm); Class(vpillDisc, "e-page", harm); RingColour(vpillDisc, harm ? BkInk : null); Icon(vpillDisc.Q("glyph"), icon, harm ? BkInk : Cream);
        }

        /// <summary>The pill's look for one frame (both presets): the words (the head word split off in soft ink, "Let go" in deep teal), the act's disc, the faces, "Watched
        /// spot", the hold's ring and the "Let go" pop, and the first shot's row.</summary>
        void PillLook(string text, string icon, bool harm, IReadOnlyList<int> faces, bool seen, float fill, string state)
        {
            bool ready = state == "ready";
            string lockShort = ""; int lsAt = text.IndexOf('\u0001'); if (lsAt >= 0) { lockShort = text[(lsAt + 1)..]; text = text[..lsAt]; } // brief 067 E3: the locked pill's short lock, drawn soft after a dot
            var row = LoudRowOf(pillFire); string rowKey = row.on ? $"{row.alert}:{row.police}:{row.seal}:{row.trail}" : "";
            string key = text + "|" + lockShort + "|" + icon + "|" + harm + "|" + seen + "|" + (faces == null ? "" : string.Join(",", faces)) + "|" + ready + "|" + pillPredNow + "|" + PredForce + "|" + pillAfterNow + "|" + rowKey;
            if (key != vpillKey)
            {
                vpillKey = key; vpillLine = text; string head = "", rest = text;
                foreach (var hw in PillHeads) if (text.StartsWith(hw + " · ")) { head = hw; rest = text[(hw.Length + 3)..]; break; }
                if (head.Length == 0 && text.StartsWith("to ") && text.IndexOf(" · ", System.StringComparison.Ordinal) is int wi and > 3) { head = text[..wi]; rest = text[(wi + 3)..]; } // review 4: the walk-in's "to Inez" is a head word, soft like "Hold"
                if (row.on) { int cut = rest.IndexOf(" · ", System.StringComparison.Ordinal); if (cut > 0) rest = rest[..cut]; } // the row says the price: the label keeps the act ("Fire")
                vpillHeadK.Set(head); Show(vpillHeadK.Root, head.Length > 0); Show(vpillDotK.Root, head.Length > 0); vpillTextK.Set(rest); vpillTextK.Root.style.marginLeft = head.Length > 0 ? 0 : 9;
                Show(vpillLockDotK.Root, lockShort.Length > 0); Show(vpillLockK.Root, lockShort.Length > 0); vpillLockK.Set(lockShort); if (lockShort.Length > 0) vpillLine = text + " · " + lockShort;
                vpillHeadK.Look(ready ? "k-deep" : "k-soft", "k-key");
                PillDiscOf(icon, harm);
                int n = faces?.Count ?? 0; Show(vpillFaces, n > 0);
                for (int i = 0; i < 4; i++) { bool on = i < n; Show(faceEls[i], on); if (on) CameoOf(faceEls[i], faces[i]); }
                Show(vpillSeen, seen); PillPred(harm ? "" : pillPredNow); Show(vpillAfter, pillAfterNow && !seen);
                LoudRowLook(row);
                vpillReady = ready; vpillRing.MarkDirtyRepaint();
            }
            // the hold's ring; a hold let go early (charge, then no charge, never ready) drains back when hold.cancel is "drain"
            float now = Time.unscaledTime, sw = fill >= 0f ? HoldSweep(fill) : -1f;
            if (sw < 0f && sweepState == "charge" && sweepShown > 0f && Tok.hold.cancel == "drain") { drainFrom = sweepShown; drainAt = now; }
            if (sw >= 0f) drainFrom = -1f;
            float shown = sw;
            if (sw < 0f && drainFrom > 0f) { float t = (now - drainAt) / Mathf.Max(0.05f, DrainS * drainFrom); if (t < 1f) shown = drainFrom * (1f - t); else drainFrom = -1f; }
            sweepShown = sw; sweepState = sw >= 0f ? state : "";
            if (SweepShownNow != shown || ready) vpillRing.MarkDirtyRepaint(); SweepShownNow = shown;
        }

        /// <summary>What a shot would set off, from its price (HudModel.PriceLine): the Alert it reaches (police come and the district seals at 2, the seal only if not yet
        /// running), the Trail. Off when the shot costs nothing new (a fight already loud) or there is no shot.</summary>
        (bool on, int alert, bool police, bool seal, string trail) LoudRowOf(HandoverHost.Intent i)
        {
            if (i == null || host.Game == null) return default;
            int alert = 0; string trail = "";
            foreach (var f in HudModel.PriceLine(host, i)) { if (f.Kind == "alert") foreach (var ch in f.Text) if (char.IsDigit(ch)) alert = ch - '0'; if (f.Kind == "trail") trail = f.Text; }
            if (alert <= host.Game.Alert) alert = 0; // already there: the shot raises nothing
            if (alert == 0) return default;
            return (true, alert, alert >= 2, alert >= 2 && !host.SealOn, trail);
        }

        void LoudRowLook((bool on, int alert, bool police, bool seal, string trail) r)
        {
            string word = host.Text?.StreetLoud ?? ""; Show(loudRow, r.on); LoudRowShown = r.on ? $"{word}:{r.alert}:{(r.police ? "police" : "")}:{(r.seal ? "seal" : "")}:{r.trail}" : ""; if (!r.on) return;
            Text(loudWord, word); Show(loudRow.Q("loud-pill"), word.Length > 0); // Fable's word, or no pill (never the host's own)
            for (int i = 0; i < 3; i++) { var l = loudLamps[i]; var c = i < r.alert ? BkOrange : StPage; if (l.style.backgroundColor.value != c) l.style.backgroundColor = c; }
            Show(loudPolice, r.police); Show(loudLockIc, r.seal); Show(loudTrailIc, r.trail.Length > 0); Show(loudTrail.Root, r.trail.Length > 0); loudTrail.Set(r.trail);
        }

        /// <summary>Places the pill above the screen point q (centred, clamped to the screen), and the first shot's row under it.</summary>
        float PillPlace(Vector2 q, out float x)
        {
            float pw = float.IsNaN(vpill.resolvedStyle.width) ? 300f : vpill.resolvedStyle.width, W = root.layout.width > 0 ? root.layout.width : 1920f;
            x = Mathf.Clamp(q.x - pw / 2f, 14f, Mathf.Max(14f, W - pw - 14f)); float top = q.y - 10f - PillH - 11f;
            top = ClearOfSpeech(SpeechRects(), x, pw, top + PillH, PillH) - PillH; // brief 044: above the thing's speech and its leader, never over it
            vpill.style.left = x; vpill.style.top = top; vpillTail.style.left = q.x - TailW / 2f; vpillTail.style.top = top + PillH - 4f;
            vpill.BringToFront(); vpillTail.BringToFront();
            if (loudRow.style.display != DisplayStyle.None) { loudRow.style.left = x + 4f; loudRow.style.top = top + PillH + 2f; loudRow.style.opacity = vpill.style.opacity; loudRow.BringToFront(); }
            return pw;
        }
    }
}
