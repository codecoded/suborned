# Brief 085: Sky, "Try Terra": the seven conversations from gpt-6.1-terra, game facts and few rules, in story order. Each scene sees what came before it.
import subprocess, json, os
OUT = '/Users/sky/Projects/suborned/briefs/085/terra'; TMP = '/tmp/suborned-terra'
BASE = """Setting: a polite, orderly city, 1950s in style, its technology far ahead. Records are paper: stamped cards and forms. Calls are voices on telephones. It's daytime, near shift change. A cafe faces a square; the Civic Records Office is across it.
The picture: the speakers stand still, facing each other. Nobody sits, walks, carries, hands over or touches anything. Only what I say happens; nobody else pays anybody.
People:
- Inez Nikolaou: waitress. Kind, tired, easily frightened. Behind on her Bureau loan. Secretly lovers with Okafor.
- Milena: works with Inez, fond of her, owes the Bureau too.
- Okafor: guard on the Office's public door, across the square. Loves Inez.
- The Fixer: a stranger, warm and easy, calls her Miss Nikolaou, says "we" and never says who "we" are.
- Zhou: gruff driver for Councillor Vale; always says "the Councillor". No he or she for Zhou.
- The Scout: one of the Fixer's people. Few words.
Write how these people would actually talk. Plain, understood first time. At most 12 words a line. No swearing, no exclamation marks. Format each line as Name: line. Only the conversation.
"""
S = {
 'listen-inez': ([], "Scene, up to 10 lines. Inez and Milena talk at the cafe door during the shift, not knowing the Scout is close by, listening. By the end we've learned Inez owes the Bureau and can't keep up, that she's seeing Okafor, and that if the Office hears it costs them both. Last line: the Scout, very low, to its own people, says what was heard: the loan could be cleared, Okafor could be used."),
 'help-inez-1': (['listen-inez'], "Scene, up to 8 lines. The Fixer has just paid part of Inez's Bureau loan, elsewhere, and comes to tell her. She doesn't know this stranger. She's grateful, frightened, and not ready to do anything for it. He only hints that one small thing may be asked later, and says he'll bring the rest."),
 'help-inez-2': (['listen-inez', 'help-inez-1'], "Scene, up to 9 lines. The Fixer comes back: the rest of the loan is paid. Now he says what's wanted: when he telephones, she calls Okafor over from his door. She realises he knows about them and is frightened. She asks what happens to him: nothing if it goes right, and he never sees us. She'll give Okafor a reason to come: something to tell him that can't go by telephone. She's won over, and offers to warn them if she sees trouble on the square."),
 'call-inez': (['listen-inez', 'help-inez-1', 'help-inez-2'], "Scene, up to 8 lines. The Fixer telephones Inez: it's time. Then she calls Okafor on his door's telephone. She opens with How's your shift, love. She gives her reason. He says he has the door till shift's end, then comes because it's her. He's happy, and knows nothing. Don't say how long."),
 'pressure-inez': (['listen-inez'], "Scene, up to 8 lines. The same woman, another way: the Fixer brings no money. Gently, he says the Office could learn about her and Okafor. She gives in fast and hates it. What's wanted: when he telephones, she calls Okafor off his door. He takes no pleasure in it. Nobody states how long, and nobody mentions her remembering his face."),
 'pay-driver': ([], "Scene, up to 9 lines. The Fixer has paid Zhou, who is waiting by the Councillor's car, and it's been a lean month. Zhou wants the job spelled out. At the chime the Councillor gets in; Zhou drives to the service bay round the back, the east yard, not home, and waits a couple of minutes, then drives on. Zhou doesn't ask who 'we' are and won't look at what happens. Zhou can send off anyone watching the bay."),
 'call-inez-pressure': (['listen-inez', 'pressure-inez'], "Scene, up to 9 lines. The Fixer telephones the pressured Inez: it's time. Then she calls Okafor on his door's telephone, saying she has something to tell him that can't go by telephone. He says he has the door, then agrees to come. It ends with him coming. Then, on its own at the end after a line reading ---, two lines for when he reaches her: Inez tells him someone knows about them and that they looked suspicious; Okafor says his door's standing empty and he goes."),
}
done = {}
for sid, (prior, ask) in S.items():
    story = ''.join(f"What the player has already read, {p}:\n{done[p]}\n\n" for p in prior)
    p = BASE + '\n' + story + ask
    r = subprocess.run(['codex','exec','-m','gpt-6.1-terra','--skip-git-repo-check','--sandbox','read-only','--ephemeral','-o',f'{TMP}/{sid}.txt',p], cwd=TMP, capture_output=True, text=True, stdin=subprocess.DEVNULL, timeout=300)
    done[sid] = open(f'{TMP}/{sid}.txt').read().strip() if os.path.exists(f'{TMP}/{sid}.txt') else ''
    print(sid, r.returncode, flush=True)
json.dump(done, open(f'{OUT}/scenes.json','w'), indent=1, ensure_ascii=False)
for k, v in done.items(): print(f'\n## {k}\n{v}')
