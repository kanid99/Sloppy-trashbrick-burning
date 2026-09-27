"""Draws every texture in this mod. Run from the repo root:

    python3 Source/Art/draw_sprites.py

Each machine is laid out ONCE in its own coordinates (`a` across its front, `f` from front to
back, in cells) and `View` places that layout for north, east, south and west. Drawing is always
upright, so the light stays at the top of the screen in every view. See stb_draw.py for the rules.

Every machine has ONE input and no output: a single intake port, centred on the edge it faces,
marked with an in-pointing chevron in VFE Factory's input green. CompHopperFeed reads fuel from
the cells just outside that edge; verify_art.py checks the chevron lands there in all four views.
"""
import math
import random

from PIL import Image, ImageDraw

from stb_draw import CELL, LIFT, SEAM, SS, Canvas, View, mix, shade

OUT = "Textures"
ROTS = ("north", "east", "south", "west")
MARGIN_2X2 = 0.5   # 2x2 machines draw at (3,3)
MARGIN_1X1 = 0.25  # the hopper draws at (1.5,1.5)

INTAKE_GREEN = (91, 175, 94)   # VFE Factory's input rail colour
STEEL = (134, 131, 128)        # VFE chassis face (138,134,132)->(119,115,113)
DECK = (96, 94, 92)
RUST = (126, 98, 82)           # desaturated: rust as a tone, not a colour
OLIVE = (112, 116, 102)
FIRE_HOT, FIRE_DEEP = (255, 200, 110), (214, 92, 36)
TEAL = (92, 156, 150)          # subdued, as in the mending repair centre
INTAKE_Z, INTAKE_H = 0.07, 0.12  # the port stands on the skid; verify_art.py allows for its lift


def intake(c, width, depth, z0, body):
    """The single intake port, centred on the front edge: a dark throat under a lit lip, with
    a flat chevron pointing IN. Chevrons follow the material, not the edge they sit on."""
    W = c.v.W
    a0, a1 = W / 2 - width / 2, W / 2 + width / 2

    def top(box, lift):
        x0, y0, x1, y1 = box
        d = c.d
        inset = c.px(0.035)
        throat = [x0 + inset, y0 + inset, x1 - inset, y1 - inset]
        c.ramp(throat, (40, 40, 40), (58, 58, 58), c.px(0.02))
        # Rails either side of the throat, in the input green, like a VFE input bay.
        horiz = c.v.along_a()
        rail = c.px(0.03)
        if horiz:
            d.rectangle([throat[0], throat[1], throat[0] + rail, throat[3]], fill=INTAKE_GREEN + (255,))
            d.rectangle([throat[2] - rail, throat[1], throat[2], throat[3]], fill=INTAKE_GREEN + (255,))
        else:
            d.rectangle([throat[0], throat[1], throat[2], throat[1] + rail], fill=INTAKE_GREEN + (255,))
            d.rectangle([throat[0], throat[3] - rail, throat[2], throat[3]], fill=INTAKE_GREEN + (255,))
        # Chevron: a flat triangle pointing from the front edge towards the back.
        fx, fy = c.v.pt(W / 2, 0)
        bx, by = c.v.pt(W / 2, depth)
        cx, cy = (fx + bx) / 2, (fy + by) / 2 - lift
        ux, uy = bx - fx, by - fy
        n = math.hypot(ux, uy)
        ux, uy = ux / n, uy / n
        s = min(width, depth) * 0.28
        tip = (cx + ux * s * 0.6, cy + uy * s * 0.6)
        l = (cx - ux * s * 0.4 - uy * s * 0.8, cy - uy * s * 0.4 + ux * s * 0.8)
        r = (cx - ux * s * 0.4 + uy * s * 0.8, cy - uy * s * 0.4 - ux * s * 0.8)
        d.polygon([(c.px(p[0]), c.px(p[1])) for p in (tip, l, r)], fill=INTAKE_GREEN + (255,))

    c.slab(a0, 0.02, a1, depth, z0, INTAKE_H, shade(body, 0.95), top_fn=top, radius=0.03)


