// The batch player as a program of its own (Splice, brief 042): sim/cli/Batch.cs on a library it was not built with. An engine pinned to an old
// commit has no batch mode in its sim/cli, and its source must stay as recorded (the engine hash covers sim/ and sim/cli), so
// tools/missions/check/engine.ts copies batch.csproj, this file and ../cli/Batch.cs beside the pinned engine (out/missions/engine/<commit>/batch/)
// and builds them against that engine's own library. The play is the library's, so a line here is the line `play --json` prints there.
// usage: suborned-batch batch [...]   (the arguments of `suborned-sim batch`; run with the engine's root above the working folder)
using System;
using System.IO;
using Suborned.Sim;

static class BatchHost
{
    static int Main(string[] args)
    {
        try
        {
            var root = Engine.FindRoot(Directory.GetCurrentDirectory());
            return BatchCli.Run(Engine.Load(root), root, args.Length > 0 && args[0] == "batch" ? args : new[] { "batch" });
        }
        catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }
}
