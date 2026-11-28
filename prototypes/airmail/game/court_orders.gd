extends RefCounted

const Yard = preload("res://court_scenery.gd")
const GRAPH = preload("res://court_contacts.json")
const CAST = [3, 6, 9]
const PASSERS = [1, 4, 7, 10]
const ENTRIES = [Vector2(-13, -15), Vector2(-8, -20), Vector2(8, -20), Vector2(14, -14)]
const SAVE = "user://parcel-court-v1.json"
var city
var yard
var talk
var people = []
var links = []
var trusted = [true, false, false]
var knows = [false, false, false]
var asked_by = ["", "", ""]
var done = [false, false]
var last_stop = -1
var pending = {}
var chain = []
var heard = 0
var pause = 0.
var moving_paths = {}
var clips = {}
var walkers = []
var save_path: String
var storage = true
var handoffs = []


func setup(street, scenery, dialogue, path: String = SAVE):
    city = street
    yard = scenery
    talk = dialogue
    save_path = path
    storage = not city.run_options.has("memory") and not city.run_options.has("out")
    people = GRAPH.data.people
    var indices = {}
    for i in range(people.size()):
        indices[people[i].key] = i
        links.append([])
        var actor = person(i)
        actor.position = position3(Yard.POSTS[i])
        actor.rotation.y = [-.45, 1.7, -.6][i]
        set_clip(actor, false)
    for edge in GRAPH.data.ties:
        var a = indices[edge.a]
        var b = indices[edge.b]
        links[a].append(b)
        if edge.mutual and not links[b].has(a):
            links[b].append(a)
    for i in range(PASSERS.size()):
        var actor = city.people[PASSERS[i]][0]
        actor.position = position3(ENTRIES[i])
        set_clip(actor, false)
        walkers.append(
            {
                "actor": actor,
                "leg": i,
                "pause": 1. + i * 1.2,
                "target": ENTRIES[i],
                "collecting": false,
                "visits": 0,
                "centre": ENTRIES[i],
                "offset": Vector2.ZERO
            }
        )
    if storage:
        restore()
    if last_stop >= 0:
        person(2).position = position3(stop_at(last_stop))
        person(2).rotation.y = PI * .5
    place_cart()
    yard.set_delivery(done[0], done[1], true)
    city.update_gate(done[0])


func person(index):
    return city.people[CAST[index]][0]


func point(actor) -> Vector2:
    return Vector2(actor.position.x, actor.position.z)


func position3(at: Vector2) -> Vector3:
    return Vector3(at.x, Yard.GROUND, at.y)


func stop_at(work: int) -> Vector2:
    return Yard.PRESS if work == 0 else Yard.MARKET


func first_name(index: int) -> String:
    return people[index].name.get_slice(" ", 0)


func introduction(source: int, target: int) -> Array:
    var queue = [[source]]
    var visited = [source]
    while not queue.is_empty():
        var path = queue.pop_front()
        if path.back() == target:
            return path
        for next in links[path.back()]:
            if not visited.has(next):
                visited.append(next)
                queue.append(path + [next])
    return []


func owns(index: int) -> bool:
    return index in CAST or index in PASSERS


func knows_source(index: int) -> String:
    if knows[index]:
        return "Knows you."
    return ("Heard it from " + asked_by[index] + ".") if not asked_by[index].is_empty() else ""


func help(index: int):
    trusted[index] = true
    persist()


func request(source: int, work: int) -> bool:
    if not pending.is_empty() or not trusted[source] or work == last_stop or work not in [0, 1]:
        return false
    var path = introduction(source, 2)
    if path.is_empty():
        return false
    knows[source] = true
    asked_by[source] = "the cell"
    chain = path
    heard = 0
    pending = {"source": source, "work": work}
    moving_paths.clear()
    pause = .85
    talk.say(
        source,
        [
            "Leave it with me. Angela's at the flower barrow.",
            "I can catch Gustav before his next round.",
            "Give me a moment to turn the trolley."
        ][source]
    )
    persist()
    return true


