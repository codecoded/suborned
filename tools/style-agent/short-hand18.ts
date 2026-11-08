/** Guide /18 (brief 102, Fable 2026-11-08): the rows of the two hand-written families told as the engine plays them, read from the sim and the host before writing
 *  their lines by hand. The /4–/17 briefs told the guard's radio report as "staff radio the desk" (so the judge proposed "Somebody's down out front"), and four of the
 *  summons' refusals by the code's name rather than by what the engine checks. */

/** report-begins/guard/radio: the host speaks it only for the guard's attention report (HostActivity.cs: attention.reporting by the guard), and the engine makes that report
 *  for one of the four standing about or running in his sight too long (GameAttention.cs AttentionStep, "reports the X loitering"); never for a body, a takedown or a fight. */
export const RADIO_18 = 'Say: the guard has had one of the cell in his sight too long, standing about or running (any of the four, the Scout a woman), and he radios the desk about them '
  + 'from where he stands: what he says as he starts. He may never have met them; he sees only what they are doing. Facts, never said: the report travels to the police, and the HUD '
  + 'shows it on its way. It plays for nothing else: never a body, a takedown or a fight.';

/** A refusal as the engine checks it (sim GameOrders.cs SummonRefusal, in its order): a busy guard is down, held or engaged, or not at his door (already on a visit); from
 *  alert on he is at his door; too far is the walk from his door to her post (summon-walk-max-s); saw violence is a memory of the cell's gunfire, violence or a drawn weapon;
 *  alert is above summon-alert-max (1: a search or a lockdown, when "security staff stay at their posts"). Came once and off the street read as at /17. */
export const SUMMON_18: Record<string, string> = {
  'summon-busy': 'he isn\'t free: he is down, or just come round and walking back to his door, or in a fight, or already away on her last call (walking over, with her, or walking back), and every line holds for each',
  'summon-too-far': 'he is at his door, and her post is a long walk from it, too far for a visit',
  'summon-alert': 'security is searching, and staff stay at their posts: he stays at his door',
  'summon-saw-violence': 'he saw one of the cell use violence, fire a shot or draw a gun, so he won\'t come; he is at his door',
};
/** The guard's busy row: when he is down he can't answer (the host still plays his line then: Latch's split, briefs/102/fable/notes.md). */
export const GUARD_BUSY_18 = 'He answers only when he can: just come round and walking back, in a fight, or away on her last call; when he is down he says nothing.';
