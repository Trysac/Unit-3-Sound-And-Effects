"""Original music for Balloon Dash. Melodies are written as 'NOTE:EIGHTHS' tokens; 'R' is a rest.

Drum patterns use one character per eighth: K kick, S snare, T low tom, t high tom,
h closed hat, '.' rest. Bass patterns: R root, F fifth, O octave, '.' rest.
"""

from synth import Track, note_freq, square, triangle

CHORDS = {
    "D": ("D", [0, 4, 7]), "G": ("G", [0, 4, 7]), "A": ("A", [0, 4, 7]),
    "Bm": ("B", [0, 3, 7]), "Am": ("A", [0, 3, 7]), "Dm": ("D", [0, 3, 7]),
    "Em": ("E", [0, 3, 7]), "E": ("E", [0, 4, 7]), "F": ("F", [0, 4, 7]),
    "C": ("C", [0, 4, 7]), "B": ("B", [0, 4, 7]),
}

WAVES = {
    "pulse": lambda p: square(p, 0.25),
    "thin": lambda p: square(p, 0.125),
    "soft": triangle,
}


def parse(melody):
    notes = []
    for token in melody.split():
        name, length = token.split(":")
        notes.append((None if name == "R" else name, int(length)))
    return notes


def kick(track, at):
    track.add_tone(at, 0.09, 150, triangle, 0.55, freq_end=45, release=0.03)


def tom(track, at, pitch, seed):
    """A war-drum hit: a pitched thump with a short skin noise."""
    track.add_tone(at, 0.16, pitch, triangle, 0.5, freq_end=pitch * 0.55, release=0.06)
    track.add_noise(at, 0.05, 0.12, smoothing=0.6, decay_power=2.0, seed=seed)


def snare(track, at, seed):
    track.add_noise(at, 0.12, 0.22, smoothing=0.25, decay_power=1.6, seed=seed)


def hat(track, at, seed):
    track.add_noise(at, 0.03, 0.05, smoothing=0.0, decay_power=2.0, seed=seed)


def render(song):
    eighth = song["eighth"]
    per_bar = song["meter"]
    bars = [bar for section in song["form"] for bar in song["sections"][section]]
    total = len(bars) * per_bar * eighth
    track = Track(total + 0.01)
    lead_wave = WAVES[song["lead"]]
    arp_wave = WAVES["thin"]

    for bar_index, (melody, chord) in enumerate(bars):
        bar_start = bar_index * per_bar * eighth
        cursor = 0
        for name, length in parse(melody):
            if name:
                track.add_tone(bar_start + cursor * eighth, length * eighth * 0.9, note_freq(name),
                               lead_wave, song["lead_volume"], decay=0.06, sustain=0.6,
                               release=0.05, vibrato=0.005 if length >= 3 else 0.0)
            cursor += length
        root, intervals = CHORDS[chord]
        bass_root = note_freq(root + "2")
        for step, kind in enumerate(song["bass"]):
            if kind == ".":
                continue
            freq = {"R": bass_root, "F": bass_root * 2 ** (7 / 12), "O": bass_root * 2}[kind]
            track.add_tone(bar_start + step * eighth, eighth * 0.8, freq, triangle, 0.4,
                           release=0.02)
        arp_root = note_freq(root + "4")
        for step in range(per_bar):
            interval = intervals[step % len(intervals)]
            track.add_tone(bar_start + step * eighth, eighth * 0.5, arp_root * 2 ** (interval / 12),
                           arp_wave, song["arp_volume"], release=0.02)
        for step, kind in enumerate(song["drums"]):
            at = bar_start + step * eighth
            seed = bar_index * 31 + step
            if kind == "K":
                kick(track, at)
            elif kind == "S":
                snare(track, at, seed)
            elif kind == "T":
                tom(track, at, 95, seed)
            elif kind == "t":
                tom(track, at, 140, seed)
            if kind in "KSh":
                hat(track, at, seed + 7)

    track.normalize(0.8)
    return track



# Run theme in D major: driving, for the runs.
RUN_A = [
    ("D6:1 R:1 A5:1 D6:1 F#6:2 E6:2", "D"), ("D6:1 B5:1 R:1 B5:1 F#5:4", "Bm"),
    ("G5:1 R:1 B5:1 D6:1 G6:2 F#6:2", "G"), ("E6:2 C#6:2 A5:4", "A"),
    ("D6:1 R:1 A5:1 D6:1 F#6:2 A6:2", "D"), ("B6:1 A6:1 F#6:1 D6:1 E6:4", "Bm"),
    ("G6:2 F#6:1 E6:1 D6:2 C#6:2", "A"), ("D6:6 R:2", "D"),
]
RUN_B = [
    ("B5:2 B5:1 C#6:1 D6:2 B5:2", "G"), ("C#6:2 C#6:1 D6:1 E6:2 C#6:2", "A"),
    ("D6:2 F#6:2 B6:2 F#6:2", "Bm"), ("A5:2 C#6:2 E6:4", "A"),
    ("G5:1 B5:1 D6:2 G5:1 B5:1 E6:2", "G"), ("F#5:1 A5:1 D6:2 F#5:1 A5:1 F#6:2", "D"),
    ("G6:2 F#6:2 E6:2 C#6:2", "Em"), ("E6:4 R:4", "A"),
]

# Sky theme: the run theme's chords and tempo with long, airy notes, for the balloon.
SKY_A = [
    ("F#6:4 A6:4", "D"), ("F#6:3 E6:1 D6:4", "Bm"),
    ("D6:4 G6:4", "G"), ("E6:6 R:2", "A"),
    ("F#6:4 A6:4", "D"), ("B6:3 A6:1 F#6:4", "Bm"),
    ("G6:2 F#6:2 E6:4", "A"), ("D6:6 R:2", "D"),
]

# Title theme in G major: relaxed, for the menus.
TITLE_A = [
    ("B5:3 A5:1 G5:2 D5:2", "G"), ("E5:3 F#5:1 G5:4", "Em"),
    ("A5:3 G5:1 F#5:2 D5:2", "D"), ("F#5:6 R:2", "D"),
    ("B5:3 A5:1 G5:2 B5:2", "G"), ("D6:3 B5:1 A5:4", "Bm"),
    ("A5:2 B5:2 A5:2 F#5:2", "D"), ("G5:6 R:2", "G"),
]

SONGS = {
    "run_theme.ogg": {
        "eighth": 0.115, "meter": 8, "form": "AABA", "lead": "pulse", "lead_volume": 0.15,
        "arp_volume": 0.05, "sections": {"A": RUN_A, "B": RUN_B},
        "bass": "RORORORF", "drums": "KhShKKSh",
    },
    "sky_theme.ogg": {
        "eighth": 0.115, "meter": 8, "form": "AA", "lead": "soft", "lead_volume": 0.26,
        "arp_volume": 0.09, "sections": {"A": SKY_A},
        "bass": "R...F...", "drums": "K...h...",
    },
    "title_theme.ogg": {
        "eighth": 0.18, "meter": 8, "form": "AA", "lead": "soft", "lead_volume": 0.28,
        "arp_volume": 0.06, "sections": {"A": TITLE_A},
        "bass": "R...F...", "drums": "K...S..h",
    },
}
