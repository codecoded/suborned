using System;
using System.Collections.Generic;
using System.Linq;

namespace Suborned.Sim
{
    public sealed partial class Game
    {
        public int CitizenResolve(int contact, int actor) =>
            Math.Max(District!.Trust[contact], District.Trust[actor]) + (Standing >= 4 ? 3 : Standing >= 2 ? 1 : 0);

        public List<int[]> CitizenRoutes(int first)
        {
            var d = District!;
            return d.Network.Routes(first, i => !ContactLost(i) && !d.Network.Busy(i) && d.Trust[i] >= 0);
        }

        string SendCitizen(Op op, Order o, int first, CitizenAct act)
        {
            var d = District!;
            var net = d.Network;
            if (d.Trust[first] < 1 && Standing < 4)
                return No("not-a-contact", "Earn their trust before asking for a favour.");
            if (!d.Map.Index.TryGetValue(o.To, out int actor))
                return No("target-unknown", "Choose who will carry this out.");
            var chain = CitizenRoutes(first).Find(p => p[p.Length - 1] == actor);
            if (chain == null)
                return No("no-leverage", "There is no available introduction between those two people.");
            int resolve = CitizenResolve(first, actor);
            if (resolve < DistrictNetwork.Needed(act))
                return No("no-leverage", "They need stronger personal trust or a better opinion of the cell.");
            if (act == CitizenAct.Guard && GuardDown)
                return No("target-down", "The guard is already down.");
            Pt site = op.Pos;
            string place = d.Map.StreetAt(site);
            if (act == CitizenAct.Guard)
            {
                site = S.Stand(S.GuardPost);
                place = "the Records Office";
            }
            else if (o.Approach != "here")
            {
                var road = Array.Find(d.Map.Streets, s => s.Id == o.Approach);
                if (road == null) return No("target-unknown", "Choose a street in the district.");
                site = road.Path.At(road.Path.Length / 2);
                place = DistrictMap.StreetName(road.Id);
            }
            if (o.Approach == "here" && (act == CitizenAct.Block || act == CitizenAct.Rally))
                site = d.Map.ClosestStreetPoint(site);
            var request = new CitizenRequest { Act = act, Chain = chain, Site = site, Place = place, Resolve = resolve, Due = Tick + DistrictNetwork.CallTicks };
            net.Requests.Add(request);
            net.KnowsCell[first] = true;
            net.AskedBy[first] = "the cell";
            d.Release(first, Tick);
            SayRequest(request, first, chain.Length == 1
                ? $"{d.Map.People[first].Name} agreed to go to {place} for you."
                : $"{d.Map.People[first].Name} will call {d.Map.People[chain[1]].Name} on your behalf.");
            if (chain.Length == 1) StartCitizenWalk(request);
            return "";
        }

        void SayRequest(CitizenRequest r, int who, string words)
        {
            r.Update = words;
            District!.Memory[who] = words;
            District.News = words;
            Note("district.citizen-order", words);
        }

        void StartCitizenWalk(CitizenRequest r)
        {
            var d = District!;
            var at = DistrictPersonAt(r.Actor);
            int arrival = at.HasValue ? d.Network.Send(r.Actor, at.Value, r.Site, Tick) : -1;
            if (arrival < 0)
            {
                EndCitizenRequest(r, $"{d.Map.People[r.Actor].Name} could not reach {r.Place}.", false);
                return;
            }
            r.Stage = CitizenStage.Walking;
            r.Due = arrival;
            string by = d.Network.AskedBy[r.Actor];
            SayRequest(r, r.Actor, $"{d.Map.People[r.Actor].Name} is walking to {r.Place} at the request of {by}.");
        }

