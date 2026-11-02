"""Sound effect recipes for Balloon Dash. Each function returns a rendered Track.

The runner's sounds are short and dry so they can fire many times a second; the game varies
their pitch (footsteps alternate, coins climb a scale while they come in a row).
"""

from synth import Track, note_freq, saw, square, triangle


def pulse(duty):
    return lambda p: square(p, duty)


def step():
    """A footstep: a tiny soft thud with a grain of grit."""
    t = Track(0.07)
    t.add_tone(0, 0.02, 190, triangle, 0.35, freq_end=110, attack=0.001, release=0.02)
    t.add_noise(0, 0.03, 0.12, smoothing=0.7, decay_power=2.5, seed=11)
    return t


def jump():
    """A jump: a springy upward chirp."""
    t = Track(0.22)
    t.add_tone(0, 0.12, 300, pulse(0.25), 0.2, freq_end=760, attack=0.002, release=0.05)
    t.add_noise(0, 0.05, 0.06, smoothing=0.5, decay_power=2.0, seed=12)
    return t


def double_jump():
    """A double jump: a higher chirp with a little sparkle on top."""
    t = Track(0.28)
    t.add_tone(0, 0.1, 520, pulse(0.125), 0.18, freq_end=1240, attack=0.002, release=0.06)
    t.add_tone(0.06, 0.08, note_freq("E7"), triangle, 0.12, release=0.1)
    t.add_noise(0, 0.08, 0.08, smoothing=0.2, decay_power=1.5, seed=13)
    return t


def land():
    """Landing: a thump with dust. The game sets its volume from the impact."""
    t = Track(0.25)
    t.add_tone(0, 0.06, 140, triangle, 0.55, freq_end=55, attack=0.001, release=0.06)
    t.add_noise(0, 0.12, 0.25, smoothing=0.75, decay_power=1.8, seed=14)
    return t


def slide():
    """A slide: a long scrape of gravel."""
    t = Track(0.5)
    t.add_noise(0, 0.42, 0.32, smoothing=0.45, decay_power=0.8, seed=15)
    t.add_tone(0, 0.3, 120, saw, 0.06, freq_end=90, release=0.1)
    return t


def dive():
    """A dive from the air: a falling whistle."""
    t = Track(0.3)
    t.add_tone(0, 0.2, 1300, triangle, 0.2, freq_end=300, release=0.06)
    t.add_noise(0, 0.18, 0.1, smoothing=0.6, decay_power=1.0, seed=16)
    return t


def coin():
    """A coin: a two-note bling."""
    t = Track(0.3)
    t.add_tone(0, 0.04, note_freq("B6"), pulse(0.25), 0.16, release=0.03)
    t.add_tone(0.045, 0.1, note_freq("E7"), pulse(0.25), 0.16, release=0.15)
    t.add_tone(0.045, 0.1, note_freq("E6"), triangle, 0.1, release=0.12)
    return t


def balloon_up():
    """The balloon inflates and lifts: a stretchy rising squeak and a whoosh."""
    t = Track(0.9)
    t.add_tone(0, 0.55, 260, pulse(0.5), 0.14, freq_end=780, release=0.12, vibrato=0.05)
    t.add_tone(0.1, 0.5, 390, triangle, 0.2, freq_end=1170, release=0.15)
    t.add_noise(0, 0.45, 0.1, smoothing=0.85, decay_power=0.6, seed=17)
    return t


def balloon_pop():
    """The balloon runs out and pops."""
    t = Track(0.45)
    t.add_noise(0, 0.08, 0.6, smoothing=0.05, decay_power=3.0, seed=18)
    t.add_tone(0.02, 0.18, 900, triangle, 0.18, freq_end=200, release=0.1)
    return t


def boing():
    """The balloon bounces off the ground (the boing from Challenge 3, synthesized anew)."""
    t = Track(0.5)
    t.add_tone(0, 0.32, 180, triangle, 0.5, freq_end=420, release=0.1, vibrato=0.08)
    t.add_tone(0, 0.2, 90, triangle, 0.3, freq_end=60, release=0.05)
    return t


def magnet():
    """The magnet switches on: a wobbling rise."""
    t = Track(0.7)
    t.add_tone(0, 0.5, 220, pulse(0.25), 0.18, freq_end=880, release=0.12, vibrato=0.06)
    t.add_tone(0.05, 0.45, 330, triangle, 0.2, freq_end=1320, release=0.12)
    return t


def shield_up():
    """The shield goes up: a bright shimmering chord."""
    t = Track(0.8)
    for i, name in enumerate(["D6", "F#6", "A6", "D7"]):
        t.add_tone(i * 0.04, 0.3, note_freq(name), triangle, 0.15, release=0.35, vibrato=0.01)
    return t


def shield_break():
    """The shield takes a hit: glass breaking over a thud."""
    t = Track(0.7)
    t.add_noise(0, 0.35, 0.45, smoothing=0.0, decay_power=2.2, seed=19)
    t.add_tone(0, 0.08, note_freq("A6"), pulse(0.5), 0.12, freq_end=note_freq("A5"), release=0.1)
    t.add_tone(0, 0.12, 110, triangle, 0.5, freq_end=50, release=0.1)
    return t


