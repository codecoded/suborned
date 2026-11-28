extends SceneTree

var street
var orders
var problems = []
var save_file = "user://court-test-" + str(OS.get_process_id()) + ".json"


func _initialize():
    call_deferred("exercise")


func check(condition, message):
    if not condition:
        problems.append(message)


func key(code):
    var event = InputEventKey.new()
    event.pressed = true
    event.keycode = code
    root.push_input(event, true)


func click_resident(index):
    var event = InputEventMouseButton.new()
    event.button_index = MOUSE_BUTTON_LEFT
    event.pressed = true
    event.position = street.camera.unproject_position(orders.person(index).position + Vector3.UP)
    root.push_input(event, true)


func advance(seconds):
    for i in range(ceili(seconds * 30.)):
        street._process(1. / 30.)


func gap():
    var start = Vector2(1, -21)
    var route = street.route_between(start, Vector2(6, -21))
    return street.route_length(start, route) if not route.is_empty() else INF


func exercise():
    if not "memory=1" in OS.get_cmdline_user_args():
        push_error("Run the test with -- memory=1 to isolate the game save.")
        quit(1)
        return
    street = load("res://street.tscn").instantiate()
    root.add_child(street)
    await process_frame
    orders = street.orders
    var yard = street.court
    check(street.dialogue.nearest() == 0, "The opening contact is out of reach.")
    check(not street.block.clear_at(Vector2(3, -21)), "The closed shutter allows pedestrians through.")
    var long_route = gap()
    check(is_finite(long_route), "The press has no alternative entrance.")
    for point in [Vector2(12, 4), Vector2(-19, -14), yard.PRESS, yard.MARKET]:
        check(not street.route_between(yard.START, point).is_empty(), "Missing route to " + str(point))
    check(
        orders.introduction(0, 2) == [0, 1, 2] and orders.introduction(2, 0).is_empty(),
        "Directed district relationships changed."
    )
    key(KEY_E)
    check(street.dialogue.speaker == 0, "E did not start a conversation with Jamie.")
    key(KEY_1)
    check(
        orders.pending.get("source") == 0, "The press order did not start through the regular input handler."
    )
    check(not orders.request(0, 1), "A second order interrupted a working delivery.")
    advance(5.)
    check(orders.heard >= 1, "Jamie did not physically reach Angela.")
    orders.storage = true
    orders.save_path = save_file
    orders.persist()
    var mid = JSON.parse_string(FileAccess.get_file_as_string(save_file))
    check(mid != null and mid.pending.get("work") == 0, "The request did not persist mid-round.")
    # A second launch resumes the last acknowledged introduction, with its own scene and route.
    street.free()
    street = load("res://street.tscn").instantiate()
    root.add_child(street)
    orders = street.orders
    orders.save_path = save_file
    orders.storage = true
    orders.restore()
    check(orders.heard == int(mid.heard), "A reload lost an acknowledged introduction.")
    advance(43.)
    check(
        orders.done == [true, false] and orders.pending.is_empty(),
        "The relayed press delivery never arrived."
    )
    check(
        street.block.clear_at(Vector2(3, -21)) and not street.grid.is_point_solid(Vector2i(3, -21)),
        "The open shutter still blocks a route."
    )
    var short_route = gap()
    check(short_route + 6. < long_route, "Opening the press did not materially shorten the route.")
    check(
        orders.knows == [true, false, false] and orders.asked_by == ["the cell", "Jamie", "Angela"],
        "A downstream contact incorrectly learned the source."
    )
    check(
        orders.point(orders.person(2)).distance_to(street.court.PRESS) < .13,
        "The trolley handler missed the press."
    )
    var old_cart = street.court.cart.position
    key(KEY_TAB)
    street.update_camera()
    click_resident(2)
    check(
        street.dialogue.approach == 2 and not street.waypoints.is_empty(),
        "Clicking a distant resident did not plan a visit."
    )
    advance(26.)
    check(street.dialogue.speaker == 2, "The distant click did not bring the player into conversation.")
    key(KEY_1)
    check(orders.trusted[2], "Lending a hand did not earn Gustav's trust.")
    key(KEY_1)
    advance(20.)
    check(
        orders.done == [true, true] and orders.last_stop == 1,
        "The market delivery failed after opening the press."
    )
    check(orders.knows == [true, false, true], "Gustav did not remember the direct approach.")
    check(old_cart.distance_to(street.court.cart.position) > 4., "The trolley stayed behind.")
    check(street.court.market_lift > .99, "The returns blind did not open.")
    advance(90.)
    check(
        orders.walkers[0].visits > 0 and orders.walkers[2].visits > 0 and orders.walkers[1].visits == 0,
        "Opening returns failed to draw both designated shoppers to the collection points."
    )
    check(orders.request(0, 0), "The press round could not be requested again.")
    advance(45.)
    check(
        orders.done == [true, true] and orders.last_stop == 0,
        "A repeat round removed an earlier consequence."
    )
    var saved = JSON.parse_string(FileAccess.get_file_as_string(save_file))
    check(
        saved.done == [true, true] and saved.knows == [true, false, true] and saved.pending.is_empty(),
        "The final state did not survive saving."
    )
    street.free()
    # A new player may choose returns first and can leave the original street fully on foot.
    street = load("res://street.tscn").instantiate()
    root.add_child(street)
    orders = street.orders
    key(KEY_E)
    key(KEY_2)
    advance(48.)
    check(
        orders.done == [false, true] and gap() > short_route + 6.,
        "Returns-first unexpectedly opened the press."
    )
    check(street.walk_to(Vector2(13, 4)), "The main tram avenue is inaccessible.")
    advance(46.)
    check(
        orders.point(street.guide).distance_to(Vector2(13, 4)) < .15,
        "The court exits do not reconnect with the avenue."
    )
    DirAccess.remove_absolute(save_file)
    street.free()
    if not problems.is_empty():
        for msg in problems:
            push_error(msg)
        quit(1)
    else:
        print(
            (
                "Parcel Court passed both delivery orders, reload during relay, directed knowledge, physical visits, shopping, walk-out, and shortcut %.1f m to %.1f m."
                % [long_route, short_route]
            )
        )
        quit()
