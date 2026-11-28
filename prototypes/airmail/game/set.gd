extends Node3D

var materials = {}
var tram
var fixtures = []
var merged = []
var environment: Environment
const PALM = preload("res://assets/palm.glb")
const TRAM = preload("res://assets/tram.glb")


func finish(id, col, metal = 0., rough = .62):
    var m = StandardMaterial3D.new()
    m.albedo_color = Color(col)
    m.metallic = metal
    m.roughness = rough
    materials[id] = m
    return m


func pavement(id, texture, normal, rough, scale3):
    var m = finish(id, "#ffffff", 0., rough)
    m.albedo_texture = load(texture)
    m.normal_enabled = true
    m.normal_texture = load(normal)
    m.normal_scale = .58
    m.uv1_triplanar = true
    m.uv1_world_triplanar = true
    m.uv1_scale = Vector3.ONE * scale3
    m.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC
    return m


func box(pos, size, id, casts = true):
    var mesh = BoxMesh.new()
    mesh.size = size
    return add_fixture(pos, mesh, id, casts)


func turned_section(pos: Vector3, radius: float, upper: float, height: float, material: String):
    var mesh = CylinderMesh.new()
    mesh.top_radius = upper
    mesh.bottom_radius = radius
    mesh.height = height
    mesh.radial_segments = 14
    mesh.rings = 0
    return add_fixture(pos, mesh, material, true)


func add_fixture(pos, mesh, id, casts):
    mesh.material = materials[id]
    var obj = MeshInstance3D.new()
    obj.mesh = mesh
    obj.position = pos
    add_child(obj)
    if not casts:
        obj.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
    fixtures.append(obj)
    return obj


func setup():
    var env = Environment.new()
    environment = env
    var sky = Sky.new()
    var pan = ShaderMaterial.new()
    pan.shader = preload("res://daylight.gdshader")
    pan.set_shader_parameter("daylight", preload("res://assets/sky.hdr"))
    pan.set_shader_parameter("arcade", preload("res://assets/arcade_reflection.jpg"))
    sky.sky_material = pan
    sky.radiance_size = Sky.RADIANCE_SIZE_256
    env.background_mode = Environment.BG_SKY
    env.sky = sky
    env.background_energy_multiplier = .85
    env.ambient_light_source = Environment.AMBIENT_SOURCE_SKY
    env.ambient_light_energy = .53
    env.reflected_light_source = Environment.REFLECTION_SOURCE_SKY
    env.tonemap_mode = Environment.TONE_MAPPER_ACES
    env.tonemap_exposure = .9
    env.tonemap_white = 5.5
    env.ssao_enabled = true
    env.ssao_radius = 1.15
    env.ssao_intensity = 1.3
    env.fog_enabled = false
    env.fog_light_color = Color("#a3baca")
    env.fog_density = .0019
    env.fog_sky_affect = .018
    var world = WorldEnvironment.new()
    world.environment = env
    add_child(world)
    shoreline()
    var sun = DirectionalLight3D.new()
    sun.rotation_degrees = Vector3(-35, -61, 0)
    sun.light_color = Color("#ffe3bf")
    sun.light_energy = 1.33
    sun.shadow_enabled = true
    sun.directional_shadow_max_distance = 65
    sun.shadow_bias = .036
    add_child(sun)
    pavement("slab", "res://assets/pavement.jpg", "res://assets/pavement_n.png", .53, .23)
    var road = pavement("road", "res://assets/road.jpg", "res://assets/road_n.jpg", .75, .19)
    road.roughness_texture = preload("res://assets/road_r.jpg")
    road.albedo_color = Color("#9e9c91")
    finish("rail", "#9e9d91", .83, .26)
    finish("iron", "#434a46", .56, .48)
    finish("brass", "#a98956", .72, .33)
    finish("curb", "#ac9e87", .02, .61)
    finish("soil", "#514d3d", 0, .91)
    var globe = finish("opal", "#f0ddba", .06, .31)
    globe.emission_enabled = true
    globe.emission = Color("#b39b76")
    globe.emission_energy_multiplier = .44
    box(Vector3(0, -.22, 0), Vector3(310, .44, 270), "road", false)
    box(Vector3(0, .076, -20.65), Vector3(230, .15, 55), "slab", false)
    box(Vector3(0, .076, 38.66), Vector3(230, .15, 38), "slab", false)
    for z in [6.88, 19.62]:
        box(Vector3(0, .072, z), Vector3(230, .145, .22), "curb", false)
        box(Vector3(0, .147, z), Vector3(230, .006, .045), "brass", false)
    for z in [11.5, 12.935, 15.7, 17.135]:
        box(Vector3(0, .009, z), Vector3(225, .018, .155), "iron", false)
        box(Vector3(0, .023, z), Vector3(225, .016, .072), "rail", false)
    for x in [-43, -21, 29, 50]:
        var palm = PALM.instantiate()
        palm.position = Vector3(x, .16, 5.43)
        palm.rotation.y = x * .78
        palm.scale = Vector3.ONE * (1.42 if x == -21 else 1.23)
        add_child(palm)
        box(Vector3(x, .159, 5.43), Vector3(1.63, .026, 1.56), "iron", false)
    for x in [-48.0, -26, 1, 29, 56]:
        turned_section(Vector3(x, .42, 19.95), .19, .13, .56, "iron")
        turned_section(Vector3(x, 2.52, 19.95), .09, .062, 3.68, "rail")
        turned_section(Vector3(x, 4.49, 19.95), .10, .41, .26, "opal")
        turned_section(Vector3(x, 4.64, 19.95), .53, .30, .09, "rail")
        turned_section(Vector3(x, 4.77, 19.95), .28, .045, .18, "iron")
    for x in [-6., 6.]:
        var fill = OmniLight3D.new()
        fill.position = Vector3(x, 3.25, -1.9)
        fill.omni_range = 4.7
        fill.light_color = Color("#ffd19b")
        fill.light_energy = .94
        add_child(fill)
    tram = TRAM.instantiate()
    add_child(tram)
    # Existing model is long along glTF X or Z depending on the source FBX.
    var bounds = AABB()
    var first = true
    for child in tram.find_children("*", "MeshInstance3D", true, false):
        var b = tram.global_transform.affine_inverse() * child.global_transform * child.get_aabb()
        bounds = b if first else bounds.merge(b)
        first = false
    if bounds.size.z > bounds.size.x:
        tram.rotation.y = PI * .5
    var factor = 12.6 / maxf(bounds.size.x, bounds.size.z)
    tram.scale = Vector3.ONE * factor
    tram.position.y = .04 - bounds.position.y * factor
    tram.position.z = 16.42
    var pool = {}
    for f in fixtures:
        var mat = f.mesh.material
        var k = str(mat.get_instance_id()) + ":" + str(f.cast_shadow)
        if not pool.has(k):
            pool[k] = []
        pool[k].append(f)
    for group in pool.values():
        var st = SurfaceTool.new()
        st.begin(Mesh.PRIMITIVE_TRIANGLES)
        for f in group:
            st.append_from(f.mesh, 0, f.transform)
        st.set_material(group[0].mesh.material)
        var obj = MeshInstance3D.new()
        obj.mesh = st.commit()
        obj.cast_shadow = group[0].cast_shadow
        obj.visible = false
        add_child(obj)
        merged.append(obj)


