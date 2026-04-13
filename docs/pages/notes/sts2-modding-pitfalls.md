---
title:
  en: STS2 modding pitfalls (notes)
  zh-CN: STS2 模组踩坑备忘
top: 10004
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

::: en

Engine quirks: damage multipliers, generated cards and pile UI, map overlays vs hover tips, `export_presets` MSIL. **Body in Chinese.**

:::

::: zh-CN

## `PowerModel.ModifyDamageMultiplicative` 返回值是「乘数」

引擎会把各 Power 的返回值参与**连乘**（语义上等价于 `damage *= 返回值`），**不是**「返回修改后的伤害绝对值」。

### 错误写法（会导致伤害被「平方」）

满耐力时 `return amount`：若当前伤害为 7，引擎再乘一次 7 → **49**；打击 4 → **16**。易被误认为寄生虫 `InfestedPower` 或占位角色问题。

### 正确写法

- 不参与本次结算：`return 1.0m`（单位元，不改变乘积）。
- 本人出伤且要按规则打折：`return 0.4m` / `0.6m` / `0.8m` / `1.0m` 等**纯系数**。
- `dealer != Owner` 时必须 `return 1.0m`，**禁止** `return amount`。

实现时请在你自己的 Power 类中按上述语义编写，并用 DevMode 或日志验证伤害。

---

## Mod 调试模式（可选日志与 Harmony 探针）

可为你的 mod 设置**自定义环境变量**（例如 `YOUR_MOD_DEBUG`），在启动时读取：若为 `1` / `true` / `yes`（大小写不敏感），则启用 Harmony 补丁中的诊断日志、伤害管线探针等。

未设置时，补丁**不**打日志或**不**应用，避免污染正式游玩日志。

**Windows 示例（PowerShell，仅当前会话）：**

```powershell
$env:YOUR_MOD_DEBUG = "1"
# 再启动游戏
```

也可在系统/Steam 启动项对应环境中配置；具体以你的启动方式为准。

---

## 生成卡加入 Draw/Discard 堆后 UI 计数不更新

### 现象

调用 `CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, ...)` 后，战斗界面抽牌堆数字未变化。

### 根本原因

`NCombatCardPile`（抽/弃/消耗堆 UI）的计数靠 `CardAddFinished` 事件驱动（`AddCard` 方法）。
`CardPileCmd.Add` 内部对**新生成卡**（`oldPile == null`）不走 VFX 流程，因此 `InvokeCardAddFinished()` 从未被调用，计数器无法更新。

### 正确写法

官方卡牌（守墓人系列：`CaptureSpirit`、`Dirge`、`GraveWarden` 等）的固定模式：

```csharp
CardCmd.PreviewCardPileAdd(
    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, addedByPlayer: true, CardPilePosition.Random));
```

`PreviewCardPileAdd` 启动飞行动画（`NCardFlyVfx`），动画落点时调用 `pile.InvokeCardAddFinished()` → `NCombatCardPile.AddCard` → 计数 +1 并触发 bump 动画。

### 适用范围

**所有**往 Draw/Discard/Exhaust 堆加入生成卡的调用处都需要补调 `CardCmd.PreviewCardPileAdd`。
加入 Hand 的不需要（手牌无此计数器）。

### 自查

若你的 mod 在多处调用 `AddGeneratedCardToCombat`，请逐一核对是否已配对 `PreviewCardPileAdd`。

---

## 在 NMapScreen 上叠加全屏 UI（技能树、自定义面板等）

技能树模块的规则边界、`ISkillTreeRules` 与调用约定见 **[技能树（示例架构）](/notes/sts2-skill-tree/)**。本节只写 **Godot 节点与引擎悬停 UI 的叠层、输入与视口适配**。

### 现象（把 FullRect Control 直接挂在 NMapScreen 下）

在 `NMapScreen._Ready()` 后置补丁里把 `Control`（FullRect）**直接**加为 NMapScreen 子节点时：

- **按钮**：能看见，但**点击没有反应**（`Pressed` 不触发）。
- **全屏面板**：`Visible = true` 后**有时**像不在屏幕上（与地图内部滚动/变换有关）。

### 根本原因（输入与坐标）

1. **输入被地图子树抢走**：地图节点、滚动区域等先处理鼠标，`TextureButton` 可能永远收不到事件。
2. **地图画布变换**：同画布下 FullRect 的实际绘制区域可能相对视口偏移。

常见应对是 **中间加一层**（见下），把 UI 与地图子树在结构或画布层上分开。

### 官方悬停气泡画在哪（与 CanvasLayer 的冲突）

引擎里 **`NHoverTipSet.CreateAndShow`** 会把实例加到 **`NGame.Instance.HoverTipsContainer`**（`game.tscn` 里 `Game` 节点的 **`%HoverTipsContainer`**，在**默认画布**上，**不是** `GlobalUi` 子节点）。

在 **Godot 4** 中：**嵌套的 `CanvasLayer`（即便 `layer = 0`）会绘制在「未放入 CanvasLayer 的默认画布内容」之上**。若全屏 mod UI 包在 **`CanvasLayer`** 里，整块会压在 **`HoverTipsContainer`** 之上，表现就是：**日志里 `NHoverTipSet` 已创建，但屏上看不见气泡**。

因此一种做法是：在 `NMapScreen` 下挂 **`Control`** 全屏容器（`FullRect`、`MouseFilter = Ignore`），**不**再包 **`CanvasLayer`**，使自定义 UI 与地图同属默认画布叠层关系，`HoverTipsContainer` 仍可按 `game.tscn` 中相对 `RootSceneContainer` 的兄弟顺序画在整局内容之上。

