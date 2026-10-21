using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>Brief 025 item 2, take control (Sky 2026-10-19: "once I've got a plan / action being done, I want to follow / take control of the
    /// player to get there and enjoy it, make the action, and then control the player to the next action"; Keel's engine 0.9.0, the drive
    /// verb, briefs/025/keel-host-interface.md; Glyph's keys, brief 024 §1, §6, §7). T drops from the map into the selected agent (the follow
    /// view behind them) and hands them to a character controller: WASD camera-relative, Shift (or G latched) to run, at the rulebook's pace
    /// times the playground pace. The controller collides against the engine's own walk grid (Game.S.Grid, x then y, as the engine resolves a
    /// step) and keeps the people gap; each tick before the step its position goes in as a drive sample (an order in the log, so a session
    /// replays exactly); after the step the engine's pose pulls it back if the sample was not taken whole. The agent is drawn from the
    /// controller. E acts on the nearest thing in front (the price chip shows on it; one way acts at once, more open the panel), judged from
    /// where the agent stands: no walk to reach while driven. Tab or F1-F4 swap the agent; T again lets go (release) and the map opens centred on
    /// them. Explore (the camera lab) pauses the clock and lets the controller roam free, view only; it is pulled back to the engine's pose on
    /// leaving. Speed is locked at 1x while driving (the controller's pace is real time), and Next Event is off.</summary>
    /// <summary>Brief 025's playground switches for the driven agent (the P panel). pace, onBusy, onRelease and gap go in the run's start state
    /// (Start.Drive*, hashed), so they take effect from the next restart (the panel says so and offers one); unset (0, "", -1) = the rulebook's
    /// cell.drive, so a run with no switch set hashes exactly as before. explore is view only (the clock held; no sample is sent).</summary>
    [Serializable]
    public sealed class DriveLab
    {
        public bool explore;
        public int pace = 0;            // per mille of every pace, 250..4000; 0 = the rulebook's (1000)
        public string onBusy = "";      // "cancel" | "hold"; "" = the rulebook's
        public string onRelease = "";   // "stand" | "resume"; "" = the rulebook's
        public int gap = -1;            // cm, 0 off; -1 = the rulebook's
        public Start ToStart()
        {
            var s = new Start();
            if (pace > 0 && pace != 1000) s.DrivePace = Mathf.Clamp(pace, 250, 4000);
            if (onBusy is "cancel" or "hold") s.DriveOnBusy = onBusy;
            if (onRelease is "stand" or "resume") s.DriveOnRelease = onRelease;
            if (gap >= 0) s.DriveGap = Mathf.Clamp(gap, 0, 500);
            return s;
        }
    }

    public sealed partial class HandoverHost
    {
        /// <summary>The playground switches (P panel). The engine reads the start state the run began with (Game.Start); these apply on restart.</summary>
        public DriveLab Lab { get; } = new();
        /// <summary>The switches differ from the run's start: a restart applies them.</summary>
        public bool LabPending => Game != null && (Lab.ToStart() is var s) && (s.DrivePace != Game.Start.DrivePace || s.DriveOnBusy != Game.Start.DriveOnBusy || s.DriveOnRelease != Game.Start.DriveOnRelease || s.DriveGap != Game.Start.DriveGap);

        /// <summary>An agent is under the player's hand (ground mode).</summary>
        public bool Driving { get; private set; }
        /// <summary>The driven agent, -1 when none.</summary>
        public int DriveAgent => Driving ? dAgent : -1;
        /// <summary>Explore: the clock held, the controller roams free (view only).</summary>
        public bool Exploring => Driving && Lab.explore;
        /// <summary>Scripted input (tests, recorded sessions): x right, y forward, in the camera's frame; null = the keyboard.</summary>
        public Vector2? DriveInputOverride { get; set; }
        public bool DriveRunOverride { get; set; }
        /// <summary>Scripted steering (the proof, recorded sessions): block-frame waypoints (cm) the controller walks through, turned into the
        /// same camera-relative input the keys give; cleared on arrival. Null: the keys (or DriveInputOverride).</summary>
        public List<Pt> DriveRoute { get; set; }
        /// <summary>The thing E acts on now (nearest in front, within reach), else null.</summary>
        public Target DriveFocus { get; private set; }
        /// <summary>Samples refused or corrected since control began (diagnostic for the lab).</summary>
        public int DriveCorrections { get; private set; }

        /// <summary>Sky 2026-10-20 (replaces brief 024's "no auto-walk in ground mode"): a right-click act or fire out of reach while driven hands
        /// the agent to the engine, which walks it in and acts, exactly as on the map; the controller trails the engine's agent and the chase camera
        /// follows. WASD takes the stick back (the walk and its queued act are dropped); the act done, the stick comes back by itself.</summary>
        public bool DriveWalking => Driving && dAuto;
        bool dAuto;
        /// <summary>Give the driven agent to the engine for a walk-to-act (the drive released on this tick; samples stop until WASD or the act ends).</summary>
        void DriveAutoBegin() { if (!Driving || dAgent < 0) return; DriveRelease(dAgent); dAuto = true; dSent = null; dVel = Vector2.zero; }
        int dAgent = -1; Vector2 dCm, dVel; float dYaw; bool dRunLatch, dMoving; Pt? dSent; bool dSentRun; int dSentFace = -1; string dFocusKey = "";
        readonly List<Pt> dPeople = new(); int dPeopleTick = -1;

        /// <summary>T: take control of the selected agent, or let go.</summary>
        public void DriveToggle() { if (Driving) DriveExit(true); else DriveEnter(); }
        /// <summary>T while driven (the player's own switch): let go and back to the map.</summary>
        public void DriveLetGoToMap() { if (Driving) DriveExit(true, true); }

        void DriveEnter()
        {
            if (Game == null || Game.Over || !cam) return;
            int k = FollowWho(); var op = Game.Ops[k];
            if (op.Down || op.Inside || op.InCar) { PopKey(op.Down ? "agent-down" : op.Inside ? "enforcer-inside" : "in-car", null, SelectedWorld(), Feedback.Refused); return; }
            Select((OpId)k, false); Speed = 1;
            DriveTake(k);
            if (!Following) FollowEnter();
            Driving = true; intentKey = "";
            PopKey("let-go", null, views.Find("op/" + Op.Name((OpId)k)) is Transform t ? t.position : (Vector3?)null, Feedback.Info);
            Status = $"Driving the {Op.Name((OpId)k)} ({controls.Label(PlayAction.FollowView)} lets go)";
        }

        void DriveTake(int k)
        {
            var op = Game.Ops[k]; dAgent = k; dCm = new Vector2(op.Pos.X, op.Pos.Y); dVel = Vector2.zero; dSent = null; dSentFace = -1; dMoving = false; DriveCorrections = 0;
            dYaw = Following ? fYaw : yawNow;
            if (views != null) { views.DriveKey = "op/" + Op.Name((OpId)k); views.DrivePos = CmWorld(dCm); views.DriveYaw = dYaw; views.DriveSpeed = 0f; }
        }

        void DriveRelease(int k) { if (k >= 0 && Game != null && Game.DrivePoseOf((OpId)k).Driven) Guard("drive", () => Game.Issue(Game.DriveRelease((OpId)k))); }

        /// <summary>Let go (release: the engine's onRelease applies) or lose control (the engine ended it); the map opens centred on the agent.</summary>
        void DriveExit(bool release, bool toMap = false)
        {
            if (!Driving) return;
            int k = dAgent; if (release) DriveRelease(k);
            if (Lab.explore) { Lab.explore = false; Paused = exploreWasPaused; }
            Driving = false; dAuto = false; dAgent = -1; DriveFocus = null; dFocusKey = "";
            dResume = !release && !toMap; // the game ended control (not the player): WASD takes it back (DriveResume)
            if (views != null) views.DriveKey = null;
            // Sky 2026-10-20: no automatic view switch: a freeze, target mode, the engine ending the drive or the agent going down leave the
            // ground view as a plain follow; only T (DriveLetGoToMap) goes back to the map
            if (toMap && Following) FollowExit();
            if (k >= 0 && views?.Find("op/" + Op.Name((OpId)k)) is Transform t) { if (!Following) LookAt(t.position); PopKey("take-control", null, t.position, Feedback.Info); }
            Status = Following ? "Follow view" : "Overhead view";
        }

        Vector3 CmWorld(Vector2 cm) { float bx = cm.x / 100f, by = cm.y / 100f; var plan = frame.ToPlan(bx, by); return new Vector3(plan.x, frame.Height(bx, by), plan.y); }
        /// <summary>A Unity ground direction as a block-frame direction (unit).</summary>
        Vector2 BlockDir(Vector3 w) { var b = frame.ToBlock(frame.origin + new Vector2(w.x, w.z)); return b.sqrMagnitude > 1e-8f ? b.normalized : Vector2.zero; }
        /// <summary>A block-frame direction as a Unity yaw (degrees).</summary>
        float WorldYaw(Vector2 blockDir) { var p = blockDir.x * frame.axisX + blockDir.y * frame.axisY; return Mathf.Atan2(p.x, p.y) * Mathf.Rad2Deg; }

        /// <summary>mm a tick the controller may move: the engine's pace rule mirrored (a run only where one is allowed), times the playground pace.</summary>
        float DrivePaceMmTick(Op op, bool run)
        {
            var R = Game.R;
            bool canRun = run && !(op.Id == OpId.Enforcer && (Game.Vale == ValeAt.WithEnforcer || Game.Vale == ValeAt.Taking)) && !op.Carrying.HasValue && op.CarryBody.Length == 0 && !(op.Id == OpId.Tech && Game.LoopOn);
            int pace = canRun ? R.RunSpeed : op.CarryBody.Length > 0 && op.Id != OpId.Enforcer ? R.CarryUntrainedSpeed : R.CellSpeed;
            return Mathf.Max(1, (int)((long)pace * (Game.Start.DrivePace ?? 1000) / 1000)); // the engine's cap, integer as it is
        }

        /// <summary>Per frame (before the clock steps): the agent swap, the input, the controller's move and the drawn pose.</summary>
        void DriveFrame(Keyboard kb)
        {
            if (!Driving || Game == null) { DriveResume(kb); return; }
            if (Game.Over) { DriveExit(false); return; }
            int who = FollowWho();
            if (who != dAgent) { DriveRelease(dAgent); var op2 = Game.Ops[who]; if (op2.Down || op2.Inside || op2.InCar) { DriveExit(false); return; } DriveTake(who); } // Tab, F1-F4, a click on an agent: control moves at once
            if (Exploring) { Paused = true; Speed = 1; }
            var op = Game.Ops[dAgent];
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            Vector2 input = Vector2.zero; bool run = dRunLatch;
            if (DriveRoute is { Count: > 0 } rt)
            {
                while (rt.Count > 0 && (new Vector2(rt[0].X, rt[0].Y) - dCm).magnitude < 25f) rt.RemoveAt(0);
                if (rt.Count == 0) DriveRoute = null;
                else
                {
                    var wd = CmWorld(new Vector2(rt[0].X, rt[0].Y)) - CmWorld(dCm); wd.y = 0f;
                    var local = Quaternion.Euler(0f, -(Following ? fYaw : yawNow), 0f) * wd.normalized; input = new Vector2(local.x, local.z); run = DriveRunOverride;
                }
            }
            else if (DriveInputOverride is Vector2 ov) { input = ov; run = DriveRunOverride; }
            else if (kb != null && Modal == ModalGate.Modal.None && !CameraLab.Typing)
            {
                if (controls.Held(kb, PlayAction.PanUp)) input.y += 1; if (controls.Held(kb, PlayAction.PanDown)) input.y -= 1;
                if (controls.Held(kb, PlayAction.PanRight)) input.x += 1; if (controls.Held(kb, PlayAction.PanLeft)) input.x -= 1;
                run |= controls.Held(kb, PlayAction.AddToGroup);
                if (controls.Down(kb, PlayAction.Gait)) dRunLatch = !dRunLatch;
                DriveE(kb); // brief 030: E is the quick click on the focus; held 0.22 s, the ring on it
            }
            if (dAuto)
            {
                if (input.sqrMagnitude > 0f && DriveRoute == null && DriveInputOverride == null) { dAuto = false; pending[dAgent] = null; dCm = new Vector2(op.Pos.X, op.Pos.Y); dVel = Vector2.zero; dSent = null; } // the stick taken back: the next sample takes control (the engine drops its walk)
                else
                {
                    dCm = new Vector2(op.Pos.X, op.Pos.Y); dMoving = op.Walk != null;
                    if (views != null) views.DriveKey = null; // the engine's walk draws the agent
                    if (pending[dAgent] == null && op.Walk == null && op.Busy == null && op.Gun.Order.Length == 0) dAuto = false; // the act done: the stick comes back
                    DriveFocusFrame(); return;
                }
            }
            bool free = (!Paused || Exploring) && !(op.Busy != null && DriveOnBusyHold) && !op.Down && !op.Inside && !op.InCar;
            DriveMove(free ? input : Vector2.zero, run, dt, Following ? fYaw : yawNow);
            if (views != null) { views.DriveKey = "op/" + Op.Name((OpId)dAgent); views.DrivePos = CmWorld(dCm); views.DriveYaw = dYaw; views.DriveSpeed = dVel.magnitude / 100f; }
            DriveFocusFrame();
        }

        /// <summary>One frame of the controller: input (x right, y forward) turned by the camera's yaw onto the ground, at the agent's pace in any
        /// direction (diagonals are as fast as straights: the direction is normalised, the speed is the pace), eased in and out, slid along the
        /// walk grid as the engine resolves a step.</summary>
        void DriveMove(Vector2 input, bool run, float dt, float camYaw)
        {
            var op = Game.Ops[dAgent]; Vector2 want = Vector2.zero;
            if (input.sqrMagnitude > 0f)
            {
                var w = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(input.x, 0f, input.y); // camera-relative
                var bd = BlockDir(w); float cmPerS = DrivePaceMmTick(op, run) * Game.Hz / 10f;
                want = bd * cmPerS;
            }
            float k = 1f - Mathf.Exp(-dt / (want.sqrMagnitude > dVel.sqrMagnitude ? 0.04f : 0.027f)); // ~0.12 s to speed, ~0.08 s to stop (Glyph §1)
            dVel = Vector2.Lerp(dVel, want, k); if (want == Vector2.zero && dVel.magnitude < 1f) dVel = Vector2.zero;
            if (dVel != Vector2.zero) dCm = DriveSlide(dCm, dCm + dVel * dt);
            dMoving = dVel.magnitude > 5f;
            if (dMoving) dYaw = WorldYaw(dVel.normalized);
        }

        /// <summary>The camera yaw at which W walks along this block-frame direction (the controls test).</summary>
        public float CameraYawFor(Vector2 blockDir) => WorldYaw(blockDir.normalized);
        /// <summary>The camera yaw the controller turns input by now.</summary>
        public float DriveCameraYaw => Following ? fYaw : yawNow;

        /// <summary>The controls test (no engine step, nothing sent): from `fromCm`, `seconds` of input at 60 frames a second with the camera at
        /// `camYaw`; returns where the controller ends (block cm) and its Unity ground direction. The controller is put back as it was.</summary>
        public (Vector2 endCm, Vector3 worldDir) DriveProbe(Vector2 fromCm, Vector2 input, bool run, float seconds, float camYaw)
        {
            if (!Driving) return (fromCm, Vector3.zero);
            var keep = (dCm, dVel, dYaw, dMoving); dCm = fromCm; dVel = Vector2.zero;
            for (float t = 0f; t < seconds; t += 1f / 60f) DriveMove(input, run, 1f / 60f, camYaw);
            var end = dCm; var wd = CmWorld(end) - CmWorld(fromCm); wd.y = 0f;
            (dCm, dVel, dYaw, dMoving) = keep;
            return (end, wd.sqrMagnitude > 1e-6f ? wd.normalized : Vector3.zero);
        }

        /// <summary>Control ended by the game, not the player (the engine ended the drive: a move order, a correction it would not take), the
        /// ground view still on: WASD takes the stick back at once (Sky 2026-10-21: "if I click on a space, then the movement WASD keys fail to
        /// respond"; brief 024 §1: input takes the stick over an engine order). The player's own let-go (T, the lab's button) and the map clear it.</summary>
        bool dResume;
        public bool DriveResumable => dResume;
        void DriveResume(Keyboard kb)
        {
            if (!dResume || Game == null) return;
            if (Game.Over || !Following) { dResume = false; return; }
            if (kb == null || Modal != ModalGate.Modal.None || CameraLab.Typing || ConvoOn || FailCamOn) return;
            if (!(controls.Held(kb, PlayAction.PanUp) || controls.Held(kb, PlayAction.PanDown) || controls.Held(kb, PlayAction.PanLeft) || controls.Held(kb, PlayAction.PanRight))) return;
            var op = Game.Ops[FollowWho()]; if (op.Down || op.Inside || op.InCar || op.CarriedBy.HasValue) return; // nothing to take: no pop each frame
            if (Time.unscaledTime < dResumeAt) return; dResumeAt = Time.unscaledTime + 0.5f; // a take the engine refuses at once is not retried every frame
            dResume = false; DriveEnter();
        }
        float dResumeAt = -1f;

        bool DriveOnBusyHold => (Game.Start.DriveOnBusy ?? Game.R.Drive?.OnBusy) == "hold";

        /// <summary>The controller's step against the walk grid, as the engine resolves one: x first, then y, each stopping before the first blocked
        /// cell; and, as the engine's gap, never ending nearer than the gap to a person on the street it came nearer to.</summary>
        Vector2 DriveSlide(Vector2 from, Vector2 to)
        {
            var g = Game.S.Grid;
            float AxisX(Vector2 p, float x)
            {
                int j = g.CJ(Mathf.RoundToInt(p.y)), i0 = g.CI(Mathf.RoundToInt(p.x)), i1 = g.CI(Mathf.RoundToInt(x));
                if (i1 == i0) return x; int step = i1 > i0 ? 1 : -1;
                for (int i = i0 + step; ; i += step) { if (!g.FreeCell(i, j)) return step > 0 ? g.X0 + i * g.Cell - 1.5f : g.X0 + (i + 1) * g.Cell + 0.5f; if (i == i1) return x; }
            }
            float AxisY(Vector2 p, float y)
            {
                int i = g.CI(Mathf.RoundToInt(p.x)), j0 = g.CJ(Mathf.RoundToInt(p.y)), j1 = g.CJ(Mathf.RoundToInt(y));
                if (j1 == j0) return y; int step = j1 > j0 ? 1 : -1;
                for (int j = j0 + step; ; j += step) { if (!g.FreeCell(i, j)) return step > 0 ? g.Y0 + j * g.Cell - 1.5f : g.Y0 + (j + 1) * g.Cell + 0.5f; if (j == j1) return y; }
            }
            var a = new Vector2(AxisX(from, to.x), from.y); a = new Vector2(a.x, AxisY(a, to.y));
            int gap = DriveGapCm(); if (gap <= 0 || Exploring) return a;
            if (dPeopleTick != Game.Tick) { dPeopleTick = Game.Tick; dPeople.Clear(); foreach (var w in Game.Walkers) if (w.P != null && !w.P.Down && Game.PosOfPerson(w.P) is Pt pp) dPeople.Add(pp); foreach (var p in Game.People) if (!p.Down && Game.PersonPos(p) is Pt cp) dPeople.Add(cp); }
            bool Clear(Vector2 t) { foreach (var p in dPeople) { var q = new Vector2(p.X, p.Y); float dn = (t - q).magnitude; if (dn < gap + 2f && dn < (from - q).magnitude) return false; } return true; }
            if (Clear(a)) return a;
            var xo = new Vector2(AxisX(from, to.x), from.y); if (Clear(xo)) return xo;
            var yo = new Vector2(from.x, AxisY(from, to.y)); if (Clear(yo)) return yo;
            return from;
        }

        int DriveGapCm() => Game.Start.DriveGap ?? Game.R.Drive?.GapCm ?? 0;

        /// <summary>Once per tick, before the step: the controller's position as a drive sample, when it changed (a still agent sends nothing).</summary>
        void DriveTick()
        {
            if (!Driving || Exploring || Game == null || Game.Over || dAuto) return;
            var at = new Pt(Mathf.RoundToInt(dCm.x), Mathf.RoundToInt(dCm.y)); bool run = (dRunLatch || DriveRunOverride || (Keyboard.current != null && DriveInputOverride == null && DriveRoute == null && controls.Held(Keyboard.current, PlayAction.AddToGroup))) && dMoving;
            int face = Mathf.RoundToInt(Mathf.Repeat(dYaw, 360f)) % 360;
            if (dSent.HasValue && dSent.Value == at && dSentRun == run && (dSentFace == face || !dMoving)) return;
            Answer a = default; if (!Guard("drive", () => a = Game.Issue(Game.DriveSample((OpId)dAgent, at, run, face)))) return;
            if (!a.ok) { DriveCorrections++; Pop(Text?.ShortLock(a.code ?? "") ?? a.why, null, views.DrivePos, Feedback.Refused, 2f, PopIcon(a.code ?? ""), a.why); DriveExit(false); return; }
            dSent = at; dSentRun = run; dSentFace = face;
        }

        /// <summary>After the step: control ended by the game leaves ground mode; a sample not taken whole pulls the controller to the engine's pose.</summary>
        void DriveAfterStep()
        {
            if (!Driving || Exploring || Game == null || dAuto) return;
            var p = Game.DrivePoseOf((OpId)dAgent);
            if (!p.Driven && dSent.HasValue) { Status = $"The {Op.Name((OpId)dAgent)} is no longer yours to walk."; DriveExit(false); return; }
            var e = new Vector2(p.Pos.X, p.Pos.Y);
            if (p.Corrected || (!dMoving && (e - dCm).magnitude > 5f)) { if ((e - dCm).magnitude > 5f) { dCm = e; dVel = Vector2.zero; } if (p.Corrected) DriveCorrections++; }
        }

        bool exploreWasPaused;

        /// <summary>Explore (the P panel): the clock held, the selected agent's controller roams free (view only: no sample is sent, nothing is
        /// seen). Off: the controller goes back to where the engine has the agent and the clock is as it was.</summary>
        public void ExploreSet(bool on)
        {
            if (Game == null || on == Exploring) return;
            if (on)
            {
                exploreWasPaused = Paused; ShowBriefing = false; Lab.explore = true;
                if (!Driving) DriveEnter();
                if (!Driving) { Lab.explore = false; return; }
                Paused = true; Status = $"Explore: the clock is held; walk anywhere, nothing sees you ({controls.Label(PlayAction.FollowView)} or the panel ends it)";
            }
            else { Lab.explore = false; DriveExploreOff(); Paused = exploreWasPaused; Status = Driving ? $"Driving the {Op.Name((OpId)dAgent)}" : Status; }
        }

        /// <summary>A restart: nobody is driven, explore is off (the views are new).</summary>
        void DriveReset() { dResume = false; Driving = false; dAuto = false; dAgent = -1; DriveFocus = null; dFocusKey = ""; Lab.explore = false; DriveInputOverride = null; DriveRunOverride = false; DriveRoute = null; }

        /// <summary>Leaving explore: the controller goes back to where the engine has the agent.</summary>
        public void DriveExploreOff() { if (Driving && Game != null) { var p = Game.Ops[dAgent].Pos; dCm = new Vector2(p.X, p.Y); dVel = Vector2.zero; dSent = null; } }

        /// <summary>The nearest thing in front of the driven agent within 8 m (people, hostiles, the cars, the door, the cabinet, cameras, the patrol,
        /// bodies, stashes; a passer-by for the Enforcer): the price chip shows on it (the hover), E acts on it.</summary>
        void DriveFocusFrame()
        {
            if (Panel != null || Modal != ModalGate.Modal.None) return; // the panel is open at the focus
            if (Exploring) { if (dFocusKey.Length > 0 || DriveFocus != null) { DriveFocus = null; Hover = null; HoverIntent = null; HoverAnchor = null; HoverName = ""; intentKey = "drive"; dFocusKey = ""; } return; } // view only: nothing to act on
            var me = views.DrivePos; var fwd = Quaternion.Euler(0f, dYaw, 0f) * Vector3.forward;
            Target best = null; float bestScore = float.MaxValue; Vector3 bestAt = default;
            var cands = new List<(Target t, Vector3 at, float score)>();
            foreach (var p in Pickables())
            {
                var t = p.T; if (t.Kind is "op" or "unit") continue;
                var d = p.Anchor - me; d.y = 0f; float dist = d.magnitude; if (dist > 8f || dist < 0.05f) continue;
                float ang = Vector3.Angle(fwd, d); if (ang > 100f) continue;
                if (cam) { var vp = cam.WorldToViewportPoint(p.Anchor); if (vp.z <= 0f || vp.x < 0.08f || vp.x > 0.85f || vp.y < 0.08f || vp.y > 0.92f) continue; } // on screen, with room for its chip
                float score = dist * (1f + ang / 60f);
                cands.Add((t, p.Anchor, score));
            }
            // brief 030 §8: Z / Shift+Z step the focus through what is in front, nearest first (dFocusStep; a lost focus starts again)
            cands.Sort((a, b) => a.score.CompareTo(b.score));
            if (cands.Count == 0) dFocusStep = 0;
            else { int pick = ((dFocusStep % cands.Count) + cands.Count) % cands.Count; best = cands[pick].t; bestAt = cands[pick].at; bestScore = cands[pick].score; }
            DriveFocus = best;
            if (best == null)
            {
                if (dFocusKey.Length > 0) { Hover = null; HoverIntent = null; HoverAnchor = null; HoverName = ""; intentKey = "drive"; }
                dFocusKey = ""; return;
            }
            // the chip sits by the focus (the HUD places it at Mouse, GUI space), not at the cursor: E is the pointer while driven
            if (cam) { var sp = cam.WorldToScreenPoint(bestAt); if (sp.z > 0f) Mouse = new Vector2(sp.x, cam.pixelHeight - sp.y); }
            var key = best.Kind + "/" + best.Key;
            // priced on a change of focus, and once a second (Preview is costly; every drive sample is an order, so the order count moves each tick)
            var ik = $"drive:{key}:{Agent}:{Picked}:{(int)Time.unscaledTime}";
            if (ik == intentKey) return;
            intentKey = ik; dFocusKey = key; Hover = best; HoverFeedback(best); HoverIntent = IntentFor(best); MoveChip = null; UpdateAim();
        }

        /// <summary>E: act on the focus, as a quick right click on it (brief 030).</summary>
        public void DriveAct()
        {
            if (DriveFocus == null && Panel == null) { dFocusKey = "?"; DriveFocusFrame(); } // the focus as it stands this instant
            if (DriveFocus is not Target t || !cam) { PopKey("nothing-here", null, views.DrivePos, Feedback.Info); return; }
            var w = TargetWorld(t) ?? views.DrivePos; var sp = cam.WorldToScreenPoint(w + Vector3.up); var screen = new Vector2(sp.x, sp.y);
            if (IsFire(t)) { PointerRightOn(t, screen); return; } // a hostile in front: E fires, as the right press does
            ActContext(t, screen); // brief 030: the same one act as a quick right click on it (else the ring, else nothing)
        }
    }
}
