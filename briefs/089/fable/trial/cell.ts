// Fable 089: cell approach 1, rare real names from small naming traditions, drawn by id. Scratch, no model.
import { readFileSync } from 'fs'; import { plain } from './base'; import { isBanned } from './gen';
const W=import.meta.dir+'/wiki/';
const rd=(f:string)=>readFileSync(W+f,'utf8').split('\n').map(x=>plain(x.trim())).filter(x=>/^[A-Z][a-z]{2,5}$/.test(x)&&!isBanned(x));
const T:Record<string,{m:string[],f:string[]}>={
 basque:{m:rd('rare_Basque_masculine_given_names.txt'),f:rd('rare_Basque_feminine_given_names.txt')},
 faroese:{m:rd('rare_Faroese_masculine_given_names.txt'),f:[]},
 georgian:{m:rd('rare_Georgian_masculine_given_names.txt'),f:rd('rare_Georgian_feminine_given_names.txt')},
 frisian:{m:[],f:rd('rare_West_Frisian_given_names.txt')},
 igbo:{m:rd('gcat_Igbo_given_names.txt'),f:[]}, yoruba:{m:rd('gcat_Yoruba_given_names.txt'),f:[]}, akan:{m:rd('gcat_Akan_given_names.txt'),f:[]},
};
// the city (D) must not already use the name
const city=new Set(JSON.parse(readFileSync(import.meta.dir+'/out-d.json','utf8')).cast.flatMap((x:any)=>[x.given,x.family]));
for(const[k,v]of Object.entries(T)){v.m=v.m.filter(n=>!city.has(n));v.f=v.f.filter(n=>!city.has(n));console.log(k,'m',v.m.join(' '),'| f',v.f.join(' '));}
const fnv=(s:string)=>{let h=0x811c9dc5;for(let i=0;i<s.length;i++){h^=s.charCodeAt(i);h=Math.imul(h,0x01000193)>>>0;}return h>>>0;};
const all=(s:'m'|'f')=>Object.entries(T).flatMap(([k,v])=>v[s].map(n=>`${n} (${k})`));
for(const[a,s]of [['scout','m'],['scout','f'],['fixer','f'],['tech','m'],['enforcer','m']] as const){const xs=all(s);console.log(a,s,[0,1,2,3,4,5,6,7].map(i=>xs[fnv(`cell-${a}/rare/${i}`)%xs.length]).join(' | '));}
