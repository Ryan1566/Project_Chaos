# -*- coding: utf-8 -*-
"""用 Edge 渲染出的真 PNG 做两件事：
   ① 羊皮纸底对照图（看每个装饰件在地图纸色上的观感）
   ② 叠图示例（把装饰件按地图 4096 坐标系摆到 Map_tianxia_zhongyuan.png 上）
"""
from PIL import Image, ImageDraw
from pathlib import Path

ART = Path(r"C:\Users\26751\Desktop\游戏美术资源")
DECO = ART / "PNG" / "18_地图装饰_天下中原"
MAP = Path(r"D:\Unity Projects\Project_Chaos\Assets\Art\PNG\GameWorld_Map\Map_tianxia_zhongyuan.png")
HERE = Path(__file__).resolve().parent
PV = HERE / "_pv"
PAPER = (231, 221, 180)
INK = (90, 79, 60)


def load(name):
    return Image.open(DECO / (name + ".png")).convert("RGBA")


def contact():
    names = sorted(p.stem for p in DECO.glob("*.png"))
    cols = 4
    cw, ch = 470, 400
    rows = (len(names) + cols - 1) // cols
    W, H = cols * cw + 60, 110 + rows * ch + 40
    sheet = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((30, 34), "MAP DECORATION SET  (parchment preview)", fill=INK)
    for i, n in enumerate(names):
        im = load(n)
        box_w, box_h = cw - 40, ch - 70
        sc = min(box_w / im.width, box_h / im.height, 1.0)
        im2 = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))), Image.LANCZOS)
        x = 30 + (i % cols) * cw
        y = 90 + (i // cols) * ch
        sheet.paste(im2, (x, y), im2)
        d.text((x, y + box_h + 8), "%s  %dx%d" % (n, im.width, im.height), fill=INK)
    out = PV / "deco_contact_edge.png"
    sheet.save(out)
    print("对照图", out, sheet.size)


# 地图 4096 坐标系下的摆放方案（演示用）
PLACEMENT = [
    ("Deco_Mountain_Range_Long", 120, 1120, 1.0),
    ("Deco_Mountain_Range_02", 880, 1480, 0.9),
    ("Deco_Mountain_Peak", 1560, 360, 1.0),
    ("Deco_Hills", 2280, 1380, 1.0),
    ("Deco_Lake_Medium", 2660, 760, 1.0),
    ("Deco_River_Branch", 1960, 1900, 1.0),
    ("Deco_Forest_Conifer", 560, 2380, 1.0),
    ("Deco_Forest_Broadleaf", 1200, 2280, 1.0),
    ("Deco_Marsh", 2280, 2340, 1.0),
    ("Deco_Dunes", 3260, 1680, 1.0),
    ("Deco_Grassland", 2620, 2100, 1.0),
    ("Deco_City", 1860, 1580, 1.0),
    ("Deco_Pass", 1080, 980, 1.0),
    ("Deco_Hamlet", 1720, 1300, 1.0),
    ("Deco_Ferry", 2180, 980, 1.0),
    ("Deco_Mine", 460, 1680, 1.0),
    ("Deco_Farmland", 1480, 1760, 1.0),
]


def demo(max_w=1800):
    m = Image.open(MAP).convert("RGBA")
    sc = max_w / m.width
    m = m.resize((max_w, int(m.height * sc)), Image.LANCZOS)
    for name, x, y, s in PLACEMENT:
        im = load(name)
        im2 = im.resize((max(1, int(im.width * s * sc)), max(1, int(im.height * s * sc))),
                        Image.LANCZOS)
        # 让装饰件稍微"压"进纸面：整体透明度 92%
        a = im2.getchannel("A").point(lambda v: int(v * 0.92))
        im2.putalpha(a)
        m.alpha_composite(im2, (int(x * sc), int(y * sc)))
    out = PV / "deco_demo_on_map.png"
    m.convert("RGB").save(out, quality=92)
    print("叠图示例", out, m.size)


if __name__ == "__main__":
    PV.mkdir(parents=True, exist_ok=True)
    contact()
    demo()
