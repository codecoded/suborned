// Glyph, brief 085. Mock only: no game code. The stage is 1920 x 1080 panel units, the HUD's own 1080p reference.
const $=(s,r=document)=>r.querySelector(s), stage=$('#stage'), rail=$('#rail');
const el=(h)=>{const t=document.createElement('template');t.innerHTML=h.trim();return t.content.firstChild};
const ic=(n,c)=>`<span class="ic i-${n}"${c?` style="--c:${c}"`:''}></span>`;
const cam=(who,cls='')=>`<span class="cam ${who==='guard'?'bare':''} ${cls}" style="background-image:var(--c_${who})"></span>`;
const q=new URLSearchParams(location.hash.slice(1));
let timers=[];const later=(f,ms)=>{const t=setTimeout(f,ms);timers.push(t);return t};
const clearAll=()=>{timers.forEach(clearTimeout);timers=[];clearInterval(tick);stage.innerHTML=''};let tick=0;

// ------------------------------------------------------------------ the mission's people, as the engine would feed them (Handover values; temperaments of the driver and guard are stand-ins, a seed sets them)
const PEOPLE={
 waitress:{name:'Inez',why:'Close to the door guard',told:{i:'debt',w:'Debt'},mood:{i:'mood_uneasy',w:'Uneasy'},need:{i:'debt',w:'Bureau loan'},nature:{i:'fearful',w:'Fearful'},ties:{i:'lovers',w:'Loves the guard'},fit:{ask:'no',pay:'half',help:'half',pressure:'ok'},walk:16,world:'tag'},
 driver:{name:'The driver',why:'Waits for Vale at the pickup',told:{i:'cash',w:'Cash'},mood:{i:'mood_friendly',w:'Calm'},need:{i:'cash',w:'Lean month'},nature:{i:'mercenary',w:'For hire'},ties:{i:'secret_none',w:'No secret'},fit:{ask:'no',pay:'ok',help:'half',pressure:'half'},walk:44,world:'edge'},
 guard:{name:'The guard',why:'Stands on the Office door',told:null,mood:{i:'mood_friendly',w:'Calm'},need:{i:'cash',w:'Promotion'},nature:{i:'loyal',w:'Loyal'},ties:{i:'lovers',w:'Loves Inez'},fit:{ask:'no',pay:'no',help:'half',pressure:'no'},walk:38,world:'edge'},
};
const WAYS={ask:{w:'Ask',t:20,$:0,tr:1,opens:null},pay:{w:'Pay',t:35,$:70000,tr:2,opens:'watch'},help:{w:'Help',t:35,$:40000,tr:1,opens:'listen'},pressure:{w:'Pressure',t:25,$:0,tr:3,opens:'listen'}};
const TIP={none:'Watch or Listen. Learning only costs time.',watched:'Listen: find out what they need.',heard:'Pick a way. There is always more than one.',turned:'Call them when the moment comes.'};
let know, focus, fresh, help='early', dock='a';
const reset=()=>{know={waitress:{},driver:{},guard:{}};focus='waitress';fresh={}};
const stateOf=k=>know[k].turned?'turned':know[k].listen?'heard':know[k].watch?'watched':'none';
const opened=k=>{const K=know[k],o=[];for(const w in WAYS){const r=WAYS[w].opens;if(!r||K[r])o.push(w)}if(K.turned)o.push('call');return o};
const fmt=s=>s>=60?`${Math.floor(s/60)}:${String(s%60).padStart(2,'0')}`:s+' s';

