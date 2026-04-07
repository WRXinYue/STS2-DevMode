using System;
using System.Collections.Generic;
using Godot;

namespace DevMode.UI;

internal static partial class RelicBrowserUI
{
    // ── Nav / accent colours ──

    private static Color ColNavActive   => DevModeTheme.Accent;
    private static readonly Color ColNavInactive = new(0.55f, 0.55f, 0.62f);
    private static readonly Color ColNavHover    = new(0.78f, 0.78f, 0.85f);
    private static Color ColNavAccent   => DevModeTheme.AccentAlpha;

    // ── Panel / border colours ──

    private static Color ColPanelBg     => DevModeTheme.PanelBg;
    private static Color ColPanelBorder => DevModeTheme.PanelBorder;
    private static Color ColSubtle      => DevModeTheme.Subtle;

    // ── Tile colours ──

    private static readonly Color ColTileBg       = new(0.13f, 0.13f, 0.17f, 0.90f);
    private static readonly Color ColTileHover    = new(0.18f, 0.18f, 0.23f, 0.95f);
    private static readonly Color ColTileSelected = new(0.22f, 0.30f, 0.42f, 0.95f);
    private static readonly Color ColTileBorder   = new(1f, 1f, 1f, 0.05f);

    // ── Sort button colours ──

    private static readonly Color ColSortBg      = new(0.16f, 0.16f, 0.20f, 0.85f);
    private static readonly Color ColSortHover   = new(0.22f, 0.22f, 0.28f, 0.90f);
    private static readonly Color ColSortPressed = new(0.26f, 0.26f, 0.34f, 0.95f);

    // ── Segment filter colours (pill-shaped toggle) ──

    private static readonly Color ColSegOff      = new(0.14f, 0.14f, 0.18f, 0.80f);
    private static readonly Color ColSegHover    = new(0.20f, 0.20f, 0.26f, 0.85f);
    private static readonly Color ColSegOn       = new(0.25f, 0.40f, 0.65f, 0.90f);
    private static readonly Color ColSegOnHover  = new(0.30f, 0.48f, 0.75f, 0.95f);

    // ── Navigation tab ──

    private static Button CreateNavTab(string text, bool active)
    {
        var btn = new Button
        {
            Text = text,
            FocusMode = Control.FocusModeEnum.None,
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(0, 32)
        };

        var flat = new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 4, ContentMarginBottom = 6
        };
        btn.AddThemeStyleboxOverride("normal",  flat);
        btn.AddThemeStyleboxOverride("hover",   flat);
        btn.AddThemeStyleboxOverride("pressed", flat);
        btn.AddThemeStyleboxOverride("focus",   flat);

        btn.AddThemeColorOverride("font_color",         active ? ColNavActive : ColNavInactive);
        btn.AddThemeColorOverride("font_hover_color",   active ? ColNavActive : ColNavHover);
        btn.AddThemeColorOverride("font_pressed_color", ColNavActive);
        btn.AddThemeFontSizeOverride("font_size", 13);

