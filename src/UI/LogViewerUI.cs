using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevMode.Icons;
using DevMode.Modding;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

/// <summary>
/// Log viewer spliced to the DevMode rail (fixed-width browser panel + backdrop, same pattern as Console).
/// Filters live in a left scroll sidebar (level, text, mod source, suppress rules); log center; stats + pie on the right.
/// </summary>
internal static class LogViewerUI {
    private const string RootName = "DevModeLogViewer";
    private const float PanelW = 880f;
    private const float FilterSideMinW = 216f;
    private const float StatsSideMinW = 164f;

    // BBCode hex colors per level
    private const string ColInfo = "#C8C8DC";
    private const string ColWarn = "#FFC840";
    private const string ColError = "#FF5F5F";
    private const string ColDebug = "#6A6A8A";
    private const string ColTime = "#55556A";

    private static Label CreateLogFilterSectionHeading(string i18nKey, string fallback) {
        var l = new Label { Text = I18N.T(i18nKey, fallback) };
        l.AddThemeFontSizeOverride("font_size", 10);
        l.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        return l;
    }

    public static void Show(NGlobalUi globalUi) {
        Remove(globalUi);

        DevPanelUI.PinRail();
        DevPanelUI.SpliceRail(globalUi, joined: true);

        var root = new Control {
            Name = RootName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 1250
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.TreeExiting += () => {
            DevPanelUI.UnpinRail();
            DevPanelUI.SpliceRail(globalUi, joined: false);
        };

        root.AddChild(DevPanelUI.CreateBrowserBackdrop(() => Remove(globalUi)));
        var panel = DevPanelUI.CreateBrowserPanel(PanelW);
        root.AddChild(panel);

        var vbox = panel.GetNode<VBoxContainer>("Content");
        vbox.AddThemeConstantOverride("separation", 8);

        // ── Header ──
        BuildHeader(vbox, () => Remove(globalUi));

        // ── Level / search / rule chips (wired below; placed in filter sidebar) ──
        var chipAll = DevPanelUI.CreateFilterChip(I18N.T("log.filter.all", "All"), active: true);
        var chipInfo = DevPanelUI.CreateFilterChip(I18N.T("log.filter.info", "≥ Info"), active: false);
        var chipWarn = DevPanelUI.CreateFilterChip(I18N.T("log.filter.warn", "≥ Warn"), active: false);
        var chipError = DevPanelUI.CreateFilterChip(I18N.T("log.filter.error", "Error"), active: false);

        var (searchRow, searchInput) = DevPanelUI.CreateSearchRow(I18N.T("log.search", "Filter text..."));
        searchRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var ruleChips = new Button[LogSuppressor.BuiltInRules.Length];
        for (int i = 0; i < LogSuppressor.BuiltInRules.Length; i++) {
            var rule = LogSuppressor.BuiltInRules[i];
            var chip = DevPanelUI.CreateFilterChip(rule.Pattern, active: rule.Enabled);
            chip.TooltipText = rule.Description;
            chip.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            ruleChips[i] = chip;

            var capturedRule = rule;
            chip.Toggled += v => { capturedRule.Enabled = v; };
        }

        // ── Body: filter sidebar | log | stats ────────────────────────────
        var bodyHBox = new HBoxContainer {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        bodyHBox.AddThemeConstantOverride("separation", 10);

        var emptyBox = new StyleBoxEmpty();

        var filterScroll = new ScrollContainer {
            CustomMinimumSize = new Vector2(FilterSideMinW, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            ClipContents = true,
        };
        filterScroll.AddThemeStyleboxOverride("panel", emptyBox);
        filterScroll.AddThemeStyleboxOverride("focus", emptyBox);

        var filterInner = new VBoxContainer {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        filterInner.AddThemeConstantOverride("separation", 10);

        var filterTitle = new Label { Text = I18N.T("log.filter.sidebar", "Filters") };
        filterTitle.AddThemeFontSizeOverride("font_size", 12);
        filterTitle.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        filterInner.AddChild(filterTitle);

        filterInner.AddChild(CreateLogFilterSectionHeading("log.filter.section.level", "Level"));
        var levelVBox = new VBoxContainer();
        levelVBox.AddThemeConstantOverride("separation", 6);
        foreach (var c in new[] { chipAll, chipInfo, chipWarn, chipError }) {
            c.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            levelVBox.AddChild(c);
        }

        filterInner.AddChild(levelVBox);

        filterInner.AddChild(new ColorRect {
            CustomMinimumSize = new Vector2(0, 1),
            Color = DevModeTheme.ButtonBgNormal,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });

        filterInner.AddChild(CreateLogFilterSectionHeading("log.filter.section.text", "Text"));
        filterInner.AddChild(searchRow);

        filterInner.AddChild(new ColorRect {
            CustomMinimumSize = new Vector2(0, 1),
            Color = DevModeTheme.ButtonBgNormal,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });

        filterInner.AddChild(CreateLogFilterSectionHeading("log.filter.section.mods", "Mod source"));
        var modFilterVBox = new VBoxContainer();
        modFilterVBox.AddThemeConstantOverride("separation", 4);
        filterInner.AddChild(modFilterVBox);

        filterInner.AddChild(new ColorRect {
            CustomMinimumSize = new Vector2(0, 1),
            Color = DevModeTheme.ButtonBgNormal,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });

        filterInner.AddChild(CreateLogFilterSectionHeading("log.filter.section.rules", "Suppress rules"));
        var rulesVBox = new VBoxContainer();
        rulesVBox.AddThemeConstantOverride("separation", 4);
        foreach (var chip in ruleChips)
            rulesVBox.AddChild(chip);

        filterInner.AddChild(rulesVBox);
        filterScroll.AddChild(filterInner);
        bodyHBox.AddChild(filterScroll);

        bodyHBox.AddChild(new ColorRect {
            CustomMinimumSize = new Vector2(1, 0),
            Color = DevModeTheme.ButtonBgNormal,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        });

        var logColumn = new VBoxContainer {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };

        var logHost = new Control {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            ClipContents = false,
            ClipChildren = Control.ClipChildrenMode.Disabled,
        };

        // RichTextLabel scrolls internally — avoids ScrollContainer↔min-height resize feedback loops
        // (those could freeze the main thread on click / layout with no managed exception).
        var richText = new RichTextLabel {
            BbcodeEnabled = true,
            SelectionEnabled = true,
            ScrollActive = true,
            FitContent = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            ContextMenuEnabled = true,
        };
        richText.AddThemeFontSizeOverride("normal_font_size", 12);
        var rtlNoBg = new StyleBoxEmpty();
        richText.AddThemeStyleboxOverride("normal", rtlNoBg);
        richText.AddThemeStyleboxOverride("focus", rtlNoBg);
        richText.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        richText.OffsetLeft = 0;
        richText.OffsetTop = 0;
        richText.OffsetRight = 0;
        richText.OffsetBottom = 0;

        var copyFloatBtn = new Button {
            Name = "LogCopyFloat",
            FocusMode = Control.FocusModeEnum.None,
            Flat = true,
            Icon = MdiIcon.ContentCopy.Texture(16, DevModeTheme.Subtle),
            Visible = false,
            ZIndex = 2,
            TooltipText = I18N.T("log.copyAll", "Copy All"),
            CustomMinimumSize = new Vector2(44, 36),
            Alignment = HorizontalAlignment.Center,
            IconAlignment = HorizontalAlignment.Center,
            ExpandIcon = false,
            ClipContents = false,
        };
        copyFloatBtn.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        copyFloatBtn.OffsetLeft = -58;
        copyFloatBtn.OffsetRight = -10;
        copyFloatBtn.OffsetTop = 6;
        copyFloatBtn.OffsetBottom = 42;
        ApplySmallFlatButton(copyFloatBtn);
        foreach (var key in new[] { "normal", "hover", "pressed", "focus" }) {
            if (copyFloatBtn.GetThemeStylebox(key) is StyleBoxFlat box) {
                const int pad = 6;
                box.ContentMarginLeft = pad;
                box.ContentMarginRight = pad;
                box.ContentMarginTop = pad;
                box.ContentMarginBottom = pad;
            }
        }

        copyFloatBtn.AddThemeConstantOverride("icon_max_width", 24);
        copyFloatBtn.AddThemeConstantOverride("icon_max_height", 24);

        logHost.AddChild(richText);
        logHost.AddChild(copyFloatBtn);
        logColumn.AddChild(logHost);
        bodyHBox.AddChild(logColumn);

        void SetCopyFloatDefaultIcon()
            => copyFloatBtn.Icon = MdiIcon.ContentCopy.Texture(16, DevModeTheme.Subtle);

        void HideCopyFloat() {
            copyFloatBtn.Visible = false;
            SetCopyFloatDefaultIcon();
        }

        void ShowCopyFloat() => copyFloatBtn.Visible = true;
        richText.MouseEntered += ShowCopyFloat;
        copyFloatBtn.MouseEntered += ShowCopyFloat;
        richText.MouseExited += () => {
            Callable.From(() => {
                var h = logHost.GetViewport().GuiGetHoveredControl();
                if (h != copyFloatBtn)
                    HideCopyFloat();
            }).CallDeferred();
        };
        copyFloatBtn.MouseExited += () => {
            Callable.From(() => {
                var h = logHost.GetViewport().GuiGetHoveredControl();
                if (h != richText)
                    HideCopyFloat();
            }).CallDeferred();
        };
        copyFloatBtn.Pressed += () => {
            DisplayServer.ClipboardSet(richText.Text);
            copyFloatBtn.Icon = MdiIcon.Check.Texture(16, DevModeTheme.Accent);
            var t = copyFloatBtn.GetTree().CreateTimer(1.15);
            t.Timeout += () => {
                if (!GodotObject.IsInstanceValid(copyFloatBtn)) return;
                SetCopyFloatDefaultIcon();
            };
        };

        bodyHBox.AddChild(new ColorRect {
            CustomMinimumSize = new Vector2(1, 0),
            Color = DevModeTheme.ButtonBgNormal,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        });

        var statsColumn = new VBoxContainer {
            CustomMinimumSize = new Vector2(StatsSideMinW, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        statsColumn.AddThemeConstantOverride("separation", 6);

        var statsTitle = new Label { Text = I18N.T("log.stats.title", "来源统计") };
        statsTitle.AddThemeFontSizeOverride("font_size", 12);
        statsTitle.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        statsColumn.AddChild(statsTitle);

        var pieChart = new LogSourcePieChart();
        statsColumn.AddChild(pieChart);

        var statsScroll = new ScrollContainer {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            ClipContents = true,
        };
        statsScroll.AddThemeStyleboxOverride("panel", emptyBox);
        statsScroll.AddThemeStyleboxOverride("focus", emptyBox);

        var statsVBox = new VBoxContainer {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        statsVBox.AddThemeConstantOverride("separation", 4);
        statsScroll.AddChild(statsVBox);
        statsColumn.AddChild(statsScroll);

        bodyHBox.AddChild(statsColumn);
        vbox.AddChild(bodyHBox);

        // ── Footer: entry count ──
        var footerRow = new HBoxContainer();
        footerRow.AddThemeConstantOverride("separation", 8);

        var countLabel = new Label { Text = "" };
        countLabel.AddThemeFontSizeOverride("font_size", 11);
        countLabel.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        footerRow.AddChild(countLabel);
        footerRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        vbox.AddChild(footerRow);

        // ── State ──
        LogLevel? minLevel = null;
        string textFilter = "";
        var modVisible = new Dictionary<string, bool>(StringComparer.Ordinal);
        var modChips = new Dictionary<string, Button>(StringComparer.Ordinal);

        void SyncModFilterChips(HashSet<string> loadedModIds) {
            var discovered = new HashSet<string>(StringComparer.Ordinal) { "Game" };
            foreach (var id in loadedModIds)
                discovered.Add(id);

            foreach (var key in modChips.Keys.ToArray()) {
                if (discovered.Contains(key)) continue;
                modChips[key].QueueFree();
                modChips.Remove(key);
                modVisible.Remove(key);
            }

            var ordered = discovered
                .OrderBy(k => k == "Game" ? 0 : 1)
                .ThenBy(k => k, StringComparer.Ordinal)
                .ToList();

            foreach (var id in ordered) {
                if (modChips.ContainsKey(id))
                    continue;

                if (!modVisible.ContainsKey(id))
                    modVisible[id] = true;

                var chip = DevPanelUI.CreateFilterChip(ShortModLabel(id), active: modVisible[id]);
                chip.TooltipText = id;
                chip.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                var capturedId = id;
                chip.Toggled += v => {
                    modVisible[capturedId] = v;
                    Repopulate();
                };
                modChips[id] = chip;
                modFilterVBox.AddChild(chip);
            }
        }

        void ScrollToBottom() {
            int lines = richText.GetLineCount();
            if (lines > 0)
                richText.ScrollToLine(lines - 1);
            var bar = richText.GetVScrollBar();
            if (bar != null)
                bar.Value = bar.MaxValue;
        }

        string BuildBbCode(List<LogCollector.Entry> entries) {
            var sb = new StringBuilder(entries.Count * 80);
            foreach (var e in entries) {
                string col = LevelColor(e.Level);
                string badge = LevelBadge(e.Level);
                string time = e.Time.ToString("HH:mm:ss");
                string text = EscapeBbCode(e.Text);
                sb.Append($"[color={ColTime}]{time}[/color] [color={col}]{badge,-4} {text}[/color]\n");
            }
            return sb.ToString();
        }

        void Repopulate() {
            var entries = LogCollector.GetSnapshot();
            LogCollector.MarkClean();

            LogSuppressor.ResetCounts();
            var loadedModIds = ModRuntime.Catalog.GetIdSet();
            SyncModFilterChips(loadedModIds);
            var (filtered, suppressed, modStats) = FilterEntries(entries, minLevel, textFilter, modVisible, loadedModIds);

            richText.Text = BuildBbCode(filtered);

            // Update count label
            countLabel.Text = suppressed > 0
                ? I18N.T("log.count.withNoise", "{0} entries  •  {1} filtered", filtered.Count, suppressed)
                : I18N.T("log.count", "{0} entries", filtered.Count);

            // Update rule chip labels with hit counts
            for (int i = 0; i < LogSuppressor.BuiltInRules.Length; i++) {
                var rule = LogSuppressor.BuiltInRules[i];
                var chip = ruleChips[i];
                // Short label: first word(s) of pattern + count
                string shortLabel = ShortRuleLabel(rule.Pattern);
                chip.Text = rule.HitCount > 0
                    ? $"{shortLabel} ({rule.HitCount})"
                    : shortLabel;
            }

            // Update stats panel + pie chart
            pieChart.SetData(modStats);
            RefreshStatsPanel(statsVBox, modStats);

            ((SceneTree)Engine.GetMainLoop()).CreateTimer(0.05).Timeout += ScrollToBottom;
        }

        // ── Level chip logic ──
        void SetMinLevel(LogLevel? level) {
            minLevel = level;
            chipAll.ButtonPressed = level == null;
            chipInfo.ButtonPressed = level == LogLevel.Info;
            chipWarn.ButtonPressed = level == LogLevel.Warn;
            chipError.ButtonPressed = level == LogLevel.Error;
            Repopulate();
        }

        chipAll.Pressed += () => SetMinLevel(null);
        chipInfo.Pressed += () => SetMinLevel(LogLevel.Info);
        chipWarn.Pressed += () => SetMinLevel(LogLevel.Warn);
        chipError.Pressed += () => SetMinLevel(LogLevel.Error);

        // Wire rule chip toggles to repopulate
        foreach (var chip in ruleChips)
            chip.Toggled += _ => Repopulate();

        searchInput.TextChanged += t => { textFilter = t; Repopulate(); };

        void OnClear() { LogCollector.Clear(); Repopulate(); }
        BuildHeaderClearWire(vbox, OnClear);

        // ── Auto-refresh timer (1 s) ──
        var timer = new Timer { WaitTime = 1.0, Autostart = true };
        timer.Timeout += () => { if (LogCollector.IsDirty) Repopulate(); };
        root.AddChild(timer);

        globalUi.AddChild(root);
        Repopulate();
    }

    public static void Remove(NGlobalUi globalUi) {
        globalUi.GetNodeOrNull<Control>(RootName)?.QueueFree();
    }

    // ── Filtering ────────────────────────────────────────────────────────

    private static (List<LogCollector.Entry> filtered, int suppressed, Dictionary<string, int> modStats)
        FilterEntries(
            List<LogCollector.Entry> entries,
            LogLevel? minLevel,
            string textFilter,
            Dictionary<string, bool> modSourceFilter,
            HashSet<string> loadedModIds) {
        var result = new List<LogCollector.Entry>(entries.Count);
        var modStats = new Dictionary<string, int>(StringComparer.Ordinal);
        int suppressed = 0;

        foreach (var e in entries) {
            if (minLevel != null && e.Level < minLevel.Value) continue;
            if (!string.IsNullOrWhiteSpace(textFilter) &&
                !e.Text.Contains(textFilter, StringComparison.OrdinalIgnoreCase)) continue;

            string source = ParseSource(e.Text, loadedModIds);
            if (modSourceFilter.TryGetValue(source, out var showMod) && !showMod)
                continue;

            if (LogSuppressor.IsSuppressed(e.Text)) { suppressed++; continue; }

            result.Add(e);

            modStats.TryGetValue(source, out int n);
            modStats[source] = n + 1;
        }
        return (result, suppressed, modStats);
    }

    /// <summary>
    /// Resolves log source by matching bracket tags against <see cref="ModRuntime.Catalog"/> ids only.
    /// Walks <c>[...]</c> segments from the left and returns the first whose inner text is a loaded mod id; otherwise "Game".
    /// </summary>
    private static string ParseSource(string text, HashSet<string> loadedModIds) {
        if (loadedModIds.Count == 0 || string.IsNullOrEmpty(text))
            return "Game";

        int i = 0;
        while (i < text.Length) {
            int open = text.IndexOf('[', i);
            if (open < 0) break;
            int close = text.IndexOf(']', open + 1);
            if (close <= open + 1) {
                i = open + 1;
                continue;
            }

            string inner = text.Substring(open + 1, close - open - 1);
            if (loadedModIds.Contains(inner))
                return inner;

            i = close + 1;
        }

        return "Game";
    }

    private static void RefreshStatsPanel(
        VBoxContainer statsVBox,
        Dictionary<string, int> modStats) {
        foreach (Node child in statsVBox.GetChildren())
            child.QueueFree();

        if (modStats.Count == 0) {
            var empty = new Label { Text = I18N.T("log.stats.empty", "无可见日志") };
            empty.AddThemeFontSizeOverride("font_size", 11);
            empty.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
            statsVBox.AddChild(empty);
            return;
        }

        // Sort: Game first, then mods by count desc
        var sorted = new List<KeyValuePair<string, int>>(modStats);
        sorted.Sort((a, b) => {
            if (a.Key == "Game") return -1;
            if (b.Key == "Game") return 1;
            return b.Value.CompareTo(a.Value);
        });

        foreach (var (source, count) in sorted) {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 4);

            var nameLabel = new Label {
                Text = source,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ClipText = true,
            };
            nameLabel.AddThemeFontSizeOverride("font_size", 11);
            nameLabel.AddThemeColorOverride("font_color",
                source == "Game" ? DevModeTheme.Subtle : DevModeTheme.TextPrimary);
            nameLabel.TooltipText = source;
            row.AddChild(nameLabel);

            var countLabel = new Label { Text = count.ToString() };
            countLabel.AddThemeFontSizeOverride("font_size", 11);
            countLabel.AddThemeColorOverride("font_color", DevModeTheme.Accent);
            row.AddChild(countLabel);

            statsVBox.AddChild(row);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Short label for mod id chips in the log filter rail.</summary>
    private static string ShortModLabel(string modId) {
        const int MaxLen = 26;
        if (modId.Length <= MaxLen) return modId;
        return modId[..(MaxLen - 1)] + "…";
    }

    /// <summary>Produce a compact display label for a rule pattern.</summary>
    private static string ShortRuleLabel(string pattern) {
        // Take up to the first 22 chars, trim at last space before cutoff
        const int MaxLen = 22;
        if (pattern.Length <= MaxLen) return pattern;
        int cut = pattern.LastIndexOf(' ', MaxLen);
        return cut > 4 ? pattern[..cut] + "…" : pattern[..MaxLen] + "…";
    }

    private static string LevelBadge(LogLevel level) => level switch {
        LogLevel.Error => "ERR ",
        LogLevel.Warn => "WARN",
        LogLevel.Info => "INFO",
        LogLevel.Load => "LOAD",
        LogLevel.Debug => "DBG ",
        LogLevel.VeryDebug => "VDB ",
        _ => "?   "
    };

    private static string LevelColor(LogLevel level) => level switch {
        LogLevel.Error => ColError,
        LogLevel.Warn => ColWarn,
        LogLevel.Info => ColInfo,
        _ => ColDebug
    };

    /// <summary>Escape characters that would be interpreted as BBCode tags.</summary>
    private static string EscapeBbCode(string text)
        => text.Replace("[", "[lb]");

    // ── Header builder ────────────────────────────────────────────────────

    private static Button? _clearBtn;

    private static void BuildHeader(VBoxContainer vbox, Action onClose) {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var title = new Label { Text = I18N.T("log.title", "Log Viewer") };
        title.AddThemeFontSizeOverride("font_size", 13);
        title.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        title.AddThemeConstantOverride("margin_left", 4);
        row.AddChild(title);

        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        _clearBtn = new Button {
            Text = I18N.T("log.clear", "Clear"),
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(64, 26),
            Icon = MdiIcon.Delete.Texture(14, DevModeTheme.Subtle)
        };
        ApplySmallFlatButton(_clearBtn);
        row.AddChild(_clearBtn);

        var closeBtn = new Button {
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(28, 28),
            Icon = MdiIcon.Close.Texture(16, DevModeTheme.Subtle)
        };
        ApplySmallFlatButton(closeBtn);
        closeBtn.Pressed += onClose;
        row.AddChild(closeBtn);

        vbox.AddChild(row);
        vbox.AddChild(new ColorRect {
            CustomMinimumSize = new Vector2(0, 1),
            Color = DevModeTheme.ButtonBgNormal,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });
    }

    private static void BuildHeaderClearWire(VBoxContainer _, Action onClear) {
        if (_clearBtn != null)
            _clearBtn.Pressed += onClear;
        _clearBtn = null;
    }

    private static void ApplySmallFlatButton(Button btn) {
        var normal = new StyleBoxFlat {
            BgColor = Colors.Transparent,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 3,
            ContentMarginBottom = 3,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        var hover = new StyleBoxFlat {
            BgColor = DevModeTheme.ButtonBgNormal,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 3,
            ContentMarginBottom = 3,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus", normal);
        btn.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
        btn.AddThemeColorOverride("font_hover_color", DevModeTheme.TextPrimary);
        btn.AddThemeFontSizeOverride("font_size", 12);
    }
}
