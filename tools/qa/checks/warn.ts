// Brief 060 §2 #9 (Suture): Q-WARN, the sim half. Every fail has a warning the player could see, early enough: the log line that warns of
// it lands at least `lead` seconds before the run ends, and that line reaches the feed as a beat (text.json cards, fate "beat"). The host
// half (Q-WARN-HUD: the seal countdown on screen in every frame from the warning to the end) waits for Latch's -bot row.
// Fails the player causes with one act (an entry or a take seen, Vale or the driver shot, the car wrecked) are warned by that act's
// preview (its price shows "fails"): those are counted as unchecked here and wait for Q-PREVIEW.
import type { Ctx, Finding, LogLine, Play } from '../types';

type Warn = { what: string; lines: (p: Play) => LogLine[]; use: 'first' | 'last'; leadS: (ctx: Ctx) => number; feed?: string };
const by = (code: string) => (p: Play) => p.log.filter(l => l.code === code);
const enforcerHits = (p: Play) => p.log.filter(l => l.code.startsWith('enforcer.hit.'));

/** The warning for a fail code (and, where it differs, for the cause's why). [S] leads: 30 s for a clock the player must act on, 5 s from
 *  the first hit for a downed Enforcer (his health shows from the first hit). */
export function warningFor(fail: string, why: string): Warn | null {
  // the lines whose card states the seal time: seal.starts ("Police seal the district at {clock}"), and fight.loud (its result says the same)
  if (fail === 'sealed') return { what: 'the seal clock (seal.starts or fight.loud)', lines: p => p.log.filter(l => l.code === 'seal.starts' || l.code === 'fight.loud'), use: 'last', leadS: c => c.rules.warnLeadS, feed: 'seal.starts' };
  if (fail === 'enforcer-down' || (fail === 'rivals-reach-vale' && why === 'downed-enforcer')) return { what: 'the first hit on the Enforcer', lines: enforcerHits, use: 'first', leadS: () => 5 };
  if (fail === 'rivals-reach-vale') return { what: 'the rivals arriving (rivals.arrive)', lines: by('rivals.arrive'), use: 'first', leadS: c => c.rules.warnLeadS, feed: 'rivals.arrive' };
  if (fail === 'vale-leaves') return { what: 'Vale coming out (vale.exits)', lines: by('vale.exits'), use: 'first', leadS: c => c.rules.warnLeadS, feed: 'vale.exits' };
  return null;
}
export const PREVIEW_WARNED = new Set(['entry-seen-guard', 'entry-seen-camera', 'entry-seen-patrol', 'abduction-seen-camera', 'vale-dead', 'driver-shot', 'getaway-lost']);

export function qWarn(p: Play, ctx: Ctx): Finding[] {
  if (p.error || p.outcome !== 'failure' || !p.fail) return [];
  const w = warningFor(p.fail, p.cause?.why ?? '');
  if (!w) {
    if (PREVIEW_WARNED.has(p.fail)) return [];
    return [{ check: 'Q-WARN', severity: 'info', key: 'Q-WARN:no-rule:' + p.fail, title: `No warning rule for fail ${p.fail}`, expected: 'a rule in tools/qa/checks/warn.ts', observed: p.fail, owner: 'Suture' }];
  }
  const out: Finding[] = [];
  const ls = w.lines(p).filter(l => l.tick <= p.tick), lead = w.leadS(ctx);
  if (!ls.length) {
    const set = p.fail === 'sealed' ? setBy(p) : '';
    out.push({ check: 'Q-WARN', severity: 'S2', key: `Q-WARN:none:${p.fail}`, title: `Fail ${p.fail} came with no warning`, expected: `${w.what} before the end`, observed: `no warning line in the log${set}`, tick: p.tick, owner: 'Keel' });
    return out;
  }
  const at = w.use === 'first' ? ls[0]! : ls[ls.length - 1]!, gotS = (p.tick - at.tick) / 10;
  const owner = p.fail === 'enforcer-down' || p.cause?.why === 'downed-enforcer' ? 'Motive' : 'Keel'; // how fast a cell member falls is fight balance (§06)
  if (gotS < lead) out.push({
    check: 'Q-WARN', severity: 'S2', key: `Q-WARN:late:${p.fail}`, title: `Fail ${p.fail} is warned less than ${lead} s ahead`,
    expected: `${w.what} at least ${lead} s before the end`, observed: `${w.what} at ${clock(at.tick)}, the run ended ${clock(p.tick)} (${gotS.toFixed(1)} s)`, tick: at.tick, owner,
  });
  if (w.feed && !ctx.text.beats.has(w.feed)) out.push({ check: 'Q-WARN', severity: 'S2', key: `Q-WARN:feed:${w.feed}`, title: `The warning ${w.feed} has no feed beat`, expected: `text.json cards.items["${w.feed}"].fate = beat`, observed: 'not a beat (the player is not told)', tick: at.tick, owner: 'Fable' });
  return out;
}

/** What set the seal clock that ended the run: the QA host's last lockdown move and the log lines at that tick. */
function setBy(p: Play): string {
  const m = p.qa?.lockdown?.[p.qa.lockdown.length - 1]; if (!m) return '';
  const near = p.log.filter(l => l.tick <= m.tick && l.tick >= m.tick - 10).map(l => l.code).filter(c => !/^(shot|ai|crowd)\./.test(c));
  return `; the seal was set to ${clock(m.at)} at ${clock(m.tick)}, beside ${[...new Set(near)].join(', ') || 'no log line'}`;
}

export const clock = (tick: number) => { const s = Math.floor(tick / 10); return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}.${tick % 10}`; };