def skid(c, col, chamfer):
    W, D = c.v.W, c.v.D

    def top(box, lift):
        # Bolt pads round the edge: greebles, a tone step off the deck.
        x0, y0, x1, y1 = box
        inset = c.px(0.09)
        for t in (0.2, 0.4, 0.6, 0.8):
            for (px, py) in ((x0 + inset, y0 + (y1 - y0) * t), (x1 - inset, y0 + (y1 - y0) * t),
                             (x0 + (x1 - x0) * t, y1 - inset)):
                c.dots([(px / (CELL * SS), py / (CELL * SS))], 0.018, shade(col, 1.18))

    c.slab(0.05, 0.05, W - 0.05, D - 0.05, 0, 0.07, col, chamfer=chamfer, radius=0, top_fn=top)


# ------------------------------------------------------------------ tier 1: cobbled pellet stove
def cobbled_stove(rot):
    """Salvage: three mismatched plates welded into a firebox, a scavenged generator drum, a
    flue stack. The fire is its one accent and the only saturated colour on it."""
    v = View(rot, 2, 2, MARGIN_2X2)
    c = Canvas(v)
    rnd = random.Random(11)
    skid(c, DECK, chamfer=0.2)
    intake(c, 0.62, 0.34, INTAKE_Z, STEEL)

    # Firebox: three plates of different scrap side by side, welded. Each is its own slab, so
    # in the side views the nearest plate's face hides the others' walls by itself.
    plates = [(0.36, 0.72, RUST), (0.72, 1.08, STEEL), (1.08, 1.46, OLIVE)]
    FB0, FB1 = 0.5, 1.3   # firebox front and back, clear of the intake
    for i, (a0, a1, col) in enumerate(plates):
        def top(box, lift, col=col, i=i, a0=a0, a1=a1):
            x0, y0, x1, y1 = box
            # Weld beads along the seam with the next plate: a row of dots two steps lighter.
            if i < 2:
                sx, sy0 = v.pt(a1, FB0)
                ex, ey = v.pt(a1, FB1)
                n = 9
                pts = [(sx + (ex - sx) * k / n, sy0 + (ey - sy0) * k / n - lift) for k in range(n + 1)]
                c.dots(pts, 0.012, shade(col, 1.25))
            # Streaks and scuffs, faint tone marks.
            for _ in range(4):
                px = rnd.uniform(x0 + (x1 - x0) * 0.15, x1 - (x1 - x0) * 0.15)
                py = rnd.uniform(y0 + (y1 - y0) * 0.15, y1 - (y1 - y0) * 0.3)
                c.d.line([(px, py), (px, py + c.px(0.06))], fill=shade(col, 0.86) + (255,),
                         width=c.px(0.012))
        c.slab(a0, FB0, a1, FB1, 0.07, 0.34, col, top_fn=top, radius=0.02)
    # A patch bolted over the rust plate.
    def patch_top(box, lift):
        x0, y0, x1, y1 = box
        m = c.px(0.02)
        pts = [(x0 + m, y0 + m), (x1 - m, y0 + m), (x0 + m, y1 - m), (x1 - m, y1 - m)]
        c.dots([(x / (CELL * SS), y / (CELL * SS)) for x, y in pts], 0.012, shade(STEEL, 1.2))
    c.slab(0.42, 0.98, 0.64, 1.2, 0.41, 0.02, shade(STEEL, 1.1), top_fn=patch_top, shadow=False, radius=0.01)

    # Fire window on top of the firebox: the one accent. A grate of bars over a hot ramp.
    def fire_top(box, lift):
        x0, y0, x1, y1 = box
        c.glow([x0 - c.px(0.06), y0 - c.px(0.06), x1 + c.px(0.06), y1 + c.px(0.06)], FIRE_DEEP, c.px(0.05), 90)
        c.ramp([x0 + c.px(0.02), y0 + c.px(0.02), x1 - c.px(0.02), y1 - c.px(0.02)], FIRE_HOT, FIRE_DEEP)
        bars = 5
        if v.along_a():
            for k in range(1, bars):
                x = x0 + (x1 - x0) * k / bars
                c.seam((x, y0), (x, y1), width=4 / 192, tone=(62, 50, 44))
        else:
            for k in range(1, bars):
                y = y0 + (y1 - y0) * k / bars
                c.seam((x0, y), (x1, y), width=4 / 192, tone=(62, 50, 44))
    c.slab(0.78, 0.62, 1.3, 0.98, 0.41, 0.015, (70, 64, 60), top_fn=fire_top, shadow=False, radius=0.02)

    # Flue stack at the back left: the round form. Soot inside, banded wall.
    def flue_cap(X, Y, R):
        d = c.d
        r = int(R * 0.62)
        d.ellipse([X - r, Y - r - R // 10, X + r, Y + r - R // 10], fill=(34, 32, 30, 255))
        r2 = int(R * 0.45)
        d.ellipse([X - r2, Y - r2 - R // 14, X + r2, Y + r2 - R // 14], fill=(24, 22, 20, 255))
    c.cylinder(0.62, 1.62, 0.24, 0.07, 0.62, shade(STEEL, 1.02), rings=3, cap_fn=flue_cap)

    # Generator drum at the back right, lying along `a`, cooling fins across it.
    def drum_top(box, lift):
        x0, y0, x1, y1 = box
        fins = 7
        if v.along_a():
            for k in range(1, fins):
                x = x0 + (x1 - x0) * k / fins
                c.seam((x, y0 + c.px(0.02)), (x, y1 - c.px(0.02)), width=3 / 192, tone=shade(OLIVE, 0.72))
        else:
            for k in range(1, fins):
                y = y0 + (y1 - y0) * k / fins
                c.seam((x0 + c.px(0.02), y), (x1 - c.px(0.02), y), width=3 / 192, tone=shade(OLIVE, 0.72))
    c.slab(1.06, 1.42, 1.86, 1.86, 0.07, 0.3, shade(OLIVE, 1.05), top_fn=drum_top, radius=0.1)
    c.slab(1.18, 1.3, 1.36, 1.42, 0.14, 0.14, shade(STEEL, 0.9), radius=0.02)   # coupling

    # Junction box on the right flank, and salvaged pipes up the left: straight runs only.
    def jbox_top(box, lift):
        x0, y0, x1, y1 = box
        horiz = v.along_a()
        for k in range(3):
            t = 0.3 + 0.2 * k
            if horiz:
                c.seam((x0 + c.px(0.03), y0 + (y1 - y0) * t), (x1 - c.px(0.03), y0 + (y1 - y0) * t),
                       tone=shade(STEEL, 0.78))
            else:
                c.seam((x0 + (x1 - x0) * t, y0 + c.px(0.03)), (x0 + (x1 - x0) * t, y1 - c.px(0.03)),
                       tone=shade(STEEL, 0.78))
    c.slab(1.56, 0.56, 1.88, 1.16, 0.07, 0.2, shade(STEEL, 0.92), top_fn=jbox_top, radius=0.02)
    c.pipe(0.14, 0.44, 0.14, 1.84, 0.07, 0.085, (104, 116, 120))
    c.pipe(0.26, 0.5, 0.26, 1.3, 0.07, 0.065, shade(RUST, 1.05))
    c.flush()
    c.save(f"{OUT}/Things/Building/Power/STB_CobbledPelletStove_{rot}.png")


# ------------------------------------------------------------------ tier 2: gasifier
def gasifier(rot):
    """The same job done properly: one housing, a sealed chamber, a filter bank, a turbine.
    Teal marks what is powered - the chamber seal, the status panel and the pilot lamp."""
    v = View(rot, 2, 2, MARGIN_2X2)
    c = Canvas(v)
    body = (138, 140, 142)
    skid(c, shade(body, 0.62), chamfer=0.26)
    intake(c, 0.62, 0.34, INTAKE_Z, body)

    def housing_top(box, lift):
        x0, y0, x1, y1 = box
        # Vent slats in the corners the chamber doesn't cover: a rack of short marks.
        for (fa0, ff0, fa1, ff1) in ((1.3, 0.5, 1.76, 0.72),):
            vx0, vy0, vx1, vy1 = v.rect(fa0, ff0, fa1, ff1)
            n = 6
            for k in range(n):
                if v.along_a():
                    x = vx0 + (vx1 - vx0) * (k + 0.5) / n
                    c.seam((c.px(x), c.px(vy0 - lift / 1)), (c.px(x), c.px(vy1 - lift)), width=5 / 192,
                           tone=shade(body, 0.78))
                else:
                    y = vy0 + (vy1 - vy0) * (k + 0.5) / n
                    c.seam((c.px(vx0), c.px(y - lift)), (c.px(vx1), c.px(y - lift)), width=5 / 192,
                           tone=shade(body, 0.78))

    c.slab(0.16, 0.44, 1.84, 1.5, 0.07, 0.32, body, top_fn=lambda b, l: housing_top(b, l), radius=0.08,
           chamfer=0.1)

    # Status panel on the housing, near the front right: a dark screen with a teal trace.
    def panel_top(box, lift):
        x0, y0, x1, y1 = box
        c.glow(box, TEAL, c.px(0.02), 70)
        m = c.px(0.025)
        c.ramp([x0 + m, y0 + m, x1 - m, y1 - m], (34, 44, 46), (28, 36, 38), c.px(0.01))
        if v.along_a():
            pts = [(x0 + (x1 - x0) * t, y0 + (y1 - y0) * (0.5 + 0.25 * math.sin(t * 9))) for t in
                   [0.15 + 0.07 * k for k in range(11)]]
        else:
            pts = [(x0 + (x1 - x0) * (0.5 + 0.25 * math.sin(t * 9)), y0 + (y1 - y0) * t) for t in
                   [0.15 + 0.07 * k for k in range(11)]]
        c.d.line(pts, fill=TEAL + (255,), width=c.px(0.012))
    c.slab(1.28, 0.8, 1.74, 1.08, 0.39, 0.03, (70, 74, 78), top_fn=panel_top, shadow=False, radius=0.02)
    # Pilot lamp beside it.
    lx, ly = v.pt(1.51, 1.26)
    c.add(ly, lambda: c.dots([(lx, ly - 0.42 * LIFT)], 0.03, (120, 214, 170)), 0.42)

    # Gasifier chamber: the round form, a teal seal ring and a bolt circle.
    def chamber_cap(X, Y, R):
        d = c.d
        w = max(2, c.px(5 / 192))
        r = int(R * 0.8)
        d.ellipse([X - r, Y - r - R // 12, X + r, Y + r - R // 12], outline=TEAL + (255,), width=w)
        for k in range(10):
            ang = k * math.pi / 5
            bx, by = X + math.cos(ang) * R * 0.9, Y - R // 20 + math.sin(ang) * R * 0.9
            rr = c.px(0.013)
            d.ellipse([bx - rr, by - rr, bx + rr, by + rr], fill=shade(body, 1.22) + (255,))
        r2 = int(R * 0.25)
        d.ellipse([X - r2, Y - r2 - R // 10, X + r2, Y + r2 - R // 10], fill=shade(body, 0.82) + (255,))
    c.cylinder(0.72, 1.0, 0.38, 0.39, 0.14, shade(body, 1.05), rings=1, cap_fn=chamber_cap)

    # Filter bank across the back: three identical canisters, the repeated mark.
    for a in (1.12, 1.4, 1.68):
        c.cylinder(a, 1.34, 0.12, 0.39, 0.2, (176, 180, 184), rings=1)

    # Turbine exhaust at the back left, and a straight manifold along the back edge.
    c.cylinder(0.36, 1.7, 0.15, 0.07, 0.5, shade(body, 1.0), rings=2,
               cap_fn=lambda X, Y, R: c.d.ellipse([X - int(R * .55), Y - int(R * .55) - R // 10,
                                                   X + int(R * .55), Y + int(R * .55) - R // 10],
                                                  fill=(40, 42, 44, 255)))
    c.pipe(0.6, 1.66, 1.86, 1.66, 0.07, 0.09, (150, 156, 164))
    c.pipe(0.6, 1.82, 1.86, 1.82, 0.07, 0.07, shade(TEAL, 0.9))
    c.flush()
    c.save(f"{OUT}/Things/Building/Power/STB_TrashbrickGasifier_{rot}.png")


# ------------------------------------------------------------------ fuel hopper
def fuel_hopper(rot):
    """1x1. A steel box round a funnel, with its spout on the edge it faces: point it at a
    stove's intake. Patched like the cobbled stove."""
    v = View(rot, 1, 1, MARGIN_1X1)
    c = Canvas(v)

    def top(box, lift):
        x0, y0, x1, y1 = box
        # Funnel: nested tone steps getting darker toward a throat set toward the spout.
        tx, ty = v.pt(0.5, 0.36)
        tx, ty = c.px(tx), c.px(ty - lift)
        for k, f in enumerate((0.9, 0.8, 0.7, 0.6, 0.5, 0.4)):
            t = k / 6
            bx0 = x0 + (tx - x0) * t + c.px(0.06) * (1 - t)
            by0 = y0 + (ty - y0) * t + c.px(0.06) * (1 - t)
            bx1 = x1 + (tx - x1) * t - c.px(0.06) * (1 - t)
            by1 = y1 + (ty - y1) * t - c.px(0.06) * (1 - t)
            c.d.rounded_rectangle([bx0, by0, bx1, by1], radius=c.px(0.02), fill=shade(STEEL, f) + (255,))
        r = c.px(0.07)
        c.d.rounded_rectangle([tx - r, ty - r, tx + r, ty + r], radius=c.px(0.015), fill=(30, 30, 30, 255))
        c.dots([(p[0], p[1] - lift) for p in (v.pt(0.12, 0.12), v.pt(0.88, 0.12), v.pt(0.12, 0.88),
                                              v.pt(0.88, 0.88))], 0.022, shade(STEEL, 1.2))

    # Spout toward the facing edge, then the box over it.
    c.slab(0.36, 0.02, 0.64, 0.2, 0.0, 0.14, shade(STEEL, 0.85), radius=0.02)
    c.slab(0.08, 0.14, 0.92, 0.92, 0.0, 0.24, STEEL, chamfer=0.1, radius=0, top_fn=top)
    # A rust patch on the back rim.
    c.slab(0.5, 0.8, 0.8, 0.9, 0.24, 0.01, RUST, shadow=False, radius=0.01)
    c.flush()
    c.save(f"{OUT}/Things/Building/Power/STB_FuelHopper_{rot}.png")


# ------------------------------------------------------------------ sludge pellets
def pellets():
    """Graphic_StackCount: three piles, small to large. Items are 128px, one cell."""
    px = 128
    for name, n, seed in (("a", 5, 1), ("b", 11, 2), ("c", 20, 3)):
        img = Image.new("RGBA", (px * SS, px * SS), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        rnd = random.Random(seed)
        u = px * SS / 32
        spread = {5: 6, 11: 10, 20: 14}[n]
        pts = sorted(((16 + rnd.gauss(0, spread / 2), 17 + rnd.gauss(0, spread / 3)) for _ in range(n)),
                     key=lambda p: p[1])
        for x, y in pts:
            ang = rnd.uniform(0, math.pi)
            ln, r = 4.6, 2.0
            dx, dy = math.cos(ang) * ln / 2, math.sin(ang) * ln / 2 * 0.6
            base = (104 + rnd.randint(-10, 10), 80 + rnd.randint(-8, 8), 58 + rnd.randint(-6, 6))
            # Tone only: a darker body, a lit ridge offset up. No outlines between pellets.
            for off, f in ((0.5, 0.62), (0, 0.9), (-0.45, 1.12)):
                w = r * (1 if f < 1 else 0.55)
                d.line([((x - dx) * u, (y - dy + off) * u), ((x + dx) * u, (y + dy + off) * u)],
                       fill=shade(base, f) + (255,), width=int(2 * w * u))
                for ex, ey in ((x - dx, y - dy + off), (x + dx, y + dy + off)):
                    d.ellipse([(ex - w) * u, (ey - w) * u, (ex + w) * u, (ey + w) * u], fill=shade(base, f) + (255,))
        from stb_draw import SILHOUETTE
        from PIL import ImageFilter
        a = img.split()[3].point(lambda v: 255 if v > 40 else 0)
        ring = a.filter(ImageFilter.MaxFilter(2 * 3 * SS // 2 + 1))
        base_img = Image.new("RGBA", img.size, SILHOUETTE + (255,))
        base_img.putalpha(ring)
        base_img.alpha_composite(img)
        out = base_img.resize((px, px), Image.LANCZOS)
        path = f"{OUT}/Things/Item/Resource/STB_SludgePellets/STB_SludgePellets_{name}.png"
        import os
        os.makedirs(os.path.dirname(path), exist_ok=True)
        out.save(path)
        print("wrote", path, out.size)


if __name__ == "__main__":
    for r in ROTS:
        cobbled_stove(r)
        gasifier(r)
        fuel_hopper(r)
    pellets()
