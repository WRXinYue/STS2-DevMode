using System;
using System.Collections.Generic;
using System.Linq;
using DevMode.Icons;
using Godot;

namespace DevMode.UI;

/// <summary>
/// Collapses mod character pool filters into one chip-style button with a checkable popup list.
/// </summary>
internal sealed partial class ModPoolFilterDropdown : Control {
    private static readonly MdiIcon ChevronDown = MdiIcon.From("chevron-down");

    private static readonly Color ColChipOff = DevModeTheme.ButtonBgNormal;
    private static readonly Color ColChipHover = DevModeTheme.ButtonBgHover;
    private static readonly Color ColChipOn = new(0.25f, 0.40f, 0.65f, 0.90f);
    private static readonly Color ColChipOnHover = new(0.30f, 0.48f, 0.75f, 0.95f);

    private readonly IReadOnlyList<(string key, string label)> _entries;
    private readonly HashSet<string> _activeFilters;
    private readonly Action _onFiltersChanged;
    private readonly Dictionary<string, CheckBox> _checkboxes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Control> _rows = new(StringComparer.Ordinal);

    private readonly PanelContainer _chip;
    private readonly Label _label;
    private readonly TextureRect _icon;
    private readonly TextureRect _chevron;
    private readonly PopupPanel _popup;
    private readonly LineEdit? _searchInput;
    private bool _hovered;

    public ModPoolFilterDropdown(
        IReadOnlyList<(string key, string label)> modEntries,
        HashSet<string> activeFilters,
        Action onFiltersChanged) {
        _entries = modEntries;
        _activeFilters = activeFilters;
        _onFiltersChanged = onFiltersChanged;

        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;

        _chip = new PanelContainer {
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.None
        };
        _chip.MouseEntered += () => { _hovered = true; RefreshButtonPresentation(); };
        _chip.MouseExited += () => { _hovered = false; RefreshButtonPresentation(); };
        _chip.GuiInput += OnChipInput;
        AddChild(_chip);

        var chipRow = new HBoxContainer();
        chipRow.AddThemeConstantOverride("separation", 4);
        _chip.AddChild(chipRow);

        _icon = new TextureRect {
            CustomMinimumSize = new Vector2(14, 14),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        chipRow.AddChild(_icon);

        _label = new Label {
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center
        };
        _label.AddThemeFontSizeOverride("font_size", 11);
        chipRow.AddChild(_label);

        _chevron = new TextureRect {
            CustomMinimumSize = new Vector2(12, 12),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        chipRow.AddChild(_chevron);

        _popup = new PopupPanel {
            MinSize = new Vector2I(200, 0)
        };
        var popupStyle = new StyleBoxFlat {
            BgColor = DevModeTheme.PanelBg,
            BorderColor = DevModeTheme.PanelBorder,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        _popup.AddThemeStyleboxOverride("panel", popupStyle);
        AddChild(_popup);

        var popupRoot = new VBoxContainer();
        popupRoot.AddThemeConstantOverride("separation", 6);
        _popup.AddChild(popupRoot);

        if (_entries.Count > 8) {
            _searchInput = new LineEdit {
                PlaceholderText = I18N.T("cardBrowser.poolModsSearch", "Search…"),
                ClearButtonEnabled = true
            };
            _searchInput.AddThemeFontSizeOverride("font_size", 11);
            _searchInput.TextChanged += ApplySearchFilter;
            popupRoot.AddChild(_searchInput);
        }

        var scroll = new ScrollContainer {
            CustomMinimumSize = new Vector2(0, Math.Min(280, _entries.Count * 28 + 8)),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        popupRoot.AddChild(scroll);

        var list = new VBoxContainer();
        list.AddThemeConstantOverride("separation", 2);
        list.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(list);

        foreach (var (key, label) in _entries) {
            var row = new MarginContainer();
            var checkbox = new CheckBox {
                Text = label,
                ButtonPressed = _activeFilters.Contains(key),
                FocusMode = Control.FocusModeEnum.None
            };
            checkbox.AddThemeFontSizeOverride("font_size", 11);
            checkbox.TooltipText = label;
            var capturedKey = key;
            checkbox.Toggled += on => OnCheckboxToggled(capturedKey, on);
            row.AddChild(checkbox);
            list.AddChild(row);
            _checkboxes[key] = checkbox;
            _rows[key] = row;
        }

        RefreshButtonPresentation();
    }

    public override Vector2 _GetMinimumSize() => _chip.GetMinimumSize();

    private void OnChipInput(InputEvent @event) {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) {
            OnButtonPressed();
            _chip.AcceptEvent();
        }
    }

    private void OnButtonPressed() {
        if (_searchInput != null)
            _searchInput.Text = "";
        ApplySearchFilter("");

        var pos = _chip.GlobalPosition;
        var size = _chip.Size;
        _popup.Popup(new Rect2I(
            (int)pos.X,
            (int)(pos.Y + size.Y + 2),
            Math.Max(200, (int)size.X),
            0));
    }

    private void OnCheckboxToggled(string key, bool on) {
        if (on)
            _activeFilters.Add(key);
        else
            _activeFilters.Remove(key);
        RefreshButtonPresentation();
        _onFiltersChanged();
    }

    private void ApplySearchFilter(string query) {
        var normalized = query.Trim();
        foreach (var (key, label) in _entries) {
            if (!_rows.TryGetValue(key, out var row)) continue;
            var visible = string.IsNullOrEmpty(normalized)
                || label.Contains(normalized, StringComparison.OrdinalIgnoreCase);
            row.Visible = visible;
        }
    }

    private void RefreshButtonPresentation() {
        var selected = _entries.Where(e => _activeFilters.Contains(e.key)).ToList();
        var active = selected.Count > 0;
        var iconColor = active || _hovered ? DevModeTheme.TextPrimary : DevModeTheme.Subtle;

        _label.Text = selected.Count switch {
            0 => I18N.T("cardBrowser.poolMods", "Mods"),
            1 => selected[0].label,
            _ => string.Format(I18N.T("cardBrowser.poolModsCount", "Mods ({0})"), selected.Count)
        };
        _label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _label.AddThemeColorOverride("font_color", iconColor);
        _icon.Texture = MdiIcon.PuzzleOutline.Texture(14, iconColor);
        _chevron.Texture = ChevronDown.Texture(12, iconColor);
        _chip.TooltipText = selected.Count > 1
            ? string.Join(", ", selected.Select(s => s.label))
            : selected.Count == 1 ? selected[0].label : I18N.T("cardBrowser.poolMods", "Mods");

        ApplyChipStyle(_chip, active, _hovered);
    }

    private static void ApplyChipStyle(PanelContainer chip, bool active, bool hovered) {
        StyleBoxFlat MakeChipStyle(Color bg) => new() {
            BgColor = bg,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };

        var bg = active ? ColChipOn : hovered ? ColChipHover : ColChipOff;
        if (active && hovered)
            bg = ColChipOnHover;
        chip.AddThemeStyleboxOverride("panel", MakeChipStyle(bg));
    }
}