        return btn;
    }

    // ── Sort toggle button ──

    private static Button CreateSortButton(string text)
    {
        var btn = new Button
        {
            Text = text,
            FocusMode = Control.FocusModeEnum.None,
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(0, 28)
        };

        StyleBoxFlat MakeStyle(Color bg) => new()
        {
            BgColor = bg,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ContentMarginLeft = 10, ContentMarginRight = 10,
            ContentMarginTop = 2, ContentMarginBottom = 2
        };

        btn.AddThemeStyleboxOverride("normal",  MakeStyle(ColSortBg));
        btn.AddThemeStyleboxOverride("hover",   MakeStyle(ColSortHover));
        btn.AddThemeStyleboxOverride("pressed", MakeStyle(ColSortPressed));
        btn.AddThemeStyleboxOverride("focus",   MakeStyle(ColSortBg));

        btn.AddThemeColorOverride("font_color", ColNavInactive);
        btn.AddThemeColorOverride("font_hover_color", ColNavHover);
        btn.AddThemeColorOverride("font_pressed_color", ColNavActive);
        btn.AddThemeFontSizeOverride("font_size", 12);

        return btn;
    }

    // ── Tier segment pill (toggle button) ──

    private static Button CreateSegmentChip(string text)
    {
        var btn = new Button
        {
            Text = text,
            ToggleMode = true,
            FocusMode = Control.FocusModeEnum.None,
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(0, 26)
        };

        StyleBoxFlat MakeStyle(Color bg) => new()
        {
            BgColor = bg,
            CornerRadiusTopLeft = 13, CornerRadiusTopRight = 13,
            CornerRadiusBottomLeft = 13, CornerRadiusBottomRight = 13,
            ContentMarginLeft = 10, ContentMarginRight = 10,
            ContentMarginTop = 2, ContentMarginBottom = 2
        };

        btn.AddThemeStyleboxOverride("normal",         MakeStyle(ColSegOff));
        btn.AddThemeStyleboxOverride("hover",          MakeStyle(ColSegHover));
        btn.AddThemeStyleboxOverride("pressed",        MakeStyle(ColSegOn));
        btn.AddThemeStyleboxOverride("hover_pressed",  MakeStyle(ColSegOnHover));
        btn.AddThemeStyleboxOverride("focus",          MakeStyle(ColSegOff));

        btn.AddThemeColorOverride("font_color",           new Color(0.60f, 0.60f, 0.68f));
        btn.AddThemeColorOverride("font_hover_color",     new Color(0.78f, 0.78f, 0.85f));
        btn.AddThemeColorOverride("font_pressed_color",   new Color(0.92f, 0.92f, 0.98f));
        btn.AddThemeFontSizeOverride("font_size", 11);

        return btn;
    }

    // ── Browser panel factory (spliced to rail, like CardBrowserUI) ──

    private static PanelContainer CreateBrowserPanel()
    {
        var panel = new PanelContainer
        {
            Name = "BrowserPanel",
            MouseFilter = Control.MouseFilterEnum.Stop,
            AnchorLeft = 0, AnchorRight = 1,
            OffsetLeft = PanelLeft, OffsetRight = -PanelRight,
            AnchorTop = 0.15f, AnchorBottom = 0.85f,
            OffsetTop = 0, OffsetBottom = 0
        };

        var style = new StyleBoxFlat
        {
            BgColor = ColPanelBg,
            CornerRadiusTopLeft = 0, CornerRadiusBottomLeft = 0,
            CornerRadiusTopRight = RailRadius, CornerRadiusBottomRight = RailRadius,
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 12, ContentMarginBottom = 16,
            BorderWidthLeft = 0,
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthRight = 1,
            BorderColor = ColPanelBorder,
            ShadowColor = new Color(0, 0, 0, 0.40f),
            ShadowSize = 20
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var content = new VBoxContainer { Name = "Content" };
        content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.AddThemeConstantOverride("separation", 8);
        panel.AddChild(content);

        float finalLeft = PanelLeft;
        panel.Ready += () =>
        {
            float slideOffset = 60f;
            panel.OffsetLeft = finalLeft - slideOffset;
            panel.Modulate = new Color(1, 1, 1, 0);

            var tween = panel.CreateTween();
            tween.TweenProperty(panel, "offset_left", finalLeft, 0.25f)
                 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.Parallel()
                 .TweenProperty(panel, "modulate:a", 1f, 0.18f)
                 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        };

        return panel;
    }

    // ── Action button (shared with right panel) ──

    private static Button CreateActionButton(string text, Color bgColor)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 36),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        var style = new StyleBoxFlat
        {
            BgColor = bgColor,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 5, ContentMarginBottom = 5
        };
        var hover = new StyleBoxFlat
        {
            BgColor = bgColor.Lightened(0.15f),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 5, ContentMarginBottom = 5
        };
        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus", style);
        btn.AddThemeFontSizeOverride("font_size", 13);
        return btn;
    }

    private static void ToggleSet<T>(HashSet<T> set, T value, bool on)
    {
        if (on) set.Add(value);
        else    set.Remove(value);
    }
}
