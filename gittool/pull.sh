#!/usr/bin/env bash
# =============================================================================
#  gittool / pull.sh  ---  Unity 项目安全拉取助手 (macOS / Linux / Git Bash)
#
#  设计目标：
#    1. 只动 Git 跟踪范围内的文件，绝不触碰 Library / Temp / obj / Logs。
#    2. 拉取前先 fetch + 预览，把变更按「对 Unity 的影响」分类，确认后才真正 pull。
#    3. 默认使用 --ff-only，绝不产生意外的 merge commit，也绝不执行 git clean。
#    4. 本地改动挡住拉取时，默认停下来问用户，而不是自作主张丢弃；
#       只有显式 --discard 才会 git reset --hard（且只丢已跟踪的改动）。
#
#  用法：
#    ./pull.sh              预览并交互确认后拉取
#    ./pull.sh --dry-run    只预览，不拉取
#    ./pull.sh --yes        跳过确认直接拉取
#    ./pull.sh --discard    丢弃本地已跟踪的改动后再拉取（不可恢复！）
#    ./pull.sh --help       查看帮助
# =============================================================================

set -uo pipefail

# 仓库里有中文文件名（如 Assets/Learn/Excel配置表工具使用说明.docx）。
# Git 默认把非 ASCII 路径转义成八进制（"\346\226\207"），会让下面的分类逻辑完全失效。
# 用环境变量全局关掉，避免逐个命令加 -c 参数。（Git >= 2.31）
export GIT_CONFIG_COUNT=1
export GIT_CONFIG_KEY_0=core.quotepath
export GIT_CONFIG_VALUE_0=false

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT" || exit 1

# ---------------------------------------------------------------- 命令行参数
REMOTE="origin"
ASSUME_YES=0
DRY_RUN=0
DISCARD=0

usage() {
    cat <<'EOF'
gittool / pull.sh --- Unity 项目安全拉取助手 (macOS / Linux / Git Bash)

设计目标:
  1. 只动 Git 跟踪范围内的文件，绝不触碰 Library / Temp / obj / Logs。
  2. 拉取前先 fetch + 预览，把变更按「对 Unity 的影响」分类，确认后才真正 pull。
  3. 默认使用 --ff-only，绝不产生意外的 merge commit，也绝不执行 git clean。
  4. 本地改动挡住拉取时，默认停下来问用户，而不是自作主张丢弃；
     只有显式 --discard 才会 git reset --hard（且只丢已跟踪的改动）。

用法:
  ./pull.sh               预览并交互确认后拉取
  ./pull.sh --dry-run     只预览，不拉取
  ./pull.sh --yes         跳过确认直接拉取
  ./pull.sh --discard     丢弃本地已跟踪的改动后再拉取（不可恢复！）
  ./pull.sh --remote <名> 指定远端（默认 origin）
  ./pull.sh --help        显示本帮助

关于 --discard:
  它执行 git reset --hard HEAD，只丢弃「已被 Git 跟踪」的文件的改动。
  这些改动会永久消失，本工具不做任何备份。

  它绝不会执行 git clean —— 未跟踪的新文件（比如刚在 Unity 里做的、还没提交的
  脚本和资源）会原样保留。这条边界是硬的，任何情况下都不突破。

  它也不会丢弃未推送的提交。如果本地领先远端，脚本会明确拒绝并列出那些提交，
  由你自己决定要不要 git reset --hard <远端分支>。

  想先看看会丢什么，用：./pull.sh --discard --dry-run
EOF
}

while [ $# -gt 0 ]; do
    case "$1" in
        -y|--yes)      ASSUME_YES=1 ;;
        -n|--dry-run)  DRY_RUN=1 ;;
        -d|--discard)  DISCARD=1 ;;
        --remote)      shift; REMOTE="${1:-origin}" ;;
        -h|--help)     usage; exit 0 ;;
        *) echo "未知参数: $1（用 --help 查看用法）"; exit 1 ;;
    esac
    shift
done

