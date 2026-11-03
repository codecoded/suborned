#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using static Suborned.OldTown.Editor.DecoKit.DecoKitFrontage;
using P = Suborned.OldTown.Editor.DecoKit.DecoKitCityPlan;
using L = Suborned.OldTown.Editor.DecoKit.DecoKitStreetLayout;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// A free-standing frontage block at any bearing (Southside): 14 m bays round a rectangle W long on its axis and D
    /// deep (28: back to back; deeper, a podium whose court the deck covers, DecoKitHighrise). The long faces run corner
    /// to corner (8 + 6k + 8 = W), each end carries (D - 16) / 6 bays taking the class of the long face they adjoin
    /// (DecoKitCityBlocks' short walls). Round it, a pavement on the block's own 2 m
    /// grid (flags courses along the axis): a width per face, 0 where a terrace or a square meets the facade; a granite
    /// kerb on its outer edge, corner pieces where two kerbs meet; tree pits every 10 m along the kerbed long faces.
    internal sealed class DecoKitBlock
    {
        internal const float Pit = 10f;

        internal readonly string Name;
        internal readonly Vector2 C;          // footprint centre (x, z)
        internal readonly float Phi, W, D;    // axis bearing (deg from +Z toward +X), length along it, depth across
        internal readonly (string cls, int n)[] Right, Left;  // long faces (right / left of the axis), tail to head
        internal readonly float WalkR, WalkL, WalkH, WalkT;   // pavement per face: right, left, head end, tail end
        internal readonly bool PitsR, PitsL;                  // tree pits along the long faces (where 4 m wide)

        internal DecoKitBlock(string name, float x, float z, float phi, float w, (string, int)[] right, (string, int)[] left,
                              float walkR = 4f, float walkL = 4f, float walkH = 4f, float walkT = 4f, float d = P.Depth,
                              bool pitsR = true, bool pitsL = true)
        {
            Name = name; C = new Vector2(x, z); Phi = phi; W = w; D = d; Right = right; Left = left;
            WalkR = walkR; WalkL = walkL; WalkH = walkH; WalkT = walkT; PitsR = pitsR; PitsL = pitsL;
        }

        internal Vector3 Axis => P.Dir(Phi);
        internal Vector3 Side => P.Dir(Phi + 90f);  // the right of the axis
        internal float Yaw => Phi + 90f;           // flags: module X along the axis, as the boulevard's (yaw 90 on +Z)

        /// World point at (u along the axis, v to its right) from the centre, on the ground.
        internal Vector3 At(float u, float v) => new Vector3(C.x, 0f, C.y) + Axis * u + Side * v;

        /// On the block or its pavement, grown by `margin`.
        internal bool Paved(Vector3 p, float margin = 0f)
        {
            var d = p - new Vector3(C.x, 0f, C.y);
            float u = Vector3.Dot(d, Axis), v = Vector3.Dot(d, Side), hw = W * .5f, hd = D * .5f;
            return u > -hw - WalkT - margin && u < hw + WalkH + margin && v > -hd - WalkL - margin && v < hd + WalkR + margin;
        }

        internal void Frontage(DecoKitPlacer p)
        {
            float hw = W * .5f, hd = D * .5f;
            foreach (var (face, segs) in new[] { ("right", Right), ("left", Left) })
            {
                int bays = 0;
                foreach (var s in segs) bays += s.n;
                if (8f + 6f * bays + 8f != W) Debug.LogError($"[DecoKit] block {Name}: {face} face has {bays} bays for W {W} (8 + 6k + 8)");
            }
            if ((D - 16f) % 6f != 0f) Debug.LogError($"[DecoKit] block {Name}: depth {D} is not 16 + 6k");
            Run(p, At(-hw, hd), Axis, Side, Right, Cap.Corner, Cap.Corner);
            Run(p, At(-hw, -hd), Axis, -Side, Left, Cap.Corner, Cap.Corner);
            Run(p, At(hw, -hd + 8f), Side, Axis, Ends(Left[^1].cls, Right[^1].cls), Cap.Open, Cap.Open);
            Run(p, At(-hw, -hd + 8f), Side, -Axis, Ends(Left[0].cls, Right[0].cls), Cap.Open, Cap.Open);
        }

        (string, int)[] Ends(string left, string right)
        {
            int k = Mathf.RoundToInt((D - 16f) / 6f);
            return left == right ? new[] { (left, k) } : new[] { (left, k - k / 2), (right, k / 2) };
        }

        /// The pavement; returns the tree pits (centre, the inward normal of their face).
        internal List<(Vector3 at, Vector3 inward)> Ring(DecoKitPlacer p)
        {
            var pits = new List<(Vector3, Vector3)>();
            float hw = W * .5f, hd = D * .5f, u0 = -hw - WalkT, u1 = hw + WalkH, v0 = -hd - WalkL, v1 = hd + WalkR;
            bool kr = WalkR > 0f, kl = WalkL > 0f, kh = WalkH > 0f, kt = WalkT > 0f;
            for (float u = u0 + 1f; u < u1; u += 2f)
                for (float v = v0 + 1f; v < v1; v += 2f)
                {
                    if (Mathf.Abs(u) < hw && Mathf.Abs(v) < hd) continue;
                    bool cu = Mathf.Abs(u) > hw, cv = Mathf.Abs(v) > hd;  // in an end strip / a long-face strip
                    bool outerU = Near(u, u0 + 1f) || Near(u, u1 - 1f), outerV = Near(v, v0 + 1f) || Near(v, v1 - 1f);
                    if (cu && cv && outerU && outerV)
                    {
                        var d = (u < 0f ? -Axis : Axis) + (v < 0f ? -Side : Side);  // outermost corner cell: a kerb corner
                        p.Put($"Kerb_GraniteCorner_{Parity(u, v, u0, v0)}", At(u, v), P.Yaw(d) + 45f);
                        continue;
                    }
                    bool pit = !cu && outerV && (v < 0f ? WalkL >= 4f && PitsL : WalkR >= 4f && PitsR) && Mathf.Abs(u) < hw - 4f && Near(Mathf.Repeat(u + hw - 5f + 1f, Pit), 1f);
                    p.Put($"{(pit ? "TreePit_Flags" : "Pave_Flags")}_{Parity(u, v, u0, v0)}", At(u, v), Yaw);
                    if (pit) pits.Add((At(u, v), v < 0 ? Side : -Side));
                }
            if (kr) Kerb(p, u0, u1, v1, true, kt, kh, -Side);
            if (kl) Kerb(p, u0, u1, v0, true, kt, kh, Side);
            if (kh) Kerb(p, v0, v1, u1, false, kl, kr, -Axis);
            if (kt) Kerb(p, v0, v1, u0, false, kl, kr, Axis);
            return pits;
        }

        /// Kerb stones along one outer edge (at `line` across it) from a to b, leaving the corner cells to the corner
        /// pieces where the crossing edge is kerbed too; walk side `inward`.
        void Kerb(DecoKitPlacer p, float a, float b, float line, bool alongAxis, bool cornerA, bool cornerB, Vector3 inward)
        {
            int k = 0;
            for (float s = a + 1f; s < b; s += 2f, k++)
            {
                if (s < a + 1.5f && cornerA || s > b - 1.5f && cornerB) continue;
                p.Put($"Kerb_Granite_{k & 1}", alongAxis ? At(s, line) : At(line, s), P.Yaw(-inward));
            }
        }

        static bool Near(float a, float b) => Mathf.Abs(a - b) < .01f;

        static int Parity(float u, float v, float u0, float v0) =>
            (Mathf.FloorToInt((u - u0) / 2f) & 1) + 2 * (Mathf.FloorToInt((v - v0) / 2f) & 1);
    }
}
#endif
