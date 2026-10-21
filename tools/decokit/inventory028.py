#!/usr/bin/env python3
"""Brief 028 inventory (Vela): every testbed/src module and every testbed-art folder against this repo. python3 -I tools/decokit/inventory028.py -> briefs/028/inventory.html (self-contained).
Status: ported (a kit candidate or in the game) / in progress (brief 026 or this brief, not yet adopted) / to port / not wanted (reason). 026 = covered by brief 026 (the other Vela session)."""
import base64
import io
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[2]
STORE = Path("~/Projects/the-suborned-store/testbed-art").expanduser()
OUT = REPO / "briefs/028/inventory.html"
P, IP, TP, NW = "ported", "in progress", "to port", "not wanted"
# name, what it is, where it shows, today's equivalent, status, owner, compression (today's kit / testbed source), 026?
SRC = [
 ("animation-bounds.js", "Reads animation extents from GLB/VAT data so cull bounds hold", "dev / culling", "Unity renderer bounds", NW, "Latch", "", 0),
 ("boarding-pavilion-preview.js", "Review page for the pavilion (LOD by screen coverage)", "boarding-pavilion.html only", "briefs/028/bay renders", NW, "Vela", "", 0),
 ("cafe-ambience.js", "Café interior and terrace fixtures: lights point down, fixture coordinates from the model report", "café front, night", "Glint's café night plan (briefs/026/glint); lamps026.py pieces", IP, "Glint", "", 1),
 ("cafe-art-geometry.js", "Pulls painted-art UV boundaries onto opaque building pixels (2.5D plate trick)", "2.5D café plate", "none: live 3D (CafeBay_v2)", NW, "Vela", "2.5D plate technique, superseded by live 3D", 0),
 ("cafe-bed-geometry.js", "Mitred inward offsets: wall thickness round courtyard planter corners", "café courtyard planters", "PlanterBed / PlanterRun / PlanterArc kit pieces (plaza.py, civic_street.py)", TP, "Vela", "compare planter walls with the testbed's", 0),
 ("cafe-botanical-geometry.js", "Leaf and frond meshes for café plants (close = closed skin, far = cubic bow)", "café planters, terrace", "plants.py (kit plants), BedTuft_A..C", TP, "Vela", "testbed far mesh is a reduced leaf; kit tufts are cut for play zoom", 0),
 ("cafe-building.js", "Real café shell geometry extending the pilot's room coordinates", "café", "CafeBay_v2 + CafeInterior (Oct 9 shell, C-0)", NW, "Vela", "the shell is locked by C-0; only the room contents come over (026)", 1),
 ("cafe-camera.js", "Camera framing modes", "testbed camera", "Unity camera (Latch), 14 at 38 deg, Q/E", NW, "Latch", "", 0),
 ("cafe-controls.js", "Orbit / walk dev controls", "testbed dev", "none", NW, "Latch", "", 0),
 ("cafe-door-layout.js", "Café door placement", "café front", "CafeBay_v2 door leaves", TP, "Vela", "compare the double door and its frame", 0),
 ("cafe-door.js", "Separates the entrance from the façade batches so it can swing", "café front", "CafeBay_v2 (glass door leaves, fixed)", TP, "Vela", "a state change: a door that opens must be live 3D", 0),
 ("cafe-exterior-light-layout.js", "Terrace and awning light positions", "café terrace, night", "lamps026.py AwningLamp, PalmUplight, StreetLampGlobe", IP, "Glint", "", 1),
 ("cafe-exterior-lights.js", "Static shared fixture batches plus real local lights on the terrace", "café terrace, night", "same: lamp pieces (Vela, done) + light rig (Glint)", IP, "Glint", "", 1),
 ("cafe-first-person.js", "First-person walk", "testbed dev", "none: never first person", NW, "Latch", "", 0),
 ("cafe-furniture.js", "Places the table sets on the terrace", "café terrace", "CafeSet_Testbed (cafe_testbed.py)", P, "Vela", "", 1),
 ("cafe-garden-composition.js", "Composes planting strips and root bays; colour drift per bay", "café and civic planting", "tools/world street planting + BedTuft", TP, "Splice", "rules, not pieces", 0),
 ("cafe-guide.js", "Guide-image toggle", "dev", "none", NW, "Vela", "", 0),
 ("cafe-local-light-culling.js", "Culls local lights per fragment (three's BRDF cost)", "renderer", "URP light limits (Glint)", NW, "Glint", "three.js specific", 0),
 ("cafe-map-pan.js", "Map pan on the ground plane", "dev", "none", NW, "Latch", "", 0),
 ("cafe-multiview-world.js", "World for the four-elevation café review, from the Blender model spec", "cafe-multiview.html", "none", NW, "Vela", "test harness", 0),
 ("cafe-multiview.js", "627-line review harness: café from four sides, art and guide layers", "cafe-multiview.html", "briefs/026/cafe renders", NW, "Vela", "test harness", 0),
 ("cafe-palms-layout.js", "Palm roots at the centre of the square bays", "café frontage", "CoconutPalm_Street (palms026.py); bay placement is the world generator's", IP, "Splice", "", 1),
 ("cafe-palms.js", "Palm instancing", "café frontage", "CoconutPalm_Street", IP, "Vela", "built from the testbed's gameplay palm (4.8k tris); the testbed has a 19,330-tri close level", 1),
 ("cafe-planting-layout.js", "One outline drives the stonework and navigation; square root bays open into troughs", "café frontage", "tools/world planters", TP, "Splice", "", 0),
 ("cafe-planting.js", "Reusable trough placements; botanical meshes shared", "café frontage", "PlanterRun_* + BedTuft", TP, "Vela", "", 0),
 ("cafe-plaza-layout.js", "Café pilot's local coordinates", "café", "the plan (Datum): contracts/world", NW, "Datum", "layout is the plan's", 0),
 ("cafe-plaza.js", "Held café footprint", "café", "civic-block.json", NW, "Datum", "", 0),
 ("cafe-railing-geometry.js", "Chrome terrace and stair rails, few sides at play zoom", "café terrace, steps", "CafeTerrace wall + rails (cafe_grand.py)", TP, "Vela", "compare rail profile", 0),
 ("cafe-stair-rails.js", "Shared rail dimensions and navigation footprints", "café steps", "same", TP, "Vela", "", 0),
 ("cafe-static-light-assignments.js", "Compiles out lights that cannot reach a static mesh", "renderer", "URP", NW, "Glint", "three.js specific", 0),
 ("cafe-table-kit.js", "Table, chair, parasol, lamp templates instanced", "café terrace", "CafeTable/Chair/Parasol/Lamp_Testbed", P, "Vela", "normal and ORM maps to 1024 x 512 (source 2048 x 1024), cloth sheen dropped (cafe_testbed/tex_cafe3d)", 1),
 ("cafe-table-lamps.js", "Atom-age table lamp, instance-tinted emission", "café tables, night", "CafeLamp_Testbed + TableLamp_* (lamps026.py)", P, "Vela", "", 1),
 ("cafe-terrace.js", "Terrace paving, reflection field, enamel finish", "café terrace", "Ground limestone/teal tile (pave026.py)", IP, "Vela", "", 1),
 ("cafe-waitress-routine.js", "Closed service aisle round the tables", "café terrace", "Pulse's routine (the café has one server outside, GDD v0.23)", TP, "Pulse", "", 0),
 ("cafe-waitress.js", "Waitress figure placement and animation", "café terrace", "tools/figures (Forma)", TP, "Forma", "", 0),
 ("chair-bake.js", "Bakes a chair into camera-facing sprites (front/left/right)", "chair-bake.html", "CafeChair_Testbed, real 3D", NW, "Vela", "an impostor: compression", 0),
 ("civic-boarding-pavilion-layout.js", "Placements: official bay scale 1.25 (15 m), getaway shelter 0.70 (8.4 m); supports as solids", "official and getaway bays", "BoardingPavilion_TB_Office / _Square (this brief); placement Splice", IP, "Splice", "", 0),
 ("civic-boarding-pavilion.js", "Loads pavilion B (3 LODs), places and LODs it", "bays", "BoardingPavilion_TB_* (28,766 tris, no LODs)", IP, "Splice", "testbed LOD1 12,000 / LOD2 5,999 not taken (Sky reviews)", 0),
 ("civic-control-cabinet-layout.js", "Cabinet placement, foreground between the planting groups", "street", "civic-control-cabinet (civic-block.json)", P, "Splice", "", 1),
 ("civic-control-cabinet.js", "Loads the service cabinet C v2", "street", "CivicCabinet (cabinet.py)", P, "Vela", "atlas at 1024 (the old runtime set); L1 0.42, L2 0.12 decimation", 1),
 ("civic-distant-ground.js", "Scenery ground beyond the block: no navigation or picking", "block edge", "ground.py horizon/ground set", TP, "Vela", "compare the edge ground", 0),
 ("civic-district-guard-layout.js", "District guard start points", "plaza", "Pulse routes", NW, "Pulse", "layout belongs to the mission", 0),
 ("civic-district-look.js", "Uniform-only art direction (grade) with no extra passes", "whole scene", "Glint's look (briefs/026/glint)", IP, "Glint", "", 0),
 ("civic-drain-grilles.js", "Road drain grilles: top, bevel only", "gutters", "DrainGrate (ground.py)", TP, "Vela", "compare shape and spacing", 0),
 ("civic-forest-hybrid.js", "Grove backdrop: shared maps, separate bounds", "block edge backdrop", "none", TP, "Vela", "decide with Datum whether groves frame the edge; skyline frames it today", 0),
 ("civic-forest-layout.js", "Backdrop roots outside the held district", "block edge", "none", TP, "Datum", "", 0),
 ("civic-forest.js", "Reuses the planter's folded leaves as 3D trees", "block edge", "none", TP, "Vela", "", 0),
 ("civic-guard-uniform.js", "Guard outfit by bone weights on one body atlas", "guards", "tools/figures (Forma)", TP, "Forma", "", 0),
 ("civic-office-guard-layout.js", "Door guard post: leaves the door axis, steps and ramp clear", "Records Office door", "mission.json door points", NW, "Waymark", "", 0),
 ("civic-office-guard.js", "Office guard using the patrol's assets", "Records Office door", "tools/figures (Forma)", TP, "Forma", "", 0),
 ("civic-office-layout.js", "Office access dimensions from the authored mesh", "Records Office", "civic-block.json (Oct 9 scale, C-0)", NW, "Datum", "the civic block stays as built (v0.24)", 0),
 ("civic-office-lighting.js", "Occupied-room pools light inner walls and ceiling", "Records Office windows, night", "Glint's night plan", TP, "Glint", "", 0),
 ("civic-office-materials.js", "Office palette and finish, shared image cache", "Records Office", "civic_records.py materials", TP, "Vela", "compare banner cloth and roof panel maps", 0),
 ("civic-office.js", "Loads the Records Office model with banners", "Records Office", "RecordsEntrance / civic_records.py (own build)", NW, "Vela", "shell locked; banner/roof art compared only", 0),
 ("civic-paving-material.js", "Ceramic paving: broad white environment highlights", "plaza paving", "Ground_PaveLimestone / TileTeal (pave026.py)", IP, "Vela", "", 1),
 ("civic-planting-layout.js", "Concept planting groups fitted to the street plan", "street", "tools/world planting + planters", TP, "Splice", "", 0),
 ("civic-road-finish.js", "Road palette and finish, independent of geometry", "road", "road_tex.py / ground_set.py (powder enamel, pearl kerb)", IP, "Vela", "ground textures are budgeted sheets (ground_tex.py)", 1),
 ("civic-road-guides.js", "Kerb mitre boundary; service-lane exits left open", "road", "tools/world street kerbs", TP, "Splice", "", 1),
 ("civic-road-layout.js", "Approved concept road topology fitted to the pilot", "road", "the plan (Datum)", NW, "Datum", "layout is the plan's", 0),
 ("civic-roads.js", "Road meshes: clipped triangle cells, not broad rectangles", "road", "ground road pieces (road_ribbon.py, ground.py)", IP, "Vela", "", 1),
 ("civic-street-light-layout.js", "Globe lamp posts along planted kerb edges", "street", "StreetLampGlobe + Streetlamp", IP, "Splice", "", 1),
 ("civic-street-props-layout.js", "Sedan and robot placement and padding", "street", "cars020 Sedan_*; Civic steward monument", TP, "Splice", "", 0),
 ("civic-street-props.js", "Loads the authored sedan and civic robot", "street", "Sedan_Getaway/Official (cars020.py); foreground-civic-robot", TP, "Vela", "compare the testbed's sedan (4.46 m) and robot", 0),
 ("civic-sun-shadows.js", "One view-local shadow map as the camera moves", "renderer", "URP shadows (Glint)", NW, "Glint", "three.js specific", 0),
 ("civic-fountain.js", "Assembles basin LODs, sculpture, water, silver, night lights", "plaza fountain", "AtomFountain_Testbed (this brief); night lights Glint", IP, "Vela", "levels gameplay (13,216 total) and near (22,480) not taken", 0),
 ("complex-movement.js", "Group movement tickets and slots", "crew moves", "sim/Suborned.Sim", NW, "Keel", "", 0),
 ("complex-navigation.js", "Navigation from floors, solids and portals", "movement", "sim/Suborned.Sim Grid.cs", NW, "Keel", "", 0),
 ("control-cabinet-lod.js", "Cabinet LOD swap sharing textures", "cabinet", "kit LODs", P, "Vela", "", 1),
 ("control-cabinet-preview.js", "Cabinet review page", "control-cabinet.html", "none", NW, "Vela", "", 0),
 ("cooperative-planner.js", "Shared nav search budget", "crew moves", "sim", NW, "Keel", "", 0),
 ("decokit-sky-shader.js", "GLSL port of DK_SpaceSky", "sky", "DK_SpaceSky shader (DecoKit, in Unity)", P, "Glint", "", 0),
 ("decokit-sky.js", "Reuses DecoKit sky textures and celestial placements", "sky", "kit sky, sky_sheet.py", P, "Glint", "", 0),
 ("entities.js", "Instanced animated people from VAT data", "crowd", "tools/figures (Forma)", TP, "Forma", "VAT far/near LODs are reductions", 0),
 ("environment.js", "Environment map setup", "renderer", "Glint", NW, "Glint", "", 0),
 ("flower-bed-compare.js", "Side-by-side flower looks", "flower-bed-compare.html", "none", NW, "Vela", "", 0),
 ("flower-bed-layout.js", "Tangent-connected sections become one footprint", "planters", "PlanterRun / PlanterArc", TP, "Vela", "", 0),
 ("flower-bed-preview.js", "Flower bed review page", "flower-beds.html", "none", NW, "Vela", "", 0),
 ("flower-look-candidates.js", "Candidate looks", "review", "none", NW, "Vela", "", 0),
 ("flower-look-lighting.js", "Flower lighting probe (166 lines)", "review", "none", NW, "Glint", "", 0),
 ("flower-look-shapes.js", "Flower head shapes in unit space", "planters", "Bloom pieces (plants.py)", TP, "Vela", "", 0),
 ("flower-optimization-cards/ (dir)", "Cards for flower LOD", "planters", "none", NW, "Vela", "an impostor / LOD technique", 0),
 ("flower-optimization-geometry/ (dir)", "Reduced flower geometry", "planters", "none", NW, "Vela", "reduction", 0),
 ("forest-impostor-atlas.js", "Bakes unlit colour and normals into tree impostors", "backdrop groves", "none", NW, "Vela", "impostors are compression", 0),
 ("fountain-garden-layout.js", "Four curved garden islands round the fountain", "plaza", "fountain-garden-* planters (civic-block.json)", TP, "Vela", "", 0),
 ("fountain-garden-lights.js", "Chrome and opal atomic globe lamp, instanced in the islands", "plaza, night", "none (PlanterArc has no lamp)", TP, "Vela", "", 0),
 ("fountain-garden.js", "Island walls with smooth normals round the curve", "plaza", "PlanterArc", TP, "Vela", "", 0),
 ("fountain-geometry.js", "Basin geometry: plinth, fluted drum, 32 coping blocks, jets", "plaza fountain", "AtomFountain_Testbed (baked: art/fountain/basin-close.json)", IP, "Vela", "finest level only", 0),
 ("fountain-guard-layout.js", "Guard post", "plaza", "Pulse", NW, "Pulse", "", 0),
 ("fountain-guard.js", "Guard circuit outside the island beds", "plaza", "Pulse routes", NW, "Pulse", "", 0),
 ("fountain-plaza.js", "Blue ceramic ring, approach bands and walkway border: 205 tiles, 3,506 tris", "plaza paving", "none", TP, "Vela", "", 0),
 ("fountain-preview.js", "Fountain review page", "fountain.html", "briefs/028/fountain", NW, "Vela", "", 0),
 ("fountain-reflection.js", "Time-sliced local cubemap probe of the actual scene", "fountain, silver", "URP reflection probe", TP, "Glint", "", 0),
 ("fountain-sculpture.js", "Loads the sculpture LODs and spins three orbits", "fountain", "AtomFountain_Testbed parts Orbit0..2 (Latch spins them)", IP, "Latch", "", 0),
 ("fountain-silver.js", "Mapped polished silver with wet contact near the water", "fountain globe", "DK_Fountain_Silver (wet band not carried)", IP, "Glint", "", 0),
 ("fountain-spec.js", "Dimensions: r 3.3, rim 0.52, water 0.39, height 4.2, anchor 0.29", "fountain", "same numbers in AtomFountain_Testbed", P, "Vela", "", 0),
 ("fountain-water.js", "Water shader: mosaic floor, waves, caustics, impact ripples, jets flow", "fountain pool", "DK_Fountain_Water static mosaic only", TP, "Glint", "shader not ported", 0),
 ("garden-botanical-geometry.js", "Distant leaf with centre vein and rolled section", "gardens", "plants.py", TP, "Vela", "", 0),
 ("garden-cache.js", "Cache key for generated gardens", "build", "none", NW, "Splice", "", 0),
 ("garden-render-batches.js", "Per-material batches keep culling bounds", "gardens", "Unity batching", NW, "Latch", "", 0),
 ("garden-runtime.js", "Same runtime path for authored and baked gardens", "gardens", "none", NW, "Latch", "", 0),
 ("garden-stem-geometry.js", "Flower stems with coordinate-derived variation", "gardens", "Bloom/BedTuft", TP, "Vela", "", 0),
 ("layout-data.js", "World coordinates for the district", "layout", "the plan (Datum)", NW, "Datum", "", 0),
 ("layout-planting.js", "Planting in measured beds", "layout", "tools/world planting", TP, "Splice", "", 0),
 ("layout-vegetation.js", "Instanced review foliage by material", "layout", "none", NW, "Vela", "", 0),
 ("layout-view.js", "Ring road round the civic island", "layout", "the plan", NW, "Datum", "", 0),
 ("light-slots.js", "Keeps light counts stable so programs do not recompile", "renderer", "URP", NW, "Glint", "three.js specific", 0),
 ("main.js", "Testbed entry: scene, footprints, loop", "testbed", "Unity scene (Latch)", NW, "Latch", "", 0),
 ("marble-horizon-config.js", "Trial links open the baked panorama", "horizon", "kit skyline (towers, rockets, planets)", NW, "Glint", "a 2D panorama / splat bake", 0),
 ("marble-horizon-viewer.js", "Splat viewer", "marble-horizon.html", "none", NW, "Glint", "", 0),
 ("marble-horizon.js", "Sky dome sampled by orientation from a baked panorama", "horizon", "kit skyline 3D", NW, "Glint", "100k and 500k splats / KTX2 panorama: compression", 0),
 ("material-environment.js", "Applies envMapIntensity to explicit maps (r163+)", "renderer", "URP", NW, "Glint", "", 0),
 ("material-reflection-colour.js", "Reflection colour response control", "renderer", "Glint", NW, "Glint", "", 0),
 ("mission-cast-layout.js", "Staging of the driver and Vale outside", "Records Office", "mission.json", NW, "Waymark", "", 0),
 ("mission-cast.js", "Driver and Vale figures", "mission", "tools/figures (Forma)", TP, "Forma", "", 0),
 ("original-player.js", "Original player figure", "testbed", "tools/figures", TP, "Forma", "", 0),
 ("pavilion-downlights.js", "Four recessed downlights (spot + bounce) and opal lens emissive 1.2 / 3.2", "bays, night", "lens = DK_Pavilion_Opal; lights Glint", IP, "Glint", "", 0),
 ("player-assets.js", "Loads cell models", "cell", "tools/figures", TP, "Forma", "", 0),
 ("player-character.js", "Cell character LOD by projected height", "cell", "tools/figures", TP, "Forma", "", 0),
 ("player-group.js", "Group slots, reachable separated points", "cell", "sim", NW, "Keel", "", 0),
 ("player-start-layout.js", "Cell starts in front of the getaway car on the open road", "mission start", "mission.json", NW, "Waymark", "", 0),
 ("projected-volume.js", "Projection authored once against the source camera", "2.5D plate", "none", NW, "Latch", "2.5D plate technique", 0),
 ("projection.js", "Camera projection helpers", "2.5D", "none", NW, "Latch", "", 0),
 ("registered-scene-plate.js", "Registers painted scene plates to the 3D camera", "2.5D", "none", NW, "Latch", "painted plates, superseded by live 3D", 0),
 ("render-meter.js", "Non-blocking performance measures", "dev", "Unity profiler", NW, "Redline", "", 0),
 ("render.js", "Renderer shim", "testbed", "none", NW, "Latch", "", 0),
 ("rigged-citizen.js", "Skinned citizens with own skeleton binds", "crowd", "tools/figures", TP, "Forma", "", 0),
 ("road-ribbon-controls.js", "Controls for the ribbon lights on the road", "road", "RoadRibbon_* (road_ribbon.py)", IP, "Vela", "", 1),
 ("scene-asset-visibility.js", "Parent gate that keeps animation and LOD visibility", "dev", "none", NW, "Latch", "", 0),
 ("scene-inspector.css", "Inspector styling", "dev", "none", NW, "Latch", "", 0),
 ("scene-inspector.js", "Click-to-edit uniforms inspector", "dev", "none", NW, "Latch", "", 0),
 ("simulation-clock.js", "One clock for movement and scenery animation", "testbed", "sim clock", NW, "Keel", "", 0),
 ("sky-backdrop.js", "Fills the viewport behind an orthographic camera", "sky", "Unity skybox", NW, "Glint", "", 0),
 ("style.css", "Page style", "dev", "none", NW, "Latch", "", 0),
 ("surface-texture-manifest.json", "Manifest of KTX2 surface textures", "build", "ground.json", NW, "Splice", "records texture compression (KTX2 UASTC, full resolution per the testbed's own policy)", 0),
 ("surface-textures.js", "Loads KTX2 surface textures", "renderer", "Unity import", NW, "Latch", "", 0),
 ("web-model-loader.js", "GLB loader with meshopt and KTX2", "renderer", "FBX import", NW, "Latch", "", 0),
 ("window-test-art.js", "Surface textures for the curved-window test (never a room picture on glass)", "window-test.html", "CafeInterior_Testbed behind CafeBay_v2 glazing", IP, "Vela", "", 1),
 ("window-test.js", "Curved window onto a common room coordinate system", "window-test.html", "same", IP, "Vela", "", 1),
 ("world.js", "Inverse-projected image rectangle bounds the world", "2.5D", "none", NW, "Latch", "", 0),
]
# folder, what, where, today, status, owner, compression, 026, thumbnail path (relative to STORE or REPO)
ART = [
 ("boarding-pavilion/", "Pavilion B: Meshy v1 source glb (28,190 tris, 4096 maps) and the testbed's 'clean-authored' LOD0/1/2 (4,748 / 1,988 / 596)", "official and getaway bays", "BoardingPavilion_TB_* from the source glb", IP, "Vela", "the 4,748 LODs and web KTX2/meshopt copies are reductions: not used", 0, "REPO:briefs/028/bay/raw/square-testbed-day.png"),
 ("boarding-pavilion-cleaned/", "A restyled rebuild (web glb LODs)", "discarded by the testbed itself (preserved decision)", "none", NW, "Vela", "a rebuild", 0, None),
 ("boarding-pavilion-illuminated/", "The signed-off pavilion: untouched body + four appended fittings (+576 tris); KTX2 web copies", "the testbed's civic square", "ported: fittings rebuilt from its script (art/pavilion)", IP, "Vela", "KTX2 ETC1S/UASTC, meshopt; LODs 12,000 / 5,999 decimations", 0, None),
 ("boarding-pavilion-preserved/", "The full original (28,190) with decimated LOD1/2 as FBX/glb", "testbed review", "source = boarding-pavilion/source glb", NW, "Vela", "LOD1 12,000, LOD2 5,999", 0, None),
 ("cafe-3d/", "Café model: shell + room contents (cafe-3d.blend/glb, 37,810 tris)", "café", "CafeInterior_Testbed (contents), shell stays Oct 9 (C-0)", IP, "Vela", "", 1, None),
 ("cafe-multiview/", "Four-elevation art and guide layers, baked colour for the café", "cafe-multiview.html", "reference only", NW, "Vela", "baked colour plates", 0, "cafe-multiview/approved-cafe-reference.png"),
 ("cafe-people/", "Café patrons, guards: LOD GLBs with VAT", "café", "tools/figures", TP, "Forma", "VAT far/near LODs", 0, None),
 ("cafe-pilot/", "Painted café front pilot (v1-v3) and guides", "2.5D plate", "concept reference (briefs/008/concepts)", NW, "Vela", "", 0, "cafe-pilot/cafe-front-v3.png"),
 ("cafe-plants/", "Botanical atlas, foliage, soil maps and reports", "planters, terrace", "plants.py textures", TP, "Vela", "", 0, "cafe-plants/botanical-atlas-v2.png"),
 ("cafe-roof/", "Roof enamel base / normal / ORM", "café roof", "none found in the kit", TP, "Vela", "", 0, "cafe-roof/roof-enamel-base-v1.png"),
 ("cafe-table-set/", "Table, chairs, parasol, lamp: sources, PBR glb, atlas", "café terrace", "CafeSet_Testbed, Parasol_Testbed, CafeLamp_Testbed", P, "Vela", "normal/ORM to 1024 x 512 from 2048 x 1024; sheen dropped", 1, "cafe-table-set/furniture-base.png"),
 ("cafe-terrace/", "Limestone paving base/normal/ORM", "café terrace", "Ground_PaveLimestone (pave026.py)", IP, "Vela", "", 1, "cafe-terrace/limestone-base.png"),
 ("chair-bake/", "Camera-facing chair sprites", "chair-bake.html", "CafeChair_Testbed", NW, "Vela", "sprites: compression", 0, "chair-bake/baked-front.png"),
 ("civic-fountain/", "Atom fountain maps (marble/ceramic/polish/mosaic), sculpture glbs (gameplay 5,072 / near 10,576 / close 20,000), HDR", "plaza", "AtomFountain_Testbed", IP, "Vela", "gameplay and near not taken; ambientCG 2k normal/ORM were already 1k in the testbed", 0, "REPO:briefs/028/fountain/raw/fountain-testbed-day.png"),
 ("civic-office/", "Records Office glb/blend, banner cloth, roof panel art", "Records Office", "civic_records.py (own build); civic block stays as built", NW, "Vela", "", 0, "civic-office/banner-cloth-base-v1.png"),
 ("civic-street-props/", "Authored sedan and civic robot (glb/blend)", "street", "cars020 Sedan_*; robots", TP, "Vela", "", 0, None),
 ("control-cabinet/", "Service cabinet C: v1 Meshy, v2 repaired, runtime LODs", "street", "CivicCabinet", P, "Vela", "atlas 1024 (v2 maps scaled), L1 0.42 / L2 0.12", 1, "control-cabinet/v2/textures/Image_0.jpg"),
 ("crowd/", "People GLBs with VAT animation (near, far)", "crowd", "tools/figures", TP, "Forma", "far/near LODs; VAT exact", 0, None),
 ("decokit-sky/", "Sky textures: moon, planets, noise, far skyline", "sky", "DecoKit sky in Unity", P, "Glint", "", 0, "decokit-sky/DK_Skyline_Far.webp"),
 ("district-road/", "Road enamel, pearl, gold, marble, asphalt maps and finish", "road", "ground_set.py / road_tex.py", IP, "Vela", "ground_tex budgets the maps", 1, "district-road/enamel-source-v1.png"),
 ("marble-horizon/", "Baked splat/KTX2 panorama of the horizon", "horizon", "kit skyline 3D", NW, "Glint", "100k/500k splats", 0, "marble-horizon/thumb.webp"),
 ("mission-cast/", "Driver and Vale GLBs", "mission", "tools/figures", TP, "Forma", "", 0, None),
 ("mobility/", "RetroFutureMobility: air, ground, rail, robots, people, streetscape, transit", "city", "mobility.py, robots.py, skyport", IP, "Vela", "check each against mobility.py", 0, "mobility/CityGround/Textures/RFG_PublicWalkway_PearlSatin_Albedo.png"),
 ("palm/", "Street palm: close 19,330 / near 6,102 / gameplay tris, maps, blend", "street trees", "CoconutPalm_Street (4.8k, from the gameplay level)", IP, "Vela", "from the lowest level: the close level is not used", 1, "palm/palm-surface-atlas-v1.png"),
 ("players/", "The cell: enforcer, scout, tech", "cell", "tools/figures", TP, "Forma", "", 0, None),
 ("reference/", "The 2.5D concept sheet", "reference", "briefs/008/concepts", NW, "Vela", "", 0, "reference/concept-v1.png"),
 ("window-test/", "Back wall and surface atlas for the curved-window room", "café glass", "CafeInterior_Testbed", IP, "Vela", "", 1, "window-test/surface-atlas-v1.png"),
 ("(top level) environment/hidden-ground/depth/coverage/*mask .png, suborned-civic-2p5d-concept-v1.png", "2.5D plates, depth and masks for the painted scene", "2.5D testbed", "none: live 3D", NW, "Vela", "plates", 0, "suborned-civic-2p5d-concept-v1.png"),
 ("(top level) animation-bounds.json, provenance.json, surface-texture-compression.json", "Bounds, provenance and the KTX2 compression record", "build", "ground.json, PROVENANCE.json", NW, "Splice", "records the testbed's KTX2 policy", 0, None),
]
COMPRESSION = [
 ("Today's kit", "kit.asset(lean, cap) and the automatic LODs: every generated piece over 400 tris gets an L1 by collapse decimation (25%), over 1,500 an L2 (6%); only pieces marked nolod (the three here) are exempt", "export_kit.py LOD_SPEC; restore026.py undid the lean/cap cuts on the old props"),
 ("Today's kit", "ground_tex.py: the ground maps are budgeted sheets (the old 4096 x 3 maps, 64 MiB, replaced)", "mats.py GROUND comment"),
 ("Today's kit", "Café testbed port: furniture normal and ORM 2048 x 1024 to 1024 x 512, sheen map dropped, counter art 1254 to 1024; CafeInterior_Testbed has L1 4,638 / L2 1,087", "tex_cafe3d.py; kit.json"),
 ("Today's kit", "CivicCabinet: atlas at 1024 (the old runtime set), shared gold at 256; L1 0.42 / L2 0.12", "cabinet.py"),
 ("Today's kit", "CoconutPalm_Street: 4.8k tris built from the testbed's gameplay palm; the close level is 19,330 and the near 6,102", "palms026.py"),
 ("Testbed source", "boarding-pavilion: LOD1 12,000 and LOD2 5,999 (decimated), the 'cleaned' rebuild at 4,748 (28,190 reduced 83%), KTX2 (ETC1S/UASTC) and meshopt web copies", "optimization.json"),
 ("Testbed source", "civic-fountain: gameplay 13,216 and near 22,480 totals; the ambientCG Marble012 2k archive was already used at 2048 / 1024 / 1024; KTX2 web copies", "model-report.json"),
 ("Testbed source", "crowd and cafe-people: @near and @far LODs; marble-horizon: 100k and 500k splat levels; chair-bake and flower cards: impostors", "folders above"),
 ("Testbed source", "The testbed's own policy: KTX2 at full source resolution, UASTC (surface-texture-compression.json)", ""),
]


