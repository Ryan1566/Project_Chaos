# gittool —— Unity 项目安全拉取助手

把「拉取最新版本」变成 **检查更新 → 看清影响 → 确认拉取** 三步，
避免在 Unity 里盲目更新，等一大段重编译却不知道发生了什么。

```
gittool/
  pull.bat        Windows 双击入口（自动找 Git 自带的 bash 并转交）
  pull.sh         真正干活的脚本（macOS / Linux / Git Bash 同样可用）
  README.md       本文件
```

配套的 Unity 编辑器窗口在 `Assets/Scripts/Editor/GitTool/`，
菜单入口：**Tool → Git拉取助手**。

---

## 先说清楚：Library 从来就不在 Git 里

这个工具**不是为了"保护 Library 不被删除"而存在的** —— 因为 `.gitignore` 里已经写了 `Library`，
`git pull` 天然就不会碰它。任何号称"拉取会覆盖 Library"的说法都是误解。

真正让你等很久的，是拉取之后 Unity 的**资源导入 + 脚本重编译**，而且这件事没法完全避免：

| 改动内容 | Unity 的反应 | 能否避免 |
|---|---|---|
| `Assets/**/*.cs` | 增量重编译（通常几秒~几十秒） | 不能，也无法加速 |
| `Assets/**` 其他资源 | 局部 Reimport | 不能 |
| `ProjectSettings/**` | 大概率**全量 Reimport**（最慢） | 不能，只能提前知道 |
| `Packages/manifest.json` | 重新解析包，可能全量 Reimport | 不能，只能提前知道 |
| `ProjectSettings/ProjectVersion.txt` | 需要换 Unity 版本，否则打不开 | 必须提前知道 |

所以本工具的价值是**提前告诉你这次更新要付多少代价**，以及顺手挡掉那些真正浪费时间的坑：

- 分支没配 upstream → `git pull` 直接报错（本仓库当前就是这样）
- 本地改动和远端撞车 → 拉取被拒绝，卡在半路
- 拉取时 Unity 正开着 → 文件占用、导入报错，排查起来比等编译久得多
- 仓库里被跟踪的构建产物（`.vs/`、`obj/`、`*.csproj`）→ 每次拉取都产生无意义冲突

---

## 用法

### 方式一：双击 / 命令行

```bash
# Windows：双击 gittool\pull.bat，或在终端里
gittool\pull.bat

# macOS / Linux / Git Bash
./gittool/pull.sh

# 只看会影响什么，不真的拉取（推荐先跑这个）
./gittool/pull.sh --dry-run

# 跳过确认直接拉取
./gittool/pull.sh --yes

# 本地改动挡住了拉取时，丢弃它们再拉（不可恢复，详见下一节）
./gittool/pull.sh --discard

# 先看会丢什么，什么都不改
./gittool/pull.sh --discard --dry-run

# 指定远端
./gittool/pull.sh --remote upstream
```

### 方式二：Unity 编辑器窗口

菜单 **Tool → Git拉取助手**

1. 点 **检查更新** —— 会 `fetch` 并列出所有变更，按对 Unity 的影响分类着色
2. 看完分类和影响说明后，点 **拉取更新** —— 弹窗二次确认，然后 fast-forward 拉取
3. 忘记配 upstream 时，窗口里会出现 **设置** 按钮一键补上
4. 本地改动挡路时，用红色的 **※ 放弃本地更改并更新** 按钮 —— 会列出待丢弃的文件并要求二次确认

`打开命令行` 按钮会在工程根目录开一个 `cmd` 窗口，拉取失败需要手动处理冲突时用得上。

---

## 放弃本地更改并更新（`--discard`）

默认行为是**保守**的：本地改动会挡住 `--ff-only`，工具停下来告诉你怎么手动处理，
绝不替你决定。当你确定那些改动不要了，用显式开关：

```bash
./gittool/pull.sh --discard --dry-run   # 先看会丢什么
./gittool/pull.sh --discard             # 确认后真的丢
```

Windows 双击用户可以用 Unity 窗口里的 **※ 放弃本地更改并更新** 按钮，效果一样。

它做的事只有一件：`git reset --hard HEAD`，然后 `git pull --ff-only`。

### 「已经是最新版本」时也会丢弃

