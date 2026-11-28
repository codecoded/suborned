extends SceneTree


# Exercise the same input handlers, camera, grid and avatar used in the Mac player.
func _initialize():
    call_deferred("check_street")


func check_street():
    var street = load("res://street.tscn").instantiate()
    root.add_child(street)
    await process_frame
    await process_frame

    var target = Vector3(16, 0.16, 4)
    var click = InputEventMouseButton.new()
    click.button_index = MOUSE_BUTTON_LEFT
    click.pressed = true
    click.position = street.camera.unproject_position(target)
    root.push_input(click, true)
    if street.waypoints.is_empty():
        fail("A pavement click did not create a walking route.")
        return
    for frame in range(180):
        street._process(1.0 / 60.0)
    if street.guide.position.distance_to(target) > 0.15:
        fail("The avatar missed the clicked destination.")
        return
    if not street.grid.is_point_solid(Vector2i(0, -5)):
        fail("The pathfinding grid allows passage through a sorting hall wall.")
        return

    var key = InputEventKey.new()
    key.pressed = true
    key.keycode = KEY_TAB
    root.push_input(key, true)
    street.update_camera()
    if street.camera.projection != Camera3D.PROJECTION_ORTHOGONAL:
        fail("The view key did not switch the camera to orthographic.")
        return
    click.position = street.camera.unproject_position(Vector3(15, 0.16, 4))
    root.push_input(click, true)
    if street.waypoints.is_empty():
        fail("An isometric pavement click did not create a walking route.")
        return
    key.keycode = KEY_H
    root.push_input(key, true)
    if street.hud.visible:
        fail("The hide key left the player hints visible.")
        return
    if street.block.catalog.size() != 42 or street.people.size() != 14:
        fail("The street is missing a kit mesh or a resident.")
        return
    var next_clip = street.animation(street.guide, "walk")
    if not next_clip or not next_clip.current_animation.to_lower().ends_with("walk"):
        fail("The resident could not play the retargeted walk cycle.")
        return
    print(
        "Passed street and isometric clicks, arrival, solid walls, view switch, HUD, 42 meshes, cast and walk cycle."
    )
    street.queue_free()
    await process_frame
    quit()


func fail(message: String):
    push_error(message)
    quit(1)
