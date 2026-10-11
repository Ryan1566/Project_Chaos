# 第三方素材许可记录（Kenney · Platformer Art Complete Pack）

> 状态：已入库　最后更新：2026-10-10　Owner：策划
> 关联：`Story/02_系统策划/01_局内地图生成系统.md` §16.8「第三方素材策略与许可证红线」

## 1. 素材包

| 项 | 值 |
| --- | --- |
| 包名 | **Platformer Art Complete Pack**（含 Base pack / Buildings / Candy / Ice / Mushroom 扩展；本次只取 **Base pack/Tiles**） |
| 作者 | Kenney Vleugels（www.kenney.nl） |
| 来源页 | https://kenney.nl/assets/platformer-art-deluxe |
| 直链（下载用） | https://kenney.nl/media/pages/assets/platformer-art-deluxe/cb30f83169-1677696393/kenney_platformer-art-deluxe.zip |
| 许可证 | **CC0 1.0（公有领域）** — http://creativecommons.org/publicdomain/zero/1.0/ |
| 署名 | 不强制（原文：`Credit (Kenney or www.kenney.nl) would be nice but is not mandatory.`）；**本项目仍在致谢名单中列出 Kenney** 以示尊重 |
| 下载日期 | 2026-10-10 |
| 校验 | 下载文件 6,582,450 字节；本机 pwsh 直连被拦，改用 Python `urllib` 下载（见下「复现方式」） |
| 许可原文 | 包内 `license.txt`（随包保存，内容见 §3） |

## 2. 本次实际使用的文件（10 张，70×70 PNG，均取自 `Base pack/Tiles/`）

| 主题 | Top（顶面） | Fill（实心） | Beam（台面） | Deco（背景，运行时染暗） |
| --- | --- | --- | --- | --- |
| 地下 `Underground` | `stoneMid` | `stoneCenter` | `stoneHalfMid` | `stoneCenter` |
| 地表 `Surface` | `grassMid` | `grassCenter` | `grassHalfMid` | `dirtCenter` |
| 地表建筑 `Building` | `castleMid` | `castleCenter` | `castleHalfMid` | `castleCenter` |

- 落位（原图，供管线重新导入）：`Assets/AI/runmap-art/kenney/*.png`
- 落位（Unity 资产）：`Assets/TileMap/Kenney/{Theme}/Tile_{Fill,Top,Beam,Deco}.png` + 同名 `.asset`
- 房间块（皮肤版）：`Assets/Prefabs/RunMap/Kenney/{Theme}/Chunk_{Theme}_{Type}.prefab`

> ⚠ **不是永远只用这几张**：这三套只是"能区分空间层"的最小集。要做正式美术时，整个 Kenney 包的 173 张 Base pack 瓦片都能用（`Left/Right/Cliff/Ledge/Hill/Half*` 这些边缘件正是自动拼接需要的），但**边缘件必须配 RuleTile**，否则接缝会很难看（见 §16.9）。

## 3. 许可原文（逐字，包内 `license.txt`）

```
###############################################################################

	Platformer Art Complete Pack by Kenney Vleugels (www.kenney.nl)

	------------------------------
		
		License (CC0)
	http://creativecommons.org/publicdomain/zero/1.0/

	You may use these graphics in personal and commercial projects.
	Credit (Kenney or www.kenney.nl) would be nice but is not mandatory.

###############################################################################
```

## 4. 复现方式（本机实测）

```powershell
# 沙箱里 pwsh 的 Invoke-WebRequest / curl 都连不上外网（TLS 层被拦），但 Python urllib 可以
& <bundled-python> -c "import urllib.request; open('kenney_dl.zip','wb').write(urllib.request.urlopen('<直链>').read())"
# 解包（只取 Base pack/Tiles 里要用的几张）
& <bundled-python> -c "import zipfile; ..."
```

> 因此**不要**把"下载素材"写成 pwsh 脚本；要走 Python。下载产物与解包产物都在 `Assets/AI/runmap-art/`（代理工作区，不进正式资产目录）。

## 5. 变更记录

| 日期 | 变更 |
| --- | --- |
| 2026-10-10 | 首次登记：下载 Platformer Art Complete Pack，按 CC0 入库 10 张瓦片用于三套空间层主题皮肤。 |
