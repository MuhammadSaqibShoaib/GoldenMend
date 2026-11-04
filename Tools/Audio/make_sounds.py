"""
Builds Golden Mend's sound effects and music into Assets/_Game/Content/Audio.

Physical sounds (ceramic clinks, coins) start from Kenney's CC0 recordings in Tools/Audio/source and are cleaned up:
mono, trimmed so the hit starts on the first sample, filtered, given one shared small-room reverb and matched in
loudness. The rest is synthesised here in C major so it agrees with the pitched lock chain in FeedbackService:
wooden taps for buttons and popups, the gold-seal chime, stars, glockenspiel-and-keys jingles and a light pop
music loop.

Run from the repository root:  python Tools/Audio/make_sounds.py
Needs numpy, scipy and soundfile.
"""
import os

import numpy as np
import soundfile as sf
from scipy.signal import butter, fftconvolve, resample_poly, sosfilt

SR = 44100
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SOURCE = os.path.join(ROOT, "Tools", "Audio", "source")
OUT = os.path.join(ROOT, "Assets", "_Game", "Content", "Audio")


def note(name):
    """'C5' -> Hz. Sharps only ('F#4')."""
    names = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"]
    pitch, octave = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((names.index(pitch) + 12 * (octave + 1) - 69) / 12)


def t_axis(seconds):
    return np.arange(int(SR * seconds)) / SR


# ---------------------------------------------------------------- processing

def load(name):
    x, sr = sf.read(os.path.join(SOURCE, name + ".ogg"))
    if x.ndim > 1:
        x = x.mean(axis=1)
    if sr != SR:
        x = resample_poly(x, SR, sr)
    return x


def highpass(x, hz, order=2):
    return sosfilt(butter(order, hz, "highpass", fs=SR, output="sos"), x)


def lowpass(x, hz, order=2):
    return sosfilt(butter(order, hz, "lowpass", fs=SR, output="sos"), x)