        void CitizenStep()
        {
            var d = District!;
            var net = d.Network;
            net.Disperse(Tick);
            for (int k = net.Requests.Count - 1; k >= 0; k--)
            {
                var r = net.Requests[k];
                if (ContactLost(r.Chain[r.Heard]) || ContactLost(r.Actor))
                {
                    EndCitizenRequest(r, "Word came back that the request could go no further.", false);
                    continue;
                }
                if (Tick < r.Due) continue;
                if (r.Stage == CitizenStage.Calling)
                {
                    int from = r.Chain[r.Heard];
                    int next = r.Chain[++r.Heard];
                    if (ContactLost(next) || d.Trust[next] < 0)
                    {
                        EndCitizenRequest(r, $"{d.Map.People[next].Name} could not pass the request on.", false);
                        continue;
                    }
                    net.AskedBy[next] = d.Map.People[from].Name;
                    SayRequest(r, next, $"{d.Map.People[next].Name} took the call from {d.Map.People[from].Name}.");
                    if (r.Heard == r.Chain.Length - 1) StartCitizenWalk(r);
                    else r.Due = Tick + DistrictNetwork.CallTicks;
                }
                else if (r.Stage == CitizenStage.Walking) CitizenArrives(r);
                else if (r.Stage == CitizenStage.Gathering) RallyArrives(r);
                else if (r.Act == CitizenAct.Guard) CitizenGuardFight(r);
                else if (r.Act != CitizenAct.Protect) EndCitizenRequest(r, $"The street at {r.Place} is clear again.", true);
            }
        }

        void CitizenArrives(CitizenRequest r)
        {
            var d = District!;
            if (r.Act == CitizenAct.Rally)
            {
                r.Stage = CitizenStage.Gathering;
                var people = Enumerable.Range(0, d.Map.People.Length)
                    .Where(i => i != r.Actor && d.Roams(i) && !d.Network.Busy(i) && !ContactLost(i) && !d.Meetings[i].HasValue)
                    .Select(i => (person: i, at: DistrictPersonAt(i)))
                    .Where(p => p.at.HasValue).OrderBy(p => G.D2(p.at!.Value, r.Site)).ThenBy(p => p.person);
                int due = Tick + 1;
                foreach (var p in people)
                {
                    var offset = new Pt(r.Crowd.Count % 2 == 0 ? -225 : 225, (r.Crowd.Count / 2) * 250 - 140);
                    int arrives = d.Network.Send(p.person, p.at!.Value, r.Site + offset, Tick);
                    if (arrives < 0) continue;
                    r.Crowd.Add(p.person);
                    d.Network.AskedBy[p.person] = d.Map.People[r.Actor].Name;
                    d.Memory[p.person] = $"{d.Map.People[r.Actor].Name} asked me to join them on {r.Place}.";
                    due = Math.Max(due, arrives);
                    if (r.Crowd.Count == 4) break;
                }
                if (r.Crowd.Count == 0) EndCitizenRequest(r, "Nobody came out to join the protest.", false);
                else
                {
                    r.Due = due;
                    SayRequest(r, r.Actor, $"Residents are making their way to {r.Place} to join {d.Map.People[r.Actor].Name}.");
                }
                return;
            }
            if (r.Act == CitizenAct.Guard)
            {
                if (GuardDown) { EndCitizenRequest(r, "Word came back that the guard was already down.", false); return; }
                var guard = GuardPose();
                if (!guard.HasValue || !G.Within(guard.Value.pos, r.Site, 650))
                {
                    r.Due = Tick + Hz;
                    return;
                }
                RaiseAlert(2, "district-guard-fight");
            }
            r.Stage = CitizenStage.Acting;
            r.Due = r.Act == CitizenAct.Protect ? INF : Tick + T(r.Act == CitizenAct.Guard ? 3 : 75);
            string action = r.Act == CitizenAct.Block ? "is holding up the road" : r.Act == CitizenAct.Guard ? "has challenged the guard" : "will stand by any cell member in trouble";
            SayRequest(r, r.Actor, $"{d.Map.People[r.Actor].Name} {action} at {r.Place}.");
            TracePublicRequest(r);
        }

        void RallyArrives(CitizenRequest r)
        {
            var present = r.Crowd.Where(p => !ContactLost(p) && DistrictPersonAt(p) is Pt at && G.Within(at, r.Site, 900)).ToArray();
            if (present.Length == 0) { EndCitizenRequest(r, "The gathering dispersed before it could start.", false); return; }
            r.Stage = CitizenStage.Acting;
            r.Due = Tick + T(60);
            foreach (var person in present) District!.Network.Met(r.Actor, person);
            RaiseAlert(2, "district-riot");
            SayRequest(r, r.Actor, $"{present.Length} residents joined {District!.Map.People[r.Actor].Name}. Traffic has stopped at the riot on {r.Place}.");
            TracePublicRequest(r);
        }

