using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace KitLib.UI;

/// <summary>Sidebar mod row: mouse click via <see cref="Control.GuiInput" />; controller up/down selects on focus.</summary>
public partial class SidebarModRowControl : Control {
    private static readonly StyleBoxEmpty BlankFocusStyle = new();

    private Panel _bgPanel = null!;
    private StyleBoxFlat _innerStyle = null!;
    private Action? _onSelect;
    private bool _selected;
    private bool _pressing;

    public string ModId { get; private set; } = "";

    public void Configure(string modId, string displayName, string tooltip, StyleBoxFlat innerStyle, Action onSelect) {
        ModId = modId;
        _innerStyle = innerStyle;
        _onSelect = onSelect;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        CustomMinimumSize = new Vector2(0f, 62f);
        TooltipText = tooltip;
        AddThemeStyleboxOverride("focus", BlankFocusStyle);

        _bgPanel = new Panel {
            MouseFilter = MouseFilterEnum.Ignore,
            ClipContents = true,
        };
        _bgPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _bgPanel.AddThemeStyleboxOverride("panel", innerStyle);
        AddChild(_bgPanel);

        var titleLbl = new Label {
            MouseFilter = MouseFilterEnum.Ignore,
            Text = displayName,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            LabelSettings = new LabelSettings {
                FontSize = 22,
                FontColor = ModPanelUiPalette.LabelPrimary,
            },
        };
        titleLbl.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var labelLeft = 18 + (int)ModPanelUiMetrics.SidebarModAccentBarWidth +
                        ModPanelUiMetrics.SidebarModAccentTextGutter;
        titleLbl.OffsetLeft = labelLeft;
        titleLbl.OffsetRight = -18;
        titleLbl.OffsetTop = 10;
        titleLbl.OffsetBottom = -10;
        AddChild(titleLbl);
    }

    public override void _Ready() {
        GuiInput += OnGuiInput;
        Connect(Control.SignalName.FocusEntered, Callable.From(OnFocusEntered));
        Connect(Control.SignalName.FocusExited, Callable.From(RefreshChrome));
        Connect(Control.SignalName.MouseEntered, Callable.From(RefreshChrome));
        Connect(Control.SignalName.MouseExited, Callable.From(RefreshChrome));
        RefreshChrome();
    }

    public void SetSelected(bool selected) {
        if (_selected == selected)
            return;
        _selected = selected;
        RefreshChrome();
    }

    private void OnFocusEntered() {
        RefreshChrome();
        if (NControllerManager.Instance?.IsUsingController == true)
            _onSelect?.Invoke();
    }

    private void OnGuiInput(InputEvent @event) {
        if (@event is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left)
            return;
        if (mb.Pressed) {
            _pressing = true;
            RefreshChrome();
        }
        else {
            _pressing = false;
            _onSelect?.Invoke();
            RefreshChrome();
        }
        AcceptEvent();
    }

    private void RefreshChrome() {
        ModPanelUI.ApplySidebarModGroupInnerRowStyle(_innerStyle, _selected, _pressing, HasFocus());
    }
}
