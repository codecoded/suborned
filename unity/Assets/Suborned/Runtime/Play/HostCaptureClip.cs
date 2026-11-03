using Suborned.Sim;
using UnityEngine;
using UnityEngine.Video;

namespace Suborned.Play
{
    /// <summary>Brief 085 (Glyph's capture-and-cues section 1, GDD section 10): while the Enforcer is inside on an Enter, a muted clip of him taking Vale, starting 102 ticks before the Enter ends, so
    /// its last frame meets him at the door. Its time is the mission clock's, never its own: it freezes on a pause and under a scene, follows the speed, and reseeks past 0.2 s of drift. Prepared when
    /// the Enter lands; not ready 3 s before the end, it is skipped for this Enter. On enter.out the last frame holds 2.5 s of mission time, then a 0.3 s fade. A click jumps the camera: to the exit
    /// door, then to him. It reads the engine and gives no order, so a run hashes the same with it on or off.</summary>
    public sealed partial class HandoverHost
    {
        public enum ClipState { Off, Waiting, Playing, Holding, Closing }
        public const int ClipLeadTicks = 102; public const float ClipHoldS = 2.5f, ClipFadeOutS = 0.3f, ClipCloseS = 0.15f, ClipPrepareBy = 3f, ClipLengthS = 10.125f;
        /// <summary>The proof turns it off for the same-hash check.</summary>
        public static bool ClipDisabled;
        public ClipState ClipNow { get; private set; } = ClipState.Off;
        public RenderTexture ClipTexture { get; private set; }
        /// <summary>For the HUD: when the clip came up and when he came out (real time), its fade, and the words on its plate.</summary>
        public float ClipBorn { get; private set; } public float ClipOutAt { get; private set; } = -1f; public float ClipAlpha { get; private set; }
        /// <summary>For checks: the video's time now, the time it should be at, and at enter.out how far apart they were.</summary>
        public double ClipVideoTime => clipPlayer ? clipPlayer.time : 0; public double ClipWantTime { get; private set; } public double ClipDriftAtOut { get; private set; } = -1; public string ClipLast { get; private set; } = "";

        Game clipGame; VideoPlayer clipPlayer; bool clipInEnter, clipBegan; int clipStart, clipHoldEnd; float clipClosing, clipSeekAt; bool clipHeld;

        VideoPlayer ClipPlayer()
        {
            if (clipPlayer) return clipPlayer;
            var video = Resources.Load<VideoClip>("Video/capture_clip"); if (!video) { ClipLast = "no clip in Resources"; return null; }
            ClipTexture = new RenderTexture(640, 360, 0, RenderTextureFormat.ARGB32) { name = "capture clip" }; ClipTexture.Create();
            var go = new GameObject("capture clip"); go.transform.SetParent(transform, false);
            var p = go.AddComponent<VideoPlayer>(); p.playOnAwake = false; p.waitForFirstFrame = true; p.isLooping = false; p.skipOnDrop = true; p.source = VideoSource.VideoClip; p.clip = video; p.audioOutputMode = VideoAudioOutputMode.None;
            p.renderMode = VideoRenderMode.RenderTexture; p.targetTexture = ClipTexture; p.aspectRatio = VideoAspectRatio.Stretch; clipPlayer = p; return p;
        }

        void ClipReset(bool closeNow = true) { if (clipPlayer) clipPlayer.Stop(); ClipNow = ClipState.Off; ClipAlpha = 0f; ClipOutAt = -1f; clipHeld = false; clipBegan = false; }

