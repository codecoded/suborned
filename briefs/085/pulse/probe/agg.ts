// usage: bun agg.ts <file.jsonl> ...  rival-stay table per plan
const med=(a:number[])=>{if(!a.length)return NaN;const s=[...a].sort((x,y)=>x-y);return s[Math.floor(s.length/2)]!};
const q=(a:number[],p:number)=>{if(!a.length)return NaN;const s=[...a].sort((x,y)=>x-y);return s[Math.min(s.length-1,Math.floor(s.length*p))]!};
for(const f of process.argv.slice(2)){
 const rows=(await Bun.file(f).text()).trim().split('\n').map(l=>JSON.parse(l));
 const plans=[...new Set(rows.map(r=>r.plan))];
 console.log(`# ${f} (${rows.length} runs)`);
 console.log('plan | win% | shotAtCrew% | preBrk% finBrk% | shot→break s med/p90 | left per break | of leavers: unhurt%, never-fired-after-shot% | up,unshot at break | downs | stay s (shot→left/down, leavers) | backoff/run | hidden% answer% | hpLost med | fight s med/p95 | opsDown | stand med | top fails');
 for(const p of plans){
  const R=rows.filter(r=>r.plan===p); const n=R.length; const wins=R.filter(r=>r.outcome==='success').length;
  const shot=R.filter(r=>r.firstAt>=0);
  const pre=R.filter(r=>r.preBreak>=0), fin=R.filter(r=>r.finBreak>=0);
  const brk=R.filter(r=>r.preBreak>=0||r.finBreak>=0);
  const t2b=brk.map(r=>((r.preBreak>=0?r.preBreak:r.finBreak)-r.firstAt)/10);
  let leavers=0,unhurt=0,nofire=0,unshot=0; const stay:number[]=[]; let downs=0; let boffs=0; let inF=0,hid=0,ans=0;
  for(const r of R){ for(const c of r.crew){ if(c.down>=0)downs++; boffs+=c.backOffs; inF+=c.inFight;hid+=c.hidden;ans+=c.answer;
    if(c.left>=0){leavers++; if(c.leftHp>=c.maxHp)unhurt++; if(c.firedAfter===0)nofire++; if(c.shotsAt===0)unshot++; if(c.firstShot>=0)stay.push((c.left-c.firstShot)/10);} } }
  const fails:Record<string,number>={};for(const r of R)if(r.outcome!=='success'){const k=r.outcome==='failure'?r.fail:r.outcome;fails[k]=(fails[k]||0)+1}
  const top=Object.entries(fails).sort((a,b)=>b[1]-a[1]).slice(0,4).map(([k,v])=>`${k} ${v}`).join(', ');
  const pc=(a:number,b:number)=>b?Math.round(a*100/b):0;
  console.log(`${p} | ${pc(wins,n)} | ${pc(shot.length,n)} | ${pc(pre.length,n)} ${pc(fin.length,n)} | ${med(t2b).toFixed(1)}/${q(t2b,.9).toFixed(1)} | ${(leavers/(brk.length||1)).toFixed(1)} | ${pc(unhurt,leavers)}, ${pc(nofire,leavers)} | ${pc(unshot,leavers)} | ${(downs/n).toFixed(1)} | ${med(stay).toFixed(1)} | ${(boffs/n).toFixed(2)} | ${pc(hid,inF)} ${pc(ans,inF)} | ${med(R.map(r=>r.hpLost))} | ${med(R.filter(r=>r.cellFightS>0).map(r=>r.cellFightS))}/${q(R.filter(r=>r.cellFightS>0).map(r=>r.cellFightS),.95)} | ${(R.reduce((a,r)=>a+r.opsDown,0)/n).toFixed(2)} | ${med(R.map(r=>r.standing))} | ${top}`);
 }
}