若你做的全屏 UI **不需要**官方 `NHoverTipSet` 叠在上面，仍可选用 **`CanvasLayer`** 规避地图输入/变换问题；注意 **`CanvasLayer` 子控件**往往仍要用下面的 **`FitControlToViewport`**（`CanvasLayer` 不是 `Control`，子节点 FullRect 锚点行为与挂在 `Control` 父级时不同）。

### 解决方案：中间容器 + `FitControlToViewport`

**示意**（命名请按你的 mod 修改）：

```csharp
var uiLayer = new Control
{
    Name = "MyModMapOverlay",
    MouseFilter = Control.MouseFilterEnum.Ignore,
};
uiLayer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
map.AddChild(uiLayer);

uiLayer.AddChild(skillTreeScreen);
FitControlToViewport(skillTreeScreen, map);

// 入口按钮：锚在右下角，仍加在 uiLayer 上
uiLayer.AddChild(btn);
```

**若仍使用 `CanvasLayer`**（其他 UI，且不关心或自行处理悬停叠层）：

```csharp
var uiLayer = new CanvasLayer { Layer = 0 }; // 或按需调整；layer 过高会盖住 NHoverTipSet，见上文
map.AddChild(uiLayer);
uiLayer.AddChild(screen);
FitControlToViewport(screen, map);
uiLayer.AddChild(btn);
```

`FitControlToViewport`：锚点全部置 0，用 `OffsetRight / OffsetBottom` 填**视口**像素宽高（子控件在 `CanvasLayer` 下尤其需要，因父节点无 Control 矩形）：

```csharp
static void FitControlToViewport(Control ctrl, Node refNode)
{
    var size = refNode.GetViewport().GetVisibleRect().Size;
    ctrl.AnchorLeft = ctrl.AnchorTop = ctrl.AnchorRight = ctrl.AnchorBottom = 0;
    ctrl.OffsetLeft = ctrl.OffsetTop = 0;
    ctrl.OffsetRight  = size.X;
    ctrl.OffsetBottom = size.Y;
}
```

### 注意事项

| 要点 | 说明 |
| ---- | ---- |
| 中间 `Control` / `CanvasLayer` 的 `Visible` | 往往需订阅 `map.VisibilityChanged` 同步 `uiLayer.Visible = map.IsVisibleInTree()`，避免非地图界面仍显示；**收起全屏 UI**须用 `NMapScreen.Closed`，见下文「关地图时收起」 |
| Harmony Postfix | `NMapScreen._Ready()` 无参数时实例**必须**名为 `__instance` |
| `TextureButton.IgnoreTextureSize = true` | Godot 4 下最小尺寸可为 0，需 `CustomMinimumSize` 保证命中框 |
| 窗口尺寸变化 | 订阅 `Viewport.SizeChanged` 重算 `FitControlToViewport`；`map.TreeExiting` 时取消订阅 |

### 关地图时收起全屏 UI：`NMapScreen.Closed` 与层显隐

官方 `NMapScreen.Close()` 在 **`EmitSignalClosed()`（`Closed` 信号）之后**才执行 `AnimClose()`；有关闭动画时，`Visible` 要等 tween 结束才变为 `false`。这段「逻辑上已关、画面上仍可见」的时间里，`IsVisibleInTree()` 往往仍为真，**单靠** `VisibilityChanged` 或树可见性推断「地图已关」会**晚一拍**，表现像「下次打开地图才关面板」。

| 职责 | 推荐做法 |
| ---- | -------- |
| 收起全屏 UI | 订阅 **`NMapScreen.SignalName.Closed`**，回调里若面板仍打开则 `HideAnimated()` |
| 中间层 `uiLayer` 的 `Visible` | 仍用 **`map.VisibilityChanged`**，在回调里设 `uiLayer.Visible = map.IsVisibleInTree()`（与 `Closed` **分工**，勿混成一套推断后误把整层关掉） |
| 避免 | 用 **`NotificationVisibilityChanged`** 在首帧/时序里把 `uiLayer.Visible = false`，易误藏右下角入口按钮 |

### 参考

DevMode 全屏弹层挂在 `NGlobalUi`（`ZIndex = 1250`，`FullRect`）。mod 若能稳定拿到 `NGlobalUi` 也可采用同类挂法；地图上的自定义 UI 则多采用上文 `NMapScreen` 下挂接方式。

---

## `export_presets.cfg` 的 `binary_format/architecture` 必须为 `msil`

### 现象

Mod 在 ARM 平台（M 系列 Mac、安卓设备）上无法正常加载运行。

### 根本原因

ModTemplate 在 2025-03-15 之前生成的 `export_presets.cfg` 中，`[preset.0.options]` 下的 `binary_format/architecture` 默认值为 `x86_64`。Godot 导出 PCK 时会按此选项决定 .NET 程序集的目标架构：

- `x86_64`：只生成 x86-64 原生代码，ARM 设备无法执行。
- `msil`：保留平台无关的 MSIL 字节码，由运行时 JIT 编译为当前 CPU 架构的原生指令，兼容所有平台。

### 修复

在每个 mod 的 `export_presets.cfg` 中，将：

```ini
binary_format/architecture="x86_64"
```

改为：

```ini
binary_format/architecture="msil"
```

### 涉及文件

| 文件 | 说明 |
| ---- | ---- |
| `your-mod/export_presets.cfg` | 主 mod 导出配置 |
| `your-mod-patch/export_presets.cfg` | 若有独立 Patch mod，同样检查 |

:::
