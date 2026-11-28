extends SceneTree


func _initialize():
    var args = OS.get_cmdline_user_args()
    var bytes = FileAccess.get_file_as_bytes(args[0])
    const size = Vector3i(50, 12, 20)
    var stride = size.x * size.y * 4
    if bytes.size() != stride * size.z:
        push_error("Invalid irradiance volume dimensions.")
        quit(1)
        return
    var slices: Array[Image] = []
    for z in size.z:
        slices.append(
            Image.create_from_data(
                size.x, size.y, false, Image.FORMAT_RGBA8, bytes.slice(z * stride, (z + 1) * stride)
            )
        )
    var texture = ImageTexture3D.new()
    var result = texture.create(Image.FORMAT_RGBA8, size.x, size.y, size.z, false, slices)
    if result == OK:
        result = ResourceSaver.save(texture, args[1], ResourceSaver.FLAG_COMPRESS)
    quit(0 if result == OK else 1)
