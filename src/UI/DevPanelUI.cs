using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using DevMode.Icons;

namespace DevMode.UI;

internal static partial class DevPanelUI
{
    private const string RootName        = "DevModeRailRoot";
    private const string TopBarName      = "DevModeTopBar";
    private const string OverlayName     = "DevModeOverlay";
    private const float  RailW           = 52f;
    private const float  IconBtnSize     = 36f;
    private const float  OverlayW        = 560f;
    private const int    Radius          = 14;

    private static Action? _onRefreshPanel;
    private static string? _activeOverlayId;
    private static int _pinRailCount;

    /// <summary>Pin the rail visible (e.g. while an external overlay is open). Call Unpin when done.</summary>
    public static void PinRail()  => _pinRailCount++;
    public static void UnpinRail() => _pinRailCount = Math.Max(0, _pinRailCount - 1);

    // ── Apple-style colour palette ──
    private static readonly Color ColRailBg       = new(0.10f, 0.10f, 0.12f, 0.88f);
    private static readonly Color ColRailBorder    = new(1f, 1f, 1f, 0.06f);
    private static readonly Color ColIconNormal    = new(0.62f, 0.62f, 0.68f);
    private static readonly Color ColIconHover     = new(0.85f, 0.85f, 0.92f);
    private static readonly Color ColIconActive    = new(0.40f, 0.68f, 1f);
    private static readonly Color ColIconActiveBg  = new(0.40f, 0.68f, 1f, 0.15f);
    private static readonly Color ColOverlayBg     = new(0.11f, 0.11f, 0.14f, 0.96f);
    private static readonly Color ColOverlayBorder = new(1f, 1f, 1f, 0.08f);
    private static readonly Color ColBackdrop      = new(0f, 0f, 0f, 0.50f);
    private static readonly Color ColSectionText   = new(0.50f, 0.50f, 0.58f);
    private static readonly Color ColSeparator     = new(1f, 1f, 1f, 0.06f);

    // ──────── Attach ────────
    public static void Attach(NGlobalUi globalUi, DevPanelActions actions)
    {
        if (((Node)globalUi).GetNodeOrNull<Control>(RootName) != null)
            return;

        _onRefreshPanel = actions.OnRefreshPanel;
        _activeOverlayId = null;

        var root = new Control
        {
            Name        = RootName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex      = 1200
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // ── Icon Rail (left edge, full height, rounded right corners) ──
        var rail = new PanelContainer
        {
            Name        = "Rail",
            MouseFilter = Control.MouseFilterEnum.Stop,
            AnchorLeft  = 0, AnchorRight  = 0,
            AnchorTop   = 0.15f, AnchorBottom = 0.85f,
            OffsetLeft  = 24, OffsetRight  = 24 + RailW,
            OffsetTop   = 0, OffsetBottom = 0
        };
        var railStyle = new StyleBoxFlat
        {
            BgColor                 = ColRailBg,
            CornerRadiusTopLeft     = Radius, CornerRadiusBottomLeft  = Radius,
            CornerRadiusTopRight    = Radius, CornerRadiusBottomRight = Radius,
            ContentMarginLeft       = 6, ContentMarginRight  = 6,
            ContentMarginTop        = 12, ContentMarginBottom = 12,
            BorderWidthRight        = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            BorderColor             = ColRailBorder,
            ShadowColor             = new Color(0, 0, 0, 0.25f),
            ShadowSize              = 8
        };
        rail.AddThemeStyleboxOverride("panel", railStyle);

        var railVBox = new VBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        railVBox.AddThemeConstantOverride("separation", 2);

        // ── Primary group: from registry ──
        foreach (var tab in DevPanelRegistry.GetTabs(DevPanelTabGroup.Primary))
        {
            var t = tab;
            var btn = CreateRailIcon(t.Icon, t.DisplayName);
            btn.Pressed += () =>
            {
                CloseAllOverlays(globalUi);
                t.OnActivate(globalUi);
            };
            railVBox.AddChild(btn);
        }

        // ── Spacer ──
        railVBox.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });

        // ── Separator line ──
        var sep = new HSeparator();
        sep.AddThemeStyleboxOverride("separator", new StyleBoxFlat
        {
            BgColor = ColSeparator,
            ContentMarginTop = 0, ContentMarginBottom = 0,
            ContentMarginLeft = 4, ContentMarginRight = 4
        });
        sep.AddThemeConstantOverride("separation", 8);
        railVBox.AddChild(sep);

        // ── Utility group: from registry ──
        foreach (var tab in DevPanelRegistry.GetTabs(DevPanelTabGroup.Utility))
        {
            var t = tab;
            var btn = CreateRailIcon(t.Icon, t.DisplayName);
            btn.Pressed += () =>
            {
                CloseAllOverlays(globalUi);
                t.OnActivate(globalUi);
            };
            railVBox.AddChild(btn);
        }

        rail.AddChild(railVBox);
        root.AddChild(rail);

