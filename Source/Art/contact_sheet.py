"""Every building view on one sheet, on a grass-toned ground, for review. Writes contact_sheet.png
to the path given (default: ./contact_sheet.png). Not shipped with the mod."""
import sys
from PIL import Image, ImageDraw

sys.path.insert(0, "Source/Art")
from compose import composite  # noqa: E402

T = "Textures/Things/Building/Power/"
ROWS = ["STB_CobbledPelletStove", "STB_TrashbrickGasifier", "STB_LargeCobbledStove", "STB_LargeGasifier", "STB_IndustrialGasifier", "STB_CobbledTurbine", "STB_SteamTurbine", "STB_FuelHopper"]
ROTS = ["north", "east", "south", "west"]
pad, cell = 16, 768
sheet = Image.new("RGBA", (pad + (cell + pad) * 4, pad + (cell + pad) * len(ROWS) + 20), (96, 104, 70, 255))
d = ImageDraw.Draw(sheet)
for j, rot in enumerate(ROTS):
    d.text((pad + j * (cell + pad) + cell // 2 - 12, 4), rot, fill=(230, 230, 220, 255))
for i, name in enumerate(ROWS):
    for j, rot in enumerate(ROTS):
        im = composite(name, rot)
        x = pad + j * (cell + pad) + (cell - im.width) // 2
        y = 20 + pad + i * (cell + pad) + (cell - im.height) // 2
        sheet.alpha_composite(im, (x, y))
sheet.save(sys.argv[1] if len(sys.argv) > 1 else "contact_sheet.png")
