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

See the main repository **README** for a full C# example.
:::

::: zh-CN
在引用 **DevMode** 程序集的 mod 中使用 `DevMode.UI.DevPanelRegistry`。优先使用 **`RegisterPanelWhenReady(Action)`** (或等价的 **`DevMode.Modding.ModRuntime.RegisterAfterAllModsLoaded`**)，使代码在所有 `[ModInitializer]` 之后、**`LocManager.Initialize` 之前**运行。

对局中请沿用内置面板的 **browser rail** 约定：`DevPanelModApi.CreateBrowserPanel`、`CreateBrowserBackdrop`、`PinRail` / `SpliceRail`。根控件名称 **必须以 `DevMode` 开头**，以便切换标签时正确关闭浮层。

完整 C# 示例见主仓库 **README**。
:::

## Dependencies{lang="en"}

## 依赖{lang="zh-CN"}

::: en
Add **`DevMode`** to your mod manifest **`dependencies`** so the engine loads DevMode before your mod.
:::

::: zh-CN
在 mod 清单的 **`dependencies`** 中加入 **`DevMode`**，确保引擎先于你的 mod 加载 DevMode。
:::