这一点容易踩坑，所以单独说明：**`--discard` 丢弃本地改动，和「远端有没有新提交」无关。**

即使你已经落后 0 个提交（没有东西可拉），`--discard` 依然会把本地已跟踪的改动丢掉，
然后告诉你「远端无新提交，没有执行拉取」。因为你的诉求是「把这些改动清掉」，
如果工具因为「没东西可拉」就什么都不做，你会以为它坏了。

反过来，**不带 `--discard` 时永远不丢**，并且会明确告诉你「本地那 N 个改动没有被丢弃」。

### 它会丢什么、不会丢什么

| | |
|---|---|
| ✅ **会丢**：已跟踪文件的未提交改动（已 `git add` 的和没 add 的都一样） | 永久消失，**没有任何备份**，reflog 也找不回来 |
| ❌ **不丢**：未跟踪的新文件 | 全程不执行 `git clean`。Unity 里刚做的、还没提交的脚本和资源都原样保留 |
| ❌ **不丢**：未推送的提交 | 见下 |

关于最后一行：如果你本地领先远端，`reset --hard HEAD` 之后 HEAD 仍在远端前面，
`--ff-only` 照样失败。所以工具在动手**之前**就会中止，列出那些提交，并告诉你确切命令：

```
本地领先 origin/main 2 个提交，--discard 解决不了这种情况
...
  git reset --hard origin/main     # 确认这些提交可以永久丢弃后再手动执行
```

它不会替你跑这一步 —— 丢提交和丢未提交的改动不是一个量级的破坏，值得你自己按一次回车。

> **为什么不用 `git clean`？**
> 这个仓库的工作区里有上千个未跟踪文件，其中包含上百个尚未提交的 `.cs` 脚本和整套
> `Assets/Art` 美术资源。`git clean` 会把它们一次性抹掉且不可恢复。
> 这是本工具的硬边界，任何参数组合都不会突破。

---

### 排查：`pull.bat --where`

Windows 上双击 `pull.bat` 时，它会先定位 Git 自带的 `bash.exe`（`pull.sh` 需要它）。
如果定位失败，用这个开关看它到底找到了什么：

```bat
gittool\pull.bat --where
```

```
  git  : E:\Git\cmd\git.exe
  bash : E:\Git\bin\bash.exe
```

**注意**：它**不会**用 `where bash` 去猜。Windows 自带 `C:\Windows\System32\bash.exe`，
那是 WSL 的启动器 —— 没装 WSL 的话会莫名弹出「正在安装 WSL」的提示。
gittool 改为从 `git.exe` 的位置反推 Git 根目录，再去里面找 `bash.exe`，
并硬性排除 `System32`。Git 装在非默认盘符（如 `E:\Git`）也能正确找到。

---

## 更新范围与分支语义

一句话：**更新整个工作区，但只针对你当前签出的那一个分支。**

| 维度 | 行为 |
|---|---|
| 更新范围 | 整个工作区（所有被 Git 跟踪的文件） |
| 分支 | 只有当前签出的那一个，跟着它的 upstream 走 |
| 选择分支 | ❌ 不能。`--remote` 只选远端，不选分支 |
| 选择路径 | ❌ 不能。Git 本身没有「按路径 pull」这种东西 |
| 本地领先远端 | `--ff-only` 直接拒绝，绝不产生 merge commit |
| 未跟踪 / 忽略的文件 | 完全不碰（`Library`、`Temp`、`obj`，以及任何没纳入版本控制的资源） |
| `--discard` 的影响 | 只改「本地已跟踪文件的改动要不要保留」，**不改变更新范围和分支选择** |

除 `git fetch --prune` 之外，它实际执行的写操作只有这些：

```bash
git pull --ff-only                                  # 优先用当前分支的 upstream
git pull --ff-only "$REMOTE" "$CURRENT_BRANCH"      # upstream 没配好时的兜底
git reset --hard HEAD                               # 仅 --discard，且在 pull 之前
```

（第三条只在显式 `--discard` 时出现，作用范围是已跟踪文件，详见上一节。）

它**不会**切分支、不会跨分支合并、不会只更新部分目录。
要更新别的分支，请自己先 `git checkout` 过去再运行本工具。

### 为什么不做「只更新指定路径」

