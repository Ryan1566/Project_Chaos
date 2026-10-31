using System;
using System.Collections.Generic;

namespace Chaos.MVP.Model.WorldMap
{
    /// <summary>Runtime label supplied by the model; normalized map coordinates, Y upward.</summary>
    public sealed class WorldFactionLabel
    {
        public uint FactionId;
        public string Name;
        public UnityEngine.Vector2 Position;
    }

    public sealed class WorldMapSnapshot
    {
        public uint Stage;
        public List<WorldFactionArea> Areas;
        public List<WorldFactionLabel> Labels;
        public List<WorldMapPoint> Points;
        public WorldMapPoint Selected;
        public string Info;
        public string Status, Hint;
        public bool ConfirmReturn;
    }

    public sealed class WorldMapModel : IDisposable
    {
        private static readonly string[] StageNames = { "未使用", "战国末期", "秦朝", "秦末起义初期", "楚汉争霸", "汉刘邦时期" };
        private uint _stage;
        private WorldMapPoint _selected;
        public bool ConfirmReturn { get; private set; }
        public string Hint { get; private set; }
        public WorldMapModel()
        {
            uint era = GameSession.Instance.LoadedPayload?.era ?? 1u;
            _stage = era >= 1u && era <= 5u ? era : 1u;
        }
        public WorldMapSnapshot Snapshot()
        {
            WorldMapService service = WorldMapService.Instance;
            List<WorldFactionArea> areas = service.Areas(_stage);
            List<WorldMapPoint> points = service.Points(1u);
            string info = "时期 " + _stage + "（" + StageNames[_stage] + "）　点位 " + points.Count
                + "　势力区域 " + areas.Count + "　—　点击点位查看详情";
            if (_selected != null) info = Describe(_selected);
            var meta = MetaProgressService.Instance;
            var inv = InventoryService.Instance;
            string status = "时代 " + meta.Era + " / 深度 " + meta.Depth + "\n物资 " + inv.GetResource(InventoryService.ResourceKind.Goods)
                + "　材料 " + inv.GetResource(InventoryService.ResourceKind.Material) + "\n图纸 " + inv.GetResource(InventoryService.ResourceKind.Blueprint)
                + "　钱财 " + inv.GetResource(InventoryService.ResourceKind.Money);
            return new WorldMapSnapshot { Stage = _stage, Areas = areas, Labels = WorldFactionLabelCatalog.Read(_stage, areas), Points = points, Selected = _selected, Info = info,
                Status = status, Hint = Hint ?? "选择门客并派遣，开始本次出征。", ConfirmReturn = ConfirmReturn };
        }
        public bool Save()
        {
            bool ok = MetaProgressService.Instance.SaveProgress(out string reason);
            Hint = ok ? (GameSession.Instance.IsSandbox ? "测试会话已在内存提交" : "已保存到槽 " + MetaProgressService.Instance.SaveSlot) : reason;
            return ok;
        }
        public void RequestReturn() { ConfirmReturn = true; Hint = "保存当前进度并返回主菜单？"; }
        public void CancelReturn() { ConfirmReturn = false; Hint = null; }
        public bool ReturnToMenu()
        {
            if (!Save()) return false;
            DispatchService.Instance.ResetSelection();
            GameSession.Instance.Clear();
            return true;
        }
        public void Select(uint pointId) { _selected = WorldMapService.Instance.Points(1u).Find(p => p.index == pointId); }
        public void Preview(int delta)
        {
            int next = (int)_stage + delta;
            _stage = (uint)(next < 1 ? 5 : next > 5 ? 1 : next);
            _selected = null;
        }
        private static string Describe(WorldMapPoint point)
        {
            string name = null;
            UnityEngine.Sprite icon = null;
            IWorldMapMetaProvider provider = WorldMapService.Instance.MetaProvider;
            if (provider != null && provider.TryGetMeta(point.kind, point.type, point.refId, out name, out icon)
                && !string.IsNullOrEmpty(name)) return name;
            string kind = point.kind == 1u ? "固定地点" : point.kind == 2u ? "兴趣点" : "预留类别";
            return kind + "　Kind=" + point.kind + "　Type=" + point.type + "　RefId=" + point.refId
                + "　X=" + point.position.x.ToString("0.0000") + "　Y=" + point.position.y.ToString("0.0000");
        }
        public void Dispose() { _selected = null; }
    }
}
