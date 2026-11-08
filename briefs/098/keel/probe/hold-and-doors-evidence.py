# Keel's scratch builder (brief 098, engine 0.50.0): briefs/098/keel/hold-and-doors.json from the round's runs (out/scratch/keel/m050, m050f).
import json, re, glob, collections
K='/Users/sky/.t3/worktrees/suborned/t3-48e18061/out/scratch/keel'; M=K+'/m050'; F=K+'/m050f'
def load(p): return json.load(open(p))
SW={'0.48.0':load(M+'/sweep-e048.json'),'0.49.0':load(M+'/sweep-e049.json'),'0.50.0':load(F+'/sweep-050.json')}
sets=[s['setting'] for s in SW['0.50.0']['settings']]
def by(e): return {s['setting']:s for s in SW[e]['settings']}
B={e:by(e) for e in SW}
bands=[]
for s in sets:
    row={'setting':s}
    for e in SW:
        x=B[e][s]; row[e]={'runs':x['runs'],'wins':x['wins'],'errors':x['errors'],'replayDiffers':x['replayDiffers'],'fails':x['fails'],'planWins':x['planWins'],'cover':x.get('cover')}
    bands.append(row)
cyc={e:{k:sum(B[e][s]['cover'][k] for s in sets) for k in ('takes','returns','runsWithReturn','runsWithAFighter3Returns','runsWithAFighter6Takes','advances')} for e in SW}
q50={'0.48.0':'37/50 (74 %): returns 10 in 5 runs, a fighter with 6+ covers in 2','0.49.0':'14/50 (28 %): returns 321 in 50 runs, a fighter with 3+ returns in 38, 6+ covers in 36','0.50.0':None}
q=load(F+'/quietg50-050.json')['settings'][0]; q50['0.50.0']=f"{q['wins']}/50 ({2*q['wins']} %): returns {q['cover']['returns']} in {q['cover']['runsWithReturn']} runs, most {q['cover']['mostReturnsByOne']} by one fighter; fails {json.dumps(q['fails'])}"
def regions(path):
    d=load(path); out=[]
    for s in d['settings']:
        for r in s['regions']:
            if r['region']=='cafe-alley': continue
            keys={}
            for l in r['longest']:
                m=re.match(r'([0-9.]+) s (\S+) at \((-?\d+),(-?\d+)\) to (\S+(?: \S+)?) \((\S+) #(\d+)\)', l)
                if m: k=(m.group(2),m.group(3),m.group(4),m.group(5),m.group(7)); keys[k]=max(keys.get(k,0.0), float(m.group(1)))
            top=sorted(keys.items(), key=lambda kv:-kv[1])[:3]
            out.append({'setting':s['setting'],'region':r['region'],'box':r['box'],'distinct5sOrMore':len(keys),'distinct10sOrMore':sum(1 for v in keys.values() if v>=10),'perPlan5sOrMore':r['held5sOrMore'],'perPlan10sOrMore':r['held10sOrMore'],'maxS':r['maxS10']/10,'longest':[f"{v:.1f} s {k[0]} at ({k[1]},{k[2]}) to {k[3]} (seed {k[4]})" for k,v in top]})
    return out
holds={'how':"Vantage's regions probe (briefs/098/vantage/regions-probe.cs in a scratch copy of sim/cli, the same file built against each engine): 15 adaptive plans x 10 seeds a setting. A held spell is binned where the body stood when it ended; the city on a seed plays the same under every plan, so a spell shows once per plan: distinct counts each spell once (who, where, when it ended, seed) across plans (out/scratch/keel/m050/distinct.py). 0.48.0's per-plan counts are walk-law.json's",'0.49.0':regions(M+'/regions-049L.json'),'0.50.0':regions(F+'/regions-050L.json')}
def bod(path):
    d=load(path); return [{k:s[k] for k in ('setting','runs','wins','jumps','insideTwoRadii','tenetPairs','lawOverlaps','errors','held10sOrMore','heldMaxS10','changedPlans')} for s in d['settings']]
bodies={'how':'suborned-sim dial bodies: every adaptive plan x 3 seeds x 7 settings (315 runs), every tick read by the independent check (BodyCheck); and every route script x 3 seeds at people=64, guards=64 people=64, x4 (129 runs a setting; its 3 errors a setting are routes.json, the manifest, which is no script)','0.49.0':bod(M+'/bodies-049.json'),'0.50.0':bod(F+'/bodies-050.json'),'routes0.49.0':bod(M+'/bodies-routes-049.json'),'routes0.50.0':bod(F+'/bodies-routes-050.json')}
best={}
for f in glob.glob(F+'/perf-*.json'):
    n=f.split('/')[-1][5:]; e,_,rest=n.partition('-'); sc,_,rnd=rest.rpartition('-')
    for s in load(f)['settings']:
        k=(sc,s['setting'],e)
        if k not in best or s['msPerTick']<best[k]['msPerTick']: best[k]=s
