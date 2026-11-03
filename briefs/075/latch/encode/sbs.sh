#!/bin/sh
# Brief 075 item 6 (Latch): an old-against-new side by side at 100% (each reel 1920 x 1080, so 3840 x 1080), at ReviewVideo's share
# settings (CRF 14, slow, tune grain, aq-mode 3, yuv420p, BT.709 tagged). The old reel is untagged BT.601 (its encoder's default);
# ffmpeg reads untagged as BT.601 and converts both to RGB before the stack, so neither side is shifted.
# Usage: sh briefs/075/latch/encode/sbs.sh <old.mp4> <new-master.mp4 or new.mp4> <out.mp4>
set -e
F=/opt/homebrew/bin/ffmpeg
S='scale=3840:1080:flags=lanczos+accurate_rnd+full_chroma_int+full_chroma_inp:in_range=pc:out_range=tv:out_color_matrix=bt709,setparams=range=tv:colorspace=bt709:color_primaries=bt709:color_trc=bt709'
"$F" -hide_banner -loglevel error -y -i "$1" -i "$2" -filter_complex \
  "[0:v]scale=1920:1080:flags=lanczos+accurate_rnd+full_chroma_int:in_color_matrix=bt601:in_range=tv,format=gbrp[l];[1:v]scale=1920:1080:flags=lanczos+accurate_rnd+full_chroma_int:in_color_matrix=bt709:in_range=tv,format=gbrp[r];[l][r]hstack=inputs=2:shortest=1,$S,format=yuv420p[v]" \
  -map "[v]" -c:v libx264 -preset slow -tune grain -crf 14 -x264-params aq-mode=3 -pix_fmt yuv420p \
  -colorspace bt709 -color_primaries bt709 -color_trc bt709 -color_range tv -movflags +faststart "$3"
