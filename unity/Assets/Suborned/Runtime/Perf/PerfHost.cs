using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Suborned.Perf
{
    /// <summary>The machine a run is measured on: device, display and render settings into the report, and the power state
    /// (`pmset -g therm` and `-g batt` on macOS) at the run's edges, never while a stop records.</summary>
    public static class PerfHost
    {
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
            r.vSyncCount = QualitySettings.vSyncCount; r.targetFrameRate = Application.targetFrameRate;
            r.device = SystemInfo.deviceModel; r.gpu = SystemInfo.graphicsDeviceName; r.graphicsApi = SystemInfo.graphicsDeviceType.ToString();
            r.cpu = $"{SystemInfo.processorType} ({SystemInfo.processorCount} cores)"; r.os = SystemInfo.operatingSystem; r.memoryMB = SystemInfo.systemMemorySize;
            r.frameTimingEnabled = FrameTimingManager.IsFeatureEnabled();
            r.commandLine = Environment.CommandLine;
        }

        public static PowerSample Power(string when) => new() { when = when, therm = Run("/usr/bin/pmset", "-g therm"), batt = Run("/usr/bin/pmset", "-g batt") };

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
