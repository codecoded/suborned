import re,sys,json
# incl.py <sample.txt> <lldb.out> [pattern...]: inclusive main-thread share per function (managed names via mono_pmip)
path,lld=sys.argv[1],sys.argv[2]; pats=sys.argv[3:] or ['Suborned','UIElements.Panel:Repaint','GPUResidentDrawer:PostPostLateUpdate','RebuildDrawListsIfNeeded','UniversalRenderPipeline:Render ','GC_','mono_gc','nextDrawable','WaitForLastPresentation','ScheduleBatchedJobsAndComplete','Internal_Cull','Submit_Internal','PlayerRender','DelayedCallManager','Coroutine','MonoBehaviour::CallUpdateMethod','RepaintPanels']
names={}; cur=None
for l in open(lld,errors='replace'):
    m=re.match(r'\(lldb\) expr -l c -- \(char\*\)mono_pmip\(\(void\*\)(0x[0-9a-f]+)\)',l)
    if m: cur=m.group(1); continue
    m=re.match(r'\(char \*\) \$\d+ = 0x[0-9a-f]+ " ?(.*?) \[\{',l)
    if m and cur: names[cur]=m.group(1); cur=None
lines=open(path,errors='replace').read().split('\n')
start=next(i for i,l in enumerate(lines) if 'com.apple.main-thread' in l and re.match(r'^\s+\d+ Thread_',l))
total=int(lines[start].split()[0]); stack=[]; incl={}
for l in lines[start+1:]:
    if re.match(r'^\s+\d+ Thread_',l) or not l.strip(): break
    m=re.match(r'^([\s+!:|]*)(\d+) (.*)$',l)
    if not m: continue
    d=len(m.group(1)); n=int(m.group(2)); name=m.group(3)
    a=re.search(r'\?\?\?  \(in <unknown binary>\)  \[(0x[0-9a-f]+)\]',name)
    if a: name=names.get(a.group(1),'???')
    else: name=re.sub(r'\s+\(in [^)]*\).*$','',name)
    name=re.sub(r'\s*\(.*$','',name) if not name.startswith('(wrapper') else name
    while stack and stack[-1][0]>=d: stack.pop()
    anc=set(s[1] for s in stack)
    if name not in anc: incl[name]=incl.get(name,0)+n
    stack.append((d,name))
print(f"main-thread samples {total}")
rows=sorted(incl.items(),key=lambda x:-x[1])
for k,v in rows:
    if any(p in k for p in pats) and v/total>=0.004: print(f"{100*v/total:5.1f}%  {v:5d}  {k[:150]}")
