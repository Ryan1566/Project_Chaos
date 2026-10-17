# -*- coding: utf-8 -*-
"""升级用的迷你绘图内核（暂存态，不交付）：只保留重建图标总览与手绘风示例所需能力。"""
from __future__ import annotations

import random

INK = "#12100E"
PANEL = "#1E1A16"
PANEL_2 = "#2A241D"
LACQ = "#3B2C1F"
LACQ_2 = "#4E3A28"
BRONZE_D = "#2F4F45"
BRONZE = "#4E7A69"
BRONZE_L = "#86A894"
GOLD_D = "#7C6116"
GOLD = "#C9A227"
GOLD_L = "#EBCB6A"
RED_D = "#6E1C16"
RED = "#A32B22"
RED_L = "#D24433"
PAPER = "#D8C7A0"
PAPER_L = "#F0E6CC"
BONE = "#EDE7DA"
PURPLE = "#4A2B4E"
BLUE = "#2E5A87"
BLUE_D = "#22405F"
BLUE_L = "#6EA8D8"
GREEN = "#4C7A3F"
GREEN_D = "#2C4A2A"
GREY = "#8A8177"
WHITE = "#FFFFFF"
BLACK = "#000000"
FONT = "SimHei"


def n(v):
    if isinstance(v, float):
        s = "%.3f" % v
        return s.rstrip("0").rstrip(".")
    return str(v)


def a(**kw):
    out = []
    for k, v in kw.items():
        if v is None:
            continue
        out.append('%s="%s"' % (k.replace("_", "-"), n(v) if isinstance(v, float) else v))
    return (" " + " ".join(out)) if out else ""


def esc(s):
    return str(s).replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def rect(x, y, w, h, fill=None, stroke=None, sw=1, rx=0, op=None, dash=None):
    return "<rect%s/>" % a(x=x, y=y, width=w, height=h, fill=fill or "none", stroke=stroke,
                           stroke_width=None if stroke is None else sw, rx=rx or None, opacity=op,
                           stroke_dasharray=dash)


def line(x1, y1, x2, y2, stroke, sw=1, cap="butt", op=None, dash=None):
    return "<line%s/>" % a(x1=x1, y1=y1, x2=x2, y2=y2, stroke=stroke, stroke_width=sw,
                           stroke_linecap=cap, opacity=op, stroke_dasharray=dash)


def circle(cx, cy, r, fill=None, stroke=None, sw=1, op=None, dash=None):
    return "<circle%s/>" % a(cx=cx, cy=cy, r=r, fill=fill or "none", stroke=stroke,
                             stroke_width=None if stroke is None else sw, opacity=op,
                             stroke_dasharray=dash)


def poly(points, fill="none", stroke=None, sw=1, op=None):
    return "<polygon%s/>" % a(points=" ".join("%s,%s" % (n(round(p[0], 2)), n(round(p[1], 2)))
                                              for p in points),
                              fill=fill, stroke=stroke,
                              stroke_width=None if stroke is None else sw, opacity=op)


def polyline(points, stroke, sw=1, fill="none", cap="round", join="round", op=None):
    return "<polyline%s/>" % a(points=" ".join("%s,%s" % (n(round(p[0], 2)), n(round(p[1], 2)))
                                               for p in points),
                               fill=fill, stroke=stroke, stroke_width=sw, stroke_linecap=cap,
                               stroke_linejoin=join, opacity=op)


def text(x, y, s, size=14, fill=BONE, anchor="start", op=None, ls=None):
    return "<text%s>%s</text>" % (a(x=x, y=y, font_family=FONT, font_size=size, fill=fill,
                                    text_anchor=anchor, opacity=op, letter_spacing=ls), esc(s))


def vgrad(x, y, w, h, c0, c1, steps=14, op=1.0, rx=0):
    """垂直渐变的色带近似；rx>0 时两端用圆角收口，避免圆角控件露出直角。"""
    def mix(t):
        A = [int(c0[i:i + 2], 16) for i in (1, 3, 5)]
        B = [int(c1[i:i + 2], 16) for i in (1, 3, 5)]
        return "#%02X%02X%02X" % tuple(int(round(A[i] + (B[i] - A[i]) * t)) for i in range(3))
    if not rx:
        return [rect(x, y + h * i / steps, w, h / steps + 0.5,
                     fill=mix(i / max(1, steps - 1)), op=op) for i in range(steps)]
    cap = min(rx, h / 2)
    out = [rect(x, y, w, h, fill=c0, rx=rx, op=op),
           rect(x, y + h - cap, w, cap, fill=c1, rx=rx, op=op)]
    inner = max(0.0, h - 2 * cap)
    for i in range(steps):
        out.append(rect(x, y + cap + inner * i / steps, w, inner / steps + 0.5,
                        fill=mix(i / max(1, steps - 1)), op=op))
    return out


