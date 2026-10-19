using System;
using System.IO;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Which build this is (Sky 2026-10-19: "need to be upping build numbers now"; every build on the Air said Version 0.1.0). GameBuild
    /// writes StreamingAssets/build.json for the build only (tools/game/version.json's version and the build's number, the mission recording, the
    /// git commit) and takes it out again; the engine's version is the DLL's own (Engine.Version). One quiet line, in the pause menu and the
    /// briefing's corner: "Build 0.2.4 · c023 · sim 0.8.2 · 8bf8ce6". In the Editor: "Editor · sim 0.8.2".</summary>
    public static class BuildStamp
    {
        public const string FileName = "build.json";
        static string line;

        public static string Sim => Engine.Version.StartsWith("suborned.sim/", StringComparison.Ordinal) ? Engine.Version["suborned.sim/".Length..] : Engine.Version;

        public static string Line => line ??= Load();

        static string Load()
        {
            var path = Path.Combine(Application.streamingAssetsPath, FileName);
            if (Application.isEditor || !File.Exists(path)) return $"Editor · sim {Sim}";
            try
            {
                var n = JNode.Parse(File.ReadAllText(path), FileName);
                string S(string k) => n.Opt(k) is JNode v && v.Kind == JKind.Str ? v.Str() : "";
                var version = S("version"); var rec = S("recording"); var sha = S("sha");
                bool dirty = n.Opt("dirty") is JNode d && d.Kind == JKind.Bool && d.Bool();
                return $"Build {(version.Length > 0 ? version : Application.version)}{(rec.Length > 0 ? $" · {rec}" : "")} · sim {Sim}{(sha.Length > 0 ? $" · {sha}{(dirty ? "+" : "")}" : "")}";
            }
            catch (Exception e) { Debug.LogWarning($"BuildStamp: {path}: {e.Message}"); return $"Build {Application.version} · sim {Sim}"; }
        }
    }
}
