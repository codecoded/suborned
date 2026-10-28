using System.Globalization;

namespace Suborned.World.Review
{
    /// <summary>The ffmpeg settings every review video uses (brief 075 item 6, Latch, 2026-10-29), so a reel is judged on the render, not
    /// on the encode. Raw RGB24 frames (bottom row first) in; out:
    /// - the share copy (&lt;out&gt;.mp4): H.264 High, yuv420p, CRF 14, preset slow, tune grain (keeps the sky's dither and wall texture
    ///   that CRF 18 / tune none smoothed into bands and smears), aq-mode 3 (more bits to flat gradients), tagged BT.709 limited range;
    /// - optionally the review master (&lt;out&gt;-master.mp4): H.264 High 4:4:4, yuv444p, CRF 8, the same tuning: full chroma for thin
    ///   pinstripes, and the source the side-by-side composites are cut from (plays in ffmpeg/VLC/Chrome, not QuickTime).
    /// The RGB to YUV step is explicit: lanczos (the 2x supersample down to the output size in the same pass), accurate rounding, full
    /// chroma interpolation, BT.709 matrix. ffmpeg's default would convert with the BT.601 matrix and leave the file untagged, so players
    /// that assume BT.709 for HD shift the colours (the old DecoKit reel and the 074/075 reels before this all have that fault).</summary>
    public static class ReviewVideo
    {
        public const string Ffmpeg = "/opt/homebrew/bin/ffmpeg";
        public const int ShareCrf = 14, MasterCrf = 8;
        public const string Preset = "slow";

        /// <summary>The conversion of a flipped RGB frame to YUV at the output size, BT.709 limited range; setparams stamps the frames
        /// (ffmpeg 8 writes the stream's colour tags from the frames, so the -color_* options alone leave primaries and transfer unset).</summary>
        /// Reel banding (Glint, brief 075, 2026-10-29): two steps, so the dither comes after the downscale. Step 1 is the lanczos
        /// downscale into 16-bit RGB (rgb48le): the 2x frame's four 8-bit samples each carry the camera's own dither, so their average
        /// holds the sky gradient between 8-bit steps. Step 2 is the BT.709 conversion from that 16-bit source to 8-bit YUV, and
        /// swscale dithers when it reduces a 16-bit source (it never does from rgb24, which is why one step rounded the
        /// average back into bands). The synthetic test cut low-frequency banding 0.124 to 0.020 LSB rms (briefs/075/parity/reelband).
        public static string Scale(int w, int h) =>
            $"scale={w}:{h}:flags=lanczos+accurate_rnd+full_chroma_int+full_chroma_inp,format=rgb48le," +
            $"scale={w}:{h}:flags=lanczos+accurate_rnd+full_chroma_int+full_chroma_inp:in_range=pc:out_range=tv:out_color_matrix=bt709," +
            "setparams=range=tv:colorspace=bt709:color_primaries=bt709:color_trc=bt709";

        /// <summary>The H.264 encoder options for one output (pix: yuv420p share, yuv444p master).</summary>
        public static string Encode(string pix, int crf, string preset = Preset) =>
            $"-c:v libx264 -preset {preset} -tune grain -crf {crf} -x264-params aq-mode=3 -pix_fmt {pix} " +
            "-colorspace bt709 -color_primaries bt709 -color_trc bt709 -color_range tv -movflags +faststart";

        /// <summary>The full ffmpeg argument line: raw rgb24 inW x inH at fps on stdin, the share mp4 at outW x outH, and the master beside it
        /// when master is set.</summary>
        public static string Args(int inW, int inH, int outW, int outH, int fps, string share, bool master, int shareCrf = ShareCrf, string preset = Preset)
        {
            var input = $"-hide_banner -loglevel error -y -f rawvideo -pix_fmt rgb24 -s {inW}x{inH} -r {fps.ToString(CultureInfo.InvariantCulture)} -i - ";
            var s = Scale(outW, outH);
            if (!master)
                return input + $"-vf \"vflip,{s},format=yuv420p\" " + Encode("yuv420p", shareCrf, preset) + $" -r {fps} \"{share}\"";
            var masterFile = MasterPath(share);
            return input + $"-filter_complex \"[0:v]vflip,split=2[a][b];[a]{s},format=yuv420p[s];[b]{s},format=yuv444p[m]\" " +
                   $"-map \"[s]\" {Encode("yuv420p", shareCrf, preset)} -r {fps} \"{share}\" " +
                   $"-map \"[m]\" {Encode("yuv444p", MasterCrf, preset)} -r {fps} \"{masterFile}\"";
        }

        public static string MasterPath(string share) =>
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(share) ?? "", System.IO.Path.GetFileNameWithoutExtension(share) + "-master.mp4");
    }
}
