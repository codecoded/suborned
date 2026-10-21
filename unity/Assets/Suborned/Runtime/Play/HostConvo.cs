using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 032 (Sky 2026-10-21, timing B; Glyph's spec): the conversation scene. When an agent begins Listen, Ask, Pay, Help,
    /// Pressure, Call or Mislead (the act's first tick), its opening lines play as a close side two-shot of the two people, the mission clock
    /// held: at most 3 beats (the cell's arrive and the person's first line; Listen: the Scout's arrive and the overheard pair's first two
    /// beats; Call: the call scene's ack and confirm; Mislead: the Scout's ack and arrive), one bubble at a time up to bubbleSeconds each, a
    /// 0.4 s beat after the last, then a hard cut back (0.12 s dip each way) to exactly the view the player left. Click or Space advances, Esc
    /// or the Skip keycap ends it. No reply, done, result or read line is ever in it: they play at landing as bubbles, as before.
    /// When it does not play: Never set; First time and the person already had one; a fight on or Alert 2+; the run over; either person down,
    /// inside or out of view; another scene playing or waiting; a freeze or target mode; the menu or the briefing. It falls back to bubbles.
    /// Nothing here is an order, a step or a random draw: the lines are the ones the host already picked (the act card's scene, the chatter's
    /// ack and arrive), the clock is held by not stepping, so a replay is identical with scenes Always, First or Never.</summary>
    public sealed partial class HandoverHost
    {
        public sealed class Convo
        {
            public int Agent; public string Verb = "", Target = "", KeyA = "", KeyB = ""; // KeyA: the near speaker's view (the cell member), KeyB: the person
            public readonly List<(string speaker, string text)> Lines = new();
            public int Beat; public float BeatAt, Born, EndAt = -1f, OutAt = -1f; public bool Single;
            public Vector3 Look, From, Side; public float Dist;
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

        public const float ConvoSingleM = 3f, ConvoSingleDist = 2.6f, ConvoDist = 3.6f, ConvoHeight = 1.9f, ConvoPitch = 8f, ConvoFov = 35f, ConvoPush = 0.08f, ConvoDipS = 0.12f, ConvoEndBeatS = 0.4f;

        readonly HashSet<string> convoMet = new(StringComparer.Ordinal);
        (int k, string verb, string target, FeedCard card, int tick)? convoWant;
        readonly (int tick, string text)[] lastAck = new (int, string)[4], lastArrive = new (int, string)[4];
        // the view before the cut, restored exactly
        Vector3 cvPos; Quaternion cvRot; bool cvOrtho; float cvSize, cvFov, cvNear, cvFar, cvFadeR, cvGhost, cvFeather; bool cvCut;

        void ResetConvo() { if (ConvoNow != null) ConvoRestore("restart"); ConvoNow = null; convoWant = null; convoMet.Clear(); Array.Clear(lastAck, 0, 4); Array.Clear(lastArrive, 0, 4); ConvoDip = 0f; }

        /// <summary>The chatter's ack and arrive as said (the scene shows the same lines, never a new pick).</summary>
        void ConvoHeard(int k, bool arrive)
        {
            if (k < 0 || k > 3) return;
            for (int i = Talk.Count - 1; i >= 0 && Talk[i].Tick == Game.Tick; i--) if (Talk[i].Agent == k) { if (arrive) lastArrive[k] = (Game.Tick, Talk[i].Text); else lastAck[k] = (Game.Tick, Talk[i].Text); return; }
        }

        /// <summary>An act began (FeedTrack's card, FeedCall): a scene is wanted; it is decided at the start of the next frame, once the act's
        /// arrive has been said.</summary>
        void ConvoAct(int k, string verb, string target, FeedCard card)
        {
            if (controls.conversations == ConversationScenes.Never || ConvoNow != null || convoWant != null) return; // a second act at the same time: bubbles
            if (verb is not ("listen" or "ask" or "pay" or "help" or "pressure" or "call" or "mislead")) return;
            convoWant = (k, verb, target, card, Game.Tick);
        }

        /// <summary>Why a scene can't play now (null: it can).</summary>
        string ConvoBlock(int k, string target)
        {
            var g = Game;
            if (g == null || g.Over || Ending.Length > 0 || Fault != null) return "over";
            if (MenuOpen || ShowBriefing || FailCamOn) return "modal";
            if (Frozen || TargetFreeze) return "freeze"; // a freeze, or target mode (Y)
            if (g.FightOn || g.Alert >= 2) return "fight";
            if (controls.conversations == ConversationScenes.FirstTime && convoMet.Contains(target)) return "met";
            var op = g.Ops[k]; if (op.Down || op.Inside) return "agent";
            if (target.Length == 0 || ConvoView(target) is not Transform t || !t.gameObject.activeInHierarchy) return "unseen";
            if (CastOf(target) is Person p && p.Down) return "person";
            return null;
        }

        Transform ConvoView(string key) => views == null ? null : key switch
        {
            "scout" or "fixer" or "tech" or "enforcer" => views.Find("op/" + key),
            "rivals" => views.Find("rivals/0"),
            _ => views.Find(key),
        };

        /// <summary>The wanted scene, built from the lines already picked; false when it falls back to bubbles.</summary>
        bool ConvoStart()
        {
            var w = convoWant.Value; convoWant = null;
            string why = ConvoBlock(w.k, w.target); if (why != null) { ConvoLast = $"bubbles ({why}): {w.verb} {w.target}"; return false; }
            var c = new Convo { Agent = w.k, Verb = w.verb, Target = w.target, Card = w.card, KeyA = Op.Name((OpId)w.k), KeyB = w.target };
            string Arrive() // the arrive the chatter said at the act's start; after a short walk it says none (Fable §4), so the scene peeks the one pickLine would give
            {
                if (lastArrive[w.k].tick == w.tick && !string.IsNullOrEmpty(lastArrive[w.k].text)) return lastArrive[w.k].text;
                var line = Text?.PeekLine(new PlayText.Query { Event = "arrive", Verb = w.verb, Speaker = Op.Name((OpId)w.k) });
                return string.IsNullOrEmpty(line) ? null : Fill(line, Name(w.target), 0, 0, AgentNames[w.k]);
            }
            string Ack() => !string.IsNullOrEmpty(lastAck[w.k].text) ? lastAck[w.k].text : null;
            var lines = w.card?.Lines;
            switch (w.verb)
            {
                case "ask": case "pay": case "help": case "pressure":
                    {   // the approach scene up to the reply: the Fixer's arrive, the person's first line, the Fixer's ask
                        bool arrives = false; if (w.card?.Scene != null) foreach (var v in w.card.Scene.Variants) foreach (var b in v) if (b.Ref == "arrive") arrives = true;
                        if (!arrives || lines == null) break;
                        foreach (var l in lines) { if (l.Reply || c.Lines.Count >= 3) break; if (l.Text.Length > 0) c.Lines.Add((l.Speaker, l.Text)); }
                        break;
                    }
                case "listen":
                    {   // the Scout's arrive, then the overheard pair's first two beats (never the Scout's read)
                        var ar = Arrive(); if (ar == null || lines == null) break;
                        c.Lines.Add((Op.Name((OpId)w.k), ar));
                        for (int i = 0; i < lines.Count && c.Lines.Count < 3; i++) if (!lines[i].Reply && lines[i].Text.Length > 0) c.Lines.Add((lines[i].Speaker, lines[i].Text));
                        // the two speakers of the overheard scene are framed when the other one stands in the street near her; else the target and the Scout
                        foreach (var l in lines) if (l.Speaker != w.target && IsRole(l.Speaker) && ConvoView(l.Speaker) is Transform os && os.gameObject.activeInHierarchy && ConvoView(w.target) is Transform ts && Vector3.Distance(os.position, ts.position) < 6f) { c.KeyA = l.Speaker; break; }
                        break;
                    }
                case "call":
                    {   // the call scene's opening: the Fixer's ack and the contact's confirm (the call's exchange has no arrive)
                        if (lines == null) break;
                        for (int i = 0; i < lines.Count && c.Lines.Count < 2; i++) if (lines[i].Text.Length > 0) c.Lines.Add((lines[i].Speaker, lines[i].Text));
                        break;
                    }
                case "mislead":
                    {
                        var ar = Arrive(); if (ar == null) break;
                        var ak = Ack(); if (ak != null) c.Lines.Add((Op.Name((OpId)w.k), ak));
                        c.Lines.Add((Op.Name((OpId)w.k), ar));
                        break;
                    }
            }
            if (c.Lines.Count == 0) { ConvoLast = $"bubbles (no arrive): {w.verb} {w.target}"; return false; }
            if (!ConvoFrameShot(c)) { ConvoLast = $"bubbles (no shot): {w.verb} {w.target}"; return false; }
            // the scene's lines are not bubbled again when the act's progress reveals them (view only)
            if (w.card != null && w.verb is not "mislead") w.card.Bubbled = Math.Max(w.card.Bubbled, Math.Min(w.card.Lines.Count, w.verb == "listen" ? c.Lines.Count - 1 : c.Lines.Count));
            convoMet.Add(w.target);
            c.Born = Time.unscaledTime; c.BeatAt = c.Born + ConvoDipS; c.Beat = 0;
            ConvoNow = c; cvCut = false; ConvoCount++;
            ClearHover(); if (Panel != null) ClosePanel(); CloseRing();
            ConvoLast = $"playing: {w.verb} {w.target}, {c.Lines.Count} beats{(c.Single ? ", single" : "")}";
            return true;
        }

        /// <summary>Where the camera stands: on the perpendicular bisector of the pair, 3.6 m out (further when they stand apart), 1.9 m up, 8°
        /// down, on the side the person faces and the boom is clear; else over the cell member's shoulder. A pair more than 3 m apart (a call,
        /// the Fixer across the terrace) frames the person alone, centred (Latch 2026-10-21: at 4 to 6 m apart the two-shot put Inez at the
        /// frame's edge behind a palm), from the clear side nearest the way they face.</summary>
        bool ConvoFrameShot(Convo c)
        {
            if (!cam) return false;
            var tb = ConvoView(c.KeyB); var ta = ConvoView(c.KeyA); if (!tb) return false;
            Vector3 b = tb.position, a = ta && ta.gameObject.activeInHierarchy ? ta.position : b;
            var ab = b - a; ab.y = 0f; float sep = ab.magnitude;
            Vector3 face = tb.forward; face.y = 0f; if (face.sqrMagnitude < 1e-4f) face = Vector3.forward; face.Normalize();
            if (sep > ConvoSingleM || sep < 0.2f)
            {   // the person alone, from where they face (else the nearest clear bearing to it, 30° steps; none clear: from where they face)
                c.Single = true; c.Look = b + Vector3.up * 1.45f; c.Dist = ConvoSingleDist; c.Side = face;
                var bearings = new[] { 0, 30, -30, 60, -60, 90, -90, 120, -120, 150, -150, 180 }; bool found = false;
                for (int pass = 0; pass < 2 && !found; pass++) // a clear line first; else at least a camera outside every mass
                    foreach (int deg in bearings)
                    {
                        var s = Quaternion.Euler(0f, deg, 0f) * face; var from = c.Look + s * c.Dist; from.y = b.y + ConvoHeight;
                        if (pass == 0 ? !ConvoClear(c.Look, from) : !ConvoClear(from, from)) continue;
                        c.Side = s; found = true; break;
                    }
                c.From = c.Look + c.Side * c.Dist; c.From.y = b.y + ConvoHeight; return true;
            }
            var mid = (a + b) / 2f; var dir = ab / sep; var perp = new Vector3(-dir.z, 0f, dir.x);
            float dist = Mathf.Clamp(Mathf.Max(ConvoDist, sep / 1.1f / Mathf.Tan(Mathf.Atan(Mathf.Tan(ConvoFov * 0.5f * Mathf.Deg2Rad) * 16f / 9f))), ConvoDist, 6f);
            var sides = Vector3.Dot(perp, face) >= 0f ? new[] { perp, -perp } : new[] { -perp, perp }; // the person's face toward the camera first
            foreach (var s in sides)
            {
                var from = mid + s * dist; from.y = mid.y + ConvoHeight;
                if (!ConvoClear(mid + Vector3.up * 1.6f, from)) continue;
                c.Side = s; c.Dist = dist; c.From = from; c.Look = from - s * dist + Vector3.down * Mathf.Tan(ConvoPitch * Mathf.Deg2Rad) * dist; return true;
            }
            // neither side clear: over the shoulder behind the cell member, 2.2 m back, 0.5 m to the side, 1.9 m high
            var right = new Vector3(dir.z, 0f, -dir.x);
            c.From = a - dir * 2.2f + right * 0.5f; c.From.y = a.y + ConvoHeight; c.Look = b + Vector3.up * 1.5f; c.Side = -dir; c.Dist = Vector3.Distance(c.From, c.Look); return true;
        }

        /// <summary>The boom's line from the pair's heads to the camera keeps 0.4 m clear of every wall (HostFollow's index of the street's solids).</summary>
        bool ConvoClear(Vector3 from, Vector3 to)
        {
            FollowIndex(); var ray = new Ray(from, (to - from).normalized); float len = Vector3.Distance(from, to);
            var seen = new HashSet<Renderer>();
            for (float t = 0f; t <= len + 4f; t += 4f)
            {
                var q = ray.GetPoint(Mathf.Min(t, len));
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                {
                    var (cx, cz) = FCell(q.x, q.z); if (!fIndex.TryGetValue((cx + dx, cz + dz), out var l)) continue;
                    foreach (var (r, bb, prop) in l)
                    {
                        if (prop || !seen.Add(r) || !r || !r.enabled) continue; var e = bb; e.Expand(0.8f); // 0.4 m each side
                        if (e.Contains(to)) return false; // the camera inside a mass (Latch 2026-10-21: Inez at the café door was shot from inside the café, through its door frame)
                        if (e.Contains(from)) continue; // the pair under an awning: not between them and the camera
                        if (e.IntersectRay(ray, out var hit) && hit <= len) return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Per frame, before the controls: a wanted scene starts, a playing one runs (its keys, its beats, its camera). True while one
        /// plays (nothing else this frame: no step, no other input).</summary>
        bool ConvoFrame()
        {
            if (ConvoNow == null && convoWant != null && !ConvoStart()) return false;
            var c = ConvoNow; if (c == null) { ConvoDip = 0f; return false; }
            float now = Time.unscaledTime; var kb = Keyboard.current; var mouse = UnityEngine.InputSystem.Mouse.current;
            ClearHover();
            if (c.OutAt < 0f)
            {
                bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;
                bool next = controls.Down(kb, PlayAction.Pause) || mouse != null && mouse.leftButton.wasPressedThisFrame && !OverHud(mouse.position.ReadValue());
                float hold = Text?.BubbleS ?? 3f;
                if (esc) ConvoSkip();
                else if (now >= c.BeatAt)
                {
                    if (c.EndAt < 0f && (next || now - c.BeatAt >= hold)) { if (c.Beat < c.Lines.Count - 1) { c.Beat++; c.BeatAt = now; } else c.EndAt = now + (next ? 0f : ConvoEndBeatS); }
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
            {   // the shot's one motion: a slow push in of 8 % over the scene
                float span = Mathf.Max(1f, c.Lines.Count * (Text?.BubbleS ?? 3f)), k = Mathf.Clamp01((now - c.Born) / span);
                var from = Vector3.Lerp(c.From, c.Look, ConvoPush * k);
                cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(c.Look - from, Vector3.up));
            }
            views.Frame(Paused || Game.Over ? 1f : Mathf.Clamp01(acc / 0.1f)); // people held where the engine has them; their idles play
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
        public void ConvoAdvance() { var c = ConvoNow; if (c == null || c.OutAt >= 0f) return; if (c.Beat < c.Lines.Count - 1) { c.Beat++; c.BeatAt = Time.unscaledTime; } else c.OutAt = Time.unscaledTime; }

        void ConvoCut()
        {
            cvPos = cam.transform.position; cvRot = cam.transform.rotation; cvOrtho = cam.orthographic; cvSize = cam.orthographicSize; cvFov = cam.fieldOfView; cvNear = cam.nearClipPlane; cvFar = cam.farClipPlane;
            if (fade) { cvFadeR = fade.radiusPerZoom; cvGhost = fade.ghost; cvFeather = fade.feather; fade.radiusPerZoom = 0.02f; fade.ghost = 0f; fade.SetPoints(null); } // the close shot: no ghosted disc round the overhead focus
            cam.orthographic = false; cam.fieldOfView = ConvoFov; cam.nearClipPlane = Mathf.Min(cvNear, 0.1f);
            var c = ConvoNow; cam.transform.SetPositionAndRotation(c.From, Quaternion.LookRotation(c.Look - c.From, Vector3.up));
            ConvoHideOccluders(c);
            cvCut = true;
        }

        /// <summary>Back to the view the player left, exactly (map position, yaw and zoom, or the follow view): every value saved at the cut.</summary>
        void ConvoRestore(string why)
        {
            if (!cvCut || !cam) { cvCut = false; return; }
            cam.orthographic = cvOrtho; cam.orthographicSize = cvSize; cam.fieldOfView = cvFov; cam.nearClipPlane = cvNear; cam.farClipPlane = cvFar;
            cam.transform.SetPositionAndRotation(cvPos, cvRot);
            if (fade) { fade.radiusPerZoom = cvFadeR; fade.ghost = cvGhost; fade.feather = cvFeather; }
            foreach (var r in cvHidden) if (r) r.forceRenderingOff = false; cvHidden.Clear();
            bool same = cam.transform.position == cvPos && cam.transform.rotation == cvRot && cam.orthographic == cvOrtho && cam.orthographicSize == cvSize && cam.fieldOfView == cvFov;
            ConvoLast = $"{(ConvoLast.StartsWith("skipped") ? "skipped" : why)}; view back {(same ? "bit-identical" : "DIFFERENT")}";
            cvCut = false;
        }

        readonly List<Renderer> cvHidden = new();
        /// <summary>What would stand in the shot: street props (a planter's palm) on the line from the camera to either person's head or chest
        /// (1.5 m and 1.1 m: a potted palm's fronds hid Inez's body under a clear head line) or within 1.6 m of the camera, and people (passers-by, the cell) standing in those lines or within 1.6 m of the camera, are not drawn while the
        /// scene plays (forceRenderingOff; back at the cut back). A low solid (a planter, a tree bed: under 4 m tall, 6 m wide) on those lines goes too; facades are never hidden (the camera picked a side clear of them).</summary>
        void ConvoHideOccluders(Convo c)
        {
            cvHidden.Clear(); var from = c.From;
            var heads = new List<Vector3>(); foreach (var k in c.Single ? new[] { c.KeyB } : new[] { c.KeyA, c.KeyB }) if (ConvoView(k) is Transform t && t.gameObject.activeInHierarchy) { heads.Add(t.position + Vector3.up * 1.5f); heads.Add(t.position + Vector3.up * 1.1f); }
            bool OnLine(Bounds b) { foreach (var h in heads) { var ray = new Ray(from, (h - from).normalized); float len = Vector3.Distance(from, h); var e = b; e.Expand(0.3f); if (e.IntersectRay(ray, out var hit) && hit < len - 0.4f) return true; } return b.SqrDistance(from) < 1.6f * 1.6f; }
            FollowIndex(); var seen = new HashSet<Renderer>();
            foreach (var l in fIndex.Values) foreach (var (r, b, prop) in l) if ((prop || b.size.y < 4f && Mathf.Max(b.size.x, b.size.z) < 6f) && r && r.enabled && !r.forceRenderingOff && seen.Add(r) && (b.center - from).sqrMagnitude < 30f * 30f && OnLine(b)) { r.forceRenderingOff = true; cvHidden.Add(r); }
            var keep = new HashSet<Transform>(); foreach (var k in c.Single ? new[] { c.KeyB } : new[] { c.KeyA, c.KeyB }) if (ConvoView(k) is Transform t) keep.Add(t);
            foreach (var (n, x) in views.All())
            {
                if (!x || !x.gameObject.activeInHierarchy || keep.Contains(x)) continue;
                var p = x.position; if ((p - from).sqrMagnitude > 25f * 25f) continue;
                var bb = new Bounds(p + Vector3.up * 0.9f, new Vector3(0.7f, 1.8f, 0.7f)); if (!OnLine(bb)) continue;
                foreach (var r in x.GetComponentsInChildren<Renderer>()) if (r.enabled && !r.forceRenderingOff) { r.forceRenderingOff = true; cvHidden.Add(r); }
            }
        }

        /// <summary>Where a scene's speaker stands (their head, world) under the cutaway camera; null for one with no view in the street.</summary>
        public Vector3? ConvoHead(string speaker) => ConvoView(speaker) is Transform t && t.gameObject.activeInHierarchy ? t.position + Vector3.up * 1.8f : null;
        /// <summary>The pair's feet (for the row of dots under them).</summary>
        public Vector3? ConvoFeet(Convo c) { var b = ConvoView(c.KeyB); var a = ConvoView(c.KeyA); if (!b) return null; return c.Single || !a ? b.position : (a.position + b.position) / 2f; }
    }
}
