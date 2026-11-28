extends Node3D

const Block = preload("res://block.gd")
const Streetscape = preload("res://set.gd")
const Bench = preload("res://timing.gd")
const ArcadeLight = preload("res://arcade_light.gd")
const ParcelCourt = preload("res://court_scenery.gd")
const Deliveries = preload("res://court_orders.gd")
const Conversation = preload("res://court_dialogue.gd")
const CourtAudio = preload("res://court_audio.gd")
const FIXER = preload("res://assets/fixer.glb")
const PLAN_HEADING = -1.12
const PEOPLE = [preload("res://assets/c1.glb"), preload("res://assets/c6.glb")]
const COURT_PEOPLE = [
    preload("res://assets/cafe_w3.glb"), preload("res://assets/sh2.glb"), preload("res://assets/workers_d5.glb")
]
var block
var streetscape
var arcade_light
var camera = Camera3D.new()
var guide
var guide_anim
var people = []
var grid = AStarGrid2D.new()
var waypoints = PackedVector2Array()
var yaw = .47
var street_heading = -.87
var court_heading = PLAN_HEADING
var iso = false
var zoom = 23.
var elapsed = 0.
var run_options = {}
var timings
var caption = Label.new()
var phase = "idle"
var hud = CanvasLayer.new()
var court
var orders
var dialogue
var court_audio
var plan_ink = ShaderMaterial.new()
var plan_active = false
var plan_contact = -1
var leaving = false


func _exit_tree():
    if dialogue:
        dialogue.callback.clear()
        dialogue.orders = null
        orders.talk = null


func _ready():
    for value in OS.get_cmdline_user_args():
        var key = value.get_slice("=", 0)
        run_options[key] = value.get_slice("=", 1) if "=" in value else "1"
    change_block(run_options.get("mode", "optimized"), run_options.get("corner", "turned"))
    streetscape = Streetscape.new()
    add_child(streetscape)
    streetscape.setup()
    streetscape.combine_fixtures(run_options.get("mode", "optimized") in ["lod", "optimized"])
    if not run_options.has("avenue") and (run_options.has("yard") or not run_options.has("out")):
        court = ParcelCourt.new()
        add_child(court)
        court.setup(self)
    guide = (FIXER if court else PEOPLE[0]).instantiate()
    add_child(guide)
    guide.position = Vector3(13, .16, 4.2)
    if court:
        guide.position = Vector3(ParcelCourt.START.x, .165, ParcelCourt.START.y)
        yaw = -.87
        guide.rotation.y = yaw + PI
        zoom = 13.
    guide_anim = animation(guide, "idle")
    for i in range(14):
        var model = COURT_PEOPLE[Deliveries.CAST.find(i)] if court and i in Deliveries.CAST else PEOPLE[i % 2]
        var actor = model.instantiate()
        add_child(actor)
        var moving = i % 3 != 0
        var player = animation(actor, "walk" if moving else "idle")
        if player:
            player.seek(fposmod(i * .31, maxf(.1, player.current_animation_length)), true)
        var x = -38. + i * 6.5
        var z = [3.74, 4.09, 22.9, 1.95][i % 4]
        if i == 0:
            x = 6.
            z = -2.05
        if i == 9:
            x = -7.
            z = -1.25
        people.append([actor, Vector3(x, .16, z), moving, i * .49])
    add_child(camera)
    camera.current = true
    camera.far = 250.
    camera.near = .09
    setup_navigation()
    add_child(hud)
    hud.add_child(caption)
    caption.position = Vector2(23, 18)
    caption.add_theme_font_size_override("font_size", 15)
    caption.add_theme_color_override("font_shadow_color", Color(0, 0, 0, .85))
    caption.add_theme_constant_override("shadow_offset_x", 1)
    caption.add_theme_constant_override("shadow_offset_y", 1)
    if court:
        orders = Deliveries.new()
        dialogue = Conversation.new()
        orders.setup(self, court, dialogue)
        dialogue.setup(self, orders)
        if not run_options.has("out") and DisplayServer.get_name() != "headless":
            court_audio = CourtAudio.new()
            add_child(court_audio)
            court_audio.setup(self)
            get_tree().auto_accept_quit = false
            get_window().close_requested.connect(leave_block)
    arcade_light = ArcadeLight.new()
    add_child(arcade_light)
    arcade_light.prepare(self)
    if court:
        plan_ink.shader = preload("res://plan_figure.gdshader")
        arcade_light.sunlight.directional_shadow_mode = DirectionalLight3D.SHADOW_PARALLEL_2_SPLITS
        arcade_light.sunlight.directional_shadow_max_distance = 55.
    var initial_light = "original" if run_options.has("suite") else "surfaced"
    arcade_light.set_look(run_options.get("lighting", initial_light))
    update_hint()
    if run_options.has("out"):
        hud.visible = false
        timings = Bench.new()
        add_child(timings)
        timings.start(self, run_options)
    update_camera()


