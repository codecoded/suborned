using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 085 (Sky on a passer-by in the cutaway: "frozen. Would be good keep them walking"): while the clock is held for a conversation, everyone not in it carries on, view only. A walker
    /// keeps the heading and pace of its last tick with its walk clip, turning aside at the walk grid's edge and short of a speaker, and stopping idle only with no open heading. Standing people keep their idles. Nothing reaches the engine;
    /// the views go back to the engine's place at the cut out, because each frame starts from it.</summary>
    public sealed partial class HostViews
    {
        const float SceneStopM = 0.75f;
        /// <summary>The proof reads it: walkers moved, and stopped, in the scene now.</summary>
        public int SceneWalkers { get; private set; }
        public int SceneStopped { get; private set; }
        public int SceneTurned { get; private set; }
        int sceneSeen = -1;

        /// <summary>Is this figure walking at the engine's last tick? A person walking is not hidden from the cutaway's lens.</summary>
        public bool WalkingNow(string key) { if (!things.TryGetValue(key, out var x)) return false; var d = x.to - x.from; d.y = 0f; return d.magnitude / 0.1f > 0.15f; }

        /// <summary>For the proof: where the engine has a thing now (its last tick), and an inset's picture.</summary>
        public Vector3? EngineAt(string key) => things.TryGetValue(key, out var x) ? x.to : (Vector3?)null;
        public RenderTexture InsetPicture(int k) => k >= 0 && k < insets.Length && insets[k] != null ? insets[k].Rt : null;

        readonly Dictionary<string, ((Transform t, Vector3 from, Vector3 to, bool shown) was, float yaw)> standWas = new();
        /// <summary>Brief 085, Listen: who stands where for the scene (Convo.Stands), view only: before the shot is framed and each frame while it plays. Everything they were goes back at the cut.</summary>
        public void SceneStands(HandoverHost.Convo c)
        {
            if (c == null) return;
            foreach (var kv in c.Stands)
            {
                if (!things.TryGetValue(kv.Key, out var x)) continue;
                figs.TryGetValue(kv.Key, out var f);
                if (!standWas.ContainsKey(kv.Key)) standWas[kv.Key] = (x, f != null ? f.yaw : 0f);
                things[kv.Key] = (x.t, kv.Value.pos, kv.Value.pos, true); if (!x.t.gameObject.activeSelf) x.t.gameObject.SetActive(true); x.t.position = kv.Value.pos;
                if (f != null && !float.IsNaN(kv.Value.yaw)) f.yaw = kv.Value.yaw;
            }
        }
        public void SceneStandsEnd()
        {
            foreach (var kv in standWas) { things[kv.Key] = kv.Value.was; var w = kv.Value.was; if (w.t.gameObject.activeSelf != w.shown) w.t.gameObject.SetActive(w.shown); if (w.shown) w.t.position = w.to; if (figs.TryGetValue(kv.Key, out var f)) f.yaw = kv.Value.yaw; }
            standWas.Clear();
        }

        static readonly float[] SceneTurns = { 35f, -35f, 70f, -70f, 110f, -110f };

        /// <summary>One bystander, one frame: moves the view along its heading from where the engine has it; the speed its clip should run at. At the walk grid's edge or short of a speaker it turns aside to
        /// the nearest open heading and walks on, easing its yaw; with none open it stops, idle, until the cut out.</summary>
        float SceneWalk(Fig f, Transform t, Vector3 d, float speed)
        {
            if (host.ConvoCount != sceneSeen) { sceneSeen = host.ConvoCount; SceneWalkers = 0; SceneStopped = 0; SceneTurned = 0; }
            if (f.sceneId != host.ConvoCount) { f.sceneId = host.ConvoCount; f.sceneOff = Vector3.zero; f.sceneStop = false; f.sceneDir = d / Mathf.Max(1e-4f, d.magnitude); if (speed > 0.15f) SceneWalkers++; }
            if (speed <= 0.15f) return 0f;
            var g = host.Game; float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f); var was = t.position + f.sceneOff; var next = was;
            bool Open(Vector3 dir)
            {
                var ahead = was + dir * (speed * dt + 0.3f);
                if (g.Walk.Free(frame.Engine(was)) && !g.Walk.Free(frame.Engine(ahead))) return false;
                foreach (var key in host.ConvoSpeakerViews()) if (things.TryGetValue(key, out var sv) && sv.shown) { var a = sv.t.position - ahead; a.y = 0f; if (a.magnitude < SceneStopM && Vector3.Dot(a, dir) > 0f) return false; }
                return true;
            }
            if (!f.sceneStop)
            {
                if (!Open(f.sceneDir))
                {
                    bool found = false; var from = f.sceneDir;
                    foreach (var turn in SceneTurns) { var c = Quaternion.Euler(0f, turn, 0f) * from; if (Open(c)) { f.sceneDir = c; found = true; SceneTurned++; break; } }
                    if (!found) { f.sceneStop = true; SceneStopped++; }
                }
                if (!f.sceneStop) { next = was + f.sceneDir * speed * dt; f.yaw = Mathf.MoveTowardsAngle(f.yaw, Mathf.Atan2(f.sceneDir.x, f.sceneDir.z) * Mathf.Rad2Deg, 270f * dt); }
            }
            f.sceneOff = next - t.position; var b = frame.ToBlock(new Vector2(next.x, next.z)); float floor = frame.Height(b.x, b.y); if (Mathf.Abs(floor - next.y) < 0.6f) next.y = floor;
            t.position = next;
            return f.sceneStop ? 0f : speed;
        }
    }
}