知道 `ProjectSettings/**` 会触发全量 Reimport 之后，很容易想「那我只拉 `Assets/Scripts` 不就好了」。
技术上可行（`git checkout origin/main -- Assets/Scripts`），但在这个项目里不建议：

- 工作区会变成一个**不对应任何 commit 的混合体**，下次 pull 必然大面积冲突
- `ProjectSettings` 与资源版本不匹配，会引出更难查的问题（Tag / Layer 缺失、渲染管线设置对不上）
- `.meta` 的 GUID 交叉引用错乱，是 Unity 项目里最疼的一类故障

省下的那点导入时间，通常远不够赔。含高影响文件的更新，让 Unity 自己导入完就好。

---

## 变更分类

脚本和编辑器窗口用同一套分类规则（等级越高越费时间）：

| 等级 | 类别 | 匹配路径 | 对 Unity 的影响 |
|---|---|---|---|
| 🔴 3 | 版本升级 | `ProjectSettings/ProjectVersion.txt` | 需要装对应 Unity 版本，否则打不开工程 |
| 🔴 3 | 工程配置 | `ProjectSettings/**` | 大概率全量 Reimport |
| 🔴 3 | 包依赖 | `Packages/**` | 重新解析包，可能全量 Reimport |
| 🟡 2 | C#脚本 | `**/*.cs` | 增量重编译 |
| 🟡 2 | 插件程序集 | `Assets/**/*.dll` | 重编译引用它的脚本 |
| ⚪ 1 | 资源 / 元数据 | `Assets/**` 其他 | 局部 Reimport |
| ⚪ 0 | IDE工程 | `*.csproj`、`*.sln` | Unity 会自行重生成，忽略即可 |
| ⚪ 0 | 本地生成物 | `.vs/`、`obj/`、`UserSettings/` 等 | 不该进版本库 |

---

## 它绝不会做的事

这是刻意设计的边界，避免工具"帮你"帮出事故：

- ❌ **不执行 `git clean`** —— 任何时候、任何参数都不。未跟踪的文件永远安全。
- ⚠️ **不执行 `git reset --hard`，除非你显式传了 `--discard`**（或点了窗口里那个红色按钮）。
  默认模式下不存在这条路径。
- ❌ **不丢弃未推送的提交** —— 即使开了 `--discard` 也不会，只会列出来让你自己决定。
- ❌ 不自动 `stash` 或 `rebase`（会动你的工作区）
- ❌ 不删除、不重建 `Library` / `Temp` / `obj`
- ❌ 默认只用 `--ff-only`，绝不产生意外的 merge commit

拉取遇到冲突时，工具默认停下并告诉你手工怎么处理，而不是自作主张替你决定。
只有 `--discard` 是例外 —— 那是你明确要求它动手的。

---

## 常见问题

**「分支没有配置 upstream」**
当前 `Branch_feature_1` 就没有。工具会推断出该跟踪哪个远端分支（优先同名分支，其次 `main`/`master`），
问你一句后帮你设好。也可以用 Git Bash 手动设：
```bash
git branch --set-upstream-to=origin/main Branch_feature_1
```

**「无法 checkout main / main 被占用」**
本仓库的 `main` 被签出在另一个 worktree（`D:\Unity Projects\worktree1`），
所以在主工程目录里 `git checkout main` 会失败。这是 Git 的正常限制，不是工具的问题。
要么去那个 worktree 里操作，要么先 `git worktree remove` 掉它。

**「拉取被拒绝：local changes would be overwritten」**
本地改动和远端更新撞上了。工具默认不会替你 stash，三个选择：

```bash
# 1. 改动还要 → 先收起来，拉完再放回去
git stash && git pull && git stash pop

# 2. 改动还要，但想留下提交记录
git pull --rebase

# 3. 改动不要了 → 让工具丢掉它们再拉（不可恢复，未跟踪文件不受影响）
./gittool/pull.sh --discard
```

拿不准选哪个就先 `./gittool/pull.sh --discard --dry-run` 看一眼会丢什么。

**「更新完第一次打开 Unity 特别慢」**
说明这次含 `ProjectSettings` / `Packages` 级别的高影响变更，让 Unity 自己导入完就行。
中途别关编辑器、别删 `Library` —— 删了只会让它从头再来一遍，更慢。

---

## 关于本仓库 .gitignore 的建议（未自动修改）

