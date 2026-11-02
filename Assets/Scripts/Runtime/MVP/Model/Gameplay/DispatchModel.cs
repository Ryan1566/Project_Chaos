using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;
using LocalizationSystem;
using RunMap;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>门客列表的三种筛选口径。</summary>
    public enum DispatchFilter
    {
        All = 0,
        Available = 1,
        Team = 2
    }

    /// <summary>左侧门客卡片需要的只读数据。</summary>
    public sealed class DispatchCardData
    {
        public uint Id;
        public uint Quality;
        public string Name;
        public string QualityText;
        public int Confidence;
        public string Status;
        public string UnavailableReason;
        public bool Available;
        public bool Wounded;
        public bool InTeam;
        public bool Controlled;
        public int TeamIndex;
    }

    /// <summary>中部四个编队槽之一的只读数据。</summary>
    public sealed class DispatchSlotData
    {
        public int Index;
        public bool Occupied;
        public uint Id;
        public bool Controlled;
        public bool Available;
        public string Name;
        public string QualityText;
        public string Status;
    }

    /// <summary>右侧角色详情与预计派遣属性。</summary>
    public sealed class DispatchRoleData
    {
        public uint Id;
        public bool HasRole;
        public string Name;
        public string Profession;
        public string QualityText;
        public int Confidence;
        public string Status;
        public string WoundTime;
        public bool Wounded;
        public bool Available;
        public string UnavailableReason;
        public bool InTeam;
        public bool Controlled;
        public int TeamIndex;
        public bool CanAdd;
        public bool CanRemove;
        public bool CanSetControlled;
        public string JoinReason;
        public int Hp;
        public int Attack;
        public float Speed;
        public int DefenseReduction;
        public Sprite Portrait;
        public SkeletonDataAsset SpineAsset;
        public Material SpineMaterial;
        public string PreviewSkin;
        public bool HasSpine;
    }

    /// <summary>一次渲染所需的完整只读快照。</summary>
    public sealed class DispatchSnapshot
    {
        public DispatchFilter Filter;
        public string Search;
        public uint ResolvedViewId;
        public DispatchCardData[] Cards;
        public DispatchSlotData[] Slots;
        public DispatchRoleData Detail;
        public int TotalRetainers;
        public int AvailableCount;
        public int TeamCount;
        public bool HasSession;
        public bool IsInRun;
        public bool IsTransitioning;
        public bool CanEdit;
        public bool CanDepart;
        public string DepartReason;
        public uint Era;
        public uint Depth;
        public uint DepthCap;
    }

    /// <summary>
    /// 派遣界面只读适配层：集中读取门客、编队、进度和角色预计属性；
    /// 不写存档，不执行命令，不改变任何服务状态。
    /// </summary>
    public sealed class DispatchModel
    {
        private const int TeamSize = 4;
        private const int DefaultHp = 100;
        private const int DefaultPlayerAttack = 10;
        private const int DefaultAiAttack = 6;
        private const float DefaultSpeed = 6f;

        private static bool _assetsLoaded;
        private static PlayerHealth _playerHealth;
        private static PlayerAttack _playerAttack;
        private static PlayerLocomotion _playerLocomotion;
        private static RunCharacterPresentation _playerPresentation;
        private static SkeletonDataAsset _playerSpine;
        private static Material _spineMaterial;
        private static bool _allySpeedLoaded;
        private static float _allySpeed = DefaultSpeed;

        /// <summary>读取一帧界面快照。viewId 失效时自动回退到队首或首位门客。</summary>
        public DispatchSnapshot Read(DispatchFilter filter, string search, uint viewId)
        {
            var result = new DispatchSnapshot();
            var dispatch = DispatchService.Instance;
            var retainers = RetainerService.Instance;
            var progress = MetaProgressService.Instance;

            result.Filter = filter;
            result.Search = search ?? string.Empty;
            result.IsInRun = dispatch.IsInRun;
            result.IsTransitioning = RunSceneFlow.IsTransitioning;
            result.HasSession = GameSession.Instance.HasSession;
            result.CanEdit = result.HasSession && !result.IsInRun && !result.IsTransitioning;
            result.Era = progress.Era;
            result.Depth = progress.Depth;
            result.DepthCap = progress.DepthCap;

            List<RetainerSave> all = dispatch.GetCandidates();
            List<uint> team = new List<uint>(result.IsInRun ? dispatch.RunTeam : dispatch.SelectedIds);

            var teamIndex = new Dictionary<uint, int>();
            for (int i = 0; i < team.Count && i < TeamSize; i++)
            {
                if (!teamIndex.ContainsKey(team[i])) teamIndex.Add(team[i], i);
            }

            var cards = new List<DispatchCardData>();
            int available = 0;
            int total = 0;
            if (all != null)
            {
                for (int i = 0; i < all.Count; i++)
                {
                    RetainerSave retainer = all[i];
                    if (retainer == null) continue;
                    total++;

                    RetainerTable_RetainerConfig definition = retainers.GetDefinition(retainer.protoId);
                    bool canDeploy = retainers.IsDeployable(retainer);
                    if (canDeploy) available++;

                    int index;
                    bool inTeam = teamIndex.TryGetValue(retainer.id, out index);
                    var card = new DispatchCardData
                    {
                        Id = retainer.id,
                        Quality = retainer.quality,
                        Name = NameOf(retainer, definition),
                        QualityText = QualityText(retainer.quality),
                        Confidence = retainer.confidence,
                        Status = StatusText(retainer, retainers),
                        UnavailableReason = dispatch.GetUnavailableReason(retainer),
                        Available = canDeploy,
                        Wounded = retainer.wounded,
                        InTeam = inTeam,
                        Controlled = inTeam && index == 0,
                        TeamIndex = inTeam ? index + 1 : 0
                    };

                    if (MatchesFilter(card, filter) && MatchesSearch(card.Name, result.Search))
                        cards.Add(card);
                }
            }

            result.TotalRetainers = total;
            result.AvailableCount = available;
            result.TeamCount = team.Count;
            result.Cards = cards.ToArray();
            result.Slots = BuildSlots(team, retainers);
            result.ResolvedViewId = ResolveViewId(viewId, all, team);
            result.Detail = BuildRole(result, result.ResolvedViewId, all, team, retainers, dispatch);

            bool canDepart = dispatch.CanDepartWithReason(out string reason);
            result.CanDepart = canDepart && !result.IsInRun && !result.IsTransitioning;
            if (result.IsInRun)
                result.DepartReason = GameplayText.Text("ui_dispatch_reason_inrun",
                    "局内只读：当前显示本局实际 RunTeam。可按规则撤离返回局外。");
            else if (result.IsTransitioning)
                result.DepartReason = string.IsNullOrEmpty(reason)
                    ? GameplayText.Text("ui_dispatch_reason_transition", "场景切换中，请稍候。") : reason;
            else if (result.CanDepart)
                result.DepartReason = GameplayText.Text("ui_dispatch_reason_ready", "出发检查通过：可以出发。");
            else
                result.DepartReason = string.IsNullOrEmpty(reason)
                    ? GameplayText.Text("ui_dispatch_reason_blocked", "当前不能出发。") : reason;

            return result;
        }

        /// <summary>给 Presenter 组装操作提示时使用的显示名。</summary>
        public string GetRoleName(uint id)
        {
            if (id == 0) return string.Empty;
            RetainerSave retainer;
            if (!RetainerService.Instance.TryGet(id, out retainer)) return "门客 #" + id;
            return NameOf(retainer, RetainerService.Instance.GetDefinition(retainer.protoId));
        }

        private static DispatchSlotData[] BuildSlots(List<uint> team, RetainerService retainers)
        {
            var slots = new DispatchSlotData[TeamSize];
            for (int i = 0; i < TeamSize; i++)
            {
                var slot = new DispatchSlotData { Index = i + 1, Controlled = i == 0 };
                if (i < team.Count)
                {
                    slot.Occupied = true;
                    slot.Id = team[i];
                    RetainerSave retainer;
                    if (retainers.TryGet(team[i], out retainer))
                    {
                        RetainerTable_RetainerConfig definition = retainers.GetDefinition(retainer.protoId);
                        slot.Name = NameOf(retainer, definition);
                        slot.QualityText = QualityText(retainer.quality);
                        slot.Status = StatusText(retainer, retainers);
                        slot.Available = retainers.IsDeployable(retainer);
                    }
                    else
                    {
                        slot.Name = "门客 #" + team[i];
                        slot.QualityText = QualityText(0);
                        slot.Status = GameplayText.Text("ui_dispatch_missing", "数据缺失");
                        slot.Available = false;
                    }
                }
                slots[i] = slot;
            }
            return slots;
        }

        private static DispatchRoleData BuildRole(
            DispatchSnapshot snapshot,
            uint id,
            List<RetainerSave> all,
            List<uint> team,
            RetainerService retainers,
            DispatchService dispatch)
        {
            var role = new DispatchRoleData
            {
                Id = id,
                Name = string.Empty,
                Profession = GameplayText.Text("ui_dispatch_profession_placeholder", "职业待配置"),
                QualityText = QualityText(0),
                Status = string.Empty,
                WoundTime = "—",
                JoinReason = string.Empty
            };

            int index = team.IndexOf(id);
            role.InTeam = index >= 0;
            role.Controlled = index == 0;
            role.TeamIndex = index >= 0 ? index + 1 : 0;

            RetainerSave retainer = null;
            bool found = id != 0 && retainers.TryGet(id, out retainer);
            role.HasRole = found;
            if (!found)
            {
                if (id != 0)
                {
                    role.Name = "门客 #" + id;
                    role.Status = GameplayText.Text("ui_dispatch_missing", "数据缺失");
                    role.UnavailableReason = GameplayText.Text("ui_dispatch_missing_reason", "门客已不存在");
                }
                role.JoinReason = role.InTeam
                    ? GameplayText.Text("ui_dispatch_join_missing", "该成员数据缺失，只能尝试移出编队。")
                    : GameplayText.Text("ui_dispatch_join_empty", "点击左侧卡片或编队槽位查看详情。");
                role.CanRemove = snapshot.CanEdit && role.InTeam;
                return role;
            }

            RetainerTable_RetainerConfig definition = retainers.GetDefinition(retainer.protoId);
            role.Name = NameOf(retainer, definition);
            role.QualityText = QualityText(retainer.quality);
            role.Confidence = retainer.confidence;
            role.Status = StatusText(retainer, retainers);
            role.Wounded = retainer.wounded;
            role.WoundTime = retainer.wounded ? retainer.woundedRemainSeconds + "s" : "—";
            role.Available = retainers.IsDeployable(retainer);
            role.UnavailableReason = dispatch.GetUnavailableReason(retainer);
            role.Profession = ProfessionText(role.Controlled);

            int baseHp = DefaultHp;
            int baseAttack = DefaultAiAttack;
            float baseSpeed = GetAllySpeed();
            if (role.Controlled)
            {
                EnsurePlayerAssets();
                if (_playerHealth != null) baseHp = _playerHealth.maxHp;
                baseAttack = _playerAttack != null ? _playerAttack.damage : DefaultPlayerAttack;
                baseSpeed = _playerLocomotion != null ? _playerLocomotion.moveSpeed : DefaultSpeed;
            }
            else if (definition != null)
            {
                baseHp = (int)definition.Hp;
            }

            role.Hp = MetaCharacterStats.MaxHealth(baseHp);
            role.Attack = MetaCharacterStats.AttackDamage(baseAttack);
            role.Speed = MetaCharacterStats.MoveSpeed(baseSpeed);
            role.DefenseReduction = EffectAggregator.Instance.Snapshot.DefenseBonus;

            // 门客与玩家共用原型骨骼；先确保只读素材已加载，避免首次查看非受控门客时预览空置。
            EnsurePlayerAssets();
            // 头像和皮肤是玩家专属外观，不套到 AI 门客上；骨骼素材仍作为共用模板展示。
            if (role.Controlled)
            {
                role.Portrait = _playerPresentation != null ? _playerPresentation.portrait : null;
                role.PreviewSkin = _playerPresentation != null ? _playerPresentation.previewSkin : null;
            }
            role.SpineAsset = _playerSpine;
            role.SpineMaterial = _spineMaterial;
            role.HasSpine = _playerSpine != null && _spineMaterial != null;

            bool full = team.Count >= TeamSize;
            role.CanAdd = snapshot.CanEdit && !role.InTeam && role.Available && !full;
            role.CanRemove = snapshot.CanEdit && role.InTeam;
            role.CanSetControlled = snapshot.CanEdit && role.InTeam && index > 0 && role.Available;

            if (!snapshot.HasSession)
                role.JoinReason = GameplayText.Text("ui_dispatch_join_no_session", "请先读档并进入已就绪的村庄。");
            else if (snapshot.IsInRun)
                role.JoinReason = GameplayText.Text("ui_dispatch_join_inrun", "局内只读：不能调整编队，可从底部撤离。");
            else if (snapshot.IsTransitioning)
                role.JoinReason = GameplayText.Text("ui_dispatch_join_transition", "场景切换中，暂不能调整编队。");
            else if (role.InTeam)
                role.JoinReason = role.Controlled
                    ? GameplayText.Text("ui_dispatch_join_controlled", "当前为玩家操控位。")
                    : GameplayText.Text("ui_dispatch_join_member", "已在该编队中，可移出或设为玩家操控。");
            else if (!role.Available)
                role.JoinReason = string.IsNullOrEmpty(role.UnavailableReason)
                    ? GameplayText.Text("ui_dispatch_join_unavailable", "当前不可出战。") : role.UnavailableReason;
            else if (full)
                role.JoinReason = GameplayText.Text("ui_dispatch_join_full", "队伍已满（最多 4 位）。");
            else
                role.JoinReason = GameplayText.Text("ui_dispatch_join_ready", "可以加入编队。");

            return role;
        }

        private static bool MatchesFilter(DispatchCardData card, DispatchFilter filter)
        {
            switch (filter)
            {
                case DispatchFilter.Available: return card.Available;
                case DispatchFilter.Team: return card.InTeam;
                default: return true;
            }
        }

        private static bool MatchesSearch(string name, string search)
        {
            if (string.IsNullOrEmpty(search)) return true;
            return !string.IsNullOrEmpty(name)
                   && name.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static uint ResolveViewId(uint viewId, List<RetainerSave> all, List<uint> team)
        {
            if (viewId != 0 && (ContainsRetainer(all, viewId) || team.Contains(viewId))) return viewId;
            if (team.Count > 0) return team[0];
            if (all != null)
            {
                for (int i = 0; i < all.Count; i++)
                    if (all[i] != null) return all[i].id;
            }
            return 0;
        }

        private static bool ContainsRetainer(List<RetainerSave> all, uint id)
        {
            if (all == null) return false;
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].id == id) return true;
            return false;
        }

        private static string NameOf(RetainerSave retainer, RetainerTable_RetainerConfig definition)
        {
            if (retainer == null) return string.Empty;
            string fallback = "门客 #" + retainer.id;
            if (definition == null || string.IsNullOrEmpty(definition.NameKey)) return fallback;
            return GameplayText.Text(definition.NameKey, fallback);
        }

        private static string QualityText(uint quality)
        {
            return GameplayText.Text("ui_dispatch_quality", "品质 {0}", quality);
        }

        private static string StatusText(RetainerSave retainer, RetainerService retainers)
        {
            if (retainer == null) return string.Empty;
            switch (retainers.GetState(retainer))
            {
                case RetainerService.DeployState.Wounded:
                    return GameplayText.Text("ui_dispatch_status_wounded", "重伤 {0}s",
                        retainer.woundedRemainSeconds);
                case RetainerService.DeployState.LowConfidence:
                    return GameplayText.Text("ui_dispatch_status_low_confidence", "信心不足");
                default:
                    return GameplayText.Text("ui_dispatch_status_available", "可出战");
            }
        }

        private static string ProfessionText(bool controlled)
        {
            EnsurePlayerAssets();
            if (controlled)
            {
                string profession = _playerPresentation != null ? _playerPresentation.profession : null;
                if (!string.IsNullOrEmpty(profession)) return profession;
                return GameplayText.Text("ui_dispatch_profession_placeholder", "职业待配置");
            }
            return GameplayText.Text("ui_dispatch_profession_retainer", "门客 · 职业待配置");
        }

        private static float GetAllySpeed()
        {
            if (_allySpeedLoaded) return _allySpeed;
            _allySpeedLoaded = true;
            try
            {
                RunBehaviorSettings settings = RunBehaviorSettings.Load("Ally");
                if (settings != null && settings.moveSpeed > 0f) _allySpeed = settings.moveSpeed;
            }
            catch (Exception)
            {
                // 配置缺失时明确回退到既有速度口径，避免界面打不开。
                _allySpeed = DefaultSpeed;
            }
            return _allySpeed;
        }

        /// <summary>只在运行时读取 Player 预制体与共享材质，不复制或修改预制体。</summary>
        private static void EnsurePlayerAssets()
        {
            if (_assetsLoaded) return;
            _assetsLoaded = true;
            try
            {
                GameObject prefab = Resources.Load<GameObject>("Player/Player");
                if (prefab != null)
                {
                    _playerHealth = prefab.GetComponentInChildren<PlayerHealth>(true);
                    _playerAttack = prefab.GetComponentInChildren<PlayerAttack>(true);
                    _playerLocomotion = prefab.GetComponentInChildren<PlayerLocomotion>(true);
                    _playerPresentation = prefab.GetComponentInChildren<RunCharacterPresentation>(true);
                    SkeletonAnimation skeleton = prefab.GetComponentInChildren<SkeletonAnimation>(true);
                    if (skeleton != null) _playerSpine = skeleton.skeletonDataAsset;
                }
                _spineMaterial = Resources.Load<Material>("UI/Spine/RunTeamSkeletonGraphic");
            }
            catch (Exception)
            {
                _playerHealth = null;
                _playerAttack = null;
                _playerLocomotion = null;
                _playerPresentation = null;
                _playerSpine = null;
                _spineMaterial = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            _assetsLoaded = false;
            _playerHealth = null;
            _playerAttack = null;
            _playerLocomotion = null;
            _playerPresentation = null;
            _playerSpine = null;
            _spineMaterial = null;
            _allySpeedLoaded = false;
            _allySpeed = DefaultSpeed;
        }
    }
}
