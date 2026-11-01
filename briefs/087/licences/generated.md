# Generated sounds: licence record (read 2026-11-01)

**Model:** Seed Audio 1.0, `bytedance:seed-audio@1.0`, called through Runware. Runware's own model search lists it as `source: official`, creator ByteDance. Every take is in `paid/087-sound/ledger.jsonl` with its prompt, cost and sha256.

**Runware Terms** (https://runware.ai/terms):
- Section 2: "As between you and Runware, you own your Outputs", subject to third-party rights, the Model Licence and the Terms. Partner Models are "models licensed to Runware under a commercial agreement with the model owner".
- Section 10: "You may use the Services for lawful commercial purposes, including generating Outputs for commercial use." For Partner Models, Runware has obtained rights that "permit you to use Outputs generated using those Partner Models for commercial purposes."
- Section 12: outputs may "not meet the requirements for human authorship" needed for copyright. Others may be free to reuse these sounds.

**One inference for Sky to know.** No page I could read says outright that Seed Audio is a Partner Model. I take it as one because it's closed, listed `official`, and is neither an open-source release nor a community upload. Sky can get this confirmed by Runware if he wants it certain before release.

# Library sounds
**Kenney**, CC0 1.0: https://kenney.nl/support · https://creativecommons.org/publicdomain/zero/1.0/
- `camera_click`: RPG Audio, `metalLatch.ogg`, https://kenney.nl/assets/rpg-audio (`kenney-rpg-audio.txt`)
- `foot_concrete_1`, `foot_concrete_2`: Impact Sounds, `footstep_concrete_000.ogg`, `footstep_concrete_001.ogg`, https://kenney.nl/assets/impact-sounds (`kenney-impact-sounds.txt`)

# Looked at and left out
- **Sonniss #GameAudioGDC** (https://sonniss.com/gdc-bundle-license/, version 2.0): fine in a finished game, no attribution. But it bars distributing the sounds as sound effects, edited or not, and **this repo is public** (`gh repo view`: PUBLIC), so raw clips under `unity/Assets` could breach it. Usable later only from a git-ignored folder, as the kit is.
- **ElevenLabs Sound Effects v2 on fal:** the model page carries a "Commercial use" tag, but fal's terms grant "personal or internal business use" and don't address output ownership. That isn't clear enough for a commercial game, so no call was made.
- **Mirelo SFX 1.5 on Runware:** tried and not used. Most one-shots buried in a noise floor, and no tonal partials in the chime takes.

# Music sketches (read 2026-11-01; job `paid/087-music/`)
**All four picked sketches:** ACE-Step v1.5 XL Base, `runware:ace-step@v1.5-xl-base`, called through Runware.
- Repo licence: MIT (https://raw.githubusercontent.com/ace-step/ACE-Step-1.5/main/LICENSE, "Copyright (c) 2026 ACEStep"). That notice is on the code; the model card lists the model as MIT too (read through a search summary).
- Output: the Hugging Face model card (https://huggingface.co/ACE-Step/Ace-Step1.5) says generated music may be used commercially. I read it through a search summary, not the full card, and not a card for the XL variant.
- Runware terms: open-source models carry their own licence, and commercial use "may be permitted, restricted or prohibited depending on the Model Licence". Runware doesn't vouch for that information, so MIT is what this rests on.
- As with all generated sound: outputs may have no copyright, so others may be free to reuse them.

**MiniMax Music 2.6 (not in the reel):** a Partner-type model on Runware by the same inference as Seed Audio, so Runware's commercial clause should apply. MiniMax's own music terms (https://www.minimax.io/audio/doc/terms-of-service-music.html) require "clearly labeling AI-generated Output Content" and bar removing its identifiers, and don't say who owns the output. Those terms are for its own music feature; whether they reach calls through Runware I couldn't tell. Less clear than ACE-Step, and a reason to prefer ACE-Step on top of the measures.

# Layer test (read 2026-11-02; job `paid/087-music/`, pass 2)
- **Long piece (`text_t2`):** ACE-Step from a text prompt, no MiniMax audio in it. The prompt reuses our own teaser wording. Licence position as for the sketches above: this is the clean route.
- **Discarded covers and extend (`cover_groove_*`, `extend_section`, `arr_*`):** these took MiniMax's teaser, or the long piece, as input audio. Anything derived from the teaser would carry MiniMax's terms as well as ACE-Step's. Those terms don't say who owns output, and ask for AI labelling. None of the teaser-derived takes is in the reel's arrangements.
- **Stems (Demucs, `htdemucs`):** the code is MIT. **The pretrained weights are not clearly licensed for commercial use.** Hugging Face discussions report the maintainer saying they fall outside the MIT licence and are for research (https://huggingface.co/adefossez/HTDemucs/discussions/1, https://huggingface.co/adefossez/HTDemucs-ft/discussions/1), and they were trained on MUSDB18-HQ, a dataset with research-oriented terms. I read these second-hand, not the original issues. Whether that reaches stems we make with them is unsettled. So `calm`, `tension` and `fight` are a **test, not shippable** until Sky decides, and they should stay out of a commit to this public repo for now.
