using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 032 (Sky 2026-10-21, timing B; Glyph's spec): the conversation scene. When an agent begins Listen, Ask, Pay, Help,
    /// Pressure, Call or Mislead (the act's first tick), its opening lines play in a shot that holds both people (Glyph 067 Rev 2 H1: the profile
    /// two-shot, else the high two-shot at the play pitch, the intercut for a pair more than 6 m apart; ConvoFrameShot), the mission clock
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
            /// <summary>Glyph 067 Rev 2 H1, the shot the ladder took: "profile" (the two-shot from the side), "high" (the play pitch, orthographic,
            /// the cut and silhouettes on), "intercut" (each speaker alone, cut with the bubble: a pair more than 6 m apart), "single" (the two on
            /// one point). Ortho: the high shot's half height (m). FromA/LookA, FromB/LookB, Framed: the intercut's two shots and the one up now.</summary>
            public string Shot = ""; public float Ortho; public Vector3 FromA, LookA, FromB, LookB; public int Framed = -1;
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
        /// <summary>Glyph 067 Rev 2 H1: the profile two-shot holds a pair up to 6 m apart, at fit = clamp(1.44 × sep, 3.6, 8) m (then 0.8 × fit, never
        /// under 2.8 m); nobody shows more than 115° of back; nothing solid within 1.6 m of the lens. The high two-shot: the play pitch from 7 m.</summary>
        public const float ConvoTwoShotM = 6f, ConvoFitPerM = 1.44f, ConvoFitMaxM = 8f, ConvoNearM = 2.8f, ConvoBackDeg = 115f, ConvoLensClearM = 1.6f, ConvoHighPitch = 38f, ConvoHighM = 7f;
        /// <summary>Glyph 067 Rev 2 H1.1: the perpendicular, then ±12°, then ±25° round the pair's middle (past 25° one of them turns his back).</summary>
        static readonly int[] ConvoAngles = { 0, 12, -12, 25, -25 };
        /// <summary>For the proofs (H5: "one scene forced into the high two-shot"): "high" takes the high two-shot whatever the ladder finds; "" the ladder.</summary>
        public static string ConvoForceShot = "";
        readonly HashSet<string> convoMet = new(StringComparer.Ordinal);
        (int k, string verb, string target, FeedCard card, int tick)? convoWant;
        readonly (int tick, string text)[] lastAck = new (int, string)[4], lastArrive = new (int, string)[4];
        // the view before the cut, restored exactly
        Vector3 cvPos; Quaternion cvRot; bool cvOrtho; float cvSize, cvFov, cvNear, cvFar, cvFadeR, cvGhost, cvFeather, cvShadowDepth; Vector3 cvFocus; bool cvCut;
        readonly List<Vector4> cvPoints = new();

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

        /// <summary>Brief 067 H: the scene's two speakers as view keys (the cell member's, the person's), for the figures: both stand idle and face
        /// each other while it plays (no walk or run clip on a held clock, no slide). Null when no scene plays.</summary>
        public (string a, string b)? ConvoViewKeys()
        {
            var c = ConvoNow; if (c == null || views == null || c.OutAt >= 0f && !cvCut) return null;
            string K(string k) => k switch { "scout" or "fixer" or "tech" or "enforcer" => "op/" + k, "rivals" => views.RivalAnchorKey(), _ => k };
            return (K(c.KeyA), K(c.KeyB));
        }

        Transform ConvoView(string key) => views == null ? null : key switch
        {
            "scout" or "fixer" or "tech" or "enforcer" => views.Find("op/" + key),
            "rivals" => views.Find(views.RivalAnchorKey()), // brief 059: the car, else the first member drawn
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
            ConvoLast = $"playing: {w.verb} {w.target}, {c.Lines.Count} beats, {c.Shot}";
            return true;
        }

        /// <summary>Glyph 067 Rev 2 H1 (Sky on 0.2.122: "the camera frames both, agent and subject, faces visible, bubbles on screen, nothing
        /// occluding"): a ladder of shots that always holds both people, the first that works. (1) The profile two-shot for a pair up to 6 m apart:
        /// the lens on the pair's perpendicular, 1.9 m up, 8° down, at fit = clamp(1.44 × sep, 3.6, 8) m, then ±12°, then ±25° round the pair's
        /// middle, then the same five at 0.8 × fit (never under 2.8 m); at an equal angle the side the camera was on (the player's left stays left
        /// across the cut), else the side the subject faces; a lens is clear when the lines to each head and chest keep 0.4 m off every solid,
        /// nobody shows more than 115° of back and nothing solid stands within 1.6 m of it. (2) None clear: the high two-shot, the play pitch (38°)
        /// from 7 m with the same field, centred on the pair, at the yaw the player left (an orthographic lens, so the city's cut and silhouettes
        /// hold, H4). (3) A pair more than 6 m apart: the intercut, each speaker alone 2.6 m from where they face, cut with the bubble. The
        /// over-the-shoulder fallback is gone (it hid the agent's face behind his back).</summary>
        bool ConvoFrameShot(Convo c)
        {
            if (!cam) return false;
            var tb = ConvoView(c.KeyB); var ta = ConvoView(c.KeyA); if (!tb) return false;
            Vector3 b = tb.position, a = ta && ta.gameObject.activeInHierarchy ? ta.position : b;
            var ab = b - a; ab.y = 0f; float sep = ab.magnitude;
            c.Single = false; c.Framed = -1;
            if (sep < 0.2f)
            {   // the two on one point (the engine's own, the views not yet apart): the person alone frames both, from where they face
                c.Shot = "single"; c.Single = true; (c.From, c.Look, c.Side) = SingleShot(b, FigureFacing(c.KeyB) ?? Vector3.forward); c.Dist = ConvoSingleDist; return true;
            }
            var dir = ab / sep;
            if (sep > ConvoTwoShotM && ConvoForceShot != "high")
            {   // (3) the intercut: each speaker alone, from where they face (each turned to the other), cut with the bubble (ConvoFrame)
                c.Shot = "intercut"; c.Single = true;
                (c.FromA, c.LookA, _) = SingleShot(a, dir); (c.FromB, c.LookB, _) = SingleShot(b, -dir);
                IntercutOn(c, c.Lines.Count > 0 ? c.Lines[0].speaker : c.KeyB); c.Dist = ConvoSingleDist; return true;
            }
            var mid = (a + b) / 2f; var perp = new Vector3(-dir.z, 0f, dir.x);
            if (ConvoForceShot != "high")
            {   // (1) the profile two-shot
                float fit = Mathf.Clamp(ConvoFitPerM * sep, ConvoDist, ConvoFitMaxM);
                var here = cam.transform.position - mid; here.y = 0f; float sd = Vector3.Dot(perp, here);
                if (Mathf.Abs(sd) < 0.05f * Mathf.Max(0.01f, here.magnitude) && FigureFacing(c.KeyB) is Vector3 fb) sd = Vector3.Dot(perp, fb); // the camera on the pair's line: the side the subject's face is toward
                var sides = sd >= 0f ? new[] { perp, -perp } : new[] { -perp, perp };
                foreach (float dist in new[] { fit, Mathf.Max(0.8f * fit, ConvoNearM) })
                    foreach (int deg in ConvoAngles)
                        foreach (var s0 in sides)
                        {
                            var s = Quaternion.Euler(0f, deg, 0f) * s0; var from = mid + s * dist; from.y = mid.y + ConvoHeight;
                            if (!TwoShotClear(a, b, dir, from)) continue;
                            c.Shot = "profile"; c.Side = s; c.Dist = dist; c.From = from; c.Look = mid + Vector3.up * ConvoHeight + Vector3.down * Mathf.Tan(ConvoPitch * Mathf.Deg2Rad) * dist; return true;
                        }
            }
            // (2) the high two-shot: no lens on the ground holds both clear (the café door, the Office front)
            float yaw = cam.transform.eulerAngles.y; var hr = Quaternion.Euler(ConvoHighPitch, yaw, 0f); var fwd = hr * Vector3.forward;
            c.Shot = "high"; c.Look = mid + Vector3.up * 0.9f; c.From = c.Look - fwd * ConvoHighM; c.Dist = ConvoHighM; c.Side = -new Vector3(fwd.x, 0f, fwd.z).normalized;
            // the 35° field at 7 m (2.2 m half height), opened to hold both whole: the pair's spread across the frame and up it (a pair lined up with
            // the view spreads up the frame), each person 1 m wide and 1.75 m tall, inside the 6 % bars
            var d = b - a; float hx = Mathf.Abs(Vector3.Dot(d, hr * Vector3.right)) / 2f + 1.0f, hy = (Mathf.Abs(Vector3.Dot(d, hr * Vector3.up)) / 2f + 1.3f) / 0.88f;
            c.Ortho = Mathf.Max(ConvoHighM * Mathf.Tan(ConvoFov * 0.5f * Mathf.Deg2Rad), hy, hx / Mathf.Max(1f, cam.aspect));
            return true;
        }

        /// <summary>A person's drawn facing (world, flat), else null.</summary>
        Vector3? FigureFacing(string key) => views?.FigureYaw(key is "scout" or "fixer" or "tech" or "enforcer" ? "op/" + key : key) is float fy ? Quaternion.Euler(0f, fy, 0f) * Vector3.forward : (Vector3?)null;

        /// <summary>One person alone, from where they face: 2.6 m out, 1.9 m up, looking at the head; the nearest clear bearing to the facing within
        /// 60° (faces stay visible), else the facing itself.</summary>
        (Vector3 from, Vector3 look, Vector3 side) SingleShot(Vector3 p, Vector3 face)
        {
            face.y = 0f; if (face.sqrMagnitude < 1e-4f) face = Vector3.forward; face.Normalize();
            var look = p + Vector3.up * 1.45f; var side = face; bool found = false;
            var bearings = new[] { 0, 30, -30, 60, -60 };
            for (int pass = 0; pass < 2 && !found; pass++) // a clear line first; else at least a camera outside every mass
                foreach (int deg in bearings)
                {
                    var s = Quaternion.Euler(0f, deg, 0f) * face; var from = look + s * ConvoSingleDist; from.y = p.y + ConvoHeight;
                    if (pass == 0 ? !ConvoClear(look, from) : !ConvoClear(from, from)) continue;
                    side = s; found = true; break;
                }
            var f = look + side * ConvoSingleDist; f.y = p.y + ConvoHeight; return (f, look, side);
        }

        /// <summary>The intercut on the speaker of the line now (A, B; anyone else keeps the shot up): true when the shot changed.</summary>
        bool IntercutOn(Convo c, string speaker)
        {
            int f = speaker == c.KeyA ? 0 : speaker == c.KeyB ? 1 : c.Framed >= 0 ? c.Framed : 1;
            if (f == c.Framed) return false;
            c.Framed = f; c.From = f == 0 ? c.FromA : c.FromB; c.Look = f == 0 ? c.LookA : c.LookB; c.Side = (c.From - c.Look).normalized; return true;
        }

        /// <summary>Glyph 067 Rev 2 H1.1: a two-shot lens is clear when nobody shows more than 115° of back to it (each faces the other), nothing solid
        /// stands within 1.6 m of it, and the lines from it to each head (1.5 m) and chest (1.1 m) keep 0.4 m off every solid (ConvoClear).</summary>
        bool TwoShotClear(Vector3 a, Vector3 b, Vector3 dir, Vector3 lens)
        {
            var la = lens - a; la.y = 0f; var lb = lens - b; lb.y = 0f;
            if (Vector3.Angle(dir, la) > ConvoBackDeg || Vector3.Angle(-dir, lb) > ConvoBackDeg) return false;
            if (!LensClear(lens)) return false;
            foreach (var p in new[] { a, b }) if (!ConvoClear(p + Vector3.up * 1.5f, lens) || !ConvoClear(p + Vector3.up * 1.1f, lens)) return false;
            return true;
        }

        /// <summary>No wall or mass (props aside: the scene hides those) within 1.6 m of the lens.</summary>
        bool LensClear(Vector3 lens)
        {
            FollowIndex(); var (cx, cz) = FCell(lens.x, lens.z); float r2 = ConvoLensClearM * ConvoLensClearM;
            for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
            {
                if (!fIndex.TryGetValue((cx + dx, cz + dz), out var l)) continue;
                foreach (var (r, bb, prop) in l) if (!prop && r && r.enabled && bb.SqrDistance(lens) < r2) return false;
            }
            return true;
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
            {   // the shot's one motion: a slow push in of 8 % over the scene; the intercut cuts to whoever speaks (a hard cut, with the bubble)
                if (c.Shot == "intercut" && c.Beat < c.Lines.Count && IntercutOn(c, c.Lines[c.Beat].speaker)) { ConvoUnhide(); ConvoHideOccluders(c); }
                float span = Mathf.Max(1f, c.Lines.Count * (Text?.BubbleS ?? 3f)), k = Mathf.Clamp01((now - c.Born) / span);
                if (c.Shot == "high") cam.orthographicSize = c.Ortho * (1f - ConvoPush * k); // the orthographic lens pushes in by its size
                else { var from = Vector3.Lerp(c.From, c.Look, ConvoPush * k); cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(c.Look - from, Vector3.up)); }
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
        string[] ConvoFramed(Convo c) => c.Shot == "intercut" ? new[] { c.Framed == 0 ? c.KeyA : c.KeyB } : c.Single ? new[] { c.KeyB } : new[] { c.KeyA, c.KeyB };
        /// <summary>Everything the scene hid is drawn again (the cut back, or the intercut's next shot).</summary>
        void ConvoUnhide() { foreach (var r in cvHidden) if (r) r.forceRenderingOff = false; cvHidden.Clear(); }

        readonly List<Renderer> cvHidden = new();
        /// <summary>What would stand in the shot: street props (a planter's palm) on the line from the camera to either person's head or chest
        /// (1.5 m and 1.1 m: a potted palm's fronds hid Inez's body under a clear head line) or within 1.6 m of the camera, and people (passers-by, the cell) standing in those lines or within 1.6 m of the camera, are not drawn while the
        /// scene plays (forceRenderingOff; back at the cut back). A low solid (a planter, a tree bed: under 4 m tall, 6 m wide) on those lines goes too; facades are never hidden (the camera picked a side clear of them).
        /// The orthographic high shot reads its lines along the view; the intercut, the one speaker framed.</summary>
        void ConvoHideOccluders(Convo c)
        {
            cvHidden.Clear(); var from = c.From; bool high = c.Shot == "high"; var fwd = (c.Look - c.From).normalized;
            var heads = new List<Vector3>(); foreach (var k in ConvoFramed(c)) if (ConvoView(k) is Transform t && t.gameObject.activeInHierarchy) { heads.Add(t.position + Vector3.up * 1.5f); heads.Add(t.position + Vector3.up * 1.1f); }
            // the orthographic high shot sees along parallel lines: a head's line runs back from it along the view, not to one lens point
            bool OnLine(Bounds b) { foreach (var h in heads) { var o = high ? h - fwd * 30f : from; var ray = new Ray(o, (h - o).normalized); float len = Vector3.Distance(o, h); var e = b; e.Expand(0.3f); if (e.IntersectRay(ray, out var hit) && hit < len - 0.4f) return true; } return !high && b.SqrDistance(from) < 1.6f * 1.6f; }
            FollowIndex(); var seen = new HashSet<Renderer>();
            var near0 = high ? c.Look : from;
            foreach (var l in fIndex.Values) foreach (var (r, b, prop) in l) if ((prop || b.size.y < 4f && Mathf.Max(b.size.x, b.size.z) < 6f) && r && r.enabled && !r.forceRenderingOff && seen.Add(r) && (b.center - near0).sqrMagnitude < 30f * 30f && OnLine(b)) { r.forceRenderingOff = true; cvHidden.Add(r); }
            var keep = new HashSet<Transform>(); foreach (var k in new[] { c.KeyA, c.KeyB }) if (ConvoView(k) is Transform t) keep.Add(t); // neither speaker is ever hidden (the intercut's other one too)
            foreach (var (n, x) in views.All())
            {
                if (!x || !x.gameObject.activeInHierarchy || keep.Contains(x)) continue;
                var p = x.position; if ((p - near0).sqrMagnitude > 25f * 25f) continue;
                var bb = new Bounds(p + Vector3.up * 0.9f, new Vector3(0.7f, 1.8f, 0.7f)); if (!OnLine(bb)) continue;
                foreach (var r in x.GetComponentsInChildren<Renderer>()) if (r.enabled && !r.forceRenderingOff) { r.forceRenderingOff = true; cvHidden.Add(r); }
            }
        }

        /// <summary>Where a scene's speaker stands (their head, world) under the cutaway camera; null for one with no view in the street.</summary>
        public Vector3? ConvoHead(string speaker) => ConvoView(speaker) is Transform t && t.gameObject.activeInHierarchy ? t.position + Vector3.up * 1.8f : null;
        /// <summary>The pair's feet (for the row of dots under them).</summary>
        public Vector3? ConvoFeet(Convo c)
        {
            var b = ConvoView(c.KeyB); var a = ConvoView(c.KeyA); if (!b) return null;
            if (c.Shot == "intercut") return c.Framed == 0 && a ? a.position : b.position; // the dots under whoever is framed
            return c.Single || !a ? b.position : (a.position + b.position) / 2f;
        }
    }
}
