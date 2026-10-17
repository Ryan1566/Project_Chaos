# -*- coding: utf-8 -*-
"""写实版装饰件：对照图 + 叠图示例（用 Edge 渲染出的 PNG）。"""
from PIL import Image, ImageDraw
from pathlib import Path

SRC = Path(__file__).resolve().parent / "_slice5"
MAP = Path(r"D:\Unity Projects\Project_Chaos\Assets\Art\PNG\GameWorld_Map\Map_tianxia_zhongyuan.png")
PV = Path(__file__).resolve().parent / "_pv5b"
PAPER = (231, 221, 180)
INK = (90, 79, 60)


def load(n):
    return Image.open(SRC / (n + ".png")).convert("RGBA")


def contact():
    names = sorted(p.stem for p in SRC.glob("*.png"))
    cols, cw, ch = 4, 460, 380
    rows = (len(names) + cols - 1) // cols
    W, H = cols * cw + 60, 90 + rows * ch + 40
    sheet = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((30, 34), "MAP DECORATION SET  v2 (painterly / parchment preview)", fill=INK)
    for i, n in enumerate(names):
        im = load(n)
        bw, bh = cw - 40, ch - 62
        sc = min(bw / im.width, bh / im.height, 1.0)
        im2 = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))), Image.LANCZOS)
        x, y = 30 + (i % cols) * cw, 80 + (i // cols) * ch
        sheet.paste(im2, (x, y), im2)
        d.text((x, y + bh + 8), "%s  %dx%d" % (n.replace("Deco_", ""), im.width, im.height), fill=INK)
    out = PV / "contact_v2.png"
    sheet.save(out)
    print("对照图", out, sheet.size)


PLACE = [
    ("Deco_Mountain_Range_03", 120, 1120, 1.0),
    ("Deco_Mountain_Range_02", 900, 1480, 0.95),
    ("Deco_Mountain_Peak", 1620, 330, 1.0),
    ("Deco_Hills", 2280, 1420, 1.0),
    ("Deco_Lake_Medium", 2700, 740, 1.0),
    ("Deco_River_Main", 1980, 1880, 1.0),
    ("Deco_Forest_Conifer", 520, 2400, 1.0),
    ("Deco_Forest_Broadleaf", 1150, 2320, 1.0),
    ("Deco_Marsh", 2260, 2360, 1.0),
    ("Deco_Dunes", 3240, 1700, 1.0),
    ("Deco_Grassland", 2600, 2120, 1.0),
    ("Deco_City", 1900, 1560, 1.0),
    ("Deco_Pass", 1080, 960, 1.0),
    ("Deco_Hamlet", 1720, 1300, 1.0),
    ("Deco_Farmland", 1480, 1760, 1.0),
]


def demo(max_w=1800):
    m = Image.open(MAP).convert("RGBA")
    sc = max_w / m.width
    m = m.resize((max_w, int(m.height * sc)), Image.LANCZOS)
    for n, x, y, s in PLACE:
        im = load(n)
        im2 = im.resize((max(1, int(im.width * s * sc)), max(1, int(im.height * s * sc))),
                        Image.LANCZOS)
        a = im2.getchannel("A").point(lambda v: int(v * 0.93))
        im2.putalpha(a)
        m.alpha_composite(im2, (int(x * sc), int(y * sc)))
    out = PV / "demo_v2.png"
    m.convert("RGB").save(out, quality=92)
    print("叠图", out, m.size)


if __name__ == "__main__":
    PV.mkdir(parents=True, exist_ok=True)
    contact()
    demo()
