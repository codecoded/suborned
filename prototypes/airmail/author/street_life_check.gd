extends SceneTree

var failures = []


func _initialize():
    call_deferred("run")


func check(ok: bool, why: String):
    if not ok:
        failures.append(why)


func check_legs(extras):
    for circuit in extras.Routes.COURT + extras.Routes.AVENUE:
        var stops = circuit[1]
        for i in stops.size():
            var a = extras.point(stops, i)
            var b = extras.point(stops, (i + 1) % stops.size())
            var path = extras.routes.leg(a, b)
            check(path.size() >= 2 and path[-1].distance_to(b) < .01, "Unreachable stop " + str(b))
            for j in range(1, path.size()):
                check(extras.city.straight_clear(path[j - 1], path[j], .25), "Obstructed leg to " + str(b))


func run():
    if not "memory=1" in OS.get_cmdline_user_args():
        push_error("Run with -- memory=1 to isolate the game save.")
        quit(1)
        return
    var city = load("res://street.tscn").instantiate()
    root.add_child(city)
    var extras = city.extras
    var origins = []
    var moved = {}
    var bad_sites = {}
    var jumps = {}
    for e in extras.residents:
        origins.append(e.at)
    check(origins.size() == 60 and extras.batches.size() == 8, "Population or outfit batches changed.")
    var rail_start = city.horizon.cars[0].position
    var cab_start = city.horizon.taxis[0].position
    check_legs(extras)
    for tick in 900:
        if tick == 450:
            city.update_gate(true)
            check_legs(extras)
        city.orders.tick(extras.STEP)
        city.horizon.advance(extras.STEP)
        extras.simulation_step()
        for i in extras.residents.size():
            var e = extras.residents[i]
            if not city.block.clear_at(e.at, .24) or (e.at.y > 7.12 and e.at.y < 19.57):
                bad_sites[i] = e.at
            if e.at.distance_to(e.before) > .18:
                jumps[i] = e.at.distance_to(e.before)
            if e.at.distance_to(origins[i]) > 3.:
                moved[i] = true
    check(moved.size() == 39, "Expected 39 people walking circuits, saw %d." % moved.size())
    check(bad_sites.is_empty(), "People in architecture or the tracks: " + str(bad_sites))
    check(jumps.is_empty(), "People jumped between routes: " + str(jumps))
    check(city.horizon.cars[0].position.distance_to(rail_start) > 18., "Skyrail did not travel.")
    check(city.horizon.taxis[0].position.distance_to(cab_start) > 20., "Sky cab did not travel.")
    var obstructions = {}
    for tick in 300:
        city.horizon.advance(.4)
        for vehicle in city.horizon.cars + city.horizon.taxis:
            var at = vehicle.position
            for tower in city.horizon.PRECINCTS:
                if (
                    at.y <= tower[4] + 3.
                    and Vector2(at.x, at.z).distance_to(Vector2(tower[1], tower[2])) < tower[3] * .5 + 3.
                ):
                    obstructions[str(vehicle.name) + str(tower)] = true
    check(obstructions.is_empty(), "Sky traffic crossed a tower: " + str(obstructions.keys()))
    check(city.orders.people.size() == 3 and city.people.size() == 14, "The district cast changed.")
    for why in failures:
        push_error(why)
    print(
        (
            "Street life: %d residents, %d walkers, %d outfit batches, %d skyline buildings, %d failures."
            % [
                origins.size(),
                moved.size(),
                extras.batches.size(),
                city.horizon.PRECINCTS.size(),
                failures.size()
            ]
        )
    )
    quit(0 if failures.is_empty() else 1)