def trim(x, lead_db=-40, tail_db=-55, keep=0.35, fade=0.04):
    """Cuts the silence before the hit (so a tap sounds instantly) and the tail after `keep` seconds or the noise floor."""
    a = np.abs(x)
    peak = a.max()
    start = max(0, int(np.argmax(a > peak * 10 ** (lead_db / 20))) - int(0.001 * SR))
    end = len(a) - int(np.argmax(a[::-1] > peak * 10 ** (tail_db / 20)))
    end = min(end, start + int(keep * SR))
    y = x[start:end].copy()
    n = min(int(fade * SR), len(y) // 2)
    y[-n:] *= np.linspace(1, 0, n) ** 2
    y[: int(0.001 * SR)] *= np.linspace(0, 1, int(0.001 * SR))
    return y


def room(x, seconds=0.35, wet=0.12, tone=5000, seed=1):
    """A small soft room, the same for every effect, so recordings and synths sit in one space."""
    rng = np.random.default_rng(seed)
    t = t_axis(seconds)
    ir = rng.standard_normal(len(t)) * np.exp(-6.9 * t / seconds)
    ir = lowpass(ir, tone)
    ir /= np.sqrt((ir ** 2).sum())
    tail = fftconvolve(x, ir)
    y = np.zeros(len(tail))
    y[: len(x)] += x
    return y * (1 - wet) + tail * wet * 1.4


def level(x, rms_db, peak_db=-1.0):
    """Matches loudness over the audible part, then keeps peaks under peak_db with a soft limiter."""
    a = np.abs(x)
    active = x[a > a.max() * 0.03]
    rms = np.sqrt((active ** 2).mean())
    y = x * (10 ** (rms_db / 20) / rms)
    ceiling = 10 ** (peak_db / 20)
    over = np.abs(y) > ceiling * 0.7
    if over.any():
        knee = ceiling * 0.7
        s = np.sign(y[over])
        y[over] = s * (knee + (ceiling - knee) * np.tanh((np.abs(y[over]) - knee) / (ceiling - knee)))
    return y


def write(path, x):
    x = highpass(x, 25)  # removes DC drift from the plucked-string synth
    full = os.path.join(OUT, path + ".ogg")
    os.makedirs(os.path.dirname(full), exist_ok=True)
    # Written in blocks: libsndfile's Vorbis encoder overflows its stack on one long write.
    with sf.SoundFile(full, "w", SR, 1, format="OGG", subtype="VORBIS") as f:
        for i in range(0, len(x), SR // 4):
            f.write(x[i : i + SR // 4].astype(np.float32))
    print(f"{path:24} {len(x) / SR:5.2f}s")


def mix(*parts):
    """Sums (offset_seconds, signal) pairs."""
    end = max(int(o * SR) + len(s) for o, s in parts)
    y = np.zeros(end)
    for o, s in parts:
        i = int(o * SR)
        y[i : i + len(s)] += s
    return y


# ---------------------------------------------------------------- instruments

def bell(freq, seconds=1.2, bright=1.0, decay=3.0):
    """Small glass bell: inharmonic partials that die away faster the higher they are."""
    t = t_axis(seconds)
    partials = [(1.0, 1.0, 1.0), (2.0, 0.35, 1.6), (2.76, 0.45 * bright, 2.2), (5.40, 0.20 * bright, 3.4), (8.93, 0.08 * bright, 5.0)]
    y = np.zeros(len(t))
    for ratio, amp, speed in partials:
        if freq * ratio > SR * 0.45:
            continue
        y += amp * np.sin(2 * np.pi * freq * ratio * t + ratio) * np.exp(-decay * speed * t)
    attack = np.minimum(1, t / 0.0015)
    return y * attack






def noise(seconds, seed=0):
    return np.random.default_rng(seed).standard_normal(int(SR * seconds))


def sweep_noise(seconds, f0, f1, seed=0):
    """Band-limited noise whose brightness glides from f0 to f1: a whoosh."""
    n = noise(seconds, seed)
    out = np.zeros_like(n)
    steps = 24
    edges = np.linspace(0, len(n), steps + 1).astype(int)
    for k in range(steps):
        f = f0 * (f1 / f0) ** (k / (steps - 1))
        band = sosfilt(butter(2, [f * 0.6, min(f * 1.6, SR * 0.45)], "bandpass", fs=SR, output="sos"), n)
        out[edges[k] : edges[k + 1]] = band[edges[k] : edges[k + 1]]
    return out




def wood_tok(freq, seconds=0.12, decay=45, seed=0):
    """A woodblock knock: two inharmonic modes that die fast plus a tiny noise transient for the strike."""
    t = t_axis(seconds)
    body = np.sin(2 * np.pi * freq * t) * np.exp(-decay * t) + 0.45 * np.sin(2 * np.pi * freq * 2.76 * t) * np.exp(-decay * 1.8 * t)
    strike = highpass(noise(0.004, seed), 2500) * 0.25
    return mix((0, body * np.minimum(1, t / 0.0008)), (0, strike))


def keys(freq, seconds=1.6, decay=1.6):
    """Soft electric-piano note: a warm sine with a short bright tine on top and gentle tremolo."""
    t = t_axis(seconds)
    tone = np.sin(2 * np.pi * freq * t) + 0.18 * np.sin(2 * np.pi * freq * 2 * t) * np.exp(-3 * t)
    tine = 0.22 * np.sin(2 * np.pi * freq * 7.0 * t) * np.exp(-30 * t)
    trem = 1 + 0.06 * np.sin(2 * np.pi * 4.5 * t)
    env = np.minimum(1, t / 0.004) * np.exp(-decay * t)
    return (tone + tine) * trem * env


def glock(freq, seconds=1.0):
    """Glockenspiel: bright, clean, short; reads as playful rather than exotic."""
    t = t_axis(seconds)
    y = np.sin(2 * np.pi * freq * t) * np.exp(-4.0 * t) + 0.3 * np.sin(2 * np.pi * freq * 2.76 * t) * np.exp(-9 * t) \
        + 0.12 * np.sin(2 * np.pi * freq * 5.4 * t) * np.exp(-16 * t)
    return y * np.minimum(1, t / 0.001)


def kick(seconds=0.35):
    t = t_axis(seconds)
    f = 50 + 90 * np.exp(-t * 35)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-9 * t)


def snap(seed=0):
    """Finger-snap / soft clap: a short band of noise."""
    n = noise(0.12, seed)
    band = sosfilt(butter(2, [1200, 5000], "bandpass", fs=SR, output="sos"), n)
    return band * np.exp(-t_axis(0.12) * 38)


# ---------------------------------------------------------------- effects

def make_effects():
    # Pick: a light ceramic clink as the shard lifts out of the rack.
    for i, name in enumerate(["impactPlate_light_000", "impactPlate_light_002", "impactPlate_light_003"]):
        x = highpass(trim(load(name), keep=0.22, fade=0.08), 350)
        write(f"Sfx/pick_{i + 1}", level(room(x, wet=0.08), -24))

    # Park: the shard set down on the bench, a heavier plate with a soft wooden thump under it.
    for i, (plate, soft) in enumerate([("impactPlate_medium_000", "impactSoft_medium_000"),
                                       ("impactPlate_medium_002", "impactSoft_medium_002"),
                                       ("impactPlate_medium_004", "impactSoft_medium_000")]):
        clink = highpass(trim(load(plate), keep=0.28, fade=0.1), 180)
        thump = lowpass(trim(load(soft), keep=0.12, fade=0.05), 900) * 0.45
        write(f"Sfx/park_{i + 1}", level(room(mix((0, clink), (0, thump)), wet=0.1), -21))

    # Lock: the gold seal. A tiny glass click, then a bell on C5 (FeedbackService pitches it up the scale through
    # a chain), with a soft octave shimmer.
    click = highpass(trim(load("impactGlass_light_001"), keep=0.06, fade=0.03), 1500) * 0.35
    chime = mix((0, bell(note("C5"), 1.1, bright=0.8, decay=3.2)), (0, 0.25 * bell(note("C6"), 0.8, bright=0.5, decay=4.5)))
    write("Sfx/lock", level(room(mix((0, click), (0.004, chime)), wet=0.16), -19))

    # Uncover: a small high wooden tick when a buried shard frees up; quiet, it often comes with another sound.
    write("Sfx/uncover", level(room(wood_tok(1650, 0.08, decay=60, seed=3), wet=0.08), -28))

    # Reject: a dull low wooden knock for a shard that can't move yet. Gentle, not a buzzer.
    knock = highpass(trim(load("impactWood_medium_001"), keep=0.16, fade=0.06), 90)
    write("Sfx/reject", level(room(mix((0, knock), (0, 0.5 * wood_tok(330, 0.12, decay=40, seed=4))), wet=0.06), -22))

    # Expand: rising whoosh with a quick three-note bell run as the workbench grows.
    whoosh = sweep_noise(0.3, 700, 4500, seed=3) * np.hanning(int(0.3 * SR)) ** 0.5 * 0.5
    run = mix(*[(0.06 + 0.07 * k, bell(note(n), 0.7, bright=0.6, decay=4)) for k, n in enumerate(["C6", "E6", "G6"])])
    write("Sfx/expand", level(room(mix((0, whoosh), (0, run)), wet=0.18), -20))

    # Coins: real coin handling with a bright ding on top.
    for i, name in enumerate(["handleCoins", "handleCoins2"]):
        coins = highpass(trim(load(name), keep=0.5, fade=0.15), 500)
        ding = bell(note("E7"), 0.5, bright=0.4, decay=6) * 0.25
        write(f"Sfx/coins_{i + 1}", level(room(mix((0, coins), (0.01, ding)), wet=0.1), -22))

    # Button: a crisp woodblock tap with a little real wood knock underneath for body.
    body = highpass(trim(load("impactWood_light_002"), keep=0.08, fade=0.04), 200) * 0.45
    write("Sfx/button", level(room(mix((0, wood_tok(1050, 0.1, decay=52, seed=7)), (0, body)), wet=0.05), -18))

    # Popup: a soft double wooden tap as a card lands, low then high, like setting a tray down.
    land = mix((0, wood_tok(520, 0.12, decay=40, seed=8) * 0.7), (0.055, wood_tok(880, 0.1, decay=50, seed=9) * 0.5))
    write("Sfx/popup", level(room(land, wet=0.08), -24))

    # Star: one bright ding, played three times up the scale as the win stars land.
    star = mix((0, bell(note("E6"), 0.9, bright=0.9, decay=3.6)), (0, 0.35 * bell(note("B6"), 0.5, bright=0.5, decay=6)))
    write("Sfx/star", level(room(star, wet=0.2), -19))


# ---------------------------------------------------------------- jingles

def make_jingles():
    # Win: a bouncy major arpeggio on glockenspiel over a warm keys chord, the same sound world as the music.
    notes = ["C5", "E5", "G5", "C6", "E6", "G6"]
    run = [(0.08 * k, glock(note(n), 1.1) * (0.5 + 0.05 * k)) for k, n in enumerate(notes)]
    chord_at = 0.08 * len(notes)
    chord = [(chord_at, keys(note(n), 2.4, decay=1.1) * 0.45) for n in ["C4", "G4", "C5", "E5"]]
    bass = [(chord_at, lowpass(keys(note("C3"), 2.0, decay=1.4), 400) * 0.5)]
    hits = [(chord_at, kick() * 0.5), (chord_at, snap(31) * 0.25)]
    win = mix(*run, *chord, *bass, *hits)
    write("Sfx/win", level(room(win, seconds=1.0, wet=0.2, seed=2), -17))

    # Fail: three soft keys notes stepping down to a gentle minor-sixth chord. Disappointment, not punishment.
    down = [(0.2 * k, keys(note(n), 1.0, decay=2.4) * 0.6) for k, n in enumerate(["G4", "F4", "E4"])]
    rest = [(0.62, keys(note(n), 1.6, decay=1.5) * 0.35) for n in ["F3", "A3", "D4"]]
    fail = lowpass(mix(*down, *rest), 3500)
    write("Sfx/fail", level(room(fail, seconds=1.0, wet=0.2, seed=4), -21))


# ---------------------------------------------------------------- music

def make_music():
    """'Bright Workshop': a light, simple 16-bar loop at 100 bpm in C major (I-V-vi-IV). Soft keys chords, round
    bass, kick and finger snaps, a quiet shaker and a short glockenspiel tune. The reverb tail past the end is
    folded back onto the start so the loop has no seam."""
    bpm = 100
    beat = 60 / bpm
    bar = beat * 4
    bars = 16
    length = bar * bars
    progression = [
        ("C3", ["C4", "E4", "G4"]), ("G2", ["B3", "D4", "G4"]),
        ("A2", ["C4", "E4", "A4"]), ("F2", ["C4", "F4", "A4"]),
    ]
    # One note per eighth, None for a rest; diatonic C major, so it reads as Western pop, not pentatonic.
    tune = [
        ["E5", None, "G5", None, "A5", "G5", "E5", None],
        ["D5", None, "G5", None, "B5", None, "A5", "G5"],
        ["C6", None, "B5", "A5", None, "E5", None, None],
        ["F5", None, "A5", None, "C6", None, "A5", None],
        ["G5", None, "E5", None, "G5", "C6", None, None],
        ["B5", None, "D6", None, "B5", "A5", "G5", None],
        ["A5", None, "C6", "B5", "A5", None, "E5", None],
        ["F5", None, "E5", None, "D5", None, "C5", None],
    ]

    parts = []
    for b in range(bars):
        root, chord = progression[b % 4]
        at = b * bar
        intro = b < 4
        # Keys: a chord on beat 1 and a lighter push on the "and" of 2.
        for offset, vel in ((0.0, 0.26), (beat * 1.5, 0.16), (beat * 3, 0.14)):
            for n in chord:
                parts.append((at + offset, keys(note(n), 1.4, decay=2.2) * vel))
        # Bass: root on 1 and 3, octave on the "and" of 3.
        for offset, octave, vel in ((0, 1, 0.42), (beat * 2, 1, 0.34), (beat * 2.5, 2, 0.24)):
            parts.append((at + offset, lowpass(keys(note(root) * octave, 0.6, decay=4), 500) * vel))
        # Drums: kick on 1 and 3, snap on 2 and 4 (lighter in the intro), shaker eighths.
        for k in range(4):
            if k % 2 == 0:
                parts.append((at + beat * k, kick() * (0.35 if intro else 0.5)))
            else:
                parts.append((at + beat * k, snap(b * 4 + k) * (0.12 if intro else 0.2)))
        for e in range(8):
            hit = highpass(noise(0.04, 500 + b * 8 + e), 7000) * np.exp(-t_axis(0.04) * 80)
            parts.append((at + e * beat / 2, hit * (0.05 if e % 2 else 0.03)))
        # Tune from bar 5; the last four bars repeat its first half an octave lower as a calm turnaround.
        if 4 <= b < 12 or b >= 12:
            line = tune[(b - 4) % 8] if b < 12 else tune[b - 12]
            shift = 1.0 if b < 12 else 0.5
            for e, n in enumerate(line):
                if n is not None:
                    parts.append((at + e * beat / 2, glock(note(n) * shift, 0.9) * (0.2 if b < 12 else 0.15)))

    song = room(mix(*parts), seconds=1.4, wet=0.2, tone=5000, seed=9)
    n = int(length * SR)
    loop = song[:n].copy()
    tail = song[n:]
    loop[: len(tail)] += tail
    write("Music/bright_workshop", level(loop, -21, peak_db=-2))


if __name__ == "__main__":
    make_effects()
    make_jingles()
    make_music()
