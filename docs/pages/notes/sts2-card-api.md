---
title:
  en: STS2 card API (notes)
  zh-CN: STS2 卡牌 API 参考
top: 10001
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

::: en

Reference notes on DynamicVar, Power, piles, orbs, and common command helpers. Based on STS2 game API behavior and common mod patterns. **Body in Chinese.**

:::

::: zh-CN

> 基于 STS2 游戏 API 与社区模组实践整理。涵盖 DynamicVar、Power、CardPileCmd、OrbCmd 等卡牌开发常用接口。

---

## 目录

1. [DynamicVar — 卡牌数值](#dynamicvar--卡牌数值)
2. [Power — 状态效果](#power--状态效果)
3. [CardPileCmd — 牌堆操作](#cardpilecmd--牌堆操作)
4. [OrbCmd — 法球操作](#orbcmd--法球操作)
5. [CommonActions — 常用动作组合](#commonactions--常用动作组合)
6. [CreatureCmd — 直接生物操作](#creaturecmd--直接生物操作)
7. [升级模式速查](#升级模式速查)

---

## DynamicVar — 卡牌数值

### 内置具名类型

| 类 | 访问器 | 内部 key | 用途 |
|----|--------|----------|------|
| `DamageVar(n, ValueProp.Move)` | `DynamicVars.Damage` | `"Damage"` | 攻击伤害 |
| `BlockVar(n, ValueProp.Move)` | `DynamicVars.Block` | `"Block"` | 格挡 |
| `CardsVar(n)` | `DynamicVars.Cards` | `"Cards"` | 抽牌数 |
| `HealVar("key", n)` | `DynamicVars["key"]` | 自定义 | 治疗量 |
| `HpLossVar(n)` | `DynamicVars.HpLoss` | `"HpLoss"` | 生命损耗 |
| `PowerVar<T>(n)` | 见下表 | `typeof(T).Name` | Power 层数 |

### PowerVar 访问器速查

| PowerVar 类型 | 访问器 | 内部 key |
|---------------|--------|----------|
| `PowerVar<WeakPower>` | `DynamicVars.Weak` | `"WeakPower"` |
| `PowerVar<VulnerablePower>` | `DynamicVars.Vulnerable` | `"VulnerablePower"` |
| `PowerVar<StrengthPower>` | `DynamicVars.Strength` | `"StrengthPower"` |
| `PowerVar<PoisonPower>` | `DynamicVars.Poison` | `"PoisonPower"` |
| `PowerVar<DexterityPower>` | `DynamicVars.Dexterity` | `"DexterityPower"` |

> **规律**：`PowerVar<T>` 的内部 key 就是类型全名（如 `"WeakPower"`），`DynamicVars` 的访问器属性名去掉 `Power` 后缀。

### 自定义数值

```csharp
// 声明
new DynamicVar("MyKey", 5)

// 读取
int val = DynamicVars["MyKey"].IntValue;
decimal val = DynamicVars["MyKey"].BaseValue;

// 升级
DynamicVars["MyKey"].UpgradeValueBy(2m);   // +2
DynamicVars["MyKey"].UpgradeValueBy(-5m);  // -5（允许负值）
DynamicVars["MyKey"].BaseValue = 3m;        // 直接赋值（用于非线性升级）
```

---

## Power — 状态效果

### 常用 Vanilla Power 类

| 类名 | 效果 | 类型 |
|------|------|------|
| `WeakPower` | 虚弱：造成伤害 -25% | Debuff/Counter |
| `VulnerablePower` | 易伤：受到伤害 +50% | Debuff/Counter |
| `StrengthPower` | 力量：造成伤害 +n | Buff/Counter |
| `DexterityPower` | 敏捷：获得格挡 +n | Buff/Counter |
| `PoisonPower` | 毒：回合开始受毒伤 | Debuff/Counter |

### TemporaryStrengthPower 体系

`TemporaryStrengthPower` 是**抽象类**，在所属生物**该侧**回合结束时移除，并通过与底层 `StrengthPower` 的成对施加撤销本回合的数值变化。子类需重写 `OriginModel`（UI 来源）；**临时加力量**用默认 `IsPositive`；**本回合降低力量**（等价于尖啸类效果）需 `protected override bool IsPositive => false`，并对目标施加**正数**层数（引擎内部再映射到对 `StrengthPower` 的负向修正）。

```csharp
// Vanilla：临时加力量
public class FlexPotionPower : TemporaryStrengthPower { ... }

// Vanilla：本回合降低力量（负向临时）
public class PiercingWailPower : TemporaryStrengthPower
{
    protected override bool IsPositive => false;
}
```

> **坑**：不能直接实例化抽象类 `TemporaryStrengthPower`，须为每种来源建**独立子类**并在 mod 中注册。卡面写「本回合」加/减力量时，应使用子类 + `TemporaryStrengthPower`，**不要**对敌人用负向 `StrengthPower`（那会永久改层数，直到战斗结束或其它效果抵消）。自定义临时力量类请放在本 mod 命名空间下并注册。

### 施加 Power

```csharp
// 对敌人施加
await PowerCmd.Apply<WeakPower>(enemy, amount, Owner.Creature, this);
await PowerCmd.Apply<VulnerablePower>(enemy, amount, Owner.Creature, this);

// 对自己施加（示例：自定义 Power 类型）
await CommonActions.ApplySelf<MyModPower>(this, amount);
// 等价于：
await PowerCmd.Apply<MyModPower>(Owner.Creature, amount, Owner.Creature, this);

// 修改层数
await PowerCmd.ModifyAmount(power, -6, null, null);  // 减少 6 层

// 读取当前层数
var power = Owner.Creature.Powers.OfType<MyModPower>().FirstOrDefault();
decimal amount = power?.Amount ?? 0;
```

---

## CardPileCmd — 牌堆操作

### 抽牌

```csharp
await CardPileCmd.Draw(choiceContext, count, Owner);
```

### 在战斗中生成并加入牌堆

```csharp
// 1. 由 CombatState 创建牌实例（Owner 绑定到当前玩家）
var card = combat.CreateCard<MyCard>(Owner);

// 2. 若需同步升级状态（注意：方法名是 UpgradeInternal，不是 Upgrade）
if (IsUpgraded) card.UpgradeInternal();

// 3. 加入指定牌堆
await CardPileCmd.AddGeneratedCardToCombat(
    card,
    PileType.Draw,            // 抽牌堆
    addedByPlayer: true,
    CardPilePosition.Random   // 随机位置洗入
);

// 加入手牌
await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
```

### PileType 枚举

| 值 | 含义 |
|----|------|
| `PileType.Draw` | 抽牌堆（战斗中循环） |
| `PileType.Hand` | 手牌 |
| `PileType.Discard` | 弃牌堆 |
| `PileType.Exhaust` | 消耗区 |
| `PileType.Deck` | 永久牌组（非战斗） |

### 获取牌堆内容

```csharp
// 遍历抽牌堆中所有牌（示例：按类型过滤）
var drawPile = PileType.Draw.GetPile(Owner);
int count = drawPile.Cards.Count(c => c is MyTagCard);
```

---

## OrbCmd — 法球操作

```csharp
// 召唤（Channel）一个 Orb
await OrbCmd.Channel<MyOrb>(choiceContext, Owner);

// 激发最后一个 Orb
await OrbCmd.EvokeLast(choiceContext, Owner.Player);

// 触发被动
await OrbCmd.Passive(choiceContext, orb, null);
```

### 读取当前持有的 Orb

> **注意**：卡牌中 `Owner` 本身即 `Player` 类型，**不要**写 `Owner.Player`（会报 CS1061）。

```csharp
// 全部 Orb 数量（卡牌上下文）
int total = Owner.PlayerCombatState?.OrbQueue?.Orbs?.Count ?? 0;

// 若使用社区扩展方法（如 RitsuLib），可能有 GetOrbCount<T>() 等封装；否则自行遍历 OrbQueue。
```

### 自定义 Orb 最小实现

```csharp
public sealed class MyOrb : ModOrbTemplate
{
    private const decimal AttackPower = 3m;

    public override OrbAssetProfile AssetProfile => new(
        IconPath: "res://images/orbs/my_orb.png",
        VisualsScenePath: "res://scenes/orbs/orb_visuals/lightning_orb.tscn");

    public override decimal PassiveVal => ModifyOrbValue(AttackPower);
    public override decimal EvokeVal   => ModifyOrbValue(AttackPower);

    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext ctx)
    {
        var enemies = CombatState.HittableEnemies.Where(e => e.IsHittable).ToList();
        if (EvokeVal <= 0 || enemies.Count == 0) return Array.Empty<Creature>();
        PlayEvokeSfx();
        await CreatureCmd.Damage(ctx, enemies, EvokeVal, ValueProp.Unpowered, Owner.Creature);
        return enemies;
    }
}
```

> **注意**：`PassiveVal`/`EvokeVal` 中必须用 `ModifyOrbValue()` 包装，否则不会被 Focus 等效果缩放。

---

## CommonActions — 常用动作组合

```csharp
// 攻击单个目标（从 CardPlay.Target 取目标）
await CommonActions.CardAttack(this, play).Execute(choiceContext);

// 攻击指定目标
await CommonActions.CardAttack(this, play.Target, damage).Execute(choiceContext);

// 格挡
await CommonActions.CardBlock(this, play);

// 对自己施加 Power
await CommonActions.ApplySelf<MyModPower>(this, amount);
```

---

## CreatureCmd — 直接生物操作

```csharp
// 对单个敌人造成伤害
await CreatureCmd.Damage(choiceContext, enemy, amount, ValueProp.Move, creature, card);

// 对多个敌人造成伤害（传 List）
await CreatureCmd.Damage(choiceContext, enemies, amount, ValueProp.Unpowered, creature);

// 获得格挡
await CreatureCmd.GainBlock(Owner.Creature, amount, ValueProp.Move, play);

// 治疗
await CreatureCmd.Heal(Owner.Creature, amount);
```

### ValueProp 选择

| 值 | 含义 |
|----|------|
| `ValueProp.Move` | 受力量/敏捷等缩放（标准攻击/格挡） |
| `ValueProp.Unpowered` | 不受缩放（固定伤害，如 Orb 激发） |

---

## 升级模式速查

```csharp
// 数值增加
DynamicVars.Damage.UpgradeValueBy(2m);
DynamicVars.Block.UpgradeValueBy(3m);
DynamicVars["MyKey"].UpgradeValueBy(5m);

// 数值减少（负值升级）
DynamicVars.Damage.UpgradeValueBy(-5m);

// 费用变化
EnergyCost.UpgradeBy(-1);   // 费用 -1

// 关键词增删
AddKeyword(CardKeyword.Exhaust);
RemoveKeyword(CardKeyword.Exhaust);
AddKeyword(CardKeyword.Retain);
AddKeyword(CardKeyword.Innate);   // 固有

// 条件逻辑（在 OnPlay 中判断）
if (IsUpgraded) { /* 升级后的额外效果 */ }
```

:::
