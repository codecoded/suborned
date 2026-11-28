extends Node3D

const STEPS = [
    preload("res://assets/yard_foot_concrete_1.wav"), preload("res://assets/yard_foot_concrete_2.wav")
]
var street
var heel = AudioStreamPlayer.new()
var shutter = AudioStreamPlayer3D.new()
var bell = AudioStreamPlayer3D.new()
var bed = AudioStreamPlayer.new()
var previous: Vector2
var distance = 0.
var foot = 0
var press = false
var market = false


func setup(host):
    street = host
    previous = street.orders.point(street.guide)
    press = street.court.press_open
    market = street.court.market_open
    add_child(heel)
    heel.volume_db = -12.
    add_child(shutter)
    shutter.position = Vector3(3, 2, -21)
    shutter.stream = preload("res://assets/yard_cafe_door.wav")
    shutter.pitch_scale = .66
    shutter.max_distance = 29.
    add_child(bell)
    bell.position = Vector3(12, 2, -14)
    bell.stream = preload("res://assets/yard_cafe_bell.wav")
    bell.max_distance = 21.
    add_child(bed)
    bed.stream = preload("res://assets/yard_ambience.wav")
    bed.volume_db = -13.
    bed.play()


func _exit_tree():
    silence()


func silence():
    for player in [bed, heel, shutter, bell]:
        player.stop()
        player.stream = null


func tick():
    var current = street.orders.point(street.guide)
    distance += minf(current.distance_to(previous), .35)
    previous = current
    if distance >= .91:
        distance = 0.
        heel.stream = STEPS[foot]
        foot = 1 - foot
        heel.volume_db = -19. if street.iso else -12.
        heel.play()
    if not press and street.court.press_open:
        press = true
        shutter.play()
    if not market and street.court.market_open:
        market = true
        bell.play()
