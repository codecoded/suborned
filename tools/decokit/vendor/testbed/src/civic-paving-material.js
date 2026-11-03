import * as THREE from '../vendor/three-0.186.1/three.module.min.js';
import {setReflectionColourResponse,reflectionColourAnchor} from './material-reflection-colour.js';
import {loadSurfaceTexture} from './surface-textures.js';

// Ceramic paving keeps broad white environment highlights. Direct lamps and
// sunlight still shade the surface normally, including the mapped grain.
function neutralTileEnvironment(shader) {
  const lighting=THREE.ShaderChunk.lights_fragment_maps
    .replaceAll('material.roughness','max(material.roughness,0.55)')
    .replaceAll('material.clearcoatRoughness','max(material.clearcoatRoughness,0.55)');
  shader.fragmentShader=shader.fragmentShader.replace('#include <lights_fragment_maps>',lighting+`\n${reflectionColourAnchor}
    #ifdef USE_ENVMAP
      radiance*=0.4;
      #ifdef USE_CLEARCOAT
        clearcoatRadiance*=0.4;
      #endif
    #endif
  `);
}

// The standalone fountain also keeps the approved slab surface independent of
// its bespoke basin stone; changing the landmark must not erase walkway joints.
export async function loadCivicPavingMaterial(renderer) {
  const path='./art/cafe-terrace/';
  const [map,normalMap,orm]=await Promise.all(['limestone-base.png','limestone-normal.png','limestone-orm.png'].map(n=>loadSurfaceTexture(renderer,path+n)));
  map.colorSpace=THREE.SRGBColorSpace;
  for(const t of [map,normalMap,orm]){t.wrapS=t.wrapT=THREE.RepeatWrapping;t.anisotropy=Math.min(8,renderer.capabilities.getMaxAnisotropy());}
  const source=new THREE.MeshStandardMaterial({map,normalMap,normalScale:new THREE.Vector2(.8,.8),roughnessMap:orm,aoMap:orm,roughness:.72});
  const material=createCivicPavingMaterial(source);source.dispose();return material;
}

// The source atlas contains two one-metre slabs. Public geometry uses a larger
// world-space repeat for every map; the café keeps its original UVs and grain.
// Both materials share the texture objects, with independent palette/polish.
export function createCivicPavingMaterial(source, {tint='#f2dfbd',lift=.10,neutrality=.8,roughness=.61,slabMetres=.8,reflectionStrength=.7}={}) {
  const material=source.clone();material.name='Warm ivory civic tiles';material.color.set(tint);
  material.roughness=roughness;material.normalScale.multiplyScalar(.6);material.envMapIntensity=reflectionStrength;
  const lightness={value:lift},neutral={value:neutrality};
  material.userData.civicPaving={source:'cafe limestone PBR',finish:'warm ivory satin stone with readable slab grain',lift,tint,neutrality,roughness,normalDetailScale:.6,slabMetres,repeatMetres:2*slabMetres,reflectionStrength,independentPalette:true,environmentFinish:'restrained broad sheen with adjustable reflection colour',environmentMinimumRoughness:.55,environmentSpecularGain:.4};
  material.onBeforeCompile=shader=>{
    shader.uniforms.uCivicPavingLift=lightness;
    shader.uniforms.uCivicPavingNeutrality=neutral;
    shader.fragmentShader='uniform float uCivicPavingLift;\nuniform float uCivicPavingNeutrality;\n'+shader.fragmentShader;
    shader.fragmentShader=shader.fragmentShader.replace('#include <map_fragment>',
      'vec3 civicTint=diffuseColor.rgb;\n#include <map_fragment>\nvec3 civicTexture=diffuseColor.rgb/max(civicTint,vec3(0.0001));\nfloat civicLuminance=dot(civicTexture,vec3(0.2126,0.7152,0.0722));\nvec3 civicStone=mix(civicTexture,vec3(civicLuminance),uCivicPavingNeutrality);\ndiffuseColor.rgb=mix(civicStone,vec3(1.0),uCivicPavingLift)*civicTint;');
    neutralTileEnvironment(shader);
  };
  material.customProgramCacheKey=()=> 'suborned-civic-paving-v5-concept';
  setReflectionColourResponse(material,.15);
  return material;
}

export function createCivicBlueTileMaterial() {
  const material=new THREE.MeshPhysicalMaterial({
    name:'Glazed blue civic inlay tiles',color:'#669fab',vertexColors:true,
    metalness:0,roughness:.36,clearcoat:.28,clearcoatRoughness:.24
  });
  material.onBeforeCompile=neutralTileEnvironment;
  material.customProgramCacheKey=()=> 'suborned-civic-blue-paving-v2';
  setReflectionColourResponse(material,0);
  return material;
}
