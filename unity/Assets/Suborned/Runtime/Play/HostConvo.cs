using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 032, 085 (GDD v0.35.0 section 10; Glyph's spec, Fable's handover-to-latch.md): the conversation scene. When Listen, Ask, Pay, Help, Pressure, Call or Mislead lands, its whole
    /// conversation plays in a cutaway, its outcome included, the mission clock held: one bubble at a time, each held as long as it takes to read (about 16 characters a second, floor 1 s), the cards
    /// (learned, opens) after the last line, then a hard cut back (0.12 s dip each way) to exactly the view the player left. Click or Space advances, Esc or the Skip keycap ends it. What it plays and
    /// when it doesn't: HostConvo.Lines. Its shots: HostConvo.Shot. Nothing here is an order, a step or a random draw: the clock is held by not stepping, so a replay is identical with scenes Always,
    /// First or Never.</summary>
    public sealed partial class HandoverHost
    {
        public sealed class Convo
        {
            public int Agent; public string Verb = "", Target = "", KeyA = "", KeyB = "", Id = "", MetKey = ""; // KeyA: the near speaker's view (the cell member), KeyB: the person; Id: the conversation's, "" for scenes.items lines
            public readonly List<(string speaker, string text)> Lines = new();
            /// <summary>The cards (scenes.conversations learned, opens), after the last line. InCards: they are up.</summary>
            public readonly List<string> Learned = new(), Opens = new(); public bool InCards; public bool HasCards => Learned.Count + Opens.Count > 0;
            public int Beat; public float BeatAt, Born, EndAt = -1f, OutAt = -1f; public bool Single;
            public Vector3 Look, From, Side; public float Dist;
            /// <summary>Glyph 067 Rev 2 H1, the shot the ladder took: "profile" (the two-shot from the side), "high" (the play pitch, orthographic, the cut and silhouettes on), "intercut" (each speaker
            /// alone, cut with the bubble: a pair more than 6 m apart, and every call), "single" (the two on one point). Ortho: the high shot's half height (m). Cuts: the intercut's shots by speaker;
            /// Cut: the one up now; Framed: 0 KeyA, 1 KeyB, 2 a third speaker. Alone: a call: nobody turns to anybody, each stands as they stood.</summary>
            public string Shot = "", Cut = ""; public float Ortho; public int Framed = -1; public bool Alone;
            internal readonly Dictionary<string, (Vector3 from, Vector3 look)> Cuts = new(); internal readonly HashSet<string> ViewKeys = new();
            internal FeedCard Card;
        }
        /// <summary>The scene playing now (null: none). The HUD draws its bubble, dots, bars and Skip from it.</summary>
        public Convo ConvoNow { get; private set; }
        public bool ConvoOn => ConvoNow != null;
        /// <summary>The 0.12 s dip's black at a hard cut (0 clear, 1 black).</summary>
        public float ConvoDip { get; private set; }
        /// <summary>For checks: how the last scene ended, and whether the view came back bit-identical.</summary>
        public string ConvoLast { get; private set; } = "";
        public int ConvoCount { get; private set; }

        public const float ConvoSingleDist = 2.6f, ConvoDist = 3.6f, ConvoHeight = 1.9f, ConvoPitch = 8f, ConvoFov = 35f, ConvoPush = 0.08f, ConvoDipS = 0.12f, ConvoEndBeatS = 0.4f;
        /// <summary>Each line is held as long as it takes to read (brief 085, GDD section 10): about 16 characters a second, floor 1 s; one 12-word line about 4 s. The cards the same, floor 2.5 s.</summary>
        public const float ConvoReadCps = 16f, ConvoReadMinS = 1f, ConvoCardsMinS = 2.5f;
        public static float ReadS(string line) => Mathf.Max(ConvoReadMinS, (line?.Length ?? 0) / ConvoReadCps);
        /// <summary>Glyph 067 Rev 2 H1: the profile two-shot holds a pair up to 6 m apart, at fit = clamp(1.44 × sep, 3.6, 8) m (then 0.8 × fit, never
        /// under 2.8 m); nobody shows more than 115° of back; nothing solid within 1.6 m of the lens. The high two-shot: the play pitch from 7 m.</summary>
        public const float ConvoTwoShotM = 6f, ConvoFitPerM = 1.44f, ConvoFitMaxM = 8f, ConvoNearM = 2.8f, ConvoBackDeg = 115f, ConvoLensClearM = 1.6f, ConvoHighPitch = 38f, ConvoHighM = 7f;
        /// <summary>Glyph 067 Rev 2 H1.1: the perpendicular, then ±12°, then ±25° round the pair's middle (past 25° one of them turns his back).</summary>
        static readonly int[] ConvoAngles = { 0, 12, -12, 25, -25 };
        /// <summary>For the proofs (H5: "one scene forced into the high two-shot"): "high" takes the high two-shot whatever the ladder finds; "" the ladder.</summary>
        public static string ConvoForceShot = "";
        Convo convoWant;
        // the view before the cut, restored exactly
        Vector3 cvPos; Quaternion cvRot; bool cvOrtho; float cvSize, cvFov, cvNear, cvFar, cvFadeR, cvGhost, cvFeather, cvShadowDepth; Vector3 cvFocus; bool cvCut;
        readonly List<Vector4> cvPoints = new();

        void ResetConvo() { if (ConvoNow != null) ConvoRestore("restart"); ConvoNow = null; convoWant = null; convoMet.Clear(); ConvoPicks.Clear(); ConvoDip = 0f; convoQuietTick = -1; convoQuiet.Clear(); ResetStreet(); }

        /// <summary>Brief 067 H: the scene's two speakers as view keys (the cell member's, the person's), for the figures: both stand idle and face
        /// each other while it plays (no walk or run clip on a held clock, no slide). Null when no scene plays.</summary>
        public (string a, string b)? ConvoViewKeys()
        {
            var c = ConvoNow; if (c == null || views == null || c.OutAt >= 0f && !cvCut) return null;
            return (ConvoViewKey(c.KeyA), ConvoViewKey(c.KeyB));
        }
        string ConvoViewKey(string k) => k switch { "scout" or "fixer" or "tech" or "enforcer" => "op/" + k, "rivals" => views.RivalAnchorKey(), _ => k };
        /// <summary>Brief 085: is this view a speaker in the scene (stands idle), or anyone else in the shot (keeps walking; HostViews.Scene)?</summary>
        public bool ConvoIsSpeaker(string viewKey) => ConvoNow is Convo c && c.ViewKeys.Contains(viewKey);
        static readonly HashSet<string> NoViews = new();
        public HashSet<string> ConvoSpeakerViews() => ConvoNow?.ViewKeys ?? NoViews;

        Transform ConvoView(string key) => views == null ? null : key switch
        {
            "scout" or "fixer" or "tech" or "enforcer" => views.Find("op/" + key),
            "rivals" => views.Find(views.RivalAnchorKey()), // brief 059: the car, else the first member drawn
            _ => views.Find(key),
        };

        /// <summary>The scene chosen at the landing starts on the next frame; false when it fell back to bubbles at the last moment.</summary>
        bool ConvoStart()
        {
            var c = convoWant; convoWant = null;
            string why = ConvoBlock(c);
            if (why != null) { ConvoLast = $"bubbles ({why}): {c.Verb} {c.Target}"; ConvoRelease(c); return false; }
            if (!ConvoFrameShot(c)) { ConvoLast = $"bubbles (no shot): {c.Verb} {c.Target}"; ConvoRelease(c); return false; }
            foreach (var (sp, _) in c.Lines) if (ConvoView(sp) != null) c.ViewKeys.Add(ConvoViewKey(sp));
            if (ConvoView(c.KeyA) != null) c.ViewKeys.Add(ConvoViewKey(c.KeyA)); if (ConvoView(c.KeyB) != null) c.ViewKeys.Add(ConvoViewKey(c.KeyB));
            c.Born = Time.unscaledTime; c.BeatAt = c.Born + ConvoDipS; c.Beat = 0;
            ConvoNow = c; cvCut = false; ConvoCount++;
            ClearHover(); if (Panel != null) ClosePanel(); CloseRing();
            ConvoLast = $"playing: {c.Verb} {c.Target}, {c.Lines.Count} beats, {c.Shot}{(c.Id.Length > 0 ? ", " + c.Id : "")}";
            return true;
        }

        /// <summary>Per frame, before the controls: a wanted scene starts, a playing one runs (its keys, its beats, its camera). True while one
        /// plays (nothing else this frame: no step, no other input).</summary>
        bool ConvoFrame()
        {
            if (ConvoNow == null && convoWant != null && !ConvoStart()) return false;
            var c = ConvoNow; if (c == null) { ConvoDip = 0f; return false; }
            float now = Time.unscaledTime; var kb = Keyboard.current; var mouse = UnityEngine.InputSystem.Mouse.current;
            ClearHover(); ClipHeld(); ConvoCalloutsWait();
            if (c.OutAt < 0f)
            {
                bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;
                bool next = controls.Down(kb, PlayAction.Pause) || mouse != null && mouse.leftButton.wasPressedThisFrame && !OverHud(mouse.position.ReadValue());
                if (esc) ConvoSkip();
                else if (now >= c.BeatAt)
                {
                    float hold = c.InCards ? ConvoCardS(c) : ReadS(c.Beat < c.Lines.Count ? c.Lines[c.Beat].text : "");
                    if (c.EndAt < 0f && (next || now - c.BeatAt >= hold)) ConvoStep(c, now, next);
                    if (c.EndAt >= 0f && now >= c.EndAt) c.OutAt = now;
                }
            }
            // the dips: in, cut at the middle; out, cut back at the middle
            if (c.OutAt >= 0f)
            {
                float t = (now - c.OutAt) / ConvoDipS; ConvoDip = t < 0.5f ? t * 2f : Mathf.Max(0f, 2f - t * 2f);
                if (t >= 0.5f && cvCut) ConvoRestore("ended");
                if (t >= 1f) { ConvoNow = null; ConvoDip = 0f; return false; }
                if (!cvCut) return true; // restored: the play view under the last of the dip
            }
            else
            {
                float t = (now - c.Born) / ConvoDipS; ConvoDip = t < 0.5f ? t * 2f : Mathf.Max(0f, 2f - t * 2f);
                if (t >= 0.5f && !cvCut) ConvoCut();
            }
            if (cvCut)
            {   // the shot's one motion: a slow push in of 8 % over the scene; the intercut cuts to whoever speaks (a hard cut, with the bubble)
                if (c.Shot == "intercut" && c.Beat < c.Lines.Count && !c.InCards && IntercutOn(c, c.Lines[c.Beat].speaker)) { ConvoUnhide(); ConvoHideOccluders(c); }
                float span = 0f; foreach (var (_, tx) in c.Lines) span += ReadS(tx); span = Mathf.Max(1f, span); float k = Mathf.Clamp01((now - c.Born) / span);
                if (c.Shot == "high") cam.orthographicSize = c.Ortho * (1f - ConvoPush * k); // the orthographic lens pushes in by its size
                else { var from = Vector3.Lerp(c.From, c.Look, ConvoPush * k); cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(c.Look - from, Vector3.up)); }
            }
            views.Frame(Paused || Game.Over ? 1f : Mathf.Clamp01(acc / 0.1f)); // the speakers held where the engine has them; everyone else carries on, view only (HostViews.Scene)
            return true;
        }

        /// <summary>For scripted checks: an act has asked for a scene that the next frame decides.</summary>
        public bool ConvoPending => convoWant != null;
        /// <summary>For scripted checks: the scene ends at once, the view restored (no dip).</summary>
        public void ConvoEndNow() { convoWant = null; if (ConvoNow == null) return; ConvoLast = "skipped"; ConvoRestore("ended now"); ConvoNow = null; ConvoDip = 0f; }
        /// <summary>For scripted shots: a person's order as a click gives it (the walk, the ack and the arrive said), not the bare engine order.</summary>
        public void GiveScripted(Order o) { if (Game == null || !IsOpName(o.Op)) return; Give(Op.Parse(o.Op), o, new Target { Kind = IsRole(o.Target) ? "person" : "camera", Key = o.Target, Label = Name(o.Target) }, null); }

        /// <summary>Esc or the Skip keycap: the scene ends now (the dip back).</summary>
        public void ConvoSkip() { var c = ConvoNow; if (c == null || c.OutAt >= 0f) return; c.OutAt = Time.unscaledTime; ConvoLast = "skipped"; }
        /// <summary>A click on the scene (Space too): the next bubble at once; on the last, back.</summary>
        public void ConvoAdvance() { var c = ConvoNow; if (c == null || c.OutAt >= 0f) return; float now = Time.unscaledTime; ConvoStep(c, now, true); if (c.EndAt >= 0f) c.OutAt = now; }
        /// <summary>The next line; after the last, the cards; after those, the end (a short beat when no one clicked).</summary>
        void ConvoStep(Convo c, float now, bool clicked)
        {
            if (!c.InCards && c.Beat < c.Lines.Count - 1) { c.Beat++; c.BeatAt = now; }
            else if (!c.InCards && c.HasCards) { c.InCards = true; c.BeatAt = now; }
            else c.EndAt = now + (clicked ? 0f : ConvoEndBeatS);
        }
        /// <summary>The cards are held as long as they take to read.</summary>
        static float ConvoCardS(Convo c) { float s = 0f; foreach (var l in c.Learned) s += ReadS(l); foreach (var l in c.Opens) s += ReadS(l); return Mathf.Max(ConvoCardsMinS, s); }
        /// <summary>Glyph section 2: a callout's time is real time, so one up when a scene starts would run out unseen: it waits, and comes back with the time it had.</summary>
        void ConvoCalloutsWait() { float dt = Time.unscaledDeltaTime; foreach (var co in Callouts) co.Until += dt; foreach (var co in CalloutQueue) co.Until += dt; }

        void ConvoCut()
        {
            cvPos = cam.transform.position; cvRot = cam.transform.rotation; cvOrtho = cam.orthographic; cvSize = cam.orthographicSize; cvFov = cam.fieldOfView; cvNear = cam.nearClipPlane; cvFar = cam.farClipPlane;
            cvShadowDepth = shadows ? shadows.focusDepth : 0f;
            var c = ConvoNow;
            if (fade) { cvFadeR = fade.radiusPerZoom; cvGhost = fade.ghost; cvFeather = fade.feather; cvFocus = fade.focus; }
            if (c.Shot == "high")
            {   // Glyph 067 Rev 2 H4: the high two-shot keeps the map's occlusion language: an orthographic lens at the play pitch (the cut and the
                // silhouettes draw only for it), standing off as the map does; the cut sized to the two speakers (a line of discs between them and
                // each a clean person's disc), the disc round the focus kept small
                var rot = Quaternion.LookRotation(c.Look - c.From, Vector3.up);
                cam.orthographic = true; cam.orthographicSize = c.Ortho; cam.transform.SetPositionAndRotation(c.Look - rot * Vector3.forward * standOff, rot);
                cam.farClipPlane = Mathf.Max(cvFar, standOff + 1500f); if (shadows) shadows.focusDepth = standOff;
                if (fade)
                {
                    var g0 = new Vector3(c.Look.x, 0f, c.Look.z); fade.focus = g0; fade.radiusPerZoom = 0.02f; fade.ghost = 0f;
                    cvPoints.Clear(); var ta = ConvoView(c.KeyA); var tb = ConvoView(c.KeyB);
                    Vector3 a = ta ? ta.position : g0, b = tb ? tb.position : g0;
                    for (int i = 0; i <= 4; i++) { var q = Vector3.Lerp(a, b, i / 4f); cvPoints.Add(new Vector4(q.x, 0f, q.z, 2.4f)); }
                    cvPoints.Add(new Vector4(a.x, a.y, a.z, -personRadius)); cvPoints.Add(new Vector4(b.x, b.y, b.z, -personRadius));
                    fade.SetPoints(cvPoints);
                }
            }
            else
            {
                if (fade) { fade.radiusPerZoom = 0.02f; fade.ghost = 0f; fade.SetPoints(null); } // the close shot: no ghosted disc round the overhead focus
                cam.orthographic = false; cam.fieldOfView = ConvoFov; cam.nearClipPlane = Mathf.Min(cvNear, 0.1f);
                cam.transform.SetPositionAndRotation(c.From, Quaternion.LookRotation(c.Look - c.From, Vector3.up));
            }
            ConvoHideOccluders(c);
            cvCut = true;
        }

        /// <summary>Back to the view the player left, exactly (map position, yaw and zoom, or the follow view): every value saved at the cut.</summary>
        void ConvoRestore(string why)
        {
            if (!cvCut || !cam) { cvCut = false; return; }
            cam.orthographic = cvOrtho; cam.orthographicSize = cvSize; cam.fieldOfView = cvFov; cam.nearClipPlane = cvNear; cam.farClipPlane = cvFar;
            cam.transform.SetPositionAndRotation(cvPos, cvRot);
            if (shadows) shadows.focusDepth = cvShadowDepth;
            if (fade) { fade.radiusPerZoom = cvFadeR; fade.ghost = cvGhost; fade.feather = cvFeather; fade.focus = cvFocus; fade.SetPoints(null); } // the host's FadePoints sets the play view's points again on the next frame
            ConvoUnhide();
            bool same = cam.transform.position == cvPos && cam.transform.rotation == cvRot && cam.orthographic == cvOrtho && cam.orthographicSize == cvSize && cam.fieldOfView == cvFov;
            ConvoLast = $"{(ConvoLast.StartsWith("skipped") ? "skipped" : why)}; view back {(same ? "bit-identical" : "DIFFERENT")}";
            cvCut = false;
        }

        /// <summary>The scene's framed speakers (view keys): both, the intercut's one up now, or the single shot's person.</summary>
        string[] ConvoFramed(Convo c) => c.Shot == "intercut" ? new[] { c.Cut } : c.Single ? new[] { c.KeyB } : new[] { c.KeyA, c.KeyB };
        /// <summary>Everything the scene hid is drawn again (the cut back, or the intercut's next shot).</summary>
        void ConvoUnhide() { foreach (var r in cvHidden) if (r) r.forceRenderingOff = false; cvHidden.Clear(); }

        /// <summary>Where a scene's speaker stands (their head, world) under the cutaway camera; null for one with no view in the street.</summary>
        public Vector3? ConvoHead(string speaker) => speaker == "milena" ? frame.World(Game.S.At(Game.S.GuardCafe)) + Vector3.up * 2.1f /* brief 085: a voice through the café door, with no figure */ : ConvoView(speaker) is Transform t && t.gameObject.activeInHierarchy ? t.position + Vector3.up * 1.8f : null;
        /// <summary>The pair's feet (for the row of dots under them).</summary>
        public Vector3? ConvoFeet(Convo c)
        {
            var b = ConvoView(c.KeyB); var a = ConvoView(c.KeyA); if (!b) return null;
            if (c.Shot == "intercut") return ConvoView(c.Cut) is Transform cu && cu ? cu.position : b.position; // the dots under whoever is framed
            return c.Single || !a ? b.position : (a.position + b.position) / 2f;
        }
    }
}
