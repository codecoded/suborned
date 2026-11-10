using System.Collections.Generic;
using Suborned.Robots;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 104 (Sky, 2026-11-10: "he's awesome"): Vela's PatrolBot104 draws both robot roles, the patrol (the "patrol" view) and the response dial's
    /// robot units (the unit/ views' drums). Each is the kit FBX with PatrolBot (Runtime/Robots) driving its parts, and the shared LED face (RobotFace on
    /// DK_Face_Amber, the PatrolBot104 rig in robot-faces.json), which RobotFaces turns to the red stare when the patrol is alarmed (Alert 2, search) and a
    /// unit is on the street. Per frame the host reads, never writes: its gun (drawn, holstering, health, down), the shots the log gave it (ShotEvent),
    /// and the siren's rule:
    ///  · the patrol's siren lights from Alert 2 (search: the engine's response is out, and its face is already the alarm's), or while its weapon is drawn;
    ///  · a unit's siren is lit while it is on the street (it is the response answering the alarm); both go dark when down.
    /// The body faces its target when it shoots by the shared rule (Cars, from Aim). View only: replays are untouched.</summary>
    public sealed partial class HostViews
    {
        readonly Dictionary<string, PatrolBot> bots = new();
        /// <summary>Every PatrolBot drawn now, by view key (the proof reads them).</summary>
        public IReadOnlyDictionary<string, PatrolBot> Bots104 => bots;

        /// <summary>From Visual: the robot's parts rigged and its LED face attached (seeded by the view key), as a robot whose face RobotFaces drives.</summary>
        void BotView(string key, GameObject inst)
        {
            var bot = inst.AddComponent<PatrolBot>();
            if (!bot.Setup(host.robotFaceLed)) { Object.Destroy(bot); return; }
            bots[key] = bot;
            if (host.robotRigs != null && host.robotGlyphs && RobotRigs.Attach(inst, host.robotRigs, host.robotGlyphs, key, PatrolBot.Kind) is RobotFace face && inst.GetComponent<RobotIdle>() is { } idle)
            { robotHeads[key] = (idle, false); if (face.Screens == 0) Debug.LogWarning($"Play: {key}: PatrolBot104 has no _Face sheet: its face stays dark"); }
        }

        /// <summary>Per frame, after the cars and robots have turned (Cars): each robot posed from its gun, its shots and the siren's rule.</summary>
        void Bots()
        {
            var g = host.Game; if (g == null || bots.Count == 0) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            foreach (var kv in bots)
            {
                var bot = kv.Value; if (!bot || !things.TryGetValue(kv.Key, out var th) || !th.shown) continue;
                bool patrol = kv.Key == "patrol"; var gun = patrol ? g.PatrolGun : UnitGun(g, kv.Key);
                bool down = patrol ? g.PatrolDown : gun != null && gun.Down;
                bool drawn = WeaponOut(gun);
                bool siren = !down && (patrol ? g.Alert >= 2 || drawn : true);
                bool alarmed = robotHeads.TryGetValue(kv.Key, out var rh) && rh.alarmed;
                bot.Drive(dt, Time.time, drawn, shotEvents.TryGetValue(kv.Key, out var n) ? n : 0, gun?.Health ?? 0, siren, down, alarmed || drawn);
            }
        }

        /// <summary>A robot's weapon is out while the engine has it drawn (not holstering) or engaging a target: a robot's gun is built in, so the engine's
        /// patrol fights by its target (Game.PreFight) and never sets Drawn; the target clears when it loses its foe, and the hatch closes.</summary>
        public static bool WeaponOut(Gun gun) => gun != null && !gun.Down && gun.W != null && (gun.Drawn && gun.HolsterAt < 0 || gun.Target.Length > 0);

        static Gun UnitGun(Game g, string key)
        {
            if (!key.StartsWith("unit/")) return null; var id = key.Substring(5);
            foreach (var u in g.Units) if (u.Id == id) return u.Gun;
            return null;
        }

        /// <summary>A robot's muzzle (the gadget's barrel tip, world), so the shot's flash and line start at its sidearm; null for anything else.</summary>
        public Vector3? MuzzleAt(string key) => bots.TryGetValue(key, out var b) && b && b.isActiveAndEnabled && b.Ready && b.SlideM > 0.05f ? b.Muzzle : null;
    }
}
