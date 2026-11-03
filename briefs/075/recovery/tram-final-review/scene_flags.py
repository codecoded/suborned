# Read-only: compare original rail-tile render GameObjects with replacement render-only objects.
import re,sys,collections
path=sys.argv[1]
docs={};cur=None;cls=None
hdr=re.compile(r'^--- !u!(\d+) &(-?\d+)')
keep={'1','4','23','33','205'}  # GameObject, Transform, MeshRenderer, MeshFilter, LODGroup
buf=[]
def flush():
    if cur and cls in keep: docs[cur]=(cls,''.join(buf))
with open(path,errors='replace') as f:
    for line in f:
        m=hdr.match(line)
        if m:
            flush();cls,cur=m.group(1),m.group(2);buf=[];continue
        if cls in keep: buf.append(line)
flush()
def field(t,k):
    m=re.search(r'^\s*'+k+r': (.*)$',t,re.M);return m.group(1).strip() if m else None
go={i:t for i,(c,t) in docs.items() if c=='1'}
tr={i:t for i,(c,t) in docs.items() if c=='4'}
name={i:field(t,'m_Name') for i,t in go.items()}
tr_go={i:m.group(1) for i,t in tr.items() for m in [re.search(r'm_GameObject: \{fileID: (-?\d+)',t)] if m}
go_tr={v:k for k,v in tr_go.items()}
father={i:m.group(1) for i,t in tr.items() for m in [re.search(r'm_Father: \{fileID: (-?\d+)',t)] if m}
def find(n): return [i for i,v in name.items() if v==n]
rails=find('Tram rails (old Tile_TramRail on our route)');surf=find('Joined tram render surfaces (source topology)')
print('rails roots',len(rails),'surface roots',len(surf))
children=collections.defaultdict(list)
for t,p in father.items(): children[p].append(t)
def desc(t):
    out=[t]
    for c in children.get(t,[]): out+=desc(c)
    return out
comp_by_go=collections.defaultdict(list)
for i,(c,t) in docs.items():
    if c in ('23','33','205'):
        g=re.search(r'm_GameObject: \{fileID: (-?\d+)',t)
        if g: comp_by_go[g.group(1)].append((c,i,t))
def summary(root):
    flags=collections.Counter();active=collections.Counter();rend=collections.Counter();lods=0
    for t in desc(go_tr[root]):
        g=tr_go.get(t)
        if g is None: flags['(stripped/prefab)']+=1; continue
        for c,i,txt in comp_by_go[g]:
            if c=='205':lods+=1
            if c=='23':
                flags[field(go[g],'m_StaticEditorFlags')]+=1;active[field(go[g],'m_IsActive')]+=1
                rend[(field(txt,'m_Enabled'),field(txt,'m_CastShadows'),field(txt,'m_ReceiveShadows'),field(txt,'m_LightProbeUsage'),field(txt,'m_ReflectionProbeUsage'),field(txt,'m_StaticShadowCaster'),field(txt,'m_RenderingLayerMask'),field(txt,'m_ReceiveGI'))]+=1
    return dict(staticFlags=dict(flags),active=dict(active),lodGroups=lods,renderer_enabled_cast_recv_lp_rp_ssc_layermask_gi=dict(rend))
for r in rails: print('RAILS',summary(r))
for r in surf: print('SURFACE',summary(r))
# LODGroups referencing rail renderers
rail_rend=set()
for t in desc(go_tr[rails[0]]):
    for c,i,txt in comp_by_go[tr_go[t]]:
        if c=='23': rail_rend.add(i)
hits=sum(1 for i,(c,t) in docs.items() if c=='205' and any(('fileID: '+r+'}') in t for r in rail_rend))
print('LODGroups referencing original rail renderers',hits)
