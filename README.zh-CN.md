# DevMode

[English](./README.md) | **中文**

《杀戮尖塔 2》全功能游戏内工具箱：测试、作弊、脚本与 Mod 调试一体化。

![DevMode](https://raw.githubusercontent.com/WRXinYue/STS2-DevMode/main/assets/devmode.png)

## 面板一览

### 玩法与内容

- **作弊** — 无敌、无限能量/格挡、伤害倍率、冻结敌人、数值锁定、地图覆盖、奖励调整
- **卡牌** — 全卡库浏览；按类型/稀有度/费用/卡池/角色筛选；编辑数值；添加至任意牌堆；升级对比；筛选条件跨会话记忆
- **遗物** — 浏览并添加遗物
- **能力** — 施加能力（自身、所有敌人、指定、友军）；一键创建「战斗开始自动施加」钩子
- **药水** — 图标网格；一键创建「战斗开始自动使用」钩子
- **敌人** — 按房间或地图节点替换遭遇；预览内容；待机动画预览
- **事件** — 浏览与触发事件流程
- **房间** — 查看与跳转房间类型
- **预设** — 保存/加载战斗与 run 快照（手牌、牌组、遗物等）

### 自动化与 AI

- **钩子** — 「触发器 → 条件 → 动作」规则（如战斗开始加牌、抽牌时施加能力）
- **脚本** — SpireScratch 可视化积木（Blockly）；WebSocket 热重载
- **AI 托管** — 规则 AI 驱动 **单人** run（地图、战斗、奖励）。联机手打时自动禁用，避免 desync；联机请用下方 Pseudo Co-op / LAN 预设

### 开发者与调试

- **敌人意图** — 敌人行动意图实时 overlay
- **战斗统计** — 按玩家统计单局伤害/格挡/治疗
- **控制台** — 原版与 DevMode 命令可搜索参考
- **日志** — 游戏内日志流，可配置噪音过滤
- **Harmony 分析** — 查看激活补丁；按 owner 筛选；智能摘要
- **框架** — 已加载 Mod 框架快照
- **Mod 反馈** — 导出 ZIP 问题报告（日志、Mod 列表、Harmony 转储）；隐私模式抹去路径

### 工具

- **存档** — 命名槽位；携带卡牌/遗物/金币开新种子；存档详情
- **手册** — 游戏内文档浏览器
- **设置** — 主题（Dark / OLED / Light / Warm）、游戏速度、跳过动画、侧栏布局

## 联机与共斗测试（开发向）

以下功能均在 DevPanel → **AI 托管** 中**手动开启**。未开启时不影响 vanilla 单人手打，也不改抽牌速度或抽牌动画。

| 模式 | 作用 | 适用场景 |
| --- | --- | --- |
| **AI 托管（单人）** | `SimpleStrategy` 本地代打你的角色 | 单人自动化 |
| **SyncBot** | 单机模拟远程 peer 的 ACK 与默认选项；可选幻影玩家（NetId 1001） | 无双开时的主机 co-op 冒烟测试 |
| **Pseudo Co-op 预设** | 主机手打 + AI 队友（幻影/离线 peer，走动作队列） | 单机主机 + 模拟队友 |
| **LAN 主机代打 + 客机 AFK** | 主机手打本机；AI 为真实 ENet 客户端 enqueue 战斗；客机 AFK 拦截本地战斗输入；地图投票镜像 | 同机双开（启动时自动 preset） |

**LAN 双开（推荐）：** 同机启动主机 + 客机 → 自动应用 preset；主机 log 见 `LAN host preset applied`，客机见 `AFK client enabled`。

架构说明、复测标准与历史 desync 记录：**[docs/lan-host-drive-afk.md](./docs/lan-host-drive-afk.md)** · [文档索引](./docs/README.md)

## 协作与贡献

协作流程、K&R 代码风格、`dotnet format` / `make format`、Python 与本地化等说明见 **[CONTRIBUTING.md](CONTRIBUTING.md)**，或在 [GitHub](https://github.com/WRXinYue/STS2-DevMode) 提交 Issue / PR。

## 更新日志

版本历史请参阅 [CHANGELOG.zh-CN.md](https://github.com/WRXinYue/STS2-DevMode/blob/main/CHANGELOG.zh-CN.md)。

## 致谢

- [STS2-KaylaMod](https://github.com/mugongzi520/STS2-KaylaMod)

## 许可证

[MIT](https://github.com/WRXinYue/STS2-DevMode/blob/main/LICENSE)
