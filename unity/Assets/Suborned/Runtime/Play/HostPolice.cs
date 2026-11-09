using System;
using Suborned.Sim;

namespace Suborned.Play
{
    /// <summary>Brief 101 (Latch, 0.2.302): each response officer is his own target. A pick on the figure security/i is the engine's officer SecGuns[i] by his
    /// id (security-1, security-2), so the hover outline, the ring, the stack dots and a fire order land on the one under the pointer, not always on the first.
    /// The bare "security" (a scripted order, the panel guard's list) stays the unit: the engine's nearest officer with a line, drawn by the first one up.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The officer the view security/i draws: his engine id, else the bare "security".</summary>
        string OfficerOfView(string view)
        {
            int i = view.Length > 9 && int.TryParse(view.AsSpan(9), out var k) ? k : -1; // "security/".Length
            return Game != null && i >= 0 && i < Game.SecGuns.Length ? Game.SecGuns[i].Id : "security";
        }

        /// <summary>An officer's index in the engine (SecGuns); -1 for the bare unit or anything else.</summary>
        int OfficerIndex(string key)
        {
            if (Game == null || key == null) return -1;
            for (int i = 0; i < Game.SecGuns.Length; i++) if (Game.SecGuns[i].Id == key) return i;
            return -1;
        }

        /// <summary>The view a police target is drawn by: his own security/i (the views draw two officers; a third would share the second's, as AttnView says),
        /// and for the bare unit the first officer up.</summary>
        string OfficerView(string key)
        {
            int i = OfficerIndex(key);
            if (i < 0 && Game != null) for (int k = 0; k < Game.SecGuns.Length && i < 0; k++) if (Game.SecOfficerPose(k).HasValue) i = k;
            return "security/" + Math.Clamp(i, 0, 1);
        }

        /// <summary>The engine's target for a police target: his own id (the engine fires on that officer), else the bare unit (its nearest with a line).</summary>
        string OfficerTarget(Target t) => t != null && OfficerIndex(t.Key) >= 0 ? t.Key : "security";

        /// <summary>Where the engine has a police target: the officer's own pose, else the unit's lead.</summary>
        Pt? OfficerPos(Target t) { int i = t == null ? -1 : OfficerIndex(t.Key); return i >= 0 ? Game.SecOfficerPose(i)?.pos : Game.SecurityPose()?.pos; }
    }
}
