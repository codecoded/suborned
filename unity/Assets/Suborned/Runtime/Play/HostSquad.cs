using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 085, Sky's notes on 0.2.177: "when I was controlling the squad, it would sometimes just action one agent" and "only one agent would start to shoot at a target unless I
    /// pressed F". The squad survives third person: T, 1–4, Tab and a portrait change who is in hand, not who is in the squad, and the map finds it as it was left. An agent a squad order
    /// leaves out says so. The rules are the rulebook's; this is only who the host gives an order to.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>This agent takes the lead of the squad (third person: in hand, as DriveFrame reads Agent). One who is not in the selection replaces it, as a key does on the map.</summary>
        public void TakeInSquad(int k)
        {
            if (k < 0 || k > 3) return;
            if (!Selected[k]) { Select((OpId)k, false); return; }
            if (Agent != (OpId)k) Picked = -1;
            Agent = (OpId)k; intentKey = "";
        }

        /// <summary>A walk that left agents at their acts (BusyClick): what each is on, over its head. Without this a squad click moved some and the rest looked like they had not heard.</summary>
        void SayStaying(List<int> busy)
        {
            foreach (var k in busy) if (ActOn(k) is { Length: > 0 } on) { popCode = "busy-stays"; Pop($"Busy: {on}", null, (AgentWorld(k) ?? Vector3.zero) + Vector3.up * 1.1f, Feedback.Info, 2f, "icons/singles/p_time"); }
        }

        // ---------------------------------------------------------------- why an agent took no part in the squad's fire order (moved from HostHold)

        readonly string[] fireRefused = { "", "", "", "" }; readonly float[] fireRefusedAt = { -99f, -99f, -99f, -99f };
        public const float FireRefusalS = 3f;
        /// <summary>The plate's two words for agent k's refused squad fire order, for FireRefusalS after the order ("" none).</summary>
        public string FireRefusalWord(int k)
        {
            if (k < 0 || k > 3 || fireRefused[k].Length == 0 || Time.unscaledTime - fireRefusedAt[k] > FireRefusalS) return "";
            var c = fireRefused[k]; return Text?.DoneWord("fire." + c, null) ?? c.Replace('-', ' ');
        }
        /// <summary>The proof reads it: agent k's last refusal code ("" none) and how long ago.</summary>
        public (string code, float ago) FireRefusalOf(int k) => (fireRefused[k], Time.unscaledTime - fireRefusedAt[k]);
    }
}
