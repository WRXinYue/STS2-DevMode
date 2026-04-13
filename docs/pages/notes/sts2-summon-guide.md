---
title:
  en: STS2 summons & minions (notes)
  zh-CN: STS2 召唤物实现指南
top: 10008
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

::: en

MonsterModel, `PlayerCmd.AddPet`, `CreatureCmd.Add`, `MinionPower`, hidden attack Power for ally AI. **Body in Chinese.**

:::

::: zh-CN

> 基于游戏 API 行为整理，适用于直接引用游戏程序集的 mod。

---

## 目录

1. [框架现状](#框架现状)
2. [核心 API](#核心-api)
3. [完整实现流程](#完整实现流程)
4. [关键坑点](#关键坑点)
5. [文件结构参考](#文件结构参考)

---

## 框架现状

常见社区框架**不一定**提供「召唤物模板类」。可用的模板往往只有卡牌、Power、角色、遗物、药水、法球等。

召唤物需要直接继承游戏原生类 `MonsterModel`，通过底层 API 手动管理。

---

## 核心 API

### 1. 注册——ModelDb 自动扫描

`ModelDb.AllAbstractModelSubtypes` 会扫描 mod 程序集中的子类型。

**结论：只要 `MonsterModel` 子类在 mod 程序集里且可被反射发现，通常无需手动注册列表。**

Model ID 生成规则（常见）：

```csharp
StringHelper.Slugify(type.Name)
// "GhostFireMonster" → "GHOST_FIRE_MONSTER"
```

场景路径生成规则（`MonsterModel` 默认）：

```csharp
protected virtual string VisualsPath =>
    SceneHelper.GetScenePath("creature_visuals/" + base.Id.Entry.ToLowerInvariant());
// → "res://scenes/creature_visuals/ghost_fire_monster.tscn"
```

---

### 2. 召唤——PlayerCmd.AddPet（玩家宠物，Ally 侧）

```csharp
await PlayerCmd.AddPet<MyMonster>(Owner);
```

内部流程概要：从注册表取可变副本 → 在玩家同侧创建 → `CreatureCmd.Add`。

适用于：**玩家侧友方单位**。

---

### 3. 召唤——CreatureCmd.Add（指定侧，通用）

```csharp
await CreatureCmd.Add(
    ModelDb.Monster<MyMinion>().ToMutable(),
    combatState,
    CombatSide.Enemy,
    combatState.Encounter.GetNextSlot(combatState)
);
```

召唤后通常对小怪施加：

```csharp
await PowerCmd.Apply<MinionPower>(creature, 1m, null, null);
```

---

### 4. MinionPower——标记为次要单位

```csharp
await PowerCmd.Apply<MinionPower>(Creature, 1m, Creature, null);
```

效果概要：不作为主要目标、死亡表现与常规 Boss 区分等（以引擎定义为准）。

---

### 5. 自动攻击——用 Power 驱动（推荐）

**Ally 侧 MonsterModel 的 MoveStateMachine 往往不会自动攻击敌方。** 若需要每回合自动攻击，可使用隐藏 Power 在 `AfterSideTurnStart` 等回调里调用 `CreatureCmd.Damage`（示例见下文完整流程）。

---

### 6. ValueProp 区别

| ValueProp | 含义 |
|-----------|------|
| `ValueProp.Move` | 标准攻击：可被格挡，受强化/弱化影响 |
| `ValueProp.Unblockable` | 不可格挡 |
| `ValueProp.Unpowered` | 不受 Power 加成影响 |

---

## 完整实现流程

### Step 1：MonsterModel

```csharp
public sealed class MyMonster : MonsterModel
{
    public override int MinInitialHp => 2;
    public override int MaxInitialHp => 2;
    public override bool HasDeathSfx => false;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<MinionPower>(Creature, 1m, Creature, null);
        await PowerCmd.Apply<MyAttackPower>(Creature, 1m, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState idle = new("IDLE", (IReadOnlyList<Creature> _) => Task.CompletedTask, new HiddenIntent());
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine(new List<MonsterState> { idle }, idle);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("idle_loop", isLooping: true);
        AnimState attack = new("attack");
        AnimState hurt = new("hurt");
        AnimState die = new("die");
        AnimState dead = new("dead_loop", isLooping: true);

        attack.NextState = idle;
        hurt.NextState = idle;
        die.NextState = dead;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Dead", die);
        return animator;
    }
}
```

### Step 2：Godot 场景

路径示例：`scenes/creature_visuals/my_monster.tscn`（与 `Id.Entry.ToLowerInvariant()` 对应）

**场景必须节点：**

| 节点名 | 类型 | 说明 |
|--------|------|------|
| `%Visuals` | `SpineSprite` 或 `AnimatedSprite2D` | 实体外观 |
| `%Bounds` | `Control` | 点击碰撞区 |
| `%CenterPos` | `Marker2D` | VFX 生成点 |
| `%IntentPos` | `Marker2D` | 意图图标位置 |

动画名需与 `CreatureAnimator` 中一致。

### Step 3：召唤卡牌

```csharp
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
{
    for (int i = 0; i < count; i++)
        await PlayerCmd.AddPet<MyMonster>(Owner);
}
```

### Step 4：本地化

`localization/zhs/monsters.json` 等：

```json
{
  "MY_MONSTER.name": "怪物名"
}
```

键名与 `Slugify(ClassName)` 规则一致。

---

## 关键坑点

### Ally 侧 MonsterModel 不会自动攻击

主动行为通常用 **Power** 实现，而不是仅依赖敌方 AI 状态机。

### ModelDb 扫描前提

类型需 `public`、非抽象，且程序集已被加载。

### MinionPower 常需手动应用

`PlayerCmd.AddPet` 后是否自动附带以引擎版本为准；稳妥做法是在 `AfterAddedToRoom` 中显式施加。

---

## 文件结构参考

```
src/
  Tokens/
    MyMonster.cs
    MyAttackPower.cs
  Cards/
    ...

scenes/
  creature_visuals/
    my_monster.tscn

images/
  tokens/
    my_monster/

localization/
  zhs/monsters.json
  eng/monsters.json
```

---

## 引擎参考类

| 类 | 说明 |
|----|------|
| `PlayerCmd` | `AddPet<T>` |
| `CreatureCmd` | `Add` |
| `MonsterModel` | 召唤物模型基类 |
| `MinionPower` | 小怪标记 |
| `ModelDb` | 注册与 ID |

:::
