extends RefCounted

const Builder = preload("res://court_builder.gd")
var yard


func build(scenery):
    yard = scenery
    press_room()
    transfer_bridge()
    returns_dock()


func press_room():
    var node = yard.group("Dispatch island", Vector3(-.15, yard.GROUND, -17.275))
    var b = Builder.new(node, yard.mats)
    b.box(Vector3(0, 1.23, 0), Vector3(9.30, 2.46, 3.15), "brick")
    b.box(Vector3(0, .20, 0), Vector3(9.43, .4, 3.23), "iron")
    for side in [-1., 1.]:
        var face = side * 1.60
        for x in [-3.36, -1.18, 1.10, 3.24]:
            b.box(Vector3(x, 1.46, face), Vector3(1.94, 1.38, .14), "enamel")
            b.box(Vector3(x, 1.54, face + side * .083), Vector3(1.70, 1.02, .09), "shadow")
            for k in range(6):
                b.box(Vector3(x, 1.14 + k * .164, face + side * .14), Vector3(1.72, .041, .065), "zincrim")
        b.box(Vector3(0, 2.48, face), Vector3(9.62, .18, .32), "enamel")
    # Rolled zinc roof and a raised, glazed monitor over the bindery.
    for side in [-1., 1.]:
        var zz = side * 1.83
        b.sheet(
            Vector3(-4.95, 2.52, zz),
            Vector3(4.95, 2.52, zz),
            Vector3(4.95, 3.03, 0),
            Vector3(-4.95, 3.03, 0),
            "zincrim"
        )
        for x in range(-4, 5):
            b.beam(Vector3(x, 2.54, zz), Vector3(x, 3.05, 0), .032, "iron", 6)
    b.box(Vector3(.25, 3.15, 0), Vector3(5.2, .57, .94), "enamel")
    for x in range(-2, 3):
        b.box(Vector3(x + .25, 3.2, .485), Vector3(.81, .29, .035), "opal")
    b.box(Vector3(.25, 3.47, 0), Vector3(5.55, .14, 1.21), "zincrim")
    # A deep open dispensing window and its flywheel make the blind end a working front.
    var face = -4.68
    b.sheet(
        Vector3(face, .79, -1.30),
        Vector3(face, .79, 1.30),
        Vector3(face, 2.31, 1.30),
        Vector3(face, 2.31, -1.30),
        "rooms",
        Rect2(.004, .004, .492, .492)
    )
    for side in [-1.36, 1.36]:
        b.box(Vector3(face - .09, 1.52, side), Vector3(.24, 1.76, .11), "enamel")
    b.box(Vector3(face - .16, .76, 0), Vector3(.46, .11, 2.91), "timber")
    b.box(Vector3(face - .16, 2.35, 0), Vector3(.54, .17, 2.96), "enamel")
    b.poster(Vector3(face - .02, .16, .56), .69, .53, 0, -PI * .5)
    b.poster(Vector3(face - .02, .20, -.72), .54, .48, 1, -PI * .5)
    yard.press_wheel = Node3D.new()
    node.add_child(yard.press_wheel)
    yard.press_wheel.position = Vector3(face - .16, 1.28, -.70)
    var wheel = Builder.new(yard.press_wheel, yard.mats)
    for i in range(20):
        var a = TAU * i / 20.
        var c = TAU * (i + 1) / 20.
        var v = Vector3(0, sin(a), cos(a)) * .38
        wheel.beam(v, Vector3(0, sin(c), cos(c)) * .38, .025, "iron", 6)
        if i % 4 == 0:
            wheel.beam(Vector3.ZERO, v, .018, "iron", 6)
    wheel.finish()
    for side in [-1., 1.]:
        b.sheet(
            Vector3(side * 4.66, 2.44, 1.62),
            Vector3(side * 4.66, 2.44, -1.62),
            Vector3(side * 4.66, 3.0, 0),
            Vector3(side * 4.66, 3.0, 0),
            "enamel"
        )
    b.poster(Vector3(-2.66, .59, 1.77), 1.2, 1.55, 0)
    b.poster(Vector3(-.96, .68, 1.77), 1.02, 1.31, 1)
    b.poster(Vector3(2.15, .81, -1.77), 1.4, 1.2, 2, PI)
    # An enamel blade hangs over the news window, visible from both turns.
    b.beam(Vector3(-4.24, 2.45, -1.18), Vector3(-4.24, 4.95, -1.18), .064, "iron")
    b.beam(Vector3(-4.24, 4.87, -1.18), Vector3(-5.7, 4.87, -1.18), .050, "zincrim")
    b.beam(Vector3(-4.24, 4.28, -1.18), Vector3(-5.32, 4.86, -1.18), .031, "iron")
    b.box(Vector3(-5.04, 3.91, -1.17), Vector3(1.18, 1.69, .13), "enamel")
    for side in [-1., 1.]:
        var z = -1.17 + side * .071
        b.sheet(
            Vector3(-5.60, 3.10, z),
            Vector3(-4.48, 3.10, z),
            Vector3(-4.48, 4.73, z),
            Vector3(-5.60, 4.73, z),
            "blade"
        )
    b.finish(["brick", "zincrim"])
    yard.city.block.solids.append(yard.ISLAND)
    yard.cut_groups.append(
        [
            node,
            AABB(
                Vector3(yard.ISLAND.position.x, yard.GROUND, yard.ISLAND.position.y),
                Vector3(yard.ISLAND.size.x, 3.60, yard.ISLAND.size.y)
            )
        ]
    )


