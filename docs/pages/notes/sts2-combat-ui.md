---
title:
  en: STS2 combat UI (notes)
  zh-CN: STS2 战斗 UI 技术笔记
top: 10005
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

::: en

Power/relic icon path generation, custom energy icons, custom resource bars, hover tooltips vs hitboxes. Examples use a placeholder mod folder `mymod/`. **Body in Chinese.**

:::

::: zh-CN

## 1. Power / 遗物图标路径生成机制

### 路径生成流程（常见实现）

```
类名 (e.g. MyExamplePower)
  → ModelAssets.SnakeCaseLowerTypeName(GetType())
      → PascalCase 在大写字母前插 '_' 再整体小写   → my_example_power
      → res://images/powers/mymod/my_example_power.png
      → res://images/powers/mymod/big/my_example_power.png
```

遗物同理：`res://images/relics/mymod/{stem}.png` 与 `big/` 下大图。

### 关键点

- 与项目内资源约定一致：**snake_lower + 下划线** 文件名。
- 若干 Power 共用一张图时可在子类中写死常量路径，不经过 `SnakeCaseLowerTypeName`。

---

## 2. 卡牌能量图标（EnergyIcon）系统

### 常见可覆写成员

| 属性 | 说明 |
| --- | --- |
| `BigEnergyIconPath` | 卡牌右上角能量球图标路径（对应 `card.tscn/EnergyIcon` TextureRect，默认约 64×64） |
| `TextEnergyIconPath` | 卡牌描述文本内联图标路径（`[img]...[/img]`） |
| `EnergyColorName` | 控制图标来源的 key；自定义时由基类生成复合 key |

### 关键规则

- 若你的工程使用 Harmony 等补丁替换自定义能量色，**避免**在卡池中随意 override `EnergyColorName` 导致补丁链被绕过。
- 图标文件放在 `images/cards/<modId>/`，与卡牌资源统一管理。
- `TextEnergyIconPath` 不需要时返回 `null`。

### 配置示例

```csharp
// src/Character/MyCardPool.cs
public override string? BigEnergyIconPath => "res://images/cards/mymod/energy_icon.png";
public override string? TextEnergyIconPath => null;
```

### EnergyIcon 尺寸调整

可通过 Patch `NCard.Reload` 动态修改 `EnergyIcon` 节点 offset（数值按你的美术资源调整）：

```csharp
energyIcon.OffsetLeft   = -170.5f;
energyIcon.OffsetTop    = -231.5f;
energyIcon.OffsetRight  = -97.5f;
energyIcon.OffsetBottom = -158.5f;
```

---

## 3. 自定义进度条（示例：资源条 Control）

### 架构示例

```
NCreature（玩家）
  └── NMyResourceBar (Control, anchor 0.5/0.5)
        ├── frameRect  (TextureRect，框图，铺满)
        └── _fillClip  (Control, ClipContents=true，宽度按比例裁剪)
              └── _fillRect (TextureRect，数值图，始终全宽)
```

### 裁剪原理

```csharp
float ratio     = Mathf.Clamp(amount / maxAmount, 0f, 1f);
float fillWidth = BarWidth * ratio;
_fillClip.Size        = new Vector2(fillWidth, BarHeight);
_fillClip.OffsetRight = fillWidth;
```

数值图始终等于 `BarWidth`，`_fillClip` 的 `ClipContents` 将超出部分裁掉，实现进度条效果。

### 位置与缩放

```csharp
// 每帧更新（_Process）
float topY   = _nCreature.IntentContainer.OffsetBottom - 5f;
Scale        = _nCreature.Visuals.Scale;
```

- `IntentContainer.OffsetBottom`：头顶位置参考。
- `Visuals.Scale`：角色被 debuff 缩小时条可同步缩放。

### 事件订阅

```csharp
_creature.PowerApplied   += OnPowerChanged;
_creature.PowerIncreased += OnPowerIncreased;
_creature.PowerDecreased += OnPowerDecreased;
_creature.PowerRemoved   += OnPowerChanged;
```

### Hover Tooltip

进度条不依赖 Godot 的 `MouseEntered` 信号（可能被 `NCreature.Hitbox` 拦截），可改用 `_Process` 每帧轮询：

```csharp
bool over = new Rect2(Vector2.Zero, Size).HasPoint(GetLocalMousePosition());
```

**退出防抖**：连续多帧判定为「离开」再触发退出，避免布局延迟引起的单帧误判。

**Tooltip 创建**：

```csharp
NHoverTipSet.CreateAndShow(this, tip, HoverTip.GetHoverTipAlignment(_nCreature));
```

- owner 建议用**条控件自身**；若用 `_nCreature` 可能与游戏 hover 注册冲突。
- 若 Power 不可见导致 `HoverTips` 为空，需手动构造 `HoverTip` 与本地化字符串。

---

## 4. NCreature 结构速查

| 属性 | 类型 | 说明 |
| --- | --- | --- |
| `Entity` | `Creature` | 游戏逻辑实体（含 Power、HP 等） |
| `Visuals` | `NCreatureVisuals` | 角色视觉节点，`Scale` 随 debuff 变化 |
| `IntentContainer` | `Control` | 意图图标容器，`OffsetTop/Bottom` 标定头顶位置 |
| `Hitbox` | `Control` | 碰撞区域 |

`IntentContainer` 位置由 `BoundsUpdated` 等事件动态维护，缩放后自动更新。

:::
