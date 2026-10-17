# -*- coding: utf-8 -*-
"""按最终切片清单生成交付说明文档（用占位替换，避开 % 格式化陷阱）。"""
import csv
from pathlib import Path

HERE = Path(__file__).resolve().parent
SLICE = HERE / "_slice5"
DOC = HERE / "03_地图装饰与叠图说明.md"

USE = {
    "Deco_Mountain_Range_01": "山脉·主脉（3 层剪影 + 空气透视 + 雪线）",
    "Deco_Mountain_Range_02": "山脉·支脉",
    "Deco_Mountain_Range_03": "山脉·连绵长条（6 峰）",
    "Deco_Mountain_Peak": "孤峰",
    "Deco_Hills": "丘陵（三层丘包）",
    "Deco_Lake_Large": "湖泊·大（含 2 岛，中心深、边缘浅）",
    "Deco_Lake_Medium": "湖泊·中（含 1 岛）",
    "Deco_River_Main": "河流（沙岸 + 水面反光）",
    "Deco_Forest_Conifer": "森林·针叶",
    "Deco_Forest_Broadleaf": "森林·阔叶",
    "Deco_Marsh": "沼泽 / 水泽",
    "Deco_Dunes": "沙丘（丘体 + 背风面阴影）",
    "Deco_Grassland": "草原",
    "Deco_City": "城池（城墙 + 垛口 + 角楼 + 屋顶）",
    "Deco_Pass": "关隘",
    "Deco_Hamlet": "村落",
    "Deco_Farmland": "农田（田块拼贴 + 垄沟）",
}

rows = list(csv.DictReader(open(SLICE / "_切片清单.csv", encoding="utf-8-sig")))
table = "\n".join("| `%s` | %s×%s | %s |" % (r["文件名"].replace(".png", ""), r["宽"], r["高"],
                                            USE.get(r["文件名"].replace(".png", ""), ""))
                  for r in rows)

TEMPLATE = """# 天下中原 · 地图装饰件说明（v2.0 写实版 · 切片）

> **风格基准**：`Map_tianxia_zhongyuan.png`（4096×3138，羊皮纸底）
> **画风**：写实地形插画——用多层剪影、渐变与色斑做体积/光影/材质，配色压在羊皮纸的暖褐色系内
> **导出形态**：**切片图**——每张透明底单图，边缘羽化，按 alpha 精确裁到内容外接框（+4px 透明边），可直接按 native size 摆到地图上

## 交付内容

| 位置 | 内容 |
| --- | --- |
| `SVG/18_地图装饰_天下中原/` | 17 个写实版矢量源（含渐变，须用浏览器/Edge 渲染） |
| `PNG/18_地图装饰_天下中原/` | 17 张切片 PNG + `_切片清单.csv` + `00_对照图.png` + `00_叠图示例.png` |
| `Assets/Art/PNG/GameWorld_Map/Deco/` | 同 17 张，已按 Sprite 导入（PPU 100 / FullRect / 关 Mipmap） |

## 切片清单（尺寸 = 裁剪后的实际像素）

| 文件 | 尺寸 | 用途 |
| --- | --- | --- |
@@TABLE@@

## 用法

1. **直接摆**：1:1 放到 4096×3138 的地图上；缩放建议 **0.85~1.2**（已按地图尺度出稿）。
2. **不透明度**：默认 93~100；压在纸色很深的四周暗角时，降到 **80~88** 更融。
3. **方向**：光源统一**左上**。不要水平翻转，旋转别超 ±15°。
4. **层次**：山/湖/河 → 林/田/沼泽 → 城/关/村（越重要越靠上）。
5. **Unity**：已裁紧，`SetNativeSize()` 就是地图上的实际像素尺寸；直接给 `SpriteRenderer` 或 UI `Image` 用。

## 重新导出

```bat
cd /d "C:\\Users\\26751\\Desktop\\游戏美术资源\\SVG\\svg2png"
python svg2png.py --only 18_地图装饰
```

写实版用了 `linearGradient` / `radialGradient`，**必须用浏览器内核渲染**（本工具的 Edge 后端）；
用 svglib 之类的基础渲染器会丢渐变。

## 已知取舍

| # | 取舍 | 说明 |
| --- | --- | --- |
| 1 | 半写实，不是照片级 | SVG 能做出体积、光影、色斑，但笔触颗粒 / 纸张肌理仍需美术在 PS 叠加 |
| 2 | 边缘羽化 2px | 为去掉"贴纸感"；要硬边 sprite 就把导出脚本的 `FEATHER` 设 0 重跑 |
| 3 | 山脚 / 丘陵底部淡出 | 让地形"长"进纸面；压在深色区域时建议降不透明度 |
| 4 | 峰形经过两轮调平 | 现为宽体山块，比针状更接近地形插画 |
| 5 | 暂缺 | 渡口、矿场、小湖（v1 墨线版有）；需要就按同一套画法补 |
"""

DOC.write_text(TEMPLATE.replace("@@TABLE@@", table), encoding="utf-8")
print("文档已生成", DOC)
