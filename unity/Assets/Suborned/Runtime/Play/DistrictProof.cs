using System;
using System.Collections;
using System.IO;
using System.Linq;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.UIElements;
using Directory = System.IO.Directory;
using IOPath = System.IO.Path;

namespace Suborned.Play
{
    public sealed class DistrictProof : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            string dir = GameBoot.ArgValue("-districtProof");
            if (string.IsNullOrEmpty(dir))
                return;
            var go = new GameObject("Harbour proof");
            DontDestroyOnLoad(go);
            go.AddComponent<DistrictProof>().directory = dir;
        }

        string directory;
        IEnumerator Start()
        {
            Directory.CreateDirectory(directory);
            var steps = Play();
            while (true)
            {
                bool more;
                try
                {
                    more = steps.MoveNext();
                }
                catch (Exception e)
                {
                    File.WriteAllText(IOPath.Combine(directory, "proof.txt"), e.ToString());
                    Debug.LogException(e);
                    GameBoot.Leave(1);
                    yield break;
                }

                if (!more)
                    break;
                yield return steps.Current;
            }

            GameBoot.Leave(0);
        }

        static void Need(bool ok, string why)
        {
            if (!ok)
                throw new InvalidOperationException(why);
        }

        static void Submit(VisualElement panel, string words)
        {
            var button = panel.Query<Button>().ToList().FirstOrDefault(b => (b.text ?? "").StartsWith(words, StringComparison.Ordinal));
            Need(button != null, "Missing button " + words);
            Submit(button);
        }

        static void Submit(Button button)
        {
            Need(button != null, "Missing contact inventory control");
            using var submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }

        IEnumerator Play()
        {
            HandoverHost h = null;
            float limit = Time.realtimeSinceStartup + 120;
            while (!h || h.Game == null || !h.View)
            {
                h = FindFirstObjectByType<HandoverHost>();
                Need(Time.realtimeSinceStartup < limit, "OldTown did not load");
                yield return null;
            }

            var g = h.Game;
            var d = g.District;
            Need(d != null, "Play did not open the district");
            h.StartClock();
            h.SetPaused(true);
            yield return null;
            var journal = h.DistrictBook;
            var street = h.GetComponent<DistrictStreet>();
            Need(journal.Ready && journal.PortraitsReady && street.TramKit, "Portrait inventory or vehicle LOD kit is missing");
            Need(h.BookOpenNow("cell"), "The book did not open from play");
            h.BookShow("people");
            Need(journal.IsOpen, "District book did not open");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(IOPath.Combine(directory, "district-book.png"));
            yield return null;
            journal.Close();
            var scout = g.Ops[0];
            h.Select(OpId.Scout, false);
            Need(h.Issue(new Order { Op = "scout", Do = "move", Point = g.Walk.NearestFree(g.GetawaySeat) }), "Scout could not reach the car");
            for (int k = 0; k < 5000 && scout.Walk != null; k++)
                g.Step();
            Need(h.Issue(new Order { Op = "scout", Do = "board", Target = "car" }), "Scout could not board");
            g.RunTo(scout.BusyEnd);
            Need(scout.InCar, "Boarding failed");
            var road = d.Map.Streets.First(s => s.Id == "road/market-street").Path;
            var point = road.At(road.Length / 3);
            h.LookAt(h.frame.World(point));
            yield return null;
            var screen = (Vector2)h.View.WorldToScreenPoint(h.frame.World(point));
            h.SetPaused(false);
            h.LeftClickScripted(screen);
            h.SetPaused(true);
            var last=g.Orders[g.Orders.Count-1];
            Need(g.GetawayDriving, $"Ground click did not start a drive. Last order {last.order.Do} {last.code} {last.why}, picked {h.TargetAt(screen,true)?.Kind}, camera {screen}.");
            g.RunTo(scout.BusyEnd);
            Need(G.Within(point, g.GetawayCarPos(), 600), "Car missed the requested road");
            g.RunTo(Game.T(g.S.Over + 120));
            Need(!g.Over && !g.HandoverOpen, $"Handover at tick {g.Tick}: {g.Result} {g.FailCode} {g.FailText}; {d.Handover}");
            var j = d.Jobs[0];
            Need(j.A != j.B && j.B != j.C, "Invalid contact chain");
            journal.Open(j.A);
            Need(journal.IsOpen, "Contact page did not open");
            journal.Close();
            Need(h.Issue(new Order { Op = "scout", Do = "move", Point = g.DistrictExitDoor(scout) }), "Could not leave the car");
            for (int k = 0; k < 300 && (scout.Busy != null || scout.Walk != null); k++)
                g.Step();
            var source = d.Map.People[j.A];
            Need(h.Issue(new Order { Op = "scout", Do = "district-visit", Target = source.Id }), "Could not walk to the first contact");
            for (int k = 0; k < 20000 && scout.Walk != null; k++)
                g.Step();
            h.LookAt(h.frame.World(scout.Pos));
            yield return new WaitForSecondsRealtime(.5f);
            journal.Open(j.A);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(IOPath.Combine(directory, "street-dialogue.png"));
            Need(h.Issue(new Order { Op = "scout", Do = "district-help", Target = source.Id, To = j.Key }), "Native contact order refused");
            Need(j.Stage == 1, "The first contact did not vouch for the cell");
            var panel = h.transform.Find("District book").GetComponent<UIDocument>().rootVisualElement;
            journal.Open();
            Submit(panel.Q<Button>("contact-" + source.Id));
            yield return new WaitForSecondsRealtime(.15f);
            Need(!journal.IsUnread(j.A), "The first read did not clear the contact");
            Need(h.Issue(new Order { Op = "scout", Do = "district-back", Target = source.Id }), "Could not earn the first contact's trust");
            Need(journal.IsUnread(j.A), "New information did not mark a read contact again");
            journal.Open(j.A);
            Submit(panel, "Call in a favour");
            yield return new WaitForSecondsRealtime(.2f);
            var routes = g.CitizenRoutes(j.A);
            int choice = routes.FindIndex(chain => chain.Length >= 3 && d.Roams(chain[chain.Length - 1]));
            Need(choice >= 0, "The source lacks a second-hand introduction");
            Submit(panel, "Here");
            yield return new WaitForSecondsRealtime(.15f);
            Submit(panel, "Market street");
            yield return new WaitForSecondsRealtime(.15f);
            Submit(panel, "Ask them directly");
            yield return new WaitForSecondsRealtime(.15f);
            var selected = routes[choice];
            Submit(panel, d.Map.People[selected[selected.Length - 1]].Name);
            yield return new WaitForSecondsRealtime(.15f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(IOPath.Combine(directory, "citizen-order.png"));
            Submit(panel, "Pass the word");
            yield return new WaitForSecondsRealtime(.2f);
            Need(d.Network.Requests.Count == 1, "Submitting the citizen form did not dispatch the request");
            var request = d.Network.Requests[0];
            journal.Close();
            for (int k = 0; k < 20000 && request.Stage != CitizenStage.Acting && !g.Over; k++) g.Step();
            Need(request.Stage == CitizenStage.Acting && d.Network.HoldsRoad(request.Site), "The citizen did not block the street");
            Need(!d.Network.KnowsCell[request.Actor] && d.Network.AskedBy[request.Actor] == d.Map.People[request.Chain[request.Chain.Length - 2]].Name, "The cell's identity leaked along the chain");
            h.LookAt(h.frame.World(request.Site));
            yield return new WaitForSecondsRealtime(.5f);
            journal.Open();
            Need(g.ContactKnown(j.A) && journal.IsUnread(j.A), "A new street contact was missing its ribbon");
            Need(g.ContactKnown(request.Actor), "The citizen working through a friend was missing from contacts");
            var tiles = panel.Q("contact-grid");
            int known = Enumerable.Range(0, g.ContactCount).Count(g.ContactKnown);
            Need(tiles != null && tiles.childCount == known && known > 1 && known < g.ContactCount, "The roster did not match the contacts known to the cell");
            Need(tiles.Q("portrait-" + source.Id) != null && tiles.Q("new-" + source.Id) != null, "The source's painted portrait or New ribbon was missing");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(IOPath.Combine(directory, "contact-roster.png"));
            yield return null;
            var search = panel.Q<TextField>("contact-search");
            search.value = source.Name;
            Need(panel.Q("contact-grid").childCount == 1, "Searching a contact failed");
            search.value = "";
            var runHash = g.ClosingHash();
            Submit(panel.Q<Button>("contact-" + source.Id));
            yield return new WaitForSecondsRealtime(.15f);
            Need(!journal.IsUnread(j.A) && journal.IsUnread(request.Actor), "Reading one portrait changed another contact's ribbon");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(IOPath.Combine(directory, "contact-dossier.png"));
            yield return null;
            Submit(panel.Q<Button>("close-contacts"));
            yield return new WaitForSecondsRealtime(.15f);
            Need(journal.IsOpen && panel.Q("contact-grid") != null && panel.Q("new-" + source.Id) == null && panel.Q("new-" + g.ContactId(request.Actor)) != null, "Returning from a dossier lost the roster or its read state");
            Need(g.ClosingHash() == runHash, "Reading a contact changed the simulation");
            Submit(panel.Q<Button>("contacts-nav-requests"));
            yield return new WaitForSecondsRealtime(.15f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(IOPath.Combine(directory, "street-order.png"));
            journal.Close();
            var rail = DistrictMap.Shuttle(d.Map.Trams[0].Path, g.Tick, 1200, 730, 95);
            h.LookAt(h.frame.World(rail.at));
            yield return new WaitForSecondsRealtime(.6f);
            int vehicles = street.VisibleVehicles;
            Need(vehicles > 0, "No transit visible at the tram position");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(IOPath.Combine(directory, "tram-street.png"));
            yield return new WaitForSecondsRealtime(.4f);
            string save = h.SaveSessionTo(IOPath.Combine(directory, "played.script.json"));
            h.ContinueFrom(save);
            yield return null;
            journal.Open();
            Need(journal.IsOpen && h.Game.ClosingHash() == runHash, "Continue failed to rebuild the played run");
            Need(!journal.IsUnread(j.A) && journal.IsUnread(request.Actor) && panel.Q("new-" + source.Id) == null, "Continue lost the portraits' read state");
            File.WriteAllText(IOPath.Combine(directory, "proof.txt"), $"OldTown opened. Book → People displayed {known} discovered contacts with their painted portraits. Searching by name returned one person. Reading {source.Name} cleared only that New ribbon. Earning trust marked the portrait again. Closing the dossier returned to the grid, and a saved-game Continue preserved the read state. A ground click drove the boarded scout to {d.Map.StreetAt(point)}. The Handover closed and play continued at tick {g.Tick}. {source.Name} vouched for the cell with {d.Map.People[j.B].Name}. The district book sent a request through {d.Network.Describe(request.Chain)}. The last citizen blocked {request.Place} knowing only who called them. District residents {d.Map.People.Length}, main cast {g.People.Count}, open leads {d.Jobs.Length}, visible vehicles at the rail {vehicles}.");
        }
    }
}
