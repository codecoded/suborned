using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Suborned.World.Play
{
    /// <summary>Linux needs the capture clip as VP8. Override only for the build, then restore the artist's original desktop settings.</summary>
    internal sealed class LinuxVideoImport : IDisposable
    {
        readonly List<(VideoClipImporter importer, VideoImporterTargetSettings original)> changed = new();
        const string Platform = "Standalone";
        public LinuxVideoImport()
        {
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:VideoClip", new[] { "Assets/Suborned/Resources/Video" }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as VideoClipImporter;
                    if (importer == null) throw new InvalidOperationException($"Linux video: no importer for {path}");
                    var previous = importer.GetTargetSettings(Platform);
                    changed.Add((importer, previous));
                    var settings = importer.defaultTargetSettings;
                    settings.enableTranscoding = true;
                    settings.codec = VideoCodec.VP8;
                    settings.resizeMode = VideoResizeMode.OriginalSize;
                    importer.SetTargetSettings(Platform, settings);
                    importer.SaveAndReimport();
                    if (importer.transcodeSkipped) throw new InvalidOperationException($"Linux video transcode skipped: {path}");
                    Debug.Log($"Linux video: {path} imported as VP8 at source size");
                }
            }
            catch { Dispose(); throw; }
        }
        public void Dispose()
        {
            foreach (var (importer, original) in changed)
            {
                if (original == null) importer.ClearTargetSettings(Platform);
                else importer.SetTargetSettings(Platform, original);
                importer.SaveAndReimport();
            }
            changed.Clear();
        }
    }
}