func set_clip(actor, moving: bool):
    var clip = "idle"
    if moving:
        clip = "push" if actor == person(2) else "walk"
    if clips.get(actor) == clip:
        return
    clips[actor] = clip
    city.animation(actor, clip)


func face(actor, target: Vector2):
    var offset = target - point(actor)
    if offset.length_squared() > .001:
        actor.rotation.y = atan2(offset.x, offset.y)


# Each actor follows the same obstruction-aware pavement grid as the player.
func advance_to(actor, target: Vector2, speed: float, delta: float) -> bool:
    if point(actor).distance_to(target) < .12:
        set_clip(actor, false)
        moving_paths.erase(actor)
        return true
    if not moving_paths.has(actor) or moving_paths[actor].target.distance_to(target) > .15:
        var route = city.route_between(point(actor), target)
        if route.is_empty():
            set_clip(actor, false)
            return false
        moving_paths[actor] = {"target": target, "route": route}
    var job = moving_paths[actor]
    var at = point(actor)
    var path: PackedVector2Array = job.route
    var budget = speed * delta
    while budget > .0001 and not path.is_empty():
        var distance = at.distance_to(path[0])
        var next = at.move_toward(path[0], budget)
        face(actor, next)
        actor.position = position3(next)
        at = next
        budget -= distance
        if distance <= .0001 or at.distance_squared_to(path[0]) < .00001:
            path.remove_at(0)
    job.route = path
    set_clip(actor, not path.is_empty())
    if path.is_empty():
        moving_paths.erase(actor)
    return path.is_empty()


func place_cart():
    var handler = person(2)
    var dir = Vector2(sin(handler.rotation.y), cos(handler.rotation.y))
    yard.cart.position = position3(point(handler) + dir * 1.09)
    yard.cart.rotation.y = handler.rotation.y - PI * .5


func adjacent(to: Vector2, from: Vector2) -> Vector2:
    # Aim for a neighbouring clear grid point, not the space the listener occupies.
    var best = to
    var shortest = INF
    for direction in [Vector2.LEFT, Vector2.RIGHT, Vector2.UP, Vector2.DOWN]:
        var at = to.round() + direction
        if not city.block.clear_at(at, .46) or not city.straight_clear(at, to):
            continue
        var route = city.route_between(from, at)
        if route.is_empty() and from.distance_to(at) > .15:
            continue
        var length = city.route_length(from, route)
        if length < shortest:
            best = at
            shortest = length
    return best


func tick(delta):
    yard.animate(delta)
    if yard.press_open and yard.gate_lift >= .9 and not city.block.closed_passages.is_empty():
        city.update_gate(true)
        moving_paths.clear()
    advance_walkers(delta)
    if pending.is_empty():
        for i in [0, 1]:
            advance_to(person(i), Yard.POSTS[i], 1.55, delta)
        return
    if pause > 0.:
        pause -= delta
        return
    var carrier = chain[heard]
    var actor = person(carrier)
    if heard < chain.size() - 1:
        var listener = chain[heard + 1]
        var other = person(listener)
        var target = (
            moving_paths[actor].target if moving_paths.has(actor) else adjacent(point(other), point(actor))
        )
        if not advance_to(actor, target, 2.05, delta):
            return
        face(actor, point(other))
        face(other, point(actor))
        asked_by[listener] = first_name(carrier)
        handoffs.append([carrier, listener])
        heard += 1
        pause = 1.5
        var line = (
            (
                "Gustav's cases. Can you send him to the %s?"
                if carrier == 0
                else "Gustav, will you take the cases to the %s?"
            )
            % ("press" if pending.work == 0 else "market")
        )
        talk.say(carrier, line)
        persist()
    else:
        var arrived = advance_to(actor, stop_at(pending.work), 1.65, delta)
        place_cart()
        if arrived:
            actor.rotation.y = PI * .5
            place_cart()
            last_stop = pending.work
            done[last_stop] = true
            pending = {}
            yard.set_delivery(done[0], done[1])
            for rec in walkers:
                rec.pause = minf(rec.pause, .8)
            talk.say(
                2,
                (
                    "Press copies are in. You can use the cut now."
                    if last_stop == 0
                    else "Empty cases for returns. Come round this side!"
                )
            )
            persist()


