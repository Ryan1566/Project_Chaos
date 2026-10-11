# -*- coding: utf-8 -*-
"""
生成「局内地图生成」所需的 7 张配置表（xlsx），格式严格对齐 Assets/Excel/Chaos_excel/ 下既有表：
  第 1 行：A 列「类名」+ B 列 表名
  第 2 行：A 列「是否读取」+ B 列 IsConfig
  第 3 行：中文列说明
  第 4 行：英文属性名（= 生成的 C# 字段名 / Json key；第一列固定 Id）
  第 5 行：端标记（本系统全部 client）
  第 6 行：类型（只有 12 个白名单：int uint long ulong byte sbyte short ushort float double bool string）
  第 7 行起：正式数据（导出器从第 7 行开始取值）

内容与 RunMap.RunMapCatalog.CreateGreybox() （已实测 800/800 通过的那套）**逐字段一致**，
所以换成表驱动后同样的种子必须生成一模一样的地图 —— 这是验收判据。

跑法：
  python gen_map_configs.py            # 输出到 out/ 目录
产物再由 Unity 进程复制进 Assets/Excel/Chaos_excel/（沙箱写不了那个目录）。
"""
import os
import openpyxl

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "xlsx")

# ── 白名单类型（唯一权威 = ConfigManager.Convert 的 case 列表）──
T_UINT, T_STR, T_FLOAT, T_BOOL = "uint", "string", "float", "bool"


def sheet_rows(table_name, cols, rows):
    """cols: [(英文属性名, 类型, 中文说明)]；rows: [[值...]]"""
    # ⚠ 行宽必须**正好等于列数**：
    #   第 1/2 行是 A 列放"类名/是否读取"、B 列放表名/IsConfig，其余留空；
    #   第 3 行 A 列是"说明"，B..N 列是各列的中文说明（对应 B..N 列）。
    #   一旦某行比列数宽，sheet 用域会被撑宽，导出器的 GetEnd() 会在第 4 行扫到空格 → 整批中断
    #   （我就是这么踩的：ChunkSlot 5 列、表头写成 6 格 → "第4行第6列为空" → 7 张表一张都没导出）。
    n = len(cols)
    head1 = (["类名", table_name] + [""] * (n - 2)) if n >= 2 else [table_name]
    head2 = (["是否读取", "IsConfig"] + [""] * (n - 2)) if n >= 2 else ["IsConfig"]
    head3 = ["说明"] + [c[2] for c in cols[1:]]
    head4 = [c[0] for c in cols]
    head5 = ["client"] * len(cols)
    head6 = [c[1] for c in cols]
    return [head1, head2, head3, head4, head5, head6] + rows


def write_table(file_base, sheet_name, cols, rows):
    wb = openpyxl.Workbook()
    ws = wb.active
    ws.title = sheet_name
    for r, row in enumerate(sheet_rows(file_base, cols, rows), start=1):
        for c, v in enumerate(row, start=1):
            ws.cell(row=r, column=c, value=v)
    path = os.path.join(OUT, file_base + ".xlsx")
    wb.save(path)
    return path, len(rows), len(cols)


# ══════════════════════════════════════════════════════════════
#  1. 读条阶段描述（5 语言；不走游戏本地化系统，直接按语言取列）
# ══════════════════════════════════════════════════════════════

LOADING_COLS = [
    ("Id", T_UINT, "序号"),
    ("stage", T_UINT, "阶段码：1 参数装配 2 生成房间图 3 排布网格 4 挑选房间块 5 实例化 6 封堵填充 7 校验 8 完成"),
    ("sort", T_UINT, "展示顺序"),
    ("descCN", T_STR, "简体中文描述（禁止英文逗号/双引号/换行）"),
    ("descTW", T_STR, "繁体中文描述"),
    ("descEN", T_STR, "英语描述"),
    ("descJP", T_STR, "日语描述"),
    ("descKR", T_STR, "韩语描述"),
]

