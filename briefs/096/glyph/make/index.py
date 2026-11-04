"""Glyph, brief 096: builds ../index.html, self-contained (images embedded as JPEG). Run after render.sh."""
import base64, io
from PIL import Image
D = '/Users/sky/Projects/suborned/briefs/096/glyph/'
def img(name, w=None, q=86):
    im = Image.open(D + name).convert('RGB')
    if w and im.width > w: im = im.resize((w, round(im.height * w / im.width)), Image.LANCZOS)
    b = io.BytesIO(); im.save(b, 'JPEG', quality=q); return 'data:image/jpeg;base64,' + base64.b64encode(b.getvalue()).decode()
def fig(name, cap, w=1920): return f'<figure><img src="{img(name, w)}" alt="{name}"><figcaption><code>{name}</code> · {cap}</figcaption></figure>'

KEYS = [
 ('J', 'The book', 'No', 'HostBook.cs (PlayAction.Book)', '<b>Book key</b>, bottom right, in the notes key\'s place (mock 2). J stays.'),
 ('B', 'Briefing', 'No', 'HandoverHost.cs:420', 'Retire: the briefing is the book\'s Job page. B opens the book on Job.'),
 ('L', 'History (the log)', 'Yes: the notes key', 'HudToolkit.Less.cs:111', 'Moves into the book\'s Journal. The notes key becomes the Book key. L opens the book on Journal.'),
 ('Space', 'Pause', '<b>No</b>: the pill\'s pause glyph is only a picture', 'HudToolkit.Less.cs:88', 'Make the pill\'s pause glyph a button: click pauses and resumes (mock 2d).'),
 ('Esc', 'Pause menu', '<b>No</b>', 'HostMenu.cs', '"Menu" as the last item on the book\'s rail (Esc stays). Question 7.'),
 ('Q / E', 'Turn the view', '<b>No</b>', 'HostTurn.cs:88', 'Two turn buttons on the plot scope\'s rim (mock 2e).'),
 ('Alt (hold)', 'Detail: zero fields, the breakdown, every learned cone', '<b>No</b>', 'HostSight.cs:86', 'Hover a price for about 0.6 s and it shows the breakdown. Alt stays as "everything at once".'),
 ('X', 'Stop firing', '<b>No</b>: the fire line shows an "X stop" keycap, not a button', 'HudToolkit.Fire.cs:60', 'Make that plate a button ("✕ Stop"). Holstering on the portrait also stops.'),
 ('= / −', 'Speed', '<b>No</b>', 'HandoverHost.cs:453', 'No button: it hides waiting. Keep the keys for testers (question 6).'),
 ('N', 'Next event', '<b>No</b>', 'HandoverHost.cs', 'No button. Off in the player\'s build ("skipping isn\'t the game mechanic I want"). Question 6.'),
 ('T', 'Third person [lab]', 'Leaving yes ("Back to map" in the Squad slot); entering no', 'HostFollow.cs:306, HudToolkit.Hold.cs:167', '[lab]: no button until you promote it. Then a "Walk as" item on the agent\'s ring.'),
 ('H', 'Hints', 'Partly: the "?" disc brings one back, ✕ dismisses, on/off is in Options', 'HudToolkit.Less.cs:144, FrontEnd.Panel.cs:324', 'Fine. Add a "Show hints again" button in Options for Shift+H.'),
 ('K', 'Acknowledge a card', 'Yes: click the card', 'HudToolkit.cs:1002', 'Fine. In the new feed, a click is how a card goes.'),
 ('I', 'Intel', 'Yes: the people discs under the pill', 'HudToolkit.Less.cs:96', 'Fine. Later it becomes the book\'s People page.'),
 ('R', 'Response list', 'Yes: the response pill, when shown', 'HudToolkit.Takedown.cs:61', 'Fine.'),
 ('C', 'Cancel', 'Yes: ✕ on the busy chip', 'HudToolkit.Less.cs:126', 'Fine.'),
 ('F', 'Draw / holster', 'Yes: the sidearm mark on the portrait', 'HudToolkit.cs:500', 'Fine.'),
 ('V', 'Stance', 'Yes: the stance fan', 'HudToolkit.Live.cs:224', 'Fine.'),
 ('G', 'Walk / run', 'Yes: the busy chip\'s lead glyph', 'HudToolkit.Less.cs:130', 'Fine.'),
 ('1–4, 5, Tab, ⇧1–4, Z', 'Select, select all, step or pick an action, step the target', 'Yes: portraits, Squad, the ring, the wheel on a stack', 'HudToolkit.cs:311, HudToolkit.Hold.cs:167', 'Fine.'),
 ('Return', 'Play again', 'Yes: Replay on the end card, Restart in the menu', 'HudToolkit.cs:609', 'Fine.'),
 ('Y, P, F11, F12, `', 'Target mode [lab], camera lab [lab], HUD view, debug, all strips', 'No', 'PlayControls.cs', 'Dev and lab tools. Keep them key-only and hide them from the player\'s Controls page.'),
]
CRUCIAL = [
 ('1', 'Alert goes up', 'It changes who notices what right now, so the plan has to change.'),
 ('2', 'One of ours is seen acting', 'It adds Trail and can start a report. You may need to stop or move.'),
 ('3', 'A report lands, or police are called', 'A clock starts: get away, or deal with it before they arrive.'),
 ('4', 'The seal starts', 'This is the hard deadline for Go. The countdown also stays under the clock.'),
 ('5', 'The rival car turns in', 'Others are after Popovic, and they can beat you to him or the car.'),
 ('6', 'Rivals start for our car (obj. 7)', 'A held car means Go is refused. It\'s the one loss you can still head off, with a fix on the card.'),
 ('7', 'One of ours goes down', 'You lose what that agent can do. If it\'s the Enforcer, you lose the run.'),
 ('8', 'Popovic comes out', 'The door window closes and the pickup window opens.'),
 ('9', 'A turn lands', 'A way in opens: Inez can be called, or the driver will bring him.'),
 ('10', 'A turn is refused', 'A way in closes or costs more. Change plans while there\'s still time.'),
 ('11', 'A body is found (my addition)', 'Trail and Alert jump, and an investigation starts.'),
]
NOT = 'Overheard lines and learned facts (Intel fills, the book\'s dot shows new) · acts finishing ("Looped", "Watched": the portrait\'s success beat and the busy chip already say so) · city chatter and scenes · the chime (sound and the clock) · rivals posting and parking · refusals (a pop at the click) · the cell\'s chatter (the ticker).'