# ---------------------------------------------------------------- 输出配色
if [ -t 1 ]; then
    C_RESET=$'\033[0m'; C_BOLD=$'\033[1m'; C_DIM=$'\033[2m'
    C_RED=$'\033[31m';  C_GREEN=$'\033[32m'; C_YELLOW=$'\033[33m'; C_CYAN=$'\033[36m'
else
    C_RESET=''; C_BOLD=''; C_DIM=''; C_RED=''; C_GREEN=''; C_YELLOW=''; C_CYAN=''
fi

title() { printf '\n%s%s== %s ==%s\n' "$C_BOLD" "$C_CYAN" "$1" "$C_RESET"; }
ok()    { printf '%s  [OK]%s %s\n'   "$C_GREEN"  "$C_RESET" "$1"; }
warn()  { printf '%s  [!]%s  %s\n'   "$C_YELLOW" "$C_RESET" "$1"; }
err()   { printf '%s  [x]%s  %s\n'   "$C_RED"    "$C_RESET" "$1"; }
info()  { printf '       %s\n' "$1"; }

die() { err "$1"; exit 1; }

# =============================================================================
#  路径分类：把变更文件映射到「类别 + 对 Unity 的影响等级」
#  等级 3 = 可能触发全量 Reimport / 需要升级编辑器（最费时间）
#  等级 2 = 触发脚本重编译（增量，通常较快）
#  等级 1 = 局部资源 Reimport（快）
#  等级 0 = 不影响 Unity 导入流程
# =============================================================================
classify_path() {
    case "$1" in
        ProjectSettings/ProjectVersion.txt)                        printf '版本升级|3' ;;
        ProjectSettings/*)                                         printf '工程配置|3' ;;
        Packages/*)                                                printf '包依赖|3' ;;
        .vs/*|obj/*|UserSettings/*|Logs/*|Library/*|Temp/*)        printf '本地生成物|0' ;;
        *.csproj|*.sln)                                            printf 'IDE工程|0' ;;
        Assets/*.dll)                                              printf '插件程序集|2' ;;
        *.cs)                                                      printf 'C#脚本|2' ;;
        *.meta)                                                    printf '资源元数据|1' ;;
        Assets/*)                                                  printf '资源|1' ;;
        *)                                                         printf '其他|1' ;;
    esac
}

# 类别 -> 一句影响说明
impact_of() {
    case "$1" in
        版本升级)     printf '需要安装对应 Unity 版本，否则无法打开工程' ;;
        工程配置)     printf '大概率触发全量 Reimport（最耗时，建议先关闭 Unity）' ;;
        包依赖)       printf '需要重新解析包，可能触发全量 Reimport' ;;
        插件程序集)   printf '重编译所有引用该程序集的脚本' ;;
        C#脚本)       printf '增量重编译（通常几秒~几十秒）' ;;
        资源元数据)   printf '可能引起资源引用重连' ;;
        资源)         printf '局部 Reimport' ;;
        IDE工程)      printf '仅 IDE 工程文件，Unity 会自行重生成' ;;
        本地生成物)   printf '本地缓存，不该进版本库' ;;
        *)            printf '—' ;;
    esac
}

level_color() {
    case "$1" in
        3) printf '%s' "$C_RED" ;;
        2) printf '%s' "$C_YELLOW" ;;
        0) printf '%s' "$C_DIM" ;;
        *) printf '%s' "$C_RESET" ;;
    esac
}

# 终端里的显示宽度：ASCII 记 1 列，UTF-8 汉字（3 字节）记 2 列。
# bash 的 ${#var} 与 printf %-Ns 都按字节算，直接用会导致中文列错位。
disp_width() {
    [ -n "$1" ] || { printf '0'; return; }
    printf '%s' "$1" | LC_ALL=C awk '{
        total = length($0); ascii = 0
        for (i = 1; i <= total; i++) if (substr($0, i, 1) ~ /^[\001-\177]$/) ascii++
        printf "%d", ascii + (total - ascii) / 3 * 2
    }'
}

# 按显示宽度右侧补空格到指定列宽
pad_to() {
    w="$(disp_width "$1")"
    printf '%s' "$1"
    n=$(( $2 - w ))
    while [ "$n" -gt 0 ]; do printf ' '; n=$((n - 1)); done
}

# =============================================================================
#  0. 环境自检
# =============================================================================
title "环境自检"

command -v git >/dev/null 2>&1 || die "找不到 git，请先安装 Git 并加入 PATH"
git rev-parse --git-dir >/dev/null 2>&1 || die "当前目录不是 Git 仓库: $REPO_ROOT"
ok "仓库根目录: $REPO_ROOT"
info "git 版本: $(git --version)"

# Unity 是否正在运行 —— 它会给 Assets/ 与 Library/ 加锁，拉取时机不对会互相打架
UNITY_RUNNING=0
if [ -f "$REPO_ROOT/Temp/UnityLockfile" ]; then
    UNITY_RUNNING=1
fi
if [ "$UNITY_RUNNING" -eq 1 ]; then
    warn "检测到 Unity 正在运行（存在 Temp/UnityLockfile）"
    info "建议：先关闭 Unity 再拉取，否则可能出现"
    info "      · 文件占用导致拉取中断"
    info "      · Unity 与 Git 同时改写 Assets，产生难以排查的导入错误"
else
    ok "Unity 当前未运行"
fi

# 未完成的 merge / rebase，必须先解决再谈拉取
GITDIR="$(git rev-parse --git-dir)"
GITDIR="$(cd "$GITDIR" && pwd)"
IN_PROGRESS=""
[ -d "$GITDIR/rebase-merge" ]  && IN_PROGRESS="rebase"
[ -d "$GITDIR/rebase-apply" ]  && IN_PROGRESS="rebase"
[ -f "$GITDIR/MERGE_HEAD" ]     && IN_PROGRESS="merge"
[ -f "$GITDIR/CHERRY_PICK_HEAD" ] && IN_PROGRESS="cherry-pick"
if [ -n "$IN_PROGRESS" ]; then
    die "检测到未完成的 $IN_PROGRESS 操作，请先解决冲突（或 git $IN_PROGRESS --abort）后再运行本工具"
fi
ok "没有未完成的 merge / rebase"

# =============================================================================
#  1. 分支与 upstream
# =============================================================================
title "分支状态"

CURRENT_BRANCH="$(git symbolic-ref --short -q HEAD || echo '')"
if [ -z "$CURRENT_BRANCH" ]; then
    die "当前处于 detached HEAD 状态，请先 git checkout 到一个分支"
fi
ok "当前分支: $CURRENT_BRANCH"

UPSTREAM="$(git for-each-ref --format='%(upstream:short)' "refs/heads/$CURRENT_BRANCH")"
# fetch --prune 之后上游可能已被删除，此时 for-each-ref 仍会返回名字，需验证引用真实存在
if [ -n "$UPSTREAM" ] && ! git rev-parse --verify --quiet "refs/remotes/$UPSTREAM" >/dev/null; then
    warn "配置的上游 '$UPSTREAM' 已不存在（远端分支可能被删除）"
    UPSTREAM=""
fi

if [ -z "$UPSTREAM" ]; then
    warn "分支 '$CURRENT_BRANCH' 没有配置 upstream，git pull 会直接失败"

    SUGGEST_UPSTREAM=""

    # 1) 先只查本地已记录的远端分支 —— 不联网，最稳
    for candidate in "$CURRENT_BRANCH" main master; do
        if git show-ref --verify --quiet "refs/remotes/$REMOTE/$candidate"; then
            SUGGEST_UPSTREAM="$REMOTE/$candidate"
            break
        fi
    done

    # 2) 本地完全没记录，才联网问远端的默认分支
    if [ -z "$SUGGEST_UPSTREAM" ]; then
        REMOTE_SYMBOLIC="$(git ls-remote --symref "$REMOTE" HEAD 2>/dev/null)"
        DEFAULT_BRANCH="$(printf '%s\n' "$REMOTE_SYMBOLIC" \
            | awk '/^ref:/ {sub("refs/heads/", "", $2); print $2; exit}')"
        if [ -n "$DEFAULT_BRANCH" ]; then
            SUGGEST_UPSTREAM="$REMOTE/$DEFAULT_BRANCH"
        elif [ -z "$REMOTE_SYMBOLIC" ]; then
            die "无法连接远端 '$REMOTE'（网络或凭据问题），请检查网络后重试"
        else
            die "无法推断 '$CURRENT_BRANCH' 应跟踪哪个远端分支，请手动 git branch --set-upstream-to=<远端分支>"
        fi
    fi

    info "建议跟踪: $SUGGEST_UPSTREAM"
    if [ "$ASSUME_YES" -eq 1 ] || [ "$DRY_RUN" -eq 1 ]; then
        info "（--yes / --dry-run 模式，跳过设置 upstream）"
    else
        printf '       是否将 upstream 设置为 %s ？[Y/n] ' "$SUGGEST_UPSTREAM"
        read -r reply
        case "$reply" in
            [nN]*) die "已取消" ;;
            *)
                git branch --set-upstream-to="$SUGGEST_UPSTREAM" "$CURRENT_BRANCH" >/dev/null 2>&1 \
                    || die "设置 upstream 失败"
                UPSTREAM="$SUGGEST_UPSTREAM"
                ok "upstream 已设置为 $UPSTREAM"
                ;;
        esac
    fi
else
    ok "跟踪分支: $UPSTREAM"
fi

# =============================================================================
#  2. 工作区是否干净
# =============================================================================
title "工作区状态"

# --untracked-files=no 是刻意的：未跟踪的文件不在本工具的处置范围内，
# 这里只统计「已被 Git 跟踪」的改动。配合的硬性约束是：全程不执行 git clean。
DIRTY_LIST="$(git status --porcelain --untracked-files=no)"
DIRTY_COUNT=0
if [ -z "$DIRTY_LIST" ]; then
    ok "没有未提交的改动"
else
    DIRTY_COUNT="$(printf '%s\n' "$DIRTY_LIST" | wc -l | tr -d ' ')"
    if [ "$DISCARD" -eq 1 ]; then
        warn "有 $DIRTY_COUNT 个已跟踪文件被改动，--discard 下它们将被丢弃："
    else
        warn "有 $DIRTY_COUNT 个文件未提交："
    fi
    printf '%s\n' "$DIRTY_LIST" | head -20 | while IFS= read -r line; do
        info "${line:0:1}${line:1:1}  ${line:3}"
    done
    [ "$DIRTY_COUNT" -gt 20 ] && info "... 以及另外 $((DIRTY_COUNT - 20)) 个"

    if [ "$DISCARD" -eq 1 ]; then
        info "这些改动会永久消失，本工具不做备份。"
        info "未跟踪的新文件（还没 git add 的脚本、资源）不在其中，不会被删除。"
    else
        info "这些改动如果与远端更新撞车，--ff-only 拉取会被拒绝。"
        info "想直接丢弃它们再拉取，用：$0 --discard（不可恢复）"
    fi
fi

# =============================================================================
#  3. 拉取远端最新（只改 refs，不动工作区）
# =============================================================================
title "获取远端更新"
info "git fetch $REMOTE ..."
if ! git fetch --prune "$REMOTE"; then
    die "fetch 失败（网络或凭据问题）"
fi
ok "fetch 完成"

if [ -z "$UPSTREAM" ]; then
    warn "没有 upstream，无法比较差异（--yes/--dry-run 模式下跳过了 upstream 设置）"
    exit 0
fi

# =============================================================================
#  4. 预览差异并分类
# =============================================================================
title "更新预览"

# 注意：这里绝不能把失败降级成 0，否则工具会谎报「已是最新版本」而实际没更新
if ! BEHIND="$(git rev-list --count "HEAD..$UPSTREAM" 2>/dev/null)"; then
    die "无法计算与 '$UPSTREAM' 的差异（该引用可能不存在），请先 git fetch $REMOTE 后重试"
fi
if ! AHEAD="$(git rev-list --count "$UPSTREAM..HEAD" 2>/dev/null)"; then
    die "无法计算与 '$UPSTREAM' 的差异，请先 git fetch $REMOTE 后重试"
fi

# 注意：这里不能直接 exit。「没有更新可拉」和「不用丢弃本地改动」是两件事，
# 把它们划等号会导致最令人困惑的结果：用户明确要求放弃本地更改，工具却什么都没做。
# 所以 --discard 模式下即使落后 0 个提交也要继续走下去，进第 5 节丢弃改动。
if [ "$BEHIND" -eq 0 ]; then
    if [ "$DISCARD" -ne 1 ]; then
        ok "已经是最新版本（落后 0 个提交）"
        [ "$AHEAD" -gt 0 ] && info "本地领先远端 $AHEAD 个提交（记得 push）"
        [ "$DIRTY_COUNT" -gt 0 ] && info "本地那 $DIRTY_COUNT 个改动没有被丢弃（本工具默认不动它们）"
        exit 0
    fi

    ok "已经是最新版本（落后 0 个提交），没有需要拉取的更新"
    info "--discard 模式下仍会丢弃本地已跟踪的改动"
    [ "$AHEAD" -gt 0 ] && info "（本地领先远端 $AHEAD 个提交，丢弃改动不会影响它们）"
else
    info "落后 $UPSTREAM $BEHIND 个提交"
    [ "$AHEAD" -gt 0 ] && warn "本地同时领先 $AHEAD 个提交，--ff-only 会失败，需要 rebase 或 merge"

    # --discard 只解决「未提交的改动」，解决不了「本地领先」。后者要丢的是提交历史，
    # 那是另一个量级的破坏，本工具不替用户决定 —— 明确拒绝并给出确切命令。
    # 只在「确实有东西要拉」时才拒绝：落后 0 时丢弃改动本身没问题。
    if [ "$DISCARD" -eq 1 ] && [ "$AHEAD" -gt 0 ]; then
        title "无法自动处理：本地领先远端"
        err "本地领先 $UPSTREAM $AHEAD 个提交，--discard 解决不了这种情况"
        info "git reset --hard HEAD 只丢未提交的改动，丢不掉这些提交，"
        info "重置后 HEAD 仍在远端前面，--ff-only 依旧会失败。"
        printf '\n'
        info "将被放弃的提交（如果你决定那么做）："
        git log --oneline --no-decorate "$UPSTREAM..HEAD" 2>/dev/null | sed 's/^/         /'
        printf '\n'
        info "确认这些提交可以永久丢弃后，手动执行："
        info "  git reset --hard $UPSTREAM"
        info "本工具不会替你执行这一步 —— 那会永久丢失提交历史。"
        info "如果不想丢，先把它们 rebase 到远端之上：git pull --rebase"
        exit 1
    fi
fi

# 整个预览打包成函数，这样「没有更新可拉」时可以整段跳过，而不是打印一堆空表。
# 函数与调用同在一个 shell 里，所以 LEVEL3_COUNT / TOTAL_FILES 的赋值对外可见
# （注意块内用的是 `done < file` 重定向而非管道，没有子 shell 问题）。
show_update_preview() {
printf '\n%s' "$C_BOLD"
pad_to "类别" 14; printf ' %s ' "数量"; printf '影响'
printf '%s\n' "$C_RESET"
printf '%s\n' "---------------------------------------------------------------"

# 分类结果落盘为：类别 <TAB> 等级 <TAB> 路径 <TAB> 状态
DIFF_FILE="$(mktemp)"
CLS_FILE="${DIFF_FILE}.cls"
git diff --name-status "HEAD..$UPSTREAM" > "$DIFF_FILE"

LEVEL3_COUNT=0

while IFS=$'\t' read -r status path rest; do
    [ -z "${status:-}" ] && continue
    [ -z "${path:-}" ] && continue
    # 重命名/复制时第三列才是真实目标路径
    case "$status" in
        R*|C*) [ -n "${rest:-}" ] && path="$rest" ;;
    esac

    cls="$(classify_path "$path")"
    name="${cls%%|*}"; lvl="${cls##*|}"
    [ "$lvl" -eq 3 ] && LEVEL3_COUNT=$((LEVEL3_COUNT + 1))
    printf '%s\t%s\t%s\t%s\n' "$name" "$lvl" "$path" "$status" >> "$CLS_FILE"
done < "$DIFF_FILE"

TOTAL_FILES=0
if [ -s "$CLS_FILE" ]; then
    TOTAL_FILES="$(wc -l < "$CLS_FILE" | tr -d ' ')"

    # 按「类别 + 等级」聚合，SUBSEP 做复合键，避免手工切分字符串
    awk -F'\t' '
        { key = $1 SUBSEP $2; cnt[key]++ }
        END {
            for (k in cnt) {
                split(k, a, SUBSEP)
                printf "%s\t%d\t%s\n", a[2], cnt[k], a[1]   # 等级 <TAB> 数量 <TAB> 类别
            }
        }' "$CLS_FILE" \
    | sort -t$'\t' -k1,1nr -k2,2nr \
    | while IFS=$'\t' read -r lvl cnt name; do
        printf '%s' "$(level_color "$lvl")"
        pad_to "$name" 14
        printf ' %-5s %s%s\n' "$cnt" "$(impact_of "$name")" "$C_RESET"
    done
else
    info "（没有文件级差异）"
fi

printf '%s\n' "---------------------------------------------------------------"

# 详细文件清单（最多 60 条）
printf '\n%s详细变更文件：%s\n' "$C_BOLD" "$C_RESET"
head -60 "$CLS_FILE" 2>/dev/null | while IFS=$'\t' read -r name lvl path status; do
    case "$status" in
        A*) st="新增" ;; D*) st="删除" ;; M*) st="修改" ;;
        R*) st="重命名" ;; C*) st="复制" ;; *) st="$status" ;;
    esac
    printf '  %s%-4s%s %s\n' "$(level_color "$lvl")" "$st" "$C_RESET" "$path"
done
[ "$TOTAL_FILES" -gt 60 ] && info "... 以及另外 $((TOTAL_FILES - 60)) 个文件"

# ---- 风险总结 ----
title "影响评估"
if [ "$LEVEL3_COUNT" -gt 0 ]; then
    warn "本次更新包含 $LEVEL3_COUNT 个高影响文件，可能触发全量 Reimport"
    info "这类更新后首次打开 Unity 会比较慢，属于正常现象，不要删 Library 试图加速。"
else
    ok "没有高影响文件，预计本次更新是增量重编译，耗时较短"
fi

rm -f "$DIFF_FILE" "$CLS_FILE"
}

# 有更新可拉时才值得看分类表；落后 0 个提交时表和清单都是空的
LEVEL3_COUNT=0
TOTAL_FILES=0
[ "$BEHIND" -gt 0 ] && show_update_preview

# =============================================================================
#  5. 放弃本地已跟踪的改动（仅 --discard）
# =============================================================================
# 独立成节，且不依赖「有没有更新可拉」：
# 用户点了「放弃本地更改」，期待的就是改动消失。如果因为落后 0 个提交就什么都不做，
# 是最令人困惑的结果 —— 这条曾经是 bug，别改回去。
if [ "$DISCARD" -eq 1 ]; then
    title "放弃本地更改"

    if [ "$DIRTY_COUNT" -eq 0 ]; then
        ok "工作区干净，没有需要丢弃的已跟踪改动"
    elif [ "$DRY_RUN" -eq 1 ]; then
        warn "--dry-run：上面列出的 $DIRTY_COUNT 个已跟踪改动会被丢弃"
        info "（现在什么都没做，去掉 --dry-run 才会真的丢）"
    else
        info "即将丢弃这 $DIRTY_COUNT 个已跟踪文件的改动（不可恢复）："
        printf '%s\n' "$DIRTY_LIST" | head -20 | while IFS= read -r line; do
            info "${line:0:1}${line:1:1}  ${line:3}"
        done
        [ "$DIRTY_COUNT" -gt 20 ] && info "... 以及另外 $((DIRTY_COUNT - 20)) 个"
        info "未跟踪的新文件不在其中，不会被删除。"

        if [ "$ASSUME_YES" -ne 1 ]; then
            printf '       确认丢弃这 %s 个改动（不可恢复）？[y/N] ' "$DIRTY_COUNT"
            read -r reply
            case "$reply" in
                [yY]*) ;;
                *) info "已取消，未修改任何文件"; exit 0 ;;
            esac
        fi

        if git reset --hard HEAD >/dev/null 2>&1; then
            ok "已丢弃 $DIRTY_COUNT 个已跟踪文件的改动（未跟踪文件未受影响）"
            DIRTY_COUNT=0
        else
            die "git reset --hard 失败，已中止"
        fi
    fi
fi

if [ "$DRY_RUN" -eq 1 ]; then
    title "演练结束"
    info "--dry-run 模式，未修改任何文件"
    exit 0
fi

# =============================================================================
#  6. 确认并拉取
# =============================================================================
if [ "$BEHIND" -eq 0 ]; then
    title "完成"
    if [ "$DISCARD" -eq 1 ]; then
        ok "本地改动已丢弃；远端本来就没有新提交，无需拉取"
    else
        ok "已经是最新版本"
    fi
    exit 0
fi

title "执行拉取"

# ---- 拉取（丢弃已经在第 5 节做完了） --------------------------------------
if [ "$ASSUME_YES" -ne 1 ]; then
    printf '       确认拉取 %s 个提交到 %s ？[y/N] ' "$BEHIND" "$CURRENT_BRANCH"
    read -r reply
    case "$reply" in
        [yY]*) ;;
        *) info "已取消，未修改任何文件"; exit 0 ;;
    esac
fi

if git pull --ff-only; then
    ok "拉取成功（fast-forward）"
elif git pull --ff-only "$REMOTE" "$CURRENT_BRANCH" 2>/dev/null; then
    ok "拉取成功（fast-forward）"
else
    err "fast-forward 拉取失败"
    if [ "$DISCARD" -eq 1 ]; then
        info "本地未提交的改动已经在上一步丢弃，仍然失败说明问题不在工作区。"
        info "多半是本地有未推送的提交（本工具不会替你丢弃提交历史）。"
        info "查看：git log --oneline $UPSTREAM..HEAD"
    else
        info "常见原因：本地有提交或未提交改动与远端冲突。"
        info "可选处理："
        info "  git pull --rebase        # 把本地提交挪到远端之后（推荐）"
        info "  git stash && git pull    # 临时收起未提交改动"
        info "  $0 --discard             # 丢弃本地改动再拉取（不可恢复）"
        info "本工具不会自动帮你 rebase 或 stash，以免覆盖你的工作。"
    fi
    exit 1
fi

# =============================================================================
#  7. 拉取后提示
# =============================================================================
title "完成"
ok "工作区已更新到 $UPSTREAM"

if [ "$LEVEL3_COUNT" -gt 0 ]; then
    info "本次含高影响变更，下次打开 Unity 会做全量/大范围 Reimport。"
    info "建议：直接打开 Unity 让它自己导入完，中途不要关掉，也不要手动删 Library。"
fi
if [ "$UNITY_RUNNING" -eq 1 ]; then
    warn "Unity 之前处于运行状态，请回到 Unity 让它重新聚焦以触发刷新"
fi
if [ "$DISCARD" -eq 1 ]; then
    info "本次用了 --discard：代码回退可以 git reflog 找回拉取前的提交，"
    info "但被丢弃的未提交改动不在 reflog 里，无法恢复。"
else
    info "如需回退：git reflog 找到拉取前的提交，再 git reset --hard <提交>"
fi
