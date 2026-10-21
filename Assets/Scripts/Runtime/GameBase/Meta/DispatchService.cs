using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 派遣编队（W2）：选 1~4 位**门客**，出发即**就地**生成局内地图（不切场景、不改 RunMap）。
///
/// ══════════════ 口径 ══════════════
/// * 队伍上限 **4**、最少 **1**；单选/取消；
/// * **不可出战**（重伤 / 信心不足）不可选，并给出原因；
/// * **不显示任何单人收益/风险提示**（用户 U6）；
/// * 出发：写 `SavePayload.runTeam`、`runs++`、`PrototypeMapSpawner.Spawn(1)`、`IsInRun=true`；
///   撤离：`PrototypeMapSpawner.Clear()`、`IsInRun=false`；
/// * 无会话（未读档）也能用 —— 进度读写统一走 `MetaProgressService.Payload`（内存态兜底）。
/// </summary>
public class DispatchService : SingletonBase<DispatchService>
{
    /// <summary>队伍上限。</summary>
    public const int MaxTeamSize = 4;
    /// <summary>队伍最少人数。</summary>
    public const int MinTeamSize = 1;

    /// <summary>局内起始场景序号（原型固定第 1 场景）。</summary>
    public const int StartSceneIndex = 1;

    private readonly List<uint> _selected = new List<uint>();
    private bool _isInRun;

    /// <summary>是否在局内（原型：地图就地生成中）。</summary>
    public bool IsInRun { get { return _isInRun; } }

    /// <summary>当前选中的门客 id（只读视图）。</summary>
    public List<uint> SelectedIds { get { return _selected; } }

    public int SelectedCount { get { return _selected.Count; } }

    /// <summary>可以出发：1~4 人 且 不在局内。</summary>
    public bool CanDepart
    {
        get { return !_isInRun && _selected.Count >= MinTeamSize && _selected.Count <= MaxTeamSize; }
    }

    /// <summary>存档里的本局队伍（出发时写入）。</summary>
    public List<uint> RunTeam
    {
        get
        {
            SavePayload p = MetaProgressService.Instance.Payload;
            if (p.runTeam == null) p.runTeam = new List<uint>();
            return p.runTeam;
        }
    }

    /// <summary>全部门客（UI 列表用；可出战与否另查 `GetUnavailableReason`）。</summary>
    public List<RetainerSave> GetCandidates()
    {
        return RetainerService.Instance.GetAll();
    }

    /// <summary>不可出战的原因；可出战返回 null。</summary>
    public string GetUnavailableReason(RetainerSave retainer)
    {
        if (retainer == null) return "门客不存在";
        RetainerService svc = RetainerService.Instance;
        if (svc.IsDeployable(retainer)) return null;

        switch (svc.GetState(retainer))
        {
            case RetainerService.DeployState.Wounded: return "重伤 " + retainer.woundedRemainSeconds + "s";
            case RetainerService.DeployState.LowConfidence: return "信心不足";
            default: return "不可出战";
        }
    }

    /// <summary>选/取消某门客；超上限或不可出战会被拒绝并给出原因。</summary>
    public bool Toggle(uint id, out string reason)
    {
        reason = null;

        if (_isInRun)
        {
            reason = "已经在局内，先在面板里「撤离」再编队";
            return false;
        }

        if (_selected.Contains(id))
        {
            _selected.Remove(id);
            ChaosLog.Info(LogChannel.Config, "[派遣] 取消选择门客 #" + id + "，当前 " + _selected.Count + "/" + MaxTeamSize);
            return true;
        }

        RetainerSave retainer;
        if (!RetainerService.Instance.TryGet(id, out retainer))
        {
            reason = "门客 #" + id + " 不存在";
            return false;
        }

        string unavailable = GetUnavailableReason(retainer);
        if (!string.IsNullOrEmpty(unavailable))
        {
            reason = "门客 #" + id + " 不可出战（" + unavailable + "）";
            ChaosLog.Warn(LogChannel.Config, "[派遣] " + reason);
            return false;
        }

        if (_selected.Count >= MaxTeamSize)
        {
            reason = "队伍已满（最多 " + MaxTeamSize + " 位）";
            ChaosLog.Warn(LogChannel.Config, "[派遣] " + reason);
            return false;
        }

        _selected.Add(id);
        ChaosLog.Info(LogChannel.Config, "[派遣] 选中门客 #" + id + "，当前 " + _selected.Count + "/" + MaxTeamSize);
        return true;
    }

    public void ResetSelection()
    {
        _selected.Clear();
    }

