# -*- coding: utf-8 -*-
"""天下中原地图装饰件（写实版）：有体积、光影与材质的"手绘地形"贴图。

与上一版（墨线符号）的区别：
  · 用多层剪影 + 渐变 + 手绘色斑做体积与材质，不再只靠线条；
  · 色彩取自地图的暖褐体系，但提高饱和与明暗层次，接近"羊皮纸上的水粉地形画"；
  · 每个元素画布按内容裁紧（切片），导出后还会再按 alpha 精确 trim。

渲染：Edge（支持 linearGradient / radialGradient）。SVG 源仍自包含。
"""
import math
import random
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mini as M  # noqa: E402

# ── 地图取样色（纸）+ 写实地形色（去饱和、暖褐系，压得住羊皮纸）
PAPER = "#E7DDB4"
ROCK_HI = "#C9BBA2"
ROCK_MID = "#A8957C"
ROCK_LO = "#7D6B57"
ROCK_DK = "#5E4F3E"
SNOW = "#F2EBD8"
FOREST_HI = "#8C9A6E"
FOREST_MID = "#61704E"
FOREST_LO = "#465239"
WATER_HI = "#A9B7A8"
WATER_MID = "#7E9189"
WATER_LO = "#5C6F6B"
SAND_HI = "#D8C79E"
SAND_MID = "#C0AC7E"
SAND_LO = "#9C8A62"
FIELD_A = "#B3A171"
FIELD_B = "#A08E5F"
FIELD_C = "#C1AF7C"
ROOF_HI = "#8E5B41"
ROOF_LO = "#5E3A2A"
WALL_HI = "#C4B394"
WALL_LO = "#8D7C62"
SHADOW = "#4A3B2A"


# ────────────────────────────────────────────── 基础
def f(v):
    return M.n(round(v, 2))


class Doc:
    """带 defs 的写实 SVG 文档。"""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.defs = []
        self.body = []
        self._n = 0

    def uid(self, p="g"):
        self._n += 1
        return "%s%d" % (p, self._n)

    def lin(self, stops, x1=0, y1=0, x2=0, y2=1):
        i = self.uid("lg")
        s = "".join('<stop offset="%s" stop-color="%s"%s/>' %
                    (f(o), c, (' stop-opacity="%s"' % f(a)) if a is not None else "")
                    for o, c, a in stops)
        self.defs.append('<linearGradient id="%s" x1="%s" y1="%s" x2="%s" y2="%s" '
                         'gradientUnits="objectBoundingBox">%s</linearGradient>'
                         % (i, x1, y1, x2, y2, s))
        return "url(#%s)" % i

    def rad(self, stops, cx=0.5, cy=0.5, r=0.6):
        i = self.uid("rg")
        s = "".join('<stop offset="%s" stop-color="%s"%s/>' %
                    (f(o), c, (' stop-opacity="%s"' % f(a)) if a is not None else "")
                    for o, c, a in stops)
        self.defs.append('<radialGradient id="%s" cx="%s" cy="%s" r="%s" '
                         'gradientUnits="objectBoundingBox">%s</radialGradient>' % (i, cx, cy, r, s))
        return "url(#%s)" % i

    def poly(self, pts, fill, op=1.0, stroke=None, sw=1.0):
        p = " ".join("%s,%s" % (f(x), f(y)) for x, y in pts)
        extra = (' stroke="%s" stroke-width="%s" stroke-linejoin="round"' % (stroke, f(sw))) if stroke else ""
        self.body.append('<polygon points="%s" fill="%s" opacity="%s"%s/>' % (p, fill, f(op), extra))

    def ell(self, cx, cy, rx, ry, fill, op=1.0, rot=None):
        tr = (' transform="rotate(%s %s %s)"' % (f(rot), f(cx), f(cy))) if rot else ""
        self.body.append('<ellipse cx="%s" cy="%s" rx="%s" ry="%s" fill="%s" opacity="%s"%s/>'
                         % (f(cx), f(cy), f(rx), f(ry), fill, f(op), tr))

    def path(self, d, fill, op=1.0, stroke=None, sw=1.0):
        extra = (' stroke="%s" stroke-width="%s" stroke-linecap="round" stroke-linejoin="round"'
                 % (stroke, f(sw))) if stroke else ""
        self.body.append('<path d="%s" fill="%s" opacity="%s"%s/>' % (d, fill, f(op), extra))

    def svg(self, title=""):
        d = ("<defs>%s</defs>" % "".join(self.defs)) if self.defs else ""
        return ('<svg xmlns="http://www.w3.org/2000/svg" width="%s" height="%s" '
                'viewBox="0 0 %s %s"><title>%s</title>%s%s</svg>\n'
                % (f(self.w), f(self.h), f(self.w), f(self.h), title, d, "".join(self.body)))


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


