"""Brief 090 (Chime): what each service voices. Game lines are copied from contracts/missions/handover/text.json (never edited); TEST lines exist only to check how a name is said, and are marked."""
SCENE = [  # listen-inez, in order
    ('milena', "Have you eaten? You've looked at that Office door all shift."),
    ('inez', "Eat? With the Bureau after me? Waiting tables, I can't keep up."),
    ('milena', "I owe them too. We'll be buried with those cards half stamped."),
    ('inez', "Then bury me somewhere I can sit down."),
    ('milena', "You'd get up to look at that door. Is it so handsome?"),
    ('inez', "Who says? Did Arthur say something? Who else has he told?"),
    ('milena', "He said nothing, you goose. You just did."),
    ('inez', "Yes, I'm seeing him. If the Office hears, it costs us both."),
    ('milena', "Not from me, they won't. Stop looking, though. You can't afford it."),
    ('larthi', "Bureau loan, behind, could be cleared. Arthur, Office door, could be used."),
]
SHOUTS = [  # (speaker, line, where in text.json)
    ('arthur', "Hold it! No running in the square!", 'event run-seen, guard'),
    ('arthur', "Weapon! Drop it! Drop it now!", 'event drawn-seen, guard'),
    ('arthur', "Armed men! Not in my square!", 'event fight-guard, guard'),
    ('arthur', "You! Stop right there!", 'event fail entry-seen-guard, guard'),
    ('larthi', "That car's on fire. Get clear!", 'event car-burns, scout'),
    ('larthi', "Cut loose!", 'ack panic, scout'),
    ('arruns', "Quiet! Quiet!", 'arrive takedown struggle, enforcer'),
    ('police', "Civic Police! Drop your weapons!", 'event fight-loud, security'),
    ('rival', "That's him! Take the car!", 'event fight-safe, rivals'),
    ('watcher', "Gun on camera! Call it in!", 'event drawn-seen, watcher'),
]
NAMES = [  # one line per remaining voice, each carrying a name
    ('hasti', "That's the rest paid. You owe the Bureau nothing, Miss Nikolaou.", 'help-inez-2, fixer'),
    ('hasti', "Fair enough, Lukac. A couple of minutes. We'll be at the bay.", 'pay-driver, fixer'),
    ('lukac', "It's paid, then. It's been a lean month. For what, exactly?", 'pay-driver, driver'),
    ('arthur', "It's only a door. All right, I'm coming.", 'call-inez-pressure, guard'),
    ('larthi', "Popovic's been missed.", 'the close.warning beat, said aloud'),
    ('arruns', "Larthi. With me.", 'TEST line, not in the game'),
    ('sethre', "The loop's running. Two minutes, Larthi.", 'TEST line, not in the game'),
    ('popovic', "Councillor Popovic. I'll take my car now.", 'TEST line, not in the game (he has no lines)'),
]
# how each name is said: IPA for services that read it inline, a respelling for the rest
SAY = {'Larthi': ('ˈlɑːrti', 'Lartee'), 'Popovic': ('ˈpɒpəvɪtʃ', 'Popovitch'), 'Lukac': ('ˈluːkɑːtʃ', 'Lookatch'), 'Nikolaou': ('nɪkoʊˈlaʊ', 'Neeko-lau')}
