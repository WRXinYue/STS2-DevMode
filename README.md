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

External mods can register custom tabs via `DevPanelRegistry`.

**During a run**, when the rail is attached to `NGlobalUi`, custom tab content must use the same **browser-style layout** as built-in panels (e.g. Console), via **`DevPanelModApi`**: `CreateBrowserPanel` + `CreateBrowserBackdrop` + `PinRail` / `SpliceRail`. Centered main-menu modals are internal to DevMode and are **not** exposed to mods. The root `Control` name **must start with `DevMode`** so `CloseAllOverlays` can remove it when switching rail tabs.

```csharp
using DevMode.Icons;
using DevMode.UI;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

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

### Recommended: `RegisterPanelWhenReady`

Calling `DevPanelRegistry.Register` directly from another mod’s `[ModInitializer]` can run before DevMode has finished its own startup, or fight mod load order. Use **`DevPanelRegistry.RegisterPanelWhenReady(Action)`** instead: DevMode queues the callback and runs it **once**, after every mod initializer has finished and **before** `LocManager.Initialize` (same safe point as external registration; DevMode ships a `LocManager.Initialize` Harmony prefix to flush the queue).

If your mod references DevMode at compile time, add **`DevMode`** to your manifest **`dependencies`** so the engine loads DevMode before your mod (otherwise the CLR may not resolve the DevMode assembly when your initializer runs).

Built-in tabs use order values 100, 200, … 800 — pick values in between to control placement.

### Icons

DevMode bundles [Material Design Icons](https://pictogrammers.com/library/mdi/) via the `MdiIcon` struct (`DevMode.Icons` namespace). Pre-defined fields use PascalCase:

```csharp
MdiIcon.Bug           // "bug"
MdiIcon.Star          // "star"
MdiIcon.Cards         // "cards"
MdiIcon.Robot         // "robot"
```

To get a Godot texture: `MdiIcon.Bug.Texture(size: 20, color: Colors.White)`.

For any icon not pre-defined, use the kebab-case name:

```csharp
MdiIcon.Get("account-check", size: 24);
```

> **Tree-shaking:** Only icons referenced as `MdiIcon.XxxYyy` in source code are bundled at build time. If you use `MdiIcon.Get("...")` with a dynamic name, that icon must already be bundled by a static reference somewhere, or it will not be available at runtime.

See [`src/Icons/MdiIcon.cs`](src/Icons/MdiIcon.cs) for the full list of pre-defined icons.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

## Acknowledgments

- [STS2-KaylaMod](https://github.com/mugongzi520/STS2-KaylaMod)

## License

MIT
