"""Checks the textures against the defs and the C#. Run from the repo root after any change to
either side; exits non-zero on a mismatch.

    python3 Source/Art/verify_art.py

1. Every graphicData in the defs resolves to real files at the right size: all four rotations
   for Graphic_Multi, at drawSize x 192px per cell, and a non-empty folder for Graphic_StackCount.
2. The intake contract. CompHopperFeed.IntakeCells reads fuel from the cells just outside the
   edge in rot.FacingCell's direction. The sprite marks its one intake with VFE's input green.
   For every rotation this finds the green and checks it sits on THAT edge, centred along it,
   and that none of it is on the opposite half. Map z runs up and screen y runs down, so north
   is the top of the texture - the sign error the mending mod shipped once.
3. The green in the art is the same colour the C# outlines the intake cells with.
"""
import glob
import re
import sys
import xml.etree.ElementTree as ET

from PIL import Image

sys.path.insert(0, "Source/Art")
from draw_sprites import INTAKE_GREEN, INTAKE_H, INTAKE_Z  # noqa: E402
from stb_draw import CELL, LIFT  # noqa: E402

# Textures another mod ships. VEF provides the hidden-conduit texture VE's hidden pipes all use.
EXTERNAL = {"UI/CSG/IConduit"}

FACING = {"north": (0, 1), "east": (1, 0), "south": (0, -1), "west": (-1, 0)}
fails = []


def fail(msg):
    fails.append(msg)
    print("FAIL", msg)


def vec(s):
    return tuple(float(x) for x in s.strip("() ").split(","))


defs, named = {}, {}
for path in glob.glob("Defs/**/*.xml", recursive=True) + glob.glob("Mods/*/Defs/**/*.xml", recursive=True):
    for td in ET.parse(path).getroot().iter("ThingDef"):
        if td.get("Name"):
            named[td.get("Name")] = td
        gd = td.find("graphicData")
        if gd is None or td.findtext("defName") is None:
            continue
        defs[td.findtext("defName")] = td


def lookup(td, path):
    """findtext that follows ParentName through this mod's own abstract defs."""
    while td is not None:
        v = td.findtext(path)
        if v:
            return v
        td = named.get(td.get("ParentName"))
    return None

# 1. files and sizes
for name, td in defs.items():
    gd = td.find("graphicData")
    tex, cls = gd.findtext("texPath"), gd.findtext("graphicClass")
    ds = vec(gd.findtext("drawSize") or "(1,1)")
    base = f"Textures/{tex}"
    if cls == "Graphic_Multi":
        for rot in FACING:
            p = f"{base}_{rot}.png"
            try:
                im = Image.open(p)
            except FileNotFoundError:
                fail(f"{name}: missing {p}")
                continue
            w, h = (ds[0], ds[1]) if rot in ("north", "south") else (ds[1], ds[0])
            want = (round(w * CELL), round(h * CELL))
            if im.size != want:
                fail(f"{name}: {p} is {im.size}, drawSize {ds} wants {want}")
    elif cls == "Graphic_StackCount":
        if not glob.glob(f"{base}/*.png"):
            fail(f"{name}: no textures in {base}/")
    elif tex not in EXTERNAL:
        try:
            Image.open(f"{base}.png")
        except FileNotFoundError:
            fail(f"{name}: missing {base}.png")
    # Blueprint atlases and menu icons.
    for extra in (lookup(td, "building/blueprintGraphicData/texPath"), lookup(td, "uiIconPath")):
        if extra and extra not in EXTERNAL:
            try:
                Image.open(f"Textures/{extra}.png")
            except FileNotFoundError:
                fail(f"{name}: missing Textures/{extra}.png")
    print(f"ok   {name}: {cls} textures present" if not any(name in f for f in fails) else "", end="")
    print()

# 2. the intake contract
def is_green(px):
    r, g, b, a = px
    return a > 200 and abs(r - INTAKE_GREEN[0]) < 18 and abs(g - INTAKE_GREEN[1]) < 18 and abs(b - INTAKE_GREEN[2]) < 18


for name, td in defs.items():
    comps = td.find("comps")
    if comps is None or not any((li.get("Class") or "").endswith("CompProperties_HopperFeed") for li in comps):
        continue
    size = vec(td.findtext("size"))
    ds = vec(td.find("graphicData").findtext("drawSize"))
    tex = td.find("graphicData").findtext("texPath")
    for rot, (fx, fz) in FACING.items():
        im = Image.open(f"Textures/{tex}_{rot}.png").convert("RGBA")
        W, H = im.size
        pts = [(x, y) for y in range(H) for x in range(W) if is_green(im.getpixel((x, y)))]
        if not pts:
            fail(f"{name} {rot}: no intake green found")
            continue
        cx = sum(p[0] for p in pts) / len(pts) / CELL
        cy = sum(p[1] for p in pts) / len(pts) / CELL
        # Footprint centre on the canvas, and the midpoint of the facing edge. Screen y = -z.
        sw, sh = (size[0], size[1]) if rot in ("north", "south") else (size[1], size[0])
        ocx, ocy = W / CELL / 2, H / CELL / 2
        # The port's top face is drawn raised by its height, straight up the screen in every view.
        ex, ey = ocx + fx * sw / 2, ocy - fz * sh / 2 - (INTAKE_Z + INTAKE_H) * LIFT
        along = abs((cx - ex) if fx == 0 else (cy - ey))
        across = abs((cy - ey) if fx == 0 else (cx - ex))
        opposite = [p for p in pts if (p[0] / CELL - ocx) * fx - (p[1] / CELL - ocy) * fz < 0]
        ok = along < 0.1 and across < 0.45 and not opposite
        msg = f"{name} {rot}: intake at ({cx:.2f},{cy:.2f}) cells, facing edge mid ({ex:.2f},{ey:.2f}), " \
              f"off-centre {along:.2f}, in from edge {across:.2f}, on far side {len(opposite)}px"
        print(("ok   " if ok else "") + msg) if ok else fail(msg)

