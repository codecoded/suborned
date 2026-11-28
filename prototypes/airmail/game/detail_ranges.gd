extends RefCounted

# The street camera uses distance LODs. In an orthographic orbit, distance changes
# even when a module occupies exactly the same number of pixels.
var levels = []
var in_plan = false


func add(node: GeometryInstance3D, level: int, limits: Vector4, plan_level: int):
    node.visibility_range_begin = limits[level]
    node.visibility_range_end = limits[level + 1]
    levels.append([node, limits[level], limits[level + 1], level == plan_level])


func set_plan(enabled: bool):
    if in_plan == enabled:
        return
    in_plan = enabled
    for entry in levels:
        var node: GeometryInstance3D = entry[0]
        node.visibility_range_begin = 0. if enabled else entry[1]
        node.visibility_range_end = 0. if enabled else entry[2]
        node.visible = entry[3] if enabled else true