Q = [
 ('Start page: drop the old briefing\'s stills and the drawn plan?',
  'The old page (brief 070) had five close-up stills and a small plan. On Linux the stills came up blank, and they need their own capture step. The book shows the real street instead.',
  'A) Drop both. Faces stay as cameos, and hovering a way or a face turns the street to the place (mock 1b). B) Keep the stills as a strip under each way. C) Keep a small plan inset.',
  'A. It\'s one data source with no capture step, and it shows the place where you\'ll actually play it.'),
 ('The rivals\' 2:35 on the start page: shown, or "?" until seen?',
  'The built book shows "?" for the rival car until the cell sees it (its timeline has known: false). The old briefing told 2:35 up front, and your brief lists it.',
  'A) Show 2:35 from the start on both the start page and the book. B) "?" until seen on both.',
  'A. The briefing has always told it, and one design needs one rule. Remember that seeds may move it.'),
 ('Where the book icon sits',
  'Today the notes (history) key is bottom right, and the feed stacks above it.',
  'A) Bottom right, replacing the notes key, with the feed card just above and the "new" dot on the key. B) Top right, under the people discs.',
  'A. The new card and the dot are in the same place, and it adds about 0.14 % of the screen at rest.'),
 ('Icon alone, or icon + the word "Book"?',
  'Your complaint was that people can\'t find it. The Squad button already carries a word.',
  'A) Icon + "Book" (mock). B) Icon only.',
  'A. It reads first time, and drops to icon-only later if it crowds.'),
 ('Does a crucial card ever leave on its own?',
  'You said cards "last until clicked". Some go stale: rivals turn back, or the police are already here.',
  'A) A card that\'s no longer true drops into the Journal by itself. Everything else waits for a click. B) Every card waits for a click.',
  'A. A stale warning misleads, and a fair check shouldn\'t depend on clearing old cards.'),
 ('Speed (= / −) and next event (N)',
  'They are key-only today. You turned down skipping ("Syndicate with strategy").',
  'A) No buttons. N is off in the player\'s build, and = / − stay as keys for testers. B) A speed button on the pill.',
  'A.'),
 ('A menu without Esc',
  'Esc is the only way to reach Options, Controls, Restart and Quit during play.',
  'A) "Menu" as the last item on the book\'s rail. B) A small ≡ plate on the HUD.',
  'A. It costs the HUD nothing, and the book is one click away.'),
 ('The order of queued cards',
  'Several can arrive at once in a fight.',
  'A) Most urgent first: seal > police > agent down > rivals for the car > Alert > seen > Popovic out > rival car > turned or refused > body found. B) Oldest first.',
  'A.'),
]

