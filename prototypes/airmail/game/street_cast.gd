extends RefCounted

const PEOPLE = [preload("res://assets/c1.glb"), preload("res://assets/c6.glb")]
const COURT_PEOPLE = [
    preload("res://assets/cafe_w3.glb"),
    preload("res://assets/sh2.glb"),
    preload("res://assets/workers_d5.glb")
]
const Deliveries = preload("res://court_orders.gd")


static func populate(city):
    for i in range(14):
        var contact = Deliveries.CAST.find(i) if city.court else -1
        var model = COURT_PEOPLE[contact] if contact >= 0 else PEOPLE[i % 2]
        var actor = model.instantiate()
        city.add_child(actor)
        var moving = i % 3 != 0
        var player = city.animation(actor, "walk" if moving else "idle")
        if player:
            player.seek(fposmod(i * .31, maxf(.1, player.current_animation_length)), true)
        var x = -38. + i * 6.5
        var z = [3.74, 4.09, 22.9, 1.95][i % 4]
        if i == 0:
            x = 6.
            z = -2.05
        if i == 9:
            x = -7.
            z = -1.25
        city.people.append([actor, Vector3(x, .16, z), moving, i * .49])


static func advance(city):
    for i in range(city.people.size()):
        if city.orders and city.orders.owns(i):
            continue
        var rec = city.people[i]
        var actor = rec[0]
        var p: Vector3 = rec[1]
        var a = city.elapsed * .17 + rec[3]
        if rec[2]:
            p.x += sin(a) * 5.7
            actor.rotation.y = PI * .5 if cos(a) > 0 else -PI * .5
        else:
            actor.rotation.y = PI + rec[3] * .64
        actor.position = p
