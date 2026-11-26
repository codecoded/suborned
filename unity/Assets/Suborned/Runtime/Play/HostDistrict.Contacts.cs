using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    public sealed partial class HandoverHost
    {
        Target StreetTargetAt(Vector2 screen, Target hit)
        {
            var d = Game?.District;
            if (d == null || !HarbourStreet || hit != null && hit.Kind != "crowd") return hit;
            int resident;
            if (hit != null && d.Map.Index.TryGetValue(hit.Id, out int civic) && d.Roams(civic)) resident = civic;
            else resident = HarbourStreet.PickResident(screen);
            if (resident < 0) return hit;
            var p = d.Map.People[resident];
            return new Target { Kind = "resident", Id = p.Id, Key = p.Id, Label = p.Name };
        }

        Vector3? StreetPosition(string id) =>
            Game?.District is DistrictRun d && d.Map.Index.TryGetValue(id, out int who) && Game.DistrictPersonAt(who) is Pt p ? frame.World(p) : (Vector3?)null;

        string StreetName(string id) =>
            Game?.District is DistrictRun d && d.Map.Index.TryGetValue(id, out int who) ? d.Map.People[who].Name : null;

        bool SocialPanel(ActionPanel panel, int onlyAgent)
        {
            if (panel.T.Kind == "contacts") { EarnedCallRows(panel); return true; }
            if (panel.T.Kind != "resident") return false;
            var d = Game.District;
            if (d == null || !d.Map.Index.TryGetValue(panel.T.Id, out int who) || Game.DistrictPersonAt(who) is not Pt at)
                return true;
            var p = d.Map.People[who];
            if (d.Network.Away(who)) { panel.Note = p.Name + " is carrying out a request."; return true; }
            int near = DistrictBook.Speaker(at, true);
            int walker = near >= 0 ? near : DistrictBook.Speaker(at, false);
            int by = walker >= 0 ? walker : (int)Agent;
            if (onlyAgent >= 0 && by != onlyAgent) return true;
            var op = Game.Ops[by];
            void Add(string verb, string label, string icon, string to = "", string money = "", bool bluff = false)
            {
                var order = new Order { Op = Op.Name((OpId)by), Do = verb, Target = p.Id, To = to };
                var i = new Intent { Order = order, Label = label, Lead = icon, Usd = money, Trail = bluff ? "T +1" : "T 0" };
                if (bluff) i.Does = "Both contacts will trust you less.";
                panel.Rows.Add(new PanelRow { By = (OpId)by, I = i, Verb = label, Available = true });
            }
            if (near < 0)
            {
                if (op.InCar && Able(by))
                {
                    if (Game.DistrictRoadPoint(at).HasValue) Add("district-drive-near", "Drive nearby", "car");
                    Add("district-step-out", "Step out", "s_moving");
                }
                else if (walker >= 0) Add("district-visit", "Walk to meet", "s_moving");
                else panel.Note = "Select an available operative to visit.";
                return true;
            }
            Add("district-meet", "Talk", "listen");
            var job = d.Available(who);
            var prices = Game.R.DistrictPrices;
            if (job != null)
            {
                string Fee(bool bluff) => "+$" + Suborned.Sim.Game.Usd(prices.ErrandFee(bluff));
                Add("district-help", d.Choice(job), "help", job.Key, Fee(job.Stage == 2 && job.Bluff));
                if (job.Stage == 1) Add("district-bluff", d.Choice(job, true), "pressure", job.Key, Fee(true), true);
            }
            if (d.Trust[who] <= 0) Add("district-back", "Help with " + p.Need, "help", money: "-$" + Suborned.Sim.Game.Usd(prices.Help));
            if (d.Trust[who] == 1) Add("district-bond", "Stand surety", "pay", money: "-$" + Suborned.Sim.Game.Usd(prices.Surety));
            if (d.Trust[who] > 0 || Game.Standing >= 4) Add("district-request", "Call in a favour", "call");
            if (d.Trust[who] > 0)
                foreach (var tie in d.Network.Neighbours(who))
                    if (d.Roams(tie.person) && d.Trust[tie.person] <= 0)
                    {
                        var friend = d.Map.People[tie.person];
                        Add("district-refer", "Vouch for " + friend.Name, "ask", friend.Id);
                    }
            return true;
        }

        void StreetAction(PanelRow row, Target target, Vector2 screen)
        {
            var d = Game.District;
            if (d == null || !d.Map.Index.TryGetValue(target.Id, out int who) || Game.DistrictPersonAt(who) is not Pt at) return;
            var op = Game.Ops[(int)row.By];
            var order = row.I.Order;
            switch (order.Do)
            {
                case "district-visit":
                    order.Run = true;
                    if (Issue(order, screen, frame.World(at))) DistrictBook.FollowVisit(who, row.By);
                    return;
                case "district-drive-near":
                    if (Game.DistrictRoadPoint(at) is Pt road) Issue(new Order { Op = order.Op, Do = "drive-car", Point = road }, screen, frame.World(road));
                    return;
                case "district-step-out":
                    Issue(new Order { Op = order.Op, Do = "move", Point = Game.DistrictExitDoor(op) }, screen, frame.World(op.Pos));
                    return;
                case "district-meet":
                    StreetTalk(who, row.By, screen);
                    return;
                case "district-request":
                    DistrictBook.Open(who);
                    if (DistrictBook.IsOpen) DistrictBook.OpenCitizenOrders();
                    return;
            }
            var job = d.Available(who);
            string line = null;
            if (order.Do == "district-help" || order.Do == "district-bluff")
                line = job != null ? d.Speaker(job) : null;
            else if (order.Do == "district-refer" && d.Map.Index.TryGetValue(order.To, out int friend))
                line = "I'll put in a word with " + d.Map.People[friend].Name.Split(' ')[0] + ".";
            else if (order.Do == "district-back") line = "Thank you. That takes a weight off.";
            else if (order.Do == "district-bond") line = "It means something to have your name beside mine.";
            string scene = job != null && (order.Do == "district-help" || order.Do == "district-bluff")
                ? "harbour/" + job.Key + "/" + job.Stage : order.Do + "/" + target.Id;
            if (!Issue(order, screen, frame.World(at))) return;
            if (line != null) PresentStreetLine(who, row.By, scene, line, d.News);
            else Pop(d.News, screen, frame.World(at), Feedback.Info, ReadS(d.News));
        }

        internal bool HoldingStreetSpeaker(string id) => convoWant?.Target == id || ConvoNow?.Target == id;

        internal void StreetTalk(int who, OpId by, Vector2 screen)
        {
            var d = Game.District;
            if (d == null || d.Network.Away(who) || Game.DistrictPersonAt(who) is not Pt at) return;
            var p = d.Map.People[who];
            string agent = Op.Name(by);
            if (!Issue(new Order { Op = agent, Do = "district-meet", Target = p.Id }, screen, frame.World(at))) return;
            var job = d.Available(who);
            string scene = job == null ? "meet/" + p.Id : "harbour/" + job.Key + "/" + job.Stage;
            string line = job != null ? d.Speaker(job) : d.Trust[who] > 0 ? "What do you need?" : "Can I help you?";
            PresentStreetLine(who, by, scene, line, null);
        }

        void PresentStreetLine(int who, OpId by, string scene, string line, string receipt)
        {
            var p = Game.District.Map.People[who];
            var c = new Convo { Agent = (int)by, KeyA = Op.Name(by), KeyB = p.Id, Target = p.Id, Verb = "district-meet", Id = scene, MetKey = scene };
            c.Lines.Add((p.Id, line));
            if (!string.IsNullOrEmpty(receipt)) c.Learned.Add(receipt);
            HarbourStreet.PrepareSpeaker(who);
            OfferConversation(c);
        }
    }
}