// ------------------------------------------------------------------ the board
function priceHtml(k,what){const P=PEOPLE[k];
 if(what==='watch')return `<span>Watch</span>${ic('p_time')}<span>${fmt(P.walk+20)}</span><span class="dim">· what ${k==='waitress'?'she':'he'} does, ${k==='waitress'?'her':'his'} mood · opens Pay</span>`;
 if(what==='listen')return `<span>Listen</span>${ic('p_time')}<span>${fmt(P.walk+30)}</span><span class="dim">· need, nature, ties · opens Help, Pressure</span>`;
 if(what==='call')return `<span>Call ${P.name}</span>${ic('p_time')}<span>5 s</span><span class="dim">· ${k==='waitress'?'pulls the guard off the door':'he moves when you say'}</span>`;
 const W=WAYS[what];
 if(W.opens&&!know[k][W.opens])return `<span>${W.w}</span>${ic('lock','#b0cdc7')}<span class="dim">opens after ${W.opens==='watch'?'Watch':'Listen'}</span>`;
 return `<span>${W.w}</span>${W.$?`<span>$${W.$.toLocaleString('en-US')}</span>`:''}${ic('p_time')}<span>${fmt(P.walk+W.t)}</span>${ic('p_trail')}<span>+${W.tr}</span>`;
}
function nextOf(k){const K=know[k];if(K.turned)return 'call';if(!K.watch)return 'watch';if(!K.listen)return 'listen';return null}
function slotHtml(kind,word,val,toldV,fill){
 if(val)return `<span class="slot known ${fill?'fill':''}">${ic(val.i)}${val.w}</span>`;
 if(toldV)return `<span class="slot told">${ic(toldV.i)}${toldV.w}</span>`;
 return `<span class="slot unk">${word}</span>`;
}
function rowHtml(k,i,opts={}){const P=PEOPLE[k],K=know[k],foc=focus===k&&help==='early',face=`style="background-image:var(--c_${k})"`,fill=fresh['fill_'+k];
 const nx=nextOf(k);
 let body='';
 if(help==='later'||(!foc)){ // compact: the glance, with the next step
  const dots=[K.watch?P.mood:null,K.listen?P.need:null,K.listen?P.nature:null,K.listen?P.ties:null];
  const words=['Mood','Need','Nature','Ties'];
  const gl=dots.map((d,j)=>d?`<span class="gdot">${ic(d.i)}</span>`:(j===1&&P.told)?`<span class="gdot told">${ic(P.told.i)}</span>`:`<span class="gdot unk" title="${words[j]}"></span>`).join('');
  const who=nx==='watch'?'scout':nx==='listen'?'scout':nx==='call'?'fixer':'fixer';
  const chip=K.turned?`<span class="gdot next" data-do="call" data-who="${k}">${cam('fixer')}${ic('call','#60e0ea')}</span>`:nx?`<span class="gdot next" data-do="${nx}" data-who="${k}">${cam(who)}${ic(nx,'#60e0ea')}</span>`:`<span class="gdot next" data-focus="${k}">${cam('fixer')}${ic('turn_person','#60e0ea')}</span>`;
  return `<div class="row ${K.turned?'turned':''}" data-row="${k}"><div class="top"><div class="face ${k==='guard'?'bare':''}" ${face}></div><div><div class="nm">${P.name}</div>${help==='early'?`<div class="why">${P.why}</div>`:''}${help==='later'?`<div style="display:flex;gap:4px;margin-top:4px">${gl}</div>`:''}</div><div class="glance">${help==='early'?gl:''}${chip}</div></div></div>`;
 }
 if(K.turned){
  return `<div class="row focus turned" data-row="${k}"><div class="top"><div class="face ${k==='guard'?'bare':''}" ${face}></div><div><div class="nm">${P.name}</div><div class="why tq">Turned</div></div><div class="glance"><span class="act next" data-do="call" data-who="${k}" data-price="call">${cam('fixer')}${ic('call','#60e0ea')}<span class="w">Call</span>${fresh['way_'+k+'_call']?'<i class="sparkle"></i>':''}</span></div></div><div class="price" data-p="${k}">${priceHtml(k,'call')}</div></div>`;
 }
 const way=w=>{const W=WAYS[w],shut=W.opens&&!K[W.opens],mk=K.listen?P.fit[w]:(K.watch&&w==='help'?'half':'unk'),fr=fresh['way_'+k+'_'+w];
  return `<span class="act way ${shut?'shut':''} ${fr?'fresh':''}" data-way="${w}" data-who="${k}" data-price="${w}">${ic(shut?'lock':w)}${W.w}${shut?'':`<span class="mk" style="background-image:var(--mk_${mk})"></span>`}</span>`};
 const watchBtn=K.watch?'':`<span class="act ${nx==='watch'?'next':''}" data-do="watch" data-who="${k}" data-price="watch"><i class="sweep"></i>${cam('scout')}${ic('watch',nx==='watch'?'#60e0ea':'')}<span class="w">Watch</span></span>`;
 const listenBtn=K.listen?'':`<span class="act ${nx==='listen'?'next':''}" data-do="listen" data-who="${k}" data-price="listen"><i class="sweep"></i>${cam('scout')}${ic('listen',nx==='listen'?'#60e0ea':'')}<span class="w">Listen</span></span>`;
 return `<div class="row focus" data-row="${k}"><div class="top"><div class="face ${k==='guard'?'bare':''}" ${face}></div><div><div class="nm">${P.name}</div><div class="why">${P.why}</div></div></div>
  <div class="learn">${watchBtn}${slotHtml('mood','Mood',K.watch?P.mood:null,null,fill==='watch')}</div>
  <div class="learn">${listenBtn}<span class="slots">${slotHtml('need','Need',K.listen?P.need:null,P.told,fill==='listen')}${slotHtml('nature','Nature',K.listen?P.nature:null,null,fill==='listen')}${slotHtml('ties','Ties',K.listen?P.ties:null,null,fill==='listen')}</span></div>
  <div class="ways">${Object.keys(WAYS).map(way).join('')}</div>
  <div class="price" data-p="${k}">${nx&&nx!=='call'?priceHtml(k,nx):priceHtml(k,'help')}</div></div>`;
}
function renderBoard(){let b=$('#board');if(!b)return;
 const order=Object.keys(PEOPLE);
 b.className='panel'+(help==='later'?' later':'')+(b.classList.contains('hide')?' hide':'');
 b.innerHTML=`<div class="head">${cam('vale')}<span>Take Vale alive</span><span class="dim" style="margin-left:auto;display:flex;gap:5px;align-items:center">${ic('p_time','#b0cdc7')}he leaves after 4:30</span></div>`
  +order.map((k,i)=>rowHtml(k,i)).join('')
  +`<div class="tip"><span class="hd">Try</span><span>${TIP[stateOf(focus)]}</span></div>`;
 hook(b);
}
function hook(b){
 b.querySelectorAll('[data-row]').forEach(r=>{const k=r.dataset.row;
  r.onmouseenter=()=>{worldLit(k,true);r.classList.add('lit')};r.onmouseleave=()=>{worldLit(k,false);r.classList.remove('lit')};
  r.onclick=e=>{if(e.target.closest('[data-do],[data-way]'))return;focus=k;renderBoard()}});
 b.querySelectorAll('[data-price]').forEach(a=>{const k=a.dataset.who,P=b.querySelector(`[data-p="${k}"]`);if(!P)return;const rest=P.innerHTML;
  a.onmouseenter=()=>P.innerHTML=priceHtml(k,a.dataset.price);a.onmouseleave=()=>P.innerHTML=rest});
 b.querySelectorAll('[data-do]').forEach(a=>a.onclick=e=>{e.stopPropagation();run(a.dataset.who,a.dataset.do,a)});
 b.querySelectorAll('[data-way]').forEach(a=>a.onclick=e=>{e.stopPropagation();if(a.classList.contains('shut'))return;run(a.dataset.who,a.dataset.way,a)});
 b.querySelectorAll('[data-focus]').forEach(a=>a.onclick=e=>{e.stopPropagation();focus=a.dataset.focus;renderBoard()});
}
function run(k,what,btn){ // the mock runs an act in 1.6 s; the game takes its priced seconds
 focus=k;const sw=btn.querySelector('.sweep');if(sw){sw.style.transition='width 1.6s linear';requestAnimationFrame(()=>sw.style.width='100%')}
 later(()=>land(k,what),sw?1650:300);
}
function land(k,what,o={}){const before=opened(k),K=know[k];
 if(what==='watch')K.watch=1;else if(what==='listen'){K.listen=1;K.watch=1}else if(what==='call'){callPop(k);return}
 else{const f=PEOPLE[k].fit[what];if(f==='ok'||f==='half')K.turned=1;else{popWorld(`${WAYS[what].w} refused`);return}}
 const now=opened(k),gained=now.filter(w=>!before.includes(w));
 fresh['fill_'+k]=what;gained.forEach(w=>fresh['way_'+k+'_'+w]=1);
 renderBoard();later(()=>{delete fresh['fill_'+k]},1000);
 if(gained.length&&!o.quiet&&onNew)onNew(k,gained);
 later(()=>{gained.forEach(w=>delete fresh['way_'+k+'_'+w]);renderBoard()},14000);
}
let onNew=null;
function popWorld(t){const p=el(`<div class="plate abs" style="left:880px;top:420px;z-index:9">${ic('refused')}${t}</div>`);stage.append(p);later(()=>p.remove(),1800)}
function callPop(k){const p=el(`<div class="plate abs" style="left:800px;top:420px;z-index:9;height:34px">${cam('fixer')}${ic('call')}Call ${PEOPLE[k].name}</div>`);stage.append(p);later(()=>p.remove(),2000)}

