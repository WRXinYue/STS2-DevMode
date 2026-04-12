# DevMode

[English](./README.md) | **中文**

适用于《杀戮尖塔 2》的开发者模式模组。

## 功能特性

- 主菜单新增开发者模式面板
- 支持自定义遗物、卡牌、金币及遭遇战，便于测试
- 敌人遭遇战系统，含统一选择界面、战斗怪物生成及待机动画预览
- 国际化支持，提供英文与简体中文本地化
- 集成 STS2AI 模组，支持 AI 控制面板、速度与动画控制
- 可扩展面板注册机制 — 其他 mod 可向 DevMode 侧边栏添加自定义标签页

## 扩展 DevMode

外部 mod 可通过 `DevPanelRegistry` 注册自定义标签页。

在 **游戏流程中**（侧栏已附着于 `NGlobalUi`）打开的自定义页，须通过 **`DevPanelModApi`** 使用与内置「控制台」等相同的 **浏览器式布局**：`CreateBrowserPanel` + `CreateBrowserBackdrop` + `PinRail` / `SpliceRail`。主菜单居中浮层由 DevMode 内部实现，**不对外暴露**。根节点名称须以 **`DevMode` 开头**，否则切换侧栏标签时 `CloseAllOverlays` 无法自动移除你的面板。

```csharp
using DevMode.Icons;
using DevMode.UI;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

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

### 推荐：`RegisterPanelWhenReady`

若在外部 mod 的 `[ModInitializer]` 里直接调用 `DevPanelRegistry.Register`，可能早于 DevMode 自身初始化完成，或与 mod 加载顺序冲突。请改用 **`DevPanelRegistry.RegisterPanelWhenReady(Action)`**：DevMode 会在 **全部 mod 初始化结束之后**、本地化初始化之前统一执行回调（由 DevMode 内置的 `LocManager.Initialize` 补丁触发），无需各 mod 自行写 Harmony。

```csharp
using DevMode.UI;

DevPanelRegistry.RegisterPanelWhenReady(() =>
{
    DevPanelRegistry.Register(
        "mymod.debug",
        MdiIcon.Bug,
        "我的调试面板",
        350,
        DevPanelTabGroup.Primary,
        onActivate: globalUi => { /* ... */ });
});
```

若你的 mod **编译期引用** DevMode，请在 manifest 的 `dependencies` 中声明 **`DevMode`**，以便引擎先加载 DevMode，再加载你的 mod（否则在 `Initialize` 里调用上述 API 时仍可能无法解析 DevMode 程序集）。

内置标签页使用 100、200、…、800 的排序值，选择中间值即可控制位置。

### 图标

DevMode 通过 `MdiIcon` 结构体（`DevMode.Icons` 命名空间）内置了 [Material Design Icons](https://pictogrammers.com/library/mdi/) 图标集。预定义字段使用 PascalCase 命名：

```csharp
MdiIcon.Bug           // "bug"
MdiIcon.Star          // "star"
MdiIcon.Cards         // "cards"
MdiIcon.Robot         // "robot"
```

获取 Godot 纹理：`MdiIcon.Bug.Texture(size: 20, color: Colors.White)`。

对于未预定义的图标，可使用 kebab-case 名称：

```csharp
MdiIcon.Get("account-check", size: 24);
```

> **Tree-shaking 机制：** 构建时仅打包源码中以 `MdiIcon.XxxYyy` 方式引用的图标。若通过 `MdiIcon.Get("...")` 使用动态名称，该图标必须已被某处静态引用，否则运行时不可用。

完整预定义图标列表见 [`src/Icons/MdiIcon.cs`](src/Icons/MdiIcon.cs)。

## 协作与贡献

协作流程、K&R 代码风格、`dotnet format` / `make format`、Python 与本地化等说明见 **[CONTRIBUTING.md](CONTRIBUTING.md)**。

## 更新日志

版本历史请参阅 [CHANGELOG.zh-CN.md](CHANGELOG.zh-CN.md)。

## 致谢

- [STS2-KaylaMod](https://github.com/mugongzi520/STS2-KaylaMod)

## 许可证

MIT
