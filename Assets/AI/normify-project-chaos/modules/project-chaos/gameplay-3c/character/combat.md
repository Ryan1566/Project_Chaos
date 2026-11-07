---
uid: a3c00022
id: project-chaos.gameplay-3c.character.combat
parent: project-chaos.gameplay-3c.character
tags: [character, combat, health]
name: {zh: "攻击与生命", en: "Attack & Health"}
description:
  zh: >
      带盒形重叠查询的近战攻击，与玩家生命：道具/局内加成、无敌帧、接触伤害、倒地态与满血重置。受击反馈转发给战斗镜头抖动，不再改写角色缩放与朝向。
      
  en: >
      Melee attack with a box overlap query and the player's health: item and run bonuses, invulnerability window, contact damage, downed state and refill. Hit feedback is forwarded to the combat camera shake rather than rewriting the character's scale or facing.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.746Z"
fingerprint: 28a925945dd516a05d8326b09541ac3909fe7901f63959bea951a3f1b2535fcd
source:
  - path: "Assets/Scripts/Runtime/3C/Character/PlayerAttack.cs"
  - path: "Assets/Scripts/Runtime/3C/Character/PlayerHealth.cs"
apis:
  - protocol: rpc
    path: "PlayerAttack.LastHitCount"
    description:
      zh: >
          LastHitCount：上一帧挥击命中的目标数。
          
      en: >
          LastHitCount: how many targets the last swing hit.
          
  - protocol: rpc
    path: "PlayerHealth.ApplyContactDamage"
    description:
      zh: >
          ApplyContactDamage(amount)：受击入口，尊重无敌帧与倒地态。
          
      en: >
          ApplyContactDamage(amount): damage entry that respects invulnerability and downed state.
          
  - protocol: rpc
    path: "PlayerHealth.ForceDown"
    description:
      zh: >
          ForceDown()：强制进入倒地态。
          
      en: >
          ForceDown(): forces the downed state.
          
  - protocol: rpc
    path: "PlayerHealth.RestoreHealth"
    description:
      zh: >
          RestoreHealth(amount)：治疗并返回实际生效量。
          
      en: >
          RestoreHealth(amount): heals and returns the applied amount.
          
  - protocol: rpc
    path: "PlayerHealth.RefillFullHp"
    description:
      zh: >
          RefillFullHp()：把生命重置为当前有效上限。
          
      en: >
          RefillFullHp(): resets health to the effective maximum.
          
---