h = ['''<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Brief 096 · Glyph mocks: the start page, the book key, the feed and the Journal, the held car</title>
<style>
body{margin:0;background:#f4f1e8;color:#17302f;font:16px/1.5 -apple-system,"Segoe UI",sans-serif}
main{max-width:1500px;margin:0 auto;padding:28px 28px 80px}
h1{font-size:28px;margin:0 0 4px} h2{font-size:22px;margin:42px 0 8px;padding-top:14px;border-top:2px solid #d9d4c6} h3{font-size:17px;margin:20px 0 6px}
.lead{color:#5d6b68;margin:0 0 18px}
.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px} .tri{display:grid;grid-template-columns:repeat(3,1fr);gap:16px;align-items:start}
figure{margin:0;background:#fff;border-radius:10px;padding:8px;box-shadow:0 1px 3px rgba(0,0,0,.08)} figure img{width:100%;display:block;border-radius:6px}
figcaption{font-size:13.5px;color:#5d6b68;padding:6px 4px 2px}
.rec{background:#e3f0ec;border-left:4px solid #1f6c67;padding:10px 14px;border-radius:6px;margin:10px 0}
table{border-collapse:collapse;width:100%;font-size:14px;background:#fff} th,td{text-align:left;padding:6px 8px;border-bottom:1px solid #e6e1d4;vertical-align:top} th{background:#ebe6d8}
code{font-size:12.5px;background:#ebe6d8;padding:1px 4px;border-radius:3px}
.q{background:#fff;border-radius:8px;padding:12px 16px;margin:10px 0;box-shadow:0 1px 3px rgba(0,0,0,.06)} .q b.t{display:block;font-size:16px}
.small{font-size:13.5px;color:#5d6b68}
</style></head><body><main>
<h1>Brief 096 · mocks for Sky (Glyph, 2026-11-04)</h1>
<p class="lead">Mocks only: nothing in <code>unity/</code> changed. Every frame is a real capture (the batch's own shots, 0.2.2xx) with the kit's own fonts, icons and plates on top, drawn from HTML by <code>make/</code> (<code>prep.py</code>, <code>render.sh</code>, <code>index.py</code>). No image generator was used and nothing was spent. All words are placeholders for Fable. Times follow the shift at 5:30 (objective 1): chime 5:10, last chance 5:20, Popovic out 5:30, his car 5:50, with the rivals at 2:35.</p>
<div class="rec"><b>In short.</b> (1) The start <i>is</i> the book's Job page, with Begin where Back would be. (2) A Book key, bottom right, takes over the notes key. (3) The feed shows one crucial card at a time and keeps it until clicked. Everything else goes to the book's Journal, and a dot on the Book key says there's something new. (4) Rivals going for the held car get a card with its fix, and Go on the car's ring says who holds it and what to do.</div>

<h2>1 · The start briefing is the book's Job page</h2>
''', '<div class="pair">', fig('1-start-job.png', 'the start: the clock is stopped at 0:00, the opening view behind, Begin bottom right'), fig('1b-start-job-hover.png', 'hovering a way or a face turns the frozen street to the place, joined by a line (Pickup → the driver at the official bay)'), '</div>',
'''<h3>What's on it, and where it comes from</h3>
<ul>
<li><b>Target, goal and fails</b>: the book's job slot (<code>text.json book.job</code>), with the same folded "Ways to fail 9 ›". The hook moves to a short second line (Fable).</li>
<li><b>The two ways, with their conditions</b>: <code>book.job.needs</code>, so they're open circles at the start and ticked live in play from the same slots. New words: "Before 5:30", "From 5:50", "The driver is turned <i>by 5:20</i>".</li>
<li><b>Who each way runs through</b>: face cameos (Inez and the guard; the driver). A hover or click turns the street to them. These replace the stills.</li>
<li><b>The shared end</b> (objective 3's line, for Fable): "Both end at our car: Popovic in · Go · the rest clear before the seal".</li>
<li><b>The clock</b>: the book's track with the new times. The last station is "His car leaves 5:50", which is plainer than "Popovic at car".</li>
<li><b>The cell</b>: four faces, name, job word and one line each. A face opens that agent's Cell page. <b>Dropped:</b> "Gives the rivals a false tip". The Scout's line is now "Watches from furthest; listens in."</li>
<li><b>The purse and Heat</b> on the "Ways to fail" row, since §10 lists Heat on the briefing. <b>Begin</b> is the book's orange. The rail works, and People and Journal are dim until there's something in them.</li>
</ul>
<div class="rec"><b>What I keep and drop.</b> I'd drop the five stills and the drawn plan. The frozen street is the plan and the stills, it's what you'll play on, and it needs no capture step: the Linux blank came from that step. I'd keep the faces, because a new player needs to know <i>who</i> each way runs through. One design and one data source mean Job in play and Job at the start are the same page. Begin closes it and the clock runs from the view behind it.</div>

<h2>2 · A clickable book, and every key-only feature</h2>
''', fig('2-hud-book.png', 'the HUD at rest (a real 0:05 frame): ① the Book key replaces the notes key ② the pause glyph becomes a button ③ turn buttons on the scope\'s rim', 1920),
'<div class="tri">', fig('2c-book-key-2x.png', 'Book key at 2x: the kit plate, a stand-in open-book glyph, the word, the turquoise "new" dot', 800), fig('2d-pill-pause-2x.png', 'the pill\'s pause glyph as a button (hover ring shown), at 2x', 800), fig('2e-scope-turn-2x.png', 'Q / E as two turn buttons on the scope\'s rim (the kit\'s turn_view icon), at 2x', 600), '</div>',
'''<div class="rec"><b>Recommendation.</b> The book key goes bottom right, with an icon and the word "Book", in the notes key's slot. The notes become the book's Journal, so nothing is lost, and the feed card sits right above it. It costs about +0.14 % of the screen at rest (104 × 40 against 36 × 36). The glyph is a stand-in that Vela or I would draw into <code>icons/singles</code>. J stays, and so do B (opens Job) and L (opens Journal).</div>
<h3>Every key in <code>PlayControls.cs</code>, checked in the code for a click</h3>
<table><tr><th>Key</th><th>Feature</th><th>Clickable today?</th><th>Where (code)</th><th>Proposal</th></tr>''']
for k in KEYS: h.append('<tr>' + ''.join(f'<td>{c}</td>' for c in (f'<b>{k[0]}</b>', k[1], k[2], f'<code>{k[3]}</code>', k[4])) + '</tr>')
h.append('</table><p class="small">Key-only today, and players need a click for each: J, B, Space, Esc, Q/E, Alt, X. Speed and next event are a design question, not a gap. Lab and dev keys stay as they are.</p>')
h.append('<h2>3 · The feed and the Journal</h2><div class="pair">')
h += [fig('3a-feed.png', 'the slim feed in a real 2:00 frame: one card, kept until clicked, "1 of 2 ›", above the Book key with its dot'), fig('3c-journal.png', 'the book\'s Journal: today\'s full L history, newest first, with time and place. Crucial rows are orange. A clicked row opens its lines and result, and the street turns to the place')]
h.append('</div><div class="pair" style="margin-top:16px">')
h += [fig('3b-feed-cards.png', 'the crucial list, one card each (1.4×), numbered as in the table below'), fig('3d-feed-card-2x.png', 'the card at 2x: the kit\'s panel_dark, a badge on a disc clear of the rim, the count clear of the screws', 900)]
h.append('''</div>
<h3>How it behaves</h3>
<ul>
<li><b>One card at a time</b>, bottom right, above the Book key. It stays until clicked, and a click shows the next ("1 of n ›"). The clock runs, nothing pauses, and there's no time limit, so nobody misses one. Today's version shows two beats at once for 4 s each (<code>text.json less.feed</code>: maxOnScreen 2, seconds 4), which is why it felt "so fast I hardly bother reading it".</li>
<li><b>The card</b> is the kit's <code>panel_dark</code> plate: a badge or face on a disc, a title of 4 words or fewer, then where and when in one dim line. Only the rivals card carries a fix, as two small chips. Clicking the place pans there without dismissing the card.</li>
<li><b>Everything goes to the Journal</b>, crucial or not. The Book key's dot means the Journal has rows you haven't seen. Opening the book stops the clock, so it's the place to read.</li>
<li><b>The Journal</b> holds today's L history as rows: time, icon, what happened, place, and a turquoise dot for unread. The filters are today's (All, Crucial, Learned, Turned, City). A row opens to the scene's lines (speech in Barlow, with the speaker's cameo) and its result as icons. The face on the curve is who the row was about, and the line goes to where it happened, the same rule as the people pages.</li>
</ul>
<h3>The crucial list (proposal for you to judge in a build)</h3>
<table><tr><th>#</th><th>When</th><th>Why it's crucial</th></tr>''')
for c in CRUCIAL: h.append(f'<tr><td>{c[0]}</td><td>{c[1]}</td><td>{c[2]}</td></tr>')
h.append(f'</table><p class="small"><b>Journal only, never a card:</b> {NOT}</p>')
h.append('<h2>4 · Objective 7: guarding the held car</h2><div class="pair">')
h += [fig('4a-rivals-car.png', 'rivals start for our car: the crucial card with its fix chips. Each rival walking in wears the car disc in a violet ring (built, r123 §3), and the car\'s lock marker takes the same ring. Seat pips full'), fig('4c-car-ring-2x.png', 'the car\'s ring at 2x, Go hovered and shut: the striped wedge with its lock and one violet holder dot, the pill "Go 🔒 held by ◆" and the fix line under it. The holder\'s mark pulses', 900)]
h.append('''</div>
<div class="rec"><b>Recommendation.</b> The card fires on <code>Game.RivalsForCar</code> (code <code>rivals.for-car</code>, one per beat, as the feed already dedupes) and stays until clicked or until no rival is still walking in (question 5). On the ring, Go stays at 12 o'clock once Popovic is aboard. While the car is held it's shut by shape (stripes and a lock, never a colour of its own), with one violet holder dot on the wedge. Its pill says what happened and who did it ("Go 🔒 held by" and a badge per holder), and the line under it gives the fix ("Step out and draw · or down them"). That follows §10's refusal rule: what happened, why, the fix. A click on the shut wedge pulses the holder marks, which is already in the GDD. Fable's book line ("while the car is held, someone guards it outside, gun drawn") teaches this before it costs.</div>
<p class="small">These frames are borrowed from a 3:50 fight capture, with the marks painted on. In it the Enforcer is still carrying Popovic while the seat pips show him aboard, and the goal line still reads "Get Popovic to the car". Read the marks, not the story. Only one rival is near the car, so the pill says "held by" one, and the other two wear the car disc.</p>
<h2>Open questions for Sky</h2>''')
for i, (t, ctx, opts, rec) in enumerate(Q, 1):
    h.append(f'<div class="q"><b class="t">{i}. {t}</b><div class="small">{ctx}</div><div>{opts}</div><div><b>Recommended:</b> {rec}</div></div>')
h.append('''<h2>Evidence and limits</h2>
<ul class="small">
<li>The key audit: every <code>PlayAction</code> traced with <code>rg</code> to where it's read, and every HUD element with a <code>PointerDownEvent</code> or <code>ClickEvent</code> listed (<code>HudToolkit*.cs</code>, <code>Host*.cs</code>). The file and line references are in the table.</li>
<li>Every HUD piece was checked cropped at 2x (2c, 2d, 2e, 3d, 4c). Two faults found and fixed: the card's count sat under a corner screw, and its badge crossed the rim.</li>
<li>HUD budget: not measured by <code>tools/hud-budget</code>, since mocks have no HUD layer. Worked out by hand: +0.14 % at rest (the Book key). The card is about 1.4 % while shown, against 22 % allowed with a card.</li>
<li>The frames are real captures with the HUD painted out where the page covers it. Soft smudges show where bubbles or the pill were removed (above Inez in 3c, the top of 1b).</li>
<li>New words for Fable: "Book", the card titles, the shared-end line, "The driver is turned by 5:20", "His car leaves", the Journal's subline, "Go · held by", "Step out and draw · or down them".</li>
</ul>
</main></body></html>''')
open(D + 'index.html', 'w').write('\n'.join(h))
print('ok')
