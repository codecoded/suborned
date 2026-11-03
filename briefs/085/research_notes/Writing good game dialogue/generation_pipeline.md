# Good written dialogue from a build-time generation pipeline

Reading key for every finding below. **[Measured]** means a study with data. **[Self-report]** means interviews or practitioner reports. **[Opinion]** means argument or craft judgement. **[Local count]** means my own count over our files; nothing was run but a word counter, and no model was called.

Two facts change how `scenes.json` should be read:
- Its header says it was "Written by Fable in the session ... NOT by a pipeline run" — [scenes.json](/Users/sky/Projects/suborned/briefs/085/fable/scenes.json). So the three takes, the editor pass and the check-and-fix loop have never been judged by these samples. They test the brief and the sheets, not the pipeline.
- Most pages reached me through a summarising fetch tool, so quotes should be checked against the original before being repeated publicly.

## What are the known tells of model-written dialogue and fiction, and which show in scenes.json?

### Takeaway
The main fault in our samples is one clipped register shared by everybody: 72% of lines are exactly 9 or 10 words, and the Fixer and Inez have near-identical rhythm. Next come sheet directions transcribed into speech and lines written as closers. The literal "not X, but Y" is nearly absent, so banning it would fix little. Several tells are well documented for prose, but only thinly for dialogue specifically.

### Cited Findings

