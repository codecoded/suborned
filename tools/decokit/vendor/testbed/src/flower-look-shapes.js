import {finishGardenGeometry,gardenAtlasUV} from './garden-botanical-geometry.js';

const detailRows={gameplay:{small:5,broad:4,strap:5},near:{small:7,broad:7,strap:7},close:{small:11,broad:10,strap:11}};

function leafProfile(kind,t,a){
  const swell=Math.sin(Math.PI*t);
  if(kind==='small'){
    const breadth=1.16*swell**.32*(1+.045*a*Math.sin(5.4*t+1));
    return {x:.025*swell+a*breadth*.5,z:.13*swell-.055*t*t-.085*Math.abs(a)*breadth};
  }
  if(kind==='broad'){
    const breadth=1.14*swell**.44*(1+.065*a*Math.sin(8*t+.6));
    return {x:.055*swell+.025*Math.sin(Math.PI*2*t)+a*breadth*.5,
      z:.29*swell+.055*Math.sin(Math.PI*2*t)-.085*t*t-.13*Math.abs(a)*breadth};
  }
  const breadth=1.0*swell**.68*(.66+.5*t)*(1+.055*a*Math.sin(10*t));
  return {x:.055*Math.sin(Math.PI*t)+.02*Math.sin(Math.PI*2*t)+a*breadth*.5,
    z:.19*swell+.025*Math.sin(Math.PI*2*t)-.08*t*t-.075*Math.abs(a)*breadth};
}

// Root and tip remain unit-space y=0 and y=1, so the existing placement,
// footprint fitting, colour variation and PBR atlas continue to work.
export function flowerLookLeafGeometry(kind,level,region=1,{rows}={}){
  if(!['small','broad','strap'].includes(kind))throw new Error(`Unknown flower-bed leaf kind: ${kind}`);
  const tier=level==='inspection'?'close':level;
  if(!detailRows[tier])throw new Error(`Unknown flower-bed leaf detail: ${level}`);
  const count=Math.max(detailRows[tier][kind],rows??0);
  if(!Number.isInteger(count)||count>32)throw new Error('Leaf rows must be an integer at most 32');
  const columns=kind==='broad'?3:2;
  const positions=[],uvs=[],colours=[],indices=[];
  for(let row=0;row<=count;row++){
    const t=taperedRow(kind,tier,count,row),ao=.42+.58*Math.min(1,t*2.8);
    for(let col=0;col<columns;col++){
      const u=col/(columns-1),a=u*2-1,shape=leafProfile(kind,t,a);
      positions.push(shape.x,t,shape.z);
      uvs.push(...gardenAtlasUV(region,u,t));
      colours.push(ao,ao,ao);
      if(row===count||col===columns-1)continue;
      const n=row*columns+col;
      if(row>0)indices.push(n,n+1,n+columns+1);
      if(row<count-1)indices.push(n,n+columns+1,n+columns);
    }
  }
  return finishGardenGeometry(positions,uvs,colours,indices);
}

function taperedRow(kind,tier,count,row){
  if(tier==='gameplay'&&kind==='small'&&count===5)return [0,.18,.42,.68,.89,1][row];
  if(tier==='gameplay'&&kind==='broad'&&count===4)return [0,.20,.50,.85,1][row];
  return row/count;
}
