"""Brief 087 (Chime): the first pack's table: which take makes which cue, how it is cut, what level it is mastered to, and how it plays. build.py reads it. Times in seconds in the take.
level = the clip's momentary maximum (LUFS), loops: integrated. The mix is set by these levels; volumes in play stay 1.0 and only the buses and Options move it."""
T = 'paid/087-sound/takes/'
K = 'out/scratch/chime/kenney/'
SEED = {'source': 'Generated: Seed Audio 1.0 (bytedance:seed-audio@1.0) via Runware', 'licence': 'runware-terms-partner-model'}
KENNEY = {'source': 'Kenney', 'licence': 'CC0-1.0'}
OFF = 'view only, 2D'


def c(id, label, group, src, cut=None, level=-24, trigger='', spatial='2d', voices=1, cooldown=0.0, loop=False, hp=None, lp=None, fade=0.0, fin=0.0, kind='seed', pitch=0, edit=None, rate=48000, stereo=False, xf=0.0, load='decompress', note='', priority=128, onlog=None, take=None, optional=False, drive=0.0, layers=None, even=False):
    return dict(id=id, label=label, group=group, src=src, cut=cut, level=level, trigger=trigger, spatial=spatial, voices=voices, cooldown=cooldown, loop=loop, hp=hp, lp=lp, fade=fade, fin=fin, kind=kind, pitch=pitch, edit=edit, rate=rate, stereo=stereo, xf=xf, load=load, note=note, priority=priority, onlog=onlog, optional=optional, drive=drive, layers=layers, even=even)