func change_block(mode, corner):
    if block:
        remove_child(block)
        block.free()
    get_viewport().use_occlusion_culling = mode == "optimized"
    block = Block.new()
    add_child(block)
    block.build(mode != "full", corner == "turned", mode != "instances")
    if court:
        block.detail.set_plan(iso)
    if streetscape:
        streetscape.combine_fixtures(mode in ["lod", "optimized"])
    if arcade_light:
        arcade_light.apply_block()


func animation(actor, clip):
    var player = actor.find_child("AnimationPlayer", true, false) as AnimationPlayer
    if player:
        for name in player.get_animation_list():
            if String(name).to_lower().ends_with(clip):
                player.get_animation(name).loop_mode = Animation.LOOP_LINEAR
                player.play(name)
                return player
    return player


func setup_navigation():
    grid.region = Rect2i(-56, -29, 125, 54)
    grid.cell_size = Vector2.ONE
    grid.diagonal_mode = AStarGrid2D.DIAGONAL_MODE_ONLY_IF_NO_OBSTACLES
    grid.update()
    for x in range(-56, 69):
        for y in range(-29, 25):
            if not block.clear_at(Vector2(x, y), .48):
                grid.set_point_solid(Vector2i(x, y))


func update_gate(open: bool):
    block.closed_passages.clear()
    if not open:
        block.closed_passages.append(ParcelCourt.GATE)
    var rect = Rect2i(ParcelCourt.GATE.grow(2.).abs())
    for x in range(rect.position.x, rect.end.x + 2):
        for z in range(rect.position.y, rect.end.y + 2):
            grid.set_point_solid(Vector2i(x, z), not block.clear_at(Vector2(x, z), .48))


func straight_clear(a: Vector2, b: Vector2, margin = .12) -> bool:
    var steps = maxi(1, ceili(a.distance_to(b) / .32))
    for i in range(steps + 1):
        if not block.clear_at(a.lerp(b, float(i) / steps), margin):
            return false
    return true


func grid_entry(at: Vector2) -> Vector2i:
    var cell = Vector2i(at.round())
    if grid.is_in_boundsv(cell) and not grid.is_point_solid(cell) and straight_clear(at, Vector2(cell), .26):
        return cell
    var best = Vector2i(-1000, -1000)
    var distance = INF
    for x in range(cell.x - 1, cell.x + 2):
        for z in range(cell.y - 1, cell.y + 2):
            var test = Vector2i(x, z)
            if not grid.is_in_boundsv(test) or grid.is_point_solid(test):
                continue
            var d = at.distance_squared_to(Vector2(test))
            if d < distance and straight_clear(at, Vector2(test), .26):
                best = test
                distance = d
    return best


func route_between(from: Vector2, to: Vector2) -> PackedVector2Array:
    var route = PackedVector2Array()
    if not block.clear_at(to):
        return route
    var a = grid_entry(from)
    var b = grid_entry(to)
    if not grid.is_in_boundsv(a) or not grid.is_in_boundsv(b):
        return route
    route = grid.get_point_path(a, b)
    if route.is_empty():
        return route
    if route.size() > 1 and straight_clear(from, route[1], .27):
        route.remove_at(0)
    elif route[0].distance_to(from) < .01:
        route.remove_at(0)
    if route.is_empty() or route[-1].distance_to(to) > .01:
        route.append(to)
    return route


func route_length(start: Vector2, route: PackedVector2Array) -> float:
    var distance = 0.
    for point in route:
        distance += start.distance_to(point)
        start = point
    return distance


func walk_to(point: Vector2) -> bool:
    var route = route_between(Vector2(guide.position.x, guide.position.z), point)
    if route.is_empty():
        return false
    waypoints = route
    return true


func update_hint():
    if court:
        caption.text = "Parcel Court    /    Meridian Air Post\nClick to walk or talk   ·   Tab for view   ·   Right-drag to turn   ·   H to hide"
        return
    caption.text = (
        "Meridian Air Post   ·   "
        + ("Tactical" if iso else "Street")
        + "   ·   "
        + ArcadeLight.NAMES[ArcadeLight.LOOKS.find(arcade_light.look)]
        + "\nClick to walk   ·   Tab for view   ·   L to compare finishes   ·   Right-drag to turn   ·   H to hide"
    )


