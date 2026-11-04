"""
Builds Golden Mend's sound effects and music into Assets/_Game/Content/Audio.

Physical sounds (ceramic clinks, coins) start from Kenney's CC0 recordings in Tools/Audio/source and are cleaned up:
mono, trimmed so the hit starts on the first sample, filtered, given one shared small-room reverb and matched in
loudness. Musical sounds (the gold-seal chime, jingles, button pop, stars, music) are synthesised here, all in
C major pentatonic so they agree with each other and with the pitched lock chain in FeedbackService.

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


def pluck(freq, seconds=0.9, bright=0.55, seed=0):
    """Kalimba / koto-like pluck (Karplus-Strong with a soft, slightly muted string)."""
    rng = np.random.default_rng(seed)
    period = int(round(SR / freq))
    n = int(SR * seconds)
    buf = lowpass(rng.uniform(-1, 1, period), freq * 6) if period > 12 else rng.uniform(-1, 1, period)
    y = np.zeros(n)
    y[:period] = buf
    damp = 0.5 + 0.495 * bright
    for i in range(period, n):
        y[i] = damp * 0.5 * (y[i - period] + y[i - period + 1]) + (1 - damp) * y[i - period]
    body = 0.6 * np.sin(2 * np.pi * freq * t_axis(seconds)) * np.exp(-5 * t_axis(seconds))
    out = y / (np.abs(y).max() + 1e-9) + body
    return out * np.minimum(1, t_axis(seconds) / 0.002)


def pad(freqs, seconds, attack=0.8, release=1.0, tone=1800):
    """Soft warm pad: slightly detuned triangle pairs, low-passed, with slow swells."""
    t = t_axis(seconds)
    y = np.zeros(len(t))
    for f in freqs:
        for detune in (-0.12, 0.12):
            ph = 2 * np.pi * (f * (1 + detune / 100)) * t + np.sin(2 * np.pi * 0.17 * t) * 0.6
            y += (2 / np.pi) * np.arcsin(np.sin(ph))
    y = lowpass(y, tone)
    env = np.minimum(1, t / attack) * np.minimum(1, (seconds - t) / release)
    return y * np.clip(env, 0, 1) / max(1, len(freqs))


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


def blip(f0, f1, seconds, decay=30):
    """A pitch-dropping sine 'pop', the hyper-casual button sound."""
    t = t_axis(seconds)
    f = f1 + (f0 - f1) * np.exp(-t * 40)
    ph = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(ph) * np.exp(-decay * t) * np.minimum(1, t / 0.001)


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

    # Lock: the gold seal. A tiny glass click, then a bell on C5 (FeedbackService pitches it up the pentatonic
    # scale through a chain), with a soft octave shimmer.
    click = highpass(trim(load("impactGlass_light_001"), keep=0.06, fade=0.03), 1500) * 0.35
    chime = mix((0, bell(note("C5"), 1.1, bright=0.8, decay=3.2)), (0, 0.25 * bell(note("C6"), 0.8, bright=0.5, decay=4.5)))
    write("Sfx/lock", level(room(mix((0, click), (0.004, chime)), wet=0.16), -19))

    # Uncover: a soft wooden tok when a buried shard frees up; quiet, it often comes with another sound.
    tok = mix((0, blip(note("G5") * 1.6, note("G5"), 0.12, decay=38)), (0, 0.3 * blip(note("G6"), note("G6"), 0.05, decay=70)))
    write("Sfx/uncover", level(room(lowpass(tok, 5000), wet=0.1), -27))

    # Reject: a dull low bonk for a shard that can't move yet. Gentle, not a buzzer.
    bonk = mix((0, blip(220, 150, 0.16, decay=22)), (0, 0.6 * lowpass(trim(load("impactSoft_medium_002"), keep=0.12, fade=0.05), 600)))
    write("Sfx/reject", level(room(bonk, wet=0.06), -22))

    # Expand: rising whoosh with a quick three-note bell run as the workbench grows.
    whoosh = sweep_noise(0.3, 700, 4500, seed=3) * np.hanning(int(0.3 * SR)) ** 0.5 * 0.5
    run = mix(*[(0.06 + 0.07 * k, bell(note(n), 0.7, bright=0.6, decay=4)) for k, n in enumerate(["C6", "E6", "G6"])])
    write("Sfx/expand", level(room(mix((0, whoosh), (0, run)), wet=0.18), -20))

    # Coins: real coin handling with a bright ding on top.
    for i, name in enumerate(["handleCoins", "handleCoins2"]):
        coins = highpass(trim(load(name), keep=0.5, fade=0.15), 500)
        ding = bell(note("E7"), 0.5, bright=0.4, decay=6) * 0.25
        write(f"Sfx/coins_{i + 1}", level(room(mix((0, coins), (0.01, ding)), wet=0.1), -22))

    # Button: a round bubble pop.
    pop = mix((0, blip(1150, 520, 0.09, decay=45)), (0, 0.15 * highpass(noise(0.006, 7), 3000)))
    write("Sfx/button", level(pop, -18))

    # Popup: a short soft whoosh into a pop as a card slides in.
    swish = sweep_noise(0.16, 900, 3500, seed=5) * np.hanning(int(0.16 * SR)) * 0.35
    write("Sfx/popup", level(room(mix((0, swish), (0.12, blip(900, 450, 0.1, decay=40))), wet=0.1), -23))

    # Star: one bright ding, played three times up the scale as the win stars land.
    star = mix((0, bell(note("E6"), 0.9, bright=0.9, decay=3.6)), (0, 0.35 * bell(note("B6"), 0.5, bright=0.5, decay=6)))
    write("Sfx/star", level(room(star, wet=0.2), -19))


# ---------------------------------------------------------------- jingles

def make_jingles():
    # Win: a quick kalimba run up the pentatonic scale into a held bell chord with sparkles.
    notes = ["C5", "D5", "E5", "G5", "A5", "C6", "E6"]
    run = [(0.075 * k, pluck(note(n), 0.9, seed=k) * (0.6 + 0.06 * k)) for k, n in enumerate(notes)]
    chord_at = 0.075 * len(notes) + 0.05
    chord = [(chord_at, bell(note(n), 2.2, bright=0.6, decay=1.4) * 0.5) for n in ["C5", "G5", "E6"]]
    warm = [(chord_at - 0.1, pad([note("C4"), note("G4"), note("E5")], 2.3, attack=0.25, release=1.2) * 0.5)]
    rng = np.random.default_rng(11)
    sparkle = [(chord_at + 0.1 + 0.12 * k + rng.uniform(0, 0.05),
                bell(note(rng.choice(["C7", "D7", "E7", "G7", "A7"])), 0.5, bright=0.3, decay=7) * 0.12) for k in range(8)]
    win = mix(*run, *chord, *warm, *sparkle)
    write("Sfx/win", level(room(win, seconds=1.2, wet=0.28, seed=2), -17))

    # Fail: three soft plucks stepping down, ending on a calm low chord. Disappointment, not punishment.
    down = [(0.17 * k, pluck(note(n), 1.0, bright=0.4, seed=20 + k) * 0.7) for k, n in enumerate(["G4", "E4", "D4"])]
    rest = [(0.55, pad([note("A3"), note("C4"), note("E4")], 1.4, attack=0.15, release=0.9) * 0.6)]
    fail = lowpass(mix(*down, *rest), 3500)
    write("Sfx/fail", level(room(fail, seconds=1.2, wet=0.25, seed=4), -21))


# ---------------------------------------------------------------- music

def make_music():
    """'Night Workshop': a calm 16-bar loop at 76 bpm. Soft pad and bass, a sparse kalimba melody, a light shaker.
    The reverb tail past the end is folded back onto the start so the loop has no seam."""
    bpm = 76
    beat = 60 / bpm
    bar = beat * 4
    bars = 16
    length = bar * bars
    chords = [  # two bars each, pentatonic-friendly
        ("A3", ["A3", "C4", "E4", "G4"]), ("F3", ["F3", "A3", "C4", "E4"]),
        ("C3", ["C4", "E4", "G4", "D5"]), ("G3", ["G3", "C4", "D4", "A4"]),
    ]
    scale = ["C", "D", "E", "G", "A"]
    melody_notes = [s + "5" for s in scale] + ["C6", "D6", "E6"]
    rng = np.random.default_rng(76)

    parts = []
    for b in range(0, bars, 2):
        root, voicing = chords[(b // 2) % len(chords)]
        at = b * bar
        parts.append((at, pad([note(n) for n in voicing], bar * 2 + 0.6, attack=1.2, release=1.4) * 0.30))
        bass = np.sin(2 * np.pi * note(root) / 2 * t_axis(bar * 2)) * np.exp(-0.25 * t_axis(bar * 2))
        parts.append((at, lowpass(bass, 300) * 0.32))

    # Melody: an eighth-note grid, sparse, a gentle random walk on the scale; quieter in the first four bars.
    idx = 3
    for step in range(bars * 8):
        b = step // 8
        density = 0.18 if b < 4 else 0.34
        if step % 8 == 0 or rng.random() < density:
            idx = int(np.clip(idx + rng.choice([-2, -1, -1, 0, 1, 1, 2]), 0, len(melody_notes) - 1))
            vel = 0.22 + 0.12 * rng.random() + (0.06 if step % 8 == 0 else 0)
            parts.append((step * beat / 2, pluck(note(melody_notes[idx]), 1.4, bright=0.5, seed=step) * vel))

    # Shaker on the off-beats, very quiet.
    for k in range(bars * 4):
        hit = highpass(noise(0.05, 100 + k), 6000) * np.exp(-t_axis(0.05) * 70)
        parts.append((k * beat + beat / 2, hit * (0.035 if k % 2 else 0.025)))

    song = room(mix(*parts), seconds=2.2, wet=0.32, tone=4000, seed=9)
    n = int(length * SR)
    loop = song[:n].copy()
    tail = song[n:]
    loop[: len(tail)] += tail
    write("Music/night_workshop", level(loop, -21, peak_db=-2))


if __name__ == "__main__":
    make_effects()
    make_jingles()
    make_music()
