# Keel's scratch patch (brief 098, engine 0.50.0): the cover-cycle counter in a dial sweep (sim/cli/CoverCycles.cs), applied the same way to
# this branch's Dial.cs and to the before-engines' scratch copies. Usage: python3 -I patch-dial.py <Dial.cs>
import sys
p=sys.argv[1]
s=open(p).read()
if 'CoverCycles' in s: print('already'); sys.exit(0)
rep=[
("public string Counts = \"\"; public readonly Jams J = new Jams(); }",
 "public string Counts = \"\"; public readonly Jams J = new Jams(); public readonly CoverCycles.Tally Cover = new CoverCycles.Tally(); } // engine 0.50.0: the cover cycles (CoverCycles.cs)"),
("jams: Jams.Of(g, p.Extra, System.IO.Path.GetFileName(it).Replace(\".adaptive.json\", \"\"))); //",
 "jams: Jams.Of(g, p.Extra, System.IO.Path.GetFileName(it).Replace(\".adaptive.json\", \"\")), cover: CoverCycles.Of(g)); //"),
("            r.J.Merge(x.jams);\n",
 "            r.J.Merge(x.jams); r.Cover.Add(x.cover, $\"{plan} #{jobs[j].seed}\");\n"),
("r.J.Write(w, r.Runs); w.EndObj();",
 "r.J.Write(w, r.Runs); r.Cover.Write(w); w.EndObj();"),
("            var jl = r.J.Line(ok); if (jl.Length > 0) Console.Error.WriteLine(jl);\n",
 "            var jl = r.J.Line(ok); if (jl.Length > 0) Console.Error.WriteLine(jl);\n            Console.Error.WriteLine(r.Cover.Line());\n"),
("res[j].error != null ? \"-\" : res[j].value.downs.ToString(CultureInfo.InvariantCulture))));",
 "res[j].error != null ? \"-\" : res[j].value.downs.ToString(CultureInfo.InvariantCulture), res[j].error != null ? \"-\" : res[j].value.cover.Returns.ToString(CultureInfo.InvariantCulture), res[j].error != null ? \"-\" : res[j].value.cover.MaxTakes.ToString(CultureInfo.InvariantCulture))));"),
]
for a,b in rep:
    n=s.count(a)
    if n!=1: print('MISS', n, a[:90]); sys.exit(1)
    s=s.replace(a,b)
open(p,'w').write(s); print('patched')