func transfer_bridge():
    var node = yard.group("Overhead parcel conveyor", Vector3(8.7, 4.38, -16.95))
    var b = Builder.new(node, yard.mats)
    var length = 12.08
    b.box(Vector3(0, .07, 0), Vector3(2.00, .22, length), "iron")
    b.box(Vector3(0, 1.67, 0), Vector3(2.06, .19, length + .17), "enamel")
    for side in [-1., 1.]:
        var x = side * .88
        b.box(Vector3(x, .30, 0), Vector3(.15, .32, length), "enamel")
        for j in range(9):
            var z = -5.48 + j * 1.36
            b.box(Vector3(x, .99, z), Vector3(.10, 1.12, 1.22), "shadow")
            b.box(Vector3(x + side * .07, 1.23, z), Vector3(.038, .51, 1.14), "opal")
            b.beam(
                Vector3(x + side * .10, .43, z - .64), Vector3(x + side * .10, 1.57, z + .64), .042, "zincrim"
            )
            b.box(Vector3(x + side * .12, .96, z - .65), Vector3(.10, 1.24, .08), "iron")
    for z in [-5.1, -2.7, 0, 2.7, 5.1]:
        b.box(Vector3(0, -.095, z), Vector3(2.13, .17, .11), "zincrim")
    b.finish(["iron", "enamel"])
    yard.cut_groups.append(
        [node, AABB(node.position + Vector3(-1., -.15, -length * .5), Vector3(2., 2., length))]
    )


func returns_dock():
    var node = yard.group("Return cases", Vector3(15.0, yard.GROUND, -18.5))
    var b = Builder.new(node, yard.mats)
    b.box(Vector3(.3, .17, 0), Vector3(2.35, .34, 5.0), "brick")
    b.box(Vector3(1.12, 1.54, 0), Vector3(.31, 3.07, 4.83), "enamel")
    b.box(Vector3(.12, 3.14, 0), Vector3(2.64, .16, 5.26), "zincrim")
    for z in [-2.38, 2.38]:
        b.beam(Vector3(-.96, .3, z), Vector3(-.96, 3.12, z), .065, "enamel")
        b.beam(Vector3(-.96, 2.59, z), Vector3(-.32, 3.11, z), .038, "iron")
    for j in range(9):
        var z = -.07 + (j % 3) * .74 - 1.22
        var at = Vector3(-.35 + floori(j / 6.) * .78, .35 + floori((j % 6) / 3.) * .49, z)
        crate(b, at, j)
    b.poster(Vector3(-.03, .90, 2.44), 1.06, 1.27, 3)
    b.finish(["enamel", "zincrim"])
    yard.city.block.core(15.23, -18.5, 2.40, 5.02, 0.)
    yard.cut_groups.append([node, AABB(node.position + Vector3(-1.24, 0, -2.63), Vector3(2.90, 3.27, 5.26))])


func crate(b, at, row):
    for x in [-.42, .42]:
        for z in [-.30, .30]:
            b.box(at + Vector3(x, .235, z), Vector3(.074, .47, .065), "timber")
    for i in range(3):
        var y = .083 + i * .142
        for x in [-.43, .43]:
            b.box(
                at + Vector3(x, y, 0), Vector3(.034, .101, .62), "canvas" if (row + i) % 4 == 0 else "timber"
            )
        for z in [-.31, .31]:
            b.box(at + Vector3(0, y, z), Vector3(.88, .101, .039), "timber")
