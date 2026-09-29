"""Animated concept previews of moving parts on the turbines, for choosing before any of it goes
into the game. Writes GIFs and a still sheet to the path given (default: the current directory).

    python3 Source/Art/turbine_motion_concepts.py OUTDIR

Each concept is the real south-view sprite with the moving parts drawn over it, frame by frame,
in the same machine coordinates the sprite uses (a across the front, f front to back, z up;
Source/Art/stb_draw.py's View). What moves on a real steam turbine set, seen from above:

  governor   a flyball governor on the steam chest: two weights on arms, whirling round a spindle
             and flying out with speed. Seen from above it's a spinning pair - it reads at any zoom.
  coupling   the shaft coupling between turbine and generator: a horizontal shaft, so from above
             its bolt heads roll across its width - they appear on one side, cross, and vanish.
  blades     through an inspection hatch in the casing, the rotor's blade rows stream past, across
             the shaft - the one view of the actual turbine inside.
  fan        the generator's cooling fan (what the game has now).
  flywheel   (cobbled) an old flywheel: from above, its rim runs across the machine, balance
             weights and a bolted patch rolling over the top.
  belt       (cobbled) a flat belt from a pulley on the flywheel to a salvaged dynamo on the flank:
             its laced splice runs along the belt and the pulleys spin. The most jerry-rigged motion.
"""
import math
import os
import sys

from PIL import Image, ImageDraw

CELL = 192
LIFT = 0.55
M = 0.5          # margin: the 2x3 turbines draw at (3,4)
W, D = 2, 3
SS = 3           # supersampled per frame
FRAMES = 24
TEX = "Textures/Things/Building/Power"

STEEL = (134, 131, 128)
HOT = (190, 98, 68)
DARK = (40, 40, 40)
BELT = (74, 62, 52)
BRASS = (176, 150, 96)


def P(a, f, z=0.0):
    """Machine coordinates to supersampled pixels, south view."""
    x = (a + M) * CELL * SS
    y = (D - f + M - z * LIFT) * CELL * SS
    return x, y


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3])


def base(name):
    im = Image.open(f"{TEX}/{name}_south.png").convert("RGBA")
    for layer in ("LooseA", "LooseB"):
        path = f"{TEX}/{name}_{layer}_south.png"
        if os.path.exists(path):
            im.alpha_composite(Image.open(path).convert("RGBA"))
    return im


def disc(d, x, y, r, col, outline=None):
    d.ellipse([x - r, y - r, x + r, y + r], fill=col + (255,), outline=(outline + (255,)) if outline else None,
              width=max(2, int(r * 0.12)) if outline else 0)


def fan(d, t, a=1.0, f=2.43, z=0.47, size=0.62):
    """The current cooling fan, for reference."""
    x, y = P(a, f, z)
    R = size * CELL * SS / 2
    disc(d, x, y, R, (14, 14, 14))
    disc(d, x, y, R * 0.93, shade(STEEL, 0.62))
    disc(d, x, y, R * 0.84, (46, 46, 46))
    ang = t * 360 * 2
    for k in range(5):
        a0 = ang + k * 72
        d.pieslice([x - R * 0.84, y - R * 0.84, x + R * 0.84, y + R * 0.84], a0, a0 + 34, fill=shade(STEEL, 1.08) + (255,))
    disc(d, x, y, R * 0.26, shade(STEEL, 0.74))


def governor(d, t, a=0.38, f=0.74, z=0.43):
    """A flyball governor on the steam chest: a spindle, and two brass weights whirling on arms."""
    x, y = P(a, f, z)
    top = P(a, f, z + 0.22)
    s = CELL * SS
    # Spindle column up from the chest cap.
    d.rectangle([x - s * 0.02, top[1], x + s * 0.02, y], fill=shade(STEEL, 0.8) + (255,))
    ang = t * 2 * math.pi * 3
    R = s * 0.13
    for k in (0, math.pi):
        bx, by = top[0] + math.cos(ang + k) * R, top[1] + math.sin(ang + k) * R * 0.55
        d.line([top, (bx, by)], fill=shade(STEEL, 1.1) + (255,), width=int(s * 0.018))
        disc(d, bx, by, s * 0.045, BRASS, outline=(20, 20, 20))
    disc(d, top[0], top[1], s * 0.03, shade(STEEL, 1.25), outline=(20, 20, 20))


