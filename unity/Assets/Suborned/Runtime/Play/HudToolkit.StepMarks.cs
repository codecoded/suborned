using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    /// <summary>The end steps' marks (moved here from HudToolkit.Less by brief 101 step 6): the door marker, the car marker, the held car's lock and count, our car's five seat
    /// pips, the exit lane and the hold ring on the ground, and the holders' marks with their tethers. Brief 101 step 6 (Glyph's S mocks, briefs/101/glyph/r2/S-busy.png):
    /// the held car's lock and count and the seat pips are laid on the car as street marks (ink on a cream keyline, pips in an ink keyline, no backing pill or disc); a
    /// holder's mark is a struck disc in his signal colour (amber police and guards, violet rivals); the rival car's hold words are a street label.</summary>
    public sealed partial class HudToolkit
    {
        // ---------------------------------------------------------------- the end steps (Sky 2026-10-18: "it's really unclear on how to take Vale")

        VisualElement doorMark, carMark, heldMark, doorNext, doorTurn; Capsule doorNextCap; KeyText heldK; bool doorHover;
        readonly Dictionary<string, (Vector3 at, float lost)> holderSeen = new(); readonly HashSet<string> holderIds = new();
        /// <summary>The door marker while Enter is open to the Enforcer (a dashed ring with the Enter glyph, the hint ring's grammar, quiet: no
        /// pulse), with his progress round it while he is inside; the car marker once he is out with Vale. A click gives the order (HostValeSteps).
        /// His first knob carries a dashed ring while either is open, unless it is already armed.</summary>
        /// <summary>Glyph techwatch r2 B: the door marker's unknown eye as strokes on the flat ink disc (the kit's eye_unk at 14 px drew its "?" at
        /// about 1.75 px, its hook on the disc's rim): the eye and its ring pupil at 1.6 px, the "?" at 1.5 px, its hook 1 px further in.</summary>
        /// <summary>Glyph r3: captured as handcuffs drawn as strokes (the kit's "restrain" read as an infinity sign at 12 px): two open C cuffs,
        /// their gaps at the bottom, joined by a chain bar, in a 16 px box scaled to the element.</summary>
        static void CuffsPaint(Painter2D p, Rect r, Color c)
        {
            float k = Mathf.Min(r.width, r.height) / 16f; Vector2 P(float x, float y) => new(x * k, y * k);
            p.strokeColor = c; p.lineWidth = 1.9f * k; p.lineCap = LineCap.Round;
            foreach (var cx in new[] { 4.3f, 11.7f })
            {
                float a0 = 125f, a1 = 415f, rr = 3.3f; var c0 = new Vector2(cx, 10.4f);
                p.BeginPath(); p.MoveTo(P(c0.x + rr * Mathf.Cos(a0 * Mathf.Deg2Rad), c0.y + rr * Mathf.Sin(a0 * Mathf.Deg2Rad))); p.Arc(P(c0.x, c0.y), rr * k, new Angle(a0, AngleUnit.Degree), new Angle(a1, AngleUnit.Degree)); p.Stroke();
            }
            p.lineWidth = 1.6f * k; p.BeginPath(); p.MoveTo(P(6.4f, 6.4f)); p.LineTo(P(9.6f, 6.4f)); p.Stroke(); // the chain bar
            p.fillColor = c; foreach (var x in new[] { 6.4f, 9.6f }) { p.BeginPath(); p.MoveTo(P(x + 1.1f, 6.4f)); p.Arc(P(x, 6.4f), 1.1f * k, new Angle(0, AngleUnit.Degree), new Angle(360, AngleUnit.Degree)); p.Fill(); }
        }
        /// <summary>A 16 px cuffs glyph in a row (relative, so flex lays it out).</summary>
        static VisualElement Cuffs(VisualElement parent, Color c, float size = 16) { var e = P(parent, 0, 0, size, size, (p, r) => CuffsPaint(p, r, c)); e.style.position = Position.Relative; e.style.left = StyleKeyword.Auto; e.style.top = StyleKeyword.Auto; e.style.flexShrink = 0; return e; }

        static void UnknownEye(Painter2D p, Rect r)
        {
            // every subpath starts with its own MoveTo (a path whose first command was a curve drew a stray line from the element's origin)
            p.strokeColor = Cream; p.lineWidth = 1.6f; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            p.BeginPath(); p.MoveTo(new Vector2(6.6f, 15.2f)); p.QuadraticCurveTo(new Vector2(12.2f, 9.4f), new Vector2(17.8f, 15.2f)); p.Stroke();
            p.BeginPath(); p.MoveTo(new Vector2(17.8f, 15.2f)); p.QuadraticCurveTo(new Vector2(12.2f, 21f), new Vector2(6.6f, 15.2f)); p.Stroke();
            p.BeginPath(); p.MoveTo(new Vector2(14.3f, 15.2f)); p.Arc(new Vector2(12.2f, 15.2f), 2.1f, Deg(0), Deg(360)); p.Stroke();
            p.lineWidth = 1.5f; // the "?": a hook over the eye's right corner, its dot just above the lid
            var h0 = new Vector2(16f, 7.4f) + new Vector2(Mathf.Cos(185f * Mathf.Deg2Rad), Mathf.Sin(185f * Mathf.Deg2Rad)) * 1.8f;
            p.BeginPath(); p.MoveTo(h0); p.Arc(new Vector2(16f, 7.4f), 1.8f, Deg(185), Deg(395)); p.LineTo(new Vector2(16f, 9.8f)); p.Stroke();
            p.fillColor = Cream; p.BeginPath(); p.MoveTo(new Vector2(16.85f, 11.5f)); p.Arc(new Vector2(16f, 11.5f), 0.85f, Deg(0), Deg(360)); p.Fill();
        }

        readonly List<VisualElement> holderEls = new(); int holderUsed; readonly Dictionary<VisualElement, KeyText> holderWords = new();
        VisualElement HolderEl()
        {
            while (holderEls.Count <= holderUsed)
            {   // brief 101 step 6 (Glyph's S mock): the holder mark is a struck disc in the holder's signal colour on an ink keyline (amber police and guards, violet rivals), 20 px in a 26 px box
                var d = E(marks, null, 0, 0, 26, 26); var k = E(d, "e-ink", 1.5f, 1.5f, 23, 23); Round(k, 11.5f); var f = E(k, null, 1.5f, 1.5f, 20, 20); f.name = "fill"; Round(f, 10f);
                var st = E(f, "e-ink", 3.5f, 9f, 13, 2.6f); st.style.rotate = new Rotate(-45f); Round(st, 1.3f); manifest.Add((d, "image", () => "holder")); holderEls.Add(d);
            }
            var el = holderEls[holderUsed++]; Show(el, true); el.style.scale = StyleKeyword.Null; return el;
        }

        void StepMarks(float px, float now)
        {
            holderUsed = 0; seatPill = default;
            VisualElement Mk(string which)
            {
                // brief 101 round 3 (Glyph's rule 2): a round tab (the book's body, the cream ring, a soft shadow); the unknown eye drawn as strokes on it
                var e = RoundTab(marks, 26f, "", true); e.style.position = Position.Absolute; E(e, null, 0, 0, 0, 0).name = "flat"; P(e, 0, 0, 26, 26, UnknownEye).name = "unk";
                e.RegisterCallback<PointerDownEvent>(ev => { if (ev.button != 0) return; ev.StopPropagation(); host.StepMarkClick(which); });
                if (which == "door") { e.RegisterCallback<PointerEnterEvent>(_ => doorHover = true); e.RegisterCallback<PointerLeaveEvent>(_ => doorHover = false); }
                manifest.Add((e, "image", () => which + " marker")); return e;
            }
            doorMark ??= Mk("door"); carMark ??= Mk("car");
            if (heldMark == null)
            {   // the held car: a lock and the number of hostiles holding it, no words (Glyph fight r1 A)
                // brief 101 step 6 (Glyph's S mock): the lock and the holders' count laid on the car as street type (ink on its cream keyline), no disc
                heldMark = StackRow(marks, 26f); heldMark.style.position = Position.Absolute; heldMark.style.justifyContent = Justify.FlexStart; heldMark.pickingMode = PickingMode.Position;
                KIcon(heldMark, "lock", 18f); heldK = KText(heldMark, 16f); heldK.Root.style.marginLeft = 3;
                heldMark.RegisterCallback<PointerDownEvent>(ev => { if (ev.button != 0) return; ev.StopPropagation(); host.StepMarkClick("car"); });
                manifest.Add((heldMark, "image", () => "car held marker")); Show(heldMark, false);
            }
            var g = host.Game; bool live = g != null && !m.Ended && !m.BriefingOpen;
            bool door = live && (host.DoorMarked || host.EnterProgress >= 0), car = live && (host.BoardAvailable || host.GoAvailable); // brief 018: Board for the Enforcer with Vale, then Go once anyone is in
            // Glyph's ruling: the door from what the cell learned: go (turquoise, the Enter glyph), unknown (cream, "?" eye), seen (the watcher's eye, no ring)
            string ds = host.EnterProgress >= 0 ? "go" : host.DoorState; Color dc = ds == "unk" ? Cream : TurqLight;
            string dIcon = ds == "unk" ? "eye_unk" : ds == "seen" ? "eye_open" : "enter"; Color dIconC = ds == "seen" ? (host.DoorEye == "camera" ? Red : Amber) : dc;
            void Place(VisualElement e, bool on, Vector3 w, string icon, bool ringIt, int progress, Color col, Color iconCol)
            {
                if (!on || !Proj(w, out var q)) { Show(e, false); return; }
                Show(e, true); e.style.left = q.x - 13; e.style.top = q.y - 13; Icon(e.Q("g"), icon, iconCol);
                { float pt = Time.unscaledTime - (e == carMark ? host.CarPulseAt : e == doorMark ? host.DoorPulseAt : -9f), pu = pt >= 0f && pt < 0.4f ? Mathf.Sin(pt / 0.4f * Mathf.PI) : 0f; e.style.scale = new Scale(Vector2.one * (1f + 0.3f * pu)); } // brief 067 G1 (Glyph): a portrait click on an agent aboard pulses the car marker (inside, the door marker)
                // Glyph techwatch fix 4: the marker reads whole on a ghosted tower's dither: an ink halo under the disc, the dashes on an ink
                // underlay (the 018 tethers' grammar), and the unknown eye 2 px smaller, so its "?" sits inside the bezel, not on it
                var gl = e.Q("g"); bool small = icon == "eye_unk"; Show(gl, !small); Show(e.Q("flat"), small); if (e.Q("unk") is VisualElement uk) Show(uk, small); // flat ink under the unknown eye (the disc art's gloss ate the "?"); the eye and its "?" drawn as strokes (UnknownEye)
                if (ringIt) GroundRing(w, 46f * px / 2f * GroundPxM, col, 2f, true, 0.6f); // the dashed ring on the ground round the marker's foot (Glyph's leftover: depth-tested, behind legs; brief 046)
                if (progress >= 0) { var c = q; float f = progress / 1000f; paints.Add(pp => { RingArc(pp, c, 16, 1f, new Color(1, 1, 1, 0.22f), 2.6f); RingArc(pp, c, 16, f, TurqLight, 2.6f); }); }
            }
            bool hintHere(string id) => host.HintNow?.Id == id && hintAlpha > 0f; // the hint's own ring stands there: no second ring
            if (g != null)
            {
                // Glyph's 0.2.94 review #2: a target ring at the post (the guard on his door, amber) is never under the marker's disc: the marker
                // steps down the screen, clear of the ring, and its own dashed ground ring stays off (the guard's ring marks the spot)
                var post = host.frame.World(g.S.At(g.S.GuardPost)); bool ringAtPost = false;
                foreach (var tr in host.TargetRings) { var dd = tr.At - post; dd.y = 0f; if (dd.sqrMagnitude < 1.44f) { ringAtPost = true; break; } }
                var doorAt = post; if (ringAtPost) { var (gUp, _) = GroundAxes(); doorAt = post - gUp * (RingR + TickOut + 0.75f); }
                Place(doorMark, door, doorAt, dIcon, !ringAtPost && host.EnterProgress < 0 && ds != "seen" && !hintHere("H14") && !hintHere("H13"), host.EnterProgress, dc, dIconC); // Glyph 016 r2: the ring round the marker only (it touched the Z)
                // GDD v0.24: a wall hides the door at this yaw: a small turn glyph on the marker's shoulder, pulsing slowly; a click turns the view in
                // quarter steps to the nearest yaw where the door is clear (HostDoorView.TurnToDoor)
                if (doorTurn == null)
                {
                    doorTurn = RoundTab(marks, 23f, "turn_view", true); doorTurn.style.position = Position.Absolute; // brief 101 round 3: a round tab
                    doorTurn.RegisterCallback<PointerDownEvent>(ev => { if (ev.button != 0) return; ev.StopPropagation(); host.TurnToDoor(); });
                    manifest.Add((doorTurn, "image", () => "door turn")); Show(doorTurn, false);
                }
                {
                    bool turnOn = door && host.EnterProgress < 0 && doorMark.resolvedStyle.display != DisplayStyle.None && host.DoorHiddenNow && host.DoorTurnSteps() != 0;
                    Show(doorTurn, turnOn);
                    if (turnOn)
                    {
                        doorTurn.style.left = doorMark.style.left.value.value - 26; doorTurn.style.top = doorMark.style.top.value.value - 13; // the upper left shoulder (the next-step capsule takes the right)
                        float k = 1f + 0.09f * Mathf.Sin(now * 2.6f); doorTurn.style.scale = new Scale(new Vector3(k, k, 1f)); // a slow pulse
                    }
                }
                // "unk": who learns it next (Sky 2026-10-18, the Tech watches machines): the Intel chip's grammar beside the marker, the agent's
                // cameo and the Watch glyph, its rim turquoise when that agent is the one selected; a click selects, a second gives the Watch
                if (doorNext == null)
                {
                    // brief 101 round 3 (Glyph's rule 2): a capsule, the agent's face and the Watch's pictogram, its inline turquoise while that agent is selected
                    doorNextCap = NewCapsule(marks, 24f, true, () => host.DoorNextClick()); doorNext = doorNextCap.Root; doorNext.style.position = Position.Absolute;
                    manifest.Add((doorNext, "image", () => "door next step")); Show(doorNext, false);
                }
                {
                    var nx = door && ds == "unk" && doorMark.resolvedStyle.display != DisplayStyle.None ? host.DoorNext() : null;
                    Show(doorNext, nx.HasValue);
                    if (nx is { } dn)
                    {
                        bool sel = host.Selected[(int)dn.by] && host.Agent == dn.by;
                        doorNext.style.left = doorMark.style.left.value.value + 26 + 6; doorNext.style.top = doorMark.style.top.value.value + 1;
                        SetCapsule(doorNextCap, "watch", "", (int)dn.by, live: sel); // the live accent when selected
                    }
                }
                // brief 018 grammar (Glyph fight r1 A-C): one car marker whose shape is its state. In the final fight the hold reach is a ring on
                // the ground, solid and quiet while the car is free, dashed while it is held; the marker turns to a lock with the holder count; each
                // holder carries a lock (and, out on the lane, a tether to the car), and both fade over a beat when it stops holding.
                var carW = host.GetawayWorld; bool ring = host.CarRingOn, held = ring && host.CarHeldNow; seatPill = default;
                var holders = held ? host.CarHolders() : new List<(string id, Vector3 at)>();
                Place(carMark, (car || ring) && !held, carW, "car", !ring && !hintHere("H15"), -1, TurqLight, TurqLight);
                if (held && !(host.Ring?.Plan?.T?.Kind == "car") && Proj(carW, out var cq0)) { Show(heldMark, true); /* Glyph's 0.2.112 check: under an open car ring the marker sat on the ring's middle, its lock and count against the car pip; the Go pill shows the lock and the holders there */ heldK.Set(holders.Count.ToString()); heldMark.style.left = cq0.x - 9; heldMark.style.top = cq0.y - 13; }
                else Show(heldMark, false);
                // brief 018 §6.3: five seat pips under the car marker (Vale, then the Scout, Fixer, Tech, Enforcer), filled as each boards; shown with
                // the marker, the held lock, or anyone in the car. Vale's pip is the larger.
                if (live && (car || ring || g.AnyInCar || host.AnyBoardPending) && Proj(carW, out var sq0))
                {
                    var (seats, seatPend, seatAt) = host.SeatState(); var sc = sq0 + new Vector2(0, 21); float pnow = Time.unscaledTime; // brief 067 C3 (Glyph): pending pips turn; a landing beats
                    seatPill = Rect.MinMaxRect(sc.x - 29f, sc.y - 7f, sc.x + 29f, sc.y + 7f); // StepAside: plates keep off it
                    paints.Add(pp =>
                    {   // brief 101 step 6 (Glyph's S mock): five pips laid on the car, 8 px each in a 1.4 px ink keyline, 11 px apart, no backing pill: turquoise once aboard, the
                        // book's page cream while empty (Vale's, the first, a little larger); a pending seat turns three turquoise dashes, a landing beats
                        for (int k = 0; k < 5; k++)
                        {
                            var c = sc + new Vector2((k - 2) * 11f, 0); float r = k == 0 ? 4.6f : 4f;
                            pp.fillColor = BkInk; pp.BeginPath(); pp.Arc(c, r + 1.4f, Deg(0), Deg(360)); pp.Fill();
                            if (seats[k])
                            {
                                float bt = pnow - seatAt[k], beat = bt >= 0f && bt < 0.3f ? Mathf.Sin(bt / 0.3f * Mathf.PI) : 0f; // the landing: 1 → 1.4 → 1 over 0.3 s, and one ring leaving it
                                pp.fillColor = BkTurq; pp.BeginPath(); pp.Arc(c, r * (1f + 0.4f * beat), Deg(0), Deg(360)); pp.Fill();
                                if (bt >= 0f && bt < 0.3f) { float u = bt / 0.3f; pp.strokeColor = new Color(BkTurq.r, BkTurq.g, BkTurq.b, 0.8f * (1f - u)); pp.lineWidth = 1.4f; pp.BeginPath(); pp.Arc(c, r * (1f + 1.2f * u), Deg(0), Deg(360)); pp.Stroke(); }
                            }
                            else
                            {
                                pp.fillColor = StPage; pp.BeginPath(); pp.Arc(c, r, Deg(0), Deg(360)); pp.Fill();
                                if (seatPend[k]) { float rot = pnow / 1.5f * 360f; pp.strokeColor = BkDeep; pp.lineWidth = 1.6f; for (int dsh = 0; dsh < 3; dsh++) { float a0 = rot + dsh * 120f; pp.BeginPath(); pp.Arc(c, r - 1.2f, Deg(a0), Deg(a0 + 70f)); pp.Stroke(); } } // pending: three dashes turning once in 1.5 s
                            }
                        }
                    });
                }
                // brief 018 §6.1: the exit lane (site getaway.exit), drawn with the ring only in the final fight: its two sides on the ground.
                // Glyph r123 F1 (batch 0.2.123 pre-flight): both are ground decals now (HudToolkit.Ground: depth-tested, laid on the drawn floor),
                // not screen paints, so the Office roof hides them at yaw 30 and the occlusion cut shows them round the car. Held: a static dash;
                // free: solid at 55 %. The lane is never drawn past 12 m of the car.
                if (ring && !host.Following) // the follow view keeps its own rings only (StreetLess's Ground)
                {
                    var (lane, half) = host.ExitLane();
                    if (lane.Count >= 2 && half > 0)
                    {
                        var laneCol = new Color(TurqLight.r, TurqLight.g, TurqLight.b, held ? 1f : 0.55f);
                        foreach (int sgn in new[] { -1, 1 })
                        {
                            var side = new List<Vector3>(lane.Count);
                            for (int k = 0; k < lane.Count; k++)
                            {
                                var dir = (k < lane.Count - 1 ? lane[k + 1] - lane[k] : lane[k] - lane[k - 1]); dir.y = 0; dir.Normalize(); var nrm = new Vector3(-dir.z, 0, dir.x);
                                side.Add(lane[k] + nrm * half * sgn);
                            }
                            foreach (var piece in ClipToDisc(side, carW, CarMarkReachM)) FloorPath(piece, laneCol, held ? 2.2f : 1.4f, held, held ? 0.45f : 0.3f);
                        }
                    }
                }
                float holdM = host.CarHoldM; bool ringDrawn = ring && holdM > 0 && holdM <= CarMarkReachM;
                if (ringDrawn && !host.Following) // brief 018 §6.1: the 8 m ring (a reach over 12 m would fill the screen: noise, not a ring); Glyph 022: the quiet ring 2.4 px (1.4 read as a hairline)
                    FloorRing(carW, holdM, new Color(TurqLight.r, TurqLight.g, TurqLight.b, held ? 1f : 0.55f), held ? 2.6f : 2.4f, held ? 32 : 0, held ? 0.85f : 0.5f);
                foreach (var (id, at) in holders) holderSeen[id] = (at, -1f);
                holderIds.Clear(); foreach (var (id, _) in holders) holderIds.Add(id);
                var gone = new List<string>();
                foreach (var kv in new List<KeyValuePair<string, (Vector3 at, float lost)>>(holderSeen)) // a copy: the loop writes the lost time back (Mono's Dictionary throws on a write during enumeration)
                {
                    var (hw0, lost) = kv.Value; if (!holderIds.Contains(kv.Key) && lost < 0) { lost = now; holderSeen[kv.Key] = (hw0, lost); }
                    float alpha = lost < 0 ? 1f : 1f - (now - lost) / 0.4f; if (alpha <= 0f || !ring) { gone.Add(kv.Key); continue; }
                    if (!Proj(hw0 + Vector3.up * 2.4f, out var hq) || !Proj(hw0, out var fq)) continue;
                    // Glyph N1 (0.2.137, GDD §10 getaway): the holder marks and tethers stay in a fight (G6's cut undone). A holder who is also the
                    // focus: his holder disc stands right of the lock disc, at its height, 4 px clear of the disc (of its dashed ring while it waits), never over it or under the pips
                    var e = HolderEl(); e.style.left = hq.x - 13; e.style.top = hq.y - 13; e.style.opacity = alpha; e.Q("fill").style.backgroundColor = kv.Key == "car:rivals" || PlayText.RoleOfId(kv.Key) == "rival" ? RingViolet : RingAmber; // brief 101 step 6: the holder's signal colour (Glyph r2's no-entry sign before)
                    if (reticles.TryGetValue(kv.Key, out var hr) && hr.OnScreen && hr.Gone < 0f) { e.style.left = hr.At.x + ReticleD / 2f + (hr.Locked ? 0f : RingPast) + 4f; /* 4 px past the disc, or past its dashed ring while it waits (0.2.138: a locked disc stood 8 px off) */ e.style.top = hr.At.y - ReticleD - 6f; }
                    { string rw = kv.Key == "car:rivals" ? host.RivalsHoldShort() : ""; bool rOn = rw.Length > 0; if (!holderWords.TryGetValue(e, out var nk) && rOn) { nk = holderWords[e] = KText(e, 14f, "k-semi"); nk.Root.style.position = Position.Absolute; nk.Root.style.left = 30; nk.Root.style.top = 2; nk.Root.name = "np"; }
                      if (nk != null) { Show(nk.Root, rOn); if (rOn) nk.Set(rw); } } // engine 0.33.0: the rival car holds the car until its first rider steps out; Fable's fillLockShort for car-held by the rival car ("Rivals coming · 18 s"): a street label (brief 101 step 6)
                    float pulse = now - host.HolderPulseAt; if (pulse >= 0 && pulse < 1f) { float sc2 = 1f + 0.3f * Mathf.Sin(pulse * Mathf.PI * 3f) * (1f - pulse); e.style.scale = new Scale(new Vector2(sc2, sc2)); } else e.style.scale = new Scale(Vector2.one); // §6.5: a refused Go pulses the holders
                    // Glyph r123 §2: no tether for a holder inside the hold ring (the ring, his no-entry disc and the lock's count say it); one only for
                    // a holder out on the lane, 1 px thinner, at 60 %
                    { var dh = hw0 - carW; dh.y = 0f; if (ringDrawn && dh.magnitude <= holdM) continue; }
                    if (!Proj(carW, out var eq)) continue; eq += (fq - eq).normalized * 15f; // Glyph r3: to the car marker's disc edge (its centre is carW; the seat pill hangs below it)
                    var a = fq; var b = eq; float al = alpha * 0.6f;
                    paints.Add(pp => // Glyph r3: a dotted tether (cream dots every 7 px on ink), a rhythm unlike the lane's dashes, from the holder's feet to the car's disc
                    {
                        float len = Vector2.Distance(a, b); if (len < 1f) return; var dir = (b - a) / len;
                        for (int pass = 0; pass < 2; pass++)
                        {
                            pp.fillColor = pass == 0 ? new Color(Ink.r, Ink.g, Ink.b, 0.7f * al) : new Color(Cream.r, Cream.g, Cream.b, al); float r0 = pass == 0 ? 1.8f : 0.8f;
                            for (float d0 = 0; d0 <= len; d0 += 7f) { var c0 = a + dir * d0; pp.BeginPath(); pp.MoveTo(c0 + new Vector2(r0, 0)); pp.Arc(c0, r0, Deg(0), Deg(360)); pp.Fill(); }
                        }
                    });
                }
                foreach (var k in gone) holderSeen.Remove(k);
                for (int i = holderUsed; i < holderEls.Count; i++) Show(holderEls[i], false);
            }
            else { Show(doorMark, false); Show(carMark, false); }
            knobOn = live && m.Agent == (int)OpId.Enforcer && (host.EnterAvailable || host.ExtractAvailable) && host.Picked != 0; // the row's Enter tab wears the dashed ring (HudToolkit.VerbRow)
        }
    }
}
