/** Brief 102 (engine 0.51.0, Sky's calls on the guard, 2026-11-09; Fable): the rows of the guard's end of the call, told as the engine and the host play them. They are
 *  hand rows (briefs/102/fable/guard-call.json), so only a hand run has them as items; no saved run's text has them, so every older run rebuilds as it ran.
 *  A row `about` the guard is his own call (HostCall.SummonAbout: the one called is the one who'd leave his post); summon-unanswered is Inez's call to a guard who is down. */
type Row = { event: string; speaker: string; outcome?: string; about?: string };

/** His own call refused, as the engine checks it from his end (GameCall.cs SummonRefusal; the one called and the one who leaves are both him, so his memory of the cell's
 *  violence is call-saw-violence, which plays nothing, and no-visit never comes from his end). */
const REFUSED: Record<string, string> = {
  'summon-busy': 'Inez is down (knocked out, tied or dead; he may not know it), or he isn\'t free: just come round and walking back, in a fight, or away from his door on an earlier visit (walking over, with her at the café, or walking back), and every line holds for each',
  'summon-alert': 'security is searching and staff stay at their posts; he is at his door',
  'summon-came-once': 'he has already been off his door once this shift, on her call or his own; he is back at his door',
  'summon-too-far': 'he is at his door, and her post is a long walk from it, too far for a visit',
  'summon-off-street': 'she is off the street, indoors, with no post on it; he is at his door',
};

/** What a hand run is told about a row of the guard's end, or null for any other row. `temp` is the temperament clause the bark briefs give a row with one. */
export function guard102(r: Row, temp: string): string | null {
  if (r.event === 'confirm' && r.speaker === 'guard')
    return `The Fixer calls the turned guard himself at his door (brief 102, option 1): he says yes, he'll leave it for a while. Said to the Fixer. Paid, helped or pressured, a Mercenary takes any of the three; a Loyal guard only Help. He goes to Inez at her post, which the Fixer may not know of.${temp}`;
  if (r.event === 'summon-unanswered')
    return 'Inez\'s call to the guard finds him down (knocked out, tied or dead) and she doesn\'t know it: she tries him and nobody answers, and she tells the Fixer on the telephone. Facts, never said: why he is silent. Nothing is spent.';
  if (r.about !== 'guard') return null;
  if (r.event === 'summon-refused' && r.speaker === 'guard')
    return `The Fixer calls the turned guard himself (brief 102, option 1), and the call can't work now: ${REFUSED[r.outcome ?? ''] ?? r.outcome}. He answers the Fixer, never Inez. Nothing is spent.`;
  if (r.event === 'summon-refused' && r.speaker === 'waitress')
    return `The Fixer called the guard himself, and he has just said no (${REFUSED[r.outcome ?? ''] ?? r.outcome}). Inez hears only his side, standing within earshot of him; she knows nothing of the call or who made it. Nothing is spent.`;
  if (r.event === 'guard-at-cafe')
    return 'The guard has walked to Inez at her post because the Fixer called him (brief 102, option 1): she never sent for him and may know nothing of the cell. The Office door is unguarded while he stays.';
  return null;
}
