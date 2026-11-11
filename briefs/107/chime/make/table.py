"""Brief 107 (Chime): the weather and night cues: which takes, how each is cut and mastered, which I pick and why. build.py reads it.
Levels sit against the 087 pack (briefs/087/cues.json): street_bed -38 LUFS integrated, foot_concrete -37 LUFS momentary max, patrol_loop -33 integrated.
Rain is light background (Sky, 2026-11-11: "rain needs to be light background noise"): with the street bed unchanged, the shower, awnings and drips together add about half a LU to it.
meter 'i' = integrated (beds and steady loops), 'm' = momentary maximum (sparse sounds and one-shots). Times in seconds in the take."""
T = 'paid/107-weather-sound/takes/'


def c(id, label, kind, takes, level, meter, plays, spatial, pick, why, *, crest=None, voices=1, cooldown=0.0, load='decompress', when=None, place='', role='', hp=45, xf=0.0, stereo=False, rate=48000, ride=False, cut=None, join=None, steps=4, min_gap=0.3, optional=False):
    return dict(crest=crest, voices=voices, cooldown=cooldown, load=load, when=when or {}, place=place, role=role, id=id, label=label, kind=kind, takes=takes, level=level, meter=meter, plays=plays, spatial=spatial, pick=pick, why=why, hp=hp, xf=xf, stereo=stereo, rate=rate, ride=ride,
                cut=cut or {}, join=join, steps=steps, min_gap=min_gap, optional=optional)


CUES = [
    c('rain_bed', 'Summer shower', 'loop', ['rain_bed_t11', 'rain_bed_t12', 'rain_bed_t13'], -48, 'i',
      'While it rains: over the street bed (or the night bed), not instead of it. 2D stereo, ambience bus. 10 dB under the street bed: light background, not a feature.', '2d',
      'rain_bed_t11', 'The evenest take (0.8 LU swing over 4 s windows) with no gusts or rumbles once the lows are cut; take 2 has a low rumble at 17-18 s that could read as thunder; take 3 is thinner and swings 3.4 LU.',
      load='stream', when={'weather': ['rain']}, role='layer over the bed', place='everywhere: the listener', hp=120, xf=3.0, stereo=True, rate=32000, cut={'rain_bed_t11': (0.15, 19.85), 'rain_bed_t12': (0.15, 21.35), 'rain_bed_t13': (0.15, 19.85)}),
    c('rain_awning', 'Rain on awnings and parasols', 'loop', ['rain_awning_t11', 'rain_awning_t12', 'rain_awning_t13'], -48, 'i',
      'While it rains: one 3D loop on each cafe awning or parasol cluster, start offset by hash so neighbours differ. At the shower\'s level, so it is noticed only close up.', '3d-near',
      'rain_awning_t13', 'Even (1.2 LU swing) with no events and the fewest tonal lines; take 1 has a low thump at 8.5 s; take 2 is the same texture but came out 13 dB quieter, so it carries more noise once raised. Every take has a few loud drops off the edge (12-15 dB over the patter); their peaks are held to -12 dBTP so none pops (24 dB over the loop\'s loudness).',
      crest=24.0, voices=4, load='memory', when={'weather': ['rain']}, role='3D loop per awning or parasol cluster', place='awnings and parasol clusters: the four nearest the camera focus', hp=100, xf=1.5, rate=48000, cut={'rain_awning_t11': (0.15, 21.35), 'rain_awning_t12': (0.15, 19.85), 'rain_awning_t13': (0.15, 21.35)}),
    c('drips', 'Drips from the eaves', 'loop', ['drips_t11', 'drips_t12', 'drips_t13'], -46, 'm',
      'While it rains: a quiet 3D loop under eaves and awning edges, a few per block. Its loudest drip sits 9 dB under a footstep, so it is noticed only close up.', '3d-near',
      'drips_t11', 'Separate bright drips all the way through (about one a second) at an even level (1.8 LU swing); take 2 has heavier splats and swings 5.8 LU; take 3 is near silence with few clear drops.',
      voices=4, load='memory', when={'weather': ['rain']}, role='3D loop per eave', place='eaves and awning edges, a few per block: the four nearest the camera focus', hp=200, xf=0.6, rate=48000, cut={'drips_t11': (0.15, 14.85), 'drips_t12': (0.15, 14.85), 'drips_t13': (0.1, 12.8)}),
    c('wet_steps', 'Wet footsteps', 'steps', ['wet_steps_t11', 'wet_steps_t12', 'wet_steps_t13'], -37, 'm',
      'In rain and after: four single steps per set, played as foot_concrete would be (3D-near, alternating, pitch from the hash). Same level as foot_concrete.', '3d-near',
      None, 'Set by the build from how cleanly the steps stand over the noise between them (see the table).',
      voices=2, when={'weather': ['rain']}, role='footsteps while the ground is wet', place='at the walker', optional=True, hp=120, min_gap=0.3),
    c('night_bed', 'Night street', 'loop', ['night_bed_t12', 'night_bed_t22', 'night_bed_t11', 'night_bed_t13', 'night_bed_t21'], -40, 'i',
      'At night: in place of the street bed. 2D stereo, ambience bus. 2 dB under the day bed, so the night is calmer and the cues stand clearer.', '2d',
      'night_bed_join', 'The steady parts of takes 1-2 and 2-2 joined and evened into a 27 s loop. Take 1-1 has short tones that change pitch (could be a tune or a whine); 1-3 swings 9 LU; 2-1 and 2-2 have a tram swelling in partway, which would repeat on every loop.',
      load='stream', when={'light': ['night']}, role='replaces street_bed', place='everywhere: the listener', hp=45, xf=3.0, stereo=True, rate=32000, ride=True,
      cut={'night_bed_t12': (0.2, 19.8), 'night_bed_t22': (0.2, 10.5), 'night_bed_t11': (0.2, 19.8), 'night_bed_t13': (0.2, 19.8), 'night_bed_t21': (0.2, 19.8)},
      join=['night_bed_t12', 'night_bed_t22']),
    c('night_tram', 'Far tram at night (optional)', 'oneshot', ['night_bed_t21'], -40, 'i',
      'Optional: now and then at night (every 40-90 s by hash), a tram gliding by far off, so the night bed itself stays free of events.', '2d',
      'night_bed_t21', 'Cut from take 2-1, where a tram swells in from 10 s; faded in over 3 s and out over 2.5 s.',
      cooldown=40.0, load='memory', when={'light': ['night']}, role='2D one-shot now and then', place='everywhere: the listener', hp=45, stereo=True, rate=32000, cut={'night_bed_t21': (9.0, 19.9)}, optional=True),
]
