using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Actions;

namespace DevMode.UI;

/// <summary>Power browser — two-pane layout with icon grid on the left and detail / apply on the right.</summary>
internal static class PowerSelectUI
{
    private const string RootName     = "DevModePowerSelect";
    private const float  PanelW       = 920f;
    private const float  TileMinWidth = 72f;
    private const float  IconSize     = 44f;
    private const int    FrameRadius  = 6;
    private const float  GridHSep     = 8f;
    private const float  GridVSep     = 8f;

    private static readonly Color ColFrameBg       = new(0.14f, 0.14f, 0.20f, 1f);
    private static readonly Color ColFrameHover    = new(0.22f, 0.22f, 0.30f, 1f);
    private static readonly Color ColFrameSelected = new(0.24f, 0.34f, 0.50f, 1f);
    private static readonly Color ColDetailBg      = new(0.09f, 0.09f, 0.14f, 1f);
    private static readonly Color ColBuff          = new(0.30f, 0.75f, 0.45f);
    private static readonly Color ColDebuff        = new(0.85f, 0.35f, 0.30f);
    private static readonly Color ColNone          = new(0.55f, 0.55f, 0.65f);
    private static readonly Color ColLight         = new(0.85f, 0.85f, 0.90f);

    // ─────────────────────────────── State ───────────────────────────────

    private sealed class State
    {
        public required Player           Player;
        public List<PowerModel>          AllPowers      = [];
        public List<PowerModel>          Filtered       = [];
        public PowerModel?               Selected;
        public Panel?                    SelectedFrame;
        public PowerType?                TypeFilter;
        public string                    SearchText     = "";
        public PowerTarget               Target         = PowerTarget.Self;
        public int                       Amount         = 1;

        // Left pane
        public GridContainer             Grid           = null!;
        public ScrollContainer           GridScroll     = null!;
        public Label                     CountLabel     = null!;

        // Right detail pane
        public TextureRect               DetailIcon         = null!;
        public ColorRect                 DetailIconFallback = null!;
        public Label                     DetailName         = null!;
        public Label                     DetailTypeBadge    = null!;
        public Label                     DetailStackBadge   = null!;
        public RichTextLabel             DetailDesc         = null!;
        public Button                    BtnSelf            = null!;
        public Button                    BtnAllEnemies      = null!;
        public Button                    BtnAllies          = null!;
        public SpinBox                   AmountSpin         = null!;
        public Button                    ApplyBtn           = null!;
        public VBoxContainer             CurrentPowersList  = null!;
        public Label                     CombatWarningLabel = null!;
    }

    // ─────────────────────────────── Public API ───────────────────────────────

    public static void Show(NGlobalUi globalUi, Player player)
    {
        Remove(globalUi);

        DevPanelUI.PinRail();
        DevPanelUI.SpliceRail(globalUi, joined: true);

        var root = new Control { Name = RootName, MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 1250 };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.TreeExiting += () =>
        {
            DevPanelUI.UnpinRail();
            DevPanelUI.SpliceRail(globalUi, joined: false);
        };

        root.AddChild(DevPanelUI.CreateBrowserBackdrop(() => Remove(globalUi)));
        var panel = DevPanelUI.CreateBrowserPanel(PanelW);
        root.AddChild(panel);

        var s = new State { Player = player };
        s.AllPowers = PowerActions.GetAllPowers()
            .OrderBy(p => p.Type)
            .ThenBy(p => PowerActions.GetPowerDisplayName(p))
            .ToList();

        var vbox = panel.GetNode<VBoxContainer>("Content");
        vbox.AddThemeConstantOverride("separation", 8);

        // ── Nav bar (title + type filter chips + search) ──
        var (search, filterChips) = BuildNavBar(vbox, s);

        vbox.AddChild(MakeDivider());

        // ── Main body ──
        var body = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 14);
        vbox.AddChild(body);

        BuildLeftPane(body, s);
        BuildRightPane(body, s, player);

        // ── Wire filter chips ──
        WireFilterChips(filterChips, s);

