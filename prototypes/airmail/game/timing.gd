extends Node

var host
var options
var index = 0
var warm = 80
var samples = 240
var data = []
var last = 0
var finishing = false
var path = ""
var shots = []
var snapshot = false
var shot_ticks = 0
var jobs = []
var root_path = ""
var suite = false
var soak_time = 0.
var soak_samples = []


func start(owner_node, opts):
    host = owner_node
    options = opts.duplicate()
    root_path = options["out"]
    path = root_path
    suite = options.has("suite") or options.has("lightbench")
    if options.get("raster", "") == "1080":
        # Render the same number of pixels even if macOS limits the window's height.
        var window = get_window()
        window.content_scale_size = Vector2i(1920, 1080)
        window.content_scale_aspect = Window.CONTENT_SCALE_ASPECT_KEEP
        window.content_scale_mode = Window.CONTENT_SCALE_MODE_VIEWPORT
    samples = maxi(30, int(options.get("frames", "240")))
    warm = maxi(35, int(options.get("warm", "80")))
    DirAccess.make_dir_recursive_absolute(path)
    RenderingServer.viewport_set_measure_render_time(get_viewport().get_viewport_rid(), true)
    shots = (
        ["hero", "vault", "aerial", "detail", "lane", "street", "tactical", "threshold", "collection", "sill", "stone"]
        if options.has("shots")
        else []
    )
    if suite:
        var cases = [
            ["full", "turned", "street"],
            ["instances", "turned", "street"],
            ["optimized", "turned", "street"],
            ["full", "turned", "iso"],
            ["optimized", "turned", "iso"],
            ["lod", "open", "corner"],
            ["optimized", "open", "corner"],
            ["lod", "turned", "corner"],
            ["optimized", "turned", "corner"]
        ]
        if options.has("lightbench"):
            cases = []
            for view in ["street", "threshold", "iso"]:
                for light in ["original", "arcade", "surfaced"]:
                    cases.append([light, "turned", view])
        for rep in range(int(options.get("repeats", "2"))):
            var order = cases.duplicate()
            if rep % 2 == 1:
                order.reverse()
            for row in order:
                jobs.append([row[0], row[1], row[2], rep])
        next_job()
    last = Time.get_ticks_usec()


func next_job():
    var row = jobs.pop_front()
    options["mode"] = "optimized" if options.has("lightbench") else row[0]
    options["corner"] = row[1]
    options["view"] = row[2]
    options["repeat"] = row[3]
    path = root_path + "/%s-%s-%s-%d" % row
    DirAccess.make_dir_recursive_absolute(path)
    if options.has("lightbench"):
        options["lighting"] = row[0]
        host.arcade_light.set_look(row[0])
    else:
        host.change_block(row[0], row[1])
    index = 0
    data.clear()
    last = Time.get_ticks_usec()


func tick():
    if finishing:
        return
    if snapshot:
        if shots.is_empty():
            finish()
            return
        # Set after the controller places the normal follow camera, before rendering.
        host.call_deferred("portrait", shots[0])
        shot_ticks += 1
        if shot_ticks == 8:
            take_image()
        return
    var now = Time.get_ticks_usec()
    if options.has("soak"):
        if index == 0:
            soak_time = now * .000001
        var age = now * .000001 - soak_time
        if soak_samples.is_empty() or age >= float(soak_samples[-1][0]) + 1.0:
            soak_samples.append(
                [
                    age,
                    Performance.get_monitor(Performance.RENDER_TEXTURE_MEM_USED),
                    Performance.get_monitor(Performance.OBJECT_RESOURCE_COUNT)
                ]
            )
        host.tour(fposmod(age, 18.) / 18., "street" if int(age / 18.) % 2 == 0 else "iso")
        index += 1
        if age >= float(options["soak"]):
            var f = FileAccess.open(path + "/soak.json", FileAccess.WRITE)
            f.store_string(JSON.stringify(soak_samples))
            f.close()
            shut_down()
        return
    if index > warm and last > 0:
        var vp = get_viewport().get_viewport_rid()
        data.append(
            [
                float(now - last) / 1000.,
                RenderingServer.viewport_get_measured_render_time_gpu(vp),
                RenderingServer.viewport_get_measured_render_time_cpu(vp),
                RenderingServer.viewport_get_render_info(
                    vp,
                    RenderingServer.VIEWPORT_RENDER_INFO_TYPE_VISIBLE,
                    RenderingServer.VIEWPORT_RENDER_INFO_DRAW_CALLS_IN_FRAME
                ),
                RenderingServer.viewport_get_render_info(
                    vp,
                    RenderingServer.VIEWPORT_RENDER_INFO_TYPE_VISIBLE,
                    RenderingServer.VIEWPORT_RENDER_INFO_PRIMITIVES_IN_FRAME
                ),
                RenderingServer.viewport_get_render_info(
                    vp,
                    RenderingServer.VIEWPORT_RENDER_INFO_TYPE_SHADOW,
                    RenderingServer.VIEWPORT_RENDER_INFO_DRAW_CALLS_IN_FRAME
                ),
                RenderingServer.viewport_get_render_info(
                    vp,
                    RenderingServer.VIEWPORT_RENDER_INFO_TYPE_SHADOW,
                    RenderingServer.VIEWPORT_RENDER_INFO_PRIMITIVES_IN_FRAME
                ),
                Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME),
                Performance.get_monitor(Performance.RENDER_TEXTURE_MEM_USED)
            ]
        )
    last = now
    var t = clampf(float(index - warm) / samples, 0., 1.)
    host.tour(t, options.get("view", "street"))
    index += 1
    if data.size() >= samples:
        if shots.is_empty() or (suite and not jobs.is_empty()):
            finish()
        else:
            snapshot = true


func take_image():
    await RenderingServer.frame_post_draw
    if shots.is_empty():
        return
    var image = get_viewport().get_texture().get_image()
    var filename = path + "/" + shots.pop_front() + ".jpg"
    if image.save_jpg(filename, .92) != OK:
        push_error("Could not save " + filename)
    shot_ticks = 0


func finish():
    finishing = true
    var vp = get_viewport()
    var info = {
        "renderer": RenderingServer.get_current_rendering_method(),
        "driver": RenderingServer.get_current_rendering_driver_name(),
        "device": RenderingServer.get_video_adapter_name(),
        "viewport": str(vp.get_texture().get_size()),
        "render_scale": vp.scaling_3d_scale,
        "vsync": DisplayServer.window_get_vsync_mode(),
        "options": options,
        "samples": data,
        "columns":
        [
            "wall_ms",
            "gpu_ms",
            "render_cpu_ms",
            "visible_draws",
            "visible_primitives",
            "shadow_draws",
            "shadow_primitives",
            "total_draws",
            "texture_bytes"
        ]
    }
    var f = FileAccess.open(path + "/run.json", FileAccess.WRITE)
    if f == null:
        push_error("Cannot save benchmark")
        get_tree().quit(2)
        return
    f.store_string(JSON.stringify(info))
    f.close()
    print(
        "MERIDIAN BENCH ",
        path.get_file(),
        " ",
        data.size(),
        " frames ",
        info["device"],
        " ",
        info["viewport"]
    )
    if suite and not jobs.is_empty():
        finishing = false
        snapshot = false
        next_job()
    else:
        shut_down()


func shut_down():
    finishing = true
    var tree = get_tree()
    tree.create_timer(.15, true, false, true).timeout.connect(tree.quit)
    host.call_deferred("queue_free")
