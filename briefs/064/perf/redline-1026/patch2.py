# Redline scratch only: the follow-up instrumentation (alloc, Sight3, Grid.Route/Nearest, string-pull) on a COPY patched by patch.py.
import pathlib, sys
n = pathlib.Path(sys.argv[1])
g = n/'sim/Suborned.Sim/Game.cs'; s = g.read_text()
s = s.replace('try { var __s = System.Diagnostics.Stopwatch.GetTimestamp(); StepInner();', 'try { Prof.BeginTick(); var __s = System.Diagnostics.Stopwatch.GetTimestamp(); StepInner();', 1); g.write_text(s)
p = n/'sim/Suborned.Sim/Site.cs'; s = p.read_text()
old3 = 'public bool Sight3(Pt a, int za, Pt b, int zb, bool own = false, bool ownB = false)\n        {\n'
assert old3 in s
s = s.replace(old3, old3 + '            long __t = Prof.On ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; try {\n', 1)
i = s.index(old3); j = s.index('            return true;\n        }', i)
s = s[:j] + '            return true;\n            } finally { if (Prof.On) { Prof.SightN++; Prof.SightT += System.Diagnostics.Stopwatch.GetTimestamp() - __t; } }\n        }' + s[j+len('            return true;\n        }'):]
p.write_text(s)
p = n/'sim/Suborned.Sim/Grid.cs'; s = p.read_text()
sig = 'public Pt[]? Route(Pt a, Pt b)\n        {\n'; assert sig in s
s = s.replace(sig, sig.replace('public Pt[]? Route(Pt a, Pt b)', 'public Pt[]? Route(Pt a, Pt b) { long __t = Prof.On ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; try { return Route_(a, b); } finally { if (Prof.On) { Prof.RouteN++; Prof.RouteT += System.Diagnostics.Stopwatch.GetTimestamp() - __t; } } }\n        Pt[]? Route_(Pt a, Pt b)'), 1)
sig = 'public Pt? Nearest(Pt from, Func<Pt, bool> ok, int maxCm)\n        {\n'
if sig in s:
    s = s.replace(sig, sig.replace('public Pt? Nearest(Pt from, Func<Pt, bool> ok, int maxCm)', 'public Pt? Nearest(Pt from, Func<Pt, bool> ok, int maxCm) { long __t = Prof.On ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; try { return Nearest_(from, ok, maxCm); } finally { if (Prof.On) { Prof.NearN++; Prof.NearT += System.Diagnostics.Stopwatch.GetTimestamp() - __t; } } }\n        Pt? Nearest_(Pt from, Func<Pt, bool> ok, int maxCm)'), 1)
a = s.index('Pt[]? Route_(Pt a, Pt b)')
i = s.index('            if (P.G(t) == int.MaxValue) return null;', a)
s = s[:i] + '            long __pull = Prof.On ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; if (P.G(t) == int.MaxValue) { if (Prof.On) Prof.RouteNull++; return null; }\n' + s[i+len('            if (P.G(t) == int.MaxValue) return null;\n'):]
j = s.index('            return path.ToArray();', a)
s = s[:j] + '            if (Prof.On) { Prof.PullT += System.Diagnostics.Stopwatch.GetTimestamp() - __pull; Prof.RawLen += raw.Count; }\n' + s[j:]
p.write_text(s)
print('ok')
