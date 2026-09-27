"""Builds About/Preview.png and About/ModIcon.png from the mod's own in-game textures, so the
store page always shows what is actually in the game. Re-run after any texture change:

    python3 Source/Art/make_about_art.py
"""
from PIL import Image, ImageDraw, ImageFilter, ImageFont

TEX = "Textures/Things/Building/Power/"
BOLD = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
W, H = 640, 360     # ModMetaData.PreviewImagePath; 16:9 matches the Workshop banner
FIRE = (236, 150, 70, 255)   # the cobbled stove's fire, its one accent

card = Image.new("RGBA", (W, H))
d = ImageDraw.Draw(card)
for y in range(H):
    t = y / (H - 1)
    d.line([(0, y), (W, y)], fill=tuple(round(a + (b - a) * t) for a, b in zip((48, 44, 40), (26, 24, 22))) + (255,))


def fit(name, width):
    im = Image.open(TEX + name).convert("RGBA")
    im = im.crop(im.getbbox())
    return im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)


def shadow(img, blur=7, alpha=130, off=(3, 5)):
    pad = blur * 3
    sh = Image.new("RGBA", (img.width + pad * 2, img.height + pad * 2), (0, 0, 0, 0))
    sh.paste(Image.new("RGBA", img.size, (0, 0, 0, alpha)), (pad + off[0], pad + off[1]), img.split()[3])
    return sh.filter(ImageFilter.GaussianBlur(blur)), pad


placements = (
    (fit("STB_TrashbrickGasifier_south.png", 230), (390, 22)),
    (fit("STB_CobbledPelletStove_south.png", 168), (236, 160)),
    (fit("STB_FuelHopper_south.png", 60), (440, 280)),
)
for img, pos in placements:
    assert pos[0] >= 0 and pos[1] >= 0 and pos[0] + img.width <= W and pos[1] + img.height <= H, (pos, img.size)
    sh, pad = shadow(img)
    card.alpha_composite(sh, (pos[0] - pad, pos[1] - pad))
    card.alpha_composite(img, pos)

d = ImageDraw.Draw(card)
d.text((30, 36), "SLOPPYMODS", font=ImageFont.truetype(BOLD, 18), fill=(146, 142, 136, 255))
d.text((30, 60), "TRASHBRICK", font=ImageFont.truetype(BOLD, 40), fill=(238, 234, 228, 255))
d.text((30, 104), "BURNING", font=ImageFont.truetype(BOLD, 40), fill=FIRE)
d.line([(32, 156), (200, 156)], fill=FIRE, width=3)
for i, line in enumerate(("Stirling generators", "fuelled by trashbricks")):
    d.text((30, 166 + i * 20), line, font=ImageFont.truetype(BOLD, 15), fill=(168, 164, 158, 255))
card.convert("RGB").save("About/Preview.png")

# ModMetaData.ModIconImagePath - shown at about 32px in the mod list, so one machine, filling it.
ICON = 256
icon = Image.new("RGBA", (ICON, ICON), (0, 0, 0, 0))
src = Image.open(TEX + "STB_CobbledPelletStove_south.png").convert("RGBA")
src = src.crop(src.getbbox())
scale = min(ICON / src.width, ICON / src.height) * 0.98
small = src.resize((round(src.width * scale), round(src.height * scale)), Image.LANCZOS)
icon.alpha_composite(small, ((ICON - small.width) // 2, (ICON - small.height) // 2))
icon.save("About/ModIcon.png")
print("wrote About/Preview.png and About/ModIcon.png")
