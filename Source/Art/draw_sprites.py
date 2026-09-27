"""Draws every texture in this mod. Run from the repo root: python3 Source/Art/draw_sprites.py

Nothing is generated or painted by hand, so the sprites can be rebuilt whenever a footprint
changes. Each one is drawn at SS times its final size and downsampled, which gives soft,
antialiased edges close to RimWorld's airbrushed look. Conventions borrowed from vanilla:
a soft dark outline on the silhouette only, flat tonal ramps for depth, muted colours, and a
3/4 top-down view with the "front" face at the bottom.
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter

SS = 4  # supersampling factor
OUT = "Textures"
OUTLINE = (34, 32, 30, 255)


def rgba(c, a=255):
    return (c[0], c[1], c[2], a)


def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c[:3])


def canvas(px):
    return Image.new("RGBA", (px * SS, px * SS), (0, 0, 0, 0))


def finish(img, px, path, outline=True):
    if outline:
        # Soft dark silhouette ring: grow the alpha, paint it dark, put the art on top.
        a = img.split()[3].point(lambda v: 255 if v > 0 else 0)
        ring = a.filter(ImageFilter.MaxFilter(2 * SS + 1))
        base = Image.new("RGBA", img.size, OUTLINE)
        base.putalpha(ring)
        base.alpha_composite(img)
        img = base
    img = img.resize((px, px), Image.LANCZOS)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print("wrote", path)


def S(v):
    return int(round(v * SS))


def box(d, x0, y0, x1, y1, fill, r=0, edge=None, w=1.2):
    d.rounded_rectangle([S(x0), S(y0), S(x1), S(y1)], radius=S(r), fill=rgba(fill),
                        outline=rgba(edge) if edge else None, width=S(w) if edge else 0)


def vgrad(img, x0, y0, x1, y1, top, bottom, r=0):
    """A vertical ramp from top to bottom colour, clipped to a rounded rect."""
    w, h = S(x1) - S(x0), S(y1) - S(y0)
    g = Image.new("RGBA", (w, h))
    gd = ImageDraw.Draw(g)
    for y in range(h):
        t = y / max(1, h - 1)
        gd.line([(0, y), (w, y)], fill=rgba(tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))))
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, w - 1, h - 1], radius=S(r), fill=255)
    img.paste(g, (S(x0), S(y0)), m)


def circle(d, cx, cy, r, fill, edge=None, w=1.2):
    d.ellipse([S(cx - r), S(cy - r), S(cx + r), S(cy + r)], fill=rgba(fill) if fill else None,
              outline=rgba(edge) if edge else None, width=S(w) if edge else 0)


def bolts(d, pts, c=(150, 146, 138), r=1.3):
    for x, y in pts:
        circle(d, x, y, r, c, shade(c, 0.45), 0.6)


def pipe(d, pts, width, c):
    """A round pipe: dark body, mid core, thin highlight along its upper edge."""
    p = [(S(x), S(y)) for x, y in pts]
    d.line(p, fill=rgba(shade(c, 0.45)), width=S(width + 1.6), joint="curve")
    d.line(p, fill=rgba(c), width=S(width), joint="curve")
    hp = [(S(x), S(y - width * 0.22)) for x, y in pts]
    d.line(hp, fill=rgba(shade(c, 1.35)), width=S(max(1, width * 0.25)), joint="curve")
    for x, y in (pts[0], pts[-1]):
        circle(d, x, y, width / 2 + 0.4, shade(c, 0.8), shade(c, 0.45), 0.6)


# ---------------------------------------------------------------- tier 1: cobbled pellet stove
def cobbled_stove():
    """2x2, 256px. Mismatched scrap plates, a firebox with a glowing grate, a flue stack,
    a drum generator, and a bundle of patched pipes."""
    px = 256
    img = canvas(px)
    d = ImageDraw.Draw(img)
    u = px / 64  # design on a 64-unit grid
    rnd = random.Random(7)

    def U(*v):
        return [x * u for x in v]

    rust, grey, green = (120, 74, 48), (104, 104, 100), (86, 98, 74)
    # Skid it all sits on.
    box(d, *U(4, 44, 60, 60), shade(grey, 0.7), 1.5 * u)
    for x in (8, 20, 32, 44, 56):
        circle(d, *U(x, 58), 0.9 * u, shade(grey, 0.5))

    # Firebox: three mismatched plates welded together, top face lighter than front.
    x0, y0, x1, y1 = U(6, 14, 38, 52)
    fy = 30 * u  # where the top face meets the front face
    plates = [(6, 17, rust), (17, 28, grey), (28, 38, green)]
    for a, b, c in plates:
        vgrad(img, a * u, y0, b * u, fy, shade(c, 1.25), shade(c, 1.05))
        vgrad(img, a * u, fy, b * u, y1, shade(c, 0.9), shade(c, 0.65))
        # Rust streaks and scuffs.
        for _ in range(5):
            sx = rnd.uniform(a + 1, b - 1) * u
            sy = rnd.uniform(fy / u + 1, 33) * u
            d.line([(S(sx), S(sy)), (S(sx), S(sy + rnd.uniform(1.5, 3) * u))],
                   fill=rgba(shade(rust, 0.9), 90), width=S(0.8 * u))
    # Weld seams between plates.
    for x in (17, 28):
        d.line([(S(x * u), S(y0)), (S(x * u), S(y1))], fill=rgba((70, 64, 58)), width=S(0.9 * u))
        for y in range(15, 52, 3):
            circle(d, x * u, y * u, 0.45 * u, (132, 124, 112))
    d.line([(S(x0), S(fy)), (S(x1), S(fy))], fill=rgba((60, 56, 52)), width=S(0.7 * u))
    # A patch plate bolted over the rust panel.
    box(d, *U(8, 18, 14, 25), shade(grey, 1.1), 0.5 * u, shade(grey, 0.5), 0.5 * u)
    bolts(d, [U(9, 19), U(13, 19), U(9, 24), U(13, 24)], r=0.5 * u)

    # Fuel chute on the top face: a sloped funnel.
    d.polygon([tuple(map(S, U(10, 15))), tuple(map(S, U(22, 15))), tuple(map(S, U(20, 22))),
               tuple(map(S, U(12, 22)))], fill=rgba((72, 70, 66)), outline=rgba((40, 38, 36)), width=S(0.6 * u))
    d.polygon([tuple(map(S, U(12.5, 16.5))), tuple(map(S, U(19.5, 16.5))), tuple(map(S, U(18.5, 20.5))),
               tuple(map(S, U(13.5, 20.5)))], fill=rgba((28, 26, 24)))

    # Grate window on the front face, glowing.
    gx0, gy0, gx1, gy1 = U(12, 36, 30, 47)
    box(d, gx0 - u, gy0 - u, gx1 + u, gy1 + u, (58, 54, 50), 1.2 * u)
    vgrad(img, gx0, gy0, gx1, gy1, (255, 196, 90), (206, 72, 20), 0.8 * u)
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse([S(gx0 - 3 * u), S(gy0 - 2 * u), S(gx1 + 3 * u), S(gy1 + 3 * u)],
                                 fill=(255, 130, 40, 70))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(S(2.5 * u))))
    d = ImageDraw.Draw(img)
    for x in range(13, 30, 3):
        d.line([(S(x * u + u), S(gy0)), (S(x * u + u), S(gy1))], fill=rgba((52, 46, 42)), width=S(0.9 * u))
    bolts(d, [U(11, 35), U(31, 35), U(11, 48), U(31, 48)], r=0.6 * u)

    # Generator drum on the right, lying on its side, with cooling fins.
    vgrad(img, *U(40, 30, 58, 50), (150, 146, 128), (84, 82, 72), 3 * u)
    d = ImageDraw.Draw(img)
    for x in range(42, 57, 2):
        d.line([(S(x * u), S(31 * u)), (S(x * u), S(49 * u))], fill=rgba((70, 68, 60)), width=S(0.5 * u))
    box(d, *U(38, 36, 41, 44), (96, 92, 84), 0.6 * u, (48, 46, 42), 0.5 * u)  # coupling to firebox
    # A crude junction box with a mismatched cover.
    box(d, *U(46, 22, 56, 30), (140, 120, 60), 0.8 * u, (70, 60, 30), 0.6 * u)
    d.line([(S(47 * u), S(26 * u)), (S(55 * u), S(26 * u))], fill=rgba((90, 76, 36)), width=S(0.5 * u))

    # Flue: elbow out of the firebox top and up into a round stack seen from above.
    pipe(d, [U(30, 20), U(40, 20), U(48, 14)], 3.2 * u, (110, 106, 100))
    circle(d, *U(52, 10), 6.5 * u, (96, 92, 88), (40, 38, 36), 0.8 * u)
    circle(d, *U(52, 10), 5 * u, (122, 116, 108))
    circle(d, *U(52, 10), 3.4 * u, (20, 18, 18))
    soot = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(soot).ellipse([S(46 * u), S(4 * u), S(58 * u), S(16 * u)], outline=(30, 28, 26, 110), width=S(1.2 * u))
    img.alpha_composite(soot.filter(ImageFilter.GaussianBlur(S(0.6 * u))))
    d = ImageDraw.Draw(img)
    # Wire straps round the stack.
    d.arc([S(45.5 * u), S(3.5 * u), S(58.5 * u), S(16.5 * u)], 200, 340, fill=rgba((150, 140, 110)), width=S(0.6 * u))

    # Bundle of salvaged pipes up the left side, with a valve wheel and a taped joint.
    pipe(d, [U(3, 50), U(3, 12), U(10, 6), U(30, 6)], 2.4 * u, (92, 110, 118))
    pipe(d, [U(6.5, 50), U(6.5, 14)], 1.8 * u, (128, 96, 64))
    box(d, *U(1.2, 26, 4.8, 30), (170, 160, 120), 0.4 * u)  # taped joint
    circle(d, *U(18, 6), 2.6 * u, None, (170, 50, 40), 0.9 * u)
    d.line([(S(15.6 * u), S(6 * u)), (S(20.4 * u), S(6 * u))], fill=rgba((170, 50, 40)), width=S(0.6 * u))
    d.line([(S(18 * u), S(3.6 * u)), (S(18 * u), S(8.4 * u))], fill=rgba((170, 50, 40)), width=S(0.6 * u))

    finish(img, px, f"{OUT}/Things/Building/Power/STB_CobbledPelletStove.png")


# ---------------------------------------------------------------- tier 2: gasifier
def gasifier():
    """2x2, 256px. A clean enclosed housing: gasifier chamber, catalytic filter stack, tidy
    parallel pipes and a teal status panel. Reads as the same machine, grown up."""
    px = 256
    img = canvas(px)
    d = ImageDraw.Draw(img)
    u = px / 64

    def U(*v):
        return [x * u for x in v]

    body = (112, 118, 124)
    teal = (70, 200, 190)
    # Housing: top face then front face.
    vgrad(img, *U(5, 12, 59, 34), shade(body, 1.3), shade(body, 1.1), 3 * u)
    vgrad(img, *U(5, 30, 59, 56), shade(body, 0.95), shade(body, 0.7), 3 * u)
    d = ImageDraw.Draw(img)
    d.line([(S(6 * u), S(31 * u)), (S(58 * u), S(31 * u))], fill=rgba(shade(body, 0.55)), width=S(0.6 * u))
    # Base plinth.
    box(d, *U(4, 54, 60, 60), shade(body, 0.55), 1.5 * u)

    # Gasifier chamber: a big round lid on the top face, with a teal seal ring.
    circle(d, *U(20, 21), 9 * u, shade(body, 0.9), shade(body, 0.5), 0.8 * u)
    circle(d, *U(20, 21), 7.2 * u, None, teal, 0.9 * u)
    circle(d, *U(20, 21), 5.6 * u, shade(body, 1.35))
    circle(d, *U(20, 21), 2 * u, shade(body, 0.8))
    bolts(d, [(20 * u + math.cos(a) * 8.1 * u, 21 * u + math.sin(a) * 8.1 * u)
              for a in [i * math.pi / 4 for i in range(8)]], c=(170, 176, 182), r=0.5 * u)

    # Catalytic filter stack: three small cylinders in a row.
    for i, x in enumerate((38, 45, 52)):
        circle(d, *U(x, 19), 3.2 * u, shade(body, 0.85), shade(body, 0.5), 0.6 * u)
        circle(d, *U(x, 19), 2.2 * u, (190, 196, 200))
        circle(d, *U(x, 19), 1.1 * u, (150, 156, 160))
    pipe(d, [U(34, 26), U(56, 26)], 1.6 * u, (160, 166, 172))

    # Front face: a fuel intake hatch with hazard stripes, a status panel, and vents.
    box(d, *U(9, 36, 25, 50), shade(body, 0.6), 1 * u, shade(body, 0.4), 0.6 * u)
    stripes = Image.new("RGBA", img.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(stripes)
    for k in range(-6, 12):
        x = (9 + k * 2.5) * u
        sd.polygon([(S(x), S(36 * u)), (S(x + 1.25 * u), S(36 * u)), (S(x + 1.25 * u + 2 * u), S(38.2 * u)),
                    (S(x + 2 * u), S(38.2 * u))], fill=(220, 180, 50, 255))
    m = Image.new("L", img.size, 0)
    ImageDraw.Draw(m).rectangle([S(9 * u), S(36 * u), S(25 * u), S(38.2 * u)], fill=255)
    img.paste(stripes, (0, 0), Image.composite(stripes.split()[3], Image.new("L", img.size, 0), m))
    d = ImageDraw.Draw(img)
    box(d, *U(12, 40, 22, 47), shade(body, 0.4), 0.6 * u)
    d.line([(S(13 * u), S(43.5 * u)), (S(21 * u), S(43.5 * u))], fill=rgba(shade(body, 0.9)), width=S(0.8 * u))

    box(d, *U(30, 37, 42, 45), (30, 40, 44), 0.8 * u, shade(body, 0.4), 0.5 * u)
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.rounded_rectangle([S(31 * u), S(38 * u), S(41 * u), S(44 * u)], radius=S(0.5 * u), fill=rgba(teal, 90))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(S(0.8 * u))))
    d = ImageDraw.Draw(img)
    d.line([(S(32 * u), S(42 * u)), (S(34.5 * u), S(40 * u)), (S(37 * u), S(41.5 * u)), (S(40 * u), S(39 * u))],
           fill=rgba(teal), width=S(0.6 * u))
    circle(d, *U(47, 41), 1.4 * u, (80, 220, 120))
    circle(d, *U(51, 41), 1.4 * u, (70, 90, 80))
    for y in range(47, 53, 2):
        d.line([(S(30 * u), S(y * u)), (S(55 * u), S(y * u))], fill=rgba(shade(body, 0.45)), width=S(0.6 * u))
    bolts(d, [U(7, 33), U(57, 33), U(7, 53), U(57, 53)], c=(170, 176, 182), r=0.6 * u)

    # Tidy pipework along the back edge and out the side.
    pipe(d, [U(6, 8), U(58, 8)], 2.2 * u, (150, 156, 164))
    pipe(d, [U(58, 8), U(61, 11), U(61, 40)], 2.2 * u, (150, 156, 164))
    pipe(d, [U(10, 5.5), U(54, 5.5)], 1.4 * u, (96, 150, 150))

    finish(img, px, f"{OUT}/Things/Building/Power/STB_TrashbrickGasifier.png")


# ---------------------------------------------------------------- fuel hopper
def fuel_hopper():
    """1x1, 128px. A square steel frame round a funnel, seen from above, patched like the stove."""
    px = 128
    img = canvas(px)
    d = ImageDraw.Draw(img)
    u = px / 32

    def U(*v):
        return [x * u for x in v]

    frame = (108, 104, 98)
    box(d, *U(2, 2, 30, 30), frame, 1.5 * u)
    # Funnel: nested squares getting darker toward the throat.
    for i, f in enumerate((0.9, 0.78, 0.66, 0.54, 0.42)):
        a = 4 + i * 2.4
        box(d, a * u, a * u, (32 - a) * u, (32 - a) * u, shade(frame, f), 0.8 * u)
    box(d, *U(14, 14, 18, 18), (20, 18, 16), 0.5 * u)
    for a, b in ((4, 4), (28, 4), (4, 28), (28, 28)):
        d.line([(S(a * u), S(b * u)), (S((16 + (a - 16) * 0.2) * u), S((16 + (b - 16) * 0.2) * u))],
               fill=rgba(shade(frame, 0.5)), width=S(0.5 * u))
    # A rusty patch and corner bolts.
    box(d, *U(20, 3, 28, 6.5), (120, 74, 48), 0.4 * u, (70, 44, 30), 0.4 * u)
    bolts(d, [U(4, 4), U(28, 4), U(4, 28), U(28, 28)], r=0.8 * u)
    finish(img, px, f"{OUT}/Things/Building/Power/STB_FuelHopper.png")


# ---------------------------------------------------------------- sludge pellets
def pellets():
    """Graphic_StackCount: three piles, small to large, 64px each."""
    px = 64
    for name, n, seed in (("a", 5, 1), ("b", 11, 2), ("c", 20, 3)):
        img = canvas(px)
        d = ImageDraw.Draw(img)
        rnd = random.Random(seed)
        u = px / 32
        spread = {5: 6, 11: 10, 20: 14}[n]
        pts = sorted(((16 + rnd.gauss(0, spread / 2), 17 + rnd.gauss(0, spread / 3)) for _ in range(n)),
                     key=lambda p: p[1])
        for x, y in pts:
            ang = rnd.uniform(0, math.pi)
            ln, r = 4.6, 2.0
            dx, dy = math.cos(ang) * ln / 2, math.sin(ang) * ln / 2 * 0.6
            base = (104 + rnd.randint(-10, 10), 76 + rnd.randint(-8, 8), 48 + rnd.randint(-6, 6))
            d.line([(S((x - dx) * u), S((y - dy) * u)), (S((x + dx) * u), S((y + dy) * u))],
                   fill=rgba(shade(base, 0.8)), width=S(2 * r * u))
            for cx, cy in ((x - dx, y - dy), (x + dx, y + dy)):
                circle(d, cx * u, cy * u, r * u, shade(base, 0.8))
            d.line([(S((x - dx) * u), S((y - dy - 0.5) * u)), (S((x + dx) * u), S((y + dy - 0.5) * u))],
                   fill=rgba(shade(base, 1.2)), width=S(0.9 * u))
        finish(img, px, f"{OUT}/Things/Item/Resource/STB_SludgePellets/STB_SludgePellets_{name}.png")


if __name__ == "__main__":
    cobbled_stove()
    gasifier()
    fuel_hopper()
    pellets()
