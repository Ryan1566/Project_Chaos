# -*- coding: utf-8 -*-
"""秦汉风地图装饰件 + 斜向山脉 + 大幅山脉图（v2 几何 + 秦汉配色语言）。

底座几何复用已验证的 gen_map_deco_pt（写实版），在其上做三件事：
  1) 换成秦汉漆器 / 画像石 / 早期青绿山水配色（石青、石绿、赭石、朱砂、墨、金）；
  2) 斜向山脉：山脊基线沿对角线，峰体保持竖直；
  3) 大幅山脉图：多层山带 + 云气纹 + 山脚林带与河，接近"青绿山水"的构图。
"""
import math
import random
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gen_map_deco_pt as P  # noqa: E402

# ── ① 秦汉调色：覆盖模块级常量（函数体内引用，调用时生效）
INK = "#2A2420"
P.INK3 = "#6E5C46"
P.PAPER = "#E7DDB4"
P.ROCK_HI = "#B6C9CC"
P.ROCK_MID = "#7FA3AE"
P.ROCK_LO = "#4E7480"
P.ROCK_DK = INK
P.SNOW = "#EFE7D0"
P.FOREST_HI = "#8FA97B"
P.FOREST_MID = "#5F7A4C"
P.FOREST_LO = "#3F5540"
P.WATER_HI = "#B6C9CC"
P.WATER_MID = "#7FA3AE"
P.WATER_LO = "#3E6E7A"
P.SAND_HI = "#D9C79B"
P.SAND_MID = "#C0A877"
P.SAND_LO = "#9A7B50"
P.FIELD_A = "#C8B67F"
P.FIELD_B = "#B9A571"
P.FIELD_C = "#D4C48C"
P.ROOF_HI = "#5E5850"      # 汉瓦
P.ROOF_LO = "#3A3630"
P.WALL_HI = "#D8C6A2"      # 夯土
P.WALL_LO = "#9A7B50"
P.SHADOW = INK

AZ_FAR, AZ_MID = "#B6C9CC", "#7FA3AE"
GRN_MID, GRN_NEAR = "#93AC7C", "#5F7A4C"
OCHRE_HI, OCHRE_LO = "#B98A5E", "#8A5A3A"
RED = "#B03A2C"
GOLD = "#C9A227"

Doc = P.Doc
smooth = P.smooth
blob_pts = P.blob_pts


