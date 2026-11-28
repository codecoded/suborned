extends Node3D

const Routes = preload("res://crowd_routes.gd")
const LIBRARY = preload("res://assets/street_extras.glb")
const OUTFITS = ["d2m", "d4", "cafe_w2", "d5c", "d6", "el1", "cafe_w5", "d4b"]
const STEP = 1. / 15.
const WALK = 0
const IDLE = 1
const TALK = 2
var city
var routes
var residents = []
var batches = []
var material = ShaderMaterial.new()
var remainder = 0.
var clock = 0.
var visible_count = 0


class Extra:
    var outfit: int
    var stops: Array
    var stop = 0
    var path = PackedVector2Array()
    var vertex = 1
    var centre = Vector2.ZERO
    var at = Vector2.ZERO
    var before = Vector2.ZERO
    var offset = Vector2.ZERO
    var yaw = 0.
    var old_yaw = 0.
    var facing = 0.
    var home_heading = 0.
    var watching_tram = false
    var wait = 0.
    var speed = 1.3
    var size = 1.
    var phase = 0.
    var stride = 0.
    var old_stride = 0.
    var mode = IDLE
    var previous = IDLE
    var changed = -1.


func setup(street):
    city = street
    routes = Routes.new(street)
    material.shader = preload("res://street_extra.gdshader")
    material.set_shader_parameter("wardrobe", preload("res://assets/street_wardrobe.jpg"))
    material.set_shader_parameter("poses", preload("res://assets/street_poses.png"))
    material.set_shader_parameter("arcade_field", preload("res://assets/arcade-field.res"))
    var circuits = Routes.COURT + Routes.AVENUE if city.court else Routes.AVENUE
    for circuit in circuits:
        distribute(circuit[0], circuit[1])
    var posts = Routes.COURT_POSTS + Routes.AVENUE_POSTS if city.court else Routes.AVENUE_POSTS
    for post in posts:
        var extra = person(post[3])
        extra.centre = Vector2(post[0], post[1])
        extra.facing = deg_to_rad(post[2])
        extra.home_heading = extra.facing
        extra.watching_tram = extra.centre.y > 5.5 and extra.centre.y < 22.1
        place(extra)
    var library = LIBRARY.instantiate()
    for i in range(OUTFITS.size()):
        var source = library.find_child(OUTFITS[i], true, false) as MeshInstance3D
        var mm = MultiMesh.new()
        mm.transform_format = MultiMesh.TRANSFORM_3D
        mm.use_custom_data = true
        mm.mesh = source.mesh
        var count = 0
        for extra in residents:
            if extra.outfit == i:
                count += 1
        mm.instance_count = count
        mm.visible_instance_count = 0
        mm.custom_aabb = AABB(Vector3(-58, -.3, -30), Vector3(129, 3.4, 56))
        var node = MultiMeshInstance3D.new()
        node.name = OUTFITS[i]
        node.multimesh = mm
        node.material_override = material
        add_child(node)
        batches.append(mm)
    library.free()
    render()


func person(outfit: int) -> Extra:
    var e = Extra.new()
    var i = residents.size()
    e.outfit = outfit
    e.phase = fposmod(i * .618034, 1.)
    e.stride = e.phase
    e.old_stride = e.stride
    e.speed = (.98 if outfit == 5 else 1.24) + fmod(i * .17, .27)
    e.size = .955 + fmod(i * .029, .112)
    residents.append(e)
    return e


func place(e: Extra):
    e.at = e.centre
    e.before = e.at
    e.yaw = e.facing
    e.old_yaw = e.yaw


func point(stops: Array, i: int) -> Vector2:
    return Vector2(stops[i].x, stops[i].y)


func distribute(count: int, stops: Array):
    var segments = []
    var total = 0.
    for j in range(stops.size()):
        var k = (j + 1) % stops.size()
        var path = routes.leg(point(stops, j), point(stops, k))
        for v in range(1, path.size()):
            var length = path[v].distance_to(path[v - 1])
            if length > .001:
                segments.append([total, length, k, path, v])
                total += length
    if segments.is_empty():
        return
    for j in range(count):
        var d = total * (j + .31) / count
        var e = person(posmod(residents.size() * 5 + int(stops[0].x), OUTFITS.size()))
        e.stops = stops
        e.mode = WALK
        e.previous = WALK
        start_on_circuit(e, segments, d)
        var fixer = Vector2(city.guide.position.x, city.guide.position.z)
        if e.centre.distance_squared_to(fixer) < 2.25:
            start_on_circuit(e, segments, fposmod(d + 3., total))
        place(e)


func start_on_circuit(e: Extra, segments: Array, distance: float):
    for part in segments:
        if distance >= part[0] + part[1]:
            continue
        e.stop = part[2]
        e.path = part[3]
        e.vertex = part[4]
        var from = e.path[e.vertex - 1]
        var to = e.path[e.vertex]
        e.centre = from.lerp(to, (distance - part[0]) / part[1])
        e.facing = atan2(to.x - from.x, to.y - from.y)
        return


func motion(e: Extra, next: int):
    if e.mode != next:
        e.previous = e.mode
        e.mode = next
        e.changed = clock


