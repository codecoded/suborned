import json,sys
for n in sys.argv[1:]:
  d=json.load(open(f'/Users/sky/Projects/suborned/briefs/048/runs/{n}/measure.json'))
  print('==',n,d['screen'],'scale',d['renderScale'])
  for r in d['rows']:
    c=r['counters'];ce=r.get('census',{})
    print('%-34s %6.2f p99 %6.2f gpu %5.2f main %5.2f rt %5.2f | tris %5.2fM setpass %4.0f casters %5.0f | drawn %s b150 %s' % (r['name'],r['meanMs'],r['p99Ms'],r['gpuMs'],r['cpuMainMs'],r['cpuRenderMs'],c.get('Triangles Count',0)/1e6,c.get('SetPass Calls Count',0),c.get('Shadow Casters Count',0),ce.get('drawn','-'),ce.get('beyond150','-')))
