extends Node3D

const Surface = preload("res://arcade_material.gdshader")
const Field = preload("res://assets/arcade-field.res")
const LOOKS = ["original", "arcade", "surfaced"]
const NAMES = ["Original daylight", "Arcade lighting", "Arcade lighting and finishes"]
var look = "surfaced"
var city
var sunlight: DirectionalLight3D
var standard_lamps = []
var surfaces = {}
var ground = []
var characters = []
var kit_surface: ShaderMaterial
var on = false


func prepare(street):
    city = street
    sunlight = city.streetscape.find_children("*", "DirectionalLight3D", true, false)[0]
    standard_lamps = city.streetscape.find_children("*", "OmniLight3D", true, false)
    var streetscape = city.streetscape
    kit_surface = surface(city.block.catalog["Vault_L0"].surface_get_material(0))
    kit_surface.set_shader_parameter("architecture", true)
    for obj in streetscape.fixtures + streetscape.merged:
        var mat = obj.mesh.surface_get_material(0)
        if mat == streetscape.materials["slab"] or mat == streetscape.materials["road"]:
            ground.append([obj, surface(mat, true)])
    var actors = [city.guide]
    for person in city.people:
        actors.append(person[0])
    for actor in actors:
        for mesh in actor.find_children("*", "MeshInstance3D", true, false):
            for slot in mesh.mesh.get_surface_count():
                characters.append([mesh, slot, surface(mesh.mesh.surface_get_material(slot))])
    make_counter_lamps()


func surface(original: StandardMaterial3D, pavement = false) -> ShaderMaterial:
    if surfaces.has(original):
        return surfaces[original]
    var m = ShaderMaterial.new()
    m.shader = Surface
    m.set_shader_parameter("arcade_field", Field)
    m.set_shader_parameter("base_map", original.albedo_texture)
    m.set_shader_parameter("tint", original.albedo_color)
    m.set_shader_parameter("metal", original.metallic)
    m.set_shader_parameter("rough", original.roughness)
    if original.normal_enabled:
        m.set_shader_parameter("bump", original.normal_texture)
        m.set_shader_parameter("bump_depth", original.normal_scale)
    if original.emission_enabled:
        m.set_shader_parameter("lamps", original.emission_texture)
        m.set_shader_parameter(
            "lamp_energy", original.emission.srgb_to_linear().r * original.emission_energy_multiplier
        )
    if original.roughness_texture:
        m.set_shader_parameter("surface_map", original.roughness_texture)
        m.set_shader_parameter("packed_surface", original.roughness_texture == original.metallic_texture)
        m.set_shader_parameter("rough_map", true)
    m.set_shader_parameter("paving", pavement)
    m.set_shader_parameter("paving_scale", original.uv1_scale.x)
    m.set_shader_parameter("stone_tile", preload("res://assets/finish_stone.png"))
    m.set_shader_parameter("glaze_tile", preload("res://assets/finish_faience.png"))
    m.set_shader_parameter("drawn_metal", preload("res://assets/finish_metal.png"))
    m.set_shader_parameter("roof_sheet", preload("res://assets/finish_roof.png"))
    m.set_shader_parameter("postal_plates", preload("res://assets/postal_plates.png"))
    m.set_shader_parameter("sorting_rooms", preload("res://assets/sorting_rooms.png"))
    m.set_shader_parameter("door_coat", preload("res://assets/collection_coat.png"))
    m.set_shader_parameter("door_finish", preload("res://assets/collection_finish.png"))
    surfaces[original] = m
    return m


func make_counter_lamps():
    var glass = StandardMaterial3D.new()
    glass.albedo_color = Color("#f8e7c2")
    glass.emission_enabled = true
    glass.emission = Color("#ffdda4")
    glass.emission_energy_multiplier = 1.65
    var strip = BoxMesh.new()
    strip.size = Vector3(3.85, .055, .18)
    var tool = SurfaceTool.new()
    tool.begin(Mesh.PRIMITIVE_TRIANGLES)
    for x in [-6., 0., 6., 12.]:
        var source = Vector3(x, 3.85, -1.22)
        tool.append_from(strip, 0, Transform3D(Basis.IDENTITY, source))
        var light = SpotLight3D.new()
        add_child(light)
        light.position = source - Vector3.UP * .10
        light.look_at(Vector3(x, 1., -2.92))
        light.light_color = Color("#ffd09b")
        light.light_energy = 5.4
        light.spot_range = 5.8
        light.spot_angle = 53.
        light.spot_angle_attenuation = 1.15
        light.shadow_enabled = false
    tool.set_material(glass)
    var fittings = MeshInstance3D.new()
    fittings.mesh = tool.commit()
    fittings.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
    add_child(fittings)


func set_look(next: String):
    look = next if next in LOOKS else "surfaced"
    var enabled = look != "original"
    kit_surface.set_shader_parameter("dressed", look == "surfaced")
    for pair in ground:
        pair[1].set_shader_parameter("dressed", look == "surfaced")
    on = enabled
    visible = enabled
    for lamp in standard_lamps:
        lamp.visible = not enabled
    for pair in ground:
        pair[0].material_override = pair[1] if enabled else null
    for pair in characters:
        pair[0].set_surface_override_material(pair[1], pair[2] if enabled else null)
    apply_block()
    var env = city.streetscape.environment
    sunlight.rotation_degrees = Vector3(-27, -108, 0) if enabled else Vector3(-35, -61, 0)
    sunlight.light_color = Color("#ffcf9c") if enabled else Color("#ffe3bf")
    sunlight.light_energy = 2.02 if enabled else 1.33
    if city.court and enabled:
        sunlight.light_energy = 1.59
    sunlight.light_angular_distance = .52 if enabled else 0.
    env.ambient_light_color = Color("#b4cce8") if enabled else Color.BLACK
    env.ambient_light_sky_contribution = .70 if enabled else 1.0
    env.ambient_light_energy = .80 if enabled else .53
    env.sky.sky_material.set_shader_parameter("low_sun", enabled)
    env.sky.sky_material.set_shader_parameter(
        "arcade",
        (
            preload("res://assets/arcade_latelight.jpg")
            if enabled
            else preload("res://assets/arcade_reflection.jpg")
        )
    )


func apply_block():
    for batch in city.block.batches:
        batch.material_override = kit_surface if on else null
