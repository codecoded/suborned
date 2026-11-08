using System.Collections;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 085, look C: the scene's line, face, Listen's framing and held moments, and the card after the cut, by real input. Split from Proof085Scenes; it reads the words from the file, never pinning a line.</summary>
    public static partial class Proof085Scenes
    {
        /// <summary>The Listen on the street before it lands: he walks to the spot and waits, side on; she stops, Milena walks out, they face each other; nobody can click Milena.</summary>
        static IEnumerator ListenOnStreet(HandoverHost h, RouteRun r, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            var g = h.Game; var inez = h.Views.Find("waitress"); var sc = h.Views.Find("op/scout"); bool waited = false; string plate = ""; h.LookAt(inez.position);
            for (int i = 0; i < 1200 && g.Ops[0].Busy?.Do != "listen" && !g.Over && !h.ConvoPending; i++) { Step(h, r, g.Tick + 1); if (g.ListenSpotNow("scout") is { waiting: true } && !waited) { waited = true; plate = h.PlateWords(0); h.LookAt(sc.position); yield return Frames(3); yield return Shoot(dir, "r085-scout-waiting", log); } }
            var spot = g.ListenSpotNow("scout"); int tick0 = g.Tick; row("r085listenSpot", g.Ops[0].Busy?.Do == "listen" && spot != null, $"under way at {HandoverHost.MissionClock(g.Tick)}; spot {spot?.at} chair '{spot?.chair}', waited {waited}, plate '{plate}'; Scout {Vector3.Distance(sc.position, inez.position):0.0} m from Inez");
            h.LookAt(inez.position); h.SetSpeed(1); h.SetPaused(false); float t0 = Time.unscaledTime; bool midShot = false; while (h.Views.ListenPartnerAlong < 1f && Time.unscaledTime - t0 < 12f) { if (!midShot && h.Views.ListenPartnerAlong > 0.45f) { midShot = true; h.SetPaused(true); yield return Frames(2); yield return Shoot(dir, "r085-partner-walking", log); h.SetPaused(false); } yield return null; }
            float took = Time.unscaledTime - t0; yield return new WaitForSecondsRealtime(0.7f); h.SetPaused(true); yield return Frames(3); yield return Shoot(dir, "r085-partner-talking", log);
            var mil = h.Views.Find(h.Views.ListenPartnerUp("waitress") ?? ""); float gap = mil ? Vector3.Distance(mil.position, inez.position) : 99f; var inezFace = Quaternion.Euler(0f, h.Views.FigureYaw("waitress") ?? 0f, 0f) * Vector3.forward; float toMil = mil ? Vector3.Angle(inezFace, mil.position - inez.position) : 180f; var scFace = Quaternion.Euler(0f, h.Views.FigureYaw("op/scout") ?? 0f, 0f) * Vector3.forward; var scTo = inez.position - sc.position; scTo.y = 0f; float scAt = Vector3.Angle(scFace, scTo);
            bool pick = false; foreach (var q in h.Pickables()) if (mil && q.T?.Key == mil.name) pick = true;
            row("r085partnerOut", mil && h.Views.ListenPartnerAlong >= 1f && gap < 1.8f && toMil < 30f && !pick, $"walked out in {took:0.0} s; {gap:0.00} m from Inez, Inez {toMil:0} deg off her, pickable {pick}; clip {h.Views.FigureClip(mil ? mil.name : "").Item1}");
            var chair = g.SeatedAt("scout"); bool seated = h.Views.SeatedDrawn("op/scout"); float chairOff = chair is { } ch0 && h.CafeSeats != null && h.CafeSeats.Chairs.TryGetValue(ch0.chair, out var cs) ? Mathf.Abs(Mathf.DeltaAngle(h.Views.FigureYaw("op/scout") ?? 0f, Mathf.Atan2(h.frame.Dir(cs.face).x, h.frame.Dir(cs.face).z) * Mathf.Rad2Deg)) : 180f;
            row("r085scoutSideOn", chair != null ? seated && chairOff < 5f : scAt > 60f, chair != null ? $"seated in {chair.Value.chair} as a customer, facing the table ({chairOff:0} deg off the chair's way), {scAt:0} deg off looking at her; clip {h.Views.FigureClip("op/scout").Item1}" : $"standing, {scAt:0} deg off looking at her, clip {h.Views.FigureClip("op/scout").Item1}, plate '{h.PlateWords(0)}'"); // engine 0.49.0: seated, she faces the table as a customer does; standing, side on
        }

        /// <summary>The Scout not beside her (the routes still walk him to beside-inez): 4 m off, as the rule has him: the plate while he waits, and the shot frames the pair with him off to the side.</summary>
        static IEnumerator ListenApart(HandoverHost h, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            yield return Fresh(h, ConversationScenes.Always); var g = h.Game; var cam = h.View; var r = new RouteRun { Orders = Route("route-a").FindAll(o => o.Tick < 500 && !(o.Tick == 0 && o.Op == "scout")) };
            var door = g.S.At(g.S.GuardCafe); var pt = new Pt(door.X + 450, door.Y + 250); h.Issue(new Order { Op = "scout", Do = "move", Point = g.Walk.NearestFree(pt) ?? pt });
            bool waited = false; string plate = ""; var inez = h.Views.Find("waitress"); var sc = h.Views.Find("op/scout"); h.LookAt(inez.position);
            for (int i = 0; i < 1500 && g.Ops[0].Busy?.Do != "listen" && !g.Over && !h.ConvoPending; i++) { Step(h, r, g.Tick + 1); if (g.ListenSpotNow("scout") is { waiting: true } && !waited) { waited = true; plate = h.PlateWords(0); yield return Frames(3); yield return Shoot(dir, "r085-scout-waiting", log); } }
            var spot = g.ListenSpotNow("scout"); h.SetPaused(false); float t0 = Time.unscaledTime; while (h.Views.ListenPartnerAlong < 1f && Time.unscaledTime - t0 < 14f) yield return null; yield return new WaitForSecondsRealtime(0.5f); h.SetPaused(true); yield return Frames(3); yield return Shoot(dir, "r085-apart-street", log);
            float dist = Vector3.Distance(sc.position, inez.position); var clip = h.Views.FigureClip("op/scout").Item1; var got = new bool[1]; yield return ToScene(h, r, g.Tick + 900, got); var c = h.ConvoNow;
            if (!got[0] || c == null) { row("r085listenApart", false, $"no scene: {h.ConvoLast}; {dist:0.0} m, spot {spot?.at} '{spot?.chair}'"); yield return EndScene(h); yield break; }
            var mil = h.Views.Find(h.Views.ListenPartnerUp("waitress") ?? ""); Vector3 VP(Transform t) => cam.WorldToViewportPoint(t.position + Vector3.up * 1.2f); var vm = VP(mil); var vi = VP(inez); var vs = VP(sc); bool pairIn = vm.z > 0f && vi.z > 0f && vm.x > 0.05f && vm.x < 0.95f && vi.x > 0.05f && vi.x < 0.95f, inWay = vs.z > 0f && vs.z < Mathf.Min(vm.z, vi.z) && vs.x > Mathf.Min(vm.x, vi.x) - 0.08f && vs.x < Mathf.Max(vm.x, vi.x) + 0.08f;
            yield return Shoot(dir, "r085-apart-shot", log);
            row("r085listenApart", dist > 2.5f && dist < 6f && c.LivePartner && c.Shot == "profile" && pairIn && !inWay, $"Scout {dist:0.0} m from Inez, spot {spot?.at} '{spot?.chair}', waited {waited}, plate '{plate}', clip {clip}; {c.Shot}, partner live {c.LivePartner}; Milena {vm:0.00}, Inez {vi:0.00}, Scout {vs:0.00}, gap {Vector3.Distance(mil.position, inez.position):0.00} m");
            yield return EndScene(h); h.SetPaused(false); float t1 = Time.unscaledTime; while (h.Views.ListenPartnerAlong >= 0f && Time.unscaledTime - t1 < 16f) yield return null; row("r085partnerBack", h.Views.ListenPartnerAlong < 0f, $"gone in after {Time.unscaledTime - t1:0.0} s"); h.SetPaused(true);
        }

        /// <summary>The chair (engine 0.49.0, brief 098: "Sit them down if they're listening"; sitting is the strong default): the engine walks her into the seat, the last step across the
        /// table's ground at a walk, and sits her (Game.SeatedAt). Drawn, she walks that step (no slide, no jump: the figure never faster than a stride over a walk) and sits on the chair as
        /// a customer does, facing the table, by Forma's clips (HostSeats.OpSeat).</summary>
        static IEnumerator ListenOnChair(HandoverHost h, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            yield return Fresh(h, ConversationScenes.Never); var g = h.Game; string got = ""; h.Issue(new Order { Op = "scout", Do = "watch", Target = "waitress" }); for (int i = 0; i < 1500 && !g.PersonByRole("waitress").KRoutine; i++) h.RunTo(g.Tick + 5);
            if (g.ListenSpotAhead("scout", "waitress") is { } a0 && a0.chair.Length > 0) { got = a0.chair; h.Issue(new Order { Op = "scout", Do = "listen", Target = "waitress" }); } // from where she watched: the strong default
            else foreach (var name in h.CafeSeats.Chairs.Keys)
            {
                var st = g.Walk.NearestFree(g.People2.Places.TryGetValue(name, out var pl) ? pl : default); if (!st.HasValue || g.Walk.Route(g.Ops[0].Pos, st.Value) == null) continue;
                h.Issue(new Order { Op = "scout", Do = "move", Point = st.Value }); for (int i = 0; i < 900 && g.Ops[0].Walk != null; i++) h.RunTo(g.Tick + 1);
                if (g.ListenSpotAhead("scout", "waitress") is { } a && a.chair.Length > 0) { got = a.chair; h.Issue(new Order { Op = "scout", Do = "listen", Target = "waitress" }); break; }
            }
            if (got.Length == 0) { row("r085scoutChair", false, "no chair came up from any table"); yield break; }
            var seat = g.People2.Places[got]; for (int i = 0; i < 1500 && g.Ops[0].Walk != null && !G.Within(g.Ops[0].Pos, seat, 300) && !g.Over; i++) h.RunTo(g.Tick + 1); // headless to her last 3 m
            var sc = h.Views.Find("op/scout"); var seatW = h.frame.World(seat); h.LookAt(sc.position); h.SetSpeed(1); h.SetPaused(false);
            Vector3 Feet() { foreach (var (k, feet, _) in h.Views.FigureFeet()) if (k == "op/scout") return feet; return sc.position; }
            float most = 0f, t0 = Time.unscaledTime; var was = Feet(); string walked = ""; bool stepShot = false; int from = g.Tick + 2; // the view catches up the headless ticks first (one lerp over them): measured from the second tick played in time
            while (!h.Views.SeatedDrawn("op/scout") && Time.unscaledTime - t0 < 8f && !g.Over)
            {
                yield return null; var now0 = Feet(); var mv = now0 - was; mv.y = 0f; was = now0; if (g.Tick >= from) most = Mathf.Max(most, mv.magnitude / Mathf.Max(Time.unscaledDeltaTime, 1e-4f));
                var c0 = h.Views.FigureClip("op/scout").Item1; if (c0.StartsWith("walk")) walked = c0;
                if (!stepShot && G.Within(g.Ops[0].Pos, seat, 120)) { stepShot = true; h.SetPaused(true); yield return Frames(2); yield return Shoot(dir, "r085-chair-step", log); h.SetPaused(false); was = Feet(); from = g.Tick + 2; }
            }
            yield return new WaitForSecondsRealtime(0.6f); h.SetPaused(true); yield return Frames(2); yield return Shoot(dir, "r085-chair-sitting", log);
            var at = Feet(); at.y = seatW.y; float off = Vector3.Distance(at, seatW); var clip = h.Views.FigureClip("op/scout").Item1; var now = g.ListenSpotNow("scout"); bool seated = h.Views.SeatedDrawn("op/scout");
            row("r085chairStep", walked.Length > 0 && most <= 2.7f, $"the last 3 m into {got} drawn on {(walked.Length > 0 ? walked : "no walk clip")}, the figure at most {most:0.00} m/s (the cell walks 1.8; 2.7 is a stride over: a slide or a jump would pass it)");
            row("r085scoutChair", clip.Contains("sit") && seated && off < 0.05f && g.SeatedAt("scout")?.chair == got, $"chair {got}, clip {clip}, seated {seated}, {off * 100f:0} cm off the seat, engine seated in '{g.SeatedAt("scout")?.chair}'; now: spot {now?.at} '{now?.chair}' waiting {now?.waiting}, walk {g.Ops[0].Walk != null}, busy {g.Ops[0].Busy?.Do}, act {g.Ops[0].Act?.Do}, at {g.Ops[0].Pos} tick {g.Tick}; plate '{h.PlateWords(0)}'");
        }

        /// <summary>From the third line of listen-inez to the end: the lean, the doorway and the frame's edge, the reveal by its time and by clicks, the live face, the listener alone.</summary>
        static IEnumerator ListenBeats(HandoverHost h, HandoverHost.Convo c, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            // the frame: the voice's figure stands in the doorway, the two fill the shot, the listener at the edge with his back to them
            var cam = h.View; var mil = h.Views.Find("crowd/" + h.Game.Walkers.FindIndex(w => w.P.Name.StartsWith("Milena "))); var inez = h.Views.Find("waitress"); var sc = h.Views.Find("op/scout");
            Vector3 VP(Transform t, float up) => cam.WorldToViewportPoint(t.position + Vector3.up * up);
            var vm = VP(mil, 1.2f); var vi = VP(inez, 1.2f); var vs = VP(sc, 1.2f); var toInez = inez.position - sc.position; toInez.y = 0f; var facing = Quaternion.Euler(0f, h.Views.FigureYaw("op/scout") ?? 0f, 0f) * Vector3.forward; float atHer = Vector3.Angle(facing, toInez);
            bool pairIn = vm.z > 0f && vi.z > 0f && vm.x > 0.05f && vm.x < 0.95f && vi.x > 0.05f && vi.x < 0.95f, inTheWay = vs.z > 0f && vs.z < Mathf.Min(vm.z, vi.z) && vs.x > Mathf.Min(vm.x, vi.x) - 0.08f && vs.x < Mathf.Max(vm.x, vi.x) + 0.08f;
            row("r085listenFrame", c.Shot is "profile" or "high" && c.LivePartner && mil.gameObject.activeInHierarchy && pairIn && !inTheWay && atHer > 60f, $"{c.Shot} (the ladder's two-shot: profile where a lens on the ground holds both clear, else high; engine 0.49.0 lands route A's Listen among the terrace tables), partner live {c.LivePartner}; Milena viewport {vm:0.00}, Inez {vi:0.00}, Scout {vs:0.00} ({Vector3.Distance(sc.position, inez.position):0.0} m from her), {atHer:0} deg off looking at her");
            yield return Shoot(dir, "r085-listen-frame", log);
            string Spk() => c.Beat < c.Lines.Count ? c.Lines[c.Beat].speaker : "";
            // on to each moment by Space; a live face on every line, whoever speaks
            var faces = new List<string>(); bool leanOk = false, revealOk = false, revealClick = false, plainOk = true; var seenF = new HashSet<string>();
            for (int i = 0; i < 40 && h.ConvoOn && c.Beat < c.Lines.Count - 1; i++)
            {
                yield return new WaitForSecondsRealtime(0.28f);
                var f = c.Beat < c.Fields.Count ? c.Fields[c.Beat] : null; var rt = HudToolkit.ConvoFacePicture; string a = PictureHash(rt); float fill = PictureFill(rt);
                if (seenF.Add(Spk())) { yield return new WaitForSecondsRealtime(0.3f); string b = PictureHash(HudToolkit.ConvoFacePicture); faces.Add($"{Spk()} fill {fill:0.00}, frames differ {a != b}"); plainOk &= fill > 0.05f && a != b; }
                if (f != null && f.Lean.Length > 0 && f.RevealAfter.Length == 0) { leanOk = HudToolkit.ConvoLeanNow.StartsWith(f.Lean); yield return Shoot(dir, "r085-lean", log); }
                if (f != null && f.RevealAfter.Length > 0)
                {   // its own run from the start: by its time, then the same line again isn't possible, so the first click's half is taken at the start of a replay of the wait
                    string atStart = HudToolkit.ConvoSaid; yield return Shoot(dir, "r085-reveal-start", log); int beat = c.Beat; c.BeatAt = Time.unscaledTime - f.RevealS * 0.1f; yield return Frames(2); string before = HudToolkit.ConvoSaid;
                    yield return Press(Key.Space); string afterOne = HudToolkit.ConvoSaid; int beatOne = c.Beat; yield return Press(Key.Space); int beatTwo = c.Beat;
                    revealOk = atStart.Contains(f.RevealAfter) && atStart.Length < h.Text.ConvName(Spk()).Length + 2 + f.Line.Length + 1; revealClick = before.EndsWith(f.RevealAfter) && afterOne.EndsWith(f.Line) && beatOne == beat && beatTwo == beat + 1;
                    log.Add($"r085 reveal: '{atStart}' / '{before}' / '{afterOne}'; beats {beat} {beatOne} {beatTwo}"); continue;
                }
                yield return Press(Key.Space);
            }
            row("r085lean", leanOk, $"lean word in SemiBold: '{HudToolkit.ConvoLeanNow}'");
            row("r085reveal", revealOk && revealClick, $"the start alone {revealOk}, first click completes and second advances {revealClick}");
            row("r085liveFace", plainOk && seenF.Contains("milena") && seenF.Contains("waitress"), string.Join("; ", faces));
            // the last line: the listener alone
            yield return Frames(3); yield return new WaitForSecondsRealtime(0.25f); yield return Shoot(dir, "r085-scout-alone", log);
            row("r085scoutAlone", c.Shot == "intercut" && c.Cut == "scout" && Spk() == "scout" && HudToolkit.ConvoFacePicture != null, $"{c.Shot} on {c.Cut}, speaker {Spk()}");
            yield return Press(Key.Space);
        }

        /// <summary>After the cut: the card on her, the plain chip stood aside, no old line beside it, New with what's opened; then, the view elsewhere, waiting (brief 101 round 3: it never docks).</summary>
        static IEnumerator CardOnPerson(HandoverHost h, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            var inez = h.Views.Find("waitress"); h.LookAt(inez.position); yield return new WaitForSecondsRealtime(0.9f); yield return Frames(3);
            var pc = h.PersonCardNow; string words = HudToolkit.PersonCardWords; var cv = h.Text.Conversations.Find(x => x.Id == pc?.Id);
            bool bubbles = h.Bubbles.Exists(b => b.Speaker is "scout" or "waitress" or "milena" && Time.unscaledTime - b.Born < b.Life);
            bool all = cv != null && cv.Learned.TrueForAll(words.Contains) && cv.Opens.TrueForAll(words.Contains) && words.Contains(h.Text.ConvNew + " ");
            yield return Shoot(dir, "r085-conv-cards", log);
            row("r085cards", HudToolkit.PersonCardUp && !HudToolkit.PersonCardWaiting && all && HudToolkit.ChipsShown == 0 && !bubbles && pc.Until - pc.At >= HandoverHost.CardReadS(pc) - 0.01f, $"up {HudToolkit.PersonCardUp}, waiting {HudToolkit.PersonCardWaiting}, chips {HudToolkit.ChipsShown}, old bubbles {bubbles}, holds {pc?.Until - pc?.At:0.0} s: {words}");
            // brief 101 round 3 (Glyph's slip): with the view elsewhere the card never docks: it waits, its read time held, and her key face carries the Book's new dot
            float left0 = pc != null ? pc.Until - Time.unscaledTime : 0f;
            h.LookAt(inez.position + new Vector3(70f, 0f, 70f)); yield return new WaitForSecondsRealtime(0.9f); yield return Frames(3); yield return Shoot(dir, "r085-card-dock", log);
            float left1 = pc != null ? pc.Until - Time.unscaledTime : 0f;
            row("r085cardDock", !HudToolkit.PersonCardUp && HudToolkit.PersonCardWaiting && HudToolkit.KeyDotFor == "waitress" && left1 >= left0 - 0.2f, $"view elsewhere: up {HudToolkit.PersonCardUp}, waiting {HudToolkit.PersonCardWaiting}, key dot '{HudToolkit.KeyDotFor}', read time left {left0:0.0} s then {left1:0.0} s");
        }

        /// <summary>call-inez: the held moments by clicks and by time; three speakers, each framed alone.</summary>
        static IEnumerator CallHolds(HandoverHost h, HandoverHost.Convo c, List<string> cuts, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            bool before = false, beforeTime = false, after = false, afterOn = false, shotGuard = false;
            for (int i = 0; i < 24 && h.ConvoOn; i++)
            {
                cuts.Add(c.Cut); if (c.Cut == "guard" && !shotGuard) { shotGuard = true; yield return new WaitForSecondsRealtime(0.3f); yield return Shoot(dir, "r085-call-guard", log); }
                var f = c.Beat < c.Fields.Count ? c.Fields[c.Beat] : null;
                if (f != null && f.HoldBefore > 0f && !before)
                {   // the shot, no line; left alone the line comes in its time; a click brings it at once: both, from the same hold
                    yield return new WaitForSecondsRealtime(0.2f); string held = HudToolkit.ConvoSaid; yield return Shoot(dir, "r085-hold-before", log); float wait = c.BeatAt - Time.unscaledTime; yield return new WaitForSecondsRealtime(Mathf.Max(0f, wait) + 0.1f); beforeTime = held.Length == 0 && HudToolkit.ConvoSaid.Length > 0;
                    c.BeatAt = Time.unscaledTime + 1f; yield return Frames(2); string again = HudToolkit.ConvoSaid; int b0 = c.Beat; yield return Press(Key.Space); before = again.Length == 0 && HudToolkit.ConvoSaid.Length > 0 && c.Beat == b0; log.Add($"r085 holdBefore: '{held}', '{again}', then '{HudToolkit.ConvoSaid}'");
                }
                if (f != null && f.HoldAfter > 0f && !after)
                {   // the shot held on holdOn, no line; a click ends it
                    yield return Press(Key.Space); yield return new WaitForSecondsRealtime(0.2f); after = c.AfterUntil >= 0f && HudToolkit.ConvoSaid.Length == 0; afterOn = c.Cut == f.HoldOn; cuts.Add(c.Cut); yield return Shoot(dir, "r085-hold-after", log);
                    yield return Press(Key.Space); yield return Frames(3); after &= c.OutAt >= 0f || !h.ConvoOn; continue;
                }
                yield return Press(Key.Space); yield return Frames(3);
            }
            row("r085holds", before && beforeTime && after && afterOn, $"holdBefore: by time {beforeTime}, a click shows it {before}; holdAfter: no line and a click ends it {after}, framed on its holdOn {afterOn}");
        }
    }
}
