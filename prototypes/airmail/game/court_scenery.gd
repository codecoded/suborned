extends Node3D

const Builder = preload("res://court_builder.gd")
const Trades = preload("res://assets/yard_trades.glb")
const GROUND = .163
const START = Vector2(-10, -15)
const POSTS = [Vector2(-9, -17), Vector2(-7, -20), Vector2(4, -13)]
const PRESS = Vector2(9, -21)
const MARKET = Vector2(12, -15)
const GATE = Rect2(2.65, -22.6, .70, 4.)
const ISLAND = Rect2(-4.8, -18.85, 9.3, 3.15)
var city
var meshes = {}
var mats = {}
var cut_groups = []
var cart: Node3D
var gate: Node3D
var market_lid: Node3D
var press_open = false
var market_open = false
var gate_lift = 0.
var market_lift = 0.
var press_wheel: Node3D


func material(code, color, rough, metal = 0.):
    var mat = StandardMaterial3D.new()
    mat.albedo_color = Color(color)
    mat.roughness = rough
    mat.metallic = metal
    mats[code] = mat
    return mat


func group(title, at = Vector3.ZERO):
    var node = Node3D.new()
    node.name = title
    add_child(node)
    node.position = at
    return node


func setup(street):
    city = street
    var lib = Trades.instantiate()
    for ob in lib.find_children("*", "MeshInstance3D", true, false):
        meshes[String(ob.name)] = ob.mesh
    lib.free()
    material("iron", "#414945", .50, .64)
    material("zincrim", "#aaa995", .31, .78)
    material("enamel", "#346c64", .33, .17)
    material("oxide", "#9d4f39", .62)
    material("plaster", "#b5ab89", .84)
    material("timber", "#7c6044", .74)
    material("canvas", "#cfb78a", .88)
    material("shadow", "#283631", .81)
    var gl = material("opal", "#f4d5a2", .35)
    gl.emission_enabled = true
    gl.emission = Color("#ffca85")
    gl.emission_energy_multiplier = 1.3
    var print_mat = material("print", "#ffffff", .69)
    print_mat.albedo_texture = preload("res://assets/yard_print.jpg")
    print_mat.cull_mode = BaseMaterial3D.CULL_DISABLED
    var blade = material("blade", "#f5e4ce", .34, .15)
    blade.albedo_texture = preload("res://assets/yard_blade.jpg")
    blade.cull_mode = BaseMaterial3D.CULL_DISABLED
    var board = material("brick", "#cfbba8", .86)
    board.albedo_texture = preload("res://assets/yard_brick.jpg")
    board.normal_enabled = true
    board.normal_texture = preload("res://assets/yard_brick_n.jpg")
    board.normal_scale = .64
    board.uv1_triplanar = true
    board.uv1_world_triplanar = true
    board.uv1_scale = Vector3.ONE * .28
    for id in ["zincrim", "enamel", "iron", "oxide"]:
        var base = mats[id]
        var coat = ShaderMaterial.new()
        coat.shader = preload("res://court_metal.gdshader")
        coat.set_shader_parameter("coat", base.albedo_color)
        coat.set_shader_parameter("rough", base.roughness)
        coat.set_shader_parameter("metal", base.metallic)
        coat.set_shader_parameter("finish", preload("res://assets/finish_metal.png"))
        mats[id] = coat
    var cloth = material("stripe", "#dacbad", .93)
    cloth.albedo_texture = preload("res://assets/yard_cloth.jpg")
    cloth.cull_mode = BaseMaterial3D.CULL_DISABLED
    for id in ["leaves", "flowers"]:
        var leaves = material(id, "#e4dbaf", .82)
        leaves.albedo_texture = (
            preload("res://assets/yard_ivy.png")
            if id == "leaves"
            else preload("res://assets/yard_flowers.png")
        )
        leaves.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR
        leaves.alpha_scissor_threshold = .52
        leaves.cull_mode = BaseMaterial3D.CULL_DISABLED
    var rooms = ShaderMaterial.new()
    rooms.shader = preload("res://court_window.gdshader")
    rooms.set_shader_parameter("rooms", preload("res://assets/yard_workrooms.jpg"))
    mats["rooms"] = rooms
    var pavement = PlaneMesh.new()
    pavement.size = Vector2(39, 22)
    var surface = ShaderMaterial.new()
    surface.shader = preload("res://court_ground.gdshader")
    surface.set_shader_parameter("setts", preload("res://assets/yard_cobbles.jpg"))
    surface.set_shader_parameter("relief", preload("res://assets/yard_cobbles_n.jpg"))
    surface.set_shader_parameter("stone_arm", preload("res://assets/yard_cobbles_arm.jpg"))
    surface.set_shader_parameter("wear", preload("res://assets/yard_wear.png"))
    surface.set_shader_parameter("asphalt", preload("res://assets/road.jpg"))
    pavement.material = surface
    var floor_mesh = MeshInstance3D.new()
    floor_mesh.mesh = pavement
    floor_mesh.position = Vector3(.5, GROUND + .002, -15)
    floor_mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
    add_child(floor_mesh)
    var fixed = Builder.new(group("Yard fittings"), mats)
    for i in range(5):
        frontage(-12. + i * 6., -22.6, 0, ["TicketBay", "FloristBay", "MarketBay"][i % 3], i)
    for x in [-10., 2., 10.]:
        frontage(x, -10.60, 180, "TicketBay" if x < 0 else "MarketBay", int(x + 16))
    preload("res://court_printworks.gd").new().build(self)
    loading_gate(fixed)
    market_hatch(fixed)
    add_prop("ProduceStall", Vector2(11.8, -12.05), 180, Vector2(3.0, 1.76))
    add_prop("FlowerBarrow", Vector2(-7.3, -21.65), 5, Vector2(1.93, 1.02))
    add_prop("PneumaticPost", Vector2(-13.1, -18.3), -45, Vector2(.94, .9))
    add_prop("LuggageTrolley", Vector2(-12.1, -12.3), 78, Vector2(.9, 1.84))
    cart = trade("LuggageTrolley", Vector3(4.0, GROUND, -14.05), 90)
    var load = Builder.new(cart, mats)
    stack_papers(load, Vector3(.27, .48, 0))
    load.finish()
    for x in [-13.2, -8.1, 1.7, 7.3, 13.0]:
        drain(fixed, Vector3(x, GROUND, -21.92))
    for item in [[-5.35, -17.6], [5.65, -17.5], [12.3, -11.36], [-11.9, -12.0]]:
        stack_papers(fixed, Vector3(item[0], GROUND, item[1]))
        city.block.solids.append(Rect2(item[0] - .44, item[1] - .29, .88, .58))
    for i in range(7):
        var x = -9.8 + i * .38
        var at = Vector3(x, GROUND + .009, -18.1 + sin(i * 5.3) * .6)
        var side = Vector3(.12, 0, 0).rotated(Vector3.UP, i * 1.7)
        var end = Vector3(0, 0, -.18).rotated(Vector3.UP, i * 1.7)
        fixed.sheet(
            at - side, at + side, at + side + end, at - side + end, "print", Rect2(.015, .012, .46, .46)
        )
    fixed.finish()
    for pair in [
        [Vector3(-6.4, 5, -22.25), Vector3(-3.6, 4.2, -18.6)],
        [Vector3(7.3, 5.1, -22.3), Vector3(4.45, 4.06, -18.5)]
    ]:
        festoon(pair[0], pair[1])
    for spot in [Vector3(-5.1, 2.55, -17.3), Vector3(-7.4, 2.7, -21.), Vector3(11.9, 2.5, -11.8)]:
        var lamp = OmniLight3D.new()
        lamp.position = spot
        lamp.light_color = Color("#ffd399")
        lamp.light_energy = 2.6
        lamp.omni_range = 4.0
        lamp.shadow_enabled = false
        add_child(lamp)


