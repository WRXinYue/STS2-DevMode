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

外部 mod 可通过 `DevPanelRegistry` 注册自定义标签页：

```csharp
using DevMode.Icons;
using DevMode.UI;

DevPanelRegistry.Register(
    id:    "mymod.debug",
    icon:  MdiIcon.Bug,
    name:  "我的调试面板",
    order: 350,
    group: DevPanelTabGroup.Primary,
    onActivate: globalUi =>
    {
        var panel = DevPanelUI.CreateStandardPanel();
        var content = panel.GetNode<VBoxContainer>("Content");
        // ... 在此构建自定义 UI ...
        ((Node)globalUi).AddChild(panel);
    }
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

## 更新日志

版本历史请参阅 [CHANGELOG.zh-CN.md](CHANGELOG.zh-CN.md)。

## 致谢

- [STS2-KaylaMod](https://github.com/mugongzi520/STS2-KaylaMod)

## 许可证

MIT
