using System;
using DevMode.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace DevMode.Patches;

[HarmonyPatch(typeof(NMainMenu))]
public static class MainMenuPatch {
    private static NMainMenuTextButton? _devModeButton;
    private static NMainMenu? _mainMenuRef;

    [HarmonyPrefix]
    [HarmonyPatch("_Ready")]
    public static void AddDevModeButtonPrefix(NMainMenu __instance) {
        _mainMenuRef = __instance;

        var settingsBtn = __instance.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/SettingsButton");
        if (settingsBtn == null) {
            MainFile.Logger.Warn("DevMode: Could not find Settings button.");
            return;
        }

        var container = settingsBtn.GetParent();

        _devModeButton = MainMenuTextButtonFactory.CreateFrom(
            settingsBtn,
            container,
            "DevModeButton",
            I18N.T("menu.developerMode", "Developer Mode"),
            OnDevModeButtonPressed);

        container.MoveChild(_devModeButton, settingsBtn.GetIndex() + 1);

        MainFile.Logger.Info("DevMode: Main menu Developer Mode button added.");
    }

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void AddDevModeButtonPostfix(NMainMenu __instance) {
        if (__instance != _mainMenuRef || _devModeButton == null || !GodotObject.IsInstanceValid(_devModeButton))
            return;

        var textRow = __instance.GetNodeOrNull<Control>("%MainMenuTextButtons")
            ?? __instance.GetNodeOrNull<Control>("MainMenuTextButtons");
        if (textRow != null) {
            foreach (var child in textRow.GetChildren()) {
                if (child is NMainMenuTextButton button) {
                    button.FocusNeighborLeft = new NodePath(".");
                    button.FocusNeighborRight = new NodePath(".");
                }
            }
        }

        if (DevModeState.AutoProceedToCharSelect) {
            DevModeState.AutoProceedToCharSelect = false;
            DevModeState.InDevRun = true;
            MainFile.Logger.Info("DevMode: Auto-proceeding to character select (Restart with Seed).");
            var charSelect = __instance.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
            charSelect.InitializeSingleplayer();
            __instance.SubmenuStack.Push(charSelect);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMainMenu.RefreshButtons))]
    public static void KeepDevButtonVisible(NMainMenu __instance) {
        if (__instance != _mainMenuRef) return;

        if (_devModeButton != null && GodotObject.IsInstanceValid(_devModeButton)) {
            var settingsBtn = __instance.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/SettingsButton");
            if (settingsBtn != null)
                _devModeButton.Visible = settingsBtn.Visible;
        }

        if (DevMainMenuUI.IsVisible)
            DevMainMenuUI.ReapplyHide();
    }

    private static void OnDevModeButtonPressed(NButton _) {
        if (_mainMenuRef == null) return;

        MainFile.Logger.Info("DevMode: Opening dev mode menu...");

        DevMainMenuUI.Show(_mainMenuRef, new DevMainMenuActions {
            OnNewTest = () => {
                DevModeState.InDevRun = true;
                var charSelect = _mainMenuRef.SubmenuStack.GetSubmenuType<NCharacterSelectScreen>();
                charSelect.InitializeSingleplayer();
                _mainMenuRef.SubmenuStack.Push(charSelect);
            },
            OnCardLibrary = () => {
                DevModeState.InMenuPreview = true;
                var stack = _mainMenuRef.SubmenuStack;
                DevModeState.OnMenuPreviewClosed = () => {
                    stack.Pop();
                    OnDevModeButtonPressed(null!);
                };
                AccessTools.Method(typeof(NMainMenu), "OpenCompendiumSubmenu")
                    ?.Invoke(_mainMenuRef, [null]);
                var compendium = stack.Peek();
                AccessTools.Method(compendium.GetType(), "OpenCardLibrary")
                    ?.Invoke(compendium, [null]);
            },
            OnRelicCollection = () => {
                DevModeState.InMenuPreview = true;
                var stack = _mainMenuRef.SubmenuStack;
                DevModeState.OnMenuPreviewClosed = () => {
                    stack.Pop();
                    OnDevModeButtonPressed(null!);
                };
                AccessTools.Method(typeof(NMainMenu), "OpenCompendiumSubmenu")
                    ?.Invoke(_mainMenuRef, [null]);
                var compendium = stack.Peek();
                AccessTools.Method(compendium.GetType(), "OpenRelicCollection")
                    ?.Invoke(compendium, [null]);
            }
        });
    }
}
