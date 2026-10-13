import {streetDensity} from './model';
const h=+(process.argv[2]??17.2);
for(const r of streetDensity(h)) console.log(r.id.padEnd(18),r.cls.padEnd(8),'foot',r.foot,'spill',r.spill.toFixed(2),'walk',r.walk.toFixed(2),'thru',r.through.toFixed(2),'prom',r.prom.toFixed(2),'dens',((r.spill+r.walk+r.through+r.prom)/r.foot).toFixed(3))
