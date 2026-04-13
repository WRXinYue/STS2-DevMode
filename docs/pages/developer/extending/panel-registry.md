---
title:
  en: Dev panel registry
  zh-CN: 开发者面板注册
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Registering a rail tab{lang="en"}

## 注册轨道标签页{lang="zh-CN"}

::: en
Use `DevMode.UI.DevPanelRegistry` from a mod that references the **DevMode** assembly. Prefer **`RegisterPanelWhenReady(Action)`** (or equivalently **`DevMode.Modding.ModRuntime.RegisterAfterAllModsLoaded`**) so your code runs **after** all `[ModInitializer]` entries and **before** `LocManager.Initialize`.

During a run, follow the same **browser rail** pattern as built-in panels: `DevPanelModApi.CreateBrowserPanel`, `CreateBrowserBackdrop`, `PinRail` / `SpliceRail`. The root control name **must start with `DevMode`** so overlays can be closed when switching tabs.
:::

::: zh-CN
在引用 **DevMode** 程序集的 mod 中使用 `DevMode.UI.DevPanelRegistry`。优先使用 **`RegisterPanelWhenReady(Action)`** (或等价的 **`DevMode.Modding.ModRuntime.RegisterAfterAllModsLoaded`**)，使代码在所有 `[ModInitializer]` 之后、**`LocManager.Initialize` 之前**运行。

对局中请沿用内置面板的 **browser rail** 约定：`DevPanelModApi.CreateBrowserPanel`、`CreateBrowserBackdrop`、`PinRail` / `SpliceRail`。根控件名称 **必须以 `DevMode` 开头**，以便切换标签时正确关闭浮层。
:::

## Full example{lang="en"}

## 完整示例{lang="zh-CN"}

::: en

```csharp
using DevMode.Icons;
using DevMode.UI;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

DevPanelRegistry.RegisterPanelWhenReady(() =>
{
    DevPanelRegistry.Register(
        id:    "mymod.debug",
        icon:  MdiIcon.Bug,
        name:  "My Debug Panel",
        order: 350,
        group: DevPanelTabGroup.Primary,
        onActivate: globalUi =>
        {
            var rootName = "DevModeMyModDebug";
            void Remove() => ((Node)globalUi).GetNodeOrNull<Control>(rootName)?.QueueFree();

            Remove();
            DevPanelModApi.PinRail();
            DevPanelModApi.SpliceRail(globalUi, joined: true);

            var root = new Control
            {
                Name = rootName,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ZIndex = 1250,
            };
            root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.TreeExiting += () =>
            {
                DevPanelModApi.UnpinRail();
                DevPanelModApi.SpliceRail(globalUi, joined: false);
            };

            root.AddChild(DevPanelModApi.CreateBrowserBackdrop(Remove));
            var panel = DevPanelModApi.CreateBrowserPanel(520f);
            root.AddChild(panel);

            var content = panel.GetNode<VBoxContainer>("Content");
            // ... build your UI here ...

            ((Node)globalUi).AddChild(root);
        },
        onDeactivate: globalUi =>
            ((Node)globalUi).GetNodeOrNull<Control>("DevModeMyModDebug")?.QueueFree()
    );
});
```

Alternatively, implement `IDevPanelTab` for full control:

```csharp
public class MyTab : IDevPanelTab
{
    public string Id => "mymod.tab";
    public MdiIcon Icon => MdiIcon.Star;
    public string DisplayName => "My Tab";
    public int Order => 250;
    public DevPanelTabGroup Group => DevPanelTabGroup.Primary;

    public void OnActivate(NGlobalUi globalUi) { /* open your UI */ }
    public void OnDeactivate(NGlobalUi globalUi) { /* cleanup */ }
}

DevPanelRegistry.Register(new MyTab());
```

:::

::: zh-CN

