using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using IOPath = System.IO.Path;

namespace Suborned.Play
{
    public sealed partial class DistrictJournal
    {
        [Serializable]
        sealed class ReadFile
        {
            public string[] ids;
            public string[] versions;
        }

        readonly Dictionary<string, string> read = new(StringComparer.Ordinal);
        static string ReadPath(string session) => string.IsNullOrEmpty(session) ? null : IOPath.ChangeExtension(session, "contacts.json");

        public bool IsUnread(int i) => bound.ContactKnown(i) &&
            (!read.TryGetValue(bound.ContactId(i), out var key) || key != bound.ContactNewsKey(i));

        void RememberRead(int i)
        {
            if (!IsUnread(i)) return;
            read[bound.ContactId(i)] = bound.ContactNewsKey(i);
            WriteRead(host.SessionPath);
        }

        void LoadIndex() => ReadFrom(host.SessionPath);

        void ReadFrom(string session)
        {
            read.Clear();
            string path = ReadPath(session);
            if (path == null || !File.Exists(path)) return;
            try
            {
                var saved = JsonUtility.FromJson<ReadFile>(File.ReadAllText(path));
                if (saved?.ids == null || saved.versions == null || saved.ids.Length != saved.versions.Length) return;
                for (int i = 0; i < saved.ids.Length; i++)
                    if (!string.IsNullOrEmpty(saved.ids[i]) && saved.versions[i] != null) read[saved.ids[i]] = saved.versions[i];
            }
            catch (Exception e) { Debug.LogWarning("Could not read the contact index. " + e.Message); }
        }

        void WriteRead(string session)
        {
            string path = ReadPath(session);
            if (path == null) return;
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                var ids = new List<string>(read.Keys);
                ids.Sort(StringComparer.Ordinal);
                var saved = new ReadFile { ids = ids.ToArray(), versions = ids.ConvertAll(id => read[id]).ToArray() };
                File.WriteAllText(temp, JsonUtility.ToJson(saved));
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            catch (Exception e) { Debug.LogWarning("Could not save the contact index. " + e.Message); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        public void ContinueIndex(string previous)
        {
            ReadFrom(previous);
            if (previous != host.SessionPath) WriteRead(host.SessionPath);
        }

        public void CopyIndex(string destination)
        {
            if (host.Game?.District != null) WriteRead(destination);
        }
    }
}
