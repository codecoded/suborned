extends Node3D

# Architecture, occupancy boundaries and occluders share each placement transform.
const KIT = preload("res://assets/meridian.glb")
var catalog = {}
var pieces = []
var solids = []
var closed_passages: Array[Rect2] = []
var optimized = true
var turned = true
var batches = []
var occluders = []
var grouped = true
var detail = preload("res://detail_ranges.gd").new()


func build(use_lods: bool, corner: bool, combine: bool = true):
    optimized = use_lods
    grouped = combine
    turned = corner
    var library = KIT.instantiate()
    for part in library.find_children("*", "MeshInstance3D", true, false):
        catalog[String(part.name)] = part.mesh
    library.free()
    for x in [-6, 0, 6, 12]:
        place(["SortingVault", "Vault", "BookingVault", "SortingVault"][int((x + 6) / 6)], x, 0, 0)
        core(x, -5.7, 5.9, 3.2, 4.0)
    place("Marquee", 3, -.38, 0, 4.1)
    place("ClockTower", -12, -3.2, 0)
    core(-12, -3.2, 6.1, 6.1, 12.0)
    place("Rotunda", 20, -3.0, 0)
    core(20, -3.0, 6.1, 6.1, 8.4)
    for x in [-57, -51, -45, -39, -33, -27, -21]:
        place("DispatchBay" if x % 4 == 1 else "RibbonBay", x, -1, 0)
        core(x, -5, 5.95, 7.85, 10.0)
    for x in [37, 43, 49, 55, 61, 67, 73]:
        place("RibbonBay", x, -3, 0)
        core(x, -7, 5.95, 7.85, 10.0)
    place("DispatchEnd", -21, -1, 0)
    place("DispatchEnd", 37, -11, 180)
    # The corner test moves the same three bays into an L behind the eastern frontage.
    for i in range(3):
        var x = 26.0 if turned else 37.0 + i * 6.0
        var z = -16.0 - i * 6.0 if turned else -34.0
        var yaw = -90 if turned else 0
        place("RibbonBay", x, z, yaw)
        if i == 0:
            place("DispatchEnd", x, z, yaw)
        var center = Vector3(x, 0, z) + Vector3(0, 0, -4).rotated(Vector3.UP, deg_to_rad(yaw))
        core(center.x, center.z, 7.85 if turned else 5.95, 5.95 if turned else 7.85, 10)
    for x in [-54, -48, -42, -36, -30, -24, -18, 18, 24, 30, 36, 42, 48, 54]:
        place("RibbonBay", x, 25, 180)
        core(x, 29, 5.95, 7.85, 10.0)
    for x in range(-72, 79, 6):
        if x < 23 or x > 36:
            place("DispatchBay" if x % 4 == 0 else "RibbonBay", x, -32, 0)
    for p in [
        Vector2(-78, -18),
        Vector2(-86, 8),
        Vector2(-110, -42),
        Vector2(-89, 34),
        Vector2(81, -42),
        Vector2(91, 1),
        Vector2(99, 29),
        Vector2(-62, -57),
        Vector2(-9, -88),
        Vector2(39, -77),
        Vector2(74, -85),
        Vector2(-29, -131)
    ]:
        place("Spire", p.x, p.y, 0)
    for p in [Vector2(-30, 3.7), Vector2(9, 2.9), Vector2(32, 4.6), Vector2(-1, 22)]:
        place("LetterBox", p.x, p.y, 0)
        core(p.x, p.y, .8, .62, 0.0)
    for x in [-19.0, 3.8, 26.5, 45.0]:
        place("Bench", x, 4.9, 180)
        core(x, 4.9, 1.92, .57, 0.0)
    for p in [Vector3(-4, 18, 1.6), Vector3(5.5, -15, 1.8), Vector3(14.5, 32, 5.25)]:
        place("ParcelTrolley", p.x, p.z, p.y)
        core(p.x, p.z, 1.48, .72, 0.0)
    for p in [
        Vector2(-26, 5.2), Vector2(-9, 5.98), Vector2(17.9, 5.97), Vector2(37, 5.95), Vector2(7.4, 20.7)
    ]:
        place("FlowerTrough", p.x, p.y, 0)
        core(p.x, p.y, 3.72, 1.21, 0.0)
    render_placements()