func trade(id, at: Vector3, yaw: float, parent: Node3D = null):
    var root = Node3D.new()
    (parent if parent else self).add_child(root)
    root.position = at
    root.rotation.y = deg_to_rad(yaw)
    for i in range(3):
        var item = MeshInstance3D.new()
        item.mesh = meshes[id + "_L" + str(i)]
        item.visibility_range_begin = [0., 15., 37.][i]
        item.visibility_range_end = [15., 37., 110.][i]
        item.visibility_range_begin_margin = .5
        item.visibility_range_end_margin = .5
        if i == 2 or parent != null:
            item.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
        root.add_child(item)
    return root


func add_prop(id, at: Vector2, yaw: float, size: Vector2):
    trade(id, Vector3(at.x, GROUND, at.y), yaw)
    city.block.solids.append(Rect2(at - size * .5, size))


func frontage(x, z, yaw, id, variant):
    var back = -1. if yaw == 0 else 1.
    var depth = 3.1 if yaw == 0 else 2.4
    var center = Vector3(x, GROUND, z + back * depth * .5)
    var part = group("Shop", Vector3(x, GROUND, z))
    part.rotation_degrees.y = yaw
    trade(id, Vector3.ZERO, 0, part)
    var b = Builder.new(part, mats)
    var height = 6.4 + (variant % 3) * 1.05
    b.box(Vector3(0, height * .5, -depth * .5 - .90), Vector3(5.90, height, depth), "brick")
    b.box(Vector3(0, height + .025, -depth * .5 - .78), Vector3(6.10, .18, depth + .65), "zincrim")
    if variant % 2 == 0:
        b.box(Vector3(.65, height + .45, -depth * .54), Vector3(2.7, .90, 1.3), "enamel")
        b.beam(Vector3(-1.6, height, -1.05), Vector3(-1.6, height + 1.13, -1.05), .16, "iron")
    b.beam(Vector3(-2.81, .21, .16), Vector3(-2.81, 6.83, .16), .075, "zincrim")
    for y in [.59, 2.51, 4.70]:
        b.box(Vector3(-2.81, y, .16), Vector3(.22, .045, .22), "iron")
    var cell = 1 if id == "FloristBay" else (2 if id == "MarketBay" else 3)
    var panel = Rect2((cell % 2) * .5 + .004, floori(cell / 2.) * .5 + .004, .492, .492)
    b.sheet(
        Vector3(-2.47, .26, -.59),
        Vector3(2.47, .26, -.59),
        Vector3(2.47, 2.94, -.59),
        Vector3(-2.47, 2.94, -.59),
        "rooms",
        panel
    )
    b.box(Vector3(0, .63, -.18), Vector3(5.14, .13, .75), "timber")
    if id != "TicketBay" or variant == 0:
        awning(b, 5.24, 2.04 if id == "FloristBay" else 1.35, 3.51)
    if id == "FloristBay" or variant % 4 == 0:
        b.box(Vector3(-.75, 4.45, .47), Vector3(3.1, .37, .54), "oxide")
        for i in range(6):
            foliage(
                b,
                Vector3(-2. + i * .50, 4.36 + sin(i * 3.4) * .12, .77),
                .82,
                .85,
                "flowers" if i % 4 == 1 else "leaves"
            )
    b.finish(["brick", "stripe"])
    city.block.core(center.x, center.z, 5.95, depth, 0.)
    cut_groups.append(
        [
            part,
            AABB(
                Vector3(x - 3., GROUND, z + minf(back * depth, 0.) - .42),
                Vector3(6., height + 1.1, depth + .85)
            )
        ]
    )


