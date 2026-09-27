"""Shared drawing primitives for this mod's sprites.

The rules are the ones the SloppyMods mending and Riimba art settled on (see README.md here):

* Drawn at SS x and reduced with LANCZOS, because PIL's shapes are hard-aliased.
* Black is the silhouette and nothing else. Every division inside a sprite is a SEAM - a thin
  (49,49,49)-ish line - or a plain tone step. Nothing is outlined to make it look raised.
* Height is a lit top face, a darker side WALL under it, and a soft shadow cast down and right.
  The light never moves: it comes from the top of the screen in every rotation.
* Rotations are VIEWS. A layout is written once in the machine's own coordinates and placed
  per rotation by `View`, then drawn upright - pixels are never rotated, so the lighting stays put.
* Straight pipe runs only. Bends read as debris at play zoom.
"""
import os

from PIL import Image, ImageDraw, ImageFilter

SS = 4             # supersampling factor
CELL = 192         # final pixels per cell, as in the mending and Riimba sprites
SILHOUETTE = (14, 14, 14)
SEAM = (49, 49, 49)
# How far up the screen one cell of height is drawn. VFE's machines are shallow - a tall part
# rises well under its own footprint - and at 1.0 anything tall behind a port on the far edge
# (the north view) covered it completely.
LIFT = 0.55


def shade(c, f):
    return tuple(max(0, min(255, int(round(v * f)))) for v in c[:3])


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


class View:
    """Maps machine coordinates to one rotation's canvas.

    Machine coordinates, in cells: `a` runs across the machine's FRONT from its left to its
    right as you face it, `f` runs from the front (f=0) to the back. The front is the side the
    building faces, so for `south` it is the bottom of the texture.

    Map z runs up and screen y runs down, so north's front is the TOP of the texture.
    """

    def __init__(self, rot, width, depth, margin=0.0):
        self.rot, self.W, self.D, self.M = rot, width, depth, margin
        fw, fh = (width, depth) if rot in ("north", "south") else (depth, width)
        # The canvas is the footprint plus a transparent margin all round, so raised parts can
        # rise above the footprint without being clipped. drawSize = size + 2 * margin, the way
        # VFE draws its machines a cell larger than they stand.
        self.cols, self.rows = fw + 2 * margin, fh + 2 * margin

    def pt(self, a, f):
        W, D, M = self.W, self.D, self.M
        x, y = {
            "south": (a, D - f),
            "north": (W - a, f),
            "east": (D - f, W - a),
            "west": (f, a),
        }[self.rot]
        return x + M, y + M

    def rect(self, a0, f0, a1, f1):
        (x0, y0), (x1, y1) = self.pt(a0, f0), self.pt(a1, f1)
        return min(x0, x1), min(y0, y1), max(x0, x1), max(y0, y1)

    def along_a(self):
        """True if the machine's `a` axis runs left-right on this canvas."""
        return self.rot in ("north", "south")