def blob_pts(cx, cy, rx, ry, seed, n=20, rough=0.10, lobes=3, amp=0.08):
    r = random.Random(seed)
    ph = r.uniform(0, math.tau)
    out = []
    for i in range(n):
        a = math.tau * i / n
        k = 1 + (r.random() * 2 - 1) * rough + amp * math.sin(lobes * a + ph)
        out.append((cx + math.cos(a) * rx * k, cy + math.sin(a) * ry * k))
    return out


def dabs(D, pts_area, count, color, rmin, rmax, op_min, op_max, seed, squash=0.55):
    """手绘色斑（笔触颗粒），用于岩石/树冠/水面/沙地的材质。"""
    r = random.Random(seed)
    for _ in range(count):
        x, y = pts_area(r)
        rx = r.uniform(rmin, rmax)
        D.ell(x, y, rx, rx * squash, color, r.uniform(op_min, op_max), rot=r.uniform(0, 180))


# ────────────────────────────────────────────── 山
def ridge_profile(w, base, h, peaks, seed, sharp=0.55):
    """生成一条山脊剪影（峰值点之间用控制点连接，形成自然山谷）。"""
    r = random.Random(seed)
    pts = [(w * 0.02, base)]
    n = peaks
    for i in range(n):
        t = (i + 0.5) / n
        px = w * (0.06 + 0.88 * t) + r.uniform(-w * 0.02, w * 0.02)
        ph = h * (0.42 + 0.34 * r.random()) * (0.78 + 0.22 * math.sin(math.pi * t))
        py = base - ph
        # 峰两侧的肩部（用 shoulder 点让轮廓不那么尖）
        pts.append((px - w * 0.06, base - ph * 0.42))
        pts.append((px + r.uniform(-w * 0.02, w * 0.02), py))
        pts.append((px + w * 0.07, base - ph * sharp * 0.55))
    pts.append((w * 0.98, base))
    return pts


def ridge(D, pts, top, bottom, op=1.0, glow=None):
    d = smooth(pts, closed=False, tension=0.55)
    d = d + " L %s %s L %s %s Z" % (f(pts[-1][0]), f(max(p[1] for p in pts) + 200),
                                    f(pts[0][0]), f(max(p[1] for p in pts) + 200))
    fill = D.lin([(0, top, None), (1, bottom, None)], 0, 0, 0, 1)
    D.path(d, fill, op)
    if glow:
        D.path(smooth(pts, closed=False, tension=0.55), "none", glow[1])  # 预留