def coupling(d, t, a0=0.78, a1=1.22, f0=1.96, f1=2.16, z=0.34, bolts=6):
    """The shaft coupling, from above: bolt heads rolling across the flange's width."""
    x0, y0 = P(a0, f1, z)
    x1, y1 = P(a1, f0, z)
    cx, w = (x0 + x1) / 2, (x1 - x0) / 2
    # The flange face, shaded as a cylinder lying along the shaft.
    for i in range(int(x1 - x0)):
        u = (i - w) / w
        k = 0.7 + 0.5 * math.sqrt(max(0.0, 1 - u * u))
        d.line([(x0 + i, y0), (x0 + i, y1)], fill=shade(STEEL, k) + (255,))
    d.rectangle([x0, y0, x1, y1], outline=(20, 20, 20, 255), width=3)
    ang = t * 2 * math.pi * 2
    for k in range(bolts):
        th = ang + k * 2 * math.pi / bolts
        if math.cos(th) <= 0:
            continue                       # round the back of the shaft
        bx = cx + math.sin(th) * w * 0.85
        rr = (y1 - y0) * 0.3 * (0.45 + 0.55 * math.cos(th))
        disc(d, bx, (y0 + y1) / 2, max(3, rr), shade(STEEL, 1.0 + 0.5 * math.cos(th)), outline=(20, 20, 20))


def blades(d, t, a0=0.64, a1=1.36, f0=1.12, f1=1.38, z=0.37):
    """An inspection hatch in the casing: blade rows streaming across, the shaft under them."""
    x0, y0 = P(a0, f1, z)
    x1, y1 = P(a1, f0, z)
    d.rectangle([x0 - 6, y0 - 6, x1 + 6, y1 + 6], fill=shade(STEEL, 0.85) + (255,), outline=(20, 20, 20, 255), width=4)
    d.rectangle([x0, y0, x1, y1], fill=(26, 26, 28, 255))
    pitch = (x1 - x0) / 7
    off = (t * pitch * 6) % pitch
    for row, yy in enumerate((y0 + (y1 - y0) * 0.3, y0 + (y1 - y0) * 0.72)):
        h = (y1 - y0) * 0.36
        for k in range(-1, 9):
            bx = x0 + k * pitch + off + (pitch / 2 if row else 0)
            poly = [(bx, yy - h / 2), (bx + pitch * 0.45, yy - h / 2), (bx + pitch * 0.2, yy + h / 2), (bx - pitch * 0.25, yy + h / 2)]
            poly = [(min(max(px, x0), x1), py) for px, py in poly]
            d.polygon(poly, fill=shade(STEEL, 1.15) + (255,))
    # Bolted hatch rim.
    for k in range(6):
        bx = x0 + (x1 - x0) * k / 5
        disc(d, bx, y0 - 3, 6 * SS / 2, shade(STEEL, 1.25))
        disc(d, bx, y1 + 3, 6 * SS / 2, shade(STEEL, 1.25))


def flywheel(d, t, a0=0.3, a1=1.7, f0=2.02, f1=2.16, z=0.49):
    """The cobbled flywheel's rim, from above: balance weights and a patch rolling across it."""
    x0, y0 = P(a0, f1, z)
    x1, y1 = P(a1, f0, z)
    # The rim: a dark band, lit along its crown, so what rolls over it stands out.
    d.rectangle([x0, y0, x1, y1], fill=(62, 60, 58, 255), outline=(20, 20, 20, 255), width=4)
    d.rectangle([x0 + 6, y0 + (y1 - y0) * 0.3, x1 - 6, y0 + (y1 - y0) * 0.45], fill=shade(STEEL, 0.95) + (255,))
    # A flywheel turning about the shaft (along f) shows its rim top moving along a.
    period = x1 - x0
    off = (t * period * 1.5) % period
    for base_x, col, wdt in ((0.1, shade(STEEL, 1.4), 0.05), (0.45, (170, 120, 84), 0.12), (0.78, shade(STEEL, 1.4), 0.05)):
        bx = x0 + ((base_x * period + off) % period)
        ww = wdt * period
        # Parts near the ends are going round the edge: squash them.
        edge = min(bx - x0, x1 - bx) / (period / 2)
        ww *= 0.3 + 0.7 * min(1.0, edge * 2.2)
        d.rectangle([bx - ww / 2, y0 + 4, bx + ww / 2, y1 - 4], fill=col + (255,), outline=(20, 20, 20, 255), width=2)


