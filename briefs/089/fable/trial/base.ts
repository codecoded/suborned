// Fable, brief 089: real-name base from Wikipedia lists (saved under wiki/). Scratch. No model anywhere.
import { readFileSync, writeFileSync, readdirSync } from 'fs';
const W = import.meta.dir + '/wiki/';
const ORIGIN: Record<string,string> = {
 Albania:'balkan',Armenia:'armenian',Austria:'germanic',Azerbaijan:'turkic',Belarus:'slavic',Belgium:'lowland',Bosniaks:'balkan',Serbs:'balkan','Bosnia and Herzegovina':'balkan',Bulgaria:'slavic',Croatia:'balkan','Czech Republic':'slavic',Denmark:'nordic','Faroe Islands':'nordic',Estonia:'baltic',Finland:'finnish',France:'french',Georgia:'georgian',Germany:'germanic',Greece:'greek',Hungary:'hungarian',Iceland:'nordic',Ireland:'celtic',Italy:'italian',Kazakhstan:'turkic',Kosovo:'balkan',Latvia:'baltic',Lithuania:'baltic',Luxembourg:'germanic',Malta:'maltese',Moldova:'romanian',Montenegro:'balkan',Netherlands:'lowland','North Macedonia':'balkan',Norway:'nordic',Poland:'slavic',Portugal:'iberian',Romania:'romanian',Russia:'slavic',Serbia:'balkan',Slovakia:'slavic',Slovenia:'balkan',Spain:'iberian','Canary Islands':'iberian',Sweden:'nordic',Switzerland:'germanic',Turkey:'turkic',Ukraine:'slavic','United Kingdom':'british',
 Bangladesh:'south-asian',Cambodia:'southeast-asian',China:'east-asian',India:'south-asian',Indonesia:'southeast-asian',Israel:'levantine',Japan:'east-asian',Korea:'east-asian',Malaysia:'southeast-asian',Nepal:'south-asian',Pakistan:'south-asian',Philippines:'southeast-asian','Sri Lanka':'south-asian',Taiwan:'east-asian',Thailand:'southeast-asian',Vietnam:'southeast-asian',
 Egypt:'arabic',Algeria:'arabic',Morocco:'arabic',Tunisia:'arabic',Libya:'arabic',Lebanon:'levantine',Jordan:'arabic',Syria:'levantine',Iraq:'arabic','Saudi Arabia':'arabic',Iran:'persian',Afghanistan:'persian',Nigeria:'west-african',Ghana:'west-african',Senegal:'west-african',Mali:'west-african','Ivory Coast':'west-african',Cameroon:'west-african','Equatorial Guinea':'west-african',Kenya:'east-african',Tanzania:'east-african',Uganda:'east-african',Ethiopia:'east-african',Somalia:'east-african',Rwanda:'east-african','South Africa':'southern-african',Zimbabwe:'southern-african',Botswana:'southern-african',Angola:'southern-african',Mozambique:'southern-african',
 Mexico:'latin-american',Cuba:'caribbean',Jamaica:'caribbean','Puerto Rico':'caribbean','Dominican Republic':'caribbean',Haiti:'caribbean',Trinidad:'caribbean',Argentina:'latin-american',Brazil:'latin-american',Chile:'latin-american',Colombia:'latin-american',Peru:'latin-american',Venezuela:'latin-american',Ecuador:'latin-american',Bolivia:'latin-american',Uruguay:'latin-american',Paraguay:'latin-american',
 Samoa:'polynesian',Tonga:'polynesian',Fiji:'polynesian','New Zealand':'polynesian',Hawaii:'polynesian',
};
const dict = new Set(readFileSync('/usr/share/dict/words','utf8').split('\n').filter(w=>w===w.toLowerCase()));
export const plain = (s:string)=>s.normalize('NFD').replace(/[̀-ͯ]/g,'').replace(/[łŁ]/g,'l').replace(/ß/g,'ss').replace(/[øØ]/g,'o').replace(/[æÆ]/g,'ae').replace(/[ıİ]/g,'i').replace(/[đĐ]/g,'d').replace(/[şŞ]/g,'s').replace(/ə/g,'a');
const clean=(s:string)=>{s=plain(s.trim());if(!/^[A-Z][a-z]{2,10}$/.test(s))return null;if(dict.has(s.toLowerCase()))return null;return s;};
function tokens(line:string){const out:string[]=[];
 line=line.replace(/<ref[^>]*\/>/g,'').replace(/<ref.*?<\/ref>/g,'').replace(/\{\{[^}]*\}\}/g,'').replace(/\([^)]*\)/g,'');
 for(const c of line.split(/\|\||^\||,|\bor\b|\//)){const m=c.match(/^\s*\[\[([^\]|]+\|)?([^\]]+)\]\]\s*$/);const t=m?m[2]:c.replace(/'''/g,'');const k=clean(t);if(k)out.push(k);}
 return out;}
const fam:Record<string,Set<string>>={};const add=(o:string,n:string)=>(fam[o]??=new Set()).add(n);
for(const f of ['European','Asian','North_American','South_American','Oceanian']){let o:string|undefined;
 for(const l of readFileSync(W+`sur_${f}.wiki`,'utf8').split('\n')){const h=l.match(/^==+\s*(.+?)\s*==+/);if(h){o=ORIGIN[h[1].replace(/\[\[|\]\]/g,'')];continue;}
  if(!o||!l.startsWith('|')||l.startsWith('|-')||l.startsWith('|+'))continue;
  const cells=tokens(l);for(const n of cells.slice(0,2))add(o,n);}}
const CAT:Record<string,string>={'Yoruba-language_surnames':'west-african','Igbo-language_surnames':'west-african','Hausa-language_surnames':'west-african','Zulu-language_surnames':'southern-african','Somali-language_surnames':'east-african','Swahili-language_surnames':'east-african','Samoan-language_surnames':'polynesian'};
for(const[c,o]of Object.entries(CAT))for(const n of readFileSync(W+`cat_${c}.txt`,'utf8').split('\n')){const k=clean(n);if(k)add(o,k);}
// given names, by region table and sex
const giv:Record<string,{m:Set<string>,f:Set<string>}>={};let sex:'m'|'f'='m';let o:string|undefined;
for(const l of readFileSync(W+'List_of_most_popular_given_names.wiki','utf8').split('\n')){
 if(/^====\s*Male/.test(l)){sex='m';continue}if(/^====\s*Female/.test(l)){sex='f';continue}
 if(!l.startsWith('|')||l.startsWith('|-')){continue}
 const m=l.match(/^\|\|?\s*\[\[([^\]|]+)/);let rest=l;
 if(m&&ORIGIN[m[1]]){o=ORIGIN[m[1]];rest=l.replace(/^\|\|?[^|]*?(<ref.*?(<\/ref>|\/>))?\s*(\|\|)/,'||');if(rest===l)continue;}
 else if(m&&!l.includes('||')&&!l.startsWith('|colspan')){o=undefined;continue}
 if(!o)continue;
 const r=l.startsWith('|colspan')?l.replace(/^\|colspan=\d+\|/,''):rest;
 for(const n of tokens(r)){(giv[o]??={m:new Set(),f:new Set()})[sex].add(n);}
}
const J=(s:Record<string,Set<string>>)=>Object.fromEntries(Object.entries(s).map(([k,v])=>[k,[...v].sort()]));
const out={family:J(fam),given:Object.fromEntries(Object.entries(giv).map(([k,v])=>[k,{m:[...v.m].sort(),f:[...v.f].sort()}]))};
writeFileSync(import.meta.dir+'/base.json',JSON.stringify(out,null,0));


