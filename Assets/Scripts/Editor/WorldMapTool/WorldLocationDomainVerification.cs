#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 领域自检：使用内存沙盒，不读写真实存档；可由主脑在编辑器菜单独立运行。
/// 物品 9101..9104 尚未由主脑写入 ItemTable 时，奖励/产物相关步骤标记 SKIP。
/// </summary>
public static class WorldLocationDomainVerification
{
    [MenuItem("Tools/QA/World Locations/Domain Verification")]
    public static void RunMenu()
    {
        string report = Run();
        if (report.StartsWith("FAIL")) Debug.LogError(report); else Debug.Log(report);
    }

    public static string Run()
    {
        StringBuilder log = new StringBuilder();
        int fail = 0, skip = 0;
        Action<bool, string> check = (ok, msg) =>
        {
            log.AppendLine((ok ? "PASS " : "FAIL ") + msg);
            if (!ok) fail++;
        };
        try
        {
            SavePayload p = new SavePayload
            {
                era = 1, runs = 5, clears = 5,
                resMoney = 10000, resMaterial = 10000, resGoods = 10000, resBlueprint = 10000,
            };
            GameSession.Instance.BeginSandbox(p);
            WorldLocationService service = WorldLocationService.Instance;
            service.Reload();
            InventoryService.Instance.Reload();

            int[] expected = { 7, 1, 8, 19, 11 };
            int capitalStates = 0;
            for (uint era = 1; era <= 5; era++)
            {
                p.era = era;
                List<WorldLocationSnapshot> all = service.GetLocations(era, WorldLocationIdentity.Family);
                WorldLocationSnapshot[] caps = all.Where(x => x.Kind == WorldLocationKind.Capital).ToArray();
                capitalStates += caps.Length;
                check(caps.Length == expected[era - 1], "era " + era + " capital count " + caps.Length);
                check(caps.Select(x => x.FactionId).Distinct().Count() == caps.Length, "era " + era + " one capital per faction");
                check(all.Count(x => x.Kind == WorldLocationKind.Trade || x.Kind == WorldLocationKind.Tribe) == 13, "era " + era + " 13 trade/tribe");
                check(all.Count(x => x.Kind == WorldLocationKind.Resource) == 10, "era " + era + " 10 resources");
                check(all.Select(x => x.Id).Distinct().Count() == all.Count, "era " + era + " unique ids");
                check(all.All(x => x.Position.x >= 0f && x.Position.x <= 1f && x.Position.y >= 0f && x.Position.y <= 1f), "era " + era + " bounded positions");
            }
            check(capitalStates == 46, "46 capital era states");

            p.era = 1;
            string before = JsonUtility.ToJson(p);
            List<WorldLocationSnapshot> family = service.GetLocations(1, WorldLocationIdentity.Family);
            WorldLocationSnapshot qin = service.GetLocations(1, WorldLocationIdentity.Official)
                .First(x => x.Kind == WorldLocationKind.Capital && x.FactionId == 1);
            WorldLocationSnapshot zhao = service.GetLocations(1, WorldLocationIdentity.Official)
                .First(x => x.Kind == WorldLocationKind.Capital && x.FactionId == 2);
            check(qin.CanOpenOffice && !zhao.CanOpenOffice && !zhao.CanTrade, "official same allowed / hostile blocked");
            service.GetLocations(4, WorldLocationIdentity.Official);
            check(before == JsonUtility.ToJson(p), "queries do not mutate payload");

            WorldLocationSnapshot market = family.First(x => x.Kind == WorldLocationKind.Trade && x.CanTrade);
            uint item = service.GetMarketItems(market.Id).First(x => !RunInventory.IsImportant(x));
            uint price = service.GetBuyPrice(market.Id, item);
            uint cash = p.resMoney, owned = MarketService.Instance.GetCount(item);
            check(price == (uint)Math.Ceiling(InventoryService.Instance.GetDefinition(item).BuyPrice * market.BuyPercent / 100.0),
                "buy price rounds up by relation");
            WorldLocationSnapshot hostile = family.First(x => x.Kind == WorldLocationKind.Trade && !x.CanTrade);
            string locked = JsonUtility.ToJson(p);
            check(!service.TryBuy(hostile.Id, item, out _) && locked == JsonUtility.ToJson(p), "hostile trade rejected atomically");
            check(service.TryBuy(market.Id, item, out _) && p.resMoney == cash - price
                && MarketService.Instance.GetCount(item) == owned + 1, "buy exact money/item");
            uint sell = service.GetSellPrice(market.Id, item);
            cash = p.resMoney;
            check(service.TrySell(market.Id, item, out _) && p.resMoney == cash + sell
                && MarketService.Instance.GetCount(item) == owned, "sell exact money/item");
            p.resMoney = 0;
            locked = JsonUtility.ToJson(p);
            check(!service.TryBuy(market.Id, item, out _) && locked == JsonUtility.ToJson(p), "insufficient money unchanged");
            p.resMoney = 10000;

            WorldLocationSnapshot resource = family.First(x => x.Kind == WorldLocationKind.Resource && x.CanBuild
                && service.GetLocation(4, x.Id, WorldLocationIdentity.Family)?.CanBuild != true);
            uint money = p.resMoney, material = p.resMaterial;
            check(WorldResourceService.Instance.Build(resource.Id, out _)
                && p.resMoney == money - 50 && p.resMaterial == material - 20, "build costs exactly once");
            string built = JsonUtility.ToJson(p);
            check(!WorldResourceService.Instance.Build(resource.Id, out _) && built == JsonUtility.ToJson(p), "duplicate build unchanged");
            WorldResourceService.Instance.Tick(60);
            WorldLocationSnapshot point = service.GetLocation(1, resource.Id, WorldLocationIdentity.Family);
            check(point.PendingOutput == 1, "60s -> 1 output");
            WorldResourceService.Instance.Tick(6000);
            point = service.GetLocation(1, resource.Id, WorldLocationIdentity.Family);
            check(point.PendingOutput == 10, "buffer cap 10");
            if (InventoryService.Instance.GetDefinition(point.OutputItemId) == null)
            {
                skip++;
                log.AppendLine("SKIP partial harvest / temp reward: ItemTable missing " + point.OutputItemId);
            }
            else
            {
                p.items = new List<ItemStackSave> { new ItemStackSave { itemId = point.OutputItemId, count = 98 } };
                check(WorldResourceService.Instance.Harvest(resource.Id, out uint amount, out _)
                    && amount == 1 && MarketService.Instance.GetCount(point.OutputItemId) == 99, "partial harvest 98->99 amount 1");
                check(service.GetLocation(1, resource.Id, WorldLocationIdentity.Family).PendingOutput == 9, "harvest remainder retained");
                p.items.Clear();
                check(WorldResourceService.Instance.Harvest(resource.Id, out _, out _), "restored access collects remainder");

                p.items.Clear();
                WorldLocationSnapshot temp = service.GetLocations(1, WorldLocationIdentity.Family)
                    .First(x => x.Kind == WorldLocationKind.Temporary && x.CanResolve);
                check(service.ResolveTemporary(temp.Id, out _), "temporary resolves through inventory");
                string done = JsonUtility.ToJson(p);
                check(!service.ResolveTemporary(temp.Id, out _) && done == JsonUtility.ToJson(p), "repeat temp click unchanged");
                GameSession.Instance.BeginSandbox(JsonUtility.FromJson<SavePayload>(done));
                p = GameSession.Instance.LoadedPayload;
                check(!service.ResolveTemporary(temp.Id, out _) && JsonUtility.ToJson(p) == done, "temp completion survives round trip");
            }

            p.era = 4;
            string era4 = JsonUtility.ToJson(p);
            WorldResourceService.Instance.Tick(120);
            check(!WorldResourceService.Instance.Harvest(resource.Id, out _, out _) && era4 == JsonUtility.ToJson(p),
                "era loss freezes without confiscation");
            p.era = 1;
            var mapIds = new HashSet<uint>();
            for (uint era = 1; era <= 5; era++)
                foreach (WorldMapPoint mapPoint in service.BuildMapPoints(era, WorldLocationIdentity.Family))
                    mapIds.Add(mapPoint.refId);
            check(mapIds.Count == 62, "map points generated for merge (62 stable refs)");

            SavePayload legacy = JsonUtility.FromJson<SavePayload>(
                "{\"era\":1,\"resMoney\":20,\"officeTasks\":[{\"taskId\":9001,\"cycle\":1,\"status\":0,\"deadlineClock\":123}]}");
            check(legacy.officeTasks[0].status == OfficeTaskStatus.Active && legacy.officeTasks[0].deadlineClock == 123,
                "legacy payload keeps task state");
        }
        catch (Exception e)
        {
            fail++;
            log.AppendLine("FAIL exception: " + e);
        }
        log.AppendLine(fail == 0 ? ("PASS ALL (skipped " + skip + ")") : ("FAIL count=" + fail + " skipped=" + skip));
        return log.ToString().TrimEnd();
    }
}
#endif
