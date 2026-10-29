# Read-only semantic scene diff keyed by Unity fileID: class counts of removed/added/changed docs and changed fields.
import re,sys,collections,difflib,json
hdr=re.compile(r'^--- !u!(\d+) &(-?\d+)(.*)$')
def load(p):
    d={};cur=None;buf=[]
    with open(p,errors='replace') as f:
        for line in f:
            m=hdr.match(line)
            if m:
                if cur:d[cur[1]]=(cur[0],cur[2],''.join(buf))
                cur=(m.group(1),m.group(2),m.group(3));buf=[];continue
            buf.append(line)
    if cur:d[cur[1]]=(cur[0],cur[2],''.join(buf))
    return d
A=load(sys.argv[1]);B=load(sys.argv[2])
rem=[k for k in A if k not in B];add=[k for k in B if k not in A];chg=[k for k in A if k in B and A[k]!=B[k]]
C=collections.Counter
print('docs A',len(A),'B',len(B));print('removed by class',C(A[k][0] for k in rem));print('added by class',C(B[k][0] for k in add));print('changed by class',C(A[k][0] for k in chg))
fields=C();examples=collections.defaultdict(list)
for k in chg:
    a=A[k][2].splitlines();b=B[k][2].splitlines()
    for line in difflib.unified_diff(a,b,lineterm='',n=0):
        if line.startswith(('---','+++','@@')):continue
        key=line[0]+re.sub(r'[-0-9.e]+','N',line[1:].strip())[:90];fields[key]+=1
        if len(examples[key])<2:examples[key].append((A[k][0],k,line[:160]))
for f,n in fields.most_common(40):print(n,f)
json.dump({'removed':rem[:50],'changedSample':{f:examples[f] for f,_ in fields.most_common(40)}},open('scene_semdiff.json','w'),indent=0)
