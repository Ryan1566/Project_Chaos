# -*- coding: utf-8 -*-
"""导出"切片图"：渲染好的 PNG → 边缘羽化 → 按 alpha 精确裁剪 → 单图 + 清单。

羽化：让贴在羊皮纸上的地形边缘自然融进纸面（去掉贴纸感）。
裁剪：每张图裁到内容外接框 + 4px 透明边，得到干净的 sprite（Unity 直接按 native size 用）。
"""
import csv
from pathlib import Path

from PIL import Image, ImageFilter

HERE = Path(__file__).resolve().parent
SRC = HERE / "_png5"
DST = HERE / "_slice5"
PAD = 4          # 裁切后保留的透明边
FEATHER = 2.0    # alpha 羽化半径（像素）


def slice_one(p: Path, dst_dir: Path):
    im = Image.open(p).convert("RGBA")
    # ① 羽化 alpha：做两遍轻度模糊，避免边缘过软
    a = im.getchannel("A")
    a2 = a.filter(ImageFilter.GaussianBlur(FEATHER))
    a3 = a2.filter(ImageFilter.GaussianBlur(FEATHER * 0.5))
    im.putalpha(a3)
    # ② 用羽化后的 alpha 求外接框（阈值 6，忽略极淡残影）
    a = im.getchannel("A").point(lambda v: 255 if v > 6 else 0)
    bbox = a.getbbox()
    if bbox is None:
        raise RuntimeError("空图: " + p.name)
    x0, y0, x1, y1 = bbox
    W, H = im.size
    x0 = max(0, x0 - PAD); y0 = max(0, y0 - PAD)
    x1 = min(W, x1 + PAD); y1 = min(H, y1 + PAD)
    out = im.crop((x0, y0, x1, y1))
    dst_dir.mkdir(parents=True, exist_ok=True)
    out.save(dst_dir / p.name)
    return out.size


def main():
    rows = []
    for p in sorted(SRC.glob("Deco_*.png")):
        w, h = slice_one(p, DST)
        rows.append((p.stem, w, h, round(w / h, 3)))
    with open(DST / "_切片清单.csv", "w", encoding="utf-8-sig", newline="") as fh:
        wr = csv.writer(fh)
        wr.writerow(["文件名", "宽", "高", "宽高比", "说明"])
        for name, w, h, ar in rows:
            wr.writerow([name + ".png", w, h, ar, "透明底切片，已羽化边缘"])
    print("切片完成 %d 张 -> %s" % (len(rows), DST))
    for name, w, h, ar in rows:
        print("   %-26s %4dx%-4d  %.2f" % (name, w, h, ar))


if __name__ == "__main__":
    main()
