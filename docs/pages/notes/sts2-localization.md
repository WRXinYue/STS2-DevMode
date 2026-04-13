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
