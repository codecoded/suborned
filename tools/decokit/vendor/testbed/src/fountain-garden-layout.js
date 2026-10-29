// Four curved islands from the approved civic concept. This footprint is shared
// by the stone beds, contained planting patches and navigation obstacles.
export function createFountainGardenLayout({x,y}){
  const inner=5.35,outer=7.05,centre=(inner+outer)/2,half=(outer-inner)/2,span=.48;
  const beds=[],palms=[];
  const point=(radius,angle)=>({x:x+Math.cos(angle)*radius,y:y+Math.sin(angle)*radius});
  for(const [i,name] of ['north-east','north-west','south-west','south-east'].entries()){
    const angle=Math.PI/4+i*Math.PI/2,start=angle-span/2,end=angle+span/2,vertices=[];
    for(let j=0;j<=16;j++)vertices.push(point(outer,start+span*j/16));
    const endCentre=point(centre,end),startCentre=point(centre,start);
    for(let j=1;j<=12;j++){const a=end+Math.PI*j/12;vertices.push({x:endCentre.x+Math.cos(a)*half,y:endCentre.y+Math.sin(a)*half});}
    for(let j=1;j<=16;j++)vertices.push(point(inner,end-span*j/16));
    for(let j=1;j<12;j++){const a=start+Math.PI+Math.PI*j/12;vertices.push({x:startCentre.x+Math.cos(a)*half,y:startCentre.y+Math.sin(a)*half});}
    const bed={id:'fountain-garden-'+name,type:'polygon',kind:'planter',vertices,
      x:[Math.min(...vertices.map(p=>p.x)),Math.max(...vertices.map(p=>p.x))],
      y:[Math.min(...vertices.map(p=>p.y)),Math.max(...vertices.map(p=>p.y))],
      z:[0,.52],wall:.14,module:'curved-tropical-island',region:'fountain'};
    const root=point(centre,angle+.035);
    if(i<2)palms.push({id:bed.id+'-palm',...root,base:.465,scale:.77,heading:-.9+i*Math.PI/2,region:'fountain'});
    bed.treeBay={centre:root,soilHeight:.465};beds.push(bed);
  }
  for(const bed of beds.slice(2))delete bed.treeBay;
  // The fountain kit authors one prototype, repeated at these quarter turns.
  return {beds,palms:palms.slice(0,2),reference:'art/suborned-civic-2p5d-concept-v1.png',
    innerRadius:inner,outerRadius:outer,bedHeight:.52,cardinalApproachesClear:true};
}