def belt(d, t):
    """Cobbled: a flat belt from a pulley on the flywheel's end to a salvaged dynamo on the flank."""
    s = CELL * SS
    p1 = P(1.78, 2.09, 0.28)                 # drive pulley on the flywheel's end
    p2 = P(1.84, 1.62, 0.36)                 # dynamo pulley, on the spare tank
    for (px, py), r in ((p1, 0.1), (p2, 0.07)):
        disc(d, px, py, r * s, shade(STEEL, 0.75), outline=(20, 20, 20))
    # The two runs of the belt.
    for dx in (-0.075, 0.075):
        d.line([(p1[0] + dx * s, p1[1]), (p2[0] + dx * s * 0.8, p2[1])], fill=BELT + (255,), width=int(s * 0.035))
    # The laced splice, travelling up one run and down the other.
    u = t % 1.0
    run, v = (0, u * 2) if u < 0.5 else (1, (u - 0.5) * 2)
    dx = -0.075 if run == 0 else 0.075
    a, b = (p1, p2) if run == 0 else (p2, p1)
    sx = a[0] + dx * s + (b[0] - a[0]) * v
    sy = a[1] + (b[1] - a[1]) * v
    d.rectangle([sx - s * 0.022, sy - s * 0.012, sx + s * 0.022, sy + s * 0.012], fill=BRASS + (255,))
    # Spokes on both pulleys, turning.
    for (px, py), r, sp in ((p1, 0.1, 1.0), (p2, 0.07, 1.43)):
        ang = t * 2 * math.pi * 2 * sp
        for k in range(3):
            th = ang + k * 2 * math.pi / 3
            d.line([(px, py), (px + math.cos(th) * r * s * 0.8, py + math.sin(th) * r * s * 0.8)],
                   fill=shade(STEEL, 1.2) + (255,), width=int(s * 0.014))
        disc(d, px, py, r * s * 0.25, shade(STEEL, 1.1))


CONCEPTS = [
    ("A_steam_governor_coupling", "STB_SteamTurbine", [governor, coupling, fan],
     "Steam turbine: flyball governor on the steam chest, rolling shaft coupling, cooling fan"),
    ("B_steam_inspection_hatch", "STB_SteamTurbine", [blades, fan],
     "Steam turbine: blades streaming past an inspection hatch, cooling fan"),
    ("C_cobbled_flywheel_belt", "STB_CobbledTurbine", [flywheel, belt, fan],
     "Cobbled turbine: flywheel rim rolling, flat belt to a side dynamo, cooling fan"),
    ("D_cobbled_governor_flywheel", "STB_CobbledTurbine",
     [lambda d, t: governor(d, t, 0.38, 0.74, 0.37), flywheel, fan],
     "Cobbled turbine: scrap flyball governor, flywheel rim rolling, cooling fan"),
]


def render(name, parts):
    b = base(name)
    frames = []
    for i in range(FRAMES):
        t = i / FRAMES
        layer = Image.new("RGBA", (b.width * SS, b.height * SS), (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        for part in parts:
            part(d, t)
        frame = Image.new("RGBA", b.size, (92, 86, 74, 255))
        frame.alpha_composite(b)
        frame.alpha_composite(layer.resize(b.size, Image.LANCZOS))
        frames.append(frame)
    return frames


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    stills = []
    for key, name, parts, caption in CONCEPTS:
        frames = render(name, parts)
        path = os.path.join(out, f"turbine_{key}.gif")
        frames[0].convert("RGB").save(path, save_all=True, append_images=[f.convert("RGB") for f in frames[1:]],
                                      duration=60, loop=0)
        print("wrote", path)
        stills.append((frames[0], caption))
    w = sum(s.width for s, _ in stills) + 10 * (len(stills) + 1)
    sheet = Image.new("RGBA", (w, stills[0][0].height + 20), (92, 86, 74, 255))
    x = 10
    for s, _ in stills:
        sheet.alpha_composite(s, (x, 10))
        x += s.width + 10
    sheet.save(os.path.join(out, "turbine_motion_concepts.png"))


if __name__ == "__main__":
    main()
