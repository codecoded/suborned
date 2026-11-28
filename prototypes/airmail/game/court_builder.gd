extends RefCounted

# One mesh per material in each small architectural group. Coordinates are local metres.
var host: Node3D
var materials: Dictionary
var tools = {}


func _init(parent: Node3D, finishes: Dictionary):
    host = parent
    materials = finishes


func tool(id: String) -> SurfaceTool:
    if not tools.has(id):
        var st = SurfaceTool.new()
        st.begin(Mesh.PRIMITIVE_TRIANGLES)
        st.set_material(materials[id])
        tools[id] = st
    return tools[id]


func box(at: Vector3, size: Vector3, id: String, angle = 0.):
    var b = BoxMesh.new()
    b.size = size
    tool(id).append_from(b, 0, Transform3D(Basis(Vector3.UP, angle), at))


func beam(a: Vector3, b: Vector3, radius: float, id: String, sides = 8):
    var direction = b - a
    var tube = CylinderMesh.new()
    tube.radial_segments = sides
    tube.rings = 0
    tube.height = direction.length()
    tube.top_radius = radius
    tube.bottom_radius = radius
    var tr = Transform3D(Basis(Quaternion(Vector3.UP, direction.normalized())), (a + b) * .5)
    tool(id).append_from(tube, 0, tr)


func sheet(a: Vector3, b: Vector3, c: Vector3, d: Vector3, id: String, region = Rect2(0, 0, 1, 1)):
    var st = SurfaceTool.new()
    st.begin(Mesh.PRIMITIVE_TRIANGLES)
    var n = (b - a).cross(c - a).normalized()
    var corners = [a, b, c, d]
    var uvs = [Vector2(0, 1), Vector2(1, 1), Vector2(1, 0), Vector2(0, 0)]
    # Godot's front face is clockwise. The normal faces the observer on the printed side.
    var t = (b - a).normalized()
    for i in range(4):
        st.set_normal(n)
        st.set_tangent(Plane(t, 1.))
        st.set_uv(region.position + uvs[i] * region.size)
        st.add_vertex(corners[i])
    for i in [0, 2, 1] if c == d else [0, 2, 1, 0, 3, 2]:
        st.add_index(i)
    tool(id).append_from(st.commit(), 0, Transform3D.IDENTITY)


func poster(at: Vector3, width: float, height: float, index: int, heading = 0.):
    var side = Vector3.RIGHT.rotated(Vector3.UP, heading) * width * .5
    var up = Vector3.UP * height
    var region = Rect2((index % 2) * .5 + .003, floori(index / 2.) * .5 + .003, .494, .494)
    sheet(at - side, at + side, at + side + up, at - side + up, "print", region)


func finish(shadow_materials: Array = []):
    for id in tools:
        var item = MeshInstance3D.new()
        item.mesh = tools[id].commit()
        item.name = id
        if id not in shadow_materials:
            item.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
        host.add_child(item)
    tools.clear()
