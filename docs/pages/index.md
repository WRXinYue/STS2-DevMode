---
title:
  en: DevMode
  zh-CN: DevMode

features:
  title:
    en: Overview
    zh-CN: 概览
  subtitle:
    en: Slay the Spire 2
    zh-CN: 杀戮尖塔 2
  text:
    en: >-
      A developer rail for Slay the Spire 2. Modify cards, relics, gold, and encounters mid-run;
      configure test setups from the main menu; run SpireScratch scripts, event hooks, and view in-game logs;
      tweak developer settings; and an extensible tab API for other mods.
    zh-CN: >-
      杀戮尖塔 2 开发者轨道。对局中修改卡牌、遗物、金币与遭遇；在主菜单配置测试对局；
      运行 SpireScratch 脚本、事件钩子并查看游戏内日志；调整开发者设置；以及供其他 mod 注册标签的扩展接口。

  cards:
    - title:
        en: Run controls
        zh-CN: 对局控制
      details:
        en: >-
          Add or remove cards, relics, potions, and powers; adjust gold and encounters; spawn enemies mid-combat.
        zh-CN: >-
          增删卡牌、遗物、药水、能力；调整金币与遭遇；战斗中直接生成敌人
    - title:
        en: Test setup
        zh-CN: 测试配置
      details:
        en: >-
          Configure starting relics, cards, gold, and encounters from the main menu before launching a test run;
          supports seed-based and repeatable flows.
        zh-CN: >-
          在主菜单启动测试对局前预设开局遗物、卡牌、金币与遭遇；支持固定种子与可重复测试流程
    - title:
        en: Console, scripts, and hooks
        zh-CN: 控制台、脚本与钩子
      details:
        en: >-
          Command console, SpireScratch script runner with live reload, event hooks, and an in-game log viewer.
        zh-CN: >-
          命令控制台、支持热重载的 SpireScratch 脚本运行器、事件钩子，以及游戏内日志查看器
    - title:
        en: Developer settings
        zh-CN: 开发者设置
      details:
        en: >-
          Game speed, skip animation, UI theme, and other developer options; save / load state via named slots.
        zh-CN: >-
          游戏速度、跳过动画、界面主题等开发者选项；通过命名槽位存档 / 读档
    - title:
        en: Extensibility
        zh-CN: 可扩展性
      details:
        en: >-
          Other mods register rail tabs via DevPanelRegistry / RegisterPanelWhenReady,
          use browser-rail layout through DevPanelModApi, and access Material Design Icons via MdiIcon.
        zh-CN: >-
          其他 mod 通过 DevPanelRegistry / RegisterPanelWhenReady 注册轨道标签，
          通过 DevPanelModApi 使用浏览器式轨道布局，通过 MdiIcon 使用 Material Design Icons
    - title:
        en: Localization
        zh-CN: 本地化
      details:
        en: >-
          Built-in English and Simplified Chinese; pull requests for more languages are welcome.
        zh-CN: >-
          内置英文与简体中文；欢迎通过 PR 补充翻译
---
