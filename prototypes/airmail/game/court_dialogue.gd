extends RefCounted

const Yard = preload("res://court_scenery.gd")
var city
var orders
var speaker = -1
var approach = -1
var tags = []
var root = VBoxContainer.new()
var bottom = MarginContainer.new()
var line = Label.new()
var choices = VBoxContainer.new()
var callback = []
var said = ""
var voice = -1
var left = 0.


func setup(street, delivery):
    city = street
    orders = delivery
    city.hud.add_child(bottom)
    bottom.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
    bottom.offset_left = 24
    bottom.offset_right = -24
    bottom.offset_bottom = -24
    bottom.grow_vertical = Control.GROW_DIRECTION_BEGIN
    bottom.mouse_filter = Control.MOUSE_FILTER_IGNORE
    bottom.add_child(root)
    root.mouse_filter = Control.MOUSE_FILTER_IGNORE
    root.add_theme_constant_override("separation", 4)
    root.add_child(line)
    line.add_theme_font_size_override("font_size", 19)
    line.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    line.add_theme_constant_override("outline_size", 5)
    line.add_theme_color_override("font_outline_color", Color(.07, .09, .085, .84))
    root.add_child(choices)
    choices.mouse_filter = Control.MOUSE_FILTER_IGNORE
    for i in range(3):
        var tag = Label3D.new()
        tag.text = orders.first_name(i)
        tag.billboard = BaseMaterial3D.BILLBOARD_ENABLED
        tag.font_size = 32
        tag.outline_size = 7
        tag.pixel_size = .0045
        tag.modulate = Color("#f6e6bc")
        tag.outline_modulate = Color("#26352e")
        orders.person(i).add_child(tag)
        tag.position = Vector3.UP * 2.04
        tags.append(tag)
    say(0, "Press Cut's locked again. Gustav has their first edition.")


func say(index: int, words: String):
    voice = index
    said = words
    left = 4.5


func position2(index):
    return orders.point(orders.person(index))


func nearest() -> int:
    var pos = orders.point(city.guide)
    var best = -1
    var distance = 2.9
    for i in range(3):
        var other = position2(i)
        var m = pos.distance_to(other)
        if m < distance and city.straight_clear(pos, other):
            best = i
            distance = m
    return best


func pick(mouse: Vector2) -> int:
    var best = -1
    var error = INF
    var eye = Vector2(city.camera.position.x, city.camera.position.z)
    for i in range(3):
        var actor = orders.person(i)
        var middle = actor.position + Vector3.UP * .95
        if city.camera.is_position_behind(middle):
            continue
        if not city.iso and not city.straight_clear(eye, position2(i)):
            continue
        var top = city.camera.unproject_position(actor.position + Vector3.UP * 1.92)
        var foot = city.camera.unproject_position(actor.position)
        var height = maxf(10., top.distance_to(foot))
        var nearest_pixel = Geometry2D.get_closest_point_to_segment(mouse, top, foot)
        var d = mouse.distance_to(nearest_pixel)
        if d < clampf(height * .19, 9, 36) and d < error:
            best = i
            error = d
    return best


func visit(index):
    close()
    var from = orders.point(city.guide)
    if from.distance_to(position2(index)) <= 2.9 and city.straight_clear(from, position2(index)):
        open(index)
    else:
        var at = orders.adjacent(position2(index), from)
        if city.walk_to(at):
            approach = index


func open(index):
    approach = -1
    speaker = index
    city.waypoints.clear()
    orders.face(city.guide, position2(index))
    clear_choices()
    var prefix = orders.first_name(index) + "  ·  "
    if not orders.pending.is_empty():
        var known = orders.knows_source(index)
        var source = orders.first_name(orders.chain[orders.heard])
        line.text = (
            prefix
            + (
                "The trolley is on its way."
                if index == 2 and orders.chain[orders.heard] == 2
                else "The word is with " + source + "."
            )
        )
        if not known.is_empty():
            line.text += "  " + known
    elif not orders.trusted[index]:
        var referred = orders.asked_by[index]
        var recollection = (
            ("It was " + referred + " who asked me.  ")
            if not referred.is_empty() and referred != "the cell"
            else ""
        )
        line.text = (
            prefix
            + recollection
            + (
                "Hold the stems while I tie this bunch?"
                if index == 1
                else "Hold the strap while I pin this load?"
            )
        )
        option(
            "Lend a hand",
            func():
                orders.help(index)
                open(index)
        )
    else:
        var opening = [
            "I can get word to Gustav through Angela.",
            "I know Gustav. Where do you need his round?",
            "Which way do you want this load?"
        ][index]
        line.text = prefix + opening
        if orders.knows[index]:
            line.text += "  I remember you."
        elif index == 0:
            line.text += "  She'll hear it from me."
        elif index == 1:
            line.text += "  He'll hear it from me."
        else:
            line.text += "  I'll remember who asked."
        for work in range(2):
            if orders.last_stop == work:
                continue
            var choice = ["Take the press copies in. Open the cut.", "Take the empties to Fruit Returns."][work]
            option(choice, send.bind(index, work))
    option("Leave", close)


func option(words: String, action: Callable):
    var button = Button.new()
    button.text = str(callback.size() + 1) + "    " + words
    button.flat = true
    button.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
    button.alignment = HORIZONTAL_ALIGNMENT_LEFT
    button.focus_mode = Control.FOCUS_NONE
    button.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
    button.add_theme_font_size_override("font_size", 16)
    button.add_theme_color_override("font_color", Color("#f7e6be"))
    button.add_theme_color_override("font_hover_color", Color.WHITE)
    button.add_theme_color_override("font_outline_color", Color("#232b28"))
    button.add_theme_constant_override("outline_size", 5)
    choices.add_child(button)
    callback.append(action)
    button.pressed.connect(action)


func send(index, work):
    if orders.request(index, work):
        close()


func choose(number: int):
    if number >= 0 and number < callback.size():
        var action: Callable = callback[number]
        action.call()


func clear_choices():
    callback.clear()
    for child in choices.get_children():
        choices.remove_child(child)
        child.queue_free()


func close():
    approach = -1
    speaker = -1
    clear_choices()
    line.text = ""


func tick(delta):
    var pos = orders.point(city.guide)
    var near = nearest()
    var pointed = speaker if speaker >= 0 else approach
    if city.iso and city.hud.visible and pointed < 0:
        pointed = pick(city.get_viewport().get_mouse_position())
    for i in range(3):
        if city.iso:
            tags[i].visible = city.hud.visible and i == pointed
            continue
        var focus = (
            (speaker < 0 or speaker == i)
            and (near < 0 or near == i or (not orders.pending.is_empty() and i == orders.chain[orders.heard]))
        )
        tags[i].visible = (
            city.hud.visible
            and focus
            and pos.distance_to(position2(i)) < 7.
            and city.straight_clear(pos, position2(i))
        )
    if approach >= 0:
        if pos.distance_to(position2(approach)) <= 2.7 and city.straight_clear(pos, position2(approach)):
            open(approach)
        elif city.waypoints.is_empty():
            visit(approach)
    if speaker >= 0:
        if pos.distance_to(position2(speaker)) > 3.8:
            close()
        return
    left -= delta
    var audible = left > 0. and voice >= 0 and pos.distance_to(position2(voice)) < 12.
    var text = orders.first_name(voice) + "  ·  " + said if audible else ""
    if near >= 0:
        if audible:
            text += "\n"
        text += "E   Talk to " + orders.first_name(near)
    if line.text != text:
        line.text = text
