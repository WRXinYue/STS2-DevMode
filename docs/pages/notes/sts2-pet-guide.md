---
title:
  en: STS2 mod pets (advanced notes)
  zh-CN: Mod 宠物开发手册（进阶）
top: 10007
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

::: en

`.tscn` layout for `NCreatureVisuals`, Harmony patch for `CreateVisuals`, pet positioning and HP bar. Uses generic `your-mod` paths. **Body in Chinese.**

:::

::: zh-CN

本文说明如何在 **STS2 模组**中创建可召唤的 mod 宠物（Pet），包括场景结构、视觉加载、定位和血条显示。路径与类名均为占位，请替换为你自己的 mod。

---

## 1. 场景文件（`.tscn`）

### 结构

场景根节点使用 `Node2D`（不挂脚本），包含以下 unique name 子节点：

| 子节点名 | 类型 | `unique_name_in_owner` | 用途 |
|---|---|---|---|
| `Visuals` | `AnimatedSprite2D` 或 `SpineSprite` | ✓ | Body，播放 idle/attack/hurt/die 动画 |
| `Bounds` | `Control` | ✓ | 碰撞/悬停区域，决定 Hitbox 和血条宽度 |
| `CenterPos` | `Marker2D` | ✓ | VFX 生成位置 |
| `IntentPos` | `Marker2D` | ✓ | 意图图标位置 |
| `OrbPos` | `Marker2D`（可选） | ✓ | 宝珠位置，缺失时引擎用 IntentPos |
| `TalkPos` | `Marker2D`（可选） | ✓ | 对话气泡位置 |

`NCreatureVisuals._Ready()` 通过 `%` 查找（`GetNode<T>("%Name")`）获取这些节点，`unique_name_in_owner = true` 必须设置。

### 参考尺寸（官方宠物风格）

```
Visuals: position=(-19,-23), scale=(0.26,0.26)  # SpineSprite
Bounds:  offset_left=-116, offset_top=-204, offset_right=116, offset_bottom=0  # 232×204
CenterPos: position=(-15,-109)
IntentPos: position=(0,-218)
```

文件位置示例：`your-mod-patch/scenes/creature_visuals/my_pet.tscn`

---

## 2. MonsterModel

继承 `MonsterModel`，定义 HP、动画状态机、召唤后的初始 Power 等。

```csharp
public sealed class MyPet : MonsterModel
{
    public override int MinInitialHp => 40;
    public override int MaxInitialHp => 40;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<MinionPower>(Creature, 1m, Creature, null);
        // ... 其他 Power
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine() { /* ... */ }
    public override CreatureAnimator GenerateAnimator(MegaSprite controller) { /* ... */ }
}
```

### 动画状态

`GenerateAnimator` 中使用的动画名必须与 `.tscn` 的 `SpriteFrames` 动画名一致：

| 动画名 | 用途 | loop |
|---|---|---|
| `idle` / `idle_loop` | 待机 | ✓ |
| `idle_damaged` | 受伤待机（可选，HP 切换用） | ✓ |
| `attack` | 攻击 | ✗ |
| `hurt` | 受击 | ✗ |
| `die` | 死亡 | ✗ |
| `dead_loop` | 死亡后循环 | ✓ |

---

## 3. 视觉加载（Harmony Patch）

### 为什么需要 Patch

`MonsterModel.CreateVisuals()` 调用 `PackedScene.Instantiate<NCreatureVisuals>()`，要求场景根节点类型是 `NCreatureVisuals`。但 Godot 4 不自动扫描 mod DLL 的 `ScriptPathAttribute`，场景根节点只能是 `Node2D`，导致转型失败。且 `CreateVisuals()` 不是 virtual，无法 override。

### 实现方式

通过 Harmony Prefix 拦截 `MonsterModel.CreateVisuals()`，手动完成节点迁移：

```csharp
var source = scene.Instantiate();
var visuals = new NCreatureVisuals();
visuals.Name = source.Name;
foreach (var child in new List<Node>(source.GetChildren()))
{
    source.RemoveChild(child);
    visuals.AddChild(child);
    child.Owner = visuals;
}
source.QueueFree();
```

**关键**：`child.Owner = visuals` 必须设置，否则 `NCreatureVisuals._Ready()` 的 `%Visuals`、`%Bounds` 等查找会失败。

### Prefix 阶段节点状态

Prefix 中创建的 `NCreatureVisuals` 尚未进入场景树（`_Ready()` 未执行），`Body` 等属性可能为 null。若需在 Prefix 中操作节点，直接遍历子节点查找。

### 注册新宠物

在你维护的 `ScenePaths` 字典中增加：`类名 → res:// 场景路径`。

### 调用链

```
卡牌.OnPlay()
  → PlayerCmd.AddPet<T>()
    → CreatureCmd.Add()
      → NCreature.Create()
        → Creature.CreateVisuals()
          → MonsterModel.CreateVisuals()  ← Harmony Prefix 在此拦截
```

---

## 4. 宠物定位（Harmony Patch）

### 引擎默认行为

`NCombatRoom.AddCreature()` 对非 Osty 类宠物可能堆叠在玩家前方并**隐藏血条**（`ToggleIsInteractable(false)`）。mod 若需要独立位置与可见血条，可在 Postfix 中重新定位并 `ToggleIsInteractable(true)`。

### 注册

在自定义 `HashSet<string>` 或等价结构中登记你的 `MonsterModel` 类名。

### 定位参数（示例）

| 参数 | 说明 |
|---|---|
| 偏移 | 相对玩家节点，向右/向上为常见友方宠物摆放 |
| 多宠物 | 可用固定间距递增 |

---

## 5. HP 视觉切换（可选）

若使用**主 mod + 补丁 mod** 双 PCK：可通过同一路径场景覆盖、运行时 `GD.Load` 补纹理等方式切换立绘/动画。注意 Godot 导出 PCK 时**只打包本项目内资源**，跨项目引用须在运行时加载。

---

## 6. 添加新宠物的步骤清单

1. 准备动画素材与 `.tscn`（符合上文节点结构）。
2. 实现 `MonsterModel` 子类。
3. 实现召唤卡牌：`PlayerCmd.AddPet<T>()`。
4. 注册视觉 Patch 的场景映射。
5. 若需要：注册定位 / 血条 Patch。
6. 用 **DevMode** 等工具实机验证。

---

## 7. 引擎约束（摘要）

| 约束 | 应对 |
|---|---|
| `CreateVisuals` 非 virtual | Harmony Prefix |
| 场景根不能是 mod C# 类型 | 根节点 `Node2D` + Patch 迁移到 `NCreatureVisuals` |
| 非 Osty 宠物血条可能被隐藏 | Postfix 中 `ToggleIsInteractable(true)` |
| Prefix 阶段属性未初始化 | 子节点遍历 |

---

## 8. 游戏源码参考

| 类 | 命名空间 |
|---|---|
| `MonsterModel` | `MegaCrit.Sts2.Core.Models` |
| `NCreatureVisuals` | `MegaCrit.Sts2.Core.Nodes.Combat` |
| `NCreature` | `MegaCrit.Sts2.Core.Nodes.Combat` |
| `NCombatRoom` | `MegaCrit.Sts2.Core.Nodes.Rooms` |
| `Osty` | `MegaCrit.Sts2.Core.Models.Monsters` |

:::
