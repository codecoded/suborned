using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 085 (GDD v0.36.0 section 10): Listen's framing, view only. The overheard pair fill the shot, the listener at its edge, turned away, then alone for the last line. A voice in the conversation
    /// that is a real person of the street (Milena Rask, of the café) stands in the doorway for the scene wherever the engine has her; the listener stands at the frame's edge. Each frame starts again from the
    /// engine, so nothing here outlasts the cut; the engine never hears of it. She's the one speaker §10's "inside" rule doesn't stop: she isn't there for the engine, so she can't be down, gone or inside.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>A voice speaker's view while its scene plays (milena: crowd/n).</summary>
        readonly Dictionary<string, string> convoVoices = new(StringComparer.Ordinal);
        /// <summary>The doorway's voice stands along the front and this far toward the door from the person she talks to, and only when that person is this close to the door.</summary>
        public const float ConvoDoorInM = 0.7f, ConvoDoorNearM = 7f; static readonly float[] ConvoDoorAlong = { 1.25f, 1f, 0.8f };

        void ConvoVoiceFigure(Convo c)
        {
            convoVoices.Clear(); if (c.Verb != "listen" || c.Id.Length == 0 || views == null) return;
            foreach (var (sp, _) in c.Lines)
            {
                if (IsRole(sp) || Array.IndexOf(OpKeys, sp) >= 0 || convoVoices.ContainsKey(sp)) continue;
                if (views.ListenPartnerUp(c.Target) is string up) { convoVoices[sp] = up; c.KeyA = sp; c.LivePartner = true; break; } // she has already walked out to her: nothing to place
                string first = Text.ConvName(sp) + " "; int i = Game.Walkers.FindIndex(w => w.P.Name.StartsWith(first, StringComparison.Ordinal)); if (i < 0) continue;
                string key = "crowd/" + i; if (views.Find(key) is not Transform || ConvoView(c.KeyB) is not Transform tb) continue;
                Pt door = Game.S.At(Game.S.GuardCafe); var dw = frame.World(door); var cf = Game.S.CafeFacing; var inward = frame.World(new Pt(door.X + cf.X, door.Y + cf.Y)) - dw; inward.y = 0f; inward.Normalize();
                if ((tb.position - dw).magnitude > ConvoDoorNearM) continue; // she isn't by the door: no one to stand in it with her
                // on the door's step beside the person she talks to, rather than behind her: the lens stands out on the street and has them both in profile, the door behind them
                var along = new Vector3(inward.z, 0f, -inward.x); var stand = Vector3.zero; bool free = false; float best = float.MaxValue; var doorLine = dw + inward * ConvoDoorInM;
                foreach (float a in ConvoDoorAlong) foreach (float side in new[] { 1f, -1f })
                {
                    var q = tb.position + along * (side * a) + inward * ConvoDoorInM; bool ok = Game.Walk.Free(frame.Engine(q)); float off = (q - doorLine).sqrMagnitude;
                    if (ok && !free || ok == free && off < best) { free = ok; best = off; stand = q; } // on open ground if there is any, and nearest the door's line; view only, so a planter's edge is no bar
                }
                convoVoices[sp] = key; c.KeyA = sp; c.Stands[key] = (stand, float.NaN); views.SceneStands(c);
                break; // one voice stands; a second would be a voice
            }
        }

        /// <summary>The listener, after the shot is framed: at the frame's edge on the side the line doesn't use, nearer than the pair, his back to them; the last line is his alone.</summary>
        void ConvoListener(Convo c)
        {
            if (convoVoices.Count == 0) return;
            string ag = Op.Name((OpId)c.Agent); string key = "op/" + ag; if (views.Find(key) is not Transform t) return;
            if (c.LivePartner && !(c.Shot == "profile" && ListenerInWay(c, c.From, ConvoView(c.KeyA).position, ConvoView(c.KeyB).position))) { if (c.Lines.Count > 1 && c.Lines[^1].speaker == ag) c.LastAlone = ag; return; } // he stays where the Listen had him, in the distance or at the edge, as he is; only a Scout the player stood right up against her is moved to the edge, for the shot
            var ta = ConvoView(c.KeyA); var tb = ConvoView(c.KeyB); var mid = ta && tb ? (ta.position + tb.position) / 2f : t.position; Vector3 stand = t.position;
            if (c.Shot == "profile")
            {
                var fwd = c.Look - c.From; fwd.y = 0f; fwd.Normalize(); var right = new Vector3(fwd.z, 0f, -fwd.x); float d = c.Dist * ConvoEdgeDepth, half = d * Mathf.Tan(ConvoFov * 0.5f * Mathf.Deg2Rad) * cam.aspect; var lens = new Vector3(c.From.x, t.position.y, c.From.z);
                foreach (float side in new[] { 1f, -1f })
                {
                    var p = lens + fwd * d + right * (side * half * ConvoEdgeAt); if (!Game.Walk.Free(frame.Engine(p))) continue;
                    var b = frame.ToBlock(new Vector2(p.x, p.z)); p.y = frame.Height(b.x, b.y); stand = p; break;
                }
            }
            var away = stand - mid; away.y = 0f; float yaw = away.sqrMagnitude > 0.01f ? Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg : float.NaN;
            c.Stands[key] = (stand, yaw); views.SceneStands(c);
            if (c.Lines.Count > 1 && c.Lines[^1].speaker == ag) c.LastAlone = ag;
        }
        public const float ConvoEdgeDepth = 0.72f, ConvoEdgeAt = 0.86f;

        /// <summary>The last line: the listener alone (the intercut's own framing from here on; the pair behind him aren't drawn: ConvoHideOccluders).</summary>
        void ConvoToAlone(Convo c)
        {
            string who = c.LastAlone; c.LastAlone = ""; if (ConvoView(who) is not Transform t) return;
            var (f, l, _) = SingleShot(t.position, FigureFacing(who) ?? Vector3.forward); c.Cuts[who] = (f, l); c.Shot = "intercut"; c.Single = true; c.Dist = ConvoSingleDist; c.Cut = ""; c.Solo = who;
            cam.orthographic = false; cam.fieldOfView = ConvoFov; cam.nearClipPlane = Mathf.Min(cvNear, 0.1f); if (fade) { fade.radiusPerZoom = 0.02f; fade.ghost = 0f; fade.SetPoints(null); }
        }

        /// <summary>A Listen's shot doesn't put the listener between the lens and the pair, or under the lens, when another side will do.</summary>
        bool ListenerInWay(Convo c, Vector3 lens, Vector3 a, Vector3 b)
        {
            if (!c.LivePartner || views.Find("op/" + Op.Name((OpId)c.Agent)) is not Transform t) return false; var p = t.position; p.y = 0f; lens.y = 0f; if ((p - lens).magnitude < 1.5f) return true;
            foreach (var q0 in new[] { a, b }) { var q = q0; q.y = 0f; var d = q - lens; float k = Mathf.Clamp01(Vector3.Dot(p - lens, d) / Mathf.Max(0.01f, d.sqrMagnitude)); if (k < 0.98f && (lens + d * k - p).magnitude < 0.8f) return true; }
            return false;
        }

        void ConvoStandsEnd() { views?.SceneStandsEnd(); convoVoices.Clear(); }
    }
}
