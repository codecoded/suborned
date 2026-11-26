// The open space is the ground between the sited wings. The same rounded footprints go to
// the Blender builder, the level checks and the district walking grid.
import type { Megastructure } from '../../../plan/plan';
import type { V } from '../../../plan/geom';
import type { Ctx } from '../common';
import type { Rect } from '../geo';
import type { MegaOut } from '../types';
import { rectMass } from './kit';

export type CourtPlan = Megastructure & {
  wings: { id: string; rect: Rect; floors: number; radius: number }[];
  bridge: { rect: Rect; base: number; height: number };
  clock: { at: V; radius: number };
  planting: { at: V; radius: number; kit: string }[];
};

function rounded([x0,x1,y0,y1]: Rect, radius: number): V[] {
  const ring: V[] = [];
  for (const [x,y,a] of [[x1-radius,y0+radius,-90],[x1-radius,y1-radius,0],
                         [x0+radius,y1-radius,90],[x0+radius,y0+radius,180]] as const)
    for (let i=0;i<=8;i++) {
      const angle=(a+i*90/8)*Math.PI/180;
      ring.push([+(x+radius*Math.cos(angle)).toFixed(3),+(y+radius*Math.sin(angle)).toFixed(3)]);
    }
  return ring;
}

export function courtyard(ctx: Ctx, m: CourtPlan): MegaOut {
  const id='mega/'+m.id;
  const parts=m.wings.map(w=>({
    id:id+'/court-wing/'+w.id, surface:'residential', ring:rounded(w.rect,w.radius),
    base:ctx.pav, height:3.85+(w.floors-1)*3.05+.55,
  }));
  parts.push(rectMass(id+'/glazed-bridge','residential',m.bridge.rect,ctx.pav+m.bridge.base,ctx.pav+m.bridge.base+m.bridge.height));
  for (const [i,bed] of [m.clock,...m.planting].entries()) {
    const [x,y]=bed.at,r=bed.radius;
    parts.push({id:id+'/bed/'+i,surface:'limestone',ring:rounded([x-r,x+r,y-r,y+r],r),base:ctx.pav,height:.52});
  }
  return {id,parts,prims:[],decks:[],hosts:[],cores:[],connectors:[],avoid:[]};
}
