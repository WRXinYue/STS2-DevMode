using System;
using System.Collections.Generic;
using Godot;
using KitLib.Host;
using KitLib.Settings;
using KitLib.UI;

namespace KitLib.Integration;

/// <summary>
/// Hotkey settings section; capture follows official <c>NInputSettingsPanel</c> (_UnhandledKeyInput).
/// </summary>
internal partial class KitLibHotkeySettingsSection : VBoxContainer {
    internal static KitLibHotkeySettingsSection? Active { get; private set; }

    private static readonly (string ActionId, string LabelKey, string LabelFallback)[] Rows = {
        (HotkeyActionId.ToggleRail, "hotkeys.toggleRail", "Toggle sidebar"),
        (HotkeyActionId.ClosePanel, "hotkeys.closePanel", "Close panel"),
        (HotkeyActionId.NextTab, "hotkeys.nextTab", "Next tab"),
        (HotkeyActionId.PrevTab, "hotkeys.prevTab", "Previous tab"),
        (HotkeyActionId.LockRail, "hotkeys.lockRail", "Lock sidebar"),
        (HotkeyActionId.QuickSave, "hotkeys.quickSave", "Quick save"),
        (HotkeyActionId.QuickLoad, "hotkeys.quickLoad", "Quick load"),
        (HotkeyActionId.QuickReplayCombat, "hotkeys.quickReplayCombat", "Replay combat"),
        (HotkeyActionId.QuickReplayTurn, "hotkeys.quickReplayTurn", "Replay turn"),
        (HotkeyActionId.TogglePerfHud, "hotkeys.togglePerfHud", "Performance overlay"),
    };

    private readonly Dictionary<string, Button> _bindingButtons = new(StringComparer.Ordinal);
    private string? _listeningActionId;

    internal KitLibHotkeySettingsSection() {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 8);
        SetProcessUnhandledInput(true);
    }

    public override void _EnterTree() => Active = this;

    public override void _ExitTree() {
        CancelListening();
        if (Active == this)
            Active = null;
    }

    internal void Build() {
        AddChild(KitLibNativeModSettingsUi.CreateBoolToggle(
            I18N.T("settings.hotkeysEnabled", "Enable keyboard shortcuts"),
            null,
            () => SettingsStore.Current.HotkeysEnabled,
            enabled => {
                SettingsStore.SetHotkeysEnabled(enabled);
                NotifyChanged();
            }));

        foreach (var (actionId, labelKey, labelFallback) in Rows)
            AddChild(CreateBindingRow(actionId, labelKey, labelFallback));

        var resetBtn = new Button {
            Text = I18N.T("hotkeys.reset", "Reset shortcuts to defaults"),
            FocusMode = FocusModeEnum.All,
        };
        DevModeFormChrome.ApplyAccentPillButton(resetBtn);
        resetBtn.Pressed += () => {
            CancelListening();
            SettingsStore.ResetHotkeys();
            NotifyChanged();
            RefreshAllBindingButtons();
        };
        AddChild(resetBtn);
    }

    internal static void CancelCapture() => Active?.CancelListening();

    internal void CancelListening() {
        _listeningActionId = null;
        RefreshAllBindingButtons();
    }

    public override void _UnhandledKeyInput(InputEvent inputEvent) {
        if (_listeningActionId == null)
            return;
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        var actionId = _listeningActionId;
        CancelListening();

        if (key.Keycode == Key.Escape)
            return;

        var binding = HotkeyBinding.From(key);
        var reason = SettingsStore.TrySetHotkeyBinding(actionId, binding);
        if (reason != null) {
            MainFile.Logger.Info($"Hotkey rebind rejected: {I18N.T(reason, reason)}");
            return;
        }

        NotifyChanged();
        GetViewport()?.SetInputAsHandled();
    }

    private Control CreateBindingRow(string actionId, string labelKey, string labelFallback) {
        var bindBtn = new Button {
            CustomMinimumSize = new Vector2(DevModeFormChrome.Metrics.ChoiceRowMinWidth,
                DevModeFormChrome.Metrics.ValueColumnMinHeight),
            FocusMode = FocusModeEnum.All,
        };
        bindBtn.SetMeta(KitLibHotkeySettingsUi.BindingButtonMeta, true);
        StyleBindingButton(bindBtn, listening: false);
        UpdateBindingButtonText(bindBtn, actionId);
        bindBtn.Pressed += () => BeginListening(actionId, bindBtn);
        _bindingButtons[actionId] = bindBtn;

        return DevModeFormChrome.CreateLabeledValueRow(
            I18N.T(labelKey, labelFallback),
            null,
            bindBtn);
    }

    private void BeginListening(string actionId, Button bindBtn) {
        CancelListening();
        _listeningActionId = actionId;
        StyleBindingButton(bindBtn, listening: true);
        bindBtn.Text = I18N.T("hotkeys.listening", "Listening…");
    }

    private static void NotifyChanged() => KitLibHost.NotifyHotkeySettingsChanged?.Invoke();

    private void RefreshAllBindingButtons() {
        foreach (var (actionId, btn) in _bindingButtons) {
            if (!GodotObject.IsInstanceValid(btn))
                continue;
            StyleBindingButton(btn, _listeningActionId == actionId);
            UpdateBindingButtonText(btn, actionId);
        }
    }

    private void UpdateBindingButtonText(Button btn, string actionId) {
        if (_listeningActionId == actionId)
            return;
        btn.Text = SettingsStore.GetHotkeyBinding(actionId).FormatLabel();
    }

    private static void StyleBindingButton(Button btn, bool listening) {
        if (listening) {
            var sb = new StyleBoxFlat {
                BgColor = new Color(KitLibTheme.Accent.R, KitLibTheme.Accent.G, KitLibTheme.Accent.B, 0.42f),
                BorderColor = KitLibTheme.Accent,
                BorderWidthBottom = 2,
                BorderWidthTop = 2,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                ContentMarginLeft = 11,
                ContentMarginRight = 11,
                ContentMarginTop = 7,
                ContentMarginBottom = 7,
            };
            btn.AddThemeStyleboxOverride("normal", sb);
            btn.AddThemeStyleboxOverride("hover", sb);
            btn.AddThemeStyleboxOverride("pressed", sb);
            btn.AddThemeStyleboxOverride("focus", sb);
        }
        else {
            ModSettingsRitsuFormDevTheme.ApplyFieldControl(btn);
        }
        btn.AddThemeFontSizeOverride("font_size", 14);
        btn.AddThemeColorOverride("font_color", KitLibTheme.TextPrimary);
        btn.AddThemeColorOverride("font_hover_color", KitLibTheme.TextPrimary);
        btn.AddThemeColorOverride("font_pressed_color", KitLibTheme.TextPrimary);
    }
}
