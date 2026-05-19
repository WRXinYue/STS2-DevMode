using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevMode.CombatStats;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

/// <summary>DevPanel overlay for per-combat damage statistics (MVP).</summary>
internal static partial class CombatStatsUI {
    private const string RootName = "DevModeCombatStats";
    private const float PanelW = 720f;
    private const double AutoRefreshIntervalSec = 0.5;
    private const float BarAnimDuration = 0.22f;
    private const float ValueAnimDuration = 0.18f;

    private enum ViewMode {
        Summary,
        ByCard,
        BySource,
        ByTurn,
    }

    public static void Show(NGlobalUi globalUi) {
        Remove(globalUi);

        var (root, _, vbox) = DevPanelUI.CreateBrowserOverlayShell(
            globalUi, RootName, PanelW, () => Remove(globalUi), contentSeparation: 10);

        var titleBox = new VBoxContainer();
        titleBox.AddThemeConstantOverride("separation", 4);
        titleBox.AddChild(DevPanelUI.CreatePanelTitle(I18N.T("combatStats.title", "Combat Stats")));
        var subtitle = new Label {
            Text = I18N.T("combatStats.subtitle",
                "Live combat statistics from CombatHistory. Updates during fights; last combat is kept after victory."),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        subtitle.AddThemeFontSizeOverride("font_size", 11);
        subtitle.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        titleBox.AddChild(subtitle);
        vbox.AddChild(titleBox);
        vbox.AddChild(DevPanelUI.CreateOverlaySeparator());

        var statusLabel = new Label { Text = "" };
        statusLabel.AddThemeFontSizeOverride("font_size", 11);
        statusLabel.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        vbox.AddChild(statusLabel);

        var chipRow = new HBoxContainer();
        chipRow.AddThemeConstantOverride("separation", 6);

        var chipSummary = DevPanelUI.CreateFilterChip(I18N.T("combatStats.view.summary", "Summary"), active: true);
        var chipByCard = DevPanelUI.CreateFilterChip(I18N.T("combatStats.view.byCard", "By card"), active: false);
        var chipBySource = DevPanelUI.CreateFilterChip(I18N.T("combatStats.view.bySource", "Damage taken"), active: false);
        var chipByTurn = DevPanelUI.CreateFilterChip(I18N.T("combatStats.view.byTurn", "By turn"), active: false);
        chipRow.AddChild(chipSummary);
        chipRow.AddChild(chipByCard);
        chipRow.AddChild(chipBySource);
        chipRow.AddChild(chipByTurn);
        vbox.AddChild(chipRow);

        var scroll = new ScrollContainer {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        var inner = new VBoxContainer {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
        };
        inner.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(inner);
        vbox.AddChild(scroll);

        void SyncInnerWidth() {
            inner.CustomMinimumSize = new Vector2(Math.Max(scroll.Size.X, 1f), 0f);
        }
        scroll.Resized += SyncInnerWidth;
        scroll.TreeEntered += SyncInnerWidth;

        var autoRefresh = new CheckButton {
            Text = I18N.T("combatStats.autoRefresh", "Auto-refresh"),
            ButtonPressed = true,
        };
        autoRefresh.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(autoRefresh);

        ViewMode mode = ViewMode.Summary;
        string? contentFingerprint = null;

        void SetMode(ViewMode next) {
            mode = next;
            contentFingerprint = null;
            chipSummary.SetPressedNoSignal(next == ViewMode.Summary);
            chipByCard.SetPressedNoSignal(next == ViewMode.ByCard);
            chipBySource.SetPressedNoSignal(next == ViewMode.BySource);
            chipByTurn.SetPressedNoSignal(next == ViewMode.ByTurn);
            UpdateDisplay(forceRebuild: true, animate: false);
        }

        chipSummary.Pressed += () => SetMode(ViewMode.Summary);
        chipByCard.Pressed += () => SetMode(ViewMode.ByCard);
        chipBySource.Pressed += () => SetMode(ViewMode.BySource);
        chipByTurn.Pressed += () => SetMode(ViewMode.ByTurn);

        void UpdateDisplay(bool forceRebuild, bool animate) {
            var snap = CombatStatsTracker.IsTracking
                ? CombatStatsTracker.Current
                : CombatStatsTracker.Last;

            if (snap == null || snap.Players.Count == 0) {
                if (forceRebuild || inner.GetChildCount() == 0) {
                    ClearScrollContent(inner);
                    contentFingerprint = null;
                }
                statusLabel.Text = I18N.T("combatStats.empty", "No combat data yet. Enter a fight to begin tracking.");
                if (inner.GetChildCount() == 0)
                    inner.AddChild(MakeHintLabel(I18N.T("combatStats.emptyHint",
                        "Tracking starts automatically when Dev Mode is active and a combat begins.")));
                ResetScrollLayout(scroll);
                return;
            }

            string encounter = string.IsNullOrEmpty(snap.EncounterKey) ? "—" : snap.EncounterKey;
            statusLabel.Text = snap.IsActive
                ? I18N.T("combatStats.status.live", "Live — {0}", encounter)
                : I18N.T("combatStats.status.last", "Last combat — {0}", encounter);

            var player = snap.PrimaryPlayer;
            if (player == null) {
                ResetScrollLayout(scroll);
                return;
            }

            string fingerprint = BuildFingerprint(mode, player, snap.MaxTurn);
            bool canRefresh = !forceRebuild
                              && contentFingerprint == fingerprint
                              && inner.GetChildCount() > 0;

            if (canRefresh) {
                RefreshContent(inner, mode, player, snap.MaxTurn, animate);
                return;
            }

            ClearScrollContent(inner);
            contentFingerprint = fingerprint;
            scroll.ScrollVertical = 0;

            switch (mode) {
                case ViewMode.Summary:
                    BuildSummary(inner, player, snap.MaxTurn, animate: false);
                    break;
                case ViewMode.ByCard:
                    BuildRankedList(inner, player.DamageByCard,
                        I18N.T("combatStats.col.card", "Card"),
                        player.DamageDealt, animate: false);
                    break;
                case ViewMode.BySource:
                    BuildRankedList(inner, player.DamageTakenBySource,
                        I18N.T("combatStats.col.source", "Source"),
                        player.DamageTaken, animate: false);
                    break;
                case ViewMode.ByTurn:
                    BuildTurnList(inner, player.DamagePerTurn, player.DamageDealt, animate: false);
                    break;
            }

            ResetScrollLayout(scroll);
        }

        Action onStatsChanged = () => {
            if (!GodotObject.IsInstanceValid(root)) return;
            UpdateDisplay(forceRebuild: false, animate: true);
        };
        CombatStatsTracker.Changed += onStatsChanged;
        root.TreeExiting += () => CombatStatsTracker.Changed -= onStatsChanged;

        var timer = new Timer {
            WaitTime = AutoRefreshIntervalSec,
            OneShot = false,
            Autostart = true,
        };
        timer.Timeout += () => {
            if (autoRefresh.ButtonPressed)
                UpdateDisplay(forceRebuild: false, animate: true);
        };
        root.AddChild(timer);

        UpdateDisplay(forceRebuild: true, animate: false);
        ((Node)globalUi).AddChild(root);
    }

    private static void ClearScrollContent(VBoxContainer inner) {
        while (inner.GetChildCount() > 0) {
            var child = inner.GetChild(0);
            inner.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static void ResetScrollLayout(ScrollContainer scroll) {
        scroll.ScrollVertical = 0;
        Callable.From(() => {
            if (GodotObject.IsInstanceValid(scroll))
                scroll.ScrollVertical = 0;
        }).CallDeferred();
    }

    public static void Remove(NGlobalUi globalUi) {
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
    }

    private static string BuildFingerprint(ViewMode mode, PlayerCombatStats player, int maxTurn) {
        var sb = new StringBuilder(128);
        sb.Append((int)mode).Append('|');
        switch (mode) {
            case ViewMode.Summary:
                sb.Append(maxTurn).Append('|');
                AppendKeys(sb, TopEntries(player.DamageByCard, 5).Select(x => x.Name));
                sb.Append('|');
                AppendKeys(sb, player.DamagePerTurn.OrderBy(kv => kv.Key).Take(8).Select(k => k.Key.ToString()));
                break;
            case ViewMode.ByCard:
                AppendKeys(sb, TopEntries(player.DamageByCard, 24).Select(x => x.Name));
                break;
            case ViewMode.BySource:
                AppendKeys(sb, TopEntries(player.DamageTakenBySource, 24).Select(x => x.Name));
                break;
            case ViewMode.ByTurn:
                AppendKeys(sb, player.DamagePerTurn.OrderBy(kv => kv.Key).Select(k => k.Key.ToString()));
                break;
        }
        return sb.ToString();
    }

    private static void AppendKeys(StringBuilder sb, IEnumerable<string> keys) {
        foreach (var key in keys)
            sb.Append(key).Append('\u001f');
    }

    private static void RefreshContent(
        VBoxContainer inner,
        ViewMode mode,
        PlayerCombatStats player,
        int maxTurn,
        bool animate) {
        switch (mode) {
            case ViewMode.Summary:
                FindValueRow(inner, "stat.dealt")?.SetValue(player.DamageDealt, animate);
                FindValueRow(inner, "stat.hits")?.SetValue(player.HitCount, animate);
                FindValueRow(inner, "stat.cards")?.SetValue(player.CardsPlayed, animate);
                FindValueRow(inner, "stat.turns")?.SetValue(maxTurn, animate);
                FindValueRow(inner, "stat.taken")?.SetValue(player.DamageTaken, animate);
                FindValueRow(inner, "stat.block")?.SetValue(player.BlockGained, animate);
                RefreshBarRows(inner, TopEntries(player.DamageByCard, 5), player.DamageDealt, animate);
                RefreshBarRows(inner,
                    player.DamagePerTurn.OrderBy(kv => kv.Key).Take(8)
                        .Select(kv => (I18N.T("combatStats.turnLabel", "Turn {0}", kv.Key), kv.Value)),
                    player.DamageDealt, animate);
                break;
            case ViewMode.ByCard:
                RefreshBarRows(inner, TopEntries(player.DamageByCard, 24), player.DamageDealt, animate);
                break;
            case ViewMode.BySource:
                RefreshBarRows(inner, TopEntries(player.DamageTakenBySource, 24), player.DamageTaken, animate);
                break;
            case ViewMode.ByTurn:
                RefreshBarRows(inner,
                    player.DamagePerTurn.OrderBy(kv => kv.Key)
                        .Select(kv => (I18N.T("combatStats.turnLabel", "Turn {0}", kv.Key), kv.Value)),
                    player.DamageDealt, animate);
                break;
        }
    }

    private static StatValueRow? FindValueRow(Node root, string id) =>
        root.FindChild(id, recursive: true, owned: false) as StatValueRow;

    private static void RefreshBarRows(
        Node root,
        IEnumerable<(string Name, int Amount)> entries,
        int maxAmount,
        bool animate) {
        int max = Math.Max(maxAmount, 1);
        foreach (var (name, amount) in entries) {
            var row = root.FindChild(StatBarRow.NameForKey(name), recursive: true, owned: false) as StatBarRow;
            row?.SetData(name, amount, max, animate);
        }
    }

    private static void BuildSummary(VBoxContainer parent, PlayerCombatStats player, int maxTurn, bool animate) {
        parent.AddChild(MakeSectionCard(I18N.T("combatStats.section.offense", "Offense"), section => {
            section.AddChild(MakeValueRow("stat.dealt", I18N.T("combatStats.dealt", "Damage dealt"), player.DamageDealt, animate));
            section.AddChild(MakeValueRow("stat.hits", I18N.T("combatStats.hits", "Hit count"), player.HitCount, animate));
            section.AddChild(MakeValueRow("stat.cards", I18N.T("combatStats.cardsPlayed", "Cards played"), player.CardsPlayed, animate));
            section.AddChild(MakeValueRow("stat.turns", I18N.T("combatStats.turns", "Turns recorded"), maxTurn, animate));
        }));

        parent.AddChild(MakeSectionCard(I18N.T("combatStats.section.defense", "Defense"), section => {
            section.AddChild(MakeValueRow("stat.taken", I18N.T("combatStats.taken", "Damage taken"), player.DamageTaken, animate));
            section.AddChild(MakeValueRow("stat.block", I18N.T("combatStats.block", "Block gained"), player.BlockGained, animate));
        }));

        if (player.DamageByCard.Count > 0) {
            parent.AddChild(MakeSectionCard(I18N.T("combatStats.section.topCards", "Top cards"), section => {
                foreach (var (name, amount) in TopEntries(player.DamageByCard, 5))
                    section.AddChild(MakeBarRow(name, amount, player.DamageDealt, animate));
            }));
        }

        if (player.DamagePerTurn.Count > 0) {
            parent.AddChild(MakeSectionCard(I18N.T("combatStats.section.topTurns", "Damage per turn"), section => {
                foreach (var (turn, amount) in player.DamagePerTurn.OrderBy(kv => kv.Key).Take(8)) {
                    string label = I18N.T("combatStats.turnLabel", "Turn {0}", turn);
                    section.AddChild(MakeBarRow(label, amount, player.DamageDealt, animate));
                }
            }));
        }
    }

    private static void BuildRankedList(
        VBoxContainer parent,
        Dictionary<string, int> data,
        string nameHeader,
        int totalForBars,
        bool animate) {
        if (data.Count == 0) {
            parent.AddChild(MakeHintLabel(I18N.T("combatStats.noData", "No entries yet.")));
            return;
        }

        parent.AddChild(MakeSectionCard(nameHeader, section => {
            foreach (var (name, amount) in TopEntries(data, 24))
                section.AddChild(MakeBarRow(name, amount, Math.Max(totalForBars, 1), animate));
        }));
    }

    private static void BuildTurnList(
        VBoxContainer parent,
        Dictionary<int, int> data,
        int totalDealt,
        bool animate) {
        if (data.Count == 0) {
            parent.AddChild(MakeHintLabel(I18N.T("combatStats.noData", "No entries yet.")));
            return;
        }

        parent.AddChild(MakeSectionCard(I18N.T("combatStats.view.byTurn", "By turn"), section => {
            foreach (var (turn, amount) in data.OrderBy(kv => kv.Key)) {
                string label = I18N.T("combatStats.turnLabel", "Turn {0}", turn);
                section.AddChild(MakeBarRow(label, amount, Math.Max(totalDealt, 1), animate));
            }
        }));
    }

    private static IEnumerable<(string Name, int Amount)> TopEntries(Dictionary<string, int> data, int limit) =>
        data.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Take(limit)
            .Select(kv => (kv.Key, kv.Value));

    private static Control MakeSectionCard(string title, Action<VBoxContainer> fillBody) {
        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var style = new StyleBoxFlat {
            BgColor = new Color(DevModeTheme.PanelBg.R, DevModeTheme.PanelBg.G, DevModeTheme.PanelBg.B, 0.55f),
            BorderColor = DevModeTheme.PanelBorder,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 12,
            ContentMarginBottom = 14,
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var outer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        outer.AddThemeConstantOverride("separation", 8);

        var head = new Label { Text = title };
        head.AddThemeFontSizeOverride("font_size", 13);
        head.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        outer.AddChild(head);

        var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 6);
        fillBody(body);
        outer.AddChild(body);

        panel.AddChild(outer);
        return panel;
    }

    private static StatValueRow MakeValueRow(string id, string label, int value, bool animate) {
        var row = new StatValueRow(label, value, animate) { Name = id };
        return row;
    }

    private static StatBarRow MakeBarRow(string name, int amount, int maxAmount, bool animate) {
        var row = new StatBarRow(name, amount, Math.Max(maxAmount, 1), animate) {
            Name = StatBarRow.NameForKey(name),
        };
        return row;
    }

    private static Label MakeHintLabel(string text) {
        var l = new Label {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        l.AddThemeFontSizeOverride("font_size", 11);
        l.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        return l;
    }

    /// <summary>Label + animated integer value.</summary>
    private sealed partial class StatValueRow : HBoxContainer {
        private readonly Label _valueLabel;
        private int _displayed;
        private Tween? _tween;

        public StatValueRow(string label, int value, bool animate) {
            AddThemeConstantOverride("separation", 10);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var left = new Label {
                Text = label + ":",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            left.AddThemeFontSizeOverride("font_size", 11);
            left.AddThemeColorOverride("font_color", DevModeTheme.Subtle);

            _valueLabel = new Label {
                HorizontalAlignment = HorizontalAlignment.Right,
                CustomMinimumSize = new Vector2(52, 0),
            };
            _valueLabel.AddThemeFontSizeOverride("font_size", 11);
            _valueLabel.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);

            AddChild(left);
            AddChild(_valueLabel);
            SetValue(value, animate);
        }

        public void SetValue(int value, bool animate) {
            if (!animate || _displayed == value) {
                _tween?.Kill();
                _displayed = value;
                _valueLabel.Text = value.ToString();
                return;
            }

            int start = _displayed;
            _tween?.Kill();
            _tween = CreateTween();
            _tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            _tween.TweenMethod(Callable.From((float t) => {
                int v = (int)Math.Round(start + (value - start) * t);
                _displayed = v;
                _valueLabel.Text = v.ToString();
            }), 0f, 1f, ValueAnimDuration);
            _tween.Finished += () => {
                _displayed = value;
                _valueLabel.Text = value.ToString();
            };
        }
    }

    /// <summary>One ranked stat row: text line above, bar below (never overlaps).</summary>
    private sealed partial class StatBarRow : VBoxContainer {
        private readonly Label _nameLabel;
        private readonly Label _amountLabel;
        private readonly StatFractionBar _bar;

        public StatBarRow(string name, int amount, int maxAmount, bool animate) {
            AddThemeConstantOverride("separation", 4);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            ClipContents = true;

            var top = new HBoxContainer {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            top.AddThemeConstantOverride("separation", 8);

            _nameLabel = new Label {
                Text = name,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            _nameLabel.AddThemeFontSizeOverride("font_size", 11);
            _nameLabel.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);

            _amountLabel = new Label {
                Text = amount.ToString(),
                HorizontalAlignment = HorizontalAlignment.Right,
                CustomMinimumSize = new Vector2(52, 0),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            };
            _amountLabel.AddThemeFontSizeOverride("font_size", 11);
            _amountLabel.AddThemeColorOverride("font_color", DevModeTheme.TextSecondary);

            top.AddChild(_nameLabel);
            top.AddChild(_amountLabel);
            AddChild(top);

            float frac = maxAmount > 0 ? Math.Clamp((float)amount / maxAmount, 0f, 1f) : 0f;
            _bar = new StatFractionBar(animate ? 0f : frac);
            AddChild(_bar);
            _bar.SetFraction(frac, animate);
        }

        public static string NameForKey(string key) =>
            "bar." + key.Replace("/", "_").Replace(".", "_");

        public void SetData(string name, int amount, int maxAmount, bool animate) {
            _nameLabel.Text = name;
            AnimateAmount(_amountLabel, amount, animate);
            _bar.SetFraction(maxAmount > 0 ? Math.Clamp((float)amount / maxAmount, 0f, 1f) : 0f, animate);
        }

        private static void AnimateAmount(Label label, int target, bool animate) {
            if (!int.TryParse(label.Text, out int start))
                start = target;
            if (!animate || start == target) {
                label.Text = target.ToString();
                return;
            }

            var tween = label.CreateTween();
            tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.TweenMethod(Callable.From((float t) => {
                label.Text = ((int)Math.Round(start + (target - start) * t)).ToString();
            }), 0f, 1f, ValueAnimDuration);
            tween.Finished += () => label.Text = target.ToString();
        }
    }

    /// <summary>Track + fill using ColorRect (layout-safe, animatable width).</summary>
    private sealed partial class StatFractionBar : Control {
        private readonly ColorRect _fill;
        private float _displayFrac;
        private Tween? _tween;

        public StatFractionBar(float initialFraction) {
            _displayFrac = initialFraction;
            ClipContents = true;
            CustomMinimumSize = new Vector2(0, 8);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var track = new ColorRect {
                Color = new Color(DevModeTheme.ButtonBgNormal.R, DevModeTheme.ButtonBgNormal.G,
                    DevModeTheme.ButtonBgNormal.B, 0.9f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            track.SetAnchorsPreset(LayoutPreset.FullRect);

            _fill = new ColorRect {
                Color = DevModeTheme.Accent,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _fill.SetAnchorsPreset(LayoutPreset.TopLeft);
            _fill.AnchorBottom = 1f;

            AddChild(track);
            AddChild(_fill);
            ApplyFillWidth();
        }

        public void SetFraction(float fraction, bool animate) {
            fraction = Mathf.Clamp(fraction, 0f, 1f);
            if (!animate || Mathf.IsEqualApprox(_displayFrac, fraction)) {
                _tween?.Kill();
                _displayFrac = fraction;
                ApplyFillWidth();
                return;
            }

            float start = _displayFrac;
            _tween?.Kill();
            _tween = CreateTween();
            _tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            _tween.TweenMethod(Callable.From((float t) => {
                _displayFrac = Mathf.Lerp(start, fraction, t);
                ApplyFillWidth();
            }), 0f, 1f, BarAnimDuration);
            _tween.Finished += () => {
                _displayFrac = fraction;
                ApplyFillWidth();
            };
        }

        private void ApplyFillWidth() {
            _fill.AnchorRight = _displayFrac;
            _fill.OffsetRight = 0f;
        }
    }
}
