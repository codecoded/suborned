// Fable, brief 089 trial: three custom rules, run on the real Handover cast. Scratch. No model anywhere.
import { readFileSync, writeFileSync } from 'fs';
import { plain } from './base';
const D=import.meta.dir;const R='/Users/sky/Projects/suborned/';
const base=JSON.parse(readFileSync(D+'/base.json','utf8'));
const ban=JSON.parse(readFileSync(R+'contracts/people/llm-name-ban-list.json','utf8'));
const banned=new Set([...Object.keys(ban.strong),...ban.reported].map((s:string)=>s.toLowerCase()));
const looks=ban.lookalike.map((r:string)=>new RegExp(r));
export const isBanned=(s:string)=>{const l=plain(s).toLowerCase();return banned.has(l)||looks.some((r:RegExp)=>r.test(l));};
const dict=new Set(readFileSync('/usr/share/dict/words','utf8').split('\n').map(w=>w.toLowerCase()));
const fnv=(s:string)=>{let h=0x811c9dc5;for(let i=0;i<s.length;i++){h^=s.charCodeAt(i);h=Math.imul(h,0x01000193)>>>0;}return h>>>0;};
const V=/[aeiouy]/;
const OKFIN=new Set('nd nt ng nk rd rk rn rt rs rm ld lk lt lm st sk sh ch th ck ks ns ms ts'.split(' '));
const OKMID=/^(?:[bcdfgklmnprstvz]|[bcdfgkpt][lr]|[bcdfgkmnprstvz]{2}|sh|ch|th|ng|nk|nd|nt|mb|mp|rt|rd|rk|rm|rn|ld|lm|st|sk|sp|sl|sm|sn|ndr|ntr|str|mbr|ngr)$/;
export const sayable=(s:string)=>{const l=s.toLowerCase();if(!V.test(l)||/(.)\1\1/.test(l))return false;if(/[jqxw]$/.test(l)||/q(?!u)/.test(l))return false;
 for(const g of l.match(/[^aeiouy]+/g)??[]){if(l.startsWith(g)){if(!/^(?:[bcdfghjklmnprstvwzq]|[bcdfgkpt][lr]|sh|ch|th|zh|st|sk|sl|sm|sn|sp|sw|kw|gw|ny|mb|nd|ng|nk)$/.test(g)&&g.length>1)return false;continue;}
  if(l.endsWith(g)){if(g.length>2||(g.length===2&&!OKFIN.has(g)&&g[0]!==g[1]))return false;continue;}
  if(g.length>3||!OKMID.test(g))return false;}
 if(/[aeiouy]{3}/.test(l))return false;if(/^[hw](?![aeiou])/.test(l))return false;return true;};
