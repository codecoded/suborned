using System; using System.Collections; using System.Collections.Generic;
using Suborned.Play; using Suborned.Sim; using UnityEngine; using UnityEngine.InputSystem;
using Order = Suborned.Sim.Order; using Target = Suborned.Play.HandoverHost.Target;

/// <summary>Sky on 0.2.212, film for Aster: the ring's Fire with the squad, map and third person, and the ring on the patrol in third person. Real time, real input through the Input System's device; a dot shows where the pointer is (the hardware cursor isn't captured).</summary>
public sealed partial class Rec085
{
    static readonly Target PatrolT = new Target { Kind = "patrol", Key = "patrol", Id = "patrol", Label = "the patrol" };
    Vector2 OnPatrol() { var w = h.TargetWorld(PatrolT) ?? Vector3.zero; foreach (var up in new[] { 1f, 0.6f, 1.4f, 0.3f }) { Vector2 s = h.View.WorldToScreenPoint(w + Vector3.up * up); if (h.ProbeAt(s).StartsWith("left patrol/patrol")) return s; } return h.View.WorldToScreenPoint(w + Vector3.up); }
    IEnumerator Beat(float s, bool track = false) { float t0 = Time.unscaledTime; while (Time.unscaledTime - t0 < s) { if (track) Dot(OnPatrol()); yield return null; } }
    void Dot(Vector2 p, bool left = false, bool right = false) { dotAt = p; dotOn = true; dotDown = left || right; Mouse2B(p, left, right); }
    static void Mouse2B(Vector2 sp, bool left, bool right) { if (Mouse.current is Mouse m) { var st = new UnityEngine.InputSystem.LowLevel.MouseState { position = sp }; if (left) st = st.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, true); if (right) st = st.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right, true); InputSystem.QueueStateEvent(m, st); } }
    IEnumerator Tap(bool left, float s = 0.1f) { var p = OnPatrol(); Dot(p); yield return null; yield return null; Dot(p, left, !left); float t0 = Time.unscaledTime; while (Time.unscaledTime - t0 < s) yield return null; Dot(p); yield return null; yield return null; }
    IEnumerator FireWedge() { if (h.RingWedgeScreen("fire") is not Vector2 w) { Note("no Fire wedge: " + h.RingWords); yield break; } var from = dotAt; float t0 = Time.unscaledTime; while (Time.unscaledTime - t0 < 0.5f) { Dot(Vector2.Lerp(from, w, (Time.unscaledTime - t0) / 0.5f)); yield return null; } Dot(w); yield return Beat(0.35f); Dot(w, true); yield return Beat(0.75f); Dot(w); yield return null; yield return null; }
    string Fires() { var l = new List<string>(); for (int k = 0; k < 4; k++) { var g = G.Ops[k].Gun; l.Add($"{Op.Name((OpId)k)}:'{g.Order}'{(g.Drawn ? " drawn" : "")}{(h.FireRefusalOf(k).code.Length > 0 && h.FireRefusalWord(k).Length > 0 ? " says " + h.FireRefusalWord(k) : "")}"); } return string.Join(", ", l); }
    IEnumerator Near(bool scoutBusy, params int[] who)
    {
        rec = false; if (h.Driving) h.DriveLetGoToMap(); if (h.Following) h.FollowToggle(); Dot(new Vector2(Screen.width * .5f, 6f)); h.controls.conversations = ConversationScenes.Never; h.SetSpeed(1); h.Begin(); yield return null; h.IntelOpen = false; h.ShowBriefing = false; h.StartClock(); h.SetPaused(false); yield return null; h.View.orthographicSize = 10f;
        if (scoutBusy) { h.GiveWatchScripted(OpId.Scout, "waitress"); for (int i = 0; i < 300 && G.Ops[0].Busy == null; i++) h.RunTo(G.Tick + 5); }
        for (int pass = 0; pass < 3; pass++) { var at = G.PosOf("patrol").Value; h.GroupWalk(new Pt(at.X + 500, at.Y + 300), false, null, new List<int>(who)); for (int i = 0; i < 120; i++) { bool on = false; foreach (var k in who) on |= G.Ops[k].Walk != null; if (!on) break; h.RunTo(G.Tick + 5); } }
        h.SetPaused(true); h.LookAt(h.TargetWorld(PatrolT) ?? Vector3.zero); yield return null; yield return null;
    }
    IEnumerator Third() { h.SetPaused(false); Keyb(Key.T); yield return null; yield return null; yield return null; Keyb(); yield return Beat(0.7f); h.FollowFace(h.TargetWorld(PatrolT) ?? Vector3.zero); yield return null; yield return null; }
    static void Keyb(params Key[] k) { if (Keyboard.current is Keyboard kb) InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(k)); }

    IEnumerator Squad()
    {
        // ---- the map, the squad: left click, hold Fire
        yield return Near(true, 1, 2, 3); h.SelectAll(); h.SetPaused(false); RecOn(); label = "Map · squad selected · left click the patrol"; yield return Beat(1.8f, true); yield return Tap(true); Note($"map ring: {h.RingWords}"); yield return Beat(1.1f); label = "Hold Fire on the ring"; yield return FireWedge(); Note("map after: " + Fires()); label = "All fire · the Scout too far, says why"; yield return Beat(7f); Note("map later: " + Fires()); RecOff();
        // ---- third person, the squad
        yield return Near(true, 1, 2, 3); h.SelectAll(); h.TakeInSquad(3); yield return Third(); RecOn(); label = "Third person · squad · left click the patrol"; yield return Beat(1.8f, true); yield return Tap(true); Note($"third ring: {h.RingWords}"); yield return Beat(1.1f); label = "Hold Fire on the ring"; yield return FireWedge(); Note("third after: " + Fires()); label = "All fire, the one in hand too"; yield return Beat(7f); Note("third later: " + Fires()); RecOff();
        // ---- third person, the Enforcer alone: both taps
        yield return Near(false, 3); h.Select(OpId.Enforcer, false); yield return Third(); RecOn(); label = "Third person · Enforcer · left click: the ring"; yield return Beat(1.8f, true); yield return Tap(true); Note($"left tap: {h.RingWords}"); yield return Beat(1.6f); h.CloseRing(); Dot(new Vector2(Screen.width * .5f, 40f)); yield return Beat(0.6f);
        label = "Right click: the ring"; yield return Beat(1f, true); yield return Tap(false); Note($"right tap: {h.RingWords}"); yield return Beat(1.2f); label = "Hold Fire on the ring"; yield return FireWedge(); Note("enforcer after: " + Fires()); label = "He draws and fires"; yield return Beat(4.5f); Note("enforcer later: " + Fires()); RecOff(); label = ""; yield return null; yield return null;
    }
}
