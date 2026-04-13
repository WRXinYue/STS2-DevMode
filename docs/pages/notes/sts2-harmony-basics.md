---
title:
  en: Harmony patching basics
  zh-CN: Harmony 补丁基础
top: 9999
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

::: en

Harmony setup for STS2 mods: **patch target** (which `MethodBase` to hook), **patch type** (Prefix / Postfix / Transpiler), **registration** (`PatchAll` or manual), **injected parameters** (`__instance`, `__result`), and maintenance risks. Upstream overview: [Harmony — Introduction](https://harmony.pardeike.net/articles/intro.html). **Full body in Chinese.**

:::

::: zh-CN

用 Harmony 写 mod 时，需要写清的不只是「patch 谁」，而是一整套**可复现的约定**。下面按常见教学顺序列出；游戏版本更新后**目标方法**可能被重命名，需用 dnSpy 等自行核对。

---

## 1. 需要一起说明白的几件事

别人能照抄你的补丁时，通常至少包括：

| 要素 | 含义 |
| --- | --- |
| **补丁目标** | 挂到哪个 **类型** 的哪个 **成员** 上：实例/静态**方法**、**构造函数**，少数情况下是 **Getter/Setter**（属性）。在 Harmony 里表现为 `MethodBase`（常由 `[HarmonyPatch(typeof(T), "Name")]` 或 `AccessTools` 解析）。 |
| **补丁类型** | **Prefix**（原逻辑之前）、**Postfix**（原逻辑之后）、**Transpiler**（改 IL）、**Finalizer**（异常处理）等；决定你的代码在调用链上的位置。 |
| **注册方式** | `PatchAll()` 扫描程序集，或 `harmony.Patch(...)` **手动**指定 `MethodBase` 与委托。 |
| **参数注入** | 补丁方法通过**参数名**接收 `__instance`、`__result`、与原方法同名的参数等；实例方法无参时 Postfix 常写 `void Postfix(T __instance)`。 |
| **初始化时机** | 在 mod 入口（如 `ModInitializer`）里**足够早**执行 `PatchAll()`，确保目标被调用前补丁已生效。 |

口语里说的「写清楚能 patch 的地方」，在文档里建议落成上表中的**具体字段**，避免只用一个模糊词概括。

---

## 2. 「补丁目标」不是什么抽象概念

**补丁目标** = 游戏或引擎里**已经存在**、且可被 Harmony 解析到的那个 **C# 成员**（绝大多数情况是**方法**）。

实现上：用 Harmony 把自定义逻辑**织入**该方法的调用过程：在**目标方法执行前**跑 Prefix，在**执行后**跑 Postfix（概念与总览见 [Harmony — Introduction](https://harmony.pardeike.net/articles/intro.html)，仓库见 [GitHub](https://github.com/pardeike/Harmony)）。读者需要你在文档里写清 **类型全名、方法名、实例/静态、关键重载**，必要时注明 Godot 生命周期（如 `_Ready`）的语义。

---

## 3. 最少要做哪几步？

| 步骤 | 说明 |
| --- | --- |
| 引用库 | 项目引用 **HarmonyLib**（与游戏 / ModTemplate 一致）。 |
| 声明补丁目标 | 在静态类上加 `[HarmonyPatch(typeof(目标类型), "方法名")]`，或使用嵌套 `[HarmonyPatch]` 指定多个目标。 |
| 写补丁方法 | `static` 方法，命名为 `Prefix` / `Postfix` / `Transpiler`（Harmony 按名识别）。 |
| 注册 | 在 mod 初始化里 `new Harmony("你的ModId").PatchAll()`；或对少数类手动 `Patch()`。 |

---

## 4. 最小示例（教学骨架）

下面是一段**独立 mod** 里可能出现的骨架（类型名请按游戏版本替换；**不要**直接复制未验证的 API）：

```csharp
using HarmonyLib;

[HarmonyPatch(typeof(SomeGameType), nameof(SomeGameType.SomeMethod))]
public static class MyExamplePatch
{
    /// <summary>在原方法执行「之后」运行；__instance 为被调用方法的 this（实例方法）。</summary>
    public static void Postfix(SomeGameType __instance)
    {
        // 挂 UI、改状态、打日志等
    }
}
```

```csharp
// Mod 入口（与 MegaCrit ModInitializer 等约定一致时）
var harmony = new Harmony("MyModId");
harmony.PatchAll();
```

**Prefix**：在原方法**之前**执行；可配合 `return false` 跳过原方法（高级用法，见 Harmony 文档）。

---

## 5. DevMode 仓库中的示例（可对照源码）

本仓库在初始化时调用 `PatchAll()`，见 `MainFile.cs` 的 `Initialize()`。

**补丁目标**：`NGlobalUi._Ready`。**补丁类型**：Postfix（在 `_Ready` 执行完毕后挂载开发者面板）。节选：

```17:27:src/Patches/DevPanelPatches.cs
[HarmonyPatch(typeof(NGlobalUi), "_Ready")]
public static class GlobalUiReadyPatch {
    // Track the instance we already attached to avoid duplicate panels on re-entry
    private static NGlobalUi? _attached;
    private static AssetWarmupService? _warmup;

    public static void Postfix(NGlobalUi __instance) {
        if (!DevModeState.InDevRun && !DevModeState.AlwaysEnabled) return;
        if (_attached == __instance) return;
        _attached = __instance;
        DevPanel.Attach(__instance);
```

语义：**全局 UI 节点就绪之后**再挂 `Control`、持有 `NGlobalUi` 引用。更多示例见 `src/Patches/*.cs`。

---

## 6. Prefix 与 Postfix

| | Prefix | Postfix |
| --- | --- | --- |
| 执行顺序 | 目标方法**之前** | 目标方法**之后** |
| 典型用途 | 改参数、条件跳过原方法 | 利用已完成的副作用、`__instance` 已就绪 |
| 注意 | 滥用 `return false` 易破坏逻辑 | 目标若抛异常，Postfix 行为受 Harmony 版本与配置影响 |

---

## 7. 参数命名约定

| 参数名 | 含义 |
| --- | --- |
| `__instance` | 实例方法的 `this`（静态方法无此项） |
| `__result` | Postfix 中可赋值以替换返回值 |
| 与原方法同名 | 对应原方法的参数（Prefix 中可改写） |

详见 [Harmony — Patch parameters](https://harmony.pardeike.net/articles/patching-injections.html)。

---

## 8. 手动 Patch

目标在运行时解析时，可 `AccessTools.Method(...)` 取得 `MethodBase` 后 `harmony.Patch(original, prefix, postfix)`。细节以官方文档为准。

---

## 9. 风险与维护

- 游戏更新可能重命名、内联或删除方法，需更新**补丁目标**。
- 优先选择**语义稳定**的入口（如 Godot `_Ready`、管理器 `SetUp`）。
- 多 mod 同目标时注意**执行顺序**与兼容性。

---

## 延伸阅读

- **[Harmony — Introduction](https://harmony.pardeike.net/articles/intro.html)** — 官方入门：补丁思路、Hello World、运行时补丁限制
- **[宠物（进阶）](/notes/sts2-pet-guide/)** — `CreateVisuals` 与 Prefix
- **[踩坑备忘](/notes/sts2-modding-pitfalls/)** — `NMapScreen` 叠层与 Postfix、`__instance` 命名

:::