// ------------------------------------------------------------------ people in the world (real bearings at yaw 30: Inez on screen; the guard and driver off it, down right, by Vale's own arrow)
const WORLD={waitress:{x:1293,y:62,gr:[1293,134]},guard:{edge:[1502,1052]},driver:{edge:[1606,1052]}};
function worldMarks(show=['waitress','guard','driver'],popIt){
 const w=$('#world')||stage.appendChild(el('<div id="world" class="abs" style="inset:0;pointer-events:none"></div>'));
 show.forEach(k=>{const W=WORLD[k],P=PEOPLE[k];if($('#t_'+k))return;
  if(W.edge){w.append(el(`<div id="t_${k}" class="plate tag ${popIt?'pop':''}" style="left:${W.edge[0]-16}px;top:${W.edge[1]-16}px;padding:0 4px;height:34px">${cam(k)}<span style="width:0;height:0;border-top:7px solid #f7f4ec;border-left:5px solid transparent;border-right:5px solid transparent"></span></div>`))}
  else{w.append(el(`<div id="t_${k}" class="plate tag ${popIt?'pop':''}" style="right:${1920-W.x+18}px;top:${W.y-6}px;padding:0 10px 0 5px">${cam(k)}${P.name}</div>`));w.append(el(`<span class="tail tailR" style="left:${W.x-18}px;top:${W.y+3}px"></span>`))}
 });
}
function worldLit(k,on){const t=$('#t_'+k);if(!t)return;t.classList.toggle('lit',on);const W=WORLD[k];
 const g=$('#gr_'+k);if(on&&W.gr&&!g)$('#world')?.append(el(`<div id="gr_${k}" class="gring" style="left:${W.gr[0]-44}px;top:${W.gr[1]-26}px;width:88px;height:52px"></div>`));if(!on&&g)g.remove();
 if(W.edge)t.style.transform=on?'scale(1.25)':'';
}