perf={'how':'suborned-sim dial perf, 10 runs a setting, one thread, this Mac, three rounds alternating engines, the best of the three; route A played 0:00 to 4:10 (until 2500); loud-fight the same script on both','rows':[{'script':sc,'setting':st,'0.49.0':best[(sc,st,'e049')],'0.50.0':best[(sc,st,'050')]} for sc in ('route-a-410','loud-fight') for st in ('unset','x4','people=64','guards=64')]}
c62=load('/Users/sky/.t3/worktrees/suborned/t3-48e18061/runs/missions/handover/c062/run.json'); c61=load('/Users/sky/.t3/worktrees/suborned/t3-48e18061/runs/missions/handover/c061/run.json')
red=lambda r:[c['id'] for c in r['conditions'] if not c['ok']]
record={'id':'c062','verdict':c62['verdict'],'red':red(c62),'c061red':red(c61),'newRed':[x for x in red(c62) if x not in red(c61)],'nowGreen':[x for x in red(c61) if x not in red(c62)],'references':{k:c62['plays'][k]['outcome'] for k in ('route-a','route-b','route-g','loud')},'values':{c['id']:c['value'] for c in c62['conditions'] if c['id'] in ('B-1 quiet-g','L96 G','B-7 quiet-a','B-7 quiet-g','B-7 thin-quiet','B-9 quiet-b','B-11 quiet-a','B-11 quiet-g','B-11 thin-quiet','L96 B-all','R-7','MC-0','MC-5','MC-8','B-13')}}
ev={'schema':'suborned.evidence/1','brief':"098, Aster's ruling on engine 0.49.0 (2026-11-08): fighters hold cover; a door's guard stands to one side of the door: engine 0.50.0 (Keel)",
 'engines':{'0.48.0':'suborned.sim/0.48.0 at 8f4b9ded, its own data root (as in walk-law.json)','0.49.0':'suborned.sim/0.49.0 at a8ebe565, from git archive with its own data','0.50.0':"this branch's head, data unchanged (the door posts and the fight law are worked out at load and in play)"},
 'coverCycles':{'how':"dial sweep's cover tally (sim/cli/CoverCycles.cs, the same file built into each engine's CLI): per fighter the ai.cover lines in order; a return is a take of a point he took before with another between (he went back to cover he had left)",'totals':cyc,'quietG50Seeds':q50,'quietGSeed1':{'0.49.0':'rival-1 cv-035 -> cv-036 six times 5:17-6:34 (engage at 15.1 m, stepped 5.6 m back to cv-035 out of his reach, turned to cv-036 on the walk, advanced again); closed-in','0.50.0':'rival-1 fights the Tech at 14 m from where he stands and drops at 5:35.4; the fight 5:17.5-5:45.6; success'}},
 'bands':{'how':'suborned-sim dial sweep --seeds 20 --settings unset;people=32;people=64;guards=32;guards=64;cars=8;x4 (15 adaptive plans x 20 seeds, 2,100 runs an engine, the same plans and seeds); never tuned to win','rows':bands},
 'holdsByPlace':holds,'bodies':bodies,'tickCost':perf,'gaps':'161/161 gaps crossed both ways (nodes 46115, edges 357152), widest walled 0 cm','tickCostSplit':'loud-fight, x4 and guards=64, three rounds each, ms a tick: 0.49.0 0.096-0.098 and 0.049-0.050; 0.50.0 0.099-0.102 and 0.052-0.053; 0.50.0 with the doorway checks off (scratch build, out/scratch/keel/e050y) 0.115 and 0.066; 0.50.0 without the off-doorway fallback (e050x) 0.099-0.101 and 0.052-0.053. The doorway rule saves work; the rest is guards working round posts beside the doors','record':record,
 'doorPosts':{'0.49.0':{'cafe-entrance':[-775,-2865],'office-entrance':[6425,-4325],'official-entrance':[7675,-2975]},'0.50.0':{'cafe-entrance':[-903,-3072],'office-entrance':[6510,-4285],'official-entrance':[7695,-2736]},'how':"Population.SitePoints via DoorWays.Beside: the nearest point to the door on its open side, a body's radius outside every door's way (300 cm out, 80 cm either side of the door's line), with a passage (two personal spaces, 160 cm) from the door's own people"}}
open('/Users/sky/.t3/worktrees/suborned/t3-48e18061/briefs/098/keel/hold-and-doors.json','w').write(json.dumps(ev,indent=1)+'\n')
print('cycles', json.dumps(cyc)); print('q50', q50['0.50.0']); print(json.dumps(record['newRed']), json.dumps(record['nowGreen']))
