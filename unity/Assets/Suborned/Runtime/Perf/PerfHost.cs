using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Perf
{
    /// <summary>The machine a run is measured on: device, display and render settings into the report; the preflight's load and
    /// power check (Redline M2: the Unity Editor, virtual machines, computer-use services, an animated wallpaper and anything else
    /// busy; Low Power Mode; AC) at the run's edges; and the thermal state (NSProcessInfo, through osascript) at every stop (M3).
    /// Nothing here runs while a stop records.</summary>
    public static class PerfHost
    {
        /// <summary>A process this busy (ps %CPU) that is not the player or the system compositor makes the machine busy.</summary>
        const double BusyCpu = 10.0;
        static readonly (string match, string what)[] Known =
        {
            ("Unity.app/Contents/MacOS/Unity", "the Unity Editor is open"),
            ("Virtualization.VirtualMachine", "a virtual machine is running"), ("qemu", "a virtual machine is running"),
            ("Parallels", "a virtual machine is running"), ("VMware", "a virtual machine is running"), ("UTM.app", "a virtual machine is running"),
            ("omputer Use", "a computer-use service is running"), ("omputer-use", "a computer-use service is running"),
        };

        public static void Describe(PerfReport r)
        {
            r.date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            r.unityVersion = Application.unityVersion;
            r.build = UnityEngine.Debug.isDebugBuild ? "development" : "release";
            r.quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            var rp = GraphicsSettings.currentRenderPipeline;
            r.pipeline = rp ? rp.name : "built-in";
            r.gpuResidentDrawer = rp is UniversalRenderPipelineAsset urp ? urp.gpuResidentDrawerMode.ToString() : "n/a";
            r.screenWidth = Screen.width; r.screenHeight = Screen.height;
            r.renderWidth = Display.main.renderingWidth; r.renderHeight = Display.main.renderingHeight;
            r.fullScreenMode = Screen.fullScreenMode.ToString();
            r.device = SystemInfo.deviceModel; r.gpu = SystemInfo.graphicsDeviceName; r.graphicsApi = SystemInfo.graphicsDeviceType.ToString();
            r.cpu = $"{SystemInfo.processorType} ({SystemInfo.processorCount} cores)"; r.os = SystemInfo.operatingSystem; r.memoryMB = SystemInfo.systemMemorySize;
            r.frameTimingEnabled = FrameTimingManager.IsFeatureEnabled();
            r.commandLine = Environment.CommandLine;
        }

        public static PerfMachine Machine(string when)
        {
            var m = new PerfMachine { when = when, therm = Run("/usr/bin/pmset", "-g therm"), batt = Run("/usr/bin/pmset", "-g batt"), thermal = Thermal() };
            m.onAC = m.batt.Contains("AC Power");
            m.lowPower = System.Text.RegularExpressions.Regex.IsMatch(Run("/usr/bin/pmset", "-g"), @"lowpowermode\s+1");
            var warnings = new List<string>();
            var ps = Run("/bin/ps", "-Ao pid=,pcpu=,pmem=,comm= -r").Split('\n');
            var top = new List<string>();
            var said = new HashSet<string>();
            foreach (var raw in ps)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (top.Count < 12) top.Add(line);
                var cols = line.Split((char[])null, 4, StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length < 4 || !double.TryParse(cols[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var cpu)) continue;
                string comm = cols[3];
                if (comm.Contains(".app/Contents/MacOS/unity") && comm.Contains("Suborned")) continue; // this player
                foreach (var (match, what) in Known) if (comm.Contains(match) && said.Add(what)) warnings.Add($"{what} ({Short(comm)}, {cpu:0.#}% CPU)");
                if ((comm.Contains("Aerial") || comm.Contains("Wallpaper")) && cpu >= 1.0 && said.Add("wallpaper")) warnings.Add($"an animated wallpaper is drawing ({Short(comm)}, {cpu:0.#}% CPU)");
                if (cpu >= BusyCpu && !comm.EndsWith("WindowServer") && !comm.Contains("kernel_task") && said.Add("cpu " + comm)) warnings.Add($"busy: {Short(comm)} at {cpu:0.#}% CPU");
            }
            if (m.lowPower) warnings.Add("Low Power Mode is on");
            if (!m.onAC) warnings.Add("not on AC power");
            if (m.thermal > 0) warnings.Add($"thermal state {m.thermal} (not nominal)");
            m.top = top.ToArray(); m.warnings = warnings.ToArray(); m.busy = warnings.Count > 0;
            return m;
        }

        /// <summary>NSProcessInfo.processInfo.thermalState (0 nominal .. 3 critical), -1 when unreadable. About 0.1 s.</summary>
        public static int Thermal()
        {
            var s = Run("/usr/bin/osascript", "-l JavaScript -e \"ObjC.import('Foundation'); $.NSProcessInfo.processInfo.thermalState\"");
            return int.TryParse(s, out var v) ? v : -1;
        }

        static string Short(string comm) { int i = comm.LastIndexOf('/'); return i >= 0 ? comm[(i + 1)..] : comm; }

        static string Run(string exe, string args)
        {
            if (Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.OSXEditor) return "n/a (not macOS)";
            try
            {
                using var p = Process.Start(new ProcessStartInfo(exe, args) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
                var text = p.StandardOutput.ReadToEnd();
                p.WaitForExit(5000);
                return text.Trim();
            }
            catch (Exception e) { return "unreadable: " + e.Message; }
        }
    }
}
