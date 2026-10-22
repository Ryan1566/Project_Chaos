using System;
using System.Collections.Generic;

namespace Chaos.MVP.Model.WorldMap
{
    public sealed class WorldMapSnapshot
    {
        public uint Stage;
        public List<WorldFactionArea> Areas;
        public List<WorldMapPoint> Points;
        public WorldMapPoint Selected;
        public string Info;
    }

    public sealed class WorldMapModel : IDisposable
    {
        private static readonly string[] StageNames = { "未使用", "战国末期", "秦朝", "秦末起义初期", "楚汉争霸", "汉刘邦时期" };
        private uint _stage;
        private WorldMapPoint _selected;
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
            return new WorldMapSnapshot { Stage = _stage, Areas = areas, Points = points, Selected = _selected, Info = info };
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
