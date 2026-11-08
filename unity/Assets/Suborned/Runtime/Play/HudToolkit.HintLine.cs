using UnityEngine;
using UnityEngine.UIElements;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 101 round 3, the sweep (Glyph's rules 1 and 2, briefs/101/glyph/r3: "the hint line" and "the hint disc"; Sky: "yes"; moved here from HudToolkit.Less): the
    /// hint line bottom centre is a street label, no plate: the agent's face in the cream ring, the verb's pictogram keyed in the book's ink, the words in keyline Jost
    /// (a noun in its marker colour on an ink keyline, with its shape); the folded line's "?" is a round tab that reopens it. It retires the plate_dark line and disc.</summary>
    public sealed partial class HudToolkit
    {
        KeyIcon goalGlyphK;

        void BuildHintLine()
        {
            goal = StackRow(root, 30f); goal.pickingMode = PickingMode.Position; goal.style.position = Position.Absolute; goal.style.bottom = 20; goal.style.justifyContent = Justify.FlexStart;
            goalCameo = SFace(goal, 22f, 2f); goalCameo.style.marginRight = 6;
            goalGlyphK = KIcon(goal, "", 16f, kw: 2f); goalGlyph = goalGlyphK.Root; goalGlyph.style.marginRight = 6;
            goalWords = E(goal); goalWords.style.flexDirection = FlexDirection.Row; goalWords.style.alignItems = Align.Center; goalWords.style.overflow = Overflow.Visible;
            goal.RegisterCallback<PointerEnterEvent>(_ => goalHover = true); goal.RegisterCallback<PointerLeaveEvent>(_ => goalHover = false);
            manifest.Add((goal, "line", () => goalText));
            hintQ = RoundTab(root, 26f, "p_unknown", true); hintQ.style.position = Position.Absolute; hintQ.style.bottom = 21f; Show(hintQ, false);
            hintQ.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { host.HintReopenAt = Time.unscaledTime; e.StopPropagation(); } });
            manifest.Add((hintQ, "image", () => "hint reopen"));
            manifest.Add((goalCameo, "image", () => "hint portrait"));
        }

        void GoalLine(float W)
        {
            var t = host.Text; var g = host.Game; float now = Time.unscaledTime;
            if (t == null || m.Ended || m.BriefingOpen) { Show(goal, false); goalText = ""; return; }
            // Sky 2026-11-07 ("don't understand this thing here - what's the triangle about? And surely they know the mission - seeing this
            // through makes no sense and takes up valuable space"): the objective is no longer shown in play (it lives on the start page and the
            // book's Job page); this line carries only the range's hint, the hints and the first act's hint
            string line = null, cameo = null, glyph = null; hintAlpha = 0f; hintQOn = false;
            if (host.RangeHint is string rh) { line = rh; glyph = HudModel.AtlasIcon("fire", "fire", ""); } // engine 0.18 (the fight range): over a bystander, how to fire on one
            else if (host.rangeMode) { } // brief 029 (Sky on 0.2.34): the range has no mission, so no Handover goal, hint or first-act line; only its own hint above
            else if (host.HintNow != null && t.Hints2.Find(x => x.Id == host.HintNow.Id) is PlayText.Hint h && h.Short.Length > 0)
            {
                if (!hintShows.TryGetValue(h.Id, out var hs)) { hs = (now, 1, -1f); hintShows[h.Id] = hs; }
                float age = now - hs.first;
                bool on = age < t.HintShowS;
                if (!on && hs.lastEnd < 0) { hs.lastEnd = hs.first + t.HintShowS; hintShows[h.Id] = hs; }
                if (!on && hs.shows < t.HintMaxShows && now - hs.lastEnd > t.HintReturnS) { hs = (now, hs.shows + 1, -1f); hintShows[h.Id] = hs; on = true; }
                // reopened (the "?" disc, or H while the line is folded): the line and its ring for another showSeconds
                if (!on && host.HintReopenAt > hs.lastEnd && host.HintReopenAt > hs.first) { hs = (now, hs.shows, -1f); hintShows[h.Id] = hs; host.HintReopenAt = -1f; age = 0f; on = true; }
                if (on || goalHover)
                {
                    float left = t.HintShowS - (now - hs.first); hintAlpha = goalHover ? 1f : Mathf.Clamp01(left / 0.6f); // the line and its ring fade together
                    hintVerbGlyph = HudModel.AtlasIcon(h.Verb, h.Verb, ""); int nb = h.Short.IndexOf('['), ne = h.Short.IndexOf(']');
                    hintNounColour = nb >= 0 && ne > nb ? Noun(h.Short[(nb + 1)..ne].Replace("{who}", host.HintNow.Who)).c : TurqLight;
                    string sh = h.Short;
                    if (host.HintNow.Cause == "door") // GDD v0.24, Fable's H13.door: the keys of the turn from this view (Q Q from the opening yaw); one quarter away, the plain short with that key
                    {
                        int st = host.DoorTurnSteps(); string keys = host.DoorTurnKeys(); int dc = h.DoorShort.LastIndexOf(':'), pc = h.Short.LastIndexOf(':');
                        sh = st == 0 || Mathf.Abs(st) == 2 ? (h.DoorShort.Length > 0 ? (st != 0 && dc > 0 ? h.DoorShort[..dc] + ": " + keys : h.DoorShort) : h.Short) : pc > 0 ? h.Short[..pc] + ": " + keys : h.Short;
                    }
                    string who = host.HintNow.Who; line = sh.Replace("{who}", who.Length == 0 ? "them" : host.Text.TargetNames.ContainsKey(who) ? Cap1(host.Text.TargetName(who, who)) : who); // a common noun ("rival") stays lower case
                    string ag = h.Agent == "any" ? (host.HintNow.Portrait >= 0 ? Op.Name((OpId)host.HintNow.Portrait) : "") : h.Agent;
                    cameo = ag.Length > 0 ? $"portraits/cameo_{ag}_normal" : null; glyph = HudModel.AtlasIcon(h.Verb, h.Verb, "");
                    if (h.Verb == "turn-view" && line.IndexOf('<') < 0 && line.LastIndexOf(':') is int ci and > 0) line = line[..ci] + ": <TurnLeft> <TurnRight>"; // Glyph 016 §3: the keys as keycaps (text.json's "Q / E" until it carries the tokens)
                }
                else hintQOn = true; // folded: a "?" disc at the goal line's place reopens it
            }
            // Glyph's 0.2.94 review #4 (brief 049 row 15; "no sentences in play"): a hint's line shows only under Alt (the detail layer); in play
            // the hint is its ring and verb disc on the noun in the street, fading as the line would
            if (hintAlpha > 0f && line != null && !host.Peeking && !goalHover && !HandoverHost.TipHints.Contains(host.HintNow?.Id ?? "")) { line = null; cameo = null; glyph = null; } // Fable 067: a tip (H21, H22) rings nothing, so its short line shows in play
            if (line == null && !hintQOn && !PlateOn && !host.rangeMode && host.FirstActDue && hintAlpha <= 0f) { line = t.FirstActHint; glyph = "p_unknown"; } // brief 030: Fable's one hint, until a right click is used (per profile)
            host.HintLineShowing = hintAlpha > 0f;
            Show(hintQ, hintQOn && !show0(line)); if (hintQOn) hintQ.style.left = (W - 26) / 2;
            if (line != null) line = Keys(line);
            bool show = line != null; Show(goal, show); goalText = show ? line.Replace("[", "").Replace("]", "").Replace("<", "").Replace(">", "") : "";
            goal.style.opacity = hintAlpha > 0f ? hintAlpha : 1f;
            if (!show) return;
            Show(goalCameo, cameo != null); if (cameo != null) { Bg(goalCameo.Q("face"), cameo, Color.white); goalCameo.style.opacity = 0.75f + 0.25f * Mathf.Sin(now * 5f); }
            goalGlyphK.Set(this, glyph ?? ""); if (glyph != null) goalGlyphK.Look("ki-ink", "ki-key"); // brief 101 round 3: the verb keyed in the book's ink
            if (line != goalKey)
            {
                goalKey = line; goalWords.Clear();
                int i = 0;
                while (i < line.Length)
                {
                    int a = line.IndexOfAny(new[] { '[', '<' }, i);
                    if (a < 0) { Word(goalWords, line[i..], null); break; }
                    if (a > i) Word(goalWords, line[i..a], null);
                    bool key = line[a] == '<'; int b = line.IndexOf(key ? '>' : ']', a); if (b < 0) b = line.Length;
                    if (key) KeyCap(goalWords, line[(a + 1)..b]); else Word(goalWords, line[(a + 1)..b], Noun(line[(a + 1)..b]));
                    i = b + 1;
                }
            }
            goal.style.left = (W - (float.IsNaN(goal.resolvedStyle.width) ? 330 : goal.resolvedStyle.width)) / 2;
        }

        void KeyCap(VisualElement parent, string label)
        {
            var kp = E(parent, "s-plate_light", w: 22, h: 19); kp.style.position = Position.Relative; kp.style.left = StyleKeyword.Auto; kp.style.top = StyleKeyword.Auto; kp.style.marginLeft = 4; kp.style.flexShrink = 0;
            var l = L(kp, "keycap"); l.text = label; l.style.flexGrow = 1; l.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        void Word(VisualElement parent, string text, (Color c, bool person)? noun)
        {
            if (noun.HasValue)
            {
                var (c, person) = noun.Value;
                if (!person && text.Trim().ToLowerInvariant() == "car") // Glyph 016 r2 C: the car's glyph, as on the knob and the ground marker (a ring read as an O)
                { var cg = E(parent, null, w: 14, h: 14); cg.style.position = Position.Relative; cg.style.left = StyleKeyword.Auto; cg.style.top = StyleKeyword.Auto; cg.style.marginLeft = 4; cg.style.marginRight = 4; Icon(cg, "car", c); }
                else {
                var shape = P(null, 0, 0, 10, 10, (p, r) =>
                {
                    if (person) { p.fillColor = c; p.BeginPath(); p.MoveTo(new Vector2(0, 1)); p.LineTo(new Vector2(10, 1)); p.LineTo(new Vector2(5, 9)); p.ClosePath(); p.Fill(); }
                    else { p.strokeColor = c; p.lineWidth = 1.8f; p.BeginPath(); p.Arc(new Vector2(5, 5), 3.8f, Deg(0), Deg(360)); p.Stroke(); }
                });
                shape.style.position = Position.Relative; shape.style.left = StyleKeyword.Auto; shape.style.top = StyleKeyword.Auto; shape.style.marginLeft = 4; shape.style.marginRight = 4; parent.Add(shape);
                }
            }
            // Glyph round 2: a word's own leading space becomes a 4 px margin and a trailing one goes, so the gaps round a shape are even
            var trimmed = text.Trim(); if (trimmed.Length == 0) return;
            var l = KText(parent, 15.5f); l.Set(trimmed.Replace(' ', '\u00a0')); // brief 101 round 3: a street label (ink on the cream keyline)
            if (noun.HasValue) { l.Look("k-ink", "k-key-ink"); l.Face.style.color = noun.Value.c == Violet ? VioletText : noun.Value.c; } // a noun in its marker colour on an ink keyline; Glyph fight r1 #3: violet words lighter (#b894ec)
            if (text.StartsWith(" ")) l.Root.style.marginLeft = 4; // (a trailing space is dropped: the next noun's shape carries its own 4 px)
        }
    }
}
