// What the claude CLI's strict --json-schema validator rejects, as far as confirmed (r001 call 02: prefixItems).
const UNKNOWN = ['prefixItems', 'unevaluatedProperties', 'unevaluatedItems', 'dependentRequired', 'dependentSchemas', '$dynamicRef', '$dynamicAnchor', 'minContains', 'maxContains'];
export function schemaProblems(root: any): string[] {
  const out: string[] = [];
  const resolves = (ref: string) => ref === '#' || (ref.startsWith('#/') && ref.slice(2).split('/').reduce((n: any, k) => n?.[k.replace(/~1/g, '/').replace(/~0/g, '~')], root) !== undefined);
  const walk = (n: any, at: string) => {
    if (Array.isArray(n)) return n.forEach((x, i) => walk(x, `${at}/${i}`));
    if (!n || typeof n !== 'object') return;
    for (const [k, v] of Object.entries(n)) {
      if (UNKNOWN.includes(k)) out.push(`strict mode: unknown keyword: "${k}" at ${at || '/'}`);
      if (k === '$ref' && typeof v === 'string' && !resolves(v)) out.push(`can't resolve reference ${v} at ${at || '/'}`);
      walk(v, `${at}/${k}`);
    }
  };
  walk(root, '');
  return out;
}