排查过程中发现两处会持续制造拉取噪音的问题，**本工具没有擅自改动你的仓库**，
如果认同，可以自己动手：

**1. 被跟踪的本地生成物** —— 每次拉取都产生无意义冲突，建议移出跟踪：
```bash
git rm -r --cached .vs obj UserSettings
printf '%s\n' '.vs/' 'obj/' 'UserSettings/' 'Temp/' '*.csproj' '*.sln' >> .gitignore
git commit -m "把本地生成物移出 Git 跟踪"
```
> `*.csproj` / `*.sln` 是 Unity 自己生成的，跟踪它们没有任何好处。

**2. `Packages/` 被整个忽略** —— 这是**各机器包版本漂移**的根源，
也是"更新后全量重导入"的常见原因之一：别人加了包，你这边完全没有。
建议至少把清单文件纳入版本控制：
```bash
printf '%s\n' 'Packages/*' '!Packages/manifest.json' '!Packages/packages-lock.json' >> .gitignore
git add -f Packages/manifest.json Packages/packages-lock.json
git commit -m "把包清单纳入版本控制"
```

---

## 维护说明

- 命令行版的**全部逻辑都在 `pull.sh`**。`pull.bat` 只负责找到 Git 自带的 `bash.exe` 并转交，
  这样不必维护两份会逐渐走样的实现。
- 编辑器窗口走 `System.Diagnostics.Process` 直接调用 `git.exe`，不依赖 bash。
- 两边的**分类规则是各自的表格**（`pull.sh` 的 `classify_path()` 与
  `GitHelper.Classify()`）。改动其中一处时记得同步另一处。
- 仓库里有中文文件名，所以两边都关掉了 `core.quotepath`
  （否则路径会被转义成 `"\346\226\207"`，分类逻辑直接失效）。
- `--discard` 的语义两边必须一致，改一边记得改另一边。三条不能动的规则：
  只作用于已跟踪文件、不碰未跟踪文件、不丢未推送的提交。
- `git clean` 在两边都不存在。加功能时不要顺手加进来。

### 改 `Assets/Scripts/Editor/GitTool/*.cs` 时务必注意

1. **文件是 GBK 编码，不是 UTF-8。** 项目里已有的脚本都是这个编码，新文件必须跟随。
   直接按 UTF-8 写会让 Unity 里的中文注释变成乱码。
   建议做法：先 `iconv -f GBK -t UTF-8` 转出来编辑，改完再 `iconv -f UTF-8 -t GBK` 转回去。
2. **`iconv` 遇到 GBK 表示不了的字符会当场截断文件**（默认行为，不回滚）。
   踩过一次：按钮文案里的 `⚠`（U+26A0）不在 GBK 里，转出来的文件从那里被切掉了。
   所以**转回去之后一定要检查文件结尾和 `file` 输出**，别直接信 `iconv` 没报错。
   GBK 可用的符号：`※ ▲ ！ ★ ● △ ◆`；不可用：`⚠`（以及绝大多数 emoji）。

### 改 `pull.bat` 时务必注意

1. **必须保存为 CRLF 换行 + 纯 ASCII。**
   - LF 换行会让 cmd.exe 找不到 `call :Label` 的标签，报
     `The system cannot find the batch label specified`。（踩过一次）
   - 文件里有中文会在 cmd 解析时变成乱码，所有提示信息都用英文。
2. **不要退回用 `where bash` 找 bash。** 系统 PATH 里通常只有 `E:\Git\cmd`
   而没有 `Git\usr\bin`，此时 `where bash` 只会返回 `C:\Windows\System32\bash.exe`，
   那是 WSL 启动器，会弹出「正在安装 WSL」的提示。（踩过一次）
   正确做法是从 `git.exe` 反推 Git 根目录再找 `bash.exe`。
3. **测试必须用干净 PATH 模拟 Explorer，不能在 Git Bash 里调 cmd。**
   Git Bash 的 PATH 里含 `Git\usr\bin`，会把上面第 2 条 bug 完全掩盖掉。
   （踩过一次 —— 前两次失败都是同一个原因）

   ```bash
   cmd /c "set PATH=C:\Windows\System32;E:\Git\cmd && gittool\pull.bat --where"
   ```

   期望输出 `bash : E:\Git\bin\bash.exe`（路径里**绝不能**出现 `System32`）。