func advance_walkers(delta):
    var round_points = [Vector2(-12, -14), Vector2(-8, -20), Vector2(9, -20), Vector2(13, -15)]
    var occupied = [point(city.guide), point(person(0)), point(person(1)), point(person(2)), point(yard.cart)]
    for i in range(walkers.size()):
        var rec = walkers[i]
        var actor = rec.actor
        actor.position = position3(rec.centre)
        if rec.pause > 0.:
            rec.pause -= delta
        elif advance_to(actor, rec.target, 1.10 + i * .13, delta):
            if rec.collecting:
                rec.visits += 1
                face(actor, Yard.MARKET)
            rec.pause = 4.6 if rec.collecting else .7 + i * .28
            rec.leg = (rec.leg + 1) % 4
            rec.target = round_points[rec.leg]
            rec.collecting = done[1] and i % 2 == 0 and rec.leg in [0, 2]
            if rec.collecting:
                rec.target = Yard.MARKET + Vector2(-1 if i == 0 else 1, 1)
        rec.centre = point(actor)
        var offset = rec.offset.move_toward(Vector2.ZERO, delta * .72)
        for obstacle in occupied:
            var away = rec.centre + offset - obstacle
            if away.length_squared() >= .64 * .64:
                continue
            # Pick the walker’s right at a head-on meeting so they pass instead of swapping sides.
            var right = Vector2(cos(actor.rotation.y), -sin(actor.rotation.y))
            if away.length_squared() < .001:
                away = right
            var side = (away.normalized() + right * .63).normalized()
            var room = (offset + side * (.68 - away.length())).limit_length(.85)
            if city.block.clear_at(rec.centre + room, .26):
                offset = room
        rec.offset = offset
        actor.position = position3(rec.centre + offset)
        occupied.append(point(actor))


func persist():
    if not storage:
        return
    var record = {
        "version": 1,
        "trusted": trusted,
        "knows": knows,
        "asked_by": asked_by,
        "done": done,
        "last_stop": last_stop,
        "pending": pending,
        "heard": heard
    }
    var tmp = save_path + ".tmp"
    var file = FileAccess.open(tmp, FileAccess.WRITE)
    if file == null:
        return
    file.store_string(JSON.stringify(record))
    file.close()
    if DirAccess.rename_absolute(tmp, save_path) != OK:
        push_warning("Could not save Parcel Court.")


func restore():
    if not FileAccess.file_exists(save_path):
        return
    var value = JSON.parse_string(FileAccess.get_file_as_string(save_path))
    if typeof(value) != TYPE_DICTIONARY or value.get("version") != 1:
        return
    for field in ["trusted", "knows", "asked_by", "done"]:
        if typeof(value.get(field)) != TYPE_ARRAY or value[field].size() != get(field).size():
            return
    trusted = value.trusted
    knows = value.knows
    asked_by = value.asked_by
    done = value.done
    last_stop = clampi(int(value.get("last_stop", -1)), -1, 1)
    var job = value.get("pending", {})
    if typeof(job) != TYPE_DICTIONARY:
        return
    var sender = int(job.get("source", -1))
    var destination = int(job.get("work", -1))
    if sender in [0, 1, 2] and destination in [0, 1]:
        chain = introduction(sender, 2)
        if not chain.is_empty():
            pending = {"source": sender, "work": destination}
            heard = clampi(int(value.get("heard", 0)), 0, chain.size() - 1)
            pause = .5
