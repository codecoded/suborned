extends SceneTree


class CameraMove:
    extends Node
    var street
    var frame = 0

    func _process(_delta):
        var shot = mini(frame / 105, 3)
        var time = clampf(float(frame % 105) / 104., 0., 1.)
        street.tour((float(frame) / 419.) * 0.88, "iso" if shot == 3 else "street")
        street.update_camera()
        street.streetscape.presentation(shot < 2)
        if shot < 2:
            var camera = street.camera
            camera.projection = Camera3D.PROJECTION_PERSPECTIVE
            if shot == 0:
                camera.fov = 54.
                camera.position = Vector3(32, 4.3, 23.8).lerp(Vector3(21.5, 3.1, 16.9), time)
                camera.look_at(Vector3(-6, 8.2, -1.6).lerp(Vector3(-4, 7., -1.8), time))
            else:
                camera.fov = 58.
                camera.position = Vector3(13, 2.57, 4.83).lerp(Vector3(7.1, 2.46, 4.53), time)
                camera.look_at(Vector3(-2, 4.13, -2.5))
        frame += 1


func _initialize():
    call_deferred("film")


func film():
    var street = load("res://street.tscn").instantiate()
    root.add_child(street)
    street.hud.visible = false
    var move = CameraMove.new()
    move.street = street
    move.process_priority = 50
    root.add_child(move)
