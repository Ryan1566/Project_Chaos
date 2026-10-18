# @local/dsh-unity-mcp

把 DSH 连到 **AnkleBreaker Unity MCP** 服务器（`anklebreaker-unity-mcp`）的 DSH 插件包。
一个纯配置 bundle：不含任何 Host / Client 代码，只插入一行 `@deepseek-ai/dsh-mcp-client` 并指向 Unity MCP 服务器。

## 文件

| 文件 | 作用 |
| --- | --- |
| `package.json` | 包清单。`dsh.bundle.patch` 指向下面的补丁，同时声明 `type: module` |
| `cordis.patch.yml` | 实际配置：插入 `mcp-unity` 行，stdio 传输，指向 `E:/unity-mcp-server/src/index.js` |

## 装了之后

模型看到的工具名是 **`mcp__unity__<工具名>`**，服务器名来自补丁里的 `serverName: unity`。
330+ 个工具覆盖场景、GameObject、组件、资源、脚本、构建、控制台、测试、Play 模式等。

## 前置条件

1. **Unity Editor 打开**，并且载入了装了桥接插件的工程。
   Project_Chaos 已在 `Packages/manifest.json` 里声明 `com.anklebreaker.unity-mcp`。
2. 桥接端口与 `UNITY_BRIDGE_PORT` 一致（默认 7890；服务器会在 7890–7899 间扫描）。

Unity 没开时不会报错，只是工具连不上；`failOnStartupError: false` 保证 DSH 本体照常启动。

## 改配置

改 `cordis.patch.yml` 后需要**重启 DSH**，或者从插件管理页重新应用，改动才会生效。

常用可调项：

| 字段 | 默认 | 说明 |
| --- | --- | --- |
| `serverName` | `unity` | 决定工具名前缀，同一作用域内必须唯一 |
| `transport` | `stdio` | 也可用 `streamable-http` 连远端 MCP 服务 |
| `toolCallTimeoutMs` | 60000 | 单个工具调用超时 |
| `failOnStartupError` | false | 是否让连接失败直接阻止插件激活 |
| `reconnect.*` | 见插件文档 | 断线重连退避策略 |

多个 Unity 工程同时开时，服务器会自动发现所有实例；选定目标后建议在每次 `unity_*` 调用里带上 `port` 参数，避免多 agent 并发时路由错乱。

## 安装方式

本包通过 **目录链接（junction）** 挂进 profile，而不是 pnpm 安装：

```
C:\Users\26751\.dsh\profiles\desktop\node_modules\@local\dsh-unity-mcp
    -> D:\Unity Projects\Project_Chaos\Tools\dsh-unity-mcp
```

并在 profile 的 `package.json` 里登记：

```json
"dependencies": {
  "@local/dsh-unity-mcp": "file:D:/Unity Projects/Project_Chaos/Tools/dsh-unity-mcp"
},
"dsh": { "profile": { "bundles": [ "...", "@local/dsh-unity-mcp" ] } }
```

之所以能这样装：DSH 解析组合包时用 Node 的模块解析路径去 `node_modules` 下找包目录
（`packageDirFromAnchor`），只看磁盘上有没有 `package.json`，不读依赖列表。
而 `@deepseek-ai/dsh-mcp-client` 随 DSH 本体发布，从安装目录解析，所以本包**不需要声明任何依赖**。

**建议**：之后如果从插件事理页（`install_bundle`）管理插件，让管理页接管这个包的登记，
避免手工的 junction 与 pnpm 对 profile 的整理互相覆盖。

## 换成正式安装（可选）

如果想改成 pnpm 正式安装、由插件管理页统一管理：

1. 在 GUI 侧边栏「插件」页删掉 / 忽略当前登记；
2. 用 `install_bundle`，`target` 填 `D:\Unity Projects\Project_Chaos\Tools\dsh-unity-mcp`。

本地路径 spec 会跳过 GitHub 可达性检查，也不需要批准任何构建脚本（本包没有依赖）。