        /// <summary>Per frame, after the clock has stepped.</summary>
        void ClipFrame()
        {
            var g = Game; if (g == null || g.Range || ClipDisabled) { if (ClipNow != ClipState.Off) ClipReset(); return; }
            if (clipGame != g) { clipGame = g; clipInEnter = false; ClipDriftAtOut = -1; ClipReset(); } // a restart clears it
            if (g.Over) { if (ClipNow != ClipState.Off) ClipReset(); return; }
            var op = g.Ops[(int)OpId.Enforcer]; float now = Time.unscaledTime;
            bool entering = op.Inside && op.Busy is { Do: "enter" } b && b.How != "out", outWithVale = !op.Inside && g.Vale == ValeAt.WithEnforcer;
            if (entering && !clipInEnter && ClipNow == ClipState.Off)
            {   // the Enter has landed: once per Enter (the both-doors wait re-arms BusyEnd each tick, so the start is read once)
                clipStart = op.BusyEnd - ClipLeadTicks; ClipDriftAtOut = -1; if (ClipPlayer() is VideoPlayer vp) { vp.Prepare(); ClipNow = ClipState.Waiting; ClipLast = $"prepared at {g.Tick}, starts {clipStart}"; }
            }
            clipInEnter = entering;
            float t = (g.Tick + Mathf.Clamp01(acc / 0.1f) - clipStart) / Game.Hz; ClipWantTime = t;
            switch (ClipNow)
            {
                case ClipState.Waiting:
                    if (!entering) { ClipReset(); break; }
                    if (clipPlayer.isPrepared && t >= 0f) { ClipNow = ClipState.Playing; ClipBorn = now; clipBegan = true; clipPlayer.time = Mathf.Min(t, ClipLengthS - 0.05f); clipSeekAt = now; ClipLast = $"playing at tick {g.Tick}"; }
                    else if (!clipPlayer.isPrepared && t >= ClipLengthS - ClipPrepareBy) { ClipLast = "not prepared 3 s before the end: skipped"; ClipReset(); }
                    break;
                case ClipState.Playing:
                    if (outWithVale) { ClipDriftAtOut = System.Math.Abs(clipPlayer.time - System.Math.Min(t, ClipLengthS)); ClipNow = ClipState.Holding; ClipOutAt = now; clipHoldEnd = g.Tick + Mathf.RoundToInt(ClipHoldS * Game.Hz); clipPlayer.Pause(); ClipLast = $"out at tick {g.Tick}, {t:0.00} s in, video {clipPlayer.time:0.00}"; break; }
                    if (!entering) { ClipNow = ClipState.Closing; clipClosing = now; clipPlayer.Pause(); ClipLast = "cancelled"; break; }
                    ClipFollow(t, now);
                    break;
                case ClipState.Holding:
                    if (op.Down || g.Vale != ValeAt.WithEnforcer) { ClipNow = ClipState.Closing; clipClosing = now; break; }
                    if (g.Tick >= clipHoldEnd) { ClipNow = ClipState.Closing; clipClosing = now + ClipFadeOutS - ClipCloseS; }
                    break;
                case ClipState.Closing:
                    if (now >= clipClosing + ClipCloseS) ClipReset();
                    break;
            }
            ClipAlpha = ClipNow switch { ClipState.Playing or ClipState.Holding => Mathf.Clamp01((now - ClipBorn) / 0.15f), ClipState.Closing => Mathf.Clamp01((clipClosing + ClipCloseS - now) / Mathf.Max(0.01f, ClipCloseS)), _ => 0f };
        }

        void ClipFollow(float t, float now)
        {
            var p = clipPlayer; bool held = Paused || Frozen || Panel != null && controls.pauseWithPanel || ShowBriefing;
            bool end = t >= ClipLengthS - 0.05f;
            if (held || end) { if (p.isPlaying) p.Pause(); if (end && System.Math.Abs(p.time - (ClipLengthS - 0.05f)) > 0.2f && now - clipSeekAt > 0.5f) { p.time = ClipLengthS - 0.05f; clipSeekAt = now; } clipHeld = true; return; }
            if (p.canSetPlaybackSpeed) p.playbackSpeed = Speed * PlaybackScale;
            if (!p.isPlaying) p.Play();
            if ((clipHeld || System.Math.Abs(p.time - t) > 0.2f) && now - clipSeekAt > 0.25f) { p.time = t; clipSeekAt = now; clipHeld = false; }
        }

        /// <summary>A conversation scene holds the clock: the picture holds with it, and comes back on the same frame.</summary>
        void ClipHeld() { if (clipPlayer && clipPlayer.isPlaying) { clipPlayer.Pause(); clipHeld = true; } }

        /// <summary>The door he'll come out of, or himself once he's out (the chevron and the click). Null: no clip.</summary>
        public Vector3? ClipLookAt()
        {
            var g = Game; if (g == null) return null;
            if (ClipNow == ClipState.Holding) return views?.Find("op/enforcer") is Transform t ? t.position : (Vector3?)null;
            return frame.World(g.S.Stand(g.DoorHeld(g.ExitDoor) ? g.S.GuardPost : g.S.ValeService));
        }
        /// <summary>The clip's one word plate: the door he'll use (Fable's scenes.clip).</summary>
        public string ClipWord { get { var g = Game; if (g == null || Text == null) return ""; return g.DoorHeld(g.ExitDoor) ? g.DoorHeld(g.EnterDoor) && ClipNow != ClipState.Holding ? Text.ClipBoth : Text.ClipPublic : Text.ClipService; } }

        /// <summary>A click on the clip: never an order. Before he's out the camera goes to the door; during the hold, to him. From the ground view it leaves the view as T does, then jumps; no yaw change.</summary>
        public void ClipClick()
        {
            if (ClipNow is ClipState.Off or ClipState.Waiting) return;
            if (Driving) DriveLetGoToMap(); else if (Following) FollowExit();
            if (ClipNow == ClipState.Holding) CentreOn(OpId.Enforcer); else if (ClipLookAt() is Vector3 at) LookAt(at);
        }
    }
}