func place(id: String, x: float, z: float, yaw: float, height: float = 0.):
    var basis = Basis(Vector3.UP, deg_to_rad(yaw))
    pieces.append([id, Transform3D(basis, Vector3(x, .16 + height, z))])


func core(x: float, z: float, w: float, d: float, h: float):
    solids.append(Rect2(x - w * .5, z - d * .5, w, d))
    if h <= 0:
        return
    var box = BoxOccluder3D.new()
    box.size = Vector3(w * .93, h * .95, d * .93)
    var occ = OccluderInstance3D.new()
    occ.occluder = box
    occ.position = Vector3(x, .16 + h * .48, z)
    add_child(occ)
    occluders.append(occ)


func clear_at(p: Vector2, margin: float = .27):
    if p.x < -56 or p.x > 68 or p.y < -29 or p.y > 24:
        return false
    for passage in closed_passages:
        if passage.grow(margin).has_point(p):
            return false
    for rect in solids:
        if rect.grow(margin).has_point(p):
            return false
    return true


func render_placements():
    var groups = {}
    for rec in pieces:
        var id = rec[0]
        var tr: Transform3D = rec[1]
        if not optimized:
            var m = MeshInstance3D.new()
            m.mesh = catalog[id + "_L0"]
            m.transform = tr
            add_child(m)
            batches.append(m)
            continue
        var small = id in ["LetterBox", "Bench", "ParcelTrolley", "FlowerTrough"]
        var band = "props" if small else ("landmark" if id == "ClockTower" else "facade")
        var cell = Vector2i(floori(tr.origin.x / 18), floori(tr.origin.z / 18))
        var k = (band if grouped else id) + "|" + str(cell)
        if not groups.has(k):
            groups[k] = []
        groups[k].append(rec)
    for key in groups:
        var entries = groups[key]
        var id = entries[0][0]
        # Moving a local group keeps the renderer's range test centred on the modules in that group.
        var center = Vector3.ZERO
        for rec in entries:
            center += rec[1].origin
        center /= float(entries.size())
        var small = id in ["LetterBox", "Bench", "ParcelTrolley", "FlowerTrough"]
        var near = 21.0 if small else (55.0 if id == "ClockTower" else 29.0)
        var far = 43.0 if small else (104.0 if id == "ClockTower" else 64.0)
        var bounds = AABB()
        for lod in range(3):
            var node: GeometryInstance3D
            if grouped:
                var st = SurfaceTool.new()
                st.begin(Mesh.PRIMITIVE_TRIANGLES)
                for rec in entries:
                    var local: Transform3D = rec[1]
                    local.origin -= center
                    var source: Mesh = catalog[rec[0] + "_L" + str(lod)]
                    st.append_from(source, 0, local)
                    st.set_material(source.surface_get_material(0))
                var combined = MeshInstance3D.new()
                combined.mesh = st.commit()
                node = combined
                if lod == 0:
                    bounds = combined.mesh.get_aabb()
                combined.custom_aabb = bounds
            else:
                var mm = MultiMesh.new()
                mm.transform_format = MultiMesh.TRANSFORM_3D
                mm.mesh = catalog[id + "_L" + str(lod)]
                mm.instance_count = entries.size()
                for i in range(entries.size()):
                    var local: Transform3D = entries[i][1]
                    local.origin -= center
                    mm.set_instance_transform(i, local)
                var instances = MultiMeshInstance3D.new()
                instances.multimesh = mm
                node = instances
            node.position = center
            detail.add(node, lod, Vector4(0, near, far, 90. if small else 250.), 0)
            node.visibility_range_begin_margin = 1.4
            node.visibility_range_end_margin = 1.4
            if lod == 2 and small:
                node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
            add_child(node)
            batches.append(node)
