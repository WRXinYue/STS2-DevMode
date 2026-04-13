---
title:
  en: Skill tree module pattern (notes)
  zh-CN: 技能树模块说明（示例架构）
top: 10006
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

::: en

Example architecture: slot layout, activation rules, separation of Godot view vs pure C# rules, public API. Paths are placeholders under `your-mod/src/`. **Body in Chinese.**

:::

::: zh-CN

面向扩展与维护：槽位布局、激活规则与 Godot 视图的分工、对外 API 与调用方式。（以下为**通用模式说明**，具体文件名以你的仓库为准。）

## 目录与职责（示例）

| 路径（示例） | 职责 |
|------|------|
| `SkillTreeTypes.cs` | `Arm`、`IconKind` 等枚举（命名空间级，无 Godot） |
| `ISkillTreeRules.cs` | 技能树**纯规则**接口：几何、臂语义、激活图、`TryToggleActivation` |
| `SkillTreeRules.cs` | 默认实现；单例 `Instance` |
| `SkillTreeLayout.cs` | **门面**：`Default` 指向默认规则；静态方法转发，兼容旧调用 |
| `SkillTreeTalentSlots.cs` | 槽位 ID、二维表映射；布局变更时改表与 `SkillTreeRules`，勿随意改枚举数值 |
| `NSkillTreeScreen.cs` | Godot 视图：节点树、动画、槽位点击；依赖 `ISkillTreeRules` |
| `SkillTreeMapLayerFactory.cs` | 地图全屏层装配（`Control` + 视口适配） |
| `SkillTreeMapButtonPatch.cs` | Harmony 入口，调用 Factory |

## 约定（读代码前先看）

- **全局 `slotIndex`**：按你的规则定义上列、左支、右支索引范围。业务代码优先用 **强类型槽位 ID** 与 **激活状态查询 API**，避免裸写魔法数字。
- **`Arm` 的 `localIndex`**：按设计文档区分「自分叉向顶端」与「自分叉向外」。
- **激活图**：仅允许设计好的相邻关系；跨臂边若不存在于规则中则不要依赖。
- **业务规则**（解锁、消耗、存档）若加，应放在纯 C# 服务或模型，不要写进 Factory / Patch；视图通过接口或事件与业务交互。

## API 调用方式

### 推荐接口（可替换实现、便于测试）

```csharp
ISkillTreeRules rules = SkillTreeLayout.Default;
// 或
ISkillTreeRules rules = SkillTreeRules.Instance;

rules.BuildSlotAnchors();
rules.TryToggleActivation(slotIndex, activated);
```

### 门面静态调用

```csharp
SkillTreeLayout.BuildSlotAnchors();
SkillTreeLayout.TryToggleActivation(slotIndex, activated);
```

常量与门面一致：`SkillTreeLayout.TopCount`、`BranchCount`、`ActivationEntryLocalIndex`（名称以你的实现为准）。

## 设计模式（简要）

| 模式 | 作用 |
|------|------|
| **Strategy** | `ISkillTreeRules` 抽象「怎么布局、怎么激活」；可换实现。 |
| **Facade** | `SkillTreeLayout` 将默认规则包成静态 API，减少调用点改动。 |
| **Singleton（默认实例）** | `SkillTreeRules.Instance` 与 `SkillTreeLayout.Default` 指向同一套默认规则。 |

## 与地图全屏 UI 的关系

地图上的全屏容器、**为何慎用 `CanvasLayer` 包裹**（与官方 `NHoverTipSet` / `%HoverTipsContainer` 的绘制顺序）、视口适配与 `FitControlToViewport` 等见 **[踩坑备忘 — NMapScreen 叠层](/notes/sts2-modding-pitfalls/)**。本页只描述**技能树 C# 规则与视图分工**。

:::
