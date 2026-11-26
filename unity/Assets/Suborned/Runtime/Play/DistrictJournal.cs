using System;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    public sealed class DistrictJournal : MonoBehaviour
    {
        HandoverHost host;
        Game bound;
        UIDocument document;
        PanelSettings settings;
        VisualElement root, sheet, tab, marker;
        Label markerText;
        ScrollView pages;
        bool open, wasPaused;
        int person = -1, tracking = -1, walking = -1, walker = -1;
        string reply = "";
        static DistrictJournal live;
        static readonly Color Paper = new Color32(23, 37, 37, 250), Ivory = new Color32(244, 235, 209, 255), Gold = new Color32(223, 183, 108, 255), Blue = new Color32(154, 210, 207, 255);
        public bool IsOpen => open;
        public bool Ready => document != null && tab != null;

        bool Available => host && host.Game?.District != null && !host.Game.Over && host.Fault == null && !host.ShowBriefing && !host.MenuOpen && !host.BookOpen && !host.ConvoOn;

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
            if (live == null || !live.Available || live.root?.panel == null)
                return false;
            if (live.open)
                return true;
            var p = RuntimePanelUtils.ScreenToPanel(live.root.panel, new Vector2(screen.x, Screen.height - screen.y));
            return live.tab.worldBound.Contains(p);
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
            if (bound != host.Game)
            {
                bound = host.Game;
                open = false;
                person = tracking = walking = walker = -1;
                reply = "";
                sheet.style.display = DisplayStyle.None;
            }

            root.style.display = Available ? DisplayStyle.Flex : DisplayStyle.None;
            if (!Available)
                return;
            settings.scale = Mathf.Max(.75f, Mathf.Round(Screen.height / 1080f)) * HudToolkit.UserScale;
            tab.style.display = open ? DisplayStyle.None : DisplayStyle.Flex;
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
                    Open(met);
                }
                else if (op.Walk == null)
                    walking = -1;
            }

            Marker();
        }

        void Build()
        {
            var source = Resources.Load<PanelSettings>("Kit/HudPanel");
            settings = source ? Instantiate(source) : ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.sortingOrder = 55;
            var go = new GameObject("District book");
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            document = go.AddComponent<UIDocument>();
            document.panelSettings = settings;
            go.SetActive(true);
            root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.style.flexGrow = 1;
            tab = Button(root, "Harbour district   N", () => Open());
            tab.style.position = Position.Absolute;
            tab.style.right = 18;
            tab.style.top = 100;
            sheet = new VisualElement();
            root.Add(sheet);
            sheet.style.position = Position.Absolute;
            sheet.style.right = 18;
            sheet.style.top = 58;
            sheet.style.bottom = 70;
            sheet.style.width = 390;
            sheet.style.maxWidth = Length.Percent(92);
            sheet.style.backgroundColor = Paper;
            sheet.style.borderTopWidth = 3;
            sheet.style.borderTopColor = Gold;
            sheet.style.paddingLeft = 18;
            sheet.style.paddingRight = 18;
            sheet.style.paddingTop = 12;
            sheet.style.paddingBottom = 12;
            pages = new ScrollView();
            pages.style.flexGrow = 1;
            sheet.Add(pages);
            Button(sheet, "Close", Close);
            sheet.style.display = DisplayStyle.None;
            marker = new VisualElement();
            marker.pickingMode = PickingMode.Ignore;
            root.Add(marker);
            marker.style.position = Position.Absolute;
            marker.style.backgroundColor = Paper;
            marker.style.paddingLeft = 8;
            marker.style.paddingRight = 8;
            markerText = Label(marker, "", 13, Gold);
            markerText.pickingMode = PickingMode.Ignore;
            marker.style.display = DisplayStyle.None;
        }

        static Label Label(VisualElement parent, string words, int size, Color color)
        {
            var l = new Label(words);
            parent.Add(l);
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.color = color;
            l.style.fontSize = size;
            l.style.marginBottom = 6;
            return l;
        }

        static Button Button(VisualElement parent, string words, Action action)
        {
            var b = new Button(action)
            {
                text = words
            };
            parent.Add(b);
            b.style.color = Ivory;
            b.style.backgroundColor = (Color)new Color32(47, 68, 65, 255);
            b.style.whiteSpace = WhiteSpace.Normal;
            b.style.unityTextAlign = TextAnchor.MiddleLeft;
            b.style.fontSize = 15;
            b.style.minHeight = 32;
            b.style.marginTop = 3;
            b.style.paddingLeft = 10;
            return b;
        }

        public void Open(int resident = -1)
        {
            if (!Available)
                return;
            if (document == null)
                Build();
            if (!open)
            {
                wasPaused = host.Paused;
                host.SetPaused(true);
            }

            open = true;
            person = resident;
            reply = "";
            sheet.style.display = DisplayStyle.Flex;
            tab.style.display = DisplayStyle.None;
            Page();
        }

        public void Close()
        {
            if (!open)
                return;
            open = false;
            sheet.style.display = DisplayStyle.None;
            tab.style.display = DisplayStyle.Flex;
            if (host.Game != null && !host.Game.Over)
                host.SetPaused(wasPaused);
        }

        public bool PickResident(Vector2 screen, string civic)
        {
            if (!Available || open || !host.View)
                return false;
            var g = host.Game;
            var d = g.District;
            if (!string.IsNullOrEmpty(civic) && d.Map.Index.TryGetValue(civic, out int known) && d.Roams(known))
            {
                Open(known);
                return true;
            }

            int closest = -1;
            float best = Mathf.Pow(15 * Mathf.Max(1f, Screen.height / 1080f), 2);
            for (int i = 0; i < d.Map.People.Length; i++)
            {
                if (!d.Roams(i) || !(g.DistrictPersonAt(i)is Pt at))
                    continue;
                var p = host.View.WorldToScreenPoint(host.frame.World(at) + Vector3.up);
                if (p.z <= 0 || p.z > 100)
                    continue;
                float dist = ((Vector2)p - screen).sqrMagnitude;
                if (dist < best)
                {
                    closest = i;
                    best = dist;
                }
            }

            if (closest < 0)
                return false;
            Open(closest);
            return true;
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

        int Speaker(Pt pos, bool nearby)
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
            pages.Clear();
            pages.scrollOffset = Vector2.zero;
            if (person < 0)
            {
                Label(pages, "Harbour district", 27, Ivory);
                Label(pages, "The Handover", 19, Gold);
                Label(pages, d.HandoverNews, 15, Ivory);
                if (g.HandoverOpen)
                    Button(pages, "Locate the Records Office", () =>
                    {
                        Close();
                        host.LookAt(host.frame.World(g.S.Stand(g.S.GuardPost)));
                    });
                Label(pages, "Word on the street", 19, Gold);
                foreach (var j in d.Jobs)
                {
                    var target = j.Contact;
                    Button(pages, d.Title(j) + "  ›", () => Open(target));
                    Label(pages, d.Lead(j), 14, Ivory);
                }

                if (d.News.Length > 0)
                {
                    Label(pages, "Latest word", 17, Gold);
                    Label(pages, d.News, 14, Ivory);
                }

                var op = g.Ops[(int)host.Agent];
                if (op.InCar)
                {
                    Label(pages, "Your car", 18, Gold);
                    Label(pages, "Click a street to drive. Click again to change course.", 14, Ivory);
                    Button(pages, "Step out here", () =>
                    {
                        if (host.Issue(new Order { Op = Op.Name(op.Id), Do = "move", Point = g.DistrictExitDoor(op) }))
                            Close();
                    });
                }
                else
                    Label(pages, "Click a resident to talk. When aboard a car, click a street to drive.", 14, Blue);
                return;
            }

            var r = d.Map.People[person];
            var at = g.DistrictPersonAt(person);
            Button(pages, "‹  District book", () => Open());
            Label(pages, r.Name, 25, Ivory);
            Label(pages, r.Job.Replace('-', ' ') + "  ·  " + (at.HasValue ? d.Map.StreetAt(at.Value) : "off the street"), 14, Blue);
            var job = d.Available(person);
            if (job != null)
            {
                Label(pages, d.Title(job), 19, Gold);
                Label(pages, d.Lead(job), 15, Ivory);
            }
            else
                Label(pages, "On their round between " + r.Place + " and " + r.Errand + ".", 15, Ivory);
            if (!at.HasValue)
            {
                Label(pages, "The other district leads are still open.", 15, Ivory);
                return;
            }

            int by = Speaker(at.Value, true);
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
                if (job != null)
                {
                    Label(pages, "“" + d.Speaker(job) + "”", 17, Ivory);
                    Button(pages, d.Choice(job), () => Speak(by, "district-help", job.Key));
                    if (job.Stage == 1)
                        Button(pages, d.Choice(job, true), () => Speak(by, "district-bluff", job.Key));
                }

                Label(pages, "Personal favours", 17, Gold);
                if (d.Trust[person] <= 0)
                    Button(pages, "Help with " + r.Need + "  ·  $15", () => Speak(by, "district-back", ""));
                foreach (var tie in r.Ties)
                {
                    var friend = d.Map.People[tie.person];
                    if (!d.Roams(tie.person))
                        continue;
                    if (d.Trust[person] > 0 && d.Trust[tie.person] <= 0)
                        Button(pages, "Ask for an introduction to " + friend.Name, () => Speak(by, "district-refer", friend.Id));
                    else
                        Button(pages, friend.Name + "  ·  " + tie.kind, () => Open(tie.person));
                }
            }

            string memory = d.Memory[person];
            if (!string.IsNullOrEmpty(memory))
                Label(pages, memory, 14, Blue);
            if (reply.Length > 0 && reply != memory)
                Label(pages, reply, 15, Gold);
        }

        string LastAnswer()
        {
            var orders = host.Game.Orders;
            return orders.Count > 0 ? orders[orders.Count - 1].why : "";
        }

        void Speak(int by, string verb, string to)
        {
            var d = host.Game.District;
            string id = d.Map.People[person].Id;
            bool ok = host.Issue(new Order { Op = Op.Name((OpId)by), Do = verb, Target = id, To = to });
            reply = ok ? d.News : LastAnswer();
            if (ok)
            {
                tracking = -1;
                walking = -1;
            }

            Page();
        }
    }
}
