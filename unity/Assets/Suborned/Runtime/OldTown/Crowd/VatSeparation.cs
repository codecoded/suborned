using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Suborned.OldTown.Crowd
{
    /// <summary>Keeps the crowd's agents from walking through each other: agents closer than the sum of their radii
    /// push apart, and the push integrates into a per-agent sidestep that springs back towards the walk line and never
    /// exceeds <see cref="MaxPush"/>, so nobody is shoved off the pavement into a kiosk. A sidestep alone can't part
    /// agents on one link at one spot (it's capped), so an agent also brakes behind anyone walking its way within
    /// <see cref="Follow"/> of touching: its <see cref="Pace"/> (share of stride speed) eases down, and back up once
    /// clear. Neighbours come from a spatial hash (cells of the widest reach, hashed into a table sized to the crowd,
    /// not the city); the pass is one Burst job. Positions are xz only; order-independent (all forces, then all
    /// sidesteps).</summary>
    public sealed class VatSeparation : IDisposable
    {
        public const float MaxPush = .6f, Follow = 1.4f;  // m; x the pair's radii: braking starts this far apart
        const float Stiffness = 7f, Return = 1.2f;  // 1/s: overlap -> sidestep speed, sidestep decay
        const float MinPace = .15f, Ease = 3f;      // slowest share of stride speed; pace change per second
        const float SameWay = .3f;                  // heading cosine above which the one ahead is followed, not passed

        readonly float cellSize;
        NativeArray<float2> at, push, force, heading;
        NativeArray<float> radius, brake, pace;
        NativeArray<int> head, next;
        NativeArray<int2> cell;

        /// <param name="widest">the largest agent radius, m (sizes the hash cells)</param>
        public VatSeparation(int capacity, float widest)
        {
            cellSize = 2f * widest * Follow;
            heading = new NativeArray<float2>(capacity, Allocator.Persistent);
            radius = new NativeArray<float>(capacity, Allocator.Persistent);
            brake = new NativeArray<float>(capacity, Allocator.Persistent);
            pace = new NativeArray<float>(capacity, Allocator.Persistent);
            for (int i = 0; i < capacity; i++) pace[i] = 1f;
            at = new NativeArray<float2>(capacity, Allocator.Persistent);
            push = new NativeArray<float2>(capacity, Allocator.Persistent);
            force = new NativeArray<float2>(capacity, Allocator.Persistent);
            next = new NativeArray<int>(capacity, Allocator.Persistent);
            cell = new NativeArray<int2>(capacity, Allocator.Persistent);
            head = new NativeArray<int>(math.ceilpow2(math.max(capacity * 2, 64)), Allocator.Persistent);
        }

        /// <summary>This agent's current sidestep (world xz, metres).</summary>
        public Vector3 Offset(int i) => new(push[i].x, 0f, push[i].y);

        /// <summary>This agent's share of its stride speed (MinPace..1): below 1 while it follows someone.</summary>
        public float Pace(int i) => pace[i];

        /// <summary>Where agent i stands now (its walk-line position plus its current sidestep, xz), how wide it is and
        /// which way it walks (yaw, degrees).</summary>
        public void Place(int i, Vector3 walkLine, float r, float yaw)
        {
            at[i] = new float2(walkLine.x + push[i].x, walkLine.z + push[i].y);
            radius[i] = r;
            math.sincos(math.radians(yaw), out float s, out float c);
            heading[i] = new float2(s, c);
        }

        /// <summary>Advance every sidestep and pace by dt from the positions last <see cref="Place"/>d.</summary>
        public void Relax(int count, float dt) =>
            new Job
            {
                at = at, heading = heading, radius = radius, push = push, force = force, brake = brake, pace = pace,
                head = head, next = next, cell = cell, count = count, dt = dt, size = cellSize,
            }.Run();

        public void Dispose()
        {
            foreach (var a in new IDisposable[] { at, push, force, heading, radius, brake, pace, head, next, cell }) a.Dispose();
        }

        [BurstCompile(CompileSynchronously = true)]
        struct Job : IJob
        {
            [ReadOnly] public NativeArray<float2> at, heading;
            [ReadOnly] public NativeArray<float> radius;
            public NativeArray<float2> push, force;
            public NativeArray<float> brake, pace;
            public NativeArray<int> head, next;
            public NativeArray<int2> cell;
            public int count;
            public float dt, size;

            static int Hash(int2 c, int mask) => (int)(math.hash(c) & (uint)mask);

            public void Execute()
            {
                int mask = head.Length - 1;
                for (int i = 0; i < head.Length; i++) head[i] = -1;
                for (int i = 0; i < count; i++)
                {
                    cell[i] = (int2)math.floor(at[i] / size);
                    int h = Hash(cell[i], mask);
                    next[i] = head[h];
                    head[h] = i;
                    force[i] = 0;
                    brake[i] = 0;
                }
                for (int i = 0; i < count; i++)
                {
                    for (int y = -1; y <= 1; y++)
                        for (int x = -1; x <= 1; x++)
                        {
                            int2 c = cell[i] + new int2(x, y);
                            for (int j = head[Hash(c, mask)]; j >= 0; j = next[j])
                            {
                                if (j <= i || math.any(cell[j] != c)) continue;  // each pair once; skip hash collisions
                                float2 d = at[i] - at[j];
                                float d2 = math.lengthsq(d), touch = radius[i] + radius[j], reach = touch * Follow;
                                if (d2 >= reach * reach) continue;
                                float dist = math.sqrt(d2);
                                if (math.dot(heading[i], heading[j]) > SameWay)  // same way: the one behind brakes
                                {
                                    float b = math.saturate((reach - dist) / (reach - touch * .5f));
                                    if (math.dot(heading[i], d) < 0f) brake[i] = math.max(brake[i], b);  // j is ahead of i
                                    else if (math.dot(heading[j], d) > 0f) brake[j] = math.max(brake[j], b);
                                }
                                if (dist >= touch) continue;
                                // exactly coincident: split along a fixed per-agent direction instead of NaN
                                float2 dir = dist > 1e-4f ? d / dist : new float2(math.cos(i * 2.39996f), math.sin(i * 2.39996f));
                                float2 f = dir * (touch - dist);
                                force[i] += f;
                                force[j] -= f;
                            }
                        }
                }
                for (int i = 0; i < count; i++)
                {
                    float2 p = push[i] + (force[i] * Stiffness - push[i] * Return) * dt;
                    float l2 = math.lengthsq(p);
                    push[i] = l2 > MaxPush * MaxPush ? p * (MaxPush / math.sqrt(l2)) : p;
                    float want = math.lerp(1f, MinPace, brake[i]);
                    pace[i] = pace[i] + math.clamp(want - pace[i], -Ease * dt, Ease * dt);
                }
            }
        }
    }
}
