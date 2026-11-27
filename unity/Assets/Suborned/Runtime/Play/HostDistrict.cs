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
        [System.Serializable] public sealed class ServiceRobotModel
        {
            public StreetRobot kind;
            public GameObject near, middle, far;

            public static string HarbourMesh(StreetRobot kind) => kind switch {
                StreetRobot.Porter => "Porter", StreetRobot.Shopkeeper => "Clerk",
                StreetRobot.Sweeper => "Sweeper", StreetRobot.CapsuleRunner => "Runner", _ => null
            };
        }
        [HideInInspector] public ServiceRobotModel[] districtServiceRobots = System.Array.Empty<ServiceRobotModel>();
        [HideInInspector] public DistrictCrowdFigure[] districtCrowd = System.Array.Empty<DistrictCrowdFigure>();
        // Historical mission fixtures have a closed ending. Normal play starts in the district.
        [System.NonSerialized] public bool missionScenario;
        internal float StreetFrameAlpha => Paused ? 1f : Mathf.Clamp01(acc * Suborned.Sim.Game.Hz);
        public DistrictJournal DistrictBook { get; private set; }
        internal DistrictStreet HarbourStreet { get; private set; }
        string clockStart;
        int startSeconds;
        internal int CitySeconds
        {
            get
            {
                if (clockStart != MissionStartsAt)
                {
                    var parts = MissionStartsAt.Split(':');
                    startSeconds = int.Parse(parts[0]) * 3600 + int.Parse(parts[1]) * 60 + (parts.Length > 2 ? int.Parse(parts[2]) : 0);
                    clockStart = MissionStartsAt;
                }
                int nightStart = Game?.District != null && Game.Night ? 4 * 3600 : 0;
                return (startSeconds + nightStart + (Game?.Tick ?? 0) / Suborned.Sim.Game.Hz) % 86400;
            }
        }
        public string CityClock()
        {
            if (Game == null) return "";
            int s = CitySeconds;
            return $"{s / 3600:00}:{s / 60 % 60:00}:{s % 60:00}";
        }

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
