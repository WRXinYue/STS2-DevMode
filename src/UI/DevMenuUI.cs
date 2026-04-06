using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace DevMode.UI;

internal sealed class DevMenuActions
{
    public required Action OnNewTest       { get; init; }
    public required Action OnCardLibrary   { get; init; }
    public required Action OnRelicCollection { get; init; }
}

/// <summary>
/// Replaces the main menu buttons in-place with dev mode options,
/// reusing the game's own NMainMenuTextButton style and container.
/// </summary>
internal static class DevMenuUI
{
    private const string ButtonsContainerPath = "%MainMenuTextButtons";

    private static NMainMenu? _mainMenu;
    private static readonly List<NMainMenuTextButton> _addedButtons = new();
    private static readonly List<(Control control, bool wasVisible)> _hiddenControls = new();

    private static readonly FieldInfo? LocStringField =
        AccessTools.Field(typeof(NMainMenuTextButton), "_locString");

    public static void Show(NMainMenu mainMenu, DevMenuActions actions)
    {
        _mainMenu = mainMenu;

        var container = mainMenu.GetNodeOrNull<Control>(ButtonsContainerPath);
        if (container == null)
        {
            MainFile.Logger.Warn("DevMode: Could not find MainMenuTextButtons container.");
            return;
        }

        NMainMenuTextButton? template = null;
        _hiddenControls.Clear();
        foreach (var child in container.GetChildren())
        {
            if (child is not Control ctrl) continue;
            _hiddenControls.Add((ctrl, ctrl.Visible));
            ctrl.Visible = false;
            template ??= ctrl as NMainMenuTextButton;
        }

        if (template == null)
        {
            MainFile.Logger.Warn("DevMode: No NMainMenuTextButton found to duplicate.");
            RestoreButtons();
            return;
        }

        _addedButtons.Clear();
        AddButton(container, template, I18N.T("devmenu.newTest", "New Test"), () => { Hide(); actions.OnNewTest(); });

        bool anySlot = false;
        for (int i = 0; i <= SnapshotManager.SlotCount; i++)
        {
            if (SnapshotManager.HasSlot(i)) { anySlot = true; break; }
        }

        var loadBtn = AddButton(container, template, I18N.T("devmenu.loadSnapshot", "Load Snapshot"), () =>
        {
            // Attach to tree root so FullRect covers the whole screen
            SnapshotSlotUI.Show(mainMenu.GetTree().Root, saveMode: false, onConfirm: slot =>
            {
                SnapshotSlotUI.Hide();
                Hide();
                SnapshotManager.LoadFromSlot(slot);
            });
        });
        if (!anySlot)
            loadBtn.SetEnabled(false);

        AddButton(container, template, I18N.T("devmenu.cardLibrary", "Card Library"), () => { Hide(); actions.OnCardLibrary(); });
        AddButton(container, template, I18N.T("devmenu.relicCollection", "Relic Collection"), () => { Hide(); actions.OnRelicCollection(); });
        AddButton(container, template, I18N.T("devmenu.back", "Back"), Hide);
    }

    public static void Hide()
    {
        if (_mainMenu == null || !GodotObject.IsInstanceValid(_mainMenu)) return;

        SnapshotSlotUI.Hide();

        foreach (var btn in _addedButtons)
        {
            if (GodotObject.IsInstanceValid(btn))
                btn.QueueFree();
        }
        _addedButtons.Clear();

        RestoreButtons();
        _mainMenu = null;
    }

    public static bool IsVisible => _mainMenu != null && GodotObject.IsInstanceValid(_mainMenu);

    /// <summary>Re-hides original buttons after RefreshButtons() runs.</summary>
    public static void ReapplyHide()
    {
        foreach (var (ctrl, _) in _hiddenControls)
        {
            if (GodotObject.IsInstanceValid(ctrl))
                ctrl.Visible = false;
        }
    }

    private static void RestoreButtons()
    {
        foreach (var (ctrl, wasVisible) in _hiddenControls)
        {
            if (GodotObject.IsInstanceValid(ctrl))
                ctrl.Visible = wasVisible;
        }
        _hiddenControls.Clear();
    }

    private static NMainMenuTextButton AddButton(Control container, NMainMenuTextButton template, string text, Action action)
    {
        var btn = (NMainMenuTextButton)template.Duplicate(14);
        btn.Name = $"DevModeBtn_{text.Replace(" ", "")}";
        btn.Visible = true;
        btn.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => action()));
        container.AddChild(btn);

        LocStringField?.SetValue(btn, null);
        if (btn.label != null)
        {
            btn.label.Text = text;
            btn.label.Modulate = Colors.White;
            btn.label.SelfModulate = new Color("FFF6E2"); // StsColors.cream
            btn.label.Scale = Vector2.One;
        }

        _addedButtons.Add(btn);
        return btn;
    }
}
