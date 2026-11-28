extends SceneTree


func _initialize():
    call_deferred("write_positions")


func write_positions():
    var block = preload("res://block.gd").new()
    root.add_child(block)
    block.build(false, true)
    var locations = []
    for part in block.pieces:
        var transform: Transform3D = part[1]
        locations.append(
            [
                part[0],
                transform.origin.x,
                transform.origin.y,
                transform.origin.z,
                atan2(transform.basis.z.x, transform.basis.z.z)
            ]
        )
    var file = FileAccess.open(OS.get_cmdline_user_args()[0], FileAccess.WRITE)
    file.store_string(JSON.stringify(locations))
    file.close()
    block.free()
    quit()