func awning(b, width, reach, high):
    var w = width * .5
    var previous = Vector2(.13, high)
    for i in range(1, 5):
        var t = i / 4.
        var next = Vector2(.13 + t * reach, high - .52 * t - .14 * sin(t * PI))
        b.sheet(
            Vector3(-w, previous.y, previous.x),
            Vector3(w, previous.y, previous.x),
            Vector3(w, next.y, next.x),
            Vector3(-w, next.y, next.x),
            "stripe",
            Rect2(0, (i - 1) * .18, 2.4, .18)
        )
        previous = next
    b.beam(Vector3(-w, previous.y, previous.x), Vector3(w, previous.y, previous.x), .035, "zincrim")
    for x in [-w + .06, w - .06]:
        b.beam(Vector3(x, high - .83, .10), Vector3(x, previous.y - .02, previous.x), .034, "iron")
    # Individual scallops are cut into a continuous fabric valance.
    for i in range(24):
        var x0 = -w + width * i / 24.
        var x1 = -w + width * (i + 1) / 24.
        var y0 = previous.y - .16 - float(i % 2) * .075
        var y1 = previous.y - .16 - float((i + 1) % 2) * .075
        b.sheet(
            Vector3(x0, y0, previous.x),
            Vector3(x1, y1, previous.x),
            Vector3(x1, previous.y, previous.x),
            Vector3(x0, previous.y, previous.x),
            "stripe",
            Rect2(i * .1, .72, .1, .17)
        )


