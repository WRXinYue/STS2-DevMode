using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using DevMode.UI;

namespace DevMode.Patches;

/// <summary>
/// Adds a "Developer Mode" button below "Multiplayer" on the main menu.
/// When pressed, sets DevModeState.IsActive and opens the character select screen.
/// </summary>
[HarmonyPatch(typeof(NMainMenu))]
public static class MainMenuPatch
{
    private static NMainMenuTextButton? _devModeButton;
    private static NMainMenu? _mainMenuRef;

    private static readonly FieldInfo MultiplayerButtonField =
        AccessTools.Field(typeof(NMainMenu), "_multiplayerButton");

    private static readonly FieldInfo SingleplayerButtonField =
        AccessTools.Field(typeof(NMainMenu), "_singleplayerButton");

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void AddDevModeButton(NMainMenu __instance)
    {
        _mainMenuRef = __instance;

        var multiplayerBtn = (NMainMenuTextButton?)MultiplayerButtonField.GetValue(__instance);
        if (multiplayerBtn == null)
        {
            MainFile.Logger.Warn("DevMode: Could not find multiplayer button.");
            return;
        }

        var container = multiplayerBtn.GetParent();

        // Duplicate the multiplayer button without signal connections (flags: Groups|Scripts|UseInstantiation = 14)
        _devModeButton = (NMainMenuTextButton)multiplayerBtn.Duplicate(14);
        _devModeButton.Name = "DevModeButton";
        _devModeButton.Visible = true;

        // AddChild first so _Ready runs → ConnectSignals → label = GetChild<MegaLabel>(0)
        container.AddChild(_devModeButton);
        container.MoveChild(_devModeButton, multiplayerBtn.GetIndex() + 1);

        // Clear LocString so RefreshLabel() won't overwrite our text on translation change
        var locField = AccessTools.Field(typeof(NMainMenuTextButton), "_locString");
        locField?.SetValue(_devModeButton, null);

        if (_devModeButton.label != null)
        {
            _devModeButton.label.Text = "Developer Mode";
            // Reset any disabled/animation state copied from the template
            _devModeButton.label.Modulate = Colors.White;
            _devModeButton.label.SelfModulate = new Color("FFF6E2"); // StsColors.cream
            _devModeButton.label.Scale = Vector2.One;
        }

        // Connect the 'released' signal (same signal NMainMenu uses for its buttons)
        _devModeButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnDevModeButtonPressed));

        MainFile.Logger.Info("DevMode: Button added to main menu.");
    }

    /// <summary>
    /// Ensure the dev mode button follows the same visibility rules as the singleplayer button.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMainMenu.RefreshButtons))]
    public static void KeepDevButtonVisible(NMainMenu __instance)
    {
        if (__instance != _mainMenuRef) return;

        if (_devModeButton != null && GodotObject.IsInstanceValid(_devModeButton))
        {
            var singleplayerBtn = (NMainMenuTextButton?)SingleplayerButtonField.GetValue(__instance);
            if (singleplayerBtn != null)
                _devModeButton.Visible = singleplayerBtn.Visible;
        }

        // Prevent RefreshButtons from re-showing the original buttons while dev menu is open
        if (DevMenuUI.IsVisible)
            DevMenuUI.ReapplyHide();
    }

    private static void OnDevModeButtonPressed(NButton _)
    {
        if (_mainMenuRef == null) return;

        MainFile.Logger.Info("DevMode: Opening dev mode menu...");

        DevMenuUI.Show(_mainMenuRef, new UI.DevMenuActions
        {
            OnNewTest = () =>
            {
                DevModeState.IsActive = true;
                var charSelect = _mainMenuRef.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
                charSelect.InitializeSingleplayer();
                _mainMenuRef.SubmenuStack.Push(charSelect);
            },
            OnCardLibrary = () =>
            {
                DevModeState.InMenuPreview = true;
                var stack = _mainMenuRef.SubmenuStack;
                DevModeState.OnMenuPreviewClosed = () =>
                {
                    stack.Pop();
                    OnDevModeButtonPressed(null!);
                };
                AccessTools.Method(typeof(NMainMenu), "OpenCompendiumSubmenu")
                    ?.Invoke(_mainMenuRef, new object?[] { null });
                var compendium = stack.Peek();
                AccessTools.Method(compendium.GetType(), "OpenCardLibrary")
                    ?.Invoke(compendium, new object?[] { null });
            },
            OnRelicCollection = () =>
            {
                DevModeState.InMenuPreview = true;
                var stack = _mainMenuRef.SubmenuStack;
                DevModeState.OnMenuPreviewClosed = () =>
                {
                    stack.Pop();
                    OnDevModeButtonPressed(null!);
                };
                AccessTools.Method(typeof(NMainMenu), "OpenCompendiumSubmenu")
                    ?.Invoke(_mainMenuRef, new object?[] { null });
                var compendium = stack.Peek();
                AccessTools.Method(compendium.GetType(), "OpenRelicCollection")
                    ?.Invoke(compendium, new object?[] { null });
            }
        });
    }
}