def mountain_layer(D, w, h, base, peaks, top, bottom, seed, sharp=0.55, texture=True,
                   tex_color=ROCK_DK, snow=0):
    pts = ridge_profile(w, base, h, peaks, seed, sharp)
    d = smooth(pts, closed=False, tension=0.55)
    d2 = d + " L %s %s L %s %s Z" % (f(w), f(base + h * 0.02), f(0), f(base + h * 0.02))
    D.path(d2, D.lin([(0, top, None), (0.30, bottom, None), (0.72, "#B9AB90", 0.75),
                      (1, "#CBBFA8", 0.0)], 0, 0, 0, 1), 1.0)
    # 右坡阴影：沿峰顶向下的一层深色
    r = random.Random(seed + 7)
    for i in range(peaks):
        t = (i + 0.5) / peaks
        px = w * (0.06 + 0.88 * t)
        peak_y = base - h * 0.8
        D.poly([(px, peak_y), (px + w * 0.11, base), (px + w * 0.02, base)],
               D.lin([(0, ROCK_DK, 0.20), (1, ROCK_DK, 0.02)]), 0.8)
    # 受光棱线
    D.path(smooth(pts, closed=False, tension=0.55), "none")
    # 材质色斑
    if texture:
        def area(rr):
            t = rr.uniform(0.05, 0.95)
            y = rr.uniform(base - h * 0.7, base - 2)
            return (w * t, y)
        dabs(D, area, int(w * h / 2600), tex_color, 1.6, 5.5, 0.05, 0.16, seed + 3)
    # 雪线（窄受光带，不用椭圆贴片）
    if snow:
        for i in range(snow):
            t = 0.28 + 0.44 * i / max(1, snow - 1)
            px = w * (0.06 + 0.88 * t) + r.uniform(-w * 0.02, w * 0.02)
            py = base - h * (0.66 + 0.14 * r.random())
            D.path("M %s %s Q %s %s %s %s L %s %s Z" %
                   (f(px - w * 0.035), f(py + h * 0.05), f(px), f(py - h * 0.03),
                    f(px + w * 0.035), f(py + h * 0.05), f(px), f(py + h * 0.075)),
                   SNOW, 0.45)
    return pts


def el_mountain_range(w, h, peaks, seed):
    D = Doc(w, h)
    base = h * 0.96
    # 远景（空气透视：更浅、更偏纸色）
    mountain_layer(D, w, h, base - h * 0.10, max(2, peaks - 1), "#CBBFA8", "#B3A58C",
                   seed + 11, 0.5, True, ROCK_MID, 0)
    # 中景
    mountain_layer(D, w, h, base - h * 0.02, peaks, "#B0A089", "#8B7A64", seed + 23, 0.55,
                   True, ROCK_DK, 1 if peaks >= 4 else 0)
    # 近景（最强对比）
    mountain_layer(D, w, h, base + h * 0.02, max(2, peaks - 1), "#9C8B74", "#6B5A47",
                   seed + 37, 0.6, True, ROCK_DK, 1)
    return D


def el_mountain_peak(w, h, seed):
    D = Doc(w, h)
    base = h * 0.96
    mountain_layer(D, w, h, base - h * 0.06, 2, "#C2B49C", "#A5947B", seed + 5, 0.5, True, ROCK_MID, 0)
    mountain_layer(D, w, h, base, 1, "#A08E76", "#6B5A47", seed + 9, 0.62, True, ROCK_DK, 1)
    return D


def el_hills(w, h, seed):
    """丘陵：3 层柔和丘包（远浅近深），顶部受光、谷底微影。"""
    D = Doc(w, h)
    base = h * 0.92
    r = random.Random(seed)
    layers = ((5, h * 0.34, "#C6BA9F", "#AE9F85", 0.92),
              (4, h * 0.44, "#B5A68B", "#938268", 0.95),
              (3, h * 0.54, "#A2917A", "#7A6A55", 0.96))
    for li, (n, rh, col_a, col_b, op) in enumerate(layers):
        for i in range(n):
            cx = w * (0.08 + 0.84 * (i + r.uniform(0.15, 0.85)) / n)
            rw = w * r.uniform(0.17, 0.27)
            hh = rh * r.uniform(0.85, 1.1)
            pts = blob_pts(cx, base - hh * 0.42, rw, hh * 0.80,
                           seed + i * 7 + li * 31, 16, 0.07, 2, 0.06)
            D.poly(pts, D.lin([(0, col_a, None), (1, col_b, None)], 0, 0, 0, 1), op)
            # 顶部受光
            D.poly(blob_pts(cx - rw * 0.18, base - hh * 0.92, rw * 0.52, hh * 0.18,
                            seed + i * 5 + li * 11, 12, 0.12, 2, 0.08), "#DCD2B7", 0.28)
            # 谷底阴影
            D.ell(cx + rw * 0.30, base - hh * 0.12, rw * 0.52, hh * 0.13, SHADOW, 0.10)
    return D


