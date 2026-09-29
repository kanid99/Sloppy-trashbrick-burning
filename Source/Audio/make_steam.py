"""Synthesises the steam machines' running loops. Run from the repo root:

    python3 Source/Audio/make_steam.py

Sounds/STB/SteamEngine.wav - the trash gasifier's Stirling and steam plant: a steady chuff
    (filtered noise bursts, 12 per 4 second loop) over a low firebox rumble and a faint hiss.
Sounds/STB/TurbineWhine.wav - the steam turbine: a shaft hum, a rising-and-settling blade
    whine and rushing steam.

Every periodic part divides the loop exactly, and the seam is cross-faded, so both loop cleanly.
"""
import wave

import numpy as np

RATE = 22050
SECONDS = 4.0
N = int(RATE * SECONDS)
t = np.arange(N) / RATE


def lowpass(x, k):
    return np.convolve(x, np.ones(k) / k, mode="same")


def write(mix, path):
    fade = int(0.1 * RATE)
    ramp = np.linspace(0, 1, fade)
    mix = mix.copy()
    mix[:fade] = mix[:fade] * ramp + mix[-fade:] * (1 - ramp)
    mix = mix[: N - fade]
    mix /= np.max(np.abs(mix)) * 1.1
    pcm = (mix * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())
    print("wrote", path, len(pcm) / RATE, "s")


# --- steam engine
rng = np.random.default_rng(1886)
hiss = lowpass(rng.standard_normal(N), 3) - lowpass(rng.standard_normal(N), 24)
chuff = np.zeros(N)
period = N // 12
env_len = int(period * 0.55)
env = np.exp(-np.arange(env_len) / (env_len / 5.0)) * (1 - np.exp(-np.arange(env_len) / 60.0))
body = lowpass(rng.standard_normal(N), 9)
for k in range(12):
    start = k * period
    amp = 0.8 if k % 2 == 0 else 0.6       # the two strokes of a double-acting cylinder
    chuff[start:start + env_len] += amp * body[start:start + env_len] * env
rumble = 0.25 * lowpass(rng.standard_normal(N), 180) * 6 + 0.08 * np.sin(2 * np.pi * 37.5 * t)
write(chuff + 0.06 * hiss + rumble, "Sounds/STB/SteamEngine.wav")

# --- turbine
rng = np.random.default_rng(1884)
hum = 0.2 * np.sin(2 * np.pi * 50 * t) + 0.08 * np.sin(2 * np.pi * 100 * t)
wobble = 12 * np.sin(2 * np.pi * 0.5 * t)                  # two slow swells per loop
phase = 2 * np.pi * (880 * t + np.cumsum(wobble) / RATE)
whine = 0.07 * np.sin(phase) + 0.03 * np.sin(2 * phase)
rush = 0.18 * (lowpass(rng.standard_normal(N), 4) - lowpass(rng.standard_normal(N), 40))
rush *= 0.85 + 0.15 * np.sin(2 * np.pi * 0.5 * t)
write(hum + whine + rush, "Sounds/STB/TurbineWhine.wav")
