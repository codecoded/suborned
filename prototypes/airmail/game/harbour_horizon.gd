extends Node3D

const KIT = preload("res://assets/sky_district.glb")
const SURFACE = preload("res://horizon_surface.gdshader")
# Width and height are in metres. Each neighbourhood has a recognisable
# silhouette at walking height. The gaps between them frame the far shoreline.
const PRECINCTS = [
    # Skyport seen above the Evening Post. The rail crosses in front of its decks.
    ["Terrace", 42, -67, 18, 28],
    ["CivicFins", 51, -91, 18, 43],
    ["Skyport", 70, -82, 24, 48],
    ["StepSpire", 85, -104, 17, 50],
    ["Terrace", 92, -84, 19, 30],
    ["Needle", 106, -95, 14, 58],
    ["Lookout", 108, -52, 17, 45],
    ["Terrace", 123, -61, 18, 25],
    ["Starburst", 131, -95, 11, 51],
    ["CivicFins", 91, -140, 14, 57],
    ["Needle", 106, -130, 10, 70],
    ["Terrace", 123, -118, 14, 40],
    ["StepSpire", 141, -85, 15, 55],
    ["Starburst", 146, -56, 10, 54],
    ["Terrace", 77, -120, 13, 40],
    # The upland civic centre, in depth behind the flower arcade.
    ["Terrace", -43, -79, 22, 24],
    ["CivicFins", -25, -90, 19, 53],
    ["Terrace", -7, -66, 17, 28],
    ["Starburst", -10, -104, 14, 61],
    ["Planetarium", 11, -89, 30, 33],
    ["Needle", 24, -109, 12, 50],
    ["Terrace", 25, -71, 15, 25],
    ["StepSpire", -51, -106, 18, 38],
    # The east end of the tram avenue.
    ["Terrace", 94, -21, 20, 26],
    ["CivicFins", 118, -25, 19, 49],
    ["StepSpire", 137, -4, 23, 51],
    ["Lookout", 106, 13, 17, 41],
    ["Terrace", 111, 33, 24, 29],
    ["Needle", 143, 34, 15, 58],
    # Shopping terraces across the tracks; a lower interval opens onto the water.
    ["Terrace", 78, 69, 22, 31],
    ["Starburst", 92, 94, 13, 55],
    ["CivicFins", 59, 92, 20, 45],
    ["Lookout", 36, 76, 19, 37],
    ["Terrace", 13, 66, 20, 24],
    ["Planetarium", -8, 97, 29, 41],
    ["Needle", -29, 103, 15, 57],
    ["Terrace", -35, 73, 17, 25],
    ["StepSpire", -57, 89, 19, 38],
    # The west pier is visible on the return trip from the press.
    ["Terrace", -85, 44, 18, 27],
    ["CivicFins", -109, 48, 23, 47],
    ["Lookout", -91, 16, 18, 38],
    ["Terrace", -102, -13, 19, 24],
    ["Needle", -127, -21, 16, 61],
    ["Starburst", -114, -48, 12, 48],
    ["Terrace", -93, -54, 17, 27],
    ["StepSpire", -87, -82, 19, 42]
]
var library = {}
var buckets = {}
var finish = ShaderMaterial.new()
var track = Curve3D.new()
var cars = []
var taxis = []
var elapsed = 0.


func setup():
    var sources = KIT.instantiate()
    for part in sources.find_children("*", "MeshInstance3D", true, false):
        library[String(part.name)] = part.mesh
    sources.free()
    finish.shader = SURFACE
    for i in range(PRECINCTS.size()):
        var p = PRECINCTS[i]
        var mesh: Mesh = library[p[0]]
        var size = mesh.get_aabb().size
        var horizontal = float(p[3]) / maxf(size.x, size.z)
        var scale3 = Vector3(horizontal, float(p[4]) / size.y, horizontal)
        var basis = Basis(Vector3.UP, (i % 5 - 2) * .22).scaled_local(scale3)
        var origin = Vector3(p[1], -.25 - mesh.get_aabb().position.y * scale3.y, p[2])
        bucket(origin).append_from(mesh, 0, Transform3D(basis, origin))
    make_track()
    for cell in buckets:
        var mesh = buckets[cell].commit()
        if mesh:
            var section = display(mesh, "Skyline %s" % cell)
            section.material_override = finish
    buckets.clear()
    for id in ["RailHead", "RailCoach", "RailHead"]:
        cars.append(display(library[id], id))
    for id in ["CoralCab", "TealCab"]:
        taxis.append(display(library[id], id))
    advance(0.)


