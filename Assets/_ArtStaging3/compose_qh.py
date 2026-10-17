# -*- coding: utf-8 -*-
"""秦汉风套装：对照图 / 大幅山脉展示 / 叠图示例。"""
from PIL import Image, ImageDraw
from pathlib import Path

HERE = Path(__file__).resolve().parent
SRC = HERE / "_png6"
MAP = Path(r"D:\Unity Projects\Project_Chaos\Assets\Art\PNG\GameWorld_Map\Map_tianxia_zhongyuan.png")
PV = HERE / "_pv6"
PAPER = (231, 221, 180)
INK = (42, 36, 32)


def load(n):
    return Image.open(SRC / (n + ".png")).convert("RGBA")


def fit(im, bw, bh):
    sc = min(bw / im.width, bh / im.height, 1.0)
    return im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))), Image.LANCZOS)


def contact(names, out_name, cols=4, cw=470, ch=390, title="QIN-HAN DECORATION SET"):
    rows = (len(names) + cols - 1) // cols
    W, H = cols * cw + 60, 90 + rows * ch + 40
    sheet = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((30, 34), title, fill=INK)
    for i, n in enumerate(names):
        im = load(n)
        im2 = fit(im, cw - 40, ch - 66)
        x, y = 30 + (i % cols) * cw, 80 + (i // cols) * ch
        sheet.paste(im2, (x, y), im2)
        d.text((x, y + ch - 74), "%s  %dx%d" % (n, im.width, im.height), fill=INK)
    out = PV / out_name
    sheet.save(out)
    print(" ->", out, sheet.size)


def panorama_sheet(names):
    """大幅山脉：等比缩略排开，看构图与层次。"""
    box_w = 1500
    tiles = []
    for n in names:
        im = load(n)
        sc = box_w / im.width
        tiles.append((n, im, im.resize((box_w, max(1, int(im.height * sc))), Image.LANCZOS)))
    W = box_w + 60
    H = 80 + sum(t[2].height + 56 for t in tiles) + 20
    sheet = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((30, 32), "MOUNTAIN PANORAMAS  (for Qinling-scale scenery)", fill=INK)
    y = 72
    for n, orig, im2 in tiles:
        sheet.paste(im2, (30, y), im2)
        d.text((30, y + im2.height + 6), "%s   %dx%d" % (n, orig.width, orig.height), fill=INK)
        y += im2.height + 56
    out = PV / "panoramas.png"
    sheet.save(out)
    print(" ->", out, sheet.size)


PLACE = [
    # 斜向山脉带：从西南向东北排成一条线
    ("QH_Mountain_Range_Diag_Up", 120, 1560, 1.0),
    ("QH_Mountain_Range_Diag_Up", 880, 1120, 1.0),
    ("QH_Mountain_Range_Long_Diag", 1560, 700, 1.0),
    ("QH_Mountain_Range_01", 2380, 480, 1.0),
    ("QH_Mountain_Range_Diag_Down", 120, 2180, 0.9),
    ("QH_Hills_Diag", 900, 2000, 1.0),
    ("QH_Lake_Medium", 2760, 1180, 1.0),
    ("QH_River", 1900, 1980, 1.0),
    ("QH_Forest_Pine", 480, 2500, 1.0),
    ("QH_Forest_Broadleaf", 1080, 2440, 1.0),
    ("QH_City", 2160, 1560, 1.0),
    ("QH_Pass", 1560, 1340, 1.0),
    ("QH_Hamlet", 2600, 1900, 1.0),
    ("QH_Farmland", 1900, 1760, 1.0),
    ("QH_Marsh", 3100, 2280, 1.0),
    ("QH_Grassland", 2600, 2420, 1.0),
]


def demo(max_w=1800):
    m = Image.open(MAP).convert("RGBA")
    sc = max_w / m.width
    m = m.resize((max_w, int(m.height * sc)), Image.LANCZOS)
    for n, x, y, s in PLACE:
        im = load(n)
        im2 = im.resize((max(1, int(im.width * s * sc)), max(1, int(im.height * s * sc))), Image.LANCZOS)
        a = im2.getchannel("A").point(lambda v: int(v * 0.93))
        im2.putalpha(a)
        m.alpha_composite(im2, (int(x * sc), int(y * sc)))
    out = PV / "demo_qh.png"
    m.convert("RGB").save(out, quality=92)
    print(" ->", out, m.size)


if __name__ == "__main__":
    PV.mkdir(parents=True, exist_ok=True)
    decos = sorted(p.stem for p in SRC.glob("QH_*.png"))
    if decos:
        contact(decos, "contact_qh.png")
    pans = sorted(p.stem for p in SRC.glob("Range_*.png"))
    if pans:
        panorama_sheet(pans)
    demo()