func combine_fixtures(use: bool):
    for f in fixtures:
        f.visible = not use
    for f in merged:
        f.visible = use


func presentation(enabled: bool):
    environment.fog_enabled = enabled


func shoreline():
    var mat = StandardMaterial3D.new()
    mat.albedo_texture = preload("res://assets/shore.png")
    mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR
    mat.alpha_scissor_threshold = .44
    mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
    mat.cull_mode = BaseMaterial3D.CULL_DISABLED
    mat.albedo_color = Color("#ced2cf")
    var st = SurfaceTool.new()
    st.begin(Mesh.PRIMITIVE_TRIANGLES)
    st.set_material(mat)
    var count = 64
    for i in range(count):
        var a = float(i) * TAU / count
        var b = float(i + 1) * TAU / count
        var p = Vector3(cos(a) * 145., -6, sin(a) * 145.)
        var q = Vector3(cos(b) * 145., -6, sin(b) * 145.)
        var u = float(i % 16) / 16.
        var v = float(i % 16 + 1) / 16.
        for pair in [
            [p, Vector2(u, 1)],
            [q, Vector2(v, 1)],
            [p + Vector3.UP * 42, Vector2(u, 0)],
            [q, Vector2(v, 1)],
            [q + Vector3.UP * 42, Vector2(v, 0)],
            [p + Vector3.UP * 42, Vector2(u, 0)]
        ]:
            st.set_uv(pair[1])
            st.add_vertex(pair[0])
    var mesh = MeshInstance3D.new()
    mesh.mesh = st.commit()
    mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
    add_child(mesh)


func advance(t: float):
    tram.position.x = fposmod(t * 3.4 + 37, 158.) - 79.