        void TracePublicRequest(CitizenRequest r)
        {
            if (r.Act != CitizenAct.Protect && District!.Network.KnowsCell[r.Actor])
                AddTrail(1, "district-known-organiser");
        }

        void CitizenGuardFight(CitizenRequest r)
        {
            var d = District!;
            if (GuardDown) { EndCitizenRequest(r, "The guard is down. The post has fallen silent.", true); return; }
            string name = d.Map.People[r.Actor].Name;
            int harm = r.Resolve >= 4 ? 2 : 1;
            Damage("guard", GuardGun, harm, name, false, true);
            HurtResident(r.Actor, 1);
            if (GuardDown)
            {
                string news = d.Network.Fallen[r.Actor] ? $"{name} and the guard both died in the fight." : $"{name} killed the guard. The office will notice the empty post.";
                EndCitizenRequest(r, news, true);
                return;
            }
            if (d.Network.Fallen[r.Actor]) { EndCitizenRequest(r, $"{name} died fighting the guard. Their neighbours heard who asked.", false); return; }
            r.Due = Tick + T(3);
        }

        // Use the same hit that would have reached a cell member. Residents only intervene where they stand.
        bool CitizenDefends(string target, string attacker, int damage)
        {
            if (District == null || !IsOpId(target) || IsOpId(attacker)) return false;
            var op = OpOf(Sim.Op.Parse(target));
            if (op.InCar || op.Inside || op.LeftMap || op.Down) return false;
            var d = District;
            var r = d.Network.Requests.Find(o => o.Act == CitizenAct.Protect && o.Stage == CitizenStage.Acting &&
                !ContactLost(o.Actor) && DistrictPersonAt(o.Actor) is Pt at && G.Within(at, op.Pos, 850));
            if (r == null) return false;
            var name = d.Map.People[r.Actor].Name;
            HurtResident(r.Actor, damage);
            var enemy = GunOf(attacker);
            if (enemy != null && !enemy.Down && PosOf(attacker) is Pt where && G.Within(where, op.Pos, 1200))
                Damage(attacker, enemy, 1, name, false, true);
            if (d.Network.Fallen[r.Actor]) EndCitizenRequest(r, $"{name} died standing up for the {target}.", true);
            else SayRequest(r, r.Actor, $"{name} stood between the {target} and {attacker}.");
            return true;
        }

        void HurtResident(int who, int damage)
        {
            var d = District!;
            var n = d.Network;
            n.Wounds[who] += damage;
            if (n.Wounds[who] < R.HealthCivilian) return;
            n.FellAt[who] = DistrictPersonAt(who) ?? d.Pose(who, Tick).at;
            n.Fallen[who] = true;
            var resident = d.Map.People[who];
            if (districtWalkers.TryGetValue(resident.Id, out var w))
            {
                var at = d.Pose(who, Tick).at;
                w.P.Down = true;
                ShotBody(w.P, at, resident.Id);
            }
            foreach (var t in n.Neighbours(who))
            {
                d.Memory[t.person] = $"{resident.Name} died after taking a request from {n.AskedBy[who]}.";
                if (n.KnowsCell[who]) d.Trust[t.person] = Math.Max(-3, d.Trust[t.person] - 1);
            }
        }

        string RecallCitizen(Order o)
        {
            var d = District!;
            if (!d.Map.Index.TryGetValue(o.Target, out int actor)) return No("target-unknown", "Choose an active request.");
            var r = d.Network.Requests.Find(x => x.Actor == actor);
            if (r == null) return No("target-unknown", "That request has finished.");
            EndCitizenRequest(r, $"{d.Map.People[r.Chain[0]].Name} called the request off.", false);
            return "";
        }

        void EndCitizenRequest(CitizenRequest r, string message, bool completed)
        {
            var d = District!;
            d.Network.Return(r.Actor, Tick);
            foreach (int person in r.Crowd) d.Network.Return(person, Tick);
            d.Network.Requests.Remove(r);
            if (completed) d.Network.Completed++;
            SayRequest(r, r.Actor, message);
        }
    }
}
