extends SceneTree


func _initialize():
    call_deferred("capture")


func capture():
    var street = load("res://street.tscn").instantiate()
    root.add_child(street)
    street.set_process(false)
    street.hud.visible = false
    street.guide.hide()
    street.streetscape.tram.hide()
    for actor in street.people:
        actor[0].hide()
    var environment = street.get_world_3d().environment
    # Always start from the original daylight, even when regenerating the shipped reflection.
    var sun_sky = PanoramaSkyMaterial.new()
    sun_sky.panorama = load("res://assets/sky.hdr")
    environment.sky.sky_material = sun_sky
    environment.tonemap_mode = Environment.TONE_MAPPER_LINEAR
    environment.tonemap_exposure = 1.
    var camera = street.camera
    camera.projection = Camera3D.PROJECTION_PERSPECTIVE
    camera.fov = 90.
    camera.position = Vector3(4.2, 2.75, 4.9)
    var faces = [
        [Vector3.RIGHT, Vector3.UP],
        [Vector3.LEFT, Vector3.UP],
        [Vector3.UP, Vector3.FORWARD],
        [Vector3.DOWN, Vector3.BACK],
        [Vector3.BACK, Vector3.UP],
        [Vector3.FORWARD, Vector3.UP]
    ]
    var output = OS.get_cmdline_user_args()[0]
    DirAccess.make_dir_recursive_absolute(output)
    for i in range(6):
        camera.look_at(camera.position + faces[i][0], faces[i][1])
        for j in range(6):
            await RenderingServer.frame_post_draw
        var result = root.get_texture().get_image().save_png(output.path_join(str(i) + ".png"))
        if result != OK:
            quit(1)
            return
    quit()
