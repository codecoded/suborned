// Looks through simview dumps for what reads wrong in play (brief 081): jumps, overlaps, people in cars, detours, drawn and idle, refused orders. Run from the repo root: bun briefs/081/analyze.ts out/simview/dump
import { readFileSync, readdirSync } from 'node:fs';
const dir = process.argv[2]!; const site = JSON.parse(readFileSync('sim/data/handover.site.json','utf8')); const grid = site.grid;
const blocked = (x:number,y:number)=>{const ix=Math.floor((x-grid.x0)/grid.cell),iy=Math.floor((y-grid.y0)/grid.cell); if(iy<0||iy>=grid.ny||ix<0||ix>=grid.nx) return false; return grid.rows[ix][iy]==='#';};
for (const f of readdirSync(dir).filter(f=>f.endsWith('.json')&&!f.startsWith('.')).sort()) {
  const d = JSON.parse(readFileSync(`${dir}/${f}`,'utf8')); const out:string[]=[];
  const T = d.lastSample+1; const pos = new Map<string,(number[]|null)[]>(); const ents = d.entities;
  const stAt=(e:any,t:number)=>{let r=e.st[0];for(const s of e.st){if(s[0]<=t)r=s;else break;}return r;};
  for (const e of ents) { const a:(number[]|null)[]=new Array(T).fill(null); for(const s of e.spans){let x=s[1],y=s[2];a[s[0]]=[x,y];for(let i=3,t=s[0]+e.step;i<s.length;i+=2,t+=e.step){x+=s[i];y+=s[i+1];a[t]=[x,y];}} pos.set(e.id,a);
    // jumps
    let prev:any=null; let jumps=0, maxj=0, jt=0; a.forEach((p,t)=>{ if(p&&prev){const dd=Math.hypot(p[0]-prev[0],p[1]-prev[1]); if(dd>120){jumps++; if(dd>maxj){maxj=dd;jt=t;}}} prev=p; });
    if(jumps) out.push(`JUMP ${e.id}: ${jumps} ticks over 12 m/s, biggest ${(maxj/100).toFixed(1)} m at t${jt} (${stAt(e,jt)[2]})`);
    // in blocked cells while walking
    let inb=0, first=-1; a.forEach((p,t)=>{ if(p&&blocked(p[0],p[1])&&!(stAt(e,t)[1]&1)){inb++; if(first<0)first=t;} }); if(inb>10&&e.side!=='civ') out.push(`SOLID ${e.id}: ${inb} ticks on a blocked walk cell from t${first} at ${a[first]} (${stAt(e,first)[2]})`); else if(inb>30) out.push(`SOLID civ ${e.id}: ${inb} ticks from t${first} at ${a[first]}`);
    // path ratio for each continuous move
    if(e.side!=='civ'){let startT=-1,len=0,sp:any=null; for(let t=1;t<T;t++){const p=a[t],q=a[t-1]; if(p&&q){const dd=Math.hypot(p[0]-q[0],p[1]-q[1]); if(dd>0){if(startT<0){startT=t;sp=q;len=0;} len+=dd;} else if(startT>=0&&a[Math.min(T-1,t+15)]&&Math.hypot(a[Math.min(T-1,t+15)]![0]-p[0],a[Math.min(T-1,t+15)]![1]-p[1])==0){const st=Math.hypot(p[0]-sp[0],p[1]-sp[1]); if(len>1500&&len/Math.max(st,1)>1.7) out.push(`DETOUR ${e.id} t${startT}-${t}: walked ${(len/100).toFixed(0)} m for ${(st/100).toFixed(0)} m straight (${stAt(e,startT)[2]})`); startT=-1;}}}}
  }
  // overlaps
  const major = ents.filter((e:any)=>e.side!=='civ'); const pairs=new Map<string,{n:number,first:number,min:number}>();
  for(let t=0;t<T;t++) for(let i=0;i<major.length;i++){const a=pos.get(major[i].id)![t]; if(!a||(stAt(major[i],t)[1]&1))continue; for(let j=i+1;j<major.length;j++){const b=pos.get(major[j].id)![t]; if(!b||(stAt(major[j],t)[1]&1))continue; const dd=Math.hypot(a[0]-b[0],a[1]-b[1]); if(dd<45){const k=major[i].id+'+'+major[j].id; const v=pairs.get(k)??{n:0,first:t,min:dd}; v.n++; v.min=Math.min(v.min,dd); pairs.set(k,v);}}}
  for(const [k,v] of pairs) if(v.n>=10 && !/councillor/.test(k)) out.push(`OVERLAP ${k}: ${(v.n/10).toFixed(1)} s under 45 cm from t${v.first}, closest ${v.min.toFixed(0)} cm`);
  // car ground
  const L=d.carLenCm/2,W=d.carWCm/2; for(const c of d.cars){ const keys=c.keys; const inCar=new Map<string,{n:number,first:number}>(); for(let t=0;t<T;t++){let k:any=null; for(const x of keys){if(x[0]<=t)k=x;else break;} if(!k||k.length<5)continue; const hn=Math.hypot(k[3],k[4])||1,hx=k[3]/hn,hy=k[4]/hn; for(const e of ents){ const p=pos.get(e.id)![t]; if(!p||(stAt(e,t)[1]&1))continue; const dx=p[0]-k[1],dy=p[1]-k[2],al=dx*hx+dy*hy,ac=-dx*hy+dy*hx; if(Math.abs(al)<L&&Math.abs(ac)<W){const v=inCar.get(e.id)??{n:0,first:t}; v.n++; inCar.set(e.id,v);} } } for(const [id,v] of inCar) if(v.n>=5) out.push(`CAR ${id} in ${c.id}: ${(v.n/10).toFixed(1)} s from t${v.first}`);}
  // gunfight stats
  const by:Record<string,{f:number,h:number,d:number}>={}; for(const s of d.shots){const e=ents.find((x:any)=>x.id===s[1]); const k=e?.side??'?'; const b=by[k]??={f:0,h:0,d:0}; b.f++; if(s[7]){b.h++;b.d+=s[8];}}
  const firstShot=d.shots[0]?.[0], lastShot=d.shots.at(-1)?.[0];
  // armed and idle: weapon drawn, standing, with no shots, long
  for(const e of major){ const a=pos.get(e.id)!; let run=0,start=0; const fired=new Set(d.shots.filter((s:any)=>s[1]===e.id).map((s:any)=>s[0])); for(let t=1;t<T;t++){const st=stAt(e,t); const still=a[t]&&a[t-1]&&a[t]![0]===a[t-1]![0]&&a[t]![1]===a[t-1]![1]; if(still&&(st[1]&2)&&!(st[1]&1)&&!fired.has(t)){if(!run)start=t;run++;}else{if(run>=200)out.push(`STALL ${e.id}: weapon drawn, still, not firing ${(run/10).toFixed(0)} s from t${start} (${stAt(e,start)[2]})`);run=0;} } if(run>=200)out.push(`STALL ${e.id}: weapon drawn, still, not firing ${(run/10).toFixed(0)} s from t${start} to the end (${stAt(e,start)[2]})`);}
  const refused=d.orders.filter((o:any)=>!o[2]);
  const civOn=[0,600,1200,1800,2400].map(t=>ents.filter((e:any)=>e.side==='civ'&&pos.get(e.id)![Math.min(t,T-1)]).length);
  console.log(`== ${d.script.replace('.json','')}: ${d.outcome} ${d.fail} ${Math.floor(d.tick/600)}:${String(Math.floor(d.tick/10)%60).padStart(2,'0')} T${d.hud.at(-1)[1].split(',')[1]} A${d.hud.at(-1)[1].split(',')[0]} shots ${d.shots.length}${firstShot!=null?` from t${firstShot} to t${lastShot}`:''} ${Object.entries(by).map(([k,v])=>`${k} ${v.h}/${v.f}`).join(' ')} refused ${refused.length} civOnStreet ${civOn} cars ${d.cars.map((c:any)=>c.st.at(-1)[1]).join('/')}`);
  for(const o of refused.slice(0,4)) out.push(`REFUSED t${o[0]} ${o[1].op} ${o[1].do} ${o[1].target??''}: ${o[3]}`);
  for(const l of out.slice(0,18)) console.log('  '+l); if(out.length>18) console.log(`  ... ${out.length-18} more`);
}
