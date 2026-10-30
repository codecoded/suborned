// usage: bun tools/game/hostlogic.ts [filter]
// The host's decisions run for real with no Unity: who is busy, what a click does to them, what the ring offers and costs, what the HUD model says, the words, the windows, and the
// Editor tests that need no scene (brief 078; Sky: "a lot of this can be done outside Unity though"). Compiles the player's scripts (runtime-compile), a test-only copy of Unity's assemblies with native
// calls answering defaults (hostlogic/Patch.cs; cached until Unity's version changes) and the cases (hostlogic/HostLogic.cs), then runs them in plain dotnet. No scene, views, input devices or
// pictures: those stay in the player's proof. The batch runs this right after `compile`, so a logic slip stops it before the player build.
import { existsSync, mkdirSync, readdirSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { ROOT, compile, managedDir, unityVersion } from './runtime-compile';

const DIR = resolve(import.meta.dir, 'hostlogic'), OUT = join(ROOT, 'out/batch/hostlogic'), UNITY = join(ROOT, 'unity');
/** The Editor tests that read no scene, no assets and no Editor API: compiled in as they are, so one source of truth. Others need Unity. */
export const EDITOR_TESTS = ['ContextRankTests.cs', 'ModalGateTests.cs', 'TpInputMapTests.cs'];

const dotnetEnv = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
const run = (cmd: string[], cwd = OUT) => { const r = Bun.spawnSync(cmd, { cwd, env: dotnetEnv, stdout: 'pipe', stderr: 'pipe' }); return { code: r.exitCode, out: r.stdout.toString() + r.stderr.toString() }; };
/** A package in Unity's cache by its name; its hash changes with the package's version. */
const cached = (name: string, file: string) => { const p = join(UNITY, 'Library/PackageCache'), d = existsSync(p) ? readdirSync(p).find(x => x.startsWith(name + '@')) : undefined; return d ? join(p, d, file) : ''; };

/** The runner's csproj; pure, so a test can read it. */
export function runnerCsproj(o: { player: string; shim: string; sim: string; sa: string; nunit: string; tests: string[]; cases: string }): string {
  return `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>disable</Nullable><InvariantGlobalization>true</InvariantGlobalization><LangVersion>latest</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><OutputPath>bin/</OutputPath><AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath><NoWarn>CS0618;CS1701;CS1702;NU1701;MSB3277;CS0436;CS0169;CS8632</NoWarn></PropertyGroup>
  <ItemGroup>
    <Compile Include="${o.cases}" />
${o.tests.map(f => `    <Compile Include="${f}" />`).join('\n')}
    <Reference Include="${o.player}" />
    <Reference Include="${o.sim}" />
    <Reference Include="${o.nunit}" />
    <Reference Include="${o.shim}/Unity*.dll" />
    <Reference Include="${o.sa}/Unity.InputSystem.dll" />
    <Reference Include="${o.sa}/Unity.RenderPipelines.Core.Runtime.dll" />
    <Reference Include="${o.sa}/Unity.RenderPipelines.Universal.Runtime.dll" />
  </ItemGroup>
</Project>
`;
}

/** The patched copy of Unity's assemblies, once per Unity version. */
function shim(ver: string): { dir: string; error?: string } {
  const dir = join(OUT, `unity-${ver}`);
  if (existsSync(join(dir, 'UnityEngine.CoreModule.dll')) && existsSync(join(dir, 'UnityEditor.CoreModule.dll'))) return { dir };
  const cecil = cached('com.unity.nuget.mono-cecil', 'Mono.Cecil.dll'), proj = join(OUT, 'patch/patch.csproj');
  if (!cecil) return { dir, error: 'Mono.Cecil is not in unity/Library/PackageCache: the project has to have opened once' };
  mkdirSync(join(OUT, 'patch'), { recursive: true });
  writeFileSync(proj, `<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>disable</Nullable><InvariantGlobalization>true</InvariantGlobalization><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NoWarn>CS1701;CS1702;MSB3277</NoWarn></PropertyGroup><ItemGroup><Compile Include="${join(DIR, 'Patch.cs')}"/><Reference Include="${cecil}"/></ItemGroup></Project>\n`);
  const editor = `/Applications/Unity/Hub/Editor/${ver}/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/UnityEditor.CoreModule.dll`;
  const r = run(['dotnet', 'run', '--project', proj, '--', managedDir(ver), dir, editor]);
  return r.code === 0 ? { dir } : { dir, error: `the Unity copy failed:\n${r.out.slice(-800)}` };
}

export type HostLogic = { ok: boolean; line: string; out: string; seconds: number };
export function hostLogic(filter = '', o: { skipCompile?: boolean } = {}): HostLogic {
  const t0 = performance.now(), done = (ok: boolean, line: string, out = ''): HostLogic => ({ ok, line, out, seconds: (performance.now() - t0) / 1000 });
  if (!o.skipCompile) { const c = compile(); if (c.missing) return done(false, c.missing); if (!c.ok) return done(false, `${c.errors.length} compile errors`, c.errors.slice(0, 20).join('\n')); }
  mkdirSync(OUT, { recursive: true });
  const ver = unityVersion(), s = shim(ver); if (s.error) return done(false, s.error);
  const patched = join(OUT, 'player'), pp = run(['dotnet', join(OUT, 'patch/bin/Debug/net10.0/patch.dll'), 'player', join(ROOT, 'out/batch/compile/bin/netstandard2.1/check.dll'), patched]);
  if (pp.code !== 0) return done(false, 'the player copy failed', pp.out.slice(-800));
  const nunit = cached('com.unity.ext.nunit', 'net472/unity-custom/nunit.framework.dll'); if (!nunit) return done(false, 'NUnit is not in unity/Library/PackageCache: the project has to have opened once');
  const tests = EDITOR_TESTS.map(f => join(UNITY, 'Assets/Suborned/Tests/Editor', f));
  const proj = join(OUT, 'hostlogic.csproj');
  writeFileSync(proj, runnerCsproj({ player: join(patched, 'check.dll'), shim: s.dir, sim: join(UNITY, 'Assets/Suborned/Sim/Plugins/Suborned.Sim.dll'), sa: join(UNITY, 'Library/ScriptAssemblies'), nunit, tests, cases: join(DIR, 'HostLogic.cs') }));
  const b = run(['dotnet', 'build', proj, '-nologo', '-v', 'q', '-clp:NoSummary']);
  if (b.code !== 0) return done(false, 'the cases do not compile', b.out.split('\n').filter(l => /: error /.test(l)).slice(0, 12).join('\n'));
  const r = run(['dotnet', join(OUT, 'bin/hostlogic.dll'), ROOT, ...(filter ? [filter] : [])]), lines = r.out.trim().split('\n');
  return done(r.code === 0, lines.at(-1) ?? '', r.out.trim());
}

if (import.meta.main) {
  const r = hostLogic(process.argv[2] ?? '');
  if (r.out) console.log(r.out); else console.log(r.line);
  console.log(`total ${r.seconds.toFixed(1)} s`);
  process.exit(r.ok ? 0 : 1);
}
