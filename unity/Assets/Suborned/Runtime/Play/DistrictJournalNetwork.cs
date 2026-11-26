using System;
using System.Collections.Generic;
using System.Linq;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    public sealed partial class DistrictJournal
    {
        bool writingOrder;
        CitizenAct chosenAct;
        int chosenActor, chosenStreet;
        string openList = "";
        static readonly CitizenAct[] Acts = { CitizenAct.Block, CitizenAct.Rally, CitizenAct.Guard, CitizenAct.Protect };
        static string Verb(CitizenAct act) => act switch
        {
            CitizenAct.Block => "district-block",
            CitizenAct.Rally => "district-rally",
            CitizenAct.Guard => "district-guard",
            _ => "district-protect"
        };

        void Locate(int resident)
        {
            var at = host.Game.DistrictPersonAt(resident);
            if (!at.HasValue) return;
            tracking = resident;
            Close();
            host.LookAt(host.frame.World(at.Value));
        }

        void NetworkOverview()
        {
            var d = host.Game.District;
            if (d.Network.Requests.Count == 0) return;
            Label(pages, "People at work", 19, Gold);
            foreach (var order in d.Network.Requests)
            {
                Label(pages, DistrictNetwork.Name(order.Act) + "  ·  " + order.Place, 16, Ivory);
                Label(pages, order.Update, 14, Blue);
                Button(pages, "Locate " + d.Map.People[order.Actor].Name, () => Locate(order.Actor));
                Button(pages, "Call it off", () =>
                {
                    host.Issue(new Order { Op = "cell", Do = "district-recall", Target = d.Map.People[order.Actor].Id });
                    Page();
                });
            }
        }

        public void OpenCitizenOrders()
        {
            writingOrder = true;
            openList = "";
            chosenActor = person;
            chosenStreet = 0;
            chosenAct = CitizenAct.Block;
            reply = "";
            Page();
        }

        void SelectField(string title, List<string> options, int selected)
        {
            Label(pages, title, 15, Blue);
            var field = Button(pages, options[selected] + "   ▾", () => { openList = title; reply = ""; Page(); });
            field.style.borderLeftWidth = 2;
            field.style.borderLeftColor = Gold;
            field.style.marginBottom = 10;
        }

        bool ChoiceList(string title, List<string> options, int selected, Action<int> select)
        {
            if (openList != title) return false;
            Label(pages, title, 19, Gold);
            Button(pages, "‹  Order details", () => { openList = ""; Page(); });
            for (int n = 0; n < options.Count; n++)
            {
                int index = n;
                var option = Button(pages, options[n], () => { select(index); openList = ""; reply = ""; Page(); });
                if (n == selected) option.style.color = Brass;
            }
            return true;
        }

        void CitizenOrderPage()
        {
            var g = host.Game;
            var d = g.District;
            Button(pages, "‹  Conversation", () => { writingOrder = false; Page(); });
            Label(pages, "Call in a favour", 22, Gold);
            var routes = g.CitizenRoutes(person);
            if (routes.Count == 0)
            {
                Label(pages, "They're already committed or unwilling to carry another request.", 15, Ivory);
                return;
            }
            var names = routes.Select(p => p.Length == 1 ? "Ask them directly" : d.Map.People[p[p.Length - 1]].Name).ToList();
            int choice = Math.Max(0, routes.FindIndex(p => p[p.Length - 1] == chosenActor));
            var chain = routes[choice];
            chosenActor = chain[chain.Length - 1];
            var actions = Acts.Select(DistrictNetwork.Name).ToList();
            var locations = d.Map.Streets.Select(s => DistrictMap.StreetName(s.Id)).ToList();
            locations.Insert(0, "Here");
            const string what = "What do you need?", where = "Where?", who = "Who should carry it out?";
            int actIndex = Array.IndexOf(Acts, chosenAct);
            if (ChoiceList(what, actions, actIndex, i => chosenAct = Acts[i]) ||
                ChoiceList(where, locations, chosenStreet, i => chosenStreet = i) ||
                ChoiceList(who, names, choice, i => chosenActor = routes[i][routes[i].Length - 1])) return;
            SelectField(what, actions, actIndex);
            if (chosenAct != CitizenAct.Guard) SelectField(where, locations, chosenStreet);
            SelectField(who, names, choice);
            if (chain.Length > 1) Label(pages, d.Network.Describe(chain), 16, Gold);
            bool known = chain.Length == 1 || d.Network.KnowsCell[chosenActor];
            Label(pages, chain.Length == 1 ? "They will know the request came from you." :
                known ? "The request goes through their contact. They already know the cell." :
                d.Map.People[chosenActor].Name + " will know only who called them.", 15, Ivory);
            string terms = chosenAct switch
            {
                CitizenAct.Block => "Traffic waits until the road clears.",
                CitizenAct.Rally => "Residents will gather. The crowd can stop traffic and raise Alert to 2.",
                CitizenAct.Guard => "The guard and the person you send can both die. Alert rises to 2.",
                _ => "They'll defend a nearby cell member until recalled. They can be hurt or killed."
            };
            Label(pages, terms + (chosenAct != CitizenAct.Protect && known ? " The public act adds 1 Trail." : ""), 14, Ivory);

            if (g.CitizenResolve(person, chosenActor) < DistrictNetwork.Needed(chosenAct))
            {
                Label(pages, "They won't risk that yet. Keep a promise, stand surety, or build the cell's standing.", 15, Gold);
                return;
            }
            var at = g.DistrictPersonAt(person);
            int op = at.HasValue ? Speaker(at.Value, true) : -1;
            if (op < 0) return;
            Button(pages, chain.Length == 1 ? "Ask in person" : "Pass the word", () =>
            {
                var order = new Order
                {
                    Op = Op.Name((OpId)op), Do = Verb(chosenAct), Target = d.Map.People[person].Id,
                    To = d.Map.People[chosenActor].Id, Approach = chosenStreet == 0 ? "here" : d.Map.Streets[chosenStreet - 1].Id
                };
                if (host.Issue(order)) Open();
                else { reply = LastAnswer(); Page(); }
            });
            if (reply.Length > 0) Label(pages, reply, 15, Gold);
        }
    }
}