const NOISE=new Set(['Arabic','Karelian','English','Croat','Bulacan','Batangas','Cavite','Cebu','Korea','Poland','Monaco','Dubai','Hudson','Spanish','Kobe','Coby','Abraham','David','Denis','Ali','Athanasius','Ryan','Kennedy','Fernando','Moore','Murray','Sullivan','Bernard','Andre','Francois','Reilly','Doyle','Reynolds','Henderson','Hernandez','Rodriguez','Wagner','Suzuki','Yamamoto','Tanaka','Sato','Kobayashi']);
const families:Record<string,string[]>=Object.fromEntries(Object.entries(base.family).map(([k,v]:any)=>[k,v.filter((n:string)=>!NOISE.has(n))]));
const realFamily=new Set(Object.values(families).flat().map((s:any)=>s.toLowerCase()));
for(const n of JSON.parse(readFileSync(R+'contracts/people/harbour-district.cast.json','utf8')).names.family)realFamily.add(n.name.toLowerCase());
// ---- given: real, sexed, by origin; a name in 4+ origins marks no origin and is dropped
const giv:Record<string,{m:string[],f:string[]}>=base.given;
const G=(o:string,m:string,f:string)=>{const rd=(p:string)=>readFileSync(D+'/wiki/gcat_'+p+'.txt','utf8').split('\n').map(x=>plain(x)).filter(x=>/^[A-Z][a-z]{2,9}$/.test(x));(giv[o]??={m:[],f:[]});giv[o].m=[...new Set([...giv[o].m,...rd(m)])];giv[o].f=[...new Set([...giv[o].f,...rd(f)])];};
G('east-asian','Japanese_masculine_given_names','Japanese_feminine_given_names');G('east-asian','Chinese_masculine_given_names','Chinese_feminine_given_names');G('greek','Greek_masculine_given_names','Greek_feminine_given_names');
const seen:Record<string,Set<string>>={};for(const[o,v]of Object.entries(giv))for(const n of[...v.m,...v.f])(seen[n]??=new Set()).add(o);
const noisy=new Set(['Tajikistan','Catalonia','Andorra','Gibraltar','Australia','Rembrandt','Poland','Monaco','Dubai','Hudson','Googoosh','Oisha','Uasal','Muire','Matha','Ryan','Mabel','Ethan','Archie','Alfie','Freddie','Charlie','Bonnie','Hallie','Imogen','Abby','Annie','Emily','James','Oliver','Charles','Arthur','Alfred','Dhr','Abdullo','Abd']);
const firstSight=(n:string)=>!/ao|ua|ae|ei[aeo]|aoi|uai|ii|uu|aa|ee$|^mb|^ng|dh|gh|bh|th$|yk|kw$/i.test(n);
const OK=/^.{3,7}$/;
const gpool=(s:'m'|'f')=>Object.entries(giv).flatMap(([o,v])=>v[s].filter(n=>OK.test(n)&&seen[n].size<4&&!noisy.has(n)&&firstSight(n)&&!isBanned(n)&&sayable(n)&&!dict.has(n.toLowerCase())).map(n=>({n,o}))).sort((a,b)=>a.n<b.n?-1:1);
export const GM=gpool('m'),GF=gpool('f');
// ---- A card names
export const clip=(s:string,k=5)=>{let c=s.slice(0,k);while(c.length>2){const l=c.toLowerCase(),e=l.slice(-2);if(!V.test(l.at(-1)!)&&!V.test(e[0])&&!OKFIN.has(e)){c=c.slice(0,-1);continue;}if(/[jqwh]$/.test(l)&&!/[aeo]h$/.test(l)){c=c.slice(0,-1);continue;}break;}return c;};
const cardFam=()=>{const o=new Map<string,string>();for(const[or,ns]of Object.entries(families))for(const n of ns){if(n.length<6||isBanned(n))continue;const c=clip(n);if(c.length<4||isBanned(c)||dict.has(c.toLowerCase())||!sayable(c))continue;if(!o.has(c))o.set(c,`${n} (${or})`);}return o;};
// ---- B joined names
const groups=(s:string)=>s.toLowerCase().match(/[aeiouy]+|[^aeiouy]+/g)!;
export const headOf=(s:string)=>{const g=groups(s);let v=0,o='';for(const x of g){o+=x;if(V.test(x[0])){v++;if(v===(g.filter(y=>V.test(y[0])).length>=3?2:1)&&o.length>=3)break;}}return o;};
export const tailOf=(s:string)=>{const g=groups(s);let i=g.length-1;while(i>=0&&!V.test(g[i][0]))i--;let on=i>0?g[i-1]:'';on=on.length>2?on.slice(-2):on;if(on.length===2&&!/^(?:[bcdfgkpt][lr]|sh|ch|th|st|sk|mb|nd|ng|nk|ll|nn|tt|ss|rr)$/.test(on))on=on.slice(-1);return on+g.slice(i).join('');};
const FAMILYGROUP:Record<string,string>={'south-asian':'S','southeast-asian':'E','east-asian':'E','west-african':'A','east-african':'A','southern-african':'A',arabic:'M',levantine:'M',turkic:'M',armenian:'C',georgian:'C',iberian:'L','latin-american':'L',caribbean:'L',italian:'L',french:'L',romanian:'L',maltese:'L',germanic:'N',nordic:'N',lowland:'N',british:'N',celtic:'N',slavic:'Y',balkan:'Y',baltic:'Y',finnish:'Y',hungarian:'Y',greek:'G',polynesian:'P'};
const parts=()=>{const hs:{p:string,n:string,o:string}[]=[],ts:{p:string,n:string,o:string}[]=[];for(const[o,ns]of Object.entries(families))for(const n of ns){if(n.length<4||isBanned(n))continue;const h=headOf(n),t=tailOf(n);if(h.length>=3&&h.length<=5&&V.test(h.at(-1)!))hs.push({p:h,n,o});if(t.length>=2&&t.length<=5&&V.test(t))ts.push({p:t,n,o});}return{hs,ts};};
const cap=(s:string)=>s[0].toUpperCase()+s.slice(1);
const TAILS:[string,string][]=[['ov','slavic'],['ev','slavic'],['enko','slavic'],['ski','slavic'],['ic','balkan'],['ek','slavic'],['sen','nordic'],['berg','nordic'],['lund','nordic'],['strom','nordic'],['mann','germanic'],['elli','italian'],['etti','italian'],['ini','italian'],['ez','iberian'],['es','iberian'],['ian','armenian'],['yan','armenian'],['idze','georgian'],['ani','persian'],['pour','persian'],['nia','persian'],['oglu','turkic'],['wala','south-asian'],['kar','south-asian'],['mura','east-asian'],['moto','east-asian'],['yama','east-asian'],['nen','finnish'],['escu','romanian'],['idis','greek'],['akis','greek'],['ou','greek'],['ola','west-african'],['ade','west-african'],['ike','west-african'],['wu','west-african'],['nga','east-african'],['azi','southern-african'],['ua','polynesian']];
const joinFam=()=>{const{hs}=parts();const ts=TAILS.map(([p,o])=>({p,n:p,o}));const out=new Map<string,string>();const hu=new Map(hs.map(h=>[h.p,h]));const tu=new Map(ts.map(t=>[t.p,t]));for(const h of hu.values())for(const t of tu.values()){if(FAMILYGROUP[h.o]===FAMILYGROUP[t.o])continue;const j=h.p+t.p;if(j.length<5||j.length>8)continue;if(/[aeiouy]{2}/.test(h.p.slice(-1)+t.p[0]))continue;const c=cap(j);if(realFamily.has(j)||isBanned(c)||dict.has(j)||!sayable(c))continue;if(!out.has(c))out.set(c,`${cap(h.p)}- from ${h.n} (${h.o}) + -${t.p}, a ${t.o} ending`);}return out;};
// ---- C trade names
export const THINGS=['card','film','reel','tube','stamp','file','tab','dial','wire','lamp','lens','glass','lead','pile','rod','rail','tram','dock','quay','berth','hull','crane','beam','gate','tower','deck','lift','sky','pier','bay','bell','pylon','valve','coil','rivet','sluice','ledger','docket','signal','gantry'];
const SUF:[string,string][]=[['ward','keeper, as in Hayward'],['wright','maker, as in Wainwright'],['smith','metalworker'],['man','crewman']];
const tradeFam=()=>{const o=new Map<string,string>();for(const t of THINGS)for(const[s,w]of SUF){let x=t+s;if(/e$/.test(t)&&s==='er')x=t+'r';if(/([^aeiou])$/.test(t)&&s==='er'&&/[aeiou][^aeiou]$/.test(t)&&!/[wy]$/.test(t))x=t+t.at(-1)+'er';const c=cap(x);if(c.length>10||dict.has(x)||isBanned(c)||realFamily.has(x))continue;o.set(c,`${t} + -${s} (${w})`);}return o;};
export const FAM={card:cardFam(),joined:joinFam(),trade:tradeFam()};
// ---- run on the real cast
const people=JSON.parse(readFileSync(R+'sim/data/handover.people.json','utf8')).people;
const FEMALE_JOBS=new Set<string>();
const pick=<T,>(xs:T[],k:string)=>xs[fnv(k)%xs.length];
const run=(c:'card'|'joined'|'trade')=>{const fam=[...FAM[c].keys()].sort();const used=new Set<string>();return people.map((p:any)=>{const sex=(fnv(p.id+'/sex')%2)?'f':'m';const gs=sex==='f'?GF:GM;for(let i=0;;i++){const os=[...new Set(gs.map(x=>x.o))].sort();const go=pick(os,`${p.id}/origin/${i}`);let g=pick(gs.filter(x=>x.o===go),`${p.id}/given/${i}`);let gn=g.n;const f=pick(fam,`${p.id}/family/${i}`);const full=`${gn} ${f}`;if(!used.has(full)||i>200){used.add(full);return{id:p.id,job:p.job,sex,name:full,given:gn,fromg:`${g.n}, ${g.o}`,family:f,why:FAM[c].get(f)};}}});};
if(import.meta.main){
 console.log('given pool',GM.length,GF.length);
 for(const c of ['card','joined','trade'] as const){const m=FAM[c];const r=run(c);const fs=r.map((x:any)=>x.family);const u=new Set(fs);const top=Object.entries(fs.reduce((a:any,f:string)=>(a[f]=(a[f]||0)+1,a),{})).sort((a:any,b:any)=>b[1]-a[1]).slice(0,3);
  const len=[...m.keys()].map(k=>k.length);console.log(`\n== ${c}: family pool ${m.size}; full names ${m.size*(GM.length+GF.length)}; mean len ${(len.reduce((a,b)=>a+b,0)/len.length).toFixed(1)}; cast 130: unique family ${u.size}, top ${JSON.stringify(top)}; full-name dupes ${130-new Set(r.map((x:any)=>x.name)).size}; banned ${r.filter((x:any)=>isBanned(x.given)||isBanned(x.family)).length}`);
  console.log(r.slice(0,24).map((x:any)=>`${x.name} [${x.job}]`).join(', '));
  writeFileSync(D+`/out-${c}.json`,JSON.stringify({pool:Object.fromEntries(m),cast:r},null,1));}
 writeFileSync(D+'/given.json',JSON.stringify({m:GM,f:GF}));
}
