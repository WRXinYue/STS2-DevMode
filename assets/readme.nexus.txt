All-in-one in-game toolkit for Slay the Spire 2 — test builds, cheat, script, and debug mods without leaving the game.

[img]https://raw.githubusercontent.com/WRXinYue/STS2-DevMode/main/assets/devmode.png[/img]

[b]Getting started[/b]

[list]
[*][b]During a run[/b] — Hover the left-edge [b]peek tab[/b] to expand the dev rail, then click a panel icon. Browser panels slide in from the left; combat overlays use the game’s right edge or floating windows.
[*][b]Title screen[/b] — Click [b]DEVMODE[/b] for test runs, snapshots, diagnostics, progress protection, and multiplayer dev tools (no run required).
[*][b]Settings → Sidebar[/b] — Drag to reorder rail tabs and hide panels you do not need. [b]Harmony analysis[/b], [b]Scripts[/b], and [b]Frameworks[/b] start hidden; enable them here when needed.
[*][b]Settings → Game[/b] — [b]In-game right sidebar[/b] (combat shortcuts + stats rail), game speed, skip animations, overlay toggles.
[*][b]Normal runs[/b] — From title [b]DEVMODE[/b], cycle [b]Normal run: Disabled / Dev Mode / Cheat Mode[/b] to keep the rail available outside test runs.
[/list]

