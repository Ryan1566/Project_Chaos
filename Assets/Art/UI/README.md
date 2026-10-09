# Art/UI — 界面 UI 切图库（UI sprite atlas source）

> 来源：`Project_Chaos` 界面美术切图库（设计基准 **2800×1575**，16:9）
> 交付源：`游戏美术资源/PNG/切图库`（中文名）→ 本目录（**英文名，不带尺寸**）
> 生成日期：2026-10-10　｜　共 **164 张**

## 1. 命名

```
<类别>_<变体>.png          例：Button_Lg_Normal.png  Slider_Fill.png  Bar_Fill_Health.png
```

* 目录 `NN_<Category>`：`00_Panels` … `10_System`
* 文件名**不含尺寸**；尺寸与九宫格参数查 `切图清单_EN.csv`（随交付提供）

## 2. 已写入的导入设置（全部 164 张）

| 项 | 值 |
| --- | --- |
| Texture Type | `Sprite (2D and UI)` |
| Sprite Mode | `Single` |
| Pixels Per Unit | **100**（与 Canvas 的 referencePixelsPerUnit 一致，`SetNativeSize()` = 像素尺寸）|
| Mesh Type | **`Full Rect`**（九宫格必须）|
| Border | 逐张按清单写入（如 `Panel_Main` = 36/36/36/36）|
| Mipmap / Filter / Wrap | off / Bilinear / Clamp |
| Alpha Is Transparency | on |

## 3. 使用约定

* **Sliced**：元素尺寸 ≥ Border 之和时使用（面板、按钮、条、行底等）。
* **Simple**：元素比 Border 还小时使用（如 8×56 的细条按钮），避免四边被裁切。
* 当前预制体按 **1920×1080** 参考分辨率布局，素材按 **2800×1575** 设计，
  比例 1920/2800 ≈ 0.686；**不要**为了对齐素材而改 RectTransform 尺寸，让九宫格拉伸即可。
* 需要 1:1 使用素材时，把 Canvas 参考分辨率改成 2800×1575。

## 4. 已换图的预制体

`Resources/UIPanels/MainMenuPanel`、`SLPanel`、`SettingPanel`、`SubUI_Prefab/RecordCell`
（共替换 108 个 Image；`Viewport` / `Checkmark` / 按键 `Icon` / 分组容器刻意保留原样）

## 5. 重新导出位图

素材的矢量源在 `游戏美术资源/SVG/切图库/`，改完用：

```bat
cd /d "C:\Users\26751\Desktop\游戏美术资源\SVG\svg2png"
python svg2png.py --only 切图库
```

再把 PNG 覆盖回本目录（英文重命名对照表见交付文档 `02_切图库与Unity接入说明.md` §8）。