# ────────────────────────────────────────────── 水
def el_lake(w, h, seed, islands=1):
    D = Doc(w, h)
    cx, cy = w / 2, h / 2
    r = random.Random(seed)
    shore = blob_pts(cx, cy, w * 0.45, h * 0.41, seed, 24, 0.10, 3, 0.06)
    # 外圈浅滩（沙岸）
    for k, (sc, col, op) in enumerate(((1.06, SAND_MID, 0.45), (1.02, SAND_HI, 0.6))):
        pts = blob_pts(cx, cy, w * 0.45 * sc, h * 0.41 * sc, seed + 1 + k, 24, 0.10, 3, 0.06)
        D.poly(pts, col, op)
    # 水体：中心深、边缘浅
    water = D.rad([(0, WATER_LO, None), (0.62, WATER_MID, None), (1, WATER_HI, None)], 0.5, 0.52, 0.62)
    D.poly(shore, water, 1.0)
    # 水面高光与波纹
    for k in range(9):
        yy = cy + h * (0.26 - 0.07 * (k % 4)) + r.uniform(-3, 3)
        xx = cx + r.uniform(-w * 0.26, w * 0.20)
        D.ell(xx, yy, w * r.uniform(0.03, 0.09), h * 0.010, WATER_HI, r.uniform(0.18, 0.34))
    # 岛
    for k in range(islands):
        ix = cx + (k - (islands - 1) / 2) * w * 0.26 + r.uniform(-6, 6)
        iy = cy + r.uniform(-h * 0.08, h * 0.08)
        isl = blob_pts(ix, iy, w * 0.085, h * 0.065, seed + 20 + k, 14, 0.14, 2, 0.10)
        D.poly(blob_pts(ix, iy, w * 0.095, h * 0.072, seed + 30 + k, 14, 0.14, 2, 0.10), SAND_HI, 0.55)
        D.poly(isl, D.rad([(0, "#7E8C63", None), (1, FOREST_MID, None)], 0.4, 0.35, 0.7), 0.95)
        D.ell(ix - w * 0.02, iy - h * 0.015, w * 0.03, h * 0.014, "#A7B483", 0.35, rot=-15)
    return D


def el_river(w, h, seed):
    D = Doc(w, h)
    r = random.Random(seed)
    n = 9
    top, bot = [], []
    for i in range(n):
        t = i / (n - 1)
        x = w * (0.02 + 0.96 * t)
        y = h * 0.52 + math.sin(t * math.pi * 1.7 + 0.5) * h * 0.10
        hw = h * 0.16 * (0.35 + 0.65 * math.sin(t * math.pi) ** 0.55)
        top.append((x, y - hw))
        bot.append((x, y + hw))
    # 沙岸
    d_bank = smooth(top, closed=False) + " " + " ".join("L %s %s" % (f(p[0]), f(p[1])) for p in reversed(bot)) + " Z"
    D.path(d_bank, "#CBB88C", 0.55)
    # 水面（略窄）
    top2 = [(x, y + h * 0.018) for x, y in top]
    bot2 = [(x, y - h * 0.020) for x, y in bot]
    d_w = smooth(top2, closed=False) + " " + " ".join("L %s %s" % (f(p[0]), f(p[1])) for p in reversed(bot2)) + " Z"
    D.path(d_w, D.lin([(0, WATER_HI, None), (0.5, WATER_MID, None), (1, WATER_LO, None)], 0, 0, 0, 1), 1.0)
    # 水面反光
    for k in range(10):
        t = 0.08 + 0.84 * k / 9
        x = w * t
        y = h * 0.52 + math.sin(t * math.pi * 1.7 + 0.5) * h * 0.10
        D.ell(x, y, w * r.uniform(0.012, 0.028), h * 0.006, "#DCE4D2", r.uniform(0.2, 0.4))
    return D


