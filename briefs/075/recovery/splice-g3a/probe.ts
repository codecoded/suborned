// Read-only audit: no loadPieces/loadBounds, generators, shared-cache writes or output regeneration.
import {readFileSync,writeFileSync,mkdirSync,existsSync} from 'node:fs';
import {createHash} from 'node:crypto';
import {loadSim} from '../../../../tools/geometry/sim';
import {buildVisible,kitIdsIn} from '../../../../tools/geometry/visible';
import {auditSight} from '../../../../tools/geometry/audit';
import {fbxOf,kitAssets,MEASURE_VERSION,MEASURE_CELL,PIECE_DIR} from '../../../../tools/geometry/pieces';
import {inPoly,segDist} from '../../../../tools/plan/geom';
const dir='out/scratch/splice/recovery-g3a', ev='briefs/075/recovery/splice-g3a';
const sha=(b:any)=>createHash('sha256').update(b).digest('hex');
const pins=new Map<string,string>();
const read=(p:string)=>{const b=readFileSync(p);const h=sha(b);if(pins.has(p)&&pins.get(p)!==h)throw Error('input changed '+p);pins.set(p,h);return b;};
const json=(p:string)=>JSON.parse(read(p).toString());
for(const p of ['out/world/harbour-district.world.json','out/world/harbour-district.walk.json','briefs/055/geometry.summary.json','briefs/075/oldbuild/READY-geometry-056','contracts/world/harbour-district.plan.json','contracts/world/kit.json','contracts/world/civic-block.json','contracts/world/civic-block.adjust.json','contracts/world/civic-look.json','contracts/rules/rulebook.json','contracts/world/geometry.ratchet.json',...new Bun.Glob('sim/data/*').scanSync({onlyFiles:true}),...new Bun.Glob('tools/geometry/*.{ts,py}').scanSync({onlyFiles:true})]) read(p);
const current=json(dir+'/current.world.json'), historical=json(dir+'/historical.world.json'), sim=loadSim(current);
const corners=[[sim.window.x0,sim.window.y0],[sim.window.x1,sim.window.y0],[sim.window.x0,sim.window.y1],[sim.window.x1,sim.window.y1]].map(b=>sim.frame.toPlan(b as any));
const win={x0:Math.min(...corners.map(p=>p[0])),x1:Math.max(...corners.map(p=>p[0])),y0:Math.min(...corners.map(p=>p[1])),y1:Math.max(...corners.map(p=>p[1]))};
const assets=kitAssets(), oldAssets=new Map(json(dir+'/historical-kit.json').assets.map((a:any)=>[a.id,a]));
const pieces=new Map(), provenance:any[]=[];
mkdirSync(dir+'/pieces',{recursive:true});
for(const id of new Set([...kitIdsIn(current,win),...kitIdsIn(historical,win)])){
 const a=assets.get(id),f=a&&fbxOf(a), cp=PIECE_DIR+'/'+id+'.json';
 if(!f||!existsSync(cp)){provenance.push({id,missing:true});continue;}
 const mesh=sha(read(f)),pd=json(cp); if(pd.sha!==mesh+':'+MEASURE_VERSION+':'+MEASURE_CELL)throw Error('stale cache '+id);
 writeFileSync(dir+'/pieces/'+id+'.json',read(cp));pieces.set(id,pd);
 const old:any=oldAssets.get(id);provenance.push({id,meshSha256:mesh,cacheSha256:pins.get(cp),cacheSignature:pd.sha,fbx:f,historicalCatalogueSha256:old?.files.find((x:any)=>x.role==='fbx')?.sha256,historicalMeshMatches:mesh===old?.files.find((x:any)=>x.role==='fbx')?.sha256});
}
function run(world:any,label:string){
 const vis=buildVisible(world,pieces,win),sight=auditSight({sim,vis}),owners=new Map<string,any>();
 for(const cell of sight.cells.filter(c=>c.kind==='eye')){
  const [i,j]=vis.R.cellOf(sim.frame.toPlan(cell.b)),id=vis.R.solidRel(i,j,sim.eye)!;
  let row=owners.get(id);if(!row){const meta=vis.meta.get(id);row={id,meta,mesh:provenance.find(p=>p.id===meta?.piece),samples:[],worldItem:world.street.find((s:any)=>s.id===id)??world.buildings?.find((s:any)=>s.id===id)};owners.set(id,row);}
  const nearest=sim.occluders.filter(o=>o.z<=sim.eye&&o.z+o.h>=sim.eye).map(o=>({id:o.id,d:inPoly(cell.b,o.poly)?0:Math.min(...o.poly.map((a,k)=>segDist(cell.b,a,o.poly[(k+1)%o.poly.length]!)))})).sort((a,b)=>a.d-b.d)[0];
  row.samples.push({block:cell.b,plan:sim.frame.toPlan(cell.b),ground:vis.R.ground[vis.R.idx(i,j)],nearestEyeOccluder:nearest});
 }
 const result={label,worldSha256:pins.get(dir+'/'+label+'.world.json'),eyeCells:sight.eyeMissing,chestCells:sight.chestMissing,count:sight.drawnEye.reduce((n,g)=>n+g.instances,0),groups:sight.drawnEye,unmeasured:[...vis.unmeasured],owners:[...owners.values()]};
 writeFileSync(ev+'/'+label+'.json',JSON.stringify(result,null,2)+'\n');console.log(label,result.count,result.eyeCells,result.chestCells,'unmeasured',result.unmeasured);
 return result;
}
const old=run(historical,'historical'),now=run(current,'current');
const currentOccluders=sim.occluders;
const site=json(dir+'/historical-handover.site.json');
sim.occluders=[...site.occluders,...site.cover.blockers.map((b:any)=>({...b,id:'cover:'+b.id}))].map((o:any)=>({...o,z:o.z/100,h:o.h/100,poly:o.poly.map((p:any)=>p.map((v:number)=>v/100))}));
const baseline=run(historical,'baseline-occluders');
const oldOccluders=sim.occluders;
sim.occluders=currentOccluders;
const coverage=now.owners.filter(r=>['street/capstone/planter-bed-13','street/capstone/planter-bed-17'].includes(r.id)).map(r=>({id:r.id,identicalWorldItem:JSON.stringify(historical.street.find((s:any)=>s.id===r.id))===JSON.stringify(current.street.find((s:any)=>s.id===r.id)),samples:r.samples.map((s:any)=>({...s,baselineNearest:oldOccluders.filter(o=>o.z<=sim.eye&&o.z+o.h>=sim.eye).map(o=>({occluder:o,d:inPoly(s.block,o.poly)?0:Math.min(...o.poly.map((a,k)=>segDist(s.block,a,o.poly[(k+1)%o.poly.length]!)))})).sort((a,b)=>a.d-b.d)[0]}))}));
writeFileSync(ev+'/planter-coverage.json',JSON.stringify(coverage,null,2)+'\n');
const historicalIds=new Set(kitIdsIn(historical,win));
const missingHistorical=provenance.filter(p=>historicalIds.has(p.id)&&!p.historicalMeshMatches);
if(missingHistorical.length||old.unmeasured.length||baseline.count!==58||baseline.eyeCells!==460||baseline.chestCells!==621||now.count!==59||now.eyeCells!==463||!coverage.every(r=>r.identicalWorldItem&&r.samples.every((s:any)=>s.baselineNearest.d<=0.6&&s.nearestEyeOccluder.d>0.6)))throw Error('focused diagnosis assertions failed');
console.log('PASS historical mesh provenance, exact baseline counts, current counts, unchanged planter exports, old fringe coverage and current missing coverage');
// Identity matching by civic id or street kind+position, since brief 075 changed minted street ids.
const key=(r:any)=>r.meta?.layer==='street'?JSON.stringify([r.meta.kind,r.meta.place?.at??r.worldItem?.at]):r.id;
const om=new Map(old.owners.map(r=>[key(r),r])),nm=new Map(now.owners.map(r=>[key(r),r]));
const delta={added:now.owners.filter(r=>!om.has(key(r))),removed:old.owners.filter(r=>!nm.has(key(r))),changed:now.owners.filter(r=>om.has(key(r))).flatMap(r=>{const o=om.get(key(r));return JSON.stringify([o.meta?.place,o.mesh?.meshSha256,o.samples])===JSON.stringify([r.meta?.place,r.mesh?.meshSha256,r.samples])?[]:[{old:o,current:r}];})};
writeFileSync(ev+'/delta.json',JSON.stringify(delta,null,2)+'\n');
writeFileSync(ev+'/mesh-provenance.json',JSON.stringify(provenance,null,2)+'\n');
const changed=[...pins].filter(([p,h])=>sha(readFileSync(p))!==h);
writeFileSync(ev+'/pins.json',JSON.stringify({pins:Object.fromEntries(pins),changed},null,2)+'\n');if(changed.length)throw Error('Inputs changed during comparison');
console.log('delta',delta.added.map(r=>r.id),delta.removed.map(r=>r.id),'changed',delta.changed.map(r=>r.current.id));
