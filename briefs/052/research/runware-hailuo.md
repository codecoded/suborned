# Runware MiniMax Hailuo research (2026-10-23, Forma)
Sources: https://runware.ai/docs/models/minimax-hailuo-2-3 (fetched 2026-10-23) and https://runware.ai/pricing (via web search).
- Model search (Runware modelSearch, free): minimax:4@1 Hailuo 2.3, minimax:4@2 Hailuo 2.3 Fast, minimax:3@1 Hailuo 02.
- Hailuo 2.3 (minimax:4@1): taskType videoInference; duration 6 or 10 s; fixed 25 fps; 1366x768 (768p) and 1920x1080 (1080p), 16:9 only; 1080p is 6 s only; frameImages supports the first frame only (frame "first" or 0); width/height only when no frameImages; providerSettings.minimax.promptOptimizer on/off (affects reproducibility); about 2.5 to 3.5 min per clip.
- Prices: 768p 6 s $0.28, 768p 10 s $0.56, 1080p 6 s $0.49. Fast: 768p 6 s $0.19, 1080p 6 s $0.33.
- Choice: Hailuo 2.3 at 1080p 6 s (highest resolution), three shots of 6 s = 18 s, trimmed to 15 s; no 10 s clips (they cap at 768p).

## Seedance (added 2026-10-23 for the fallback)
Sources: https://runware.ai/docs/models/bytedance-seedance-2-0 and https://runware.ai/docs/models/bytedance-seedance-2-5 (fetched 2026-10-23); model search lists bytedance:seedance@2.5, 2.0, 2.0-fast, 2.0-mini, 1.5-pro and 1.0-pro.
- Seedance 2.0 (bytedance:seedance@2.0): 4 to 15 s, 480p/720p/1080p/4k, $0.40 per second at 1080p (no separate audio price listed), settings.audio on/off, inputs.referenceImages (1 to 9) or inputs.frameImages (first/last), never both.
- Seedance 2.5 (bytedance:seedance@2.5): 4 to 30 s, 1080p $0.614 per second, up to 30 reference images. Not used.
- Found by trial: Seedance moderates the input image ("may contain real person") on some of our painted people (the Fixer and Enforcer wide frames were rejected, the Scout and Tech passed). Hailuo took no objection.
- Hailuo without width/height renders 1366x768 (about 1364x768 out) at $0.28; send width 1920 and height 1080 with the first frame image for 1080p at $0.49.

## MiniMax H3 (checked 2026-10-23, after Sky's question and Aster's note)
Sources: Runware modelSearch (free) and https://runware.ai/docs/models/minimax-h3 (fetched 2026-10-23).
- Runware has it: `minimax:h3@0` (H3), `minimax:h3@fast`, `minimax:h3@max`, `minimax:h3@max-turbo` (also Seedance 2.5, Kling 3.0, Veo 3.1, Wan 3.0, Sora 2 per Aster's search). Hailuo 2.3 (`minimax:4@1`) is the older model.
- H3: 4 to 15 s, 768p ($0.08/s) or 1440p/"2K" ($0.13/s, 2560x1440 for 16:9), first and/or last frame (frameImages, 1 or 2), 1 to 9 reference images, reference video and audio inputs, native synchronized audio per the page (no audio switch documented), settings.promptExpansion only. A 10 s 1440p clip would cost $1.30 against $0.49 for a 6 s 1080p Hailuo 2.3 clip and about $2 for Seedance 4 s: cheaper per second and longer.
- Not tried: `gen.py` supports `--model h3` (first, or first+last frame with `--mode fl`), but Runware refused every video request with insufficientCredits (minimum balance 3 credits; the account holds $2.61), so no H3 clip exists yet, and moderation, audio and quality are unchecked.
- Runware's video requests need a minimum balance (about 3 credits) even when the clip costs less; Seedance and Hailuo requests also reserve more than they cost.
