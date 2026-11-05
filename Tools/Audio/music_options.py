"""
Renders alternative music loops to Logs/Audio for auditioning, without touching the game's audio.
Uses the instruments in make_sounds.py. Run from the repository root:  python Tools/Audio/music_options.py
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_sounds as ms  # noqa: E402

PREVIEW = os.path.join(ms.ROOT, "Logs", "Audio")


def loop_from(parts, length, wet, room_seconds):
    song = ms.room(ms.mix(*parts), seconds=room_seconds, wet=wet, tone=5000, seed=21)
    n = int(length * ms.SR)
    loop = song[:n].copy()
    tail = song[n:]
    loop[: len(tail)] += tail
    return ms.level(loop, -21, peak_db=-2)


def breezy():
    """'Breezy': 112 bpm in F major (I-V-vi-IV), bouncy plucked keys on the off-beats, octave bass, claps on 2 and 4,
    and a glockenspiel hook that answers itself every two bars."""
    bpm = 112
    beat = 60 / bpm
    bar = beat * 4
    bars = 16
    prog = [("F2", ["F4", "A4", "C5"]), ("C2", ["E4", "G4", "C5"]), ("D2", ["D4", "F4", "A4"]), ("A#1", ["D4", "F4", "A#4"])]
    hook = [["A5", None, "C6", None, "A5", "G5", "F5", None], ["G5", None, None, "E5", "G5", None, None, None],
            ["A5", None, "C6", None, "D6", "C6", "A5", None], ["A#5", None, "A5", None, "G5", None, None, None]]
    parts = []
    for b in range(bars):
        root, chord = prog[b % 4]
        at = b * bar
        for e in range(8):
            if e % 2 == 1:
                for n in chord:
                    parts.append((at + e * beat / 2, ms.keys(ms.note(n), 0.35, decay=9) * 0.16))
        for e, octv in enumerate([1, 2, 1, 2]):
            parts.append((at + e * beat, ms.lowpass(ms.keys(ms.note(root) * octv, 0.4, decay=7), 600) * 0.36))
        for k in range(4):
            if k % 2 == 0:
                parts.append((at + k * beat, ms.kick() * 0.45))
            else:
                parts.append((at + k * beat, ms.snap(b * 4 + k) * 0.26))
        for e in range(8):
            hit = ms.highpass(ms.noise(0.035, 900 + b * 8 + e), 7500) * np.exp(-ms.t_axis(0.035) * 90)
            parts.append((at + e * beat / 2, hit * (0.05 if e % 2 else 0.028)))
        if b >= 4:
            for e, n in enumerate(hook[b % 4]):
                if n:
                    parts.append((at + e * beat / 2, ms.glock(ms.note(n), 0.8) * 0.2))
    return loop_from(parts, bar * bars, 0.16, 1.0)


def sunny_lofi():
    """'Sunny lo-fi': 84 bpm, warm seventh chords (Cmaj7-Am7-Dm7-G7) on soft keys, a lazy swung bass, gentle kick and
    snap, and a sparse glockenspiel line."""
    bpm = 84
    beat = 60 / bpm
    bar = beat * 4
    bars = 16
    swing = beat * 0.16
    prog = [("C2", ["E4", "G4", "B4", "D5"]), ("A1", ["C4", "E4", "G4", "B4"]),
            ("D2", ["C4", "F4", "A4", "D5"]), ("G1", ["B3", "F4", "A4", "D5"])]
    line = [["E5", None, None, "G5", None, None, "B5", None], [None, None, "A5", None, "G5", None, "E5", None],
            ["F5", None, None, "A5", None, "C6", None, None], ["B5", None, "A5", None, "G5", None, "D5", None]]
    parts = []
    for b in range(bars):
        root, chord = prog[b % 4]
        at = b * bar
        for i, n in enumerate(chord):
            parts.append((at + i * 0.012, ms.keys(ms.note(n), bar * 1.05, decay=0.9) * 0.17))
            parts.append((at + beat * 2.5 + swing + i * 0.012, ms.keys(ms.note(n), beat * 1.6, decay=2.4) * 0.09))
        for offset, octv, vel in ((0, 1, 0.42), (beat * 1.5 + swing, 1, 0.26), (beat * 2, 2, 0.24), (beat * 3.5 + swing, 1, 0.22)):
            parts.append((at + offset, ms.lowpass(ms.keys(ms.note(root) * octv, 0.6, decay=4), 450) * vel))
        parts.append((at, ms.kick() * 0.4))
        parts.append((at + beat * 2.5 + swing, ms.kick() * 0.28))
        parts.append((at + beat, ms.lowpass(ms.snap(300 + b), 3500) * 0.22))
        parts.append((at + beat * 3, ms.lowpass(ms.snap(400 + b), 3500) * 0.22))
        for e in range(8):
            t = at + e * beat / 2 + (swing if e % 2 else 0)
            hit = ms.highpass(ms.noise(0.03, 700 + b * 8 + e), 8000) * np.exp(-ms.t_axis(0.03) * 100)
            parts.append((t, hit * (0.03 if e % 2 else 0.018)))
        if b >= 4:
            for e, n in enumerate(line[b % 4]):
                if n:
                    parts.append((at + e * beat / 2 + (swing if e % 2 else 0), ms.glock(ms.note(n), 1.0) * 0.16))
    return loop_from(parts, bar * bars, 0.24, 1.6)


def write_preview(name, x):
    import soundfile as sf
    os.makedirs(PREVIEW, exist_ok=True)
    path = os.path.join(PREVIEW, name + ".ogg")
    x = ms.highpass(x, 25)
    with sf.SoundFile(path, "w", ms.SR, 1, format="OGG", subtype="VORBIS") as f:
        for i in range(0, len(x), ms.SR // 4):
            f.write(x[i: i + ms.SR // 4].astype(np.float32))
    print(f"{path}  {len(x) / ms.SR:.1f}s")


if __name__ == "__main__":
    write_preview("music-option-A-breezy", breezy())
    write_preview("music-option-B-sunny-lofi", sunny_lofi())