def hgrad(x, y, w, h, c0, c1, steps=14, op=1.0, rx=0):
    """水平渐变的色带近似；rx>0 时两端用圆角收口（条 / 滑条填充专用）。"""
    def mix(t):
        A = [int(c0[i:i + 2], 16) for i in (1, 3, 5)]
        B = [int(c1[i:i + 2], 16) for i in (1, 3, 5)]
        return "#%02X%02X%02X" % tuple(int(round(A[i] + (B[i] - A[i]) * t)) for i in range(3))
    if not rx:
        return [rect(x + w * i / steps, y, w / steps + 0.5, h,
                     fill=mix(i / max(1, steps - 1)), op=op) for i in range(steps)]
    cap = min(rx, w / 2)
    out = [rect(x, y, w, h, fill=c0, rx=rx, op=op),
           rect(x + w - cap, y, cap, h, fill=c1, rx=rx, op=op)]
    inner = max(0.0, w - 2 * cap)
    for i in range(steps):
        out.append(rect(x + cap + inner * i / steps, y, inner / steps + 0.5, h,
                        fill=mix(i / max(1, steps - 1)), op=op))
    return out


# ---------------------------------------------------------------- 手绘笔触
def ink_line(x1, y1, x2, y2, stroke=GOLD, sw=1.4, seed=1, amp=0.9, seg=5, op=None, dash=None):
    """手绘墨线：直线拆成轻微起伏的折线。"""
    r = random.Random(seed)
    pts = []
    for i in range(seg + 1):
        t = i / seg
        x = x1 + (x2 - x1) * t
        y = y1 + (y2 - y1) * t
        if 0 < i < seg:
            x += r.uniform(-amp, amp)
            y += r.uniform(-amp, amp)
        elif i in (0, seg):
            x += r.uniform(-amp * 0.35, amp * 0.35)
            y += r.uniform(-amp * 0.35, amp * 0.35)
        pts.append((x, y))
    out = polyline(pts, stroke=stroke, sw=sw, op=op)
    return out


def ink_rect(x, y, w, h, stroke=GOLD, sw=1.6, seed=1, amp=0.7, op=None):
    """手绘方框（四边独立的轻微起伏）。"""
    return [ink_line(x, y, x + w, y, stroke, sw, seed + 1, amp, op=op),
            ink_line(x + w, y, x + w, y + h, stroke, sw, seed + 2, amp, op=op),
            ink_line(x + w, y + h, x, y + h, stroke, sw, seed + 3, amp, op=op),
            ink_line(x, y + h, x, y, stroke, sw, seed + 4, amp, op=op)]


def taper(x1, y1, x2, y2, w1, w2, color=INK, op=1.0):
    """两端粗细不同的楔形笔触（模拟毛笔：起笔重、收笔轻）。"""
    import math
    dx, dy = x2 - x1, y2 - y1
    L = math.hypot(dx, dy) or 1
    nx, ny = -dy / L, dx / L
    return poly([(x1 + nx * w1 / 2, y1 + ny * w1 / 2), (x2 + nx * w2 / 2, y2 + ny * w2 / 2),
                 (x2 - nx * w2 / 2, y2 - ny * w2 / 2), (x1 - nx * w1 / 2, y1 - ny * w1 / 2)],
                fill=color, op=op)


def specks(w, h, n=90, seed=7, colors=(INK, GOLD_D, LACQ_2), rmax=3.2, op=0.09):
    """绢纹/纸纹：极低透明度的墨点散落。"""
    r = random.Random(seed)
    out = []
    for _ in range(n):
        x, y = r.uniform(0, w), r.uniform(0, h)
        rad = r.uniform(0.6, rmax)
        out.append(circle(x, y, rad, fill=r.choice(colors), op=r.uniform(op * 0.5, op)))
    return out


def seal(x, y, s, text_="混沌", color=RED, bg=RED_D):
    """朱文印：手绘风常用点缀。"""
    return [rect(x, y, s, s, fill=bg, rx=s * 0.08, op=0.92),
            ink_rect(x + 1, y + 1, s - 2, s - 2, RED_L, 2.2, seed=int(x + y), amp=s * 0.012),
            text(x + s / 2, y + s * 0.66, text_, size=s * 0.5, fill=PAPER_L, anchor="middle")]


class Doc:
    def __init__(self, w, h, bg=None):
        self.w, self.h = w, h
        self.items = []
        if bg:
            self.items.append(rect(0, 0, w, h, fill=bg))

    def add(self, *items):
        for it in items:
            if it is None:
                continue
            if isinstance(it, (list, tuple)):
                self.add(*it)
            else:
                self.items.append(it)
        return self

    def __iadd__(self, other):
        return self.add(other)

    append = add

    def render(self, title=None):
        head = ('<svg xmlns="http://www.w3.org/2000/svg" width="%s" height="%s" '
                'viewBox="0 0 %s %s">') % (n(self.w), n(self.h), n(self.w), n(self.h))
        t = ("<title>%s</title>" % esc(title)) if title else ""
        return head + t + "".join(self.items) + "</svg>\n"
