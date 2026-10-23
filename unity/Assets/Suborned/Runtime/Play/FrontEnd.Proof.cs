using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>`-frontEndShots &lt;dir&gt;` (brief 051's done-check): the player opens on the front end as a person's launch does, then the proof captures
    /// every screen at the window's size (CaptureSize's `-capture-size`): loading, the main menu (with and without Continue), Options' four pages,
    /// Controls (a row waiting for a key, then a clash), the mission brief, the pause menu over play and Options opened from it; and checks the
    /// settings: a rebind round trip through the file (what a relaunch reads), a clash shown and not written until Swap, Reset all, each of the twelve
    /// old toggles and the hints through the file, and the PlayerPrefs migration (fed a table, never the player's keys). Run it with
    /// `-settings &lt;file&gt;` so it never touches Sky's settings.json. Writes `&lt;dir&gt;/frontend.json` (suborned.frontend.proof/1) and quits 0 if every check held.</summary>
    public sealed partial class FrontEnd
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ProofBoot()
        {
            var dir = GameBoot.ArgValue("-frontEndShots"); if (dir == null || Application.isEditor) return;
            var go = new GameObject("front-end-proof"); DontDestroyOnLoad(go); go.AddComponent<FrontEndProof>().Dir = dir;
        }
    }

    public sealed class FrontEndProof : MonoBehaviour
    {
        public string Dir = "";
        readonly List<string> shots = new(), checks = new(), log = new(); bool ok = true;

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            float t0 = Time.realtimeSinceStartup; FrontEnd fe = null; HandoverHost h = null;
            while ((fe = FrontEnd.Live) == null && Time.realtimeSinceStartup - t0 < 30f) yield return null;
            while (((h = FindAnyObjectByType<HandoverHost>()) == null || h.Game == null) && Time.realtimeSinceStartup - t0 < 120f) yield return null;
            if (fe == null || h == null) { Write("no front end or host", false); Application.Quit(2); yield break; }
            yield return new WaitForSecondsRealtime(2.5f); // the world streams in under the picture
            Check("settingsFile", Settings.Persists && Settings.FilePath == GameBoot.ArgValue("-settings"), $"persists {Settings.Persists}, {Settings.FilePath}, source {Settings.Source}");

            // ---- the screens
            fe.Show(FrontEnd.Screen.Loading); fe.ForceProgress(0.62f, "People"); yield return Shot(fe, "loading");
            fe.Show(FrontEnd.Screen.Menu); yield return Shot(fe, "menu"); log.Add($"menu: Continue {(fe.ContinueShown ? "shown" : "absent")}");
            fe.KeyMove(1); fe.KeyMove(1); yield return Shot(fe, "menu-focus"); // keyboard focus moved down twice (the default ring is on the first button in "menu")
            FrontEnd.ForceNoSave = true; fe.Show(FrontEnd.Screen.Menu); yield return Shot(fe, "menu-nosave"); Check("menuNoSave", !fe.ContinueShown, ""); FrontEnd.ForceNoSave = false;
            fe.Show(FrontEnd.Screen.Options);
            foreach (var (i, n) in new[] { (0, "display"), (1, "audio"), (2, "camera"), (3, "gameplay") }) { fe.SetTab(i); yield return Shot(fe, "options-" + n); }
            fe.Show(FrontEnd.Screen.Controls); fe.SetTab(0); yield return Shot(fe, "controls-command");
            fe.Listen(PlayAction.Cancel, 0); yield return Shot(fe, "controls-rebind-focus"); // the rebind row: focus fill, tick, "Press a key" in the live ring
            fe.Offer(PlayAction.Cancel, 0, Key.N); yield return Shot(fe, "controls-clash"); fe.ClashCancel();
            fe.SetTab(1); yield return Shot(fe, "controls-camera"); fe.SetTab(2); yield return Shot(fe, "controls-fights"); fe.SetTab(0);

            // ---- settings checks (through the file a relaunch reads)
            yield return SettingsChecks(fe, h);

            // ---- New game → the brief → the tactical briefing → play → the pause menu → Options from it
            fe.Show(FrontEnd.Screen.Menu); yield return null;
            var b = fe.MenuButton("new"); b?.Invoke(); yield return null;
            Check("newGameBrief", fe.Showing == FrontEnd.Screen.Brief, $"showing {fe.Showing}; brief pick {fe.BriefPick}, {fe.BriefWords} words");
            yield return Shot(fe, "brief");
            fe.Show(FrontEnd.Screen.None); yield return null; yield return null;
            Check("briefingAfter", h.ShowBriefing && !FrontEnd.Covers, $"host briefing {h.ShowBriefing}");
            yield return Shot(fe, "briefing");
            h.StartClock(); yield return new WaitForSecondsRealtime(1.5f); h.OpenMenu(); yield return null; yield return null;
            yield return Shot(fe, "pause");
            FrontEnd.OpenFromPause(true); yield return Shot(fe, "options-from-pause");
            fe.Back(); yield return null; Check("backToPause", h.MenuOpen && !FrontEnd.Covers, $"menu {h.MenuOpen}, covers {FrontEnd.Covers}");
            // Quit to menu: saved, the menu up with Continue; Continue returns to the live run
            int tick = h.Game.Tick; h.QuitToMenuFromMenu(); yield return null; yield return null;
            Check("quitToMenu", fe.Showing == FrontEnd.Screen.Menu && fe.ContinueShown && !h.MenuOpen, $"showing {fe.Showing}, continue {fe.ContinueShown}");
            yield return Shot(fe, "menu-continue");
            fe.MenuButton("continue")?.Invoke(); yield return null;
            Check("continueLive", !FrontEnd.Covers && h.Game.Tick == tick, $"covers {FrontEnd.Covers}, tick {h.Game.Tick} (was {tick})");

            Write(ok ? "ok" : "failed", ok); yield return new WaitForSecondsRealtime(0.3f);
            Application.Quit(ok ? 0 : 1);
        }

        IEnumerator SettingsChecks(FrontEnd fe, HandoverHost h)
        {
            var st = Settings.Current;
            // rebind round trip: Next event → J through the page, then the file read again
            fe.Show(FrontEnd.Screen.Controls); fe.SetTab(0);
            fe.Listen(PlayAction.NextEvent, 0); fe.Offer(PlayAction.NextEvent, 0, Key.J); yield return null;
            var re = new PlayControls(); Settings.Reload(); re.LoadSettings();
            Check("rebindRoundTrip", re.BindingOf(PlayAction.NextEvent).keys?.FirstOrDefault() == Key.J && h.controls.BindingOf(PlayAction.NextEvent).keys?.FirstOrDefault() == Key.J,
                $"file {re.BindingOf(PlayAction.NextEvent).keys?.FirstOrDefault()}, game {h.controls.BindingOf(PlayAction.NextEvent).keys?.FirstOrDefault()}");
            // a clash: Cancel → J is Next event's: the strip shows, nothing written; Swap gives Cancel J and Next event C
            fe.SetTab(0); fe.Listen(PlayAction.Cancel, 0); fe.Offer(PlayAction.Cancel, 0, Key.J); yield return null;
            Settings.Reload(); var c1 = new PlayControls(); c1.LoadSettings();
            bool shown = fe.clash != null, kept = c1.BindingOf(PlayAction.Cancel).keys?.FirstOrDefault() == Key.C;
            fe.ClashSwap(); yield return null;
            Settings.Reload(); var c2 = new PlayControls(); c2.LoadSettings();
            Check("clash", shown && kept && c2.BindingOf(PlayAction.Cancel).keys?.FirstOrDefault() == Key.J && c2.BindingOf(PlayAction.NextEvent).keys?.FirstOrDefault() == Key.C,
                $"strip {shown}, untouched before Swap {kept}, after: Cancel {c2.BindingOf(PlayAction.Cancel).keys?.FirstOrDefault()}, Next event {c2.BindingOf(PlayAction.NextEvent).keys?.FirstOrDefault()}");
            // Shift + 1 (Pick action 1) is not 1 (Select agent 1); E in third person is not E on the map
            Check("contexts", c2.ClashOf(PlayAction.Verb1, Key.Digit1, "Shift", "") == null && c2.ClashOf(PlayAction.Act, Key.E, "", "third") == null && c2.ClashOf(PlayAction.Cancel, Key.N, "", "") == null, "");
            // Reset all
            Settings.Current.ResetPage("controls"); Settings.Reload(); var c3 = new PlayControls(); c3.LoadSettings();
            Check("resetAll", c3.BindingOf(PlayAction.Cancel).keys?.FirstOrDefault() == Key.C && c3.BindingOf(PlayAction.NextEvent).keys?.FirstOrDefault() == Key.N, "");
            // the twelve old toggles and the hints: changed, saved, read back as a relaunch would, and in the game's table
            st = Settings.Current; var g = st.gameplay; var cam = st.camera;
            g.clickScheme = (int)ControlPreset.DoubleClick; cam.settle = false; g.conversations = (int)ConversationScenes.Never; g.actionPlate = true; g.quickAction = true; g.clickActs = false;
            g.aimMode = (int)AimMode.Toggle; g.followLock = true; g.commanderHoldFire = true; g.tpRightFires = false; g.planTogether = false; cam.edgePan = true; g.hints = false;
            st.Save(true); yield return null;
            Settings.Reload(); var t = new PlayControls(); t.LoadSettings(); var c = h.controls; var rg = Settings.Current.gameplay;
            bool file = t.preset == ControlPreset.DoubleClick && !t.leashSettle && t.conversations == ConversationScenes.Never && t.actionPlate && t.quickAction && !t.clickActs && t.aimMode == AimMode.Toggle
                        && t.followLock && t.commanderHoldFire && !t.tpRightFires && !t.planTogether && t.edgeScroll && !rg.hints;
            bool game = c.preset == ControlPreset.DoubleClick && !c.leashSettle && c.conversations == ConversationScenes.Never && c.actionPlate && c.quickAction && !c.clickActs && c.aimMode == AimMode.Toggle
                        && c.followLock && c.commanderHoldFire && !c.tpRightFires && !c.planTogether && c.edgeScroll && !h.HintsOn;
            Check("toggles", file && game, $"file {file}, game {game}");
            Settings.Current.ResetPage("gameplay"); Settings.Current.ResetPage("camera"); yield return null;
            Check("togglesReset", h.controls.preset == ControlPreset.HoldActs && h.controls.leashSettle && h.HintsOn, $"preset {h.controls.preset}");
            // the migration from the old PlayerPrefs keys (a table, never the player's)
            var old = new Dictionary<string, int> { ["Suborned.Controls.Preset2"] = 1, ["Suborned.Controls.LeashSettle"] = 0, ["Suborned.Controls.Conversations"] = 2, ["Suborned.Controls.ActionPlate"] = 1,
                ["Suborned.Controls.QuickAction"] = 1, ["Suborned.Controls.ClickActs"] = 0, ["Suborned.Controls.AimMode"] = 1, ["Suborned.Controls.FollowLock"] = 1, ["Suborned.Controls.CommanderHoldFire"] = 1,
                ["Suborned.Controls.TpRightFires"] = 0, ["Suborned.Controls.PlanTogether"] = 0, ["Suborned.Controls.EdgePan"] = 1, ["Suborned.Play.Hints"] = 0 };
            var m = Settings.MigrateFrom(k => old.TryGetValue(k, out var v) ? v : null); var mg = m.gameplay;
            bool mig = m.MigratedKeys == 13 && mg.clickScheme == 1 && !m.camera.settle && mg.conversations == 2 && mg.actionPlate && mg.quickAction && !mg.clickActs && mg.aimMode == 1 && mg.followLock && mg.commanderHoldFire
                       && !mg.tpRightFires && !mg.planTogether && m.camera.edgePan && !mg.hints && Settings.LegacyKeys.Length == 13 && Settings.LegacyKeys.All(old.ContainsKey);
            Check("migration", mig, $"{m.MigratedKeys} keys");
            fe.Show(FrontEnd.Screen.Menu);
        }

        void Check(string name, bool pass, string detail)
        {
            ok &= pass; checks.Add($"\"{name}\": {{\"ok\": {(pass ? "true" : "false")}, \"detail\": \"{(detail ?? "").Replace("\"", "'").Replace("\\", "/")}\"}}");
            log.Add($"{name}: {(pass ? "ok" : "FAILED")} {detail}");
        }

        IEnumerator Shot(FrontEnd fe, string name)
        {
            for (int i = 0; i < 3; i++) { fe.Settle(); yield return null; }
            yield return new WaitForEndOfFrame(); fe.Settle();
            var t = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(Path.Combine(Dir, name + ".png"), t.EncodeToPNG()); shots.Add($"\"{name}\""); }
            catch (Exception e) { log.Add($"{name}: not written: {e.Message}"); ok = false; }
            Destroy(t);
        }

        void Write(string status, bool good)
        {
            var json = "{\n \"schema\": \"suborned.frontend.proof/1\",\n \"status\": \"" + status + "\",\n \"build\": \"" + BuildStamp.Line.Replace("\"", "'") + "\",\n \"screen\": [" + UnityEngine.Screen.width + ", " + UnityEngine.Screen.height + "],\n \"shots\": [" + string.Join(", ", shots) + "],\n \"checks\": {\n  " + string.Join(",\n  ", checks) + "\n },\n \"log\": [" + string.Join(", ", log.Select(l => "\"" + l.Replace("\"", "'").Replace("\\", "/") + "\"")) + "]\n}\n";
            try { File.WriteAllText(Path.Combine(Dir, "frontend.json"), json); } catch (Exception e) { Debug.LogError("FrontEndProof: " + e.Message); }
        }
    }
}
