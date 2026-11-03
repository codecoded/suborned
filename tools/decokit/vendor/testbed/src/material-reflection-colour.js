import * as THREE from '../vendor/three-0.186.1/three.module.min.js';

const controls=new WeakMap();
// Expanded environment chunks can leave this anchor for the shared control.
export const reflectionColourAnchor='// reflection_environment_sampled';

// Art direction control: retain reflected luminance and scene detail while
// reducing how much environment colour changes the material's own finish.
// 0 = monochrome reflection light; 1 = full environment colour.
export function setReflectionColourResponse(material,saturation=.06) {
  if(controls.has(material)){
    const control=controls.get(material);control.saturation=saturation;return control;
  }
  const response={value:THREE.MathUtils.clamp(saturation,0,1)};
  const previousCompile=material.onBeforeCompile,previousKey=material.customProgramCacheKey();
  material.onBeforeCompile=function(shader,renderer){
    previousCompile.call(this,shader,renderer);
    shader.uniforms.uReflectionColourResponse=response;
    // A cloned material can carry the source hook. Override its uniform without
    // inserting the same declaration and reflection operation twice.
    if(shader.fragmentShader.includes('uniform float uReflectionColourResponse;'))return;
    shader.fragmentShader='uniform float uReflectionColourResponse;\n'+shader.fragmentShader;
    const anchor=shader.fragmentShader.includes(reflectionColourAnchor)?reflectionColourAnchor:'#include <lights_fragment_maps>';
    if(!shader.fragmentShader.includes(anchor))throw new Error(`No reflection shader anchor in ${material.name||material.type}`);
    shader.fragmentShader=shader.fragmentShader.replace(anchor,`${anchor}
      #ifdef USE_ENVMAP
        vec3 reflectionLuma=vec3(0.2126,0.7152,0.0722);
        iblIrradiance=mix(vec3(dot(iblIrradiance,reflectionLuma)),iblIrradiance,uReflectionColourResponse);
        radiance=mix(vec3(dot(radiance,reflectionLuma)),radiance,uReflectionColourResponse);
        #ifdef USE_CLEARCOAT
          clearcoatRadiance=mix(vec3(dot(clearcoatRadiance,reflectionLuma)),clearcoatRadiance,uReflectionColourResponse);
        #endif
      #endif
    `);
  };
  material.customProgramCacheKey=()=>previousKey+':reflection-colour-v1';
  material.userData.reflectionColour={
    get saturation(){return response.value;},
    set saturation(value){response.value=THREE.MathUtils.clamp(value,0,1);},
    preservesLuminance:true,preservesSceneDetail:true,
  };
  controls.set(material,material.userData.reflectionColour);
  material.needsUpdate=true;
  return material.userData.reflectionColour;
}

export function getReflectionColourResponse(material){return controls.get(material);}
