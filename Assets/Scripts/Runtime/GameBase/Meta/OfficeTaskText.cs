using System.Collections.Generic;
using LocalizationSystem;

/// <summary>任务框架的界面文字；任务叙事正文由配置表按世界观编写。</summary>
public static class OfficeTaskText
{
    static readonly Dictionary<string,string[]> Texts = new Dictionary<string,string[]> {
        {"title",new[]{"任务与事件","Tasks & Events","任務與事件","任務と出来事","임무와 사건"}},
        {"county",new[]{"郡县指派事务","County Assignments","郡縣指派事務","郡県の指示","군현 지시"}},
        {"admin",new[]{"行政事件","Administration","行政事件","行政の出来事","행정 사건"}},
        {"preset",new[]{"预设任务","Side Tasks","預設任務","個別任務","개별 임무"}},
        {"emergency",new[]{"突发任务","Emergencies","突發任務","緊急任務","긴급 임무"}},
        {"metrics",new[]{"声望 {0}　影响力 {1}　村庄发展度 {2}","Reputation {0}   Influence {1}   Development {2}","聲望 {0}　影響力 {1}　村莊發展度 {2}","名声 {0}　影響力 {1}　村の発展 {2}","명성 {0}   영향력 {1}   마을 발전 {2}"}},
        {"back",new[]{"返回","Back","返回","戻る","뒤로"}},
        {"resume",new[]{"继续游戏","Resume","繼續遊戲","ゲーム再開","게임 계속"}},
        {"settings",new[]{"设置","Settings","設定","設定","설정"}},
        {"reports",new[]{"待处理来报（{0}）","Pending Reports ({0})","待處理來報（{0}）","未処理の報告（{0}）","미처리 보고 ({0})"}},
        {"noReports",new[]{"目前没有待处理来报。","There are no pending reports.","目前沒有待處理來報。","未処理の報告はありません。","미처리 보고가 없습니다."}},
        {"empty",new[]{"当前分类暂无符合条件的事务。","No eligible tasks in this category.","目前分類暫無符合條件的事務。","この分類に該当する任務はありません。","이 분류에 해당하는 임무가 없습니다."}},
        {"active",new[]{"进行中","Active","進行中","進行中","진행 중"}},
        {"completed",new[]{"已完成","Completed","已完成","完了","완료"}},
        {"failed",new[]{"已逾期","Expired","已逾期","期限切れ","기한 만료"}},
        {"battle",new[]{"来袭待处置","Raid Pending","來襲待處置","来襲に対応待ち","습격 대응 대기"}},
        {"awaitBattle",new[]{"来袭敌情待处置；局外战斗尚未接入，本任务不结算胜负。","Raid response pending. Village combat is not available; no battle result is settled.","來襲敵情待處置；局外戰鬥尚未接入，本任務不結算勝負。","来襲への対応待ち。村の戦闘は未実装のため勝敗は確定しません。","습격 대응 대기. 마을 전투가 아직 연결되지 않아 승패를 정산하지 않습니다."}},
        {"prepared",new[]{"应对方案已记录，等待时限结束。","Preparation recorded; awaiting the deadline.","應對方案已記錄，等待時限結束。","対応方針を記録しました。期限を待っています。","대응 방안이 기록되어 기한을 기다립니다."}},
        {"time",new[]{"剩余 {0}:{1}","Time {0}:{1}","剩餘 {0}:{1}","残り {0}:{1}","남은 시간 {0}:{1}"}},
        {"progress",new[]{"进度 {0}/{1}","Progress {0}/{1}","進度 {0}/{1}","進捗 {0}/{1}","진행 {0}/{1}"}},
        {"cost",new[]{"消耗{0} {1}","Cost: {0} {1}","消耗{0} {1}","必要: {0} {1}","소모: {0} {1}"}},
        {"reward",new[]{"获得{0} {1}","Gain: {0} {1}","獲得{0} {1}","獲得: {0} {1}","획득: {0} {1}"}},
        {"deltas",new[]{"声望{0}　影响力{1}　发展度{2}","Reputation {0}   Influence {1}   Development {2}","聲望{0}　影響力{1}　發展度{2}","名声{0}　影響力{1}　発展{2}","명성 {0}   영향력 {1}   발전 {2}"}},
        {"saved",new[]{"处置结果已保存。","Outcome saved.","處置結果已保存。","処理結果を保存しました。","처리 결과가 저장되었습니다."}},
        {"stale",new[]{"当前事务入口已失效，请重新进入。","This task entry is no longer valid. Please reopen it.","目前事務入口已失效，請重新進入。","この入口は無効です。開き直してください。","현재 업무 입구가 유효하지 않습니다. 다시 열어 주세요."}}
    };
    public static string Get(string key,params object[] args)
    {
        var language=LocalizationManager.GetInstance()?.CurrentLanguage??LanguageType.ChineseSimplified;
        int index=language==LanguageType.English?1:language==LanguageType.ChineseTraditional?2:language==LanguageType.Japanese?3:language==LanguageType.Korean?4:0;
        string text=Texts.TryGetValue(key,out var row)?row[index]:key;
        return args.Length==0?text:string.Format(text,args);
    }
    public static string Status(OfficeTaskStatus state) {return state==OfficeTaskStatus.Available?"可接取":Get(state==OfficeTaskStatus.Completed?"completed":state==OfficeTaskStatus.Failed?"failed":state==OfficeTaskStatus.AwaitingBattle?"battle":"active");}
    public static string Time(double seconds) {long value=(long)System.Math.Ceiling(System.Math.Max(0,seconds));return Get("time",value/60,(value%60).ToString("00"));}
}