        // ── Wire search ──
        search.TextChanged += filter =>
        {
            s.SearchText = filter;
            Rebuild(s);
        };

        Rebuild(s);
        ShowEmptyDetail(s);

        // Dynamic grid column sizing
        s.GridScroll.Resized += () => UpdateGridColumns(s);
        UpdateGridColumns(s);

        ((Node)globalUi).AddChild(root);
        search.GrabFocus();
    }

    public static void Remove(NGlobalUi globalUi)
        => ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();

    // ─────────────────────────────── Nav bar ───────────────────────────────

    private static (LineEdit search, List<(Button chip, PowerType? type)> chips) BuildNavBar(VBoxContainer vbox, State s)
    {
        // Row 1: title + type chips
        var row1 = new HBoxContainer();
        row1.AddThemeConstantOverride("separation", 10);

        var title = new Label
        {
            Text = I18N.T("power.nav.title", "Powers"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        row1.AddChild(title);

        var accentLine = new ColorRect
        {
            Color = DevModeTheme.Accent,
            CustomMinimumSize = new Vector2(2, 18),
        };
        row1.AddChild(accentLine);

        row1.AddChild(new Control { CustomMinimumSize = new Vector2(6, 0) });

        // Type filter chips
        var chipDefs = new (string label, PowerType? type)[]
        {
            (I18N.T("power.filter.all",    "All"),    null),
            (I18N.T("power.filter.buff",   "Buff"),   PowerType.Buff),
            (I18N.T("power.filter.debuff", "Debuff"), PowerType.Debuff),
        };

        var chips = new List<(Button chip, PowerType? type)>();
        foreach (var (label, type) in chipDefs)
        {
            var chip = DevPanelUI.CreateFilterChip(label);
            chip.ButtonPressed = type == null; // "All" active by default
            chip.ToggleMode = true;
            chips.Add((chip, type));
            row1.AddChild(chip);
        }

        row1.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        vbox.AddChild(row1);

        // Row 2: search bar
        var (searchRow, search) = DevPanelUI.CreateSearchRow(I18N.T("power.search", "Search powers..."));
        vbox.AddChild(searchRow);

        return (search, chips);
    }

    private static void WireFilterChips(List<(Button chip, PowerType? type)> chips, State s)
    {
        foreach (var (chip, type) in chips)
        {
            var capturedType = type;
            chip.Pressed += () =>
            {
                s.TypeFilter = capturedType;
                foreach (var (c, t) in chips)
                    c.ButtonPressed = t == capturedType;
                Rebuild(s);
            };
        }
    }

    // ─────────────────────────────── Left pane ───────────────────────────────

    private static void BuildLeftPane(HBoxContainer body, State s)
    {
        var pane = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical   = Control.SizeFlags.ExpandFill,
        };
        pane.AddThemeConstantOverride("separation", 6);

        s.GridScroll = new ScrollContainer
        {
            SizeFlagsVertical        = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode     = ScrollContainer.ScrollMode.Disabled,
        };

        var gridWrapper = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        gridWrapper.AddThemeConstantOverride("separation", 0);

        s.Grid = new GridContainer
        {
            Columns             = 6,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        s.Grid.AddThemeConstantOverride("h_separation", (int)GridHSep);
        s.Grid.AddThemeConstantOverride("v_separation", (int)GridVSep);

        gridWrapper.AddChild(s.Grid);
        s.GridScroll.AddChild(gridWrapper);
        pane.AddChild(s.GridScroll);

        s.CountLabel = new Label { HorizontalAlignment = HorizontalAlignment.Left };
        s.CountLabel.AddThemeFontSizeOverride("font_size", 11);
        s.CountLabel.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        pane.AddChild(s.CountLabel);

        body.AddChild(pane);
    }

    // ─────────────────────────────── Right detail pane ───────────────────────────────

    private static void BuildRightPane(HBoxContainer body, State s, Player player)
    {
        var pane = new PanelContainer
        {
            CustomMinimumSize   = new Vector2(280f, 0),
            SizeFlagsVertical   = Control.SizeFlags.ExpandFill,
        };
        var bgStyle = new StyleBoxFlat
        {
            BgColor                  = ColDetailBg,
            CornerRadiusTopLeft      = 8, CornerRadiusTopRight      = 8,
            CornerRadiusBottomLeft   = 8, CornerRadiusBottomRight   = 8,
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderColor = DevModeTheme.PanelBorder,
        };
        pane.AddThemeStyleboxOverride("panel", bgStyle);

        var margin = new MarginContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        margin.AddThemeConstantOverride("margin_left",   16);
        margin.AddThemeConstantOverride("margin_right",  16);
        margin.AddThemeConstantOverride("margin_top",    16);
        margin.AddThemeConstantOverride("margin_bottom", 16);

        var inner = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        inner.AddThemeConstantOverride("separation", 10);

        // ── Icon + name header ──
        var iconRow = new HBoxContainer();
        iconRow.AddThemeConstantOverride("separation", 12);

        var iconHost = new Control { CustomMinimumSize = new Vector2(60, 60) };
        var iconBg   = new Panel();
        iconBg.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor                = ColFrameBg,
            CornerRadiusTopLeft    = 8, CornerRadiusTopRight    = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        });
        iconBg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        iconHost.AddChild(iconBg);

        s.DetailIcon = new TextureRect
        {
            ExpandMode   = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode  = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(60, 60),
            Visible      = false,
        };
        iconHost.AddChild(s.DetailIcon);

        s.DetailIconFallback = new ColorRect
        {
            Color             = ColNone.Darkened(0.5f),
            CustomMinimumSize = new Vector2(60, 60),
            Visible           = true,
        };
        iconHost.AddChild(s.DetailIconFallback);

        iconRow.AddChild(iconHost);

        var nameCol = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        nameCol.AddThemeConstantOverride("separation", 5);

        s.DetailName = new Label
        {
            Text                = I18N.T("power.detail.placeholder", "Select a power"),
            AutowrapMode        = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        s.DetailName.AddThemeFontSizeOverride("font_size", 14);
        s.DetailName.AddThemeColorOverride("font_color", ColLight);
        nameCol.AddChild(s.DetailName);

        var badgeRow = new HBoxContainer();
        badgeRow.AddThemeConstantOverride("separation", 6);
        s.DetailTypeBadge  = MakeBadgeLabel("", ColNone);
        s.DetailStackBadge = MakeBadgeLabel("", DevModeTheme.Subtle);
        badgeRow.AddChild(s.DetailTypeBadge);
        badgeRow.AddChild(s.DetailStackBadge);
        nameCol.AddChild(badgeRow);

        iconRow.AddChild(nameCol);
        inner.AddChild(iconRow);

        // ── Description ──
        s.DetailDesc = DevModeTheme.CreateGameBbcodeLabel();
        s.DetailDesc.CustomMinimumSize = new Vector2(0, 50);
        s.DetailDesc.AddThemeFontSizeOverride("normal_font_size", 11);
        s.DetailDesc.AddThemeColorOverride("default_color", DevModeTheme.Subtle);
        inner.AddChild(s.DetailDesc);

        inner.AddChild(MakeDivider());

        // ── Combat warning (shown when not in combat) ──
        s.CombatWarningLabel = new Label
        {
            Text         = I18N.T("power.combat_only", "⚠  Powers only work during combat"),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Visible      = false,
        };
        s.CombatWarningLabel.AddThemeFontSizeOverride("font_size", 11);
        s.CombatWarningLabel.AddThemeColorOverride("font_color", new Color(1f, 0.78f, 0.28f));
        inner.AddChild(s.CombatWarningLabel);

        // ── Target buttons ──
        var targetHdr = new Label { Text = I18N.T("power.target.label", "Target") };
        targetHdr.AddThemeFontSizeOverride("font_size", 11);
        targetHdr.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        inner.AddChild(targetHdr);

        var targetRow = new HBoxContainer();
        targetRow.AddThemeConstantOverride("separation", 6);
        s.BtnSelf       = MakeTargetButton(I18N.T("power.target.self",       "Self"));
        s.BtnAllEnemies = MakeTargetButton(I18N.T("power.target.allEnemies", "All Enemies"));
        s.BtnAllies     = MakeTargetButton(I18N.T("power.target.allies",     "Allies"));
        s.BtnSelf      .Pressed += () => { s.Target = PowerTarget.Self;       SyncTargetButtons(s); };
        s.BtnAllEnemies.Pressed += () => { s.Target = PowerTarget.AllEnemies; SyncTargetButtons(s); };
        s.BtnAllies    .Pressed += () => { s.Target = PowerTarget.Allies;     SyncTargetButtons(s); };
        targetRow.AddChild(s.BtnSelf);
        targetRow.AddChild(s.BtnAllEnemies);
        targetRow.AddChild(s.BtnAllies);
        inner.AddChild(targetRow);
        SyncTargetButtons(s);

        // ── Amount row ──
        var amountRow = new HBoxContainer();
        amountRow.AddThemeConstantOverride("separation", 8);
        var amountLbl = new Label
        {
            Text                = I18N.T("power.amount", "Amount"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        amountLbl.AddThemeFontSizeOverride("font_size", 12);
        amountLbl.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        amountRow.AddChild(amountLbl);
        s.AmountSpin = new SpinBox { MinValue = 1, MaxValue = 999, Value = 1, Step = 1, CustomMinimumSize = new Vector2(88, 28) };
        s.AmountSpin.ValueChanged += v => s.Amount = (int)v;
        amountRow.AddChild(s.AmountSpin);
        inner.AddChild(amountRow);

        // ── Apply button ──
        s.ApplyBtn = new Button
        {
            Text                = I18N.T("power.apply", "Apply Power"),
            Disabled            = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize   = new Vector2(0, 34),
        };
        ApplyBtnStyle(s.ApplyBtn);
        s.ApplyBtn.Pressed += () =>
        {
            if (s.Selected == null) return;
            TaskHelper.RunSafely(PowerActions.AddPower(player, s.Selected, s.Amount, s.Target));
            RefreshCurrentPowers(s, player);
        };
        inner.AddChild(s.ApplyBtn);

        inner.AddChild(MakeDivider());

        // ── Active powers on player ──
        var curHdr = new HBoxContainer();
        curHdr.AddThemeConstantOverride("separation", 8);
        var curLbl = new Label
        {
            Text                = I18N.T("power.current", "Active Powers"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        curLbl.AddThemeFontSizeOverride("font_size", 11);
        curLbl.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        curHdr.AddChild(curLbl);

        var clearBtn = new Button { Text = I18N.T("power.clear_all", "Clear All"), FocusMode = Control.FocusModeEnum.None };
        clearBtn.AddThemeFontSizeOverride("font_size", 10);
        clearBtn.Pressed += () =>
        {
            if (player.Creature != null)
                PowerActions.RemoveAllPowers(player.Creature);
            RefreshCurrentPowers(s, player);
        };
        curHdr.AddChild(clearBtn);
        inner.AddChild(curHdr);

        var curScroll = new ScrollContainer
        {
            SizeFlagsVertical        = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode     = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize        = new Vector2(0, 60),
        };
        s.CurrentPowersList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        s.CurrentPowersList.AddThemeConstantOverride("separation", 4);
        curScroll.AddChild(s.CurrentPowersList);
        inner.AddChild(curScroll);

        margin.AddChild(inner);
        pane.AddChild(margin);
        body.AddChild(pane);

        RefreshCurrentPowers(s, player);
    }

    // ─────────────────────────────── Grid rebuild ───────────────────────────────

    private static void Rebuild(State s)
    {
        foreach (var child in s.Grid.GetChildren())
            ((Node)child).QueueFree();

        var list = s.AllPowers.AsEnumerable();

        if (s.TypeFilter.HasValue)
            list = list.Where(p => p.Type == s.TypeFilter.Value);

        if (!string.IsNullOrWhiteSpace(s.SearchText))
            list = list.Where(p => PowerActions.GetPowerDisplayName(p)
                .Contains(s.SearchText, StringComparison.OrdinalIgnoreCase));

        s.Filtered = list.ToList();

        // Reselect tile if previously selected power is still visible
        s.SelectedFrame = null;

        foreach (var power in s.Filtered)
            s.Grid.AddChild(CreatePowerTile(power, s));

        s.CountLabel.Text = I18N.T("power.count", "{0} powers", s.Filtered.Count);
    }

    private static void UpdateGridColumns(State s)
    {
        var available = s.GridScroll.Size.X;
        if (available <= 0) return;
        var cols = Math.Max(1, (int)((available + GridHSep) / (TileMinWidth + GridHSep)));
        if (s.Grid.Columns != cols)
            s.Grid.Columns = cols;
    }

    // ─────────────────────────────── Power tile ───────────────────────────────

    private static Control CreatePowerTile(PowerModel power, State s)
    {
        var typeCol = TypeColor(power.Type);
        var name    = PowerActions.GetPowerDisplayName(power);

        var outer = new VBoxContainer
        {
            CustomMinimumSize   = new Vector2(TileMinWidth, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText         = name,
            MouseFilter         = Control.MouseFilterEnum.Stop,
        };
        outer.AddThemeConstantOverride("separation", 4);

        // ── Icon frame ──
        var frameCenter = new CenterContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize   = new Vector2(0, TileMinWidth),
            MouseFilter         = Control.MouseFilterEnum.Ignore,
        };
        var frameSize = TileMinWidth - 4f;
        var frameHost = new Control
        {
            CustomMinimumSize = new Vector2(frameSize, frameSize),
            MouseFilter       = Control.MouseFilterEnum.Ignore,
        };

        var frame = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        frame.AddThemeStyleboxOverride("panel", MakeFrameStyle(ColFrameBg, typeCol, 0.40f));
        frame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        frameHost.AddChild(frame);

        // Icon
        Texture2D? iconTex = null;
        try { iconTex = power.Icon; } catch { /* missing atlas entry */ }

        if (iconTex != null)
        {
            frameHost.AddChild(new TextureRect
            {
                Texture           = iconTex,
                ExpandMode        = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(IconSize, IconSize),
                Position          = new Vector2((frameSize - IconSize) / 2f, (frameSize - IconSize) / 2f),
                MouseFilter       = Control.MouseFilterEnum.Ignore,
            });
        }
        else
        {
            frameHost.AddChild(new ColorRect
            {
                Color             = typeCol.Darkened(0.5f),
                CustomMinimumSize = new Vector2(IconSize, IconSize),
                Position          = new Vector2((frameSize - IconSize) / 2f, (frameSize - IconSize) / 2f),
                MouseFilter       = Control.MouseFilterEnum.Ignore,
            });
        }

        frameCenter.AddChild(frameHost);
        outer.AddChild(frameCenter);

        // ── Name ──
        var nameColor = typeCol.Lerp(ColLight, 0.45f);
        var label = new Label
        {
            Text                = name,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode        = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize   = new Vector2(0, 26),
            MouseFilter         = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 10);
        label.AddThemeColorOverride("font_color", nameColor);
        outer.AddChild(label);

        // ── Mouse interaction ──
        outer.MouseEntered += () =>
        {
            if (s.SelectedFrame != frame)
                frame.AddThemeStyleboxOverride("panel", MakeFrameStyle(ColFrameHover, typeCol, 0.70f));
        };
        outer.MouseExited += () =>
        {
            if (s.SelectedFrame != frame)
                frame.AddThemeStyleboxOverride("panel", MakeFrameStyle(ColFrameBg, typeCol, 0.40f));
        };
        outer.GuiInput += evt =>
        {
            if (evt is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
                SelectTile(s, frame, power, typeCol);
        };

        return outer;
    }

    private static void SelectTile(State s, Panel newFrame, PowerModel power, Color typeCol)
    {
        if (s.SelectedFrame != null && s.Selected != null)
        {
            var oldCol = TypeColor(s.Selected.Type);
            s.SelectedFrame.AddThemeStyleboxOverride("panel", MakeFrameStyle(ColFrameBg, oldCol, 0.40f));
        }

        s.Selected      = power;
        s.SelectedFrame = newFrame;
        newFrame.AddThemeStyleboxOverride("panel", MakeFrameStyle(ColFrameSelected, typeCol, 0.95f));

        ShowPowerDetail(s, power);
    }

    // ─────────────────────────────── Detail update ───────────────────────────────

    private static void ShowEmptyDetail(State s)
    {
        s.DetailName.Text           = I18N.T("power.detail.placeholder", "Select a power");
        s.DetailTypeBadge.Text      = "";
        s.DetailStackBadge.Text     = "";
        s.DetailDesc.Text           = "";
        s.DetailIcon.Visible        = false;
        s.DetailIconFallback.Visible= true;
        s.ApplyBtn.Disabled         = true;
        s.CombatWarningLabel.Visible= false;
    }

    private static void ShowPowerDetail(State s, PowerModel power)
    {
        s.DetailName.Text = PowerActions.GetPowerDisplayName(power);

        var typeCol = TypeColor(power.Type);
        s.DetailTypeBadge.Text  = power.Type.ToString();
        s.DetailTypeBadge.AddThemeColorOverride("font_color", typeCol);

        s.DetailStackBadge.Text = power.StackType.ToString();
        s.DetailStackBadge.AddThemeColorOverride("font_color", DevModeTheme.Subtle);

        // Icon — try BigIcon first, fall back to atlas Icon
        Texture2D? icon = null;
        try { icon = power.BigIcon; } catch { }
        if (icon == null) { try { icon = power.Icon; } catch { } }

        if (icon != null)
        {
            s.DetailIcon.Texture         = icon;
            s.DetailIcon.Visible         = true;
            s.DetailIconFallback.Visible = false;
        }
        else
        {
            s.DetailIcon.Visible         = false;
            s.DetailIconFallback.Visible = true;
            s.DetailIconFallback.Color   = typeCol.Darkened(0.5f);
        }

        // Description
        try
        {
            var raw = power.Description?.GetFormattedText();
            s.DetailDesc.Text = string.IsNullOrEmpty(raw)
                ? I18N.T("power.no_desc", "(No description)")
                : DevModeTheme.ConvertGameBbcode(raw);
        }
        catch { s.DetailDesc.Text = I18N.T("power.no_desc", "(No description)"); }

        s.ApplyBtn.Disabled = false;

        // Show combat warning if not in combat
        var inCombat = CombatManager.Instance?.IsInProgress ?? false;
        s.CombatWarningLabel.Visible = !inCombat;

        RefreshCurrentPowers(s, s.Player);
    }

    private static void RefreshCurrentPowers(State s, Player player)
    {
        foreach (var child in s.CurrentPowersList.GetChildren())
            ((Node)child).QueueFree();

        var powers = player.Creature?.Powers?.Where(p => p != null).ToArray() ?? [];

        if (powers.Length == 0)
        {
            var none = new Label { Text = I18N.T("power.none_active", "No active powers") };
            none.AddThemeFontSizeOverride("font_size", 11);
            none.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
            s.CurrentPowersList.AddChild(none);
            return;
        }

        foreach (var p in powers)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);

            // Small icon
            Texture2D? tex = null;
            try { tex = p.Icon; } catch { }
            if (tex != null)
            {
                row.AddChild(new TextureRect
                {
                    Texture           = tex,
                    ExpandMode        = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
                    CustomMinimumSize = new Vector2(18, 18),
                });
            }

            var col = TypeColor(p.Type);
            var nameLabel = new Label
            {
                Text                = $"{PowerActions.GetPowerDisplayName(p)}  ×{p.Amount}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment   = VerticalAlignment.Center,
            };
            nameLabel.AddThemeFontSizeOverride("font_size", 11);
            nameLabel.AddThemeColorOverride("font_color", col.Lerp(ColLight, 0.4f));
            row.AddChild(nameLabel);

            var removeBtn = new Button
            {
                Text              = "×",
                CustomMinimumSize = new Vector2(24, 22),
                FocusMode         = Control.FocusModeEnum.None,
            };
            removeBtn.AddThemeFontSizeOverride("font_size", 13);
            var captured = p;
            removeBtn.Pressed += () =>
            {
                PowerActions.RemovePower(player.Creature!, captured);
                RefreshCurrentPowers(s, player);
            };
            row.AddChild(removeBtn);

            s.CurrentPowersList.AddChild(row);
        }
    }

    // ─────────────────────────────── Target buttons ───────────────────────────────

    private static void SyncTargetButtons(State s)
    {
        SetTargetActive(s.BtnSelf,       s.Target == PowerTarget.Self);
        SetTargetActive(s.BtnAllEnemies, s.Target == PowerTarget.AllEnemies);
        SetTargetActive(s.BtnAllies,     s.Target == PowerTarget.Allies);
    }

    private static void SetTargetActive(Button btn, bool active)
    {
        btn.AddThemeColorOverride("font_color", active ? DevModeTheme.Accent : DevModeTheme.Subtle);
        btn.AddThemeStyleboxOverride("normal", MakeTargetStyle(active ? DevModeTheme.AccentAlpha : DevModeTheme.PanelBorder));
        btn.AddThemeStyleboxOverride("hover",  MakeTargetStyle(active ? DevModeTheme.Accent      : new Color(0.4f, 0.4f, 0.55f)));
    }

    private static StyleBoxFlat MakeTargetStyle(Color borderCol) => new()
    {
        BgColor                = new Color(0.16f, 0.16f, 0.22f),
        CornerRadiusTopLeft    = 5, CornerRadiusTopRight    = 5,
        CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5,
        BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
        BorderColor            = borderCol,
        ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 4, ContentMarginBottom = 4,
    };

    private static Button MakeTargetButton(string label) => new()
    {
        Text                = label,
        FocusMode           = Control.FocusModeEnum.None,
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        CustomMinimumSize   = new Vector2(0, 28),
    };

    // ─────────────────────────────── Helpers ───────────────────────────────

    private static StyleBoxFlat MakeFrameStyle(Color bg, Color border, float borderAlpha) => new()
    {
        BgColor                = bg,
        CornerRadiusTopLeft    = FrameRadius, CornerRadiusTopRight    = FrameRadius,
        CornerRadiusBottomLeft = FrameRadius, CornerRadiusBottomRight = FrameRadius,
        BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
        BorderColor            = border with { A = borderAlpha },
    };

    private static Label MakeBadgeLabel(string text, Color col)
    {
        var lbl = new Label { Text = text };
        lbl.AddThemeFontSizeOverride("font_size", 10);
        lbl.AddThemeColorOverride("font_color", col);
        return lbl;
    }

    private static void ApplyBtnStyle(Button btn)
    {
        var style = new StyleBoxFlat
        {
            BgColor                = new Color(0.28f, 0.48f, 0.72f, 0.90f),
            CornerRadiusTopLeft    = 6, CornerRadiusTopRight    = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
        };
        var hoverStyle = new StyleBoxFlat
        {
            BgColor                = new Color(0.35f, 0.58f, 0.85f, 0.95f),
            CornerRadiusTopLeft    = 6, CornerRadiusTopRight    = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
        };
        btn.AddThemeStyleboxOverride("normal",  style);
        btn.AddThemeStyleboxOverride("pressed", style);
        btn.AddThemeStyleboxOverride("hover",   hoverStyle);
        btn.AddThemeFontSizeOverride("font_size", 13);
        btn.AddThemeColorOverride("font_color", ColLight);
    }

    private static Color TypeColor(PowerType type) => type switch
    {
        PowerType.Buff   => ColBuff,
        PowerType.Debuff => ColDebuff,
        _                => ColNone,
    };

    private static ColorRect MakeDivider() => new()
    {
        Color             = DevModeTheme.Separator,
        CustomMinimumSize = new Vector2(0, 1),
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
    };
}
