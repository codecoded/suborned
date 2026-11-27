using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    // Rigid hinges authored in the FBX. All phases come from the machine's own scheduled round.
    internal sealed class DistrictServiceMotion
    {
        enum Part { Links, Deck, Wheel, NoseWheel, Head, Upper, Fore, Hand, Bowl }

        readonly struct Joint
        {
            public readonly Part part;
            readonly Transform node;
            readonly Quaternion rest, axes;
            readonly Vector3 position, up, forward;

            public Joint(Part kind, Transform transform, Transform vehicle)
            {
                part = kind;
                node = transform;
                rest = node.localRotation;
                position = node.localPosition;
                axes = Quaternion.Inverse(node.parent.rotation) * vehicle.rotation;
                up = node.parent.InverseTransformVector(vehicle.up);
                forward = node.parent.InverseTransformVector(vehicle.forward);
            }

            // The same world-space hinge works for metre-scaled kits and FBX roots with converted axes.
            public void Rotate(Quaternion q) => node.localRotation = axes * q * Quaternion.Inverse(axes) * rest;
            public void Translate(Vector3 offset) => node.localPosition = position + up * offset.y + forward * offset.z;
        }

        readonly List<Joint> joints = new();
        readonly List<Renderer> faces = new();
        readonly MaterialPropertyBlock glass = new();
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap_ST");
        static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap_ST");
        static readonly Vector3 LiftBar = new Vector3(0, -.142f, .595f) * 1.05f;
        readonly bool porter;
        int lastFace = -1, sampledTick = -1, lastId = -1;
        (int lift, long wheelMm) previous, next;
        public void ResetRound() => lastId = -1;

        public DistrictServiceMotion(Transform root, StreetRobot kind, bool papers)
        {
            porter = kind == StreetRobot.Porter;
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                switch (node.name)
                {
                    case "Porter_LinkUpper": case "Porter_LinkLower": Add(Part.Links, node); break;
                    case "Porter_Deck": Add(Part.Deck, node); break;
                    case "Porter_WheelR": case "Porter_WheelL": case "Clerk_Caster0": case "Clerk_Caster1": case "Clerk_Caster2":
                        Add(Part.Wheel, node); break;
                    case "Porter_NoseWheel": Add(Part.NoseWheel, node); break;
                    case "Porter_Head": case "Clerk_Head": Add(Part.Head, node); break;
                    case "Clerk_PickUpper": Add(Part.Upper, node); break;
                    case "Clerk_PickFore": Add(Part.Fore, node); break;
                    case "Clerk_PickHand": Add(Part.Hand, node); break;
                    case "Clerk_BowlFore": Add(Part.Bowl, node); break;
                    case "Porter_Head_Face": case "Clerk_Head_Face":
                        if (node.TryGetComponent<Renderer>(out var face)) faces.Add(face);
                        break;
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
            int state = moving ? 0 : stroke < .2f ? 1 : stroke < .86f ? 2 : 3;
            if (state != lastFace)
            {
                var tile = new Vector4(.25f, 1, state * .25f, 0);
                glass.SetVector(BaseMap, tile);
                glass.SetVector(EmissionMap, tile);
                foreach (var face in faces) face.SetPropertyBlock(glass);
                lastFace = state;
            }
            if (!detailed) return;
            double travel = (previous.wheelMm + (next.wheelMm - previous.wheelMm) * (double)fraction) * .001;
            Quaternion Spin(float radius) => Quaternion.AngleAxis((float)((travel / radius * Mathf.Rad2Deg) % 360), Vector3.right);
            var wheel = Spin(porter ? .335f * 1.05f : .081f * 1.1f);
            var nose = porter ? Spin(.103f * 1.05f) : Quaternion.identity;
            var links = Quaternion.AngleAxis(-21 * stroke, Vector3.right);
            foreach (var joint in joints)
            {
                switch (joint.part)
                {
                    case Part.Wheel: joint.Rotate(wheel); break;
                    case Part.NoseWheel: joint.Rotate(nose); break;
                    case Part.Links: joint.Rotate(links); break;
                    case Part.Deck: joint.Translate(links * LiftBar - LiftBar); break;
                    case Part.Head: joint.Rotate(porter ? Quaternion.Euler(0, 9 * stroke, 0) : Quaternion.Euler(4 * (1 - stroke), 9 - 16 * stroke, 0)); break;
                    case Part.Upper: joint.Rotate(Quaternion.Euler(-14 * stroke, 0, -9 * stroke)); break;
                    case Part.Fore: joint.Rotate(Quaternion.Euler(-23 * stroke, 0, 5 * stroke)); break;
                    case Part.Hand: joint.Rotate(Quaternion.Euler(18 * stroke, 13 * stroke, 0)); break;
                    case Part.Bowl: joint.Rotate(Quaternion.AngleAxis(-4 * stroke, Vector3.right)); break;
                }
            }
        }
    }
}
