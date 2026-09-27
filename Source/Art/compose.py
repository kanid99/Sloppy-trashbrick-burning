"""Composites a building's texture with its loose-part layers, as the game draws it: the building,
then _LooseA and _LooseB over it. `shake` offsets each layer by a few pixels, to show a frame of
CompLooseParts' rattle. Used by the contact sheet and the store art."""
import os

from PIL import Image

TEX = "Textures/Things/Building/Power/"


def composite(name, rot, shake=None):
    base = Image.open(f"{TEX}{name}_{rot}.png").convert("RGBA")
    out = base.copy()
    for i, layer in enumerate(("LooseA", "LooseB")):
        path = f"{TEX}{name}_{layer}_{rot}.png"
        if not os.path.exists(path):
            continue
        im = Image.open(path).convert("RGBA")
        dx, dy = shake[i] if shake else (0, 0)
        frame = Image.new("RGBA", out.size, (0, 0, 0, 0))
        frame.alpha_composite(im, (max(0, dx), max(0, dy)), (max(0, -dx), max(0, -dy)))
        out.alpha_composite(frame)
    return out