def smash():
    """An obstacle breaks apart: splinters."""
    t = Track(0.4)
    t.add_noise(0, 0.05, 0.5, smoothing=0.3, decay_power=2.0, seed=20)
    t.add_noise(0.04, 0.2, 0.3, smoothing=0.55, decay_power=1.8, seed=21)
    t.add_tone(0, 0.08, 220, square, 0.12, freq_end=90, release=0.05)
    return t


def crash():
    """Running into an obstacle: a heavy thud and a tumble."""
    t = Track(0.9)
    t.add_tone(0, 0.18, 160, triangle, 0.7, freq_end=40, attack=0.001, release=0.1)
    t.add_noise(0, 0.3, 0.55, smoothing=0.65, decay_power=1.6, seed=22)
    t.add_noise(0.25, 0.08, 0.2, smoothing=0.7, decay_power=2.0, seed=23)
    t.add_noise(0.42, 0.06, 0.12, smoothing=0.7, decay_power=2.0, seed=24)
    return t


def bomb():
    """A bomb goes off: a long crunchy blast under a falling rumble."""
    t = Track(1.4)
    t.add_noise(0, 1.1, 0.7, smoothing=0.75, decay_power=1.8, seed=4)
    t.add_noise(0, 0.25, 0.35, smoothing=0.2, decay_power=2.5, seed=5)
    t.add_tone(0, 0.6, 120, triangle, 0.7, freq_end=35, release=0.3)
    return t


def poof():
    """Ground obstacles vanish for the balloon: a soft puff."""
    t = Track(0.3)
    t.add_noise(0, 0.22, 0.3, smoothing=0.8, decay_power=1.2, seed=25)
    t.add_tone(0, 0.1, 600, triangle, 0.08, freq_end=1200, release=0.08)
    return t


def power_end():
    """A power-up runs out."""
    t = Track(0.3)
    t.add_tone(0, 0.08, note_freq("E5"), triangle, 0.28, release=0.04)
    t.add_tone(0.1, 0.1, note_freq("A4"), triangle, 0.28, release=0.08)
    return t


def milestone():
    """Another 100 metres: three quick rising notes."""
    t = Track(0.45)
    for i, name in enumerate(["D6", "F#6", "A6"]):
        t.add_tone(i * 0.06, 0.06, note_freq(name), pulse(0.25), 0.18, release=0.1)
    return t


def new_record():
    """A new best score: a bright fanfare."""
    t = Track(1.6)
    for name, at, length in [("D5", 0.0, 0.1), ("F#5", 0.12, 0.1), ("A5", 0.24, 0.1),
                             ("D6", 0.36, 0.2), ("A5", 0.6, 0.1), ("D6", 0.72, 0.6)]:
        t.add_tone(at, length, note_freq(name), pulse(0.25), 0.22, release=0.15)
    t.add_tone(0.72, 0.6, note_freq("D3"), triangle, 0.45, release=0.3)
    return t


def game_over():
    """The run ends without a record: a short falling line."""
    t = Track(1.2)
    for name, at, length in [("A5", 0.0, 0.15), ("F#5", 0.18, 0.15), ("D5", 0.36, 0.5)]:
        t.add_tone(at, length, note_freq(name), triangle, 0.35, release=0.2)
    t.add_tone(0.36, 0.5, note_freq("D3"), triangle, 0.35, release=0.25)
    return t


def go():
    """The run starts."""
    t = Track(0.45)
    t.add_tone(0, 0.08, note_freq("A5"), pulse(0.25), 0.2, release=0.03)
    t.add_tone(0.1, 0.2, note_freq("D6"), pulse(0.25), 0.22, release=0.12)
    return t


def move():
    """Menu cursor."""
    t = Track(0.06)
    t.add_tone(0, 0.02, 1100, pulse(0.5), 0.14, release=0.02)
    return t


def select():
    """Menu confirm."""
    t = Track(0.16)
    t.add_tone(0, 0.04, note_freq("E6"), pulse(0.25), 0.18, release=0.02)
    t.add_tone(0.05, 0.06, note_freq("B6"), pulse(0.25), 0.18, release=0.05)
    return t


def pause():
    """The game pauses or resumes."""
    t = Track(0.25)
    t.add_tone(0, 0.08, note_freq("E5"), triangle, 0.3, release=0.05)
    t.add_tone(0.09, 0.08, note_freq("B4"), triangle, 0.3, release=0.08)
    return t


def wind():
    """A loop of wind for the sky, played under the balloon."""
    t = Track(2.0)
    t.add_noise(0, 2.0, 0.35, smoothing=0.96, decay_power=0.0, seed=26)
    t.fade(0.02, 0.02)
    return t


EFFECTS = {
    "step.ogg": step,
    "jump.ogg": jump,
    "double_jump.ogg": double_jump,
    "land.ogg": land,
    "slide.ogg": slide,
    "dive.ogg": dive,
    "coin.ogg": coin,
    "balloon_up.ogg": balloon_up,
    "balloon_pop.ogg": balloon_pop,
    "boing.ogg": boing,
    "magnet.ogg": magnet,
    "shield_up.ogg": shield_up,
    "shield_break.ogg": shield_break,
    "smash.ogg": smash,
    "crash.ogg": crash,
    "bomb.ogg": bomb,
    "poof.ogg": poof,
    "power_end.ogg": power_end,
    "milestone.ogg": milestone,
    "new_record.ogg": new_record,
    "game_over.ogg": game_over,
    "go.ogg": go,
    "move.ogg": move,
    "select.ogg": select,
    "pause.ogg": pause,
    "wind.ogg": wind,
}
