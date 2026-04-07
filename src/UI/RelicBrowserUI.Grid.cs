using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace DevMode.UI;

internal static partial class RelicBrowserUI
{
    private const float TileMinWidth   = 74f;
    private const float TileHeight     = 92f;
    private const float IconSize       = 44f;
    private const int   GridSeparation = 6;
    private const int   GridPadH       = 14;
    private const int   GridPadV       = 10;
    private const float TierStripH     = 3f;
    private const int   MaxColumns     = 8;

    private static Control CreateRelicTile(RelicModel relic, Player? player, State s)
    {
        var rarity = GetRelicRarity(relic);
        var rarityCol = RarityToColor(rarity);
        var name = GetRelicDisplayName(relic);

        var outer = new Control
        {
            CustomMinimumSize = new Vector2(TileMinWidth, TileHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Stop,
            FocusMode = Control.FocusModeEnum.None,
            TooltipText = name
        };

        // Background panel
        var bg = new Panel();
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        bg.MouseFilter = Control.MouseFilterEnum.Ignore;
        var bgStyle = new StyleBoxFlat
        {
            BgColor = ColTileBg,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            BorderColor = ColTileBorder
        };
        bg.AddThemeStyleboxOverride("panel", bgStyle);
        outer.AddChild(bg);

        // Content VBox
        var vbox = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 2);
        vbox.OffsetLeft = 4; vbox.OffsetRight = -4;
        vbox.OffsetTop = 6; vbox.OffsetBottom = -6;

        // Spacer above icon for vertical centering
        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 2), MouseFilter = Control.MouseFilterEnum.Ignore });

        // Icon
        Texture2D? iconTex = null;
        try { iconTex = relic.Icon; } catch { }

        if (iconTex != null)
        {
            var iconRect = new TextureRect
            {
                Texture = iconTex,
                CustomMinimumSize = new Vector2(IconSize, IconSize),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            vbox.AddChild(iconRect);
        }
        else
        {
            // Fallback: colored square with first char
            var fallback = new ColorRect
            {
                Color = rarityCol.Darkened(0.5f),
                CustomMinimumSize = new Vector2(IconSize, IconSize),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            vbox.AddChild(fallback);
        }

        // Name label
        var label = new Label
        {
            Text = name,
            HorizontalAlignment = HorizontalAlignment.Center,
            ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        label.AddThemeFontSizeOverride("font_size", 9);
        label.AddThemeColorOverride("font_color", new Color(0.72f, 0.72f, 0.78f));
        vbox.AddChild(label);

        outer.AddChild(vbox);

        // Rarity accent strip at bottom
        var strip = new ColorRect
        {
            Color = rarityCol,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 0, AnchorRight = 1,
            AnchorTop = 1, AnchorBottom = 1,
            OffsetLeft = 8, OffsetRight = -8,
            OffsetTop = -TierStripH - 3, OffsetBottom = -3
        };
        outer.AddChild(strip);

        // Owned badge (green dot in top-right for "All" view)
        if (IsAllSource && player != null && IsRelicOwned(relic, player))
        {
            var badge = new ColorRect
            {
                Color = new Color(0.3f, 0.75f, 0.45f, 0.85f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                AnchorLeft = 1, AnchorRight = 1,
                AnchorTop = 0, AnchorBottom = 0,
                OffsetLeft = -12, OffsetRight = -4,
                OffsetTop = 4, OffsetBottom = 12
            };
            outer.AddChild(badge);
        }

        // Hover / click
        outer.MouseEntered += () =>
        {
            if (s.SelectedRelic != relic)
                SetBgStyle(bg, ColTileHover, ColTileBorder);
        };
        outer.MouseExited += () =>
        {
            if (s.SelectedRelic != relic)
                SetBgStyle(bg, ColTileBg, ColTileBorder);
        };
        outer.GuiInput += evt =>
        {
            if (evt is not InputEventMouseButton mb || !mb.Pressed || mb.ButtonIndex != MouseButton.Left)
                return;
            SelectTile(s, bg, relic);
            outer.AcceptEvent();
        };

        return outer;
    }

    private static void SetBgStyle(Panel panel, Color bgColor, Color borderColor)
    {
        if (panel.GetThemeStylebox("panel") is StyleBoxFlat sb)
        {
            sb.BgColor = bgColor;
            sb.BorderColor = borderColor;
        }
    }

    private static void SelectTile(State s, Panel tileBg, RelicModel relic)
    {
        if (s.SelectedBg != null)
            SetBgStyle(s.SelectedBg, ColTileBg, ColTileBorder);

        s.SelectedBg = tileBg;
        s.SelectedRelic = relic;
        SetBgStyle(tileBg, ColTileSelected, new Color(0.40f, 0.68f, 1f, 0.35f));
        ShowRightPanel(s, relic);
    }

    private static bool IsRelicOwned(RelicModel relic, Player player)
    {
        try
        {
            var relicId = ((AbstractModel)relic).Id;
            return player.Relics.Any(r => ((AbstractModel)r).Id == relicId);
        }
        catch { return false; }
    }

    // ── Grid operations ──

    private static List<RelicModel> GetRelics(State s)
    {
        if (IsAllSource)
            return ModelDb.AllRelics.ToList();
        return s.Player.Relics.ToList();
    }

    private static void InvalidateRelicCache(State s)
    {
        s.CachedAllRelics = GetRelics(s);
    }

    private static void RebuildGrid(State s, string searchText)
    {
        s.SelectedBg = null;

        foreach (var child in s.RelicGrid.GetChildren())
        {
            s.RelicGrid.RemoveChild((Node)child);
            ((Node)child).QueueFree();
        }

        s.FilteredRelics = s.CachedAllRelics.Where(r =>
        {
            if (!MatchesRaritySet(r, s.ActiveRarityFilters)) return false;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var n = GetRelicDisplayName(r);
                var id = GetRelicId(r);
                var combined = n + " " + id;
                if (!combined.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }).ToList();

        s.FilteredRelics.Sort((a, b) => CompareRelics(a, b, s.CurrentSort, s.SortAsc));

        foreach (var relic in s.FilteredRelics)
        {
            var tile = CreateRelicTile(relic, s.Player, s);
            s.RelicGrid.AddChild(tile);
        }

        Callable.From(() => UpdateGridColumns(s)).CallDeferred();

        s.StatusLabel.Text = string.Format(I18N.T("relicBrowser.count", "{0} / {1} relics"),
            s.FilteredRelics.Count, s.CachedAllRelics.Count);
    }

    private static void UpdateGridColumns(State s)
    {
        if (!s.RelicGrid.IsNodeReady()) return;
        float w = s.GridScroll.GetRect().Size.X - 2f * GridPadH;
        if (w < 2f) return;
        float slotW = TileMinWidth + GridSeparation;
        int cols = Math.Max(1, (int)Math.Floor((w - 4f) / slotW));
        cols = Math.Min(cols, MaxColumns);
        if (s.RelicGrid.Columns != cols)
            s.RelicGrid.Columns = cols;
    }
}
