extends SceneTree


# Record the ordinary street controller. All walking uses its pavement routes.
class Walk:
    extends Node
    var street
    var frame = 0
    var turned = false
    var followed = false
    var crossed = false
    var last_heard = 0

    func key(code):
        var event = InputEventKey.new()
        event.pressed = true
        event.keycode = code
        get_viewport().push_input(event, true)

    func _process(delta):
        var orders = street.orders
        if frame == 50:
            key(KEY_E)
        if frame == 133:
            key(KEY_1)
        if frame > 145 and not orders.pending.is_empty():
            if orders.heard == 1 and last_heard == 0:
                key(KEY_TAB)
                street.zoom = 15.
                street.walk_to(Vector2(-6, -14))
            if orders.heard == 1 and not followed and street.waypoints.is_empty():
                followed = true
                street.walk_to(Vector2(4, -14))
            if orders.heard == 2 and not turned:
                turned = true
                if street.iso:
                    key(KEY_TAB)
                street.walk_to(Vector2(8, -19))
            last_heard = orders.heard
            var citizen = orders.person(orders.chain[orders.heard])
            var offset = street.guide.position - citizen.position
            var look = atan2(offset.x, offset.z)
            if not street.iso:
                street.yaw = lerp_angle(street.yaw, look, 1. - exp(-delta * 2.8))
            else:
                street.yaw = lerp_angle(street.yaw, -2.84, 1. - exp(-delta * 2.2))
        if orders.done[0] and street.court.gate_lift > .92 and not crossed:
            crossed = true
            street.walk_to(Vector2(-2, -21))
            if not street.iso:
                key(KEY_TAB)
            street.yaw = 2.78
            street.zoom = 12.
            print("Recorded open shortcut at frame ", frame)
        if frame == 902:
            key(KEY_ESCAPE)
        frame += 1


func _initialize():
    call_deferred("record")


func record():
    var street = load("res://street.tscn").instantiate()
    root.add_child(street)
    var walk = Walk.new()
    walk.street = street
    walk.process_priority = 50
    root.add_child(walk)
