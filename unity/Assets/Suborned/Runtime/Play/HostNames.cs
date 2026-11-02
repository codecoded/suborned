namespace Suborned.Play
{
    /// <summary>Brief 089: the people the HUD names come from data, never code: the councillor as the mission's words call him (text.json names.targets, "Popovic"),
    /// (text.json mirrors the site cast the engine's sentences read); an agent's own name from book.cell.name ("Larthi"), its job word from names.speakers ("Scout").</summary>
    public sealed partial class HandoverHost
    {
        public string ValeWord => Text?.TargetName("councillor", null) ?? "the councillor";
        /// <summary>An agent's own name (book.cell.name), else its job word.</summary>
        public string AgentName(int k) { var r = Suborned.Sim.Op.Name((Suborned.Sim.OpId)k); return BookWordsFile.Get().AgentNames.TryGetValue(r, out var n) && n.Length > 0 ? n : AgentJob(k); }
        /// <summary>An agent's job word (names.speakers: "Scout").</summary>
        public string AgentJob(int k) { var r = Suborned.Sim.Op.Name((Suborned.Sim.OpId)k); return Text != null && Text.Speakers.TryGetValue(r, out var s) ? s : r; }
    }
}
