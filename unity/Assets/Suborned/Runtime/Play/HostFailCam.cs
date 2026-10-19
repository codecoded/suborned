using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>The fail camera (Sky 2026-10-18: "if you fail mission, the camera should pan around the item / person that failed you so you
    /// know what it was for next time"). On a failure the run's own Game.Cause (FailCause, Keel) names what decided it. A second game is built
    /// from the run's start state and order log to a few seconds before the deciding tick (off screen, as Engine.Play replays), then stepped
    /// on at a slow pace while the camera orbits the culprit (the single culprit, else the origin's reporter, else its scene), its view cone
    /// drawn and the operatives involved ringed; the HUD hides but for one caption and its tip (Fable's less.failcam). A click or Esc skips to
    /// the end card, which offers "Show me again". View only: the run that was played is kept as it ended, so its replay and checkpoints are
    /// untouched; the second game is thrown away.</summary>
    public sealed partial class HandoverHost
    {
        [Header("The fail camera")]
        [Tooltip("Seconds of the run before the deciding moment that the fail camera replays.")]
        public float failRewindS = 3f;
        [Tooltip("Pace of that replay (1 = real time).")]
        public float failPace = 0.6f;
        [Tooltip("Seconds the camera holds on the deciding moment before the end card.")]
        public float failHoldS = 1.2f;
        [Tooltip("Degrees the camera turns round the culprit over the whole replay; pitch (degrees) and zoom (orthographic half-height) while it does.")]
        public float failOrbitDeg = 24f, failPitch = 36f, failZoom = 9f, failZoomMin = 9f, failZoomMax = 9f, failFadeRadius = 1.3f; // zoom 9 (Glyph 022: the closest shot, 6, drew the Enforcer at 127 px against 85 in the others; Glyph round 2 measured 95 at 8.5: 9 holds 85-90 in each). fade: the disc as a share of the zoom while it plays (play 0.8)

        [Tooltip("Checks and shots: at the deciding moment the fail camera holds until stopped (as played, it holds failHoldS).")]
        public bool failHoldForShots;
        [Tooltip("Width (m) of the fade's dithered edge while the fail camera plays (play 2): a person near an edge stays clear of the dither.")]
        public float failFeather = 0.4f;

        /// <summary>The fail camera is playing (the HUD shows only its caption; the end card waits).</summary>
        public bool FailCamOn { get; private set; }
        /// <summary>The run's end has something to orbit: the end card's "Show me again".</summary>
        public bool FailCamAvailable => endGame?.Cause is FailCause c && FailTarget(c) != null;
        /// <summary>The caption and its tip while it plays (Fable's less.failcam, filled).</summary>
        public string FailCaption { get; private set; } = "";
        public string FailTip { get; private set; } = "";
        /// <summary>For the HUD's marks while it plays: the operative caught (feet, world) and the culprit (head, world), the replay's pace,
        /// and whether the replay has reached the deciding moment (the tip shows then).</summary>
        public Vector3? FailOpAt { get; private set; } public Vector3? FailCulpritAt { get; private set; }
        /// <summary>Glyph 022 (N125, "Enemy fire goes for the Enforcer" with no fire drawn): the culprit shot the operative; the HUD draws a tracer
        /// from FailShooterAt (the shooter's chest) to the operative's, not the watcher's dotted line.</summary>
        public bool FailShot => FailCamOn && endGame?.Cause is FailCause fc && fc.Single && fc.Culprits.Count > 0 && fc.Culprits[0].Role == "shot";
        public Vector3 FailShooterAt => failCulprit + Vector3.up * 1.35f; public bool FailAtMoment => FailCamOn && failHoldUntil >= 0f; public float FailPace => failPace;

        Suborned.Sim.Game endGame, failGame; readonly List<Order> failOrders = new(); int failNext, failTo; float failAcc, failStart, failHoldUntil, failClock; // failClock: its own time, each frame capped at 0.1 s (a stalled frame slows it, never skips it)
        float fadeR0, ghost0, feather0; readonly List<Renderer> failProps = new(), failHidden = new(); string failOpKey; Vector3 failCulprit; GameObject coneGo; Mesh coneMesh; LineRenderer coneRim, coneSweep; readonly List<Vector4> failFade = new(); Vector3 failPivot; Quaternion camRot0; Vector3 camPos0, focus0; float camSize0; bool camOrtho0; readonly bool[] sel0 = new bool[4]; float yaw0;

        /// <summary>What the camera orbits: the single culprit, else the origin's reporter, else the origin's scene; null for nothing placed.</summary>
        static (Pt at, Blamed who)? FailTarget(FailCause c)
        {
            if (c.Fail is "withdraw" or "engine-error" || c.Why == "unresolved") return null;
            if (c.Single && c.Culprits.Count > 0 && c.Culprits[0].Placed) return (c.Culprits[0].At, c.Culprits[0]);
            foreach (var b in c.Culprits) if (b.Placed && c.Single) return (b.At, b);
            if (c.Origin?.Reporter is Blamed r && r.Placed) return (r.At, r);
            if (c.Origin is CauseOrigin o && o.ScenePlaced) return (o.Scene, null);
            foreach (var b in c.Culprits) if (b.Placed) return (b.At, b);
            return null;
        }

        /// <summary>After End(): a failure with something to orbit starts the fail camera (the end card waits for it).</summary>
        void FailCamAfterEnd() { endGame = Game; if (Game.Result == Outcome.Failure && FailCamAvailable) FailCamStart(); }

        /// <summary>Scripted checks and shots: adopt a run played elsewhere (a scenario, headless) as this host's ended run, with its views, end
        /// card and debrief, and start its fail camera. The played game is only read.</summary>
        public void ShowEnded(Suborned.Sim.Game played)
        {
            if (FailCamOn) FailCamStop();
            if (viewRoot) Destroy(viewRoot.gameObject);
            Game = played; Fault = null;
            viewRoot = new GameObject("engine").transform; viewRoot.SetParent(transform, false);
            views = new HostViews(this, viewRoot, frame); views.Tick(); views.Tick(); views.Frame(1f);
            try { EndDebrief = Debrief.Of(played); } catch { EndDebrief = null; }
            Ending = $"{played.Result}{(played.FailCode.Length > 0 ? $" ({played.FailCode})" : "")} at {Report.Clock(played.Tick)} · scenario";
            ShowBriefing = false; ClearHover(); if (Panel != null) ClosePanel();
            FailCamAfterEnd();
        }

        /// <summary>The end card's "Show me again", or the start after a failure.</summary>
        public void FailCamStart()
        {
            if (endGame?.Cause is not FailCause c || FailTarget(c) is not { } tgt || !cam || FailCamOn) return;
            if (Following) FollowExit();
            var orders = new List<(int idx, Order o)>(); for (int k = 0; k < endGame.Orders.Count; k++) orders.Add((k, endGame.Orders[k].order));
            orders.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.idx.CompareTo(b.idx));
            failOrders.Clear(); foreach (var (_, o) in orders) failOrders.Add(o);
            failTo = !c.Single && c.Origin != null && c.Origin.Tick > 0 ? c.Origin.Tick : c.Tick; // no single culprit (the seal): the moment it began, the report that raised it
            int from = Mathf.Max(0, failTo - Mathf.RoundToInt(failRewindS * Game.Hz));
            Suborned.Sim.Game g = null; bool ok = Guard("fail camera", () =>
            {
                g = Engine.New(Bundle.Inputs, endGame.Start); failNext = 0;
                while (failNext < failOrders.Count && failOrders[failNext].Tick < from) { var o = failOrders[failNext++]; g.RunTo(o.Tick); if (g.Over) break; if (g.Tick == o.Tick) g.Issue(o); }
                g.RunTo(from);
            });
            if (!ok || g == null) { Fault = null; return; }
            failGame = g; Game = g; views.Tick(); views.Tick(); views.Frame(1f);
            // the camera: the culprit at the centre, a little lower than play pitch, turning slowly round it
            camPos0 = cam.transform.position; camRot0 = cam.transform.rotation; camSize0 = cam.orthographicSize; camOrtho0 = cam.orthographic; focus0 = focus; yaw0 = yawNow;
            System.Array.Copy(Selected, sel0, 4); System.Array.Clear(Selected, 0, 4); foreach (var o in c.Ops) if (IsOpName(o)) Selected[(int)Op.Parse(o)] = true;
            failPivot = frame.World(tgt.at);
            if (!c.Single && c.Origin is CauseOrigin og && og.ScenePlaced && og.Reporter is Blamed rp && rp.Placed) failPivot = (frame.World(og.Scene) + frame.World(rp.At)) / 2f; // the seal: the reporter and what it saw, both in frame
            failAcc = 0f; failStart = Time.unscaledTime; failHoldUntil = -1f; failClock = 0f; failYawSet = false; failWalls.Clear();
            (FailCaption, FailTip) = FailWords(c, tgt.who);
            if (fade) { fadeR0 = fade.radiusPerZoom; ghost0 = fade.ghost; feather0 = fade.feather; fade.radiusPerZoom = 0.02f; fade.ghost = 0f; fade.feather = failFeather; } // Glyph r1: only what stands on the culprit-op line, cut clean (no dither); a narrow edge
            FailProps(failPivot);
            failOpKey = c.Ops.Count > 0 && IsOpName(c.Ops[0]) ? "op/" + c.Ops[0] : null;
            failCulprit = frame.World(tgt.at); failCulpritKey = tgt.who is Blamed cb && cb.Kind is not ("camera" or "op") && views?.Find(cb.Id) != null ? cb.Id : "";
            // where the pair ends (the run's last tick): ending within 2.5 m, the camera faces the culprit's front (the engine's facing, or across
            // the line between them), so the view's side step (HostViews.Untangle) shows both
            failClose = false;
            if (c.Ops.Count > 0 && IsOpName(c.Ops[0]))
            {
                // Glyph round 3 (N119): where the pair stand at the deciding moment the replay holds on, not at the run's end (the Enforcer had
                // walked on): a second copy of the run, replayed to that tick, off screen and thrown away
                var opAt = endGame.Ops[(int)Op.Parse(c.Ops[0])].Pos;
                Guard("fail camera", () =>
                {
                    var g2 = Engine.New(Bundle.Inputs, endGame.Start); int nx = 0;
                    while (nx < failOrders.Count && failOrders[nx].Tick < failTo) { var o = failOrders[nx++]; g2.RunTo(o.Tick); if (g2.Over) break; if (g2.Tick == o.Tick) g2.Issue(o); }
                    if (!g2.Over) g2.RunTo(failTo); opAt = g2.Ops[(int)Op.Parse(c.Ops[0])].Pos;
                });
                var oe = frame.World(opAt); var de = failCulprit - oe; de.y = 0f;
                if (de.magnitude < 2.5f)
                {
                    failClose = true;
                    // Glyph round 3 (N119: from the guard's front the Enforcer stood half behind him): look across the line between them, so the
                    // pair stand apart on screen, from the side the culprit faces (his front, and his eye, toward the camera)
                    float across = de.sqrMagnitude > 0.01f ? Mathf.Atan2(de.x, de.z) * Mathf.Rad2Deg + 90f : yaw0;
                    if (tgt.who is Blamed bw && bw.Faces) { var fw = frame.Dir(bw.Facing); var look = Quaternion.Euler(0f, across, 0f) * Vector3.forward; if (Vector3.Dot(look, new Vector3(fw.x, 0f, fw.z)) > 0f) across += 180f; } // the camera looks against his facing
                    failCloseYaw = across;
                }
            }
            FailCamOn = true; FailPlace(0f);
        }

        /// <summary>Back to the run as it ended: its game, the camera and the selection as they were; the end card shows.</summary>
        /// <summary>How the last fail camera ended (for checks): why, its own clock, the second game's tick and the target tick.</summary>
        public string FailCamLast { get; private set; } = "";
        public void FailCamStop() => FailCamStop("stopped");
        void FailCamStop(string why)
        {
            if (!FailCamOn) return;
            FailCamLast = $"{why} at clock {failClock:0.00} s, tick {failGame?.Tick} of {failTo}, over {failGame?.Over}";
            FailCamOn = false; Game = endGame; failGame = null; views.Tick(); views.Frame(1f); views.ShowFans(null);
            cam.orthographic = camOrtho0; cam.orthographicSize = camSize0; cam.transform.SetPositionAndRotation(camPos0, camRot0); focus = focus0; if (fade) fade.focus = focus;
            System.Array.Copy(sel0, Selected, 4);
            if (fade) { fade.radiusPerZoom = fadeR0; fade.ghost = ghost0; fade.feather = feather0; fade.SetPoints(null); }
            foreach (var r in failHidden) if (r) r.forceRenderingOff = false; failHidden.Clear(); failProps.Clear();
            if (coneGo) coneGo.SetActive(false); FailOpAt = FailCulpritAt = null;
        }

        /// <summary>Per frame while it plays: step the second game at the slow pace (feeding the run's orders at their ticks), turn the camera,
        /// draw the culprit's cone; at the deciding tick hold a beat, then the end card. A click or Esc skips.</summary>
        bool FailCamFrame()
        {
            if (!FailCamOn) return false;
            var mouse = UnityEngine.InputSystem.Mouse.current; var kb = Keyboard.current;
            if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame && Time.unscaledTime - failStart > 0.3f)) { FailCamStop(kb != null && kb.escapeKey.wasPressedThisFrame ? "esc" : "click"); return true; }
            var g = failGame; float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f); failClock += dt;
            if (g.Tick < failTo && !g.Over)
            {
                failAcc += dt * failPace;
                while (failAcc >= 0.1f && g.Tick < failTo && !g.Over)
                {
                    failAcc -= 0.1f;
                    Guard("fail camera", () => { while (failNext < failOrders.Count && failOrders[failNext].Tick == g.Tick) g.Issue(failOrders[failNext++]); g.Step(); });
                    views.Tick();
                }
            }
            else if (failHoldUntil < 0f) failHoldUntil = failClock + failHoldS;
            views.Frame(g.Tick >= failTo || g.Over ? 1f : Mathf.Clamp01(failAcc / 0.1f));
            // the culprit's cone (a camera, the guard, the patrol, an officer or robot) as the engine draws it, on the street
            if (endGame.Cause is FailCause c)
            {
                Fan f = null; var id = c.Single && c.Culprits.Count > 0 ? c.Culprits[0].Id : c.Origin?.Reporter?.Id ?? "";
                if (id.Length > 0) Guard("fail cone", () => { if (Game.WatcherKind(id).Length > 0) f = g.ConeFan(id); });
                views.ShowFans(null); FailCone(f);
            }
            float total = failRewindS / Mathf.Max(0.05f, failPace) + failHoldS;
            FailPlace(Mathf.Clamp01(failClock / total));
            if (failHoldUntil >= 0f && failClock >= failHoldUntil && !failHoldForShots) FailCamStop("held");
            if (failHoldForShots && failHoldUntil >= 0f) failClock = failHoldUntil; // held at the moment for a shot
            return true;
        }

        void FailPlace(float t)
        {
            // Glyph r1: the culprit and the caught operative together: aimed 60 % of the way from the op to the culprit, the camera looking
            // from the op's side (culprit above the centre, op below), zoomed so the pair spans about 40 % of the width (people 40-60 px)
            Vector3? op = failOpKey != null && views?.Find(failOpKey) is Transform ot && ot.gameObject.activeSelf ? ot.position : (Vector3?)null;
            var cu = failCulprit; FailCulpritAt = cu + Vector3.up * 2.2f; FailOpAt = op;
            float yawBase = yaw0, size = failZoom; var aim = cu;
            if (op is Vector3 o)
            {
                var d = cu - o; d.y = 0f; float dist = d.magnitude;
                if (dist > 0.5f) { yawBase = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; aim = o + d * (FailShot ? 0.5f : 0.6f); /* Glyph round 3 (N125): a shot is framed on the tracer's midpoint */ size = Mathf.Clamp(dist / (0.8f * Mathf.Max(1f, cam.aspect)), failZoomMin, failZoomMax); } // Glyph r2: a held replay, not play: it may go under play zoom
                if (failClose)
                {
                    // Glyph round 3 (N119): the engine may put the pair on one point (the Enforcer at the guard's door) and the views step the agent
                    // aside (HostViews.Untangle); look across the line between them as drawn, easing toward it, so they stand apart on screen
                    if (failCulpritKey.Length > 0 && views?.Find(failCulpritKey) is Transform ct && ct.gameObject.activeInHierarchy)
                    {
                        var dd = ct.position - o; dd.y = 0f;
                        if (dd.magnitude > 0.3f)
                        {
                            float acr = Mathf.Atan2(dd.x, dd.z) * Mathf.Rad2Deg + 90f; if (Mathf.Abs(Mathf.DeltaAngle(failCloseYaw, acr + 180f)) < Mathf.Abs(Mathf.DeltaAngle(failCloseYaw, acr))) acr += 180f;
                            failCloseYaw = Mathf.MoveTowardsAngle(failCloseYaw, acr, (t <= 0f ? 360f : 90f) * Mathf.Min(Time.unscaledDeltaTime, 0.1f));
                        }
                        cu = new Vector3(ct.position.x, cu.y, ct.position.z);
                    }
                    yawBase = failCloseYaw; aim = (o + cu) / 2f; size = failZoom;
                } // Glyph 022 (N119: the guard stood behind the Enforcer at the door, out of sight): a pair that ends close is seen side by side
                if (!failYawSet) { failYawSet = true; failYawBias = ClearestYaw(aim, size, yawBase, failClose ? 15f : FailShot ? 90f : 45f); /* Glyph round 3 (N125): a shot may look from further round to clear a slab */ } // Glyph 022: the side with the least building between the camera and the pair, once
                yawBase += failYawBias;
                failFade.Clear(); for (int i = 0; i <= 4; i++) { var q = Vector3.Lerp(o, cu, i / 4f); failFade.Add(new Vector4(q.x, 0f, q.z, 4.5f)); } // a 4.5 m capsule along the line, the op and culprit inside it
                // brief 020 (Glyph): the culprit, the op and every signal figure in frame keep a clean person's disc (no dither down a back)
                var people = new List<Vector3> { cu, o }; foreach (var w in FigurePoints()) people.Add(w);
                if (views?.Find("guard") is Transform gt && gt.gameObject.activeInHierarchy) people.Add(gt.position);
                foreach (var w in people)
                {
                    if (failFade.Count >= ArchitectureFade.MaxPoints) break;
                    var vp = cam.WorldToViewportPoint(w); if (vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f) continue;
                    failFade.Add(new Vector4(w.x, w.y, w.z, -personRadius));
                }
                if (fade) fade.SetPoints(failFade);
            }
            float yaw = yawBase - failOrbitDeg / 2f + failOrbitDeg * (t * t * (3f - 2f * t));
            cam.orthographic = true; cam.orthographicSize = size;
            cam.transform.rotation = Quaternion.Euler(failPitch, yaw, 0f);
            var p = new Vector3(aim.x, 0f, aim.z); focus = p;
            cam.transform.position = p - cam.transform.forward * standOff;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, standOff + 1500f);
            if (shadows) shadows.focusDepth = standOff;
            if (fade) fade.focus = p;
            FailOccluders(op);
        }

        /// <summary>Brief 021 (Glyph, N119): props between the camera and the people it shows (the Records Office globe, a crossing orb, a
        /// lamp: drawn whole, or glass the fade cannot cut) are hidden while the fail camera looks through them. Candidates once per start:
        /// small untagged renderers (props under 6 m) or uncuttable ones within 30 m of the pivot, not the game's own views.</summary>
        void FailProps(Vector3 pivot)
        {
            failProps.Clear();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                if (!r.enabled || r.transform.IsChildOf(transform)) continue;
                var b = r.bounds; var d = b.center - pivot; d.y = 0f; if (d.magnitude > 30f + b.extents.magnitude) continue;
                bool small = b.size.x < 6f && b.size.y < 6f && b.size.z < 6f && b.max.y > 0.6f && (r.renderingLayerMask & ArchitectureFade.Layer) == 0; // a prop, not a building tile (the fade cuts those)
                bool uncut = false; foreach (var m in r.sharedMaterials) if (m && !m.shader.keywordSpace.FindKeyword(FadeReach.Keyword).isValid && m.renderQueue >= 2450) { uncut = true; break; }
                if (small || (uncut && b.max.y > 1.5f)) failProps.Add(r);
            }
        }

        /// <summary>For checks: the last start's yaw scores (offset=score).</summary>
        public string FailYawScores { get; private set; } = "";
        float failYawBias, failCloseYaw; bool failYawSet, failClose; string failCulpritKey = ""; readonly List<Bounds> failWalls = new();
        /// <summary>Glyph 022 (N119, the guard at the door: the Office filled half the frame): the yaw offset from `yawBase` (within ±range, 15°
        /// steps) whose view of the pair crosses the fewest building pieces (tiles over 3 m within 45 m) before the ground; rays near the centre,
        /// where the pair stands, count double, and a tie keeps the nearer offset. Once per start.</summary>
        float ClearestYaw(Vector3 aim, float size, float yawBase, float range)
        {
            if (failWalls.Count == 0)
                foreach (var r in Object.FindObjectsByType<MeshRenderer>())
                {
                    if (!r.enabled || r.transform.IsChildOf(transform) || (r.renderingLayerMask & ArchitectureFade.Layer) == 0) continue;
                    var b = r.bounds; if (b.max.y < 3f) continue; var d = b.center - aim; d.y = 0f; if (d.magnitude > 45f + b.extents.magnitude) continue;
                    failWalls.Add(b);
                }
            float best = 0f, bestScore = float.MaxValue; var p = new Vector3(aim.x, 0f, aim.z); var log = new System.Text.StringBuilder($"walls {failWalls.Count}:");
            for (float off = -range; off <= range + 0.01f; off += 15f)
            {
                var q = Quaternion.Euler(failPitch, yawBase + off, 0f); Vector3 fw = q * Vector3.forward, rt = q * Vector3.right, up = q * Vector3.up; var o = p - fw * standOff;
                float score = Mathf.Abs(off) / 60f;
                for (int i = 0; i < 9; i++) for (int j = 0; j < 5; j++)
                {
                    float u = i / 8f - 0.5f, v = j / 4f - 0.5f; var org = o + rt * (u * 1.8f * size * cam.aspect) + up * (v * 1.8f * size);
                    if (fw.y >= -1e-3f) continue; float tg = -org.y / fw.y; var ray = new Ray(org, fw);
                    foreach (var b in failWalls) if (b.IntersectRay(ray, out var dd) && dd < tg - 1f) { score += Mathf.Abs(u) < 0.2f && Mathf.Abs(v) < 0.3f ? 2f : 1f; break; }
                }
                if (score < bestScore - 0.01f) { bestScore = score; best = off; }
                log.Append($" {off:0}={score:0.#}");
            }
            FailYawScores = log.ToString(); return best;
        }

        void FailOccluders(Vector3? op)
        {
            foreach (var r in failHidden) if (r) r.forceRenderingOff = false; failHidden.Clear();
            var fwd = cam.transform.forward; var targets = new List<Vector3> { failCulprit }; if (op is Vector3 o) targets.Add(o);
            if (views?.Find("guard") is Transform gt && gt.gameObject.activeInHierarchy) targets.Add(gt.position);
            foreach (var w in FigurePoints()) targets.Add(w);
            foreach (var r in failProps)
            {
                if (!r) continue; var b = r.bounds; bool hit = false;
                foreach (var t in targets)
                {
                    for (float h = 0.2f; h < 2f && !hit; h += 0.8f)
                    {
                        var at = t + Vector3.up * h; var ray = new Ray(at - fwd * standOff, fwd);
                        if (b.Contains(at)) continue; // what the person stands in (a canopy over the door) is the fade's
                        if (b.IntersectRay(ray, out var dist) && dist < standOff - 0.4f) hit = true;
                    }
                    if (hit) break;
                }
                if (hit) { r.forceRenderingOff = true; failHidden.Add(r); }
            }
        }

        /// <summary>Glyph r1: the culprit's sight as one convex wedge (the hull of the engine's fan: walls clip it), filled at 40 % with a
        /// rim and a line sweeping slowly across it; unlit, on the street, drawn by the fail camera alone.</summary>
        void FailCone(Fan f)
        {
            if (f == null) { if (coneGo) coneGo.SetActive(false); return; }
            if (!coneGo)
            {
                coneGo = new GameObject("fail-cone"); coneGo.transform.SetParent(transform, false);
                coneMesh = new Mesh { name = "fail-cone" }; coneMesh.MarkDynamic(); coneGo.AddComponent<MeshFilter>().sharedMesh = coneMesh;
                var mat = new Material(fanTemplate ? fanTemplate : unlitTemplate); mat.SetColor("_BaseColor", new Color(coneColour.r, coneColour.g, coneColour.b, 0.42f)); if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
                var mr = coneGo.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                LineRenderer Line(string n, float w)
                {
                    var g = new GameObject(n); g.transform.SetParent(coneGo.transform, false); var lr = g.AddComponent<LineRenderer>();
                    var lm = new Material(unlitTemplate); lm.SetColor("_BaseColor", coneColour); lr.sharedMaterial = lm; lr.widthMultiplier = w; lr.useWorldSpace = true; lr.numCapVertices = 2; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return lr;
                }
                coneRim = Line("rim", 0.12f); coneRim.loop = true; coneSweep = Line("sweep", 0.04f); { var sm = coneSweep.sharedMaterial; sm.SetColor("_BaseColor", new Color(coneColour.r, coneColour.g, coneColour.b, 1f)); }
                coneSweep.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.35f)); // tapering: a scan, never a second rim
            }
            coneGo.SetActive(true);
            var hub = frame.World(f.At); var pts = new List<Vector2> { new(hub.x, hub.z) };
            var mid = FailOpAt is Vector3 om ? (om + failCulprit) / 2f : failCulprit; float lim = Mathf.Min(f.Reach / 100f, Vector2.Distance(new Vector2(hub.x, hub.z), new Vector2(mid.x, mid.z)) + 10f); // Glyph r2: clipped to about 10 m round the pair, and never past the watcher's reach
            foreach (var q in f.Points) { var w = frame.World(q); var v = new Vector2(w.x - hub.x, w.z - hub.z); if (v.magnitude > lim) v = v.normalized * lim; pts.Add(new Vector2(hub.x, hub.z) + v); }
            var hull = Hull(pts); if (hull.Count < 3) { coneGo.SetActive(false); return; }
            float y = hub.y + 0.06f; var verts = new List<Vector3>(); foreach (var h in hull) verts.Add(new Vector3(h.x, y, h.y));
            var tris = new List<int>(); for (int i = 1; i + 1 < verts.Count; i++) { tris.Add(0); tris.Add(i); tris.Add(i + 1); }
            coneMesh.Clear(); coneMesh.SetVertices(verts); coneMesh.SetTriangles(tris, 0); coneMesh.RecalculateBounds();
            coneRim.positionCount = verts.Count; coneRim.SetPositions(verts.ToArray());
            // the sweep: from the hub to the hull's far edge, across the wedge's angles and back over 3 s
            float a0 = float.MaxValue, a1 = float.MinValue; float hubA = 0f; var h0 = new Vector2(hub.x, hub.z);
            foreach (var h in hull) { var d = h - h0; if (d.sqrMagnitude < 0.01f) continue; float a = Mathf.Atan2(d.y, d.x); if (a0 == float.MaxValue) hubA = a; a = hubA + Mathf.DeltaAngle(hubA * Mathf.Rad2Deg, a * Mathf.Rad2Deg) * Mathf.Deg2Rad; a0 = Mathf.Min(a0, a); a1 = Mathf.Max(a1, a); }
            float s = Mathf.Lerp(0.25f, 0.75f, Mathf.PingPong(failClock / 1.5f, 1f)), aa = Mathf.Lerp(a0, a1, s); // never near a side (Glyph 022: it read as a double edge)
            // Glyph 022 (N69b, N127: a spike past the cone's left side; N119: a laser to the screen's corner): the sweep ends where it leaves the
            // hull, never at the bare reach
            var dir = new Vector2(Mathf.Cos(aa), Mathf.Sin(aa)); float best = 0f;
            for (int i = 0; i < hull.Count; i++)
            {
                var a = hull[i]; var b = hull[(i + 1) % hull.Count]; var e = b - a; float den = dir.x * e.y - dir.y * e.x; if (Mathf.Abs(den) < 1e-6f) continue;
                var w = a - h0; float t = (w.x * e.y - w.y * e.x) / den, u = (w.x * dir.y - w.y * dir.x) / den;
                if (t > 0.05f && u >= -1e-4f && u <= 1f + 1e-4f && t > best) best = t;
            }
            // Glyph 022 round 2 (N69b, N127: in a still, a sweep that reached the rim read as a second edge, or a spike off it): it stops at 70 %
            // of the way to the rim, tapering, so it always reads as a line inside the wedge
            coneSweep.enabled = best > 0.3f; float reach = best * 0.7f;
            coneSweep.positionCount = 2; coneSweep.SetPosition(0, new Vector3(hub.x, y + 0.01f, hub.z)); coneSweep.SetPosition(1, new Vector3(hub.x + dir.x * reach, y + 0.01f, hub.z + dir.y * reach));
        }

        static List<Vector2> Hull(List<Vector2> p)
        {
            p.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var h = new List<Vector2>(); float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
            foreach (var q in p) { while (h.Count >= 2 && Cross(h[^2], h[^1], q) <= 0) h.RemoveAt(h.Count - 1); h.Add(q); }
            int lo = h.Count + 1; for (int i = p.Count - 2; i >= 0; i--) { var q = p[i]; while (h.Count >= lo && Cross(h[^2], h[^1], q) <= 0) h.RemoveAt(h.Count - 1); h.Add(q); }
            if (h.Count > 1) h.RemoveAt(h.Count - 1); return h;
        }

        /// <summary>Fable's caption and tip for a cause (less.failcam): by the fail code and why (for the seal, why is the origin: desk or heard
        /// when that reported it, else what the seal began from); {by} the culprit in words, {agent} the first operative involved.</summary>
        (string caption, string tip) FailWords(FailCause c, Blamed who)
        {
            var t = Text; string why = c.Why;
            if (c.Fail == "sealed") why = c.Origin?.Reporter?.Kind is "desk" or "heard" ? c.Origin.Reporter.Kind : c.Origin?.Because ?? why;
            string cap = t != null && t.FailCaption.TryGetValue(c.Fail + "|" + why, out var cp) ? cp : (Text?.EndFail.TryGetValue(c.Fail, out var ef) == true ? ef : c.Fail);
            string tip = t != null && t.FailTip.TryGetValue(c.Fail + "|" + why, out var tp) ? tp : "";
            var by = who ?? (c.Culprits.Count > 0 ? c.Culprits[0] : null);
            string byWords = by == null ? "Someone" : by.Kind switch
            {
                "camera" => Cap(t != null && t.TargetNames.TryGetValue("camera-" + by.Id.Replace("camera:", ""), out var cn) ? cn : "a camera"),
                "op" => "The " + (t != null && t.Speakers.TryGetValue(by.Id, out var sp) ? sp : Cap(by.Id)),
                "person" => t != null && t.TargetNames.TryGetValue(by.Id, out var pn) ? Cap(pn) : (t != null && t.FailBy.TryGetValue("person", out var ap) ? ap : "A passer-by"),
                _ => t != null && t.FailBy.TryGetValue(by.Id == "rivals" ? "rivals" : by.Kind, out var bw) ? bw : Cap(by.Id),
            };
            string agent = c.Ops.Count > 0 ? "the " + (t != null && t.Speakers.TryGetValue(c.Ops[0], out var ag) ? ag : c.Ops[0]) : "us";
            string Fill(string s) => s.Replace("{by}", byWords).Replace("{agent}", agent);
            return (Fill(cap), Fill(tip));
        }
    }
}