# ────────────────────────────────────────────── 植被 / 地貌
def canopy(D, x, y, r, hgt, seed, cols):
    """一丛树冠：暗底 + 亮部 + 两三笔高光。"""
    rnd = random.Random(seed)
    for k, (sc, col, op, dy) in enumerate(cols):
        pts = blob_pts(x + rnd.uniform(-r * 0.2, r * 0.2), y - hgt * dy,
                       r * sc, r * sc * 0.82, seed + k * 7, 12, 0.12, 2, 0.10)
        D.poly(pts, col, op)


def el_forest(w, h, seed, kind="conifer"):
    D = Doc(w, h)
    r = random.Random(seed)
    base = h * 0.92
    # 地面阴影
    D.ell(w * 0.5, base + h * 0.02, w * 0.42, h * 0.06, SHADOW, 0.16)
    rows = 3
    for j in range(rows):
        cnt = 6 - j
        for i in range(cnt):
            x = w * (0.08 + 0.84 * (i + r.uniform(0.1, 0.9)) / cnt)
            y = base - h * 0.06 * j + r.uniform(-h * 0.01, h * 0.01)
            s = h * r.uniform(0.30, 0.40) * (1 - 0.10 * j)
            if kind == "conifer":
                # 针叶：锥形三层
                for k, (sc, col) in enumerate(((1.0, FOREST_LO), (0.78, FOREST_MID), (0.52, FOREST_HI))):
                    D.poly([(x - s * 0.55 * sc, y), (x, y - s * 0.95 * sc), (x + s * 0.55 * sc, y)],
                           col, 0.95)
                D.ell(x + s * 0.12, y - s * 0.42, s * 0.18, s * 0.26, "#9FAE7E", 0.35, rot=-12)
                D.poly([(x - s * 0.06, y), (x + s * 0.06, y), (x + s * 0.05, y + s * 0.08),
                        (x - s * 0.05, y + s * 0.08)], "#5A4632", 0.9)
            else:
                canopy(D, x, y - s * 0.55, s * 0.42, s * 0.5, seed + i * 13 + j * 31,
                       ((1.15, FOREST_LO, 0.95, 0.0), (0.86, FOREST_MID, 0.95, 0.06),
                        (0.52, FOREST_HI, 0.9, 0.12)))
                D.poly([(x - s * 0.05, y - s * 0.10), (x + s * 0.05, y - s * 0.10),
                        (x + s * 0.04, y + s * 0.06), (x - s * 0.04, y + s * 0.06)],
                       "#5A4632", 0.85)
    return D


def el_marsh(w, h, seed):
    D = Doc(w, h)
    r = random.Random(seed)
    for k in range(3):
        cx = w * (0.22 + 0.28 * k) + r.uniform(-8, 8)
        cy = h * (0.58 + r.uniform(-0.05, 0.05))
        pts = blob_pts(cx, cy, w * 0.11, h * 0.10, seed + k, 14, 0.12, 2, 0.10)
        D.poly(pts, D.rad([(0, WATER_LO, None), (1, WATER_HI, None)], 0.5, 0.45, 0.7), 0.9)
        D.poly(blob_pts(cx, cy + h * 0.02, w * 0.12, h * 0.035, seed + k + 9, 12, 0.12, 2, 0.1),
               "#8FA07A", 0.35)
    # 芦苇
    for k in range(26):
        x = w * r.uniform(0.06, 0.94)
        y = h * r.uniform(0.74, 0.95)
        gh = h * r.uniform(0.10, 0.20)
        D.path("M %s %s Q %s %s %s %s" % (f(x), f(y), f(x + r.uniform(-3, 3)), f(y - gh * 0.6),
                                          f(x + r.uniform(-6, 6)), f(y - gh)),
               "#6C7A4E", 0.85, stroke=None)
        D.ell(x + 1, y - gh * 0.92, 1.6, 4.2, "#8B9A62", 0.8, rot=r.uniform(-8, 8))
    return D