// ------------------------------------------------------------------ tabs
const TABS={people:['People','10 + 8'],newact:['New action','8'],doing:['Over the head','6'],log:['The log','9'],seen:['Seen in the shot','checks']};
let tab=q.get('tab')||'people';
function shell(title,html){rail.innerHTML=`<div class="tabs">${Object.entries(TABS).map(([k,[n,t]])=>`<button data-t="${k}" class="${k===tab?'on':''}">${n} <span style="opacity:.6">${t}</span></button>`).join('')}</div><h1>${title}</h1>${html}`;
 rail.querySelectorAll('[data-t]').forEach(b=>b.onclick=()=>{tab=b.dataset.t;go()})}
const opt=(group,items,cur)=>`<div class="opts">${items.map(([v,l])=>`<button data-${group}="${v}" class="${v===cur?'on':''}">${l}</button>`).join('')}</div>`;
const bg=n=>{const d=el('<div class="bg"></div>');d.style.backgroundImage=`url(${FRAMES[n]})`;stage.append(d)};

// ------------------------------------------------------------------ 1. people
function people(){clearAll();bg('map');onNew=(k,g)=>{later(()=>calloutAt(k,g,false),250)};
 const b=el('<div id="board" class="panel"></div>');stage.append(b);reset();
 const st=q.get('state');if(st==='watched')know.waitress.watch=1;if(st==='heard'){know.waitress.watch=know.waitress.listen=1}if(st==='turned'){know.waitress.watch=know.waitress.listen=know.waitress.turned=1}
 shell('Who matters, and what to try',`
 <p class="small">Sky: "it's so understated." In the build it starts <b>closed</b>, behind a 103 px plate of four dim discs, so a new player never sees it. "?" says nothing, and the step chip takes two clicks.</p>
 <h2>Help</h2>${opt('help',[['early','Early mission'],['later','Later'],['off','Off']],help)}
 <h2>How it opens</h2>${opt('dock',[['a','A · Stays big'],['b','B · Opens big, docks small']],dock)}
 <div class="pick">Pick: <b>A</b>. Play starts at once and it stays where you need it, shrinking as you learn.</div>
 <div class="opts" style="margin-top:6px"><button id="replay">Replay the opening</button></div>
 <h2>Try it</h2>
 <ul><li><b>Click Watch or Listen.</b> Mocked at 1.6 s. Slots fill, ways open and it says so.</li><li><b>Hover a person.</b> They light up in the street, or at the edge if off screen. Their face pans there in the game.</li><li><b>Hover a button</b> for the price. Click a row to open it.</li></ul>
 <h2>How it reads</h2>
 <ul><li><b>Who matters:</b> face, name, why, all from the mission's roles and ties.</li><li><b>Known:</b> solid. <b>Told:</b> dashed. <b>Hidden:</b> dotted, and named.</li><li><b>How to fill it:</b> the button beside the slot.</li><li><b>What to try:</b> every way, locks and all. Nothing says which is right.</li></ul>
 <h2>Early to later</h2>
 <p class="small">No mission numbers. Each piece retires for the profile once used, as hints do today: why lines and tips after a Watch, Listen and turn have landed; the roll call once a mission's been finished. A later mission with a new kind of act brings only that back. Options keeps Early, Later, Off.</p>
 `);
 rail.querySelectorAll('[data-help]').forEach(x=>x.onclick=()=>{help=x.dataset.help;q.delete('state');people()});
 rail.querySelectorAll('[data-dock]').forEach(x=>x.onclick=()=>{dock=x.dataset.dock;people()});
 $('#replay').onclick=()=>people();
 if(help==='off'){b.style.display='none';return}
 renderBoard();
 if(help==='later'){worldMarks(['waitress','guard','driver'],false);$$('.tag,.tail').forEach(t=>t.style.opacity=0);return}
 if(dock==='b')openDock(b);else openStay(b);
}
const $$=(s)=>[...document.querySelectorAll(s)];
function openStay(b){ // the roll call: the panel comes in, each row lights in turn and their mark comes up in the street
 if(q.get('still')){worldMarks();return}
 b.classList.add('hide');later(()=>{b.classList.remove('hide');renderBoard()},350);
 ['waitress','driver','guard'].forEach((k,i)=>later(()=>{worldMarks([k],true);const r=$(`[data-row="${k}"]`);r?.classList.add('lit');later(()=>$(`[data-row="${k}"]`)?.classList.remove('lit'),650)},1000+i*700));
}
function openDock(b){ // B: the same panel, large over the street with the clock held; a click or 7 s sends it to the corner
 worldMarks();const big='translate(-500px,120px) scale(1.42)';b.style.transition='none';b.style.transform=big;
 const sc=el('<div id="scrim" class="abs" style="inset:0;background:rgba(11,22,21,.42);transition:opacity .4s;z-index:1"></div>');stage.insertBefore(sc,b);b.style.zIndex=2;
 const go=()=>{b.style.transition='';b.style.transform='';sc.style.opacity=0;later(()=>sc.remove(),450);help='later';renderBoard();stage.onclick=null;help='early'};
 if(q.get('docked')){go();return}
 later(go,7000);later(()=>{stage.onclick=e=>{if(!e.target.closest('[data-do],[data-way]'))go()}},50);
}

