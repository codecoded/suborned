extends RefCounted

# x, z, seconds at the counter, heading while stopped (degrees about up).
# Each circuit stays on its own pavement. Press workers can use the real shutter.
const COURT = [
    [
        4,
        [
            Vector4(-13.9, -14.4, .8, -45),
            Vector4(-6.8, -14.85, 0, 0),
            Vector4(6.2, -14.85, 0, 0),
            Vector4(12.95, -13.55, 2.8, 0),
            Vector4(6.4, -12.3, 1., 0),
            Vector4(-4, -13.05, 1.1, 0)
        ]
    ],
    [
        4,
        [
            Vector4(-10.1, -20.55, 2.2, 180),
            Vector4(-4.8, -20.6, .7, 170),
            Vector4(.8, -20.35, 1.7, 175),
            Vector4(-5.6, -19.75, 0, 0),
            Vector4(-11.6, -19.2, 0, 0),
            Vector4(-13.6, -20.65, 1.4, 180)
        ]
    ],
    [
        3,
        [
            Vector4(.65, -21.1, 1.8, 180),
            Vector4(7.2, -20.65, 2., 160),
            Vector4(10, -19.9, 1., -165),
            Vector4(8.1, -15., 0, 0),
            Vector4(-6.5, -14.75, 0, 0),
            Vector4(-7.5, -18.55, 1., -90)
        ]
    ],
    [
        3,
        [
            Vector4(-15.2, -18., 1.1, -105),
            Vector4(-16.5, -11.9, 0, 0),
            Vector4(-16.5, 1.75, 0, 0),
            Vector4(-9., 3.9, 2.4, 168),
            Vector4(-15.55, .55, 0, 0),
            Vector4(-15.6, -14.25, 0, 0)
        ]
    ],
    [
        3,
        [
            Vector4(8.05, -20.15, 1.6, 175),
            Vector4(12.7, -19.55, 2., 100),
            Vector4(13.05, -15.35, 0, 0),
            Vector4(8.6, -13., 1.4, 12)
        ]
    ]
]
const AVENUE = [
    [
        7,
        [
            Vector4(-52, 2.1, 2., 175),
            Vector4(-31, 2.05, 1.6, 173),
            Vector4(-17, 3.75, 0, 0),
            Vector4(-5.3, 3.45, 0, 0),
            Vector4(2.3, 1.1, 2.7, 180),
            Vector4(-13.1, 4.05, 0, 0),
            Vector4(-28.5, 3.95, 0, 0),
            Vector4(-45, 3.7, 1., -5)
        ]
    ],
    [
        7,
        [
            Vector4(4.22, 3.1, 2.3, -165),
            Vector4(16, 3.45, 0, 0),
            Vector4(30.3, 2.6, 1., 160),
            Vector4(48.35, 2.15, 2.1, 172),
            Vector4(62, 3.5, .8, 163),
            Vector4(42.5, 4.05, 0, 0),
            Vector4(25.3, 4.15, 0, 0),
            Vector4(13.6, 3.9, .9, 0)
        ]
    ],
    [
        8,
        [
            Vector4(-45, 22.75, 1., 5),
            Vector4(-22, 21.45, 1.3, 180),
            Vector4(-4.9, 21.3, 0, 0),
            Vector4(18, 21.6, 0, 0),
            Vector4(39, 22.6, 1.8, -171),
            Vector4(15.5, 23.45, .9, 0),
            Vector4(-7.3, 23.35, 1.4, 0),
            Vector4(-29, 23.55, 0, 0)
        ]
    ]
]
# People standing together have individual postures and alternate listening and talking.
# The heading points towards a companion, a window or the approaching tram.
const COURT_POSTS = [
    [-12.15, -21.32, 160, 5],
    [-11.18, -21.58, -117, 2],
    [-4.95, -15.12, 132, 1],
    [-3.85, -15.06, -122, 6],
    [1.2, -11.55, 8, 4],
    [2.35, -11.8, -47, 0],
    [9.4, -21.92, 156, 3],
    [10.65, -21.85, -126, 7],
    [12.78, -14.06, 39, 2]
]
const AVENUE_POSTS = [
    [-34., 5.62, 83, 3],
    [-32.86, 5.79, -97, 1],
    [-18.95, 2.85, 166, 5],
    [1.05, 5.91, 65, 2],
    [2.15, 6.09, -99, 4],
    [6., 5.85, 15, 6],
    [30.1, 4.95, 30, 0],
    [31.45, 5.38, -87, 7],
    [-20.25, 21.06, 163, 4],
    [10.4, 21.98, 135, 1],
    [11.5, 21.64, -135, 5],
    [32., 21.05, 167, 3]
]

var city
var cache = {}
var closed = false


func _init(street):
    city = street


func leg(a: Vector2, b: Vector2) -> PackedVector2Array:
    var shutter = not city.block.closed_passages.is_empty()
    if shutter != closed:
        closed = shutter
        cache.clear()
    var key = Vector4(a.x, a.y, b.x, b.y)
    if cache.has(key):
        return cache[key]
    var points = PackedVector2Array([a])
    if city.straight_clear(a, b, .32):
        points.append(b)
    else:
        var path = city.route_between(a, b)
        var first = 0
        while first < path.size():
            var last = first
            while last + 1 < path.size() and city.straight_clear(points[-1], path[last + 1], .31):
                last += 1
            if not points[-1].is_equal_approx(path[last]):
                points.append(path[last])
            first = last + 1
    cache[key] = points
    return points
