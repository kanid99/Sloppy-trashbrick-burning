"""Synthesises Sounds/STB/CobbledRattle.wav, the jerry-rigged machines' running loop.

    python3 Source/Audio/make_rattle.py

Four layers over a 4 second loop that repeats seamlessly: every periodic part's rate divides the
loop exactly, and the random parts are generated with the loop's own seed and cross-faded at the seam.

  hum       a low mains-and-shaft hum, 50Hz with a couple of harmonics
  knock     a worn bearing knocking once per revolution, a short decaying thud
  rattle    loose panels chattering - bursts of filtered noise, uneven in timing and strength
  whine     a faint slipping-belt whine that wavers
"""
import wave

import numpy as np

RATE = 22050
SECONDS = 4.0
N = int(RATE * SECONDS)
rng = np.random.default_rng(1947)
t = np.arange(N) / RATE


def lowpass(x, k):
    """A cheap moving-average low-pass: k samples wide."""
    return np.convolve(x, np.ones(k) / k, mode="same")


hum = 0.18 * np.sin(2 * np.pi * 50 * t) + 0.08 * np.sin(2 * np.pi * 100 * t) + 0.04 * np.sin(2 * np.pi * 150 * t)
hum *= 0.85 + 0.15 * np.sin(2 * np.pi * 0.5 * t)          # 0.5Hz: two swells per loop

knock = np.zeros(N)
period = N // 17                                          # 17 knocks per loop, ~4.25Hz
thud = np.exp(-np.arange(1200) / 180.0) * np.sin(2 * np.pi * 90 * np.arange(1200) / RATE)
for k in range(17):
    start = max(0, k * period + int(rng.integers(-60, 60)))
    amp = 0.55 + 0.25 * rng.random()
    seg = thud[: max(0, min(len(thud), N - start))]
    knock[start:start + len(seg)] += amp * seg

rattle = np.zeros(N)
noise = lowpass(rng.standard_normal(N), 6) - lowpass(rng.standard_normal(N), 60)
pos = 0
while pos < N:
    length = int(rng.integers(300, 1400))
    env = np.hanning(length) * (0.2 + 0.6 * rng.random())
    end = min(N, pos + length)
    rattle[pos:end] += noise[pos:end] * env[: end - pos]
    pos += length + int(rng.integers(200, 2600))
rattle *= 0.5

whine = 0.03 * np.sin(2 * np.pi * (1320 * t + 18 * np.sin(2 * np.pi * 1.25 * t) / (2 * np.pi * 1.25)))

mix = hum + knock + rattle + whine

# Seamless seam: cross-fade the last 0.1s into the first.
fade = int(0.1 * RATE)
ramp = np.linspace(0, 1, fade)
mix[:fade] = mix[:fade] * ramp + mix[-fade:] * (1 - ramp)
mix = mix[: N - fade]

mix /= np.max(np.abs(mix)) * 1.1
pcm = (mix * 32767).astype(np.int16)
with wave.open("Sounds/STB/CobbledRattle.wav", "wb") as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(RATE)
    w.writeframes(pcm.tobytes())
print("wrote Sounds/STB/CobbledRattle.wav", len(pcm) / RATE, "s")
