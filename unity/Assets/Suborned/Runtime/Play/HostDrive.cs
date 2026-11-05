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
    /// step; engine 0.33.1: and the standing cars' ground, Game.Walk, which the engine's drive step does not read: the host is the stricter)
    /// and keeps the people gap; each tick before the step its position goes in as a drive sample (an order in the log, so a session
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
        public void DriveToggle() { if (Driving) { DriveExit(true); followOnly = true; /* a script's or the lab's let-go keeps a view with no agent in hand: asked for */ } else DriveEnter(); }
        /// <summary>T while driven (the player's own switch): let go and back to the map, centred on the agent at play zoom, at the yaw it was left
        /// at (brief 067 G3); `glide` (T itself): by Glyph's 0.5 s glide; scripts get the map at once.</summary>
        public void DriveLetGoToMap(bool glide = false) { if (Driving) DriveExit(true, true, glide); }

        /// <summary>Brief 083 note 3 (Sky on 0.2.159: "sometimes moving to third person, the WASD controls were disabled and was the point to click movement"): the follow view with no agent in hand is
        /// asked for only by the camera lab's "Follow view only", its let-go and scripts (followOnly). In play it is never a resting state: DriveResume takes the agent as soon as nothing forbids it.</summary>
        bool followOnly;
        /// <summary>Brief 083: a press carried across the switch is not an order in the next view (a left button down at T let go on the map as a ground click that dropped the act).</summary>
        void DrivePressesClear() { lp = default; rp = default; Charge = null; dragging = false; orbiting = false; BoxScreen = null; }

        /// <summary>The proof: T as the player presses it.</summary>
        public void DriveEnter083() { if (!Driving) DriveEnter(); }
        /// <summary>The proof: the same order the ring gives, for an agent already at it.</summary>
        public void GiveWatchRepeatScripted(OpId who, string target) { var r = new PanelRow { By = who, I = new Intent { Order = new Order { Op = Op.Name(who), Do = "watch", Target = target } } }; GiveNow((int)who, r, null, default, false, "proof"); }
        /// <summary>The proof: a hold under way dropped.</summary>
        public void CancelHoldScripted() => CancelHold();
        /// <summary>The button's word (Fable's actions.drive.ways.release).</summary>
        public string BackToMapWord => Text != null && Text.Actions.TryGetValue("drive", out var da) && da.Ways.TryGetValue("release", out var rw) ? rw.label : "Back to map";
        /// <summary>The roster's "Back to map" button in third person (the T key's act): the view goes back, the agent's act or walk goes on (a trailed agent has no drive to release; a driven one stands).</summary>
        public void BackToMap() { if (Driving) DriveLetGoToMap(true); else if (Following) FollowExit(); }

        void DriveEnter()
        {
            if (Game == null || Game.Over || !cam) return;
            int k = FollowWho(); var op = Game.Ops[k];
            if (op.Down || op.Inside || op.InCar) { PopKey(op.Down ? "agent-down" : op.Inside ? "enforcer-inside" : "in-car", null, SelectedWorld(), Feedback.Refused); return; }
            TakeInSquad(k); Speed = 1; DrivePressesClear(); // brief 085: T keeps the squad (it was cut to this agent, and stayed cut on the map)
            followOnly = false; dBusyAskAt = -9f;
            DriveTake(k);
            if (!Following) FollowEnter();
            Driving = true; intentKey = "";
            DriveTrailIfBusy(k);
            // brief 083 (Sky on 0.2.159: "I saw a Let Go label where squad is - no idea what that means - did nothing when I clicked on it"): the "Let go: T" pop was a tip on a plate that looked like a button. The way back is a real button now (BackToMap, in the Squad button's place)
            Status = $"Driving the {Op.Name((OpId)k)} ({controls.Label(PlayAction.FollowView)} lets go)";
        }

        /// <summary>Sky after 0.2.56: "when I go third person, it shouldn't stop the character walking / running or doing whatever action they were
        /// until I intervene." The switch takes the view, not the agent's work: an agent walking or running, walking in to an act, in an act or
        /// holding a fire order is trailed (no drive sample, no cease: the engine carries on) until movement input or a new order takes the stick
        /// (DriveFrame's dAuto); the work done, the stick comes back by itself.</summary>
        public bool DriveTrailing => Driving && dAuto;
        bool DriveHasWork(int k) { var op = Game.Ops[k]; return op.Walk != null || op.Busy != null || op.Act != null || pending[k] != null || op.Gun.Order.Length > 0; }
        void DriveTrailIfBusy(int k) { if (Driving && !Lab.explore && DriveHasWork(k)) { dAuto = true; dSent = null; dVel = Vector2.zero; if (views != null) views.DriveKey = null; } }

        /// <summary>The player intervenes on a trailed agent (the trigger; movement input does the same in DriveFrame): the stick is taken back
        /// now, one drive sample at the agent's place (the engine's drive rule: the first sample takes control and ends a walk in progress).</summary>
        bool DriveTakeBack()
        {
            if (!Driving || !dAuto || Game == null || dAgent < 0) return Driving;
            var op = Game.Ops[dAgent]; dAuto = false; pending[dAgent] = null; dCm = new Vector2(op.Pos.X, op.Pos.Y); dVel = Vector2.zero; dSent = null;
            if (views != null) { views.DriveKey = "op/" + Op.Name((OpId)dAgent); views.DrivePos = CmWorld(dCm); }
            int face = Mathf.RoundToInt(Mathf.Repeat(dYaw, 360f)) % 360; var at = new Pt(op.Pos.X, op.Pos.Y);
            Answer a = default; if (!Guard("drive", () => a = Game.Issue(Game.DriveSample((OpId)dAgent, at, false, face)))) return false;
            if (a.ok) { dSent = at; dSentRun = false; dSentFace = face; }
            return a.ok;
        }

        /// <summary>Sky's walk-off (2026-10-25, Aster): a movement key held over from the last agent never walks the one just taken. On every take
        /// (T, Shift+T, Tab, F1-F4, a portrait or a click), the movement keys are ignored until all of them are up and one is pressed again; a Tech
        /// holding the loop is not walked off the cabinet by the key that was walking someone else. WASD resuming a drive the game ended
        /// (DriveResume) is the press itself and takes the stick at once. DriveKeysIgnored counts the frames a held key was ignored (checks).</summary>
        bool dKeyLatch, dNoLatch;
        public bool DriveKeyLatched => Driving && dKeyLatch;
        public int DriveKeysIgnored { get; private set; }

        void DriveTake(int k)
        {
            dKeyLatch = !dNoLatch;
            // brief 024 dropped the standing fire order here ("the trigger is the player's now"); Sky after 0.2.56: the switch never ends work (DriveTrailIfBusy)
            var op = Game.Ops[k]; dAgent = k; dCm = new Vector2(op.Pos.X, op.Pos.Y); dVel = Vector2.zero; dSent = null; dSentFace = -1; dMoving = false; DriveCorrections = 0;
            dYaw = AgentYaw(k, Following ? fYaw : yawNow); // brief 067 G: the agent keeps the heading it had (it no longer turns to the camera's yaw on a take)
            if (views != null) { views.DriveKey = "op/" + Op.Name((OpId)k); views.DrivePos = CmWorld(dCm); views.DriveYaw = dYaw; views.DriveSpeed = 0f; }
        }

        void DriveRelease(int k) { if (k >= 0 && Game != null && Game.DrivePoseOf((OpId)k).Driven) Guard("drive", () => Game.Issue(Game.DriveRelease((OpId)k))); }

        /// <summary>Let go (release: the engine's onRelease applies) or lose control (the engine ended it); the map opens centred on the agent.</summary>
        void DriveExit(bool release, bool toMap = false, bool glide = false)
        {
            if (!Driving) return;
            int k = dAgent; AimEnd(); if (release) DriveRelease(k); DrivePressesClear();
            if (Lab.explore) { Lab.explore = false; Paused = exploreWasPaused; }
            Driving = false; dAuto = false; dAgent = -1; DriveFocus = null; dFocusKey = "";
            dResume = !release && !toMap; // the game ended control (not the player): WASD takes it back (DriveResume)
            if (views != null) views.DriveKey = null;
            // Sky 2026-10-20: no automatic view switch: a freeze, target mode, the engine ending the drive or the agent going down leave the
            // ground view as a plain follow; only T (DriveLetGoToMap) goes back to the map
            if (toMap && Following && glide) FollowExitGlide(k); // brief 067 G3 (Sky on 0.2.122, Glyph): back on the map by a 0.5 s glide, centred on the agent at play zoom, at the quarter-step yaw it was left at (HostFollow)
            else if (toMap && Following) { FollowExit(); if (cam) { cam.orthographicSize = Mathf.Clamp(Cam.overhead.zoom, zoomMin, zoomMax); centreGoal = null; centreStart = -1f; camFollow = false; } } // a script's let-go: the same map view at once (LookAt below)
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
            if (who != dAgent) { DriveRelease(dAgent); var op2 = Game.Ops[who]; if (op2.Down || op2.Inside || op2.InCar) { DriveExit(false); return; } DriveTake(who); dAuto = false; DriveTrailIfBusy(who); } // Tab, F1-F4, a click on an agent: control moves at once (an agent at work is trailed, as on T)
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
                if (dKeyLatch) { bool any = controls.Held(kb, PlayAction.PanUp) || controls.Held(kb, PlayAction.PanDown) || controls.Held(kb, PlayAction.PanRight) || controls.Held(kb, PlayAction.PanLeft); if (any) { input = Vector2.zero; DriveKeysIgnored++; } else dKeyLatch = false; } // held over from the last agent: not this one's
                run |= controls.Held(kb, PlayAction.AddToGroup);
                if (controls.Down(kb, PlayAction.Gait) && !HoldOn) dRunLatch = !dRunLatch; // brief 041 (hold): G is weapons free; Shift runs
                TpE(kb); // brief 024 revision: E taps act on the prompt, held 0.22 s the ring (HostAim)
            }
            if (DriveRoute == null && DriveInputOverride == null) input = DriveBusyGuard(input); // brief 083: WASD on an agent at an act asks first (the engine's onBusy would cancel it on the push)
            if (dAuto)
            {
                if (input.sqrMagnitude > 0f && DriveRoute == null && DriveInputOverride == null) { dAuto = false; pending[dAgent] = null; dCm = new Vector2(op.Pos.X, op.Pos.Y); dVel = Vector2.zero; dSent = null; } // the stick taken back: the next sample takes control (the engine drops its walk)
                else
                {
                    dCm = new Vector2(op.Pos.X, op.Pos.Y); dMoving = op.Walk != null;
                    if (views != null) views.DriveKey = null; // the engine's walk draws the agent
                    if (!DriveHasWork(dAgent)) dAuto = false; // the act done: the stick comes back (a walk-in between its walk and its act included)
                    DriveFocusFrame(); return;
                }
            }
            if (TriggerDown && !run) input = Vector2.zero; // brief 024 rule 3: firing at a walk plants the agent; a runner keeps going (the engine reads the run flag)
            bool free = (!Paused || Exploring) && !(op.Busy != null && DriveOnBusyHold) && !op.Down && !op.Inside && !op.InCar;
            DriveMove(free ? input : Vector2.zero, run, dt, DriveRoute == null && DriveInputOverride == null ? MoveYaw(input.sqrMagnitude > 0f) : Following ? fYaw : yawNow); // brief 041: the keys' frame latched while held (MoveYaw)
            if (Aiming && AimFaceOn && !dMoving) dYaw = AimFace; // brief 024: aiming, the agent turns to the reticle (the sample's facing; the camera follows the travel heading only)
            if (views != null) { views.DriveKey = "op/" + Op.Name((OpId)dAgent); views.DrivePos = CmWorld(dCm); views.DriveYaw = dYaw; views.DriveSpeed = dVel.magnitude / 100f; }
            DriveFocusFrame();
        }

        /// <summary>Brief 083 note 2 (Sky on 0.2.159: "when moving to third person it cancels any action"): T itself never did: entering trails the agent (DriveTrailIfBusy), leaving releases a drive and
        /// the engine leaves Busy be. The cancel was the first tap of WASD after T: its sample pushes a busy agent past cell.drive.busyPush and the engine cancels (onBusy "cancel"), and on a walk-in the
        /// host drops the queued act. So here, with no change to the rule: while the agent is on an act, no movement reaches the engine. The first key press says what it is on; a second press within
        /// BusyConfirmS stops the act (the cancel order) and the stick is theirs. A key held across the prompt is one press.</summary>
        Vector2 DriveBusyGuard(Vector2 input)
        {
            bool held = input.sqrMagnitude > 0f, rose = held && !dInputWas; dInputWas = held;
            if (Paused || ActOn(dAgent) == null) return input; // nothing to lose (paused: DriveFrame holds the stick anyway)
            if (!rose) return Vector2.zero;
            float ago = Time.unscaledTime - dBusyAskAt;
            if (ago <= BusyConfirmS && doing[dAgent].CanCancel) { dBusyAskAt = -9f; BusyStops++; CancelAgent(dAgent, null); return input; }
            dBusyAskAt = Time.unscaledTime; BusyAsks++; BusyWords = BusyLine(dAgent, DW("cancel.againKey", "Press again to stop"));
            Pop(BusyWords, null, (AgentWorld(dAgent) ?? Vector3.zero) + Vector3.up * 1.1f, Feedback.Info, BusyConfirmS, "icons/singles/p_time");
            return Vector2.zero;
        }
        bool dInputWas; float dBusyAskAt = -9f;

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
            if (Game == null) return;
            if (Game.Over || !Following || FollowExiting) { dResume = false; return; }
            if (followOnly || Frozen || Modal != ModalGate.Modal.None || CameraLab.Typing || ConvoOn || FailCamOn) return; // brief 083: T under the briefing, a restart in third person, the engine ending the drive: all land here, and take the agent as soon as play can
            var op = Game.Ops[FollowWho()]; if (op.Down || op.Inside || op.InCar || op.CarriedBy.HasValue) return; // nothing to take: no pop each frame
            if (Time.unscaledTime < dResumeAt) return; dResumeAt = Time.unscaledTime + 0.5f; // a take the engine refuses at once is not retried every frame
            bool held = kb != null && (controls.Held(kb, PlayAction.PanUp) || controls.Held(kb, PlayAction.PanDown) || controls.Held(kb, PlayAction.PanLeft) || controls.Held(kb, PlayAction.PanRight));
            dResume = false; DriveRetakes++; dNoLatch = held; try { DriveEnter(); } finally { dNoLatch = false; }
        }
        /// <summary>The proof reads it: follow views with no agent in hand that took one back.</summary>
        public int DriveRetakes { get; private set; }
        float dResumeAt = -1f;

        bool DriveOnBusyHold => (Game.Start.DriveOnBusy ?? Game.R.Drive?.OnBusy) == "hold";

        /// <summary>The controller's step against the walk grid, as the engine resolves one: x first, then y, each stopping before the first blocked
        /// cell; and, as the engine's gap, never ending nearer than the gap to a person on the street it came nearer to.</summary>
        Vector2 DriveSlide(Vector2 from, Vector2 to)
        {
            var g = Game.S.Grid;
            // engine 0.33.1 (rulebook car.footprint): the controller also stops at a standing car's ground (Game.Walk), stricter than the engine's
            // drive step (which reads the site's grid), so every sample is still one the engine takes whole; stood on a car's ground already (a
            // car stopped on the agent), the site's grid until it steps off, so it is never trapped
            var wg = CarWalk; bool onCar = wg.CarCovers(new Pt(Mathf.RoundToInt(from.x), Mathf.RoundToInt(from.y)));
            bool Open(int i, int j) => onCar ? g.FreeCell(i, j) : wg.FreeCell(i, j);
            float AxisX(Vector2 p, float x)
            {
                int j = g.CJ(Mathf.RoundToInt(p.y)), i0 = g.CI(Mathf.RoundToInt(p.x)), i1 = g.CI(Mathf.RoundToInt(x));
                if (i1 == i0) return x; int step = i1 > i0 ? 1 : -1;
                for (int i = i0 + step; ; i += step) { if (!Open(i, j)) return step > 0 ? g.X0 + i * g.Cell - 1.5f : g.X0 + (i + 1) * g.Cell + 0.5f; if (i == i1) return x; }
            }
            float AxisY(Vector2 p, float y)
            {
                int i = g.CI(Mathf.RoundToInt(p.x)), j0 = g.CJ(Mathf.RoundToInt(p.y)), j1 = g.CJ(Mathf.RoundToInt(y));
                if (j1 == j0) return y; int step = j1 > j0 ? 1 : -1;
                for (int j = j0 + step; ; j += step) { if (!Open(i, j)) return step > 0 ? g.Y0 + j * g.Cell - 1.5f : g.Y0 + (j + 1) * g.Cell + 0.5f; if (j == j1) return y; }
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
                exploreWasPaused = Paused; ShowBriefing = false; BriefCamLand(); Lab.explore = true;
                if (!Driving) DriveEnter();
                if (!Driving) { Lab.explore = false; return; }
                Paused = true; Status = $"Explore: the clock is held; walk anywhere, nothing sees you ({controls.Label(PlayAction.FollowView)} or the panel ends it)";
            }
            else { Lab.explore = false; DriveExploreOff(); Paused = exploreWasPaused; Status = Driving ? $"Driving the {Op.Name((OpId)dAgent)}" : Status; }
        }

        /// <summary>A restart: nobody is driven, explore is off (the views are new).</summary>
        void DriveReset() { Aiming = false; TriggerDown = false; dResume = false; Driving = false; dAuto = false; dAgent = -1; DriveFocus = null; dFocusKey = ""; Lab.explore = false; DriveInputOverride = null; DriveRunOverride = false; DriveRoute = null; }

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
                float score = dist * (1f + ang / 60f) + (t.Kind is "door" or "cabinet" or "stash" && RowRank(t) > 0 ? 2f : 0f); // brief 085: the door with no way for this agent never takes the focus from the guard who stands in it
                cands.Add((t, p.Anchor, score));
            }
            // brief 030 §8: the focus stepped through what is in front, nearest first (dFocusStep; Z left the keys in brief 096, so it stays 0: the nearest)
            cands.Sort((a, b) => a.score.CompareTo(b.score));
            if (cands.Count == 0) dFocusStep = 0;
            else { int pick = ((dFocusStep % cands.Count) + cands.Count) % cands.Count; best = cands[pick].t; bestAt = cands[pick].at; bestScore = cands[pick].score; }
            // brief 024 revision: the prompt is the thing under the cursor (within 15 m, on screen, off the HUD), else the nearest in front (Z steps it)
            var mp = PointerWorld is Vector3 pw && cam ? (Vector2)cam.WorldToScreenPoint(pw) : PointerOverride ?? UnityEngine.InputSystem.Mouse.current?.position.ReadValue();
            if (cam && mp is Vector2 mpv && dFocusStep == 0 && mpv.x >= 0 && mpv.y >= 0 && mpv.x <= Screen.width && mpv.y <= Screen.height && !OverHud(mpv))
            {
                var under = TargetAt(mpv);
                if (under != null && under.Kind is not ("op" or "unit") && TargetWorld(under) is Vector3 uw && (uw - me).magnitude <= 15f) { best = under; bestAt = uw + Vector3.up * (under.Kind == "car" ? 0.8f : under.Kind == "camera" ? 0f : under.Kind is "body" or "stash" ? 0.25f : 1.0f); }
            }
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
            intentKey = ik; dFocusKey = key; Hover = best; HoverFeedback(best); HoverIntent = HoldOn ? HoldIntent(best) : IntentFor(best); /* item 7 (Keel 2026-10-22): the pill offers what the holds give, as PointerHover does */ MoveChip = null; UpdateAim();
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