LOADING_ROWS = [
    [1, 1, 1, "正在准备本次探索的地形…", "正在準備本次探索的地形…",
     "Preparing the terrain for this expedition…", "今回の探索の地形を準備しています…", "이번 탐험의 지형을 준비하고 있습니다…"],
    [2, 2, 2, "正在勾勒边缘空间的轮廓…", "正在勾勒邊緣空間的輪廓…",
     "Sketching the outline of the Edge Realm…", "エッジレルムの輪郭を描いています…", "엣지 렐름의 윤곽을 그리고 있습니다…"],
    [3, 3, 3, "正在铺设地表与地下…", "正在鋪設地表與地下…",
     "Laying out the surface and the depths…", "地表と地下を敷き詰めています…", "지표와 지하를 깔고 있습니다…"],
    [4, 4, 4, "正在挑选房间与走廊…", "正在挑選房間與走廊…",
     "Choosing rooms and corridors…", "部屋と通路を選んでいます…", "방과 복도를 고르고 있습니다…"],
    [5, 5, 5, "正在建造建筑与通路…", "正在建造建築與通路…",
     "Raising buildings and passages…", "建物と通路を建てています…", "건물과 통로를 세우고 있습니다…"],
    [6, 6, 6, "正在封闭多余的通道…", "正在封閉多餘的通道…",
     "Sealing the unused passages…", "不要な通路を塞いでいます…", "사용하지 않는 통로를 막고 있습니다…"],
    [7, 7, 7, "正在确认每一条路径都能走通…", "正在確認每一條路徑都能走通…",
     "Verifying that every path connects…", "すべての経路が繋がるか確認しています…", "모든 경로가 연결되는지 확인하고 있습니다…"],
    [8, 8, 8, "地形已生成，准备进入…", "地形已生成，準備進入…",
     "Terrain ready. Proceeding…", "地形の生成が完了しました…", "지형 생성이 완료되었습니다…"],
]

# ══════════════════════════════════════════════════════════════
#  2~7. 地图生成本体（与 CreateGreybox 的灰盒名录逐字段一致）
# ══════════════════════════════════════════════════════════════

THEMES = ["Underground", "Surface", "Building"]
THEME_CN = ["地下", "地表", "地表建筑"]
THEME_LAYER = [1, 2, 3]
# 与 RunMapPlaceholderPipeline 生成顺序一致：Entry / Corridor / Combat / DeadEnd / LayerLink / BossGate
KINDS = [("Entry", 1), ("Corridor", 2), ("Combat", 3), ("DeadEnd", 11), ("LayerLink_Stair", 9), ("BossGate", 10)]
SOCKET_DIRS = {
    "Entry": [(2, 0, 1)],                                            # 2=右
    "Corridor": [(1, 0, 1), (2, 0, 1), (3, 0, 1), (4, 0, 1)],         # L R U D
    "Combat": [(1, 0, 1), (2, 0, 1), (3, 0, 1), (4, 0, 1)],
    "DeadEnd": [(1, 0, 1), (3, 0, 1), (4, 0, 1)],
    "LayerLink_Stair": [(3, 0, 2), (4, 0, 2)],                        # 跨层通道
    "BossGate": [(1, 0, 1), (3, 0, 1), (4, 0, 1), (2, 1, 3)],         # 右侧是 boss 门（type=3，档 1）
}
SLOTS = {
    "Entry": [(5, 5.5, 3.5)],
    "Corridor": [(1, 12.0, 3.5)],
    "Combat": [(1, 10.0, 3.5), (1, 20.0, 12.5), (2, 33.0, 8.5)],
    "DeadEnd": [(3, 31.5, 5.5)],
    "LayerLink_Stair": [(1, 20.5, 12.0)],
    "BossGate": [(2, 28.0, 11.5)],
}


