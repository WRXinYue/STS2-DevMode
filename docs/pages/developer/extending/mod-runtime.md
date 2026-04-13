---
title:
  en: Mod runtime API
  zh-CN: Mod 运行时 API
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## `DevMode.Modding.ModRuntime`

::: en
Public hooks for other mods:

| Member | Purpose |
| --- | --- |
| `ModRuntime.Catalog` (`IModCatalog`) | Snapshot of loaded mods (`GetSnapshot`, `GetIdSet`) backed by `ModManager.LoadedMods`. |
| `ModRuntime.RegisterAfterAllModsLoaded(Action)` | Same queue/timing as `DevPanelRegistry.RegisterPanelWhenReady`. |

`DevModeModInfo` exposes **Id**, **DisplayName**, and **Version** from each manifest.

Use this instead of re-implementing scans over `ModManager` when you need a consistent view (e.g. logging, save metadata, or UI filters).
:::

::: zh-CN
面向其他 mod 的公开钩子：

| 成员 | 作用 |
| --- | --- |
| `ModRuntime.Catalog` (`IModCatalog`) | 已加载 mod 的快照 (`GetSnapshot`、`GetIdSet`)，数据来自 `ModManager.LoadedMods`。 |
| `ModRuntime.RegisterAfterAllModsLoaded(Action)` | 与 `DevPanelRegistry.RegisterPanelWhenReady` 相同的队列与时机。 |

`DevModeModInfo` 暴露各清单中的 **Id**、**DisplayName**、**Version**。

在需要一致视图 (例如日志、存档元数据、UI 过滤) 时，请使用本 API，而不是自行反复扫描 `ModManager`。
:::
