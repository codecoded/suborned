// Fable, brief 089 option D: real names by origin and sex, drawn by id; the top 3 of each national list skipped; ban list skipped. Scratch. No model.
import { readFileSync, writeFileSync } from 'fs';
import { plain, ORIGIN } from './base';
import { isBanned } from './gen';
const W=import.meta.dir+'/wiki/', R='/Users/sky/Projects/suborned/';
const dict=new Set(readFileSync('/usr/share/dict/words','utf8').split('\n').filter(w=>w===w.toLowerCase()));
const ok=(s:string)=>{s=plain(s.trim());return /^[A-Z][a-z]{2,9}$/.test(s)&&!dict.has(s.toLowerCase())&&!isBanned(s)?s:null;};
Object.assign(ORIGIN,{England:'british',Scotland:'british','Northern Ireland':'celtic',Australia:'british'});
const SKIP=3, NOISE=new Set(['Arabic','Karelian','English','Croat','Bulacan','Batangas','Cavite','Cebu','Korea','Poland','Monaco','Dubai','Hudson','Tajikistan','Catalonia','Andorra','Gibraltar','Australia','Quebec','Brunei','Messi','Thor','Spanish','Russian','Kazakh','Sinhalese','Tamil','Malay','Chinese','Uzbek','Moor','John','Turkish','Greek','Arab','Armenian','Georgian','Ukrainian','Polish','German','Dutch','Somali','Hindu','Muslim','Christian','Portuguese','Mestizo','White','Black','Mixed','Indigenous','Amerindian']);
const cells=(l:string)=>{l=l.replace(/<ref[^>]*\/>/g,'').replace(/<ref.*?<\/ref>/g,'').replace(/\{\{[^}]*\}\}/g,'').replace(/\([^)]*\)/g,'');return l.split(/\|\||^\||,|\//).map(c=>{const m=c.match(/^\s*\[\[([^\]|]+\|)?([^\]]+)\]\]\s*$/);return (m?m[2]:c).replace(/'''/g,'').trim();}).filter(Boolean);};
// family: per country section, rank = row order; keep rank
const fam:Record<string,{n:string,rank:number,c:string}[]>={};
for(const f of ['European','Asian','North_American','South_American','Oceanian']){let o:string|undefined,c='',rank=0;
 for(const l of readFileSync(W+`sur_${f}.wiki`,'utf8').split('\n')){const h=l.match(/^==+\s*(.+?)\s*==+/);if(h){c=h[1].replace(/\[\[|\]\]/g,'');o=ORIGIN[c];rank=0;continue;}
  if(!o||!l.startsWith('|')||l.startsWith('|-')||l.startsWith('|+'))continue;const xs=cells(l).map(ok).filter(Boolean) as string[];if(!xs.length)continue;const n=xs[0];rank++;if(rank<=SKIP||NOISE.has(n))continue;(fam[o]??=[]).push({n,rank,c});}}
for(const[cat,o]of Object.entries({'Yoruba-language_surnames':'west-african','Igbo-language_surnames':'west-african','Hausa-language_surnames':'west-african','Zulu-language_surnames':'southern-african','Somali-language_surnames':'east-african','Samoan-language_surnames':'polynesian'}))for(const n of readFileSync(W+`cat_${cat}.txt`,'utf8').split('\n')){const k=ok(n);if(k&&!NOISE.has(k))(fam[o]??=[]).push({n:k,rank:0,c:cat});}
// given: per country row, No.1..No.10 in order; skip the first SKIP of each row
const giv:Record<string,{m:Set<string>,f:Set<string>}>={};let sex:'m'|'f'='m',o:string|undefined;
for(const l of readFileSync(W+'List_of_most_popular_given_names.wiki','utf8').split('\n')){
 if(/^====\s*Male/.test(l)){sex='m';continue}if(/^====\s*Female/.test(l)){sex='f';continue}if(!l.startsWith('|')||l.startsWith('|-'))continue;
 const m=l.match(/^\|\|?\s*\[\[([^\]|]+)/);let rest:string;
 if(m&&ORIGIN[m[1]]){o=ORIGIN[m[1]];if(!l.includes('||'))continue;rest=l.slice(l.indexOf('||'));}else if(m){o=undefined;continue}else if(o)rest=l.replace(/^\|colspan=\d+\|/,'||');else continue;
 const xs=cells(rest).map(ok).filter(Boolean) as string[];for(const n of xs.slice(SKIP))if(!NOISE.has(n))(giv[o!]??={m:new Set(),f:new Set()})[sex].add(n);}
const G=(o:string,mc:string,fc:string)=>{const rd=(p:string)=>readFileSync(W+'gcat_'+p+'.txt','utf8').split('\n').map(x=>ok(x)).filter(Boolean) as string[];(giv[o]??={m:new Set(),f:new Set()});rd(mc).forEach(n=>giv[o].m.add(n));rd(fc).forEach(n=>giv[o].f.add(n));};
G('east-asian','Japanese_masculine_given_names','Japanese_feminine_given_names');G('greek','Greek_masculine_given_names','Greek_feminine_given_names');
// a name that many origins share marks no origin: drop it
const seen:Record<string,number>={};for(const v of Object.values(giv))for(const n of new Set([...v.m,...v.f]))seen[n]=(seen[n]||0)+1;
const GV:Record<string,{m:string[],f:string[]}>={};for(const[k,v]of Object.entries(giv))GV[k]={m:[...v.m].filter(n=>seen[n]<6).sort(),f:[...v.f].filter(n=>seen[n]<6).sort()};
const ORIGINS=Object.keys(fam).filter(k=>GV[k]&&GV[k].m.length>=5&&GV[k].f.length>=5&&fam[k].length>=5).sort();
const fnv=(s:string)=>{let h=0x811c9dc5;for(let i=0;i<s.length;i++){h^=s.charCodeAt(i);h=Math.imul(h,0x01000193)>>>0;}return h>>>0;};
const pick=<T,>(xs:T[],k:string)=>xs[fnv(k)%xs.length];
export const draw=(id:string,sex:'m'|'f',i=0,origin?:string)=>{const o=origin??pick(ORIGINS,`${id}/origin/${i}`);const g=pick(GV[o][sex],`${id}/given/${i}`),f=pick(fam[o],`${id}/family/${i}`);return{o,name:`${g} ${f.n}`,given:g,family:f.n,from:`${f.c} #${f.rank||'cat'}`};};
if(import.meta.main){
 console.log('origins',ORIGINS.length,ORIGINS.join(' '));console.log('family',ORIGINS.reduce((a,k)=>a+fam[k].length,0),'given m',ORIGINS.reduce((a,k)=>a+GV[k].m.length,0),'f',ORIGINS.reduce((a,k)=>a+GV[k].f.length,0));
 const P=JSON.parse(readFileSync(R+'sim/data/handover.people.json','utf8')).people;
 const named:Record<string,[string,'m'|'f']>={'p-records-office-07':['Vale','m'],'p-cafe-14':['Inez','f'],'p-records-office-01':['guard','m'],'p-records-office-31':['driver','m'],'p-cafe-12':['Milena','f']};
 for(const[id,[r,s]]of Object.entries(named)){console.log(`\n${r} (${id}):`,[0,1,2,3,4,5].map(i=>{const d=draw(id,s,i);return `${d.name} [${d.o}, ${d.from}]`}).join(' | '));}
 console.log('\nordinary:');const out:any[]=[];const used=new Set<string>();let n=0;for(const p of P){const s=(fnv(p.id+'/sex')%2)?'f':'m';let i=0,d;do d=draw(p.id,s,i++);while(used.has(d.name));used.add(d.name);out.push({id:p.id,job:p.job,sex:s,...d});}
 console.log(out.filter(x=>!named[x.id]).slice(0,8).map(x=>`${x.name} [${x.job}, ${x.sex}, ${x.o}]`).join(' | '));
 const fs=out.map(x=>x.family);console.log('130: unique family',new Set(fs).size,'banned',out.filter(x=>isBanned(x.given)||isBanned(x.family)).length,'top',Object.entries(fs.reduce((a:any,f:string)=>(a[f]=(a[f]||0)+1,a),{})).sort((a:any,b:any)=>b[1]-a[1]).slice(0,3));
 const cell:Record<string,[string,'m'|'f']>={scout:['latin-american','f'],fixer:['east-asian','f'],tech:['west-african','m'],enforcer:['slavic','m']};
 for(const[k,[o,s]]of Object.entries(cell))console.log('cell',k,o,[0,1,2,3,4,5].map(i=>draw('cell-'+k,s,i,o).name).join(' | '));
 writeFileSync(import.meta.dir+'/out-d.json',JSON.stringify({origins:ORIGINS,cast:out},null,0));
}