def el_dunes(w, h, seed):
    D = Doc(w, h)
    r = random.Random(seed)
    for k in range(5):
        y = h * (0.24 + 0.15 * k)
        x0, x1 = w * (0.03 + 0.02 * (k % 2)), w * (0.95 - 0.02 * (k % 3))
        mid = (x0 + x1) / 2
        cy = y - h * 0.16
        d = "M %s %s Q %s %s %s %s" % (f(x0), f(y), f(mid), f(cy), f(x1), f(y))
        # 丘体：向下补一段体积（越靠下越高），形成沙丘堆叠
        d2 = ("%s L %s %s L %s %s L %s %s Z" %
              (d, f(x1), f(y + h * 0.16), f(mid), f(cy + h * 0.30), f(x0), f(y + h * 0.16)))
        D.path(d2, D.lin([(0, SAND_HI, None), (1, SAND_LO, None)], 0, 0, 0, 1), 1.0)
        # 背风面阴影
        D.path("M %s %s Q %s %s %s %s L %s %s Z" %
               (f(mid), f(y - h * 0.17), f(mid + w * 0.10), f(y - h * 0.04), f(x1), f(y),
                f(x1), f(y + h * 0.05)),
               D.lin([(0, SAND_LO, 0.35), (1, SAND_LO, 0.05)]), 0.9)
        # 沙纹
        for i in range(4):
            xx = x0 + (x1 - x0) * (0.2 + 0.18 * i)
            D.path("M %s %s Q %s %s %s %s" % (f(xx), f(y - h * 0.02), f(xx + w * 0.05), f(y - h * 0.05),
                                              f(xx + w * 0.11), f(y - h * 0.02)),
                   "none", 1.0, stroke="#B9A57C", sw=1.4)
    return D


def el_grassland(w, h, seed):
    D = Doc(w, h)
    r = random.Random(seed)
    # 色块
    for k in range(5):
        cx = w * r.uniform(0.15, 0.85)
        cy = h * r.uniform(0.45, 0.85)
        pts = blob_pts(cx, cy, w * r.uniform(0.12, 0.22), h * r.uniform(0.10, 0.18),
                       seed + k, 14, 0.15, 2, 0.1)
        D.poly(pts, ["#B0AE7C", "#98A06A", "#BDB680"][k % 3], 0.62)
    for k in range(70):
        x = w * r.uniform(0.03, 0.97)
        y = h * r.uniform(0.40, 0.96)
        gh = h * r.uniform(0.05, 0.11)
        D.path("M %s %s Q %s %s %s %s" % (f(x), f(y), f(x + r.uniform(-2, 2)), f(y - gh * 0.6),
                                          f(x + r.uniform(-4, 4)), f(y - gh)),
               ["#8E9560", "#7C8552", "#9AA46C"][k % 3], r.uniform(0.45, 0.8))
    return D


# ────────────────────────────────────────────── 聚落 / 设施
def house(D, x, y, w, h, seed, roof_hi=ROOF_HI, roof_lo=ROOF_LO):
    """一座小屋（带屋顶受光/背光与地面阴影）。"""
    r = random.Random(seed)
    D.ell(x + w * 0.15, y + h * 0.05, w * 0.62, h * 0.10, SHADOW, 0.18)
    # 墙体
    D.poly([(x - w * 0.42, y), (x + w * 0.42, y), (x + w * 0.40, y + h * 0.30),
            (x - w * 0.40, y + h * 0.30)], D.lin([(0, WALL_HI, None), (1, WALL_LO, None)],
                                                0, 0, 0, 1), 1.0)
    # 屋顶（两坡）
    D.poly([(x - w * 0.54, y), (x, y - h * 0.52), (x + w * 0.54, y)],
           D.lin([(0, roof_hi, None), (1, roof_lo, None)], 0, 0, 0, 1), 1.0)
    D.poly([(x, y - h * 0.52), (x + w * 0.54, y), (x + w * 0.20, y)],
           D.lin([(0, roof_lo, 0.55), (1, roof_lo, 0.1)]), 0.9)
    # 窗
    D.ell(x - w * 0.16, y + h * 0.15, w * 0.05, h * 0.05, "#6B5335", 0.7)
    D.ell(x + w * 0.12, y + h * 0.15, w * 0.05, h * 0.05, "#6B5335", 0.7)