# 2b. linked atlases: tile i (links N=1 E=2 S=4 W=8) at column i % 4, row 3 - i // 4. Tile 5 is
# the vertical straight and tile 10 the horizontal one, so each must be solid through its middle
# along its own axis and clear at the ends of the other.
for name, td in defs.items():
    gd = td.find("graphicData")
    if gd.findtext("linkType") is None or gd.findtext("texPath") in EXTERNAL:
        continue
    for tex in (gd.findtext("texPath"), lookup(td, "building/blueprintGraphicData/texPath")):
        if not tex:
            continue
        im = Image.open(f"Textures/{tex}.png").convert("RGBA")
        t = im.width // 4

        def alpha(i, fx, fy):
            col, row = i % 4, 3 - i // 4
            return im.getpixel((int(col * t + fx * t), int(row * t + fy * t)))[3]
        vertical = alpha(5, 0.5, 0.02) > 100 and alpha(5, 0.5, 0.98) > 100 and alpha(5, 0.02, 0.5) < 50
        horizontal = alpha(10, 0.02, 0.5) > 100 and alpha(10, 0.98, 0.5) > 100 and alpha(10, 0.5, 0.02) < 50
        empty_ok = alpha(0, 0.5, 0.02) < 50 and alpha(0, 0.02, 0.5) < 50
        if vertical and horizontal and empty_ok:
            print(f"ok   {name}: {tex} atlas tiles in link order")
        else:
            fail(f"{name}: {tex} tiles out of order (v={vertical} h={horizontal} isolated-clear={empty_ok})")

# 2c. loose-part layers (CompLooseParts): every layer in all four views at the building's own size,
# the building drawn in real time so they can move, and none of them covering the intake.
for name, td in defs.items():
    comps = td.find("comps")
    if comps is None:
        continue
    loose = [li for li in comps if (li.get("Class") or "").endswith("CompProperties_LooseParts")]
    if not loose:
        continue
    if lookup(td, "drawerType") != "MapMeshAndRealTime":
        fail(f"{name}: has loose parts but drawerType isn't MapMeshAndRealTime, so they'd never be drawn")
    base_tex = td.find("graphicData").findtext("texPath")
    feeds = any((li.get("Class") or "").endswith("CompProperties_HopperFeed") for li in comps)
    for layer in loose[0].find("layers"):
        tex = layer.findtext("texPath")
        for rot in FACING:
            try:
                im = Image.open(f"Textures/{tex}_{rot}.png").convert("RGBA")
            except FileNotFoundError:
                fail(f"{name}: missing loose layer Textures/{tex}_{rot}.png")
                continue
            base = Image.open(f"Textures/{base_tex}_{rot}.png").convert("RGBA")
            if im.size != base.size:
                fail(f"{name}: {tex}_{rot} is {im.size}, the building is {base.size}")
                continue
            if feeds:
                covered = 0
                for y in range(0, base.height, 2):
                    for x in range(0, base.width, 2):
                        if is_green(base.getpixel((x, y))) and im.getpixel((x, y))[3] > 60:
                            covered += 1
                if covered:
                    fail(f"{name}: {tex}_{rot} covers the intake ({covered} samples)")
        print(f"ok   {name}: loose layer {tex} in four views, clear of the intake" if feeds
              else f"ok   {name}: loose layer {tex} in four views")

# 3. the C# outline colour matches the art
cs = open("Source/TrashbrickBurning/CompHopperFeed.cs").read()
m = re.search(r"IntakeColor = new UnityEngine.Color\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)", cs)
if not m:
    fail("could not find IntakeColor in CompHopperFeed.cs")
else:
    c = tuple(round(float(v) * 255) for v in m.groups())
    if max(abs(a - b) for a, b in zip(c, INTAKE_GREEN)) > 3:
        fail(f"C# IntakeColor {c} != art INTAKE_GREEN {INTAKE_GREEN}")
    else:
        print(f"ok   C# IntakeColor {c} matches the art's {INTAKE_GREEN}")

print(f"\n{'FAILED: ' + str(len(fails)) if fails else 'all checks passed'}")
sys.exit(1 if fails else 0)