CUES = [
    # ---- timing: never ducked but by a conversation, heard wherever the camera is
    c('cafe_bell', 'Cafe bell', 'clock', 'cafe_bell_t22.wav', (0.08, 1.7), -26, 'The guard reaches the cafe: the door is open for you.', hp=400, fade=0.5, voices=1, cooldown=2, onlog=['guard.at-cafe'], priority=40),
    c('cafe_door', 'Cafe door', 'clock', 'cafe_door_t21.wav', None, -28, 'The guard leaves the cafe: the window is closing.', hp=120, edit=[(0.42, 2.3), (5.45, 6.3)], fade=0.2, cooldown=2, onlog=['guard.leaves-cafe'], priority=40),
    c('handover_chime', 'Handover chime, four thirty', 'clock', 'handover_chime_t23.wav', (0.05, 5.95), -20, 'The Office shift ends: Site.Chime, 4:30 on the Handover.', hp=180, fade=1.2, load='memory', priority=20, cooldown=30),
    # ---- danger: a rising family, high and dry so it sits over the bed
    c('notice_1', 'Watcher notices, stage one', 'danger', 'notice_ting_t21.wav', (0.38, 0.85), -26, 'A watcher notices an agent.', hp=1500, fade=0.15, cooldown=0.5, onlog=['attention.noticed'], priority=30),
    c('notice_2', 'Watcher wary, stage two', 'danger', 'notice_ting_t21.wav', (0.38, 0.85), -22, 'That watcher is wary of him for the rest of the mission.', hp=1500, fade=0.15, pitch=3, edit='double', cooldown=0.5, onlog=['attention.wary'], priority=30),
    c('notice_3', 'Watcher reports, stage three', 'danger', 'phone_bell_t21.wav', (0.08, 1.35), -19, 'That watcher calls it in: a report is on its way.', hp=500, fade=0.35, cooldown=0.5, onlog=['attention.reporting'], priority=25),
    c('camera_click', 'Surveillance click', 'danger', K + 'rpg-audio/x/Audio/metalLatch.ogg', None, -30, "The patrol's sweep turns; a live camera's scan comes round. Only for a watcher whose cone the player can see.", kind='kenney', hp=800, fade=0.04, spatial='3d-near', cooldown=1, voices=2, priority=120),
    c('patrol_loop', 'Patrol on its route', 'danger', 'patrol_loop_t22.wav', (1.0, 14.5), -33, 'While the patrol walks its sweep.', hp=50, loop=True, spatial='3d', xf=0.05, load='memory', priority=100, note='cut to whole steps'),
    c('patrol_chase', 'Patrol off its route', 'danger', 'patrol_break_t22.wav', (1.0, 14.5), -27, 'While the patrol has broken route: it chases, or answers an alarm.', hp=50, loop=True, spatial='3d', xf=0.05, load='memory', priority=80, note='cut to whole steps'),
    c('patrol_break', 'Patrol breaks route', 'danger', 'patrol_sting_t22.wav', (0.3, 1.6), -22, 'The tick the patrol breaks route.', hp=120, fade=0.3, spatial='3d-far', cooldown=3, onlog=['patrol.breaks', 'patrol.alarm'], priority=30),
    c('police_siren', 'Police, from their direction', 'danger', 'police_siren_t21.wav', (0.1, 6.9), -21, 'The desk sends the response: panned to where they enter.', hp=300, fin=1.2, fade=1.6, spatial='2d-pan', cooldown=20, load='memory', onlog=['alarm.dispatches'], priority=35),
    # ---- fight: one voice per weapon
    c('fire_pistol', 'Cell pistol: Scout, Fixer, Tech', 'fight', 'fire_pistol_t21.wav', (0.17, 1.5), -18, 'A shot by the Scout, Fixer or Tech.', hp=120, fade=0.7, spatial='3d-fight', voices=3, priority=60),
    c('fire_carbine', 'Enforcer carbine', 'fight', 'fire_carbine_t21.wav', (0.31, 2.2), -15, 'A shot by the Enforcer.', hp=50, fade=0.9, drive=8, spatial='3d-fight', voices=2, priority=55),
    c('fire_police', 'Police and guard sidearm', 'fight', 'fire_police_t22.wav', (0.08, 1.05), -19, 'A shot by a guard, an officer or an armed civilian.', hp=90, lp=9000, fade=0.5, spatial='3d-fight', voices=3, priority=65),
    c('fire_rival', 'Rival handgun', 'fight', 'fire_rival_t22.wav', (0.08, 0.95), -17, 'A shot by a rival.', hp=250, fade=0.45, drive=8, spatial='3d-fight', voices=3, priority=65),
    c('fire_robot', 'Robot bolt gun', 'fight', 'fire_robot_t22.wav', None, -18, 'A shot by the patrol or a response robot.', hp=70, edit=[(0.155, 0.55), (1.82, 2.25)], fade=0.15, spatial='3d-fight', voices=2, priority=65),
    c('hit_body', 'Hit on a person', 'fight', 'hit_body_t22.wav', (0.1, 0.65), -21, 'A shot lands on a person.', hp=80, fade=0.25, spatial='3d-fight', voices=3, onlog=['shot.hit'], priority=70),
    c('hit_metal', 'Hit on a robot or a camera', 'fight', 'hit_metal_t22.wav', (0.1, 0.85), -21, 'A shot lands on a robot, a camera or a car.', hp=200, fade=0.4, spatial='3d-fight', voices=2, priority=70),
    c('miss', 'Miss, on the stone', 'fight', 'miss_t22.wav', (0.08, 1.5), -26, 'A shot misses: on the puff.', hp=250, fade=0.4, spatial='3d-fight', voices=3, onlog=['shot.miss'], priority=90),
    c('down_body', 'A person goes down', 'fight', 'down_body_t22.wav', (0.32, 1.6), -20, 'Anyone goes down, the cell or a hostile.', hp=60, fade=0.5, spatial='3d-fight', voices=2, priority=45),
    c('down_robot', 'A robot goes down', 'fight', 'down_robot_t21.wav', (0.06, 2.6), -19, 'A robot goes down.', hp=60, fade=0.9, spatial='3d-fight', voices=1, priority=45),
    # ---- conversation
    c('cut_in', 'Conversation, cut in', 'convo', 'cut_soft_t22.wav', (0.84, 1.3), -32, 'The cut to a conversation: on the dip.', hp=50, fade=0.2, priority=60),
    c('cut_out', 'Conversation, cut out', 'convo', 'cut_soft_t22.wav', (0.08, 0.42), -34, 'The cut back to the street: on the dip.', hp=400, fin=0.01, fade=0.15, priority=60),
    c('room_tone', 'Room tone under the cutaway', 'convo', 'room_tone_t21.wav', (0.3, 16.2), -46, 'Under the whole cutaway.', hp=40, loop=True, xf=1.5, rate=32000, load='stream', priority=20),
    c('card_new', 'New card chime', 'convo', 'card_new_t22.wav', (0.08, 1.7), -28, 'A card with New arrives on the person.', hp=300, pitch=7, fade=0.6, cooldown=0.5, priority=50),
    # ---- city
    c('street_bed', 'Street ambience', 'ambience', ['street_bed_t21.wav', 'street_bed_t23.wav'], None, -38, 'All through play: the warm floor under everything.', hp=45, loop=True, xf=3.0, rate=32000, stereo=True, load='stream', priority=10),
    c('foot_concrete_1', 'Footstep one (optional)', 'ambience', K + 'impact-sounds/x/Audio/footstep_concrete_000.ogg', None, -37, 'Optional: a selected agent walking at play zoom.', kind='kenney', hp=90, spatial='3d-near', voices=2, priority=200, optional=True),
    c('foot_concrete_2', 'Footstep two (optional)', 'ambience', K + 'impact-sounds/x/Audio/footstep_concrete_001.ogg', None, -37, 'Optional: the other foot.', kind='kenney', hp=90, spatial='3d-near', voices=2, priority=200, optional=True),
    # ---- cars (Sky: "What about car explosion and on fire?"): heavy 1950s metal with an atomic-age engine
    c('car_disabled', 'Car disabled, its engine dies', 'fight', 'car_disabled_t32.wav', (0.05, 3.6), -19, 'A car takes its fifth damage: it stops for good.', hp=120, fin=0.02, fade=0.5, spatial='3d-far', cooldown=0.5, voices=2, onlog=['car.disabled'], load='memory', priority=40),
    c('car_ignite', 'Car catches fire', 'fight', 'car_ignite_t32.wav', (0.08, 2.3), -18, 'The car reaches 0: it burns, and blows in 6 s.', hp=70, fin=0.003, fade=0.8, spatial='3d-far', voices=2, onlog=['car.burning'], load='memory', priority=30),
    c('car_burn', 'Car burning, the countdown', 'danger', 'car_burn_t31.wav', (0.3, 14.7), -26, 'From car.burning until it blows. The host raises its pitch and level over the burn (wiring.md), so it can be read as a countdown.', hp=70, loop=True, xf=1.0, spatial='3d', voices=2, load='memory', priority=25, layers=[('car_burn_t33.wav', 0.3, 14.7, 0.0, -2.0, 900)]),
    c('car_warn', 'Car about to blow: tick', 'danger', 'car_warn_t31.wav', (0.17, 0.5), -20, 'At 3, 2, 1 and 0.5 s before the blast, rising by pitch.', hp=350, fade=0.12, spatial='3d-far', voices=2, load='decompress', priority=25),
    c('car_explode', 'Car explodes', 'blast', 'car_explode_t33.wav', (0.2, 4.4), -11, 'The blast: 3 damage within 5 m, the cell included.', hp=35, fin=0.002, fade=1.0, drive=7, spatial='3d-blast', voices=2, onlog=['car.exploded'], load='memory', priority=5, layers=[('car_explode_t32.wav', 2.0, 4.6, 0.9, -7.0, 500)]),
    c('car_burnout', 'Wreck, the fire dying', 'fight', 'car_burnout_t32.wav', (0.3, 8.0), -29, 'After the blast: the fire dies back and the metal ticks as it cools.', hp=70, fin=0.25, fade=3.0, spatial='3d', voices=2, load='memory', rate=32000, priority=120),
    # ---- a level-matched bed, for Sky's call: see wiring.md. street_bed swings 9.5 LU, because its two takes were not matched
    c('street_bed_even', 'Street ambience, evened', 'ambience', ['street_bed_t22.wav', 'street_bed_t21.wav'], None, -38, 'Would replace street_bed, if Sky keeps it: same place in the mix, no swing.', hp=45, loop=True, xf=3.0, rate=32000, stereo=True, load='stream', priority=10, even=True, optional=True),
]

