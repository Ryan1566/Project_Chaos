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
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:29:20.163Z"
fingerprint: 726bc182e93a35267335fa4afb68dea9266dd6eb0905861237af9bec338d8a7e
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
