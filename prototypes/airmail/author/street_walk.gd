extends SceneTree


class Walk:
    extends Node
    var street
    var frame = 0

    func key(code):
        var event = InputEventKey.new()
        event.pressed = true
        event.keycode = code
        get_viewport().push_input(event, true)

    func _process(_delta):
        match frame:
            0:
                key(KEY_H)
            48:
                street.walk_to(Vector2(8.45, -14.2))
            247:
                key(KEY_TAB)
                street.zoom = 18.
                street.walk_to(Vector2(12.45, -19.6))
            357:
                # The second shot starts across the tram line, on a real pavement.
                key(KEY_TAB)
                street.waypoints.clear()
                street.guide.position = Vector3(-6.5, .165, 22.65)
                street.guide.rotation.y = PI / 2.
                street.yaw = -.53
                street.update_camera()
            389:
                street.walk_to(Vector2(3.8, 22.6))
            624:
                street.court_audio.silence()
        if frame >= 60 and frame < 162:
            street.yaw -= .0034
        if frame >= 260 and frame < 336:
            street.yaw += .0042
        if frame >= 429 and frame < 594:
            street.yaw -= .0023
        frame += 1


func _initialize():
    call_deferred("record")


func record():
    if not "memory=1" in OS.get_cmdline_user_args():
        push_error("Run with -- memory=1 to isolate the game save.")
        quit(1)
        return
    var street = load("res://street.tscn").instantiate()
    root.add_child(street)
    var walk = Walk.new()
    walk.street = street
    walk.process_priority = 50
    root.add_child(walk)
