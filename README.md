# DevMode

**English** | [中文](./README.zh-CN.md)

A developer mode mod for Slay the Spire 2.

## Features

- Developer panel accessible from the main menu
- Customizable relics, cards, gold, and encounter selection for testing
- Enemy encounter system with unified select UI, combat monster spawning, and idle animation preview
- i18n support with English and Simplified Chinese localization
- STS2AI integration panel with AI control, speed, and animation controls
- Extensible panel registry — other mods can add custom tabs to the DevMode rail

## Extending DevMode

External mods can register custom tabs via `DevPanelRegistry`:

```csharp
using DevMode.Icons;
using DevMode.UI;

DevPanelRegistry.Register(
    id:    "mymod.debug",
    icon:  MdiIcon.Bug,
    name:  "My Debug Panel",
    order: 350,
    group: DevPanelTabGroup.Primary,
    onActivate: globalUi =>
    {
        var panel = DevPanelUI.CreateStandardPanel();
        var content = panel.GetNode<VBoxContainer>("Content");
        // ... build your UI here ...
        ((Node)globalUi).AddChild(panel);
    }
);
```

You can also implement `IDevPanelTab` for full control:

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

Built-in tabs use order values 100, 200, … 800 — pick values in between to control placement.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

## Acknowledgments

- [STS2-KaylaMod](https://github.com/mugongzi520/STS2-KaylaMod)

## License

MIT
