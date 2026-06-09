using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace KitLib.UI;

/// <summary>LB/RB trigger page switcher (official settings tab manager pattern; one title at a time).</summary>
public partial class ModPanelPageTabChrome : Control {
    public readonly record struct PageEntry(string Id, string Label);

    private static readonly StringName TabLeftHotkey = MegaInput.viewDeckAndTabLeft;
    private static readonly StringName TabRightHotkey = MegaInput.viewExhaustPileAndTabRight;

    private TextureRect _leftTrigger = null!;
    private TextureRect _rightTrigger = null!;
    private Label _titleLabel = null!;
    private Label _indexLabel = null!;
    private readonly List<PageEntry> _pages = [];
    private int _selectedIndex;

    public event Action<string>? PageSelected;

    public int PageCount => _pages.Count;

    public ModPanelPageTabChrome() {
        Name = "ModPanelPageTabChrome";
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ShrinkBegin;
        CustomMinimumSize = new Vector2(0f, 48f);

        var row = new HBoxContainer {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride("separation", 14);
        AddChild(row);

        _leftTrigger = CreateTriggerIcon("LeftTriggerIcon");
        _leftTrigger.GuiInput += ev => OnTriggerGuiInput(ev, -1);
        row.AddChild(_leftTrigger);

        var titleWrap = new PanelContainer {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        titleWrap.AddThemeStyleboxOverride("panel", CreateTitlePanelStyle());
        var titleVBox = new VBoxContainer {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        titleVBox.AddThemeConstantOverride("separation", 2);
        _titleLabel = new Label {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 15);
        _titleLabel.AddThemeColorOverride("font_color", ModPanelUiPalette.LabelPrimary);
        titleVBox.AddChild(_titleLabel);
        _indexLabel = new Label {
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _indexLabel.AddThemeFontSizeOverride("font_size", 11);
        _indexLabel.AddThemeColorOverride("font_color", ModPanelUiPalette.RichTextSecondary);
        titleVBox.AddChild(_indexLabel);
        titleWrap.AddChild(titleVBox);
        row.AddChild(titleWrap);

        _rightTrigger = CreateTriggerIcon("RightTriggerIcon");
        _rightTrigger.GuiInput += ev => OnTriggerGuiInput(ev, 1);
        row.AddChild(_rightTrigger);
    }

    public void SetPages(IReadOnlyList<PageEntry> pages, string selectedPageId) {
        _pages.Clear();
        _pages.AddRange(pages);
        _selectedIndex = 0;
        for (var i = 0; i < _pages.Count; i++) {
            if (string.Equals(_pages[i].Id, selectedPageId, StringComparison.OrdinalIgnoreCase)) {
                _selectedIndex = i;
                break;
            }
        }
        Visible = _pages.Count > 1;
        UpdateDisplay();
        RefreshTriggerIcons();
    }

    public void ClearPages() {
        _pages.Clear();
        _selectedIndex = 0;
        Visible = false;
        _titleLabel.Text = "";
        _indexLabel.Text = "";
    }

    public string? GetSelectedPageId()
        => _pages.Count == 0 ? null : _pages[_selectedIndex].Id;

    public bool TrySwitchPage(int delta) {
        if (_pages.Count <= 1)
            return false;
        var next = Mathf.Clamp(_selectedIndex + delta, 0, _pages.Count - 1);
        if (next == _selectedIndex)
            return false;
        SelectIndex(next);
        return true;
    }

    public void RefreshTriggerIcons() {
        var show = _pages.Count > 1;
        _leftTrigger.Visible = show;
        _rightTrigger.Visible = show;
        if (!show)
            return;
        var usingController = NControllerManager.Instance?.IsUsingController == true;
        _leftTrigger.Texture = NInputManager.Instance.GetHotkeyIcon(TabLeftHotkey);
        _rightTrigger.Texture = NInputManager.Instance.GetHotkeyIcon(TabRightHotkey);
        var modulate = usingController ? Colors.White : new Color(1f, 1f, 1f, 0.45f);
        _leftTrigger.Modulate = modulate;
        _rightTrigger.Modulate = modulate;
    }

    private void SelectIndex(int index) {
        _selectedIndex = index;
        UpdateDisplay();
        PageSelected?.Invoke(_pages[index].Id);
    }

    private void UpdateDisplay() {
        if (_pages.Count == 0) {
            _titleLabel.Text = "";
            _indexLabel.Text = "";
            return;
        }
        _titleLabel.Text = _pages[_selectedIndex].Label;
        _indexLabel.Text = _pages.Count > 1 ? $"{_selectedIndex + 1} / {_pages.Count}" : "";
    }

    private void OnTriggerGuiInput(InputEvent ev, int delta) {
        if (ev is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            return;
        TrySwitchPage(delta);
        GetViewport()?.SetInputAsHandled();
    }

    private static TextureRect CreateTriggerIcon(string name) {
        return new TextureRect {
            Name = name,
            Visible = false,
            CustomMinimumSize = new Vector2(52f, 36f),
            MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
    }

    private static StyleBoxFlat CreateTitlePanelStyle() {
        var accent = ModPanelUiPalette.SidebarModActiveAccent;
        return new StyleBoxFlat {
            BgColor = new Color(accent.R, accent.G, accent.B, 0.12f),
            BorderColor = new Color(accent.R, accent.G, accent.B, 0.55f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 18,
            ContentMarginRight = 18,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
    }
}
