// Uniform-only art direction. No added light slots, texture samples, passes,
// geometry or per-frame work; the powder-blue road comparison stays separate.
const warmLook=Object.freeze({
  revision:'sunlit-garden-v1',
  daylight:Object.freeze({sun:3.15,sky:.28,environment:.62,propEnvironment:.60,exposure:1.04,
    sunColour:'#ffe3b1',skyColour:'#d8edf3',groundColour:'#ad9470'}),
  foliage:'#d5e7ba',
  asphalt:Object.freeze({colour:'#b5b4ae',roughness:.84,normalStrength:.60}),
  lamp:Object.freeze({finish:'champagne',colour:'#e4b56d',roughness:.26}),
});
const baselineLook=Object.freeze({
  revision:'authored-baseline',daylight:null,foliage:'#afbea1',
  asphalt:Object.freeze({colour:'#687580',roughness:.88,normalStrength:.45}),
  lamp:Object.freeze({finish:'chrome',colour:'#d8dfe4',roughness:.16}),
});
// Keep the authored default until the timing follow-up clears the preview.
// Node-side imports and unknown revisions also retain the original finish.
export const civicDistrictLook=typeof location!=='undefined'&&
  new URLSearchParams(location.search).get('look')===warmLook.revision?warmLook:baselineLook;

// Developer-only census, called once in a frozen benchmark. Unique resources
// are counted by identity, including inactive LODs, so sharing remains visible.
export function civicRenderResourceBudget(root){
  const geometry=new Set(),materials=new Set(),textures=new Set();
  let meshes=0,lights=0,shadowLights=0,triangles=0;
  root.traverse(o=>{
    if(o.isLight){lights++;if(o.castShadow)shadowLights++;}
    if(!o.isMesh)return;
    meshes++;
    if(!geometry.has(o.geometry)){
      geometry.add(o.geometry);
      triangles+=(o.geometry.index?.count??o.geometry.attributes.position?.count??0)/3;
    }
    for(const m of Array.isArray(o.material)?o.material:[o.material]){
      materials.add(m);
      for(const value of Object.values(m))if(value?.isTexture)textures.add(value);
    }
  });
  return {meshes,geometries:geometry.size,uniqueGeometryTriangles:triangles,
    materialInstances:materials.size,textureObjects:textures.size,lights,shadowCastingLights:shadowLights};
}
