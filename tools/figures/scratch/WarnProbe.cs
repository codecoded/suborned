// Scratch Unity Editor script (brief 022, Forma): lists every model under Assets/People that has import messages (animation import warnings). Throwaway project like FigCheck.
// unity -batchmode -quit -projectPath <scratch> -executeMethod WarnProbe.Run -logFile <log>; grep WARNPROBE <log>
using System; using System.Linq; using UnityEditor; using UnityEngine;
public static class WarnProbe
{
    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        int total = 0;
        foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { "Assets/People" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var imp = AssetImporter.GetAtPath(p); if (imp == null) continue;
            var so = new SerializedObject(imp); var w = so.FindProperty("m_AnimationImportWarnings"); var e = so.FindProperty("m_AnimationImportErrors");
            var log = AssetImporter.GetImportLog(p);
            int n = log == null ? 0 : log.logEntries.Length;
            if (n > 0 || (w != null && w.stringValue.Length > 0) || (e != null && e.stringValue.Length > 0)) { total++; Debug.Log($"WARNPROBE {p} entries={n} warn=[{w?.stringValue}] err=[{e?.stringValue}]"); }
        }
        Debug.Log($"WARNPROBE done, models with import messages: {total}");
    }
}
