using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    // Rigid hinges authored in the FBX. All phases come from the machine's own scheduled round.
    internal sealed class DistrictServiceMotion
    {
        enum Part { Links, Deck, Wheel, NoseWheel, Head, Upper, Fore, Hand, Bowl, BrushLeft, BrushRight, Flag, Hatch }

        readonly struct Joint
        {
            public readonly Part part;
            readonly Transform node;
            readonly Quaternion rest, axes, inverseAxes;
            readonly Vector3 position, up, forward;

            public Joint(Part kind, Transform transform, Transform vehicle)
            {
                part = kind;
                node = transform;
                rest = node.localRotation;
                position = node.localPosition;
                axes = Quaternion.Inverse(node.parent.rotation) * vehicle.rotation;
                inverseAxes = Quaternion.Inverse(axes);
                up = node.parent.InverseTransformVector(vehicle.up);
                forward = node.parent.InverseTransformVector(vehicle.forward);
            }

            // The same world-space hinge works for metre-scaled kits and FBX roots with converted axes.
            public void Rotate(Quaternion q) => node.localRotation = axes * q * inverseAxes * rest;
            public void Translate(Vector3 offset) => node.localPosition = position + up * offset.y + forward * offset.z;
        }

        readonly List<Joint> joints = new();
        static readonly Vector3 LiftBar = new Vector3(0, -.142f, .595f) * 1.05f;
        readonly Suborned.Robots.RobotFace display;
        readonly StreetRobot kind;
        readonly float tyreRadius, casterRadius;
        int sampledTick = -1, lastId = -1;
        (int lift, long wheelMm) previous, next;
        public void ResetRound() => lastId = -1;

        public DistrictServiceMotion(Transform root, StreetRobot kind, bool papers, Suborned.Robots.RobotFace display)
        {
            this.kind = kind;
            this.display = display;
            tyreRadius = kind switch { StreetRobot.Porter => .335f * 1.05f, StreetRobot.Sweeper => .119f,
                StreetRobot.CapsuleRunner => .150f, _ => .081f * 1.1f };
            casterRadius = kind == StreetRobot.CapsuleRunner ? .067f : .103f * 1.05f;
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                switch (node.name)
                {
                    case "Porter_LinkUpper": case "Porter_LinkLower": Add(Part.Links, node); break;
                    case "Porter_Deck": Add(Part.Deck, node); break;
                    case "Porter_WheelR": case "Porter_WheelL": case "Clerk_Caster0": case "Clerk_Caster1": case "Clerk_Caster2":
                    case "Sweeper_WheelL": case "Sweeper_WheelR": case "Runner_WheelL": case "Runner_WheelR":
                        Add(Part.Wheel, node); break;
                    case "Porter_NoseWheel": case "Runner_Caster": Add(Part.NoseWheel, node); break;
                    case "Porter_Head": case "Clerk_Head": case "Runner_Head": Add(Part.Head, node); break;
                    case "Sweeper_BrushL": Add(Part.BrushLeft, node); break;
                    case "Sweeper_BrushR": Add(Part.BrushRight, node); break;
                    case "Runner_Flag": Add(Part.Flag, node); break;
                    case "Runner_Hatch": Add(Part.Hatch, node); break;
                    case "Clerk_PickUpper": Add(Part.Upper, node); break;
                    case "Clerk_PickFore": Add(Part.Fore, node); break;
                    case "Clerk_PickHand": Add(Part.Hand, node); break;
                    case "Clerk_BowlFore": Add(Part.Bowl, node); break;
                    case "Clerk_Papers": node.gameObject.SetActive(papers); break;
                    case "Clerk_Stem": case "Clerk_Flowers": node.gameObject.SetActive(!papers); break;
                }
            }
            void Add(Part part, Transform node) => joints.Add(new Joint(part, node, root));
        }

        public void Apply(DistrictTraffic routes, int id, int tick, float fraction, bool moving, bool detailed)
        {
            if (lastId != id || sampledTick != tick)
            {
                previous = lastId == id && sampledTick == tick - 1 ? next : routes.ServiceMechanism(id, Mathf.Max(0, tick - 1));
                next = routes.ServiceMechanism(id, tick);
                lastId = id;
                sampledTick = tick;
            }
            float stroke = Mathf.SmoothStep(0, 1, Mathf.Lerp(previous.lift, next.lift, fraction) / 1000f);
            float seconds = Mathf.Max(0, tick - 1 + fraction) / Game.Hz;
            int gaze = moving || stroke < .25f ? 0 : kind == StreetRobot.CapsuleRunner ? 2 : -2;
            if (display) display.PoseAt(seconds, gaze);
            if (!detailed) return;
            double travel = (previous.wheelMm + (next.wheelMm - previous.wheelMm) * (double)fraction) * .001;
            Quaternion Spin(float radius) => Quaternion.AngleAxis((float)((travel / radius * Mathf.Rad2Deg) % 360), Vector3.right);
            var wheel = Spin(tyreRadius);
            var nose = Spin(casterRadius);
            var links = Quaternion.AngleAxis(-21 * stroke, Vector3.right);
            float brush = ((tick % Game.Hz + fraction) * 360f / Game.Hz + id * 37) % 360f;
            foreach (var joint in joints)
            {
                switch (joint.part)
                {
                    case Part.Wheel: joint.Rotate(wheel); break;
                    case Part.NoseWheel: joint.Rotate(nose); break;
                    case Part.Links: joint.Rotate(links); break;
                    case Part.Deck: joint.Translate(links * LiftBar - LiftBar); break;
                    case Part.Head: joint.Rotate(kind == StreetRobot.Shopkeeper ? Quaternion.Euler(4 * (1 - stroke), 9 - 16 * stroke, 0) : Quaternion.Euler(0, 9 * stroke, 0)); break;
                    case Part.BrushLeft: joint.Rotate(Quaternion.AngleAxis(-brush, Vector3.up)); break;
                    case Part.BrushRight: joint.Rotate(Quaternion.AngleAxis(brush, Vector3.up)); break;
                    case Part.Flag: joint.Rotate(Quaternion.AngleAxis(-59 * (1 - stroke), Vector3.right)); break;
                    case Part.Hatch: joint.Rotate(Quaternion.AngleAxis(-40 * stroke, Vector3.right)); break;
                    case Part.Upper: joint.Rotate(Quaternion.Euler(-14 * stroke, 0, -9 * stroke)); break;
                    case Part.Fore: joint.Rotate(Quaternion.Euler(-23 * stroke, 0, 5 * stroke)); break;
                    case Part.Hand: joint.Rotate(Quaternion.Euler(18 * stroke, 13 * stroke, 0)); break;
                    case Part.Bowl: joint.Rotate(Quaternion.AngleAxis(-4 * stroke, Vector3.right)); break;
                }
            }
        }
    }
}
