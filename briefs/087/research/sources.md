# Sources behind the pack (Chime, 2026-11-01)
- Runware audio models and schemas: https://runware.ai/audio-generation-api · https://runware.ai/docs/models/bytedance-seed-audio-1-0 · https://runware.ai/docs/models/mirelo-sfx-1-5 · https://runware.ai/models/mirelo-sfx-1-5
- Runware terms: https://runware.ai/terms
- Mirelo terms (paid plans commercial, free plan not): https://mirelo.ai/terms
- Kenney licence and packs: https://kenney.nl/support · https://kenney.nl/assets/rpg-audio · https://kenney.nl/assets/impact-sounds
- Sonniss bundles and licence: https://sonniss.com/gameaudiogdc/ · https://sonniss.com/gdc-bundle-license/
- fal ElevenLabs SFX (schema, price, terms; not used): https://fal.ai/models/fal-ai/elevenlabs/sound-effects/v2 · https://fal.ai/terms

What shaped the work: the Sonniss bar on distributing sounds, against a public repo; fal's unclear output terms; Seed Audio's clean one-shots against Mirelo's noise floor (`out/scratch/chime/sheets/`, take sheets from `briefs/087/chime/sheet.py`).

Music: https://runware.ai/docs/models/ace-step-v1-5-xl-turbo · https://runware.ai/docs/models/minimax-music-2-6 · https://raw.githubusercontent.com/ace-step/ACE-Step-1.5/main/LICENSE · https://huggingface.co/ACE-Step/Ace-Step1.5 · https://www.minimax.io/audio/doc/terms-of-service-music.html

What shaped the music: ACE-Step takes tempo, key and metre as settings, MiniMax only reads them in the prompt; on our takes MiniMax ran past the length asked and drifted in tempo (`briefs/087/music-measures.json`).