// ------------------------------------------------------------------ 2. new action
let naMode='b',naView='3p';
const NA={'3p':{bg:'cafe3p',raw:'cafe3p_raw',inez:[962,600],scout:[955,672]},map:{bg:'cafe',raw:'cafe_raw',inez:[962,458],scout:[1012,508]}};
function calloutAt(k,gained,fromMock){ // A: docked at the row, its tail on the row
 const r=$(`[data-row="${k}"]`);if(!r)return;const rb=r.getBoundingClientRect(),sb=stage.getBoundingClientRect(),s=sb.width/1920;
 $('#callout')?.remove();
 const c=el(`<div id="callout" class="panel callout" style="left:${(rb.left-sb.left)/s-292-44}px;top:${Math.max(110,(rb.top-sb.top)/s-4)}px"><span class="tailr"></span>${newHead(k,gained)}<div class="pl">${gained.map(w=>wayBtn(k,w)).join('')}</div></div>`);
 stage.append(c);c.querySelectorAll('[data-w]').forEach(a=>a.onclick=()=>{c.remove();land(k,a.dataset.w)});later(()=>c.remove(),8000);
}
const verbWord=w=>w==='call'?'Call':WAYS[w].w;
const wayBtn=(k,w)=>`<span class="act next" data-w="${w}">${cam('fixer')}${ic(w,'#60e0ea')}<span class="w">${verbWord(w)}</span></span>`;
const newHead=(k,g)=>`<div class="newhd">${g.length>1?'New actions':'New action'}<span class="who">· ${PEOPLE[k].name}</span></div>`;
function newact(){clearAll();onNew=null;const V=NA[naView];const built=naMode==='built';bg(built?V.raw:V.bg);reset();know.waitress.watch=1;focus='waitress';
 const b=el('<div id="board" class="panel"></div>');
 if(!built){stage.append(b);renderBoard()}
 shell('When you learn something',`
 <p class="small">Sky: "it should say clearly a new action is available."</p>
 <p class="small"><b>Why he missed it:</b> the build has a chip with the facts and small verb pills, but the third-person view hides it (same moment, 1:00: <b>As built</b>, Third person vs Map). And it never says "new".</p>
 <h2>View</h2>${opt('view',[['3p','Third person'],['map','Map']],naView)}
 <h2>The moment</h2>${opt('mode',[['built','As built'],['a','A · At the panel'],['b','B · On the person, then the panel']],naMode)}
 <div class="pick">Pick: <b>B</b>. Your eye is on the street when the act lands. It says so there, then flies into the panel, which teaches where it lives. Off screen, it starts at the panel (A).</div>
 <div class="opts" style="margin-top:6px"><button id="replay">Replay</button></div>
 ${naView==='map'&&naMode!=='built'?'<p class="small">On the map this frame has the old chip baked in, so a chip stays on her here. Third person shows it clean.</p>':''}
 <h2>The rules</h2>
 <ul><li>"New action" comes from the engine's list of what opened, never a script. Nothing opened: no banner, just the facts.</li><li>The buttons work: click one to give the order, price on hover.</li><li>Opened ways stay lit for 90 s, in the panel and on the ring.</li><li>Inez: Watch opens Pay. Listen opens Help and Pressure. Turning her opens Call.</li></ul>
 <h2>Meeting a cutscene</h2>
 <p class="small">Today a scene plays when the act <b>starts</b> and the result comes at landing. Sky wants the scene to explain the whole talk. If it tells you her debt and the slot only fills 30 s later, they disagree. Best: the telling part plays at landing, then this moment plays as the cut ends. For Sky, with Fable.</p>`);
 rail.querySelectorAll('[data-view]').forEach(x=>x.onclick=()=>{naView=x.dataset.view;newact()});
 rail.querySelectorAll('[data-mode]').forEach(x=>x.onclick=()=>{naMode=x.dataset.mode;newact()});$('#replay').onclick=()=>newact();
 if(built)return;
 const ps=plate({who:'scout',verb:'Listening',icon:'listen',tgt:'waitress',secs:3,x:V.scout[0],y:V.scout[1],frac:.9,sel:true});stage.append(ps);
 let s=3;const cd=setInterval(()=>{s--;const t=ps.querySelector('.sc');if(t)t.textContent=Math.max(s,0)+' s'},700);timers.push(cd);
 const landAt=+q.get('land')||2100;
 later(()=>{clearInterval(cd);ps.remove();
  if(naMode==='a'){onNew=(k,g)=>{later(()=>calloutAt(k,g),120);const P=el(`<div class="pulse" style="left:${V.inez[0]-50}px;top:${V.inez[1]+(naView==='map'?64:190)}px"></div>`);stage.append(P);later(()=>P.remove(),6000)};land('waitress','listen');return}
  // B: the chip over her, then it flies home
  const P=PEOPLE.waitress,gained=['help','pressure'];
  const c=el(`<div class="panel chip" style="left:${V.inez[0]-165}px;top:${V.inez[1]-192}px"><div class="tp">${cam('waitress')}<div>${[P.need,P.nature,P.ties].map(f=>`<div class="ln">${ic(f.i)}${f.w}</div>`).join('')}</div></div><div class="hr"></div>${newHead('waitress',gained)}<div class="pl">${gained.map(w=>wayBtn('waitress',w)).join('')}</div></div>`);
  const T=el(`<span class="tail tailD" style="left:${V.inez[0]-8}px;top:${V.inez[1]-28}px;z-index:6"></span>`);stage.append(c,T);
  c.querySelectorAll('[data-w]').forEach(a=>a.onclick=()=>{c.remove();T.remove();land('waitress',a.dataset.w,{quiet:true})});
  later(()=>{const sb=stage.getBoundingClientRect(),s=sb.width/1920,cr=[...c.querySelectorAll('[data-w]')].map(a=>a.getBoundingClientRect());
   const fl=gained.map((w,i)=>{const r=cr[i];const f=el(`<span class="act next fly" style="left:${(r.left-sb.left)/s}px;top:${(r.top-sb.top)/s}px">${ic(w,'#60e0ea')}<span class="w">${verbWord(w)}</span></span>`);stage.append(f);return f});
   if(naView!=='map'){c.style.transition='opacity .25s';c.style.opacity=0;T.remove()}know.waitress.listen=1;renderBoard();
   const dest=gained.map(w=>$(`[data-way="${w}"]`).getBoundingClientRect());
   requestAnimationFrame(()=>requestAnimationFrame(()=>fl.forEach((f,i)=>{f.style.left=(dest[i].left-sb.left)/s+'px';f.style.top=(dest[i].top-sb.top)/s+'px'})));
   later(()=>{fl.forEach(f=>f.remove());if(naView!=='map')c.remove();gained.forEach(w=>fresh['way_waitress_'+w]=1);fresh.fill_waitress='listen';renderBoard();later(()=>delete fresh.fill_waitress,1000)},800);
  },+q.get('hold')||3600);
 },landAt);
}