func follow(e: Extra):
    if e.stops.is_empty():
        e.facing = e.home_heading
        var tram = city.streetscape.tram.position
        if e.watching_tram and absf(tram.x - e.centre.x) < 11.:
            e.facing = atan2(tram.x - e.centre.x, tram.z - e.centre.y)
            motion(e, IDLE)
            return
        var speaking = fposmod(clock + e.phase * 19., 11.8) < 4.1
        motion(e, TALK if speaking else IDLE)
        return
    if e.wait > 0.:
        e.wait -= STEP
        motion(e, IDLE)
        return
    if e.vertex >= e.path.size():
        var next_stop = (e.stop + 1) % e.stops.size()
        var next_path = routes.leg(point(e.stops, e.stop), point(e.stops, next_stop))
        if next_path.size() < 2:
            e.wait = 1.
            motion(e, IDLE)
            return
        e.stop = next_stop
        e.path = next_path
        e.vertex = 1
    var remaining = STEP * e.speed
    motion(e, WALK)
    while remaining > 0. and e.vertex < e.path.size():
        var diff = e.path[e.vertex] - e.centre
        var length = diff.length()
        var distance = minf(remaining, length)
        if length > .001:
            e.centre += diff * (distance / length)
            e.stride = fposmod(e.stride + distance / (1.48 * e.size), 1.)
            e.facing = atan2(diff.x, diff.y)
        remaining -= distance
        if length <= distance + .0001:
            e.vertex += 1
    if e.vertex == e.path.size():
        e.wait = e.stops[e.stop].z * (.84 + .32 * e.phase)
        if e.wait > .2:
            e.facing = deg_to_rad(e.stops[e.stop].w)


func occupy(grid: Dictionary, at: Vector2, radius: float):
    var cell = Vector2i((at / 1.6).floor())
    if not grid.has(cell):
        grid[cell] = []
    grid[cell].append(Vector3(at.x, at.y, radius))


func simulation_step():
    clock += STEP
    var occupied = {}
    occupy(occupied, Vector2(city.guide.position.x, city.guide.position.z), .65)
    for person_record in city.people:
        var p = person_record[0].position
        occupy(occupied, Vector2(p.x, p.z), .62)
    if city.court:
        var cart = city.court.cart.position
        occupy(occupied, Vector2(cart.x, cart.z), .93)
    for e in residents:
        e.before = e.at
        e.old_yaw = e.yaw
        e.old_stride = e.stride
        follow(e)
        var offset = e.offset.move_toward(Vector2.ZERO, STEP * .58)
        var cell = Vector2i((e.centre / 1.6).floor())
        for x in range(cell.x - 1, cell.x + 2):
            for z in range(cell.y - 1, cell.y + 2):
                var key = Vector2i(x, z)
                if not occupied.has(key):
                    continue
                for other in occupied[key]:
                    var away = e.centre + offset - Vector2(other.x, other.y)
                    if away.length_squared() >= other.z * other.z:
                        continue
                    var side = Vector2(cos(e.facing), -sin(e.facing))
                    var direction = (away.normalized() + .34 * side).normalized()
                    var trial = (offset + direction * (other.z - away.length() + .055)).limit_length(1.1)
                    if city.block.clear_at(e.centre + trial, .27):
                        offset = trial
        e.offset = offset
        var next = e.at.move_toward(e.centre + offset, STEP * (e.speed + .65))
        if city.block.clear_at(next, .26):
            e.at = next
        e.yaw = rotate_toward(e.yaw, e.facing, STEP * 3.65)
        occupy(occupied, e.at, .63)


func advance(delta: float):
    remainder += minf(delta, .15)
    while remainder >= STEP:
        remainder -= STEP
        simulation_step()
    render()


func frame(e: Extra, clip: int, t: float) -> float:
    if clip == WALK:
        var foot = e.old_stride + fposmod(e.stride - e.old_stride, 1.) * remainder / STEP
        return 16. * fposmod(foot, 1.)
    if clip == IDLE:
        return 17. + 12. * fposmod(t / 8.8 + e.phase, 1.)
    return 30. + 16. * fposmod(t / 5.2 + e.phase, 1.)


func render():
    if batches.is_empty():
        return
    var cam: Camera3D = city.camera
    var local = cam.global_transform.affine_inverse()
    var viewport = cam.get_viewport().get_visible_rect().size
    var aspect = viewport.x / maxf(1., viewport.y)
    var slope = tan(deg_to_rad(cam.fov) * .5)
    var counts = PackedInt32Array()
    counts.resize(batches.size())
    visible_count = 0
    var alpha = remainder / STEP
    for e in residents:
        var at = e.before.lerp(e.at, alpha)
        var world = Vector3(at.x, .161, at.y)
        var view = local * (world + Vector3.UP)
        var height = cam.size * .5 if city.iso else maxf(-view.z, 0.) * slope
        # Six metres also admits a figure whose low sun shadow enters the frame.
        if view.z > 6. or absf(view.x) > height * aspect + 6. or absf(view.y) > height + 6.:
            continue
        var mm: MultiMesh = batches[e.outfit]
        var slot = counts[e.outfit]
        var facing = lerp_angle(e.old_yaw, e.yaw, alpha)
        mm.set_instance_transform(
            slot, Transform3D(Basis(Vector3.UP, facing).scaled(Vector3.ONE * e.size), world)
        )
        var t = clock + remainder
        var blend = smoothstep(0., .30, t - e.changed)
        mm.set_instance_custom_data(
            slot, Color(frame(e, e.previous, t) / 64., frame(e, e.mode, t) / 64., blend, 1.)
        )
        counts[e.outfit] += 1
        visible_count += 1
    for i in range(batches.size()):
        if batches[i].visible_instance_count != counts[i]:
            batches[i].visible_instance_count = counts[i]
