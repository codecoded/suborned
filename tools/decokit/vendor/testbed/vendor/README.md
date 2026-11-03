# Three.js vendor bundle

The active scene uses Three.js 0.186.1 (r186), pinned in package-lock.json.
Run `npm ci && npm run vendor:three` to reproduce the runtime bundle.
`THREE-source.json` records upstream paths, generated hashes and minifier version.
Every addon import resolves locally; the deployed site needs no npm installation
or runtime CDN. Active imports use the immutable `three-0.186.1/` directory,
including the split module/core builds and Basis transcoder files. Top-level
JavaScript paths re-export the versioned modules for historical consumers.
The runtime does not use these compatibility aliases.

The GLTF/KTX2 loaders, meshopt decoder, Basis transcoder, worker pool and helpers
all come from the same npm package. `RGBELoader.js` is a local compatibility
alias for upstream `HDRLoader.js`.

`marble/` is an independent archived Spark viewer bundle and is not updated by
this script. It is not part of the active scene's Three.js module graph.

Migration reference: https://github.com/mrdoob/three.js/wiki/Migration-Guide
WebGL 2 is required. r181 PBR energy conservation and r182 soft PCF filtering
change the rendered lighting slightly; runtime visual QA remains necessary.
