using System;
using System.IO;
using System.Text;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 067 E (Suture's 0.2.122 findings §4: "the selection, hovers, ring opens and lock pops live only in the host, so a note like
    /// 'I hovered the guard' cannot be tied to a tick"): the host's own record beside the session script, `&lt;script&gt;.host.jsonl` (as the
    /// `.end.txt` is), one JSON line per change, each with the engine tick and the unscaled time:
    ///  · {"sel": [...]} when the selection changes;
    ///  · {"hover": "kind/key", "pill": state, "words": "..."} when the thing under the pointer, or the pill's state on it, changes;
    ///  · {"ring": "open" | "open-shut" | "take" | "shut", "on": "kind/key", "ways": "..."} as a ring opens, a wedge is taken, it closes untaken;
    ///  · {"pop": code, "text": "..."} for every pop at a click (a lock's code where it has one);
    ///  · {"click": "select" | "ring" | "walk-thing" | "walk-ground", "on": "kind/key"} for every left click in the hold preset.
    /// Outside the engine and the replay: nothing here is an order or is hashed; a failure to write is logged once and the record stops.</summary>
    public sealed partial class HandoverHost
    {
        StreamWriter side; string sidePath = ""; bool sideBroken; int sideLines;
        int sideSel = -1; string sideHover = "\u0000", sidePill = "\u0000"; RingState sideRing; bool sideTaken;
        /// <summary>Where this session's host record is written ("" before a session).</summary>
        public string SidecarPath => sidePath;
        /// <summary>Lines written to this session's host record (the proof reads it).</summary>
        public int SidecarLines => sideLines;
        /// <summary>The code of the pop being made (PopKey, a lock's pop), for the record; cleared by Pop.</summary>
        string popCode = "";

        static string SideTarget(Target t) => t == null ? "" : $"{t.Kind}/{t.Key}";
        static string J(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var b = new StringBuilder(s.Length + 8);
            foreach (var c in s) { if (c == '"' || c == '\\') b.Append('\\').Append(c); else if (c < ' ') b.Append(' '); else b.Append(c); }
            return b.ToString();
        }

        /// <summary>The record for the session now: closed and reopened when the session file changes (Begin).</summary>
        void SideEnsure()
        {
            if (sideBroken || string.IsNullOrEmpty(SessionPath)) return;
            var want = System.IO.Path.ChangeExtension(SessionPath, null) + ".host.jsonl";
            if (side != null && want == sidePath) return;
            SideClose();
            try { side = new StreamWriter(want, false, new UTF8Encoding(false)) { AutoFlush = false }; sidePath = want; sideLines = 0; sideSel = -1; sideHover = "\u0000"; sidePill = "\u0000"; sideRing = null; }
            catch (Exception e) { sideBroken = true; side = null; Debug.LogWarning($"Play: the host record was not opened ({e.Message}); play goes on without it"); }
        }

        void SideClose() { try { side?.Flush(); side?.Dispose(); } catch (Exception) { } side = null; }

        void SideWrite(string body)
        {
            SideEnsure(); if (side == null || Game == null) return;
            try { side.Write("{\"tick\":"); side.Write(Game.Tick); side.Write(",\"t\":"); side.Write(Time.unscaledTime.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)); side.Write(','); side.Write(body); side.Write("}\n"); sideLines++; if (sideLines % 20 == 0) side.Flush(); }
            catch (Exception e) { sideBroken = true; SideClose(); Debug.LogWarning($"Play: the host record stopped ({e.Message})"); }
        }

        /// <summary>One record line: what ("ring", "pop", "click", ...), its value, and the thing it is on.</summary>
        void SideLog(string what, string value, Target on = null, string more = "")
        {
            if (Game == null) return;
            SideWrite($"\"{what}\":\"{J(value)}\"" + (on != null ? $",\"on\":\"{J(SideTarget(on))}\"" : "") + more);
        }

        /// <summary>Per frame, after the input: the selection, the hover and its pill, the ring opening or closing.</summary>
        void SideFrame()
        {
            if (Game == null || sideBroken) return;
            int mask = 0; for (int k = 0; k < 4; k++) if (Selected[k]) mask |= 1 << k;
            if (mask != sideSel)
            {
                sideSel = mask; var sb = new StringBuilder("\"sel\":["); bool first = true;
                for (int k = 0; k < 4; k++) if ((mask & (1 << k)) != 0) { if (!first) sb.Append(','); sb.Append('"').Append(Op.Name((OpId)k)).Append('"'); first = false; }
                SideWrite(sb.Append(']').ToString());
            }
            string hover = SideTarget(Hover), pill = HudToolkit.PillState ?? "";
            if (hover != sideHover || pill != sidePill)
            {
                sideHover = hover; sidePill = pill;
                SideWrite($"\"hover\":\"{J(hover)}\",\"pill\":\"{J(pill)}\",\"words\":\"{J(HudToolkit.PillWords)}\"");
            }
            if (Ring != sideRing)
            {
                if (sideRing != null && !sideTaken) SideLog("ring", "shut", sideRing.Plan?.T);
                if (Ring != null) { var ways = string.Join(" | ", Ring.Segs.ConvertAll(x => x.More ? "..." : (x.Shut ? "~" : "") + x.Word + (x.Shut && x.ShutText.Length > 0 ? " (" + x.ShutText + ")" : ""))); SideLog("ring", Ring.Segs.TrueForAll(x => x.More || x.Shut) ? "open-shut" : "open", Ring.Plan?.T, $",\"ways\":\"{J(ways)}\""); }
                sideRing = Ring; sideTaken = false;
            }
        }

        /// <summary>A wedge taken (RingTake): the record says which, before the ring closes.</summary>
        void SideTake(RingSeg s, Target on)
        {
            sideTaken = true; var o = s.Row?.I?.Order;
            SideLog("ring", "take", on, $",\"way\":\"{J(s.More ? "..." : s.Word)}\",\"do\":\"{J(o?.Do ?? "")}\",\"by\":\"{J(s.By.Count > 0 ? string.Join(",", s.By.ConvertAll(k => Op.Name(k))) : "")}\"");
        }

        /// <summary>Pop's hook: every pop at a click, with the code of the lock or key that made it.</summary>
        void SidePop(string text, string icon)
        {
            var code = popCode; popCode = "";
            SideWrite($"\"pop\":\"{J(code)}\",\"text\":\"{J(text)}\",\"icon\":\"{J(icon)}\"");
        }
    }
}
