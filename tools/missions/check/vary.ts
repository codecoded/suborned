// The variations the mission checker plays (brief 011 B; GDD §09): each a pure transform of a route script, so a recorded check
// regenerates exactly the scripts it played from the route scripts it recorded.
// - without a contact: every order that targets the role (watch, listen, turn, call) is dropped, as if the person could not be
//   reached; the rest of the plan plays as written (a player would improvise; the script does not, which is the point).
// - late from a step: every order at or after a tick is given `d` seconds later (the player is late there and stays late).
import type { Order, Script } from './engine';

/** The roles a script works through: the targets of its person orders. */
export const contactsOf = (s: Script, roles: readonly string[]) => [...new Set(s.orders.filter(o => o.target && roles.includes(o.target) && ['watch', 'listen', 'turn', 'call'].includes(o.do)).map(o => o.target!))];

/** The script without a contact: its orders that target `role` are dropped. */
export function withoutContact(s: Script, role: string): Script {
  return { ...s, orders: s.orders.filter(o => o.target !== role || !['watch', 'listen', 'turn', 'call'].includes(o.do)) };
}

/** The distinct order ticks of a script: the steps a player can be late at. */
export const stepsOf = (s: Script) => [...new Set(s.orders.map(o => o.tick))].sort((a, b) => a - b);

/** The script with every order at or after `tick` given `seconds` later (10 ticks a second); it plays to its own `until` or the
 *  last order, whichever is later. */
export function lateFrom(s: Script, tick: number, seconds: number): Script {
  const orders = s.orders.map((o): Order => (o.tick >= tick ? { ...o, tick: o.tick + seconds * 10 } : o));
  return { ...s, orders, until: Math.max(s.until, ...orders.map(o => o.tick)) };
}
