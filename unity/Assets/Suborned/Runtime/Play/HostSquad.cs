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

        // ---------------------------------------------------------------- a Fire way that names nobody (Sky on 0.2.212: "I didn't seem to have them all aiming")

        /// <summary>A way taken from the ring, a quick click or E: nobody was named, so a Fire goes to the whole squad, as the right hold does (GDD §06 group fire). DoRow stays for routes that
        /// name one agent: a row in the full panel, Shift+1-4.</summary>
        void DoWay(PanelRow r, Target t, Vector2 at, Vector3? world, string source)
        {
            if (r?.Available == true && r.I?.Order?.Do == "fire" && SquadFire(r.I.Order, t, at, source)) return;
            DoRow(r, t, at, world, source); // any other way; or a Fire only a busy agent could take: the row's own path (HandoverHost.IssueCore: Fire is urgent, his act is cancelled or waited for), as the wedge offered
        }

        /// <summary>One order per shooter on one tick, through one going-loud card (Fight): a held wedge has already held it. Those who can't say why, on their plates. False: nobody takes it. The agent in hand out of
        /// reach is handed to the engine's walk, as FireRow does for him (the way was priced with its walk in).</summary>
        bool SquadFire(Order o, Target t, Vector2 at, string source)
        {
            var who = Shooters(false, o.Target, o.Force); if (who.Count == 0) return false;
            Shooters(false, o.Target, o.Force, mark: true); // each one left out says why
            if (Driving && who.Contains(dAgent) && Game.PosOf(o.Target) is Pt tp && !(G.Within(Game.Ops[dAgent].Pos, tp, Game.Ops[dAgent].Gun.W.MaxCm) && Game.S.Sight(Game.Ops[dAgent].Pos, tp, Game.R.Eye))) DriveAutoBegin();
            var list = new List<Order>(); foreach (var k in who) list.Add(new Order { Op = Op.Name((OpId)k), Do = "fire", Target = o.Target, Force = o.Force });
            sweepLast = o.Target; Fight(list, source, at, TargetWorld(t), $"fire on {Name(o.Target)}");
            LocksDrop(who); FireLocks.Add(new FireSession { Agents = who, Id = o.Target, Name = FireName(o.Target), Force = o.Force, Start = Time.unscaledTime });
            FireLogAdd($"way-fire\t{string.Join(",", who)}\t{o.Target}"); return true;
        }

        /// <summary>The faces on a Fire wedge: who it will reach now (the same Shooters as the order and the right hold's pill), else the row's givers.</summary>
        List<OpId> FireFaces(Order o, List<OpId> givers)
        {
            var who = Shooters(false, o.Target, o.Force); if (who.Count == 0) return givers;
            return who.ConvertAll(k => (OpId)k);
        }

        /// <summary>Shift+1-4 in the ring is one agent's own action: on a Fire wedge, that agent alone. False: not a Fire wedge.</summary>
        bool RingKeyFire(RingState rs, int seg)
        {
            var s = rs.Segs[seg]; if (s.Row?.I?.Order?.Do != "fire" || !s.Row.Available) return false;
            var plan = rs.Plan; var at = rs.Screen; SideTake(s, plan.T); CloseRing(); DoRow(s.Row, plan.T, at, plan.Pn.World, "left"); return true;
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
