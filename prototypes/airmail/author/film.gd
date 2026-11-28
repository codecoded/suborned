extends SceneTree


class CameraMove:
    extends Node
    var street
    var frame = 0
    var postal = false

    func _process(_delta):
        if postal:
            material_walk()
            return
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

    func material_walk():
        var shot = mini(frame / 120, 4)
        var t = float(frame % 120) / 119.
        var cam = street.camera
        street.tour(t, "threshold" if shot == 3 else ("iso" if shot == 4 else "street"))
        street.update_camera()
        street.streetscape.presentation(shot == 0)
        if shot < 3:
            cam.projection = Camera3D.PROJECTION_PERSPECTIVE
            cam.fov = [54., 49., 50.][shot]
            if shot == 0:
                cam.position = Vector3(30,4.7,20.8).lerp(Vector3(21.5,3.4,15.2), t)
                cam.look_at(Vector3(-4,7.2,-1.6).lerp(Vector3(-3,6.2,-1.8), t))
            elif shot == 1:
                cam.position = Vector3(9.71,1.63,4.26).lerp(Vector3(9.37,1.36,4.04),t)
                cam.look_at(Vector3(9,1.25,3.12))
            else:
                cam.position = Vector3(3.96,1.32,-1.08).lerp(Vector3(4.92,1.18,-1.26),t)
                cam.look_at(Vector3(6.31,.79,-2.66).lerp(Vector3(7.48,.80,-2.71),t))
        frame += 1


func _initialize():
    call_deferred("film")


func film():
    var street = load("res://street.tscn").instantiate()
    street.run_options["avenue"] = "1"
    root.add_child(street)
    street.hud.visible = false
    var move = CameraMove.new()
    move.street = street
    move.postal = "postal" in OS.get_cmdline_user_args()
    move.process_priority = 50
    root.add_child(move)
