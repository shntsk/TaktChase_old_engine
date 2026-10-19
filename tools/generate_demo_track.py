"""Generates the original demo track (120 BPM, kick on every beat from t=0, 44 s, mono 22.05 kHz).
Deterministic: fixed seed, no external audio. Usage: python3 tools/generate_demo_track.py <out.wav>"""
import sys, wave
import numpy as np

SR, BPM, SECONDS = 22050, 120, 44
BEAT = 60 / BPM
rng = np.random.default_rng(1234)
out = np.zeros(int(SR * SECONDS))

def add(t, sig, gain=1.0):
    i = int(t * SR)
    if i >= len(out): return
    sig = sig[: len(out) - i]
    out[i:i + len(sig)] += sig * gain

def tone(freq, dur, decay):
    t = np.arange(int(dur * SR)) / SR
    return np.sin(2 * np.pi * freq * t) * np.exp(-t * decay)

def kick():
    t = np.arange(int(0.3 * SR)) / SR
    f = 45 + 110 * np.exp(-t * 30)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 12)

def noise(dur, decay):
    t = np.arange(int(dur * SR)) / SR
    return rng.uniform(-1, 1, len(t)) * np.exp(-t * decay)

pent = [220.0, 261.63, 293.66, 329.63, 392.0, 440.0]
k = kick()
for b in range(int(SECONDS / BEAT)):
    t = b * BEAT
    add(t, k, 0.9 if b >= 4 else 0.6)
    if b >= 4:
        add(t + BEAT / 2, noise(0.05, 90), 0.12)                 # hi-hat
        add(t, tone([55, 55, 65.4, 82.4][(b // 4) % 4], 0.45, 5), 0.35)  # bass
    if b >= 8 and b % 2 == 1:
        add(t, noise(0.15, 25), 0.25)                            # snare
    if b >= 16 and b < 80:
        add(t, tone(pent[(b * 3) % 6], 0.3, 9), 0.2)             # lead
        add(t + BEAT / 2, tone(pent[(b * 3 + 2) % 6], 0.25, 10), 0.15)
for f in (220, 277.18, 329.63, 440):                             # finale on beat 80
    add(80 * BEAT, tone(f, 3.0, 1.5), 0.25)
add(80 * BEAT, noise(1.0, 5), 0.3)

out *= 0.85 / max(1e-9, np.abs(out).max())
with wave.open(sys.argv[1], "wb") as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
    w.writeframes((out * 32767).astype("<i2").tobytes())