// ------------------------------------------------------------------ 3. over the head
function plate({who,verb,icon,tgt,secs,x,y,frac,going,sel,short,eye,thing}){
 const face=tgt?cam(tgt):'';const showWord=!short||sel;
 const d=el(`<div class="doing ${going?'going':''} ${sel?'sel':''}" style="left:${x}px;top:${y}px"><div class="plate"><span class="d">${ic(going?'s_moving':icon)}</span>${showWord?`<span>${verb}</span>`:''}${going&&!showWord?ic(icon):''}${face}<span class="sc">${secs} s</span>${eye?`<span class="eye">${ic(eye)}</span>`:''}${going?'':`<span class="ud"><i style="width:${(frac||0)*100}%"></i></span>`}</div><span class="tl"></span></div>`);
 return d;
}
let doView='map',doMode='a';
const SCN={map:[
  {who:'scout',verb:'Listening',icon:'listen',tgt:'waitress',secs:12,x:805,y:524,frac:.6,sel:true},
  {who:'fixer',verb:'To help',icon:'help',tgt:'waitress',secs:21,x:668,y:622,going:true},
  {who:'tech',verb:'Looping',icon:'loop',secs:9,x:905,y:628,frac:.55},
 ],third:[
  {who:'scout',verb:'Listening',icon:'listen',tgt:'waitress',secs:12,x:958,y:632,frac:.6,sel:true,eye:'eye_unk'},
  {who:'fixer',verb:'To help',icon:'help',tgt:'waitress',secs:21,x:112,y:845,going:true},
  {who:'tech',verb:'Looping',icon:'loop',secs:9,x:812,y:950,frac:.55},
 ]};
