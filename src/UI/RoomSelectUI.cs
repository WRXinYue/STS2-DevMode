using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using DevMode.Actions;
using DevMode.Icons;

namespace DevMode.UI;

/// <summary>Room teleport panel — lets the developer jump directly into any room type.</summary>
internal static class RoomSelectUI
{
    private const string RootName = "DevModeRoomSelect";
    private const float  PanelW   = 420f;

    // ── Room entry definitions ────────────────────────────────────────────────

    private readonly record struct RoomEntry(
        RoomType   Type,
        string     NameKey,
        string     NameFallback,
        string     DescKey,
        string     DescFallback,
        Color      Accent,
        MdiIcon    Icon);

    private static readonly RoomEntry[] Rooms =
    {
        new(RoomType.Shop,
            "room.type.shop",     "Shop",
            "room.desc.shop",     "Visit the merchant — buy and remove cards.",
            new Color(0.88f, 0.72f, 0.22f),
            MdiIcon.Star),

        new(RoomType.RestSite,
            "room.type.rest",     "Rest Site",
            "room.desc.rest",     "Rest or smith — recover HP or upgrade a card.",
            new Color(0.35f, 0.78f, 0.52f),
            MdiIcon.Heart),

        new(RoomType.Treasure,
            "room.type.treasure", "Treasure",
            "room.desc.treasure", "Open a chest — gain a relic.",
            new Color(0.90f, 0.65f, 0.20f),
            MdiIcon.TreasureChest),

        new(RoomType.Map,
            "room.type.map",      "Map",
            "room.desc.map",      "Return to the map screen.",
            new Color(0.42f, 0.68f, 0.92f),
            MdiIcon.Map),
    };

    // ── Public API ────────────────────────────────────────────────────────────

    public static void Show(NGlobalUi globalUi)
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

        var vbox = panel.GetNode<VBoxContainer>("Content");
        vbox.AddThemeConstantOverride("separation", 10);

        // ── Header ──
        BuildNavTab(vbox, I18N.T("room.nav.title", "Room Teleport"));

        // ── No-run warning ──
        var warnLabel = new Label
        {
            Text                = I18N.T("room.noRun", "No active run — start a run first."),
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible             = false,
        };
        warnLabel.AddThemeFontSizeOverride("font_size", 11);
        warnLabel.AddThemeColorOverride("font_color", new Color(0.88f, 0.55f, 0.35f));
        vbox.AddChild(warnLabel);

        // ── Room button list ──
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical    = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(list);
        vbox.AddChild(scroll);

        // ── Status label ──
        var statusLabel = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Center };
        statusLabel.AddThemeFontSizeOverride("font_size", 11);
        statusLabel.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        vbox.AddChild(statusLabel);

        // ── Build room buttons ──
        foreach (var entry in Rooms)
        {
            var card = BuildRoomCard(entry, warnLabel, statusLabel);
            list.AddChild(card);
        }

        ((Node)globalUi).AddChild(root);
    }

    public static void Remove(NGlobalUi globalUi)
        => ((Node)globalUi).GetNodeOrNull<Control>(RootName)?.QueueFree();

    // ── Widget builders ───────────────────────────────────────────────────────

    private static Control BuildRoomCard(RoomEntry entry, Label warnLabel, Label statusLabel)
    {
        var card = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var cardStyle = new StyleBoxFlat
        {
            BgColor                = DevModeTheme.ButtonBgNormal,
            CornerRadiusTopLeft    = 8, CornerRadiusTopRight    = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            BorderWidthLeft        = 3,
            BorderColor            = entry.Accent with { A = 0.6f },
        };
        card.AddThemeStyleboxOverride("panel", cardStyle);
        card.MouseFilter = Control.MouseFilterEnum.Stop;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   14);
        margin.AddThemeConstantOverride("margin_right",  14);
        margin.AddThemeConstantOverride("margin_top",    10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        margin.MouseFilter = Control.MouseFilterEnum.Ignore;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        hbox.MouseFilter = Control.MouseFilterEnum.Ignore;

        // Icon
        var iconRect = new TextureRect
        {
            Texture           = entry.Icon.Texture(20, entry.Accent),
            StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(24, 24),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter       = Control.MouseFilterEnum.Ignore,
        };
        hbox.AddChild(iconRect);

        // Text column
        var textCol = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        textCol.AddThemeConstantOverride("separation", 2);
        textCol.MouseFilter = Control.MouseFilterEnum.Ignore;

        var nameLabel = new Label { Text = I18N.T(entry.NameKey, entry.NameFallback) };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.AddThemeColorOverride("font_color", DevModeTheme.TextPrimary);
        nameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        textCol.AddChild(nameLabel);

        var descLabel = new Label
        {
            Text         = I18N.T(entry.DescKey, entry.DescFallback),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        descLabel.AddThemeFontSizeOverride("font_size", 11);
        descLabel.AddThemeColorOverride("font_color", DevModeTheme.Subtle);
        descLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        textCol.AddChild(descLabel);

        hbox.AddChild(textCol);

        // Chevron arrow
        var arrowRect = new TextureRect
        {
            Texture           = MdiIcon.ChevronRight.Texture(16, DevModeTheme.Subtle),
            StretchMode       = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(20, 20),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter       = Control.MouseFilterEnum.Ignore,
        };
        hbox.AddChild(arrowRect);

        margin.AddChild(hbox);
        card.AddChild(margin);

        // ── Hover style ──
        card.MouseEntered += () =>
        {
            cardStyle.BgColor      = DevModeTheme.ButtonBgHover;
            cardStyle.BorderColor  = entry.Accent with { A = 0.90f };
        };
        card.MouseExited += () =>
        {
            cardStyle.BgColor      = DevModeTheme.ButtonBgNormal;
            cardStyle.BorderColor  = entry.Accent with { A = 0.60f };
        };

        // ── Click ──
        var capturedEntry = entry;
        card.GuiInput += evt =>
        {
            if (evt is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                return;

            if (!RoomActions.IsRunInProgress)
            {
                warnLabel.Visible  = true;
                statusLabel.Text   = "";
                return;
            }

            warnLabel.Visible = false;
            bool ok = RoomActions.TryEnterRoom(capturedEntry.Type);
            statusLabel.Text = ok
                ? I18N.T("room.entered", "Entering: {0}", I18N.T(capturedEntry.NameKey, capturedEntry.NameFallback))
                : I18N.T("room.error",   "Failed to enter room.");
        };

        return card;
    }

    private static void BuildNavTab(VBoxContainer vbox, string title)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);

        var tab = new Button { Text = title, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 32) };
        var flat = new StyleBoxFlat
        {
            BgColor            = Colors.Transparent,
            ContentMarginLeft  = 16, ContentMarginRight = 16,
            ContentMarginTop   = 4,  ContentMarginBottom = 6,
        };
        foreach (var s in new[] { "normal", "hover", "pressed", "focus" })
            tab.AddThemeStyleboxOverride(s, flat);
        tab.AddThemeColorOverride("font_color", DevModeTheme.Accent);
        tab.AddThemeFontSizeOverride("font_size", 13);
        row.AddChild(tab);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        vbox.AddChild(row);
        vbox.AddChild(new ColorRect
        {
            CustomMinimumSize   = new Vector2(0, 1),
            Color               = DevModeTheme.Separator,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });
    }
}
