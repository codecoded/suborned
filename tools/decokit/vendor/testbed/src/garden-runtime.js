import * as THREE from '../vendor/three-0.186.1/three.module.min.js';

export const gardenLevels=['gameplay','near','close','inspection'];

// Identical runtime behaviour for authored geometry and the pre-baked asset.
export function createGardenRuntime({rendering,materials,statistics:authored,textureResolution,precomputed=false,availableLevels=gardenLevels,loadLevel,onGeometryReady}){
  const levels=gardenLevels;
  const group=new THREE.Group();group.name='Mixed lush café flower beds';
  const {meshes,materialInstances,triangles}=rendering;group.add(...meshes);let detail='gameplay',projectedLeafPixels=null;
  const lod=authored.lod??Object.fromEntries(levels.map(l=>[l,triangles(l)]));
  const available=new Set(availableLevels),pending=new Map(),failed=new Set();let requested=detail;
  function setLOD(level){
    if(!levels.includes(level))throw new Error(`Unknown planting LOD: ${level}`);
    requested=level;
    if(!available.has(level)){
      if(loadLevel&&!pending.has(level)&&!failed.has(level)){
        pending.set(level,Promise.resolve().then(()=>loadLevel(level)).then(()=>{
          available.add(level);pending.delete(level);
          if(requested===level){setLOD(level);onGeometryReady?.();}
        }).catch(error=>{pending.delete(level);failed.add(level);console.warn(`Plant detail ${level} unavailable; keeping loaded detail.`,error.message);}));
      }
      return false;
    }
    if(detail===level)return false;detail=level;rendering.setLOD(detail);return true;
  }
  function updateLOD({worldWidth,viewportWidth,inspect=false,override='auto'}){
    if(!(worldWidth>0&&viewportWidth>0))throw new Error('Planting LOD requires a positive camera and viewport width');
    projectedLeafPixels=.4*viewportWidth/worldWidth;
    let level=override;
    if(override==='auto'){
      if(inspect||projectedLeafPixels>=72||(detail==='inspection'&&projectedLeafPixels>=60))level='inspection';
      else if(projectedLeafPixels>=32||(detail==='close'&&projectedLeafPixels>=28))level='close';
      else if(projectedLeafPixels>=18||(detail==='near'&&projectedLeafPixels>=16))level='near';
      else level='gameplay';
    }return setLOD(level);
  }
  function setWet(on){for(const m of materialInstances.foliage)m.roughness=on?.34:.49;for(const m of materialInstances.flowers)m.roughness=on?.46:.64;for(const m of materialInstances.soil)m.roughness=on?.75:1;}
  function setLighting(mode){for(const m of materialInstances.foliage)m.envMapIntensity=mode==='night'?.28:mode==='day'?.55:.42;for(const m of materialInstances.flowers)m.envMapIntensity=mode==='night'?.22:.4;}
  const lodSummary=()=>({detail,triangles:lod[detail],lod,projectedLeafPixels});
  function statistics(){return {...authored,...lodSummary(),textureResolution,batches:meshes.length,materials:new Set(meshes.map(m=>m.material)).size,precomputed,pendingLOD:[...pending.keys()],loadedLODs:[...available]};}
  return {group,pickables:meshes,materials,setWet,setLighting,setLOD,setDetail:on=>setLOD(on?'inspection':'gameplay'),updateLOD,lodSummary,statistics};
}
