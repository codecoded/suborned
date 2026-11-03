// The phone and tablet side of the lab (Sky, 2026-10-24: "make the controls lab mobile friendly").
// It sets body.m (a phone-sized or touch screen: panels fold, buttons are 44 px) and body.touch (a coarse pointer: the dock, 2x pixel cap),
// builds the dock of buttons for the keyboard-only things, and keeps the panels collapsible. Lab styling on purpose; nothing here is the game's HUD.
// The buttons press the same keys as a keyboard would (a synthetic keydown and keyup), so a key and its button can never differ.
import { G, say } from './state.js';

const $ = (id) => document.getElementById(id);
const key = (code, shift = false) => { for (const type of ['keydown', 'keyup']) window.dispatchEvent(new KeyboardEvent(type, { code, key: code.replace(/^(Key|Digit)/, '').toLowerCase(), shiftKey: shift, bubbles: true, cancelable: true })); };

// [data-tb, label, class, code or null]
const DOCK = [
  ['key', '↺ Q', 'cmd', 'KeyQ', 'Turn the camera a quarter left'], ['key', 'E ↻', 'cmd', 'KeyE', 'Turn the camera a quarter right'],
  ['key', '3rd', 'cmd', 'KeyT', 'Third person (T)'], ['key', 'Back', 'tp', 'Escape', 'Back to the commander view'],
  ['key', 'Follow', 'cmd', 'KeyC', 'Camera follows the lead (C)'], ['key', 'Act', 'tp', 'KeyE', 'Do the prompted act (E)'],
  ['key', 'Pause', '', 'Space', 'Pause or play (Space)'], ['key', 'Stop', '', 'KeyX', 'Stop the selected agents (X)'],
  ['key', 'Tab', 'cmd', 'Tab', 'Step through the people under your last touch (Tab)'], ['firemode', 'Fire', 'cmd', null, 'Fire mode: hold on a person or car, let go to fire'],
  ['firehold', 'Hold to fire', 'tp big', null, 'Hold to fire at the nearest hostile ahead'],
];

export function initMobile() {
  const q = new URLSearchParams(location.search), b = document.body;
  // a phone or tablet: a coarse primary pointer, or an Apple or Android device with several touch points (an iPad with a keyboard says it has a fine pointer)
  const coarse = matchMedia('(pointer: coarse)'), device = navigator.maxTouchPoints > 1 && /Mac|iPhone|iPad|Android/.test(navigator.userAgent), touch = q.get('touch') === '1' || (q.get('touch') !== '0' && (coarse.matches || device));
  const apply = () => { b.classList.toggle('touch', touch); b.classList.toggle('m', touch || q.get('m') === '1' || innerWidth < 820); };
  apply(); addEventListener('resize', apply); addEventListener('orientationchange', apply);

  // the menu (presets, settings, a short touch legend) folds into one button
  const top = $('top'), mk = document.createElement('button'); mk.id = 'menukey'; mk.textContent = '☰ Menu'; top.prepend(mk);
  const help = document.createElement('div'); help.id = 'touchhelp';
  help.innerHTML = '<b>Touch</b> tap: select, walk, or open a thing\'s ring · double-tap the ground: run · press and hold a thing: do it, lift to go · drag: pan · pinch: zoom · twist two fingers: turn the camera · Fire mode, then hold on a person to fire · portrait drag onto a thing: that agent goes<br><b>Third person</b> drag on the left: walk (far edge runs) · drag on the right: look · Hold to fire';
  top.append(help);
  mk.addEventListener('click', () => { const on = b.classList.toggle('menu-open'); mk.textContent = on ? '✕ Close' : '☰ Menu'; if (!on) b.classList.remove('opts-open'); });

  // the dock
  const dock = $('dock'); dock.innerHTML = '<button id="dockkey" data-tb="dock">Controls ▾</button>' + DOCK.map(([kind, label, cls, code, tip]) => `<button data-tb="${kind}" ${code ? `data-code="${code}"` : ''} class="${cls}" title="${tip}" ${kind === 'firehold' ? 'id="tpfire"' : ''}>${label}</button>`).join('');
  dock.addEventListener('click', (e) => {
    const t = e.target.closest('button'); if (!t) return;
    if (t.dataset.tb === 'dock') { const off = b.classList.toggle('dock-off'); t.textContent = off ? 'Controls ▴' : 'Controls ▾'; }
    else if (t.dataset.tb === 'key') key(t.dataset.code);
    else if (t.dataset.tb === 'firemode') { G.touchFire = !G.touchFire; say(G.touchFire ? 'Fire mode on · hold on a person or car, let go to fire' : 'Fire mode off', G.touchFire ? 'warn' : ''); }
    t.blur();
  });
  dock.addEventListener('pointerdown', (e) => e.stopPropagation());

  // no page zoom, scroll or bounce under the fingers (iOS ignores user-scalable)
  for (const t of ['gesturestart', 'gesturechange', 'gestureend']) document.addEventListener(t, (e) => e.preventDefault());
  document.addEventListener('touchmove', (e) => { if (e.touches.length > 1 || !e.target.closest?.('#top, #opts, #hist')) e.preventDefault(); }, { passive: false });
  return { touch, get m() { return b.classList.contains('m'); } };
}

// per frame: the dock follows the state (view, pause, follow, fire mode)
const last = {};
export function syncMobile() {
  const b = document.body;
  const set = (k, v, fn) => { if (last[k] !== v) { last[k] = v; fn(v); } };
  set('view', G.view, (v) => (b.dataset.view = v));
  set('pause', G.paused, (v) => { const el = document.querySelector('#dock [data-code="Space"]'); if (el) { el.textContent = v ? 'Play' : 'Pause'; el.classList.toggle('on', v); } });
  set('follow', G.camFollow, (v) => document.querySelector('#dock [data-code="KeyC"]')?.classList.toggle('on', v));
  set('fire', !!G.touchFire, (v) => document.querySelector('#dock [data-tb="firemode"]')?.classList.toggle('on', v));
  set('armed', !!G.touchFire && G.view === 'cmd', (v) => b.classList.toggle('fire-armed', v));
}