function doing(){clearAll();onNew=null;bg(doView==='map'?'map':'third');
 shell('What each agent is doing',`
 <p class="small">Sky: "what the agent is doing above their [head]… performing an action or on their way to do one."</p>
 <p class="small"><b>083 only gave:</b> a 27 px icon and seconds. Nothing says on whom. On the way it's the same icon with a dashed ring and no time. Beside a known person it steps down to the agent's feet, so it reads as someone else's.</p>
 <h2>View</h2>${opt('dv',[['map','Map'],['third','Third person']],doView)}
 <h2>Mark</h2>${opt('dm',[['built','As built'],['a','A · Word, face, seconds'],['b','B · Face, seconds; word when selected']],doMode)}
 <div class="pick">Pick: <b>A</b> for early missions, retiring to <b>B</b> with the rest of the help. Sky's complaint is that icons alone don't say it.</div>
 <h2>Doing, or on the way</h2>
 <ul><li><b>Doing:</b> "Listening", underline draining, filled ring.</li><li><b>On the way:</b> footprints in a turning dashed ring, "To help", no underline, seconds until it arrives (walk included).</li><li>A plain walk has no mark. Its ring on the ground says it.</li></ul>
 <h2>Fewer things over a head</h2>
 <ul><li>The plate's tail <b>is</b> the selected arrow: chrome, or the turquoise arrow when selected. One object, not a stack.</li><li>In third person the "?" eye joins the plate's end. Today it floats alone and reads as a question for you.</li><li>It never moves to the feet. It stacks above a person's own icons.</li></ul>
 <p class="small">The figures are stills from the build. Only the marks move here.</p>`);
 rail.querySelectorAll('[data-dv]').forEach(x=>x.onclick=()=>{doView=x.dataset.dv;doing()});
 rail.querySelectorAll('[data-dm]').forEach(x=>x.onclick=()=>{doMode=x.dataset.dm;doing()});
 const L=SCN[doView];
 if(doMode==='built'){ // the as-built mark: a disc, seconds, the arrow under it
  L.forEach(a=>{const t=a.going?'':`<span class="plate">${a.secs} s</span>`;
   const e=el(`<div class="built" style="left:${a.x-18}px;top:${a.y-(a.sel?46:30)}px"><span class="disc" ${a.going?'style="outline:1.6px dashed rgba(255,255,255,.7);outline-offset:-5px;border-radius:50%"':''}>${ic(a.icon)}</span>${t}</div>`);stage.append(e);
   if(a.sel){const c=el(`<span class="chev" style="left:${a.x-11}px;top:${a.y-8}px"></span>`);stage.append(c)}
  });return}
 const els=L.map(a=>{const p=plate({...a,short:doMode==='b'});stage.append(p);return{a,p,s:a.secs,f:a.frac||0}});
 tick=setInterval(()=>els.forEach(o=>{o.s=o.s<=1?o.a.secs:o.s-1;o.p.querySelector('.sc').textContent=o.s+' s';const i=o.p.querySelector('.ud i');if(i)i.style.width=(1-o.s/(o.a.secs/(1-o.f)))*100+'%'}),1000);
}

