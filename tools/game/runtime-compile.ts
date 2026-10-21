// usage: bun tools/game/runtime-compile.ts [extra.cs ...]
// Compiles the player's scripts (unity/Assets/Suborned/Runtime/**) headless with dotnet against the Unity version's own reference assemblies and the
// project's compiled packages (unity/Library/ScriptAssemblies), without opening or touching the Editor. A script that does not compile is found in
// seconds, not after the Editor's reload or a 20-minute player build. `bun run batch` runs it before it asks Unity for anything. Reads Library/, never
// writes it: output goes to out/batch/compile/. Extra .cs paths are compiled in as well (a staged script, before it is installed).
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

export const ROOT = resolve(import.meta.dir, '../..');
const UNITY = join(ROOT, 'unity');

export function unityVersion(): string { return readFileSync(join(UNITY, 'ProjectSettings/ProjectVersion.txt'), 'utf8').match(/m_EditorVersion: (\S+)/)?.[1] ?? ''; }
/** The reference assemblies of the Editor the project opens in (the mono player variation's UnityEngine modules). */
export const managedDir = (ver = unityVersion()) => `/Applications/Unity/Hub/Editor/${ver}/PlaybackEngines/MacStandaloneSupport/Variations/mono/Managed`;
const PACKAGES = ['Unity.InputSystem', 'Unity.RenderPipelines.Core.Runtime', 'Unity.RenderPipelines.Universal.Runtime', 'Unity.RenderPipelines.GPUDriven.Runtime'];

export type Compiled = { ok: boolean; errors: string[]; warnings: number; seconds: number; missing?: string };

/** The csproj text; pure, so a test can read it. */
export function csproj(o: { runtime: string; managed: string; scriptAssemblies: string; sim: string; extra: string[] }): string {
  return `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework><LangVersion>9.0</LangVersion><Nullable>disable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems><OutputPath>bin/</OutputPath><TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <NoWarn>CS0618;CS0649;CS0414;CS0169;CS0219;CS8632;CS0108;CS0067;CS0168;CS1591;CS0659;CS0661;CS0162;CS0660</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="${o.runtime}/**/*.cs" />
${o.extra.map(f => `    <Compile Include="${f}" />`).join('\n')}
    <Reference Include="${o.managed}/UnityEngine*.dll" />
${PACKAGES.map(p => `    <Reference Include="${o.scriptAssemblies}/${p}.dll" />`).join('\n')}
    <Reference Include="${o.sim}" />
  </ItemGroup>
</Project>
`;
}

export function compile(extra: string[] = []): Compiled {
  const t0 = performance.now(), managed = managedDir(), sa = join(UNITY, 'Library/ScriptAssemblies'), sim = join(UNITY, 'Assets/Suborned/Sim/Plugins/Suborned.Sim.dll');
  const need = [managed, ...PACKAGES.map(p => join(sa, `${p}.dll`)), sim].find(p => !existsSync(p));
  if (need) return { ok: false, errors: [], warnings: 0, seconds: 0, missing: `${need} is missing (Unity ${unityVersion()} installed? the project opened once? \`bun tools/sim/unity.ts\` run?)` };
  const dir = join(ROOT, 'out/batch/compile'); mkdirSync(dir, { recursive: true });
  const proj = join(dir, 'check.csproj');
  writeFileSync(proj, csproj({ runtime: join(UNITY, 'Assets/Suborned/Runtime'), managed, scriptAssemblies: sa, sim, extra: extra.map(f => resolve(f)) }));
  const env = { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_NOLOGO: '1', ...(process.env.DOTNET_ROOT || !existsSync('/opt/homebrew/opt/dotnet/libexec') ? {} : { DOTNET_ROOT: '/opt/homebrew/opt/dotnet/libexec' }) };
  const r = Bun.spawnSync(['dotnet', 'build', proj, '-nologo', '-v', 'q', '-clp:NoSummary'], { cwd: dir, env, stdout: 'pipe', stderr: 'pipe' });
  const text = r.stdout.toString() + r.stderr.toString();
  const errors = [...new Set(text.split('\n').filter(l => /: error \w+:/.test(l)).map(l => l.replace(/ \[[^\]]*check\.csproj\]$/, '').replace(UNITY + '/', 'unity/')))];
  const warnings = new Set(text.split('\n').filter(l => /: warning \w+:/.test(l))).size;
  return { ok: r.exitCode === 0 && !errors.length, errors, warnings, seconds: (performance.now() - t0) / 1000 };
}

if (import.meta.main) {
  const r = compile(process.argv.slice(2));
  if (r.missing) { console.error('runtime-compile: ' + r.missing); process.exit(2); }
  for (const e of r.errors.slice(0, 30)) console.log(e);
  console.log(r.ok ? `runtime-compile: Suborned.Runtime compiles (${r.warnings} warnings, ${r.seconds.toFixed(1)} s)` : `runtime-compile: ${r.errors.length} error${r.errors.length === 1 ? '' : 's'}`);
  process.exit(r.ok ? 0 : 1);
}
