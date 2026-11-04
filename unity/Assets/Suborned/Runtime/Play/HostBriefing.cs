using System;
using Path = System.IO.Path;
using File = System.IO.File;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.Play
{
    /// <summary>The start sequence (brief 062 B, brief 070; brief 096: the briefing is now the book's Job page, HostBook.Start). Begin closes the start book onto the
    /// opening view and the sequence plays its settle: the four ground rings pulse and the roster slides in (0.25 s), then the clock bar fades in and the clock starts
    /// at 0:00. The old page's lift and the door-to-cell glide are gone with the page (the constants stay: the sequence's beats are timed from them). View only: the
    /// engine sees the clock start, as before. Scripts and proofs that call StartClock or SetPaused(false) skip the sequence.</summary>
    public sealed partial class HandoverHost
    {
        public const float BriefExitS = 0.3f, BriefGlideS = 3.0f, BriefSettleS = 0.3f, BriefRosterS = 0.25f;
        /// <summary>When the clock starts, in seconds after Begin (3.6).</summary>
        public const float BriefPlayAt = BriefExitS + BriefGlideS + BriefSettleS;
        /// <summary>Seconds since Begin while the start sequence runs; -1 when it does not.</summary>
        public float StartT { get; private set; } = -1f;
        public bool Starting => StartT >= 0f;
        /// <summary>Glyph's N4 (0.2.122): the pointer's badge and every hover cue stay hidden while the briefing is up and through the glide, until the
        /// roster lands (the settle, 3.3 s after Begin); no order can be given there.</summary>
        public bool PointerHeld => ShowBriefing || Starting && StartT < BriefExitS + BriefGlideS;
        /// <summary>Proofs: the sequence holds where it is (StartSeek moves it) so a still can be taken at a given moment.</summary>
        public static bool StartFreeze;
        /// <summary>Automated runs open on the opening view as before; a proof of the briefing (FrontEnd.Proof) sets this to see the street briefing and the glide.</summary>
        public static bool BriefCamInAutomation;
        /// <summary>The camera stands on the briefing frame (it glides to the cell on Begin, or lands there when the clock starts another way).</summary>
        public bool BriefCamOn { get; private set; }
        /// <summary>The briefing's data, read once a run (BriefScreen.Load).</summary>
        public BriefScreen Brief { get; private set; }
        public int StartSkips { get; private set; }
        /// <summary>When the start sequence started the clock (the clock bar fades in from here); -100 when a script started it (no fade).</summary>
        public float SequencePlayedAt { get; private set; } = -100f;

        Vector3 glideFrom, glideTo; float glideYawFrom, glideYawTo;

        /// <summary>At a new run (Begin): the briefing's data, read once. Brief 096: the start is the book's Job page over the opening view, so the camera
        /// stays where Begin() put it (the old page's door frame and its glide to the cell are retired with the page).</summary>
        void BriefingCamera()
        {
            StartT = -1f; BriefCamOn = false;
            Guard("briefing", () => Brief = BriefScreen.Load(this));
        }

        /// <summary>The camera from the old briefing frame to the opening view at once; brief 096: nothing stands on that frame now, so it only clears the flag.</summary>
        void BriefCamLand()
        {
            if (!BriefCamOn || !cam) return; BriefCamOn = false; StartT = -1f;
            yawNow = yawFrom = yawTo = baseYaw; turnStart = -1f; cam.transform.rotation = Quaternion.Euler(pitch, yawNow, 0f);
            LookAt(frame.World(OpeningFocus));
        }

        /// <summary>Per frame from Controls while the sequence runs (its keys only): true while it holds the input.</summary>
        bool StartFrame(Keyboard kb, UnityEngine.InputSystem.Mouse mouse)
        {
            if (StartT < 0f) return false;
            ClearHover();
            if (!StartFreeze) StartT += Time.unscaledDeltaTime;
            bool skip = kb != null && kb.anyKey.wasPressedThisFrame || mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame);
            if (skip && StartT < BriefExitS + BriefGlideS) { StartT = BriefExitS + BriefGlideS; StartSkips++; } // lands now; the settle and the clock follow as ever
            ApplyGlide();
            if (StartT >= BriefPlayAt) { StartT = -1f; BriefCamOn = false; StartClock(); SequencePlayedAt = Time.unscaledTime; }
            return true;
        }

        void ApplyGlide()
        {
            if (!cam) return;
            float u = Mathf.Clamp01((StartT - BriefExitS) / BriefGlideS), e = u < 0.5f ? 4f * u * u * u : 1f - Mathf.Pow(-2f * u + 2f, 3f) / 2f; // ease in and out (cubic)
            yawNow = yawFrom = yawTo = glideYawFrom + Mathf.DeltaAngle(glideYawFrom, glideYawTo) * e; turnStart = -1f;
            cam.transform.rotation = Quaternion.Euler(pitch, yawNow, 0f);
            focus = Vector3.Lerp(glideFrom, glideTo, e); focus.y = 0f; PlaceCamera();
        }

        /// <summary>Proofs: the sequence at `t` seconds after Begin (with StartFreeze, a still of that moment).</summary>
        public void StartSeek(float t) { if (StartT < 0f) return; StartT = Mathf.Max(0f, t); ApplyGlide(); if (StartT >= BriefPlayAt) { StartT = -1f; BriefCamOn = false; StartClock(); SequencePlayedAt = Time.unscaledTime; } }
    }

    /// <summary>Brief 070's briefing data, slimmed by brief 096 (the start is the book's Job page, which reads brief.json and text.json itself): the mission's files,
    /// the target's door (brief.target.at) the Job page frames in play, and brief.camera.yaw as the fallback quarter step. No stills, pins or plan: the stills step
    /// (tools/game/briefstills.ts) is retired with the page.</summary>
    public sealed class BriefScreen
    {
        public string TargetRole = "councillor";
        public Vector3? TargetAt; public float YawFromOpening;

        /// <summary>The mission's JSON file (contracts/missions/&lt;m&gt;/&lt;file&gt; in the Editor; StreamingAssets/missions/&lt;m&gt;/&lt;file&gt; in a build), or null.</summary>
        public static JNode MissionFile(string file, string mission = "handover")
        {
            string path = Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "contracts", "missions", mission, file)) : Path.Combine(Application.streamingAssetsPath, "missions", mission, file);
            try { return File.Exists(path) ? JNode.Parse(File.ReadAllText(path), file) : null; } catch (Exception e) { Debug.LogWarning($"Briefing: {path}: {e.Message}"); return null; }
        }

        static string S(JNode n, string k) => n?.Opt(k) is JNode v && v.Kind == JKind.Str ? v.Str() : null;

        public static BriefScreen Load(HandoverHost h)
        {
            var b = new BriefScreen(); var site = h.Game.S;
            var mission = MissionFile("mission.json"); var bj = MissionFile("brief.json");
            JNode Brief(string k) => bj?.Opt(k) ?? mission?.Opt("brief")?.Opt(k);
            var tnode = Brief("target"); string targetAt = tnode != null && tnode.Kind != JKind.Str ? S(tnode, "at") : null;
            b.TargetRole = tnode is JNode tr && tr.Kind == JKind.Str ? tr.Str() : S(tnode, "role") ?? "councillor";
            Vector3? Place(string id) { try { return site.HasPlace(id) ? h.frame.World(site.At(id)) : site.HasPlace("d:" + id) ? h.frame.World(site.At("d:" + id)) : null; } catch (Exception) { return null; } }
            b.TargetAt = (targetAt != null ? Place(targetAt) : null) ?? (site.HasPlace(site.GuardPost) ? h.frame.World(site.At(site.GuardPost)) : null);
            if (Brief("camera") is JNode cm && cm.Opt("yaw") is JNode y && y.Kind == JKind.Num) b.YawFromOpening = y.Int();
            return b;
        }
    }
}
