using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    public sealed partial class HandoverHost
    {
        // Filled from the existing vehicle kit for the build and Editor play.
        [HideInInspector]
        public GameObject districtTram, districtTramMid, districtTramFar, districtRobotMid, districtRobotFar;
        [HideInInspector] public DistrictCrowdFigure[] districtCrowd = System.Array.Empty<DistrictCrowdFigure>();
        // Historical mission fixtures have a closed ending. Normal play starts in the district.
        [System.NonSerialized] public bool missionScenario;
        internal float StreetFrameAlpha => Paused ? 1f : Mathf.Clamp01(acc * Suborned.Sim.Game.Hz);
        public DistrictJournal DistrictBook { get; private set; }
        internal DistrictStreet HarbourStreet { get; private set; }

        void InstallDistrict()
        {
            DistrictBook = gameObject.AddComponent<DistrictJournal>();
            HarbourStreet = gameObject.AddComponent<DistrictStreet>();
        }

        void StreetDrivers(Pt centre, Vector2? screen, List<int> agents)
        {
            if (Game.District == null)
                return;
            for (int car = 0; car < 2; car++)
            {
                bool second = car == 1;
                int driver = agents.FindIndex(k => Game.Ops[k].InCar && Game.Ops[k].InLent == second);
                if (driver < 0)
                    continue;
                int k = agents[driver];
                void Send()
                {
                    if (Issue(new Order { Op = Op.Name((OpId)k), Do = "drive-car", Point = centre }, screen, frame.World(centre)))
                    {
                        Pulses.Add((frame.World(Game.DistrictRoadPoint(centre) ?? centre), Feedback.Ok, Time.unscaledTime));
                        while (Pulses.Count > 8)
                            Pulses.RemoveAt(0);
                    }
                }

                if (Queueing)
                    QueuePlan(k, "Drive", "s_moving", new List<Vector3> { frame.World(Game.Ops[k].Pos), frame.World(centre) }, Send);
                else
                    Send();
                agents.RemoveAll(n => Game.Ops[n].InCar && Game.Ops[n].InLent == second);
            }
        }
    }
}
