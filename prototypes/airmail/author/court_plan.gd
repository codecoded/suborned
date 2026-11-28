extends SceneTree

# A real renderer also checks the ink against the same skinned figure with the
# second pass switched off. The headless run covers geometry, controls and LODs.
var city
var failures = []


func _initialize():
    call_deferred("exercise")


func check(condition: bool, message: String):
    if not condition and message not in failures:
        failures.append(message)


func key(code):
    var event = InputEventKey.new()
    event.keycode = code
    event.pressed = true
    root.push_input(event, true)


func paving_at(at: Vector2):
    var top := Vector3(at.x, .3, at.y)
    var end := Vector3(at.x, .06, at.y)
    var height := -INF
    var surface
    for root_node in [city.streetscape, city.court]:
        for instance in root_node.find_children("*", "MeshInstance3D", true, false):
            if not instance.is_visible_in_tree() or not instance.mesh:
                continue
            var bounds: AABB = instance.global_transform * instance.get_aabb()
            if bounds.grow(.001).intersects_segment(top, end) == null:
                continue
            var faces = instance.mesh.get_faces()
            for i in range(0, faces.size(), 3):
                var hit = Geometry3D.segment_intersects_triangle(
                    top,
                    end,
                    instance.to_global(faces[i]),
                    instance.to_global(faces[i + 1]),
                    instance.to_global(faces[i + 2])
                )
                if hit != null and hit.y > height:
                    height = hit.y
                    surface = instance
    return surface


func check_levels(detail):
    for i in range(0, detail.levels.size(), 3):
        var active = 0
        for entry in detail.levels.slice(i, i + 3):
            var item = entry[0]
            if item.is_visible_in_tree():
                active += 1
                check(
                    item.visibility_range_begin == 0 and item.visibility_range_end == 0,
                    "Plan mesh still has a distance cutoff."
                )
        check(active == 1, "An orthographic module has missing or duplicate geometry.")


func rendered_frame():
    for i in range(3):
        await RenderingServer.frame_post_draw
    return root.get_texture().get_image()


func ink_pixels(spot: Vector2, heading: float, label: String) -> int:
    city.guide.position = Vector3(spot.x, .165, spot.y)
    city.yaw = heading
    city.update_camera()
    city.mark_figure(city.guide, true)
    var with_ink: Image = await rendered_frame()
    city.mark_figure(city.guide, false)
    var without: Image = await rendered_frame()
    var foot = city.camera.unproject_position(city.guide.position)
    var head = city.camera.unproject_position(city.guide.position + Vector3.UP * 1.95)
    var bounds = Rect2(foot, Vector2.ZERO).expand(head).grow(34.)
    var area = Rect2i(bounds).intersection(Rect2i(Vector2i.ZERO, with_ink.get_size()))
    var pixels = 0
    for y in range(area.position.y, area.end.y):
        for x in range(area.position.x, area.end.x):
            var a = with_ink.get_pixel(x, y)
            var b = without.get_pixel(x, y)
            if maxf(absf(a.r - b.r), maxf(absf(a.g - b.g), absf(a.b - b.b))) > .08:
                pixels += 1
    if city.run_options.has("proof"):
        with_ink.save_png(city.run_options.proof.path_join(label + ".png"))
        without.save_png(city.run_options.proof.path_join(label + "-without.png"))
    return pixels


func exercise():
    if "memory=1" not in OS.get_cmdline_user_args():
        push_error("Use -- memory=1 to keep the player's saved errands separate.")
        quit(1)
        return
    city = load("res://street.tscn").instantiate()
    root.add_child(city)
    await process_frame
    for point in [Vector2(-8.35, -14), Vector2(-1.4, -13.3), Vector2(9.75, -19.70)]:
        var floor_mesh = paving_at(point)
        check(
            floor_mesh != null and floor_mesh.mesh.surface_get_material(0) == city.streetscape.materials.slab,
            "The original Meridian street paving is covered at " + str(point)
        )
    var street_yaw = city.yaw
    key(KEY_TAB)
    check(city.iso and city.camera.projection == Camera3D.PROJECTION_ORTHOGONAL, "Tab did not enter plan.")
    var architecture = []
    for child in city.court.get_children():
        if child is Node3D and not child is Light3D:
            architecture.append(child)
    check(architecture.size() >= 12, "The working court is incomplete.")
    for position in [
        city.court.START,
        Vector2(-7, -20),
        Vector2(3, -14),
        Vector2(8.7, -17),
        Vector2(9, -21),
        Vector2(17, -4),
        Vector2(13, 4)
    ]:
        city.guide.position = Vector3(position.x, .165, position.y)
        for zoom in [10., 23., 34.]:
            city.zoom = zoom
            for i in range(8):
                city.yaw = TAU * i / 8.
                city.update_camera()
                for part in architecture:
                    check(part.is_visible_in_tree(), "A turn hid " + str(part.name))
            check_levels(city.court.detail)
            check_levels(city.block.detail)
    var orbit = city.yaw
    key(KEY_TAB)
    check(not city.iso and is_equal_approx(city.yaw, street_yaw), "Lost the street camera heading.")
    for detail in [city.court.detail, city.block.detail]:
        for entry in detail.levels:
            var item = entry[0]
            check(
                (
                    item.visible
                    and item.visibility_range_begin == entry[1]
                    and item.visibility_range_end == entry[2]
                ),
                "Returning to the street lost a distance LOD."
            )
    key(KEY_TAB)
    check(is_equal_approx(orbit, city.yaw), "Lost the player's plan orbit.")
    if DisplayServer.get_name() != "headless":
        city.set_process(false)
        for animator in city.find_children("*", "AnimationPlayer", true, false):
            animator.pause()
        city.hud.hide()
        city.dialogue.tick(0)
        city.zoom = 13.
        var exposed = await ink_pixels(Vector2(-8, -14), city.PLAN_HEADING, "Open lane")
        var covered = await ink_pixels(Vector2(8.7, -17), 0., "Under the conveyor")
        check(exposed <= 4, "The ink altered an exposed Fixer.")
        check(covered >= 60, "The Fixer disappeared beneath the intact conveyor.")
        print("Changed figure pixels on open paving and beneath the conveyor: ", exposed, " / ", covered)
        if city.court_audio:
            city.court_audio.silence()
            await create_timer(.2, true, false, true).timeout
    for msg in failures:
        push_error(msg)
    if failures.is_empty():
        print(
            "Plan passed original paving, 168 orbit and zoom poses, complete buildings, one LOD per module and camera return."
        )
    quit(0 if failures.is_empty() else 1)
