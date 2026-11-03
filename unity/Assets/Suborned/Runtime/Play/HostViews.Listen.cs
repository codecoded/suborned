using System.Collections.Generic;
using System.IO;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 085, engine 0.39.0 (briefs/085/motive/discreet-listen.md), view only: the discreet Listen as it plays on the street. Whoever a Listen is on has someone step out to talk to
    /// (mission.json listenPartners: Milena Rask): her view walks out of the door to them, they face each other and talk, and she goes back after, or at once if a fight starts. The engine doesn't know her:
    /// nobody can click her (crowd views aren't pickable), she blocks no cone. The listener stands side on to the person, never looking at them; given a chair by the engine he slides the metre on and sits.</summary>
    public sealed partial class HostViews
    {
        public const float ListenPartnerGapM = 1.15f, ListenPartnerWalk = 1.4f, ListenPartnerRun = 2.7f, ListenLingerS = 1.5f, ListenSlideS = 0.8f;
        sealed class Partner { public Game For; public string Key = "", Role = ""; public readonly List<Vector3> Path = new(); public float Len, At, Linger = ListenLingerS; }
        Partner listenP; float scoutFaceYaw = float.NaN; string scoutFaceFor = "";

        /// <summary>A partner's view is the view's own while she's out: the positions leave it alone.</summary>
        bool ListenOwns(string key) => listenP != null && listenP.Key == key;
        /// <summary>The partner standing with this person now (her view's key), else null.</summary>
        public string ListenPartnerUp(string role) => listenP != null && listenP.Role == role ? listenP.Key : null;
        /// <summary>The partner's head while she's out, for her street lines.</summary>
        public Vector3? ListenPartnerHead() => listenP != null && things.TryGetValue(listenP.Key, out var x) ? x.t.position + Vector3.up * 1.8f : (Vector3?)null;
        /// <summary>For the proof: how far along her walk (0 at the door, 1 with the person).</summary>
        public float ListenPartnerAlong => listenP == null ? -1f : listenP.At / Mathf.Max(0.01f, listenP.Len);

        void ListenView(Game g)
        {
            if (g == null || g.Range) return;
            ListenPartnerStep(g);
            // the listener: side on, never at the person; turned once, and again only if the person ends up in front of him or dead behind him
            for (int k = 0; k < 4; k++)
            {
                string key = OpKeys[k]; if (g.ListenSpotNow(Op.Name((OpId)k)) is not { } n || g.Ops[k].Walk != null || !things.TryGetValue(key, out var me) || !me.shown || !figs.TryGetValue(key, out var f)) { if (key == scoutFaceFor) { scoutFaceFor = ""; scoutFaceYaw = float.NaN; } continue; }
                if (!things.TryGetValue(n.target, out var them)) continue; var to = them.t.position - me.t.position; to.y = 0f; if (to.sqrMagnitude < 0.01f) continue;
                float toYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, off = Mathf.Abs(Mathf.DeltaAngle(float.IsNaN(scoutFaceYaw) ? f.yaw : scoutFaceYaw, toYaw));
                if (float.IsNaN(scoutFaceYaw) || off < 80f || off > 150f) { float side = Mathf.DeltaAngle(toYaw, f.yaw) >= 0f ? 105f : -105f; scoutFaceYaw = toYaw + side; scoutFaceFor = key; }
                faces[key] = Quaternion.Euler(0f, scoutFaceYaw, 0f) * Vector3.forward;
            }
        }

        void ListenPartnerStep(Game g)
        {
            var who = host.ListenPartners; if (who == null) return;
            if (listenP != null && listenP.For != g) { var old = things[listenP.Key]; things[listenP.Key] = (old.t, old.to, old.to, false); listenP = null; }
            string role = null; for (int k = 0; k < 4; k++) if (g.Ops[k].Busy?.Do == "listen") role = g.Ops[k].Busy.Target;
            bool fight = g.FightOn || g.Alert >= 2, talk = role != null && who.ContainsKey(role) && !fight;
            if (listenP == null) { if (!talk || !ListenPartnerOut(g, role, who[role])) return; }
            var p = listenP; float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f), pace = host.ConvoOn || host.Paused || g.Over ? 0f : host.Speed;
            if (!fight && (talk || host.ConvoOn || host.ConvoPending || host.StreetLinesDue)) p.Linger = ListenLingerS; else p.Linger -= dt * pace;
            bool back = fight || p.Linger <= 0f; float speed = fight ? ListenPartnerRun : ListenPartnerWalk, target = back ? 0f : p.Len, was = p.At;
            p.At = Mathf.MoveTowards(p.At, target, speed * dt * pace);
            var pos = PathAt(p, p.At); var dir = (PathAt(p, Mathf.Min(p.Len, p.At + 0.1f)) - PathAt(p, Mathf.Max(0f, p.At - 0.1f))).normalized * (back ? -1f : 1f);
            var x = things[p.Key]; bool moving = p.At != was;
            if (back && p.At <= 0f) { things[p.Key] = (x.t, pos, pos, false); x.t.gameObject.SetActive(false); listenP = null; return; }
            things[p.Key] = (x.t, moving ? pos - dir * speed * 0.1f : pos, pos, true); if (!x.t.gameObject.activeSelf) x.t.gameObject.SetActive(true); x.t.position = pos;
            if (p.At >= p.Len - 0.01f && things.TryGetValue(p.Role, out var them))
            {   // they face each other and talk; her own facing is the engine's and is only drawn away from it
                var d = them.t.position - pos; d.y = 0f; if (d.sqrMagnitude > 0.01f) { faces[p.Key] = d.normalized; faces[p.Role] = -d.normalized; }
                phases[p.Key] = "together"; phases[p.Role] = "together";
            }
        }

        /// <summary>Her walk: from just inside the door, by the walk grid, to a person's arm's length from them on the door's side.</summary>
        bool ListenPartnerOut(Game g, string role, (string name, string from) to)
        {
            int i = g.Walkers.FindIndex(w => w.P.Name == to.name); if (i < 0 || i >= crowdKeys.Length || !things.TryGetValue(crowdKeys[i], out var x) || !things.TryGetValue(role, out var them) || !them.shown) return false;
            Pt door; try { door = g.S.At("d:" + to.from); } catch (System.Exception) { return false; }
            var dw = frame.World(door); var cf = g.S.CafeFacing; var inward = frame.World(new Pt(door.X + cf.X, door.Y + cf.Y)) - dw; inward.y = 0f; inward.Normalize();
            float edge = 0.2f; while (edge < 3f && g.Walk.Free(frame.Engine(dw + inward * edge))) edge += 0.1f;
            // beside her along the front and a little toward the door, rather than straight in line with it: a lens out on the street then has them both in profile
            var tp = them.t.position; var along = new Vector3(inward.z, 0f, -inward.x); var meet = tp + along * ListenPartnerGapM; float bestOff = float.MaxValue; bool anyFree = false;
            foreach (float side in new[] { 1f, -1f }) { var q = tp + (along * (side * 0.88f) + inward * 0.47f) * ListenPartnerGapM; bool free = g.Walk.Free(frame.Engine(q)); float off = (q - dw).sqrMagnitude; if (free && !anyFree || free == anyFree && off < bestOff) { anyFree = free; bestOff = off; meet = q; } }
            var meetPt = g.Walk.NearestFree(frame.Engine(meet)) ?? frame.Engine(meet);
            var p = new Partner { For = g, Key = crowdKeys[i], Role = role }; p.Path.Add(dw + inward * (edge + 0.2f)); p.Path.Add(dw);
            var route = g.Walk.Route(door, meetPt); if (route != null) foreach (var q in route) p.Path.Add(frame.World(q)); else p.Path.Add(frame.World(meetPt));
            for (int k = 1; k < p.Path.Count - 1; k++) { var o = p.Path[k] - tp; o.y = 0f; if (o.magnitude < ListenPartnerGapM) p.Path[k] = new Vector3(tp.x, p.Path[k].y, tp.z) + (o.sqrMagnitude > 0.01f ? o.normalized : along) * ListenPartnerGapM; } // round her, never through her
            for (int k = 1; k < p.Path.Count; k++) p.Len += Vector3.Distance(p.Path[k - 1], p.Path[k]);
            listenP = p; return true;
        }
        static Vector3 PathAt(Partner p, float at)
        {
            for (int k = 1; k < p.Path.Count; k++) { float l = Vector3.Distance(p.Path[k - 1], p.Path[k]); if (at <= l || k == p.Path.Count - 1) return Vector3.Lerp(p.Path[k - 1], p.Path[k], l > 0f ? Mathf.Clamp01(at / l) : 1f); at -= l; }
            return p.Path[0];
        }

        // the chair: the engine holds him about a metre off it; the view slides him on and sits him, and gets him up after
        float listenSeatK; float listenStandS; string listenChair = "", listenSeatKey = "";
        void ListenSeat(string key, Fig f, Transform t, Game g, ref float speed, ref (Vector3 at, float yaw, int from, int to)? seat)
        {
            if (seat != null || !key.StartsWith("op/") || host.CafeSeats == null || !Has(f, "sit")) return;
            string ag = key[3..]; var n = g.ListenSpotNow(ag); var op = g.Ops[(int)Op.Parse(ag)]; float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f), pace = host.Paused || g.Over ? 0f : Mathf.Max(1f, host.Speed);
            bool wants = n is { } s && s.chair.Length > 0 && op.Walk == null;
            if (wants) { listenChair = n.Value.chair; listenSeatKey = key; listenStandS = SitTicks / (float)Game.Hz; } else if (key != listenSeatKey) return; // the one agent on the chair
            if (listenChair.Length == 0 || !(listenSeatK > 0f || wants) || !host.CafeSeats.Chairs.TryGetValue(listenChair, out var ch)) return;
            bool hold = !wants && host.ConvoOn && op.Walk == null; var at = frame.World(ch.at); at.y = t.position.y; var fw = frame.World(new Pt(ch.at.X + ch.face.X, ch.at.Y + ch.face.Y)) - frame.World(ch.at); float yaw = Mathf.Atan2(fw.x, fw.z) * Mathf.Rad2Deg;
            if (listenSeatK >= 1f && (wants || hold)) { seat = (at, yaw, 0, int.MaxValue); f.seated = true; return; }
            if (listenSeatK >= 1f && op.Walk == null && listenStandS > 0f) { listenStandS -= dt * pace; seat = (at, yaw, 0, g.Tick + 1); f.seated = true; return; } // up: sit_to_stand, unless he's already off
            listenSeatK = Mathf.MoveTowards(listenSeatK, wants ? 1f : 0f, dt * pace / ListenSlideS); if (listenSeatK <= 0f) { listenChair = ""; return; }
            var was = f.model.position; var to = Vector3.Lerp(t.position, at, Mathf.SmoothStep(0f, 1f, listenSeatK)); f.model.position = to; var d = wants ? at - t.position : t.position - at; d.y = 0f; if (d.sqrMagnitude > 0.01f) { f.yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; f.model.rotation = Quaternion.Euler(0f, f.yaw, 0f); }
            speed = Mathf.Max(speed, 1.25f);
        }
        /// <summary>For the proof: seated by a Listen's chair now.</summary>
        public bool ListenSeatedNow => listenSeatK >= 1f && listenChair.Length > 0;
    }

    public sealed partial class HandoverHost
    {
        /// <summary>mission.json listenPartners, by role (the sim bundle's copy); null when absent. View only.</summary>
        public Dictionary<string, (string name, string from)> ListenPartners { get { if (!partnersTried) { partnersTried = true; try { var path = System.IO.Path.Combine(SimBundle.Dir, "handover.listen-partners.json"); if (File.Exists(path)) { partners = new(); foreach (var e in JNode.Parse(File.ReadAllText(path), "handover.listen-partners.json")["partners"].Items) partners[e["role"].Str()] = (e["name"].Str(), e["from"].Str()); } } catch (System.Exception ex) { partners = null; Debug.LogWarning($"Listen partners: {ex.Message}; nobody steps out"); } } return partners; } }
        Dictionary<string, (string name, string from)> partners; bool partnersTried;
    }
}
