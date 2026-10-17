# -*- coding: utf-8 -*-
"""天下中原地图装饰件（v2）——纸本墨线风，与 Map_tianxia_zhongyuan.png 同语汇。

v2 重点修：山体改成「古典地图单峰 + 内凹双翼 + 右坡排线」并叠成山脉链；
树、丘陵、沙丘按地图符号比例重做；所有元素透明底，可直接叠图。
"""
import math
import random
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mini as M  # noqa: E402

INK = "#5A4F3C"
INK2 = "#6E6249"
INK3 = "#8C7F62"
WATER = "#AEB79B"
WATER_OP = 0.18
SW = 3.0


def f(v):
    return M.n(round(v, 2))


def path(d, stroke=INK, w=SW, op=0.86, fill="none", dash=None):
    extra = (' stroke-dasharray="%s"' % dash) if dash else ""
    return ('<path d="%s" fill="%s" stroke="%s" stroke-width="%s" stroke-linecap="round" '
            'stroke-linejoin="round" opacity="%s"%s/>' % (d, fill, stroke, M.n(w), M.n(op), extra))


def smooth(pts, closed=True, tension=1.0):
    n = len(pts)
    if n < 3:
        return ""
    def P(i):
        return pts[i % n] if closed else pts[max(0, min(n - 1, i))]
    d = ["M %s %s" % (f(pts[0][0]), f(pts[0][1]))]
    for i in range(n if closed else n - 1):
        p0, p1, p2, p3 = P(i - 1), P(i), P(i + 1), P(i + 2)
        c1 = (p1[0] + (p2[0] - p0[0]) / 6 * tension, p1[1] + (p2[1] - p0[1]) / 6 * tension)
        c2 = (p2[0] - (p3[0] - p1[0]) / 6 * tension, p2[1] - (p3[1] - p1[1]) / 6 * tension)
        d.append("C %s %s %s %s %s %s" % (f(c1[0]), f(c1[1]), f(c2[0]), f(c2[1]), f(p2[0]), f(p2[1])))
    if closed:
        d.append("Z")
    return " ".join(d)


def wob(x1, y1, x2, y2, color=INK2, w=SW, seed=1, amp=1.4, seg=5, op=0.8):
    r = random.Random(seed)
    pts = []
    for i in range(seg + 1):
        t = i / seg
        x, y = x1 + (x2 - x1) * t, y1 + (y2 - y1) * t
        if 0 < i < seg:
            x += r.uniform(-amp, amp)
            y += r.uniform(-amp, amp)
        pts.append((x, y))
    return M.polyline(pts, stroke=color, sw=w, op=op)


def blob(cx, cy, rx, ry, seed=1, n=18, rough=0.07, lobes=3, lobe_amp=0.07):
    r = random.Random(seed)
    ph = r.uniform(0, math.tau)
    pts = []
    for i in range(n):
        a = math.tau * i / n
        k = 1 + (r.random() * 2 - 1) * rough + lobe_amp * math.sin(lobes * a + ph)
        pts.append((cx + math.cos(a) * rx * k, cy + math.sin(a) * ry * k))
    return smooth(pts, closed=True, tension=1.0)


