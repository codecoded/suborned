using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace Suborned.Perf
{
    /// <summary>The GPU Resident Drawer's per-view culling counters (Redline M5), by reflection (they are internal; the same reads as
    /// out/unity/lod/s2.cs in the Editor): Begin turns the drawer's batcher stats on and clears them in one frame's Update; Read, in
    /// a later Update, sums what the renders in between culled per view (camera; each shadow split) and turns the stats off again.
    /// Settle phase only: the stats cost the drawer time.</summary>
    public sealed class PerfDrawer
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        object grd, culler, stats, dbg;
        PropertyInfo display;
        MethodInfo flush;
        FieldInfo list;
        bool profiler, shown, begun;
        string unavailable = "not resolved yet";

        /// <summary>Finds the running drawer (it starts with the first render, after the harness): until then, unavailable.</summary>
        void Resolve()
        {
            if (grd != null) return;
            unavailable = null;
            try
            {
                var t = typeof(UnityEngine.Rendering.GPUResidentDrawer);
                grd = t.GetField("s_Instance", Any)?.GetValue(null);
                if (grd == null) { unavailable = "the drawer is not running"; return; }
                var g = grd; grd = null; // set again only when every member resolves
                culler = t.GetField("m_Culler", Any).GetValue(g);
                var ctx = t.GetField("m_GRDContext", Any).GetValue(g);
                stats = ctx.GetType().GetProperty("debugStats", Any).GetValue(ctx);
                list = stats?.GetType().GetField("instanceCullerStats", Any);
                flush = culler.GetType().GetMethod("FlushDebugCounters", Any);
                dbg = t.GetProperty("debugDisplaySettings", Any)?.GetValue(null);
                display = dbg?.GetType().GetProperty("displayBatcherStats", Any);
                if (display == null || list == null || flush == null) unavailable = "the drawer's debug stats are not in this player (no debug display settings)";
                else grd = g;
            }
            catch (Exception e) { unavailable = "unreadable: " + e.Message; }
        }

        public bool Available => unavailable == null;

        public void Begin()
        {
            Resolve();
            if (!Available) return;
            if (!begun) { profiler = Profiler.enabled; shown = (bool)display.GetValue(dbg); } // a Begin without a Read keeps the state from before the first
            begun = true;
            Profiler.enabled = true; display.SetValue(dbg, true);
            flush.Invoke(culler, null);
        }

        /// <summary>Begun and never read (settle ran out): puts the profiler and the batcher stats back as they were (Redline L3).</summary>
        public void Cancel()
        {
            if (!begun) return;
            begun = false;
            display.SetValue(dbg, shown); Profiler.enabled = profiler; flush.Invoke(culler, null);
        }

        public DrawerStats Read()
        {
            if (!Available) return new DrawerStats { error = unavailable };
            var r = new DrawerStats();
            try
            {
                var l = list.GetValue(stats);
                int len = (int)l.GetType().GetProperty("Length").GetValue(l);
                var item = l.GetType().GetProperty("Item");
                var per = new StringBuilder();
                for (int i = 0; i < len; i++)
                {
                    var s = item.GetValue(l, new object[] { i }); var st = s.GetType();
                    string view = st.GetField("viewType").GetValue(s).ToString();
                    int inst = (int)st.GetField("visibleInstancesOnCPU").GetValue(s), tris = (int)st.GetField("visiblePrimitivesOnCPU").GetValue(s), draws = (int)st.GetField("drawCommands").GetValue(s);
                    if (view == "Camera") { r.cameraInstances += inst; r.cameraTriangles += tris; r.cameraDraws += draws; }
                    else if (view == "Light") { r.shadowInstances += inst; r.shadowTriangles += tris; r.shadowDraws += draws; r.shadowSplits++; per.Append($"[{st.GetField("splitIndex").GetValue(s)}: {inst} inst {tris} tris {draws} draws] "); }
                    else per.Append($"({view} {inst}/{tris}/{draws}) ");
                }
                r.splits = per.ToString().Trim();
            }
            catch (Exception e) { r.error = "unreadable: " + e.Message; }
            finally { begun = false; display.SetValue(dbg, shown); Profiler.enabled = profiler; flush.Invoke(culler, null); }
            return r;
        }
    }
}
