"""Generate the original, loop-safe Plant Spirit background music assets.

The score is deterministic and uses only synthesized waveforms, so the project
does not depend on third-party music or an online generation service.
"""

from pathlib import Path

import numpy as np
from scipy.io import wavfile


SAMPLE_RATE = 48_000
ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "植物精灵/Assets/Game/Audio/Resources/PlantSpirit/Audio/Music"


def frequency(midi_note: float) -> float:
    return 440.0 * 2.0 ** ((midi_note - 69.0) / 12.0)


def oscillator(phase: np.ndarray, shape: str) -> np.ndarray:
    if shape == "triangle":
        return 2.0 / np.pi * np.arcsin(np.sin(phase))
    if shape == "soft_square":
        return np.tanh(2.4 * np.sin(phase))
    if shape == "bell":
        return np.sin(phase) + 0.32 * np.sin(2.01 * phase) + 0.12 * np.sin(3.98 * phase)
    return np.sin(phase)


def add_tone(track: np.ndarray, start: float, duration: float, note: float, amplitude: float,
             shape: str = "sine", pan: float = 0.0, attack: float = 0.02,
             release: float = 0.08, vibrato: float = 0.0) -> None:
    begin = max(0, int(start * SAMPLE_RATE))
    end = min(len(track), begin + int(duration * SAMPLE_RATE))
    count = end - begin
    if count <= 1:
        return
    t = np.arange(count, dtype=np.float64) / SAMPLE_RATE
    phase = 2.0 * np.pi * frequency(note) * t
    if vibrato:
        phase += vibrato * np.sin(2.0 * np.pi * 5.1 * t)
    sound = oscillator(phase, shape)
    envelope = np.ones(count, dtype=np.float64)
    attack_samples = min(count, max(1, int(attack * SAMPLE_RATE)))
    release_samples = min(count, max(1, int(release * SAMPLE_RATE)))
    envelope[:attack_samples] *= np.sin(np.linspace(0.0, np.pi / 2.0, attack_samples)) ** 2
    envelope[-release_samples:] *= np.sin(np.linspace(np.pi / 2.0, 0.0, release_samples)) ** 2
    left = np.sqrt((1.0 - pan) * 0.5)
    right = np.sqrt((1.0 + pan) * 0.5)
    track[begin:end, 0] += amplitude * left * sound * envelope
    track[begin:end, 1] += amplitude * right * sound * envelope


def add_kick(track: np.ndarray, start: float, amplitude: float = 0.22) -> None:
    duration = 0.28
    begin = int(start * SAMPLE_RATE)
    end = min(len(track), begin + int(duration * SAMPLE_RATE))
    count = end - begin
    if count <= 1:
        return
    t = np.arange(count, dtype=np.float64) / SAMPLE_RATE
    phase = 2.0 * np.pi * (49.0 * t + 32.0 * (1.0 - np.exp(-18.0 * t)))
    sound = np.sin(phase) * np.exp(-15.0 * t)
    sound[:max(1, int(.003 * SAMPLE_RATE))] *= np.linspace(0.0, 1.0, max(1, int(.003 * SAMPLE_RATE)))
    track[begin:end] += amplitude * sound[:, None]


def add_noise_hit(track: np.ndarray, start: float, rng: np.random.Generator,
                  amplitude: float = 0.08, duration: float = 0.16, pan: float = 0.0) -> None:
    begin = int(start * SAMPLE_RATE)
    end = min(len(track), begin + int(duration * SAMPLE_RATE))
    count = end - begin
    if count <= 1:
        return
    t = np.arange(count, dtype=np.float64) / SAMPLE_RATE
    noise = rng.normal(0.0, 1.0, count)
    noise = np.concatenate(([0.0], np.diff(noise))) * np.exp(-28.0 * t)
    attack_samples = min(count, max(1, int(.002 * SAMPLE_RATE)))
    noise[:attack_samples] *= np.linspace(0.0, 1.0, attack_samples)
    left = np.sqrt((1.0 - pan) * 0.5)
    right = np.sqrt((1.0 + pan) * 0.5)
    track[begin:end, 0] += amplitude * left * noise
    track[begin:end, 1] += amplitude * right * noise


