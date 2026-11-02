"""The pages as data only: no positions. Each option's skin fills the same slots. Words are draft for Fable; sources are in ../../notes.md and ../notes.md. Glyph, brief 085."""
L, T, U = 'learned', 'told', 'unk'
INEZ_K = [('fixer', "The Fixer's face"), ('p_time', 'Where and when')]
INEZ = dict(section='people', clock='2:10', pic='waitress', name='Inez', sub='café waitress', state='Turned',
            facts=[('debt', 'Bureau loan', 'cleared', L), ('fearful', 'Fearful', None, L), ('lovers', 'Loves Okafor', None, L), ('help', 'Helped twice', '$40,000', L)],
            does=[('call', 'Call', 'Door 2:54', '')],
            meters=[('Loyal', 2, 3), ('Afraid', 1, 3), ('Bitter', 0, 2), ('Brave', 0, 3)], knows=INEZ_K,
            track=[('watch', 'Watched', '0:29', 1), ('listen', 'Listened', '0:59', 1), ('help', 'Not ready', '1:35', 1), ('help', 'Turned', '2:10', 2)],
            others=[(None, 'Okafor'), ('driver', 'Driver'), ('councillor', 'Vale')])
# later in a campaign: the same woman with everything the rules can read. Only fields a rule reads (needs, temperament, ties, ladders, FactsOf)
INEZ12 = dict(INEZ, clock='1:12', sub='café waitress · third job',
              facts=INEZ['facts'] + [('s_ready', 'Accomplice', None, L), ('kin', 'Workmate Milena', None, L), ('car', 'Rode in our car', None, L), ('s_calling', 'Called the guard', 'twice', L),
                                     ('take', "Saw the Enforcer", None, L), ('mood_friendly', 'Friendly', None, L), ('housing', 'New need: papers', None, T), ('talker', 'Keeps quiet', None, L)],
              others=[(None, 'Okafor'), ('driver', 'Driver'), ('councillor', 'Vale')])
GUARD = dict(section='people', clock='0:05', pic=None, bust=True, name='The guard', sub='door guard', state=None,
             facts=[(None, 'Needs', '?', U), (None, 'Nature', '?', U), (None, 'Secret', '?', U)],
             does=[('watch', 'Watch', 'Scout 20 s', ''), ('listen', 'Listen', 'Watch first', 'shut')], others=[('waitress', 'Inez'), ('driver', 'Driver'), ('councillor', 'Vale')])
AGENT = dict(section='cell', clock='0:05', pic='scout', name='Scout', sub='sees furthest, 30 m', state=None,
             facts=[('s_ready', 'Free', None, L), ('fire', 'Sidearm', None, L)],
             does=[('watch', 'Watch', '20 s', ''), ('listen', 'Listen', '30 s', ''), ('mislead', 'Tip', 'No rivals yet', 'shut')], others=[('fixer', 'Fixer'), ('tech', 'Tech'), ('enforcer', 'Enforcer')])
JOB = dict(section='job', clock='2:40', pic='councillor', name='Take Vale alive', sub='to the getaway car', state=None,
           facts=[('body_dead', 'Vale dies', None, L), ('car', 'Vale leaves by car', None, L), ('takedown', 'Rivals reach him', None, L), ('eye_open', 'Seen going in', None, L),
                  ('s_downed', 'Enforcer down', None, L), ('car', 'Our car wrecked', None, L), ('lock', 'District seals', None, L)],
           does=[('enter', 'Front door', 'Before 4:50', ''), ('car', 'Pickup', 'From 5:10', '')],
           track=[('car', 'Rival car', '2:35', 1), ('nextevent', 'Chime', '4:30', 0), ('turn_person', 'Last chance, driver', '4:40', 0), ('s_moving', 'Vale leaves', '4:50', 0), ('car', "Vale's car leaves", '5:10', 0)])
CASH = dict(section='cash', clock='2:40', glyph='cash', name='$60,000', sub='in hand', state=None,
            facts=[('p_cash', 'Started with', '$100,000', L), ('help', "Inez's loan", '$40,000', L)],
            track=[('help', 'Inez, first visit', '1:35', 1), ('help', 'Inez, second visit', '2:10', 1)])
NAMES8 = [('waitress', 'Inez'), (None, 'Okafor'), ('driver', 'Zhou'), ('councillor', 'Vale'), (None, 'Milena'), (None, 'A. Dahl'), (None, 'A. Haddad'), (None, 'O. Ekman')]
LIST8 = dict(section='people', clock='2:40', title='The people', count='8', people=NAMES8)
# the rule above 24 people: groups, a big number each. Scaled from the real 130-person cast's jobs to 200: counts are illustrative
LIST200 = dict(section='people', clock='2:40', title='The people', count='200',
               groups=[('turned', 'Turned', 12), ('p_cash', 'Clerks', 89), ('s_inside', 'Residents', 37), ('door_held', 'Guards', 25), ('help', 'Servers', 22), ('care', 'Others', 15)])
SECTIONS = [('cell', 'units', 'Cell'), ('people', 'lovers', 'People'), ('job', 'take', 'Job'), ('journal', 'papers', 'Journal')]
WITH_CASH = SECTIONS[:3] + [('cash', 'cash', 'Cash')] + SECTIONS[3:]
PAGES = dict(inez=('Inez, turned', INEZ, SECTIONS), job=('The job', JOB, SECTIONS), facts12=('12 facts', INEZ12, SECTIONS), unknown=('Mostly "?"', GUARD, SECTIONS), agent=('An agent', AGENT, SECTIONS),
             list8=('8 people', LIST8, SECTIONS), list200=('200 people', LIST200, SECTIONS), cash=('A new section', CASH, WITH_CASH))
