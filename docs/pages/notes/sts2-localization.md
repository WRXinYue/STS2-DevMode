---
title:
  en: STS2 localization (notes)
  zh-CN: STS2 本地化规范
top: 10002
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

::: en

Key naming, interpolation, BBCode, and newline rules for STS2 mod JSON. **Body in Chinese.**

:::

::: zh-CN

## 文件结构

```text
localization/
├── eng/
│   ├── cards.json
│   ├── powers.json
│   ├── relics.json
│   ├── characters.json
│   ├── card_keywords.json
│   └── static_hover_tips.json
└── zhs/
    └── （同上）
```

每个 JSON 文件的 key 格式：`<MOD_ID>-<ENTRY_ID>.<field>`

---

## Key 命名规则

| field | 适用对象 | 说明 |
|---|---|---|
| `.title` | 全部 | 显示名称 |
| `.description` | 全部 | 静态描述（不含动态变量） |
| `.smartDescription` | Power | 含动态变量的描述，在 hover tip 中显示 |
| `.flavor` | Relic | 风味文本（斜体小字） |

> Power 若同时定义了 `description` 和 `smartDescription`，游戏优先使用 `smartDescription`。

---

## 插值变量

### 卡牌（`cards.json`）

变量来自卡牌类中定义的 `DynamicVar`，语法：`{VarName:formatter}`

| 语法 | 说明 |
|---|---|
| `{Damage:diff()}` | 显示数值，升级时高亮差异 |
| `{Block:diff()}` | 同上，适用于格挡值 |
| `{IfUpgraded:show:文本}` | 仅升级后显示文本 |

示例：

```json
"description": "造成 {Damage:diff()} 点伤害。{IfUpgraded:show:\n获得 {Block:diff()} 点[gold]格挡[/gold]。}"
```

### Power（`powers.json` smartDescription）

变量来自 Power 自身属性：

| 变量 | 说明 |
|---|---|
| `{Amount}` | 当前 power 层数 / 数值 |
| `{OwnerName}` | 持有者名称 |
| `{OnPlayer:A\|B}` | 持有者为玩家时显示 A，否则显示 B |

示例：

```json
"smartDescription": "{OnPlayer:你拥有|[gold]{OwnerName}[/gold] 拥有} [blue]{Amount}[/blue] 层。"
```

### Relic（`relics.json`）

变量来自 Relic 定义的 `DynamicVar`，语法与卡牌相同，使用 `{VarName}` 或 `{VarName:diff()}`。

---

## BBCode 标签

所有文本均通过 Godot `RichTextLabel` 渲染，支持以下标签：

| 标签 | 颜色 | 用途 |
|---|---|---|
| `[gold]...[/gold]` | 金色 | 机制关键词（格挡、消耗、能力名等） |
| `[blue]...[/blue]` | 蓝色 | 具体数值、百分比 |

> `[gold]` / `[blue]` 是游戏自定义别名，等价于对应颜色的 `[color=...]`。

**推荐约定：**

- 游戏机制术语 → `[gold]`
- 需要强调的数值 → `[blue]`
- 普通数值不加标签

---

## 卡牌关键词的自动追加机制

引擎在渲染卡牌描述时，会**自动**将 `CardModel.Keywords` 中的部分关键词拼接到描述文本的前/后——**不需要**在 description 里手写。

源码参考：`CardModel.GetDescriptionForPile()`（`CardKeywordOrder.cs`）：

```csharp
// 自动追加在描述【前面】
beforeDescription = { Ethereal, Sly, Retain, Innate, Unplayable }

// 自动追加在描述【后面】
afterDescription  = { Exhaust, Eternal }
```

### 常见陷阱：关键词重复显示

若描述文本里**手写**了这些关键词，同时卡牌的 `CanonicalKeywords`（或 `OnUpgrade()` 里 `AddKeyword`）也包含它们，游戏内会连续显示两次。

**错误示例（消耗出现两次）：**

```json
"description": "对所有敌人造成 12 点伤害。[gold]消耗[/gold]。"
```

```csharp
public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
```

**正确写法：** description 里只写效果文字，关键词交给引擎自动追加。

```json
"description": "对所有敌人造成 12 点伤害。"
```

### 升级后新增关键词

若 `OnUpgrade()` 调用 `AddKeyword(CardKeyword.Innate)` 等，引擎会在升级后自动在描述前插入「固有」。**无需**在 description 里用 `{IfUpgraded:show:\n[gold]固有[/gold]。}` 手动显示。

```csharp
// 正确：只在代码里加，description 不写
protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
```

```json
// 正确：description 不含 {IfUpgraded:show:\n[gold]固有[/gold]。}
"description": "每回合开始时，所有敌人获得 2 层[gold]敏感[/gold]。"
```

若升级后同时有**新增关键词**和**其他额外文字**，只保留额外文字部分：

```json
// 升级前后都会自动显示固有，仅保留升级额外效果文字
"description": "你的回合结束时，恢复 4 点生命。{IfUpgraded:show:\n使用时抽一张牌。}"
```

### 总结：哪些关键词不用手写

| 关键词 | 自动位置 | 触发条件 |
| --- | --- | --- |
| 虚无 Ethereal | 描述前 | `CanonicalKeywords` 包含 |
| 狡猾 Sly | 描述前 | 当回合满足条件 |
| 保留 Retain | 描述前 | `Keywords` 包含（含 `AddKeyword`） |
| 固有 Innate | 描述前 | `Keywords` 包含（含 `AddKeyword`） |
| 不可打出 Unplayable | 描述前 | `Keywords` 包含 |
| **消耗 Exhaust** | 描述后 | `Keywords` 包含（含 `AddKeyword`） |
| 永恒 Eternal | 描述后 | `Keywords` 包含 |

其他出现在描述中的词（易伤、虚弱、力量等）只是富文本高亮，**不会**自动追加，需手动写入。

---

## 换行

不同渲染路径对换行符的处理不同：

| 文件 | 换行方式 | 原因 |
|---|---|---|
| `cards.json` | `NL` | 经过 DynamicVar 卡牌格式化层，`NL` 被替换为换行 |
| `powers.json` | `\n` | 直接由 RichTextLabel 渲染，不经过卡牌格式化层 |
| `relics.json` | `\n` | 同上 |
| `static_hover_tips.json` | `\n` | 同上 |

> **`NL` 只能用于 `cards.json`。** 在其他文件中写 `NL` 会原样显示为字符串。

---

## 完整示例

### 卡牌

```json
"MYMOD-EXAMPLE_STRIKE.title": "示例打击",
"MYMOD-EXAMPLE_STRIKE.description": "造成 {Damage:diff()} 点伤害。{IfUpgraded:show:\n获得 {Block:diff()} 点[gold]格挡[/gold]。}"
```

### Power

```json
"MYMOD-EXAMPLE_BUFF_POWER.title": "示例增益",
"MYMOD-EXAMPLE_BUFF_POWER.smartDescription": "{OnPlayer:你拥有|[gold]{OwnerName}[/gold] 拥有} [blue]{Amount}[/blue] 层[gold]示例增益[/gold]。"
```

### Relic

```json
"MYMOD-EXAMPLE_RELIC.title": "示例遗物",
"MYMOD-EXAMPLE_RELIC.flavor": "风味文本。",
"MYMOD-EXAMPLE_RELIC.description": "每场战斗开始时，获得 [blue]{SpiritAmount}[/blue] 点能量与 [blue]{Block}[/blue] 点格挡。"
```

:::
