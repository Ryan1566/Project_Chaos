---
uid: 4c1f9a2b
id: project-chaos
parent: null
repository: "https://github.com/Ryan1566/Project_Chaos.git"
tags: [unity, architecture-root, project-chaos]
name: {zh: "Project_Chaos 游戏工程", en: "Project_Chaos Game Project"}
description:
  zh: >
      Unity 2022.3.57f1c2 的 2D 横板 Roguelite 工程总览（仓库根 D:\Unity Projects\Project_Chaos）。8 个一级域：启动与场景骨架、运行时框架、MVP 视图层、配置表管线、编辑器工具集、资源库、工程基建与第三方、团队协作与文档契约。这是全队开发前必查的架构树。
  en: >
      Architecture overview of the Unity 2022.3.57f1c2 side-scrolling 2D roguelite project at D:\Unity Projects\Project_Chaos. Eight top-level domains: bootstrap & scene shell, runtime framework, MVP view layer, config-table pipeline, editor tooling, asset library, project infra & third-party, and team process/docs.
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T11:55:00Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
---

# Project_Chaos 架构模块树 —— 生成与维护规范（权威）

本目录（`Assets/AI/normify-project-chaos/`）是 Project_Chaos 的架构结构数据。仓库根 = `D:\Unity Projects\Project_Chaos`（`source.path` 一律相对该根、正斜杠）。
**以后所有开发先查这棵树**：`normify_search` / `normify_brief` 定位模块与契约，再按模块的 apis/deps 写代码。

## 1. 单树与 id 命名

- 只有一棵树，根 id = `project-chaos`。
- 段字符集 `^[a-z][a-z0-9-]*$`：全小写、词间连字符、禁数字开头；末段不要用 `index`。
- id 表达“职责链”而不是“目录链”，例：`project-chaos.framework.ui.panel-animator`。
- 深度不设上限；能说清“它内部由哪几块组成”就继续下钻。

## 2. 模块粒度（叶子判据）

- 一个 C# 类 / 一组内聚函数 / 一个 UI 组件 / 一个预制体族 / 一张配置表 / 一条启动步骤 = 一个叶子。
- **单文件 > 220 行必须按职责拆成 ≥2 个叶子**，source 指向同一文件的不同行段（`line` / `end_line`）。
- 叶子必须写 `apis`（理想 3–5 条）；有子模块的容器**禁止**写 `apis`。
- 不要为了省事把多个 Manager 塞进一个框。

## 3. source（代码证据）

- 只能是**仓库内已存在的文件**（目录算不出指纹 → error）。
- 给 `line` / `end_line`，指向该模块真正负责的代码段。
- 容器模块 `source: []`，`fingerprint` 用空串哈希 `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`。
- 本轮测绘（已有代码）：叶子写 `state: planned` + `fingerprint: pending`，结束后由 Lead 统一 `normify_module_refresh({all:true, activate:true})` 落指纹并转 active。

## 4. apis（只在叶子，键 = `protocol:path`，全项目唯一）

- C# 公开方法/属性/事件 → `{ protocol: rpc, path: "类名.成员" }`。类名全工程唯一；不唯一时带父目录，例 `OtherUIManagerBasePanel.Show`。
- 编辑器菜单命令 → `{ protocol: rpc, path: "Menu/<菜单路径>" }`。
- 资产文件（prefab / scene / asset / png / mp3 / font / xlsx / json / csv / docx / py / txt）→ `{ protocol: file, path: "<仓库相对路径>" }`。
- 每条都要双语一句话简介；未接箭头的 API 合法，不要因为“没人调”删掉。

## 5. deps（箭头，只存源端）

- 只在能确认调用/引用时写：`call`=同步调用、`event`=事件发布订阅、`dataflow`=数据流、`reference`=一般引用。
- 两端都有 API 时必须补 `from_api` / `to_api`（API 直连），否则箭头落不到具体 API 行。
- **架构硬约束**（`policy.yml` 强制）：运行时框架层不得指向 MVP 视图层；运行时代码不得指向编辑器工具集；依赖图无环。

## 6. 渲染数据

每个容器模块都要 `normify_layout_upsert`：`order`（按数据流/调用方向）+ `reading`（一句阅读导语）必写；同层语义成簇时用 `mode: groups` + `groups`。

## 7. 相伴开发流程（以后新增/改动功能时）

```
normify_brief(任务) → normify_check(拟建模块与依赖) → normify_module_batch(state=planned)
  → 写代码 → normify_module_refresh(ids, activate=true) → normify_validate(0 error)
  → normify_build → normify_render → normify_change_close(0 error 强制)
```

代码改了但树没跟上 = 结构漂移：用 `normify_sync` 检出，再 `normify_module_patch` 跟随更新。
