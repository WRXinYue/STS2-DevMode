---
title:
  en: Contributing
  zh-CN: 参与贡献
categories:
  - dev
top: 10050
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Overview{lang="en"}

## 概述{lang="zh-CN"}

::: en
This page is for **people working on the DevMode repository**: building the mod, running the documentation site, and sending changes upstream. If you only want to install the released mod, use **[Install](/guide/install/)** in the user guide.
:::

::: zh-CN
本文面向 **参与 DevMode 仓库开发** 的贡献者：构建 mod、运行文档站、提交改动等。若你只想安装已发布的 mod，请使用用户指南里的 **[安装](/guide/install/)**。
:::

## Dev{lang="en"}

## 开发{lang="zh-CN"}

::: en
**Prerequisites:** same as in [Install](/guide/install/) (Steam + writable `mods`, **.NET 9 SDK**; **Python 3** optional for `make init` / icon scripts).

**First-time setup:** clone the repository, then run `make init` once (or `python scripts/init.py`) to generate `local.props` with your STS2 install path.

**Common Makefile targets** (Windows cmd recommended with `make`):

| Target | Description |
| --- | --- |
| `make init` | Detect STS2 + Godot, write `local.props` |
| `make build` | Publish to `build/DevMode/` |
| `make deploy` | Publish into the game `mods\DevMode` folder |
| `make sync` | `build` then `deploy` |
| `make format` | `dotnet format DevMode.sln` (run before PRs) |

Collaboration norms (commits, formatting, localization) live in **CONTRIBUTING.md** at the **repository root** next to this `docs/` tree.
:::

::: zh-CN
**前置条件：** 与 [安装](/guide/install/) 一致 (Steam + 可写 `mods`、**.NET 9 SDK**；**Python 3** 可选，用于 `make init` / 图标脚本)。

**首次设置：** 克隆仓库后执行一次 `make init` (或 `python scripts/init.py`)，生成包含 STS2 安装路径的 `local.props`。

**常用 Makefile 目标** (Windows 下建议用 cmd 配合 `make`)：

| 目标 | 说明 |
| --- | --- |
| `make init` | 检测 STS2、Godot，写入 `local.props` |
| `make build` | 发布到 `build/DevMode/` |
| `make deploy` | 发布到游戏目录 `mods\DevMode` |
| `make sync` | 先 `build` 再 `deploy` |
| `make format` | `dotnet format DevMode.sln` (提 PR 前建议执行) |

协作约定 (提交信息、格式化、本地化等) 写在仓库根目录、与本 `docs/` 目录同级的 **CONTRIBUTING.md** 中。
:::

## Docs{lang="en"}

## 文档站{lang="zh-CN"}

::: en
This documentation site lives under **`docs/`** and is built with **[Valaxy](https://valaxy.site/)**. **Use pnpm** (pin the version with Corepack, same as this repo’s `docs/package.json`).

From the **repository root**:

```bash
cd docs
corepack enable
corepack prepare pnpm@10.24.0 --activate
pnpm install
pnpm dev
```

If `corepack` is not on your `PATH`, install pnpm once globally, for example: `npm install -g pnpm@10.24.0`.

**Static export:**

```bash
pnpm run build
```

Output goes to **`docs/dist/`** by default.

For Markdown syntax and Valaxy-specific features (containers, frontmatter, i18n blocks, etc.), see the **[Markdown writing guide](https://oceanus.wrxinyue.org/guide/writing/markdown)**.
:::

::: zh-CN
本站源码在 **`docs/`** 目录，使用 **[Valaxy](https://valaxy.site/)** 构建。**请使用 pnpm**，并用 Corepack 固定版本 (与本仓库 `docs/package.json` 一致)。

在 **仓库根目录** 执行：

```bash
cd docs
corepack enable
corepack prepare pnpm@10.24.0 --activate
pnpm install
pnpm dev
```

若系统找不到 `corepack`，可全局安装 pnpm，例如：`npm install -g pnpm@10.24.0`。

**静态导出：**

```bash
pnpm run build
```

默认输出在 **`docs/dist/`**。

Markdown 语法与 Valaxy 特有功能（容器、frontmatter、i18n 块等）参见 **[Markdown 编写指南](https://oceanus.wrxinyue.org/guide/writing/markdown)**。
:::

## Collaboration{lang="en"}

## 协作{lang="zh-CN"}

::: en
- Prefer **Conventional Commits** for PR titles and commits (`feat:`, `fix:`, `docs:`, …).
- Run **`make format`** (or `dotnet format DevMode.sln`) and a clean **`dotnet build`** on `DevMode.sln` before opening a PR.
- Do **not** commit secrets, `local.props`, or generated icon/npm artifacts listed in **CONTRIBUTING.md**.
:::

::: zh-CN
- 提交与 PR 标题建议遵循 **Conventional Commits** (`feat:`、`fix:`、`docs:` 等)。
- 开 PR 前在 `DevMode.sln` 上跑通 **`dotnet build`**，并执行 **`make format`** (或 `dotnet format DevMode.sln`)。
- 勿提交密钥、`local.props` 以及 **CONTRIBUTING.md** 中列出的生成物或本地依赖目录。
:::
