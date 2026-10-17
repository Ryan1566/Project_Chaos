# -*- coding: utf-8 -*-
"""把地图装饰件渲染成 PNG 并拼成「羊皮纸底」对照图（本地快速自检；最终 PNG 走 Edge）。"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent / "_ArtStaging2"))
sys.path.insert(0, str(HERE.parent / "_ArtStaging2" / "libs"))

from PIL import Image, ImageDraw  # noqa: E402

from reportlab.graphics import renderPM  # noqa: E402
from svglib.svglib import svg2rlg  # noqa: E402


def render_transparent(svg, png):
    """先把纸底色注入 SVG 再渲染——用来预览「贴在地图上」的真实观感。"""
    import re
    s = Path(svg).read_text(encoding="utf-8")
    m = re.search(r"<svg[^>]*>", s)
    head = m.group(0)
    w = re.search(r"width=\"([0-9.]+)\"", head).group(1)
    h = re.search(r"height=\"([0-9.]+)\"", head).group(1)
    s = s.replace(head, head + '<rect width="%s" height="%s" fill="#E7DDB4"/>' % (w, h), 1)
    tmp = Path(png).with_suffix(".tmp.svg")
    tmp.write_text(s, encoding="utf-8")
    with open(tmp, "rb") as fh:
        d = svg2rlg(fh)
    renderPM.drawToFile(d, str(png), fmt="PNG")
    tmp.unlink()

SRC = HERE / "_out4"
PV = HERE / "_pv"
# 与地图同色系
PAPER = (231, 221, 180)
PAPER_D = (187, 177, 143)
INK = (94, 83, 64)


def render_all():
    PV.mkdir(parents=True, exist_ok=True)
    outs = []
    for svg in sorted(SRC.glob("*.svg")):
        png = PV / (svg.stem + ".png")
        render_transparent(svg, png)
        outs.append((svg.stem, Image.open(png).convert("RGBA")))
    return outs


def contact_sheet(items):
    """两列排布，铺羊皮纸底 + 轻微渐晕，模拟贴在地图上的观感。"""
    pad, gap = 48, 26
    col_w = 860
    rows = []
    x = y = 0
    cur = []
    for name, im in items:
        w = min(im.width, col_w)
        if x + w > pad * 2 + col_w * 2 + gap:
            rows.append(cur); cur = []; x = 0; y += 0
        cur.append((name, im, x, w))
        x += w + gap
        if len(cur) == 2:
            rows.append(cur); cur = []; x = 0
    if cur:
        rows.append(cur)

    # 计算高度
    heights = []
    for row in rows:
        heights.append(max(im.height for _, im, _, _ in row))
    W = pad * 2 + col_w * 2 + gap
    H = pad * 2 + sum(heights) + gap * (len(rows) - 1) + 60 * len(rows)
    sheet = Image.new("RGB", (W, H), PAPER)
    # 纸纹：轻微噪点
    import random
    rnd = random.Random(5)
    d = ImageDraw.Draw(sheet)
    for _ in range(W * H // 900):
        xx, yy = rnd.randrange(W), rnd.randrange(H)
        c = PAPER_D if rnd.random() < 0.5 else PAPER
        d.point((xx, yy), c)

    y = pad
    for ri, row in enumerate(rows):
        x = pad
        hmax = heights[ri]
        for name, im, _, w in row:
            scale = min(1.0, 1.0)
            im2 = im if scale == 1 else im.resize((int(im.width * scale), int(im.height * scale)),
                                                 Image.LANCZOS)
            sheet.paste(im2, (x, y), im2)
            d.text((x + 4, y + hmax + 10), name, fill=INK)
            x += w + gap
        y += hmax + 60
    return sheet


if __name__ == "__main__":
    items = render_all()
    print("渲染", len(items), "个")
    sheet = contact_sheet(items)
    out = PV / "deco_contact.png"
    sheet.save(out)
    print("对照图", out, sheet.size)