class Canvas:
    def __init__(self, view):
        self.v = view
        self.img = Image.new("RGBA", (round(view.cols * CELL * SS), round(view.rows * CELL * SS)), (0, 0, 0, 0))
        self.jobs = []

    # --- units
    @staticmethod
    def px(cells):
        return int(round(cells * CELL * SS))

    def box_px(self, x0, y0, x1, y1):
        return [self.px(x0), self.px(y0), self.px(x1), self.px(y1)]

    @property
    def d(self):
        return ImageDraw.Draw(self.img)

    # --- queued drawing, painter's order by the screen line each element stands on
    def add(self, key, fn, z=0.0):
        """Painter's order: lower parts first, then back to front along the screen line each
        part stands on. A base plate must never be sorted over what stands on it."""
        self.jobs.append(((round(z, 3), key), len(self.jobs), fn))

    def flush(self):
        for _, _, fn in sorted(self.jobs, key=lambda j: (j[0], j[1])):
            fn()
        self.jobs = []

    # --- shading helpers
    def ramp(self, box, top, bottom, radius=0, chamfer=0):
        x0, y0, x1, y1 = box
        w, h = max(1, x1 - x0), max(1, y1 - y0)
        g = Image.new("RGBA", (w, h))
        gd = ImageDraw.Draw(g)
        for y in range(h):
            gd.line([(0, y), (w, y)], fill=mix(top, bottom, y / max(1, h - 1)) + (255,))
        m = Image.new("L", (w, h), 0)
        md = ImageDraw.Draw(m)
        if chamfer:
            c = min(chamfer, w // 2, h // 2)
            md.polygon([(c, 0), (w - 1 - c, 0), (w - 1, c), (w - 1, h - 1 - c), (w - 1 - c, h - 1),
                        (c, h - 1), (0, h - 1 - c), (0, c)], fill=255)
        else:
            md.rounded_rectangle([0, 0, w - 1, h - 1], radius=radius, fill=255)
        self.img.paste(g, (x0, y0), m)

    def cast_shadow(self, box, radius=0, strength=80, reach=0.05):
        """Soft shadow down and right of a raised part, painted before the part itself."""
        x0, y0, x1, y1 = box
        o = self.px(reach)
        sh = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        ImageDraw.Draw(sh).rounded_rectangle([x0 + o, y0 + o, x1 + o, y1 + o], radius=radius,
                                             fill=(0, 0, 0, strength))
        self.img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(o * 0.8 + 1)))

    def seam(self, p0, p1, width=3 / 192, tone=SEAM, alpha=255):
        self.d.line([p0, p1], fill=tone + (alpha,), width=max(1, self.px(width)))

    # --- raised parts
    def slab(self, a0, f0, a1, f1, z0, h, top, wall=None, radius=0.04, chamfer=0.0,
             shadow=True, top_fn=None, wall_fn=None):
        """A block standing on height z0, h tall. top_fn(box) and wall_fn(box) paint detail."""
        wall = wall or shade(top, 0.68)
        sx0, sy0, sx1, sy1 = self.v.rect(a0, f0, a1, f1)

        def draw():
            lift0, lift1 = z0 * LIFT, (z0 + h) * LIFT
            topbox = self.box_px(sx0, sy0 - lift1, sx1, sy1 - lift1)
            whole = self.box_px(sx0, sy0 - lift1, sx1, sy1 - lift0)
            r, ch = self.px(radius), self.px(chamfer)
            if shadow:
                self.cast_shadow(whole, r)
            self.ramp(whole, shade(wall, 1.08), shade(wall, 0.82), r, ch)
            self.ramp(topbox, shade(top, 1.04), shade(top, 0.93), r, ch)
            if h > 0.02:
                y = topbox[3]
                inset = max(r // 2, ch)
                self.seam((topbox[0] + inset, y), (topbox[2] - inset, y), tone=shade(wall, 0.6))
            wallbox = [whole[0], topbox[3], whole[2], whole[3]]
            if top_fn:
                top_fn(topbox, lift1)
            if wall_fn and wallbox[3] - wallbox[1] > 2:
                wall_fn(wallbox)

        self.add(sy1, draw, z0)

    def cylinder(self, a, f, r, z0, h, cap, wall=None, rings=0, cap_fn=None):
        """A round form standing upright: wall, banded, then a cap lit by stacked discs."""
        wall = wall or shade(cap, 0.72)
        cx, cy = self.v.pt(a, f)

        def draw():
            top_y, bot_y = cy - (z0 + h) * LIFT, cy - z0 * LIFT
            R = self.px(r)
            X = self.px(cx)
            Yt, Yb = self.px(top_y), self.px(bot_y)
            self.cast_shadow([X - R, Yt - R, X + R, Yb + R], R)
            d = self.d
            d.ellipse([X - R, Yb - R, X + R, Yb + R], fill=shade(wall, 0.8) + (255,))
            # Wall: a horizontal ramp, lit at the left-centre, like VFE's tanks.
            wbox = [X - R, Yt, X + R, Yb]
            if wbox[3] > wbox[1]:
                g = Image.new("RGBA", (wbox[2] - wbox[0], wbox[3] - wbox[1]))
                gd = ImageDraw.Draw(g)
                for x in range(g.width):
                    t = abs((x / max(1, g.width - 1)) - 0.38) / 0.62
                    gd.line([(x, 0), (x, g.height)], fill=shade(wall, 1.12 - 0.35 * t) + (255,))
                self.img.paste(g, (wbox[0], wbox[1]))
            for k in range(rings):
                y = Yt + (Yb - Yt) * (k + 1) / (rings + 1)
                self.d.arc([X - R, int(y) - R // 3, X + R, int(y) + R // 3], 0, 180,
                           fill=shade(wall, 0.62) + (255,), width=max(1, self.px(2.5 / 192)))
            d = self.d
            # Cap: three stacked discs, each lighter and nudged up - the crescent is the shading.
            d.ellipse([X - R, Yt - R, X + R, Yt + R], fill=shade(cap, 0.78) + (255,))
            r2 = int(R * 0.92)
            d.ellipse([X - r2, Yt - r2 - R // 20, X + r2, Yt + r2 - R // 20], fill=cap + (255,))
            r3 = int(R * 0.7)
            d.ellipse([X - r3, Yt - r3 - R // 10, X + r3, Yt + r3 - R // 10], fill=shade(cap, 1.07) + (255,))
            if cap_fn:
                cap_fn(X, Yt, R)

        self.add(cy + r, draw, z0)

    def pipe(self, a0, f0, a1, f1, z, width, col):
        """A straight pipe run. Asserts it is straight: bends read as debris at play zoom."""
        assert a0 == a1 or f0 == f1, "pipe runs must be straight"
        (x0, y0), (x1, y1) = self.v.pt(a0, f0), self.v.pt(a1, f1)
        z = z * LIFT + width / 2

        def draw():
            horiz = abs(y1 - y0) < 1e-6
            w = width / 2
            if horiz:
                box = self.box_px(min(x0, x1), y0 - z - w, max(x0, x1), y0 - z + w)
            else:
                box = self.box_px(x0 - w, min(y0, y1) - z, x0 + w, max(y0, y1) - z)
            self.cast_shadow(box, self.px(w), 70, 0.03)
            if horiz:
                self.ramp(box, shade(col, 1.25), shade(col, 0.62), self.px(w))
            else:
                g = Image.new("RGBA", (box[2] - box[0], box[3] - box[1]))
                gd = ImageDraw.Draw(g)
                for x in range(g.width):
                    t = abs(x / max(1, g.width - 1) - 0.35) / 0.65
                    gd.line([(x, 0), (x, g.height)], fill=shade(col, 1.2 - 0.55 * t) + (255,))
                m = Image.new("L", g.size, 0)
                ImageDraw.Draw(m).rounded_rectangle([0, 0, g.width - 1, g.height - 1], radius=self.px(w), fill=255)
                self.img.paste(g, (box[0], box[1]), m)
            # Flanges at both ends, a tone step darker.
            for (x, y) in ((x0, y0), (x1, y1)):
                fw = w * 1.35
                fb = self.box_px(x - fw, y - z - fw, x + fw, y - z + fw) if not horiz else \
                    self.box_px(x - fw * 0.5, y - z - fw, x + fw * 0.5, y - z + fw)
                if not horiz:
                    fb = self.box_px(x - fw, y - z - fw * 0.5, x + fw, y - z + fw * 0.5)
                self.d.rounded_rectangle(fb, radius=self.px(fw * 0.3), fill=shade(col, 0.8) + (255,))

        self.add(max(y0, y1) + 0.001, draw, z)

    # --- flat marks: greebles are a couple of tone steps off what they sit on, never black
    def dots(self, pts, r, col):
        for x, y in pts:
            R = self.px(r)
            X, Y = self.px(x), self.px(y)
            self.d.ellipse([X - R, Y - R + R // 3, X + R, Y + R + R // 3], fill=shade(col, 0.7) + (255,))
            self.d.ellipse([X - R, Y - R, X + R, Y + R], fill=col + (255,))

    def glow(self, box, colour, blur, alpha):
        g = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        ImageDraw.Draw(g).rounded_rectangle(box, radius=blur, fill=colour + (alpha,))
        self.img.alpha_composite(g.filter(ImageFilter.GaussianBlur(blur)))

    # --- output
    def save(self, path, silhouette_px=5):
        # Only solid pixels get the silhouette; cast shadows (alpha <= ~80) stay soft.
        a = self.img.split()[3].point(lambda v: 255 if v > 150 else 0)
        ring = a.filter(ImageFilter.MaxFilter(2 * silhouette_px * SS // 2 + 1))
        base = Image.new("RGBA", self.img.size, SILHOUETTE + (255,))
        base.putalpha(ring)
        base.alpha_composite(self.img)
        out = base.resize((round(self.v.cols * CELL), round(self.v.rows * CELL)), Image.LANCZOS)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        out.save(path)
        print("wrote", path, out.size)