// ------------------------------------------------------------------ 4. the log
let logMode='a';
function log(){clearAll();onNew=null;bg('map');
 shell('What the log is for',`
 <p class="small">Sky: "way too busy, and not helpful enough."</p>
 <div class="pick"><b>It answers one question: what's changed that I can do something about?</b></div>
 <h2>Log</h2>${opt('lm',[['built','As built'],['a','A · Now, then Before'],['b','B · Now only']],logMode)}
 <div class="pick">Pick: <b>A</b>. A keeps a short look back for "what did I miss?"; B is calmer but loses it.</div>
 <h2>What leaves</h2>
 <ul><li>Filters, Read all, unread dots, the "Scenes · History" title.</li><li>Two-line entries: one line, what then so what.</li><li>Orders you gave yourself ("Camera looped. The Tech is free to go": the marker and roster say it).</li><li>Speech. It belongs to the scene.</li><li>Facts about people: they fill the panel. The line points there.</li></ul>
 <h2>What each line does</h2>
 <ul><li><b>Now:</b> still true, with a count where there is one. Goes when it stops being true.</li><li><b>Before:</b> dim, the last few, the wheel scrolls back.</li><li>Click a line: pans there, or opens that person's row.</li><li>Closed by default, on the notes key with its dot. Story beats still show for 4 s.</li></ul>`);
 rail.querySelectorAll('[data-lm]').forEach(x=>x.onclick=()=>{logMode=x.dataset.lm;log()});
 if(logMode==='built'){const i=el(`<img class="abs" src="${FRAMES.log_built}" style="right:10px;bottom:56px;width:410px">`);stage.append(i);return}
 let left=72,gv=24;
 const nowLines=()=>[
  {id:'cam',d:'sawyou',t:'Guard saw the Fixer run',so:'walk near him',tm:''},
  {id:'riv',d:'car',t:'Rival car at the pickup',so:'they want Vale too',tm:''},
  {id:'loop',d:'loop',t:'Camera looped',so:'',tm:fmt2(left)},
 ];
 const old=[{d:'mood_uneasy',c:'waitress',t:'Inez is uneasy · Help is harder'},{d:'cash',c:'driver',t:'Driver needs cash · Listen for more'}];
 const fmt2=s=>`${Math.floor(s/60)}:${String(s%60).padStart(2,'0')}`;
 if(logMode==='a'){
  const p=el(`<div id="log" class="panel"><div class="lhd"><span>Now</span></div><div id="now"></div><div class="lhd" style="margin-top:6px;border-top:1px solid #99784d;padding-top:5px"><span>Before</span></div>${old.map(o=>`<div class="ln2 old"><span class="dd">${ic(o.d)}</span>${o.c?cam(o.c):''}<span>${o.t}</span></div>`).join('')}</div>`);stage.append(p);
  const draw=()=>{$('#now').innerHTML=nowLines().map(l=>`<div class="ln2"><span class="dd">${ic(l.d)}</span><span>${l.t}${l.so?`<span class="dim"> · ${l.so}</span>`:''}</span><span class="tm">${l.tm}</span></div>`).join('')};draw();tick=setInterval(()=>{left=left<=1?72:left-1;draw()},1000);
 }else{
  const p=el(`<div class="nowstack" id="now"></div>`);stage.append(p);
  const draw=()=>{p.innerHTML=nowLines().map(l=>`<div class="plate"><span class="dd">${ic(l.d)}</span><span>${l.t}${l.so?`<span class="dim"> · ${l.so}</span>`:''}</span>${l.tm?`<span class="tq">${l.tm}</span>`:''}</div>`).join('')};draw();tick=setInterval(()=>{left=left<=1?72:left-1;draw()},1000);
 }
}

// ------------------------------------------------------------------ 5. seen
function seen(){clearAll();onNew=null;
 stage.append(el(`<div id="seen"><div class="seengrid">
  <div><h3>The clock: a real bug, every build</h3><p>The next-mark arrow sits on the clock's last digit, so the seconds can't be read. Not HUD scale: it's in the 1080p capture too.</p><span class="tagc">Sky's shot</span><img src="${FRAMES.c_clock_sky}"><span class="tagc">Build, 1080p capture</span><img src="${FRAMES.c_clock_build}"></div>
  <div><h3>A "?" over your own agent</h3><p>That's the "could a watcher see this?" eye. Alone over the head, it reads as a question for the player. It joins the plate in the new mark.</p><img src="${FRAMES.c_eye}" style="height:300px"></div>
  <div><h3>The guard has no face</h3><p>Inez and the driver are painted. He's an amber shield. With the panel leading, he needs a portrait.</p><img src="${FRAMES.c_guard}"></div>
  <div><h3>The log's small labels</h3><p>"Read all" rides high in its plate, and the filter words are soft on their chrome pills. These go with the redesign.</p><img src="${FRAMES.c_loghead}"></div>
 </div></div>`));
 shell('Seen in the shot',`<p class="small">Checks, nothing for Sky to pick. His shot is a crop, so nothing about screen edges or HUD scale is measured from it.</p>
 <h2>Cause of the clock</h2><p class="small">Brief 080's close underline was added as a child of the clock's label. A text element with a child stops measuring its text, so it takes no width and the next items slide over it. In <code>HudToolkit.cs</code> line 261. One line for Latch: hang the bar off a wrapper.</p>`);
}

// ------------------------------------------------------------------ boot
function go(){({people,newact,doing,log,seen})[tab]()}
function fit(){const L=$('#left'),s=Math.min(L.clientWidth/1920,L.clientHeight/1080);stage.style.transform=`scale(${s})`;const B=$('#stagebox');B.style.width=1920*s+'px';B.style.height=1080*s+'px'}
addEventListener('resize',fit);fit();
if(q.get('help'))help=q.get('help');if(q.get('dock'))dock=q.get('dock');if(q.get('mode'))naMode=q.get('mode');if(q.get('view'))naView=q.get('view');if(q.get('dm'))doMode=q.get('dm');if(q.get('dv'))doView=q.get('dv');if(q.get('lm'))logMode=q.get('lm');
go();
if(q.get('do')){const[k,w]=q.get('do').split(':');later(()=>{focus=k;land(k,w)},+q.get('doAt')||600)}
