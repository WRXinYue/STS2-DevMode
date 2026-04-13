---
title:
  en: Install
  zh-CN: 安装
top: 10000
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Prerequisites{lang="en"}

## 前置条件{lang="zh-CN"}

::: en
- **Slay the Spire 2** (Steam) with a writable `mods` folder.
- **.NET 9 SDK** for building the mod DLL.
- **Python 3** (optional) for `make init` / icon scripts.
:::

::: zh-CN
- **《杀戮尖塔 2》** (Steam) 且 `mods` 目录可写。
- 构建 mod DLL 需要 **.NET 9 SDK**。
- **Python 3** (可选)，用于 `make init` / 图标脚本等。
:::

## As a player{lang="en"}

## 作为玩家{lang="zh-CN"}

::: en
1. Download a release **zip** from the project releases (or build locally).
2. Extract into `Slay the Spire 2\mods\DevMode\` so that `DevMode.dll` and `DevMode.json` sit next to `editor\` and `scripts\` folders as shipped.
:::

::: zh-CN
1. 从项目 Release 下载 **zip** (或在本地自行构建)。
2. 解压到 `Slay the Spire 2\mods\DevMode\`，使 `DevMode.dll`、`DevMode.json` 与随包附带的 `editor\`、`scripts\` 等目录同级。
:::

## Build from source{lang="en"}

## 从源码构建{lang="zh-CN"}

::: en
Clone, `make init`, `make build` / `make deploy`, Makefile targets, the Valaxy **`docs/`** site, and PR norms are documented under **[Contributing](/dev/)**.
:::

::: zh-CN
克隆仓库、`make init`、`make build` / `make deploy`、Makefile 目标、基于 Valaxy 的 **`docs/`** 文档站以及 PR 约定，见 **[参与开发](/dev/)**。
:::
