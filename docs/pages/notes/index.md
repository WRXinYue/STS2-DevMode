---
title:
  en: STS2 dev notes
  zh-CN: STS2 开发笔记
top: 10000
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Overview{lang="en"}

## 概述{lang="zh-CN"}

::: en

These pages are **community-style tutorials** for Slay the Spire 2 modding: engine behavior, APIs, Godot UI, and multiplayer patterns. They are maintained alongside DevMode and are **not** limited to DevMode’s own features. Titles and examples use generic placeholders (`your-mod`, `MYMOD-…`) instead of any single mod’s name.

:::

::: zh-CN

本目录收录 **《杀戮尖塔 2》模组开发**相关的笔记与教程：引擎行为、常用 API、Godot 战斗 UI、多人确定性同步等。内容与 DevMode 本体的「用户指南 / 扩展接口」互补，**不局限于 DevMode 功能**。文中示例已统一为中性占位（如 `your-mod`、`MYMOD-…`），避免绑定某一具体模组。

:::

## Pages{lang="en"}

## 目录{lang="zh-CN"}

::: en

| Page | Topics |
| --- | --- |
| [Harmony basics](/notes/sts2-harmony-basics/) | Patch targets, Prefix/Postfix, registration, injections |
| [Card API](/notes/sts2-card-api/) | DynamicVar, Power, CardPileCmd, OrbCmd, CommonActions |
| [Localization](/notes/sts2-localization/) | JSON keys, BBCode, `NL` vs `\n` |
| [Image sizes](/notes/sts2-image-standards/) | Card, power, relic, portrait dimensions |
| [Pitfalls](/notes/sts2-modding-pitfalls/) | Damage multipliers, pile UI, map overlay, export presets |
| [Combat UI](/notes/sts2-combat-ui/) | Power icons, energy icons, custom bars, tooltips |
| [Skill tree (example)](/notes/sts2-skill-tree/) | Rules vs view, Harmony entry |
| [Summon / minions](/notes/sts2-summon-guide/) | MonsterModel, AddPet, MinionPower |
| [Pets (advanced)](/notes/sts2-pet-guide/) | `.tscn` layout, CreateVisuals patch, positioning |
| [Multiplayer sync (case study)](/notes/sts2-multiplayer-sync/) | Deterministic extra state via `INetMessage` |

:::

::: zh-CN

| 页面 | 内容 |
| --- | --- |
| [Harmony 入门](/notes/sts2-harmony-basics/) | 补丁目标、补丁类型、注册、参数注入、维护 |
| [卡牌 API](/notes/sts2-card-api/) | DynamicVar、Power、CardPileCmd、OrbCmd、CommonActions |
| [本地化](/notes/sts2-localization/) | JSON key、BBCode、`NL` 与 `\n` |
| [图片规格](/notes/sts2-image-standards/) | 卡牌、能力、遗物、立绘等尺寸 |
| [踩坑备忘](/notes/sts2-modding-pitfalls/) | 伤害乘数、牌堆 UI、地图叠层、`export_presets` |
| [战斗 UI](/notes/sts2-combat-ui/) | 能力图标路径、能量图标、自定义条、悬停 |
| [技能树（示例架构）](/notes/sts2-skill-tree/) | 规则与视图分离、Harmony 入口 |
| [召唤物](/notes/sts2-summon-guide/) | MonsterModel、`AddPet`、MinionPower |
| [宠物（进阶）](/notes/sts2-pet-guide/) | 场景结构、CreateVisuals 补丁、定位 |
| [多人同步（案例）](/notes/sts2-multiplayer-sync/) | 确定性额外状态与 `INetMessage` |

:::
