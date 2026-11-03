import {flowerBedRun} from './flower-bed-layout.js';

export const civicPlantingReference='art/suborned-civic-2p5d-concept-v1.png';

// Concept groups fitted to the held street plan, with the entrance axis and
// service lane left open. These authored metres can also drive Unity placement.
export function createCivicPlantingLayout(){
  const beds=[],palms=[];
  function bed(id,x,y,length,width,heading=0,role='perimeter',sections=[{length}],accent={}){
    const b=flowerBedRun({id:'civic-'+id,x,y,heading,width,seed:71+beds.length*37,sections});
    b.region='civic';b.conceptRole=role;b.reference=civicPlantingReference;
    b.plantingAccent=accent;
    beds.push(b);return b;
  }
  function palm(b,x,y,scale,heading=-2.4){
    const p={id:b.id+'-palm-'+palms.length,x,y,base:.465,scale,heading,region:'civic'};
    palms.push(p);b.palms??=[];b.palms.push(p);
    for(const patch of b.patches)patch.plantingPoints=patch.plantingPoints.filter(q=>Math.hypot(q.x-x,q.y-y)>.20);
  }
  const entranceWest=bed('entrance-west',26.4,-26.15,0,2.4,0,'entrance-flank',
    [{length:3.6},{turn:.42,radius:2.6},{length:.6}],{flowerDensity:1.1,fanDensity:.65});
  palm(entranceWest,29,-25.9,.74,-2.7);palm(entranceWest,27.1,-26.4,.25,.6);palm(entranceWest,31.3,-25.85,.33,-.8);
  const entranceEast=bed('entrance-east',42,-26.4,0,2,.08,'entrance-flank',
    [{length:2.3},{turn:-.38,radius:2.5},{length:.65}],{flowerDensity:.85,fanDensity:1.15});
  palm(entranceEast,45.4,-26.3,.91,-1.8);palm(entranceEast,42.4,-26.45,.28,.4);
  entranceEast.palette.palettes=[['gold','coral'],['gold','cream'],['coral','gold']];
  // The original facade has its own low planting, separate from the street
  // islands. Fit the chamfers without covering windows or narrowing the ramp.
  const facadeWest=bed('facade-southwest',27.1,-20.8,3.3,1.05,-.597,'entrance-facade',undefined,
    {flowerDensity:1.25,fanDensity:.8});
  palm(facadeWest,28.05,-21.45,.20,.4);palm(facadeWest,29.45,-22.25,.23,-.5);
  const facadeEast=bed('facade-southeast',42.3,-23.45,3.4,1.05,.66,'entrance-facade',undefined,
    {flowerDensity:1.1,fanDensity:.9});
  palm(facadeEast,43.15,-22.80,.22,-1);palm(facadeEast,44.45,-21.80,.24,.3);
  const west=bed('office-west',23.7,-7.8,0,1.5,-Math.PI/2,'facade-border',
    [{length:6.8},{turn:.25,radius:3},{length:1.1}],{flowerDensity:.45,fanDensity:1.2});
  palm(west,23.7,-9.7,.57);palm(west,23.9,-15.1,.26,.8);palm(west,23.95,-16.1,.31,-.3);
  const north=bed('office-north',29.5,-2.5,3.5,1.5,-.12,'facade-border',undefined,{flowerDensity:.3,fanDensity:.55});
  palm(north,31.8,-2.8,.42);
  const dropWest=bed('dropoff-west',25.8,8.9,0,2.1,-.35,'dropoff-flank',
    [{turn:.75,radius:2.1}],{flowerDensity:1.15,fanDensity:.6});
  palm(dropWest,26.8,8.8,.70);palm(dropWest,25.6,8.7,.22);
  const dropEast=bed('dropoff-east',47.6,9.9,1.7,2.4,-.18,'dropoff-flank',undefined,{flowerDensity:.5,fanDensity:1.1});
  palm(dropEast,48.9,9.6,.57);palm(dropEast,47.3,9.6,.30);
  const eastNorth=bed('east-north',61.95,1.5,0,1.65,-Math.PI/2,'perimeter',
    [{length:2.7},{turn:.38,radius:2.1},{turn:-.38,radius:2.1},{length:.6}],{flowerDensity:.9,fanDensity:.65});
  palm(eastNorth,61.95,.7,.62);palm(eastNorth,62.1,-2.1,.26,.5);
  const eastMiddle=bed('east-middle',62.2,-10,6.4,1.5,-Math.PI/2+.025,'perimeter',undefined,{flowerDensity:.25,fanDensity:1.3});
  palm(eastMiddle,62.25,-12.1,.76);palm(eastMiddle,62.4,-13.7,.24,.9);palm(eastMiddle,62.4,-14.5,.29,-.6);
  const eastSouth=bed('east-south',62.05,-24,0,1.8,-Math.PI/2,'perimeter',
    [{length:4.2},{turn:-.35,radius:2.2},{length:3.5}],{flowerDensity:.65,fanDensity:.8});
  palm(eastSouth,62,-26.9,.47);palm(eastSouth,61.4,-30.3,.69,-1.1);
  const foregroundWest=bed('foreground-west',22.8,-41.4,0,2.5,-.1,'foreground-frame',
    [{length:3.5},{turn:.55,radius:2.5}],{flowerDensity:1.2,fanDensity:.5});
  palm(foregroundWest,23.2,-41.3,.35);palm(foregroundWest,27,-41.5,.72);palm(foregroundWest,26.1,-41.6,.24,.4);
  const foregroundEast=bed('foreground-east',39.8,-42.1,3.7,1.5,.12,'foreground-frame',undefined,{flowerDensity:.45,fanDensity:1.1});
  palm(foregroundEast,40.8,-41.95,.56);palm(foregroundEast,42.5,-41.82,.30,.5);
  const cafe=bed('cafe-west-border',-12.3,-12,0,1.7,-Math.PI/2,'precinct-frame',
    [{length:2.4},{turn:.26,radius:2.1},{turn:-.26,radius:2.1},{length:5.8}],{flowerDensity:.65,fanDensity:.85});
  palm(cafe,-12.3,-13.2,.62);palm(cafe,-12.15,-18.8,.46);palm(cafe,-12.15,-19.8,.25,.3);
  const getaway=bed('getaway-edge',-15,-37.2,3.2,1.3,-Math.PI/2+.05,'precinct-frame',undefined,{flowerDensity:1.1,fanDensity:.5});
  palm(getaway,-14.9,-39.5,.49);
  const monumentWest=bed('monument-west',31.1,-42.0,1.0,1.05,0,'monument-flank',undefined,{flowerDensity:1.2,fanDensity:.7});
  palm(monumentWest,31.65,-42.0,.18,-.7);
  const monumentEast=bed('monument-east',37.7,-41.7,.65,1.0,0,'monument-flank',undefined,{flowerDensity:1.2,fanDensity:.7});
  palm(monumentEast,38,-41.7,.18,.5);
  const getawayFront=bed('getaway-front',-.8,-40.4,2.0,1.4,0,'getaway-flank',undefined,{flowerDensity:1.1,fanDensity:.65});
  palm(getawayFront,.15,-40.4,.52,-1.2);
  return {beds,palms,reference:civicPlantingReference,entranceAxis:{x:37,y:[-29,-22]},serviceLaneClear:true};
}