def el_city(w, h, seed):
    D = Doc(w, h)
    r = random.Random(seed)
    x0, y0, x1, y1 = w * 0.16, h * 0.34, w * 0.84, h * 0.82
    # 地面阴影
    D.ell(w * 0.5, y1 + h * 0.05, w * 0.40, h * 0.06, SHADOW, 0.18)
    # 城墙
    D.poly([(x0, y0), (x1, y0), (x1, y1), (x0, y1)],
           D.lin([(0, WALL_HI, None), (1, WALL_LO, None)], 0, 0, 0, 1), 1.0)
    D.poly([(x0, y0), (x1, y0), (x1, y0 + h * 0.06), (x0, y0 + h * 0.06)], "#D6C7A8", 0.7)
    # 垛口
    for i in range(8):
        cx = x0 + (x1 - x0) * i / 8
        D.poly([(cx, y0 - h * 0.045), (cx + w * 0.035, y0 - h * 0.045),
                (cx + w * 0.035, y0), (cx, y0)], WALL_HI, 0.95)
    # 角楼
    for (cx, cy) in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
        D.poly([(cx - w * 0.045, cy - h * 0.045), (cx + w * 0.045, cy - h * 0.045),
                (cx + w * 0.045, cy + h * 0.045), (cx - w * 0.045, cy + h * 0.045)],
               D.lin([(0, WALL_HI, None), (1, WALL_LO, None)], 0, 0, 0, 1), 1.0)
    # 城内屋群
    for j in range(3):
        for i in range(3):
            hx = w * (0.30 + 0.20 * i)
            hy = y1 - h * (0.10 + 0.18 * j)
            house(D, hx, hy, w * 0.13, h * 0.15, seed + i * 7 + j * 13)
    # 城门
    D.path("M %s %s L %s %s A %s %s 0 0 1 %s %s L %s %s Z" %
           (f(w * 0.42), f(y1), f(w * 0.42), f(y1 - h * 0.16), f(w * 0.08), f(h * 0.16),
            f(w * 0.58), f(y1 - h * 0.16), f(w * 0.58), f(y1)), "#4E3A28", 0.95)
    return D


def el_pass(w, h, seed):
    D = Doc(w, h)
    base = h * 0.86
    D.ell(w * 0.5, base + h * 0.04, w * 0.42, h * 0.05, SHADOW, 0.16)
    # 两侧山体
    mountain_layer(D, w, h, base, 2, "#B3A48A", "#7D6B57", seed, 0.6, True, ROCK_DK, 0)
    # 城墙 + 门洞
    D.poly([(w * 0.30, base - h * 0.42), (w * 0.70, base - h * 0.42),
            (w * 0.70, base), (w * 0.30, base)],
           D.lin([(0, WALL_HI, None), (1, WALL_LO, None)], 0, 0, 0, 1), 1.0)
    for i in range(4):
        cx = w * 0.30 + w * 0.10 * i
        D.poly([(cx, base - h * 0.46), (cx + w * 0.06, base - h * 0.46),
                (cx + w * 0.06, base - h * 0.42), (cx, base - h * 0.42)], WALL_HI, 0.95)
    D.path("M %s %s L %s %s A %s %s 0 0 1 %s %s L %s %s Z" %
           (f(w * 0.42), f(base), f(w * 0.42), f(base - h * 0.20), f(w * 0.08), f(h * 0.20),
            f(w * 0.58), f(base - h * 0.20), f(w * 0.58), f(base)), "#4E3A28", 0.95)
    return D


def el_hamlet(w, h, seed):
    D = Doc(w, h)
    base = h * 0.86
    for k, (rx, ry, s) in enumerate(((0.30, 0.20, 0.22), (0.62, 0.28, 0.24), (0.46, 0.48, 0.19))):
        house(D, w * rx, base - h * ry, w * s, h * s * 0.9, seed + k * 11)
    # 树
    canopy(D, w * 0.84, base - h * 0.10, w * 0.09, h * 0.10, seed + 40,
           ((1.15, FOREST_LO, 0.95, 0.0), (0.85, FOREST_MID, 0.95, 0.05), (0.5, FOREST_HI, 0.9, 0.1)))
    D.poly([(w * 0.84 - 3, base - h * 0.06), (w * 0.84 + 3, base - h * 0.06),
            (w * 0.84 + 3, base), (w * 0.84 - 3, base)], "#5A4632", 0.9)
    return D