func bucket(at: Vector3) -> SurfaceTool:
    var cell = Vector2i(floori(at.x / 52.), floori(at.z / 52.))
    if not buckets.has(cell):
        var st = SurfaceTool.new()
        st.begin(Mesh.PRIMITIVE_TRIANGLES)
        buckets[cell] = st
    return buckets[cell]


func display(mesh: Mesh, title: String) -> MeshInstance3D:
    var node = MeshInstance3D.new()
    node.name = title
    node.mesh = mesh
    node.material_override = finish
    node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
    add_child(node)
    return node


func rail_point(a: float) -> Vector3:
    return Vector3(23. + 97. * cos(a), 24. + 1.2 * sin(a + .35), -94. + 46. * sin(a))


func section_at(at: Vector3, right: Vector3, section: Vector2) -> Vector3:
    return at + right * section.x + Vector3.UP * section.y


func beam_panel(st: SurfaceTool, corners: Array, normal: Vector3, color: Color):
    var order = (
        [0, 2, 1, 0, 3, 2]
        if (corners[1] - corners[0]).cross(corners[2] - corners[0]).dot(normal) > 0.
        else [0, 1, 2, 0, 2, 3]
    )
    for i in order:
        st.set_normal(normal)
        st.set_uv(Vector2(.5625, .5))
        st.set_color(color)
        st.add_vertex(corners[i])


func make_track():
    var cross_section = [
        Vector2(-.80, 0),
        Vector2(.80, 0),
        Vector2(.84, -.26),
        Vector2(.78, -.42),
        Vector2(.59, -.56),
        Vector2(.48, -1.42),
        Vector2(-.48, -1.42),
        Vector2(-.59, -.56),
        Vector2(-.78, -.42),
        Vector2(-.84, -.26)
    ]
    var ivory = Color("#e9ddbc").srgb_to_linear()
    var stripe = Color("#438d84").srgb_to_linear()
    track.bake_interval = .9
    for i in range(145):
        track.add_point(rail_point(TAU * i / 144.))
    for i in range(144):
        var a = TAU * i / 144.
        var b = TAU * (i + 1) / 144.
        var p = rail_point(a)
        var q = rail_point(b)
        var right = Vector3(46. * cos(a), 0, 97. * sin(a)).normalized()
        var right_b = Vector3(46. * cos(b), 0, 97. * sin(b)).normalized()
        var st = bucket((p + q) * .5)
        for k in range(cross_section.size()):
            var j = (k + 1) % cross_section.size()
            var edge: Vector2 = cross_section[j] - cross_section[k]
            var normal = (-right * edge.y + Vector3.UP * edge.x).normalized()
            beam_panel(
                st,
                [
                    section_at(p, right, cross_section[k]),
                    section_at(q, right_b, cross_section[k]),
                    section_at(q, right_b, cross_section[j]),
                    section_at(p, right, cross_section[j])
                ],
                normal,
                stripe if k in [2, 8] else ivory
            )
        if i % 8 == 0:
            var yaw = atan2(right.x, right.z) - PI * .5
            var turn = Basis(Vector3.UP, yaw)
            var foot = Vector3(p.x, 0, p.z)
            var cap: Mesh = library.RailSaddle
            var shaft: Mesh = library.RailColumn
            var h = p.y - 1.42 - cap.get_aabb().size.y
            st.append_from(library.RailFoot, 0, Transform3D(turn, foot))
            st.append_from(
                shaft, 0, Transform3D(turn.scaled_local(Vector3(1, h / shaft.get_aabb().size.y, 1)), foot)
            )
            st.append_from(cap, 0, Transform3D(turn, p - Vector3.UP * 1.42))


func advance(delta: float):
    elapsed += delta
    var length = track.get_baked_length()
    for i in range(cars.size()):
        var distance = fposmod(length * .145 + elapsed * 11.8 - i * 18.2, length)
        var p = track.sample_baked(distance, true)
        var along = track.sample_baked(fposmod(distance + 1., length), true) - p
        cars[i].position = p
        cars[i].rotation.y = atan2(along.x, along.z) + (PI if i == 2 else 0.)
    for i in range(taxis.size()):
        var a = [.49, 2.61][i] + elapsed * [.071, -.054][i]
        var vel = Vector3(-98. * sin(a), 0, 55. * cos(a)) * (1. if i == 0 else -1.)
        # Both lanes climb past the civic spires and descend over the lower quays.
        var climb = maxf(31. * smoothstep(-.18, .90, -sin(a)), 11. * smoothstep(.64, .99, cos(a)))
        taxis[i].position = Vector3(
            17. + 98. * cos(a), [39., 48.][i] + climb + 1.4 * sin(2. * a), -56. + 55. * sin(a)
        )
        taxis[i].rotation.y = atan2(vel.x, vel.z)
        taxis[i].rotation.z = .07 * cos(a)