# Who holds what: bus → Options slider. Master is applied today; the rest must be wired (wiring.md).
BUSES = {
    'master': {'db': 0, 'options': 'Master'},
    'clock': {'parent': 'master', 'db': 0, 'options': 'Effects', 'duck_convo_db': 0},
    'danger': {'parent': 'master', 'db': 0, 'options': 'Effects', 'duck_convo_db': -18},
    'fight': {'parent': 'master', 'db': -3, 'options': 'Effects', 'duck_convo_db': -18},
    'blast': {'parent': 'master', 'db': -2, 'options': 'Effects', 'duck_convo_db': -18},
    'convo': {'parent': 'master', 'db': 0, 'options': 'Effects', 'duck_convo_db': 0},
    'ambience': {'parent': 'master', 'db': 0, 'options': 'Ambience (new)', 'duck_convo_db': -14},
    'vehicles': {'parent': 'master', 'db': -3, 'options': 'Effects', 'duck_convo_db': -18},
}
SPATIAL = {
    '2d': {'blend': 0},
    '2d-pan': {'blend': 0, 'pan': 'clamp(dot(cameraRight, entry - focus) / 40 m, -0.8, 0.8), recomputed each frame while it rings'},
    '3d': {'blend': 1, 'rolloff': 'logarithmic', 'minDistanceM': 12, 'maxDistanceM': 90},
    '3d-near': {'blend': 1, 'rolloff': 'logarithmic', 'minDistanceM': 8, 'maxDistanceM': 45},
    '3d-far': {'blend': 0.7, 'rolloff': 'logarithmic', 'minDistanceM': 20, 'maxDistanceM': 150},
    '3d-blast': {'blend': 0.5, 'rolloff': 'logarithmic', 'minDistanceM': 40, 'maxDistanceM': 220},
    '3d-fight': {'blend': 0.7, 'rolloff': 'logarithmic', 'minDistanceM': 18, 'maxDistanceM': 140},
}
