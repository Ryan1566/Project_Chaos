---
uid: f1c00062
id: project-chaos.framework.dev-tests.ui-probes
parent: project-chaos.framework.dev-tests
name: {zh: "UI 自测探针", en: "UI Probes"}
description:
  zh: >
      GameTest 下仅剩的 UI 自测探针：一个空壳 TestPanel。原本的 Push/Pop 探针（UITestEnter.cs）已改名为 GM/GM_UITestEnter.cs，并归到 dev-tests.gm-console.ui-test-entry。
      
  en: >
      UI dev probe left in GameTest: an empty TestPanel shell. Its push/pop probe (originally UITestEnter.cs) moved to Assets/Scripts/Runtime/GM/GM_UITestEnter.cs and is now catalogued as dev-tests.gm-console.ui-test-entry.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.419Z"
fingerprint: 245daecccdb03a153b32d115d682a64d68578676a0db7250629eccb39f50ffa4
source:
  - path: "Assets/Scripts/Runtime/GameTest/UITest/TestPanel.cs"
    line: 1
    end_line: 26
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameTest/UITest/TestPanel.cs"
    description:
      zh: >
          空面板探针（内容已全部注释）。
          
      en: >
          Empty panel probe shell (all content commented out).
          
---