func foliage(b, at, w, h, id):
    for heading in [-.38, 1.12]:
        var side = Vector3(w * .5, 0, 0).rotated(Vector3.UP, heading)
        var up = Vector3.UP * h
        b.sheet(at - side, at + side, at + side + up, at - side + up, id)


func loading_gate(b):
    var x = GATE.get_center().x
    var z = GATE.get_center().y
    for dz in [-1.91, 1.91]:
        b.box(Vector3(x, 1.51, z + dz), Vector3(.28, 2.77, .23), "enamel")
    b.box(Vector3(x, 2.9, z), Vector3(.52, .48, 4.10), "enamel")
    b.poster(Vector3(x - .275, 2.55, z), 1.62, .52, 2, -PI * .5)
    gate = group("Press shutter", Vector3(x, .16, z))
    var shutter = Builder.new(gate, mats)
    for i in range(12):
        shutter.box(Vector3(0, .11 + i * .214, 0), Vector3(.13, .19, 3.65), "enamel" if i % 4 else "zincrim")
    shutter.finish(["enamel"])


func market_hatch(b):
    b.poster(Vector3(13., .85, -10.96), 1., 1.25, 3, PI)
    market_lid = group("Returns blind", Vector3(12., 2.5, -11.1))
    var lid = Builder.new(market_lid, mats)
    lid.box(Vector3(0, -.55, 0), Vector3(1.88, 1.1, .10), "oxide")
    lid.poster(Vector3(0, -1.05, -.058), 1.52, .91, 3, PI)
    lid.finish(["oxide"])


func drain(b, at):
    b.box(at + Vector3.UP * .018, Vector3(1.17, .035, .32), "iron")
    for i in range(12):
        b.box(at + Vector3(-.52 + i * .095, .039, 0), Vector3(.040, .023, .29), "zincrim")


func stack_papers(b, at):
    for j in range(3):
        var pos = at + Vector3((j % 2) * .13 - .15, .106 + j * .155, 0)
        b.box(pos, Vector3(.76, .14, .49), "canvas", j * .04)
        for x in [-.22, .22]:
            b.box(pos + Vector3(x, .004, 0), Vector3(.019, .156, .51), "timber", j * .04)
        var v = pos + Vector3(0, .075, 0)
        b.sheet(
            v + Vector3(-.36, 0, .23),
            v + Vector3(.36, 0, .23),
            v + Vector3(.36, 0, -.23),
            v + Vector3(-.36, 0, -.23),
            "print",
            Rect2(.006, .005, .485, .486)
        )


func festoon(a, b):
    var g = Builder.new(group("Work lights"), mats)
    var old = a
    for i in range(1, 9):
        var t = i / 8.
        var p = a.lerp(b, t) - Vector3.UP * .46 * sin(t * PI)
        g.beam(old, p, .020, "iron", 5)
        if i % 2 == 1:
            g.beam(p, p - Vector3.UP * .16, .026, "iron", 6)
            g.beam(p - Vector3.UP * .15, p - Vector3.UP * .27, .076, "opal", 9)
        old = p
    g.finish()


func set_delivery(press: bool, market: bool, instant = false):
    press_open = press
    market_open = market
    if instant:
        gate_lift = float(press)
        market_lift = float(market)
    animate(0.)


func animate(delta):
    press_wheel.rotate_x(delta * (1.75 if press_open else .42))
    gate_lift = move_toward(gate_lift, float(press_open), delta * .85)
    market_lift = move_toward(market_lift, float(market_open), delta * .95)
    gate.scale.y = maxf(.02, 1. - gate_lift * .98)
    gate.position.y = GROUND + 2.56 * gate_lift
    market_lid.rotation.x = market_lift * 1.45


func cutaway(at: Vector3, eye: Vector3, tactical: bool):
    var inside = tactical && Rect2(-20, -28, 41, 23).has_point(Vector2(at.x, at.z))
    var sideways = Vector3.UP.cross(eye - at).normalized() * 2.7
    for item in cut_groups:
        var hit = false
        if inside:
            for span in [-sideways, Vector3.ZERO, sideways]:
                if item[1].intersects_segment(at + Vector3.UP * 1.05 + span, eye + span) != null:
                    hit = true
                    break
        item[0].visible = not hit
