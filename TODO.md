# 战斗统计 — 待办

DevMode 战斗伤害统计。MVP 基于 `CombatHistory`（公开 API，不对伤害管线打 Harmony patch）。

## MVP ✅（已完成）

- [x] **数据层** — `CombatStatsTracker` + `CombatHistoryTailer`
  - DevMode 激活时订阅 `CombatManager.CombatSetUp` / `CombatEnded`
  - 增量消费 `CombatHistory.Changed`
  - 处理：`DamageReceivedEntry`、`BlockGainedEntry`、`CardPlayFinishedEntry`
- [x] **单场战斗、按玩家聚合**
  - 总造成伤害（玩家/宠物 → 敌人）
  - 总承伤（未格挡部分）
  - 获得格挡
  - 出牌数
  - 分组：按卡牌、按伤害来源、按回合（DPT）
- [x] **UI** — DevPanel 侧栏 **战斗统计**
  - 概览 + 分类标签 + 排行列表
  - 面板打开时自动刷新
  - 战斗结束后保留上一场摘要
- [x] **i18n** — 英文 + 简体中文

## 完整版（MVP 测试通过后）

### 数据与归因

- [ ] 无 dealer 的能力伤害（毒、绞杀、Haunt、Doom 等）
- [ ] 宠物/召唤物归因边界（Misery、助攻按比例分摊等）
- [ ] 溢出伤害 / 被目标格挡 等列
- [ ] 敌人对玩家造成的伤害（怪物行动归因）
- [ ] 每回合能量消耗 / 浪费
- [ ] 药水使用统计
- [ ] 施加 debuff 统计
- [ ] 战斗事件时间线

### 范围与持久化

- [ ] 整局 Run 累计（当前 run 内所有战斗）
- [ ] 导出快照 JSON（反馈 / 调试报告用）
- [ ] 可选：Mod Feedback ZIP 附带最近一场战斗统计

### 联机

- [ ] 合作模式按玩家分色、显示名称
- [ ] 助攻 / 同目标伤害分摊

### UI

- [ ] 回合条形图（复用简单 bar 组件）
- [ ] 战斗内迷你 HUD（设置里可选开关）
- [ ] 当前场 vs 上一场对比
- [ ] 设置：战斗开始自动打开、HUD 位置

### 集成

- [ ] Hook 触发：`OnCombatStatsUpdated`（供脚本用，可选）
- [ ] 控制台命令：`dm stats` 输出 dump

## 参考

- 游戏 API：`CombatManager.Instance.History`，条目在 `MegaCrit.Sts2.Core.Combat.History.Entries`
- 伤害写入：`CreatureCmd.Damage` → `History.DamageReceived(...)`
- 参考 mod（仅思路）：DamageMeter 的 `HistoryTailer` + `CombatDataCollector`
