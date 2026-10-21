// Anyone can be shot (Sky 2026-10-21: "players should be able to shoot anyone (think GTA) - nothing more annoying than being restricted in a
// game"; Motive briefs/027/motive/tuning.md §9, engine 0.15.0). Every person and every machine that acts can be shot; the consequences come
// from the systems that already price harm, never from a refusal. The one target refusal kept is the cell's own (fire-on-cell; Sky §9.7).
//  · A knocked-out or restrained person (a brief-013 body, not dead, open, not carried) is a target where they lie: a hit kills them, with
//    a takedown kill's consequences plus the gunfire's.
//  · Gap 1: a person shot down outside the final fight leaves a dead body of brief 013 where they fall (found, carried, stashed like any).
//  · Gap 2: a camera ("camera:<id>") is a target with weapons.health.camera; shot out it is dark for the rest of the mission. Its shot is
//    gunfire (Alert, the response, gunfire-trail once); no hit or down Trail, no Standing (it is not a person).
// Cars, doors and props are not targets (cars: Sky §9.7 says yes; their rule waits for Motive's numbers).
using System;
using System.Collections.Generic;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        readonly SortedDictionary<string, Gun> camGuns = new SortedDictionary<string, Gun>(StringComparer.Ordinal); // cameras a fire order has named (hashed in HashTakedown)

        static bool IsCameraId(string id) => id.StartsWith("camera:", StringComparison.Ordinal);
        Camera? CameraOf(string id) { if (!IsCameraId(id)) return null; var cid = id.Substring(7); foreach (var c in S.Cameras) if (c.Id == cid) return c; return null; }
        /// <summary>A camera shot out (tuning.md §9.2): dark for the rest of the mission; it watches nothing, and a loop on it is moot.</summary>
        public bool CameraDark(string camId) => camGuns.TryGetValue("camera:" + camId, out var g) && g.Down;
        void EnsureCameraGun(string id) { if (CameraOf(id) != null && !camGuns.ContainsKey(id)) camGuns[id] = NewGun(id, null, false, R.HealthCamera); }
        /// <summary>A camera's gun: the one a fire order made, else a fresh one for a preview (never stored, so a preview changes nothing).</summary>
        Gun? CameraGun(string id) => camGuns.TryGetValue(id, out var g) ? g : CameraOf(id) != null ? NewGun(id, null, false, R.HealthCamera) : null;

        /// <summary>A knocked-out or restrained person a shot can reach (tuning.md §9.1): their body lies open on the street, not dead, not
        /// being seized, not carried. Target id: the person's id or role ("guard" too).</summary>
        Body? KoBody(string id)
        {
            var p = id == "guard" ? GuardP : PersonById(id); if (p == null) return null;
            var b = BodyOf(p);
            return b != null && !b.Dead && !b.Seizing && b.Open && !b.Carrier.HasValue ? b : null;
        }

        /// <summary>A shot lands on a knocked-out body: it is a kill (takedown kill's consequences: the act becomes a kill, found it is a found
        /// killing; its kill Trail once if the knockout's Trail was already charged), plus the gunfire's own (CellGunfire, Damage).</summary>
        void ShotKill(Body b, string by)
        {
            var a = ActOf(b); var p = b.P;
            if (!a.Kill && a.ActTrail) AddTrail(R.KillTrail, "takedown:shot:" + a.Victim);
            a.Kill = true; b.Dead = true; b.WakeAt = INF; b.NextCallOut = -1; b.FreeAt = -1;
            if (p == GuardP) { GuardDown = true; GuardGun.Down = true; }
            if (civGuns.TryGetValue(p.Id, out var cg)) cg.Down = true;
            Chain.Add("killed:" + Who(p));
            Note("takedown.dead", $"{Who(p)} is shot where he lies. He is dead.");
        }

        /// <summary>Gap 1 (tuning.md §9.2): a person shot down outside the final fight lies dead where they fell, a body of brief 013. The
        /// shooting already dispatched (its report), so the body's act starts landed: a later report of it tops up the same dispatch, never a
        /// second (one incident, one dispatch). Its Trail is the gunfire's (hit, down, gunfire); found, it is a found killing (Standing), a
        /// case, and the ties grieve (between missions).</summary>
        void ShotBody(Person p, Pt at, string t)
        {
            if (BodyOf(p) != null || p == ValeP) return;
            var a = new TdAct { Id = Acts.Count, Victim = p.Id, How = "shot", P = p, At = at, Scene = at, Tick = Tick, Kill = true, ActTrail = true, Landed = true };
            if (fireIncident.TryGetValue(t, out var inc)) a.Lead = inc;
            Acts.Add(a);
            Bodies.Add(new Body { Id = p.Id, P = p, W = WalkerOf(p), Act = a.Id, Seizing = false, Dead = true, At = at, DownTick = Tick, WakeAt = INF, Phase = (int)(Streams.Fnv1a32(p.Id) % (uint)R.BodyCheckTicks) });
            // no log line of its own: civilian.down or guard.down says it (a Fable card for the body is §9.4's)
        }

        internal void HashCameras(Hash.H h) { if (camGuns.Count == 0) return; h.S("cams"); foreach (var kv in camGuns) Hash.HashGun(h, kv.Value); }
    }
}
