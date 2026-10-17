# -*- coding: utf-8 -*-
"""生成秦汉风套装说明文档（尺寸取自导出清单）。"""
import csv
from pathlib import Path

HERE = Path(__file__).resolve().parent
SLICE = HERE / "_slice6"
DOC = HERE / "04_秦汉风地图装饰与山脉说明.md"

USE = {
    "QH_Mountain_Range_01": "山脉·横向（石青/石绿/赭石三排成串 + 云气）",
    "QH_Mountain_Range_Diag_Up": "山脉·斜向（左低右高，峰体竖直）",
    "QH_Mountain_Range_Diag_Down": "山脉·斜向（左高右低）",
    "QH_Mountain_Range_Long_Diag": "山脉·斜向长条（6 峰，适合沿山带铺）",
    "QH_Hills": "丘陵（三排丘包 + 卷云）",
    "QH_Hills_Diag": "丘陵·斜向",
    "QH_Lake_Large": "湖泊·大（石青水面 + 水纹 + 2 岛）",
    "QH_Lake_Medium": "湖泊·中（1 岛）",
    "QH_River": "河流（沙岸 + 石青水 + 云气）",
    "QH_Forest_Pine": "松林（三层塔形冠，墨绿）",
    "QH_Forest_Broadleaf": "阔叶林",
    "QH_Marsh": "沼泽 / 水泽",
    "QH_Grassland": "草原",
    "QH_Dunes": "沙丘 / 荒漠",
    "QH_City": "城池（夯土墙 + 双阙楼 + 黑瓦朱墙）",
    "QH_Pass": "关隘（关城 + 双阙）",
    "QH_Hamlet": "村落（夯土民舍 + 黑瓦）",
    "QH_Farmland": "农田（阡陌）",
    "Range_Qinling_01": "大幅山脉（4 层山带 + 云气 + 林带 + 河）",
    "Range_Qinling_02": "大幅山脉·变体（层更紧、谷更窄）",
    "Range_Panorama_Wide": "大幅山脉·超宽（3200 宽，适合横向铺满）",
}

rows = list(csv.DictReader(open(SLICE / "_切片清单.csv", encoding="utf-8-sig")))
lines = []
for r in rows:
    n = r["文件名"].replace(".png", "")
    lines.append("| `%s` | %s×%s | %s |" % (n, r["宽"], r["高"], USE.get(n, "")))
table = "\n".join(lines)

TPL = """# 天下中原 · 秦汉风地图装饰与大幅山脉说明（v3.0）

> **世界观取色**：秦汉漆器 / 汉画像石 / 早期青绿山水
> 墨 `#2A2420` · 朱砂 `#B03A2C` · 赭石 `#8A5A3A` · 石青 `#3E6E7A` · 石绿 `#6B8A5A` · 金 `#C9A227` · 纸 `#E7DDB4`
>
> **一、装饰件**：透明底切片（羽化 2px + 按 alpha 裁紧），叠在 `Map_tianxia_zhongyuan.png` 上使用
> **二、斜向山脉**：山脊基线沿对角线，**峰体保持竖直**（不是整体旋转），可沿地图上的斜向山带连续铺排
> **三、大幅山脉**：整幅不透明构图（纸底 + 4 层山带 + 云气 + 林带 + 河），可直接当"秦岭"这类区域美术图

## 交付内容

| 位置 | 内容 |
| --- | --- |
| `SVG/19_地图装饰_秦汉风/` | 18 个装饰件 + 3 张大幅山脉的矢量源 |
| `PNG/19_地图装饰_秦汉风/` | 18 张装饰切片 + `panoramas/` 3 张大幅图 + `_切片清单.csv` + 3 张预览 |
| `Assets/Art/PNG/GameWorld_Map/Deco_QinHan/` | 18 张装饰切片（Sprite 导入） |
| `Assets/Art/PNG/GameWorld_Map/Range/` | 3 张大幅山脉（Sprite 导入，整幅） |

## 清单

| 文件 | 尺寸 | 用途 |
| --- | --- | --- |
@@TABLE@@

## 秦汉风的关键做法

| 元素 | 时代语言 |
| --- | --- |
| 山 | 三排成串（远石青 / 中石绿 / 近赭石），带山肩折点、墨边、左坡金线受光；山间与山脚走**云气纹** |
| 水 | 石青→石青浅的径向渐变，水面加画像石式短水纹；岸用赭石浅滩 |
| 松林 | 三段塔形冠，墨绿压暗、石绿提亮（汉画像里的松） |
| 城/关 | 夯土墙 + 横向夯层 + **双阙楼**（夯土楼身、朱柱、斗拱层、黑瓦歇山）+ 朱墙黑瓦屋群 |
| 村落 | 夯土民舍 + 黑瓦顶 + 朱门 |
| 云气 | 卷云曲线（螺旋）+ 柔边雾带，是秦汉图绘最有辨识度的语言 |

## 用法

1. **装饰件**：1:1 摆到地图（4096×3138），缩放 0.85~1.2；不透明度 93~100，压在暗角处降到 80~88。
2. **斜向山脉**：`Diag_Up` 沿"西南→东北"的山带连续铺；`Diag_Down` 用于反向山带；`Long_Diag` 适合长距离铺排，首尾可与 `Diag_*` 衔接。
3. **大幅山脉**：整幅图自带纸底，直接当区域美术图（建议 1:1 或缩小到 0.6~0.8 放在地图的山区，也可单独作为读条/过场背景）。
4. 光源统一左上（金线在左坡、暗面在右坡）；不要水平翻转。

## 重新导出

```bat
cd /d "C:\\Users\\26751\\Desktop\\游戏美术资源\\SVG\\svg2png"
python svg2png.py --only 19_地图装饰
```

含 `linearGradient` / `radialGradient`，**必须用浏览器内核渲染**（工具默认 Edge 后端）。

## 已知取舍

| # | 说明 |
| --- | --- |
| 1 | 山体是"成串单峰"的地图符号语言，不是写实照片级山体（要更写实需美术在 PS 里做笔触与体积） |
| 2 | 大幅山脉为程序化构图，适合当"占位级成品"；正式美术图建议以 `Range_Qinling_01` 为构图参考重绘 |
| 3 | 云气纹为矢量卷云，颗粒/晕染效果需后期叠加 |
| 4 | 暂缺：渡口、矿场；水面可另加"波纹层"做动态 |
"""

DOC.write_text(TPL.replace("@@TABLE@@", table), encoding="utf-8")
print("文档:", DOC)
