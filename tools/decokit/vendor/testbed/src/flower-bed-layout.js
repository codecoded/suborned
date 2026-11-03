const TAU=Math.PI*2;
const finite=(...values)=>values.every(Number.isFinite);
const point=(x,y,heading)=>({x,y,heading});

// Tangent-connected sections become one footprint, not overlapping troughs.
// The same outline is consumed by the stone, soil, planting and navigation.
export function flowerBedRun({id,x=0,y=0,heading=0,width=.8,base=0,height=.52,wall=.08,seed=42,sections=[{length:2.4}]}){
  if(typeof id!=='string'||!id||!finite(x,y,heading,width,base,height,wall,seed)||width<.4||height<.12||base<0||wall<=0||wall>=width/3||!Array.isArray(sections)||!sections.length)
    throw new Error('Invalid modular flower-bed dimensions');
  const start=point(x,y,heading),path=[start],plantingPoints=[],joins=[];
  let cursor=start,totalLength=0;
  for(const section of sections){
    const curved=section.turn!==undefined;
    if(curved?(!finite(section.turn,section.radius)||Math.abs(section.turn)<.01||Math.abs(section.turn)>Math.PI||section.radius<width):(!finite(section.length)||section.length<.2))
      throw new Error('Sections require a positive length or a safe curve radius and turn');
    const origin=cursor,length=curved?Math.abs(section.turn)*section.radius:section.length;
    function at(distance){
      if(!curved)return point(origin.x+Math.cos(origin.heading)*distance,origin.y+Math.sin(origin.heading)*distance,origin.heading);
      const sign=Math.sign(section.turn),angle=origin.heading+sign*distance/section.radius;
      return point(origin.x+sign*section.radius*(Math.sin(angle)-Math.sin(origin.heading)),origin.y-sign*section.radius*(Math.cos(angle)-Math.cos(origin.heading)),angle);
    }
    const steps=curved?Math.ceil(Math.abs(section.turn)/(Math.PI/24)):1;
    for(let i=1;i<=steps;i++)path.push(at(length*i/steps));
    const sideRows=width>=.65?Math.max(1,Math.ceil((width/2-wall)/.35)):0;
    const rows=Array.from({length:sideRows*2+1},(_,i)=>sideRows?(i-sideRows)/sideRows:0),spread=(width/2-wall)*(width>1?.8:.62);
    for(const row of rows){
      const plants=Math.max(1,Math.ceil(length/(row===0?.38:.30)));
      for(let i=0;i<plants;i++){
        const p=at(length*(i+(row===-1?.35:row===1?.65:.5))/plants);
        plantingPoints.push({...p,x:p.x-row*Math.sin(p.heading)*spread,y:p.y+row*Math.cos(p.heading)*spread,layer:row===0?'canopy':'border'});
      }
    }
    totalLength+=length;cursor=at(length);joins.push({...cursor,distance:totalLength});
  }
  const half=width/2,offset=(p,side)=>({x:p.x-side*Math.sin(p.heading)*half,y:p.y+side*Math.cos(p.heading)*half});
  const vertices=path.map(p=>offset(p,-1));
  function cap(p,from){for(let i=1;i<=12;i++){const angle=from+Math.PI*i/12;vertices.push({x:p.x+Math.cos(angle)*half,y:p.y+Math.sin(angle)*half});}}
  cap(cursor,cursor.heading-Math.PI/2);
  vertices.push(...path.slice(0,-1).reverse().map(p=>offset(p,1)));
  cap(start,start.heading+Math.PI/2);vertices.pop();
  // Self-intersecting runs cannot form a valid shared soil/collision polygon.
  const cross=(a,b,c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
  const onSegment=(p,a,b)=>Math.abs(cross(a,b,p))<1e-8&&p.x>=Math.min(a.x,b.x)-1e-8&&p.x<=Math.max(a.x,b.x)+1e-8&&p.y>=Math.min(a.y,b.y)-1e-8&&p.y<=Math.max(a.y,b.y)+1e-8;
  for(let i=0;i<vertices.length;i++)for(let j=i+2;j<vertices.length;j++){
    if(i===0&&j===vertices.length-1)continue;
    const a=vertices[i],b=vertices[(i+1)%vertices.length],c=vertices[j],d=vertices[(j+1)%vertices.length];
    const abC=cross(a,b,c),abD=cross(a,b,d),cdA=cross(c,d,a),cdB=cross(c,d,b);
    if(abC*abD<0&&cdA*cdB<0||onSegment(a,c,d)||onSegment(b,c,d)||onSegment(c,a,b)||onSegment(d,a,b))throw new Error('Flower-bed sections overlap; use separate beds');
  }
  const bounds=axis=>[Math.min(...vertices.map(p=>p[axis])),Math.max(...vertices.map(p=>p[axis]))];
  for(const [p,direction] of [[start,-1],[cursor,1]])for(const turn of [-.85,0,.85]){
    const angle=p.heading+(direction<0?Math.PI:0)+turn,radius=width*.325;
    plantingPoints.push({...p,x:p.x+Math.cos(angle)*radius,y:p.y+Math.sin(angle)*radius,layer:'border'});
  }
  return {id,kind:'planter',type:'polygon',module:'modular-tropical-run',vertices,x:bounds('x'),y:bounds('y'),z:[base,base+height],wall,seed,
    plantingProfile:'lush-tropical',
    palette:{name:'Approved tropical coral and amber',palettes:[['coral','gold'],['gold','coral'],['coral','gold']]},
    patches:[{id:id+'-planting',x:bounds('x'),y:bounds('y'),seed,plantingPoints,width}],
    assembly:{sections:sections.map(s=>({...s})),length:totalLength,joins:joins.slice(0,-1),continuousSoil:true,roundedEndCaps:2,
      sockets:[{...start,heading:(start.heading+Math.PI)%TAU},{...cursor,heading:cursor.heading%TAU}]}};
}

export function flowerBedKitExamples(){
  const quarter=Math.PI/2;
  const island=flowerBedRun({id:'palm-island',x:-1.25,y:6.4,width:2.2,sections:[{length:2.5}],seed:35});
  island.palms=[{id:'island-canopy',x:.35,y:6.7,base:.465,scale:.7,heading:-2.5},
    {id:'island-understory-west',x:-.9,y:6.3,base:.465,scale:.22,heading:.7},
    {id:'island-understory-east',x:1.05,y:6.1,base:.465,scale:.24,heading:-.8}];
  island.patches[0].plantingPoints=island.patches[0].plantingPoints.filter(p=>island.palms.every(tree=>Math.hypot(p.x-tree.x,p.y-tree.y)>.23));
  return [island,
    flowerBedRun({id:'straight',x:-5.6,y:3,sections:[{length:3}],seed:18}),
    flowerBedRun({id:'corner',x:-.9,y:2.7,sections:[{turn:quarter,radius:1.2}],seed:11}),
    flowerBedRun({id:'crescent',x:3,y:2.7,sections:[{turn:Math.PI,radius:1.15}],seed:28}),
    flowerBedRun({id:'joined-border',x:-5.6,y:-1.7,sections:[{length:3},{turn:quarter,radius:1.1},{length:1.15}],seed:81}),
    flowerBedRun({id:'sweep',x:.3,y:-1.7,sections:[{length:1.3},{turn:quarter,radius:1.2},{turn:-quarter,radius:1.2},{length:1}],seed:15})
  ];
}