def thumb(p):
    if not p:
        return ""
    f = REPO / p[5:] if p.startswith("REPO:") else STORE / p
    try:
        im = Image.open(f).convert("RGB")
    except Exception:
        return ""
    im.thumbnail((150, 110))
    b = io.BytesIO()
    im.save(b, "JPEG", quality=78)
    return f'<img src="data:image/jpeg;base64,{base64.b64encode(b.getvalue()).decode()}" width="{im.width}" height="{im.height}">'


def badge(s):
    return f'<span class="s {s.replace(" ", "")}">{s}</span>'


def rows(data, art):
    out = []
    for r in data:
        name, what, where, today, st, owner, comp, c26 = r[:8]
        th = thumb(r[8]) if art else ""
        out.append(f"<tr><td class=n>{name}{' <span class=b26>026</span>' if c26 else ''}</td>" + (f"<td>{th}</td>" if art else "") +
                   f"<td>{what}</td><td>{where}</td><td>{today}</td><td>{badge(st)}</td><td>{owner}</td><td class=c>{comp}</td></tr>")
    return "\n".join(out)


def count(data):
    d = {}
    for r in data:
        d[r[4]] = d.get(r[4], 0) + 1
    return ", ".join(f"{v} {k}" for k, v in d.items())


def main():
    head = "<th>Module</th><th>What it is</th><th>Where it shows</th><th>Today's equivalent</th><th>Status</th><th>Owner</th><th>Compression (kit or testbed source)</th>"
    html = f"""<!doctype html><meta charset=utf-8><title>028 Vela: testbed inventory</title>
<style>body{{font:13px/1.4 -apple-system,Helvetica,sans-serif;max-width:1700px;margin:2em auto;padding:0 1em;background:#fbf7ee;color:#222}}table{{border-collapse:collapse;width:100%}}td,th{{border:1px solid #cbbf9f;padding:3px 7px;vertical-align:top}}th{{background:#efe5cc;text-align:left;position:sticky;top:0}}.n{{font-family:ui-monospace,Menlo,monospace;white-space:nowrap}}.c{{color:#7a3b00}}.s{{padding:1px 6px;border-radius:9px;color:#fff;font-size:11px;white-space:nowrap}}.ported{{background:#2f8f5b}}.inprogress{{background:#c78a00}}.toport{{background:#c0392b}}.notwanted{{background:#777}}.b26{{background:#2a6fb5;color:#fff;border-radius:9px;padding:0 5px;font-size:10px}}img{{border:1px solid #ccc;display:block}}h2{{margin-top:2em}}</style>
<h1>028 Vela: the testbed against this repo</h1>
<p>2026-10-20. Sky: "as much as possible from testbed should come over to new." Sources: <code>~/Projects/the-suborned/testbed/src</code> (modules) and <code>~/Projects/the-suborned-store/testbed-art</code> (art). Status: <b>ported</b> (a kit piece or in the game) / <b>in progress</b> (brief 026 or this brief, candidate not yet adopted) / <b>to port</b> / <b>not wanted</b> (reason in the columns). <span class=b26>026</span> = brief 026 already covers it (road, paving, café lighting, sign, parasols and interior, palms, old props, run_lean). Compression column: anything reduced in today's kit or in the testbed's own source, for Sky's review; the new ports take no compression.</p>
<p><b>Modules:</b> {count(SRC)} ({len(SRC)} rows). <b>Art:</b> {count(ART)} ({len(ART)} rows).</p>
<p><b>This brief:</b> car bay and fountain are candidates in contracts/world/kit.json (BoardingPavilion_TB_Square, BoardingPavilion_TB_Office, AtomFountain_Testbed); renders in briefs/028/bay and briefs/028/fountain.</p>
<h2>Compression register</h2><table><tr><th>Where</th><th>What was reduced</th><th>Source</th></tr>
{''.join(f'<tr><td>{a}</td><td>{b}</td><td class=n>{c}</td></tr>' for a, b, c in COMPRESSION)}</table>
<h2>testbed-art folders</h2><table><tr><th>Folder</th><th>Preview</th><th>What it is</th><th>Where it shows</th><th>Today's equivalent</th><th>Status</th><th>Owner</th><th>Compression</th></tr>
{rows(ART, True)}</table>
<h2>testbed/src modules</h2><table><tr>{head}</tr>
{rows(SRC, False)}</table>"""
    OUT.write_text(html)
    print(OUT, len(SRC), len(ART), count(SRC), "|", count(ART))


main()