        // ── Peek tab (small arrow visible when rail is hidden) ──
        var peekTab = new Button
        {
            Name              = "RailPeekTab",
            CustomMinimumSize = new Vector2(14, 48),
            AnchorLeft        = 0, AnchorRight  = 0,
            AnchorTop         = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft        = 0, OffsetRight  = 14,
            OffsetTop         = -24, OffsetBottom = 24,
            FocusMode         = Control.FocusModeEnum.None,
            MouseFilter       = Control.MouseFilterEnum.Stop,
            IconAlignment     = HorizontalAlignment.Center,
            Icon              = MdiIcon.ChevronRight.Texture(12, ColIconNormal)
        };
        var peekStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.15f, 0.6f),
            CornerRadiusTopLeft = 0, CornerRadiusBottomLeft = 0,
            CornerRadiusTopRight = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 0, ContentMarginRight = 0,
            ContentMarginTop = 0, ContentMarginBottom = 0
        };
        peekTab.AddThemeStyleboxOverride("normal",  peekStyle);
        peekTab.AddThemeStyleboxOverride("hover",   peekStyle);
        peekTab.AddThemeStyleboxOverride("pressed", peekStyle);
        peekTab.AddThemeStyleboxOverride("focus",   peekStyle);
        root.AddChild(peekTab);

        // ── Auto-hide: timer-based mouse position polling ──
        float hiddenX  = -(24 + RailW);
        float visibleX = 24f;
        bool  railShown = false;
        Tween? railTween = null;

        rail.OffsetLeft  = hiddenX;
        rail.OffsetRight = hiddenX + RailW;
        rail.Modulate    = new Color(1, 1, 1, 0);

        void SlideRail(bool show)
        {
            if (railShown == show) return;
            railShown = show;

            railTween?.Kill();
            railTween = rail.CreateTween();

            float targetLeft  = show ? visibleX : hiddenX;
            float targetRight = targetLeft + RailW;
            float targetAlpha = show ? 1f : 0f;

            railTween.TweenProperty(rail, "offset_left",  targetLeft,  0.2f)
                     .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            railTween.Parallel()
                     .TweenProperty(rail, "offset_right", targetRight, 0.2f)
                     .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            railTween.Parallel()
                     .TweenProperty(rail, "modulate:a",   targetAlpha, 0.15f)
                     .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

            peekTab.Visible = !show;
        }

        var pollTimer = new Timer
        {
            Name      = "RailPollTimer",
            WaitTime  = 0.1f,
            Autostart = true
        };
        float hitZoneRight = visibleX + RailW + 16f;

        pollTimer.Timeout += () =>
        {
            if (_activeOverlayId != null || _pinRailCount > 0)
            {
                if (!railShown) SlideRail(true);
                return;
            }

            var mousePos = root.GetViewport().GetMousePosition();
            var railRect = rail.GetGlobalRect();
            bool inHitZone = mousePos.X < hitZoneRight
                          && mousePos.Y > railRect.Position.Y - 20
                          && mousePos.Y < railRect.End.Y + 20;
            bool overRail = railShown && railRect.Grow(8).HasPoint(mousePos);

            if (inHitZone || overRail)
                SlideRail(true);
            else if (railShown)
                SlideRail(false);
        };
        root.AddChild(pollTimer);

        peekTab.Pressed += () => SlideRail(true);

        ((Node)globalUi).AddChild(root);
    }

    // ──────── Detach ────────
    public static void Detach(NGlobalUi globalUi)
    {
        _activeOverlayId = null;
        _pinRailCount = 0;
        ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();
        RemoveTopBar(globalUi);
        _onRefreshPanel = null;
    }

    // ──────── Close all known overlays (internal + external UIs) ────────
    private static readonly HashSet<string> _keepNodes = new() { RootName, TopBarName };

    /// <summary>
    /// Close the internal overlay (cheats/save/ai) and remove all DevMode external
    /// panels from globalUi.  Uses the "DevMode" naming convention so new panels are
    /// picked up automatically — no list to maintain.
    /// </summary>
    public static void CloseAllOverlays(NGlobalUi globalUi)
    {
        CloseOverlay(globalUi);

        var parent = (Node)globalUi;
        foreach (var child in parent.GetChildren())
        {
            if (child is Control ctrl
                && ctrl.Name.ToString().StartsWith("DevMode", StringComparison.Ordinal)
                && !_keepNodes.Contains(ctrl.Name))
            {
                parent.RemoveChild(ctrl);
                ctrl.QueueFree();
            }
        }
    }

    // ──────── Overlay: toggle / close ────────
    private static void ToggleOverlay(NGlobalUi globalUi, string id, Action<Control> buildContent)
    {
        if (_activeOverlayId == id)
        {
            CloseOverlay(globalUi);
            return;
        }

        CloseOverlay(globalUi);
        _activeOverlayId = id;

        var root = ((Node)globalUi).GetNodeOrNull<Control>(RootName);
        if (root == null) return;

        var clickaway = new Control
        {
            Name        = "OverlayClickaway",
            MouseFilter = Control.MouseFilterEnum.Stop,
            AnchorLeft  = 0, AnchorRight  = 1,
            AnchorTop   = 0, AnchorBottom = 1,
            OffsetLeft  = RailW + 32, OffsetRight = 0,
            OffsetTop   = 0, OffsetBottom = 0
        };
        clickaway.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true })
                CloseOverlay(globalUi);
        };
        root.AddChild(clickaway);
        root.MoveChild(clickaway, 0);

        var panel = CreateOverlayPanel();
        panel.Name = OverlayName;
        root.AddChild(panel);

        var content = panel.GetNode<VBoxContainer>("Content");
        buildContent(content);
    }

    private static void CloseOverlay(NGlobalUi globalUi)
    {
        var root = ((Node)globalUi).GetNodeOrNull<Control>(RootName);
        if (root == null) { _activeOverlayId = null; return; }

        var clickaway = root.GetNodeOrNull<Control>("OverlayClickaway");
        if (clickaway != null)
        {
            root.RemoveChild(clickaway);
            clickaway.QueueFree();
        }

        var panel = root.GetNodeOrNull<PanelContainer>(OverlayName);
        if (panel != null)
        {
            root.RemoveChild(panel);
            panel.QueueFree();
        }
        _activeOverlayId = null;
    }
}
