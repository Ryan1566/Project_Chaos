using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BattleSlotSave
{
    public RunBagSection section;
    public int index;
    public uint itemId, count;

    public BattleSlotSave Copy() => (BattleSlotSave)MemberwiseClone();
}

[Serializable]
public sealed class BattlePresetSave
{
    public bool saved;
    public List<BattleSlotSave> slots = new List<BattleSlotSave>();
}

// format 为零表示没有进行中的出征，兼容 Unity 内联对象和没有此字段的旧档。
[Serializable]
public sealed class ActiveRunSave
{
    public const int CurrentFormat = 1;
    public int format, stage, generatorVersion;
    public uint seed, departure, era;
    public string configFingerprint, layoutSignature;
    public List<uint> team = new List<uint>();
    public List<BattleSlotSave> bag = new List<BattleSlotSave>();
    public List<RunMemberSave> members = new List<RunMemberSave>();
    public List<RunTimedItemSave> buffs = new List<RunTimedItemSave>();
    public List<RunTimedItemSave> cooldowns = new List<RunTimedItemSave>();
    public List<RunQuestSave> quests = new List<RunQuestSave>();
    public List<RunContentClaimSave> claims = new List<RunContentClaimSave>();
    public List<string> defeatedEnemies = new List<string>();
    public List<RunGroundDropSave> drops = new List<RunGroundDropSave>();
    public List<uint> startingImportant = new List<uint>();
    public string explored;
    public Vector2 playerFeet;
    public bool campUsed;
    public float timeMinutes, playSeconds;
    public int stagesPassed, kills, highestDamage;
    public uint killSequence;
    // 0 为进行中，1 为成功结算，2 为失败结算。
    public int outcome, settlementPage;
    public List<uint> retained = new List<uint>();
}

[Serializable]
public sealed class RunMemberSave
{
    public uint id;
    public int hp, level, experience, points, health, stamina, mana;
    public bool downed;
}

[Serializable]
public sealed class RunTimedItemSave
{
    public uint id;
    public int value;
    public float seconds;
}

[Serializable]
public sealed class RunQuestSave
{
    public string id;
    public Chaos.Gameplay.Interaction.RunNpcQuestDefinition definition;
    public int progress;
    public bool completed;
    public List<string> gathered = new List<string>();
}

[Serializable]
public sealed class RunContentClaimSave
{
    public string id;
    public List<int> claimed = new List<int>();
}

[Serializable]
public sealed class RunGroundDropSave
{
    public uint id, count;
    public Vector2 position;
}
