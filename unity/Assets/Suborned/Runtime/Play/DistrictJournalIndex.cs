using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Suborned.Play
{
    public sealed partial class DistrictJournal
    {
        string search = "";
        TextField searchBox;
        public bool Searching => searchBox != null && searchBox.panel?.focusController.focusedElement is VisualElement e && (e == searchBox || searchBox.Contains(e));

        void IndexPage()
        {
            if (section == Section.Faces)
            {
                Heading("City contacts");
                MakeSearch();
                DrawFaces();
                return;
            }

            pages.style.backgroundColor = Paper;
            if (section == Section.Leads)
            {
                Heading("District leads");
                ShowLeads();
            }
            else
            {
                Heading("Requests");
                var d = bound.District;
                if (d.Network.Requests.Count == 0)
                {
                    Label(pages, "No requests in progress", 22, Ivory);
                    Label(pages, "Someone who trusts the cell may be willing to act, or to ask a friend on your behalf.", 16, Blue);
                }
                else NetworkOverview();
                Caps(foot, d.Network.Requests.Count + (d.Network.Requests.Count == 1 ? " active request" : " active requests"), Brass);
            }
        }

        void MakeSearch()
        {
            var wrap = new VisualElement();
            bar.Insert(bar.childCount - 1, wrap);
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.alignItems = Align.Center;
            wrap.style.marginRight = 20;
            wrap.style.borderBottomWidth = 1;
            wrap.style.borderBottomColor = Trim;
            var magnifier = Box(wrap);
            magnifier.style.width = magnifier.style.height = 12;
            magnifier.style.marginRight = 7;
            Border(magnifier, 1.5f, Cream);
            magnifier.style.borderTopLeftRadius = magnifier.style.borderTopRightRadius =
                magnifier.style.borderBottomLeftRadius = magnifier.style.borderBottomRightRadius = 8;
            var handle = Box(magnifier);
            handle.style.position = Position.Absolute;
            handle.style.left = 8; handle.style.top = 9; handle.style.height = 1.5f; handle.style.width = 6;
            handle.style.rotate = new Rotate(new Angle(45)); handle.style.backgroundColor = Cream;
            searchBox = new TextField { name = "contact-search", value = search, tooltip = "Search contacts by name or occupation" };
            searchBox.textEdition.placeholder = "Find a contact";
            searchBox.style.width = 146;
            wrap.Add(searchBox);
            var input = searchBox.Q(className: "unity-base-text-field__input");
            if (input != null)
            {
                input.style.backgroundColor = Color.clear; Border(input, 0, Trim);
                input.style.color = Cream; input.style.fontSize = 16;
                input.style.unityFontDefinition = FontDefinition.FromFont(bodyFont);
            }
            searchBox.RegisterValueChangedCallback(e => { search = e.newValue; DrawFaces(); });
        }

        void DrawFaces()
        {
            pages.Clear();
            foot.Clear();
            var roster = new List<int>();
            int known = 0, unread = 0;
            var query = search.Trim();
            for (int i = 0; i < bound.ContactCount; i++)
            {
                if (!bound.ContactKnown(i)) continue;
                known++;
                if (IsUnread(i)) unread++;
                if (query.Length == 0 || bound.ContactName(i).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    || bound.ContactJob(i).Replace('-', ' ').IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) roster.Add(i);
            }
            roster.Sort((a, b) =>
            {
                int fresh = IsUnread(b).CompareTo(IsUnread(a));
                if (fresh != 0) return fresh;
                int names = StringComparer.OrdinalIgnoreCase.Compare(bound.ContactName(a), bound.ContactName(b));
                return names != 0 ? names : StringComparer.Ordinal.Compare(bound.ContactId(a), bound.ContactId(b));
            });
            var grid = Box(pages, "contact-grid");
            grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap;
            foreach (int i in roster) FaceTile(grid, i);
            if (roster.Count == 0)
            {
                var quiet = Box(pages);
                All(quiet, 22);
                Text(quiet, known == 0 ? "Who will you meet first?" : "No matching contacts", 32, Cream, serifFont);
                var line = Text(quiet, known == 0 ? "Meet someone on the street, or follow a district lead." : "Try another name or occupation.", 18, C(199, 210, 193));
                line.style.marginTop = 8;
                if (known == 0)
                {
                    var b = Button(quiet, "Explore district leads", () => { section = Section.Leads; Page(); });
                    b.style.alignSelf = Align.FlexStart; b.style.marginTop = 18;
                }
            }
            Caps(foot, known + (known == 1 ? " contact" : " contacts") + (unread > 0 ? "   ·   " + unread + " new" : ""), Brass);
            var rule = Box(foot); rule.style.height = 1; rule.style.flexGrow = 1;
            rule.style.marginLeft = rule.style.marginRight = 18; rule.style.backgroundColor = C(65, 87, 75);
        }

        void FaceTile(VisualElement grid, int i)
        {
            var id = bound.ContactId(i);
            var b = new Button(() => ReadContact(i)) { name = "contact-" + id, tooltip = bound.ContactJob(i).Replace('-', ' ') };
            grid.Add(b);
            const int size = 132;
            All(b, 3); Border(b, 1, Trim); b.style.backgroundColor = C(48, 68, 58);
            b.style.width = size + 8; b.style.flexShrink = 0; b.style.alignItems = Align.Stretch;
            b.style.marginLeft = 0; b.style.marginRight = 10; b.style.marginTop = 0; b.style.marginBottom = 12;
            Portrait(b, id, size);
            var name = Text(b, bound.ContactName(i), 16, Cream);
            name.style.minHeight = 29; name.style.paddingLeft = 3; name.style.paddingTop = 4; name.style.paddingBottom = 3;
            name.style.unityTextAlign = TextAnchor.MiddleLeft;
            if (IsUnread(i))
            {
                var ribbon = Caps(b, "New", C(54, 41, 22), 11);
                ribbon.name = "new-" + id;
                ribbon.style.position = Position.Absolute; ribbon.style.left = -2; ribbon.style.top = 8;
                ribbon.style.paddingLeft = 7; ribbon.style.paddingRight = 8; ribbon.style.backgroundColor = Brass;
                ribbon.style.height = 20; ribbon.style.unityTextAlign = TextAnchor.MiddleCenter;
            }
            void Highlight(bool lit)
            {
                Border(b, 1, lit ? Brass : Trim);
                b.style.backgroundColor = lit ? C(68, 89, 70) : C(48, 68, 58);
            }
            b.RegisterCallback<PointerEnterEvent>(_ => Highlight(true)); b.RegisterCallback<PointerLeaveEvent>(_ => Highlight(false));
            b.RegisterCallback<FocusInEvent>(_ => Highlight(true)); b.RegisterCallback<FocusOutEvent>(_ => Highlight(false));
        }

        void ReadContact(int i)
        {
            RememberRead(i);
            Open(i);
        }

        void ShowLeads()
        {
            var d = bound.District;
            Label(pages, "The Handover", 22, Ivory);
            Label(pages, bound.HandoverOpen ? "The councillor is due at the Records Office." : d.HandoverNews, 16, Blue);
            Button(pages, "Open the Handover file", () => { HidePage(); host.BookShow("job"); });
            foreach (var job in d.Jobs)
            {
                Label(pages, d.Title(job), 20, Gold);
                Label(pages, d.Lead(job), 16, Ivory);
                Button(pages, bound.ContactName(job.Contact), () => Open(job.Contact));
            }
            if (!string.IsNullOrEmpty(d.News)) Label(pages, d.News, 15, Blue);
            var op = bound.Ops[(int)host.Agent];
            if (op.InCar) Button(pages, "Step out here", () =>
            {
                if (host.Issue(new Order { Op = Op.Name(op.Id), Do = "move", Point = bound.DistrictExitDoor(op) })) Close();
            });
            Caps(foot, d.Jobs.Length + " district leads", Brass);
        }

        void CivicPage()
        {
            var p = bound.CivicContact(person);
            var at = bound.ContactAt(person);
            PersonHeader(p.Id, p.Name, p.Job, at.HasValue ? bound.District.Map.StreetAt(at.Value) : "Off the street");
            Label(pages, "The Handover", 21, Gold);
            if (p.KNeed) Label(pages, "Needs help with " + p.Need.Replace('-', ' ') + ".", 17, Ivory);
            if (p.KTemperament) Label(pages, "Temperament  ·  " + p.Temperament, 16, Ivory);
            if (p.KRoutine) Label(pages, "Usual post  ·  " + p.Place.Replace('-', ' '), 16, Ivory);
            if (p.Turned) Label(pages, "They have agreed to help the cell.", 17, Gold);
            if (p.KSecret && p.Lover != null)
            {
                Label(pages, "Personal connection", 16, Blue);
                int other = bound.People.IndexOf(p.Lover);
                if (other >= 0) LinkFace(pages, bound.District.Map.People.Length + other, "Partner");
            }
            if (bound.CallOffered(p.Role))
            {
                var answer = bound.CallQuery(p.Role);
                if (answer.ok)
                {
                    Caps(pages, "Earned favour", Gold);
                    string name = p.Name.Split(' ')[0];
                    string action = p == bound.Driver ? "Call for pickup" : p == bound.Inez && p.KSecret && p.Lover != null ?
                        "Ask " + name + " to call " + p.Lover.Name.Split(' ')[0] : "Call " + name;
                    Button(pages, action + "  ·  +" + bound.R.CallTrail + " Trail", () =>
                    {
                        Close();
                        host.Issue(new Order { Op = "fixer", Do = "call", Target = p.Role });
                    });
                }
                else Label(pages, "Call unavailable  ·  " + answer.why, 15, Blue);
            }
            if (!p.KNeed && !p.KRoutine && !p.Turned && !p.KSecret)
                Label(pages, "There is more to learn from watching their round or striking up a conversation.", 17, Ivory);
            if (p.Down) Label(pages, "Incapacitated", 17, Gold);
            else if (at.HasValue)
                Button(pages, "Find " + p.Name.Split(' ')[0] + " on the street", () =>
                {
                    var target = new HandoverHost.Target { Kind = "person", Key = p.Role, Id = p.Role, Label = p.Name };
                    host.BookTakeWay(new BookWay { Target = target });
                    HidePage();
                });
        }

        void LinkFace(VisualElement parent, int i, string tie)
        {
            var b = new Button(() => ReadContact(i));
            parent.Add(b); All(b, 3); Border(b, 1, C(168, 172, 148)); b.style.backgroundColor = C(251, 245, 224);
            b.style.flexDirection = FlexDirection.Row; b.style.alignItems = Align.Center;
            b.style.alignSelf = Align.FlexStart; b.style.marginBottom = 4;
            Portrait(b, bound.ContactId(i), 32);
            var caption = Text(b, bound.ContactName(i) + "  ·  " + tie, 15, Ivory);
            caption.style.marginLeft = caption.style.marginRight = 8;
        }
    }
}