    /// <summary>
    /// **出发**：写 `runTeam` → `runs++` → **就地**生成第 1 场景 → `IsInRun = true`。
    /// 不切场景、不走 LoadingScene、不改 RunMap。
    /// </summary>
    public bool Depart(out string reason)
    {
        reason = null;

        ChaosLog.Warn(LogChannel.UI, "[诊断] Depart 进入 frameCount=" + Time.frameCount + " id=" + GetHashCode() + " IsInRun=" + _isInRun + " 选中=" + _selected.Count);

        //W3.1k：幂等 —— 未撤离则先自动撤离再继续（防重复生成）
        if (_isInRun)
        {
            ChaosLog.Warn(LogChannel.UI, "[派遣] 检测到未撤离的局内状态，先自动撤离");
            string autoReason;
            ReturnToMeta(out autoReason);
        }
        if (_selected.Count < MinTeamSize) { reason = "至少选择 " + MinTeamSize + " 位门客"; return false; }
        if (_selected.Count > MaxTeamSize) { reason = "队伍超过上限 " + MaxTeamSize; return false; }

        //出发前再校验一次（可能在面板里待久了，状态变了）
        RetainerService svc = RetainerService.Instance;
        for (int i = 0; i < _selected.Count; i++)
        {
            RetainerSave retainer;
            if (!svc.TryGet(_selected[i], out retainer))
            {
                reason = "门客 #" + _selected[i] + " 已不存在";
                return false;
            }
            string unavailable = GetUnavailableReason(retainer);
            if (!string.IsNullOrEmpty(unavailable))
            {
                reason = "门客 #" + _selected[i] + " 已不可出战（" + unavailable + "）";
                return false;
            }
        }

        SavePayload payload = MetaProgressService.Instance.Payload;
        payload.runTeam = new List<uint>(_selected);
        payload.runs++;

        PlayerSpawner.CaptureOutsideCameraState();   //必须在生图前记局外相机尺寸
        //W3.1a：统一走 RunEncounter.EnterScene（清旧 → 生图 → 出生点 → 玩家 → 边界 → 敌人）
        if (!RunEncounter.EnterScene(StartSceneIndex, _selected))
        {
            reason = "就地生成局内场景失败（看 Console）";
            return false;
        }

        _isInRun = true;
        ChaosLog.Success(LogChannel.Config,
            "[派遣] 出发：队伍 [" + string.Join(",", System.Array.ConvertAll(_selected.ToArray(), x => x.ToString())) + "]"
            + "（" + _selected.Count + " 人），runs=" + payload.runs
            + "，就地生成场景 " + StartSceneIndex + "（容器 PrototypeRunMap，不切场景）");
        return true;
    }

    /// <summary>**撤离 / 返回局外**：清掉就地生成的地图，`IsInRun = false`。</summary>
    public bool ReturnToMeta(out string reason)
    {
        RunDepthHud.DisableAndDestroy();   //W4e：关闸 + 销毁
        reason = null;
        if (!_isInRun)
        {
            reason = "当前不在局内";
            return false;
        }

        RunEncounter.Clear();
        PlayerSpawner.Clear();

        //恢复进局前隐藏的局外 UI（集中在这里做：GM 的撤离按钮也走这条路）
        if (UIManager.Instance != null) UIManager.Instance.RestoreAfterGameplay();

        PrototypeMapSpawner.Clear();
        _isInRun = false;

        ChaosLog.Info(LogChannel.Config, "[派遣] 已撤离局内：地图已清除，IsInRun=false");

        //W5.1：**回局外是剧情事件检查的唯一正式时机**（16_剧情事件.md §3.3 C1）。
        //只在这里调一次 —— 不做 Update/定时轮询（C4 禁止每帧）；有满足的事件才弹面板。
        EventService.Instance.CheckAndShowOnReturnToMeta();
        return true;
    }

    public string DumpState()
    {
        SavePayload p = MetaProgressService.Instance.Payload;
        var sb = new System.Text.StringBuilder();
        sb.Append("[派遣] IsInRun=").Append(_isInRun)
          .Append("  已选 ").Append(_selected.Count).Append("/").Append(MaxTeamSize)
          .Append("  CanDepart=").Append(CanDepart)
          .Append("  选中=[");
        for (int i = 0; i < _selected.Count; i++) sb.Append(i == 0 ? "" : ",").Append(_selected[i]);
        sb.Append("]  runTeam=[");
        List<uint> team = RunTeam;
        for (int i = 0; i < team.Count; i++) sb.Append(i == 0 ? "" : ",").Append(team[i]);
        sb.Append("]  runs=").Append(p.runs)
          .Append("  地图已生成=").Append(PrototypeMapSpawner.IsSpawned);
        return sb.ToString();
    }
}