func _unhandled_input(event):
    if timings:
        return
    if event is InputEventKey and event.pressed and not event.echo:
        if dialogue and event.keycode == KEY_E:
            if dialogue.speaker >= 0:
                dialogue.close()
            elif dialogue.nearest() >= 0:
                dialogue.open(dialogue.nearest())
            return
        if dialogue and dialogue.speaker >= 0:
            if event.keycode == KEY_ESCAPE:
                dialogue.close()
                return
            if event.keycode in [KEY_1, KEY_2, KEY_3]:
                dialogue.choose(event.keycode - KEY_1)
                return
        if event.keycode == KEY_TAB:
            if court:
                if iso:
                    court_heading = yaw
                    yaw = street_heading
                else:
                    street_heading = yaw
                    yaw = court_heading
            iso = not iso
            update_hint()
            update_camera()
        if event.keycode == KEY_H:
            hud.visible = not hud.visible
        if event.keycode == KEY_L:
            var next = (ArcadeLight.LOOKS.find(arcade_light.look) + 1) % ArcadeLight.LOOKS.size()
            arcade_light.set_look(ArcadeLight.LOOKS[next])
            update_hint()
        if event.keycode == KEY_ESCAPE:
            leave_block()
    if event is InputEventMouseMotion and Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
        yaw -= event.relative.x * .007
    if event is InputEventMouseButton and event.pressed:
        if event.button_index == MOUSE_BUTTON_WHEEL_UP:
            zoom = clampf(zoom - 2, 10 if court else 14, 34)
        if event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
            zoom = clampf(zoom + 2, 10 if court else 14, 34)
        if event.button_index == MOUSE_BUTTON_LEFT:
            if dialogue:
                var person_index = dialogue.pick(event.position)
                if person_index >= 0:
                    dialogue.visit(person_index)
                    return
                dialogue.close()
            var plane = Plane(Vector3.UP, .16)
            var hit = plane.intersects_ray(camera.project_ray_origin(event.position), camera.project_ray_normal(event.position))
            if hit != null:
                walk_to(Vector2(hit.x, hit.z))


func leave_block():
    if leaving:
        return
    leaving = true
    if court_audio:
        set_process(false)
        set_process_unhandled_input(false)
        court_audio.silence()
        await get_tree().create_timer(.16, true, false, true).timeout
    get_tree().quit()


func _process(delta):
    if not guide:
        return
    if timings:
        timings.tick()
    else:
        elapsed += delta
        var axis = Vector2(
            float(Input.is_physical_key_pressed(KEY_D)) - float(Input.is_physical_key_pressed(KEY_A)),
            float(Input.is_physical_key_pressed(KEY_S)) - float(Input.is_physical_key_pressed(KEY_W))
        )
        var from = Vector2(guide.position.x, guide.position.z)
        var toward = Vector2.ZERO
        var distance = minf(delta, .1) * 3.
        if axis.length_squared() > 0:
            if dialogue and (dialogue.speaker >= 0 or dialogue.approach >= 0):
                dialogue.close()
            waypoints.clear()
            toward = axis.normalized().rotated(-yaw)
        elif not waypoints.is_empty():
            var difference = waypoints[0] - from
            toward = difference.normalized()
            if difference.length() <= distance:
                distance = difference.length()
                waypoints.remove_at(0)
        var step = from + toward * distance
        if toward.length_squared() > 0 and block.clear_at(step):
            guide.position.x = step.x
            guide.position.z = step.y
            guide.rotation.y = atan2(toward.x, toward.y)
        var next = "walk" if toward.length_squared() > 0 else "idle"
        if next != phase:
            phase = next
            animation(guide, next)
    for i in range(people.size()):
        if orders and orders.owns(i):
            continue
        var rec = people[i]
        var actor = rec[0]
        var p: Vector3 = rec[1]
        var a = elapsed * .17 + rec[3]
        if rec[2]:
            p.x += sin(a) * 5.7
            actor.rotation.y = PI * .5 if cos(a) > 0 else -PI * .5
        else:
            actor.rotation.y = PI + rec[3] * .64
        actor.position = p
    if orders:
        orders.tick(minf(delta, .15))
        dialogue.tick(delta)
        if court_audio:
            court_audio.tick()
    streetscape.advance(elapsed)
    update_camera()


func update_camera():
    var offset = Vector3(sin(yaw), 0, cos(yaw))
    var p = guide.position
    if iso:
        camera.projection = Camera3D.PROJECTION_ORTHOGONAL
        camera.size = zoom
        camera.position = p + (offset * 13. + Vector3.UP * 25. if court else offset * 19. + Vector3.UP * 17.5)
        camera.look_at(p + Vector3.UP * .9)
    else:
        camera.projection = Camera3D.PROJECTION_PERSPECTIVE
        camera.fov = 60.
        var in_yard = court and Rect2(-20, -28, 41, 23).has_point(Vector2(p.x, p.z))
        var reach = 3.85 if in_yard else 4.65
        for i in range(1, 11):
            var test = p + offset * (reach * i / 10.)
            if not block.clear_at(Vector2(test.x, test.z), .12):
                reach = maxf(.8, reach * (i - 1) / 10.)
                break
        camera.position = p + offset * reach + Vector3.UP * 2.18
        camera.look_at(p - offset * 3.8 + Vector3.UP * (1.73 if in_yard else 2.43))
    if court:
        update_plan()