**Documented tells, general**
- [Self-report, 2023, GPT-3.5] 17 MFA students wrote 30 stories with the model. They said it "effectively autocompletes with the most obvious version of whatever you ask for", "can't effectively write real scenes with realistic dialogue", is "much better at telling than showing", and "has one narrative that it knows how to write". On endings: "very much 'Here is the lesson learnt'", and it conflates "a satisfying ending" with "a completely tied up one" — [Chakrabarty et al., Creativity Support in the Age of LLMs](https://ar5iv.labs.arxiv.org/html/2309.12570).
- [Self-report, 2022, Chinchilla] 15 theatre and film professionals on Dramatron: "a bit too verbalised, there should be more subtext" (p6), motivations stated outright and relationships "so tight and prescriptive" (p4, p5), "on the nose, pun names" (p2), and "it has no idea of what the previous dialogue contained", because dialogue was generated in parallel (p1). The creative team described a flat, declarative "glitch style" — [Mirowski et al., Dramatron](https://ar5iv.labs.arxiv.org/html/2209.14958).
- [Measured, 2024–25, GPT-4o, Claude 3.5 Sonnet, Llama 3.1] 18 MFA writers made 8,035 edits on 1,057 LLM paragraphs. Shares reported to me: awkward word choice 28%, poor sentence structure 20%, unnecessary exposition 18%, cliché 17%. Purple prose, lack of specificity and tense inconsistency complete the taxonomy, with shares not reported in what I read. No model beat the others. Word-choice and cliché edits fall as quality scores rise; exposition and lack-of-specificity edits stay roughly constant. This is prose paragraphs, not dialogue — [Chakrabarty, Laban, Wu, Can AI writing be salvaged?](https://ar5iv.labs.arxiv.org/html/2409.14509).
- [Measured, June 2026, including Claude Opus 4.6, GPT-5.2, Gemini 3.1 Pro] Stories from the same model were judged similar to each other 80–90% of the time for most models (Kimi K2.5 73.9%), against 30.8% for human stories. Frontier models converge on a "mean" generic narrative. This measures plot sameness over 200–300 word stories — [Thennal DK & Hatzel](https://arxiv.org/html/2606.17350).
- [Measured, secondhand] "Not X but Y": Pangram's founding engineer estimates "not just X but Y" appears about three times as often in AI writing, per The Atlantic. A Washington Post analysis found the construction in about 6% of July 2025 ChatGPT chats. The article's denominators don't reconcile and its causes are unconfirmed — [Lazovic, July 2026](https://brandonlazovic.dev/articles/llm-negative-parallelism-tic/). Originals not opened: [The Atlantic](https://www.theatlantic.com/technology/2026/07/ai-chatbot-writing-tic-negative-parallelism/687892/), [Boston Globe](https://www.bostonglobe.com/2025/11/13/business/chatgpt-writing-style-clues/).
- [Opinion, unreviewed field guide] Wikipedia's guide lists negative parallelisms ("It's not ..., it's ...", "no ..., no ..., just ...") and has a rule-of-three section (heading seen, text not read). It says later output (it names GPT-6, mid-2026) shows "much more period and comma usage and shorter sentences around a strict 8–20 word limit". It warns that these signs also appear in human writing, and that it is "observations, not rules" — [Signs of AI writing](https://en.wikipedia.org/wiki/Wikipedia:Signs_of_AI_writing).
- [Opinion, informal] In Mark Lawrence's eight-story experiment, as summarised in a newsletter, the only pieces with good dialogue were human-written. The summary itself says it was not a formal study. Seen as a search excerpt only — [Franks](https://jasonfranks.substack.com/p/llms-and-the-business-of-fiction-573).
- I found no source that documents "every line a clever retort" as a measured tell. In the notes below it is my reading only.

**Our samples: rhythm [Local count over [scenes.json](/Users/sky/Projects/suborned/briefs/085/fable/scenes.json), 50 lines; descriptive only, small sample]**

| Measure | Result |
|---|---|
| Lines of exactly 9 or 10 words | 36 of 50 (72%) |
| Lines of 8–10 words | 41 of 50 (82%) |
| Mean, sd, all lines | 8.7, 1.7 words |
| Fixer (18 lines) | mean 9.1, sd 1.1 |
| Inez (20 lines) | mean 8.9, sd 1.1 |
| Words per sentence or fragment: all, Inez, Fixer, Scout | 3.9, 4.0, 4.2, 3.3 |
| Lines of 2+ sentences or fragments | 42 of 50 |
| Lines of exactly three sentences or fragments | 19 of 50 (38%) |
| Lines containing a question | 13 of 50 |
| Lines broken off or interrupted | 0 of 50 |

The tone contract gives the Scout "fragments" and the Fixer "warm and persuasive: reassures, says 'we', puts people at ease". It gives Inez "warm café patter, quick, tired" — [text.json](/Users/sky/Projects/suborned/contracts/missions/handover/text.json). On these counts everyone talks like the Scout: the Fixer's sentences average 4.2 words against the Scout's 3.3.

**Our samples: the tells, with lines from [scenes.json](/Users/sky/Projects/suborned/briefs/085/fable/scenes.json)**

| Tell | Lines | My judgement |
|---|---|---|
| Clipped three-part staccato | "Tube came for you. Bureau stamp. That's three this month." · "Three payments. I'll catch up. I always catch up." · "Keep smiling. You and Okafor. The Office doesn't know, yet." · "The rest of it. Look. You owe them nothing." · "Heavy. Spell it out. I've been paid vague before." · "I know. I don't enjoy this. It changes nothing." | Strongest fault |
| Speakers swappable | Fixer: "I know. I don't enjoy this. It changes nothing." could be Zhou's. Zhou: "Couple of minutes. Then I drive on, whoever's late." could be the Fixer's. Neither is warm or gruff by its form | Strong |
| Sheet directions turned into speech | Sheet "been paid to be vague before" → "I've been paid vague before." · Sheet "nothing happens to him if this goes right, and he never sees us" → "If this goes right, nothing. He never even sees us." · Sheet "takes no pleasure in it and doesn't pretend otherwise" → "I don't enjoy this." · Sheet "must sound ordinary" → "Let me sound like myself first." ([sheets](/Users/sky/Projects/suborned/tools/dialogue/conversations.ts)) | Strong; the source of the on-the-nose lines |
| Too articulate about feeling or situation | "If the Office hears about Okafor, we're both finished." · "This is all I've got." · "We keep our word, whatever you think of us." | Moderate; the first is demanded by the brief |
| Written closers | "I've had a good look at your face." · "Who's we? Knowing costs extra. I'll shift anyone watching my bay." · "People can see this door. I've said yes to nothing." · "He'll walk back and never know. Thank you, Inez." | Strong; four of six scenes |
| Retorts and sharpness | "On tips? Ask your friend. He's looking again." · "He crosses the whole square for one coffee, Inez." · "Knowing costs extra." | Moderate |
| Stock lines from the genre | "Keep smiling." · "What do you want?" (word for word in two scenes: help-inez-2, pressure-inez) · "Nothing's wrong." · "Who told you? Who else knows?" | Moderate |
| Negative closure, the neighbour of "not X, but Y" | 9 of 50 lines use "nothing" or "nobody": "It changes nothing." · "You owe them nothing." · "I've said yes to nothing." · "Not for nothing." | Moderate tic. The literal "not X, but Y" form is almost absent |
| Sentimental ending | "Thank you, Inez." · "We keep our word" | Mild |
| Strict ping-pong | No overlap or line cut off, against the writer brief's "A line can break off because of the work". "Not where the tables can see." is the clearest of very few lines that react to the work at hand | Strong |

Lines that show what to protect, in my judgement: "Then take it back. No. They'd only send another notice." (she changes her mind inside the turn, and the reason belongs to this city), "Six years I've paid them." and "On shift? You've never once asked me that."

### Inferences
- Most of these tells trace to what we asked for rather than to the model's default alone. Two sheet phrases request closers directly: "Let it land near the end" (pressure-inez) and "says why not, in a line" (pay-driver). The editor's "Better one strong line than three fair ones" rewards sharpness. The brief's "aim for 10 or fewer" sits exactly where the lines cluster. The evidence is the matching of lines to phrases; no comparison run tests it.
- Scene independence shows up as a continuity tell. Milena's "He crosses the whole square for one coffee" sits badly with the sheet's "The two are never together unless she calls him" and with Okafor's "You've never once asked me that". The code runs scenes with `Promise.all`, so each is blind to the others — [conversations.ts](/Users/sky/Projects/suborned/tools/dialogue/conversations.ts). That is the failure Dramatron's p1 described. These samples were hand-run, but the pipeline would have the same blindness. Whether the coffee line breaks a rule is Motive's call.
- A banned-tells list copied from the internet would miss our main problem. The literal famous tics are rare here; the sameness is structural.

### Gaps
- I found no peer-reviewed study that measures dialogue-specific tells: retorts, speaker sameness or closers in generated dialogue. [DramaBench](https://arxiv.org/html/2512.19012v1) (Dec 2025, search excerpt only) defines an out-of-character rate and a voice-distinctiveness score, judged by an LLM. I could not see how well that judge agrees with human raters.
- [I, ChatGPT](https://www.nature.com/articles/s41599-025-06341-2) (2025) may have measured linguistic differences, but it redirected to a login and was not read.
- No study tests Fable-class models. The newest measured evidence I read covers Opus 4.6 and GPT-5.2.
- The 2022–23 complaints used far weaker models and may not hold in degree, though the June 2026 sameness result and our samples suggest the sameness tells persist.

## What do studies and practitioners say works?

### Takeaway
The best-measured levers are real voice samples, targeted expert-style edits, a human editor, and asking for many candidates in a way that defeats the model's typicality. Temperature, thinking and "be as different as possible" did little. Nearly every practice is measured on prose, not dialogue, so these transfers are inference.

### Cited Findings

**Voice samples**
- [Measured, Oct 2025–Mar 2026, preregistered] 28 MFA readers and 516 general readers judged passages of up to 450 words in the style of 50 authors. With in-context prompting, MFA readers strongly preferred the human writers (OR 0.16 for fidelity, 0.13 for quality). With ChatGPT fine-tuned on each author's complete works, they preferred the AI (OR 8.16, 1.87). Detectors flagged 97% of prompted output and 3% of fine-tuned. Median fine-tuning cost was $81 per author — [Chakrabarty, Ginsburg, Dhillon](https://arxiv.org/abs/2510.13939).
- [Measured, Sep 2025, GPT-4o and others] Few-shot beat zero-shot on every dataset reported; for example authorship-attribution top-5 on Blog was 39.39 vs 17.22 for GPT-4o. Going from 2 to 10 examples had "very little" effect. Imitation was much weaker for informal writing than for formal, and most output remained detectable by GPTZero. This covers everyday authors' prose, not dialogue — [Wang et al., Catch Me If You Can?](https://arxiv.org/html/2509.14543v1).
- [Measured, 2023, GPT-4] 28 Disco Elysium players compared GPT-4 infills against the designers' lines, in a game-state-driven dialogue system built from about 1.1M words of the game's dialogue. Human writing was significantly preferred — [Akoury, Yang, Iyyer, abstract](https://aclanthology.org/2023.findings-emnlp.151/). A search excerpt gives 64% human vs 23% GPT-4, with the rest ties. It says LLMs did well at persona-driven style transfer, were weaker on logical consistency and narrative grounding, and came closest on interesting options. The PDF would not decode, so those details are unverified — [PDF](https://aclanthology.org/2023.findings-emnlp.151.pdf).
- [Self-report, 2022] 13 professional writers: tools "struggle to preserve style and authorial voice, and they lack deep understanding of story contents" — [Ippolito et al., Wordcraft](https://arxiv.org/abs/2211.05030). This older result is in tension with Akoury's persona-transfer finding. The difference may be model generation, or real samples versus none.
- [Vendor guidance, current models] "Examples are one of the most reliable ways to steer Claude's output format, tone, and structure". Use 3–5 that are relevant and diverse enough not to teach unintended patterns — [Anthropic prompting best practices](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices).

**Editing and separate passes**
- [Measured, 2024–25] Expert preference ranks, lower is better: writer-edited 1.47–1.53, LLM-edited 1.99, raw LLM 2.47–2.55. LLM-edited beat raw at p≈1e-11 to 1e-13; writer-edited beat raw more strongly. The rewriter detects problem spans, then uses a category-specific prompt with a definition and 25 expert examples. Span detection precision was 0.46 overall (experts agree with each other at 0.57) and 0.21 by category (experts 0.23). Agreement among judges was moderate (Kendall's W 0.505) — [Chakrabarty, Laban, Wu](https://ar5iv.labs.arxiv.org/html/2409.14509).
- [Self-report with counts, 2023] Writers kept under 35% of the model's initial draft in 15 of 30 stories. Of the 10 most-retained instruction types, 9 were local rewrites; 7 of the 10 least-retained were planning requests. Feedback was widely valued. One writer called it "most helpful as an editing tool rather than as an idea generator" — [Chakrabarty et al.](https://ar5iv.labs.arxiv.org/html/2309.12570).
- [Self-report with counts, 2022] In Dramatron, eight participants mainly cut or curated and four mainly added. They deleted repetitive dialogue loops, and the plot outline was the least edited level. 13 of 15 answered the survey; 46% were proud of the output. Hierarchical chaining (log line, characters, plot, locations, then dialogue) was built for coherence — [Mirowski et al.](https://ar5iv.labs.arxiv.org/html/2209.14958).

**Many takes and selection**
- [Measured, authors' own, Oct 2025, ICML 2026] Verbalized Sampling asks for several candidates with their probabilities. In creative writing, diversity rose 1.6–2.1×, human evaluation scores rose 25.7%, and more capable models benefited more. It blames typicality bias in human preference data for mode collapse. Abstract-level only — [Zhang et al.](https://arxiv.org/html/2510.01171v2).
- [Measured, June 2026] Sequential "as different as possible" prompting lowered self-similarity but barely improved overall diversity. Temperature had little effect except on one unaligned checkpoint, and Top-P had no consistent effect. Thinking did not help: Qwen3 self-similarity was 89.8% with thinking vs 83.5% without, and OLMo 93.5% vs 89.8%. Persona prompting was not tested. The fetch tool flagged that the paper's text calls the OLMo result "lower" while its numbers show higher — [Thennal DK & Hatzel](https://arxiv.org/html/2606.17350).
- [Practice, no published numbers, Mar 2023] Ghostwriter: writers define a character and an interaction type, it proposes variations, writers choose between pairs and polish, and each choice trains the tool — [Ubisoft](https://news.ubisoft.com/en-us/article/7Cm07zbBGy4Xml6WgYi25d/the-convergence-of-ai-and-creativity-introducing-ghostwriter).

**Character bibles and speech rules**
- [Practice, weak outcome, Mar 2024] For Ubisoft's NEO NPC prototype the narrative team wrote each character's backstory, knowledge base and conversational style with Inworld's tools. Press excerpts called one demo character "pretty on-point" and "the second one not so much", and described "somewhat robotic dialogue". It stayed a prototype. It also ran at runtime, unlike ours. Search excerpts only — [Inworld](https://inworld.ai/blog/ubisoft-neo-npc-prototype), [NVIDIA](https://www.nvidia.com/en-gb/geforce/news/nvidia-ace-gdc-gtc-2024-ai-character-game-and-app-demo-videos), [Tom's Hardware](https://www.tomshardware.com/video-games/ubisoft-nvidia-and-inworld-ai-partnership-to-produce-neo-npc-game-characters-with-ai-backed-responses).
- I found no controlled test of speech-rule bibles against samples, of forbidding quips, or of writing long then cutting.

### Inferences

Verdicts for our prompts. All verdicts are my judgement; the evidence column says how firm the basis is.

| # | Fix | Evidence | Where | Verdict |
|---|---|---|---|---|
| 1 | 3–5 human-written exemplar exchanges per voice, kept separate from the rules | Measured on prose; vendor guidance. Two samples may be nearly enough | Writer, editor; add a voice corpus outside the GDD RAG | **Do first** |
| 2 | Speech rules per speaker instead of one global style. Fragments belong to the Scout and Enforcer only. Fixer in full, unhurried sentences that say "we". Inez in run-on patter that breaks off. Zhou prices things. Okafor in officialese going soft. Extends the existing `text.tone.voices` | Practitioner only; our counts show the failure | `VOICES` in conversations.ts, writer.md | **Do first**, cheap |
| 3 | Rewrite sheets as bare facts: no quotable phrasing, no "let it land", no "in a line". Keep the want, the fear and what's at stake | My reading of our lines; Dramatron p4–p6 | `specs` in conversations.ts | **Do first**, cheap |
| 4 | Remove the steer to sharpness: replace "Better one strong line than three fair ones" with "most lines are ordinary; at most one line may be sharp, and it need not come last" | Opinion; emerging-writer endings complaint | editor.md, writer.md | **Do** |
| 5 | Draft uncapped, then cut and split into bubbles in a later pass | Dramatron cut-heavy editing; no controlled test | Writer draft, new final pass | **Try**; costs one more call |
| 6 | Separate edit passes per fault, each with examples, working on named spans and leaving the rest alone: rhythm and voice, then cut, then facts | Measured improvement over raw, still short of a human. Detection is noisy, so expect misses | Replace the one all-in-one editor | **Do**, after 1–3 |
| 7 | Scenes in sequence with a shared record of what has been said | Dramatron p1; our coffee line, duplicated "What do you want?" | Replace `Promise.all` | **Do**, small code change |
| 8 | Add a mechanical rhythm check to `check()`: per-speaker sentence length, share of three-part lines, share of lines in any 2-word band. Thresholds would be our own starting guesses | Our counts; no outside validation | `check()` | **Do**; it catches the main fault cheaply |
| 9 | More candidates per scene using candidates-with-probabilities | Measured, authors' own, not on dialogue. Three takes "that differ in situation" relies partly on the instruction Thennal found weak | `Takes` schema | **Try** later; it adds variety rather than voice |
| 10 | Human selection over model selection | Ghostwriter practice; typicality bias suggests a model picker favours the typical (inference) | Capstone scenes | **Do** for capstones. Sky's reading time is the cost |
| 11 | Fine-tune on a corpus | Strongest result, but on whole bodies of work. We have no such corpus, and I did not check availability for our model | n/a | **Not now** |
| 12 | Raising temperature or effort | Measured as weak or counterproductive for diversity (open models for thinking) | `effort: 'high'` | **Don't expect gains** from it |

- Keep the rules reviewer [review.md](/Users/sky/Projects/suborned/briefs/085/fable/review.md) as it stands. It is already a separate pass for fact and rule fit and explicitly leaves taste alone, which matches the evidence that facts and voice are different jobs.
- One editor problem deserves a specific note. The editor is told to remove any "number" the sheet doesn't give, yet the writer's colour rule omits numbers from its list. A strict editor would remove "Six years", which is among the better details. LAMP shows lack of specificity is a persistent fault. Narrow the rule to numbers a player plans around.
- The writer and editor share a role in the code (`role: 'dialogue-designer'`), so the editor brings the same habits to the same text. I did not read the role card that is loaded as the system prompt, so its effect on voice is unchecked.

### Gaps
- No study I found measured any of these on game dialogue except Akoury 2023 (GPT-4, infill).
- Hidden Door, Spirit AI and AI Dungeon gave no usable data on process (see the next question and the human-loop question).
- Whether fine-tuning is available for the model the pipeline uses was not checked.

## How do procedural and systemic narrative practitioners keep authored voice, and what should machines not be asked to do?

### Takeaway
They keep voice by making the units human-written and letting the system choose and combine, tied to game state, rather than asking a machine for the voice itself. Nearly all of this predates LLMs, so it is craft opinion rather than evidence about current models.

### Cited Findings
- [Opinion, 2016] Emily Short, crediting Kate Compton's "procedural oatmeal" (quoted through a reader's comment): "it is very easy to pour 10,000 bowls of plain oatmeal." The test is "whether the generation is connected to anything mechanical". Procgen is "a way of designing content" and not a substitute for content design. Authors still write narrative-centric dialogue, jokes, callbacks and the generator's goals; plot generation is much harder — [Bowls of Oatmeal and Text Generation](https://emshort.blog/2016/09/21/bowls-of-oatmeal-and-text-generation/).
- [Practice, 2017] Versu scenes held a body of dialogue "spoken only in that scene", plus parameters, character moods, thought bubbles and endings. Exposition could go in a "memory": narration fired the first time a topic arose, rather than spoken. Her advice: list the beats the player must get, the decisions and the exits, write starter dialogue for them, then grow it by replaying — [Mailbag: Writing for Versu](https://emshort.blog/2017/05/18/mailbag-writing-for-versu/).
- [Opinion, 2018–2020] Short on early models: "generating surface text rather than plot"; "Textual style transfer is a more challenging problem"; of GPT-3 AI Dungeon, "reasonably coherent but often sort of conservative". She works on a "partially human-authored experience" — [emshort.blog AI category](https://emshort.blog/category/ai/). These are old models.
- [Opinion and practice, 2016] Bruno Dias on Voyageur: a tagged grammar choosing phrases by a model of what is described, at mixed granularity, because "Too much granularity, and you end up without enough meaning". He says "Meaning is always created by the reader", and rejects generation as "a weak substitute for authored content" — [Procedural meaning](https://www.gamedeveloper.com/design/procedural-meaning-pragmatic-procgen-in-voyageur).
- [Opinion, 2014] Ken Levine's "Narrative Legos" GDC talk proposed breaking story into small recombinable elements. A search excerpt has him calling it "a back of the napkin thought experiment". It concerns systemic story structure, not text generation. I did not watch the talk or open these pages — [Game Informer](https://gameinformer.com/b/news/archive/2014/03/22/ken-levine-talks-player-participation-and-breaking-away-from-linear-narratives.aspx), [GDC Vault](https://gdcvault.com/play/1021234/Narrative).
- [Practice, Mar 2023] Ghostwriter targets barks; a search excerpt of a VGC report has Ubisoft's Ben Swanson excluding cinematics and lore, and advising developers to build their own models rather than use pre-existing ones. The VGC page returned 403 and was not read — [VGC](https://videogameschronicle.com/news/ubisoft-is-developing-an-ai-tool-that-aims-to-support-scriptwriters).
- [Practice, search excerpts, not opened at source] Hidden Door's Hilary Mason: "We pre-generate, and handwrite in some cases, bits of narrative in the engine that are tropes", with game state held in a database. I could not confirm which page carries the quote; the likely candidates are [Game Developer](https://gamedeveloper.com/programming/hidden-door-ceo-ai-controversy-is-about-value-not-tech) and [PC Gamer](https://pcgamer.com/hidden-door-ai-game-narrative-rpg). The page I did open has her saying "We haven't built a machine to tell stories" — [GamesBeat, 2022](https://gamesbeat.com/hidden-door-reveals-its-ai-powered-narrative-game-building-platform/).

### Inferences
- These practitioners would not ask a model to do the three things we now ask at once: invent the voice, carry the exposition, and supply the wit. Our generator is already sound on the part they stress most, because facts are bound from sim data and the scene is tied to mechanics through `learned` and `opens`.
- Versu's "memory" is a close model for us. `learned` and `opens` already say the plain facts in the game's own voice. If they count towards "clearly explain", the dialogue no longer has to say "If the Office hears about Okafor, we're both finished." This touches Sky's request that the conversation itself clearly explains, so it is his call. Our giving the last-beat summary to the Scout is already in this spirit.
- Allowing silent stage business in the schema would help. `Beat` holds only speaker and line, so all subtext must be spoken, and that pushes towards the too-articulate tell. Versu's moods and thought bubbles are the precedent. It would need a spec change from Glyph and Sky's agreement.
- Dias's granularity warning suggests where to vary: generate whole turns from authored voice, not word swaps.

### Gaps
- I could not find Emily Short's published view on LLMs. Her [Nov 2023 Utah talk](https://games.utah.edu/news/guest-lecture-building-responsive-npcs-in-the-age-of-gpt/) promises "what the new generation of LLMs can and cannot offer", but the page only announces it. Her blog's AI category listing ends at 2020.
- Kate Compton's own [post](https://galaxykate0.tumblr.com/post/139774965871/so-you-want-to-build-a-generator) returned 403, so she is cited only through Short.
- Nothing usable found on Spirit AI's Character Engine.
- A search excerpt reports that Versu conversations "tended to exhaust one topic before moving mechanically to another" until salient-topic sets were added. That bears on our ping-pong fault, but I could not confirm which page says it, so don't rely on it yet. Possible sources: [Short's Versu post](https://emshort.blog/2013/02/26/versu/) or [IF50](https://if50.substack.com/p/2013-a-family-supper).

## Do hard line-length limits harm voice, and is negative or positive instruction better?

### Takeaway
No study I found tests word caps on voice. Our counts suggest the cap is less to blame than the combination of a target of "10 or fewer", a one-register tone line, and too many facts per word; together these produce the clipped lines. On negation, evidence that "don't" lists backfire is real but narrow. Moving bans into code and stating the wanted behaviour positively is low-risk either way.

### Cited Findings
- [Measured, Jan 2026, one open model, single-word bans, single author] With 2,500 prompts and 40,000 samples, violations rose from 9.1% to 46.3% as the banned word's natural likelihood rose. 87.5% of violations were "priming" failures, summarised as "the very act of naming a forbidden word primes the model to produce it". The paper does not claim this generalises, and it does not test style — [Rana, Semantic Gravity Wells](https://arxiv.org/html/2601.08070v1).
- [Measured, June 2026, frontier models] Negative prompting did little for narrative diversity — [Thennal DK & Hatzel](https://arxiv.org/html/2606.17350). The tested form was "as different as possible" against prior stories, not a list of don'ts.
- [Vendor guidance, current models; framed around output formatting] "Tell Claude what to do instead of what not to do". The style of a prompt "might influence Claude's response style", so match the prompt to the output you want. Explaining why an instruction matters helps — [Anthropic](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices).
- [Weak] A [Zenodo record](https://zenodo.org/records/18599526) claims a breakdown above about 40% negative instructions. Its method could not be verified from the excerpt, so I would not use the number.
- [Opinion] Wikipedia's guide reports recent models already favouring short sentences, per the first question — [Signs of AI writing](https://en.wikipedia.org/wiki/Wikipedia:Signs_of_AI_writing). If that is true of our model too, our brief pushes the same way as its habit.
- [Measured, indirect] Lack-of-specificity edits usually make text longer — [Chakrabarty, Laban, Wu](https://ar5iv.labs.arxiv.org/html/2409.14509). So some real fixes need room.

**Our inputs [Local count]**
- [writer.md](/Users/sky/Projects/suborned/briefs/085/fable/writer.md) quotes eight bad lines and no good ones, and [editor.md](/Users/sky/Projects/suborned/briefs/085/fable/editor.md) quotes none of either. Some of the bad examples, such as "Then I'll go. After the shift.", are in the same clipped register as the output.
- The brief says "at most 12 words; aim for 10 or fewer. One sentence or two short ones". Output: 36 of 50 lines at 9–10 words, and one line at 11. No line reaches 12.
- Scenes average about 72 words (435 words over 6 scenes), which must carry who they are, the stakes, the fact, the next step, a detail of the city, and temperament.
- The contract's world line itself says "Speech is plain, short and spoken" — [text.json](/Users/sky/Projects/suborned/contracts/missions/handover/text.json).
- `text.tone.voices.waitress` says "the lender" while the brief and `JARGON` ban it. The contract and the check disagree.

### Inferences
- The 12-word cap does not by itself force this register: a single 12-word sentence and a 2-word line both pass, and such lines are rare in the samples. The likely cause is compression. Too much content per bubble makes the writer drop subjects and articles, and the result is fragments. This has not been tested; a comparison run would test it.
- The cap can stay as a UI rule for one bubble while its effect on voice is removed:
  - Budget words per scene instead of steering each line towards 10, so the same 72 words could hold a 3-word line and a 20-word turn.
  - Allow a turn to run over two consecutive bubbles. Nothing in `check()` forbids it, and call-inez already does it, though `maxBeats` counts each bubble.
  - Replace "aim for 10" with a per-speaker range.
  This respects Sky's "the reading, it's too much", since the total needn't grow.
- Our main bans are already enforced in `check()` (exclamation marks, line length, the avoid list and most design words), so the prompt could stop naming them. Word lists don't belong in the writer's attention; give the positive voice instead and let the fix loop catch misses. `JARGON` omits some brief words (need, target, order, contact, cell) probably because they are ordinary words, so keep those few in the prompt or review for them.
- The brief's closing-quip warning, "Avoid the fortune-cookie line and the closing quip", plausibly primes in the same way, though Rana only tested single words. Replace it with a positive: end on an unfinished action, or on someone returning to work. Rephrase the "What goes wrong" section as rules, and supply paired before-and-after examples as the LAMP rewriter does, not bad lines alone.
- Anthropic's point about prompt style applies: our sheets are terse ("learned: 2 lines (the debt; the lover)"), and the retrieved GDD text adds design prose. Prose sheets and voice samples near the task may help, but this guidance was framed around formatting and is untested for voice.

### Gaps
- No controlled study on word or line caps and voice or creative quality.
- No controlled study of negative versus positive instruction on creative writing quality. Rana covers word bans in one model; Thennal covers one form of negative prompt.
- Whether the Wikipedia-reported shift to short sentences applies to Fable: not known.

## Should the human step change: a lead writer authoring voice bibles and exemplar scenes first, with generation filling out variants?

### Takeaway
Yes, on the balance of evidence, though no studio has published a controlled result for this. Every serious production account I found has humans setting the voice and selecting or editing, with generation confined to variants and low-stakes lines. The Handover's six scenes are the capstone, which is the kind of content those teams keep human.

### Cited Findings
- [Measured] Readers preferred writer-edited text over LLM-edited over raw, and the human edit was the largest single gain — [Chakrabarty, Laban, Wu](https://ar5iv.labs.arxiv.org/html/2409.14509). A search excerpt also reports that human editing or co-creation substantially increases diversity. I could not confirm which paper says it; a candidate is the 2026 survey [Magic, Madness, Heaven, Sin](https://arxiv.org/pdf/2604.01504), which I did not open.
- [Measured] Players preferred designers' lines to GPT-4 infills even when the model was given the game's real dialogue to work from — [Akoury et al., 2023](https://aclanthology.org/2023.findings-emnlp.151/).
- [Practice, no numbers] Ghostwriter: humans define, pick and polish, for barks, so writers have time for core plot — [Ubisoft, 2023](https://news.ubisoft.com/en-us/article/7Cm07zbBGy4Xml6WgYi25d/the-convergence-of-ai-and-creativity-introducing-ghostwriter). Search excerpts of March 2023 coverage report developers objecting that editing could take longer than writing, and that barks are where writers start. I did not open these or confirm which page says which; examples are [The Decoder](https://the-decoder.com/ubisofts-ghostwriter-aims-to-make-life-easier-for-video-game-writers/) and [TrueAchievements](https://www.trueachievements.com/n53287/ubisoft-ghostwriter-ai-xbox). I found no later report on production use.
- [Self-report] The student project behind Remains found some edited messages a significant improvement but needing more effort than writing from scratch; excerpt only — [Parsons, 2023](https://parsons.edu/dt-2023/publication_entry/remains-experiments-with-procedural-dialogue/). A playwright study cited in a search excerpt reports writers preferring tools that flag inconsistencies over ones that rewrite; source page not confirmed.
- [Practice, weak outcome] Ubisoft's narrative team wrote each NEO NPC's backstory and style, and press excerpts still called the early output somewhat robotic, though that was a live prototype (sources in the second question).
- [Measured, strong, but not a studio] A model trained on an author's whole body of work beat expert human imitators, while prompting did not, for expert readers. The authors note the cost excludes the work of making output publishable — [Chakrabarty, Ginsburg, Dhillon](https://arxiv.org/abs/2510.13939). General readers already favoured prompted AI on quality (OR 1.82), while experts did not.

### Inferences
A written bible alone doesn't look sufficient, going by NEO NPC. And the expert/general split in the 2025 study suggests a general-reader standard may not satisfy Sky, who is judging this closely.

Proposed process, my judgement from the above:
1. **A human writes the voice first.** Sky or a hired writer hand-writes about 2–5 short exemplar exchanges per main voice (Inez, Fixer, Zhou, Okafor, Scout), a page of speech rules, and two complete scenes. The few-shot evidence suggests a small amount is enough to start, about 200–400 words, not a large bible. The bar is that Sky says "yes, that's them".
2. **Keep capstones human-final.** The model drafts options; a human writes the last version of the Handover's six scenes. Those then join the exemplars.
3. **Generate the variants.** Generated missions take new situations, facts bound from the sim, exemplars retrieved by speaker and scene kind, and the staged passes. Human review is by sampling, plus the mechanical rhythm check.
4. **Use models for what they did well:** fact and rule review (already `review.md`), flagging continuity, and feedback. Local rewrites were the requests writers kept most in the 2023 study. Unassisted scene drafting is the weakest use.

Points for Sky to decide:
- The project tiebreaker "systems > scripts (nothing hand-authored)" appears to clash with hand-writing — [CLAUDE.md](/Users/sky/Projects/suborned/CLAUDE.md). Exemplars used only as generator input keep the game generated on a literal reading, but hand-final capstones wouldn't. This is a design decision.
- Who is the writer? A model can't fill the lead role under this evidence, and Sky's time is limited. A short paid commission for a voice pack may be cheaper than continuing prompt iteration. I found no cost figures for games at this scale.
- Exemplars drawn from published plays or scripts would be a shortcut, but I did not research copyright. The cited 2025 study is about training on copyrighted books, and I did not read its legal discussion.

A cheap way to test before committing: without changing anything else, compare the current brief with exemplars plus stripped sheets on two scenes, and have Sky read them blind. This would need his approval for live calls and is not done here. The result would be one reader's judgement, which is the standard that counts here.

### Gaps
- No studio has published a controlled comparison of lead-writer-first with generate-first that I could find. All studio accounts are practice reports, often from announcements or vendors, and most are from 2023–24.
- No published post-mortem on Hidden Door's or Inworld's writing process. Inworld is runtime and of limited relevance to us.
- No time or cost data on editing generated dialogue vs writing it, beyond anecdotes.
- [The AI Fiction Paradox](https://arxiv.org/pdf/2603.13545) and [Fabula](https://arxiv.org/pdf/2606.14411) appeared in search and may be relevant, but were not opened. [AI Fiction in the Wild](https://arxiv.org/html/2606.22748) was opened; it has no dialogue metrics, only usage data and illustrative melodrama, so it doesn't bear on these questions.
