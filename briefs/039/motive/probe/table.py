# Print the probe rows as a table: python3 -I table.py rows.jsonl [config ...]
import json, sys
rows = [json.loads(l) for l in open(sys.argv[1], encoding='utf-8') if l.strip()]
want = sys.argv[2:]
def clk(t): return '-' if t < 0 else '%d:%04.1f' % (t // 600, (t % 600) / 10)
print('%-8s %-7s %-6s %-26s %6s %6s %5s %4s %5s %5s %5s %4s %4s %4s' % ('config', 'bench', 'player', 'result', 'fight', 'held', 'down', 'capt', 'lost%', 'hitsT', 'foesD', 'brk', 'lostO', 'ordr'))
for r in rows:
    if want and r['config'] not in want: continue
    print('%-8s %-7s %-6s %-26s %6.1f %6.1f %5d %4d %5d %5d %5d %4d %4d %4d' % (r['config'], r['bench'], r['policy'], r['result'][:26], r['fightS'], r['heldS'], r['down'], r['captured'], r['lostPct'], r['hitsTaken'], r['foesDown'], r['breaks'], r['lostOrders'], r['humanOrders']))
