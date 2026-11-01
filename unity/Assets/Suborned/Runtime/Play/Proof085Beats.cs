using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 085, look C: the scene's line, face, Listen's framing and held moments, and the card after the cut, by real input. Split from Proof085Scenes; it reads the words from the file, never pinning a line.</summary>
    public static partial class Proof085Scenes
    {
        /// <summary>From the third line of listen-inez to the end: the lean, the doorway and the frame's edge, the reveal by its time and by clicks, the live face, the listener alone.</summary>
        static IEnumerator ListenBeats(HandoverHost h, HandoverHost.Convo c, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            // the frame: the voice's figure stands in the doorway, the two fill the shot, the listener at the edge with his back to them
            var cam = h.View; var mil = h.Views.Find("crowd/" + h.Game.Walkers.FindIndex(w => w.P.Name.StartsWith("Milena "))); var inez = h.Views.Find("waitress"); var sc = h.Views.Find("op/scout");
            Vector3 VP(Transform t, float up) => cam.WorldToViewportPoint(t.position + Vector3.up * up);
            var vm = VP(mil, 1.2f); var vi = VP(inez, 1.2f); var vs = VP(sc, 1.2f); var away = sc.position - (mil.position + inez.position) / 2f; away.y = 0f; float scoutYaw = h.Views.FigureYaw("op/scout") ?? 0f; var facing = Quaternion.Euler(0f, scoutYaw, 0f) * Vector3.forward;
            bool pairIn = vm.z > 0f && vi.z > 0f && vm.x > 0.05f && vm.x < 0.95f && vi.x > 0.05f && vi.x < 0.95f, edge = vs.z > 0f && (vs.x > 0.68f || vs.x < 0.32f), turned = Vector3.Angle(facing, away) < 30f;
            row("r085listenFrame", c.Shot == "profile" && mil.gameObject.activeInHierarchy && pairIn && edge && turned, $"{c.Shot}; Milena viewport {vm:0.00}, Inez {vi:0.00}, Scout {vs:0.00}, his back to them {Vector3.Angle(facing, away):0} deg");
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

        /// <summary>After the cut: the card on her, the plain chip stood aside, no old line beside it, New with what's opened; then docked with the view elsewhere.</summary>
        static IEnumerator CardOnPerson(HandoverHost h, string dir, List<string> log, System.Action<string, bool, string> row)
        {
            var inez = h.Views.Find("waitress"); h.LookAt(inez.position); yield return new WaitForSecondsRealtime(0.9f); yield return Frames(3);
            var pc = h.PersonCardNow; string words = HudToolkit.PersonCardWords; var cv = h.Text.Conversations.Find(x => x.Id == pc?.Id);
            bool bubbles = h.Bubbles.Exists(b => b.Speaker is "scout" or "waitress" or "milena" && Time.unscaledTime - b.Born < b.Life);
            bool all = cv != null && cv.Learned.TrueForAll(words.Contains) && cv.Opens.TrueForAll(words.Contains) && words.Contains(h.Text.ConvNew + " ");
            yield return Shoot(dir, "r085-conv-cards", log);
            row("r085cards", HudToolkit.PersonCardUp && !HudToolkit.PersonCardDocked && all && HudToolkit.ChipsShown == 0 && !bubbles && pc.Until - pc.At >= HandoverHost.CardReadS(pc) - 0.01f, $"up {HudToolkit.PersonCardUp}, docked {HudToolkit.PersonCardDocked}, chips {HudToolkit.ChipsShown}, old bubbles {bubbles}, holds {pc?.Until - pc?.At:0.0} s: {words}");
            h.LookAt(inez.position + new Vector3(70f, 0f, 70f)); yield return new WaitForSecondsRealtime(0.9f); yield return Frames(3); yield return Shoot(dir, "r085-card-dock", log);
            row("r085cardDock", HudToolkit.PersonCardUp && HudToolkit.PersonCardDocked, $"view elsewhere: up {HudToolkit.PersonCardUp}, docked {HudToolkit.PersonCardDocked}");
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