def bez(p0, c, p1, t):
    return ((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * c[0] + t ** 2 * p1[0],
            (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * c[1] + t ** 2 * p1[1])


# ---------------------------------------------------------------- 山
def _flank(p0, p1, bulge, n=9):
    """沿二次曲线采样一翼轮廓（bulge 控制外鼓/内凹）。"""
    c = ((p0[0] + p1[0]) / 2 + bulge[0], (p0[1] + p1[1]) / 2 + bulge[1])
    return [bez(p0, c, p1, i / (n - 1)) for i in range(n)]


def mountain(cx, base_y, w, h, seed, hatch_n=9, op=0.86, fill_op=0.0):
    """古典地图单峰：平滑轮廓 + 右坡短排线（不画山脊/内侧长线，避免轮廓互穿时的「线框感」）。"""
    r = random.Random(seed)
    px = cx + r.uniform(-w * 0.04, w * 0.04)
    py = base_y - h
    L, R = cx - w * 0.5, cx + w * 0.5
    left = _flank((L, base_y), (px, py), (-w * 0.12, h * 0.20), 7)
    right = _flank((px, py), (R, base_y), (w * 0.12, h * 0.20), 7)
    sil = smooth(left + right[1:], closed=False, tension=0.8)
    out = []
    if fill_op > 0:
        out.append(path(sil + " Z", fill="#C6BB96", op=fill_op, stroke="none"))
    out.append(path(sil, INK, SW, op))
    # 右坡排线：贴轮廓内侧、长度随高度递减
    for i in range(hatch_n):
        t = 0.14 + 0.72 * i / max(1, hatch_n - 1)
        x, y = bez((px, py), (px + w * 0.12, py + h * 0.20), (R, base_y), t)
        ln = min(h * 0.17, (0.55 - 0.4 * t) * h)
        out.append(wob(x - 1.5, y, x - ln * 0.45, y + ln, INK2, 2.1,
                       seed + 10 + i, 0.4, 2, op * 0.62))
    return out


def el_mountain_range(w, h, n_hint, seed):
    """山脉：峰高/峰宽由画布高自动定，峰数由宽度定（相邻峰轻微重叠）。"""
    out = []
    r = random.Random(seed)
    ph = h * 0.70
    pw = ph / 0.62
    n = max(2, min(9, int(round(w * 0.80 / pw)) + 1))
    base = h * 0.97
    # 后排：矮一点、淡一点，塞进前排间隙
    nb = n + 1
    for i in range(nb):
        t = (i + 0.5) / nb
        cx = w * (0.08 + 0.84 * t) + r.uniform(-w * 0.015, w * 0.015)
        out += mountain(cx, base - ph * 0.10, pw * 0.70, ph * 0.62,
                        seed + i * 17, 6, 0.52)
    # 前排：主峰
    for i in range(n):
        t = i / max(1, n - 1)
        cx = w * (0.14 + 0.72 * t) + r.uniform(-w * 0.02, w * 0.02)
        hh = ph * r.uniform(0.86, 1.08)
        out += mountain(cx, base, pw * r.uniform(0.88, 1.10), hh,
                        seed + 100 + i * 23, 9, 0.88, 0.40)
    # 山脚地平线 + 疏点
    out.append(wob(w * 0.03, h * 0.99, w * 0.97, h * 0.99, INK3, 1.4, seed + 7, 1.0, 8, 0.20))
    for i in range(10):
        out.append(M.circle(w * (0.05 + 0.90 * i / 9), h * 0.995 - r.uniform(0, 3),
                            r.uniform(0.7, 1.4), fill=INK3, op=0.22))
    return out


def el_peak(w, h, seed):
    """孤峰：1 大 + 2 小（高度按宽度 0.62 的古典比例）。"""
    out = mountain(w * 0.50, h * 0.97, w * 0.74, w * 0.74 * 0.62, seed, 12)
    out += mountain(w * 0.20, h * 0.985, w * 0.40, w * 0.40 * 0.62, seed + 11, 7, 0.72)
    out += mountain(w * 0.80, h * 0.99, w * 0.34, w * 0.34 * 0.62, seed + 21, 6, 0.72)
    return out


def el_hills(w, h, seed):
    """丘陵：矮而圆、带排线的连续丘包。"""
    r = random.Random(seed)
    out = []
    base = h * 0.86
    n = 6
    for i in range(n):
        cx = w * (0.10 + 0.80 * i / (n - 1)) + r.uniform(-w * 0.02, w * 0.02)
        rw = w * r.uniform(0.10, 0.16)
        rh = h * r.uniform(0.30, 0.52)
        d = "M %s %s Q %s %s %s %s Q %s %s %s %s" % (
            f(cx - rw), f(base), f(cx - rw * 0.55), f(base - rh * 1.35), f(cx), f(base - rh),
            f(cx + rw * 0.55), f(base - rh * 1.35), f(cx + rw), f(base))
        out.append(path(d, INK2, 2.8, 0.82))
        for k in range(4):
            t = (k + 1) / 5
            out.append(wob(cx + rw * t * 0.8, base - rh * (1 - t) * 0.8,
                           cx + rw * t * 0.62, base - rh * (1 - t) * 0.35,
                           INK3, 1.5, seed + i * 9 + k, 0.7, 3, 0.32))
    out.append(wob(w * 0.02, base + 3, w * 0.98, base + 3, INK3, 1.6, seed + 5, 1.6, 8, 0.26))
    return out


# ---------------------------------------------------------------- 水
def el_lake(w, h, seed, islands=1):
    cx, cy = w / 2, h / 2
    out = []
    shore = blob(cx, cy, w * 0.45, h * 0.41, seed, 22, 0.06, 3, 0.06)
    out.append(path(shore, fill=WATER, op=WATER_OP))
    out.append(path(shore, INK, SW, 0.88))
    # 复线（断续，手绘感）
    out.append(path(blob(cx, cy, w * 0.462, h * 0.422, seed + 3, 22, 0.06, 3, 0.06),
                    INK3, 1.5, 0.3, dash="30 22"))
    # 岸外短笔
    r = random.Random(seed + 9)
    for i in range(12):
        a = math.tau * i / 12 + r.uniform(-0.15, 0.15)
        x, y = cx + math.cos(a) * w * 0.475, cy + math.sin(a) * h * 0.435
        out.append(wob(x, y, x + math.cos(a) * r.uniform(10, 26), y + math.sin(a) * r.uniform(8, 20),
                       INK3, 1.6, seed + i, 0.8, 2, 0.26))
    # 岛
    for k in range(islands):
        ix = cx + (k - (islands - 1) / 2) * w * 0.27 + r.uniform(-6, 6)
        iy = cy + r.uniform(-h * 0.08, h * 0.08)
        il = blob(ix, iy, w * 0.080, h * 0.062, seed + 20 + k, 14, 0.18, 2, 0.14)
        out.append(path(il, INK2, 2.4, 0.8))
        # 岛上两三点碎石/灌木（避免两点对称像眼睛）
        for m in range(3):
            out.append(M.circle(ix + (m - 1) * 9 + 3, iy + (m % 2) * 5 - 3, 1.6,
                                fill=INK3, op=0.45))
    # 水纹（短横）
    for k in range(5):
        y = cy + h * (0.16 - 0.08 * (k % 3))
        x0 = cx - w * (0.18 - 0.03 * k)
        out.append(wob(x0, y, x0 + w * 0.16, y, INK3, 1.4, seed + 40 + k, 0.9, 3, 0.24))
    return out


def el_river(w, h, seed, width_k=0.30, n=8):
    """河道：两端收细、带弯，外侧岸线加排线。"""
    r = random.Random(seed)
    out = []
    y0 = h * 0.5
    top, bot = [], []
    for i in range(n):
        t = i / (n - 1)
        x = w * (0.02 + 0.96 * t)
        y = y0 + math.sin(t * math.pi * 1.7 + 0.4) * h * 0.10
        taper = math.sin(t * math.pi) ** 0.55          # 两端细
        hw = h * width_k * (0.35 + 0.65 * taper) * (0.85 + 0.3 * math.sin(t * 5.1 + r.random()))
        top.append((x, y - hw))
        bot.append((x, y + hw))
    d = smooth(top, closed=False, tension=0.9) + " " + \
        " ".join("L %s %s" % (f(p[0]), f(p[1])) for p in reversed(bot)) + " Z"
    out.append(path(d, fill=WATER, op=WATER_OP * 0.9))
    out.append(path(smooth(top, closed=False, tension=0.9), INK, SW, 0.86))
    out.append(path(smooth(bot, closed=False, tension=0.9), INK, SW, 0.86))
    # 河心短流线
    for i in range(7):
        t = 0.10 + 0.78 * i / 6
        x = w * t
        y = y0 + math.sin(t * math.pi * 1.7 + 0.4) * h * 0.10
        out.append(wob(x - w * 0.035, y, x + w * 0.035, y, INK3, 1.4, seed + i, 0.8, 3, 0.26))
    # 外侧弯道排线
    for i in (2, 4, 6):
        t = i / (n - 1)
        x = w * (0.02 + 0.96 * t)
        y = y0 + math.sin(t * math.pi * 1.7 + 0.4) * h * 0.10
        out.append(wob(x, y + h * width_k * 0.9, x + w * 0.03, y + h * width_k * 1.35,
                       INK3, 1.4, seed + 30 + i, 0.7, 3, 0.3))
    return out


# ---------------------------------------------------------------- 植被 / 地貌
def conifer(x, y, s, seed):
    out = [wob(x, y, x, y - s * 0.22, INK2, 1.8, seed, 0.4, 2, 0.75)]
    for i, k in enumerate((0.40, 0.62, 0.82)):
        hw = s * (0.26 - i * 0.055)
        yy = y - s * (0.18 + i * 0.20)
        out.append(wob(x - hw, yy, x, yy - s * 0.30, INK2, 1.9, seed + i * 3, 0.5, 2, 0.82))
        out.append(wob(x + hw, yy, x, yy - s * 0.30, INK2, 1.9, seed + i * 5, 0.5, 2, 0.82))
    out.append(wob(x - s * 0.05, y, x + s * 0.05, y, INK3, 1.4, seed + 9, 0.5, 2, 0.4))
    return out


def broadleaf(x, y, s, seed):
    out = [wob(x, y, x, y - s * 0.26, INK2, 1.8, seed, 0.4, 2, 0.75)]
    c = blob(x, y - s * 0.48, s * 0.24, s * 0.21, seed + 11, 10, 0.12, 2, 0.10)
    out.append(path(c, INK2, 2.0, 0.82))
    out.append(wob(x + s * 0.02, y - s * 0.44, x + s * 0.10, y - s * 0.38, INK3, 1.3,
                   seed + 5, 0.5, 2, 0.35))
    return out


def el_forest(w, h, seed, kind="conifer"):
    r = random.Random(seed)
    out = []
    # 沿等高线一条带，树小而密
    rows = 3
    for j in range(rows):
        cnt = 7 - j
        for i in range(cnt):
            x = w * (0.08 + 0.84 * (i + r.uniform(0.15, 0.85)) / max(1, cnt))
            y = h * (0.88 - 0.20 * j) + r.uniform(-h * 0.02, h * 0.02)
            s = h * r.uniform(0.22, 0.28)
            if kind == "conifer":
                out += conifer(x, y, s, seed + i * 13 + j * 29)
            else:
                out += broadleaf(x, y, s, seed + i * 17 + j * 31)
    out.append(wob(w * 0.04, h * 0.93, w * 0.96, h * 0.93, INK3, 1.6, seed + 3, 1.4, 7, 0.25))
    return out


def el_marsh(w, h, seed):
    r = random.Random(seed)
    out = []
    for k in range(3):
        cx = w * (0.24 + 0.26 * k) + r.uniform(-6, 6)
        cy = h * (0.52 + r.uniform(-0.06, 0.06))
        p = blob(cx, cy, w * 0.085, h * 0.09, seed + k, 12, 0.10, 2, 0.10)
        out.append(path(p, fill=WATER, op=WATER_OP * 0.7))
        out.append(path(p, INK2, 2.0, 0.7, dash="12 9"))
    # 芦苇与草丛
    for k in range(20):
        x = w * r.uniform(0.06, 0.94)
        y = h * r.uniform(0.72, 0.94)
        s = h * r.uniform(0.10, 0.18)
        out.append(wob(x, y, x + r.uniform(-2, 2), y - s, INK2, 1.6, seed + k, 0.5, 2, 0.72))
        out.append(wob(x + 4, y, x + 7, y - s * 0.75, INK3, 1.2, seed + k + 40, 0.4, 2, 0.55))
    # 水纹虚线
    for k in range(4):
        y = h * (0.62 + 0.06 * k)
        out.append(wob(w * 0.10, y, w * 0.90, y, INK3, 1.3, seed + 60 + k, 1.0, 5, 0.22, ))
    return out


def el_dunes(w, h, seed):
    """沙丘：多道新月形丘脊 + 背风面排线 + 沙粒点。"""
    out = []
    r = random.Random(seed)
    for k in range(6):
        y = h * (0.20 + 0.13 * k)
        x0 = w * (0.04 + 0.06 * (k % 2))
        x1 = w * (0.92 - 0.05 * (k % 3))
        mid = (x0 + x1) * 0.5
        d = "M %s %s Q %s %s %s %s" % (f(x0), f(y), f(mid), f(y - h * 0.16), f(x1), f(y))
        out.append(path(d, INK2, 2.4, 0.78))
        # 背风面（右侧陡坡）排线
        for i in range(6):
            t = 0.58 + 0.07 * i
            bx = x0 + (x1 - x0) * t
            by = y - h * 0.16 * math.sin(t * math.pi)
            out.append(wob(bx, by + 3, bx + 9, by + 15, INK3, 1.3,
                           seed + k * 11 + i, 0.5, 3, 0.26))
    # 沙粒点与零星草
    for k in range(34):
        out.append(M.circle(w * r.uniform(0.04, 0.96), h * r.uniform(0.30, 0.96),
                            r.uniform(0.7, 1.5), fill=INK3, op=0.24))
    for k in range(5):
        x, y = w * r.uniform(0.10, 0.90), h * r.uniform(0.55, 0.92)
        out.append(wob(x, y, x + 4, y - 7, INK3, 1.2, seed + 90 + k, 0.4, 2, 0.4))
    return out


def el_grassland(w, h, seed):
    """草原：两道缓等高线 + 较密的草簇。"""
    r = random.Random(seed)
    out = []
    out.append(wob(w * 0.03, h * 0.40, w * 0.97, h * 0.33, INK3, 1.5, seed + 1, 2.0, 8, 0.20))
    out.append(wob(w * 0.05, h * 0.66, w * 0.95, h * 0.60, INK3, 1.5, seed + 2, 2.0, 8, 0.18))
    for k in range(44):
        x = w * r.uniform(0.04, 0.96)
        y = h * r.uniform(0.36, 0.94)
        s = h * r.uniform(0.06, 0.13)
        for t in (-1, 0, 1):
            out.append(wob(x + t * 4, y, x + t * 7, y - s * (1 if t == 0 else 0.72),
                           INK2 if t == 0 else INK3, 1.5 if t == 0 else 1.1,
                           seed + k * 3 + t, 0.4, 2, 0.6 if t == 0 else 0.4))
    return out


# ---------------------------------------------------------------- 聚落 / 设施
def el_city(w, h, seed):
    out = []
    x0, y0 = w * 0.17, h * 0.30
    x1, y1 = w * 0.83, h * 0.80
    for a, b in (((x0, y0), (x1, y0)), ((x1, y0), (x1, y1)),
                 ((x1, y1), (x0, y1)), ((x0, y1), (x0, y0))):
        out.append(wob(a[0], a[1], b[0], b[1], INK, SW, seed + int(a[0]), 1.0, 4, 0.86))
    for i in range(7):
        cx = x0 + (x1 - x0) * (i + 0.5) / 7
        out.append(M.rect(cx - 4.5, y0 - 7, 9, 7, stroke=INK, sw=2.0))
    for (cx, cy) in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
        out.append(M.rect(cx - 12, cy - 12, 24, 24, stroke=INK, sw=2.4))
        out.append(M.rect(cx - 12, cy - 16, 24, 5, stroke=INK, sw=1.8))
    # 城门（拱）
    gw = w * 0.10
    out.append(path("M %s %s L %s %s A %s %s 0 0 1 %s %s L %s %s" %
                    (f(w * 0.5 - gw), f(y1), f(w * 0.5 - gw), f(y1 - h * 0.13),
                     f(gw), f(h * 0.13), f(w * 0.5 + gw), f(y1 - h * 0.13), f(w * 0.5 + gw), f(y1)),
                    INK, 2.6, 0.86))
    # 城内屋顶（成排）
    for j, ry in enumerate((0.44, 0.56, 0.68)):
        for i in range(3):
            cx = w * (0.30 + 0.20 * i)
            cy = h * ry
            out.append(wob(cx - w * 0.055, cy, cx, cy - h * 0.05, INK2, 2.0,
                           seed + i * 3 + j * 7, 0.6, 2, 0.78))
            out.append(wob(cx + w * 0.055, cy, cx, cy - h * 0.05, INK2, 2.0,
                           seed + i * 5 + j * 11, 0.6, 2, 0.78))
            out.append(M.rect(cx - w * 0.045, cy, w * 0.09, h * 0.05, stroke=INK2, sw=1.6))
    return out


def el_pass(w, h, seed):
    out = []
    base = h * 0.86
    out.append(wob(w * 0.06, base, w * 0.94, base, INK, SW, seed, 1.0, 4, 0.86))
    for cx in (w * 0.24, w * 0.76):
        out.append(M.rect(cx - w * 0.11, base - h * 0.36, w * 0.22, h * 0.36, stroke=INK, sw=2.4))
        for k in range(3):
            out.append(M.rect(cx - w * 0.11 + k * w * 0.075, base - h * 0.42, w * 0.035, 9,
                              stroke=INK, sw=1.8))
    out.append(path("M %s %s L %s %s A %s %s 0 0 1 %s %s L %s %s" %
                    (f(w * 0.39), f(base), f(w * 0.39), f(base - h * 0.17),
                     f(w * 0.11), f(h * 0.17), f(w * 0.61), f(base - h * 0.17), f(w * 0.61), f(base)),
                    INK, 2.4, 0.86))
    for k in range(4):
        out.append(wob(w * 0.12 + k * 8, base - h * 0.30, w * 0.12 + k * 8, base - h * 0.06,
                       INK3, 1.4, seed + k, 0.6, 2, 0.3))
    return out


def el_hamlet(w, h, seed):
    out = []
    base = h * 0.86
    for k, (rx, ry, s) in enumerate(((0.32, 0.26, 0.20), (0.60, 0.34, 0.22), (0.46, 0.56, 0.18))):
        cx, cy = w * rx, base - h * ry
        rw = w * s
        out.append(wob(cx - rw, cy, cx, cy - rw * 0.5, INK2, 2.2, seed + k, 0.6, 2, 0.82))
        out.append(wob(cx + rw, cy, cx, cy - rw * 0.5, INK2, 2.2, seed + k + 7, 0.6, 2, 0.82))
        out.append(M.rect(cx - rw * 0.8, cy, rw * 1.6, rw * 0.55, stroke=INK2, sw=1.8))
    out += broadleaf(w * 0.82, base, h * 0.22, seed + 30)
    out.append(wob(w * 0.10, base, w * 0.92, base, INK3, 1.6, seed + 40, 1.0, 5, 0.26))
    return out


def el_ferry(w, h, seed):
    """渡口：木栈桥 + 小舟 + 水纹（地图小符号的尺度）。"""
    out = []
    wy = h * 0.66
    # 水岸线
    out.append(wob(w * 0.04, wy, w * 0.96, wy, INK2, 2.0, seed, 1.0, 5, 0.7))
    # 水纹
    for k in range(3):
        out.append(wob(w * 0.08, wy + 9 + k * 8, w * 0.40, wy + 9 + k * 8, INK3, 1.3,
                       seed + k, 0.9, 4, 0.24))
    # 栈桥：两根桩 + 桥面
    out.append(wob(w * 0.24, wy - h * 0.16, w * 0.24, wy + h * 0.06, INK, 2.6, seed + 5, 0.4, 2, 0.86))
    out.append(wob(w * 0.34, wy - h * 0.16, w * 0.34, wy + h * 0.06, INK, 2.6, seed + 7, 0.4, 2, 0.86))
    out.append(wob(w * 0.18, wy - h * 0.16, w * 0.40, wy - h * 0.16, INK, 2.4, seed + 6, 0.5, 2, 0.86))
    # 小舟：船身（浅弧）+ 桅杆 + 桨
    out.append(path("M %s %s Q %s %s %s %s Q %s %s %s %s" %
                    (f(w * 0.48), f(wy - h * 0.10), f(w * 0.55), f(wy + h * 0.02),
                     f(w * 0.64), f(wy - h * 0.09), f(w * 0.58), f(wy - h * 0.14),
                     f(w * 0.50), f(wy - h * 0.14)), INK, 2.4, 0.86))
    out.append(wob(w * 0.57, wy - h * 0.09, w * 0.57, wy - h * 0.24, INK2, 2.0, seed + 8, 0.4, 2, 0.78))
    out.append(wob(w * 0.57, wy - h * 0.20, w * 0.68, wy - h * 0.02, INK3, 1.6, seed + 9, 0.5, 2, 0.6))
    return out


def el_mine(w, h, seed):
    out = []
    out.append(path("M %s %s Q %s %s %s %s" % (f(w * 0.06), f(h * 0.84), f(w * 0.50), f(h * 0.14),
                                              f(w * 0.94), f(h * 0.84)), INK, SW, 0.88))
    for k in range(6):
        out.append(wob(w * 0.52 + k * 9, h * 0.36 + k * 12, w * 0.62 + k * 9, h * 0.36 + k * 12 + 18,
                       INK3, 1.5, seed + k, 0.7, 3, 0.3))
    out.append(path("M %s %s L %s %s A %s %s 0 0 1 %s %s L %s %s" %
                    (f(w * 0.34), f(h * 0.84), f(w * 0.34), f(h * 0.62),
                     f(w * 0.16), f(h * 0.12), f(w * 0.66), f(w * 0.66), f(w * 0.66), f(h * 0.84)),
                    INK, 2.6, 0.88))
    out.append(M.rect(w * 0.32, h * 0.58, w * 0.36, 7, stroke=INK2, sw=2.0))
    out.append(M.rect(w * 0.33, h * 0.58, 6, h * 0.26, stroke=INK2, sw=1.8))
    out.append(M.rect(w * 0.61, h * 0.58, 6, h * 0.26, stroke=INK2, sw=1.8))
    return out


def el_farmland(w, h, seed):
    out = []
    out.append(path("M %s %s L %s %s L %s %s L %s %s Z" %
                    (f(w * 0.10), f(h * 0.22), f(w * 0.90), f(h * 0.16),
                     f(w * 0.86), f(h * 0.84), f(w * 0.14), f(h * 0.80)), INK2, 2.4, 0.8))
    for i in range(8):
        t = (i + 0.5) / 8
        out.append(wob(w * 0.12, h * (0.22 + 0.62 * t), w * 0.88, h * (0.16 + 0.68 * t),
                       INK3, 1.5, seed + i, 1.0, 4, 0.32))
    out.append(wob(w * 0.50, h * 0.18, w * 0.52, h * 0.82, INK3, 1.6, seed + 20, 0.9, 4, 0.28))
    out += broadleaf(w * 0.10, h * 0.86, h * 0.18, seed + 40)
    return out


# ---------------------------------------------------------------- 清单
DECOS = [
    ("Deco_Mountain_Range_01", 1000, 420, lambda w, h: el_mountain_range(w, h, 5, 101), "山脉·主脉"),
    ("Deco_Mountain_Range_02", 760, 340, lambda w, h: el_mountain_range(w, h, 3, 202), "山脉·支脉"),
    ("Deco_Mountain_Range_Long", 1400, 340, lambda w, h: el_mountain_range(w, h, 8, 303), "山脉·连绵长条"),
    ("Deco_Mountain_Peak", 300, 270, lambda w, h: el_peak(w, h, 404), "孤峰"),
    ("Deco_Hills", 640, 220, lambda w, h: el_hills(w, h, 505), "丘陵"),
    ("Deco_Lake_Large", 700, 460, lambda w, h: el_lake(w, h, 606, 2), "湖泊·大（含 2 岛）"),
    ("Deco_Lake_Medium", 440, 300, lambda w, h: el_lake(w, h, 707, 1), "湖泊·中"),
    ("Deco_Lake_Small", 220, 150, lambda w, h: el_lake(w, h, 808, 0), "湖泊·小"),
    ("Deco_River_Main", 1000, 260, lambda w, h: el_river(w, h, 909, 0.30), "河流·主"),
    ("Deco_River_Branch", 600, 190, lambda w, h: el_river(w, h, 111, 0.24, 7), "河流·支"),
    ("Deco_Forest_Conifer", 480, 280, lambda w, h: el_forest(w, h, 222, "conifer"), "森林·针叶"),
    ("Deco_Forest_Broadleaf", 480, 280, lambda w, h: el_forest(w, h, 333, "broadleaf"), "森林·阔叶"),
    ("Deco_Marsh", 560, 260, lambda w, h: el_marsh(w, h, 444), "沼泽/水泽"),
    ("Deco_Dunes", 580, 230, lambda w, h: el_dunes(w, h, 555), "沙丘/荒漠"),
    ("Deco_Grassland", 560, 200, lambda w, h: el_grassland(w, h, 666), "草原"),
    ("Deco_City", 260, 260, lambda w, h: el_city(w, h, 777), "城池"),
    ("Deco_Pass", 230, 230, lambda w, h: el_pass(w, h, 888), "关隘"),
    ("Deco_Hamlet", 200, 185, lambda w, h: el_hamlet(w, h, 999), "村落"),
    ("Deco_Ferry", 180, 165, lambda w, h: el_ferry(w, h, 121), "渡口"),
    ("Deco_Mine", 200, 185, lambda w, h: el_mine(w, h, 232), "矿场"),
    ("Deco_Farmland", 300, 250, lambda w, h: el_farmland(w, h, 343), "农田"),
]


def main():
    out_dir = HERE / "_out4"
    out_dir.mkdir(parents=True, exist_ok=True)
    for name, w, h, fn, _d in DECOS:
        doc = M.Doc(w, h)
        doc.add(fn(w, h))
        (out_dir / (name + ".svg")).write_text(doc.render(name), encoding="utf-8")
    print("生成", len(DECOS), "个装饰件（v2）")


if __name__ == "__main__":
    main()