def finish(track: np.ndarray, path: Path) -> None:
    # Gentle saturation catches layered transients, then leave 3 dB headroom for SFX.
    track = np.tanh(track * 1.15)
    track -= np.mean(track, axis=0, keepdims=True)
    edge = min(len(track) // 2, int(.012 * SAMPLE_RATE))
    ramp = np.sin(np.linspace(0.0, np.pi / 2.0, edge)) ** 2
    track[:edge] *= ramp[:, None]
    track[-edge:] *= ramp[::-1, None]
    peak = np.max(np.abs(track))
    if peak > 0:
        track *= 10.0 ** (-3.0 / 20.0) / peak
    path.parent.mkdir(parents=True, exist_ok=True)
    wavfile.write(path, SAMPLE_RATE, np.asarray(track * 32767.0, dtype=np.int16))


def make_menu() -> None:
    bpm, bars = 90.0, 12
    beat = 60.0 / bpm
    bar = beat * 4.0
    track = np.zeros((round(bars * bar * SAMPLE_RATE), 2), dtype=np.float64)
    rng = np.random.default_rng(260905)
    chords = [(50, 53, 57, 60), (46, 50, 53, 57), (48, 52, 55, 59), (45, 48, 52, 55)]
    melody = [69, 72, 74, 72, 67, 69, 65, 67, 69, 72, 76, 74, 72, 69, 67, 65]
    for bar_index in range(bars):
        start = bar_index * bar
        chord = chords[bar_index % len(chords)]
        for index, note in enumerate(chord):
            add_tone(track, start, bar, note, .055, "triangle", -.45 + index * .3, .18, .28, .018)
        add_tone(track, start, bar * .92, chord[0] - 12, .09, "sine", -.08, .04, .18)
        for step in range(8):
            note = melody[(bar_index * 3 + step) % len(melody)]
            add_tone(track, start + step * beat / 2.0, beat * .42, note, .045,
                     "bell", .38 if step % 2 else -.28, .015, .1, .012)
        add_noise_hit(track, start, rng, .018, .22, -.3)
        add_noise_hit(track, start + 2 * beat, rng, .014, .18, .35)
    finish(track, OUTPUT / "menu_music.wav")


def make_level() -> None:
    bpm, bars = 128.0, 16
    beat = 60.0 / bpm
    bar = beat * 4.0
    track = np.zeros((round(bars * bar * SAMPLE_RATE), 2), dtype=np.float64)
    rng = np.random.default_rng(260906)
    chords = [(45, 48, 52), (41, 45, 48), (43, 47, 50), (40, 43, 47)]
    pulse = [57, 60, 64, 60, 55, 59, 62, 59, 57, 60, 65, 64, 55, 59, 62, 67]
    for bar_index in range(bars):
        start = bar_index * bar
        chord = chords[bar_index % len(chords)]
        for index, note in enumerate(chord):
            add_tone(track, start, bar, note, .04, "triangle", -.5 + index * .5, .08, .18, .01)
        for step in range(8):
            at = start + step * beat / 2.0
            note = pulse[(bar_index * 5 + step) % len(pulse)]
            add_tone(track, at, beat * .38, note, .06, "soft_square",
                     -.25 if step % 2 else .25, .008, .06)
            if step % 2 == 0:
                add_tone(track, at, beat * .42, chord[0] - 12, .1, "sine", 0.0, .006, .07)
        for step in range(4):
            at = start + step * beat
            add_kick(track, at, .18 if step in (0, 2) else .1)
            add_noise_hit(track, at + beat / 2.0, rng, .035, .09, .5 if step % 2 else -.5)
        add_noise_hit(track, start + beat, rng, .07, .2, -.15)
        add_noise_hit(track, start + 3 * beat, rng, .075, .2, .15)
    finish(track, OUTPUT / "level_music.wav")


if __name__ == "__main__":
    make_menu()
    make_level()
    print(f"Generated menu_music.wav and level_music.wav in {OUTPUT}")
