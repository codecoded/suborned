using System;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    public sealed partial class DistrictJournal : MonoBehaviour
    {
        HandoverHost host;
        Game bound;
        UIDocument document;
        PanelSettings settings;
        VisualElement root, sheet, marker;
        Label markerText;
        ScrollView pages;
        bool open;
        int person = -1, tracking = -1, walking = -1, walker = -1;
        string reply = "";
        static DistrictJournal live;

        public bool IsOpen => open;
        public bool Ready => document != null && sheet != null;

        bool Available => host && host.Game?.District != null && !host.Game.Over && host.Fault == null && !host.ShowBriefing && !host.MenuOpen && !host.ConvoOn;

        void Awake()
        {
            host = GetComponent<HandoverHost>();
            live = this;
        }

        void OnDestroy()
        {
            if (live == this)
                live = null;
            if (settings)
                Destroy(settings);
        }

        public static bool Blocks(Vector2 screen)
        {
            return live != null && live.IsOpen;
        }

        public bool Controls()
        {
            if (!Available)
                return false;
            var kb = Keyboard.current;
            if (kb != null && (kb.nKey.wasPressedThisFrame || (open && kb.escapeKey.wasPressedThisFrame)))
            {
                if (open)
                    Close();
                else
                    Open();
                return true;
            }

            return open;
        }

        void Update()
        {
            if (!host || host.Game?.District == null)
            {
                if (root != null)
                    root.style.display = DisplayStyle.None;
                return;
            }

            if (document == null)
                Build();
            if (bound != host.Game) Bind();
            if (open && (!host.BookOpen || host.BookSectionNow != "people")) HidePage();
            root.style.display = Available ? DisplayStyle.Flex : DisplayStyle.None;
            if (!Available) return;
            if (open) SizePage();
            if (walking >= 0 && !open && walker >= 0)
            {
                var op = bound.Ops[walker];
                var p = bound.DistrictPersonAt(walking);
                if (!p.HasValue || op.Down || op.LeftMap || op.InCar)
                {
                    walking = -1;
                }
                else if (!op.Inside && op.Busy == null && G.Within(op.Pos, p.Value, 490))
                {
                    int met = walking;
                    walking = -1;
                    Meet(met, walker);
                }
                else if (op.Walk == null)
                    walking = -1;
            }

            Marker();
        }

        void Bind()
        {
            bound = host.Game;
            open = false;
            tracking = walking = walker = -1;
            person = -1;
            LoadIndex();
            sheet.style.display = DisplayStyle.None;
        }

        public void ShowFromBook()
        {
            if (!Available) return;
            if (document == null) Build();
            if (bound != host.Game) Bind();
            if (open) return;
            open = true;
            person = -1;
            writingOrder = false;
            section = Section.Faces;
            reply = "";
            sheet.style.display = DisplayStyle.Flex;
            SizePage();
            Page();
        }

        public void Open(int resident = -1)
        {
            if (!Available) return;
            if (!host.BookOpen)
            {
                if (!host.BookOpenNow("people")) return;
            }
            else if (host.BookSectionNow != "people") host.BookShow("people");
            ShowFromBook();
            if (!open) return;
            if (resident >= 0 && person < 0) returnTo = section;
            person = resident;
            writingOrder = false;
            section = Section.Faces;
            reply = "";
            Page();
        }

        public void Close()
        {
            if (!open) return;
            host.BookClose();
            HidePage();
        }

        public bool BackToIndex()
        {
            if (!open || person < 0) return false;
            if (writingOrder)
            {
                if (openList.Length > 0) openList = "";
                else writingOrder = false;
                Page();
                return true;
            }
            string id = bound.ContactId(person);
            person = -1;
            section = returnTo;
            Page();
            pages.Q<Button>("contact-" + id)?.Focus();
            return true;
        }

        void Meet(int resident, int agent)
        {
            if (host.Game.DistrictPersonAt(resident) is not Pt at) return;
            host.StreetTalk(resident, (OpId)agent, host.View.WorldToScreenPoint(host.frame.World(at)));
        }

        internal void FollowVisit(int resident, OpId agent)
        {
            walking = tracking = resident;
            walker = (int)agent;
        }

        void HidePage()
        {
            open = false;
            sheet.style.display = DisplayStyle.None;
            pages?.Clear();
            search = "";
        }

        void Marker()
        {
            if (open || tracking < 0 || !host.View || !(bound.DistrictPersonAt(tracking)is Pt at))
            {
                marker.style.display = DisplayStyle.None;
                return;
            }

            var p = host.View.WorldToScreenPoint(host.frame.World(at) + Vector3.up * 2.3f);
            if (p.z < 0 || p.x < 15 || p.x > Screen.width - 30 || p.y < 30 || p.y > Screen.height - 45)
            {
                marker.style.display = DisplayStyle.None;
                return;
            }

            var ui = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(p.x, Screen.height - p.y));
            marker.style.left = ui.x - 22;
            marker.style.top = ui.y - 18;
            markerText.text = bound.District.Map.People[tracking].Name;
            marker.style.display = DisplayStyle.Flex;
        }

        internal int Speaker(Pt pos, bool nearby)
        {
            int who = -1;
            long best = long.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                var o = host.Game.Ops[i];
                if (!host.Selected[i] || o.Down || o.LeftMap || o.Inside || o.InCar || o.Busy != null)
                    continue;
                long distance = G.D2(pos, o.Pos);
                if (distance < best && (!nearby || G.Within(pos, o.Pos, 650)))
                {
                    who = i;
                    best = distance;
                }
            }

            return who;
        }

        void Page()
        {
            var g = host.Game;
            var d = g.District;
            PreparePage();
            if (person < 0) { IndexPage(); return; }
            if (g.IsCivicContact(person)) { CivicPage(); return; }
            var r = d.Map.People[person];
            var at = g.ContactAt(person);
            PersonHeader(r.Id, r.Name, r.Job, at.HasValue ? d.Map.StreetAt(at.Value) : "Off the street");
            if (writingOrder)
            {
                CitizenOrderPage();
                return;
            }
            var ties = d.Network.Neighbours(person);
            bool firstLink = true;
            foreach (var tie in ties)
            {
                if (!g.ContactKnown(tie.person)) continue;
                if (firstLink) { Caps(pages, "Known connections", Gold); firstLink = false; }
                LinkFace(pages, tie.person, tie.kind);
            }
            var job = d.Available(person);
            if (job != null)
            {
                Label(pages, d.Title(job), 19, Gold);
                Label(pages, d.Lead(job), 15, Ivory);
            }
            else
                Label(pages, "On their round between " + StreetPlace(r.Place) + " and " + StreetPlace(r.Errand) + ".", 15, Ivory);
            string memory = g.ContactUpdate(person);
            if (!string.IsNullOrEmpty(memory))
            {
                Caps(pages, "Latest word", Gold);
                Label(pages, memory, 15, Ivory);
            }
            if (!at.HasValue)
            {
                Label(pages, d.Network.Fallen[person] ? "This contact has died." : "Currently off the street.", 15, Blue);
                return;
            }

            int by = Speaker(at.Value, true);
            if (d.Network.Away(person))
            {
                Button(pages, "Locate on the street", () => Locate(person));
                return;
            }
            if (by < 0)
            {
                Button(pages, "Show on the street", () =>
                {
                    tracking = person;
                    Close();
                    host.LookAt(host.frame.World(at.Value));
                });
                int visit = Speaker(at.Value, false);
                if (visit >= 0)
                    Button(pages, "Walk to meet " + r.Name.Split(' ')[0], () =>
                    {
                        if (host.Issue(new Order { Op = Op.Name((OpId)visit), Do = "district-visit", Target = r.Id, Run = true }))
                        {
                            walking = tracking = person;
                            walker = visit;
                            Close();
                            host.CentreOn((OpId)visit);
                        }
                        else
                        {
                            reply = LastAnswer();
                            Page();
                        }
                    });
                else
                {
                    var op = g.Ops[(int)host.Agent];
                    if (op.InCar)
                    {
                        var road = g.DistrictRoadPoint(at.Value);
                        if (road.HasValue)
                            Button(pages, "Drive nearby", () =>
                            {
                                tracking = person;
                                if (host.Issue(new Order { Op = Op.Name(op.Id), Do = "drive-car", Point = road.Value }))
                                    Close();
                                else
                                {
                                    reply = LastAnswer();
                                    Page();
                                }
                            });
                        Button(pages, "Step out here", () =>
                        {
                            if (host.Issue(new Order { Op = Op.Name(op.Id), Do = "move", Point = g.DistrictExitDoor(op) }))
                                Close();
                        });
                    }
                    else
                        Label(pages, "Select an available operative to visit.", 15, Ivory);
                }
            }
            else
            {
                Button(pages, "Speak on the street", () =>
                {
                    int resident = person;
                    Close();
                    Meet(resident, by);
                });
                if (d.Trust[person] > 0 || g.Standing >= 4)
                    Button(pages, "Call in a favour  ›", OpenCitizenOrders);
            }

            if (reply.Length > 0 && reply != memory)
                Label(pages, reply, 15, Gold);
        }

        static string StreetPlace(string place)
        {
            int queue = place.LastIndexOf(" queue ", StringComparison.OrdinalIgnoreCase);
            return queue >= 0 && int.TryParse(place.Substring(queue + 7), out _) ? place.Substring(0, queue) : place;
        }

        string LastAnswer()
        {
            var orders = host.Game.Orders;
            return orders.Count > 0 ? orders[orders.Count - 1].why : "";
        }

    }
}