def el_farmland(w, h, seed):
    D = Doc(w, h)
    r = random.Random(seed)
    # 田块拼贴
    pts = [(w * 0.08, h * 0.20), (w * 0.92, h * 0.14), (w * 0.88, h * 0.86), (w * 0.12, h * 0.80)]
    D.poly(pts, FIELD_A, 0.85)
    cols = [FIELD_B, FIELD_C, "#AD9C6B", "#B7A574"]
    for i in range(7):
        t0 = i / 7
        t1 = (i + 1) / 7
        q = [(w * (0.10 + 0.78 * t0), h * (0.20 + 0.60 * t0)),
             (w * (0.10 + 0.78 * t1), h * (0.20 + 0.60 * t1)),
             (w * (0.10 + 0.78 * t1), h * (0.26 + 0.54 * t1)),
             (w * (0.10 + 0.78 * t0), h * (0.26 + 0.54 * t0))]
        D.poly(q, cols[i % len(cols)], 0.55)
    # 垄沟
    for i in range(16):
        t = (i + 0.5) / 16
        D.path("M %s %s L %s %s" % (f(w * (0.11 + 0.76 * t)), f(h * 0.20),
                                    f(w * (0.11 + 0.76 * t)), f(h * 0.80)), "none", 1.0,
               stroke="#8D7C52", sw=1.2)
    D.poly([(x + 2, y + 3) for x, y in pts], "#6E5F42", 0.12)
    D.poly(pts, "none", 1.0, stroke="#8D7C52", sw=2.0)
    return D


# ────────────────────────────────────────────── 清单（画布按内容裁紧）
DECOS = [
    ("Deco_Mountain_Range_01", 900, 380, lambda w, h: el_mountain_range(w, h, 4, 101)),
    ("Deco_Mountain_Range_02", 620, 320, lambda w, h: el_mountain_range(w, h, 3, 202)),
    ("Deco_Mountain_Range_03", 1180, 360, lambda w, h: el_mountain_range(w, h, 6, 303)),
    ("Deco_Mountain_Peak", 300, 260, lambda w, h: el_mountain_peak(w, h, 404)),
    ("Deco_Hills", 560, 210, lambda w, h: el_hills(w, h, 505)),
    ("Deco_Lake_Large", 620, 400, lambda w, h: el_lake(w, h, 606, 2)),
    ("Deco_Lake_Medium", 400, 280, lambda w, h: el_lake(w, h, 707, 1)),
    ("Deco_River_Main", 900, 220, lambda w, h: el_river(w, h, 808)),
    ("Deco_Forest_Conifer", 420, 270, lambda w, h: el_forest(w, h, 909, "conifer")),
    ("Deco_Forest_Broadleaf", 420, 270, lambda w, h: el_forest(w, h, 111, "broadleaf")),
    ("Deco_Marsh", 460, 230, lambda w, h: el_marsh(w, h, 222)),
    ("Deco_Dunes", 520, 220, lambda w, h: el_dunes(w, h, 333)),
    ("Deco_Grassland", 460, 190, lambda w, h: el_grassland(w, h, 444)),
    ("Deco_City", 300, 280, lambda w, h: el_city(w, h, 555)),
    ("Deco_Pass", 260, 240, lambda w, h: el_pass(w, h, 666)),
    ("Deco_Hamlet", 220, 200, lambda w, h: el_hamlet(w, h, 777)),
    ("Deco_Farmland", 300, 230, lambda w, h: el_farmland(w, h, 888)),
]


def main():
    out = HERE / "_out5"
    out.mkdir(parents=True, exist_ok=True)
    for name, w, h, fn in DECOS:
        D = fn(w, h)
        (out / (name + ".svg")).write_text(D.svg(name), encoding="utf-8")
    print("生成写实版装饰件", len(DECOS), "个")


if __name__ == "__main__":
    main()
