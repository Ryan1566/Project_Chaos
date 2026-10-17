# -*- coding: utf-8 -*-
"""秦汉风套装导出：装饰件切片（羽化 + alpha 裁剪），大幅山脉整幅导出。"""
import csv
import shutil
from pathlib import Path

from PIL import Image, ImageFilter

HERE = Path(__file__).resolve().parent
SRC = HERE / "_png6"
DST = HERE / "_slice6"
PAN = DST / "panoramas"
PAD = 4
FEATHER = 2.0


def slice_one(p: Path):
    im = Image.open(p).convert("RGBA")
    a = im.getchannel("A")
    a = a.filter(ImageFilter.GaussianBlur(FEATHER)).filter(ImageFilter.GaussianBlur(FEATHER * 0.5))
    im.putalpha(a)
    bbox = im.getchannel("A").point(lambda v: 255 if v > 6 else 0).getbbox()
    if bbox is None:
        raise RuntimeError("空图 " + p.name)
    x0, y0, x1, y1 = bbox
    W, H = im.size
    out = im.crop((max(0, x0 - PAD), max(0, y0 - PAD), min(W, x1 + PAD), min(H, y1 + PAD)))
    out.save(DST / p.name)
    return out.size


def main():
    DST.mkdir(parents=True, exist_ok=True)
    PAN.mkdir(parents=True, exist_ok=True)
    rows = []
    for p in sorted(SRC.glob("QH_*.png")):
        w, h = slice_one(p)
        rows.append((p.stem, w, h))
    for p in sorted(SRC.glob("Range_*.png")):
        shutil.copy2(p, PAN / p.name)
        im = Image.open(p)
        rows.append((p.stem, im.width, im.height))
    with open(DST / "_切片清单.csv", "w", encoding="utf-8-sig", newline="") as fh:
        wr = csv.writer(fh)
        wr.writerow(["文件名", "宽", "高", "说明"])
        for n, w, h in rows:
            wr.writerow([n + ".png", w, h, "大幅山脉（整幅）" if n.startswith("Range_") else "装饰件切片（透明底，已羽化）"])
    print("导出 %d 个（装饰件 %d + 大幅山脉 %d）" % (len(rows),
                                              sum(1 for r in rows if not r[0].startswith("Range_")),
                                              sum(1 for r in rows if r[0].startswith("Range_"))))
    for n, w, h in rows:
        print("   %-30s %4d x %-4d" % (n, w, h))


if __name__ == "__main__":
    main()