Install from [url=https://github.com/WRXinYue/STS2-DevMode/releases]Releases[/url] or build from source ([i]python scripts/init.py[/i], then [i]make sync[/i]). Steam [b]beta[/b] builds need the matching beta mod package.

[b]Panels[/b]

[b]Gameplay & content[/b]

[list]
[*][b]Cheats[/b] — God mode, infinite energy/block/stars, damage multipliers, enemy freeze, stat locks, map overrides (free travel while map is open), reward tweaks; some options limited in [b]multiplayer[/b]
[*][b]Cards[/b] — Full card library; filter by type/rarity/cost/pool/character/[b]mod source[/b]; [b]show hidden cards[/b]; right-click a filter chip to [b]exclude[/b]; edit stats and enchantments; add to any pile; upgrade preview; filters persist across sessions
[*][b]Relics[/b] — Browse and add relics; [b]mod source[/b] filter
[*][b]Powers[/b] — Apply powers (self, all enemies, specific, allies); one-click Auto-Apply hooks; [b]mod source[/b] filter
[*][b]Potions[/b] — Visual grid; one-click Auto-Apply hooks; [b]mod source[/b] filter
[*][b]Enemies[/b] — Replace encounters by room or map node; preview content; idle animation preview; edit per-turn enemy intents
[*][b]Events[/b] — Browse and trigger event flows; [b]mod source[/b] filter
[*][b]Rooms[/b] — Inspect and jump between room types; teleport to ancient shop locations
[*][b]Presets[/b] — Save/load combat and run snapshots (hand, deck, relics, etc.)
[/list]

[b]Automation & AI[/b]

[list]
[*][b]Hooks[/b] — Trigger → Condition → Action rules (e.g. add a card on combat start, apply a power on draw)
[*][b]Scripts[/b] — SpireScratch visual scripting (Blockly); live reload via WebSocket
[*][b]AI Host[/b] — Rule-based bot for [b]solo[/b] runs (map, combat, rewards). Disabled during multiplayer hand-play to avoid desync; use Pseudo Co-op / LAN presets instead (see below)
[/list]

[b]Developer & debug[/b]

[list]
[*][b]Enemy intents[/b] — [b]Enemy intents[/b] rail tab: next-turn preview; optional [b]draggable overlay[/b] during fights (off by default); intent badges on the combat sidebar stack when an enemy has multiple intents
[*][b]Combat stats[/b] — Live damage/block/heal breakdown by card, source, and turn; pie chart sidebar; run totals; JSON export; [i]dmstats[/i] console command; slim [b]right-rail bars[/b] in solo; [b]draggable top-right MP overlay[/b] in co-op
[*][b]Console[/b] — Searchable reference for native and DevMode commands
[*][b]Logs[/b] — See [b][url=#logs]Logs[/url][/b] below
[*][b]Harmony analysis[/b] — Inspect active patches; filter by owner; smart summary
[*][b]Frameworks[/b] — Loaded mod framework snapshot
[*][b]Mod feedback[/b] — See [b][url=#mod-feedback]Mod feedback[/url][/b] below
[/list]

[b]Utility[/b]

[list]
[*][b]Save / Load[/b] — Named DevMode snapshot slots (separate from vanilla [i]progress.save[/i]); carry cards/relics/gold into a new seed; slot detail view
[*][b]Manual[/b] — In-game documentation browser (one page per tool)
[*][b]Settings[/b] — Theme (Dark / OLED / Light / Warm), game speed, skip animations, rail layout, combat overlays, [b]progress protection[/b] and [b]crash recovery[/b] toggles
[/list]

[b]In-combat overlays[/b]

These are optional and mostly [b]off by default[/b] — turn them on under [b]Settings → Game[/b] or their panel.

[list]
[*][b]In-game right sidebar[/b] — Live contribution bars, enemy intent preview rail, compact combat tools (add encounter/monster, kill enemies). Default: [b]Off[/b]
[*][b]Enemy intent overlay[/b] — Draggable float with next-turn intents. Default: [b]Off[/b]
[*][b]Multiplayer combat stats overlay[/b] — Draggable top-right score bars per player in co-op. Default: [b]On[/b]
[/list]

During fights, intent badges on the right sidebar stack vertically when an enemy has multiple intents. Opening the full [b]Combat stats[/b] panel can merge flush with the right rail when the browser is nearly full width.

[b]Multiplayer cheat sync[/b] — When hosting with [b]Multiplayer cheat[/b] enabled (title [b]DEVMODE → Multiplayer[/b]), cheats, card/relic/potion edits, combat enemy tools, powers, and per-player cheat flags can sync across clients (all peers need DevMode).

[b]Logs[/b]

Open from the in-run [b]Logs[/b] rail tab or title screen [b]DEVMODE → Diagnostics → Logs[/b].

[list]
[*][b]Live + file history[/b] — Streams new log lines and hydrates earlier lines from the session log ([i]mod_data/DevMode/instances/{pid}/session.log[/i], with fallback to Godot [i]user://logs/[/i]).
[*][b]Filters[/b] — Level chips (All / ≥ Info / ≥ Warn / Error), text search, per-mod source toggles, and toggleable [b]noise suppression[/b] rules (known benign patterns with hit counts).
[*][b]Presentation[/b] — Mod vs game source coloring; session boundary markers between DevMode restarts.
[*][b]Stats sidebar[/b] — Entry counts by level and mod; [b]source pie chart[/b].
[*][b]Copy all[/b] — Copy the currently filtered log text to the clipboard.
[*][b]Alerts[/b] — The [b]Logs[/b] rail icon blinks on unseen Warn/Error until you open the viewer. The peek tab blinks until your first rail hover (then stays dismissed).
[/list]

[b]Mod feedback[/b]

Open from the in-run rail or title screen [b]DEVMODE → Diagnostics → Mod Feedback[/b].

Fill in a title and description, optionally attach a game log tail, and export a [b]ZIP report[/b] for mod authors. [b]Privacy mode[/b] replaces user-data paths with [i]&lt;user-data&gt;[/i] in all text files.

Typical ZIP contents:

[list]
[*][i]report.txt[/i] — Your description and environment summary
[*][i]mods.txt[/i] — Loaded mod list
[*][i]logs-filtered.txt[/i] — DevMode-filtered log excerpt
[*][i]harmony-patches.txt[/i] — Active Harmony patch dump
[*][i]framework-bridge.txt[/i] — Framework snapshot
[*][i]combat-stats.json[/i] — Current combat stats export (if in a fight)
[*][i]game-logs/[/i] — Optional attached vanilla log tail
[/list]

Reports are written under [i]user://devmode-reports/[/i] (account-scoped user data, same tree as [i]mod_data/DevMode/[/i]).

When DevMode detects an unhandled error or an abnormal exit, it can open a dialog that links here with a [b]prefilled crash summary[/b] — see [b][url=#crash-recovery]Crash recovery[/url][/b] below.

[b]Crash recovery[/b]

DevMode can prompt you to export a feedback ZIP after serious failures (without spamming a popup on every log line).

[b]In-game error dialog[/b]

[list]
[*]On an [b]unhandled .NET exception[/b], DevMode writes a crash report and tries to show a dialog: [b]View logs[/b], [b]Export feedback ZIP[/b], or [b]Close[/b].
[*]The export form is prefilled with an automatic summary (exception type, message, stack excerpt, DevMode version).
[/list]

[b]Next-launch prompt[/b]

[list]
[*]If the game [b]exits abnormally[/b] (e.g. kill process) and the previous session did not shut down cleanly, the [b]main menu[/b] offers the same export flow on next startup.
[*]Session markers live under [i]mod[i]data/DevMode/instances/{pid}/session.active[/i]; pending reports under [i]mod[/i]data/DevMode/pending-crash-report.json[/i].
[/list]

[b]Settings[/b]

[list]
[*]Toggle: [b]Settings → Crash recovery → Prompt to export feedback on crash[/b] (on by default).
[*]Progress-loss restore prompts take priority if both would show on startup.
[/list]

Look for log lines prefixed [b][i][DevMode CrashRecovery][/i][/b].

[b]Title screen (DEVMODE)[/b]

On the main menu, [b]DEVMODE[/b] replaces separate dev buttons with one submenu:

[list]
[*][b]New Test[/b] — Start a quick test run
[*][b]New Test (Seed)[/b] — Test run with an optional seed
[*][b]Load Save[/b] — Load a DevMode snapshot slot (disabled when no slots exist)
[*][b]Normal run: …[/b] — Cycle [b]Disabled[/b] → [b]Dev Mode[/b] → [b]Cheat Mode[/b] for non-test runs
[*][b]Multiplayer[/b] — Multiplayer dev submenu (see below)
[*][b]Unlock All Progress[/b] — Unlock timeline epochs, Ascension 10, and compendium entries (confirmation required)
[*][b]Diagnostics[/b] — [b]Logs[/b] and [b]Mod feedback[/b]
[*][b]Progress protection[/b] — Backup status, restore, per-backup [b]Details[/b]
[*][b]Back[/b] — Return to the stock main menu
[/list]

[b]Multiplayer[/b] submenu:

[list]
[*][b]Multiplayer cheat: ON/OFF[/b] — Opt in to synced multiplayer cheat sessions
[*][b]Pseudo Co-op Test (Host)[/b] — Host with character/seed pickers; optional SyncBot, phantom player (NetId 1001), AI teammate
[*][b]LAN Multiplayer[/b] — Open the built-in multiplayer test scene
[/list]

Restore from [b]Progress protection[/b] is title-screen only. Prefer matching the backup’s mod set when possible.

[b]Progress protection[/b]

Changing the loaded mod set can cause vanilla save filtering to strip or zero mod character stats in [i]progress.save[/i]. DevMode backs up and helps you recover that progress.

[b]Automatic backup[/b]

[list]
[*]On startup, when the loaded mod fingerprint differs from the last session, DevMode copies the active profile’s [i]progress.save[/i] (and optional [i]prefs.save[/i] / [i]current_run.save[/i]) [b]before[/b] vanilla filtering runs.
[*]Keeps up to [b]10 backups per profile[/b] (oldest removed).
[*]Toggle: [b]Settings → Progress protection → Auto-backup on mod set change[/b] (on by default).
[/list]

[b]Startup restore prompt[/b]

[list]
[*]After progress loads on the title screen, DevMode scans recent backups for mod character stats that are missing or degraded in the current save (e.g. Ascension / wins reset to zero while a backup still has progress).
[*]If recoverable data exists, a [b]Restore[/b] / [b]Not now[/b] dialog appears on the main menu.
[*]Toggle: [b]Settings → Progress protection → Prompt on mod character progress loss[/b] (on by default).
[*]You can also restore anytime from [b]DEVMODE → Progress protection[/b].
[/list]

[b]Manual restore[/b]

[list=1]
[*]Title screen → [b]DEVMODE → Progress protection[/b]
[*]Choose a backup → [b]Restore[/b], or open [b]Details[/b] first
[*]Confirm; DevMode writes a [i]progress.save.pre[i]restore[/i]{timestamp}[/i] next to the active save before overwriting
[*]Reload the main menu or restart the game so progress reloads from disk
[/list]

[b]File locations[/b]

[b]DevMode user data root[/b] (settings, snapshots, backups):

[code]
%AppData%\SlayTheSpire2\steam\{SteamId}\mod_data\DevMode\
[/code]

[b]Profile backups[/b] (one folder per backup):

[code]
...\mod_data\DevMode\profile_backups\{yyyyMMdd_HHmmss}_profile{N}\
  progress.save
  backup_meta.json    # timestamp, mod fingerprint, copied files
  prefs.save          # optional
  current_run.save    # optional
[/code]

[b]Active game progress[/b] (path depends on vanilla vs modded profile layout):

[code]
...\steam\{SteamId}\profile{N}\saves\progress.save
...\steam\{SteamId}\modded\profile{N}\saves\progress.save   # when using modded saves
[/code]

On macOS/Linux, [i]%AppData%[/i] is the game’s account-scoped user data directory (see Godot [i]user://steam/{userId}/[/i]).

[b]Troubleshooting[/b]

[list]
[*]Look for log lines prefixed [b][i][ProgressGuard][/i][/b] (startup scan, restore, prompts) or [b][i][ModChangeGuard][/i][/b] (fingerprint change, backup creation).
[*]If you build from source, deploy with [b][i]make sync[/i][/b] so the game loads the latest DLL.
[/list]

[b]Multiplayer & co-op testing (dev)[/b]

These features are [b]opt-in[/b] from DevPanel → [b]AI Host[/b]. They do not change vanilla solo hand-play or draw speed unless you enable AI / cheats yourself.

[list]
[*][b]AI Host (solo)[/b] — [i]SimpleStrategy[/i] drives your character locally. Use for single-player automation.
[*][b]SyncBot[/b] — Simulates remote peer ACKs and default choices on one machine; optional phantom player (NetId 1001). Use for host-only co-op smoke tests without a second client.
[*][b]Pseudo Co-op preset[/b] — Hand-play host + AI teammate for phantom/offline peers via action queue. Use for solo host with simulated teammate.
[*][b]LAN host-drive + AFK[/b] — Host hand-plays local player; AI enqueues combat for connected ENet client; client AFK blocks local combat input; map votes mirrored. Use for two game instances on one PC (auto preset on dual launch).
[/list]

[b]Dual-instance LAN (recommended):[/b] launch host + client on the same machine → presets apply automatically; host logs [i]LAN host preset applied[/i], client logs [i]AFK client enabled[/i].

Detailed architecture, verification checklist, and desync history: [b][url=./docs/lan-host-drive-afk.md]docs/lan-host-drive-afk.md[/url][/b] · [url=./docs/README.md]docs index[/url]

[b]Contributing[/b]

See [b][url=CONTRIBUTING.md]CONTRIBUTING.md[/url][/b] for collaboration norms, K&R brace style, formatting commands, and localization, or open an issue / PR on [url=https://github.com/WRXinYue/STS2-DevMode]GitHub[/url].

[b]Changelog[/b]

See [url=https://github.com/WRXinYue/STS2-DevMode/blob/main/CHANGELOG.md]CHANGELOG.md[/url] for version history.

[b]Acknowledgments[/b]

[list]
[*][url=https://github.com/mugongzi520/STS2-KaylaMod]STS2-KaylaMod[/url]
[/list]

[b]License[/b]

[url=https://github.com/WRXinYue/STS2-DevMode/blob/main/LICENSE]MIT[/url]

[line]

《杀戮尖塔 2》全功能游戏内工具箱：测试、作弊、脚本与 Mod 调试一体化。

[b]快速上手[/b]

[list]
[*][b]局内[/b] — 鼠标移到左侧 [b]peek 标签[/b] 展开 dev 侧栏，点击图标打开面板。浏览器面板从左侧滑入；战斗 overlay 在游戏右侧或浮动窗口。
[*][b]标题画面[/b] — 点击 [b]DEVMODE[/b] 可开测试局、读快照、诊断、进度保护、联机开发工具（无需进 run）。
[*][b]设置 → 侧栏（Sidebar）[/b] — 拖拽排序、隐藏不需要的标签。[b]Harmony 分析[/b]、[b]脚本[/b]、[b]框架[/b] 默认隐藏，需要时在此开启。
[*][b]设置 → 游戏（Game）[/b] — [b]局内右侧边栏[/b]（战斗快捷 + 统计 rail）、游戏速度、跳过动画、overlay 开关。
[*][b]普通 run[/b] — 标题 [b]DEVMODE[/b] 中切换 [b]Normal run: 关闭 / Dev Mode / Cheat Mode[/b]，在非测试局也保留侧栏。
[/list]

可从 [url=https://github.com/WRXinYue/STS2-DevMode/releases]Releases[/url] 安装，或源码构建（[i]python scripts/init.py[/i]，再 [i]make sync[/i]）。Steam [b]beta[/b] 分支需使用对应的 beta mod 包。

[b]面板一览[/b]

[b]玩法与内容[/b]

[list]
[*][b]作弊[/b] — 无敌、无限能量/格挡/星星、伤害倍率、冻结敌人、数值锁定、地图覆盖（地图打开时自由跳转）、奖励调整；[b]联机[/b]下部分选项受限
[*][b]卡牌[/b] — 全卡库浏览；按类型/稀有度/费用/卡池/角色/[b]Mod 来源[/b]筛选；[b]显示隐藏卡牌[/b]；右键筛选 chip [b]排除[/b]；编辑数值与附魔；添加至任意牌堆；升级对比；筛选条件跨会话记忆
[*][b]遗物[/b] — 浏览并添加遗物；[b]Mod 来源[/b]筛选
[*][b]能力[/b] — 施加能力（自身、所有敌人、指定、友军）；一键创建「战斗开始自动施加」钩子；[b]Mod 来源[/b]筛选
[*][b]药水[/b] — 图标网格；一键创建「战斗开始自动使用」钩子；[b]Mod 来源[/b]筛选
[*][b]敌人[/b] — 按房间或地图节点替换遭遇；预览内容；待机动画预览；编辑敌人每回合意图
[*][b]事件[/b] — 浏览与触发事件流程；[b]Mod 来源[/b]筛选
[*][b]房间[/b] — 查看与跳转房间类型；传送到远古商店位置
[*][b]预设[/b] — 保存/加载战斗与 run 快照（手牌、牌组、遗物等）
[/list]

[b]自动化与 AI[/b]

[list]
[*][b]钩子[/b] — 「触发器 → 条件 → 动作」规则（如战斗开始加牌、抽牌时施加能力）
[*][b]脚本[/b] — SpireScratch 可视化积木（Blockly）；WebSocket 热重载
[*][b]AI 托管[/b] — 规则 AI 驱动 [b]单人[/b] run（地图、战斗、奖励）。联机手打时自动禁用，避免 desync；联机请用下方 Pseudo Co-op / LAN 预设
[/list]

[b]开发者与调试[/b]

[list]
[*][b]敌人意图[/b] — [b]敌人意图[/b] rail 标签：下回合预览；可选战斗内 [b]可拖拽 overlay[/b]（默认关）；多意图敌人在战斗侧栏上纵向堆叠 badge
[*][b]战斗统计[/b] — 按卡牌/来源/回合统计伤害/格挡/治疗；饼图侧栏；整 run 合计；JSON 导出；[i]dmstats[/i] 控制台命令；单人 [b]右侧 slim 条[/b]；联机 [b]可拖拽右上角 overlay[/b]
[*][b]控制台[/b] — 原版与 DevMode 命令可搜索参考
[*][b]日志[/b] — 见下方 [b][url=#日志]日志[/url][/b]
[*][b]Harmony 分析[/b] — 查看激活补丁；按 owner 筛选；智能摘要
[*][b]框架[/b] — 已加载 Mod 框架快照
[*][b]Mod 反馈[/b] — 见下方 [b][url=#mod-反馈]Mod 反馈[/url][/b]
[/list]

[b]工具[/b]

[list]
[*][b]存档[/b] — DevMode 命名快照槽（与 vanilla [i]progress.save[/i] 独立）；携带卡牌/遗物/金币开新种子；存档详情
[*][b]手册[/b] — 游戏内文档浏览器（每个工具一页）
[*][b]设置[/b] — 主题（Dark / OLED / Light / Warm）、游戏速度、跳过动画、侧栏布局、战斗 overlay、[b]进度保护[/b]与[b]崩溃恢复[/b]开关
[/list]

[b]战斗 overlay[/b]

多为[b]默认关闭[/b]，在 [b]设置 → 游戏[/b] 或对应面板中开启。

[list]
[*][b]局内右侧边栏[/b] — 实时贡献条、敌人意图 preview rail、战斗快捷（加遭遇/怪物、击杀）。默认：[b]关[/b]
[*][b]敌人意图 overlay[/b] — 可拖拽浮动窗，显示下回合意图。默认：[b]关[/b]
[*][b]联机战斗统计 overlay[/b] — 联机时右上角可拖拽各玩家得分条。默认：[b]开[/b]
[/list]

打开完整 [b]战斗统计[/b] 面板且浏览器几乎全宽时，可与右侧 rail 对齐合并。

[b]联机作弊同步[/b] — 主机在标题 [b]DEVMODE → Multiplayer[/b] 开启 [b]Multiplayer cheat[/b] 后，作弊、卡牌/遗物/药水编辑、战斗敌人工具、能力及 per-player 作弊标记可跨客户端同步（所有 peer 需安装 DevMode）。

[b]日志[/b]

从局内 [b]日志[/b] rail 标签，或标题 [b]DEVMODE → Diagnostics → Logs[/b] 打开。

[list]
[*][b]实时 + 文件历史[/b] — 流式接收新日志，并从会话日志回填更早行（[i]mod_data/DevMode/instances/{pid}/session.log[/i]，回退 Godot [i]user://logs/[/i]）。
[*][b]筛选[/b] — 级别 chip（全部 / ≥ Info / ≥ Warn / Error）、文本搜索、按 mod 来源开关、可切换的[b]噪音抑制[/b]规则（已知无害模式 + 命中次数）。
[*][b]展示[/b] — mod 与游戏来源分色；DevMode 重启之间的会话边界标记。
[*][b]统计侧栏[/b] — 按级别与 mod 计数；[b]来源饼图[/b]。
[*][b]复制全部[/b] — 将当前筛选结果复制到剪贴板。
[*][b]提醒[/b] — 出现未读 Warn/Error 时 [b]日志[/b] rail 图标闪烁，打开查看器后清除。peek 标签在首次 hover 侧栏前闪烁（之后永久关闭）。
[/list]

[b]Mod 反馈[/b]

从局内 rail 或标题 [b]DEVMODE → Diagnostics → Mod Feedback[/b] 打开。

填写标题与描述，可选附加游戏日志尾部，导出供 mod 作者使用的 [b]ZIP 报告[/b]。[b]隐私模式[/b] 会将用户数据路径替换为 [i]&lt;user-data&gt;[/i]。

ZIP 典型内容：

[list]
[*][i]report.txt[/i] — 描述与环境摘要
[*][i]mods.txt[/i] — 已加载 mod 列表
[*][i]logs-filtered.txt[/i] — DevMode 过滤后的日志摘录
[*][i]harmony-patches.txt[/i] — Harmony 补丁转储
[*][i]framework-bridge.txt[/i] — 框架快照
[*][i]combat-stats.json[/i] — 当前战斗统计（若在战斗中）
[*][i]game-logs/[/i] — 可选附加的原版日志尾部
[/list]

报告写入 [i]user://devmode-reports/[/i]（账号作用域用户数据，与 [i]mod_data/DevMode/[/i] 同树）。

当 DevMode 检测到未捕获异常或异常退出时，可弹出对话框并[b]预填崩溃摘要[/b]跳转至此导出流程 — 见下方 [b][url=#崩溃恢复]崩溃恢复[/url][/b]。

[b]崩溃恢复[/b]

DevMode 可在严重故障后提示导出反馈 ZIP（不会对每条日志 Error 都弹窗）。

[b]局内错误对话框[/b]

[list]
[*]发生 [b]未捕获 .NET 异常[/b] 时，DevMode 写入崩溃报告并尽量弹出对话框：[b]查看日志[/b]、[b]导出反馈 ZIP[/b] 或 [b]关闭[/b]。
[*]导出表单会预填自动摘要（异常类型、消息、堆栈节选、DevMode 版本）。
[/list]

[b]下次启动提示[/b]

[list]
[*]若游戏 [b]异常退出[/b]（如强杀进程）且上次会话未正常关闭，[b]主菜单[/b] 在下次启动时提供相同导出流程。
[*]会话标记位于 [i]mod[i]data/DevMode/instances/{pid}/session.active[/i]；待处理报告位于 [i]mod[/i]data/DevMode/pending-crash-report.json[/i]。
[/list]

[b]设置[/b]

[list]
[*]开关：[b]设置 → 崩溃恢复 → 崩溃时提示导出反馈包[/b]（默认开启）。
[*]若与进度丢失恢复提示同时满足，优先显示进度保护弹窗。
[/list]

关注日志前缀 [b][i][DevMode CrashRecovery][/i][/b]。

[b]标题画面（DEVMODE）[/b]

主菜单 [b]DEVMODE[/b] 合并原分散 dev 按钮为一个子菜单：

[list]
[*][b]New Test[/b] — 快速测试局
[*][b]New Test (Seed)[/b] — 可填种子的测试局
[*][b]Load Save[/b] — 读取 DevMode 快照槽（无槽位时禁用）
[*][b]Normal run: …[/b] — 在非测试局循环 [b]关闭 / Dev Mode / Cheat Mode[/b]
[*][b]Multiplayer[/b] — 联机开发子菜单（见下）
[*][b]Unlock All Progress[/b] — 解锁时间线纪元、进阶 10、图鉴（需确认）
[*][b]Diagnostics[/b] — [b]日志[/b] 与 [b]Mod 反馈[/b]
[*][b]进度保护[/b] — 备份状态、恢复、每条 [b]详情[/b]
[*][b]Back[/b] — 返回原版主菜单
[/list]

[b]Multiplayer[/b] 子菜单：

[list]
[*][b]Multiplayer cheat: ON/OFF[/b] — 联机作弊同步 opt-in
[*][b]Pseudo Co-op Test (Host)[/b] — 选角色/种子；可选 SyncBot、幻影玩家（NetId 1001）、AI 队友
[*][b]LAN Multiplayer[/b] — 打开内置联机测试场景
[/list]

从 [b]进度保护[/b] 恢复仅限标题画面；尽量让当前 mod 集与备份时一致。

[b]进度保护[/b]

更换已加载 mod 集时，原版存档过滤可能清掉或归零 mod 角色在 [i]progress.save[/i] 中的进度。DevMode 会在过滤前自动备份，并帮助恢复。

[b]自动备份[/b]

[list]
[*]启动时若 mod 指纹与上次会话不同，DevMode 会在原版过滤运行[b]之前[/b]复制当前 profile 的 [i]progress.save[/i]（以及可选的 [i]prefs.save[/i] / [i]current_run.save[/i]）。
[*]每个 profile 最多保留 [b]10 份[/b]备份（超出则删最旧）。
[*]开关：[b]设置 → 进度保护 → mod 集变化时自动备份[/b]（默认开启）。
[/list]

[b]启动恢复提示[/b]

[list]
[*]标题画面加载进度后，DevMode 会扫描最近备份，查找当前存档中缺失或降级的 mod 角色进度（例如进阶/胜场被归零，但备份里仍有数据）。
[*]若存在可恢复数据，主菜单会弹出 [b]恢复[/b] / [b]暂不[/b] 对话框。
[*]开关：[b]设置 → 进度保护 → mod 角色进度丢失时提示恢复[/b]（默认开启）。
[*]也可随时从 [b]DEVMODE → 进度保护[/b] 手动恢复。
[/list]

[b]手动恢复[/b]

[list=1]
[*]标题画面 → [b]DEVMODE → 进度保护[/b]
[*]选择备份 → [b]恢复[/b]，或先打开 [b]详情[/b]
[*]确认后，DevMode 会在覆盖前于当前存档目录写入 [i]progress.save.pre[i]restore[/i]{timestamp}[/i]
[*]重新进入主菜单或重启游戏，以便从磁盘重新加载进度
[/list]

[b]文件位置[/b]

[b]DevMode 用户数据根目录[/b]（设置、快照、备份等）：

[code]
%AppData%\SlayTheSpire2\steam\{SteamId}\mod_data\DevMode\
[/code]

[b]Profile 备份[/b]（每次备份一个文件夹）：

[code]
...\mod_data\DevMode\profile_backups\{yyyyMMdd_HHmmss}_profile{N}\
  progress.save
  backup_meta.json    # 时间戳、mod 指纹、已复制文件列表
  prefs.save          # 可选
  current_run.save    # 可选
[/code]

[b]游戏当前进度[/b]（路径随 vanilla / modded profile 布局而定）：

[code]
...\steam\{SteamId}\profile{N}\saves\progress.save
...\steam\{SteamId}\modded\profile{N}\saves\progress.save   # 使用 modded 存档时
[/code]

macOS / Linux 下 [i]%AppData%[/i] 对应游戏账号作用域的用户数据目录（Godot [i]user://steam/{userId}/[/i]）。

[b]排查[/b]

[list]
[*]关注日志前缀 [b][i][ProgressGuard][/i][/b]（启动扫描、恢复、弹窗）与 [b][i][ModChangeGuard][/i][/b]（指纹变化、创建备份）。
[*]若从源码构建，请用 [b][i]make sync[/i][/b] 部署，确保游戏加载最新 DLL。
[/list]

[b]联机与共斗测试（开发向）[/b]

以下功能均在 DevPanel → [b]AI 托管[/b] 中[b]手动开启[/b]。未开启时不影响 vanilla 单人手打，也不改抽牌速度或抽牌动画。

[list]
[*][b]AI 托管（单人）[/b] — [i]SimpleStrategy[/i] 本地代打你的角色。适用于单人自动化。
[*][b]SyncBot[/b] — 单机模拟远程 peer 的 ACK 与默认选项；可选幻影玩家（NetId 1001）。适用于无双开时的主机 co-op 冒烟测试。
[*][b]Pseudo Co-op 预设[/b] — 主机手打 + AI 队友（幻影/离线 peer，走动作队列）。适用于单机主机 + 模拟队友。
[*][b]LAN 主机代打 + 客机 AFK[/b] — 主机手打本机；AI 为真实 ENet 客户端 enqueue 战斗；客机 AFK 拦截本地战斗输入；地图投票镜像。适用于同机双开（启动时自动 preset）。
[/list]

[b]LAN 双开（推荐）：[/b] 同机启动主机 + 客机 → 自动应用 preset；主机 log 见 [i]LAN host preset applied[/i]，客机见 [i]AFK client enabled[/i]。

架构说明、复测标准与历史 desync 记录：[b][url=./docs/lan-host-drive-afk.md]docs/lan-host-drive-afk.md[/url][/b] · [url=./docs/README.md]文档索引[/url]

[b]协作与贡献[/b]

协作流程、K&R 代码风格、[i]dotnet format[/i] / [i]make format[/i]、Python 与本地化等说明见 [b][url=CONTRIBUTING.md]CONTRIBUTING.md[/url][/b]，或在 [url=https://github.com/WRXinYue/STS2-DevMode]GitHub[/url] 提交 Issue / PR。

[b]更新日志[/b]

版本历史请参阅 [url=https://github.com/WRXinYue/STS2-DevMode/blob/main/CHANGELOG.zh-CN.md]CHANGELOG.zh-CN.md[/url]。

[b]致谢[/b]

[list]
[*][url=https://github.com/mugongzi520/STS2-KaylaMod]STS2-KaylaMod[/url]
[/list]

[b]许可证[/b]

[url=https://github.com/WRXinYue/STS2-DevMode/blob/main/LICENSE]MIT[/url]