func update_plan():
    if plan_active != iso:
        plan_active = iso
        block.detail.set_plan(iso)
        court.detail.set_plan(iso)
        mark_figure(guide, iso)
    var contact = -1
    if iso:
        contact = dialogue.speaker if dialogue.speaker >= 0 else dialogue.approach
    if contact != plan_contact:
        if plan_contact >= 0:
            mark_figure(orders.person(plan_contact), false)
        if contact >= 0:
            mark_figure(orders.person(contact), true)
        plan_contact = contact


func mark_figure(actor: Node3D, enabled: bool):
    if DisplayServer.get_name() == "headless":
        return
    for mesh in actor.find_children("*", "MeshInstance3D", true, false):
        mesh.material_overlay = plan_ink if enabled else null
        mesh.ignore_occlusion_culling = enabled


func tour(t: float, view: String):
    elapsed = t * 14.0
    var q = clampf(t, 0, 1)
    if court:
        var turn = [ParcelCourt.START, Vector2(-7, -15), Vector2(-2, -14), Vector2(6, -14), Vector2(8, -18), Vector2(9, -21)]
        var s = q * (turn.size() - 1)
        var part = mini(floori(s), turn.size() - 2)
        var at = turn[part].lerp(turn[part + 1], s - part)
        guide.position = Vector3(at.x, .165, at.y)
        yaw = PLAN_HEADING + .37 * q if view == "iso" else lerpf(-.87, -.25, q * q)
        var heading = turn[part + 1] - turn[part]
        guide.rotation.y = atan2(heading.x, heading.y)
        iso = view == "iso"
    elif view == "threshold":
        guide.position = Vector3(7.7 - q * 1.3, .16, 6.2 - 9.0 * q)
        yaw = .12 + .33 * q
        iso = false
    elif view == "corner":
        guide.position = Vector3(29. + 3. * q, .16, 5.4 - 10. * q)
        yaw = -.16 + .24 * q
        iso = false
    else:
        guide.position = Vector3(20. - q * 22., .16, 3.84 + .30 * sin(q * PI))
        yaw = .65 - .68 * q
        iso = view == "iso"
    if not court:
        guide.rotation.y = atan2(-1.3, -9.0) if view == "threshold" else -PI * .5
    if phase != "walk":
        phase = "walk"
        animation(guide, "walk")


func portrait(view):
    if court:
        tour(.05 if view == "court" else .68, "iso" if view == "court-iso" else "street")
        update_camera()
        return
    streetscape.presentation(view in ["hero", "vault", "aerial", "detail"])
    if view in ["street", "tactical"]:
        tour(.32, "iso" if view == "tactical" else "street")
        pose_portrait_life(elapsed)
        update_camera()
        return
    pose_portrait_life(14.)
    var positions = {
        "sorting": [Vector3(4.63, 1.92, -.98), Vector3(6.06, 1.92, -3.38)],
        "collection": [Vector3(9.51, 1.33, 4.02), Vector3(9.00, 1.14, 3.13)],
        "sill": [Vector3(4.03, 1.27, -1.28), Vector3(6.18, .77, -2.66)],
        "stone": [Vector3(8.14, 1.30, -.40), Vector3(8.79, 1.13, -1.97)],
        "threshold": [Vector3(7.55, 1.90, -1.75), Vector3(-1.5, 2.03, 3.8)],
        "hero": [Vector3(26, 3.45, 19.4), Vector3(-5, 8.8, -2.)],
        "vault": [Vector3(8.3, 2.25, 2.6), Vector3(-2, 4.2, -2.4)],
        "aerial": [Vector3(27, 23, 28), Vector3(-1, 3., -1)],
        "detail": [Vector3(10.7, 1.82, 4.13), Vector3(8.9, 1.85, 2.6)],
        "lane": [Vector3(29.1, 2.25, 2.6), Vector3(30., 2.85, -24)]
    }
    var at = positions[view]
    camera.projection = Camera3D.PROJECTION_PERSPECTIVE
    camera.fov = 56.
    camera.position = at[0]
    camera.look_at(at[1])


func pose_portrait_life(time: float):
    # The two lighting plates must show the same pose, even on displays with different cadence.
    streetscape.advance(time)
    for i in range(people.size()):
        var actor = people[i][0]
        var animator = actor.find_child("AnimationPlayer", true, false) as AnimationPlayer
        if animator and animator.current_animation_length > 0.:
            animator.seek(fposmod(time + i * .31, animator.current_animation_length), true)
    if guide_anim and guide_anim.current_animation_length > 0.:
        guide_anim.seek(fposmod(time, guide_anim.current_animation_length), true)