```csharp
using DevMode.Icons;
using DevMode.UI;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

DevPanelRegistry.RegisterPanelWhenReady(() =>
{
    DevPanelRegistry.Register(
        id:    "mymod.debug",
        icon:  MdiIcon.Bug,
        name:  "我的调试面板",
        order: 350,
        group: DevPanelTabGroup.Primary,
        onActivate: globalUi =>
        {
            var rootName = "DevModeMyModDebug";
            void Remove() => ((Node)globalUi).GetNodeOrNull<Control>(rootName)?.QueueFree();

            Remove();
            DevPanelModApi.PinRail();
            DevPanelModApi.SpliceRail(globalUi, joined: true);

            var root = new Control
            {
                Name = rootName,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ZIndex = 1250,
            };
            root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.TreeExiting += () =>
            {
                DevPanelModApi.UnpinRail();
                DevPanelModApi.SpliceRail(globalUi, joined: false);
            };

            root.AddChild(DevPanelModApi.CreateBrowserBackdrop(Remove));
            var panel = DevPanelModApi.CreateBrowserPanel(520f);
            root.AddChild(panel);

            var content = panel.GetNode<VBoxContainer>("Content");
            // ... 在此构建自定义 UI ...

            ((Node)globalUi).AddChild(root);
        },
        onDeactivate: globalUi =>
            ((Node)globalUi).GetNodeOrNull<Control>("DevModeMyModDebug")?.QueueFree()
    );
});
```

也可以实现 `IDevPanelTab` 接口获得完全控制：

```csharp
public class MyTab : IDevPanelTab
{
    public string Id => "mymod.tab";
    public MdiIcon Icon => MdiIcon.Star;
    public string DisplayName => "我的标签页";
    public int Order => 250;
    public DevPanelTabGroup Group => DevPanelTabGroup.Primary;

    public void OnActivate(NGlobalUi globalUi) { /* 打开你的 UI */ }
    public void OnDeactivate(NGlobalUi globalUi) { /* 清理资源 */ }
}

DevPanelRegistry.Register(new MyTab());
```

:::

## Icons{lang="en"}

## 图标{lang="zh-CN"}

::: en
DevMode bundles [Material Design Icons](https://pictogrammers.com/library/mdi/) via the `MdiIcon` struct (`DevMode.Icons` namespace). Pre-defined fields use PascalCase:

```csharp
MdiIcon.Bug           // "bug"
MdiIcon.Star          // "star"
MdiIcon.Cards         // "cards"
```

To get a Godot texture: `MdiIcon.Bug.Texture(size: 20, color: Colors.White)`.

For icons not pre-defined, use the kebab-case name:

```csharp
MdiIcon.Get("account-check", size: 24);
```

> **Tree-shaking:** Only icons referenced as `MdiIcon.XxxYyy` in source are bundled at build time. Icons used only via `MdiIcon.Get("...")` must already be bundled by a static reference, or they will not be available at runtime.

See [`src/Icons/MdiIcon.cs`](src/Icons/MdiIcon.cs) for the full list of pre-defined icons.
:::

::: zh-CN
DevMode 通过 `MdiIcon` 结构体（`DevMode.Icons` 命名空间）内置了 [Material Design Icons](https://pictogrammers.com/library/mdi/) 图标集。预定义字段使用 PascalCase 命名：

```csharp
MdiIcon.Bug           // "bug"
MdiIcon.Star          // "star"
MdiIcon.Cards         // "cards"
```

获取 Godot 纹理：`MdiIcon.Bug.Texture(size: 20, color: Colors.White)`。

对于未预定义的图标，可使用 kebab-case 名称：

```csharp
MdiIcon.Get("account-check", size: 24);
```

> **Tree-shaking 机制：** 构建时仅打包源码中以 `MdiIcon.XxxYyy` 方式静态引用的图标。若通过 `MdiIcon.Get("...")` 使用动态名称，该图标必须已被某处静态引用，否则运行时不可用。

完整预定义图标列表见 [`src/Icons/MdiIcon.cs`](src/Icons/MdiIcon.cs)。
:::

## Dependencies{lang="en"}

## 依赖{lang="zh-CN"}

::: en
Add **`DevMode`** to your mod manifest **`dependencies`** so the engine loads DevMode before your mod.
:::

::: zh-CN
在 mod 清单的 **`dependencies`** 中加入 **`DevMode`**，确保引擎先于你的 mod 加载 DevMode。
:::