def socket_xy(d, profile):
    if d == 1:
        return 0.5, 5 + (profile * 8)
    if d == 2:
        return 39.5, 5 + (profile * 8)
    if d == 3:
        return 20.5, 23.5
    return 20.5, 0.5


def build():
    built = []
    OUT_DIR = OUT
    os.makedirs(OUT_DIR, exist_ok=True)

    # 1) 读条表
    built.append(write_table("MapLoading_地图生成读条表", "MapLoadingSheet0", LOADING_COLS, LOADING_ROWS))

    # 2) 主题表
    biome_cols = [
        ("Id", T_UINT, "主题 ID"),
        ("themeKey", T_STR, "美术/瓦片主题 Key（与 TileMap 目录名一致）"),
        ("nameCN", T_STR, "中文名"),
        ("layer", T_UINT, "空间层：1 地下 2 地表 3 地表建筑"),
        ("retryMax", T_UINT, "整图重生成上限"),
        ("enable", T_BOOL, "是否启用"),
    ]
    biome_rows = [[i + 1, THEMES[i], THEME_CN[i], THEME_LAYER[i], 20, "true"] for i in range(3)]
    built.append(write_table("MapBiome_地图主题表", "MapBiomeSheet0", biome_cols, biome_rows))

    # 3) 预设表
    preset_cols = [
        ("Id", T_UINT, "预设 ID"),
        ("biomeId", T_UINT, "外键 → MapBiome.Id"),
        ("nameCN", T_STR, "备注名"),
        ("mainPathMin", T_UINT, "主路房间数下限"),
        ("mainPathMax", T_UINT, "主路房间数上限"),
        ("branchMin", T_UINT, "支路房间总数下限"),
        ("branchMax", T_UINT, "支路房间总数上限"),
        ("totalMin", T_UINT, "房间总数下限"),
        ("totalMax", T_UINT, "房间总数上限"),
        ("specialTreasure", T_UINT, "死路奖励房数量下限"),
        ("specialCamp", T_UINT, "扎营点数量（预留）"),
        ("retryMax", T_UINT, "单节点选块/回溯上限"),
        ("enable", T_BOOL, "是否启用"),
    ]
    single = dict(mainPathMin=6, mainPathMax=8, branchMin=4, branchMax=7, totalMin=10, totalMax=15,
                  specialTreasure=2, specialCamp=1, retryMax=30)
    preset_rows = []
    for i in range(3):
        r = single
        preset_rows.append([10 + i, i + 1, "单层_" + THEMES[i], r["mainPathMin"], r["mainPathMax"],
                            r["branchMin"], r["branchMax"], r["totalMin"], r["totalMax"],
                            r["specialTreasure"], r["specialCamp"], r["retryMax"], "true"])
    preset_rows.append([20, 1, "双层_地表_地下", 7, 9, 4, 8, 12, 18, 2, 1, 40, "true"])
    built.append(write_table("MapPreset_原型图主表", "MapPresetSheet0", preset_cols, preset_rows))

    # 4) 预设层序表
    layer_cols = [
        ("Id", T_UINT, "序号（表内唯一）"),
        ("presetId", T_UINT, "外键 → MapPreset.Id"),
        ("layerOrder", T_UINT, "层顺序，从 1 连续递增"),
        ("layer", T_UINT, "空间层：1 地下 2 地表 3 地表建筑"),
        ("isMainPath", T_BOOL, "主路是否必须经过本层"),
        ("linkFromLayerOrder", T_UINT, "与第几层相连（0=首层）"),
        ("allowDirect", T_BOOL, "是否允许直连该层"),
        ("linkMin", T_UINT, "跨层连接件数量下限"),
        ("linkMax", T_UINT, "跨层连接件数量上限"),
    ]
    layer_rows = []
    n = 0
    for i in range(3):
        n += 1
        layer_rows.append([n, 10 + i, 1, THEME_LAYER[i], "true", 0, "true", 0, 0])
    n += 1
    layer_rows.append([n, 20, 1, 2, "true", 0, "true", 1, 2])
    n += 1
    layer_rows.append([n, 20, 2, 1, "true", 1, "true", 1, 2])
    built.append(write_table("PresetLayer_原型图层序表", "PresetLayerSheet0", layer_cols, layer_rows))

    # 5) 房间块表
    chunk_cols = [
        ("Id", T_UINT, "房间块 ID"),
        ("biomeId", T_UINT, "外键 → MapBiome.Id"),
        ("themeKey", T_STR, "主题 Key"),
        ("kind", T_UINT, "类型：1 入口 2 通路 3 战斗 9 层间连接 10 boss 门 11 死路奖励"),
        ("layer", T_UINT, "所属空间层（0=跨层）"),
        ("prefabPath", T_STR, "Resources 相对路径（不含扩展名）"),
        ("weight", T_UINT, "抽取权重"),
        ("maxCount", T_UINT, "单图最大出现次数"),
        ("isUnique", T_BOOL, "全图是否最多一次"),
        ("enable", T_BOOL, "是否启用"),
    ]
    chunk_rows = []
    for t in range(3):
        for (tname, kind) in KINDS:
            cid = 1000 + t * 100 + kind
            layer = 0 if tname == "LayerLink_Stair" else THEME_LAYER[t]
            chunk_rows.append([cid, t + 1, THEMES[t], kind, layer,
                               "Prefabs/RunMap/Kenney/" + THEMES[t] + "/Chunk_" + THEMES[t] + "_" + tname,
                               10, 99, "false", "true"])
    built.append(write_table("MapChunk_房间块表", "MapChunkSheet0", chunk_cols, chunk_rows))

    # 6) 接口表
    sock_cols = [
        ("Id", T_UINT, "序号（表内唯一）"),
        ("chunkId", T_UINT, "外键 → MapChunk.Id"),
        ("dir", T_UINT, "方向：1 左 2 右 3 上 4 下"),
        ("heightProfile", T_UINT, "横向高度档（0/1/2）；竖直开口恒为 0，按同列匹配"),
        ("socketType", T_UINT, "1 普通通道 2 跨层通道 3 boss 门"),
        ("posX", T_FLOAT, "块内局部坐标 X（单位：米）"),
        ("posY", T_FLOAT, "块内局部坐标 Y（单位：米）"),
    ]
    sock_rows = []
    n = 0
    for t in range(3):
        for (tname, kind) in KINDS:
            cid = 1000 + t * 100 + kind
            for (d, prof, stype) in SOCKET_DIRS[tname]:
                n += 1
                x, y = socket_xy(d, prof)
                sock_rows.append([n, cid, d, prof, stype, x, y])
    built.append(write_table("ChunkSocket_房间块接口表", "ChunkSocketSheet0", sock_cols, sock_rows))

    # 7) 标记点表
    slot_cols = [
        ("Id", T_UINT, "序号（表内唯一）"),
        ("chunkId", T_UINT, "外键 → MapChunk.Id"),
        ("slotType", T_UINT, "1 敌人 2 精英 3 宝箱 5 场景事件 6 扎营点"),
        ("posX", T_FLOAT, "块内局部坐标 X（单位：米）"),
        ("posY", T_FLOAT, "块内局部坐标 Y（单位：米）"),
    ]
    slot_rows = []
    n = 0
    for t in range(3):
        for (tname, kind) in KINDS:
            cid = 1000 + t * 100 + kind
            for (st, x, y) in SLOTS[tname]:
                n += 1
                slot_rows.append([n, cid, st, x, y])
    built.append(write_table("ChunkSlot_房间块标记点表", "ChunkSlotSheet0", slot_cols, slot_rows))

    return built


if __name__ == "__main__":
    for (path, rows, cols) in build():
        print("OK %-46s rows=%3d cols=%d" % (os.path.basename(path), rows, cols))
    print("输出目录：", OUT)
