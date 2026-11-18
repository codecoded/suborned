using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 112 (Sky 2026-11-12: "replace our main cell with these characters and animations ... use appropriate animations"): the cell's
    /// own figures (the Happy Mouse bundles, FigureBuild.CellOverrides). Each has its own clip for most of the shared states, and clips for states
    /// only the cell has. Here: which of those a cell member plays, and its props (pistol, carbine, visor) shown as the bundle shows them, by state.
    /// View only: the engine says what the agent is doing (HandoverHost.DoingOf) and whether the gun is out; nothing here is read by it.
    /// A figure with no own states (the stand-ins, everyone else) passes through untouched.</summary>
    public sealed partial class HostViews
    {
        /// <summary>A cell figure's own states and its prop renderers, read once when it is dressed; the props start as idle has them.</summary>
        void CellDress(Fig f, FigureLibrary.Figure e)
        {
            if (e.ownStates.Length == 0) return;
            f.own = new HashSet<string>(e.ownStates);
            var found = new List<Renderer>(); var shown = new List<string>();
            foreach (var r in f.model.GetComponentsInChildren<Renderer>(true))
            {
                int i = System.Array.IndexOf(e.propNodes, r.name); if (i < 0) continue;
                found.Add(r); shown.Add("," + e.propStates[i] + ",");
            }
            f.props = found.ToArray(); f.propIn = shown.ToArray(); CellProps(f, "idle");
        }

        /// <summary>The props the state's action shows (the bundle's per-action list, by state: the gun in the armed states, the visor always).</summary>
        void CellProps(Fig f, string state)
        {
            if (f.props == null || f.propFor == state) return;
            f.propFor = state;
            for (int i = 0; i < f.props.Length; i++)
            {
                bool on = f.propIn[i] == ",*," || f.propIn[i].Contains("," + state + ",");
                if (f.props[i].enabled != on) f.props[i].enabled = on;
            }
        }

        /// <summary>The cell's own clip where the shared set has only idle or walk. Gun out: the armed idle and the armed walk (the gun stays in the
        /// hand instead of vanishing between aims), and the holster clip while the engine puts it away. Gun away and standing at work: the act's
        /// clip, by the engine's verb (watching looks about, the Tech works the cabinet while a loop or a reroute runs or holds, the Scout misleads,
        /// a call is on the phone). `held`: down, carried, seated or speaking in a conversation scene, where the built state stands.</summary>
        string CellState(Fig f, int k, string state, bool armed, bool held)
        {
            if (f.own == null || k < 0 || held) return state;
            bool standing = state is "idle" or "idle_alt";
            if (armed)
            {
                if (standing) return host.DoingOf(k).Verb == "holster" && f.own.Contains("holster") ? "holster" : f.own.Contains("idle_armed") ? "idle_armed" : state;
                return state is "walk" or "walk_f" && f.own.Contains("walk_armed") ? "walk_armed" : state;
            }
            if (!standing) return state;
            var d = host.DoingOf(k); if (d.Phase is not ("acting" or "holding")) return state;
            string act = d.Verb switch { "watch" => "look_around", "loop" or "reroute" => "tinker", "mislead" => "mislead", "call" => "phone", _ => null };
            return act != null && f.own.Contains(act) ? act : state;
        }
    }
}
