export type GddFile = { path: string; version: string };

/** Numeric semver comparison: 0.10.0 follows 0.9.0, independently of filename dates. */
export function compareVersions(a: string, b: string): number {
  if (![a, b].every(v => /^\d+\.\d+\.\d+$/.test(v))) throw new Error('Expected three-part numeric GDD version');
  const av = a.split('.').map(BigInt), bv = b.split('.').map(BigInt);
  for (let i = 0; i < 3; i++) if (av[i] !== bv[i]) return av[i]! > bv[i]! ? 1 : -1;
  return 0;
}

/** Both discovery and RAG must select the same latest matched design sources. */
export function selectLatestGdd(names: string[]): { agent: GddFile; human: GddFile } {
  const latest = (kind: 'agent' | 'human'): GddFile => {
    const pattern = kind === 'agent'
      ? /^the-suborned-agent-gdd-v(\d+\.\d+\.\d+)-\d{4}-\d{2}-\d{2}\.md$/
      : /^the-suborned-gdd-v(\d+\.\d+\.\d+)-\d{4}-\d{2}-\d{2}\.html$/;
    const files = names.flatMap(name => { const m = pattern.exec(name); return m ? [{ path: `GDD/${name}`, version: m[1]! }] : []; });
    files.sort((a, b) => compareVersions(b.version, a.version));
    const top = files[0];
    if (!top) throw new Error(`No versioned ${kind} GDD found`);
    if (files[1] && compareVersions(top.version, files[1].version) === 0) throw new Error(`Ambiguous latest ${kind} GDD v${top.version}: multiple files`);
    return top;
  };
  const agent = latest('agent'), human = latest('human');
  if (compareVersions(agent.version, human.version)) throw new Error(`GDD version mismatch: agent v${agent.version}, human v${human.version}. Update both before discovery.`);
  return { agent, human };
}