def _bez(p0, c, p1, t):
    return ((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * c[0] + t ** 2 * p1[0],
            (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * c[1] + t ** 2 * p1[1])


def _flank(p0, p1, bulge, n=9):
    """二次曲线采样一翼轮廓（bulge 控制外鼓/内凹）。"""
    c = ((p0[0] + p1[0]) / 2 + bulge[0], (p0[1] + p1[1]) / 2 + bulge[1])
    return [_bez(p0, c, p1, i / (n - 1)) for i in range(n)]


# ── ② 云气纹
def cloud_curl(D, cx, cy, s, color=AZ_MID, op=0.5, sw=1.5, turns=1.5, flip=1):
    pts = []
    N = 28
    for i in range(N):
        t = i / (N - 1)
        a = t * turns * math.tau
        rad = s * (0.12 + 0.88 * t)
        pts.append((cx + math.cos(a) * rad * flip, cy + math.sin(a) * rad * 0.62))
    D.path(smooth(pts, closed=False, tension=1.0), "none", op, stroke=color, sw=sw)


def cloud_band(D, x0, x1, y, hgt, seed, color=AZ_FAR, op=0.45, curls=3):
    r = random.Random(seed)
    n = max(3, int((x1 - x0) / max(1.0, hgt * 1.8)))
    top, bot = [], []
    for i in range(n):
        t = i / (n - 1)
        x = x0 + (x1 - x0) * t
        wob = math.sin(t * 5.1 + r.random() * 0.3) * hgt * 0.26
        top.append((x, y - hgt * 0.5 + wob - hgt * 0.16 * math.sin(t * math.pi)))
        bot.append((x, y + hgt * 0.20 + wob * 0.5))
    d = smooth(top, closed=False) + " " + " ".join("L %s %s" % (P.f(p[0]), P.f(p[1])) for p in reversed(bot)) + " Z"
    D.path(d, D.lin([(0, color, 0.0), (0.45, color, op), (1, color, 0.0)], 0, 0, 0, 1), 1.0)
    for k in range(curls):
        t = (k + 0.5) / curls
        cloud_curl(D, x0 + (x1 - x0) * t + r.uniform(-hgt, hgt),
                   y + r.uniform(-hgt * 0.12, hgt * 0.12), hgt * r.uniform(0.34, 0.52),
                   color=AZ_MID, op=0.28, sw=1.2)


# ── ③ 秦汉山体：复用 v2 的「多层剪影 + 空气透视」画法，叠汉色与斜基线
def ridge_profile_slope(w, base, h, peaks, seed, slope=0.0, sharp=0.55):
    """山脊剪影；slope≠0 时基线沿对角线（峰体仍竖直）。"""
    r = random.Random(seed)

    def by(x):
        return base - slope * (x - w * 0.5)

    pts = [(w * 0.02, by(w * 0.02))]
    for i in range(peaks):
        t = (i + 0.5) / peaks
        px = w * (0.06 + 0.88 * t) + r.uniform(-w * 0.02, w * 0.02)
        b = by(px)
        ph = h * (0.42 + 0.34 * r.random()) * (0.78 + 0.22 * math.sin(math.pi * t))
        pts.append((px - w * 0.06, b - ph * 0.42))
        pts.append((px + r.uniform(-w * 0.01, w * 0.01), b - ph))
        pts.append((px + w * 0.07, b - ph * sharp * 0.55))
    pts.append((w * 0.98, by(w * 0.98)))
    return pts, by


def range_layer_qh(D, w, h, base, peaks, ct, cl, seed, slope=0.0, tex=INK, tex_n=1.0,
                   gold=True, snow=0):
    pts, by = ridge_profile_slope(w, base, h, peaks, seed, slope)
    d = smooth(pts, closed=False, tension=0.55)
    d2 = d + " L %s %s L %s %s Z" % (P.f(w * 1.02), P.f(by(w * 1.02) + h * 0.04),
                                     P.f(-w * 0.02), P.f(by(-w * 0.02) + h * 0.04))
    D.path(d2, D.lin([(0, ct, None), (0.30, cl, None), (0.74, cl, 0.72),
                      (1, P.PAPER, 0.0)], 0, 0, 0, 1), 1.0)
    r = random.Random(seed + 5)
    for i in range(peaks):
        t = (i + 0.5) / peaks
        px = w * (0.06 + 0.88 * t)
        b = by(px)
        ph = h * (0.42 + 0.20 * r.random())
        # 右坡阴影
        D.poly([(px, b - ph), (px + w * 0.10, b), (px + w * 0.02, b)],
               D.lin([(0, INK, 0.18), (1, INK, 0.02)]), 0.8)
        # 墨脊
        D.path("M %s %s Q %s %s %s %s" % (P.f(px), P.f(b - ph * 0.96), P.f(px - w * 0.03),
                                          P.f(b - ph * 0.55), P.f(px - w * 0.08), P.f(b - ph * 0.06)),
               "none", 0.42, stroke=INK, sw=max(1.0, h * 0.006))
        # 金线受光（左坡）
        if gold:
            D.path("M %s %s Q %s %s %s %s" % (P.f(px - w * 0.05), P.f(b - ph * 0.40),
                                              P.f(px - w * 0.03), P.f(b - ph * 0.72),
                                              P.f(px), P.f(b - ph)),
                   "none", 0.42, stroke=GOLD, sw=max(1.0, h * 0.005))
    if tex_n > 0:
        def area(rr):
            return (w * rr.uniform(0.03, 0.97), rr.uniform(base - h * 0.6, base + h * 0.02))
        P.dabs(D, area, int(w * h / 3400 * tex_n), tex, 1.4, 4.8, 0.04, 0.12, seed + 9)
    if snow:
        for i in range(snow):
            t = 0.30 + 0.40 * i / max(1, snow - 1)
            px = w * (0.06 + 0.88 * t)
            b = by(px)
            py = b - h * 0.60
            D.path("M %s %s Q %s %s %s %s L %s %s Z" %
                   (P.f(px - w * 0.030), P.f(py + h * 0.05), P.f(px), P.f(py - h * 0.03),
                    P.f(px + w * 0.030), P.f(py + h * 0.05), P.f(px), P.f(py + h * 0.07)), P.SNOW, 0.5)


def peak_unit(D, cx, base_y, w, h, seed, ct, cl, ink_edge=True, gold=True):
    """一座独立山体：带山肩的轮廓 + 渐变体 + 右坡暗面 + 墨边 + 左坡金线。"""
    r = random.Random(seed)
    px = cx + r.uniform(-w * 0.04, w * 0.04)
    py = base_y - h
    L, R = cx - w * 0.5, cx + w * 0.5
    # 左：山脚→山肩→峰；右：峰→山肩→山脚（折点让轮廓不是直边三角）
    pts = [(L, base_y),
           (cx - w * 0.34, base_y - h * 0.30),
           (cx - w * 0.16, base_y - h * 0.64),
           (px, py),
           (px + w * 0.13, base_y - h * 0.58),
           (cx + w * 0.30, base_y - h * 0.26),
           (R, base_y)]
    sil = smooth(pts, closed=False, tension=0.85)
    D.path(sil + " L %s %s L %s %s Z" % (P.f(R), P.f(base_y + h * 0.05),
                                        P.f(L), P.f(base_y + h * 0.05)),
           D.lin([(0, ct, None), (0.50, cl, None), (1, cl, 0.92)], 0, 0, 0, 1), 0.97)
    # 右坡暗面（沿右侧山肩）
    D.path("M %s %s Q %s %s %s %s Q %s %s %s %s L %s %s Z" %
           (P.f(px), P.f(py), P.f(px + w * 0.06), P.f(base_y - h * 0.5),
            P.f(cx + w * 0.20), P.f(base_y - h * 0.22), P.f(cx + w * 0.34), P.f(base_y - h * 0.05),
            P.f(R), P.f(base_y), P.f(px + w * 0.05), P.f(base_y)),
           D.lin([(0, INK, 0.24), (1, INK, 0.05)]), 0.9)
    if ink_edge:
        D.path(sil, "none", 0.62, stroke=INK, sw=max(1.0, h * 0.018))
    if gold:
        D.path("M %s %s Q %s %s %s %s" %
               (P.f(cx - w * 0.20), P.f(base_y - h * 0.40), P.f(px - w * 0.10),
                P.f(base_y - h * 0.78), P.f(px), P.f(py)),
               "none", 0.55, stroke=GOLD, sw=max(1.0, h * 0.012))


def _rows(D, w, h, rows, base, slope, seed):
    """按"排"布置山体：沿（可斜）基线成串，位置与大小都带随机起伏。"""
    for ri, (cnt, uw, ct, cl, dy) in enumerate(rows):
        step = 0.86 / max(1, cnt - 1) if cnt > 1 else 0
        for i in range(cnt):
            rr = random.Random(seed + i * 13 + ri * 71)
            t = (0.07 + step * i) + (rr.uniform(-0.025, 0.025) if cnt > 1 else 0.43)
            cx = w * t
            by = base + h * dy - slope * (cx - w * 0.5)
            uwi = uw * rr.uniform(0.80, 1.22)
            hh = uwi * 0.72 * rr.uniform(0.86, 1.16)
            peak_unit(D, cx, by, uwi, hh, seed + i * 29 + ri * 211, ct, cl)


def el_range_qh(w, h, peaks, seed, slope=0.0, cloud=True):
    """山脉：远石青 / 中石绿 / 近赭石，三排成串（斜向靠 slope 把基线倾斜）。"""
    D = Doc(w, h)
    base = h * 0.78
    uw = w * (0.30 if peaks <= 3 else 0.24)
    rows = [(max(2, peaks - 1), uw * 0.92, AZ_FAR, AZ_MID, -0.10),
            (peaks, uw, GRN_MID, GRN_NEAR, 0.02),
            (max(2, peaks - 1), uw * 0.96, OCHRE_HI, OCHRE_LO, 0.13)]
    _rows(D, w, h, rows, base, slope, seed)
    if cloud:
        cloud_band(D, w * 0.02, w * 0.98, base - h * 0.06, h * 0.13, seed + 55, curls=2)
        cloud_band(D, w * 0.04, w * 0.96, base + h * 0.19, h * 0.11, seed + 66, curls=2)
    return D


def el_hills_qh(w, h, seed, slope=0.0):
    D = Doc(w, h)
    r = random.Random(seed)
    base = h * 0.84
    for li, (n, rh, ca, cb) in enumerate(((5, h * 0.26, AZ_FAR, AZ_MID),
                                          (4, h * 0.34, GRN_MID, GRN_NEAR),
                                          (3, h * 0.42, OCHRE_HI, OCHRE_LO))):
        for i in range(n):
            t = (i + r.uniform(0.15, 0.85)) / n
            cx = w * (0.08 + 0.84 * t)
            by = base - slope * (cx - w * 0.5) + h * 0.05 * li
            rw = w * r.uniform(0.16, 0.25)
            hh = rh * r.uniform(0.85, 1.05)
            D.poly(blob_pts(cx, by - hh * 0.40, rw, hh * 0.70, seed + i * 7 + li * 31, 16, 0.07, 2, 0.06),
                   D.lin([(0, ca, None), (1, cb, None)], 0, 0, 0, 1), 0.95)
            cloud_curl(D, cx - rw * 0.55, by - hh * 0.10, hh * 0.20, AZ_MID, 0.22, 1.0)
    return D


# ── ④ 秦汉建筑：阙楼 + 汉瓦
def que_tower(D, x, y_top, w, h, seed):
    """阙楼：夯土楼身 + 朱柱 + 斗拱 + 黑瓦歇山顶。y_top 为楼底。"""
    D.poly([(x - w * 0.5, y_top), (x + w * 0.5, y_top), (x + w * 0.44, y_top + h),
            (x - w * 0.44, y_top + h)], D.lin([(0, P.WALL_HI, None), (1, P.WALL_LO, None)], 0, 0, 0, 1), 1.0)
    for k in (-1, 1):
        D.poly([(x + k * w * 0.30 - w * 0.055, y_top + h * 0.06), (x + k * w * 0.30 + w * 0.055, y_top + h * 0.06),
                (x + k * w * 0.30 + w * 0.055, y_top + h), (x + k * w * 0.30 - w * 0.055, y_top + h)], RED, 0.92)
    # 斗拱层
    D.poly([(x - w * 0.54, y_top - h * 0.06), (x + w * 0.54, y_top - h * 0.06),
            (x + w * 0.54, y_top), (x - w * 0.54, y_top)], "#E2D2AE", 1.0)
    # 黑瓦
    D.poly([(x - w * 0.70, y_top - h * 0.06), (x + w * 0.70, y_top - h * 0.06),
            (x + w * 0.46, y_top - h * 0.30), (x - w * 0.46, y_top - h * 0.30)],
           D.lin([(0, "#6E6860", None), (1, "#312D28", None)], 0, 0, 0, 1), 1.0)
    D.poly([(x - w * 0.46, y_top - h * 0.30), (x + w * 0.46, y_top - h * 0.30),
            (x, y_top - h * 0.40)], "#312D28", 1.0)


def el_city_qh(w, h, seed):
    D = Doc(w, h)
    r = random.Random(seed)
    x0, y0, x1, y1 = w * 0.13, h * 0.34, w * 0.87, h * 0.84
    D.ell(w * 0.5, y1 + h * 0.03, w * 0.44, h * 0.05, INK, 0.14)
    # 夯土城墙 + 夯层
    D.poly([(x0, y0), (x1, y0), (x1, y1), (x0, y1)],
           D.lin([(0, P.WALL_HI, None), (1, P.WALL_LO, None)], 0, 0, 0, 1), 1.0)
    for k in range(5):
        yy = y0 + (y1 - y0) * (k + 0.5) / 5
        D.path("M %s %s L %s %s" % (P.f(x0 + 4), P.f(yy), P.f(x1 - 4), P.f(yy)), "none", 0.16, stroke=INK, sw=1.2)
    # 女墙 + 垛口
    D.poly([(x0, y0 - h * 0.05), (x1, y0 - h * 0.05), (x1, y0), (x0, y0)], "#E2D2AE", 0.95)
    for i in range(9):
        cx = x0 + (x1 - x0) * (i + 0.5) / 9
        D.poly([(cx - w * 0.016, y0 - h * 0.09), (cx + w * 0.016, y0 - h * 0.09),
                (cx + w * 0.016, y0 - h * 0.05), (cx - w * 0.016, y0 - h * 0.05)], "#E2D2AE", 0.95)
    # 两座阙楼夹门
    que_tower(D, w * 0.30, y0 + h * 0.02, w * 0.19, h * 0.30, seed + 3)
    que_tower(D, w * 0.70, y0 + h * 0.02, w * 0.19, h * 0.30, seed + 7)
    # 城门洞
    D.path("M %s %s L %s %s A %s %s 0 0 1 %s %s L %s %s Z" %
           (P.f(w * 0.44), P.f(y1), P.f(w * 0.44), P.f(y1 - h * 0.17), P.f(w * 0.06), P.f(h * 0.17),
            P.f(w * 0.56), P.f(y1 - h * 0.17), P.f(w * 0.56), P.f(y1)), INK, 0.92)
    # 城内屋（朱墙黑瓦）
    for j in range(2):
        for i in range(3):
            cx = w * (0.28 + 0.22 * i)
            cy = y1 - h * (0.11 + 0.16 * j)
            D.poly([(cx - w * 0.065, cy), (cx + w * 0.065, cy), (cx + w * 0.06, cy + h * 0.07),
                    (cx - w * 0.06, cy + h * 0.07)], RED, 0.85)
            D.poly([(cx - w * 0.085, cy), (cx, cy - h * 0.075), (cx + w * 0.085, cy)],
                   D.lin([(0, "#6E6860", None), (1, "#312D28", None)], 0, 0, 0, 1), 1.0)
            D.poly([(cx - w * 0.085, cy), (cx + w * 0.085, cy), (cx + w * 0.085, cy + h * 0.008),
                    (cx - w * 0.085, cy + h * 0.008)], INK, 0.35)
    return D


def el_pass_qh(w, h, seed):
    D = Doc(w, h)
    base = h * 0.88
    for i in range(2):
        range_layer_qh(D, w, h, base - h * 0.02, 2, GRN_MID, GRN_NEAR, seed + i * 17, 0.0, INK, 0.8, True, 0)
    D.poly([(w * 0.30, base - h * 0.38), (w * 0.70, base - h * 0.38), (w * 0.70, base), (w * 0.30, base)],
           D.lin([(0, P.WALL_HI, None), (1, P.WALL_LO, None)], 0, 0, 0, 1), 1.0)
    que_tower(D, w * 0.315, base - h * 0.40, w * 0.15, h * 0.22, seed + 5)
    que_tower(D, w * 0.685, base - h * 0.40, w * 0.15, h * 0.22, seed + 9)
    D.path("M %s %s L %s %s A %s %s 0 0 1 %s %s L %s %s Z" %
           (P.f(w * 0.42), P.f(base), P.f(w * 0.42), P.f(base - h * 0.20), P.f(w * 0.08), P.f(h * 0.20),
            P.f(w * 0.58), P.f(base - h * 0.20), P.f(w * 0.58), P.f(base)), INK, 0.92)
    cloud_band(D, w * 0.02, w * 0.98, base - h * 0.06, h * 0.14, seed + 31, curls=2)
    return D


# ── ⑤ 大幅山脉（秦岭级构图）
def panorama_qh(w, h, seed, belts=4):
    D = Doc(w, h)
    r = random.Random(seed)
    D.poly([(0, 0), (w, 0), (w, h), (0, h)], P.PAPER, 1.0)
    # 天：极淡石青
    D.poly([(0, 0), (w, 0), (w, h * 0.48), (0, h * 0.44)],
           D.lin([(0, AZ_FAR, 0.30), (1, AZ_FAR, 0.02)], 0, 0, 0, 1), 1.0)
    specs = [
        (0.30, 12, 0.072, AZ_FAR, AZ_MID, 0.00),
        (0.40, 10, 0.098, AZ_MID, "#5E8794", 0.00),
        (0.52, 8, 0.135, GRN_MID, GRN_NEAR, 0.00),
        (0.64, 6, 0.190, OCHRE_HI, OCHRE_LO, 0.00),
    ]
    for li, (by, cnt, ufk, ct, cl, dy) in enumerate(specs[:belts]):
        uw = w * ufk
        for i in range(cnt):
            t = (i + r.uniform(0.15, 0.85)) / cnt
            cx = w * (0.02 + 0.96 * t)
            base_y = h * by + h * 0.045 * li
            uwi = uw * r.uniform(0.80, 1.25)
            hh = uwi * 0.74 * r.uniform(0.82, 1.20)
            peak_unit(D, cx, base_y, uwi, hh, seed + i * 29 + li * 211, ct, cl)
        if li in (1, 2):
            cloud_band(D, w * 0.01, w * 0.99, h * by + h * 0.03, h * 0.075, seed + 300 + li, curls=4)
    # 山脚：林带
    for k in range(int(w / 46)):
        x = w * r.uniform(0.01, 0.99)
        y = h * r.uniform(0.80, 0.97)
        s = h * r.uniform(0.045, 0.085)
        if k % 3:
            P.canopy(D, x, y - s * 0.5, s * 0.40, s * 0.5, seed + k * 13,
                     ((1.15, P.FOREST_LO, 0.95, 0.0), (0.85, P.FOREST_MID, 0.95, 0.05),
                      (0.52, P.FOREST_HI, 0.9, 0.12)))
        else:
            D.poly([(x - s * 0.10, y), (x, y - s * 0.95), (x + s * 0.10, y)], P.FOREST_LO, 0.95)
            D.poly([(x - s * 0.07, y), (x, y - s * 0.62), (x + s * 0.07, y)], P.FOREST_MID, 0.95)
    # 河
    for k, (sw, op) in enumerate(((max(2.0, h * 0.006), 0.85), (max(1.0, h * 0.003), 0.4))):
        D.path("M %s %s Q %s %s %s %s Q %s %s %s %s" %
               (P.f(w * 0.01), P.f(h * (0.93 + k * 0.012)), P.f(w * 0.28), P.f(h * (0.87 + k * 0.008)),
                P.f(w * 0.52), P.f(h * (0.92 + k * 0.010)), P.f(w * 0.74), P.f(h * (0.99 + k * 0.005)),
                P.f(w * 0.99), P.f(h * (0.90 + k * 0.012))),
               "none", op, stroke=P.WATER_LO if k == 0 else P.WATER_MID, sw=sw)
    cloud_band(D, w * 0.04, w * 0.96, h * 0.78, h * 0.07, seed + 777, curls=3)
    return D


# ── ⑥ 清单
DECOS = [
    ("QH_Mountain_Range_01", 900, 400, lambda w, h: el_range_qh(w, h, 4, 101)),
    ("QH_Mountain_Range_Diag_Up", 1000, 470, lambda w, h: el_range_qh(w, h, 4, 202, slope=-0.26)),
    ("QH_Mountain_Range_Diag_Down", 1000, 470, lambda w, h: el_range_qh(w, h, 4, 303, slope=0.26)),
    ("QH_Mountain_Range_Long_Diag", 1350, 450, lambda w, h: el_range_qh(w, h, 6, 404, slope=-0.20)),
    ("QH_Hills", 620, 250, lambda w, h: el_hills_qh(w, h, 505)),
    ("QH_Hills_Diag", 780, 320, lambda w, h: el_hills_qh(w, h, 606, slope=-0.24)),
    ("QH_Lake_Large", 620, 400, lambda w, h: P.el_lake(w, h, 707, 2)),
    ("QH_Lake_Medium", 400, 280, lambda w, h: P.el_lake(w, h, 808, 1)),
    ("QH_River", 900, 220, lambda w, h: P.el_river(w, h, 909)),
    ("QH_Forest_Pine", 420, 280, lambda w, h: P.el_forest(w, h, 111, "conifer")),
    ("QH_Forest_Broadleaf", 420, 280, lambda w, h: P.el_forest(w, h, 222, "broadleaf")),
    ("QH_Marsh", 460, 230, lambda w, h: P.el_marsh(w, h, 333)),
    ("QH_Grassland", 460, 200, lambda w, h: P.el_grassland(w, h, 444)),
    ("QH_Dunes", 500, 220, lambda w, h: P.el_dunes(w, h, 555)),
    ("QH_City", 330, 310, lambda w, h: el_city_qh(w, h, 666)),
    ("QH_Pass", 300, 270, lambda w, h: el_pass_qh(w, h, 777)),
    ("QH_Hamlet", 230, 200, lambda w, h: P.el_hamlet(w, h, 888)),
    ("QH_Farmland", 300, 230, lambda w, h: P.el_farmland(w, h, 999)),
]

PANORAMAS = [
    ("Range_Qinling_01", 2600, 1150, lambda w, h: panorama_qh(w, h, 121)),
    ("Range_Qinling_02", 2200, 980, lambda w, h: panorama_qh(w, h, 232)),
    ("Range_Panorama_Wide", 3200, 900, lambda w, h: panorama_qh(w, h, 343)),
]


def main():
    out = HERE / "_out6"
    out.mkdir(parents=True, exist_ok=True)
    for name, w, h, fn in DECOS + PANORAMAS:
        (out / (name + ".svg")).write_text(fn(w, h).svg(name), encoding="utf-8")
    print("秦汉风：装饰件 %d + 大幅山脉 %d" % (len(DECOS), len(PANORAMAS)))


if __name__ == "__main__":
    main()
