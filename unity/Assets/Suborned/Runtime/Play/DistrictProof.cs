using System;
using System.Collections;
using System.IO;
using System.Linq;
using Suborned.Sim;
using UnityEngine;
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
            Need(journal.Ready && street.TramKit, "Journal or vehicle LOD kit is missing");
            journal.Open();
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
            journal.Close();
            var rail = DistrictMap.Shuttle(d.Map.Trams[0].Path, g.Tick, 1200, 730, 95);
            h.LookAt(h.frame.World(rail.at));
            yield return new WaitForSecondsRealtime(.6f);
            Need(street.VisibleVehicles > 0, "No transit visible at the tram position");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(IOPath.Combine(directory, "tram-street.png"));
            yield return new WaitForSecondsRealtime(.4f);
            File.WriteAllText(IOPath.Combine(directory, "proof.txt"), $"OldTown opened. The district book and contact page opened. A ground click drove the boarded scout to {d.Map.StreetAt(point)}. The Handover closed and play continued at tick {g.Tick}. {source.Name} vouched for the cell with {d.Map.People[j.B].Name}. Residents {d.Map.People.Length}, open leads {d.Jobs.Length}, visible vehicles at the rail {street.VisibleVehicles}.");
        }
    }
}
